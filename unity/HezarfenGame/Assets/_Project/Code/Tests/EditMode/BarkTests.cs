using System.Collections.Generic;
using System.Linq;
using Hezarfen.Sehir;
using Hezarfen.Zaman;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Katman 2 korpusu gerçekten konuşabiliyor mu.</b>
    ///
    /// Korpusun sessiz hataları üç türlü: (1) bir meslek hiç replik almaz
    /// ve oyunda hiç konuşmaz, (2) bir replik yanlış tarihte söylenir —
    /// 1632'de olmamış bir yangından bahsedilir ya da 1634'te kapatılmış
    /// bir kahvehaneye çağrılır, (3) bir dönem hatası korpusa sızar ve
    /// beş bin satırın içinde kimse görmez.
    ///
    /// Üçü de <b>sayılabilir</b>, o yüzden üçü de burada.
    /// </summary>
    public class BarkTests
    {
        [SetUp] public void Setup() => BarkKorpusu.Unut();

        private const int BirMayis1632 = 122;

        /// <summary>Korpus yükleniyor ve "binlerce satır" gerçekten var.</summary>
        [Test]
        public void TheCorpusIsActuallyThousandsOfLines()
        {
            var hepsi = BarkKorpusu.Hepsi;
            Assert.Greater(hepsi.Length, 3000,
                $"Korpusta {hepsi.Length} replik var; plan Bolum 11.3 "
                + "\"binlerce satirlik ambiyans repligi korpusu\" diyor.");

            Assert.IsTrue(hepsi.All(r => !string.IsNullOrWhiteSpace(r.metin)),
                "Bos replik var.");
            Assert.IsTrue(hepsi.All(r => !string.IsNullOrWhiteSpace(r.kaynak)),
                "Kaynaksiz replik var — her replik bir seye dayanmali.");
            Assert.AreEqual(hepsi.Length, hepsi.Select(r => r.id).Distinct().Count(),
                "Replik kimlikleri benzersiz degil.");
        }

        /// <summary>
        /// <b>Her meslek konuşuyor — ve tekrar etmeyecek kadar.</b>
        ///
        /// Toplam sayı bir şeyi gizler. İlk üretimde 4676 replik vardı ve
        /// "binlerce" ölçüsü karşılanmış görünüyordu; ama 4234'ü esnafındı
        /// ve dilencinin sekiz repliği vardı. Oyuncu bir dakikada hepsini
        /// duyar ve şehir bir teybe döner. Önemli olan toplam değil
        /// <b>kişi başına</b> çeşitlilik.
        /// </summary>
        [Test]
        public void EveryTradeHasEnoughToSayToNotRepeat()
        {
            const int Taban = 40;
            var say = new Dictionary<NPCMeslek.Tip, int>();
            foreach (NPCMeslek.Tip t in System.Enum.GetValues(
                         typeof(NPCMeslek.Tip)))
                say[t] = 0;

            foreach (var r in BarkKorpusu.Hepsi)
            {
                Assert.That(r.meslek, Is.InRange(0, say.Count - 1),
                    $"Replik {r.id} tanimsiz bir meslege ait ({r.meslek}) — "
                    + "korpus ureticisiyle NPCMeslek.Tip ayrismis.");
                say[(NPCMeslek.Tip)r.meslek]++;
            }

            var ince = say.Where(k => k.Value < Taban)
                          .Select(k => $"{k.Key}={k.Value}").ToList();
            Assert.IsEmpty(ince,
                $"Cesitlilik tabani {Taban} altinda: {string.Join(", ", ince)} "
                + "— bu meslekler oyunda kendini tekrar eder.");
        }

        /// <summary>
        /// <b>Dönem hatası korpusa sızmamış.</b>
        ///
        /// Bu denetim üreticide de var. İki kere yazılmasının sebebi
        /// tembellik değil: üretici <b>üretilen</b> metni denetler, bu test
        /// <b>gemiye giden</b> veriyi. Aradaki fark elle düzenlenmiş ya da
        /// eski bir korpus dosyasıdır — ve o dosya üreticiden hiç geçmez.
        /// </summary>
        [Test]
        public void NoAnachronismSlippedIntoTheShippedCorpus()
        {
            // Her biri bir sebeple burada; sebep testin mesajinda.
            var yasak = new Dictionary<string, string>
            {
                { "tulumba", "ilk tulumba teskilati 1720'ler" },
                { "patates", "Yeni Dunya bitkisi" },
                { "domates", "Yeni Dunya bitkisi" },
                { "cay", "Osmanli'da yayginlasmasi 19-20. yy" },
                { "kibrit", "modern kibrit 19. yy" },
                { "gazete", "ilk Osmanli gazetesi 19. yy" },
                { "vapur", "buharli gemi 19. yy" },
                { "polis", "kolluk subasi/asesbasi/yeniceridir" },
                { "karakol", "19. yy kurumu" },
                { "lokanta", "gec donem" },
                { "banka", "gec donem" },
                { "sigara", "gec donem" },
                { "dakika", "gundelik dilde olcu birimi degil" },
            };

            foreach (var r in BarkKorpusu.Hepsi)
            {
                string d = Sade(r.metin);
                foreach (var y in yasak)
                    Assert.IsFalse(d.Contains(y.Key),
                        $"{r.id} \"{r.metin}\" icinde '{y.Key}' — {y.Value}.");

                // "kopru yok" DOGRUDUR; "kopruden gec" donem hatasidir.
                // Yasak olan sozcuk degil IDDIA.
                Assert.IsFalse(d.Contains("kopruden") || d.Contains("kopruye"),
                    $"{r.id} \"{r.metin}\" — 1632'de Halic'te kopru YOK.");
            }
        }

        private static string Sade(string s)
            => s.ToLowerInvariant()
                .Replace("ı", "i").Replace("ş", "s").Replace("ğ", "g")
                .Replace("ü", "u").Replace("ö", "o").Replace("ç", "c");

        /// <summary>
        /// <b>Olmamış bir olaydan bahsedilmiyor.</b>
        ///
        /// Cibali yangını 26 Ağustos 1633'tür. 1632'de oynayan bir oyuncu
        /// onu duyarsa oyun geleceği biliyor demektir — ve bu, kronolojiyi
        /// kurma zahmetinin tamamını boşa çıkarır.
        /// </summary>
        [Test]
        public void NobodyTalksAboutSomethingThatHasNotHappenedYet()
        {
            bool Gecer(string parca, int yil, int gun)
            {
                foreach (NPCMeslek.Tip m in System.Enum.GetValues(
                             typeof(NPCMeslek.Tip)))
                    foreach (VakitHesabi.Vakit v in System.Enum.GetValues(
                                 typeof(VakitHesabi.Vakit)))
                        foreach (var r in BarkKorpusu.Havuz(m, v, yil, gun,
                                                            false))
                            if (Sade(r.metin).Contains(parca)) return true;
                return false;
            }

            // OLCU: "cibali" YETMEZ — Cibali ayni zamanda bir SEMTTIR
            // ve siradan dedikoduda gecer ("Yine hastalik varmis
            // Cibali'te"). Ilk yazimda test bu yuzden patladi ve
            // patlamasi hakliydi: bozuk olan veri degil OLCME BICIMIYDI.
            // Aranan sey semtin adi degil OLAYIN kendisi.
            Assert.IsFalse(Gecer("kalafatci", 1632, BirMayis1632),
                "1632'de Cibali yangininden bahsediliyor — yangin 26 "
                + "Agustos 1633'tur.");
            Assert.IsFalse(Gecer("ferman cikti", 1632, BirMayis1632),
                "1632'de kahvehane fermanindan bahsediliyor — ferman 2 "
                + "Eylul 1633'tur.");

            // 1634: ikisi de OLDU ve konusuluyor.
            Assert.IsTrue(Gecer("kalafatci", 1634, BirMayis1632),
                "Yangindan sonra kimse yangindan bahsetmiyor.");

            // Subat 1632 isyani: 7 Subat'tan ONCE yok, SONRA var.
            Assert.IsFalse(Gecer("atmeydani", 1632, 20),
                "20 Ocak 1632'de Atmeydani isyanindan bahsediliyor — isyan "
                + "7 Subat'tir.");
            Assert.IsTrue(Gecer("atmeydani", 1632, 60),
                "Isyandan sonra kimse ondan bahsetmiyor.");
        }

        /// <summary>
        /// <b>Kapatılmış bir yere çağrı yapılmıyor.</b>
        ///
        /// 2 Eylül 1633'ten sonra kahvehane yok. "Akşam kahvehanede miyiz?"
        /// 1632'de sıradan bir cümle, 1634'te yıkılmış bir binaya davettir.
        /// Aynı korpus, farklı yıl, farklı şehir.
        /// </summary>
        [Test]
        public void NobodyIsInvitedToACoffeehouseThatWasTornDown()
        {
            List<string> Metinler(int yil, int gun)
            {
                var liste = new List<string>();
                foreach (NPCMeslek.Tip m in System.Enum.GetValues(
                             typeof(NPCMeslek.Tip)))
                    foreach (VakitHesabi.Vakit v in System.Enum.GetValues(
                                 typeof(VakitHesabi.Vakit)))
                        foreach (var r in BarkKorpusu.Havuz(m, v, yil, gun,
                                                            false))
                            liste.Add(Sade(r.metin));
                return liste;
            }

            var once = Metinler(1632, BirMayis1632);
            var sonra = Metinler(1634, BirMayis1632);

            Assert.IsTrue(once.Any(t => t.Contains("kahve")),
                "1632'de kahveden hic bahsedilmiyor — kahvehaneler ACIK.");
            Assert.IsFalse(
                sonra.Any(t => t.Contains("kahvehanede")
                               || t.Contains("kahvede oturur")),
                "Yasaktan sonra kahvehaneye cagri yapiliyor.");

            // Satis bagirisi da susmali: kapali malin bagirisi olmaz.
            var satis = new List<string>();
            foreach (VakitHesabi.Vakit v in System.Enum.GetValues(
                         typeof(VakitHesabi.Vakit)))
                foreach (var r in BarkKorpusu.Havuz(NPCMeslek.Tip.Esnaf, v,
                                                    1634, BirMayis1632, false))
                    if (r.tur == (int)BarkTuru.Satis) satis.Add(Sade(r.metin));

            foreach (string yasakli in new[] { "kahve", "tutun", "boza" })
                Assert.IsFalse(satis.Any(t => t.Contains(yasakli)),
                    $"1634'te '{yasakli}' bagiriliyor — kahve/tutun 2 Eylul "
                    + "1633 fermaniyla, boza IV. Murad doneminde kapatildi.");

            // 1632'de ucu de bagirilir.
            var satis32 = new List<string>();
            foreach (VakitHesabi.Vakit v in System.Enum.GetValues(
                         typeof(VakitHesabi.Vakit)))
                foreach (var r in BarkKorpusu.Havuz(NPCMeslek.Tip.Esnaf, v,
                                                    1632, BirMayis1632, false))
                    if (r.tur == (int)BarkTuru.Satis) satis32.Add(Sade(r.metin));
            foreach (string acik in new[] { "kahve", "tutun", "boza" })
                Assert.IsTrue(satis32.Any(t => t.Contains(acik)),
                    $"1632'de '{acik}' bagirilmiyor — o yil hepsi ACIK.");
        }

        /// <summary>
        /// <b>Aranan adama başka türlü konuşuluyor.</b>
        ///
        /// Ases temiz gezene "yolun açık olsun" der, aranana "dur!". İki
        /// havuz aynıysa aranma sistemi şehirde hiç duyulmaz.
        /// </summary>
        [Test]
        public void TheCityTalksDifferentlyToAWantedMan()
        {
            var temiz = BarkKorpusu.Havuz(NPCMeslek.Tip.Ases,
                                          VakitHesabi.Vakit.Yatsi,
                                          1632, BirMayis1632, false);
            var aranan = BarkKorpusu.Havuz(NPCMeslek.Tip.Ases,
                                           VakitHesabi.Vakit.Yatsi,
                                           1632, BirMayis1632, true);

            Assert.IsNotEmpty(temiz, "Ases temiz gezene hic bir sey demiyor.");
            Assert.IsNotEmpty(aranan, "Ases aranana hic bir sey demiyor.");

            var ortak = temiz.Select(r => r.id).Intersect(
                        aranan.Select(r => r.id)).ToList();
            Assert.IsEmpty(ortak,
                $"{ortak.Count} replik hem aranana hem temize soyleniyor — "
                + "aranma durumu sehirde duyulmuyor.");
        }

        /// <summary>
        /// <b>Seçim deterministik.</b> Aynı adam aynı vakitte aynı şeyi
        /// söyler; oyuncu onu tanıyabilsin diye (ADR 0070 ile aynı gerekçe).
        /// </summary>
        [Test]
        public void TheSamePersonSaysTheSameThing()
        {
            var a = BarkKorpusu.Sec(NPCMeslek.Tip.Esnaf,
                                    VakitHesabi.Vakit.Ogle,
                                    1632, BirMayis1632, false, 4242);
            var b = BarkKorpusu.Sec(NPCMeslek.Tip.Esnaf,
                                    VakitHesabi.Vakit.Ogle,
                                    1632, BirMayis1632, false, 4242);
            Assert.IsNotNull(a);
            Assert.AreEqual(a.id, b.id, "Ayni tohum farkli replik verdi.");

            // Ama farkli kisiler farkli seyler soylemeli.
            var farkli = new HashSet<string>();
            for (int t = 0; t < 60; t++)
                farkli.Add(BarkKorpusu.Sec(NPCMeslek.Tip.Esnaf,
                                           VakitHesabi.Vakit.Ogle,
                                           1632, BirMayis1632, false, t).id);
            Assert.Greater(farkli.Count, 30,
                $"Altmis esnaf yalnizca {farkli.Count} farkli sey soyledi.");
        }

        /// <summary>
        /// <b>Her meslek her vakitte söyleyecek bir şey buluyor.</b>
        ///
        /// Havuzu boş bir bağlam, o vakitte o kişinin dilsiz olması demek.
        /// </summary>
        [Test]
        public void NobodyIsEverLeftSpeechless()
        {
            var bos = new List<string>();
            foreach (NPCMeslek.Tip m in System.Enum.GetValues(
                         typeof(NPCMeslek.Tip)))
                foreach (VakitHesabi.Vakit v in System.Enum.GetValues(
                             typeof(VakitHesabi.Vakit)))
                    foreach (int yil in new[] { 1632, 1634 })
                        if (BarkKorpusu.Havuz(m, v, yil, BirMayis1632,
                                              false).Count == 0)
                            bos.Add($"{m}/{v}/{yil}");

            Assert.IsEmpty(bos,
                $"{bos.Count} baglamda kimse konusmuyor: "
                + string.Join(", ", bos.Take(10)));
        }
    }
}
