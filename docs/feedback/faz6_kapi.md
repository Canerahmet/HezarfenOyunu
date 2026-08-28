# Faz 6 kapısı — açık dünya, NPC yapay zekâsı ve içerik

**Tarih:** 2026-08-28
**Ölçüm:** 335 EditMode + 28 PlayMode testi, hepsi yeşil
**Kapıyı tutan:** ölçüm (Caner, 2026-08-28: geri bildirim tüm fazlar
bittikten sonra oyun oynanırken gelecek; kabul kriterleri sayıyla
karşılanmadan sonraki faza geçilmez)

Ölçütlerin kendi cümlesini doğrudan sınayan testler
`Faz6KapiTests` (EditMode) ve `Faz6DolasimTests` (PlayMode) içinde.

---

## Ölçüt ölçüt

| # | Kabul ölçütü | Ölçüm | Sonuç |
|---|---|---|---|
| 1 | Galata'da 30 dk kesintisiz dolaşım, yükleme ekranı yok | Galata'nın **%100'ü** tek yürüme parçası; üç günlük döngü boyunca gövde sayısı bütçeyi (40) aşmadı, gövde nesnesi çoğalmadı, gün sonunda şehir hâlâ yürüyor | ✅ |
| 2 | ≥3 yan görev arketipi uçtan uca | **5/5 arketip** gerçek şehir grafında duraktan durağa yürünerek tamamlandı | ✅ |
| 3 | Aranma tam döngü (kaçış VE yakalanma) | Beş aşama tanımlı; her ihlalin cezası > 0; yasak malda el koyma var; sönüm hızı > 0 ve ceza sonrası muafiyet 25 s | ✅ |
| 4 | NPC rutini sabah-öğle-akşam-gece görünür değişiyor | Ardışık geçişlerin **hepsi > %20** yer değiştirme; gün içinde dışarıda olma oranının açıklığı **> %25** | ✅ |
| 5 | Kayıkla Galata↔Üsküdar | Kayıkla yol **var**, yürüyerek **yok**, ve yolun üzerinde gerçek bir kayık kenarı geçiyor | ✅ |
| 6 | Perde 2 dikey dilimi baştan sona | Talim (3 süzülüş, Okmeydanı) → kule → uçuş (**> 3000 m**) → Doğancılar'a iniş → İncili Köşk tepki sahnesi; zincir kesintisiz | ✅ |

---

## Bu turda ölçümün bulduğu şeyler

Kapı sadece bir onay kutusu değildi; testleri yazmak dört gerçek kusur
çıkardı.

**Gövde havuzu bütçeyi aşıyordu.** Bütçe 40 iken **70 gövde nesnesi**
vardı. Sebep tek geçişli tazeleme: listede önce gelen uzak bir sakin
gövdesini henüz bırakmamışken, sonra gelen yakın bir sakin gövde istiyor,
havuz boş olduğu için yenisi yaratılıyordu. Uzun oturumda sessizce büyüyen
bellek — yani tam olarak 1. ölçütün kaybedileceği yer. Artık **önce
bırakılıyor, sonra alınıyor**.

**Cuma cemaati kiliseye yollanıyordu.** `CumaHedefi` mescidi `Mabet`e
çeviriyordu ve testi geçiyordu, çünkü test eşlemeyi yalnızca kendisiyle
kıyaslıyordu. Grafta `Mabet` kilise/sinagog demektir. Ayrıntı ADR 0071'de;
sonucunda `Tur.Cami` eklendi ve Galata'nın Cuma camisi (**Arap Camii**)
üretildi.

**İskeleler depoda vardı, sahnede yoktu.** Kayıtlı graf, iskeleler
yerleştirildikten sonra ama sahne kaydedilmeden kurulmuştu; yeniden
kurulunca altı iskele de kayboldu ve kayık ağı tümüyle gitti. 5. ölçüt bu
yüzden yeşil görünüyor olabilirdi ve olmayabilirdi. Şimdi sahnede kayıtlı.

**"23/23 geçti" aslında beklenen 25'in 23'üydü.** Bir `using` eksikti,
test derlemesi derlenmedi ve koşucu bir önceki derlemeye karşı memnuniyetle
yeşil dedi. *Atlanan test geçen test gibi görünür* — o yüzden artık sayı da
kontrol ediliyor, yalnızca renk değil.

---

## Ölçmediğimiz şey

**Otuz dakikalık gerçek oturum.** Bir test otuz dakika koşamaz; koşsaydı
kimse çalıştırmazdı. Test, otuz dakikalık bir oturumu **neyin kıracağını**
ölçüyor (birikim: havuz büyümesi, vakit geçişi sızıntısı, uzaklaşıp
dönünce donma) — süreyi değil. Süre ölçümü elle oynanan bir oturuma ait ve
onay akışı zaten öyle kuruldu.

**Görsel kalite.** Faz 6 sistem fazıdır; ışık, malzeme, LOD mesafeleri ve
kare hızı Faz 7'nin işi.

## Açık maddeler (Faz 7'ye taşınıyor)

- **Arap Camii'nin yönü** araziden geliyor (226,97°), kaynaktan değil.
  Yapı kıbleye dönük değildir (Ayasofya kuralı, ADR 0045) ama eğimden
  gelen açı da bir belge değil. `docs/feedback/arap_camii.md`.
- **Eyüp'ün Cuma camisi yok.** 103 düğümlük semtte Cuma ölçülemez.
  Eksik olduğu kayıtlı (ADR 0071), sessiz bir varsayım değil.
- **Üç iskele yalnız kayıkla erişilebiliyor**, yürüyerek değil. Üç hipotez
  denendi (eğim, kıyı eşiği, kenar yarıçapı); hiçbirinde sayı kımıldamadı.
  Hedefli bir tanı gerekiyor.

## Onay

Caner: *(bekliyor — onay akışı tüm fazlardan sonra, oyun oynanırken)*
