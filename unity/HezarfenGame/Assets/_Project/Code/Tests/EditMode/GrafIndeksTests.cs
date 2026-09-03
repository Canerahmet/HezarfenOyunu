using System.Collections.Generic;
using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests.EditMode
{
    /// <summary>
    /// <b>Izgara indeksi doğrusal taramayla aynı cevabı versin.</b>
    ///
    /// ## Neden değişti
    ///
    /// <c>SokakGrafi.EnYakin</c> bütün düğümleri tarıyordu ve
    /// <c>NPCYonetici.AjaniYenile</c> onu iki kez çağırıyor. Yenileme
    /// kuyruğu kare başına 400 sakin işliyor; 1.544 düğümde bu kare
    /// başına ~1,2 milyon mesafe hesabı. Aynı işlevde <c>Komsuluk()</c>
    /// de her yol aramasında düğüm sayısı kadar liste ayırıyordu.
    ///
    /// ## Neden test
    ///
    /// Hızlandırma <b>cevabı değiştirirse</b> hızlandırma değildir:
    /// NPC yanlış eve gider, yanlış mescide yürür ve bu hiçbir yerde
    /// hata vermez — yalnızca şehir tuhaflaşır. Test kendi kaba
    /// kuvvet karşılığını taşıyor; bir sabiti kopyalamıyor, cevabı
    /// <b>bağımsız olarak</b> hesaplıyor.
    /// </summary>
    public class GrafIndeksTests
    {
        private static SokakGrafi Graf(int adet, int tohum)
        {
            var g = ScriptableObject.CreateInstance<SokakGrafi>();
            var rng = new System.Random(tohum);
            var turler = new[]
            {
                SokakGrafi.Tur.Ev, SokakGrafi.Tur.Mescit,
                SokakGrafi.Tur.Cesme, SokakGrafi.Tur.Dukkan,
                SokakGrafi.Tur.Iskele,
            };
            for (int i = 0; i < adet; i++)
            {
                g.dugumler.Add(new SokakGrafi.Dugum
                {
                    konum = new Vector3(
                        (float)(rng.NextDouble() * 4000.0 - 2000.0),
                        0f,
                        (float)(rng.NextDouble() * 4000.0 - 2000.0)),
                    // Iskele bilerek SEYREK: seyrek bir turde halka
                    // aramasi cok acilmak zorunda kalir ve durma kurali
                    // orada sinanir.
                    tur = i % 97 == 0 ? SokakGrafi.Tur.Iskele
                                      : turler[i % 4],
                });
            }
            return g;
        }

        /// <summary>Kaba kuvvet karşılık — testin kendi ölçüsü.</summary>
        private static int KabaKuvvet(SokakGrafi g, Vector3 p,
                                      SokakGrafi.Tur? tur)
        {
            int en = -1;
            float d2 = float.MaxValue;
            for (int i = 0; i < g.dugumler.Count; i++)
            {
                if (tur.HasValue && g.dugumler[i].tur != tur.Value) continue;
                float d = (g.dugumler[i].konum - p).sqrMagnitude;
                if (d < d2) { d2 = d; en = i; }
            }
            return en;
        }

        [Test]
        public void TheGridFindsExactlyWhatALinearScanWouldFind()
        {
            var g = Graf(2000, 1632);
            var rng = new System.Random(7);
            var turler = new SokakGrafi.Tur?[]
            {
                null, SokakGrafi.Tur.Ev, SokakGrafi.Tur.Mescit,
                SokakGrafi.Tur.Iskele, SokakGrafi.Tur.Hamam,
            };

            for (int deneme = 0; deneme < 400; deneme++)
            {
                var p = new Vector3(
                    (float)(rng.NextDouble() * 5000.0 - 2500.0),
                    (float)(rng.NextDouble() * 50.0),
                    (float)(rng.NextDouble() * 5000.0 - 2500.0));
                var tur = turler[deneme % turler.Length];

                int izgara = g.EnYakin(p, tur);
                int kaba = KabaKuvvet(g, p, tur);

                if (izgara == kaba) continue;

                // Esit uzaklikta iki dugum varsa hangisinin secildigi
                // onemli degil; MESAFE esit olmali.
                Assert.AreNotEqual(-1, izgara,
                    $"Izgara bulamadi ama kaba kuvvet buldu ({tur}).");
                float a = (g.dugumler[izgara].konum - p).sqrMagnitude;
                float b = (g.dugumler[kaba].konum - p).sqrMagnitude;
                Assert.AreEqual(b, a, 1e-3f,
                    $"Izgara {izgara}, kaba kuvvet {kaba} — ve mesafeler "
                    + $"farkli ({tur}, nokta {p}).");
            }
        }

        [Test]
        public void AskingForATypeThatDoesNotExistReturnsNothing()
        {
            // Hamam hic yok: arama halka halka sehrin kosegenine kadar
            // acilir ve DURMALI. Kilit yoksa bu dongu bitmez.
            var g = Graf(500, 3);
            Assert.AreEqual(-1,
                g.EnYakin(Vector3.zero, SokakGrafi.Tur.Hamam));
        }

        [Test]
        public void TheCachedAdjacencyMatchesAFreshlyBuiltOne()
        {
            var g = Graf(300, 11);
            var rng = new System.Random(5);
            for (int i = 0; i < 900; i++)
            {
                int a = rng.Next(g.dugumler.Count);
                int b = rng.Next(g.dugumler.Count);
                if (a == b) continue;
                g.kenarlar.Add(new SokakGrafi.Kenar
                {
                    a = a, b = b,
                    uzunluk = Vector3.Distance(g.dugumler[a].konum,
                                               g.dugumler[b].konum),
                    kayik = i % 13 == 0,
                });
            }

            foreach (bool kayikVar in new[] { true, false })
            {
                var beklenen = new List<int>[g.dugumler.Count];
                for (int i = 0; i < beklenen.Length; i++)
                    beklenen[i] = new List<int>();
                foreach (var e in g.kenarlar)
                {
                    if (!kayikVar && e.kayik) continue;
                    beklenen[e.a].Add(e.b);
                    beklenen[e.b].Add(e.a);
                }

                // Iki kez sorulur: ikincisi ONBELLEKTEN gelir ve ayni
                // olmali. Onbellegin sessizce eskimesi, NPC'nin var
                // olmayan bir sokaktan yurumesi demektir.
                for (int tur = 0; tur < 2; tur++)
                {
                    var kom = g.Komsuluk(kayikVar);
                    for (int i = 0; i < beklenen.Length; i++)
                        CollectionAssert.AreEqual(beklenen[i], kom[i],
                            $"Dugum {i} komsulari ayrildi "
                            + $"(kayikVar={kayikVar}, tur={tur}).");
                }
            }
        }

        [Test]
        public void AddingNodesInvalidatesTheCaches()
        {
            var g = Graf(50, 2);
            var p = new Vector3(9999f, 0f, 9999f);
            g.EnYakin(p);                       // indeks kurulur
            g.Komsuluk();                       // onbellek kurulur

            g.dugumler.Add(new SokakGrafi.Dugum
            { konum = p, tur = SokakGrafi.Tur.Ev });

            Assert.AreEqual(g.dugumler.Count - 1, g.EnYakin(p),
                "Yeni eklenen dugum indekste gorunmuyor — onbellek "
                + "eskidi.");
            Assert.AreEqual(g.dugumler.Count, g.Komsuluk().Length,
                "Komsuluk onbellegi eski dugum sayisini tasiyor.");
        }
    }
}
