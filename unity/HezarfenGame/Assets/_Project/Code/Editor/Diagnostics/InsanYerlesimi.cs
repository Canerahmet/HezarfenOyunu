using System.Collections;
using System.IO;
using System.Text;
using Hezarfen.Sehir;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>İnsanlar yere basıyor mu, duvarın içinde mi.</b>
    ///
    /// ## Neden bu ölçüm eksikti
    ///
    /// Binalar için ölçüm var ve iyi: 36.302 yapı, görünür boşluğu olan
    /// 0 (<c>zemin_denetimi.md</c>); 10.900 ev, çakışan 0
    /// (<c>ev_cakismasi.md</c>). İnsan için <b>hiç yoktu</b> — ve en
    /// çok bakılan şey insan.
    ///
    /// Bedeli karelerde duruyordu: bir gövde saçak kotunda dikiliyor,
    /// biri kaldırıma kalçasına kadar gömülü, biri taş duvarın içinden
    /// çıkıyor. <c>NPCAjan.Sapma</c>'nın belgesi bunu önceden kabul
    /// etmişti — *"duvara girenler olabilir — kabul"* — ama kabul
    /// edilen şey hiç <b>sayılmamıştı</b>. Ölçülmeyen bir kabul, kabul
    /// değil varsayımdır.
    ///
    /// ## Ne ölçülüyor
    ///
    /// Görünür her gövde için üç sayı: ayağın zeminden farkı, statik
    /// geometriyle kesişme, ve en yakın komşuya mesafe. Üçü de
    /// <b>karede görülen</b> kusurlara karşılık geliyor — havada duran,
    /// duvarın içindeki, üst üste binen.
    /// </summary>
    public static class InsanYerlesimi
    {
        private const string Cikti = "../../renders/denetim";

        /// <summary>Ayak-zemin farkı bu kadarı aşarsa kusurdur (m).</summary>
        public const float ZeminPayi = 0.25f;

        [MenuItem("Hezarfen/Olcum/Insan yerlesimini denetle")]
        public static void Baslat()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[Hezarfen] Once OYNAT.");
                return;
            }
            var k = Object.FindAnyObjectByType<Kosucu>()
                    ?? new GameObject("INSAN_YERLESIMI")
                       .AddComponent<Kosucu>();
            k.StartCoroutine(k.Kos());
        }

        public class Kosucu : MonoBehaviour
        {
            internal IEnumerator Kos()
            {
                var y = Object.FindAnyObjectByType<NPCYonetici>();
                if (y == null)
                {
                    Debug.LogError("[Hezarfen] NPC yoneticisi yok.");
                    yield break;
                }

                // Sahne otursun: ilk karelerde govdeler henuz
                // yerlesmemis olur ve olcum yerlesmeyi degil
                // yerlesmemisligi olcer.
                for (int i = 0; i < 120; i++) yield return null;

                int sayilan = 0, havada = 0, gomulu = 0, icinde = 0;
                float enKotuHava = 0f, enKotuGomme = 0f;
                var enKotular = new StringBuilder();

                foreach (var a in y.Sakinler)
                {
                    if (a.govde == null || !a.govde.gameObject.activeInHierarchy)
                        continue;
                    sayilan++;
                    var p = a.govde.position;

                    // --- ayak-zemin farki ---
                    //
                    // CETVEL, DUZELTMENIN KOPYASI OLMAMALI.
                    //
                    // Once burada `p + up*1,2` noktasindan asagi bir
                    // isin atiliyordu — yani `NPCYonetici`'nin gövdeyi
                    // OTURTURKEN kullandigi isinin birebir aynisi. Ayni
                    // soruyu ayni cetvelle sormak, cevabin sifir
                    // cikmasini garanti eder: olcum yerlestiricinin
                    // varsayimini dogrular, oyuncunun gordugu seyi
                    // degil. Bu depoda ucuncu kez ayni tuzak
                    // (`PermeTests` sentetik graf, `OrtamSesiTests`
                    // atanmamis katman) ve bu kez ben kurmusum.
                    //
                    // Dogru cetvel: **ayak kemigi**. İskeletin ayağı
                    // nerede duruyorsa gozun gordugu yer orasidir; kok
                    // transformu degil.
                    var ayak = AyakNoktasi(a.govde);
                    float fark = 0f;
                    if (Physics.Raycast(ayak + Vector3.up * 1.2f,
                                        Vector3.down, out var v, 8f, ~0,
                                        QueryTriggerInteraction.Ignore))
                        fark = ayak.y - v.point.y;

                    if (fark > ZeminPayi)
                    {
                        havada++;
                        if (fark > enKotuHava)
                        {
                            enKotuHava = fark;
                            enKotular.AppendLine(
                                $"  havada {fark:F2} m @ ({p.x:F0}, {p.y:F0}, {p.z:F0})");
                        }
                    }
                    else if (fark < -ZeminPayi)
                    {
                        gomulu++;
                        if (-fark > enKotuGomme)
                        {
                            enKotuGomme = -fark;
                            enKotular.AppendLine(
                                $"  gomulu {-fark:F2} m @ ({p.x:F0}, {p.y:F0}, {p.z:F0})");
                        }
                    }

                    // --- statik geometriyle kesisme ---
                    //
                    // Govde kapsulu: yaricap 0,30, boy 1,70 (oyuncunun
                    // kapsuluyle ayni sozlesme). Tetikleyiciler
                    // sayilmaz — su kupunun tetikleyicisi bir duvar
                    // degil.
                    if (Physics.CheckCapsule(p + Vector3.up * 0.35f,
                                             p + Vector3.up * 1.45f,
                                             0.28f, ~0,
                                             QueryTriggerInteraction.Ignore))
                    {
                        icinde++;
                        if (icinde <= 20)
                            enKotular.AppendLine(
                                $"  geometri icinde @ ({p.x:F0}, {p.y:F0}, {p.z:F0})");
                    }
                }

                Yaz(sayilan, havada, gomulu, icinde,
                    enKotuHava, enKotuGomme, enKotular.ToString());
            }

            /// <summary>
            /// Gövdenin <b>ayağının</b> dünya konumu.
            ///
            /// Animatörü olan bir iskelette ayak kemiği sorulur; yoksa
            /// kök transform'a düşülür. Fark bir ayrıntı değil ölçümün
            /// kendisi: yürüme çevriminde ayak kökten 15-20 cm sapar ve
            /// karelerde "kaldırıma gömülü" görünen tam olarak odur.
            /// </summary>
            private static Vector3 AyakNoktasi(Transform govde)
            {
                var an = govde.GetComponentInChildren<Animator>();
                if (an != null && an.isHuman)
                {
                    var sol = an.GetBoneTransform(HumanBodyBones.LeftFoot);
                    var sag = an.GetBoneTransform(HumanBodyBones.RightFoot);
                    if (sol != null && sag != null)
                        return sol.position.y < sag.position.y
                               ? sol.position : sag.position;
                    if (sol != null) return sol.position;
                    if (sag != null) return sag.position;
                }
                return govde.position;
            }

            private void Yaz(int n, int havada, int gomulu, int icinde,
                             float enHava, float enGomme, string kotular)
            {
                float p(int x) => n > 0 ? 100f * x / n : 0f;

                var sb = new StringBuilder();
                sb.AppendLine("# İnsan yerleşimi denetimi");
                sb.AppendLine();
                sb.AppendLine("Binalar için bu ölçüm vardı ve iyiydi —");
                sb.AppendLine("36.302 yapı, görünür boşluğu olan 0. İnsan");
                sb.AppendLine("için hiç yoktu, ve en çok bakılan şey insan.");
                sb.AppendLine();
                sb.AppendLine($"Görünür gövde: **{n}**");
                sb.AppendLine();
                sb.AppendLine("| ölçü | sayı | oran | kapı |");
                sb.AppendLine("|---|---:|---:|---|");
                sb.AppendLine($"| havada (>{ZeminPayi:F2} m) | {havada} "
                              + $"| %{p(havada):F1} | ≤ %3 |");
                sb.AppendLine($"| gömülü (>{ZeminPayi:F2} m) | {gomulu} "
                              + $"| %{p(gomulu):F1} | ≤ %3 |");
                sb.AppendLine($"| geometri içinde | {icinde} "
                              + $"| %{p(icinde):F1} | ≤ %2 |");
                sb.AppendLine();
                sb.AppendLine($"En kötü: havada {enHava:F2} m, "
                              + $"gömülü {enGomme:F2} m.");
                if (kotular.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("## En kötü konumlar");
                    sb.AppendLine();
                    sb.Append(kotular);
                }

                Directory.CreateDirectory(Cikti);
                File.WriteAllText($"{Cikti}/insan_yerlesimi.md", sb.ToString());
                Debug.Log($"[Hezarfen] Insan yerlesimi: {n} govde, "
                          + $"havada {havada}, gomulu {gomulu}, "
                          + $"geometri icinde {icinde} "
                          + $"-> {Cikti}/insan_yerlesimi.md");
            }
        }
    }
}
