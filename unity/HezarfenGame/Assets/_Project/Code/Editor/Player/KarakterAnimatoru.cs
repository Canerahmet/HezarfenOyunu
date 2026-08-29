using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Player
{
    /// <summary>
    /// <b>Karakterin animatörünü prefaba bağlar.</b>
    ///
    /// Caner (2026-08-29): *"karakter gorunumundeyken kosmaya baslayinca
    /// problem oluyor."*
    ///
    /// Ölçüldü: oyuncunun gövdesindeki <c>Animator</c>'ın
    /// <b>controller'ı YOKTU</b> ve <c>applyRootMotion</c> <b>açıktı</b>.
    /// Yani karakter hiç canlanmıyor; yürürken de koşarken de duruş
    /// pozunda <b>kayıyor</b>. Koşuda göze batmasının sebebi basit: hız
    /// arttıkça kayma da hızlanıyor.
    ///
    /// Aynı prefab NPC'lere de gidiyor, yani şehirdeki herkes aynı şekilde
    /// kayacaktı — <see cref="Hezarfen.Sehir.NPCYonetici"/> gövdeyi alıp
    /// <c>SetFloat("hiz", …)</c> çağırıyor ama karşısında controller yok.
    ///
    /// ## applyRootMotion neden KAPALI
    ///
    /// Karakteri <c>CharacterController</c> hareket ettiriyor. Kök hareketi
    /// açık kalırsa animasyon da hareket ettirmeye çalışır ve ikisi
    /// birbiriyle yarışır: karakter kayar, takılır, yürüyüş hızıyla
    /// animasyon hızı ayrışır. Bir hareketin iki sahibi olamaz.
    /// </summary>
    public static class KarakterAnimatoru
    {
        private const string Controller =
            "Assets/_Project/Art/Animation/AC_Hezarfen.controller";

        private static readonly string[] Prefablar =
        {
            "Assets/_Project/Art/Prefabs/PF_Hezarfen_Sivil.prefab",
            "Assets/_Project/Art/Prefabs/PF_Hezarfen_Ucus.prefab",
        };

        [MenuItem("Hezarfen/Boru Hatti/Karakter animatorunu bagla")]
        public static void Bagla()
        {
            var ac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                Controller);
            if (ac == null)
            {
                Debug.LogError($"[Hezarfen] {Controller} yok — once "
                               + "Boru Hatti -> Animator kontrolcusunu uret.");
                return;
            }

            foreach (string yol in Prefablar)
            {
                var kok = PrefabUtility.LoadPrefabContents(yol);
                try
                {
                    var anim = kok.GetComponentInChildren<Animator>(true);
                    if (anim == null)
                    {
                        Debug.LogWarning($"[Hezarfen] {yol}: Animator yok.");
                        continue;
                    }

                    bool degisti = false;
                    if (anim.runtimeAnimatorController != ac)
                    { anim.runtimeAnimatorController = ac; degisti = true; }
                    if (anim.applyRootMotion)
                    { anim.applyRootMotion = false; degisti = true; }
                    // Ekran disinda da guncellensin: kalabalikta govde
                    // havuzdan alinip yeniden yerlestiriliyor ve
                    // CullCompletely donmus poz birakiyor.
                    if (anim.cullingMode != AnimatorCullingMode.CullUpdateTransforms)
                    { anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                      degisti = true; }

                    if (!degisti)
                    {
                        Debug.Log($"[Hezarfen] {kok.name}: animator zaten bagli.");
                        continue;
                    }
                    PrefabUtility.SaveAsPrefabAsset(kok, yol);
                    Debug.Log($"[Hezarfen] {kok.name}: controller baglandi, "
                              + "kok hareketi kapatildi.");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(kok);
                }
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Prefabın animatörü hazır mı — test okur.
        /// Dönen metin boşsa her şey yolunda; değilse eksik yazıyor.
        /// </summary>
        public static string Eksik(GameObject prefab)
        {
            var anim = prefab.GetComponentInChildren<Animator>(true);
            if (anim == null) return "Animator yok";
            if (anim.runtimeAnimatorController == null)
                return "controller atanmamis — karakter hic canlanmaz, "
                       + "durus pozunda kayar";
            if (anim.applyRootMotion)
                return "applyRootMotion acik — CharacterController ile "
                       + "yarisir, karakter kayar";
            return "";
        }

        /// <summary>Test için prefab yolları.</summary>
        public static string[] PrefabYollari => Prefablar;
    }
}
