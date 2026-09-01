using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// Blender'dan gelen her FBX'in import ayarlarını tek yerden sabitler.
    ///
    /// Neden zorunlu: ihraç tarafını `export_fbx.py` ile kilitlemek yarım çözümdür.
    /// Unity tarafında ölçek çarpanı ya da normal/teğet ayarı elle değiştirilirse
    /// aynı hata öbür uçtan geri gelir. Boru hattının iki ucu da scriptte yaşamalı;
    /// Inspector'dan yapılan değişiklik sessizce kaybolan bir değişikliktir.
    ///
    /// Kapsam: yalnızca <c>Assets/_Import/</c> (iniş alanı) ve
    /// <c>Assets/_Project/Art/Models/</c> altı. Paket içi modellere dokunulmaz.
    /// </summary>
    public class ModelImportPolicy : AssetPostprocessor
    {
        private const string ImportLanding = "assets/_import/";
        private const string ArtModels = "assets/_project/art/models/";

        /// <summary>
        /// Politika değişince Unity'nin varlıkları yeniden import etmesini sağlar.
        /// Bu sayının artırılmadığı bir politika değişikliği, diskteki eski
        /// ayarlarla çalışmaya devam eder — sessiz ve bulunması zor bir tuzak.
        /// </summary>
        public override uint GetVersion() => 6;

        public static bool IsGoverned(string assetPath)
        {
            string p = assetPath.Replace('\\', '/').ToLowerInvariant();
            return p.StartsWith(ImportLanding) || p.StartsWith(ArtModels);
        }

        // KLIP KOKUNU AYAGA TASIMAK DENENDI VE GERI ALINDI.
        //
        // `AyakIKTests` "karakter zeminden +0,426 m uzakta basliyor"
        // diyor ve haklı: `MX_Hezarfen@Durus` klibi kökü kalçadan
        // ölçüyor (`keepOriginalPositionY: 1`). Doğru görünen düzeltme
        // `heightFromFeet` idi — Unity'nin "Based Upon: Feet" ayarı.
        //
        // ÖLÇÜM AKSİNİ SÖYLEDİ: 0,426 hiç değişmedi **ve** yürüyen
        // kliplerin yer hızı ölçümü bozuldu (PlayMode 47 → 43). Yani
        // kökün dikey çapası bu kusurun kaynağı değil; kaynağı başka
        // bir yerde ve henüz bilmiyorum.
        //
        // Bir düzeltmenin doğru GÖRÜNMESİ, doğru olduğunun kanıtı
        // değil. Geri alındı ve kusur açık kaydedildi.

        private void OnPreprocessModel()
        {
            if (!IsGoverned(assetPath)) return;

            var importer = (ModelImporter)assetImporter;

            // --- Olcek: sozlesmenin Unity tarafi (1 birim = 1 metre) ---
            // useFileScale, FBX'in kendi birim bilgisini (cm) kullanir; export
            // tarafinda apply_unit_scale=True oldugu icin sonuc birebir metredir.
            importer.globalScale = 1f;
            importer.useFileScale = true;

            // --- Geometri ---
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;              // bellek; gerekirse varlik bazinda acilir
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.weldVertices = true;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.preserveHierarchy = false;
            importer.sortHierarchyByName = false;     // FBX sirasi korunur (LOD sirasi)

            // --- Normal / teget ---
            // Normalleri Blender'dan aliyoruz (mesh_smooth_type=FACE ile yazildi).
            // Tegetler MikkTSpace ile Unity'de uretilir: graybox mesh'lerinde UV
            // yok, dolayisiyla FBX'te teget de yok; "Import" secilirse Unity uyari
            // basip bos teget uretir.
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;

            // --- Malzeme ---
            // ImportViaMaterialDescription, etkin render hattina (HDRP) uygun
            // shader secer. ImportStandard, HDRP'de macenta malzeme uretir.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

            // Gomulu kalir; gercek baglama OnAssignMaterialModel'de yapilir.
            //
            // `MaterialLocation.External` Unity 6'da KALDIRILDI (obsolete uyarisi
            // verir ve calismaz). Desteklenen yol, her FBX malzemesi icin
            // OnAssignMaterialModel kancasindan proje varligini dondurmektir;
            // Unity bunu kalici bir yeniden esleme olarak kaydeder.
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;

            // --- Collider ---
            // UCX_ mesh'lerini kendimiz bagliyoruz; otomatik collider siluetten
            // genis cikip carpismalari adaletsiz yapar.
            importer.addCollider = false;

            // --- Animasyon ---
            //
            // Statik varliklar animasyon tasimaz ve tasimamalidir: her
            // graybox kutusuna bos bir klip iliskirmek hem ice aktarmayi
            // yavaslatir hem de Animator penceresini coplestirir.
            //
            // ANCAK bu kural bir zamanlar "SK_ ile baslamayan her sey
            // statiktir" diye yazilmisti ve o gun dogruydu — yorumun
            // kendisi soyluyor: "su an boru hattinda yoklar".
            //
            // Bugun yoklar degil. Mixamo klipleri `MX_Hezarfen@Yurume`
            // adiyla geliyor ve bu kural onlarin animasyonunu SESSIZCE
            // siliyordu. Bulunmasi pahaliya mal oldu: dosya bicimini,
            // Unity'nin FBX okuyucusunu ve eksik deriyi sucladim; ucu de
            // masumdu. Kusuru ortaya cikaran sey KONTROL oldu — calistigi
            // bilinen bir klibi ayni yoldan gecirince o da sifir klip
            // verdi. Bir olcum, kendi dogrulugunu kanitlayamiyorsa once
            // olcumu sinamak gerekir.
            //
            // Kural artik dosyanin NE OLDUGUNU soruyor:
            //   SK_  — iskeletli karakter varligi
            //   MX_  — Mixamo'dan gelen klip
            //   @    — Unity'nin "model@klip" animasyon dosyasi kurali
            string dosyaAdi = System.IO.Path.GetFileName(assetPath);
            bool animasyonTasir =
                dosyaAdi.StartsWith("SK_") ||
                dosyaAdi.StartsWith("MX_") ||
                dosyaAdi.Contains("@");
            if (!animasyonTasir)
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
            }
        }

        /// <summary>
        /// FBX'teki her malzeme için, aynı adı taşıyan **proje malzemesini** bağlar.
        ///
        /// Neden gerekli: FBX'ten gömülü üretilen malzeme yalnızca taban rengi
        /// taşır — maske haritası, normal, parlaklık yoktur. Elle düzeltilse bile
        /// bir sonraki Blender koşusunda silinir. Bizim malzemelerimizi
        /// <see cref="OttomanMaterialBuilder"/> doku bildiriminden üretir ve
        /// Blender'daki adlar (M_Timber_AsiRed gibi) Unity varlık adlarıyla
        /// birebir aynıdır.
        ///
        /// Arama <b>tek klasörle</b> sınırlı: projenin her yerinde ada göre
        /// aramak, bir gün adaş bir malzemeyi sessizce bağlardı.
        ///
        /// Karşılığı yoksa <c>null</c> döner ve Unity varsayılanı gömer — ilk
        /// koşuşta (malzemeler henüz üretilmemişken) olan budur; malzemeler
        /// üretilince yeniden import bağlar.
        /// </summary>
        public Material OnAssignMaterialModel(Material material, Renderer renderer)
        {
            if (!IsGoverned(assetPath) || material == null) return null;
            string path = $"{OttomanMaterialBuilder.MaterialDir}/{material.name}.mat";
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!IsGoverned(assetPath)) return;

            // UCX_ carpisma mesh'leri gorunur olmamali. Unreal'den gelen bu
            // adlandirma sozlesmesini (CLAUDE.md) Unity bilmez; renderer'i biz kapatiriz.
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.gameObject.name.StartsWith("UCX")) continue;   // UCX_ ve UCXB_
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }
        }
    }
}
