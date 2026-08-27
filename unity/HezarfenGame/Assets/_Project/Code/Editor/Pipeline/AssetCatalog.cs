using System;
using System.Collections.Generic;
using System.IO;
using Hezarfen.Core;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// Blender jeneratörlerinin yazdığı <c>art/blend/**/catalog.json</c>
    /// dosyalarını okur ve varlık adını <see cref="HistoricalTag"/> değerlerine
    /// bağlar.
    ///
    /// Neden gerekli: <see cref="ImportLanding"/> prefab'ı HER koşuşta sıfırdan
    /// yazar. Etiket elle konursa ilk yeniden üretimde sessizce kaybolur ve
    /// prefab <c>Graybox</c>'a düşer — CLAUDE.md'nin "her sahne öğesine
    /// HistoricalTag" kuralı böylece kâğıt üstünde kalır. Bu yüzden kademe ve
    /// kaynak notu, biçimi üreten scriptin yanında, katalogda durur; Unity
    /// yalnızca okur.
    ///
    /// Katalogda karşılığı olmayan model <c>Graybox</c> kalır ama bu
    /// <b>loglanır</b> — sessizce doğru görünmesindense gürültülü biçimde
    /// eksik görünmesi yeğdir.
    /// </summary>
    public static class AssetCatalog
    {
        public readonly struct Entry
        {
            public readonly HistoricalTier Tier;
            public readonly string Source;
            /// <summary>Jeneratörün yazdığı tür: "ev", "agac", "mezar", "han"…</summary>
            public readonly string Kind;
            public Entry(HistoricalTier tier, string source, string kind)
            { Tier = tier; Source = source; Kind = kind; }
        }

        /// <summary>
        /// Varlık bir <b>yapı</b> mı (ağaç/mezar taşı gibi bitki ve donatı
        /// değil)?
        ///
        /// Neden katalogdan soruluyor: "hiçbir köşesi arazinin altında
        /// kalmasın" kuralı yapılar içindir — yapı en yüksek köşeye oturur,
        /// altındaki boşluk taş kaideyle dolar. Ağacın kaidesi yoktur: gövdesi
        /// tek noktada zemine değer, tacı havada durur ve tacın sınır kutusu
        /// yamaçta doğal olarak araziye girer. Muafiyeti burada, ad listesiyle
        /// değil <b>üreticinin kendi beyanıyla</b> vermek şart — elle tutulan
        /// muafiyet listesi zamanla mutlaka yalancı olur (ADR 0020 §4).
        /// </summary>
        public static bool IsBuilding(string assetName)
        {
            if (!TryGet(assetName, out var e)) return true;   // bilinmiyorsa denetle
            string k = (e.Kind ?? "").ToLowerInvariant();
            return !k.StartsWith("agac") && !k.StartsWith("mezar");
        }

        private static Dictionary<string, Entry> _byAsset;

        /// <summary>Depo kökü: Assets → HezarfenGame → unity → kök.</summary>
        public static string RepoRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));

        public static string CatalogRoot => Path.Combine(RepoRoot, "art", "blend");

        public static void Invalidate() => _byAsset = null;

        public static bool TryGet(string assetName, out Entry entry)
        {
            Load();
            return _byAsset.TryGetValue(assetName, out entry);
        }

        public static int Count { get { Load(); return _byAsset.Count; } }

        private static void Load()
        {
            if (_byAsset != null) return;
            _byAsset = new Dictionary<string, Entry>(StringComparer.Ordinal);

            if (!Directory.Exists(CatalogRoot)) return;
            foreach (string path in Directory.GetFiles(CatalogRoot, "catalog.json",
                                                      SearchOption.AllDirectories))
            {
                CatalogFile file;
                try
                {
                    file = JsonUtility.FromJson<CatalogFile>(File.ReadAllText(path));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Hezarfen] Katalog okunamadi: {path} ({e.Message})");
                    continue;
                }
                if (file?.variants == null) continue;

                foreach (var v in file.variants)
                {
                    if (string.IsNullOrEmpty(v.name)) continue;
                    // Ayni ad iki katalogda olamaz: model dosyasi da tek olurdu.
                    if (_byAsset.ContainsKey(v.name))
                    {
                        Debug.LogWarning($"[Hezarfen] Katalogda cift kayit: {v.name}");
                        continue;
                    }
                    _byAsset[v.name] = new Entry(ParseTier(v.tier), v.source ?? "",
                                                 v.kind ?? "");
                }
            }
        }

        private static HistoricalTier ParseTier(string tier)
        {
            switch ((tier ?? "").Trim().ToUpperInvariant())
            {
                case "T1": return HistoricalTier.Documented;
                case "T2": return HistoricalTier.Reconstruction;
                case "T3": return HistoricalTier.Legend;
                default: return HistoricalTier.Graybox;
            }
        }

        [Serializable]
        private class CatalogFile { public List<CatalogEntry> variants; }

        [Serializable]
        private class CatalogEntry
        {
            public string name;
            public string tier;
            public string source;
            public string kind;
        }
    }
}
