using Hezarfen.Arayuz;
using UnityEditor;
using UnityEditor.Events;
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

            // GIRDI MODULU: InputSystemUIInputModule, StandaloneInputModule
            // DEGIL.
            //
            // UGUI'nin varsayilan modulu eski `UnityEngine.Input` sinifini
            // okuyor; proje Input System'e gecmis durumda ve o sinif
            // CALISMA ZAMANINDA istisna atiyor. Derleme sessiz, hata
            // oyunda: menu acilir, dugmeler TIKLANMAZ.
            //
            // Ayni tuzak Faz 5 kapisinda da yakalanmisti.
            var esGo = new GameObject("EventSystem");
            var olayDizgesi = esGo
                .AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<
                UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            var menu = Panel(canvasGo.transform, "MenuPaneli");
            var yukleme = Panel(canvasGo.transform, "YuklemePaneli");
            yukleme.SetActive(false);

            // --- MENU ---
            Yazi(menu.transform, "Baslik", "HEZARFEN", 96,
                 new Vector2(0f, 180f), new Vector2(900f, 130f));
            Yazi(menu.transform, "AltBaslik", "1632 · İstanbul", 40,
                 new Vector2(0f, 90f), new Vector2(900f, 60f));

            // SURUM DAMGASI — kose yazisi.
            //
            // Hangi build'in oynandigi ekranda yazmayinca bir tur boyunca
            // "duzeltme tutmadi mi, eski build mi" ayirt edilemedi.
            // Metin BURADA yazilmaz; AcilisMenusu onu calisma zamaninda
            // okur (damgayi build adimi uretir, bu sahne ondan once
            // kaydedilir).
            var damga = Yazi(menu.transform, "SurumYazi", "", 18,
                             new Vector2(0f, -500f), new Vector2(900f, 30f));
            damga.color = new Color(0.55f, 0.50f, 0.42f);

            var acilis = canvasGo.AddComponent<AcilisMenusu>();
            acilis.menuPaneli = menu;
            acilis.yuklemePaneli = yukleme;
            acilis.surumYazi = damga;

            // DINLEYICILER KALICI OLMALI — AddListener SAHNEYE YAZILMAZ.
            //
            // `onClick.AddListener(...)` bir CALISMA ZAMANI dinleyicisi
            // ekler. Editorde kurulum sirasinda cagrilinca dugme o an
            // calisir gibi gorunur, ama sahne kaydedilirken o dinleyici
            // serilestirilmez: sahne bir daha acildiginda dort dugme de
            // BOSA basar. Menu kusursuz gorunur ve hicbir sey olmaz —
            // Caner'in build'de yasadigi tam olarak buydu.
            //
            // `UnityEventTools.AddPersistentListener` ise hedefi ve metot
            // adini sahneye yazar; kalici dinleyici sayisi olculebilir ve
            // bir test onu tutuyor (AcilisMenusuTests).
            //
            // Ders eskisiyle ayni: menuyu "dogruladigimda" panelleri
            // KODDAN cagirmistim (m.KredileriAc()), yani tiklama yolunu
            // hic sinamamistim. Olculmeyen yol, olmayan yoldur.

            var basla = Dugme(menu.transform, "BaslaDugme", "Başla",
                              new Vector2(0f, -20f));
            UnityEventTools.AddPersistentListener(basla.onClick,
                                               acilis.Basla);

            // DEVAM ET — VE NEDEN YOKTU.
            //
            // Menude dort dugme vardi ve hicbiri kaydi acmiyordu.
            // Dunku oturumunu surdurmek isteyen oyuncu **Basla**'ya
            // basmak, sehri bastan yuklemek, varsayilan noktada dogmak
            // ve sonra ekranin kosesindeki tus satirindan F9'u ogrenip
            // ona basmak zorundaydi. Kayit sistemi on iki alan tutuyor,
            // Perde 2 ilerlemesi dahil — ve menude bir yuzu yoktu.
            //
            // Dugme her zaman KURULUR ama kayit yoksa GORUNMEZ:
            // olmayan bir kaydi sunmak, olmayan bir secenek sunmaktir.
            var devam = Dugme(menu.transform, "DevamDugme", "Devam et",
                              new Vector2(0f, -95f));
            UnityEventTools.AddPersistentListener(devam.onClick,
                                               acilis.DevamEt);
            acilis.devamDugmesi = devam.gameObject;

            var ayarDugme = Dugme(menu.transform, "AyarDugme", "Ayarlar",
                                  new Vector2(0f, -170f));
            UnityEventTools.AddPersistentListener(ayarDugme.onClick,
                                               acilis.AyarlariAc);

            var krediDugme = Dugme(menu.transform, "KrediDugme", "Krediler",
                                   new Vector2(0f, -245f));
            UnityEventTools.AddPersistentListener(krediDugme.onClick,
                                               acilis.KredileriAc);

            var cik = Dugme(menu.transform, "CikDugme", "Çık",
                            new Vector2(0f, -320f));
            UnityEventTools.AddPersistentListener(cik.onClick,
                                               acilis.Cik);

            // KLAVYE ICIN SECILI BIR NESNE SART.
            //
            // Fare olmadan "Enter" bir yere basmaz: Submit eylemi
            // EventSystem'in SECILI nesnesine gider ve secili nesne yoksa
            // hicbir yere gitmez. Caner "tuslara bastim, giremedim" derken
            // menude secili hicbir sey YOKTU.
            olayDizgesi.firstSelectedGameObject = basla.gameObject;
            acilis.ilkSecim = basla.gameObject;

            // --- AYARLAR ---
            var ayarlar = Panel(canvasGo.transform, "AyarlarPaneli");
            ayarlar.SetActive(false);
            acilis.ayarlarPaneli = ayarlar;

            Yazi(ayarlar.transform, "AyarBaslik", "AYARLAR", 56,
                 new Vector2(0f, 260f), new Vector2(900f, 80f));

            // Kademe adlari OLDUGU GIBI: olculen sey ile menude yazan sey
            // ayrilirsa oyuncu neden 50 FPS aldigini anlamaz.
            for (int i = 0; i < UnityEngine.QualitySettings.names.Length
                            && i < 3; i++)
            {
                int k = i;
                var d = Dugme(ayarlar.transform, $"Kademe{i}",
                              UnityEngine.QualitySettings.names[i],
                              new Vector2(0f, 140f - i * 75f));
                // Lambda serilestirilemez; int argumanli kalici
                // dinleyici sahneye yaziliyor.
                UnityEventTools.AddIntPersistentListener(
                    d.onClick, acilis.KademeSec, k);
            }

            acilis.kademeYazi = Yazi(ayarlar.transform, "KademeYazi",
                Hezarfen.Arayuz.Ayarlar.KademeAciklamasi[
                    Mathf.Clamp(Hezarfen.Arayuz.Ayarlar.Kademe, 0, 2)],
                24, new Vector2(0f, -70f), new Vector2(1100f, 60f));

            var ayarGeri = Dugme(ayarlar.transform, "AyarGeri", "Geri",
                                 new Vector2(0f, -200f));
            UnityEventTools.AddPersistentListener(ayarGeri.onClick,
                                               acilis.Geri);

            // --- KREDILER ---
            //
            // ODbL atfi HUKUKI yukumluluk; metin sabitten geliyor ve bir
            // test icerigini sinar (Krediler.ZorunluAtif).
            var krediler = Panel(canvasGo.transform, "KrediPaneli");
            krediler.SetActive(false);
            acilis.krediPaneli = krediler;

            // PUNTO OLCULEREK: metin ~60 satir ve 18 punto/820 px
            // kutuda TASIYORDU — ekranda ortasindan basliyordu. Krediler
            // tasarsa atif GORUNMEZ olur ve gorunmeyen bir atif, olmayan
            // bir atiftir.
            var krediYazi = Yazi(krediler.transform, "KrediMetin",
                Hezarfen.Arayuz.Krediler.Metin, 15,
                new Vector2(0f, 30f), new Vector2(1560f, 960f));
            krediYazi.alignment = TextAnchor.UpperLeft;
            krediYazi.resizeTextForBestFit = true;
            krediYazi.resizeTextMinSize = 9;
            krediYazi.resizeTextMaxSize = 16;

            var krediGeri = Dugme(krediler.transform, "KrediGeri", "Geri",
                                  new Vector2(0f, -505f));
            UnityEventTools.AddPersistentListener(krediGeri.onClick,
                                               acilis.Geri);

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
