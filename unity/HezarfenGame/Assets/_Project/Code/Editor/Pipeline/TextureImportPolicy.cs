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
        public override uint GetVersion() => 1;

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
            t.anisoLevel = 4;                              // sig acili duvar/cati icin
            t.mipmapEnabled = true;
            t.streamingMipmaps = true;
            t.maxTextureSize = 2048;
        }
    }
}
