using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hezarfen.Arayuz
{
    /// <summary>
    /// <b>Açılış akışı: menü → yükleme → şehir.</b>
    /// PLAN Bölüm 12 bu kararı Faz 7'ye bırakmıştı.
    ///
    /// ## Neden ayrı bir menü sahnesi
    ///
    /// Şehir sahnesi arazi, su, sur ve semt katmanlarıyla birlikte
    /// yükleniyor ve bu <b>saniyeler</b> sürüyor. Doğrudan oraya açılmak,
    /// oyuncuyu donmuş bir ekranla karşılamak demekti — ilk izlenim bir
    /// takılma olurdu.
    ///
    /// Menü sahnesi neredeyse boştur: anında açılır, oyuncu bir şey
    /// görür, şehir <b>arkada</b> yüklenir.
    ///
    /// ## Yükleme ekranı burada YASAK DEĞİL
    ///
    /// Faz 6'nın "yükleme ekranı yok" ölçütü <b>serbest dolaşım</b> için:
    /// şehirde gezerken ekran kesilmemeli. Açılıştaki tek yükleme onun
    /// kapsamında değil ve olması da gerekir — bir şeyin yüklendiğini
    /// söylemek, sessizce beklemekten dürüsttür.
    /// </summary>
    public class AcilisMenusu : MonoBehaviour
    {
        [Tooltip("Yüklenecek şehir sahnesi.")]
        public string sehirSahnesi = "Faz1_Terrain";

        [Header("Bağlantılar")]
        public GameObject menuPaneli;
        public GameObject yuklemePaneli;
        public GameObject ayarlarPaneli;
        public GameObject krediPaneli;
        public Slider ilerleme;
        public Text ilerlemeYazi;

        [Tooltip("Seçili kademenin ölçülmüş açıklaması.")]
        public Text kademeYazi;

        [Tooltip("Açılışta klavyeyle seçili olacak düğme.")]
        public GameObject ilkSecim;

        /// <summary>Yükleme başladı mı — test okur.</summary>
        public bool Yukleniyor { get; private set; }

        /// <summary>Son görülen ilerleme (0-1) — test okur.</summary>
        public float SonIlerleme { get; private set; }

        private void Start()
        {
            // AYARLAR ACILISTA UYGULANIR.
            //
            // Kaydedilmis ama uygulanmamis bir ayar, olmayan bir ayardir:
            // menude "Balanced" yazar, oyun High Fidelity kosar ve oyuncu
            // neden 50 FPS aldigini anlamaz.
            Ayarlar.Uygula();
            Panel(menuPaneli);
            if (ilkSecim != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(ilkSecim);
        }

        /// <summary>Yalnız verilen paneli açar, ötekileri kapatır.</summary>
        private void Panel(GameObject acik)
        {
            foreach (var p in new[] { menuPaneli, yuklemePaneli,
                                      ayarlarPaneli, krediPaneli })
                if (p != null) p.SetActive(p == acik);
            Sec(acik);
        }

        /// <summary>Şu an açık olan panel.</summary>
        public GameObject AcikPanel
        {
            get
            {
                foreach (var p in new[] { menuPaneli, ayarlarPaneli,
                                          krediPaneli, yuklemePaneli })
                    if (p != null && p.activeInHierarchy) return p;
                return null;
            }
        }

        /// <summary>
        /// Panelin ilk düğmesini <b>seçili</b> yapar.
        ///
        /// Klavye ve oyun kolu <c>Submit</c> eylemi EventSystem'in seçili
        /// nesnesine gider. Panel değişince eski seçim devre dışı kalan
        /// nesneyle birlikte düşer ve o andan sonra <b>hiçbir tuş bir işe
        /// yaramaz</b> — menü açık durur, oyuncu basar, hiçbir şey olmaz.
        /// </summary>
        private static void Sec(GameObject panel)
        {
            var es = EventSystem.current;
            if (es == null || panel == null) return;
            var ilk = panel.GetComponentInChildren<Button>(false);
            es.SetSelectedGameObject(ilk != null ? ilk.gameObject : null);
        }

        /// <summary>
        /// Seçim düşerse geri koyar — klavye asla boşa basmasın.
        ///
        /// Fareyle boşluğa tıklamak da seçimi düşürür; bu güvenlik ağı
        /// olmadan menü o noktadan sonra yalnız fareyle kullanılabilir
        /// hale gelirdi.
        /// </summary>
        private void Update()
        {
            var es = EventSystem.current;
            if (es == null) return;
            var s = es.currentSelectedGameObject;
            if (s == null || !s.activeInHierarchy) Sec(AcikPanel);
        }

        private void Goster(bool menu)
            => Panel(menu ? menuPaneli : yuklemePaneli);

        /// <summary>"Ayarlar" düğmesi.</summary>
        public void AyarlariAc() => Panel(ayarlarPaneli);

        /// <summary>"Krediler" düğmesi.</summary>
        public void KredileriAc() => Panel(krediPaneli);

        /// <summary>"Geri" düğmesi — menüye döner.</summary>
        public void Geri() => Panel(menuPaneli);

        /// <summary>Kalite kademesini değiştirir ve uygular.</summary>
        public void KademeSec(int k)
        {
            Ayarlar.Kademe = k;
            if (kademeYazi != null)
                kademeYazi.text = Ayarlar.KademeAciklamasi[
                    Mathf.Clamp(k, 0, Ayarlar.KademeAciklamasi.Length - 1)];
        }

        /// <summary>"Başla" düğmesi.</summary>
        public void Basla()
        {
            if (Yukleniyor) return;
            Goster(menu: false);
            StartCoroutine(Yukle());
        }

        /// <summary>"Çık" düğmesi.</summary>
        public void Cik()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Şehri asenkron yükler ve ilerlemeyi gösterir.</summary>
        public IEnumerator Yukle()
        {
            Yukleniyor = true;
            SonIlerleme = 0f;

            var is_ = SceneManager.LoadSceneAsync(sehirSahnesi);
            if (is_ == null)
            {
                Debug.LogError($"[Hezarfen] Sahne yuklenemedi: {sehirSahnesi} "
                               + "— build listesinde mi? "
                               + "(Hezarfen -> Boru Hatti -> Build sahne "
                               + "listesini duzelt)");
                Yukleniyor = false;
                Goster(menu: true);
                yield break;
            }

            // Unity ilerlemeyi 0,9'da durdurur ve etkinlestirmeyi bekler;
            // 0,9'u %100 sayarak oyuncuya yalan soylemiyoruz.
            while (!is_.isDone)
            {
                SonIlerleme = Mathf.Clamp01(is_.progress / 0.9f);
                if (ilerleme != null) ilerleme.value = SonIlerleme;
                if (ilerlemeYazi != null)
                    ilerlemeYazi.text = $"Şehir yükleniyor… %{SonIlerleme * 100f:F0}";
                yield return null;
            }
        }
    }
}
