using UnityEngine;

namespace Hezarfen.City
{
    /// <summary>
    /// <b>Her eve kendi tonunu verir — mesh çoğaltmadan.</b>
    ///
    /// Varyant sayısı 26'dan 201'e çıktı ve Galata'da varyant başına
    /// tekrar 418'den 13'e indi. Ama 13 hâlâ 13: aynı sokakta iki ev
    /// aynı kalıptan olabilir, yalnız yan yana değil. Gözün onları
    /// birbirinden ayırdığı ilk şey biçim değil <b>renk</b>tir.
    ///
    /// Yeni mesh üretmek pahalı ve gereksiz: 10.900 eve tekil ağ vermek
    /// bellek ve yükleme demek. Bir <see cref="MaterialPropertyBlock"/>
    /// ise hiçbir şey çoğaltmaz — GPU örnekleme sürerken yalnız sabit
    /// tampon değişir.
    ///
    /// ## Ton KONUMDAN türer, kur'adan değil
    ///
    /// Aynı ev, sahne iki kez yüklendiğinde aynı renkte olmalı. Bu
    /// projenin her yerindeki determinizm kuralı burada da geçerli:
    /// tohum, evin dünya konumudur. Kayıt tutmaya, sahneye sayı
    /// yazmaya gerek yok.
    ///
    /// ## Sınırlar dar, ve bilerek
    ///
    /// Ton oynaması bir <b>palet değişimi değil, yıpranma</b>dır:
    /// aynı kireç badanadan iki ev, biri güneşte solmuş öteki gölgede
    /// kalmış. Geniş bir aralık mahalleyi karnavala çevirirdi —
    /// RESEARCH §4.1 dönemin renk dünyasını dar tutuyor ve gayrimüslim
    /// mahallede daha da koyu.
    /// </summary>
    [DisallowMultipleComponent]
    public class EvTonu : MonoBehaviour
    {
        /// <summary>Parlaklık oynaması (çarpan olarak ±).</summary>
        [Range(0f, 0.30f)] public float parlaklik = 0.10f;

        /// <summary>Doygunluk oynaması (çarpan olarak ±).</summary>
        [Range(0f, 0.40f)] public float doygunluk = 0.14f;

        /// <summary>Renk tonu oynaması (derece ±).</summary>
        [Range(0f, 12f)] public float tonDerece = 3.5f;

        private static readonly int IdRenk = Shader.PropertyToID("_BaseColor");

        private void Awake() => Uygula();

        /// <summary>
        /// Tonu uygular. Editörden de çağrılabilir; oyunda
        /// <see cref="Awake"/> bir kez çalıştırır.
        /// </summary>
        public void Uygula()
        {
            var p = transform.position;
            // Tohum: konumun santimetre çözünürlüğünde karması. Aynı
            // yerdeki ev her açılışta aynı rengi alır.
            unchecked
            {
                int tohum = Mathf.RoundToInt(p.x * 100f) * 73856093
                          ^ Mathf.RoundToInt(p.z * 100f) * 19349663;
                var rng = new System.Random(tohum);

                float dv = ((float)rng.NextDouble() * 2f - 1f) * parlaklik;
                float ds = ((float)rng.NextDouble() * 2f - 1f) * doygunluk;
                float dh = ((float)rng.NextDouble() * 2f - 1f)
                           * (tonDerece / 360f);

                var blok = new MaterialPropertyBlock();
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    var mat = r.sharedMaterial;
                    if (mat == null || !mat.HasProperty(IdRenk)) continue;

                    r.GetPropertyBlock(blok);
                    Color taban = mat.GetColor(IdRenk);
                    Color.RGBToHSV(taban, out float h, out float s2, out float v);
                    h = Mathf.Repeat(h + dh, 1f);
                    s2 = Mathf.Clamp01(s2 * (1f + ds));
                    v = Mathf.Clamp01(v * (1f + dv));
                    var yeni = Color.HSVToRGB(h, s2, v);
                    yeni.a = taban.a;
                    blok.SetColor(IdRenk, yeni);
                    r.SetPropertyBlock(blok);
                }
            }
        }
    }
}
