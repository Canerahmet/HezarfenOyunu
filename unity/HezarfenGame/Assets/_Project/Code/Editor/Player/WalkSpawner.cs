using Hezarfen.Editor.Diagnostics;
using Hezarfen.Editor.Gis;
using Hezarfen.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Editor.PlayerTools
{
    /// <summary>
    /// Gezinti karakterini sahneye koyar — <b>elle kurulan hiçbir şey kalmasın
    /// diye</b> (CLAUDE.md: "sadece sohbette var olan varlık yasak").
    ///
    /// Karakteri elle sürükleyip bırakmak da olurdu; ama o zaman sahne bir
    /// daha kurulduğunda kaybolurdu ve "nereye koymuştuk" sorusu her seferinde
    /// yeniden sorulurdu. Menüden konuyor, başlangıç noktası **mescitten**
    /// türüyor ve zemin ışınla bulunuyor.
    /// </summary>
    public static class WalkSpawner
    {
        public const string RootName = "GEZGIN";

        [MenuItem("Hezarfen/Gezinti/Mahalleye gezgin koy ve ac")]
        public static void SpawnInMahalle()
        {
            var scene = EditorSceneManager.OpenScene(OttomanStreetBuilder.ScenePath,
                                                     OpenSceneMode.Single);
            var go = Spawn(out string report);
            if (go == null) { Debug.LogError("[Hezarfen] " + report); return; }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = go;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log($"[Hezarfen] {report}\n"
                      + "OYNAT'a bas (Ctrl+P). Tuslar: WASD yuru · fare bak · "
                      + "Shift kos · Space zipla · F UCUS KIPI (damlari gormek "
                      + "icin) · Esc imleci birak.");
        }

        [MenuItem("Hezarfen/Gezinti/Kule dibine gezgin koy ve ac")]
        public static void SpawnAtTower()
        {
            var scene = EditorSceneManager.OpenScene(LandmarkPlacer.WorldScene,
                                                     OpenSceneMode.Single);
            var go = Spawn(out string report, preferTower: true);
            if (go == null) { Debug.LogError("[Hezarfen] " + report); return; }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = go;
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log($"[Hezarfen] {report}\nOYNAT'a bas (Ctrl+P). F = ucus kipi.");
        }

        /// <summary>
        /// Gezgini kurar ve <b>ölçülen</b> bir zemine oturtur.
        ///
        /// Başlangıç noktası elle yazılmıyor: mahallede mescidin avlu
        /// kapısının önü, dünyada kulenin dibi. İkisi de sahne yeniden
        /// kurulduğunda yerinde kalır — sabit bir koordinat kalmazdı.
        /// </summary>
        public static GameObject Spawn(out string report, bool preferTower = false)
        {
            report = "";
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);

            if (!FindStart(preferTower, out Vector3 start, out string where,
                           out Vector3 lookAt))
            { report = "Baslangic noktasi bulunamadi (sahne bos?)."; return null; }

            var go = new GameObject(RootName);
            go.transform.position = start;
            // NEREYE BAKARAK DOGDUGU DA KURULUR. Ilk denemede yon birimdi
            // (+Z) ve gezgin dukkanin sivali duvarina bakiyordu: "oynattim,
            // beyaz bir duvar gordum". Gorulmeye gelinen sey neyse ona
            // donuk baslar — mahallede mescit kapisi, dunyada kule.
            Vector3 flat = lookAt - start; flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                go.transform.rotation = Quaternion.LookRotation(flat.normalized,
                                                               Vector3.up);

            // CharacterController: yaricap 0,30 m — 4,6 m'lik sokakta ve
            // 2,70 m'lik sur kapisinda sikismadan gecmeli.
            var cc = go.AddComponent<CharacterController>();
            cc.radius = 0.30f;
            cc.height = 1.80f;
            cc.center = new Vector3(0f, 0.90f, 0f);
            // Basamak: kaldirim rihti 0,17 m (ADR 0016) — gezgin merdiveni
            // TIRMANABILMELI, yoksa mahallenin yarisi kapali kalir.
            cc.stepOffset = 0.45f;
            cc.slopeLimit = 55f;
            cc.skinWidth = 0.03f;

            var camGo = new GameObject("Goz");
            camGo.transform.SetParent(go.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.70f, 0f);
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<HDAdditionalCameraData>();
            cam.nearClipPlane = 0.08f;      // 0,3 m yaricapta duvara burnunu sokabilsin
            cam.farClipPlane = 4000f;
            cam.fieldOfView = 60f;
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";

            go.AddComponent<WalkController>();

            // Sahnedeki OTEKI kameralari kapat: iki etkin kamera varsa
            // hangisinin gorundugu Unity'nin depth sirasina kalir ve
            // "oynattim ama hicbir sey degismedi" diye gorunur.
            int off = 0;
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c != cam && c.enabled) { c.enabled = false; off++; }

            report = $"Gezgin kuruldu: {where} @ {start.ToString("F1")}"
                   + (off > 0 ? $"; {off} baska kamera kapatildi." : ".");
            return go;
        }

        private static bool FindStart(bool preferTower, out Vector3 pos,
                                      out string where, out Vector3 lookAt)
        {
            pos = Vector3.zero; where = ""; lookAt = Vector3.zero;
            Transform anchor = null;
            Vector3 offset = Vector3.zero;

            if (!preferTower)
            {
                // Mahallede: avlu kapisinin onu — mescidi ve carsiyi bir arada
                // goren nokta (01_cekirdek karesiyle ayni yer).
                foreach (var t in Object.FindObjectsByType<Transform>(
                             FindObjectsSortMode.None))
                    if (t.name.StartsWith("PF_AvluKapi")) { anchor = t; break; }
                if (anchor != null)
                {
                    offset = anchor.forward * 6f;
                    where = "mescit avlu kapisinin onu";
                }
            }
            if (anchor == null)
            {
                foreach (var t in Object.FindObjectsByType<Transform>(
                             FindObjectsSortMode.None))
                    if (t.name.StartsWith("PF_GalataKulesi")) { anchor = t; break; }
                if (anchor != null)
                {
                    offset = anchor.forward * 22f;
                    where = "Galata Kulesi'nin onu";
                }
            }
            if (anchor == null) return false;

            // BASLANGIC NOKTASI SINANIR, TAHMIN EDILMEZ.
            //
            // Ilk yazimda nokta "kapinin 6 m onu" diye hesaplaniyordu ve
            // olcum onu `PF_Dukkan_B`'nin ICINDE buldu: karakter dukkanin
            // icinde beliriyor, fizik onu disari itiyor ve oynatinca 34 m
            // savruluyordu. Inceleme paketinde ogrenilen kuralin aynisi —
            // dar bir Osmanli mahallesinde "su kadar ileri" diye bir yer
            // yoktur, bazen orada bir dukkan vardir.
            //
            // Aday noktalar cepecevre taranir; kapsul bos VE altinda zemin
            // olan ilki kazanir.
            Physics.SyncTransforms();
            Vector3 fwd = offset.sqrMagnitude > 1e-4f
                ? offset.normalized : anchor.forward;
            float reach = offset.magnitude > 0.5f ? offset.magnitude : 6f;

            for (int ring = 0; ring < 4; ring++)
            {
                float r = reach + ring * 3.5f;
                for (int i = 0; i < 16; i++)
                {
                    // Kapinin onunden basla, sonra iki yana ac.
                    float ang = (i % 2 == 0 ? 1f : -1f) * (i / 2) * 22.5f;
                    Vector3 dir = Quaternion.Euler(0f, ang, 0f) * fwd;
                    Vector3 c = anchor.position + dir * r;
                    Vector3 g = FrameMetric.OnSurface(c);
                    if (g.y < 1f) continue;                    // suya konmaz

                    Vector3 stand = g + Vector3.up * 0.15f;
                    // Kapsul BOS mu: kendi cizicisi henuz yok, o yuzden
                    // buradaki her carpma gercek bir engeldir.
                    var hit = Physics.OverlapCapsule(stand + Vector3.up * 0.30f,
                                                     stand + Vector3.up * 1.50f,
                                                     0.32f);
                    if (hit.Length > 0) continue;
                    // Altinda gercekten zemin olmali.
                    if (!Physics.Raycast(stand + Vector3.up * 0.4f, Vector3.down,
                                         2.0f))
                        continue;

                    // GORDUGU SEYI GORMELI. Kapsulun bos olmasi yetmiyor:
                    // olcum ilk gecen adayin 2,1 m onunde bir dukkan buldu,
                    // yani gezgin mescide donuk ama duvara bakiyordu.
                    // `MahalleReview.TryEye`'daki kosulun aynisi — bir
                    // noktadan hedef GORUNUYOR mu.
                    Vector3 eye = stand + Vector3.up * 1.70f;
                    Vector3 target = anchor.position + Vector3.up * 1.6f;
                    float dist = Vector3.Distance(eye, target);
                    RaycastHit blk;
                    if (Physics.Linecast(eye, target, out blk)
                        && blk.distance < dist - 1.2f)
                        continue;

                    pos = stand;
                    lookAt = anchor.position;
                    where += $" (taranan {ring * 16 + i + 1}. aday, "
                           + $"{r:F0} m, {ang:+0;-0} derece)";
                    return true;
                }
            }
            Debug.LogWarning("[Hezarfen] Gezgin icin bos yer bulunamadi — "
                             + "cepecevre 64 aday da doluydu.");
            return false;
        }
    }
}
