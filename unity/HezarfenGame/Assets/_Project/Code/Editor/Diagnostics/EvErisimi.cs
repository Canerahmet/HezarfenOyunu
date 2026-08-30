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
        public const int Ornek = 300;

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
            var basarisiz = new List<string>();

            for (int i = 0; i < evler.Count; i += adim)
            {
                var ev = evler[i];
                var col = ev.GetComponentInChildren<Collider>();
                if (col == null) { colliderYok++; continue; }
                denenen++;
                if (Girilebilir(ev, col.bounds)) giren++;
                else if (basarisiz.Count < 8)
                    basarisiz.Add($"{ev.name} @ {ev.position:F0}");
            }

            var sb = new StringBuilder($"EV ERISIMI {semt}\n");
            sb.AppendLine($"  {evler.Count} ev, {denenen} ornek denendi");
            sb.AppendLine($"  GIRILEBILEN: {giren} "
                          + $"(%{(denenen == 0 ? 0f : 100f * giren / denenen):0.0})");
            if (colliderYok > 0)
                sb.AppendLine($"  collider'i olmayan: {colliderYok}");
            foreach (var b in basarisiz) sb.AppendLine($"  girilemedi: {b}");
            Debug.Log("[Hezarfen] " + sb);

            EditorSceneManager.CloseScene(sahne, true);
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
