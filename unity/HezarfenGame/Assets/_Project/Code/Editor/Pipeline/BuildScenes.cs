using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// Build sahne listesi — <b>hangi sahne pakete girer.</b>
    ///
    /// ## Neden bir komut, elle bir ayar değil
    ///
    /// Denetim turunda (2026-08-24) listede **tek** sahne vardı ve o da bizim
    /// değildi: <c>Sandbox/OutdoorsScene.unity</c>, HDRP şablonunun boş örnek
    /// sahnesi (dört nesne: kamera, güneş, gökyüzü, ışık probu). Bugün bir şey
    /// bozmuyordu çünkü henüz build almıyoruz; Faz 7'de bu hâliyle **HDRP
    /// örneği paketlenirdi** ve kimse fark etmezdi.
    ///
    /// Elle düzeltmek aynı tuzağı bir yıl sonra tekrar kurardı. Liste artık
    /// koddan geliyor, gerekçesiyle birlikte, ve testle korunuyor.
    ///
    /// ## Semt sahneleri bu listede YOKTUR — ve olmamalı
    ///
    /// <c>Districts/D_*.unity</c> sahneleri <see cref="Hezarfen.Streaming"/>
    /// tarafından <b>Addressables</b> ile yükleniyor
    /// (<c>Addressables.LoadSceneAsync</c>). Addressable bir sahne build
    /// listesine de konursa Unity onu <b>iki kez</b> paketler; liste
    /// dolu görünür ama yayın hattı bozulur. Yayınla ilgili karar ADR 0011'de.
    ///
    /// ## Sandbox sahneleri de yok
    ///
    /// `Faz2_GalataSokagi`, `Faz2_BalatSokagi`, `Faz2_Okmeydani`,
    /// `Bench_*` — hepsi inceleme ve ölçüm sahnesi. Pakete girmezler.
    /// </summary>
    public static class BuildScenes
    {
        /// <summary>
        /// Sırayla; ilki <b>açılış sahnesidir</b>.
        ///
        /// `Faz1_Terrain` önce: gerçek dünya odur (arazi, su, sur/semt
        /// katmanları, yeşil kütleler). `FlightSlice` uçuş grayboxudur ve
        /// Faz 1'in ölçüm sahnesi olarak yaşamaya devam ediyor.
        ///
        /// Gerçek açılış akışı (menü → yükleme → şehir) <b>Faz 7'de
        /// kararlaştırıldı</b>: ilk sahne <c>Acilis</c>, oradan asenkron
        /// olarak <c>Faz1_Terrain</c>.
        /// </summary>
        public static readonly string[] Wanted =
        {
            // FAZ 7 KARARI: acilis MENUDUR, sehir degil.
            //
            // Dogrudan sehre acmak, oyuncuyu arazi + su + sur + semt
            // katmanlari yuklenirken donmus bir ekranla karsilamak
            // demekti; ilk izlenim bir takilma olurdu. Menu sahnesi
            // neredeyse bostur, aninda acilir ve sehri ARKADA yukler.
            //
            // Faz 6'nin "yukleme ekrani yok" olcutu bunu yasaklamaz: o
            // olcut SERBEST DOLASIM icindir — sehirde gezerken ekran
            // kesilmemeli. Acilistaki tek yukleme onun kapsaminda degil.
            "Assets/_Project/Scenes/Acilis.unity",
            "Assets/_Project/Scenes/Faz1_Terrain.unity",
            "Assets/_Project/Scenes/FlightSlice.unity",
        };

        [MenuItem("Hezarfen/Boru Hatti/Build sahne listesini duzelt")]
        public static void ApplyMenu()
        {
            Apply(out string ozet);
            Debug.Log("[Hezarfen] " + ozet);
        }

        /// <summary>
        /// Listeyi düzeltir ve özet döndürür.
        ///
        /// Build hattı da bunu çağırıyor: liste elle bozulmuş olabilir ve
        /// bozuk bir listeyle alınan build sessizce yanlış sahneyle açılır.
        /// İkinci bir kopya yazmak yerine gövde ortak.
        /// </summary>
        public static void Apply(out string ozet)
        {
            ozet = string.Empty;
            int before = EditorBuildSettings.scenes.Length;
            var removed = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
            {
                bool keep = false;
                foreach (string w in Wanted) if (s.path == w) keep = true;
                if (!keep) removed.Add(s.path);
            }

            var list = new List<EditorBuildSettingsScene>();
            var missing = new List<string>();
            foreach (string p in Wanted)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(p) == null)
                { missing.Add(p); continue; }
                list.Add(new EditorBuildSettingsScene(p, true));
            }

            if (list.Count == 0)
            {
                ozet = "Istenen sahnelerin HICBIRI yok — liste "
                       + "bosaltilmadi. Eksik: " + string.Join(", ", missing);
                Debug.LogError("[Hezarfen] " + ozet);
                return;
            }

            EditorBuildSettings.scenes = list.ToArray();
            ozet = $"Build sahne listesi: {before} -> {list.Count}. "
                   + $"Acilis: {list[0].path}."
                   + (removed.Count > 0
                        ? "\n  Cikarilan: " + string.Join(", ", removed) : "")
                   + (missing.Count > 0
                        ? "\n  BULUNAMADI: " + string.Join(", ", missing) : "");
        }
    }
}
