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
            var hepsi = (GorevArketip[])Enum.GetValues(typeof(GorevArketip));
            for (int i = 0; i < hepsi.Length; i++)
            {
                var a = hepsi[(_sayac + i) % hepsi.Length];
                if (!GorevUretici.Uygun(a, yil, gun)) continue;
                var g = GorevUretici.Uret(graf, a, oyuncu.position,
                                          tohum + _sayac * 7919, yil, gun);
                if (g == null || g.duraklar.Count == 0) continue;
                Simdiki = g;
                _sayac++;
                GorevBasladi?.Invoke(g);
                return;
            }
            Debug.LogWarning("[Hezarfen] Uretilecek uygun gorev bulunamadi.");
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
