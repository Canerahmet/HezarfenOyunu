using Hezarfen.Zaman;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Vakit girdiğini oyuncuya söyler.</b>
    ///
    /// ## Neden bu bileşen var
    ///
    /// <see cref="VakitHesabi"/> bu projenin en iyi yazılmış parçası:
    /// Hanefî ikindi, gerçek güneş sapması, batıştan kurulan ezanî
    /// saat. Ve <see cref="ZamanSistemi.VakitGirdi"/> olayının çalışma
    /// zamanında <b>tek abonesi yoktu</b> — depoda yalnız tanımı ve bir
    /// test dosyası geçiyordu.
    ///
    /// Yani oyuncunun vakti görebildiği tek yer HUD'ın köşesindeki
    /// küçük bir yazıydı. Bir oyun günü 24 gerçek dakika, yani oyuncu
    /// her 24 dakikada beş vakit yaşıyor ve hiçbirini fark etmiyordu.
    /// Ezanî saat oyunun kimliği olarak seçildi; kimlik <b>fark
    /// edilen</b> şeydir, ödev okunan şeydir.
    ///
    /// Bu, bu oturumda altıncı kez çıkan aynı desen: sistem yazılmış,
    /// oyuna bağlanmamış.
    ///
    /// ## Ses neden burada yok
    ///
    /// Ezanın kendisi bir <b>insan sesi</b> ve onu sentezlemek iki
    /// bakımdan yanlış olurdu: teknik olarak kötü çıkar, ve taklit
    /// edilmesi saygısızdır. Doğrusu ticari kullanıma açık (CC0 / kamu
    /// malı) bir kayıt ve o, <c>refs/LICENSES.md</c>'ye satırı
    /// yazılmadan projeye giremez (CLAUDE.md).
    ///
    /// O kayıt gelene kadar an <b>görülür</b>: vakit adı ekranın
    /// ortasında belirir ve söner. Ses geldiğinde bu bileşen onu
    /// çalacak yerdir — <see cref="ezanKlibi"/> boş bırakıldı ve
    /// doluysa en yakın minareden çalar.
    /// </summary>
    [AddComponentMenu("Hezarfen/Vakit Bildirimi")]
    public class VakitBildirimi : MonoBehaviour
    {
        [Header("Bağlantılar (boşsa sahnede aranır)")]
        public ZamanSistemi zaman;
        public SokakGrafi graf;
        public Transform oyuncu;

        [Tooltip("Ezan kaydı — lisanslı bir kayıt gelene kadar boş.")]
        public AudioClip ezanKlibi;

        [Tooltip("Bildirimin ekranda kalma süresi (s).")]
        public float bildirimSuresi = 3.5f;

        /// <summary>Şu an gösterilecek metin; yoksa boş.</summary>
        public string Bildirim { get; private set; } = "";

        /// <summary>Bildirimin solma oranı 0..1 — HUD saydamlık için okur.</summary>
        public float Tazelik { get; private set; }

        /// <summary>Bu oturumda kaç vakit duyuruldu — ölçüm okur.</summary>
        public int Duyurulan { get; private set; }

        private float _sonu;
        private AudioSource _kaynak;

        private void Awake()
        {
            if (zaman == null) zaman = FindAnyObjectByType<ZamanSistemi>();
            if (oyuncu == null)
            {
                var go = GameObject.Find("OYUNCU");
                if (go != null) oyuncu = go.transform;
            }
            if (graf == null)
            {
                var y = FindAnyObjectByType<NPCYonetici>();
                if (y != null) graf = y.graf;
            }
        }

        private void OnEnable()
        {
            if (zaman != null) zaman.VakitGirdi += Girdi;
        }

        private void OnDisable()
        {
            if (zaman != null) zaman.VakitGirdi -= Girdi;
        }

        private void Update()
        {
            if (_sonu <= 0f) return;
            float kalan = _sonu - Time.time;
            if (kalan <= 0f) { Bildirim = ""; Tazelik = 0f; _sonu = 0f; return; }
            // Son yarim saniyede soner: ani kaybolan yazi bir hata gibi
            // okunur, solan yazi bir gecis gibi.
            Tazelik = Mathf.Clamp01(kalan / 0.5f);
        }

        /// <summary>
        /// Vakit girdi. <b>Public</b>, çünkü test bunu olay beklemeden
        /// çağırabilmeli — zamana bağlı bir şey, zamanı verilebilir
        /// olmalı.
        /// </summary>
        public void Girdi(VakitHesabi.Vakit v)
        {
            Bildirim = Ad(v);
            Tazelik = 1f;
            _sonu = Time.time + bildirimSuresi;
            Duyurulan++;

            if (ezanKlibi == null) return;

            // EZAN EN YAKIN MINAREDEN GELIR, KAFANIN ICINDEN DEGIL.
            //
            // 3B bir kaynak, sesin nereden geldigini soyler ve oyuncu
            // mescidin yonunu ogrenir — sehirde yon bulmanin donem
            // dogru bicimi de budur.
            var yer = EnYakinMinare();
            if (_kaynak == null)
            {
                var go = new GameObject("EZAN");
                go.transform.SetParent(transform, false);
                _kaynak = go.AddComponent<AudioSource>();
                _kaynak.spatialBlend = 1f;
                _kaynak.rolloffMode = AudioRolloffMode.Logarithmic;
                _kaynak.minDistance = 40f;
                _kaynak.maxDistance = 600f;
            }
            _kaynak.transform.position = yer;
            _kaynak.PlayOneShot(ezanKlibi);
        }

        /// <summary>Vaktin oyuncuya görünen adı.</summary>
        public static string Ad(VakitHesabi.Vakit v) => v switch
        {
            VakitHesabi.Vakit.Sabah => "sabah",
            VakitHesabi.Vakit.Gunes => "güneş",
            VakitHesabi.Vakit.Ogle => "öğle",
            VakitHesabi.Vakit.Ikindi => "ikindi",
            VakitHesabi.Vakit.Aksam => "akşam",
            VakitHesabi.Vakit.Yatsi => "yatsı",
            _ => v.ToString().ToLowerInvariant(),
        };

        private Vector3 EnYakinMinare()
        {
            if (graf == null || oyuncu == null) return transform.position;
            int i = graf.EnYakin(oyuncu.position, SokakGrafi.Tur.Mescit);
            if (i < 0) i = graf.EnYakin(oyuncu.position, SokakGrafi.Tur.Cami);
            if (i < 0) return oyuncu.position;
            // Minare tepesi: ses damdan degil YUKARIDAN gelir.
            return graf.dugumler[i].konum + Vector3.up * 22f;
        }
    }
}
