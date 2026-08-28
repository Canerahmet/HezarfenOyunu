using System.Collections.Generic;
using System.IO;
using Hezarfen.Core;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// <c>Assets/_Import/</c> iniş alanındaki FBX'leri kalıcı yerine taşır ve
    /// oynatılabilir prefab'a çevirir.
    ///
    /// Neden ayrı bir adım: CLAUDE.md "_Import sadece iniş alanı" der. Blender her
    /// koşuşunda oraya yazar; orası her an silinebilir olmalı. Varlığın kalıcı
    /// kimliği (GUID, prefab, sahne referansları) <c>_Project/Art/</c> altındadır.
    /// Taşımayı AssetDatabase yapar, böylece GUID korunur ve sahnedeki referanslar
    /// kopmaz — dosyayı Explorer'dan taşımak tam olarak bunu kırar.
    ///
    /// Prefab kurulumu FBX'ten üretilemeyecek üç şeyi ekler:
    ///   1. UCX_ mesh'inden convex MeshCollider (FBX collider taşımaz),
    ///   2. <see cref="HistoricalTag"/> (CLAUDE.md: her sahne öğesine zorunlu),
    ///   3. UCX yardımcı nesnesinin temizliği.
    /// LODGroup'u Unity, <c>_LOD0/_LOD1</c> adlandırmasından kendisi kurar.
    /// </summary>
    public static class ImportLanding
    {
        public const string LandingDir = "Assets/_Import";
        public const string ModelDir = "Assets/_Project/Art/Models";
        public const string PrefabDir = "Assets/_Project/Art/Prefabs";

        [MenuItem("Hezarfen/Boru Hatti/_Import'u yerlestir ve prefab uret")]
        public static void PromoteAllMenu()
        {
            int n = PromoteAll();
            Debug.Log($"[Hezarfen] Boru hatti: {n} model yerlestirildi.");
        }

        /// <summary>Batchmode'dan da çağrılabilir. Dönüş: işlenen model sayısı.</summary>
        public static int PromoteAll()
        {
            if (!AssetDatabase.IsValidFolder(LandingDir)) return 0;

            // Katalog Unity disinda (Blender kosusunda) degismis olabilir.
            AssetCatalog.Invalidate();
            EnsureFolder(ModelDir);
            EnsureFolder(PrefabDir);

            var moved = new List<string>();
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { LandingDir }))
                {
                    string src = AssetDatabase.GUIDToAssetPath(guid);
                    string dst = $"{ModelDir}/{Path.GetFileName(src)}";

                    if (src == dst) { moved.Add(dst); continue; }

                    // Ayni adli varlik varsa USTUNE yazmiyoruz: eski varligin GUID'i
                    // sahnelerde referansli olabilir. Once eskiyi silip yenisini
                    // tasimak GUID'i degistirir ve referanslari koparir. Bunun
                    // yerine dosya icerigini degistirip yeniden import ediyoruz.
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(dst) != null)
                    {
                        File.Copy(ToSystemPath(src), ToSystemPath(dst), overwrite: true);
                        AssetDatabase.DeleteAsset(src);
                        AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
                        moved.Add(dst);
                        continue;
                    }

                    string err = AssetDatabase.MoveAsset(src, dst);
                    if (!string.IsNullOrEmpty(err))
                    {
                        Debug.LogError($"[Hezarfen] Tasima basarisiz: {src} -> {dst}: {err}");
                        continue;
                    }
                    moved.Add(dst);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            foreach (string path in moved)
                BuildPrefab(path);

            AssetDatabase.SaveAssets();
            return moved.Count;
        }

        /// <summary>Bir model varlığından oynatılabilir prefab üretir. Dönüş: prefab yolu.</summary>
        public static string BuildPrefab(string modelPath)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogError($"[Hezarfen] Model okunamadi: {modelPath}");
                return null;
            }

            EnsureFolder(PrefabDir);

            string baseName = Path.GetFileNameWithoutExtension(modelPath);
            if (baseName.StartsWith("SM_")) baseName = baseName.Substring(3);
            string prefabPath = $"{PrefabDir}/PF_{baseName}.prefab";

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.name = $"PF_{baseName}";

            try
            {
                // FBX'ten gelen prefab bagini kopar: prefab'a bilesen ekleyecegiz,
                // model prefab'i degistirilemez (immutable).
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely,
                                                   InteractionMode.AutomatedAction);

                AttachCollider(inst);

                // UCX nesnesi silindikten sonra LOD sinirlari bayat kalir;
                // yeniden hesaplanmazsa LOD gecisleri yanlis mesafede tetiklenir.
                var lods = inst.GetComponent<LODGroup>();
                if (lods != null)
                {
                    lods.RecalculateBounds();
                    SetLodThresholds(lods);
                }

                // Tarihsel kademe KATALOGDAN gelir, elle konmaz.
                //
                // Bu prefab her boru hatti kosusunda sifirdan yazilir; elle
                // konan etiket ilk yeniden uretimde sessizce Graybox'a duserdi.
                // Karsiligi olmayan model Graybox kalir ama LOGLANIR — sessizce
                // dogru gorunmesindense gurultuyle eksik gorunmesi yegdir.
                var tag = inst.GetComponent<HistoricalTag>() ?? inst.AddComponent<HistoricalTag>();
                if (AssetCatalog.TryGet(baseName, out var entry))
                {
                    tag.tier = entry.Tier;
                    tag.sourceNote = entry.Source;
                }
                else
                {
                    tag.tier = HistoricalTier.Graybox;
                    tag.sourceNote = "";
                    Debug.LogWarning($"[Hezarfen] {baseName}: katalogda kayit yok, "
                                     + "HistoricalTag Graybox kaldi "
                                     + "(uretici scriptin katalogunu kontrol et).");
                }

                var saved = PrefabUtility.SaveAsPrefabAsset(inst, prefabPath, out bool ok);
                if (!ok || saved == null)
                {
                    Debug.LogError($"[Hezarfen] Prefab yazilamadi: {prefabPath}");
                    return null;
                }
            }
            finally
            {
                Object.DestroyImmediate(inst);
            }

            return prefabPath;
        }

        /// <summary>
        /// UCX_ mesh'ini convex MeshCollider'a çevirir ve yardımcı nesneyi siler.
        /// Convex: uçuş oyununda çarpışma sorgusu sık ve ucuz olmalı; ayrıca
        /// Rigidbody ile çarpışabilmesi için zaten convex şart.
        /// </summary>
        private static void AttachCollider(GameObject root)
        {
            MeshFilter ucx = null;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.gameObject.name.StartsWith("UCX_")) { ucx = mf; break; }
            }

            if (ucx == null)
            {
                Debug.LogWarning($"[Hezarfen] {root.name}: UCX_ mesh yok, collider eklenmedi.");
                return;
            }

            var col = root.GetComponent<MeshCollider>();
            if (col == null) col = root.AddComponent<MeshCollider>();
            col.sharedMesh = ucx.sharedMesh;
            col.convex = true;

            Object.DestroyImmediate(ucx.gameObject);
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            string[] parts = assetFolder.Split('/');
            string acc = parts[0];                       // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{acc}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(acc, parts[i]);
                acc = next;
            }
        }

        private static string ToSystemPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    

        /// <summary>
        /// LOD merdivenini <b>mevcut butun prefab'lara</b> uygular.
        ///
        /// Neden ayri bir komut: esikler boru hattinda yaziliyor ama boru
        /// hatti yalnizca <c>_Import</c>'a dusen varligi isler. Merdiven
        /// eklendiginde depoda zaten 150'den fazla prefab vardi ve onlari
        /// yenilemek icin butun FBX'leri yeniden aktarmak gerekirdi —
        /// ki bu, icerigi hic degismeyen 150 ikili dosyayi LFS'e KALICI
        /// olarak ikinci kez yazmak demekti (CLAUDE.md, yeniden uretim
        /// gurultusu kurali). Prefab'i yerinde duzeltmek hem dogru hem ucuz.
        /// </summary>
        [MenuItem("Hezarfen/Boru Hatti/LOD merdivenini uygula")]
        public static void ApplyLodLadderMenu()
        {
            int dokunulan = 0, atlanan = 0;
            foreach (var guid in AssetDatabase.FindAssets(
                         "t:Prefab", new[] { "Assets/_Project/Art/Prefabs" }))
            {
                string yol = AssetDatabase.GUIDToAssetPath(guid);
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
                if (pf == null) { atlanan++; continue; }
                var grup = pf.GetComponent<LODGroup>();
                if (grup == null) { atlanan++; continue; }
                SetLodThresholds(grup);
                EditorUtility.SetDirty(pf);
                dokunulan++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Hezarfen] LOD merdiveni: {dokunulan} prefab "
                      + $"guncellendi, {atlanan} atlandi (LODGroup yok).");
        }

        /// <summary>
        /// LOD gecis esikleri — Unity'nin varsayilanina BIRAKILMAZ.
        ///
        /// Esik, nesnenin ekran yuksekliginin oranidir; FOV 40 derecede
        /// mesafeye su formulle cevrilir: <c>d = boy / (esik * 2*tan(FOV/2))</c>.
        /// Varsayilan iki kademeli kurulumda 0,25 / 0,01 geliyordu ve bu
        /// OLCULDU: Suleymaniye'nin tam ayrintili mesh'i yalnizca
        /// <b>573 m</b>'ye kadar goruntuleniyordu, otesinde 456 ucgenlik
        /// blok. Hezarfen'in ucusu ise <b>3336 m</b> — yani butun ayrinti,
        /// oyunun merkez sahnesinde hic gorunmuyordu.
        ///
        /// Uc kademeli ladder (varlik uretecinde orta kademe eklendi):
        ///   LOD0 tam ayrinti  : 0,25'e kadar  → Suleymaniye'de ~570 m
        ///   LOD1 orta kademe  : 0,03'e kadar  → ~4 800 m (ucusun tamami)
        ///   LOD2 blok siluet  : 0,004'e kadar → ~36 km
        /// Iki kademeli eski varliklar icin ayni ladder'in ilk ve son
        /// basamagi kullanilir.
        /// </summary>
        /// <summary>Küçük/büyük ayrımı (m). Ev ~10 m, cami ~100 m.</summary>
        public const float SmallObjectSize = 40f;

        //  boy < 40 m  (ev, dukkan, mezar, agac…)
        static readonly float[] KucukUc = { 0.08f, 0.012f, 0.0015f };
        static readonly float[] KucukIki = { 0.08f, 0.0015f };
        //  boy >= 40 m (cami, sur, bedesten…)
        static readonly float[] BuyukUc = { 0.25f, 0.03f, 0.004f };
        static readonly float[] BuyukIki = { 0.25f, 0.004f };

        //  KARAKTER — boyu kucuk ama davranisi baska.
        //
        //  Bir ev uzaktan bakilan bir seydir; karakter UCUNCU SAHIS
        //  KAMERASINDA surekli 3-5 m otededir. Kucuk nesne merdiveni
        //  (0,08) ilk kademeyi ~20 m'de dusururdu ve oyuncu kendi
        //  karakterinin basitlestigini gorurdu. Ote yandan kul esigi
        //  0,0015 bir insani ~1 km'ye kadar cizmeye devam ederdi; oysa
        //  bir insan o mesafede zaten birkac pikseldir.
        //
        //  Yani karakterin merdiveni ters yonde ayarlanir: ilk kademe
        //  GEC duser (0,22 ~ 7 m), kul esigi ERKEN gelir (0,010 ~ 156 m).
        //  Faz 6'nin NPC kalabaligi da bu merdiveni kullanacak.
        static readonly float[] KarakterUc = { 0.22f, 0.04f, 0.010f };
        static readonly float[] KarakterIki = { 0.22f, 0.010f };

        /// <summary>
        /// Bu LODGroup için geçerli eşik merdiveni.
        ///
        /// <b>Tek kaynak.</b> Sayılar hem boru hattında hem testte
        /// gerekiyor ve testte KOPYALANMIŞLARDI — bu projenin defalarca
        /// yakaladığı "aynı sayı iki yerde" hatası. İkisi de artık burayı
        /// okur; merdiven değişince test kendiliğinden yeni sayıyı bekler.
        ///
        /// Karakter ayrımı bir LİSTEYE değil <b>yapıya</b> dayanır: deri
        /// bağlı (skinned) renderer taşıyan grup karakterdir. Bakımı
        /// gereken bir isim listesi, eklenmeyi unutulan ilk varlıkta yalan
        /// söylerdi.
        /// </summary>
        public static float[] Merdiven(LODGroup group)
        {
            var lods = group.GetLODs();
            bool ucKademe = lods.Length >= 3;
            if (Karakter(group))
                return ucKademe ? KarakterUc : KarakterIki;
            bool kucuk = group.size < SmallObjectSize;
            return ucKademe ? (kucuk ? KucukUc : BuyukUc)
                            : (kucuk ? KucukIki : BuyukIki);
        }

        /// <summary>Deri bağlı renderer taşıyorsa karakterdir.</summary>
        public static bool Karakter(LODGroup group)
        {
            return group != null
                && group.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                        .Length > 0;
        }

        public static void SetLodThresholds(LODGroup group)
        {
            var lods = group.GetLODs();
            if (lods.Length == 0) return;

            // Esik nesnenin BOYUNA gore secilir. Neden iki merdiven:
            //
            // Ekran yuksekligi orani buyuk yapilar icin dogru davraniyor ama
            // kucuk olanlar icin degil — OLCULDU. Tek merdivende (0,25/0,03/
            // 0,004) 10 m'lik bir ev yalnizca 55 m'ye kadar tam ayrintili
            // kaliyor ve 3 434 m'de kul ediliyordu. Planor 50-100 m'de ucuyor
            // ve Hezarfen'in ucusu 3 336 m: yani hem sehir hep orta kademede
            // goruntuleniyordu, hem varis semti ucus sirasinda yoktan var
            // oluyordu.
            //
            // Butce sayisi bunu GIZLEDI: %6,7 kullanim verimlilik gibi
            // okunuyordu, oysa yokluktu. Ayni bakista cizilen nesne sayisi
            // 472'ydi; kucuk merdivenle 3 194 oluyor — yani sehrin coğu
            // gorunmuyordu. Maliyeti %6,7'den %8,6'ya cikiyor.
            float[] esik = Merdiven(group);

            for (int i = 0; i < lods.Length; i++)
            {
                lods[i].screenRelativeTransitionHeight =
                    i < esik.Length ? esik[i] : esik[esik.Length - 1];
            }
            group.SetLODs(lods);
        }
}
}
