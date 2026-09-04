using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hezarfen.Editor.Pipeline
{
    /// <summary>
    /// <c>_Project/Art/Textures/</c> altındaki dokuların import ayarlarını sabitler.
    ///
    /// Neden script: PBR hattındaki hataların çoğu <b>sessizdir</b>. Bir maske
    /// haritası yanlışlıkla sRGB olarak işaretlenirse doku yüklenmiş görünür,
    /// hiçbir uyarı çıkmaz, ama pürüzlülük ve AO yanlış eğride okunur — yüzeyler
    /// "biraz plastik" olur ve sebebi kolay kolay bulunmaz. Inspector'dan
    /// düzeltmek de kalıcı değildir; ayar dosyada yaşamalı.
    ///
    /// Sözleşme, dosya adının son ekidir (<c>build_unity_maps.py</c> üretir):
    ///   <c>_BC</c>   taban renk — <b>sRGB</b>
    ///   <c>_MASK</c> HDRP maskesi (R metalik, G AO, B detay, A parlaklık) — <b>ham veri</b>
    ///   <c>_N</c>    normal — <b>NormalMap</b> tipi
    /// </summary>
    public class TextureImportPolicy : AssetPostprocessor
    {
        private const string Governed = "assets/_project/art/textures/";

        /// <summary>
        /// Politika değişince yeniden import tetikler. Artırılmazsa değişiklik
        /// diskteki eski ayarlarla yaşamaya devam eder.
        /// </summary>
        public override uint GetVersion() => 2;   // aniso 4 -> 8

        public static bool IsGoverned(string assetPath) =>
            assetPath.Replace('\\', '/').ToLowerInvariant().StartsWith(Governed);

        private void OnPreprocessTexture()
        {
            if (!IsGoverned(assetPath)) return;

            var t = (TextureImporter)assetImporter;
            string name = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();

            if (name.EndsWith("_n"))
            {
                t.textureType = TextureImporterType.NormalMap;
                t.sRGBTexture = false;
                // Poly Haven'dan `nor_gl` indiriliyor: Unity'nin bekledigi yon (Y+).
                // DirectX (Y-) alinsaydi butun girintiler cikinti olurdu.
                t.textureCompression = TextureImporterCompression.CompressedHQ;
            }
            else if (name.EndsWith("_mask"))
            {
                t.textureType = TextureImporterType.Default;
                t.sRGBTexture = false;                    // VERI, renk degil
                t.alphaSource = TextureImporterAlphaSource.FromInput;
                // Alfa parlaklik tasiyor, seffaflik degil. 'true' birakmak
                // Unity'nin alfayi onceden carpmasina yol acardi.
                t.alphaIsTransparency = false;
                t.textureCompression = TextureImporterCompression.CompressedHQ;
            }
            else if (name.EndsWith("_bc"))
            {
                t.textureType = TextureImporterType.Default;
                t.sRGBTexture = true;
                t.alphaSource = TextureImporterAlphaSource.None;
                t.textureCompression = TextureImporterCompression.Compressed;
            }
            else
            {
                return;                                    // sozlesme disi dosya
            }

            // Dunya olcekli UV 0-1 araligini ASAR (u = mesafe / doku_boyu).
            // Clamp olsaydi duvarin ilk 2 metresinden sonrasi tek renge yayilirdi.
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            // SIG ACILI YUZEY: 4 yetmiyor.
            //
            // Iki gerekce, biri kesin biri hipotez — ayrildi ki
            // karistirilmasin.
            //
            // KESIN OLAN: arazi hatti (`TerrainCoverBuilder`) dokularini
            // zaten 8 ile aliyor. Ayni projede ayni ozelligin iki degeri
            // olmasi, sebebi ne olursa olsun, yanlis.
            //
            // HIPOTEZ: Galata karesinde uzaktaki catilar kirmizi benek
            // yigini olarak cikiyor. Iki aciklama olculup elendi —
            // tarama degil (`tarama_gurultusu.py`, satranc ilintisi
            // 0,0001), LOD'lar arasi UV kaymasi da degil
            // (`uv_yogunlugu.py`, oran 1,00). Sig aci kaldi: 80 derece
            // gelme acisinda dokunun en-boy orani 1/cos(80) ~ 5,8'dir ve
            // aniso 4 bunu ortmez. 8, ~83 dereceye kadar orter.
            //
            // Hipotez KARE ILE dogrulanmadan bu satir bir cozum diye
            // yazilmadi: 4'ten 8'e cikis tutarlilik icin dogru, benegi
            // kapatip kapatmadigi ayri bir olcum.
            t.anisoLevel = 8;
            t.mipmapEnabled = true;
            t.streamingMipmaps = true;
            t.maxTextureSize = 2048;
        }
    }
}
