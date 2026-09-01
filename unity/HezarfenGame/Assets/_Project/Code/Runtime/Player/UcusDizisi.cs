using System;
using Hezarfen.Flight;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hezarfen.Player
{
    /// <summary>
    /// <b>Kule tepesinde kuşanma → atlayış → süzülüş → iniş.</b>
    ///
    /// Faz 5'in kabul ölçütü bu zincirin **kesintisiz** olmasıdır. Zincir
    /// iki ayrı fizik dünyasını birleştirir ve asıl iş orada:
    ///
    /// - Yerde <see cref="CharacterController"/> sürer — kapsül, adım
    ///   yüksekliği, eğim sınırı. Kütlesi yoktur, ivmesi yoktur, çarpınca
    ///   durur.
    /// - Havada <see cref="Rigidbody"/> sürer — kütle, sürükleme,
    ///   kaldırma. Kanat bir kuvvet uygular, karakter ona uyar.
    ///
    /// İkisi aynı anda açık olamaz: `CharacterController` her karede
    /// konumu kendisi yazar ve Rigidbody'nin uyguladığı her kuvveti
    /// sessizce yutar. Bu, "uçuyorum ama düşmüyorum" diye görünen türden
    /// bir hatadır.
    ///
    /// ## Neden bir durum makinesi, neden bayrak değil
    ///
    /// "Uçuyor mu" diye tek bir bool tutmak yetmez, çünkü aradaki
    /// **geçişlerin süresi var**: kuşanma iki buçuk saniye sürer ve o
    /// sırada ne yürünür ne uçulur. Bayrakla yazınca o iki buçuk saniye
    /// ya yok sayılır (animasyon kesilir) ya da oyuncu donar (girdi
    /// kaybolur). Durum makinesi süreyi bir yer olarak tutar.
    /// </summary>
    [DisallowMultipleComponent]
    public class UcusDizisi : MonoBehaviour
    {
        public enum Durum
        {
            /// <summary>Yerde, kanat sırtta katlı.</summary>
            Yerde,
            /// <summary>Kanat kuşanılıyor — girdi kilitli, klip oynuyor.</summary>
            Kusaniyor,
            /// <summary>Kanat açık, kenarda bekliyor. Atlayabilir.</summary>
            Hazir,
            /// <summary>Havada.</summary>
            Ucuyor,
            /// <summary>Yere değdi, iniş klibi oynuyor.</summary>
            Iniyor,
            /// <summary>Sert çarptı.</summary>
            Cakildi,
        }

        [Header("Bağlantılar")]
        public HezarfenAnimator animasyon;
        public WalkController yurume;
        public CharacterController kapsul;
        public Rigidbody govde;
        public GlideController suzulme;
        public FlightLaunch firlatma;

        [Header("Süreler (s) — klip uzunluklarıyla aynı olmalı")]
        [Tooltip("Kuşanma klibinin uzunluğu. Kısa verilirse oyuncu " +
                 "animasyon bitmeden atlar ve kanat sırtta görünürken uçar.")]
        public float kusanmaSuresi = 2.5f;

        [Tooltip("İniş/yuvarlanma klibinin uzunluğu.")]
        public float inisSuresi = 1.5f;

        [Header("İniş ölçütü")]
        [Tooltip("Yere değerken bu dikey hızın altındaysa iniş, " +
                 "üstündeyse çakılma (m/s, negatif = aşağı).")]
        public float cakilmaHizi = -9f;

        [Tooltip("Zeminden bu kadar yakınsa 'yere değdi' sayılır (m).")]
        public float temasMesafesi = 0.35f;

        [Header("Girdi")]
        /// <summary>
        /// Girdi **yeni Input System**'den okunur, `UnityEngine.Input`'tan
        /// değil.
        ///
        /// İlk yazımda `KeyCode` + `Input.GetKeyDown` kullanmıştım. Proje
        /// Player Settings'te Input System paketine geçmiş durumda ve o
        /// kipte eski API **çalışma anında istisna atar** — derleme
        /// sessizdir, hata ancak oyun koşarken çıkar. PlayMode testi
        /// yazılmasaydı bu, ilk oynayışta ortaya çıkardı.
        /// </summary>
        // E DEGIL.
        //
        // E ayni karede hem "su al" hem "kanadi kusan" demeye
        // basladi: `OyunHud.etkilesimTusu` da E idi. Su kupune uzanan
        // oyuncu 2,5 saniyelik kusanma animasyonunu baslatiyor ve
        // girdisi kilitleniyordu. Iki AYRI fiil, iki ayri tus —
        // etkilesim surekli, kusanma bir kez yapilan bir sey.
        public Key kusanTusu = Key.G;

        public Key atlaTusu = Key.Space;

        /// <summary>Şu anki durum — HUD ve test okur.</summary>
        public Durum Simdiki { get; private set; } = Durum.Yerde;

        /// <summary>Durum değişince tetiklenir (HUD, ses, kodeks).</summary>
        public event Action<Durum> DurumDegisti;

        private float _sayac;

        private void Awake()
        {
            if (animasyon == null) animasyon = GetComponent<HezarfenAnimator>();
            if (yurume == null) yurume = GetComponent<WalkController>();
            if (kapsul == null) kapsul = GetComponent<CharacterController>();
            if (govde == null) govde = GetComponent<Rigidbody>();
            if (suzulme == null) suzulme = GetComponent<GlideController>();
            if (firlatma == null) firlatma = GetComponent<FlightLaunch>();
            YereGec();
        }

        private void Update()
        {
            // DURAKLATILMISKEN KANAT TAKILMAZ.
            //
            // `WalkController` bu dersi zaten yaziyordu ama uçuş
            // dizisi onu paylasmiyordu: menü açıkken G'ye basmak
            // kanadı kuşandırıyor, hatta kuleden atlatıyordu —
            // duraklatılmış bir oyunda oyuncu düşmeye başlıyordu.
            if (Time.timeScale == 0f) return;

            switch (Simdiki)
            {
                case Durum.Yerde:
                    if (Basildi(kusanTusu)) Kusan();
                    break;

                case Durum.Kusaniyor:
                    _sayac -= Time.deltaTime;
                    if (_sayac <= 0f) Gec(Durum.Hazir);
                    break;

                case Durum.Hazir:
                    // ZIPLAMA KAPALI: `Hazir` durumunda Space firlatir.
                    // Ikisi acikken tek basis hem ziplatiyor hem
                    // atlatiyordu ve firlatma ivmesi ziplama hizinin
                    // ustune biniyordu.
                    if (yurume != null) yurume.atlayabilir = false;
                    if (Basildi(atlaTusu)) Atla();

                    // KANAT CIKARILABILIR: KAPI IKI YONLU.
                    //
                    // `Hazir` tek yonlu bir kapandi — girilince
                    // ziplama kapaniyor ve `Yerde`'ye donmenin TEK yolu
                    // ucup inmekti. Carsida merakla G'ye basan oyuncu
                    // artik bir bordure, bir merdivene, `EvMerdiveni`nin
                    // ciktigi hicbir yere cikamiyor; Space'e her basisi
                    // bir firlatma denemesi oluyordu. Kanadi kapatmak
                    // icin kendini bir yerden atmasi gerekiyordu.
                    //
                    // Bir moda giren her oyun o modun kapisini iki
                    // yonlu yapar.
                    else if (Basildi(kusanTusu)) { YereGec(); Gec(Durum.Yerde); }
                    break;

                case Durum.Ucuyor:
                    TemasDenetle();
                    break;

                case Durum.Iniyor:
                case Durum.Cakildi:
                    _sayac -= Time.deltaTime;
                    if (_sayac <= 0f) { YereGec(); Gec(Durum.Yerde); }
                    break;
            }
        }

        /// <summary>Klavye yoksa (test, sunucu) sessizce false döner.</summary>
        private static bool Basildi(Key k)
        {
            var kb = Keyboard.current;
            if (kb != null && kb[k].wasPressedThisFrame) return true;

            // KOL DA BASABILMELI.
            //
            // Kanat kusanmak ve atlamak oyunun iki tek-seferlik fiili
            // ve ikisi de yalniz klavyedeydi. Kolla oynayan biri
            // menuden oyuna girip yuruyebiliyor ama UCAMIYORDU —
            // yani oyunun adini tasiyan seye erisemiyordu.
            var kol = Gamepad.current;
            if (kol == null) return false;
            if (k == Key.G) return kol.rightShoulder.wasPressedThisFrame;
            if (k == Key.Space) return kol.buttonSouth.wasPressedThisFrame;
            return false;
        }

        /// <summary>Kanadı kuşan — girdi bu süre boyunca kilitli.</summary>
        public void Kusan()
        {
            if (Simdiki != Durum.Yerde) return;
            animasyon?.Kusan();
            _sayac = kusanmaSuresi;
            Gec(Durum.Kusaniyor);
        }

        /// <summary>Atla: yürüme fiziği kapanır, uçuş fiziği açılır.</summary>
        public void Atla()
        {
            if (Simdiki != Durum.Hazir) return;
            animasyon?.Atla();
            HavayaGec();
            Gec(Durum.Ucuyor);
        }

        /// <summary>
        /// Uçuşta açılan kapsül — yerde kapalıdır.
        ///
        /// <see cref="kapsul"/> (CharacterController) uçuşa geçerken
        /// kapanır ve o an oyuncunun üzerinde <b>hiçbir çarpıştırıcı
        /// kalmazdı</b>: Rigidbody araziden ve binalardan geçiyordu.
        /// </summary>
        public CapsuleCollider ucusKapsulu;

        //: Bir onceki karenin konumu — temas taramasi bu aralikta yapilir.
        private Vector3 _oncekiKonum;

        /// <summary>
        /// Kalkış payı (s) — bu süre boyunca <b>yakınlık</b> denetimi
        /// susar.
        ///
        /// ## Neden gerekli
        ///
        /// Kalkış anında zemin, tanımı gereği ayağın hemen altındadır.
        /// Yakınlık denetimi 0,55 m'ye bakıyor ve kalkışın ilk karesinde
        /// o mesafede zemin <b>her zaman</b> var. Fırlatma 12,4 m/s
        /// yatay hız veriyor ama bir karede aldığı yol 0,2 m: dam
        /// kenarına yetişemiyor.
        ///
        /// Sonuç ölçüldü: yirmi otomatik uçuşun yirmisi de kalkıştan
        /// bir kare sonra "indi" dedi. Ama bu bir ölçüm kusuru değil —
        /// <b>oyunun kendisi</b> öyle: düz bir damın ortasından
        /// kalkan oyuncu anında iner. Kule şerefesi de düz bir dam.
        ///
        /// Pay yalnız <b>yakınlığı</b> susturur; tünel denetimi açık
        /// kalır. Yani "zemin yakın" göz ardı edilir ama "zeminin
        /// İÇİNDEN geçtik" edilmez — geometriyi delip düşme kusuru
        /// (bu dosyanın en pahalı hatası) geri gelmez.
        ///
        /// 0,40 s, 12,4 m/s'de ~5 m yol demek: her damın kenarını
        /// geçmeye yeter, hiçbir binanın içine girmeye yetmez.
        /// </summary>
        public float kalkisPayi = 0.40f;

        private float _kalkisSayaci;

        /// <summary>
        /// Deniz seviyesi (m) — ADR 0007'nin dünya sözleşmesi.
        ///
        /// Dünya orijini Galata Kulesi tabanı ve <b>y = 0 deniz
        /// seviyesi</b>. Su yüzeyinin çarpıştırıcısı olmadığı için
        /// temas denetiminin sorabileceği tek doğru soru budur.
        /// </summary>
        public const float DenizSeviyesi = 0f;

        /// <summary>
        /// Suya inen oyuncuyu <b>en yakın kıyıya</b> bırakır.
        ///
        /// ## Neden yüzeyde bırakmak yetmedi
        ///
        /// İlk düzeltme oyuncuyu su yüzeyine koyuyordu ve bir oyuncu
        /// raporu ne olduğunu anlattı: bir kare yüzeyde duruyor, sonra
        /// <see cref="WalkController"/> devralıp yerçekimiyle
        /// <b>deniz tabanına indiriyor</b> (−12 m) ve orada yürümeye
        /// başlıyor. Kıyı basamağı 58° eğimli, karakterin tırmanma
        /// sınırı 45°: <b>çıkış yok.</b> Tek çare oyunu yeniden
        /// başlatmak.
        ///
        /// Daha kötüsü, bu kapanı oyunun kendi kuralı zorunlu
        /// kılıyordu: uçuşun sayılması için 800 m isteniyor ve hedefe
        /// doğru kıyı <b>652 m</b>'de bitiyor. Yani oyuncunun doğru
        /// oynaması, kesin olarak denize düşmesi demekti.
        ///
        /// ## Neden yüzme değil
        ///
        /// Yüzme ayrı bir mekanik ve ayrı bir tur. Dönemin cevabı da
        /// zaten hazır: Haliç ve Boğaz kayık kaynıyor (373 kayık
        /// sahnede). Suya düşen adamı kayıkçılar çıkarır — bu bir
        /// kolaylık değil, 1632'de suya düşmenin gerçek sonucu.
        /// </summary>
        private void KiyiyaBirak()
        {
            var p = transform.position;

            // Kiyi HESAPLANMAZ, ARANIR: arazi kotunu sekiz yonde
            // artan yariclarla ornekle, deniz seviyesinin uzerine
            // cikan ilk noktayi al.
            var arazi = Terrain.activeTerrain;
            Vector3 hedef = p;
            bool bulundu = false;
            if (arazi != null)
            {
                for (float r = 40f; r <= 900f && !bulundu; r += 40f)
                    for (int i = 0; i < 12 && !bulundu; i++)
                    {
                        float a = i * 30f * Mathf.Deg2Rad;
                        var q = p + new Vector3(Mathf.Sin(a), 0f,
                                                Mathf.Cos(a)) * r;
                        float kot = arazi.SampleHeight(q)
                                    + arazi.transform.position.y;
                        if (kot <= DenizSeviyesi + 0.5f) continue;
                        hedef = new Vector3(q.x, kot + 0.2f, q.z);
                        bulundu = true;
                    }
            }
            if (!bulundu) { hedef = p; hedef.y = DenizSeviyesi + 0.2f; }

            if (govde != null) govde.position = hedef;
            transform.position = hedef;
            SuyaDustu?.Invoke();
        }

        /// <summary>Suya düşüp kıyıya bırakılınca — HUD okur.</summary>
        public event System.Action SuyaDustu;

        private void TemasDenetle()
        {
            // TEMAS, IKI KARE ARASINDAKI YOLUN TAMAMINDA ARANIR.
            //
            // Onceki hali tek bir Raycast'ti ve yalnizca [y-0,35, y+0,20]
            // bandina, yani 0,55 m'ye bakiyordu. Serbest dususte iki
            // Update arasindaki dusey yol bunu asar asmaz — 60 fps'te
            // ~33 m/s, 30 fps'te ~16,5 m/s; 14 m'lik bir damdan atlamak
            // yetiyor — isin zemini ISKALIYOR ve oyuncu araziyi delip
            // sonsuza kadar dusuyordu. Durum kalici olarak `Ucuyor`da
            // kaldigi ve WalkController kapali oldugu icin onun "dunyaya
            // geri koy" kurtarmasi da hic calismiyordu: oyunu yeniden
            // baslatmaktan baska cikis yoktu.
            //
            // Simdi onceki karenin konumundan bugunkune bir KURE taranir;
            // ne kadar hizli dusulurse dusulsun aradaki zemin kacmaz.
            Vector3 su = transform.position + Vector3.up * 0.2f;
            Vector3 once = _oncekiKonum + Vector3.up * 0.2f;
            _oncekiKonum = transform.position;

            // (a) YAKINLIK: ayagin hemen altinda zemin var mi.
            //
            // Kalkis payi boyunca bu soru sorulmaz: kalkis aninda cevap
            // her zaman "evet"tir ve bu bir inis degil, bir kalkistir.
            _kalkisSayaci -= Time.deltaTime;
            bool yakin = _kalkisSayaci <= 0f
                         && Physics.Raycast(su, Vector3.down,
                                            temasMesafesi + 0.2f,
                                            ~0, QueryTriggerInteraction.Ignore);

            // (b) TUNEL: iki kare arasinda bir seyin ICINDEN gectik mi.
            //
            // (a) tek basina yeterli sanilmisti ve degildi: bandi yalnizca
            // 0,55 m. Serbest dususte iki Update arasindaki dusey yol bunu
            // asar asmaz — 60 fps'te ~33 m/s; 14 m'lik bir damdan atlamak
            // yetiyor — zemin ISKALANIR. Oyuncu araziyi delip sonsuza
            // kadar duser, durum kalici olarak `Ucuyor`da kalir ve
            // WalkController kapali oldugu icin onun kurtarmasi da hic
            // calismaz: oyunu yeniden baslatmaktan baska cikis yoktur.
            //
            // Bu, (a)'nin YERINE degil YANINA konuyor. Kure taramasiyla
            // denendi ve yanlisti: kure zaten zemine degen bir noktadan
            // basladigi icin atlayis karesinde "indik" diyordu — uc ucus
            // testi birden kirmizi yandi. Cizgi ise iki KONUM arasini
            // sorar; havalanma karesinde iki konum aynidir, yani sorusu
            // bostur ve cevabi da.
            bool tunel = (su - once).sqrMagnitude > 1e-6f
                         && Physics.Linecast(once, su, ~0,
                                             QueryTriggerInteraction.Ignore);

            // (c) SU: DENIZIN CARPISTIRICISI YOK.
            //
            // HDRP `WaterSurface` bir carpistirici tasimiyor ve bunu
            // olcen kimse olmadi: (a) ve (b) su yuzeyini GORMEZ, deniz
            // TABANINI gorur (`seabed_depth_m: 12`). Yani Bogaz'a
            // dusen oyuncu su yuzeyinden geciyor, −12 m'ye iniyor,
            // "Indin" yaziyor ve **denizin dibinde yurumeye
            // basliyordu**. Yuzme yok, bogulma yok, geri koyma yok.
            //
            // Deniz seviyesi bu projede bir sozlesme: y = 0 (ADR 0007).
            // Bir carpistirici eklemek yerine sozlesmeyi sormak hem
            // ucuz hem dogru — su, dunyanin her yerinde ayni kotta.
            bool suya = transform.position.y <= DenizSeviyesi;
            if (suya) KiyiyaBirak();

            if (!yakin && !tunel && !suya) return;

            float dikey = govde != null ? govde.linearVelocity.y : 0f;
            bool sert = dikey < cakilmaHizi;
            if (sert) { animasyon?.Cakil(); _sayac = 1.0f; Gec(Durum.Cakildi); }
            else { animasyon?.In(); _sayac = inisSuresi; Gec(Durum.Iniyor); }

            // Fizik HEMEN kapanir; animasyon suresi boyunca karakter
            // yerdedir. Beklemek, inis klibi oynarken govdenin
            // yuvarlanmaya devam etmesi demekti.
            YereGec();
        }

        private void HavayaGec()
        {
            // SIRA ONEMLI: kapsul once kapanmali. Acikken Rigidbody'yi
            // kinematik olmaktan cikarmak, ayni karede iki farkli
            // konum yazicisi demek.
            if (yurume != null) yurume.enabled = false;
            if (kapsul != null) kapsul.enabled = false;
            // Yurume kapsulu kapanirken UCUS kapsulu acilir; arada
            // carpistiricisiz tek bir kare bile kalmaz.
            if (ucusKapsulu != null) ucusKapsulu.enabled = true;
            _oncekiKonum = transform.position;
            if (govde != null)
            {
                govde.isKinematic = false;
                govde.useGravity = true;
            }
            if (suzulme != null) suzulme.enabled = true;
            if (firlatma != null) firlatma.Launch();
            _kalkisSayaci = kalkisPayi;
            GostergeyiAyarla(true);
        }

        /// <summary>
        /// Uçuş göstergesini açar/kapatır.
        ///
        /// Gösterge sahnede sürekli açıktı ve yürüyen oyuncunun
        /// ekranında hava hızı, hücum açısı ve yatış yazıyordu —
        /// hiçbiri o an anlamlı değil. `FlightHud.OnGUI` yalnız
        /// `glider == null` diye bakıyor, `glider.enabled` diye
        /// bakmıyordu.
        /// </summary>
        private void GostergeyiAyarla(bool acik)
        {
            var hud = GetComponent<Flight.FlightHud>();
            if (hud != null) hud.enabled = acik;
        }

        private void YereGec()
        {
            // Ziplama geri acilir: yerde Space yine ziplamadir.
            if (yurume != null) yurume.atlayabilir = true;
            GostergeyiAyarla(false);
            if (suzulme != null) suzulme.enabled = false;
            if (govde != null)
            {
                govde.linearVelocity = Vector3.zero;
                govde.angularVelocity = Vector3.zero;
                govde.isKinematic = true;
                govde.useGravity = false;
            }
            if (ucusKapsulu != null) ucusKapsulu.enabled = false;
            if (kapsul != null) kapsul.enabled = true;
            if (yurume != null) yurume.enabled = true;
        }

        private void Gec(Durum yeni)
        {
            if (Simdiki == yeni) return;
            Simdiki = yeni;
            DurumDegisti?.Invoke(yeni);
        }
    }
}
