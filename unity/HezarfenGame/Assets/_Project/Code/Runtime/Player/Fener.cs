using Hezarfen.Zaman;
using UnityEngine;

namespace Hezarfen.Player
{
    /// <summary>
    /// <b>Oyuncunun feneri.</b>
    ///
    /// Bu bir oynanış kolaylığı değil, <b>dönemin kuralı</b>: 17. yüzyıl
    /// İstanbul'unda yatsıdan sonra sokakta fenersiz dolaşmak yasaktı ve
    /// asesin durdurma sebebiydi (RESEARCH §6; <see cref="ZamanSistemi.Gece"/>
    /// zaten "fener zorunluluğu ve ases buna bakar" diye yazılmış).
    /// Yani gecenin karanlığını çözen şey, oyunun kendi kurgusundan
    /// çıkıyor — dışarıdan eklenen bir el feneri değil.
    ///
    /// ## Neden nokta ışık, neden spot değil
    ///
    /// Fener el lambası değil: dört yanı camlı bir kaptır ve <b>her
    /// yöne</b> ışır. Spot koymak modern bir el feneri hissi verirdi ve
    /// oyuncunun baktığı yeri aydınlatırdı — oysa fenerin işi yolu
    /// göstermek değil, <b>taşıyanı görünür kılmak</b> ve iki adım
    /// ötesini seçtirmek. Menzil bilerek kısa.
    ///
    /// ## Titreme
    ///
    /// Sabit bir nokta ışık, ateş değil ampul okur. Titreme iki farklı
    /// hızlı gürültünün çarpımı: alevin kendi çırpınması ve mumun
    /// yavaş nefes alması.
    /// </summary>
    [AddComponentMenu("Hezarfen/Fener")]
    public class Fener : MonoBehaviour
    {
        [Tooltip("Boşsa sahnede aranır.")]
        public ZamanSistemi zaman;

        [Tooltip("Fenerin ışık şiddeti (lümen).")]
        //
        // 42 ILE BASLADI VE POZLAMAYI KACIRDI.
        //
        // Otomatik poz kamerayi en parlak seye gore ayarlar; fener
        // kameranin 30 cm yanindaydi ve sahnedeki her seyden parlakti.
        // Sonuc: yakin cevre turuncu, uzak alan yine simsiyah, ve
        // ayin mavisi hic gorunmedi. Yani fener gecenin CARESI degil,
        // gecenin YENI SEBEBI olmustu.
        //
        // 18 lumen, elde tasinan bir mum fenerinin gercek buyuklugune
        // de daha yakin: iki adim otesini secersiniz, sokagi degil.
        public float lumen = 18f;

        [Tooltip("Işığın eriştiği yarıçap (m).")]
        public float menzil = 7.5f;

        [Tooltip("Gündüz de yansın (hata ayıklama).")]
        public bool hepZaman;

        /// <summary>Fener şu an yanıyor mu — ases ve test okur.</summary>
        public bool Yaniyor { get; private set; }

        private Light _isik;
        private float _tohum;

        /// <summary>
        /// Aranma sistemi — fenerin <b>kanunî</b> tarafı.
        ///
        /// <c>AranmaSistemi.FenerVar</c> alanı vardı ve depoda ona
        /// <b>hiç kimse yazmıyordu</b>: fener yanıyordu ama ases için
        /// yanmıyordu. Yani bu sınıfın yirmi satırlık belgesinde
        /// "bu bir oynanış kolaylığı değil, dönemin kuralı" yazıyordu
        /// ve kural hiçbir yere bağlı değildi — fener yalnızca bir
        /// ışıktı.
        /// </summary>
        public Sehir.AranmaSistemi aranma;

        private void Awake()
        {
            if (zaman == null) zaman = FindAnyObjectByType<ZamanSistemi>();
            if (aranma == null)
                aranma = FindAnyObjectByType<Sehir.AranmaSistemi>();
            _tohum = Random.value * 100f;

            var go = new GameObject("FENER");
            go.transform.SetParent(transform, false);
            // Elde tasinir: govdenin yaninda, bel hizasinda.
            go.transform.localPosition = new Vector3(0.28f, 1.05f, 0.15f);

            _isik = go.AddComponent<Light>();
            _isik.type = LightType.Point;
            _isik.range = menzil;
            // Mum/kandil rengi: turuncuya yakin, ~1900 K.
            _isik.color = new Color(1.00f, 0.72f, 0.42f);
            _isik.shadows = LightShadows.None;   // kare butcesi
            _isik.enabled = false;
        }

        private void Update()
        {
            if (_isik == null) return;
            bool gece = hepZaman || (zaman != null && zaman.Gece);
            Yaniyor = gece;
            if (aranma != null) aranma.FenerVar = Yaniyor;
            _isik.enabled = gece;
            if (!gece) return;

            _isik.range = menzil;
            _isik.intensity = lumen * Titreme(Time.time + _tohum);
        }

        /// <summary>
        /// Alev titremesi 0,80–1,05 arası. Saf ve statik: test bunu
        /// sahne kurmadan sorabilsin diye.
        /// </summary>
        public static float Titreme(float t)
        {
            float hizli = Mathf.PerlinNoise(t * 9.5f, 0.13f);
            float yavas = Mathf.PerlinNoise(t * 1.7f, 4.21f);
            return 0.80f + 0.25f * (hizli * 0.65f + yavas * 0.35f);
        }
    }
}
