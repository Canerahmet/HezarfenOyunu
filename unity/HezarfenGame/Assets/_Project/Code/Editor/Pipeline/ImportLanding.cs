using System.Collections.Generic;
using System.IO;
using Hezarfen.Core;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// <c>Assets/_Import/</c> iniş alanındaki FBX'leri kalıcı yerine taşır ve
    /// oynatılabilir prefab'a çevirir.
    ///
    /// Neden ayrı bir adım: CLAUDE.md "_Import sadece iniş alanı" der. Blender her
    /// koşuşunda oraya yazar; orası her an silinebilir olmalı. Varlığın kalıcı
    /// kimliği (GUID, prefab, sahne referansları) <c>_Project/Art/</c> altındadır.
    /// Taşımayı AssetDatabase yapar, böylece GUID korunur ve sahnedeki referanslar
    /// kopmaz — dosyayı Explorer'dan taşımak tam olarak bunu kırar.
    ///
    /// Prefab kurulumu FBX'ten üretilemeyecek üç şeyi ekler:
    ///   1. UCX_ mesh'inden convex MeshCollider (FBX collider taşımaz),
    ///   2. <see cref="HistoricalTag"/> (CLAUDE.md: her sahne öğesine zorunlu),
    ///   3. UCX yardımcı nesnesinin temizliği.
    /// LODGroup'u Unity, <c>_LOD0/_LOD1</c> adlandırmasından kendisi kurar.
    /// </summary>
    public static class ImportLanding
    {
        public const string LandingDir = "Assets/_Import";
        public const string ModelDir = "Assets/_Project/Art/Models";
        public const string PrefabDir = "Assets/_Project/Art/Prefabs";

        [MenuItem("Hezarfen/Boru Hatti/_Import'u yerlestir ve prefab uret")]
        public static void PromoteAllMenu()
        {
            int n = PromoteAll();
            Debug.Log($"[Hezarfen] Boru hatti: {n} model yerlestirildi.");
        }

        /// <summary>Batchmode'dan da çağrılabilir. Dönüş: işlenen model sayısı.</summary>
        public static int PromoteAll()
        {
            if (!AssetDatabase.IsValidFolder(LandingDir)) return 0;

            // Katalog Unity disinda (Blender kosusunda) degismis olabilir.
            AssetCatalog.Invalidate();
            EnsureFolder(ModelDir);
            EnsureFolder(PrefabDir);

            var moved = new List<string>();
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { LandingDir }))
                {
                    string src = AssetDatabase.GUIDToAssetPath(guid);
                    string dst = $"{ModelDir}/{Path.GetFileName(src)}";

                    if (src == dst) { moved.Add(dst); continue; }

                    // Ayni adli varlik varsa USTUNE yazmiyoruz: eski varligin GUID'i
                    // sahnelerde referansli olabilir. Once eskiyi silip yenisini
                    // tasimak GUID'i degistirir ve referanslari koparir. Bunun
                    // yerine dosya icerigini degistirip yeniden import ediyoruz.
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(dst) != null)
                    {
                        File.Copy(ToSystemPath(src), ToSystemPath(dst), overwrite: true);
                        AssetDatabase.DeleteAsset(src);
                        AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
                        moved.Add(dst);
                        continue;
                    }

                    string err = AssetDatabase.MoveAsset(src, dst);
                    if (!string.IsNullOrEmpty(err))
                    {
                        Debug.LogError($"[Hezarfen] Tasima basarisiz: {src} -> {dst}: {err}");
                        continue;
                    }
                    moved.Add(dst);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            foreach (string path in moved)
                BuildPrefab(path);

            AssetDatabase.SaveAssets();
            return moved.Count;
        }

        /// <summary>Bir model varlığından oynatılabilir prefab üretir. Dönüş: prefab yolu.</summary>
        public static string BuildPrefab(string modelPath)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogError($"[Hezarfen] Model okunamadi: {modelPath}");
                return null;
            }

            EnsureFolder(PrefabDir);

            string baseName = Path.GetFileNameWithoutExtension(modelPath);
            if (baseName.StartsWith("SM_")) baseName = baseName.Substring(3);
            string prefabPath = $"{PrefabDir}/PF_{baseName}.prefab";

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.name = $"PF_{baseName}";

            try
            {
                // FBX'ten gelen prefab bagini kopar: prefab'a bilesen ekleyecegiz,
                // model prefab'i degistirilemez (immutable).
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely,
                                                   InteractionMode.AutomatedAction);

                AttachCollider(inst);

                // UCX nesnesi silindikten sonra LOD sinirlari bayat kalir;
                // yeniden hesaplanmazsa LOD gecisleri yanlis mesafede tetiklenir.
                var lods = inst.GetComponent<LODGroup>();
                if (lods != null) lods.RecalculateBounds();

                // Tarihsel kademe KATALOGDAN gelir, elle konmaz.
                //
                // Bu prefab her boru hatti kosusunda sifirdan yazilir; elle
                // konan etiket ilk yeniden uretimde sessizce Graybox'a duserdi.
                // Karsiligi olmayan model Graybox kalir ama LOGLANIR — sessizce
                // dogru gorunmesindense gurultuyle eksik gorunmesi yegdir.
                var tag = inst.GetComponent<HistoricalTag>() ?? inst.AddComponent<HistoricalTag>();
                if (AssetCatalog.TryGet(baseName, out var entry))
                {
                    tag.tier = entry.Tier;
                    tag.sourceNote = entry.Source;
                }
                else
                {
                    tag.tier = HistoricalTier.Graybox;
                    tag.sourceNote = "";
                    Debug.LogWarning($"[Hezarfen] {baseName}: katalogda kayit yok, "
                                     + "HistoricalTag Graybox kaldi "
                                     + "(uretici scriptin katalogunu kontrol et).");
                }

                var saved = PrefabUtility.SaveAsPrefabAsset(inst, prefabPath, out bool ok);
                if (!ok || saved == null)
                {
                    Debug.LogError($"[Hezarfen] Prefab yazilamadi: {prefabPath}");
                    return null;
                }
            }
            finally
            {
                Object.DestroyImmediate(inst);
            }

            return prefabPath;
        }

        /// <summary>
        /// UCX_ mesh'ini convex MeshCollider'a çevirir ve yardımcı nesneyi siler.
        /// Convex: uçuş oyununda çarpışma sorgusu sık ve ucuz olmalı; ayrıca
        /// Rigidbody ile çarpışabilmesi için zaten convex şart.
        /// </summary>
        private static void AttachCollider(GameObject root)
        {
            MeshFilter ucx = null;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.gameObject.name.StartsWith("UCX_")) { ucx = mf; break; }
            }

            if (ucx == null)
            {
                Debug.LogWarning($"[Hezarfen] {root.name}: UCX_ mesh yok, collider eklenmedi.");
                return;
            }

            var col = root.GetComponent<MeshCollider>();
            if (col == null) col = root.AddComponent<MeshCollider>();
            col.sharedMesh = ucx.sharedMesh;
            col.convex = true;

            Object.DestroyImmediate(ucx.gameObject);
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            string[] parts = assetFolder.Split('/');
            string acc = parts[0];                       // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{acc}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(acc, parts[i]);
                acc = next;
            }
        }

        private static string ToSystemPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
