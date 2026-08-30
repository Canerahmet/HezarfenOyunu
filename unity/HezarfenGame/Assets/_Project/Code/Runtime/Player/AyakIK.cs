using UnityEngine;

namespace Hezarfen.Player
{
    /// <summary>
    /// <b>Ayakları gerçek zemine oturtur.</b>
    ///
    /// Animasyon klipleri **düz zeminde** çekilmiştir; klip ayağı kendi
    /// düzleminde doğru yere koyar ve o düzlem yokuşta yanlış yerdedir.
    /// İstanbul'da bu istisna değil kural: oyunun geçtiği yedi tepenin
    /// hiçbiri düz değil, sokakların çoğu eğimli.
    ///
    /// Düzeltmenin görünürlüğü küçük ama sürekli: ayak ya zemine gömülür
    /// ya havada durur, ve her ikisi de karakteri "oyuna yapıştırılmış"
    /// gösterir. Bu, `docs/PLAN.md` Bölüm II.A'nın kapı maddelerinden
    /// biridir.
    ///
    /// ## Ölçü elle yazılmaz
    ///
    /// Ayak kemiğinin taban ile arasındaki mesafe (bizim gövdede
    /// <b>0,064 m</b>) modele bağlıdır ve model değişince değişir. Bu
    /// projede aynı hatanın bedeli ölçüldü: elle yazılan bir sayı taban
    /// gövde değişince sessizce yanlışa döner. O yüzden ofset
    /// <see cref="Start"/>'ta <b>avatarın kendisinden</b> okunur.
    ///
    /// ## Neden gövde de iner
    ///
    /// Yalnız ayakları çekmek yetmez: basamakta duran adamın alt ayağı
    /// erişemeyeceği kadar aşağıdaysa bacak gerilir ve diz kilitlenir.
    /// Gövde, iki ayaktan **daha çok inmesi gerekenin** kadarını iner;
    /// böylece iki bacak da bükülü kalır.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public class AyakIK : MonoBehaviour
    {
        [Header("Arama")]
        [Tooltip("Ayağın bulunduğu kottan bu kadar YUKARIDAN ışın atılır. " +
                 "Basamağa çıkarken ayak zeminin içinde başlayabilir; " +
                 "yukarıdan atmak onu yakalar.")]
        public float aramaYukari = 0.45f;

        [Tooltip("Ayağın altında bu kadar derine bakılır. Daha uzağı " +
                 "zemin değil boşluktur ve orada IK uygulanmaz.")]
        public float aramaAsagi = 0.55f;

        [Tooltip("Zemin sayılan katmanlar.")]
        public LayerMask zeminMaskesi = ~0;

        [Header("Yumuşatma")]
        [Tooltip("Ağırlığın ve ofsetin hedefe varma süresi (s). Sıfır = " +
                 "ani; ani değişim yokuş kırıklarında ayağı zıplatır.")]
        [Range(0f, 0.5f)] public float yumusatma = 0.12f;

        [Tooltip("Gövdenin inebileceği en çok mesafe (m). Sınırsız " +
                 "bırakmak, ışın bir çukura düştüğünde karakteri yere " +
                 "gömerdi.")]
        public float govdeEnCokInis = 0.40f;

        [Tooltip("Ayak bu açıdan dik yüzeylere oturtulmaz — duvara " +
                 "basmış gibi görünürdü.")]
        [Range(0f, 80f)] public float enCokEgim = 55f;

        private Animator _animator;
        private Transform _solAyak, _sagAyak;

        /// <summary>Ayak kemiğinin tabandan yüksekliği — ÖLÇÜLÜR.</summary>
        private float _ayakOfset = -1f;

        private float _agirlikSol, _agirlikSag, _govdeInis;

        /// <summary>Son karede ölçülen taban–zemin farkı (test okur).</summary>
        public float SonSolFark { get; private set; }

        /// <summary>Son karede ölçülen taban–zemin farkı (test okur).</summary>
        public float SonSagFark { get; private set; }

        /// <summary>IK uygulanıyor mu (uçarken uygulanmaz).</summary>
        public bool Etkin { get; set; } = true;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            if (_animator == null || !_animator.isHuman) return;
            _solAyak = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _sagAyak = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
            OfsetiOlc();
        }

        /// <summary>
        /// Ayak kemiği ile karakterin kökü arasındaki yüksekliği ölçer.
        ///
        /// Kök zeminde olduğu için bu, kemiğin tabandan yüksekliğidir.
        /// İki ayaktan <b>alçak olanı</b> alınır: duruş pozunda bir ayak
        /// öne çıkmış olabilir ve o ayak zeminden yüksektedir.
        /// </summary>
        private void OfsetiOlc()
        {
            if (_solAyak == null || _sagAyak == null) return;
            // KOK: prefabin en usttteki nesnesi. Bilesen Animator'un
            // kendi nesnesinde duruyor ve o nesne prefab kokunun cocugu
            // olabilir; kendi transform'unu referans almak, model
            // hiyerarsisi degistigi gun sessizce yanlisa doner. Boru
            // hattinin sozlesmesi "pivot tabanda" der ve o pivot koktur.
            float kok = transform.root.position.y;
            _ayakOfset = Mathf.Min(_solAyak.position.y - kok,
                                   _sagAyak.position.y - kok);
            // Sifir ya da negatif bir olcum modelin kokunun ayakta
            // OLMADIGI anlamina gelir; o durumda IK'yi kapatmak,
            // karakteri yere gommekten iyidir.
            if (_ayakOfset <= 0.001f)
            {
                Debug.LogWarning(
                    $"[Hezarfen] {name}: ayak ofseti {_ayakOfset:F3} m — "
                    + "kok ayakta degil, ayak IK kapatildi.");
                Etkin = false;
            }
        }

        private void OnAnimatorIK(int katman)
        {
            if (!Etkin || _animator == null || !_animator.isHuman) return;
            if (_ayakOfset < 0f) return;

            float dt = Time.deltaTime;
            float k = yumusatma <= 0f ? 1f : Mathf.Clamp01(dt / yumusatma);

            bool solVar = ZeminAra(AvatarIKGoal.LeftFoot, out Vector3 solNokta,
                                   out Vector3 solNormal, out float solFark);
            bool sagVar = ZeminAra(AvatarIKGoal.RightFoot, out Vector3 sagNokta,
                                   out Vector3 sagNormal, out float sagFark);
            SonSolFark = solVar ? solFark : float.NaN;
            SonSagFark = sagVar ? sagFark : float.NaN;

            // --- GOVDE: iki ayaktan cok INMESI gerekenin kadari ----------
            //
            // Yalniz asagi. Yukari itmek karakteri havaya kaldirirdi;
            // yukari cikmasi gereken ayak zaten IK ile yukari cekilebilir,
            // asagi uzanamayan bacak ise gerilir.
            float hedefInis = 0f;
            if (solVar) hedefInis = Mathf.Min(hedefInis, solFark);
            if (sagVar) hedefInis = Mathf.Min(hedefInis, sagFark);
            hedefInis = Mathf.Max(hedefInis, -govdeEnCokInis);
            _govdeInis = Mathf.Lerp(_govdeInis, hedefInis, k);
            if (Mathf.Abs(_govdeInis) > 0.0005f)
            {
                _animator.bodyPosition += Vector3.up * _govdeInis;
            }

            _agirlikSol = Mathf.Lerp(_agirlikSol, solVar ? 1f : 0f, k);
            _agirlikSag = Mathf.Lerp(_agirlikSag, sagVar ? 1f : 0f, k);

            Uygula(AvatarIKGoal.LeftFoot, solVar, solNokta, solNormal,
                   _agirlikSol);
            Uygula(AvatarIKGoal.RightFoot, sagVar, sagNokta, sagNormal,
                   _agirlikSag);
        }

        /// <summary>
        /// Bu ayağın altındaki zemini bulur.
        /// </summary>
        /// <param name="fark">
        /// Ayağın tabanının zemine göre farkı: <b>eksi</b> ise taban
        /// zeminin üstünde (ayak indirilmeli), artı ise gömülü.
        /// </param>
        private bool ZeminAra(AvatarIKGoal hedef, out Vector3 nokta,
                              out Vector3 normal, out float fark)
        {
            nokta = default;
            normal = Vector3.up;
            fark = 0f;

            Vector3 ayak = _animator.GetIKPosition(hedef);
            Vector3 bas = ayak + Vector3.up * aramaYukari;
            float uzunluk = aramaYukari + aramaAsagi;

            // KENDI GOVDESINI ZEMIN SAYMA.
            //
            // Isin oyuncunun kendi CharacterController kapsulunden
            // baslıyor ve tek isinla atarsak ilk carptigi sey o olur:
            // ayak "zemini" kendi bacaginda bulur ve karakter her karede
            // biraz daha yukari tirmanir. RaycastAll ile kendi hiyerarsisi
            // elenir.
            RaycastHit[] vurus = Physics.RaycastAll(
                bas, Vector3.down, uzunluk, zeminMaskesi,
                QueryTriggerInteraction.Ignore);
            bool bulundu = false;
            RaycastHit v = default;
            for (int i = 0; i < vurus.Length; i++)
            {
                if (vurus[i].collider.transform.IsChildOf(transform)) continue;
                if (!bulundu || vurus[i].distance < v.distance)
                {
                    v = vurus[i];
                    bulundu = true;
                }
            }
            if (!bulundu) return false;

            // Dik yuzey zemin degildir: duvara basan bir ayak, havada
            // duran ayaktan daha kotu gorunur.
            if (Vector3.Angle(v.normal, Vector3.up) > enCokEgim) return false;

            normal = v.normal;
            nokta = v.point + Vector3.up * _ayakOfset;
            // Tabanin kotu = ayak kemigi - ofset.
            fark = v.point.y - (ayak.y - _ayakOfset);
            return true;
        }

        private void Uygula(AvatarIKGoal hedef, bool var_, Vector3 nokta,
                            Vector3 normal, float agirlik)
        {
            _animator.SetIKPositionWeight(hedef, agirlik);
            _animator.SetIKRotationWeight(hedef, agirlik);
            if (!var_ || agirlik <= 0.001f) return;

            _animator.SetIKPosition(hedef, nokta);
            // Ayak yuzeye YATAR ama yonunu korur: normale gore dondurmek
            // ayagi yana cevirirdi.
            Quaternion mevcut = _animator.GetIKRotation(hedef);
            Vector3 ileri = Vector3.ProjectOnPlane(mevcut * Vector3.forward,
                                                   normal);
            if (ileri.sqrMagnitude > 1e-6f)
            {
                _animator.SetIKRotation(hedef,
                    Quaternion.LookRotation(ileri, normal));
            }
        }
    }
}
