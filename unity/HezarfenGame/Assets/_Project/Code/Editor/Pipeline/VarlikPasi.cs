using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// <b>Yeni bir varlık üretildikten sonra koşulması gereken her şey —
    /// tek çağrıda, doğru sırayla.</b>
    ///
    /// Blender tarafında bir kit değişince Unity tarafında beş ayrı menü
    /// koşmak gerekiyor ve sıraları önemli. Her biri ayrı bir toplu kip
    /// açılışı demek (her açılış 1-2 dk) ve daha kötüsü, biri
    /// unutulduğunda kusur <b>sessiz</b> oluyor: malzeme eski kalır,
    /// prefab LOD'suz iner, animatör yeni klibi görmez. Bu oturumda üç
    /// test tam bu yüzden kırmızıya döndü.
    ///
    /// ## Sıra ve gerekçesi
    ///
    /// 1. <b>Malzemeler</b> — bildirimdeki değişiklikler (örneğin bir
    ///    yüzeyin iki yüzlü olması) malzeme varlığına yazılır. Önce
    ///    koşar, çünkü prefablar malzemeye <i>referansla</i> bağlanır ve
    ///    referans zaten varsa sonradan değişen değer kendiliğinden
    ///    ulaşır — ama malzeme <i>yoksa</i> iniş boş yuva bırakır.
    /// 2. <b>Karakterler</b> — <c>_Import</c>'taki <c>SK_</c> dosyaları
    ///    Humanoid olarak <c>Art/Models/Karakter</c>'e iner. ÖNCE, çünkü
    ///    bir sonraki adım aynı klasörü okur ve karakterleri statik
    ///    varlık sanar.
    /// 3. <b>_Import'u yerleştir</b> — FBX'ler kalıcı yerlerine taşınır
    ///    ve prefab üretilir. İniş alanı boşalır (CLAUDE.md).
    /// 4. <b>LOD merdiveni</b> — inen prefablara LODGroup takılır.
    /// 5. <b>LOD geçişi</b> — sert sıçrama taramalı geçişe çevrilir.
    ///    Merdivenden SONRA, çünkü olmayan bir gruba bant verilemez.
    /// 6. <b>Animatör</b> — yeni klipler kontrolcüye bağlanır.
    ///
    /// Her adım kendi hatasını yakalar ve pas devam eder: beşinci adım
    /// ikincinin hatası yüzünden atlanırsa, iki kusur birden görünmez
    /// olur. Sonda tek bir özet yazılır.
    /// </summary>
    public static class VarlikPasi
    {
        [MenuItem("Hezarfen/Boru Hatti/Varlik pasini kos")]
        public static void Kos()
        {
            var sb = new StringBuilder("VARLIK PASI\n");
            int hata = 0;

            hata += Adim(sb, "1 malzemeler",
                         OttomanMaterialBuilder.BuildMenu);
            // KARAKTERLER _Import'TAN ONCE ALINIR.
            //
            // Sira ilk yazimda "malzeme -> _Import -> LOD -> ..." idi ve
            // ilk kosumda bedeli goruldu: `_Import` icinde hem statik
            // mesh hem de SK_ karakterleri vardi; `ImportLanding` hepsini
            // statik varlik sanip `Art/Models/` altina duz duz tasidi ve
            // yedi NPC icin ikinci bir prefab ailesi uretti
            // (`PF_SK_Sakin_*`), oysa dogrusu `Art/Models/Karakter/` ve
            // `PF_Sakin_*`. Kimse hata vermedi — iki prefab ailesi sessizce
            // yan yana durdu.
            //
            // `KarakterLanding` de `_Import`tan okur; once o kosarsa
            // karakterleri alir ve geriye `ImportLanding`in isi kalir.
            // Iki adim ayni klasoru paylasiyor, o yuzden SIRA bir zevk
            // degil bir sozlesme.
            hata += Adim(sb, "2 karakterler (Humanoid)",
                         KarakterLanding.Place);
            hata += Adim(sb, "3 _Import'u yerlestir",
                         ImportLanding.PromoteAllMenu);
            hata += Adim(sb, "4 LOD merdiveni",
                         ImportLanding.ApplyLodLadderMenu);
            hata += Adim(sb, "5 LOD gecisi",
                         Hezarfen.Editor.Lighting.LodGecisi.Uygula);
            hata += Adim(sb, "6 animator",
                         AnimatorKur.Uret);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            sb.Append(hata == 0
                          ? "-> pas temiz."
                          : $"-> {hata} adim HATA verdi (yukarida).");
            if (hata == 0) Debug.Log("[Hezarfen] " + sb);
            else Debug.LogError("[Hezarfen] " + sb);
        }

        /// <summary>
        /// Tek adımı koşar, hatasını yutmadan kaydeder ve pası
        /// durdurmaz. Dönüş: hata sayısı (0 ya da 1).
        /// </summary>
        private static int Adim(StringBuilder sb, string ad, Action is_)
        {
            try
            {
                is_();
                sb.Append($"  {ad}: tamam\n");
                return 0;
            }
            catch (Exception e)
            {
                sb.Append($"  {ad}: HATA {e.GetType().Name} {e.Message}\n");
                return 1;
            }
        }
    }
}
