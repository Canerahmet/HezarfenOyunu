using System.Collections;
using Hezarfen.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Karakter hâlâ bir insana benziyor mu?</b>
    ///
    /// ## Neden var
    ///
    /// Bir tur boyunca on üç animasyon klibi yeniden üretildi. Amaç
    /// meşruydu: koşu klibi 3,6 m/s için yapılmıştı, oyun 6,0 m/s
    /// gidiyordu ve ayaklar yerde kayıyordu. Üretim başarılı görünüyordu —
    /// çözülen kayma 1,3 ve 2,0 cm, 366 EditMode testi yeşil, kataloğun
    /// bütün sayıları tutuyordu.
    ///
    /// Oyuna girip <b>bakınca</b> karakter parçalanmıştı: kaftan gövdeyi
    /// yutan içi boş bir silindir, sarık başın yanında ayrı duran bir
    /// halka, kollar çıplak. Hiçbir test kırmızı yanmadı, çünkü bütün
    /// testler <b>sayıları</b> okuyordu — süre, tempo, kayma, kök
    /// yüksekliği — ve bunların hepsi doğruydu.
    ///
    /// Sebep deneyle bulundu: klipler depodaki hâline döndürülünce
    /// karakter düzeldi; yalnız yürüme ve koşma yeni hâlleriyle
    /// bırakılınca <b>yine düzgün kaldı</b>. Yani bozan, yeniden
    /// üretilen öteki on bir klipten biri.
    ///
    /// ## Bu testin ölçtüğü
    ///
    /// Poz doğru mu diye sormuyor — o bir görüş. <b>Siluet insan
    /// zarfında mı</b> diye soruyor, ve bu tek sayıya iner: deri
    /// pişirilir, dünya ölçüleri alınır. Sağlam karakter
    /// 0,79 × 1,80 × 0,62 m. Kaftan gövdeden koparsa genişlik ya da
    /// derinlik zarfı taşar.
    ///
    /// <c>renderer.bounds</c> KULLANILMAZ: deri değişen mesh'te şişiktir
    /// ve sağlam karakterde bile (2,25 × 2,48 × 2,63) okur — yani bozuk
    /// olanı sağlamdan ayırmaz. Bu, bu projede yanlış cetvelin kaçıncı
    /// kez bir kusuru gizlediğinin sayısını bir artırırdı.
    /// </summary>
    public class KarakterSiluetiTests
    {
        private const string Sahne = "Assets/_Project/Scenes/Faz1_Terrain.unity";

        /// <summary>Sağlam ölçü 1,80 m; ölçek sözleşmesi 1,70 m figürüdür.</summary>
        private const float EnAzBoy = 1.60f;
        private const float EnCokBoy = 1.95f;

        /// <summary>Yatayda insan zarfı — kollar açıkken bile 1,20 m yeter.</summary>
        private const float EnCokYatay = 1.20f;

        [UnityTest]
        public IEnumerator TheCharacterStillFitsInsideAHumanEnvelope()
        {
            yield return LoadSceneAsync();

            var oy = Object.FindAnyObjectByType<WalkController>();
            Assert.IsNotNull(oy, "OYUNCU sahnede yok.");
            var smr = oy.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.IsNotNull(smr, "Karakterin deri değişen mesh'i yok.");

            // Animatörün ilk pozunu kurması için birkaç kare.
            for (int i = 0; i < 5; i++) yield return null;

            var mesh = new Mesh();
            smr.BakeMesh(mesh, true);
            var noktalar = mesh.vertices;
            Assert.Greater(noktalar.Length, 0, "Pişirilen mesh boş.");

            var mtx = smr.transform.localToWorldMatrix;
            var enAz = new Vector3(1e9f, 1e9f, 1e9f);
            var enCok = -enAz;
            foreach (var p in noktalar)
            {
                var d = mtx.MultiplyPoint3x4(p);
                enAz = Vector3.Min(enAz, d);
                enCok = Vector3.Max(enCok, d);
            }
            Object.DestroyImmediate(mesh);

            var olcu = enCok - enAz;
            Assert.That(olcu.y, Is.InRange(EnAzBoy, EnCokBoy),
                $"Karakterin boyu {olcu.y:0.00} m — insan değil.");
            Assert.Less(olcu.x, EnCokYatay,
                $"Karakter {olcu.x:0.00} m genişlemiş: bir parçası "
                + "gövdeden kopmuş olabilir (kaftan, sarık).");
            Assert.Less(olcu.z, EnCokYatay,
                $"Karakter {olcu.z:0.00} m derinleşmiş: bir parçası "
                + "gövdeden kopmuş olabilir.");

            // Siluet İSKELETİN üstünde durmalı: kopan parça kütleyi kaydırır.
            var an = oy.GetComponentInChildren<Animator>();
            var kalca = an != null ? an.GetBoneTransform(HumanBodyBones.Hips) : null;
            if (kalca != null)
            {
                var merkez = (enAz + enCok) * 0.5f;
                float kayma = new Vector2(merkez.x - kalca.position.x,
                                          merkez.z - kalca.position.z).magnitude;
                Assert.Less(kayma, 0.35f,
                    $"Siluetin merkezi kalçadan {kayma:0.00} m uzakta — "
                    + "gövdeden kopmuş bir parça var.");
            }
        }

        /// <summary>
        /// <b>Şehri arkamda bırakmam.</b>
        ///
        /// Bu test gerçek oyun sahnesini yükler ve o sahne, koşum bitince
        /// <b>yüklü kalır</b>: içindeki Main Camera, arazi ve binalar
        /// sonraki testlerin dünyasına karışır. Yazıldığı gün tam olarak
        /// bu oldu — <c>NPCYoneticiTests.TheCityActuallySpeaks</c>
        /// kırmızı yandı, çünkü <c>BarkGosterici</c> görüş hattını
        /// <c>Camera.main</c>'den kuruyor ve o kamera artık şehrin
        /// içindeydi: sentetik sahnedeki sakinlere giden ışın gerçek
        /// binalara çarpıyordu.
        ///
        /// Bir testin bıraktığı artık, başka bir testin kusuru gibi
        /// görünür. Sahne boşaltılarak bırakılır.
        /// </summary>
        [UnityTearDown]
        public IEnumerator Temizle()
        {
            // Once BOS bir sahne etkin yapilir: son yuklu sahne
            // bosaltilamaz, Unity buna izin vermez.
            var bos = SceneManager.CreateScene(
                "KARAKTER_SILUET_BOS_" + System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(bos);

            var sehir = SceneManager.GetSceneByPath(Sahne);
            if (sehir.IsValid() && sehir.isLoaded)
            {
                var op = SceneManager.UnloadSceneAsync(sehir);
                if (op != null) while (!op.isDone) yield return null;
            }
            yield return null;
        }

        private static IEnumerator LoadSceneAsync()
        {
            var op = SceneManager.LoadSceneAsync(Sahne, LoadSceneMode.Single);
            Assert.IsNotNull(op, $"Sahne yuklenemedi: {Sahne}");
            while (!op.isDone) yield return null;
            yield return null;
        }
    }
}
