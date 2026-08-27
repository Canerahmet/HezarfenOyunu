using System;
using System.Collections.Generic;
using System.IO;
using Hezarfen.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// Osmanlı mahalle dokusunu kurar — **ızgara değil**.
    ///
    /// Caner (2026-08-20): *"tek bir kusursuz çizgi üzerinde olması biraz
    /// şüphelendiriyor beni, sanki doğallık ve gerçekçilik bozuluyormuş gibi."*
    /// Haklıydı. Araştırma bunu doğruladı ve kuralları verdi (RESEARCH.md §4.1);
    /// bu sınıf o kuralların koddaki karşılığıdır. Her kural, uyguladığı yerde
    /// numarasıyla anılır.
    ///
    /// Uygulanan yedi kural (hepsi <b>T2</b> — nitel kaynaktan çıkarım):
    ///   1. Sokak ekseni eğridir ve yamaçta <b>eş yükselti eğrisini izler</b>.
    ///   2. Ev cephesi sokak eksenine <b>yerel olarak</b> dik durur.
    ///   3. Cephe hattında düzensizlik: geri/ileri ve açı sapması.
    ///   4. Ana sokaktan dallanan <b>çıkmazlar</b>, 3-6 eve hizmet eder.
    ///   5. Ev <b>sokak çizgisine oturur</b> (duvarıyla), bahçe arkadadır.
    ///   6. Cumba ve saçak sokağın <b>üstüne</b> taşar.
    ///   7. Köşelerde iki cepheli varyant kullanılır.
    ///
    /// Neden eş yükselti: İstanbul'un yamaçlarında sokak, yokuşu dik kesmez —
    /// yamacı yanlamasına tarar; dikleştiği yerde merdivenlenir (RESEARCH.md
    /// "dar, çıkmaz ve merdivenli sokaklar"). Rastgele eğrilik bunu vermez,
    /// **araziye bakan** bir yürüyüş verir.
    ///
    /// Üretim deterministiktir: aynı tohum = aynı mahalle. Aksi hâlde bir
    /// ölçüm ya da inceleme paketi tekrar edilemezdi.
    /// </summary>
    public static class OttomanStreetBuilder
    {
        public const string CatalogPath = "art/blend/variants/catalog.json";
        public const string StreetCatalogPath = "art/blend/street/catalog.json";
        public const string NatureCatalogPath = "art/blend/nature/catalog.json";
        public const string MahalleCatalogPath = "art/blend/mahalle/catalog.json";
        public const string MescitPrefab = "PF_Mescit_A";
        public const string SinagogPrefab = "PF_Sinagog_A";
        public const string PrefabDir = "Assets/_Project/Art/Prefabs";
        public const string ScenePath = "Assets/_Project/Scenes/Sandbox/Faz2_GalataSokagi.unity";
        public const string RootName = "MAHALLE_Galata";

        // --- doku ölçüleri (RESEARCH.md §4.1) ---

        /// <summary>Ana sokak genişliği. 1848'de ana yollar 7,6 m'ye ÇIKARILMAYA
        /// çalışıldı; öncesi bundan dardı. Fıkıhtaki alt sınır ~3,4-3,8 m
        /// ("yüklü deve"). İkisinin arası seçildi — T2.</summary>
        public const float StreetWidth = 4.6f;

        /// <summary>Çıkmaz sokak hususi yoldur, ana yoldan dardır.</summary>
        public const float AlleyWidth = 3.0f;

        [Serializable] private class Variant
        {
            public string name;
            public string prefab;
            public string why;
            public string palette;
            public string facades;
            public string kind;
            public float wall_width;
            public float wall_depth;
            public float footprint_x;
            public float height;
            public int tris_lod0;
            public int floors;
        }

        [Serializable] private class Catalog { public Variant[] variants; }

        /// <summary>
        /// Bir semtin **grameri**. Doku kuralları (ADR 0016) her semtte aynıdır;
        /// değişen şey <b>kimin oturduğu</b>dur ve bu üç şeyden okunur:
        /// çekirdek yapı, ibadet yapıları, ev paleti.
        ///
        /// Neden parametre: Galata'yı kuran kod Balat'ı da kurar — ama Balat'a
        /// mescit, Galata'ya sinagog koymak dönem hatasıdır. İkisini ayrı sınıf
        /// yazmak ise doku kurallarını ikiye böler ve zamanla ayrışırlar.
        /// </summary>
        public class QuarterSpec
        {
            public string Name;
            public string RootName;
            public string ScenePath;
            /// <summary>Dünya orijinine (Galata Kulesi) göre sokağın başlangıcı.</summary>
            public Vector2 Origin;
            public Vector2 Direction = new Vector2(1f, 0.25f);
            /// <summary>Ev paleti: "default" (Müslüman) ya da "nonmuslim".</summary>
            public string HousePalette = "default";
            /// <summary>Mahalle çekirdeği: mescit mi, avlulu sinagog mu.</summary>
            public string CoreKind = "mescit";
            /// <summary>Büyükten küçüğe; arazinin kaldırdığı ilk boy seçilir.</summary>
            public string[] ChurchPrefabs = new string[0];
            /// <summary>Han ticarî semte aittir; her mahallede bulunmaz.</summary>
            public bool HasHan = false;

            /// <summary>
            /// <b>Nadir kurumlar mahalle başına DEĞİL, semt başına sayılır.</b>
            ///
            /// Tek örnek sokak sahnesinde hamam, medrese ve kilise koşulsuz
            /// konuyordu ve bu doğruydu: o mahalle semtin tamamını temsil
            /// ediyordu. Semt gerçekten 34 mahalleye bölününce aynı kod
            /// <b>22 hamam, 22 medrese ve 22 Latin kilisesi</b> üretti —
            /// ölçüldü. Galata'da o kadar hamam yoktu; mahalle sayısı kadar
            /// medrese hiç yoktu.
            ///
            /// Kural artık şu: <b>mahalle ne söylenirse onu kurar; kaç tane
            /// olacağına semt karar verir</b> (<c>DistrictFiller</c>). Tek
            /// mahalle kuran eski menüler bayrakları açık bırakır ve
            /// davranışları değişmez.
            /// </summary>
            public bool HasChurch = true;
            public bool HasHamam = true;
            public bool HasMedrese = true;
            public bool HasFirin = true;
            public bool HasKahvehane = true;
            public bool HasBozahane = true;

            /// <summary>
            /// Sebil de <b>semt başına</b> sayılır. Vakıf kurumudur ama
            /// çeşmeden farklıdır: çeşme mahallenin suyudur ve her mahallede
            /// bulunur; sebil bir <b>hayır</b> kurumudur — birinin parasıyla
            /// kurulur, bir görevli durur ve su dağıtır. Her mahallede sebil
            /// olması, her mahallede bir hayır sahibi olması demekti.
            /// Ölçüldü: 34 mahallelik Galata'da 34 sebil çıkıyordu.
            /// </summary>
            public bool HasSebil = true;

            /// <summary>
            /// Sıbyan mektebi ve türbe **vakıf** kurumlarıdır: ikisi de bir
            /// müslüman vakfın parçasıdır ve mescitle birlikte kurulur. Balat'a
            /// sıbyan mektebi koymak dönem hatası olurdu — oradaki karşılığı
            /// cemaatin kendi okuludur (Talmud Tora), ayrı bir tiptir ve
            /// üretilmedi. Bayrak elle değil ÇEKİRDEKTEN türer; iki yerde
            /// tutulsa bir gün ayrışırdı.
            /// </summary>
            public bool HasVakif => CoreKind == "mescit";
        }

        public static QuarterSpec Galata => new QuarterSpec
        {
            Name = "Galata",
            RootName = RootName,
            ScenePath = ScenePath,
            Origin = Vector2.zero,                    // dunya orijini = Galata Kulesi
            HousePalette = "default",
            CoreKind = "mescit",
            // Galata tek cemaatli degildir: 1453 ahidnamesiyle korunan Latin
            // kiliseleri yerinde kaldi (RESEARCH.md 4.2a).
            ChurchPrefabs = new[] { "PF_Kilise_Latin_A", "PF_Kilise_Latin_B" },
            HasHan = true,          // Galata liman ve ticaret semtidir
        };

        /// <summary>
        /// Balat — Haliç'in güney kıyısı, sur içi. 17. yy'dan itibaren ağırlıklı
        /// Yahudi mahallesi, en az on bir sinagogla (RESEARCH.md §4.2c).
        /// Konum, Galata Kulesi orijinine göre yaklaşık ölçüldü (T2).
        /// </summary>
        public static QuarterSpec Balat => new QuarterSpec
        {
            Name = "Balat",
            RootName = "MAHALLE_Balat",
            ScenePath = "Assets/_Project/Scenes/Sandbox/Faz2_BalatSokagi.unity",
            Origin = new Vector2(-2102f, 429f),
            Direction = new Vector2(0.85f, -0.53f),
            HousePalette = "nonmuslim",
            CoreKind = "sinagog",
            // Rum kilisesi: kulesiz, alcak. Fener bitisiktir ve Patrikhane
            // 1601'den beri oradadir.
            ChurchPrefabs = new[] { "PF_Kilise_Rum_A", "PF_Kilise_Rum_B" },
        };

        [MenuItem("Hezarfen/GIS/Galata sokagi sahnesi kur")]
        public static void BuildGalataMenu()
        {
            Build(Galata, seed: 1632);
            Debug.Log($"[Hezarfen] Galata sokagi sahnesi: {ScenePath}");
        }

        [MenuItem("Hezarfen/GIS/Balat sokagi sahnesi kur")]
        public static void BuildBalatMenu()
        {
            var q = Balat;
            Build(q, seed: 1632);
            Debug.Log($"[Hezarfen] Balat sokagi sahnesi: {q.ScenePath}");
        }

        public static UnityEngine.SceneManagement.Scene Build(int seed) =>
            Build(Galata, seed);

        public static UnityEngine.SceneManagement.Scene Build(QuarterSpec q, int seed)
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/Faz1_Terrain.unity", OpenSceneMode.Single);

            var terrainGo = GameObject.Find("TR_Istanbul");
            var terrain = terrainGo != null ? terrainGo.GetComponent<Terrain>() : null;
            if (terrain == null) throw new Exception("TR_Istanbul yok — once GIS/Terrain uret.");

            var gis = GameObject.Find(GeoJsonImporter.RootName);
            if (gis != null) gis.SetActive(false);      // gizmo'lar goruntuyu kirletir

            var old = GameObject.Find(q.RootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject(q.RootName);
            ResetQuarterState();
            BuildInto(root.transform, terrain, q, seed);

            SettlementMask.Write(q.Name, taken);
            var kutu = SettlementBounds(taken);
            if (kutu.HasValue) TerrainCoverBuilder.RepaintSettlement(kutu.Value);
            EnsureFolder(Path.GetDirectoryName(q.ScenePath).Replace('\\', '/'));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, q.ScenePath);
            return scene;
        }

        /// <summary>
        /// Mahalle kurulumundan önce ortak durumu sıfırlar.
        ///
        /// <c>taken</c> (çakışma daireleri), <c>podiums</c> ve
        /// <c>pavingStrips</c> sınıf düzeyindedir. Tek mahalle kurarken bunu
        /// <c>Build</c> yapardı; bir semte onlarca mahalle kurulurken
        /// <b>semtin başında bir kez</b> yapılmalı, yoksa ikinci mahalle
        /// birincinin üstüne kurulur.
        /// </summary>
        public static void ResetQuarterState()
        {
            taken.Clear();
            podiums.Clear();
            pavingStrips.Clear();
        }

        /// <summary>
        /// Bir mahalleyi <b>verilen ebeveynin altına</b>, açık sahneye kurar.
        ///
        /// <c>Build</c> tek mahalle için sahneyi açar, kurar ve kaydeder —
        /// Faz 2'nin örnek sokak sahneleri böyle üretildi. Faz 4 ise bir semte
        /// ONLARCA mahalle koyar; her biri için sahne açmak öncekini silerdi.
        /// Bu yüzden kurulum sahne yönetiminden ayrıldı: <c>BuildInto</c>
        /// yalnızca geometriyi kurar, sahneyi ne açar ne kaydeder.
        ///
        /// Çakışma listesini temizlemez — bkz. <see cref="ResetQuarterState"/>.
        /// Döndürdüğü sayı yerleştirilen ev adedidir.
        /// </summary>
        public static int BuildInto(Transform root, Terrain terrain,
                                    QuarterSpec q, int seed)
        {

            var all = LoadCatalog();
            if (all.Count == 0) throw new Exception($"{CatalogPath} bos/yok.");

            // Ev paleti semte gore SUZULUR. Balat'a asi kirmizisi cumbali ev
            // koymak, gayrimuslim palet kurulmus olmasina ragmen onu hic
            // kullanmamak demekti.
            var variants = all.FindAll(v => v.palette == q.HousePalette);
            if (variants.Count == 0)
                throw new Exception($"{q.Name}: '{q.HousePalette}' paletinde ev varyanti yok.");
            if (variants.Count < 6)
                Debug.LogWarning($"[Hezarfen] {q.Name}: yalnizca {variants.Count} ev "
                                 + "varyanti — sokak tekrarli gorunecek.");

            var rng = new System.Random(seed);
            int placed = 0, skipped = 0, stepCount = 0;

            // --- KURAL 1: ana sokak, es yukselti egrisini izler ---
            var spine = TraceContour(terrain, new Vector3(q.Origin.x, 0f, q.Origin.y),
                                     q.Direction, 46, 7.5f, rng);
            // --- MAHALLE ÇEKİRDEĞİ, evlerden ÖNCE ---
            //
            // RESEARCH.md §4.1(g): mahalle mescitten dallanır. Bu yüzden mescit
            // ve çevresi (çeşme, dükkânlar) evlerden ÖNCE yerleşir ve yerlerini
            // rezerve eder. Sonra konsaydı ya evlerin arasına sıkışırdı ya da
            // ev yerleştirmeyi geriye dönük bozmak gerekirdi — mahalle merkezi
            // artık gerçekten merkez.
            var coreGo = new GameObject($"Cekirdek_{q.CoreKind}");
            coreGo.transform.SetParent(root.transform, false);
            int coreCount = PlaceCore(q, spine, terrain, coreGo.transform, rng,
                                      out float coreS);

            // Mahalle tek cemaatli degildir: ikinci cemaatin ibadet yapisi da
            // evlerden ONCE yerini alir, yoksa doku onu disari iter.
            var churchGo = new GameObject("Cekirdek_Kilise");
            churchGo.transform.SetParent(root.transform, false);
            int churchCount = !q.HasChurch ? 0 : PlaceChurch(q.ChurchPrefabs, spine, terrain,
                                          churchGo.transform, coreS);

            // Hamam her mahallenin ihtiyacidir; han TICARI cekirdege aittir
            // (Galata liman semtidir, Balat konut mahallesi).
            var civicGo = new GameObject("Cekirdek_Kamusal");
            civicGo.transform.SetParent(root.transform, false);
            int civicCount = !q.HasHamam ? 0 : PlaceBig(new[] { "PF_Hamam_A", "PF_Hamam_B" },
                                      spine, terrain, civicGo.transform, coreS,
                                      eastFacing: false);
            if (q.HasHan)
                civicCount += PlaceBig(new[] { "PF_Han_A", "PF_Han_B" },
                                       spine, terrain, civicGo.transform,
                                       coreS, eastFacing: false, nearCore: true);

            // MEKTEP VAKFIN ÖTEKİ YARISI: banî türbesini hazîreye, mektebi
            // çekirdeğin yanına kurar. İkisi ayrı yerde durmaz — mektep uzağa
            // düşerse mahalle vakfı değil, rastgele bir okul olur.
            if (q.HasVakif)
            {
                civicCount += PlaceBig(new[] { "PF_Mektep_A" }, spine, terrain,
                                       civicGo.transform, coreS,
                                       eastFacing: false, nearCore: true);
                // Medrese: vakfın en büyük yapısı. Büyükten küçüğe denenir —
                // arazi 28 m'lik avluyu kaldırmıyorsa 22 m'lik kurulur.
                if (q.HasMedrese) civicCount += PlaceBig(new[] { "PF_Medrese_A", "PF_Medrese_B" },
                                       spine, terrain, civicGo.transform, coreS,
                                       eastFacing: false, nearCore: true);
            }

            // FIRIN her mahallededir — ekmek cemaate göre değişmez.
            if (q.HasFirin) civicCount += PlaceBig(new[] { "PF_Firin_A", "PF_Firin_B" }, spine,
                                   terrain, civicGo.transform, coreS,
                                   eastFacing: false, nearCore: true);

            // KAHVEHANE ÇARŞI UCUNDA: dükkân sırasının ve hanın yanında.
            // 1632'de açıktır; Eylül 1633'ten sonra bu prefab sahneden
            // KALDIRILMALIDIR (ADR 0021 §5) — oyunun tek zaman işareti.
            if (q.HasKahvehane) civicCount += PlaceBig(new[] { "PF_Kahvehane_A", "PF_Kahvehane_B" },
                                   spine, terrain, civicGo.transform, coreS,
                                   eastFacing: false, nearCore: true);
            // Kahvehanenin CINARI: gölge, kahvehanenin ikinci odasıdır.
            civicCount += PlaceTreeBeside(civicGo.transform, "PF_Kahvehane",
                                          "Cinar", terrain, rng);

            // BOZAHANE — oyunun İKİNCİ zaman işareti, ve kahvehaneyle aynı
            // raftadır. 1638 esnaf sayımında İstanbul'da **300 bozahane** var
            // (RESEARCH.md §4.7c): bu bir külliye yapısı değil, mahalle
            // dükkânıdır ve çarşı ucunda, kahvehanenin yanında durur.
            //
            // Faz 2b'nin öteki yeni yapıları buraya GİRMEZ ve sebepleri ayrı:
            // muvakkithane selâtin camisine aittir (mahalle mescidine değil,
            // ADR 0030 §2); imaret bir külliyenin mutfağıdır; arasta bir
            // sokak tipolojisidir, tek prefab olarak mahalleye tıkılmaz; su
            // değirmeni dere, su terazisi ise Kırkçeşme hattı ister — ikisi
            // de GIS işi. Elde var diye koymak, her birinin kendi tezini
            // bozardı.
            if (q.HasBozahane) civicCount += PlaceBig(new[] { "PF_Bozahane_A" }, spine, terrain,
                                   civicGo.transform, coreS,
                                   eastFacing: false, nearCore: true);

            var mainGo = new GameObject("Sokak_Ana");
            mainGo.transform.SetParent(root.transform, false);
            placed += PlaceAlong(spine, StreetWidth, variants, terrain,
                                 mainGo.transform, rng, ref skipped, corners: true);
            stepCount += AddPaving(spine, StreetWidth, terrain);

            // --- KURAL 4: cikmazlar ana sokaktan dallanir ---
            int alleyCount = 0;
            for (int i = 6; i < spine.Count - 6; i += 9 + rng.Next(4))
            {
                Vector2 t = Tangent(spine, i);
                Vector2 n = new Vector2(-t.y, t.x) * (rng.Next(2) == 0 ? 1f : -1f);
                Vector3 start = spine[i] + new Vector3(n.x, 0f, n.y) * (StreetWidth * 0.5f + 6f);
                // 3-6 eve hizmet eden kisa kol: 4-7 adim yeter.
                var alley = TraceContour(terrain, start, n, 4 + rng.Next(4), 7.0f, rng);
                if (alley.Count < 3) continue;
                var go = new GameObject($"Cikmaz_{alleyCount:00}");
                go.transform.SetParent(root.transform, false);
                placed += PlaceAlong(alley, AlleyWidth, variants, terrain,
                                     go.transform, rng, ref skipped, corners: false);
                stepCount += AddPaving(alley, AlleyWidth, terrain);
                alleyCount++;
            }

            // Butun kaideler tek mesh: 100+ ayri nesne cizim cagrisi ekler,
            // hicbir sey kazandirmaz (kaideler hareket etmez).
            int podiumCount = podiums.Count;
            BuildPodiums(root.transform, "Kaideler", q.Name);
            BuildPaving(root.transform, "Kaldirim", q.Name);

            // ZEMİN: mahallenin bastığı yer otlak değil, ÇİĞNENMİŞ TOPRAK.
            //
            // Caner (Karar 12): mahalle çayırın üstünde duruyordu. Kaldırım
            // düzeldi ama kaldırım dışındaki zemin — hazire tabanı, avlu
            // çevresi, dükkân önü — arazi örtüsünün çimeniydi.
            //
            // Maske bir SINIR iddiası değil (ADR 0024 onu haklı olarak
            // reddetmişti): kaynağı, sahneye az önce koyduğumuz yapıların
            // kendisi. `taken` zaten hepsinin merkezini ve yarıçapını
            // tutuyor — yerleşimin nerede olduğunu ondan iyi bilen yok.
            //
            // Burada çağrılıyor çünkü zemin ve üstündeki yapı AYNI TURDA
            // güncellenmezse biri unutulur; iki ayrı menü komutu, bir gün
            // ayrışan iki gerçek demektir.
            var tag = root.GetComponent<HistoricalTag>()
                      ?? root.gameObject.AddComponent<HistoricalTag>();
            tag.tier = HistoricalTier.Reconstruction;   // T2
            tag.sourceNote = "Sokak dokusu RESEARCH.md 4.1'den cikarim (T2): organik eksen, "
                           + "es yukselti takibi, cikmaz kollar, duvar sokak cizgisinde. "
                           + "Konum ve ev-ev yerlesim TASLAK.";

            Debug.Log($"[Hezarfen] {q.Name}: {placed} ev ({variants.Count} varyant, "
                      + $"{q.HousePalette}), {coreCount} cekirdek yapisi "
                      + $"({q.CoreKind}/cesme/dukkan), {churchCount} kilise, {civicCount} hamam/han, {alleyCount} cikmaz, {stepCount} basamak, "
                      + $"{podiumCount} tas kaide, {skipped} yerlesim elendi "
                      + $"(su/cakisma). Ana sokak {spine.Count} dugum, tohum {seed}.");
            return placed;
        }

        // ------------------------------------------------------- sokak ekseni

        /// <summary>
        /// KURAL 1 — eş yükselti eğrisini izleyen bir eksen üretir.
        ///
        /// Her adımda arazinin eğimi ölçülür ve ona **dik** yön alınır; bu,
        /// yüksekliği koruyan yöndür. Gürültü eklenir ki eksen matematiksel
        /// bir eğri gibi durmasın. Düz arazide eğim ~0'dır ve yön yalnızca
        /// gürültüyle sapar — orada da doğru davranış budur: düzlükte sokak
        /// serbesttir.
        /// </summary>
        private static List<Vector3> TraceContour(Terrain terrain, Vector3 start,
                                                  Vector2 dir, int steps, float stepLen,
                                                  System.Random rng)
        {
            var pts = new List<Vector3>();
            Vector2 p = new Vector2(start.x, start.z);
            Vector2 d = dir.normalized;

            for (int i = 0; i < steps; i++)
            {
                float y = Height(terrain, p);
                if (y < 3f) break;                       // denize girmez
                pts.Add(new Vector3(p.x, y, p.y));

                Vector2 g = Gradient(terrain, p);
                if (g.sqrMagnitude > 1e-6f)
                {
                    // Egime dik iki yon var; mevcut gidise yakin olani secilir,
                    // yoksa sokak her adimda geri doner.
                    Vector2 c = new Vector2(-g.y, g.x).normalized;
                    if (Vector2.Dot(c, d) < 0f) c = -c;
                    d = Vector2.Lerp(d, c, 0.55f);
                }
                // Gurultu: -12..+12 derece. Duzlukte tek sapma kaynagi budur.
                float jitter = ((float)rng.NextDouble() - 0.5f) * 24f * Mathf.Deg2Rad;
                d = Rotate(d, jitter).normalized;
                p += d * stepLen;
            }
            return pts;
        }

        private static float Height(Terrain t, Vector2 p) =>
            t.SampleHeight(new Vector3(p.x, 0f, p.y)) + t.transform.position.y;

        private static Vector2 Gradient(Terrain t, Vector2 p, float h = 6f)
        {
            float dx = Height(t, p + Vector2.right * h) - Height(t, p - Vector2.right * h);
            float dz = Height(t, p + Vector2.up * h) - Height(t, p - Vector2.up * h);
            return new Vector2(dx, dz) / (2f * h);
        }

        private static Vector2 Rotate(Vector2 v, float rad)
        {
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        private static Vector2 Tangent(List<Vector3> pts, int i)
        {
            int a = Mathf.Max(0, i - 1), b = Mathf.Min(pts.Count - 1, i + 1);
            var d = new Vector2(pts[b].x - pts[a].x, pts[b].z - pts[a].z);
            return d.sqrMagnitude > 1e-6f ? d.normalized : Vector2.right;
        }

        // ---------------------------------------------------------- yerleşim

        /// <summary>
        /// Eksenin iki yanına ev dizer.
        ///
        /// KURAL 5 — konum, evin <b>duvarı</b> sokak çizgisine gelecek şekilde
        /// hesaplanır: pivot taban merkezdedir ve ön duvar yerel +Z'de
        /// <c>wall_depth/2</c> uzaktadır. Ayak izini (saçak dahil) kullanmak
        /// evleri yarım metre geri iter ve doku gevşerdi.
        ///
        /// KURAL 6 — saçak ve cumba bu yüzden sokağın üstüne taşar; istenen budur.
        /// </summary>
        private static int PlaceAlong(List<Vector3> spine, float streetWidth,
                                      List<Variant> variants, Terrain terrain,
                                      Transform parent, System.Random rng,
                                      ref int skipped, bool corners)
        {
            if (spine.Count < 2) return 0;

            int placed = 0;
            foreach (int side in new[] { 1, -1 })
            {
                float s = 1.5f;                            // eksen boyunca yol alinan mesafe
                float total = PolylineLength(spine);

                while (s < total - 2f)
                {
                    // KURAL 7: kose evleri yalnizca ana sokakta ve uclara yakin.
                    bool wantCorner = corners && (s < 12f || s > total - 14f);
                    var v = Pick(variants, rng, wantCorner);

                    SampleAt(spine, s, out Vector3 pos, out Vector2 tan);
                    Vector2 nrm = new Vector2(-tan.y, tan.x) * side;

                    // KURAL 3: cephe hattinda duzensizlik.
                    float setback = (float)rng.NextDouble() * 0.5f - 0.1f;   // -0,10 .. +0,40 m
                    float yaw = ((float)rng.NextDouble() - 0.5f) * 12f;      // ±6 derece

                    float off = streetWidth * 0.5f + v.wall_depth * 0.5f + setback;
                    Vector2 c = new Vector2(pos.x, pos.z) + nrm * off;

                    // KURAL 8 (ölçümden doğdu) — ev, ayak izinin EN YÜKSEK
                    // köşesine oturur ve altındaki boşluk taş kaideyle dolar.
                    //
                    // Ölçüldü: mahalle arazisinin eğimi medyan %14, p90 %29 —
                    // ve bu DEM gürültüsü değil (4 m ve 20 m adımda aynı çıktı).
                    // Sokak eş yükseltiyi izlediği için evler ona DİK, yani
                    // yamacın en dik yönüne oturur; 8 m'lik bir ayak izi altında
                    // 1-2,5 m kot farkı olağandır. İlk denemede 108 evin 89'u
                    // hem havada hem gömülü çıktı.
                    //
                    // Ortalama kota oturtmak yarı gömer; en alçağa oturtmak
                    // havada bırakır. En yükseğe oturtup altını doldurmak,
                    // yamaç evinin gerçekte yapıldığı şeydir: taş istinat/
                    // subasman duvarı (RESEARCH.md §4.1 — merdivenli sokaklar).
                    FootprintHeights(terrain, c, v, out float loH, out float hiH);
                    float y = hiH;
                    float radius = Mathf.Max(v.footprint_x, v.wall_depth) * 0.5f;
                    if (loH < 3f || Overlaps(taken, c, radius * 0.72f))
                    {
                        skipped++;
                        s += v.wall_width * 0.6f + 1f;
                        continue;
                    }

                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{v.prefab}.prefab"),
                        parent);
                    if (inst == null) { skipped++; s += 6f; continue; }

                    inst.transform.position = new Vector3(c.x, y, c.y);
                    // KURAL 2: cephe eksene YEREL olarak dik. Evin onu +Z (CLAUDE.md),
                    // sokaga bakmasi icin -normal yonune cevrilir.
                    var look = new Vector3(-nrm.x, 0f, -nrm.y);
                    inst.transform.rotation = Quaternion.LookRotation(look, Vector3.up)
                                            * Quaternion.Euler(0f, yaw, 0f);

                    taken.Add((c, radius * 0.72f));
                    // Kaide: evin tabanindan en alcak kose ALTINA kadar.
                    if (y - loH > 0.05f)
                        podiums.Add(new Podium
                        {
                            center = c,
                            top = y,
                            bottom = loH - 0.5f,          // araziye gomulsun, kenar acikta kalmasin
                            width = v.wall_width + 0.16f,
                            depth = v.wall_depth + 0.16f,
                            yawDeg = inst.transform.rotation.eulerAngles.y,
                        });
                    placed++;

                    // Bitisik nizam: aralik kucuk. Arada bahce duvari/gecit olsun
                    // diye ara sira daha buyuk bosluk birakilir.
                    float gap = rng.Next(6) == 0 ? 2.5f + (float)rng.NextDouble() * 2.5f
                                                 : 0.25f + (float)rng.NextDouble() * 0.7f;
                    s += v.wall_width + gap;
                }
            }
            return placed;
        }

        // ------------------------------------------------------- taş kaideler

        private struct Podium
        {
            public Vector2 center;
            public float top, bottom, width, depth, yawDeg;
        }

        private static readonly List<Podium> podiums = new List<Podium>();

        /// <summary>Ayak izinin dört köşesinde arazi kotu — en alçak ve en yüksek.</summary>
        private static void FootprintHeights(Terrain t, Vector2 c, Variant v,
                                             out float lo, out float hi)
        {
            lo = float.MaxValue; hi = float.MinValue;
            float hw = v.wall_width * 0.5f, hd = v.wall_depth * 0.5f;
            for (int i = -1; i <= 1; i += 2)
                for (int j = -1; j <= 1; j += 2)
                {
                    float h = Height(t, c + new Vector2(i * hw, j * hd));
                    lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                }
        }

        /// <summary>
        /// Bütün kaideleri TEK mesh'te birleştirir.
        ///
        /// Ayrı ayrı nesne yapmak 100+ çizim çağrısı daha ekler ve hiçbir şey
        /// kazandırmaz — kaideler hareket etmez. UV **dünya ölçeğinde** üretilir
        /// (metre / 2,0 m), yani taş dokusu evlerdekiyle aynı yoğunlukta okunur;
        /// ölçeklenmiş bir küp kullanmak tam da bunu bozardı (ADR 0012 §5).
        /// </summary>
        /// <summary>Kaldırım için biriken şeritler — hepsi tek mesh olur.</summary>
        private static readonly List<Vector3[]> pavingStrips = new List<Vector3[]>();

        /// <summary>
        /// Sokak yüzeyi: **kaldırım şeridi** + kenar bordürü, dikleşince
        /// kendiliğinden **merdivenlenir**.
        ///
        /// Neden gerekli: mahalle kuruldu, evler, cami, çeşme, ağaç yerleşti —
        /// ama yaya hâlâ çıplak arazi üstünde yürüyordu. RESEARCH.md §4 sokağı
        /// *"dar, çıkmaz ve **merdivenli** sokaklar"* diye anar; merdiven bir
        /// süs değil, eğimin zorunlu sonucudur.
        ///
        /// Yüzey araziyi birebir izlemez, **basamaklara yuvarlanır**: yürünen
        /// yüzey düz olmak zorundadır. Kot farkı bir rıht (0,17 m) biriktiğinde
        /// bir basamak atılır; birikmediği sürece şerit yataydır. Böylece
        /// merdiven "eklenmez", eğimden **doğar** — düz yerde hiç çıkmaz.
        ///
        /// Enine kot arazinin o kesitteki EN YÜKSEK noktasından alınır ve altta
        /// kalan boşluk bordürle kapatılır; evlerin taş kaidesiyle aynı mantık
        /// (Kural 8). Ortalama kota oturtmak kaldırımı yarı gömerdi.
        /// </summary>
        private static int AddPaving(List<Vector3> spine, float width, Terrain terrain)
        {
            if (spine.Count < 2) return 0;
            const float ds = 1.3f;                    // ornekleme adimi
            const float riser = 0.17f;                // bir rihtin yuksekligi
            const float lift = 0.05f;                 // araziden yukari pay

            float total = PolylineLength(spine);
            int n = Mathf.Max(2, Mathf.FloorToInt(total / ds));
            var left = new Vector3[n];
            var right = new Vector3[n];
            var ground = new float[n];
            int steps = 0;

            for (int i = 0; i < n; i++)
            {
                float s = total * i / (n - 1);
                SampleAt(spine, s, out Vector3 pos, out Vector2 tan);
                Vector2 nrm = new Vector2(-tan.y, tan.x);
                Vector2 c = new Vector2(pos.x, pos.z);
                Vector2 a = c - nrm * width * 0.5f, b = c + nrm * width * 0.5f;

                // Kesitin EN YUKSEK noktasi: kaldirim gomulmesin.
                ground[i] = Mathf.Max(Height(terrain, a),
                                      Mathf.Max(Height(terrain, c), Height(terrain, b)));
                left[i] = new Vector3(a.x, 0f, a.y);
                right[i] = new Vector3(b.x, 0f, b.y);
            }

            // Yurunen yuzey: basamaklara yuvarlanmis kot.
            var walk = new float[n];
            float cur = ground[0] + lift;
            for (int i = 0; i < n; i++)
            {
                float target = ground[i] + lift;
                float d = target - cur;
                if (Mathf.Abs(d) >= riser)
                {
                    cur += riser * Mathf.Round(d / riser);
                    steps++;
                }
                walk[i] = cur;
            }

            for (int i = 0; i < n; i++)
            {
                left[i].y = walk[i];
                right[i].y = walk[i];
            }
            pavingStrips.Add(new[] { new Vector3(width, ds, 0f) });   // basligi
            for (int i = 0; i < n; i++)
                pavingStrips.Add(new[] { left[i], right[i], new Vector3(0f, ground[i], 0f) });
            return steps;
        }

        /// <summary>
        /// Üretilen mesh'in varlık yolu — <b>semte göre</b>.
        ///
        /// İlk hâli semtten bağımsızdı (`SM_Kaldirim.asset`) ve iki mahalle
        /// aynı dosyayı paylaşıyordu: Balat kurulunca Galata'nın kaldırımı ve
        /// taş kaideleri <b>siliniyor</b>, yerine 2 km ötedeki Balat'ın
        /// geometrisi geçiyordu. Ölçüldü: Galata sahnesindeki `SM_Kaldirim`'in
        /// merkezi x = −1976 idi, yani Galata sokağı kaldırımsız ve kaidesiz
        /// kalmıştı. Sahne bozulmuş görünmüyordu — eksik olan şey sessizce
        /// başka bir yerdeydi.
        /// </summary>
        private static string MeshAssetPath(string name, string quarter)
        {
            string dir = "Assets/_Project/Art/Models/Generated";
            EnsureFolder(dir);
            return $"{dir}/SM_{name}_{quarter}.asset";
        }

        private static void BuildPaving(Transform parent, string name, string quarter)
        {
            if (pavingStrips.Count == 0) return;
            const float TexMeters = 2.4f;             // cobblestone_floor_001 gercek olcusu

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            int i0 = 0;
            while (i0 < pavingStrips.Count)
            {
                float width = pavingStrips[i0][0].x;
                i0++;
                int start = i0;
                while (i0 < pavingStrips.Count && pavingStrips[i0].Length == 3) i0++;
                int count = i0 - start;
                if (count < 2) continue;

                float run = 0f;
                for (int i = start; i < i0 - 1; i++)
                {
                    Vector3 l0 = pavingStrips[i][0], r0 = pavingStrips[i][1];
                    Vector3 l1 = pavingStrips[i + 1][0], r1 = pavingStrips[i + 1][1];
                    float seg = Vector3.Distance(new Vector3(l0.x, 0, l0.z),
                                                 new Vector3(l1.x, 0, l1.z));

                    // Kot degistiyse once DUSEY RIHT, sonra yatay basamak.
                    if (Mathf.Abs(l1.y - l0.y) > 1e-3f)
                    {
                        var lm = new Vector3(l1.x, l0.y, l1.z);
                        var rm = new Vector3(r1.x, r0.y, r1.z);
                        Strip(verts, uvs, tris, l0, r0, lm, rm, run, seg, width, TexMeters);
                        Strip(verts, uvs, tris, lm, rm, l1, r1, run + seg, 0f, width, TexMeters);
                    }
                    else
                    {
                        Strip(verts, uvs, tris, l0, r0, l1, r1, run, seg, width, TexMeters);
                    }

                    // Bordur: kenardan arazinin altina inen dusey serit.
                    float g0 = pavingStrips[i][2].y, g1 = pavingStrips[i + 1][2].y;
                    Kerb(verts, uvs, tris, l0, l1, g0, g1, run, seg, TexMeters, true);
                    Kerb(verts, uvs, tris, r0, r1, g0, g1, run, seg, TexMeters, false);
                    run += seg;
                }
            }

            var mesh = new Mesh { name = $"SM_{name}" };
            mesh.indexFormat = verts.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, MeshAssetPath(name, quarter));

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_Project/Art/Materials/Ottoman/M_Paving_Kaldirim.mat");
            go.AddComponent<MeshCollider>().sharedMesh = mesh;

            var tag = go.AddComponent<HistoricalTag>();
            tag.tier = HistoricalTier.Reconstruction;
            tag.sourceNote = "Sokak kaldirimi (T2): duzensiz tas, egimde merdivenli. "
                           + "RESEARCH.md 4 'dar, cikmaz ve merdivenli sokaklar'.";
            pavingStrips.Clear();
        }

        /// <summary>
        /// İki kesit arasına dörtgen — UV dünya ölçekli (u boyunca, v enine).
        ///
        /// ## Sarım yönü: kaldırım TERSTİ
        ///
        /// Ölçüldü: <c>SM_Kaldirim</c>'in 698 yatay üçgeninin <b>697'si aşağı</b>
        /// bakıyordu (kaide mesh'inde 166 yukarı, 0 aşağı — o doğruydu). Yani
        /// yürünen yüzey ters yüzdü ve üç sonucu birden vardı:
        ///   * üstten bakınca ışıksız/siyah okunuyordu (inceleme karesinde
        ///     sokağın ortasından geçen o bant),
        ///   * ışın sorguları arka yüzü <b>görmez</b> (Unity'nin varsayılanı),
        ///     yani çarpıcı fiilen yoktu — oyuncu kaldırımdan düşerdi,
        ///   * sokak çimen görünüyordu, çünkü görünen tek yüzey araziydi.
        ///
        /// Kusur ADR 0016 turundan beri duruyordu ve hiçbir kare onu
        /// göstermedi: kareler hep kaldırımın ALTINDAN alınmıştı ve oradan
        /// bakınca yüzey doğru görünüyor.
        ///
        /// Sarım artık (0,1,2)+(0,2,3); basamak rıhtı da aynı düzeltmeyle
        /// yürüyene döner. Bordür (<see cref="Kerb"/>) ölçüldü ve zaten
        /// doğruydu — ona dokunulmadı.
        /// </summary>
        private static void Strip(List<Vector3> v, List<Vector2> uv, List<int> tri,
                                  Vector3 l0, Vector3 r0, Vector3 l1, Vector3 r1,
                                  float run, float seg, float width, float tex)
        {
            int b = v.Count;
            v.Add(l0); v.Add(r0); v.Add(r1); v.Add(l1);
            uv.Add(new Vector2(run / tex, 0f));
            uv.Add(new Vector2(run / tex, width / tex));
            uv.Add(new Vector2((run + seg) / tex, width / tex));
            uv.Add(new Vector2((run + seg) / tex, 0f));
            tri.Add(b); tri.Add(b + 1); tri.Add(b + 2);
            tri.Add(b); tri.Add(b + 2); tri.Add(b + 3);
        }

        private static void Kerb(List<Vector3> v, List<Vector2> uv, List<int> tri,
                                 Vector3 e0, Vector3 e1, float g0, float g1,
                                 float run, float seg, float tex, bool flip)
        {
            float d0 = Mathf.Min(g0, e0.y) - 0.45f;
            float d1 = Mathf.Min(g1, e1.y) - 0.45f;
            var b0 = new Vector3(e0.x, d0, e0.z);
            var b1 = new Vector3(e1.x, d1, e1.z);
            int b = v.Count;
            if (flip) { v.Add(e0); v.Add(b0); v.Add(b1); v.Add(e1); }
            else { v.Add(b0); v.Add(e0); v.Add(e1); v.Add(b1); }
            uv.Add(new Vector2(run / tex, 0f));
            uv.Add(new Vector2(run / tex, (e0.y - d0) / tex));
            uv.Add(new Vector2((run + seg) / tex, (e1.y - d1) / tex));
            uv.Add(new Vector2((run + seg) / tex, 0f));
            tri.Add(b); tri.Add(b + 2); tri.Add(b + 1);
            tri.Add(b); tri.Add(b + 3); tri.Add(b + 2);
        }

        private static void BuildPodiums(Transform parent, string name, string quarter)
        {
            if (podiums.Count == 0) return;

            const float TexMeters = 2.0f;               // old_stone_wall gercek olcusu
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            foreach (var p in podiums)
            {
                float h = p.top - p.bottom;
                if (h <= 0.01f) continue;
                var rot = Quaternion.Euler(0f, p.yawDeg, 0f);
                var org = new Vector3(p.center.x, p.bottom, p.center.y);
                float hw = p.width * 0.5f, hd = p.depth * 0.5f;

                // Dort yan yuz + ust. Alt yuz hic gorunmez, uretilmez.
                AddQuad(verts, uvs, tris, org, rot,
                        new Vector3(-hw, 0, -hd), new Vector3(hw, 0, -hd), h, TexMeters);
                AddQuad(verts, uvs, tris, org, rot,
                        new Vector3(hw, 0, hd), new Vector3(-hw, 0, hd), h, TexMeters);
                AddQuad(verts, uvs, tris, org, rot,
                        new Vector3(hw, 0, -hd), new Vector3(hw, 0, hd), h, TexMeters);
                AddQuad(verts, uvs, tris, org, rot,
                        new Vector3(-hw, 0, hd), new Vector3(-hw, 0, -hd), h, TexMeters);
                AddTop(verts, uvs, tris, org, rot, hw, hd, h, TexMeters);
            }

            var mesh = new Mesh { name = $"SM_{name}" };
            mesh.indexFormat = verts.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, MeshAssetPath(name, quarter));

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_Project/Art/Materials/Ottoman/M_Stone_Rubble.mat");
            go.AddComponent<MeshCollider>().sharedMesh = mesh;   // statik, convex degil

            var tag = go.AddComponent<HistoricalTag>();
            tag.tier = HistoricalTier.Reconstruction;
            tag.sourceNote = "Yamac evinin tas istinat/subasman kaidesi (T2). "
                           + "Olculdu: mahalle egimi medyan %14, p90 %29.";
            podiums.Clear();
        }

        private static void AddQuad(List<Vector3> v, List<Vector2> uv, List<int> tri,
                                    Vector3 org, Quaternion rot, Vector3 a, Vector3 b,
                                    float h, float tex)
        {
            int i0 = v.Count;
            float len = Vector3.Distance(a, b);
            // UV yatayda kenar boyunca gercek metre, dikeyde yukseklik.
            v.Add(org + rot * a);
            v.Add(org + rot * b);
            v.Add(org + rot * (b + Vector3.up * h));
            v.Add(org + rot * (a + Vector3.up * h));
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(len / tex, 0f));
            uv.Add(new Vector2(len / tex, h / tex));
            uv.Add(new Vector2(0f, h / tex));
            tri.AddRange(new[] { i0, i0 + 2, i0 + 1, i0, i0 + 3, i0 + 2 });
        }

        private static void AddTop(List<Vector3> v, List<Vector2> uv, List<int> tri,
                                   Vector3 org, Quaternion rot, float hw, float hd,
                                   float h, float tex)
        {
            int i0 = v.Count;
            var up = Vector3.up * h;
            v.Add(org + rot * (new Vector3(-hw, 0, -hd) + up));
            v.Add(org + rot * (new Vector3(hw, 0, -hd) + up));
            v.Add(org + rot * (new Vector3(hw, 0, hd) + up));
            v.Add(org + rot * (new Vector3(-hw, 0, hd) + up));
            uv.Add(new Vector2(-hw / tex, -hd / tex));
            uv.Add(new Vector2(hw / tex, -hd / tex));
            uv.Add(new Vector2(hw / tex, hd / tex));
            uv.Add(new Vector2(-hw / tex, hd / tex));
            tri.AddRange(new[] { i0, i0 + 2, i0 + 1, i0, i0 + 3, i0 + 2 });
        }

        // Cakisma engeli TUM yerlesim boyunca ortaktir: cekirdek once yerini
        // rezerve eder, evler sonra ona carpmadan dizilir.
        private static readonly List<(Vector2 c, float r)> taken =
            new List<(Vector2 c, float r)>();

        /// <summary>
        /// Yerleşimin dünya kutusu (XZ) + boyanacak geçiş payı. Boşsa null.
        /// </summary>
        private static Rect? SettlementBounds(List<(Vector2 c, float r)> discs)
        {
            if (discs.Count == 0) return null;
            const float Pad = 30f;      // maskenin tam+geçiş payından geniş
            float minX = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxZ = float.MinValue;
            foreach (var (c, r) in discs)
            {
                minX = Mathf.Min(minX, c.x - r - Pad);
                maxX = Mathf.Max(maxX, c.x + r + Pad);
                minZ = Mathf.Min(minZ, c.y - r - Pad);
                maxZ = Mathf.Max(maxZ, c.y + r + Pad);
            }
            return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }

        private static bool Overlaps(List<(Vector2 c, float r)> taken, Vector2 c, float r)
        {
            foreach (var t in taken)
                if ((t.c - c).sqrMagnitude < (t.r + r) * (t.r + r)) return true;
            return false;
        }

        // -------------------------------------------------------- çekirdek

        /// <summary>
        /// Mescit + çeşme + dükkânlar: mahallenin durulan yeri.
        ///
        /// Mescit sokaktan **geri çekilir** (avlu payı) — mahalle mescidi
        /// cephe hattına dizilmez, önünde küçük bir avlusu vardır. Çeşme
        /// mescit avlusunun sokağa bakan köşesindedir: su alınan yer geçiş
        /// noktasında olur. Dükkânlar mescidin karşı sırasına dizilir —
        /// Osmanlı mahallesinde ticari çekirdek dinî çekirdeğin yanındadır.
        /// </summary>
        /// <summary>
        /// Galata Latin kilisesi — Ceneviz mirası, sokak cephesinde, çan kuleli.
        ///
        /// Neden Galata sahnesine giriyor: Galata 1453'te antlaşmayla teslim
        /// oldu ve Latin kiliseleri yerinde kaldı; 1632'de mahalle Müslüman,
        /// Katolik, Rum ve Ermeni'nin iç içe yaşadığı bir dokudur. Tek cemaatli
        /// bir Galata dönem hatasıdır.
        ///
        /// Mescitten farkı **avlusuz** olmasıdır: mescit avlusuyla birlikte bir
        /// meydan kurar, Latin kilisesi yoğun dokunun içinde cephesiyle sokağa
        /// oturur ve işareti kulesidir. Buraya Osmanlı avlu duvarı koymak,
        /// elimizde hazır olduğu için kolay ama yanlış olurdu.
        ///
        /// Balat'ta aynı işi <c>PF_Kilise_Rum_*</c> görür: kulesiz, alçak.
        /// Hangi kilisenin nereye ait olduğunu <see cref="QuarterSpec"/> söyler;
        /// Balat'a Latin bazilikası koymak dönem hatası olurdu.
        /// </summary>
        private static int PlaceChurch(string[] prefabs, List<Vector3> spine,
                                       Terrain terrain, Transform parent, float coreS)
            => PlaceBig(prefabs, spine, terrain, parent, coreS, eastFacing: true);

        /// <summary>
        /// Büyük yapıyı (kilise, hamam, han) sokağa yerleştirir.
        ///
        /// Üç yapının da sorunu aynıydı, çözümü de aynı: **cemaat/vakıf,
        /// arazinin kaldırdığı büyüklükte yapı yapar.** Büyükten küçüğe denenir,
        /// sokağa yakın düz yer bulan ilk boy kazanır.
        ///
        /// `eastFacing`: kilise apsisini doğuya döndürür (ADR 0018 §3.4a).
        /// Hamam ve han öyle bir kısıt taşımaz — kapıları sokağa bakar.
        /// </summary>
        private static int PlaceBig(string[] prefabs, List<Vector3> spine,
                                    Terrain terrain, Transform parent, float coreS,
                                    bool eastFacing, bool nearCore = false)
        {
            // CEMAAT, ARAZİNİN KALDIRDIĞI BÜYÜKLÜKTE KİLİSE YAPAR.
            //
            // Büyük bazilikayı zorlamak iki kötü sonuçtan birini veriyordu:
            // ya sokak kenarında 5,2 m'lik istinat duvarı (kale), ya da düz
            // cebi bulmak için 38 m içeri kaçış — ölçtüm, 45 m yarıçapında
            // **sıfır ev** kalıyordu: Galata değil, kır kilisesi. İkisi de
            // yerleştirme hatasıydı; asıl hata yapı seçiminin sabit olmasıydı.
            //
            // Büyükten küçüğe denenir; sokağa yakın düz bir yer bulan İLK
            // (yani en büyük) kilise kazanır. Mimarlık tarihinde de olan budur:
            // yamaç parseli, üstüne kurulacak yapının ölçüsünü tayin eder.
            if (prefabs == null || prefabs.Length == 0) return 0;
            foreach (string name in prefabs)
            {
                int n = TryPlaceChurch(name, spine, terrain, parent, coreS,
                                       nearOnly: true, eastFacing: eastFacing,
                                       nearCore: nearCore);
                if (n > 0) return n;
            }
            // Hiçbiri sokak kenarında düz yer bulamadı: en küçüğü, taramanın
            // tamamıyla (geri çekilme dahil) yerleştir.
            return TryPlaceChurch(prefabs[prefabs.Length - 1], spine, terrain,
                                  parent, coreS, nearOnly: false,
                                  eastFacing: eastFacing, nearCore: nearCore);
        }

        private static int TryPlaceChurch(string prefabName, List<Vector3> spine,
                                          Terrain terrain, Transform parent,
                                          float coreS, bool nearOnly,
                                          bool eastFacing = true,
                                          bool nearCore = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabDir}/{prefabName}.prefab");
            if (prefab == null)
            {
                Debug.LogWarning($"[Hezarfen] {prefabName} yok — atlandi.");
                return 0;
            }

            float total = PolylineLength(spine);
            float w = PrefabWidth(prefab), d = PrefabDepth(prefab);
            const float setback = 2.0f;               // kilise sokaga YAKIN oturur

            // KİLİSE SOKAĞA GÖRE DEĞİL, DOĞUYA GÖRE DÖNER.
            //
            // İlk denemede eve/mescide uygulanan kural buraya da uygulanmıştı:
            // "cepheni sokağa dön". Ölçüm 5,60 m kot farkı verdi — mescitte
            // düzelttiğim kale görüntüsünün 30 metrelik hâli. Sebebi şuydu:
            // sokak eş yükselti eğrisini izler, dolayısıyla sokağa DİK yön
            // yamacın **en dik yönü**dür; 29 m derinliğindeki bir bazilikayı
            // oraya dikmek en kötü seçimdi.
            //
            // Doğru kural mimarîden gelir, topoğrafyadan değil: Hristiyan
            // kilisesi **apsisi doğuya** bakacak şekilde kurulur. Yön böylece
            // sabittir; serbest kalan tek şey konumdur ve tarama onu arar.
            // Arazi kaydı gerçek UTM'dir, +X doğudur. Prefabın önü +Z olduğuna
            // göre apsis −Z'dedir; apsisi doğuya çevirmek için +Z batıya bakar.
            var rot = Quaternion.LookRotation(Vector3.left, Vector3.up);
            float ex = eastFacing ? d : w, ez = eastFacing ? w : d;

            // Seçim ölçütü İKİ AŞAMALIDIR, ağırlıklı toplam değil.
            //
            // Tek bir puanda (kot farkı + geri çekilme × katsayı) birleştirmeyi
            // denedim; kilise 38 m içeri kaçtı ve **boş yamaçta tek başına**
            // kaldı — Galata değil, kır kilisesi. Gerçek gereklilik şu: 5 m'lik
            // istinat duvarı kale gibi görünür, 2 m'lik görünmez. Yani düzlük
            // bir EŞİKtir, ölçek değil. Eşiği geçen adaylar arasında sokağa en
            // yakın olan kazanır; hiçbiri geçemezse en düz olan alınır.
            // 3,2 m eşiği ölçülerek seçildi. Bütün mahalle kutusu tarandığında
            // 29×20 m'lik bir ayak izi için **0,20 m**'lik yerler var — ama
            // hiçbiri sokağın yanında değil; sokak yamacı yanlamasına tarar ve
            // kenarı eğimlidir. "Dokunun içinde kal" ile "düz zemin" burada
            // çelişir ve dokunun içi kazanır: yamaç kilisesi zaten taş bir
            // altyapının üstüne oturur. Kale görüntüsünü veren 5,8 m'ydi; 3 m
            // subasman, mahallenin taş kaideleriyle aynı dilde okunur.
            const float flatEnough = 3.2f;
            float bestS = -1f, bestRange = float.MaxValue, bestBack = float.MaxValue;
            Vector2 bestC = Vector2.zero;
            bool haveFlat = false;
            for (int i = 0; i < 24; i++)
            {
                float s = total * (0.08f + 0.84f * i / 23f);
                // HAN ÇARŞIYA AİT, MAHALLEYE DEĞİL.
                //
                // Han'ı diğer büyük yapılar gibi "çekirdekten uzak dur" kuralıyla
                // yerleştirdim ve ölçüm 46,8 m uzağa düştüğünü gösterdi: boş
                // yamaçta tek başına bir han. Hata mesafede değil, kuralın
                // kendisindeydi — han konut mahallesinin değil, **ticaret
                // çekirdeğinin** yapısıdır ve dükkân sırasının yanında durur.
                // Diğerleri için kural aynen geçerli: mescitle aynı meydanı
                // paylaşmazlar.
                if (nearCore ? Mathf.Abs(s - coreS) > 60f
                             : Mathf.Abs(s - coreS) < 45f) continue;
                SampleAt(spine, s, out Vector3 pc, out Vector2 tc);
                Vector2 nc = new Vector2(-tc.y, tc.x);
                // Tarama sokak kenarıyla SINIRLI DEĞİL.
                //
                // Yalnız sokağa bitişik noktalara bakıldığında en iyi aday
                // 5,22 m kot farkı veriyordu; ölçüm bunun yerleştirme değil
                // ARAZİ olduğunu gösterdi: yamacın eğimi ~%15 ve 29 m derinlik
                // zaten ~4,4 m düşer. Mescidin 1,02 m'lik yeri 14 m'lik bir
                // düzlük cebiydi — 30 m'lik yapı oraya sığmıyor.
                //
                // Bu yüzden kilise sokaktan İÇERİ de çekilebilir. Karşılığı
                // gerçektir: kilise avlusu dokunun içindedir, cephesi her zaman
                // ana sokağa oturmaz; sokakla bağı bir geçittir.
                // "Yakin" 16 m'ye kadar sayilir.
                //
                // Ilk halinde {2, 8} idi ve tek sokaga dort buyuk yapi
                // (mescit + kilise + hamam + han) dizilince sonrakiler yakin
                // duz yer bulamayip 30-38 m geriye dusuyordu — olctum, en
                // yakin ev 34 m. Dokuya ait olmayan bir hamam, hamam degildir.
                // 16 m hala mahalle icidir: arada bir sira ev olur, cikmaz
                // oraya varir.
                var backs = nearOnly ? new[] { setback, 8f, 16f }
                                     : new[] { setback, 12f, 24f, 38f };
                foreach (float back in backs)
                foreach (int side in new[] { -1, 1 })
                {
                    Vector2 c = new Vector2(pc.x, pc.z)
                              + nc * (side * (StreetWidth * 0.5f + back
                                              + Mathf.Max(ex, ez) * 0.5f));
                    if (Overlaps(taken, c, Mathf.Max(ex, ez) * 0.5f)) continue;

                    float lo = float.MaxValue, hi = float.MinValue;
                    for (int a = -1; a <= 1; a += 2)
                        for (int b = -1; b <= 1; b += 2)
                        {
                            float h = Height(terrain,
                                c + new Vector2(a * ex * 0.5f, b * ez * 0.5f));
                            lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                        }
                    if (lo < 3f) continue;                 // suya kurulmaz
                    float range = hi - lo;
                    bool flat = range <= flatEnough;
                    bool better = flat
                        ? (!haveFlat || back < bestBack)   // duzse: sokaga en yakin
                        : (!haveFlat && range < bestRange); // hicbiri duz degilse: en duz
                    if (!better) continue;
                    haveFlat |= flat;
                    bestRange = range; bestS = s; bestC = c; bestBack = back;
                }
            }

            // Yakin taramada duz yer YOKSA bu boy kiliseye bu sokak uygun degil:
            // bir kucugu denensin diye BOS donulur.
            if (bestS < 0f || (nearOnly && !haveFlat)) return 0;
            if (bestRange > flatEnough)
                Debug.LogWarning($"[Hezarfen] {prefabName} {bestRange:F2} m kot "
                                 + "farkina oturdu — istinat duvari yuksek kalacak. "
                                 + "Bu arazide daha duz yer yok.");

            float y = TopOfFootprint(terrain, bestC, Mathf.Max(ex, ez));
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = new Vector3(bestC.x, y, bestC.y);
            if (eastFacing) inst.transform.rotation = rot;
            else
            {
                SampleAt(spine, bestS, out Vector3 pf, out Vector2 tf);
                Vector2 nf = new Vector2(-tf.y, tf.x);
                if (Vector2.Dot(bestC - new Vector2(pf.x, pf.z), nf) > 0f) nf = -nf;
                inst.transform.rotation = Quaternion.LookRotation(
                    new Vector3(nf.x, 0f, nf.y), Vector3.up);
            }
            w = ex; d = ez;

            // Kilise avlusu: servi + mezarlik. Kilise de mahalle cekirdegidir;
            // cevresi cıplak kalirsa dokuya degil, araziye konmus gibi durur.
            // Hristiyan mezarinda bas batida, ayak doguda — mezar ekseni bu
            // yuzden kilisenin apsis eksenine paraleldir.
            if (!eastFacing) { taken.Add((bestC, Mathf.Max(ex, ez) * 0.55f)); goto placed; }
            var churchNature = LoadNatureCatalog();
            Vector2 yardC = bestC + new Vector2(0f, ez * 0.5f + 5.5f);
            if (Height(terrain, yardC) > 3f)
                PlaceHazire(yardC, 6.0f, 8.0f, terrain, parent,
                            new System.Random(1632), muslim: false,
                            nature: churchNature);

            // Kural 8: en yuksek koseye oturur, altindaki bosluk tas kaideyle dolar.
            podiums.Add(new Podium
            {
                center = bestC, top = y,
                bottom = Mathf.Min(Height(terrain, bestC) - 0.6f, y - 0.4f),
                width = w + 0.6f, depth = d + 0.6f,
                yawDeg = inst.transform.rotation.eulerAngles.y,
            });
            taken.Add((bestC, Mathf.Max(w, d) * 0.55f));

        placed:
            Debug.Log($"[Hezarfen] {prefabName}: sokak boyunca "
                      + $"{bestS:F0} m, sokaktan {bestBack:F0} m geride, ayak izi "
                      + $"altinda kot farki {bestRange:F2} m (apsis doguya).");
            return 1;
        }

        /// <summary>
        /// Yerleşmiş bir yapının yanına ağaç koyar (kahvehane ↔ çınar).
        ///
        /// Neden yapıyı ADIYLA arıyor: <see cref="PlaceBig"/> nereye
        /// koyduğunu döndürmüyor ve imzasını yalnız bunun için değiştirmek,
        /// dört çağıranın hepsini ilgilendirmeyen bir bilgiyi taşımak olurdu.
        /// Yerleşemediyse ağaç da olmaz ve bu **sessiz kalmaz**.
        /// </summary>
        private static int PlaceTreeBeside(Transform parent, string prefixName,
                                           string treePrefix, Terrain terrain,
                                           System.Random rng)
        {
            Transform host = null;
            foreach (Transform t in parent)
                if (t.name.StartsWith(prefixName)) { host = t; break; }
            if (host == null) return 0;

            var trees = LoadNatureCatalog().FindAll(v => v.name.StartsWith(treePrefix));
            if (trees.Count == 0) return 0;
            var v = trees[rng.Next(trees.Count)];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabDir}/{v.prefab}.prefab");
            if (prefab == null) return 0;

            // Onunde degil YANINDA: cinar sacagi kapatmaz, sekiyi golgeler.
            Vector3 f = host.forward, r = host.right;
            float halfW = PrefabWidth(host.gameObject) * 0.5f;
            foreach (int side in new[] { 1, -1 })
                foreach (float ahead in new[] { 2.5f, 5.0f })
                {
                    Vector3 p = host.position + r * (side * (halfW + 2.6f)) + f * ahead;
                    Vector2 c = new Vector2(p.x, p.z);
                    float y = Height(terrain, c);
                    if (y < 3f || Overlaps(taken, c, 2.2f)) continue;
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    inst.transform.position = new Vector3(c.x, y, c.y);
                    inst.transform.rotation = Quaternion.Euler(
                        0f, (float)rng.NextDouble() * 360f, 0f);
                    taken.Add((c, 2.0f));
                    Debug.Log($"[Hezarfen] {v.prefab} kahvehanenin yaninda "
                              + $"({Vector3.Distance(inst.transform.position, host.position):F1} m).");
                    return 1;
                }
            Debug.LogWarning($"[Hezarfen] {prefixName} yanina {treePrefix} "
                             + "konamadi — dort aday da elendi.");
            return 0;
        }

        /// <summary>
        /// Türbe — hazîrenin kıble ucunda, kapısı mezarlığa bakar.
        ///
        /// Hazîrenin İÇİNE değil UCUNA: 7,3 m'lik sekizgen, 7×9 m'lik bir
        /// hazîrenin duvarları arasına sığmaz ve zorlanınca mezar taşlarının
        /// üstüne oturur. Gerçekte de türbe hazîrenin sınırını **oluşturur**;
        /// duvarın bir parçasıdır.
        /// </summary>
        private static int PlaceTurbe(Vector2 hazireCenter, Vector2 axis,
                                      float halfDepth, Terrain terrain,
                                      Transform parent)
        {
            foreach (string name in new[] { "PF_Turbe_A", "PF_Turbe_B" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{PrefabDir}/{name}.prefab");
                if (prefab == null) continue;
                float r = Mathf.Max(PrefabWidth(prefab), PrefabDepth(prefab)) * 0.5f;

                foreach (int sgn in new[] { 1, -1 })
                {
                    Vector2 c = hazireCenter + axis * (sgn * (halfDepth + r + 0.6f));
                    if (Height(terrain, c) < 3f) continue;
                    if (Overlaps(taken, c, r * 0.9f)) continue;

                    float lo = float.MaxValue, hi = float.MinValue;
                    for (int a = -1; a <= 1; a += 2)
                        for (int b = -1; b <= 1; b += 2)
                        {
                            float h = Height(terrain, c + new Vector2(a * r, b * r));
                            lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                        }
                    if (hi - lo > 2.2f) continue;      // turbe kucuk, terasi olmaz

                    float y = TopOfFootprint(terrain, c, r * 2f);
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    inst.transform.position = new Vector3(c.x, y, c.y);
                    inst.transform.rotation = Quaternion.LookRotation(
                        new Vector3(-sgn * axis.x, 0f, -sgn * axis.y), Vector3.up);
                    taken.Add((c, r * 0.95f));
                    Debug.Log($"[Hezarfen] {name} hazirenin ucunda, kot farki "
                              + $"{hi - lo:F2} m.");
                    return 1;
                }
            }
            Debug.LogWarning("[Hezarfen] Turbe konamadi — hazirenin iki ucu da "
                             + "elendi (su/kot/cakisma).");
            return 0;
        }

        private static int PlaceCore(QuarterSpec q, List<Vector3> spine, Terrain terrain,
                                     Transform parent, System.Random rng,
                                     out float coreS)
        {
            coreS = -1f;
            if (spine.Count < 8) return 0;
            float total = PolylineLength(spine);

            // Çekirdek, sokağın EN DÜZ yerine kurulur.
            //
            // İlk denemede sabit bir noktaya (uzunluğun %42'si) konuyordu ve
            // orası dik bir yamaca denk geldi: teras 5,8 m yükseldi, istinat
            // duvarı mahalleyi kale gibi gösterdi. Sebep yerleştirme kuralıydı,
            // teras değil.
            //
            // Mahalle merkezi gerçekte de düzlüğe kurulur: cami, çeşme, dükkân
            // ve toplanma yeri düz zemin ister; ev tek başına yamaca oturabilir,
            // meydan oturamaz. 20 aday nokta taranıp avlu ayak izi altındaki
            // kot farkı en küçük olan seçilir.
            float s0 = total * 0.5f, bestRange = float.MaxValue;
            for (int i = 0; i < 20; i++)
            {
                float s = total * (0.18f + 0.64f * i / 19f);
                SampleAt(spine, s, out Vector3 pc, out Vector2 tc);
                Vector2 nc = new Vector2(-tc.y, tc.x);
                Vector2 probe = new Vector2(pc.x, pc.z) + nc * 9f;
                float lo = float.MaxValue, hi = float.MinValue;
                for (int a = -1; a <= 1; a += 2)
                    for (int b = -1; b <= 1; b += 2)
                    {
                        float h = Height(terrain, probe + new Vector2(a * 7f, b * 7f));
                        lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                    }
                if (lo < 3f) continue;                 // suya kurulmaz
                if (hi - lo < bestRange) { bestRange = hi - lo; s0 = s; }
            }
            Debug.Log($"[Hezarfen] Cekirdek konumu: sokak boyunca {s0:F0} m, "
                      + $"avlu altinda kot farki {bestRange:F2} m (20 aday tarandi).");
            coreS = s0;
            int n = 0;

            // --- çekirdek yapı: mescit ya da sinagog ---
            //
            // İkisi de **avlulu** yerleşir ama sebepleri farklıdır ve fark
            // avlu payına yansır. Mescidin avlusu bir MEYDANdır: şadırvanıyla,
            // kapısıyla mahallenin toplanma yeri. Sinagogunki ise bir
            // PERDEdir — "kendine özgü mimarîsi olmayan, yüksek duvarlı bir
            // avlunun içinde" duran yapıyı sokaktan ayırır (RESEARCH.md
            // §4.2c). Sinagogu sinagog yapan şey cephesi değil, o duvardır;
            // duvarsız koymak varlığın kendi tezini eksik bırakır.
            bool sinagog = q.CoreKind == "sinagog";
            string corePrefab = sinagog ? SinagogPrefab : MescitPrefab;
            float yard = sinagog ? 5.0f : 3.5f;

            SampleAt(spine, s0, out Vector3 p0, out Vector2 t0);
            Vector2 n0 = new Vector2(-t0.y, t0.x);
            var core = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabDir}/{corePrefab}.prefab");
            if (core != null)
            {
                float depth = PrefabDepth(core);
                Vector2 c = new Vector2(p0.x, p0.z)
                          + n0 * (StreetWidth * 0.5f + yard + depth * 0.5f);
                float y = TopOfFootprint(terrain, c, depth * 1.1f);
                if (y > 3f)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(core, parent);
                    inst.transform.position = new Vector3(c.x, y, c.y);
                    inst.transform.rotation = Quaternion.LookRotation(
                        new Vector3(-n0.x, 0f, -n0.y), Vector3.up);
                    // Cekirdek + avlusu rezerve edilir; evler buraya giremez.
                    taken.Add((c, depth * 0.75f + 2.0f));
                    n++;
                    n += PlaceCourtyard(inst.transform, c, n0, yard,
                                        PrefabWidth(core), depth,
                                        new Vector2(p0.x, p0.z), terrain, parent,
                                        sadirvan: !sinagog);

                    var nature = LoadNatureCatalog();
                    float cw = PrefabWidth(core) + 2.6f;
                    float cd = depth + yard + 1.2f;
                    Vector2 cc = c - n0 * (yard * 0.5f + 0.6f);
                    n += PlaceCourtyardTrees(cc, n0, cw, cd, y, terrain, parent,
                                             rng, nature, c, depth * 0.60f);

                    // Hazire caminin ARKASINDA ya da yanında: avluyu daraltmaz,
                    // sokaktan görülür ama geçişi kesmez. Tek aday nokta
                    // yetmedi — çekirdeğin kendi rezervasyonu onu eliyordu.
                    // Ölçüt YİNE `taken` değil, yapının kendisi.
                    //
                    // İkinci kez aynı tuzağa düştüm: çekirdek kendi çevresini
                    // `depth*0,75+2` yarıçapla rezerve ediyor ve hazirenin
                    // bütün aday noktaları o dairenin içinde kalıyordu — üçü de
                    // sessizce elendi. Hazire zaten çekirdeğin AVLUSUNA aittir;
                    // kaçınması gereken tek şey caminin gövdesidir. Kendi
                    // yerini `PlaceHazire` rezerve eder, evler ona çarpmaz.
                    // HAZIRE YALNIZCA CAMIDE.
                    //
                    // Sinagogun yanina mezarlik koymak donem ve gelenek
                    // hatasidir: Yahudi defni yerlesimin DISINDA yapilir —
                    // Istanbul'da Haskoy ve Kuzguncuk mezarliklari mahallelerin
                    // disindadir. Cami yanindaki hazire ise Osmanli
                    // pratiginin kendisidir. Kilisenin mezarligi ayrica
                    // `PlaceChurch` icinde kurulur.
                    Vector2 sd0 = Perp(n0);
                    int hazireCount = 0;
                    if (!sinagog)
                    foreach (var off in new[] { n0 * (depth * 0.5f + 9.0f),
                                                sd0 * (PrefabWidth(core) * 0.5f + 8.0f),
                                                -sd0 * (PrefabWidth(core) * 0.5f + 8.0f) })
                    {
                        Vector2 hz = c + off;
                        if (Height(terrain, hz) < 3f) continue;
                        if (Vector2.Distance(hz, c) < depth * 0.55f + 4.0f) continue;
                        hazireCount = PlaceHazire(hz, 7.0f, 9.0f, terrain, parent,
                                                  rng, muslim: !sinagog, nature: nature);
                        if (hazireCount > 0) break;
                    }
                    if (hazireCount == 0 && !sinagog)
                        Debug.LogWarning("[Hezarfen] Hazire kurulamadi — "
                                         + "cekirdegin uc yani da elendi.");
                    n += hazireCount;
                }
            }
            else
            {
                Debug.LogWarning($"[Hezarfen] {corePrefab} yok — {q.Name} cekirdeksiz kaldi.");
            }

            // --- çeşme ve dükkânlar ---
            var street = LoadStreetCatalog();
            var cesme = street.FindAll(v => v.name.StartsWith("Cesme"));
            var dukkan = street.FindAll(v => v.name.StartsWith("Dukkan"));

            // ÇEŞME TEK DENEMEYLE BIRAKILMAZ.
            //
            // Balat'ta tam bu oldu: tek aday nokta elendi ve mahalle **susuz**
            // kaldı — hiçbir uyarı çıkmadan. Oysa mahallenin toplanma sebebi
            // sudur; çeşmesiz mahalle, mescitsiz mahalle kadar eksiktir.
            // Sokak boyunca birkaç konum ve iki yan denenir, hepsi elenirse
            // LOGLANIR.
            int cesmeCount = 0;
            if (cesme.Count > 0)
            {
                var v = cesme[rng.Next(cesme.Count)];
                foreach (float ds0 in new[] { -9f, -14f, -5f, -20f, 6f, 12f })
                    foreach (int sd in new[] { +1, -1 })
                    {
                        if (cesmeCount > 0) break;
                        cesmeCount += PlaceProp(spine, terrain, parent, v,
                                                s0 + ds0, sd, 0f);
                    }
            }
            if (cesmeCount == 0)
                Debug.LogWarning("[Hezarfen] Mahalle CESMESIZ kaldi — "
                                 + "butun aday noktalar elendi (su/cakisma).");
            n += cesmeCount;

            // SEBİL ÇEKİRDEĞİN KÖŞESİNDE, ÇEŞMENİN YANINDA DEĞİL.
            //
            // İkisi de su verir ama farklı biçimde: çeşmeden kendin alırsın,
            // sebilden sana verilir — sebil bir görevlinin durduğu odadır ve
            // kalabalığın geçtiği yere, avlu kapısının yanına konur. Bu yüzden
            // `PlaceBig`in 60 m'lik "çekirdeğe yakın" penceresi burada çok
            // geniş: sebil çekirdeğin YANINDA olmalı, mahallenin bir yerinde
            // değil. Aday noktalar ±12 m ile sınırlı.
            //
            // Vakıf kurumudur (su vakfı), yani türbe ve mektep gibi yalnız
            // müslüman mahallesinde.
            if (q.HasVakif && q.HasSebil)
            {
                var sebil = LoadMahalleCatalog().FindAll(v => v.name.StartsWith("Sebil"));
                int sebilCount = 0;
                if (sebil.Count > 0)
                    foreach (float ds0 in new[] { 6f, -6f, 11f, -11f })
                        foreach (int sd in new[] { +1, -1 })
                        {
                            if (sebilCount > 0) break;
                            sebilCount += PlaceProp(spine, terrain, parent,
                                                    sebil[0], s0 + ds0, sd, 0f);
                        }
                if (sebilCount == 0)
                    Debug.LogWarning("[Hezarfen] Sebil konamadi — cekirdegin "
                                     + "cevresindeki sekiz aday da elendi.");
                else
                    Debug.Log("[Hezarfen] Sebil cekirdegin kosesinde.");
                n += sebilCount;
            }

            // DÜKKÂN SIRASI, mescidin KARŞI sırasında.
            //
            // İlk hâlinde dört slot deneniyor ve elenen slot **kayboluyordu**:
            // ölçüldü, sahnede yalnız **iki** dükkân vardı. Sebep çakışma
            // değil sıraydı — sebil ve çeşme çekirdeğin çevresine ondan önce
            // yerleşiyor ve ilk iki slotu kaplıyor. Yani "birkaç dükkân"
            // sessizce "iki dükkân" oluyordu.
            //
            // Sıra artık slot değil **hedef** sayıyor: dördü yerleşene kadar
            // sokak boyunca ilerlenir. Bir dükkân sırası zaten böyle uzar;
            // dolu yerin üstüne değil, yanına dizilir.
            const int WantShops = 4;
            float ds = s0 - 7f;
            int shops = 0;
            for (int guard = 0; guard < 24 && shops < WantShops && dukkan.Count > 0;
                 guard++)
            {
                var v = dukkan[rng.Next(dukkan.Count)];
                if (PlaceProp(spine, terrain, parent, v, ds, -1, 0f) > 0)
                { shops++; n++; }
                ds += v.wall_width + 0.4f;
            }
            if (shops < WantShops)
                Debug.LogWarning($"[Hezarfen] Dukkan sirasi eksik: {shops}/{WantShops} "
                                 + "— 24 slot tarandi, kalani elendi.");
            return n;
        }

        private static int PlaceProp(List<Vector3> spine, Terrain terrain,
                                     Transform parent, Variant v, float s, int side,
                                     float extraSetback)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabDir}/{v.prefab}.prefab");
            if (prefab == null) return 0;

            SampleAt(spine, s, out Vector3 pos, out Vector2 tan);
            Vector2 nrm = new Vector2(-tan.y, tan.x) * side;
            Vector2 c = new Vector2(pos.x, pos.z)
                      + nrm * (StreetWidth * 0.5f + v.wall_depth * 0.5f + extraSetback);

            float r = Mathf.Max(v.wall_width, v.wall_depth) * 0.5f;
            float y = TopOfFootprint(terrain, c, Mathf.Max(v.wall_width, v.wall_depth));
            if (y < 3f || Overlaps(taken, c, r * 0.7f)) return 0;

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = new Vector3(c.x, y, c.y);
            inst.transform.rotation = Quaternion.LookRotation(
                new Vector3(-nrm.x, 0f, -nrm.y), Vector3.up);
            taken.Add((c, r * 0.7f));
            return 1;
        }

        /// <summary>
        /// Mescit avlusu: **teras + merdiven + duvar + kapı + şadırvan**.
        ///
        /// Ölçüldü: geri çekilme (avlu payı) ile "en yüksek köşe" kuralı dik
        /// yamaçta birikiyor ve mescit sokaktan **5,8 m** yukarıda kalıyordu —
        /// yanlış değil (yamaç camisi teraslıdır) ama bağlantısızdı: yapıya
        /// çıkmanın bir yolu yoktu.
        ///
        /// Çözüm yamaç camisinin gerçekte yapıldığı şey: avlu **düz bir teras**
        /// olur, aşağı yüzü taş istinat duvarıyla tutulur, sokaktan **merdivenle**
        /// çıkılır. Teras ve basamaklar kaide mesh'ini yeniden kullanır (aynı
        /// dünya ölçekli UV, aynı tek mesh) — ayrı bir sistem kurmak, iki farklı
        /// taş dokusu yoğunluğu demek olurdu.
        /// </summary>
        private static int PlaceCourtyard(Transform mescit, Vector2 mc, Vector2 nrm,
                                          float courtyard, float mw, float md,
                                          Vector2 streetPoint, Terrain terrain,
                                          Transform parent, bool sadirvan = true)
        {
            float yTop = mescit.position.y;
            float yaw = mescit.rotation.eulerAngles.y;
            Vector2 toStreet = -nrm;                       // avlu sokaga dogru uzanir

            float cw = mw + 2.6f;                          // avlu genisligi
            float cd = md + courtyard + 1.2f;              // mescit + avlu derinligi
            Vector2 cc = mc + toStreet * (courtyard * 0.5f + 0.6f);

            // --- teras ---
            float lo = float.MaxValue;
            for (int i = -1; i <= 1; i += 2)
                for (int j = -1; j <= 1; j += 2)
                    lo = Mathf.Min(lo, Height(terrain,
                        cc + nrm * (j * cd * 0.5f) + Perp(nrm) * (i * cw * 0.5f)));
            podiums.Add(new Podium
            {
                center = cc, top = yTop, bottom = Mathf.Min(lo - 0.6f, yTop - 0.4f),
                width = cw, depth = cd, yawDeg = yaw,
            });

            // --- merdiven: sokaktan terasa ---
            Vector2 frontEdge = cc + toStreet * (cd * 0.5f);
            float yStreet = Height(terrain, streetPoint);
            float rise = yTop - yStreet;
            int steps = Mathf.Clamp(Mathf.RoundToInt(rise / 0.19f), 1, 40);
            float tread = 0.30f;
            for (int i = 0; i < steps; i++)
            {
                float top = yStreet + (i + 1) * rise / steps;
                float outward = (steps - i) * tread;
                podiums.Add(new Podium
                {
                    center = frontEdge + toStreet * (outward * 0.5f),
                    top = top, bottom = top - rise / steps - 0.35f,
                    width = 3.2f, depth = outward, yawDeg = yaw,
                });
            }

            int n = 0;
            var wall = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/PF_AvluDuvar.prefab");
            var gate = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/PF_AvluKapi.prefab");
            var sad = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/PF_Sadirvan.prefab");

            // --- kapi: on kenarin ortasinda, merdivenin tepesinde ---
            if (gate != null)
            {
                n += Put(gate, parent, frontEdge, yTop, toStreet);
                taken.Add((frontEdge, 2.0f));
            }

            // --- duvar halkasi: on kenar (kapi bosluklu) + iki yan ---
            if (wall != null)
            {
                const float seg = 4.0f;
                Vector2 side = Perp(nrm);
                // On kenar: kapinin iki yani
                for (int s = -1; s <= 1; s += 2)
                {
                    float span = cw * 0.5f - 1.9f;         // kapi yarim genisligi
                    int cnt = Mathf.FloorToInt(span / seg);
                    for (int i = 0; i < cnt; i++)
                        n += Put(wall, parent,
                                 frontEdge + side * (s * (1.9f + seg * (i + 0.5f))),
                                 yTop, toStreet);
                }
                // Yan kenarlar
                for (int s = -1; s <= 1; s += 2)
                {
                    int cnt = Mathf.FloorToInt((cd - 1.0f) / seg);
                    for (int i = 0; i < cnt; i++)
                    {
                        Vector2 pos = cc + side * (s * cw * 0.5f)
                                    + toStreet * (cd * 0.5f - seg * (i + 0.5f) - 0.4f);
                        n += Put(wall, parent, pos, yTop, side * s);
                    }
                }
            }

            // --- sadirvan: avlunun ortasinda, mescitle kapi arasinda ---
            //
            // YALNIZ camide. Sadirvan abdest icindir; sinagog avlusuna koymak
            // "elimizde vardi" demekten baska gerekce tasimaz.
            if (sad != null && sadirvan)
            {
                Vector2 sc = mc + toStreet * (md * 0.5f + courtyard * 0.55f);
                n += Put(sad, parent, sc, yTop, toStreet);
                taken.Add((sc, 3.0f));
            }
            return n;
        }

        /// <summary>
        /// Kıble azimutu (İstanbul'dan Kâbe'ye), kuzeyden saat yönünde derece.
        /// Mezar ekseni buna **dik**tir: ölü sağ yanına, yüzü kıbleye dönük yatar.
        /// </summary>
        public const float QiblaAzimuthDeg = 151.6f;

        private static Vector2 FromAzimuth(float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(r), Mathf.Cos(r));   // +Z = kuzey
        }

        /// <summary>
        /// **Hazire** — cami avlusunun ya da kilisenin yanındaki küçük mezarlık.
        ///
        /// Neden mahalle dokusunun parçası: RESEARCH.md §4 mezarlıkları
        /// *"servi alanlarıyla kent içi büyük yeşil kütleler"* diye anar. Cami
        /// yanındaki hazire bunun mahalle ölçeğidir ve serviyle birlikte gelir;
        /// serviyi ayırırsan geriye dikili taşlar kalır.
        ///
        /// Mezar ekseni **inançtan** gelir, arazi ya da sokaktan değil:
        ///   * Müslüman mezarı kıbleye diktir (bkz. <see cref="QiblaAzimuthDeg"/>),
        ///   * Hristiyan mezarında baş batıda, ayak doğudadır.
        /// Bu, kilisenin apsisini doğuya döndüren kuralın mezar ölçeğidir; iki
        /// mezarlığı üstten bile ayırt ettirir.
        /// </summary>
        private static int PlaceHazire(Vector2 center, float width, float depth,
                                       Terrain terrain, Transform parent,
                                       System.Random rng, bool muslim,
                                       List<Variant> nature)
        {
            var graves = nature.FindAll(v => v.kind != null && v.kind.StartsWith("mezar"));
            var servi = nature.FindAll(v => v.name.StartsWith("Servi"));
            if (graves.Count == 0) return 0;

            Vector2 axis = muslim ? FromAzimuth(QiblaAzimuthDeg + 90f)
                                  : new Vector2(1f, 0f);      // bati -> dogu
            Vector2 side = Perp(axis);

            int n = 0;
            const float rowGap = 2.9f;        // mezar boyu + yol
            const float colGap = 1.25f;       // yan yana mezar
            int rows = Mathf.Max(1, Mathf.FloorToInt(depth / rowGap));
            int cols = Mathf.Max(1, Mathf.FloorToInt(width / colGap));

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    // Boşluklar bilerek: hazire tam dolu bir ızgara değildir.
                    if (rng.NextDouble() < 0.22) continue;
                    Vector2 p = center
                              + axis * ((r - (rows - 1) * 0.5f) * rowGap)
                              + side * ((c - (cols - 1) * 0.5f) * colGap);
                    p += side * ((float)rng.NextDouble() - 0.5f) * 0.22f;
                    float y = Height(terrain, p);
                    if (y < 3f) continue;
                    var v = graves[rng.Next(graves.Count)];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        $"{PrefabDir}/{v.prefab}.prefab");
                    if (prefab == null) continue;
                    float yaw = ((float)rng.NextDouble() - 0.5f) * 10f;
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    inst.transform.position = new Vector3(p.x, y, p.y);
                    inst.transform.rotation =
                        Quaternion.LookRotation(new Vector3(axis.x, 0f, axis.y), Vector3.up)
                        * Quaternion.Euler(0f, yaw, 0f);
                    n++;
                }

            // Servi mezarlığın KENARINDA, taşların arasında değil: kökü mezarı
            // bozar, gölgesi yolu gölgeler.
            if (servi.Count > 0)
                for (int i = 0; i < 4; i++)
                {
                    Vector2 p = center
                              + side * ((i % 2 == 0 ? -1f : 1f) * (width * 0.5f + 1.4f))
                              + axis * ((i < 2 ? -1f : 1f) * depth * 0.30f);
                    float y = Height(terrain, p);
                    if (y < 3f) continue;
                    var sv = servi[rng.Next(servi.Count)];
                    n += Put(AssetDatabase.LoadAssetAtPath<GameObject>(
                        $"{PrefabDir}/{sv.prefab}.prefab"), parent, p, y, axis);
                }

            // Hazire DUVARLIDIR: mezarlık sokaktan alçak bir duvarla ayrılır.
            // Duvarsız bırakınca taşlar araziye serpilmiş gibi duruyordu;
            // hazireyi hazire yapan şey o sınırdır — cami avlusunun devamıdır,
            // boş arsa değil.
            float hw = width * 0.5f + 1.9f, hd = depth * 0.5f + 1.3f;

            // TÜRBE hazîrenin ucunda. Mahalleye adını veren yapı budur:
            // vakfı kuran kişi kendi mescidinin hazîresine gömülür ve mahalle
            // onun adıyla anılır (ADR 0021 §2). Yalnız müslüman hazîresinde —
            // hristiyan mezarlığında bu tip yoktur.
            if (muslim) n += PlaceTurbe(center, axis, hd, terrain, parent);

            var seg = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabDir}/PF_AvluDuvarKisa.prefab");
            if (seg != null)
            {
                const float segLen = 2.0f;
                int nx = Mathf.Max(1, Mathf.RoundToInt(hw * 2f / segLen));
                int nz = Mathf.Max(1, Mathf.RoundToInt(hd * 2f / segLen));
                for (int i = 0; i < nx; i++)
                    foreach (int sgn in new[] { -1, 1 })
                    {
                        Vector2 pw = center + axis * (sgn * hd)
                                   + side * (-hw + segLen * (i + 0.5f));
                        float y = Height(terrain, pw);
                        if (y > 3f) n += Put(seg, parent, pw, y, axis);
                    }
                for (int i = 0; i < nz; i++)
                    foreach (int sgn in new[] { -1, 1 })
                    {
                        Vector2 pw = center + side * (sgn * hw)
                                   + axis * (-hd + segLen * (i + 0.5f));
                        float y = Height(terrain, pw);
                        if (y > 3f) n += Put(seg, parent, pw, y, side * sgn);
                    }
            }

            taken.Add((center, Mathf.Max(width, depth) * 0.5f + 2.6f));
            return n;
        }

        /// <summary>Avluya ağaç diker — servi köşelere, çınar varsa ortaya yakın.</summary>
        private static int PlaceCourtyardTrees(Vector2 cc, Vector2 nrm, float cw,
                                               float cd, float yTop, Terrain terrain,
                                               Transform parent, System.Random rng,
                                               List<Variant> nature,
                                               Vector2 coreCenter, float coreRadius)
        {
            // Avlu ağaçsız kurulunca taş bir meydan gibi okunur; oysa oraya
            // gölgelenmek için oturulur. Servi cami avlusunun imzasıdır, çınar
            // gölge ağacıdır — çınar ancak avlu onu kaldırıyorsa dikilir,
            // yoksa tacı duvarları yutar.
            var servi = nature.FindAll(v => v.name.StartsWith("Servi"));
            var cinar = nature.FindAll(v => v.name.StartsWith("Cinar"));
            if (servi.Count == 0) return 0;

            Vector2 side = Perp(nrm);
            int n = 0;
            // `taken`e BAKILMAZ: avlunun tamamı zaten çekirdek için rezerve
            // edilmiştir, dolayısıyla her ağaç kendi avlusuna çarpar. İlk
            // denemede tam bu oldu ve avlu yine ağaçsız kaldı. Kaçınılacak şey
            // rezervasyon değil, **yapının kendisi**: gövdeden uzak dur.
            foreach (var (sx, sy) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                Vector2 p = cc + side * (sx * (cw * 0.5f - 1.1f))
                               + nrm * (sy * (cd * 0.5f - 1.2f));
                if (Vector2.Distance(p, coreCenter) < coreRadius) continue;
                if (Height(terrain, p) < 3f) continue;
                var v = servi[rng.Next(servi.Count)];
                n += Put(AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{PrefabDir}/{v.prefab}.prefab"), parent, p, yTop, nrm);
            }

            var fit = cinar.FindAll(v => v.wall_width < Mathf.Min(cw, cd) - 1.0f);
            if (fit.Count > 0)
            {
                Vector2 p = cc + side * (cw * 0.22f);
                var v = fit[rng.Next(fit.Count)];
                if (Vector2.Distance(p, coreCenter) >= coreRadius
                    && Height(terrain, p) >= 3f)
                    n += Put(AssetDatabase.LoadAssetAtPath<GameObject>(
                        $"{PrefabDir}/{v.prefab}.prefab"), parent, p, yTop, nrm);
            }
            return n;
        }

        private static Vector2 Perp(Vector2 v) => new Vector2(-v.y, v.x);

        /// <summary>Prefab'ı verilen noktaya, `facing` yönüne bakacak şekilde koyar.</summary>
        private static int Put(GameObject prefab, Transform parent, Vector2 c,
                               float y, Vector2 facing)
        {
            if (prefab == null) return 0;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = new Vector3(c.x, y, c.y);
            inst.transform.rotation = Quaternion.LookRotation(
                new Vector3(facing.x, 0f, facing.y), Vector3.up);
            return 1;
        }

        private static float PrefabWidth(GameObject prefab)
        {
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
                if (r.gameObject.name.EndsWith("LOD0")) return r.bounds.size.x;
            return 10f;
        }

        /// <summary>Ayak izinin en yüksek köşesi — ev yerleşimiyle aynı kural (8).</summary>
        private static float TopOfFootprint(Terrain t, Vector2 c, float size)
        {
            float h = size * 0.5f, hi = float.MinValue;
            for (int i = -1; i <= 1; i += 2)
                for (int j = -1; j <= 1; j += 2)
                    hi = Mathf.Max(hi, Height(t, c + new Vector2(i * h, j * h)));
            return hi;
        }

        private static float PrefabDepth(GameObject prefab)
        {
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
                if (r.gameObject.name.EndsWith("LOD0")) return r.bounds.size.z;
            return 10f;
        }

        private static List<Variant> LoadStreetCatalog() =>
            LoadVariants(StreetCatalogPath, "Sokak donatisi");

        private static List<Variant> LoadNatureCatalog() =>
            LoadVariants(NatureCatalogPath, "Doga/mezar");

        private static List<Variant> LoadMahalleCatalog() =>
            LoadVariants(MahalleCatalogPath, "Mahalle yapisi");

        private static List<Variant> LoadVariants(string catalogPath, string label)
        {
            var list = new List<Variant>();
            string repo = TerrainImporter.RepositoryRoot();
            if (repo == null) return list;
            string path = Path.Combine(repo,
                catalogPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Hezarfen] {label} katalogu yok: {catalogPath}");
                return list;
            }
            var cat = JsonUtility.FromJson<Catalog>(File.ReadAllText(path));
            if (cat?.variants == null) return list;
            foreach (var v in cat.variants)
                if (AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{v.prefab}.prefab") != null)
                    list.Add(v);
            return list;
        }

        private static Variant Pick(List<Variant> all, System.Random rng, bool corner)
        {
            var pool = new List<Variant>();
            foreach (var v in all)
                if ((v.facades == "sides") == corner) pool.Add(v);
            if (pool.Count == 0) pool = all;
            return pool[rng.Next(pool.Count)];
        }

        private static float PolylineLength(List<Vector3> p)
        {
            float t = 0f;
            for (int i = 1; i < p.Count; i++)
                t += Vector2.Distance(new Vector2(p[i - 1].x, p[i - 1].z),
                                      new Vector2(p[i].x, p[i].z));
            return t;
        }

        private static void SampleAt(List<Vector3> p, float s, out Vector3 pos, out Vector2 tan)
        {
            float acc = 0f;
            for (int i = 1; i < p.Count; i++)
            {
                var a = new Vector2(p[i - 1].x, p[i - 1].z);
                var b = new Vector2(p[i].x, p[i].z);
                float len = Vector2.Distance(a, b);
                if (acc + len >= s || i == p.Count - 1)
                {
                    float u = len > 1e-4f ? Mathf.Clamp01((s - acc) / len) : 0f;
                    Vector2 c = Vector2.Lerp(a, b, u);
                    pos = new Vector3(c.x, 0f, c.y);
                    tan = (b - a).sqrMagnitude > 1e-6f ? (b - a).normalized : Vector2.right;
                    return;
                }
                acc += len;
            }
            pos = p[0]; tan = Vector2.right;
        }

        // ------------------------------------------------------------ katalog

        private static List<Variant> LoadCatalog()
        {
            string repo = TerrainImporter.RepositoryRoot();
            var list = new List<Variant>();
            if (repo == null) return list;

            string path = Path.Combine(repo, CatalogPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                Debug.LogError($"[Hezarfen] Katalog yok: {CatalogPath}. Once calistir: "
                               + "blender --background --python tools/blender/gen_house_variants.py");
                return list;
            }

            var cat = JsonUtility.FromJson<Catalog>(File.ReadAllText(path));
            if (cat?.variants == null) return list;

            foreach (var v in cat.variants)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{v.prefab}.prefab") == null)
                {
                    Debug.LogWarning($"[Hezarfen] Prefab yok, atlandi: {v.prefab} "
                                     + "(Hezarfen -> Boru Hatti -> _Import'u yerlestir)");
                    continue;
                }
                list.Add(v);
            }
            return list;
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
