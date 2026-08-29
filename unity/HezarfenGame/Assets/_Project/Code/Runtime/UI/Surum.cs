using UnityEngine;

namespace Hezarfen.Arayuz
{
    /// <summary>
    /// <b>Hangi build oynanıyor — ekranda yazar.</b>
    ///
    /// Bu sınıf bir tartışmayı bitirmek için var. Caner "aynı sorunlar devam
    /// ediyor" dedi; sahneyi ölçtüm ve düzeltmelerin <b>hepsi yerindeydi</b>
    /// (TAA açık, kamera kipi kurulu, hız 2,2). İki ihtimal kalıyordu ve
    /// ikisini ayırt etmenin yolu yoktu: ya düzeltme işe yaramadı, ya da
    /// oynanan build eski.
    ///
    /// Bir tur boyunca hangi ihtimalin doğru olduğunu bilememek, ölçüm
    /// yapmadan tartışmak demek. Damga bunu kesiyor: menüde build zamanı ve
    /// commit özeti yazar, ekran görüntüsü bile yeter.
    ///
    /// Değer <c>Resources/surum.txt</c>'ten okunur ve o dosyayı
    /// <c>BuildPipelineEntry</c> her build'den önce yazar — yani elle
    /// güncellenmesi gereken bir sayı değil.
    /// </summary>
    public static class Surum
    {
        private const string Kaynak = "surum";

        private static string _deger;

        /// <summary>Build damgası; yoksa "editor".</summary>
        public static string Damga
        {
            get
            {
                if (_deger != null) return _deger;
                var ta = Resources.Load<TextAsset>(Kaynak);
                _deger = ta != null && !string.IsNullOrWhiteSpace(ta.text)
                    ? ta.text.Trim()
                    : "editor (damga yok)";
                return _deger;
            }
        }

        /// <summary>Test için sıfırlar.</summary>
        public static void Unut() => _deger = null;
    }
}
