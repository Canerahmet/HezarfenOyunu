# ADR 0068 — Karakter yükseltme yolu: MakeHuman / Mixamo

**Durum:** **Ertelendi** — Caner'in isteğiyle, tüm fazlar bittikten sonra
bakılacak (2026-08-28)
**Bağlam:** Faz 5 sonrası, Faz 6 NPC'leri de etkiler

---

## Caner'in notu

> *"karakterler ile alakali daha gercekci ve iyi olmasi icin makehuman,
> mixamo da kullanilabilir. oyle bir dusuncem var. tum fazlar bitince
> tekrar bakariz."*

Bu ADR kararı **vermiyor**; kararı verecek bilgiyi şimdi topluyor, çünkü
o gün geldiğinde "acaba lisansı neydi" diye başlamak istemem.

## Şu an ne var

| parça | kaynak | durum |
|---|---|---|
| Taban gövde | Blender Studio Human Base Meshes v1.4.1, **CC0** | 1,70 m, 21 160 üçgen, UV'li |
| Kıyafet | gövdeden türetilen kabuklar, Rålamb'dan okunan oranlar | T2, taslak |
| Rig | **22 kemikli Unity Humanoid** (ADR 0066) | avatar geçerli |
| Animasyon | 13 klip, scriptle üretilmiş, ayak kayması ölçülü | 0,4–1,8 cm |

## MakeHuman

**Ne verir:** parametrik insan gövdesi — yaş, kilo, boy, ırk, cinsiyet
kaydırıcıları. Faz 6'nın NPC çeşitliliği için doğrudan işe yarar: tek
tabandan yüzlerce farklı gövde.

**Lisans:** uygulama AGPL ama **çıktı varlıkları CC0** — MakeHuman ekibi
bunu bilerek böyle yaptı, ticari kullanım serbest. (Kullanılırsa
`refs/LICENSES.md`'ye satır girer, tıpkı CC0 taban gövdede olduğu gibi.)

**Bizim yapımıza etkisi:** `kiyafet_kit` giysiyi **gövdeden türetiyor**,
yani taban değişince kıyafet kendini yeniden kurar — elden modellenmiş
bir entari olsaydı baştan yapılırdı. `rig_kit` de eklemleri gövdeden
ölçüyor. Yani taban değişimi bu hattın **tasarlandığı** durumdur, bir
kırılma değil.

**Bedeli:** kurulum + indirme (Caner'in işi), ve karakterin "portresi
yok" beyanı korunmalı — MakeHuman'ın hazır yüzlerinden biri Hezarfen'in
yüzü olamaz, çünkü öyle bir yüz yok.

## Mixamo

**Ne verir:** mocap tabanlı hazır animasyon kütüphanesi. Benim
ürettiğim klipler prosedüreldir; ölçüsü doğru (ayak kayması 0,4–1,8 cm)
ama **mocap'in ağırlığı, dengesi, ivmesi yok**. Bu fark bir oyunda
görülür.

**Lisans:** Adobe hesabı gerektirir (ücretsiz); indirilen karakter ve
animasyonlar telifsiz, ticari projede kullanılabilir. **Adobe hesabı =
Caner'in işi**, benim değil.

**Bizim yapımıza etkisi — ve ADR 0066'nın karşılığı:** Mixamo klipleri
Unity Humanoid'e retarget edilir. Rig'i **Rigify yerine doğrudan Unity
Humanoid** kurmuş olmamız tam da bunu ücretsiz kılıyor: Mixamo klibi
avatara takılır, ara katman yok. Rigify'la gitseydik iki isim uzayı
arasında çeviri gerekirdi.

**Bedeli ve dikkat edilecek şey:** Mixamo'da **süzülüş, kanat kuşanma,
kuleden atlayış YOK.** Kütüphane genel hareket (yürüme, koşma, tırmanma,
düşme) için zengin; bu oyunun kendine ait dört-beş klibi yine elde
üretilir. Yani Mixamo bir **değiştirme** değil, **karışım** olur ve
karışımın riski üslup farkıdır: mocap yürüyüşün yanında prosedürel
süzülüş cansız kalabilir.

## Öneri (o gün için)

1. **MakeHuman: evet, ama Faz 6'da** — asıl karşılığı NPC çeşitliliğinde.
   Hezarfen'in kendi gövdesi için CC0 taban zaten yeterli.
2. **Mixamo: locomotion için evet, uçuş için hayır.** Yürüme/koşma/
   tırmanma/düşme Mixamo'dan gelsin; kuşanma, kalkış, süzülüş, iniş,
   çakılma bende kalsın — çünkü onların referansı yok ve olmayan bir
   şeyin mocap'i de yok.
3. Her iki durumda da **ayak kayması ölçümü kalır**: dışarıdan gelen bir
   klibin doğru olduğunu varsaymak, kendi klibimin doğru olduğunu
   varsaymaktan daha güvenli değil.

## Bu ADR'nin kendisi bir hatırlatma

Karar günü geldiğinde bakılacak yer burası. O gün ölçülecek şey şu:
**Mixamo klibi bu rig'e takıldığında ayak kayması kaç santimetre?**
Cevap 5 cm'in altındaysa mesele üslup tercihidir; üstündeyse retarget
sorunu vardır ve önce o çözülür.
