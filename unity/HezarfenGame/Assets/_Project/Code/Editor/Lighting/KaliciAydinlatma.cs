using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Editor.Lighting
{
    /// <summary>
    /// <b>Kalıcı ışık pası</b> — Faz 7'nin ilk işi.
    ///
    /// PLAN Bölüm 12: *"Faz 7 başlarken yapılacak İLK iş: geçici aydınlatma
    /// takımını SİLMEK. Kalıcı ışık pası bunun ÜSTÜNE kurulmaz, YERİNE
    /// kurulur."*
    ///
    /// ## Geçici takım neyi taklit ediyordu
    ///
    /// Eksik olan tek terim vardı: <b>sıçrama</b>. HDRP'de gerçek zamanlı
    /// küresel aydınlatma yoktur; ışık pişirilmediği sürece bir duvar
    /// yalnızca güneşi ve göğü görür, karşı duvardan dönen ışığı görmez.
    /// Dar bir Osmanlı sokağında gölgeyi asıl dolduran şey odur. Geçici
    /// takım bunu iki <b>gölgesiz</b> dolgu ışığı ve şişirilmiş bir gök
    /// çarpanıyla taklit ediyordu — fizikî değil, kadraja göre ayarlanmış.
    ///
    /// ## Kalıcı çözüm o terimi TAKLİT etmiyor, HESAPLIYOR
    ///
    /// İki katman, ikisi de gerçek:
    ///
    /// <list type="number">
    /// <item><b>Adaptive Probe Volumes (APV)</b> — temel. Sahne bir prob
    ///   ızgarasıyla kaplanır ve sıçrama <b>pişirilir</b>. Her donanım
    ///   kademesinde açık; şehir aktığında problar da akar. Bir sokağın
    ///   gölgesini gerçekten karşı cephe doldurur.</item>
    /// <item><b>SSGI</b> — üstüne. Ekranda görünen yüzeylerden gelen anlık
    ///   sıçrama; probların yakalayamadığı ince ayrıntıyı (cumba altı,
    ///   kemer içi) tamamlar. PLAN'ın dediği gibi <b>kademeli</b>:
    ///   Performant'ta kapalı, Balanced ve High'da açık.</item>
    /// </list>
    ///
    /// ## Poz artık elle çekilmiyor
    ///
    /// Geçici takım pozu 14,5'ten 13,0 EV'ye çekiyordu, çünkü sahne toptan
    /// az pozlanmıştı — sıçrama eksik olduğu için. Terim yerine gelince o
    /// düzeltmeye gerek kalmaz: poz <b>fizikî</b> değerine döner ve
    /// otomatik poz gün boyu (öğle 15 EV, alacakaranlık 6 EV) kendi
    /// ayarlar. Gözün yaptığı da budur.
    /// </summary>
    public static class KaliciAydinlatma
    {
        public const string RigName = "AYDINLATMA_Kalici";
        public const string ProbeName = "APV_Sehir";

        private const string ProfileDir = "Assets/_Project/Settings";
        private const string ProfilePath =
            ProfileDir + "/VP_Kalici_Aydinlatma.asset";

        /// <summary>
        /// Fizikî öğle pozu (EV100). Geçici takım bunu 13,0'a çekiyordu;
        /// sıçrama terimi geri gelince gerçek değerine dönüyor.
        /// </summary>
        public const float OgleEV = 14.5f;

        /// <summary>Alacakaranlık pozu — otomatik pozun alt sınırı.</summary>
        public const float AlacakaranlikEV = 6.0f;

        /// <summary>
        /// Haliç sabahının sisi (m). Sis bir efekt değil bir <b>yer</b>
        /// bilgisi: Haliç sabahları basar ve Galata'dan bakınca suriçi
        /// siluetini yumuşatan şey odur.
        /// </summary>
        public const float SisMesafesi = 1400f;

        // --------------------------------------------------------------
        [MenuItem("Hezarfen/Aydinlatma/Kalici isik pasini kur")]
        public static void KurMenu()
        {
            int n = Kur(out string rapor);
            Debug.Log($"[Hezarfen] Kalici isik pasi kuruldu ({n} parca).\n{rapor}");
            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene());
        }

        [MenuItem("Hezarfen/Aydinlatma/Kalici isik pasini kaldir")]
        public static void KaldirMenu()
        {
            bool vardi = Kaldir();
            Debug.Log(vardi ? "[Hezarfen] Kalici isik pasi kaldirildi."
                            : "[Hezarfen] Kalici isik pasi zaten yoktu.");
            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene());
        }

        public static bool Kaldir()
        {
            var eski = GameObject.Find(RigName);
            if (eski == null) return false;
            Object.DestroyImmediate(eski);
            return true;
        }

        /// <summary>
        /// Kalıcı pası kurar. <b>Geçici takımı da siler</b> — ikisi bir
        /// arada durursa sıçrama iki kere sayılır ve sahne patlar.
        /// </summary>
        public static int Kur(out string rapor)
        {
            Kaldir();
            bool geciciVardi = InterimLighting.Uninstall();

            var kok = new GameObject(RigName);
            int n = 0;

            // 1) GLOBAL VOLUME — poz, sis, SSGI.
            var volumeGo = new GameObject("VOL_Kalici");
            volumeGo.transform.SetParent(kok.transform, false);
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;   // gecici profilin ustunde
            volume.sharedProfile = Profil();
            n++;

            // 2) PROB HACMI — sicrama terimi burada PISIRILIR.
            //
            // MODE.GLOBAL DEGIL, ve bu bir ayrinti degil.
            //
            // Ilk yazimda `Mode.Global` vardi ("sahnenin tamamini kapla")
            // ve pisirme bitmedi: editor onbes dakika donduktan sonra tek
            // satir cikti bile uretmemisti. Sebep olculdu — sokak sahnesi
            // ARAZIYI de iceriyor, yani "sahnenin tamami" 10 km x 10 km
            // demek. APV'ye 100 km2'lik bos yamaci prob prob pisirtmeye
            // calismisim.
            //
            // Problar oyuncunun YURUDUGU yere aittir: binalarin arasina,
            // sokaga, avluya. Bos yamacta sicrama diye bir sorun zaten
            // yok — orada gokyuzu her seyi goruyor.
            var probeGo = new GameObject(ProbeName);
            probeGo.transform.SetParent(kok.transform, false);
            var pv = probeGo.AddComponent<ProbeVolume>();
            pv.mode = ProbeVolume.Mode.Local;

            var sinir = YapiSinirlari();
            probeGo.transform.position = sinir.center;
            pv.size = sinir.size;
            n++;

            rapor = (geciciVardi
                        ? "Gecici takim SILINDI (ustune degil yerine).\n"
                        : "Gecici takim zaten yoktu.\n")
                    + $"Poz: fiziki {OgleEV} EV (otomatik, alt sinir "
                    + $"{AlacakaranlikEV}).\n"
                    + $"Sis: {SisMesafesi} m (Halic sabahi).\n"
                    + $"Prob hacmi: {_probYazi}\n"
                    + "Sicrama: APV (pisirilmeli) + SSGI (kademeli).\n"
                    + "SONRAKI ADIM: Hezarfen -> Aydinlatma -> Problari pisir";
            return n;
        }

        /// <summary>En büyük prob hacmi kenarı (m) — pişirme bütçesi.</summary>
        public const float EnBuyukProbKenari = 600f;

        private static string _probYazi = "";

        /// <summary>
        /// Prob hacminin kaplayacağı kutu: <b>araziyi saymaz</b>.
        ///
        /// Sıçrama terimi binalar arasında gerekli; boş yamaçta gökyüzü
        /// zaten her şeyi görüyor. Araziyi katmak, prob ızgarasını on
        /// kilometreye yaymak ve pişirmeyi bitmez hâle getirmek demekti —
        /// bir kez denendi, bitmedi.
        /// </summary>
        public static Bounds YapiSinirlari()
        {
            bool ilk = true;
            var kutu = new Bounds(Vector3.zero, Vector3.one);
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(
                         FindObjectsSortMode.None))
            {
                if (r.GetComponentInParent<Terrain>() != null) continue;
                // Su duzlemi ve benzeri devasa tek parcalar da sayilmaz.
                if (r.bounds.size.x > EnBuyukProbKenari * 2f) continue;
                if (ilk) { kutu = r.bounds; ilk = false; }
                else kutu.Encapsulate(r.bounds);
            }
            if (ilk) kutu = new Bounds(Vector3.zero, Vector3.one * 60f);

            // Kenari yine de sinirla: bir semt 600 m'yi asarsa prob hacmi
            // semt basina BOLUNMELI, tek hacme sisirilmemeli.
            var b = kutu.size;
            b.x = Mathf.Min(b.x + 12f, EnBuyukProbKenari);
            b.z = Mathf.Min(b.z + 12f, EnBuyukProbKenari);
            b.y = Mathf.Min(b.y + 12f, 120f);   // catilarin biraz ustu
            kutu.size = b;

            _probYazi = $"{b.x:F0} x {b.y:F0} x {b.z:F0} m @ "
                + $"({kutu.center.x:F0}, {kutu.center.y:F0}, "
                + $"{kutu.center.z:F0})  [arazi HARIC]";
            return kutu;
        }

        /// <summary>Kalıcı Volume profili — yoksa üretilir.</summary>
        public static VolumeProfile Profil()
        {
            var mevcut = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            // BOS bir profil, olmayan bir profilden DAHA KOTUdur: menu
            // "kuruldu" der, sahne kararir ve sebep gorunmez. Bos bulursak
            // atip yeniden kuruyoruz.
            if (mevcut != null && mevcut.components.Count > 0) return mevcut;
            if (mevcut != null) AssetDatabase.DeleteAsset(ProfilePath);

            Directory.CreateDirectory(ProfileDir);
            var profil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profil, ProfilePath);

            // BILESENLER ALT-VARLIK OLARAK KAYDEDILMELI.
            //
            // `VolumeProfile.Add<T>()` bileseni yalnizca BELLEKTE kurar;
            // diske yazilmasi icin ayrica `AddObjectToAsset` gerekir. Ilk
            // yazimda bu yoktu ve sonuc SESSIZDI: dosya olustu, menu
            // "kuruldu" dedi, ama profilin icinde hicbir sey yoktu. Poz
            // gecici takimin 13,0 EV'sini kaybedince sahne karardi ve
            // olcum 2,62'den 0,55'e dustu. Once sicramadan suphelendim;
            // oysa profil bostu.

            // --- POZ: otomatik, fiziki sinirlar icinde ------------------
            //
            // Sabit bir EV, gun boyu donen bir gunesle calismaz: ogleyin
            // dogru olan deger alacakaranlikta sahneyi karartir. Goz de
            // uyum saglar; kamera da saglasin.
            var poz = profil.Add<Exposure>(true);
            AssetDatabase.AddObjectToAsset(poz, profil);
            poz.mode.overrideState = true;
            poz.mode.value = ExposureMode.AutomaticHistogram;
            poz.limitMin.overrideState = true;
            poz.limitMin.value = AlacakaranlikEV;
            poz.limitMax.overrideState = true;
            poz.limitMax.value = OgleEV + 2.0f;
            poz.adaptationMode.overrideState = true;
            poz.adaptationMode.value = AdaptationMode.Progressive;
            // Gozun uyumu ani degildir: karanlik bir hana girince bir an
            // hicbir sey gorulmez, sonra acilir. Hizli uyum bu duyguyu
            // tumden siler.
            poz.adaptationSpeedDarkToLight.overrideState = true;
            poz.adaptationSpeedDarkToLight.value = 1.2f;
            poz.adaptationSpeedLightToDark.overrideState = true;
            poz.adaptationSpeedLightToDark.value = 0.6f;

            // --- SIS: Halic sabahi -------------------------------------
            var sis = profil.Add<Fog>(true);
            AssetDatabase.AddObjectToAsset(sis, profil);
            sis.enabled.overrideState = true;
            sis.enabled.value = true;
            sis.meanFreePath.overrideState = true;
            sis.meanFreePath.value = SisMesafesi;
            sis.baseHeight.overrideState = true;
            sis.baseHeight.value = 0f;          // deniz seviyesi
            sis.maximumHeight.overrideState = true;
            sis.maximumHeight.value = 140f;     // Galata sirtinin ustu
            sis.enableVolumetricFog.overrideState = true;
            sis.enableVolumetricFog.value = true;

            // --- SSGI: sicramanin ustune, kademeli ---------------------
            var ssgi = profil.Add<GlobalIllumination>(true);
            AssetDatabase.AddObjectToAsset(ssgi, profil);
            ssgi.enable.overrideState = true;
            ssgi.enable.value = true;

            EditorUtility.SetDirty(profil);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ProfilePath);

            // KURDUGUNU DOGRULA. Bos bir profil sessizce kabul edilirse
            // sahne kararir ve sebep gorunmez.
            if (profil.components.Count == 0)
                Debug.LogError("[Hezarfen] Kalici profil BOS kaldi — poz, "
                               + "sis ve SSGI yazilamadi.");
            return profil;
        }

        // --------------------------------------------------------------
        /// <summary>
        /// Prob ızgarasını pişirir — <b>sıçrama terimi burada doğar</b>.
        ///
        /// <c>Lightmapping.Bake()</c> DEĞİL: o çağrı sahnenin her şeyini
        /// pişirir (ışık haritaları dahil), eşzamanlıdır ve editörü
        /// kilitler — ilk denemede editör dakikalarca yanıt vermedi ve
        /// hangi işin sürdüğü bile görünmedi. Burada yalnızca <b>problar</b>
        /// pişiriliyor ve iş asenkron: editör yaşamaya devam eder.
        /// </summary>
        [MenuItem("Hezarfen/Aydinlatma/Problari pisir")]
        public static void PisirMenu()
        {
            var pv = Object.FindAnyObjectByType<ProbeVolume>();
            if (pv == null)
            {
                Debug.LogError("[Hezarfen] Sahnede prob hacmi yok — once "
                               + "'Kalici isik pasini kur'.");
                return;
            }
            Debug.Log("[Hezarfen] Problar pisiriliyor (yalniz APV)...");
            AdaptiveProbeVolumes.BakeAsync();
        }
    }
}
