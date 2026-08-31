using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Karşıya geçilebiliyor mu.</b>
    ///
    /// Oyuncu şehrin %19,7'sinde hapisti: yaya grafı dört ayrık
    /// bileşene bölünüyor ve doğum noktası Galata'nın 304 düğümünde.
    /// Üsküdar'ın 2.328 evi ve Sûriçi'nin tamamı yürüyerek erişilemez —
    /// ve bu <b>doğru</b>, çünkü 1632'de Haliç'te köprü yok. Eksik olan
    /// tasarım değil, tasarımın karşılığı olan mekanikti.
    ///
    /// Her parçası zaten yazılmıştı — kayık kenarı, kayıklı yol arama,
    /// mesafeye göre ücret, kese — ve dördü de yalnız testlerde
    /// çağrılıyordu. Bu dosya, o parçaların artık <b>oyuncuya</b>
    /// bağlandığını soruyor.
    /// </summary>
    public class PermeTests
    {
        private SokakGrafi _graf;
        private GameObject _iskele, _oyuncu, _gorevGo;
        private Perme _perme;
        private GorevYonetici _gorev;

        /// <summary>
        /// İki ayrı kıyı, aralarında yalnız kayık kenarı: yürünmez,
        /// geçilir. Gerçek şehrin küçültülmüş hâli.
        /// </summary>
        [SetUp]
        public void Kur()
        {
            _graf = ScriptableObject.CreateInstance<SokakGrafi>();
            void Ekle(Vector3 p, SokakGrafi.Tur t, string semt) =>
                _graf.dugumler.Add(new SokakGrafi.Dugum
                { konum = p, tur = t, semt = semt });

            Ekle(new Vector3(0f, 0f, 0f), SokakGrafi.Tur.Iskele, "D_Galata");
            Ekle(new Vector3(700f, 0f, 0f), SokakGrafi.Tur.Iskele, "D_Uskudar");
            Ekle(new Vector3(40f, 0f, 0f), SokakGrafi.Tur.Cesme, "D_Galata");

            _graf.kenarlar.Add(new SokakGrafi.Kenar
            { a = 0, b = 1, uzunluk = 700f, kayik = true });
            _graf.kenarlar.Add(new SokakGrafi.Kenar
            { a = 1, b = 0, uzunluk = 700f, kayik = true });
            _graf.kenarlar.Add(new SokakGrafi.Kenar
            { a = 0, b = 2, uzunluk = 40f });

            _oyuncu = new GameObject("OYUNCU_TEST");
            _gorevGo = new GameObject("GOREV_TEST");
            _gorev = _gorevGo.AddComponent<GorevYonetici>();
            _gorev.graf = _graf;
            _gorev.oyuncu = _oyuncu.transform;

            _iskele = new GameObject("PF_Iskele_TEST");
            _perme = _iskele.AddComponent<Perme>();
            _perme.graf = _graf;
            _perme.gorev = _gorev;
            _perme.geceKapali = false;   // testte saat yok
            _perme.Kur();
        }

        [TearDown]
        public void Yik()
        {
            if (_iskele != null) Object.DestroyImmediate(_iskele);
            if (_gorevGo != null) Object.DestroyImmediate(_gorevGo);
            if (_oyuncu != null) Object.DestroyImmediate(_oyuncu);
            if (_graf != null) Object.DestroyImmediate(_graf);
        }

        [Test]
        public void TheJettyKnowsWhereItIsAndWhereItCanGo()
        {
            Assert.AreEqual(0, _perme.Dugum,
                "Iskele kendi graf dugumunu bulamadi.");
            Assert.AreEqual(1, _perme.Hedef,
                "Karsi iskele bulunamadi — kayik kenari okunmuyor.");
        }

        [Test]
        public void TheFareComesFromTheDistanceNotFromNowhere()
        {
            // 700 m = kisa gecis = taban ucret.
            Assert.AreEqual(Ekonomi.KayikUcreti, _perme.Ucret,
                "Ucret mesafeden turemiyor.");
            Assert.IsTrue(_perme.Ipucu.Contains("akçe"),
                $"Ipucu ucreti soylemiyor: '{_perme.Ipucu}'");
            Assert.IsTrue(_perme.Ipucu.Contains("Uskudar"),
                $"Ipucu nereye gidildigini soylemiyor: '{_perme.Ipucu}'");
        }

        [Test]
        public void PayingCarriesYouAcross()
        {
            _gorev.Kese.Kazan(10);
            _oyuncu.transform.position = Vector3.zero;

            Assert.IsTrue(_perme.Etkiles(_oyuncu), "Gecis yapilamadi.");
            Assert.AreEqual(9, _gorev.Kese.akce, "Ucret alinmadi.");
            Assert.AreEqual(700f, _oyuncu.transform.position.x, 1f,
                "Oyuncu karsiya gecmedi — mekanik bagli degil demektir.");
        }

        [Test]
        public void AnEmptyPurseDoesNotPayAndDoesNotCross()
        {
            _oyuncu.transform.position = Vector3.zero;
            Assert.AreEqual(0, _gorev.Kese.akce);

            Assert.IsFalse(_perme.Etkiles(_oyuncu),
                "Parasiz gecis yapildi.");
            Assert.AreEqual(0, _gorev.Kese.akce,
                "Kese eksiye dusmedi ama para alindi — `Kese.Ode` "
                + "yetmeyen odemede eldekini ALIR; gecis once `Yeter` "
                + "sormali, yoksa oyuncu gecemedigi bir gecis icin "
                + "para kaybeder.");
            Assert.AreEqual(0f, _oyuncu.transform.position.x, 0.01f,
                "Odenmeden gecildi.");
        }

        [Test]
        public void AtNightTheBoatDoesNotRun()
        {
            _perme.geceKapali = true;
            var zamanGo = new GameObject("ZAMAN_TEST");
            var z = zamanGo.AddComponent<Hezarfen.Zaman.ZamanSistemi>();
            z.saat = 2f;                       // gece yarisi
            z.yilinGunu = 121;
            z.Yenile();
            _perme.zaman = z;

            _gorev.Kese.Kazan(10);
            Assert.IsFalse(_perme.Hazir, "Gece kayik calisiyor.");
            Assert.IsFalse(_perme.Etkiles(_oyuncu), "Gece gecildi.");
            Assert.AreEqual(10, _gorev.Kese.akce, "Gece para alindi.");

            Object.DestroyImmediate(zamanGo);
        }
    }
}
