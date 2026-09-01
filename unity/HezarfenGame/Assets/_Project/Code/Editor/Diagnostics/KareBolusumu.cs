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

        /// <summary>
        /// Toplu kipten koşulabilen giriş — <b>projenin kapısı bu ölçüm</b>.
        ///
        /// <c>Baslat</c> oynatma kipi ister ve oynatma kipine yalnız
        /// Editor penceresinden girilebiliyordu; yani sözleşmenin
        /// *"bir faz kendi kare ölçümü olmadan bitmiş sayılmaz"*
        /// kuralı, elle yapılan bir adıma bağlıydı. Elle yapılan adım,
        /// yapılmayan adımdır.
        ///
        /// <c>-nographics</c> <b>verilmemeli</b>: grafik aygıtı
        /// olmadan ölçülen kare süresi bir sayı üretir ama hiçbir şey
        /// ölçmez.
        /// </summary>
        public static void TopluKos()
        {
            // SAHNE ACILMADAN OYNATILMAZ.
            //
            // Ilk denemede toplu kosum acik olan sahneyi oynatti ve
            // rapor "0,1 ms, sakin=0, cizim cagrisi=0" yazdi. Yani
            // cetvel bir sayi uretti ve hicbir sey olcmedi — bu
            // oturumda dort kez yakalanan kusurun cetvelin kendi
            // elinden cikmis hali.
            UnityEditor.SceneManagement.EditorSceneManager
                .OpenScene(OyunSahnesi);
            EditorApplication.playModeStateChanged += DurumDegisti;
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Ölçümün yapılacağı sahne.</summary>
        private const string OyunSahnesi =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";

        private static void DurumDegisti(PlayModeStateChange d)
        {
            if (d != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= DurumDegisti;
            Baslat();
        }

        /// <summary>Toplu koşumdan gelindiyse ölçüm bitince çıkılır.</summary>
        private static bool Toplu =>
            System.Environment.CommandLine.Contains("TopluKos");

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

                // ISINMA — TABAN BUNSUZ YALAN SOYLER.
                //
                // Bir tur olcum sunu verdi:
                //
                //     hepsi acik            123,9 ms
                //     replik kapali          15,2 ms   (+108,7)
                //     NPC yoneticisi kapali  13,6 ms   (+110,3)
                //
                // Okundugu gibi alinsa "replik gostericisi 108 ms
                // yiyor" denirdi. Yemiyordu: ilk 120 kare SEMT
                // AKISININ icine dusuyordu — D_Galata ve D_Halic o
                // sirada diskten yukleniyordu. Taban sisti, ondan
                // cikarilan her fark sisti, ve ucu birden ayni sahte
                // sayiyi tasidi.
                //
                // 20 karelik atlama vardi ve yetmedi: 100 ms'lik
                // karede 20 kare 2 saniyedir. Sure tahmin etmek yerine
                // KARARLILIGIN KENDISI olculuyor — pencere pencere
                // bakilir, iki komsu pencere birbirine yakinsa sahne
                // oturmustur.
                yield return Isin();

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

                // BOS SAHNE OLCULMEZ.
                //
                // Sifir cizim cagrisi bir performans sonucu degil, bir
                // olcum basarisizligidir; onu 0,1 ms diye yazmak
                // yalandir. Bir cetvel, olcemedigini soyleyebilmeli.
                int cizim = agac != null ? agac.CizimCagrisi : 0;
                if (cizim == 0 || (npc != null && npc.GorunurSayisi == 0))
                {
                    Debug.LogError(
                        "[Hezarfen] Kare olculemedi: cizim cagrisi "
                        + $"{cizim}, gorunur govde "
                        + $"{(npc != null ? npc.GorunurSayisi : -1)}. "
                        + "Sahne bos ya da akis yuklenmedi — rapor "
                        + "YAZILMADI.");
                    if (Toplu) EditorApplication.Exit(1);
                    yield break;
                }

                Directory.CreateDirectory(Cikti);
                File.WriteAllText($"{Cikti}/kare_bolusumu.md", sb.ToString());
                Debug.Log($"[Hezarfen] Kare bolusumu yazildi -> {Cikti}/kare_bolusumu.md");
                if (Toplu) EditorApplication.Exit(0);
            }

            /// <summary>
            /// Sahne oturana kadar bekler: 30 karelik pencereler alir,
            /// iki ardisik pencere %8 icinde bulusunca doner.
            /// Ust sinir 900 kare — oturmayan sahne olculur ama
            /// oturmadigi konsola yazilir.
            /// </summary>
            private static IEnumerator Isin()
            {
                const int Pencere = 30, EnCokKare = 900;
                float onceki = -1f;
                int gecen = 0;

                // KARARLILIK, HAZIRLIK DEMEK DEGIL.
                //
                // Ilk yazimda yalniz "iki komsu pencere birbirine
                // yakinsa oturmustur" deniyordu ve olcum yine kandi:
                // "90 karede oturdu (191,6 ms)". Yuk sirasinda kare
                // suresi 190 ms'de DUZ bir plato ciziyor; iki pencere
                // pekala anlasir. Duran bir sey oturmus degildir.
                //
                // Semt akisi da yeterli sinyal cikmadi: akis bosta
                // gorunurken kare hala 180 ms'ti — sisiren sey disk
                // degil, ilk karelerde derlenen govde/golge/prob
                // gecisleri. Yani "kim mesgul" diye sormak da yanlis
                // soruydu.
                //
                // Dogru soru en basitiymis: SIMDIYE KADARKI EN IYIYE
                // yakin miyiz. Isinma tam olarak budur — sahne, bir
                // daha inmeyecegi bir taban bulur. Iki komsu pencere
                // birbirine YAKIN ve ikisi de en iyinin %25 ustunde
                // DEGILSE, oturmustur. Plato artik kandiramaz cunku
                // 180 ms'lik plato 13 ms'lik en iyiye yakin degildir.
                var akis = Object.FindAnyObjectByType<
                    Streaming.DistrictStreamer>();
                float enIyi = float.MaxValue;

                while (gecen < EnCokKare)
                {
                    float toplam = 0f;
                    for (int i = 0; i < Pencere; i++)
                    { yield return null; toplam += Time.unscaledDeltaTime; }
                    gecen += Pencere;
                    float su = toplam / Pencere * 1000f;
                    enIyi = Mathf.Min(enIyi, su);
                    bool akisBosta = akis == null || akis.LoadsInFlight == 0;
                    bool tabanaYakin = su <= enIyi * 1.25f;

                    // MUTLAK TAVAN — GORECE OLCU DORDUNCU KEZ KANDI.
                    //
                    // "Simdiye kadarki en iyiye yakin miyiz" testi, EN
                    // IYININ KENDISI kotuyse calismiyor: ilk pencerelerin
                    // hepsi yukleme platosuna dustugunde kosan minimum da
                    // sisiyor ve "195,1 ms; gorulen en iyi 193,6 ms" diye
                    // memnun bir sekilde oturuyor.
                    //
                    // Bir goreceli olcu, karsilastirdigi seyin dogrulugunu
                    // hic sorgulamaz. Yanina mutlak bir sinir gerekiyor ve
                    // o sinir uydurulmuyor: butce 16,7 ms, yukleme platosu
                    // ~190 ms. 45 ms ikisinin arasinda genis bir yerde
                    // duruyor — bir kare 45 ms'nin altindaysa sahne
                    // yuklenmeyi bitirmistir, 16,7'yi asiyor olsa bile.
                    bool makul = su < 45f;

                    if (akisBosta && tabanaYakin && makul && onceki > 0f
                        && Mathf.Abs(su - onceki) <= onceki * 0.08f)
                    {
                        Debug.Log($"[Hezarfen] Isinma: {gecen} karede "
                                  + $"oturdu ({su:0.0} ms; gorulen en iyi "
                                  + $"{enIyi:0.0} ms).");
                        yield break;
                    }
                    onceki = su;
                }
                Debug.LogWarning($"[Hezarfen] Isinma: {EnCokKare} karede "
                                 + "oturmadi — asagidaki sayilar kararsiz "
                                 + "bir sahneden geliyor.");
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
