using Hezarfen.Arayuz;
using NUnit.Framework;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Atıf bir nezaket değil, yükümlülük.</b>
    ///
    /// OpenStreetMap verisi ODbL altında; atıf eksikse bu bir incelik
    /// kusuru değil <b>lisans ihlali</b>. Krediler ekranı bir gün yeniden
    /// yazılabilir, kısaltılabilir, çevrilebilir — ve atıf o sırada
    /// <b>sessizce</b> düşebilir. Bu test onu tutuyor.
    /// </summary>
    public class KredilerTests
    {
        /// <summary>
        /// <b>ODbL atfı yerinde.</b> CLAUDE.md'nin varlık kuralı da aynı
        /// yöne bakıyor: kaydı olmayan hiçbir şey kullanılmaz.
        /// </summary>
        [Test]
        public void TheOpenStreetMapAttributionIsPresent()
        {
            foreach (string gereken in Krediler.ZorunluAtif)
                Assert.IsTrue(Krediler.Metin.Contains(gereken),
                    $"Krediler metninde '{gereken}' YOK — OSM verisi ODbL "
                    + "altinda ve atif zorunlu. Bu bir incelik kusuru "
                    + "degil, lisans ihlali.");
        }

        /// <summary>CC0 kaynakları da anılıyor (kayıt kuralı, ADR yok).</summary>
        [Test]
        public void TheCreativeCommonsSourcesAreNamed()
        {
            foreach (string k in new[] { "Poly Haven", "Blender Studio",
                                         "Rålamb" })
                Assert.IsTrue(Krediler.Metin.Contains(k),
                    $"Krediler metninde '{k}' anilmiyor.");
        }

        /// <summary>
        /// <b>Kaynakça var</b> — PLAN Bölüm 13 bunu oyunun pazarlama gücü
        /// sayıyor: *"tarih meraklısı oyuncuya kaynakça vermek bu oyunun
        /// pazarlama gücüdür."*
        /// </summary>
        [Test]
        public void TheBibliographyIsThereBecauseItIsThePoint()
        {
            foreach (string k in new[] { "Evliya", "TDV", "RESEARCH.md" })
                Assert.IsTrue(Krediler.Metin.Contains(k),
                    $"Kaynakcada '{k}' yok.");
        }

        /// <summary>
        /// <b>Zirvenin tartışmalı olduğu SÖYLENİYOR.</b>
        ///
        /// Oyunun en çok inanılmak istediği yer, kaynağın en zayıf olduğu
        /// yer. Kodeks bunu zaten söylüyor (<c>Perde2Dilimi</c>);
        /// krediler de söylemeli — üç fazdır dipnot tutmanın anlamı bu.
        /// </summary>
        [Test]
        public void TheCreditsAdmitTheFlightIsContested()
        {
            Assert.IsTrue(Krediler.Metin.Contains("Evliya"),
                "Ucusun tek kaynagi anilmiyor.");
            Assert.IsTrue(Krediler.Metin.Contains("55:1"),
                "Suzulme orani celiskisi gizlenmis.");
            Assert.IsTrue(Krediler.Metin.Contains("1638"),
                "Tarih celiskisi (1632 mi 1638 mi) gizlenmis.");
        }

        /// <summary>
        /// <b>Kademe adları menüde ölçüldüğü gibi.</b>
        ///
        /// High Fidelity 1440p/60 vermiyor ve bu ölçüldü. Menüde ona "en
        /// iyisi" deyip oyuncuyu 50 FPS'e düşürmek dürüstlük olmazdı.
        /// </summary>
        [Test]
        public void TheQualityTiersTellTheTruthAboutTheirCost()
        {
            Assert.AreEqual(3, Ayarlar.KademeAciklamasi.Length);
            Assert.IsTrue(Ayarlar.KademeAciklamasi[0].Contains("1440p"),
                "High Fidelity'nin bedeli soylenmemis.");
            Assert.IsTrue(Ayarlar.KademeAciklamasi[1].Contains("1080p/60"),
                "Balanced'in olculen hedefi yazmiyor.");
        }
    }
}
