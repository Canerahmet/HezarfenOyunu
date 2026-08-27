using UnityEngine;

namespace Hezarfen.Core
{
    /// <summary>
    /// Bir sahne öğesinin tarihsel dayanağı (docs/PLAN.md Bölüm 2).
    /// Oyunun kimliği bu ayrımın dürüstlüğünde: neyi bildiğimizi, neyi kurduğumuzu
    /// ve neyin efsane olduğunu oyuncudan saklamıyoruz.
    /// </summary>
    public enum HistoricalTier
    {
        /// <summary>
        /// Prototip yer tutucusu — hiçbir tarihsel iddia taşımaz.
        /// Plandaki T1/T2/T3 şemasında yoktur; graybox öğeleri etiketsiz kalmasın diye var.
        /// **Faz 1 çıkışında sahnede Graybox kalmamalıdır** (GrayboxOnlyInPrototype testi bunu korur).
        /// </summary>
        Graybox = 0,

        /// <summary>T1 — Belgeli. Konum ve biçim kaynaklarla destekli. <see cref="sourceNote"/> ZORUNLU.</summary>
        Documented = 1,

        /// <summary>T2 — Makul rekonstrüksiyon. Ev-ev kaydı yok; kurallarla üretildi (konut dokusu, sokak örüntüsü).</summary>
        Reconstruction = 2,

        /// <summary>T3 — Efsane. Tek kaynaklı (Evliya Çelebi): uçuşun kendisi, Lagari'nin roketi.</summary>
        Legend = 3
    }

    /// <summary>
    /// Her sahne öğesine takılır (CLAUDE.md kuralı). Kodeks UI'ı bu veriyi oyuncuya gösterir.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Hezarfen/Historical Tag")]
    public class HistoricalTag : MonoBehaviour
    {
        [Tooltip("Tarihsel dayanak seviyesi. Graybox yalnızca prototip içindir.")]
        public HistoricalTier tier = HistoricalTier.Graybox;

        [TextArea(2, 5)]
        [Tooltip("T1 (Documented) için ZORUNLU: docs/RESEARCH.md'den kaynak satırı. " +
                 "T3 (Legend) için tek-kaynak uyarısı yazılır.")]
        public string sourceNote = "";

        /// <summary>Bu etiket, dolu bir kaynak notu gerektiriyor mu?</summary>
        public bool RequiresSource => tier == HistoricalTier.Documented;

        /// <summary>Etiket kendi kuralına uyuyor mu? Editor testi bunu kullanır.</summary>
        public bool IsValid => !RequiresSource || !string.IsNullOrWhiteSpace(sourceNote);
    }
}
