using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hezarfen.Editor.Lighting
{
    /// <summary>
    /// <b>Her semtin kendi prob hacmi</b> — şehrin sıçrama ışığı burada
    /// doğar.
    ///
    /// ## Ölçüm: şehrin dolaylı ışığı yoktu
    ///
    /// Turun üstten çekilen denetim karesinde Sûriçi sokağı simsiyahtı.
    /// Ama içindeki insanlar aydınlıktı ve karanlık bölge <b>her yerde
    /// tıpatıp aynı rengi</b> okuyordu — üstte (37,1 / 16,0 / 0,2),
    /// ortada (36,6 / 15,0 / 0,2), altta (36,4 / 14,7 / 0,2). Gerçek bir
    /// gölge altındaki yüzeye göre değişir; sabit bir renk gölge değil,
    /// <b>hiç ışık almayan yüzey + sis</b> demektir.
    ///
    /// Aynı yerin göz hizasındaki karesi ise normaldi: gölgedeki kaldırım
    /// mavi/kırmızı 0,63, gölgedeki sıva 0,86. <b>Aynı yer, aynı saniye,
    /// iki kamera, iki sonuç.</b> Farkı yaratan tek şey açıydı — yani
    /// dolaylı ışık <b>ekran uzayından</b> geliyordu (SSGI). Göz
    /// hizasında gökyüzü ve güneşli duvarlar ekranda; yukarıdan dar
    /// sokağa bakınca değiller, ve ışık sıfıra düşüyordu.
    ///
    /// Sebep fırının kaydında yazılıydı: pişirme kümesi <b>tek sahne</b>
    /// içeriyordu (<c>singleSceneMode: 1</c>, tek GUID) ve o sahne
    /// binaların olduğu sahne değil, taban sahneydi. Şehir sekiz semt
    /// sahnesinde yaşıyor (35 + 41 + 34 + 27 MB…), taban sahne 1,1 MB.
    /// 2.829.507 prob pişti ve hepsi <b>boş bir 729 × 648 m yamanın</b>
    /// üstündeydi.
    ///
    /// ## Hacim: semtin kendi sınırı, ama yürünen bant kadar yüksek
    ///
    /// Önceki hâl kutuyu <c>YapiSinirlari()</c> ile hesaplıyor ve 600
    /// m'ye kırpıyordu; kendi yorumu da *"bir semt 600 m'yi aşarsa prob
    /// hacmi semt başına BÖLÜNMELİ"* diyordu. Bölme hiç yapılmadı —
    /// ders yazılıydı, iş bağlanmamıştı.
    ///
    /// Önce <c>Mode.Global</c> denendi: hacim sahnenin kendi sınırından
    /// türer ve her pişirmede yeniden hesaplanır, yani eskiyecek bir
    /// sayı kalmaz. Doğru fikirdi ama sınır **tam** sınırdı: minarenin
    /// tepesi, tepenin sırtı, çatının üstü. Prob geometrinin çevresine
    /// konur, yani oralara da konuyordu — ve orada gökyüzü zaten her
    /// şeyi görüyor; sıçrama terimi ölçüm gürültüsü kadar.
    ///
    /// Ölçüm bunun bedelini gösterdi: şehrin tamamı bir oturuşta
    /// pişmiyor (GPU çöküyor, CPU 172 dakikada bitmiyor). Yüksekliği
    /// kırpmak denendi ve <b>elendi</b> — gerekçesi aşağıda, kodun
    /// yanında: tek bir kutu yamacı izleyemez. Çözüm bu yüzden hacmi
    /// küçültmek değil, pişirmeyi <b>semt semt</b> bölmek oldu
    /// (<c>KaliciAydinlatma.TopluPisirSemt</c>).
    ///
    /// ## Neden 3 m aralık
    ///
    /// Sıçrama ışığı <b>alçak frekanslıdır</b>: bir duvarın yansıttığı
    /// ışık metrelerce yumuşak değişir. 1 m aralık küçük bir odada
    /// anlamlı, 10 km'lik bir şehirde yalnız bellek yer. Aralığı üçe
    /// katlamak prob sayısını yaklaşık dokuzda bire indirir ve
    /// kazanılan yerle şehrin <b>tamamı</b> pişebilir. Ölçü sahibi tek:
    /// <see cref="ProbAraligi"/>.
    /// </summary>
    public static class SemtProblari
    {
        public const string SemtDizini = "Assets/_Project/Scenes/Districts";

        /// <summary>Problar arası en az uzaklık (m) — tek sahibi burası.</summary>
        public const float ProbAraligi = 3f;

        // YURUNEN BANTLA SINIRLAMA DENENDI VE ELENDI.
        //
        // Fikir dogruydu: minarenin tepesine prob koymak, gokyuzunun
        // zaten yaptigi isi ikinci kez odemektir. Uygulamasi degildi —
        // tek bir kutunun alt siniri sahnenin EN ALCAK noktasidir ve
        // Galata bir YAMAC: zeminden 24 m, tepedeki her seyi keserdi.
        //
        // Bir kutu bir tepeyi izleyemez. Bunu yapmanin dogru yolu
        // semti izgaraya bolup her hucreyi kendi zeminine oturtmak;
        // o ayri bir isin konusu ve olculmeden yazilmayacak. Sayi
        // yazmadan once bunun kaydi burada duruyor ki ayni fikir
        // ikinci kez ayni hatayla denenmesin.

        /// <summary>Semt sahnelerinin yolları (alfabetik).</summary>
        public static List<string> Semtler()
        {
            if (!Directory.Exists(SemtDizini)) return new List<string>();
            return Directory.GetFiles(SemtDizini, "D_*.unity")
                .Select(y => y.Replace('\\', '/'))
                .OrderBy(y => y).ToList();
        }

        /// <summary>Projedeki pişirme kümesi (tek olmalı).</summary>
        public static ProbeVolumeBakingSet Kume()
        {
            foreach (string guid in AssetDatabase.FindAssets(
                         "t:ProbeVolumeBakingSet"))
            {
                var s = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (s != null) return s;
            }
            return null;
        }

        /// <summary>
        /// <b>Pişirme kümesine bağlanmamış semtler</b> — testin okuduğu
        /// ölçü.
        ///
        /// Sahne dosyasını açmadan cevaplanabilir: kümenin sahne GUID
        /// listesi diskte duruyor. Bağlı olmayan bir semtin probu hiç
        /// pişmez ve orada dolaylı ışık yoktur.
        /// </summary>
        public static List<string> BaglanmamisSemtler()
        {
            var kume = Kume();
            var eksik = new List<string>();
            if (kume == null)
            {
                eksik.Add("Projede hic ProbeVolumeBakingSet yok.");
                return eksik;
            }
            var bagli = new HashSet<string>(kume.sceneGUIDs);
            foreach (string yol in Semtler())
            {
                string guid = AssetDatabase.AssetPathToGUID(yol);
                if (!bagli.Contains(guid))
                    eksik.Add(Path.GetFileNameWithoutExtension(yol));
            }
            return eksik;
        }

        [MenuItem("Hezarfen/Aydinlatma/Semt problarini kur")]
        public static void KurMenu()
        {
            int n = Kur(out string rapor);
            Debug.Log($"[Hezarfen] Semt problari: {n} semt hazir.\n{rapor}\n"
                      + "SONRAKI ADIM: Hezarfen -> Aydinlatma -> Problari pisir");
        }

        /// <summary>
        /// Semt sahnelerini pişirme kümesine bağlar ve her birine
        /// kendi prob hacmini koyar. Sahneler <b>zaten açık olmalı</b>
        /// (toplu pişirme onları ek olarak açar); açık değilse burada
        /// açılır.
        /// </summary>
        public static int Kur(out string rapor)
        {
            var satirlar = new List<string>();
            var kume = Kume();
            if (kume == null)
            {
                rapor = "Pisirme kumesi bulunamadi.";
                return 0;
            }

            // ARALIK VE COK SAHNE KIPI — kumenin kendi alanlari.
            var so = new SerializedObject(kume);
            var aralik = so.FindProperty("minDistanceBetweenProbes");
            if (aralik != null) aralik.floatValue = ProbAraligi;
            var tek = so.FindProperty("singleSceneMode");
            if (tek != null) tek.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(kume);
            satirlar.Add($"Prob araligi {ProbAraligi:0.#} m, cok sahne kipi acik.");

            int n = 0;
            foreach (string yol in Semtler())
            {
                string ad = Path.GetFileNameWithoutExtension(yol);
                var sahne = EditorSceneManager.GetSceneByPath(yol);
                if (!sahne.isLoaded)
                    sahne = EditorSceneManager.OpenScene(
                        yol, OpenSceneMode.Additive);

                string guid = AssetDatabase.AssetPathToGUID(yol);
                bool eklendi = kume.TryAddScene(guid);

                // HACIM SEMTIN KENDI SAHNESINDE YASAR.
                //
                // APV verisi sahne sahne saklanir; hacim baska bir
                // sahnede olsaydi semt akisla gidip geldiginde probu
                // gelmezdi.
                var pv = sahne.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<ProbeVolume>(true))
                    .FirstOrDefault();
                bool yeni = pv == null;
                if (yeni)
                {
                    var go = new GameObject($"PV_{ad}");
                    EditorSceneManager.MoveGameObjectToScene(go, sahne);
                    pv = go.AddComponent<ProbeVolume>();
                }
                // HACIM SEMTIN KENDI SINIRI — VE YUKSEKLIK
                // KIRPILMADI.
                //
                // `Mode.Global` hacmi sahnenin sinirindan turetir ve
                // her pisirmede yeniden hesaplar; eskiyecek bir sayi
                // kalmaz. Sinir TAM sinirdir: minarenin tepesi de icine
                // girer ve orada probun isi yoktur — gokyuzu zaten her
                // seyi goruyor.
                //
                // Yuksekligi "yurunen bant" kadar kirpmayi denedim ve
                // ELEDIM: tek bir kutunun alt siniri sahnenin EN ALCAK
                // noktasidir ve Galata bir YAMAC — zeminden 24 m,
                // tepedeki her seyi keserdi. Bir kutu bir tepeyi
                // izleyemez.
                //
                // Dogru yol semti izgaraya bolup her hucreyi kendi
                // zeminine oturtmaktir; o ayri bir isin konusu ve
                // olculmeden yazilmayacak. Kayit burada duruyor ki ayni
                // fikir ikinci kez ayni hatayla denenmesin. Suanki
                // cozum hacmi kucultmek degil, pisirmeyi SEMT SEMT
                // bolmek (`KaliciAydinlatma.TopluPisirSemt`).
                pv.mode = ProbeVolume.Mode.Global;
                pv.overridesSubdivLevels = false;
                EditorUtility.SetDirty(pv);
                EditorSceneManager.MarkSceneDirty(sahne);
                n++;
                satirlar.Add($"{ad}: hacim {(yeni ? "KURULDU" : "vardi")}, "
                             + $"kumeye {(eklendi ? "EKLENDI" : "zaten bagliydi")}.");
            }

            AssetDatabase.SaveAssets();
            rapor = string.Join("\n", satirlar);
            return n;
        }
    }
}
