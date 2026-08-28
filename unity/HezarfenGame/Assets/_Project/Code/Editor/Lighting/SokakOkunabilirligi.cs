using UnityEditor;
using UnityEngine;
using Hezarfen.Editor.Diagnostics;

namespace Hezarfen.Editor.Lighting
{
    /// <summary>
    /// <b>Sokak okunabilir mi — ölçüm aracı.</b>
    ///
    /// Bu sınıf ışık <b>kurmaz</b>, ışığı <b>ölçer</b>. Ayrı durmasının
    /// sebebi tam olarak bu: ölçü, onu sağlayan uygulamadan bağımsız
    /// olmalı. Geçici aydınlatma takımı silindi (ADR 0072) ama gereklilik
    /// yerinde duruyor — *kim sağlarsa sağlasın, göz hizasından bakıldığında
    /// kare okunabilir olmalı*.
    ///
    /// Ölçüm daha önce geçici takımın içinde yaşıyordu ve takım silinince
    /// onunla birlikte gidecekti. Bir aletin ölçtüğü şeyle aynı dosyada
    /// oturması, aletin de geçici olduğunu söylerdi.
    ///
    /// <b>Ölçü parlaklık değil AYRINTI</b> (bkz. <c>LightingTests</c>):
    /// Balat'ın paleti bilerek koyudur ve parlaklık ölçüsü karanlık ışıkla
    /// karanlık malzemeyi ayırt edemiyordu. Sorulan soru şu: doku deseni
    /// görünüyor mu.
    /// </summary>
    public static class SokakOkunabilirligi
    {

        public static string Measure(out float darkFraction)
        {
            darkFraction = 1f;

            // ÖLÇÜLEN ŞEY: GÖLGEDEKİ BİR EV CEPHESİ.
            //
            // İki kez yanlış şeyi ölçtüm. Önce çekirdeğin 14 m önünü aldım ve
            // orası avlu duvarının dibine düştü — kare 2 m ötedeki bir duvarla
            // doluydu. Sonra sokak koridoruna baktım ve karenin yarısını
            // yamacın çıplak arazisi kapladı: sayı mimariyi değil araziyi
            // ölçüyordu ve arazinin karanlığı bu turun sorunu değil.
            //
            // Gereklilik neyse ölçü o olmalı: *gölgede kalan bir cephenin
            // dokusu okunuyor mu.* Kadraj o cepheyle dolar.
            var street = GameObject.Find("Sokak_Ana");
            if (street == null)
                foreach (var t in Object.FindObjectsByType<Transform>())
                    if (t.name == "Sokak_Ana") { street = t.gameObject; break; }
            if (street == null) return "Olcum YAPILAMADI: Sokak_Ana yok.";

            var sun = FindSun();
            if (sun == null) return "Olcum YAPILAMADI: gunes yok.";
            Vector3 sunDir = sun.transform.forward;     // isigin YOL ALDIGI yon

            // Kardes sirasi BELIRLEYICIDIR; ada gore siralama degildi. Evlerin
            // cogu ayni prefab adini tasiyor, `List.Sort` esitlikte kararsiz ve
            // ayni sahne iki kosumda iki farkli eve bakiyordu.
            Transform target = null;
            foreach (Transform t in street.transform)
            {
                if (t.GetComponent<LODGroup>() == null) continue;
                // Cephe gunesten YUZ CEVIRMIS olmali: isik yonuyle ayni yone
                // bakan yuzey golgede kalir.
                if (Vector3.Dot(t.forward, sunDir) > 0.25f) { target = t; break; }
            }
            if (target == null) return "Olcum YAPILAMADI: golgede cephe bulunamadi.";

            // Göz BASILAN YÜZEYİN üstünde, evin tabanının değil: ev taş bir
            // kaidenin üstünde durur ve bir kez ölçüm gözü yerden 3,03 m
            // yukarı çıkarmıştı — yaya seviyesi değil birinci kat hizası
            // ölçülüyordu.
            //
            // Arazi de doğru zemin DEĞİL: yaya kaldırıma basar ve kaldırım
            // yamaçta arazinin metrelerce üstündedir. Mahalle paketinde tam
            // bu yüzden kareler taşın ALTINDA çıktı; ölçü de aynı hatayı
            // taşıyordu, aynı aletle düzeltildi.
            Vector3 eye = FrameMetric.OnSurface(target.position + target.forward * 8.0f)
                        + Vector3.up * 1.70f;

            // Kareyi alan ve ölçen kod ORTAKTIR (FrameMetric): aynı ölçüyü
            // arazi örtüsü de kullanıyor. İkinci bir kopya, oradaki iki
            // tuzağın (Volume'ların kaydı, ısınma kareleri) da ikinci
            // kopyası olurdu.
            var st = FrameMetric.Capture(eye, target.position + Vector3.up * 3.0f,
                                         48f, "Captures/olcum_sokak.png");

            // Dışarıya AYRINTI ölçüsü döner; eşik de onun üstünde kurulur.
            // Parlaklık yüzdesi raporda kalır — bilgi olarak yararlı, ölçüt
            // olarak yanıltıcı.
            darkFraction = st.Detail;
            return $"Golgedeki cephe ({target.name}, 8 m, goz hizasi): {st}";
        }


        private static Light FindSun()
        {
            foreach (var l in Object.FindObjectsByType<Light>())
                if (l.type == LightType.Directional && l.shadows != LightShadows.None
                    && l.transform.parent == null)
                    return l;
            foreach (var l in Object.FindObjectsByType<Light>())
                if (l.type == LightType.Directional) return l;
            return null;
        }
    }
}
