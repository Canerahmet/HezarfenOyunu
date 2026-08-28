using System;
using System.Collections.Generic;
using System.IO;
using Hezarfen.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// Landmark <b>prefablarını</b> dünyaya yerleştirir (Faz 3).
    ///
    /// ## Gizmo değil YAPI
    ///
    /// <see cref="GeoJsonImporter"/> landmark noktalarını sahneye zaten
    /// alıyordu — ama gizmo olarak: nereye geleceğini gösteren işaretler.
    /// Bu sınıf üretilmiş olanın kendisini koyar. İkisi ayrı kalmalı, çünkü
    /// katalogda **22 landmark** var ve şu an **biri** üretildi; gizmo
    /// ötekilerin yerini göstermeye devam ediyor.
    ///
    /// ## Konum kataloğdan, kot ARAZİDEN, yön EĞİMDEN
    ///
    /// Konum <c>landmarks_1632_local.json</c>'dan gelir (GIS hattının
    /// ürettiği yerel metre; Galata Kulesi tam <b>(0, 0)</b> — dünya
    /// orijininin tanımı, ADR 0007).
    ///
    /// Kot, ayak izinin **en yüksek** köşesinden alınır — mahalle
    /// yerleştiricisinin 8. kuralının aynısı. Ortalamaya oturtmak yapıyı yarı
    /// gömer, en alçağa oturtmak havada bırakır.
    ///
    /// Yön <b>eğimden türer</b>, elle yazılmaz: kulenin kapısı yokuş aşağı,
    /// yani şehre ve limana bakar. Bir kaynak "kapı şu yöndedir" demiyor;
    /// ama "kule kapısı kasabaya bakar" bir çıkarım olarak yazılabilir ve
    /// arazinin kendi eğimi onu ölçülebilir kılar. Sabit bir açı yazmak,
    /// arazi değiştiğinde sessizce yanlışa dönerdi.
    /// </summary>
    public static class LandmarkPlacer
    {
        public const string LocalJsonPath =
            "data/gis/istanbul/landmarks_1632_local.json";
        public const string PrefabDir = "Assets/_Project/Art/Prefabs";
        public const string WorldScene = "Assets/_Project/Scenes/Faz1_Terrain.unity";
        public const string RootName = "LANDMARK_1632";

        /// <summary>
        /// Üretilmiş landmark'lar: katalog kimliği → prefab.
        ///
        /// Liste **kısa ve açık** olmalı: burada olmayan bir landmark
        /// yerleştirilmez ve bu bir hata değil, henüz üretilmediğinin
        /// kaydıdır. Faz 3 ilerledikçe satır eklenir.
        /// </summary>
        public static readonly Dictionary<string, string> Built =
            new Dictionary<string, string>
            {
                { "LM_GalataKulesi", "PF_GalataKulesi" },
                { "LM_KizKulesi", "PF_KizKulesi" },
                { "LM_UskudarMihrimah", "PF_UskudarMihrimah" },
                { "LM_DogancilarCamii", "PF_DogancilarCamii" },
                { "LM_HudayiTekkesi", "PF_HudayiTekkesi" },
                { "LM_HudayiTurbesi", "PF_HudayiTurbesi" },
                { "LM_MihrimahMedrese", "PF_MihrimahMedrese" },
                { "LM_MihrimahMektebi", "PF_MihrimahMektebi" },
                // AHSAP varyant (Caner, 2026-08-27 — ADR 0039). TDV kubbe
                // der, Sedad Hakki Eldem ahsap; Eldem Osmanli SIVIL
                // mimarisi uzmanidir ve kosk sivil bir yapidir. Secim
                // yapiyi kendi ailesine de yaklastiriyor: Alay Kosku ve
                // Kiz Kulesi de 1632'de ahsaptir. Kubbeli varyant depoda
                // KALIR — celiski cozulmedi, yalnizca bir tarafi secildi.
                { "LM_IncliKosk", "PF_IncliKosk_Ahsap" },
                { "LM_TopkapiAdaletKulesi", "PF_TopkapiAdaletKulesi" },
                { "LM_TopkapiBabusselam", "PF_TopkapiBabusselam" },
                { "LM_OkcularTekkesi", "PF_Tekke_Okcular" },
                { "LM_OkmeydaniNamazgah", "PF_Namazgah_Okmeydani" },
                { "LM_YeniCamiHarabe", "PF_YeniCamiHarabe" },
                { "LM_Suleymaniye", "PF_Suleymaniye" },
                // Ayasofya bir KILISEDIR ve kibleye donuk degildir; yonu
                // katalogdan `face_deg` ile gelir (303,5 derece) ve
                // kiblenin onune gecer. Bkz. ADR 0045.
                { "LM_Ayasofya", "PF_Ayasofya" },
                // Sultanahmet'in ekseni OLCULDU (133,6) ve sehrin 1632
                // kiblesiyle (133,7) ayni cikti — bu yapi ADR 0046'nin
                // cikis noktasidir, o yuzden face_deg bildirmez: kural
                // zaten onun olcusunden dogdu.
                { "LM_Sultanahmet", "PF_Sultanahmet" },
                // Fatih Camii'nin 1632 hali BUGUNKU YAPI DEGILDIR: bir
                // yarim kubbe (mihrap yonunde), iki ayak, yanlarda ucer
                // kucuk kubbe, birer serefeli iki minare. Bugunku barok
                // sema 1767-71'dir. Bkz. ADR 0048.
                { "LM_FatihCamii", "PF_FatihCamii" },
                // Yedikule surun ICINDEDIR ve Altin Kapi DISA bakar. Yon
                // elle yazilmadi: sur hattinin oradaki DIS NORMALI olculdu
                // (261,2 derece). Bkz. ADR 0050.
                { "LM_Yedikule", "PF_Yedikule" },
                { "LM_Beyazit", "PF_Beyazit" },
                // Bedestenler cami DEGILDIR: kibleye donmezler. Kind
                // "bedesten" ve yerlestirici onlari egime gore dondurur.
                { "LM_CevahirBedesteni", "PF_CevahirBedesteni" },
                { "LM_SandalBedesteni", "PF_SandalBedesteni" },
                // Turbeler kibleye DONMEZ: mezar kiblesi olculur ama yapinin
                // KAPISI hazirenin duzenine gore acilir. Kind "turbe_selatin"
                // ve yerlestirici onlari egime gore dondurur. ADR 0054.
                { "LM_TurbeSelimII", "PF_TurbeSelimII" },
                { "LM_TurbeMuradIII", "PF_TurbeMuradIII" },
                { "LM_TurbeMehmedIII", "PF_TurbeMehmedIII" },
                { "LM_TurbeSultanAhmed", "PF_TurbeSultanAhmed" },
                // Iskele `ShoreKinds` uyesidir: SUYA doner ve arazi 0,5 m'nin
                // altindaysa su duzlemine oturur (Kiz Kulesi kurali).
                // ARAP CAMII — Galata'nin CUMA camisi (ADR 0071).
                //
                // Uretilmesinin sebebi estetik degil olcum: Cuma namazi
                // mescitte kilinmaz ve Galata'nin camisi yoktu, yani
                // oyuncunun fiilen dolastigi semtte Cuma HICBIR SEY
                // yapmiyordu. Uretilen ama gorunmeyen bir oge, olmayan bir
                // ogedir.
                //
                // Yapi bir KILISEDIR ve kibleye donuk DEGILDIR (San
                // Domenico, ~1323-37; 1475'ten beri cami). Ayasofya
                // kurali burada da isler: yon katalogdan `face_deg` ile
                // gelir ve kiblenin onune gecer (ADR 0045).
                { "LM_ArapCamii", "PF_ArapCamii" },
                { "LM_UskudarIskele", "PF_UskudarIskelesi" },
                { "LM_AlayKosku", "PF_AlayKosku" },
            };

        // JSON AYRISTIRICISI YENIDEN YAZILMADI.
        //
        // Ilk yazimda burada kendi `[Serializable]` kaplarim vardi ve Unity
        // haklı olarak uyardı: `JsonUtility` **ic ice generic listeleri**
        // (`List<List<T>>`) cozemez. Sonuc sessiz degildi ama inceydi —
        // `rings` bos geldi ve kule "konum yok" diye atlandi.
        //
        // `GeoJsonImporter.ParseLocal` bu sorunu zaten biliyor ve el yazimi
        // bir ayristiriciyla cozuyor. Ikinci bir nusha yazmak, ayni tuzagin
        // ikinci bir kopyasini da yazmak olurdu.

        [MenuItem("Hezarfen/GIS/Landmark'lari sahneye yerlestir")]
        public static void PlaceMenu()
        {
            var scene = EditorSceneManager.OpenScene(WorldScene, OpenSceneMode.Single);
            int n = Place(out string report);
            if (n <= 0) { Debug.LogError("[Hezarfen] " + report); return; }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Hezarfen] {n} landmark yerlestirildi -> {WorldScene}\n{report}");
        }

        public static int Place(out string report)
        {
            report = "";
            var terrainGo = GameObject.Find("TR_Istanbul");
            var terrain = terrainGo != null ? terrainGo.GetComponent<Terrain>() : null;
            if (terrain == null)
            { report = "TR_Istanbul yok — once GIS/Terrain uret."; return -1; }

            string root = TerrainImporter.RepositoryRoot();
            string path = root == null ? null : Path.Combine(root,
                LocalJsonPath.Replace('/', Path.DirectorySeparatorChar));
            if (path == null || !File.Exists(path))
            { report = $"{LocalJsonPath} yok — once tools/gis/landmarks_build.py"; return -1; }

            var doc = GeoJsonImporter.ParseLocal(File.ReadAllText(path));
            if (doc?.features == null || doc.features.Count == 0)
            { report = "Landmark dosyasi bos."; return -1; }

            var old = GameObject.Find(RootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var host = new GameObject(RootName);

            var lines = new List<string>();
            int placed = 0, skipped = 0;
            foreach (var f in doc.features)
            {
                if (f.layer != "landmark") continue;
                if (!Built.TryGetValue(f.id, out string prefabName)) { skipped++; continue; }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{PrefabDir}/{prefabName}.prefab");
                if (prefab == null)
                {
                    lines.Add($"  {f.id}: {prefabName} YOK — atlandi.");
                    continue;
                }
                if (f.rings == null || f.rings.Count == 0 || f.rings[0].Count == 0)
                {
                    lines.Add($"  {f.id}: konum yok — atlandi.");
                    continue;
                }

                var p = f.rings[0][0];
                var c = new Vector2(p.x, p.z);
                float radius = FootprintRadius(prefab);

                // Kot: ayak izinin EN YUKSEK kosesi (mahalle 8. kurali).
                float lo, hi;
                Corners(terrain, c, radius, out lo, out hi);

                // DENIZDEKI LANDMARK: ada DEM'de olmayabilir.
                //
                // Kiz Kulesi'nde olculdu: cevresi bastan basa -12 m, yani
                // Copernicus GLO-30 kayaligi hic gormuyor. Arazi kotuna
                // oturtmak kuleyi deniz TABANINA gomerdi. Boyle bir yerde
                // dogru kot SU DUZLEMIDIR (y=0) ve varligin kendi kayaligi
                // su cizgisini kesip yukari cikar.
                float y = hi;
                if (hi < 0.5f)
                {
                    y = 0f;
                    lines.Add($"  {f.id}: arazi {hi:F1} m (DEM'de ada YOK) — "
                              + "su duzlemine (y=0) oturtuldu; kayalik "
                              + "varligin kendi parcasi.");
                }

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, host.transform);
                inst.transform.position = new Vector3(c.x, y, c.y);

                // YON: caminin yonunu egim DEGIL KIBLE belirler.
                //
                // Ilk yazimda Uskudar Mihrimah da yokus asagi dondurulmustu
                // ve 322 derece cikmisti — dogruya 8,4 derece uzak ve daha
                // kotusu, YANLIS GEREKCEYLE. Bir caminin mihrabi arazinin
                // egimine bakmaz.
                string kind = KindOf(prefabName);
                bool mosque = MosqueKinds.Contains(kind);
                bool shore = ShoreKinds.Contains(kind);

                // YON SIRASI: once varligin KENDI BILDIRDIGI yon.
                //
                // Kapinin, kulenin ya da kosk pencerelesinin baktigi taraf
                // bazen BELGELIDIR ve arazi egiminden turetilemez.
                // Babusselam birinci avludan ikinciye acilir, yani guneye
                // bakar; egim onu batiya donduruyordu. Bu alan olmadan tek
                // cozum yerlestiriciye yapiya ozel bir istisna yazmakti —
                // yani kurali bozmakti.
                // `face_deg: 0` BILDIRIM SAYILMAZ.
                //
                // Alay Kosku'nun kaydinda `face_deg=0.0` vardi ve "yon
                // bildirilmedi" demek istiyordum; yerlestirici onu "KUZEYE
                // BAK" diye okudu ve kosku 0 dereceye cevirdi. Sifir hem
                // "yok" hem "kuzey" anlamina gelemez.
                //
                // Sozlesme: kuzeye bakan bir yapi **360** yazar. Sifir ya
                // da negatif = bildirilmemis.
                Vector3 face;
                float? declared = FaceDegOf(prefabName);
                if (declared.HasValue && declared.Value > 0.01f)
                    face = Bearing(declared.Value);
                else if (mosque) face = Bearing(QiblaEntranceDeg);
                else if (shore) face = Waterward(terrain, c);
                else if (hi < 0.5f) face = Seaward(terrain, c);
                else face = Downhill(terrain, c);
                inst.transform.rotation = Quaternion.LookRotation(face,
                                                                  Vector3.up);
                placed++;
                lines.Add($"  {f.id} -> {prefabName} @ ({c.x:F1}, {y:F1}, {c.y:F1}), "
                          + $"ayak izi altinda kot farki {hi - lo:F2} m, "
                          + $"yon {inst.transform.eulerAngles.y:F1} derece "
                          + (mosque ? "(KIBLE)" : "(egim)"));
            }

            var tag = host.AddComponent<HistoricalTag>();
            tag.tier = HistoricalTier.Documented;
            tag.sourceNote = "Faz 3 landmark'lari. Konum landmarks_1632.geojson "
                           + "(T1/T2), kot araziden, yon egimden turedi. Her "
                           + "yapinin kendi kaynagi ve dogruluk basamagi kendi "
                           + "prefabindadir.";

            report = $"{placed} yerlestirildi, {skipped} henuz uretilmedi.\n"
                   + string.Join("\n", lines);
            return placed;
        }

        /// <summary>
        /// <b>Bugünün kıblesi</b> — büyük daire, ızgara kuzeyine göre.
        ///
        /// Kâbe 21,4225 K / 39,8262 D: gerçek kuzeye göre 151,73°, UTM 35N
        /// meridyen yakınsaması (λ−27°)·sin φ = 1,32° çıkarılınca
        /// <b>150,40°</b>. Şehir ölçeğinde sabittir: kataloğun 22
        /// landmark'ında yayılım yalnızca <b>0,198°</b>.
        ///
        /// <b>Ama 1632'nin camileri buraya bakmıyor.</b> Bu sabit artık
        /// yalnızca karşılaştırma içindir; yerleştiren sayı
        /// <see cref="QiblaDeg"/>'dir.
        /// </summary>
        public const float ModernQiblaDeg = 150.40f;

        /// <summary>
        /// <b>1632'nin kıblesi</b> — ızgara kuzeyine göre, <b>ölçüldü</b>.
        ///
        /// ## Neden büyük daire değil
        ///
        /// Sultanahmet'in ekseni yedi bağımsız ölçüyle <b>133,6°</b> çıktı
        /// (harim kütlesi, iki avlu kütlesi, kemer duvarı, dört uzun
        /// minarenin dikdörtgeni, kısa minare çiftinin doğrultusu, avlu→harim
        /// hattı). Hesaplanan kıble orada 150,32°. Arada <b>16,7°</b> var ve
        /// bu bir ölçüm gürültüsü değil.
        ///
        /// Tek yapı olsa şüphelenirdim. Ondan sonra <b>on</b> tarihî cami
        /// ölçüldü (Bizans kilisesinden çevrilenler hariç tutuldu — onların
        /// ekseni kilisenindir):
        ///
        /// <code>
        ///   Piyale Paşa (1573)              -19,9
        ///   Ayazma (1758-61)                -19,0
        ///   Hadım İbrahim Paşa (1551)       -18,5
        ///   Azapkapı Sokollu (1577-78)      -17,2
        ///   Sultanahmet (1616)              -16,7
        ///   Zal Mahmut Paşa (1577)          -16,6
        ///   Bali Paşa (1504)                -14,4
        ///   Neslişah Sultan (16. yy)        -12,2
        ///   Atik Ali Paşa (1496)            -11,7
        ///   Süleymaniye (1557)              -11,3
        ///                          medyan   -16,6
        /// </code>
        ///
        /// Onu da hep <b>aynı yöne</b>: Osmanlı camisi büyük daireden
        /// <b>doğuya</b> sapar. Sebep belgelidir — dönemin koordinat
        /// cetvelleri ve manyetik pusula (bkz. ADR 0046); mekanizmayı
        /// uydurmuyorum, sapmayı ölçüyorum.
        ///
        /// ## Yöntem doğrulandı
        ///
        /// Aynı ölçüm <b>Şakirin Camii</b>'nde (2009, "çok hassas")
        /// <b>+0,04°</b> veriyor. Yani yöntem sapmayı uydurmuyor; modern
        /// camide sıfır, tarihî camide −17 buluyor. 1 197 İstanbul camisinin
        /// tamamında medyan −1,7° — çünkü örneklem mahalle camileriyle
        /// dolu ve onlar bugünün kıblesine göre yapıldı.
        ///
        /// ## Sayı
        ///
        /// Gerçek kuzeye göre <b>135,0°</b> (tam güneydoğu — bu bir
        /// gözlemdir, iddia değil); UTM yakınsaması çıkınca ızgarada
        /// <b>133,7°</b>.
        ///
        /// Ölçülen ekseni olan yapı bunu kullanmaz: kendi
        /// <c>face_deg</c>'ini bildirir ve o kazanır.
        /// </summary>
        public const float QiblaDeg = 133.70f;

        /// <summary>
        /// Giriş cephesinin baktığı yön: kıblenin tam tersi.
        ///
        /// Prefabın <c>+Z</c>'si giriş cephesidir (Blender'da <c>−Y</c>);
        /// mihrap duvarı arkada kalır.
        /// </summary>
        public const float QiblaEntranceDeg = QiblaDeg + 180f;

        /// <summary>Kıbleye göre yönlendirilen yapı türleri.</summary>
        private static readonly HashSet<string> MosqueKinds =
            new HashSet<string>
            {
                "selatin", "mescit", "cami",
                // Medresenin DERSHANESI mihrapli bir mekandir ve
                // kulliyede camiyle hizali durur; egime gore
                // dondurmek onu kulliyeden kopartirdi.
                "medrese",
                // NAMAZGAH bir ibadet yeridir: mihrabi ve (Okmeydani'nda)
                // minberi vardir. Egime gore dondurmek, acik hava
                // namazgahini kiblesiz birakir — yani namazgah olmaktan
                // cikarir. TEKKE de kendi mescidiyle hizalidir.
                "namazgah", "tekke",
                // HARABE de kibleye doner: Yeni Cami'nin MIHRAP DUVARI
                // 1597-1603 arasinda orulmustu. Bitmemis olmasi, yonunun
                // bilinmedigi anlamina gelmez — plan kibleye gore kurulur,
                // duvarlar ondan sonra yukselir.
                "harabe",
            };

        /// <summary>
        /// Kıyıdaki yapıların yönü: <b>denize doğru</b>.
        ///
        /// İncili Köşk için eğim de kıble de yanlış olurdu. Köşk Bizans
        /// deniz suruna oturur, cumbası denize taşar ve padişah kıyıdaki
        /// töreni oradan seyreder — yani yapı suya bakar. Ölçü: çevredeki
        /// <b>en alçak</b> arazi yönü.
        /// </summary>
        private static readonly HashSet<string> ShoreKinds =
            new HashSet<string> { "kosk", "iskele" };

        private static Vector3 Waterward(Terrain t, Vector2 c)
        {
            Vector3 best = Vector3.forward;
            float lowest = 1e9f;
            foreach (float radius in Rings)
                for (int i = 0; i < 32; i++)
                {
                    float a = Mathf.PI * 2f * i / 32f;
                    var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    var q = new Vector3(c.x, 0f, c.y) + d * radius;
                    float h = t.SampleHeight(q) + t.transform.position.y;
                    if (h < lowest) { lowest = h; best = d; }
                }
            return best;
        }

        private static Vector3 Bearing(float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
        }

        /// <summary>
        /// Prefabın türü — Blender kataloğundan (<c>kind</c>).
        ///
        /// Tür listesini burada elle tutmak, üreticiyle yerleştiricinin iki
        /// ayrı gerçeği olması demekti. Tek kaynak üreticinin bildirdiğidir.
        /// </summary>
        private static string KindOf(string prefabName)
        {
            if (_kinds == null)
            {
                _kinds = new Dictionary<string, string>();
                _faces = new Dictionary<string, float>();
                // BUTUN kataloglar taranir, yalniz landmark'inki degil.
                //
                // Okmeydani'nin namazgahi ve tekkesi Faz 2'de uretildi ve
                // kendi katalogundadir (`art/blend/okmeydani/`). Yalnizca
                // landmark katalogunu okumak, o varliklari yerlestirilebilir
                // olmaktan cikariyordu — turleri ve bildirilen yonleri
                // gorunmuyordu.
                string root = TerrainImporter.RepositoryRoot();
                foreach (var cat in CatalogPaths(root))
                    foreach (var line in File.ReadAllText(cat)
                                             .Split('{'))
                    {
                        string n = Field(line, "prefab"), k = Field(line, "kind");
                        if (n != null && k != null) _kinds[n] = k;
                        string fd = Field(line, "face_deg", quoted: false);
                        if (n != null && fd != null
                            && float.TryParse(fd,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out float fv))
                            _faces[n] = fv;
                    }
            }
            return _kinds.TryGetValue(prefabName, out string v) ? v : "";
        }

        /// <summary>`art/blend/*/catalog.json` — var olanlar.</summary>
        // PUBLIC, internal degil: test assembly'si (Hezarfen.Tests.EditMode)
        // ayri bir assembly'dir ve Hezarfen.Editor'un internal uyelerini
        // goremez. Bir kez CS0117 ile ogrenildi — hata mesaji "internal"
        // demez, "boyle bir tanim yok" der ve yaniltir.
        public static IEnumerable<string> CatalogPaths(string root)
        {
            if (root == null) yield break;
            string dir = Path.Combine(root, "art", "blend");
            if (!Directory.Exists(dir)) yield break;
            foreach (var sub in Directory.GetDirectories(dir))
            {
                string p = Path.Combine(sub, "catalog.json");
                if (File.Exists(p)) yield return p;
            }
        }

        private static Dictionary<string, string> _kinds;

        /// <summary>
        /// Varlığın kataloğda bildirdiği <b>belgeli yön</b> (derece), varsa.
        /// </summary>
        private static float? FaceDegOf(string prefabName)
        {
            KindOf(prefabName);                      // katalogu yukler
            if (_faces != null && _faces.TryGetValue(prefabName, out float v))
                return v;
            return null;
        }

        private static Dictionary<string, float> _faces;

        private static string Field(string chunk, string key,
                                    bool quoted = true)
        {
            int i = chunk.IndexOf($"\"{key}\"");
            if (i < 0) return null;
            int colon = chunk.IndexOf(':', i);
            if (colon < 0) return null;
            if (quoted)
            {
                int a = chunk.IndexOf('"', colon + 1);
                if (a < 0) return null;
                int b = chunk.IndexOf('"', a + 1);
                return b < 0 ? null : chunk.Substring(a + 1, b - a - 1);
            }
            // Sayisal alan: tirnaksiz, virgul ya da satir sonuna kadar.
            int e = colon + 1;
            // JSON'da sayisal degeri ',' ya da '}' bitirir; satir sonu
            // karakterini aramaya gerek yok ve C# char literaline gercek
            // bir satir sonu yazmak derlemeyi kirar (bir kez kirdi).
            while (e < chunk.Length && chunk[e] != ',' && chunk[e] != '}')
                e++;
            return chunk.Substring(colon + 1, e - colon - 1).Trim();
        }

        /// <summary>LOD0'ın yatay yarıçapı (m).</summary>
        private static float FootprintRadius(GameObject prefab)
        {
            var rs = prefab.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return 8f;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return Mathf.Max(b.extents.x, b.extents.z);
        }

        private static void Corners(Terrain t, Vector2 c, float r,
                                    out float lo, out float hi)
        {
            lo = float.MaxValue; hi = float.MinValue;
            for (int i = -1; i <= 1; i += 2)
                for (int j = -1; j <= 1; j += 2)
                {
                    var p = new Vector3(c.x + i * r, 0f, c.y + j * r);
                    float h = t.SampleHeight(p) + t.transform.position.y;
                    lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                }
        }

        /// <summary>
        /// Denizdeki bir yapının baktığı yön: <b>karaya doğru</b>.
        ///
        /// Eğimden türetilemez — çevresi baştan başa deniz tabanı. Kız
        /// Kulesi Salacak kıyısından ~100 m açıktadır ve kıyı doğusundadır;
        /// yön dünya orijinine (Galata) değil, en yakın KARAYA bakar.
        /// </summary>
        private static Vector3 Seaward(Terrain t, Vector2 c)
        {
            // UC HALKA, tek halka degil. Tek 200 m'lik halkada Kiz Kulesi'nin
            // cevresindeki en yuksek ornek -7,2 m cikiyordu: yon KARAYA degil
            // deniz tabaninin en sig noktasina gore seciliyordu. Dogru cikti
            // (kiyi zaten o yonde) ama gerekce gurultuydu. 800 m'de kara
            // +43,8 m'ye ciktigi icin karar artik tartisilmaz.
            Vector3 best = Vector3.forward;
            float bestH = -1e9f;
            foreach (float radius in Rings)
                for (int i = 0; i < 16; i++)
                {
                    float a = Mathf.PI * 2f * i / 16f;
                    var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    var q = new Vector3(c.x, 0f, c.y) + d * radius;
                    float h = t.SampleHeight(q) + t.transform.position.y;
                    if (h > bestH) { bestH = h; best = d; }
                }

            if (bestH < 0.5f)
                Debug.LogWarning("[Hezarfen] Denizdeki landmark'in 800 m "
                                 + "cevresinde KARA yok — yon guvenilir degil.");
            return best;
        }

        /// <summary>Karayi ararken taranan yariçaplar (m).</summary>
        private static readonly float[] Rings = { 200f, 400f, 800f };

        /// <summary>
        /// Yokuş aşağı yön (yatay birim vektör) — kapının baktığı taraf.
        ///
        /// Arazinin eğimi 40 m tabanla ölçülür: 7,49 m'lik DEM örneğinde daha
        /// dar bir taban gürültüyü yön sanardı. Düzlükte eğim sıfıra iner ve
        /// yön anlamsızlaşır; o durumda kuzey verilir ve bu <b>söylenir</b>.
        /// </summary>
        private static Vector3 Downhill(Terrain t, Vector2 c, float h = 40f)
        {
            float H(float x, float z) =>
                t.SampleHeight(new Vector3(x, 0f, z)) + t.transform.position.y;
            float dx = H(c.x + h, c.y) - H(c.x - h, c.y);
            float dz = H(c.x, c.y + h) - H(c.x, c.y - h);
            var g = new Vector3(dx, 0f, dz);
            if (g.sqrMagnitude < 1e-4f)
            {
                Debug.LogWarning("[Hezarfen] Landmark duz arazide — kapi yonu "
                                 + "egimden turetilemedi, kuzeye bakiyor.");
                return Vector3.forward;
            }
            return -g.normalized;                 // egimin TERSI = yokus asagi
        }
    }
}
