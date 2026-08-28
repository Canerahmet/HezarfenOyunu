# ADR 0066 — Rig: Rigify değil, doğrudan Unity Humanoid iskeleti

**Durum:** Uygulandı — **Caner'in onayına açık** (planla farklılık)
**Tarih:** 2026-08-28
**Bağlam:** Faz 5, rig turu

---

## Plan ne diyor

Plan Bölüm 10: *"Rig/animasyon: Blender **Rigify** → Unity Humanoid
retarget."*

## Neden saptım

Rigify'ın verdiği şey **etkileşimli animasyon için IK/FK kontrol
iskeletidir**: yüzden fazla kemik, kontrol kolları, kutup hedefleri,
tweak kemikleri. Bunların hepsi bir insanın Blender'da fare ile poz
vermesi içindir.

Bu projede animasyonu **ben scriptle üretiyorum.** Kontrol kolları hiç
kullanılmayacak. Geriye kalan iş — deform kemiklerini Unity Humanoid
adlarına eşlemek — Rigify'ın adlandırmasıyla (`DEF-upper_arm.L`)
fazladan bir çeviri katmanı demek: her animasyon turunda, her hata
ayıklamada, iki isim uzayı arasında gidip gelmek.

Onun yerine **Unity Humanoid'in tam istediği iskelet** doğrudan
kuruluyor: 22 kemik, Unity adlarıyla (`Hips`, `LeftLowerArm`, …),
hiyerarşisi Humanoid'in beklediği gibi.

## İki seçenek

**A. Rigify (planın dediği).**
- ✅ Blender'da elle poz vermek kolay (IK).
- ✅ Plan değişmez.
- ❌ ~100+ kemik, beşte dördü hiç kullanılmayacak.
- ❌ Ad eşleme katmanı — hata ayıklamada iki isim uzayı.
- ❌ Headless üretimde Rigify eklentisini etkinleştirip metarig'i gövdeye
  oturtmak ek kırılganlık.

**B. Doğrudan Humanoid iskeleti (yapılan). — ÖNERİM**
- ✅ 22 kemik, hepsi kullanılıyor.
- ✅ Eşleme katmanı yok; Unity avatarı ilk denemede geçerli kurdu.
- ✅ Her kemiğin yeri **ölçülmüş** bir sayı ve testle bağlı.
- ❌ Blender'da elle poz vermek zorlaşır (IK yok).
- ❌ Plandan sapma.

**Kaybın büyüklüğü:** yalnızca *elle* poz vermek zorlaşır. Gerekirse
Rigify sonradan bu iskelete takılabilir — tersi (Rigify'dan temiz
iskelete inmek) daha zordur.

## Eklem yerleri ölçülür, tablodan alınmaz

Antropometrik oran tabloları bir gövde için **ortalamadır**, elimdeki
gövde için doğru değildir. Kit uzuvların merkez çizgisini ağı
dilimleyerek çıkarır ve eklemleri o çizgi üzerinde **yay uzunluğuna
göre** bulur.

Dizinle almak (`nokta[int(t*n)]`) yanlış olurdu: dilimler eşit kot
aralıklı ama kol A-pozunda yana açıldığı için eşit **uzunlukta** değil.

## Bu turun bulduğu üç yanlış

1. **Kol çizgisi ayakları da kola sayıyordu.** Filtre "gövde ekseninden
   uzak noktalar"dı; parmak uçlarının 55 cm altında ayaklar da o filtreye
   uyuyordu. Çizgi omuzdan ayağa iniyor, %82'sine yürüyünce bilek **ayak
   bileği hizasında** çıkıyordu: dirsek %42,7, bilek %14,6. Çözüm ek bir
   sayı değil, **boşlukta durmak** oldu — parmak ucu ile ayak arasındaki
   boşluk gerçek bir yapısal işarettir.
2. **Omuz eklemi ölçülemez.** Koltuk altının üstünde kol ile gövde tek
   parçadır; dilimleyerek ayrılmaz. Kalçada ölçülen tek eşik (0,256 m)
   omuz hizasında kolu hiç yakalamıyordu ve üst kol eklemi 12 cm aşağı
   düşüyordu. Omuz artık omuz **genişliğinden** türetiliyor; ölçülebilen
   ilk nokta koltuk altıdır.
3. **Boyun ve baş "gövde" filtresiyle ölçülüyordu** ve o filtre trapez
   kasını da sayıyordu: dilim boyun değil omuz platosuydu. Boyun 2,5 cm
   önde, kafatası 5,9 cm arkada — 15 cm kotta 8,4 cm'lik bir kırık.
   Boyun **dar bir sütundur** ve ancak dar bir filtre görür.

## Sonuç ve doğrulama

- Unity avatarı **2/2 geçerli**, 22 kemik, 2 skinned mesh.
- Omurga zincirinin ön-arka sapması **1,5 cm** (eşik 3 cm).
- Dirsek %63,7, diz %29,4 — ikisi de insan aralığında.
- `KarakterTests` dört yeni test: kemik tamlığı, eklem sırası ve kotu,
  omurga pürüzsüzlüğü, sol/sağ simetri.

**LOD merdiveni de bu turda tek kaynağa alındı.** Karakter üçüncü şahıs
kamerasında sürekli 3–5 m ötededir; küçük nesne merdiveni ilk kademeyi
~20 m'de düşürüp oyuncuya kendi karakterinin basitleştiğini gösterirdi.
Karakter merdiveni ters yönde ayarlı: ilk kademe **geç** düşer (~7 m),
kül eşiği **erken** gelir (~156 m). Ayrım bir isim listesine değil
**yapıya** dayanır — deri bağlı renderer taşıyan grup karakterdir.

Bunu yazarken test ile boru hattının **aynı sayıyı iki yerde**
tuttuğu ortaya çıktı; test artık `ImportLanding.Merdiven()`'i okuyor.
*Bir sayının iki sahibi varsa er ya da geç iki değeri olur.*

## Caner'e soru

Rigify'ı istiyor musun? Şu an gerek yok gibi görünüyor ve iskelet
çalışıyor. Ama Blender'da elle poz vermek isteyeceksen (örneğin bir
sahne için özel bir duruş) Rigify'ı bu iskeletin üstüne takarım —
o zaman iş kaybı olmaz, ek katman olur.

İlgili: ADR 0065 (karakter), ADR 0063 (LOD merdiveni).
