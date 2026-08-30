using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hezarfen.Sehir;
using Hezarfen.Zaman;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Faz 6 kapısı — "Galata'da 30 dk kesintisiz serbest dolaşım".</b>
    ///
    /// ## Bu test ne ölçer, ne ölçmez
    ///
    /// <b>Ölçmez:</b> otuz dakikalık gerçek oturum. Bir test otuz dakika
    /// koşamaz ve koşsaydı da kimse çalıştırmazdı. O ölçüm elle oynanan
    /// bir oturuma aittir — ve zaten onay akışı da öyle kuruldu (Caner,
    /// 2026-08-28: geri bildirim oyun oynanırken gelecek).
    ///
    /// <b>Ölçer:</b> otuz dakikalık bir oturumun <b>ne yaparsa kırılacağını</b>.
    /// Kesintisiz dolaşımı bozan şey süre değil <b>birikimdir</b>: gövde
    /// havuzu büyür, vakit geçişleri sızdırır, uzaklaşıp dönünce şehir
    /// donar. Test bunları sıkıştırılmış biçimde yapıyor — bütün gün
    /// döngüsü, defalarca uzaklaşma-dönme, yüzlerce hedef yenilemesi.
    ///
    /// Birikim yoksa süre bir şey bozmaz; birikim varsa otuz dakika
    /// beklemeye gerek kalmaz, burada görünür.
    /// </summary>
    public class Faz6DolasimTests
    {
        private GameObject _kok, _oyuncuGo, _zamanGo, _prefab;

        /// <summary>Test başlarken sahnede duran gövde nesnesi sayısı.</summary>
        private int _baslangictakiGovde;

        private (NPCYonetici y, ZamanSistemi z) Kur(int sakin = 400,
                                                    int butce = 40)
        {
            var g = ScriptableObject.CreateInstance<SokakGrafi>();
            // Galata olcusunde bir izgara: 8x8 dugum, 60 m arayla.
            var turler = new[]
            {
                SokakGrafi.Tur.Ev, SokakGrafi.Tur.Dukkan,
                SokakGrafi.Tur.Mescit, SokakGrafi.Tur.Cesme,
                SokakGrafi.Tur.Firin, SokakGrafi.Tur.Han,
                SokakGrafi.Tur.Cami, SokakGrafi.Tur.Mektep,
            };
            for (int x = 0; x < 8; x++)
                for (int z = 0; z < 8; z++)
                    g.dugumler.Add(new SokakGrafi.Dugum
                    {
                        konum = new Vector3(x * 60f, 0f, z * 60f),
                        tur = turler[(x + z) % turler.Length],
                        semt = "D_Galata",
                    });

            void K(int a, int b) => g.kenarlar.Add(new SokakGrafi.Kenar
            { a = a, b = b, uzunluk = Vector3.Distance(
                g.dugumler[a].konum, g.dugumler[b].konum) });
            for (int x = 0; x < 8; x++)
                for (int z = 0; z < 8; z++)
                {
                    int i = x * 8 + z;
                    if (x < 7) K(i, (x + 1) * 8 + z);
                    if (z < 7) K(i, x * 8 + z + 1);
                }

            var meslek = ScriptableObject.CreateInstance<NPCMeslek>();
            meslek.tip = NPCMeslek.Tip.Esnaf;
            meslek.pay = 1f;
            meslek.cizelge = new List<NPCMeslek.Adim>();
            foreach (VakitHesabi.Vakit v in
                     System.Enum.GetValues(typeof(VakitHesabi.Vakit)))
            {
                var hedef = v == VakitHesabi.Vakit.Yatsi
                    ? SokakGrafi.Tur.Ev
                    : (v == VakitHesabi.Vakit.Ogle
                        ? SokakGrafi.Tur.Mescit : SokakGrafi.Tur.Dukkan);
                meslek.cizelge.Add(new NPCMeslek.Adim
                { vakit = v, hedef = hedef, olasilik = 1f, disarida = true });
            }

            _baslangictakiGovde = Object.FindObjectsByType<GameObject>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(o => o.name.StartsWith("GovdePrefab"));

            _prefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _prefab.name = "GovdePrefab";
            _prefab.SetActive(false);

            _oyuncuGo = new GameObject("dolasim_oyuncu");
            _zamanGo = new GameObject("dolasim_zaman");
            var z2 = _zamanGo.AddComponent<ZamanSistemi>();
            z2.gunDakika = 0f;
            z2.gunesiSur = false;
            z2.yilinGunu = 122;
            z2.saat = 6f;
            z2.Yenile();

            _kok = new GameObject("dolasim_yonetici");
            var y = _kok.AddComponent<NPCYonetici>();
            y.graf = g;
            y.meslekler = new List<NPCMeslek> { meslek };
            y.sakinSayisi = sakin;
            y.govdeButcesi = butce;
            y.gorunurMesafe = 90f;
            y.dilim = 8;
            y.govdePrefab = _prefab;
            y.oyuncu = _oyuncuGo.transform;
            y.zaman = z2;
            return (y, z2);
        }

        [TearDown]
        public void Temizle()
        {
            foreach (var go in new[] { _kok, _oyuncuGo, _zamanGo, _prefab })
                if (go != null) Object.DestroyImmediate(go);
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go != null && go.name.StartsWith("GovdePrefab"))
                    Object.DestroyImmediate(go);
        }

        /// <summary>
        /// <b>Gün boyu dolaşım şehri yormuyor.</b>
        ///
        /// Bütün vakitlerden defalarca geçilir, oyuncu şehirde dolaşır ve
        /// arada uzaklaşıp döner. Ölçülen şey <b>birikim</b>: gövde sayısı
        /// bütçeyi aşmamalı, gövde nesneleri çoğalmamalı ve şehir sonunda
        /// hâlâ yürüyor olmalı.
        /// </summary>
        [UnityTest]
        public IEnumerator ADayOfWanderingDoesNotWearTheCityDown()
        {
            var (y, z) = Kur();
            yield return null;

            int enCokGovde = 0;
            var vakitler = (VakitHesabi.Vakit[])System.Enum.GetValues(
                typeof(VakitHesabi.Vakit));

            // Uc gunluk dongu: her vakitten defalarca gecilir.
            for (int tur = 0; tur < 3; tur++)
            {
                foreach (var v in vakitler)
                {
                    z.VakteAtla(v);
                    yield return null;

                    // Oyuncu sehirde dolassin.
                    for (int adim = 0; adim < 4; adim++)
                    {
                        _oyuncuGo.transform.position = new Vector3(
                            Random.Range(0f, 420f), 0f, Random.Range(0f, 420f));
                        yield return null;
                        enCokGovde = Mathf.Max(enCokGovde, y.GorunurSayisi);
                    }

                    // UZAKLAS ve DON: havuzun sizdirdigi yer burasi.
                    _oyuncuGo.transform.position = new Vector3(9000f, 0f, 9000f);
                    yield return null;
                    Assert.AreEqual(0, y.GorunurSayisi,
                        "Sehirden uzaklasildi ama gövdeler hala cizili.");
                }
            }

            Assert.LessOrEqual(enCokGovde, y.govdeButcesi,
                $"En cok {enCokGovde} govde cizildi, butce {y.govdeButcesi} "
                + "— butce asilirsa 30 dakikalik oturum kare duserek biter.");

            // GOVDE NESNELERI COGALMADI MI: havuz yeniden kullanmali.
            //
            // OLCU YONETICININ KENDI SAYACI, sahneyi ada gore taramak
            // DEGIL. Once sahne taraniyordu ve bu olcu IKI KEZ yanlis
            // alarm verdi (66 ve 57): saydigi sey bu yoneticinin havuzu
            // degil, onceki testlerden kalan nesnelerdi. Bir olcu,
            // kendisiyle ilgisiz degisikliklerde patliyorsa yanlis seyi
            // sayiyordur.
            Assert.LessOrEqual(y.UretilenGovde, y.govdeButcesi,
                $"Havuz {y.UretilenGovde} govde uretti, butce "
                + $"{y.govdeButcesi} — sizdiriyor; uzun oturumda bellek "
                + "buyur.");

            // SEHIR HALA YURUYOR MU: don ve hareket olctur.
            //
            // ÖLÇÜM GÜNDÜZ YAPILIR — YATSIDA DURMAK DOĞRUDUR.
            //
            // Bu ölçüm döngünün bıraktığı vakitte yapılıyordu ve o vakit
            // enum'un sonuncusu, yani <b>Yatsı</b>. Yatsıda rutin herkesi
            // EVE gönderir; evinde olan bir sakinin yolu tek düğümdür ve
            // kımıldamaması <b>doğru davranıştır</b>. Test bunu yine de
            // geçiyordu, çünkü gün boyunca kareler yavaştı: replik
            // korpusu her sakin için 5.088 satır tarıyordu, kare uzuyordu,
            // <c>dt</c> büyüyordu ve sakinler evlerinden uzaklaşacak kadar
            // yol alıyordu. Korpus önbelleğe alınıp kareler hızlanınca
            // sakinler evde kaldı ve test kırmızı yandı — <b>ölçtüğü şey
            // yürüyüş değil, kendi yavaşlığıydı.</b>
            //
            // Ölçüm artık hedefin evden BAŞKA olduğu bir vakitte yapılır:
            // "şehir hâlâ yürüyor mu" sorusu böyle sorulur.
            z.VakteAtla(VakitHesabi.Vakit.Ogle);
            _oyuncuGo.transform.position = new Vector3(200f, 0f, 200f);
            yield return null;
            yield return null;
            var once = y.Sakinler.Select(a => a.konum).ToList();
            float t = 0f;
            while (t < 1.2f) { t += Time.deltaTime; yield return null; }

            int kimildayan = 0;
            for (int i = 0; i < once.Count; i++)
                if ((y.Sakinler[i].konum - once[i]).sqrMagnitude > 0.01f)
                    kimildayan++;

            Assert.Greater(kimildayan, y.Sakinler.Count / 10,
                $"{y.Sakinler.Count} sakinin yalnizca {kimildayan} tanesi "
                + "kimildiyor — gun sonunda sehir dondu.");
        }

        /// <summary>
        /// <b>Vakit geçişi kare düşürmüyor.</b>
        ///
        /// Vakit değiştiğinde bütün sakinlerin hedefi yenilenir ve yol
        /// aranır. Bu, gün içinde altı kez olan tek toplu iştir; oyuncu
        /// için ezan vaktinde takılan bir oyun demek olurdu.
        /// </summary>
        [UnityTest]
        public IEnumerator ThePrayerCallDoesNotStallTheGame()
        {
            var (y, z) = Kur(sakin: 800, butce: 40);
            _oyuncuGo.transform.position = new Vector3(200f, 0f, 200f);
            yield return null;
            yield return null;

            var saat = new System.Diagnostics.Stopwatch();
            double enUzun = 0;

            foreach (VakitHesabi.Vakit v in
                     System.Enum.GetValues(typeof(VakitHesabi.Vakit)))
            {
                saat.Restart();
                z.VakteAtla(v);
                yield return null;           // yenileme bu karede olur
                saat.Stop();
                enUzun = System.Math.Max(enUzun, saat.Elapsed.TotalMilliseconds);
            }

            // 800 sakin icin esik cömert ama sonsuz degil: yarim saniyelik
            // bir takilma ezan vaktinde her seferinde hissedilir.
            Assert.Less(enUzun, 500.0,
                $"Vakit gecisi en cok {enUzun:F0} ms surdu — 800 sakinle "
                + "ezan vaktinde oyun takiliyor.");
        }
    }
}
