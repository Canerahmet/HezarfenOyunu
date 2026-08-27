using Hezarfen.Editor.Gis;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Arazi örtüsünün <b>kuralını</b> kilitler, görüntüsünü değil.
    ///
    /// Örtünün nasıl göründüğü sanatsal bir karardır ve değişebilir; nereye
    /// düştüğü ise ölçülebilir bir iddiadır: kaya dik yamaçta olur, kıyı deniz
    /// seviyesinde olur, hiçbir katman ölü durmaz. Test bunları sınar.
    ///
    /// Neden gerek: splatmap 4,2 milyon texel'dir ve gözle denetlenemez.
    /// Kuralın işaretini ters yazmak (kayayı DÜZ yere koymak gibi) uzaktan
    /// bakınca "dokulu arazi" olarak görünür — hata ancak yamaca yaklaşınca
    /// fark edilir, o da fark edilirse.
    /// </summary>
    public class TerrainCoverTests
    {
        private TerrainData data;
        private TerrainImporter.DemMeta meta;

        [SetUp]
        public void SetUp()
        {
            meta = TerrainImporter.ReadMeta(TerrainImporter.DefaultDataDir);
            if (meta == null) Assert.Ignore("DEM verisi yok.");

            data = AssetDatabase.LoadAssetAtPath<TerrainData>(
                TerrainImporter.TerrainAssetPath);
            Assert.IsNotNull(data, "TerrainData yok — Hezarfen/GIS/DEM'den Terrain uret");

            if (data.terrainLayers == null || data.terrainLayers.Length == 0)
                Assert.Ignore("Arazi ortusu kurulmamis — Hezarfen/GIS/Arazi ortusunu kur");
        }

        // ------------------------------------------------------------ katman

        [Test]
        public void Cover_HasExactlyFourLayers()
        {
            // Dort, bedava olan sinir: splatmap RGBA'dir, besinci katman
            // ikinci bir doku ve ikinci bir ornekleme demek.
            Assert.AreEqual(4, data.terrainLayers.Length,
                "Splatmap RGBA tasir; dort katman tek dokuya sigar.");
        }

        [Test]
        public void Layers_TileInMetres()
        {
            // 1 birim = 1 metre sozlesmesi katmanlara da uygulanir. Karo
            // olcusu bildirimde yazar; iki taraf ayrisirsa zeminin olcegi
            // sessizce yanlis olur ve kimse fark etmez.
            var man = TerrainCoverBuilder.ReadManifest();
            Assert.IsNotNull(man, "terrain_layers.json okunamadi.");
            Assert.AreEqual(man.layers.Length, data.terrainLayers.Length);

            for (int i = 0; i < man.layers.Length; i++)
            {
                var spec = man.layers[i];
                var layer = data.terrainLayers[i];
                StringAssert.Contains(spec.name, layer.name,
                    $"{i}. katman bildirimdeki sirayla eslesmiyor.");
                Assert.AreEqual(spec.sizeMeters, layer.tileSize.x, 0.01f,
                    $"{spec.name}: karo olcusu metre cinsinden bildirimden gelmeli.");
                Assert.AreEqual(layer.tileSize.x, layer.tileSize.y, 0.01f,
                    $"{spec.name}: karo kare olmali.");
            }
        }

        [Test]
        public void NoStaleLayerAssetsLeftBehind()
        {
            // Katman adi degistiginde (DryGrass -> Grass) eski `.terrainlayer`
            // dosyasi klasorde kalir ve dokulari silinmis oldugu icin KIRIK
            // bir varlik olur. Kimse ona bakmadigi surece sessizdir.
            var live = new System.Collections.Generic.HashSet<string>();
            foreach (var l in data.terrainLayers) live.Add(l.name);

            foreach (string guid in AssetDatabase.FindAssets(
                         "t:TerrainLayer", new[] { TerrainCoverBuilder.LayerFolder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                string n = System.IO.Path.GetFileNameWithoutExtension(p);
                if (!n.StartsWith("TL_Terrain")) continue;   // TL_Graybox baskasinin
                Assert.IsTrue(live.Contains(n),
                    $"{p} artik hicbir yerde kullanilmiyor — dokulari silinmisse " +
                    "kirik bir varliktir. 'Arazi ortusunu kur' temizler.");
            }
        }

        [Test]
        public void Layers_CarryAllThreeMaps()
        {
            foreach (var l in data.terrainLayers)
            {
                Assert.IsNotNull(l.diffuseTexture, $"{l.name}: taban renk yok.");
                Assert.IsNotNull(l.normalMapTexture, $"{l.name}: normal yok.");
                Assert.IsNotNull(l.maskMapTexture, $"{l.name}: maske yok — " +
                    "yukseklik harmani maskenin B kanalini okur, maskesiz katman " +
                    "sinirda hep kaybeder.");

                // Maske RENK dokusu olarak okunursa purzuluk ve AO sessizce
                // yanlis olur: yuzey ne hata verir ne eksik gorunur, yalnizca
                // yanlis parlar.
                string p = AssetDatabase.GetAssetPath(l.maskMapTexture);
                var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                Assert.IsNotNull(imp);
                Assert.IsFalse(imp.sRGBTexture, $"{l.name}: maske sRGB isaretli.");
            }
        }

        // ------------------------------------------------------------ dağılım

        [Test]
        public void Alphamap_WeightsSumToOne()
        {
            var (map, res, n) = Sample();
            for (int j = 0; j < res; j += 37)
                for (int i = 0; i < res; i += 41)
                {
                    float s = 0f;
                    for (int k = 0; k < n; k++) s += map[j, i, k];
                    Assert.AreEqual(1f, s, 0.01f,
                        $"({i},{j}) agirlik toplami {s:F3} — kalan butceden " +
                        "dagitim bozulmus.");
                }
        }

        [Test]
        public void EveryLayerActuallyCoversGround()
        {
            // Olu bir katman bosa bellek ve sessiz bir hatadir: kural yanlis
            // yazildiginda katman yuklenir, gorunur, ama hicbir yerde cizilmez.
            var (map, res, n) = Sample();
            var share = new double[n];
            int count = 0;
            for (int j = 0; j < res; j += 7)
                for (int i = 0; i < res; i += 7)
                {
                    for (int k = 0; k < n; k++) share[k] += map[j, i, k];
                    count++;
                }

            for (int k = 0; k < n; k++)
                Assert.Greater(share[k] / count, 0.01,
                    $"{data.terrainLayers[k].name} arazinin %1'inden azini kapliyor.");
        }

        [Test]
        public void RockOnSteepSlopes_NotOnFlatGround()
        {
            // ASIL iddia: kaya EGIMLE gelir.
            //
            // Esik SABIT ACI OLARAK yazilamaz. Ilk yazimda "45 derece ustu
            // dik" diyordu ve arazinin karasinin yalnizca %0,01'i oraya
            // giriyor — orneklem bos kaliyordu. Sebep jeoloji degil olcek:
            // 7,5 m ornek arali bir DEM'de egim 15 m tabanla olculur ve
            // gercek bir kaya yari ortalamayla silinir (karada p99 = 24°).
            //
            // Bu yuzden test de uretici de ayni sekilde konusur: esikler
            // arazinin KENDI dagilimindan gelir. DEM yeniden uretilirse ikisi
            // birlikte kayar; sabit aci yazan taraf sessizce yanilirdi.
            var (map, res, _) = Sample();
            int hm = data.heightmapResolution;
            float[,] h = data.GetHeights(0, 0, hm, hm);
            float mps = data.size.x / (hm - 1);
            float b = (float)meta.base_elevation_m;

            var q = TerrainCoverBuilder.SlopeQuantiles(h, hm, mps, data.size.y, b,
                                                       0.50f, 0.995f);
            float mid = q[0], steepCut = q[1];

            double steepSum = 0, flatSum = 0;
            int steepN = 0, flatN = 0;
            const int rockLayer = 2;

            for (int j = 2; j < res - 2; j += 5)
                for (int i = 2; i < res - 2; i += 5)
                {
                    int hx = Mathf.Clamp(Mathf.RoundToInt((i + 0.5f) / res * (hm - 1)), 1, hm - 2);
                    int hy = Mathf.Clamp(Mathf.RoundToInt((j + 0.5f) / res * (hm - 1)), 1, hm - 2);
                    if (b + h[hy, hx] * data.size.y < 0.5f) continue;   // deniz tabani

                    float dx = (h[hy, hx + 1] - h[hy, hx - 1]) * data.size.y / (2 * mps);
                    float dz = (h[hy + 1, hx] - h[hy - 1, hx]) * data.size.y / (2 * mps);
                    float slope = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;

                    if (slope > steepCut) { steepSum += map[j, i, rockLayer]; steepN++; }
                    else if (slope < mid) { flatSum += map[j, i, rockLayer]; flatN++; }
                }

            Assert.Greater(steepN, 30, "Ornekte yeterince dik kara yamaci yok.");
            Assert.Greater(flatN, 200, "Ornekte yeterince yumusak kara yok.");

            double steep = steepSum / steepN, flat = flatSum / flatN;
            Assert.Greater(steep, 0.50,
                $"Karanin en dik %0,5'inde kaya payi {steep:F2} — az " +
                $"(esik {steepCut:F1} derece).");
            Assert.Less(flat, 0.05,
                $"Ortancanin altindaki egimde kaya payi {flat:F2} — kaya " +
                "yumusak araziye tasmis.");

            // "Gecti" ile "dogru" ayni sey degil: ayirt etme gucunu de olc.
            Assert.Greater(steep - flat, 0.45,
                $"Kural ayirt etmiyor: dik {steep:F2}, yumusak {flat:F2}.");
        }

        [Test]
        public void ShoreHugsSeaLevel()
        {
            var (map, res, n) = Sample();
            int hm = data.heightmapResolution;
            float[,] h = data.GetHeights(0, 0, hm, hm);
            float b = (float)meta.base_elevation_m;

            double lowSum = 0, highSum = 0;
            int lowN = 0, highN = 0;
            const int shoreLayer = 3;

            for (int j = 0; j < res; j += 5)
                for (int i = 0; i < res; i += 5)
                {
                    int hx = Mathf.Clamp(Mathf.RoundToInt((i + 0.5f) / res * (hm - 1)), 0, hm - 1);
                    int hy = Mathf.Clamp(Mathf.RoundToInt((j + 0.5f) / res * (hm - 1)), 0, hm - 1);
                    float elev = b + h[hy, hx] * data.size.y;

                    if (elev < -2f) { lowSum += map[j, i, shoreLayer]; lowN++; }
                    else if (elev > 25f) { highSum += map[j, i, shoreLayer]; highN++; }
                }

            Assert.Greater(lowN, 50);
            Assert.Greater(highN, 50);
            Assert.Greater(lowSum / lowN, 0.90,
                "Deniz tabani kiyi katmaniyla kaplanmali.");
            Assert.Less(highSum / highN, 0.02,
                "25 m kotta kiyi kumu olmamali — kural kota degil baska bir " +
                "seye baglanmis.");
        }

        [Test]
        public void SplatResolutionMatchesSourceSampling()
        {
            // Ortu, DEM'in kendi cozunurlugunden ince olmamali: olmayan bir
            // bilgiyi taklit etmek olurdu.
            Assert.AreEqual(TerrainCoverBuilder.AlphamapResolution,
                            data.alphamapResolution);
            float mPerTexel = data.size.x / data.alphamapResolution;
            Assert.AreEqual((float)meta.meters_per_sample_x, mPerTexel, 0.05f,
                "Splat texel'i DEM ornek araligiyla eslesmeli.");
        }

        private (float[,,], int, int) Sample()
        {
            int res = data.alphamapResolution;
            int n = data.terrainLayers.Length;
            return (data.GetAlphamaps(0, 0, res, res), res, n);
        }
    }
}
