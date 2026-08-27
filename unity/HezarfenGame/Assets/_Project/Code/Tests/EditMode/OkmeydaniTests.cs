using System.Collections.Generic;
using Hezarfen.Core;
using Hezarfen.Editor.Gis;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hezarfen.Tests
{
    /// <summary>
    /// Okmeydanı sahnesinin <b>belgeye bağlı</b> iddialarını kilitler.
    ///
    /// Bunların hiçbiri gözle denetlenemez: tekkenin <b>minaresiz</b> olması
    /// (minare 1770–71), namazgâhın <b>kıbleye</b> dönmesi, her menzilin
    /// yönünün <b>rüzgârından</b> çıkması, taşlar arasındaki mesafenin taşın
    /// üstünde yazan gez sayısı olması, ve Tozkoparan'ın taşının eksenden
    /// tam <b>80 gez şast</b>ta durması — ki o taş ayrı bir menzil sayılmasını
    /// bu sapmaya borçludur.
    ///
    /// En güzeli sonuncular: dünyadaki nesne, belgedeki sayıyı <b>ölçüyor</b>.
    /// </summary>
    public class OkmeydaniTests
    {
        private Scene scene;

        [SetUp]
        public void SetUp()
        {
            if (!System.IO.File.Exists(OkmeydaniBuilder.ScenePath))
                Assert.Ignore("Okmeydani sahnesi yok — Hezarfen/GIS/Okmeydani sahnesi kur");
            scene = EditorSceneManager.OpenScene(OkmeydaniBuilder.ScenePath,
                                                 OpenSceneMode.Additive);
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }

        // ------------------------------------------------------------ varlık

        [Test]
        public void SceneHasTekkeNamazgahAndStones()
        {
            Assert.IsNotNull(Find("PF_Tekke_Okcular"), "Okcular Tekkesi yok.");
            Assert.IsNotNull(Find("PF_Namazgah_Okmeydani"), "Namazgah yok.");
            Assert.GreaterOrEqual(FootStones().Count, 3, "En az uc ayak tasi bekleniyordu.");
            Assert.GreaterOrEqual(HeadStones().Count, 4, "En az dort bas tasi bekleniyordu.");
        }

        [Test]
        public void EverythingCarriesItsSource()
        {
            // CLAUDE.md: her sahne ogesine HistoricalTag. Not kaybolursa
            // "minaresiz" gibi bir YOKLUK iddiasi da kaybolur.
            foreach (var t in Roots())
            {
                var tag = t.GetComponent<HistoricalTag>();
                Assert.IsNotNull(tag, $"{t.name}: HistoricalTag yok.");
                Assert.IsTrue(tag.IsValid, $"{t.name}: etiket eksik.");
            }
        }

        // ----------------------------------------------------- belgeye baglı

        [Test]
        public void TekkeHasNoMinaret()
        {
            // RESEARCH.md: tekke mescidinin minaresi ancak 1770-71'de eklendi.
            // 1632'de MINARESIZDIR. Bir gun biri "mescit gibi dursun" diye
            // minare eklerse bu test onu yakalar.
            var tekke = Find("PF_Tekke_Okcular");
            Assert.IsNotNull(tekke);

            var mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var mx = -mn;
            foreach (var r in tekke.GetComponentsInChildren<Renderer>(true))
            {
                mn = Vector3.Min(mn, r.bounds.min);
                mx = Vector3.Max(mx, r.bounds.max);
            }
            float h = mx.y - mn.y;
            float footprint = Mathf.Max(mx.x - mn.x, mx.z - mn.z);

            Assert.Less(h / footprint, 0.55f,
                $"Tekke {h:F1} m yuksek, ayak izi {footprint:F1} m — bu oran "
                + "minareli bir yapiya benziyor. RESEARCH.md: 1632'de MINARESIZ.");

            StringAssert.Contains("MINARESIZ",
                tekke.GetComponent<HistoricalTag>().sourceNote.ToUpperInvariant());
        }

        [Test]
        public void NamazgahFacesQibla()
        {
            var nz = Find("PF_Namazgah_Okmeydani");
            Assert.IsNotNull(nz);

            // Modelde mihrap +Y'de; Unity'ye -Z olarak gelir. Yani kible yonu
            // nesnenin ARKASIDIR: -forward.
            Vector3 back = -nz.transform.forward;
            float azi = Mathf.Repeat(Mathf.Atan2(back.x, back.z) * Mathf.Rad2Deg, 360f);
            Assert.AreEqual(OkmeydaniBuilder.QiblaAzimuthDeg, azi, 2.0f,
                $"Mihrap {azi:F1} dereceye bakiyor, kible "
                + $"{OkmeydaniBuilder.QiblaAzimuthDeg:F1}.");
        }

        // -------------------------------------------------------- menzil işi

        [Test]
        public void ShotDirectionIsTheWindReversed()
        {
            // Kaynak: "Her menzil icin belirlenen bir ruzgar vardi... ruzgar
            // ARKAYA alinip aticinin ruzgardan faydalanmasi saglanir." Ruzgar
            // adlari geldigi yonu soyler, o halde ok TERS yone gider.
            //
            // Isaret hatasi buradaki en kolay hatadir ve sessizdir: her seyi
            // 180 derece dondurur, hicbir sey kirilmaz, sahne "calisir".
            Assert.AreEqual(180f, OkmeydaniBuilder.ShotAzimuth("yildiz"), 0.01f,
                "Yildiz KUZEYDEN eser; ok guneye gitmeli.");
            Assert.AreEqual(45f, OkmeydaniBuilder.ShotAzimuth("lodos"), 0.01f,
                "Lodos GUNEYBATIDAN eser; ok kuzeydoguya gitmeli.");
            Assert.AreEqual(270f, OkmeydaniBuilder.ShotAzimuth("gundogusu"), 0.01f,
                "Gundogusu DOGUDAN eser; ok batiya gitmeli.");
        }

        [Test]
        public void EveryMenzilIsLongEnoughToBeOne()
        {
            // Menzil acmanin alt siniri 900 gezdir; bunun altina ok atan
            // kemankes menzil acamaz. Once elimde 588 m'lik (~890 gez) bir
            // "menzil" vardi — kaideye gore menzil bile degil.
            foreach (var m in OkmeydaniBuilder.Menzils)
                foreach (var s in m.stones)
                    Assert.GreaterOrEqual(s.gez, OkmeydaniBuilder.MinMenzilGez,
                        $"{m.name}: {s.gez} gez — menzil acmanin alt siniri "
                        + $"{OkmeydaniBuilder.MinMenzilGez} gez.");
        }

        [Test]
        public void EachStoneMeasuresItsWrittenDistanceInGez()
        {
            // ASIL TEST: taslarin arasindaki mesafe, tasin uzerinde yazan
            // sayidir. Menzil tasi bir sus degil bir OLCUDUR.
            //
            // Mesafe EKSEN BOYUNCA olculur, kus ucusu degil: Tozkoparan'in
            // tasi eksenden 80 gez yanda durur ve menzili yine 1279,5 gezdir.
            foreach (var m in OkmeydaniBuilder.Menzils)
            {
                Vector2 fwd = Dir(OkmeydaniBuilder.ShotAzimuth(m.wind));
                Vector2 sast = new Vector2(-fwd.y, fwd.x);
                Vector2 foot = FootOf(m.id);

                foreach (var s in m.stones)
                {
                    var go = Find($"BasTasi_{m.id}_{Mathf.RoundToInt(s.gez)}gez");
                    Assert.IsNotNull(go, $"{m.name}: {s.gez:0.#} gezlik bas tasi yok.");

                    Vector2 d = new Vector2(go.position.x, go.position.z) - foot;
                    float alongGez = Vector2.Dot(d, fwd) / OkmeydaniBuilder.GezM;
                    Assert.AreEqual(s.gez, alongGez, 5f,
                        $"{m.name}: yazili {s.gez:0.#} gez, olculen {alongGez:0.#} gez.");

                    float sideGez = Vector2.Dot(d, sast) / OkmeydaniBuilder.GezM;
                    if (Mathf.Abs(s.sideGez) > 0.01f)
                        Assert.AreEqual(s.sideGez, sideGez, 3f,
                            $"{m.name}: tas eksenden {sideGez:0.#} gez yanda, "
                            + $"belgede {s.sideGez:0.#} gez.");
                }
            }
        }

        [Test]
        public void TozkoparanStoneIsOutsideTheCorridor_WhichIsWhyItIsItsOwnMenzil()
        {
            // Tozkoparan'in oku ana tasin 80 gez SASTINA dustu; 17. yuzyil
            // kaidesi koridoru her yandan 40 gezle sinirlar, yani atis "asiri
            // salki"ydi. Tas ancak Seyhulmeydan Hamdullah Efendi'nin karariyla
            // AYRI BIR MENZIL sayilarak ayakta kaldi.
            //
            // Yani bu 80 gez tesadufi bir sayi degil, menzilin var olma
            // sebebidir. Biri "duzgun dursun" diye taşı eksene çekerse hikaye
            // de gider — test bunu bekliyor.
            OkmeydaniBuilder.Stone stone = default;
            bool found = false;
            foreach (var m in OkmeydaniBuilder.Menzils)
                if (m.id == "DelikliKaya") { stone = m.stones[0]; found = true; }
            Assert.IsTrue(found, "Delikli Kaya menzili yok.");

            Assert.Greater(Mathf.Abs(stone.sideGez), OkmeydaniBuilder.CorridorHalfGez,
                $"Tozkoparan'in tasi eksenden {stone.sideGez} gez yanda — "
                + $"koridorun yari eni {OkmeydaniBuilder.CorridorHalfGez} gez. "
                + "Koridorun ICINDE kalsaydi ayri bir menzil sayilmazdi.");

            // Ve iki menzil AYNI ayak yerini paylasir: Tozkoparan o oku
            // Havandelen'in ayak tasindan atmisti. Ayri bir ayak tasi dikmek,
            // ayri bir atis yeri uydurmak olurdu.
            Assert.IsNotNull(Find("AyakTasi_Havandelen"), "Havandelen ayak tasi yok.");
            Assert.IsNull(Find("AyakTasi_DelikliKaya"),
                "Delikli Kaya'ya AYRI ayak tasi dikilmis. Menzil ayri sayildi "
                + "ama atis Havandelen'in ayak yerinden yapilmisti.");
        }

        [Test]
        public void RecordStoneMatchesTheEncyclopaediaMetre()
        {
            // TDV 'Okmeydani' meydanin rekorunu 845,66 m verir. Bizde rekor
            // Arkuri Menzili'nde 1281,5 gezdir. Ikisi ancak 1 gez = 0,66 m
            // kabulunde ortusur — gez sabitini degistiren biri capa kaynagiyla
            // celismis olur ve bu test onu uyarir.
            foreach (var m in OkmeydaniBuilder.Menzils)
                if (m.id == "Arkuri")
                {
                    float metres = m.stones[0].gez * OkmeydaniBuilder.GezM;
                    Assert.AreEqual(845.66f, metres, 1.0f,
                        $"Rekor {metres:F2} m cikti, TDV 845,66 m. Gez sabiti "
                        + $"({OkmeydaniBuilder.GezM}) capa kaynakla celisiyor.");
                    return;
                }
            Assert.Fail("Arkuri Menzili yok — meydanin rekoru orada.");
        }

        [Test]
        public void StonesAreMarbleNotMasonry()
        {
            // Kaynak: menzil taslari "tek parca MERMER sutun"dur. Once kesme
            // tas malzemesiyle uretiliyorlardi; kesme tas bir DUVAR
            // malzemesidir ve sutuna sarildiginda derzleri "tek parca"
            // iddiasini yalanliyordu — sahnede olculen dikey periyot 0,95 m,
            // yani bir tas sirasi. Ayrica tas cayirdan 4,4 kat koyuydu
            // (36,7 / 162,5); mermer isikta duran en acik seydir.
            //
            // Bu testin bir gecmisi var: kusuru bir DOKU ISTATISTIGI ile
            // yakalamayi uc formulasyonla denedim ve ucu de bilinen-kusurluyu
            // bilinen-iyiden ayiramadi (gerekce: gen_marble_texture.py).
            // Olcemeyen alet sayi uretmemeli; koruma bu yuzden piksel degil
            // BORU HATTI seviyesinde: tas mermer rolunu kullanir.
            foreach (var t in HeadStones())
                foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                    {
                        Assert.IsNotNull(m, $"{t.name}: bos malzeme yuvasi.");
                        StringAssert.StartsWith("M_Marble", m.name,
                            $"{t.name} '{m.name}' kullaniyor. Menzil tasi "
                            + "MERMERDIR; duvar malzemesi derz getirir.");
                    }
        }

        [Test]
        public void HeadStonesLookBackAtTheShootingPlace()
        {
            // Kaynak: menzil taslari "mihrabi atisin yapildigi yere dogru
            // bakan" taslardir. Kitabe modelde -Y'de, Unity'de +Z = onu.
            // Yani tasin ONU ayak tasina donmeli: yazi, atisin yapildigi
            // yerden okunur.
            //
            // DIKKAT — bu testin goremedigi sey: kitabenin GERCEKTEN o yuzde
            // olup olmadigi. Olculen sey nesnenin donusudur, geometrisi
            // degil. Kitabe bir kez sekizgenin KOSESINE denk geldigi icin
            // yalnizca kenari gorunur haldeydi ve bu test yine geciyordu;
            // kusuru Blender inceleme paketi gosterdi. Geometrinin bekcisi
            // renders/review/MenzilTasi_*/contact_sheet.png'dir.
            foreach (var m in OkmeydaniBuilder.Menzils)
            {
                Vector2 foot = FootOf(m.id);
                foreach (var s in m.stones)
                {
                    var go = Find($"BasTasi_{m.id}_{Mathf.RoundToInt(s.gez)}gez");
                    Assert.IsNotNull(go);
                    Vector2 toFoot = (foot - new Vector2(go.position.x,
                                                         go.position.z)).normalized;
                    var fwd = new Vector2(go.forward.x, go.forward.z).normalized;
                    Assert.Greater(Vector2.Dot(fwd, toFoot), 0.9f,
                        $"{go.name}: kitabe ayak tasina bakmiyor.");
                }
            }
        }

        [Test]
        public void BuildingsStayOffTheField_StonesDoNot()
        {
            // II. Bayezid vakfiyesi meydana YAPI yapilmasini yasaklar; menzil
            // taslari ise meydanin kendi donanimidir. Kural bu yuzden iki
            // yonlu ve testin iki yonu de olculuyor — yalnizca "yapi disarida"
            // demek, taslari da disari atan bir hatayi gecirirdi.
            var areas = GreeneryBuilder.ReadAreas();
            if (areas == null) Assert.Ignore("greenery_local.json yok.");
            GreeneryBuilder.Area field = null;
            foreach (var a in areas)
                if (a.id == "G_Okmeydani_Yasak") { field = a; break; }
            Assert.IsNotNull(field);

            foreach (string n in new[] { "PF_Tekke_Okcular", "PF_Namazgah_Okmeydani" })
            {
                var go = Find(n);
                Assert.IsNotNull(go);
                Assert.IsFalse(Inside(field, go.position),
                    $"{n} meydanin ICINDE. II. Bayezid vakfiyesi meydana YAPI "
                    + "yapilmasini yasaklar — yapilar cepere konur.");
            }

            // Taslarin HEPSI iceride olmali: bir menzil koridoru meydanin
            // disina tasarsa o atis meydanda yapilmamis demektir.
            var stones = new List<Transform>(HeadStones());
            stones.AddRange(FootStones());
            foreach (var s in stones)
                Assert.IsTrue(Inside(field, s.position),
                    $"{s.name} meydanin DISINDA — koridor alana sigmamis.");
        }

        [Test]
        public void NoStoneStandsInWater()
        {
            // Olctum: merkeze gore simetrik yerlestirmede Arkuri'nin bas tasi
            // kot -12 m'ye, yani Halic'e dusuyordu. Poligonun icinde olmak
            // yetmiyor — alanin yaklasik %8'i su.
            foreach (var t in HeadStones())
                Assert.Greater(t.position.y, 2.0f,
                    $"{t.name} kot {t.position.y:F1} m — suda ya da kiyi cizgisinde.");
            foreach (var t in FootStones())
                Assert.Greater(t.position.y, 2.0f,
                    $"{t.name} kot {t.position.y:F1} m — suda.");
        }

        // ---------------------------------------------------------- yardımcı

        /// <summary>
        /// YALNIZ "Okmeydani" kökünün altı.
        ///
        /// İlk yazımda sahnedeki bütün kök nesnelerin çocukları geziliyordu ve
        /// test, arazi sahnesinden gelen GIS öğelerini (kıyı çizgisi, surlar)
        /// de Okmeydanı varlığı sanıp onlarda HistoricalTag arıyordu.
        /// </summary>
        private IEnumerable<Transform> Roots()
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name != "Okmeydani") continue;
                foreach (Transform t in go.transform) yield return t;
            }
        }

        private Transform Find(string prefix)
        {
            foreach (var t in Roots())
                if (t.name.StartsWith(prefix)) return t;
            return null;
        }

        private List<Transform> HeadStones() => Collect("BasTasi_");
        private List<Transform> FootStones() => Collect("AyakTasi_");

        private List<Transform> Collect(string prefix)
        {
            var list = new List<Transform>();
            foreach (var t in Roots())
                if (t.name.StartsWith(prefix)) list.Add(t);
            return list;
        }

        /// <summary>Menzilin ayak taşı — paylaşılan ayak yerini de bulur.</summary>
        private Vector2 FootOf(string menzilId)
        {
            var go = Find($"AyakTasi_{menzilId}");
            if (go == null && menzilId == "DelikliKaya") go = Find("AyakTasi_Havandelen");
            Assert.IsNotNull(go, $"{menzilId}: ayak tasi yok.");
            return new Vector2(go.position.x, go.position.z);
        }

        private static Vector2 Dir(float azimuthDeg)
        {
            float r = azimuthDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(r), Mathf.Cos(r));
        }

        private static bool Inside(GreeneryBuilder.Area a, Vector3 p)
        {
            bool inside = false;
            var ring = a.ring;
            for (int i = 0; i < ring.Length; i++)
            {
                var u = ring[i];
                var w = ring[(i + 1) % ring.Length];
                if ((u.z > p.z) != (w.z > p.z))
                {
                    float xc = u.x + (p.z - u.z) * (w.x - u.x) / (w.z - u.z);
                    if (p.x < xc) inside = !inside;
                }
            }
            return inside;
        }
    }
}
