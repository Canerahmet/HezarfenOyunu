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
    /// Bir semti <b>mahallelerle doldurur</b> — Faz 4'ün çekirdeği.
    ///
    /// ## Neden ayrı bir sınıf
    ///
    /// <see cref="OttomanStreetBuilder"/> tek bir mahalleyi doğru kuruyordu ve
    /// yedi kuralı belgeliydi (eş yükselti takibi, sokak çizgisine oturan
    /// cephe, çıkmazlar…). Ama yalnızca <b>örnek sahne</b> üretiyordu: bir
    /// başlangıç noktası, bir yön, 46 düğümlük tek bir sokak. Faz 4'ün istediği
    /// şey semtin kendisi. Bu sınıf o sokağı çoğaltır; sokağın nasıl
    /// kurulacağına dair hiçbir kural burada YENİDEN yazılmaz.
    ///
    /// ## Deterministik olmak zorunda
    ///
    /// Plan bunu açıkça istiyor: *"Aynı seed = aynı şehir (test edilebilirlik)"*.
    /// Bu yüzden çekirdek konumları rastgele serpilmez; semtin sınır kutusu
    /// üzerinde <b>sabit bir ızgara</b> taranır ve her hücre kendi tohumundan
    /// türeyen bir sarsıntı alır. Tarama sırası ızgaranın kendisinden gelir,
    /// yani liste sırası, kayan nokta toplama sırası ya da sahnedeki nesne
    /// sırası sonucu değiştiremez.
    ///
    /// ## Nereye mahalle kurulmaz — ve neden bunlar
    ///
    /// Dördü de <b>araziye ya da sahneye sorulur</b>, elle çizilmez:
    /// <list type="bullet">
    /// <item><b>Su ve kıyı şeridi</b>: kot <c>minElevationMeters</c>'ın altı.
    ///       1632 kıyı çizgisi bugünkünden içeridedir ve dolgu alanları
    ///       (Eminönü, Sirkeci) yoktur.</item>
    /// <item><b>Dik yamaç</b>: eğim <c>maxSlopeDegrees</c>'ın üstü. İstanbul'un
    ///       yamacında sokak yokuşu yanlamasına tarar; dikleştiği yerde
    ///       yerleşim seyrelir, kaybolmaz — ama mahalle ÇEKİRDEĞİ (mescit
    ///       avlusu) düz zemin ister.</item>
    /// <item><b>Landmark çevresi</b>: külliyenin kendi alanı vardır ve oraya
    ///       mahalle kurmak Süleymaniye'nin avlusuna ev dikmek olurdu.</item>
    /// <item><b>Semt sınırı dışı</b>: <c>DistrictDef.boundary</c>.</item>
    /// </list>
    ///
    /// ## Sokağın yönü seçilmez, ÖLÇÜLÜR
    ///
    /// Her mahallenin ana sokağı eş yükselti eğrisini izler (Kural 1). Bu
    /// yüzden başlangıç yönü rastgele verilmez: arazinin o noktadaki
    /// <b>eğim gradyanına dik</b> yön alınır — yani yamacı yanlamasına tarayan
    /// yön. Düz zeminde gradyan anlamsızlaşır; orada ızgara tohumundan gelen
    /// bir açı kullanılır ve bu <b>dürüstlükdür</b>: düz zeminde sokağın
    /// yönünü belirleyen şey arazi değildir.
    /// </summary>
    public static class DistrictFiller
    {
        const string TerrainScene = "Assets/_Project/Scenes/Faz1_Terrain.unity";
        const string RegistryPath =
            "Assets/_Project/Data/DistrictDefs/DistrictRegistry.asset";

        /// <summary>Semtin kökü — silinip yeniden kurulur.</summary>
        public static string RootNameOf(string districtId) => $"SEMT_{districtId}";

        [MenuItem("Hezarfen/GIS/Semti doldur (Galata)")]
        public static void FillGalataMenu() => FillAndSave("D_Galata", 1632);

        [MenuItem("Hezarfen/GIS/Semti doldur (Uskudar)")]
        public static void FillUskudarMenu() => FillAndSave("D_Uskudar", 1632);

        /// <summary>
        /// Semti doldurur ve arazi sahnesini kaydeder. Menü yolu budur.
        /// </summary>
        public static void FillAndSave(string districtId, int seed)
        {
            var scene = EditorSceneManager.OpenScene(TerrainScene, OpenSceneMode.Single);

            var terrainGo = GameObject.Find("TR_Istanbul");
            var terrain = terrainGo != null ? terrainGo.GetComponent<Terrain>() : null;
            if (terrain == null)
                throw new Exception("TR_Istanbul yok — once GIS/Terrain uret.");

            var gis = GameObject.Find(GeoJsonImporter.RootName);
            if (gis != null) gis.SetActive(false);

            int mahalle = Fill(districtId, terrain, seed, out int ev);

            // Semt icerigi KENDI sahnesine kaydedilir, arazi sahnesine DEGIL.
            //
            // Ilk denemede Faz1_Terrain'e yazildi ve sahne 932 KB'dan 15 MB'a
            // cikti. Iki sebeple yanlisti: (1) streaming tasarimi zaten semt
            // basina bir sahne ongoruyor (`DistrictDef.sceneAddress`), (2) Faz 4
            // boyunca semt defalarca yeniden kurulacak ve her kurulum 15 MB'lik
            // bambaska bir YAML uretecekti — ADR 0059'un yeniden uretim
            // gurultusu, sahne olceginde.
            string hedef = $"Assets/_Project/Scenes/Districts/{districtId}.unity";
            EnsureFolder("Assets/_Project/Scenes/Districts");

            var semt = GameObject.Find(RootNameOf(districtId));
            var yeni = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                                                   NewSceneMode.Additive);
            if (semt != null) EditorSceneManager.MoveGameObjectToScene(semt, yeni);
            EditorSceneManager.SaveScene(yeni, hedef);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TerrainScene);
            Debug.Log($"[Hezarfen] {districtId}: {mahalle} mahalle, {ev} ev "
                      + $"(tohum {seed}) -> {hedef}");
        }

        /// <summary>
        /// Semti doldurur. Sahneyi ne açar ne kaydeder — ölçüm ve test için.
        /// Döndürdüğü sayı kurulan mahalle adedidir.
        /// </summary>
        public static int Fill(string districtId, Terrain terrain, int seed,
                               out int toplamEv)
        {
            var def = FindDef(districtId);
            if (def == null)
                throw new Exception($"{districtId}: DistrictDef bulunamadi. "
                                    + "Once 'GIS/Semtleri ice aktar'.");
            if (def.boundary == null || def.boundary.Length < 3)
                throw new Exception($"{districtId}: sinir poligonu yok.");

            string rootName = RootNameOf(districtId);
            var old = GameObject.Find(rootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject(rootName);

            var tag = root.AddComponent<HistoricalTag>();
            tag.tier = HistoricalTier.Reconstruction;   // T2
            tag.sourceNote =
                "Semt dokusu: mahalle cekirdekleri araziden turetildi (kot, "
                + "egim, landmark uzakligi), sokak kurallari RESEARCH.md 4.1. "
                + "Mahallelerin SAYISI ve YERI taslak (T2); kaynaklarda "
                + "1632 mahalle sinirlari yoktur.";

            var landmarks = LandmarkPositions();
            var noktalar = QuarterOrigins(def, terrain, seed, landmarks);

            // Nadir kurumlari mahallelere DAGIT. Hangi mahalleye dustugu
            // tohumdan turer; sayisi semtin butcesinden gelir.
            int n = noktalar.Count;
            var kilise = Dagit(n, def.churchCount, seed ^ 0x11);
            var hamam = Dagit(n, def.hamamCount, seed ^ 0x22);
            var medrese = Dagit(n, def.medreseCount, seed ^ 0x33);
            var han = Dagit(n, def.hasHan ? def.hanCount : 0, seed ^ 0x44);
            var firin = Dagit(n, def.firinCount, seed ^ 0x55);
            var kahve = Dagit(n, def.kahvehaneCount, seed ^ 0x66);
            var boza = Dagit(n, def.bozahaneCount, seed ^ 0x77);
            var sebil = Dagit(n, def.sebilCount, seed ^ 0x88);

            OttomanStreetBuilder.ResetQuarterState();
            toplamEv = 0;
            int kurulan = 0;
            for (int i = 0; i < noktalar.Count; i++)
            {
                var (nokta, yon) = noktalar[i];
                var q = SpecFor(def, i, nokta, yon);
                q.HasChurch = kilise.Contains(i) && q.ChurchPrefabs.Length > 0;
                q.HasHamam = hamam.Contains(i);
                q.HasMedrese = medrese.Contains(i);
                q.HasHan = han.Contains(i);
                q.HasFirin = firin.Contains(i);
                q.HasKahvehane = kahve.Contains(i);
                q.HasBozahane = boza.Contains(i);
                q.HasSebil = sebil.Contains(i);

                var qGo = new GameObject($"Mahalle_{i:00}");
                qGo.transform.SetParent(root.transform, false);
                try
                {
                    // Her mahalle KENDI tohumunu alir ve o tohum semtin
                    // tohumuyla sirasindan turer: mahalle 7 her zaman ayni
                    // mahalle 7'dir, komsulari degisse bile.
                    toplamEv += OttomanStreetBuilder.BuildInto(
                        qGo.transform, terrain, q, seed * 397 + i);
                    kurulan++;
                }
                catch (Exception e)
                {
                    UnityEngine.Object.DestroyImmediate(qGo);
                    Debug.LogWarning($"[Hezarfen] {districtId} mahalle {i} "
                                     + $"kurulamadi: {e.Message}");
                }
            }
            return kurulan;
        }

        // ------------------------------------------------------------ konumlar

        /// <summary>
        /// Mahalle çekirdeklerini üretir: (konum, sokak yönü).
        ///
        /// Izgara adımı <c>quarterSpacingMeters</c>'dır ve her hücrenin
        /// sarsıntısı kendi indisinden türer — böylece bir hücre komşusundan
        /// bağımsız olarak aynı yeri verir.
        /// </summary>
        public static List<(Vector2 nokta, Vector2 yon)> QuarterOrigins(
            DistrictDef def, Terrain terrain, int seed, List<Vector2> landmarks)
        {
            var cikti = new List<(Vector2, Vector2)>();
            if (def.settlementDensity <= 0.001f) return cikti;

            float adim = Mathf.Max(60f, def.quarterSpacingMeters
                                        / Mathf.Max(0.2f, def.settlementDensity));

            Vector2 min = def.boundary[0], max = def.boundary[0];
            foreach (var v in def.boundary)
            {
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }

            int nx = Mathf.CeilToInt((max.x - min.x) / adim);
            int nz = Mathf.CeilToInt((max.y - min.y) / adim);
            for (int gz = 0; gz <= nz; gz++)
            {
                for (int gx = 0; gx <= nx; gx++)
                {
                    // Hucre tohumu: semt tohumu + hucre indisi. Tarama sirasi
                    // degisse bile ayni hucre ayni sarsintiyi alir.
                    var rng = new System.Random(seed ^ (gx * 73856093) ^ (gz * 19349663));
                    float jx = (float)(rng.NextDouble() - 0.5) * adim * 0.6f;
                    float jz = (float)(rng.NextDouble() - 0.5) * adim * 0.6f;
                    var p = new Vector2(min.x + gx * adim + jx,
                                        min.y + gz * adim + jz);

                    if (!DistrictDef.ContainsXZ(def.boundary, p.x, p.y)) continue;
                    if (!Buildable(terrain, def, p)) continue;
                    if (TooCloseToLandmark(landmarks, p, def.landmarkClearanceMeters))
                        continue;

                    cikti.Add((p, StreetDirection(terrain, p, rng)));
                }
            }
            return cikti;
        }

        /// <summary>Arazi bu noktada mahalle kaldırır mı?</summary>
        static bool Buildable(Terrain terrain, DistrictDef def, Vector2 p)
        {
            var td = terrain.terrainData;
            Vector3 o = terrain.transform.position;
            float u = (p.x - o.x) / td.size.x;
            float v = (p.y - o.z) / td.size.z;
            if (u < 0.01f || u > 0.99f || v < 0.01f || v > 0.99f) return false;

            float h = terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + o.y;
            if (h < def.minElevationMeters) return false;

            // Egim: Unity'nin kendi ornekleyicisi. Elle gradyan hesaplamak
            // ayni sayiyi ikinci kez, farkli yuvarlamayla uretmek olurdu.
            float egim = td.GetSteepness(u, v);
            return egim <= def.maxSlopeDegrees;
        }

        static bool TooCloseToLandmark(List<Vector2> landmarks, Vector2 p, float r)
        {
            float r2 = r * r;
            foreach (var l in landmarks)
                if ((l - p).sqrMagnitude < r2) return true;
            return false;
        }

        /// <summary>
        /// Sokağın başlangıç yönü — <b>eğim gradyanına dik</b>, yani eş
        /// yükselti boyunca. Düz zeminde gradyan yön bildirmez; orada hücre
        /// tohumundan bir açı gelir.
        /// </summary>
        static Vector2 StreetDirection(Terrain terrain, Vector2 p, System.Random rng)
        {
            const float d = 8f;
            float hx = terrain.SampleHeight(new Vector3(p.x + d, 0f, p.y))
                     - terrain.SampleHeight(new Vector3(p.x - d, 0f, p.y));
            float hz = terrain.SampleHeight(new Vector3(p.x, 0f, p.y + d))
                     - terrain.SampleHeight(new Vector3(p.x, 0f, p.y - d));
            var grad = new Vector2(hx, hz);
            if (grad.magnitude < 0.25f)                 // duz zemin
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
            var dik = new Vector2(-grad.y, grad.x).normalized;
            return dik;
        }

        // -------------------------------------------------------------- karakter

        /// <summary>
        /// Semt karakterinden mahalle tarifi üretir.
        ///
        /// Karakterin tek kaynağı <see cref="DistrictDef"/>tir; burada hiçbir
        /// palet ya da çekirdek tipi <b>karar verilmez</b>, yalnızca aktarılır.
        /// İki yerde tutulan bir gerçek bir gün ayrışır.
        /// </summary>
        static OttomanStreetBuilder.QuarterSpec SpecFor(
            DistrictDef def, int index, Vector2 nokta, Vector2 yon)
        {
            return new OttomanStreetBuilder.QuarterSpec
            {
                Name = $"{def.districtId}_M{index:00}",
                RootName = $"Mahalle_{index:00}",
                ScenePath = "",                 // BuildInto sahne kaydetmez
                Origin = nokta,
                Direction = yon,
                HousePalette = def.housePalette,
                CoreKind = def.coreKind,
                ChurchPrefabs = def.churchPrefabs ?? new string[0],
                // Han ticarî semte aittir ve semtte BIR tanedir: her mahalleye
                // han koymak semti hanlar caddesine cevirirdi.
                // Nadir yapi bayraklari burada DEGIL, `Fill` icinde semt
                // butcesinden dagitilir; burada varsayilan kapalidir ki
                // dagitim unutulursa fazla degil EKSIK uretsin.
                HasHan = false,
                HasChurch = false,
                HasHamam = false,
                HasMedrese = false,
                HasFirin = false,
                HasKahvehane = false,
                HasBozahane = false,
                HasSebil = false,
            };
        }


        /// <summary>
        /// <c>adet</c> tanesini <c>n</c> mahalleye deterministik dağıtır.
        ///
        /// Fisher-Yates'in ilk <c>adet</c> adımı: sıralı bir indis dizisi
        /// tohumla karıştırılır ve baştan alınır. Rastgele indis seçip
        /// tekrarları elemekten farkı, <b>adet ≥ n</b> olduğunda da doğru
        /// davranması — o durumda hepsi seçilir, sonsuz döngü olmaz.
        /// </summary>
        static HashSet<int> Dagit(int n, int adet, int seed)
        {
            var küme = new HashSet<int>();
            if (n <= 0 || adet <= 0) return küme;
            var idx = new int[n];
            for (int i = 0; i < n; i++) idx[i] = i;
            var rng = new System.Random(seed);
            for (int i = 0; i < n - 1; i++)
            {
                int j = i + rng.Next(n - i);
                (idx[i], idx[j]) = (idx[j], idx[i]);
            }
            for (int i = 0; i < Mathf.Min(adet, n); i++) küme.Add(idx[i]);
            return küme;
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

        static DistrictDef FindDef(string districtId)
        {
            var reg = AssetDatabase.LoadAssetAtPath<DistrictRegistry>(RegistryPath);
            if (reg != null && reg.districts != null)
                foreach (var d in reg.districts)
                    if (d != null && d.districtId == districtId) return d;

            foreach (var guid in AssetDatabase.FindAssets("t:DistrictDef"))
            {
                var d = AssetDatabase.LoadAssetAtPath<DistrictDef>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (d != null && d.districtId == districtId) return d;
            }
            return null;
        }

        /// <summary>
        /// Sahnedeki landmark konumları. Mahalle onların üstüne kurulmaz.
        /// </summary>
        static List<Vector2> LandmarkPositions()
        {
            var liste = new List<Vector2>();
            var kok = GameObject.Find(LandmarkPlacer.RootName);
            if (kok == null) return liste;
            foreach (Transform t in kok.transform)
                liste.Add(new Vector2(t.position.x, t.position.z));
            return liste;
        }
    }
}
