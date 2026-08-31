using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// Alınabilir bir şey: su küpü, odunluk, sebze tahtası.
    ///
    /// Nesne <b>tükenir</b> ve bir süre sonra dolar. Sonsuz su küpü,
    /// dünyayı bir düğme tarlasına çevirir; boşalan küp ise oyuncuya
    /// başka bir avluya gitmesi için sebep verir.
    /// </summary>
    [AddComponentMenu("Hezarfen/Toplanabilir Esya")]
    public class ToplanabilirEsya : MonoBehaviour, IEtkilesim
    {
        public EsyaTuru tur = EsyaTuru.Su;

        [Tooltip("Kaç kez alınabilir; sonra dolmayı bekler.")]
        public int stok = 2;

        [Tooltip("Boşaldıktan sonra dolma süresi (oyun saati).")]
        public float dolmaSaati = 6f;

        // -1 = "henuz dokunulmadi", yani stok kadar dolu.
        //
        // Bu once `OnEnable` icinde tohumlaniyordu ve test onu
        // reddetti: Unity, `ExecuteAlways` tasimayan bir bilesenin
        // `OnEnable`ini Editor kipinde CAGIRMAZ, dolayisiyla kup
        // oynatmadan once bos gorunuyordu. Kusur testin degil
        // tasarimindi — bir sayinin degeri, o sayiyi kimin ne zaman
        // uyandirdigina bagli olmamali. Artik deger okurken turetiliyor
        // ve yasam dongusune hic ihtiyaci yok.
        private int _kalan = -1;
        private float _bosaldi = -999f;

        /// <summary>Kalan alim hakki.</summary>
        public int Kalan => _kalan < 0 ? Mathf.Max(0, stok) : _kalan;

        public string Ipucu => tur switch
        {
            EsyaTuru.Su => "Su al",
            EsyaTuru.Odun => "Odun al",
            EsyaTuru.Sebze => "Sebze topla",
            EsyaTuru.Ekmek => "Ekmek al",
            _ => "Al",
        };

        public bool Hazir => Kalan > 0;

        public bool Etkiles(GameObject aktor)
        {
            if (!Hazir) return false;
            var env = aktor.GetComponentInParent<Envanter>();
            if (env == null || !env.Ekle(tur)) return false;
            _kalan = Kalan - 1;
            if (_kalan == 0) _bosaldi = Time.time;
            return true;
        }

        private void Update()
        {
            if (Kalan > 0 || _bosaldi < 0f) return;
            // Oyun saati gerçek saatten hızlı akar; burada gerçek
            // saniyeyle yaklaşılıyor çünkü zaman sistemi bu nesneye
            // bağlı değil ve ikinci bir zaman sahibi yaratmak
            // istemiyoruz. 6 oyun saati ≈ 90 gerçek saniye.
            if (Time.time - _bosaldi > dolmaSaati * 15f)
            {
                _kalan = stok;
                _bosaldi = -999f;
            }
        }
    }
}
