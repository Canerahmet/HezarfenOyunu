using System.IO;
using System.Text.RegularExpressions;
using Hezarfen.Arayuz;
using Hezarfen.Editor.Pipeline;
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
    
        //: Kayitta "atif zorunlu" diye isaretlenmis her kaynak icin,
        //: krediler metninde gecmesi gereken ayirt edici bir simge.
        private static readonly string[] AtifSimgeleri =
        {
            "Copernicus",
            "OpenStreetMap",
        };

        /// <summary>
        /// <b>Kayıt ile ekran birbirine bağlı.</b>
        ///
        /// <c>refs/LICENSES.md</c> bir kaynağı "atıf zorunlu" diye
        /// işaretliyorsa, o kaynak krediler metninde de geçmek
        /// zorundadır. İkisi ayrı yerlerde durduğu sürece ayrışırlar —
        /// ve <b>ayrıştılar</b>: Copernicus DEM GLO-30 kayıtta "atıf
        /// zorunlu" yazılıydı, şart koşulan metin
        /// <c>tools/gis/dem_fetch.py</c> içinde duruyordu, krediler
        /// ekranında ise yalnızca "kamu erişimli DEM kaynakları" vardı —
        /// arazinin tamamı o veriden türetilmiş olmasına rağmen.
        ///
        /// Bu, bu projede defalarca çıkan "bir sayının iki sahibi varsa
        /// er ya da geç iki değeri olur" kusurunun lisans hâli. Oyun
        /// Steam'de ticari yayınlanacak; bu bir incelik değil, yayın
        /// koşulu.
        /// </summary>
        [Test]
        public void EveryAttributionTheRegisterDemandsIsOnScreen()
        {
            string yol = Path.Combine(AssetCatalog.RepoRoot, "refs", "LICENSES.md");
            Assert.IsTrue(File.Exists(yol), "Lisans kaydi yok: " + yol);
            string kayit = File.ReadAllText(yol);

            var desen = new Regex(@"(at[ıi]f\s+zorunlu|zorunlu\s+at[ıi]f)",
                                  RegexOptions.IgnoreCase);
            int bakilan = 0;
            foreach (string satir in kayit.Split('\n'))
            {
                // YALNIZ KAYIT SATIRLARI. Kayit bir tablodur; kuralin
                // KENDISINI anlatan duzyazi da "atif zorunlu" ifadesini
                // tasir ve ilk yazimda test kendi aciklamasini bir kaynak
                // sanip kirmizi yandi. Bir denetim, denetlediği metnin
                // hakkındaki cumleyi de olcuyorsa yanlis seyi olcuyor.
                if (!satir.TrimStart().StartsWith("|")) continue;
                if (!desen.IsMatch(satir)) continue;
                // "atif zorunlu DEGIL" bir yukumluluk degil, tam tersi.
                // Ilk yazimda desen bunu da yakaladi ve CC0 taban govde
                // satirini "taninmayan zorunlu kaynak" diye bildirdi:
                // olumsuzlamayi okumayan bir desen, kuralin tersini olcer.
                if (Regex.IsMatch(satir, @"zorunlu\s+de[gğ]il",
                                  RegexOptions.IgnoreCase)) continue;
                bakilan++;

                bool taniniyor = false;
                foreach (string simge in AtifSimgeleri)
                    if (satir.Contains(simge)) { taniniyor = true; break; }

                Assert.IsTrue(taniniyor,
                    "Kayitta atif ZORUNLU diye isaretli ama taninmayan bir "
                    + "kaynak var: " + satir.Trim()
                    + " -- Bu kaynagi AtifSimgeleri'ne ve krediler metnine "
                    + "ekle. Ticari yayin icin atif bir yukumluluktur.");
            }

            Assert.Greater(bakilan, 0,
                "Kayitta hic 'atif zorunlu' satiri bulunamadi — desen "
                + "bozulmus olabilir. Sessizce gecen bir denetim, denetim "
                + "degildir.");

            foreach (string simge in AtifSimgeleri)
                Assert.IsTrue(Krediler.Metin.Contains(simge),
                    "Kayit '" + simge + "' icin atif zorunlu diyor ama "
                    + "krediler metninde gecmiyor. Lisans ihlali.");
        }

        /// <summary>
        /// Şart koşulan metinler <b>birebir</b> geçiyor.
        ///
        /// Copernicus ve ODbL kendi cümlelerini dayatıyor; parafraz
        /// yeterli değil. Kayıt ikisini de yazılı tutuyor
        /// (<c>refs/LICENSES.md</c>, "ZORUNLU ATIF METNİ").
        /// </summary>
        [Test]
        public void ThePrescribedWordingIsVerbatim()
        {
            foreach (string parca in new[]
            {
                "Produced using Copernicus WorldDEM-30",
                "Airbus Defence and Space GmbH",
                "Contains information from OpenStreetMap",
                "Open Database License (ODbL)",
            })
                Assert.IsTrue(Krediler.Metin.Contains(parca),
                    "Sart kosulan metnin parcasi eksik: " + parca);
        }
    }
}
