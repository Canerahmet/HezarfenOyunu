using UnityEngine;

namespace Hezarfen.Arayuz
{
    /// <summary>
    /// <b>Oyuncu ayarları.</b> Faz 7'de ölçülen kademeler burada oyuncuya
    /// açılıyor.
    ///
    /// ## Kademe adları OLDUĞU GİBİ
    ///
    /// Unity'nin kalite seviyeleri High Fidelity / Balanced / Performant.
    /// Bunlara "Ultra / Yüksek / Orta" demek, ölçülen şeyle menüde yazan
    /// şeyi ayırırdı. Daha önemlisi: <b>High Fidelity 1440p/60 VERMİYOR</b>
    /// ve bunu ölçtük (SSGI boş bir yamaçta bile 6,9 ms). Menü bunu
    /// söylüyor; "en iyisi" diye sunup oyuncuyu 50 FPS'e düşürmek
    /// dürüstlük olmazdı.
    /// </summary>
    public static class Ayarlar
    {
        private const string AnhKademe = "hz_kademe";
        private const string AnhSes = "hz_ses";
        private const string AnhTamEkran = "hz_tamekran";

        /// <summary>Kademe açıklamaları — ölçümden geliyor, tahminden değil.</summary>
        public static readonly string[] KademeAciklamasi =
        {
            "High Fidelity — SSGI açık. En zengin ışık, ama 1440p'de 60 FPS vermez.",
            "Balanced — ölçülen hedef: 1080p/60 ve 1440p/60.",
            "Performant — düşük donanım; SSGI ve prob sıçraması kapalı.",
        };

        /// <summary>Kalite kademesi (0 = High Fidelity).</summary>
        public static int Kademe
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(AnhKademe, 1),
                               0, QualitySettings.names.Length - 1);
            set
            {
                int k = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
                PlayerPrefs.SetInt(AnhKademe, k);
                QualitySettings.SetQualityLevel(k, true);
            }
        }

        /// <summary>Genel ses seviyesi (0-1).</summary>
        public static float Ses
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(AnhSes, 0.8f));
            set
            {
                float s = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(AnhSes, s);
                AudioListener.volume = s;
            }
        }

        public static bool TamEkran
        {
            get => PlayerPrefs.GetInt(AnhTamEkran, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(AnhTamEkran, value ? 1 : 0);
                Screen.fullScreen = value;
            }
        }

        /// <summary>
        /// Kayıtlı ayarları uygular.
        ///
        /// <b>Açılışta çağrılmazsa ayarlar yalnız menüde görünür</b> ve
        /// oyunda hiçbir şey değişmez — kaydedilmiş ama uygulanmamış bir
        /// ayar, olmayan bir ayardır.
        /// </summary>
        public static void Uygula()
        {
            QualitySettings.SetQualityLevel(Kademe, true);
            AudioListener.volume = Ses;
            Screen.fullScreen = TamEkran;
        }

        /// <summary>Varsayılanlara döner (Balanced — ölçülen hedef).</summary>
        public static void Sifirla()
        {
            PlayerPrefs.DeleteKey(AnhKademe);
            PlayerPrefs.DeleteKey(AnhSes);
            PlayerPrefs.DeleteKey(AnhTamEkran);
            Uygula();
        }
    }
}
