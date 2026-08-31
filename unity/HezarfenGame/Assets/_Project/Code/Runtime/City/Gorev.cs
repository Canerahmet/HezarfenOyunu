using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Yan görev arketipleri.</b> Plan Bölüm 11.2'nin tablosu.
    ///
    /// Her arketip bir <b>şablon</b>; varyasyonlar (kim, nerede, ne yük,
    /// ne ödül) veriden üretilir. Sekiz arketipin hepsi dönem dayanağına
    /// bağlı — bu bir süs değil, görevin ne olduğunu belirleyen şey:
    /// "kutuyu A'dan B'ye götür" her oyunda var, "iskeleden çarşıya yük
    /// taşı, çünkü Haliç'te köprü yok" yalnız burada.
    /// </summary>
    public enum GorevArketip
    {
        /// <summary>Hamal/ulak teslimatı — iskele-çarşı yük akışı.</summary>
        Teslimat = 0,

        /// <summary>Kayık yolcu taşıma — Haliç pereme trafiği.</summary>
        KayikYolcu = 1,

        /// <summary>Kayıp eşya/kişi izleme — mahalle-imam kefalet düzeni.</summary>
        Kayip = 2,

        /// <summary>
        /// Kahve/tütün kaçakçılığı — <b>yalnız 2 Eylül 1633'ten sonra</b>.
        /// Yüksek akçe, yüksek aranma riski.
        /// </summary>
        Kacakcilik = 3,

        /// <summary>Esnaf tedarik zinciri — Evliya'nın lonca listesi.</summary>
        Tedarik = 4,
    }

    /// <summary>Ödül türü — plan Bölüm 11.2'nin sağ sütunu.</summary>
    [Flags]
    public enum Odul
    {
        Yok = 0,
        Akce = 1,
        LoncaItibari = 2,
        MahalleItibari = 4,
        BeceriPuani = 8,
        Kodeks = 16,
    }

    /// <summary>
    /// <b>Üretilmiş bir görev.</b> Şablondan çıkmış somut bir iş.
    ///
    /// Durak listesi grafın düğüm indeksleridir: görev "bir yere git"
    /// demez, <b>şehirdeki bir yere</b> gider. Bu, üretilen her görevin
    /// tamamlanabilirliğinin ölçülebilmesini sağlıyor — duraklar aynı
    /// bileşende mi, oraya yürünebiliyor mu.
    /// </summary>
    [Serializable]
    public class Gorev
    {
        public GorevArketip arketip;
        public string baslik;

        /// <summary>Sırayla gidilecek düğümler.</summary>
        public List<int> duraklar = new();

        public Odul odul;
        public int akce;

        /// <summary>Bu görev yasak mal taşıtıyor mu (aranma riski).</summary>
        public bool yasakMal;

        /// <summary>Kaçıncı durakta olduğu.</summary>
        public int siradaki;

        public bool Bitti => siradaki >= duraklar.Count;

        /// <summary>Şu an gidilecek düğüm; bittiyse -1.</summary>
        public int Hedef => Bitti ? -1 : duraklar[siradaki];

        /// <summary>Bir durağı tamamlar.</summary>
        public void DurakTamam() { if (!Bitti) siradaki++; }
    }

    /// <summary>
    /// <b>Görev üreticisi:</b> şablon + şehir → somut görev.
    ///
    /// ## Neden üretiliyor, elle yazılmıyor
    ///
    /// Plan Bölüm 11.2: *"varyasyonlar (NPC, yer, yük, ödül) veriden
    /// üretilir — el yapımı 'altın' görevler + şablon türevleri
    /// karışımı."* Elle yazılmış otuz teslimat görevi, otuz kere aynı
    /// cümleyi farklı adlarla yazmaktır; üretilen görev şehrin kendi
    /// dokusunu kullanır ve mahalle değişince görev de değişir.
    ///
    /// ## Üretilen görev TAMAMLANABILIR olmalı
    ///
    /// Bir görev, durakları aynı yürüme bileşeninde değilse
    /// tamamlanamaz — oyuncu Üsküdar'daki bir dükkâna Galata'dan yük
    /// götüremez (arada Boğaz var). Üretici bunu <b>kontrol eder</b>,
    /// çünkü tamamlanamayan bir görev sessizdir: oyuncu yürür, yürür,
    /// varamaz ve oyunu suçlar.
    /// </summary>
    public static class GorevUretici
    {
        /// <summary>Bu arketip bu tarihte üretilebilir mi.</summary>
        public static bool Uygun(GorevArketip a, int yil, int gun)
        {
            return a switch
            {
                // Kacakcilik YASAK DONEMININ isidir: yasak yokken
                // kacirilacak bir sey de yoktur.
                GorevArketip.Kacakcilik => !Kronoloji.KahvehaneAcik(yil, gun),
                _ => true,
            };
        }

        /// <summary>
        /// Bir görev üretir; üretilemezse `null`.
        ///
        /// `null` dönmek bir hata değil bir <b>cevap</b>: o mahallede o
        /// görevin gerektirdiği yerler yoksa görev de yoktur. Zorla
        /// üretmek, oyuncuyu olmayan bir dükkâna yollamak olurdu.
        /// </summary>
        public static Gorev Uret(SokakGrafi graf, GorevArketip arketip,
                                 Vector3 yakininda, int tohum,
                                 int yil = 1632, int gun = 121)
        {
            if (graf == null || !Uygun(arketip, yil, gun)) return null;
            var rng = new System.Random(tohum);

            var turler = Duraklar(arketip);
            if (turler == null) return null;

            var bilesen = BilesenEtiketleri(graf);
            int bas = graf.EnYakin(yakininda);
            if (bas < 0) return null;
            int b = bilesen[bas];

            var duraklar = new List<int>();
            Vector3 son = yakininda;
            foreach (var tur in turler)
            {
                int d = RastgeleAyniBilesende(graf, bilesen, b, tur, son, rng,
                                              duraklar);
                if (d < 0) return null;          // o yer bu semtte yok
                duraklar.Add(d);
                son = graf.dugumler[d].konum;
            }

            var g = new Gorev
            {
                arketip = arketip,
                duraklar = duraklar,
                baslik = Baslik(arketip),
                odul = Odulu(arketip),
                akce = Akce(arketip, rng),
                yasakMal = arketip == GorevArketip.Kacakcilik,
            };
            return g;
        }

        /// <summary>Arketipin durak türleri, sırayla.</summary>
        private static SokakGrafi.Tur[] Duraklar(GorevArketip a) => a switch
        {
            // Iskeleden yuku al, carsiya goturr — Halic'te kopru yok,
            // yuk boyle akar.
            GorevArketip.Teslimat => new[]
            { SokakGrafi.Tur.Iskele, SokakGrafi.Tur.Han, SokakGrafi.Tur.Dukkan },

            // Yolcuyu iskeleden al, obur iskeleye birak.
            GorevArketip.KayikYolcu => new[]
            { SokakGrafi.Tur.Iskele, SokakGrafi.Tur.Iskele },

            // Kefalet duzeni: imam sorar, iz cesmeden mescide gider.
            GorevArketip.Kayip => new[]
            { SokakGrafi.Tur.Mescit, SokakGrafi.Tur.Cesme, SokakGrafi.Tur.Ev },

            // Kacakcilik: hanit al, kahvehaneye degil EVE goturr —
            // kahvehaneler kapali, is artik gizli yapiliyor.
            GorevArketip.Kacakcilik => new[]
            { SokakGrafi.Tur.Han, SokakGrafi.Tur.Ev },

            // Esnaf tedariki: firindan dukkana, degirmenden firina.
            GorevArketip.Tedarik => new[]
            { SokakGrafi.Tur.Han, SokakGrafi.Tur.Firin, SokakGrafi.Tur.Dukkan },

            _ => null,
        };

        private static string Baslik(GorevArketip a) => a switch
        {
            GorevArketip.Teslimat => "Iskeleden carsiya yuk",
            GorevArketip.KayikYolcu => "Karsiya yolcu",
            GorevArketip.Kayip => "Kayip esya",
            GorevArketip.Kacakcilik => "Gizli yuk",
            GorevArketip.Tedarik => "Esnaf tedariki",
            _ => "Gorev",
        };

        private static Odul Odulu(GorevArketip a) => a switch
        {
            GorevArketip.Teslimat => Odul.Akce,
            GorevArketip.KayikYolcu => Odul.Akce | Odul.LoncaItibari,
            GorevArketip.Kayip => Odul.Akce | Odul.Kodeks,
            GorevArketip.Kacakcilik => Odul.Akce,
            GorevArketip.Tedarik => Odul.LoncaItibari | Odul.Akce,
            _ => Odul.Yok,
        };

        /// <summary>
        /// Ödül akçesi — <b>yevmiyeden türer</b>.
        ///
        /// Bir teslimat yarım günlük emek, kaçakçılık birkaç günlük:
        /// risk ödülü belirler. Sayılar elle yazılsaydı ekonominin
        /// çapası değişince sessizce yanlış kalırdı (bkz.
        /// <see cref="Ekonomi"/>).
        /// </summary>
        private static int Akce(GorevArketip a, System.Random rng)
        {
            float gun = a switch
            {
                GorevArketip.Teslimat => 0.5f,
                GorevArketip.KayikYolcu => 0.4f,
                GorevArketip.Kayip => 1.2f,
                // Yuksek akce, yuksek aranma riski (plan Bolum 11.2).
                GorevArketip.Kacakcilik => 4.0f,
                GorevArketip.Tedarik => 1.0f,
                _ => 0f,
            };
            float oynak = 0.85f + (float)rng.NextDouble() * 0.30f;
            return Mathf.Max(1, Mathf.RoundToInt(
                Ekonomi.IrgatYevmiyesi * gun * oynak));
        }

        /// <summary>
        /// Bir sonraki durağın <b>en az</b> bu kadar uzakta olması
        /// gerekir (m).
        ///
        /// Ölçüldü: 60 görevlik gerçek bir dizide 168 durağın **18'i
        /// (%11)** daha üretildiği anda oyuncunun 15 m'lik varış
        /// eşiğinin içindeydi. Yani üç duraklı bir "Kayıp eşya" görevi,
        /// çeşmeye varıldığı anda bitiyor ve üçüncü durak hiç
        /// yaşanmıyordu. En kısa gerçek durak arası 9,1 m ölçüldü —
        /// oysa koddaki yorum "40 m'nin altına inmiyor" diyordu.
        ///
        /// 60 m: varış eşiğinin (15 m) dört katı, yani bir durağa
        /// varmak asla bir sonrakini tetiklemez.
        /// </summary>
        public const float EnAzDurakArasi = 60f;

        private static int RastgeleAyniBilesende(
            SokakGrafi graf, int[] bilesen, int b, SokakGrafi.Tur tur,
            Vector3 yakininda, System.Random rng,
            List<int> kullanilanlar = null)
        {
            var adaylar = new List<int>();
            for (int i = 0; i < graf.dugumler.Count; i++)
            {
                if (graf.dugumler[i].tur != tur || bilesen[i] != b) continue;

                // AYNI DUGUM IKI KEZ SECILMEZ.
                //
                // Galata bileseninde tek iskele var ve `KayikYolcu`
                // arketipi iki iskele istiyor: ureteç aynı düğümü iki
                // kez seçiyordu. Ölçüldü — 60 görevin **12'si (%20)**
                // [1523, 1523] üretti, yani ikinci durağın mesafesi
                // 0,0 m ve görev ilk durağa varan oyuncunun elinde
                // kendiliğinden bitiyordu.
                //
                // "Karşıya yolcu götür" görevinin karşıya gitmemesi,
                // görevin kendisinin olmaması demek.
                if (kullanilanlar != null && kullanilanlar.Contains(i))
                    continue;

                // COK YAKIN DURAK, DURAK DEGIL.
                if (kullanilanlar != null && kullanilanlar.Count > 0)
                {
                    float d2 = (graf.dugumler[i].konum - yakininda)
                               .sqrMagnitude;
                    if (d2 < EnAzDurakArasi * EnAzDurakArasi) continue;
                }

                adaylar.Add(i);
            }
            if (adaylar.Count == 0) return -1;

            // Yakindakileri yegle ama hep en yakini secme: ayni arketip
            // hep ayni yeri kullanirsa varyasyon olmaz.
            adaylar.Sort((x, y) =>
                (graf.dugumler[x].konum - yakininda).sqrMagnitude.CompareTo(
                (graf.dugumler[y].konum - yakininda).sqrMagnitude));
            int havuz = Mathf.Min(adaylar.Count, 8);
            return adaylar[rng.Next(havuz)];
        }

        private static int[] BilesenEtiketleri(SokakGrafi graf)
        {
            var kom = graf.Komsuluk(kayikVar: false);
            var etiket = new int[graf.dugumler.Count];
            for (int i = 0; i < etiket.Length; i++) etiket[i] = -1;
            int c = 0;
            var yigin = new Stack<int>();
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
            return etiket;
        }
    }
}
