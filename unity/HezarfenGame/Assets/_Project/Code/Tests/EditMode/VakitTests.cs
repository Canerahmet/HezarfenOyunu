using Hezarfen.Zaman;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Şehrin saati gerçek gökyüzüne mi bakıyor?</b>
    ///
    /// Vakitler tablodan okunmuyor, güneşten hesaplanıyor — çünkü oyun
    /// üç yıllık bir takvim taşıyor ve İstanbul'da gün uzunluğu 9,2 ile
    /// 15,1 saat arasında gidip geliyor. Hesap yanlışsa hata sabit
    /// değildir: yılın bir yerinde doğru, başka yerinde saatlerce şaşar.
    ///
    /// Bu yüzden testler tek bir günü değil <b>yılın uçlarını</b>
    /// sınıyor ve doğrulama ölçütü bilinen İstanbul değerleri:
    /// gündönümlerinde ~9h15m ve ~15h10m, ekinoksta 12 saatin biraz
    /// üstü (kırılma payı).
    /// </summary>
    public class VakitTests
    {
        private const int KisDonumu = 355;    // 21 Aralik
        private const int YazDonumu = 172;    // 21 Haziran
        private const int Ekinoks = 80;       // 21 Mart
        private const int BirMayis = 121;     // ADR 0025: secilen gun

        /// <summary>Gün uzunlukları İstanbul'un bilinen değerleri.</summary>
        [Test]
        public void DayLengthMatchesIstanbulsRealSky()
        {
            var kis = VakitHesabi.Hesapla(KisDonumu);
            var yaz = VakitHesabi.Hesapla(YazDonumu);
            var ekn = VakitHesabi.Hesapla(Ekinoks);

            Assert.That(kis.GunUzunlugu, Is.EqualTo(9.25).Within(0.2),
                $"Kis donumu gunu {kis.GunUzunlugu:0.00} sa — Istanbul'da "
                + "~9 sa 15 dk.");
            Assert.That(yaz.GunUzunlugu, Is.EqualTo(15.17).Within(0.2),
                $"Yaz donumu gunu {yaz.GunUzunlugu:0.00} sa — ~15 sa 10 dk.");
            Assert.That(ekn.GunUzunlugu, Is.EqualTo(12.16).Within(0.15),
                $"Ekinoks gunu {ekn.GunUzunlugu:0.00} sa. On iki saatin "
                + "biraz USTU olmali: kirilma ve gunes diski batisi geciktirir. "
                + "Tam 12,00 cikiyorsa ufuk duzeltmesi unutulmus demektir.");
        }

        /// <summary>Doğuş ve batış öğleye göre simetrik.</summary>
        [Test]
        public void SunriseAndSunsetAreSymmetricAboutNoon()
        {
            foreach (int gun in new[] { KisDonumu, Ekinoks, BirMayis, YazDonumu })
            {
                var g = VakitHesabi.Hesapla(gun);
                double ortalama = (g.gunes + g.aksam) * 0.5;
                Assert.That(ortalama, Is.EqualTo(12.0).Within(0.02),
                    $"Gun {gun}: dogus/batis ortasi {ortalama:0.000}. Gercek "
                    + "gunes saatinde ogle TANIM GEREGI 12:00'dir; sapma, "
                    + "zaman denkleminin yanlislikla karistigini gosterir.");
            }
        }

        /// <summary>Vakitler gün içinde doğru sırada.</summary>
        [Test]
        public void ThePrayersFallInOrderAllYear()
        {
            for (int gun = 1; gun <= 365; gun += 7)
            {
                var g = VakitHesabi.Hesapla(gun);
                Assert.Less(g.sabah, g.gunes, $"Gun {gun}: fecr dogustan sonra.");
                Assert.Less(g.gunes, g.ogle, $"Gun {gun}: dogus ogleden sonra.");
                Assert.Less(g.ogle, g.ikindi, $"Gun {gun}: ogle ikindiden sonra.");
                Assert.Less(g.ikindi, g.aksam, $"Gun {gun}: ikindi batistan sonra.");
                Assert.Less(g.aksam, g.yatsi, $"Gun {gun}: aksam yatsidan sonra.");
            }
        }

        /// <summary>
        /// İkindi <b>Hanefî</b> ölçüsünde — Şâfiî'den belirgin geç.
        ///
        /// Osmanlı İstanbul'u Hanefî'dir ve bu bir ayar değil tarihî bir
        /// olgu. Şâfiî ölçüsü (gölge bir kat) ikindiyi yaklaşık yarım
        /// saat erkene alır ve NPC rutinlerinin bütün öğleden sonrasını
        /// kaydırırdı.
        /// </summary>
        [Test]
        public void AsrUsesTheHanafiShadowTheOttomansUsed()
        {
            Assert.AreEqual(2.0, VakitHesabi.IkindiGolgeKati, 0.001,
                "Hanefi olcusu iki kattir.");

            var g = VakitHesabi.Hesapla(BirMayis);
            // Safii olcusuyle ayni gunu hesapla ve KARSILASTIR.
            double d = VakitHesabi.Sapma(BirMayis);
            double golgeOgle = System.Math.Abs(System.Math.Tan(
                VakitHesabi.IstanbulEnlem * Mathf.Deg2Rad - d));
            double safiiYuk = System.Math.Atan(1.0 / (1.0 + golgeOgle))
                              * Mathf.Rad2Deg;
            double? w = VakitHesabi.SaatAcisi(d, safiiYuk);
            Assert.IsTrue(w.HasValue);
            double safii = 12.0 + w.Value / 15.0;

            Assert.Greater(g.ikindi, safii + 0.3,
                $"Hanefi ikindi {g.ikindi:0.00}, Safii {safii:0.00} — aradaki "
                + "fark en az yarim saat olmali; degilse golge katsayisi "
                + "hesaba girmiyor.");
        }

        /// <summary>
        /// Ezanî saat gün batımında tam 12:00.
        ///
        /// Bu, sistemin sıfır noktası. Kayarsa şehrin bütün saati kayar
        /// ve oyuncunun gördüğü saat ile ases devriyesinin saati ayrışır.
        /// </summary>
        [Test]
        public void TheOttomanClockReadsTwelveAtSunset()
        {
            foreach (int gun in new[] { KisDonumu, Ekinoks, BirMayis, YazDonumu })
            {
                var g = VakitHesabi.Hesapla(gun);
                double e = VakitHesabi.Ezani(g.aksam, g.aksam);
                Assert.That(e, Is.EqualTo(12.0).Within(0.001),
                    $"Gun {gun}: batista ezani saat {e:0.000} — 12:00 olmali. "
                    + "Osmanli gunu gun batiminda baslar.");

                // Bir saat sonra 13:00 (gosterimde 1).
                double sonra = VakitHesabi.Ezani(g.aksam + 1.0, g.aksam);
                Assert.That(sonra, Is.EqualTo(13.0).Within(0.001));
                Assert.AreEqual("1:00", VakitHesabi.EzaniYazi(sonra));
            }
        }

        /// <summary>
        /// Gece bayrağı batış ile doğuş arasında açık.
        ///
        /// Fener zorunluluğu (1633 sonrası) ve ases devriyesi buna bakar;
        /// yanlış bir gece tanımı oyuncuyu güpegündüz suçlu yapardı.
        /// </summary>
        [Test]
        public void NightIsBetweenSunsetAndSunrise()
        {
            var g = VakitHesabi.Hesapla(BirMayis);
            Assert.IsTrue(VakitHesabi.Gece(g, g.aksam + 0.5), "Batistan sonra gece.");
            Assert.IsTrue(VakitHesabi.Gece(g, 2.0), "Gece yarisi gece.");
            Assert.IsTrue(VakitHesabi.Gece(g, g.gunes - 0.2), "Dogustan once gece.");
            Assert.IsFalse(VakitHesabi.Gece(g, 12.0), "Ogle gunduz.");
            Assert.IsFalse(VakitHesabi.Gece(g, g.gunes + 0.2), "Dogustan sonra gunduz.");
        }

        /// <summary>Her an bir vakte düşer; boşluk yok.</summary>
        [Test]
        public void EveryMomentBelongsToSomePrayerPeriod()
        {
            var g = VakitHesabi.Hesapla(BirMayis);
            for (double t = 0; t < 24.0; t += 0.25)
            {
                var v = VakitHesabi.SuAnki(g, t);
                Assert.IsTrue(System.Enum.IsDefined(typeof(VakitHesabi.Vakit), v),
                    $"{t:0.00} saatinde vakit tanimsiz.");
            }
            // Gece yarisindan sonrasi hala yatsi vaktidir.
            Assert.AreEqual(VakitHesabi.Vakit.Yatsi,
                VakitHesabi.SuAnki(g, 2.0),
                "Gece yarisindan sonra yatsi vakti surer.");
            Assert.AreEqual(VakitHesabi.Vakit.Ikindi,
                VakitHesabi.SuAnki(g, (g.ikindi + g.aksam) * 0.5));
        }

        /// <summary>
        /// Bileşen saati sürüyor ve vakit değişince haber veriyor.
        /// </summary>
        [Test]
        public void TheClockAnnouncesEveryPrayerItPasses()
        {
            var go = new GameObject("saat");
            var z = go.AddComponent<ZamanSistemi>();
            z.gunDakika = 0f;              // elle surelim
            z.yilinGunu = BirMayis;
            z.gunesiSur = false;

            int haber = 0;
            z.VakitGirdi += _ => haber++;

            // Gunun basindan sonuna tara: alti vaktin hepsine girilmeli.
            z.saat = 0f; z.Yenile();
            haber = 0;
            for (float t = 0f; t < 24f; t += 0.1f)
            {
                z.saat = t;
                z.Yenile();
            }
            Assert.GreaterOrEqual(haber, 5,
                $"Gun boyunca yalnizca {haber} vakit girisi bildirildi; "
                + "alti vakit var ve NPC rutinleri bu olaya bagli.");

            z.VakteAtla(VakitHesabi.Vakit.Aksam);
            Assert.That(VakitHesabi.Ezani(z.saat, z.Bugun.aksam),
                Is.EqualTo(12.0).Within(0.05),
                "Aksam vaktine atlayinca ezani saat 12 civarinda olmali.");
            Object.DestroyImmediate(go);
        }
    
        /// <summary>
        /// <b>Sabah güneşi doğudan doğar.</b>
        ///
        /// Vakitleri saniyesine kadar doğrulayan sekiz test vardı ve
        /// hepsi yeşildi — ama hiçbiri <b>ışığa</b> bakmıyordu. Güneşin
        /// açısı doğru hesaplanıp ışığa <b>180 derece ters</b> yazılmıştı:
        /// yönlü ışığın <c>forward</c>'ı ışığın gittiği yöndür, güneşin
        /// durduğu yön değil. Oyun 122. günün 09:00'ında başlıyor;
        /// gölgeler güneşin batı-kuzeybatıda olduğunu söylüyordu.
        ///
        /// Hesap doğruyken çıktının ters olması, ölçmediğin şeyin
        /// bozulduğunu görmemenin en temiz örneği.
        /// </summary>
        [Test]
        public void TheMorningSunRisesInTheEast()
        {
            var go = new GameObject("ZAMAN_TEST");
            var isik = new GameObject("GUNES").AddComponent<Light>();
            isik.type = LightType.Directional;
            var z = go.AddComponent<ZamanSistemi>();
            z.gunesIsigi = isik;
            z.gunesiSur = true;
            z.yilinGunu = 122;          // 1 Mayis civari
            z.saat = 9f;
            z.Yenile();

            // Gunesin BULUNDUGU yon: isigin geldigi yon, yani -forward.
            Vector3 gunes = -isik.transform.forward;
            Assert.Greater(gunes.y, 0.1f,
                "Saat 09:00'da gunes ufkun USTUNDE olmali.");

            // Azimut: kuzeyden (+Z) saat yonunde.
            float azimut = Mathf.Repeat(
                Mathf.Atan2(gunes.x, gunes.z) * Mathf.Rad2Deg, 360f);
            // 1 Mayis, 09:00 Istanbul: ~110 derece (dogu-guneydogu).
            Assert.That(azimut, Is.InRange(80f, 140f),
                $"Sabah 09:00'da gunes azimutu {azimut:0} derece — "
                + "dogu-guneydogu (80-140) olmali. 180 sapma varsa isik "
                + "gunesin GITTIGI yone degil DURDUGU yone yazilmis.");

            Object.DestroyImmediate(isik.gameObject);
            Object.DestroyImmediate(go);
        }
}
}
