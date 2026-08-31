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

            // KAYIK GOREVI KAYIGI KULLANABILIR.
            //
            // Bilesen etiketleri `kayikVar: false` ile cikariliyordu —
            // yani "yalniz yuruyerek erisilebilen" kumeler. Bu dogru
            // varsayilan, ama `KayikYolcu` icin yanlisti: "karsiya
            // yolcu gotur" gorevi, tanimi geregi kayik kenarini
            // gecmelidir. Ayni yuruyus bileseninde iki iskele
            // arayinca Galata'daki TEK iskeleyi buluyor ve gorev
            // ya uretilemiyor ya da karsiya hic gitmiyordu.
            var bilesen = BilesenEtiketleri(
                graf, arketip == GorevArketip.KayikYolcu);
            int bas = graf.EnYakin(yakininda);
            if (bas < 0) return null;
            int b = bilesen[bas];

            // YOL BUTCESI: EN IYISINI SEC, ILKINI DEGIL.
            //
            // Aday suzgeci her duragi TEK BASINA yakin tutuyor ama
            // dizinin TOPLAMINI hic sormuyordu. Uc yakin durak
            // birbirinden uzaga dizilirse gorev yine uzun olur —
            // olculdu, gercek grafta 2.206 m.
            //
            // Cozum bir kisit degil bir SECIM: ayni tohum ailesinden
            // birkac aday dizi uretilir ve en kisasi alinir. Kisit
            // koymak ureteci bogardi (dar bilesende hicbir dizi
            // butceyi tutmayabilir); secmek her zaman bir cevap verir
            // ve cevabin iyi olma ihtimalini artirir.
            List<int> enIyiDizi = null;
            float enIyiYol = float.MaxValue;

            for (int deneme = 0; deneme < DiziDenemesi; deneme++)
            {
                var aday = new List<int>();
                var adayRng = new System.Random(tohum + deneme * 7919);
                Vector3 nokta = yakininda;
                float yol = 0f;
                bool tam = true;

                foreach (var tur in turler)
                {
                    int d = RastgeleAyniBilesende(graf, bilesen, b, tur,
                                                  nokta, adayRng, aday);
                    if (d < 0) { tam = false; break; }
                    yol += (graf.dugumler[d].konum - nokta).magnitude;
                    aday.Add(d);
                    nokta = graf.dugumler[d].konum;
                }

                if (!tam) continue;
                if (yol >= enIyiYol) continue;
                enIyiYol = yol;
                enIyiDizi = aday;
                if (yol <= YolButcesi) break;   // yeterince iyi
            }

            if (enIyiDizi == null) return null;   // o yer bu semtte yok
            var duraklar = enIyiDizi;

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
            //
            // HAN ARADAN CIKTI (ADR 0085). Olculdu: bu arketip dogum
            // noktasindan **2.731 m** uretiyordu ve bu yuzden hic
            // secilmiyordu — 20 gorevin 20'si `Kayip` cikmisti.
            // Sebep grafta: Galata'nin yuruyus bileseninde TEK han var.
            //
            // Ve bu bir dunya kusuru DEGIL. RESEARCH §Yapi envanteri
            // 1632'de buyuk hanlarin yoklugunu acikca kayda geciyor
            // (Buyuk Yeni Han ~1761, Buyuk Valide Han tartismali).
            // Yani han kitligi tarihsel olarak DOGRU; uyarlanacak olan
            // gorev, dunya degil. Iddia zaten "yuk sudan gelir,
            // carsiya gider" — ve o iddia hansiz da ayakta.
            GorevArketip.Teslimat => new[]
            { SokakGrafi.Tur.Iskele, SokakGrafi.Tur.Dukkan,
              SokakGrafi.Tur.Dukkan },

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
            //
            // YORUM ILE KOD AYRI SEY SOYLUYORDU. Yorum "firindan
            // dukkana" diyor, kod ilk durak olarak **Han** istiyordu —
            // ve han Galata'da bir tane. Olculen sonuc 1.985 m'ydi,
            // yani arketip hic secilmiyordu. Kodu kendi yazili
            // niyetine getirmek, hem daha kisa hem daha dogru.
            GorevArketip.Tedarik => new[]
            { SokakGrafi.Tur.Dukkan, SokakGrafi.Tur.Firin,
              SokakGrafi.Tur.Dukkan },

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

        /// <summary>
        /// Bir sonraki durağın <b>tercihen</b> bu kadar yakın olması
        /// (m).
        ///
        /// Kuş uçuşu 400 m, gerçek yolda ölçülen orana (Galata'da
        /// medyan 1,48) göre ~590 m yürüyüş — koşarak iki dakika.
        /// Üç duraklı bir görev böylece 5-7 dakikada biter; bugün
        /// ölçülen 28,2 dakikaydı.
        ///
        /// <b>Tercihen</b>, çünkü bu bant boş kalırsa üretici yine de
        /// bir durak bulmalı. Bir kısıt üreteci boğarsa kusuru
        /// düzeltmez, sistemi susturur.
        /// </summary>
        public const float UygunDurakUzakligi = 400f;

        /// <summary>
        /// Bir görevin kuş uçuşu toplam yolu, hedef (m).
        ///
        /// Gerçek yolda ölçülen orana (medyan 1,48) göre ~1.330 m
        /// yürüyüş, koşarak 3,7 dakika. Bir işin bir turu bu kadar
        /// sürmeli; 28 dakika süren bir tur, tur değil yürüyüştür.
        /// </summary>
        public const float YolButcesi = 900f;

        /// <summary>
        /// Kaç aday dizi denenir.
        ///
        /// Sekiz: dar bir bileşende (Galata'da tek han) çoğu deneme
        /// aynı düğüme düşer ve fark yaratmaz; geniş bileşende sekiz
        /// deneme bütçeyi tutan bir dizi bulmaya yeter. Daha fazlası
        /// üretim maliyetini görev başına birkaç yüz mikrosaniyeden
        /// milisaniyeye çıkarır ve karşılığı yok.
        /// </summary>
        public const int DiziDenemesi = 8;

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

            adaylar.Sort((x, y) =>
                (graf.dugumler[x].konum - yakininda).sqrMagnitude.CompareTo(
                (graf.dugumler[y].konum - yakininda).sqrMagnitude));

            // HAVUZ SAYIYLA DEGIL MESAFEYLE KAPANIR.
            //
            // Once `Min(adaylar.Count, 8)` yaziliyordu ve gerekcesi
            // "yakindakileri yegle" idi. Ama Galata bileseninde Han
            // **1**, Iskele **1**, Firin 9 — yani aday sayisi 8'in
            // altinda olan her tur icin bu kural HICBIR SEY yapmiyor,
            // havuz butun kumeye esit oluyordu.
            //
            // Bedeli olculdu: gorev basina **3.724 m / 28,2 dakika**
            // yuruyus, ve 20 gorevin 12'si ayni dugumden geciyordu.
            // Oyunun tek tekrarlayan dongusu bir turda yarim saat
            // suruyordu.
            //
            // Mesafe suzgeci once denenir: 45 m'den uzak, 400 m'den
            // yakin. Bos donerse en yakin sekize duser — kisit
            // ureteci BOGMAMALI, yalnizca yonlendirmeli.
            var yakin = new List<int>();
            foreach (int i in adaylar)
            {
                float d = (graf.dugumler[i].konum - yakininda).magnitude;
                if (d > EnAzDurakArasi * 0.75f && d <= UygunDurakUzakligi)
                    yakin.Add(i);
            }
            var havuzListe = yakin.Count > 0 ? yakin : adaylar;
            int havuz = Mathf.Min(havuzListe.Count, 8);
            return havuzListe[rng.Next(havuz)];
        }

        /// <summary>Son hesaplanan bileşen etiketleri (graf, kayık) → dizi.</summary>
        private static SokakGrafi _onbellekGraf;
        private static int _onbellekDugum = -1;
        private static readonly int[][] _onbellek = new int[2][];

        /// <summary>
        /// Grafın yürüme (ya da kayıklı) bağlantı bileşenleri.
        ///
        /// <b>Önbellekli, çünkü ölçüldü.</b> Görev üretimi uygun her
        /// arketip için bir kez çağırıyor ve her çağrı grafın
        /// <b>1.541 düğümü</b> için ayrı bir <c>List&lt;int&gt;</c>
        /// ayırıyordu — görev başına dört arketip = 6.164 liste, hepsi
        /// aynı cevabı veriyor. Bu, görev bittiği karede bir çöp
        /// fırtınası demekti; yani oyuncunun bir işi <b>bitirdiği</b>
        /// an takılıyordu.
        ///
        /// Graf çalışma zamanında değişmez. Değişirse (düğüm sayısı
        /// başka) önbellek kendini atar — bir önbellek, geçersiz
        /// olduğunu anlayamıyorsa önbellek değil hatadır.
        /// </summary>
        private static int[] BilesenEtiketleri(SokakGrafi graf,
                                               bool kayikVar = false)
        {
            int k = kayikVar ? 1 : 0;
            if (!ReferenceEquals(_onbellekGraf, graf)
                || _onbellekDugum != graf.dugumler.Count)
            {
                _onbellekGraf = graf;
                _onbellekDugum = graf.dugumler.Count;
                _onbellek[0] = null; _onbellek[1] = null;
            }
            if (_onbellek[k] != null) return _onbellek[k];

            var kom = graf.Komsuluk(kayikVar);
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
            _onbellek[k] = etiket;
            return etiket;
        }
    }
}
