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
            v.akce = akce;
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
            if (aranma != null) aranma.YasakMalTasiyor = v.yasakMal;
            akce = v.akce;

            // SEHIR YENIDEN KURULUR: sakinler tarihten ve tohumdan doğar,
            // kayıt dosyasından değil (ADR 0070).
            if (sehir != null) sehir.Kur();

            SonYuklemeBasarili = true;
        }

        /// <summary>Kaydet.</summary>
        public bool Kaydet() => Kayit.Yaz(Topla());

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
