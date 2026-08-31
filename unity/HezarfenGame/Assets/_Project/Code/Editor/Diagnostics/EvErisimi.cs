using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Eve gerçekten girilebiliyor mu.</b>
    ///
    /// Kapı `near` kipinde aylardır gerçek bir açıklıktı: duvar delinmiş,
    /// sövesi ve nişi var, eşiği taş. Ama çarpışma kütlesi <b>tek bir dolu
    /// kutuydu</b> — yani ev, fizik için katı bir bloktu ve o kapıdan
    /// kimse geçemiyordu. Görünenle olan arasındaki bu ayrım hiçbir testin
    /// sorusu değildi.
    ///
    /// ## Ölçüt: oyuncunun kapsülü geçiyor mu
    ///
    /// Kapının eni ölçülüp "1,16 m, yeter" demek bir <b>beyan</b>dır.
    /// Burada yapılan şey oyuncunun gerçek kapsülüyle
    /// (<see cref="Yaricap"/>, <see cref="Boy"/>) kapının önünden içeriye
    /// bir yol aramaktır — kapsül kutusu (<c>CheckCapsule</c>) her adımda
    /// gerçekten boş mu diye sorulur.
    ///
    /// Aynı ölçütü bahçe kapılarında kullandık ve orada da beyanla gerçek
    /// ayrılmıştı: "kapı yeterince geniş" diyen 36 ışınlı bir sınav 20
    /// bahçenin hepsini açık göstermişti, taşma dolgusu 16'sını.
    /// </summary>
    public static class EvErisimi
    {
        private const string TerrainScene =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";
        private const string DistrictDir =
            "Assets/_Project/Scenes/Districts";

        /// <summary>Oyuncu kapsülünün yarıçapı (m) — `WalkController` ile aynı.</summary>
        public const float Yaricap = 0.30f;

        /// <summary>Oyuncu kapsülünün boyu (m).</summary>
        public const float Boy = 1.75f;

        /// <summary>Kaç ev örneklenecek (hepsi çok yavaş).</summary>
        public const int Ornek = 90;

        [MenuItem("Hezarfen/Olcum/Evlere girilebiliyor mu (D_Galata)")]
        public static void Galata() => Olc("D_Galata");

        public static void Olc(string semt)
        {
            EditorSceneManager.OpenScene(TerrainScene, OpenSceneMode.Single);
            var sahne = EditorSceneManager.OpenScene(
                $"{DistrictDir}/{semt}.unity", OpenSceneMode.Additive);

            var evler = new List<Transform>();
            foreach (var go in sahne.GetRootGameObjects())
                foreach (var t in go.GetComponentsInChildren<Transform>())
                    if (t.name.StartsWith("PF_House")) evler.Add(t);

            if (evler.Count == 0)
            {
                Debug.LogError($"[Hezarfen] {semt}: ev yok.");
                return;
            }

            var rng = new System.Random(1632);
            int adim = Mathf.Max(1, evler.Count / Ornek);
            int denenen = 0, giren = 0, colliderYok = 0;
            float odaToplam = 0f;
            int odaOlculen = 0;
            int ustKatVar = 0, ustKatErisilen = 0;
            var bantDagilim = new Dictionary<int, int>();
            var basarisiz = new List<string>();

            for (int i = 0; i < evler.Count; i += adim)
            {
                var ev = evler[i];
                var col = ev.GetComponentInChildren<Collider>();
                if (col == null) { colliderYok++; continue; }
                denenen++;
                if (Girilebilir(ev, col.bounds))
                {
                    giren++;
                    float oran = IcErisim(ev, col, out bool ustVar,
                                          out bool ustErisildi, out int bant);
                    if (!bantDagilim.ContainsKey(bant)) bantDagilim[bant] = 0;
                    bantDagilim[bant]++;
                    if (oran >= 0f) { odaToplam += oran; odaOlculen++; }
                    if (ustVar) { ustKatVar++; if (ustErisildi) ustKatErisilen++; }
                }
                else if (basarisiz.Count < 8)
                    basarisiz.Add($"{ev.name} @ {ev.position:F0}");
            }

            var sb = new StringBuilder($"EV ERISIMI {semt}\n");
            sb.AppendLine($"  {evler.Count} ev, {denenen} ornek denendi");
            sb.AppendLine($"  GIRILEBILEN: {giren} "
                          + $"(%{(denenen == 0 ? 0f : 100f * giren / denenen):0.0})");
            if (odaOlculen > 0)
                sb.AppendLine($"  ic hacmin erisilen orani: "
                    + $"%{100f * odaToplam / odaOlculen:0.0} "
                    + $"({odaOlculen} evde olculdu)");
            if (ustKatVar > 0)
                sb.AppendLine($"  ust kata cikilabilen: {ustKatErisilen}/{ustKatVar} "
                    + $"(%{100f * ustKatErisilen / ustKatVar:0.0})");
            foreach (var kv in bantDagilim)
                sb.AppendLine($"  {kv.Key} kat bandi: {kv.Value} ev");
            if (colliderYok > 0)
                sb.AppendLine($"  collider'i olmayan: {colliderYok}");
            foreach (var b in basarisiz) sb.AppendLine($"  girilemedi: {b}");
            Debug.Log("[Hezarfen] " + sb);

            EditorSceneManager.CloseScene(sahne, true);
        }

        /// <summary>
        /// <b>İçeride nereye yürünebiliyor — merdiven dahil.</b>
        ///
        /// Dönüş: zemin kattaki gezilebilir hücrelerin oranı.
        /// <paramref name="ustVar"/> evin üst katı varsa,
        /// <paramref name="ustErisildi"/> oraya <b>yürüyerek</b>
        /// çıkılabiliyorsa doğrudur.
        ///
        /// ## Neden iki katmanlı model işe yaramadı
        ///
        /// Önce iki yatay katman kurup aralarında dikey geçiş arıyordum.
        /// Ölçüm sırayla %100, %97,2, %0 ve %9 dedi — dördü de yanlıştı,
        /// çünkü model yanlıştı: <b>merdiven bir rampadır.</b> Oyuncu
        /// dikey bir şafttan geçmez, basamak basamak <b>çapraz</b>
        /// yükselir. Kesit ölçüldüğünde görüldü: merdiven boşluğu açık
        /// (döşeme hizasında 10-28 hücre boş) ama sürekli açık dikey
        /// sütun yalnız <b>bir</b> tane — çünkü boşluğun geri kalanını
        /// basamakların kendisi dolduruyor. Doğrusu bu.
        ///
        /// ## Doğru model: yürünebilirlik
        ///
        /// Üç boyutlu ızgara. Bir hücre <b>durulabilir</b> sayılır:
        /// gövde boşluğu boş ve hemen altı dolu (yani basacak bir şey
        /// var). Komşuluk, bir kat aşağı/yukarı <b>tek adım</b> farkına
        /// izin verir — basamak yüksekliği 0,22 m, ızgara 0,25 m. Bu,
        /// bir gezinme ağının (navmesh) yaptığı işin en yalın hâli ve
        /// merdiveni doğal olarak tırmanır.
        /// </summary>
        private static float IcErisim(Transform ev, Collider col,
                                      out bool ustVar, out bool ustErisildi,
                                      out int bantSayisi)
        {
            ustVar = false;
            ustErisildi = false;
            bantSayisi = 0;

            // IZGARA BASAMAGA GORE SECILIR.
            //
            // 0,35 m yatay hucre ile olcum "ust kata cikilabilen 1/112"
            // dedi. Sebep basamak genisligiydi: tread 0,26 m, yani
            // 0,35 m'lik iki komsu hucre arasindaki kot farki
            // 0,35/0,26 x 0,22 = 0,30 m'ye ciiyor ve tek dikey adim
            // (0,25 m) yetmiyor. Merdiven yururken sorunsuz —
            // CharacterController'in adim payi 0,30 m — ama IZGARA
            // tirmanamiyordu.
            //
            // Yatay hucre basamak genisliginden KUCUK olmali; o zaman
            // ardisik hucreler arasinda en cok bir basamak vardir.
            //
            // 0,25 m yeterli degildi (3/60): tread 0,26 ile neredeyse
            // esit oldugu icin ardisik basamaklarin hucreleri bazen IKI
            // hucre otede kaliyor ve dort komsuluk yetismiyordu. Kesit
            // merdiven zincirini gosteriyordu — k=3'ten k=11'e her katta
            // 1-3 durulabilir hucre — yani basamaklar duruyordu, kopan
            // sey zincirdi. 0,15 m ile her basamaga en az bir hucre
            // dusuyor ve zincir kapaniyor.
            const float H = 0.15f;      // yatay hucre (m) << tread 0,26
            const float DY = 0.22f;     // dikey hucre (m) = basamak

            var mc = col as MeshCollider;
            Bounds yerel = mc != null && mc.sharedMesh != null
                ? mc.sharedMesh.bounds
                : new Bounds(Vector3.zero, Vector3.one * 1e6f);
            // IC SINIR PAYI 0,05 m — ve sebebi olculdu.
            //
            // 0,35 m ile zincir tam olarak SU noktada kopuyordu:
            // zemin kat k=2 (y=0,69), ilk merdiven hucresi k=4 (y=1,13),
            // arada k=3 BOS. Cunku merdivenin ilk basamagi yan duvarin
            // 0,13 m icinde ve 0,35'lik pay onu disarida birakiyordu.
            // Yani ölçüm merdivenin ilk basamagini hic gormuyordu.
            //
            // Pay'in isi disarisini elemekti; onu zaten fizik yapiyor —
            // duvarin icindeki hucrede kapsul duvara carpar. Pay yalniz
            // ev sinirini isaretler, ic hacmi kirpmaz.
            const float Pay = 0.05f;

            bool Iceride(Vector3 dunya)
            {
                Vector3 l = ev.InverseTransformPoint(dunya);
                return l.x > yerel.min.x + Pay && l.x < yerel.max.x - Pay
                    && l.z > yerel.min.z + Pay && l.z < yerel.max.z - Pay;
            }

            Bounds kutu = col.bounds;
            var mn = new Vector2(kutu.min.x + 0.3f, kutu.min.z + 0.3f);
            int en = Mathf.Max(2, Mathf.FloorToInt((kutu.size.x - 0.6f) / H));
            int boy = Mathf.Max(2, Mathf.FloorToInt((kutu.size.z - 0.6f) / H));
            int kat = Mathf.Max(2, Mathf.FloorToInt((kutu.size.y - 0.5f) / DY));
            if (en * boy * kat > 200000) return -1f;

            // DURULABILIR: govde boslugu bos VE altinda basacak bir sey.
            var dur = new bool[en * boy * kat];
            int durSayi = 0;
            for (int k = 0; k < kat; k++)
            {
                float y = kutu.min.y + 0.25f + k * DY;
                for (int j = 0; j < boy; j++)
                    for (int i = 0; i < en; i++)
                    {
                        var ayak = new Vector3(mn.x + (i + 0.5f) * H, y,
                                               mn.y + (j + 0.5f) * H);
                        if (!Iceride(ayak)) continue;
                        if (Physics.CheckCapsule(ayak + Vector3.up * 0.35f,
                                                 ayak + Vector3.up * 1.45f,
                                                 Yaricap * 0.85f, ~0,
                                                 QueryTriggerInteraction.Ignore))
                            continue;
                        if (!Physics.CheckSphere(ayak - Vector3.up * 0.10f,
                                                 0.14f, ~0,
                                                 QueryTriggerInteraction.Ignore))
                            continue;
                        dur[(k * boy + j) * en + i] = true;
                        durSayi++;
                    }
            }
            if (durSayi < 6) return -1f;

            int bant = 0;
            bool oncekiAcik = false;
            var katSayi = new int[kat];
            for (int k = 0; k < kat; k++)
            {
                int say = 0;
                for (int t = 0; t < en * boy; t++)
                    if (dur[k * en * boy + t]) say++;
                katSayi[k] = say;
                bool acik = say >= 6;
                if (acik && !oncekiAcik) bant++;
                oncekiAcik = acik;
            }
            bantSayisi = bant;
            ustVar = bant >= 2;

            Vector3 ic = ev.position - ev.forward * 0.9f;
            int si = Mathf.Clamp(Mathf.RoundToInt((ic.x - mn.x) / H - 0.5f), 0, en - 1);
            int sj = Mathf.Clamp(Mathf.RoundToInt((ic.z - mn.y) / H - 0.5f), 0, boy - 1);
            int sk = -1;
            for (int k = 0; k < kat && sk < 0; k++)
                for (int r = 0; r <= 3 && sk < 0; r++)
                    for (int dj = -r; dj <= r && sk < 0; dj++)
                        for (int di = -r; di <= r && sk < 0; di++)
                        {
                            int i2 = si + di, j2 = sj + dj;
                            if (i2 < 0 || i2 >= en || j2 < 0 || j2 >= boy) continue;
                            if (!dur[(k * boy + j2) * en + i2]) continue;
                            si = i2; sj = j2; sk = k;
                        }
            if (sk < 0) return -1f;

            int zeminKat = sk;
            int zeminSayi = 0;
            for (int t = 0; t < en * boy; t++)
                if (dur[zeminKat * en * boy + t]) zeminSayi++;
            if (zeminSayi < 4) return -1f;

            var gorulen = new bool[dur.Length];
            var yigin = new Stack<int>();
            int basla = (sk * boy + sj) * en + si;
            gorulen[basla] = true;
            yigin.Push(basla);
            int zeminErisilen = 0, enYuksekKat = sk;

            while (yigin.Count > 0)
            {
                int idx = yigin.Pop();
                int k0 = idx / (en * boy);
                int kalanIdx = idx % (en * boy);
                int j0 = kalanIdx / en, i0 = kalanIdx % en;
                if (k0 == zeminKat) zeminErisilen++;
                if (k0 > enYuksekKat) enYuksekKat = k0;

                for (int d = 0; d < 4; d++)
                {
                    int i2 = i0 + (d == 0 ? 1 : d == 1 ? -1 : 0);
                    int j2 = j0 + (d == 2 ? 1 : d == 3 ? -1 : 0);
                    if (i2 < 0 || i2 >= en || j2 < 0 || j2 >= boy) continue;
                    for (int dk = -1; dk <= 1; dk++)
                    {
                        int k2 = k0 + dk;
                        if (k2 < 0 || k2 >= kat) continue;
                        int n = (k2 * boy + j2) * en + i2;
                        if (gorulen[n] || !dur[n]) continue;
                        gorulen[n] = true;
                        yigin.Push(n);
                    }
                }
            }

            if (ustVar)
            {
                int esik = zeminKat;
                while (esik + 1 < kat && katSayi[esik + 1] >= 6) esik++;
                ustErisildi = enYuksekKat > esik + 1;
            }
            return (float)zeminErisilen / zeminSayi;
        }

        /// <summary>
        /// Evin önünden içine kapsülle bir yol var mı.
        ///
        /// Kapı cephenin ortasındadır ve cephe evin <b>−Z</b>'sine bakar
        /// (CLAUDE.md: evin önü +Z, yani sokak −Z yönünde). Kapsül dışarıda
        /// bir noktadan başlar ve evin merkezine doğru 20 cm'lik adımlarla
        /// yürütülür; herhangi bir adımda kapsül doluysa yol kapalıdır.
        /// </summary>
        private static bool Girilebilir(Transform ev, Bounds kutu)
        {
            // SOKAK +forward TARAFINDA.
            //
            // CLAUDE.md: "evin onu +Z". Yerlestirici de evi sokaga
            // BAKACAK sekilde donduruyor (`LookRotation(-nrm)`), yani
            // evin ileri yonu sokagi gosterir. Ilk yazimda `-forward`
            // aldim ve arka duvardan girmeye calistim: 332 evin
            // 332'sinde "girilemedi" cikti. Kapi acikligi collider'da
            // olculmustu (1,16 m) — yanlis olan kapi degil, kapiya
            // hangi yandan yaklastigimdi.
            Vector3 on = ev.forward;                        // sokak yonu
            float derinlik = Vector3.Scale(kutu.size, new Vector3(
                Mathf.Abs(ev.forward.x), 0f, Mathf.Abs(ev.forward.z))).magnitude;
            if (derinlik < 1f) derinlik = kutu.size.magnitude * 0.5f;

            // Kapinin esigi subasmanin ustunde: taban, evin oturdugu kot +
            // subasman. Kutu tabanindan 0,2 m yukarisi guvenli bir baslangic.
            // Zemin katin dosemesi subasmanin ustunde ve subasman
            // varyanttan varyanta 0,30-0,95 m degisiyor. Tek bir kot
            // denemek varyantlarin bir bolumunu yanlislikla "kapali"
            // gosterirdi; kot taranir.
            // OLCULEN SEY KAPI GECISI, SOKAK DEGIL.
            //
            // Ilk olcum sokaktan 1,2 m disaridan basliyordu ve %43,4
            // verdi. Ama yolun disarida kalan bolumu evin kapisi
            // hakkinda bir sey soylemiyor: kaldirim kenari, komsu bahce
            // duvari, yokusta yukselen zemin — hepsi kapsulu durdurur ve
            // hepsi ayri birer kusurdur. Bir olcut ayni anda iki sey
            // olcuyorsa hangisinin bozuk oldugunu soyleyemez.
            //
            // Burada yalniz duvarin iki yani taranir: disarida 0,9 m,
            // iceride 1,2 m. Kapinin onunu tikayan sey varsa onu
            // ZeminDenetimi olcer.
            float duvar = derinlik * 0.5f;
            // Kot taramasinin USTU subasmandan gelir. 1,90 m ile
            // durdurdugumda %92,5 cikti ve elenenlerin arasinda
            // `K_Yuksek` vardi — subasmani 0,95 m, yani dosemesi 0,95'te,
            // kapsul merkezi en az 1,83'te olmali. Tarama tam oraya
            // yetisiyordu. En yuksek subasman 0,95 + kat yuksekligi
            // payi: 2,60 m guvenli ust sinir.
            for (float yukseklik = 0.40f; yukseklik <= 2.60f; yukseklik += 0.10f)
            {
                bool acik = true;
                for (float s = duvar + 0.9f; s >= duvar - 1.2f; s -= 0.15f)
                {
                    Vector3 c = ev.position + on * s + Vector3.up * yukseklik;
                    Vector3 a = c + Vector3.up * (Yaricap - Boy * 0.5f);
                    Vector3 b = c + Vector3.up * (Boy * 0.5f - Yaricap);
                    if (Physics.CheckCapsule(a, b, Yaricap,
                                             ~0, QueryTriggerInteraction.Ignore))
                    {
                        acik = false;
                        break;
                    }
                }
                if (acik) return true;
            }
            return false;
        }
    }
}
