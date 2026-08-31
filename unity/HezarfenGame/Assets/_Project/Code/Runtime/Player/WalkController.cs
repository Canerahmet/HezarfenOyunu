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

        /// <summary>
        /// Zıplama açık mı.
        ///
        /// <see cref="UcusDizisi"/> kanat kuşanılınca kapatır: o
        /// durumda Space fırlatma tuşudur ve ikisi birden açıkken tek
        /// basış hem zıplatıp hem atlatıyor, fırlatma ivmesi zıplama
        /// hızının üstüne biniyordu. Tuşun bir sahibi olmalı ve o
        /// sahip duruma göre değişir.
        /// </summary>
        [System.NonSerialized] public bool atlayabilir = true;
        public float gravity = -9.81f;

        [Header("Bakış")]
        public float mouseSensitivity = 0.08f;

        /// <summary>
        /// Sağ çubuğun bakış hızı (derece/s).
        ///
        /// Fare deltası bir <b>yol</b> bildirir (bu karede kaç piksel),
        /// çubuk ise bir <b>hız</b>. İkisini aynı birim sanmak, yüksek
        /// kare hızında kolu uçurur — bu yüzden çubuk kare süresiyle
        /// çarpılır, fare çarpılmaz.
        /// </summary>
        public float KolBakisHizi = 180f;

        /// <summary>Çubuk ölü bölgesi — bırakılmış çubuk kamerayı kaydırmasın.</summary>
        public float KolOluBolge = 0.15f;

        /// <summary>
        /// Etkin fare hassasiyeti — ayarlardan gelir.
        ///
        /// Alan <c>mouseSensitivity</c> Inspector'da duruyordu ve
        /// hiçbir menüye bağlı değildi. Ayarlar bir değer kaydettiyse o
        /// kazanır; kaydetmediyse alanın kendisi. Bir sayının iki
        /// sahibi olmasın diye okuma tek yerden yapılıyor.
        /// </summary>
        private float Hassasiyet =>
            Arayuz.Ayarlar.Hassasiyet > 0f
                ? Arayuz.Ayarlar.Hassasiyet : mouseSensitivity;

        /// <summary>Dikey bakış ters mi — ayarlardan.</summary>
        private bool YTers => Arayuz.Ayarlar.YTers;
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

        // KAPANIRKEN IMLEC BIRAKILMAZ.
        //
        // `UcusDizisi` atlayista bu bileseni kapatiyor ve OnDisable
        // imleci serbest birakiyordu: kuleden atlar atlamaz Windows fare
        // imleci ekranin ortasinda beliriyor ve butun ucus boyunca —
        // oyunun en uzun izlenen sahnesinde — orada duruyordu. Penceredeki
        // bir tiklama odagi oyundan cikariyordu.
        //
        // Imlecin sahibi duraklatma menusudur; yurume bileseni yalnizca
        // ACILIRKEN yakalar.

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

        /// <summary>
        /// Fare bakışını ve imleci açar/kapatır.
        ///
        /// <b>Public, çünkü tek sahibi var:</b> duraklatma menüsü. Eskiden
        /// bu sınıf da Esc'i dinliyordu ve <see cref="UI.OyunHud"/> de —
        /// ikisi aynı tuşa iki farklı anlam yüklüyordu. Sonucu şuydu:
        /// Esc ile duraklat, menüden "Devam et"e tıkla; oyun devam eder,
        /// imleç kilitlenir, <b>ama fare kamerayı bir daha döndürmez</b>.
        /// Çünkü "Devam et" imleci geri kilitlerken bu sınıftaki
        /// <c>looking</c> hâlâ Esc'in bıraktığı yerde, false'ta duruyordu.
        ///
        /// İmlecin de bakışın da tek yazarı olsun diye Esc buradan
        /// kaldırıldı; artık HUD duraklatırken burayı da çağırıyor.
        /// </summary>
        public void Capture(bool on)
        {
            looking = on;
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
        }

        private void Update()
        {
            // Duraklatilmisken girdi OKUNMAZ. Bakis blogu deltaTime
            // kullanmadigi icin timeScale=0'da bile calisirdi ve karakter
            // donmus menunun altinda fareyle donerdi.
            if (Time.timeScale == 0f) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            var kol = Gamepad.current;

            // KLAVYE YOKSA DA OYNANIR.
            //
            // Once `if (kb == null) return;` yaziliyordu: klavye
            // takili degilse — Steam Deck'te oldugu gibi — bu bilesen
            // hicbir sey yapmiyordu. Kolla menuden oyuna girilebiliyor,
            // sonra yurunemiyordu bile.
            if (kb == null && kol == null) return;

            // Esc ARTIK BURADA DEGIL: duraklatmanin tek sahibi OyunHud.
            // (Gerekcesi `Capture`in belgesinde.)

            // Yercekimsiz inceleme kipi bir GELISTIRME araci: yayinlanan
            // oyunda F'ye basan oyuncu sehrin uzerinde suzulmemeli.
            if ((Application.isEditor || Debug.isDebugBuild)
                && kb != null && kb.fKey.wasPressedThisFrame)
                flying = !flying;

            // --- bakis: fare VE sag cubuk ---
            //
            // Cubuk deltasi kare suresiyle carpilir, fare deltasi
            // carpilmaz — cunku fare zaten bir YOL bildirir (bu karede
            // kac piksel), cubuk ise bir HIZ. Ikisini ayni birim
            // sanmak, yuksek kare hizinda kolu ucurur.
            if (looking)
            {
                Vector2 d = Vector2.zero;
                if (mouse != null)
                    d += mouse.delta.ReadValue() * Hassasiyet;
                if (kol != null)
                    d += kol.rightStick.ReadValue()
                         * (KolBakisHizi * Time.deltaTime);
                if (YTers) d.y = -d.y;

                if (d.sqrMagnitude > 0f)
                {
                    transform.Rotate(0f, d.x, 0f, Space.World);
                    pitch = Mathf.Clamp(pitch - d.y, -pitchLimit, pitchLimit);
                    if (cam != null && kamera == null)
                        cam.transform.localRotation =
                            Quaternion.Euler(pitch, 0f, 0f);
                }
            }

            // --- yatay girdi: WASD VE sol cubuk ---
            float x = 0f, z = 0f;
            if (kb != null)
            {
                x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
                z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            }
            // Cubugun kendi itilme miktari AYRI tutulur: klavyeyle
            // toplanmis hali kosma esigi icin kullanilamaz (asagida).
            float kolItme = 0f;
            if (kol != null)
            {
                var sol = kol.leftStick.ReadValue();
                if (sol.sqrMagnitude > KolOluBolge * KolOluBolge)
                { x += sol.x; z += sol.y; kolItme = sol.magnitude; }
            }
            var wish = transform.right * x + transform.forward * z;
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            // KOSMA CUBUGUN NE KADAR ITILDIGINE DE BAKAR.
            //
            // Klavye ikili: yuru ya da kos, arada hicbir sey yok. Kol
            // analog ve bu bedava bir kazanc — cubuk yarim itilirse
            // yuruyus, sonuna kadar itilirse kosu. Shift yine calisir.
            // ...AMA CUBUGUN ITILMESI, CUBUKTAN OKUNUR.
            //
            // Once `itme` BIRLESIK yondan (x, z) hesaplaniyordu ve
            // klavye o yone tam 1,0 katiyor. Sonuc: **kol takiliyken
            // W'ye basmak kosmak oluyordu** — cubuk hic dokunulmamis
            // olsa bile, cunku 1,0 > 0,85. Oyuncunun yurume hakki bir
            // aygitin varligiyla elinden aliniyordu.
            //
            // Analog esik yalnizca analog girdinin sorusudur.
            bool kosuTusu = kb != null && kb.leftShiftKey.isPressed;
            bool kolKosu = kolItme > 0.85f;
            float speed = (kosuTusu || kolKosu) ? runSpeed : walkSpeed;

            if (flying)
            {
                // UCUS KIPI: yercekimi yok, bakis yonunde serbest hareket.
                // Damlari, kubbeleri ve kule tepesini gormek icin — bunlar
                // yaya seviyesinden hic gorunmuyor.
                var f = cam != null ? cam.transform.forward : transform.forward;
                var move = f * z + transform.right * x;
                if (kb != null && kb.spaceKey.isPressed) move += Vector3.up;
                if (kb != null && kb.leftCtrlKey.isPressed) move += Vector3.down;
                cc.Move(move.normalized * flySpeed
                        * (kosuTusu ? 3f : 1f) * Time.deltaTime);
                vSpeed = 0f;
                return;
            }

            // --- yerde: yercekimi + zipla ---
            if (cc.isGrounded)
            {
                // Kucuk bir negatif hiz: tam sifir olursa `isGrounded` yamacta
                // titriyor ve karakter basamaklarda takiliyor.
                vSpeed = -1.5f;
                bool atlaBasildi =
                    (kb != null && kb.spaceKey.wasPressedThisFrame)
                    || (kol != null && kol.buttonSouth.wasPressedThisFrame);
                if (atlayabilir && atlaBasildi) vSpeed = jumpSpeed;
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
