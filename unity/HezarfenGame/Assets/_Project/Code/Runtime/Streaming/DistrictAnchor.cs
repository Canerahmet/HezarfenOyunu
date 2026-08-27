using UnityEngine;

namespace Hezarfen.Streaming
{
    /// <summary>
    /// Bir semt sahnesinin kök işaretçisi.
    ///
    /// Sahne adı yerine bir bileşen kullanılıyor çünkü sahne dosyası taşınabilir ya
    /// da yeniden adlandırılabilir; kimlik varlığın kendisinde durmalıdır. Ayrıca
    /// yükleme sonrası "hangi semt geldi" sorusu, sahnenin kök nesnesine bakılarak
    /// yanıtlanabilir olur — Addressables tutamacını taşımaya gerek kalmaz.
    /// </summary>
    [AddComponentMenu("Hezarfen/District Anchor")]
    public class DistrictAnchor : MonoBehaviour
    {
        [Tooltip("DistrictDef.districtId ile aynı olmalı — ör. D_Galata")]
        public string districtId = "";
    }
}
