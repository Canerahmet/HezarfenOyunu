using System;
using Hezarfen.Zaman;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Görevi oyuncuya bağlar.</b>
    ///
    /// ## Neden bu sınıf var
    ///
    /// <see cref="GorevUretici"/> beş arketip üretiyor, <see cref="Gorev"/>
    /// durakları grafın düğümlerine oturtuyor, <see cref="Ekonomi"/> ücret
    /// hesaplıyor, <see cref="Kese"/> akçe tutuyor. Hepsi yazılmış, hepsi
    /// test edilmiş — ve <b>hiçbirini oyunda çağıran yoktu</b>. Depoda
    /// arandığında `GorevUretici`, `new Kese`, `FenerVar`,
    /// `DurumuGeriYukle`, `SeviyeyiGeriYukle`, `AsamaDegisti` ve
    /// `SuAnkiIhlal` yalnız <b>kendi tanımlandıkları dosyada</b> geçiyordu.
    ///
    /// Bu, uçuşun başına gelenin aynısı: sistem var, oyuna bağlı değil
    /// (ADR 0082). Orada beş sınıf sahneye konmamıştı; burada altı sınıf
    /// birbirine bağlanmamış.
    ///
    /// Daha kötüsü, Faz 6 kapısı bu hâliyle <b>yeşil</b>di. Kapıyı geçiren
    /// test görevi <b>kendisi oynuyordu</b>:
    /// <c>while (!q.Bitti) { …; q.DurakTamam(); }</c> — durağa varan bir
    /// oyuncu, bir tetikleyici, bir varış algısı yoktu. Ölçüm grafın
    /// tamamlanabilirliğini sorguluyordu, oyuncunun tamamlayabilirliğini
    /// değil. Bir kapının en tehlikeli hâli, yanlış şeyi ölçüp yeşil
    /// yanmasıdır.
    ///
    /// ## Ne yapıyor
    ///
    /// Bir görev üretir, sıradaki durağı söyler, oyuncu 15 m'ye
    /// yaklaşınca durağı tamamlar, bitince akçeyi öder ve yenisini
    /// üretir. Yeni bir mekanik değil — var olanların arasındaki tel.
    ///
    /// ## Yük envanterden geçer
    ///
    /// Teslimat görevinde ilk durakta yük <b>alınır</b>, son durakta
    /// <b>verilir</b>. Böylece <see cref="Envanter"/> ilk kez bir işe
    /// yarar: şehirdeki 15.815 toplanabilir eşyayla aynı dili konuşur ve
    /// "aldığın şey bir yere gidiyor" cümlesi kurulur.
    /// </summary>
    [AddComponentMenu("Hezarfen/Gorev Yonetici")]
    public class GorevYonetici : MonoBehaviour
    {
        [Header("Bağlantılar (boşsa sahnede aranır)")]
        public SokakGrafi graf;
        public Transform oyuncu;
        public ZamanSistemi zaman;
        public Envanter envanter;

        [Tooltip("Durağa bu kadar yaklaşmak yeter (m).")]
        //
        // 15 m: sokak eni 7,2 m (ADR 0075) ve graf dugumu sokagin
        // ortasinda duruyor. Daha dar bir esik oyuncuyu dugumun
        // ustunde dans ettirirdi; daha genis olani komsu duraga
        // tasardi — cunku duraklar arasi en kisa mesafe olculdu ve
        // 40 m'nin altina inmiyor.
        public float varisMesafesi = 15f;

        [Tooltip("Görev tohumu — aynı tohum aynı görev dizisini verir.")]
        public int tohum = 1632;

        /// <summary>Şu anki görev; yoksa null.</summary>
        public Gorev Simdiki { get; private set; }

        /// <summary>Oyuncunun kesesi.</summary>
        public Kese Kese { get; private set; } = new Kese();

        /// <summary>Bu oturumda bitirilen görev sayısı — ölçüm okur.</summary>
        public int Bitirilen { get; private set; }

        /// <summary>Sıradaki durağın dünya konumu; görev yoksa <c>null</c>.</summary>
        public Vector3? HedefKonum
        {
            get
            {
                if (Simdiki == null || Simdiki.Bitti || graf == null) return null;
                int h = Simdiki.Hedef;
                if (h < 0 || h >= graf.dugumler.Count) return null;
                return graf.dugumler[h].konum;
            }
        }

        /// <summary>
        /// Durağa varmak için izin verilen en büyük yükseklik farkı (m).
        ///
        /// <see cref="HedefMesafe"/> yatay ölçüyor ve bu <b>pusula</b>
        /// için doğru; <b>varış</b> için değildi. Kule tepesinden kanadı
        /// açan oyuncu 200 m irtifada mescidin üstünden geçerken durak
        /// 1/3'ü, çeşmenin üstünden geçerken 2/3'ü atlıyor ve eve hiç
        /// inmeden görevi bitiriyordu — akçe keseye düşüyordu.
        ///
        /// Yani oyunun reklam yüzü olan uçuş, oyunun tek döngüsünü
        /// havadan iptal ediyordu. Dört metre: bir evin kat yüksekliği,
        /// merdivenden ya da sedirden varmayı bozmaz, çatıdan varmayı
        /// bozar.
        /// </summary>
        public const float VarisDikeyPayi = 4f;

        /// <summary>Sıradaki durağa yatay mesafe (m); görev yoksa −1.</summary>
        public float HedefMesafe
        {
            get
            {
                var k = HedefKonum;
                if (k == null || oyuncu == null) return -1f;
                var a = new Vector2(oyuncu.position.x, oyuncu.position.z);
                var b = new Vector2(k.Value.x, k.Value.z);
                return Vector2.Distance(a, b);
            }
        }

        [Tooltip("Uçuş durumu — boşsa sahnede aranır. Uçarken durak geçilmez.")]
        public Player.UcusDizisi ucus;

        public event Action<Gorev> GorevBasladi;
        public event Action<Gorev> GorevBitti;

        private int _sayac;

        private void Awake()
        {
            if (oyuncu == null)
            {
                var go = GameObject.Find("OYUNCU");
                if (go != null) oyuncu = go.transform;
            }
            if (zaman == null) zaman = FindAnyObjectByType<ZamanSistemi>();
            if (ucus == null) ucus = FindAnyObjectByType<Player.UcusDizisi>();
            if (envanter == null) envanter = FindAnyObjectByType<Envanter>();
            if (graf == null)
            {
                var y = FindAnyObjectByType<NPCYonetici>();
                if (y != null) graf = y.graf;
            }
        }

        private void Start() => YeniGorev();

        /// <summary>
        /// Yeni bir görev üretir. Arketip <b>tarihe uygun</b> olanlar
        /// arasından seçilir: kaçakçılık 1633 Eylül'ünden önce yoktur
        /// ve olmayan bir görevi üretmeye çalışmak <c>null</c> döner.
        /// </summary>
        /// <summary>
        /// Bir görevin kabul edilebilir en uzun kuş uçuşu yolu (m).
        ///
        /// 1.200 m ≈ gerçek yolda 1.780 m ≈ 2,2 m/s'de <b>13,5 dakika
        /// yürüyüş, koşarak yaklaşık 5</b>. Ölçülen üç hâl:
        /// 3.724 m (28,2 dk, hiçbir kısıt yok) → 882 m (6,7 dk, hep en
        /// yakını — ve 20/20 aynı görev) → bu.
        ///
        /// Sayı bir uzlaşma ve öyle olduğu yazılsın: en kısa turu değil,
        /// <b>her turu farklı ve hâlâ makul</b> olanı seçiyor. Tek tip
        /// 6,7 dakikalık bir tur, çeşitli 9 dakikalıktan kötüdür.
        /// </summary>
        public const float YuruyusTavani = 1200f;

        public void YeniGorev()
        {
            Simdiki = null;
            if (graf == null || oyuncu == null) return;

            int yil = zaman != null ? zaman.yil : 1632;
            int gun = zaman != null ? zaman.yilinGunu : 121;

            // Uygun arketipler arasindan sirayla dene: biri uretilemezse
            // (grafta o turden durak yoksa) otekine gecilir. Sessizce
            // gorevsiz kalmak, bu projede tekrar eden "sistem var ama
            // ulasmiyor" kusurunun yeni bir hali olurdu.
            // ARKETIP SIRAYLA DEGIL, YAKINLIGA GORE SECILIR.
            //
            // Once sirayla deneniyordu ve ilk uretilen alinıyordu.
            // Olculdu: gorev basina 14,6 dakika yuruyus, cunku
            // `Teslimat` ve `Tedarik` Han istiyor ve Galata
            // bileseninde **tek han** var — o arketipler her seferinde
            // sehrin obur ucuna gonderiyordu.
            //
            // Arketiplerin durak turleri tarihsel iddia tasiyor
            // ("iskeleden yuku al, carsiya goturur — Halic'te kopru
            // yok") ve onlari gevsetmek bir TARIH karari olur. Ama
            // hangisinin secilecegi bir muhendislik karari: uygun olan
            // hepsini uret, en yakinini ver.
            //
            // Sonuc: `Kayip` (mescit-cesme-ev; 130 mescit, 272 cesme)
            // Galata'da bol, `Teslimat` seyrek — ve oyuncu yakindakini
            // aliyor. Uzak arketipler kaybolmuyor, sirasi geldiginde
            // ve yakininda karsiligi oldugunda cikiyor.
            // ...VE SONRA BU OLCULDU: 20 gorevin 20'si `Kayip` cikti.
            //
            // "En yakini ver" kurali yuruyusu 3.724 m'den 882 m'ye
            // indirdi ve **cesitliligi sifirladi**. Cunku en yakin
            // olan hep aynidir: Galata'da 130 mescit ve 272 cesme var,
            // 1 han ve 1 iskele. Bir siralama kuralinin birincisi
            // degismiyorsa o kural bir secim degil bir sabittir.
            //
            // Ucuncu hal: **siraya gore don, ama tavani asani atla.**
            // Rotasyon cesitliligi, tavan yuruyusu tutar. Hicbiri
            // tavani tutmuyorsa en kisasi verilir — bir kisit oyunu
            // gorevsiz birakmamali.
            var hepsi = (GorevArketip[])Enum.GetValues(typeof(GorevArketip));
            Gorev enIyi = null, siradaki = null;
            float enIyiYol = float.MaxValue;

            for (int i = 0; i < hepsi.Length; i++)
            {
                var a = hepsi[(_sayac + i) % hepsi.Length];
                if (!GorevUretici.Uygun(a, yil, gun)) continue;
                var g = GorevUretici.Uret(graf, a, oyuncu.position,
                                          tohum + _sayac * 7919, yil, gun);
                if (g == null || g.duraklar.Count == 0) continue;

                float yol = Yol(g);
                if (siradaki == null && yol <= YuruyusTavani) siradaki = g;
                if (yol >= enIyiYol) continue;
                enIyi = g;
                enIyiYol = yol;
            }

            if (siradaki != null) enIyi = siradaki;

            if (enIyi != null)
            {
                Simdiki = enIyi;
                _sayac++;
                GorevBasladi?.Invoke(enIyi);
                return;
            }
            Debug.LogWarning("[Hezarfen] Uretilecek uygun gorev bulunamadi.");
        }

        /// <summary>Görevin kuş uçuşu toplam yolu (m).</summary>
        private float Yol(Gorev g)
        {
            if (graf == null || oyuncu == null) return float.MaxValue;
            float t = 0f;
            var son = oyuncu.position;
            foreach (int d in g.duraklar)
            {
                if (d < 0 || d >= graf.dugumler.Count) return float.MaxValue;
                t += Vector3.Distance(son, graf.dugumler[d].konum);
                son = graf.dugumler[d].konum;
            }
            return t;
        }

        private void Update() => Adimla();

        /// <summary>
        /// Bir adım ilerlet. <c>Update</c>'ten ayrı, çünkü test bunu
        /// <b>zamanı verilebilir</b> biçimde çağırabilmeli: zamana bağlı
        /// bir sistem, zamanı dışarıdan alabilmeli
        /// (<c>Perde2Dilimi.Ilerle</c> ile aynı gerekçe).
        /// </summary>
        public void Adimla()
        {
            if (Simdiki == null || oyuncu == null) return;

            float d = HedefMesafe;
            if (d < 0f || d > varisMesafesi) return;

            // UCARKEN DURAK GECILMEZ.
            //
            // Iki bagimsiz kosul, cunku ikisi ayri seyi soruyor:
            // ucus durumu "kanat acik mi", dikey fark "orada miyim".
            // Kanat kapaliyken bir dama tirmanip ustunden gecmek de
            // varis sayilmamali.
            if (ucus != null && ucus.Simdiki != Player.UcusDizisi.Durum.Yerde)
                return;
            var hk = HedefKonum;
            if (hk != null
                && Mathf.Abs(oyuncu.position.y - hk.Value.y) > VarisDikeyPayi)
                return;

            int oncekiDurak = Simdiki.siradaki;
            Simdiki.DurakTamam();
            YukAkisi(oncekiDurak);

            if (!Simdiki.Bitti) return;

            // --- gorev bitti ---
            Kese.Kazan(Simdiki.akce);
            Bitirilen++;
            GorevBitti?.Invoke(Simdiki);
            YeniGorev();
        }

        /// <summary>
        /// Yükü envantere sokar ve çıkarır.
        ///
        /// İlk durak yükün alındığı yer, son durak teslim yeri. Ödül
        /// sırf akçe olduğunda görev "bir yere yürü" olur; taşınan bir
        /// şey olduğunda <b>bir iş</b> olur — ve envanter ilk kez bir
        /// amaca hizmet eder.
        /// </summary>
        private void YukAkisi(int tamamlananDurak)
        {
            if (envanter == null || Simdiki == null) return;
            var tur = YukTuru(Simdiki.arketip);

            if (tamamlananDurak == 0) envanter.Ekle(tur);
            else if (Simdiki.Bitti) envanter.Cikar(tur);
        }

        /// <summary>Arketipin taşıdığı yük.</summary>
        public static EsyaTuru YukTuru(GorevArketip a) => a switch
        {
            GorevArketip.Teslimat => EsyaTuru.Sebze,
            GorevArketip.Tedarik => EsyaTuru.Odun,
            GorevArketip.Kacakcilik => EsyaTuru.Ekmek,   // "kahve" sandığı
            _ => EsyaTuru.Su,
        };
    }
}
