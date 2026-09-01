using UnityEngine;

namespace Hezarfen.Player
{
    /// <summary>
    /// <b>Ayak sesi.</b>
    ///
    /// ## Neden bu bileşen var
    ///
    /// Bir oyuncunun raporundan: *"Ayak sesim yok — koşuyorum, taş
    /// kaldırımda hiçbir şey duyulmuyor."* Şehirde 36.302 yapı,
    /// 40.000 sakin, ezanî saat ve narh defteri var; oyuncunun kendi
    /// adımı yok.
    ///
    /// Ayak sesi bir süs değil: yürüdüğünü, ne kadar hızlı
    /// yürüdüğünü ve neyin üstünde yürüdüğünü söyleyen tek kanal.
    /// Sessiz bir karakter, kontrol ettiğin bir şey gibi değil,
    /// izlediğin bir şey gibi hissettirir.
    ///
    /// ## Adım hızı hesaplanmaz, hızdan türer
    ///
    /// Bir insanın adım boyu yaklaşık <b>0,75 m</b> yürürken,
    /// koşarken uzar. Sesi zamanlayıcıya değil <b>kat edilen yola</b>
    /// bağlamak, hızlanınca temponun kendiliğinden artmasını sağlar ve
    /// yavaşlarken sesin kaymamasını. Bu, animasyonla senkron tutmanın
    /// da en ucuz yolu.
    /// </summary>
    [AddComponentMenu("Hezarfen/Adim Sesi")]
    [RequireComponent(typeof(CharacterController))]
    public class AdimSesi : MonoBehaviour
    {
        [Tooltip("Adım örnekleri — dördü de aynı sesin varyantı.")]
        public AudioClip[] ornekler;

        [Tooltip("Yürürken bir adımda kat edilen yol (m).")]
        public float adimBoyu = 0.75f;

        [Range(0f, 1f)] public float ses = 0.45f;

        private CharacterController _cc;
        private AudioSource _kaynak;
        private float _yol;
        private int _sonSecim = -1;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            var go = new GameObject("AdimKaynagi");
            go.transform.SetParent(transform, false);
            _kaynak = go.AddComponent<AudioSource>();
            _kaynak.playOnAwake = false;
            _kaynak.spatialBlend = 0f;   // kendi ayagin: kulagında
        }

        private void Update()
        {
            if (_cc == null || ornekler == null || ornekler.Length == 0) return;

            // HAVADAKI AYAK SES CIKARMAZ.
            if (!_cc.isGrounded) { _yol = 0f; return; }

            var v = _cc.velocity;
            float hiz = new Vector2(v.x, v.z).magnitude;
            if (hiz < 0.3f) { _yol = 0f; return; }

            _yol += hiz * Time.deltaTime;

            // Kosarken adim boyu uzar: 0,75 -> ~1,05 m.
            float boy = adimBoyu * Mathf.Lerp(1f, 1.4f,
                                              Mathf.InverseLerp(2f, 6f, hiz));
            if (_yol < boy) return;
            _yol = 0f;

            // AYNI ORNEK ART ARDA CALINMAZ.
            //
            // Dort varyantin varlik sebebi tekrari duyulmaz kilmak;
            // rastgele secim ayni ornegi ust uste secerse o sebep
            // kaybolur.
            int i = Random.Range(0, ornekler.Length);
            if (ornekler.Length > 1 && i == _sonSecim)
                i = (i + 1) % ornekler.Length;
            _sonSecim = i;

            // Perde ve siddet hafifce oynatilir: iki adim hic ayni
            // degildir ve kulak bunu tekrar sanmaz.
            _kaynak.pitch = Random.Range(0.92f, 1.08f);
            _kaynak.PlayOneShot(ornekler[i],
                                ses * Random.Range(0.85f, 1.0f));
        }
    }
}
