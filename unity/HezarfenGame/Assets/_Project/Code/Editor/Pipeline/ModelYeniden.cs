using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// <b>Modelleri yeniden içe aktarır, boşa düşmüş malzeme yuvalarını
    /// onarır ve gömülü kalan var mı sayar.</b>
    ///
    /// ## Neden gerekti
    ///
    /// <see cref="ModelImportPolicy.OnAssignMaterialModel"/> her FBX
    /// malzemesini <c>Art/Materials/Ottoman/M_&lt;ad&gt;.mat</c> ile
    /// eşler. Eşleşme <b>içe aktarma anında</b> yapılır ve sonucu prefaba
    /// pişer. Proje malzemesi o an yoksa Unity FBX'in kendi gömülü
    /// malzemesini bırakır; gömülü malzeme Standard gölgelendirici
    /// kullandığı için HDRP'de <b>macenta</b> çizilir. Malzeme sonradan
    /// üretilince model düzelir ama <b>prefabın yuvası boşa düşer</b> —
    /// yine macenta.
    ///
    /// Bu, oyun turunda görüldü: oyuncunun sırtındaki katlı kanat, on
    /// durağın onunda da ekranın yarısını kaplayan parlak mor bir
    /// levhaydı. Kanadın malzemesi (<c>M_Feather</c>) diskte vardı;
    /// prefabın iki yuvası boştu.
    ///
    /// Aynı sınıftan ikinci bulgu: <c>M_Beard.mat</c> hiç yoktu.
    /// <c>ottoman_kit.py</c> içindeki yorum bunun ölçülüp düzeltildiğini
    /// yazıyor — palete girdi, Unity malzemesi hiç üretilmedi. Yorum
    /// doğruydu, iş yarım kalmıştı.
    ///
    /// Sıra: <c>build_unity_maps.py</c> → <c>Osmanli malzemelerini
    /// uret</c> → <b>bu adım</b>. Malzeme üretildikten sonra modeller
    /// yeniden okunmazsa eşleme dünkü hâlinde kalır.
    /// </summary>
    public static class ModelYeniden
    {
        private const string ModelDir = "Assets/_Project/Art/Models";
        private const string PrefabDir = "Assets/_Project/Art/Prefabs";

        /// <summary>
        /// Ölçü aletinin renk kodları — bilerek gömülü.
        ///
        /// <c>SM_AxisCalibration</c> eksen/ölçek doğrulaması için var ve
        /// oyuna hiç girmez; renkleri de o yüzden kendi dosyasında
        /// yaşıyor. Muafiyet bir unutma değil, kayıtlı bir karar.
        /// </summary>
        private static readonly string[] Muaf =
            { "M_Cal_BX2", "M_Cal_BY3", "M_Cal_BZ4", "M_Cal_Unit" };

        [MenuItem("Hezarfen/Boru Hatti/Modelleri yeniden ice aktar")]
        public static void Yenile()
        {
            var yollar = Directory
                .GetFiles(ModelDir, "*.fbx", SearchOption.AllDirectories)
                .Select(y => y.Replace('\\', '/'))
                .OrderBy(y => y).ToList();

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string y in yollar)
                    AssetDatabase.ImportAsset(y, ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();

            int tazelenen = MalzemeleriTazele(out int yuva);
            var eksik = GomuluMalzemeler();

            Debug.Log($"[Hezarfen] {yollar.Count} model yeniden okundu; "
                      + $"{tazelenen} prefabta {yuva} bos malzeme yuvasi "
                      + $"onarildi; gomulu kalan: {eksik.Count}"
                      + (eksik.Count > 0
                          ? " -> " + string.Join(", ", eksik.OrderBy(a => a))
                          : "."));
            if (eksik.Count > 0)
                Debug.LogError("[Hezarfen] Bu malzemeler HDRP'de MACENTA "
                               + "cizilir. Once: build_unity_maps.py, sonra "
                               + "Osmanli malzemelerini uret.");
        }

        /// <summary>
        /// Prefabların <b>boşa düşmüş</b> malzeme yuvalarını onarır.
        /// Dönüş: değişen prefab sayısı; <paramref name="yuvaSayisi"/>
        /// onarılan yuva sayısı.
        /// </summary>
        public static int MalzemeleriTazele(out int yuvaSayisi)
        {
            int duzeltilen = 0;
            yuvaSayisi = 0;
            if (!Directory.Exists(PrefabDir)) return 0;

            foreach (string yol in Directory.GetFiles(
                         PrefabDir, "*.prefab", SearchOption.AllDirectories))
            {
                string p = yol.Replace('\\', '/');
                var kok = PrefabUtility.LoadPrefabContents(p);
                bool degisti = false;
                try
                {
                    foreach (var r in kok.GetComponentsInChildren<Renderer>(true))
                    {
                        var m = r.sharedMaterials;
                        bool yuvaDegisti = false;
                        for (int i = 0; i < m.Length; i++)
                        {
                            if (!Bozuk(m[i])) continue;
                            string ad = m[i] != null ? m[i].name
                                                     : ModeldenAd(r, i);
                            if (string.IsNullOrEmpty(ad)) continue;
                            var yeni = AssetDatabase.LoadAssetAtPath<Material>(
                                $"{OttomanMaterialBuilder.MaterialDir}/{ad}.mat");
                            if (yeni == null) continue;
                            m[i] = yeni;
                            yuvaDegisti = true;
                            yuvaSayisi++;
                        }
                        if (!yuvaDegisti) continue;
                        r.sharedMaterials = m;
                        degisti = true;
                    }
                    if (degisti)
                    {
                        PrefabUtility.SaveAsPrefabAsset(kok, p);
                        duzeltilen++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(kok);
                }
            }
            AssetDatabase.SaveAssets();
            return duzeltilen;
        }

        /// <summary>Yuva boş mu, yoksa bir model dosyasının içini mi gösteriyor.</summary>
        private static bool Bozuk(Material m)
        {
            if (m == null) return true;
            string yol = AssetDatabase.GetAssetPath(m);
            return string.IsNullOrEmpty(yol)
                   || yol.EndsWith(".fbx", System.StringComparison
                                            .OrdinalIgnoreCase);
        }

        /// <summary>
        /// Boşa düşmüş yuvanın karşılığı olan malzeme <b>adı</b>.
        ///
        /// İki deneme prefabın soyundan gitmeye çalıştı
        /// (<c>GetCorrespondingObjectFromOriginalSource</c>, sonra
        /// <c>GetCorrespondingObjectFromSource</c>) ve ikisi de null
        /// döndü. Ölçüldü: bu prefablar modelin varyantı değil, örneği
        /// alınıp <b>ayrılmış</b> kopyaları — soy zinciri yok.
        ///
        /// Mesh ise her zaman kendi dosyasını bilir. Boşa düşmüş bir
        /// yuvanın karşılığı, <b>aynı mesh'i taşıyan</b> modeldeki aynı
        /// yuvanın malzemesidir; bu, soydan bağımsızdır.
        /// </summary>
        private static string ModeldenAd(Renderer r, int yuva)
        {
            Mesh mesh = Agi(r);
            if (mesh == null) return null;

            string yol = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(yol)
                || !yol.EndsWith(".fbx", System.StringComparison
                                          .OrdinalIgnoreCase))
                return null;

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
            if (model == null) return null;
            foreach (var mr in model.GetComponentsInChildren<Renderer>(true))
            {
                if (Agi(mr) != mesh) continue;
                var mats = mr.sharedMaterials;
                if (yuva < mats.Length && mats[yuva] != null)
                    return mats[yuva].name;
            }
            return null;
        }

        private static Mesh Agi(Renderer r)
        {
            if (r is SkinnedMeshRenderer smr) return smr.sharedMesh;
            var mf = r.GetComponent<MeshFilter>();
            return mf != null ? mf.sharedMesh : null;
        }

        /// <summary>
        /// Modellerin içinde <b>gömülü kalmış</b> malzeme adları
        /// (muaf olanlar hariç).
        ///
        /// Gömülü malzeme, model dosyasının kendi içinde yaşayan
        /// alt-varlıktır (<c>.mat</c> dosyası yoktur). Testler de bu
        /// listeyi okur; ölçünün tek sahibi burasıdır.
        /// </summary>
        public static HashSet<string> GomuluMalzemeler()
        {
            var eksik = new HashSet<string>();
            if (!Directory.Exists(ModelDir)) return eksik;

            foreach (string yol in Directory.GetFiles(
                         ModelDir, "*.fbx", SearchOption.AllDirectories))
            {
                string p = yol.Replace('\\', '/');
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
                {
                    if (!(o is Material m)) continue;
                    if (Muaf.Contains(m.name)) continue;
                    eksik.Add(m.name);
                }
            }
            return eksik;
        }

        /// <summary>
        /// Prefablardaki <b>boş</b> malzeme yuvaları — testin okuduğu ölçü.
        /// </summary>
        public static List<string> BosYuvalar()
        {
            var liste = new List<string>();
            if (!Directory.Exists(PrefabDir)) return liste;

            foreach (string yol in Directory.GetFiles(
                         PrefabDir, "*.prefab", SearchOption.AllDirectories))
            {
                string p = yol.Replace('\\', '/');
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pf == null) continue;
                foreach (var r in pf.GetComponentsInChildren<Renderer>(true))
                {
                    var m = r.sharedMaterials;
                    for (int i = 0; i < m.Length; i++)
                        if (Bozuk(m[i]))
                            liste.Add($"{Path.GetFileNameWithoutExtension(p)}"
                                      + $"/{r.name}[{i}]");
                }
            }
            return liste;
        }
    }
}
