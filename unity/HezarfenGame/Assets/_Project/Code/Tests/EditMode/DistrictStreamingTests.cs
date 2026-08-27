using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hezarfen.Editor.Gis;
using Hezarfen.Streaming;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Bölge yayını iskeleti — plan Faz 1 madde 6.
    ///
    /// Üç ayrı şeyi korur:
    ///   1. **Geometri.** İçerme ve kenar uzaklığı. Uzaklık merkeze göre ölçülseydi
    ///      Haliç gibi ince bükük şeritlerde yanlış olurdu; test bunu bağlar.
    ///   2. **Histerezis.** "Yükleme ekranı yok" vaadi, sınırda gidip gelen oyuncunun
    ///      sahneyi sürekli yükleyip boşaltmamasına dayanır. Tek eşikle bu çöker.
    ///   3. **Veri bütünlüğü.** İçe aktarılmış varlıkların uçuş koridorunu kapsaması,
    ///      adreslerin tekil olması, sınırların dünyanın içinde kalması.
    ///
    /// Addressables'a hiç dokunulmaz: karar mantığı bilerek saf tutuldu ki
    /// doğruluğu bir build ya da oyun oturumu gerektirmeden kanıtlanabilsin.
    /// </summary>
    public class DistrictStreamingTests
    {
        // 1 km kenarli kare, orijinde
        private static Vector2[] Square(float half = 500f) => new[]
        {
            new Vector2(-half, -half), new Vector2(half, -half),
            new Vector2(half, half), new Vector2(-half, half),
        };

        private static DistrictDef MakeDef(string id, Vector2[] ring,
                                           float load = 700f, float unload = 910f,
                                           int priority = 1)
        {
            var d = ScriptableObject.CreateInstance<DistrictDef>();
            d.districtId = id;
            d.displayName = id;
            d.sceneAddress = "Districts/" + id;
            d.boundary = ring;
            d.loadDistanceMeters = load;
            d.unloadDistanceMeters = unload;
            d.priority = priority;
            return d;
        }

        // ------------------------------------------------------------- geometri

        [Test]
        public void Contains_InsideAndOutside()
        {
            var ring = Square();
            Assert.IsTrue(DistrictDef.ContainsXZ(ring, 0f, 0f));
            Assert.IsTrue(DistrictDef.ContainsXZ(ring, 499f, -499f));
            Assert.IsFalse(DistrictDef.ContainsXZ(ring, 501f, 0f));
            Assert.IsFalse(DistrictDef.ContainsXZ(ring, 0f, 5000f));
        }

        [Test]
        public void Distance_IsZeroInsideAndEdgeDistanceOutside()
        {
            var ring = Square();
            Assert.AreEqual(0f, DistrictDef.DistanceXZ(ring, 0f, 0f), 1e-3f,
                "Icerideyken uzaklik 0 olmali.");
            Assert.AreEqual(300f, DistrictDef.DistanceXZ(ring, 800f, 0f), 1e-2f,
                "Kenara uzaklik yanlis.");
            // Kosegen disi: en yakin nokta kose
            Assert.AreEqual(Mathf.Sqrt(2f) * 300f,
                DistrictDef.DistanceXZ(ring, 800f, 800f), 1e-2f);
        }

        [Test]
        public void Distance_MeasuredToEdge_NotCentre()
        {
            // Ince uzun serit — Halic'in bicimi. Merkez uzakligi kullanilsaydi
            // seridin bir ucundayken obur ucu da "yakin" sayilirdi.
            var strip = new[]
            {
                new Vector2(-4000f, -100f), new Vector2(4000f, -100f),
                new Vector2(4000f, 100f), new Vector2(-4000f, 100f),
            };
            // Seridin bati ucunun 200 m kuzeyi
            float edge = DistrictDef.DistanceXZ(strip, -4000f, 300f);
            Assert.AreEqual(200f, edge, 1e-2f);

            float toCentre = new Vector2(-4000f, 300f).magnitude;
            Assert.Greater(toCentre, 3900f,
                "Merkez uzakligi 3,9 km'den buyuk; kenar uzakligi 200 m. " +
                "Ikisini karistirmak yayin esiklerini anlamsiz kilar.");
        }

        // ----------------------------------------------------------- histerezis

        [Test]
        public void Hysteresis_NoThrashWhenOscillatingOnBoundary()
        {
            // Yukleme esigi 700, bosaltma 910. Oyuncu 700 m'nin iki yanında
            // gidip gelirse: yuklenir ve YUKLU KALIR. Tek esikli bir sistemde
            // her salinim bir yukleme + bir bosaltma uretirdi.
            var def = MakeDef("D_Test", Square());
            var defs = new List<DistrictDef> { def };
            var loaded = new HashSet<string>();
            var toLoad = new List<DistrictDef>();
            var toUnload = new List<DistrictDef>();

            int loads = 0, unloads = 0;
            // Kare kenari 500; kenar uzakligi = |x| - 500.
            // x = 1150 -> 650 m (yukle), x = 1250 -> 750 m (histerezis bandi)
            for (int i = 0; i < 50; i++)
            {
                float x = (i % 2 == 0) ? 1150f : 1250f;
                DistrictStreamingPlan.Evaluate(defs, loaded, new Vector3(x, 0f, 0f),
                                               toLoad, toUnload);
                foreach (var d in toUnload) { loaded.Remove(d.districtId); unloads++; }
                foreach (var d in toLoad) { loaded.Add(d.districtId); loads++; }
            }

            Assert.AreEqual(1, loads, "Salinim sirasinda birden fazla yukleme oldu — titreme.");
            Assert.AreEqual(0, unloads, "Histerezis bandinda bosaltma olmamali.");
            Assert.IsTrue(loaded.Contains("D_Test"));
        }

        [Test]
        public void Hysteresis_UnloadsOnlyBeyondUnloadDistance()
        {
            var def = MakeDef("D_Test", Square());
            var defs = new List<DistrictDef> { def };
            var loaded = new HashSet<string> { "D_Test" };
            var toLoad = new List<DistrictDef>();
            var toUnload = new List<DistrictDef>();

            // kenar uzakligi 800 m — bosaltma esigi 910'un ALTINDA
            DistrictStreamingPlan.Evaluate(defs, loaded, new Vector3(1300f, 0f, 0f),
                                           toLoad, toUnload);
            Assert.IsEmpty(toUnload, "Histerezis bandinda erken bosaltma.");

            // kenar uzakligi 1000 m — esigin ustunde
            DistrictStreamingPlan.Evaluate(defs, loaded, new Vector3(1500f, 0f, 0f),
                                           toLoad, toUnload);
            Assert.AreEqual(1, toUnload.Count);
        }

        [Test]
        public void Evaluate_OrdersByPriorityThenDistance()
        {
            // Oncelik 1 uzakta, oncelik 2 yakinda: ucus ekseni ONCE yuklenmeli.
            var far = MakeDef("D_Far", Square(200f), priority: 1);
            var near = MakeDef("D_Near", Square(200f), priority: 2);
            for (int i = 0; i < far.boundary.Length; i++) far.boundary[i] += new Vector2(600f, 0f);

            var defs = new List<DistrictDef> { near, far };
            var toLoad = new List<DistrictDef>();
            var toUnload = new List<DistrictDef>();
            DistrictStreamingPlan.Evaluate(defs, new HashSet<string>(), Vector3.zero,
                                           toLoad, toUnload);

            Assert.AreEqual(2, toLoad.Count);
            Assert.AreEqual("D_Far", toLoad[0].districtId,
                "Oncelik-1 semt, daha yakin oncelik-2 semtten once yuklenmeli.");
        }

        [Test]
        public void Evaluate_DoesNotReloadAlreadyResident()
        {
            var def = MakeDef("D_Test", Square());
            var toLoad = new List<DistrictDef>();
            var toUnload = new List<DistrictDef>();
            DistrictStreamingPlan.Evaluate(new List<DistrictDef> { def },
                                           new HashSet<string> { "D_Test" },
                                           Vector3.zero, toLoad, toUnload);
            Assert.IsEmpty(toLoad);
            Assert.IsEmpty(toUnload);
        }

        // ------------------------------------------------------ ayrıştırıcı

        [Test]
        public void ImporterParser_ReadsNumbersWithInvariantCulture()
        {
            // Bu makinenin yerel ayari ondalik ayirici olarak VIRGUL kullaniyor.
            // Kulturden bagimsiz ayristirma olmasaydi 700.0 -> 7000 olurdu ve
            // yayin cemberi on kat buyurdu; hicbir hata mesaji cikmadan.
            const string body = "{\"id\": \"D_X\", \"priority\": 2, \"load_distance_m\": 700.5, " +
                                "\"kind\": \"water\", \"scene_address\": \"Districts/D_X\"}";
            Assert.AreEqual(700.5f, DistrictImporter.ReadFloat(body, "load_distance_m", -1f), 1e-3f);
            Assert.AreEqual(2, DistrictImporter.ReadInt(body, "priority", -1));
            Assert.AreEqual("water", DistrictImporter.ReadString(body, "kind"));
            Assert.AreEqual("Districts/D_X", DistrictImporter.ReadString(body, "scene_address"));
        }

        [Test]
        public void ImporterParser_MissingKeyFallsBackNotZero()
        {
            // Eksik anahtar SESSIZCE 0 donseydi yayin esigi 0 olur ve semt asla
            // yuklenmezdi — hicbir hata da uretmezdi.
            Assert.AreEqual(700f, DistrictImporter.ReadFloat("{\"id\":\"x\"}", "yok", 700f), 1e-3f);
        }

        [Test]
        public void ImporterParser_ExtractsCorrectFeatureBody()
        {
            const string json =
                "{\"features\":[{\"id\": \"D_A\", \"priority\": 1}," +
                "{\"id\": \"D_B\", \"priority\": 3}]}";
            string b = DistrictImporter.ExtractFeatureBody(json, "D_B");
            Assert.AreEqual(3, DistrictImporter.ReadInt(b, "priority", -1),
                "Yanlis ozellik govdesi kesildi.");
        }

        // --------------------------------------------- içe aktarılmış veri

        private static DistrictRegistry LoadRegistry()
        {
            return AssetDatabase.LoadAssetAtPath<DistrictRegistry>(DistrictImporter.RegistryPath);
        }

        [Test]
        public void Registry_ExistsAndCoversPlannedDistricts()
        {
            var reg = LoadRegistry();
            if (reg == null) Assert.Ignore("Semtler henuz ice aktarilmadi " +
                                           "(Hezarfen → GIS → Semtleri ice aktar).");

            // Plan Faz 1 madde 6'nin adiyla saydigi semtler
            foreach (string id in new[] { "D_Galata", "D_Surici_Dogu", "D_Surici_Bati",
                                          "D_Uskudar", "D_Halic", "D_Eyup" })
                Assert.IsNotNull(reg.Find(id), $"Plan'da adi gecen {id} kayitli degil.");
        }

        [Test]
        public void Registry_AddressesAreUniqueAndWellFormed()
        {
            var reg = LoadRegistry();
            if (reg == null) Assert.Ignore("Semtler henuz ice aktarilmadi.");

            var seen = new HashSet<string>();
            foreach (var d in reg.districts)
            {
                Assert.IsNotEmpty(d.sceneAddress, $"{d.districtId} adressiz.");
                Assert.IsTrue(d.sceneAddress.StartsWith("Districts/"),
                    $"{d.districtId} adresi 'Districts/' ile baslamiyor: {d.sceneAddress}");
                Assert.IsTrue(seen.Add(d.sceneAddress),
                    $"Cift adres: {d.sceneAddress} — biri digerini golgeler.");
            }
        }

        [Test]
        public void Registry_UnloadDistanceExceedsLoadDistance()
        {
            var reg = LoadRegistry();
            if (reg == null) Assert.Ignore("Semtler henuz ice aktarilmadi.");

            foreach (var d in reg.districts)
                Assert.Greater(d.unloadDistanceMeters, d.loadDistanceMeters,
                    $"{d.districtId}: bosaltma esigi yukleme esiginden buyuk degil — " +
                    "sinirda titreme kacinilmaz.");
        }

        [Test]
        public void Registry_BoundariesAreClosedPolygonsInsideWorld()
        {
            var reg = LoadRegistry();
            if (reg == null) Assert.Ignore("Semtler henuz ice aktarilmadi.");

            // Dunya ~15,3 km kenarli, orijin Galata; en uzak kose ~9,5 km.
            const float worldReach = 12000f;
            foreach (var d in reg.districts)
            {
                Assert.GreaterOrEqual(d.boundary.Length, 3,
                    $"{d.districtId} poligon degil ({d.boundary.Length} nokta).");
                Assert.AreNotEqual(d.boundary[0], d.boundary[d.boundary.Length - 1],
                    $"{d.districtId}: son nokta ilkiyle ayni — kapanis ORTUK olmali, " +
                    "tekrarlanan nokta sifir uzunlukta kenar uretir.");
                foreach (var p in d.boundary)
                    Assert.Less(p.magnitude, worldReach,
                        $"{d.districtId} sinir noktasi dunyanin disinda: {p}");
            }
        }

        [Test]
        public void FlightCorridor_IsCoveredByPriorityOneDistricts()
        {
            // Oyunun omurgasi: Galata Kulesi -> Dogancilar. Bu hat boyunca yuklu
            // olmayan bir bosluk, ucus ortasinda bos araziye bakmak demektir.
            var reg = LoadRegistry();
            if (reg == null) Assert.Ignore("Semtler henuz ice aktarilmadi.");

            var p1 = reg.districts.Where(d => d.priority == 1).ToList();
            Assert.IsNotEmpty(p1, "Hic oncelik-1 semt yok.");

            // Galata Kulesi = dunya orijini. Dogancilar olculen mesafede (GameUnits).
            Vector3 start = Vector3.zero;
            var dogancilar = reg.Find("D_Uskudar");
            Assert.IsNotNull(dogancilar, "D_Uskudar yok.");
            Vector3 end = new Vector3(dogancilar.center.x, 0f, dogancilar.center.y);

            int steps = 40;
            for (int i = 0; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(start, end, i / (float)steps);
                bool covered = p1.Any(d => d.Contains(p) ||
                                           d.DistanceMeters(p) <= d.loadDistanceMeters);
                Assert.IsTrue(covered,
                    $"Ucus koridorunda kapsanmayan nokta: {p} (adim {i}/{steps}).");
            }
        }

        [Test]
        public void Districts_AreTaggedAsGameRegionsNotHistoricalBoundaries()
        {
            // Bu poligonlar 1632 mahalle siniri DEGILDIR (ADR 0011). Ozet metninde
            // bunun yazili olmasi, sonradan yanlislikla T1/T2 muamelesi gormelerini
            // zorlastirir.
            var reg = LoadRegistry();
            if (reg == null) Assert.Ignore("Semtler henuz ice aktarilmadi.");

            foreach (var d in reg.districts)
                Assert.IsTrue(d.summary.Contains("mahalle siniri DEGIL") ||
                              d.summary.Contains("mahalle sınırı DEĞİL"),
                    $"{d.districtId} ozeti tarihsel-iddia-yok uyarisini tasimiyor.");
        }

        [Test]
        public void ExclusiveLandArea_IsNotDoubleCounted()
        {
            var reg = LoadRegistry();
            if (reg == null) Assert.Ignore("Semtler henuz ice aktarilmadi.");

            foreach (var d in reg.districts)
            {
                Assert.LessOrEqual(d.exclusiveLandAreaHectares, d.landAreaHectares + 0.05f,
                    $"{d.districtId}: tekil kara, toplam karadan buyuk olamaz.");
                if (d.kind == DistrictKind.Water)
                    Assert.AreEqual(0f, d.exclusiveLandAreaHectares, 0.05f,
                        $"{d.districtId} su bolgesi ama kara sahiplenmis — " +
                        "Faz 4 butcesi ayni araziyi iki kez sayar.");
            }

            // Capraz kontrol: suricinin iki yarisi, gercek tarihi yarimadaya (~1400 ha)
            // yakin olmali. Tutmuyorsa poligonlar kaymistir.
            var dogu = reg.Find("D_Surici_Dogu");
            var bati = reg.Find("D_Surici_Bati");
            if (dogu != null && bati != null)
            {
                float peninsula = dogu.exclusiveLandAreaHectares + bati.exclusiveLandAreaHectares;
                Assert.That(peninsula, Is.InRange(1100f, 1800f),
                    $"Suriçi toplami {peninsula:F0} ha — tarihi yarimada ~1400 ha. " +
                    "Bu araligin disi, poligonlarin kaydigini gosterir.");
            }
        }
    }
}
