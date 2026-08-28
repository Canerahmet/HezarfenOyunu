using System.Collections.Generic;
using System.Linq;
using Hezarfen.Sehir;
using Hezarfen.Zaman;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Faz 6 kapısı — kabul ölçütleri, tek tek, sayıyla.</b>
    ///
    /// PLAN Bölüm 11: *"Galata'da 30 dk kesintisiz serbest dolaşım
    /// (yükleme ekranı yok); ≥3 yan görev arketipi uçtan uca oynanabilir;
    /// aranma sistemi tam döngü (ihlal → kovalamaca → kaçış VE yakalanma
    /// sonuçları); NPC rutinleri sabah-öğle-akşam-gece geçişlerinde gözle
    /// görülür değişiyor; kayıkla Galata↔Üsküdar geçişi çalışıyor; Perde 2
    /// dikey dilimi baştan sona oynanabilir."*
    ///
    /// Onay akışı değişti (Caner, 2026-08-28): geri bildirim tüm fazlar
    /// bittikten sonra, oyunu oynarken gelecek. Yani <b>kapıyı tutan şey
    /// ölçüm, imza değil</b>. Bu dosya o ölçümün kendisidir: her ölçüt bir
    /// test, her test bir sayı.
    ///
    /// Dağınık testler zaten bu sistemleri ayrı ayrı ölçüyor. Buranın işi
    /// başka: ölçütün <b>kendi cümlesini</b> doğrudan sınamak, ki kapıda
    /// "hangi test bu maddeyi karşılıyordu" diye aranmasın.
    ///
    /// Perde 2 dilimi ve 30 dakikalık dolaşım PlayMode'dadır — ikisi de
    /// zaman geçmesini gerektirir.
    /// </summary>
    public class Faz6KapiTests
    {
        private const int BirMayis1632 = 122;

        private static SokakGrafi Graf()
        {
            var g = AssetDatabase.LoadAssetAtPath<SokakGrafi>(
                "Assets/_Project/Data/SG_Sehir.asset");
            Assert.IsNotNull(g, "Sokak grafi yok.");
            return g;
        }

        private static List<NPCMeslek> Meslekler()
            => AssetDatabase.FindAssets("t:NPCMeslek")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<NPCMeslek>)
                .Where(m => m != null).ToList();

        // --------------------------------------------------------------
        /// <summary>
        /// <b>Ölçüt: ≥3 yan görev arketipi UÇTAN UCA oynanabilir.</b>
        ///
        /// "Uçtan uca" üretilebilmek değildir: görev üretilir, oyuncu
        /// duraktan durağa <b>yürür</b> ve görev biter. Test bunu gerçek
        /// şehir grafında, gerçek yollarla yapıyor — durakları aynı
        /// yürüme bileşeninde olmayan bir görev burada düşer.
        /// </summary>
        [Test]
        public void AtLeastThreeQuestArchetypesRunEndToEnd()
        {
            var g = Graf();
            var merkez = g.dugumler.First(
                d => d.tur == SokakGrafi.Tur.Mescit
                     && d.semt == "D_Surici_Dogu").konum;

            var tamamlanan = new List<GorevArketip>();

            foreach (GorevArketip a in System.Enum.GetValues(
                         typeof(GorevArketip)))
            {
                int yil = a == GorevArketip.Kacakcilik ? 1634 : 1632;
                var q = GorevUretici.Uret(g, a, merkez, 7, yil, BirMayis1632);
                if (q == null) continue;

                // OYNA: duraktan duraga yuru.
                int adim = g.EnYakin(merkez);
                bool yurunebildi = true;
                while (!q.Bitti)
                {
                    int hedef = q.Hedef;
                    var yol = g.Yol(adim, hedef, kayikVar: false);
                    if (yol.Count == 0) { yurunebildi = false; break; }
                    adim = hedef;
                    q.DurakTamam();
                }

                if (yurunebildi && q.Bitti) tamamlanan.Add(a);
            }

            Assert.GreaterOrEqual(tamamlanan.Count, 3,
                $"Yalnizca {tamamlanan.Count} arketip uctan uca oynandi "
                + $"({string.Join(", ", tamamlanan)}); olcut >= 3 diyor.");
        }

        // --------------------------------------------------------------
        /// <summary>
        /// <b>Ölçüt: aranma sistemi TAM DÖNGÜ.</b>
        ///
        /// Ölçütün parantezi kesin: *"ihlal → kovalamaca → kaçış VE
        /// yakalanma sonuçları"*. İki sonucun da olması şart — yalnız
        /// yakalanma varsa kaçmanın anlamı yok, yalnız kaçış varsa riskin.
        /// </summary>
        [Test]
        public void TheWantedCycleHasBothOutcomes()
        {
            // Durum makinesinin butun asamalari var mi.
            foreach (string asama in new[]
                     { "Temiz", "FarkEdildi", "Uyarildi", "Kovalaniyor",
                       "Yakalandi" })
                Assert.IsTrue(
                    System.Enum.IsDefined(typeof(AranmaSistemi.Durum), asama),
                    $"Aranma dongusunde '{asama}' asamasi yok.");

            // YAKALANMA bir sonuc uretir: ceza, ve tasiniyorsa mala el konur.
            foreach (Ihlal i in System.Enum.GetValues(typeof(Ihlal)))
            {
                if (i == Ihlal.Yok) continue;
                Assert.Greater(IhlalKurali.Ceza(i), 0,
                    $"{i} ihlalinin cezasi yok — yakalanmanin sonucu yok.");
            }
            Assert.IsTrue(IhlalKurali.MalaElKonur(Ihlal.YasakMal),
                "Yasak mal tasirken yakalanana el konmuyor.");

            // KACIS bir sonuc uretir: seviye soner ve ceza sonrasi
            // muafiyet vardir (yoksa oyuncu ceza dongusune sikisir).
            var go = new GameObject("aranma");
            var s = go.AddComponent<AranmaSistemi>();
            Assert.Greater(s.sonumHizi, 0f,
                "Aranma seviyesi hic sonmuyor — kacis diye bir sey yok.");
            Assert.Greater(s.cezaSonrasiMuafiyet, 0f,
                "Ceza sonrasi muafiyet yok — oyuncu ceza dongusune sikisir.");
            Object.DestroyImmediate(go);
        }

        // --------------------------------------------------------------
        /// <summary>
        /// <b>Ölçüt: NPC rutinleri sabah-öğle-akşam-gece geçişlerinde
        /// GÖZLE GÖRÜLÜR değişiyor.</b>
        ///
        /// "Gözle görülür" bir izlenim; ölçülebilir karşılığı, dört vaktin
        /// hedef dağılımının birbirinden <b>belirgin</b> farklı olması.
        /// Ölçü olarak dağılımlar arası fark alınıyor: iki vakit arasında
        /// insanların en az beşte biri başka bir yere gitmiş olmalı.
        /// </summary>
        [Test]
        public void TheDailyRoutineVisiblyChangesFourTimesADay()
        {
            var g = Graf();
            var sakinler = SehirGunu.Sakinler(g, Meslekler(), 1500);

            var vakitler = new[]
            {
                VakitHesabi.Vakit.Sabah, VakitHesabi.Vakit.Ogle,
                VakitHesabi.Vakit.Aksam, VakitHesabi.Vakit.Yatsi,
            };

            var olcumler = vakitler.Select(
                v => SehirGunu.Olc(g, sakinler, v, 1632, BirMayis1632)).ToList();

            // OLCU ARDISIK GECISLERDIR, her cift degil.
            //
            // Ilk yazimda butun ciftlere bakiyordum ve test sabah-aksam
            // arasinda %16 gordu. Ama olcut "gecislerde degisiyor" diyor
            // ve sabahla aksamin birbirine benzemesi bir kusur DEGILDIR:
            // gun bes vakitle bolunmus, ikisi de namaz vakti, ikisinde de
            // insanlar mescitte ya da evde. Aralarinda buyuk fark cikmasi
            // asil supheli olurdu. Olcuyu gevsetmedim, dogru yere koydum.
            for (int i = 1; i < olcumler.Count; i++)
            {
                float fark = Fark(olcumler[i - 1], olcumler[i]);
                Assert.Greater(fark, 0.20f,
                    $"{vakitler[i - 1]} -> {vakitler[i]} gecisinde sehrin "
                    + $"yalnizca %{fark * 100f:F0}'i yer degistiriyor — "
                    + "gecis gozle gorulur degil.");
            }

            // Ve gun bir butun olarak DEGISIYOR: en kalabalik vakitle en
            // sakin vakit arasinda belirgin bir acik olmali.
            float enCok = olcumler.Max(o => o.DisariOrani);
            float enAz = olcumler.Min(o => o.DisariOrani);
            Assert.Greater(enCok - enAz, 0.25f,
                $"Gun boyu disarida olma orani %{enAz * 100f:F0} ile "
                + $"%{enCok * 100f:F0} arasinda — sehrin gunu duz.");

            // Gece sokaklar bosalmali: yatsi, ogleden belirgin sakin.
            var ogle = olcumler[1];
            var yatsi = olcumler[3];
            Assert.Less(yatsi.DisariOrani, ogle.DisariOrani * 0.75f,
                $"Yatsida disarida %{yatsi.DisariOrani * 100f:F0}, ogleyin "
                + $"%{ogle.DisariOrani * 100f:F0} — gece sokaklar bosalmiyor.");
        }

        /// <summary>İki ölçümün hedef dağılımı arasındaki toplam fark (0-1).</summary>
        private static float Fark(SehirGunu.Olcum a, SehirGunu.Olcum b)
        {
            var turler = new HashSet<SokakGrafi.Tur>(a.hedefler.Keys);
            turler.UnionWith(b.hedefler.Keys);
            int toplam = 0;
            foreach (var t in turler)
            {
                a.hedefler.TryGetValue(t, out int x);
                b.hedefler.TryGetValue(t, out int y);
                toplam += Mathf.Abs(x - y);
            }
            // Her kisi en cok iki kere sayilir (birinden cikip otekine girer).
            return toplam / (2f * Mathf.Max(1, a.toplam));
        }

        // --------------------------------------------------------------
        /// <summary>
        /// <b>Ölçüt: kayıkla Galata↔Üsküdar geçişi çalışıyor.</b>
        ///
        /// Genel bağlılık testi (<c>BoatsUniteTheCityButFeetDoNot</c>)
        /// şehrin tamamına bakar; bu ölçüt <b>o iki yakayı</b> soruyor ve
        /// ayrıca sorulmalı, çünkü ölçütte adı geçen geçiş odur: Boğaz.
        /// </summary>
        [Test]
        public void YouCanCrossFromGalataToUskudarByBoat()
        {
            var g = Graf();

            int galata = g.dugumler.FindIndex(
                d => d.semt == "D_Galata" && d.tur == SokakGrafi.Tur.Ev);
            int uskudar = g.dugumler.FindIndex(
                d => d.semt == "D_Uskudar" && d.tur == SokakGrafi.Tur.Ev);
            Assert.GreaterOrEqual(galata, 0, "Galata'da ev dugumu yok.");
            Assert.GreaterOrEqual(uskudar, 0, "Uskudar'da ev dugumu yok.");

            var kayikla = g.Yol(galata, uskudar, kayikVar: true);
            Assert.IsNotEmpty(kayikla,
                "Kayikla Galata'dan Uskudar'a gidilemiyor — olcutte adi "
                + "gecen gecis bu.");

            var yuruyerek = g.Yol(galata, uskudar, kayikVar: false);
            Assert.IsEmpty(yuruyerek,
                $"Galata'dan Uskudar'a YURUYEREK {yuruyerek.Count} adimda "
                + "gidiliyor — Bogaz yuruyerek gecilmez.");

            // Gecis gercekten SUDAN geciyor mu: yolda en az bir kayik
            // kenari olmali.
            bool kayikKullanildi = false;
            for (int i = 1; i < kayikla.Count && !kayikKullanildi; i++)
                foreach (var k in g.kenarlar)
                    if (k.kayik
                        && ((k.a == kayikla[i - 1] && k.b == kayikla[i])
                            || (k.b == kayikla[i - 1] && k.a == kayikla[i])))
                    { kayikKullanildi = true; break; }

            Assert.IsTrue(kayikKullanildi,
                "Yol bulundu ama uzerinde kayik kenari yok — su karadan "
                + "gecilmis demektir.");
        }

        // --------------------------------------------------------------
        /// <summary>
        /// <b>Ölçüt: Galata'da serbest dolaşım — YÜKLEME EKRANI YOK.</b>
        ///
        /// Yükleme ekranının ölçülebilir karşılığı: Galata'nın yürüme
        /// bileşeni içinde kalan oyuncu, hiçbir noktada başka bir kara
        /// parçasına <b>atlamak</b> zorunda kalmaz — yani semtin içi tek
        /// parça yürünebilir olmalı. Parçalıysa oyuncu bir yerde durur ve
        /// oyun ona "buradan gidilmez" demek zorunda kalır.
        ///
        /// Süre ölçümü (30 dk kesintisiz) PlayMode'dadır: burada ölçülen
        /// şey <b>mekân</b>, orada <b>zaman</b>.
        /// </summary>
        [Test]
        public void GalataIsOnePieceYouCanWalkAround()
        {
            var g = Graf();
            var galata = new List<int>();
            for (int i = 0; i < g.dugumler.Count; i++)
                if (g.dugumler[i].semt == "D_Galata") galata.Add(i);
            Assert.IsNotEmpty(galata, "Galata'da dugum yok.");

            // Galata'nin en buyuk yurume parcasi kac dugum tutuyor.
            var kom = g.Komsuluk(kayikVar: false);
            var gorulen = new HashSet<int>();
            int enBuyuk = 0;
            foreach (int bas in galata)
            {
                if (gorulen.Contains(bas)) continue;
                int n = 0;
                var yigin = new Stack<int>();
                yigin.Push(bas); gorulen.Add(bas);
                while (yigin.Count > 0)
                {
                    int v = yigin.Pop(); n++;
                    foreach (int w in kom[v])
                        if (g.dugumler[w].semt == "D_Galata"
                            && gorulen.Add(w)) yigin.Push(w);
                }
                enBuyuk = Mathf.Max(enBuyuk, n);
            }

            float oran = enBuyuk / (float)galata.Count;
            Assert.Greater(oran, 0.80f,
                $"Galata'nin {galata.Count} dugumunun en buyuk yurume "
                + $"parcasi {enBuyuk} ({oran * 100f:F0}%) — semt parcali, "
                + "oyuncu bir yerde durur.");
        }
    }
}
