using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// <b>Karakterin kendi iniş alanı</b> — statik boru hattından ayrı.
    ///
    /// `ImportLanding` bir mesh'i alıp LODGroup'lu bir prefab yapar ve
    /// landmark'lar için doğru olan budur. Karakter başka bir şeydir:
    /// deri bağlı (skinned), Humanoid avatarı olmalı, ve avatar
    /// <b>kurulmazsa</b> hiçbir animasyon çalışmaz. Statik hatta zorlamak,
    /// hatanın nerede olduğunu gizlerdi.
    ///
    /// ## Avatar neden burada doğrulanıyor
    ///
    /// Unity, Humanoid avatarı kuramazsa importer sessizce Generic'e
    /// düşmez — kurar ama <c>isValid=false</c> der ve o bayrağa kimse
    /// bakmazsa yedi animasyon yazıldıktan sonra ortaya çıkar. Bu araç
    /// avatarı kurar, <b>geçerliliğini okur</b> ve hangi kemiğin
    /// eşleşmediğini söyler.
    /// </summary>
    public static class KarakterLanding
    {
        private const string ImportDir = "Assets/_Import";
        private const string ModelDir = "Assets/_Project/Art/Models/Karakter";
        private const string PrefabDir = "Assets/_Project/Art/Prefabs";

        /// <summary>Karakter FBX'leri bu önekle tanınır.</summary>
        private const string Prefix = "SK_Hezarfen_";

        [MenuItem("Hezarfen/Boru Hatti/Karakteri yerlestir (Humanoid)")]
        public static void Place()
        {
            if (!Directory.Exists(ModelDir)) Directory.CreateDirectory(ModelDir);
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);

            var fbx = Directory.Exists(ImportDir)
                ? Directory.GetFiles(ImportDir, Prefix + "*.fbx")
                : new string[0];
            if (fbx.Length == 0)
            {
                Debug.LogWarning($"[Hezarfen] {ImportDir} altinda karakter FBX yok. "
                                 + "Once: blender ... gen_hezarfen.py -- --export");
                return;
            }

            var sb = new StringBuilder("KARAKTER YERLESTIRME");
            int ok = 0;
            foreach (string src in fbx.OrderBy(p => p))
            {
                string ad = Path.GetFileNameWithoutExtension(src);
                string hedef = $"{ModelDir}/{ad}.fbx";
                // Var olan yolun uzerine yazmak GUID'i korur; silip yeniden
                // yaratmak butun referanslari sessizce kirardi.
                if (File.Exists(hedef)) File.Delete(hedef);
                File.Move(src, hedef);
                string meta = src + ".meta";
                if (File.Exists(meta)) File.Delete(meta);
                AssetDatabase.ImportAsset(hedef, ImportAssetOptions.ForceUpdate);

                var mi = AssetImporter.GetAtPath(hedef) as ModelImporter;
                if (mi == null)
                {
                    sb.AppendLine($"  {ad}: ModelImporter yok");
                    continue;
                }
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.importAnimation = true;
                // 1 birim = 1 metre; Blender zaten metre yaziyor.
                mi.globalScale = 1f;
                mi.useFileScale = true;
                mi.importNormals = ModelImporterNormals.Import;
                mi.SaveAndReimport();

                sb.AppendLine(Rapor(ad, hedef, ref ok));
                sb.AppendLine(Prefab(ad, hedef));
            }

            sb.AppendLine($"gecerli avatar: {ok}/{fbx.Length}");
            if (ok == fbx.Length) Debug.Log("[Hezarfen] " + sb);
            else Debug.LogError("[Hezarfen] " + sb);
            AssetDatabase.Refresh();
        }

        private static string Rapor(string ad, string yol, ref int ok)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(yol)
                .OfType<Avatar>().FirstOrDefault();

            if (avatar == null)
                return $"  {ad}: AVATAR YOK — iskelet Humanoid'e eslenemedi.";
            if (!avatar.isValid)
                return $"  {ad}: AVATAR GECERSIZ — {Eksikler(go)}";

            ok++;
            var smr = go != null
                ? go.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                : new SkinnedMeshRenderer[0];
            int kemik = smr.Length > 0 ? smr[0].bones.Length : 0;
            var b = go != null ? go.GetComponentsInChildren<Transform>(true) : null;
            float boy = 0f;
            if (b != null)
                foreach (var t in b) boy = Mathf.Max(boy, t.localPosition.y);
            return $"  {ad}: avatar GECERLI, {smr.Length} skinned mesh, "
                   + $"{kemik} kemik";
        }

        /// <summary>
        /// Prefab: HistoricalTag + LODGroup.
        ///
        /// Karakter bir sahne ögesidir, yani CLAUDE.md'nin "her yeni sahne
        /// ögesine HistoricalTag" kurali ona da isler. Etiket katalogdan
        /// okunur (`AssetCatalog`) — elle yazilsa iki yerde iki farkli
        /// dogruluk iddiasi olurdu.
        ///
        /// Animator BILEREK konmuyor: kontrolcu ve klipler animasyon
        /// turunun isi ve bos bir Animator "animasyon var" gibi gorunur.
        /// </summary>
        private static string Prefab(string ad, string modelYol)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelYol);
            if (model == null) return $"  {ad}: prefab icin model yuklenemedi";

            string kisa = ad.StartsWith("SK_") ? ad.Substring(3) : ad;
            string yol = $"{PrefabDir}/PF_{kisa}.prefab";

            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = $"PF_{kisa}";

            var smr = go.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .OrderByDescending(r => r.sharedMesh != null
                                        ? r.sharedMesh.triangles.Length : 0)
                .ToArray();
            if (smr.Length >= 2)
            {
                // Unity, `_LOD0` / `_LOD1` adlandirmasini gorunce LODGroup'u
                // ICE AKTARIRKEN kendisi kuruyor. Korukorune AddComponent
                // cagirmak "component zaten var" diyip null donduruyor ve bir
                // satir sonra NullReferenceException'a cikiyordu. Var olani
                // kullan, yoksa kur.
                var grup = go.GetComponent<LODGroup>();
                if (grup == null) grup = go.AddComponent<LODGroup>();
                grup.SetLODs(new[]
                {
                    new LOD(1f, new Renderer[] { smr[0] }),
                    new LOD(0f, new Renderer[] { smr[1] }),
                });
                grup.RecalculateBounds();
                // Esikleri BURADA yazmiyoruz: merdivenin tek sahibi
                // `ImportLanding`. Elle sayi yazmak, o merdiven degisince
                // karakteri sessizce geride birakirdi.
                ImportLanding.SetLodThresholds(grup);
            }

            var tag = go.GetComponent<Hezarfen.Core.HistoricalTag>();
            if (tag == null) tag = go.AddComponent<Hezarfen.Core.HistoricalTag>();
            if (AssetCatalog.TryGet(kisa, out var kayit))
            {
                tag.tier = kayit.Tier;
                tag.sourceNote = kayit.Source;
            }
            else
            {
                return $"  {ad}: katalogda '{kisa}' yok — HistoricalTag "
                       + "Graybox kaldi.";
            }

            PrefabUtility.SaveAsPrefabAsset(go, yol);
            Object.DestroyImmediate(go);
            return $"  {ad}: prefab {yol} ({smr.Length} kademe, "
                   + $"{tag.tier})";
        }

        /// <summary>
        /// Avatar geçersizse hangi kemiğin eksik olduğunu söyler.
        ///
        /// Unity'nin kendi hata mesajı "Invalid avatar" der ve orada
        /// bırakır. Hangi kemiğin eksik olduğunu bilmeden düzeltmek
        /// tahmine dayanır.
        /// </summary>
        private static string Eksikler(GameObject go)
        {
            if (go == null) return "model yuklenemedi";
            var var_ = new HashSet<string>(
                go.GetComponentsInChildren<Transform>(true).Select(t => t.name));
            string[] zorunlu =
            {
                "Hips", "Spine", "Head", "Neck",
                "LeftUpperArm", "LeftLowerArm", "LeftHand",
                "RightUpperArm", "RightLowerArm", "RightHand",
                "LeftUpperLeg", "LeftLowerLeg", "LeftFoot",
                "RightUpperLeg", "RightLowerLeg", "RightFoot",
            };
            var eksik = zorunlu.Where(z => !var_.Contains(z)).ToArray();
            return eksik.Length == 0
                ? "kemikler var ama esleme tutmadi (oran ya da yon sorunu)"
                : "eksik kemik: " + string.Join(", ", eksik);
        }
    }
}
