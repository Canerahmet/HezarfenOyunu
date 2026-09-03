using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Editor.Lighting
{
    /// <summary>
    /// <b>Prob verisi akışını açar</b> — 10 km'lik bir şehir GPU'ya
    /// bir kerede sığmaz.
    ///
    /// ## Ölçüm
    ///
    /// Tur kareleri ölçüldü: Sûriçi'nde gölgedeki zemin
    /// <c>(36, 15, 0)</c>, yani <b>mavi kanal sıfır</b>. Aynı turda,
    /// aynı pozla çekilen Galata sokağının gölgesi 0,755. Gölge her iki
    /// yerde de gölge; ikisini ayıran şey <b>yer</b>.
    ///
    /// Fırının gökyüzünü bağladım (<see cref="KaliciAydinlatma.GokAyari"/>),
    /// 2,8 milyon probu yeniden pişirdim ve kareyi tekrar ölçtüm:
    /// <b>(36,2 / 14,6 / 0,0) — bayt bayt aynı</b>. Düzeltme gerekliydi
    /// ama sebep o değildi; ve bunu söyleyen şey yeni ölçümdü.
    ///
    /// Sebep koşum günlüğünde tek satır hâlinde duruyordu:
    ///
    /// <i>"Max Memory Budget for Adaptive Probe Volumes has been reached,
    /// but there is still more data to load."</i>
    ///
    /// Diskteki fırın 98 MB hücre + 98 MB isteğe bağlı + 197 MB destek
    /// verisi; bütçe ise 1024. Şehrin <b>bir kısmı</b> yükleniyor,
    /// gerisi hiç yüklenmiyor ve yüklenmeyen yerde dolaylı ışık
    /// <b>yok</b> — sıfır, karanlık değil. Galata'nın düzgün görünmesi
    /// bir tesadüf değil: yüklenen hücreler oradaydı.
    ///
    /// ## Neden akış, neden bütçe değil
    ///
    /// Bütçeyi büyütmek 8 GB'lık bir karta 400 MB prob yüklemeye
    /// çalışmak olurdu ve şehir daha da büyüyecek. APV'nin bu iş için
    /// kendi mekanizması var: hücreler kameranın çevresinde
    /// <b>akar</b> (diskten belleğe, bellekten GPU'ya). Uçuş oyununda
    /// kamera 3 km yükselip şehrin öbür ucuna gidiyor; sabit bir
    /// yükleme bunu zaten karşılayamaz.
    ///
    /// ## Neden üç varlığın üçü de
    ///
    /// Kalite seviyeleri kendi HDRP varlıklarını taşıyor ve etkin olan
    /// <b>Balanced</b>. Yalnız onu açmak, bir sonraki kalite
    /// değişiminde aynı karanlığı geri getirirdi. Ölçü de üçünü birden
    /// okur (<see cref="AkisKapaliOlanlar"/>).
    /// </summary>
    public static class ProbAkisi
    {
        /// <summary>APV kullanan varlıklarda akış açık mı — testin ölçüsü.</summary>
        public static List<string> AkisKapaliOlanlar()
        {
            var eksik = new List<string>();
            foreach (var varlik in Varliklar())
            {
                var so = new SerializedObject(varlik);
                if (!ApvKullaniyor(so)) continue;
                var gpu = so.FindProperty(
                    "m_RenderPipelineSettings.supportProbeVolumeGPUStreaming");
                var disk = so.FindProperty(
                    "m_RenderPipelineSettings.supportProbeVolumeDiskStreaming");
                if (gpu == null || disk == null)
                {
                    eksik.Add($"{varlik.name} (alan bulunamadi)");
                    continue;
                }
                if (!gpu.boolValue) eksik.Add($"{varlik.name}: GPU akisi kapali");
                if (!disk.boolValue) eksik.Add($"{varlik.name}: disk akisi kapali");
            }
            return eksik;
        }

        [MenuItem("Hezarfen/Aydinlatma/Prob akisini ac")]
        public static void AcMenu()
        {
            int n = Ac(out string rapor);
            Debug.Log($"[Hezarfen] Prob akisi: {n} varlik guncellendi.\n{rapor}");
        }

        public static int Ac(out string rapor)
        {
            int n = 0;
            var satirlar = new List<string>();
            foreach (var varlik in Varliklar())
            {
                var so = new SerializedObject(varlik);
                if (!ApvKullaniyor(so))
                {
                    satirlar.Add($"{varlik.name}: APV kullanmiyor, atlandi.");
                    continue;
                }
                bool degisti = false;
                degisti |= Yaz(so,
                    "m_RenderPipelineSettings.supportProbeVolumeGPUStreaming");
                degisti |= Yaz(so,
                    "m_RenderPipelineSettings.supportProbeVolumeDiskStreaming");
                if (degisti)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(varlik);
                    n++;
                    satirlar.Add($"{varlik.name}: akis ACILDI.");
                }
                else
                {
                    satirlar.Add($"{varlik.name}: zaten acikti.");
                }
            }
            AssetDatabase.SaveAssets();
            rapor = string.Join("\n", satirlar);
            return n;
        }

        private static bool Yaz(SerializedObject so, string yol)
        {
            var p = so.FindProperty(yol);
            if (p == null || p.boolValue) return false;
            p.boolValue = true;
            return true;
        }

        /// <summary>
        /// Varlık APV kullanıyor mu (<c>lightProbeSystem == 1</c>).
        ///
        /// Eski prob sistemini kullanan bir varlıkta akış alanları
        /// hiçbir şey yapmaz; onu "eksik" saymak testi yalancı yapardı.
        /// </summary>
        private static bool ApvKullaniyor(SerializedObject so)
        {
            var p = so.FindProperty(
                "m_RenderPipelineSettings.lightProbeSystem");
            return p != null && p.intValue == 1;
        }

        private static IEnumerable<HDRenderPipelineAsset> Varliklar()
        {
            foreach (string guid in AssetDatabase.FindAssets(
                         "t:HDRenderPipelineAsset"))
            {
                string yol = AssetDatabase.GUIDToAssetPath(guid);
                var a = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(yol);
                if (a != null) yield return a;
            }
        }
    }
}
