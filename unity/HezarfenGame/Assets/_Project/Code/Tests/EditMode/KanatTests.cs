using System;
using System.Collections.Generic;
using System.IO;
using Hezarfen.Core;
using Hezarfen.Editor.Pipeline;
using Hezarfen.Flight;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Görünen kanat ile uçan kanat aynı şey mi?</b>
    ///
    /// Kanat aygıtının tasarımı serbesttir — tarihî plan yoktur (ADR 0064) —
    /// ama <b>tek bir sert sayısı</b> vardır: alan. Uçuş bütçesi
    /// (<c>FlightBudget</c>) ve termik sınavı
    /// (<c>ThermalFlightSim</c>) <c>WindTuning.wingArea</c> ile
    /// ölçüldü. Modelin alanı ondan saparsa oyuncu <b>bir şey görüp başka
    /// bir şeyin fiziğini yaşar</b>.
    ///
    /// Blender tarafında zaten bir bekçi var: üretici zarı ölçer ve %6'dan
    /// fazla sapmada durur. Ama o bekçi <b>Unity'yi göremez</b>. Biri
    /// Inspector'da <c>wingArea</c>'yı 18'e çekerse Blender bunu asla
    /// öğrenmez ve iki zincir sessizce ayrışır. Bu test o boşluğu kapatır:
    /// iki ucu birbirine bağlar.
    ///
    /// Karşılaştırılan şey <b>izdüşüm</b> alanıdır, yüzey alanı değil —
    /// fiziğin istediği o. Bu yüzden üretici zarı dihedral uygulanmadan
    /// önce ölçer ve sayıyı kataloğa yazar.
    /// </summary>
    public class KanatTests
    {
        private const string CatalogPath = "art/blend/kanat/catalog.json";
        private const string PrefabDir = "Assets/_Project/Art/Prefabs";

        /// <summary>Alan bütçesinin taşımaya razı olduğu sapma.</summary>
        private const float AreaTolerance = 0.06f;

        [Serializable] private class File_ { public List<Entry_> variants; }

        [Serializable]
        private class Entry_
        {
            public string name;
            public string state;
            public string tier;
            public float wing_area;
            public float span;
            public float span_nominal;
            public int tris_lod0;
            public int tris_lod1;
        }

        private static List<Entry_> ReadCatalog()
        {
            string path = Path.Combine(AssetCatalog.RepoRoot, CatalogPath);
            // Assert.Ignore DEGIL: katalog depoya girer, yani yoklugu bir
            // ortam sorunu degil bir HATA. Bu projede atlanan test dort kez
            // gecen test sanildi (ADR 0041/0043/0044).
            Assert.IsTrue(File.Exists(path), $"Kanat katalogu yok: {path}");
            var file = JsonUtility.FromJson<File_>(File.ReadAllText(path));
            Assert.IsNotNull(file?.variants, $"Katalog okunamadi: {path}");
            Assert.IsNotEmpty(file.variants, "Katalogda hic kanat yok.");
            return file.variants;
        }

        private static Entry_ Find(string state)
        {
            foreach (var v in ReadCatalog())
                if (v.state == state) return v;
            Assert.Fail($"Katalogda '{state}' durumunda kanat yok.");
            return null;
        }

        private static WindTuning Tuning()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:WindTuning"))
            {
                var w = AssetDatabase.LoadAssetAtPath<WindTuning>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (w != null) return w;
            }
            Assert.Fail("WindTuning varligi bulunamadi.");
            return null;
        }

        /// <summary>
        /// Açık kanadın <b>ölçülen</b> alanı, fiziğin kullandığı
        /// <c>wingArea</c> ile aynı olmalı.
        /// </summary>
        [Test]
        public void TheVisibleWingHasTheAreaThePhysicsFliesWith()
        {
            var acik = Find("open");
            float fizik = Tuning().wingArea;
            float sapma = Mathf.Abs(acik.wing_area - fizik) / fizik;

            Assert.Less(sapma, AreaTolerance,
                $"Kanat alani {acik.wing_area:0.00} m2 ama WindTuning.wingArea "
                + $"{fizik:0.00} m2 (sapma %{sapma * 100f:0.0}). Ya modeli "
                + "yeniden uret (tools/blender/gen_kanat.py) ya da ucus "
                + "butcesini yeni alanla olc — ikisi ayri kalamaz.");
        }

        /// <summary>
        /// Kırık kanat gerçekten alan kaybetmeli.
        ///
        /// İlk yazımda kırıkta çıtalar ve tüyler düşüyordu ama zar tamdı:
        /// katalog kırık kanat için de 15,00 m² diyordu. Yani hasar
        /// <b>görsel bir süstü</b> — kırık bir kanat sağlamıyla aynı
        /// fiziği taşırdı. Hasarın bir bedeli yoksa hasar yoktur.
        /// </summary>
        [Test]
        public void ABrokenWingActuallyLosesLiftingArea()
        {
            var acik = Find("open");
            var kirik = Find("broken");

            Assert.Less(kirik.wing_area, acik.wing_area * 0.95f,
                $"Kirik kanat {kirik.wing_area:0.00} m2, saglam "
                + $"{acik.wing_area:0.00} m2 — hasarin bir bedeli yok.");
            Assert.Less(kirik.span, acik.span * 0.95f,
                $"Kirik kanadin OLCULEN acikligi {kirik.span:0.00} m; "
                + "bir uc dusmusse bu sayi kucumeli. Katalog nominal "
                + "degeri yaziyorsa kirik kanat saglam gorunur.");
        }

        /// <summary>
        /// Katlı kanat sırtta taşınır: açık halin kabaca üçte biri kadar
        /// yer kaplamalı, yoksa kule merdiveninden çıkılamaz.
        /// </summary>
        [Test]
        public void TheFoldedWingFitsThroughTheTowerStair()
        {
            var katli = Find("folded");
            Assert.Less(katli.span, 3.2f,
                $"Katli kanat {katli.span:0.00} m — Galata kulesinin "
                + "helezon merdiveninden gecmez.");
        }

        /// <summary>
        /// LOD1 bir yedek olmalı, bir yokluk değil.
        ///
        /// İlk üretimde LOD1 yalnızca zardı: <b>4 üçgen</b>. 772'den 4'e
        /// düşmek bir merdiven değil, bir kaybolmadır — ve kanadın okunan
        /// şeyi zar değil, yelpaze gibi açılan çıta siluetidir. Render bu
        /// hatayı gösteremezdi çünkü render her zaman LOD0'ı çizer;
        /// yakalayan şey katalogdaki sayı oldu.
        /// </summary>
        [Test]
        public void TheWingLodKeepsItsSilhouette()
        {
            foreach (var v in ReadCatalog())
            {
                Assert.Greater(v.tris_lod1, v.tris_lod0 * 0.04f,
                    $"{v.name}: LOD1 {v.tris_lod1} ucgen, LOD0 {v.tris_lod0} — "
                    + "bu bir merdiven degil, bir yok olus.");
                Assert.Less(v.tris_lod1, v.tris_lod0 * 0.5f,
                    $"{v.name}: LOD1 {v.tris_lod1} ucgen, LOD0'in yarisindan "
                    + "fazlasi — kademe hic tasarruf etmiyor.");
            }
        }

        /// <summary>
        /// Üç durumun da prefab'ı var, T3 etiketli ve LOD grubu kurulu.
        ///
        /// T3 (Efsane) bir eksiklik değil bir <b>beyandır</b>: kanadın
        /// tarihî planı yok (ADR 0064). Etiket Graybox'a düşerse o beyan
        /// kaybolur — ve bu bir turda gerçekten oldu, çünkü katalog
        /// anahtarını yanlış yazmıştım ve boru hatti yine de
        /// "3 model yerlestirildi" dedi.
        /// </summary>
        [Test]
        public void EveryWingStateIsAPrefabTaggedAsLegend()
        {
            foreach (var v in ReadCatalog())
            {
                string path = $"{PrefabDir}/PF_{v.name}.prefab";
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(go, $"Prefab yok: {path}");

                var tag = go.GetComponent<HistoricalTag>();
                Assert.IsNotNull(tag, $"{path}: HistoricalTag yok.");
                Assert.AreEqual(HistoricalTier.Legend, tag.tier,
                    $"{path}: kanadin tarihi plani yoktur, etiketi T3 olmali "
                    + "(ADR 0064). Graybox'a dusmus.");
                Assert.IsNotEmpty(tag.sourceNote ?? "",
                    $"{path}: kaynak notu bos — plan olmadigini soyleyen sey o.");

                var lod = go.GetComponent<LODGroup>();
                Assert.IsNotNull(lod, $"{path}: LODGroup yok.");
                Assert.GreaterOrEqual(lod.lodCount, 2, $"{path}: tek kademe.");
            }
        }
    }
}
