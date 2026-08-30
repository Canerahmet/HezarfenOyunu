using System;
using System.Collections.Generic;
using Hezarfen.Core;
using Hezarfen.Streaming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// <b>Haliç ve Boğaz trafiği</b> — kayık ve peremeleri suya koyar.
    ///
    /// ## Neden bu bir donatı değil, ulaşım
    ///
    /// RESEARCH.md "Ulaşım": *"kayık ve pereme (deniz taksisi) ana ulaşım…
    /// Boğaz ve Haliç geçişleri kayıkla."* **1632'de Haliç'te köprü yoktur.**
    /// İki yaka arasındaki her geçiş bir teknededir; boş bir Haliç, boş bir
    /// cadde kadar yanlıştır. Plan bu yüzden donatı geçişinde kayıkları
    /// ayrıca sayıyor.
    ///
    /// ## Nereye konur — sorulur, çizilmez
    ///
    /// Tekne <b>suda</b> ve <b>kıyıya yakın</b> durur. İkisi de araziden
    /// ölçülür: bir nokta ancak kotu deniz seviyesinin altındaysa sudur, ve
    /// ancak yakınında kara varsa kıyıdır. Açık denizin ortasına kayık
    /// serpmek, kürek teknesini olmayacağı yere koymak olurdu — bir kayıkçı
    /// kıyıdan uzaklaşmaz.
    ///
    /// İskelelerin çevresi ayrıca <b>yoğunlaşır</b>: iskele tarifelidir ve
    /// pereme orada bekler. Bağlı varyantlar (kürekleri indirilmiş) iskeleye
    /// yakın, kürekli olanlar açığa konur.
    ///
    /// ## Yön
    ///
    /// Tekne kıyıya <b>paralel</b> durur, dik değil: bağlı bir tekne
    /// rıhtıma yanaşır, açıktaki bir tekne de kıyı boyunca gider. Yön en
    /// yakın kara yönünün dikidir — yani gradyandan değil, <b>karanın
    /// nerede olduğundan</b> türer.
    /// </summary>
    public static class BoatScatter
    {
        public const string RootName = "TEKNELER_1632";
        const string TerrainScene = "Assets/_Project/Scenes/Faz1_Terrain.unity";
        const string BoatScene = "Assets/_Project/Scenes/Districts/D_Tekneler.unity";

        /// <summary>Suyun kıyıdan bu kadar açığına kadar tekne konur (m).</summary>
        public const float MaxOffshore = 260f;

        /// <summary>İki tekne arası en az bu kadar (m).</summary>
        public const float MinSpacing = 34f;

        static readonly string[] Kurekli =
            { "PF_Kayik", "PF_Pereme" };
        static readonly string[] Bagli =
            { "PF_Kayik_Bagli", "PF_Pereme_Bagli" };

        [MenuItem("Hezarfen/GIS/Tekneleri suya koy")]
        public static void ScatterMenu()
        {
            var scene = EditorSceneManager.OpenScene(TerrainScene, OpenSceneMode.Single);
            var tgo = GameObject.Find("TR_Istanbul");
            var terrain = tgo != null ? tgo.GetComponent<Terrain>() : null;
            if (terrain == null) throw new Exception("TR_Istanbul yok.");

            int n = Scatter(terrain, 1632, out int bagliN);

            EnsureFolder("Assets/_Project/Scenes/Districts");
            // Tekneler KENDI SULARININ sahnesine gider; ayri bir
            // "Tekneler" sahnesi YOK (gerekcesi asagida).
            SulariylaBirlikteKaydet();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TerrainScene);

            Debug.Log($"[Hezarfen] Tekneler: {n} adet ({bagliN} bagli, "
                      + $"{n - bagliN} kurekli) su semtlerine dagitildi.");
        }

        /// <summary>
        /// <b>Tekneleri kendi sularının semt sahnelerine dağıtır.</b>
        ///
        /// Sahne üretiliyordu (373 tekne, 1 MB YAML) ama <b>hiçbir yerden
        /// yüklenmiyordu</b>: ne yapı ayarlarının sahne listesinde, ne
        /// Addressables kataloğunda, ne <c>DistrictRegistry</c>'de vardı.
        /// Haliç ve Boğaz'ın üstünde uçan oyuncu için su bomboştu —
        /// 1632'de Haliç'te köprü yoktur ve tüm geçiş kayıkladır, yani
        /// eksik olan şey dönemin <b>en görünür ulaşım trafiği</b>.
        ///
        /// ## Neden ayrı bir "Tekneler" semti DEĞİL
        ///
        /// İlk düzeltme tek bir <c>D_Tekneler</c> semti kurdu. Çalıştı ama
        /// yanlıştı ve testi kırdı: tekneler Haliç'ten Boğaz'a yayılıyor,
        /// yani tek bir semt olarak yarıçapı <b>10,6 km</b> çıkıyor ve
        /// oyuncu Üsküdar'ın içinde otururken bile yüklü kalıyordu. Akış
        /// testi bunu yakaladı — üç semt aynı anda çözülme kuyruğunda.
        ///
        /// Su zaten semtlere bölünmüş durumda (<c>D_Halic</c>,
        /// <c>D_Bogaz</c>): tekne hangi suyun üstündeyse onun sahnesine
        /// yazılır. Yeni semt yok, yeni adres yok, kayıt değişmiyor — ve
        /// oyuncu o suya yaklaşınca tekneler onunla birlikte geliyor.
        /// </summary>
        static void SulariylaBirlikteKaydet()
        {
            var kok = GameObject.Find(RootName);
            if (kok == null) return;

            // Su semtleri: tanımları kayıttan okunur, elle yazılmaz.
            var kayit = AssetDatabase.LoadAssetAtPath<DistrictRegistry>(
                "Assets/_Project/Data/DistrictDefs/DistrictRegistry.asset");
            var sular = new List<DistrictDef>();
            if (kayit != null)
                foreach (var d in kayit.districts)
                    if (d != null && d.kind == DistrictKind.Water) sular.Add(d);
            if (sular.Count == 0)
            {
                Debug.LogError("[Hezarfen] Su semti yok — tekneler "
                               + "yuklenebilir bir yere yazilamadi.");
                return;
            }

            // Her tekneyi ait olduğu suya ata. Poligonun dışında kalan
            // (kıyı payı yüzünden) en yakın su semtine gider — ATILMAZ.
            var gruplar = new Dictionary<string, GameObject>();
            var sayilar = new Dictionary<string, int>();
            var tekneler = new List<Transform>();
            foreach (Transform t in kok.transform) tekneler.Add(t);

            foreach (var t in tekneler)
            {
                DistrictDef sahip = null;
                foreach (var d in sular)
                    if (d.Contains(t.position)) { sahip = d; break; }
                if (sahip == null)
                {
                    float en = float.MaxValue;
                    foreach (var d in sular)
                    {
                        float m = Vector2.Distance(
                            d.center, new Vector2(t.position.x, t.position.z));
                        if (m < en) { en = m; sahip = d; }
                    }
                }
                if (sahip == null) continue;

                if (!gruplar.TryGetValue(sahip.districtId, out var g))
                {
                    g = new GameObject(RootName);
                    var etiket = g.AddComponent<HistoricalTag>();
                    var kaynak = kok.GetComponent<HistoricalTag>();
                    if (kaynak != null)
                    {
                        etiket.tier = kaynak.tier;
                        etiket.sourceNote = kaynak.sourceNote;
                    }
                    gruplar[sahip.districtId] = g;
                    sayilar[sahip.districtId] = 0;
                }
                t.SetParent(g.transform, true);
                sayilar[sahip.districtId]++;
            }
            UnityEngine.Object.DestroyImmediate(kok);

            foreach (var kv in gruplar)
            {
                string yol = $"Assets/_Project/Scenes/Districts/{kv.Key}.unity";
                var mevcut = EditorSceneManager.OpenScene(yol, OpenSceneMode.Additive);
                // Eski tekneler once silinir: iki kez calistirmak sayiyi
                // ikiye katlamamali.
                foreach (var r in mevcut.GetRootGameObjects())
                    if (r.name == RootName) UnityEngine.Object.DestroyImmediate(r);
                EditorSceneManager.MoveGameObjectToScene(kv.Value, mevcut);
                EditorSceneManager.MarkSceneDirty(mevcut);
                EditorSceneManager.SaveScene(mevcut, yol);
                EditorSceneManager.CloseScene(mevcut, true);
                Debug.Log($"[Hezarfen] {sayilar[kv.Key]} tekne -> {kv.Key}");
            }
            AssetDatabase.SaveAssets();
        }

        public static int Scatter(Terrain terrain, int seed, out int bagliSayi)
        {
            var eski = GameObject.Find(RootName);
            if (eski != null) UnityEngine.Object.DestroyImmediate(eski);
            var kok = new GameObject(RootName);
            var tag = kok.AddComponent<HistoricalTag>();
            tag.tier = HistoricalTier.Reconstruction;      // T2
            tag.sourceNote =
                "Kayik ve pereme trafigi. RESEARCH.md 'Ulasim': kayik ve "
                + "pereme ana ulasim, Halic'te KOPRU YOK. Tip ve islev T1; "
                + "tekne SAYISI ve YERI taslak (T2) — kaynaklarda 1632'nin "
                + "kayik sayimi yok.";

            var iskeleler = PierPositions();
            // Su bolgelerinin kendi siniri var (D_Halic, D_Bogaz) ve tekne
            // oralara aittir. Ilk denemede yalnizca "su + kiyiya yakin"
            // olcutuyle serpildi ve 618 teknenin 600'u acik suya dagildi:
            // trafik degil konfeti. Halic 1632'de KOPRUSUZDUR, yani gecisin
            // kendisi oradadir; Bogaz gecilir ama daha seyrek; Marmara'nin
            // acigi kurek teknesinin isi degildir.
            var halic = WaterBoundary("D_Halic");
            var bogaz = WaterBoundary("D_Bogaz");
            var td = terrain.terrainData;
            Vector3 o = terrain.transform.position;

            // Izgara adimi tekne araligindan turer; her hucre kendi
            // tohumundan sarsilir (DistrictFiller ile ayni desen).
            float adim = MinSpacing * 1.6f;
            var konan = new List<Vector2>();
            int bagli = 0;

            int nx = Mathf.CeilToInt(td.size.x / adim);
            int nz = Mathf.CeilToInt(td.size.z / adim);
            for (int gz = 0; gz <= nz; gz++)
            for (int gx = 0; gx <= nx; gx++)
            {
                var rng = new System.Random(seed ^ (gx * 73856093) ^ (gz * 19349663));
                float jx = (float)(rng.NextDouble() - 0.5) * adim * 0.8f;
                float jz = (float)(rng.NextDouble() - 0.5) * adim * 0.8f;
                var p = new Vector2(o.x + gx * adim + jx, o.z + gz * adim + jz);

                if (!Water(terrain, p)) continue;
                if (!NearShore(terrain, p, out Vector2 karaYon)) continue;

                // Iskele yakini YOGUNLASIR, acik su SEYRELIR: iskele
                // tarifelidir ve pereme orada bekler.
                float iskeleUz = NearestPier(iskeleler, p);
                float sans;
                if (iskeleUz < 150f) sans = 0.90f;          // iskele onu
                else if (Inside(halic, p)) sans = 0.42f;    // Halic: gecis yolu
                else if (Inside(bogaz, p)) sans = 0.16f;    // Bogaz: gecilir
                else sans = 0.015f;                         // acik Marmara
                if (rng.NextDouble() > sans) continue;

                bool cakisma = false;
                foreach (var k in konan)
                    if ((k - p).sqrMagnitude < MinSpacing * MinSpacing)
                    { cakisma = true; break; }
                if (cakisma) continue;

                // Iskeleye yakinsa BAGLI (kurekleri indirilmis) varyant.
                bool bagliMi = iskeleUz < 150f;
                var havuz = bagliMi ? Bagli : Kurekli;
                string ad = havuz[rng.Next(havuz.Length)];
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/_Project/Art/Prefabs/{ad}.prefab");
                if (pf == null) continue;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, kok.transform);
                // Pivot SU HATTINDADIR (gen_kayik.py), yani y=0 dogru yuzdurur.
                go.transform.position = new Vector3(p.x, 0f, p.y);
                // Kiyiya PARALEL: kara yonunun diki.
                float aci = Mathf.Atan2(karaYon.x, karaYon.y) * Mathf.Rad2Deg + 90f;
                aci += (float)(rng.NextDouble() - 0.5) * 24f;
                go.transform.rotation = Quaternion.Euler(0f, aci, 0f);

                konan.Add(p);
                if (bagliMi) bagli++;
            }
            bagliSayi = bagli;
            return konan.Count;
        }

        /// <summary>Bu nokta su mu? Arazi kotu deniz seviyesinin altındaysa.</summary>
        static bool Water(Terrain terrain, Vector2 p)
        {
            var td = terrain.terrainData;
            Vector3 o = terrain.transform.position;
            float u = (p.x - o.x) / td.size.x, v = (p.y - o.z) / td.size.z;
            if (u < 0.002f || u > 0.998f || v < 0.002f || v > 0.998f) return false;
            return terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + o.y < -0.5f;
        }

        /// <summary>
        /// Kıyıya yakın mı, ve kara hangi yönde? Sekiz yönde tarar; ilk kara
        /// bulunan yön döner. Bulunamazsa açık denizdir ve tekne konmaz.
        /// </summary>
        static bool NearShore(Terrain terrain, Vector2 p, out Vector2 karaYon)
        {
            karaYon = Vector2.zero;
            for (float r = 40f; r <= MaxOffshore; r += 40f)
                for (int i = 0; i < 8; i++)
                {
                    float a = Mathf.PI * 2f * i / 8f;
                    var q = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    if (!Water(terrain, q))
                    {
                        karaYon = (q - p).normalized;
                        return true;
                    }
                }
            return false;
        }

        /// <summary>Su bölgesinin sınır poligonu (yoksa boş).</summary>
        static Vector2[] WaterBoundary(string districtId)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:DistrictDef"))
            {
                var d = AssetDatabase.LoadAssetAtPath<Hezarfen.Streaming.DistrictDef>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (d != null && d.districtId == districtId) return d.boundary;
            }
            return new Vector2[0];
        }

        static bool Inside(Vector2[] ring, Vector2 p)
        {
            if (ring == null || ring.Length < 3) return false;
            return Hezarfen.Streaming.DistrictDef.ContainsXZ(ring, p.x, p.y);
        }

        static List<Vector2> PierPositions()
        {
            var liste = new List<Vector2>();
            var kok = GameObject.Find(LandmarkPlacer.RootName);
            if (kok == null) return liste;
            foreach (Transform t in kok.transform)
                if (t.name.Contains("Iskele") || t.name.Contains("Kule"))
                    liste.Add(new Vector2(t.position.x, t.position.z));
            return liste;
        }

        static float NearestPier(List<Vector2> iskeleler, Vector2 p)
        {
            float en = float.MaxValue;
            foreach (var i in iskeleler)
                en = Mathf.Min(en, Vector2.Distance(i, p));
            return en;
        }

        static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            var parca = assetFolder.Split('/');
            string yol = parca[0];
            for (int i = 1; i < parca.Length; i++)
            {
                string alt = yol + "/" + parca[i];
                if (!AssetDatabase.IsValidFolder(alt))
                    AssetDatabase.CreateFolder(yol, parca[i]);
                yol = alt;
            }
        }
    }
}
