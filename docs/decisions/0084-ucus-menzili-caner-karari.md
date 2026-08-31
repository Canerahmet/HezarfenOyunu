# ADR 0084 — Uçuş menzili yetmiyor: iki seçenek, Caner'in kararı

- **Tarih:** 2026-09-01
- **Durum:** **Caner'in kararı bekleniyor**
- **Bağlam:** ADR 0082 (uçuş oyunda yoktu), ADR 0083 (sarmal dalış)

## Soru

Kuleden Doğancılar'a uçuş, bugünkü fizikle **bitirilemiyor**. Kanat
düzeltildi, pilot düzeltildi, termik bağlandı — ve açık hâlâ duruyor.
Kapatmanın iki yolu var ve ikisi de bir şeyden vazgeçiyor. Hangisinden
vazgeçileceği bir **tasarım kararı**, mühendislik kararı değil.

## Ölçülen durum

| ne | değer | nereden |
|---|---|---|
| Kule şerefesi → Doğancılar, yatay | **3.336 m** | `Perde2Dilimi` |
| Kot farkı | **51,6 m** | ölçüldü (RESEARCH ~62 m der) |
| Kanadın en iyi süzülme oranı | **11,56 : 1** | `Aerodynamics`, doğrulandı |
| Kaldıraçsız menzil | **597 m** | 51,6 × 11,56 |
| **Gereken oran** | **65 : 1** | 3.336 ÷ 51,6 |
| Ölçülen en iyi kaldıraç | **+1,87 m/s** | kuleden 160 m batı-güneybatı |
| 33° yatışta batış | **2,12 m/s** | `SustainedBank_DoesNotSpiralDive` |
| **Termikte net tırmanış** | **−0,25 m/s** | 1,87 − 2,12 |

Son satır her şeyi söylüyor: **bu arazideki en iyi termikte bile dönmek
irtifa kaybettiriyor.** Termik var, kanat sağlam, pilot doğru uçuyor —
ama dönüş verimi kaldıracın altında olduğu sürece yükselmek mümkün değil.

Düz uçuşta katedilen mesafe 177 m'den **1.206 m**'ye çıktı (kontrol
uçuşu 630 m, teorik 597 m ile tutuyor). Gereken 3.336 m.

## Seçenek 1 — Dönüş verimini düzelt (fizik işi)

Asılı planör gerçekte sabit hücum açısı değil **sabit hava hızı**
trimler; ağırlık aktarımı trim hızını değiştirir. Model bunu yaparsa
sarmal kendiliğinden kapanır ve 33° batışı ~1,40'a inebilir. O zaman
net tırmanış +0,47 m/s olur ve 237 m'lik açık ~8 dakikada kapanır.

- **Kazanç:** uçuş gerçekten *uçuş* olur — beceriyle yükselmek,
  termik aramak, yolu planlamak. ADR 0037'nin "şans değil beceri"
  iddiası karşılığını bulur.
- **Bedel:** `GlideController`'ın pitch döngüsü baştan yazılır. Bir
  prototip denendi ve tutmadı (alfa 55°'de salınıma girdi). Bu **ayrı
  bir tur** ve riski gerçek: bugün geçen sekiz uçuş testi yeniden
  ayarlanmak zorunda kalabilir.

## Seçenek 2 — İniş hedefini menzil içine al (anlatı işi)

597 m'lik kaldıraçsız menzil, Galata'dan **Haliç'in karşı kıyısına**
(Cibali–Unkapanı hattı) yeter. Doğancılar (Üsküdar) yerine oraya inilir.

- **Kazanç:** bugün çalışır. Perde 2 bitirilebilir hâle gelir ve
  `TepkiKodeksi` — projenin en iyi yazılmış içeriği — ilk kez bir
  oyuncuya ulaşır.
- **Bedel:** anlatı değişir. Evliya'nın rivayeti **Üsküdar'a iniş**
  der; Haliç'i geçmek daha kısa ve daha az etkileyici bir uçuştur.
  Oyunun adını taşıyan an küçülür.

## Önerim

**Seçenek 1**, ama Seçenek 2'yi *geçici* olarak da alarak.

Yani: hedefi şimdilik Haliç kıyısına çek ki Perde 2 uçtan uca
oynanabilsin ve `TepkiKodeksi` ulaşılabilir olsun; dönüş verimi ayrı bir
turda düzeltilince hedef Doğancılar'a geri taşınsın. Böylece oyun her an
oynanabilir kalır (PLAN II.J: "her fazın sonunda oynanabilir bir şey
bırakılır") ve tarihsel iddia terk edilmez, ertelenir.

Ölçüm bunu tutar: `Ucus denemesi` kapısı %70'i geçmeden Perde 2 "var"
sayılmaz, hedef nereye konursa konsun.

## Caner'e soru

1. Hedef geçici olarak Haliç kıyısına çekilsin mi, yoksa Doğancılar'da
   kalıp uçuş bitirilemez olarak mı beklesin?
2. Dönüş verimi düzeltmesi (pitch döngüsünün yeniden yazımı) bir sonraki
   turun ana işi olsun mu, yoksa cila fazına mı ertelensin?
