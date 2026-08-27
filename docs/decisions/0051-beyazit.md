# ADR 0051 — Beyazıt: oyunun padişahı bir şantiyenin sahibi

- **Tarih**: 2026-08-27
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/beyazit.md`)
- **Bağlam**: Faz 3, A-kademe. 1501-1506; 1632'de 126 yaşında.

## Bulgu — 1632 bu yapıda bir **an**, bir durum değil

TDV: şadırvanın üstündeki **sekiz sütuna oturan kubbeyi IV. Murad**
eklettirmiştir, **1623-1640** arası. Oyunun geçtiği yıl o aralığın **tam
ortasıdır**.

Şimdiye kadarki bütün "1632'de var / yok" kararları temizdi: yapı ya
1632'den önceydi ya sonra. Bu ilk kez **bilinemez** bir durum ve
bilinemezliğin kaynağı oyunun **kendi padişahı**.

**Karar: kubbe konmadı.** Gerekçe tarihsel — Murad IV 1623'te on bir
yaşında tahta çıktı ve gerçek iktidarı **1632'de** aldı; büyük hayrat
işleri o tarihten sonra beklenir. Ama bu bir **olasılıktır**, kesinlik
değil.

Katalog `sadirvan_dome: false` diye kaydediyor ve üretici bir bayrakla
(`--sadirvan-dome`) ötekini de üretebiliyor. Karar **görünür** kaldı; bir
gün tersi belgelenirse tek satır değişir.

## Karar 1 — 79 m kütleyi bağlar

Beyazıt'ın minareleri camiye değil **tabhâne kanatlarına** bitişiktir ve
aralarında **79 m** vardır. Bu, yapının en tanınan sayısal özelliğidir.

Kanat uzunluğu **elle girilmedi**: (79 − harimin dış genişliği) / 2 =
**19,37 m**. Kara surlarındaki burç aralığı (ADR 0049) ve Yedikule'nin
beşgen yarıçapı (ADR 0050) ile aynı ilke — **ölçülen sayı, türetileni
belirler**.

## Karar 2 — Türetilen kot ölçülü bandın **dışına çıkamaz**

Beyazıt'ın kubbe yüksekliği yayımlanmamış. Uydurmak yerine iki kısıta
bağlandı:

1. saçak, iki katlı yan neflerin çatısını geçmeli,
2. **kilit/çap oranı**, ölçülü dört selâtin camisinin bandına düşmeli —
   Ayasofya **1,68**, Sultanahmet **1,83**, Süleymaniye **2,00**, Üsküdar
   Mihrimah **2,12**.

Beyazıt: 35,00 / 16,78 = **2,09**. `validate` bandın dışını **reddediyor**
ve test bandı kataloğun kendisinden yeniden hesaplıyor — sabitler
düzelirse bant da düzelir.

Bu, Fatih Camii'nde (ADR 0048) kurulan yöntemin sertleştirilmiş hâli:
orada kot "sayılan bir değerden" türemişti, burada **ölçülen komşulardan**.

## Karar 3 — Sayılan pencereler mesh'te yaşar

Ana kubbede **yirmi**, her yarım kubbede **yedişer** pencere. Kubbe
**yirmi dilimli** üretiliyor ki pencereler dilim aralarına düşsün —
Ayasofya'nın kırk kaburgasıyla aynı ilke (ADR 0045).

## Kayda geçen çelişki

Tabhâne hücre sayısında kaynaklar ayrışıyor: TDV *"kubbeli **dörder**
hücre"*, yaygın anlatım *"**beşer** kubbe"*. İkisi aynı şeyi saymıyor
olabilir (hücre ≠ kubbe). **TDV alındı**, çelişki kataloğa yazıldı.

## 1509 ve 1573

1509 depreminde kubbe *"dağılıp pâre pâre"* oldu, medrese yıkıldı. Sinan
**1573-74**'te *"bir kemer-i cedîdle"* yapıyı takviye etti. 1632'de
ayakta olan şey **iki yapısal müdahaleden geçmiştir** — ama biçimi
değişmemiştir. Fatih Camii'nin tersi bir hikâye: orada deprem şemayı
değiştirdi, burada değiştirmedi.

## Sonuç

- `Beyazit` LOD0 4 802; ayak izi 85,0 × 82,0 m, yükseklik 45,90 m.
- Ölçülü: kubbe **16,78 m**, harim **37,06 × 36,80 m** (kaynak "kare
  biçimli" der; ölçü 26 cm fark verir — **tarif ölçüyle doğrulandı**),
  minareler arası **79 m**.
- Sayılan: 2 yarım kubbe, 4 pâye, 20 + 2×7 pencere, 2 minare / birer
  şerefe, 4+4 tabhâne hücresi, 24 avlu kubbesi.
- Sahnede **20 landmark**. EditMode **228/228**.

## Bu turda ayrıca

Test koşumu bir **derleme hatasını** yuttu ve yeşil döndü: ADR 0052.

## Açık kalanlar

- Külliyenin öteki yapıları (medrese 1507, sıbyan mektebi 1507, imaret,
  kervansaray 1507-08, hamam) ve **II. Bayezid türbesi** — hepsi 1632'de
  ayakta, hiçbiri üretilmedi.
- Tabhâne kanatları kaba kütle; hücre bölünmesi yalnızca kubbelerden
  okunuyor.
- Kilit kotu **D3** ve türetilmiş; ölçülü bir kot bulunursa değişir.
