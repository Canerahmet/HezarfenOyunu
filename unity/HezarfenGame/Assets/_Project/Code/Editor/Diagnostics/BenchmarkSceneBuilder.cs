using System.Collections.Generic;
using Hezarfen.Core;
using Hezarfen.Diagnostics;
using Hezarfen.Editor.Gis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// HDRP/URP karar kapısı için ölçüm sahnesi kurar (plan Faz 1 kabul:
    /// *"FPS hedefi (ör. 1080p'de 60) graybox'ta tutuyor"*).
    ///
    /// Ölçülen soru şu değil: "boş arazi kaç FPS veriyor?" — o soru kolaydır ve
    /// hiçbir şey söylemez. Ölçülen: **HDRP'nin sabit maliyeti + kademeli şehir
    /// yükü altında kare süresi**, yani Faz 2–4'ün üstüne kuracağı bütçe.
    ///
    /// Yapılar Galata Kulesi çevresine, GERÇEK araziye, yalnızca karaya oturtulur.
    /// Kamera kule tepesindedir ve şehre bakar — Faz 4'ün kabul kriteriyle
    /// (*"kule tepesinden 360° bakışta FPS hedefi tutuyor"*) aynı bakış.
    /// </summary>
    public static class BenchmarkSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Bench_Galata.unity";
        public const string HousePrefab = "Assets/_Project/Art/Prefabs/PF_BoxHouse.prefab";

        /// <summary>Osmanlı kiti sahnesi — ayrı dosya, çünkü ikisi KIYASLANACAK.</summary>
        public const string OttomanScenePath = "Assets/_Project/Scenes/Bench_Galata_Ottoman.unity";
        public const string OttomanPrefab = "Assets/_Project/Art/Prefabs/PF_House_A.prefab";

        /// <summary>Grup başına yapı sayısı. Probe grupları kademeli açar.</summary>
        public const int PerGroup = 1000;
        public const int GroupCount = 8;          // toplam 8000 yapi

        [MenuItem("Hezarfen/Olcum/Benchmark sahnesi kur (graybox)")]
        public static void BuildMenu()
        {
            Build(HousePrefab, ScenePath);
            Debug.Log($"[Hezarfen] Benchmark sahnesi hazir: {ScenePath}\n" +
                      "Play'e bas; FrameTimeProbe adimlari sirayla olcup konsola yazar.");
        }

        /// <summary>
        /// Aynı ölçümü GERÇEK kit eviyle kurar.
        ///
        /// Neden ayrı: graybox ev 44 üçgen ve TEK malzemeydi; kit evi 1 980 üçgen
        /// ve ALTI malzeme. ADR 0010'un ölçümü "içerik ağırlığını kanıtlamaz"
        /// diye kendi içinde uyarıyordu. Trim sheet/atlas kararı (ADR 0014 §8)
        /// bu iki ölçümün FARKINA dayanacak — tahmine değil.
        /// </summary>
        [MenuItem("Hezarfen/Olcum/Benchmark sahnesi kur (Osmanli evi)")]
        public static void BuildOttomanMenu()
        {
            Build(OttomanPrefab, OttomanScenePath);
            Debug.Log($"[Hezarfen] Osmanli benchmark sahnesi hazir: {OttomanScenePath}\n" +
                      "Play'e bas; FrameTimeProbe adimlari sirayla olcup konsola yazar.");
        }

        public static UnityEngine.SceneManagement.Scene Build(
            string housePrefab = HousePrefab, string outScenePath = ScenePath)
        {
            // Faz1 sahnesinden turet: ayni arazi, su, gokyuzu, GIS katmanlari.
            var scene = EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/Faz1_Terrain.unity", OpenSceneMode.Single);

            var terrainGo = GameObject.Find("TR_Istanbul");
            var terrain = terrainGo != null ? terrainGo.GetComponent<Terrain>() : null;
            if (terrain == null) throw new System.Exception("TR_Istanbul yok — once GIS/Terrain uret.");

            // GIS gizmo'lari olcumu etkilemez ama sahneyi agirlastirir; kapatilir.
            var gis = GameObject.Find(GeoJsonImporter.RootName);
            if (gis != null) gis.SetActive(false);

            var old = GameObject.Find("BENCH_Root");
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject("BENCH_Root");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(housePrefab);
            if (prefab == null) throw new System.Exception($"{housePrefab} yok.");

            var groups = new List<GameObject>();
            // Deterministik: ayni seed = ayni sehir. Olcum tekrar edilebilir olmali.
            var rng = new System.Random(1632);

            int placed = 0, rejected = 0;
            for (int g = 0; g < GroupCount; g++)
            {
                var group = new GameObject($"Load_{g:00}");
                group.transform.SetParent(root.transform, false);
                groups.Add(group);

                int made = 0;
                int guard = 0;
                while (made < PerGroup && guard < PerGroup * 40)
                {
                    guard++;
                    // Galata cevresinde, yaricapla artan bir disk
                    double ang = rng.NextDouble() * System.Math.PI * 2.0;
                    double r = System.Math.Sqrt(rng.NextDouble()) * 1800.0;
                    float x = (float)(System.Math.Cos(ang) * r);
                    float z = (float)(System.Math.Sin(ang) * r);

                    float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
                    if (y < 3f) { rejected++; continue; }        // denize ev kurulmaz

                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
                    inst.transform.position = new Vector3(x, y, z);
                    inst.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);
                    made++; placed++;
                }
            }

            // --- yogun sokak kumesi ---
            //
            // Dagitilmis 8000 ev, kule tepesinden bakis icin dogru yuktur ama
            // SOKAK yukunu olcmez: o dagilimda ortalama ev araligi ~28 m ve
            // yaya seviyesinde ekrana pek az ev girer. Oysa malzeme maliyetinin
            // en yuksek oldugu yer tam olarak sokaktir — yakin evler LOD0'dadir
            // ve LOD0 ALTI alt-mesh tasir (LOD2 yalnizca iki).
            //
            // Bu kume 20x20 izgara, 13 m x 20 m adimla: dar yan aralik, genis
            // sokak. Sokaga bakan cepheler birbirine doner (arka sira 180 derece
            // cevrilir), yani kadraja giren sey gercekten CEPHE'dir.
            var dense = new GameObject("Load_08_YogunSokak");
            dense.transform.SetParent(root.transform, false);
            int denseMade = 0;
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    float x = (i - 9.5f) * 13f;
                    float z = (j - 9.5f) * 20f;
                    float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
                    if (y < 3f) continue;
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, dense.transform);
                    inst.transform.position = new Vector3(x, y, z);
                    inst.transform.rotation = Quaternion.Euler(0f, z < 0f ? 0f : 180f, 0f);
                    denseMade++;
                }
            }
            groups.Add(dense);

            // --- CARSI KALABALIGI (Faz 7) ------------------------------
            //
            // Yapi yuku ile KALABALIK yuku ayri seylerdir ve ayri
            // olculmeli: sekiz bin ev CPU'da toplu isleniyor, kalabalik ise
            // her govde icin ayri bir Transform, ayri bir Animator ve ayri
            // bir gölge. Sokak karesi kalabaliksiz olculurse "carsi 60 FPS
            // veriyor" demek, carsinin bos halini olcmek olur.
            //
            // Sayi kaprisli degil: NPCYonetici'nin govde butcesi 60 ve
            // olcum onun USTUNU zorlamali — butceyi karsilayan bir sahne,
            // butcenin yetip yetmedigini soylemez.
            const int KalabalikSayisi = 80;
            var crowd = new GameObject("Load_09_CarsiKalabaligi");
            crowd.transform.SetParent(root.transform, false);
            var kisiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Art/Prefabs/PF_Hezarfen_Sivil.prefab");
            var kalabalikRng = new System.Random(1632);
            int kisiMade = 0;
            for (int i = 0; i < KalabalikSayisi; i++)
            {
                // Sokak koridoru boyunca dagit: kamera tam icine bakar.
                float x = (float)(kalabalikRng.NextDouble() * 120.0 - 60.0);
                float z = (float)(kalabalikRng.NextDouble() * 40.0 - 20.0);
                float y = terrain.SampleHeight(new Vector3(x, 0f, z))
                          + terrain.transform.position.y;
                if (y < 3f) continue;

                GameObject kisi;
                if (kisiPrefab != null)
                    kisi = (GameObject)PrefabUtility.InstantiatePrefab(
                        kisiPrefab, crowd.transform);
                else
                {
                    // Prefab yoksa OLCUM DURMAZ ama neyi olctugunu soyler:
                    // kapsul, gercek karakterin maliyetini TASIMAZ.
                    kisi = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    kisi.transform.SetParent(crowd.transform, false);
                    kisi.transform.localScale = new Vector3(0.5f, 0.85f, 0.5f);
                }
                kisi.transform.position = new Vector3(x, y, z);
                kisi.transform.rotation = Quaternion.Euler(
                    0f, (float)(kalabalikRng.NextDouble() * 360.0), 0f);
                kisiMade++;
            }
            if (kisiPrefab == null)
                Debug.LogWarning("[Hezarfen] PF_Hezarfen_Sivil bulunamadi — kalabalik "
                                 + "KAPSULLERLE kuruldu. Bu adimin sayisi gercek "
                                 + "karakter maliyetini ALTTAN gosterir.");
            groups.Add(crowd);

            // --- olcum kamerasi: kule tepesinden sehre bakis ---
            var camGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>() ?? camGo.AddComponent<Camera>();
            if (camGo.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>() == null)
                camGo.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();

            float galata = terrain.SampleHeight(Vector3.zero) + terrain.transform.position.y;
            camGo.transform.position = new Vector3(0f, galata + 70f, 0f);   // kule tepesi
            camGo.transform.rotation = Quaternion.Euler(12f, 135f, 0f);     // suricine bakis
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 20000f;

            // --- probe ---
            var probeGo = GameObject.Find("BENCH_Probe") ?? new GameObject("BENCH_Probe");
            var probe = probeGo.GetComponent<FrameTimeProbe>() ?? probeGo.AddComponent<FrameTimeProbe>();
            probe.targetCamera = cam;
            probe.loadGroups = groups;
            probe.warmupFrames = 150;
            // 20 saniyelik tam tur icin ornek sayisi: 60 FPS'te 1200 kare.
            // Az orneklemek turun yalnizca bir dilimini olcmek olurdu.
            probe.sampleFrames = 1200;
            var tower = camGo.transform.position;
            var towerRot = camGo.transform.rotation.eulerAngles;
            // Yaya: sokagin ortasinda, 1,65 m'de, sokak boyunca bakis.
            var street = new Vector3(0f, galata + 1.65f, 0f);
            var streetRot = new Vector3(2f, 90f, 0f);

            probe.steps = new List<FrameTimeProbe.Step>
            {
                new FrameTimeProbe.Step { label = "bos arazi + su",       resolution = new Vector2Int(1920, 1080), activeGroups = 0, overrideCamera = true, cameraPosition = tower, cameraEuler = towerRot },
                new FrameTimeProbe.Step { label = "1000 yapi",            resolution = new Vector2Int(1920, 1080), activeGroups = 1 },
                new FrameTimeProbe.Step { label = "3000 yapi",            resolution = new Vector2Int(1920, 1080), activeGroups = 3 },
                new FrameTimeProbe.Step { label = "8000 yapi",            resolution = new Vector2Int(1920, 1080), activeGroups = 8 },
                new FrameTimeProbe.Step { label = "8000 yapi (1440p)",    resolution = new Vector2Int(2560, 1440), activeGroups = 8 },
                new FrameTimeProbe.Step { label = "bos arazi (1440p)",    resolution = new Vector2Int(2560, 1440), activeGroups = 0 },
                // Yaya seviyesi: malzeme maliyetinin en yuksek oldugu kadraj.
                // `activeGroups = 9` HEPSINI acar (8000 dagitilmis + 400 yogun).
                // Dagitilmislar da 1800 m yaricapta oldugu icin sokaktan gorunurler;
                // yani bu, kitin gercekci EN KOTU kadraji. Hemen ardindaki bos
                // adim ayni kadrajin temel maliyetini verir — fark, evlerin payi.
                new FrameTimeProbe.Step { label = "SOKAK: 8000+400 yapi", resolution = new Vector2Int(1920, 1080), activeGroups = 9, overrideCamera = true, cameraPosition = street, cameraEuler = streetRot },
                new FrameTimeProbe.Step { label = "SOKAK: bos (kiyas)",   resolution = new Vector2Int(1920, 1080), activeGroups = 0 },

                // --- FAZ 7: KULE TURU --------------------------------
                //
                // Plan Faz 4'un olcutu "kule tepesinden 360 derece bakista
                // FPS hedefi tutuyor" diyor ve Faz 7 bunu iki cozunurlukte
                // kilitliyor (1080p/60 orta segment, 1440p/60 ust).
                //
                // Sabit bakis YETMEZ: bir yon sehrin en ucuzu olabilir.
                // Kamera ORNEKLENEN PENCERE boyunca tam tur atar ve p95
                // turun en pahali yonunu de icerir.
                new FrameTimeProbe.Step { label = "KULE TURU 360 (1080p)", resolution = new Vector2Int(1920, 1080), activeGroups = 9, overrideCamera = true, cameraPosition = tower, cameraEuler = towerRot, yawSweep360 = true },
                new FrameTimeProbe.Step { label = "KULE TURU 360 (1440p)", resolution = new Vector2Int(2560, 1440), activeGroups = 9, yawSweep360 = true },

                // --- FAZ 7: CARSI KALABALIGI -------------------------
                //
                // Yaya seviyesi + kalabalik: malzeme maliyeti ve govde
                // sayisi ayni anda tepede. Kalabalik `loadGroups`in son
                // grubunda; yoksa adim yine kosar ve rapor farki gosterir.
                new FrameTimeProbe.Step { label = "CARSI KALABALIK (1080p)", resolution = new Vector2Int(1920, 1080), activeGroups = 10, overrideCamera = true, cameraPosition = street, cameraEuler = streetRot },
                new FrameTimeProbe.Step { label = "CARSI KALABALIK (1440p)", resolution = new Vector2Int(2560, 1440), activeGroups = 10 },
            };

            var tag = probeGo.GetComponent<HistoricalTag>() ?? probeGo.AddComponent<HistoricalTag>();
            tag.tier = HistoricalTier.Graybox;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, outScenePath);

            Debug.Log($"[Hezarfen] {System.IO.Path.GetFileNameWithoutExtension(housePrefab)}: " +
                      $"{placed} dagitilmis + {denseMade} yogun sokak yapisi, " +
                      $"{rejected} deneme suya dustugu icin elendi. " +
                      $"Kule kamerasi y={galata + 70f:F0} m, sokak kamerasi y={galata + 1.65f:F1} m.");
            return scene;
        }
    }
}
