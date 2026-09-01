using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Akçe bir yere gidiyor mu.</b>
    ///
    /// Ekonominin bir kaynağı vardı (görev ödülü) ve neredeyse hiç
    /// gideri yoktu. Kese şişiyor, `Ekonomi`'nin narh defterinden
    /// türetilmiş bütün özeni — sipahi yevmiyesi 40, ırgat 10, günlük
    /// ekmek 1 akçe — oyuncuya hiç görünmüyordu.
    ///
    /// Bir ekonomi tek yönlü çalışmaz: kazanılan şeyin harcanacak bir
    /// yeri yoksa kazanmak da bir şey ifade etmez.
    /// </summary>
    public class DukkanTests
    {
        private GameObject _dGo, _oGo, _gGo;
        private Dukkan _d;
        private Envanter _env;
        private GorevYonetici _gorev;

        [SetUp]
        public void Kur()
        {
            _oGo = new GameObject("OYUNCU_T");
            _env = _oGo.AddComponent<Envanter>();

            _gGo = new GameObject("GOREV_T");
            _gorev = _gGo.AddComponent<GorevYonetici>();

            _dGo = new GameObject("PF_Firin_T");
            _d = _dGo.AddComponent<Dukkan>();
            _d.envanter = _env;
            _d.gorev = _gorev;
            _d.satilan = EsyaTuru.Ekmek;
            _d.zaman = null;              // saat yok = hep acik
        }

        [TearDown]
        public void Yik()
        {
            if (_dGo != null) Object.DestroyImmediate(_dGo);
            if (_gGo != null) Object.DestroyImmediate(_gGo);
            if (_oGo != null) Object.DestroyImmediate(_oGo);
        }

        [Test]
        public void BreadCostsWhatTheNarhSaysItCosts()
        {
            Assert.AreEqual(Ekonomi.GunlukEkmek, Dukkan.Fiyat(EsyaTuru.Ekmek),
                "Ekmek fiyati narh defterinden gelmiyor.");
            Assert.Greater(Dukkan.Fiyat(EsyaTuru.Odun), 0);
            // KANAT PARCASI ARTIK SATILIK — VE OLMASI GEREKIYORDU.
            //
            // Bu satir once `0` sart kosuyordu, gerekcesi "Hezarfen'in
            // kendi isi". Sonucu olculdu: parca ne alinabiliyor, ne
            // satilabiliyor, dunyada sifir tane vardi ve hicbir gorev
            // tasitmiyordu — oyunun adini tasiyan aygitin parcasi
            // **yalniz bir enum adiydi**. Kese de bosalmiyordu: bir
            // oyuncu iki saatte ~130 akce biriktirip harcayacak bir sey
            // bulamadi.
            //
            // Kendi isi olmasi, malzemesini kendisinin yontugu anlamina
            // gelmiyor; ahsabi ve bezi carsidan alinir.
            Assert.AreEqual(25, Dukkan.Fiyat(EsyaTuru.KanatParcasi),
                "Kanat parcasi alinamiyor — kesenin gidecek yeri yok.");
            Assert.AreEqual(0, Dukkan.SatisFiyati(EsyaTuru.KanatParcasi),
                "Kanat parcasi geri satilabiliyor — geri alinabilen bir "
                + "yatirim yatirim degildir.");
        }

        [Test]
        public void BuyingTakesTheCoinAndGivesTheGoods()
        {
            _gorev.Kese.Kazan(5);
            Assert.AreEqual(0, _env.Adet(EsyaTuru.Ekmek));

            Assert.IsTrue(_d.Etkiles(_oGo), "Satin alinamadi.");
            Assert.AreEqual(1, _env.Adet(EsyaTuru.Ekmek), "Mal gelmedi.");
            Assert.AreEqual(5 - Ekonomi.GunlukEkmek, _gorev.Kese.akce,
                "Akce alinmadi — ekonomi tek yonlu kaliyor.");
        }

        [Test]
        public void AnEmptyPurseBuysNothing()
        {
            Assert.AreEqual(0, _gorev.Kese.akce);
            Assert.IsFalse(_d.Etkiles(_oGo), "Parasiz alisveris yapildi.");
            Assert.AreEqual(0, _env.Adet(EsyaTuru.Ekmek));
            Assert.AreEqual(0, _gorev.Kese.akce, "Kese eksiye dustu.");
        }

        [Test]
        public void TheShopBuysBackForLessThanItSells()
        {
            foreach (EsyaTuru t in System.Enum.GetValues(typeof(EsyaTuru)))
            {
                if (Dukkan.Fiyat(t) <= 0) continue;
                Assert.Less(Dukkan.SatisFiyati(t), Dukkan.Fiyat(t),
                    $"{t}: alis ve satis fiyati esit — oyuncuya sonsuz "
                    + "akce basan bir dongu acilir (al, sat, al).");
            }
        }

        [Test]
        public void SellingWhatYouGatheredTurnsItIntoCoin()
        {
            // Sehirde 15.815 toplanabilir esya var ve bugune kadar
            // hicbiri bir ise yaramiyordu.
            _env.Ekle(EsyaTuru.Odun, 2);
            int once = _gorev.Kese.akce;

            _d.Etkiles(_oGo);          // 1. basis: al (parasi yok, olmaz)
            _d.Etkiles(_oGo);          // 2. basis: sat

            Assert.AreEqual(1, _env.Adet(EsyaTuru.Odun), "Odun satilmadi.");
            Assert.Greater(_gorev.Kese.akce, once,
                "Satistan akce gelmedi — toplanan esya hala olu.");
        }

        /// <summary>
        /// <b>Aynı dükkâna sonsuz mal satılamaz.</b>
        ///
        /// Bir oyuncu on dakikada buldu: avludaki su küpünden bedava
        /// al, dükkâna sat, tekrar al. Şehirde 15.815 toplanabilir eşya
        /// var ve hepsi ~90 saniyede yenileniyor — akçe sonsuzdu.
        ///
        /// Kapatılan şey mekanik değil, tek bir dükkânın önünde durup
        /// para basmak. Şehri dolaşan oyuncu yine kazanır.
        /// </summary>
        [Test]
        public void ASingleShopWillNotBuyForever()
        {
            _env.Ekle(EsyaTuru.Odun, 20);

            int satilan = 0;
            for (int i = 0; i < 40; i++)
            {
                // Cift basis: kip al/sat arasinda donuyor.
                _d.Etkiles(_oGo);
                if (_d.Etkiles(_oGo)) satilan++;
            }

            Assert.LessOrEqual(satilan, Dukkan.GunlukAlim,
                $"Ayni dukkana bir gunde {satilan} kalem satildi; "
                + $"sinir {Dukkan.GunlukAlim}. Sinirsiz alim, bedava "
                + "esyayla birlesince sonsuz akce demek.");
            Assert.Greater(satilan, 0, "Dukkan hic almadi.");
        }

        /// <summary>
        /// <b>Akçenin gideceği bir yer var, ve karşılığı uçuşta.</b>
        ///
        /// Bir oyuncu iki saatte ~130 akçe biriktirdi ve harcayacak
        /// hiçbir şey bulamadı; en pahalı mal 3 akçeydi. Zincir artık
        /// kapanıyor: çalış → kanat parçası → daha uzağa uç.
        ///
        /// Bu test o zincirin <b>iki ucunu</b> birden tutar: parça
        /// alınabiliyor mu, ve alınan parçanın uçuşta bir karşılığı
        /// var mı. Yalnız fiyatı ölçmek, ödediği şeyin bir işe
        /// yaradığını söylemezdi — bu depoda tam o kusur defalarca
        /// çıktı.
        /// </summary>
        [Test]
        public void CoinBuysReachAndNotJustAnInventoryLine()
        {
            _gorev.Kese.Kazan(Dukkan.Fiyat(EsyaTuru.KanatParcasi) * 3);
            _d.satilan = EsyaTuru.KanatParcasi;

            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(_d.Etkiles(_oGo), $"{i + 1}. parca alinamadi.");
                _d.Etkiles(_oGo);   // kip sat'a doner, geri al
            }
            Assert.AreEqual(3, _env.Adet(EsyaTuru.KanatParcasi));

            // Ucusta karsiligi: surukleme duser.
            var go = new GameObject("KANAT_T");
            var g = go.AddComponent<Hezarfen.Flight.GlideController>();
            g.kanatParcasi = 3;
            float indirim = 1f - Hezarfen.Flight.GlideController
                                 .ParcaBasinaIndirim * 3;
            Assert.Less(indirim, 1f,
                "Uc parca surukleme indirimi getirmiyor — alinan sey "
                + "bir envanter satiri olarak kaliyor.");
            Assert.Greater(indirim, 0.5f,
                "Indirim asiri: kanat uc parcayla planore donuyor.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ShuttersCloseAtNight()
        {
            var zGo = new GameObject("ZAMAN_T");
            var z = zGo.AddComponent<Hezarfen.Zaman.ZamanSistemi>();
            z.yilinGunu = 121;
            z.saat = 23f;
            z.Yenile();
            _d.zaman = z;

            _gorev.Kese.Kazan(5);
            Assert.IsFalse(_d.Acik, "Gece yarisi kepenk acik.");
            Assert.IsFalse(_d.Etkiles(_oGo), "Kapali dukkandan alindi.");
            Assert.AreEqual(5, _gorev.Kese.akce, "Kapali dukkan para aldi.");

            Object.DestroyImmediate(zGo);
        }
    }
}
