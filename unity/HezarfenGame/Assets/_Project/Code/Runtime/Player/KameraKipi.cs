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
            // DURAKLATILMISKEN KAMERA KIPI DEGISMEZ.
            //
            // Ayni ders `WalkController` ve `UcusDizisi` icin yazildi,
            // buraya konmamisti: menu acikken V (ya da R3) hala kamerayi
            // birinci sahistan ucuncu sahisa ceviriyordu.
            if (Time.timeScale == 0f) return;

            var kb = Keyboard.current;
            var kol = Gamepad.current;

            // Kip degistirme: V ya da sag cubuk basmasi (R3).
            if ((kb != null && kb[degistirTusu].wasPressedThisFrame)
                || (kol != null && kol.rightStickButton.wasPressedThisFrame))
                Degistir();

            if (kip != Bakis.UcuncuSahis) return;

            // Kol uzunlugu: fare tekeri ya da D-pad sag/sol.
            //
            // Tekerin karsiligi kolda yok; D-pad yatay ekseni bos ve
            // kaydet/yukle dikeyde. Kola sahip bir oyuncunun kamerayi
            // hic ayarlayamamasi, ayari olmayan bir kamera demekti.
            float teker = 0f;
            var fare = Mouse.current;
            if (fare != null) teker = fare.scroll.ReadValue().y;
            if (kol != null)
            {
                if (kol.dpad.right.wasPressedThisFrame) teker = -1f;
                else if (kol.dpad.left.wasPressedThisFrame) teker = 1f;
            }
            if (Mathf.Abs(teker) > 0.01f)
                mesafe = Mathf.Clamp(mesafe - Mathf.Sign(teker) * tekerAdimi,
                                     enYakin, enUzak);
        }

        /// <summary>
        /// Kamerayı ve gövdeyi <b>geç de olsa</b> bulur.
        ///
        /// Awake'te aramak yetmiyor: kamera bu bileşenden SONRA eklenirse
        /// <c>_kam</c> sonsuza dek boş kalır ve sınıf hiçbir şey yapmaz —
        /// hata da vermez, kamera sadece hiç kıpırdamaz. Testler tam olarak
        /// bunu yakaladı. Sahne kurulumunda sıra bugün doğru; yarın
        /// değişebilir ve o gün kimse bunu hata olarak görmez.
        /// </summary>
        private bool Baglan()
        {
            if (_kam == null) _kam = GetComponentInChildren<Camera>(true);
            if (_yurume == null) _yurume = GetComponent<WalkController>();
            if (govde == null)
            {
                GovdeBul();
                if (govde != null)
                {
                    CiziciTopla();
                    GovdeGoster(kip == Bakis.UcuncuSahis);
                }
            }
            return _kam != null && _yurume != null;
        }

        /// <summary>
        /// Kamerayı yerleştirir.
        ///
        /// <c>LateUpdate</c>: gövde o kare zaten hareket etmiş olur.
        /// <c>Update</c>'te yerleştirmek kamerayı bir kare geriden takip
        /// ettirir ve yürürken titreme olarak görünür — bu oyunda titreme
        /// zaten bir şikâyet konusu, ikincisini eklemeyelim.
        /// </summary>
        /// <summary>Yürürken alan açısı (derece).</summary>
        public const float YerFov = 55f;

        /// <summary>En hızlı süzülüşte alan açısı (derece).</summary>
        public const float UcusFov = 78f;

        /// <summary>Eğrinin doyduğu hava hızı (m/s) — modelin üst ucu.</summary>
        public const float DoygunHiz = 21f;

        private Hezarfen.Flight.GlideController _suzulme;
        private float _fov = YerFov;

        /// <summary>
        /// <b>Hızın görülmesi.</b>
        ///
        /// <c>UcusKamerasi</c> yazıldı, test edildi ve <b>hiçbir
        /// sahneye konmadı</b> — ne oyun sahnesine ne uçuş dilimine.
        /// Ölçüldü: projede çalışma zamanında <c>fieldOfView</c>'a
        /// yazan tek satır yoktu, yani oyunda alan açısı Unity
        /// varsayılanı <b>60°'de sabitti</b>. Model 6,9 ile 21,2 m/s
        /// arasında uçuyor: üç katlık bir hız aralığı, sıfır görsel
        /// karşılık.
        /// </summary>
        private void AlanAcisi()
        {
            if (_suzulme == null)
                _suzulme = GetComponentInParent<Hezarfen.Flight.GlideController>();

            float hiz = _suzulme != null && _suzulme.isActiveAndEnabled
                        ? _suzulme.AirspeedMps : 0f;

            // Kare alinir: dusuk hizda alan acisi oynamasin, cunku
            // yuruyusteki her kucuk hiz degisimi kamerayi soluk
            // aldirir ve bu bir ucus etkisi degil bir kusurdur.
            float t = Mathf.Clamp01(hiz / DoygunHiz);
            float hedef = Mathf.Lerp(YerFov, UcusFov, t * t);

            // Ussel yaklasim: ani bir alan acisi degisimi mide
            // bulandirir; 0,35 s bir nefes kadar.
            _fov = Mathf.Lerp(_fov, hedef,
                              1f - Mathf.Exp(-Time.deltaTime / 0.35f));
            _kam.fieldOfView = _fov;
        }

        private void LateUpdate()
        {
            // UCUSTA DA CALISIR.
            //
            // Onceki hali `!_yurume.enabled` ise geri donuyordu ve
            // `UcusDizisi` atlayista tam da o bileseni kapatiyor: kamera
            // atlayisin oldugu karede DONUYORDU. Kol boyu, omuz kaymasi
            // ve acisi neyse oyle kaliyor, fare hicbir sey yapmiyordu —
            // oyuncu butun ucus boyunca etrafina bakamiyordu.
            if (!Baglan()) return;

            AlanAcisi();

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

            // ENGEL TARAMASI KENDI GOVDESINI ATLAR.
            //
            // Burada tek `Physics.SphereCast` vardi ve calismiyordu:
            // tarama omuz noktasindan basliyor, omuz noktasi oyuncunun
            // KENDI kapsulunun (yaricap 0,30; omuz kaymasi 0,45; kure
            // yaricapi 0,25) icinde kaliyor, ve Unity baslangicta cakisan
            // carpisticilari SphereCast'te yok sayiyor. Yani hicbir engel
            // gorulmuyordu.
            //
            // Olculdu: `SphereCastAll` ayni noktada OYUNCU'yu 0,00 m'de
            // dondurdu, tekil `SphereCast` ise hicbir sey dondurmedi.
            // Sonucu ekranda goruldu — yamacta kol tam boyunda kalip
            // kamerayi terasin ICINE soktu; oyuncu bele kadar gomulmus
            // gibi gorunuyordu, oysa gomulen kameraydi.
            float istenen = mesafe;
            var vuruslar = Physics.SphereCastAll(
                eksen, carpismaYaricapi, geri, mesafe, ~0,
                QueryTriggerInteraction.Ignore);
            float enKisa = float.MaxValue;
            foreach (var v in vuruslar)
            {
                // Kendi govdesi ve gorsel karakteri engel sayilmaz.
                if (v.transform == transform
                    || v.transform.IsChildOf(transform)) continue;
                // Baslangicta cakisan carpistirici 0 mesafe dondurur;
                // o durumda kolu en kisaya cekmek dogrudur.
                if (v.distance < enKisa) enKisa = v.distance;
            }
            if (enKisa < float.MaxValue)
                // Alt sinir enYakin'in YARISI degil KENDISI.
                //
                // Yarisi 0,70 m ediyordu ve olculdu: kaidenin ustunde
                // duvara sirti donuk duran oyuncuda kol tam o degere
                // cokuyor, kamera karakterin kafasinin icine giriyordu
                // (turda uc durakta birden). 1,40 m'de sirt gorunur
                // kalir; daha yakini zaten kadraj olmaktan cikar.
                istenen = Mathf.Max(enYakin,
                                    enKisa - carpismaYaricapi);

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
