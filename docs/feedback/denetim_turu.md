# Denetim turu — Faz 0'dan bugüne

**Tarih:** 2026-08-24 · **İsteyen:** Caner — *"ara sıra değişiklikler yaptık,
bu bazı kısımları değiştirmiş veya bozmuş olabilir. Faz 2 bittikten sonra en
baştan geldiğimiz noktaya kadar olan kısımları kontrol edelim."*

Haklıydın. **Beş gerçek kusur** çıktı, dördü sessizdi — hiçbiri hata vermiyor,
hiçbiri gözle görünmüyordu. Beşi de düzeltildi ve testle bağlandı.

---

## Bulunan ve düzeltilen

### 1. İki mahalle birbirinin kaldırımını siliyordu

Galata ve Balat, ürettikleri mesh'i **aynı dosyaya** yazıyordu
(`SM_Kaldirim.asset`, `SM_Kaideler.asset`). Balat kurulunca Galata'nın kaldırımı
ve bütün taş kaideleri siliniyor, yerlerine **2 km ötedeki** Balat'ın geometrisi
geçiyordu.

Ölçüldü: Galata sahnesindeki kaldırım mesh'inin merkezi **x = −1976** idi —
yani Galata sokağı kaldırımsız ve kaidesizdi. Sahne bozuk *görünmüyordu*; eksik
olan şey sessizce başka bir yerde duruyordu.

Varlık yolu artık semte göre (`SM_Kaldirim_Galata`, `_Balat`). İki sahne de
yeniden kuruldu, sahipsiz kalan iki eski dosya silindi (referansları
denetlendi: sıfır). Test: `GeneratedMeshesBelongToThisQuarter`.

### 2. Kaldırımın yürünen yüzü tersti

`SM_Kaldirim`'in 698 yatay üçgeninin **697'si aşağı** bakıyordu. Kıyas taşı aynı
sahnedeydi: kaide mesh'inde 166 yukarı, **0 aşağı**.

Üç sonucu vardı: yüzey üstten **ışıksız/siyah** okunuyordu, Unity ışın sorguları
arka yüzü görmediği için **çarpıcı fiilen yoktu** (oyuncu kaldırımdan düşerdi)
ve sokak **çimen** görünüyordu.

ADR 0016 turundan beri duruyordu ve hiçbir kare göstermedi — çünkü o turun
bütün kareleri kaldırımın **altından** alınmıştı, ve alttan bakınca yüzey doğru
görünür. Test: `PavementWalkingSurfaceFacesUp`.

### 3. Kubbeli caminin kubbesi malzemesizdi

`PF_Cami_Kubbe` **20 Ağustos**'ta üretilmiş; kurşun malzemesi (`M_Lead_Sheet`)
**21 Ağustos**'ta, ADR 0021 turunda eklendi. Arada kalan varlık hiç yeniden
üretilmedi: kubbesinin malzeme yuvası **boş**tu, yani sahnede kullanılsa
macenta çıkardı. Katalogdaki kaynak notu da eskiydi (kubbeli camiye "ahşap
çatılı mahalle mescidi" diyordu).

Boru hattından geçirildi. Geometri **bit bit aynı** çıktı (13,53×15,20 m, 2308
üçgen) — yani arada başka bir şey kaymamış; kayıp yalnız malzemeydi.

### 4. Faz 0 graybox'ı da malzemesizdi

`PF_BoxHouse` (17 Ağustos, boru hattının ilk doğrulama varlığı) 6 boş malzeme
yuvası taşıyordu. Üretici zaten doğru adları yazıyor; FBX o adlar eklenmeden
önceki hâldeydi. Yeniden üretildi.

**Şimdi: 86 prefabın 704 malzeme yuvasında BOŞ olan sıfır.**

---

## Temiz çıkanlar

| Ne | Sonuç |
|---|---|
| Unity EditMode | **147/147** geçti (turdan önce 142) |
| Unity PlayMode | **9/9** geçti |
| Blender öz-testi | hepsi geçti |
| GIS georeferansı | 7 noktanın hepsi toleransta (`dem_probe`) |
| `_Import` | boş (yalnız `Materials`) |
| 9 katalog → prefab | eksik yok |
| 86 prefab | HistoricalTag ✅ LODGroup ✅ collider ✅ — **eksiği olan yok** |
| ADR numaraları | 0001–0031, boşluk yok, tekrar yok |
| `refs/` telifi | altında **hiç görsel yok** — kısıt korunmuş |
| Faz 1 arazi sahnesi | bozulmamış (15,3 km, 4 katman, 42 857 ağaç, GIS 10 katman) |
| Elle üretilen mesh sayısı | Unity'de yalnız 2 (kaldırım + kaide); ikisi de denetlendi |

---

## Kararlar — ikisi de kapandı (2026-08-24)

**~~Karar 13~~ — build listesi.** Kayıtlı tek sahne `Sandbox/OutdoorsScene.unity`
idi: HDRP şablonunun boş örnek sahnesi. **(a)** uygulandı — liste artık koddan
geliyor (`BuildScenes`), açılış `Faz1_Terrain`, ikinci `FlightSlice`. Semt
sahneleri **bilerek yok**: Addressables ile yükleniyorlar (ADR 0011) ve build
listesine de konursa Unity onları iki kez paketler. Dört test kilitliyor.

**~~Karar 12~~ — mahallenin zemini.** **(a)** uygulandı: yerleştirici, koyduğu
yapıların dairelerinden bir yerleşim maskesi yazıyor ve arazi örtüsünü o
bölgede yeniden boyuyor. Sınır iddiası değil — kaynağı sahnenin kendisi.

Bu tur **beşinci bir kusur** daha çıkardı: `alphamapResolution` atamak (aynı
değeri atasan bile) bütün splatmap'i siliyor. Kısmi boyama eklenince bütün
İstanbul toprağa düştü; kuşbakışı kare makul görünüyordu, yakalayan şey örtü
testleri oldu (ot %0,02, kaya %0, kıyı %0). Ayrıntı: **ADR 0032**.

---

## Kayda geçen, karar istemeyen

- **Kanonik `.blend` dosyaları iki yerde.** Yeni kitler `art/blend/<kit>/`
  altına yazıyor; `SM_Mescit_A.blend`, `SM_Cami_Kubbe.blend`, `SM_BoxHouse.blend`,
  `SM_House_*.blend` ise `art/blend/` kökünde. Kural (ADR 0005) ikisini de
  yasaklamıyor ama tek yer olmalı. Toplama işi; dosya taşımak kanonik yolları
  değiştirir, ayrı bir turda yapılmalı.
- **`renders/review/` altında 38 varlık ailesi var**, 48 prefabın kendi paketi
  yok — bunlar varyantlar (`_B`, `_C`, ev varyantları). İnceleme disiplini aile
  başına tek paket; kasıtlı.
- **Faz 1'in FPS kabulü hâlâ doğrulanmadı** — oyuncu yapısı gerekiyor,
  `-executeMethod` bu makinede lisans hatasıyla düşüyor (SETUP.md).
- **13 inceleme notu imzasız.** Hiçbiri "OK vN" almadı.
