using System.Collections.Generic;
using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Üretilen görev oynanabilir mi.</b>
    ///
    /// `GorevTests` görevin <b>tamamlanabilirliğini</b> soruyor — bütün
    /// duraklar aynı bileşende mi, oraya yürünebiliyor mu. Bu dosya
    /// ayrı bir soru soruyor: <b>üretilen görev bir iş mi.</b>
    ///
    /// Fark boş bir ayrım değil. Gerçek şehir grafında 60 görevlik bir
    /// dizi ölçüldü ve iki kusur çıktı, ikisi de "tamamlanabilir"
    /// testinden geçiyordu:
    ///
    /// - 60 görevin **12'si (%20)** aynı iskeleyi iki kez seçmişti.
    ///   `KayikYolcu` iki iskele ister; Galata bileşeninde tek iskele
    ///   var. İkinci durağın mesafesi 0,0 m, yani "karşıya yolcu
    ///   götür" görevi ilk durağa varan oyuncunun elinde kendiliğinden
    ///   bitiyordu.
    /// - 168 durağın **18'i (%11)** daha üretildiği anda 15 m'lik
    ///   varış eşiğinin içindeydi. Üç duraklı bir görev iki durakta
    ///   bitiyordu.
    ///
    /// İkisi de "yol var mı" sorusuna doğru cevap veriyordu. Yanlış
    /// olan cevap değil, sorunun kendisiydi.
    /// </summary>
    public class GorevUretimKalitesiTests
    {
        private SokakGrafi _graf;

        /// <summary>
        /// <b>Dar</b> bir graf — kusurun ortaya çıktığı koşul.
        ///
        /// Tek iskele, tek han: gerçek Galata bileşeninin durumu. Bol
        /// adaylı bir grafta bu kusurlar hiç görünmez ve test yanlış
        /// yere güven verir.
        /// </summary>
        [SetUp]
        public void Kur()
        {
            _graf = ScriptableObject.CreateInstance<SokakGrafi>();
            void Ekle(Vector3 p, SokakGrafi.Tur t) =>
                _graf.dugumler.Add(new SokakGrafi.Dugum
                { konum = p, tur = t, semt = "D_Test" });

            Ekle(new Vector3(0f, 0f, 0f), SokakGrafi.Tur.Iskele);
            Ekle(new Vector3(120f, 0f, 0f), SokakGrafi.Tur.Han);
            Ekle(new Vector3(240f, 0f, 0f), SokakGrafi.Tur.Dukkan);
            Ekle(new Vector3(360f, 0f, 0f), SokakGrafi.Tur.Mescit);
            Ekle(new Vector3(480f, 0f, 0f), SokakGrafi.Tur.Cesme);
            Ekle(new Vector3(600f, 0f, 0f), SokakGrafi.Tur.Ev);
            Ekle(new Vector3(720f, 0f, 0f), SokakGrafi.Tur.Dukkan);
            // Ikinci iskele: 130 m otede, yani EnAzDurakArasi'ni gecer.
            Ekle(new Vector3(130f, 0f, 90f), SokakGrafi.Tur.Iskele);

            for (int i = 0; i + 1 < _graf.dugumler.Count; i++)
            {
                _graf.kenarlar.Add(new SokakGrafi.Kenar
                { a = i, b = i + 1, uzunluk = 120f });
                _graf.kenarlar.Add(new SokakGrafi.Kenar
                { a = i + 1, b = i, uzunluk = 120f });
            }
        }

        [TearDown]
        public void Yik()
        {
            if (_graf != null) Object.DestroyImmediate(_graf);
        }

        private List<Gorev> Dizi(int n)
        {
            var l = new List<Gorev>();
            var turler = (GorevArketip[])
                System.Enum.GetValues(typeof(GorevArketip));
            for (int i = 0; i < n; i++)
            {
                var a = turler[i % turler.Length];
                if (!GorevUretici.Uygun(a, 1632, 121)) continue;
                var g = GorevUretici.Uret(_graf, a, Vector3.zero,
                                          1632 + i * 7919, 1632, 121);
                if (g != null) l.Add(g);
            }
            return l;
        }

        [Test]
        public void NoQuestSendsYouToTheSamePlaceTwice()
        {
            foreach (var g in Dizi(60))
            {
                var gorulen = new HashSet<int>();
                foreach (int d in g.duraklar)
                    Assert.IsTrue(gorulen.Add(d),
                        $"{g.arketip}: {d} numarali durak iki kez "
                        + "kullanildi. Ayni yere iki kez giden bir gorev, "
                        + "ilk varista kendiliginden biter.");
            }
        }

        [Test]
        public void NoStopIsBornInsideTheArrivalThreshold()
        {
            // GorevYonetici'nin varis esigi 15 m; duraklar arasi ondan
            // acikca uzak olmali, yoksa bir duraga varmak bir sonrakini
            // de tetikler.
            foreach (var g in Dizi(60))
                for (int i = 0; i + 1 < g.duraklar.Count; i++)
                {
                    float d = Vector3.Distance(
                        _graf.dugumler[g.duraklar[i]].konum,
                        _graf.dugumler[g.duraklar[i + 1]].konum);
                    Assert.GreaterOrEqual(d, GorevUretici.EnAzDurakArasi - 1f,
                        $"{g.arketip}: {i}. ve {i + 1}. durak arasi "
                        + $"{d:F1} m — varis esiginin (15 m) icinde dogan "
                        + "bir durak, hic yasanmayan bir duraktir.");
                }
        }

        [Test]
        public void EveryQuestStillHasAllItsStops()
        {
            // Kisitlar eklendi; uretecin bunlar yuzunden gorev
            // URETEMEZ hale gelmedigini de sormak lazim. Bir kusuru
            // duzeltirken sistemi susturmak, kusuru duzeltmek degil.
            var dizi = Dizi(60);
            Assert.Greater(dizi.Count, 20,
                $"Yalniz {dizi.Count} gorev uretilebildi — kisitlar "
                + "ureteci bogmus olabilir.");
            foreach (var g in dizi)
                Assert.Greater(g.duraklar.Count, 1,
                    $"{g.arketip} tek duraga dustu.");
        }
        /// <summary>
        /// <b>Gerçek şehirde görev ne kadar yürütüyor.</b>
        ///
        /// Sentetik graf kusuru gösterebilir ama ölçeği gösteremez.
        /// Gerçek grafta ölçüldü: görev başına **3.724 m** kuş uçuşu
        /// toplam, yani 2,2 m/s'de **28,2 dakika** yürüyüş. Oyunun tek
        /// tekrarlayan döngüsü bir turda yarım saat sürüyordu — ve
        /// 20 görevin 12'si aynı düğümden geçiyordu, çünkü Galata
        /// bileşeninde tek han var.
        ///
        /// Bu test o sayıyı bir <b>cırcır</b> olarak tutuyor: bugünkü
        /// değerden kötüye gitmesin. Hedef 900 m; oraya inmek aday
        /// süzgecinin yanında görev üretecinin yol bütçesini de
        /// istiyor ve o ayrı bir iş.
        /// </summary>
        [Test]
        public void QuestsInTheRealCityDoNotBecomeMarches()
        {
            var gercek = UnityEditor.AssetDatabase
                .LoadAssetAtPath<SokakGrafi>(
                    "Assets/_Project/Data/SG_Sehir.asset");
            Assert.IsNotNull(gercek, "SG_Sehir.asset yok.");

            // Gercek dogum noktasi (OyunSahnesiKur.BaslangicNoktasi
            // civari): Galata bilesenine dusuyor.
            var dogum = new Vector3(8f, 70f, 296f);

            var turler = (GorevArketip[])
                System.Enum.GetValues(typeof(GorevArketip));
            float toplam = 0f;
            int sayi = 0, enUzunGorev = 0;

            for (int i = 0; i < 20; i++)
            {
                var a = turler[i % turler.Length];
                if (!GorevUretici.Uygun(a, 1632, 121)) continue;
                var g = GorevUretici.Uret(gercek, a, dogum,
                                          1632 + i * 7919, 1632, 121);
                if (g == null || g.duraklar.Count == 0) continue;

                float yol = 0f;
                var son = dogum;
                foreach (int d in g.duraklar)
                {
                    yol += Vector3.Distance(son, gercek.dugumler[d].konum);
                    son = gercek.dugumler[d].konum;
                }
                toplam += yol;
                sayi++;
                if (yol > enUzunGorev) enUzunGorev = Mathf.RoundToInt(yol);
            }

            Assert.Greater(sayi, 5, "Gercek grafta gorev uretilemedi.");
            float ort = toplam / sayi;

            // CIRCIR: 3.724 -> 2.206 -> 1.923 m (28,2 -> 14,6 dk).
            //
            // Iki duzeltme: aday suzgeci sayidan mesafeye cevrildi
            // (3.724 -> 2.206) ve ureteca yol butcesi verildi
            // (2.206 -> 1.923). Ikincisi beklenenden az kazandirdi ve
            // sebebi ogretici: Galata bileseninde **tek han ve tek
            // iskele** var, yani sekiz aday dizinin cogu ayni duguma
            // dusuyor.
            //
            // Yani kalan darbogaz uretecte degil **sehrin kendisinde**.
            // 900 m hedefine inmenin yolu daha akilli bir ureteç
            // degil, Galata'ya gidilecek yer koymak — ya da gorev
            // arketiplerinin Han/Iskele sartini gevsetmek.
            Assert.Less(ort, 2000f,
                $"Gorev basina ortalama {ort:F0} m — KOTULESTI. "
                + "Olculen taban 1.923 m, hedef 900 m.");
            Debug.Log($"[Hezarfen] Gercek grafta gorev: ortalama "
                      + $"{ort:F0} m ({ort / 2.2f / 60f:F1} dk yuruyus), "
                      + $"en uzun {enUzunGorev} m, {sayi} gorev.");
        }

    }
}
