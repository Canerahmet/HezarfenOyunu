using System.IO;
using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Kayıt gerçekten geri geliyor mu?</b>
    ///
    /// Kayıt sisteminin sessiz hataları oyuncuya en pahalıya patlayan
    /// hatalardır: dosya yazılır, oyun kapanır, açılır — ve oyuncu
    /// bıraktığı yerde <b>değildir</b>. Hata da vermez.
    ///
    /// Bu yüzden ölçülen şey "yazdı mı" değil, <b>okunanın yazılanla
    /// aynı olup olmadığı</b>.
    /// </summary>
    public class KayitTests
    {
        [SetUp] public void Hazirla() => Kayit.Sil();
        [TearDown] public void Temizle() => Kayit.Sil();

        private static KayitVerisi Ornek() => new KayitVerisi
        {
            yil = 1633, yilinGunu = 245, saat = 14.25f,
            x = 1234.5f, y = 52.3f, z = -678.9f, bakisYaw = 137f,
            akce = 96, yasakMal = true,
            gorevArketip = 3, gorevTohum = 11, gorevSiradaki = 1,
            aranmaSeviyesi = 0.42f,
            perde2Asama = 2, talimSayisi = 3,
            sehirTohumu = 1632,
        };

        /// <summary>
        /// <b>Yazılan aynen okunuyor.</b> Her alan tek tek — bir alanın
        /// unutulması, o alanın oyuncu için hiç var olmaması demek.
        /// </summary>
        [Test]
        public void WhatIsWrittenComesBackUnchanged()
        {
            var a = Ornek();
            Assert.IsTrue(Kayit.Yaz(a), "Kayit yazilamadi.");
            Assert.IsTrue(Kayit.Var, "Kayit dosyasi olusmadi.");

            var b = Kayit.Oku();
            Assert.IsNotNull(b, "Kayit okunamadi.");

            Assert.AreEqual(a.yil, b.yil, "Yil degisti.");
            Assert.AreEqual(a.yilinGunu, b.yilinGunu, "Gun degisti.");
            Assert.AreEqual(a.saat, b.saat, 0.001f, "Saat degisti.");
            Assert.AreEqual(a.x, b.x, 0.001f, "Konum x degisti.");
            Assert.AreEqual(a.y, b.y, 0.001f, "Konum y degisti.");
            Assert.AreEqual(a.z, b.z, 0.001f, "Konum z degisti.");
            Assert.AreEqual(a.bakisYaw, b.bakisYaw, 0.001f, "Bakis degisti.");
            Assert.AreEqual(a.akce, b.akce, "Kese degisti.");
            Assert.AreEqual(a.yasakMal, b.yasakMal, "Yasak mal degisti.");
            Assert.AreEqual(a.gorevArketip, b.gorevArketip, "Gorev degisti.");
            Assert.AreEqual(a.gorevSiradaki, b.gorevSiradaki, "Durak degisti.");
            Assert.AreEqual(a.aranmaSeviyesi, b.aranmaSeviyesi, 0.001f,
                "Aranma degisti.");
            Assert.AreEqual(a.perde2Asama, b.perde2Asama, "Perde 2 degisti.");
            Assert.AreEqual(a.talimSayisi, b.talimSayisi, "Talim degisti.");
            Assert.AreEqual(a.sehirTohumu, b.sehirTohumu, "Sehir tohumu degisti.");
        }

        /// <summary>
        /// <b>Şehir kayıt dosyasında DEĞİL.</b>
        ///
        /// Rutin saf bir işlev olduğu için (ADR 0070) iki bin sakinin
        /// nerede olduğu saklanmıyor; tarih ve tohum yetiyor. Bunun
        /// ölçülebilir karşılığı dosyanın <b>küçük</b> kalması.
        ///
        /// Sakinler dosyaya girmeye başlarsa bu test önce patlar — ve
        /// patlaması iyi olur, çünkü o gün kayıt göçü de yazılmak
        /// zorunda kalırdı.
        /// </summary>
        [Test]
        public void TheCityIsNotInTheSaveFile()
        {
            Assert.IsTrue(Kayit.Yaz(Ornek()));
            var bilgi = new FileInfo(Kayit.Yol);
            Assert.Less(bilgi.Length, 2048,
                $"Kayit dosyasi {bilgi.Length} bayt — sehir dosyaya "
                + "sizmis olmali. Rutin saf oldugu icin sakinler "
                + "kaydedilmez (ADR 0070).");
        }

        /// <summary>
        /// <b>Bozuk kayıt sessizce yeni oyun başlatmıyor.</b>
        ///
        /// En kötü davranış şudur: dosya bozuktur, oyun bunu yutar ve
        /// oyuncuya boş bir başlangıç verir. Oyuncu ilerlemesinin
        /// gittiğini ancak oynarken anlar.
        /// </summary>
        [Test]
        public void ACorruptSaveIsRefusedLoudly()
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(Kayit.Yol, "{ bu json degil ");

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("Kayit okunamadi"));

            Assert.IsNull(Kayit.Oku(), "Bozuk kayit okunmus gibi dondu.");
        }

        /// <summary>
        /// <b>Çok eski bir sürüm reddediliyor.</b>
        ///
        /// Sürüm alanı ilk günden var, çünkü sürümsüz bir dosya ilk
        /// güncellemede <b>sessizce yanlış</b> okunur: alanlar kayar ve
        /// oyuncu kesesinde başka bir sayı bulur.
        /// </summary>
        [Test]
        public void AnAncientSaveVersionIsRefused()
        {
            var v = Ornek();
            v.surum = Kayit.EnEskiOkunabilirSurum - 1;
            Kayit.Yaz(v);

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("cok"));

            Assert.IsNull(Kayit.Oku(), "Eski surumlu kayit kabul edildi.");
        }

        /// <summary>Kayıt olmayınca okuma sessizce null döner (hata değil).</summary>
        [Test]
        public void NoSaveIsNotAnError()
        {
            Assert.IsFalse(Kayit.Var);
            Assert.IsNull(Kayit.Oku(), "Kayit yokken bir sey dondu.");
        }
    }
}
