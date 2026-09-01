using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Bir sakin gövdesinin kimliği</b> — cinsiyet, yaş bandı, çıplak boy.
    ///
    /// ## Neden bir bileşen, neden bir tablo değil
    ///
    /// Şehirde yedi arketip var (yetişkin erkek, genç, yaşlı, kadın, yaşlı
    /// kadın, oğlan, kız) ve <see cref="NPCYonetici"/> her sakine bunlardan
    /// birini vermek zorunda. En kolay yol Unity tarafında "hangi prefab
    /// hangi yaş" diye bir tablo yazmaktı. Bu depoda o kararın bedeli üç
    /// kez ölçüldü: <b>bir sayının iki sahibi olduğunda</b> biri değişir,
    /// öteki eskir ve yalanı ancak oyunda görünür.
    ///
    /// Sayının tek sahibi üreteçtir (`tools/blender/lib/sakin_kit.py`).
    /// Oradan `art/blend/karakter/catalog.json`'a, oradan da yerleştirme
    /// adımında (`KarakterLanding`) bu bileşene yazılır. Unity hiçbir
    /// yerde "kadın 1,58 m'dir" demez; <b>okur</b>.
    ///
    /// ## Çıplak boy neden gerekli
    ///
    /// <see cref="InsanDNA"/> her sakine bir hedef boy veriyor (dağılımdan).
    /// Ölçek çarpanı <c>hedefBoy / tabanBoy</c>'dur ve <c>tabanBoy</c>
    /// arketipten arketipe değişir — 1,24 m'lik oğlanı 1,70 m'lik adamın
    /// tabanıyla ölçeklemek çocuğu cüceye çevirirdi.
    /// </summary>
    [DisallowMultipleComponent]
    public class SakinGovde : MonoBehaviour
    {
        [Tooltip("Giysi tipi: erkek / genc / yasli / kadin / cocuk / kiz")]
        public string tip = "erkek";

        [Tooltip("Cinsiyet: erkek | kadin")]
        public string cinsiyet = "erkek";

        [Tooltip("Yas bandi: cocuk | genc | yetiskin | yasli")]
        public string yasBandi = "yetiskin";

        [Tooltip("Ciplak govdenin boyu (m) — olcek bunun uzerine kurulur.")]
        public float tabanBoy = 1.70f;

        /// <summary>
        /// Hangi havuzdan geldiği — çalışma zamanı, kaydedilmez.
        /// <see cref="NPCYonetici"/> gövdeyi doğru havuza geri koyabilsin
        /// diye: tek havuza dönen gövde bir sonraki sahibine yanlış
        /// arketip olarak giderdi.
        /// </summary>
        [System.NonSerialized] public int havuzDizini = -1;

        /// <summary>Kadın gövdesi mi.</summary>
        public bool Kadin => cinsiyet == "kadin";

        /// <summary>
        /// Yaş bandının sayısal karşılığı: 0 çocuk … 3 yaşlı.
        /// <see cref="InsanDNA"/>'nın 0-1 yaşıyla karşılaştırılabilsin diye.
        /// </summary>
        public int BandDizini
        {
            get
            {
                switch (yasBandi)
                {
                    case "cocuk": return 0;
                    case "genc": return 1;
                    case "yasli": return 3;
                    default: return 2;
                }
            }
        }
    }
}
