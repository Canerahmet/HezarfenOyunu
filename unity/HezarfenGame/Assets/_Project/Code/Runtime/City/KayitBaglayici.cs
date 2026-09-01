using Hezarfen.Zaman;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Kaydı sahneye bağlar:</b> neyin yazılacağı ve nereye
    /// döneceği tek yerde.
    ///
    /// Her sistem kendi kaydını yazsaydı, biri unutulduğunda oyun
    /// <b>sessizce eksik</b> yüklenirdi — oyuncu doğru yerde ama yanlış
    /// tarihte uyanırdı ve kimse bunu bir hata olarak görmezdi. Toplama
    /// ve dağıtma burada, yan yana duruyor ki biri eklenirken öteki
    /// atlanmasın.
    /// </summary>
    public class KayitBaglayici : MonoBehaviour
    {
        [Header("Bağlantılar (boşsa sahnede aranır)")]
        public ZamanSistemi zaman;
        public Transform oyuncu;
        public AranmaSistemi aranma;
        public NPCYonetici sehir;
        public Envanter envanter;
        public GorevYonetici gorev;
        public Player.Perde2Dilimi perde2;

        [Tooltip("Oyuncunun kesesi — akçe buradan okunur.")]
        public int akce = 0;

        /// <summary>Son yükleme başarılı mıydı — test ve HUD okur.</summary>
        public bool SonYuklemeBasarili { get; private set; }

        private void Awake() => Bul();

        private void Bul()
        {
            if (zaman == null) zaman = FindAnyObjectByType<ZamanSistemi>();
            if (aranma == null) aranma = FindAnyObjectByType<AranmaSistemi>();
            if (sehir == null) sehir = FindAnyObjectByType<NPCYonetici>();
            if (envanter == null) envanter = FindAnyObjectByType<Envanter>();
            if (gorev == null) gorev = FindAnyObjectByType<GorevYonetici>();
            if (perde2 == null) perde2 = FindAnyObjectByType<Player.Perde2Dilimi>();
            if (oyuncu == null)
            {
                var go = GameObject.Find("OYUNCU");
                if (go != null) oyuncu = go.transform;
            }
        }

        /// <summary>Şu anki durumu toplar.</summary>
        public KayitVerisi Topla()
        {
            Bul();
            var v = new KayitVerisi();

            if (zaman != null)
            {
                v.yil = zaman.yil;
                v.yilinGunu = zaman.yilinGunu;
                v.saat = zaman.saat;
            }
            if (oyuncu != null)
            {
                v.x = oyuncu.position.x;
                v.y = oyuncu.position.y;
                v.z = oyuncu.position.z;
                v.bakisYaw = oyuncu.rotation.eulerAngles.y;
            }
            if (aranma != null)
            {
                v.aranmaSeviyesi = aranma.Seviye;
                v.yasakMal = aranma.YasakMalTasiyor;
            }
            // KAYIT ALANLARININ YARISI HIC DOLDURULMUYORDU.
            //
            // `perde2Asama`, `talimSayisi`, `gorevArketip`, `gorevTohum`,
            // `gorevSiradaki` alanlari `KayitVerisi`de duruyordu ve ne
            // yaziliyor ne okunuyordu. Ustelik `Perde2Dilimi` ve
            // `AranmaSistemi` bunun icin birer geri-yukleme metodu
            // yazmisti — ve o metotlari da kimse cagirmiyordu. Yani
            // duzeltme yazilmis, baglanmamisti.
            if (perde2 != null)
            {
                v.perde2Asama = (int)perde2.Simdiki;
                v.talimSayisi = perde2.TalimSayisi;
            }
            if (gorev != null && gorev.Simdiki != null)
            {
                v.gorevArketip = (int)gorev.Simdiki.arketip;
                v.gorevTohum = gorev.tohum;
                v.gorevSiradaki = gorev.Simdiki.siradaki;
                akce = gorev.Kese.akce;
            }
            v.akce = akce;
            if (envanter != null) v.envanter = envanter.Serilestir();
            return v;
        }

        /// <summary>Kaydı uygular. Şehir tarihten ve tohumdan yeniden doğar.</summary>
        public void Uygula(KayitVerisi v)
        {
            SonYuklemeBasarili = false;
            if (v == null) return;
            Bul();

            if (zaman != null)
            {
                zaman.yil = v.yil;
                zaman.yilinGunu = v.yilinGunu;
                zaman.saat = v.saat;
                zaman.Yenile();
            }
            if (oyuncu != null)
            {
                // KARAKTER DENETLEYICISI KAPATILMADAN tasinmaz: acikken
                // konum atamasi bir sonraki karede geri alinir ve oyuncu
                // kaydettigi yerde DEGIL, eski yerinde uyanir.
                var cc = oyuncu.GetComponent<CharacterController>();
                bool acikti = cc != null && cc.enabled;
                if (acikti) cc.enabled = false;

                oyuncu.position = new Vector3(v.x, v.y, v.z);
                oyuncu.rotation = Quaternion.Euler(0f, v.bakisYaw, 0f);

                if (acikti) cc.enabled = true;
            }
            if (aranma != null)
            {
                aranma.YasakMalTasiyor = v.yasakMal;
                aranma.SeviyeyiGeriYukle(v.aranmaSeviyesi);
            }
            if (perde2 != null)
                perde2.DurumuGeriYukle(v.perde2Asama, v.talimSayisi);
            akce = v.akce;
            if (gorev != null) gorev.Kese.akce = v.akce;
            if (envanter != null) envanter.Yukle(v.envanter);

            // SEHIR YENIDEN KURULUR: sakinler tarihten ve tohumdan doğar,
            // kayıt dosyasından değil (ADR 0070).
            if (sehir != null) sehir.Kur();

            SonYuklemeBasarili = true;
        }

        /// <summary>Kaydet.</summary>
        public bool Kaydet() => Kayit.Yaz(Topla());

        /// <summary>
        /// <b>Kendiliğinden kaydeder.</b>
        ///
        /// ## Neden gerekti
        ///
        /// Bir oyuncu kırk sekiz dakika oynadı ve F5'e basmadığı için
        /// hepsini kaybetti. Aynı oyuncu ana menüdeki yeni "Devam et"
        /// düğmesini de hiç görmedi — düğme kayıt varsa görünüyor,
        /// kayıt ise yalnız F5 ile oluşuyordu. Yani düğme, ona en çok
        /// ihtiyacı olan kişiye görünmez kalıyordu: *"kapıyı takıp
        /// menteşeyi unutmak."*
        ///
        /// ## Neden bu iki an
        ///
        /// Kaydetme anı keyfi olmamalı. Bir işin bitmesi ve perdenin
        /// ilerlemesi, oyuncunun *"bir şey başardım"* dediği iki andır;
        /// oyunun onu koruduğu an da orası olmalı. Zamanlayıcıyla
        /// kaydetmek daha sık yazar ama hiçbir şey ifade etmez ve
        /// oyuncuyu kötü bir anda dondurabilir.
        /// </summary>
        private void OnEnable()
        {
            if (gorev == null) gorev = FindAnyObjectByType<GorevYonetici>();
            if (gorev != null) gorev.GorevBitti += IsBitince;

            _perde = FindAnyObjectByType<Hezarfen.Player.Perde2Dilimi>();
            if (_perde != null) _perde.AsamaDegisti += AsamaDegisince;
        }

        private void OnDisable()
        {
            if (gorev != null) gorev.GorevBitti -= IsBitince;
            if (_perde != null) _perde.AsamaDegisti -= AsamaDegisince;
        }

        private Hezarfen.Player.Perde2Dilimi _perde;

        private void IsBitince(Gorev g) => KaydetGuvenliyse();

        private void AsamaDegisince(Hezarfen.Player.Perde2Dilimi.Asama a)
            => KaydetGuvenliyse();

        /// <summary>
        /// Yalnızca oyuncunun <b>kurtulabileceği</b> bir andaysa kaydeder.
        ///
        /// ## Neden bir koşul gerekti
        ///
        /// Otomatik kayıt bir oyuncunun kırk sekiz dakikasını
        /// kurtarmak için eklendi ve bir sonraki turda onun oyununu
        /// kurtarılamaz hâle getirdi: kulenin tepesinde kapalı bir
        /// yerde Space'e bastığında perde Kule→Uçuş→İniş→Kule diye
        /// art arda değişiyor, her değişimde kaydediliyor ve tek slot
        /// olduğu için geri dönüş kalmıyordu. *"Geçen turdaki 48
        /// dakikamı kaybettim diye eklediğiniz özellik, bu turda
        /// oyunumu kurtarılamaz hâle getirdi."*
        ///
        /// İki kural: iki kayıt arasında en az beş saniye olmalı
        /// (art arda üç yazmayı keser), ve oyuncu <b>uçuyorsa</b>
        /// kaydedilmez — havadaki bir konum, yüklendiğinde düşmenin
        /// ortasıdır.
        /// </summary>
        private void KaydetGuvenliyse()
        {
            if (Time.unscaledTime - _sonKayit < 5f) return;

            var dizi = FindAnyObjectByType<Hezarfen.Player.UcusDizisi>();
            if (dizi != null
                && dizi.Simdiki != Hezarfen.Player.UcusDizisi.Durum.Yerde)
                return;

            _sonKayit = Time.unscaledTime;
            Kaydet();
        }

        private float _sonKayit = -999f;

        /// <summary>Yükle. Kayıt yoksa ya da bozuksa <c>false</c>.</summary>
        public bool Yukle()
        {
            var v = Kayit.Oku();
            if (v == null) return false;
            Uygula(v);
            return SonYuklemeBasarili;
        }
    }
}
