using UnityEngine;
// TEK DOSYA = TEK MonoBehaviour.
//
// Bu uc tur once tek dosyadaydi ve Unity sessizce reddetti: bir
// MonoBehaviour'un sinif adi dosya adiyla ayni degilse Editor onun
// MonoScript'ini bulamaz, bilesen sahneye yazilmaz. Iki gecis
// "15.815 esya isaretlendi" diye rapor verdi ve sahne dosyalarina
// SIFIR bilesen dustu. Basarili gorunen bir olcumun yalan
// soylemesinin bedeli budur; bolme bu yuzden zorunlu, uslup degil.


namespace Hezarfen.Sehir
{
    /// <summary>
    /// Oyuncunun bir şeyle <b>bir şey yapabilmesi</b>.
    ///
    /// Şehir bugüne kadar bakılan bir şeydi: 19.992 avlu eşyası, 10.900
    /// ev, 373 kayık — hiçbirine dokunulamıyordu. Bir dünyanın canlı
    /// hissettirmesi, içindeki nesnelerin **cevap vermesine** bağlı;
    /// su küpünden su içilebiliyorsa küp bir dekor olmaktan çıkar.
    /// </summary>
    public interface IEtkilesim
    {
        /// <summary>Ekranda görünecek kısa ipucu ("Su iç").</summary>
        string Ipucu { get; }

        /// <summary>Şu an etkileşilebilir mi (boşalmış küp değil).</summary>
        bool Hazir { get; }

        /// <summary>Etkileşimi uygular. Dönüş: bir şey oldu mu.</summary>
        bool Etkiles(GameObject aktor);
    }

    /// <summary>Dönem envanterinde bir kalem.</summary>
    public enum EsyaTuru
    {
        /// <summary>Su — susuzluk yok ama içmek bir eylemdir.</summary>
        Su = 0,
        /// <summary>Odun — ocak ve mangal için.</summary>
        Odun = 1,
        /// <summary>Sebze — bahçeden.</summary>
        Sebze = 2,
        /// <summary>Ekmek.</summary>
        Ekmek = 3,
        /// <summary>Kanat parçası — Hezarfen'in işi.</summary>
        KanatParcasi = 4,
    }

}
