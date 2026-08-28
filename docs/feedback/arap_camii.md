# Arap Camii (San Domenico) — inceleme

**Paket:** `renders/review/ArapCamii_v1/`
**Üretim:** `tools/blender/gen_church_kit.py` → varyant `ArapCamii`
**Kademe:** **T1** — tipoloji değil, adı olan bir yapı
**İlgili:** ADR 0071 (Cuma nasıl görünür), ADR 0018 (kilise ve sinagog)

## Neden üretildi

Estetik bir istek değil, bir **ölçümün sonucu**. Cuma namazı mahalle
mescidinde kılınmaz; Galata'nın Cuma camisi yoktu; dolayısıyla oyuncunun
fiilen dolaştığı semtte Cuma **hiçbir şey yapmıyordu**. Test bunu sayıyla
söyledi, sonra yapı üretildi.

## Ölçüler — kaynakla karşılaştırma

RESEARCH.md §4.2(a), T1 (Koç Ü. İstanbul Surları; Mitler, *The Genoese in
Galata 1453–1682*):

> San Domenico (bugün Arap Camii, ~1323–37): **üç nefli** bazilika,
> **40 × 15 m**, moloz taş ve tuğla almaşık örgü, **sivri kemerli
> pencereler**, **ahşap çatı**; orta nef yan neflerden yüksek; **kare
> planlı çan kulesi** (sonradan minareye çevrilen kule budur).

| Ölçü | Kaynak | Model | |
|---|---|---|---|
| Gövde eni | 15 m | **15.00 m** | ✅ |
| Gövde boyu | 40 m | **40.00 m** | ✅ |
| Nef sayısı | 3 | 3 | ✅ |
| Orta nef yükseltisi | var | 11.5 vs 7.0 m | ✅ |
| Kule planı | kare | kare, 4.20 m | ✅ |
| Pencere | sivri kemer | sivri kemer | ✅ |
| Çatı | ahşap | ahşap makas + kiremit | ✅ |

Ayak izi renderda **19.85 × 44.75 m** yazıyor: aradaki fark kule (batı
köşesinde, gövdenin dışında) ve apsis (doğu ucunda). Kaynağın 40 × 15'i
**bazilika gövdesidir** ve model o gövdeyi tam tutturuyor.

LOD0 10 036 üçgen, LOD1 36.

## Minare

Kule **yeni bir yapı değil**: çan kulesinin kendisi. Dönüşüm haçı
indirmek, şerefe eklemek, külahı kurşunlamaktır. İstanbul'un en tanınır
siluetlerinden biri tam bu yüzden — bir İtalyan çan kulesi, minare
olarak.

Şerefe **ölçüldü**, gözle onaylanmadı: kule 4.20 × 4.20 m, şerefe
5.30 × 5.30 m → her yüzde **0.55 m** balkon. (Renderda küçük görünüyor;
küçük olması doğru.)

Yükseklikler (kule 22 m gövde, toplam 24.79 m) ve pencere ritmi
**rekonstrüksiyondur** — kaynak plan ölçüsü verir, yükseklik vermez.

## AÇIK MADDE — yapının yönü

Sahnede **226.97°** duruyor ve bu sayı **araziden**, kaynaktan değil:
katalogda `face_deg` yok.

Yapı bir kilisedir ve **kıbleye dönük değildir** — Ayasofya kuralı burada
da işler (ADR 0045), yani onu kıbleye çevirmek yanlış olurdu. Ama eğimden
gelen açı da bir belge değil. Gerçek eksen ölçülebilir (yapı ayakta) ama
elimde yazılı bir kaynak yok, o yüzden **uydurulmadı**.

Bu bir **Faz 7 görsel cila maddesidir**; Cuma'nın çalışması için gereken
şey konumdur ve konum katalogdadır (~100 m yaklaşıklıkla).

## Onay

Caner: *(bekliyor — onay akışı tüm fazlardan sonra, oyun oynanırken)*
