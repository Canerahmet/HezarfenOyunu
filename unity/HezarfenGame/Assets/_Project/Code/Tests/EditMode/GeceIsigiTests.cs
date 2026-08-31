using Hezarfen.Player;
using Hezarfen.Zaman;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Gece siyah olmasın.</b>
    ///
    /// Gece karesi yakalandı ve 78 KB'lık bir PNG çıktı — neredeyse tek
    /// renk, yani tamamen siyah. Sebep tekti ve ölçüldü: sahnede bir
    /// ışık var, güneş batınca kapanıyor, geriye hiçbir kaynak
    /// kalmıyor. Bu test o boşluğun bir daha sessizce açılmamasını
    /// sağlıyor.
    ///
    /// Sorular sahne kurmadan sorulabiliyor çünkü ay evresi ve
    /// titreme SAF fonksiyonlar. Bir aydınlatma kararını sahneye
    /// bağlamak, onu ancak sahne kurulunca ölçülebilir kılardı — ve o
    /// zaman da hiç ölçülmezdi.
    /// </summary>
    public class GeceIsigiTests
    {
        [Test]
        public void TheMoonGoesThroughItsWholeCycleInAMonth()
        {
            // Referans yeni aydan tam yarim ay sonra dolunay olmali.
            double yeni = AyIsigi.Evre(AyIsigi.ReferansYeniAy);
            double dolu = AyIsigi.Evre(
                AyIsigi.ReferansYeniAy + (int)(AyIsigi.SinodikAy / 2));

            Assert.Less(yeni, 0.02, $"Yeni ay gunu evre {yeni:0.000} — "
                                    + "sayim referanstan kaymis.");
            Assert.That(dolu, Is.InRange(0.47, 0.53),
                $"Yarim ay sonra evre {dolu:0.000} — dolunay olmali.");
        }

        [Test]
        public void EveryDayOfTheYearHasALegalPhase()
        {
            for (int g = 1; g <= 365; g++)
            {
                double e = AyIsigi.Evre(g);
                Assert.That(e, Is.InRange(0.0, 1.0),
                    $"{g}. gun evre {e:0.000} — cember disina cikti.");
            }
        }

        [Test]
        public void AFullMoonIsMuchBrighterThanAHalfMoon()
        {
            float dolunay = AyIsigi.Aydinlik(0.5);
            float yarim = AyIsigi.Aydinlik(0.25);
            float yeniay = AyIsigi.Aydinlik(0.0);

            Assert.Greater(dolunay, 0.98f, "Dolunay tam aydinlik degil.");
            Assert.Less(yeniay, 0.02f, "Yeni ay isik veriyor.");
            // Yarim ay dolunayin YARISI degil, onda biri kadar aydinlatir.
            Assert.Less(yarim, dolunay * 0.35f,
                $"Yarim ay {yarim:0.00}, dolunay {dolunay:0.00} — egri "
                + "dogrusal kalmis; oysa ayin parlakligi dolunaya "
                + "yaklasirken hizla artar ve bu, bazi gecelerin "
                + "gercekten daha karanlik olmasini saglayan sey.");
        }

        [Test]
        public void EvenANewMoonNightHasSomeSkyGlow()
        {
            var go = new GameObject("AY");
            go.AddComponent<Light>();
            var ay = go.AddComponent<AyIsigi>();
            Assert.Greater(ay.tabanLuks, 0.01f,
                "Yeni ayda taban isik sifir — o gece ekran siyah olur "
                + "ve oyuncu bunu tarihsel sadakat degil, hata sanar.");
            Assert.Less(ay.tabanLuks, ay.dolunayLuks,
                "Taban isik dolunaydan parlak.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TheLanternFlickersWithoutEverGoingOut()
        {
            float enAz = 9f, enCok = 0f;
            for (float t = 0f; t < 60f; t += 0.05f)
            {
                float k = Fener.Titreme(t);
                enAz = Mathf.Min(enAz, k);
                enCok = Mathf.Max(enCok, k);
            }
            Assert.Greater(enAz, 0.6f,
                $"Fener {enAz:0.00} katsayisina kadar kisiliyor — "
                + "sonup yanan bir isik, titreyen bir alev degil "
                + "bozuk bir ampul okur.");
            Assert.Less(enCok, 1.2f, "Fener patliyor.");
            Assert.Greater(enCok - enAz, 0.05f,
                "Hic titremiyor — sabit nokta isik ates degil, ampul.");
        }
    }
}
