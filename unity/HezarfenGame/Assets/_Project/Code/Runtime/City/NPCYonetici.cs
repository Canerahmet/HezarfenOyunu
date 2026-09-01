using System.Collections.Generic;
using Hezarfen.Zaman;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Şehrin sakinlerini yaşatır — ve ancak görülecek olanları çizer.</b>
    ///
    /// Faz 6'nın kabul ölçütü *"Galata'da 30 dk kesintisiz serbest
    /// dolaşım"* diyor. İki bin sakini her karede canlandırmak o ölçütü
    /// baştan kaybettirirdi. Bu yüzden iki kademe var:
    ///
    /// <list type="bullet">
    /// <item><b>Sanal</b> — konumu ilerler, gövdesi yoktur. Bütün şehir
    ///       her zaman böyle yaşar.</item>
    /// <item><b>Görünür</b> — oyuncunun yakınındakiler bir gövde alır ve
    ///       animasyonu oynar.</item>
    /// </list>
    ///
    /// Ayrım <b>yaşamak</b> ile <b>görünmek</b> arasında, "var olmak" ile
    /// "yok olmak" arasında değil. Oyuncu bir mahalleye girdiğinde insanlar
    /// orada belirmez; zaten oradadırlar, artık çiziliyorlardır. Tersi,
    /// dünyayı oyuncunun etrafında dönen bir sahneye çevirirdi ve
    /// mahalleden çıkıp dönünce herkes başka bir hayat yaşıyor olurdu.
    ///
    /// ## Sanal sakinler seyrek güncellenir
    ///
    /// İki bin sakini her karede ilerletmek gereksiz: kimse bakmıyor.
    /// Güncelleme <b>dilimlenir</b> — her karede listenin bir bölümü.
    /// Hareket süreye bağlı olduğu için (`NPCAjan.Ilerle(dt)`) seyrek
    /// güncellenen sakin yavaşlamaz, sadece daha büyük adımlarla yürür.
    /// </summary>
    [DisallowMultipleComponent]
    public class NPCYonetici : MonoBehaviour
    {
        [Header("Veri")]
        public SokakGrafi graf;
        public List<NPCMeslek> meslekler = new();
        public ZamanSistemi zaman;

        [Header("Görsel")]
        [Tooltip("NPC gövdesi — tek varyant. Yedek: govdePrefablar boşsa " +
                 "kullanılır.")]
        public GameObject govdePrefab;

        /// <summary>
        /// <b>Sakin gövdeleri</b> — yedi arketip (yetişkin erkek, genç,
        /// yaşlı, kadın, yaşlı kadın, oğlan, kız).
        ///
        /// Bir oyuncu şunu yazdı: *"sokaktaki herkes aynı sakallı adamın
        /// kopyası; çocuklar minik sakallı adamlar, kadın hiç yok."*
        /// Ölçüldü ve doğruydu — <see cref="InsanDNA"/> ölçek, ton ve
        /// tempo çeşitlendiriyordu ama hepsi <b>tek bir gövdeye</b>
        /// uygulanıyordu. Tonlamak ve ölçeklemek çeşitlilik değil
        /// tekrardır: gözü ayıran şey renk değil <b>siluet</b>.
        ///
        /// Hangi gövdenin kime gideceğini <see cref="TurSec"/> seçer ve
        /// seçim gövdenin kendi kimliğinden okunur
        /// (<see cref="SakinGovde"/>), burada yazılı bir tablodan değil.
        /// </summary>
        [Tooltip("Sakin arketipleri. Boşsa govdePrefab kullanılır.")]
        public GameObject[] govdePrefablar = new GameObject[0];

        [Tooltip("Kamerayı/oyuncuyu izler. Boşsa ana kamera aranır.")]
        public Transform oyuncu;

        [Tooltip("Aranma sistemi — replik seçimi buna bakar (Katman 2).")]
        public AranmaSistemi aranma;

        [Header("Kademe")]
        /// <summary>
        /// Şehirdeki toplam sakin. <b>9.000 — 1.200 değil.</b>
        ///
        /// Caner (2026-08-29, oynarken): *"npc ler yok, ve sehir daha dolu
        /// gozuksun."* Ölçüldü ve haklıydı: 1.200 sakin sokak grafının
        /// 9,6 × 7,8 km'lik kapsamına yayılıyordu, yani <b>75 km²'ye 1.200
        /// kişi</b>. Şehrin <b>en kalabalık</b> 90 m'lik dairesinde bile
        /// yalnız <b>21</b> kişi vardı ve ortalama <b>8,9</b>'du; gövde
        /// bütçesi 60 iken hiç dolmuyordu. Yani kalabalık kodu çalışıyordu,
        /// gösterecek kalabalık yoktu.
        ///
        /// Sayı ölçülerek seçildi: yoğunluk sakin sayısıyla doğru orantılı,
        /// 8,9 × (9000/1200) ≈ <b>67</b> — yani 90 m'lik dairede gövde
        /// bütçesini dolduracak kadar, aşırıya kaçmadan.
        ///
        /// Sanal sakin ucuzdur: konum + meslek + saf işlev rutini, ve
        /// güncelleme <c>dilim</c> kare boyunca yayılıyor. Pahalı olan
        /// <see cref="govdeButcesi"/>, o değişmedi.
        /// </summary>
        public int sakinSayisi = VarsayilanSakin;

        /// <summary>
        /// Nüfusun <b>tek sahibi</b>. Sahne kurulumu bu sabiti yazar;
        /// alan varsayılanını değiştirmek zaten kurulmuş bir sahneyi
        /// değiştirmez ve bir kez tam olarak bu oldu.
        /// </summary>
        public const int VarsayilanSakin = 40000;

        // 9.000 -> 40.000. Gerekce OLCULDU, tahmin degil:
        //
        // NPC cizicileri kapatilip kare farki alindi — 640x360'lik karede
        // yalnizca 277 piksel degisti (%0,12). Yani insanlar CIZILIYORDU,
        // ama her biri iki-uc piksel: 58 kisi 120 m yaricapa, yani
        // 45.000 m²'ye yayilmisti. 1.000 m²'ye 1,3 kisi.
        //
        // Bir sokagin kalabalik gorunmesi icin insanin YAKINDA olmasi
        // gerekir; 120 m'deki bir adam 19 piksel boyundadir. Govde
        // butcesi 60 iken o butce uzaktaki minicik insanlara harcaniyordu.
        //
        // 1632 Istanbul'unun nufusu ~700.000; 40.000 hala bunun
        // yirmide biri. Sanal sakin ucuz (konum + meslek + saf islev
        // rutini) ve guncelleme `dilim` kare boyunca yayiliyor; pahali
        // olan govde sayisi degismedi.

        /// <summary>
        /// Bu mesafeden yakındakiler gövde alır (m). <b>90 → 120.</b>
        ///
        /// Sokak 4,6 m ve evler iki katlı; 90 m'de bir sonraki köşe
        /// görünüyor ama oradaki kimse görünmüyordu. 120 m, dolaşırken
        /// insanların <b>uzaktan belirip</b> yaklaşmasını sağlıyor —
        /// birden bire yanınızda belirmelerini değil.
        /// </summary>
        public float gorunurMesafe = VarsayilanGorunurMesafe;

        /// <summary>Görünür mesafenin tek sahibi (m).</summary>
        public const float VarsayilanGorunurMesafe = 120f;

        [Tooltip("Aynı anda en fazla kaç gövde. Bütçe: 30 dk kesintisiz " +
                 "dolaşım bunu aşarsa kaybedilir.")]
        public int govdeButcesi = 60;

        [Tooltip("Her karede sanal sakinlerin kaçta biri güncellensin.")]
        // Dilim 12 -> 40: sakin sayisi dortten fazla katlandi, is ayni
        // sayida kareye sigmaz. Bir sakin artik 40 karede bir guncellenir
        // (~0,67 sn) ve `dilimDt` bunu telafi ediyor, yani yuruyus hizi
        // degismiyor.
        [Range(1, 60)] public int dilim = VarsayilanDilim;

        /// <summary>Dilimin tek sahibi — sahne kurulumu bunu yazar.</summary>
        public const int VarsayilanDilim = 40;

        /// <summary>
        /// Vakit değiştiğinde kare başına kaç sakine yeni hedef verilir.
        ///
        /// 400: 40.000 sakin ~100 karede (1,7 s) yenilenir ve tek karede
        /// 400 Dijkstra, ölçülen kare bütçesinin içinde kalır. Tümünü bir
        /// karede yapmak her ezanda oyunu birkaç saniye kilitliyordu.
        /// </summary>
        [Range(50, 5000)] public int yenilemeButcesi = 400;

        //: Yenileme kuyruğunda kaçıncı sakine gelindi.
        private int _yenilemeDizini = int.MaxValue;

        /// <summary>Şu an gövdesi olan sakin sayısı — tanı ve test okur.</summary>
        public int GorunurSayisi { get; private set; }

        /// <summary>
        /// Havuzun ürettiği toplam gövde nesnesi — <b>sızıntının ölçüsü</b>.
        ///
        /// Testler bunu sahneyi ada göre tarayarak sayıyordu ve o ölçü
        /// iki kez yanlış alarm verdi: saydığı şey bu yöneticinin havuzu
        /// değil, önceki testlerden kalanlardı. Sayacı buraya koymak
        /// ölçüyü olması gereken yere getiriyor — havuz kendi büyümesini
        /// zaten biliyor.
        /// </summary>
        public int UretilenGovde { get; private set; }

        /// <summary>Bütün sakinler (görünür olsun olmasın).</summary>
        public IReadOnlyList<NPCAjan> Sakinler => _sakinler;

        /// <summary>
        /// Bu karede <b>gövdesi çizilen</b> sakinler.
        ///
        /// <see cref="Sakinler"/> kırk bin kişidir ve şehrin tamamıdır;
        /// gövdesi olan altmışıdır. Aradaki fark bir kolaylık değil bir
        /// <b>ücret</b>: <see cref="BarkGosterici"/> her karede kırk bin
        /// kişiyi tarayıp mesafe soruyordu ve ölçülen bedeli 1,0 ms
        /// (bütçenin %6'sı) — ekranda en çok iki yazı için.
        ///
        /// Bir kareyi ölçmek, o karede kimin var olduğunu bilmekle
        /// başlar.
        /// </summary>
        public IReadOnlyList<NPCAjan> GorunurSakinler => _gorunurler;

        private readonly List<NPCAjan> _gorunurler = new();

        private GorevYonetici _gorev;
        private Hezarfen.Zaman.ZamanSistemi _zaman;

        private readonly List<NPCAjan> _sakinler = new();
        /// <summary>
        /// Her arketip KENDI havuzunu tutar.
        ///
        /// Tek havuz kolaydı ve yanlıştı: havuzdan çıkan gövde bir
        /// önceki sahibinin arketipiydi, yani bir kadın ajanı pekâlâ
        /// oğlan gövdesi alabilirdi. Ölçek ve ton yeniden uygulandığı
        /// için bu bir hata gibi görünmez — sokakta 1,58 m'ye
        /// ölçeklenmiş çocuk gövdeleri gezerdi.
        /// </summary>
        private Stack<Transform>[] _havuzlar = new Stack<Transform>[0];
        private readonly Stack<Transform> _havuz = new();
        private int _dilimSayaci;
        private float _sonGuncelleme;
        private VakitHesabi.Vakit _sonVakit = (VakitHesabi.Vakit)(-1);

        private void Start()
        {
            if (oyuncu == null && Camera.main != null)
                oyuncu = Camera.main.transform;
            _gorev = FindAnyObjectByType<GorevYonetici>();
            _zaman = FindAnyObjectByType<Hezarfen.Zaman.ZamanSistemi>();
            Kur();
        }

        /// <summary>Sakinleri dağıtır ve ilk hedeflerini verir.</summary>
        public void Kur()
        {
            // ESKI GOVDELER ONCE HAVUZA DONER.
            //
            // `_sakinler.Clear()` yalniz LISTEYI bosaltiyordu; o sakinlerin
            // sahnede duran gövdeleri hiçbir yere bağlı kalmıyordu.
            // Kayıt yüklemek `Kur`u yeniden çağırır (F9, ya da duraklat
            // menüsünden Yükle), yani her yüklemede o anda görünen ~60
            // şehirli oldukları yerde DONUYOR: adım pozunda çakılı,
            // yürüyen yeni kalabalığın içinden geçiliyor, hiç kaybolmuyor.
            // İkinci yüklemede 60 tane daha ekleniyordu.
            foreach (var a in _sakinler)
            {
                if (a.govde == null) continue;
                GovdeBirak(a.govde);
                a.govde = null;
            }
            _sakinler.Clear();
            _yenilemeDizini = int.MaxValue;
            if (graf == null || meslekler.Count == 0) return;

            foreach (var s in SehirGunu.Sakinler(graf, meslekler, sakinSayisi))
            {
                var a = new NPCAjan
                {
                    meslek = s.meslek,
                    evDugum = s.evDugum,
                    tohum = s.tohum,
                    konum = graf.dugumler[s.evDugum].konum,
                    // Herkes ayni hizda yurumez; %15'lik bir yayilim
                    // kalabaligi "tek vucut" olmaktan cikarir.
                    yurumeHizi = 1.4f * (0.85f + 0.30f
                        * Mathf.Abs(Mathf.Sin(s.tohum * 0.618f))),
                };
                _sakinler.Add(a);
            }
            _sonVakit = (VakitHesabi.Vakit)(-1);
            _sonGuncelleme = Time.time;
        }

        private void Update()
        {
            if (graf == null || _sakinler.Count == 0) return;

            // --- VAKIT DEGISTI MI: herkese yeni hedef ------------------
            //
            // HEDEFLER KARELERE YAYILIR.
            //
            // Onceki hali butun sakinleri AYNI KAREDE yeniliyordu ve her
            // sakin icin iki `EnYakin` taramasi + bir `Yol` (Dijkstra)
            // kosuyordu: 40.000 kisi, 1.543 dugumluk graf. Olculdu degil,
            // gorulduydu — her ezanda oyun birkac saniye tamamen
            // kilitleniyordu (kamera, girdi, ses). 30 dakikalik serbest
            // dolasimda bu yedi kez oluyor.
            //
            // Yenileme artik bir kuyruk: kare basina `yenilemeButcesi`
            // kisi. Sirasi gelmemis sakin eski yolunda yurumeye devam
            // eder — gorunurde hicbir sey olmaz, cunku zaten yurumektedir.
            if (zaman != null && zaman.Vakit != _sonVakit)
            {
                _sonVakit = zaman.Vakit;
                _yenilemeDizini = 0;          // kuyruğu baştan başlat
            }
            if (_yenilemeDizini < _sakinler.Count)
            {
                int son = Mathf.Min(_sakinler.Count,
                                    _yenilemeDizini + Mathf.Max(1, yenilemeButcesi));
                HedefleriYenile(_yenilemeDizini, son);
                _yenilemeDizini = son;
            }

            // --- SANAL: dilimlenmis ilerleme ---------------------------
            float simdi = Time.time;
            float dt = simdi - _sonGuncelleme;
            _sonGuncelleme = simdi;

            // GECEN SUREYI SINIRLA.
            //
            // Iki sebep, ikisi de yasandi:
            //
            //  * `_sonGuncelleme` sifirdan baslarsa ilk karedeki `dt`
            //    oyunun basindan beri gecen suredir. Sehrin tamami
            //    hedefine ISINLANIR ve sonra hic kimildamaz — test
            //    "30 sakinin 2'si yurudu" dedi.
            //  * Yukleme ya da duraksama sonrasi buyuk bir `dt` ayni
            //    seyi yapar. Sinir olmadan her takilma sehri bir anda
            //    ileri sariyor.
            dt = Mathf.Clamp(dt, 0f, 0.5f);

            int bas = _dilimSayaci;
            _dilimSayaci = (_dilimSayaci + 1) % dilim;
            // Dilimlenen sakin `dilim` kare bekledi; gectigi sure o kadar.
            //
            // AMA CARPIM DA SINIRLANIR. `dt` 0,5 s'ye kirpilmis olsa bile
            // `dilim` 40 iken carpim 20 SANIYE eder ve bir ajan tek karede
            // 30 m ilerler: meydanin obur ucuna isinlayan bir adam. Her
            // takilmadan sonra gorulen tam da buydu. Bir sayinin
            // kirpilmasi, ondan turetilen sayinin kirpildigi anlamina
            // gelmiyor.
            //
            // TAVAN NORMAL BIR KAREDEN BUYUK OLMALI. Ilk yazimda 1,0 s
            // koydum ve bu YANLISTI: 20 fps'te dilim 40 iken normal
            // carpim zaten 2,0 s'dir, yani tavan siradan yavas karelerde
            // devreye girip butun kalabaligi yavaslatirdi. Testte
            // goruldu — sakinler gun boyu evlerinden cikamadi ve gunun
            // sonunda "kimse kimildamiyor" cikti. Kusuru duzeltirken
            // saglam olani kirpmak, kusurun kendisinden daha sinsi.
            //
            // 4,0 s: 20 fps'in iki kati pay birakir, 20 saniyelik
            // patolojik sicramayi ise hala kesip 5,6 m'ye indirir.
            float dilimDt = Mathf.Min(dt * dilim, 4.0f);
            for (int i = bas; i < _sakinler.Count; i += dilim)
                _sakinler[i].Ilerle(graf, dilimDt);


            // --- GORUNUR KADEME ----------------------------------------
            KademeYenile();
        }

        /// <summary>
        /// [<paramref name="bas"/>, <paramref name="son"/>) aralığındaki
        /// sakinlere yeni hedef verir. Aralık, kare bütçesidir.
        /// </summary>
        private void HedefleriYenile(int bas, int son)
        {
            int yil = zaman != null ? zaman.yil : 1632;
            int gun = zaman != null ? zaman.yilinGunu : 121;

            for (int i = bas; i < son; i++) AjaniYenile(_sakinler[i], yil, gun);
        }

        /// <summary>Bir sakine yeni hedef ve replik verir.</summary>
        private void AjaniYenile(NPCAjan a, int yil, int gun)
        {
            {
                if (a.meslek == null) return;
                a.vakitDamgasi = (int)_sonVakit;
                // Canli sehir ve simulasyon AYNI islevi cagirir; takvim
                // ve olaylar yalniz orada uygulanir (ADR 0071).
                var tur = Rutin.Hedef(a.meslek, _sonVakit, a.tohum, yil, gun);

                // NE SOYLEYECEGI de vakitle birlikte secilir (Katman 2).
                // Aranma durumu kolluk sisteminden gelir; yoksa temiz.
                a.replik = BarkKorpusu.Sec(
                    a.meslek.tip, _sonVakit, yil, gun,
                    aranma != null && aranma.Seviye > 0f, a.tohum);

                int cikis = graf.EnYakin(a.konum);
                int hedef = tur == SokakGrafi.Tur.Ev
                    ? a.evDugum
                    : graf.EnYakin(a.konum, tur);
                if (hedef < 0) hedef = a.evDugum;

                // Kayik KULLANILMAZ: rutin gundelik hayattir ve kimse
                // her ogle namazi icin Bogaz'i gecmez. Kayik yolculugu
                // bir GOREVDIR, rutin degil.
                a.YolaKoy(graf.Yol(cikis, hedef, kayikVar: false), hedef);
            }
        }

        private void KademeYenile()
        {
            Vector3 merkez = oyuncu != null ? oyuncu.position : Vector3.zero;
            float d2 = gorunurMesafe * gorunurMesafe;

            // KIM GORUNECEK: once KARAR, sonra birakma, en son alma.
            //
            // Iki gecis yetmedi. Birinci gecis yalniz MENZIL DISINDAKILERI
            // birakiyordu; menzilde olup butceye sigmayanlar govdelerini
            // ikinci gecisin ICINDE, hem de alimlardan SONRA birakiyordu.
            // Sonuc: 40 alim yapilirken 25 eski govde hala tutuluyor,
            // havuz bos, yenileri yaratiliyor. Olcum bunu soyledi —
            // butce 40 iken havuz 65 govde uretmisti.
            //
            // Once bir kez daha yanlis olcmustum: testler sahneyi ada
            // gore tariyordu ve saydiklari onceki testlerden kalanlardi.
            // Sayac yoneticinin kendisine tasininca gercek sizinti
            // gorundu. Yanlis olcu, olmayan bir sorunu iki kez gosterip
            // gercegini gizledi.
            // BUTCE EN YAKINLARA GIDER — LISTENIN BASINA DEGIL.
            //
            // Onceki hali menzildeki ilk `govdeButcesi` kisiyi aliyordu ve
            // "ilk" demek LISTE SIRASI demekti: sakinler kurulus sirasina
            // gore diziliydi, yani sehrin her yerinden karisik. Sonucu
            // oyuncunun gordugu sey suydu — yanibasindaki adam aniden yok
            // oluyor, ayni anda sokagin 110 m ilerisinde biri beliriyor.
            // En yakin insanlar en guvenilmez sekilde ciziliyordu; tam
            // tersi olmali.
            //
            // Butun menzili siralamak pahali (menzilde binlerce kisi
            // olabilir), o yuzden `govdeButcesi` boyunda bir MAX-YIGIN
            // tutuluyor: yigindaki en uzagi, gelen daha yakinsa atar.
            // Maliyet O(n log k), k = 60.
            if (_enYakinDizin == null || _enYakinDizin.Length != govdeButcesi)
            {
                _enYakinDizin = new int[govdeButcesi];
                _enYakinD2 = new float[govdeButcesi];
            }
            // Tarih yigin dongusunden ONCE okunur: `Disarida` takvime
            // bakiyor (kahvehane 1633 Eylul'unde kapaniyor) ve bunu
            // dongunun icinde her sakin icin yeniden sormak, ayni
            // cevabi kirk bin kez hesaplamak olurdu.
            int yil0 = zaman != null ? zaman.yil : 1632;
            int gun0 = zaman != null ? zaman.yilinGunu : 121;

            int yigin = 0;
            for (int i = 0; i < _sakinler.Count; i++)
            {
                var a = _sakinler[i];
                a.gorunmeli = false;
                float m2 = (a.konum - merkez).sqrMagnitude;
                if (m2 > d2) continue;

                // ICERIDEKI ADAM CIZILMEZ — VE BU TEK SATIR SEHRIN
                // GUNUNU ILK KEZ GORUNUR KILIYOR.
                //
                // Yigin secimi yalniz MESAFEYE bakiyordu. Simulasyon ise
                // gercek bir gun tutuyor: on meslek cizelgesinden
                // hesaplandi, disarudaki nufus oglen %43,5, yatsida
                // %4,0 — **on kat** fark. Oyuncu bunun sifirini
                // goruyordu, cunku `Rutin.Disarida` calisma zamaninda
                // hicbir yerden cagrilmiyordu.
                //
                // Gece karesinde yatsi vakti sokakta on kisi dikiliyor
                // ve bu, `SehirGunu` + `Rutin` + `Kronoloji` katmaninin
                // tamamini oyuncu acisindan olu kod yapiyordu. Pahali
                // olan yarisi bitmisti; eksik olan tel buydu.
                //
                // Kare butcesi de DUSER: gece cizilen govde azalir.
                if (a.meslek != null
                    && !Rutin.Disarida(a.meslek, _sonVakit, a.tohum,
                                       yil0, gun0))
                    continue;

                if (yigin < govdeButcesi)
                {
                    _enYakinDizin[yigin] = i; _enYakinD2[yigin] = m2;
                    int c = yigin++;
                    while (c > 0)
                    {
                        int ana = (c - 1) >> 1;
                        if (_enYakinD2[ana] >= _enYakinD2[c]) break;
                        Takas(ana, c); c = ana;
                    }
                }
                else if (m2 < _enYakinD2[0])
                {
                    _enYakinDizin[0] = i; _enYakinD2[0] = m2;
                    int c = 0;
                    while (true)
                    {
                        int sol = c * 2 + 1, sag = sol + 1, buyuk = c;
                        if (sol < yigin && _enYakinD2[sol] > _enYakinD2[buyuk]) buyuk = sol;
                        if (sag < yigin && _enYakinD2[sag] > _enYakinD2[buyuk]) buyuk = sag;
                        if (buyuk == c) break;
                        Takas(buyuk, c); c = buyuk;
                    }
                }
            }
            int gorunur = yigin;
            int yil2 = zaman != null ? zaman.yil : 1632;
            int gun2 = zaman != null ? zaman.yilinGunu : 121;
            for (int i = 0; i < yigin; i++)
            {
                var a = _sakinler[_enYakinDizin[i]];
                a.gorunmeli = true;
                // GORUNEN BEKLEMEZ.
                //
                // Yenileme kuyrugu 40.000 kisiyi kare basina 400'er
                // isliyor; sirasi gelmemis biri eski hedefiyle yurumeye
                // devam eder ve bu gorunmez. Ama REPLIK oyle degil:
                // yenilenmemis sakin `replik == null` tasir ve oyuncunun
                // yanindaki adam susar. Olculdu — testte oyuncunun
                // etrafinda konusan kimse kalmadi.
                //
                // Butce burada 60 govdeyle sinirli, yani bedeli sabit.
                if (a.vakitDamgasi != (int)_sonVakit)
                    AjaniYenile(a, yil2, gun2);
            }

            // BIRAK: gorunmeyecek herkes, alim yapilmadan once.
            foreach (var a in _sakinler)
            {
                if (a.govde == null || a.gorunmeli) continue;
                GovdeBirak(a.govde);
                a.govde = null;
            }

            // AL ve yerlestir.
            int cizilen = 0;
            _gorunurler.Clear();
            foreach (var a in _sakinler)
            {
                if (!a.gorunmeli) continue;
                bool yeniGovde = a.govde == null;
                if (yeniGovde) a.govde = GovdeAl(TurSec(InsanDNA.Uret(a.tohum)));
                if (a.govde == null) continue;
                // DNA GOVDE DEGISTIGINDE UYGULANIR.
                //
                // Govde havuzdan geliyor, yani bir onceki sahibinin
                // olcegini ve tonunu tasiyor. Her karede uygulamak
                // israf, hic uygulamamak ise kalabaligi yine tek tip
                // yapar — uygulama ani, govdenin el degistirdigi andir.
                if (yeniGovde) DNAUygula(a);

                // SAPMA: eksenin tam ustunde durmasinlar.
                //
                // Yigilma olculdu: 9.000 sakin 3.070 ayri noktada, bir
                // noktada 33 kisiye kadar. Sokak ekseni bir CIZGI, oysa
                // insanlar bir SERIT boyunca yurur. Sapma tohumdan turer,
                // yani sehir deterministik kalir.
                var yon = a.YonBul(graf);
                var ileri = yon.sqrMagnitude > 1e-4f
                    ? yon.normalized : Vector3.forward;
                var yanal = new Vector3(-ileri.z, 0f, ileri.x);
                // SACILAN GOVDE YENIDEN ZEMINE OTURUR.
                //
                // Sapma gövdeyi YATAYDA kaydiriyor ama yukseklik sokak
                // ekseninden geliyordu; yamacta bu, herkesi zeminden
                // koparir. Olculdu: 60 govdenin ortalamasi 0,63 m HAVADA,
                // 15 tanesi 25 cm'den fazla GOMULU — ayni anda ikisi.
                //
                // Isin yukaridan atiliyor cunku arazi kotu yetmez:
                // kaldirim, kaide ve yol arazinin ustundedir. Kare basina
                // en fazla `govdeButcesi` (60) isin — olculdu, kare
                // suresine etkisi yok.
                var yer = a.konum + yanal * a.Sapma + ileri * a.Boylamsal;

                // ISIN SACAGIN ALTINDAN BASLAR.
                //
                // Once `+4 m`den atiliyordu ve saçak yerden ~3 m'de:
                // ısın sacagin USTUNDEN basliyor, sacaga carpiyor ve
                // gövde damin ustune oturuyordu. Karelerde bir sakin
                // saçak kotunda dikiliyor, biri kaldirima gömülü.
                //
                // 1,2 m: ayagin ustunde ama her saçagin altinda.
                // Yukaridan atmanin sebebi kaldirim ve kaidenin
                // arazinin ustunde olmasiydi ve onlar 0,2-0,5 m — yani
                // 1,2 m o isi de goruyor.
                //
                // Isin 6 m asagi bakar: yamacta dugumden asagi kalan
                // yol bundan uzunsa zaten baska bir sey yanlistir.
                if (Physics.Raycast(yer + Vector3.up * 1.2f, Vector3.down,
                                    out var vurus, 6f, ~0,
                                    QueryTriggerInteraction.Ignore))
                    yer.y = vurus.point.y;
                a.govde.position = yer;
                _gorunurler.Add(a);

                // GOVDE EL DEGISTIRDIGINDE KIM OLDUGU DA DEGISIR.
                if (yeniGovde)
                {
                    var sk = a.govde.GetComponent<Sakin>();
                    if (sk != null)
                    {
                        sk.gorev = _gorev;
                        sk.zaman = _zaman;
                        sk.aranma = aranma;
                    }
                }
                var s2 = a.govde.GetComponent<Sakin>();
                if (s2 != null) s2.ajan = a;
                if (a.hiz > 0.05f && yon.sqrMagnitude > 1e-4f)
                    a.govde.rotation = Quaternion.LookRotation(yon);
                var an = a.govde.GetComponentInChildren<Animator>();
                if (an != null) an.SetFloat("hiz", a.hiz);
                cizilen++;
            }
            GorunurSayisi = cizilen;
        }

        /// <summary>
        /// Gövde havuzu — <b>Instantiate/Destroy yok</b>.
        ///
        /// Oyuncu yürüdükçe sakinler sürekli menzile girip çıkar. Her
        /// girişte yaratıp her çıkışta yok etmek, otuz dakikalık bir
        /// dolaşımda binlerce tahsis ve düzenli çöp toplama duraksaması
        /// demekti — yani tam olarak kabul ölçütünün kaybedildiği yer.
        /// </summary>
        /// <summary>Kullanılabilir arketip sayısı (yedeğiyle birlikte).</summary>
        private int TurSayisi =>
            govdePrefablar != null && govdePrefablar.Length > 0
                ? govdePrefablar.Length : 1;

        private GameObject TurPrefab(int tur)
        {
            if (govdePrefablar == null || govdePrefablar.Length == 0)
                return govdePrefab;
            if (tur < 0 || tur >= govdePrefablar.Length) tur = 0;
            return govdePrefablar[tur] != null ? govdePrefablar[tur]
                                               : govdePrefab;
        }

        private Stack<Transform> Havuz(int tur)
        {
            int n = TurSayisi;
            if (_havuzlar.Length != n)
            {
                var yeni = new Stack<Transform>[n];
                for (int i = 0; i < n; i++)
                    yeni[i] = i < _havuzlar.Length && _havuzlar[i] != null
                        ? _havuzlar[i] : new Stack<Transform>();
                _havuzlar = yeni;
            }
            if (tur < 0 || tur >= n) tur = 0;
            return _havuzlar[tur];
        }

        /// <summary>
        /// Bu insana hangi gövde gider — <b>cinsiyet ve yaş bandına</b> göre.
        ///
        /// Eşleşme prefabın kendi kimliğinden okunur
        /// (<see cref="SakinGovde"/>), burada yazılı bir sıralamadan
        /// değil: dizideki sıra değişirse ya da bir arketip eklenirse
        /// bu işlev kendini düzeltir. Sabit bir indeks tablosu yazsaydım,
        /// prefab dizisini Inspector'da sürüklemek sessizce kadınları
        /// çocuk yapardı.
        ///
        /// Cinsiyet, yaştan <b>ağır basar</b> (4'e 1): bir kadını yaşlı
        /// erkek gövdesiyle göstermek, orta yaşlı kadını yaşlı kadın
        /// gövdesiyle göstermekten çok daha yanlış görünür.
        /// </summary>
        private int TurSec(InsanDNA dna) => ArketipSec(govdePrefablar, dna);

        /// <summary>
        /// Seçimin kendisi — <b>statik</b>, çünkü ölçülebilir olmalı.
        ///
        /// Örnek bir işlev olarak yazsaydım kalabalığın dağılımını ancak
        /// oyunu oynayıp bakarak denetleyebilirdim; bu depoda "bakarak
        /// denetlemek" birkaç kez yanlış sonuç verdi. Statik hâliyle bir
        /// EditMode testi bin tohum üretip *sokakta kaç kadın, kaç çocuk
        /// var* diye SAYABILIYOR.
        /// </summary>
        public static int ArketipSec(GameObject[] govdePrefablar,
                                     InsanDNA dna)
        {
            if (govdePrefablar == null || govdePrefablar.Length == 0) return 0;
            int enIyi = 0;
            float enIyiCeza = float.MaxValue;
            for (int i = 0; i < govdePrefablar.Length; i++)
            {
                var p = govdePrefablar[i];
                if (p == null) continue;
                var sg = p.GetComponent<SakinGovde>();
                // Kimliksiz prefab en sona: hicbir sey bilmiyoruz, ama
                // hicbir govde olmamasindan iyidir.
                if (sg == null)
                {
                    if (enIyiCeza == float.MaxValue) enIyi = i;
                    continue;
                }
                float ceza = (sg.Kadin == dna.kadin ? 0f : 4f)
                           + Mathf.Abs(sg.BandDizini - dna.Band);
                if (ceza < enIyiCeza) { enIyiCeza = ceza; enIyi = i; }
            }
            return enIyi;
        }

        private Transform GovdeAl(int tur)
        {
            var havuz = Havuz(tur);
            if (havuz != null && havuz.Count > 0)
            {
                var t = havuz.Pop();
                t.gameObject.SetActive(true);
                return t;
            }
            var kaynak = TurPrefab(tur);
            if (kaynak == null) return null;
            var go = Instantiate(kaynak, transform);
            UretilenGovde++;
            var etiket = go.GetComponent<SakinGovde>();
            if (etiket != null) etiket.havuzDizini = tur;

            // GOVDEYE DOKUNULABILMELI.
            //
            // `EtkilesimAlgila` fizikten geciyor ve NPC govdesinde
            // hicbir carpistirici yoktu: kirk bin sakinin altmisi her
            // an ekranda yuruyor ve hicbirine dokunulamiyordu.
            // Tetikleyici, cunku gövde oyuncunun yolunu KESMEMELI —
            // kalabalik bir carsida her insan bir duvar olsaydi
            // yurunmezdi.
            // KAPSUL GOVDENIN BOYUNDAN — 1,7 SABITINDEN DEGIL.
            //
            // Yedi arketip gelince sabit yalan soylemeye basladi:
            // 1,24 m'lik oglanin dokunma kapsulu basinin 46 cm ustune
            // cikiyor ve oyuncu cocugun yaninda BOSLUGA dokununca
            // konusma acilıyordu.
            float kapBoy = etiket != null ? etiket.tabanBoy : 1.7f;
            var kap = go.AddComponent<CapsuleCollider>();
            kap.isTrigger = true;
            kap.height = kapBoy;
            kap.radius = kapBoy * 0.206f;
            kap.center = new Vector3(0f, kapBoy * 0.5f, 0f);
            go.AddComponent<Sakin>();

            return go.transform;
        }

        /// <summary>
        /// Ajanın DNA'sını gövdeye işler: ölçek, giysi tonu, adım hızı.
        ///
        /// Kalabalık kapatılmadan önce 40.000 sakin tek gövdeydi —
        /// aynı boy, aynı renk, aynı tempo. Evlerde bu kusur ölçülüp
        /// 26 varyanttan 201'e çıkarıldı; insanda hiç ele alınmamıştı.
        /// Burada çözüm mesh çoğaltmak değil (insan yürür, deri
        /// hesabı pahalıdır): <see cref="InsanDNA"/> tohumdan ölçek,
        /// ton ve tempo türetir. `EvTonu` ile aynı fikir.
        /// </summary>
        private void DNAUygula(NPCAjan a)
        {
            var dna = InsanDNA.Uret(a.tohum);
            // OLCEK GOVDENIN KENDI TABANINA GORE.
            //
            // Once `dna.olcek` dogrudan uygulaniyordu ve o, hedef boyun
            // 1,70'e bolunmus haliydi. Tek govde varken dogruydu; yedi
            // arketiple yanlis oldu: 1,24 m'lik oglan govdesine 1,58/1,70
            // carpani uygulamak 1,15 m'lik bir cocuk degil, YANLIS
            // olcekli bir cocuk yapar — cunku hedef boy zaten bir
            // YETISKIN kadinin boyuydu ve o ajan cocuk govdesi almadi
            // demektir.
            //
            // Dogrusu: gövde arketipi zaten dogru bandda secildi
            // (`TurSec`), o yuzden burada yalniz o arketipin ICINDEKI
            // fark uygulanir. Sinir +-%12: birey farki bu kadardir,
            // daha fazlasi arketip degistirmek demektir ve onu `TurSec`
            // yapar.
            var sg = a.govde.GetComponent<SakinGovde>();
            float olcek = dna.olcek;
            if (sg != null && sg.tabanBoy > 0.1f)
                olcek = Mathf.Clamp(dna.boy / sg.tabanBoy, 0.88f, 1.12f);
            a.govde.localScale = Vector3.one * olcek;
            a.yurumeHizi = dna.hiz;

            if (_tonBlok == null) _tonBlok = new MaterialPropertyBlock();
            foreach (var r in a.govde.GetComponentsInChildren<Renderer>(true))
            {
                // SLOT SLOT GEZILIR — `sharedMaterial` YALNIZ 0. SLOTU
                // OKUR.
                //
                // Once `r.sharedMaterial` yaziyordu ve gövdenin 0.
                // slotu M_Skin. `Boyanir("M_Skin")` false donuyor,
                // dongu `continue` ediyor ve HICBIR NPC boyanmiyordu.
                // Karede bir iyilesme gorulmustu ve gercekti — ama
                // sebebi "ton dogru yere gidiyor" degil, "ton hic
                // gitmiyor" idi: onceki hal her seyi turuncuya
                // cevirdigi icin, hic boyamamak ondan iyi gorunuyordu.
                //
                // Ustelik `SetPropertyBlock(blok)` indekssiz cagrilinca
                // butun slotlara birden yazar; yani dongu calissaydi
                // bile duzeltmeye calistigi hatayi yapardi. Ikisi
                // birlikte, "duzeltildi" denen seyin iki kez yanlis
                // oldugu anlamina geliyordu.
                var malzemeler = r.sharedMaterials;
                for (int i = 0; i < malzemeler.Length; i++)
                {
                    var m = malzemeler[i];
                    if (m == null || !m.HasProperty(TonKimlik)) continue;
                    if (!Boyanir(m.name)) continue;

                    r.GetPropertyBlock(_tonBlok, i);
                    _tonBlok.SetColor(TonKimlik,
                                      Boya(m.GetColor(TonKimlik), dna.ton));
                    r.SetPropertyBlock(_tonBlok, i);
                }
            }

            var anim = a.govde.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                // Adim hizi boyla olceklenir: kisa adam ayni mesafeyi
                // daha cok adimda alir.
                anim.speed = dna.hiz / 1.42f / Mathf.Max(0.6f, dna.olcek);
                // Faz kaymasi: herkes ayni ayakla yurumesin.
                anim.Play(0, 0, dna.faz);
            }
        }

        /// <summary>
        /// Bu malzeme kişiden kişiye renk değiştirir mi.
        ///
        /// ## Neden hepsi değil
        ///
        /// Çarpan önce <b>her</b> renderer'a uygulanıyordu ve sonuç
        /// karede görüldü: ten, sarık, mest ve kaftan aynı tona
        /// kayıyor, figürler tek renk turuncu/pembe lekeler hâline
        /// geliyordu. Kalabalık çeşitlensin diye eklenen şey,
        /// kalabalığı DAHA tekdüze yapmıştı — çünkü bir insanı
        /// okunur kılan şey renk çeşitliliği değil, üstündeki
        /// renkler arasındaki <b>karşıtlık</b>: koyu kaftan, açık
        /// gömlek, beyaz sarık, ten.
        ///
        /// Boyanan yalnız dış giysi. Gömlek ham keten, sarık beyaz,
        /// ten ten, mest deri kalır — hepsi dönemin kendi kuralı,
        /// hepsi ayrıca siluetin okunmasını sağlayan şey.
        /// </summary>
        private static bool Boyanir(string malzemeAdi)
        {
            if (string.IsNullOrEmpty(malzemeAdi)) return false;
            return malzemeAdi.StartsWith("M_Cloth_Entari")
                || malzemeAdi.StartsWith("M_Cloth_Salvar")
                || malzemeAdi.StartsWith("M_Cloth_Kusak");
        }

        /// <summary>
        /// Tabanı DNA tonuna kaydırır — <b>parlaklığını bozmadan</b>.
        ///
        /// Düz çarpma (<c>taban * ton * 1.6f</c>) rengi kaydırırken
        /// parlaklığı da kaydırıyordu ve 1,6 katsayısı çoğu figürü
        /// beyaza doğru patlatıyordu. Burada tondan yalnız <b>renk
        /// yönü</b> alınıyor, parlaklık tabandan geliyor: kaftan koyu
        /// kalıyor, yalnız hangi koyu olduğu değişiyor.
        /// </summary>
        private static Color Boya(Color taban, Color ton)
        {
            float tonIsik = ton.r * 0.299f + ton.g * 0.587f + ton.b * 0.114f;
            if (tonIsik < 1e-3f) return taban;
            var yon = new Color(ton.r / tonIsik, ton.g / tonIsik,
                                ton.b / tonIsik, 1f);
            return new Color(taban.r * yon.r, taban.g * yon.g,
                             taban.b * yon.b, taban.a);
        }

        private MaterialPropertyBlock _tonBlok;
        private static readonly int TonKimlik =
            Shader.PropertyToID("_BaseColor");

        //: En yakin `govdeButcesi` kisiyi tutan max-yigin (kare basina
        //: yeniden ayrilmasin diye alan).
        private int[] _enYakinDizin;
        private float[] _enYakinD2;

        private void Takas(int a, int b)
        {
            (_enYakinDizin[a], _enYakinDizin[b]) = (_enYakinDizin[b], _enYakinDizin[a]);
            (_enYakinD2[a], _enYakinD2[b]) = (_enYakinD2[b], _enYakinD2[a]);
        }

        private void GovdeBirak(Transform t)
        {
            // KIMLIK GOVDEYLE BIRLIKTE BIRAKILIR.
            //
            // Havuza donen govde bir sonraki sahibine kadar KIMSE
            // degil; ustunde eski sahibinin ajani kalirsa oyuncu
            // orada olmayan biriyle konusur.
            var sk = t.GetComponent<Sakin>();
            if (sk != null) sk.ajan = null;

            t.gameObject.SetActive(false);
            var etiket = t.GetComponent<SakinGovde>();
            var havuz = Havuz(etiket != null ? etiket.havuzDizini : 0);
            (havuz ?? _havuz).Push(t);
        }
    }

    /// <summary>Ajanın bakış yönü — yolun bir sonraki adımına doğru.</summary>
    public static class NPCAjanUzanti
    {
        public static Vector3 YonBul(this NPCAjan a, SokakGrafi graf)
        {
            if (graf == null || a.Vardi) return Vector3.zero;
            var hedef = graf.dugumler[a.yol[a.adim]].konum;
            var d = hedef - a.konum;
            d.y = 0f;
            return d;
        }
    }
}
