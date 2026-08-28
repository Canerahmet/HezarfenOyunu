using System.Linq;
using Hezarfen.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Animator grafiği ile oyunun geri kalanı aynı şeyi mi söylüyor?</b>
    ///
    /// Bir Animator kontrolcüsü ikili bir varlıktır: diff'i okunmaz, elle
    /// düzenlenebilir ve düzenlendiği hiçbir yerde yazmaz. Bu yüzden
    /// üretilerek kuruluyor (`AnimatorKur`) — ve bu yüzden **testle**
    /// bağlanıyor.
    ///
    /// Asıl bağlanan şey grafiğin şekli değil, içindeki <b>sayılar</b>:
    /// karışım düğümleri hız eşikleridir ve o hızlar
    /// <see cref="WalkController"/>'da da yazılıdır. İkisi ayrışırsa
    /// oyuncu yürürken karakter koşar. Hiçbir şey hata vermez; sadece
    /// yanlış görünür ve kimse nedenini bulamaz.
    ///
    /// Bu projede aynı sınıf hata üç kez çıktı: göz yüksekliği
    /// (1,70 boy mu göz mü), LOD merdiveni (test sayıları kopyalamıştı),
    /// kanat alanı (Blender bekçisi Unity'yi göremiyordu).
    /// </summary>
    public class AnimatorGrafigiTests
    {
        private const string Yol =
            "Assets/_Project/Art/Animation/AC_Hezarfen.controller";

        private static AnimatorController Grafik()
        {
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(Yol);
            // Assert.Ignore DEGIL: kontrolcu depoya girer.
            Assert.IsNotNull(ac,
                $"Animator kontrolcusu yok: {Yol}. Uret: "
                + "Hezarfen/Boru Hatti/Animator kontrolcusunu uret");
            return ac;
        }

        private static AnimatorState Durum(AnimatorController ac, string ad)
        {
            foreach (var s in ac.layers[0].stateMachine.states)
                if (s.state.name == ad) return s.state;
            return null;
        }

        /// <summary>Planın istediği her durum grafikte var.</summary>
        [Test]
        public void EveryStateTheFlightNeedsExists()
        {
            var ac = Grafik();
            foreach (string ad in new[] { "Locomotion", "Merdiven", "Kusanma",
                                          "Kalkis", "Suzulme", "Inis",
                                          "Cakilma" })
                Assert.IsNotNull(Durum(ac, ad), $"'{ad}' durumu yok.");
        }

        /// <summary>Parametreler eksiksiz ve doğru tipte.</summary>
        [Test]
        public void TheParametersMatchWhatTheRuntimeSets()
        {
            var ac = Grafik();
            void Var_(string ad, AnimatorControllerParameterType tip)
            {
                var p = ac.parameters.FirstOrDefault(x => x.name == ad);
                Assert.IsNotNull(p, $"'{ad}' parametresi yok — "
                    + "HezarfenAnimator onu her karede yaziyor.");
                Assert.AreEqual(tip, p.type, $"'{ad}' yanlis tipte.");
            }
            Var_("hiz", AnimatorControllerParameterType.Float);
            Var_("tirmaniyor", AnimatorControllerParameterType.Bool);
            Var_("ucuyor", AnimatorControllerParameterType.Bool);
            Var_("pitch", AnimatorControllerParameterType.Float);
            Var_("roll", AnimatorControllerParameterType.Float);
            foreach (string t in new[] { "kusan", "atla", "in", "cakil" })
                Var_(t, AnimatorControllerParameterType.Trigger);
        }

        /// <summary>
        /// Karışım eşikleri <see cref="WalkController"/>'ın hızlarıyla
        /// AYNI sayı.
        ///
        /// Yürüme düğümü 1,4 m/s'deyse ve kontrolcü 1,4 m/s'de
        /// yürütüyorsa karakter tam o hızda yürüme klibini oynar. Biri
        /// değişip öbürü kalırsa oyuncu yürürken karakter koşar — ya da
        /// daha sinsisi, ikisinin arasında kalıp iki klibi yarı yarıya
        /// karıştırır ve ayaklar kayar. Ayak kaymasını Blender'da
        /// sıfırlayıp burada geri getirmek olmaz (ADR 0067).
        /// </summary>
        [Test]
        public void TheBlendThresholdsAreTheWalkControllersOwnSpeeds()
        {
            var ac = Grafik();
            var loco = Durum(ac, "Locomotion");
            Assert.IsNotNull(loco);
            var tree = loco.motion as BlendTree;
            Assert.IsNotNull(tree, "Locomotion bir karisim agaci degil.");
            Assert.AreEqual("hiz", tree.blendParameter);

            var wc = new GameObject("gecici").AddComponent<WalkController>();
            float yurume = wc.walkSpeed, kosma = wc.runSpeed;
            Object.DestroyImmediate(wc.gameObject);

            var esikler = tree.children.Select(c => c.threshold).ToArray();
            Assert.AreEqual(3, esikler.Length,
                "Locomotion uc dugumlu olmali: durus, yurume, kosma.");
            Assert.AreEqual(0f, esikler[0], 0.001f, "Durus dugumu 0'da olmali.");
            Assert.AreEqual(yurume, esikler[1], 0.01f,
                $"Yurume dugumu {esikler[1]} m/s ama WalkController "
                + $"{yurume} m/s'de yurutuyor.");
            Assert.AreEqual(kosma, esikler[2], 0.01f,
                $"Kosma dugumu {esikler[2]} m/s ama WalkController "
                + $"{kosma} m/s'de kosturuyor.");
        }

        /// <summary>
        /// Süzülüş karışımı <b>iki eksenli</b>.
        ///
        /// Tek eksende olsaydı burun aşağı ile sola yatış aynı eksende
        /// yarışırdı ve ikisi aynı anda yapılamazdı — oysa süzülüşün
        /// tamamı o ikisinin bileşimidir.
        /// </summary>
        [Test]
        public void TheGlideBlendIsTwoDimensional()
        {
            var tree = Durum(Grafik(), "Suzulme")?.motion as BlendTree;
            Assert.IsNotNull(tree, "Suzulme bir karisim agaci degil.");
            Assert.AreEqual(BlendTreeType.FreeformCartesian2D, tree.blendType,
                "Suzulme 2D olmali: pitch ve roll bagimsizdir.");
            Assert.AreEqual("pitch", tree.blendParameter);
            Assert.AreEqual("roll", tree.blendParameterY);
            Assert.AreEqual(5, tree.children.Length,
                "Merkez + dort uc poz bekleniyor.");
            // Merkez (0,0) olmali: notr suzulus bir uc poz degildir.
            Assert.IsTrue(tree.children.Any(
                c => c.position.sqrMagnitude < 0.001f),
                "Karisimin merkezinde notr suzulus yok.");
        }

        /// <summary>
        /// Çakılmaya her durumdan gidilebilir.
        ///
        /// Her yerden düşülebilir. Yalnızca uçuştan erişilen bir çakılma,
        /// merdivenden düşen oyuncuyu yürüme animasyonuyla yere yapıştırır.
        /// </summary>
        [Test]
        public void YouCanCrashFromAnywhere()
        {
            var ac = Grafik();
            foreach (string ad in new[] { "Locomotion", "Merdiven", "Kusanma",
                                          "Kalkis", "Suzulme", "Inis" })
            {
                var s = Durum(ac, ad);
                Assert.IsNotNull(s);
                bool var_ = s.transitions.Any(
                    t => t.destinationState != null
                         && t.destinationState.name == "Cakilma");
                Assert.IsTrue(var_, $"'{ad}' durumundan cakilmaya gecis yok.");
            }
        }

        /// <summary>Her durumun bir klibi var — boş durum sessizce donar.</summary>
        [Test]
        public void NoStateIsLeftWithoutMotion()
        {
            var ac = Grafik();
            foreach (var s in ac.layers[0].stateMachine.states)
                Assert.IsNotNull(s.state.motion,
                    $"'{s.state.name}' durumunun klibi yok — o duruma "
                    + "girildiginde karakter T-pozunda donar.");
        }
    }
}
