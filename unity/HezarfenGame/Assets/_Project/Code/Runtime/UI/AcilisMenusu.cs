using System.Collections;
using UnityEngine;
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
        public Slider ilerleme;
        public Text ilerlemeYazi;

        /// <summary>Yükleme başladı mı — test okur.</summary>
        public bool Yukleniyor { get; private set; }

        /// <summary>Son görülen ilerleme (0-1) — test okur.</summary>
        public float SonIlerleme { get; private set; }

        private void Start() => Goster(menu: true);

        private void Goster(bool menu)
        {
            if (menuPaneli != null) menuPaneli.SetActive(menu);
            if (yuklemePaneli != null) yuklemePaneli.SetActive(!menu);
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
