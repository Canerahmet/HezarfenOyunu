using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Şehir bütçesi</b> — bir bakıştan ekrana kaç üçgen düştüğünü sayar.
    ///
    /// ## Neden FPS değil de üçgen
    ///
    /// Faz 4'ün kabul kriteri *"kule tepesinden 360° bakışta FPS hedefi
    /// tutuyor"* diyor ve bütçeyi sayıyla veriyor: ekranda ≤ ~2,5 M üçgen,
    /// ≤ ~1500 draw call. FPS ölçmek cazip ama **tekrarlanabilir değildir**:
    /// editörün kendi yükü, arka plandaki içe aktarım, pencere boyutu ve o an
    /// çalışan başka ne varsa hepsi sayıya karışır. Bu proje bir kez
    /// "gördüğüm kusuru ölçmeden düzeltme" dersini aldı; buradaki karşılığı
    /// **üçgeni saymak**, kare süresini değil.
    ///
    /// Sayılan şey analitiktir ve deterministiktir: her <c>LODGroup</c> için
    /// kameranın uzaklığına göre HANGİ kademenin etkin olacağı Unity'nin
    /// kendi formülüyle hesaplanır, sonra o kademenin mesh'leri frustum
    /// içindeyse üçgenleri toplanır. Aynı sahne + aynı bakış = aynı sayı.
    ///
    /// ## Formül
    ///
    /// Unity'nin LOD seçimi ekran yüksekliği oranıdır:
    /// <c>h = boy / (uzaklık · 2·tan(FOV/2) · lodBias)</c>. Kademe, <c>h</c>
    /// kendi eşiğinin üstünde kalan ilk kademedir. Aynı formül
    /// <c>ImportLanding.SetLodThresholds</c>'ta merdiveni mesafeye çevirirken
    /// de kullanılıyor — iki yerde iki ayrı formül bir gün ayrışırdı.
    /// </summary>
    public static class CityBudget
    {
        /// <summary>Faz 4 bütçesi (plan Bölüm 9).</summary>
        public const int TriangleBudget = 2_500_000;

        /// <summary>Galata Kulesi'nin tepesi — dünya orijini, kâgir gövde 34,5 m.</summary>
        public static readonly Vector3 TowerTop = new Vector3(0f, 52f, 0f);

        public struct Result
        {
            public long triangles;
            public int renderers;
            public int lodGroups;
            public float fov;
            public int directions;
            public float worstPitch;
            public float worstYaw;
        }

        /// <summary>
        /// Ölçülen eğimler. <b>Yalnız yatay bakmak yanılttı</b>: kule
        /// tepesindeki oyuncu şehre yukarıdan bakar, ufka değil. Yatay bir
        /// 40°'lik koni şehrin çoğunu koninin ALTINDA bırakıyor ve bütçeyi
        /// olduğundan küçük gösteriyordu (173 bin üçgen — inandırıcı
        /// olmayacak kadar az). En pahalı kare hangi eğimdeyse bütçeyi o
        /// belirler.
        /// </summary>
        //
        // Değerler AŞAĞI bakma derecesidir. Unity'de
        // `Quaternion.Euler(x, y, z)` pozitif x ile ileri yönü AŞAĞI
        // çevirir; negatif yazmak kamerayı gökyüzüne bakdırırdı ve
        // ölçüm yine yanıltıcı çıkardı.
        static readonly float[] Pitches = { 0f, 12f, 25f, 40f, 60f };

        [MenuItem("Hezarfen/Olcum/Sehir butcesini olc (kule tepesi 360)")]
        public static void MeasureMenu()
        {
            var r = Measure(TowerTop, 40f, 8);
            string durum = r.triangles <= TriangleBudget ? "TUTUYOR" : "ASIYOR";
            Debug.Log($"[Hezarfen] Kule tepesi 360° ({r.directions} yon x "
                      + $"{Pitches.Length} egim, FOV {r.fov:0}°): en kotu kare "
                      + $"yaw {r.worstYaw:0}° / {r.worstPitch:0}° asagi -> "
                      + $"{r.triangles:N0} ucgen, {r.renderers} renderer "
                      + $"({r.lodGroups} LODGroup tarandi). "
                      + $"Butce {TriangleBudget:N0} — {durum}.");
        }

        /// <summary>
        /// <paramref name="dirs"/> yöne bakıp <b>en kötü</b> yönün üçgen
        /// sayısını döndürür. 360° bir bakış değil, bir taramadır: oyuncu
        /// döner ve en pahalı kare hangisiyse bütçeyi o belirler.
        /// </summary>
        public static Result Measure(Vector3 eye, float fov, int dirs)
        {
            var gruplar = new List<LODGroup>();
            var tekil = new List<Renderer>();
            Topla(gruplar, tekil);

            float k = 2f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float bias = QualitySettings.lodBias;

            long enKotu = 0;
            int enKotuRend = 0;
            float enKotuPitch = 0f, enKotuYaw = 0f;
            for (int d = 0; d < dirs * Pitches.Length; d++)
            {
                float a = 360f * (d % dirs) / dirs;
                float pitch = Pitches[d / dirs];
                var rot = Quaternion.Euler(pitch, a, 0f);   // + = asagi
                var m = Matrix4x4.Perspective(fov, 16f / 9f, 1f, 20000f)
                        * Matrix4x4.TRS(eye, rot, Vector3.one).inverse;
                var duzlem = GeometryUtility.CalculateFrustumPlanes(m);

                long tri = 0;
                int rn = 0;
                foreach (var g in gruplar)
                {
                    var lods = g.GetLODs();
                    if (lods.Length == 0) continue;
                    float boy = g.size * Mathf.Max(g.transform.lossyScale.x,
                                                   g.transform.lossyScale.y,
                                                   g.transform.lossyScale.z);
                    float uz = Vector3.Distance(
                        eye, g.transform.TransformPoint(g.localReferencePoint));
                    if (uz < 0.01f) uz = 0.01f;
                    float h = boy / (uz * k) * bias;

                    int sec = -1;
                    for (int i = 0; i < lods.Length; i++)
                        if (h >= lods[i].screenRelativeTransitionHeight) { sec = i; break; }
                    if (sec < 0) continue;                 // kulldu

                    foreach (var rend in lods[sec].renderers)
                    {
                        if (rend == null || !rend.enabled) continue;
                        if (!GeometryUtility.TestPlanesAABB(duzlem, rend.bounds))
                            continue;
                        tri += Ucgen(rend);
                        rn++;
                    }
                }
                foreach (var rend in tekil)
                {
                    if (rend == null || !rend.enabled) continue;
                    if (!GeometryUtility.TestPlanesAABB(duzlem, rend.bounds)) continue;
                    tri += Ucgen(rend);
                    rn++;
                }
                if (tri > enKotu)
                {
                    enKotu = tri; enKotuRend = rn;
                    enKotuPitch = pitch; enKotuYaw = a;
                }
            }

            return new Result
            {
                triangles = enKotu,
                renderers = enKotuRend,
                lodGroups = gruplar.Count,
                fov = fov,
                directions = dirs,
                worstPitch = enKotuPitch,
                worstYaw = enKotuYaw,
            };
        }

        /// <summary>
        /// Açık BÜTÜN sahnelerdeki renderer'lar. Semtler kendi sahnelerinde
        /// durduğu için tek sahneye bakmak şehri göremezdi.
        /// </summary>
        static void Topla(List<LODGroup> gruplar, List<Renderer> tekil)
        {
            var lodIcinde = new HashSet<Renderer>();
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var sc = EditorSceneManager.GetSceneAt(i);
                if (!sc.isLoaded) continue;
                foreach (var kok in sc.GetRootGameObjects())
                {
                    foreach (var g in kok.GetComponentsInChildren<LODGroup>(false))
                    {
                        gruplar.Add(g);
                        foreach (var l in g.GetLODs())
                            foreach (var r in l.renderers)
                                if (r != null) lodIcinde.Add(r);
                    }
                }
            }
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var sc = EditorSceneManager.GetSceneAt(i);
                if (!sc.isLoaded) continue;
                foreach (var kok in sc.GetRootGameObjects())
                    foreach (var r in kok.GetComponentsInChildren<Renderer>(false))
                        if (!lodIcinde.Contains(r)) tekil.Add(r);
            }
        }

        static long Ucgen(Renderer r)
        {
            var mf = r.GetComponent<MeshFilter>();
            var mesh = mf != null ? mf.sharedMesh : null;
            if (mesh == null) return 0;
            long t = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                t += mesh.GetIndexCount(i) / 3;
            return t;
        }
    }
}
