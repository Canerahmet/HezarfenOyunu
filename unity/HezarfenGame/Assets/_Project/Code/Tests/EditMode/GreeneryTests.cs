using System.Collections.Generic;
using Hezarfen.Editor.Gis;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Yeşil dokunun <b>kuralını</b> kilitler.
    ///
    /// En önemlisi bir YOKLUK: II. Bayezid'in Okmeydanı vakfiyesi meydanda
    /// yapı, mezar, su yolu, <b>bağ ve bahçe</b> yapılmasını yasaklar. Orası
    /// bilinçle boş tutulmuş bir talim alanıdır ve Hezarfen'in talim yaptığı
    /// yerdir. Oraya bir ağaç düşerse bu görsel bir kusur değil, <b>belgeye
    /// aykırılık</b> olur — ve 45 bin ağacın içinde gözle bulunamaz.
    /// </summary>
    public class GreeneryTests
    {
        private TerrainData data;
        private GreeneryBuilder.Area[] areas;
        private TerrainImporter.DemMeta meta;

        [SetUp]
        public void SetUp()
        {
            meta = TerrainImporter.ReadMeta(TerrainImporter.DefaultDataDir);
            if (meta == null) Assert.Ignore("DEM verisi yok.");

            data = AssetDatabase.LoadAssetAtPath<TerrainData>(
                TerrainImporter.TerrainAssetPath);
            Assert.IsNotNull(data, "TerrainData yok.");

            areas = GreeneryBuilder.ReadAreas();
            if (areas == null) Assert.Ignore("greenery_local.json yok — greenery_build.py");
            if (data.treeInstanceCount == 0)
                Assert.Ignore("Agac dikilmemis — Hezarfen/GIS/Yesil dokuyu dik");
        }

        // ------------------------------------------------------------- veri

        [Test]
        public void EveryAreaDeclaresItsEvidence()
        {
            // Sinir CIZILMISTIR, olculmemistir; bunu tasiyan alan `tier`dir.
            // Kaybolursa taslak bir sinir belgeli gibi okunur.
            foreach (var a in areas)
            {
                Assert.IsNotEmpty(a.id);
                Assert.IsNotEmpty(a.tier, $"{a.id}: tier yok.");
                Assert.Greater(a.ring.Length, 2, $"{a.id}: halka eksik.");
                Assert.Greater(a.area_ha, 0.5f, $"{a.id}: alan sifira yakin.");
            }
        }

        [Test]
        public void EveryBoundaryDeclaresWhatHoldsItsSize()
        {
            // `tier` sinirin TARIHSEL guvenini soyler; `basis` sinirin
            // BUYUKLUGUNU neyin tuttugunu soyler. Ikisi ayri sorulardir ve
            // bir tanesi eksikken on bir sinir da ayni sekilde cizilmisti.
            // Tek sinandiklari yerde biri yari yariya kucuk (Okmeydani),
            // bir baskasi ALTI KAT buyuk (Galata) cikti.
            var allowed = new HashSet<string>
                { "documented", "walls", "terrain", "drawn" };
            foreach (var a in areas)
            {
                Assert.IsNotEmpty(a.basis, $"{a.id}: basis yok — sinirin "
                    + "buyuklugunu neyin tuttugu yazili degil.");
                Assert.IsTrue(allowed.Contains(a.basis),
                    $"{a.id}: bilinmeyen basis '{a.basis}'.");
            }
        }

        [Test]
        public void WallBackedBoundariesMatchTheWalls()
        {
            // `basis == "walls"` bir IDDIADIR: "bu sinir ayri bir cizim degil,
            // surun kendisidir". Iki dosya (walls_1632_local, greenery_local)
            // ayri uretiliyor, yani iddia bir gun sessizce yalan olabilir.
            //
            // Olculen sey: sur icini iddia eden poligonun her kosesi, sur
            // hattinin bir noktasindan 1 m'den yakin olmali.
            var wall = WallPoints();
            if (wall.Count == 0) Assert.Ignore("walls_1632_local.json yok.");

            int checkedAreas = 0;
            foreach (var a in areas)
            {
                if (a.basis != "walls" || a.id == "G_YedikuleBostanlari") continue;
                checkedAreas++;
                foreach (var p in a.ring)
                {
                    float best = float.MaxValue;
                    foreach (var w in wall)
                        best = Mathf.Min(best, (w - new Vector2(p.x, p.z)).sqrMagnitude);
                    Assert.Less(Mathf.Sqrt(best), 1.0f,
                        $"{a.id}: bir kosesi sur hattindan {Mathf.Sqrt(best):F0} m "
                        + "uzakta — 'sinir surun kendisidir' iddiasi tutmuyor.");
                }
            }
            Assert.Greater(checkedAreas, 0, "Sur tabanli hicbir sinir yok.");
        }

        private static List<Vector2> WallPoints()
        {
            var list = new List<Vector2>();
            // `DefaultDataDir` proje kokune GORE bir yoldur; Unity'nin calisma
            // dizini oradan farkli olabilir, bu yuzden cozumleyici sart.
            string dir = TerrainImporter.ResolveDataDir(TerrainImporter.DefaultDataDir);
            if (dir == null) return list;
            string path = System.IO.Path.Combine(dir, "walls_1632_local.json");
            if (!System.IO.File.Exists(path)) return list;
            // Elle ayristirma: dosya {"x":..,"z":..} ciftlerinden olusuyor ve
            // JsonUtility ic ice degisken semayi okuyamiyor.
            string txt = System.IO.File.ReadAllText(path);
            int i = 0;
            while ((i = txt.IndexOf("\"x\":", i)) >= 0)
            {
                int c = txt.IndexOf(',', i);
                int zi = txt.IndexOf("\"z\":", c);
                if (zi < 0) break;
                int e = txt.IndexOfAny(new[] { ',', '}' }, zi + 4);
                if (float.TryParse(txt.Substring(i + 4, c - i - 4),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float x)
                    && float.TryParse(txt.Substring(zi + 4, e - zi - 4),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float z))
                    list.Add(new Vector2(x, z));
                i = e;
            }
            return list;
        }

        [Test]
        public void PlantingAreasAreMostlyOnLand()
        {
            foreach (var a in areas)
                if (a.density > 0f)
                    Assert.Greater(a.land_fraction, 0.55f,
                        $"{a.name}: alanin yalnizca %{a.land_fraction * 100:F0}'i kara.");
        }

        // ------------------------------------------------------ YOKLUK kurali

        [Test]
        public void NoTreesOnOkmeydaniTrainingGround()
        {
            var yasak = Find("G_Okmeydani_Yasak");
            Assert.IsNotNull(yasak, "Okmeydani yasak alani veri dosyasinda yok.");

            int inside = CountTreesInside(yasak);
            Assert.AreEqual(0, inside,
                $"Okmeydani'na {inside} agac dusmus. II. Bayezid vakfiyesi meydanda "
                + "YAPI, MEZAR, SU YOLU, BAG VE BAHCE yapilmasini yasaklar; orasi "
                + "bilincle bos tutulmus bir talim alanidir (Hezarfen'in talim yeri).");

            // "Gecti" ile "dogru" ayni sey degil: ayni testin agac SAYABILDIGINI
            // de gostermeli, yoksa sayac hep 0 dondurse de yesil kalirdi.
            var mezarlik = Find("G_Karacaahmet");
            Assert.IsNotNull(mezarlik);
            Assert.Greater(CountTreesInside(mezarlik), 500,
                "Denetimin ayirt etme gucu yok: mezarlikta da agac sayamiyor.");
        }

        [Test]
        public void NoTreesInsideSettledCores()
        {
            foreach (var a in areas)
            {
                if (a.kind != "yerlesim" && a.kind != "bostan") continue;
                int inside = CountTreesInside(a);
                Assert.AreEqual(0, inside,
                    $"{a.name}: {inside} agac. Bostan sebze tarhidir, yerlesim yapilidir.");
            }
        }

        // ------------------------------------------------------------ dikim

        [Test]
        public void EveryNamedGroveActuallyHasTrees()
        {
            // Olu bir alan sessiz bir hatadir: kural yanlis yazildiginda
            // poligon durur, kaynagi durur, ama tek agac cikmaz.
            foreach (var a in areas)
            {
                if (a.density <= 0f) continue;
                Assert.Greater(CountTreesInside(a), 100,
                    $"{a.name}: neredeyse hic agac yok — kural onu eliyor olabilir.");
            }
        }

        [Test]
        public void NoTreeStandsInTheSea()
        {
            var t = data.treeInstances;
            float b = (float)meta.base_elevation_m;
            int wet = 0;
            for (int i = 0; i < t.Length; i += 7)
            {
                float el = b + data.GetInterpolatedHeight(t[i].position.x, t[i].position.z);
                if (el < 0.5f) wet++;
            }
            Assert.AreEqual(0, wet, $"{wet} agac deniz seviyesinin altinda.");
        }

        [Test]
        public void PrototypesCoverEverySpeciesInUse()
        {
            Assert.Greater(data.treePrototypes.Length, 0, "Agac prototipi yok.");
            foreach (var p in data.treePrototypes)
                Assert.IsNotNull(p.prefab, "Prototipin prefabi bos.");

            // Tek varyantla dikilen bir mezarlik, kopyalanmis tek agactan
            // olusur ve bunu goz hemen yakalar.
            var used = new HashSet<int>();
            var t = data.treeInstances;
            for (int i = 0; i < t.Length; i += 13) used.Add(t[i].prototypeIndex);
            Assert.Greater(used.Count, 2,
                $"Yalnizca {used.Count} varyant kullanilmis — doku tekrari okunur.");
        }

        // ---------------------------------------------------------- yardimci

        private GreeneryBuilder.Area Find(string id)
        {
            foreach (var a in areas) if (a.id == id) return a;
            return null;
        }

        private int CountTreesInside(GreeneryBuilder.Area a)
        {
            Vector3 origin = TerrainOrigin();
            var t = data.treeInstances;
            int n = 0;
            foreach (var ti in t)
            {
                float x = origin.x + ti.position.x * data.size.x;
                float z = origin.z + ti.position.z * data.size.z;
                float dx = x - a.center_x, dz = z - a.center_z;
                if (dx * dx + dz * dz > a.radius_m * a.radius_m) continue;
                if (PointInRing(x, z, a.ring)) n++;
            }
            return n;
        }

        private Vector3 TerrainOrigin() =>
            new Vector3((float)meta.world_origin_offset_m.x,
                        (float)meta.base_elevation_m,
                        (float)meta.world_origin_offset_m.z);

        private static bool PointInRing(float x, float z, GreeneryBuilder.LocalPoint[] ring)
        {
            bool inside = false;
            for (int i = 0; i < ring.Length; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Length];
                if ((a.z > z) != (b.z > z))
                {
                    float xc = a.x + (z - a.z) * (b.x - a.x) / (b.z - a.z);
                    if (x < xc) inside = !inside;
                }
            }
            return inside;
        }
    }
}
