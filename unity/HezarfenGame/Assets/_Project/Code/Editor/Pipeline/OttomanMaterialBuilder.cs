using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// Blender'ın ürettiği doku bildiriminden HDRP <c>Lit</c> malzemeleri kurar.
    ///
    /// Neden bildirimden: malzeme adı ↔ doku dosyası eşlemesi <b>tek yerde</b>
    /// yaşamalı. İki tarafta elle tekrarlanırsa bir gün ayrışır ve ayrışma
    /// sessizdir — yanlış dokulu bir duvar "çalışıyor" görünür.
    /// Bildirimi <c>tools/textures/build_unity_maps.py</c> yazar.
    ///
    /// FBX'teki malzeme adları Blender'daki <c>M_...</c> adlarıyla aynıdır ve
    /// <c>ModelImportPolicy</c> import'u ada göre eşleştirir; yani bu malzemeler
    /// üretildikten sonra modeller onları kendiliğinden bulur.
    /// </summary>
    public static class OttomanMaterialBuilder
    {
        public const string TextureDir = "Assets/_Project/Art/Textures/Ottoman";
        public const string MaterialDir = "Assets/_Project/Art/Materials/Ottoman";
        private const string ManifestFile = "materials.json";

        [Serializable]
        private class Entry
        {
            public string name;
            public string kind;
            public string asset;
            public float[] sizeMeters;
            public float roughness = 0.8f;
            public float metallic;
            public bool baked;
            public string baseColorFile;
            public string maskFile;
            public string normalFile;
            public float[] baseColor;

            /// <summary>Alfa kesme — saç kartı gibi delikli yüzeyler.</summary>
            public bool alphaClip;

            /// <summary>Kesme eşiği; alfa bunun altındaysa piksel çizilmez.</summary>
            public float alphaCutoff = 0.5f;
        }

        [Serializable]
        private class Manifest { public Entry[] materials; }

        [MenuItem("Hezarfen/Boru Hatti/Osmanli malzemelerini uret")]
        public static void BuildMenu()
        {
            int n = Build(out var problems);
            // Eksik doku bir UYARI degil, KIRIK MALZEMEDIR.
            //
            // Bir kez sessizce geçti: çatı boyası değişince doku yeni bir adla
            // yazıldı, malzeme henüz import edilmemiş dosyayı bulamadı ve
            // `_BaseColorMap` NULL kaldı. Konsol "11 malzeme üretildi, 1 uyarı"
            // dedi; hata ancak Balat sahnesinde **bembeyaz evler** olarak
            // görüldü. Uyarı, geçmiş sayılan bir adımın içinde kaybolur.
            foreach (string p in problems) Debug.LogError($"[Hezarfen] {p}");
            if (problems.Count > 0)
                Debug.LogError($"[Hezarfen] {problems.Count} malzeme sorunu — "
                               + "duzeltmeden sahne kurma.");
            Debug.Log($"[Hezarfen] {n} HDRP malzemesi uretildi/guncellendi"
                      + (problems.Count > 0 ? $", {problems.Count} SORUN." : "."));
        }

        /// <summary>
        /// Bildirimde <c>kind == "pbr"</c> olan malzemelerin adları — yani
        /// **dokusu olması gereken**ler.
        ///
        /// Neden gerekli: kurşun ve cam bilerek dokusuzdur (uygun CC0 dokusu
        /// yok — ADR 0017), açıklık gölgesi de öyle. "Her malzemenin üç
        /// haritası olmalı" diyen bir test bunları haksız yere düşürür; elle
        /// muafiyet listesi tutmak ise listeyi zamanla yalancı yapar.
        /// Tek doğru kaynak bildirimin kendisidir.
        /// </summary>
        public static HashSet<string> PbrMaterialNames()
        {
            var set = new HashSet<string>();
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{TextureDir}/{ManifestFile}");
            if (json == null) return set;
            var manifest = JsonUtility.FromJson<Manifest>(json.text);
            if (manifest?.materials == null) return set;
            foreach (var e in manifest.materials)
                if (e.kind == "pbr") set.Add(e.name);
            return set;
        }

        /// <summary>Batchmode'dan da çağrılabilir. Dönüş: işlenen malzeme sayısı.</summary>
        public static int Build(out List<string> problems)
        {
            problems = new List<string>();

            string manifestPath = $"{TextureDir}/{ManifestFile}";
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(manifestPath);
            if (json == null)
            {
                problems.Add($"Bildirim yok: {manifestPath}. Once calistir: "
                             + "blender --background --python tools/textures/build_unity_maps.py");
                return 0;
            }

            var shader = Shader.Find("HDRP/Lit");
            if (shader == null)
            {
                problems.Add("HDRP/Lit shader bulunamadi — proje HDRP degil mi?");
                return 0;
            }

            var manifest = JsonUtility.FromJson<Manifest>(json.text);
            if (manifest?.materials == null || manifest.materials.Length == 0)
            {
                problems.Add($"Bildirim bos ya da okunamadi: {manifestPath}");
                return 0;
            }

            EnsureFolder(MaterialDir);
            int count = 0;

            foreach (var e in manifest.materials)
            {
                if (string.IsNullOrEmpty(e.name)) continue;
                string path = $"{MaterialDir}/{e.name}.mat";

                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool isNew = mat == null;
                if (isNew) mat = new Material(shader);
                else mat.shader = shader;

                Configure(mat, e, problems);
                HDMaterial.ValidateMaterial(mat);

                if (isNew) AssetDatabase.CreateAsset(mat, path);
                else EditorUtility.SetDirty(mat);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Modelleri yeniden import et: malzeme baglama import ANINDA olur
            // (ModelImportPolicy.OnAssignMaterialModel). Bu adim atlanirsa
            // malzemeler diskte dogru, sahnede eski gomulu hallerinde kalir —
            // ve bu, "her sey yolunda gorunuyor ama duvarlar duz renk" diye
            // ortaya cikan sinsi bir tutarsizliktir.
            ReimportGovernedModels(problems);

            Verify(manifest, problems);
            return count;
        }

        private static void ReimportGovernedModels(List<string> problems)
        {
            var dirs = new List<string>();
            foreach (string d in new[] { ImportLanding.ModelDir, ImportLanding.LandingDir })
                if (AssetDatabase.IsValidFolder(d)) dirs.Add(d);
            if (dirs.Count == 0) return;

            int n = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string guid in AssetDatabase.FindAssets("t:Model", dirs.ToArray()))
                {
                    AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid),
                                              ImportAssetOptions.ForceUpdate);
                    n++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
            if (n == 0) problems.Add("Yeniden import edilecek model bulunamadi.");
        }

        private static void Configure(Material mat, Entry e, List<string> problems)
        {
            mat.SetColor("_BaseColor", Color.white);

            if (e.kind == "untextured")
            {
                // Aciklik arkasindaki karanlik: dokusuz, cok mat, koyu.
                // Degerler DOGRUSAL uzayda (Blender paletiyle ayni), cunku HDRP
                // dogrusal renk uzayinda calisir ve SetColor degeri oldugu gibi gecer.
                var c = e.baseColor != null && e.baseColor.Length >= 3
                    ? new Color(e.baseColor[0], e.baseColor[1], e.baseColor[2], 1f)
                    : Color.black;
                mat.SetColor("_BaseColor", c);
                mat.SetTexture("_BaseColorMap", null);
                mat.SetTexture("_MaskMap", null);
                mat.SetTexture("_NormalMap", null);
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Smoothness", Mathf.Clamp01(1f - e.roughness));
                return;
            }

            // ALFA KESME: HDRP alfayi BASE MAP'IN ALFA KANALINDAN okur.
            //
            // Blender tarafinda alfa AYRI dosyadir ve olmak zorundadir: BC
            // sRGB, alfa Non-Color okunur ve ayni dosya iki renk uzayi
            // tasiyamaz. Iki motor iki bicim istiyor; birlestirme
            // `build_unity_maps.py`de yapiliyor.
            //
            // `_AlphaCutoffEnable` yazmadan sadece dokuyu vermek yetmez —
            // HDRP alfa kanalini gormezden gelir ve sac karti DUZ BIR
            // LEVHA olur. Bu sessiz bir hatadir: doku yuklenmis gorunur.
            if (e.alphaClip)
            {
                mat.SetFloat("_AlphaCutoffEnable", 1f);
                mat.SetFloat("_AlphaCutoff", Mathf.Clamp01(e.alphaCutoff));
                mat.SetFloat("_SurfaceType", 0f);          // Opaque + cutoff
                mat.SetFloat("_DoubleSidedEnable", 1f);    // kart iki yuzlu
                mat.SetFloat("_DoubleSidedNormalMode", 1f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_DOUBLESIDED_ON");
                mat.renderQueue = 2450;                    // AlphaTest
            }

            // DOKULU AMA RENGI PALETTEN GELEN MALZEME.
            //
            // `_BaseColor` dokulu malzemelerde beyaz birakiliyordu ve tas
            // ile ahsap icin dogruydu: onlarin dokusu rengini de tasir.
            // Kumas oyle degil — dokumasinin albedosu bilerek NOTR
            // uretiliyor (`gen_kumas_texture.py`) ve rengi paletten
            // geliyor. Beyaz birakmak on iki kumasi tek bir gri tona
            // dusururdu; ustelik `NPCYonetici` kisiden kisiye tonu
            // `_BaseColor` ile CARPTIGI icin kalabaligin renk cesitliligi
            // de sifira inerdi.
            //
            // Bayrak bildirimden gelir (`baseColor` dolu mu), koda gomulu
            // bir malzeme listesinden degil.
            if (e.baseColor != null && e.baseColor.Length >= 3)
                mat.SetColor("_BaseColor", new Color(
                    e.baseColor[0], e.baseColor[1], e.baseColor[2], 1f));
            mat.SetTexture("_BaseColorMap", LoadTex(e.baseColorFile, e.name, problems));
            mat.SetTexture("_NormalMap", LoadTex(e.normalFile, e.name, problems));
            mat.SetTexture("_MaskMap", LoadTex(e.maskFile, e.name, problems));
            mat.SetFloat("_NormalScale", 1f);

            // Metaliklik BİLDİRİMDEN gelir; burada sabit değildir.
            //
            // HDRP maskenin R kanalını `_Metallic` ile **çarpar**. Kitte metal
            // yokken 0 yazmak doğruydu ve öyle yazılmıştı. Kurşun örtü
            // eklendiğinde (ADR 0021) aynı satır sessizce yanlış oldu: maske
            // oksit örtüsünü piksel piksel taşıyor ama çarpan 0 olduğu için
            // kubbe yine mat gri kalırdı — doku "yüklenmiş" görünür, hata
            // görünmez. Çarpan artık rolün kendi beyanı.
            mat.SetFloat("_Metallic", Mathf.Clamp01(e.metallic));

            // Maske varken parlaklik ve AO maskeden gelir; yeniden esleme kimlik
            // birakilir ki Blender'da gorulen deger Unity'de degismesin.
            mat.SetFloat("_SmoothnessRemapMin", 0f);
            mat.SetFloat("_SmoothnessRemapMax", 1f);
            mat.SetFloat("_AORemapMin", 0f);
            mat.SetFloat("_AORemapMax", 1f);
            // Maske olmayan durum icin makul yedek.
            mat.SetFloat("_Smoothness", Mathf.Clamp01(1f - e.roughness));

            // Dosyalama YOK: UV zaten dunya olcekli uretiliyor (u = mesafe /
            // doku_boyu), yani tekrar sayisi mesh'te. Burada 1'den farkli bir
            // olcek vermek texel yogunlugunu ikinci kez carpar ve dokuyu bozar.
            mat.SetTextureScale("_BaseColorMap", Vector2.one);
            mat.SetTextureOffset("_BaseColorMap", Vector2.zero);
        }

        private static Texture LoadTex(string file, string owner, List<string> problems)
        {
            if (string.IsNullOrEmpty(file)) return null;
            string p = $"{TextureDir}/{file}";
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(p);
            if (tex == null) problems.Add($"{owner}: doku bulunamadi {p}");
            return tex;
        }

        /// <summary>
        /// Üretilen malzemeleri geri okuyup <b>sessiz</b> hataları arar.
        ///
        /// Aranan şey: maske ve normal haritalarının sRGB işaretli olmaması.
        /// Bu yanlışsa hiçbir hata çıkmaz; yalnızca yüzeyler yanlış eğride
        /// okunur. Yazdığını geri okumak bunu saniyede yakalar.
        /// </summary>
        private static void Verify(Manifest manifest, List<string> problems)
        {
            foreach (var e in manifest.materials)
            {
                if (e.kind != "pbr") continue;

                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/{e.name}.mat");
                if (mat == null) { problems.Add($"{e.name}: malzeme yazilamadi"); continue; }
                if (mat.GetTexture("_MaskMap") == null)
                    problems.Add($"{e.name}: maske haritasi bagli degil");
                // Albedo, GÖRÜLEN haritadır: bağlı değilse yapı bembeyaz çıkar.
                // Denetimde yalnızca maske aranıyordu; en görünür hata
                // denetlenmeyen tek hataydı.
                if (mat.GetTexture("_BaseColorMap") == null)
                    problems.Add($"{e.name}: albedo (_BaseColorMap) bagli degil — "
                                 + $"beklenen doku: {e.baseColorFile}");
                if (mat.GetTexture("_NormalMap") == null)
                    problems.Add($"{e.name}: normal haritasi bagli degil");
                // Maskenin metalikligi `_Metallic` ile CARPILDIGI icin carpanin
                // yanlis olmasi, maskeyi tamamen etkisiz birakabilir ve bu
                // hicbir uyari uretmez.
                float met = mat.GetFloat("_Metallic");
                if (Mathf.Abs(met - Mathf.Clamp01(e.metallic)) > 1e-3f)
                    problems.Add($"{e.name}: _Metallic {met:0.###}, "
                                 + $"bildirimde {e.metallic:0.###}");

                foreach (string file in new[] { e.maskFile, e.normalFile })
                {
                    if (string.IsNullOrEmpty(file)) continue;
                    var imp = AssetImporter.GetAtPath($"{TextureDir}/{file}") as TextureImporter;
                    if (imp == null) continue;
                    if (imp.sRGBTexture)
                        problems.Add($"{file}: sRGB isaretli ama VERI olmali "
                                     + "(TextureImportPolicy calismamis)");
                }
            }
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            string[] parts = assetFolder.Split('/');
            string acc = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{acc}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(acc, parts[i]);
                acc = next;
            }
        }
    }
}
