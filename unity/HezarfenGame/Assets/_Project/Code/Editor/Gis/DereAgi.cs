using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Gis
{
    /// <summary>
    /// <b>Dere yatağı çizilmez, ÖLÇÜLÜR.</b>
    ///
    /// ADR 0074: Caner üç dereyi de istedi (Kağıthane, Alibey, Lykos). Ama
    /// bu depoda hiçbirinin güzergâhı için kaynak satırı yok ve CLAUDE.md
    /// niteliksel kaynaktan metrik geometri üretmeyi yasaklıyor.
    ///
    /// Çıkış yolu şu: derenin nerede aktığı zaten <b>arazinin kendisinde
    /// yazılı</b>. Su en dik inişi izler; yeterince su bir araya geldiği
    /// yerde yatak oluşur. Yani yatağı elle çizmek yerine DEM'e sordurmak,
    /// bir çizim değil bir <b>ölçüm</b>dür — ve ölçümün kaynağı, bütün
    /// arazinin kaynağıyla aynıdır (ADR 0007).
    ///
    /// ## Yöntem — D8 akış birikimi
    ///
    /// 1. Yükseklik ızgarası okunur.
    /// 2. Her hücre sekiz komşusundan <b>en dik inen</b>ine akar (D8).
    /// 3. Hücreler yüksekten alçağa sıralanır ve her biri kendi suyunu
    ///    aşağıya devreder. Bir hücrede biriken sayı, ona kadar süzülen
    ///    <b>havzanın alanıdır</b>.
    /// 4. Birikimi eşiği aşan ve denize ulaşan hatlar akarsudur.
    ///
    /// Çukurlar (DEM gürültüsünün açtığı kapalı havzalar) akışı kilitler;
    /// bunlar önce doldurulur, yoksa dereler yarı yolda kaybolur.
    ///
    /// ## Neden önce ÖLÇÜM, sonra geometri
    ///
    /// Bu turda dört kez yanlış cetvelle ölçtüm ve dördünde de sayı
    /// gerçekten olduğundan kötü göründü. Bir dere yatağını doğrudan
    /// sahneye yazmak, aynı hatayı 15 km'lik bir çizgi olarak gömmek
    /// olurdu. Önce rapor üretilir, ağzının nerede olduğuna BAKILIR.
    /// </summary>
    public static class DereAgi
    {
        private const string TerrainScene =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";

        /// <summary>
        /// Analiz ızgarası kenarı. Arazi 15.337 m; 512 hücre ≈ <b>30 m</b>
        /// çözünürlük. Dere yatağı 10-30 m genişliğinde olduğuna göre bu
        /// hattı bulmaya yeter ve 2049'luk tam çözünürlükte D8 sıralaması
        /// 4,2 milyon hücre demek olurdu.
        /// </summary>
        public const int Izgara = 512;

        /// <summary>
        /// Akarsu sayılmak için gereken en küçük havza — hücre adedi.
        /// 300 hücre ≈ <b>0,27 km²</b>. Daha küçüğü yağmur oluğudur.
        /// </summary>
        public const int AkarsuEsigi = 300;

        [MenuItem("Hezarfen/GIS/Dere aglarini olc")]
        public static void Olc()
        {
            EditorSceneManager.OpenScene(TerrainScene, OpenSceneMode.Single);
            var arazi = Object.FindAnyObjectByType<Terrain>();
            if (arazi == null)
            {
                Debug.LogError("[Hezarfen] TR_Istanbul yok.");
                return;
            }

            var kollar = Bul(arazi, out float[,] birikim, out float[,] h);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Dere ağı — arazinin kendi söylediği");
            sb.AppendLine();
            sb.AppendLine($"Izgara {Izgara}×{Izgara} "
                          + $"(hücre ≈ {arazi.terrainData.size.x / Izgara:0.0} m) · "
                          + $"akarsu eşiği {AkarsuEsigi} hücre "
                          + $"(≈ {AkarsuEsigi * Mathf.Pow(arazi.terrainData.size.x / Izgara, 2) / 1e6f:0.00} km²)");
            sb.AppendLine();
            sb.AppendLine("D8 akış birikimi: her hücre en dik inen komşusuna");
            sb.AppendLine("akar; biriken sayı o noktaya süzülen havzanın");
            sb.AppendLine("alanıdır. Yatak **çizilmedi**, bulundu.");
            sb.AppendLine();
            sb.AppendLine("## Denize ulaşan en büyük on kol");
            sb.AppendLine();
            sb.AppendLine("| # | havza (km²) | uzunluk (m) | ağız (x, z) | "
                          + "kaynak kotu (m) |");
            sb.AppendLine("|---:|---:|---:|---|---:|");

            float hucreAlan = Mathf.Pow(arazi.terrainData.size.x / Izgara, 2);
            for (int i = 0; i < Mathf.Min(10, kollar.Count); i++)
            {
                var k = kollar[i];
                float uzunluk = 0f;
                for (int j = 1; j < k.nokta.Count; j++)
                    uzunluk += Vector3.Distance(k.nokta[j - 1], k.nokta[j]);
                var agiz = k.nokta[k.nokta.Count - 1];
                sb.AppendLine($"| {i + 1} | {k.havza * hucreAlan / 1e6f:0.00} | "
                              + $"{uzunluk:0} | ({agiz.x:0}, {agiz.z:0}) | "
                              + $"{k.nokta[0].y:0} |");
            }

            // --- HARITAYA KENARDAN GIREN VADILER ---
            sb.AppendLine();
            sb.AppendLine("## Haritaya kenardan giren vadiler");
            sb.AppendLine();
            sb.AppendLine("Yukaridaki tablo bir seyi acikca soyluyor: en buyuk");
            sb.AppendLine("havza **0,83 km²**. Kagithane deresinin gercek havzasi");
            sb.AppendLine("100 km²'nin ustundedir. Yani bu haritada o derelerin");
            sb.AppendLine("**havzalari yok** — arazi 15,3 km ve Galata merkezli,");
            sb.AppendLine("dereler kenardan zaten buyumus olarak giriyor.");
            sb.AppendLine("Akis birikimi bu yuzden yalniz yerel oluklari buluyor.");
            sb.AppendLine();
            sb.AppendLine("Alt tablo baska bir sey olcuyor: haritanin kenarindan");
            sb.AppendLine("baslayip denize inen **vadi tabanlari**. Bir vadinin");
            sb.AppendLine("varligi havza buyuklugune degil, arazinin kendi");
            sb.AppendLine("bicimine bakar; oyuk derinligi (yol boyunca cevre");
            sb.AppendLine("sirtlarindan ne kadar asagida aktigi) bunu olcer.");
            sb.AppendLine();
            sb.AppendLine("| # | uzunluk (m) | oyuk derinliği (m) | giriş (x, z) | "
                          + "ağız (x, z) |");
            sb.AppendLine("|---:|---:|---:|---|---|");

            var vadiler = VadiBul(arazi);
            sb.AppendLine();
            sb.AppendLine("**Eleme:** " + SonEleme);
            sb.AppendLine();
            for (int i = 0; i < Mathf.Min(8, vadiler.Count); i++)
            {
                var v = vadiler[i];
                float uz = 0f;
                for (int j = 1; j < v.nokta.Count; j++)
                    uz += Vector3.Distance(v.nokta[j - 1], v.nokta[j]);
                var g = v.nokta[0];
                var a2 = v.nokta[v.nokta.Count - 1];
                sb.AppendLine($"| {i + 1} | {uz:0} | {v.havza:0.0} | "
                              + $"({g.x:0}, {g.z:0}) | ({a2.x:0}, {a2.z:0}) |");
            }

            Directory.CreateDirectory("../../renders/denetim");
            File.WriteAllText("../../renders/denetim/dere_agi.md", sb.ToString());
            Debug.Log($"[Hezarfen] Dere agi: {kollar.Count} kol bulundu.\n"
                      + "renders/denetim/dere_agi.md");
        }

        /// <summary>Bulunan bir akarsu kolu.</summary>
        public sealed class Kol
        {
            /// <summary>Kaynaktan ağza, dünya koordinatında.</summary>
            public List<Vector3> nokta = new List<Vector3>();

            /// <summary>Ağızdaki birikim — havza büyüklüğü (hücre).</summary>
            public float havza;
        }

        /// <summary>
        /// Akarsu kollarını bulur — havzası büyükten küçüğe sıralı.
        /// </summary>
        public static List<Kol> Bul(Terrain arazi, out float[,] birikim,
                                    out float[,] h)
        {
            var data = arazi.terrainData;
            int n = Izgara;
            float boy = data.size.x / n;
            var kok = arazi.transform.position;

            // --- 1. yukseklik izgarasi (dunya kotu) ---
            // `out` parametresi lambda icinde kullanilamaz; yerel bir
            // degiskene calisilip sonunda disari verilir.
            var kot = new float[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var d = new Vector3(kok.x + (x + 0.5f) * boy, 0f,
                                        kok.z + (y + 0.5f) * boy);
                    kot[x, y] = arazi.SampleHeight(d) + kok.y;
                }
            h = kot;

            // --- 2. cukurlari doldur ---
            //
            // DEM gurultusunun actigi kapali havza akisi kilitler ve dere
            // yari yolda kaybolur. Priority-flood'un basit hali: hucreler
            // alcaktan yuksege islenir, her biri komsusundan asagi
            // olamaz.
            var dolu = (float[,])kot.Clone();
            var sira = new List<(int x, int y)>(n * n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++) sira.Add((x, y));
            sira.Sort((a, b) => kot[a.x, a.y].CompareTo(kot[b.x, b.y]));

            for (int gecis = 0; gecis < 3; gecis++)
                foreach (var (x, y) in sira)
                {
                    if (dolu[x, y] <= 0f) continue;      // deniz
                    float enAlcakKomsu = float.MaxValue;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int ax = x + dx, ay = y + dy;
                            if (ax < 0 || ay < 0 || ax >= n || ay >= n) continue;
                            enAlcakKomsu = Mathf.Min(enAlcakKomsu, dolu[ax, ay]);
                        }
                    if (dolu[x, y] < enAlcakKomsu)
                        dolu[x, y] = enAlcakKomsu + 0.01f;
                }

            // --- 3. D8 akis yonu ---
            var yonX = new int[n, n];
            var yonY = new int[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float enDik = 0f;
                    yonX[x, y] = 0; yonY[x, y] = 0;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int ax = x + dx, ay = y + dy;
                            if (ax < 0 || ay < 0 || ax >= n || ay >= n) continue;
                            float mesafe = (dx != 0 && dy != 0) ? 1.4142f : 1f;
                            float egim = (dolu[x, y] - dolu[ax, ay]) / mesafe;
                            if (egim > enDik) { enDik = egim; yonX[x, y] = dx; yonY[x, y] = dy; }
                        }
                }

            // --- 4. birikim: yuksekten alcaga devret ---
            birikim = new float[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++) birikim[x, y] = 1f;

            sira.Sort((a, b) => dolu[b.x, b.y].CompareTo(dolu[a.x, a.y]));
            foreach (var (x, y) in sira)
            {
                int dx = yonX[x, y], dy = yonY[x, y];
                if (dx == 0 && dy == 0) continue;
                birikim[x + dx, y + dy] += birikim[x, y];
            }

            // --- 5. denize ulasan kollari cikar ---
            //
            // Agiz: birikimi esigi asan ve komsusu DENIZ olan hucre.
            var kollar = new List<Kol>();
            var alinan = new HashSet<(int, int)>();

            var agizlar = new List<(int x, int y, float acc)>();
            for (int y = 1; y < n - 1; y++)
                for (int x = 1; x < n - 1; x++)
                {
                    if (kot[x, y] <= 0f) continue;
                    if (birikim[x, y] < AkarsuEsigi) continue;
                    int dx = yonX[x, y], dy = yonY[x, y];
                    if (dx == 0 && dy == 0) continue;
                    if (kot[x + dx, y + dy] > 0f) continue;   // hala karada
                    agizlar.Add((x, y, birikim[x, y]));
                }

            foreach (var a in agizlar.OrderByDescending(t => t.acc))
            {
                // Agizdan YUKARI, en cok su getiren komsuyu izleyerek
                // kaynaga yuru; sonra ters cevir.
                var yol = new List<(int x, int y)>();
                int cx = a.x, cy = a.y;
                var gorulen = new HashSet<(int, int)>();
                while (gorulen.Add((cx, cy)))
                {
                    yol.Add((cx, cy));
                    int enX = -1, enY = -1; float enAcc = 0f;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int bx = cx + dx, by = cy + dy;
                            if (bx < 0 || by < 0 || bx >= n || by >= n) continue;
                            // Yalniz BU hucreye akan komsu
                            if (bx + yonX[bx, by] != cx
                                || by + yonY[bx, by] != cy) continue;
                            if (birikim[bx, by] > enAcc)
                            { enAcc = birikim[bx, by]; enX = bx; enY = by; }
                        }
                    if (enX < 0 || enAcc < AkarsuEsigi * 0.25f) break;
                    cx = enX; cy = enY;
                }

                // Baska bir kolun uzerine binen kollar elenir: ayni yatagi
                // iki kez uretmek dereyi kalinlastirmaz, cakistirir.
                int ortak = yol.Count(p => alinan.Contains(p));
                if (ortak > yol.Count * 0.3f) continue;
                foreach (var p in yol) alinan.Add(p);

                var kol = new Kol { havza = a.acc };
                for (int i = yol.Count - 1; i >= 0; i--)
                {
                    var (x, y) = yol[i];
                    kol.nokta.Add(new Vector3(
                        kok.x + (x + 0.5f) * boy,
                        kot[x, y],
                        kok.z + (y + 0.5f) * boy));
                }
                if (kol.nokta.Count >= 4) kollar.Add(kol);
            }

            return kollar.OrderByDescending(k => k.havza).ToList();
        }

        /// <summary>
        /// <b>Haritanın kenarından girip denize inen vadi tabanları.</b>
        ///
        /// Akış birikimi bu harita için yeterli değil: Kağıthane ve Alibey
        /// derelerinin havzası arazinin <b>dışında</b>. O dereler haritaya
        /// kenardan, zaten büyümüş olarak girer ve birikim sayacı onları
        /// yerel bir oluk gibi gösterir.
        ///
        /// Vadinin varlığı havza büyüklüğüne değil <b>arazinin biçimine</b>
        /// bakar. Burada ölçülen şey <i>oyuk derinliği</i>: yolun her
        /// noktasında, 300 m yarıçapındaki çevrenin medyan kotu ile yatağın
        /// kotu arasındaki fark. Derin ve sürekli bir oyuk vadidir; sığ ve
        /// kesikli olan yamaç kıvrımıdır.
        ///
        /// <c>Kol.havza</c> alanı burada <b>metre cinsinden oyuk
        /// derinliğini</b> taşır — aynı alanın iki anlamı olması hoş değil,
        /// ama rapor tek tabloda okunuyor ve ayrı bir tip kurmak bu ölçüm
        /// için fazlaydı.
        /// </summary>
        /// <summary>Son <see cref="VadiBul"/> çağrısının eleme sayaçları.</summary>
        public static string SonEleme = "";

        public static List<Kol> VadiBul(Terrain arazi)
        {
            int sayBasla = 0, sayDenizDegil = 0, sayKisa = 0, sayDenizeVarmadi = 0,
                sayCakisma = 0, saySig = 0;
            float enDerinGorulen = 0f; int enUzunGorulen = 0;
            int n = Izgara;
            float boy = arazi.terrainData.size.x / n;
            var kok = arazi.transform.position;

            var kot = new float[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    kot[x, y] = arazi.SampleHeight(new Vector3(
                        kok.x + (x + 0.5f) * boy, 0f,
                        kok.z + (y + 0.5f) * boy)) + kok.y;

            // CUKURLARI DOLDUR — yoksa iz ILK cukurda takilir.
            //
            // Ilk denemede bu adim yoktu ve tablo BOS cikti: her yol
            // birkac hucrede bir DEM gurultusunun actigi kucuk bir
            // cukura girip duruyordu. "Vadi yok" sonucu, vadinin
            // yoklugunu degil olcumun eksikligini gosteriyordu.
            var dolu = (float[,])kot.Clone();
            var siraliHucre = new List<(int x, int y)>(n * n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++) siraliHucre.Add((x, y));
            siraliHucre.Sort((a, b) => kot[a.x, a.y].CompareTo(kot[b.x, b.y]));
            for (int gecis = 0; gecis < 4; gecis++)
                foreach (var (x, y) in siraliHucre)
                {
                    if (dolu[x, y] <= 0f) continue;
                    float enAlcakKomsu = float.MaxValue;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int ax = x + dx, ay = y + dy;
                            if (ax < 0 || ay < 0 || ax >= n || ay >= n) continue;
                            enAlcakKomsu = Mathf.Min(enAlcakKomsu, dolu[ax, ay]);
                        }
                    if (dolu[x, y] < enAlcakKomsu)
                        dolu[x, y] = enAlcakKomsu + 0.01f;
                }

            var sonuc = new List<Kol>();
            var alinan = new HashSet<(int, int)>();

            // Kenar hucrelerinden basla.
            var baslangic = new List<(int x, int y)>();
            for (int i = 1; i < n - 1; i++)
            {
                baslangic.Add((i, 1));
                baslangic.Add((i, n - 2));
                baslangic.Add((1, i));
                baslangic.Add((n - 2, i));
            }

            foreach (var (bx, by) in baslangic)
            {
                sayBasla++;
                if (dolu[bx, by] <= 2f) { sayDenizDegil++; continue; }
                int cx = bx, cy = by;
                var yol = new List<(int, int)>();
                var gorulen = new HashSet<(int, int)>();
                while (gorulen.Add((cx, cy)) && dolu[cx, cy] > 0f)
                {
                    yol.Add((cx, cy));
                    int enX = -1, enY = -1; float enAlcak = dolu[cx, cy];
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int ax = cx + dx, ay = cy + dy;
                            if (ax < 1 || ay < 1 || ax >= n - 1 || ay >= n - 1)
                                continue;
                            if (dolu[ax, ay] < enAlcak)
                            { enAlcak = dolu[ax, ay]; enX = ax; enY = ay; }
                        }
                    if (enX < 0) break;                  // cukurda takildi
                    cx = enX; cy = enY;
                }

                enUzunGorulen = Mathf.Max(enUzunGorulen, yol.Count);
                if (yol.Count < 25) { sayKisa++; continue; }
                if (dolu[cx, cy] > 2f) { sayDenizeVarmadi++; continue; }
                int ortak = yol.Count(pp => alinan.Contains(pp));
                if (ortak > yol.Count * 0.25f) { sayCakisma++; continue; }

                // Oyuk derinligi: 300 m (10 hucre) yaricapin medyani
                // eksi yatak kotu.
                float toplam = 0f; int say = 0;
                foreach (var (x, y) in yol)
                {
                    var cevre = new List<float>();
                    for (int dy = -10; dy <= 10; dy += 2)
                        for (int dx = -10; dx <= 10; dx += 2)
                        {
                            int ax = x + dx, ay = y + dy;
                            if (ax < 0 || ay < 0 || ax >= n || ay >= n) continue;
                            cevre.Add(kot[ax, ay]);
                        }
                    if (cevre.Count == 0) continue;
                    cevre.Sort();
                    toplam += cevre[cevre.Count / 2] - kot[x, y];
                    say++;
                }
                float derinlik = say == 0 ? 0f : toplam / say;
                enDerinGorulen = Mathf.Max(enDerinGorulen, derinlik);
                if (derinlik < 6f) { saySig++; continue; }

                foreach (var pp in yol) alinan.Add(pp);
                var kol = new Kol { havza = derinlik };
                foreach (var (x, y) in yol)
                    kol.nokta.Add(new Vector3(kok.x + (x + 0.5f) * boy,
                                              kot[x, y],
                                              kok.z + (y + 0.5f) * boy));
                sonuc.Add(kol);
            }

            SonEleme = $"baslangic {sayBasla} · zaten deniz {sayDenizDegil} · "
                       + $"kisa {sayKisa} · denize varmadi {sayDenizeVarmadi} · "
                       + $"cakisma {sayCakisma} · sig {saySig} · "
                       + $"gecen {sonuc.Count} | en uzun yol {enUzunGorulen} hucre, "
                       + $"en derin oyuk {enDerinGorulen:0.0} m";
            return sonuc.OrderByDescending(k => k.havza * k.nokta.Count).ToList();
        }
    }
}
