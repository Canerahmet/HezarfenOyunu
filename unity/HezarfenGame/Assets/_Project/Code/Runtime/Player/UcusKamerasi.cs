using Hezarfen.Flight;
using Unity.Cinemachine;
using UnityEngine;

namespace Hezarfen.Player
{
    /// <summary>
    /// <b>Üçüncü şahıs kamerası: omuz üstü ↔ geniş.</b>
    ///
    /// Plan Bölüm 10: *"Üçüncü şahıs varsayılan; uçuşta omuz-üstü ↔
    /// geniş kamera geçişi."*
    ///
    /// ## Neden iki kamera, tek kamera değil
    ///
    /// Yerde önemli olan **karakterdir**: nereye bastığı, kapıdan geçip
    /// geçmediği, elinin neye değdiği. Havada önemli olan **manzaradır**:
    /// nereye gittiğin, kıyının ne kadar uzakta olduğu, altındaki şehir.
    /// Aynı çerçeve ikisini birden yapamaz — omuz üstünden bakarken
    /// Boğaz'ı göremezsin, geniş çekimde ise karakterin ayağının nereye
    /// bastığı okunmaz.
    ///
    /// Bu yüzden iki ayrı sanal kamera var ve geçişi Cinemachine'in
    /// harmanlaması yapıyor. Tek kamerayı uzaklaştırıp yaklaştırmak
    /// daha basit görünür ama omuz kamerası karakterin **yanında**,
    /// geniş kamera **arkasında ve yukarısında** durur — mesafe değil
    /// çerçeveleme değişir.
    ///
    /// ## Görüş açısı hızla açılır
    ///
    /// Süzülüş hızlandıkça FOV genişler. Bu bir süs değil: sabit FOV'da
    /// hız duygusu tamamen kenar akışına kalır ve yüksekte kenar akışı
    /// yoktur — 300 m'de 12 m/s ile 20 m/s aynı görünür. Açının açılması
    /// hızı **görülebilir** kılar.
    /// </summary>
    [DisallowMultipleComponent]
    public class UcusKamerasi : MonoBehaviour
    {
        [Header("Sanal kameralar")]
        public CinemachineCamera omuz;
        public CinemachineCamera genis;

        [Header("Öncelik")]
        [Tooltip("Etkin kameranın önceliği. Harmanlama süresi " +
                 "CinemachineBrain'in Default Blend ayarındadır.")]
        public int etkinOncelik = 20;
        public int pasifOncelik = 5;

        [Header("Görüş açısı (derece)")]
        [Tooltip("Yerde ve düşük hızda.")]
        public float fovTaban = 55f;

        [Tooltip("Doyum hızındaki görüş açısı.")]
        public float fovAcik = 78f;

        [Tooltip("Bu hızda FOV `fovAcik`e ulaşır (m/s). Kanadın en iyi " +
                 "süzülme hızının belirgin üstünde seçilir ki normal " +
                 "uçuşta açı sürekli oynamasın.")]
        public float fovDoyumHiz = 26f;

        [Tooltip("FOV'un hedefe varma süresi (s).")]
        [Range(0.02f, 1.5f)] public float fovYumusatma = 0.35f;

        [Header("Bağlantı")]
        public GlideController suzulme;

        private float _fov;

        /// <summary>Şu an geniş kamera mı etkin — tanı ve test için.</summary>
        public bool GenisEtkin { get; private set; }

        private void Awake()
        {
            if (suzulme == null) suzulme = GetComponentInParent<GlideController>();
            _fov = fovTaban;
            Uygula(false);
        }

        private void LateUpdate()
        {
            bool ucuyor = suzulme != null && suzulme.isActiveAndEnabled;
            if (ucuyor != GenisEtkin) Uygula(ucuyor);

            float hiz = suzulme != null ? suzulme.AirspeedMps : 0f;
            float t = Mathf.Clamp01(hiz / Mathf.Max(1f, fovDoyumHiz));
            float hedef = ucuyor ? Mathf.Lerp(fovTaban, fovAcik, t) : fovTaban;

            float k = 1f - Mathf.Exp(-Mathf.Max(1e-4f, Time.deltaTime)
                                     / Mathf.Max(0.02f, fovYumusatma));
            _fov = Mathf.Lerp(_fov, hedef, k);

            var aktif = ucuyor ? genis : omuz;
            if (aktif != null)
            {
                var lens = aktif.Lens;
                lens.FieldOfView = _fov;
                aktif.Lens = lens;
            }
        }

        private void Uygula(bool genisEtkin)
        {
            GenisEtkin = genisEtkin;
            if (omuz != null)
                omuz.Priority = genisEtkin ? pasifOncelik : etkinOncelik;
            if (genis != null)
                genis.Priority = genisEtkin ? etkinOncelik : pasifOncelik;
        }
    }
}
