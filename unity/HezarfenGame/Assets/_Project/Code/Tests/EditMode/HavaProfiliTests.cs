using Hezarfen.Flight;
using NUnit.Framework;
using UnityEngine;

namespace Hezarfen.Tests
{
    /// <summary>
    /// <b>Rüzgâr her yerde AYNI rüzgâr mı?</b>
    ///
    /// PLAN Bölüm 12 "bulut hızı, dalga, ağaç savrulması <b>senkron</b>"
    /// diyor ve bu bir süs değil bir kısıt: bu oyunda rüzgâr efekt değil
    /// <b>ana mekanik</b>. Hezarfen'in uçuşunu mümkün kılan şey lodostur
    /// ve oyuncu onu gökyüzünden okumak zorunda. Bulut bir yöne, dalga
    /// başka yöne giderse oyuncu yanlış yöne atlar — ve oyunu suçlar.
    ///
    /// Bu yüzden ölçülen şey görüntü değil <b>tek kaynak</b>: hepsi aynı
    /// vektörden türüyor mu.
    /// </summary>
    public class HavaProfiliTests
    {
        private static HavaProfili Kur(Ruzgar r, float hiz)
        {
            var go = new GameObject("hava");
            var h = go.AddComponent<HavaProfili>();
            h.ruzgar = r;
            h.hiz = hiz;
            return h;
        }

        [TearDown]
        public void Temizle()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go != null && go.name == "hava") Object.DestroyImmediate(go);
        }

        /// <summary>
        /// <b>Lodos güneybatıdan eser</b> — ve bu belgeli.
        ///
        /// RESEARCH §4.6: Osmanlı rüzgâr gülü yıldız (K), poyraz (KD),
        /// gündoğusu (D), keşişleme (GD), kıble (G), <b>lodos (GB)</b>,
        /// karayel (KB). Ad yanlış yöne bağlanırsa oyunun birinci tasarım
        /// direği yanlış yerden eser.
        /// </summary>
        [Test]
        public void TheWindRoseUsesItsDocumentedDirections()
        {
            Assert.AreEqual(225, (int)Ruzgar.Lodos,
                "Lodos guneybatidan eser (RESEARCH 4.6).");
            Assert.AreEqual(0, (int)Ruzgar.Yildiz, "Yildiz kuzeydir.");
            Assert.AreEqual(45, (int)Ruzgar.Poyraz, "Poyraz kuzeydogudur.");
            Assert.AreEqual(180, (int)Ruzgar.Kible, "Kible guneydir.");
            Assert.AreEqual(315, (int)Ruzgar.Karayel, "Karayel kuzeybatidir.");
        }

        /// <summary>
        /// <b>Rüzgâr geldiği yerle anılır ama gittiği yöne eser.</b>
        ///
        /// Kaynak bunu okçuluk üzerinden açıkça söylüyor: *"ok azimutu =
        /// rüzgârın geldiği azimut + 180°"*. Yani lodos güneybatıdan gelir
        /// ve <b>kuzeydoğuya</b> gider. İşareti ters çevirmek, Galata'dan
        /// atlayan adamı denize değil yamaca sürüklerdi.
        /// </summary>
        [Test]
        public void TheWindIsNamedForWhereItComesFromButBlowsTheOtherWay()
        {
            var h = Kur(Ruzgar.Lodos, 10f);
            var yon = h.Yon;

            // Guneybatidan gelen ruzgar KUZEYDOGUYA gider: +x, +z.
            Assert.Greater(yon.x, 0.5f,
                $"Lodos doguya gitmiyor (x={yon.x:F2}).");
            Assert.Greater(yon.z, 0.5f,
                $"Lodos kuzeye gitmiyor (z={yon.z:F2}).");
            Assert.AreEqual(1f, yon.magnitude, 0.001f, "Yon birim olmali.");
            Assert.AreEqual(45f, h.Azimut(), 0.01f,
                "Lodosun gittigi azimut kuzeydogu (45) olmali.");

            // Kible (guney) KUZEYE gitmeli.
            h.ruzgar = Ruzgar.Kible;
            Assert.Greater(h.Yon.z, 0.99f, "Kible kuzeye gitmiyor.");
        }

        /// <summary>
        /// <b>Uçuş fiziği aynı vektörü alıyor.</b>
        ///
        /// <see cref="WindField"/> "sahnedeki rüzgârın tek kaynağı" olduğunu
        /// söylüyor; hava profili onu <b>beslemek</b> zorunda, yanına ikinci
        /// bir rüzgâr koymak değil.
        /// </summary>
        [Test]
        public void TheFlightPhysicsGetsTheSameVector()
        {
            var ayar = ScriptableObject.CreateInstance<WindTuning>();
            var h = Kur(Ruzgar.Lodos, 12f);
            h.ayar = ayar;
            h.Uygula();

            Assert.AreEqual(h.Vektor.x, ayar.globalWind.x, 0.001f);
            Assert.AreEqual(h.Vektor.z, ayar.globalWind.z, 0.001f);
            Assert.AreEqual(12f, ayar.globalWind.magnitude, 0.01f,
                "Ucus fizigi baska bir hiz goruyor.");
            Object.DestroyImmediate(ayar);
        }

        /// <summary>
        /// <b>Ağaç kancası da aynı vektörü yayınlıyor.</b>
        ///
        /// Ağaç malzemesi bunu henüz okumuyor (savrulma bir vertex shader'ı
        /// ister ve ağaçlar dokusuz katı geometri — ADR 0019). Kanca yine
        /// de şimdi kuruldu: shader geldiğinde rüzgârın <b>ikinci bir
        /// sahibi</b> doğmasın diye.
        /// </summary>
        [Test]
        public void TheTreeHookPublishesTheSameVector()
        {
            var h = Kur(Ruzgar.Poyraz, 7f);
            h.Uygula();

            var v = Shader.GetGlobalVector(HavaProfili.RuzgarKimlik);
            Assert.AreEqual(h.Yon.x, v.x, 0.001f);
            Assert.AreEqual(h.Yon.z, v.z, 0.001f);
            Assert.AreEqual(7f, v.w, 0.001f,
                "Shader'a giden hiz profildekinden farkli.");
        }

        /// <summary>
        /// <b>Rüzgâr değişince HEPSİ birlikte değişiyor.</b>
        ///
        /// Asıl sınanan şey bu: tek bir alanı değiştirmek bütün
        /// dinleyicileri aynı anda döndürmeli. Biri geride kalırsa sahne
        /// oyuncuya iki farklı rüzgâr gösterir.
        /// </summary>
        [Test]
        public void ChangingTheWindTurnsEverythingAtOnce()
        {
            var ayar = ScriptableObject.CreateInstance<WindTuning>();
            var h = Kur(Ruzgar.Lodos, 9f);
            h.ayar = ayar;
            h.Uygula();

            var lodosFizik = ayar.globalWind;
            var lodosShader = Shader.GetGlobalVector(HavaProfili.RuzgarKimlik);

            h.ruzgar = Ruzgar.Karayel;   // kuzeybatiya don
            h.Uygula();

            var karayelFizik = ayar.globalWind;
            var karayelShader = Shader.GetGlobalVector(HavaProfili.RuzgarKimlik);

            // TEK BILESENE BAKMA. Ilk yazimda yalnizca x karsilastirildi
            // ve test patladi: lodos kuzeydoguya, karayel guneydoguya
            // eser — ikisinin de x'i +0,707. Degisen sey z. Vektorun
            // kendisi olculmeli, bir bileseni degil.
            Assert.Greater((lodosFizik - karayelFizik).magnitude, 1f,
                "Ruzgar degisti ama ucus fizigi ayni kaldi.");
            Assert.Greater(
                ((Vector3)lodosShader - (Vector3)karayelShader).magnitude, 0.5f,
                "Ruzgar degisti ama agac kancasi ayni kaldi.");

            // Ve ikisi HALA birbiriyle ayni yone bakiyor.
            Vector3 shaderYon = new Vector3(karayelShader.x, karayelShader.y,
                                            karayelShader.z);
            Assert.AreEqual(1f,
                Vector3.Dot(karayelFizik.normalized, shaderYon), 0.001f,
                "Fizik ve agac ayri yonlere bakiyor.");
            Object.DestroyImmediate(ayar);
        }
    }
}
