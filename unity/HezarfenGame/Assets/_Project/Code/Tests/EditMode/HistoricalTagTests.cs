using System.Collections.Generic;
using System.IO;
using Hezarfen.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// T1 etiketinin kaynaksız kalamayacağını kilitler. Bu, oyunun tarihsel
    /// dürüstlük iddiasının makineyle korunan tek parçası — insan disiplinine bırakılmaz.
    /// </summary>
    public class HistoricalTagTests
    {
        private GameObject go;
        private HistoricalTag tag;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("TagHost");
            tag = go.AddComponent<HistoricalTag>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        [Test]
        public void DefaultTier_IsGraybox()
        {
            // Yeni bir ögenin sessizce "belgeli" sayilmasi en tehlikeli varsayilan olurdu.
            Assert.AreEqual(HistoricalTier.Graybox, tag.tier);
        }

        [Test]
        public void Documented_WithoutSource_IsInvalid()
        {
            tag.tier = HistoricalTier.Documented;
            tag.sourceNote = "";
            Assert.IsTrue(tag.RequiresSource);
            Assert.IsFalse(tag.IsValid, "T1 kaynak notu olmadan gecerli sayilmamali.");
        }

        [Test]
        public void Documented_WithWhitespaceSource_IsInvalid()
        {
            tag.tier = HistoricalTier.Documented;
            tag.sourceNote = "   \n\t ";
            Assert.IsFalse(tag.IsValid, "Bosluk karakterleri kaynak sayilmamali.");
        }

        [Test]
        public void Documented_WithSource_IsValid()
        {
            tag.tier = HistoricalTier.Documented;
            tag.sourceNote = "docs/RESEARCH.md Bolum 3 - Galata Kulesi 1632 durumu";
            Assert.IsTrue(tag.IsValid);
        }

        [Test]
        public void NonDocumentedTiers_DoNotRequireSource()
        {
            foreach (var tier in new[] { HistoricalTier.Graybox, HistoricalTier.Reconstruction, HistoricalTier.Legend })
            {
                tag.tier = tier;
                tag.sourceNote = "";
                Assert.IsFalse(tag.RequiresSource, $"{tier} kaynak zorunlulugu tasimamali.");
                Assert.IsTrue(tag.IsValid, $"{tier} kaynaksiz da gecerli olmali.");
            }
        }

        /// <summary>
        /// <b>Sahnelerdeki etiketler de kaynaklı olsun.</b>
        ///
        /// ## Neden gerekti
        ///
        /// Yukarıdaki testler <c>HistoricalTag</c> <b>sınıfını</b>
        /// sınıyor: T1 kaynaksızsa <c>IsValid</c> false döner. Ama
        /// sahnelerdeki <b>veriyi</b> kimse sormuyordu — ölçüldü, on beş
        /// sahnede <b>989</b> etiket var ve hiçbiri okunmuyordu.
        /// CLAUDE.md kuralı sahneler için: *"Her yeni sahne öğesine
        /// HistoricalTag ata; T1 için RESEARCH.md'den kaynak satırı
        /// yaz."* Sınıfın doğru cevap verebilmesi, verinin doğru
        /// olduğunu göstermez.
        ///
        /// Bugünkü sayım temiz — <b>kaynaksız tek bir T1 yok</b>. Test
        /// bunu kilitliyor: yeni bir T1 kaynaksız eklendiğinde kırmızı
        /// döner.
        ///
        /// ## Neden dosyadan
        ///
        /// On beş sahnenin toplamı 150 MB'ı geçiyor; her koşumda açmak
        /// süiti dakikalarca uzatır. Etiket sahnede düz alan olarak
        /// duruyor (<c>tier:</c> ve <c>sourceNote:</c>) ve ölçü orada
        /// okunabiliyor. Betiğin GUID'i elle yazılmıyor, varlık
        /// veritabanından türetiliyor — bir sayının iki sahibi olmasın.
        /// </summary>
        [Test]
        public void EveryDocumentedTagInTheScenesCitesASource()
        {
            string guid = null;
            foreach (string g in AssetDatabase.FindAssets("t:MonoScript HistoricalTag"))
            {
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(
                    AssetDatabase.GUIDToAssetPath(g));
                if (ms != null && ms.GetClass() == typeof(HistoricalTag))
                { guid = g; break; }
            }
            Assert.IsNotNull(guid, "HistoricalTag betigi bulunamadi.");

            const string kok = "Assets/_Project/Scenes";
            Assert.IsTrue(Directory.Exists(kok), $"{kok} yok.");

            int etiket = 0;
            var kusur = new List<string>();
            foreach (string yol in Directory.GetFiles(kok, "*.unity",
                                                     SearchOption.AllDirectories))
            {
                var satir = File.ReadAllLines(yol);
                for (int i = 0; i < satir.Length; i++)
                {
                    if (satir[i].IndexOf(guid, System.StringComparison.Ordinal) < 0)
                        continue;
                    etiket++;
                    int tier = -1;
                    string not = null;
                    for (int j = i; j < System.Math.Min(i + 8, satir.Length); j++)
                    {
                        if (satir[j].StartsWith("  tier:"))
                            int.TryParse(satir[j].Substring(7).Trim(), out tier);
                        if (satir[j].StartsWith("  sourceNote:"))
                            not = satir[j].Substring(13).Trim();
                    }
                    if (tier == (int)HistoricalTier.Documented
                        && string.IsNullOrWhiteSpace(not))
                        kusur.Add($"{Path.GetFileName(yol)}:{i + 1}");
                }
            }

            Assert.Greater(etiket, 0,
                "Sahnelerde hic HistoricalTag bulunamadi — olcu bos "
                + "donuyor, yani test hicbir seyi korumuyor.");
            Assert.IsEmpty(kusur,
                "T1 (Documented) etiketi kaynak notu olmadan duruyor. "
                + "Oyunun tarihsel durustluk iddiasi tam olarak bu "
                + "satirda yasiyor; RESEARCH.md'den kaynak yaz.\n  "
                + string.Join("\n  ", kusur));
        }
    }
}