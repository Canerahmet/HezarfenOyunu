using System.Linq;
using System.Text;
using Hezarfen.Flight;
using Hezarfen.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Player
{
    /// <summary>
    /// <b>Faz 5'in kabul ölçütünü sahneye kurar.</b>
    ///
    /// *"Kule tepesinde kuşanma → atlayış → süzülüş → Doğancılar inişi,
    /// kesintisiz animasyonlarla oynanabiliyor."*
    ///
    /// `WalkSpawner` gezgini kurar — kamera bir gözdür, karakter yoktur.
    /// Bu araç **oyunu** kurar: gövde, kanat fiziği, animasyon grafiği,
    /// iki kameralı üçüncü şahıs takımı ve ikisini birleştiren durum
    /// makinesi.
    ///
    /// Neden bir Editor aracı, neden sahneye elle konmuş bir prefab
    /// değil: elle kurulmuş bir oyuncu, kurulumu bir yerde yazılı
    /// olmayan bir oyuncudur. Bu araç kurar, **ölçer** ve ne kurduğunu
    /// söyler.
    /// </summary>
    public static class HezarfenSpawner
    {
        private const string PrefabYol =
            "Assets/_Project/Art/Prefabs/PF_Hezarfen_Ucus.prefab";
        private const string ControllerYol =
            "Assets/_Project/Art/Animation/AC_Hezarfen.controller";
        private const string KokAd = "OYUNCU_Hezarfen";

        [MenuItem("Hezarfen/Ucus/Oyuncuyu kule tepesine kur")]
        public static void KuleyeKur() => Kur(true);

        [MenuItem("Hezarfen/Ucus/Oyuncuyu mahalleye kur")]
        public static void MahalleyeKur() => Kur(false);

        private static void Kur(bool kule)
        {
            var eski = GameObject.Find(KokAd);
            if (eski != null) Object.DestroyImmediate(eski);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabYol);
            if (prefab == null)
            {
                Debug.LogError($"[Hezarfen] {PrefabYol} yok. Once: "
                    + "gen_hezarfen.py -- --export, sonra Karakteri yerlestir.");
                return;
            }

            if (!Yer(kule, out Vector3 poz, out string nere, out Vector3 bak))
            {
                Debug.LogError("[Hezarfen] Baslangic noktasi bulunamadi "
                               + "(kule ya da mahalle sahnede yok).");
                return;
            }

            var kok = new GameObject(KokAd);
            kok.transform.position = poz;
            var duz = new Vector3(bak.x - poz.x, 0f, bak.z - poz.z);
            if (duz.sqrMagnitude > 0.01f)
                kok.transform.rotation = Quaternion.LookRotation(duz.normalized);

            var govde = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            govde.transform.SetParent(kok.transform, false);
            govde.name = "Govde";

            var sb = new StringBuilder("OYUNCU KURULDU");
            sb.AppendLine($"  yer: {nere} @ {poz.ToString("F1")}");

            // --- FIZIK -------------------------------------------------------
            // Kapsul karakter modelinin BOYUYLA ayni (1,70 m). Sarigin
            // 9 cm'i kapsule girmez: insan sapkasiyla carpismaz.
            var cc = kok.AddComponent<CharacterController>();
            cc.height = 1.70f;
            cc.radius = 0.30f;
            cc.center = new Vector3(0f, 0.85f, 0f);
            cc.stepOffset = 0.45f;
            cc.slopeLimit = 55f;
            cc.skinWidth = 0.03f;

            var rb = kok.AddComponent<Rigidbody>();
            rb.mass = 78f;                 // pilot + aygit
            rb.isKinematic = true;         // yerde baslar
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // --- KONTROL -----------------------------------------------------
            var wc = kok.AddComponent<WalkController>();
            var glide = kok.AddComponent<GlideController>();
            glide.enabled = false;         // havada acilir
            var launch = kok.AddComponent<FlightLaunch>();
            launch.launchOnStart = false;  // diziyi UcusDizisi yonetir

            // --- ANIMASYON ---------------------------------------------------
            var anim = govde.GetComponentInChildren<Animator>();
            if (anim == null) anim = govde.AddComponent<Animator>();
            var ac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                ControllerYol);
            if (ac == null)
                sb.AppendLine("  UYARI: Animator kontrolcusu yok — once "
                              + "'Animator kontrolcusunu uret'.");
            else anim.runtimeAnimatorController = ac;
            anim.applyRootMotion = false;  // yer degistirmeyi kontrolcu yazar

            var ha = kok.AddComponent<HezarfenAnimator>();
            ha.animator = anim;
            ha.karakterKontrol = cc;
            ha.suzulme = glide;

            // --- KAMERA ------------------------------------------------------
            var hedef = new GameObject("KameraHedef");
            hedef.transform.SetParent(kok.transform, false);
            hedef.transform.localPosition = new Vector3(0f, 1.45f, 0f);

            var kam = KameraKur(kok, hedef.transform, out var omuz,
                                out var genis);
            var uk = kok.AddComponent<UcusKamerasi>();
            uk.omuz = omuz;
            uk.genis = genis;
            uk.suzulme = glide;

            // --- DIZI --------------------------------------------------------
            var dizi = kok.AddComponent<UcusDizisi>();
            dizi.animasyon = ha;
            dizi.yurume = wc;
            dizi.kapsul = cc;
            dizi.govde = rb;
            dizi.suzulme = glide;
            dizi.firlatma = launch;

            // Sureleri KLIPLERDEN oku: elle yazilan bir sure, klip
            // degisince sessizce yanlis olur ve oyuncu animasyon bitmeden
            // atlar — kanat sirtta gorunurken ucar.
            dizi.kusanmaSuresi = KlipSuresi(ac, "Kusanma", 2.5f);
            dizi.inisSuresi = KlipSuresi(ac, "Inis", 1.5f);
            sb.AppendLine($"  sureler kliplerden: kusanma "
                          + $"{dizi.kusanmaSuresi:0.00} s, inis "
                          + $"{dizi.inisSuresi:0.00} s");

            // Baska kameralar kapatilir; acik kalirsa hangisinin
            // ciziyor oldugu bakisla anlasilmaz.
            int off = 0;
            foreach (var c in Object.FindObjectsByType<Camera>(
                         FindObjectsInactive.Exclude))
                if (c != kam && c.enabled) { c.enabled = false; off++; }
            if (off > 0) sb.AppendLine($"  {off} baska kamera kapatildi");

            sb.AppendLine($"  {ac?.animationClips?.Length ?? 0} klip bagli");
            sb.AppendLine("  E = kusan, Space = atla");
            Selection.activeGameObject = kok;
            Debug.Log("[Hezarfen] " + sb);
        }

        private static Camera KameraKur(GameObject kok, Transform hedef,
                                        out CinemachineCamera omuz,
                                        out CinemachineCamera genis)
        {
            var kamGo = new GameObject("Kamera");
            var kam = kamGo.AddComponent<Camera>();
            kam.nearClipPlane = 0.08f;
            kam.farClipPlane = 6000f;      // Bogaz'in obur yakasi gorunmeli
            kamGo.AddComponent<CinemachineBrain>();
            kamGo.tag = "MainCamera";

            omuz = SanalKamera("VC_Omuz", hedef,
                               new Vector3(0.42f, 0.18f, -1.65f), 20);
            genis = SanalKamera("VC_Genis", hedef,
                                new Vector3(0f, 3.2f, -8.5f), 5);
            omuz.transform.SetParent(kok.transform, false);
            genis.transform.SetParent(kok.transform, false);
            return kam;
        }

        private static CinemachineCamera SanalKamera(string ad, Transform hedef,
                                                     Vector3 ofset, int oncelik)
        {
            var go = new GameObject(ad);
            var vc = go.AddComponent<CinemachineCamera>();
            vc.Follow = hedef;
            vc.LookAt = hedef;
            vc.Priority = oncelik;
            var f = go.AddComponent<CinemachineFollow>();
            f.FollowOffset = ofset;
            // Omuzda daha siki takip; genis kamerada gecikme manzarayi
            // sakinlestirir.
            float gecikme = ofset.magnitude > 4f ? 0.45f : 0.10f;
            f.TrackerSettings.PositionDamping = Vector3.one * gecikme;
            go.AddComponent<CinemachineRotationComposer>();
            return vc;
        }

        private static float KlipSuresi(RuntimeAnimatorController ac,
                                        string ad, float varsayilan)
        {
            if (ac?.animationClips == null) return varsayilan;
            foreach (var c in ac.animationClips)
                if (c != null && c.name == ad) return c.length;
            return varsayilan;
        }

        private static bool Yer(bool kule, out Vector3 poz, out string nere,
                                out Vector3 bak)
        {
            poz = Vector3.zero; nere = ""; bak = Vector3.zero;
            var kokler = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Exclude);

            if (kule)
            {
                var t = kokler.FirstOrDefault(
                    x => x.name.StartsWith("PF_GalataKulesi"));
                if (t == null) return false;
                // Kule tepesi: serefe kotu. `ThermalFlightSim` ile AYNI
                // sayi (+46 m) — iki yerde iki farkli tepe olmasi, ucus
                // sinaviyla oyunun ayni ucustan bahsetmemesi demekti.
                poz = t.position + Vector3.up * 46f;
                nere = "Galata kulesi serefesi";
                // Dogancilar'a bakar: ucusun hedefi.
                var hedef = kokler.FirstOrDefault(
                    x => x.name.Contains("Dogancilar"));
                bak = hedef != null ? hedef.position
                                    : poz + new Vector3(3215f, 0f, -643f);
                return true;
            }

            var kapi = kokler.FirstOrDefault(
                x => x.name.StartsWith("PF_AvluKapi"));
            if (kapi != null)
            {
                poz = kapi.position + kapi.forward * 6f;
                nere = "mescit avlu kapisinin onu";
                bak = kapi.position;
                return true;
            }
            var kulet = kokler.FirstOrDefault(
                x => x.name.StartsWith("PF_GalataKulesi"));
            if (kulet == null) return false;
            poz = kulet.position + kulet.forward * 12f;
            nere = "kule dibi";
            bak = kulet.position;
            return true;
        }
    }
}
