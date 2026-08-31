using System.Collections.Generic;
using Hezarfen.Zaman;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>İskeleden karşıya geçiş.</b>
    ///
    /// ## Neden bu bileşen var
    ///
    /// Oyuncu şehrin **%19,7'sinde hapisti**. Yaya grafı dört ayrık
    /// bileşene bölünüyor — Sûriçi 804 düğüm, Üsküdar 329, Galata 304,
    /// Eyüp 104 — ve doğum noktası Galata'nın 304'ünde. Yani Üsküdar'ın
    /// 2.328 evi, Sûriçi'nin tamamı ve Eyüp yürüyerek **erişilemez**.
    ///
    /// Bu bir kusur değil, tasarımın kendisi: 1632'de Haliç'te köprü
    /// yoktur ve karşıya kayıkla geçilir. `Faz6KapiTests` bunu kriter
    /// olarak yazmış bile — Galata→Üsküdar yürüyüşünün **boş dönmesi**
    /// zorunlu. Eksik olan tasarım değil, tasarımın karşılığı olan
    /// mekanikti.
    ///
    /// Ve her parçası zaten yazılmıştı: graf kayık kenarlarını taşıyor
    /// (15 kenar), `SokakGrafi.Yol(kayikVar: true)` karşıya yol
    /// buluyor, `Ekonomi.Ucret` mesafeden ücret hesaplıyor,
    /// <see cref="Kese"/> ödemeyi tutuyor. Dördü de depoda
    /// <b>yalnız testlerde</b> çağrılıyordu. Bu, bu projede dördüncü kez
    /// çıkan aynı desen: sistem var, oyuna bağlı değil.
    ///
    /// ## Neden menü yok
    ///
    /// Nereye gidileceği sorulmuyor. Görev varsa hedefe en yakın karşı
    /// iskeleye, yoksa en yakın **başka bileşendeki** iskeleye geçilir;
    /// E'ye tekrar basmak sıradakine döner. Bir kayıkçıya "nereye"
    /// diye sorulmaz, "karşıya" denir — ve oyuncunun zaten bir hedefi
    /// varsa onu iki kez söyletmenin anlamı yok.
    /// </summary>
    [AddComponentMenu("Hezarfen/Perme")]
    public class Perme : MonoBehaviour, IEtkilesim
    {
        [Header("Bağlantılar (boşsa sahnede aranır)")]
        public SokakGrafi graf;
        public GorevYonetici gorev;
        public ZamanSistemi zaman;

        [Tooltip("Gece kayık çalışmaz (RESEARCH §6).")]
        public bool geceKapali = true;

        /// <summary>Bu iskelenin graf düğümü; −1 = bulunamadı.</summary>
        public int Dugum { get; private set; } = -1;

        /// <summary>Karşıya geçilebilecek iskeleler.</summary>
        private readonly List<int> _karsilar = new();

        /// <summary>
        /// Karşı iskelelerin okunur adları — <see cref="Kur"/>'da bir kez.
        ///
        /// <see cref="Ipucu"/> her karede okunuyor (HUD çizerken) ve
        /// <see cref="YerAdi"/> grafın <b>1.541 düğümünün tamamını</b>
        /// tarıyordu. Doğru cevabı hesaplıyordu; yalnızca saniyede altmış
        /// kez, hiç değişmeyen bir şey için. Bir ad, iskele taşınmadıkça
        /// değişmez — bu yüzden kurulumda bir kez sorulur.
        /// </summary>
        private readonly List<string> _adlar = new();
        private int _secili;

        private void Awake() => Kur();

        /// <summary>
        /// Kendi düğümünü ve karşı iskeleleri bulur.
        ///
        /// Ayrı bir metot çünkü sahne kurulumu (Editor kipi)
        /// <c>Awake</c> çağırmıyor — bu depoda iki kez bedeli ödenmiş
        /// bir ders.
        /// </summary>
        public void Kur()
        {
            if (graf == null)
            {
                var y = FindAnyObjectByType<NPCYonetici>();
                if (y != null) graf = y.graf;
            }
            if (gorev == null) gorev = FindAnyObjectByType<GorevYonetici>();
            if (zaman == null) zaman = FindAnyObjectByType<ZamanSistemi>();
            if (graf == null) return;

            Dugum = graf.EnYakin(transform.position, SokakGrafi.Tur.Iskele);
            _karsilar.Clear();
            if (Dugum < 0) return;

            // KAYIK KENARLARI GRAFTA ZATEN VAR.
            //
            // `SokakGrafiKur.KayikBagla` iskeleleri birbirine bagliyor
            // ve kenarlari `kayik = true` diye isaretliyor. Burada
            // uydurulan bir baglanti yok; var olan ag okunuyor.
            foreach (var k in graf.kenarlar)
            {
                if (!k.kayik) continue;
                if (k.a == Dugum && !_karsilar.Contains(k.b)) _karsilar.Add(k.b);
                else if (k.b == Dugum && !_karsilar.Contains(k.a)) _karsilar.Add(k.a);
            }

            _adlar.Clear();
            foreach (int h in _karsilar) _adlar.Add(YerAdi(h));
        }

        /// <summary>Şu an geçilecek iskele; yoksa −1.</summary>
        public int Hedef
        {
            get
            {
                if (_karsilar.Count == 0) return -1;
                return _karsilar[Mathf.Abs(_secili) % _karsilar.Count];
            }
        }

        /// <summary>Bu geçişin ücreti (akçe).</summary>
        public int Ucret
        {
            get
            {
                int h = Hedef;
                if (h < 0 || graf == null) return 0;
                float d = Vector3.Distance(graf.dugumler[Dugum].konum,
                                           graf.dugumler[h].konum);
                return Ekonomi.Ucret(d);
            }
        }

        public string Ipucu
        {
            get
            {
                int h = Hedef;
                if (h < 0) return "";
                if (Gece) return "Kayık gece işlemez";
                int i = Mathf.Abs(_secili) % _karsilar.Count;
                string ad = i < _adlar.Count ? _adlar[i] : YerAdi(h);
                return $"{ad}'ya geç · {Ucret} akçe";
            }
        }

        /// <summary>
        /// Varış iskelesinin oyuncuya söylenecek adı.
        ///
        /// ## Neden düğümün kendi semti yetmiyor
        ///
        /// İskeleler <b>arazi sahnesinde</b> duruyor, semt sahnelerinde
        /// değil — bu yüzden altı iskele düğümünün altısı da
        /// <c>semt: "TERRAIN"</c> taşıyor ve ekranda
        /// <b>"TERRAIN'ya geç"</b> yazıyordu.
        ///
        /// Daha kötüsü, bunu yazdığım test <b>göremedi</b>: sentetik
        /// bir graf kurup düğüme elle <c>"D_Uskudar"</c> yazıyor ve
        /// yeşil yanıyordu. Yani test kendi kurduğu şeyi ölçüyordu —
        /// bu projede defalarca yakalanan kusurun benim elimden çıkmış
        /// hâli.
        ///
        /// Çözüm ad uydurmak değil, <b>komşuya sormak</b>: iskelenin
        /// çevresindeki en yakın gerçek semt düğümü nerede olduğunu
        /// zaten biliyor.
        /// </summary>
        public string YerAdi(int iskele)
        {
            if (graf == null || iskele < 0
                || iskele >= graf.dugumler.Count) return "karsiya";

            string yer = graf.dugumler[iskele].semt;
            if (Gecerli(yer)) return Duzelt(yer);

            var p = graf.dugumler[iskele].konum;
            float enIyi = float.MaxValue;
            string bulunan = null;
            for (int i = 0; i < graf.dugumler.Count; i++)
            {
                var d = graf.dugumler[i];
                if (!Gecerli(d.semt)) continue;
                float m2 = (d.konum - p).sqrMagnitude;
                if (m2 >= enIyi) continue;
                enIyi = m2;
                bulunan = d.semt;
            }
            return bulunan != null ? Duzelt(bulunan) : "karsiya";
        }

        private static bool Gecerli(string semt) =>
            !string.IsNullOrEmpty(semt) && semt != "TERRAIN";

        private static string Duzelt(string semt) =>
            semt.Replace("D_", "").Replace("_", " ");

        private bool Gece => geceKapali && zaman != null && zaman.Gece;

        public bool Hazir
        {
            get
            {
                if (Hedef < 0 || Gece) return false;
                // Parasi yetmeyen de iskeleye YAKLASABILIR: ipucu
                // gorunur, ucret yazar, gecemez. "Hicbir sey yok" ile
                // "param yetmiyor" ayri seyler ve oyuncu ikincisini
                // bilmeli.
                return true;
            }
        }

        public bool Etkiles(GameObject aktor)
        {
            int h = Hedef;
            if (h < 0 || Gece || graf == null) return false;

            var kese = gorev != null ? gorev.Kese : null;
            int ucret = Ucret;

            // ONCE YETER MI DIYE SOR, SONRA ODE.
            //
            // `Kese.Ode` yetmeyen odemede **eldeki kadarini alir** ve
            // `false` doner — bu kasitli bir karar ve testi var
            // ("yetmeyen odeme: eldeki kadari alinir, borc kalmaz").
            // Ama bir gecis icin yanlis olurdu: oyuncu parasini verip
            // karsiya gecemezdi. `Yeter` tam bunun icin var.
            if (kese == null || !kese.Yeter(ucret))
            {
                // Yetmiyorsa sonraki iskeleye don: belki daha ucuzdur.
                _secili++;
                return false;
            }
            kese.Ode(ucret);

            var varis = graf.dugumler[h].konum;

            // KARAKTER DENETLEYICISI KAPATILMADAN TASINMAZ.
            //
            // Acikken konum atamasi bir sonraki karede geri alinir —
            // `KayitBaglayici` ayni dersi yazili tasiyor.
            var cc = aktor.GetComponentInParent<CharacterController>();
            bool acikti = cc != null && cc.enabled;
            if (acikti) cc.enabled = false;

            var kok = cc != null ? cc.transform : aktor.transform;
            kok.position = Iner(varis);

            if (acikti) cc.enabled = true;
            return true;
        }

        /// <summary>
        /// Karşı iskelede ayağın basacağı nokta.
        ///
        /// ## Neden düğümün konumu yetmiyor
        ///
        /// Graf düğümleri <b>y = 0</b>'da duruyor (deniz seviyesi), ama
        /// iskele bir <b>tahta platform</b>: <c>PF_Iskele</c>'nin güvertesi
        /// 1,60 m yukarıda. Düğüme ışınlanmak oyuncuyu güvertenin 1,4 m
        /// <b>altına</b>, denizin içine bırakıyordu — ve karakter
        /// denetleyicisi oradan yukarı çıkamıyor, çünkü tahta tavan
        /// oluyor. Yani geçiş çalışıyor, varış çalışmıyordu.
        ///
        /// Doğru nokta hesaplanmaz, <b>sorulur</b>: yukarıdan aşağı bir
        /// ışın iskelenin üstünü bulur. Bulamazsa (iskele kaldırılmışsa)
        /// eski davranışa döner — bir ölçüm başarısız olduğunda oyunu
        /// kilitlemek yerine bilinen hâle düşmek.
        /// </summary>
        private static Vector3 Iner(Vector3 dugum)
        {
            const float Yukari = 6f;     // guvertenin (1,6 m) uzerinden
            const float Menzil = 10f;
            var basla = dugum + Vector3.up * Yukari;
            if (Physics.Raycast(basla, Vector3.down, out var v, Menzil, ~0,
                                QueryTriggerInteraction.Ignore))
                return v.point + Vector3.up * 0.15f;
            return dugum + Vector3.up * 0.2f;
        }
    }
}
