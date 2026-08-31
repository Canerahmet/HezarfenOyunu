using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Hezarfen.Flight;
using Hezarfen.Player;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Oyunun finali gerçekten uçuluyor mu.</b>
    ///
    /// ## Neden bu ölçüm var
    ///
    /// Uçuş bu oyunun kalbi ve bugüne kadar hiç <b>oyun sahnesinde</b>
    /// ölçülmedi. Ölçülmeyince de şu görülmedi: kule (52 m) ile
    /// Doğancılar (46,6 m) arası 3.336 m yatay ve 5,4 m kot farkı var,
    /// yani gereken süzülme oranı <b>618:1</b>. Kanadın verdiği
    /// 11,56:1. Sabit 9 m/s kuyruk rüzgârıyla gidilen mesafe 1.037 m.
    /// Oyunun finali <b>aritmetik olarak</b> bitirilemezdi ve hiçbir
    /// test bunu sormuyordu — çünkü bütün testler kanadın <b>fiziğini</b>
    /// soruyordu, kanadın <b>gittiği yeri</b> değil.
    ///
    /// Açığı kapatacak şey termik: uçuş önce Galata yamacında yükselir,
    /// sonra Boğaz'ı geçer. Gereken net kazanç 283 m. Bu sınıf o
    /// kazancın gerçekten toplanıp toplanmadığını sayar.
    ///
    /// ## Neden otomatik pilot, neden elle uçmak değil
    ///
    /// Elle uçulan tek bir uçuş, bir kere yapılan bir gözlemdir; ertesi
    /// gün tekrarlanamaz ve iyileşip iyileşmediği bilinemez. Burada
    /// aynı 20 uçuş her turda aynı tohumla tekrarlanır. Pilot basittir
    /// ve bilerek öyle: <b>en iyi süzülme hızını tut, tırmanış
    /// bulduğunda dön</b>. Bir insanın yapacağından kötü uçar, yani
    /// ölçüm iyimser değil kötümserdir — ve bir kapı için doğru olan
    /// yön budur.
    /// </summary>
    public static class UcusDenemesi
    {
        private const string Cikti = "../../renders/denetim";

        /// <summary>
        /// Kule <b>tabanı</b> — dünya orijini (ADR 0007).
        ///
        /// Kalkış noktası bu DEĞİL: ilk koşumda oyuncu buraya konuldu
        /// ve yirmi uçuşun yirmisi de sıfır saniye sürdü, çünkü burası
        /// yer seviyesi. Atlayan anında zemine değip iniyordu ve tablo
        /// "0/20 vardı" diyordu — doğru bir sayı, yanlış bir soru.
        ///
        /// Tepe <see cref="KuleTepesi"/> ile <b>ölçülür</b>, yazılmaz:
        /// kule modeli değişirse sayı da değişsin.
        /// </summary>
        public static readonly Vector3 KuleTabani = new Vector3(0f, 52f, 0f);

        /// <summary>Doğancılar — iniş hedefi.</summary>
        public static readonly Vector3 Dogancilar =
            new Vector3(3267.6f, 46.6f, -672.9f);

        /// <summary>Perde2Dilimi'nin iniş yarıçapı.</summary>
        public const float InisYaricapi = 220f;

        /// <summary>Kaç uçuş denenir.</summary>
        public const int Deneme = 20;

        /// <summary>
        /// Kulenin şerefesi — atlayış buradan yapılır.
        ///
        /// Yukarıdan aşağı ışın atılır; ilk çarptığı yer kulenin
        /// tepesidir. Bir sayı yazmak yerine ölçmek, kule modeli
        /// değiştiğinde bu ölçümün sessizce yanlışa düşmesini önler —
        /// bu oturumda tam olarak öyle bir sessiz yanlış yaşandı.
        ///
        /// RESEARCH.md 168: <i>"Galata Kulesi ile Doğancılar arası kot
        /// farkı ~62 m, yatay mesafe ~3358 m"</i>. Ölçülen tepe bu
        /// sayıyla tutmalı; tutmuyorsa ya kule ya hedef yanlış yerde.
        /// </summary>
        public static Vector3 KuleTepesi()
        {
            var tepeden = new Vector3(KuleTabani.x, KuleTabani.y + 300f,
                                      KuleTabani.z);
            if (Physics.Raycast(tepeden, Vector3.down, out var v, 320f, ~0,
                                QueryTriggerInteraction.Ignore))
                return v.point;
            return KuleTabani;
        }

        [MenuItem("Hezarfen/Olcum/Ucus denemesi (20 ucus)")]
        public static void Baslat()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[Hezarfen] Once OYNAT.");
                return;
            }
            var k = Object.FindAnyObjectByType<Kosucu>()
                    ?? new GameObject("UCUS_DENEMESI").AddComponent<Kosucu>();
            k.StartCoroutine(k.Kos());
        }

        public class Kosucu : MonoBehaviour
        {
            private sealed class Sonuc
            {
                public int no;
                public float yatay;        // katedilen yatay mesafe (m)
                public float sure;         // uçuş süresi (s)
                public float enYuksek;     // en yüksek kot (m)
                public float kazanc;       // kalkış kotuna göre net kazanç
                public float hedefeUzak;   // iniş noktası → Doğancılar
                public bool vardi;
                public string hata;
                public Vector3 bas;
            }

            internal IEnumerator Kos()
            {
                var oyuncu = GameObject.Find("OYUNCU");
                if (oyuncu == null)
                {
                    Debug.LogError("[Hezarfen] OYUNCU yok.");
                    yield break;
                }
                var suzulme = oyuncu.GetComponent<GlideController>();
                var dizi = oyuncu.GetComponent<UcusDizisi>();
                var govde = oyuncu.GetComponent<Rigidbody>();
                if (suzulme == null || dizi == null || govde == null)
                {
                    Debug.LogError("[Hezarfen] Ucus bilesenleri eksik.");
                    yield break;
                }

                var alan = Object.FindAnyObjectByType<WindField>();
                var sonuclar = new List<Sonuc>();

                // ZAMAN SABITLENIR.
                //
                // Kare suresi degiskense her ucus baska bir fizik adimi
                // gorur ve yirmi ucusun dagilimi ruzgari degil bilgisayari
                // olcer. 60 Hz sabit.
                float eskiYakala = Time.captureDeltaTime;
                Time.captureDeltaTime = 1f / 60f;

                for (int i = 0; i < Deneme; i++)
                {
                    // Baslangic yonu: kuleden yelpaze halinde bes yon,
                    // her yonde dort tekrar (turbulans ve faz farki).
                    float yaw = -40f + (i % 5) * 20f;
                    var s = new Sonuc { no = i + 1 };
                    yield return Uc(oyuncu, dizi, govde, yaw, s);
                    sonuclar.Add(s);
                }

                Time.captureDeltaTime = eskiYakala;
                Yaz(sonuclar, alan);
            }

            /// <summary>
            /// Bir uçuş — <b>durum makinesini atlayarak</b>.
            ///
            /// İlk üç koşum <see cref="UcusDizisi"/> üzerinden gitti ve
            /// üçü de başka bir yerde takıldı: kule tabanında doğuldu,
            /// kuşanırken dama düşüldü, kalkışta zemin ayağın altında
            /// bulundu. Her seferinde tabloda sıfır vardı ve her
            /// seferinde sebep <b>uçuşun kendisi değildi</b>.
            ///
            /// Sorulan soru şu: <i>kanat ve termik, 3.336 m'yi
            /// kapatıyor mu.</i> Bu sorunun kuşanma animasyonuyla,
            /// temas denetimiyle ya da giriş durumuyla ilgisi yok.
            /// Onları ölçüme karıştırmak, ölçülmek istenen şeyi üç
            /// ayrı gürültünün arkasına saklamak oldu.
            ///
            /// Burada uçuş fiziği <b>doğrudan</b> sürülüyor: gövde
            /// kinematiklikten çıkar, süzülme açılır, fırlatma hızı
            /// verilir ve zemine değene kadar entegre edilir. Durum
            /// makinesinin çalışıp çalışmadığı ayrı bir soru ve ayrı
            /// bir yerde sorulmalı.
            /// </summary>
            private IEnumerator Uc(GameObject oyuncu, UcusDizisi dizi,
                                   Rigidbody govde, float yaw, Sonuc s)
            {
                var cc = oyuncu.GetComponent<CharacterController>();
                var yurume = oyuncu.GetComponent<Hezarfen.Player.WalkController>();
                var kapsul = oyuncu.GetComponent<CapsuleCollider>();
                var suzulme = oyuncu.GetComponent<GlideController>();
                var girdi = oyuncu.GetComponent<PlayerFlightInput>();

                // --- yurume fizigini kapat, ucus fizigini ac ---
                if (dizi != null) dizi.enabled = false;
                if (yurume != null) yurume.enabled = false;
                if (cc != null) cc.enabled = false;
                if (kapsul != null) kapsul.enabled = true;
                if (girdi != null) girdi.enabled = false;

                // ISINLAMA FIZIK YOLUYLA.
                //
                // Onceki kosum `transform.SetPositionAndRotation` ile
                // kuleye koydu ve tutmadi: tabloda "en yuksek 69-71 m,
                // hedefe 3401 m" yaziyordu ve bunlar kulenin degil
                // DOGUM YERININ sayilari. Bir Rigidbody varken konumun
                // sahibi transform degil govdedir; ikisine ayri ayri
                // yazmak, bir sayinin iki sahibi olmasinin bu
                // dosyadaki hali.
                var tepe = KuleTepesi();
                var kalkis = tepe + Vector3.up * 2.0f;

                govde.isKinematic = false;
                govde.useGravity = true;
                govde.position = kalkis;
                govde.rotation = Quaternion.Euler(0f, yaw, 0f);
                oyuncu.transform.SetPositionAndRotation(
                    kalkis, Quaternion.Euler(0f, yaw, 0f));
                suzulme.enabled = true;
                var pilot = new ConstantFlightInput();
                suzulme.SetInput(pilot);

                var firlatma = oyuncu.GetComponent<FlightLaunch>();
                if (firlatma != null) firlatma.Launch();
                yield return new WaitForFixedUpdate();

                var bas = oyuncu.transform.position;
                s.bas = bas;
                float t = 0f, enYuksek = bas.y;
                var arazi = Terrain.activeTerrain;

                while (t < 900f)
                {
                    t += Time.deltaTime;
                    var p = oyuncu.transform.position;
                    enYuksek = Mathf.Max(enYuksek, p.y);
                    Pilotla(oyuncu, govde, pilot);

                    // YERE DEGDI MI: arazi kotunun 1,5 m altina inen
                    // ucus bitmistir. Carpistirici degil KOT sorulur —
                    // carpistirici sorusu, bu olcumun uc kez takildigi
                    // yer.
                    float kot = arazi != null
                        ? arazi.SampleHeight(p) + arazi.transform.position.y
                        : 0f;
                    if (p.y < Mathf.Max(kot, 0f) + 1.5f) break;
                    yield return null;
                }

                var son = oyuncu.transform.position;
                s.sure = t;
                s.enYuksek = enYuksek;
                s.kazanc = enYuksek - bas.y;
                s.yatay = Vector2.Distance(new Vector2(bas.x, bas.z),
                                           new Vector2(son.x, son.z));
                s.hedefeUzak = Vector2.Distance(
                    new Vector2(son.x, son.z),
                    new Vector2(Dogancilar.x, Dogancilar.z));
                s.vardi = s.hedefeUzak <= InisYaricapi;
                if (t >= 900f) s.hata = "sure doldu";

                // --- yurume fizigine don ---
                govde.linearVelocity = Vector3.zero;
                govde.angularVelocity = Vector3.zero;
                govde.isKinematic = true;
                govde.useGravity = false;
                suzulme.enabled = false;
                if (kapsul != null) kapsul.enabled = false;
                if (cc != null) cc.enabled = true;
                if (yurume != null) yurume.enabled = true;
                if (girdi != null) girdi.enabled = true;
                if (dizi != null) dizi.enabled = true;
                yield return null;
            }

            /// <summary>
            /// Basit pilot: hedefe yönel, tırmanış varsa dön (termikte kal),
            /// yoksa en iyi süzülme hızını tut.
            ///
            /// Kasten basit — bkz. sınıf belgesi. İnsandan kötü uçar,
            /// yani ölçüm kötümser tarafta durur.
            /// </summary>
            private static void Pilotla(GameObject oyuncu, Rigidbody govde,
                                        ConstantFlightInput pilot)
            {
                var p = oyuncu.transform.position;
                var hedefYon = new Vector3(Dogancilar.x - p.x, 0f,
                                           Dogancilar.z - p.z).normalized;

                // Tirmaniyor muyuz: dikey hiz pozitifse termiktesin,
                // daire ciz ve yuksel.
                bool tirmaniyor = govde.linearVelocity.y > 0.15f;

                // Yeterince yuksekse termikte oyalanma, yola cik.
                float ground = 0f;
                var arazi = Terrain.activeTerrain;
                if (arazi != null)
                    ground = arazi.SampleHeight(p) + arazi.transform.position.y;
                float agl = p.y - ground;
                bool yeter = agl > 380f;

                Vector3 istenen = (tirmaniyor && !yeter)
                    ? Quaternion.Euler(0f, 55f, 0f) * oyuncu.transform.forward
                    : hedefYon;

                float aci = Vector3.SignedAngle(oyuncu.transform.forward,
                                                istenen, Vector3.up);
                pilot.Pitch = tirmaniyor && !yeter ? 0.10f : 0f;
                pilot.Roll = Mathf.Clamp(aci / 45f, -1f, 1f);
            }

            private void Yaz(List<Sonuc> l, WindField alan)
            {
                int varan = 0;
                float toplamYatay = 0f, toplamKazanc = 0f, toplamSure = 0f;
                foreach (var s in l)
                {
                    if (s.vardi) varan++;
                    toplamYatay += s.yatay;
                    toplamKazanc += s.kazanc;
                    toplamSure += s.sure;
                }
                int n = Mathf.Max(1, l.Count);

                float gereken = Vector2.Distance(
                    new Vector2(KuleTabani.x, KuleTabani.z),
                    new Vector2(Dogancilar.x, Dogancilar.z));
                var tepe = KuleTepesi();
                float kot = tepe.y - Dogancilar.y;

                var sb = new StringBuilder();
                sb.AppendLine("# Uçuş denemesi — kuleden Doğancılar'a");
                sb.AppendLine();
                sb.AppendLine($"Gereken yatay mesafe **{gereken:F0} m**, "
                              + $"iniş yarıçapı {InisYaricapi:F0} m.");
                sb.AppendLine($"Ölçülen şerefe kotu **{tepe.y:F1} m**, hedef "
                              + $"{Dogancilar.y:F1} m, kot farkı "
                              + $"**{kot:F1} m** (RESEARCH.md 168: ~62 m).");
                sb.AppendLine($"Kanadın en iyi süzülme oranı 11,56:1, yani "
                              + $"kaldıraçsız menzil {kot * 11.56f:F0} m. "
                              + $"Gereken oran **{gereken / Mathf.Max(1f, kot):F0}:1**. "
                              + $"Açığı kapatmak için gereken net yükselme "
                              + $"**{gereken / 11.56f - kot:F0} m** — bu uçuş "
                              + "**termikle** yapılır ya da hiç yapılmaz.");
                sb.AppendLine();
                sb.AppendLine($"Rüzgâr alanı: "
                              + (alan == null ? "**YOK**"
                                 : (alan.terrainThermal != null
                                    ? "termik BAĞLI" : "termik yok")));
                sb.AppendLine();
                sb.AppendLine("| # | kalkış | yatay (m) | süre (s) "
                              + "| en yüksek (m) | kazanç (m) | hedefe (m) "
                              + "| vardı |");
                sb.AppendLine("|---:|---|---:|---:|---:|---:|---:|:--:|");
                foreach (var s in l)
                    sb.AppendLine($"| {s.no} "
                                  + $"| ({s.bas.x:F0}, {s.bas.y:F0}, {s.bas.z:F0}) "
                                  + $"| {s.yatay:F0} | {s.sure:F1} "
                                  + $"| {s.enYuksek:F0} | {s.kazanc:F0} "
                                  + $"| {s.hedefeUzak:F0} "
                                  + $"| {(s.vardi ? "✔" : s.hata ?? "—")} |");
                sb.AppendLine();
                sb.AppendLine($"**Varan: {varan}/{l.Count} "
                              + $"(%{100f * varan / n:F0})** · "
                              + $"ortalama yatay {toplamYatay / n:F0} m · "
                              + $"ortalama kazanç {toplamKazanc / n:F0} m · "
                              + $"ortalama süre {toplamSure / n:F0} s");
                sb.AppendLine();
                sb.AppendLine("Kapı: varan ≥ %70.");

                Directory.CreateDirectory(Cikti);
                File.WriteAllText($"{Cikti}/ucus_denemesi.md", sb.ToString());
                Debug.Log($"[Hezarfen] Ucus denemesi: {varan}/{l.Count} vardi "
                          + $"-> {Cikti}/ucus_denemesi.md");
            }
        }
    }
}
