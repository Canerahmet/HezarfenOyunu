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
            public int kemik;
            public List<Kemik_> kemikler;
        }

        [Serializable]
        private class Kemik_
        {
            public string ad;
            public List<float> bas;
            public float uzunluk;
            public float kot_orani;
        }

        /// <summary>Unity Humanoid'in zorunlu saydığı kemikler.</summary>
        private static readonly string[] Zorunlu =
        {
            "Hips", "Spine", "Chest", "Neck", "Head",
            "LeftUpperArm", "LeftLowerArm", "LeftHand",
            "RightUpperArm", "RightLowerArm", "RightHand",
            "LeftUpperLeg", "LeftLowerLeg", "LeftFoot",
            "RightUpperLeg", "RightLowerLeg", "RightFoot",
        };

        private static Kemik_ Kemik(Entry_ v, string ad)
        {
            if (v.kemikler == null) return null;
            foreach (var k in v.kemikler) if (k.ad == ad) return k;
            return null;
        }

        private static Entry_ Rigli()
        {
            foreach (var v in Katalog())
                if (v.kemikler != null && v.kemikler.Count > 0) return v;
            Assert.Fail("Katalogda kemik raporu tasiyan varyant yok.");
            return null;
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
        /// <summary>Humanoid'in istediği her kemik var.</summary>
        [Test]
        public void TheSkeletonHasEveryBoneUnityHumanoidRequires()
        {
            var v = Rigli();
            foreach (string ad in Zorunlu)
                Assert.IsNotNull(Kemik(v, ad),
                    $"'{ad}' kemigi yok — Unity avatari kurmaz ve hata "
                    + "mesaji hangi kemigin eksik oldugunu SOYLEMEZ.");
        }

        /// <summary>
        /// Eklemler doğru sırada ve doğru kotta.
        ///
        /// Bu test bir hatanın anısıdır. Kol merkez çizgisi, gövde
        /// ekseninden uzak noktaları arıyordu ve o filtre parmak uçlarının
        /// 55 cm altında <b>ayakları</b> da yakalıyordu. Çizgi omuzdan
        /// ayağa iniyor, %82'sine yürüyünce bilek <b>ayak bileği
        /// hizasında</b> çıkıyordu: dirsek %42,7, bilek %14,6. İskelet
        /// kurulmuş, hiçbir şey hata vermemişti.
        /// </summary>
        [Test]
        public void TheJointsSitWhereHumanJointsSit()
        {
            var v = Rigli();
            foreach (string yan in new[] { "Left", "Right" })
            {
                var omuz = Kemik(v, yan + "UpperArm");
                var dirsek = Kemik(v, yan + "LowerArm");
                var bilek = Kemik(v, yan + "Hand");
                Assert.IsNotNull(omuz); Assert.IsNotNull(dirsek);
                Assert.IsNotNull(bilek);
                Assert.Greater(omuz.kot_orani, dirsek.kot_orani,
                    $"{yan}: omuz dirsekten yukarida olmali.");
                Assert.Greater(dirsek.kot_orani, bilek.kot_orani,
                    $"{yan}: dirsek bilekten yukarida olmali.");
                Assert.That(dirsek.kot_orani, Is.InRange(0.56f, 0.70f),
                    $"{yan} dirsek %{dirsek.kot_orani * 100:0.0} kotta — "
                    + "A-pozunda %58-68 arasinda olur.");
                Assert.Greater(bilek.kot_orani, 0.35f,
                    $"{yan} bilek %{bilek.kot_orani * 100:0.0} kotta — bu "
                    + "kadar asagisi ayak bilegi hizasidir, el degil.");

                var kalca = Kemik(v, yan + "UpperLeg");
                var diz = Kemik(v, yan + "LowerLeg");
                var ayak = Kemik(v, yan + "Foot");
                Assert.Greater(kalca.kot_orani, diz.kot_orani);
                Assert.Greater(diz.kot_orani, ayak.kot_orani);
                Assert.That(diz.kot_orani, Is.InRange(0.24f, 0.32f),
                    $"{yan} diz %{diz.kot_orani * 100:0.0} kotta.");
            }
        }

        /// <summary>
        /// Omurga pürüzsüz: baş, boyunun tam üstünde durur.
        ///
        /// Boyun ve baş eklemleri "gövde" filtresiyle ölçülürken o filtre
        /// <b>trapez kasını</b> da sayıyordu ve dilim boyun değil omuz
        /// platosu oluyordu. Sonuç: boyun 2,5 cm önde, kafatası 5,9 cm
        /// arkada — 15 cm kotta 8,4 cm'lik bir kırık.
        /// </summary>
        [Test]
        public void TheSpineIsSmoothFromHipsToHead()
        {
            var v = Rigli();
            float en = float.MaxValue, boy = float.MinValue;
            foreach (string ad in new[] { "Hips", "Spine", "Chest",
                                          "UpperChest", "Neck", "Head" })
            {
                var k = Kemik(v, ad);
                if (k == null || k.bas == null || k.bas.Count < 3) continue;
                en = Mathf.Min(en, k.bas[1]);
                boy = Mathf.Max(boy, k.bas[1]);
                Assert.Less(Mathf.Abs(k.bas[0]), 0.005f,
                    $"{ad} gövde ekseninde degil (x={k.bas[0]:0.000}).");
            }
            Assert.Less(boy - en, 0.030f,
                $"Omurga zincirinin on-arka sapmasi {(boy - en) * 100f:0.0} cm "
                + "— 3 cm'den fazlasi bir kirik demektir.");
        }

        /// <summary>Sol ve sağ simetrik.</summary>
        [Test]
        public void TheSkeletonIsSymmetric()
        {
            var v = Rigli();
            foreach (string ad in new[] { "UpperArm", "LowerArm", "Hand",
                                          "UpperLeg", "LowerLeg", "Foot" })
            {
                var l = Kemik(v, "Left" + ad);
                var r = Kemik(v, "Right" + ad);
                if (l == null || r == null) continue;
                Assert.AreEqual(l.kot_orani, r.kot_orani, 0.01f,
                    $"{ad}: sol %{l.kot_orani * 100:0.0}, sag "
                    + $"%{r.kot_orani * 100:0.0} — govde simetriktir.");
                Assert.AreEqual(l.bas[0], -r.bas[0], 0.02f,
                    $"{ad}: sol/sag x aynalanmiyor.");
            }
        }

        // ---------------------------------------------------------- animasyon

        [Serializable] private class AnimFile { public List<AnimKlip> klipler; }

        [Serializable]
        private class AnimKlip
        {
            public string ad;
            public string tur;
            public int kare;
            public float sure;
            public float hiz;
            public bool dongu;
            public float kayma_cm;
        }

        private static List<AnimKlip> Klipler()
        {
            string yol = Path.Combine(AssetCatalog.RepoRoot,
                                      "art/blend/karakter/animasyon.json");
            Assert.IsTrue(File.Exists(yol), $"Animasyon katalogu yok: {yol}");
            var f = JsonUtility.FromJson<AnimFile>(File.ReadAllText(yol));
            Assert.IsNotNull(f?.klipler, "Animasyon katalogu okunamadi.");
            Assert.IsNotEmpty(f.klipler, "Katalogda hic klip yok.");
            return f.klipler;
        }

        /// <summary>
        /// Yere basan ayak kaymıyor.
        ///
        /// Bir yürüyüş döngüsünün "iyi göründüğü" bir görüştür; <b>yanlış
        /// olduğu</b> bir ölçümdür ve tek sayıya iner. Adım uzunluğu ile
        /// hız tutmuyorsa ayaklar paten kayar — oyun animasyonundaki en
        /// görünür kusur, ve gözle bakınca "biraz tuhaf" diye geçiştirilir.
        /// </summary>
        [Test]
        public void PlantedFeetDoNotSkate()
        {
            foreach (var k in Klipler())
            {
                if (k.tur != "dongu" || k.hiz < 0.01f) continue;
                Assert.Less(k.kayma_cm, 5.0f,
                    $"{k.ad}: ayak kaymasi {k.kayma_cm:0.0} cm. Adim "
                    + $"uzunlugu {k.hiz:0.0} m/s hiziyla tutmuyor.");
            }
        }

        /// <summary>
        /// Yürüyüş temposu bir insana ait.
        ///
        /// Kaymayı sıfırlamak tek başına yetmez: adım uzunluğunu serbest
        /// bırakıp süreyi uzatarak da sıfırlanır, ve o zaman karakter
        /// 74 adım/dakika ile bir cenaze temposunda yürür. Gerçekten oldu.
        /// </summary>
        [Test]
        public void TheWalkCadenceIsHuman()
        {
            foreach (var k in Klipler())
            {
                if (k.tur != "dongu" || k.hiz < 0.01f || k.sure < 0.01f)
                    continue;
                float tempo = 120f / k.sure;      // dongu = iki adim
                Assert.That(tempo, Is.InRange(85f, 185f),
                    $"{k.ad}: {tempo:0} adim/dk — insan 90-170 arasindadir.");
            }
        }

        /// <summary>Planın istediği klipler eksiksiz.</summary>
        [Test]
        public void EveryClipThePlanAsksForExists()
        {
            var var_ = new HashSet<string>();
            foreach (var k in Klipler()) var_.Add(k.ad);
            // Plan Bolum 10: locomotion, tirmanma, kusanma, kalkis,
            // suzulus (blend agaci), inis/yuvarlanma, cakilma.
            string[] zorunlu =
            {
                "Durus", "Yurume", "Kosma", "Merdiven", "Kusanma",
                "Kalkis", "Suzulme", "Inis", "Cakilma",
            };
            foreach (string z in zorunlu)
                Assert.IsTrue(var_.Contains(z), $"'{z}' klibi yok.");
            // Suzulus blend agaci: pitch ve roll uc pozlari.
            foreach (string z in new[] { "Suzulme_Burun", "Suzulme_Kuyruk",
                                         "Suzulme_Sol", "Suzulme_Sag" })
                Assert.IsTrue(var_.Contains(z),
                    $"'{z}' yok — pitch/roll blend agaci kurulamaz.");
        }
    }
}
