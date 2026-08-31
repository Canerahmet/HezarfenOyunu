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
        [Range(1, 12)] public int ayniAndaEnCok = 4;

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

                    bool cakisti = false;
                    foreach (var e in _ekran)
                        if (Mathf.Abs(e.y - d.y) < EkranAyrikY
                            && Mathf.Abs(e.x - d.x) < EkranAyrikX)
                        { cakisti = true; break; }

                    if (cakisti) _adaylar.RemoveAt(i);
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
                    // Alt sinir 6 m: daha yakinda buyumesin.
                    float uzak = Mathf.Max(6f, bakis.magnitude);
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
                _havuz.Add(tm);
            }
            return _havuz[i];
        }
    }
}
