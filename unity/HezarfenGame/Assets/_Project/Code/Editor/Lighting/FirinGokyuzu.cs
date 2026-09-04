using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Editor.Lighting
{
    /// <summary>
    /// <b>Fırının kendi gökyüzü</b> — oyunun gökyüzünden ayrı, parlaklığı
    /// açıkça verilmiş.
    ///
    /// ## Neden ayrı bir profil
    ///
    /// Fırın bitti, diske 157 MB prob verisi yazdı ve sokağın gölgesi
    /// yine mavi/kırmızı <b>0,000</b> okudu. Verinin kendisi ölçüldü:
    /// <c>CellData</c> (L0, ışınım) sıfır, <c>CellOptionalData</c> (L1,
    /// yön) baştan sona <c>0x7f</c> — yani problar <b>yerinde, geçerli
    /// ve ışıksız</b>.
    ///
    /// Sebep zinciri kısa: fırında ışık yok (bilinçli — güneş
    /// <c>Realtime</c>, <c>ZamanSistemi</c> onu saate göre döndürüyor),
    /// geriye gökyüzü kalıyor ve fırın gökyüzünü
    /// <c>RenderSettings.ambientProbe</c> üzerinden görüyor. O prob
    /// ölçüldü: <b>0,0370 / 0,0421 / 0,0546</b>. Mavimsi, yani gerçekten
    /// gökten geliyor — ama bir gün ışığı göğü için çok karanlık, ve
    /// 0,037'lik bir ortamdan tek sıçrama L0'ın kodlama çözünürlüğünün
    /// altına düşüp sıfır yazılıyor.
    ///
    /// Neden karanlık: <b>PhysicallyBasedSky güneş ışığının atmosferde
    /// saçılmasıyla parlar.</b> Güneş fırının dışındaysa saçılacak bir
    /// şey de yoktur; geriye göğün kendi terimi kalır.
    ///
    /// ## Karar (ADR 0087)
    ///
    /// Pişirmenin gökyüzü, oyunun gökyüzünden ayrılıyor. Oyunun profili
    /// (<c>VP_Faz1_Sky</c>) hem sahnedeki <c>Volume</c> hem de
    /// <c>StaticLightingSky</c> tarafından kullanılıyordu; parlaklığını
    /// değiştirmek oyunun görüntüsünü de değiştirirdi.
    ///
    /// Bu profil <b>yalnız fırının</b>: parlaklığı <c>Lux</c> kipinde ve
    /// açıkça verilmiş. HDRP gök küpünü çizer, sonra bütünleşik
    /// aydınlanması istenen lux değerine gelecek şekilde ölçekler — yani
    /// güneşin o anda nerede olduğundan <b>bağımsız</b> bir gök.
    ///
    /// Bedeli: gece ile gündüz aynı dolaylı ışığı alır. Kazancı: sıçrama
    /// saatten bağımsız, tek pişirme bütün günü kapatır, ve sokak
    /// okunur. Bu makinede tek pişirme iki saat sürüyor; saat başına bir
    /// pişirme seçenek değil.
    /// </summary>
    public static class FirinGokyuzu
    {
        public const string ProfilYolu =
            "Assets/_Project/Art/Settings/VP_Firin_Sky.asset";

        /// <summary>
        /// Fırının göğünün aydınlanması (lux).
        ///
        /// <b>Ölçüden türer, seçilmez.</b> Sahnedeki güneş 100.000 lux
        /// (öğle güneşi) ve gün ışığında <b>gökyüzünün payı toplamın
        /// %10-20'sidir</b> — açık gökte güneş doğrudan ~100.000 lux
        /// verirken yatay yüzeye düşen gök ışığı 10.000-20.000 lux
        /// arasındadır. Üst uç alındı: sıçrama ışığı bu oyunda sokağın
        /// okunabilmesi için var.
        /// </summary>
        public const float GokLuxu = 20000f;

        /// <summary>
        /// Fırın skybox'ının pozu — <b>ölçülecek kaldıraç</b>.
        ///
        /// Fırının gördüğü gök doğrudan bu değerden geliyor. Ölçüldü:
        /// bu malzeme bağlanınca ortam probu <c>0,037/0,042/0,055</c>'ten
        /// <c>0,182/0,228/0,302</c>'ye çıktı — yani ortamı belirleyen
        /// şey <see cref="GokLuxu"/> değil, <b>bu sayı</b>. İlerlemeli
        /// fırın HDRP'nin statik aydınlatma göğünü değil, Unity'nin
        /// kendi skybox yolunu okuyor.
        ///
        /// <b>Hedef ölçülebilir:</b> pişmiş probların, yerine geçtikleri
        /// yedek ortamdan (0,18/0,23/0,30) daha karanlık olmaması. A/B
        /// bugün tersini söylüyor — APV kapatılınca gölge aydınlanıyor
        /// (0,0217 &gt; 0,0202), yani problar yedek ortamdan karanlık.
        ///
        /// Deney on dakika (`D_Okmeydani`), ölçü iki tane: pişirme
        /// kaydındaki ortam probu satırı ve
        /// <c>tools/olcum/prob_isigi.py</c>. 1,3 bir gündüz göğünün
        /// alışıldık değeri; ölçüm onu tutmazsa değişecek olan sayı
        /// budur.
        /// </summary>
        public const float SkyboxPozu = 1.3f;

        [MenuItem("Hezarfen/Aydinlatma/Firin gokyuzunu uret")]
        public static void UretMenu()
        {
            var vp = Uret();
            Selection.activeObject = vp;
            Debug.Log($"[Hezarfen] {ProfilYolu} — {vp.components.Count} "
                      + $"bilesen, {GokLuxu:0} lux (diske yazildi).");
        }

        public static VolumeProfile Uret()
        {
            const string klasor = "Assets/_Project/Art/Settings";
            if (!AssetDatabase.IsValidFolder(klasor))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Settings");

            // TEMIZ KUR: eski alt-nesneler kalirsa profil sisirilir.
            // Gerekcesi `SkyProfileBuilder`da yazili — `VolumeProfile.Add`
            // bileseni bellekte kurar, asset'e YAZMAZ.
            AssetDatabase.DeleteAsset(ProfilYolu);
            var profil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profil, ProfilYolu);

            var ve = Ekle<VisualEnvironment>(profil);
            ve.skyType.overrideState = true;
            ve.skyType.value = (int)SkyType.PhysicallyBased;
            ve.skyAmbientMode.overrideState = true;
            // Bu profil YALNIZ pisirme icin: ortam gogu statik olmali.
            ve.skyAmbientMode.value = SkyAmbientMode.Static;

            var gok = Ekle<PhysicallyBasedSky>(profil);
            // PARLAKLIK LUX KIPINDE — ISIN ASIL DUGUMU BURASI.
            //
            // Oyunun profili `Exposure` kipinde (carpan 1, poz 0), yani
            // gogun parlakligi o anki sahnenin fizigine birakilmis. Fırın
            // icin bu, "gunes disarida oldugu icin karanlik" demek.
            // `Lux` kipinde HDRP gok kubbesini cizer ve butunlesik
            // aydinlanmasi `desiredLuxValue`ya gelecek sekilde OLCEKLER:
            // sonuc, gunesin o anda nerede oldugundan bagimsiz.
            gok.skyIntensityMode.overrideState = true;
            gok.skyIntensityMode.value = SkyIntensityMode.Lux;
            gok.desiredLuxValue.overrideState = true;
            gok.desiredLuxValue.value = GokLuxu;

            EditorUtility.SetDirty(profil);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ProfilYolu,
                                      ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilYolu);
        }

        public const string SkyboxYolu =
            "Assets/_Project/Art/Settings/M_Firin_Skybox.mat";

        /// <summary>
        /// <b>Fırın için bir skybox malzemesi</b> — ilerlemeli fırının
        /// ortamı buradan okuduğu hipotezinin sınanması.
        ///
        /// ## Ölçüm
        ///
        /// Problar L0 = <b>tam sıfır</b> pişti. "Az ışık" değil, <i>hiç
        /// ışık</i>: `CellData` dosyasının tamamı iki desenin tekrarı
        /// (<c>0000000000000038</c> ve <c>7f7f7f...</c>), yani ışınım
        /// sıfır ve yön nötr. Gök profilini Lux kipinde 20.000'e
        /// sabitlemek <b>hiçbir şeyi değiştirmedi</b> — aynı iki desen.
        ///
        /// Ortam probu ölçüldü ve 0,037 çıktı; sıfır değil. Yani
        /// lightmapper ortamını <c>RenderSettings.ambientProbe</c>'dan
        /// okusaydı sonuç küçük ama sıfırdan farklı olurdu. Tam sıfır,
        /// ortamın <b>siyah</b> olduğunu söyler.
        ///
        /// Sahnede <c>m_AmbientMode: 0</c> (Skybox) ve
        /// <c>m_SkyboxMaterial: {fileID: 0}</c> — yani <b>skybox
        /// malzemesi yok</b>. HDRP kendi göğünü çizdiği için buna
        /// ihtiyaç duymaz; ama ilerlemeli fırın Unity'nin kendi ortam
        /// yolunu kullanıyorsa gördüğü şey boş bir skybox, yani siyah.
        ///
        /// Bu malzeme o hipotezi sınar. Doğruysa fırın ışığı görür;
        /// yanlışsa L0 yine sıfır kalır ve hipotez elenir — iki
        /// durumda da on dakikada cevap.
        ///
        /// Güneş diski <b>kapalı</b>: güneş artık fırının İÇİNDE
        /// (<c>Mixed</c>) ve skybox'a bir güneş koymak onu ikinci kez
        /// eklemek olurdu. Kalan şey gökyüzünün difüz kubbesi.
        ///
        /// ## Sonuç: hipotez tek başına yanlış çıktı, malzeme kaldı
        ///
        /// Skybox <b>tek başına</b> problara ışık koymadı — L0 yine tam
        /// sıfır pişti. Işığı getiren şey güneşin <c>Mixed</c> olmasıydı
        /// (ADR 0087). Ama malzeme kaldı, çünkü ortam probunu ölçülebilir
        /// biçimde yükseltiyor (0,037 → 0,18/0,23/0,30) ve gökyüzü
        /// sıçraması gerçek bir terim.
        ///
        /// <b>Sınanmamış:</b> güneş fırına girdikten sonra skybox hâlâ
        /// gerekli mi. Deneyi on dakika: `D_Okmeydani`'yi bir kez
        /// skybox'sız pişir ve `tools/olcum/prob_isigi.py` ile desen
        /// sayısına bak. Gerekmiyorsa malzeme kaldırılır — HDRP onu
        /// çizimde kullanmıyor, yani sahnede duran ölü bir bağ olur.
        /// </summary>
        public static Material SkyboxUret()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(SkyboxYolu);
            var sh = Shader.Find("Skybox/Procedural");
            if (sh == null) return m;
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, SkyboxYolu);
            }
            m.shader = sh;
            m.SetFloat("_SunDisk", 0f);              // gunes diski YOK
            m.SetFloat("_AtmosphereThickness", 1.0f);
            m.SetColor("_SkyTint", new Color(0.5f, 0.5f, 0.5f, 1f));
            m.SetColor("_GroundColor", new Color(0.37f, 0.35f, 0.32f, 1f));
            // Poz: gok kubbesinin butunlesik aydinlanmasi kabaca
            // `GokLuxu` mertebesinde olsun. Prosedurel skybox fiziksel
            // birim tasimaz; 1,3 gunduk gogunun alisildik degeri ve
            // sonucu ZATEN OLCULECEK (prob L0'i sifirdan farkli mi).
            m.SetFloat("_Exposure", SkyboxPozu);
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            return m;
        }

        /// <summary>Bileşeni profile ekler VE asset'e alt-nesne yazar.</summary>
        private static T Ekle<T>(VolumeProfile profil) where T : VolumeComponent
        {
            var c = profil.Add<T>(overrides: true);
            c.hideFlags = HideFlags.HideInHierarchy;
            c.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(c, profil);
            return c;
        }
    }
}
