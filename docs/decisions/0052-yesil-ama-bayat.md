# ADR 0052 — Yeşil ama bayat: derleme hatası test koşumunu susturuyor

- **Tarih**: 2026-08-27
- **Durum**: Kabul
- **Bağlam**: Faz 3, Beyazıt turu. Bulgu tek bir yapıyı değil **bütün
  test disiplinini** ilgilendiriyor.

## Ne oldu

`LandmarkTests`e dört test eklendi. Aynı düzenlemede bir **CS0102** vardı
(`dome_windows` alanı ikinci kez bildirilmişti) ve test assembly'si
**derlenmedi**.

Koşum yine de **223/223 YEŞİL** döndü.

Unity, derleme başarısız olunca bir önceki **sağlam** assembly'yi
çalıştırdı. Yani sonuç doğruydu — ama **yanlış sürümün** sonucuydu.

## Bu, bildiğim hatanın daha kötüsü

Faz 3 boyunca üç kez "atlanan test geçen test gibi görünür" dedim
(ADR 0041, 0043, 0044). Orada eksik test **görünmüyordu**: toplam sayı
beklediğimden azdı ve bakarsam görürdüm.

Burada sayı bile yalan söylemiyor — **223 gerçekten koştu ve geçti**.
Yalan olan, o 223'ün *hangi kodun* 223'ü olduğu.

Fark ettim çünkü sayının artmamasına takıldım ve konsola baktım. Bir
sonraki sefer bakmayabilirim.

## Karar — bekçi, koruduğu hatanın içinde de çalışmalı

`ProjectConventionsTests.CompiledTestCountMatchesTheSource`:

* diskteki `Tests/EditMode/*.cs` dosyalarında `[Test]` satırlarını sayar
  (**güncel**),
* assembly'deki `[Test]` niteliğini yansımayla sayar (**derlenen**),
* ikisi tutmazsa patlar.

İşe yaramasının sebebi ince ve önemli: **bu test eski assembly'de de
vardır.** Derleme başarısız olduğunda o eski nüsha koşar, diski okur ve
kendi bayat assembly'sini sayar — ve tutmadığını görür. Yani bekçi,
korumak istediği durumun **içinde** çalışıyor.

Kendi kendini denetleyemeyen bir bekçi yazsaydım (örneğin yeni bir teste
"ben koştum mu" diye sordursaydım) derleme çöktüğünde o da koşmazdı ve
hiçbir şey olmazdı.

## Yöntem kuralı

**Yeşil bir koşum, derleme yeşilse yeşildir.** Test sonucuna bakmadan
önce konsolda hata olup olmadığına bakılır; sayı beklenenden azsa
sonuç değil **derleme** sorgulanır.

## Sonuç

- `CompiledTestCountMatchesTheSource` eklendi; kaynak 227 = assembly 227.
- EditMode **228/228** (227 Hezarfen + 1 paket testi), atlanan yok.
