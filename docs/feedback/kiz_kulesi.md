# Kız Kulesi — inceleme notu

İnceleme paketi: `renders/review/KizKulesi_v3/contact_sheet.png`
Karar kaydı: ADR 0035. Araştırma: RESEARCH.md §5.3.

## Bakarken bilmen gereken tek şey

**Bu, bildiğin Kız Kulesi değil — ve olmaması gerekiyor.** Kâgir gövde,
camlı köşk ve kurşun kubbe **1725**'tir. Kule 1720'de yandı; Damat İbrahim
Paşa yerine kâgir fener kulesini yaptırdı. 1632'de kule **ahşaptır** ve
**fener değil karakoldur**: yatsıdan sonra ve seher vakti mehter çalar.
Tepesindeki korkuluklu düzlük fener yeri değil, **nöbet sahanlığı**.

Kubbe yok, fener yok, camlı köşk yok, zincir yok. Hepsi bilinçli.

## Ölçüler

| | |
|---|---|
| Su üstünde yükseklik | 20,00 m (1725 kâgir kulesi ~23 m — altında kalmalı) |
| Kayalık | 26,0 × 20,0 m, su altına −2,50 m'ye iner |
| Ahşap gövde | 9,0 × 9,0 m, 2 kat |
| Nöbet sahanlığı çıkması | 1,40 m, altında payanda sırası |
| LOD0 / LOD1 | 799 üçgen / var |
| Doğruluk | **D3** (tipolojik), `status: draft` |

Ölçülü çizim **yok** — 1632 kulesi yanmıştır. Sayılar uydurulmadı, tipolojik
olarak kuruldu ve tek sayısal kısıt teste bağlandı (1725 kulesinden alçak).

## Bu turda ölçülerek düzeltilenler

1. **Gövde aşı kırmızısı çıkmıştı.** Kod yorumum "boyasız ahşap" diyordu,
   malzeme boyalıydı (`trim` = ASI_DARK %70). Boyasız kereste için ayrı bir
   rol açıldı; ölçü doygunluk: boyasız kroma 5,4, aşı ailesi 11–28.
   R/G 1,51 → **1,08**.
2. **Sonra gövde fazla karardı** (kaya oranı 0,43 → 0,30). Değer geri
   kaldırıldı → **0,47**.
3. **Sahanlık okunmuyordu** — çıkma gövdenin %10'uydu. 1,40 m'ye çıkarıldı,
   payanda eklendi, korkuluk dolu levhadan direk+kuşağa çevrildi.
4. **Kayalık kum yığını gibiydi** (tek koni). Üç kütleli obeğe çevrildi.

## Sana sorduğum

1. Ahşap gövdenin **tonu** doğru mu? Tuzlu havada yıllarca durmuş boyasız
   kereste gri-kahve olur; kırmızıyı bilinçli olarak çıkardım.
2. Kule **fazla mı sade**? 1632 için elimde tek bir görsel kaynak yok;
   sadeliği bilgisizliğin değil kaynağın sınırı olarak kabul ettim.
3. **Silüetteki payı** yeterli mi? Uçuş hattının üstünde ve tek başına
   duruyor; 20 m alçak gelirse söyle — ama yükseltmek 1725'e kaymak olur.

## Bilerek eksik bıraktıklarım

- Kayalık dokusu hâlâ moloz duvar dokusu; kayalık için ayrı doku yok.
- Kule ile Salacak arasındaki ~100 m su, Faz 5 işi.

---

**Onay**: _(bekliyor — "OK vN" yaz)_
