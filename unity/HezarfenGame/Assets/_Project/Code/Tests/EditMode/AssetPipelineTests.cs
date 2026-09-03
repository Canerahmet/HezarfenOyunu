using System.Collections.Generic;
using Hezarfen.Core;
using Hezarfen.Editor.Pipeline;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Blender → FBX → Unity boru hattının ölçek ve eksen sözleşmesini kilitler
    /// (plan Görev 7: "1 m küp tam 1 m mi?").
    ///
    /// Neden test, neden gözle kontrol değil: ölçek ve eksen hataları sessizdir.
    /// Yan yatmış bir ev fark edilir; %1,27 büyük bir ev fark edilmez ve şehrin
    /// tamamı üretildikten sonra ortaya çıkar. Bu testler o hatayı üretim anında,
    /// tek varlıkta yakalar.
    ///
    /// Ölçüt varlığı <c>SM_AxisCalibration.fbx</c> bir sanat varlığı değil ölçü
    /// aletidir: üç işaretçi üç FARKLI uzaklıkta durur (2/3/4 m), böylece eksen
    /// takası ile eksen çevrimi birbirinden ayırt edilebilir.
    ///
    /// Aşağıdaki sayılar tahmin değil ÖLÇÜMDÜR (2026-08-17, Blender 5.2 +
    /// Unity 6000.5.8f1). Gerekçe ve türetimi: docs/decisions/0005-asset-pipeline.md.
    /// </summary>
    public class AssetPipelineTests
    {
        private const string CalibrationPath =
            "Assets/_Project/Art/Models/Calibration/SM_AxisCalibration.fbx";
        private const string HousePrefabPath =
            "Assets/_Project/Art/Prefabs/PF_BoxHouse.prefab";

        /// <summary>Kayan nokta payı. FBX 32-bit float taşır; 0,1 mm fazlasıyla sıkı.</summary>
        private const float Eps = 1e-4f;

        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
        }

        private GameObject Spawn(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Assert.IsNotNull(asset, $"Varlik bulunamadi: {assetPath}");
            var inst = Object.Instantiate(asset);
            spawned.Add(inst);
            return inst;
        }

        private static Renderer FindRenderer(GameObject root, string name)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (r.gameObject.name == name) return r;
            Assert.Fail($"'{name}' renderer'i yok. Kalibrasyon varligi bozulmus.");
            return null;
        }

        // ----------------------------------------------------------- politika

        [Test]
        public void ImportPolicy_GovernsPipelineFolders()
        {
            Assert.IsTrue(ModelImportPolicy.IsGoverned("Assets/_Import/x.fbx"));
            Assert.IsTrue(ModelImportPolicy.IsGoverned("Assets/_Project/Art/Models/x.fbx"));
            Assert.IsTrue(ModelImportPolicy.IsGoverned(@"Assets\_Project\Art\Models\x.fbx"),
                "Windows ters bolu ile gelen yollar da kapsanmali.");
            Assert.IsFalse(ModelImportPolicy.IsGoverned("Packages/com.unity.x/Model.fbx"),
                "Paket ici modellere dokunulmamali.");
        }

        [Test]
        public void ImportSettings_AreLockedByPolicy()
        {
            var imp = AssetImporter.GetAtPath(CalibrationPath) as ModelImporter;
            Assert.IsNotNull(imp, $"ModelImporter yok: {CalibrationPath}");

            Assert.AreEqual(1f, imp.globalScale, Eps, "Olcek carpani 1 olmali.");
            Assert.IsTrue(imp.useFileScale, "FBX'in kendi birimi kullanilmali.");
            Assert.AreEqual(ModelImporterNormals.Import, imp.importNormals);
            Assert.AreEqual(ModelImporterTangents.CalculateMikk, imp.importTangents);
            Assert.AreEqual(ModelImporterMaterialImportMode.ImportViaMaterialDescription,
                imp.materialImportMode,
                "ImportStandard, HDRP'de macenta malzeme uretir.");
            Assert.AreEqual(ModelImporterMaterialLocation.InPrefab, imp.materialLocation,
                "MaterialLocation.External Unity 6'da KALDIRILDI; baglama "
                + "ModelImportPolicy.OnAssignMaterialModel uzerinden yapilir.");
            Assert.IsFalse(imp.addCollider,
                "Otomatik collider siluetten genis cikar; UCX_ mesh'i kullaniyoruz.");
        }

        // ------------------------------------------------------------- olcek

        [Test]
        public void UnitCube_IsExactlyOneMeter()
        {
            // Sozlesmenin ta kendisi: 1 birim = 1 metre (CLAUDE.md).
            var cube = FindRenderer(Spawn(CalibrationPath), "SM_AxisCal_UnitCube");
            Vector3 size = cube.bounds.size;

            Assert.AreEqual(1f, size.x, Eps, $"X kenari 1 m degil: {size.x}");
            Assert.AreEqual(1f, size.y, Eps, $"Y kenari 1 m degil: {size.y}");
            Assert.AreEqual(1f, size.z, Eps, $"Z kenari 1 m degil: {size.z}");
        }

        [Test]
        public void MarkerDistances_SurviveTranslation()
        {
            // Merkezdeki kup dogru olup uzaktaki isaretci kaymissa, sorun olcek
            // degil birim donusumudur (ornegin cm/m karisimi). Bu test onu ayirir.
            var root = Spawn(CalibrationPath);
            Assert.AreEqual(2f, FindRenderer(root, "SM_AxisCal_BX2").bounds.center.magnitude, Eps);
            Assert.AreEqual(3f, FindRenderer(root, "SM_AxisCal_BY3").bounds.center.magnitude, Eps);
            Assert.AreEqual(4f, FindRenderer(root, "SM_AxisCal_BZ4").bounds.center.magnitude, Eps);
        }

        [Test]
        public void ImportedRoot_HasIdentityTransform()
        {
            // Blender FBX'i varsayilan ayarlarla verildiginde Unity'de kok nesne
            // (-89.98, 0, 0) rotasyonuyla gelir. export_fbx.py'deki
            // bake_space_transform=True bunu mesh verisine isleyerek onler.
            // Kirilirsa her prefab'a elle duzeltme rotasyonu girmek gerekir.
            var root = Spawn(CalibrationPath);
            var rot = root.transform.localRotation.eulerAngles;

            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, rot.x), 0.01f, "Kok X rotasyonu sifir olmali.");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, rot.y), 0.01f, "Kok Y rotasyonu sifir olmali.");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, rot.z), 0.01f, "Kok Z rotasyonu sifir olmali.");
            Assert.AreEqual(Vector3.one, root.transform.localScale);

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                Assert.AreEqual(Vector3.one, t.localScale,
                    $"'{t.name}' uygulanmamis olcek tasiyor.");
        }

        // ------------------------------------------------------------- eksen

        [Test]
        public void AxisMapping_BlenderUp_BecomesUnityUp()
        {
            // Blender +Z (yukari, 4 m) -> Unity +Y. Bu kirilirsa binalar yan yatar.
            var m = FindRenderer(Spawn(CalibrationPath), "SM_AxisCal_BZ4").bounds.center;
            Assert.AreEqual(new Vector3(0f, 4f, 0f).ToString("F4"), m.ToString("F4"));
        }

        [Test]
        public void AxisMapping_BlenderFront_BecomesUnityForward()
        {
            // Blender +Y (arka, 3 m) -> Unity -Z. Yani Blender'da -Y'ye bakan
            // cephe Unity'de +Z'ye (ileri) bakar; genel kabul goren esleme budur.
            var m = FindRenderer(Spawn(CalibrationPath), "SM_AxisCal_BY3").bounds.center;
            Assert.AreEqual(new Vector3(0f, 0f, -3f).ToString("F4"), m.ToString("F4"));
        }

        [Test]
        public void AxisMapping_BlenderX_IsInverted()
        {
            // Blender +X (2 m) -> Unity -X. Tek basina "aynalanmis" gibi gorunur
            // ama degildir: Blender sag-elli, Unity sol-elli. Isaret cevrimi tam
            // olarak bu el degisimini karsilar; AxisMapping_PreservesHandedness
            // bunu birlikte dogrular.
            var m = FindRenderer(Spawn(CalibrationPath), "SM_AxisCal_BX2").bounds.center;
            Assert.AreEqual(new Vector3(-2f, 0f, 0f).ToString("F4"), m.ToString("F4"));
        }

        [Test]
        public void AxisMapping_PreservesHandedness()
        {
            // Model AYNALANMAMIS olmali. Blender'da nesnenin kendi sag/yukari/ileri
            // ucluleri (-X / +Z / -Y), Unity'nin sag/yukari/ileri ucluleriyle
            // (+X / +Y / +Z) ortusmeli. Ortusmezse yazilar ters, merdivenler
            // yanlis yonde doner ve hata ancak dokular gelince fark edilir.
            var root = Spawn(CalibrationPath);
            Vector3 bx = FindRenderer(root, "SM_AxisCal_BX2").bounds.center / 2f;   // Blender +X
            Vector3 by = FindRenderer(root, "SM_AxisCal_BY3").bounds.center / 3f;   // Blender +Y
            Vector3 bz = FindRenderer(root, "SM_AxisCal_BZ4").bounds.center / 4f;   // Blender +Z

            Vector3 objectRight = -bx;      // Blender'da nesnenin sagi -X
            Vector3 objectUp = bz;          //                    yukarisi +Z
            Vector3 objectForward = -by;    //                    ilerisi  -Y

            Assert.AreEqual(Vector3.right.ToString("F3"), objectRight.ToString("F3"));
            Assert.AreEqual(Vector3.up.ToString("F3"), objectUp.ToString("F3"));
            Assert.AreEqual(Vector3.forward.ToString("F3"), objectForward.ToString("F3"));

            // Sol-elli sistemde right x up = forward. Aynalanma olsaydi isaret doner.
            Assert.AreEqual(1f, Vector3.Dot(Vector3.Cross(objectRight, objectUp), objectForward), 1e-3f,
                "Uclu sol-elli degil: model aynalanmis.");
        }

        [Test]
        public void Materials_UseActiveRenderPipelineShader()
        {
            // ImportStandard secilirse HDRP'de macenta malzeme gelir ve inceleme
            // paketleri bastan cope gider. Bu test o hatayi import aninda yakalar.
            foreach (var r in Spawn(CalibrationPath).GetComponentsInChildren<Renderer>(true))
            {
                Assert.IsNotNull(r.sharedMaterial, $"'{r.name}' malzemesiz.");
                StringAssert.StartsWith("HDRP/", r.sharedMaterial.shader.name,
                    $"'{r.name}' HDRP disi shader kullaniyor: {r.sharedMaterial.shader.name}");
            }
        }

        // --------------------------------------------------- kutu ev / prefab

        [Test]
        public void BoxHouse_LandsOnGroundWithoutOffset()
        {
            // Modelin orijini taban merkezindedir: prefab (x, 0, z) konumuna
            // birakildiginda zemine oturur. Bu olmazsa sehirdeki her ev icin
            // elle Y ofseti girmek gerekir — 20.000 evde imkansiz.
            var house = Spawn(HousePrefabPath);
            var lod0 = FindRenderer(house, "SM_BoxHouse_LOD0");

            Assert.AreEqual(0f, lod0.bounds.min.y, 1e-3f, "Ev tabani y=0'da olmali.");
            Assert.AreEqual(8.2f, lod0.bounds.size.y, 1e-3f,
                "Toplam yukseklik: 0,6 subasman + 2x2,7 kat + 2,2 cati.");
            Assert.AreEqual(8.9f, lod0.bounds.size.x, 1e-3f,
                "Genislik: 7,0 + 2x0,25 yan cikma + 2x0,7 sacak.");
        }

        [Test]
        public void BoxHouse_CumbaFacesUnityForward()
        {
            // Cumba Blender'da sokak cephesine (-Y) tasar; esleme geregi Unity'de
            // +Z'ye bakar. "Evin onu +Z'dir" kurali sokak yerlestiricisinin
            // (Faz 2) dayanacagi sozlesmedir.
            var lod0 = FindRenderer(Spawn(HousePrefabPath), "SM_BoxHouse_LOD0");
            Assert.Greater(lod0.bounds.max.z, -lod0.bounds.min.z,
                "Cumba +Z yonunde tasmali.");
            Assert.AreEqual(0.8f, lod0.bounds.max.z + lod0.bounds.min.z, 1e-3f,
                "Asimetri tam olarak cumba derinligi kadar olmali.");
        }

        [Test]
        public void BoxHouse_PrefabIsComplete()
        {
            var house = Spawn(HousePrefabPath);

            var lodGroup = house.GetComponent<LODGroup>();
            Assert.IsNotNull(lodGroup, "LODGroup yok. _LOD0/_LOD1 adlandirmasi bozulmus.");
            Assert.AreEqual(2, lodGroup.lodCount);

            var tag = house.GetComponent<HistoricalTag>();
            Assert.IsNotNull(tag, "HistoricalTag zorunlu (CLAUDE.md).");
            Assert.IsTrue(tag.IsValid);

            var col = house.GetComponent<MeshCollider>();
            Assert.IsNotNull(col, "UCX_ mesh'inden collider uretilmeliydi.");
            Assert.IsTrue(col.convex, "Rigidbody ile carpisabilmesi icin convex sart.");

            foreach (var mf in house.GetComponentsInChildren<MeshFilter>(true))
                Assert.IsFalse(mf.gameObject.name.StartsWith("UCX"),
                    "UCX_ yardimci nesnesi prefab'da kalmamali.");
        }

        [Test]
        public void BoxHouse_ColliderStaysInsideSilhouette()
        {
            // Carpisma kutlesi siluetten DAR olmali. Ucus oyununda oyuncu
            // "degmedim ama carpistim" hissini affetmez; sacak altindan gecmek
            // mumkun kalmali.
            var house = Spawn(HousePrefabPath);
            var lod0 = FindRenderer(house, "SM_BoxHouse_LOD0");
            var col = house.GetComponent<MeshCollider>();

            Assert.Less(col.bounds.size.x, lod0.bounds.size.x, "Collider X'te dar olmali.");
            Assert.Less(col.bounds.size.z, lod0.bounds.size.z, "Collider Z'de dar olmali.");
            Assert.LessOrEqual(col.bounds.size.y, lod0.bounds.size.y);
        }

        [Test]
        public void CataloguedPrefabs_CarryTheirHistoricalTier()
        {
            // CLAUDE.md: "Her yeni sahne ogesine HistoricalTag ata". Etiket
            // prefab'a ELLE konulamaz — boru hatti prefab'i her kosuşta sifirdan
            // yazar ve el yazisi kaybolur. Bu yuzden kaynak katalogdur; test
            // katalogda kaydi olan HER varligin prefab'inin gercekten o kademeyi
            // tasidigini dogrular.
            //
            // Testin disi: yalnizca "Graybox degil" demek yetmez, kaynak notunun
            // da dolu olmasi aranir — bos notlu bir T2 etiketi, iddiayi
            // dogrulanabilir kilmadigi icin etiketsizden farksizdir.
            Assert.Greater(AssetCatalog.Count, 0,
                "Katalog bos: art/blend/**/catalog.json okunamadi. "
                + "Uretici scriptleri calistirin.");

            var missing = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab",
                         new[] { ImportLanding.PrefabDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string asset = System.IO.Path.GetFileNameWithoutExtension(path);
                if (asset.StartsWith("PF_")) asset = asset.Substring(3);
                if (!AssetCatalog.TryGet(asset, out var entry)) continue;

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var tag = go != null ? go.GetComponent<HistoricalTag>() : null;
                if (tag == null) { missing.Add($"{asset}: HistoricalTag yok"); continue; }
                if (tag.tier != entry.Tier)
                    missing.Add($"{asset}: kademe {tag.tier}, katalog {entry.Tier}");
                else if (string.IsNullOrWhiteSpace(tag.sourceNote))
                    missing.Add($"{asset}: kaynak notu bos");
            }

            Assert.IsEmpty(missing,
                "Katalogdaki kademe prefab'a islenmemis: " + string.Join("; ", missing));
        }

        [Test]
        public void ImportLanding_IsEmpty()
        {
            // _Import bir inis alanidir, depo degil. Icinde varlik unutulursa
            // hangi kopyanin kanonik oldugu belirsizlesir.
            var stray = AssetDatabase.FindAssets("t:Model", new[] { ImportLanding.LandingDir });
            Assert.AreEqual(0, stray.Length,
                "_Import bos olmali: 'Hezarfen/Boru Hatti/_Import'u yerlestir' calistirin.");
        }
    
        /// <summary>
        /// <b>Ayrıntı, görüntülendiği yerde olmalı.</b>
        ///
        /// Ayrıntı geçişi LOD0'ı altı katına çıkardı ve LOD1'e dokunmadı.
        /// Sonuç ÖLÇÜLDÜ: LODGroup eşiği 0,25 ekran yüksekliği ve FOV 40°
        /// iken Süleymaniye'nin tam ayrıntılı mesh'i yalnızca <b>573 m</b>'ye
        /// kadar görüntüleniyordu; ötesinde 456 üçgenlik blok geliyordu.
        /// Hezarfen'in uçuşu ise <b>3336 m</b>. Yani üretilen ayrıntının
        /// tamamı, oyunun merkez sahnesinde hiç görünmüyordu ve geçiş tek
        /// adımda 197 kat düşüyordu.
        ///
        /// Bu testin tuttuğu iki olgu:
        /// <list type="number">
        /// <item>Ağır bir varlık (LOD0 &gt; 20 bin üçgen) <b>üç kademeli</b>
        ///       olmak zorunda — arada bir orta kademe bulunmalı.</item>
        /// <item>Geçiş eşikleri Unity'nin varsayılanına bırakılmaz; merdiven
        ///       <c>ImportLanding.SetLodThresholds</c>'ta yazılıdır.</item>
        /// </list>
        ///
        /// Gözle yakalanamayacak bir kusurdu: her şey doğru görünüyordu,
        /// çünkü yakından bakınca ayrıntı gerçekten oradaydı.
        /// Gerekçe: docs/decisions/0061-lod-merdiveni.md
        /// </summary>
        [Test]
        public void HeavyLandmarksHaveAMidLodAndAnExplicitLadder()
        {
            var eksik = new List<string>();
            var yanlisEsik = new List<string>();

            foreach (var guid in AssetDatabase.FindAssets(
                         "t:Prefab", new[] { "Assets/_Project/Art/Prefabs" }))
            {
                string yol = AssetDatabase.GUIDToAssetPath(guid);
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
                if (pf == null) continue;
                var grup = pf.GetComponent<LODGroup>();
                if (grup == null) continue;

                var lods = grup.GetLODs();
                if (lods.Length == 0) continue;

                // DERI BAGLI AG DA SAYILIR.
                //
                // Sayim yalniz `MeshFilter`a bakiyordu ve karakterler
                // `SkinnedMeshRenderer` tasiyor: 58.000 ucgenlik bir
                // gövde bu kurala gore SIFIR ucgendi ve iki kademeyle
                // gecip gidiyordu. Kural yazilmisti, kapsami eksikti —
                // bu depoda tekrar eden bicimiyle: bir olcu, olcmesi
                // gereken seyi hic gormuyor.
                int tris = 0;
                foreach (var r in lods[0].renderers)
                {
                    if (r == null) continue;
                    Mesh ag = r is SkinnedMeshRenderer smr
                        ? smr.sharedMesh
                        : r.GetComponent<MeshFilter>()?.sharedMesh;
                    if (ag != null) tris += ag.triangles.Length / 3;
                }

                if (tris > 20000 && lods.Length < 3)
                    eksik.Add($"{pf.name} ({tris} ucgen, {lods.Length} kademe)");

                // Merdiven `ImportLanding`den OKUNUR, burada kopyalanmaz.
                //
                // Eskiden sayilar burada da yaziliydi ve iki kopya bir
                // sure ayni kaldi. Karakter merdiveni eklenince ayristilar:
                // boru hatti dogru sayiyi yaziyordu, test eski sayiyi
                // bekliyordu ve KIRMIZI YANAN test dogru olan taraftı degil.
                // Bir sayinin iki sahibi varsa er ya da gec iki degeri olur.
                float[] merdiven = ImportLanding.Merdiven(grup);
                for (int i = 0; i < lods.Length && i < merdiven.Length; i++)
                {
                    float e = lods[i].screenRelativeTransitionHeight;
                    if (Mathf.Abs(e - merdiven[i]) > 1e-4f)
                        yanlisEsik.Add($"{pf.name} LOD{i}: {e} != {merdiven[i]}");
                }
            }

            Assert.IsEmpty(eksik,
                "Agir varlik orta kademesiz: bu mesh'ler ancak birkac yuz "
                + "metreden goruntuleniyor, otesinde bloga dusuyor. "
                + "Uretecte `ottoman_kit.build_with_mid_lod` kullan. "
                + string.Join("; ", eksik));

            Assert.IsEmpty(yanlisEsik,
                "LOD esikleri merdivenden sapmis — `ImportLanding."
                + "SetLodThresholds` calismamis ya da prefab elle "
                + "duzenlenmis. " + string.Join("; ", yanlisEsik));
        }
}
}
