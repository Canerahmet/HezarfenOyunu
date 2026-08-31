using System.Collections.Generic;
using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Kese ve dokunma.</b>
    ///
    /// Envanterin sinavi kolay gorunur ("ekle, cikar") ve tam bu yuzden
    /// sessizce bozulur: kayittan gelen bozuk bir sayi negatif adet
    /// yaratir, bilinmeyen bir tur enum'a zorlanir, dolu kese sessizce
    /// tasar. Ucu de burada soruluyor — cunku ucu de kayit dosyasindan
    /// gelebilir ve kayit dosyasi oyuncunun makinesinde, bizim
    /// denetimimiz disinda yasiyor.
    /// </summary>
    public class EtkilesimTests
    {
        private static Envanter Yeni()
        {
            var go = new GameObject("kese");
            return go.AddComponent<Envanter>();
        }

        [Test]
        public void AddingAndRemovingKeepsTheCount()
        {
            var e = Yeni();
            Assert.AreEqual(0, e.Adet(EsyaTuru.Su));
            Assert.IsTrue(e.Ekle(EsyaTuru.Su));
            Assert.IsTrue(e.Ekle(EsyaTuru.Su));
            Assert.AreEqual(2, e.Adet(EsyaTuru.Su));
            Assert.IsTrue(e.Cikar(EsyaTuru.Su, 2));
            Assert.AreEqual(0, e.Adet(EsyaTuru.Su));
            Assert.AreEqual(0, e.TurSayisi,
                "Sifirlanan tur kesede kalmamali — bosalan kalem, "
                + "kaydi ve arayuzu gereksiz doldurur.");
            Object.DestroyImmediate(e.gameObject);
        }

        [Test]
        public void APurseThatIsFullSaysSoInsteadOfSwallowing()
        {
            var e = Yeni();
            for (int i = 0; i < Envanter.TurBasinaEnCok; i++)
                Assert.IsTrue(e.Ekle(EsyaTuru.Odun), $"{i}. odun girmedi.");
            Assert.IsFalse(e.Ekle(EsyaTuru.Odun),
                "Dolu kese 'tamam' dedi — oyuncu aldigini sanar, "
                + "almadigini sonra fark eder.");
            Assert.AreEqual(Envanter.TurBasinaEnCok, e.Adet(EsyaTuru.Odun));
            Object.DestroyImmediate(e.gameObject);
        }

        [Test]
        public void TakingMoreThanYouHaveFailsWithoutChangingAnything()
        {
            var e = Yeni();
            e.Ekle(EsyaTuru.Sebze, 2);
            Assert.IsFalse(e.Cikar(EsyaTuru.Sebze, 3));
            Assert.AreEqual(2, e.Adet(EsyaTuru.Sebze),
                "Basarisiz cikarma keseyi degistirdi — yarim islem, "
                + "hic islem yapmamaktan kotudur.");
            Object.DestroyImmediate(e.gameObject);
        }

        [Test]
        public void ThePurseSurvivesARoundTripThroughTheSaveFile()
        {
            var a = Yeni();
            a.Ekle(EsyaTuru.Su, 3);
            a.Ekle(EsyaTuru.Ekmek, 1);
            a.Ekle(EsyaTuru.KanatParcasi, 2);

            var v = new KayitVerisi { envanter = a.Serilestir() };
            string json = JsonUtility.ToJson(v);
            var geri = JsonUtility.FromJson<KayitVerisi>(json);

            var b = Yeni();
            b.Yukle(geri.envanter);
            Assert.AreEqual(3, b.Adet(EsyaTuru.Su));
            Assert.AreEqual(1, b.Adet(EsyaTuru.Ekmek));
            Assert.AreEqual(2, b.Adet(EsyaTuru.KanatParcasi));
            Assert.AreEqual(3, b.TurSayisi);

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
        }

        [Test]
        public void ACorruptSaveCannotPoisonThePurse()
        {
            var e = Yeni();
            // Elle bozulmus / eski surumden gelmis bir liste:
            // bilinmeyen tur, negatif adet, tasan adet, tek kalan eleman.
            e.Yukle(new List<int> { 99, 4, (int)EsyaTuru.Su, -5,
                                    (int)EsyaTuru.Odun, 5000, 3 });
            Assert.AreEqual(0, e.Adet((EsyaTuru)99));
            Assert.AreEqual(0, e.Adet(EsyaTuru.Su),
                "Negatif adet kesede sifir olmali.");
            Assert.AreEqual(Envanter.TurBasinaEnCok, e.Adet(EsyaTuru.Odun),
                "Tasan adet ust sinira kirpilmali.");
            Object.DestroyImmediate(e.gameObject);
        }

        [Test]
        public void AnEmptyBarrelStopsGivingAndSaysSo()
        {
            var go = new GameObject("kup");
            var kup = go.AddComponent<ToplanabilirEsya>();
            kup.tur = EsyaTuru.Su;
            kup.stok = 2;

            var oyuncu = new GameObject("oyuncu");
            oyuncu.AddComponent<Envanter>();

            Assert.IsTrue(kup.Hazir);
            Assert.IsTrue(kup.Etkiles(oyuncu));
            Assert.IsTrue(kup.Etkiles(oyuncu));
            Assert.IsFalse(kup.Hazir,
                "Iki kova su veren kup ucuncuyu de verdi — sonsuz "
                + "kaynak, dunyayi bir dugme tarlasina cevirir.");
            Assert.IsFalse(kup.Etkiles(oyuncu));
            Assert.AreEqual(2,
                oyuncu.GetComponent<Envanter>().Adet(EsyaTuru.Su));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(oyuncu);
        }

        [Test]
        public void SomethingWithNoPurseGetsNothingAndTheBarrelKeepsItsStock()
        {
            var go = new GameObject("kup");
            var kup = go.AddComponent<ToplanabilirEsya>();
            kup.stok = 1;

            var kedi = new GameObject("kedi");   // envanteri yok
            Assert.IsFalse(kup.Etkiles(kedi));
            Assert.IsTrue(kup.Hazir,
                "Kese tasimayan biri kupu bosaltti — envantere "
                + "girmeyen sey stoktan da dusmemeli.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(kedi);
        }
    }
}
