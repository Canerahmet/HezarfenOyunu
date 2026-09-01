using UnityEngine;

namespace Hezarfen.Flight
{
    /// <summary>
    /// <b>Hızın duyulması.</b>
    ///
    /// ## Neden bu bileşen var
    ///
    /// <c>Runtime/Flight/</c> altında <b>hiç ses kaynağı yoktu</b>.
    /// Tek rüzgâr sesi <see cref="Hezarfen.Ses.OrtamSesi"/>'nden
    /// geliyor ve <b>irtifadan</b> besleniyordu — hava hızından değil.
    /// Yani oyuncu 300 m'de sabit süzülürken de, burnunu indirip
    /// 12,4'ten 22 m/s'ye çıkarken de kulağında birebir aynı ses vardı.
    ///
    /// 300 metrede referans nesnesi yok. Kamera eğimi donuk, alan açısı
    /// sabit; hızın oyuncuya ulaşan tek kanalı HUD'daki bir <b>rakam</b>.
    /// Hız bir sayı olarak biliniyor, bir duyu olarak hiç bilinmiyordu.
    ///
    /// ## Neden yeni bir varlık üretilmedi
    ///
    /// Yatak zaten var: <c>SFX_Ortam_Ruzgar.wav</c>, kendi ürettiğimiz
    /// (bkz. <c>tools/audio/gen_ortam.py</c>), döngüye kapalı ve ticari
    /// kullanımı serbest. Bir sesin hızlanması, yeni bir kayıt değil
    /// <b>aynı kaydın perdesi ve şiddeti</b>dir — planörcülükte de öyle:
    /// duyulan şey gövdenin üstünden geçen aynı havadır.
    /// </summary>
    [AddComponentMenu("Hezarfen/Ucus Sesi")]
    [RequireComponent(typeof(GlideController))]
    public class UcusSesi : MonoBehaviour
    {
        [Tooltip("Rüzgâr yatağı — boşsa Resources'tan değil, elle bağlanır.")]
        public AudioClip yatak;

        [Tooltip("Kanadın duyulmaya başladığı hava hızı (m/s).")]
        public float sessizHiz = 7.4f;

        /// <summary>
        /// Sesin doyduğu hava hızı (m/s) — <b>kanattan türer</b>.
        ///
        /// 30 yazıyordu ve bu kanadın <b>fiziksel olarak ulaşamadığı</b>
        /// bir hız: en dik dengeli uçuşta 21,2 m/s. Yani bandın yalnız
        /// 0,20–0,60 arası kullanılıyor ve perde 0,93'ten 1,09'a
        /// çıkıyordu — 1,4 yarım ses, ayırt etme eşiğinin dibinde.
        /// Aynı turda <see cref="Hezarfen.Player.KameraKipi.DoygunHiz"/>
        /// doğru sayıyı (21) yazdı: iki bileşen, aynı büyüklük, iki
        /// farklı sabit.
        /// </summary>
        public float doygunHiz = 21f;

        [Range(0f, 1f)] public float enCokSes = 0.55f;

        private GlideController _kanat;
        private AudioSource _hava, _cirpinma;
        private float _s, _c;

        private void Awake()
        {
            _kanat = GetComponent<GlideController>();
            _hava = Kaynak("HavaSesi", 1.0f);
            _cirpinma = Kaynak("KumasCirpinmasi", 1.35f);
        }

        private AudioSource Kaynak(string ad, float perde)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(transform, false);
            var a = go.AddComponent<AudioSource>();
            a.clip = yatak;
            a.loop = true;
            a.playOnAwake = false;
            a.volume = 0f;
            a.pitch = perde;
            // 2B: ses kanadin uzerinde degil, PILOTUN kulaginda.
            a.spatialBlend = 0f;
            if (yatak != null) a.Play();
            return a;
        }

        private void Update()
        {
            if (_kanat == null) return;

            // KANAT KAPALIYSA HIZ SIFIRDIR — DONMUS BIR ALAN HIZ DEGILDIR.
            //
            // `UcusDizisi.YereGec()` `suzulme.enabled = false` yapinca
            // `GlideController.Step()` hic cagrilmiyor ve
            // `AirspeedMps` **son ucus degerinde doniyor**. Bu sinifi
            // bu turda ben yazdim ve tam bu kusuru koydum: oyuncu
            // kanadi cikariyor, carsiya iniyor, kalabaligin icinde
            // yuruyor ve kulaginda hic dinmeyen bir ruzgar oluyor.
            // Stall'da inmisse kumas cirpinmasi da sonsuza kadar
            // caliyor.
            bool ucuyor = _kanat.isActiveAndEnabled;
            float hiz = ucuyor ? _kanat.AirspeedMps : 0f;
            bool stall = ucuyor && _kanat.IsStalled;

            float t = Mathf.InverseLerp(sessizHiz, doygunHiz, hiz);

            // SES ANI DEGISMEZ.
            //
            // Hava hizi kare kare oynar (rüzgâr örneklemesi, çarpma
            // düzeltmesi); şiddeti doğrudan ona bağlamak sesi
            // titretir. Yaklaşma sabiti kulağın kendi tepkisi kadar:
            // yaklaşık üçte bir saniye.
            _s = Mathf.MoveTowards(_s, t * enCokSes, 3f * Time.deltaTime);
            _hava.volume = _s;

            // Perde de hizla acilir: 0,85 -> 1,25. Bu araligi
            // asmak sesi oyuncak dudugune cevirir.
            _hava.pitch = Mathf.Lerp(0.85f, 1.25f, t);

            // KUMAS CIRPINMASI YALNIZ PERDEDE.
            //
            // Stall bir ses degil bir UYARI: ekrandaki kirmizi yazidan
            // once kulakta olmali, cunku oyuncu o an ekranin ortasina
            // degil ufka bakiyor.
            float c = stall ? enCokSes * 0.7f : 0f;
            _c = Mathf.MoveTowards(_c, c, 6f * Time.deltaTime);
            _cirpinma.volume = _c;
        }
    }
}
