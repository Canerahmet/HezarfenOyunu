using System.IO;
using Hezarfen.Core;
using Hezarfen.Editor.Gis;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hezarfen.Tests
{
    /// <summary>
    /// DEM → Unity Terrain sözleşmesini kilitler (plan Görev 9).
    ///
    /// Neden test: arazi, şehrin tüm koordinat sisteminin taşıyıcısıdır. Ölçek ya da
    /// orijin yanlışsa her landmark, her sokak ve her uçuş mesafesi yanlış olur — ve
    /// hata ancak binlerce varlık yerleştirildikten sonra fark edilir. Gözle bakınca
    /// yarım hücre kaymış bir İstanbul da İstanbul'a benzer.
    ///
    /// Testlerin çoğu DEM'in İÇERİĞİNDEN bağımsızdır: kaynak dosyayla Unity'deki
    /// arazi karşılaştırılır. Böylece DEM yeniden üretildiğinde (farklı yumuşatma,
    /// farklı çözünürlük) testler kırılmaz ama bozuk bir import yine yakalanır.
    /// </summary>
    public class TerrainPipelineTests
    {
        private const string DataDir = TerrainImporter.DefaultDataDir;
        private const string ScenePath = "Assets/_Project/Scenes/Faz1_Terrain.unity";

        private TerrainImporter.DemMeta meta;
        private TerrainData data;

        [SetUp]
        public void SetUp()
        {
            meta = TerrainImporter.ReadMeta(DataDir);
            if (meta == null)
                Assert.Ignore($"DEM verisi yok. Once: tools/gis/dem_fetch.py --out {DataDir}");

            data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainImporter.TerrainAssetPath);
            Assert.IsNotNull(data, $"TerrainData yok: {TerrainImporter.TerrainAssetPath}. " +
                                   "Menu: Hezarfen/GIS/DEM'den Terrain uret");
        }

        // ------------------------------------------------------------- ölçek

        [Test]
        public void Terrain_UsesRealWorldMetres()
        {
            // Sozlesmenin ta kendisi: 1 birim = 1 metre. Elle carpan YOK.
            Assert.AreEqual((float)meta.size_x_m, data.size.x, 0.5f, "X metre cinsinden olmali.");
            Assert.AreEqual((float)meta.size_z_m, data.size.z, 0.5f, "Z metre cinsinden olmali.");
            Assert.AreEqual((float)meta.height_range_m, data.size.y, 0.5f,
                "Y ekseni irtifa araligini metre olarak tasimali.");
        }

        [Test]
        public void Terrain_ResolutionMatchesSource()
        {
            Assert.AreEqual(meta.resolution, data.heightmapResolution);

            // Unity heightmap cozunurlugu 2^n+1 OLMAK ZORUNDA; degilse Unity sessizce
            // en yakin gecerli degere yuvarlar ve metre/ornek orani kayar.
            int r = data.heightmapResolution - 1;
            Assert.AreEqual(0, r & (r - 1), $"{data.heightmapResolution} bir 2^n+1 degeri degil.");
        }

        [Test]
        public void Terrain_SampleSpacingIsUniform()
        {
            // X ve Z'de farkli metre/ornek, detayin bir yonde ezilmesi demektir.
            float mx = data.size.x / (data.heightmapResolution - 1);
            float mz = data.size.z / (data.heightmapResolution - 1);

            Assert.AreEqual((float)meta.meters_per_sample_x, mx, 0.01f);
            Assert.AreEqual(mx, mz, 0.01f, "Ornek araligi X ve Z'de esit olmali (kare alan).");
        }

        // ------------------------------------------------------ import sadakati

        [Test]
        public void Terrain_MatchesSourceHeightmapExactly()
        {
            // Kaynak .r16 ile Unity'deki arazi birebir mi? Bu test DEM'in
            // ICERIGINDEN bagimsizdir: veri degisse de import bozuksa yakalar.
            string dir = TerrainImporter.ResolveDataDir(DataDir);
            string rawPath = Path.Combine(dir, meta.heightmap_file);
            Assert.IsTrue(File.Exists(rawPath), $"Kaynak heightmap yok: {rawPath}");

            byte[] bytes = File.ReadAllBytes(rawPath);
            int n = meta.resolution;
            Assert.AreEqual(n * n * 2, bytes.Length, "uint16 x N x N bekleniyordu.");

            float[,] heights = data.GetHeights(0, 0, n, n);

            // Kosegen + kenarlar: eksen takasi ve ayna hatalarini yakalayan ornekler.
            var probes = new (int x, int y, string what)[]
            {
                (0, 0, "guneybati kose"),
                (n - 1, 0, "guneydogu kose"),
                (0, n - 1, "kuzeybati kose"),
                (n - 1, n - 1, "kuzeydogu kose"),
                (n / 2, n / 2, "merkez"),
                (n / 4, 3 * n / 4, "ceyrek nokta"),
                (7, n - 13, "asimetrik nokta"),
            };

            // Tolerans: Unity arazi yuksekliklerini icerde 15-bit olarak saklar
            // (0..32766), 16-bit olarak degil. Yuvarlama adimi 1/32766 ~ 3,05e-5;
            // gidis-donus hatasi bunun yarisini asamaz. Daha sikisi olcum degil
            // temenni olur. Bu arazide 291 m / 32766 = ~9 mm dusey cozunurluk
            // demektir — oyun icin fazlasiyla yeterli.
            const float unityHeightQuantum = 1f / 32766f;
            foreach (var (x, y, what) in probes)
            {
                int i = (y * n + x) * 2;
                ushort raw = (ushort)(bytes[i] | (bytes[i + 1] << 8));
                Assert.AreEqual(raw / 65535f, heights[y, x], unityHeightQuantum,
                    $"{what} ({x},{y}) kaynakla eslesmiyor — satir sirasi ya da bayt duzeni bozuk.");
            }
        }

        [Test]
        public void Terrain_SeaLevelIsWorldZero()
        {
            // **y = 0 DENIZ SEVIYESIDIR.** Ucus oyununda irtifa okumasi buna dayanir.
            // Terrain nesnesi ise deniz TABANINA yerlestirilir — su duzlemi y=0'da
            // durdugu icin taban da 0 olsaydi iki yuzey cakisir, denizin derinligi
            // olmaz ve su ya hic gorunmez ya z-fighting yapardi. (Bu yasandi:
            // ilk Faz 1 karesinde Bogaz'in yerinde kara rengi duz zemin vardi.)
            Assert.AreEqual(0.0, meta.sea_level_m, 1e-6, "Deniz seviyesi 0 olmali.");
            Assert.Greater(meta.seabed_depth_m, 0.0, "Denizin derinligi olmali.");
            Assert.AreEqual(-meta.seabed_depth_m, meta.base_elevation_m, 1e-6,
                "Terrain tabani = deniz seviyesi - derinlik.");

            float[,] h = data.GetHeights(0, 0, data.heightmapResolution, data.heightmapResolution);
            float min = float.MaxValue;
            foreach (float v in h) if (v < min) min = v;

            // En dusuk nokta tam taban kotunda; DUNYA uzayinda -seabed_depth eder.
            Assert.AreEqual(0f, min, 1e-4f, "Heightmap'in en dusugu tabana oturmali.");
            double worldMin = meta.base_elevation_m + min * meta.height_range_m;
            Assert.AreEqual(-meta.seabed_depth_m, worldMin, 0.05,
                "Deniz tabani dunya uzayinda -derinlik kotunda olmali.");
        }

        [Test]
        public void Terrain_LandStaysAboveSeaLevel()
        {
            // Deniz tabani asagi indirilirken kara kotlarinin KAYMAMIS olmasi sart.
            // Galata Kulesi orijindedir ve ~52 m olcumustu (ADR 0007).
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

                float galata = terrain.SampleHeight(Vector3.zero) + terrain.transform.position.y;
                Assert.AreEqual(52f, galata, 2f,
                    $"Galata sirti {galata:F1} m — taban kaydirmasi kara kotlarini bozmus.");

                // Bogaz ortasi: deniz tabani kotunda olmali
                float bogaz = terrain.SampleHeight(new Vector3(4249f, 0f, 1693f))
                              + terrain.transform.position.y;
                Assert.AreEqual(-(float)meta.seabed_depth_m, bogaz, 0.5f,
                    $"Bogaz tabani {bogaz:F1} m olmali degil.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        // ------------------------------------------------------ georeferans

        [Test]
        public void Terrain_PlacedAtGalataOrigin()
        {
            // Plan Faz 1 madde 4: dunya orijini Galata Kulesi tabanidir.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Terrain terrain = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    terrain = root.GetComponent<Terrain>();
                    if (terrain != null) break;
                }
                Assert.IsNotNull(terrain, $"{ScenePath} icinde Terrain yok.");

                Vector3 pos = terrain.transform.position;
                Assert.AreEqual((float)meta.world_origin_offset_m.x, pos.x, 0.5f);
                Assert.AreEqual((float)meta.base_elevation_m, pos.y, 0.5f);
                Assert.AreEqual((float)meta.world_origin_offset_m.z, pos.z, 0.5f);

                // Dunya orijini (0,0) arazinin ICINDE olmali; disindaysa Galata
                // Kulesi haritanin disina dusmus demektir.
                Assert.Less(pos.x, 0f, "Galata orijini arazinin dogusunda kalmali.");
                Assert.Less(pos.z, 0f, "Galata orijini arazinin kuzeyinde kalmali.");
                Assert.Greater(pos.x + data.size.x, 0f, "Orijin arazinin dogu kenarini asmis.");
                Assert.Greater(pos.z + data.size.z, 0f, "Orijin arazinin kuzey kenarini asmis.");

                // Galata Kulesi tabani suyun uzerinde bir sirttir; 0 cikarsa
                // arazi denize kaymis demektir.
                float atOrigin = terrain.SampleHeight(Vector3.zero);
                Assert.Greater(atOrigin, 20f,
                    $"Dunya orijininde arazi {atOrigin:F1} m — Galata sirti burada olmali.");
                Assert.Less(atOrigin, 90f, "Galata sirti icin fazla yuksek.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void Terrain_HasHistoricalTagWithSource()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                // ARAZININ etiketi aranir. Sahnede baska etiketli nesneler de var
                // (su yuzeyi, GIS ogeleri); "ilk buldugunu al" demek, arazinin
                // etiketi silinse bile testin yesil kalmasi demekti.
                HistoricalTag tag = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    var terrain = root.GetComponentInChildren<Terrain>(true);
                    if (terrain == null) continue;
                    tag = terrain.GetComponent<HistoricalTag>();
                    break;
                }

                Assert.IsNotNull(tag, "Arazi HistoricalTag tasimali (CLAUDE.md).");
                Assert.IsTrue(tag.IsValid);
                Assert.AreEqual(HistoricalTier.Reconstruction, tag.tier,
                    "Modern topografya belge degil rekonstruksiyon dayanagidir.");

                // Copernicus DEM lisansi ATIF ZORUNLU kilar. Not kaybolursa
                // atif yukumlulugu de sessizce kaybolur.
                StringAssert.Contains("Copernicus", tag.sourceNote);
                StringAssert.Contains("Atif zorunlu", tag.sourceNote);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void DemMeta_RecordsAttributionObligation()
        {
            Assert.IsTrue(meta.attribution_required,
                "Copernicus DEM atif zorunlulugu meta'da kayitli olmali.");
            Assert.IsNotEmpty(meta.licence);
            StringAssert.Contains("Copernicus", meta.source);
            StringAssert.Contains("DSM", meta.source_type,
                "Kaynagin DSM oldugu (ciplak zemin degil) kayitli kalmali.");
        }
    }
}
