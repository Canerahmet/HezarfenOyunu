# Beyazıt II Camii — inceleme notu

İnceleme paketi: `renders/review/Beyazit_v1/contact_sheet.png`
Karar kaydı: **ADR 0051**. Araştırma: RESEARCH.md §5.17.

## 1632 bu yapıda bir *an*, bir durum değil

Şimdiye kadarki bütün "1632'de var / yok" kararları temizdi: yapı ya
öncesindeydi ya sonrasında. Burada ilk kez **bilinemez** bir şey var —
ve bilinemezliğin sebebi oyunun **kendi padişahı**.

TDV, şadırvanın üstündeki sekiz sütuna oturan kubbeyi **IV. Murad**'ın
eklettiğini yazıyor, **1623-1640** arası. Oyunun yılı o aralığın **tam
ortası**.

**Kubbeyi koymadım.** Gerekçem tarihsel: Murad IV 1623'te on bir yaşında
tahta çıktı ve gerçek iktidarı **1632'de** aldı; büyük hayrat işleri
ondan sonra beklenir. Ama bu bir **olasılık**. Üretici bir bayrakla
ötekini de yapıyor (`--sadirvan-dome`); istersen değiştiririm.

## 79 metre kütleyi bağlıyor

Beyazıt'ın minareleri camiye değil **tabhâne kanatlarına** bitişik ve
aralarında **79 m** var — yapının en tanınan sayısal özelliği bu.

Kanat uzunluğunu **yazmadım**: (79 − harimin dış genişliği) / 2 =
**19,37 m**. Kara surlarındaki burç aralığı ve Yedikule'nin yarıçapıyla
aynı ilke — ölçülen sayı türetileni belirliyor.

## Kubbe yüksekliği yayımlanmamış; uydurmadım

İki kısıta bağladım:

1. saçak, iki katlı yan neflerin çatısını geçmeli,
2. **kilit/çap oranı**, ölçülü dört caminin bandına düşmeli —
   Ayasofya 1,68, Sultanahmet 1,83, Süleymaniye 2,00, Üsküdar Mihrimah
   2,12.

Beyazıt: 35,00 / 16,78 = **2,09**. Doğrulama bandın dışını reddediyor ve
test bandı kataloğun kendisinden yeniden hesaplıyor.

## Ölçüler

| | | |
|---|---|---|
| Kubbe | **16,78 m** | ölçülü |
| Harim | **37,06 × 36,80 m** | ölçülü — kaynak "kare biçimli" der, ölçü 26 cm fark verir |
| Minareler arası | **79 m** | ölçülü |
| Kilit | 35,00 m | **türetildi, D3** |
| Pencere | kubbede **20**, yarım kubbelerde **7'şer** | sayılan |
| Tabhâne | **4'er** hücre | TDV (yaygın anlatım "5'er kubbe" der) |
| Avlu | **24** kubbeli revak | sayılan |
| LOD0 | 4 802 üçgen | |

## Bir şey daha buldum ve tek yapıyı aşıyor

Bu tura eklediğim dört test **koşmadı** — dosyada bir derleme hatası
vardı ve test assembly'si derlenmedi. Ama koşum **223/223 yeşil** döndü,
çünkü Unity bir önceki sağlam assembly'yi çalıştırdı.

Bu, üç kez yakaladığım "atlanan test geçen test gibi görünür"den
**daha kötüsü**: sayı bile yalan söylemiyor, 223 gerçekten koştu. Yalan
olan, o 223'ün *hangi kodun* 223'ü olduğu.

Kalıcı bekçi yazdım: kaynaktaki `[Test]` sayısı ile derlenen
assembly'deki sayı tutmazsa test patlıyor. İşe yaramasının sebebi ince —
**bu bekçi eski assembly'de de var**, yani derleme çöktüğünde o eski
nüsha koşuyor, diski okuyor ve tutmadığını görüyor. Ayrıntı: ADR 0052.

## Sana sorduğum

1. **Şadırvan kubbesi**: koyayım mı? Gerekçemi yukarıda yazdım ama
   1632 o aralığın tam ortası ve bu bir zar atışı.
2. **Kubbe yüksekliği** (35 m) türetilmiş; yapı sana fazla basık ya da
   fazla yüksek görünüyorsa söyle.

## Bilerek eksik

- Külliyenin öteki yapıları (medrese, sıbyan mektebi, imaret,
  kervansaray, hamam) ve **II. Bayezid türbesi** — hepsi 1632'de ayakta.
- Tabhâne kanatları kaba kütle; hücre bölünmesi yalnızca kubbelerden
  okunuyor.

---

**Onay**: _(bekliyor — "OK vN" yaz)_
