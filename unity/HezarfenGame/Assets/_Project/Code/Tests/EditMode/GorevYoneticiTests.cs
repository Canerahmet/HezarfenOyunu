using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Görev oyuncuya ulaşıyor mu.</b>
    ///
    /// ## Neden bu test, var olanın üstüne
    ///
    /// `GorevTests` zaten vardı ve geçiyordu; Faz 6 kapısı onunla
    /// yeşil yanmıştı. Ama o test görevi <b>kendisi oynuyordu</b>:
    /// <c>while (!q.Bitti) q.DurakTamam();</c>. Yani ölçtüğü şey
    /// grafın tamamlanabilirliğiydi — bir düğümden ötekine yol var mı.
    /// Oyuncunun tamamlayabilirliği hiç sorulmamıştı: görevi kim
    /// veriyor, hedef nerede yazıyor, varış nasıl anlaşılıyor, akçe
    /// kimin kesesine giriyor.
    ///
    /// Bir kapının en tehlikeli hâli budur — yanlış şeyi ölçüp yeşil
    /// yanmak. Bu dosya eksik olan yarıyı soruyor.
    ///
    /// Ölçüm sahne kurmadan yapılıyor: küçük bir graf elle örülüyor ve
    /// oyuncu bir <c>Transform</c> olarak taşınıyor. Gerçek şehirde
    /// koşan sürüm ayrı bir iş (PlayMode) ve bu, ondan önce gelmeli:
    /// bir şeyi büyük dünyada ölçmek, küçük dünyada ölçemediğin şeyi
    /// bulmanı kolaylaştırmaz.
    /// </summary>
    public class GorevYoneticiTests
    {
        private GameObject _kok;
        private GorevYonetici _y;
        private Transform _oyuncu;
        private SokakGrafi _graf;

        /// <summary>
        /// Küçük ama <b>gerçekçi</b> bir graf: aynı bileşende, birbirine
        /// bağlı, farklı türde düğümler. Görev üreteci durak türlerine
        /// bakıyor; tek türden bir graf hiçbir arketip üretemez ve test
        /// "görev yok" diye kırmızı yanardı — yanlış sebeple.
        /// </summary>
        [SetUp]
        public void Kur()
        {
            _graf = ScriptableObject.CreateInstance<SokakGrafi>();
            void Ekle(Vector3 p, SokakGrafi.Tur t) =>
                _graf.dugumler.Add(new SokakGrafi.Dugum
                { konum = p, tur = t, semt = "D_Test" });

            Ekle(new Vector3(0f, 0f, 0f), SokakGrafi.Tur.Iskele);
            Ekle(new Vector3(60f, 0f, 0f), SokakGrafi.Tur.Dukkan);
            Ekle(new Vector3(120f, 0f, 0f), SokakGrafi.Tur.Han);
            Ekle(new Vector3(180f, 0f, 0f), SokakGrafi.Tur.Ev);
            Ekle(new Vector3(240f, 0f, 0f), SokakGrafi.Tur.Cesme);
            Ekle(new Vector3(300f, 0f, 0f), SokakGrafi.Tur.Firin);
            Ekle(new Vector3(360f, 0f, 0f), SokakGrafi.Tur.Mescit);

            for (int i = 0; i + 1 < _graf.dugumler.Count; i++)
            {
                _graf.kenarlar.Add(new SokakGrafi.Kenar
                { a = i, b = i + 1, uzunluk = 60f });
                _graf.kenarlar.Add(new SokakGrafi.Kenar
                { a = i + 1, b = i, uzunluk = 60f });
            }

            _kok = new GameObject("OYUNCU_TEST");
            _oyuncu = _kok.transform;
            // Oyuncu grafin USTUNDE degil, yaninda dursun.
            //
            // Once (0,0,0)'a konuyordu ve ilk durak da oradaki
            // iskeleydi: "hedefe mesafe > 0" testi kirmizi yandi ve
            // kod dogruydu — kurgu yanlisti. Sifir mesafe bir kusur
            // degil, oyuncunun zaten hedefte durmasiydi.
            _oyuncu.position = new Vector3(-40f, 0f, 25f);
            _kok.AddComponent<Envanter>();

            var yg = new GameObject("GOREV_TEST");
            _y = yg.AddComponent<GorevYonetici>();
            _y.graf = _graf;
            _y.oyuncu = _oyuncu;
            _y.envanter = _kok.GetComponent<Envanter>();
        }

        [TearDown]
        public void Yik()
        {
            if (_y != null) Object.DestroyImmediate(_y.gameObject);
            if (_kok != null) Object.DestroyImmediate(_kok);
            if (_graf != null) Object.DestroyImmediate(_graf);
        }

        [Test]
        public void APlayerIsGivenSomethingToDo()
        {
            _y.YeniGorev();
            Assert.IsNotNull(_y.Simdiki,
                "Oyuncuya hicbir gorev verilmedi — sehirde yapilacak "
                + "bir sey olmamasi, bu projenin en buyuk acigi.");
            Assert.Greater(_y.Simdiki.duraklar.Count, 0, "Gorev duraksiz.");
        }

        [Test]
        public void TheTargetHasAPlaceAndADistance()
        {
            _y.YeniGorev();
            Assert.IsNotNull(_y.HedefKonum,
                "Hedefin dunyada bir yeri yok — HUD ne gosterecek?");
            Assert.Greater(_y.HedefMesafe, 0f,
                "Hedefe mesafe olculemedi.");
        }

        [Test]
        public void WalkingToTheStopCompletesIt()
        {
            _y.YeniGorev();
            var g = _y.Simdiki;
            int oncekiDurak = g.siradaki;

            // Oyuncuyu hedefe TASI ve bir kare simule et.
            _oyuncu.position = _y.HedefKonum.Value;
            _y.Adimla();

            // Ya ayni gorevde bir sonraki durakta olmali, ya da gorev
            // bitip yenisi baslamis olmali. Ikisi de "varis algilandi"
            // demek; ucuncu bir sonuc yok.
            bool ilerledi = (_y.Simdiki == g && g.siradaki > oncekiDurak)
                            || _y.Bitirilen > 0;
            Assert.IsTrue(ilerledi,
                "Duraga varmak duragi tamamlamadi — gorev oyuncunun "
                + "hareketine hic bakmiyor demektir.");
        }

        [Test]
        public void FinishingAJobPaysAndStartsTheNextOne()
        {
            _y.YeniGorev();
            Assert.IsNotNull(_y.Simdiki);
            int basKese = _y.Kese.akce;

            // Butun duraklari sirayla yuru.
            for (int adim = 0; adim < 20 && _y.Bitirilen == 0; adim++)
            {
                var k = _y.HedefKonum;
                if (k == null) break;
                _oyuncu.position = k.Value;
                _y.Adimla();
            }

            Assert.AreEqual(1, _y.Bitirilen,
                "Butun duraklara varildi ama gorev bitmedi.");
            Assert.Greater(_y.Kese.akce, basKese,
                "Gorev bitti ve kese bos — akce hicbir yerde "
                + "kazanilmiyorsa ekonomi bir sabitler dosyasidir, "
                + "oyun ekonomisi degil.");
            // DONGU KAPANIR, AMA BIR INSANDAN GECEREK.
            //
            // Bu satir once `IsNotNull` diyordu ve o zaman dogruydu:
            // biten gorevin yerine yoneticinin kendisi aninda yenisini
            // koyuyordu. O tasarim degisti — is artik `Sakin`'den, yani
            // konusulan kisiden geliyor; cunku kimin isi, kim sordu,
            // neden sorularinin cevabi olmadan bir gorev bir konveyor
            // bandidir.
            //
            // Test gevsetilmiyor: ayni iddia — "dongu kapaniyor" —
            // yeni yolundan olculuyor.
            Assert.IsNull(_y.Simdiki,
                "Gorev bitti ve yerine kendiliginden yenisi kondu. "
                + "Bir isin bitmesi bir an olmali.");
            Assert.IsTrue(_y.IsIste(_oyuncu.position),
                "Bitirdikten sonra yeni is istenemiyor — dongu "
                + "kapanmiyor.");
            Assert.IsNotNull(_y.Simdiki, "Is istendi ama gelmedi.");
        }

        [Test]
        public void CarriedGoodsPassThroughTheInventory()
        {
            _y.YeniGorev();
            var tur = GorevYonetici.YukTuru(_y.Simdiki.arketip);
            var env = _kok.GetComponent<Envanter>();
            Assert.AreEqual(0, env.Adet(tur));

            // Ilk duraga var: yuk alinmali.
            _oyuncu.position = _y.HedefKonum.Value;
            _y.Adimla();
            Assert.AreEqual(1, env.Adet(tur),
                "Ilk durakta yuk alinmadi — envanter hala hicbir ise "
                + "yaramiyor demektir.");
        }
    }
}
