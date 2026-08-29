using UnityEngine;
using UnityEngine.InputSystem;

namespace Hezarfen.Player
{
    /// <summary>Kamera nereden bakıyor.</summary>
    public enum Bakis
    {
        /// <summary>Karakterin gözünden.</summary>
        BirinciSahis = 0,

        /// <summary>Omuz üstünden — GTA / RDR / AC kadrajı.</summary>
        UcuncuSahis = 1,
    }

    /// <summary>
    /// <b>Kamera kipi: göz ya da omuz üstü.</b>
    ///
    /// Caner (2026-08-29, oynarken): *"oyunun kamera acisini degistirmeye
    /// izin versin. karakterin gozlerinden veya gta rdr ac gibi karakterin
    /// ustunden bir kamera olsun."*
    ///
    /// ## Kamerayı TEK bir yer yerleştirir
    ///
    /// Önceden kamerayı <see cref="WalkController"/> koyuyordu
    /// (<c>localPosition = (0, eyeHeight, 0)</c>). İki kip olunca o satır
    /// bir yerleştirme kararı olmaktan çıkıp bir <b>çakışma</b> haline
    /// gelirdi: aynı transformu iki sınıf yazar, hangisinin kazandığı kare
    /// sırasına bağlı olurdu. Bu projede o hatanın adı var — <i>bir sayının
    /// iki sahibi varsa er ya da geç iki değeri olur</i>. Artık bakış
    /// AÇISINI (yaw/pitch) WalkController okur, kameranın YERİNİ yalnız bu
    /// sınıf yazar.
    ///
    /// ## Boom neden çarpışmayı gözetiyor
    ///
    /// Şehrin sokakları 4,6 m (ADR 0016) ve evler iki katlı: 3,2 m'lik sabit
    /// bir kol dar sokakta duvarın <b>içine</b> girer ve oyuncu evin
    /// arkasını görür. Küre taraması kolu engele kadar kısaltıyor.
    ///
    /// ## Birinci şahısta gövde silinmez, GÖLGEYE düşer
    ///
    /// Renderer'ları kapatmak karakterin gölgesini de siler ve oyuncu
    /// güneşin altında gölgesiz yürür — 1632 İstanbul'unda dikkat çeken bir
    /// ayrıntı olurdu. <c>ShadowsOnly</c> gövdeyi gizler, gölgeyi bırakır.
    /// </summary>
    [AddComponentMenu("Hezarfen/Kamera Kipi")]
    [RequireComponent(typeof(WalkController))]
    public class KameraKipi : MonoBehaviour
    {
        [Header("Kip")]
        public Bakis kip = Bakis.UcuncuSahis;

        [Tooltip("Kipi değiştiren tuş.")]
        public Key degistirTusu = Key.V;

        [Header("Üçüncü şahıs (m)")]
        [Tooltip("Boom'un döndüğü nokta — omuz hizası.")]
        public float omuzYuksekligi = 1.45f;

        [Tooltip("Sağ omza kayma; 0 verilirse kamera tam arkada durur.")]
        public float omuzKaymasi = 0.45f;

        public float mesafe = 3.2f;
        public float enYakin = 1.4f;
        public float enUzak = 6.0f;

        [Tooltip("Fare tekerleğinin bir tıkı kaç metre.")]
        public float tekerAdimi = 0.35f;

        [Tooltip("Boom'un engele çarpma yarıçapı.")]
        public float carpismaYaricapi = 0.25f;

        [Tooltip("Boom'un yumuşama hızı (1/s). 0 = anında.")]
        public float yumusatma = 14f;

        [Header("Bağlantılar")]
        [Tooltip("Görünür karakter gövdesi; boşsa çocuklardan aranır.")]
        public Transform govde;

        private WalkController _yurume;
        private Camera _kam;
        private Renderer[] _ciziciler;
        private UnityEngine.Rendering.ShadowCastingMode[] _eskiGolge;
        private float _suAnMesafe;

        /// <summary>Boom'un o anki uzunluğu — test okur.</summary>
        public float SonMesafe => _suAnMesafe;

        private void Awake()
        {
            _yurume = GetComponent<WalkController>();
            _kam = GetComponentInChildren<Camera>(true);
            if (govde == null) GovdeBul();
            CiziciTopla();
            _suAnMesafe = mesafe;
        }

        private void OnEnable() => GovdeGoster(kip == Bakis.UcuncuSahis);

        /// <summary>Görünür gövdeyi kendi başına bulur.</summary>
        private void GovdeBul()
        {
            foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var t = r.transform;
                while (t.parent != null && t.parent != transform) t = t.parent;
                govde = t;
                return;
            }
        }

        private void CiziciTopla()
        {
            if (govde == null)
            {
                _ciziciler = new Renderer[0];
                _eskiGolge = new UnityEngine.Rendering.ShadowCastingMode[0];
                return;
            }
            _ciziciler = govde.GetComponentsInChildren<Renderer>(true);
            _eskiGolge = new UnityEngine.Rendering
                .ShadowCastingMode[_ciziciler.Length];
            for (int i = 0; i < _ciziciler.Length; i++)
                _eskiGolge[i] = _ciziciler[i].shadowCastingMode;
        }

        /// <summary>Kipi değiştirir.</summary>
        public void Degistir()
            => Kip(kip == Bakis.BirinciSahis
                   ? Bakis.UcuncuSahis : Bakis.BirinciSahis);

        /// <summary>Kipi doğrudan kurar.</summary>
        public void Kip(Bakis yeni)
        {
            kip = yeni;
            GovdeGoster(kip == Bakis.UcuncuSahis);
            if (kip == Bakis.UcuncuSahis) _suAnMesafe = mesafe;
        }

        private void GovdeGoster(bool goster)
        {
            if (_ciziciler == null) return;
            for (int i = 0; i < _ciziciler.Length; i++)
            {
                if (_ciziciler[i] == null) continue;
                _ciziciler[i].shadowCastingMode = goster
                    ? _eskiGolge[i]
                    : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[degistirTusu].wasPressedThisFrame)
                Degistir();

            if (kip != Bakis.UcuncuSahis) return;
            var fare = Mouse.current;
            if (fare == null) return;
            float teker = fare.scroll.ReadValue().y;
            if (Mathf.Abs(teker) > 0.01f)
                mesafe = Mathf.Clamp(mesafe - Mathf.Sign(teker) * tekerAdimi,
                                     enYakin, enUzak);
        }

        /// <summary>
        /// Kamerayı yerleştirir.
        ///
        /// <c>LateUpdate</c>: gövde o kare zaten hareket etmiş olur.
        /// <c>Update</c>'te yerleştirmek kamerayı bir kare geriden takip
        /// ettirir ve yürürken titreme olarak görünür — bu oyunda titreme
        /// zaten bir şikâyet konusu, ikincisini eklemeyelim.
        /// </summary>
        private void LateUpdate()
        {
            if (_kam == null || _yurume == null || !_yurume.enabled) return;

            float pitch = _yurume.Pitch;

            if (kip == Bakis.BirinciSahis)
            {
                _kam.transform.localPosition =
                    new Vector3(0f, _yurume.eyeHeight, 0f);
                _kam.transform.localRotation =
                    Quaternion.Euler(pitch, 0f, 0f);
                _suAnMesafe = 0f;
                return;
            }

            var eksen = transform.position
                        + Vector3.up * omuzYuksekligi
                        + transform.right * omuzKaymasi;
            var yon = Quaternion.Euler(pitch, transform.eulerAngles.y, 0f);
            var geri = yon * Vector3.back;

            float istenen = mesafe;
            if (Physics.SphereCast(eksen, carpismaYaricapi, geri,
                                   out var vurus, mesafe, ~0,
                                   QueryTriggerInteraction.Ignore))
                istenen = Mathf.Max(enYakin * 0.5f,
                                    vurus.distance - carpismaYaricapi);

            // Engele DOGRU ani, engelden UZAKLASIRKEN yumusak: duvara
            // girerken gecikmek kamerayi tasin icine sokar, cikarken
            // aninda firlatmak ise goz yorar.
            _suAnMesafe = istenen < _suAnMesafe
                ? istenen
                : (yumusatma <= 0f
                   ? istenen
                   : Mathf.Lerp(_suAnMesafe, istenen,
                                1f - Mathf.Exp(-yumusatma * Time.deltaTime)));

            _kam.transform.position = eksen + geri * _suAnMesafe;
            _kam.transform.rotation = yon;
        }
    }
}
