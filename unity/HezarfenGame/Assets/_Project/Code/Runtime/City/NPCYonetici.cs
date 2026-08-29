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
        [Tooltip("NPC gövdesi. Şimdilik karakter prefabı — gövde " +
                 "varyantları ayrı tur (ADR 0068).")]
        public GameObject govdePrefab;

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
        public const int VarsayilanSakin = 9000;

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
        [Range(1, 60)] public int dilim = 12;

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

        private readonly List<NPCAjan> _sakinler = new();
        private readonly Stack<Transform> _havuz = new();
        private int _dilimSayaci;
        private float _sonGuncelleme;
        private VakitHesabi.Vakit _sonVakit = (VakitHesabi.Vakit)(-1);

        private void Start()
        {
            if (oyuncu == null && Camera.main != null)
                oyuncu = Camera.main.transform;
            Kur();
        }

        /// <summary>Sakinleri dağıtır ve ilk hedeflerini verir.</summary>
        public void Kur()
        {
            _sakinler.Clear();
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
            if (zaman != null && zaman.Vakit != _sonVakit)
            {
                _sonVakit = zaman.Vakit;
                HedefleriYenile();
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
            float dilimDt = dt * dilim;
            for (int i = bas; i < _sakinler.Count; i += dilim)
                _sakinler[i].Ilerle(graf, dilimDt);

            // --- GORUNUR KADEME ----------------------------------------
            KademeYenile();
        }

        private void HedefleriYenile()
        {
            int yil = zaman != null ? zaman.yil : 1632;
            int gun = zaman != null ? zaman.yilinGunu : 121;

            foreach (var a in _sakinler)
            {
                if (a.meslek == null) continue;
                // Canli sehir ve simulasyon AYNI islevi cagirir; takvim
                // ve olaylar yalniz orada uygulanir (ADR 0071).
                var tur = Rutin.Hedef(a.meslek, _sonVakit, a.tohum, yil, gun);

                // NE SOYLEYECEGI de vakitle birlikte secilir (Katman 2).
                // Aranma durumu kolluk sisteminden gelir; yoksa temiz.
                a.replik = BarkKorpusu.Sec(
                    a.meslek.tip, _sonVakit, yil, gun,
                    aranma != null && aranma.Seviye > 0f, a.tohum);

                int bas = graf.EnYakin(a.konum);
                int hedef = tur == SokakGrafi.Tur.Ev
                    ? a.evDugum
                    : graf.EnYakin(a.konum, tur);
                if (hedef < 0) hedef = a.evDugum;

                // Kayik KULLANILMAZ: rutin gundelik hayattir ve kimse
                // her ogle namazi icin Bogaz'i gecmez. Kayik yolculugu
                // bir GOREVDIR, rutin degil.
                a.YolaKoy(graf.Yol(bas, hedef, kayikVar: false), hedef);
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
            int gorunur = 0;
            for (int i = 0; i < _sakinler.Count; i++)
            {
                var a = _sakinler[i];
                a.gorunmeli = (a.konum - merkez).sqrMagnitude <= d2
                              && gorunur < govdeButcesi;
                if (a.gorunmeli) gorunur++;
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
            foreach (var a in _sakinler)
            {
                if (!a.gorunmeli) continue;
                if (a.govde == null) a.govde = GovdeAl();
                if (a.govde == null) continue;

                a.govde.position = a.konum;
                if (a.hiz > 0.05f)
                {
                    var yon = a.YonBul(graf);
                    if (yon.sqrMagnitude > 1e-4f)
                        a.govde.rotation = Quaternion.LookRotation(yon);
                }
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
        private Transform GovdeAl()
        {
            if (_havuz.Count > 0)
            {
                var t = _havuz.Pop();
                t.gameObject.SetActive(true);
                return t;
            }
            if (govdePrefab == null) return null;
            var go = Instantiate(govdePrefab, transform);
            UretilenGovde++;
            return go.transform;
        }

        private void GovdeBirak(Transform t)
        {
            t.gameObject.SetActive(false);
            _havuz.Push(t);
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
