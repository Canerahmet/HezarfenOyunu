using Hezarfen.Sehir;
using NUnit.Framework;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Bu ekonomide yaşanabiliyor mu?</b>
    ///
    /// Bir oyun ekonomisinin sayıları tek tek makul olup topluca
    /// yaşanmaz olabilir — ve bu, ancak oynarken fark edilen türden bir
    /// hatadır: oyuncu sürekli parasızdır, kimse sebebini bilmez.
    ///
    /// Buradaki testler tek tek sayılara değil <b>oranlara</b> bakıyor:
    /// bir günlük emek neye yeter, bir ceza kaç güne mal olur, geçiş
    /// ücreti günlük ulaşımı engelliyor mu.
    ///
    /// Çapa <b>tek</b> ve belgeli: Evliya'nın *"40 akçe yevmiye ile
    /// sipahi"*i. Geri kalanı ondan türüyor, yani biri değişirse hepsi
    /// birlikte değişir.
    /// </summary>
    public class EkonomiTests
    {
        /// <summary>Belgeli çapa yerinde duruyor.</summary>
        [Test]
        public void TheAnchorIsEvliyasSipahiWage()
        {
            Assert.AreEqual(40, Ekonomi.SipahiYevmiyesi,
                "Capa Evliya'nin '40 akce yevmiye ile sipahi' kaydidir.");
            Assert.AreEqual(70, Ekonomi.LagariYevmiyesi,
                "Lagari'nin odul yevmiyesi 70 akcedir ve olaganin ustudur.");
            Assert.Greater(Ekonomi.LagariYevmiyesi, Ekonomi.SipahiYevmiyesi,
                "Odul yevmiyesi olagandan yuksek olmali.");
        }

        /// <summary>
        /// <b>Günlük geçiş bir günlük emeği yemiyor.</b>
        ///
        /// Haliç'te köprü yok ve insanlar her gün karşıya geçiyor. Geçiş
        /// yevmiyenin belirgin bir kısmını yeseydi kimse geçmezdi ve
        /// ulaşım mekaniği ölü doğardı — kayık ağını kurup kimseyi
        /// bindirememek.
        /// </summary>
        [Test]
        public void ADailyCrossingDoesNotEatADaysWork()
        {
            float gunluk = Ekonomi.IrgatYevmiyesi;
            float gidisDonus = Ekonomi.KayikUcreti * 2;
            Assert.Less(gidisDonus / gunluk, 0.30f,
                $"Gidis-donus {gidisDonus} akce, yevmiye {gunluk} akce — "
                + "gunluk gecis emegin ucte birinden fazlasini yiyor.");
            Assert.Greater(Ekonomi.KayikUcreti, 0,
                "Gecis bedava olmamali; iskeleler tarifelidir.");
        }

        /// <summary>Boğaz geçişi Haliç'ten pahalı.</summary>
        [Test]
        public void TheLongCrossingCostsMore()
        {
            Assert.Greater(Ekonomi.PeremeUcreti, Ekonomi.KayikUcreti,
                "Bogaz'i gecmek Halic'i gecmekle ayni fiyat olamaz.");
            Assert.AreEqual(Ekonomi.KayikUcreti, Ekonomi.Ucret(600f),
                "Kisa gecis taban ucret olmali.");
            Assert.AreEqual(Ekonomi.PeremeUcreti, Ekonomi.Ucret(3000f),
                "Uzun gecis pereme ucreti olmali.");
            Assert.That(Ekonomi.Ucret(1600f),
                Is.InRange(Ekonomi.KayikUcreti, Ekonomi.PeremeUcreti),
                "Ara mesafe iki ucretin arasinda olmali.");
        }

        /// <summary>
        /// <b>Cezalar acıtıyor ama ödenebilir.</b>
        ///
        /// Ölçü akçe değil <b>gün</b>: "280 akçe" bir şey söylemez,
        /// "yirmi sekiz günlük yevmiye" söyler. Fenersiz gezmek birkaç
        /// gün, saray duvarına tırmanmak bir aya yakın olmalı — ama
        /// hiçbiri oyunu bitiren bir ceza değil.
        /// </summary>
        [Test]
        public void FinesHurtWithoutEndingTheGame()
        {
            float fener = Ekonomi.GunCinsinden(IhlalKurali.Ceza(
                Ihlal.FenersizGece));
            float mal = Ekonomi.GunCinsinden(IhlalKurali.Ceza(Ihlal.YasakMal));
            float bolge = Ekonomi.GunCinsinden(IhlalKurali.Ceza(
                Ihlal.YasakBolge));

            Assert.That(fener, Is.InRange(1f, 4f),
                $"Fenersiz gezme cezasi {fener:0.0} gunluk yevmiye — "
                + "bir uyari olmali, bir felaket degil.");
            Assert.That(mal, Is.InRange(3f, 10f),
                $"Yasak mal cezasi {mal:0.0} gunluk yevmiye.");
            Assert.That(bolge, Is.InRange(15f, 45f),
                $"Yasak bolge cezasi {bolge:0.0} gunluk yevmiye — sur "
                + "burcuna tirmanmak agir olmali ama oyunu bitirmemeli.");

            // Siralama: agirlik artikca ceza artmali.
            Assert.Less(IhlalKurali.Ceza(Ihlal.FenersizGece),
                        IhlalKurali.Ceza(Ihlal.YasakMal));
            Assert.Less(IhlalKurali.Ceza(Ihlal.YasakMal),
                        IhlalKurali.Ceza(Ihlal.YasakBolge));
        }

        /// <summary>
        /// <b>Bir günlük emek bir günlük hayata yetiyor.</b>
        ///
        /// Ekmek + gidiş-dönüş geçiş, yevmiyenin altında kalmalı; yoksa
        /// oyuncu çalıştıkça fakirleşir ve bunu kimse fark etmez, yalnız
        /// "para hiç birikmiyor" diye hissedilir.
        /// </summary>
        [Test]
        public void ADaysWorkCoversADaysLiving()
        {
            int gider = Ekonomi.GunlukEkmek + Ekonomi.KayikUcreti * 2;
            Assert.Less(gider, Ekonomi.IrgatYevmiyesi,
                $"Gunluk gider {gider} akce, yevmiye "
                + $"{Ekonomi.IrgatYevmiyesi} akce — calisan fakirlesiyor.");
            Assert.Greater(Ekonomi.IrgatYevmiyesi - gider, 2,
                "Gunluk emekten geriye neredeyse hic kalmiyor; hicbir sey "
                + "biriktirilemez ve ilerleme eksenleri anlamsizlasir.");
        }

        /// <summary>
        /// Kese <b>borca düşmüyor</b> ve ne olduğunu kaydediyor.
        ///
        /// 1632'de kesende ne varsa odur; eksiye düşen bir bakiye,
        /// olmayan bir kredi kurumu uydurmak olurdu.
        /// </summary>
        [Test]
        public void ThePurseNeverGoesIntoDebtAndKeepsLedger()
        {
            var k = new Kese(30);
            Assert.IsTrue(k.Ode(10));
            Assert.AreEqual(20, k.akce);

            // Yetmeyen odeme: eldeki kadari alinir, borc kalmaz.
            Assert.IsFalse(k.Ode(50),
                "Yetmeyen odeme basarili bildirildi.");
            Assert.AreEqual(0, k.akce, "Kese eksiye dustu.");

            k.Kazan(25);
            Assert.AreEqual(25, k.akce);
            Assert.AreEqual(25, k.Kazanilan);
            Assert.AreEqual(30, k.Harcanan,
                "Defter tutulmadi: harcanan 10 + 20 olmali.");
        }

        /// <summary>Her sayı çapadan türüyor — biri değişirse hepsi değişir.</summary>
        [Test]
        public void EveryNumberDerivesFromTheAnchor()
        {
            Assert.AreEqual(Ekonomi.SipahiYevmiyesi / 4,
                Ekonomi.IrgatYevmiyesi,
                "Irgat yevmiyesi capadan turemiyor; elle yazilmis bir sayi "
                + "capa degisince sessizce yanlis kalir.");
        }
    }
}
