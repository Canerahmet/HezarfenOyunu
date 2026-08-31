using System.Collections.Generic;
using System.Linq;
using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Üretilen görev oynanabilir mi?</b>
    ///
    /// Şablondan görev üretmenin sessiz hatası şudur: görev üretilir,
    /// listede görünür, oyuncu yola çıkar ve <b>varamaz</b> — çünkü
    /// durak başka bir kara parçasındadır ya da o mahallede öyle bir yer
    /// yoktur. Oyuncu oyunu suçlar; oysa suçlu üreticidir.
    ///
    /// Bu yüzden testler görevin metnine değil <b>duraklarına</b>
    /// bakıyor: hepsi var mı, hepsine yürünebiliyor mu, ödül ekonominin
    /// ölçeğinde mi, ve kronoloji tutuyor mu.
    /// </summary>
    public class GorevTests
    {
        private static SokakGrafi Graf()
        {
            var g = AssetDatabase.LoadAssetAtPath<SokakGrafi>(
                "Assets/_Project/Data/SG_Sehir.asset");
            Assert.IsNotNull(g, "Sokak grafi yok — once grafi kur.");
            return g;
        }

        /// <summary>Şehirde bir mahalle merkezinden görev üret.</summary>
        private static Vector3 Merkez(SokakGrafi g)
        {
            var mescit = g.dugumler.First(
                d => d.tur == SokakGrafi.Tur.Mescit && d.semt == "D_Surici_Dogu");
            return mescit.konum;
        }

        /// <summary>
        /// <b>Her üretilen görevin durakları gerçekten var ve bağlı.</b>
        ///
        /// Bir görev, durakları aynı yürüme bileşeninde değilse
        /// tamamlanamaz: Galata'dan Üsküdar'daki dükkâna yük götürülmez,
        /// arada Boğaz vardır.
        /// </summary>
        [Test]
        public void EveryGeneratedQuestCanActuallyBeWalked()
        {
            var g = Graf();
            var merkez = Merkez(g);
            int uretilen = 0;

            foreach (GorevArketip a in System.Enum.GetValues(
                         typeof(GorevArketip)))
            {
                // Kacakcilik yalniz yasak doneminde uretilir.
                int yil = a == GorevArketip.Kacakcilik ? 1634 : 1632;
                for (int t = 0; t < 25; t++)
                {
                    var gorev = GorevUretici.Uret(g, a, merkez, t, yil, 121);
                    if (gorev == null) continue;
                    uretilen++;

                    Assert.IsNotEmpty(gorev.duraklar,
                        $"{a}: duraksiz gorev uretildi.");
                    for (int i = 1; i < gorev.duraklar.Count; i++)
                    {
                        // KAYIK GOREVI KAYIKLA OLCULUR.
                        //
                        // Bu satir `kayikVar: false` yaziyordu ve bu
                        // butun arketipler icin dogruydu — bir tanesi
                        // haric. `KayikYolcu`'nun TANIMI karsiya
                        // gecmektir; yuruyerek erisilebilirligini
                        // sormak, gorevin var olmamasini sart kosmak
                        // olur. Testi gevsetmiyoruz: soruyu gorevin
                        // kendi sozlesmesine gore soruyoruz.
                        bool kayikla = a == GorevArketip.KayikYolcu;
                        var yol = g.Yol(gorev.duraklar[i - 1],
                                        gorev.duraklar[i], kayikla);
                        Assert.IsNotEmpty(yol,
                            $"{a}: {i - 1}. duraktan {i}. duraga YOL YOK. "
                            + "Oyuncu yurur, yurur, varamaz.");
                    }
                }
            }
            Assert.Greater(uretilen, 60,
                $"Yalnizca {uretilen} gorev uretilebildi; sablonlar sehrin "
                + "dokusuyla eslesmiyor.");
        }

        /// <summary>
        /// <b>Kaçakçılık yasaktan önce yok.</b>
        ///
        /// Yasak yokken kaçırılacak bir şey de yoktur. Bu, kronolojinin
        /// görev listesinde de görünmesi demek: 1632'de oynayan oyuncu o
        /// görevi hiç görmez, 1634'te görür.
        /// </summary>
        [Test]
        public void SmugglingOnlyExistsOnceThereIsABan()
        {
            var g = Graf();
            var merkez = Merkez(g);

            Assert.IsFalse(GorevUretici.Uygun(GorevArketip.Kacakcilik,
                                              1632, 121),
                "1632'de kacakcilik gorevi uygun sayildi — kahvehaneler acik.");
            Assert.IsTrue(GorevUretici.Uygun(GorevArketip.Kacakcilik,
                                             1634, 121),
                "1634'te kacakcilik gorevi uretilemiyor — yasak yururlukte.");

            for (int t = 0; t < 10; t++)
                Assert.IsNull(
                    GorevUretici.Uret(g, GorevArketip.Kacakcilik, merkez,
                                      t, 1632, 121),
                    "1632'de kacakcilik gorevi uretildi.");
        }

        /// <summary>
        /// <b>Kaçakçılık yasak mal taşıtıyor</b> — yani aranma riski
        /// gerçek. Ödül yüksek çünkü risk gerçek; risk sahteyse ödül de
        /// sahtedir.
        /// </summary>
        [Test]
        public void SmugglingActuallyPutsContrabandInYourHands()
        {
            var g = Graf();
            var gorev = GorevUretici.Uret(g, GorevArketip.Kacakcilik,
                                          Merkez(g), 3, 1634, 121);
            Assert.IsNotNull(gorev, "Kacakcilik gorevi uretilemedi.");
            Assert.IsTrue(gorev.yasakMal,
                "Kacakcilik gorevi yasak mal tasitmiyor — aranma riski "
                + "yoksa yuksek odulun karsiligi da yok.");

            var teslimat = GorevUretici.Uret(g, GorevArketip.Teslimat,
                                             Merkez(g), 3, 1634, 121);
            Assert.IsFalse(teslimat.yasakMal,
                "Siradan teslimat yasak mal sayildi.");
        }

        /// <summary>
        /// <b>Ödüller ekonominin ölçeğinde ve risk sıralamasında.</b>
        ///
        /// Ölçü akçe değil <b>gün</b>: bir teslimat yarım günlük emek,
        /// kaçakçılık birkaç günlük. Sayılar elle yazılsaydı ekonominin
        /// çapası değişince sessizce yanlış kalırdı.
        /// </summary>
        [Test]
        public void RewardsSitOnTheEconomysScale()
        {
            var g = Graf();
            var merkez = Merkez(g);

            float Ortalama(GorevArketip a, int yil)
            {
                var liste = new List<int>();
                for (int t = 0; t < 40; t++)
                {
                    var q = GorevUretici.Uret(g, a, merkez, t, yil, 121);
                    if (q != null) liste.Add(q.akce);
                }
                Assert.IsNotEmpty(liste, $"{a} hic uretilemedi.");
                return (float)liste.Average();
            }

            float teslimat = Ortalama(GorevArketip.Teslimat, 1632);
            float kayip = Ortalama(GorevArketip.Kayip, 1632);
            float kacak = Ortalama(GorevArketip.Kacakcilik, 1634);

            Assert.Less(teslimat, kayip,
                "Bir teslimat, kayip esya izlemekten cok kazandiriyor.");
            Assert.Less(kayip, kacak,
                "Kacakcilik en riskli is ama en cok kazandiran degil.");

            // Gun cinsinden makul band.
            Assert.That(Ekonomi.GunCinsinden((int)teslimat),
                Is.InRange(0.3f, 1.0f),
                "Bir teslimat yarim gunluk emek civari olmali.");
            Assert.That(Ekonomi.GunCinsinden((int)kacak),
                Is.InRange(2.5f, 7.0f),
                "Kacakcilik birkac gunluk yevmiye kadar olmali — daha azi "
                + "riski karsilamaz, daha cogu ekonomiyi bozar.");
        }

        /// <summary>
        /// <b>Aynı arketip farklı tohumda farklı yerler kullanıyor.</b>
        ///
        /// Şablon üretmenin anlamı varyasyon. Hep aynı dükkâna giden
        /// otuz teslimat, elle yazılmış tek bir görevden daha kötüdür:
        /// hem tekrar eder hem şehri hiç göstermez.
        /// </summary>
        [Test]
        public void TheSameTemplateVisitsDifferentPlaces()
        {
            var g = Graf();
            var merkez = Merkez(g);
            var gorulen = new HashSet<int>();
            for (int t = 0; t < 30; t++)
            {
                var q = GorevUretici.Uret(g, GorevArketip.Teslimat,
                                          merkez, t, 1632, 121);
                if (q != null) foreach (int d in q.duraklar) gorulen.Add(d);
            }
            Assert.Greater(gorulen.Count, 6,
                $"Otuz teslimat yalnizca {gorulen.Count} farkli yer "
                + "kullandi — sablonun varyasyonu yok.");
        }

        /// <summary>Görev durak durak ilerliyor ve bitiyor.</summary>
        [Test]
        public void AQuestAdvancesStopByStopAndEnds()
        {
            var g = Graf();
            var q = GorevUretici.Uret(g, GorevArketip.Kayip, Merkez(g),
                                      11, 1632, 121);
            Assert.IsNotNull(q);
            int n = q.duraklar.Count;
            Assert.Greater(n, 1, "Kayip esya izi tek duraktan ibaret.");

            for (int i = 0; i < n; i++)
            {
                Assert.IsFalse(q.Bitti, $"{i}. durakta gorev erken bitti.");
                Assert.AreEqual(q.duraklar[i], q.Hedef);
                q.DurakTamam();
            }
            Assert.IsTrue(q.Bitti, "Butun duraklar gezildi ama gorev bitmedi.");
            Assert.AreEqual(-1, q.Hedef);
        }
    }
}
