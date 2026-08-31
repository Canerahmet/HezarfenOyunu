using System.Collections.Generic;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Replikleri görünür kılar.</b>
    ///
    /// Beş bin replik üretmek, şehri konuşturmaz. *Üretilen ama görünmeyen
    /// bir öğe, olmayan bir öğedir* — korpus bir dosyada dururken oyuncu
    /// için hiç yoktur. Bu bileşen onu duyulur hâle getiriyor: yakındaki
    /// sakinlerin başının üstünde, kısa süre duran bir yazı.
    ///
    /// ## Neden yalnız YAKINDAKİLER
    ///
    /// Otuz kişilik bir meydanda otuz balon aynı anda açılırsa okunacak
    /// hiçbir şey kalmaz; gürültü olur. Duyma mesafesi gerçek bir insan
    /// sesinin mesafesi kadar (<see cref="duyulmaMesafesi"/>) ve aynı anda
    /// en çok <see cref="ayniAndaEnCok"/> tanesi görünür — en yakınları.
    ///
    /// ## Havuz
    ///
    /// Etiketler <see cref="NPCYonetici"/>'nin gövdeleri gibi havuzlanır:
    /// her karede yazı nesnesi yaratmak, konuşan kalabalığı çöp toplayıcı
    /// duraksamalarına çevirirdi.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class BarkGosterici : MonoBehaviour
    {
        public NPCYonetici yonetici;
        public Transform oyuncu;

        [Tooltip("Bir repliğin duyulduğu mesafe (m).")]
        public float duyulmaMesafesi = 12f;

        [Tooltip("Aynı anda en çok kaç replik görünsün.")]
        //: DORT COKTU. IKI.
        //
        // Karelerde ayni anda ALTI replik sayildi (dordu sinir olsa da:
        // ayrisma suzgeci elenenleri listeden cikariyor ama liste zaten
        // dolmus oluyordu). Sonuc `isik_gunbatimi.png`'de goruldu —
        // ekranin yarisi dev punto dunya yazisi, ust uste binmis,
        // duvarin icinden gecmis, hicbiri okunmuyor.
        //
        // Bir kalabaligin canli duyulmasi icin herkesin ayni anda
        // konusmasi gerekmiyor; tersine, herkes konusursa kimse
        // duyulmaz. Iki replik bir sokak sesidir, alti bir gurultudur.
        [Range(1, 12)] public int ayniAndaEnCok = 2;

        [Tooltip("Yazının başın üstündeki yüksekliği (m).")]
        public float yukseklik = 1.95f;

        public int GorunurReplik { get; private set; }

        private readonly List<TextMesh> _havuz = new();
        /// <summary>
        /// İki replik ekranda bu kadar yakınsa biri gizlenir (piksel).
        ///
        /// Y daha dar, X daha geniş: yazı yatay uzanır, yani yan yana
        /// iki replik birbirine değmeden durabilir ama üst üste gelen
        /// iki satır kesin okunmaz olur. 1080p'ye göre; ekran
        /// yüksekliğiyle ölçeklenmesi HUD'un ölçek işiyle birlikte
        /// gelecek.
        /// </summary>
        private const float EkranAyrikY = 46f;
        private const float EkranAyrikX = 300f;

        private readonly List<Vector3> _ekran = new List<Vector3>();

        /// <summary>
        /// Repliğin ekrandaki kabaca genişliği (piksel).
        ///
        /// Gerçek ölçü <c>Renderer.bounds</c>'un ekrana izdüşümü olurdu
        /// ama etiket bu karede henüz yerleştirilmedi; metinden tahmin
        /// etmek, sabit bir sayı kullanmaktan ölçülebilir biçimde iyi.
        /// </summary>
        private float MetinEni(NPCAjan a, Vector3 ekranNoktasi)
        {
            int harf = a.replik != null && a.replik.metin != null
                ? a.replik.metin.Length : 0;
            // Etiket dunya uzayinda `uzak/12` olcekli ve ekrandaki boyu
            // bu yuzden mesafeden bagimsiz; 1080p'de harf basina ~11 px
            // olcuduk.
            return Mathf.Clamp(harf * 11f, 40f, Screen.width * 0.8f);
        }

        private readonly List<(NPCAjan ajan, float d2)> _adaylar = new();
        private Camera _kamera;

        private void LateUpdate()
        {
            if (yonetici == null || oyuncu == null) return;
            if (_kamera == null) _kamera = Camera.main;

            float menzil2 = duyulmaMesafesi * duyulmaMesafesi;
            _adaylar.Clear();

            foreach (var a in yonetici.Sakinler)
            {
                // Govdesi olmayan konusmaz: gorunmeyen bir agizdan cikan
                // yazi havada asili kalirdi.
                if (a.govde == null || a.replik == null) continue;
                float d2 = (a.konum - oyuncu.position).sqrMagnitude;
                if (d2 > menzil2) continue;

                // GORUS HATTI SART.
                //
                // Yazi dunyada duruyor ama duvarin ARKASINDAN da
                // okunuyordu: cami avlusunda cekilen karede dort replik
                // ust uste binmis, direklerin ve evlerin onunde asili
                // duruyordu. Duyulmasi degil GORULMESI sorun — konusan
                // kisi gorunmuyorsa sozu de gorunmemeli.
                if (_kamera != null)
                {
                    var agiz = a.konum + Vector3.up * yukseklik;
                    var goz = _kamera.transform.position;
                    var fark = agiz - goz;
                    if (Physics.Raycast(goz, fark.normalized,
                                        fark.magnitude - 0.4f, ~0,
                                        QueryTriggerInteraction.Ignore))
                        continue;
                }

                _adaylar.Add((a, d2));
            }

            // En yakinlar konusur — kalabalikta duyulan da odur.
            _adaylar.Sort((x, y) => x.d2.CompareTo(y.d2));

            // EKRANDA AYRI DURSUNLAR — VE OLCU EKRANDA ALINIR.
            //
            // Once kural dunya uzayindaydi: "birbirine 3 m'den yakin
            // konusanlardan yalniz biri konusur". Yanlis cetveldi ve
            // uc ayri karede ayni kusuru birakti — bakis ekseni
            // boyunca dizilmis iki konusmaci dunyada 20 m ayriktir ama
            // EKRANDA ust uste biner. Yazi dunyada degil ekranda
            // okunuyor; ayrik olmasi gereken yer de orasi.
            //
            // Elenen aday susmaz, yalnizca YAZISI gorunmez: konusma
            // sesi ve rutini yerinde kalir.
            if (_kamera != null)
            {
                _ekran.Clear();
                for (int i = _adaylar.Count - 1; i >= 0; i--)
                {
                    var d = _kamera.WorldToScreenPoint(
                        _adaylar[i].ajan.konum + Vector3.up * yukseklik);
                    if (d.z <= 0f) { _adaylar.RemoveAt(i); continue; }

                    // AYRISMA YAZININ GENISLIGINE BAKAR, CAPAYA DEGIL.
                    //
                    // Sabit 300 px kullaniliyordu ve replikler farkli
                    // uzunlukta: 800 px genisligindeki bir yazi,
                    // 301 px otedeki komsusunu gecirip ustune biniyordu.
                    // Capalar ayrik, yazilar ic ice.
                    //
                    // Genislik metinden turetiliyor: karakter basina
                    // kabaca 0,55 punto ve etiket ortalanmis, yani
                    // yarisi her yana tasar. Kaba ama olcunun kendisi
                    // metne bagli ve bu, sabit bir sayidan iyi.
                    float benimEn = MetinEni(_adaylar[i].ajan, d);
                    bool cakisti = false;
                    foreach (var e in _ekran)
                    {
                        float pay = (benimEn + e.z) * 0.5f + 16f;
                        if (Mathf.Abs(e.y - d.y) < EkranAyrikY
                            && Mathf.Abs(e.x - d.x) < pay)
                        { cakisti = true; break; }
                    }
                    d.z = benimEn;   // z artik derinlik degil GENISLIK

                    // EKRAN KENARINDAN TASAN YAZI DA ELENIR.
                    //
                    // Karelerde iki replik sol kenardan kesikti: capa
                    // ekranin icindeydi ama yazi disari tasiyordu.
                    // Yarisi gorunmeyen bir replik, gorunmeyen bir
                    // repliktir.
                    float yari = benimEn * 0.5f;
                    bool tasti = d.x - yari < 8f
                                 || d.x + yari > Screen.width - 8f;

                    if (cakisti || tasti) _adaylar.RemoveAt(i);
                    else _ekran.Add(d);
                }
            }

            int n = Mathf.Min(_adaylar.Count, ayniAndaEnCok);
            GorunurReplik = n;

            for (int i = 0; i < n; i++)
            {
                var t = Etiket(i);
                var a = _adaylar[i].ajan;
                t.text = a.replik.metin;
                // Golge yaziyi izler: metin degisince ikisi birlikte
                // degismeli, yoksa kontur bir onceki repligi gosterir.
                var g = t.transform.childCount > 0
                    ? t.transform.GetChild(0).GetComponent<TextMesh>() : null;
                if (g != null) g.text = t.text;
                t.transform.position = a.konum + Vector3.up * yukseklik;
                if (_kamera != null)
                {
                    var bakis = t.transform.position - _kamera.transform.position;
                    t.transform.rotation = Quaternion.LookRotation(bakis);

                    // EKRANDAKI BOYU SABIT TUT.
                    //
                    // TextMesh dunya uzayindadir: 3 m'deki bir replik
                    // ekranin ucte birini kapliyordu ("Selamunaleykum"
                    // karenin yarisi kadardi). Olcek mesafeyle buyur,
                    // boylece yazi uzakta okunur, yakinda ekrani yemez.
                    // KELEPCE YORUMUN TAM TERSINI YAPIYORDU.
                    //
                    // "Alt sinir 6 m: daha yakinda buyumesin" yaziyordu
                    // ve `Mathf.Max(6f, uzak)` tam olarak buyumesine
                    // sebep oluyordu: 6 m'nin altinda olcek 0,5'te
                    // sabitleniyor ama mesafe kuculmeye devam ediyor,
                    // yani ekrandaki boy BUYUYOR. Karelerde bir replik
                    // karenin yarisini kapliyordu.
                    //
                    // Olcek mesafeyle DOGRU ORANTILI oldugunda ekrandaki
                    // boy her mesafede sabit kalir — istenen buydu ve
                    // kelepce onu bozan seydi. Kaldirmak yeterli.
                    float uzak = Mathf.Max(0.5f, bakis.magnitude);
                    t.transform.localScale = Vector3.one * (uzak / 12f);
                }
                t.gameObject.SetActive(true);
            }
            for (int i = n; i < _havuz.Count; i++)
                _havuz[i].gameObject.SetActive(false);
        }

        private TextMesh Etiket(int i)
        {
            while (_havuz.Count <= i)
            {
                var go = new GameObject($"BARK_{_havuz.Count}");
                go.transform.SetParent(transform, false);
                var tm = go.AddComponent<TextMesh>();
                tm.characterSize = 0.055f;
                tm.fontSize = 64;
                tm.anchor = TextAnchor.LowerCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = new Color(1f, 0.96f, 0.86f, 1f);

                // KONTUR: BEYAZ YAZI BEYAZ SIVANIN USTUNDE OKUNMUYOR.
                //
                // Tek renk bir yazi, arka plani ne olursa olsun ayni
                // parlaklikta ciziliyordu. `isik_ogle.png`'de "Bre
                // ignelikci geldi!" acik siva uzerinde eriyip gidiyor,
                // `isik_gunbatimi.png`'de turuncu gokyuzunde kayboluyor.
                //
                // Cozum ayri bir sader: aynı yazidan koyu bir kopya,
                // birkaç santim geride. Kamera yaziya bakiyor, yani
                // "geride" kameradan uzakta demek — koyu kopya her
                // acidan yazinin arkasinda kalir ve bir kontur gibi
                // okunur. TextMesh'in kendi kontur destegi yok ve
                // TMP'ye gecmek ayri bir is (HUD'un tamami IMGUI).
                var golgeGo = new GameObject("golge");
                golgeGo.transform.SetParent(go.transform, false);
                golgeGo.transform.localPosition = new Vector3(0f, 0f, 0.03f);
                var golge = golgeGo.AddComponent<TextMesh>();
                golge.characterSize = tm.characterSize;
                golge.fontSize = tm.fontSize;
                golge.anchor = tm.anchor;
                golge.alignment = tm.alignment;
                golge.color = new Color(0f, 0f, 0f, 0.85f);

                _havuz.Add(tm);
            }
            return _havuz[i];
        }
    }
}
