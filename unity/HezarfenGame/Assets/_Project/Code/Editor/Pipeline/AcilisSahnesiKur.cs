using Hezarfen.Arayuz;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// Açılış menüsü sahnesini <b>koddan</b> kurar.
    ///
    /// Elle kurulmuş bir sahne, kurulumunun gerekçesini taşımaz ve bir
    /// daha üretilemez. Bu projedeki her sahne gibi bu da bir komuttan
    /// doğuyor (ADR 0005'in mantığı).
    /// </summary>
    public static class AcilisSahnesiKur
    {
        public const string ScenePath = "Assets/_Project/Scenes/Acilis.unity";

        [MenuItem("Hezarfen/Boru Hatti/Acilis sahnesini kur")]
        public static void KurMenu()
        {
            var sahne = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Kamera: menude sehir yok, duz bir zemin rengi yeter.
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Gece mavisi degil, MUREKKEP: donem gravurlerinin kagit ve
            // mürekkep dunyasi. Menude sehir gostermek yerine bir kitap
            // sayfasi hissi.
            cam.backgroundColor = new Color(0.09f, 0.08f, 0.07f);
            camGo.AddComponent<AudioListener>();

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var olcek = canvasGo.AddComponent<CanvasScaler>();
            olcek.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            olcek.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<
                UnityEngine.EventSystems.StandaloneInputModule>();

            var menu = Panel(canvasGo.transform, "MenuPaneli");
            var yukleme = Panel(canvasGo.transform, "YuklemePaneli");
            yukleme.SetActive(false);

            // --- MENU ---
            Yazi(menu.transform, "Baslik", "HEZARFEN", 96,
                 new Vector2(0f, 180f), new Vector2(900f, 130f));
            Yazi(menu.transform, "AltBaslik", "1632 · İstanbul", 40,
                 new Vector2(0f, 90f), new Vector2(900f, 60f));

            var acilis = canvasGo.AddComponent<AcilisMenusu>();
            acilis.menuPaneli = menu;
            acilis.yuklemePaneli = yukleme;

            var basla = Dugme(menu.transform, "BaslaDugme", "Başla",
                              new Vector2(0f, -40f));
            basla.onClick.AddListener(acilis.Basla);

            var cik = Dugme(menu.transform, "CikDugme", "Çık",
                            new Vector2(0f, -130f));
            cik.onClick.AddListener(acilis.Cik);

            // --- YUKLEME ---
            acilis.ilerlemeYazi = Yazi(yukleme.transform, "IlerlemeYazi",
                "Şehir yükleniyor…", 36, new Vector2(0f, 40f),
                new Vector2(900f, 60f));

            var cubukGo = new GameObject("Ilerleme",
                typeof(RectTransform), typeof(Slider));
            cubukGo.transform.SetParent(yukleme.transform, false);
            var cubukRt = cubukGo.GetComponent<RectTransform>();
            cubukRt.sizeDelta = new Vector2(700f, 18f);
            cubukRt.anchoredPosition = new Vector2(0f, -20f);
            var slider = cubukGo.GetComponent<Slider>();
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0f;
            slider.interactable = false;

            var arka = Kutu(cubukGo.transform, "Arka",
                            new Color(0.18f, 0.16f, 0.14f, 1f));
            var dolu = Kutu(cubukGo.transform, "Dolu",
                            new Color(0.78f, 0.68f, 0.45f, 1f));
            slider.targetGraphic = arka;
            slider.fillRect = dolu.rectTransform;
            acilis.ilerleme = slider;

            EditorSceneManager.MarkSceneDirty(sahne);
            EditorSceneManager.SaveScene(sahne, ScenePath);
            Debug.Log($"[Hezarfen] Acilis sahnesi kuruldu: {ScenePath}\n"
                      + "SONRAKI ADIM: Hezarfen -> Boru Hatti -> Build "
                      + "sahne listesini duzelt");
        }

        private static GameObject Panel(Transform ebeveyn, string ad)
        {
            var go = new GameObject(ad, typeof(RectTransform));
            go.transform.SetParent(ebeveyn, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        private static Text Yazi(Transform ebeveyn, string ad, string metin,
                                 int punto, Vector2 poz, Vector2 boyut)
        {
            var go = new GameObject(ad, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(ebeveyn, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = boyut;
            rt.anchoredPosition = poz;
            var t = go.GetComponent<Text>();
            t.text = metin;
            t.fontSize = punto;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.90f, 0.85f, 0.74f);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return t;
        }

        private static Image Kutu(Transform ebeveyn, string ad, Color renk)
        {
            var go = new GameObject(ad, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(ebeveyn, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = renk;
            return img;
        }

        private static Button Dugme(Transform ebeveyn, string ad,
                                    string metin, Vector2 poz)
        {
            var go = new GameObject(ad, typeof(RectTransform), typeof(Image),
                                    typeof(Button));
            go.transform.SetParent(ebeveyn, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 64f);
            rt.anchoredPosition = poz;
            go.GetComponent<Image>().color =
                new Color(0.20f, 0.17f, 0.14f, 1f);
            Yazi(go.transform, "Yazi", metin, 32, Vector2.zero,
                 new Vector2(360f, 64f));
            return go.GetComponent<Button>();
        }
    }
}
