using System.Collections;
using System.Collections.Generic;
using System.IO;
using Hezarfen.Player;
using Hezarfen.Sehir;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Oyunu dolaşır, kare yakalar, ölçer.</b>
    ///
    /// Caner (2026-08-30): *"senin oyunu oynamani ve bu problemleri tespit
    /// edip duzeltene kadar devam etmeni istiyorum."*
    ///
    /// ## Neden böyle bir araç
    ///
    /// Bu oturumun bütün pahalı hataları aynı yerden çıktı: bir şeyi
    /// <b>ölçtüm</b> ama <b>bakmadım</b>. Sayı "18.338 yapı, sıfır boşluk"
    /// diyordu, Caner ekranda havada duran Ayasofya'yı görüyordu. Menü
    /// testleri yeşildi, hiçbir düğme çalışmıyordu. Kamera "arkada" diye
    /// ölçülüyordu, model tersti.
    ///
    /// Ortak sebep: elimde <b>oyunun kendisine bakan</b> bir araç yoktu.
    /// Bu sınıf onu veriyor — oyuncuyu şehrin farklı yerlerine götürür,
    /// yürütür, koşturur, her durakta kamera karesini kaydeder ve yanına
    /// sayıları yazar. Kare bir <b>gözlem</b>, sayı bir <b>kanıt</b>;
    /// ikisi yan yana olmadan hangisinin yalan söylediği anlaşılmıyor.
    ///
    /// ## Ne ölçülür
    ///
    /// Her durakta: ayağın altındaki yüzey, arazi kotundan fark, kamera
    /// kolunun boyu, görünür NPC, görünür replik, ve kare süresi. Bunlar
    /// "oyun bozuk" cümlesini <b>hangi</b> bozukluk olduğuna çevirir.
    /// </summary>
    public static class OyunTuru
    {
        private const string Cikti = "../../renders/tur";

        /// <summary>Bir durak: nereye gidilecek, ne yapılacak.</summary>
        internal struct Durak
        {
            public string ad;
            public Vector3 nokta;      // sıfırsa oyuncunun doğduğu yer
            public float bakisYaw;
            public bool kos;           // gidip koşsun mu
            public string neden;
        }

        /// <summary>
        /// Duraklar. Konumlar bu oturumda ölçülen gerçek yerlerden:
        /// Galata (dünya orijini), Haliç'in başı (−3277, 2591), kara
        /// surları x ≈ −3400, Marmara kıyısı z ≈ −2800, Ayasofya
        /// (549, −1886), kırsal doku (−2500, 0).
        /// </summary>
        private static readonly Durak[] Duraklar =
        {
            new Durak { ad = "01_dogum", nokta = Vector3.zero, bakisYaw = 0f,
                        neden = "Oyuncu ilk burayi gorur." },
            new Durak { ad = "02_dogum_kosu", nokta = Vector3.zero,
                        bakisYaw = 0f, kos = true,
                        neden = "Kosarken karakter ve kamera." },
            new Durak { ad = "03_galata_sokak", nokta = new Vector3(120f, 0f, 60f),
                        bakisYaw = 200f,
                        neden = "Dar sokakta kamera kolu ve kalabalik." },
            new Durak { ad = "04_surici", nokta = new Vector3(-700f, 0f, -1500f),
                        bakisYaw = 90f,
                        neden = "Surici dokusu ve NPC yogunlugu." },
            new Durak { ad = "05_ayasofya", nokta = new Vector3(549f, 0f, -1886f),
                        bakisYaw = 270f,
                        neden = "Landmark oturmasi ve olcek." },
            new Durak { ad = "06_kara_surlari", nokta = new Vector3(-3300f, 0f, -1200f),
                        bakisYaw = 90f,
                        neden = "Sur burclarinin oturmasi." },
            new Durak { ad = "07_kirsal", nokta = new Vector3(-2500f, 0f, -600f),
                        bakisYaw = 45f,
                        neden = "Bostan, yol ve meyvelik — bos zemin sikayeti." },
            new Durak { ad = "08_halic_basi", nokta = new Vector3(-3100f, 0f, 2500f),
                        bakisYaw = 200f,
                        neden = "Dere agzi ve su." },
            new Durak { ad = "09_marmara", nokta = new Vector3(-1850f, 0f, -2700f),
                        bakisYaw = 180f,
                        neden = "Kiyi, iskele ve deniz." },
            new Durak { ad = "10_uskudar", nokta = new Vector3(3500f, 0f, 200f),
                        bakisYaw = 270f,
                        neden = "Karsi yaka — semt akisi calisiyor mu." },
        };

        [MenuItem("Hezarfen/Denetim/Oyun turu (kare + olcum)")]
        public static void Baslat()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[Hezarfen] Once OYNAT: tur oyun calisirken "
                               + "kosar. Play'e bas ve komutu tekrarla.");
                return;
            }
            var kosucu = Object.FindAnyObjectByType<TurKosucu>();
            if (kosucu == null)
            {
                var go = new GameObject("TUR_KOSUCU");
                kosucu = go.AddComponent<TurKosucu>();
            }
            kosucu.StartCoroutine(kosucu.Kos(Duraklar));
        }

        /// <summary>
        /// Turu <b>oyun içinde</b> yürüten davranış.
        ///
        /// Editor kodu tek karede iş yapar; tur ise fizik istiyor: ışınlama
        /// sonrası zeminin oturması, koşarken animasyonun karışması, semt
        /// akışının yüklenmesi hep <b>kare geçmesini</b> bekler. Bu yüzden
        /// coroutine.
        /// </summary>
        public class TurKosucu : MonoBehaviour
        {
            internal IEnumerator Kos(Durak[] duraklar)
            {
                Directory.CreateDirectory(Cikti);
                var satirlar = new List<string>();
                satirlar.Add("# Oyun turu");
                satirlar.Add("");
                satirlar.Add("Her durakta kamera karesi kaydedildi ve yanina");
                satirlar.Add("sayilar yazildi. Kare bir GOZLEM, sayi bir KANIT.");
                satirlar.Add("");
                satirlar.Add("| durak | ayak altinda | arazi farki | kamera kolu "
                             + "| gorunur NPC | replik | kare (ms) | neden |");
                satirlar.Add("|---|---|---:|---:|---:|---:|---:|---|");

                var oyuncu = Object.FindAnyObjectByType<WalkController>();
                var kip = Object.FindAnyObjectByType<KameraKipi>();
                var npc = Object.FindAnyObjectByType<NPCYonetici>();
                var bark = Object.FindAnyObjectByType<BarkGosterici>();
                var arazi = Object.FindAnyObjectByType<Terrain>();
                var kam = Camera.main;
                if (oyuncu == null || kam == null)
                {
                    Debug.LogError("[Hezarfen] Oyuncu ya da kamera yok.");
                    yield break;
                }

                var cc = oyuncu.GetComponent<CharacterController>();
                var dogum = oyuncu.transform.position;

                foreach (var d in duraklar)
                {
                    // --- ISINLA ---
                    var hedef = d.nokta == Vector3.zero ? dogum : d.nokta;
                    if (d.nokta != Vector3.zero)
                    {
                        // Yuzeyi bul: arazi kotu yeterli degil, kaldirim
                        // ve kaide arazinin USTUNDE.
                        var tepe = new Vector3(hedef.x, 400f, hedef.z);
                        hedef = Physics.Raycast(tepe, Vector3.down,
                                                out var v, 800f, ~0,
                                                QueryTriggerInteraction.Ignore)
                            ? v.point + Vector3.up * 0.3f
                            : new Vector3(hedef.x,
                                          (arazi != null
                                           ? arazi.SampleHeight(hedef)
                                             + arazi.transform.position.y
                                           : 0f) + 0.3f,
                                          hedef.z);
                    }

                    cc.enabled = false;
                    oyuncu.transform.position = hedef;
                    oyuncu.transform.rotation = Quaternion.Euler(0f, d.bakisYaw, 0f);
                    cc.enabled = true;
                    Physics.SyncTransforms();

                    // Semt akisi ve zemin otursun.
                    for (int i = 0; i < 90; i++) yield return null;

                    // --- KOS ---
                    if (d.kos)
                    {
                        float t0 = Time.time;
                        while (Time.time - t0 < 2.0f)
                        {
                            cc.Move(oyuncu.transform.forward
                                    * oyuncu.runSpeed * Time.deltaTime
                                    + Vector3.down * 4f * Time.deltaTime);
                            yield return null;
                        }
                    }

                    // --- OLC ---
                    var p = oyuncu.transform.position;
                    string altinda = "?";
                    if (Physics.Raycast(p + Vector3.up * 0.6f, Vector3.down,
                                        out var alt, 12f, ~0,
                                        QueryTriggerInteraction.Ignore))
                        altinda = alt.collider.name;
                    float araziKot = arazi != null
                        ? arazi.SampleHeight(p) + arazi.transform.position.y
                        : 0f;

                    // Kare suresi: on kare ortalamasi.
                    float toplam = 0f;
                    for (int i = 0; i < 10; i++)
                    { yield return null; toplam += Time.unscaledDeltaTime; }
                    float ms = toplam / 10f * 1000f;

                    // --- YAKALA ---
                    var rt = new RenderTexture(1280, 720, 24,
                                               RenderTextureFormat.ARGB32);
                    kam.targetTexture = rt;
                    for (int i = 0; i < 6; i++) kam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;
                    kam.targetTexture = null;
                    File.WriteAllBytes($"{Cikti}/{d.ad}.png",
                                       ImageConversion.EncodeToPNG(tex));
                    Object.DestroyImmediate(tex);
                    rt.Release();

                    satirlar.Add($"| {d.ad} | {altinda} | "
                                 + $"{p.y - araziKot:+0.0;-0.0} | "
                                 + $"{(kip != null ? kip.SonMesafe.ToString("0.00") : "?")} | "
                                 + $"{(npc != null ? npc.GorunurSayisi : 0)} | "
                                 + $"{(bark != null ? bark.GorunurReplik : 0)} | "
                                 + $"{ms:0.0} | {d.neden} |");
                    Debug.Log($"[Hezarfen] tur {d.ad}: {altinda}, "
                              + $"kol {(kip != null ? kip.SonMesafe : 0f):0.0}, "
                              + $"npc {(npc != null ? npc.GorunurSayisi : 0)}, "
                              + $"{ms:0.0} ms");
                }

                File.WriteAllText($"{Cikti}/tur.md",
                                  string.Join("\n", satirlar));
                Debug.Log($"[Hezarfen] OYUN TURU BITTI -> {Cikti}/tur.md");
            }
        }
    }
}
