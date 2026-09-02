using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hezarfen.Sehir;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Tests.EditMode
{
    /// <summary>
    /// <b>Kalabalıkta kaç ayrı insan var</b> — göz kararıyla değil sayarak.
    ///
    /// Caner'in isteği açıktı: *"npcler birbirinin aynısı olmasın, her
    /// biri birbirinden ayrışsın, gerçek hayattaki gibi."* Bu, ölçülmeden
    /// "yapıldı" denemeyecek bir istek: yedi gövde üretilmiş olabilir,
    /// dokuz kumaş rengi olabilir, ve ekrandaki altmış kişi yine
    /// birbirinin kopyası görünebilir — çünkü göze çarpan şey parça
    /// sayısı değil <b>bileşimdir</b>.
    ///
    /// Ölçü şu: bir kişinin görünüşünü ayırt eden şeyleri (gövde
    /// arketipi, boy, ten tonu, her giysinin kendi tonu) kabaca
    /// nicemleyip bir <b>imza</b> yaz. Aynı imzayı taşıyan iki kişi
    /// sokakta birbirinin aynısı görünür. Eşik, aynı anda görünen kişi
    /// sayısı üzerinden konur (<c>govdeButcesi</c> = 60), çünkü asıl
    /// soru "kaç varyant ürettik" değil, <b>"aynı karede kaç ikiz
    /// var"</b>.
    /// </summary>
    public class KalabalikCesitliligiTests
    {
        private const string PrefabDir = "Assets/_Project/Art/Prefabs";

        /// <summary>Bir sakinin görünüşünü kabaca özetleyen imza.</summary>
        private static string Imza(GameObject[] arketipler, int tohum)
        {
            var dna = InsanDNA.Uret(tohum);
            int tur = NPCYonetici.ArketipSec(arketipler, dna);
            var sg = arketipler[tur].GetComponent<SakinGovde>();

            // Boy 3 cm'lik kovalarda: bundan ince bir fark sokakta
            // ayirt edilmez.
            float boy = sg != null && sg.tabanBoy > 0.1f
                ? sg.tabanBoy * Mathf.Clamp(dna.boy / sg.tabanBoy, 0.88f, 1.12f)
                : dna.boy;
            var s = new System.Text.StringBuilder();
            s.Append(tur).Append('|').Append(Mathf.RoundToInt(boy / 0.03f));

            // Kafa orani: silüetin en okunur farki. 20 kova.
            s.Append('|').Append(Mathf.RoundToInt(dna.kafa.y * 20f))
             .Append(':').Append(Mathf.RoundToInt(dna.kafa.x * 20f));

            // Ten: kanal basina 10 kova.
            s.Append('|').Append(Mathf.RoundToInt(dna.ten.r * 10f))
             .Append(Mathf.RoundToInt(dna.ten.g * 10f))
             .Append(Mathf.RoundToInt(dna.ten.b * 10f));

            // Her giysinin KENDI tonu — asil cesitlilik burada.
            foreach (string ad in new[] { "M_Cloth_Entari", "M_Cloth_Salvar",
                                          "M_Cloth_Kusak", "M_Cloth_Ferace" })
            {
                var c = NPCYonetici.MalzemeTonu(Color.white, dna.ton, ad);
                Color.RGBToHSV(c, out float h, out float sat, out _);
                s.Append('|').Append(Mathf.RoundToInt(h * 24f))
                 .Append(':').Append(Mathf.RoundToInt(sat * 8f));
            }
            return s.ToString();
        }

        private static GameObject[] Arketipler()
        {
            return AssetDatabase
                .FindAssets("PF_Sakin_ t:Prefab", new[] { PrefabDir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(y => Path.GetFileName(y).StartsWith("PF_Sakin_"))
                .OrderBy(y => y)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(g => g != null).ToArray();
        }

        [Test]
        public void SixtyPeopleOnScreenAreNotEachOthersCopies()
        {
            var hepsi = Arketipler();
            if (hepsi.Length == 0) Assert.Inconclusive("arketip yok");

            // 60 = `NPCYonetici.govdeButcesi`, yani ayni anda gorunen
            // en cok kisi. Sorunun sorulmasi gereken kume bu.
            const int n = 60;
            int enKotu = int.MaxValue;
            string ornek = "";
            for (int tur = 0; tur < 40; tur++)
            {
                var imzalar = new HashSet<string>();
                for (int i = 0; i < n; i++)
                    imzalar.Add(Imza(hepsi, tur * 9973 + i * 7919 + 13));
                if (imzalar.Count < enKotu)
                {
                    enKotu = imzalar.Count;
                    ornek = $"tur {tur}";
                }
            }

            // 60 kisilik bir kalabalikta en az 54 ayri gorunus: yani en
            // cok uc cift ikiz. Ucten fazlasi goze "kopyalanmis
            // kalabalik" olarak carpar — sikayetin kendisi buydu.
            // Esik 54'ten 58'e cikti: kafa orani eklendikten sonra
            // olculen en kotu kare 58/60 verdi. Bir esigi olculen
            // degerin ALTINDA birakmak, kazanilan sey geri gittiginde
            // testin susmasi demektir.
            Assert.GreaterOrEqual(enKotu, 58,
                $"En kotu karede 60 kisiden yalniz {enKotu} tanesi ayri "
                + $"gorunuyor ({ornek}) — geri kalani birbirinin kopyasi.");
        }

        [Test]
        public void SkinToneVariesAcrossThePopulation()
        {
            // TEN CESITLILIGI AYRICA OLCULUR.
            //
            // Yukaridaki imza testi ten sabit kalsa bile giysi tonlariyla
            // gecerdi; oysa bir kalabalikta en cok degisen sey tendir ve
            // bu proje uzun sure herkesi ayni tende gezdirdi.
            var kovalar = new HashSet<int>();
            float enAz = 9f, enCok = 0f;
            for (int i = 0; i < 400; i++)
            {
                var dna = InsanDNA.Uret(i * 104729 + 7);
                float v = (dna.ten.r + dna.ten.g + dna.ten.b) / 3f;
                enAz = Mathf.Min(enAz, v);
                enCok = Mathf.Max(enCok, v);
                kovalar.Add(Mathf.RoundToInt(v * 20f));
            }
            Assert.GreaterOrEqual(kovalar.Count, 8,
                $"Ten yalniz {kovalar.Count} kovaya dagiliyor — sehirdeki "
                + "herkes neredeyse ayni tende.");
            Assert.Greater(enCok - enAz, 0.35f,
                $"Ten araligi {enCok - enAz:0.00} — acikla koyu arasindaki "
                + "fark bir kalabalikta bundan buyuktur.");
        }
    }
}
