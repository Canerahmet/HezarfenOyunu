# Oynarken gelen geri bildirim — Faz 8

Caner build'i çalıştırıp oynuyor; gelen her madde buraya, geldiği hâliyle
yazılıyor. Onay formatı "OK vN".

---

## 001 — Menüden oyuna girilemiyor

**Caner (2026-08-29, build 547 MB):**
> "baslangic menusunde tuslara basmama ragmen oyuna giremedim."

**Durum:** düzeltildi (v2 build bekliyor).

### Ne olmuştu

İki ayrı kusur vardı ve ikisi cümlenin iki yarısına birebir oturuyor.

**1. Fare — düğmelerin hiçbiri bağlı değildi.** Açılış sahnesi koddan
kuruluyor ve düğmeler `onClick.AddListener(acilis.Basla)` ile
bağlanmıştı. Bu çağrı bir **çalışma zamanı** dinleyicisi ekler: kurulum
anında düğme çalışır, ama sahne kaydedilirken dinleyici serileştirilmez.
Sahne bir daha açıldığında dördü de boşa basar.

Ölçüm: gönderilen `Acilis.unity` dosyasında kalıcı çağrı sayısı
**sıfırdı** (`git show HEAD:… | grep -c "m_MethodName"` → 0). Düzeltmeden
sonra **9**.

**2. Klavye — seçili hiçbir nesne yoktu.** `Submit` eylemi EventSystem'in
seçili nesnesine gider; seçili nesne yoksa hiçbir yere gitmez.
`firstSelectedGameObject` hiç atanmamıştı. Yani tıklama çalışsaydı bile
"tuşlara basmak" yine bir işe yaramayacaktı.

### Neden 382 test bunu yakalamadı

Çünkü hiçbiri **düğmeye basmıyordu**. Menüyü "doğrularken" panelleri
koddan çağırmıştım (`m.KredileriAc()`) ve ekran görüntüsüne bakmıştım —
bu, tıklama yolunun hiçbir adımına dokunmuyor: ne dinleyiciye, ne
EventSystem'e, ne seçime.

Bu, bu projede üçüncü kez aynı biçim: *ölçtüğün şey değil, ölçme biçimin
bozuktu.* Ağaçlarda sayaç "3.175 ağaç çizildi" derken her ağacın yaprağı
eksikti; gövde sayımında test kendi artığını sayıyordu; burada da
"menü çalışıyor" diyen gözlem menünün çalışmayan yarısını hiç geçmiyordu.

### Şimdi ne tutuyor

- `AcilisMenusuTests` (EditMode, 6 test) — sahneyi **diskten** açar ve
  her düğmenin kalıcı dinleyicisini, hedefini ve metodunun var olduğunu
  sınar; `firstSelectedGameObject`u ve raycast yolunu ölçer.
- `AcilisTiklamaTests` (PlayMode, 4 test) — olayı **EventSystem
  üzerinden** gönderir, yani oyuncunun bastığı yolun kendisini yürütür.

Kurucu artık `UnityEventTools.AddPersistentListener` kullanıyor ve
`AcilisMenusu` seçim düşerse geri koyuyor (fareyle boşluğa tıklamak da
seçimi düşürür).
