"""
Hezarfen: 1632 — Karakter (Faz 5).

Taban gövde CC0 paketten gelir (kayıt: `art/base/blender-studio/meta.json`);
kıyafet **gövdeden türetilen kabuklarla** kurulur ve biçim Rålamb
albümünden okunan dilbilgisine dayanır (`docs/RESEARCH.md`).

Üretilen üç durum:

- `Hezarfen_Govde`  — çıplak taban. Ara ürün; rig ve NPC varyantları için.
- `Hezarfen_Sivil`  — ayak bileğine inen uzun entari. Yerdeki Hezarfen.
- `Hezarfen_Ucus`   — baldıra kadar kısa entari + dizlik. Kuleye çıkan ve
                      uçan Hezarfen. Kısalık bir tasarım tercihi değil:
                      plakalarda **çalışan adamın entarisi kısadır**.

Kullanım:
  blender --background --python tools/blender/gen_hezarfen.py -- [--export]
"""

import argparse
import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz             # noqa: E402
import karakter_kit as kar          # noqa: E402
import kiyafet_kit as kiy           # noqa: E402
import ottoman_kit as kit           # noqa: E402
import rig_kit as rk                # noqa: E402
import sac_kit as sk                # noqa: E402
from export_fbx import export_fbx   # noqa: E402

COLLECTION = "Export"

SOURCE_GOVDE = (
    "HEZARFEN AHMED CELEBI'nin GOVDESI. Portresi YOKTUR — ne minyaturu, "
    "ne tarifi. RESEARCH.md: sahsi hakkinda bilinen tek sey Evliya'nin "
    "birkac cumlesidir. Bu yuzden govde bir BENZERLIK iddiasi tasimaz; "
    "genel yetiskin erkek anatomisidir. Taban geometri Blender Studio "
    "Human Base Meshes v1.4.1 (CC0, kayit: art/base/blender-studio/"
    "meta.json). Boy 1,70 m ve bu sayi keyfi degil: bu projenin butun "
    "inceleme paketleri 1,70 m'lik olcek figurune gore yargilandi."
)
SOURCE_KIYAFET = (
    "KIYAFET: Ralamb kiyafet albumunden (1657-58, kamu mali, refs/ralamb/) "
    "OKUNAN dilbilgisi. Minyatur kopyalanmadi; okunan sey oranlardir — "
    "entarinin boyu isi soyler (oturan ayak bilegine, calisan baldira), "
    "kusak dogal belde ve dar, salvar entarinin altindan GORUNUR, kol agzi "
    "astari ters cevrilir, dizlik gercek bir ogedir (plaka 50), baslik "
    "rutbe gostergesidir ve Hezarfen ne pasa ne asker. UYARI: album "
    "1657-58, oyun 1632 — YIRMI BES YIL fark. Ana hatlar (salvar-gomlek-"
    "entari-kusak-sarik) bu aralikta degismedi ama ayrinti degisebilir. "
    "Peter Mundy (1618) obur yandan yaklasiyor; 1632'nin TAM ORTASINDA "
    "BIR KAYNAK YOK ve bu bosluk kapatilamaz, ancak soylenebilir."
)

#: (ad, etek_kotu_orani, dizlik_var, neden)
VARIANTS = [
    ("Hezarfen_Sivil", kiy.BILEK_ORAN, False,
     "yerdeki Hezarfen — uzun entari, oturan adamin boyu"),
    ("Hezarfen_Ucus", kiy.BALDIR_ORAN, True,
     "kuleye cikan ve ucan Hezarfen — kisa entari + dizlik"),
]


def giydir(govde, col, mats, etek_orani, dizlik_var):
    """Gövdeye kıyafeti giydirir; parça listesi döner."""
    parts = []
    mn, mx = hz.bounds(govde)
    boy = mx[2] - mn[2]

    z_bel = boy * kiy.BEL_ORAN
    z_kalca = boy * kiy.KALCA_ORAN
    z_diz = boy * kiy.DIZ_ORAN
    z_etek = boy * etek_orani
    z_boyun = boy * 0.855

    # --- Kol/govde siniri OLCULUR, elle yazilmaz -------------------------
    kol_esik = kiy.kol_siniri(govde, z_kalca) or (boy * 0.11)

    # Kol bolgesinin en alt noktasi parmak ucudur; bilek ondan bir el boyu
    # (boyun ~%10,5'i) yukaridadir. Kol agzi bilekte biter — plakalarda
    # parmaklar gorunur, entari eli yutmaz.
    kol_vs = [v.co.z for v in govde.data.vertices if abs(v.co.x) >= kol_esik]
    z_parmak = min(kol_vs) if kol_vs else boy * 0.36
    z_bilek = z_parmak + boy * 0.105

    # --- GOMLEK (ic keten) ------------------------------------------------
    gomlek = kiy.kopya_kabuk(
        govde, "Gomlek", col,
        tut=lambda c: z_kalca <= c.z <= z_boyun,
        sisme=0.008, kalinlik=kiy.GOMLEK_KAL)
    if gomlek:
        parts.append(hz.assign(gomlek, mats["gomlek"]))

    # --- SALVAR ------------------------------------------------------------
    # Sisme kota GORE degisir: bilekte dar, uylukte bol, belde toplanir.
    # Salvarin bicimi budur; sabit bir ofset dar pantolon verirdi ve
    # salvar dar pantolonun tam tersidir.
    z0 = boy * 0.045

    # KATMAN KURALI: ust katman alt katmandan HER KOTTA daha cok sismeli.
    # Ilk turda salvar belde 0,030, entari 0,021 sisiyordu — alt katman
    # ustu deldi ve karinda kirmizi bir leke cikti. Kumas kalinligi degil
    # SISME sirasi belirler kimin ustte oldugunu.
    SALVAR_UYLUK = 0.042
    SALVAR_BEL = 0.022
    ENTARI_SIS = 0.034          # > SALVAR_BEL
    ETEK_PAY = 0.058            # > SALVAR_UYLUK

    def salvar_sis(c):
        t = min(1.0, max(0.0, (c.z - z0) / max(1e-6, z_bel - z0)))
        if t < 0.55:
            return 0.010 + (SALVAR_UYLUK - 0.010) * (t / 0.55)
        return SALVAR_UYLUK + (SALVAR_BEL - SALVAR_UYLUK) * ((t - 0.55) / 0.45)

    salvar = kiy.kopya_kabuk(
        govde, "Salvar", col,
        tut=lambda c: (z0 <= c.z <= z_bel) and abs(c.x) < kol_esik,
        sisme=salvar_sis, kalinlik=kiy.GOMLEK_KAL)
    if salvar:
        parts.append(hz.assign(salvar, mats["salvar"]))

    # --- ENTARI: govde + kollar --------------------------------------------
    # Bacaklar DISARIDA birakiliyor: etek onlari takip etmez, serbest duser.
    entari_ust = kiy.kopya_kabuk(
        govde, "Entari_Ust", col,
        tut=lambda c: (z_kalca <= c.z <= z_boyun)
        or (abs(c.x) >= kol_esik and c.z >= z_bilek),
        sisme=ENTARI_SIS, kalinlik=kiy.ENTARI_KAL)
    if entari_ust:
        parts.append(hz.assign(entari_ust, mats["entari"]))

    # --- ENTARI ETEGI -------------------------------------------------------
    kalca = (kiy.kesit(govde, z_kalca, x_esik=kol_esik)
             or (boy * 0.11, boy * 0.075))
    r_ust = (kalca[0] + ETEK_PAY, kalca[1] + ETEK_PAY)
    # Kisa entari daha cok acilir: hareket eden adamin adimina yer birakir.
    acilma = 1.34 if etek_orani < 0.15 else 1.52
    r_alt = (r_ust[0] * acilma, r_ust[1] * acilma)
    parts.append(hz.assign(kiy.etek(
        "Entari_Etek", col, z_kalca + 0.02, z_etek,
        r_ust, r_alt, kiy.ENTARI_KAL, yarik=True), mats["entari"]))

    # --- KUSAK ---------------------------------------------------------------
    bel = (kiy.kesit(govde, z_bel, x_esik=kol_esik)
           or (boy * 0.10, boy * 0.068))
    # Kusak entarinin USTUNDE baglanir; ic katman degildir. Ilk turda
    # yaricapi entarininkinden kucuktu (0,024 < 0,034) ve kusak entarinin
    # ICINDE kaldi — yalnizca belin ve sirtin CUKUR yerlerinde bir damla
    # gibi disari sizdi. Bir kusak gorunmuyorsa kusak degildir.
    kusak_pay = ENTARI_SIS + kiy.ENTARI_KAL + 0.012
    parts.append(hz.assign(kiy.band(
        "Kusak", col, z_bel, (bel[0] + kusak_pay, bel[1] + kusak_pay),
        boy * 0.055, kiy.KUSAK_KAL), mats["kusak"]))

    # --- DIZLIK (yalniz ucus varyanti) ---------------------------------------
    if dizlik_var:
        # Dizlik SALVARIN USTUNE baglanir — salvari dizde toplayan sey odur
        # (plaka 50). Yaricap bacaktan OLCULUR ve salvarin o kottaki
        # sismesi eklenir; elle bir sayi yazsaydim dizlik salvarin icinde
        # kalir ve hic gorunmezdi.
        diz_sis = salvar_sis(type("P", (), {"z": z_diz})())
        for sx in (-1, 1):
            bk = kiy.bacak_kesit(govde, z_diz, sx)
            if bk is None:
                continue
            cx, rx, ry = bk
            pay = diz_sis + kiy.GOMLEK_KAL + 0.010
            b = kiy.band(f"Dizlik_{sx}", col, z_diz,
                         (rx + pay, ry + pay), boy * 0.030, 0.007)
            for v in b.data.vertices:
                v.co.x += cx
            b.data.update()
            parts.append(hz.assign(b, mats["leather"]))

    # --- SARIK ----------------------------------------------------------------
    # Sarik ALINDAN baslar ve basin tepesini ASAR — baslik odur. Yarıcap
    # kafanin kendi genisliginden turer, elle yazilmaz.
    # Taban KASIN USTUNDE: ilk turda 0,905'ten basliyordu ve sarik gozleri
    # ortuyordu. Bir baslik yuzu kapatmaz.
    bas = kiy.kesit(govde, boy * 0.955) or (boy * 0.048, boy * 0.058)
    bas_r = max(bas[0], boy * 0.048)
    # KAVUK: sarigin altindaki cekirdek. Plaka 35 ve 50'de sarigin
    # tepesinden kirmizi bir tepe gorunur — sarik bir kupa degil, bir
    # cekirdegin uzerine sarilmis bezdir. Cekirdek olmadan sarigin
    # tepesindeki delikten bosluk gorunuyordu.
    parts.append(hz.assign(kiy.band(
        "Kavuk", col, boy * 1.005, (bas_r * 0.80, bas_r * 0.74),
        boy * 0.075, 0.006), mats["kavuk"]))
    parts.append(hz.assign(kiy.sarik(
        "Sarik", col, boy * 0.948, boy * 1.052, bas_r), mats["sarik"]))

    # --- SAKAL ve SAC ----------------------------------------------------------
    #
    # AZ kart, cunku Hezarfen sarikli: sarik sacin cogunu orter ve
    # gorunen sey sakal, sakak, ense. Kafanin tamamini kaplamak hic
    # gorunmeyecek 40 kartin bedelini odemek olurdu.
    sac_mat = sk.hair_material()
    hat = sk.cene_hatti(govde, boy)
    # UC KATMAN. Ilk yazimda tek siraydi ve sakal render'da neredeyse
    # gorunmuyordu — kartlar dogru yerdeydi (olculdu), ama bir sakal
    # birkac tel degil bir KUTLEDIR. Alfa kapsamasi %24; tek katman
    # yuzeyin dortte birini doldurur. Uc katman ust uste binince kutle
    # okunur, ve bindirme zaten sac kartlarinin calisma bicimidir.
    #
    # Katmanlar farkli SERIT kullanir: hepsi ayni desen olsaydi
    # bindirme bir tekrar deseni uretirdi ve o tekrar okunurdu.
    # KATMAN OFSETI mutlak mesafedir, oran degil. Ilk yazimda konumu
    # 0,986/1,008/1,028 ile olcekliyordum; y ~ -0,06 oldugu icin bu
    # katmanlari 1,7 mm ayiriyordu — hicbir sey. Giysi kabuklarinda
    # ogrendigimiz sey burada da gecerli: dis katman ic katmandan
    # OLCULEBILIR kadar disarida olmali.
    for kat, (pay, ser, uz) in enumerate((
            (0.008, 0, 0.058), (0.016, 2, 0.082), (0.024, 1, 0.104))):
        for i, (p, yon) in enumerate(hat):
            for sx in (-1, 1):
                if sx > 0 and abs(p.x) < boy * 0.004:
                    continue              # on ortadaki kart tek
                k = sk.kart(f"Sakal_{kat}_{sx}_{i}",
                            (p.x * sx + yon.x * sx * pay,
                             p.y + yon.y * pay, p.z),
                            # Sakal asagi ve hafifce disari sarkar.
                            (yon.x * sx * 0.42, yon.y * 0.42, -1.0),
                            (yon.x * sx, yon.y, 0.30),
                            boy * uz, boy * 0.034, col,
                            serit=ser, egim=0.20 + 0.06 * kat)
                parts.append(hz.assign(k, sac_mat))

    # BIYIK: ust dudak. Plaka 20 ve 35'te sakalla birlikte var.
    for sx in (-1, 1):
        b = sk.kart(f"Biyik_{sx}", (sx * boy * 0.010, -boy * 0.052,
                                    boy * 0.905),
                    (sx * 0.85, -0.30, -0.42), (0.0, -1.0, 0.25),
                    boy * 0.026, boy * 0.020, col, serit=2, egim=0.10)
        parts.append(hz.assign(b, sac_mat))

    # Sakak ve ense: sarigin altindan cikan tutamlar.
    for sx in (-1, 1):
        for i, (dy, dz, ser) in enumerate((
                (0.30, 0.905, 0), (0.10, 0.895, 3), (-0.34, 0.900, 1))):
            kesit = kiy.kesit(govde, boy * dz) or (boy * 0.05, boy * 0.06)
            k = sk.kart(f"Sac_{sx}_{i}",
                        (sx * kesit[0] * 0.94, dy * kesit[1], boy * dz),
                        (sx * 0.35, dy * 0.3, -1.0), (sx, dy * 0.4, 0.25),
                        boy * 0.060, boy * 0.034, col, serit=ser, egim=0.30)
            parts.append(hz.assign(k, sac_mat))

    # --- MEST ------------------------------------------------------------------
    mest = kiy.kopya_kabuk(
        govde, "Mest", col,
        tut=lambda c: c.z <= boy * 0.042,
        sisme=0.007, kalinlik=0.004)
    if mest:
        kiy.zemine_otur(mest)
        parts.append(hz.assign(mest, mats["mest"]))

    return parts, kol_esik


def taban_kur(args, mats):
    """Çıplak taban gövdeyi getirir, ölçer, normalleştirir."""
    govde = kar.taban_getir(args.taban, col=hz.collection(COLLECTION))
    kar.temiz_ag(govde)
    aci = kar.one_cevir(govde)
    k = kar.normalize(govde)
    hz.assign(govde, mats["skin"])
    return govde, aci, k


def denetle(olcu):
    """Oranlar insan mı — render 'doğru görünüyor' der, bunlar doğru
    OLUP OLMADIĞINI söyler."""
    if not 6.5 <= olcu["bas_orani"] <= 8.5:
        raise SystemExit(f"[HZ] HATA bas orani 1/{olcu['bas_orani']} — "
                         "yetiskin insan 1/7 ile 1/8 arasindadir.")
    if not 0.36 <= olcu["omuz_genisligi"] <= 0.50:
        raise SystemExit(f"[HZ] HATA omuz {olcu['omuz_genisligi']:.3f} m — "
                         "yetiskin erkek 0,38-0,48 m arasindadir.")
    if olcu["boyun_genisligi"] >= olcu["omuz_genisligi"] * 0.62:
        raise SystemExit(f"[HZ] HATA boyun {olcu['boyun_genisligi']:.3f} m, "
                         f"omuz {olcu['omuz_genisligi']:.3f} m — olcum "
                         "boynu bulamadi.")


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"))
    ap.add_argument("--blend-dir", default=os.path.join("art", "blend", "karakter"))
    ap.add_argument("--catalog", default=os.path.join("art", "blend", "karakter",
                                                      "catalog.json"))
    ap.add_argument("--taban", default=None, help="Human Base Meshes .blend yolu")
    ap.add_argument("--no-textures", action="store_true")
    # Rig'siz bir karakteri prefab yapmak erken olur; `_Import` bos kalir
    # (CLAUDE.md: "_Import bos birakilir").
    ap.add_argument("--export", action="store_true",
                    help="FBX'i _Import'a yaz (varsayilan: yazma)")
    args = ap.parse_args(hz.argv_after_dashes())

    os.makedirs(args.out_dir, exist_ok=True)
    os.makedirs(args.blend_dir, exist_ok=True)
    catalog = []

    # --- 1) CIPLAK TABAN ----------------------------------------------------
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    mats, tex_sizes = kit.build_materials("default", textured=not args.no_textures)
    asset = "SK_Hezarfen_Govde"
    govde, aci, k = taban_kur(args, mats)
    govde.name = f"{asset}_LOD0"
    govde.data.name = govde.name
    olcu = kar.olcu_al(govde)
    hz.log(f"taban: boy {olcu['boy']:.3f} m, bas orani 1/{olcu['bas_orani']}, "
           f"omuz {olcu['omuz_genisligi']:.3f} m (boyun "
           f"{olcu['boyun_genisligi']:.3f} m), yon {aci * 57.2958:.0f} derece")
    denetle(olcu)

    lod1 = kar.desimasyon(govde, 0.35, f"{asset}_LOD1")
    hz.link(lod1, col)
    hz.assign(lod1, mats["skin"])
    catalog.append(dict(
        name="Hezarfen_Govde", prefab=None,
        prefab_notu="Ara urun: rig ve kiyafet tamamlanmadan prefab uretilmez.",
        kind="karakter", state="base", status="draft", accuracy="D3",
        tier="T3", source=SOURCE_GOVDE,
        taban_paket="human-base-meshes-bundle-v1.4.1",
        taban_obje=kar.TABAN_OBJE, taban_lisans="CC0-1.0",
        yon_duzeltme_derece=round(aci * 57.2958, 2), olcek_carpani=round(k, 5),
        tris_lod0=kar.hz_tri(govde), tris_lod1=kar.hz_tri(lod1),
        uv_var=kar.uv_var_mi(govde), **olcu))
    hz.save_blend(os.path.join(args.blend_dir, f"{asset}.blend"))

    # --- 2) GIYINIK VARYANTLAR ------------------------------------------------
    for ad, etek_orani, dizlik_var, why in VARIANTS:
        hz.reset_scene()
        col = hz.collection(COLLECTION)
        mats, tex_sizes = kit.build_materials("default",
                                              textured=not args.no_textures)
        asset = f"SK_{ad}"
        govde, _, _ = taban_kur(args, mats)
        giysi, kol_esik = giydir(govde, col, mats, etek_orani, dizlik_var)

        # Eklemler CIPLAK govdeden olculur, giyinikten degil: entari
        # omzu 3,4 cm kalinlastirir ve omuz eklemini o kadar disari
        # atardi. Rig tenin altindadir.
        eklem = rk.eklemleri_olc(govde, kol_esik)
        rig_hata = rk.uzuv_denetimi(eklem, kar.HEDEF_BOY)
        if rig_hata:
            raise SystemExit(f"[HZ] HATA {ad} rig: " + "; ".join(rig_hata))

        govde.name = f"{asset}_ten"
        lod0 = kit.join_parts([govde] + giysi, f"{asset}_LOD0", col)
        lod1 = kar.desimasyon(lod0, 0.30, f"{asset}_LOD1")
        hz.link(lod1, col)

        arm = rk.iskelet_kur(f"AR_{ad}", eklem, col)
        for m in (lod0, lod1):
            rk.deri_bagla(m, arm)

        mn, mx = hz.bounds(lod0)
        bilgi = dict(
            name=ad, prefab=None,
            prefab_notu="Ara urun: rig tamamlanmadan prefab uretilmez.",
            kind="karakter", state="dressed", status="draft", accuracy="D3",
            # Varlik EN DUSUK bileseninden etiketlenir: govde T3, kiyafet T2
            # -> giyinik karakter T3. Kiyafeti T2 diye etiketleyip govdeyi
            # gormemek, bilinmeyeni bilinir gostermek olurdu.
            tier="T3", kiyafet_tier="T2", source=SOURCE_KIYAFET, why=why,
            boy=round(mx[2] - mn[2], 4),
            en_genis=round(mx[0] - mn[0], 4),
            derinlik=round(mx[1] - mn[1], 4),
            etek_kotu=round(kar.HEDEF_BOY * etek_orani, 3),
            dizlik=dizlik_var, giysi_parca=len(giysi),
            tris_lod0=kar.hz_tri(lod0), tris_lod1=kar.hz_tri(lod1),
            kemik=len(arm.data.bones),
            kemikler=rk.kemik_raporu(arm, kar.HEDEF_BOY))

        # Genislik denetimi: giyinik adam ciplaktan en fazla ~%25 genis
        # olur (kollar entariyle kalinlasir, etek acilir). Ilk turda 3,29 m
        # cikti — cunku govde paketin kendi konumunda kalmisti ve giysiler
        # baska yerde duruyordu. Bounding box yalan soylemez: bir sayi
        # insana ait olamayacak kadar buyukse once o sorulur.
        if bilgi["en_genis"] > olcu["en_genis"] * 1.25:
            raise SystemExit(
                f"[HZ] HATA {ad}: giyinik genislik {bilgi['en_genis']:.3f} m, "
                f"ciplak {olcu['en_genis']:.3f} m — bir parca govdeden "
                "kopmus olmali.")
        # Unity Humanoid eksik kemikle avatar kurmaz ve hata mesaji
        # hangi kemigin eksik oldugunu soylemez; burada soyluyoruz.
        eksik = [b for b, _ in rk.HUMANOID if b not in arm.data.bones]
        if eksik:
            raise SystemExit(
                f"[HZ] HATA {ad}: Humanoid kemikleri eksik: {eksik}")
        # Sarik boyu artirir ama 1,70 + sarik hala insan boyudur.
        if bilgi["boy"] > kar.HEDEF_BOY * 1.10:
            raise SystemExit(
                f"[HZ] HATA {ad}: giyinik boy {bilgi['boy']:.3f} m — ciplak "
                f"{kar.HEDEF_BOY} m'nin %10'undan fazla ustunde.")
        # Bir kabuk secimi bos donerse giysi sessizce eksik kalir ve
        # karakter yari ciplak cikar; sayi onu yakalar.
        beklenen = 50 if dizlik_var else 48
        if bilgi["giysi_parca"] < beklenen:
            raise SystemExit(
                f"[HZ] HATA {ad}: {bilgi['giysi_parca']} giysi parcasi "
                f"uretildi, en az {beklenen} bekleniyordu — bir kabuk "
                "secimi bos donmus olabilir.")

        catalog.append(bilgi)
        hz.log(f"{ad:16s} boy {bilgi['boy']:.3f} m, etek {bilgi['etek_kotu']:.2f} m, "
               f"{bilgi['giysi_parca']} parca, {bilgi['kemik']} kemik, "
               f"{bilgi['tris_lod0']:6d} / {bilgi['tris_lod1']:5d} ucgen")

        hz.save_blend(os.path.join(args.blend_dir, f"{asset}.blend"))
        if args.export:
            export_fbx(os.path.join(args.out_dir, f"{asset}.fbx"),
                       collection_name=COLLECTION, skinned=True)

    if not args.export:
        hz.log("FBX yazilmadi (--export ile): karakter rig'siz Unity'ye gitmez.")

    catalog.sort(key=lambda v: v["name"])
    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(catalog)} karakter durumu; katalog: {args.catalog}")


if __name__ == "__main__":
    main()
