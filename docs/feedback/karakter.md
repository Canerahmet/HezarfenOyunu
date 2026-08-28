# Karakter ve kıyafet — inceleme paketi v5

**Durum:** onay bekliyor
**Tarih:** 2026-08-28
**Paketler:** `renders/review/Hezarfen_Govde_v1/`,
`Hezarfen_Sivil_v2/`, `Hezarfen_Ucus_v5/`
**Karar kaydı:** [ADR 0065](../decisions/0065-karakter-ve-kiyafet.md)

---

## Önce iki dürüstlük notu

**1. Hezarfen'in portresi yok.** Ne minyatürü, ne tarifi. Şahsı hakkında
bilinen tek şey Evliya'nın birkaç cümlesi. Bu gövde bir **benzerlik
iddiası taşımıyor**; dönemin genel yetişkin erkek anatomisi.

**2. Kıyafet 25 yıl sonrasından okundu.** Rålamb albümü 1657–58, oyun
1632. Ana hatlar (şalvar–gömlek–entari–kuşak–sarık) bu aralıkta değişmedi
ama ayrıntı değişebilir. Mundy albümü (1618) öbür yandan yaklaşıyor —
**1632'nin tam ortasında bir kaynak yok** ve bu boşluk kapatılamaz, ancak
söylenebilir. Kıyafet bu yüzden T2, taslak.

## Taban gövde: projenin tek istisnası

Bu projede her şeyi scriptle sıfırdan ürettim. Karakter tek istisna ve
istisnayı plan koydu: taban gövde Blender Studio'nun CC0 paketinden
geliyor. Sebep anatomi değil **deformasyon** — dirsek, diz, omuz ve göz
çevresindeki kenar halkaları animasyon için özel yerleştirilmiş. Bunu
üretebilirim; **ürettiğimin iyi olduğunu ölçemem.** Ev için sayı var,
omuz halkası için yok.

Pakette dört gövde vardı. Seçimi ölçüm yaptı: `body_male_realistic`
**1,69 m**, `body_male_stylized` 1,80 m. Bu projenin bütün inceleme
paketleri **1,70 m'lik ölçek figürüne** göre onaylandı — stilize gövdeyi
seçmek 36 landmark ve 142 mahalleyi sessizce %6 kaydırırdı.

CC0 atıf istemiyor; künyeye yine de yazacağım. "Zorunlu değil" ile
"söylemeye değmez" aynı şey değil.

## Kıyafet: okundu, kopyalanmadı

Dört plaka indirdim (kamu malı, `refs/ralamb/`). Mimaride uyguladığımız
kuralın aynısı: minyatürü **kopyalamadım**, oranları okudum.

Altı kural çıktı — hepsi `docs/RESEARCH.md`'de kayıtlı:

- **Entarinin boyu işi söyler.** Oturan adam ayak bileğine, çalışan adam
  dize giyer. Uçuş varyantının kısalığı benim tercihim değil; plakaların
  söylediği şey.
- Kuşak doğal belde, dar, kontrast renkte.
- Şalvar entarinin altından **görünür** — entari onu örtmez.
- **Dizlik gerçek bir öğe** (plaka 50). Planın istediği "dizlik" uydurma
  değil, gözlenmiş.
- Başlık rütbe göstergesi; Hezarfen ne paşa ne asker → orta hacim sarık,
  altında kırmızı kavuk çekirdeği (plaka 35 ve 50'de görünüyor).

Renkler de plakalardan: mavi-yeşil entari, kırmızı çakşır, dar kırmızı
kuşak, beyaz sarık, sarı mest.

## Üç durum

| durum | boy | etek | üçgen (LOD0/LOD1) |
|---|---:|---:|---:|
| `Hezarfen_Govde` (çıplak taban) | 1,700 m | — | 21 160 / 7 406 |
| `Hezarfen_Sivil` | 1,793 m | 0,09 m (ayak bileği) | 60 068 / 18 020 |
| `Hezarfen_Ucus` | 1,793 m | 0,54 m (diz üstü) | 60 388 / 18 116 |

Giysi elden modellenmedi: **gövdeden türeyen kabuklar**. Oturması bir göz
kararı değil, yapısal garanti — poz değişince entari gövdeye batmaz,
çünkü zaten o gövdenin ofseti.

## Unity'de duran bir çelişkiyi de bulduk

Karakter 1,70 m çıkınca üç sayının üç ayrı insanı tarif ettiği görüldü:
model **1,70 m**, yürüyüş kapsülü **1,80 m**, göz yüksekliği **1,70 m**
(yani 1,81 m'lik bir adam). Göz alanının yanındaki not "1,70 m: ölçü
figürüyle aynı" diyordu — nota bakınca doğru, sayıya bakınca yanlış:
**1,70 o figürün boyu, gözü değil.**

Düzelttim: göz 1,59 m (modelin kendi ölçüsünden `boy − baş/2`), kapsül
1,70 m. Şunu bilmen gerekir: **şehir artık 11 cm daha alçaktan
görünüyor** — ve doğrusu bu, çünkü şehri onaylarken baktığın figür
1,70 m boyundaydı.

## Senden gereken

**`OK v5`** yeterli. Sorun görürsen maddele.

Özellikle üç şeye bakmanı isterim:

1. **Renkler doğru mu?** Mavi-yeşil entari + kırmızı şalvar plakalardan
   geliyor ama o kombinasyon plaka 20 ve 35'te iki AYRI adamda. Hezarfen
   için birleştirdim. Daha sade istersen (tek renk entari, koyu şalvar)
   bir alan değişikliği.
2. **Sarık hacmi.** Şu an orta: plaka 20'nin hacimli sivil sarığı ile
   plaka 50'nin sıkı asker sarığının arası. Hezarfen'i daha "âlim"
   göstermek istersen büyütürüm — ama sarık büyüdükçe rütbe iddiası büyür.
3. **Uçuş entarisi yeterince kısa mı?** Dizin hemen üstünde bitiyor.
   Kuleden atlayacak adam için daha da kısa (uyluk ortası) yapabilirim
   ama plakalarda o boy yok; uydurma olur.

## Rig — ve plandan bir sapma

Karakter artık **rig'li ve Unity'de**: 22 kemikli Humanoid iskelet, iki
prefab (`PF_Hezarfen_Sivil`, `PF_Hezarfen_Ucus`), avatar **2/2 geçerli**.

Ama plandan saptım ve bunu sana sormam gerekiyor
([ADR 0066](../decisions/0066-rig-rigify-degil.md)):

Plan **Rigify** diyor. Rigify'ın verdiği şey elle poz vermek için
IK/FK kontrol iskeletidir — yüzden fazla kemik. Animasyonları scriptle
üreteceğim için o kontrollerin hiçbirini kullanmayacağım; geriye
kalan tek iş kemik adlarını Unity'ninkilere çevirmek olurdu. Onun
yerine Unity Humanoid'in tam istediği 22 kemiği doğrudan kurdum.

**Kaybettiğimiz tek şey:** Blender'da elle poz vermek zorlaşır (IK yok).
Gerekirse Rigify'ı sonradan bu iskeletin üstüne takabilirim — tersi
daha zor.

Eklem yerleri tablodan değil **gövdeden ölçüldü**: dirsek %63,7, diz
%29,4 kotta. Bu ölçüm iki kez yanlış yeri buldu ve ikisini de sayı
yakaladı — bir keresinde kol çizgisi **ayakları** kola sayıyordu ve
bilek ayak bileği hizasına düşmüştü.

## Animasyon — 13 klip, ölçülmüş

Plan Bölüm 10'un istediği set hazır ve Unity'de: duruş, yürüme, koşma,
merdiven, kuşanma, kalkış, süzülüş (+ pitch/roll blend ağacının dört uç
pozu), iniş, çakılma.

Animasyon bu projenin ölçemediği ilk şey gibi görünüyordu. Değilmiş —
**yere basan ayak kaymamalı**, ve bu bir sayı:

| klip | süre | tempo | ayak kayması |
|---|---:|---:|---:|
| Yürüme | 1,10 s | 110 adım/dk | **0,7 cm** |
| Koşma | 0,73 s | 165 adım/dk | **1,8 cm** |
| Merdiven | 1,27 s | 96 adım/dk | **0,4 cm** |

Tempoyu da ölçüyorum, çünkü tek başına kaymayı sıfırlamak yetmiyor:
adımı uzatıp süreyi de uzatarak sıfırlanır ve karakter **74 adım/dakika**
ile cenaze temposunda yürür. Gerçekten oldu, sonra düzeltildi.

Merdiven ayrı bir iş çıktı: tırmanan adamın gövdesi yükselir, ayağı
basamakta durur. Düz zemin ölçütünü oraya uygulamak yanlış şeyi
ölçmekti. Şimdi ayak yolu merdivenin geometrisinden türüyor (rıht
0,19 m, basamak 0,26 m — **bunlar T2/taslak, Galata'nın merdiven ölçüsü
kaynakta yok**) ve açılar IK ile çözülüyor.

**Bu turda yedi hata buldum ve yedisi de ölçüm aletindeydi**, animasyonda
değil. Bunu yazıyorum çünkü bir ders: bozuk olan çoğu zaman ölçtüğün şey
değil, ölçtüğün şeyi ölçme biçimin.

## Bilerek yapılmayanlar

- **Saç ve sakal** — hair cards, ayrı tur. Şu an gövde saçsız ve bu
  eksiklik render'da görünüyor.
- **Animator kontrolcüsü ve blend ağacı** — klipler var, geçişleri
  kuran durum makinesi yok. Sıradaki tur; Faz 5'in kabul ölçütü
  ("kesintisiz oynanabiliyor") onu gerektiriyor.
- **Üçüncü şahıs kamerası** (omuz-üstü ↔ geniş geçişi) — aynı tur.
- **Yüz detayı** — taban ağın yüzü genel; Hezarfen'e ait bir yüz
  yapılmayacak, çünkü ait olduğu bir yüz yok.
- **Animator ve kontrolcü** — prefab'larda Animator YOK. Boş bir
  Animator "animasyon var" gibi görünürdü.
- **Kanat kayışlarının vücuda oturması** — kanat kayışları kanadın kendi
  parçası; karakterle buluşmaları rig turunda.
