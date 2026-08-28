using System.Linq;
using Hezarfen.Sehir;
using Hezarfen.Zaman;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Takvim ve olaylar tarihin kendisi mi?</b>
    ///
    /// Oyunun kronolojik eşikleri <b>gün hassasiyetinde</b>: kahvehane
    /// fermanı 2 Eylül 1633'tür, 1 Eylül değil. Takvimde bir günlük bir
    /// kayma sessizdir — kimse "13 Ağustos mu 14 Ağustos mu" diye
    /// bakmaz — ama eşiği kaydırır.
    /// </summary>
    public class OlayTests
    {
        /// <summary>
        /// Artık yıl sayılıyor. 1632 artık yıldır ve şubattan sonraki
        /// her tarih buna bağlıdır.
        /// </summary>
        [Test]
        public void TheCalendarKnowsAboutLeapYears()
        {
            Assert.IsTrue(Kronoloji.ArtikYil(1632), "1632 artik yildir.");
            Assert.IsFalse(Kronoloji.ArtikYil(1633));
            Assert.AreEqual(366, Kronoloji.YilUzunlugu(1632));
            Assert.AreEqual(365, Kronoloji.YilUzunlugu(1633));

            // 1 Mayis: artik yilda 122, degilse 121.
            Assert.AreEqual(122, Kronoloji.YilinGunu(1632, 5, 1),
                "1632 artik yil: 1 Mayis 122. gundur.");
            Assert.AreEqual(121, Kronoloji.YilinGunu(1633, 5, 1));

            Assert.AreEqual("1 Mayıs 1632", Kronoloji.Tarih(1632, 122));
            Assert.AreEqual("2 Eylül 1633", Kronoloji.Tarih(1633, 245),
                "Kahvehane fermaninin tarihi 2 Eylul 1633 olmali.");
            Assert.AreEqual("26 Ağustos 1633", Kronoloji.Tarih(1633, 238),
                "Cibali yangininin tarihi 26 Agustos 1633 olmali.");
        }

        /// <summary>
        /// <b>Hafta günleri gerçek takvimin günleri.</b>
        ///
        /// Ve kaydın doğruladığı bir ayrıntı: Cibali yangını da (26
        /// Ağustos 1633) kahvehane fermanı da (2 Eylül 1633) <b>Cuma</b>
        /// günüdür — bir hafta arayla, iki Cuma. Yangının Cuma çıkması
        /// ve fermanın ertesi Cuma gelmesi, olayların ne kadar hızlı
        /// birbirini kovaladığını gösteriyor.
        /// </summary>
        [Test]
        public void TheWeekdaysAreHistorysOwn()
        {
            Assert.AreEqual(Kronoloji.Gun.Persembe,
                Kronoloji.HaftaGunu(1632, 1),
                "1 Ocak 1632 Persembe'dir — takvimin capasi.");
            Assert.AreEqual(Kronoloji.Gun.Cumartesi,
                Kronoloji.HaftaGunu(1632, 122),
                "1 Mayis 1632 Cumartesi'dir.");
            Assert.AreEqual(Kronoloji.Gun.Cuma,
                Kronoloji.HaftaGunu(1633, 238),
                "26 Agustos 1633 (Cibali yangini) Cuma'dir.");
            Assert.AreEqual(Kronoloji.Gun.Cuma,
                Kronoloji.HaftaGunu(1633, 245),
                "2 Eylul 1633 (kahvehane fermani) Cuma'dir.");
            Assert.AreEqual(Kronoloji.Gun.Pazartesi,
                Kronoloji.HaftaGunu(1634, 121),
                "1 Mayis 1634 Pazartesi'dir.");
        }

        /// <summary>Cuma haftada tam bir kez gelir.</summary>
        [Test]
        public void FridayComesExactlyOnceAWeek()
        {
            int cuma = 0;
            for (int g = 1; g <= 28; g++)
                if (Kronoloji.Cuma(1633, g)) cuma++;
            Assert.AreEqual(4, cuma,
                $"Dort haftada {cuma} Cuma sayildi.");
        }

        /// <summary>
        /// <b>Cibali yangını yalnız kendi gününde.</b>
        ///
        /// Belgeli bir olayın rastgele tekrar etmesi, onu belgeli
        /// olmaktan çıkarır.
        /// </summary>
        [Test]
        public void TheCibaliFireHappensOnlyOnItsOwnDay()
        {
            Assert.IsTrue(Olaylar.YanginBugun(1633, 238),
                "26 Agustos 1633'te yangin yok.");
            Assert.IsFalse(Olaylar.YanginBugun(1633, 237));
            Assert.IsFalse(Olaylar.YanginBugun(1633, 239));
            Assert.IsFalse(Olaylar.YanginBugun(1632, 238),
                "Yangin yanlis yilda cikti.");

            int kac = 0;
            for (int y = 1631; y <= 1635; y++)
                for (int g = 1; g <= Kronoloji.YilUzunlugu(y); g++)
                    if (Olaylar.YanginBugun(y, g)) kac++;
            Assert.AreEqual(1, kac,
                $"Bes yilda {kac} Cibali yangini — belgeli olay bir kez olur.");
        }

        /// <summary>
        /// <b>Tulumba yok.</b>
        ///
        /// İlk tulumba teşkilatı 1720'lerdedir (Gerçek Davud). 1632
        /// söndürme yöntemi su taşımak, yıkıcılarla ateş hattı kesmek ve
        /// bina yıkmaktır. Bu bir ayrıntı değil: tulumbayı eklemek
        /// yangını doksan yıl ileri taşımak olurdu.
        /// </summary>
        [Test]
        public void ThereIsNoFireEngineInSixteenThirtyTwo()
        {
            foreach (string yol in Olaylar.SondurmeYollari)
                Assert.IsFalse(yol.Contains("tulumba"),
                    $"Sondurme yollarinda '{yol}' var — tulumba teskilati "
                    + "1720'lerdedir.");
            Assert.AreEqual(3, Olaylar.SondurmeYollari.Length);
            Assert.Contains("su tasima", Olaylar.SondurmeYollari);
        }

        /// <summary>
        /// <b>Yangının ödülü akçe değil, mahalle itibarı.</b>
        ///
        /// Yangına koşan adam para almaz; mahalle onu tanır. Ödülü akçeye
        /// çevirmek olayı bir işe dönüştürürdü.
        /// </summary>
        [Test]
        public void RunningToAFireEarnsStandingNotCoin()
        {
            Assert.IsTrue(Olaylar.YanginOdulu.HasFlag(Odul.MahalleItibari));
            Assert.IsFalse(Olaylar.YanginOdulu.HasFlag(Odul.Akce),
                "Yangina kosana akce verilirse olay bir ise donusur.");
        }

        /// <summary>
        /// <b>Cuma namazı mescide değil selâtin camisine akar.</b>
        ///
        /// Rutin bunu bilmezse Cuma sıradan bir gün olur ve haftanın tek
        /// özel günü oyunda hiç görünmez.
        /// </summary>
        [Test]
        public void FridayPrayerGoesToTheGreatMosque()
        {
            Assert.AreEqual(SokakGrafi.Tur.Cami,
                Olaylar.CumaHedefi(SokakGrafi.Tur.Mescit),
                "Cuma gunu hedef hala mahalle mescidi.");
            Assert.AreNotEqual(SokakGrafi.Tur.Mabet,
                Olaylar.CumaHedefi(SokakGrafi.Tur.Mescit),
                "Cuma cemaati KILISEYE yollandi — grafta Mabet "
                + "kilise/sinagog demektir (ADR 0018).");
            // Diger hedefler degismez: Cuma isi durdurmaz, namaza toplar.
            Assert.AreEqual(SokakGrafi.Tur.Dukkan,
                Olaylar.CumaHedefi(SokakGrafi.Tur.Dukkan));
            Assert.Greater(Olaylar.CumaKatsayisi, 1f,
                "Cuma namazi siradan ogleden kalabalik olmali.");
        }

        /// <summary>
        /// <b>Cuma şehirde ÖLÇÜLEBİLİR bir fark yapıyor.</b>
        ///
        /// Bir olayın kodda tanımlı olması onu var etmez. Cuma'nın var
        /// olması demek, o gün öğle vaktinde şehrin sayılabilir biçimde
        /// başka türlü davranması demek: mahalle mescitleri boşalır,
        /// cemaat camiye akar ve kalabalık sıradan öğleden çoktur.
        ///
        /// Ölçü bir görüş değil bir oran: Cuma payı, sıradan öğle payının
        /// <see cref="Olaylar.CumaKatsayisi"/> katı olmalı.
        /// </summary>
        [Test]
        public void FridayVisiblyChangesTheCity()
        {
            var g = AssetDatabase.LoadAssetAtPath<SokakGrafi>(
                "Assets/_Project/Data/SG_Sehir.asset");
            Assert.IsNotNull(g, "Sokak grafi yok — once grafi kur.");
            var meslekler = AssetDatabase.FindAssets("t:NPCMeslek")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<NPCMeslek>)
                .Where(m => m != null).ToList();
            Assert.IsNotEmpty(meslekler);

            var sakinler = SehirGunu.Sakinler(g, meslekler, 1500);

            // 1 Mayis 1632 Cumartesi (siradan), 30 Nisan 1632 Cuma.
            Assert.IsFalse(Kronoloji.Cuma(1632, 122), "Kontrol gunu Cuma.");
            Assert.IsTrue(Kronoloji.Cuma(1632, 121), "Olcum gunu Cuma degil.");

            var siradan = SehirGunu.Olc(g, sakinler,
                                        VakitHesabi.Vakit.Ogle, 1632, 122);
            var cuma = SehirGunu.Olc(g, sakinler,
                                     VakitHesabi.Vakit.Ogle, 1632, 121);

            int Say(SehirGunu.Olcum o, SokakGrafi.Tur t)
                => o.hedefler.TryGetValue(t, out int n) ? n : 0;

            int sMescit = Say(siradan, SokakGrafi.Tur.Mescit);
            int sCami = Say(siradan, SokakGrafi.Tur.Cami);
            int cMescit = Say(cuma, SokakGrafi.Tur.Mescit);
            int cCami = Say(cuma, SokakGrafi.Tur.Cami);

            Assert.Greater(sMescit, 0, "Siradan ogle mescide akis yok.");
            Assert.AreEqual(0, sCami,
                $"Siradan gunde {sCami} kisi Cuma camisine gitti — Cuma "
                + "namazi haftada bir kilinir.");
            Assert.AreEqual(0, cMescit,
                $"Cuma gunu {cMescit} kisi hala mahalle mescidinde — Cuma "
                + "namazi mescitte kilinmaz.");

            // Kalabalik katsayi kadar artmali.
            float beklenen = sMescit * Olaylar.CumaKatsayisi;
            Assert.That(cCami, Is.EqualTo(beklenen).Within(beklenen * 0.12f),
                $"Siradan ogle {sMescit} kisi, Cuma {cCami}; katsayi "
                + $"{Olaylar.CumaKatsayisi} ise {beklenen:F0} bekleniyordu.");

            // Ve bu gercekten bir AKIS: camiye gidenler yurumek zorunda.
            Assert.Greater(cCami, sMescit,
                "Cuma sıradan ogleden kalabalik degil.");
        }

        /// <summary>
        /// <b>Cuma camisi şehirde gerçekten var.</b>
        ///
        /// Hedefi üretmek yetmez; oraya yürünebilmeli. Cami düğümü yoksa
        /// Cuma günü bütün cemaat yerinde döner ve olay <b>sessizce</b>
        /// hiç olmaz.
        /// </summary>
        [Test]
        public void TheFridayMosquesExistOnTheMap()
        {
            var g = AssetDatabase.LoadAssetAtPath<SokakGrafi>(
                "Assets/_Project/Data/SG_Sehir.asset");
            Assert.IsNotNull(g);
            int cami = g.Say(SokakGrafi.Tur.Cami);
            Assert.Greater(cami, 0,
                "Grafta hic Cuma camisi yok — Cuma gunu cemaat yerinde "
                + "doner ve olay sessizce hic olmaz.");

            // GALATA'DAN CAMIYE YURUNEBILIYOR MU.
            //
            // Dugumun `semt` alani SAHNE adidir, cografya degil: landmark'lar
            // arazi sahnesinde durdugu icin hepsi "TERRAIN" yazar. O yuzden
            // soru etiketle degil YOLLA sorulur — zaten onemli olan da bu.
            // Faz 6 kabul kriteri "Galata'da 30 dakika serbest dolasim"
            // diyor; Cuma orada bir sey yapmiyorsa uretilmemistir.
            var galataEvler = g.dugumler
                .Select((d, i) => (d, i))
                .Where(t => t.d.tur == SokakGrafi.Tur.Ev
                            && t.d.semt == "D_Galata")
                .Select(t => t.i).ToList();
            Assert.IsNotEmpty(galataEvler, "Galata'da ev dugumu yok.");

            var camiler = g.dugumler
                .Select((d, i) => (d, i))
                .Where(t => t.d.tur == SokakGrafi.Tur.Cami)
                .Select(t => t.i).ToList();

            int varan = galataEvler.Count(
                ev => camiler.Any(c => g.Yol(ev, c, kayikVar: false).Count > 0));
            Assert.Greater(varan, galataEvler.Count / 2,
                $"Galata'nin {galataEvler.Count} evinden yalnizca {varan} "
                + "tanesi bir Cuma camisine YURUYEREK varabiliyor — Cuma "
                + "gunu cemaat yerinde doner (ADR 0071).");
        }

        /// <summary>
        /// <b>Çarşı sabahı da ölçülebilir bir fark yapıyor.</b>
        ///
        /// Güneş vakti kepenkler açılır; dükkâna akış sıradan bir vakitten
        /// çok olmalı. Cuma ile aynı formül, aynı disiplin: katsayı tek
        /// yerde yazılı ve ölçüm onu doğruluyor.
        /// </summary>
        [Test]
        public void TheMarketMorningFillsTheShops()
        {
            var g = AssetDatabase.LoadAssetAtPath<SokakGrafi>(
                "Assets/_Project/Data/SG_Sehir.asset");
            var meslekler = AssetDatabase.FindAssets("t:NPCMeslek")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<NPCMeslek>)
                .Where(m => m != null).ToList();
            var sakinler = SehirGunu.Sakinler(g, meslekler, 1500);

            int Dukkan(VakitHesabi.Vakit v)
            {
                var o = SehirGunu.Olc(g, sakinler, v, 1632, 122);
                return o.hedefler.TryGetValue(SokakGrafi.Tur.Dukkan,
                                              out int n) ? n : 0;
            }

            // BEKLENEN, `temel * katsayi` DEGILDIR.
            //
            // Ilk yazimda oyleydi ve test patladi (446 olculdu, 542
            // bekleniyordu). Sebep veri degil aritmetik: cizelgesinde
            // gunes vakti dukkan payi ZATEN 1 olan bir meslek daha fazla
            // gonderemez. `p*k > 1` fiziksel olarak imkansizdir; doymus
            // meslekler icin beklenen deger 1'de durur.
            //
            // Yani dogru olcu kisi basina `min(1, p*k)` toplamidir.
            float temel = 0f, beklenen = 0f;
            foreach (var s in sakinler)
            {
                if (s.meslek == null) continue;
                float p = s.meslek.Olasilik(VakitHesabi.Vakit.Gunes,
                                            SokakGrafi.Tur.Dukkan);
                temel += p;
                beklenen += Mathf.Min(1f, p * Olaylar.PazarKatsayisi);
            }

            int olculen = Dukkan(VakitHesabi.Vakit.Gunes);
            Assert.Greater(temel, 0f, "Gunes vaktinde dukkan adimi yok.");
            Assert.Greater(beklenen, temel,
                "Carsi sabahi hic kimseyi fazladan cekmiyor.");
            Assert.That(olculen, Is.EqualTo(beklenen).Within(beklenen * 0.15f),
                $"Carsi sabahi {olculen} kisi dukkanda; temel pay {temel:F0}, "
                + $"katsayi {Olaylar.PazarKatsayisi}, doyma sonrasi "
                + $"beklenen {beklenen:F0}.");
        }

        /// <summary>
        /// <b>Gece devriyesi görüşü artırır ama geceyi gündüz yapmaz.</b>
        ///
        /// Karanlık saklar, devriye arttırır; ikisi zıt yönde ve ikisi de
        /// gerçek. Net sonuç gündüzden <b>düşük</b> olmalı — yoksa gece
        /// gizlenmenin bir anlamı kalmaz ve bütün gece oynanışı çöker.
        /// </summary>
        [Test]
        public void TheNightWatchSeesMoreButNotAsMuchAsDaylight()
        {
            Assert.IsTrue(Olaylar.DevriyeVar(VakitHesabi.Vakit.Yatsi),
                "Yatsida gece devriyesi yok.");
            Assert.IsFalse(Olaylar.DevriyeVar(VakitHesabi.Vakit.Ogle),
                "Ogle vaktinde gece devriyesi cikti.");

            var sistem = new GameObject("aranma").AddComponent<AranmaSistemi>();
            float gunduz = sistem.gorusMesafesi;
            float gece = gunduz * sistem.geceGorusCarpani;
            float devriyeli = gece * Olaylar.DevriyeKatsayisi;
            Object.DestroyImmediate(sistem.gameObject);

            Assert.Greater(devriyeli, gece,
                "Devriye gecesi siradan geceden daha genis gormeli.");
            Assert.Less(devriyeli, gunduz,
                $"Devriye menzili {devriyeli:F1} m, gunduz {gunduz:F1} m — "
                + "gece gunduzden daha tehlikeli olursa gizlenmenin anlami "
                + "kalmaz.");
        }

        /// <summary>Günün olayları vakte göre doğru sırada geliyor.</summary>
        [Test]
        public void TheDaysEventsArriveAtTheRightPrayer()
        {
            // 26 Agustos 1633: hem Cuma hem yangin gunu.
            var ogle = Olaylar.Bugun(1633, 238, VakitHesabi.Vakit.Ogle);
            Assert.Contains(OlayTuru.Cuma, ogle);
            Assert.Contains(OlayTuru.Yangin, ogle);

            var sabah = Olaylar.Bugun(1633, 240, VakitHesabi.Vakit.Gunes);
            Assert.Contains(OlayTuru.Pazar, sabah,
                "Carsi gunes vaktinde kurulmali.");
            Assert.IsFalse(sabah.Contains(OlayTuru.GeceDevriyesi),
                "Gunduz gece devriyesi cikti.");

            var gece = Olaylar.Bugun(1633, 240, VakitHesabi.Vakit.Yatsi);
            Assert.Contains(OlayTuru.GeceDevriyesi, gece);
            Assert.IsFalse(gece.Contains(OlayTuru.Pazar),
                "Yatsida carsi kuruldu.");
        }
    }
}
