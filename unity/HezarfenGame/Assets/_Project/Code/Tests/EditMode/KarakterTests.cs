using System;
using System.Collections.Generic;
using System.IO;
using Hezarfen.Editor.Pipeline;
using Hezarfen.Player;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Şehri onaylayan figür ile şehirde yürüyen adam aynı kişi mi?</b>
    ///
    /// Bu projenin bütün inceleme paketleri karede duran <b>1,70 m'lik bir
    /// ölçek figürüne</b> göre yargılandı: 36 landmark, 142 mahalle,
    /// 12 248 ev. O figür bir çizim değil, bir <b>sözleşme</b>. Karakter
    /// modeli başka boyda olursa şehir yanlış ölçekte kurulmuş olur — ya da
    /// daha kötüsü, şehir doğru ama karakter yanlış olur ve kimse fark
    /// etmez, çünkü ikisi hiçbir yerde yan yana gelmez.
    ///
    /// Bu testler o sözleşmeyi üç yere birden bağlar: Blender kataloğu,
    /// <see cref="WalkController.eyeHeight"/> ve <c>WalkSpawner</c>'ın
    /// CharacterController kapsülü.
    ///
    /// ## Neden gerekliydi
    ///
    /// Yazıldığı gün üçü de ayrışmıştı ve hiçbiri hata vermiyordu:
    /// model 1,70 m, kapsül <b>1,80 m</b>, göz <b>1,70 m</b>. Göz
    /// alanının yanındaki not "1,70 m: ölçü figürüyle aynı" diyordu — nota
    /// bakınca doğru, sayıya bakınca yanlış: 1,70 o figürün <b>boyu</b>,
    /// gözü değil. Yani gezgin 1,81 m'lik bir adamın gözünden bakıyor,
    /// 1,80 m'lik bir kapsülü sürüyor ve 1,70 m'lik bir adamı temsil
    /// ediyordu. <b>Bir yorum, bir sayının yanlış olduğunu gizleyebilir.</b>
    /// </summary>
    public class KarakterTests
    {
        private const string CatalogPath = "art/blend/karakter/catalog.json";

        /// <summary>Şehrin onaylandığı ölçek figürünün boyu (m).</summary>
        private const float OlcekFiguru = 1.70f;

        [Serializable] private class File_ { public List<Entry_> variants; }

        [Serializable]
        private class Entry_
        {
            public string name;
            public string state;
            public float boy;
            public float bas_boy;
            public float bas_orani;
            public float omuz_genisligi;
            public float boyun_genisligi;
            public float etek_kotu;
            public bool dizlik;
            public int tris_lod0;
            public int tris_lod1;
        }

        private static List<Entry_> Katalog()
        {
            string path = Path.Combine(AssetCatalog.RepoRoot, CatalogPath);
            // Assert.Ignore DEGIL — katalog depoya girer, yoklugu bir hatadir.
            Assert.IsTrue(File.Exists(path), $"Karakter katalogu yok: {path}");
            var f = JsonUtility.FromJson<File_>(File.ReadAllText(path));
            Assert.IsNotNull(f?.variants, $"Katalog okunamadi: {path}");
            Assert.IsNotEmpty(f.variants, "Katalogda hic karakter yok.");
            return f.variants;
        }

        private static Entry_ Bul(string state)
        {
            foreach (var v in Katalog())
                if (v.state == state) return v;
            Assert.Fail($"Katalogda '{state}' durumunda karakter yok.");
            return null;
        }

        /// <summary>Taban gövde, şehrin onaylandığı figürle aynı boyda.</summary>
        [Test]
        public void TheCharacterIsTheSameHeightAsTheFigureTheCityWasApprovedAgainst()
        {
            var taban = Bul("base");
            Assert.AreEqual(OlcekFiguru, taban.boy, 0.01f,
                $"Karakter {taban.boy:0.000} m ama sehir {OlcekFiguru} m'lik "
                + "olcek figurune gore onaylandi. Ikisinden biri degismeli ve "
                + "sehri degistirmek 36 landmark + 142 mahalle demek.");
        }

        /// <summary>Oranlar bir yetişkine ait.</summary>
        [Test]
        public void TheBodyHasAdultHumanProportions()
        {
            var t = Bul("base");
            Assert.That(t.bas_orani, Is.InRange(6.5f, 8.5f),
                $"bas orani 1/{t.bas_orani} — yetiskin 1/7 ile 1/8 arasindadir.");
            Assert.That(t.omuz_genisligi, Is.InRange(0.36f, 0.50f),
                $"omuz {t.omuz_genisligi:0.000} m degil, 0,38-0,48 m olmali.");
            Assert.Less(t.boyun_genisligi, t.omuz_genisligi * 0.62f,
                "boyun omuzdan belirgin DAR olmali; degilse olcum boynu "
                + "bulamamis demektir (bir kez kafatasinin tepesini buldu).");
        }

        /// <summary>
        /// Gezginin gözü, karakterin gözüyle aynı kotta.
        ///
        /// Göz tepe ile çene arasının ortasındadır: <c>boy − baş/2</c>.
        /// Kitaptan bir oran değil, modelin kendi ölçüsünden türetiliyor.
        /// </summary>
        [Test]
        public void TheWalkerLooksFromTheCharactersEyes()
        {
            var t = Bul("base");
            float goz = t.boy - t.bas_boy * 0.5f;
            var wc = new GameObject("gecici").AddComponent<WalkController>();
            float ayarli = wc.eyeHeight;
            UnityEngine.Object.DestroyImmediate(wc.gameObject);

            Assert.AreEqual(goz, ayarli, 0.05f,
                $"Karakterin gozu {goz:0.00} m'de ama WalkController "
                + $"{ayarli:0.00} m'den bakiyor. 1,70 m FIGURUN BOYUDUR, "
                + "gozu degil — bu ayrimin karistirilmasi gezgini 1,81 m'lik "
                + "bir adam yapmisti.");
        }

        /// <summary>
        /// Uçuş varyantının eteği dizin ÜSTÜNDE biter.
        ///
        /// Rålamb plaka 50'de kaftan dizde biter ve dizlik bandı tam altında
        /// durur. İlk üretimde etek 0,37 m'ye iniyordu, diz ise 0,48 m'de:
        /// dizlik üretiliyor ama eteğin altında kalıp <b>hiç
        /// görünmüyordu</b>. Üretilen ama görünmeyen bir öğe, olmayan bir
        /// öğedir — ve sayı onu gösterdi, render değil.
        /// </summary>
        [Test]
        public void TheFlightCoatEndsAboveTheKneeSoTheKneeBandShows()
        {
            var ucus = Bul("dressed");
            foreach (var v in Katalog())
                if (v.dizlik) { ucus = v; break; }

            Assert.IsTrue(ucus.dizlik,
                "Ucus varyantinda dizlik yok — plan Bolum 10 istiyor ve "
                + "plaka 50'de gozlenmis bir ogedir.");
            // Diz, boyun ~%28,5'idir (kiyafet_kit.DIZ_ORAN).
            float diz = ucus.boy * 0.285f;
            Assert.Greater(ucus.etek_kotu, diz,
                $"Etek {ucus.etek_kotu:0.00} m'de bitiyor, diz {diz:0.00} m'de "
                + "— etek dizi ortuyor ve dizlik gorunmuyor.");
        }

        /// <summary>Sivil varyant uzun, uçuş varyantı kısa: boy işi söyler.</summary>
        [Test]
        public void TheSittingMansCoatIsLongerThanTheWorkingMans()
        {
            Entry_ uzun = null, kisa = null;
            foreach (var v in Katalog())
            {
                if (v.state != "dressed") continue;
                if (v.dizlik) kisa = v; else uzun = v;
            }
            Assert.IsNotNull(uzun, "Sivil (uzun entari) varyanti yok.");
            Assert.IsNotNull(kisa, "Ucus (kisa entari) varyanti yok.");
            Assert.Less(uzun.etek_kotu, kisa.etek_kotu,
                $"Sivil etek {uzun.etek_kotu:0.00} m, ucus {kisa.etek_kotu:0.00} m "
                + "— plakalarda CALISAN adamin entarisi kisadir; ikisi ayni "
                + "boydaysa ayrimin anlami yok.");
        }

        /// <summary>Giyinik karakter, çıplaktan makul ölçüde büyük.</summary>
        [Test]
        public void ClothingAddsAHatsWorthOfHeightAndNoMore()
        {
            var taban = Bul("base");
            foreach (var v in Katalog())
            {
                if (v.state != "dressed") continue;
                Assert.Greater(v.boy, taban.boy,
                    $"{v.name}: giyinik boy {v.boy:0.000} m, ciplak "
                    + $"{taban.boy:0.000} m — sarik hic yukseklik eklememis, "
                    + "yani basin ustunde degil.");
                Assert.Less(v.boy, taban.boy * 1.10f,
                    $"{v.name}: {v.boy:0.000} m — bir sarik 17 cm'den fazla "
                    + "eklemez; bir parca govdeden kopmus olmali.");
            }
        }
    }
}
