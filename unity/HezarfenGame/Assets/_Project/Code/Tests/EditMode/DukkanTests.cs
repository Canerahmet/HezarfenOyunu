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
            Assert.AreEqual(0, Dukkan.Fiyat(EsyaTuru.KanatParcasi),
                "Kanat parcasi satilik — oysa Hezarfen'in kendi isi.");
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
