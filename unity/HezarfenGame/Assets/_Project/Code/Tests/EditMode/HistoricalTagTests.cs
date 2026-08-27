using Hezarfen.Core;
using NUnit.Framework;
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
    }
}
