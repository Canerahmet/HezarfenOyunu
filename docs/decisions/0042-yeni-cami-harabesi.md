# ADR 0042 — Yeni Cami: 1632'de bir cami değil, bir şantiye

- **Tarih**: 2026-08-26
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/yeni_cami_harabe.md`)
- **Bağlam**: Faz 3, A-kademe. PLAN bunu baştan beri "kritik detay" sayar.

## Karar 1 — Yapı **çatısız bir kabuk** olarak kurulur

İnşaat 1597'de başladı, **1603**'te durdu (III. Mehmed öldü, Safiye Sultan
Eski Saray'a gönderildi), 1604'te tamamen bırakıldı. İş durduğunda yapı
**ilk pencere seviyesine** kadar yükselmişti. **1632'de kabuk 29
yaşındadır**; ancak 1660-63'te Turhan Sultan tamamlattı.

Yani 1632'de görülen şey duvarlar ve fil ayaklarıdır: **kubbesiz,
minaresiz, kurşunsuz**. Halk ona **"Zulmiye"** derdi — aşırı masraf ek
vergilere yol açtığı ve yapı harabeye döndüğü için.

Bu, projenin en kolay yapılacak hatasıdır: "tanıdık Yeni Cami"yi koymak
1632'yi siler. Üretici ve test bunu birlikte yasaklıyor — ve **bayrağa
değil kütleye** bakarak: kabuğun toplam yüksekliği duvar + subasman payını
aşarsa hata verir. `roofed=False` yazmak çatısız olduğunu kanıtlamaz
(Hüdâyî türbesinde `acik` bayrağı tam böyle sessiz kalmıştı, ADR 0037).

## Karar 2 — **Yıkıntı değil, durmuş şantiye**

İkisi farklı görünür ve karıştırmak yapıyı yanlış anlatır. Yıkıntının üstü
düzensiz kırılır; durmuş bir şantiyenin üstü **sıra sıra** biter.

İlk denemede yedi parça ve alternatif kotlar vardı (0,1,0,2,1,3,1) ve
render'da **mazgal** gibi okundu — sanki bir kale bedeni. Beş parçaya,
tekdüze artan bir diziye ve cephe başına taban kaymasına çevrildi: uzun
düzlükler, birkaç basamak, ve bir cephe ötekinden daha ileri. Avluya
işlenmiş ama yerine konmamış **taş yığınları** kondu — "yıkıntı" ile
"durmuş iş" arasındaki farkı tek başına anlatan şey budur.

## Karar 3 — Ölçülen plan korunur, yükseklik türetilir

Harim **35,50 × 40,90 m** ölçülüdür ve teste bağlandı. Ana kubbe **dört
fil ayağına** oturacaktı (sayım). Kubbe çapı kaynaklarda 16,20 m (mimari
tarif) ve 17,5 m (yaygın anlatım) — muhtemelen iç/dış farkı, §5.4'teki
Mihrimah çelişkisinin aynısı; 1632 kabuğunda ikisi de görünmez.

Duvar yüksekliği ölçülmedi; "ilk pencere seviyesi" tarifinden türetildi ve
**D3**'tür.

## Karar 4 — Konum 148 m düzeltildi; kalan fark **DEM'in kendisi**

Katalog değeri elle girilmişti ve yapıyı yamaca koyuyordu (kot 12,1 m).
Yeni Cami Haliç kıyısında **bataklık zemine** kuruldu. Ölçülü koordinat:
**41,0168787 / 28,9722347** (Fatih/Rüstem Paşa).

Sonuç: kot 14,6 → **10,8 m**, denize 230 → **170 m**. Kalan fark bir
yerleştirme hatası **değil**: Copernicus GLO-30 bir **yüzey** modelidir ve
yoğun yapılı Eminönü'nde çatıları okur. Yapıyı ölçülü konumundan
kaydırarak "düzeltmek" doğru sayıyı yanlış nedenle bozmak olurdu.

Bu turun ikinci koordinat düzeltmesi; toplamda beşinci
(Doğancılar 771 m, Üsküdar Mihrimah 164 m, İncili Köşk 156 m,
Okmeydanı 700 m — o farklı bir tür —, Yeni Cami 148 m).

## Karar 5 — Harabe de **kıbleye** döner

Yön eğimden geliyordu (46°). Ama Yeni Cami'nin **mihrap duvarı**
1597-1603 arasında örülmüştü: bitmemiş olması yönünün bilinmediği anlamına
gelmez — plan kıbleye göre kurulur, duvarlar ondan sonra yükselir.
`harabe` türü kıble kuralına eklendi; ölçüldü: **330,4°**.

## Sonuç

- `YeniCamiHarabe` LOD0 360; ayak izi 36,90 × 43,80 m, yükseklik 9,54 m.
- Yerleşim (−128, 10,8, −976); Galata Kulesi'ne ~1 km.
- Sahnede **14 landmark**; boş/gömülü malzeme yok. EditMode **189/189**.

## Açık kalanlar

- Çevresindeki **sıkışık gayrimüslim mahallesi ve mezbelelik** (kaynakta
  geçiyor) modellenmedi — Faz 4.
- Kabuğun **iç** düzeni (kemer başlangıçları, iskele izleri) yok; siluet
  ve orta mesafe için gerekmiyor.
