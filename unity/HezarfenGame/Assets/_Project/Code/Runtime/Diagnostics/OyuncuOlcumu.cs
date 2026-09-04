using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hezarfen.Diagnostics
{
    /// <summary>
    /// <b>Yapılmış oyunda kare süresini ölçer.</b>
    ///
    /// ## Neden gerekiyor
    ///
    /// Plan Bölüm 6.1'in kabul satırı 21 Ağustos'tan beri açık:
    /// *"FPS hedefi DOĞRULANMADI — editör render'ı kararlı bir ölçüm
    /// ortamı değil (saçılma farkın on katı) ve gerçek yargı bir oyuncu
    /// yapısı ister."* <see cref="FrameTimeProbe"/> o işi yapıyor ama
    /// yalnız <c>Bench_*</c> sahnelerinde yaşıyor ve o sahneler build
    /// listesinde değil. Yani ölçüm aleti var, ölçülecek yerde yok.
    ///
    /// ## Neden sahneye dokunmuyor
    ///
    /// Kendini <c>RuntimeInitializeOnLoadMethod</c> ile kuruyor ve
    /// yalnızca komut satırında <c>-hezarfenOlcum</c> varsa doğuyor.
    /// Sahneye ya da prefaba bir bileşen eklemek, ölçüm yapılmayan her
    /// koşuda da o bileşeni taşımak demekti; üstelik sahneyi
    /// değiştirmek fırın A/B'sini bozardı.
    ///
    /// ## Ne raporlar — ortalama DEĞİL
    ///
    /// Ortalama takılmayı gizler; oyuncu ortalamayı değil **en kötü
    /// kareleri** hisseder. Ortanca ve **p95** ayrı yazılır ve kapıyı
    /// tutan p95'tir. Bütçe 16,7 ms (60 FPS).
    ///
    /// Çıktı <c>Player.log</c>'a <c>[HZ-OLCUM]</c> önekiyle yazılır;
    /// oradan bir betikle okunur.
    ///
    /// ## Kullanım
    ///
    /// <code>
    /// Hezarfen1632.exe -hezarfenOlcum -logFile olcum.log
    /// </code>
    /// </summary>
    public static class OyuncuOlcumu
    {
        /// <summary>Komut satırı anahtarı.</summary>
        public const string Anahtar = "-hezarfenOlcum";

        /// <summary>Kare bütçesi (ms) — 60 FPS.</summary>
        public const float Butce = 16.7f;

        /// <summary>Bir durakta örneklenen kare sayısı.</summary>
        private const int Ornek = 240;

        /// <summary>Işınlanmadan sonra oturma payı (kare).</summary>
        private const int Otur = 150;

        /// <summary>
        /// Duraklar — oyun turunun ölçtüğü yerlerin aynısı, çünkü kare
        /// süresi ile karenin içeriği aynı yerde okunmalı.
        /// </summary>
        private static readonly (string ad, Vector3 nokta, float yaw, bool donus)[]
            Duraklar =
        {
            ("galata_sokak", new Vector3(300f, 0f, 100f), 0f, false),
            ("surici",       new Vector3(-700f, 0f, -1500f), 0f, false),
            ("uskudar",      new Vector3(4789f, 0f, 716f), 260f, false),
            // KULE TURU: PLANIN TEK KALAN KIRMIZISI.
            //
            // `faz7_performans.md` on iki adimin on birini gecmis,
            // kalan tek adim **kule turu 360°** (p95 17,83 ms, butce
            // 16,67). Bos bir kir durağı olcmek kolay gecer ve hicbir
            // sey ogretmez; olculmesi gereken sey KALAN kusurdur.
            // Bu durakta kamera ornekleme boyunca tam bir tur doner.
            ("kule_turu_360", new Vector3(25f, 0f, 25f), 0f, true),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Kur()
        {
            bool istendi = false;
            foreach (string a in System.Environment.GetCommandLineArgs())
                if (a == Anahtar) { istendi = true; break; }
            if (!istendi) return;

            var go = new GameObject("HZ_OLCUM");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Kosucu>();
        }

        /// <summary>Ölçümü yürüten davranış.</summary>
        private class Kosucu : MonoBehaviour
        {
            private IEnumerator Start()
            {
                // VSYNC VE KARE SINIRI KAPATILIR.
                //
                // Acik kalirsa olculen sey oyunun hizi degil EKRANIN
                // hizidir: 60 Hz'de her kare 16,7 ms okur ve butce tam
                // butce cikar — yani olcum, olcmesi gereken seyin
                // yerine bir sabit koyar. `FrameTimeProbe` de ayni
                // sebeple `targetFrameRate = -1` yaziyor.
                int _eskiVSync = QualitySettings.vSyncCount;
                int _eskiHedef = Application.targetFrameRate;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                Debug.Log($"[HZ-OLCUM] basladi (vsync {_eskiVSync} -> 0, "
                          + $"hedef {_eskiHedef} -> -1, "
                          + $"kademe {QualitySettings.names[QualitySettings.GetQualityLevel()]})");
                // Sahnenin ve akisin oturmasi.
                for (int i = 0; i < 300; i++) yield return null;

                // OYUN, MENUDEN BASLAR — OLCUM ONU BEKLEYEMEZ.
                //
                // Yapilmis oyun `Acilis` sahnesinde aciliyor ve sehre
                // ancak menude "Basla"ya basilinca geciliyor.
                // `RuntimeInitializeOnLoadMethod` ilk sahne yuklenince
                // bir kez calisir — yani bu kosucu MENUDE doguyor ve
                // orada `WalkController` yok. Ilk yazimda tam da bu
                // oldugu icin olcum "oyuncu yok" deyip cikardi.
                //
                // Cozum menuye tiklamak degil, sehir sahnesini DOGRUDAN
                // yuklemek: olculecek sey menu degil sehir.
                if (Object.FindAnyObjectByType<Hezarfen.Player.WalkController>()
                    == null)
                {
                    var menu = Object.FindAnyObjectByType<Hezarfen.Arayuz.AcilisMenusu>();
                    string sahne = menu != null && !string.IsNullOrEmpty(menu.sehirSahnesi)
                                   ? menu.sehirSahnesi : "Faz1_Terrain";
                    Debug.Log($"[HZ-OLCUM] sehir sahnesi yukleniyor: {sahne}");
                    var yukle = UnityEngine.SceneManagement.SceneManager
                                    .LoadSceneAsync(sahne);
                    while (yukle != null && !yukle.isDone) yield return null;
                    for (int i = 0; i < 240; i++) yield return null;
                }

                var oyuncu = Object.FindAnyObjectByType<Hezarfen.Player.WalkController>();
                var akis = Object.FindAnyObjectByType<Hezarfen.Streaming.DistrictStreamer>();
                if (oyuncu == null)
                {
                    Debug.LogError("[HZ-OLCUM] oyuncu yok — olcum yapilamadi");
                    Bitir(1);
                    yield break;
                }
                var cc = oyuncu.GetComponent<CharacterController>();
                // ARAZI KOTU OLCULUR — DURAGIN y'si SIFIRDIR.
                //
                // Durak listesi x/z tasiyor; y'yi 0 birakip oraya
                // isinlamak oyuncuyu Galata sirtinda otuz metre YERIN
                // ALTINA koyardi ve olculen sey karanlik bir kaya
                // olurdu. Tur aracinin yaptigi da bu: kotu araziden
                // sorar.
                var arazi = Object.FindAnyObjectByType<Terrain>();

                bool hepsiGecti = true;
                foreach (var d in Duraklar)
                {
                    if (cc != null) cc.enabled = false;
                    float kot = arazi != null
                        ? arazi.SampleHeight(d.nokta) + arazi.transform.position.y
                        : d.nokta.y;
                    oyuncu.transform.position =
                        new Vector3(d.nokta.x, kot + 0.3f, d.nokta.z);
                    oyuncu.transform.rotation = Quaternion.Euler(0f, d.yaw, 0f);
                    if (cc != null) cc.enabled = true;
                    Physics.SyncTransforms();

                    // AKIS BEKLENIR — YOKSA OLCULEN SEY BOS BIR ARAZIDIR.
                    float t0 = Time.realtimeSinceStartup;
                    while (akis != null && akis.LoadsInFlight > 0
                           && Time.realtimeSinceStartup - t0 < 30f)
                        yield return null;
                    for (int i = 0; i < Otur; i++) yield return null;

                    var s = new List<float>(Ornek);
                    for (int i = 0; i < Ornek; i++)
                    {
                        yield return null;
                        if (d.donus)
                        {
                            // Tam 360°, ornekleme penceresine yayilmis.
                            float t = (i + 1) / (float)Ornek;
                            oyuncu.transform.rotation =
                                Quaternion.Euler(0f, d.yaw + 360f * t, 0f);
                        }
                        s.Add(Time.unscaledDeltaTime * 1000f);
                    }
                    s.Sort();
                    float ortanca = s[s.Count / 2];
                    float p95 = s[Mathf.Clamp((int)(s.Count * 0.95f), 0, s.Count - 1)];
                    float enKotu = s[s.Count - 1];
                    bool gecti = p95 <= Butce;
                    hepsiGecti &= gecti;
                    Debug.Log($"[HZ-OLCUM] {d.ad}: ortanca {ortanca:0.00} ms, "
                              + $"p95 {p95:0.00} ms, en kotu {enKotu:0.00} ms, "
                              + $"butce {Butce:0.0} ms -> {(gecti ? "GECTI" : "KALDI")}");
                }
                Debug.Log($"[HZ-OLCUM] sonuc: {(hepsiGecti ? "GECTI" : "KALDI")}");
                Bitir(hepsiGecti ? 0 : 2);
            }

            private static void Bitir(int kod)
            {
                Debug.Log($"[HZ-OLCUM] bitti (kod {kod})");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit(kod);
#endif
            }
        }
    }
}
