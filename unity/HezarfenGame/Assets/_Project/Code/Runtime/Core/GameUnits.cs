using UnityEngine;

namespace Hezarfen.Core
{
    /// <summary>
    /// Projenin ölçek sözleşmesi: <b>1 Unity birimi = 1 metre</b>.
    /// Blender tarafı da Metric / Unit Scale 1.0 ile aynı sözleşmeye bağlıdır
    /// (bkz. docs/PLAN.md Bölüm 1 ve tools/blender/export_fbx.py).
    ///
    /// Bu sınıf, ölçekle ilgili sabitlerin kodun içine dağılmasını engeller.
    /// Bir sayının "metre mi, birim mi" olduğu asla tartışma konusu olmamalı.
    /// </summary>
    public static class GameUnits
    {
        /// <summary>Bir metrenin Unity birimi karşılığı. Sözleşme gereği 1.</summary>
        public const float MetersPerUnit = 1f;

        /// <summary>
        /// Efsanevi süzülüşün <b>tasarım hedefi</b> (metre) — uçuş ayarının referans noktası.
        ///
        /// <b>Bu kesin bir tarihsel değer DEĞİLDİR.</b> Evliya Çelebi metninde metrik
        /// mesafe vermez; 3358 / 3400 / 3558 m rakamları modern ikincil kaynakların
        /// yorumudur ve birbiriyle çelişir (docs/RESEARCH.md "Caveats"). Burada en çok
        /// anılan değer tasarım hedefi olarak seçilmiştir; oyuncuya <b>tek kesin sayı
        /// olarak sunulmamalıdır</b> — Kodeks <see cref="LegendaryDistanceMinMeters"/> ile
        /// <see cref="LegendaryDistanceMaxMeters"/> arasındaki aralığı göstermelidir.
        /// </summary>
        public const float LegendaryGlideDistanceMeters = 3358f;

        /// <summary>Çelişkili modern yorumların alt ucu (metre).</summary>
        public const float LegendaryDistanceMinMeters = 3358f;

        /// <summary>Çelişkili modern yorumların üst ucu (metre).</summary>
        public const float LegendaryDistanceMaxMeters = 3558f;

        /// <summary>
        /// Tasarım hedefine karşılık gelen kot farkı (metre). 3358 m / 62 m ~ 54:1
        /// süzülme oranı demektir; modern delta kanat ~15:1'dir. Oyun bu farkı fizik
        /// sabitleriyle değil rüzgâr sistemiyle kapatır (docs/PLAN.md Bölüm 2).
        /// Üst uç yorumda (3558 m) kot farkı ~86 m verilir — o da ~41:1 eder.
        /// </summary>
        public const float LegendaryGlideDropMeters = 62f;

        /// <summary>
        /// Galata Kulesi ile Doğancılar arasında <b>bizim</b> ölçtüğümüz yatay mesafe.
        ///
        /// Kaynak: Copernicus DEM üzerine oturtulmuş landmark kataloğu
        /// (`tools/gis/landmarks_build.py`). Doğancılar bir MEYDANDIR ve koordinatı
        /// ~100 m mertebesinde yaklaşıktır; yine de sonuç, çelişkili yorumların
        /// **üst ucunu** (3558 m) destekler, yaygın anılan 3358 m'yi değil.
        /// Faz 1 madde 3'ün georeferanslaması bu sayıyı sınayacak.
        /// </summary>
        public const float MeasuredGalataToDogancilarMeters = 3709f;

        /// <summary>Metreyi Unity birimine çevirir.</summary>
        public static float MetersToUnits(float meters) => meters / MetersPerUnit;

        /// <summary>Unity birimini metreye çevirir.</summary>
        public static float UnitsToMeters(float units) => units * MetersPerUnit;

        /// <summary>
        /// Bir mesh'in dünya uzayındaki en uzun kenarını metre cinsinden verir.
        /// İçe aktarılan varlıkların ölçek doğrulamasında kullanılır.
        /// </summary>
        public static float LongestEdgeMeters(Bounds worldBounds)
        {
            Vector3 size = worldBounds.size;
            return UnitsToMeters(Mathf.Max(size.x, Mathf.Max(size.y, size.z)));
        }
    }
}
