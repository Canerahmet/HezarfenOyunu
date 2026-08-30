using UnityEngine;
using UnityEngine.InputSystem;

namespace Hezarfen.Player
{
    /// <summary>
    /// <b>Yaya gezinti denetleyicisi</b> — Caner mahalleyi kendi gözüyle
    /// dolaşsın diye (Caner, 2026-08-25: *"bir dolaşmak istiyorum, etrafa
    /// bakmak istiyorum"*).
    ///
    /// ## Bu bir OYUN karakteri değil, bir ALET
    ///
    /// Faz 5'in Hezarfen karakteri ayrı bir iştir (animasyonlu, kuşanma ve
    /// atlayış eylemleriyle). Bu sınıf onun yerini almaz: dokuyu **insan
    /// ölçeğinden denetlemek** için bir kamera taşıyıcısıdır. İnceleme
    /// paketi sabit kadrajlar verir; burada kadrajı sen seçersin.
    ///
    /// ## Ölçüler dokunun kendisinden
    ///
    /// Göz **1,70 m** — bütün inceleme paketlerindeki ölçü figürüyle aynı,
    /// yani gördüğün şey render'larda ölçtüğüm şeyle aynı yükseklikten.
    /// Gövde yarıçapı 0,30 m: 4,6 m'lik sokakta (ADR 0016) ve 2,70 m'lik sur
    /// kapısında (Erdoğan 2013 rölövesi) sıkışmadan geçmeli — kapıdan
    /// geçemeyen bir gezgin, kapının dar olduğunu değil aletin kaba olduğunu
    /// gösterirdi.
    ///
    /// Yürüme hızı **1,4 m/s** (insan yürüyüşü); Shift ile 3,6 m/s. Hız
    /// önemli çünkü mesafe duygusu ona bağlı: 2,5 km'lik sur hattını "hızlı"
    /// bir kamerayla dolaşmak şehri küçük gösterir.
    ///
    /// ## Tuşlar
    ///
    /// WASD yürü · fare bak · Shift koş · Space zıpla · Ctrl çömel ·
    /// <b>V kamera kipi</b> (göz / omuz üstü — <see cref="KameraKipi"/>) ·
    /// <b>F uçuş kipi</b> (yerçekimsiz serbest kamera — damları ve kule
    /// tepesini görmek için) · Esc imleci bırak.
    /// </summary>
    [AddComponentMenu("Hezarfen/Walk Controller")]
    [RequireComponent(typeof(CharacterController))]
    public class WalkController : MonoBehaviour
    {
        [Header("Ölçüler (m) — karakter modeliyle aynı")]
        /// <summary>
        /// Göz yüksekliği. <b>1,59 m — 1,70 değil.</b>
        ///
        /// Eski değer 1,70'ti ve yanındaki not "ölçü figürüyle aynı"
        /// diyordu. Nota bakınca doğru görünüyor; sayıya bakınca değil:
        /// 1,70 m o figürün <b>boyu</b>, gözü değil. Yani gezgin,
        /// 1,81 m boyunda bir adamın gözünden bakıyordu ve şehir 1,70 m'lik
        /// bir figüre göre onaylanmıştı.
        ///
        /// Yeni sayı karakter modelinden TÜRETİLDİ, kitaptan değil:
        /// göz, tepe ile çene arasının ortasındadır ve model 1,700 m boy,
        /// 0,221 m baş veriyor → 1,700 − 0,221/2 = <b>1,59 m</b>.
        /// (`art/blend/karakter/catalog.json`; `KarakterTests` bağlar.)
        /// </summary>
        public float eyeHeight = 1.59f;

        /// <summary>
        /// Yürüme hızının <b>tek sahibi</b>.
        ///
        /// Bu sayı iki yerde kullanılıyor: burada ve Animator karışım
        /// ağacının eşiğinde. İkisi ayrışırsa ayaklar yerde kayar — ve bir
        /// kez ayrıştı: <c>AnimatorKur</c> 1,4 ve 3,6'yı ELLE yazıyordu,
        /// yorumu ise "WalkController ile aynı" diyordu. Hız 2,2'ye
        /// çıkınca yorum hâlâ doğru görünüyordu, sayı değil.
        /// <c>AnimatorGrafigiTests</c> yakaladı.
        /// </summary>
        public const float VarsayilanYurume = 2.2f;

        /// <summary>Koşu hızının tek sahibi — bkz. <see cref="VarsayilanYurume"/>.</summary>
        public const float VarsayilanKosma = 6.0f;

        [Header("Hız (m/s)")]
        /// <summary>
        /// Yürüme hızı. <b>2,2 m/s — 1,4 değil</b> (Caner, 2026-08-29
        /// oynarken: *"karakterin hizi biraz yavas gibi onu
        /// hizlandiralim"*).
        ///
        /// Eski sayı 1,4'tü ve doğruydu: ortalama insan yürüyüşü budur.
        /// Ama bu bir <b>oyun</b> ve şehir 10 km: Galata'dan Beyazıt'a
        /// 1,4 m/s ile yürümek 40 dakika sürüyordu. Bu, gerçekçiliği
        /// oynanabilirliğe tercih etmekti ve tercih yanlıştı.
        ///
        /// 2,2 m/s hâlâ bir <b>insan</b> hızı — tempolu yürüyüş — yani
        /// mesafe duygusu ölmüyor, yalnız bekleme kısalıyor. Şehri
        /// küçültmemek için koşu sınırı da açık uçlu bırakılmadı.
        ///
        /// Animator karışım eşikleri bu sayılardan TÜRETİLİR; ikisi
        /// ayrışırsa ayaklar yerde kayar ve <c>AnimatorGrafigiTests</c>
        /// kırılır.
        /// </summary>
        public float walkSpeed = VarsayilanYurume;

        /// <summary>Koşu — insan sprintinin alt sınırı (6,0 m/s).</summary>
        public float runSpeed = VarsayilanKosma;
        public float flySpeed = 12f;           // ucus kipi
        public float jumpSpeed = 3.6f;
        public float gravity = -9.81f;

        [Header("Bakış")]
        public float mouseSensitivity = 0.08f;
        public float pitchLimit = 89f;

        private CharacterController cc;
        private Camera cam;
        private float pitch;
        private KameraKipi kamera;

        /// <summary>
        /// Bakış eğimi (derece). <see cref="KameraKipi"/> okur.
        ///
        /// Açıyı bu sınıf, kameranın YERİNİ o sınıf sahiplenir. Bir sayının
        /// iki sahibi olursa er ya da geç iki değeri olur.
        /// </summary>
        public float Pitch => pitch;
        private float vSpeed;
        private bool flying;
        private bool looking = true;

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            cam = GetComponentInChildren<Camera>();
            kamera = GetComponent<KameraKipi>();
            // KameraKipi varsa kamerayi O yerlestirir; burada da yazmak
            // ayni transforma iki sahip vermek olurdu.
            if (cam != null && kamera == null)
                cam.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
        }

        private void OnEnable() => Capture(true);

        private void OnDisable() => Capture(false);

        /// <summary>
        /// <b>Dünyanın dışına düşeni geri koyar.</b>
        ///
        /// Ölçüldü: oyuncu bir sokak düğümüne — o düğüm bir yapının
        /// içindeydi — yerleştirilince fizik onu dışarı iterken zeminden
        /// geçirdi ve <b>arazinin 160 m altına</b> düştü. Grafın içindeki
        /// 58 bozuk düğüm atıldı, ama bu tür bir sıkışma her zaman
        /// mümkündür: dar bir aralığa girmek, bir kaidenin köşesine
        /// sürtünmek yeter.
        ///
        /// Oyun tarafında doğru davranış düşmeyi <b>engellemek</b> değil,
        /// düşüşten <b>dönmek</b>: arazinin bu kadar altına inen bir
        /// oyuncu zaten kaybolmuştur ve orada bırakmak oyunu bitirir.
        ///
        /// Eşik 3 m: kuyu, mahzen ya da oyulmuş dere yatağı gibi meşru
        /// çukurlar bundan sığdır; 3 m aşağısı artık zeminin içidir.
        /// </summary>
        private void DunyayaGeriKoy()
        {
            if (_arazi == null)
            {
                _arazi = Terrain.activeTerrain;
                if (_arazi == null) return;
            }

            float zemin = _arazi.SampleHeight(transform.position)
                          + _arazi.transform.position.y;
            if (transform.position.y >= zemin - 3f) return;

            // Yuzey ZEMIN KATI olmali — dam degil.
            //
            // Ilk yazimda en yuksek vurus aliniyordu ve bu, dusen oyuncuyu
            // bir binanin CATISINA koyabiliyordu: turda iki durak
            // PF_Mektep_A'nin 5,8 m ustunde olctu. Kaldirim ve kaide
            // arazinin biraz ustundedir; dam cok daha yukarida.
            float yuzey = zemin;
            if (Physics.Raycast(new Vector3(transform.position.x, zemin + 6f,
                                            transform.position.z),
                                Vector3.down, out var v, 12f, ~0,
                                QueryTriggerInteraction.Ignore)
                && v.point.y - zemin <= 2f)
                yuzey = v.point.y;

            cc.enabled = false;
            transform.position = new Vector3(transform.position.x,
                                             yuzey + 0.4f,
                                             transform.position.z);
            cc.enabled = true;
            vSpeed = 0f;
            Dusus++;
            Debug.LogWarning($"[Hezarfen] Oyuncu zeminin altina dustu "
                             + $"({zemin - transform.position.y:F1} m) — "
                             + "yuzeye geri konuldu.");
        }

        /// <summary>Kaç kez dünyadan düşüldü — tanı ve test okur.</summary>
        public int Dusus { get; private set; }

        private Terrain _arazi;

        private void Capture(bool on)
        {
            looking = on;
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return;

            // Esc: imleci birak — Editor'de pencereler arasi gecis icin sart.
            if (kb.escapeKey.wasPressedThisFrame) Capture(!looking);
            if (kb.fKey.wasPressedThisFrame) flying = !flying;

            // --- bakis ---
            if (looking && mouse != null)
            {
                Vector2 d = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0f, d.x, 0f, Space.World);
                pitch = Mathf.Clamp(pitch - d.y, -pitchLimit, pitchLimit);
                if (cam != null && kamera == null)
                    cam.transform.localRotation =
                        Quaternion.Euler(pitch, 0f, 0f);
            }

            // --- yatay girdi ---
            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            var wish = transform.right * x + transform.forward * z;
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            float speed = kb.leftShiftKey.isPressed ? runSpeed : walkSpeed;

            if (flying)
            {
                // UCUS KIPI: yercekimi yok, bakis yonunde serbest hareket.
                // Damlari, kubbeleri ve kule tepesini gormek icin — bunlar
                // yaya seviyesinden hic gorunmuyor.
                var f = cam != null ? cam.transform.forward : transform.forward;
                var move = f * z + transform.right * x;
                if (kb.spaceKey.isPressed) move += Vector3.up;
                if (kb.leftCtrlKey.isPressed) move += Vector3.down;
                cc.Move(move.normalized * flySpeed
                        * (kb.leftShiftKey.isPressed ? 3f : 1f) * Time.deltaTime);
                vSpeed = 0f;
                return;
            }

            // --- yerde: yercekimi + zipla ---
            if (cc.isGrounded)
            {
                // Kucuk bir negatif hiz: tam sifir olursa `isGrounded` yamacta
                // titriyor ve karakter basamaklarda takiliyor.
                vSpeed = -1.5f;
                if (kb.spaceKey.isPressed) vSpeed = jumpSpeed;
            }
            else
            {
                vSpeed += gravity * Time.deltaTime;
            }

            var step = wish * speed;
            step.y = vSpeed;
            cc.Move(step * Time.deltaTime);

            DunyayaGeriKoy();
        }
    }
}
