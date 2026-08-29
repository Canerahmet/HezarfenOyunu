using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// <b>Windows build hattı</b> — Faz 8'in ilk işi.
    ///
    /// CLAUDE.md bu komutu tarif ediyordu:
    /// <code>
    /// Unity.exe -batchmode -executeMethod
    ///     Hezarfen.Editor.Pipeline.BuildPipelineEntry.BuildWindows -quit
    /// </code>
    /// ama sınıf yoktu. Faz 7'nin bıraktığı "gerçek otuz dakikalık oturum"
    /// ölçümü de, kapalı test de bunun üstünde duruyor.
    ///
    /// ## Sıra ÖNEMLİ: önce Addressables, sonra oyuncu
    ///
    /// Semt sahneleri build listesinde <b>değil</b>; Addressables ile
    /// yükleniyorlar (ADR 0011, <see cref="BuildScenes"/>). Addressables
    /// içeriği oyuncu build'inden <b>önce</b> paketlenmezse, oyun açılır,
    /// menü çalışır, şehir yüklenir — ama <b>semtler gelmez</b>. Sahne
    /// boş bir arazi olarak durur ve hata da vermez.
    ///
    /// Bu sessiz olduğu için sıraya güvenmek yetmez: build sonrası çıktı
    /// <b>denetlenir</b> ve eksikse build BAŞARISIZ sayılır.
    /// </summary>
    public static class BuildPipelineEntry
    {
        /// <summary>Build çıktısının kök klasörü (depo dışı sayılır).</summary>
        public const string CiktiKok = "build/windows";

        public const string UrunAdi = "Hezarfen1632";

        /// <summary>
        /// Batchmode giriş noktası.
        ///
        /// Hata durumunda <see cref="EditorApplication.Exit"/> ile
        /// <b>sıfırdan farklı</b> çıkar — CI'nin "geçti" sanması, hiç
        /// build almamaktan kötüdür.
        /// </summary>
        public static void BuildWindows()
        {
            try
            {
                var rapor = Kur(out string ozet);
                Debug.Log($"[Hezarfen] BUILD\n{ozet}");
                if (rapor.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError("[Hezarfen] Build BASARISIZ: "
                                   + rapor.summary.result);
                    Cik(1);
                    return;
                }
                Cik(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Hezarfen] Build patladi: {e}");
                Cik(2);
            }
        }

        private static void Cik(int kod)
        {
            if (Application.isBatchMode) EditorApplication.Exit(kod);
        }

        [MenuItem("Hezarfen/Boru Hatti/Windows build al")]
        public static void BuildMenu()
        {
            var rapor = Kur(out string ozet);
            Debug.Log($"[Hezarfen] BUILD ({rapor.summary.result})\n{ozet}");
        }

        /// <summary>Build'i alır ve özet döndürür.</summary>
        public static BuildReport Kur(out string ozet)
        {
            // 1) SAHNE LISTESI KODDAN. Elle bozulmus olabilir; her build
            //    once duzeltir (BuildScenes'in kendi gerekcesi orada).
            BuildScenes.Apply(out string sahneOzet);

            // 2) ADDRESSABLES ONCE. Sirasi tersse semtler pakete girmez.
            string addrOzet = AddressablesPaketle();

            // CIKTI DEPO KOKUNE, proje klasorune degil.
            //
            // Unity'de `GetCurrentDirectory()` PROJE klasorudur
            // (unity/HezarfenGame), depo koku degil. Ilk build oraya
            // dustu ve 539 MB'lik cikti depo agacinin icinde kaldi —
            // `.gitignore`'daki `build/` onu yakaladigi icin zarar
            // vermedi ama yeri yanlisti.
            string depoKok = Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(), "..", ".."));
            string kok = Path.Combine(depoKok, CiktiKok);
            Directory.CreateDirectory(kok);
            string exe = Path.Combine(kok, UrunAdi + ".exe");

            var secenekler = new BuildPlayerOptions
            {
                scenes = BuildScenes.Wanted,
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            var rapor = BuildPipeline.BuildPlayer(secenekler);
            var s = rapor.summary;

            // 3) CIKTIYI DENETLE — "basarili" demesi yetmez.
            string denetim = Denetle(kok, exe);

            ozet = $"{sahneOzet}\n{addrOzet}\n"
                   + $"Cikti : {exe}\n"
                   + $"Sonuc : {s.result}\n"
                   + $"Boyut : {s.totalSize / (1024f * 1024f):F1} MB\n"
                   + $"Sure  : {s.totalTime}\n"
                   + $"Hata  : {s.totalErrors}, uyari {s.totalWarnings}\n"
                   + denetim;
            return rapor;
        }

        /// <summary>
        /// Addressables içeriğini paketler.
        ///
        /// Ayar yoksa <b>sessizce geçmez</b>: semt sahneleri Addressables
        /// ile yükleniyor ve ayarsız bir build onlarsız çıkardı.
        /// </summary>
        private static string AddressablesPaketle()
        {
            var ayar = AddressableAssetSettingsDefaultObject.Settings;
            if (ayar == null)
                throw new InvalidOperationException(
                    "Addressables ayari YOK — semt sahneleri pakete girmez. "
                    + "(Hezarfen -> GIS -> Semt sahnelerini kur)");

            AddressableAssetSettings.BuildPlayerContent(out var sonuc);
            if (!string.IsNullOrEmpty(sonuc.Error))
                throw new InvalidOperationException(
                    "Addressables paketleme hatasi: " + sonuc.Error);

            int girdi = ayar.groups
                .Where(g => g != null)
                .Sum(g => g.entries.Count);
            return $"Addressables: {girdi} girdi, {sonuc.Duration:F1} s "
                   + $"-> {sonuc.OutputPath}";
        }

        /// <summary>
        /// Çıktıyı denetler. <b>Eksik bir build, hatalı bir build'dir.</b>
        ///
        /// Unity "Succeeded" der ve exe'yi yazar; içinde semt paketi
        /// olmadığını söylemez. Oyuncu boş bir arazide dolaşır ve konsolda
        /// tek satır hata görmez.
        /// </summary>
        private static string Denetle(string kok, string exe)
        {
            var eksik = new System.Collections.Generic.List<string>();

            if (!File.Exists(exe)) eksik.Add("exe yok");

            string veri = Path.Combine(kok, UrunAdi + "_Data");
            if (!Directory.Exists(veri)) eksik.Add("_Data klasoru yok");

            // Addressables cikti klasoru: StreamingAssets altinda.
            string aa = Path.Combine(veri, "StreamingAssets", "aa");
            long aaBoyut = 0;
            if (!Directory.Exists(aa)) eksik.Add("StreamingAssets/aa yok "
                                                 + "(semtler pakete girmemis)");
            else
                foreach (var f in Directory.GetFiles(aa, "*",
                             SearchOption.AllDirectories))
                    aaBoyut += new FileInfo(f).Length;

            string satir = eksik.Count == 0
                ? $"Denetim: GECTI (Addressables {aaBoyut / (1024f * 1024f):F1} MB)"
                : "Denetim: KALDI -> " + string.Join("; ", eksik);

            if (eksik.Count > 0) Debug.LogError("[Hezarfen] " + satir);
            return satir;
        }
    }
}
