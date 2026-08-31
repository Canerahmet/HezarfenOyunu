using System.Collections.Generic;
using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Kalabalık gerçekten çeşitli mi.</b>
    ///
    /// Bir kalabalığın "çeşitli" olduğunu göz kararıyla söylemek
    /// kolay ve yanıltıcıdır: yirmi kişiden ikisi tıpatıp aynıysa
    /// göz onu yakalar, ama ölçmeden hangi ikisi olduğunu
    /// bilemezsiniz. Evlerde aynı soruyu sorup 26 varyantın 418 kez
    /// tekrarlandığını ölçmüştük; burada da sayıyla soruluyor.
    /// </summary>
    public class InsanDNATests
    {
        private static List<InsanDNA> Ornek(int n)
        {
            var l = new List<InsanDNA>(n);
            for (int i = 0; i < n; i++) l.Add(InsanDNA.Uret(i * 7919 + 13));
            return l;
        }

        [Test]
        public void TheSameSeedIsAlwaysTheSamePerson()
        {
            var a = InsanDNA.Uret(4242);
            var b = InsanDNA.Uret(4242);
            Assert.AreEqual(a.olcek, b.olcek, 1e-6f);
            Assert.AreEqual(a.hiz, b.hiz, 1e-6f);
            Assert.AreEqual(a.yas, b.yas, 1e-6f);
            Assert.AreEqual(a.ton, b.ton,
                "Ayni tohum ayni kisiyi vermeli — sehir akisa alinip "
                + "yeniden kuruldugunda kimse baskasina donusmesin.");
        }

        [Test]
        public void HeightSpreadsLikeAPopulationNotAConstant()
        {
            var l = Ornek(600);
            float enAz = 9f, enCok = 0f, top = 0f;
            foreach (var d in l)
            {
                float boy = d.olcek * InsanDNA.TabanBoy;
                enAz = Mathf.Min(enAz, boy);
                enCok = Mathf.Max(enCok, boy);
                top += boy;
            }
            float ort = top / l.Count;

            // Yetiskin ortalamasi cocuklarla asagi cekilir. Ilk esik
            // 1,50-1,70 idi ve olcum 1,48 dedi: yas dagilimi sokaga
            // %38 cocuk koyuyordu. Dagilim duzeltildi; esik yerinde
            // kaldi cunku yanlis olan esik degil dagilimdi.
            Assert.That(ort, Is.InRange(1.50f, 1.70f),
                $"Ortalama boy {ort:0.00} m — donem nufusu icin sapkin.");
            Assert.Greater(enCok - enAz, 0.35f,
                $"Boy araligi {enCok - enAz:0.00} m — kalabalik hala "
                + "tek tip.");
            Assert.Less(enAz, 1.35f, "Hic cocuk yok.");
            Assert.Greater(enCok, 1.75f, "Hic uzun adam yok.");
        }

        [Test]
        public void TheOldWalkSlowerThanTheYoung()
        {
            var l = Ornek(400);
            float gencTop = 0f, yasliTop = 0f;
            int genc = 0, yasli = 0;
            foreach (var d in l)
            {
                if (d.yas < 0.3f) { gencTop += d.hiz; genc++; }
                else if (d.yas > 0.75f) { yasliTop += d.hiz; yasli++; }
            }
            Assert.Greater(genc, 20, "Yeterince genc ornek yok.");
            Assert.Greater(yasli, 5, "Yeterince yasli ornek yok.");
            Assert.Greater(gencTop / genc, yasliTop / yasli + 0.15f,
                "Yasli ile genc ayni hizda yuruyor — kalabaligin "
                + "ritmi tek duze olur.");
        }

        [Test]
        public void ClothingStaysInsideThePeriodPalette()
        {
            foreach (var d in Ornek(300))
            {
                Color.RGBToHSV(d.ton, out float h, out float s, out _);
                bool aile = (h < 0.16f) || (h > 0.55f && h < 0.68f);
                Assert.IsTrue(aile,
                    $"Ton {h:0.00} dort boya ailesinin disinda — "
                    + "17. yy sokaginda yesil ve mor yok.");
                Assert.Less(s, 0.42f,
                    $"Doygunluk {s:0.00} — pahali boya zengin isidir, "
                    + "sokak solgundur.");
            }
        }
    }
}
