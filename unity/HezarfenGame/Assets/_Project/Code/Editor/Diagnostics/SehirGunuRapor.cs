using System.Linq;
using System.Text;
using Hezarfen.Sehir;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Şehrin bir gününü yazdırır.</b>
    ///
    /// Testler "öğlende mescide akış var" der ve yeşil yanar; bu araç o
    /// akışın <b>ne kadar</b> olduğunu gösterir. İkisi ayrı iş: test bir
    /// eşiği korur, rapor tabloyu okutur. Bir tasarım kararı vereceksek
    /// (esnaf payı çok mu, ases az mı) bakılacak yer burası.
    /// </summary>
    public static class SehirGunuRapor
    {
        [MenuItem("Hezarfen/Olcum/Sehrin gununu olc")]
        public static void Olc()
        {
            var graf = AssetDatabase.LoadAssetAtPath<SokakGrafi>(
                "Assets/_Project/Data/SG_Sehir.asset");
            if (graf == null)
            {
                Debug.LogError("[Hezarfen] Sokak grafi yok.");
                return;
            }
            var meslekler = AssetDatabase.FindAssets("t:NPCMeslek")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<NPCMeslek>)
                .Where(m => m != null).ToList();
            if (meslekler.Count == 0)
            {
                Debug.LogError("[Hezarfen] Meslek cizelgesi yok.");
                return;
            }

            const int adet = 2000;
            var sakinler = SehirGunu.Sakinler(graf, meslekler, adet);
            var sb = new StringBuilder(
                $"SEHRIN GUNU — {adet} sakin, {meslekler.Count} meslek\n");

            foreach (var (yil, gun, etiket) in new[]
            {
                (1632, 121, "1 Mayis 1632 (kahvehaneler ACIK)"),
                (1634, 121, "1 Mayis 1634 (kahvehaneler KAPALI)"),
            })
            {
                sb.AppendLine($"  --- {etiket} ---");
                sb.AppendLine("  vakit      disarida  mescitte  ulasilamaz  "
                              + "en cok gidilen");
                foreach (var o in SehirGunu.Gun(graf, sakinler, yil, gun))
                {
                    var enCok = o.hedefler.OrderByDescending(k => k.Value)
                        .Take(3)
                        .Select(k => $"{k.Key} {k.Value}");
                    sb.AppendLine($"  {o.vakit,-10} "
                                  + $"%{o.DisariOrani * 100,5:0.0}   "
                                  + $"%{o.MescitOrani * 100,5:0.0}   "
                                  + $"{o.ulasilamaz,8}  "
                                  + string.Join(", ", enCok));
                }
            }

            // Meslek dagilimi — pay oranlari gercekten tuttu mu.
            sb.AppendLine("  --- meslek dagilimi ---");
            foreach (var grup in sakinler.GroupBy(s => s.meslek.tip)
                         .OrderByDescending(g => g.Count()))
                sb.AppendLine($"    {grup.Key,-12} {grup.Count(),4} "
                              + $"(%{grup.Count() * 100f / adet:0.0})");

            Debug.Log("[Hezarfen] " + sb);
        }
    }
}
