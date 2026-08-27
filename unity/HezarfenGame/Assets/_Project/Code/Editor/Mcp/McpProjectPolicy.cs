using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Mcp
{
    /// <summary>
    /// MCP for Unity köprüsünün proje politikasını uygular.
    ///
    /// Neden script? Bu ayarlar Unity'nin EditorPrefs'inde yaşar — yani proje dosyalarında
    /// değil, makinede. Elle tıklanırsa "sadece sohbette var olan ayar" olur ve CLAUDE.md
    /// bunu yasaklıyor. Buradan uygulanınca hem tekrar üretilebilir hem denetlenebilir olur.
    ///
    /// Batchmode'dan:
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod Hezarfen.Editor.Mcp.McpProjectPolicy.Apply
    /// </summary>
    public static class McpProjectPolicy
    {
        // Anahtarlar paketin EditorPrefKeys sinifindan birebir alindi (v10.1.2).
        private const string TelemetryDisabledKey = "MCPForUnity.TelemetryDisabled";
        private const string ClientProjectDirKey = "MCPForUnity.ClientProjectDir";
        private const string UseHttpTransportKey = "MCPForUnity.UseHttpTransport";

        /// <summary>
        /// Claude Code'un calisma dizini. Unity projesi bunun ALT klasorunde
        /// (unity/HezarfenGame) oldugu icin override sart: paket varsayilan olarak
        /// Unity proje klasorunu kullanir ve kayit yanlis kapsama yazilir.
        /// </summary>
        private const string ClaudeWorkspaceDir = @"D:\ClaudeCodeProjects\Hezarfen_Oyunu";

        [MenuItem("Hezarfen/MCP/Proje politikasini uygula")]
        public static void Apply()
        {
            // 1. Telemetri kapali. Gerekce: ADR 0001 — sahne/varlik/prompt verisi
            //    yayinlanmamis oyun icerigi. Paket varsayilani ACIK geliyor.
            EditorPrefs.SetBool(TelemetryDisabledKey, true);

            // 2. Tasima = stdio (paket varsayilani HTTP Local:8080).
            //    Gerekce: .mcp.json'da sunucuyu Claude Code'un kendisi baslatiyor
            //    (uvx --from mcpforunityserver==10.1.2 mcp-for-unity). Stdio'da surec
            //    omru istemcide, ayri port/oturum yonetimi yok. Paketin kendi CI
            //    onyukleyicisi (McpCiBoot) de ayni tercihi yapiyor.
            EditorPrefs.SetBool(UseHttpTransportKey, false);

            // 3. Istemci kayit dizini = Claude Code workspace koku.
            if (Directory.Exists(ClaudeWorkspaceDir))
            {
                EditorPrefs.SetString(ClientProjectDirKey, ClaudeWorkspaceDir);
            }
            else
            {
                Debug.LogWarning(
                    $"[Hezarfen] Claude workspace bulunamadi: {ClaudeWorkspaceDir}. " +
                    "Depo tasindiysa McpProjectPolicy.ClaudeWorkspaceDir guncellenmeli.");
            }

            Debug.Log(
                "[Hezarfen] MCP politikasi uygulandi | " +
                $"telemetri kapali={EditorPrefs.GetBool(TelemetryDisabledKey, false)} | " +
                $"http tasima={EditorPrefs.GetBool(UseHttpTransportKey, true)} (false=stdio) | " +
                $"istemci dizini={EditorPrefs.GetString(ClientProjectDirKey, "(bos)")}");
        }

        /// <summary>Mevcut durumu konsola yazar — dogrulama icin.</summary>
        [MenuItem("Hezarfen/MCP/Politika durumunu goster")]
        public static void Report()
        {
            Debug.Log(
                "[Hezarfen] MCP politika durumu | " +
                $"telemetri kapali={EditorPrefs.GetBool(TelemetryDisabledKey, false)} | " +
                $"http tasima={EditorPrefs.GetBool(UseHttpTransportKey, true)} (false=stdio) | " +
                $"istemci dizini={EditorPrefs.GetString(ClientProjectDirKey, "(ayarlanmamis)")}");
        }
    }
}
