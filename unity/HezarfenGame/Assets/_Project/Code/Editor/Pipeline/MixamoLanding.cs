using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// <b>Mixamo kliplerinin iniş alanı.</b>
    ///
    /// Klipler artık iki kaynaktan geliyor:
    ///
    /// - **Uçuş** (süzülüş pozları, kalkış, iniş, çakılma) elle yapıldı ve
    ///   elde kalacak — süzülüşün mocap'i yok ve olamaz (ADR 0068).
    /// - **Yer hareketi** Mixamo'dan. Sebebi ADR 0076: on üç klibi toptan
    ///   yeniden üretmek karakteri parçalıyor ve kök sebep bilinmiyor.
    ///   Mixamo klibi bizim Blender hattımızdan hiç geçmiyor, yani o
    ///   kusurun etrafından dolaşmıyoruz — onu <b>gereksiz</b> kılıyoruz.
    ///
    /// ## "In Place" işaretlenmemişti — ve iyi ki
    ///
    /// İndirilen dosyalar ölçüldü (`art/mixamo/meta.json`): yürüme
    /// klibinde kök <b>1,845 m</b>, koşmada <b>3,324 m</b> yol alıyor.
    /// Yani Mixamo'nun "In Place" kutusu işaretlenmemiş. Yeniden
    /// indirtmek yerine kök XZ burada <b>Bake Into Pose</b> ile alınıyor;
    /// sonuç birebir aynı.
    ///
    /// Üstelik o kök hareketi işe yaradı: klibin <b>öz hızı</b> ondan
    /// ölçüldü. Karışım ağacındaki oynatma çarpanı bu sayıdan türüyor
    /// (<c>çarpan = eşik / öz hız</c>), ve o çarpan ayak kaymasını
    /// <i>yapısal olarak</i> sıfırlıyor: bir klibi hızlandırmak adım
    /// <b>sıklığını</b> artırır, adım <b>boyunu</b> değil.
    ///
    /// Bu sayının tek sahibi `art/mixamo/meta.json`'dur. Buraya elle bir
    /// hız yazmak, ADR 0076'nın kök sebebini (bir sayının iki sahibi)
    /// tekrar üretmek olurdu.
    /// </summary>
    public static class MixamoLanding
    {
        private const string ImportDir = "Assets/_Import";
        private const string ModelDir = "Assets/_Project/Art/Models/Karakter";
        private const string Prefix = "MX_";

        /// <summary>
        /// Unity'nin klip adı <c>dosya@klip</c> kuralı. `AnimatorKur`
        /// klipleri bu desenle topluyor; Mixamo klipleri de aynı yoldan
        /// girsin diye adları buna uyduruluyor.
        /// </summary>
        private const string Govde = "MX_Hezarfen";

        [MenuItem("Hezarfen/Boru Hatti/Mixamo kliplerini yerlestir")]
        public static void Place()
        {
            var kayit = MixamoKaydi.Oku();
            if (kayit == null || kayit.klipler == null || kayit.klipler.Length == 0)
            {
                Debug.LogError("[Hezarfen] art/mixamo/meta.json okunamadi. "
                    + "Once klipler olculmeli.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(ModelDir))
            {
                Debug.LogError($"[Hezarfen] {ModelDir} yok.");
                return;
            }

            var kayitAdi = kayit.klipler.ToDictionary(k => k.ad, k => k);
            var fbx = Directory.Exists(ImportDir)
                ? Directory.GetFiles(ImportDir, Prefix + "*.fbx")
                : new string[0];
            if (fbx.Length == 0)
            {
                Debug.LogWarning($"[Hezarfen] {ImportDir} altinda {Prefix}*.fbx "
                                 + "yok — yerlestirilecek Mixamo klibi bulunamadi.");
                return;
            }

            var sb = new StringBuilder("MIXAMO KLIPLERI\n");
            int ok = 0;
            var eksikKayit = new List<string>();

            // IKI GECIS — ve sebebi olculdu.
            //
            // Ilk yazimda kopyalama ve icе aktarma ayarlari TEK bir
            // StartAssetEditing/StopAssetEditing blogunun icindeydi. O
            // blok icinde Unity ice aktarmayi ERTELER: `GetAtPath` henuz
            // ice aktarilmamis bir varligin importer'ini dondurur ve
            // `SaveAndReimport` bosa gider. Sonuc sessizdi — menu "20/20
            // yerlesti" dedi, ama .meta dosyalarinda `animationType: 0`
            // ve `clipAnimations: []` yaziyordu. Yani hicbir klip yoktu
            // ve hicbir hata da yoktu.
            //
            // Once dosyalar tasinir ve ice aktarilir; ayarlar ANCAK
            // toplu duzenleme kapandiktan sonra yazilir.
            var yerlesen = new List<(string yol, string ad, MixamoKlip k)>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string src in fbx.OrderBy(p => p))
                {
                    string ad = Path.GetFileNameWithoutExtension(src)
                                    .Substring(Prefix.Length);
                    if (!kayitAdi.TryGetValue(ad, out var k))
                    {
                        eksikKayit.Add(ad);
                        continue;
                    }

                    // Unity'nin `@` kurali: dosya adi klip adini tasir.
                    string hedef = $"{ModelDir}/{Govde}@{ad}.fbx";
                    // Var olan yolun uzerine yazmak GUID'i korur; silip
                    // yeniden yaratmak butun referanslari sessizce kirardi.
                    if (File.Exists(hedef)) File.Delete(hedef);
                    File.Copy(src, hedef);
                    File.Delete(src);
                    string meta = src + ".meta";
                    if (File.Exists(meta)) File.Delete(meta);
                    yerlesen.Add((hedef, ad, k));
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            foreach (var (hedef, ad, k) in yerlesen)
            {
                if (Ayarla(hedef, ad, k, sb)) ok++;
            }

            if (eksikKayit.Count > 0)
            {
                sb.AppendLine($"KAYITSIZ (yerlestirilmedi): "
                              + string.Join(", ", eksikKayit));
            }
            sb.AppendLine($"yerlesen: {ok}/{fbx.Length}");
            if (ok == fbx.Length) Debug.Log("[Hezarfen] " + sb);
            else Debug.LogError("[Hezarfen] " + sb);
        }

        /// <summary>
        /// İçe aktarma ayarları. Her biri bir kararın sonucu.
        /// </summary>
        private static bool Ayarla(string yol, string ad, MixamoKlip k,
                                   StringBuilder sb)
        {
            var mi = AssetImporter.GetAtPath(yol) as ModelImporter;
            if (mi == null)
            {
                sb.AppendLine($"  {ad}: ModelImporter alinamadi.");
                return false;
            }

            // MESH YOK — "Without Skin" indirildi ve olculdu (meta.json
            // mesh=0). Yine de kapatiliyor: bir gun yanlislikla skinli bir
            // dosya gelirse Mixamo'nun kendi modeli oyuna sizmasin.
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importVisibility = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importBlendShapes = false;

            // AVATAR BU MODELDEN. "Copy From Other" ayni iskelet icindir;
            // Mixamo rig'i bizimkinden farkli (65 kemik). Humanoid'in
            // butun anlami zaten bu: iki farkli iskelet arasinda kas
            // uzayindan yeniden hedefleme.
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importAnimation = true;
            mi.resampleCurves = true;

            var mevcut = mi.defaultClipAnimations;
            if (mevcut == null || mevcut.Length == 0)
            {
                sb.AppendLine($"  {ad}: klip bulunamadi.");
                return false;
            }

            var c = mevcut[0];
            c.name = ad;
            c.loopTime = k.dongu;
            c.loopPose = k.dongu;

            // KOK XZ POZA PISIRILIR.
            //
            // Karakteri `WalkController` yurutuyor. Klip de yurutseydi
            // ikisi ayni anda iterdi ve karakter iki kat hizli giderdi.
            // "In Place" indirmede isaretlenmemis; burada ayni sonuc
            // aliniyor. Yatay yolu OLCTUKTEN sonra aliyoruz — sayi
            // meta.json'da duruyor ve karisim carpani ondan turuyor.
            c.lockRootPositionXZ = true;

            // Kok DONUSU de poza pisirilir: donusun sahibi kontrolcudur.
            // Donus klipleri (Donus_Sol/Sag) bu yuzden simdilik animator
            // grafigine baglanmiyor — yerinde donus, kontrolcunun o an
            // donmemesini gerektirir ve o ayri bir is.
            c.lockRootRotation = true;

            // DIKEY kok SERBEST kalir. Merdiven, basamak, ziplama ve
            // dusme kliplerinde yukselme hareketin KENDISIDIR; onu poza
            // pisirmek karakteri yerinde saydirirdi.
            c.lockRootHeightY = false;

            mi.clipAnimations = new[] { c };
            EditorUtility.SetDirty(mi);
            mi.SaveAndReimport();

            // AYAR YAZILDI MI — SOR, VARSAYMA.
            //
            // Bir onceki tur "yerlesti" dedi ve hicbir klip yoktu. Bir
            // boru hattinin kendi ciktisini dogrulamamasi, sessiz
            // basarisizligin en ucuz yoludur.
            var cikan = AssetDatabase.LoadAllAssetsAtPath(yol)
                .OfType<AnimationClip>()
                .Where(x => !x.name.StartsWith("__preview__"))
                .ToArray();
            if (cikan.Length == 0 || cikan.All(x => x.name != ad))
            {
                sb.AppendLine($"  {ad}: ice aktarmadan klip CIKMADI "
                    + $"({cikan.Length} klip: "
                    + string.Join("/", cikan.Select(x => x.name)) + ")");
                return false;
            }

            sb.AppendLine(
                $"  {ad,-16} {k.kare,3} kare  oz hiz {k.oz_hiz_ms,6:0.000} m/s"
                + (k.dongu ? "  dongu" : ""));
            return true;
        }
    }

    /// <summary>
    /// <b>Klibin yeniden hedeflenmiş yer hızı — bizim avatarımızda.</b>
    ///
    /// `art/mixamo/meta.json`'daki <c>oz_hiz_ms</c> Blender'da, <b>Mixamo'nun
    /// kendi iskeletinde</b> ölçüldü: yürüme klibinde kök 1,845 m yol
    /// alıyor, yani 1,786 m/s. O sayıdan türetilen oynatma çarpanı (1,232)
    /// ayak kaymasını sıfırlamadı — oyunda ölçülen kayma yürümede
    /// <b>16,4 cm</b>, koşmada <b>34,2 cm</b> çıktı.
    ///
    /// Sebep yapısal: Humanoid yeniden hedefleme pozları <b>kas
    /// uzayında</b> taşır, mesafede değil. Adım boyu kaynağın değil
    /// <b>hedefin</b> bacak uzunluğuyla ölçeklenir. Yani Mixamo'nun X
    /// Bot'unda 1,786 m/s olan klip, bizim gövdemizde başka bir hızdır ve
    /// o hız Blender'dan görülemez.
    ///
    /// Bu, bu oturumda yedinci kez aynı ders: <b>ölçtüğün şeyi, sevk
    /// ettiğin yerde ölç.</b> Burada klip bizim prefabımız üzerinde
    /// örneklenir ve basan ayağın köke göre geri gidiş hızı okunur —
    /// yer hızı tam olarak odur.
    /// </summary>
    public static class KlipYerHizi
    {
        private const string PrefabYolu =
            "Assets/_Project/Art/Prefabs/PF_Hezarfen_Sivil.prefab";

        /// <summary>
        /// Klibin yer hızı (m/s). Ölçülemezse <c>0</c> — çağıran taraf
        /// bunu bir tahminle doldurmaz.
        ///
        /// ## Yöntem: temas aramak yerine EN HIZLI GERİ GİDİŞ
        ///
        /// İlk yazımda "ayak yere yakınsa basıyordur" diye temas
        /// arıyordum ve ölçüm saçmaladı: yürüme 0,542 m/s, koşma
        /// 0,604 m/s — koşu yürümeden yavaş. Koşuda ayak havada geçen
        /// süre uzun, temas penceresi kısa ve topuk vuruşunda ayak
        /// eşiğin üstünde; yani eşik neyi yakaladığını bilmiyordu.
        ///
        /// Fizikte kesin olan tek nokta şudur: <b>orta duruşta basılı
        /// ayak, köke göre tam olarak yer hızıyla geriye gider.</b> O
        /// yüzden temas aranmıyor; her karede iki ayağın köke göre yerel
        /// hızından <b>en negatif</b> olanı alınıyor ve bunların en
        /// negatif dördünün ortancası okunuyor. Bu, orta duruş
        /// platosudur — eşiğe, temasa, ayak yüksekliğine bağlı değildir.
        /// </summary>
        public static float Olc(AnimationClip klip, int ornek = 180)
        {
            if (klip == null || klip.length <= 0.001f) return 0f;
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabYolu);
            if (pf == null) return 0f;

            var go = Object.Instantiate(pf);
            go.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var anim = go.GetComponentInChildren<Animator>();
                if (anim == null || !anim.isHuman) return 0f;
                var kemik = new[]
                {
                    anim.GetBoneTransform(HumanBodyBones.LeftFoot),
                    anim.GetBoneTransform(HumanBodyBones.RightFoot),
                };
                if (kemik[0] == null || kemik[1] == null) return 0f;

                float dt = klip.length / ornek;
                int n = ornek + 1;
                var poz = new Vector3[2][];
                poz[0] = new Vector3[n];
                poz[1] = new Vector3[n];

                for (int i = 0; i < n; i++)
                {
                    klip.SampleAnimation(go, i * dt);
                    for (int f = 0; f < 2; f++)
                        poz[f][i] = go.transform.InverseTransformPoint(
                            kemik[f].position);
                }

                // BASILI KARE: ayagin KENDI en alcak noktasina 1 cm
                // yakin oldugu kareler. Orada ayak kesinlikle yerdedir
                // ve koke gore hizi tam olarak yer hizidir.
                //
                // Isaret hic sorulmuyor: buyukluk aliniyor. Onceki iki
                // deneme once temas esigine, sonra +Z'nin ileri oldugu
                // varsayimina takildi — ikincisinde "yurume geri" klibi
                // en hizli klip cikti, cunku prefab 180 donuk.
                var hizlar = new List<float>();
                for (int f = 0; f < 2; f++)
                {
                    float enAlcak = float.MaxValue;
                    for (int i = 0; i < n; i++)
                        enAlcak = Mathf.Min(enAlcak, poz[f][i].y);

                    for (int i = 1; i < n; i++)
                    {
                        if (poz[f][i].y > enAlcak + 0.01f) continue;
                        if (poz[f][i - 1].y > enAlcak + 0.02f) continue;
                        var a = poz[f][i - 1];
                        var b = poz[f][i];
                        a.y = 0f; b.y = 0f;
                        hizlar.Add((b - a).magnitude / dt);
                    }
                }
                if (hizlar.Count < 4) return 0f;
                hizlar.Sort();
                return hizlar[hizlar.Count / 2];      // ortanca
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    /// <summary>Ölçüm kaydının C# karşılığı — `art/mixamo/meta.json`.</summary>
    [System.Serializable]
    public class MixamoKlip
    {
        public string dosya;
        public string ad;
        public bool dongu;
        public string neden;
        public int kare;
        public float sure_s;
        public float kok_yol_m;
        public float oz_hiz_ms;
        public float unity_hiz_ms;
        public int kemik;
        public int mesh;
        public float fps;
    }

    /// <summary>Kaydın tamamı.</summary>
    [System.Serializable]
    public class MixamoKayit
    {
        public string kaynak;
        public string indirme_tarihi;
        public MixamoKlip[] klipler;
    }

    /// <summary>
    /// Kaydı okur. <b>Tek sahip:</b> klip hızları yalnız burada yaşar;
    /// hem iniş hattı hem animator kontrolcüsü aynı dosyadan okur.
    /// </summary>
    public static class MixamoKaydi
    {
        public const string Yol = "art/mixamo/meta.json";

        public static MixamoKayit Oku()
        {
            string tam = Path.Combine(AssetCatalog.RepoRoot, Yol);
            if (!File.Exists(tam)) return null;
            try
            {
                return JsonUtility.FromJson<MixamoKayit>(File.ReadAllText(tam));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Hezarfen] {Yol} okunamadi: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Bu klibin öz hızı (m/s). Bulunamazsa <c>0</c> — çağıran taraf
        /// bunu bir tahminle doldurmaz, <b>söyler</b>.
        /// </summary>
        /// <summary>
        /// Oyunda ölçülen, yeniden hedeflenmiş yer hızı (m/s).
        /// Ölçülmemişse <c>0</c> — çağıran taraf yedeğe düşer ve söyler.
        /// </summary>
        public static float UnityHiz(string ad)
        {
            var k = Oku();
            if (k?.klipler == null) return 0f;
            foreach (var c in k.klipler)
                if (c.ad == ad) return c.unity_hiz_ms;
            return 0f;
        }

        public static float OzHiz(string ad)
        {
            var k = Oku();
            if (k?.klipler == null) return 0f;
            foreach (var c in k.klipler)
                if (c.ad == ad) return c.oz_hiz_ms;
            return 0f;
        }
    }
}
