# docs/feedback/ — Caner'in İnceleme Notları

Varlık başına bir dosya: `<varlık>.md` (ör. `ottoman_house.md`, `galata_tower.md`).

**Döngü (plan Bölüm 4):**
1. Claude `renders/review/<varlık>_vN/` altında inceleme paketi üretir (4-açı + yakın plan + referans kolajı).
2. Caner serbest metinle not verir — "cumba %20 daha derin", "külah daha sivri", "ışık daha sıcak".
3. Claude notu buraya **tarih + sürüm** ile loglar, uygular, `vN+1` paketini üretir.
4. Onay: **"OK vN"** → varlık o sürümde kilitlenir. Sonraki değişiklik yeni sürüm açar.

**Dosya şablonu:**

```markdown
# <varlık>

## v1 — 2026-08-17
**Paket:** renders/review/<varlık>_v1/
**Caner notu:** (buraya birebir yapıştırılır)
**Uygulanan:** (Claude'un yaptığı değişiklikler)

## v2 — ...
```
