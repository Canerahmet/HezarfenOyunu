"""
Hezarfen: 1632 — Poly Haven haritalarını Unity/HDRP düzenine çevirir.

Blender ile Unity aynı PBR haritalarını **farklı paketlerde** ister. İki iş:

## 1. Maske haritası (HDRP `Lit` düzeni)

Poly Haven `ARM` verir:   R = AO,        G = Roughness,  B = Metallic
HDRP `_MaskMap` ister:    R = Metallic,  G = AO,         B = Detay,  A = Smoothness

Yani kanallar **yer değiştirir** ve pürüzlülük **tersine çevrilir**
(smoothness = 1 − roughness). Bu dönüşüm yapılmazsa hata sessizdir ve tuhaftır:
metalik olması gereken hiçbir şey yokken duvar metalik olur, mat yüzeyler
parlar. Sebebini bulmak zordur çünkü doku "yüklenmiş" görünür.

Maske **alfa kanalı taşır**, bu yüzden JPG olamaz — PNG yazılır.

## 2. Boyalı albedo'nun pişirilmesi

Aşı boyası Blender'da düğüm grafiğiyle karışıyor (gamma → MIX/COLOR). HDRP'nin
taban renk tonu yalnızca **çarpar**; aynı sonucu vermez. Bu yüzden boyalı
yüzeylerin albedo'su burada, Blender'daki **aynı matematikle** pişirilir:

    1. BC'yi doğrusal uzaya al
    2. gamma:  c = c ^ value_gamma
    3. karışım: MIX  -> (1−f)·c + f·tint
                COLOR -> ton ve doygunluk tint'ten, DEĞER c'den; sonra f ile lerp

AO çarpımı burada **uygulanmaz** — Blender'daki AO çarpımı yalnızca inceleme
render'ı içindir; Unity AO'yu maskenin G kanalında taşır. İki yerde birden
uygulamak girintileri iki kez karartırdı.

Renk yönetimi: kaynaklar `Non-Color` olarak okunur, yani `pixels` dosyadaki ham
değeri verir. Boyalı albedo doğrusal uzayda hesaplanır ve çıkışa sRGB
kodlamasıyla yazılır — Blender'ın gördüğü neyse Unity'nin göreceği o.

Kullanım:
  blender --background --factory-startup --python tools/textures/build_unity_maps.py
"""

import json
import os
import shutil
import sys

import bpy
import numpy as np

_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
for _p in (os.path.join(_ROOT, "blender"), os.path.join(_ROOT, "blender", "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz          # noqa: E402
import materials as mtl          # noqa: E402
import ottoman_kit as kit        # noqa: E402

OUT_DIR = os.path.join("unity", "HezarfenGame", "Assets", "_Project",
                       "Art", "Textures", "Ottoman")


def slug(asset_id):
    """`weathered_planks` → `WeatheredPlanks`. Doku adı KAYNAĞI anlatır."""
    return "".join(part.capitalize() for part in asset_id.split("_"))


# ------------------------------------------------------------------ görüntü

def read_raw(path):
    """Görüntüyü HAM (Non-Color) olarak okur → (h, w, 4) float 0-1, satır 0 = alt."""
    img = bpy.data.images.load(os.path.abspath(path), check_existing=False)
    img.colorspace_settings.name = "Non-Color"
    w, h = img.size
    buf = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(buf)
    bpy.data.images.remove(img)
    return buf.reshape(h, w, 4)


def read_srgb_linear(path):
    """sRGB görüntüyü **doğrusal** uzayda okur (Blender düğüm grafiğiyle aynı)."""
    img = bpy.data.images.load(os.path.abspath(path), check_existing=False)
    img.colorspace_settings.name = "sRGB"
    w, h = img.size
    buf = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(buf)
    bpy.data.images.remove(img)
    return buf.reshape(h, w, 4)


def write(path, arr, srgb):
    """
    (h, w, 4) diziyi PNG olarak yazar.

    `srgb=True` ise dizi DOĞRUSAL kabul edilir ve sRGB kodlanarak yazılır;
    `False` ise ham veri olarak (maske, normal). Bu ayrımı kaçırmak, PBR
    hattındaki en yaygın sessiz hatadır.
    """
    h, w, _ = arr.shape
    img = bpy.data.images.new(os.path.basename(path), width=w, height=h,
                              alpha=True, float_buffer=False)
    img.colorspace_settings.name = "sRGB" if srgb else "Non-Color"
    img.alpha_mode = "CHANNEL_PACKED"
    img.pixels.foreach_set(np.ascontiguousarray(arr, dtype=np.float32).ravel())
    img.file_format = "PNG"
    img.filepath_raw = os.path.abspath(path)
    img.save()
    bpy.data.images.remove(img)


# ------------------------------------------------------------------ karışım

def blend_tint(base_lin, tint, factor, gamma, mode):
    """Blender'daki gamma → MIX/COLOR zincirinin **birebir** karşılığı."""
    c = np.clip(base_lin[..., :3], 0.0, None)
    if abs(gamma - 1.0) > 1e-6:
        c = np.power(c, gamma)                      # ShaderNodeGamma: c ** gamma
    if tint is None or factor <= 0.0:
        return c

    t = np.array(tint, dtype=np.float32)
    if mode == "MIX":
        return (1.0 - factor) * c + factor * t

    if mode == "COLOR":
        # Blender 'Color' karisimi: ton+doygunluk B'den, DEGER A'dan.
        # HSV'de deger = max kanal.
        v_a = c.max(axis=-1, keepdims=True)
        v_b = float(t.max())
        scaled = t[None, None, :] * (v_a / max(v_b, 1e-6))
        return (1.0 - factor) * c + factor * scaled

    raise ValueError(f"bilinmeyen karisim kipi: {mode}")


def build_mask(arm_path):
    """ARM → HDRP maskesi. Kanal yeri ve pürüzlülük tersi burada."""
    a = read_raw(arm_path)
    out = np.zeros_like(a)
    out[..., 0] = a[..., 2]              # R = Metallic   (ARM.B)
    out[..., 1] = a[..., 0]              # G = AO         (ARM.R)
    out[..., 2] = 0.0                    # B = Detay maskesi (kullanilmiyor)
    out[..., 3] = 1.0 - a[..., 1]        # A = Smoothness (1 - ARM.G)
    return out


def verify_mask(arm_path, mask_path):
    """
    Yazılan maskeyi **geri okuyup** kanal eşlemesini doğrular.

    Kanal karışması PBR hattındaki en sinsi hatadır: doku yüklenmiş görünür,
    hiçbir uyarı çıkmaz, ama duvar metalik olur ve mat yüzeyler parlar. Sebebi
    aramak saatler alır. Yazdığını geri okumak bunu saniyede yakalar.

    Tolerans 8-bit niceleme (1/255) artı JPEG→PNG yuvarlaması kadardır.
    """
    a, m = read_raw(arm_path), read_raw(mask_path)
    if a.shape != m.shape:
        raise AssertionError(f"{mask_path}: boyut uyusmuyor {a.shape} != {m.shape}")
    checks = (("R=Metallic(ARM.B)", m[..., 0], a[..., 2]),
              ("G=AO(ARM.R)", m[..., 1], a[..., 0]),
              ("A=1-Rough(ARM.G)", m[..., 3], 1.0 - a[..., 1]))
    errs = []
    for label, got, want in checks:
        err = float(np.abs(got - want).max())
        errs.append(f"{label} {err:.4f}")
        if err > 0.005:
            raise AssertionError(f"{os.path.basename(mask_path)}: {label} "
                                 f"eslesmiyor (en buyuk sapma {err:.4f})")
    if float(np.abs(m[..., 2]).max()) > 0.005:
        raise AssertionError(f"{os.path.basename(mask_path)}: B kanali sifir degil")

    # YANLIS eslemenin gercekten yakalanacagini da olc: R kanalini ARM.G ile
    # kiyasla. Bu fark kucuk cikarsa denetimin ayirt etme gucu yok demektir —
    # "sessizce gecti" ile "dogru" ayni sey degildir.
    decoy = float(np.abs(m[..., 0] - a[..., 1]).max())
    if decoy < 0.05:
        raise AssertionError(f"{os.path.basename(mask_path)}: denetim ayirt "
                             f"edemiyor (yanlis esleme farki yalnizca {decoy:.4f})")
    hz.log(f"    maske denetimi: {', '.join(errs)} | yanlis esleme farki {decoy:.3f}")


# -------------------------------------------------------------------- ana
#: Paletin DISINDA kalan malzemeler.
#:
#: `ottoman_kit.PALETTES` opak yuzeyler icindir ve Unity hatti onu tarar.
#: Sac oraya giremez: alfa kesme ister ve Blender tarafinda ayri dugumlerle
#: kuruluyor (`sac_kit.hair_material`). Palete zorlamak, 30'dan fazla var
#: olan malzemenin harman kipini sac icin riske atmak olurdu.
#:
#: Istisna GORUNUR olsun diye burada, acikca listeleniyor — ozel durum
#: kodun icine gomulmuyor.
EK_MALZEME = [
    dict(name="M_Hair", asset="hair_card",
         root=os.path.join("art", "textures", "generated"),
         roughness=0.46, metallic=0.0,
         alphaClip=True, alphaCutoff=0.45,
         baseColor=[0.12, 0.08, 0.055]),
]



def main():


    hz.log(f"Unity doku hatti -> {OUT_DIR}")
    os.makedirs(OUT_DIR, exist_ok=True)
    written, manifest = [], {}

    for pal_name, pal in kit.PALETTES.items():
        roles = kit.TEXTURE_ROLES.get(pal_name, {})
        for key, (color, rough, mat_name) in pal.items():
            if mat_name in manifest:                # baska palette uretildi
                continue
            role = roles.get(key)
            if role is None:                        # 'shadow' — dokusuz
                manifest[mat_name] = dict(name=mat_name, kind="untextured",
                                          baseColor=[round(c, 5) for c in color[:3]],
                                          roughness=rough)
                continue

            # `root`: prosedurel uretilmis doku Poly Haven klasorunde degil
            # (yaprak — ADR 0019 §3). Rolun kendi kokunu soylemesi, burada
            # ozel durum yazmaktan iyidir.
            meta = mtl.load_meta(role["asset"],
                                 root=role.get("root", mtl.TEXTURE_ROOT))
            if meta is None:
                hz.log(f"UYARI {mat_name}: {role['asset']} indirilmemis, atlandi")
                continue
            d, maps = meta["_dir"], meta.get("maps", {})
            # `metallic`: HDRP maskenin R kanalini `_Metallic` ile CARPAR.
            # Yani `_Metallic = 0` verilirse maskedeki metaliklik tamamen
            # silinir. Kit metalsizken 0 dogruydu; kursun geldiginde ayni satir
            # sessizce yanlis oldu — carpanin 1 olmasi gereken tek malzeme
            # metalikligini hic gostermezdi. Deger artik ROLDEN gelir.
            entry = dict(name=mat_name, kind="pbr", asset=role["asset"],
                         sizeMeters=[float(v) for v in
                                     meta.get("size_meters", (2.0, 2.0))[:2]],
                         roughness=rough,
                         metallic=1.0 if role.get("metallic") else 0.0)

            # Maske ve normal KAYNAK dokuya aittir, role degil.
            #
            # Ilk yazimda ikisi de malzeme adiyla yaziliyordu ve `weathered_planks`
            # dort rolde kullanildigi icin ayni 2K maske ve ayni 2K normal DORT
            # KEZ diske yazildi — olculdu: 85 MB saf tekrar, kitin doku
            # ayakizinin %31'i. Boyali albedo role aittir (her rolun boyasi
            # farkli), maske ve normal degildir.
            asset_slug = slug(role["asset"])

            # --- taban renk ---
            tint = role.get("tint")
            if tint is not None and role.get("tint_factor", 0.0) > 0.0:
                bc_out = os.path.join(OUT_DIR, f"T_{mat_name[2:]}_BC.png")
                if bc_out not in written:
                    lin = read_srgb_linear(os.path.join(d, maps["BC"]))
                    rgb = blend_tint(lin, tint, role["tint_factor"],
                                     role.get("value_gamma", 1.0),
                                     role.get("tint_blend", "COLOR"))
                    out = np.ones_like(lin)
                    out[..., :3] = np.clip(rgb, 0.0, 1.0)
                    write(bc_out, out, srgb=True)
                entry["baked"] = True
            else:
                # Boyasiz rol: kaynak oldugu gibi kopyalanir ve KAYNAK adiyla
                # paylasilir. Yeniden kodlamak kayipli uzerine kayipli bindirmek
                # olurdu.
                #
                # Uzanti KAYNAKTAN alinir, sabit degil. Once ".jpg" yaziliydi ve
                # Poly Haven albedo'lari JPG oldugu icin fark etmiyordu; prosedurel
                # dokular PNG gelince dosyalar ".jpg" adiyla PNG icerigi tasidi.
                # Unity ice aktarmayi uzantiya gore secer — sessizce yanlis bir
                # varsayimdi.
                ext = os.path.splitext(maps["BC"])[1].lower() or ".jpg"
                bc_out = os.path.join(OUT_DIR, f"T_{asset_slug}_BC{ext}")
                if bc_out not in written:
                    shutil.copyfile(os.path.join(d, maps["BC"]), bc_out)
                entry["baked"] = False
            written.append(bc_out)

            # --- maske (kaynak basina bir kez) ---
            if "ARM" in maps:
                arm = os.path.join(d, maps["ARM"])
                mask_out = os.path.join(OUT_DIR, f"T_{asset_slug}_MASK.png")
                if mask_out not in written:
                    write(mask_out, build_mask(arm), srgb=False)
                    verify_mask(arm, mask_out)      # yazdigini geri oku
                    written.append(mask_out)
                entry["maskFile"] = os.path.basename(mask_out)

            # --- normal (kaynak basina bir kez; nor_gl zaten Unity'nin yonu) ---
            if "N" in maps:
                n_out = os.path.join(OUT_DIR, f"T_{asset_slug}_N.png")
                if n_out not in written:
                    shutil.copyfile(os.path.join(d, maps["N"]), n_out)
                    written.append(n_out)
                entry["normalFile"] = os.path.basename(n_out)

            entry["baseColorFile"] = os.path.basename(bc_out)
            manifest[mat_name] = entry
            hz.log(f"  {mat_name:22s} <- {role['asset']}")

    # --- PALET DISI: alfa kesmeli malzemeler --------------------------------
    #
    # HDRP alfayi BASE MAP'IN ALFA KANALINDAN okur; Blender ise ayri dosya
    # ister (BC sRGB, alfa Non-Color — ayni dosya iki renk uzayi tasiyamaz).
    # Iki motor iki bicim istiyor ve dogru yer bu: kaynak ayri kalir, Unity
    # icin BIRLESTIRILIR. Blender'i HDRP'nin bicimine zorlamak alfayi sRGB
    # egrisinden gecirirdi ve tel kenarlari sismanlardi.
    for ek in EK_MALZEME:
        meta = mtl.load_meta(ek["asset"], root=ek["root"])
        if meta is None:
            hz.log(f"UYARI {ek['name']}: {ek['asset']} uretilmemis, atlandi")
            continue
        d, maps = meta["_dir"], meta.get("maps", {})
        s_ad = slug(ek["asset"])
        entry = dict(name=ek["name"], kind="pbr", asset=ek["asset"],
                     sizeMeters=[float(x) for x in meta.get("size_meters",
                                                            [1.0, 1.0])],
                     roughness=ek["roughness"], metallic=ek["metallic"],
                     baked=False, baseColor=ek["baseColor"],
                     alphaClip=bool(ek.get("alphaClip")),
                     alphaCutoff=float(ek.get("alphaCutoff", 0.5)))

        bc_out = os.path.join(OUT_DIR, f"T_{s_ad}_BC.png")
        if bc_out not in written:
            # BC zaten sRGB kodlu; `read_raw` ham okur ve `write(srgb=False)`
            # oldugu gibi geri yazar. Yeniden kodlamak sRGB egrisini iki kez
            # uygulamak olurdu.
            bc = read_raw(os.path.join(d, maps["BC"]))
            if "A" in maps:
                bc[..., 3] = read_raw(os.path.join(d, maps["A"]))[..., 0]
            else:
                bc[..., 3] = 1.0
            write(bc_out, bc, srgb=False)
            written.append(bc_out)
        entry["baseColorFile"] = os.path.basename(bc_out)

        if "ARM" in maps:
            mask_out = os.path.join(OUT_DIR, f"T_{s_ad}_MASK.png")
            if mask_out not in written:
                write(mask_out, build_mask(os.path.join(d, maps["ARM"])),
                      srgb=False)
                written.append(mask_out)
            entry["maskFile"] = os.path.basename(mask_out)

        if "N" in maps:
            n_out = os.path.join(OUT_DIR, f"T_{s_ad}_N.png")
            if n_out not in written:
                shutil.copyfile(os.path.join(d, maps["N"]), n_out)
                written.append(n_out)
            entry["normalFile"] = os.path.basename(n_out)

        manifest[ek["name"]] = entry
        hz.log(f"  {ek['name']:22s} <- {ek['asset']} (alfa kesme)")

    # Unity tarafi bu bildirimden malzeme uretir; ad-dosya eslesmesi TEK yerde
    # yasar, iki tarafta elle tekrarlanmaz. Bicim `JsonUtility`nin okuyabildigi
    # gibi: sozluk degil, kok nesne altinda LISTE (JsonUtility sozluk okumaz).
    mpath = os.path.join(OUT_DIR, "materials.json")
    payload = {"materials": [manifest[k] for k in sorted(manifest)]}
    with open(mpath, "w", encoding="utf-8") as fh:
        json.dump(payload, fh, ensure_ascii=False, indent=1)

    # Artik dosyalari temizle: adlandirma degistiginde eski dokular klasorde
    # kalirsa Unity onlari import etmeye devam eder ve bellekte tasir — silinen
    # bir malzemenin dokusu sessizce hayatta kalir. Cikti klasoru URETILMIS'tir,
    # burada elle konan dosya olmamali.
    keep = {os.path.basename(p) for p in written} | {"materials.json"}
    removed = 0
    for f in os.listdir(OUT_DIR):
        if f.endswith(".meta") or f in keep:
            continue                 # .meta'yi Unity kendi temizler
        os.remove(os.path.join(OUT_DIR, f))
        removed += 1
    if removed:
        hz.log(f"{removed} artik doku silindi (adlandirma degisti)")

    total = sum(os.path.getsize(os.path.join(OUT_DIR, os.path.basename(p)))
                for p in set(written))
    hz.log(f"{len(set(written))} benzersiz dosya ({total / 1e6:.1f} MB), "
           f"{len(payload['materials'])} malzeme bildirimi: {mpath}")


if __name__ == "__main__":
    main()
