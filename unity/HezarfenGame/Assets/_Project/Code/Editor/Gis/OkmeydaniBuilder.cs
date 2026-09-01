using System;
using System.Collections.Generic;
using Hezarfen.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// Okmeydanı sahnesi — <b>oyunun hikâyesinin başladığı yer</b>.
    ///
    /// RESEARCH.md'ye göre burası Hezarfen'in talim yaptığı yerdir: II.
    /// Mehmed'in bağımsız vakıf olarak tesis ettiği, II. Bayezid'in
    /// genişlettiği atış sahası; Okçular (Kemankeş) Tekkesi ve minberli
    /// namazgâh burada; menzil taşları dikili.
    ///
    /// ## Yerleştirmenin kuralı vakfiyeden çıkar
    ///
    /// II. Bayezid'in vakfiyesi meydana <b>yapı, mezar, su yolu, bağ ve
    /// bahçe</b> yapılmasını yasaklar. Buradan iki şey çıkıyor:
    ///
    ///   * <b>Yapılar meydanın KENARINDA durur</b> — tekke de namazgâh da
    ///     alanın içine değil, çeperine konur.
    ///   * <b>Menzil taşları meydanın İÇİNDEDİR</b> ve bu bir çelişki değil:
    ///     taş ne yapıdır, ne mezar, ne su yolu, ne bağ. Meydanın kendi
    ///     donanımıdır — okun düştüğü yeri işaretler.
    ///
    /// ## Menzilin YÖNÜNÜ rüzgâr belirler
    ///
    /// Bu, ADR 0027'de <b>yanlış</b> yazılmıştı: azimutları "kaynakta yok"
    /// diye kuzeybatı yelpazesine dağıtmıştım. Kaynakta var, sadece açıkça
    /// derece olarak değil <b>rüzgâr adıyla</b> yazılı:
    ///
    /// > "Rüzgâr, menzil okçuluğunda atış vaktinin ve <b>atış yönünün</b>
    /// > belirlenmesindeki temel unsurdu. Her menzil için belirlenen bir
    /// > rüzgâr vardı… Böylece hem <b>rüzgâr arkaya alınıp</b> atıcının
    /// > rüzgârdan faydalanması sağlanır…" (Kaya &amp; Şahin 2022, HÜTAD)
    ///
    /// Rüzgâr adları rüzgârın <b>geldiği</b> yönü söyler (kaynak bunu da
    /// doğruluyor: yıldız ile poyraz için "iki rüzgârın da <b>kuzeyden</b>
    /// esmesi"). Rüzgâr arkada olduğuna göre <b>ok ters yöne gider</b>:
    ///
    ///     ok azimutu = rüzgârın geldiği azimut + 180°
    ///
    /// Farklı rüzgârların koridorlarının birbirini kesmesi sakıncalı değil:
    /// atış yalnızca o menzilin rüzgârı estiği gün yapılırdı, yani iki
    /// koridor aynı anda kullanılmazdı.
    /// </summary>
    public static class OkmeydaniBuilder
    {
        public const string ScenePath =
            "Assets/_Project/Scenes/Sandbox/Faz2_Okmeydani.unity";
        public const string TerrainScene = "Assets/_Project/Scenes/Faz1_Terrain.unity";

        /// <summary>Kıble azimutu — sokak yerleştiricisiyle aynı sayı.</summary>
        public const float QiblaAzimuthDeg = OttomanStreetBuilder.QiblaAzimuthDeg;

        /// <summary>
        /// Bir <b>gez</b> kaç metre — ve bu sayı KESİN DEĞİLDİR.
        ///
        /// Kaynaklar 0,60 ile 0,66 m arasında dağılıyor ve dahası birim
        /// yüzyıllar içinde <b>küçülmüştür</b>: aynı fiziksel mesafe 15-16.
        /// yüzyılda 1236 gez, 19. yüzyılda 1279,5 gez sayılır (Kaya &amp;
        /// Şahin 2022). M. Şinasi Acar'ın 19. yüzyıl taşları üzerinde
        /// yaptığı <i>ölçüm</i> 60,74 cm verir; okçuluk literatürü ise 66 cm
        /// kullanır.
        ///
        /// 0,66'yı seçtim çünkü TDV'nin Okmeydanı maddesindeki yayımlanmış
        /// rekor — <b>845,66 m</b> — ancak bu değerle çıkıyor
        /// (1281,5 gez × 0,6599 = 845,66). Çapa kaynağıyla çelişmemek,
        /// ölçüm zincirlerini kendi kafama göre harmanlamaktan iyidir.
        /// Metre değerleri bu yüzden <b>±%10 belirsiz</b>; taşın taşıdığı
        /// sayı GEZ'dir, metre türetilmiştir. Karar değişirse tek sabit
        /// değişir ve her taş birlikte kayar.
        /// </summary>
        public const float GezM = 0.66f;

        /// <summary>Menzil açmanın alt sınırı: <b>900 gez</b>.</summary>
        public const float MinMenzilGez = 900f;

        /// <summary>
        /// 17. yüzyıl kaidesi (Kavâidü'r-Remy): koridor, ana taşın sağ (şast)
        /// ve sol (kabza) yanında <b>40'ar gez</b>. Dışına düşen ok sayılmaz
        /// — "salkı düştü".
        /// </summary>
        public const float CorridorHalfGez = 40f;

        /// <summary>Rüzgârın <b>geldiği</b> azimut. Ok bunun tersine gider.</summary>
        private static float WindFromAzimuth(string wind)
        {
            switch (wind)
            {
                case "yildiz": return 0f;      // kuzey
                case "poyraz": return 45f;     // kuzeydoğu
                case "gundogusu": return 90f;  // doğu
                case "kesisleme": return 135f; // güneydoğu
                case "kible": return 180f;     // güney
                case "lodos": return 225f;     // güneybatı
                case "karayel": return 315f;   // kuzeybatı
                default: throw new Exception($"bilinmeyen hava: {wind}");
            }
        }

        /// <summary>Menzilin ok yönü.</summary>
        public static float ShotAzimuth(string wind) =>
            Mathf.Repeat(WindFromAzimuth(wind) + 180f, 360f);

        /// <summary>Bir menzile dikilmiş taş.</summary>
        public struct Stone
        {
            public string archer;
            public float gez;
            /// <summary>Koridor ekseninden yanal sapma, <b>gez</b>; + şast (sağ).</summary>
            public float sideGez;
            public string prefab;
            public string note;
        }

        /// <summary>
        /// 1632'de Okmeydanı'nda ayakta olan menziller.
        ///
        /// Hepsi <b>belgelidir</b>: adı, havası ve mesafesi kaynakta yazılı.
        /// Hepsi 1632'den ÖNCE açılmıştır — Tozkoparan İskender ve Bursalı
        /// Şüca II. Bayezid devri, Mîrî Âlem Ahmed Ağa 16. yüzyıl.
        ///
        /// <b>Burada olmayan:</b> "IV. Murad'ın ~706 m'lik taşı" ADR 0027'de
        /// vardı; kaldırdım. Sayı akademik olmayan bir kaynaktan geliyordu,
        /// havası bilinmiyor, ve asıl sorun tarih: IV. Murad 1623-1640
        /// hüküm sürdü, yani taşın yarı ihtimalle 1632'den SONRA dikilmiş
        /// olması gerekir. Tarihlendiremediğim bir taşı sahneye koymak,
        /// tekkeye minare koymakla aynı hatadır. Aynı sebeple II. Mahmud
        /// menzili (19. yy) de yok.
        /// </summary>
        public static readonly (string id, string name, string wind, Stone[] stones)[]
            Menzils =
        {
            ("Havandelen", "Havandelen Solak Bali Menzili", "yildiz", new[]
            {
                new Stone { archer = "Bursali Suca", gez = 1251.5f, sideGez = 0f,
                            prefab = "PF_MenzilTasi_Bas",
                            note = "Bursali Suca'nin tasi. Ayni gun Tozkoparan "
                                 + "usule aykiri atisa devam edip bu menzili "
                                 + "bozdu — Delikli Kaya oradan dogdu." },
            }),
            // Delikli Kaya, Havandelen'in ayak yerinden atilmistir; bu yuzden
            // AYNI ayak tasini paylasirlar. Menzilin ayri sayilmasi bir KARAR
            // sonucudur, ayri bir atis yeri sonucu degil.
            ("DelikliKaya", "Tozkoparan (Delikli Kaya) Menzili", "yildiz", new[]
            {
                new Stone { archer = "Tozkoparan Iskender", gez = 1279.5f,
                            sideGez = 80f, prefab = "PF_MenzilTasi_Buyuk",
                            note = "Suca'dan 28 gez asiri. Ok ana tasin 80 gez "
                                 + "SASTINA dustu — 40 gezlik koridorun disi, "
                                 + "yani 'asiri salki'. Suca taraftarlari itiraz "
                                 + "etti, tartisma II. Bayezid'e gitti ve "
                                 + "Seyhulmeydan Hamdullah Efendi'nin karariyla "
                                 + "bu tas AYRI BIR MENZIL sayildi. Tasin "
                                 + "eksenden 80 gez yanda durmasinin sebebi budur." },
            }),
            ("Yildiz", "Yildiz Menzili", "yildiz", new[]
            {
                new Stone { archer = "Miri Alem Ahmed Aga", gez = 1146f, sideGez = 0f,
                            prefab = "PF_MenzilTasi_Bas",
                            note = "Ahmed Aga'nin ILK tasi; yildiz-poyraz "
                                 + "havasiyla atildi. Yildiz meydanda nadir "
                                 + "eser; poyraz da kuzeyden estigi icin bu "
                                 + "menzilde poyrazla atisa izin verilmisti." },
            }),
            ("Arkuri", "Arkuri Menzili", "gundogusu", new[]
            {
                new Stone { archer = "Tozkoparan Iskender", gez = 1281.5f, sideGez = 0f,
                            prefab = "PF_MenzilTasi_Buyuk",
                            note = "MEYDANIN REKORU. TDV 'Okmeydani': 845,66 m "
                                 + "— bu sayi tam olarak 1281,5 gez x 0,66'dir. "
                                 + "Tozkoparan'in en uzun atisi." },
            }),
            ("Lodos", "Lodos Menzili", "lodos", new[]
            {
                new Stone { archer = "Miri Alem Ahmed Aga", gez = 1271f, sideGez = 0f,
                            prefab = "PF_MenzilTasi_Bas",
                            note = "Ahmed Aga bu atisla Lodos Menzili'nin bas "
                                 + "tasinin sahibi oldu." },
            }),
        };

        /// <summary>Ayak yerini paylaşan menziller: çocuk → ata.</summary>
        private static readonly Dictionary<string, string> SharedFoot =
            new Dictionary<string, string> { { "DelikliKaya", "Havandelen" } };

        /// <summary>Koridorların üst üste binmemesi için yanal yuva (m).</summary>
        private static readonly Dictionary<string, float> LateralSlotM =
            new Dictionary<string, float>
            {
                { "Havandelen", 0f }, { "Yildiz", 360f },
                { "Arkuri", 0f }, { "Lodos", 0f },
            };

        /// <summary>
        /// Taşlar koridorun <b>kenarına</b> dizilir, ortasına değil — okun
        /// düştüğü hatta durmazlar. Kaynak: Topyeri Menzili'nde Ali Ağa'nın
        /// oku şast tarafına düşmüştü ama taşı, öteki taşlarla aynı hizada
        /// kalsın diye <b>kabza</b> tarafına dikildi.
        /// </summary>
        private const float KabzaOffsetM = 6f;

        [MenuItem("Hezarfen/GIS/Okmeydani sahnesi kur")]
        public static void BuildMenu()
        {
            Build(1632);
            Debug.Log($"[Hezarfen] Okmeydani sahnesi: {ScenePath}");
        }

        public static Scene Build(int seed)
        {
            var scene = EditorSceneManager.OpenScene(TerrainScene, OpenSceneMode.Single);
            var terrainGo = GameObject.Find("TR_Istanbul");
            var terrain = terrainGo != null ? terrainGo.GetComponent<Terrain>() : null;
            if (terrain == null) throw new Exception("TR_Istanbul yok — once GIS/Terrain uret.");

            var areas = GreeneryBuilder.ReadAreas();
            GreeneryBuilder.Area field = null;
            if (areas != null)
                foreach (var a in areas)
                    if (a.id == "G_Okmeydani_Yasak") { field = a; break; }
            if (field == null)
                throw new Exception("G_Okmeydani_Yasak yok — once greenery_build.py.");

            var root = new GameObject("Okmeydani");
            var center = new Vector2(field.center_x, field.center_z);

            // --- YAPILAR: meydanin DISINDA (vakfiye: meydana yapi yasak).
            //
            // Kenar noktasi yaricaptan HESAPLANAMAZ. Ilk yazimda
            // `radius * 0,86` yaziyordu ve tekke poligonun ICINDE kaldi:
            // `radius_m` cevrel dairenin yaricapidir, poligonun kendisi degil.
            Vector2 edge = OutsideEdge(field, 128f, marginM: 26f);

            // BASKA YERE KONMUS KOPYALAR TEMIZLENIR.
            //
            // `LandmarkPlacer` da ayni iki yapiyi katalog noktasina
            // koyuyordu ve sahnede iki takim duruyordu. Tabloyu
            // duzeltmek yenilerini engeller, var olanlari silmez —
            // ureteç kendi sahasini kendisi toplamali, yoksa duzeltme
            // yalniz temiz bir depoda calisir.
            // TASLAR DA TEMIZLENIR.
            //
            // Ilk halde yalniz tekke ve namazgah temizleniyordu ve bir
            // oyuncu sonucu saydi: **dokuz menzil tasi, yirmi yedi
            // ornek** — her biri birebir ayni koordinatta uc kez.
            // Ureteç uc kez kosmus, her seferinde bir takim daha
            // eklemis. Ayni kusurun bu depodaki dorduncu tekrari
            // (kapinin dort kopyasi, dukkanlarin atlanmasi, tekkenin
            // iki kopyasi, taslarin ucu).
            int temizlenen = Temizle(scene, "PF_Tekke_Okcular")
                             + Temizle(scene, "PF_Namazgah_Okmeydani")
                             + Temizle(scene, "BasTasi_")
                             + Temizle(scene, "AyakTasi_")
                             + Temizle(scene, "PF_MenzilTasi");

            var tekke = Place(root.transform, "PF_Tekke_Okcular", terrain, edge,
                              yaw: 128f + 180f,
                              note: "Okcular (Kemankes) Tekkesi. RESEARCH.md: "
                                  + "tekke mescidinin minaresi 1770-71'de eklendi, "
                                  + "1632'de MINARESIZDIR. Meydanin kenarinda: II. "
                                  + "Bayezid vakfiyesi meydana yapi yapilmasini "
                                  + "yasaklar.");

            // Namazgah tekkenin yaninda ve KIBLEYE doner.
            //
            // Modelde mihrap +Y'dedir (mosque_kit ile ayni sozlesme) ve
            // Unity'ye -Z olarak gelir. Yani nesnenin ARKASI kibleye bakmali.
            Vector2 nzPos = edge + Dir(128f + 90f) * 34f;
            var namazgah = Place(root.transform, "PF_Namazgah_Okmeydani", terrain,
                                 nzPos, yaw: QiblaAzimuthDeg + 180f,
                                 note: "Minberli namazgah. RESEARCH.md: minberi "
                                     + "Gurcu Mehmed Pasa 1624-25'te ekledi — "
                                     + "1632'de YEDI YILLIK. Mihrap kibleye "
                                     + $"({QiblaAzimuthDeg:F1} derece) doner.");

            // --- TEKKE HUCRELERI: burada YASAYAN insanlar -----------
            //
            // ## Neden gerekti
            //
            // Sakinler yalniz `Ev` turundeki graf dugumlerine
            // dagitiliyor (`SehirGunu.Sakinler`). Okmeydani'nda hic
            // `Ev` yoktu, dolayisiyla hic insan yoktu ve oyunun **ilk
            // perdesi** sehrin insan ayagi basmayan tek kosesinde
            // geciyordu. Bir oyuncu on dakika kosup vardi: *"Bir okcu
            // tekkesindeyim, icinde okcu yok."*
            //
            // ## Neden mesken degil hucre
            //
            // `DD_D_Okmeydani.settlementDensity: 0` bilincli ve dogru:
            // II. Bayezid vakfiyesi meydana yapi yapilmasini yasaklar,
            // burasi bir yerlesim degil talim alanidir. O karari
            // bozmuyoruz.
            //
            // Ama bir tekkede seyh ve dervisler **yasar** — tekke bir
            // ibadet yeri degil, bir yasama yeridir. Alti hucre, hepsi
            // tekkenin kendi avlusunda ve meydanin disinda. T2:
            // konumlari makul rekonstruksiyon, varliklari degil.
            int hucre = 0;
            for (int i = 0; i < 6; i++)
            {
                float a = 128f + 180f + (i - 2.5f) * 14f;
                Vector2 hp = edge + Dir(a) * (11f + (i % 2) * 4f);
                if (Place(root.transform, "PF_AvluKapi", terrain, hp,
                          yaw: a + 180f,
                          note: "Tekke hucresi (T2). Bir tekkede seyh ve "
                              + "dervisler yasar; graf sakinleri yalniz "
                              + "Ev dugumlerine dagitir ve Okmeydani'nda "
                              + "hic Ev yoktu.") != null)
                    hucre++;
            }

            // --- MENZILLER: her biri kendi ayak tasi + kendi koridoru.
            var feet = new Dictionary<string, Vector2>();
            var lines = new List<string>();
            int stones = 0;

            foreach (var m in Menzils)
            {
                float bearing = ShotAzimuth(m.wind);
                Vector2 fwd = Dir(bearing);
                Vector2 sast = new Vector2(-fwd.y, fwd.x);   // sag el tarafi

                float longest = 0f;
                foreach (var s in m.stones) longest = Mathf.Max(longest, s.gez * GezM);

                string footKey = SharedFoot.TryGetValue(m.id, out string parent)
                                 ? parent : m.id;
                if (!feet.TryGetValue(footKey, out Vector2 foot))
                {
                    foot = FindCorridor(field, terrain, center, fwd, sast, longest,
                                        LateralSlotM[footKey]);
                    feet[footKey] = foot;

                    var footGo = Place(root.transform, "PF_MenzilTasi_Ayak", terrain,
                        foot, yaw: bearing,
                        note: $"{m.name} — AYAK TASI, atisin yapildigi yer "
                            + "(cay-i kadem). Menzil taslari CIFT dikilir; "
                            + "ayak tasi ile bas tasi arasindaki mesafe "
                            + $"atisin kendisidir. Hava: {m.wind}, yani ok "
                            + $"{bearing:F0} dereceye gider — ruzgar arkada.");
                    if (footGo != null) footGo.name = $"AyakTasi_{footKey}";
                }

                foreach (var s in m.stones)
                {
                    float dist = s.gez * GezM;
                    float side = Mathf.Abs(s.sideGez) > 0.01f
                                 ? s.sideGez * GezM : -KabzaOffsetM;
                    Vector2 mark = foot + fwd * dist + sast * side;

                    var go = Place(root.transform, s.prefab, terrain, mark,
                                   // Kitabe modelde -Y'de, yani Unity'de +Z =
                                   // nesnenin ONU. Kaynak: menzil tasi "mihrabi
                                   // atisin yapildigi yere dogru bakan" tastir.
                                   // O halde on yuz ayak tasina donmeli.
                                   yaw: Mathf.Repeat(bearing + 180f, 360f),
                                   note: $"{m.name} — BAS TASI. {s.archer}, "
                                       + $"{s.gez:0.#} gez ({dist:F1} m; 1 gez = "
                                       + $"{GezM:F2} m kabulu, kaynaklar 0,60-0,66 "
                                       + $"arasi verir). Hava: {m.wind} — ok "
                                       + $"{bearing:F0} dereceye gider. {s.note}");
                    if (go == null) continue;
                    go.name = $"BasTasi_{m.id}_{Mathf.RoundToInt(s.gez)}gez";
                    stones++;

                    Vector2 d = mark - foot;
                    float along = Vector2.Dot(d, fwd);
                    float lat = Vector2.Dot(d, sast);
                    lines.Add($"  {m.name,-34} {m.wind,-9} ok {bearing,3:F0} | "
                              + $"yazili {s.gez,7:0.#} gez | olculen "
                              + $"{along / GezM,7:0.#} gez | yan {lat / GezM,5:0.#} gez");
                }
            }

            // ARAZI SAHNESINE GERI KAYDEDILIR — SANDBOX'A DEGIL.
            //
            // Bu ureteç `Faz1_Terrain`'i aciyor, dokuz menzil tasini,
            // Okcular Tekkesi'ni ve namazgahi oraya diziyor ve sonra
            // sonucu **`Sandbox/Faz2_Okmeydani.unity`ye farkli
            // kaydediyordu**. Icerik dogru yere kuruluyor, sonra oksuz
            // bir dosyaya tasiniyordu; build listesinde `Acilis`,
            // `Faz1_Terrain`, `FlightSlice` var ve sandbox yok.
            //
            // Bedelini bir oyuncu yazdi: oyunun ilk emri "Okmeydani'na
            // git" diyor, 3,5 km yuruyorsun ve bos bir cayira
            // variyorsun. *"Oyunun ilk perdesi sehrin tek bos odasinda
            // geciyor."*
            EditorSceneManager.SaveScene(scene, TerrainScene);

            Debug.Log($"[Hezarfen] Okmeydani: tekke {(tekke != null ? "OK" : "YOK")}, "
                      + $"{hucre} hucre, {temizlenen} kopya silindi, "
                      + $"namazgah {(namazgah != null ? "OK" : "YOK")}, "
                      + $"{Menzils.Length} menzil, {feet.Count} ayak tasi, "
                      + $"{stones} bas tasi.\n" + string.Join("\n", lines));
            _ = seed;
            return scene;
        }

        // ------------------------------------------------------------ yardımcı

        /// <summary>
        /// Koridoru araziye oturt: her iki uç ve arası poligonun içinde ve
        /// <b>karada</b> olsun.
        ///
        /// Ölçtüm: merkeze göre simetrik yerleştirmede Arkurı'nın baş taşı
        /// kot <b>-12 m</b>'ye, yani Haliç'e düşüyordu. Poligon içi olmak
        /// yetmiyor — alanın %8'i su. Koridor bu yüzden eksen boyunca ve
        /// yanal olarak kaydırılarak aranıyor; ölçüt, koridor boyunca
        /// ölçülen EN DÜŞÜK kotu büyütmek, eşitlikte merkeze yakın kalmak.
        /// </summary>
        private static Vector2 FindCorridor(GreeneryBuilder.Area field, Terrain terrain,
                                            Vector2 center, Vector2 fwd, Vector2 sast,
                                            float length, float slotM)
        {
            Vector2 baseFoot = center - fwd * (length * 0.5f) + sast * slotM;
            Vector2 best = baseFoot;
            float bestScore = float.MinValue;

            for (float along = -500f; along <= 500f; along += 20f)
                for (float side = -400f; side <= 400f; side += 40f)
                {
                    Vector2 c = baseFoot + fwd * along + sast * side;
                    float low = float.MaxValue;
                    bool ok = true;
                    for (int i = 0; i <= 20; i++)
                    {
                        Vector2 q = c + fwd * (length * i / 20f);
                        if (!InRing(field.ring, q)) { ok = false; break; }
                        low = Mathf.Min(low, terrain.SampleHeight(new Vector3(q.x, 0f, q.y))
                                             + terrain.transform.position.y);
                    }
                    if (!ok) continue;
                    // Kot metreye yuvarlaniyor ki "biraz daha yuksek" bir nokta
                    // ugruna koridor meydanin ta obur ucuna kacmasin.
                    float score = Mathf.Round(low) * 1000f
                                  - (Mathf.Abs(along) + Mathf.Abs(side));
                    if (score > bestScore) { bestScore = score; best = c; }
                }

            if (bestScore == float.MinValue)
                throw new Exception("Menzil koridoru meydana sigmadi.");
            return best;
        }

        /// <summary>
        /// Merkezden <paramref name="bearing"/> yönünde yürüyerek poligonun
        /// <b>dışına</b> çıkar ve bir pay daha ekler.
        /// </summary>
        private static Vector2 OutsideEdge(GreeneryBuilder.Area field, float bearing,
                                           float marginM)
        {
            var c = new Vector2(field.center_x, field.center_z);
            var d = Dir(bearing);
            float limit = field.radius_m * 1.6f;
            for (float r = 0f; r <= limit; r += 8f)
                if (!InRing(field.ring, c + d * r))
                    return c + d * (r + marginM);
            return c + d * (limit + marginM);
        }

        private static bool InRing(GreeneryBuilder.LocalPoint[] ring, Vector2 p)
        {
            bool inside = false;
            for (int i = 0; i < ring.Length; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Length];
                if ((a.z > p.y) != (b.z > p.y))
                {
                    float xc = a.x + (p.y - a.z) * (b.x - a.x) / (b.z - a.z);
                    if (p.x < xc) inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>Azimut (kuzeyden doğuya, derece) → yatay birim vektör.</summary>
        private static Vector2 Dir(float azimuthDeg)
        {
            float r = azimuthDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(r), Mathf.Cos(r));
        }

        private static GameObject Place(Transform parent, string prefabName,
                                        Terrain terrain, Vector2 xz, float yaw,
                                        string note)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/_Project/Art/Prefabs/{prefabName}.prefab");
            if (prefab == null)
            {
                Debug.LogWarning($"[Hezarfen] {prefabName} yok — atlandi.");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            float size = Footprint(prefab);
            go.transform.position = new Vector3(xz.x, TopOfFootprint(terrain, xz, size),
                                                xz.y);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var tag = go.GetComponent<HistoricalTag>() ?? go.AddComponent<HistoricalTag>();
            tag.tier = HistoricalTier.Reconstruction;
            tag.sourceNote = note;
            return go;
        }

        /// <summary>
        /// Ayak izinin altındaki EN YÜKSEK kot.
        ///
        /// Ortalama değil en yüksek: eğimli zeminde ortalamaya oturan bir
        /// yapının bir köşesi araziye gömülür.
        /// </summary>
        private static float TopOfFootprint(Terrain t, Vector2 c, float size)
        {
            float h = size * 0.5f, hi = float.MinValue;
            for (int i = -1; i <= 1; i += 2)
                for (int j = -1; j <= 1; j += 2)
                    hi = Mathf.Max(hi, t.SampleHeight(new Vector3(c.x + i * h, 0f,
                                                                 c.y + j * h))
                                       + t.transform.position.y);
            return hi;
        }

        private static float Footprint(GameObject prefab)
        {
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
                if (r.gameObject.name.EndsWith("LOD0"))
                    return Mathf.Max(r.bounds.size.x, r.bounds.size.z);
            return 4f;
        }

        /// <summary>
        /// Sahnedeki bu adı taşıyan eski örnekleri siler.
        ///
        /// Bir üreteç ikinci kez koştuğunda aynı sonucu vermeli; "zaten
        /// var" diye atlamak ya da üstüne bir yenisini eklemek, bu
        /// depoda üç ayrı yerde kusur üretti (kule kapısının dört
        /// kopyası, dükkânların hiç güncellenmemesi, ve burası).
        /// </summary>
        private static int Temizle(Scene scene, string ad)
        {
            var silinecek = new List<GameObject>();
            foreach (var kok in scene.GetRootGameObjects())
                foreach (var t in kok.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith(ad))
                        silinecek.Add(t.gameObject);

            foreach (var go in silinecek)
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            return silinecek.Count;
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            string[] parts = assetFolder.Split('/');
            string acc = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{acc}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(acc, parts[i]);
                acc = next;
            }
        }
    }
}
