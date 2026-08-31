using Hezarfen.City;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Ortam sesinin karışımı — çalmadan sınanır.</b>
    ///
    /// Sınanan şey ses çıkması değil, <b>hangi sesin ne zaman baskın
    /// olduğu</b>. Bir ortam sistemi sessizce yanlış karışabilir:
    /// kıyıdan 400 m içeride dalga sesi duyulursa mesafe duygusu
    /// ölür, gece çarşı uğultusu sürerse saat duygusu ölür. İkisi de
    /// kulakla fark edilir ama testle yakalanması daha ucuz.
    /// </summary>
    public class OrtamSesiTests
    {
        private OrtamSesi Kur(float yukseklik)
        {
            var go = new GameObject("OrtamSesiTest");
            go.AddComponent<AudioListener>();
            var o = go.AddComponent<OrtamSesi>();
            go.transform.position = new Vector3(0f, yukseklik, 0f);
            return o;
        }

        [TearDown]
        public void Sok()
        {
            foreach (var g in Object.FindObjectsByType<OrtamSesi>(
                         FindObjectsSortMode.None))
                Object.DestroyImmediate(g.gameObject);
        }

        [Test]
        public void TheSeaIsLoudAtTheShoreAndSilentInland()
        {
            var o = Kur(2f);
            o.Hedefler(out float kiyi, out _, out _, out _);

            // 3 x yukseklik vekili: 2 m -> 6 m "su uzakligi" (kiyida).
            Assert.Greater(kiyi, 0.8f,
                $"Kiyida deniz sesi {kiyi:0.00} — duyulmuyor.");

            var uzak = Kur(200f);   // vekil: 600 m ic bolge
            uzak.Hedefler(out float ic, out _, out _, out _);
            Assert.Less(ic, 0.05f,
                $"Ic bolgede deniz sesi {ic:0.00} — mesafe duygusu yok.");
        }

        [Test]
        public void TheCityFallsSilentAsYouClimb()
        {
            var yerde = Kur(1f);
            yerde.Hedefler(out _, out _, out float yerCarsi, out _);
            var yukarida = Kur(150f);
            yukarida.Hedefler(out _, out float yukRuzgar,
                              out float yukCarsi, out _);

            Assert.Less(yukCarsi, yerCarsi,
                "Yukseldikce sehir sesi kesilmeli — ucarken duyulan "
                + "sey ruzgardir.");
            Assert.Greater(yukRuzgar, 0.9f,
                $"150 m'de ruzgar {yukRuzgar:0.00} — baskin olmali.");
        }

        [Test]
        public void DayAndNightAreTheInverseOfEachOther()
        {
            var o = Kur(1f);
            o.Hedefler(out _, out _, out float carsi, out float gece);
            // Gunes yoksa Gunduz() 1 doner: carsi acik, gece kapali.
            Assert.Greater(carsi, 0.5f);
            Assert.Less(gece, 0.5f);
            Assert.That(carsi / 0.8f + gece, Is.EqualTo(1f).Within(0.02f),
                "Gunduz ve gece toplami sabit olmali; ikisi ayni "
                + "eksenin iki ucu.");
        }
        /// <summary>
        /// <b>Gerçek öznitelik katmanıyla su uzaklığı ölçülüyor mu.</b>
        ///
        /// Bu dosyanın öteki testleri <c>katman</c> alanını hiç
        /// atamıyor ve o yüzden hepsi <c>y * 3</c> <b>vekil dalını</b>
        /// ölçüyordu — testin kendi yorumu bunu itiraf ediyor.
        ///
        /// Sonuç: sekiz üretilmiş <c>AO_D_*.asset</c> çalışma zamanında
        /// hiç okunmuyordu ve bunu <b>üç faz boyunca kimse fark
        /// etmedi</b>, çünkü testler tam olarak eksik bağlantının
        /// üstünü örtüyordu. Yeşil bir test, ölçtüğü şeyin var
        /// olduğunu göstermez.
        /// </summary>
        [Test]
        public void TheRealAttributeLayerIsReadWhenItIsThere()
        {
            var katman = UnityEditor.AssetDatabase
                .LoadAssetAtPath<Hezarfen.Gis.AraziOznitelik>(
                    "Assets/_Project/Data/AO_D_Galata.asset");
            Assert.IsNotNull(katman, "AO_D_Galata.asset yok.");

            // Galata'nin icinde bir nokta: suya uzak olmali.
            float ic = katman.SuUzakligi(-1500f, -500f);
            Assert.GreaterOrEqual(ic, 0f, "Su uzakligi okunamadi.");

            // Vekil dal ile ayni sayiyi vermemeli: vekil yalnizca
            // yuksekligi okur ve konuma hic bakmaz.
            float baska = katman.SuUzakligi(-1200f, -300f);
            Assert.IsTrue(ic != baska || ic == Hezarfen.Gis.AraziOznitelik.Uzak,
                "Iki ayri konum ayni su uzakligini verdi — katman "
                + "okunmuyor olabilir.");
        }

    }
}
