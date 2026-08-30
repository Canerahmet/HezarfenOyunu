using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>İki ev aynı yerde mi duruyor?</b>
    ///
    /// Sokaklar 4,6'dan 7,2 m'ye genişletildi ve "evler birbirine çok
    /// yakın" şikâyetinin çözüldüğü varsayıldı. Genişliği ölçtüm —
    /// kaldırım üstünde açık genişlik ortanca 7,67 m, doğru. Ama o cetvel
    /// yalnızca <b>sokağa bakan cepheler arasını</b> ölçüyor; iki komşu
    /// evin birbirinin içine girip girmediğini <b>hiç görmüyor</b>.
    ///
    /// Bu araç onu görür: her evin döndürülmüş taban dikdörtgeni alınır ve
    /// komşularıyla <b>ayrık eksen teoremiyle</b> kesişip kesişmediğine
    /// bakılır. Çıktı tek sayı: kaç ev en az bir komşusunun içinde.
    ///
    /// ## Neden ayrı bir araç
    ///
    /// <see cref="ZeminDenetimi"/> evin <i>altını</i> ölçer (havada mı,
    /// gömülü mü). Yan yana duran iki ev ikisi de zemine mükemmel
    /// otururken birbirinin duvarından geçebilir; o denetim yeşil kalır.
    /// Bir ölçü aletinin sessiz kaldığı yer, ikinci bir alet gerektiğinin
    /// işaretidir.
    /// </summary>
    public static class EvCakismasi
    {
        private const string SemtKlasoru = "Assets/_Project/Scenes/Districts";
        private const string Cikti = "../../renders/denetim";

        /// <summary>Kesişme bu kadar örtüşmeden sonra sayılır (m).</summary>
        public const float Pay = 0.15f;

        [MenuItem("Hezarfen/Olcum/Evler birbirine giriyor mu")]
        public static void Olc()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Ev çakışması");
            sb.AppendLine();
            sb.AppendLine($"Ölçüm: döndürülmüş taban dikdörtgenleri, "
                          + $"{Pay:0.00} m'den fazla örtüşme çakışma sayılır.");
            sb.AppendLine();
            sb.AppendLine("| semt | ev | çakışan ev | oran | en kötü örtüşme (m) |");
            sb.AppendLine("|---|---:|---:|---:|---:|");

            int toplamEv = 0, toplamCakisan = 0;
            float enKotuHepsi = 0f;

            foreach (string yol in Directory.GetFiles(SemtKlasoru, "*.unity"))
            {
                var sahne = EditorSceneManager.OpenScene(
                    yol.Replace("\\", "/"), OpenSceneMode.Single);
                if (!sahne.IsValid()) continue;

                var kutular = new List<Kutu>();
                foreach (var kok in sahne.GetRootGameObjects())
                    Topla(kok.transform, kutular);

                if (kutular.Count == 0)
                {
                    sb.AppendLine($"| {sahne.name} | 0 | — | — | — |");
                    continue;
                }

                // Izgara: her evi hepsiyle denemek 4.000^2 demek.
                const float H = 24f;
                var izgara = new Dictionary<(int, int), List<int>>();
                for (int i = 0; i < kutular.Count; i++)
                {
                    var k = kutular[i];
                    int x = Mathf.FloorToInt(k.merkez.x / H);
                    int z = Mathf.FloorToInt(k.merkez.y / H);
                    for (int dz = -1; dz <= 1; dz++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            var a = (x + dx, z + dz);
                            if (!izgara.TryGetValue(a, out var l))
                            { l = new List<int>(); izgara[a] = l; }
                            l.Add(i);
                        }
                }

                var cakisan = new HashSet<int>();
                float enKotu = 0f;
                var bakildi = new HashSet<(int, int)>();
                foreach (var hucre in izgara.Values)
                    for (int a = 0; a < hucre.Count; a++)
                        for (int b = a + 1; b < hucre.Count; b++)
                        {
                            int i = hucre[a], j = hucre[b];
                            if (i > j) (i, j) = (j, i);
                            if (!bakildi.Add((i, j))) continue;
                            float ort = Ortusme(kutular[i], kutular[j]);
                            if (ort <= Pay) continue;
                            cakisan.Add(i); cakisan.Add(j);
                            if (ort > enKotu) enKotu = ort;
                        }

                toplamEv += kutular.Count;
                toplamCakisan += cakisan.Count;
                if (enKotu > enKotuHepsi) enKotuHepsi = enKotu;
                sb.AppendLine($"| {sahne.name} | {kutular.Count} | {cakisan.Count} "
                              + $"| %{100f * cakisan.Count / kutular.Count:0.0} "
                              + $"| {enKotu:0.00} |");
            }

            sb.AppendLine();
            if (_katalogsuz > 0)
                sb.AppendLine($"> Uyarı: {_katalogsuz} ev kataloğda yok, "
                              + "ortalama ölçüyle sayıldı.");
            sb.AppendLine($"**TOPLAM: {toplamEv} ev, {toplamCakisan} tanesi bir "
                          + $"komşusunun içinde (%{(toplamEv == 0 ? 0f : 100f * toplamCakisan / toplamEv):0.0}), "
                          + $"en kötü örtüşme {enKotuHepsi:0.00} m.**");

            Directory.CreateDirectory(Cikti);
            File.WriteAllText($"{Cikti}/ev_cakismasi.md", sb.ToString());
            Debug.Log($"[Hezarfen] Ev cakismasi: {toplamEv} ev, {toplamCakisan} "
                      + $"cakisan (%{(toplamEv == 0 ? 0f : 100f * toplamCakisan / toplamEv):0.0}), "
                      + $"en kotu {enKotuHepsi:0.00} m -> {Cikti}/ev_cakismasi.md");
        }

        /// <summary>Sayı testten okunabilsin diye.</summary>
        public static (int ev, int cakisan, float enKotu) SemtiOlc(string sahneYolu)
        {
            var sahne = EditorSceneManager.OpenScene(sahneYolu, OpenSceneMode.Single);
            var kutular = new List<Kutu>();
            foreach (var kok in sahne.GetRootGameObjects())
                Topla(kok.transform, kutular);
            var cakisan = new HashSet<int>();
            float enKotu = 0f;
            for (int i = 0; i < kutular.Count; i++)
                for (int j = i + 1; j < kutular.Count; j++)
                {
                    if ((kutular[i].merkez - kutular[j].merkez).sqrMagnitude > 900f)
                        continue;
                    float ort = Ortusme(kutular[i], kutular[j]);
                    if (ort <= Pay) continue;
                    cakisan.Add(i); cakisan.Add(j);
                    if (ort > enKotu) enKotu = ort;
                }
            return (kutular.Count, cakisan.Count, enKotu);
        }

        private struct Kutu
        {
            public Vector2 merkez;     // XZ
            public Vector2 yari;       // yerel yarı ölçüler
            public Vector2 eksenX;     // dünyada yerel +X
            public Vector2 eksenZ;
        }

        /// <summary>
        /// <b>Ölçülen şey DUVAR, saçak değil.</b>
        ///
        /// İlk yazımda kutu <c>MeshRenderer.localBounds</c>'tan geliyordu
        /// ve sonuç %64,2 çıktı — yani neredeyse bütün şehir kusurlu.
        /// Sayı doğruydu, <b>sorduğu soru</b> yanlıştı: renderer sınırı
        /// saçağı ve cumbayı da kapsar, oysa KURAL 6 saçağın komşunun
        /// üstüne taşmasını <b>ister</b>. Bitişik nizam bir Osmanlı
        /// mahallesinde saçaklar birbirine girer; duvarlar girmez.
        ///
        /// Bu yüzden ölçü kataloğun <c>wall_width</c> ve
        /// <c>wall_depth</c>'inden okunuyor. Katalogda olmayan bir ev
        /// <b>atlanmaz, sayılır</b> — sessizce kaybolan örneklem, bu
        /// projede zaten dört kez kör nokta üretti.
        /// </summary>
        private static void Topla(Transform t, List<Kutu> hedef)
        {
            // YALNIZ EV. Kaldırım, kaide, duvar, mescit değil: bunlar
            // birbirine değmek ZORUNDA ve çakışma sayılmamalı.
            if (t.name.StartsWith("PF_House"))
            {
                var olcu = DuvarOlcusu(t.name);
                hedef.Add(new Kutu
                {
                    merkez = new Vector2(t.position.x, t.position.z),
                    yari = olcu * 0.5f,
                    eksenX = new Vector2(t.right.x, t.right.z).normalized,
                    eksenZ = new Vector2(t.forward.x, t.forward.z).normalized,
                });
                return;                      // evin çocukları ayrı sayılmaz
            }
            for (int i = 0; i < t.childCount; i++) Topla(t.GetChild(i), hedef);
        }

        private static Dictionary<string, Vector2> _olculer;
        private static int _katalogsuz;

        /// <summary>Kataloğun duvar ölçüsü (genişlik, derinlik).</summary>
        private static Vector2 DuvarOlcusu(string prefabAdi)
        {
            if (_olculer == null)
            {
                _olculer = new Dictionary<string, Vector2>();
                string yol = Path.Combine(Pipeline.AssetCatalog.RepoRoot,
                                          "art/blend/variants/catalog.json");
                if (File.Exists(yol))
                {
                    var kat = JsonUtility.FromJson<Katalog>(File.ReadAllText(yol));
                    if (kat?.variants != null)
                        foreach (var v in kat.variants)
                            if (!string.IsNullOrEmpty(v.prefab))
                                _olculer[v.prefab] =
                                    new Vector2(v.wall_width, v.wall_depth);
                }
            }
            // Ad "PF_House_A_Dar (3)" gibi olabilir.
            string ad = prefabAdi;
            int bosluk = ad.IndexOf(' ');
            if (bosluk > 0) ad = ad.Substring(0, bosluk);
            if (_olculer.TryGetValue(ad, out var o)) return o;
            _katalogsuz++;
            return new Vector2(5.6f, 6.0f);        // katalog ortalaması
        }

        [System.Serializable] private class Katalog { public Varyant[] variants; }
        [System.Serializable] private class Varyant
        {
            public string prefab;
            public float wall_width;
            public float wall_depth;
        }

        /// <summary>
        /// İki döndürülmüş dikdörtgenin örtüşme derinliği (m); 0 = ayrık.
        ///
        /// Ayrık eksen teoremi: dört eksenin (iki kutunun kendi X/Z'leri)
        /// birinde ayrıksalar ayrıktırlar. Örtüşme, eksenler arasındaki
        /// <b>en küçük</b> girişimdir.
        /// </summary>
        private static float Ortusme(Kutu a, Kutu b)
        {
            float enAz = float.MaxValue;
            var eksenler = new[] { a.eksenX, a.eksenZ, b.eksenX, b.eksenZ };
            Vector2 d = b.merkez - a.merkez;
            foreach (var e in eksenler)
            {
                float ra = Mathf.Abs(Vector2.Dot(a.eksenX, e)) * a.yari.x
                         + Mathf.Abs(Vector2.Dot(a.eksenZ, e)) * a.yari.y;
                float rb = Mathf.Abs(Vector2.Dot(b.eksenX, e)) * b.yari.x
                         + Mathf.Abs(Vector2.Dot(b.eksenZ, e)) * b.yari.y;
                float mesafe = Mathf.Abs(Vector2.Dot(d, e));
                float giris = ra + rb - mesafe;
                if (giris <= 0f) return 0f;          // ayrık
                if (giris < enAz) enAz = giris;
            }
            return enAz;
        }
    }
}
