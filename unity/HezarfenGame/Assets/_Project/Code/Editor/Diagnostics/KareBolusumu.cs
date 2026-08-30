using System.Collections;
using System.IO;
using Hezarfen.Sehir;
using Hezarfen.Tani;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Kare süresini parçalarına ayırır.</b>
    ///
    /// Oyun turunda kare süresi <b>6,5-11,6 ms'den 17-29 ms'ye</b> fırladı;
    /// 29 ms 34 FPS demek ve Faz 7'nin 60 FPS bütçesinin dışı. O turda iki
    /// şey birden değişmişti — nüfus 9.000'den 40.000'e çıktı ve kırsal
    /// doku sıklaştı — yani hangisinin ödettiğini <b>bilmiyorum</b>.
    ///
    /// Bu araç onu bilmek için: her sistem sırayla kapatılır ve kare
    /// yeniden ölçülür. Fark, o sistemin ücretidir. Tahminle
    /// iyileştirmeye kalkmak, bu oturumda beş kez yanlış şeyi düzeltmekle
    /// sonuçlandı.
    /// </summary>
    public static class KareBolusumu
    {
        private const string Cikti = "../../renders/denetim";

        [MenuItem("Hezarfen/Olcum/Kare suresini bolustur")]
        public static void Baslat()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[Hezarfen] Once OYNAT.");
                return;
            }
            var k = Object.FindAnyObjectByType<Kosucu>()
                    ?? new GameObject("KARE_BOLUSUMU").AddComponent<Kosucu>();
            k.StartCoroutine(k.Kos());
        }

        public class Kosucu : MonoBehaviour
        {
            /// <summary>Kaç kare ortalanacak — tek kare gürültüdür.</summary>
            private const int Kare = 120;

            internal IEnumerator Kos()
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("# Kare süresi bölüşümü");
                sb.AppendLine();
                sb.AppendLine($"Her ölçüm {Kare} karenin ortalaması. Bir sistem");
                sb.AppendLine("kapatılır, fark onun ücretidir.");
                sb.AppendLine();
                sb.AppendLine("| durum | kare (ms) | fark (ms) |");
                sb.AppendLine("|---|---:|---:|");

                var npc = Object.FindAnyObjectByType<NPCYonetici>();
                var agac = Object.FindAnyObjectByType<AgacCizici>();
                var bark = Object.FindAnyObjectByType<BarkGosterici>();

                float taban = 0f;

                float t = 0f;
                yield return Olc(Kare, x => t = x);
                taban = t;
                sb.AppendLine($"| hepsi acik | {t:0.0} | — |");

                if (bark != null)
                {
                    bark.enabled = false;
                    yield return Olc(Kare, x => t = x);
                    sb.AppendLine($"| replik kapali | {t:0.0} | {taban - t:+0.0;-0.0} |");
                    bark.enabled = true;
                }

                if (npc != null)
                {
                    npc.enabled = false;
                    yield return Olc(Kare, x => t = x);
                    sb.AppendLine($"| NPC yoneticisi kapali | {t:0.0} | {taban - t:+0.0;-0.0} |");
                    npc.enabled = true;
                }

                if (agac != null)
                {
                    agac.enabled = false;
                    yield return Olc(Kare, x => t = x);
                    sb.AppendLine($"| agac cizici kapali | {t:0.0} | {taban - t:+0.0;-0.0} |");
                    agac.enabled = true;
                }

                sb.AppendLine();
                sb.AppendLine($"sakin={(npc != null ? npc.Sakinler.Count : 0)} "
                              + $"dilim={(npc != null ? npc.dilim : 0)} "
                              + $"gorunur govde={(npc != null ? npc.GorunurSayisi : 0)} "
                              + $"agac cizilen={(agac != null ? agac.CizilenAgac : 0)} "
                              + $"cizim cagrisi={(agac != null ? agac.CizimCagrisi : 0)}");

                Directory.CreateDirectory(Cikti);
                File.WriteAllText($"{Cikti}/kare_bolusumu.md", sb.ToString());
                Debug.Log($"[Hezarfen] Kare bolusumu yazildi -> {Cikti}/kare_bolusumu.md");
            }

            private static IEnumerator Olc(int kare, System.Action<float> sonuc)
            {
                // Ilk kareler degisimden etkilenir; onlari atla.
                for (int i = 0; i < 20; i++) yield return null;
                float toplam = 0f;
                for (int i = 0; i < kare; i++)
                { yield return null; toplam += Time.unscaledDeltaTime; }
                sonuc(toplam / kare * 1000f);
            }
        }
    }
}
