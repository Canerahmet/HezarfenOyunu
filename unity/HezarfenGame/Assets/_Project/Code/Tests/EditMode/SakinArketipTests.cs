using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Hezarfen.Tests.EditMode
{
    /// <summary>
    /// <b>Kalabalık gerçekten çeşitli mi</b> — bakarak değil sayarak.
    ///
    /// Bir oyuncu şunu yazdı: *"sokaktaki herkes aynı sakallı adamın
    /// kopyası; çocuklar minik sakallı adamlar, kadın hiç yok."* Yedi
    /// arketip üretildi, ama üretmek yetmez: bu depoda aynı kusur
    /// tekrar tekrar aynı biçimde doğdu — <b>yazıldı, diske geçti,
    /// ölçülmedi</b>. Prefablar diskte durup sahneye hiç bağlanmasa da,
    /// seçim işlevi herkese aynı gövdeyi verse de, ekranda görünen şey
    /// yine tek tip bir kalabalık olurdu ve hiçbir şey kırmızı dönmezdi.
    ///
    /// Bu testler o boşluğu kapatır: gövdeler var mı, kimlikleri
    /// yazılmış mı, ve <b>bin tohum</b> dağıtıldığında sokakta kaç
    /// kadın, kaç çocuk, kaç yaşlı düşüyor.
    /// </summary>
    public class SakinArketipTests
    {
        private const string PrefabDir = "Assets/_Project/Art/Prefabs";

        private static GameObject[] Arketipler()
        {
            return AssetDatabase
                .FindAssets("PF_Sakin_ t:Prefab", new[] { PrefabDir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(y => Path.GetFileName(y).StartsWith("PF_Sakin_"))
                .OrderBy(y => y)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(g => g != null)
                .ToArray();
        }

        [Test]
        public void EveryArchetypeCarriesItsOwnIdentity()
        {
            var hepsi = Arketipler();
            Assert.GreaterOrEqual(hepsi.Length, 7,
                $"{hepsi.Length} sakin arketipi bulundu — kadin, cocuk, "
                + "yasli ve genc icin en az yedi gerekiyor. Once: blender "
                + "gen_hezarfen.py --export, sonra Hezarfen > Boru Hatti > "
                + "Karakteri yerlestir.");

            foreach (var p in hepsi)
            {
                var sg = p.GetComponent<SakinGovde>();
                Assert.IsNotNull(sg,
                    $"{p.name}: SakinGovde yok — NPCYonetici bu govdenin "
                    + "cinsiyetini ve boyunu bilemez, secim onu en sona "
                    + "atar ve kalabalik sessizce tek tipe doner.");
                Assert.Contains(sg.cinsiyet, new[] { "erkek", "kadin" },
                    $"{p.name}: cinsiyet '{sg.cinsiyet}' taninmiyor.");
                Assert.Contains(sg.yasBandi,
                    new[] { "cocuk", "genc", "yetiskin", "yasli" },
                    $"{p.name}: yas bandi '{sg.yasBandi}' taninmiyor.");
                // Katalogdan gelen CIPLAK boy: cocuk ~1,2 m, adam 1,7 m.
                Assert.That(sg.tabanBoy, Is.InRange(1.10f, 1.95f),
                    $"{p.name}: taban boy {sg.tabanBoy:0.00} m — "
                    + "katalogdan okunamamis olmali.");
            }
        }

        [Test]
        public void BothSexesAndAllAgesHaveABody()
        {
            var hepsi = Arketipler().Select(p => p.GetComponent<SakinGovde>())
                                    .Where(s => s != null).ToArray();
            Assert.IsTrue(hepsi.Any(s => s.Kadin), "Kadin govdesi yok.");
            Assert.IsTrue(hepsi.Any(s => !s.Kadin), "Erkek govdesi yok.");
            foreach (int band in new[] { 0, 1, 2, 3 })
                Assert.IsTrue(hepsi.Any(s => s.BandDizini == band),
                    $"Yas bandi {band} icin hicbir govde yok — o banda "
                    + "dusen sakinler baska bir yasin govdesini alir.");
            // Cocuk govdesi de KIZ ve OGLAN olarak ayrilmali; yoksa
            // butun cocuklar ayni cinsiyette olur.
            Assert.IsTrue(hepsi.Any(s => s.BandDizini == 0 && s.Kadin)
                          && hepsi.Any(s => s.BandDizini == 0 && !s.Kadin),
                "Cocuklarin yalniz bir cinsiyeti var.");
        }

        [Test]
        public void TheCrowdIsNotOneManRepeated()
        {
            var hepsi = Arketipler();
            if (hepsi.Length == 0) Assert.Inconclusive("arketip yok");

            var sayim = new Dictionary<string, int>();
            int kadin = 0, cocuk = 0, yasli = 0;
            const int n = 1200;
            for (int i = 0; i < n; i++)
            {
                var dna = InsanDNA.Uret(i * 7919 + 13);
                int t = NPCYonetici.ArketipSec(hepsi, dna);
                string ad = hepsi[t].name;
                sayim[ad] = sayim.TryGetValue(ad, out var c) ? c + 1 : 1;

                var sg = hepsi[t].GetComponent<SakinGovde>();
                if (sg == null) continue;
                if (sg.Kadin) kadin++;
                if (sg.BandDizini == 0) cocuk++;
                if (sg.BandDizini == 3) yasli++;
            }

            // TEK BIR GOVDE SOKAGA HAKIM OLMAMALI.
            //
            // Esik %55: yetiskin bandi nufusun en genisi oldugu icin
            // yetiskin erkek dogal olarak en kalabalik olacak, ama
            // yarisindan cogu olursa "herkes ayni adam" sikayeti geri
            // gelir.
            var enCok = sayim.OrderByDescending(k => k.Value).First();
            Assert.Less(enCok.Value / (float)n, 0.55f,
                $"Sokagin %{enCok.Value * 100f / n:0} 'i {enCok.Key} — "
                + "kalabalik tek bir govdeye yigilmis.");

            // Sokakta hic kadin yoktu; sikayetin kendisi buydu.
            Assert.That(kadin / (float)n, Is.InRange(0.30f, 0.62f),
                $"Kadin orani %{kadin * 100f / n:0} — sehrin yarisi kadin.");
            // Liman sehri cocuk doludur ama ucte biri degildir
            // (InsanDNA'nin kendi olcumu).
            Assert.That(cocuk / (float)n, Is.InRange(0.08f, 0.32f),
                $"Cocuk orani %{cocuk * 100f / n:0}.");
            Assert.Greater(yasli, 0,
                "Sokakta hic yasli yok — nufus piramidi degil dikdortgen.");

            // Kullanilmayan arketip, uretilmis ama sokaga hic cikmamis
            // demektir: uretim bedeli odenmis, karsiligi alinmamis.
            foreach (var p in hepsi)
                Assert.IsTrue(sayim.ContainsKey(p.name),
                    $"{p.name} bin iki yuz tohumda HIC secilmedi — "
                    + "bu govde oyunda hic gorunmez.");
        }

        /// <summary>
        /// <b>Sakinler yürüyor mu</b> — gövde var diye animasyon var demek
        /// değil.
        ///
        /// <c>NPCYonetici</c> yıllardır her karede <c>SetFloat("hiz", …)</c>
        /// çağırıyordu; sakin gövdelerinin Animator'ında ise hiçbir
        /// kontrolcü yoktu. Kontrolcüsüz bir Animator'da <c>SetFloat</c>
        /// sessizce hiçbir şey yapar — yani dokuz bin kişi bind pozunda
        /// kayarak yürüyor ve <b>hiçbir test kırmızı dönmüyordu</b>.
        /// Sessiz kalan bir çağrı, ölçülmediği sürece çalışıyor sanılır.
        /// </summary>
        [Test]
        public void EveryResidentBodyCanActuallyAnimate()
        {
            var hepsi = Arketipler();
            if (hepsi.Length == 0) Assert.Inconclusive("arketip yok");

            foreach (var p in hepsi)
            {
                var an = p.GetComponentInChildren<Animator>(true);
                Assert.IsNotNull(an, $"{p.name}: Animator yok.");
                Assert.IsNotNull(an.runtimeAnimatorController,
                    $"{p.name}: Animator kontrolcusu YOK — NPCYonetici'nin "
                    + "SetFloat(\"hiz\") cagrisi sessizce hicbir sey yapar "
                    + "ve sakin bind pozunda kayar. Kur: Hezarfen > Boru "
                    + "Hatti > Animator kontrolcusunu uret.");
                Assert.IsFalse(an.applyRootMotion,
                    $"{p.name}: kok hareketi acik — sakin hem klibin hem "
                    + "NPCAjan'in yer degistirmesini alir ve yoldan cikar.");

                var ac = an.runtimeAnimatorController as AnimatorController;
                if (ac == null) continue;   // override controller olabilir
                Assert.IsTrue(ac.parameters.Any(pr => pr.name == "hiz"),
                    $"{p.name}: kontrolcude 'hiz' parametresi yok — "
                    + "NPCYonetici'nin yazdigi tek deger o.");
            }
        }

        /// <summary>
        /// <b>Her arketipin kişiden kişiye değişen bir kumaşı var mı.</b>
        ///
        /// Gövde çeşitliliği siluet verir, ton çeşitliliği aynı siluetin
        /// tekrarını kırar. İkisinden biri eksikse kalabalık yine
        /// kopyalanmış görünür — ve bu tam olarak oldu: yedi gövde
        /// üretildi, ama <c>NPCYonetici.Boyanir</c> üç kumaş adı
        /// tanıyordu ve kadının feracesi ile çocuğun takkesi listede
        /// yoktu. Şehirdeki bütün kadınlar aynı mor.
        /// </summary>
        /// <summary>
        /// Bilerek beyaz bırakılan kumaşlar — dönemde ağartılmış keten.
        ///
        /// Liste bir muafiyet değil bir <b>karar kaydı</b>: bu adların
        /// boyanmaması bir unutma değil, kaynağın söylediği şey.
        /// </summary>
        private static readonly string[] BeyazKalan =
            { "M_Cloth_Sarik", "M_Cloth_Yasmak", "M_Cloth_Gomlek",
              "M_Cloth_Kavuk" };

        [Test]
        public void EveryArchetypeHasAGarmentThatVariesPerPerson()
        {
            // ILK YAZIMI KUSURU YAKALAMIYORDU.
            //
            // "En az bir kumasi boyaniyor mu" diye soruyordu ve kadinda
            // salvar boyandigi icin YESIL doniyordu — oysa salvar
            // feracenin ALTINDA, hic gorunmuyor. Bir testin gecmesi,
            // ustundeki mor feracenin herkeste ayni oldugunu
            // degistirmiyordu.
            //
            // Dogru soru "her kumas hakkinda bir KARAR verilmis mi":
            // ya tona gore boyanir, ya bilerek beyaz birakilir. Yeni
            // bir kumas eklendiginde bu test onu ikisinden birine
            // yazmaya zorlar.
            foreach (var p in Arketipler())
            {
                foreach (var r in p.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null) continue;
                        string ad = m.name;
                        if (!ad.StartsWith("M_Cloth_")) continue;
                        if (NPCYonetici.MalzemeBoyanir(ad)) continue;
                        Assert.Contains(ad, BeyazKalan,
                            $"{p.name}: '{ad}' ne tona gore boyaniyor ne de "
                            + "bilerek beyaz birakilanlar arasinda — bu "
                            + "kumasi giyen herkes ayni renkte olur. Ya "
                            + "NPCYonetici.Boyanir'a ekle, ya buradaki "
                            + "BeyazKalan listesine gerekcesiyle yaz.");
                    }
                }
            }
        }

        [Test]
        public void ScaleIsRelativeToEachBodysOwnHeight()
        {
            var hepsi = Arketipler();
            if (hepsi.Length == 0) Assert.Inconclusive("arketip yok");

            // Olcek `dna.boy / sg.tabanBoy` ve +-%12 ile sinirli.
            // Olculen sey: son boy, arketipin kendi boyundan cok
            // uzaklasmamali — yoksa cocuk govdesi yetiskin boyuna
            // gerilir ve sokakta 1,5 m'lik bebekler yurur.
            for (int i = 0; i < 600; i++)
            {
                var dna = InsanDNA.Uret(i * 104729 + 7);
                var sg = hepsi[NPCYonetici.ArketipSec(hepsi, dna)]
                    .GetComponent<SakinGovde>();
                if (sg == null) continue;
                float olcek = Mathf.Clamp(dna.boy / sg.tabanBoy, 0.88f, 1.12f);
                float sonBoy = sg.tabanBoy * olcek;
                Assert.That(sonBoy, Is.InRange(1.05f, 1.95f),
                    $"tohum {i}: son boy {sonBoy:0.00} m "
                    + $"({sg.name}, taban {sg.tabanBoy:0.00}).");
                if (sg.BandDizini == 0)
                    Assert.Less(sonBoy, 1.45f,
                        $"tohum {i}: cocuk {sonBoy:0.00} m boyunda.");
            }
        }
    }
}
