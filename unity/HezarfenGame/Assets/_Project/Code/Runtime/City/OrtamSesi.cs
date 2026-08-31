using UnityEngine;

namespace Hezarfen.City
{
    /// <summary>
    /// <b>Ortam sesi: nerede ve ne zaman olduğunu duyurur.</b>
    ///
    /// Oyun bugüne kadar tamamen sessizdi — sahnede tek bir
    /// <c>AudioSource</c> yoktu. Sessiz bir şehir, ne kadar doğru
    /// modellenirse modellensin, bir maket gibi durur; kulak mesafeyi
    /// ve saati gözden önce söyler.
    ///
    /// ## Dört yatak, iki eksen
    ///
    /// Yataklar sentezle üretiliyor (`tools/audio/gen_ortam.py`) —
    /// doku hattının kararının aynısı: indirilen varlığın lisansını
    /// izlemek, üretmekten pahalı ve ticari yayın her varlığı bağlıyor.
    ///
    /// | yatak | ne zaman duyulur |
    /// |---|---|
    /// | deniz | kıyıya yaklaştıkça — <b>mesafeden</b> |
    /// | rüzgâr | yükseldikçe ve rüzgâr sertleştikçe |
    /// | çarşı | mahalle içinde, <b>gündüz</b> |
    /// | gece | güneş battıktan sonra |
    ///
    /// İki eksen: <b>yer</b> (kıyı uzaklığı, yükseklik) ve <b>saat</b>.
    /// İkisi de zaten ölçülüyor — kıyı uzaklığı arazi öznitelik
    /// katmanında (<see cref="Hezarfen.Gis.AraziOznitelik"/>), saat
    /// şehrin gününde. Yeni bir sayı uydurmuyoruz, var olanları
    /// duyulur kılıyoruz.
    ///
    /// ## Neden tek nesne, neden 3B değil
    ///
    /// Ortam sesi bir kaynaktan gelmez; her yerdedir. Dört yatak
    /// dinleyiciye bağlı 2B kaynaklar olarak çalar ve yalnız
    /// <b>ses seviyeleri</b> değişir. Sokakta yüzlerce 3B kaynak
    /// açmak hem pahalı hem yanlış olurdu: martı bir noktadan gelmez,
    /// kıyıdan gelir.
    /// </summary>
    [AddComponentMenu("Hezarfen/Ortam Sesi")]
    [RequireComponent(typeof(AudioListener))]
    public class OrtamSesi : MonoBehaviour
    {
        [Header("Yataklar")]
        public AudioClip deniz;
        public AudioClip ruzgar;
        public AudioClip carsi;
        public AudioClip gece;

        [Header("Kıyı")]
        [Tooltip("Bu uzaklıkta deniz sesi tepede (m).")]
        public float kiyiYakin = 40f;

        [Tooltip("Bu uzaklıkta deniz sesi susar (m).")]
        public float kiyiUzak = 420f;

        [Header("Yükseklik")]
        [Tooltip("Bu yükseklikten sonra rüzgâr baskın (m).")]
        public float ruzgarYuksek = 120f;

        [Header("Genel")]
        [Range(0f, 1f)] public float anaSes = 0.55f;

        [Tooltip("Ses seviyeleri saniyede bu kadar yaklaşır — ani "
                 + "sıçrama kulakta yapaylık yapar.")]
        public float yumusatma = 0.8f;

        private AudioSource _deniz, _ruzgar, _carsi, _gece;
        private float _hDeniz, _hRuzgar, _hCarsi, _hGece;

        private void Awake()
        {
            _deniz = Kur("Deniz", deniz);
            _ruzgar = Kur("Ruzgar", ruzgar);
            _carsi = Kur("Carsi", carsi);
            _gece = Kur("Gece", gece);
        }

        private AudioSource Kur(string ad, AudioClip klip)
        {
            var go = new GameObject("Ses_" + ad);
            go.transform.SetParent(transform, false);
            var a = go.AddComponent<AudioSource>();
            a.clip = klip;
            a.loop = true;
            a.playOnAwake = false;
            // 2B: ortam sesi bir noktadan gelmez.
            a.spatialBlend = 0f;
            a.volume = 0f;
            if (klip != null) a.Play();
            return a;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Hedefler(out float hd, out float hr, out float hc, out float hg);

            _hDeniz = Yaklas(_hDeniz, hd, dt);
            _hRuzgar = Yaklas(_hRuzgar, hr, dt);
            _hCarsi = Yaklas(_hCarsi, hc, dt);
            _hGece = Yaklas(_hGece, hg, dt);

            if (_deniz != null) _deniz.volume = _hDeniz * anaSes;
            if (_ruzgar != null) _ruzgar.volume = _hRuzgar * anaSes;
            if (_carsi != null) _carsi.volume = _hCarsi * anaSes;
            if (_gece != null) _gece.volume = _hGece * anaSes;
        }

        private float Yaklas(float simdi, float hedef, float dt)
            => Mathf.MoveTowards(simdi, hedef, yumusatma * dt);

        /// <summary>
        /// Dört yatağın hedef seviyesi. Ayrı bir metot çünkü test
        /// edilebilir olması gereken şey bu — ses çalmak değil,
        /// <b>karışım</b>.
        /// </summary>
        public void Hedefler(out float denizSes, out float ruzgarSes,
                             out float carsiSes, out float geceSes)
        {
            float suUzak = SuUzakligi();
            float yukseklik = transform.position.y;
            float gunduz = Gunduz();

            // Deniz: kıyıdan uzaklaştıkça söner. Karekök, mesafeyle
            // ses basıncının nasıl düştüğüne doğrusaldan daha yakın.
            float t = Mathf.InverseLerp(kiyiUzak, kiyiYakin, suUzak);
            denizSes = Mathf.Sqrt(Mathf.Clamp01(t));

            // Rüzgâr: yükseklikle ve rüzgâr hızıyla artar. Yerde bile
            // sert lodosta duyulur.
            float hiz = RuzgarHizi();
            float yukPay = Mathf.Clamp01(yukseklik / ruzgarYuksek);
            float hizPay = Mathf.InverseLerp(3f, 16f, hiz);
            ruzgarSes = Mathf.Clamp01(0.25f + 0.75f * Mathf.Max(yukPay, hizPay));

            // Çarşı: gündüz ve yerde. Yükseldikçe şehir sesi kesilir —
            // uçarken duyulan şey rüzgârdır.
            float yerde = 1f - Mathf.Clamp01(yukseklik / 90f);
            carsiSes = gunduz * yerde * 0.8f;

            // Gece: gündüzün tersi, ve yerde.
            geceSes = (1f - gunduz) * yerde;
        }

        /// <summary>
        /// En yakın suya uzaklık (m). Arazi öznitelik katmanı varsa
        /// oradan okur; yoksa deniz seviyesine olan yükseklik farkını
        /// kaba bir vekil olarak kullanır ve bunu <b>söyler</b>.
        /// </summary>
        private float SuUzakligi()
        {
            var p = transform.position;
            if (_katman != null && _katman.Icinde(p.x, p.z))
                return _katman.SuUzakligi(p.x, p.z);
            // Vekil: deniz seviyesinden yükseklik. Kıyı kotu düşüktür,
            // yani yükseklik uzaklıkla korelasyonludur — ama korelasyon
            // ölçü değildir; katman geldiğinde bu dal ölür.
            return Mathf.Max(0f, p.y) * 3f;
        }

        [Header("Bağlantılar (boşsa aranır)")]
        public Hezarfen.Gis.AraziOznitelik katman;
        private Hezarfen.Gis.AraziOznitelik _katman;

        private void OnEnable() => _katman = katman;

        /// <summary>0 gece, 1 tam gündüz.</summary>
        private float Gunduz()
        {
            var gunes = RenderSettings.sun;
            if (gunes == null) return 1f;
            // Güneşin yere bakma açısı: ufkun altındaysa gece.
            float y = -gunes.transform.forward.y;
            return Mathf.Clamp01(Mathf.InverseLerp(-0.12f, 0.18f, y));
        }

        private float RuzgarHizi()
        {
            var v = Shader.GetGlobalVector(
                Hezarfen.Flight.HavaProfili.RuzgarKimlik);
            return v.w;
        }
    }
}
