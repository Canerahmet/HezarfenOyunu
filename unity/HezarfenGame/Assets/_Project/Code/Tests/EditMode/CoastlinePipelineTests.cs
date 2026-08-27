using System.Collections.Generic;
using System.IO;
using Hezarfen.Core;
using Hezarfen.Editor.Gis;
using Hezarfen.Gis;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Kıyı çizgisi taslağı ve GeoJSON→sahne aktarımı (plan Görev 10).
    ///
    /// İki ayrı şeyi korur:
    ///   1. **Ayrıştırıcı doğruluğu.** Elle yazılmış JSON okuyucusunun sessizce sıfır
    ///      üretmemesi. Bu tam olarak bir kez oldu ve tüm geometri orijine çöktü.
    ///   2. **Kıyı–arazi tutarlılığı.** Kıyı çizgisi ile arazi aynı kaynaktan gelir;
    ///      ayrışırlarsa deniz düzlemi karayı keser.
    /// </summary>
    public class CoastlinePipelineTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Faz1_Terrain.unity";
        private const string GeoJsonPath = "refs/maps/coastline_1632.geojson";

        // -------------------------------------------------- ayrıştırıcı birim testi

        [Test]
        public void Parser_SkipsWhitespaceAfterColon()
        {
            // REGRESYON: Python'un json.dump'i varsayilan olarak `"x": -6306.5` yazar
            // (ayirici ": "). Bosluk atlanmazsa sayi taramasi hemen durur, deger
            // sessizce 0 olur ve BUTUN geometri orijine coker. Sessiz olmasi,
            // bu testin var olma sebebi.
            const string json = @"{""features"":[{""layer"":""modern_shoreline"",""id"":"""",
                ""name"":""x"",""tier"":""Reconstruction"",""action"":"""",""note"":""n"",
                ""closed"":false,""rings"":[[{""x"": -6306.5, ""z"": -8260.25},
                {""x"": 120.0, ""z"": -7.5}]]}]}";

            var parsed = GeoJsonImporter.ParseLocal(json);
            Assert.AreEqual(1, parsed.features.Count);
            Assert.AreEqual(1, parsed.features[0].rings.Count);

            var ring = parsed.features[0].rings[0];
            Assert.AreEqual(2, ring.Count);
            Assert.AreEqual(-6306.5f, ring[0].x, 1e-3f, "Bosluk atlanmiyor — deger sifira dustu.");
            Assert.AreEqual(-8260.25f, ring[0].z, 1e-3f);
            Assert.AreEqual(120.0f, ring[1].x, 1e-3f);
            Assert.AreEqual(-7.5f, ring[1].z, 1e-3f);
        }

        [Test]
        public void Parser_UsesInvariantCultureForDecimals()
        {
            // Bu makinenin yerel ayari ondalik ayirici olarak VIRGUL kullaniyor.
            // Kulturden bagimsiz ayristirma olmazsa "-6306.5" degeri -63065 olur:
            // kiyi cizgisi on kat buyuyup dunyanin disina tasar.
            const string json =
                @"{""features"":[{""layer"":""l"",""rings"":[[{""x"": -6306.5, ""z"": 0.125}]]}]}";

            var ring = GeoJsonImporter.ParseLocal(json).features[0].rings[0];
            Assert.AreEqual(-6306.5f, ring[0].x, 1e-3f);
            Assert.AreEqual(0.125f, ring[0].z, 1e-4f);
        }

        [Test]
        public void Parser_ReadsFeatureMetadata()
        {
            const string json = @"{""features"":[{""layer"":""correction_zone"",
                ""id"":""CZ_Test"",""name"":""Deneme"",""tier"":""Legend"",
                ""action"":""remove_fill"",""note"":""aciklama"",""closed"":true,
                ""rings"":[[{""x"": 1.0, ""z"": 2.0}]]}]}";

            var f = GeoJsonImporter.ParseLocal(json).features[0];
            Assert.AreEqual("correction_zone", f.layer);
            Assert.AreEqual("CZ_Test", f.id);
            Assert.AreEqual("Deneme", f.name);
            Assert.AreEqual("remove_fill", f.action);
            Assert.AreEqual("aciklama", f.note);
            Assert.IsTrue(f.closed);
        }

        // ------------------------------------------------------------- artefakt

        [Test]
        public void GeoJson_IsPresentAndDeclaredDraft()
        {
            string repo = TerrainImporter.RepositoryRoot();
            Assert.IsNotNull(repo);

            string path = Path.Combine(repo, GeoJsonPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(path), $"{GeoJsonPath} yok. Once: tools/gis/coastline_build.py");

            string text = File.ReadAllText(path);
            StringAssert.Contains("FeatureCollection", text);
            StringAssert.Contains("modern_shoreline", text);
            StringAssert.Contains("shoreline_1632", text);
            StringAssert.Contains("correction_zone", text);

            // Yontem ve kararin KAYITLI olmasi sart: dosya elden ele gectiginde
            // "bu 1632 kiyisi nereden geldi" sorusu dosyanin kendisinden cevaplanmali.
            StringAssert.Contains("corrections_method", text);
            StringAssert.Contains("measured_shift_m", text);

            // Belge TASLAK oldugunu KENDI icinde soylemeli. Dosya elden ele
            // gectiginde bu uyari yaninda gitmezse, taslak veri belgelenmis
            // gercek gibi kullanilir.
            StringAssert.Contains("TASLAK", text);
            StringAssert.Contains("metrik ofset", text);
        }

        // ---------------------------------------------------------------- sahne

        /// <summary>Bir düzeltme alanının sınırları içinde kalan kıyı uzunluğu (m).</summary>
        private static float ShorelineLengthInside(List<GisFeature> features, string layer,
                                                   GisFeature zone)
        {
            var box = new Bounds(zone.points[0], Vector3.zero);
            foreach (var p in zone.points) box.Encapsulate(p);

            float total = 0f;
            foreach (var f in features)
            {
                if (f.layer != layer) continue;
                for (int i = 1; i < f.points.Length; i++)
                {
                    Vector3 a = f.points[i - 1], b = f.points[i];
                    bool inA = a.x >= box.min.x && a.x <= box.max.x && a.z >= box.min.z && a.z <= box.max.z;
                    bool inB = b.x >= box.min.x && b.x <= box.max.x && b.z >= box.min.z && b.z <= box.max.z;
                    if (inA || inB) total += Vector3.Distance(a, b);
                }
            }
            return total;
        }

        private static List<GisFeature> LoadSceneFeatures(Scene scene)
        {
            var found = new List<GisFeature>();
            foreach (var root in scene.GetRootGameObjects())
                found.AddRange(root.GetComponentsInChildren<GisFeature>(true));
            return found;
        }

        [Test]
        public void Scene_ContainsShorelineAndZones()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var features = LoadSceneFeatures(scene);
                Assert.IsNotEmpty(features, $"{ScenePath} icinde GisFeature yok.");

                // Katmanlar TURLERINE gore ayrilir; "landmark disindaki her sey
                // cizgidir" varsayimi bir kez kirildi: sur kapilari sahneye
                // girdiginde (ADR 0011) onlar da tek noktadir ve test patladi.
                // Simdi nokta katmanlari ACIKCA listeleniyor ve tek nokta OLDUKLARI
                // da dogrulaniyor — ters hata (kapinin cizgiye donusmesi) de yakalanir.
                var pointLayers = new HashSet<string> { "landmark", "wall_gate" };

                int shorelines = 0, zones = 0, lines = 0;
                float km = 0f;
                foreach (var f in features)
                {
                    if (pointLayers.Contains(f.layer))
                    {
                        Assert.AreEqual(1, f.points.Length,
                            $"'{f.name}' nokta katmaninda ({f.layer}) ama {f.points.Length} noktali.");
                        continue;
                    }

                    Assert.Greater(f.points.Length, 1, $"'{f.name}' tek noktali.");

                    if (f.layer == "correction_zone") zones++;
                    else if (f.layer == "district" || f.layer.StartsWith("wall_")) lines++;
                    else { shorelines++; km += f.LengthMeters / 1000f; }
                }

                Assert.Greater(shorelines, 0, "Kiyi halkasi yok.");
                Assert.GreaterOrEqual(zones, 3, "RESEARCH.md en az uc duzeltme alani gerektiriyor.");
                // Uzunluk YALNIZCA kiyi katmanindan olculur. Onceden sur hatlari da
                // bu toplama giriyordu; kiyinin kendi uzunlugunu olcmek varken
                // baska katmanlarin karismasi olcuyu kor eder.
                Assert.Greater(km, 20f, $"Kiyi cizgisi cok kisa ({km:F1} km) — ayristirma bozuk olabilir.");
                Assert.Less(km, 300f, $"Kiyi cizgisi cok uzun ({km:F1} km) — birim hatasi olabilir.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void Shoreline_AgreesWithTerrainWaterline()
        {
            // Kiyi cizgisi ile arazi AYNI kaynaktan gelir (DEM). Ayrisirlarsa deniz
            // duzlemi karayi keser ya da kiyida gorunmez bir ucurum kalir — oyuncu
            // suyun ustunde uctugu icin bu dogrudan gorunur.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Terrain terrain = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    terrain = root.GetComponentInChildren<Terrain>(true);
                    if (terrain != null) break;
                }
                Assert.IsNotNull(terrain, "Sahnede Terrain yok.");

                // Tolerans VERIDEN turer, sihirli sayi degil: kiyi hucresinde deniz
                // tabani ile kara arasinda `seabed_depth` kadar bir basamak vardir
                // (7,5 m'lik hucrede 12 m). Bilineer ornekleme bir tepe noktasini o
                // basamagin ortasina dusurebilir, yani fiziksel sapma siniri
                // derinligin yarisidir. Daha sikisi olcum degil temenni olurdu.
                var dem = TerrainImporter.ReadMeta(TerrainImporter.DefaultDataDir);
                float tolerance = dem != null ? (float)dem.seabed_depth_m * 0.5f : 6f;

                int sampled = 0, onWaterline = 0;
                float worst = 0f, absSum = 0f;
                foreach (var f in LoadSceneFeatures(scene))
                {
                    // YALNIZCA modern kiyi: 1632 kiyisi dolgular geri alindigi icin
                    // bilerek bugunku KARANIN uzerindedir (asagidaki ayri test).
                    if (f.layer != "modern_shoreline") continue;
                    for (int i = 0; i < f.points.Length; i += 3)
                    {
                        // SampleHeight TERRAIN-YEREL yukseklik verir. Arazi deniz
                        // tabanina (-derinlik) yerlestirildigi icin dunya kotu icin
                        // nesnenin y'si EKLENMELIDIR; eklenmezse butun kiyi
                        // "derinlik kadar yuksekte" gorunur ve test yanlis kirilir.
                        float h = terrain.SampleHeight(f.transform.TransformPoint(f.points[i]))
                                  + terrain.transform.position.y;
                        sampled++;
                        absSum += Mathf.Abs(h);
                        if (Mathf.Abs(h) <= tolerance) onWaterline++;
                        if (Mathf.Abs(h) > worst) worst = Mathf.Abs(h);
                    }
                }

                Assert.Greater(sampled, 20, "Yeterli ornek yok.");
                Assert.GreaterOrEqual(onWaterline / (float)sampled, 0.95f,
                    $"Kiyi noktalarinin yalnizca {onWaterline}/{sampled}'i su hattinda " +
                    $"(+/-{tolerance:F1} m, en kotu {worst:F2} m). Kiyi ile arazi ayrismis.");

                // Ortalama, tek tek sapmalardan daha saglam bir gostergedir:
                // kiyi 100 m kaymis olsaydi ortalama on metrelere cikardi.
                float mean = absSum / sampled;
                Assert.Less(mean, 2.0f,
                    $"Kiyi noktalarinin ortalama |kot| sapmasi {mean:F2} m — cok yuksek.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void Shoreline1632_IsInlandOfModern()
        {
            // Dolgular geri alindiginda 1632 kiyisi BUGUNKU KARANIN uzerine duser.
            // Yon yanlis olsaydi (kiyi denize dogru genislemis olsaydi) ortalama
            // yukseklik modernden DUSUK cikardi.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Terrain terrain = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    terrain = root.GetComponentInChildren<Terrain>(true);
                    if (terrain != null) break;
                }
                Assert.IsNotNull(terrain);

                float modernSum = 0f, oldSum = 0f;
                int modernN = 0, oldN = 0;
                foreach (var f in LoadSceneFeatures(scene))
                {
                    bool isOld = f.layer == "shoreline_1632";
                    if (!isOld && f.layer != "modern_shoreline") continue;

                    for (int i = 0; i < f.points.Length; i += 2)
                    {
                        float h = terrain.SampleHeight(f.transform.TransformPoint(f.points[i]));
                        if (isOld) { oldSum += h; oldN++; } else { modernSum += h; modernN++; }
                    }
                }

                Assert.Greater(oldN, 0, "shoreline_1632 katmani yok.");
                Assert.Greater(modernN, 0, "modern_shoreline katmani yok.");
                Assert.Greater(oldSum / oldN, modernSum / modernN,
                    "1632 kiyisi modern kiyidan daha ICERIDE olmali (daha yuksek arazide).");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void Langa_IsNotFlooded()
        {
            // REGRESYON: Langa/Vlanga alani, Marmara duzeltme alaninin TAMAMEN
            // icindedir. Esik alani yalnizca maksimum alsaydi, Marmara'nin 5 m
            // esigi Langa'nin acik muafiyetini ezer ve 1632'de BOSTAN olan dolmus
            // limani yeniden denize cevirirdi. (Bu yasandi; onizlemede Langa
            // kutusunun icinde kapali bir su halkasi belirdi.)
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var features = LoadSceneFeatures(scene);

                GisFeature langa = null;
                foreach (var f in features)
                    if (f.action == "convert_to_gardens") { langa = f; break; }
                Assert.IsNotNull(langa, "Langa alani yok — bostan isareti kaybolmus.");

                // "Icinden kiyi gecmesin" demek yanlis olurdu: Langa kutusunun guney
                // kenari GERCEK Marmara kiyisini kesiyor, oradan kiyi gecmesi normal.
                // Dogru soru: 1632 kiyisi Langa'ya YENI bir koy oydu mu? Su basmis
                // olsaydi alanin icinde kapali bir halka belirir ve kutu icindeki
                // kiyi uzunlugu sicrardi.
                float modern = ShorelineLengthInside(features, "modern_shoreline", langa);
                float old1632 = ShorelineLengthInside(features, "shoreline_1632", langa);

                Assert.Greater(modern, 100f, "Langa kutusunda kiyi yok — alan yanlis yerde.");
                Assert.Less(Mathf.Abs(old1632 - modern) / modern, 0.20f,
                    $"Langa kutusunda kiyi uzunlugu {modern:F0} m -> {old1632:F0} m degisti. " +
                    "Dolmus liman yeniden denize cevrilmis olabilir.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void FillZones_ActuallyMovedTheShoreline()
        {
            // Langa testinin ikizi. O, duzeltmenin FAZLA is yapmadigini korur;
            // bu, HIC is yapmadigini yakalar. Ikisi olmadan, sessizce devre disi
            // kalmis bir duzeltme "yesil testler" arkasinda fark edilmeden gecerdi.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var features = LoadSceneFeatures(scene);
                int changed = 0, checkedZones = 0;

                foreach (var zone in features)
                {
                    if (zone.action != "remove_fill") continue;

                    float modern = ShorelineLengthInside(features, "modern_shoreline", zone);
                    float old1632 = ShorelineLengthInside(features, "shoreline_1632", zone);
                    if (modern < 100f) continue;

                    checkedZones++;
                    if (Mathf.Abs(old1632 - modern) / modern > 0.03f) changed++;
                }

                Assert.GreaterOrEqual(checkedZones, 3, "Dolgu alani bulunamadi.");
                Assert.GreaterOrEqual(changed, 2,
                    $"{checkedZones} dolgu alanindan yalnizca {changed} tanesinde kiyi degisti. " +
                    "Dolgu geri alma calismiyor olabilir.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void CorrectionZones_AreMarkedAsDraft()
        {
            // Duzeltme alanlarinin metrik dayanagi YOK. Etiket bunu soylemezse
            // taslak geometri, belgelenmis tarih gibi kullanilir — oyunun kimligi
            // tam olarak bu ayrimin durustlugunde (plan Bolum 2).
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                int zones = 0;
                foreach (var f in LoadSceneFeatures(scene))
                {
                    if (f.layer != "correction_zone") continue;
                    zones++;

                    var tag = f.GetComponent<HistoricalTag>();
                    Assert.IsNotNull(tag, $"'{f.featureName}' HistoricalTag tasimiyor.");
                    Assert.IsTrue(tag.IsValid);
                    Assert.AreNotEqual(HistoricalTier.Documented, tag.tier,
                        $"'{f.featureName}' BELGELI olamaz: metrik ofset kaynakli degil.");
                    StringAssert.Contains("TASLAK", tag.sourceNote,
                        $"'{f.featureName}' notu taslak oldugunu soylemiyor.");
                }

                Assert.GreaterOrEqual(zones, 3);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void Features_StayInsideWorldBounds()
        {
            var meta = TerrainImporter.ReadMeta(TerrainImporter.DefaultDataDir);
            if (meta == null) Assert.Ignore("DEM verisi yok.");

            float minX = (float)meta.world_origin_offset_m.x;
            float minZ = (float)meta.world_origin_offset_m.z;
            float maxX = minX + (float)meta.size_x_m;
            float maxZ = minZ + (float)meta.size_z_m;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (var f in LoadSceneFeatures(scene))
                {
                    foreach (var p in f.points)
                    {
                        Assert.GreaterOrEqual(p.x, minX - 1f, $"'{f.name}' dunyanin batisina tasti.");
                        Assert.LessOrEqual(p.x, maxX + 1f, $"'{f.name}' dunyanin dogusuna tasti.");
                        Assert.GreaterOrEqual(p.z, minZ - 1f, $"'{f.name}' dunyanin guneyine tasti.");
                        Assert.LessOrEqual(p.z, maxZ + 1f, $"'{f.name}' dunyanin kuzeyine tasti.");
                        Assert.AreEqual(0f, p.y, 1e-4f, "Kiyi noktalari deniz seviyesinde olmali.");
                    }
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
