using Hezarfen.Sehir;
using Hezarfen.Zaman;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Vakit oyuncuya ulaşıyor mu.</b>
    ///
    /// <c>ZamanSistemi.VakitGirdi</c> olayının çalışma zamanında hiç
    /// abonesi yoktu — depoda yalnız tanımı ve bir test dosyası
    /// geçiyordu. Yani <c>VakitHesabi</c>'nin bütün doğruluğu (Hanefî
    /// ikindi, gerçek sapma, batıştan kurulan ezanî saat) oyuncuya
    /// köşede küçük bir yazı olarak ulaşıyordu.
    ///
    /// Bu dosya olayın <b>bir dinleyicisi olduğunu</b> ve dinleyicinin
    /// bir şey ürettiğini soruyor. Var olan <c>VakitTests</c> hesabın
    /// doğruluğunu ölçüyor; doğruluğun ulaşıp ulaşmadığını ölçmüyordu.
    /// </summary>
    public class VakitBildirimiTests
    {
        private GameObject _go;
        private VakitBildirimi _b;

        [SetUp]
        public void Kur()
        {
            _go = new GameObject("VAKIT_TEST");
            _b = _go.AddComponent<VakitBildirimi>();
        }

        [TearDown]
        public void Yik()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void EveryPrayerTimeHasAName()
        {
            foreach (VakitHesabi.Vakit v in
                     System.Enum.GetValues(typeof(VakitHesabi.Vakit)))
            {
                string ad = VakitBildirimi.Ad(v);
                Assert.IsNotEmpty(ad, $"{v} icin ad yok.");
                Assert.AreNotEqual(v.ToString(), ad,
                    $"{v} icin ad ceviri degil, enum adinin kendisi — "
                    + "oyuncu 'Yatsi' degil 'yatsı' okumali.");
            }
        }

        [Test]
        public void EnteringAPrayerTimeProducesSomethingToSee()
        {
            Assert.IsEmpty(_b.Bildirim, "Baslangicta bildirim var.");
            _b.Girdi(VakitHesabi.Vakit.Aksam);

            Assert.AreEqual("akşam", _b.Bildirim,
                "Vakit girdi ve ekranda hicbir sey belirmedi — olayin "
                + "dinleyicisi olmasi, dinleyicinin bir sey uretmesi "
                + "demek degil.");
            Assert.Greater(_b.Tazelik, 0.9f, "Bildirim taze degil.");
            Assert.AreEqual(1, _b.Duyurulan);
        }

        [Test]
        public void TheDayAnnouncesSixTimes()
        {
            foreach (VakitHesabi.Vakit v in
                     System.Enum.GetValues(typeof(VakitHesabi.Vakit)))
                _b.Girdi(v);

            Assert.AreEqual(6, _b.Duyurulan,
                "Bir gunde alti vakit duyurulmali; oyunun kimligi bu "
                + "ritim.");
        }

        [Test]
        public void WithoutAClipItStillWorksAndDoesNotThrow()
        {
            // Ezan kaydi lisansli bir kayit isi ve henuz yok. Sesin
            // olmamasi, ANIN olmamasini gerektirmemeli — ve eksik bir
            // klip hicbir seyi dusurmemeli.
            Assert.IsNull(_b.ezanKlibi);
            Assert.DoesNotThrow(() => _b.Girdi(VakitHesabi.Vakit.Sabah));
            Assert.IsNotEmpty(_b.Bildirim);
        }
    }
}
