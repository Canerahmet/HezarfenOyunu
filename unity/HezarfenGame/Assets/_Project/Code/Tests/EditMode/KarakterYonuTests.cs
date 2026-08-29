using Hezarfen.Editor.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Karakter ileri bakıyor mu.</b>
    ///
    /// Bu dosya bir dersin üstüne yazıldı. Caner omuz üstü kamerasının
    /// "önden baktığını" söyledi. Kamerayı ölçtüm: oyuncunun <b>3,20 m
    /// arkasındaydı</b>, yani doğruydu. İki kare aldım: oyuncunun
    /// <b>önünden</b> bakınca <b>ense</b>, <b>arkasından</b> bakınca
    /// <b>yüz</b> görünüyordu. Ters olan model'di.
    ///
    /// O sırada <c>KameraKipi</c>'nin beş testi yeşildi ve hepsi doğruydu —
    /// kameranın gövdenin arkasında olduğunu sınıyorlardı. Modelin hangi
    /// yöne baktığını sınayan <b>hiçbir test yoktu</b>. Ölçülmeyen yön,
    /// olmayan yöndür.
    ///
    /// ## Neden burun
    ///
    /// Gövde önden-arkadan neredeyse simetrik: ayak +0,275 / −0,271, baş
    /// +0,111 / −0,107, göğüs +0,182 / −0,169. Kaba ölçü yön söylemiyor.
    /// Yüz hizasında merkez şeridinde ise burun keskin bir asimetri veriyor
    /// ve ölçülebiliyor.
    /// </summary>
    public class KarakterYonuTests
    {
        private static readonly string[] Prefablar =
        {
            "Assets/_Project/Art/Prefabs/PF_Hezarfen_Sivil.prefab",
            "Assets/_Project/Art/Prefabs/PF_Hezarfen_Ucus.prefab",
        };

        /// <summary>
        /// <b>Burun +Z'de.</b> CLAUDE.md'nin ekseni: prefabın önü +Z.
        /// Karakter geriye bakarsa üçüncü şahıs kamerası oyuncuya sürekli
        /// yüzünü gösterir ve şehirdeki bütün NPC'ler geri geri yürür —
        /// aynı prefab hepsine gidiyor.
        /// </summary>
        [Test]
        public void TheCharacterFacesForward([ValueSource(nameof(Prefablar))] string yol)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
            Assert.IsNotNull(pf, $"{yol} yok.");

            float yon = KarakterYonu.YuzYonu(pf);
            Assert.IsFalse(float.IsNaN(yon),
                $"{pf.name}: yon olculemedi — SkinnedMeshRenderer yok mu?");
            Assert.Greater(yon, 0f,
                $"{pf.name} GERIYE bakiyor ({yon:+0.000;-0.000} m). Omuz ustu "
                + "kamerasi oyuncuya yuzunu gosterir ve butun NPC'ler geri "
                + "geri yurur. Duzeltme: Hezarfen -> Boru Hatti -> "
                + "Karakter yonunu duzelt.");
        }

        /// <summary>
        /// <b>Ölçüm gerçekten yön ölçüyor.</b> Prefabı 180° çevirince
        /// işaret dönmeli; dönmüyorsa ölçüt yönü değil başka bir şeyi
        /// okuyor demektir ve yeşil kalması hiçbir şey söylemez.
        /// </summary>
        [Test]
        public void TheRulerItselfDetectsAFlip()
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Prefablar[0]);
            float duz = KarakterYonu.YuzYonu(pf);

            var ornek = (GameObject)PrefabUtility.InstantiatePrefab(pf);
            try
            {
                foreach (Transform t in ornek.transform)
                    t.localRotation = Quaternion.Euler(0f, 180f, 0f)
                                      * t.localRotation;

                float ters = KarakterYonu.YuzYonuOrnekten(ornek);
                Assert.Less(ters, 0f,
                    $"180 derece cevrilmis modelde olcut hala {ters:+0.000;-0.000} "
                    + "veriyor — olcut yonu okumuyor.");
                Assert.Greater(duz, 0f);
            }
            finally
            {
                Object.DestroyImmediate(ornek);
            }
        }
    }
}
