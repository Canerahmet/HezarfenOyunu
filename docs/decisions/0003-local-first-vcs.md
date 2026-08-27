# ADR 0003 — Sürüm Kontrolü: Lokal Önce, Bulut Sonra

**Tarih:** 2026-08-17
**Durum:** ~~Kabul edildi~~ → **YERİNE GEÇİLDİ** (2026-08-27, [ADR 0059](0059-git-gecisi.md))

> Bu karar amacına ulaştı: Unity projesi kurulmadan `.gitignore` hazırdı, > `Library/` hiç depoya girmedi. Faz 3'ün sonunda git'e geçildi. Aşağıdaki > "aktarım anı için hazır komut dizisi" **kullanılmadı** — `git add .` ile tek > commit yerine, her katmanı kendi gerekçesiyle anlatan 18 commit'lik bir dizi > tercih edildi ve ikili varlık politikası ölçüme dayandırıldı (ADR 0059).
**Karar veren:** Caner

## Bağlam
Plan (Görev 1) repo iskeletiyle birlikte Git + Git LFS kurulumunu öngörüyordu. Caner lokal
çalışmayı, bulut aktarımını sonraya bırakmayı tercih etti.

## Karar
Şimdilik `git init` YOK, uzak depo YOK. Klasör yapısı, `.gitignore` ve `.gitattributes` (LFS
kuralları dahil) şimdiden hazır tutulur; aktarım anında `git init` + `git lfs install` +
ilk commit tek adımda yapılır.

## Sonuçlar

**Kabul edilen bedel:**
- Geri alma ağı yok — hatalı bir toplu değişiklikte "önceki hale dön" imkânı yok.
- İnceleme paketi sürümleri (`_v1`, `_v2`) commit yerine klasör adıyla izlenir. Zaten plan böyle
  öngörüyor (Bölüm 4, Geri Bildirim Protokolü), yani bu kayıp küçük.
- Sürüm kilitleri (ADR 0001) commit yerine dosyada yaşar — disipline bağlı.

**Azaltma:**
- Faz sınırlarında Caner klasörün tam kopyasını (`Hezarfen_Oyunu_yedek_<tarih>`) alır. Bu, git
  yerine geçmez ama tek katastrofik hatayı karşılar.
- **Unity projesi oluşturulmadan önce git'e geçilmesi güçlü tavsiyedir:** Unity'nin ürettiği
  `Library/` klasörü on binlerce dosyadır ve `.gitignore`suz ilk commit acı verir. `.gitignore`
  zaten hazır olduğu için bu maliyetsizdir.
- Kural gevşemesi YOK: "sadece sohbette var olan varlık yasak" (CLAUDE.md) aynen geçerli — her
  kalıcı çıktı dosyaya yazılır.

## Aktarım anı için hazır komut dizisi
```powershell
cd d:\ClaudeCodeProjects\Hezarfen_Oyunu
git init
git lfs install
git add .gitattributes .gitignore
git commit -m "Add version control config"
git add .
git commit -m "Initial project skeleton"
# sonra: git remote add origin <url>; git push -u origin main
```
