using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Player
{
    /// <summary>
    /// <b>Animasyon kliplerinin kök yüksekliğini ayağa bağlar.</b>
    ///
    /// ## Belirti
    ///
    /// Animatör bağlanır bağlanmaz karakter <b>yere gömüldü</b>: turda
    /// çekilen karede göğsüne kadar toprağın içindeydi.
    ///
    /// ## Ölçüm
    ///
    /// Gerçek köşelerle (renderer'ın şişirilmiş kutusuyla değil — o
    /// 2,48 m diyordu ve yanlış cetveldi):
    ///
    /// <code>
    ///   bind pozu   : ayak +0,00 m, boy 1,79   ← model DOĞRU
    ///   animasyonlu : ayak −0,97 m, boy 1,80   ← animasyon indiriyor
    ///   Hips        : kökten −0,06 m            ← kalça YERDE
    /// </code>
    ///
    /// Kalçanın yerde olması işi ele veriyor: kalça bir yetişkinde ~0,95 m
    /// yukarıdadır. Yani klip, kökü kalçaya oturtuyor.
    ///
    /// ## Sebep
    ///
    /// Klip <c>.meta</c>'larında <c>heightFromFeet: 0</c> ve
    /// <c>keepOriginalPositionY: 0</c>. Humanoid'de ikisi birden sıfırken
    /// <b>Root Transform Position (Y) = Center of Mass</b> demektir ve
    /// Unity gövdeyi ağırlık merkezine göre hizalar — yaklaşık kalça boyu
    /// kadar aşağı.
    ///
    /// <c>CharacterController</c> ile sürülen bir karakterde doğru referans
    /// <b>ayak</b>tır: kapsülün tabanı nerede duruyorsa ayaklar orada
    /// olmalı. Aksi halde çarpışma bir yerde, görüntü bir metre aşağıda
    /// olur — ve bu oturumun tekrar eden dersi tam olarak buydu: bir
    /// konumun iki sahibi.
    /// </summary>
    public static class KarakterKlipleri
    {
        private const string Klasor = "Assets/_Project/Art/Models/Karakter";

        [MenuItem("Hezarfen/Boru Hatti/Karakter kliplerini ayaga bagla")]
        public static void Bagla()
        {
            int dosya = 0, klip = 0, degisen = 0;

            foreach (string guid in AssetDatabase.FindAssets(
                         "t:Model", new[] { Klasor }))
            {
                string yol = AssetDatabase.GUIDToAssetPath(guid);
                var mi = AssetImporter.GetAtPath(yol) as ModelImporter;
                if (mi == null) continue;

                var klipler = mi.clipAnimations;
                if (klipler == null || klipler.Length == 0)
                {
                    // Klip listesi bos ise varsayilanlar kullaniliyor;
                    // once onlari alip yazmak gerekir, yoksa ayar
                    // "kaydedildi" gorunup hicbir seye uygulanmaz.
                    klipler = mi.defaultClipAnimations;
                    if (klipler == null || klipler.Length == 0) continue;
                }
                dosya++;

                bool dokunuldu = false;
                for (int i = 0; i < klipler.Length; i++)
                {
                    klip++;
                    if (klipler[i].heightFromFeet
                        && !klipler[i].keepOriginalPositionY) continue;
                    klipler[i].heightFromFeet = true;
                    klipler[i].keepOriginalPositionY = false;
                    klipler[i].heightOffset = 0f;
                    dokunuldu = true;
                    degisen++;
                }

                if (!dokunuldu) continue;
                mi.clipAnimations = klipler;
                EditorUtility.SetDirty(mi);
                mi.SaveAndReimport();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Hezarfen] Klip kok yuksekligi: {dosya} dosya, "
                      + $"{klip} klip tarandi, {degisen} tanesi AYAGA "
                      + "baglandi.");
        }

        /// <summary>
        /// Ayağa bağlanmamış klip sayısı — test okur.
        /// "Komutu çalıştırdım" demek yetmez; kaçının kaldığı yazmalı.
        /// </summary>
        public static int AyagaBaglanmamis()
        {
            int n = 0;
            foreach (string guid in AssetDatabase.FindAssets(
                         "t:Model", new[] { Klasor }))
            {
                var mi = AssetImporter.GetAtPath(
                    AssetDatabase.GUIDToAssetPath(guid)) as ModelImporter;
                if (mi == null) continue;
                var klipler = mi.clipAnimations;
                if (klipler == null || klipler.Length == 0)
                    klipler = mi.defaultClipAnimations;
                if (klipler == null) continue;
                foreach (var k in klipler)
                    if (!k.heightFromFeet || k.keepOriginalPositionY) n++;
            }
            return n;
        }
    }
}
