using System;
using System.Collections.Generic;
using System.IO;
using Hezarfen.Core;
using Hezarfen.Editor.Gis;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Landmark iddialarını kilitler — Faz 3'ün kabul ölçütünün test tarafı.
    ///
    /// ## Landmark'ta sınanan şey TİPOLOJİ DEĞİL, BELGE
    ///
    /// Kamusal kit testleri bir yapının *tipine uygun* olduğunu ölçer
    /// (imaretin en az üç gözü olmalı, su terazisi baca gibi ince olmamalı).
    /// Landmark başka bir şey: **tek** ve **belgeli**. Burada sınanan, modelin
    /// kaynağın söylediği sayıyı taşıyıp taşımadığıdır.
    ///
    /// Galata Kulesi'nde bunun en keskin hâli şu: <b>1632 kulesi bugünkünden
    /// alçaktır.</b> Bugünkü 62,59 m, 1831'de eklenen sofa ve 1875'te eklenen
    /// sekizgen gözlem katlarını içerir; ikisi de 1632'de yoktur. Bir gün biri
    /// "kule alçak kalmış" deyip boyu uzatırsa, bu test onu durdurur ve
    /// nedenini söyler.
    /// </summary>
    public class LandmarkTests
    {
        private const string CatalogPath = "art/blend/landmark/catalog.json";
        private const string PrefabDir = "Assets/_Project/Art/Prefabs";

        /// <summary>Bugünkü kule (m) — model bunun ALTINDA kalmak zorunda.</summary>
        private const float TodayTotalH = 62.59f;

        /// <summary>Belgeli dış çap (m) — TDV.</summary>
        private const float OuterD = 16.45f;

        [Serializable] private class Variant
        {
            public string name, prefab, tier, accuracy, source, crown, status, kind, plan,
                          material;
            public int storeys, minarets, sherefe_each, half_domes, portico_bays,
                       sherefe_total;
            public string roof;
            public float hall, minaret_h;
            public bool double_portico, acik, walls, eyvan, roofed;
            public int columns, hucre, baca, arch_sarayburnu, arch_ahirkapi,
                       stone_tiers, towers, piers;
            public float cumba, face_deg, harim_w, harim_d, wall_h;
            public float height, shaft_d, shaft_h, cone_h, outer_d, width, depth, opening,
                         above_water, dome_d, dome_crown_z, measured_dome_d,
                         measured_crown_z, podium_h, minaret_top;
            // Ayasofya (ADR 0045)
            public float dome_rise, rise_ratio, dome_d_in_ns, dome_d_in_ew,
                         qibla_offset_deg, minaret_r_thin, minaret_r_thick;
            public int dome_ribs, dome_windows, exedrae_interior, brick_minarets;
            public bool sadirvan, turbe_of_mustafa;
            // Sultanahmet (ADR 0047)
            public float dome_d_in, half_dome_d, pier_d,
                         minaret_h_tall, minaret_h_short;
            public int exedrae;
            // Fatih Camii, 1766 oncesi ozgun sema (ADR 0048)
            public int side_domes, side_domes_total, court_columns, court_gates;
            public float aisle_h;
            // Kara surlari (ADR 0049)
            public int tower_count;
            public float body_h, jut, section_total, moat_w, moat_d;
            // Yedikule ve kara kapisi (ADR 0050)
            public int round_towers, gate_arches;
            public float area_m2, radius, height_tower;
            // Beyazit (ADR 0051) — `dome_windows` YUKARIDA (Ayasofya) zaten
            // var; ikinci kez bildirmek CS0102 verir ve o hatayla test
            // kosumu ESKI assembly uzerinden YESIL doner.
            public int half_dome_windows, tabhane_cells, tabhane_cells_total;
            public float minaret_span, wing_len;
            public bool sadirvan_dome;
            // Bedestenler (ADR 0053)
            public int cols, rows, domes, doors;
            public float bay_w, bay_d;
            // Padisah turbeleri (ADR 0054)
            public bool revak, marble, double_shell;
            public float face_spread, footprint_x;
            public int sides;
            // Iskele (ADR 0055)
            public float length, deck_z;
            public int piles;
        }

        [Serializable] private class Catalog { public Variant[] variants; }

        private static Variant[] Load()
        {
            string root = TerrainImporter.RepositoryRoot();
            Assert.IsNotNull(root, "Depo kokü bulunamadi.");
            string path = Path.Combine(root,
                CatalogPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(path),
                $"{CatalogPath} yok — once gen_galata_kulesi.py");
            var cat = JsonUtility.FromJson<Catalog>(File.ReadAllText(path));
            Assert.IsNotNull(cat?.variants, "Katalog bos.");
            Assert.Greater(cat.variants.Length, 0, "Katalogda varyant yok.");
            return cat.variants;
        }

        /// <summary>
        /// Katalog Faz 3'ün BUTUN landmark'larini tasir; kuleye ait iddialar
        /// yalnizca kuleyi baglar. Ilk yazimda suzgec yoktu ve sur burcu
        /// "govde capi 16,45 m olmali" testine takildi — dogru test, yanlis
        /// nesne.
        /// </summary>
        private static System.Collections.Generic.List<Variant> OfKind(string kind)
        {
            var list = new System.Collections.Generic.List<Variant>();
            foreach (var v in Load()) if (v.kind == kind) list.Add(v);
            Assert.Greater(list.Count, 0, $"Katalogda '{kind}' turu yok.");
            return list;
        }

        /// <summary>
        /// Tek bir yapıya ait iddialar için <b>ada göre</b> süzgeç.
        ///
        /// <see cref="OfKind"/> bir tür süzgecidir ve bir tür <b>birden çok
        /// yapı</b> içerebilir: Süleymaniye eklenince Üsküdar Mihrimah'ın
        /// iddiaları (11,40 m kubbe, üç yarım kubbe, çifte minare) ona da
        /// uygulandı ve dört test birden patladı.
        ///
        /// Doğru test, yanlış nesne — katalogda bir kez düşülmüş tuzağın
        /// (sur burcu "gövde çapı 16,45 m" testine takılmıştı) bir kat
        /// derini. Bir yapıya özgü sayı, o yapının <b>adıyla</b> aranır.
        /// </summary>
        private static Variant Named(string name)
        {
            foreach (var v in Load()) if (v.name == name) return v;
            Assert.Fail($"Katalogda '{name}' yok.");
            return null;
        }

        /// <summary>
        /// <b>1632 kulesi bugünkünden ALÇAK.</b>
        ///
        /// Zincir: II. Mahmud 1831'de 32,60 m'den yukarısını yıktırdı; o kot
        /// 1794 yangını onarımında zaten 1,90 m alçaltılmıştı → 1632'de kâgir
        /// gövde ~34,5 m. Üstüne kurşun kaplı külah. Bugünkü 62,59 m ise 1831
        /// sofasını ve 1875 sekizgen katlarını da içerir.
        /// </summary>
        [Test]
        public void GalataTowerIsShorterThanTodays()
        {
            foreach (var v in OfKind("kule"))
            {
                Assert.Less(v.height, TodayTotalH,
                    $"{v.name} {v.height:F1} m — bugunku kule {TodayTotalH} m ve "
                    + "1632 ONDAN ALCAK olmali. 1831 sofasi ve 1875 sekizgen "
                    + "gozlem katlari 1632'de YOK (RESEARCH.md 5.1).");
                // Alt sinir da var: govde tek basina ~34,5 m, ustune kulah.
                Assert.Greater(v.height, 38f,
                    $"{v.name} {v.height:F1} m — kagir govde zaten ~34,5 m ve "
                    + "Evliya Celebi tepesinde kursun kapli bir kulah tarif eder.");
            }
        }

        /// <summary>
        /// Dış çap **ölçülmüş** bir sayıdır (16,45 m) ve modelin taşıdığı
        /// gövde çapı odur.
        ///
        /// Ölçülen şey ayak izi DEĞİL gövdedir: saçaklı varyantta külah
        /// 0,95 m taşıyor ve ayak izi 18,35 m çıkıyor. Üreticinin kendi
        /// denetimi ilk yazımda tam bu yüzden haksız yere hata verdi.
        /// </summary>
        [Test]
        public void GalataTowerCarriesTheMeasuredDiameter()
        {
            foreach (var v in OfKind("kule"))
            {
                Assert.AreEqual(OuterD, v.shaft_d, 0.05f,
                    $"{v.name} govde capi {v.shaft_d:F2} m — belgeli deger "
                    + $"{OuterD} m (TDV).");
                Assert.AreEqual(OuterD, v.outer_d, 1e-3f,
                    "Katalogdaki belgeli cap degismis.");
            }
        }

        /// <summary>
        /// Her landmark **doğruluk basamağını yazmak zorundadır** (PLAN §8.1).
        /// Kaynaksız bir "gerçeğe uygun" model, yanlış olduğunda kimsenin fark
        /// edemeyeceği bir hatadır.
        /// </summary>
        [Test]
        public void EveryLandmarkDeclaresItsAccuracyStep()
        {
            foreach (var v in Load())
            {
                Assert.AreEqual("T1", v.tier, $"{v.name} landmark, T1 olmali.");
                StringAssert.IsMatch("^D[123]$", v.accuracy ?? "",
                    $"{v.name} dogruluk basamagi yazili degil (D1/D2/D3).");

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{PrefabDir}/{v.prefab}.prefab");
                Assert.IsNotNull(prefab, $"{v.prefab} yok.");
                var tag = prefab.GetComponent<HistoricalTag>();
                Assert.IsNotNull(tag, $"{v.prefab} HistoricalTag tasimiyor.");
                Assert.AreEqual(HistoricalTier.Documented, tag.tier,
                    $"{v.prefab} landmark: T1 (Documented) olmali.");
                StringAssert.Contains(v.accuracy, tag.sourceNote,
                    "Dogruluk basamagi prefabin kaynak notunda da yazmali — "
                    + "sahnede duran sey odur.");
            }
        }

        /// <summary>
        /// 1632'de OLMAYANLAR kaynak notunda **adıyla** anılmalı.
        ///
        /// Bir landmark'ta asıl hata eklemektir: bugünkü kuleye bakıp
        /// modellemek, 1831'in sofasını ve 1875'in gözlem katlarını 1632'ye
        /// taşımak demektir. Kontrol listesi notta yaşamazsa kimse
        /// hatırlamaz.
        /// </summary>
        [Test]
        public void The1632AbsencesAreNamedInTheNote()
        {
            foreach (var v in OfKind("kule"))
                foreach (string must in new[] { "1831", "1875", "1832" })
                    StringAssert.Contains(must, v.source ?? "",
                        $"{v.name} kaynak notu {must} eklentisinin YOKLUGUNU "
                        + "anmiyor (RESEARCH.md 5.1e).");
        }

        /// <summary>
        /// Galata Kulesi <b>dünya orijinindedir</b> — ve bu bir tercih değil,
        /// koordinat sisteminin tanımıdır (ADR 0007: dünya orijini = Galata
        /// Kulesi tabanı, 28,974017 D / 41,025637 K).
        ///
        /// Kule kayarsa şehrin tamamı kayar. Test onu bir metreye kilitliyor.
        /// </summary>
        [Test]
        public void GalataTowerStandsAtTheWorldOrigin()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                Hezarfen.Editor.Gis.LandmarkPlacer.WorldScene,
                UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                GameObject host = null, terrainGo = null;
                foreach (var go in scene.GetRootGameObjects())
                {
                    if (go.name == Hezarfen.Editor.Gis.LandmarkPlacer.RootName) host = go;
                    if (go.name == "TR_Istanbul") terrainGo = go;
                }
                Assert.IsNotNull(host,
                    "LANDMARK_1632 yok. Menu: Hezarfen -> GIS -> "
                    + "Landmark'lari sahneye yerlestir");

                Transform kule = null;
                foreach (Transform t in host.transform)
                    if (t.name.StartsWith("PF_GalataKulesi")) { kule = t; break; }
                Assert.IsNotNull(kule, "Galata Kulesi sahnede yok.");

                Vector3 p = kule.position;
                Assert.Less(Mathf.Abs(p.x), 1.0f,
                    $"Kule x={p.x:F2} — dunya orijini KULENIN TABANIDIR (ADR 0007).");
                Assert.Less(Mathf.Abs(p.z), 1.0f,
                    $"Kule z={p.z:F2} — dunya orijini KULENIN TABANIDIR (ADR 0007).");

                // Araziye OTURMALI: ne havada ne gomulu.
                var terrain = terrainGo != null ? terrainGo.GetComponent<Terrain>() : null;
                if (terrain != null)
                {
                    float ground = terrain.SampleHeight(p) + terrain.transform.position.y;
                    Assert.Less(Mathf.Abs(p.y - ground), 3.0f,
                        $"Kule tabani {p.y:F1} m, arazi {ground:F1} m — "
                        + "ayak izinin en yuksek kosesine oturmali.");
                }
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// Sur perdesinin <b>üst yüzü yukarı</b> bakmalı — ve bu test bir
        /// tekrardan doğdu.
        ///
        /// Kaldırımda 698 üçgenin 697'si ters sarılmıştı (ADR 0031 §2) ve ders
        /// yazılmıştı. Sur perdesini yazarken **aynı hatayı yaptım**: sarımın
        /// doğru olduğunu söyleyen bir yorumun altında, 4 199 yatay üçgenin
        /// 4 198'i aşağı bakıyordu. Yorum kanıt değildir; sayı kanıttır.
        ///
        /// Ters sarımın üç sonucu: yüzey üstten ışıksız okunur, ışın sorguları
        /// arka yüzü görmez (yani çarpıcı fiilen yoktur) ve altta kalan şey
        /// zemin sanılır.
        /// </summary>
        [Test]
        public void WallCurtainTopFacesUp()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                Hezarfen.Editor.Gis.WallBuilder.WorldScene,
                UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                GameObject host = null;
                foreach (var go in scene.GetRootGameObjects())
                    if (go.name == Hezarfen.Editor.Gis.WallBuilder.RootName) host = go;
                Assert.IsNotNull(host,
                    "SUR_Galata yok. Menu: Hezarfen -> GIS -> Galata surlarini kur");

                var perde = host.transform.Find("Perde");
                Assert.IsNotNull(perde, "Perde duvar mesh'i yok.");
                var mesh = perde.GetComponent<MeshFilter>()?.sharedMesh;
                Assert.IsNotNull(mesh, "Perde mesh verisi yok.");

                var v = mesh.vertices;
                var t = mesh.triangles;
                int up = 0, down = 0;
                for (int i = 0; i < t.Length; i += 3)
                {
                    Vector3 nrm = Vector3.Cross(v[t[i + 1]] - v[t[i]],
                                                v[t[i + 2]] - v[t[i]]).normalized;
                    if (nrm.y > 0.5f) up++;
                    else if (nrm.y < -0.5f) down++;
                }
                Assert.Greater(up, 500,
                    $"Duvarin ustunde yukari bakan yuzey yok ({up} ucgen).");
                Assert.Less(down, up * 0.05f,
                    $"Perdenin {down} ucgeni ASAGI bakiyor ({up} yukari). Sarim "
                    + "ters: yuzey ustten isiksiz okunur ve isin sorgulari arka "
                    + "yuzu gormedigi icin cizici fiilen YOKTUR.");

                Assert.IsNotNull(perde.GetComponent<MeshCollider>(),
                    "Perde duvarin cizicisi yok — oyuncu icinden gecer.");
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// Duvar kalınlığı <b>belgeli</b> (~2 m); yükseklik <b>değil</b>.
        ///
        /// Test ikisini ayrı tutuyor: belgeli sayı kilitlenir, taslak sayı
        /// yalnızca "taslak olduğunu söylüyor mu" diye sınanır. Uydurulmuş bir
        /// sayıyı belgeliymiş gibi kilitlemek, bu projenin kaçındığı tek şey.
        /// </summary>
        [Test]
        public void WallDimensionsComeFromTheSurvey()
        {
            Assert.AreEqual(2.0f, Hezarfen.Editor.Gis.WallBuilder.WallThickness, 0.01f,
                "Galata suru ~2 m kalinliktadir (olculu).");

            foreach (string kind in new[] { "burc", "kapi" })
                foreach (var v in OfKind(kind))
                {
                    Assert.AreEqual("measured", v.status,
                        $"{v.name}: olculer roloveden geliyor, status 'measured'.");
                    // Kaynak ADIYLA anilmali: bir gun biri sayilari degistirmek
                    // isterse neyi curutmesi gerektigini bilsin.
                    StringAssert.Contains("Erdogan", v.source ?? "",
                        $"{v.name}: olcunun kaynagi (Erdogan 2013, ITU rolovesi) "
                        + "notta anilmali.");
                    // Surun kendi zaman isareti: 1864'e kadar EKSIKSIZ ayakta,
                    // yani 1632'de tam. Yikim tarihini anmayan bir not, bugunku
                    // "kalintilar" haline bakip modellemeye acik kapi birakir.
                    StringAssert.Contains("1864", v.source ?? "",
                        $"{v.name}: sur 1864'e kadar eksiksiz ayaktaydi; not "
                        + "bunu anmali.");
                    // OLCULU olan D2, olcusu OLMAYAN D3. Dortgen burcun
                    // VARLIGI belgeli ama OLCUSU degil — ikisini ayni
                    // basamakta gostermek, olcunun anlamini silerdi.
                    string want = v.plan == "dortgen" ? "D3" : "D2";
                    Assert.AreEqual(want, v.accuracy,
                        $"{v.name}: plan='{v.plan}' icin dogruluk basamagi "
                        + $"{want} olmali (olculu mu, degil mi).");
                }

            // IKI TIP DE BELGELI. Once "hepsi U planli" diye asiri
            // duzeltmistim; tez "DORTGEN VE U planli burclar" diyor. Bugune
            // kalan iki ornegin U planli olmasi karenin olmadigini
            // gostermez — hayatta kalan ornek orneklem degildir.
            int u = 0, dortgen = 0;
            foreach (var v in OfKind("burc"))
            {
                if (v.plan == "dortgen") { dortgen++; continue; }
                u++;
                Assert.Less(v.depth, v.width,
                    $"{v.name} {v.width}x{v.depth} — U planli burcta derinlik "
                    + "genislikten kucuktur (on yuz yarim daire, Erdogan 2013).");
            }
            Assert.Greater(u, 0, "U planli burc yok.");
            Assert.Greater(dortgen, 0,
                "Dortgen burc yok — tez iki tipi de belgeliyor.");
        }

        /// <summary>
        /// Külahın biçimi <b>D3</b>'tür ve bu yüzden iki varyant üretildi;
        /// ikisinin silueti gerçekten ayırt edilebilir olmalı, yoksa seçim
        /// sunmanın anlamı kalmaz.
        /// </summary>
        // ============================================ Uskudar Mihrimah (1548)

        /// <summary>
        /// Kubbe **ölçülen** sayıyı taşımalı — ve mesh'ten ölçülmüş olmalı.
        ///
        /// Dış çap 11,40 m, kilit 24,20 m (Vardar 2021). Üretici kubbeyi
        /// <b>birleşmeden önce</b> ölçüp kataloğa yazar; Galata turunda
        /// birleşmiş kütleden ölçülen şeyin gövde değil AYAK İZİ olduğu
        /// pahalıya öğrenildi.
        /// </summary>
        [Test]
        public void MihrimahDomeCarriesTheMeasuredNumbers()
        {
            {
                var v = Named("UskudarMihrimah");
                Assert.AreEqual(11.40f, v.dome_d, 0.01f,
                    $"{v.name} kubbe capi {v.dome_d:F2} — olculen 11,40 m.");
                Assert.AreEqual(v.dome_d, v.measured_dome_d, 0.06f,
                    $"{v.name}: bildirilen cap {v.dome_d:F2} ama MESH'ten "
                    + $"olculen {v.measured_dome_d:F2}. Sayiyi yazmak "
                    + "yetmez, geometrinin onu tasimasi gerekir.");
                Assert.AreEqual(24.20f, v.dome_crown_z, 0.01f,
                    $"{v.name} kilit {v.dome_crown_z:F2} — olculen 24,20 m.");
                Assert.AreEqual(v.dome_crown_z + v.podium_h, v.measured_crown_z,
                    0.03f, $"{v.name}: kilit kotu set dahil olculmeli.");
            }
        }

        /// <summary>
        /// <b>ÜÇ</b> yarım kubbe — dört değil, iki değil.
        ///
        /// Yapıyı önemli kılan şey budur: üç yarım kubbeli planın
        /// İstanbul'daki ilk ve tek örneğidir ve giriş yönünde yarım kubbe
        /// <b>yoktur</b>. Dördüncüsü konursa plan sıradanlaşır.
        /// </summary>
        [Test]
        public void MihrimahHasExactlyThreeHalfDomes()
        {
            {
                var v = Named("UskudarMihrimah");
                Assert.AreEqual(3, v.half_domes,
                    $"{v.name} {v.half_domes} yarim kubbe — UC olmali "
                    + "(iki yan + kible; giriste YOK).");
            }
        }

        /// <summary>
        /// Çift minare, her biri **tek** şerefeli; çift revak, **beş** gözlü.
        ///
        /// Hepsi sayılan değerdir: Hadîkatü'l Cevâmi "birer şerefeli
        /// minaresi" der; birinci revak beş kubbeli ve altı mermer
        /// sütunludur; çift revak Sinan'ın özgün tipidir (bu cami beş gözlü
        /// çift revaklı yedi caminin ilki).
        /// </summary>
        [Test]
        public void MihrimahCountedFeaturesMatchTheSources()
        {
            {
                var v = Named("UskudarMihrimah");
                Assert.AreEqual(2, v.minarets, $"{v.name}: cifte minare.");
                Assert.AreEqual(1, v.sherefe_each,
                    $"{v.name}: her minare TEK serefeli — 'birer serefeli "
                    + "minaresi' (Hadikatu'l Cevami).");
                Assert.AreEqual(5, v.portico_bays,
                    $"{v.name}: birinci revak BES gozlu.");
                Assert.IsTrue(v.double_portico,
                    $"{v.name}: cift revak Sinan'in ozgun tipidir, sonradan "
                    + "eklenmis degildir — birakmak yapiyi baska bir cami "
                    + "yapar.");
            }
        }

        /// <summary>
        /// Minareler kubbeyi <b>aşmalı</b>, ama kütleyi ezmemeli.
        ///
        /// Ölçü yok; kitin yazılı kuralı şerefeyi ana kubbe kilidine koyar,
        /// üstüne petek + külah + âlem gelir. Bu test kuralın sonucunu
        /// kilitliyor: minare tepesi kubbeden yüksek olacak, ama iki
        /// katından fazla olmayacak.
        /// </summary>
        [Test]
        public void MihrimahMinaretsRiseAboveTheDomeButDoNotDwarfIt()
        {
            {
                var v = Named("UskudarMihrimah");
                Assert.Greater(v.minaret_top, v.dome_crown_z + 5f,
                    $"{v.name}: minare tepesi {v.minaret_top:F1} m, kubbe "
                    + $"{v.dome_crown_z:F1} m — serefe kubbe kilidinde ve "
                    + "ustune petek/kulah/alem gelir.");
                Assert.Less(v.minaret_top, v.dome_crown_z * 2f,
                    $"{v.name}: minare {v.minaret_top:F1} m — kubbenin iki "
                    + "katindan yuksek minare bu olcekte bir cami degil.");
            }
        }

        /// <summary>
        /// 1632'de <b>olmayanlar</b> notta adıyla geçmeli.
        ///
        /// Üsküdar meydanının bugünkü iki simgesi 1632'de yoktur: Yeni
        /// Valide Camii (1708-11) ve III. Ahmed Meydan Çeşmesi (1728).
        /// Külliyenin türbe/hamam/kasır/muvakkithanesi de sonradandır.
        /// </summary>
        [Test]
        public void Mihrimah1632AbsencesAreNamed()
        {
            string[] absent = { "YENI VALIDE", "III. AHMED", "turbe",
                                "muvakkithane", "gunes saati" };
            var v = Named("UskudarMihrimah");
            {
                foreach (var a in absent)
                    StringAssert.Contains(a, v.source ?? "",
                        $"{v.name} notunda '{a}' gecmiyor — 1632'de YOK olan "
                        + "parcalar adiyla yazilmali (RESEARCH.md 5.4).");
            }
        }

        /// <summary>
        /// <b>Kıble sabiti gerçekten kıble mi?</b>
        ///
        /// Sabiti yazmak kolay, yanlış yazmak da öyle. Test büyük daire
        /// formülünü burada yeniden kurar ve UTM 35N meridyen yakınsamasını
        /// çıkarır — oyun dünyası ızgara kuzeyindedir, gerçek kuzeyde değil
        /// (ADR 0007). 1,3°'lik bu fark sessizce yutulursa her cami eğik
        /// durur.
        /// </summary>
        [Test]
        public void QiblaConstantIsActuallyTheQibla()
        {
            const double kaabaLat = 21.4225, kaabaLon = 39.8262;
            const double lat = 41.0257, lon = 29.0148;   // Uskudar Mihrimah
            const double lambda0 = 27.0;                 // UTM 35N orta meridyen

            double la1 = lat * System.Math.PI / 180.0;
            double la2 = kaabaLat * System.Math.PI / 180.0;
            double dl = (kaabaLon - lon) * System.Math.PI / 180.0;
            double q = 180.0 / System.Math.PI * System.Math.Atan2(
                System.Math.Sin(dl),
                System.Math.Cos(la1) * System.Math.Tan(la2)
                - System.Math.Sin(la1) * System.Math.Cos(dl));
            double gamma = (lon - lambda0) * System.Math.Sin(la1);
            double grid = (q - gamma + 360.0) % 360.0;

            // BUGUNUN kiblesi hala hesaplanabilmeli: sabit karsilastirma
            // dayanagi, o yuzden dogru kalmali.
            Assert.AreEqual(grid, LandmarkPlacer.ModernQiblaDeg, 0.05,
                $"modern kible sabiti {LandmarkPlacer.ModernQiblaDeg:F2} ama "
                + $"hesaplanan {grid:F2} derece.");

            // AMA YERLESTIREN SAYI BU DEGIL. 1632'nin camileri buyuk daireye
            // bakmiyor: on tarihi camide olculen sapma medyani -16,6 derece
            // ve HEP AYNI YONE (dogu). Yontem Sakirin Camii'nde (2009)
            // +0,04 veriyor, yani sapmayi uydurmuyor. ADR 0046.
            float sapma = Mathf.DeltaAngle(LandmarkPlacer.QiblaDeg,
                                           LandmarkPlacer.ModernQiblaDeg);
            Assert.AreEqual(16.7f, sapma, 1.5f,
                $"1632 kiblesi bugunkunden {sapma:F1} derece farkli — olculen "
                + "medyan 16,6 derece (dogu yonunde). Bu sabit tahmin degil "
                + "OLCUM; degistirmek icin yeniden olcum gerekir.");
            Assert.Less(LandmarkPlacer.QiblaDeg, LandmarkPlacer.ModernQiblaDeg,
                "Osmanli camisi buyuk daireden DOGUYA sapar, batiya degil.");

            Assert.AreEqual((LandmarkPlacer.QiblaDeg + 180f) % 360f,
                LandmarkPlacer.QiblaEntranceDeg % 360f, 0.01f,
                "giris cephesi kiblenin tam tersi olmali.");
        }

        // ================================== Dogancilar (inis noktasi, 1632)

        /// <summary>
        /// Doğancılar Camii'nin çatısı <b>ahşaptır</b>, kubbe değil.
        ///
        /// Kaynağın kesin söylediği tek biçim niteliği bu: "duvarları kâgir
        /// çatısı ahşaptır". Kubbe koymak yapıyı bir başka cami yapar —
        /// üstelik daha "gösterişli" göründüğü için sessizce kayması kolay
        /// bir hatadır.
        /// </summary>
        [Test]
        public void DogancilarMosqueHasATimberRoofNotADome()
        {
            foreach (var v in OfKind("cami"))
                Assert.AreEqual("timber", v.roof,
                    $"{v.name} ortusu '{v.roof}' — Dogancilar Camii ve Hudayi "
                    + "tekkesi AHSAP catilidir (kaynak: 'duvarlari kargir "
                    + "catisi ahsaptir'). RESEARCH.md 5.5");
        }

        /// <summary>
        /// İniş noktasının yapıları <b>taslak</b> olarak işaretli kalmalı.
        ///
        /// İkisinin de 1632 hâlinin ölçülü çizimi yok: bugünkü Doğancılar
        /// Camii büyük ölçüde 1857, Hüdâyî Külliyesi 1855-56'dır. Ölçüler
        /// tipolojik varsayılandır, ölçüm değil — ve bunu kataloğun kendisi
        /// söylemek zorunda, yoksa ileride biri bu sayıları veri sanır.
        /// </summary>
        [Test]
        public void LandingPointBuildingsStayMarkedDraft()
        {
            string[] ids = { "DogancilarCamii", "HudayiTekkesi", "HudayiTurbesi" };
            foreach (var v in Load())
                if (System.Array.IndexOf(ids, v.name) >= 0)
                {
                    Assert.AreEqual("draft", v.status,
                        $"{v.name} status='{v.status}' — olculu cizimi yok, "
                        + "taslak kalmali.");
                    Assert.AreEqual("D3", v.accuracy,
                        $"{v.name} accuracy='{v.accuracy}' — tipolojik.");
                }
        }

        /// <summary>
        /// Hüdâyî türbesi <b>1632'de ayakta</b> — ve açık türbedir.
        ///
        /// Bir tur önce bu varlık üretilmiş ama <i>yerleştirilmemişti</i>:
        /// türbe yapısının ne zaman kurulduğunu bulamamıştım. Sonra kaynağa
        /// doğru soru soruldu ve cevap çıktı: Hüdâyî <b>Safer 1038</b>'de
        /// (Ekim 1628) öldü, türbe <b>aynı hicrî yıl içinde, 1038'de
        /// (1628-29)</b> yapıldı. 1632'de yapı üç-dört yaşındadır.
        ///
        /// Biçim ayrı bir sorudur ve ayrı taşınır: TDV 1850 yangını öncesi
        /// yapıyı <b>açık türbe</b> diye tanımlar ve bugünkü kubbe
        /// <b>dört mermer sütun</b> üzerine oturur. Varlık belgeli (T1),
        /// biçim tipolojik (D3) — ikisini karıştırmamak kataloğun bütün
        /// sözleşmesi.
        ///
        /// Ayrıca bir bayrak tuzağı kapatıldı: <c>TurbeParams.acik</c>
        /// <b>vardı ama hiçbir şey yapmıyordu</b>; kapalı bir türbe kurulup
        /// "açık" diye kataloglanıyordu. Bu test bayrağa değil, üretilen
        /// yapının bildirdiğine bakar.
        /// </summary>
        [Test]
        public void HudayiTombStandsIn1632AndIsAnOpenBaldachin()
        {
            Assert.IsTrue(LandmarkPlacer.Built.ContainsKey("LM_HudayiTurbesi"),
                "HudayiTurbesi yerlestirme listesinde yok — 1038 (1628-29) "
                + "tarihi belgeli, 1632'de ayakta.");

            bool found = false;
            foreach (var v in Load())
            {
                if (v.name != "HudayiTurbesi") continue;
                found = true;
                Assert.AreEqual("T1", v.tier,
                    "varligi belgeli: turbe 1038'de (1628-29) yapildi.");
                Assert.AreEqual("D3", v.accuracy,
                    "bicim tipolojik: 1632 halinin olculu cizimi yok.");
                Assert.IsTrue(v.acik,
                    "ACIK turbe olmali — 'acik' bayragi bir donem kitte "
                    + "HIC OKUNMUYORDU ve kapali turbe 'acik' diye "
                    + "kataloglaniyordu.");
                Assert.IsFalse(v.walls,
                    "acik turbenin duvari olmaz.");
                Assert.AreEqual(4, v.columns,
                    "kaynak 'dort mermer sutun' der.");
                StringAssert.Contains("1038", v.source ?? "",
                    "kaynak notu tarihi tasimali.");
            }
            Assert.IsTrue(found, "HudayiTurbesi katalogda yok.");
        }

        /// <summary>
        /// <b>Uçuş geometrisi</b> — Galata Kulesi'nden Doğancılar'a.
        ///
        /// Doğancılar'ın koordinatı bir tur önce <b>771 m yanlıştı</b> (elle
        /// girilmişti) ve Galata'ya 3709 m veriyordu: modern kaynakların
        /// hiçbiriyle (3358 / 3400 / 3558 m) uyuşmuyordu. Kültür Envanteri
        /// kaydındaki koordinatla mesafe <b>3336 m</b> — en düşük modern
        /// değere %0,7 yakın.
        ///
        /// Test bu düzeltmeyi kilitliyor: konum sessizce eski hâline
        /// dönerse finalin bütün geometrisi kayar.
        /// </summary>
        [Test]
        public void FlightGeometryMatchesTheDocumentedDistance()
        {
            var doc = GeoJsonImporter.ParseLocal(File.ReadAllText(
                Path.Combine(TerrainImporter.RepositoryRoot(),
                    LandmarkPlacer.LocalJsonPath.Replace('/',
                        Path.DirectorySeparatorChar))));

            Vector2? kule = null, dog = null;
            foreach (var f in doc.features)
            {
                if (f.rings == null || f.rings.Count == 0
                    || f.rings[0].Count == 0) continue;
                var q = f.rings[0][0];
                if (f.id == "LM_GalataKulesi") kule = new Vector2(q.x, q.z);
                if (f.id == "LM_Dogancilar") dog = new Vector2(q.x, q.z);
            }
            Assert.IsTrue(kule.HasValue && dog.HasValue,
                "Kule veya Dogancilar katalogda yok.");

            float d = Vector2.Distance(kule.Value, dog.Value);
            Assert.AreEqual(3336f, d, 40f,
                $"Galata -> Dogancilar {d:F0} m. Beklenen ~3336 m (Kultur "
                + "Envanteri koordinati). 3709 m cikiyorsa konum elle "
                + "girilmis ESKI degere donmustur.");

            // Modern yorumlarin en dususu 3358 m; olcumumuz onun ALTINDA
            // kalmali ama %5'ten fazla uzaklasmamali.
            Assert.Less(d, 3358f * 1.05f,
                $"{d:F0} m, modern en dusuk degerden (3358 m) fazla sapiyor.");
        }

        // ===================================== Ucus butcesi (finalin fizigi)

        /// <summary>
        /// Süzülme oranı <b>savunulabilir</b> kalmalı.
        ///
        /// Ölçüldü: Galata Kulesi'nden Doğancılar'a 3336 m, düşüş 51,7 m —
        /// yani geometrinin istediği oran <b>64,6 : 1</b>. Karşılaştırma:
        /// yamaç paraşütü ~10:1, modern delta kanat ~15:1, yarış planörü
        /// 50-60:1.
        ///
        /// Finali "çalışır" kılmanın en kolay yolu kanadın oranını 65:1'e
        /// çekmektir ve tam bu yüzden bir bekçi gerekiyor: o değişiklik
        /// hiçbir testi bozmaz, hiçbir render'da görünmez, ve oyunun bütün
        /// iddiasını sessizce çöpe atar.
        ///
        /// Doğru çözüm oranı şişirmek değil <b>yükselen hava</b>dır:
        /// mesafeyi kapatmak için gereken ortalama tırmanma yalnızca
        /// ~0,9 m/s — zayıf bir termiğin bile altında. Ayrıntı: ADR 0037,
        /// Soru 1 ve <c>Hezarfen → Uçuş → Uçuş bütçesini ölç</c>.
        /// </summary>
        [Test]
        public void GlideRatioStaysHistoricallyDefensible()
        {
            var t = AssetDatabase.LoadAssetAtPath<Hezarfen.Flight.WindTuning>(
                Hezarfen.Editor.Flight.FlightBudget.TuningPath);
            Assert.IsNotNull(t, "Ayar varligi yok.");

            float r = Hezarfen.Flight.Aerodynamics.BestGlideRatio(t).ratio;
            Assert.Less(r, Hezarfen.Editor.Flight.FlightBudget.MaxDefensibleGlideRatio,
                $"en iyi suzulme {r:F1} : 1 — 17. yy kanadi icin ust sinir "
                + $"{Hezarfen.Editor.Flight.FlightBudget.MaxDefensibleGlideRatio} : 1. "
                + "Finali kapatmak icin orani sismek yerine YUKSELEN HAVA "
                + "kullan (ADR 0037 Soru 1).");
            Assert.Greater(r, 3f,
                $"en iyi suzulme {r:F1} : 1 — bu bir kanat degil bir tas.");
        }

        /// <summary>
        /// Uçuş bütçesi <b>ölçülebilir</b> olmalı ve bilinen sonucu vermeli.
        ///
        /// Test bir "geçiyor mu" testi değil, bir <b>çapa</b>: bütçenin
        /// bileşenleri (kule tepesi, iniş kotu, mesafe) birlikte tutarlı
        /// kalmalı. Biri sessizce kayarsa — konum, kule yüksekliği ya da
        /// arazi — burada patlar.
        /// </summary>
        [Test]
        public void FlightBudgetIsMeasurableAndMatchesTheRecordedFigures()
        {
            if (!Hezarfen.Editor.Flight.FlightBudget.TryLandmarks(
                    out Vector2 kule, out Vector2 dog, out string err))
                Assert.Fail(err);

            float d = Vector2.Distance(kule, dog);
            Assert.AreEqual(3336f, d, 40f,
                $"mesafe {d:F0} m — ADR 0037'de olculen 3336 m.");

            var t = AssetDatabase.LoadAssetAtPath<Hezarfen.Flight.WindTuning>(
                Hezarfen.Editor.Flight.FlightBudget.TuningPath);
            float ratio = Hezarfen.Flight.Aerodynamics.BestGlideRatio(t).ratio;

            // Kule tepesi 98,2 m, Dogancilar 46,6 m -> dusus ~51,7 m.
            const float drop = 51.7f;
            float range = drop * ratio;
            Assert.Less(range, d,
                $"sakin hava menzili {range:F0} m >= mesafe {d:F0} m. "
                + "Bu SEVINILECEK bir sonuc degil: ya suzulme orani "
                + "sismis ya iniss noktasi kaymistir — ikisi de sessiz "
                + "hatalardir. ADR 0037.");
        }

        // ============================ Mihrimah kulliyesi (medrese + mektep)

        /// <summary>
        /// Medrese <b>on altı</b> hücre taşımalı.
        ///
        /// Elimdeki tek sayısal belge bu: "kubbeli bir dershane ve on altı
        /// öğrenci hücresi" (TDV; İBB Kültürel Miras). Sayım bir ölçüdür ve
        /// geometriyi bağlar — üretici avluyu bu sayı tutana kadar arar,
        /// sayıyı avluya uydurmaz. İlk denemem 14 verdi ve üreticinin kendi
        /// denetimi reddetti.
        ///
        /// Kısıtın gücü de kayda geçsin: parametre uzayı tarandığında 16
        /// hücreyi veren <b>100 kombinasyon</b> çıktı. Yani sayı avluyu
        /// gevşek sınırlıyor; doğruluk basamağı bu yüzden D3 kalıyor.
        /// </summary>
        [Test]
        public void MedreseCarriesTheDocumentedCellCount()
        {
            bool found = false;
            foreach (var v in Load())
            {
                if (v.name != "MihrimahMedrese") continue;
                found = true;
                Assert.AreEqual(16, v.hucre,
                    $"medrese {v.hucre} hucre — belgeli sayi 16 (TDV: "
                    + "'kubbeli bir dershane ve on alti ogrenci hucresi').");
            }
            Assert.IsTrue(found, "MihrimahMedrese katalogda yok.");
        }

        /// <summary>
        /// Sıbyan mektebinde <b>yazlık eyvan</b> olmalı.
        ///
        /// Kaynak yapıyı iki parça olarak tarif eder: "kubbeli bir dershane
        /// ve kubbeli <b>açık eyvan</b>; kışlık ve yazlık bölümleri
        /// vardır". Eyvansız kurmak yapının yarısını silmek olurdu — ve ilk
        /// üretimde tam bu oldu, render'a bakınca fark ettim.
        /// </summary>
        [Test]
        public void MektepHasItsSummerEyvan()
        {
            bool found = false;
            foreach (var v in Load())
            {
                if (v.name != "MihrimahMektebi") continue;
                found = true;
                Assert.IsTrue(v.eyvan,
                    "mektepte yazlik ACIK EYVAN yok — kaynak 'kubbeli bir "
                    + "dershane ve kubbeli acik eyvan' der.");
            }
            Assert.IsTrue(found, "MihrimahMektebi katalogda yok.");
        }

        /// <summary>
        /// Külliyenin <b>belgeli göreli konumları</b> sahnede de tutmalı.
        ///
        /// Kaynaklar iki ilişki verir: medrese <b>caminin doğusunda</b>,
        /// sıbyan mektebi <b>kıble tarafında</b>. Bu test o iki cümleyi
        /// ölçüye çevirir — ve bir hatayı da böyle yakaladı: iki yapının
        /// <i>ölçülü</i> koordinatları eklenince ilişkiler tutmadı, çünkü
        /// <b>caminin kendi koordinatı elle girilmişti ve ~164 m yanlıştı</b>.
        /// Düzeltince mektebin kıble bileşeni 1,00'e oturdu.
        ///
        /// Yani burada sınanan şey bir zevk değil: külliye kendi içinde
        /// tutarlı olmazsa, konumlardan biri sessizce kaymış demektir.
        /// </summary>
        [Test]
        public void KulliyeMembersKeepTheirDocumentedRelativePositions()
        {
            var doc = GeoJsonImporter.ParseLocal(File.ReadAllText(
                Path.Combine(TerrainImporter.RepositoryRoot(),
                    LandmarkPlacer.LocalJsonPath.Replace('/',
                        Path.DirectorySeparatorChar))));

            Vector2? cami = null, med = null, mek = null;
            foreach (var f in doc.features)
            {
                if (f.rings == null || f.rings.Count == 0
                    || f.rings[0].Count == 0) continue;
                var q = f.rings[0][0];
                if (f.id == "LM_UskudarMihrimah") cami = new Vector2(q.x, q.z);
                if (f.id == "LM_MihrimahMedrese") med = new Vector2(q.x, q.z);
                if (f.id == "LM_MihrimahMektebi") mek = new Vector2(q.x, q.z);
            }
            Assert.IsTrue(cami.HasValue && med.HasValue && mek.HasValue,
                "Kulliye uyeleri katalogda eksik.");

            // Bir kulliyenin uyeleri KOMSUDUR.
            float dMed = Vector2.Distance(cami.Value, med.Value);
            float dMek = Vector2.Distance(cami.Value, mek.Value);
            Assert.Less(dMed, 120f,
                $"medrese camiden {dMed:F0} m — kulliye uyeleri komsudur; "
                + "biri kaymis olmali.");
            Assert.Less(dMek, 120f,
                $"mektep camiden {dMek:F0} m — kulliye uyeleri komsudur.");

            // Medrese DOGUDA: dogu bileseni pozitif olmali.
            Assert.Greater((med.Value - cami.Value).x, 0f,
                "medrese caminin DOGUSUNDA olmali (TDV/IBB).");

            // Mektep KIBLE tarafinda: kible yonuyle ayni yari duzlemde.
            float r = LandmarkPlacer.QiblaDeg * Mathf.Deg2Rad;
            var q2 = new Vector2(Mathf.Sin(r), Mathf.Cos(r));
            float dot = Vector2.Dot((mek.Value - cami.Value).normalized, q2);
            Assert.Greater(dot, 0.6f,
                $"mektep kible bileseni {dot:F2} — kaynak 'caminin KIBLE "
                + "tarafinda' der. Dusukse konumlardan biri yanlistir.");
        }

        // ================================== Incili Kosk (IV. Murad buradan izledi)

        /// <summary>
        /// Köşkün <b>sayılan</b> özellikleri kütlede gerçekten var olmalı.
        ///
        /// Ölçüsü yok (1872'de yıkıldı), ama kaynak sayılabilir şeyler
        /// söylüyor: çıkmanın yan cephelerinde <b>Sarayburnu tarafında bir,
        /// Ahırkapı tarafında iki</b> kemer; esas mekânın <b>dört</b>
        /// köşesinde birer baca; denize doğru <b>taşan</b> bir cumba.
        ///
        /// Asimetri özellikle önemli: simetrik yapmak "daha düzgün" görünür
        /// ve tam bu yüzden sessizce kayar.
        /// </summary>
        [Test]
        public void IncliKoskCountedFeaturesMatchTheSources()
        {
            // SUZGEC TURE DEGIL ADA GORE.
            //
            // Bu test once `OfKind("kosk")` diyordu ve **Alay Kosku** da
            // "kosk" turunde olunca Incili Kosk'un sayilari ona uygulandi:
            // "Sarayburnu tarafinda BIR kemer" diye patladi — Alay
            // Kosku'nde oyle bir sey yok ve olmamali.
            //
            // Ucuncu kez ayni hata (Suleymaniye/Mihrimah, ADR 0044): `OfKind`
            // bir **tur** suzgecidir ve bir tur birden cok yapi icerir. Bir
            // yapiya ozgu sayi ADIYLA aranir.
            foreach (var v in new[] { Named("IncliKosk"),
                                      Named("IncliKosk_Ahsap") })
            {
                Assert.AreEqual(1, v.arch_sarayburnu,
                    $"{v.name}: Sarayburnu tarafinda BIR kemer.");
                Assert.AreEqual(2, v.arch_ahirkapi,
                    $"{v.name}: Ahirkapi tarafinda IKI kemer (asimetri "
                    + "belgelidir).");
                Assert.AreEqual(4, v.baca,
                    $"{v.name}: 'esas mekanin dort kosesinde birer baca'.");
                Assert.Greater(v.cumba, 1.2f,
                    $"{v.name}: cumba {v.cumba:F2} m — denize TASMALI.");
            }
        }

        /// <summary>
        /// Örtü <b>tartışmalı</b>, o yüzden iki varyant var ve farklılar.
        ///
        /// TDV kubbe der, bir tasvir piramidal gösterir, Sedat Hakkı Eldem
        /// örtünün ahşap olduğunu savunur. Galata Kulesi'nin külahında
        /// izlenen yol (ADR 0033) burada da geçerli: karar verilmiş gibi
        /// davranmak yerine iki varyant üretilir.
        ///
        /// Test ikisinin gerçekten ayrıştığını sınıyor — Hüdâyî türbesinde
        /// <c>acik</c> bayrağı okunmadığı hâlde "açık" diye kataloglanmıştı;
        /// aynı sessizlik burada da mümkün.
        /// </summary>
        [Test]
        public void IncliKoskRoofDisputeIsCarriedAsTwoRealVariants()
        {
            // Yine ADA gore: "kosk" turunde artik Alay Kosku de var ve
            // sayim ucu buluyordu. Tartisma INCILI KOSK'un ortusune ait.
            var all = new[] { Named("IncliKosk"), Named("IncliKosk_Ahsap") };
            var roofs = new HashSet<string>();
            foreach (var v in all) roofs.Add(v.roof);
            CollectionAssert.AreEquivalent(new[] { "kubbe", "ahsap" }, roofs,
                "varyantlar 'kubbe' ve 'ahsap' olmali.");
            Assert.AreNotEqual(all[0].height, all[1].height,
                "iki varyant ayni yukseklikte — `roof` okunmuyor olabilir.");
        }

        /// <summary>
        /// Köşk <b>suya taşar</b>: alt yapısı su çizgisinin altına iner.
        ///
        /// Kaynak yapıyı Bizans deniz surunun önüne eklenen kemerli bir alt
        /// yapının üstünde tarif eder. Önceki katalog koordinatı denizden
        /// <b>125 m içeride ve 14,7 m yukarıdaydı</b> — 156 m yanlıştı ve
        /// yapıyı bir tepe evine çeviriyordu.
        ///
        /// Su çizgisinde kesilen kütle "yüzüyor" gibi okunur; bu Kız Kulesi
        /// turunda ölçülmüştü (ADR 0035) ve aynı kural burada da geçerli.
        /// </summary>
        [Test]
        public void IncliKoskSitsOnTheWaterline()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabDir}/PF_IncliKosk.prefab");
            Assert.IsNotNull(prefab, "PF_IncliKosk yok.");
            var rs = prefab.GetComponentsInChildren<Renderer>(true);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            Assert.Less(b.min.y, -0.5f,
                $"alt yapi tabani {b.min.y:F2} m — su cizgisinin ALTINA "
                + "inmeli; kosk denize tasar.");
            Assert.Greater(b.max.y, 12f,
                $"tepe {b.max.y:F2} m — kosk alt yapisiyla birlikte okunur.");
        }

        // ============================== Topkapi siluetinin belirleyicileri

        /// <summary>
        /// Adalet Kulesi 1632'de <b>üç</b> taş katlıdır — dört değil.
        ///
        /// Kanûnî 1527-29'da taş bölümü ekletti; kule o hâliyle üç taş kat +
        /// ahşap üst kat + kurşun piramidal külahtır. <b>Dördüncü taş kat
        /// II. Mahmud'undur (1819-20)</b>, bugünkü yüksek sivri külah da
        /// Abdülaziz'in. Yani fotoğraftaki kule 19. yüzyıldır.
        ///
        /// Bu, Galata Kulesi'ndekiyle <b>aynı</b> hata ailesi (ADR 0033):
        /// tanınan siluet sonraki yüzyılların eseridir ve "herkesin bildiği
        /// hâli" modellemek 1632'yi siler.
        /// </summary>
        [Test]
        public void AdaletKulesiHasThreeStoneTiersNotFour()
        {
            foreach (var v in OfKind("saray_kule"))
                Assert.AreEqual(3, v.stone_tiers,
                    $"{v.name} {v.stone_tiers} tas kat — 1632'de UC'tur; "
                    + "dorduncu kat II. Mahmud'un (1819-20) eklemesidir.");
        }

        /// <summary>
        /// Bâbüsselâm <b>iki</b> kulelidir ve kule kapıdan yüksektir.
        ///
        /// Çifte konik külah sarayın en tanınan işaretidir. İkinci iddia
        /// siluet kuralıdır: Adalet Kulesi sarayın en yüksek öğesidir;
        /// kapıdan alçak kalırsa uçuş hattından kaybolur.
        /// </summary>
        [Test]
        public void BabusselamHasTwoTowersAndStaysBelowTheTower()
        {
            float gate = -1f, tower = -1f;
            foreach (var v in Load())
            {
                if (v.kind == "saray_kapi")
                {
                    Assert.AreEqual(2, v.towers,
                        $"{v.name}: CIFTE konik kulahli kapi.");
                    gate = v.height;
                }
                if (v.kind == "saray_kule") tower = v.height;
            }
            Assert.Greater(gate, 0f, "Babusselam katalogda yok.");
            Assert.Greater(tower, gate,
                $"Adalet Kulesi {tower:F1} m, Babusselam {gate:F1} m — kule "
                + "sarayin EN YUKSEK ogesidir.");
        }

        /// <summary>
        /// <b>Belgeli yön</b> türetilen yönü ezmeli.
        ///
        /// Bâbüsselâm birinci avludan ikinciye açılır, yani <b>güneye</b>
        /// bakar; arazi eğimi onu batıya döndürüyordu (278°). Adalet
        /// Kulesi'nin hünkâr penceresi Kubbealtı'na bakar. İkisi de eğimden
        /// türetilemez.
        ///
        /// Çözüm yerleştiriciye yapıya özel istisna yazmak <i>değil</i>,
        /// varlığın kendi yönünü <b>bildirebilmesiydi</b> (<c>face_deg</c>).
        /// Bu test hem alanın var olduğunu hem de öteki kuralları
        /// ezmediğini sınıyor.
        /// </summary>
        [Test]
        public void DeclaredFacingOverridesDerivedFacing()
        {
            var declared = new Dictionary<string, float>();
            foreach (var v in Load())
                if (v.face_deg > 0f) declared[v.prefab] = v.face_deg;

            Assert.IsTrue(declared.ContainsKey("PF_TopkapiBabusselam"),
                "Babusselam yonunu bildirmeli: birinci avludan ikinciye "
                + "acilir, guneye bakar.");
            Assert.IsTrue(declared.ContainsKey("PF_TopkapiAdaletKulesi"),
                "Adalet Kulesi yonunu bildirmeli: hunkar penceresi "
                + "Kubbealti'na bakar.");

            // ILK YAZIMDA burada "kible kuralini kullanan bir yapi face_deg
            // BILDIREMEZ" yaziyordu. Kural, bir YAPI onu curutunceye kadar
            // dogru gorunuyordu: AYASOFYA 1632'de bir camidir ve kibleye
            // DONUK DEGILDIR — bir kilise olarak apsise gore kurulmustur,
            // mihrap apsise EGIK oturtulmustur (olculen sapma 26,9 derece).
            //
            // Yani testin dayandigi varsayim ("cami her zaman kibleye
            // doner") yanlisti. Ama testi silmek de yanlis olurdu: yakalamak
            // istedigi hata gercek — iki yon kaynagi birbirini SESSIZCE
            // ezerse hangisinin kazandigi bilinmez.
            //
            // Kural bu yuzden yasaktan BILDIRIM ZORUNLULUGUNA cevrildi:
            // bir cami kibleden sapabilir, ama sapmayi kendisi soylemek
            // ZORUNDADIR ve soyledigi sayi bildirdigi yonle tutmalidir.
            foreach (var v in Load())
            {
                if (v.kind != "selatin" && v.kind != "cami") continue;
                if (v.face_deg <= 0f) continue;          // kural: kible

                // ESIK YOK, TUTARLILIK VAR.
                //
                // Ilk halinde burada "sapma 10 dereceden buyuk olmali"
                // yaziyordu: bildirimin gercek bir istisna olmasi icin.
                // ADR 0046'dan sonra o esik yanlis oldu — kible sabiti
                // olculen 1632 degerine cekilince Suleymaniye'nin OLCULEN
                // ekseni ondan yalnizca 5 derece farkli kaldi. Bildirim
                // hala dogru (olculen turetileni yener) ama esigi gecemezdi.
                //
                // Yakalanmak istenen hata zaten esik degildi: iki yon
                // kaynaginin birbirini SESSIZCE ezmesiydi. O yuzden tek
                // kosul kaldi — sapma ne olursa olsun KAYITLI ve kendisiyle
                // TUTARLI olacak.
                float offset = Mathf.Abs(Mathf.DeltaAngle(
                    v.face_deg, LandmarkPlacer.QiblaEntranceDeg));
                Assert.AreEqual(offset, v.qibla_offset_deg, 0.5f,
                    $"{v.name} kibleden {offset:F1} derece sapiyor ama "
                    + $"katalogda {v.qibla_offset_deg:F1} yaziyor — istisna "
                    + "KAYITLI ve kendisiyle tutarli olmali, yoksa sessiz "
                    + "kalir.");
            }
        }

        // ==================================== Okmeydani (talim alani)

        /// <summary>
        /// Okmeydanı'nda <b>845,66 m</b>'lik menzil rekoru atılabilmeli.
        ///
        /// Bu bir zevk testi değil, konumun <b>gerekçesi</b>. Önceki
        /// koordinat yeşil poligonun ağırlık merkeziydi ve yamaca
        /// düşüyordu: 400×400 m içinde <b>94,1 m</b> kot yayılımı. Bir ok
        /// meydanı böyle bir yerde olamaz — 845 m'lik atış bir vadinin
        /// karşısına yapılmaz.
        ///
        /// 2×2 km tarandı; en düz nokta 700 m doğuda çıktı (kot 94,5 m,
        /// 300×300 m'de 10,1 m yayılım) ve 30° yönünde 900 m'lik koridorda
        /// yalnızca 5,6 m. Nokta mevcut yeşil poligonun <b>içindedir</b>,
        /// yani poligon doğruydu; merkezi temsil etmiyordu.
        ///
        /// Test terazi arazi ister: meydan boyunca en az 850 m'lik bir
        /// koridorda kot farkı 15 m'yi aşmamalı.
        /// </summary>
        [Test]
        public void OkmeydaniHasGroundFlatEnoughForTheRecordShot()
        {
            const float RecordShotM = 845.66f;

            // ARAZI VARLIGINDAN okunur, sahneden DEGIL.
            //
            // Ilk yazimda `GameObject.Find("TR_Istanbul")` vardi ve test
            // her kosumda ATLANDI: test kosucusunun acik sahnesinde arazi
            // yok. Atlanan bir test, gecen bir test gibi yesil gorunur ve
            // hicbir sey korumaz. TerrainData bir VARLIKTIR; sahneden
            // bagimsiz okunur.
            var meta = TerrainImporter.ReadMeta(TerrainImporter.DefaultDataDir);
            if (meta == null) Assert.Ignore("DEM verisi yok.");
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(
                TerrainImporter.TerrainAssetPath);
            Assert.IsNotNull(data, "TerrainData yok.");

            var origin = new Vector3((float)meta.world_origin_offset_m.x,
                                     0f,
                                     (float)meta.world_origin_offset_m.z);

            var doc = GeoJsonImporter.ParseLocal(File.ReadAllText(
                Path.Combine(TerrainImporter.RepositoryRoot(),
                    LandmarkPlacer.LocalJsonPath.Replace('/',
                        Path.DirectorySeparatorChar))));
            Vector2? ok = null;
            foreach (var f in doc.features)
            {
                if (f.id != "LM_Okmeydani") continue;
                if (f.rings == null || f.rings.Count == 0
                    || f.rings[0].Count == 0) continue;
                var q = f.rings[0][0];
                ok = new Vector2(q.x, q.z);
            }
            Assert.IsTrue(ok.HasValue, "LM_Okmeydani katalogda yok.");

            // Dunya metresi -> heightmap orneklemesi.
            float Height(float x, float z)
            {
                float u = (x - origin.x) / data.size.x;
                float v = (z - origin.z) / data.size.z;
                u = Mathf.Clamp01(u); v = Mathf.Clamp01(v);
                return data.GetInterpolatedHeight(u, v)
                       + (float)meta.min_elevation_m;
            }

            // En duz koridoru ara: 18 yon, +-RecordShot/2 boyunca.
            float bestSpread = float.MaxValue;
            for (int i = 0; i < 18; i++)
            {
                float a = Mathf.PI * i / 18f;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float lo = float.MaxValue, hi = float.MinValue;
                for (float r = -RecordShotM * 0.5f; r <= RecordShotM * 0.5f;
                     r += 25f)
                {
                    float h = Height(ok.Value.x + d.x * r,
                                     ok.Value.y + d.y * r);
                    lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                }
                bestSpread = Mathf.Min(bestSpread, hi - lo);
            }

            Assert.Less(bestSpread, 15f,
                $"Okmeydani'nin en duz {RecordShotM:F0} m koridorunda kot "
                + $"farki {bestSpread:F1} m. Bir ok meydani duz olmali; "
                + "koordinat yamaca kaymis olabilir (ADR 0041).");
        }

        /// <summary>
        /// Okmeydanı'nın tekkesi ve namazgâhı <b>yerleştirilmiş</b> olmalı.
        ///
        /// İkisi de Faz 2'de üretilmişti ama kendi kataloglarında kalmıştı;
        /// yerleştirici yalnızca landmark kataloğunu okuduğu için
        /// yerleştirilebilir değillerdi. Artık bütün <c>art/blend/*</c>
        /// katalogları taranıyor.
        /// </summary>
        [Test]
        public void OkmeydaniBuildingsArePlaceable()
        {
            foreach (var id in new[] { "LM_OkcularTekkesi",
                                       "LM_OkmeydaniNamazgah" })
                Assert.IsTrue(LandmarkPlacer.Built.ContainsKey(id),
                    $"{id} yerlestirme listesinde yok.");

            foreach (var pf in new[] { "PF_Tekke_Okcular",
                                       "PF_Namazgah_Okmeydani" })
                Assert.IsNotNull(
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        $"{PrefabDir}/{pf}.prefab"),
                    $"{pf} prefabi yok.");
        }

        // ============================ Yeni Cami harabesi ("Zulmiye", 1632)

        /// <summary>
        /// 1632'de Yeni Cami bir cami <b>değildir</b>: çatısız bir kabuktur.
        ///
        /// İnşaat 1597'de başladı, <b>1603</b>'te III. Mehmed ölünce durdu,
        /// 1604'te tamamen bırakıldı; yapı <b>57 yıl</b> öyle kaldı ve
        /// ancak 1660-63'te Turhan Sultan tamamlattı. 1632'de kabuk
        /// <b>29 yaşındadır</b> ve halk ona <b>"Zulmiye"</b> der.
        ///
        /// Bu testin işi tek bir şeyi yasaklamak: yapının üstünü örtmek.
        /// Kubbe, yarım kubbe, minare, kurşun — hepsi 1660 sonrasıdır ve
        /// "tanıdık Yeni Cami"yi koymak 1632'yi siler. Bayrağa değil
        /// <b>kütleye</b> bakılır: kabuğun toplam yüksekliği duvar
        /// yüksekliğini aşmamalı.
        /// </summary>
        [Test]
        public void YeniCamiIsARoofless1632Shell()
        {
            foreach (var v in OfKind("harabe"))
            {
                Assert.IsFalse(v.roofed,
                    $"{v.name}: 1632'de bu yapinin ORTUSU YOKTUR.");
                Assert.AreEqual(0, v.minarets,
                    $"{v.name}: minareler 1660 sonrasidir.");
                Assert.AreEqual(4, v.piers,
                    $"{v.name}: ana kubbe DORT fil ayagina oturacakti.");

                // Kutleden sorulan olcu: kabuk duvar + subasman + ayak
                // payini asmamali. Bir kubbe olsaydi asardi.
                float ceiling = v.wall_h + 1.10f + 2f * 0.52f + 0.05f;
                Assert.LessOrEqual(v.height, ceiling,
                    $"{v.name} {v.height:F2} m — en fazla {ceiling:F2} m. "
                    + "Bir sey CATI gibi yukseliyor.");

                StringAssert.Contains("ZULMIYE", (v.source ?? "").ToUpperInvariant(),
                    "halkin verdigi ad notta gecmeli: yapinin 1632'deki "
                    + "anlami budur.");
            }
        }

        /// <summary>
        /// Harabenin <b>ölçülen</b> harim planı korunmalı: 35,50 × 40,90 m.
        ///
        /// Kabuğun yüksekliği bilinmiyor (D3) ama planı biliniyor — ve
        /// 1632'de görülen şey tam olarak o plandır, duvar hâlinde.
        /// </summary>
        [Test]
        public void YeniCamiKeepsItsMeasuredPlan()
        {
            foreach (var v in OfKind("harabe"))
            {
                Assert.AreEqual(35.50f, v.harim_w, 0.01f,
                    $"{v.name} harim genisligi {v.harim_w:F2} — olculen 35,50 m.");
                Assert.AreEqual(40.90f, v.harim_d, 0.01f,
                    $"{v.name} harim derinligi {v.harim_d:F2} — olculen 40,90 m.");
            }
        }

        /// <summary>
        /// Her landmark'ın konumu <b>araziyle tutarlı</b> olmalı.
        ///
        /// Faz 3 boyunca <b>beş</b> konum hatası çıktı ve hiçbiri gözle
        /// görülmedi: Doğancılar 771 m, Okmeydanı 700 m, Üsküdar Mihrimah
        /// 164 m, İncili Köşk 156 m, Yeni Cami 148 m — sonra denetimle üç
        /// tane daha (Yavuz Selim 150 m, Yedikule 160 m, Tersane 247 m).
        /// Hepsi elle girilmişti ve her biri ancak <b>başka bir ölçümle
        /// çeliştiğinde</b> ortaya çıktı.
        ///
        /// Ortak nokta: <b>arazi, koordinatın doğruluğu hakkında konuşur.</b>
        /// Tepeyi taçlandıran bir cami yerel zirveye yakın durmalı; bir
        /// tersane suyun kenarında olmalı; bir ok meydanı düz olmalı.
        /// Bunlar zevk değil ölçülebilir iddialardır ve
        /// <see cref="LandmarkAudit"/> onları tek tek sorar.
        ///
        /// Bu test o denetimi kalıcı kılar: yeni bir koordinat sessizce
        /// yamaca ya da denize düşerse burada patlar.
        /// </summary>
        [Test]
        public void EveryLandmarkPositionAgreesWithTheTerrain()
        {
            var meta = TerrainImporter.ReadMeta(TerrainImporter.DefaultDataDir);
            if (meta == null) Assert.Ignore("DEM verisi yok.");

            var height = LandmarkAudit.HeightFromAsset();
            if (height == null) Assert.Ignore("Arazi varligi yok.");

            var rows = LandmarkAudit.Measure(height);
            Assert.Greater(rows.Count, 0, "Landmark okunamadi.");

            var sb = new System.Text.StringBuilder();
            int bad = 0;
            foreach (var r in rows)
                foreach (var c in r.complaints)
                { sb.AppendLine($"{r.id}: {c}"); bad++; }

            Assert.AreEqual(0, bad,
                $"{bad} konum araziyle celisiyor: "
                + System.Environment.NewLine + sb);
        }

        // ======================================== Suleymaniye Camii (1557)

        /// <summary>
        /// <b>Dört minare, on şerefe</b> — 3+3+2+2.
        ///
        /// Sayı belgelidir ve yapının en tanınan sayısal özelliğidir;
        /// yaygın yorum Süleyman'ın <b>onuncu</b> padişah ve İstanbul'da
        /// hüküm süren <b>dördüncü</b> padişah olmasına bağlar. Yorumu
        /// değil sayıyı kilitliyoruz.
        /// </summary>
        [Test]
        public void SuleymaniyeHasFourMinaretsAndTenBalconies()
        {
            bool found = false;
            foreach (var v in Load())
            {
                if (v.name != "Suleymaniye") continue;
                found = true;
                Assert.AreEqual(4, v.minarets, "DORT minare.");
                Assert.AreEqual(10, v.sherefe_total,
                    $"{v.sherefe_total} serefe — ON olmali (3+3+2+2).");
                Assert.AreEqual(2, v.half_domes,
                    $"{v.half_domes} yarim kubbe — Suleymaniye IKI "
                    + "yarim kubbelidir (ana eksende, Ayasofya semasi). "
                    + "Uskudar Mihrimah'ta UC idi; sayi plani tanimlar.");
                Assert.AreEqual("D2", v.accuracy,
                    "kubbe olculu: 26,50 m cap / 53 m kilit.");
            }
            Assert.IsTrue(found, "Suleymaniye katalogda yok.");
        }

        /// <summary>
        /// Kubbe <b>mesh'ten</b> ölçülmeli, bildirilenle uyuşmalı.
        ///
        /// Galata turunda öğrenildi: birleşmiş kütleden ölçülen şey gövde
        /// değil ayak izidir. Üretici kubbeyi birleşmeden önce ölçüp
        /// kataloğa yazar.
        /// </summary>
        [Test]
        public void SuleymaniyeDomeCarriesTheMeasuredNumber()
        {
            foreach (var v in Load())
            {
                if (v.name != "Suleymaniye") continue;
                Assert.AreEqual(26.50f, v.dome_d, 0.01f,
                    $"kubbe capi {v.dome_d:F2} — olculen 26,50 m.");
                Assert.AreEqual(v.dome_d, v.measured_dome_d, 0.08f,
                    $"bildirilen {v.dome_d:F2} ama MESH'ten olculen "
                    + $"{v.measured_dome_d:F2}.");
                Assert.AreEqual(53.00f, v.dome_crown_z, 0.01f,
                    $"kilit {v.dome_crown_z:F2} — olculen 53 m.");
            }
        }

        /// <summary>
        /// Süleymaniye sahnenin <b>en yüksek</b> yapısı olmalı.
        ///
        /// Üçüncü tepeyi taçlandırır ve siluete hâkimdir; Galata Kulesi'nin
        /// tepesi 98,2 m iken Süleymaniye'nin minare uçları 124,8 m'ye
        /// çıkar. Bu bir zevk değil: uçuş hattından bakınca şehri
        /// tanıtacak ilk şey odur.
        /// </summary>
        [Test]
        public void SuleymaniyeDominatesTheSkyline()
        {
            // SAHNEDEN DEGIL, KATALOG + ARAZI VARLIGINDAN.
            //
            // Ilk yazimda sahnedeki LANDMARK_1632'yi ariyordu ve atlandi —
            // bugun ucuncu kez ayni tuzak. Ustelik sahneye bakmak YANLIS
            // olcuydu: dogru soru "sahnede ne var" degil, "dunyada hangi
            // yapinin tepesi en yuksek" — o da konum + arazi + yapi
            // yuksekligiyle hesaplanir.
            var height = LandmarkAudit.HeightFromAsset();
            if (height == null) Assert.Ignore("Arazi varligi yok.");

            var doc = GeoJsonImporter.ParseLocal(File.ReadAllText(
                Path.Combine(TerrainImporter.RepositoryRoot(),
                    LandmarkPlacer.LocalJsonPath.Replace('/',
                        Path.DirectorySeparatorChar))));

            var byName = new Dictionary<string, Variant>();
            foreach (var v in Load()) byName[v.prefab] = v;

            float best = -1e9f; string who = "";
            foreach (var f in doc.features)
            {
                if (!LandmarkPlacer.Built.TryGetValue(f.id, out string pf))
                    continue;
                if (!byName.TryGetValue(pf, out var v)) continue;
                if (f.rings == null || f.rings.Count == 0
                    || f.rings[0].Count == 0) continue;
                var q = f.rings[0][0];
                float top = height(q.x, q.z) + v.height;
                if (top > best) { best = top; who = v.name; }
            }

            Assert.AreEqual("Suleymaniye", who,
                $"dunyanin en yuksek yapisi {who} ({best:F1} m) — "
                + "Suleymaniye olmali: ucuncu tepeyi taclandirir ve "
                + "siluete hakimdir.");
        }

        /// <summary>
        /// Her landmark prefabının malzemesi **proje varlığı** olmalı — FBX'in
        /// içine gömülü olmamalı.
        ///
        /// Bu tuzak Kız Kulesi turunda kuruldu ve mevcut testlerin hiçbiri
        /// yakalamadı: palete <c>timber_bare</c> rolü eklendi ama Unity
        /// malzemesi henüz üretilmemişti, FBX içe alınınca yuva <b>boş
        /// kalmadı</b> — gövdeye FBX'e gömülü, dokusuz, albedosu 0,906 beyaz
        /// bir malzeme bağlandı. "Boş yuva yok" denetimi bunu geçirir; ayırt
        /// eden şey malzemenin bir VARLIK YOLU olmasıdır.
        ///
        /// Sıra da bu yüzden önemli: yeni bir palet rolü eklendiğinde önce
        /// <c>build_unity_maps.py</c> ve <b>Osmanlı malzemelerini üret</b>,
        /// sonra FBX boru hattı.
        /// </summary>
        [Test]
        public void LandmarkPrefabsUseAuthoredMaterialsNotEmbeddedOnes()
        {
            foreach (var kv in LandmarkPlacer.Built)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{PrefabDir}/{kv.Value}.prefab");
                Assert.IsNotNull(prefab, $"{kv.Value} yok — once boru hatti.");

                foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                    {
                        Assert.IsNotNull(m,
                            $"{kv.Value}/{r.name}: BOS malzeme yuvasi.");
                        string path = AssetDatabase.GetAssetPath(m);
                        StringAssert.EndsWith(".mat", path,
                            $"{kv.Value}/{m.name} '{path}' icinden geliyor — "
                            + "FBX'e GOMULU malzeme. Yeni bir palet rolu "
                            + "eklendiyse once build_unity_maps.py + "
                            + "'Osmanli malzemelerini uret', SONRA boru hatti.");
                    }
            }
        }

        // ================================================== Kiz Kulesi (1632)

        /// <summary>Bugünkü (1725) kâgir kule ~23 m.</summary>
        private const float KizTodayH = 23.0f;

        /// <summary>
        /// <b>1632'de Kız Kulesi AHŞAPTIR.</b>
        ///
        /// Herkesin bildiği kâgir kule, camlı köşk ve kurşun kubbe
        /// <b>1725</b>'tir: kule 1720'de yanmış, Damat İbrahim Paşa yerine
        /// kâgir bir fener kulesi yaptırmıştır. 1509 depreminden sonra
        /// yapılan da <i>"yine ahşap"</i>tır (Göksoy Özkan 2012).
        ///
        /// Bu test bir yazım denetimi değil: modelin malzemesi sessizce
        /// "kagir"e dönerse modellenen şey 1632 kulesi olmaktan çıkar.
        /// </summary>
        [Test]
        public void KizKulesiIsTimberIn1632()
        {
            foreach (var v in OfKind("kizkulesi"))
            {
                Assert.AreEqual("ahsap", v.material,
                    $"{v.name} malzemesi '{v.material}' — 1632 kulesi AHSAPTIR; "
                    + "kagir kule 1725'tir (RESEARCH.md 5.3).");
                Assert.GreaterOrEqual(v.storeys, 2,
                    $"{v.name} {v.storeys} kat — nobet kulesi cok katlidir.");
            }
        }

        /// <summary>
        /// Ahşap kule, yerine geçen kâgir kuleden <b>alçak</b> kalmalı.
        ///
        /// Sınır tipolojik değil mantıksal: 1725 kulesi ~23 m'dir ve onu
        /// aşan bir ahşap kütle, 1632'yi değil 1725'i modelliyor demektir.
        /// </summary>
        [Test]
        public void KizKulesiStaysBelowThe1725Tower()
        {
            foreach (var v in OfKind("kizkulesi"))
                Assert.Less(v.above_water, KizTodayH,
                    $"{v.name} su ustunde {v.above_water:F1} m — 1725 kagir "
                    + $"kulesi ~{KizTodayH} m ve AHSAP kule ondan yuksek "
                    + "olamaz.");
        }

        /// <summary>
        /// 1632'de <b>olmayanlar</b> notta adıyla anılmalı.
        ///
        /// Kubbe ve fener bu yapının en tanınan iki parçasıdır ve ikisi de
        /// 1632'de yoktur. Notta adları geçmezse, ileride biri onları
        /// "eksik" sanıp ekler.
        /// </summary>
        [Test]
        public void KizKulesi1632AbsencesAreNamed()
        {
            string[] absent = { "KUBBE", "FENERI", "kosk", "ZINCIR" };
            foreach (var v in OfKind("kizkulesi"))
                foreach (var a in absent)
                    StringAssert.Contains(a, v.source ?? "",
                        $"{v.name} notunda '{a}' gecmiyor — 1632'de YOK olan "
                        + "parcalar adiyla yazilmali (RESEARCH.md 5.3).");
        }

        /// <summary>
        /// Kule <b>su düzlemine</b> oturur, araziye değil.
        ///
        /// Ölçüldü: adacığın 150 m çevresi baştan başa −12 m — Copernicus
        /// GLO-30 kayalığı hiç görmüyor. Arazi kotuna oturtmak kuleyi deniz
        /// tabanına gömerdi; bu yüzden kayalık varlığın kendi parçasıdır ve
        /// pivot su çizgisindedir. Kayalık su altına da uzanmalı, yoksa
        /// yapı su yüzeyinde kesilmiş gibi durur.
        /// </summary>
        [Test]
        public void KizKulesiSitsOnTheWaterPlaneNotTheSeabed()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabDir}/PF_KizKulesi.prefab");
            Assert.IsNotNull(prefab, "PF_KizKulesi yok — once boru hatti.");

            var rs = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.Greater(rs.Length, 0, "PF_KizKulesi'nde renderer yok.");
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);

            Assert.Less(b.min.y, -0.5f,
                $"kayalik tabani {b.min.y:F2} m — su cizgisinin ALTINA "
                + "uzanmali, yoksa kule suda kesilmis gibi durur.");
            Assert.Greater(b.max.y, 15f,
                $"tepe {b.max.y:F2} m — kule su ustunde okunmali.");
        }

        [Test]
        public void BothCrownVariantsExistAndReallyDiffer()
        {
            var all = OfKind("kule");
            Variant sacakli = null, mazgalli = null;
            foreach (var v in all)
            {
                if (v.crown == "sacakli") sacakli = v;
                if (v.crown == "mazgalli") mazgalli = v;
            }
            Assert.IsNotNull(sacakli, "'sacakli' varyant yok.");
            Assert.IsNotNull(mazgalli, "'mazgalli' varyant yok.");

            // Kaynak birini "cok daha basik ve genis", otekini "dar ve
            // yuksekce" diye ayirir. Kulah boyu bunu tasimali.
            Assert.Greater(mazgalli.cone_h, sacakli.cone_h * 1.4f,
                $"kulah boylari {sacakli.cone_h:F1} / {mazgalli.cone_h:F1} — "
                + "'basik ve genis' ile 'dar ve yuksekce' ayirt edilemiyor.");
        }

        // =============================================== Ayasofya (ADR 0045)

        /// <summary>
        /// <b>Ayasofya'nın kubbesi Osmanlı kubbesi değildir.</b>
        ///
        /// Kitin Osmanlı kubbe zinciri (ADR 0036) basıklık oranını
        /// <c>0,78</c> varsayar. Ayasofya'nın kubbesi 558'de çöktü ve
        /// 562'de İsidoros'un yeğeni tarafından <b>yükseltilerek</b>
        /// yeniden kuruldu; ölçülen oran <c>15,00 / 16,50 = 0,909</c>.
        ///
        /// Osmanlı oranını uygulamak kilidi 55,60 m'den 42,4 m'ye
        /// düşürürdü — yapının en tanınan özelliğini silerdi. Bu test,
        /// birinin "kubbe zinciri zaten var" deyip `sinan_kit`e bağlamasını
        /// engeller.
        /// </summary>
        [Test]
        public void AyasofyaDomeIsByzantineNotOttoman()
        {
            var v = Named("Ayasofya");
            Assert.AreEqual(0.909f, v.rise_ratio, 0.005f,
                $"basiklik orani {v.rise_ratio:F3} — Ayasofya'nin OLCULEN "
                + "orani 0,909 (15,00 / 16,50).");
            Assert.Greater(Mathf.Abs(v.rise_ratio - 0.78f), 0.05f,
                "basiklik orani OSMANLI oranina (0,78) yaklasmis — "
                + "Ayasofya'nin kubbesi 562'de YUKSELTILEREK kuruldu.");
            Assert.AreEqual(55.60f, v.measured_crown_z, 0.05f,
                $"kilit {v.measured_crown_z:F2} m — OLCULEN deger 55,60 m.");
        }

        /// <summary>
        /// Kubbe eteğindeki <b>kırk</b> kaburga ve <b>kırk</b> pencere.
        ///
        /// Sayı yalnızca katalogda değil, mesh'te de yaşar: kubbe
        /// <c>segments = 40</c> ile üretilir ve pencereler kaburgaların
        /// <i>arasına</i> düşer. Blender öz-testi köşeleri sayarak bunu
        /// doğrular (<c>t_dome_facets_carry_the_counted_rib_number</c>);
        /// burada sayının kataloğa doğru geçtiği sınanır.
        /// </summary>
        [Test]
        public void AyasofyaHasFortyRibsAndFortyWindows()
        {
            var v = Named("Ayasofya");
            Assert.AreEqual(40, v.dome_ribs, "kaburga SAYILAN degerdir: 40.");
            Assert.AreEqual(40, v.dome_windows, "pencere SAYILAN degerdir: 40.");
        }

        /// <summary>
        /// <b>Dört minare birbirinin aynı değildir</b> — ve bu ölçülmüş
        /// bir iddiadır.
        ///
        /// Doğu çifti Ø3,6 m, batı çifti Ø4,0 m (Sinan'ın ikizleri).
        /// Kaynaklar tuğla minarenin hangi köşede olduğunda çelişir; ölçü
        /// TDV'nin "güneybatı" iddiasını <b>eler</b>, çünkü o köşe ikiz
        /// çiftin üyesidir ve tuğla minare tektir.
        ///
        /// Tek bir <c>minarets = 4</c> bunu taşıyamaz: dördünü aynı yapmak
        /// kataloğu bozmaz ama yapıyı bozar.
        /// </summary>
        [Test]
        public void AyasofyaFourMinaretsAreNotIdentical()
        {
            var v = Named("Ayasofya");
            Assert.AreEqual(4, v.minarets, "Ayasofya'da DORT minare var.");
            Assert.AreEqual(1, v.brick_minarets,
                $"{v.brick_minarets} tugla minare — TAM BIR tanesi tugladir "
                + "ve farki RENGINDEDIR.");
            Assert.Greater(v.minaret_r_thick, v.minaret_r_thin * 1.05f,
                $"govde yaricaplari {v.minaret_r_thin:F2} / "
                + $"{v.minaret_r_thick:F2} — dogu cifti INCE (O3,6), bati "
                + "cifti KALIN (O4,0) olmali; dordunu esitlemek olculu "
                + "bir farki siler.");
        }

        /// <summary>
        /// Prefab'ın <b>tuğla</b> malzemesi gerçekten var mı.
        ///
        /// Katalogdaki <c>brick_minarets = 1</c> bir sayıdır; bu test onun
        /// Unity'de bir <b>malzemeye</b> döndüğünü sınar. Biri dört
        /// minareyi de kesme taşa çevirse katalog susardı, prefab
        /// konuşmaz — yalnız bu test konuşur.
        /// </summary>
        [Test]
        public void AyasofyaPrefabActuallyCarriesTheBrickMaterial()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabDir}/PF_Ayasofya.prefab");
            Assert.IsNotNull(prefab, "PF_Ayasofya yok — once boru hatti.");

            bool brick = false;
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.name.Contains("Brick")) brick = true;

            Assert.IsTrue(brick,
                "PF_Ayasofya'da tugla malzeme yok — dort minareden biri "
                + "TUGLADIR ve o fark yalnizca malzemede yasar.");
        }

        /// <summary>
        /// <b>Ayasofya kıbleye dönük değildir.</b>
        ///
        /// Bir kilisedir: ekseni apsise bakar (ölçülen azimut 123,5°).
        /// Izgara kıblesi 150,40°, yani arada <b>26,9°</b> var; mihrap
        /// apsise eğik oturtulmuştur. Yerleştirici camileri kıbleye
        /// döndürdüğü için katalog <c>face_deg</c> bildirir ve bildirilen
        /// yön kıbleyi yener.
        ///
        /// Test iki şeyi birden tutar: sapmanın gerçekten büyük olduğunu
        /// (yoksa <c>face_deg</c> gereksiz bir süs olurdu) ve yönün
        /// bildirilmiş olduğunu.
        /// </summary>
        [Test]
        public void AyasofyaIsNotOrientedToTheQibla()
        {
            var v = Named("Ayasofya");
            Assert.Greater(v.face_deg, 0.1f,
                "Ayasofya face_deg bildirmeli — yoksa yerlestirici onu "
                + "KIBLEYE dondurur ve yapi 26,9 derece yanlis durur.");

            float delta = Mathf.Abs(Mathf.DeltaAngle(
                v.face_deg, LandmarkPlacer.QiblaEntranceDeg));
            Assert.Greater(delta, 5f,
                $"bildirilen yon kibleden yalnizca {delta:F1} derece "
                + "farkli — o zaman face_deg'in bir isi yok.");
            Assert.AreEqual(10.2f, v.qibla_offset_deg, 0.3f,
                $"kayit edilen sapma {v.qibla_offset_deg:F1} derece. "
                + "OLCU 1632'nin kiblesine goredir (133,7, ADR 0046), "
                + "bugunkune degil: Bizans'in dogu ekseni 1632'ye BUGUNDEN "
                + "daha yakin (26,9 degil 10,2 derece).");
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(
                v.face_deg, LandmarkPlacer.ModernQiblaDeg + 180f)), 20f,
                "bugunun kiblesine gore sapma 26,9 derece olmali.");
        }

        /// <summary>
        /// 1632'de Ayasofya'nın kubbesi <b>hâlâ şehrin en büyüğü</b>.
        ///
        /// Süleymaniye 26,5 m ile ondan küçüktür ve bu bir ayrıntı değil:
        /// Sinan'ın ömür boyu ölçüştüğü şey bu açıklıktı. Kataloğa üç ayrı
        /// kubbe girdikten sonra sıralamanın korunduğunu sınamak, birinin
        /// "yuvarlak sayı" diye 33'ü 30'a çekmesini engeller.
        /// </summary>
        [Test]
        public void AyasofyaStillHasTheLargestDomeInTheCity()
        {
            var aya = Named("Ayasofya");
            foreach (var v in Load())
            {
                if (v.name == "Ayasofya" || v.measured_dome_d <= 0.01f) continue;
                Assert.Less(v.measured_dome_d, aya.measured_dome_d,
                    $"{v.name} kubbesi {v.measured_dome_d:F2} m — Ayasofya'nin "
                    + $"{aya.measured_dome_d:F2} m'lik kubbesi 1632'de hala "
                    + "sehrin EN BUYUGUDUR.");
            }
        }

        /// <summary>
        /// 1632'de <b>yok</b> olanlar kataloğa da yok diye geçmeli.
        ///
        /// Bugün Ayasofya'nın avlusunda görülen şadırvan I. Mahmud'undur
        /// (1740). Vaftizhâne ayaktadır ama <b>yağhânedir</b>: I. Mustafa
        /// oraya 1639'da, Sultan İbrahim 1648'de gömülür — ikisi de
        /// 1632'den sonra. Yapıyı koyup işlevini yanlış yazmak, yapıyı hiç
        /// koymamaktan daha sinsi bir hata olurdu.
        /// </summary>
        [Test]
        public void AyasofyaHasNoSadirvanAndNoMustafaTurbeIn1632()
        {
            var v = Named("Ayasofya");
            Assert.IsFalse(v.sadirvan,
                "sadirvan 1632'de YOK — bugunku I. Mahmud'undur (1740).");
            Assert.IsFalse(v.turbe_of_mustafa,
                "I. Mustafa/Ibrahim turbesi 1632'de YOK — vaftizhane o "
                + "tarihte YAGHANEDIR (1639 ve 1648).");
        }

        /// <summary>
        /// Kütle basamakları <b>ölçülen</b> zincirle tutarlı.
        ///
        /// Kubbe kilidi (55,60) ve dış çap (33,0) ölçülüdür; ondan türeyen
        /// kubbe kaidesi 40,60 ve kemer uzengisi 24,10 m'dir. Plandan
        /// bağımsız okunan değerler 41,0 ve 25,0 — ikisi de bir metrenin
        /// altında. Bu test o anlaşmayı kilitler: biri kilidi ya da çapı
        /// oynatırsa zincir sessizce kaymaz, test konuşur.
        /// </summary>
        [Test]
        public void AyasofyaDomeChainAgreesWithTheMeasuredPlan()
        {
            var v = Named("Ayasofya");
            float domeBase = v.dome_crown_z - v.dome_rise;
            float archSpring = domeBase - v.measured_dome_d * 0.5f;

            Assert.AreEqual(41.0f, domeBase, 1.0f,
                $"turetilen kubbe kaidesi {domeBase:F2} m — plandan okunan "
                + "41,0 m ile bir metre icinde bulusmali.");
            Assert.AreEqual(25.0f, archSpring, 1.0f,
                $"turetilen kemer uzengisi {archSpring:F2} m — plandan "
                + "okunan yan nef catisi 25,0 m ile bulusmali.");
        }

        /// <summary>
        /// <b>OpenStreetMap atıf yükümlülüğü kaydedilmiş olmalı.</b>
        ///
        /// Ayasofya'nın eksen azimutu, kütle basamakları ve minare gövde
        /// çapları OSM izlerinden <b>türetildi</b> (ADR 0045): kaynaklar
        /// tuğla minarenin köşesinde çeliştiği için kaynak seçmek yerine
        /// plana bakıldı. Veri depoya girmedi — ama ODbL üretilen eser için
        /// <b>atıf</b> ister.
        ///
        /// Copernicus'ta öğrenilen ders: yükümlülük tek yerde yazılıysa
        /// sessizce kaybolur. Bu test kaydın yerinde durduğunu sınar; bir
        /// gün Krediler ekranı yazıldığında metin oradan alınacak.
        /// </summary>
        [Test]
        public void OpenStreetMapAttributionIsRecorded()
        {
            var v = Named("Ayasofya");
            StringAssert.Contains("OpenStreetMap", v.source,
                "Ayasofya'nin kaynagi OSM'den turetilen olculeri anmali.");

            string root = TerrainImporter.RepositoryRoot();
            Assert.IsNotNull(root, "Depo koku bulunamadi.");
            string lic = Path.Combine(root, "refs", "LICENSES.md");
            Assert.IsTrue(File.Exists(lic), "refs/LICENSES.md yok.");
            string text = File.ReadAllText(lic);

            StringAssert.Contains("Open Database License", text,
                "OSM ODbL atif metni LICENSES.md'de olmali — yukumluluk tek "
                + "yerde yazilirsa sessizce kaybolur (Copernicus dersi).");
            StringAssert.Contains("Contains information from OpenStreetMap",
                text, "Krediler ekranina girecek ATIF METNI aynen kayitli "
                + "olmali.");
        }

        // ============================================ Sultanahmet (ADR 0047)

        /// <summary>
        /// <b>Altı minare, on altı şerefe</b> — ve ikisi kısa.
        ///
        /// O güne kadar denenmemiş bir düzendir: harim köşesindeki dördü
        /// üçer, avlu köşesindeki ikisi ikişer şerefeli. Altısını da eşit
        /// yapmak sayıyı korur ama <b>düzeni</b> siler; bu yüzden boyların
        /// da ayrıştığı sınanıyor.
        /// </summary>
        [Test]
        public void SultanahmetHasSixMinaretsAndSixteenSerefe()
        {
            var v = Named("Sultanahmet");
            Assert.AreEqual(6, v.minarets, "ALTI minare.");
            Assert.AreEqual(16, v.sherefe_total,
                $"{v.sherefe_total} serefe — ON ALTI olmali (4x3 + 2x2).");
            Assert.Greater(v.minaret_h_tall, v.minaret_h_short * 1.1f,
                $"minare boylari {v.minaret_h_short:F0} / "
                + $"{v.minaret_h_tall:F0} m — harim minareleri avlu "
                + "minarelerinden belirgin UZUN olmali.");
        }

        /// <summary>
        /// <b>Dört</b> yarım kubbe ve <b>on iki</b> eksedra.
        ///
        /// Eksedralar burada mesh'e <b>girer</b>. Ayasofya'da girmemişlerdi
        /// (ADR 0045) çünkü orada iç mekân öğesidirler; Sultanahmet'te yarım
        /// kubbelerin eteğinden dışa taşar ve siluetin basamaklı kaskadını
        /// onlar yapar. Aynı sözcük, iki yapıda iki ayrı şey — kataloğun
        /// ikisini ayrı alanda tutmasının sebebi bu.
        /// </summary>
        [Test]
        public void SultanahmetHasFourHalfDomesAndTwelveExedrae()
        {
            var v = Named("Sultanahmet");
            Assert.AreEqual(4, v.half_domes,
                "DORT yarim kubbe — dort yonde birer.");
            Assert.AreEqual(12, v.exedrae,
                $"{v.exedrae} eksedra — her yarim kubbede UC, toplam ON IKI.");
            Assert.AreEqual(0, v.exedrae_interior,
                "Sultanahmet'in eksedralari IC MEKAN ogesi degildir; "
                + "disaridan gorunurler ve mesh'te vardirlar.");
        }

        /// <summary>
        /// <b>Yarım kubbe sayısı planı tanımlar</b> — dördü birbirine
        /// benzemez.
        ///
        /// Üsküdar Mihrimah <b>üç</b> (İstanbul'daki ilk ve tek örnek),
        /// Süleymaniye ve Ayasofya <b>iki</b> (ana eksende), Sultanahmet
        /// <b>dört</b> (dört yönde birer). Dördü de "selâtin" türündedir ve
        /// bir kişi hepsini aynı sayıya çekse katalog tutarlı görünürdü —
        /// ama dört ayrı plan tek plana inerdi.
        /// </summary>
        [Test]
        public void HalfDomeCountDistinguishesThePlans()
        {
            Assert.AreEqual(3, Named("UskudarMihrimah").half_domes,
                "Uskudar Mihrimah UC yarim kubbelidir — Istanbul'daki ilk "
                + "ve tek ornek.");
            Assert.AreEqual(2, Named("Suleymaniye").half_domes,
                "Suleymaniye IKI (ana eksende).");
            Assert.AreEqual(2, Named("Ayasofya").half_domes,
                "Ayasofya IKI (ana eksende) — Suleymaniye'nin semasi "
                + "buradan gelir.");
            Assert.AreEqual(4, Named("Sultanahmet").half_domes,
                "Sultanahmet DORT (dort yonde birer).");
        }

        /// <summary>
        /// Bir Osmanlı kubbesinin <b>üç</b> sayısı vardır ve üçü de doğrudur.
        ///
        /// Sultanahmet'te: TDV "içten" <b>22,40 m</b>, açıklık
        /// <b>23,50 m</b>, plandan okunan kurşun izi (kasnak + saçak)
        /// <b>27,7 m</b>. Mesh <b>açıklığı</b> taşır.
        ///
        /// Ayasofya'da yalnızca iki sayı vardı çünkü Bizans kubbesinde
        /// kasnak yok. Üçüncü sayıyı kasnak doğuruyor — yani bu bir
        /// muhasebe sıkıntısı değil, <b>mimari bir fark</b>.
        /// </summary>
        [Test]
        public void SultanahmetDomeHasThreeNumbersAndTheMeshCarriesTheSpan()
        {
            var v = Named("Sultanahmet");
            Assert.AreEqual(23.50f, v.measured_dome_d, 0.08f,
                $"mesh capi {v.measured_dome_d:F2} m — ACIKLIK 23,50 m.");
            Assert.Less(v.dome_d_in, v.dome_d,
                $"ic olcu {v.dome_d_in:F2} aciklıktan {v.dome_d:F2} kucuk "
                + "olmali; ikisi ayni yazilmissa biri kopyalanmis demektir.");
            Assert.AreEqual(1.1f, v.dome_d - v.dome_d_in, 0.15f,
                "aciklik ile ic olcu arasindaki fark kubbe kabugudur "
                + "(~1,1 m) — Ayasofya'da da 1,1 m cikmisti.");
            // Yarim kubbe ana kubbeden KUCUK ama yakin: dort yaprakli sema
            // ayni kemerleri paylasir.
            Assert.Less(v.half_dome_d, v.dome_d);
            Assert.Greater(v.half_dome_d, v.dome_d * 0.85f,
                $"yarim kubbe {v.half_dome_d:F1} m — ana kubbenin "
                + $"{v.dome_d:F1} m'sine YAKIN olmali (dort yaprakli sema "
                + "ayni buyuk kemerleri paylasir).");
        }

        /// <summary>
        /// Osmanlı kubbe zinciri (ADR 0036) <b>üçüncü kez</b> bağımsız
        /// olarak doğrulandı.
        ///
        /// Yalnızca ölçülen kilitten (43,00 m) ve açıklıktan (23,50 m)
        /// türeyen kemer kilidi <b>28,97 m</b>; plandan bağımsız okunan
        /// kemer katı <b>30 m</b>. Bir metrenin altında buluşuyorlar.
        ///
        /// Zincir Üsküdar Mihrimah'ta kurulmuştu, Ayasofya'da Bizans
        /// oranıyla beslenip tutmuştu, burada Osmanlı oranıyla yine tutuyor.
        /// </summary>
        [Test]
        public void SultanahmetDomeChainAgreesWithTheMeasuredPlan()
        {
            var v = Named("Sultanahmet");
            float r = v.dome_d * 0.5f;
            float spring = v.dome_crown_z - r * 0.78f;
            float archCrown = spring - r * (Mathf.Sqrt(2f) - 1f);
            Assert.AreEqual(30.0f, archCrown, 1.2f,
                $"turetilen kemer kilidi {archCrown:F2} m — plandan okunan "
                + "kemer kati 30 m ile bir metre icinde bulusmali.");
        }

        /// <summary>
        /// <b>Dört fil ayağı, çapı beş metre</b> — ve açıklık onlardan türer.
        ///
        /// Bu, 23,50 m'yi bir kaynaktan kopyalamadığımın kaydı: ayak
        /// duvarlarının ekseni plandan 30,75 m aralıklı ölçüldü, duvar
        /// 3,65 m kalın, iç yüzler arası <b>23,45 m</b>. Yayımlanan sayı
        /// tam olarak budur.
        /// </summary>
        [Test]
        public void SultanahmetHasFourPiersFiveMetresAcross()
        {
            var v = Named("Sultanahmet");
            Assert.AreEqual(4, v.piers, "DORT fil ayagi.");
            Assert.AreEqual(5.0f, v.pier_d, 0.05f,
                $"fil ayagi capi {v.pier_d:F2} m — TDV 5 m der.");
        }

        // ===================== Fâtih Camii, 1766 öncesi şema (ADR 0048)

        /// <summary>
        /// <b>1632'de Fâtih Camii'nin BİR yarım kubbesi vardır.</b>
        ///
        /// Bugün görülen dört yarım kubbeli barok şema <b>1767-71</b>'dir;
        /// 1766 depreminden sonra yapının kalanı <i>zemine kadar
        /// yıktırılmıştır</i>. TDV özgün hâli şöyle tarif eder: ortada bir
        /// büyük kubbe, <b>mihrap tarafında bir</b> yarım kubbe, yanlarda
        /// daha alçak <b>üçer</b> küçük kubbeli bölüm.
        ///
        /// Bu test Faz 3'ün en büyük tarihsel farkını kilitliyor. Biri
        /// "Fâtih Camii de selâtin, ötekiler gibi yapayım" derse dört
        /// yarım kubbe koyar ve <b>üç yüzyıl</b> ileri gider.
        /// </summary>
        [Test]
        public void FatihCamiiHasTheOriginalSingleHalfDomeScheme()
        {
            var v = Named("FatihCamii");
            Assert.AreEqual(1, v.half_domes,
                $"{v.half_domes} yarim kubbe — 1632'de MIHRAP yonunde BIR "
                + "tane vardi. Dort yarim kubbeli sema 1767-71'dir.");
            Assert.AreEqual(2, v.piers,
                $"{v.piers} ayak — ozgun kubbe 'duvarlar ve IKI ayak' "
                + "uzerindeydi; bugunku dort fil ayagi sonradandir.");
            Assert.AreEqual(3, v.side_domes,
                "yanlarda UCER kucuk kubbe (sayilan).");
            Assert.AreEqual(6, v.side_domes_total, "toplam ALTI yan kubbe.");
        }

        /// <summary>
        /// Özgün minareler <b>birer</b> şerefeliydi.
        ///
        /// Kaynak: "şimdiki minarelerin yerinde <b>birer şerefeli iki
        /// minare</b>". Bugünküler ikişer şerefelidir. Minarelerin kaide,
        /// pabuç ve gövde başlangıçları ilk yapıdan kalmadır — yani 1632'de
        /// duran taş bugün de duruyor, ama şerefesi durmuyor.
        /// </summary>
        [Test]
        public void FatihCamiiMinaretsHaveOneSerefeEach()
        {
            var v = Named("FatihCamii");
            Assert.AreEqual(2, v.minarets, "IKI minare.");
            Assert.AreEqual(1, v.sherefe_each,
                $"{v.sherefe_each} serefe — ozgun minareler BIRER "
                + "serefeliydi; ikiser serefe sonradandir.");
        }

        /// <summary>
        /// Avlunun sayıları <b>doğrudan 1632'yi bağlar</b>.
        ///
        /// İlk yapıdan bugüne kalanlar: şadırvan avlusunun <b>üç duvarı</b>,
        /// ortadaki <b>şadırvan</b>, <b>taçkapı</b>, <b>mihrap</b> ve
        /// minarelerin şerefe altına kadarki gövdeleri. Yani avlunun
        /// <b>on sekiz sütunu</b>, <b>yirmi iki kubbesi</b> ve <b>üç
        /// kapısı</b> türetilmiş değil, <b>ayakta duran</b> ölçülerdir —
        /// harimden farklı olarak.
        /// </summary>
        [Test]
        public void FatihCamiiCourtyardNumbersAreTheSurvivingOnes()
        {
            var v = Named("FatihCamii");
            Assert.AreEqual(18, v.court_columns, "ON SEKIZ sutun.");
            Assert.AreEqual(22, v.portico_bays, "YIRMI IKI kubbe.");
            Assert.AreEqual(3, v.court_gates, "UC kapi (ikisi yanlarda).");
        }

        /// <summary>
        /// <b>Kapalı revak halkasında göz sayısı mesnet sayısına eşittir</b>
        /// ve fark tam <b>dört</b>: köşeler sütunla değil <b>köşe ayağıyla</b>
        /// taşınır, ayak da kaynakların sütun sayımına girmez.
        ///
        /// Bu, iki kaynağın çeliştiği sanılan bir yerde bulundu.
        /// Sultanahmet için TDV "<b>yirmi altı sütun</b>, <b>otuz</b> kubbeli
        /// birim" der; iki sayı farklı olduğu için biri yanlış görünüyordu.
        /// Değildi: kapalı bir dikdörtgen revakta her göz sınırı bir sonraki
        /// gözle paylaşılır, dolayısıyla mesnet sayısı göz sayısına eşittir
        /// (30) ve dördü köşededir → 26 sütun. Sayılar aynı geometriyi tarif
        /// ediyormuş.
        ///
        /// Sonra Fâtih bunu <b>bağımsız olarak</b> doğruladı: avlusu
        /// 1471'den ayakta ve iki sayısı da ölçülmüş — 22 kubbe, 18 sütun,
        /// fark yine tam dört. Bir yapıda bulunmuş bir okuma, ikincisinde
        /// sınandığı için artık bir yasa.
        ///
        /// Bu test her iki yapıyı da tutar: kit birine uygulanıp öbürüne
        /// uygulanmazsa ya da köşe ayakları sütuna dönüşürse burada kırılır.
        /// </summary>
        [Test]
        public void ClosedArcadeRingHasFourCornerPiers()
        {
            foreach (var ad in new[] { "Sultanahmet", "FatihCamii" })
            {
                var v = Named(ad);
                Assert.Greater(v.portico_bays, 0, ad + ": goz sayisi yok.");
                Assert.Greater(v.court_columns, 0, ad + ": sutun sayisi yok.");
                Assert.AreEqual(4, v.portico_bays - v.court_columns,
                    ad + ": kapali halkada goz - sutun farki tam DORT "
                    + "olmali (dort kose ayagi). Bulunan: "
                    + v.portico_bays + " - " + v.court_columns);
            }
        }

        /// <summary>
        /// Yan bölümler <b>daha alçaktır</b> — özgün şemayı bugünkünden
        /// ayıran basamak budur.
        ///
        /// Kaynağın sözü: "yanlarda <b>daha alçak</b> üçer küçük kubbeli
        /// bölümleri". İlk kurulumda yanlar da orta kütle kadar yüksekti ve
        /// üçer kubbe çatıda 1,3 m'lik kabarcıklara dönüyordu: sayı mesh'te
        /// vardı ama <b>siluette yoktu</b>.
        /// </summary>
        [Test]
        public void FatihCamiiSideSectionsAreLowerThanTheCentre()
        {
            var v = Named("FatihCamii");
            Assert.Greater(v.wall_h, v.aisle_h * 1.25f,
                $"orta kutle {v.wall_h:F1} m, yan nefler {v.aisle_h:F1} m — "
                + "kaynak yanlar icin 'DAHA ALCAK' der ve fark siluette "
                + "okunacak kadar olmali.");
        }

        /// <summary>
        /// Kubbe <b>26 m</b> ve bu, bir yüzyıl boyunca İstanbul'un en
        /// büyüğüydü.
        ///
        /// İddia kendi kendini denetliyor: Fâtih 1470, Süleymaniye 1557
        /// (26,5 m) — arada <b>87 yıl</b> var ve "bir yüzyıl" tarifine
        /// oturuyor. Test iki yapının sırasını da tutar: Süleymaniye
        /// Fâtih'i geçmiş olmalı, ama az.
        /// </summary>
        [Test]
        public void FatihDomeHeldTheRecordUntilSuleymaniye()
        {
            float fatih = Named("FatihCamii").measured_dome_d;
            float sul = Named("Suleymaniye").measured_dome_d;
            Assert.AreEqual(26.0f, fatih, 0.08f,
                $"Fatih kubbesi {fatih:F2} m — 26 m.");
            Assert.Greater(sul, fatih,
                "Suleymaniye (1557) Fatih'in rekorunu KIRDI.");
            Assert.Less(sul - fatih, 2.0f,
                $"aradaki fark {sul - fatih:F2} m — Suleymaniye rekoru az "
                + "farkla kirdi; buyuk fark ikisinden birinin yanlis "
                + "olcuyu tasidigina isarettir.");
        }

        // ================================ Kara surları (ADR 0049)

        /// <summary>
        /// Kesitin toplamı <b>belgeli 70 m</b>'ye oturmak zorunda.
        ///
        /// Kaynağın verdiği asıl sayı katmanların tek tek ölçüleri değil,
        /// <b>toplam savunma derinliği</b>: hendek + parateikhion + dış sur
        /// + peribolos + iç sur + glasi = <b>70 m</b>.
        ///
        /// Ara ölçüler tipolojiktir (D3) ve tek tek değiştirilebilir; ama
        /// toplamları belgeli sayıdan kaçamaz. Bu, "kaynak niteliksel
        /// olduğunda metrik geometri uydurma" kuralının buradaki biçimi:
        /// <b>uydurulan sayı yok, paylaşılan bir toplam var.</b>
        /// </summary>
        [Test]
        public void LandWallSectionSumsToTheDocumentedSeventyMetres()
        {
            Assert.AreEqual(LandWallBuilder.TotalDepth,
                LandWallBuilder.SectionTotal(), 0.01f,
                $"kesit toplami {LandWallBuilder.SectionTotal():F1} m — "
                + $"belgeli deger {LandWallBuilder.TotalDepth:F1} m. Ara "
                + "olculer degistiyse toplam da duzeltilmeli.");
        }

        /// <summary>
        /// Burç aralığı <b>elle girilmez</b> — sayılan 96'dan ve hattın
        /// ölçülen uzunluğundan türer.
        ///
        /// Galata'da "burçlar arası hedef mesafe 60 m" diye bir <i>taslak</i>
        /// sayı yazmıştım ve kaynak vermiyordu. Burada gerek yok. Üstelik
        /// sonuç kendi kendini denetliyor: kaynak aralığı <b>bağımsız</b>
        /// olarak "21-77 m, çoğu 40-60" diye veriyor ve türetilen sayı o
        /// bandın içine düşüyor.
        /// </summary>
        [Test]
        public void LandWallTowerSpacingIsDerivedFromCountAndMeasuredLine()
        {
            string root = TerrainImporter.RepositoryRoot();
            Assert.IsNotNull(root, "Depo koku bulunamadi.");
            string path = Path.Combine(root,
                LandWallBuilder.LocalJsonPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(path), "walls_1632_local.json yok.");

            var doc = GeoJsonImporter.ParseLocal(File.ReadAllText(path));
            List<Vector2> line = null;
            foreach (var f in doc.features)
            {
                if (f.id != "wall_land" || f.rings == null || f.rings.Count == 0)
                    continue;
                line = new List<Vector2>();
                foreach (var q in f.rings[0]) line.Add(new Vector2(q.x, q.z));
            }
            Assert.IsNotNull(line, "wall_land hatti yok.");
            Assert.Greater(line.Count, 1);

            float length = 0f;
            for (int i = 0; i + 1 < line.Count; i++)
                length += Vector2.Distance(line[i], line[i + 1]);

            // Theodosius kesimi ~5,7 km; belgeli 7,5 km Blachernae uzantisini
            // da sayar. Hattimiz o kesimi cizer.
            Assert.AreEqual(5700f, length, 600f,
                $"kara suru hatti {length:F0} m — Theodosius kesimi ~5,7 km "
                + "olmali (belgeli 7,5 km Blachernae'yi de sayar).");

            float spacing = length / LandWallBuilder.InnerTowers;
            Assert.GreaterOrEqual(spacing, 21f,
                $"burc araligi {spacing:F1} m — kaynak 21-77 m verir.");
            Assert.LessOrEqual(spacing, 77f,
                $"burc araligi {spacing:F1} m — kaynak 21-77 m verir.");
        }

        /// <summary>
        /// İç sur burcu duvarın <b>iki katı</b>dır: 25 / 12.
        ///
        /// Bu oran siluetin kendisidir — burç duvarın üstünde bir kulecik
        /// değil, duvarı <b>aşan</b> bir kütledir. Ve 25 m <b>toplam</b>
        /// yüksekliktir: ilk kurulumda onu gövdeye verdim, mesh 28 m çıktı
        /// ve üreticinin kendi denetimi yakaladı.
        /// </summary>
        [Test]
        public void LandWallInnerTowerIsTwiceTheWall()
        {
            var v = Named("KaraSurBurcu");
            Assert.AreEqual(25.0f, v.height, 0.05f,
                $"burc {v.height:F2} m — belgeli TOPLAM yukseklik 25 m.");
            Assert.AreEqual(12.0f, v.wall_h, 0.05f, "ic sur 12 m.");
            Assert.AreEqual(25.0f / 12.0f, v.height / v.wall_h, 0.05f,
                "kaynak burcu duvarin iki kati verir.");
        }

        /// <summary>
        /// <b>İki burç planı da üretilmiş olmalı.</b>
        ///
        /// Kaynak "çoğunlukla <b>kare</b>, bazıları <b>sekizgen</b>, altıgen
        /// ve beşgen" der. Tek tip üretmek belgeye aykırıdır — Galata'da
        /// aynı dersi bir kez almıştım (ADR 0034: <i>hayatta kalan örnek
        /// örneklem değildir</i>); burada kaynak zaten çok tipli olduğunu
        /// söylüyor, yani mazeret bile yok.
        /// </summary>
        [Test]
        public void LandWallHasBothTowerPlans()
        {
            Assert.AreEqual("kare", Named("KaraSurBurcu").plan);
            Assert.AreEqual("sekizgen", Named("KaraSurBurcu_Sekizgen").plan);
            Assert.AreEqual(96, Named("KaraSurBurcu").tower_count,
                "burc sayisi SAYILAN degerdir: 96.");
        }

        /// <summary>
        /// Dış sur iç surdan <b>alçaktır</b> ve burcu da öyle.
        ///
        /// Kaynak dış sura 8,5-9 m verir, iç sura 12. Dış sur burcunun
        /// yüksekliğini <b>vermez</b> — o D3'tür ve doğrulama ilk denemeyi
        /// (12 m) reddetti: gövde dış surun 25 cm üstünde kalıyordu, yani
        /// burç değil duvarın parçası oluyordu.
        /// </summary>
        [Test]
        public void LandWallOuterLineIsLowerThanInner()
        {
            var ic = Named("KaraSurBurcu");
            var dis = Named("KaraSurBurcu_Dis");
            Assert.Less(dis.wall_h, ic.wall_h,
                $"dis sur {dis.wall_h:F2} m, ic sur {ic.wall_h:F2} m — "
                + "dis sur ALCAK olmali.");
            Assert.Less(dis.height, ic.height,
                "dis burc ic burctan alcak olmali.");
            Assert.Greater(dis.body_h, dis.wall_h + 1.0f,
                $"dis burc govdesi {dis.body_h:F2} m, dis sur "
                + $"{dis.wall_h:F2} m — burc duvari asmali.");
        }

        // ================================ Yedikule ve kara kapısı (ADR 0050)

        /// <summary>
        /// <b>Yedi kule</b> — hisarın adı budur, yani sayı yapının kendisidir.
        ///
        /// Üçü Fatih'in <b>dairesel</b> kuleleri, dördü Bizans'tan (Altın
        /// Kapı'nın iki <b>mermer</b> kulesi + Theodosius surunun iki
        /// burcu). Kaynak bunu açıkça ayırır; "yedi kule" deyip hepsini aynı
        /// yapmak adı korur, <b>yapıyı</b> siler.
        /// </summary>
        [Test]
        public void YedikuleHasSevenTowersAndThreeAreRound()
        {
            var v = Named("Yedikule");
            Assert.AreEqual(7, v.towers,
                $"{v.towers} kule — hisarin ADI Yedikule'dir.");
            Assert.AreEqual(3, v.round_towers,
                $"{v.round_towers} dairesel kule — kaynak Fatih'in UC "
                + "dairesel kulesini sayar; kalan dordu Bizans'tandir.");
        }

        /// <summary>
        /// <b>Altın Kapı üç kemerlidir</b>: ortadaki büyük kemer yalnızca
        /// imparatorlara, iki yanındaki küçükler halka. Klasik bir zafer
        /// takıdır ve imparatorların zafer alayı başında şehre girdiği ana
        /// tören kapısıdır.
        ///
        /// Üçünü aynı ölçüde yapmak <i>daha kolay</i> olurdu (tek panel);
        /// ama farkı taşıyan şey büyüklük farkıdır.
        /// </summary>
        [Test]
        public void YedikuleGoldenGateHasThreeArches()
        {
            Assert.AreEqual(3, Named("Yedikule").gate_arches,
                "Altin Kapi UC kemerlidir.");
        }

        /// <summary>
        /// Beşgenin yarıçapı <b>ölçülen alandan</b> türer, elle girilmez.
        ///
        /// Belgeli açık alan <b>15 000 m²</b>; düzgün beşgen alanı
        /// 2,378·R² → <b>R ≈ 79,4 m</b>, kenar ≈ 93 m. Kara surlarındaki
        /// burç aralığıyla aynı ilke: sayı elle yazılmaz, ölçülenden çıkar.
        /// </summary>
        [Test]
        public void YedikuleRadiusComesFromTheMeasuredArea()
        {
            var v = Named("Yedikule");
            Assert.AreEqual(15000f, v.area_m2, 1f, "belgeli alan 15 000 m2.");
            float r = Mathf.Sqrt(v.area_m2 / (2.5f * Mathf.Sin(72f * Mathf.Deg2Rad)));
            Assert.AreEqual(r, v.radius, 0.5f,
                $"yaricap {v.radius:F2} m — {v.area_m2:F0} m2 alandan "
                + $"{r:F2} m cikar; elle girilmis olmamali.");
        }

        /// <summary>
        /// Yedikule bir <b>hisardır</b>, cami değil: kıbleye dönmez.
        ///
        /// Yönü elle de yazılmadı — sur hattının Yedikule'deki <b>dış
        /// normali</b> ölçüldü (261,2°) ve Altın Kapı oraya bakıyor. Hisar
        /// surun içindedir ve kapısı şehirden <b>uzağa</b> açılır.
        /// </summary>
        [Test]
        public void YedikuleFacesOutwardNotTheQibla()
        {
            var v = Named("Yedikule");
            Assert.AreEqual("hisar", v.kind,
                "Yedikule cami degil hisardir; kible kurali ona uygulanmaz.");
            Assert.AreEqual(261.2f, v.face_deg, 0.2f,
                $"Altin Kapi {v.face_deg:F1} dereceye bakiyor — sur hattinin "
                + "oradaki DIS normali 261,2.");
            float delta = Mathf.Abs(Mathf.DeltaAngle(
                v.face_deg, LandmarkPlacer.QiblaEntranceDeg));
            Assert.Greater(delta, 30f,
                "hisarin yonu kibleyle karistirilmamali.");
        }

        /// <summary>
        /// Kara sur kapısı <b>Galata'nın kapısı değildir</b> — ölçüsü de
        /// ondan kopyalanmadı.
        ///
        /// Galata'nın Harup Kapı rölövesi 2,70 m açıklık verir ama o
        /// <b>2 m</b> kalınlığında bir duvarındır. Burada duvar <b>5 m</b>
        /// ve burçlar <b>25 m</b>; aynı açıklık bu kütlede bir mazgal deliği
        /// gibi okunurdu. Kapıyı kapı yapan iki şey teste bağlı: açıklığın
        /// duvara oranı, ve <b>kendi iki burcu</b>.
        /// </summary>
        [Test]
        public void LandGateIsNotACopyOfTheGalataGate()
        {
            var kara = Named("KaraSurKapisi");
            var galata = Named("SurKapisi");
            Assert.Greater(kara.opening, galata.opening,
                $"kara kapisi acikligi {kara.opening:F2} m — Galata'nin "
                + $"{galata.opening:F2} m'sinden genis olmali; duvar iki "
                + "buçuk kat kalin.");
            Assert.AreEqual(2, kara.towers,
                "kara sur kapisi KENDI IKI BURCUYLA gelir.");
            Assert.Greater(kara.height_tower, kara.wall_h + 3f,
                "kapi burclari duvardan belirgin yuksek olmali.");
        }

        // ==================================== Beyazıt Camii (ADR 0051)

        /// <summary>
        /// <b>Minareler arası 79 m</b> — ve bu ölçü kütlenin genişliğini
        /// bağlar.
        ///
        /// Beyazıt'ın minareleri camiye değil <b>tabhâne kanatlarına</b>
        /// bitişiktir; yapının en tanınan sayısal özelliği bu açıklıktır.
        /// Kanat uzunluğu elle girilmez, 79 m'den <b>türer</b>: (79 − harim
        /// dış genişliği) / 2.
        /// </summary>
        [Test]
        public void BeyazitMinaretSpanIsMeasuredAndDrivesTheWings()
        {
            var v = Named("Beyazit");
            Assert.AreEqual(79.0f, v.minaret_span, 0.01f,
                "minareler arasi OLCULU 79 m.");
            float outer = v.harim_w + 2f * 1.6f;
            Assert.AreEqual((79.0f - outer) * 0.5f, v.wing_len, 0.05f,
                $"tabhane kanadi {v.wing_len:F2} m — 79 m'lik olcuden "
                + "TUREMELI, elle girilmemeli.");
        }

        /// <summary>
        /// Sayılan pencereler: ana kubbede <b>yirmi</b>, her yarım kubbede
        /// <b>yedişer</b>.
        ///
        /// Kubbe yirmi dilimli üretiliyor ki pencereler dilim aralarına
        /// düşsün — Ayasofya'daki kırk kaburga ile aynı ilke (ADR 0045):
        /// sayılan bir değer <b>mesh'te</b> yaşamalı, katalogda değil.
        /// </summary>
        [Test]
        public void BeyazitWindowCountsAreCarriedByTheMesh()
        {
            var v = Named("Beyazit");
            Assert.AreEqual(20, v.dome_windows, "ana kubbede YIRMI pencere.");
            Assert.AreEqual(7, v.half_dome_windows,
                "her yarim kubbede YEDISER pencere.");
        }

        /// <summary>
        /// Türetilen kilit kotu, <b>ölçülü dört caminin bandında</b> olmalı.
        ///
        /// Beyazıt'ın kubbe yüksekliği yayımlanmamış. Uydurmak yerine iki
        /// kısıta bağlandı; ikincisi budur: kilit/çap oranı Ayasofya (1,68),
        /// Sultanahmet (1,83), Süleymaniye (2,00) ve Üsküdar Mihrimah
        /// (2,12) bandına düşmeli. Bu test o bandı <b>kataloğun kendisinden</b>
        /// yeniden hesaplar — sabitler kaymışsa bant da kayar.
        /// </summary>
        [Test]
        public void BeyazitDerivedCrownStaysInsideTheMeasuredBand()
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var name in new[] { "Ayasofya", "Sultanahmet",
                                         "Suleymaniye", "UskudarMihrimah" })
            {
                var m = Named(name);
                float ratio = m.dome_crown_z / m.dome_d;
                lo = Mathf.Min(lo, ratio); hi = Mathf.Max(hi, ratio);
            }
            var v = Named("Beyazit");
            float r = v.dome_crown_z / v.dome_d;
            Assert.GreaterOrEqual(r, lo - 0.02f,
                $"Beyazit kilit/cap {r:F2} — olculu bandin ({lo:F2}-{hi:F2}) "
                + "altinda; turetilen bir kot olcunun disina cikamaz.");
            Assert.LessOrEqual(r, hi + 0.02f,
                $"Beyazit kilit/cap {r:F2} — olculu bandin ({lo:F2}-{hi:F2}) "
                + "ustunde.");
        }

        /// <summary>
        /// <b>Şadırvanın kubbesi 1632'de yok.</b>
        ///
        /// TDV: sekiz sütuna oturan kubbeyi <b>IV. Murad</b> eklettirmiştir,
        /// <b>1623-1640</b> arası — oyunun yılı o aralığın tam ortası.
        /// Model kubbeyi koymuyor çünkü Murad IV gerçek iktidarı 1632'de
        /// aldı ve büyük hayrat işleri ondan sonra beklenir.
        ///
        /// Bu bir <b>olasılık</b>, kesinlik değil; katalog kararı açıkça
        /// kaydediyor ki bir gün tersi belgelenirse tek satır değişsin.
        /// </summary>
        [Test]
        public void BeyazitSadirvanHasNoDomeIn1632()
        {
            var v = Named("Beyazit");
            Assert.IsTrue(v.sadirvan, "avlunun ortasinda sadirvan var.");
            Assert.IsFalse(v.sadirvan_dome,
                "sadirvan kubbesi IV. Murad'in ekidir (1623-1640) ve "
                + "1632'de buyuk olasilikla YOKTUR.");
            StringAssert.Contains("1623-1640", v.source,
                "kararin gerekcesi kaynak notunda durmali.");
        }

        // ==================================== Bedestenler (ADR 0053)

        /// <summary>
        /// <b>Üç bağımsız sayı bir geometriyi kapatıyor.</b>
        ///
        /// Bedesten bir ızgaradır: kubbeler sıra sıra dizilir, ayaklar
        /// ızgaranın <b>iç düğümlerinde</b> durur. Yani
        /// <c>kubbe = sütun × satır</c> ve
        /// <c>ayak = (sütun−1) × (satır−1)</c>.
        ///
        /// Kaynaklar üçünü de <b>ayrı ayrı</b> verir ve üçü de tutar:
        /// Cevahir 15 kubbe / 8 ayak / 5×3; Sandal 20 kubbe / 12 ayak /
        /// 5×4. Bu, projede ilk kez üç bağımsız sayının bir geometriyi
        /// kapatması — biri değişirse öteki ikisi de değişmek zorunda.
        /// </summary>
        [Test]
        public void BedestenGridClosesOnThreeIndependentNumbers()
        {
            foreach (var name in new[] { "CevahirBedesteni", "SandalBedesteni" })
            {
                var v = Named(name);
                Assert.AreEqual(v.cols * v.rows, v.domes,
                    $"{name}: kubbe = sutun x satir olmali "
                    + $"({v.cols}x{v.rows} = {v.cols * v.rows}, katalog "
                    + $"{v.domes}).");
                Assert.AreEqual((v.cols - 1) * (v.rows - 1), v.piers,
                    $"{name}: ayak = (sutun-1) x (satir-1) olmali.");
            }
            Assert.AreEqual(15, Named("CevahirBedesteni").domes,
                "Cevahir ON BES kubbelidir.");
            Assert.AreEqual(8, Named("CevahirBedesteni").piers,
                "Cevahir SEKIZ ayaklidir (iki sira).");
            Assert.AreEqual(20, Named("SandalBedesteni").domes,
                "Sandal YIRMI kubbelidir.");
            Assert.AreEqual(12, Named("SandalBedesteni").piers,
                "Sandal ON IKI ayaklidir (uc sira).");
        }

        /// <summary>
        /// Izgara <b>ölçüyle de</b> tutmalı: kubbeli bir göz <b>kareye
        /// yakın</b> olur.
        ///
        /// Cevahir'in gözü 9,06 × 9,83 m, Sandal'ınki 8,00 × 8,00 m. Bu,
        /// ızgaranın doğru seçildiğinin bağımsız denetimidir — 15 kubbeyi
        /// 15×1 diye dizmek sayıyı korur, <b>yapıyı</b> siler.
        /// </summary>
        [Test]
        public void BedestenBaysAreNearlySquare()
        {
            foreach (var name in new[] { "CevahirBedesteni", "SandalBedesteni" })
            {
                var v = Named(name);
                float oran = v.bay_w / v.bay_d;
                Assert.GreaterOrEqual(oran, 0.80f,
                    $"{name}: goz {v.bay_w:F2} x {v.bay_d:F2} m (oran "
                    + $"{oran:F2}) — kubbeli goz kareye yakin olmali.");
                Assert.LessOrEqual(oran, 1.25f,
                    $"{name}: goz orani {oran:F2} — kareye yakin olmali.");
            }
        }

        /// <summary>
        /// Cevahir'in kubbe kilidi <b>ölçülüdür</b> (14,89 m); Sandal'ınki
        /// <b>türetilmiştir</b> ve ondan alçaktır.
        ///
        /// Sandal'ın gözü daha küçük (8,00 &lt; 9,44), dolayısıyla kubbesi
        /// de alçak olmalı. Bu ilişki tersine dönerse türetme yanlış
        /// yapılmış demektir.
        /// </summary>
        [Test]
        public void SandalIsLowerThanCevahirBecauseItsBaysAreSmaller()
        {
            var cev = Named("CevahirBedesteni");
            var san = Named("SandalBedesteni");
            Assert.AreEqual(14.89f, cev.dome_crown_z, 0.02f,
                "Cevahir'in kilidi OLCULU: 14,89 m.");
            Assert.Less(san.bay_w, cev.bay_w,
                "Sandal'in gozu Cevahir'inkinden kucuk.");
            Assert.Less(san.dome_crown_z, cev.dome_crown_z,
                $"Sandal {san.dome_crown_z:F2} m, Cevahir "
                + $"{cev.dome_crown_z:F2} m — kucuk gozlu bedesten ALCAK "
                + "olmali; tersi turetmenin yanlis oldugunu gosterir.");
        }

        /// <summary>
        /// Bedestenin <b>dört</b> kapısı vardır ve kapalılığı onun
        /// <b>tanımıdır</b>: kıymetli mal saklanan, gece kilitlenen yerdir.
        /// </summary>
        [Test]
        public void BedestenHasFourDoors()
        {
            foreach (var name in new[] { "CevahirBedesteni", "SandalBedesteni" })
                Assert.AreEqual(4, Named(name).doors,
                    $"{name}: bedestenin DORT kapisi var.");
        }

        /// <summary>
        /// <b>1632'de Kapalıçarşı bugünkü değildir</b> ve bu kaynak notunda
        /// yazmalı.
        ///
        /// Bugün akla gelen kâgir tonozlu sokaklar ağı sonradır (1701
        /// yangını, 1894 depremi onarımları); 17. yüzyılda aralar <b>ahşap</b>
        /// örtülüydü. Üstelik <b>1618 yangını</b> 1632'den yalnızca on dört
        /// yıl öncedir. Bu yüzden yalnızca iki bedesten üretildi.
        /// </summary>
        [Test]
        public void BedestenSourceRecordsThatTheBazaarIsNotYetVaulted()
        {
            foreach (var name in new[] { "CevahirBedesteni", "SandalBedesteni" })
            {
                var v = Named(name);
                StringAssert.Contains("1618", v.source,
                    $"{name}: 1618 yangini kaynak notunda anilmali — "
                    + "1632'de carsi yakin zamanda yeniden kurulmustu.");
                StringAssert.Contains("AHSAP", v.source,
                    $"{name}: 17. yuzyilda aralarin AHSAP ortulu oldugu "
                    + "kayitli olmali.");
            }
        }

        // ============================ Padişah türbeleri (ADR 0054)

        /// <summary>
        /// <b>Üç türbe, üç ayrı plan</b> — ve üçünü de düzgün sekizgen
        /// yapmak katalogda tutarlı görünürdü.
        ///
        /// Kaynaklar planları ayrı ayrı verir: II. Selim <b>kare, köşeleri
        /// pahlı</b> (Sinan, 1577); III. Murad <b>altıgen</b> (Dâvud Ağa,
        /// 1599); III. Mehmed <b>sekizgen</b> (1604-08). Faz 3 boyunca
        /// kovaladığım hata tam olarak buydu: birbirine benzeyen yapıları
        /// tek şablona indirmek (yarım kubbe sayıları, ADR 0048).
        ///
        /// Kare-pahlı plan <b>düzgün değildir</b> — dört uzun, dört kısa
        /// yüz — ve bu, yüz uzunluklarının yayılımından <b>ölçülür</b>.
        /// Düzgün sekizgende yayılım sıfırdır.
        /// </summary>
        [Test]
        public void SultanTurbePlansStayThreeDifferentThings()
        {
            var selim = Named("TurbeSelimII");
            var murad = Named("TurbeMuradIII");
            var mehmed = Named("TurbeMehmedIII");

            Assert.AreEqual("kare_pahli", selim.plan,
                "II. Selim: kare, koseleri pahli (Sinan, 1577).");
            Assert.AreEqual("altigen", murad.plan,
                "III. Murad: ALTIGEN (Davud Aga, 1599).");
            Assert.AreEqual("sekizgen", mehmed.plan,
                "III. Mehmed: SEKIZGEN (1604-08).");

            Assert.AreEqual(6, murad.sides, "altigenin ALTI yuzu var.");
            Assert.AreEqual(8, mehmed.sides, "sekizgenin SEKIZ yuzu var.");
            Assert.AreEqual(8, selim.sides,
                "kare-pahli plan da sekiz yuzlu — ama DUZGUN DEGIL.");

            // Ve iste ayrimin OLCUSU: kare-pahli duzgun degildir.
            Assert.Greater(selim.face_spread, 0.3f,
                $"II. Selim'in yuz yayilimi {selim.face_spread:F3} — kare "
                + "koseleri pahli bir plan DUZGUN OLAMAZ; sifira yakinsa "
                + "duzgun sekizgene donmus demektir.");
            Assert.Less(mehmed.face_spread, 0.02f,
                $"III. Mehmed'in yuz yayilimi {mehmed.face_spread:F3} — "
                + "duzgun sekizgen olmali.");
        }

        /// <summary>
        /// III. Murad'ın türbesi <b>revaklı</b> ve <b>mermer kaplı</b>;
        /// Osmanlı'nın en büyük türbelerinden.
        ///
        /// Kaynak ikisini de ayrıca söyler. Öteki iki Ayasofya türbesinde
        /// ne revak ne mermer var — kataloğun bunları ayrı alanlarda
        /// tutmasının sebebi bu.
        /// </summary>
        [Test]
        public void MuradIIITurbeIsTheMarbleOneWithAPorch()
        {
            var murad = Named("TurbeMuradIII");
            Assert.IsTrue(murad.revak, "III. Murad turbesi REVAKLIDIR.");
            Assert.IsTrue(murad.marble, "distan MERMER kaplidir.");
            Assert.IsFalse(Named("TurbeSelimII").marble,
                "II. Selim turbesi mermer kapli diye anilmaz.");
            Assert.IsFalse(Named("TurbeMehmedIII").revak,
                "III. Mehmed turbesinde revak anilmaz.");
            // En buyuklerinden: oteki Ayasofya turbelerinden buyuk olmali.
            Assert.Greater(murad.footprint_x,
                Named("TurbeMehmedIII").footprint_x,
                "III. Murad turbesi Osmanli'nin en buyuk turbelerindendir.");
        }

        /// <summary>
        /// Üçü de <b>çift kabuklu</b>dur ve iç kabuk <b>üretilmez</b>.
        ///
        /// Sinan'ın Kanûnî türbesinde kullandığı örtü. İç kabuk dışarıdan
        /// görünmez; Ayasofya'nın eksedralarında verilen kararın aynısı
        /// (ADR 0045): görünmeyen geometri mesh'e girmez, katalogda kalır.
        /// </summary>
        [Test]
        public void SultanTurbesRecordTheDoubleShellWithoutModellingIt()
        {
            foreach (var n in new[] { "TurbeSelimII", "TurbeMuradIII",
                                      "TurbeMehmedIII" })
                Assert.IsTrue(Named(n).double_shell,
                    $"{n}: cift kubbeli oldugu KAYITLI olmali.");
        }

        /// <summary>
        /// <b>1632'de Ayasofya haziresinde dört türbe vardır, beş değil.</b>
        ///
        /// I. Mustafa ve İbrahim türbesi <b>1639</b>'dur ve o tarihte
        /// Ayasofya'nın vaftizhânesi hâlâ <b>yağhânedir</b> (ADR 0045).
        /// Bu test kataloğun o beşinciyi taşımadığını sınıyor — bir gün
        /// biri "Ayasofya'nın türbeleri" diye toptan eklerse patlar.
        /// </summary>
        [Test]
        public void NoMustafaITurbeIn1632()
        {
            foreach (var v in Load())
            {
                Assert.IsFalse(v.name != null && v.name.Contains("Mustafa"),
                    "I. Mustafa turbesi 1639'dur; 1632'de vaftizhane hala "
                    + "YAGHANEDIR.");
                Assert.IsFalse(v.name != null && v.name.Contains("Ibrahim")
                               && v.kind == "turbe_selatin",
                    "Sultan Ibrahim 1648'de gomuldu.");
            }
            // Ayasofya'nin kendi kaydi da bunu soylemeli.
            Assert.IsFalse(Named("Ayasofya").turbe_of_mustafa);
        }

        // ================= İskele ve Alay Köşkü (ADR 0055)

        /// <summary>
        /// Üsküdar iskelesi <b>1632'de ahşaptır</b> ve caminin adının
        /// kaynağıdır.
        ///
        /// Üsküdar Mihrimah Sultan Camii'nin yaygın adı <b>"İskele
        /// Camii"</b>dir; yani iskele camiden bağımsız bir ayrıntı değil,
        /// caminin adının sebebi. Faz 3'te cami üretilmiş ama iskele
        /// "eksik" diye kayıtlıydı (ADR 0036).
        ///
        /// Kâgir rıhtımlar 19. yüzyıldır. Yapısal ahşap <b>boyanmaz</b>
        /// (ADR 0035) — tuzlu havada duran bir iskele aşı boyalı bir cumba
        /// değildir.
        /// </summary>
        [Test]
        public void UskudarPierIsTimberAndReachesIntoTheWater()
        {
            var v = Named("UskudarIskelesi");
            Assert.AreEqual("iskele", v.kind,
                "tur 'iskele' olmali — yerlestirici onu SUYA dondurur.");
            Assert.AreEqual("ahsap", v.material,
                "1632'de iskele AHSAPTIR; kagir rihtimlar 19. yuzyildir.");
            // Iskele DENIZE UZANIR: boyu eninin en az uc kati.
            Assert.Greater(v.length, v.width * 3f,
                $"iskele {v.length:F1} x {v.width:F1} m — denize uzanan bir "
                + "yapi; boyu eninin en az uc kati olmali, yoksa rihtim olur.");
            Assert.GreaterOrEqual(v.piles, 4, "en az dort kazik cifti.");
        }

        /// <summary>
        /// Alay Köşkü <b>1632'de ahşaptır</b> — ve bugünkünden <b>alçak
        /// değil</b>.
        ///
        /// Bugünkü kâgir köşk <b>1810</b> ya da <b>1819-20</b>, II.
        /// Mahmud'undur; kaynak o yapının <b>daha yüksek</b> bir köşkün ya
        /// da kulenin yerine geçtiğini söyler. Yani burada 1632 yapısı
        /// bugünkünden <b>yüksektir</b> — Galata Kulesi (ADR 0033) ve
        /// Adalet Kulesi'nin (ADR 0040) <b>tersi</b>.
        ///
        /// "Eski olan alçaktır" diye bir kural yok; her yapı ayrı sorulur.
        /// </summary>
        [Test]
        public void AlayKoskuIsTimberAndProjectsOverTheStreet()
        {
            var v = Named("AlayKosku");
            Assert.AreEqual("ahsap", v.material,
                "1632'de Alay Kosku AHSAPTIR; kagir kosk 1810/1819-20.");
            Assert.Greater(v.cumba, 1.2f,
                $"tasma {v.cumba:F2} m — Alay Kosku'nu Alay Kosku yapan sey "
                + "sokaga TASMASIDIR; tasmayan bir kutle seyir yeri olmaz.");
            StringAssert.Contains("DAHA YUKSEK", v.source,
                "1632 yapisinin bugunkunden YUKSEK oldugu kayitli olmali.");
        }

        /// <summary>
        /// <b><c>face_deg = 0</c> bildirim sayılmaz.</b>
        ///
        /// Alay Köşkü'nün kaydında bir kez <c>face_deg = 0.0</c> vardı ve
        /// "yön bildirilmedi" demek istiyordum; yerleştirici onu <b>"kuzeye
        /// bak"</b> diye okudu. Sıfır hem "yok" hem "kuzey" anlamına
        /// gelemez.
        ///
        /// Sözleşme: kuzeye bakan bir yapı <b>360</b> yazar; sıfır ya da
        /// negatif "bildirilmedi" demektir. Bu test kataloğun hiçbir yerinde
        /// sıfır bir <c>face_deg</c> kalmadığını sınıyor — kalırsa yapı
        /// sessizce kuzeye döner.
        /// </summary>
        [Test]
        public void NoLandmarkDeclaresZeroFacing()
        {
            // KATALOGUN HAM METNINE bakilir, ayristirilmis nesneye degil.
            //
            // Ilk yazimda `Load()` uzerinden `face_deg != 0` diye
            // sinamistim ve premis YANLISTI: alani hic yazmayan bir varlikta
            // JsonUtility zaten 0 verir, yani "yazmadi" ile "sifir yazdi"
            // ayni gorunur. Ayrimi ancak metin tasir.
            string root = TerrainImporter.RepositoryRoot();
            Assert.IsNotNull(root, "Depo koku bulunamadi.");
            foreach (var path in LandmarkPlacer.CatalogPaths(root))
            {
                string text = File.ReadAllText(path);
                StringAssert.DoesNotContain("\"face_deg\": 0.0", text,
                    $"{path}: bir kayitta face_deg TAM SIFIR. "
                    + "Yerlestiricide bu 'KUZEYE BAK' demektir, "
                    + "'bildirilmedi' degil — kuzeye bakan bir yapi 360 "
                    + "yazar, bildirmeyen alani HIC yazmaz.");
                StringAssert.DoesNotContain("\"face_deg\": 0,", text,
                    $"{path}: bir kayitta face_deg TAM SIFIR.");
            }
        }
    }
}
