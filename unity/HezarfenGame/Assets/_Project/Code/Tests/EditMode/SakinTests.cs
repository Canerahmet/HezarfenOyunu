using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Şehirdeki insanla konuşulabiliyor mu, ve iş ondan mı geliyor.</b>
    ///
    /// Sekiz tur boyunca <see cref="IEtkilesim"/>'i üç sınıf uyguladı —
    /// dükkân, iskele, toplanabilir eşya — ve hiçbiri insan değildi.
    /// Kırk bin sakin, altmışı her an ekranda, beş binden fazla replik;
    /// ve oyuncu hiçbirine dokunamıyordu.
    ///
    /// İkinci ve daha ağır olan: görevi kimse vermiyordu.
    /// <c>GorevYonetici.Start()</c> oyunun sıfırıncı saniyesinde iş
    /// atıyor, biten işin yerine anında yenisini koyuyordu. Bir işin
    /// sahibi olması, o işin var olmasının şartı.
    /// </summary>
    public class SakinTests
    {
        private SokakGrafi _graf;
        private GameObject _oyuncuGo, _gorevGo, _kisiGo;
        private GorevYonetici _gorev;
        private Sakin _kisi;

        [SetUp]
        public void Kur()
        {
            _graf = UnityEditor.AssetDatabase.LoadAssetAtPath<SokakGrafi>(
                "Assets/_Project/Data/SG_Sehir.asset");

            _oyuncuGo = new GameObject("OYUNCU_T");
            _oyuncuGo.transform.position = new Vector3(8f, 70f, 296f);
            _oyuncuGo.AddComponent<Envanter>();

            _gorevGo = new GameObject("GOREV_T");
            _gorev = _gorevGo.AddComponent<GorevYonetici>();
            _gorev.graf = _graf;
            _gorev.oyuncu = _oyuncuGo.transform;
            _gorev.envanter = _oyuncuGo.GetComponent<Envanter>();

            _kisiGo = new GameObject("SAKIN_T");
            _kisi = _kisiGo.AddComponent<Sakin>();
            _kisi.gorev = _gorev;
            _kisi.ajan = new NPCAjan
            {
                konum = _oyuncuGo.transform.position,
                tohum = 4242,
            };
        }

        [TearDown]
        public void Yik()
        {
            if (_kisiGo != null) Object.DestroyImmediate(_kisiGo);
            if (_gorevGo != null) Object.DestroyImmediate(_gorevGo);
            if (_oyuncuGo != null) Object.DestroyImmediate(_oyuncuGo);
        }

        [Test]
        public void APersonIsSomethingYouCanInteractWith()
        {
            Assert.IsInstanceOf<IEtkilesim>(_kisi,
                "Sakin bir etkilesim degil — kalabalik yine dekor.");
            Assert.IsTrue(_kisi.Hazir, "Ajani olan sakin hazir degil.");
            Assert.IsNotEmpty(_kisi.Ipucu, "Ipucu bos.");
        }

        [Test]
        public void AnUnclaimedBodyIsNobody()
        {
            _kisi.ajan = null;
            Assert.IsFalse(_kisi.Hazir,
                "Havuza donmus govde hala konusuyor — oyuncu orada "
                + "olmayan biriyle konusur.");
            Assert.IsFalse(_kisi.Etkiles(_oyuncuGo));
        }

        [Test]
        public void TheWorkComesFromAPersonNotFromTheClock()
        {
            Assert.IsNull(_gorev.Simdiki,
                "Oyun issiz baslamiyor — is hala konveyorden geliyor.");

            Assert.IsTrue(_kisi.Etkiles(_oyuncuGo), "Is verilemedi.");
            Assert.IsNotNull(_gorev.Simdiki, "Konusuldu ama is gelmedi.");
            Assert.IsNotEmpty(_kisi.SonSoz, "Kimse bir sey soylemedi.");
        }

        [Test]
        public void TheJobStartsWhereThePersonStands()
        {
            _kisi.ajan.konum = _graf.dugumler[500].konum;
            Assert.IsTrue(_kisi.Etkiles(_oyuncuGo));

            var ilk = _graf.dugumler[_gorev.Simdiki.duraklar[0]].konum;
            float d = Vector3.Distance(ilk, _kisi.ajan.konum);
            Assert.Less(d, GorevUretici.YolButcesi,
                $"Ilk durak konusulan kisiden {d:F0} m otede. Is, onu "
                + "verenin durdugu yerden baslamali.");
        }

        [Test]
        public void SomeoneWithAJobInHandDoesNotHandOutAnother()
        {
            Assert.IsTrue(_kisi.Etkiles(_oyuncuGo));
            var ilk = _gorev.Simdiki;

            Assert.IsTrue(_kisi.Etkiles(_oyuncuGo),
                "Isi olan biriyle konusulamiyor — o zaman kalabalik "
                + "yalniz bir gorev dagiticisi olur.");
            Assert.AreSame(ilk, _gorev.Simdiki,
                "Elinde is varken ikinci is verildi.");
        }
    }
}
