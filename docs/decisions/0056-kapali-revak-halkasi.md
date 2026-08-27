# ADR 0056 — Kapalı revak halkası: göz − sütun = 4

- **Durum:** kabul (ölçüldü, iki yapıda bağımsız doğrulandı)
- **Tarih:** 2026-08-27
- **Bağlam:** Faz 3 ayrıntı geçişi, avlu revakları

## Sorun

Sultanahmet'in avlu revakı için TDV **"yirmi altı sütun, otuz kubbeli
birim"** der. İki sayı farklıydı ve ilk okuyuşta biri yanlış görünüyordu:
bir revak sırasında `n` göz `n+1` sütunla taşınır, yani sütun sayısı göz
sayısından **fazla** olmalıydı — az değil.

Ayrıca kaynak **hangi kenarda kaç göz** olduğunu söylemiyor. İlk
kurulumda o dağılımı elle tahmin ettim (10/10/5/5) ve ön göz **13 m**
genişliğinde çıktı; bir revak gözü değil, bir salon.

## Bulgu

Kaynak çelişmiyordu. **Kapalı** bir dikdörtgen revakta her göz sınırı iki
gözle paylaşılır, dolayısıyla:

```
mesnet sayısı = göz sayısı          (kapalı halka)
sütun sayısı  = göz sayısı − 4      (dört köşe, sütunla değil
                                     KÖŞE AYAĞIYLA taşınır)
```

30 göz → 30 mesnet → 4'ü köşe ayağı → **26 sütun**. İki sayı aynı
geometriyi tarif ediyormuş.

**Bağımsız doğrulama:** Fâtih'in avlusu 1471'den ayaktadır ve iki sayısı
da ölçülmüştür — **22 kubbe, 18 sütun**. Fark yine tam dört. Bir yapıda
bulunan okuma, ikincisinde sınandığı için artık bir kural.

| yapı | göz | sütun | fark | kaynak |
|---|---:|---:|---:|---|
| Sultanahmet | 30 | 26 | 4 | TDV (ikisi de) |
| Fâtih Camii | 22 | 18 | 4 | ayakta, ölçülmüş |
| Beyazıt | 24 | (20) | 4 | kubbe kaynaktan; **sütun türetilmiş** |

Beyazıt'ın sütun sayısı kaynakta yok. Kural 20 öngörüyor ama bu
**türetilmiş** bir değerdir; ölçülmüş gibi davranmıyorum ve bu yüzden
Beyazıt'a Fâtih/Sultanahmet'teki gibi bir mesh denetimi yazmadım.

## Karar

1. `detay_kit.revak_sirasi(..., ends=(False, False))` sıranın uçlarındaki
   sütunu basmaz; köşelere `detay_kit.kose_ayagi` konur. Böylece sayım
   **geometride** durur, katalogda değil.
2. Göz dağılımı elle yazılmaz: `detay_kit.gozleri_dagit(toplam, kenarlar)`
   sayılan toplamı kenar uzunlukları oranında, en büyük artık yöntemiyle
   dağıtır. Varsayım gerektirmeyen tek dağılım budur — gözler birbirine
   eşit olur.
   - Sultanahmet 30 → 7/7/8/8 (gözler 7,9 ve 8,2 m)
   - Fâtih 22 → 6/6/5/5 (elle yazılmış eski dağılımın **aynısı** —
     yöntem kendini doğruladı)
   - Beyazıt 24 → 6/6/6/6
3. Üreteçler sayımı meshten okuyup doğrular; tutmazsa `ValueError`.
4. Unity tarafında `ClosedArcadeRingHasFourCornerPiers` her iki yapıyı da
   katalogdan denetler.

## Sonuç

İki sayının çeliştiği yerde üçüncü bir ölçüm aramak yerine, sayıların
**hangi geometriyi** tarif ettiğini sorduk. Çelişki, kapalı halka
varsayımı eklenince kayboldu.

Ders: *iki sayı çelişiyor gibiyse, önce ikisinin aynı şeyi sayıp
saymadığına bak.*

İlgili: [ADR 0044](0044-suleymaniye-avlu-minareleri.md),
[ADR 0057](0057-ayrinti-gecisi.md)
