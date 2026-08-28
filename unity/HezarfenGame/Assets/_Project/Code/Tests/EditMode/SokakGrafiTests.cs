using System.Linq;
using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Şehrin gezilebilir iskeleti sağlam mı?</b>
    ///
    /// Bağlantısızlık sessiz bir hatadır: NPC hedefine gidemez, yerinde
    /// döner ve oyuncu "yapay zekâ bozuk" der. Oysa bozuk olan haritadır
    /// ve haritanın bozukluğu ancak <b>ölçülünce</b> görünür.
    ///
    /// Testler grafın tamamının bağlı olmasını <b>istemez</b> — 1632'de
    /// Haliç'te köprü yok ve Boğaz'ı yürüyerek geçemezsin. Beklenen şey
    /// her semtin <b>kendi içinde</b> bağlı olması; semtler arası kopukluk
    /// denizdir ve kayık ağının işidir.
    /// </summary>
    public class SokakGrafiTests
    {
        private const string Yol = "Assets/_Project/Data/SG_Sehir.asset";

        private static SokakGrafi Graf()
        {
            var g = AssetDatabase.LoadAssetAtPath<SokakGrafi>(Yol);
            // Assert.Ignore DEGIL: graf depoya girer.
            Assert.IsNotNull(g, $"Sokak grafi yok: {Yol}. Uret: "
                                + "Hezarfen/GIS/Sokak grafini kur");
            return g;
        }

        /// <summary>Graf boş değil ve şehrin yerlerini taşıyor.</summary>
        [Test]
        public void TheGraphCarriesThePlacesARoutineNeeds()
        {
            var g = Graf();
            Assert.Greater(g.dugumler.Count, 500,
                $"Yalnizca {g.dugumler.Count} dugum — sehir 142 mahalle.");
            Assert.Greater(g.kenarlar.Count, g.dugumler.Count,
                "Kenar sayisi dugumden az: graf agac bile degil.");

            // Rutinin hedef secebilmesi icin bu turler bulunmali.
            foreach (var t in new[] { SokakGrafi.Tur.Mescit, SokakGrafi.Tur.Ev,
                                      SokakGrafi.Tur.Cesme, SokakGrafi.Tur.Firin,
                                      SokakGrafi.Tur.Dukkan })
                Assert.Greater(g.Say(t), 0,
                    $"'{t}' turunde hic dugum yok — NPC rutini o hedefi "
                    + "hic secemez ve o meslek yerinde durur.");
        }

        /// <summary>
        /// Her semt kendi içinde bağlı (en az %50).
        ///
        /// Eşik %100 değil: coğrafya semtin içinde de ayırabilir.
        /// Ölçüm bunu gösterdi — Galata'nın 53 düğümlük cebi kulenin
        /// 1330 m batısında, bir dere vadisiyle ayrılmış Kasımpaşa.
        /// Oraya zorla yaya kenarı çakmak olmayan bir köprü uydurmak
        /// olurdu; bağlantıyı kayık ağı verecek.
        /// </summary>
        [Test]
        public void EveryDistrictIsWalkableWithinItself()
        {
            var g = Graf();
            var kom = g.Komsuluk();
            var etiket = new int[g.dugumler.Count];
            for (int i = 0; i < etiket.Length; i++) etiket[i] = -1;
            int c = 0;
            var yigin = new System.Collections.Generic.Stack<int>();
            for (int s = 0; s < etiket.Length; s++)
            {
                if (etiket[s] >= 0) continue;
                yigin.Push(s); etiket[s] = c;
                while (yigin.Count > 0)
                {
                    int v = yigin.Pop();
                    foreach (int w in kom[v])
                        if (etiket[w] < 0) { etiket[w] = c; yigin.Push(w); }
                }
                c++;
            }

            foreach (var grup in g.dugumler.Select((d, i) => (d, i))
                         .GroupBy(x => x.d.semt))
            {
                if (grup.Key == "TERRAIN") continue;
                int enBuyuk = grup.GroupBy(x => etiket[x.i]).Max(x => x.Count());
                float oran = enBuyuk / (float)grup.Count();
                Assert.GreaterOrEqual(oran, 0.50f,
                    $"{grup.Key}: dugumlerin yalnizca {oran:P0}'i tek parca. "
                    + "Oradaki NPC'lerin cogu hedefine gidemez.");
            }
        }

        /// <summary>
        /// Kenarlar suyun üstünden geçmiyor.
        ///
        /// Bu testin varlık sebebi bir hata: ilk üretimde yalnızca eğime
        /// bakılıyordu ve <b>hiçbir aday reddedilmedi</b> — çünkü Haliç'in
        /// tabanı yumuşak eğimlidir, dik değil. Eğim testi yanlış soruyu
        /// soruyordu. Su denetimi eklenince 0 ret, 4790 oldu.
        /// </summary>
        [Test]
        public void NoEdgeCrossesTheWater()
        {
            var g = Graf();
            int suda = 0;
            foreach (var e in g.kenarlar)
            {
                var a = g.dugumler[e.a].konum;
                var b = g.dugumler[e.b].konum;
                // Orta nokta deniz seviyesinin altindaysa kenar suyun
                // uzerinden geciyor demektir. Dugumlerin kendisi karada.
                var orta = (a + b) * 0.5f;
                if (orta.y < -0.5f) suda++;
            }
            Assert.AreEqual(0, suda,
                $"{suda} kenar suyun uzerinden geciyor. 1632'de Halic'te "
                + "kopru yok; karsiya kayikla gecilir.");
        }

        /// <summary>Kenar uzunlukları kaydedilen değerle tutuyor.</summary>
        [Test]
        public void EdgeLengthsMatchTheirEndpoints()
        {
            var g = Graf();
            foreach (var e in g.kenarlar.Take(500))
            {
                float gercek = Vector3.Distance(g.dugumler[e.a].konum,
                                                g.dugumler[e.b].konum);
                Assert.AreEqual(gercek, e.uzunluk, 0.5f,
                    $"Kenar {e.a}-{e.b}: kayitli {e.uzunluk:0.0} m, gercek "
                    + $"{gercek:0.0} m. A* maliyeti bu sayidan okunuyor.");
            }
        }

        /// <summary>
        /// A* aynı bileşendeki iki nokta arasında yol buluyor.
        ///
        /// Yol bulunamıyorsa NPC hedefine gidemez — ve bu, grafın bağlı
        /// olmasından ayrı bir sorudur: graf bağlı olup arama bozuk
        /// olabilir.
        /// </summary>
        [Test]
        public void PathfindingFindsAWayWithinAComponent()
        {
            var g = Graf();
            // Ayni semtte, birbirinden uzak iki mescit sec.
            var mescitler = g.dugumler.Select((d, i) => (d, i))
                .Where(x => x.d.tur == SokakGrafi.Tur.Mescit
                            && x.d.semt == "D_Surici_Dogu")
                .ToList();
            Assert.Greater(mescitler.Count, 2,
                "Surici Dogu'da yeterli mescit yok.");

            var a = mescitler.First();
            var b = mescitler.OrderByDescending(
                x => Vector3.Distance(x.d.konum, a.d.konum)).First();

            var yol = g.Yol(a.i, b.i);
            Assert.IsNotEmpty(yol,
                $"Surici Dogu'nun iki ucu arasinda yol yok — oysa o semt "
                + "tek parca olcuulmustu.");
            Assert.AreEqual(a.i, yol[0]);
            Assert.AreEqual(b.i, yol[^1]);

            // Yolun her adimi gercekten komsu olmali.
            var kom = g.Komsuluk();
            for (int i = 1; i < yol.Count; i++)
                Assert.Contains(yol[i], kom[yol[i - 1]],
                    $"Yolun {i}. adimi komsu degil — arama grafi atliyor.");
        }

        /// <summary>
        /// Kayık ağı var ve iskeleleri birbirine bağlıyor.
        ///
        /// 1632'de Haliç'te köprü yok; karşıya kayık ve peremeyle gidilir
        /// ve iskeleler tarifelidir (RESEARCH §6). Bu yüzden kayık bir
        /// süs değil <b>ulaşımın kendisi</b>.
        /// </summary>
        [Test]
        public void TheBoatNetworkJoinsThePiers()
        {
            var g = Graf();
            int iskele = g.Say(SokakGrafi.Tur.Iskele);
            Assert.GreaterOrEqual(iskele, 4,
                $"Yalnizca {iskele} iskele — Halic'in iki yakasi ve "
                + "Uskudar en az dort iskele ister.");

            int kayik = g.kenarlar.Count(e => e.kayik);
            Assert.AreEqual(iskele * (iskele - 1) / 2, kayik,
                "Her iskeleden her iskeleye kayik gitmeli: Halic ve Bogaz "
                + "tek su kutlesidir.");

            // Kayik kenarinin IKI ucu da iskele olmali.
            foreach (var e in g.kenarlar.Where(x => x.kayik))
            {
                Assert.AreEqual(SokakGrafi.Tur.Iskele, g.dugumler[e.a].tur);
                Assert.AreEqual(SokakGrafi.Tur.Iskele, g.dugumler[e.b].tur);
            }
        }

        /// <summary>
        /// Kayıkla şehrin tamamı gezilebilir; <b>yürüyerek değil</b>.
        ///
        /// Bu iki sayının farkı oyunun ulaşım mekaniğidir. Yürüyen graf
        /// kara parçalarına bölünmüş olmalı — birleşikse suyun üstünde
        /// bir yol var demektir.
        ///
        /// Bu test bir hatanın anısı: iskeleye giden kenarı her denetimden
        /// muaf tutmuştum ve Eminönü iskelesi hem Suriçi'ne hem Galata'ya
        /// bağlanınca <b>Haliç'in üstünde yaya köprüsü</b> oluştu. Bileşen
        /// sayısı düştü ve bu bir iyileşme gibi göründü.
        /// </summary>
        [Test]
        public void BoatsUniteTheCityButFeetDoNot()
        {
            var g = Graf();
            Assert.AreEqual(g.dugumler.Count, g.EnBuyukBilesen(kayikVar: true),
                "Kayikla bile sehrin tamami gezilemiyor.");
            Assert.Less(g.EnBuyukBilesen(kayikVar: false), g.dugumler.Count,
                "YURUYEREK sehrin tamami geziliyor — yani suyun ustunden "
                + "bir yol var. 1632'de Halic'te kopru yoktur.");
        }

        /// <summary>En yakın düğüm araması türe göre süzebiliyor.</summary>
        [Test]
        public void TheNearestSearchCanFilterByKind()
        {
            var g = Graf();
            var mescit = g.dugumler.First(d => d.tur == SokakGrafi.Tur.Mescit);
            int i = g.EnYakin(mescit.konum, SokakGrafi.Tur.Cesme);
            Assert.GreaterOrEqual(i, 0, "Mescide en yakin cesme bulunamadi.");
            Assert.AreEqual(SokakGrafi.Tur.Cesme, g.dugumler[i].tur,
                "Suzgec calismiyor: baska turden dugum dondu.");
        }
    }
}
