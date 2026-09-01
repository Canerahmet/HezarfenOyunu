using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// <b>Animator kontrolcüsü — elle değil, ÜRETİLEREK.</b>
    ///
    /// Bir Animator grafiğini Unity penceresinde kurmak mümkün ve
    /// çoğu proje öyle yapar. Bu projede yapamayız: CLAUDE.md'nin
    /// kuralı "sadece sohbette var olan varlık yasak"tır ve elle
    /// kurulmuş bir durum makinesi de aynı şeydir — kimse onu yeniden
    /// üretemez, diff'i okunamaz, neden öyle olduğu bir yerde yazmaz.
    ///
    /// Bu araç grafiği <b>klip kataloğundan</b> kurar. Klip eklenince
    /// yeniden koşulur ve grafik kendini günceller.
    ///
    /// ## Grafiğin şekli neden bu
    ///
    /// Uçuş oyunu iki ayrı dünyada geçer ve ikisi arasındaki geçiş
    /// oyunun kendisidir:
    ///
    /// <list type="bullet">
    /// <item><b>Yer</b> — locomotion (hız karışımı) ve merdiven. Burada
    ///       ayak yere basar, kayma ölçülür (ADR 0067).</item>
    /// <item><b>Hava</b> — süzülüş, iki eksenli karışım (pitch/roll).
    ///       Burada ayak yoktur; okunacak şey gövdenin havayı nasıl
    ///       kestiğidir.</item>
    /// </list>
    ///
    /// Aradaki köprü <b>tek yönlü</b>: kuşanma → kalkış → süzülüş →
    /// iniş. Çakılma her yerden erişilir çünkü her yerden düşülebilir.
    /// </summary>
    public static class AnimatorKur
    {
        private const string ModelDir = "Assets/_Project/Art/Models/Karakter";
        private const string Cikti =
            "Assets/_Project/Art/Animation/AC_Hezarfen.controller";

        // Parametre adlari TEK YERDE: `HezarfenAnimator` bunlari okur.
        public const string PHiz = "hiz";
        public const string PTirmaniyor = "tirmaniyor";
        public const string PUcuyor = "ucuyor";
        public const string PPitch = "pitch";
        public const string PRoll = "roll";
        public const string TKusan = "kusan";
        public const string TAtla = "atla";
        public const string TIn = "in";
        public const string TCakil = "cakil";

        /// <summary>Mixamo'dan gelen kliplerin dosya öneki.</summary>
        private const string MixamoOnEki = "MX_Hezarfen@";

        /// <summary>Locomotion karışımının hız düğümleri (m/s).</summary>
        /// <remarks>
        /// Sayılar klip kataloğundan gelir, buradan değil — ama düğüm
        /// YERLERİ burada: duruş 0, yürüme 1,4, koşma 3,6. Bunlar
        /// <c>WalkController.walkSpeed</c> / <c>runSpeed</c> ile aynı
        /// olmak zorunda, yoksa oyuncu 1,4 m/s giderken karakter koşar.
        /// </remarks>
        private static readonly (string ad, float hiz)[] Locomotion =
        {
            ("Durus", 0f),
            // ELLE YAZILMIS SAYI YOK: esikler WalkController'in
            // sabitlerinden gelir. Once boyle degildi ve hiz
            // degisince esikler eski deger uzerinde kaldi.
            ("Yurume", Hezarfen.Player.WalkController.VarsayilanYurume),
            ("Kosma", Hezarfen.Player.WalkController.VarsayilanKosma),
        };

        [MenuItem("Hezarfen/Boru Hatti/Animator kontrolcusunu uret")]
        public static void Uret()
        {
            var klip = Klipler();
            if (klip.Count == 0)
            {
                Debug.LogError("[Hezarfen] Klip yok. Once: "
                    + "gen_animasyon.py -- --export, sonra "
                    + "Hezarfen/Boru Hatti/Karakteri yerlestir.");
                return;
            }

            string dizin = System.IO.Path.GetDirectoryName(Cikti);
            if (!AssetDatabase.IsValidFolder(dizin))
                AssetDatabase.CreateFolder(
                    System.IO.Path.GetDirectoryName(dizin),
                    System.IO.Path.GetFileName(dizin));

            // Var olan varliğin USTUNE yazilir: yolu koruyarak yeniden
            // uretmek GUID'i korur ve prefab/sahne referanslari kirilmaz.
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(Cikti);
            if (ac == null)
                ac = AnimatorController.CreateAnimatorControllerAtPath(Cikti);
            else
                Temizle(ac);

            ac.AddParameter(PHiz, AnimatorControllerParameterType.Float);
            ac.AddParameter(PTirmaniyor, AnimatorControllerParameterType.Bool);
            ac.AddParameter(PUcuyor, AnimatorControllerParameterType.Bool);
            ac.AddParameter(PPitch, AnimatorControllerParameterType.Float);
            ac.AddParameter(PRoll, AnimatorControllerParameterType.Float);
            foreach (string t in new[] { TKusan, TAtla, TIn, TCakil })
                ac.AddParameter(t, AnimatorControllerParameterType.Trigger);

            // IK PASS ACIK OLMALI.
            //
            // `AyakIK` ayaklari gercek zemine `OnAnimatorIK` icinde
            // oturtur ve Unity bu geri cagriyi YALNIZCA katmanin IK
            // pass'i acikken calistirir. Kapaliyken bilesen sessizce
            // hicbir sey yapmaz — hata yok, log yok, sadece ayaklar
            // yokusa gomulur. Bu, "olculmeyen yon olmayan yondur"
            // dersinin ayni sinifta tekrari olurdu.
            //
            // Katman dizisi DEGER kopyasi dondurur: alani degistirip
            // diziyi geri yazmak gerekiyor.
            var katmanlar = ac.layers;
            katmanlar[0].iKPass = true;
            ac.layers = katmanlar;

            var sm = ac.layers[0].stateMachine;
            var sb = new StringBuilder("ANIMATOR");

            // --- YER: locomotion karisimi ---------------------------------
            var locoTree = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = PHiz,
                useAutomaticThresholds = false,
            };
            AssetDatabase.AddObjectToAsset(locoTree, ac);
            var kayit = MixamoKaydi.Oku();
            foreach (var (ad, hiz) in Locomotion)
            {
                if (!klip.TryGetValue(ad, out var c)) continue;
                locoTree.AddChild(c, hiz);

                // OYNATMA CARPANI: esik / klibin OZ hizi.
                //
                // Elle yapilan klipler istenen hiz icin URETILIYORDU, o
                // yuzden carpan hep 1'di. Mixamo klibinin hizi sabittir:
                // yurume 1,786 m/s gelirken oyun 2,2 m/s yurutuyor.
                // Carpan olmadan ayak her adimda kayar.
                //
                // Klibi hizlandirmak adim SIKLIGINI artirir, adim BOYUNU
                // degil — yani kayma yapisal olarak sifirlanir. Sayi
                // art/mixamo/meta.json'dan okunur; buraya elle yazmak
                // ADR 0076'nin kok sebebini tekrar uretmek olurdu.
                // OZ HIZ UNITY'DE OLCULUR, Blender'daki kayittan degil.
                // Gerekce KlipYerHizi'nda: yeniden hedefleme adim boyunu
                // HEDEFIN bacak uzunluguyla olcekler, kaynagin degil.
                // meta.json'daki sayi yedek olarak kalir.
                // OZ HIZ: oyunda olculen, yeniden hedeflenmis yer hizi
                // (unity_hiz_ms). Blender'daki sayi yalnizca yedek —
                // gerekce KlipYerHiziOlcumu'nda: yeniden hedefleme adimi
                // HEDEFIN oranlariyla olcekler, kaynagin degil.
                float oz = MixamoKaydi.UnityHiz(ad);
                if (oz <= 0.01f) oz = MixamoKaydi.OzHiz(ad);
                var cocuklar = locoTree.children;
                int i = cocuklar.Length - 1;
                if (hiz > 0.01f && oz > 0.01f)
                {
                    cocuklar[i].timeScale = hiz / oz;
                    locoTree.children = cocuklar;
                    sb.AppendLine($"    {ad}: oz {oz:0.000} m/s -> "
                                  + $"esik {hiz:0.0} m/s, carpan "
                                  + $"{hiz / oz:0.000}");
                }
                else if (hiz > 0.01f && kayit != null)
                {
                    Debug.LogWarning($"[Hezarfen] {ad}: oz hiz kaydi yok "
                        + "(art/mixamo/meta.json). Oynatma carpani 1 "
                        + "birakildi — ayak kayabilir.");
                }
            }
            var loco = sm.AddState("Locomotion");
            loco.motion = locoTree;
            sm.defaultState = loco;
            sb.AppendLine($"  Locomotion: {locoTree.children.Length} dugum");

            var merdiven = Durum(sm, klip, "Merdiven");

            // --- HAVA: suzulus karisimi -----------------------------------
            // 2D Freeform Cartesian: pitch YATAY eksen degil, ikisi de
            // bagimsiz. Basit 1D olsaydi burun asagi ile sola yatis ayni
            // eksende yarisirdi ve ikisi ayni anda yapilamazdi — oysa
            // suzulusun tamami o ikisinin bilesimi.
            var glideTree = new BlendTree
            {
                name = "Suzulme",
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = PPitch,
                blendParameterY = PRoll,
            };
            AssetDatabase.AddObjectToAsset(glideTree, ac);
            foreach (var (ad, x, y) in new[]
            {
                ("Suzulme", 0f, 0f),
                ("Suzulme_Burun", -1f, 0f),
                ("Suzulme_Kuyruk", 1f, 0f),
                ("Suzulme_Sol", 0f, -1f),
                ("Suzulme_Sag", 0f, 1f),
            })
            {
                if (!klip.TryGetValue(ad, out var c)) continue;
                glideTree.AddChild(c, new Vector2(x, y));
            }
            var suzulme = sm.AddState("Suzulme");
            suzulme.motion = glideTree;
            sb.AppendLine($"  Suzulme: {glideTree.children.Length} dugum "
                          + "(2D pitch/roll)");

            var kusanma = Durum(sm, klip, "Kusanma");
            var kalkis = Durum(sm, klip, "Kalkis");
            var inis = Durum(sm, klip, "Inis");
            var cakilma = Durum(sm, klip, "Cakilma");

            // --- GECISLER ---------------------------------------------------
            // Merdiven: bool ile gider gelir (tirmanma bir DURUMDUR).
            Gecis(loco, merdiven, 0.15f, (PTirmaniyor, true));
            Gecis(merdiven, loco, 0.15f, (PTirmaniyor, false));

            // Kusanma → Kalkis → Suzulme: TEK YONLU zincir. Kusanma
            // bitmeden atlanamaz; kalkis bitmeden suzulunemez.
            Tetik(loco, kusanma, 0.10f, TKusan);
            Tetik(kusanma, kalkis, 0.12f, TAtla);
            Cikis(kalkis, suzulme, 0.20f);

            Tetik(suzulme, inis, 0.18f, TIn);
            Cikis(inis, loco, 0.20f);

            // Cakilma HER YERDEN erisilir: her yerden dusulebilir.
            foreach (var s in new[] { loco, merdiven, kusanma, kalkis,
                                      suzulme, inis })
                Tetik(s, cakilma, 0.06f, TCakil);
            Cikis(cakilma, loco, 0.35f);

            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            sb.AppendLine($"  {sm.states.Length} durum, "
                          + $"{ac.parameters.Length} parametre");
            sb.AppendLine($"  -> {Cikti}");
            sb.AppendLine(SakinlereBagla(ac));
            Debug.Log("[Hezarfen] " + sb);
        }

        /// <summary>
        /// Kontrolcüyü <b>sakin gövdelerine bağlar</b>.
        ///
        /// ## Ölçülen kusur
        ///
        /// <c>NPCYonetici</c> her karede <c>an.SetFloat("hiz", …)</c>
        /// çağırıyordu ve o satır yıllardır oradaydı. Ama sakin
        /// prefablarının Animator'ında <b>hiçbir kontrolcü yoktu</b>:
        /// FBX içe aktarımı Animator bileşenini kurar, kontrolcüyü
        /// kurmaz. Yani şehirdeki dokuz bin kişi bind pozunda, kollarını
        /// açmış, kayarak yürüyordu — ve hiçbir test kırmızı dönmüyordu,
        /// çünkü <c>SetFloat</c> kontrolcüsüz bir Animator'da sessizce
        /// hiçbir şey yapar.
        ///
        /// Oyuncunun gövdesi kontrolcüyü <c>HezarfenSpawner</c>'da
        /// alıyordu; sakinlerin böyle bir adımı hiç yoktu. Bu, bu
        /// depodaki en sık kusurun bir örneği daha: <b>yazıldı, diske
        /// geçti, hiç bağlanmadı.</b>
        ///
        /// Bağlama işi kontrolcüyü ÜRETEN adımın parçası: ayrı bir menü
        /// maddesi olsaydı atlanabilirdi, ve bu depoda atlanabilir bir
        /// adım bir kez atlandı (`KarakterYonu.Duzelt`).
        /// </summary>
        private static string SakinlereBagla(RuntimeAnimatorController ac)
        {
            var yollar = AssetDatabase
                .FindAssets("PF_ t:Prefab",
                            new[] { "Assets/_Project/Art/Prefabs" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(y =>
                {
                    string ad = System.IO.Path.GetFileName(y);
                    return ad.StartsWith("PF_Sakin_")
                           || ad.StartsWith("PF_Hezarfen_");
                })
                .OrderBy(y => y).ToList();

            int bagli = 0;
            foreach (string yol in yollar)
            {
                var kok = PrefabUtility.LoadPrefabContents(yol);
                try
                {
                    var an = kok.GetComponentInChildren<Animator>(true);
                    if (an == null) an = kok.AddComponent<Animator>();
                    if (an.runtimeAnimatorController == ac
                        && !an.applyRootMotion) continue;
                    an.runtimeAnimatorController = ac;
                    // Yer degistirmeyi kontrolcu degil bizim kod yazar
                    // (NPCAjan yolu yuruyor); kok hareketi acik kalirsa
                    // sakin klibin kendi yer degistirmesiyle CIFTE
                    // ilerler ve yoldan cikar.
                    an.applyRootMotion = false;
                    PrefabUtility.SaveAsPrefabAsset(kok, yol);
                    bagli++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(kok);
                }
            }
            AssetDatabase.SaveAssets();
            return $"  kontrolcu baglandi: {bagli}/{yollar.Count} prefab";
        }

        /// <summary>
        /// Klipleri model klasöründen adlarıyla toplar.
        ///
        /// ## Aynı adlı iki klip var — kim kazanır, yazılı olsun
        ///
        /// Yer hareketleri artık Mixamo'dan geliyor
        /// (<c>MX_Hezarfen@Durus</c>), uçuş klipleri elde yapılmaya devam
        /// ediyor (<c>SK_Hezarfen_Ucus@Suzulme</c>). Ama <c>Durus</c>,
        /// <c>Yurume</c>, <c>Kosma</c> ve <c>Merdiven</c> <b>ikisinde
        /// birden</b> var.
        ///
        /// Sözlüğe son yazan kazanırdı ve o sıra dosya sistemi tarama
        /// sırasıdır — yani hangi klibin oynadığı, klasörün alfabetik
        /// düzenine bağlı olurdu. Bu tam olarak ADR 0076'nın kök
        /// sebebidir: bir sayının (burada bir klibin) iki sahibi.
        ///
        /// Kural açık: <b>Mixamo kazanır</b>, ve kimin elendiği
        /// <b>söylenir</b>. Sessiz bir tercih, tercih değil kazadır.
        /// </summary>
        private static Dictionary<string, AnimationClip> Klipler()
        {
            var d = new Dictionary<string, AnimationClip>();
            var kaynak = new Dictionary<string, string>();
            var elenen = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets(
                         "t:Model", new[] { ModelDir }))
            {
                string yol = AssetDatabase.GUIDToAssetPath(guid);
                int at = yol.IndexOf('@');
                if (at < 0) continue;
                bool mixamo = System.IO.Path.GetFileName(yol)
                                     .StartsWith(MixamoOnEki);

                foreach (var c in AssetDatabase.LoadAllAssetsAtPath(yol)
                             .OfType<AnimationClip>())
                {
                    if (c.name.StartsWith("__preview__")) continue;
                    if (d.ContainsKey(c.name))
                    {
                        bool oncekiMixamo = kaynak[c.name]
                            .StartsWith(MixamoOnEki);
                        if (oncekiMixamo || !mixamo)
                        {
                            elenen.Add($"{c.name} <- "
                                       + System.IO.Path.GetFileName(yol));
                            continue;
                        }
                        elenen.Add($"{c.name} <- {kaynak[c.name]}");
                    }
                    d[c.name] = c;
                    kaynak[c.name] = System.IO.Path.GetFileName(yol);
                }
            }

            if (elenen.Count > 0)
            {
                Debug.Log("[Hezarfen] Ayni adli klipler — Mixamo tercih "
                          + "edildi, elenenler: " + string.Join(", ", elenen));
            }
            return d;
        }

        private static AnimatorState Durum(AnimatorStateMachine sm,
            Dictionary<string, AnimationClip> klip, string ad)
        {
            var s = sm.AddState(ad);
            if (klip.TryGetValue(ad, out var c)) s.motion = c;
            else Debug.LogWarning($"[Hezarfen] '{ad}' klibi yok — durum bos.");
            return s;
        }

        private static void Gecis(AnimatorState a, AnimatorState b,
                                  float sure, (string ad, bool deger) kosul)
        {
            var t = a.AddTransition(b);
            t.hasExitTime = false;
            t.duration = sure;
            t.AddCondition(kosul.deger ? AnimatorConditionMode.If
                                       : AnimatorConditionMode.IfNot,
                           0f, kosul.ad);
        }

        private static void Tetik(AnimatorState a, AnimatorState b,
                                  float sure, string tetik)
        {
            var t = a.AddTransition(b);
            t.hasExitTime = false;
            t.duration = sure;
            t.AddCondition(AnimatorConditionMode.If, 0f, tetik);
        }

        /// <summary>Klip bitince geçer — tek atımlık klipler için.</summary>
        private static void Cikis(AnimatorState a, AnimatorState b, float sure)
        {
            var t = a.AddTransition(b);
            t.hasExitTime = true;
            t.exitTime = 0.92f;
            t.duration = sure;
        }

        /// <summary>Yeniden üretimde eski grafiği siler; GUID kalır.</summary>
        private static void Temizle(AnimatorController ac)
        {
            var sm = ac.layers[0].stateMachine;
            foreach (var s in sm.states.ToArray())
                sm.RemoveState(s.state);
            foreach (var p in ac.parameters.ToArray())
                ac.RemoveParameter(p);
            // Blend tree'ler varliga gomulu alt nesnelerdir; durum
            // silinince sahipsiz kalirlar ve dosyayi sisirirler.
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(
                         AssetDatabase.GetAssetPath(ac)))
                if (o is BlendTree bt) Object.DestroyImmediate(bt, true);
        }
    }
}
