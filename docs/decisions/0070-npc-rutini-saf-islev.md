# ADR 0070 — NPC rutini saf bir işlevdir

**Durum:** Kabul edildi (uygulandı)
**Tarih:** 2026-08-28
**Bağlam:** Faz 6, NPC yapay zekâsı Katman 1

---

## Karar

Bir NPC'nin nereye gideceği **`(vakit, tohum) → hedef türü`** saf
işleviyle belirlenir. Ajanın kendi durumu, hafızası, dünkü kararı yoktur.

## Neden

Plan Bölüm 11.3: *"Şehri yaşatan asıl katman budur — açık dünya hissinin
büyük kısmı rutin ve tepkilerden gelir, diyalogdan değil."*

Bir şeyin oyunu taşıdığını iddia ediyorsak **ölçebilmemiz** gerekir. Ama
durum taşıyan bir ajan ordusunu ölçmek, onları çalıştırmayı gerektirir:
model, animasyon, kare, saatler. Saf işlev bu bağı koparıyor — şehrin bir
gününü **hiç çizmeden** simüle edip sayabiliyoruz.

Bedeli açık: NPC dün ne yaptığını hatırlamaz. Karşılığı şu sorulara
sayıyla cevap verebilmek:

- Öğle ezanında mescide akış oluyor mu?
- Yatsıdan sonra sokaklar boşalıyor mu?
- Kaç kişi hedefine gidemiyor?

## Ölçülen gün (2000 sakin, 1 Mayıs)

| vakit | dışarıda | mescitte | ulaşılamaz | en çok gidilen |
|---|---:|---:|---:|---|
| Sabah | %36,5 | %32,4 | 0 | Ev 1100, Mescit 648 |
| Güneş | %26,2 | %5,6 | 52 | Dükkân 543, **Mektep 327** |
| Öğle | %42,2 | **%33,1** | 64 | **Mescit 661**, Dükkân 341 |
| İkindi | %40,1 | %7,6 | 51 | Dükkân 519, Çeşme 406 |
| Akşam | %26,3 | %25,9 | 0 | Ev 988, Mescit 517, Kahvehane 252 |
| Yatsı | **%3,8** | %3,5 | 0 | Ev 1669 |

Günün ritmi sayıda görünüyor: öğlede mescit zirve yapıyor (%33,1 — iş
vaktinin altı katı), ikindide çarşı ve çeşme doluyor, **yatsıdan sonra
dışarıda kalan %3,8** ve o da ases devriyesi.

## Çizelge vakte bağlıdır, saate değil

"Sabah 7'de dükkânı aç" 20. yüzyıl cümlesidir. 1632'de gün beş vakitle
bölünür ve vakitler mevsime göre saatlerce kayar (ADR: `VakitHesabi`).
Esnaf sabah ezanıyla kalkar, kışın da yazın da. Saate bağlamak, aralıkta
kepenkleri karanlıkta açtırırdı.

## Kronoloji davranıştır, metin değil

**2 Eylül 1633** fermanıyla kahvehaneler kapatıldı (TDV "Kahve"; BA,
A.DVN, nr. 25/47 — RESEARCH §6). Oyun o eşiği geçince akşam rutini
kendiliğinden değişiyor:

| akşam hedefi | 1632 | 1634 |
|---|---:|---:|
| Kahvehane | **252** | **0** |
| Ev | 988 | **1240** |

Aynı çizelge, aynı şehir, farklı yıl. Tarih bir kodeks yazısı değil,
sokakta görünen bir fark.

## Meslek dağılımı — T2

On meslek, payları plan Bölüm 11.3'ün listesinden ve şehrin kendi
dokusundan: %30 esnaf, %18 çocuk (mektep 130 tane ve bu meslek olmasa
hepsi boş dururdu), %8 kayıkçı (Haliç'te köprü yok, ulaşım onların işi),
%4 ases.

Bu oranlar **belgeli değil** — 1632'nin meslek dağılımı sayıyla kayıtlı
değil. Ama bir şehrin dörtte birinin bekçi olmadığı da kesin. T2, taslak;
düzeltmek bir alan değiştirmektir.

## Ne yapılmadı

- **Hafıza yok.** NPC dün kiminle konuştuğunu bilmez. Katman 2'nin
  (yazarlıklı diyalog) işi.
- **Görsel ajan yok.** Bu tur rutinin kendisini kurdu; onu yürüten
  GameObject ve kalabalık kademesi ayrı tur.
- **Cuma yok.** Cuma namazı mahalle mescidine değil selâtin camisine
  akıtır ve bu ayrı bir çizelge katmanı.

## Doğrulama

`SehirGunuTests` (7 test): her mesleğin her vakitte işi var, her hedef
şehirde gerçekten bulunuyor, %95'ten fazlası hedefine yürüyerek
gidebiliyor, öğle akışı iş vaktinin en az 1,5 katı, gece dışarısı
gündüzün üçte birinden az **ama sıfır değil**, kahve yasağı akşamı
değiştiriyor, aynı tohum aynı günü veriyor.

291 test yeşil, sıfır atlanan.
