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
        /// Gerçek açılış akışı (menü → yükleme → şehir) Faz 7'nin kararıdır;
        /// bu liste o güne kadar <b>doğru</b> olanı tutar, nihai olanı değil.
        /// </summary>
        public static readonly string[] Wanted =
        {
            "Assets/_Project/Scenes/Faz1_Terrain.unity",
            "Assets/_Project/Scenes/FlightSlice.unity",
        };

        [MenuItem("Hezarfen/Boru Hatti/Build sahne listesini duzelt")]
        public static void ApplyMenu()
        {
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
                Debug.LogError("[Hezarfen] Istenen sahnelerin HICBIRI yok — "
                               + "liste bosaltilmadi. Eksik: "
                               + string.Join(", ", missing));
                return;
            }

            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"[Hezarfen] Build sahne listesi: {before} -> {list.Count}. "
                      + $"Acilis: {list[0].path}."
                      + (removed.Count > 0 ? "\n  Cikarilan: " + string.Join(", ", removed) : "")
                      + (missing.Count > 0 ? "\n  BULUNAMADI: " + string.Join(", ", missing) : ""));
        }
    }
}
