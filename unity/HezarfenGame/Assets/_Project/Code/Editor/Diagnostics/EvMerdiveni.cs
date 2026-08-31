using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Hezarfen.Editor.Diagnostics
{
    /// <summary>
    /// <b>Üst kata çıkılabiliyor mu — motorun kendi tanımıyla.</b>
    ///
    /// <see cref="EvErisimi"/> bu soruyu el yapımı bir ızgarayla
    /// soruyordu ve cevabı sekiz kez değişti: %100, %97,2, %0, %9, %1,3,
    /// %3,9, %13,6, %1,6. Her seferinde kusur evde değil ölçüdeydi —
    /// hücre çok kaba, sonda döşemenin altında, dolgu kapıdan sızıyor,
    /// dikey adım basamağı görmüyor, ızgara evin açısına göre kayıyor.
    ///
    /// Bir ölçüm arka arkaya birkaç makul hipotezi reddediyorsa,
    /// şüphelenilecek şey hipotezler değil ölçümdür. Ve "bir insan
    /// buradan oraya yürüyebilir mi" sorusunun Unity'de zaten bir
    /// sahibi var: <b>NavMesh</b>. Ajan yarıçapı, boyu, adım payı ve
    /// eğim sınırı motorun kendi tanımları; merdiveni de o tanımlarla
    /// tırmanır.
    ///
    /// Burada tek bir evin kutusu kadar navmesh pişirilir
    /// (<c>NavMeshBuilder.BuildNavMeshData</c>), kapının hemen içinden
    /// üst kattaki bir noktaya yol istenir ve yolun <b>tamamlanıp</b>
    /// tamamlanmadığına bakılır. Kendi modelimi taşımıyorum artık.
    ///
    /// Ek fayda: aynı pişirme Faz II.G'deki NPC gezinmesinin de
    /// temelidir. Ölçüm aracı ile oyun aracı aynı şey olunca ikisi
    /// birbirinden ayrılamaz.
    /// </summary>
    public static class EvMerdiveni
    {
        private const string TerrainScene =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";
        private const string DistrictDir =
            "Assets/_Project/Scenes/Districts";

        /// <summary>Kaç ev örneklenecek.</summary>
        public const int Ornek = 40;

        [MenuItem("Hezarfen/Olcum/Ust kata cikilabiliyor mu (NavMesh)")]
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

            // Oyuncunun kendi olculeri — WalkController ve AyakIK ile ayni
            // insan. Ayri bir "ajan" tanimlamak, bir sayinin iki sahibi
            // olmasi demekti.
            var ayar = new NavMeshBuildSettings
            {
                agentTypeID = 0,
                agentRadius = 0.30f,
                agentHeight = 1.75f,
                agentSlope = 50f,
                agentClimb = 0.30f,      // CharacterController adim payi
                minRegionArea = 0.3f,
                overrideVoxelSize = true,
                voxelSize = 0.06f,       // basamak 0,19 m: en az uc voksel
                overrideTileSize = false,
            };

            int denenen = 0, cikilan = 0, ustYok = 0;
            var kalanlar = new List<string>();

            int adim = Mathf.Max(1, evler.Count / Ornek);
            for (int i = 0; i < evler.Count; i += adim)
            {
                var ev = evler[i];
                var col = ev.GetComponentInChildren<Collider>();
                if (col == null) continue;

                var kutu = col.bounds;
                kutu.Expand(1.5f);

                var kaynak = new List<NavMeshBuildSource>();
                NavMeshBuilder.CollectSources(
                    kutu, ~0, NavMeshCollectGeometry.PhysicsColliders, 0,
                    new List<NavMeshBuildMarkup>(), kaynak);
                if (kaynak.Count == 0) continue;

                // SINIR YEREL UZAYDA VERILIR.
                //
                // Ilk yazimda dunya kutusunu hem `localBounds` hem
                // `position` olarak gecirdim; sonuc iki kez otelenmis
                // bir navmesh oldu ve 41 evin 41'inde "kapida navmesh
                // yok" cikti. `CollectSources` dunya uzayinda toplar,
                // `BuildNavMeshData` ise sinirI verilen konuma GORE
                // bekler.
                var yerelKutu = new Bounds(Vector3.zero, kutu.size);
                var veri = NavMeshBuilder.BuildNavMeshData(
                    ayar, kaynak, yerelKutu, kutu.center, Quaternion.identity);
                if (veri == null) continue;
                var ornek = NavMesh.AddNavMeshData(veri);

                try
                {
                    denenen++;
                    // BASLANGIC ISINLA BULUNUR.
                    //
                    // Once kapinin icinde bir nokta secip 2 m yaricapla
                    // navmesh'e oturtuyordum ve nokta EVIN DISINDAKI
                    // sokaga kaciyordu: yol izi son koseyi 0,00 m'de,
                    // yani evin tabaninda gosterdi. 2 m, duvari asmaya
                    // yeten bir yaricap.
                    //
                    // Dogrusu iceriden asagi bakmak: ic noktadan asagi
                    // isin, ilk carptigi yuzey zemin katin dosemesidir.
                    Vector3 icNokta = ev.position - ev.forward * 0.9f;
                    // ISIN ZEMIN KATIN ICINDEN BASLAR.
                    //
                    // Once evin tepesinden birakiyordum ve ilk carptigi
                    // yuzey CATI oluyordu: `bas` catiya oturdu, ust kat
                    // aramasi da onun 2 m ustune bakti ve 41 evin
                    // 41'inde "ust katta navmesh yok" dedi. Oysa dogrudan
                    // olcum ust katta 32 kose gosteriyordu.
                    //
                    // 1,6 m: en yuksek subasman 0,95 m, yani bu kot her
                    // varyantta zemin katin icindedir ve tavanin altinda.
                    Vector3 tepe = icNokta + Vector3.up * 1.6f;
                    if (!Physics.Raycast(tepe, Vector3.down, out var zeminVurus,
                                         2.5f, ~0,
                                         QueryTriggerInteraction.Ignore))
                    {
                        kalanlar.Add($"{Ad(ev)}: ic zemin bulunamadi");
                        continue;
                    }
                    if (!NavMesh.SamplePosition(
                            zeminVurus.point + Vector3.up * 0.2f,
                            out var bas, 0.5f, NavMesh.AllAreas))
                    {
                        kalanlar.Add($"{Ad(ev)}: zemin katta navmesh yok "
                            + $"(kot {zeminVurus.point.y - kutu.min.y - 1.5f:0.00})");
                        continue;
                    }

                    // Ust kat: zemin dosemesinin en az 2 m ustunde bir
                    // navmesh noktasi. Kot tahmin edilmez, TARANIR.
                    Vector3? hedef = null;
                    for (float h = bas.position.y + 2.0f;
                         h < kutu.max.y && hedef == null; h += 0.3f)
                    {
                        // ARAMA EVIN TAMAMINI KAPSAR.
                        //
                        // Once merkezin +-1,5 m'sine bakiyordum ve 41
                        // evin 41'inde "ust katta navmesh yok" cikti —
                        // oysa dogrudan olcum ust katta 32 kose
                        // gosteriyordu. Ust kattaki yuzey merdiven
                        // boslugunun cevresinde, yani ARKA DUVARA yakin;
                        // merkezin cevresine bakan bir arama onu
                        // gormuyordu.
                        for (float dx = -3.0f; dx <= 3.0f && hedef == null; dx += 0.5f)
                            for (float dz = -3.0f; dz <= 3.0f && hedef == null; dz += 0.5f)
                            {
                                var p = new Vector3(ev.position.x + dx, h,
                                                    ev.position.z + dz);
                                if (NavMesh.SamplePosition(p, out var u, 0.4f,
                                                           NavMesh.AllAreas)
                                    && u.position.y > bas.position.y + 2.2f)
                                    hedef = u.position;
                            }
                    }
                    if (hedef == null) { ustYok++; continue; }

                    var yol = new NavMeshPath();
                    NavMesh.CalculatePath(bas.position, hedef.Value,
                                          NavMesh.AllAreas, yol);
                    if (yol.status == NavMeshPathStatus.PathComplete) cikilan++;
                    else if (kalanlar.Count < 6)
                    {
                        // Yol nerede duruyor: son kosenin kotu, evin
                        // tabanina gore. "Yol yok" demek bir sey
                        // ogretmez; "0,8 m'de duruyor" ogretir.
                        float son = yol.corners.Length > 0
                            ? yol.corners[yol.corners.Length - 1].y - kutu.min.y - 1.5f
                            : -99f;
                        kalanlar.Add($"{Ad(ev)}: {yol.status}, "
                            + $"{yol.corners.Length} kose, son kot {son:0.00} m, "
                            + $"hedef kot {hedef.Value.y - kutu.min.y - 1.5f:0.00} m");
                    }
                }
                finally
                {
                    NavMesh.RemoveNavMeshData(ornek);
                }
            }

            var sb = new StringBuilder($"UST KAT (NavMesh) {semt}\n");
            sb.AppendLine($"  {evler.Count} ev, {denenen} ornek pisirildi");
            sb.AppendLine($"  ust katinda navmesh olmayan: {ustYok}");
            int gecerli = denenen - ustYok;
            sb.AppendLine($"  CIKILABILEN: {cikilan}/{gecerli} "
                + $"(%{(gecerli == 0 ? 0f : 100f * cikilan / gecerli):0.0})");
            foreach (var k in kalanlar) sb.AppendLine("  " + k);
            Debug.Log("[Hezarfen] " + sb);

            EditorSceneManager.CloseScene(sahne, true);
        }

        private static string Ad(Transform t)
            => System.Text.RegularExpressions.Regex
                   .Replace(t.name, @" \(\d+\)$", "");
    }
}
