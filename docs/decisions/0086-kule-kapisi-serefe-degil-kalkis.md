# ADR 0086 — Kule kapısı bir şerefeye değil, kalkışa açılır

- Tarih: 2026-09-02
- Durum: kabul (ölçümle)
- Bağlam: son kullanıcı 4. turu

## Sorun

Dört tur boyunca oyuncu kuleye çıkamadı ve her turda sebep bir katman
derinleşti:

| tur | teşhis | yaptığım |
|---|---|---|
| 1 | kapı yok | `KuleKapisi` eklendi |
| 2 | kapı **taşın 1,7 m içinde** | çarpıştırıcı sınırından hesaplandı |
| 3 | iniş noktası **külahın 1,2 m üstünde** | çarpıştırıcı kulenin kademesini izledi |
| 4 | iniş noktası **kapalı bir fıçının içinde** | — |

Dördüncü turda oyuncu şerefede beş saniye durabildi ve **inemedi**:
kurtaran kapak (36,20 m'deki disk) ile hapseden duvar (7,875 m yarıçaplı
tüp) aynı düzeltmede, art arda iki satırda yazılmıştı.

## Asıl bulgu: bu kulede gezilebilir bir şerefe yok

Modelin kendi ölçüleri (`tower_kit.py`):

| parça | yarıçap | kot |
|---|---:|---:|
| kâgir gövde | 8,225 m | 0 → 34,50 |
| mazgallı korkuluk | 8,225 m | 34,50 → 36,20 |
| ahşap kasnak | 7,875 m | 36,20 → 37,50 |
| külah, **saçaklı** | 9,175 → 0 | 37,50 → 46,00 |

Korkuluk ile kasnak arasında **0,35 m** kalıyor; oyuncunun kapsülü
0,70 m. Ve saçak (9,175 m) korkuluğun üstünü örtüyor, yani korkuluğa
yaklaşan bir başın gireceği yer külahın altı.

1632'de balkon yok — bu zaten kayıtlı ve doğru (1831 sofası ve demir
korkuluk sonradan). **Yanlış olan, olmayan bir balkonu var saymaktı.**
Üç turdur ürettiğim tuzakların ortak sebebi bu.

## Karar

Kapı bir seyir terasına açılmaz; **kalkışa** açılır. Kuleye çıkmak,
kâgir gövdenin içinden yukarı çıkıp tepedeki açıklıktan **adım atmaktır**
— külahın altında durulacak yer olmadığı için gerçekte de böyle olurdu.

Uygulama:
- Kapı, kanat kuşanılmamışsa çalışmaz ve sebebini söyler. Kanatsız
  çıkmak, 46 m'lik hasarsız bir düşüşten başka bir şey değildi.
- Oyuncu korkuluğun **dışına**, açık havaya bırakılır ve aynı karede
  uçuşa geçer. Ne düşüş, ne fıçı, ne de bir zemin varsayımı.
- Kapının önündeki nokta artık çarpıştırıcının **yerel** sınırından
  hesaplanır: dünya hizalı kutu, 205° dönük bir kule için 16,45 m'lik
  çapı 21,8 m sanıyordu ve kapıyı 3,9 m havada bırakıyordu.

## Reddedilen seçenek

**Kasnağı içeri çekip gerçek bir gezinti yolu açmak.** Yapılabilirdi
(mazgalın arkasında yürünecek 2 m) ama şehrin en tanınan silueti
değişirdi ve saçağın korkuluğu örtmesi modelin kendi kaydında duran bir
biçim kararı. Bir oynanış kolaylığı için landmark'ın görünüşünü
değiştirmek, bu projenin tarihsel iddiasını ucuza satmak olurdu.

## Ölçüt

`KuleKapisiTests` artık kapıyı değil **çıkışı** ölçüyor: kapıdan sonra
oyuncu (a) uçuyor mu, (b) kulenin çarpıştırıcısının dışında mı. Önceki
dört test kapının var olduğunu ölçüyordu ve dördü de yeşilken oyuncu
odada kilitliydi.
