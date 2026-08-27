using System.Collections.Generic;
using Hezarfen.Core;
using Hezarfen.Editor.Gis;
using Hezarfen.Editor.Pipeline;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Mahalle dokusunun kurallarını kilitler (RESEARCH.md §4.1, ADR 0016).
    ///
    /// Bu testlerin varlık sebebi Caner'in 2026-08-20'deki itirazıdır:
    /// *"tek bir kusursuz çizgi üzerinde olması biraz şüphelendiriyor beni."*
    /// İtiraz haklıydı ve araştırma doğruladı. Bir yerleştiricinin zamanla
    /// düzleşmesi kolaydır — bir sabit değişir, bir gürültü katsayısı sıfırlanır
    /// ve doku sessizce ızgaraya döner. Aşağıdaki testler o sessiz gerilemeyi
    /// yakalar: <b>sokağın eğri olduğunu ölçerler.</b>
    /// </summary>
    public class OttomanStreetTests
    {
        private const string ScenePath = OttomanStreetBuilder.ScenePath;

        private static Scene Open() =>
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        private static GameObject Root(Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == OttomanStreetBuilder.RootName) return go;
            Assert.Fail($"{OttomanStreetBuilder.RootName} yok. "
                        + "Hezarfen -> GIS -> Galata sokagi sahnesi kur");
            return null;
        }

        /// <summary>Sahnedeki örneğin KATALOG adı ("PF_Turbe_A (1)" → "Turbe_A").</summary>
        private static string AssetNameOf(Transform t)
        {
            for (var x = t; x != null; x = x.parent)
            {
                string n = x.name;
                int p = n.IndexOf(" (");
                if (p > 0) n = n.Substring(0, p);
                if (n.StartsWith("PF_")) return n.Substring(3);
            }
            return "";
        }

        private static List<Transform> Houses(GameObject root, string group)
        {
            var list = new List<Transform>();
            var g = root.transform.Find(group);
            if (g == null) return list;
            foreach (Transform t in g) if (t.GetComponent<LODGroup>() != null) list.Add(t);
            return list;
        }

        [Test]
        public void Scene_HasNeighbourhoodWithTier2Tag()
        {
            var scene = Open();
            try
            {
                var root = Root(scene);
                var tag = root.GetComponent<HistoricalTag>();
                Assert.IsNotNull(tag, "HistoricalTag zorunlu (CLAUDE.md).");
                Assert.AreEqual(HistoricalTier.Reconstruction, tag.tier,
                    "Sokak dokusu T2'dir: ev-ev kaydi yok, kurallarla uretildi.");
                Assert.IsTrue(tag.IsValid);
                Assert.Greater(Houses(root, "Sokak_Ana").Count, 40, "Ana sokak cok seyrek.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void MainStreet_IsNotAStraightLine()
        {
            // KURAL 1'in testi. Evlerin konumlarina bir DOGRU uydurulur ve
            // sapmanin karesel ortalamasi olculur. Izgara ya da duz bir dizi
            // icin bu sayi ~0'dir; organik bir eksen icin onlarca metredir.
            //
            // Esik 8 m: sokagin genisligi (4,6 m) ve ev derinligi (~6,5 m)
            // duzenli bir dizide bile birkac metre sapma uretebilir; 8 m bunun
            // acikca uzerinde ve "kusursuz cizgi" ile karistirilamaz.
            var scene = Open();
            try
            {
                var pts = new List<Vector2>();
                foreach (var t in Houses(Root(scene), "Sokak_Ana"))
                    pts.Add(new Vector2(t.position.x, t.position.z));
                Assert.Greater(pts.Count, 20, "Olcum icin yeterli ev yok.");

                Vector2 mean = Vector2.zero;
                foreach (var p in pts) mean += p;
                mean /= pts.Count;

                // Ana eksen: kovaryanstan (en buyuk yayilim yonu).
                float sxx = 0f, sxz = 0f, szz = 0f;
                foreach (var p in pts)
                {
                    Vector2 d = p - mean;
                    sxx += d.x * d.x; sxz += d.x * d.y; szz += d.y * d.y;
                }
                float theta = 0.5f * Mathf.Atan2(2f * sxz, sxx - szz);
                var axis = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
                var nrm = new Vector2(-axis.y, axis.x);

                float sum = 0f, worst = 0f;
                foreach (var p in pts)
                {
                    float d = Mathf.Abs(Vector2.Dot(p - mean, nrm));
                    sum += d * d; worst = Mathf.Max(worst, d);
                }
                float rms = Mathf.Sqrt(sum / pts.Count);

                Assert.Greater(rms, 8f,
                    $"Sokak fazla duz: dogrudan sapma RMS {rms:F1} m (en buyuk {worst:F1} m). "
                    + "Organik eksen kaybolmus olabilir — TraceContour gurultusu ya da "
                    + "egim takibi kirilmis.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void Facades_AreNotPerfectlyAligned()
        {
            // KURAL 3'un testi. Komsu evlerin yon acilari arasindaki fark
            // olculur; hepsi ayni yone bakiyorsa doku bir cephe duvarina doner.
            var scene = Open();
            try
            {
                var houses = Houses(Root(scene), "Sokak_Ana");
                Assert.Greater(houses.Count, 20);

                int varied = 0;
                for (int i = 1; i < houses.Count; i++)
                {
                    float a = houses[i - 1].rotation.eulerAngles.y;
                    float b = houses[i].rotation.eulerAngles.y;
                    if (Mathf.Abs(Mathf.DeltaAngle(a, b)) > 1.5f) varied++;
                }
                float ratio = varied / (float)(houses.Count - 1);
                Assert.Greater(ratio, 0.6f,
                    $"Cepheler fazla hizali: komsu cift oraninin yalnizca {ratio:P0}'i sapiyor.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void NoHouseIsBuriedInTheTerrain()
        {
            // KURAL 8'in testi ve bu turun asil bulgusu.
            //
            // Olculdu: mahalle egimi medyan %14, p90 %29 (DEM gurultusu DEGIL —
            // 4 m ve 20 m adimda ayni cikti). Ilk yerlestirmede 108 evin 89'u
            // hem havada hem gomuluydu. Ev artik ayak izinin EN YUKSEK kosesine
            // oturur; altindaki bosluk tas kaideyle dolar.
            var scene = Open();
            try
            {
                var root = Root(scene);
                Terrain terrain = null;
                foreach (var go in scene.GetRootGameObjects())
                {
                    terrain = go.GetComponentInChildren<Terrain>();
                    if (terrain != null) break;
                }
                if (terrain == null)
                    foreach (var t in Object.FindObjectsByType<Terrain>())
                    { terrain = t; break; }
                Assert.IsNotNull(terrain, "Arazi yok.");

                float ty = terrain.transform.position.y;
                int buried = 0; float worst = 0f;
                int skippedTrees = 0;
                foreach (var lg in root.GetComponentsInChildren<LODGroup>(true))
                {
                    // Ağaç ve mezar taşı bu kuralın DIŞINDA: kural "en yüksek
                    // köşeye otur, altını kaideyle doldur" diyor ve kaidesi olan
                    // şey yapıdır. Çınarın gövdesi tek noktada zemine değer,
                    // tacı havadadır; tacın sınır kutusunun yamaçta araziye
                    // girmesi kusur değil. Muafiyet ELLE değil katalogdan
                    // geliyor — ad listesi zamanla yalancı olur (ADR 0020 §4).
                    if (!AssetCatalog.IsBuilding(AssetNameOf(lg.transform)))
                    { skippedTrees++; continue; }

                    Renderer r0 = null;
                    foreach (var r in lg.GetComponentsInChildren<Renderer>(true))
                        if (r.gameObject.name.EndsWith("LOD0")) { r0 = r; break; }
                    if (r0 == null) continue;

                    float hx = r0.bounds.extents.x * 0.6f, hz = r0.bounds.extents.z * 0.6f;
                    Vector3 c = lg.transform.position;
                    for (int i = -1; i <= 1; i += 2)
                        for (int j = -1; j <= 1; j += 2)
                        {
                            float h = terrain.SampleHeight(
                                new Vector3(c.x + i * hx, 0f, c.z + j * hz)) + ty;
                            float bury = h - c.y;
                            if (bury > 0.35f) { buried++; worst = Mathf.Max(worst, bury); }
                        }
                }
                // Muafiyetin AYIRT EDICI olduğunu da ölç: katalog okunamazsa
                // IsBuilding her şeye "evet" der ve test sessizce eski hâline
                // döner; ağaç sayısının sıfır çıkması bunun işaretidir.
                Assert.Greater(skippedTrees, 0,
                    "Hicbir agac/mezar muaf tutulmadi — katalog okunamamis "
                    + "olabilir; muafiyet sessizce etkisiz.");
                Assert.AreEqual(0, buried,
                    $"{buried} kose arazinin altinda kaliyor (en kotu {worst:F2} m). "
                    + "Ev en YUKSEK koseye oturmali, bosluk kaideyle dolmali.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void StonePodiums_ExistAsOneMesh()
        {
            // Kaideler TEK mesh olmali: ev basina ayri nesne 100+ cizim cagrisi
            // ekler ve hicbir sey kazandirmaz.
            var scene = Open();
            try
            {
                var root = Root(scene);
                var podium = root.transform.Find("Kaideler");
                Assert.IsNotNull(podium, "Tas kaide mesh'i yok.");

                var mf = podium.GetComponent<MeshFilter>();
                Assert.IsNotNull(mf?.sharedMesh, "Kaide mesh verisi yok.");
                Assert.Greater(mf.sharedMesh.triangles.Length / 3, 100,
                    "Kaide mesh'i sasirtici derecede kucuk.");

                var mr = podium.GetComponent<MeshRenderer>();
                Assert.IsNotNull(mr?.sharedMaterial, "Kaide malzemesiz.");
                StringAssert.Contains("Stone", mr.sharedMaterial.name,
                    "Kaide tas olmali (istinat duvari).");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>
        /// Kaldırım ve kaideler <b>kendi mahallelerinin altında</b> olmalı.
        ///
        /// İki mahalle (Galata, Balat) üretilen mesh'i aynı varlık yoluna
        /// yazıyordu — `SM_Kaldirim.asset`. Balat kurulduğunda Galata'nın
        /// kaldırımı ve taş kaideleri **siliniyor**, yerlerine 2 km ötedeki
        /// Balat'ın geometrisi geçiyordu. Ölçüldü: Galata sahnesindeki
        /// `SM_Kaldirim`'in merkezi <b>x = −1976</b>, yani Galata sokağı
        /// kaldırımsız ve kaidesiz kalmıştı.
        ///
        /// Sahne bozulmuş GÖRÜNMÜYORDU: eksik olan şey sessizce başka bir
        /// yerde duruyordu. Test bunu bir mesafeye çeviriyor.
        /// </summary>
        [Test]
        public void GeneratedMeshesBelongToThisQuarter()
        {
            var scene = Open();
            try
            {
                var root = Root(scene);
                Transform core = null;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("PF_Mescit") || t.name.StartsWith("PF_Sinagog"))
                    { core = t; break; }
                Assert.IsNotNull(core, "Cekirdek yapi yok.");

                foreach (string nm in new[] { "Kaldirim", "Kaideler" })
                {
                    var go = root.transform.Find(nm);
                    Assert.IsNotNull(go, $"{nm} yok.");
                    var mesh = go.GetComponent<MeshFilter>()?.sharedMesh;
                    Assert.IsNotNull(mesh, $"{nm} mesh verisi yok.");

                    // Mesh dunya koordinatlarinda uretiliyor (donusum birim).
                    Vector3 c = mesh.bounds.center;
                    float dist = Vector2.Distance(new Vector2(c.x, c.z),
                        new Vector2(core.position.x, core.position.z));
                    Assert.Less(dist, 400f,
                        $"{nm} mesh'inin merkezi cekirdekten {dist:F0} m uzakta "
                        + $"({c}). Baska bir mahallenin geometrisi yazilmis olabilir "
                        + "— varlik yolu semte gore adlandirilmali.");
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>
        /// Kaldırımın <b>yürünen yüzü yukarı</b> bakmalı.
        ///
        /// Bu test bir kusurdan doğdu ve kusur üç turdur görünmüyordu:
        /// <c>SM_Kaldirim</c>'in 698 yatay üçgeninin <b>697'si aşağı</b>
        /// bakıyordu. Sonuçları: kaldırım üstten ışıksız/siyah okunuyordu,
        /// ışın sorguları arka yüzü görmediği için çarpıcı fiilen yoktu
        /// (oyuncu düşerdi) ve sokak çimen görünüyordu.
        ///
        /// Gözle yakalanamazdı: bütün kareler kaldırımın ALTINDAN alınmıştı
        /// ve oradan bakınca yüzey gayet doğru görünüyor. Yakalayan şey
        /// üçgen normallerinin sayılmasıydı — test de onu sayıyor.
        ///
        /// Kaideler mesh'i karşılaştırma taşıdır: onda 166 yukarı, 0 aşağı.
        /// </summary>
        [Test]
        public void PavementWalkingSurfaceFacesUp()
        {
            var scene = Open();
            try
            {
                var root = Root(scene);
                var paving = root.transform.Find("Kaldirim");
                Assert.IsNotNull(paving, "Kaldirim mesh'i yok.");
                var mesh = paving.GetComponent<MeshFilter>()?.sharedMesh;
                Assert.IsNotNull(mesh, "Kaldirim mesh verisi yok.");

                var v = mesh.vertices;
                var t = mesh.triangles;
                int up = 0, down = 0;
                for (int i = 0; i < t.Length; i += 3)
                {
                    Vector3 n = Vector3.Cross(v[t[i + 1]] - v[t[i]],
                                              v[t[i + 2]] - v[t[i]]).normalized;
                    if (n.y > 0.5f) up++;
                    else if (n.y < -0.5f) down++;
                }

                Assert.Greater(up, 100,
                    $"Kaldirimda yukari bakan yuzey yok ({up} ucgen). "
                    + "Yurunen yuzey yoksa kaldirim yok demektir.");
                Assert.Less(down, up * 0.05f,
                    $"Kaldirimin {down} ucgeni ASAGI bakiyor ({up} yukari). "
                    + "Sarim ters: yuzey ustten siyah okunur ve isin sorgulari "
                    + "arka yuzu gormedigi icin cizici fiilen YOKTUR.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
