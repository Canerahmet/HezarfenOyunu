"""
Hezarfen: 1632 — Poly Haven (CC0) doku indirici (plan Faz 2, doku stratejisi).

Planın doku kararı: *"Poly Haven (CC0) + kendi bake'lerimiz — CC0 = ticari oyunda
atıfsız kullanım serbest."* Bu script o kaynağı **tekrarlanabilir** hale getirir:
hangi dokunun indirildiği, hangi çözünürlükte, hangi gerçek dünya ölçüsünde ve
kimin ürettiği dosyaya yazılır.

## Neden elle indirmiyoruz

İki sebep, ikisi de sonradan pahalıya patlar:

1. **Gerçek dünya ölçüsü.** Poly Haven her dokunun kaç metreyi kapladığını verir
   (`dimensions`, mm). Bu bilgi olmadan UV ölçeği göz kararı olur ve duvardaki
   taş ile çatıdaki kiremit birbirine göre yanlış boyda çıkar — render'da
   "ucuz" görünmenin bir numaralı sebebi budur. Ölçü `meta.json`a yazılır ve
   malzeme kurucusu onu okur.
2. **Atıf.** Poly Haven CC0'dır, yani hukuken atıf ZORUNLU değildir; ama plan
   krediler ekranına yazılmasını istiyor ve üreticilerin adı ancak indirme
   anında kaydedilirse elde kalır. `refs/LICENSES.md` otomatik güncellenir.

## Harita seçimi

  Diffuse  -> taban renk (jpg yeter; renk verisi sıkıştırmayı affeder)
  nor_gl   -> normal, **OpenGL** yönü (Blender ve Unity'nin ikisi de Y+ bekler;
              `nor_dx` alınırsa girintiler çıkıntı olur — sessiz ve çok yaygın hata)
  Rough    -> pürüzlülük
  AO       -> ortam örtme

`arm` (AO+Rough+Metallic paketli) da indirilir çünkü Unity tarafında maske
üretimini ucuzlatır; HDRP'nin maske düzeni ARM'dan FARKLI olduğu için doğrudan
kullanılmaz, yeniden paketlenir (Faz 2 Unity pası).

Kullanım:
    python tools/textures/fetch_polyhaven.py --res 2k
    python tools/textures/fetch_polyhaven.py --only roof --res 4k
"""

import argparse
import json
import os
import sys
import urllib.request

API = "https://api.polyhaven.com"
OUT_ROOT = os.path.join("art", "textures", "polyhaven")
LICENSES = os.path.join("refs", "LICENSES.md")
MARK_BEGIN = "<!-- POLYHAVEN:BEGIN (otomatik — fetch_polyhaven.py üretir) -->"
MARK_END = "<!-- POLYHAVEN:END -->"

# rol -> (polyhaven id, ne icin, gerekce)
#
# Secimler ISIMDEN degil, kucuk onizlemelere BAKILARAK yapildi (2026-08-20):
# adaylar tek tabakaya dizilip karsilastirildi. "old_stone_wall" moloz tas
# subasmani birebir veriyor; "plastered_stone_wall" neredeyse siyahti ve elendi.
CATALOG = {
    "plaster": dict(
        id="painted_plaster_wall",
        use="Kireç badanalı kâgir kat (M_Plaster_Lime)",
        why="Açık, sıcak, pürüzsüz badana — Müslüman mahalle konutunun alt katı",
    ),
    "plaster_dark": dict(
        id="grey_plaster",
        use="Gayrimüslim mahalle varyantı (M_Plaster_Grey)",
        why="RESEARCH.md: bu evler 'daha koyu ve alçak'; T2 kural",
    ),
    "stone": dict(
        id="old_stone_wall",
        use="Taş subasman ve avlu duvarı (M_Stone_Rubble)",
        why="Düzensiz moloz taş + harç dolgusu — dönem subasmanının dokusu",
    ),
    "cutstone": dict(
        id="large_sandstone_blocks",
        use="Kesme taş: çeşme gövdesi, ayna taşı, kitabe, mescit duvarı (M_Stone_Cut)",
        why="Moloz taş (old_stone_wall) subasman içindir. Çeşmenin ayna taşı ve "
            "kitabesi OYMA taştır; moloz doku oraya konunca yapı 'duvar parçası' "
            "gibi okunur. Düzgün derzli kesme blok bu ayrımı verir",
    ),
    "timber": dict(
        id="weathered_planks",
        use="Ahşap karkas üst kat / cumba (M_Timber_AsiRed)",
        why="Yıpranmış ahşap; aşı boyası tonu malzemede renk karışımıyla verilir",
    ),
    "roof": dict(
        id="clay_roof_tiles_02",
        use="Alaturka kiremit (M_Roof_Alaturka)",
        why="Oluklu/beşik profil — alaturka kiremidin kendisi",
    ),
    "roof_aged": dict(
        id="ceramic_roof_01",
        use="Yaşlanmış çatı varyantı (M_Roof_Alaturka_Aged)",
        why="Yosunlu, soluk; eski/yoksul evlerde çeşitlilik için",
    ),
    "paving": dict(
        id="cobblestone_floor_001",
        use="Sokak kaldırımı (M_Paving_Kaldirim)",
        why="Önizlemeler karşılaştırıldı (2026-08-21): 'cobblestone_05' düzgün "
            "derzli Avrupa parke taşı, çok modern; 'brick_pavement_03' tuğla, "
            "yanlış malzeme; 'cobblestone_02' yuvarlak dere taşı — arnavut "
            "kaldırımına yakın ama 4,6 m sokakta çakıl yolu gibi okunuyor. "
            "Seçilen: çamura oturmuş düzensiz yassı taş, 'medieval' etiketli — "
            "bakımsız Osmanlı sokağının kendisi",
    ),
    "bark": dict(
        id="bark_brown_01",
        use="Servi gövdesi (M_Bark)",
        why="Dikey lifli, kızıl-kahve kabuk — servinin gövdesi",
    ),
    "bark_cinar": dict(
        id="bark_platanus",
        use="Çınar gövdesi (M_Bark_Cinar)",
        why="Doğrudan PLATANUS kabuğu: çınarın alacalı, pul pul dökülen kabuğu "
            "ağacın tanınma işaretlerinden biri; genel kabukla değiştirilemez",
    ),
}

# harita -> tercih edilen format.
# Normal PNG: jpg sikistirmasi normal haritada bantlanma ve yanlis egim uretir;
# renk haritasinda ayni sikistirma gorunmez. Bu yuzden format harita basina secilir.
MAP_FORMAT = {
    "Diffuse": "jpg",
    "nor_gl": "png",
    "Rough": "jpg",
    "AO": "jpg",
    "arm": "jpg",
}
SUFFIX = {
    "Diffuse": "BC",     # base color
    "nor_gl": "N",
    "Rough": "R",
    "AO": "AO",
    "arm": "ARM",
}


# HDRI'lar: inceleme render'inin GERCEKCI kipi icin (ADR 0006'nin notr kipi
# BOZULMAZ; ayri bir aydinlatma secenegi olarak eklenir).
#
# `puresky` serisi bilerek secildi: yalnizca gokyuzu icerir, cevrede aga/binaya
# ait yansimalar yoktur. Mimari bir varligi degerlendirirken cevreden gelen
# yabanci renkler malzemeyi yanlis gosterir.
HDRI_ROOT = os.path.join("art", "textures", "hdri")
HDRI_CATALOG = {
    "day": dict(
        id="kloofendal_48d_partly_cloudy_puresky",
        use="Gündüz inceleme aydınlatması (güneş 48°, hafif bulutlu)",
        why="Gölge verecek kadar güçlü güneş + derin gölgeleri açan gök dolgusu; "
            "İstanbul öğle sonrası için makul",
    ),
}


def log(msg):
    print(f"[HZ] {msg}", flush=True)


# Poly Haven, urllib'in varsayilan User-Agent'ini 403 ile reddediyor.
# Kendimizi tanitmak hem sarttir hem de dogrusu: otomatik bir istemciyiz.
UA = "Hezarfen1632/1.0 (oyun varlik hatti; https://polyhaven.com CC0 kullanimi)"


def _open(url, timeout):
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    return urllib.request.urlopen(req, timeout=timeout)


def get_json(url):
    with _open(url, 60) as r:
        return json.load(r)


def download(url, path):
    if os.path.exists(path) and os.path.getsize(path) > 0:
        return os.path.getsize(path), True
    os.makedirs(os.path.dirname(path), exist_ok=True)
    tmp = path + ".part"
    with _open(url, 600) as r, open(tmp, "wb") as fh:
        while True:
            chunk = r.read(1 << 20)
            if not chunk:
                break
            fh.write(chunk)
    os.replace(tmp, path)
    return os.path.getsize(path), False


def pick(files, map_name, res):
    """İstenen çözünürlük yoksa mevcut en yakınına düşer — sessizce atlamaz."""
    node = files.get(map_name)
    if not isinstance(node, dict):
        return None, None
    if res not in node:
        avail = [r for r in ("4k", "2k", "1k", "8k") if r in node]
        if not avail:
            return None, None
        log(f"    UYARI {map_name}: {res} yok, {avail[0]} kullaniliyor")
        res = avail[0]
    fmts = node[res]
    fmt = MAP_FORMAT.get(map_name, "jpg")
    if fmt not in fmts:
        fmt = next(iter(fmts))
    return fmts[fmt].get("url"), fmt


def fetch_one(role, entry, res, assets_meta):
    aid = entry["id"]
    log(f"{role} -> {aid}")
    files = get_json(f"{API}/files/{aid}")
    meta = assets_meta.get(aid) or get_json(f"{API}/info/{aid}")

    dims = meta.get("dimensions") or [2000.0, 2000.0]
    out_dir = os.path.join(OUT_ROOT, aid)
    maps = {}
    total = 0
    for map_name in ("Diffuse", "nor_gl", "Rough", "AO", "arm"):
        url, fmt = pick(files, map_name, res)
        if not url:
            log(f"    {map_name}: YOK")
            continue
        name = f"T_{aid}_{SUFFIX[map_name]}.{fmt}"
        path = os.path.join(out_dir, name)
        size, cached = download(url, path)
        total += size
        maps[SUFFIX[map_name]] = name
        log(f"    {map_name:8} -> {name} ({size // 1024} KB{', onbellek' if cached else ''})")

    record = {
        "polyhaven_id": aid,
        "name": meta.get("name", aid),
        "resolution": res,
        # Gercek dunya olcusu METRE. UV olcegi bunu okur; goz karari yapilmaz.
        "size_meters": [round(dims[0] / 1000.0, 4), round(dims[1] / 1000.0, 4)],
        "authors": meta.get("authors", {}),
        "license": "CC0",
        "source": f"https://polyhaven.com/a/{aid}",
        "role": role,
        "use": entry["use"],
        "why": entry["why"],
        "maps": maps,
        "normal_convention": "OpenGL (Y+) — nor_gl; Blender ve Unity ayni yonu bekler",
    }
    with open(os.path.join(out_dir, "meta.json"), "w", encoding="utf-8") as fh:
        json.dump(record, fh, ensure_ascii=False, indent=1)
    return record, total


def fetch_hdri(role, entry, res):
    aid = entry["id"]
    log(f"HDRI {role} -> {aid}")
    files = get_json(f"{API}/files/{aid}")
    node = files.get("hdri") or {}
    if res not in node:
        avail = [r for r in ("4k", "2k", "1k", "8k") if r in node]
        if not avail:
            raise SystemExit(f"[HZ] {aid}: hdri dosyasi yok")
        log(f"    UYARI {res} yok, {avail[0]} kullaniliyor")
        res = avail[0]
    fmts = node[res]
    fmt = "hdr" if "hdr" in fmts else next(iter(fmts))
    url = fmts[fmt]["url"]

    path = os.path.join(HDRI_ROOT, f"{aid}_{res}.{fmt}")
    size, cached = download(url, path)
    log(f"    {res} {fmt} -> {os.path.basename(path)} "
        f"({size // 1024} KB{', onbellek' if cached else ''})")

    meta = get_json(f"{API}/info/{aid}")
    rec = {
        "polyhaven_id": aid, "name": meta.get("name", aid), "kind": "hdri",
        "resolution": res, "file": os.path.basename(path),
        "authors": meta.get("authors", {}), "license": "CC0",
        "source": f"https://polyhaven.com/a/{aid}",
        "role": role, "use": entry["use"], "why": entry["why"],
        # Gunes yuksekligi HDRI adinda gecer (48d); render'da gunes yonunu
        # ayrica kurmuyoruz, aydinlatma tamamen bu haritadan gelir.
    }
    with open(os.path.join(HDRI_ROOT, f"{aid}.meta.json"), "w", encoding="utf-8") as fh:
        json.dump(rec, fh, ensure_ascii=False, indent=1)
    return rec, size


def write_licenses(records):
    """`refs/LICENSES.md` içindeki otomatik bloğu yeniden yazar."""
    hdris = [r for r in records if r.get("kind") == "hdri"]
    records = [r for r in records if r.get("kind") != "hdri"]
    lines = [MARK_BEGIN, "",
             "### Poly Haven dokuları (CC0)", "",
             "Poly Haven **CC0**'dır: hukuken atıf zorunlu değildir. Yine de plan gereği "
             "krediler ekranına yazılır ve üreticiler burada kayıtlıdır.", "",
             "| Dosya kökü | Kullanım | Gerçek ölçü | Üretici(ler) | Kaynak |",
             "|---|---|---|---|---|"]
    for r in sorted(records, key=lambda x: x["role"]):
        authors = ", ".join(r["authors"].keys()) if r["authors"] else "—"
        sm = r["size_meters"]
        lines.append(f"| `art/textures/polyhaven/{r['polyhaven_id']}/` | {r['use']} | "
                     f"{sm[0]:.2f}×{sm[1]:.2f} m | {authors} | "
                     f"[polyhaven.com/a/{r['polyhaven_id']}]({r['source']}) |")
    if hdris:
        lines += ["", "### Poly Haven HDRI'ları (CC0)", "",
                  "İnceleme render'ının **gerçekçi** aydınlatma kipi için "
                  "(nötr kip ADR 0006'da tanımlıdır ve değişmez).", "",
                  "| Dosya | Kullanım | Üretici(ler) | Kaynak |", "|---|---|---|---|"]
        for r in sorted(hdris, key=lambda x: x["role"]):
            authors = ", ".join(r["authors"].keys()) if r["authors"] else "—"
            lines.append(f"| `art/textures/hdri/{r['file']}` | {r['use']} | {authors} | "
                         f"[polyhaven.com/a/{r['polyhaven_id']}]({r['source']}) |")

    lines += ["", "Yeniden indirme: `python tools/textures/fetch_polyhaven.py --res 2k --hdris`",
              "", MARK_END]
    block = "\n".join(lines)

    text = open(LICENSES, encoding="utf-8").read() if os.path.exists(LICENSES) else "# refs/ — Kaynak ve Lisans Kaydı\n"
    if MARK_BEGIN in text and MARK_END in text:
        head = text.split(MARK_BEGIN)[0]
        tail = text.split(MARK_END)[1]
        text = head + block + tail
    else:
        text = text.rstrip() + "\n\n" + block + "\n"
    with open(LICENSES, "w", encoding="utf-8") as fh:
        fh.write(text)
    log(f"wrote {LICENSES} (Poly Haven blogu: {len(records)} kayit)")


def main():
    p = argparse.ArgumentParser(description="Poly Haven CC0 doku indirici")
    p.add_argument("--res", default="2k", choices=("1k", "2k", "4k", "8k"))
    p.add_argument("--only", nargs="*", default=None,
                   help="Yalnizca bu roller (ornek: --only roof stone)")
    p.add_argument("--hdris", action="store_true",
                   help="Inceleme aydinlatmasi icin HDRI'lari da indir")
    p.add_argument("--hdri-res", default="2k", choices=("1k", "2k", "4k"))
    p.add_argument("--skip-textures", action="store_true")
    args = p.parse_args()

    roles = [] if args.skip_textures else (args.only or list(CATALOG))
    unknown = [r for r in roles if r not in CATALOG]
    if unknown:
        raise SystemExit(f"[HZ] bilinmeyen rol: {unknown}; secenekler {list(CATALOG)}")

    records, total = [], 0

    if roles:
        log(f"katalog listesi aliniyor ({len(roles)} rol, {args.res})")
        assets_meta = get_json(f"{API}/assets?t=textures")
        for role in roles:
            rec, size = fetch_one(role, CATALOG[role], args.res, assets_meta)
            records.append(rec)
            total += size

    if args.hdris:
        for role, entry in HDRI_CATALOG.items():
            rec, size = fetch_hdri(role, entry, args.hdri_res)
            records.append(rec)
            total += size

    # Lisans blogu TAM katalogla yazilir; --only ile calisildiginda daha once
    # indirilmis rollerin kaydi silinmesin diye diskteki meta.json'lar okunur.
    all_records = {r["polyhaven_id"]: r for r in records}
    if os.path.isdir(OUT_ROOT):
        for d in os.listdir(OUT_ROOT):
            mp = os.path.join(OUT_ROOT, d, "meta.json")
            if d not in all_records and os.path.exists(mp):
                all_records[d] = json.load(open(mp, encoding="utf-8"))
    if os.path.isdir(HDRI_ROOT):
        for f in os.listdir(HDRI_ROOT):
            if not f.endswith(".meta.json"):
                continue
            rec = json.load(open(os.path.join(HDRI_ROOT, f), encoding="utf-8"))
            all_records.setdefault(rec["polyhaven_id"], rec)
    write_licenses(list(all_records.values()))

    log(f"toplam {total / (1024 * 1024):.1f} MB, {len(records)} doku")
    log("fetch_polyhaven OK")


if __name__ == "__main__":
    main()
