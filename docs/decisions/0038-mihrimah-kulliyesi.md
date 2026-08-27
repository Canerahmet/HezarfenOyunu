# ADR 0038 — Mihrimah Külliyesi: medrese, sıbyan mektebi ve caminin 164 m'lik hatası

- **Tarih**: 2026-08-25
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/mihrimah_kulliyesi.md`)
- **Bağlam**: Faz 3. ADR 0036'da cami üretilmişti; külliyenin geri kalanı.

## Karar 1 — Yalnız iki yapı üretildi, ve bu bir sınır

Külliyenin 1632'de ayakta olan dört yapısından **ikisi** yerleştirildi:

| yapı | 1632 | bugün | yerleştirildi mi |
|---|---|---|---|
| medrese | ayakta | ayakta (tıp merkezi) | **evet** |
| sıbyan mektebi | ayakta | ayakta (çocuk kütüphanesi) | **evet** |
| imaret-tabhâne | ayakta | 1722'de yandı | **hayır** |
| kervansaray (Kurşunlu Han) | ayakta | 1920'lerde çöktü | **hayır** |

Son ikisinin **yeri bilinmiyor**: TDV imaretin yerinin belirsiz olduğunu
söyler (1936'da yol genişletmesi kalıntıyı da yok etti), han ise tamamen
kaldırılmıştır. Yeri bilinmeyen bir yapıyı koymak koordinat uydurmaktır.
Eksiklik bir unutma değil, kanıt sınırı — ve öyle kaydedildi.

## Karar 2 — Medrese **sayıya** göre boyutlandırıldı

Elimdeki tek sayısal belge hücre sayısı: **on altı** ("kubbeli bir dershane
ve on altı öğrenci hücresi" — TDV, İBB Kültürel Miras). Avlu ölçüleri
bilinmiyor.

Doğru yön "makul bir avlu çizip çıkan hücre sayısını kabullenmek" değil,
avluyu **sayı tutana kadar aramaktır**. İlk denemem 14 verdi ve üreticinin
kendi denetimi reddetti.

Ama kısıtın **gücü** de kayda geçmeli: parametre uzayı tarandığında 16
hücreyi veren **100 kombinasyon** çıktı ve revak açıklığı sonucu hiç
etkilemiyor. Yani sayı avluyu gevşek sınırlıyor, sıkılaştırmıyor; seçilen
ölçüler o kümeden en derli toplu olanı, kanıt değil. Doğruluk basamağı
**D3** kalıyor.

## Karar 3 — Mektebin **yazlık eyvanı** eklendi

Kaynak yapıyı iki parça olarak tarif eder: "kubbeli bir dershane ve kubbeli
**açık eyvan**; kışlık ve yazlık bölümleri vardır". İlk üretimde eyvan
yoktu — `mahalle_kit` mahalle mektebini modelliyor ve orada tek oda doğru.
Kite `eyvan` seçeneği eklendi (varsayılan **kapalı**, mahalle mektebi
değişmedi) ve üretici eyvansız üretimi reddediyor.

Ayrıca alt yapı bir süsleme değil **belge**: kaynak "yamaçta olduğu için
altına dükkân eklenmiştir" der, yani yapı bir alt yapının üzerinde yükselir.
Düz zemine oturtmak o cümleyi silerdi.

## Karar 4 — **Caminin koordinatı 164 m yanlıştı** ve külliye onu ele verdi

Medrese ve sıbyan mektebi ölçülü koordinatlarıyla eklenince belgeli göreli
konumlar **tutmadı**: medrese "caminin doğusunda" olmalıyken 213 m
kuzeydoğuda çıktı, mektep "kıble tarafında" olmalıyken ters yönde.

Sebep, iki yeni koordinat değil, **caminin kendi koordinatıydı**: elle
girilmiş (`APPROX`) ve gerçek konumdan **~164 m** uzaktı. Düzeltilen değer
**41,0267985 K / 29,0160674 D**.

Düzeltmeden sonra:

| ilişki | ölçüm | belge |
|---|---|---|
| mektep — cami | 33 m, **kıble bileşeni 1,00** | "caminin kıble tarafında" |
| medrese — cami | 52 m, doğu bileşeni 0,39 | "caminin doğusunda" |

Mektebin kıble bileşeninin tam 1,00 çıkması bağımsız bir doğrulamadır:
o ilişkiyi koordinatı türetirken **kullanmadım**.

Bu, Doğancılar'daki hatanın (ADR 0037, 771 m) aynı ailesinden. Ortak ders:
**elle girilmiş koordinatlar sessizce yanlıştır ve ancak bir başka ölçülen
şeyle çelişince ortaya çıkar.** Yeni test
`KulliyeMembersKeepTheirDocumentedRelativePositions` bu çelişkiyi kalıcı
olarak arıyor.

## Karar 5 — Medrese kıbleye dönük yerleştirilir

`LandmarkPlacer`ın kıble kuralına `medrese` türü eklendi: medresenin
**dershanesi mihraplı bir mekândır** ve külliyede camiyle hizalı durur.
Eğime göre döndürmek onu külliyeden koparırdı.

## Sonuç

- `MihrimahMedrese`: 16 hücre (belgeli), ayak izi 24,3×28,5 m, LOD0 7 234.
- `MihrimahMektebi`: kışlık kubbeli oda + **yazlık açık eyvan**, 2,90 m'lik
  dükkân katı üzerinde, LOD0 2 182.
- Sahnede 8 landmark; boş/gömülü malzeme yok.

## Açık kalanlar

- İmaret-tabhâne ve Kurşunlu Han: yer bilinmiyor.
- `mahalle_kit` mektep penceresinde pencere gölgesi ve şebekesi ters sarımla
  üretiliyor; `ensure_outward` düzeltiyor ama **kaynağı duruyor** (bu turda
  eklenmedi, önceden vardı).
