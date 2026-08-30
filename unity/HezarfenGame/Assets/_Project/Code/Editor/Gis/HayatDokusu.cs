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
    /// <b>Mahallenin içini doldurur — evlerin arasını, avluyu, arkayı.</b>
    ///
    /// ## Ölçülen boşluk
    ///
    /// <see cref="KirsalDoku"/> şehir <b>dışını</b> doldurdu (yollar,
    /// bostan, meyvelik) ve bunu binalara <b>45 m'den fazla</b> yaklaşmayan
    /// bir dışlama ile yaptı. Doğruydu: tarla evin dibinde başlamaz. Ama
    /// sonucu ölçülünce şu çıktı — bir mahallenin 200 m'lik karesinde
    /// zeminin <b>%90,3'ü çıplak arazi</b>, <b>%81,7'sinin 4 m yakınında
    /// hiçbir şey yok</b>, ve 240 m'lik karede <b>sıfır</b> ağaç.
    ///
    /// Yani mahalle, sokağa dizilmiş bir ev şeridi ve etrafında boş bir
    /// tarlaydı. Caner bunu üç turda üç kez söyledi ve her seferinde şehir
    /// dışına bakıp "doldurduk" dedim.
    ///
    /// ## Kural: yakınlık, uzaklık değil
    ///
    /// Bu araç <see cref="KirsalDoku"/>'nun <b>tersini</b> yapar: bir nokta
    /// ancak bir binaya <see cref="EnCokUzaklik"/> metreden YAKINSA doldurulur
    /// — çünkü doldurulan şey kır değil, <b>evin eklentisi</b>. Osmanlı konutu
    /// avlulu ve hayatlıdır (RESEARCH.md 4.1); avluda kuyu, su küpü, odunluk,
    /// asma çardağı bulunur. Konan şey dekor değil, konutun kendi parçası.
    ///
    /// ## Ne konmaz
    ///
    /// Kaldırımın, kaidenin, binanın, mezarlığın üstüne konmaz; sokağın
    /// ortasına konmaz (geçiş kapanır); eğimi dik yere konmaz. Bunların
    /// hepsi <b>ölçülerek</b> elenir, ad tahminiyle değil.
    /// </summary>
    public static class HayatDokusu
    {
        private const string TerrainScene =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";
        private const string DistrictDir =
            "Assets/_Project/Scenes/Districts";
        private const string PrefabDir = "Assets/_Project/Art/Prefabs";

        /// <summary>Kök nesnenin adı — denetimler ve temizlik bunu arar.</summary>
        public const string KokAd = "HAYAT_1632";

        /// <summary>
        /// Bir binaya bundan uzak nokta avlu değil, kırdır (m).
        ///
        /// İlk yazımda 26 m'ydi ve <b>gözle bakınca yanlış olduğu
        /// görüldü</b>: eşyalar evin arkasındaki açık düzlüğe dağılıyor,
        /// çölde duran sandıklar gibi okunuyordu. Boşluğu doldurmak
        /// yerine <b>görünür kılıyordu</b> — çünkü tek başına duran bir
        /// nesne etrafındaki boşluğa işaret eder.
        ///
        /// 9 m: bir avlunun evden uzaklığı. Bu mesafede eşya duvara ait
        /// görünür; ötesi tarladır ve orayı dolduran şey başkadır
        /// (<see cref="KirsalDoku"/>).
        /// </summary>
        public const float EnCokUzaklik = 9f;

        /// <summary>Binaya bundan yakın nokta duvarın içidir (m).</summary>
        public const float EnAzUzaklik = 2.2f;

        /// <summary>Örnekleme adımı (m). Yoğunluğu bu belirler.</summary>
        public const float Izgara = 8f;

        /// <summary>Bu eğimin üstüne eşya konmaz (derece).</summary>
        public const float EnCokEgim = 22f;

        /// <summary>İki eşya arası en az bu kadar (m).</summary>
        public const float EnAzAralik = 3.4f;

        //: (prefab, ağırlık) — hangi eşya ne sıklıkta.
        private static readonly (string ad, int agirlik)[] Esyalar =
        {
            ("PF_Odunluk_A", 5), ("PF_Odunluk_B", 4),
            ("PF_SuKupu_A", 6),  ("PF_SuKupu_B", 5),
            ("PF_Sepet_A", 5),
            ("PF_Cardak_A", 3),                 // yer kaplar, seyrek
            ("PF_Kuyu_A", 2),                   // her avluda kuyu olmaz
            ("PF_Cit_A", 4),
        };

        [MenuItem("Hezarfen/GIS/Hayat dokusunu kur")]
        public static void Kur()
        {
            var sahne = EditorSceneManager.OpenScene(
                TerrainScene, OpenSceneMode.Single);
            var arazi = UnityEngine.Object.FindAnyObjectByType<Terrain>();
            if (arazi == null) { Debug.LogError("[Hezarfen] Arazi yok."); return; }

            var prefablar = new List<(GameObject go, int agirlik)>();
            foreach (var (ad, agirlik) in Esyalar)
            {
                var g = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{PrefabDir}/{ad}.prefab");
                if (g == null)
                {
                    Debug.LogError($"[Hezarfen] {ad} yok — once "
                                   + "gen_hayat.py ve boru hatti.");
                    return;
                }
                prefablar.Add((g, agirlik));
            }
            int toplamAgirlik = 0;
            foreach (var (_, a) in prefablar) toplamAgirlik += a;

            // --- BINA KUTULARI: semt sahnelerinden ---
            var kutular = new List<Rect>();
            var acilan = new List<UnityEngine.SceneManagement.Scene>();
            foreach (string sy in Directory.GetFiles(DistrictDir, "*.unity"))
            {
                try
                {
                    acilan.Add(EditorSceneManager.OpenScene(
                        sy.Replace("\\", "/"), OpenSceneMode.Additive));
                }
                catch { }
            }
            foreach (var sc in acilan)
            {
                if (!sc.IsValid()) continue;
                foreach (var kok in sc.GetRootGameObjects())
                    foreach (var mr in kok.GetComponentsInChildren<MeshRenderer>(false))
                    {
                        var b = mr.bounds;
                        // Birlesik yuzeyler (kaldirim, kaide) bina degil.
                        if (b.size.x > 120f || b.size.z > 120f) continue;
                        if (b.size.y < 2.0f) continue;
                        kutular.Add(new Rect(b.min.x, b.min.z, b.size.x, b.size.z));
                    }
            }

            // Izgara: her noktayi her binayla denemek milyonlarca deneme.
            const float H = 32f;
            var izgara = new Dictionary<(int, int), List<int>>();
            for (int i = 0; i < kutular.Count; i++)
            {
                var r = kutular[i];
                int x0 = Mathf.FloorToInt((r.xMin - EnCokUzaklik) / H);
                int x1 = Mathf.FloorToInt((r.xMax + EnCokUzaklik) / H);
                int z0 = Mathf.FloorToInt((r.yMin - EnCokUzaklik) / H);
                int z1 = Mathf.FloorToInt((r.yMax + EnCokUzaklik) / H);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        if (!izgara.TryGetValue((x, z), out var l))
                        { l = new List<int>(); izgara[(x, z)] = l; }
                        l.Add(i);
                    }
            }
            foreach (var sc in acilan)
                if (sc.IsValid()) EditorSceneManager.CloseScene(sc, true);

            if (kutular.Count == 0)
            {
                Debug.LogWarning("[Hezarfen] Bina kutusu yok — once semtleri doldur.");
                return;
            }

            // --- ESKI DOKUYU KALDIR ---
            var eski = GameObject.Find(KokAd);
            if (eski != null) UnityEngine.Object.DestroyImmediate(eski);
            var kokGo = new GameObject(KokAd);
            var etiket = kokGo.AddComponent<HistoricalTag>();
            // T2 — MAKUL REKONSTRUKSIYON, "efsane" DEGIL.
            //
            // Blender katalogunda bu varliklar T3 diye gecer ama oradaki
            // olcek baska: kaynak yoklugunu anlatiyor. Unity'deki
            // `HistoricalTier.Legend` ise "tek kaynakli efsane" demek
            // (Evliya'nin ucus anlatisi, Lagari'nin roketi) — bir su
            // kupu o degil. Mahalle dokusu neyse bu da o: kurallarla
            // uretilmis makul rekonstruksiyon.
            etiket.tier = HistoricalTier.Reconstruction;
            etiket.sourceNote =
                "Mahalle hayati donatisi: kuyu, su kupu, odunluk, cardak, "
                + "sepet, cit. Kaynakta olcu YOK; olculer insan olceginden "
                + "turetildi (bkz. tools/blender/lib/hayat_kit.py). Osmanli "
                + "konutunun avlulu/hayatli oldugu RESEARCH.md 4.1'de "
                + "kayitli; buradaki iddia avlunun BOS OLMADIGIdir.";

            // --- YERLESTIR ---
            var data = arazi.terrainData;
            var kokPos = arazi.transform.position;
            float genis = data.size.x, boy = data.size.z;
            var konanlar = new List<Vector2>();
            var konanIzgara = new Dictionary<(int, int), List<int>>();
            int konan = 0, egimElendi = 0, uzakElendi = 0, yakinElendi = 0,
                cakismaElendi = 0, yuzeyElendi = 0;

            var rng = new System.Random(1632);

            for (float wx = kokPos.x; wx < kokPos.x + genis; wx += Izgara)
            for (float wz = kokPos.z; wz < kokPos.z + boy; wz += Izgara)
            {
                // Izgarayi tohumla sars: dizilmis esya "izgara" gibi okunur.
                float jx = wx + ((float)rng.NextDouble() - 0.5f) * Izgara * 0.8f;
                float jz = wz + ((float)rng.NextDouble() - 0.5f) * Izgara * 0.8f;
                var p2 = new Vector2(jx, jz);

                // 1) BINAYA YAKIN MI (avlu mu, kir mi)
                float enYakin = float.MaxValue;
                if (izgara.TryGetValue((Mathf.FloorToInt(jx / H),
                                        Mathf.FloorToInt(jz / H)), out var yakinlar))
                    foreach (int i in yakinlar)
                    {
                        float d = Mesafe(kutular[i], p2);
                        if (d < enYakin) enYakin = d;
                    }
                if (enYakin > EnCokUzaklik) { uzakElendi++; continue; }
                if (enYakin < EnAzUzaklik) { yakinElendi++; continue; }

                // 2) EGIM
                float u = (jx - kokPos.x) / genis, v = (jz - kokPos.z) / boy;
                if (u < 0f || u > 1f || v < 0f || v > 1f) continue;
                if (data.GetSteepness(u, v) > EnCokEgim) { egimElendi++; continue; }

                // 3) ARALIK
                if (CokYakin(konanIzgara, konanlar, p2)) { cakismaElendi++; continue; }

                // 4) YUZEY: kaldirim/kaide/bina ustune konmaz.
                float zemin = arazi.SampleHeight(new Vector3(jx, 0f, jz)) + kokPos.y;
                if (Physics.Raycast(new Vector3(jx, zemin + 8f, jz), Vector3.down,
                                    out var vurus, 20f, ~0,
                                    QueryTriggerInteraction.Ignore))
                {
                    // Arazi disinda bir seye carptiysa orasi dolu.
                    if (!(vurus.collider is TerrainCollider))
                    { yuzeyElendi++; continue; }
                    zemin = vurus.point.y;
                }

                // 5) SEC ve KOY
                int pay = rng.Next(toplamAgirlik);
                GameObject secilen = prefablar[0].go;
                foreach (var (g, a) in prefablar)
                {
                    if (pay < a) { secilen = g; break; }
                    pay -= a;
                }

                var ornek = (GameObject)PrefabUtility.InstantiatePrefab(
                    secilen, kokGo.transform);
                ornek.transform.position = new Vector3(jx, zemin, jz);
                ornek.transform.rotation = Quaternion.Euler(
                    0f, (float)rng.NextDouble() * 360f, 0f);

                konanlar.Add(p2);
                Ekle(konanIzgara, konanlar.Count - 1, p2);
                konan++;
            }

            // ESYA KOMSUSUYLA BIRLIKTE AKAR.
            //
            // Ilk yazimda hepsi ana sahneye yaziliyordu ve ana sahne HEP
            // YUKLUDUR: 24.177 nesne, sahne 1 MB'dan 66 MB'a cikti. Yani
            // Uskudar'in icinde otururken Eyup'un odunlugu da bellekte
            // duruyordu. Binalar zaten semt sahnelerinde akiyor; avlunun
            // esyasi da oyle akmali.
            int dagitilan = SemtlereDagit(kokGo);
            UnityEngine.Object.DestroyImmediate(kokGo);

            EditorSceneManager.MarkSceneDirty(sahne);
            EditorSceneManager.SaveScene(sahne, TerrainScene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Hezarfen] Hayat dokusu: {konan} esya kondu, "
                      + $"{dagitilan} tanesi semtlere dagitildi. "
                      + $"Elenen — kir (binadan uzak) {uzakElendi}, "
                      + $"duvar dibi {yakinElendi}, egim {egimElendi}, "
                      + $"dolu yuzey {yuzeyElendi}, aralik {cakismaElendi}. "
                      + $"{kutular.Count} bina kutusu tarandi.");
        }

        /// <summary>
        /// Eşyaları ait oldukları semt sahnesine taşır.
        ///
        /// Semt sınırı dışında kalan (kıyı payı, semtler arası boşluk)
        /// <b>atılmaz</b>: en yakın semte gider. Sessizce kaybolan
        /// örneklem bu projede dört kez kör nokta üretti.
        /// </summary>
        private static int SemtlereDagit(GameObject kokGo)
        {
            var kayit = AssetDatabase.LoadAssetAtPath<Streaming.DistrictRegistry>(
                "Assets/_Project/Data/DistrictDefs/DistrictRegistry.asset");
            if (kayit == null || kayit.districts.Length == 0)
            {
                Debug.LogError("[Hezarfen] Semt kaydi yok — esya dagitilamadi.");
                return 0;
            }
            var karalar = new List<Streaming.DistrictDef>();
            foreach (var d in kayit.districts)
                if (d != null && d.kind == Streaming.DistrictKind.Land) karalar.Add(d);
            if (karalar.Count == 0) return 0;

            var gruplar = new Dictionary<string, GameObject>();
            var cocuklar = new List<Transform>();
            foreach (Transform t in kokGo.transform) cocuklar.Add(t);

            foreach (var t in cocuklar)
            {
                Streaming.DistrictDef sahip = null;
                foreach (var d in karalar)
                    if (d.Contains(t.position)) { sahip = d; break; }
                if (sahip == null)
                {
                    float en = float.MaxValue;
                    foreach (var d in karalar)
                    {
                        float m = Vector2.Distance(
                            d.center, new Vector2(t.position.x, t.position.z));
                        if (m < en) { en = m; sahip = d; }
                    }
                }
                if (sahip == null) continue;

                if (!gruplar.TryGetValue(sahip.districtId, out var g))
                {
                    g = new GameObject(KokAd);
                    var e = g.AddComponent<HistoricalTag>();
                    var kaynak = kokGo.GetComponent<HistoricalTag>();
                    if (kaynak != null)
                    { e.tier = kaynak.tier; e.sourceNote = kaynak.sourceNote; }
                    gruplar[sahip.districtId] = g;
                }
                t.SetParent(g.transform, true);
            }

            int toplam = 0;
            foreach (var kv in gruplar)
            {
                string yol = $"{DistrictDir}/{kv.Key}.unity";
                if (!File.Exists(yol)) continue;
                var sc = EditorSceneManager.OpenScene(yol, OpenSceneMode.Additive);
                // Iki kez calistirmak sayiyi ikiye katlamamali.
                foreach (var r in sc.GetRootGameObjects())
                    if (r.name == KokAd) UnityEngine.Object.DestroyImmediate(r);
                int n = kv.Value.transform.childCount;
                EditorSceneManager.MoveGameObjectToScene(kv.Value, sc);
                EditorSceneManager.MarkSceneDirty(sc);
                EditorSceneManager.SaveScene(sc, yol);
                EditorSceneManager.CloseScene(sc, true);
                toplam += n;
                Debug.Log($"[Hezarfen] {n} hayat esyasi -> {kv.Key}");
            }
            return toplam;
        }

        /// <summary>Noktanın dikdörtgene uzaklığı (içindeyse 0).</summary>
        private static float Mesafe(Rect r, Vector2 p)
        {
            float dx = Mathf.Max(r.xMin - p.x, 0f, p.x - r.xMax);
            float dz = Mathf.Max(r.yMin - p.y, 0f, p.y - r.yMax);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private const float KonanHucre = 8f;

        private static void Ekle(Dictionary<(int, int), List<int>> izgara,
                                 int i, Vector2 p)
        {
            var a = (Mathf.FloorToInt(p.x / KonanHucre),
                     Mathf.FloorToInt(p.y / KonanHucre));
            if (!izgara.TryGetValue(a, out var l))
            { l = new List<int>(); izgara[a] = l; }
            l.Add(i);
        }

        private static bool CokYakin(Dictionary<(int, int), List<int>> izgara,
                                     List<Vector2> konanlar, Vector2 p)
        {
            int x = Mathf.FloorToInt(p.x / KonanHucre);
            int z = Mathf.FloorToInt(p.y / KonanHucre);
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (!izgara.TryGetValue((x + dx, z + dz), out var l)) continue;
                    foreach (int i in l)
                        if ((konanlar[i] - p).sqrMagnitude < EnAzAralik * EnAzAralik)
                            return true;
                }
            return false;
        }
    }
}
