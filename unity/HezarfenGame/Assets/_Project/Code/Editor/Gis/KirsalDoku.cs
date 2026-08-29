using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hezarfen.Core;
using Hezarfen.Streaming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// <b>Sur ile mahalle arasındaki boşluğu doldurur</b> (ADR 0074, A).
    ///
    /// Caner (2026-08-29, oynarken): *"acik dunya zemini gercekci degil ve
    /// cok fazla bos duruyor mesela surdan mahalleye kadar bosluk cim var
    /// onu yerine araba yollari agaclar vs olabilir."*
    ///
    /// ## O boşluk tarihte de boştu — ama çayır değildi
    ///
    /// 17. yüzyılda kara surlarının iç tarafındaki batı üçtebir bugünkü
    /// anlamda "şehir" değildi; bostan (sulu sebze bahçesi), bağ, meyve
    /// bahçesi ve büyük servili mezarlıklarla kapılara giden yollardı.
    /// Yani Caner'in istediği "dolu dünya" ile tarih burada <b>çatışmıyor</b>;
    /// çatışan tek şey kesinlik derecesi.
    ///
    /// ## İddianın sınırı
    ///
    /// Konumu kaynaktan gelen <b>tekil</b> yapı yok. Konan şey dönemin
    /// <b>yapı türleri</b>: yol, bostan parseli, meyvelik, servilik. Hepsi
    /// <c>HistoricalTier.Reconstruction</c> (T2). İddia şudur: *"burada bu
    /// türden bir doku vardı"* — *"burada tam olarak bu vardı"* değil.
    ///
    /// ## Yol güzergâhı çizilmez, bulunur
    ///
    /// Kapıdan şehre giden yol, iki uç arasında <b>en az eğime tırmanan</b>
    /// yoldur (Dijkstra). Araba yolu yamaç aşmaz, yamacı dolaşır; maliyet
    /// bu yüzden mesafe değil <b>eğim</b> üzerinden hesaplanıyor.
    ///
    /// Yüzey ADR 0074'ün kararına uyar: suriçi ana ekseni taş, kapı yolları
    /// sıkışmış toprak.
    /// </summary>
    public static class KirsalDoku
    {
        private const string TerrainScene =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";
        private const string PrefabDir = "Assets/_Project/Art/Prefabs";
        public const string KokAdi = "KIRSAL_1632";

        /// <summary>Araba yolu genişliği (m) — iki araba yan yana.</summary>
        public const float YolGenisligi = 6.0f;

        /// <summary>Analiz ızgarası — <see cref="DereAgi.Izgara"/> ile aynı.</summary>
        public const int Izgara = 512;

        [MenuItem("Hezarfen/GIS/Kirsal dokuyu kur")]
        public static void Kur()
        {
            var sahne = EditorSceneManager.OpenScene(
                TerrainScene, OpenSceneMode.Single);
            var arazi = Object.FindAnyObjectByType<Terrain>();
            if (arazi == null)
            {
                Debug.LogError("[Hezarfen] TR_Istanbul yok.");
                return;
            }

            var eski = GameObject.Find(KokAdi);
            if (eski != null) Object.DestroyImmediate(eski);
            var kok = new GameObject(KokAdi);

            var kapilar = Kapilar();
            var sehir = SehirNoktalari();
            if (kapilar.Count == 0 || sehir.Count == 0)
            {
                Debug.LogError($"[Hezarfen] Kapi {kapilar.Count}, sehir "
                               + $"noktasi {sehir.Count} — once surlari ve "
                               + "landmark'lari kur.");
                return;
            }

            // YESIL DOKUYU ONCE SIFIRLA — bu komut IDEMPOTENT olmali.
            //
            // Meyvelik agaclari araziye TreeInstance olarak ekleniyor ve
            // arazi onlari saklar. Komutu ikinci kez calistirmak agaclari
            // UST USTE ekler: ilk denemede 42.649 agac 111.512'ye cikti ve
            // ikinci kosuda 180 bini bulacakti. Ayni tuzaga dere yataklarini
            // oyarken de dusuldu (orada yatak iki kat derinlesiyordu).
            //
            // Yesil doku burada yeniden dikiliyor, yani arazi her seferinde
            // bilinen bir baslangictan basliyor.
            GreeneryBuilder.BuildMenu();
            EditorSceneManager.OpenScene(TerrainScene, OpenSceneMode.Single);
            arazi = Object.FindAnyObjectByType<Terrain>();
            eski = GameObject.Find(KokAdi);
            if (eski != null) Object.DestroyImmediate(eski);
            kok = GameObject.Find(KokAdi) ?? new GameObject(KokAdi);

            float[,] kot = KotIzgarasi(arazi);
            var yollar = new List<List<Vector3>>();

            foreach (var k in kapilar)
            {
                // ICERI: kapidan sehre. Yol en yakin sehir noktasina gider;
                // yedi kapinin yollari boylece dogal olarak ayni eksende
                // birlesir (Divanyolu'nun mantigi budur).
                var hedef = sehir.OrderBy(
                    s => (s - k).sqrMagnitude).First();
                var yol = EnDuzYol(arazi, kot, k, hedef);
                if (yol.Count >= 4) yollar.Add(yol);
            }

            int yolMesh = 0;
            foreach (var y in yollar)
            {
                YolMeshi(kok.transform, y, arazi, yolMesh);
                yolMesh++;
            }

            int bostan = Bostanlar(kok.transform, arazi, yollar, kot);

            var tag = kok.AddComponent<HistoricalTag>();
            tag.tier = HistoricalTier.Reconstruction;
            tag.sourceNote =
                "Sur ile yerlesim arasindaki kirsal doku (T2, taslak). "
                + "Konumu kaynaktan gelen tekil yapi YOK; donemin yapi "
                + "TURLERI konuldu: kapi yollari, bostan parselleri, "
                + "meyvelik. Yol guzergahi en-az-egim yolundan (olcum), "
                + "parsel sinirlari bizim (uydurma). Karar ADR 0074 (A).";

            EditorSceneManager.MarkSceneDirty(sahne);
            EditorSceneManager.SaveScene(sahne, TerrainScene);

            Debug.Log($"[Hezarfen] Kirsal doku: {yollar.Count} yol, "
                      + $"{bostan} bostan parseli.");
        }

        /// <summary>Kara suru kapılarının dünya konumları.</summary>
        private static List<Vector2> Kapilar()
        {
            var liste = new List<Vector2>();
            var sur = GameObject.Find("SUR_Kara");
            if (sur == null) return liste;
            foreach (var t in sur.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("PF_KaraSurKapisi"))
                    liste.Add(new Vector2(t.position.x, t.position.z));
            return liste;
        }

        /// <summary>
        /// Şehrin "içerisi" — landmark konumları.
        ///
        /// Semtler ayrı sahnelerde ve bu araç arazi sahnesinde çalışıyor;
        /// landmark'lar ise burada ve şehrin nerede olduğunu onlardan iyi
        /// bilen yok.
        /// </summary>
        private static List<Vector2> SehirNoktalari()
        {
            var liste = new List<Vector2>();
            var kok = GameObject.Find("LANDMARK_1632");
            if (kok == null) return liste;
            foreach (Transform t in kok.transform)
                if (t.name.StartsWith("PF_"))
                    liste.Add(new Vector2(t.position.x, t.position.z));
            return liste;
        }

        private static float[,] KotIzgarasi(Terrain arazi)
        {
            int n = Izgara;
            float boy = arazi.terrainData.size.x / n;
            var kok = arazi.transform.position;
            var kot = new float[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    kot[x, y] = arazi.SampleHeight(new Vector3(
                        kok.x + (x + 0.5f) * boy, 0f,
                        kok.z + (y + 0.5f) * boy)) + kok.y;
            return kot;
        }

        /// <summary>
        /// İki nokta arasında <b>en az eğime tırmanan</b> yol.
        ///
        /// Dere için yazılan yol en alçağı seviyordu; araba yolu başka bir
        /// şey ister: <b>düzlük</b>. Maliyet komşular arası kot farkının
        /// karesi — yani yol dik yamaçtan kaçar, uzun da olsa yamacı
        /// dolaşır. Osmanlı arabalı yolunun yaptığı budur.
        /// </summary>
        private static List<Vector3> EnDuzYol(Terrain arazi, float[,] kot,
                                              Vector2 bas, Vector2 son)
        {
            int n = Izgara;
            var kokP = arazi.transform.position;
            float boy = arazi.terrainData.size.x / n;

            (int x, int y) H(Vector2 d) => (
                Mathf.Clamp(Mathf.FloorToInt((d.x - kokP.x) / boy), 0, n - 1),
                Mathf.Clamp(Mathf.FloorToInt((d.y - kokP.z) / boy), 0, n - 1));

            var b = H(bas); var s = H(son);
            var maliyet = new float[n, n];
            var geldi = new (int x, int y)[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                { maliyet[x, y] = float.MaxValue; geldi[x, y] = (-1, -1); }

            var acik = new SortedSet<(float m, int x, int y)>();
            maliyet[b.x, b.y] = 0f;
            acik.Add((0f, b.x, b.y));

            while (acik.Count > 0)
            {
                var su = acik.Min; acik.Remove(su);
                if (su.x == s.x && su.y == s.y) break;
                if (su.m > maliyet[su.x, su.y]) continue;

                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int ax = su.x + dx, ay = su.y + dy;
                        if (ax < 0 || ay < 0 || ax >= n || ay >= n) continue;
                        if (kot[ax, ay] < 1.5f) continue;      // suya girmez

                        float adim = (dx != 0 && dy != 0) ? 1.4142f : 1f;
                        float fark = Mathf.Abs(kot[ax, ay] - kot[su.x, su.y]);
                        // Egim CEZASI kareli: 2 m'lik bir sicrama, iki tane
                        // 1 m'lik sicramadan pahali olsun.
                        float bedel = adim + fark * fark * 0.9f;
                        float yeni = maliyet[su.x, su.y] + bedel;
                        if (yeni >= maliyet[ax, ay]) continue;
                        maliyet[ax, ay] = yeni;
                        geldi[ax, ay] = (su.x, su.y);
                        acik.Add((yeni, ax, ay));
                    }
            }

            var yol = new List<Vector3>();
            var p = s;
            var korkuluk = new HashSet<(int, int)>();
            while (p.x >= 0 && korkuluk.Add(p))
            {
                yol.Add(new Vector3(kokP.x + (p.x + 0.5f) * boy,
                                    kot[p.x, p.y],
                                    kokP.z + (p.y + 0.5f) * boy));
                if (p.x == b.x && p.y == b.y) break;
                p = geldi[p.x, p.y];
            }
            yol.Reverse();
            return Seyrelt(Yumusat(yol, 3), 2.0f);
        }

        /// <summary>
        /// Üst üste düşen yol noktalarını atar.
        ///
        /// Yumuşatma ve ızgara çözünürlüğü birlikte, ardışık iki noktayı
        /// neredeyse aynı yere koyabiliyor. Şerit mesh'i o noktada
        /// <b>sıfır alanlı üçgen</b> üretir ve PhysX çarpıştırıcıyı
        /// pişiremez: <c>cleaning the mesh failed</c>. Hata sahne her
        /// açıldığında düşüyor ve 15 test birden kırıldı — kusur testlerde
        /// değil, ürettiğim geometridedeydi.
        /// </summary>
        private static List<Vector3> Seyrelt(List<Vector3> yol, float enAz)
        {
            var c = new List<Vector3>(yol.Count);
            foreach (var p in yol)
            {
                if (c.Count > 0)
                {
                    var a = c[c.Count - 1];
                    float dx = p.x - a.x, dz = p.z - a.z;
                    if (dx * dx + dz * dz < enAz * enAz) continue;
                }
                c.Add(p);
            }
            // En az iki nokta kalsin ki serit uretilebilsin.
            if (c.Count < 2 && yol.Count >= 2)
            { c.Clear(); c.Add(yol[0]); c.Add(yol[yol.Count - 1]); }
            return c;
        }

        private static List<Vector3> Yumusat(List<Vector3> yol, int r)
        {
            var c = new List<Vector3>(yol.Count);
            for (int i = 0; i < yol.Count; i++)
            {
                Vector3 t = Vector3.zero; int k = 0;
                for (int j = -r; j <= r; j++)
                {
                    int a = i + j;
                    if (a < 0 || a >= yol.Count) continue;
                    t += yol[a]; k++;
                }
                c.Add(t / k);
            }
            return c;
        }

        /// <summary>
        /// Yol yüzeyi: araziyi izleyen şerit + iki yanda araziye inen
        /// bordür.
        ///
        /// Mahalle kaldırımıyla aynı kesit mantığı — ve <b>aynı düzeltmeyle</b>:
        /// bordür kesitin en ALÇAK noktasına iner. Kaldırımda bu satır
        /// yanlıştı ve kenar hücrelerinin %28,7'si havada kalmıştı.
        /// </summary>
        private static void YolMeshi(Transform ebeveyn, List<Vector3> yol,
                                     Terrain arazi, int no)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float mesafe = 0f;
            float ay = arazi.transform.position.y;

            float Kot(Vector2 p) =>
                arazi.SampleHeight(new Vector3(p.x, 0f, p.y)) + ay;

            for (int i = 0; i < yol.Count; i++)
            {
                Vector3 ileri = i == 0 ? yol[1] - yol[0] : yol[i] - yol[i - 1];
                ileri.y = 0f;
                if (ileri.sqrMagnitude < 1e-4f) ileri = Vector3.forward;
                ileri.Normalize();
                var yan = new Vector3(-ileri.z, 0f, ileri.x);

                var c = new Vector2(yol[i].x, yol[i].z);
                var a = c + new Vector2(yan.x, yan.z) * YolGenisligi * 0.5f;
                var b = c - new Vector2(yan.x, yan.z) * YolGenisligi * 0.5f;

                float us = Mathf.Max(Kot(a), Mathf.Max(Kot(c), Kot(b)));
                float dip = Mathf.Min(Kot(a), Mathf.Min(Kot(c), Kot(b))) - 0.3f;
                float yuzey = us + 0.04f;

                // Ust yuzey
                verts.Add(new Vector3(a.x, yuzey, a.y));
                verts.Add(new Vector3(b.x, yuzey, b.y));
                // Bordur dibi
                verts.Add(new Vector3(a.x, dip, a.y));
                verts.Add(new Vector3(b.x, dip, b.y));

                uvs.Add(new Vector2(0f, mesafe / 4f));
                uvs.Add(new Vector2(1f, mesafe / 4f));
                uvs.Add(new Vector2(0f, mesafe / 4f));
                uvs.Add(new Vector2(1f, mesafe / 4f));

                if (i > 0)
                {
                    int p = (i - 1) * 4, q = i * 4;
                    // yuzey
                    tris.Add(p); tris.Add(q); tris.Add(p + 1);
                    tris.Add(p + 1); tris.Add(q); tris.Add(q + 1);
                    // sol bordur
                    tris.Add(p); tris.Add(p + 2); tris.Add(q);
                    tris.Add(q); tris.Add(p + 2); tris.Add(q + 2);
                    // sag bordur
                    tris.Add(p + 1); tris.Add(q + 1); tris.Add(p + 3);
                    tris.Add(p + 3); tris.Add(q + 1); tris.Add(q + 3);
                    mesafe += Vector3.Distance(yol[i - 1], yol[i]);
                }
            }

            var mesh = new Mesh { name = $"SM_Yol_{no:00}" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            EnsureFolder("Assets/_Project/Art/Models/Generated");
            AssetDatabase.CreateAsset(
                mesh, $"Assets/_Project/Art/Models/Generated/SM_Yol_{no:00}.asset");

            var go = new GameObject($"Yol_{no:00}");
            go.transform.SetParent(ebeveyn, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_Project/Art/Materials/Ottoman/M_Paving_Kaldirim.mat");
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        /// <summary>
        /// Boşluğu <b>ızgarayla</b> doldurur: bostan parselleri, meyvelik,
        /// bağ.
        ///
        /// ## Neden yol kenarı yetmedi
        ///
        /// İlk yazımda parseller yalnız yol kenarına konuyordu ve sonuç
        /// ölçüldü: 4,5 km'lik şeritte <b>56 parsel</b>, kapsam
        /// x −4287…−2065. Dolan şey yolun iki yanıydı, alanın kendisi
        /// değil — yani şikâyet çözülmemişti.
        ///
        /// ## Neden tek mesh
        ///
        /// Aynı denemede 39 parsel <b>1.873 ayrı çizici</b> üretti; her
        /// duvar parçası bir nesne. Alanı gerçekten doldurmak yüzlerce
        /// parsel demek, yani on binlerce çizici, ve Faz 7'de ölçülen kare
        /// bütçesi bunu kaldırmaz. Duvarlar tek mesh'te birleşiyor —
        /// kaidelerle aynı gerekçe: parsel duvarı kıpırdamaz.
        ///
        /// Ağaçlar prefab değil <b>arazi ağacı</b> olarak konuyor; arazi
        /// onları kendi LOD'u ve billboard'uyla çiziyor.
        /// </summary>
        private static int Bostanlar(Transform ebeveyn, Terrain arazi,
                                     List<List<Vector3>> yollar, float[,] kot)
        {
            var rng = new System.Random(1632);
            float ay = arazi.transform.position.y;

            var kapilar = Kapilar();
            var sehir = SehirNoktalari();
            if (kapilar.Count == 0 || sehir.Count == 0) return 0;

            float xMin = Mathf.Min(kapilar.Min(k => k.x), sehir.Min(t => t.x));
            float xMax = Mathf.Max(kapilar.Max(k => k.x), sehir.Max(t => t.x));
            float zMin = Mathf.Min(kapilar.Min(k => k.y), sehir.Min(t => t.y));
            float zMax = Mathf.Max(kapilar.Max(k => k.y), sehir.Max(t => t.y));

            var yolNoktalari = new List<Vector2>();
            foreach (var y in yollar)
                foreach (var q in y) yolNoktalari.Add(new Vector2(q.x, q.z));

            // SEMTLERIN ICINE GIRME.
            //
            // Ilk izgara denemesinde kapsam x −4297…3526 cikti: yani
            // Uskudar'a kadar her yer. Sebep, sinirlarin kapilar ile
            // landmark'larin kutusundan turemesiydi ve o kutu butun sehri
            // kapsiyor. Bostan parseli evlerin ARASINA duserdi.
            //
            // Semt sinirlari zaten veri olarak var (DistrictDef.boundary);
            // uydurmaya gerek yok.
            var semtler = new List<DistrictDef>();
            var reg = AssetDatabase.LoadAssetAtPath<DistrictRegistry>(
                "Assets/_Project/Data/DistrictDefs/DistrictRegistry.asset");
            if (reg != null && reg.districts != null)
                foreach (var d in reg.districts)
                    if (d != null && d.boundary != null && d.boundary.Length >= 3)
                        semtler.Add(d);

            var duvarlar = new List<(Vector2 c, float y, float gen,
                                     float der, float yaw)>();
            var agacYerleri = new List<(Vector3 p, float olcek)>();
            int parsel = 0, meyvelik = 0;

            const float Adim = 58f;
            for (float z = zMin; z <= zMax; z += Adim)
                for (float x = xMin; x <= xMax; x += Adim)
                {
                    var c = new Vector2(
                        x + (float)(rng.NextDouble() - 0.5) * Adim * 0.35f,
                        z + (float)(rng.NextDouble() - 0.5) * Adim * 0.35f);

                    // Sehrin ICINE girme: landmark 130 m'den yakinsa orasi
                    // doku, bostan degil.
                    if (sehir.Any(sp => (sp - c).sqrMagnitude < 130f * 130f))
                        continue;
                    // Semtin ICINDE ya da 40 m yakininda parsel olmaz.
                    if (semtler.Any(d => DistrictDef.ContainsXZ(
                                             d.boundary, c.x, c.y)
                                      || DistrictDef.DistanceXZ(
                                             d.boundary, c.x, c.y) < 40f))
                        continue;
                    if (yolNoktalari.Any(
                            yp => (yp - c).sqrMagnitude < 18f * 18f))
                        continue;

                    float lo = float.MaxValue, hi = float.MinValue;
                    for (int a = -1; a <= 1; a += 2)
                        for (int b = -1; b <= 1; b += 2)
                        {
                            float h = arazi.SampleHeight(new Vector3(
                                c.x + a * 14f, 0f, c.y + b * 10f)) + ay;
                            lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                        }
                    if (lo < 2.5f) continue;

                    float yaw = (float)rng.NextDouble() * 360f;

                    // BOSTAN SULANIR ve su yokusta durmaz: duz zemin sarti.
                    if (hi - lo < 2.2f && rng.Next(100) < 62)
                    {
                        float gen = 24f + (float)rng.NextDouble() * 10f;
                        float der = 16f + (float)rng.NextDouble() * 8f;
                        duvarlar.Add((c, hi, gen, der, yaw));
                        parsel++;
                    }
                    else
                    {
                        // Agac sayisi olculdu: 6-12 arasi kume
                        // 68.863 agac ekliyordu ve arazi zaten 42.649
                        // tasiyordu. Faz 7'nin kare butcesi bunu
                        // sinamadan buyutmek dogru olmaz.
                        int n = 3 + rng.Next(4);
                        for (int i = 0; i < n; i++)
                        {
                            var q = c + new Vector2(
                                (float)(rng.NextDouble() - 0.5) * 34f,
                                (float)(rng.NextDouble() - 0.5) * 34f);
                            float h = arazi.SampleHeight(
                                new Vector3(q.x, 0f, q.y)) + ay;
                            if (h < 2.5f) continue;
                            agacYerleri.Add((new Vector3(q.x, h, q.y),
                                             0.8f + (float)rng.NextDouble() * 0.4f));
                        }
                        meyvelik++;
                    }
                }

            DuvarMeshi(ebeveyn, duvarlar);
            AraziAgaclari(arazi, agacYerleri);

            Debug.Log($"[Hezarfen] Kirsal: {parsel} bostan parseli, "
                      + $"{meyvelik} meyvelik/bag ({agacYerleri.Count} agac).");
            return parsel;
        }

        /// <summary>
        /// Bütün parsel duvarlarını <b>tek mesh</b>te birleştirir.
        /// Bostan duvarı bel hizasındadır (0,9 m), avlu duvarı değil.
        /// </summary>
        private static void DuvarMeshi(
            Transform ebeveyn,
            List<(Vector2 c, float y, float gen, float der, float yaw)> parseller)
        {
            if (parseller.Count == 0) return;

            const float Yukseklik = 0.9f;
            const float Kalinlik = 0.35f;
            const float TexMetre = 2.0f;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            foreach (var p2 in parseller)
            {
                var rot = Quaternion.Euler(0f, p2.yaw, 0f);
                float hw = p2.gen * 0.5f, hd = p2.der * 0.5f;
                Kutu(verts, uvs, tris, p2.c, p2.y, rot,
                     new Vector3(0f, 0f, hd), p2.gen, Kalinlik, Yukseklik, TexMetre);
                Kutu(verts, uvs, tris, p2.c, p2.y, rot,
                     new Vector3(0f, 0f, -hd), p2.gen, Kalinlik, Yukseklik, TexMetre);
                Kutu(verts, uvs, tris, p2.c, p2.y, rot,
                     new Vector3(hw, 0f, 0f), Kalinlik, p2.der, Yukseklik, TexMetre);
                Kutu(verts, uvs, tris, p2.c, p2.y, rot,
                     new Vector3(-hw, 0f, 0f), Kalinlik, p2.der, Yukseklik, TexMetre);
            }

            var mesh = new Mesh { name = "SM_BostanDuvarlari" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            EnsureFolder("Assets/_Project/Art/Models/Generated");
            AssetDatabase.CreateAsset(
                mesh,
                "Assets/_Project/Art/Models/Generated/SM_BostanDuvarlari.asset");

            var go = new GameObject("BostanDuvarlari");
            go.transform.SetParent(ebeveyn, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_Project/Art/Materials/Ottoman/M_Stone_Rubble.mat");
            go.AddComponent<MeshCollider>().sharedMesh = mesh;

            var tag = go.AddComponent<HistoricalTag>();
            tag.tier = HistoricalTier.Reconstruction;
            tag.sourceNote = "Bostan parsel duvarlari (T2, taslak). Parsel "
                           + "sinirlari kaynaktan DEGIL; donem dokusunun "
                           + "turu, karar ADR 0074 (A).";
        }

        /// <summary>Bir kutu: dört yan yüz + üst. Alt yüz görünmez.</summary>
        private static void Kutu(List<Vector3> v, List<Vector2> uv,
                                 List<int> tri, Vector2 merkez, float taban,
                                 Quaternion rot, Vector3 yerel,
                                 float gen, float der, float yuk, float tex)
        {
            var o = new Vector3(merkez.x, taban, merkez.y) + rot * yerel;
            float hw = gen * 0.5f, hd = der * 0.5f;
            Vector3[] alt =
            {
                o + rot * new Vector3(-hw, 0f, -hd),
                o + rot * new Vector3( hw, 0f, -hd),
                o + rot * new Vector3( hw, 0f,  hd),
                o + rot * new Vector3(-hw, 0f,  hd),
            };

            for (int i = 0; i < 4; i++)
            {
                var a = alt[i];
                var b = alt[(i + 1) % 4];
                int i0 = v.Count;
                // Duvar araziye 0,4 m gomulur ki alt kenari acikta kalmasin
                // — kaldirimda ogrenilen ders.
                v.Add(a + Vector3.down * 0.4f);
                v.Add(b + Vector3.down * 0.4f);
                v.Add(b + Vector3.up * yuk);
                v.Add(a + Vector3.up * yuk);
                float u = Vector3.Distance(a, b) / tex;
                float h = (yuk + 0.4f) / tex;
                uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(u, 0f));
                uv.Add(new Vector2(u, h)); uv.Add(new Vector2(0f, h));
                tri.Add(i0); tri.Add(i0 + 2); tri.Add(i0 + 1);
                tri.Add(i0); tri.Add(i0 + 3); tri.Add(i0 + 2);
            }

            int t0 = v.Count;
            for (int i = 0; i < 4; i++) v.Add(alt[i] + Vector3.up * yuk);
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(gen / tex, 0f));
            uv.Add(new Vector2(gen / tex, der / tex));
            uv.Add(new Vector2(0f, der / tex));
            tri.Add(t0); tri.Add(t0 + 2); tri.Add(t0 + 1);
            tri.Add(t0); tri.Add(t0 + 3); tri.Add(t0 + 2);
        }

        /// <summary>
        /// Meyvelik ağaçlarını <b>arazi ağacı</b> olarak ekler.
        ///
        /// Prefab örneği binlerce çizici demekti. Prototip yoksa sessizce
        /// geçilmez: ağaçsız bir meyvelik meyvelik değildir ve nedeni
        /// hiçbir yerde yazmazdı.
        /// </summary>
        private static void AraziAgaclari(Terrain arazi,
                                          List<(Vector3 p, float olcek)> yerler)
        {
            if (yerler.Count == 0) return;
            var data = arazi.terrainData;
            if (data.treePrototypes == null || data.treePrototypes.Length == 0)
            {
                Debug.LogWarning("[Hezarfen] Agac prototipi yok — meyvelik "
                                 + "agacsiz kaldi. Once GIS/Yesil dokuyu dik.");
                return;
            }

            // Meyve agaci varligimiz YOK; cinarla temsil (ADR 0026 6).
            var cinar = new List<int>();
            for (int i = 0; i < data.treePrototypes.Length; i++)
            {
                var pf = data.treePrototypes[i].prefab;
                if (pf != null && pf.name.Contains("Cinar")) cinar.Add(i);
            }
            if (cinar.Count == 0) cinar.Add(0);

            var kok = arazi.transform.position;
            var liste = new List<TreeInstance>(data.treeInstances);
            int eskiSayi = liste.Count;
            var rng = new System.Random(1632);

            foreach (var yer in yerler)
            {
                liste.Add(new TreeInstance
                {
                    position = new Vector3(
                        (yer.p.x - kok.x) / data.size.x,
                        (yer.p.y - kok.y) / data.size.y,
                        (yer.p.z - kok.z) / data.size.z),
                    prototypeIndex = cinar[rng.Next(cinar.Count)],
                    widthScale = yer.olcek,
                    heightScale = yer.olcek,
                    color = Color.white,
                    lightmapColor = Color.white,
                    rotation = (float)rng.NextDouble() * Mathf.PI * 2f,
                });
            }

            data.SetTreeInstances(liste.ToArray(), true);
            Debug.Log($"[Hezarfen] Meyvelik agaci: {liste.Count - eskiSayi} "
                      + $"eklendi (toplam {liste.Count}).");
        }

        private static void Koy(GameObject prefab, Transform ebeveyn,
                                Vector3 p, Vector3 bakis)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(
                prefab, ebeveyn);
            inst.transform.position = p;
            inst.transform.rotation = Quaternion.LookRotation(
                new Vector3(bakis.x, 0f, bakis.z), Vector3.up);
        }

        private static void EnsureFolder(string yol)
        {
            if (AssetDatabase.IsValidFolder(yol)) return;
            var parca = yol.Split('/');
            string b = parca[0];
            for (int i = 1; i < parca.Length; i++)
            {
                string alt = b + "/" + parca[i];
                if (!AssetDatabase.IsValidFolder(alt))
                    AssetDatabase.CreateFolder(b, parca[i]);
                b = alt;
            }
        }
    }
}
