using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// <b>Kalıcı ışık profilini tamamlar.</b>
    ///
    /// Profil beş ayar taşıyordu: sis, poz, tonemap, film grain,
    /// hacimsel bulut. Dördü eksikti ve eksikliği şöyle görünür:
    ///
    /// <b>Düzeltme (ölçüm):</b> bu satır önce "küresel aydınlatma"
    /// yazıyordu. Profil okundu ve öyle bir bileşen <b>yok</b>; üstelik
    /// etkin kalite seviyesi <c>Balanced</c> ve o boru hattı varlığında
    /// <c>supportSSGI: 0</c>, yani ekran uzayı GI derlemede de kapalı.
    /// <c>lightProbeSystem</c> ise 1 (APV). Sonuç: bu oyunda dolaylı
    /// yayınık ışığın tek kaynağı <b>APV</b>dir — gölgeleri bugün
    /// aydınlatan sıcak ışık hacimsel sisten geliyor (bkz. ADR 0087).
    ///
    /// | eksik | yokluğunda ne olur |
    /// |---|---|
    /// | **ortam örtme** | saçak altı, cumba altı, kapı nişi düz aydınlık — hacim yok |
    /// | **temas gölgesi** | her nesne zeminden bir parmak yukarıda duruyormuş gibi |
    /// | **bloom** | öğle güneşi ve fener alevi ile beyaz badana aynı parlaklıkta |
    /// | **renk derecelendirme** | görüntü teknik olarak doğru, dramatik olarak nötr |
    ///
    /// Bunların hiçbiri model çözünürlüğü değil. Caner'in *"daha
    /// gerçekçi ve sinematik"* isteğinin büyük kısmı burada oturuyor:
    /// 201 ev varyantı ve 55 bin üçgenlik karakter, temas gölgesi
    /// olmayan bir sahnede maket gibi durur.
    ///
    /// ## Değerler nereden
    ///
    /// Sayılar bu şehrin ışığından: İstanbul'un yaz öğlesi sert ve
    /// beyaza yakın, kışı Karadeniz'in gri ışığıdır. Derecelendirme
    /// ikisinin arasında durur — sıcağa hafif kaydırılmış gölge,
    /// nötr yüksek ışık. Doygunluk **artırılmaz**: kireç badana,
    /// aşı boyası ve kiremidin kendi renkleri zaten güçlü; üstüne
    /// doygunluk eklemek turistik kartpostal yapar.
    /// </summary>
    public static class AydinlatmaPasi
    {
        private const string Profil =
            "Assets/_Project/Settings/VP_Kalici_Aydinlatma.asset";

        [MenuItem("Hezarfen/Boru Hatti/Aydinlatma pasini tamamla")]
        public static void Tamamla()
        {
            var vp = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Profil);
            if (vp == null)
            {
                Debug.LogError($"[Hezarfen] {Profil} yok.");
                return;
            }

            var sb = new StringBuilder("AYDINLATMA PASI\n");

            // --- ORTAM ÖRTME ------------------------------------------
            // Yarıçap 1,2 m: Osmanlı cephesinin gölge ürettiği ölçek
            // bu — saçak çıkması 0,5-0,95, cumba 0,4-1,05, kapı nişi
            // 0,3. Daha büyük bir yarıçap bütün duvarı karartır ve
            // kireç badananın parlaklığını yer.
            // HDRP 2022.2'den beri adi ScreenSpaceAmbientOcclusion.
            var ao = Al<ScreenSpaceAmbientOcclusion>(vp, sb, "ortam ortme");
            ao.intensity.overrideState = true;
            ao.intensity.value = 0.75f;
            ao.radius.overrideState = true;
            ao.radius.value = 1.2f;
            ao.quality.overrideState = true;
            ao.quality.value = 2;                 // orta: 8 GB VRAM
            ao.directLightingStrength.overrideState = true;
            // Doğrudan ışığı da bir miktar örter; yoksa güneş gören
            // saçak altı yine düz kalır.
            ao.directLightingStrength.value = 0.35f;

            // --- TEMAS GÖLGESİ ----------------------------------------
            // Gölge haritasının çözemediği ölçek: sandığın zemine
            // değdiği yer, sedirin ayağı, kaldırım taşının kenarı.
            // 0,15 m menzil yaya ölçeğidir; büyütmek uzaktaki
            // yapılarda çift gölge yapar.
            var cs = Al<ContactShadows>(vp, sb, "temas golgesi");
            cs.enable.overrideState = true;
            cs.enable.value = true;
            cs.length.overrideState = true;
            cs.length.value = 0.15f;
            cs.opacity.overrideState = true;
            cs.opacity.value = 0.85f;
            cs.quality.overrideState = true;
            cs.quality.value = 1;

            // --- BLOOM ------------------------------------------------
            // Zayıf ve dar. Dönem oyununda bloom bir efekt değil bir
            // **pozlama gerçeği**: göz sert güneşe uyum sağlarken
            // parlak yüzeyler taşar. 0,12 yoğunluk, badanayı
            // parlatmadan güneşi ve alevi ayırmaya yeter.
            var bl = Al<Bloom>(vp, sb, "bloom");
            bl.intensity.overrideState = true;
            bl.intensity.value = 0.12f;
            bl.scatter.overrideState = true;
            bl.scatter.value = 0.62f;
            bl.threshold.overrideState = true;
            bl.threshold.value = 1.1f;

            // --- RENK DERECELENDİRME ----------------------------------
            var lb = Al<LiftGammaGain>(vp, sb, "lift/gamma/gain");
            // Gölge sıcağa: taş ve ahşabın yansıttığı ışık mavi değil.
            lb.lift.overrideState = true;
            lb.lift.value = new Vector4(1.02f, 1.00f, 0.96f, 0.005f);
            // Yüksek ışık nötr: gökyüzü ve badana renk almasın.
            lb.gain.overrideState = true;
            lb.gain.value = new Vector4(1.00f, 1.00f, 1.01f, 0.02f);

            var ca = Al<ColorAdjustments>(vp, sb, "renk ayari");
            ca.postExposure.overrideState = true;
            ca.postExposure.value = 0.15f;
            ca.contrast.overrideState = true;
            ca.contrast.value = 8f;
            // DOYGUNLUK ARTIRILMIYOR — gerekçe sınıf belgesinde.
            ca.saturation.overrideState = true;
            ca.saturation.value = 0f;

            var vig = Al<Vignette>(vp, sb, "vinyet");
            vig.intensity.overrideState = true;
            vig.intensity.value = 0.18f;
            vig.smoothness.overrideState = true;
            vig.smoothness.value = 0.55f;

            EditorUtility.SetDirty(vp);
            AssetDatabase.SaveAssets();

            sb.AppendLine($"  profil bileseni: {vp.components.Count}");
            Debug.Log("[Hezarfen] " + sb);
        }

        /// <summary>
        /// Bileşeni bulur; yoksa ekler, <b>diske yazar</b> ve söyler.
        ///
        /// ## AddObjectToAsset olmadan bu geçiş HİÇBİR ŞEY yapmıyordu
        ///
        /// <c>VolumeProfile.Add&lt;T&gt;()</c> bileşeni yalnız bellekte
        /// kurar. <c>SaveAssets</c> onu yazmaz, çünkü bileşen henüz
        /// varlığın bir parçası değildir — profil dosyasında hiç
        /// görünmez. Geçiş "eklendi" diye altı satır log yazdı,
        /// diskteki profil <b>beş</b> bileşende kaldı ve ortam örtme,
        /// temas gölgesi, bloom, renk derecelendirme ve vinyet oyuna
        /// hiç girmedi.
        ///
        /// Bu tuzak bu depoda İKİNCİ kez kuruldu: <c>KaliciAydinlatma</c>
        /// aynı hatayı yaşamış ve <c>Ensure&lt;T&gt;</c> deyimini yazıp
        /// gerekçesini de yanına koymuş. Yeni dosya o deyimi kullanmadı.
        /// Bir dersi yazmak, onu uygulamak değildir — bu yüzden aşağıda
        /// deyim tekrarlanmıyor, <b>testle</b> tutuluyor
        /// (<c>AydinlatmaProfiliTests</c>).
        /// </summary>
        private static T Al<T>(VolumeProfile vp, StringBuilder sb, string ad)
            where T : VolumeComponent
        {
            if (vp.TryGet(out T c) && c != null)
            {
                sb.AppendLine($"  {ad}: vardi, guncellendi");
                return c;
            }
            c = vp.Add<T>(true);
            c.hideFlags = HideFlags.HideInHierarchy;
            if (AssetDatabase.Contains(vp))
                AssetDatabase.AddObjectToAsset(c, vp);
            sb.AppendLine($"  {ad}: YOKTU, eklendi ve diske yazildi");
            return c;
        }
    }
}
