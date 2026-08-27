using System.Collections.Generic;
using Hezarfen.Core;
using Hezarfen.Editor.Pipeline;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Osmanlı konut kitinin Unity ucunu kilitler (plan Faz 2, ADR 0012 + 0013).
    ///
    /// Zincir uzun — Blender jeneratörü → FBX → import politikası → doku
    /// paketleme → HDRP malzemesi → prefab — ve her halkası <b>sessizce</b>
    /// kopabilir. En sinsileri:
    ///
    ///   * Malzeme FBX'ten gömülü gelir (maske/normal yok) ama sahne "çalışır"
    ///     görünür: duvarlar yalnızca düz renktir.
    ///   * Maske haritası sRGB işaretlenir: pürüzlülük ve AO yanlış eğride
    ///     okunur, yüzeyler "biraz plastik" olur, hiçbir uyarı çıkmaz.
    ///
    /// Aşağıdaki sayılar tahmin değil ÖLÇÜMDÜR (2026-08-20, Blender 5.2 +
    /// Unity 6000.5.8f1) ve Blender'ın bildirdiği değerlerle birebir örtüşür.
    /// Yeniden üretim: docs/decisions/0013-near-detail-construction.md.
    /// </summary>
    public class OttomanHouseTests
    {
        private const string PrefabPath = "Assets/_Project/Art/Prefabs/PF_House_A.prefab";

        /// <summary>Ölçüm payı. FBX 32-bit float taşır; 2 mm fazlasıyla sıkı.</summary>
        private const float Tol = 2e-3f;

        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
        }

        private GameObject Spawn()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(asset,
                $"Varlik yok: {PrefabPath}. Once uret:\n"
                + "  blender --background --factory-startup --python tools/blender/gen_ottoman_house.py -- "
                + "--asset House_A --textured --detail near --out-fbx unity/HezarfenGame/Assets/_Import/SM_House_A.fbx\n"
                + "  Unity: Hezarfen -> Boru Hatti -> Osmanli malzemelerini uret, sonra _Import'u yerlestir");
            var inst = Object.Instantiate(asset);
            spawned.Add(inst);
            return inst;
        }

        private static Renderer Lod(GameObject root, int level)
        {
            string want = $"SM_House_A_LOD{level}";
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (r.gameObject.name == want) return r;
            Assert.Fail($"'{want}' yok — LOD adlandirmasi bozulmus.");
            return null;
        }

        // ------------------------------------------------------------- ölçek

        [Test]
        public void House_LandsOnGroundWithoutOffset()
        {
            // Orijin taban merkezindedir: prefab (x, 0, z)'ye birakildiginda
            // zemine oturur. Olmazsa sehirdeki her ev icin elle Y ofseti gerekir.
            var lod0 = Lod(Spawn(), 0);
            Assert.AreEqual(0f, lod0.bounds.min.y, 1e-3f, "Ev tabani y=0'da olmali.");
        }

        [Test]
        public void House_FootprintMatchesBlender()
        {
            // Blender: 8,90 x 8,70 m (cati sacagi dahil). Unity eslemesi
            // Blender(x, y, z) -> Unity(-x, z, -y) oldugundan Blender derinligi
            // Unity'de Z'dir. Bu iki sayinin tutmasi, olcek sozlesmesinin
            // (1 birim = 1 m) uctan uca ayakta oldugunu gosterir.
            var lod0 = Lod(Spawn(), 0);
            Assert.AreEqual(8.900f, lod0.bounds.size.x, Tol, "Genislik (Blender X).");
            Assert.AreEqual(8.700f, lod0.bounds.size.z, Tol, "Derinlik (Blender Y).");
        }

        [Test]
        public void House_HeightsAreLayeredByLod()
        {
            // Her LOD farkli bir tepe noktasi verir ve bu FARK anlamlidir:
            //   LOD0 baca kulahiyla biter, LOD1 kulahsiz bacayla, LOD2 catiyla.
            // Sayilar birbirine yaklasirsa bir katman sessizce dusmus demektir.
            var root = Spawn();
            Assert.AreEqual(8.8453f, Lod(root, 0).bounds.max.y, Tol, "LOD0: baca kulahi.");
            Assert.AreEqual(8.7603f, Lod(root, 1).bounds.max.y, Tol, "LOD1: baca (kulahsiz).");
            Assert.AreEqual(8.5115f, Lod(root, 2).bounds.max.y, Tol, "LOD2: cati tepesi.");
        }

        [Test]
        public void House_CumbaFacesUnityForward()
        {
            // "Evin onu +Z" sozlesmesi (CLAUDE.md). Cumba Blender'da sokak
            // cephesine (-Y) tasar; esleme geregi Unity'de +Z'ye bakar. Sokak
            // yerlestiricisi (Faz 4) bu kurala dayanacak.
            var lod0 = Lod(Spawn(), 0);
            Assert.Greater(lod0.bounds.max.z, -lod0.bounds.min.z, "Cumba +Z'ye tasmali.");
            Assert.AreEqual(0.8f, lod0.bounds.max.z + lod0.bounds.min.z, Tol,
                "Asimetri tam olarak cumba derinligi kadar olmali.");
        }

        // --------------------------------------------------------- malzemeler

        [Test]
        public void House_UsesAuthoredOttomanMaterials()
        {
            // En sinsi kopma noktasi: FBX'ten GOMULU malzeme gelmesi. Sahne
            // calisir gorunur ama duvarlar yalnizca duz renktir — maske, normal
            // ve parlaklik yoktur. Malzemenin bir VARLIK YOLU olmasi, gomulu
            // olmadiginin kanitidir.
            foreach (var r in Spawn().GetComponentsInChildren<Renderer>(true))
            {
                foreach (var m in r.sharedMaterials)
                {
                    Assert.IsNotNull(m, $"'{r.name}' malzemesiz.");
                    string path = AssetDatabase.GetAssetPath(m);
                    Assert.IsNotEmpty(path,
                        $"'{m.name}' GOMULU malzeme — proje varligi baglanmamis. "
                        + "ModelImportPolicy.OnAssignMaterialModel calismamis olabilir.");
                    StringAssert.StartsWith(OttomanMaterialBuilder.MaterialDir, path,
                        $"'{m.name}' beklenmeyen klasorden geliyor: {path}");
                    StringAssert.StartsWith("HDRP/", m.shader.name,
                        $"'{m.name}' HDRP disi shader: {m.shader.name}");
                }
            }
        }

        [Test]
        public void House_UsesExactlyTheDefaultPalette()
        {
            // FBX, Blender'daki malzeme ADLARINI taşır. Bir rolün adı kitte
            // değişirse ve FBX yeniden ihraç EDİLMEZSE, model eski ada bağlı
            // kalır — ve o ad hâlâ var olan başka bir malzemeyi gösteriyorsa
            // hata sessizdir: "malzeme bulundu, HDRP, maskesi var" testlerinin
            // hepsi geçer, ama ev yanlış boyayı giyer.
            //
            // Bu tam olarak yaşandı: varsayılan paletin trim'i M_Timber_Dark →
            // M_Timber_Trim olarak ayrıştırıldı; eski ad gayrimüslim paletin
            // AHŞABINA geçti ve bayat FBX ona bağlandı. Yakalayan şey bir test
            // değil, VRAM ölçümü sırasında listeye bakmam oldu.
            var expected = new HashSet<string>
            {
                "M_Stone_Rubble", "M_Plaster_Lime", "M_Opening_Shadow",
                "M_Timber_Trim", "M_Timber_AsiRed", "M_Roof_Alaturka",
            };

            var got = new HashSet<string>();
            foreach (var m in Lod(Spawn(), 0).sharedMaterials) got.Add(m.name);

            CollectionAssert.AreEquivalent(expected, got,
                "LOD0 malzeme kumesi varsayilan paletle ortusmuyor. "
                + "Kitte ad degistiyse FBX'i YENIDEN IHRAC et.");
        }

        [Test]
        public void House_PbrMaterialsCarryMaskMap()
        {
            // Maske yoksa purüzlülük ve AO kaybolur; her sey ayni parlaklikta
            // okunur. Tek dokusuz malzeme, aciklik arkasindaki karanliktir.
            foreach (var r in Spawn().GetComponentsInChildren<Renderer>(true))
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m.name == "M_Opening_Shadow") continue;    // bilerek dokusuz
                    Assert.IsTrue(m.HasProperty("_MaskMap") && m.GetTexture("_MaskMap") != null,
                        $"'{m.name}' maske haritasi tasimiyor.");
                    Assert.IsTrue(m.HasProperty("_NormalMap") && m.GetTexture("_NormalMap") != null,
                        $"'{m.name}' normal haritasi tasimiyor.");
                    Assert.IsTrue(m.HasProperty("_BaseColorMap") && m.GetTexture("_BaseColorMap") != null,
                        $"'{m.name}' albedo tasimiyor — yapi BEMBEYAZ cikar.");
                }
            }
        }

        [Test]
        public void EveryOttomanMaterial_CarriesAllThreeMaps()
        {
            // Neden ayrı bir test: yukarıdaki testler tek bir evi (varsayılan
            // palet) gezer. Gayrimüslim paletin malzemelerine hiçbir test
            // dokunmuyordu ve `M_Roof_Ceramic`in albedosu bir tur boyunca NULL
            // kaldı — hata ancak Balat sahnesinde bembeyaz evler olarak
            // görüldü. Malzeme klasörünün TAMAMI gezilmezse palet başına yeni
            // bir kör nokta doğar.
            // Muafiyet ELLE tutulmaz: kurşun ve cam bilerek dokusuzdur
            // (uygun CC0 dokusu yok — ADR 0017), açıklık gölgesi de öyle.
            // Hangi malzemenin dokusu OLMASI GEREKTİĞİNİ bildirim söyler;
            // elle tutulan bir muafiyet listesi zamanla yalancı olur.
            var shouldHaveMaps = OttomanMaterialBuilder.PbrMaterialNames();
            Assert.Greater(shouldHaveMaps.Count, 5,
                "Bildirimde pbr malzeme yok — test dissiz kalmis. "
                + "Once build_unity_maps.py, sonra malzeme uretimi.");

            var missing = new List<string>();
            int seen = 0;
            foreach (string guid in AssetDatabase.FindAssets(
                         "t:Material", new[] { "Assets/_Project/Art/Materials/Ottoman" }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (m == null || !shouldHaveMaps.Contains(m.name)) continue;
                seen++;
                foreach (string prop in new[] { "_BaseColorMap", "_MaskMap", "_NormalMap" })
                    if (!m.HasProperty(prop) || m.GetTexture(prop) == null)
                        missing.Add($"{m.name}.{prop}");
            }
            Assert.Greater(seen, 5, "Malzeme klasoru bos — test dissiz kalmis.");
            Assert.IsEmpty(missing,
                "Doku bagli degil: " + string.Join(", ", missing)
                + ". 'Hezarfen/Boru Hatti/Osmanli malzemelerini uret' calistirin.");
        }

        [Test]
        public void MaskAndNormalTextures_AreDataNotColor()
        {
            // sRGB isaretli bir maske hicbir uyari uretmez; yalnizca purüzlülük
            // ve AO yanlis egride okunur. Bu testin varlik sebebi tam olarak
            // hatanin SESSIZ olmasidir.
            int checkedCount = 0;
            foreach (var r in Spawn().GetComponentsInChildren<Renderer>(true))
            {
                foreach (var m in r.sharedMaterials)
                {
                    foreach (string prop in new[] { "_MaskMap", "_NormalMap" })
                    {
                        if (!m.HasProperty(prop)) continue;
                        var tex = m.GetTexture(prop);
                        if (tex == null) continue;
                        var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex))
                                  as TextureImporter;
                        Assert.IsNotNull(imp, $"{tex.name}: TextureImporter yok.");
                        Assert.IsFalse(imp.sRGBTexture,
                            $"{tex.name} sRGB isaretli ama VERI olmali "
                            + "(TextureImportPolicy calismamis).");
                        Assert.AreEqual(TextureWrapMode.Repeat, imp.wrapMode,
                            $"{tex.name}: dunya olcekli UV 0-1'i asar, Repeat sart.");
                        checkedCount++;
                    }
                }
            }
            Assert.Greater(checkedCount, 0, "Hic doku denetlenmedi — test dissiz kalmis.");
        }

        [Test]
        public void BaseColorTextures_AreColorNotData()
        {
            // Simetrik hata: taban rengi 'Non-Color' isaretlenirse butun ev
            // gozle gorulur sekilde KOYULASIR ve sebebi malzemede aranir.
            foreach (var r in Spawn().GetComponentsInChildren<Renderer>(true))
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (!m.HasProperty("_BaseColorMap")) continue;
                    var tex = m.GetTexture("_BaseColorMap");
                    if (tex == null) continue;
                    var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex))
                              as TextureImporter;
                    Assert.IsNotNull(imp);
                    Assert.IsTrue(imp.sRGBTexture, $"{tex.name} sRGB olmali (taban renk).");
                }
            }
        }

        // ------------------------------------------------------------ prefab

        [Test]
        public void House_PrefabIsComplete()
        {
            var house = Spawn();

            var lods = house.GetComponent<LODGroup>();
            Assert.IsNotNull(lods, "LODGroup yok — _LOD0/_LOD1/_LOD2 adlandirmasi bozulmus.");
            Assert.AreEqual(3, lods.lodCount, "Kit uc kademe uretir.");

            var tag = house.GetComponent<HistoricalTag>();
            Assert.IsNotNull(tag, "HistoricalTag zorunlu (CLAUDE.md).");
            Assert.IsTrue(tag.IsValid);

            var col = house.GetComponent<MeshCollider>();
            Assert.IsNotNull(col, "UCX_ mesh'inden collider uretilmeliydi.");
            Assert.IsTrue(col.convex, "Rigidbody ile carpisabilmesi icin convex sart.");

            foreach (var mf in house.GetComponentsInChildren<MeshFilter>(true))
                Assert.IsFalse(mf.gameObject.name.StartsWith("UCX_"),
                    "UCX_ yardimci nesnesi prefab'da kalmamali.");
        }

        [Test]
        public void House_ColliderStaysInsideSilhouette()
        {
            // Carpisma kutlesi siluetten DAR: ucusta sacak altindan gecmek
            // mumkun kalmali, "degmedim ama carpistim" hissi olmamali.
            var house = Spawn();
            var lod0 = Lod(house, 0);
            var col = house.GetComponent<MeshCollider>();

            Assert.Less(col.bounds.size.x, lod0.bounds.size.x, "Collider X'te dar olmali.");
            Assert.Less(col.bounds.size.z, lod0.bounds.size.z, "Collider Z'de dar olmali.");
            Assert.LessOrEqual(col.bounds.size.y, lod0.bounds.size.y);
        }
    }
}
