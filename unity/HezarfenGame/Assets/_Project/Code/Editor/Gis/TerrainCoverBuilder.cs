using System;
using System.Collections.Generic;
using System.IO;
using Hezarfen.Editor.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// Araziye <b>örtü</b> giydirir: dört doku katmanı + eğim/kot kuralına göre
    /// boyanmış splatmap.
    ///
    /// ## Neden bu vardı da yoktu
    ///
    /// Faz 1'de arazi DEM'den doğru ölçekte geldi ama `TerrainLit` malzemesine
    /// **hiç katman atanmadı**. Katmansız bir arazi tek düz renktir. Bu, ADR
    /// 0023'te geçici aydınlatma kurulup yaya seviyesinden kare alınana kadar
    /// görünmedi: karanlıkta düz zeminle dokulu zemin aynı görünüyordu.
    ///
    /// ## Örtünün DAĞILIMI kuraldır, dokunun KENDİSİ yorumdur
    ///
    /// Dağılım üç ölçülebilir şeyden çıkar ve hepsi arazinin kendi verisidir:
    /// <b>kot</b> (deniz seviyesine göre), <b>eğim</b> (heightmap türevi) ve
    /// yumuşatılmış bir <b>gürültü</b> (sınırlar cetvelle çizilmiş gibi
    /// olmasın diye). Doku renkleri ise belgeli değildir — T3 sanatsal yorum.
    ///
    /// ## Mahalle maskesi neden YOK
    ///
    /// "Yerleşim yerinde çiğnenmiş toprak" doğru bir kuraldır ama uygulanacak
    /// veri yok. `districts.geojson` kullanılabilirdi — ve kullanılmadı, çünkü
    /// o dosya kendi içinde şunu yazıyor:
    /// <c>"historical_claim": "none — bu bir OYUN bölgesidir, mahalle sınırı
    /// değildir"</c>. Oyun bölgesini yerleşim sınırı gibi kullanmak, kendi
    /// yazdığımız uyarıyı çiğnemek olurdu. Surlar da yetmez: 1632'de suriçinin
    /// batısı bostanlıktı, baştan başa yapı değil.
    ///
    /// Bunun yerine toprak, <b>düz ve alçak</b> yerde çıkar — insanların
    /// yerleştiği yer de zaten orasıdır. Bağıntı iddia edilmiyor, doğuyor.
    /// </summary>
    public static class TerrainCoverBuilder
    {
        public const string ManifestPath =
            "Assets/_Project/Art/Textures/Terrain/terrain_layers.json";
        public const string LayerFolder = "Assets/_Project/Art/Terrain";

        /// <summary>
        /// Splatmap çözünürlüğü. DEM örnek aralığıyla eşleşsin diye 2048:
        /// heightmap 2049 (= 2^11 + 1) ve arazi 15 337 m, yani örtünün
        /// çözünürlüğü de 7,49 m/texel — kaynağın kendi çözünürlüğü. Daha
        /// yükseği, olmayan bir bilgiyi taklit etmek olurdu.
        /// </summary>
        public const int AlphamapResolution = 2048;

        // Kot eşikleri MUTLAKTIR: deniz seviyesi ve insan yerleşimi, DEM'in
        // çözünürlüğünden bağımsız büyüklükler.
        private const float ShoreFullM = 0.5f;    // bu kotun altı bütünüyle kıyı
        private const float ShoreTopM = 5.0f;     // bu kotun üstünde kıyı yok
        private const float LowlandM = 40f;       // toprak bu kota kadar tam
        private const float UplandM = 120f;       // bu kottan sonra hiç toprak

        // EĞİM eşikleri ise arazinin KENDİ DAĞILIMINDAN türetilir — sabit
        // açı yazılamaz, çünkü ölçülen açı DEM'in çözünürlüğüne bağlıdır.
        //
        // Ölçüldü (Copernicus GLO-30, 7,49 m/örnek): karada eğimin ortancası
        // 5,0°, p95 18,2°, p99 24,3°, en dik nokta 60°. İlk yazımda "kaya 26°
        // üstünde başlar" diyordum ve sonuç **kaya %0,0** oldu: karanın
        // yalnız %0,6'sı 26°'yi aşıyor. Hata jeolojide değil ölçekte —
        // 7,5 m'lik örnek aralığında 15 m tabanla ölçülen eğim, gerçek bir
        // kaya yarını ortalamayla siler. 30 m'lik bir DEM 5 m'lik bir sarplığı
        // gösteremez.
        //
        // Doğru kural açı değil ORAN: "karanın en dik ~%5'i çıplak anakayadır."
        // Açılar bundan çıkar ve DEM değişirse kendiliğinden güncellenir.
        private const float RockStartPct = 0.95f;   // buradan itibaren kaya girer
        private const float RockFullPct = 0.995f;   // burada bütünüyle kaya
        private const float FlatFullPct = 0.40f;    // toprak için "düz" sayılan
        private const float FlatNonePct = 0.75f;    // bunun üstünde toprak yok

        // ------------------------------------------------------------ şema

        [Serializable]
        public class LayerSpec
        {
            public string name;
            public float sizeMeters;
            public string baseColorFile, normalFile, maskFile, note;
        }

        [Serializable]
        public class Manifest
        {
            public string generated_by, license, mask_layout;
            public int resolution;
            public LayerSpec[] layers;
        }

        public static Manifest ReadManifest()
        {
            string abs = Path.Combine(Directory.GetCurrentDirectory(), ManifestPath);
            if (!File.Exists(abs))
            {
                Debug.LogError($"[Hezarfen] Arazi doku bildirimi yok: {ManifestPath}\n" +
                               "Once calistir: python tools/textures/gen_terrain_textures.py");
                return null;
            }
            return JsonUtility.FromJson<Manifest>(File.ReadAllText(abs));
        }

        // ---------------------------------------------------------- menüler

        [MenuItem("Hezarfen/GIS/Arazi ortusunu kur")]
        public static void BuildMenu()
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(
                TerrainImporter.TerrainAssetPath);
            if (data == null)
            {
                Debug.LogError("[Hezarfen] TerrainData yok — once 'DEM'den Terrain uret'.");
                return;
            }

            var layers = EnsureLayers();
            if (layers == null) return;

            data.terrainLayers = layers;
            ConfigureMaterial();

            var meta = TerrainImporter.ReadMeta(TerrainImporter.DefaultDataDir);
            if (meta == null) return;

            Paint(data, (float)meta.base_elevation_m, out string derived);
            ApplyToOpenScenes();

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Hezarfen] Arazi ortusu kuruldu: {layers.Length} katman, " +
                      $"{AlphamapResolution}x{AlphamapResolution} splatmap " +
                      $"({data.size.x / AlphamapResolution:F1} m/texel).\n" +
                      "  " + derived + "\n" + Shares(data));
        }

        [MenuItem("Hezarfen/GIS/Arazi ortusunu olc")]
        public static void MeasureMenu()
        {
            Debug.Log("[Hezarfen] " + Measure(out _));
        }

        /// <summary>
        /// Dört mesafeden inceleme karesi yazar — modellerin
        /// `renders/review/` paketinin arazi karşılığı (ADR 0006).
        ///
        /// Dördü de gerekli çünkü arazi örtüsünün kusurları mesafeye göre
        /// AYRIŞIR: yakında ayrıntı yokluğu, sokakta ölçek yanlışlığı,
        /// yamaçta kuralın yanlış yere düşmesi, havadan ise <b>karo
        /// tekrarı</b> görünür. Tek bir kare hangisini seçerse öbür üçünü
        /// gizler.
        /// </summary>
        [MenuItem("Hezarfen/GIS/Arazi ortusu inceleme paketi")]
        public static void ReviewPackMenu()
        {
            var street = GameObject.Find("Sokak_Ana");
            if (street == null)
            { Debug.LogError("[Hezarfen] Sokak_Ana yok — once mahalle sahnesi ac."); return; }

            Transform house = null;
            foreach (Transform t in street.transform)
                if (t.GetComponent<LODGroup>() != null) { house = t; break; }
            if (house == null) { Debug.LogError("[Hezarfen] Sokakta ev yok."); return; }

            var lines = new List<string>();
            void Shot(string file, Vector3 eye, Vector3 look, float fov)
            {
                var s = FrameMetric.Capture(eye, look, fov,
                                            $"Captures/inceleme_{file}.png", 960, 540);
                lines.Add($"  {file,-10} {s}");
            }

            // 1) Yakin: ciplak arazi, yaya gozu, dik bakis.
            Vector3 bare = FrameMetric.OnGround(house.position + house.right * 120f);
            Shot("yakin", bare + Vector3.up * 1.70f,
                 FrameMetric.OnGround(bare + house.forward * 2.4f), 42f);

            // 2) Sokak: yaya gozu, sokak ekseni boyunca — olcek okumasi.
            Vector3 onStreet = FrameMetric.OnGround(house.position + house.forward * 5f);
            Shot("sokak", onStreet + Vector3.up * 1.70f,
                 FrameMetric.OnGround(onStreet + house.right * 40f) + Vector3.up * 1.2f, 55f);

            // 3) Yamac: 60 m yukaridan, kuralin dogru yere dustugu gorulsun.
            Vector3 hill = FrameMetric.OnGround(house.position) + Vector3.up * 60f;
            Shot("yamac", hill, FrameMetric.OnGround(house.position + house.right * 260f), 50f);

            // 4) Kusbakisi: 400 m — KARO TEKRARININ gorundugu mesafe.
            Vector3 air = FrameMetric.OnGround(house.position) + Vector3.up * 400f;
            Shot("kusbakisi", air,
                 FrameMetric.OnGround(house.position + house.right * 500f), 55f);

            Debug.Log("[Hezarfen] Arazi ortusu inceleme paketi -> Captures/inceleme_*.png\n"
                      + string.Join("\n", lines));
        }

        // ---------------------------------------------------------- katmanlar

        /// <summary>
        /// Bildirimden `.terrainlayer` varlıklarını üretir ve dokuların
        /// **içe aktarma ayarlarını** düzeltir.
        ///
        /// Ayarlar burada olmak zorunda: Unity bir PNG'yi varsayılan olarak
        /// sRGB renk dokusu sayar. Maske sRGB okunursa pürüzlülük ve AO
        /// sessizce yanlış olur (yüzey ne hata verir ne de eksik görünür,
        /// yalnızca yanlış parlar) — kurşun turunda tam bu tür bir kanal
        /// hatası saatler yemişti.
        /// </summary>
        public static TerrainLayer[] EnsureLayers()
        {
            var man = ReadManifest();
            if (man == null || man.layers == null || man.layers.Length == 0) return null;

            EnsureFolder(LayerFolder);
            string texDir = Path.GetDirectoryName(ManifestPath).Replace('\\', '/');
            var result = new List<TerrainLayer>();

            foreach (var spec in man.layers)
            {
                var bc = ImportTexture($"{texDir}/{spec.baseColorFile}", TexKind.Color);
                var nr = ImportTexture($"{texDir}/{spec.normalFile}", TexKind.Normal);
                var mk = ImportTexture($"{texDir}/{spec.maskFile}", TexKind.Data);
                if (bc == null)
                {
                    Debug.LogError($"[Hezarfen] {spec.name}: taban renk dokusu yok.");
                    return null;
                }

                string path = $"{LayerFolder}/TL_Terrain{spec.name}.terrainlayer";
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                bool isNew = layer == null;
                if (isNew) layer = new TerrainLayer();

                layer.name = $"TL_Terrain{spec.name}";
                layer.diffuseTexture = bc;
                layer.normalMapTexture = nr;
                layer.maskMapTexture = mk;
                layer.normalScale = 1f;

                // KARO ÖLÇÜSÜ METREDİR — sözleşmenin ta kendisi (1 birim = 1 m).
                // Bildirimden gelir; burada elle yazılmış bir sayı yok.
                layer.tileSize = new Vector2(spec.sizeMeters, spec.sizeMeters);
                layer.tileOffset = Vector2.zero;

                // Maske haritası varken HDRP bu iki alanı DEĞİL, remap
                // araligini kullanir; kimlik araligi birakiliyor. Alan
                // degerleri maskesiz duruma karsi notr.
                layer.metallic = 0f;
                layer.smoothness = 1f;
                layer.maskMapRemapMin = Vector4.zero;
                layer.maskMapRemapMax = Vector4.one;

                if (isNew) AssetDatabase.CreateAsset(layer, path);
                EditorUtility.SetDirty(layer);
                result.Add(layer);
            }

            // Artik katman varliklarini sil.
            //
            // Katman ADI degistiginde (DryGrass -> Grass) eski `.terrainlayer`
            // dosyasi klasorde kaliyor ve dokulari silinmis oldugu icin KIRIK
            // bir varlik olarak duruyor. Kimse ona bakmadigi surece sessizdir;
            // bir gun biri onu araziye surukler ve arazi mor olur.
            var live = new HashSet<string>();
            foreach (var spec in man.layers) live.Add($"TL_Terrain{spec.name}");
            foreach (string guid in AssetDatabase.FindAssets("t:TerrainLayer",
                                                            new[] { LayerFolder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                string n = Path.GetFileNameWithoutExtension(p);
                // Yalniz KENDI urettiklerimize dokun; `TL_Graybox` baskasinin.
                if (!n.StartsWith("TL_Terrain") || live.Contains(n)) continue;
                AssetDatabase.DeleteAsset(p);
                Debug.Log($"[Hezarfen] Artik arazi katmani silindi: {n}");
            }

            AssetDatabase.SaveAssets();
            return result.ToArray();
        }

        private enum TexKind { Color, Normal, Data }

        private static Texture2D ImportTexture(string assetPath, TexKind kind)
        {
            if (string.IsNullOrEmpty(assetPath) ||
                !File.Exists(Path.Combine(Directory.GetCurrentDirectory(), assetPath)))
                return null;

            AssetDatabase.ImportAsset(assetPath);
            var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null) return null;

            bool dirty = false;
            var wantType = kind == TexKind.Normal
                ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (imp.textureType != wantType) { imp.textureType = wantType; dirty = true; }

            bool wantSrgb = kind == TexKind.Color;
            if (kind != TexKind.Normal && imp.sRGBTexture != wantSrgb)
            { imp.sRGBTexture = wantSrgb; dirty = true; }

            if (imp.wrapMode != TextureWrapMode.Repeat)
            { imp.wrapMode = TextureWrapMode.Repeat; dirty = true; }
            if (!imp.mipmapEnabled) { imp.mipmapEnabled = true; dirty = true; }
            if (imp.filterMode != FilterMode.Trilinear)
            { imp.filterMode = FilterMode.Trilinear; dirty = true; }

            // Anizotropi zemin için lüks değil: yere bakan bir yüzey ufka
            // doğru **çok yatık** açıyla örneklenir ve izotropik mip seçimi
            // uzağı bulanık bir şeride çevirir. 8, zeminde görünür farkın
            // durduğu yer.
            if (imp.anisoLevel != 8) { imp.anisoLevel = 8; dirty = true; }

            if (dirty) imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        /// <summary>Arazi malzemesini yükseklik harmanına ayarlar.</summary>
        public static void ConfigureMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(TerrainImporter.MaterialPath);
            if (mat == null) return;

            // Yükseklik harmanı: iki katmanın sınırında hangi katmanın
            // "daha kabarık" olduğuna bakılır ve o kazanır. Olmadığında geçiş
            // düz bir doğrusal solmadır ve çakıl ile ot birbirinin içinde
            // hayalet gibi görünür. Maskenin B kanalı bunun için üretildi.
            if (mat.HasProperty("_EnableHeightBlend"))
            {
                mat.SetFloat("_EnableHeightBlend", 1f);
                mat.EnableKeyword("_TERRAIN_BLEND_HEIGHT");
            }
            if (mat.HasProperty("_HeightTransition")) mat.SetFloat("_HeightTransition", 0.3f);
            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// Açık sahnelerdeki Terrain bileşenlerine görüntüleme ayarlarını
        /// uygular. Katmanlar TerrainData'da (paylaşılan varlık) yaşar ama
        /// `basemapDistance` bileşenin üstündedir.
        /// </summary>
        public static void ApplyToOpenScenes()
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<Terrain>())
            {
                // Bu mesafenin ötesinde arazi, katmanların önceden pişirilmiş
                // tek bir "basemap"iyle çizilir. Varsayılan 1 000 m bir uçuş
                // oyunu için kısa: 300 m irtifadan bakan oyuncu ufka kadar
                // bulanık bir zemin görür. 2 000 m, ölçülen kare süresini
                // gözle görülür biçimde değiştirmeden ufku toparlıyor.
                t.basemapDistance = 2000f;
                EditorUtility.SetDirty(t);
            }
        }

        // ------------------------------------------------------------ boyama

        /// <summary>
        /// Splatmap'i eğim/kot/gürültüden hesaplar.
        ///
        /// Ağırlıklar <b>öncelik sırasıyla ve kalan bütçeden</b> dağıtılır;
        /// böylece toplam her texel'de tam olarak 1'dir. Ayrı ayrı hesaplayıp
        /// sonda normalize etmek de olurdu ama o zaman "kıyıyı %20 azalttım"
        /// gibi bir değişiklik sessizce kayanın payını da değiştirirdi.
        /// </summary>
        public static void Paint(TerrainData data, float baseElevation) =>
            Paint(data, baseElevation, out _);

        public static void Paint(TerrainData data, float baseElevation,
                                 out string derived) =>
            Paint(data, baseElevation, out derived, null, TerrainOriginXZ());

        /// <summary>
        /// Arazinin güneybatı köşesinin DÜNYA koordinatı (metre).
        ///
        /// Splatmap kuralı arazi-yerel metre ile çalışıyor (gürültü için
        /// yeterli), ama yerleşim maskesi <b>dünya</b> koordinatındadır —
        /// mahalleler oraya konuyor. İkisini karıştırmak maskeyi 3 km
        /// öteye kaydırırdı.
        /// </summary>
        public static Vector2 TerrainOriginXZ()
        {
            var meta = TerrainImporter.ReadMeta(TerrainImporter.DefaultDataDir);
            return meta == null ? Vector2.zero
                : new Vector2((float)meta.world_origin_offset_m.x,
                              (float)meta.world_origin_offset_m.z);
        }

        /// <summary>
        /// Yerleşim maskesi değiştikten sonra <b>yalnız o bölgeyi</b> yeniden
        /// boyar. Mahalle kurulur kurulmaz çağrılır: zemin, üstüne konan
        /// yapılarla aynı turda güncellenmezse biri unutulur.
        /// </summary>
        public static void RepaintSettlement(Rect worldRect)
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(
                TerrainImporter.TerrainAssetPath);
            if (data == null || data.terrainLayers == null
                || data.terrainLayers.Length < 4)
            {
                Debug.LogWarning("[Hezarfen] Arazi ortusu kurulmamis — "
                                 + "yerlesim zemini boyanamadi.");
                return;
            }
            var meta = TerrainImporter.ReadMeta(TerrainImporter.DefaultDataDir);
            if (meta == null) return;

            Paint(data, (float)meta.base_elevation_m, out _, worldRect,
                  TerrainOriginXZ());
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// <paramref name="worldRect"/> verilirse yalnız o dikdörtgen yeniden
        /// boyanır (dünya metresi, XZ).
        ///
        /// Neden var: bir mahalle kurulduğunda yerleşim maskesi değişiyor ve
        /// zeminin ona uyması gerekiyor. Bütün splatmap'i (4,2 milyon texel,
        /// üç oktavlı gürültü) yeniden hesaplamak her mahalle denemesinde
        /// onlarca saniye yerdi. Kural texel başınadır ve komşuya bakmaz —
        /// dolayısıyla bir parçayı boyamak, tamamını boyamakla <b>aynı
        /// sonucu</b> verir. Eşik hesabı yine bütün araziden çıkar; yoksa
        /// küçük bir pencerede "en dik %5" başka bir açıya denk gelirdi.
        /// </summary>
        public static void Paint(TerrainData data, float baseElevation,
                                 out string derived, Rect? worldRect,
                                 Vector2 originXZ)
        {
            int n = data.terrainLayers.Length;
            if (n < 4)
                throw new InvalidOperationException(
                    $"Dort katman bekleniyordu, {n} var — boyama kurallari " +
                    "kiyi/kaya/toprak/ot sirasina gore yazildi.");

            // ÇÖZÜNÜRLÜĞE DOKUNMAK SPLATMAP'İ SIFIRLAR.
            //
            // `alphamapResolution` atamak — aynı değeri atamak bile — Unity'de
            // bütün alphamap'i (1,0,0,0)'a döndürür. Tam boyamada zararsızdı,
            // çünkü hemen ardından her texel yeniden yazılıyordu. Kısmi boyama
            // eklenince sessiz bir felakete dönüştü: bütün arazi toprağa
            // düştü, geri yazılan tek şey 400 m'lik mahalle dikdörtgeni oldu.
            //
            // Gözle yakalanamazdı — kuşbakışı kare "kahverengi bir yamaç"
            // gösteriyordu ve bu makul görünüyor. Yakalayan şey örtü
            // testleriydi: ot %0,02, kaya %0, kıyı %0.
            bool needsResize = data.alphamapResolution != AlphamapResolution;
            if (needsResize && worldRect.HasValue)
            {
                Debug.LogWarning("[Hezarfen] Splatmap cozunurlugu degisecek — "
                                 + "kismi boyama yapilamaz, TAMAMI boyaniyor.");
                worldRect = null;
            }
            if (needsResize) data.alphamapResolution = AlphamapResolution;
            if (data.baseMapResolution != 1024) data.baseMapResolution = 1024;

            int hm = data.heightmapResolution;
            float[,] h = data.GetHeights(0, 0, hm, hm);
            float mps = data.size.x / (hm - 1);
            float yScale = data.size.y;

            var q = SlopeQuantiles(h, hm, mps, yScale, baseElevation,
                                   RockStartPct, RockFullPct,
                                   FlatFullPct, FlatNonePct);
            float rockMin = q[0], rockMax = Mathf.Max(q[1], q[0] + 5f);
            float flatFull = q[2], flatNone = Mathf.Max(q[3], q[2] + 2f);
            derived = $"egim esikleri KARADAN turetildi: kaya {rockMin:F1}-{rockMax:F1}°, "
                    + $"duz {flatFull:F1}-{flatNone:F1}°";

            int res = AlphamapResolution;

            // YERLESIMIN CIGNEDIGI ZEMIN — sinir iddiasi degil, sahneye
            // fiilen konan yapilarin dairelerinden (bkz. SettlementMask).
            var trodden = SettlementMask.Grid.Load();
            if (trodden.Count > 0)
                derived += $"; yerlesim maskesi {trodden.Count} daire";

            // Kismi boyama: texel araligi dunya dikdortgeninden cikar.
            int i0 = 0, j0 = 0, iN = res, jN = res;
            if (worldRect.HasValue)
            {
                // Dunya -> arazi-yerel: guneybati kosesi originXZ'dedir.
                var r = worldRect.Value;
                float lx0 = r.xMin - originXZ.x, lx1 = r.xMax - originXZ.x;
                float lz0 = r.yMin - originXZ.y, lz1 = r.yMax - originXZ.y;
                i0 = Mathf.Clamp(Mathf.FloorToInt(lx0 / data.size.x * res), 0, res - 1);
                iN = Mathf.Clamp(Mathf.CeilToInt(lx1 / data.size.x * res) + 1, 1, res);
                j0 = Mathf.Clamp(Mathf.FloorToInt(lz0 / data.size.z * res), 0, res - 1);
                jN = Mathf.Clamp(Mathf.CeilToInt(lz1 / data.size.z * res) + 1, 1, res);
                if (iN <= i0 || jN <= j0)
                {
                    Debug.LogWarning("[Hezarfen] Yerlesim dikdortgeni arazinin "
                                     + "disinda — zemin boyanmadi.");
                    return;
                }
            }
            int rw = iN - i0, rh = jN - j0;
            var map = new float[rh, rw, n];

            try
            {
                for (int j = j0; j < jN; j++)
                {
                    if ((j & 127) == 0)
                        EditorUtility.DisplayProgressBar("Arazi ortusu",
                            $"Splatmap boyaniyor {j - j0}/{rh}", (j - j0) / (float)rh);

                    for (int i = i0; i < iN; i++)
                    {
                        float u = (i + 0.5f) / res, v = (j + 0.5f) / res;
                        int hx = Mathf.Clamp(Mathf.RoundToInt(u * (hm - 1)), 0, hm - 1);
                        int hy = Mathf.Clamp(Mathf.RoundToInt(v * (hm - 1)), 0, hm - 1);

                        float elev = baseElevation + h[hy, hx] * yScale;

                        int xm = Mathf.Max(hx - 1, 0), xp = Mathf.Min(hx + 1, hm - 1);
                        int ym = Mathf.Max(hy - 1, 0), yp = Mathf.Min(hy + 1, hm - 1);
                        float dzdx = (h[hy, xp] - h[hy, xm]) * yScale / ((xp - xm) * mps);
                        float dzdz = (h[yp, hx] - h[ym, hx]) * yScale / ((yp - ym) * mps);
                        float slope = Mathf.Atan(Mathf.Sqrt(dzdx * dzdx + dzdz * dzdz))
                                      * Mathf.Rad2Deg;

                        float wx = u * data.size.x, wz = v * data.size.z;

                        // Sınırlar gürültüyle kırılır. Düz bir eşik, haritada
                        // eş yükselti eğrisi gibi okunan cetvel çizgileri
                        // bırakır — doğada öyle bir çizgi yoktur.
                        float nRock = Fbm(wx / 140f, wz / 140f, 17) - 0.5f;
                        float nEarth = Fbm(wx / 90f, wz / 90f, 41);
                        float nShore = Fbm(wx / 60f, wz / 60f, 83) - 0.5f;

                        float budget = 1f;

                        float shore = 1f - Smooth(ShoreFullM + nShore * 2.4f,
                                                  ShoreTopM + nShore * 4.0f, elev);
                        shore *= budget;
                        budget -= shore;

                        float rock = Smooth(rockMin, rockMax,
                                            slope + nRock * (rockMax - rockMin) * 0.7f)
                                     * budget;
                        budget -= rock;

                        // SIRT MI ÇUKUR MU — orta ölçekteki değişimin kaynağı.
                        //
                        // Yaya 20 m öteyi, uçan 200 m öteyi görür; ikisinin
                        // arasındaki ölçekte dokunun kendi ayrıntısı çoktan
                        // mip ortalamasına inmiştir. Oraya değişimi ARAZİNİN
                        // KENDİSİ koymalı, yoksa zemin düz bir levha olur.
                        //
                        // Kural gürültü değil jeomorfoloji: dışbükey sırt
                        // toprağını kaybeder (ince, kuru, çıplak), içbükey
                        // çukur toprak ve nem tutar (otlu). Ölçüt, noktanın
                        // ~30 m yarıçaplı komşularının ortalamasından kaç
                        // metre yüksekte olduğu.
                        const int K = 4;                    // 4 örnek ≈ 30 m
                        int lx = Mathf.Max(hx - K, 0), rx2 = Mathf.Min(hx + K, hm - 1);
                        int ly = Mathf.Max(hy - K, 0), ry = Mathf.Min(hy + K, hm - 1);
                        float around = (h[hy, lx] + h[hy, rx2]
                                        + h[ly, hx] + h[ry, hx]) * 0.25f;
                        float relief = (h[hy, hx] - around) * yScale;   // metre
                        float ridge = Mathf.Clamp01(relief / 2.5f);

                        // Toprak: DÜZ ve ALÇAK yerde, lekeli; SIRTTA fazladan.
                        float flat = 1f - Smooth(flatFull, flatNone, slope);
                        float low = 1f - Smooth(LowlandM, UplandM, elev);
                        float earth = Mathf.Clamp01(flat * low * (nEarth * 1.8f - 0.35f)
                                                    + 0.55f * ridge * low)
                                      * budget;

                        // BASILMIŞ ZEMİN kuralın ÜSTÜNE yazar, yerine değil:
                        // kalan bütçeden alır, yani kıyı ve kaya payını
                        // bozmaz. Yerleşim suya ya da yara kayaya kurulmaz;
                        // kurulduğu yerde ise ot kalmaz.
                        float s = trodden.At(wx + originXZ.x, wz + originXZ.y);
                        if (s > 0f) earth = Mathf.Max(earth, s * budget);

                        float grass = budget - earth;

                        map[j - j0, i - i0, 0] = earth;
                        map[j - j0, i - i0, 1] = grass;
                        map[j - j0, i - i0, 2] = rock;
                        map[j - j0, i - i0, 3] = shore;
                    }
                }

                data.SetAlphamaps(i0, j0, map);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        /// <summary>
        /// Eğim dağılımının verilen yüzdeliklerini <b>yalnızca KARADAN</b>
        /// hesaplar (derece).
        ///
        /// Deniz tabanı dışarıda bırakılmak zorunda: Boğaz'ın kenarı DEM'de
        /// arazinin en dik yeridir (60°'ye kadar) ve karışıma girerse
        /// yüzdelikleri yukarı çeker — kara yamacı hiç kaya göremezdi. Örtü
        /// zaten orayı kıyı katmanına veriyor.
        /// </summary>
        public static float[] SlopeQuantiles(float[,] h, int hm, float mps,
                                             float yScale, float baseElevation,
                                             params float[] quantiles)
        {
            var vals = new List<float>(hm * hm / 16);
            for (int y = 1; y < hm - 1; y += 4)
                for (int x = 1; x < hm - 1; x += 4)
                {
                    if (baseElevation + h[y, x] * yScale < 0.5f) continue;   // deniz
                    float dx = (h[y, x + 1] - h[y, x - 1]) * yScale / (2f * mps);
                    float dz = (h[y + 1, x] - h[y - 1, x]) * yScale / (2f * mps);
                    vals.Add(Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg);
                }
            vals.Sort();

            var outp = new float[quantiles.Length];
            for (int i = 0; i < quantiles.Length; i++)
                outp[i] = vals.Count == 0 ? 0f
                    : vals[Mathf.Clamp(Mathf.RoundToInt(quantiles[i] * (vals.Count - 1)),
                                       0, vals.Count - 1)];
            return outp;
        }

        private static float Smooth(float a, float b, float x)
        {
            if (b - a < 1e-4f) return x < a ? 0f : 1f;
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        // Değer gürültüsü: tohumlu, tekrarlanabilir, kütüphanesiz.
        // `Mathf.PerlinNoise` kullanılmadı çünkü Unity sürümleri arasında
        // birebir aynı kalacağının garantisi yok ve bu boyama TESTLE
        // kilitleniyor — testin dayandığı sayı sürümle değişmemeli.
        private static float Hash(int x, int y, int seed)
        {
            uint hgh = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
            hgh = (hgh ^ (hgh >> 13)) * 1274126177u;
            return ((hgh ^ (hgh >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
        }

        private static float Value(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Mathf.Lerp(Hash(xi, yi, seed), Hash(xi + 1, yi, seed), fx);
            float b = Mathf.Lerp(Hash(xi, yi + 1, seed), Hash(xi + 1, yi + 1, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        /// <summary>
        /// Projenin tek değer gürültüsü — 0..1, tohumlu, sürümden bağımsız.
        ///
        /// `GreeneryBuilder` de bunu kullanır. İkinci bir gürültü yazmak,
        /// iki ayrı desenin aynı araziye binmesi ve hiçbirinin ötekiyle
        /// hizalanmaması demekti; ağacın seyrekleştiği yerle toprağın çıktığı
        /// yer ilgisiz olurdu.
        /// </summary>
        public static float Noise01(float x, float y, int seed) => Fbm(x, y, seed);

        private static float Fbm(float x, float y, int seed) =>
            Value(x, y, seed) * 0.65f + Value(x * 2.7f, y * 2.7f, seed + 1) * 0.35f;

        // ------------------------------------------------------------- ölçüm

        /// <summary>Katmanların kapladığı alan yüzdeleri.</summary>
        public static string Shares(TerrainData data)
        {
            int res = data.alphamapResolution;
            int n = data.terrainLayers.Length;
            float[,,] a = data.GetAlphamaps(0, 0, res, res);

            var sum = new double[n];
            int step = Mathf.Max(1, res / 512);
            int count = 0;
            for (int j = 0; j < res; j += step)
                for (int i = 0; i < res; i += step)
                {
                    for (int k = 0; k < n; k++) sum[k] += a[j, i, k];
                    count++;
                }

            var parts = new List<string>();
            for (int k = 0; k < n; k++)
                parts.Add($"{data.terrainLayers[k].name.Replace("TL_Terrain", "")} " +
                          $"%{sum[k] * 100.0 / count:F1}");
            return "  Ortu paylari: " + string.Join(", ", parts);
        }

        /// <summary>
        /// Yaya seviyesinden <b>zemine</b> bakan bir kare alır.
        ///
        /// ## Kadraj zeminle DOLMAK zorunda — bir kez dolmadı
        ///
        /// İlk yazımda göz 1,70 m'de, bakış noktası 11 m ileride bir yer
        /// noktasıydı. O geometride bakış açısı yalnızca <b>9° aşağı</b>dır
        /// ve kare mimariyle dolar. Sonuç: örtüsüz arazi 1,95, örtülü arazi
        /// 2,00 — yani ölçü, değiştirdiğim şeyi neredeyse hiç görmüyordu.
        /// Aynı hata aydınlatma turunda iki kez yapılmıştı; ölçü aletinin
        /// neye baktığı, ölçtüğü şey kadar önemli.
        ///
        /// Şimdi iki şey ayrı ayrı sağlanıyor:
        ///   * bakış <b>dik</b> (yaklaşık 35° aşağı), kare 1,5–6 m arası
        ///     zeminle dolar;
        ///   * nokta <b>çıplak arazide</b>, sokaktan uzakta — kaldırım da ev
        ///     de kadraja girmesin. Kaldırım ayrı bir yüzeydir ve bu ölçüm
        ///     onu değil <b>araziyi</b> sınıyor.
        /// </summary>
        public static string Measure(out float detail)
        {
            detail = 0f;
            var street = GameObject.Find("Sokak_Ana");
            if (street == null) return "Olcum YAPILAMADI: Sokak_Ana yok.";

            Transform house = null;
            foreach (Transform t in street.transform)
                if (t.GetComponent<LODGroup>() != null) { house = t; break; }
            if (house == null) return "Olcum YAPILAMADI: sokakta ev yok.";

            // Sokak ekseni boyunca 120 m: mahallenin dışında, çıplak yamaç.
            Vector3 spot = FrameMetric.OnGround(house.position + house.right * 120f);
            Vector3 eye = spot + Vector3.up * 1.70f;
            Vector3 look = FrameMetric.OnGround(spot + house.forward * 2.4f);

            var st = FrameMetric.Capture(eye, look, 42f, "Captures/olcum_zemin.png");
            detail = st.Detail;
            return $"Ciplak arazi (yaya gozu, ~35 derece asagi): {st}";
        }

        // ------------------------------------------------------------ yardımcı

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
