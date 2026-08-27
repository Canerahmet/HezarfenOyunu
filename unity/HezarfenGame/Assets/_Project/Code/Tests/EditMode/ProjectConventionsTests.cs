using Hezarfen.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Projenin bozulmaması gereken temel sözleşmelerini kilitler.
    /// Bunlar "güzel olsa iyi olur" ayarları değil; bozulduklarında tüm varlık
    /// hattı sessizce yanlış çalışır. O yüzden testle sabitleniyorlar.
    /// </summary>
    public class ProjectConventionsTests
    {
        [Test]
        public void ColorSpace_IsLinear()
        {
            // HDRP fiziksel tabanlı aydınlatma icin Linear sart. Gamma'ya dusulurse
            // tum isik/malzeme calismasi cope gider.
            Assert.AreEqual(ColorSpace.Linear, PlayerSettings.colorSpace,
                "Renk uzayi Linear olmali - HDRP ve tum isiklandirma calismasi buna bagli.");
        }

        [Test]
        public void RenderPipeline_IsHDRP()
        {
            RenderPipelineAsset rp = GraphicsSettings.defaultRenderPipeline;
            Assert.IsNotNull(rp,
                "Render pipeline atanmamis - proje Built-in'e dusmus demektir.");
            Assert.That(rp.GetType().Name, Does.Contain("HDRenderPipelineAsset"),
                $"HDRP bekleniyordu, bulunan: {rp.GetType().Name}. " +
                "URP'ye gecis bir karar kapisidir (docs/PLAN.md Bolum 1) - kazara olmamali.");
        }

        [Test]
        public void OneUnit_IsOneMeter()
        {
            // Unity'nin ilkel kupu tanim geregi 1x1x1 birimdir. Bu test, ölçek
            // sözlesmemizin (1 birim = 1 metre) kod tarafindaki karsiligini dogrular;
            // Blender'dan gelen FBX'lerin dogrulamasi ayri testte yapilir.
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                Bounds bounds = cube.GetComponent<MeshFilter>().sharedMesh.bounds;
                Assert.AreEqual(1f, GameUnits.LongestEdgeMeters(bounds), 0.0001f,
                    "Birim kup 1 metre olmali - ölçek sözlesmesi bozulmus.");
                Assert.AreEqual(1f, GameUnits.MetersPerUnit, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(cube);
            }
        }

        [Test]
        public void UnitConversion_RoundTrips()
        {
            const float meters = 3358f;
            Assert.AreEqual(meters, GameUnits.UnitsToMeters(GameUnits.MetersToUnits(meters)), 0.0001f);
        }

        [Test]
        public void ProjectFolders_FollowConvention()
        {
            // Plan Bolum 3: bize ait her sey _Project altinda, _Import sadece inis alani.
            Assert.IsTrue(AssetDatabase.IsValidFolder("Assets/_Project"),
                "Assets/_Project bulunamadi.");
            Assert.IsTrue(AssetDatabase.IsValidFolder("Assets/_Import"),
                "Assets/_Import bulunamadi - Blender FBX inis alani.");
        }

        /// <summary>
        /// <b>Derlenen test sayısı, kaynaktaki test sayısına eşit olmalı.</b>
        ///
        /// ## Bu test bir yalanı yakalamak için var
        ///
        /// 2026-08-27: `LandmarkTests`e dört test eklendi, dosyada bir
        /// <c>CS0102</c> (çift alan bildirimi) vardı ve test assembly'si
        /// <b>derlenmedi</b>. Koşum yine de <b>223/223 YEŞİL</b> döndü —
        /// çünkü Unity bir önceki, sağlam assembly'yi çalıştırdı.
        ///
        /// Bu, "atlanan test geçen test gibi görünür"den (ADR 0041, 0043,
        /// 0044) <b>daha kötüsüdür</b>: orada eksik test görünmüyordu,
        /// burada <b>yanlış sürüm</b> koşuyor ve yeşil diyor. Yalnızca
        /// konsola bakarak fark ettim; bir sonraki sefer bakmayabilirim.
        ///
        /// ## Neden bu test işe yarar
        ///
        /// Bu testin kendisi <b>eski</b> assembly'de de vardır. Derleme
        /// başarısız olduğunda o eski nüsha koşar, diskteki kaynağı okur
        /// (güncel) ve assembly'yi yansımayla sayar (bayat) — ikisi
        /// tutmayınca <b>patlar</b>. Yani bekçi, korumak istediği hatanın
        /// içinde de çalışır.
        /// </summary>
        [Test]
        public void CompiledTestCountMatchesTheSource()
        {
            string root = Hezarfen.Editor.Gis.TerrainImporter.RepositoryRoot();
            Assert.IsNotNull(root, "Depo koku bulunamadi.");
            string dir = System.IO.Path.Combine(root, "unity", "HezarfenGame",
                "Assets", "_Project", "Code", "Tests", "EditMode");
            Assert.IsTrue(System.IO.Directory.Exists(dir),
                "EditMode test klasoru yok.");

            int inSource = 0;
            foreach (var f in System.IO.Directory.GetFiles(dir, "*.cs"))
            {
                foreach (var line in System.IO.File.ReadAllLines(f))
                    if (line.Trim() == "[Test]") inSource++;
            }

            int inAssembly = 0;
            var asm = typeof(ProjectConventionsTests).Assembly;
            foreach (var t in asm.GetTypes())
                foreach (var m in t.GetMethods(
                    System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.DeclaredOnly))
                    foreach (var a in m.GetCustomAttributes(false))
                        if (a.GetType().Name == "TestAttribute") inAssembly++;

            Assert.AreEqual(inSource, inAssembly,
                $"kaynakta {inSource} [Test], derlenmis assembly'de "
                + $"{inAssembly}. Aradaki fark, test assembly'sinin "
                + "DERLENMEDIGI anlamina gelir: kosum eski surumu "
                + "calistirir ve YESIL doner. Konsolda derleme hatasi ara.");
        }
    }
}
