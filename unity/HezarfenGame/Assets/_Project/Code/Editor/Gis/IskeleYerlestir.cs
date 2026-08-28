using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hezarfen.Sehir;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// <b>İskeleleri kıyıdan TÜRETİR.</b>
    ///
    /// 1632'de kayık ve pereme ana ulaşımdır ve iskeleler tarifelidir
    /// (RESEARCH §6). Evliya Unkapanı ve Balıkpazarı iskelelerini faal
    /// sayar, Kasımpaşa Tersanesi çalışır, Karaköy Kapısı kıyıya açılır.
    /// Yani iskelelerin <b>varlığı</b> belgelidir.
    ///
    /// <b>Konumları değil.</b> Hiçbir kaynak metrik koordinat vermez ve
    /// CLAUDE.md'nin kuralı net: kaynak niteliksel olduğunda metrik
    /// geometri uydurulmaz. O yüzden konum <b>türetiliyor</b> — Üsküdar
    /// iskelesi için kullanılan yöntemin aynısıyla (RESEARCH §5.4):
    ///
    /// <list type="number">
    /// <item>Bir <b>çapa</b> seçilir: belgeli bir yapı (Yeni Cami harabesi,
    ///       Galata Kulesi) ya da ölçülmüş bir yerleşim merkezi.</item>
    /// <item>Çapadan çevreye ışın atılır; arazinin deniz seviyesini en
    ///       çabuk geçtiği yön <b>denize doğru</b>dur.</item>
    /// <item>Kıyı çizgisi bulunur, oradan denize açılır.</item>
    /// <item>Yön kıyının <b>yerel normalidir</b> — iskele kıyıya diktir.
    ///       "En alçak arazi yönü" burada yetmez: iskele zaten suyun
    ///       içindedir ve en derin yön kıyı boyunca çıkabilir.</item>
    /// </list>
    ///
    /// Sonuç <b>T2, taslak</b>. Türetme uydurmadan farklıdır: yöntemi
    /// yazılıdır, girdisi belgelidir, ve çapa değişirse sonuç değişir.
    /// </summary>
    public static class IskeleYerlestir
    {
        private const string PrefabYol = "Assets/_Project/Art/Prefabs/PF_Iskele.prefab";
        private const string KokAd = "ISKELELER_1632";

        /// <summary>Kıyıdan denize bu kadar açılır (m).</summary>
        private const float DenizeAcilma = 20f;

        /// <summary>Kıyı aranırken en fazla bu kadar uzağa bakılır (m).</summary>
        private const float MaxArama = 1400f;

        /// <summary>Işın örnekleme adımı (m).</summary>
        private const float Adim = 8f;

        /// <summary>Deniz seviyesi (ADR 0007: y=0).</summary>
        private const float DenizKotu = 0f;

        /// <summary>(ad, çapa açıklaması, çapa nesnesi ya da semt).</summary>
        private static readonly (string ad, string capa, string semt)[] Iskeleler =
        {
            ("Eminonu",   "PF_YeniCamiHarabe", null),
            ("Karakoy",   "PF_GalataKulesi",   null),
            ("Kasimpasa", null,                "D_Galata"),
            ("Eyup",      null,                "D_Eyup"),
            ("Unkapani",  null,                "D_Surici_Bati"),
        };

        [MenuItem("Hezarfen/GIS/Iskeleleri yerlestir")]
        public static void Yerlestir()
        {
            var tgo = GameObject.Find("TR_Istanbul");
            var terrain = tgo != null ? tgo.GetComponent<Terrain>() : null;
            if (terrain == null)
            {
                Debug.LogError("[Hezarfen] TR_Istanbul yok — arazi sahnesini ac.");
                return;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabYol);
            if (prefab == null)
            {
                Debug.LogError($"[Hezarfen] {PrefabYol} yok. Once: "
                    + "gen_iskele_ve_alay.py --out-dir ... , sonra boru hatti.");
                return;
            }

            var eski = GameObject.Find(KokAd);
            if (eski != null) Object.DestroyImmediate(eski);
            var kok = new GameObject(KokAd);

            var graf = AssetDatabase.LoadAssetAtPath<SokakGrafi>(
                "Assets/_Project/Data/SG_Sehir.asset");
            var sb = new StringBuilder("ISKELELER");
            int kuruldu = 0;

            foreach (var (ad, capaAd, semt) in Iskeleler)
            {
                if (!Capa(capaAd, semt, graf, out Vector3 capa, out string nasil))
                {
                    sb.AppendLine($"  {ad}: capa bulunamadi ({capaAd ?? semt})");
                    continue;
                }

                if (!Kiyi(terrain, capa, out Vector3 kiyi, out Vector2 disari,
                          out float mesafe))
                {
                    sb.AppendLine($"  {ad}: {MaxArama:0} m icinde kiyi yok");
                    continue;
                }

                Vector3 yer = kiyi + new Vector3(disari.x, 0f, disari.y)
                              * DenizeAcilma;
                yer.y = DenizKotu;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = $"PF_Iskele_{ad}";
                go.transform.SetParent(kok.transform, false);
                go.transform.position = yer;
                // Iskele kiyiya DIK uzanir: +Z disariya (denize) baksin.
                go.transform.rotation = Quaternion.LookRotation(
                    new Vector3(disari.x, 0f, disari.y), Vector3.up);

                float aci = Mathf.Atan2(disari.x, disari.y) * Mathf.Rad2Deg;
                if (aci < 0) aci += 360f;
                sb.AppendLine($"  {ad,-10} capa {nasil}, kiyi {mesafe:0} m, "
                              + $"yon {aci:0.0}° -> ({yer.x:0},{yer.z:0})");
                kuruldu++;
            }

            sb.AppendLine($"  {kuruldu}/{Iskeleler.Length} iskele kuruldu");
            sb.AppendLine("  T2/taslak: VARLIK belgeli, KONUM turetilmis.");
            Selection.activeGameObject = kok;
            if (kuruldu == Iskeleler.Length) Debug.Log("[Hezarfen] " + sb);
            else Debug.LogWarning("[Hezarfen] " + sb);
        }

        /// <summary>Çapa noktası: landmark ya da semtin düğüm merkezi.</summary>
        private static bool Capa(string capaAd, string semt, SokakGrafi graf,
                                 out Vector3 nokta, out string nasil)
        {
            nokta = Vector3.zero;
            nasil = "";
            if (!string.IsNullOrEmpty(capaAd))
            {
                foreach (var t in Object.FindObjectsByType<Transform>(
                             FindObjectsInactive.Exclude))
                    if (t.name.StartsWith(capaAd, System.StringComparison.Ordinal))
                    { nokta = t.position; nasil = capaAd; return true; }
                return false;
            }
            if (graf == null || string.IsNullOrEmpty(semt)) return false;

            // Semtin dugum merkezi — Kasimpasa icin bu OLCULMUS bir sey:
            // grafin kopuk cebi. Cografyanin ayirdigi yerin kendi merkezi.
            var kk = graf.dugumler.Where(d => d.semt == semt)
                                  .Select(d => d.konum).ToList();
            if (kk.Count == 0) return false;

            if (semt == "D_Galata")
            {
                // Kasimpasa: kulenin BATISINDAKI dugumler. Graf olcumu
                // kopuk cebi (-1330, -479) civarinda buldu; burada o
                // bolgenin kendi merkezi aliniyor.
                var bati = kk.Where(k => k.x < -800f).ToList();
                if (bati.Count == 0) return false;
                kk = bati;
            }
            nokta = kk.Aggregate(Vector3.zero, (a, b) => a + b) / kk.Count;
            nasil = $"{semt} dugum merkezi ({kk.Count} dugum)";
            return true;
        }

        /// <summary>
        /// Çapadan en yakın kıyı noktası ve <b>dışarı</b> (denize) yönü.
        ///
        /// Otuz altı yöne ışın atılır; arazinin deniz seviyesini en çabuk
        /// geçtiği yön kazanır. Tek bir yön varsaymak (örneğin "güneye
        /// bak") Haliç'in iki yakasında ters çalışırdı.
        /// </summary>
        private static bool Kiyi(Terrain terrain, Vector3 capa,
                                 out Vector3 kiyi, out Vector2 disari,
                                 out float mesafe)
        {
            kiyi = Vector3.zero;
            disari = Vector2.zero;
            mesafe = float.MaxValue;
            Vector3 o = terrain.transform.position;

            for (int i = 0; i < 36; i++)
            {
                float a = i * 10f * Mathf.Deg2Rad;
                var yon = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                float onceH = terrain.SampleHeight(capa) + o.y;
                for (float s = Adim; s <= MaxArama; s += Adim)
                {
                    var p = capa + new Vector3(yon.x, 0f, yon.y) * s;
                    float h = terrain.SampleHeight(p) + o.y;
                    if (h <= DenizKotu && onceH > DenizKotu)
                    {
                        if (s < mesafe)
                        {
                            mesafe = s;
                            // Kiyi noktasi: iki ornek arasinda dogrusal.
                            float t = onceH / Mathf.Max(0.001f, onceH - h);
                            kiyi = capa + new Vector3(yon.x, 0f, yon.y)
                                   * (s - Adim + Adim * t);
                            kiyi.y = DenizKotu;
                            disari = yon;
                        }
                        break;
                    }
                    onceH = h;
                }
            }
            return mesafe < float.MaxValue;
        }
    }
}
