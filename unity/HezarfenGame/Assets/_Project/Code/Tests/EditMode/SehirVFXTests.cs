using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEngine;

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

        /// <summary>
        /// <b>Martı beyaz görünsün, duman gri.</b>
        ///
        /// Işıksız (unlit) bir malzemede renk yansıma oranı değil,
        /// doğrudan <b>parlaklık</b>tır. Martılara 0,93 yazılmıştı ve
        /// gündüz gökyüzü bunun binlerce katı parlak; karede 24 martı
        /// gökten <b>koyu ince dilimler</b> olarak çıkıyordu. Bir kuşun
        /// mercekteki toz gibi okunması, rengin yanlış olduğunun en iyi
        /// kanıtı — nitekim önce çizim bozukluğu sandım.
        ///
        /// Bu, depoda üçüncü kez aynı sınıf kusur: duman malzemesiz
        /// olduğu için macentaydı, prefab yuvaları boş olduğu için
        /// macentaydı, martı ışıksız olduğu için siyahtı. Hiçbiri hata
        /// vermez; hepsi çizer.
        /// </summary>
        [Test]
        public void TheGullIsLitNotUnlitAndVisibleFromBelow()
        {
            var m = SehirVFX.MartiMalzeme();
            Assert.IsNotNull(m, "Marti malzemesi kurulamadi.");

            Assert.IsFalse(m.HasProperty("_UnlitColor"),
                "Marti ISIKSIZ bir malzeme kullaniyor. Isiksiz renk bir "
                + "parlaklik degeridir; gunduz gokyuzunun yaninda 0,93 "
                + "siyahtir. Kus gunes alsin.");
            Assert.IsTrue(m.HasProperty("_BaseColor"),
                "Marti malzemesinde _BaseColor yok — isikli bir "
                + "golgelendirici bekleniyordu.");

            // Marti cogu zaman ALTTAN gorunur; iki ucgenlik bir kanat
            // tek yuzluyse asagidan bakan oyuncu onu goremez.
            Assert.AreEqual(0f, m.GetFloat("_CullMode"), 0.01f,
                "Marti tek yuzlu — alttan bakinca kaybolur.");
        }

        /// <summary>
        /// Duman malzemesi <b>var</b> — malzemesiz bir parçacık
        /// sistemi HDRP'de sessizce macenta çizer.
        /// </summary>
        [Test]
        public void TheSmokeHasAMaterialAtAll()
        {
            Assert.IsNotNull(SehirVFX.DumanMalzeme(),
                "Duman malzemesi yok — HDRP bunu MACENTA cizer ve "
                + "hicbir uyari vermez.");
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
