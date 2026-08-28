using Hezarfen.Flight;
using UnityEngine;

namespace Hezarfen.Player
{
    /// <summary>
    /// <b>Animator parametrelerini gerçekten olan şeyden sürer.</b>
    ///
    /// Bu bileşen animasyon seçmez; **ölçtüğünü bildirir.** Hız
    /// `CharacterController`ın gerçek yer değiştirmesinden, pitch/roll
    /// `GlideController`ın hesapladığı hücum ve yatış açılarından gelir.
    ///
    /// Ayrımın önemi şu: bir animasyon durumunu girdiden (tuşa basıldı
    /// mı) sürmek kolaydır ve çoğu zaman <b>yalan söyler</b> — oyuncu
    /// ileri tuşuna basar ama karakter duvara dayanmıştır, yürüme
    /// animasyonu yerinde döner ve ayaklar kayar. Bu projede ayak
    /// kayması ölçülen bir şeydir (ADR 0067); onu Blender'da sıfırlayıp
    /// Unity'de girdiden sürerek geri getirmek olmaz.
    ///
    /// ## Uçuşta pitch ve roll neden normalize
    ///
    /// Blend ağacının düğümleri −1 / +1'de duruyor. Ham açıyı (derece)
    /// vermek ağacı ilk 26 derecede doyururdu. Normalizasyon eşikleri
    /// <b>kanadın kendi sınırlarından</b> gelir: hücum açısı stall
    /// sınırına, yatış ise oyuncunun dönüş yarıçapına bağlı.
    /// </summary>
    [DisallowMultipleComponent]
    public class HezarfenAnimator : MonoBehaviour
    {
        [Header("Bağlantılar")]
        [Tooltip("Boşsa aynı nesnede veya çocuklarda aranır.")]
        public Animator animator;
        public CharacterController karakterKontrol;
        public GlideController suzulme;

        [Header("Uçuş karışımı — açı eşikleri (derece)")]
        [Tooltip("Bu hücum açısında pitch girdisi ±1 olur. Kanadın stall " +
                 "açısına yakın seçilir: ötesinde zaten uçmuyoruz.")]
        public float pitchDoyum = 14f;

        [Tooltip("Bu yatış açısında roll girdisi ±1 olur.")]
        public float rollDoyum = 45f;

        [Header("Yumuşatma")]
        [Tooltip("Parametrelerin hedefe varma süresi (s). Sıfır = ani; " +
                 "ani değişim blend ağacında sıçrama gibi görünür.")]
        [Range(0f, 0.5f)] public float yumusatma = 0.12f;

        // Parametre kimlikleri: her karede string aramak israftir.
        private static readonly int IdHiz = Animator.StringToHash("hiz");
        private static readonly int IdTirman = Animator.StringToHash("tirmaniyor");
        private static readonly int IdUcuyor = Animator.StringToHash("ucuyor");
        private static readonly int IdPitch = Animator.StringToHash("pitch");
        private static readonly int IdRoll = Animator.StringToHash("roll");

        private float _hiz, _pitch, _roll;
        private Vector3 _oncekiPoz;

        /// <summary>Merdiven tırmanışı — dışarıdan (tetikleyici alan) set edilir.</summary>
        public bool Tirmaniyor { get; set; }

        /// <summary>Şu an ölçülen yatay hız (m/s). Tanı için.</summary>
        public float Hiz => _hiz;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (karakterKontrol == null)
                karakterKontrol = GetComponent<CharacterController>();
            if (suzulme == null) suzulme = GetComponentInParent<GlideController>();
            _oncekiPoz = transform.position;
        }

        public void Kusan() => animator?.SetTrigger("kusan");
        public void Atla() => animator?.SetTrigger("atla");
        public void In() => animator?.SetTrigger("in");
        public void Cakil() => animator?.SetTrigger("cakil");

        private void Update()
        {
            if (animator == null) return;
            float dt = Mathf.Max(1e-4f, Time.deltaTime);
            float k = yumusatma <= 0f ? 1f
                    : 1f - Mathf.Exp(-dt / yumusatma);

            // --- HIZ: gercek yer degistirmeden ---------------------------
            //
            // `CharacterController.velocity` da var ama duvara dayanan
            // bir karakterde girdiyi degil SONUCU vermesi gerekiyordu ve
            // bazi surumlerde istenen hizi doner. Konum farki tartisma
            // goturmez.
            Vector3 d = transform.position - _oncekiPoz;
            _oncekiPoz = transform.position;
            float yatay = new Vector2(d.x, d.z).magnitude / dt;
            _hiz = Mathf.Lerp(_hiz, yatay, k);
            animator.SetFloat(IdHiz, _hiz);
            animator.SetBool(IdTirman, Tirmaniyor);

            // --- UCUS ------------------------------------------------------
            bool ucuyor = suzulme != null && suzulme.isActiveAndEnabled;
            animator.SetBool(IdUcuyor, ucuyor);
            if (!ucuyor) return;

            float p = Mathf.Clamp(suzulme.AngleOfAttackDeg
                                  / Mathf.Max(1f, pitchDoyum), -1f, 1f);
            float r = Mathf.Clamp(suzulme.BankAngleDeg
                                  / Mathf.Max(1f, rollDoyum), -1f, 1f);
            _pitch = Mathf.Lerp(_pitch, p, k);
            _roll = Mathf.Lerp(_roll, r, k);
            animator.SetFloat(IdPitch, _pitch);
            animator.SetFloat(IdRoll, _roll);
        }
    }
}
