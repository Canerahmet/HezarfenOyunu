"""
Hezarfen: 1632 — Semt (bölge yayını) tanımları — plan Faz 1 madde 3 + madde 6.

`refs/maps/districts.geojson` üretir.

## Bunlar TARİHSEL mahalle sınırları DEĞİLDİR

Bu ayrımı en başa koyuyorum çünkü dosyanın adı yanıltıcı olabilir.

1632 İstanbul'unun mahalleleri **kadastral değildi**: 1546 ve 1600 tarihli Vakıf
Tahrir Defterleri mahalle *adlarını* ve vakıf kayıtlarını verir, sınır çizgisi
vermez (RESEARCH.md §"Mahalleler"). Mahalle bir alan değil, bir mescit çevresinde
toplanmış hane topluluğu ve bir kefalet birimiydi. Dolayısıyla "1632 mahalle
sınırları" diye bir metrik veri YOKTUR ve üretilemez — üretmek uydurmak olurdu
(CLAUDE.md: *"Kaynak niteliksel olduğunda metrik geometri UYDURMA"*).

Bu dosyadaki poligonlar bunun yerine **oyun bölgeleridir**: plan Faz 1 madde 6'nın
istediği yayın (streaming) hücreleri. Tarihsel iddia taşımazlar, o yüzden hepsi
`tier: Graybox` + `historical_claim: none` ile işaretlidir. Adları gerçek
semtlerden gelir çünkü içerikleri oralıdır; sınırları oynanış kararıdır.

## Ölçülen şeyler (iddia değil, denetim)

* Her bölge, adını taşıdığı landmark'ları **içermek zorundadır** — `requires`
  listesi `landmarks_1632.geojson`e karşı doğrulanır. Poligon kayarsa üretim durur.
* **Uçuş koridoru kapsaması:** Galata Kulesi → Doğancılar doğrusu 100 m'de bir
  örneklenir; her örnek en az bir öncelik-1 bölgenin içinde olmalıdır. Oyunun
  omurgası olan uçuşta yüklü olmayan bir hücre kalamaz.
* Her bölgenin DEM'den ölçülen **kara alanı** (ha) raporlanır. Faz 4'ün yerleştirici
  bütçesi buna dayanacak: "az semt, dolu semt" ancak alanı bilinen semtle kurulur.

Kullanım:
    tools/gis/.venv/Scripts/python.exe tools/gis/districts_build.py --dir data/gis/istanbul
"""

import argparse
import json
import math
import os
import sys

import numpy as np

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

from geodesy import (from_utm35n as _from_utm35n,  # noqa: E402
                     to_utm35n as _to_utm35n)


# rasterio.warp.transform yerine gecen kabuk — imza AYNI, gerekce geodesy.py
# basliginda: bu makinede rasterio'nun DLL'i Windows uygulama denetimince
# bloklu ve butun GIS araclarini birden calistirilamaz kiliyordu.
# Yalnizca WGS84 <-> UTM 35N (EPSG:32635) yonu desteklenir; bu boru hattinin
# kullandigi tek donusum odur ve baska bir CRS istenirse SESSIZCE yanlis
# sonuc vermek yerine yukselir.
def warp_transform(src_crs, dst_crs, xs, ys):
    wgs, utm = "EPSG:4326", "EPSG:32635"
    if (src_crs, dst_crs) == (wgs, utm):
        pairs = [_to_utm35n(x, y) for x, y in zip(xs, ys)]
    elif (src_crs, dst_crs) == (utm, wgs):
        pairs = [_from_utm35n(x, y) for x, y in zip(xs, ys)]
    else:
        raise ValueError(f"desteklenmeyen donusum: {src_crs} -> {dst_crs}")
    return [p[0] for p in pairs], [p[1] for p in pairs]

from coastline_build import load_dem, utm_to_grid   # noqa: E402
from walls_build import point_in_ring, ring_area, sample_utm, to_utm   # noqa: E402

# priority 1 = ucus ekseni, dikey dilim icin ZORUNLU (plan Bolum 0, direk 5)
# priority 2 = suricinin geri kalani ve Halic
# priority 3 = "sonra" (plan Faz 1 madde 6: "sonra Eyup")
DISTRICTS = [
    dict(
        id="D_Galata", name="Galata", priority=1, kind="land",
        summary="Kule, Galata surlari, Arap Camii, Tophane, Kasimpasa tersanesi. "
                "Ucusun KALKIS bolgesi.",
        requires=["LM_GalataKulesi", "LM_GalataSurlari", "LM_ArapCamii",
                  "LM_Tophane", "LM_Tersane"],
        ring=[(28.9540, 41.0200), (28.9800, 41.0180), (28.9900, 41.0270),
              (28.9860, 41.0360), (28.9600, 41.0400), (28.9520, 41.0300)],
    ),
    dict(
        id="D_Okmeydani", name="Okmeydanı", priority=1, kind="land",
        summary="Acik talim alani, namazgah (minaresiz), menzil taslari. "
                "Ucus talimlerinin gectigi yer (plan Bolum 8 S-kademe).",
        requires=["LM_Okmeydani"],
        # Bati kenari 28.940'ta durur: daha genisi Eyup'un uzerine biner ve
        # oncelik-1 oldugu icin Eyup'un karasini sessizce sahiplenir.
        ring=[(28.9400, 41.0460), (28.9700, 41.0450), (28.9780, 41.0570),
              (28.9600, 41.0680), (28.9400, 41.0620)],
    ),
    dict(
        id="D_Surici_Dogu", name="Suriçi — Doğu", priority=1, kind="land",
        summary="Sarayburnu, Topkapi siluetı, Ayasofya, Sultanahmet, Beyazit, "
                "Suleymaniye, Eminonu ve Yeni Cami harabesi.",
        requires=["LM_Ayasofya", "LM_Sultanahmet", "LM_TopkapiSiluet",
                  "LM_YeniCamiHarabe", "LM_Beyazit", "LM_Suleymaniye",
                  "LM_EskiSaray", "LM_RustemPasa", "LM_IncliKosk"],
        ring=[(28.9600, 40.9995), (28.9880, 41.0010), (28.9920, 41.0110),
              (28.9880, 41.0175), (28.9700, 41.0190), (28.9600, 41.0225)],
    ),
    dict(
        id="D_Surici_Bati", name="Suriçi — Batı", priority=2, kind="land",
        summary="Fatih Camii (ozgun sema), Sehzade, Yavuz Selim, kara surlari, "
                "Yedikule. Suricinin daha seyrek, bostanli batisi.",
        requires=["LM_FatihCamii", "LM_Sehzade", "LM_YavuzSelim", "LM_Yedikule"],
        ring=[(28.9180, 40.9890), (28.9600, 40.9995), (28.9600, 41.0225),
              (28.9450, 41.0400), (28.9330, 41.0300), (28.9190, 41.0000)],
    ),
    dict(
        id="D_Uskudar", name="Üsküdar", priority=1, kind="land",
        summary="Mihrimah (Iskele) Camii, Dogancilar Meydani, Kiz Kulesi. "
                "Ucusun INIS bolgesi.",
        requires=["LM_UskudarMihrimah", "LM_Dogancilar", "LM_KizKulesi"],
        # 1632 Uskudar'i iskeleye yakin toplu bir kasabadir; ic sirtlar bostan
        # ve mezarliktir. Poligonu genis tutmak "az semt, dolu semt" direktine
        # (plan Bolum 0.5) aykiri bir doldurma borcu yaratir.
        ring=[(28.9980, 41.0120), (29.0300, 41.0130), (29.0330, 41.0330),
              (29.0100, 41.0400), (28.9980, 41.0260)],
    ),
    dict(
        id="D_Bogaz", name="Boğaz", priority=1, kind="water",
        summary="Galata/Sarayburnu ile Uskudar arasindaki bogaz suyu. Ucusun "
                "gectigi ve termiklerin/lodosun okundugu yer; kayik agi.",
        requires=[],
        # Su seridi: bati yakasi (Sarayburnu->Tophane->Kabatas) yukari,
        # dogu yakasi (Uskudar->Kuzguncuk) asagi. Genis tutulmaz — genis su
        # poligonu iki yakanin karasini da yutar ve Faz 4 alan butcesini sisirir.
        ring=[(28.9880, 41.0110), (29.0040, 41.0170), (29.0110, 41.0250),
              (29.0200, 41.0420), (29.0230, 41.0530), (29.0100, 41.0520),
              (29.0000, 41.0420), (28.9890, 41.0350), (28.9830, 41.0230)],
    ),
    dict(
        id="D_Halic", name="Haliç", priority=2, kind="water",
        summary="Halic suyu ve iki yakanin iskeleleri. 1632'de KOPRU YOK — tum "
                "gecis kayikladir (RESEARCH.md; plan Bolum 11.1).",
        requires=[],
        # Guney (yarimada) yakasi bati yonunde, sonra kuzey (Kasimpasa) yakasi
        # geri. Iki yaka da kendi `shoreline_1632` cizgimizden okundu.
        ring=[(28.9880, 41.0150), (28.9700, 41.0185), (28.9610, 41.0230),
              (28.9576, 41.0282), (28.9500, 41.0325), (28.9460, 41.0392),
              (28.9420, 41.0450), (28.9450, 41.0470), (28.9520, 41.0400),
              (28.9560, 41.0345), (28.9620, 41.0325), (28.9668, 41.0292),
              (28.9682, 41.0252), (28.9770, 41.0212)],
    ),
    dict(
        id="D_Eyup", name="Eyüp", priority=3, kind="land",
        summary="Eyup Sultan ve cevresi. Plan Faz 1 madde 6: 'sonra Eyup' — "
                "dikey dilimde iceriksiz siluet olarak kalir.",
        requires=[],
        ring=[(28.9200, 41.0450), (28.9420, 41.0480), (28.9450, 41.0620),
              (28.9250, 41.0700), (28.9130, 41.0580)],
    ),
]

# Ucus koridoru: oyunun omurgasi. Bu hat boyunca yuklu olmayan hucre KALAMAZ.
CORRIDOR = [("LM_GalataKulesi", "LM_Dogancilar")]
CORRIDOR_STEP_M = 100.0

# Yukleme olcutu bolgenin KENARINA uzakliktir, merkezine degil.
#
# Merkez+yaricap kolay ama uzun bolgelerde yanlistir: D_Halic ince ve bukuk bir
# su seridi; merkezine gore yaricapi ~2,9 km cikar ve Halic'in bir ucundayken
# obur ucu da yuklu tutar. Kenar uzakligi bicimden bagimsizdir.
#
# Histerezis SART: tek esik kullanilirsa oyuncu sinirda gidip gelirken sahne
# surekli yuklenip bosalir ("thrash") ve "yukleme ekransiz gecis" vaadi coker.
LOAD_MARGIN_M = 700.0
UNLOAD_FACTOR = 1.30


def log(msg):
    print(f"[HZ] {msg}", flush=True)


GRID_STEP_M = 40.0


def ring_mask(ring_utm, X, Y):
    """Vektorize nokta-poligon testi (crossing number). X, Y ayni bicimde dizi."""
    inside = np.zeros(X.shape, dtype=bool)
    n = len(ring_utm)
    for i in range(n):
        x0, y0 = ring_utm[i]
        x1, y1 = ring_utm[(i + 1) % n]
        if y0 == y1:
            continue
        straddles = (y0 > Y) != (y1 > Y)
        with np.errstate(invalid="ignore"):
            xc = x0 + (Y - y0) * (x1 - x0) / (y1 - y0)
        inside ^= straddles & (X < xc)
    return inside


def build_grid(meta, heights):
    """
    Tum dunyayi kapsayan TEK ornekleme izgarasi.

    Bolge basina ayri izgara kurmak alanlari dogru verirdi ama CAKISMAYI
    goremezdi: su bolgeleri kara bolgeleriyle bilerek ortusur ve her biri kendi
    icindeki karayi sayarsa Faz 4'un yerlestirme butcesi ayni araziyi iki kez
    sayar. Ortak izgara, "bu hucrenin sahibi kim" sorusunu sorulabilir kilar.
    """
    minx, miny, maxx, maxy = meta["bounds_utm"]
    gx = np.arange(minx, maxx, GRID_STEP_M)
    gy = np.arange(miny, maxy, GRID_STEP_M)
    X, Y = np.meshgrid(gx, gy)

    # DEM'i dogrudan orneklemek yerine en yakin hucreyi al: 40 m adim zaten
    # DEM'in 7,5 m hucresinden kaba, bilineer ara deger bir sey kazandirmaz.
    n = meta["resolution"]
    sx = (maxx - minx) / (n - 1)
    sy = (maxy - miny) / (n - 1)
    ix = np.clip(((X - minx) / sx).astype(int), 0, n - 1)
    iy = np.clip(((Y - miny) / sy).astype(int), 0, n - 1)
    land = heights[iy, ix] > 0.5
    return X, Y, land


def main():
    p = argparse.ArgumentParser(description="1632 bolge yayini semtleri")
    p.add_argument("--dir", default="data/gis/istanbul")
    p.add_argument("--geojson", default="refs/maps/districts.geojson")
    p.add_argument("--landmarks", default="refs/maps/landmarks_1632.geojson")
    args = p.parse_args()

    meta, heights = load_dem(args.dir)
    gx = meta["world_origin"]["utm_easting"]
    gz = meta["world_origin"]["utm_northing"]

    with open(os.path.abspath(args.landmarks), encoding="utf-8") as fh:
        lm_col = json.load(fh)
    lm_xy = {}
    for f in lm_col["features"]:
        pr = f["properties"]
        lm_xy[pr["id"]] = to_utm([tuple(f["geometry"]["coordinates"])])[0]
    log(f"{len(lm_xy)} landmark okundu (dogrulama referansi)")

    X, Y, land_grid = build_grid(meta, heights)
    cell_ha = (GRID_STEP_M * GRID_STEP_M) / 10000.0
    log(f"olcum izgarasi {X.shape[1]}x{X.shape[0]} @ {GRID_STEP_M:.0f} m")

    errors = []
    built = []

    for d in DISTRICTS:
        ring = to_utm(d["ring"])
        cx = sum(p[0] for p in ring) / len(ring)
        cy = sum(p[1] for p in ring) / len(ring)
        radius = max(math.dist((cx, cy), p) for p in ring)

        # --- denetim 1: adini tasidigi landmark'lari ICERMELI ---
        missing = []
        for lid in d["requires"]:
            if lid not in lm_xy:
                errors.append(f"{d['id']}: {lid} landmark katalogunda YOK")
                continue
            if not point_in_ring(lm_xy[lid], ring):
                missing.append(lid)
        if missing:
            errors.append(f"{d['id']} ({d['name']}) su landmark'lari ICERMIYOR: "
                          f"{', '.join(missing)} — poligon yanlis yerde.")

        mask = ring_mask(ring, X, Y)
        built.append(dict(
            d=d, ring=ring, cx=cx, cy=cy, radius=radius, mask=mask,
            total_ha=int(mask.sum()) * cell_ha,
            land_ha=int((mask & land_grid).sum()) * cell_ha,
        ))

    # --- cakismasiz kara butcesi -----------------------------------------
    # Bir kara hucresinin SAHIBI, onu iceren kara bolgeleri arasinda onceligi
    # en yuksek (sayisi en kucuk) olandir. Su bolgeleri kara sahiplenmez —
    # kiyi seridine tastiklari icin sayarlarsa ayni araziyi iki kez sayarlar.
    owner = np.full(X.shape, -1, dtype=np.int16)
    order = sorted(range(len(built)),
                   key=lambda i: (built[i]["d"]["priority"], i))
    for i in order:
        if built[i]["d"]["kind"] != "land":
            continue
        claim = built[i]["mask"] & land_grid & (owner < 0)
        owner[claim] = i
    for i, b in enumerate(built):
        b["exclusive_land_ha"] = int((owner == i).sum()) * cell_ha
        d = b["d"]
        dup = b["land_ha"] - b["exclusive_land_ha"]
        log(f"{d['id']:16} P{d['priority']} {d['kind']:5} "
            f"alan {b['total_ha']:6.0f} ha / kara {b['land_ha']:6.0f} ha "
            f"({100 * b['land_ha'] / max(b['total_ha'], 1e-9):3.0f}%) / "
            f"tekil kara {b['exclusive_land_ha']:6.0f} ha"
            + (f"  [{dup:.0f} ha baska bolgeye ait]" if dup > 1.0 else "")
            + f"  yaricap {b['radius']:.0f} m")

    unowned = int((land_grid & (owner < 0)).sum()) * cell_ha
    log(f"hicbir bolgeye ait olmayan kara: {unowned:.0f} ha "
        f"(dunya kutusunun tamami kaplanmiyor — beklenen)")

    # --- denetim 2: ucus koridoru kapsanmali ---
    p1 = [b for b in built if b["d"]["priority"] == 1]
    for a_id, b_id in CORRIDOR:
        if a_id not in lm_xy or b_id not in lm_xy:
            errors.append(f"koridor {a_id}->{b_id}: landmark eksik")
            continue
        a, b = lm_xy[a_id], lm_xy[b_id]
        dist = math.dist(a, b)
        steps = max(1, int(dist / CORRIDOR_STEP_M))
        gaps = []
        for s in range(steps + 1):
            t = s / steps
            pt = (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)
            if not any(point_in_ring(pt, x["ring"]) for x in p1):
                gaps.append(round(t * dist))
        if gaps:
            errors.append(f"ucus koridoru {a_id}->{b_id}: {len(gaps)}/{steps + 1} "
                          f"ornek hicbir oncelik-1 bolgede degil "
                          f"(ilk bosluk {gaps[0]} m, son {gaps[-1]} m)")
        else:
            log(f"koridor {a_id}->{b_id}: {dist:.0f} m, {steps + 1} ornegin "
                f"tamami oncelik-1 kapsamasinda")

    if errors:
        for e in errors:
            log(f"HATA {e}")
        raise SystemExit(f"[HZ] {len(errors)} denetim hatasi. Semt dosyasi YAZILMADI.")
    log("denetim OK")

    # --- cikti ---
    features, local_features = [], []
    for b in built:
        d, ring = b["d"], b["ring"]
        lon, lat = warp_transform("EPSG:32635", "EPSG:4326",
                                  [p[0] for p in ring], [p[1] for p in ring])
        coords = [[round(a, 6), round(c, 6)] for a, c in zip(lon, lat)]
        load_d = round(LOAD_MARGIN_M, 1)
        unload_d = round(LOAD_MARGIN_M * UNLOAD_FACTOR, 1)

        features.append({
            "type": "Feature",
            "geometry": {"type": "Polygon", "coordinates": [coords + [coords[0]]]},
            "properties": {
                "layer": "district", "id": d["id"], "name": d["name"],
                "priority": d["priority"], "kind": d["kind"],
                "summary": d["summary"],
                "requires_landmarks": d["requires"],
                "tier": "Graybox",
                "historical_claim": "none — bu bir OYUN bölgesidir, mahalle sınırı değildir",
                "scene_address": f"Districts/{d['id']}",
                "center_unity": {"x": round(b["cx"] - gx, 1), "z": round(b["cy"] - gz, 1)},
                "radius_m": round(b["radius"], 1),
                "load_distance_m": load_d,
                "unload_distance_m": unload_d,
                "distance_measured_to": "polygon edge (0 if inside), NOT centre",
                "area_ha": round(b["total_ha"], 1),
                "land_area_ha": round(b["land_ha"], 1),
                "exclusive_land_area_ha": round(b["exclusive_land_ha"], 1),
                "status": "draft",
            },
        })
        # Unity TEK dosya okur. GeoJsonImporter tanimadigi alanlari yok sayar;
        # DistrictImporter ise asagidaki sayisal alanlari okur. Ikinci bir
        # dosya acmak, projeksiyon mantiginin tek yerde yasamasi kuralini
        # (ADR 0007/0008) bozmadan da olsa iki kaynagi senkron tutma borcu yaratirdi.
        local_features.append({
            "layer": "district", "id": d["id"], "name": d["name"],
            "tier": "Graybox", "action": f"P{d['priority']}/{d['kind']}",
            "note": (f"{d['summary']} | tekil kara {b['exclusive_land_ha']:.0f} ha | "
                     f"yukle {load_d:.0f} m / bosalt {unload_d:.0f} m (kenara uzaklik) | "
                     f"OYUN bolgesi — tarihsel mahalle siniri DEGIL"),
            "closed": True,
            "priority": d["priority"],
            "kind": d["kind"],
            "scene_address": f"Districts/{d['id']}",
            "load_distance_m": load_d,
            "unload_distance_m": unload_d,
            "center_x": round(b["cx"] - gx, 2),
            "center_z": round(b["cy"] - gz, 2),
            "radius_m": round(b["radius"], 2),
            "land_ha": round(b["land_ha"], 1),
            "exclusive_land_ha": round(b["exclusive_land_ha"], 1),
            "rings": [[{"x": round(px - gx, 2), "z": round(py - gz, 2)} for px, py in ring]],
        })

    total_land = sum(b["exclusive_land_ha"] for b in built)
    p1_land = sum(b["exclusive_land_ha"] for b in built if b["d"]["priority"] == 1)
    log(f"CAKISMASIZ kara toplami {total_land:.0f} ha; oncelik-1 {p1_land:.0f} ha "
        f"(dikey dilimin icerik butcesi budur)")

    collection = {
        "type": "FeatureCollection",
        "name": "districts",
        "metadata": {
            "title": "Bölge yayını (streaming) semtleri — 1632 İstanbul",
            "status": "TASLAK — sınırlar oynanış kararıdır, Caner onayı bekliyor",
            "NOT_historical": (
                "Bu poligonlar TARİHSEL MAHALLE SINIRI DEĞİLDİR. 1632 mahalleleri "
                "kadastral değildi; 1546/1600 Vakıf Tahrir Defterleri mahalle ADLARINI "
                "verir, sınır çizgisi vermez (RESEARCH.md). Metrik sınır üretmek "
                "uydurmak olurdu. Buradakiler plan Faz 1 madde 6'nın yayın hücreleridir; "
                "hepsi tier=Graybox ve historical_claim=none taşır."
            ),
            "streaming": {
                "distance_rule": ("Yükleme ölçütü poligonun KENARINA uzaklıktır (içerideyken 0), "
                                  "merkezine değil. Merkez+yarıçap uzun/bükük bölgelerde "
                                  "(Haliç şeridi) fena hâlde yanılır."),
                "hysteresis": (f"unload_distance = load_distance x {UNLOAD_FACTOR}; tek eşik "
                               f"kullanılsaydı sınırda gidip gelen oyuncu sahneyi sürekli "
                               f"yükleyip boşaltırdı"),
                "load_distance_m": LOAD_MARGIN_M,
                "unload_distance_m": LOAD_MARGIN_M * UNLOAD_FACTOR,
                "overlap_ok": ("Bölgeler bilerek çakışabilir — aynı anda birden çok "
                               "bölgenin yüklü olması yükleme ekransız geçişin ta kendisidir. "
                               "Su bölgeleri (Boğaz, Haliç) kara bölgeleriyle örtüşür."),
            },
            "measured": {
                "grid_step_m": GRID_STEP_M,
                "exclusive_land_ha_total": round(total_land, 1),
                "exclusive_land_ha_priority1": round(p1_land, 1),
                "note": ("`land_area_ha` bölgenin İÇİNDEKİ tüm karadır ve bölgeler "
                         "çakıştığı için TOPLANAMAZ. Faz 4 yerleştirme bütçesi için "
                         "`exclusive_land_area_ha` kullanılır: her kara hücresi, onu "
                         "içeren en yüksek öncelikli KARA bölgesine tek bir kez sayılır; "
                         "su bölgeleri kara sahiplenmez."),
                "corridor_checked": [f"{a}->{b}" for a, b in CORRIDOR],
            },
            "world_origin": meta["world_origin"],
            "copyright": "Bu bölgeleme bize aittir (oynanış kararı).",
        },
        "features": features,
    }

    geo_path = os.path.abspath(args.geojson)
    os.makedirs(os.path.dirname(geo_path), exist_ok=True)
    with open(geo_path, "w", encoding="utf-8") as fh:
        json.dump(collection, fh, indent=1, ensure_ascii=False)
    log(f"wrote {args.geojson} ({os.path.getsize(geo_path)//1024} KB, "
        f"{len(features)} bolge)")

    local_path = os.path.join(os.path.abspath(args.dir), "districts_local.json")
    with open(local_path, "w", encoding="utf-8") as fh:
        json.dump({"world_origin": meta["world_origin"], "features": local_features},
                  fh, ensure_ascii=False)
    log("wrote districts_local.json")
    log("districts_build OK")


if __name__ == "__main__":
    main()
