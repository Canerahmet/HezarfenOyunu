"""
Hezarfen: 1632 — Prosedürel doku üretiminin ortak parçaları.

İki üretici (`gen_foliage_texture.py`, `gen_lead_texture.py`) aynı üç şeye
ihtiyaç duyuyor: renk uzayı dönüşümü, yükseklikten normal, ve Poly Haven'ın
dosya düzeniyle **birebir aynı** çıktı yazmak. Üçü de burada tek nüsha.

Çıktının Poly Haven düzenini taklit etmesi bir tercih değil, **şart**:
`materials.py` ve `build_unity_maps.py` dokuları `meta.json` üzerinden okur ve
kaynağın nereden geldiğini bilmez. Düzen aynı olduğu sürece prosedürel doku
hattın hiçbir yerinde özel durum gerektirmez — yalnızca kök klasörü farklıdır
(`art/textures/generated/`).

## Telif

Bu dosyanın ürettiği hiçbir görüntüde üçüncü taraf verisi yoktur; girdi
yalnızca tohumlanmış sayı üretecidir. CLAUDE.md'nin "lisansı belgelenmemiş
görsel indirme" kuralını **dolanmaz, kaldırır**.
"""

import json
import os

import numpy as np
from PIL import Image

OUT_ROOT = os.path.join("art", "textures", "generated")


# ------------------------------------------------------------- renk uzayı

def srgb_to_linear(c):
    """0-255 sRGB üçlüsü → doğrusal 0-1. Renk KARIŞIMI doğrusalda yapılır."""
    c = np.asarray(c, dtype=np.float64) / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def linear_to_srgb(c):
    c = np.clip(c, 0.0, 1.0)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)


# ------------------------------------------------------------------ alanlar

def normal_from_height(h, strength):
    """
    Yükseklik → normal (OpenGL Y+, Poly Haven'ın `nor_gl` sözleşmesi).

    `h` 0-1'e normalize edilmiş kabul edilir; `strength` fiziksel eğimi verir:
    kabartı genliği metre cinsinden `a` ise `strength = a · çözünürlük / doku_boyu`.
    Gözle ayarlanmaz — yanlış değer yüzeyi ya düz ya da kabarcıklı yapar ve
    ikisinin de sebebi render'a bakınca anlaşılmaz.
    """
    gx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * 0.5
    gy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * 0.5
    nx, ny, nz = -gx * strength, -gy * strength, np.ones_like(h)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.stack([nx / ln, ny / ln, nz / ln], axis=-1) * 0.5 + 0.5


def blob_field(res, count, radius, stretch, rng, exponent=1.6,
               radius_jitter=(0.55, 1.35), weight=(0.55, 1.0), window=False,
               mode="sum"):
    """
    Örtüşen elips öbeklerinin toplamı — **döşenebilir** (toroidal mesafe).

    Her öbek kendi yönünde uzatılır; hepsi aynı yöne baksaydı doku taranmış
    gibi çizgilenir ve tekrar hemen görünürdü. Yaprak kütlesi de kurşunun
    oksit lekesi de aynı matematikten çıkar, yalnızca ölçek farklıdır.

    `window=True` öbeği yalnızca **destek kutusunda** hesaplar. Öbek yarıçapı
    dışında değer zaten tam sıfırdır (`clip(1-d², 0, 1)`), yani sonuç aynıdır;
    değişen tek şey maliyettir: öbek başına tüm kare yerine küçük bir pencere.
    9 000 ot sapını tam karede toplamak 9 milyar işlem ediyordu ve üretim
    10 dakikada bitmedi; pencereyle aynı iş 30 milyon işlem.

    Varsayılan `False` bilinçlidir: mevcut çağıranların (yaprak, kurşun)
    çıktısı **bit bit aynı** kalsın diye. Kayan noktada "matematiksel olarak
    aynı" ile "aynı" farklı şeylerdir ve onaylanmış bir dokuyu sessizce
    değiştirmek istemiyorum.

    ## `mode`: kütle mi, ayrık nesne mi

    `"sum"` örtüşen öbekleri **toplar** — yaprak kütlesi, oksit lekesi, ot
    tutamı gibi birbirine karışan şeyler için doğrudur.

    `"max"` her öbeğin kendi biçimini korur. Çakıl taşı ve ot sapı ayrık
    nesnelerdir: toplandıklarında birbirlerinin içinde eriyip düzgün bir
    alana dönüşürler. Ölçüldü — 4 200 çakıl toplandığında yakın kare
    "zımpara kâğıdı" oluyordu, taşların hiçbiri seçilmiyordu. Örtüşme oranı
    yükseldikçe toplam her zaman ortalamaya yakınsar; ayrık nesne isteyen
    yerde `max` gerekir.
    """
    if mode not in ("sum", "max"):
        raise ValueError(f"bilinmeyen kip: {mode}")
    h = np.zeros((res, res), dtype=np.float64)
    if not window:
        yy, xx = np.mgrid[0:res, 0:res].astype(np.float64)

    for _ in range(count):
        cx, cy = rng.uniform(0, res, 2)
        ang = rng.uniform(0, np.pi)
        r = radius * rng.uniform(*radius_jitter)
        st = stretch * rng.uniform(0.8, 1.25)
        w = rng.uniform(*weight)
        ca, sa = np.cos(ang), np.sin(ang)

        half = int(np.ceil(r * max(st, 1.0))) + 1
        if not window or 2 * half + 1 >= res:
            dx = (xx - cx + res * 0.5) % res - res * 0.5
            dy = (yy - cy + res * 0.5) % res - res * 0.5
            u = (dx * ca + dy * sa) / (r * st)
            v = (-dx * sa + dy * ca) / r
            blob = np.clip(1.0 - (u * u + v * v), 0.0, 1.0) ** exponent * w
            h = h + blob if mode == "sum" else np.maximum(h, blob)
            continue

        bx, by = int(np.floor(cx)), int(np.floor(cy))
        k = np.arange(-half, half + 1)
        dx = (bx + k - cx)[None, :]
        dy = (by + k - cy)[:, None]
        u = (dx * ca + dy * sa) / (r * st)
        v = (-dx * sa + dy * ca) / r
        blob = np.clip(1.0 - (u * u + v * v), 0.0, 1.0) ** exponent * w
        idx = np.ix_((by + k) % res, (bx + k) % res)
        if mode == "sum":
            np.add.at(h, idx, blob)
        else:
            h[idx] = np.maximum(h[idx], blob)

    return normalize(h)


def normalize(a):
    a = a - a.min()
    m = a.max()
    return a / m if m > 1e-9 else a


def srgb_to_lab(rgb255):
    """0-255 sRGB → CIELAB (D65). Renkler arası FARKI ölçmek için."""
    c = srgb_to_linear(rgb255)
    m = np.array([[0.4124, 0.3576, 0.1805],
                  [0.2126, 0.7152, 0.0722],
                  [0.0193, 0.1192, 0.9505]])
    xyz = m @ np.asarray(c, dtype=np.float64)
    xyz = xyz / np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16.0 / 116.0)
    return np.array([116 * f[1] - 16, 500 * (f[0] - f[1]), 200 * (f[1] - f[2])])


def delta_e(a, b):
    """İki 0-255 sRGB rengi arasındaki CIE76 farkı. ~12 üstü 'apayrı renk'."""
    return float(np.linalg.norm(srgb_to_lab(a) - srgb_to_lab(b)))


def lab_image(rgb_u8):
    """(h, w, 3) 0-255 sRGB görüntü → (h, w, 3) CIELAB."""
    c = srgb_to_linear(np.asarray(rgb_u8, dtype=np.float64))
    m = np.array([[0.4124, 0.3576, 0.1805],
                  [0.2126, 0.7152, 0.0722],
                  [0.0193, 0.1192, 0.9505]])
    xyz = c @ m.T / np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16.0 / 116.0)
    return np.stack([116 * f[..., 1] - 16,
                     500 * (f[..., 0] - f[..., 1]),
                     200 * (f[..., 1] - f[..., 2])], axis=-1)


def even_tufts(res, count, radius, rng, jitter=0.95, falloff=1.0):
    """
    **Eşit aralıklı** yumuşak tutamlar (0-1) — kümelenmeyen serpme.

    `blob_field` noktaları rastgele serper ve rastgelelik kendi başına
    kümelenir (Poisson): yer yer sık, yer yer seyrek. O yoğunluk
    dalgalanması dokunun **makro** bandına düşer ve karo döşendiğinde
    tekrarı ele verir. Ölçüldü: maki tutamları böyle serpildiğinde makro
    ΔE 1,53 (eşik 1,0); eşit aralıklı kurulduğunda düşer.

    Kümelenmesi *istenen* şeyler (yaprak kütlesi, oksit lekesi) için
    `blob_field` doğru araçtır; eşit dağılması gereken şeyler için bu.
    """
    f1, _ = worley(res, count, rng, jitter=jitter)
    return np.clip(1.0 - f1 / max(radius, 1e-6), 0.0, 1.0) ** falloff


def stretch(a, std=0.18):
    """
    Alanı 0,5 çevresinde **verilen standart sapmaya** getirir.

    `normalize` yalnızca uçları 0 ve 1'e oturtur; bir-iki aykırı piksel varsa
    kütlenin tamamı 0,5 çevresinde sıkışık kalır. `fine_grain` tam olarak
    böyledir — bir kez kutu bulanıklığı varyansı düşürür — ve normalize
    edilmiş hâli renge karıştırıldığında ölçülen katkı ±%5'te kalıyordu:
    dokunun yakın ayrıntısı yoktu. Genlik gözle değil std ile kurulur.
    """
    s = float(a.std())
    if s < 1e-9:
        return np.full_like(a, 0.5)
    return np.clip(0.5 + (a - a.mean()) * (std / s), 0.0, 1.0)


def fine_grain(res, rng, passes=3):
    """
    Ucuz, döşenebilir ince tane: rastgele alanın komşu ortalamasıyla
    yumuşatılması. `np.roll` zaten sarmalı olduğu için karo kenarında dikiş
    doğmaz — öbek toplamı kadar pahalı değildir ve burada gereken de bu.

    `passes` tane boyunu belirler: 1 kum, 3 metal dövme izi, 5 toprak tozu.
    """
    g = rng.random((res, res))
    for _ in range(passes):
        g = (g + np.roll(g, 1, 0) + np.roll(g, -1, 0)
             + np.roll(g, 1, 1) + np.roll(g, -1, 1)) / 5.0
    return normalize(g)


def worley(res, count, rng, jitter=0.95):
    """
    Döşenebilir hücre gürültüsü → (F1, F2), **piksel** cinsinden mesafe.

    `blob_field` yumuşak kütleler verir; kayanın ve kurumuş çamurun istediği
    şey ise **kırık**tır: keskin, dallanan, her yerde farklı genişlikte. İkisi
    farklı matematiktir ve öbek toplamıyla kırık taklit edilemez — denendi,
    "kabarcıklı beton" çıktı.

    Kırık çizgisi F2 − F1 ≈ 0 olan yerdir (iki hücreye eşit uzaklık); plaka
    içi ise F1'in büyüdüğü yer.

    ## Neden serpiştirilmiş nokta değil, SARSILMIŞ IZGARA

    Noktaları rastgele serpip her nokta için tüm kareyi taramak `count` ile
    doğrusal büyür: 1 600 nokta = 1 600 tam kare taraması, dakikalar. Izgaraya
    hücre başına bir nokta konup her pikselin yalnızca **3×3 komşu hücresine**
    bakılması ise `count`tan bağımsızdır — 9 tarama, hep 9.

    Yan etkisi istenen yöndedir: saf rastgele serpme çok eşitsiz hücreler
    verir (bazıları dev, bazıları iğne); kaya plakası ve çamur çatlağı böyle
    değildir, boyları birbirine yakındır. `jitter` bu düzenliliği ayarlar;
    1,0'da hücre içinde tam serbest. 3×3 komşuluk `jitter ≤ 1` için yeterlidir.
    """
    n = max(1, int(round(np.sqrt(count))))
    cell = res / n

    # pts[j, i] = (x, y) — (i, j) hücresindeki noktanın konumu
    gi, gj = np.meshgrid(np.arange(n), np.arange(n))
    pts = np.stack([gi, gj], axis=-1) + 0.5 + jitter * (rng.random((n, n, 2)) - 0.5)
    pts *= cell

    yy, xx = np.mgrid[0:res, 0:res].astype(np.float64)
    ci = np.minimum((xx / cell).astype(np.int64), n - 1)
    cj = np.minimum((yy / cell).astype(np.int64), n - 1)

    big = float(res) * 2.0
    f1 = np.full((res, res), big)
    f2 = np.full((res, res), big)

    for dj in (-1, 0, 1):
        for di in (-1, 0, 1):
            ai, aj = ci + di, cj + dj
            wi, wj = ai % n, aj % n
            # Sarma acilir: komsu hucre karenin disindaysa nokta da disarida
            # dusunulur, yoksa mesafe karsi kenardan olculurdu.
            px = pts[wj, wi, 0] + (ai - wi) * cell
            py = pts[wj, wi, 1] + (aj - wj) * cell
            d = np.hypot(xx - px, yy - py)
            nearer = d < f1
            f2 = np.where(nearer, f1, np.minimum(f2, d))
            f1 = np.where(nearer, d, f1)

    return f1, f2


# -------------------------------------------------------------------- yazım

def _u8(a):
    return (np.clip(a, 0.0, 1.0) * 255.0).round().astype(np.uint8)


def write_texture_set(tex_id, res, size_m, bc, nrm, arm, meta_extra,
                      rough=None, ao=None, out_root=OUT_ROOT):
    """
    Poly Haven düzeninde bir doku klasörü yazar: `T_<id>_<HARITA>.png` + meta.

    `rough` / `ao` ayrıca tek kanallı yazılır çünkü **Blender tarafı ARM
    okumaz**: `materials.py` `R` ve `AO` anahtarlarını arar, `build_unity_maps.py`
    ise `ARM`ı. İkisini birden yazmak inceleme render'ıyla oyun içi görüntüyü
    aynı yüzeyden konuşturur; yalnızca ARM yazmak Blender'ı Principled'ın
    varsayılan 0,5 pürüzlülüğüne bırakırdı ve fark sessiz olurdu.
    """
    d = os.path.join(out_root, tex_id)
    os.makedirs(d, exist_ok=True)

    arrays = [("BC", bc), ("N", nrm), ("ARM", arm)]
    if rough is not None:
        arrays.append(("R", np.repeat(_u8(rough)[..., None], 3, axis=-1)))
    if ao is not None:
        arrays.append(("AO", np.repeat(_u8(ao)[..., None], 3, axis=-1)))

    maps = {}
    for key, arr in arrays:
        fn = f"T_{tex_id}_{key}.png"
        Image.fromarray(arr).save(os.path.join(d, fn))
        maps[key] = fn

    meta = dict(
        polyhaven_id=None,
        name=tex_id,
        resolution=f"{res}px",
        size_meters=[float(size_m), float(size_m)],
        authors={"Hezarfen projesi (prosedürel üretim)": "All"},
        license="Kendi eserimiz — üçüncü taraf hakkı yok",
        source="prosedürel; girdi yok",
        maps=maps,
        normal_convention="OpenGL (Y+) — Poly Haven nor_gl ile ayni",
    )
    meta.update(meta_extra)
    with open(os.path.join(d, "meta.json"), "w", encoding="utf-8") as fh:
        json.dump(meta, fh, ensure_ascii=False, indent=1)
    return d
