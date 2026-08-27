"""
Hezarfen: 1632 — Doğancılar Meydanı yapıları (Faz 3, S-kademe).

**Hezarfen'in iniş noktası.** Meydanın 1632'de ayakta olan iki yapısı ve
Hüdâyî'nin mezarı. Ölçü ve gerekçeler: RESEARCH.md §5.5, ADR 0037.

## Ölçü YOK — ve uydurulmadı

Her iki caminin de 1632 hâlinin ölçülü çizimi yoktur; bugünkü Doğancılar
Camii büyük ölçüde **1857** onarımıdır ve Hüdâyî Külliyesi **1850** yangını
sonrası **1855-56**'da baştan inşa edilmiştir. Bu yüzden burada üretilen
kütleler **D3 / `status: draft`** taşır ve boyutlar birer **tipolojik
varsayılan**dır, ölçüm değil.

Kaynağın kesin söylediği tek biçim niteliği kullanılıyor: Doğancılar
Camii'nin duvarları **kâgir**, çatısı **ahşap**tır ve **tek minaresi**
vardır. Bu, `mosque_kit`'in zaten modellediği tipolojidir; yeni bir kütle
uydurmak yerine o kit kullanıldı.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_dogancilar.py -- \
      --textured --out-dir unity/HezarfenGame/Assets/_Import
"""

import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz            # noqa: E402
import mahalle_kit as mak          # noqa: E402
import mosque_kit as mk            # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SRC_CAMII = (
    "Cakircibasi Hasan Pasa Camii (DOGANCILAR CAMII), Uskudar Dogancilar "
    "Meydani. 1548'de MIMAR SINAN yapti; banisi Cakircibasi Hasan Pasa'dir "
    "ve ad, onun cakircibasilik gorevinden gelir. 1580'lerde harap dusunce "
    "HACI AHMED PASA yeniden yaptirdi ve avluya kendi turbesini de "
    "yaptirdi — yani 1632'de ayakta olan sey 1580'ler yapisidir. "
    "BICIM (kaynagin kesin soyledigi): duvarlari KAGIR, catisi AHSAP; TEK "
    "minare; dikdortgen plan; siyah-beyaz mermer kemerli tas kapi. "
    "1632'DE YOK: 1858-59 Sayeste Kadinefendi onarimi ve 1857 duzenlemesi — "
    "bugunku yapinin dikdortgen plani 1857'de esasli bicimde degistirildi. "
    "OLCU YOK: 1632 halinin olculu cizimi bulunmuyor; kutle **D3** ve "
    "olculer TIPOLOJIK VARSAYILANDIR, olcum degil. RESEARCH.md 5.5"
)

SRC_TEKKE = (
    "Aziz Mahmud Hudayi tekke-camii, Uskudar Dogancilar (Ahmet Celebi "
    "mahallesi). Hudayi arsayi 1589'da satin aldi ve ayni yil insaata "
    "basladi; ilk tekke 1003/1595'te tamamlandi, 1007/1598-99'da bizzat "
    "banisi MINBER ekleterek camiye cevirdi. 1632'de yapi 37 yasindadir ve "
    "IV. Murad doneminin en etkili seyh tekkesidir. "
    "BICIM: ahsap catili. "
    "1632'DE YOK: 1850 Uskudar Carsisi yangini sonrasi 1272/1855-56'da "
    "Sultan Abdulmecid'in bastan insa ettirdigi bugunku kulliye; yanginda "
    "TURBE DISINDA butun binalar ortadan kalkmisti. "
    "OLCU YOK: kutle **D3**, olculer tipolojik varsayilan. RESEARCH.md 5.5"
)

SRC_TURBE = (
    "Aziz Mahmud Hudayi turbesi — **ACIK (baldaken) turbe**, Uskudar "
    "Dogancilar. Hudayi Safer 1038 (Ekim 1628) vefat etti ve vasiyeti "
    "uzerine dergahinin bahcesine gomuldu. **TURBE 1038'DE (1628-29) "
    "YAPILDI** — olumunden aylar sonra, ayni hicri yil icinde; 1632'de yapi "
    "UC-DORT YASINDADIR (Kultur Envanteri, 'Aziz Mahmud Hudayi Turbesi'). "
    "BICIM: TDV, 1850 yangini oncesi ayakta kalan yapiyi ACIK turbe diye "
    "tanimlar (yanginda 'Hudayi Turbesi disinda kalan binalar ortadan "
    "kalkmisti'). Bugunku kubbe DORT MERMER SUTUN uzerine oturur ve o "
    "baldaken cekirdek, kapatilmadan onceki halin izidir; model bu yuzden "
    "dort ayaklidir. "
    "1632'DE YOK: bugunku KAPALI kagir kabuk, 7,40x8,80 m plan, on uc "
    "dilimli kubbe ve yedi pencere — hepsi 1272/1855-56'da Sultan "
    "Abdulmecid'in yeniden insasidir. "
    "OLCU YOK: 1632 halinin olculu cizimi bulunmuyor; oranlar **D3**. "
    "RESEARCH.md 5.5, ADR 0037"
)


def add_args(p):
    p.add_argument("--palette", default="default")
    p.add_argument("--textured", action="store_true")
    p.add_argument("--out-dir", default=None,
                   help="FBX'lerin yazilacagi dizin (_Import)")
    p.add_argument("--blend-dir", default=os.path.join("art", "blend", "landmark"))
    p.add_argument("--catalog", default=os.path.join("art", "blend", "landmark",
                                                     "catalog.json"))
    return p


def _write(cat_path, info):
    os.makedirs(os.path.dirname(os.path.abspath(cat_path)), exist_ok=True)
    cat = {"variants": []}
    if os.path.exists(cat_path):
        with open(cat_path, encoding="utf-8") as fh:
            cat = json.load(fh)
    rest = [v for v in cat.get("variants", []) if v.get("name") != info["name"]]
    rest.append(info)
    rest.sort(key=lambda v: v["name"])
    with open(cat_path, "w", encoding="utf-8") as fh:
        json.dump({"variants": rest}, fh, ensure_ascii=False, indent=1)
    return len(rest)


def _finish(a, asset, info, tier, source, kind):
    info.update(name=asset, prefab=f"PF_{asset}", tier=tier, source=source,
                kind=kind, status="draft", accuracy="D3")
    n = _write(a.catalog, info)
    hz.log(f"katalog: {a.catalog} ({n} kayit)")
    if a.blend_dir:
        os.makedirs(a.blend_dir, exist_ok=True)
        hz.save_blend(os.path.join(a.blend_dir, f"{asset}.blend"))
    if a.out_dir:
        os.makedirs(a.out_dir, exist_ok=True)
        export_fbx(os.path.join(a.out_dir, f"SM_{asset}.fbx"),
                   collection_name=COLLECTION)


def build_camii(a):
    """Çakırcıbaşı Hasan Paşa (Doğancılar) Camii — kâgir duvar, AHŞAP çatı."""
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    # TIPOLOJIK VARSAYILAN, olcum degil: bir cakircibasi (saray gorevlisi,
    # pasa rutbesi) camisi mahalle mescidinden buyuk, selatin camisinden
    # kucuktur. Yuvarlak sayilar bilerek: ondalikli bir sayi burada olmayan
    # bir kesinlik iddia ederdi.
    p = mk.MescitParams(hall=13.0, wall_h=7.0, plinth=0.9,
                        roof="timber", roof_pitch_deg=26.0, eave=1.0,
                        portico=True, portico_depth=3.6, portico_bays=5,
                        minaret=True, minaret_h=26.0, minaret_side=-1,
                        portico_material="stone",
                        wall_thickness=0.75, palette=a.palette)
    lod0, lod1, lod2, ucx, info = mk.build_mescit(p, col, "DogancilarCamii",
                                                  textured=a.textured)
    if info["roof"] != "timber":
        raise SystemExit("[HZ] HATA: Dogancilar Camii'nin catisi AHSAPTIR "
                         "(kaynak: 'duvarlari kargir catisi ahsaptir'); "
                         "kubbe koymak yapiyi baska bir cami yapar")
    hz.log(f"DogancilarCamii: harim {info['hall']:.1f} m, ortu={info['roof']}, "
           f"minare {info['minaret_h']:.1f} m, yukseklik {info['height']:.2f} m")
    _finish(a, "DogancilarCamii", info, "T1", SRC_CAMII, "cami")


def build_tekke(a):
    """Aziz Mahmud Hüdâyî tekke-camii (1595; minber 1598-99)."""
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    # Tekke camisi Dogancilar Camii'nden KUCUK kurulur: biri bir sarayli
    # pasanin cami vakfi, oteki bir seyhin kendi arsasina kendi yaptirdigi
    # tekkedir. Ikisini esitlemek, 1632'nin toplumsal olcegini silerdi.
    p = mk.MescitParams(hall=11.0, wall_h=6.0, plinth=0.8,
                        roof="timber", roof_pitch_deg=28.0, eave=1.0,
                        portico=True, portico_depth=3.0, portico_bays=3,
                        minaret=True, minaret_h=21.0, minaret_side=1,
                        wall_thickness=0.65, palette=a.palette)
    lod0, lod1, lod2, ucx, info = mk.build_mescit(p, col, "HudayiTekkesi",
                                                  textured=a.textured)
    hz.log(f"HudayiTekkesi: harim {info['hall']:.1f} m, ortu={info['roof']}, "
           f"minare {info['minaret_h']:.1f} m, yukseklik {info['height']:.2f} m")
    _finish(a, "HudayiTekkesi", info, "T1", SRC_TEKKE, "cami")


def build_turbe(a):
    """Hüdâyî türbesi — AÇIK (baldaken) türbe, tarihi belirsiz."""
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    # DORT ayak: bugunku (1855-56) yapinin kubbesi "dort mermer sutun
    # uzerine oturur" ve o baldaken cekirdek, kapatilmadan onceki halin
    # izidir. Oranlar D3 — 1632 halinin olculu cizimi yok.
    p = mak.TurbeParams(sides=4, apothem=1.95, wall_h=3.40, dome_h=1.85,
                        acik=True, palette=a.palette)
    lod0, lod1, ucx, info = mak.build_turbe(p, col, "HudayiTurbesi",
                                            textured=a.textured)
    # DENETIM BAYRAGA DEGIL YAPIYA BAKAR.
    #
    # Onceki yazimda burasi `if not p.acik: raise` idi ve gecti — ama
    # `acik` bayragi kitte HIC KULLANILMIYORDU: kapali bir turbe kurulup
    # "acik" diye kataloglaniyordu. Bayragin degerini sinamak, bayragin
    # okundugunu varsaymaktir. Artik uretilen seyin kendisi soruluyor.
    if not info.get("acik") or info.get("walls", True):
        raise SystemExit("[HZ] HATA: uretilen turbe KAPALI. TDV yangin "
                         "oncesi yapiyi ACIK turbe diye tanimlar.")
    if info.get("columns") != 4:
        raise SystemExit(f"[HZ] HATA: {info.get('columns')} sutun — kaynak "
                         "'dort mermer sutun' der.")
    hz.log(f"HudayiTurbesi: ACIK turbe, {info['columns']} sutun, "
           f"ayak izi {info['footprint_x']:.2f} m, "
           f"yukseklik {info['height']:.2f} m")

    # KATALOGA GIRIYOR — belirsizlik KAYNAKLA cozuldu.
    #
    # Bir tur once burada su duruyordu: "1632'de varligi belgeli degil,
    # onay bekliyor". Sonra kaynaga dogru soru soruldu ve cevap cikti:
    # turbe 1038'de (1628-29) yapilmis, yani Hudayi'nin olumunden aylar
    # sonra. 1632'de yapi ayakta ve uc-dort yasinda.
    #
    # Tier artik T1: VARLIGI belgeli. Belirsiz olan yalnizca BICIMDIR ve
    # onu accuracy=D3 ile status=draft tasiyor — ikisini karistirmamak
    # katalogun butun sozlesmesi.
    _finish(a, "HudayiTurbesi", info, "T1", SRC_TURBE, "turbe")


def main():
    parser = add_args(hz.base_parser(__doc__))
    a = parser.parse_args(hz.argv_after_dashes())
    build_camii(a)
    build_tekke(a)
    build_turbe(a)
    hz.log("gen_dogancilar OK")


if __name__ == "__main__":
    main()
