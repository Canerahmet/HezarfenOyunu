using System.Collections.Generic;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Oyuncunun taşıdıkları.</b>
    ///
    /// Küçük ve sayıya dayalı: dönem oyuncusu sırt çantası taşımaz,
    /// kuşağında ve heybesinde birkaç şey taşır. Kalem sayısını
    /// sınırlamak bir oyun kısıtı değil, o kuşağın gerçeği.
    ///
    /// Akçe burada DEĞİL: para <see cref="Ekonomi"/>'de yaşıyor ve
    /// oraya ikinci bir sahip eklemek, bu projede tekrar tekrar
    /// ölçülen kusurun ta kendisi olurdu.
    /// </summary>
    [AddComponentMenu("Hezarfen/Envanter")]
    public class Envanter : MonoBehaviour
    {
        /// <summary>Bir türden taşınabilecek en çok adet.</summary>
        public const int TurBasinaEnCok = 9;

        private readonly Dictionary<EsyaTuru, int> _kese = new();

        /// <summary>Bu türden kaç tane var.</summary>
        public int Adet(EsyaTuru t) => _kese.TryGetValue(t, out int n) ? n : 0;

        /// <summary>Taşınan tür sayısı.</summary>
        public int TurSayisi => _kese.Count;

        /// <summary>Ekler. Dönüş: gerçekten eklendi mi (dolu değilse).</summary>
        public bool Ekle(EsyaTuru t, int adet = 1)
        {
            int simdi = Adet(t);
            if (simdi >= TurBasinaEnCok) return false;
            _kese[t] = Mathf.Min(TurBasinaEnCok, simdi + adet);
            Degisti?.Invoke();
            return true;
        }

        /// <summary>Çıkarır. Dönüş: yetti mi.</summary>
        public bool Cikar(EsyaTuru t, int adet = 1)
        {
            int simdi = Adet(t);
            if (simdi < adet) return false;
            if (simdi == adet) _kese.Remove(t);
            else _kese[t] = simdi - adet;
            Degisti?.Invoke();
            return true;
        }

        /// <summary>Kayıt için düz liste: `tur:adet` çiftleri.</summary>
        public List<int> Serilestir()
        {
            var l = new List<int>(_kese.Count * 2);
            foreach (var kv in _kese) { l.Add((int)kv.Key); l.Add(kv.Value); }
            return l;
        }

        /// <summary>Kayıttan yükler. Bilinmeyen tür sessizce atlanır.</summary>
        public void Yukle(IReadOnlyList<int> duz)
        {
            _kese.Clear();
            if (duz == null) return;
            for (int i = 0; i + 1 < duz.Count; i += 2)
            {
                if (!System.Enum.IsDefined(typeof(EsyaTuru), duz[i])) continue;
                _kese[(EsyaTuru)duz[i]] = Mathf.Clamp(duz[i + 1], 0, TurBasinaEnCok);
            }
            Degisti?.Invoke();
        }

        public event System.Action Degisti;
    }
}
