using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hezarfen.Gis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// <b>Arazi öznitelik katmanını hesaplar.</b> Gerekçe:
    /// <see cref="AraziOznitelik"/>.
    ///
    /// ## Neyi bina, neyi yol sayıyoruz — ve neden ada bakıyoruz
    ///
    /// Semt sahnesinde her şey <c>Renderer</c>. Yüksekliğe bakarak
    /// ayırmak denenebilirdi ama bahçe duvarı 1,95 m, tek katlı ev
    /// 3,2 m — arada yeterli açıklık yok. Boru hattı bu nesneleri
    /// zaten <b>adlandırıyor</b> ve o adlar üreticinin kendi
    /// sözleşmesi:
    ///
    /// | ad | ne | katman |
    /// |---|---|---|
    /// | <c>PF_House*</c> | ev | bina |
    /// | <c>Cekirdek_*</c> | mescit, kilise, sinagog, kamusal | bina |
    /// | <c>Sokak_Ana</c>, <c>Cikmaz_*</c>, <c>Kaldirim</c> | sokak yüzeyi | yol |
    /// | <c>BahceDuvarlari</c>, <c>Kaideler</c> | duvar, kaide | <b>hiçbiri</b> |
    ///
    /// Bahçe duvarı bilerek dışarıda: bahçe zaten ağaç istenen yerdir,
    /// duvarı bina saymak bahçeyi çorak bırakırdı.
    ///
    /// ## Mesafe alanı: iki geçişli chamfer
    ///
    /// Tohum hücreler 0, ötekiler sonsuz; sonra bir ileri bir geri
    /// tarama. Öklit değil chamfer (3-4 komşuluk) — hatası %2 civarı ve
    /// 0,5 m'lik bayt adımının altında kalıyor, yani ölçüm
    /// çözünürlüğünde <b>fark etmiyor</b>.
    /// </summary>
    public static class AraziOznitelikKur
    {
        private const string TerrainScene =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";
        private const string DistrictDir =
            "Assets/_Project/Scenes/Districts";
        private const string CiktiDir = "Assets/_Project/Data";

        /// <summary>Deniz seviyesi (m). Dünya orijini y=0 deniz seviyesi.</summary>
        private const float DenizKotu = 0.4f;

        [MenuItem("Hezarfen/GIS/Arazi ozniteliklerini hesapla (D_Galata)")]
        public static void Galata() => Hesapla("D_Galata");

        [MenuItem("Hezarfen/GIS/Arazi ozniteliklerini hesapla (tum semtler)")]
        public static void Hepsi()
        {
            foreach (var yol in Directory.GetFiles(DistrictDir, "D_*.unity"))
                Hesapla(Path.GetFileNameWithoutExtension(yol));
        }

        public static AraziOznitelik Hesapla(string semt)
        {
            string yol = $"{DistrictDir}/{semt}.unity";
            if (!File.Exists(yol))
            {
                Debug.LogError($"[Hezarfen] {yol} yok.");
                return null;
            }

            EditorSceneManager.OpenScene(TerrainScene, OpenSceneMode.Single);
            var arazi = Object.FindAnyObjectByType<Terrain>();
            if (arazi == null)
            {
                Debug.LogError("[Hezarfen] Arazi sahnesinde Terrain yok.");
                return null;
            }
            var sahne = EditorSceneManager.OpenScene(yol, OpenSceneMode.Additive);

            // --- 1. SINIR: semtin kendi kapladigi yer ---------------------
            var kutu = new Bounds();
            bool ilk = true;
            foreach (var go in sahne.GetRootGameObjects())
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    if (ilk) { kutu = r.bounds; ilk = false; }
                    else kutu.Encapsulate(r.bounds);
                }
            if (ilk)
            {
                Debug.LogError($"[Hezarfen] {semt}: hic renderer yok.");
                return null;
            }

            // Semt sinirinin biraz disi da hesaplanir: kiyi ve yol
            // etkisi sinirda kesilmesin.
            const float Pay = 40f;
            var kok = new Vector2(kutu.min.x - Pay, kutu.min.z - Pay);
            int en = Mathf.CeilToInt((kutu.size.x + 2 * Pay) / AraziOznitelik.Hucre);
            int boy = Mathf.CeilToInt((kutu.size.z + 2 * Pay) / AraziOznitelik.Hucre);

            var bina = new bool[en * boy];
            var yolT = new bool[en * boy];
            var su = new bool[en * boy];

            // --- 2. TOHUMLAMA ---------------------------------------------
            // RENDERER'DAN YUKARI YURUNUR.
            //
            // Ilk yazimda adi tutan nesnenin kendi Renderer'ina
            // bakiyordum ve sifir bina buldum. Sebep olculdu: prefab
            // ornekleri (`PF_House_A`) Renderer TASIMIYOR — mesh alt
            // nesnede (`SM_House_A_LOD`). Sahnede 758 `PF_AvluDuvarKisa`
            // var ve hicbirinde renderer yok; renderer 1516 tane
            // `SM_AvluDuvarKisa_LOD`da.
            //
            // O yuzden gezinme renderer'dan baslar ve ata zincirinde ilk
            // taniyan ada kadar yukari cikar. Bu ayni zamanda ic ice
            // sayimi da onler: bir ev iki kez boyanmaz.
            int binaN = 0, yolN = 0;
            foreach (var go in sahne.GetRootGameObjects())
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    int tur = TurAtadan(r.transform);
                    if (tur == 0) continue;
                    var hedef = tur == 1 ? bina : yolT;
                    if (Boya(hedef, r.bounds, kok, en, boy))
                    {
                        if (tur == 1) binaN++; else yolN++;
                    }
                }

            // SU: arazi yuksekligi deniz kotunun altindaysa. Kiyi
            // cizgisi Copernicus DEM konturundan turetildi (ADR 0008),
            // yani bu ayni kaynagi okumak demek — ikinci bir sahip yok.
            var pos = arazi.transform.position;
            var data = arazi.terrainData;
            int suN = 0;
            for (int j = 0; j < boy; j++)
                for (int i = 0; i < en; i++)
                {
                    float x = kok.x + (i + 0.5f) * AraziOznitelik.Hucre;
                    float z = kok.y + (j + 0.5f) * AraziOznitelik.Hucre;
                    float u = (x - pos.x) / data.size.x;
                    float v = (z - pos.z) / data.size.z;
                    if (u < 0f || u > 1f || v < 0f || v > 1f) continue;
                    float h = data.GetInterpolatedHeight(u, v) + pos.y;
                    if (h <= DenizKotu) { su[j * en + i] = true; suN++; }
                }

            // --- 3. MESAFE ALANLARI ---------------------------------------
            var varlik = ScriptableObject.CreateInstance<AraziOznitelik>();
            varlik.semt = semt;
            varlik.kok = kok;
            varlik.en = en;
            varlik.boy = boy;
            varlik.binaUzakligi = Mesafe(bina, en, boy);
            varlik.yolUzakligi = Mesafe(yolT, en, boy);
            varlik.suUzakligi = Mesafe(su, en, boy);
            varlik.binaHucre = bina.Count(b => b);
            varlik.yolHucre = yolT.Count(b => b);
            varlik.suHucre = suN;

            if (!AssetDatabase.IsValidFolder(CiktiDir))
                AssetDatabase.CreateFolder("Assets/_Project", "Data");
            string cikti = $"{CiktiDir}/AO_{semt}.asset";
            var eski = AssetDatabase.LoadAssetAtPath<AraziOznitelik>(cikti);
            if (eski != null)
            {
                // Var olan varligin USTUNE yazilir: GUID korunur ve
                // ona bakan referanslar kirilmaz.
                EditorUtility.CopySerialized(varlik, eski);
                EditorUtility.SetDirty(eski);
                varlik = eski;
            }
            else
            {
                AssetDatabase.CreateAsset(varlik, cikti);
            }
            AssetDatabase.SaveAssets();

            var sb = new StringBuilder($"ARAZI OZNITELIK {semt}\n");
            sb.AppendLine($"  izgara {en}x{boy} @ {AraziOznitelik.Hucre} m "
                          + $"= {en * boy} hucre, kok {kok}");
            sb.AppendLine($"  tohum: bina {varlik.binaHucre} hucre "
                          + $"({binaN} nesne), yol {varlik.yolHucre} hucre "
                          + $"({yolN} nesne), su {varlik.suHucre} hucre");
            sb.AppendLine("  " + Dagilim("bina", varlik.binaUzakligi));
            sb.AppendLine("  " + Dagilim("yol ", varlik.yolUzakligi));
            sb.AppendLine("  " + Dagilim("su  ", varlik.suUzakligi));
            sb.AppendLine($"  -> {cikti}");
            Debug.Log("[Hezarfen] " + sb);

            EditorSceneManager.CloseScene(sahne, true);
            return varlik;
        }

        /// <summary>
        /// Bu renderer'in atalarindan ilk taniyan ada gore tur.
        /// 0 ilgisiz · 1 bina · 2 yol.
        /// </summary>
        private static int TurAtadan(Transform t)
        {
            for (var k = t; k != null; k = k.parent)
            {
                int tur = Tur(k.name);
                if (tur != 0) return tur;
            }
            return 0;
        }

        /// <summary>0 ilgisiz · 1 bina · 2 yol.</summary>
        private static int Tur(string ad)
        {
            if (ad.StartsWith("PF_House") || ad.StartsWith("SM_House")
                || ad.StartsWith("Cekirdek_")) return 1;
            if (ad == "Sokak_Ana" || ad == "Kaldirim"
                || ad.StartsWith("Cikmaz_")) return 2;
            return 0;
        }

        /// <summary>
        /// Bir kutuyu ızgaraya boyar. Dönüş: bir şey boyandı mı.
        ///
        /// Kutu dünya ekseninde (<c>Renderer.bounds</c>): yamuk bir ev
        /// biraz fazla yer kaplar. Bu bilerek: fazla boyamak ağacı biraz
        /// uzağa iter, eksik boyamak ağacı duvarın içine sokar. Hangi
        /// yönde yanılacağını seçebiliyorsan güvenli olanı seç.
        /// </summary>
        private static bool Boya(bool[] hedef, Bounds b, Vector2 kok,
                                 int en, int boy)
        {
            int i0 = Mathf.FloorToInt((b.min.x - kok.x) / AraziOznitelik.Hucre);
            int i1 = Mathf.CeilToInt((b.max.x - kok.x) / AraziOznitelik.Hucre);
            int j0 = Mathf.FloorToInt((b.min.z - kok.y) / AraziOznitelik.Hucre);
            int j1 = Mathf.CeilToInt((b.max.z - kok.y) / AraziOznitelik.Hucre);
            bool oldu = false;
            for (int j = Mathf.Max(0, j0); j <= Mathf.Min(boy - 1, j1); j++)
                for (int i = Mathf.Max(0, i0); i <= Mathf.Min(en - 1, i1); i++)
                {
                    hedef[j * en + i] = true;
                    oldu = true;
                }
            return oldu;
        }

        /// <summary>İki geçişli chamfer mesafe dönüşümü → 0,5 m adımlı bayt.</summary>
        private static byte[] Mesafe(bool[] tohum, int en, int boy)
        {
            const float D = 1f, K = 1.41421f;   // dik ve capraz komsu
            float sonsuz = 1e9f;
            var d = new float[en * boy];
            for (int i = 0; i < d.Length; i++) d[i] = tohum[i] ? 0f : sonsuz;

            for (int j = 0; j < boy; j++)
                for (int i = 0; i < en; i++)
                {
                    int k = j * en + i;
                    if (d[k] == 0f) continue;
                    if (i > 0) d[k] = Mathf.Min(d[k], d[k - 1] + D);
                    if (j > 0) d[k] = Mathf.Min(d[k], d[k - en] + D);
                    if (i > 0 && j > 0) d[k] = Mathf.Min(d[k], d[k - en - 1] + K);
                    if (i < en - 1 && j > 0) d[k] = Mathf.Min(d[k], d[k - en + 1] + K);
                }
            for (int j = boy - 1; j >= 0; j--)
                for (int i = en - 1; i >= 0; i--)
                {
                    int k = j * en + i;
                    if (d[k] == 0f) continue;
                    if (i < en - 1) d[k] = Mathf.Min(d[k], d[k + 1] + D);
                    if (j < boy - 1) d[k] = Mathf.Min(d[k], d[k + en] + D);
                    if (i < en - 1 && j < boy - 1) d[k] = Mathf.Min(d[k], d[k + en + 1] + K);
                    if (i > 0 && j < boy - 1) d[k] = Mathf.Min(d[k], d[k + en - 1] + K);
                }

            var cikti = new byte[en * boy];
            for (int i = 0; i < d.Length; i++)
            {
                // METREYE ONCE KIRPILIR, SONRA YUVARLANIR.
                //
                // Tersi felaketti ve sessizdi: tohum yokken mesafe
                // 1e9 hucre kalir, metreye cevrilince 4e9 olur ve
                // `RoundToInt` int'i tasirir — sonuc negatif cikip
                // Clamp(...,0,127) ile **0**'a duser. Yani "hicbir bina
                // yok" durumu, "her hucre binanin ICINDE" diye okunur.
                // Ilk kosuda tam bunu gordum: bina %100 "icinde".
                float m = Mathf.Min(d[i] * AraziOznitelik.Hucre,
                                    AraziOznitelik.Uzak);
                cikti[i] = (byte)Mathf.Clamp(
                    Mathf.RoundToInt(m / AraziOznitelik.Adim), 0, 127);
            }
            return cikti;
        }

        private static string Dagilim(string ad, byte[] k)
        {
            if (k == null || k.Length == 0) return ad + ": bos";
            int[] kova = new int[5];   // 0 · <4 · <12 · <32 · uzak
            foreach (var b in k)
            {
                float m = b * AraziOznitelik.Adim;
                if (m <= 0.01f) kova[0]++;
                else if (m < 4f) kova[1]++;
                else if (m < 12f) kova[2]++;
                else if (m < 32f) kova[3]++;
                else kova[4]++;
            }
            float n = k.Length;
            return $"{ad} uzaklik: icinde %{100f * kova[0] / n:0.0} · "
                   + $"<4m %{100f * kova[1] / n:0.0} · "
                   + $"<12m %{100f * kova[2] / n:0.0} · "
                   + $"<32m %{100f * kova[3] / n:0.0} · "
                   + $"uzak %{100f * kova[4] / n:0.0}";
        }
    }
}
