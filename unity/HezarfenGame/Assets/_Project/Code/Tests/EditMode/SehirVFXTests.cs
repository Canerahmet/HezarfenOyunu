using Hezarfen.Sehir;
using NUnit.Framework;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Havanın günle uyumu.</b>
    ///
    /// Duman ve martı, oyunun "yaşıyor" hissinin ucuz kısmıdır ve tam
    /// bu yüzden yanlış saatte çalışırsa pahalıya patlar: gece yarısı
    /// tüten yüz baca ya da karanlıkta daire çizen martı, izleyene
    /// "kuş var" demez, "burası bozuk" der. Eğri burada sayıyla
    /// soruluyor — göz kararıyla değil.
    /// </summary>
    public class SehirVFXTests
    {
        [Test]
        public void HearthsBurnAtMealTimesAndCoolAtNoon()
        {
            float sabah = SehirVFX.OcakYogunlugu(6.5f);
            float ogle = SehirVFX.OcakYogunlugu(12.0f);
            float aksam = SehirVFX.OcakYogunlugu(18.0f);

            Assert.Greater(sabah, 0.9f, "Sabah ekmek pisirilmiyor.");
            Assert.Greater(aksam, 0.9f, "Aksam yemegi pisirilmiyor.");
            Assert.Less(ogle, 0.45f,
                $"Ogleyin yogunluk {ogle:0.00} — bacalar ogle vakti "
                + "cogunlukla soguktur; her saat tuten sehir, "
                + "hicbir saat bir sey anlatmaz.");
        }

        [Test]
        public void EvenTheDeadOfNightKeepsAFewEmbers()
        {
            float gece = SehirVFX.OcakYogunlugu(2.0f);
            Assert.Greater(gece, 0.05f,
                "Gece yarisi TEK bir baca bile tutmuyor — sehir "
                + "kapanmis gibi okunur.");
            Assert.Less(gece, 0.3f, "Gece yarisi sehir yemek pisiriyor.");
        }

        [Test]
        public void TheDensityCurveNeverLeavesItsRange()
        {
            for (float s = 0f; s <= 24f; s += 0.25f)
            {
                float y = SehirVFX.OcakYogunlugu(s);
                Assert.That(y, Is.InRange(0f, 1f),
                    $"Saat {s:0.00} icin yogunluk {y:0.00} — havuz "
                    + "boyutuyla carpilan bir sayi 0-1 disina cikamaz.");
            }
        }

        [Test]
        public void GullsSleepAtNightAndFlyByDay()
        {
            Assert.IsTrue(SehirVFX.MartiGecesi(3f), "Gece marti uculuyor.");
            Assert.IsTrue(SehirVFX.MartiGecesi(22f), "Gece marti uculuyor.");
            Assert.IsFalse(SehirVFX.MartiGecesi(9f), "Gunduz marti yok.");
            Assert.IsFalse(SehirVFX.MartiGecesi(17f), "Ikindi martisi yok.");
        }
    }
}
