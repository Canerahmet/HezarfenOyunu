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
                if (d2 <= menzil2) _adaylar.Add((a, d2));
            }

            // En yakinlar konusur — kalabalikta duyulan da odur.
            _adaylar.Sort((x, y) => x.d2.CompareTo(y.d2));
            int n = Mathf.Min(_adaylar.Count, ayniAndaEnCok);
            GorunurReplik = n;

            for (int i = 0; i < n; i++)
            {
                var t = Etiket(i);
                var a = _adaylar[i].ajan;
                t.text = a.replik.metin;
                t.transform.position = a.konum + Vector3.up * yukseklik;
                if (_kamera != null)
                    t.transform.rotation = Quaternion.LookRotation(
                        t.transform.position - _kamera.transform.position);
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
