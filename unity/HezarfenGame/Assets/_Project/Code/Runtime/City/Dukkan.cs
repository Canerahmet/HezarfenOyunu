using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Akçenin harcandığı yer.</b>
    ///
    /// ## Neden bu bileşen var
    ///
    /// Ekonominin bir kaynağı vardı (görev ödülü) ve neredeyse hiç
    /// gideri yoktu: yalnız kayık bileti (1–3 akçe) ve ceza. Kese
    /// şişiyor ve hiçbir yere gitmiyordu. <see cref="Ekonomi"/>'nin
    /// narh defterinden türetilmiş bütün özeni — sipahi yevmiyesi 40,
    /// ırgat 10, günlük ekmek 1 akçe — oyuncuya hiç görünmüyordu.
    ///
    /// Bir ekonomi tek yönlü çalışmaz. Kazanılan şeyin harcanacak bir
    /// yeri yoksa kazanmak da bir şey ifade etmez.
    ///
    /// ## Fiyatlar uydurulmuyor
    ///
    /// Günlük ekmek 1 akçe (`Ekonomi.GunlukEkmek`, narh defterinden).
    /// Ötekiler ona göre: su bedava değil ama ucuz — sakalar akçenin
    /// bölünmüş hâlini alırdı, oyunda en küçük birim 1 akçe olduğu
    /// için su da 1. Odun bir günlük yakacak, ekmekle aynı sırada.
    /// Sebze mevsimlik ve ucuz.
    ///
    /// Dükkân <b>satın alır da</b>: oyuncunun topladığı şeyi geri
    /// alır, biraz ucuza. Böylece şehirdeki 15.815 toplanabilir eşya
    /// ilk kez bir işe yarar — su küpünden aldığın su bir yere gider.
    /// Alış-satış farkı esnafın kârıdır ve bir mekanik değil, ticaretin
    /// kendisi.
    /// </summary>
    [AddComponentMenu("Hezarfen/Dukkan")]
    public class Dukkan : MonoBehaviour, IEtkilesim
    {
        [Header("Bağlantılar (boşsa sahnede aranır)")]
        public GorevYonetici gorev;
        public Envanter envanter;
        public Hezarfen.Zaman.ZamanSistemi zaman;

        [Tooltip("Kepenk bu saatler arasında açık.")]
        public float acilis = 6.5f;
        public float kapanis = 19.5f;

        /// <summary>Dükkânın sattığı şey.</summary>
        public EsyaTuru satilan = EsyaTuru.Ekmek;

        private int _kip;   // 0 = al, 1 = sat

        private void Awake() => Kur();

        /// <summary>
        /// Bağlantıları bulur. Ayrı metot, çünkü sahne kurulumu
        /// (Editor kipi) <c>Awake</c> çağırmıyor — bu depoda üç kez
        /// bedeli ödenmiş bir ders.
        /// </summary>
        public void Kur()
        {
            if (gorev == null) gorev = FindAnyObjectByType<GorevYonetici>();
            if (envanter == null) envanter = FindAnyObjectByType<Envanter>();
            if (zaman == null)
                zaman = FindAnyObjectByType<Hezarfen.Zaman.ZamanSistemi>();
        }

        /// <summary>Kepenk açık mı.</summary>
        public bool Acik
        {
            get
            {
                if (zaman == null) return true;
                float s = zaman.saat;
                return s >= acilis && s <= kapanis;
            }
        }

        /// <summary>Bu malın alış fiyatı (akçe).</summary>
        public static int Fiyat(EsyaTuru t) => t switch
        {
            // Narh defterinden: gunluk ekmek 1 akce.
            EsyaTuru.Ekmek => Ekonomi.GunlukEkmek,
            // Saka suyu ucuz ama bedava degil; en kucuk birim 1 akce.
            EsyaTuru.Su => 1,
            EsyaTuru.Odun => 2,
            EsyaTuru.Sebze => 1,
            // Kanat parcasi satilik degil — Hezarfen'in kendi isi.
            _ => 0,
        };

        /// <summary>
        /// Esnafın verdiği fiyat — aldığından ucuz.
        ///
        /// Fark esnafın kârı. Alış ve satış fiyatını eşitlemek,
        /// oyuncuya sonsuz akçe basan bir döngü verirdi: al, sat, al.
        /// </summary>
        public static int SatisFiyati(EsyaTuru t) =>
            Mathf.Max(0, Fiyat(t) - 1);

        public string Ipucu
        {
            get
            {
                if (!Acik) return "Kepenk kapalı";
                if (_kip == 0)
                    return $"{Ad(satilan)} al · {Fiyat(satilan)} akçe";

                var elde = SatilabilirOlan();
                return elde == null
                    ? "Satacak bir şeyin yok"
                    : $"{Ad(elde.Value)} sat · {SatisFiyati(elde.Value)} akçe";
            }
        }

        public bool Hazir => Acik;

        public bool Etkiles(GameObject aktor)
        {
            if (!Acik) return false;
            Kur();
            var env = aktor.GetComponentInParent<Envanter>() ?? envanter;
            var kese = gorev != null ? gorev.Kese : null;
            if (env == null || kese == null) return false;

            bool oldu = _kip == 0 ? Al(env, kese) : Sat(env, kese);

            // Basarili ya da degil, kip degisir: oyuncu ikinci basista
            // otekini gorur. Menusuz bir dukkan icin en sade yol bu.
            _kip = 1 - _kip;
            return oldu;
        }

        private bool Al(Envanter env, Kese kese)
        {
            int f = Fiyat(satilan);
            if (f <= 0 || !kese.Yeter(f)) return false;
            if (!env.Ekle(satilan)) return false;   // kese dolu
            kese.Ode(f);
            return true;
        }

        private bool Sat(Envanter env, Kese kese)
        {
            var t = SatilabilirOlan();
            if (t == null) return false;
            if (!env.Cikar(t.Value)) return false;
            kese.Kazan(SatisFiyati(t.Value));
            return true;
        }

        /// <summary>Envanterde satılabilecek ilk kalem.</summary>
        private EsyaTuru? SatilabilirOlan()
        {
            if (envanter == null) return null;
            foreach (EsyaTuru t in System.Enum.GetValues(typeof(EsyaTuru)))
                if (SatisFiyati(t) > 0 && envanter.Adet(t) > 0) return t;
            return null;
        }

        /// <summary>Malın oyuncuya görünen adı.</summary>
        public static string Ad(EsyaTuru t) => t switch
        {
            EsyaTuru.Su => "su",
            EsyaTuru.Odun => "odun",
            EsyaTuru.Sebze => "sebze",
            EsyaTuru.Ekmek => "ekmek",
            EsyaTuru.KanatParcasi => "kanat parçası",
            _ => t.ToString(),
        };
    }
}
