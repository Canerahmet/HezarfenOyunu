using System.Collections.Generic;
using System.Linq;
using Hezarfen.Arayuz;
using Hezarfen.Editor.Lighting;
using Hezarfen.Flight;
using Hezarfen.Player;
using Hezarfen.Sehir;
using Hezarfen.Streaming;
using Hezarfen.Tani;
using Hezarfen.Zaman;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// <b>Şehri OYNANABİLİR hâle getirir.</b>
    ///
    /// ## Neden bu gerekti
    ///
    /// Faz 6 ve 7 bittiğinde 372 test yeşildi ve her sistem tek tek
    /// ölçülmüştü — zaman, rutin, aranma, ekonomi, görev, replik, uçuş
    /// dizisi, ışık, performans. Ama <b>hiçbiri bir sahnede bir araya
    /// gelmemişti</b>: `Faz1_Terrain` dünyayı taşıyordu (arazi, sur,
    /// landmark, iskele, ağaç) ve içinde ne oyuncu vardı ne saat.
    /// `FlightSlice` uçuşu taşıyordu ve içinde şehir yoktu.
    ///
    /// Yani oyun, testlerin hepsi geçerken bile <b>oynanamıyordu</b>.
    /// Testler parçaları ölçtü; kimse birleştirmedi. Projenin kendi
    /// kuralı bunu zaten söylüyordu — *üretilen ama görünmeyen bir öğe,
    /// olmayan bir öğedir* — ama kural sistemlere değil varlıklara
    /// uygulanmıştı.
    ///
    /// Bu komut montajı yapar ve <b>yeniden yapılabilir</b> kılar: sahne
    /// elle kurulsaydı, bir sonraki arazi yenilemesinde sessizce dağılırdı.
    /// </summary>
    public static class OyunSahnesiKur
    {
        public const string DunyaSahnesi =
            "Assets/_Project/Scenes/Faz1_Terrain.unity";

        /// <summary>Oyuncunun doğduğu yer: Galata Kulesi'nin dibi.</summary>
        public static readonly Vector3 BaslangicNoktasi =
            new Vector3(25f, 0f, 25f);

        [MenuItem("Hezarfen/Boru Hatti/Oyun sahnesini kur")]
        public static void KurMenu()
        {
            var sahne = EditorSceneManager.OpenScene(
                DunyaSahnesi, OpenSceneMode.Single);

            var rapor = new List<string>();

            // 1) ISIK: gecici takim GITMELI (ADR 0072). Dunya sahnesinde
            //    hala duruyordu — kalici pas yalniz sandik sahnesine
            //    kurulmustu.
            KaliciAydinlatma.Kur(out string isikRapor);
            rapor.Add("Isik: kalici pas kuruldu");

            // 2) OYUNCU
            var oyuncu = Oyuncu(out string oyuncuRapor);
            rapor.Add(oyuncuRapor);

            // 3) ZAMAN — vakitler gunesten hesaplanir; gunes sahnedeki.
            var zaman = Tekil<ZamanSistemi>("ZAMAN");
            zaman.yil = 1632;
            zaman.yilinGunu = 122;          // 1 Mayis 1632 (artik yil)
            zaman.saat = 9.0f;
            zaman.gunesiSur = true;
            zaman.Yenile();
            rapor.Add($"Zaman: {zaman.yil}, {zaman.yilinGunu}. gun, "
                      + $"saat {zaman.saat:F1}");

            // 4) SEHIR: sakinler
            var graf = AssetDatabase.LoadAssetAtPath<SokakGrafi>(
                "Assets/_Project/Data/SG_Sehir.asset");
            var meslekler = AssetDatabase.FindAssets("t:NPCMeslek")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<NPCMeslek>)
                .Where(m => m != null).ToList();
            var govde = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Art/Prefabs/PF_Hezarfen_Sivil.prefab");

            var sehir = Tekil<NPCYonetici>("SEHIR_NPC");
            sehir.graf = graf;
            sehir.meslekler = meslekler;
            sehir.govdePrefab = govde;
            sehir.oyuncu = oyuncu.transform;
            sehir.zaman = zaman;
            rapor.Add($"Sehir: {(graf == null ? "GRAF YOK" : graf.dugumler.Count + " dugum")}, "
                      + $"{meslekler.Count} meslek, {sehir.sakinSayisi} sakin");

            // 5) KOLLUK
            var aranma = Tekil<AranmaSistemi>("ARANMA");
            aranma.zaman = zaman;
            aranma.sehir = sehir;
            aranma.oyuncu = oyuncu.transform;
            sehir.aranma = aranma;
            rapor.Add("Aranma: kuruldu");

            // 6) REPLIKLER — sehir konussun (Katman 2).
            var bark = Tekil<BarkGosterici>("BARK");
            bark.yonetici = sehir;
            bark.oyuncu = oyuncu.transform;
            rapor.Add("Replik: gosterici kuruldu");

            // 7) HAVA — lodos; ucusu mumkun kilan sey bu.
            var hava = Tekil<HavaProfili>("HAVA");
            hava.ruzgar = Ruzgar.Lodos;
            hava.hiz = 8f;
            hava.gokVolume = Object.FindAnyObjectByType<UnityEngine.Rendering.Volume>();
            hava.Uygula();
            rapor.Add("Hava: lodos 8 m/s");

            // 8) SEMT AKISI — sehir oyuncunun etrafinda yuklenir.
            var semtKayit = AssetDatabase.LoadAssetAtPath<DistrictRegistry>(
                "Assets/_Project/Data/DistrictDefs/DistrictRegistry.asset");
            var akis = Tekil<DistrictStreamer>("SEMT_AKISI");
            akis.registry = semtKayit;
            akis.viewer = oyuncu.transform;
            rapor.Add(semtKayit == null ? "Semt akisi: KAYIT YOK"
                                    : "Semt akisi: kuruldu");

            // 9) PERDE 2 DILIMI — talim -> kule -> ucus -> inis -> tepki.
            var dilim = oyuncu.GetComponent<Perde2Dilimi>()
                        ?? oyuncu.AddComponent<Perde2Dilimi>();
            dilim.dizi = oyuncu.GetComponent<UcusDizisi>();
            dilim.oyuncu = oyuncu.transform;
            rapor.Add("Perde 2: dilim baglandi");

            // 10) KAYIT — neyin yazildigi ve nereye dondugu tek yerde.
            var kayit = Tekil<KayitBaglayici>("KAYIT");
            kayit.zaman = zaman;
            kayit.oyuncu = oyuncu.transform;
            kayit.aranma = aranma;
            kayit.sehir = sehir;
            rapor.Add("Kayit: baglayici kuruldu");

            // 11) HUD — tarih, ezani saat, aranma; ESC/F5/F9.
            var hud = Tekil<OyunHud>("HUD");
            hud.zaman = zaman;
            hud.kayit = kayit;
            hud.aranma = aranma;
            rapor.Add("HUD: ESC duraklat, F5 kaydet, F9 yukle");

            EditorSceneManager.MarkSceneDirty(sahne);
            EditorSceneManager.SaveScene(sahne);
            Debug.Log("[Hezarfen] OYUN SAHNESI KURULDU\n  "
                      + string.Join("\n  ", rapor));
        }

        /// <summary>Oyuncuyu kurar: yürüme + uçuş + kamera.</summary>
        private static GameObject Oyuncu(out string rapor)
        {
            var eski = GameObject.Find("OYUNCU");
            if (eski != null) Object.DestroyImmediate(eski);

            var go = new GameObject("OYUNCU");
            var arazi = Object.FindAnyObjectByType<Terrain>();
            float y = arazi != null
                ? arazi.SampleHeight(BaslangicNoktasi)
                  + arazi.transform.position.y
                : 0f;
            go.transform.position = new Vector3(
                BaslangicNoktasi.x, y + 0.2f, BaslangicNoktasi.z);

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.70f; cc.radius = 0.30f;
            cc.center = new Vector3(0f, 0.85f, 0f);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 78f; rb.isKinematic = true; rb.useGravity = false;

            var yurume = go.AddComponent<WalkController>();
            var suzulme = go.AddComponent<GlideController>();
            suzulme.enabled = false;

            var dizi = go.AddComponent<UcusDizisi>();
            dizi.kapsul = cc;
            dizi.govde = rb;
            dizi.suzulme = suzulme;
            dizi.yurume = yurume;

            // Kamera oyuncunun GOZUNDE. Sahnedeki bagimsiz kamera
            // kaldirilir; iki kamera olursa hangisinin cizdigi belirsiz.
            var eskiKam = GameObject.Find("Main Camera");
            if (eskiKam != null) Object.DestroyImmediate(eskiKam);

            var kamGo = new GameObject("Main Camera");
            kamGo.tag = "MainCamera";
            kamGo.transform.SetParent(go.transform, false);
            // Goz yuksekligini WalkController kendisi kuruyor
            // (`eyeHeight`); burada da yazmak ayni sayiya iki sahip
            // vermek olurdu.
            kamGo.transform.localPosition = Vector3.zero;
            kamGo.AddComponent<Camera>();
            kamGo.AddComponent<AudioListener>();
            kamGo.AddComponent<UnityEngine.Rendering.HighDefinition
                .HDAdditionalCameraData>();

            rapor = $"Oyuncu: Galata, ({go.transform.position.x:F0}, "
                    + $"{go.transform.position.y:F0}, "
                    + $"{go.transform.position.z:F0})";
            return go;
        }

        /// <summary>Adı verilen tekil sistem nesnesini bulur ya da kurar.</summary>
        private static T Tekil<T>(string ad) where T : Component
        {
            var go = GameObject.Find(ad);
            if (go == null) go = new GameObject(ad);
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }
    }
}
