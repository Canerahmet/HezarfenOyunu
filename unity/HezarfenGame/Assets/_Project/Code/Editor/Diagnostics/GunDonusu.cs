using System.Collections;
using System.IO;
using Hezarfen.Zaman;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Aynı yer, üç ışık.</b>
    ///
    /// Bir aydınlatma pası "iyi" ya da "kötü" diye ölçülemez; yalnız
    /// <b>karşılaştırılabilir</b>. Aynı kareyi öğle, gün batımı ve gece
    /// için almak, ışığın hangi saatte çalışıp hangi saatte çöktüğünü
    /// tek bakışta gösterir — ve bu proje boyunca yalnız bakınca
    /// görünen üç kusur çıktı.
    ///
    /// Öğle sertliği, gün batımı sıcaklığı ve gecenin okunabilirliği
    /// ayrı sorunlardır: gece karesi çoğu oyunda ya kör karanlıktır ya
    /// da mavi bir gündüzdür. Ölçüt <see cref="SokakOkunabilirligi"/>
    /// ile ayrıca sayılır; burada gözle bakılacak kare üretiliyor.
    /// </summary>
    public static class GunDonusu
    {
        private const string Cikti = "../../renders/tur";

        /// <summary>Yakalanacak saatler ve dosya adları.</summary>
        private static readonly (float saat, string ad)[] Anlar =
        {
            (12.5f, "isik_ogle"),
            (18.9f, "isik_gunbatimi"),   // ~akşam ezanı civarı
            (22.5f, "isik_gece"),
        };

        [MenuItem("Hezarfen/Denetim/Gun donusu kareleri")]
        public static void Baslat()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[Hezarfen] Once OYNAT.");
                return;
            }
            var k = Object.FindAnyObjectByType<Kosucu>()
                    ?? new GameObject("GUN_DONUSU").AddComponent<Kosucu>();
            k.StartCoroutine(k.Kos());
        }

        public class Kosucu : MonoBehaviour
        {
            internal IEnumerator Kos()
            {
                var zaman = Object.FindAnyObjectByType<ZamanSistemi>();
                if (zaman == null)
                {
                    Debug.LogError("[Hezarfen] ZAMAN yok.");
                    yield break;
                }

                // SAATI DONDURUYORUZ.
                //
                // `gunDakika` acikken saat ilerlemeye devam eder ve
                // uc kare uc FARKLI isikta degil, uc KAYAN isikta
                // cikardi — karsilastirma da anlamini yitirirdi.
                float eskiHiz = zaman.gunDakika;
                float eskiSaat = zaman.saat;
                zaman.gunDakika = 0f;

                Directory.CreateDirectory(Cikti);
                foreach (var (saat, ad) in Anlar)
                {
                    zaman.saat = saat;
                    zaman.Yenile();

                    // Isik ve gokyuzu bir karede oturmaz: hacim
                    // gecisleri ve TAA birikimi birkac kare ister.
                    // 30 kare, TAA'nin toplanma penceresinden uzun.
                    for (int i = 0; i < 30; i++) yield return null;
                    yield return new WaitForEndOfFrame();

                    string yol = $"{Cikti}/{ad}.png";
                    ScreenCapture.CaptureScreenshot(yol);
                    // Yakalama ASENKRON: dosya bir sonraki karede yazilir.
                    yield return null;
                    yield return null;
                    Debug.Log($"[Hezarfen] {ad}: saat {saat:0.0} -> {yol}");
                }

                zaman.gunDakika = eskiHiz;
                zaman.saat = eskiSaat;
                zaman.Yenile();
                Debug.Log("[Hezarfen] Gun donusu kareleri bitti.");
            }
        }
    }
}
