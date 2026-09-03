"""
Hezarfen: 1632 — İnceleme paketi üreticisi (plan Görev 8).

Bu script, "üretimin tamamen Claude'da olmasını gerçekçi kılan teknik"in aletidir
(plan Bölüm 4): çıktımı GÖREREK yineleyebilmem için tek komutla standart, karşılaştırılabilir
görüntüler üretir. Caner'in yazılı notu da bu paketlere dayanır.

Üretilenler — `renders/review/<varlık>_vN/`:
  01_front / 02_right / 03_back / 04_left   dört dik açı (siluet ve oran için)
  05_hero                                   3/4 kahraman açı
  06_detail_upper / 07_detail_base          yakın planlar (cumba, saçak, subasman)
  08_top                                    ayak izi
  contact_sheet.png                         hepsi tek PNG (inceleme bunun üstünden yürür)
  info.md                                   ölçüler, üçgen sayısı, üretim komutu

Tasarım kararları:
  * **1,70 m insan figürü** her karede durur. Mimari bir oyunda tek başına en yararlı
    inceleme öğesi budur: "cumba çok derin" yargısı ancak bir insana göre verilebilir.
  * **LOD1+ ve çarpıştırıcılar (`UCX_`, `UCXB_`) gizlenir.** Aynı konumda üst üste duran LOD'lar z-fighting üretir;
    kalibre edilmemiş bir görüntü üzerinde alınan her karar yanlıştır.
  * **Işık nihai ışık değildir.** Amaç güzellik değil BİÇİMİ OKUTMAK; bu yüzden sabit,
    nötr bir stüdyo düzeni kullanılır ve sürümler arası kıyaslanabilir kalır.
  * Sürüm numarası otomatik artar — geri bildirim döngüsü vN → vN+1 üzerine kurulu.

Kullanım:
  blender --background --factory-startup --python tools/blender/render_preview.py -- \
      --in art/blend/SM_BoxHouse.blend --asset BoxHouse
  blender --background --factory-startup --python tools/blender/render_preview.py -- \
      --in unity/HezarfenGame/Assets/_Project/Art/Models/SM_BoxHouse.fbx --out renders/review/BoxHouse_v2
"""

import math
import os
import re
import sys

import bpy
import numpy as np
from mathutils import Vector

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz   # noqa: E402

REVIEW_ROOT = "renders/review"
HUMAN_HEIGHT = 1.70          # m — inceleme olceginin referansi

# (dosya adi, azimut derece, yukselis derece, odak bolgesi, cerceve payi)
# odak: "all" tum kutle, "upper" ust ucte bir, "base" alt ucte bir
VIEWS = [
    ("01_front",        0.0,   8.0, "all",   1.06),
    ("02_right",       90.0,   8.0, "all",   1.06),
    ("03_back",       180.0,   8.0, "all",   1.06),
    ("04_left",       270.0,   8.0, "all",   1.06),
    ("05_hero",        35.0,  22.0, "all",   1.10),
    # Yakin planlar KOSE'den bakar: duz cepheye dik bakinca cikma derinligi
    # okunmaz, siluet duzlesir. Cumba/sacak ancak koseden goze carpar.
    ("06_detail_upper", 42.0,  10.0, "upper", 1.12),
    ("07_detail_base", -42.0,   4.0, "base",  1.12),
    # Tepe gorunumu azimut 0'da tutulur: yuksek yukselis + azimut birlesince
    # kamera kendi ekseninde yalpalar ve ayak izi egri okunur.
    ("08_top",           0.0,  68.0, "all",   1.12),
]

# Yaya seviyesi inceleme kipi (`--eye`).
#
# Yörünge kameraları varlığı bir MÜZE NESNESİ gibi gösterir: hep dışarıdan, hep
# tam kadraja sığmış. Oyuncu evi öyle görmez — 1,65 m'den, 2-5 m mesafeden,
# saçağa yukarı bakarak görür. Yakın plan detayının işe yarayıp yaramadığı
# ancak bu kadrajda anlaşılır; ölçüsü de budur.
#
# (dosya adi, azimut, yatay mesafe m, bakilan yukseklik m, odak uzakligi mm)
EYE_HEIGHT = 1.65
EYE_VIEWS = [
    # 32 mm: oyun kamerasinin genis gorus acisina yakin. Yurume hissi.
    ("01_sokak_kapi",   0.0,  4.5, 1.70, 32.0),
    ("02_sokak_gecis", 30.0,  3.4, 2.00, 32.0),
    # Sacaga YUKARI bakis: genis sacagin alti yaya gozunun gordugu en buyuk
    # tek yuzeydir. Mertekler orada okunur ya da okunmaz.
    ("03_sacak_alti",  18.0,  3.0, 5.40, 32.0),
    # 55 mm yakin planlar: sove derinligi, denizlik, esik.
    ("04_pencere",      6.0,  2.1, 2.00, 55.0),
    ("05_kapi_esik",    0.0,  2.0, 1.10, 55.0),
    ("06_kose",        58.0,  5.0, 2.60, 32.0),
    ("07_yan_cephe",   90.0,  3.6, 2.20, 32.0),
    ("08_cumba_alti",  24.0,  2.6, 3.60, 32.0),
]

SHEET_COLS = 4


# --------------------------------------------------------------------- girdi

def load_input(path, lod=0):
    """`.blend` açar ya da `.fbx` içe alır. Dönüş: kaynak açıklaması."""
    path = os.path.abspath(path)
    if not os.path.exists(path):
        raise SystemExit(f"[HZ] HATA: girdi yok: {path}")

    ext = os.path.splitext(path)[1].lower()
    if ext == ".blend":
        bpy.ops.wm.open_mainfile(filepath=path)
        hz.ensure_units()
    elif ext == ".fbx":
        hz.reset_scene()
        bpy.ops.import_scene.fbx(filepath=path)
    else:
        raise SystemExit(f"[HZ] HATA: desteklenmeyen girdi: {ext} (.blend veya .fbx)")

    hz.log(f"loaded: {path}")
    return path


def collect_targets(lod=0):
    """
    Render edilecek mesh'ler. LOD ayıklaması ZORUNLU: LOD0 ve LOD1 aynı konumda
    durur, ikisi birden render edilirse yüzeyler birbirine girer (z-fighting) ve
    ortaya çıkan görüntüye bakarak alınan her biçim kararı yanlış olur.
    """
    keep, hidden = [], []
    lod_re = re.compile(r"_LOD(\d+)$", re.IGNORECASE)

    for obj in list(bpy.context.scene.objects):
        if obj.type != "MESH":
            continue
        name = obj.name
        m = lod_re.search(name)

        # CARPISTIRICININ HER TURU GIZLENIR — YALNIZ `UCX_` DEGIL.
        #
        # Burada yalniz `UCX_` eleniyordu ve olculdu: Galata Kulesi'nin
        # inceleme karesinde kursun kulahin yerinde DUZ TEPELI BEYAZ
        # BIR SILINDIR vardi. Kulah yerindeydi (koni, 8,5 m, tepesi
        # 46,00 m'de); goruleni ureten sey `UCXB_GalataKulesi` —
        # carpistiricinin kendisi, modelin ustune cizilmis.
        #
        # `UCXB_` sonradan geldi (ici bos, disbukey DEGIL; ev ve kule
        # kademelerinde gerekti) ve bu alet guncellenmedi. Yani
        # "render bir gozlemdir" derken gozlemin kendisi yanlisti ve
        # her `UCXB_` tasiyan varlik bu kusurla incelendi.
        #
        # Desen Unity tarafindaki iniş sozlesmesiyle ayni: `UCX` ile
        # baslayan ve `_` ile devam eden her onek carpistiricidir.
        if re.match(r"^UCX[A-Z]*_", name):
            hidden.append(name)
        elif m is not None and int(m.group(1)) != lod:
            hidden.append(name)
        else:
            keep.append(obj)

    for obj in bpy.context.scene.objects:
        if obj.type == "MESH" and obj.name in hidden:
            obj.hide_render = True
            obj.hide_viewport = True

    if not keep:
        raise SystemExit("[HZ] HATA: render edilecek mesh yok.")

    hz.log(f"targets: {[o.name for o in keep]}")
    if hidden:
        hz.log(f"hidden (LOD/collider): {hidden}")
    return keep


def world_bounds(objects):
    lo = Vector((float("inf"),) * 3)
    hi = Vector((float("-inf"),) * 3)
    for obj in objects:
        for corner in obj.bound_box:
            p = obj.matrix_world @ Vector(corner)
            lo = Vector((min(lo[i], p[i]) for i in range(3)))
            hi = Vector((max(hi[i], p[i]) for i in range(3)))
    return lo, hi


# ------------------------------------------------------------------- stüdyo

HDRI_DIR = os.path.join("art", "textures", "hdri")


def _apply_hdri(hdri_path, strength=1.0, rotation_deg=-35.0):
    """
    Dünyayı HDRI ile aydınlatır (gerçekçi kip).

    **Nötr kip bozulmaz.** ADR 0006'nın stüdyo aydınlatması oranları yargılamak
    içindir ve bilerek yansız/düz tutulur. Bu kip ayrı bir soru içindir:
    *malzeme gerçek gökyüzü altında doğru okunuyor mu?* PBR bir malzeme, gerçek
    ortam ışığı olmadan değerlendirilemez — düz gri bir dünyada her şey
    plastikleşir ve kusur da erdem de görünmez olur.

    Dönüş: HDRI kullanıldıysa True.
    """
    path = hdri_path if os.path.isabs(hdri_path) else os.path.abspath(hdri_path)
    if not os.path.exists(path):
        hz.log(f"UYARI HDRI yok: {hdri_path} — notr studyo aydinlatmasi kullanilacak. "
               f"Once: python tools/textures/fetch_polyhaven.py --skip-textures --hdris")
        return False

    world = bpy.data.worlds.get("ReviewHDRI") or bpy.data.worlds.new("ReviewHDRI")
    bpy.context.scene.world = world
    world.use_nodes = True
    nt = world.node_tree
    nt.nodes.clear()

    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    env = nt.nodes.new("ShaderNodeTexEnvironment")
    mapping = nt.nodes.new("ShaderNodeMapping")
    tex_co = nt.nodes.new("ShaderNodeTexCoord")

    env.image = bpy.data.images.load(path, check_existing=True)
    bg.inputs["Strength"].default_value = strength
    # Gunesin yonu HDRI'nin icindedir; kadraja gore dondurmek, cepheye dusen
    # isigi ayarlamanin tek yoludur.
    mapping.inputs["Rotation"].default_value[2] = math.radians(rotation_deg)

    nt.links.new(mapping.inputs["Vector"], tex_co.outputs["Generated"])
    nt.links.new(env.inputs["Vector"], mapping.outputs["Vector"])
    nt.links.new(bg.inputs["Color"], env.outputs["Color"])
    nt.links.new(out.inputs["Surface"], bg.outputs["Background"])

    hz.log(f"HDRI aydinlatma: {os.path.basename(path)} (guc {strength}, "
           f"donus {rotation_deg:.0f} derece)")
    return True


def build_studio(lo, hi, with_human=True, hdri=None, hdri_strength=1.0):
    """Nötr, sürümler arası sabit inceleme stüdyosu."""
    size = hi - lo
    center = (lo + hi) * 0.5
    diag = size.length
    span = max(size.x, size.y, 1.0)

    # Zemin: temas noktasini ve golgeyi okutur; olcek hissinin yarisi buradan gelir.
    # Cok genis tutulur — kucuk bir tabla, yan acilarda kadraji kesen bir kenar
    # cizgisi birakir ve goruntu "zeminde duran bina" degil "masada duran maket"
    # gibi okunur.
    ground = hz.make_box("REF_Ground", (diag * 60.0, diag * 60.0, 0.02),
                         (center.x, center.y, lo.z - 0.01))
    hz.assign(ground, hz.make_material("M_Review_Ground", (0.30, 0.30, 0.31), roughness=0.95))

    use_hdri = _apply_hdri(hdri, hdri_strength) if hdri else False
    if use_hdri:
        # HDRI kipinde zemin NOTR bir gri kalir ama isik tamamen gokyuzunden
        # gelir; ek isik eklenmez. Yapay dolgu eklemek, HDRI'nin dogru okunan
        # golge/yansima dengesini bozar — gercekcilik tam da oradan gelir.
        if with_human:
            _build_human(lo, hi)
        return

    # Anahtar isik: bicimi okutan sert-orta golge. Hafif sicak.
    sun = bpy.data.objects.new("REF_KeySun", bpy.data.lights.new("REF_KeySun", "SUN"))
    bpy.context.scene.collection.objects.link(sun)
    sun.data.energy = 3.2
    sun.data.angle = math.radians(2.5)
    sun.data.color = (1.0, 0.96, 0.90)
    _aim_from_spherical(sun, center, diag * 3.0, azimuth=-38.0, elevation=52.0)

    # Dolgu: golgeleri ACAR. Guclu olmasi sart — cumba/sacak golgesindeki beyaz
    # siva, bastirilirsa arka plan degerine duser ve kutlede olmayan bir BOSLUK
    # gorunur. (Bu tam olarak yasandi: v2 paketinde ust kat ile alt kat arasinda
    # sahte bir bosluk okundu; kusur geometride degil aydinlatmadaydi.)
    fill_data = bpy.data.lights.new("REF_Fill", "AREA")
    fill_data.size = span * 3.0
    fill_data.energy = 140.0 * max(1.0, span * span * 0.12)
    fill_data.color = (0.84, 0.90, 1.0)
    fill = bpy.data.objects.new("REF_Fill", fill_data)
    bpy.context.scene.collection.objects.link(fill)
    _aim_from_spherical(fill, center, diag * 2.0, azimuth=145.0, elevation=28.0)

    # On dolgu: kameraya bakan golgeli yuzeyleri okunur tutar.
    front_data = bpy.data.lights.new("REF_FrontFill", "AREA")
    front_data.size = span * 4.0
    front_data.energy = 60.0 * max(1.0, span * span * 0.12)
    front_data.color = (0.95, 0.95, 0.98)
    front = bpy.data.objects.new("REF_FrontFill", front_data)
    bpy.context.scene.collection.objects.link(front)
    _aim_from_spherical(front, center, diag * 2.5, azimuth=25.0, elevation=12.0)

    # Ortam/arka plan: KOYU. Varlik paleti acik (kirec badana, kiremit) oldugu
    # icin siluet ancak koyu bir fon uzerinde guvenle okunur.
    world = bpy.data.worlds.get("Review") or bpy.data.worlds.new("Review")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg is not None:
        bg.inputs["Color"].default_value = (0.055, 0.062, 0.078, 1.0)
        bg.inputs["Strength"].default_value = 1.0

    if with_human:
        _build_human(lo, hi)


def _build_human(lo, hi):
    """
    1,70 m blok figür. Ayrıntı bilinçli olarak yok: figür incelenecek şey değil,
    incelemenin CETVELİ. Yapının sağ ön köşesinin yanında, sokak hizasında durur.
    """
    x = hi.x + 0.9
    y = lo.y - 0.5
    z = lo.z

    parts = [
        ("REF_Human_Legs",  (0.36, 0.22, 0.86), (x, y, z + 0.43)),
        ("REF_Human_Torso", (0.44, 0.25, 0.56), (x, y, z + 0.86 + 0.28)),
        ("REF_Human_Head",  (0.20, 0.20, 0.24), (x, y, z + 0.86 + 0.56 + 0.14)),
    ]
    mat = hz.make_material("M_Review_Human", (0.08, 0.09, 0.11), roughness=0.9)
    for name, size, center in parts:
        hz.assign(hz.make_box(name, size, center), mat)

    hz.log(f"human scale figure at ({x:.2f}, {y:.2f}) height {HUMAN_HEIGHT} m")


def _aim_from_spherical(obj, target, distance, azimuth, elevation):
    """Nesneyi hedefin etrafındaki küresel bir noktaya koyup hedefe baktırır."""
    az = math.radians(azimuth)
    el = math.radians(elevation)
    offset = Vector((
        math.sin(az) * math.cos(el),
        -math.cos(az) * math.cos(el),      # azimut 0 = -Y = "on cephe"
        math.sin(el),
    )) * distance
    obj.location = target + offset
    obj.rotation_euler = (-offset).to_track_quat("-Z", "Y").to_euler()


# -------------------------------------------------------------------- kamera

def make_camera(lens_mm=55.0):
    cam_data = bpy.data.cameras.new("REF_Camera")
    cam_data.lens = lens_mm                 # 55 mm: perspektif abartisi az, oran durust
    cam_data.sensor_fit = "AUTO"
    cam = bpy.data.objects.new("REF_Camera", cam_data)
    bpy.context.scene.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    return cam


def frame_view(cam, lo, hi, azimuth, elevation, focus, margin):
    """Kamerayı, istenen odak bölgesini kadraja sığdıracak mesafeye yerleştirir."""
    size = hi - lo
    center = (lo + hi) * 0.5

    if focus == "upper":
        center = Vector((center.x, center.y, lo.z + size.z * 0.72))
        radius = size.length * 0.38
    elif focus == "base":
        center = Vector((center.x, center.y, lo.z + size.z * 0.18))
        radius = size.length * 0.38
    else:
        radius = size.length * 0.5

    half_fov = 0.5 * cam.data.angle
    distance = (radius / max(math.sin(half_fov), 1e-4)) * margin
    _aim_from_spherical(cam, center, distance, azimuth, elevation)


def eye_view(cam, lo, hi, azimuth, distance, target_z, lens_mm):
    """
    Yaya kadrajı: kamera **cephe yüzeyinden** `distance` metre, 1,65 m'de.

    Mesafe kütle merkezinden değil dış yüzeyden ölçülür; yoksa geniş bir ev ile
    dar bir ev aynı "3 m"de bambaşka uzaklıkta görünür ve kadrajlar
    kıyaslanamaz hâle gelir. Kadraj varlığa **sığdırılmaz** — nokta zaten bu:
    yakından bakınca ev kadrajı taşmalı.
    """
    cam.data.lens = lens_mm
    center = (lo + hi) * 0.5
    az = math.radians(azimuth)
    dir_xy = Vector((math.sin(az), -math.cos(az), 0.0))

    # Kutunun o yöndeki dış yüzeyine olan yaklaşık mesafe.
    half = (hi - lo) * 0.5
    surface = abs(dir_xy.x) * half.x + abs(dir_xy.y) * half.y

    eye = Vector((center.x, center.y, lo.z + EYE_HEIGHT)) + dir_xy * (surface + distance)
    target = Vector((center.x, center.y, lo.z + target_z))
    cam.location = eye
    cam.rotation_euler = (target - eye).to_track_quat("-Z", "Y").to_euler()


# -------------------------------------------------------------------- render

def setup_render(resolution, samples, note):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.taa_render_samples = samples
    if hasattr(scene.eevee, "use_shadows"):
        scene.eevee.use_shadows = True

    r = scene.render
    r.resolution_x = resolution
    r.resolution_y = resolution          # kare kare: kontak sayfasi duzgun dizilir
    r.resolution_percentage = 100
    r.image_settings.file_format = "PNG"
    r.image_settings.color_mode = "RGBA"
    r.film_transparent = False

    # Damga: olculer goruntunun UZERINDE dursun. Ayri bir dosyaya bakmak zorunda
    # kalmak, inceleme sirasinda yanlis varsayimla yorum yazmaya yol acar.
    r.use_stamp = True
    r.use_stamp_note = True
    r.stamp_note_text = note
    r.use_stamp_labels = False
    r.use_stamp_date = False
    r.use_stamp_time = False
    r.use_stamp_frame = False
    r.use_stamp_render_time = False
    r.use_stamp_scene = False
    r.use_stamp_camera = False
    r.use_stamp_filename = False
    r.stamp_font_size = max(14, resolution // 55)
    r.stamp_foreground = (1.0, 1.0, 1.0, 1.0)
    r.stamp_background = (0.0, 0.0, 0.0, 0.65)


def render_to(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    hz.log(f"rendered: {os.path.basename(path)}")
    return path


# ------------------------------------------------------------- kontak sayfası

def _read_image(path, tile):
    """
    PNG → (tile, tile, 4) float dizi. Renk uzayı 'Non-Color' seçilir: amaç
    kompozisyon, renk dönüşümü değil. Aksi halde yükle-kaydet turunda gama iki
    kez uygulanır ve kontak sayfası kaynaklarından farklı görünür.
    """
    img = bpy.data.images.load(path, check_existing=False)
    img.colorspace_settings.name = "Non-Color"
    if tuple(img.size) != (tile, tile):
        img.scale(tile, tile)

    buf = np.empty(tile * tile * 4, dtype=np.float32)
    img.pixels.foreach_get(buf)
    bpy.data.images.remove(img)
    return buf.reshape(tile, tile, 4)


def contact_sheet(image_paths, out_path, tile=900, cols=SHEET_COLS, refs=None):
    """Tüm kareleri tek PNG'de toplar. İnceleme bu dosya üzerinden yürür."""
    refs = refs or []
    items = list(image_paths) + list(refs)
    rows = math.ceil(len(items) / cols)

    sheet = np.zeros((rows * tile, cols * tile, 4), dtype=np.float32)
    sheet[..., 3] = 1.0

    for i, path in enumerate(items):
        r, c = divmod(i, cols)
        # Blender pikselleri ALTTAN USTE dizer; ilk goruntu ust satira gitsin
        # diye satir indeksini ters ceviriyoruz.
        top = (rows - 1 - r) * tile
        sheet[top:top + tile, c * tile:(c + 1) * tile, :] = _read_image(path, tile)

    out = bpy.data.images.new("ContactSheet", cols * tile, rows * tile,
                              alpha=True, float_buffer=True)
    out.colorspace_settings.name = "Non-Color"
    out.pixels.foreach_set(sheet.reshape(-1))
    out.file_format = "PNG"
    out.filepath_raw = os.path.abspath(out_path)
    out.save()
    bpy.data.images.remove(out)

    hz.log(f"contact sheet: {out_path} ({cols}x{rows} @ {tile}px)")
    return out_path


# --------------------------------------------------------------------- sürüm

def next_version_dir(asset):
    """`renders/review/<asset>_vN/` — en yüksek N'yi bulup bir artırır."""
    root = os.path.abspath(REVIEW_ROOT)
    os.makedirs(root, exist_ok=True)
    pattern = re.compile(rf"^{re.escape(asset)}_v(\d+)$", re.IGNORECASE)

    highest = 0
    for entry in os.listdir(root):
        m = pattern.match(entry)
        if m:
            highest = max(highest, int(m.group(1)))

    return os.path.join(root, f"{asset}_v{highest + 1}")


def write_info(path, asset, version_dir, src, lo, hi, targets, cmd):
    size = hi - lo
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in targets)

    lines = [
        f"# İnceleme paketi — {asset}",
        "",
        f"- **Kaynak:** `{os.path.relpath(src, os.getcwd())}`",
        f"- **Paket:** `{os.path.relpath(version_dir, os.getcwd())}`",
        "",
        "## Ölçüler",
        "",
        "| Ölçü | Değer |",
        "|---|---|",
        f"| Genişlik (X) | {size.x:.3f} m |",
        f"| Derinlik (Y) | {size.y:.3f} m |",
        f"| Yükseklik (Z) | {size.z:.3f} m |",
        f"| Taban yüksekliği | {lo.z:.3f} m |",
        f"| Üçgen (LOD0) | {tris} |",
        f"| Ölçek referansı | {HUMAN_HEIGHT:.2f} m insan figürü |",
        "",
        "## Render edilen nesneler",
        "",
    ]
    lines += [f"- `{o.name}`" for o in targets]
    lines += [
        "",
        "## Yeniden üretim",
        "",
        "```",
        cmd,
        "```",
        "",
        "## Caner'in notu",
        "",
        "<!-- Serbest metin. Onay formatı: OK vN -->",
        "",
    ]

    with open(path, "w", encoding="utf-8") as fh:
        fh.write("\n".join(lines))
    hz.log(f"info: {path}")


# ----------------------------------------------------------------------- ana

def main():
    parser = hz.base_parser("Hezarfen inceleme paketi ureticisi")
    parser.add_argument("--in", dest="src", required=True, help=".blend veya .fbx")
    parser.add_argument("--out", dest="out", default=None,
                        help="Cikti klasoru. Verilmezse renders/review/<asset>_vN")
    parser.add_argument("--asset", default=None, help="Varlik adi (surum klasoru icin)")
    parser.add_argument("--lod", type=int, default=0, help="Render edilecek LOD (varsayilan 0)")
    parser.add_argument("--res", type=int, default=900, help="Kare cozunurluk (px)")
    parser.add_argument("--samples", type=int, default=64, help="EEVEE ornek sayisi")
    parser.add_argument("--ref", action="append", default=[],
                        help="Kontak sayfasina eklenecek referans gorsel (tekrarlanabilir)")
    parser.add_argument("--eye", action="store_true",
                        help="Yaya seviyesi kadrajlar (1,65 m); yakin plan yargisi icin")
    parser.add_argument("--no-human", action="store_true", help="Olcek figurunu ekleme")
    parser.add_argument("--note", default="", help="Damgaya eklenecek ek metin")
    parser.add_argument("--hdri", nargs="?", const="auto", default=None,
                        help="GERCEKCI kip: HDRI ile aydinlat. Degersiz verilirse "
                             "art/textures/hdri/ icindeki ilk .hdr kullanilir.")
    parser.add_argument("--hdri-strength", type=float, default=1.0,
                        help="HDRI gucu (varsayilan 1.0)")
    args = parser.parse_args(hz.argv_after_dashes())

    asset = args.asset or os.path.splitext(os.path.basename(args.src))[0]
    if asset.startswith("SM_"):
        asset = asset[3:]

    out_dir = os.path.abspath(args.out) if args.out else next_version_dir(asset)
    os.makedirs(out_dir, exist_ok=True)

    src = load_input(args.src, args.lod)
    targets = collect_targets(args.lod)
    lo, hi = world_bounds(targets)
    size = hi - lo
    hz.log(f"bounds: {size.x:.3f} x {size.y:.3f} x {size.z:.3f} m, base z={lo.z:.3f}")

    hdri_path = args.hdri
    if hdri_path == "auto":
        found = sorted(f for f in os.listdir(HDRI_DIR)
                       if f.endswith(".hdr")) if os.path.isdir(HDRI_DIR) else []
        hdri_path = os.path.join(HDRI_DIR, found[0]) if found else None
        if hdri_path is None:
            hz.log("UYARI art/textures/hdri/ bos — notr studyo kipine dusuldu")

    build_studio(lo, hi, with_human=not args.no_human,
                 hdri=hdri_path, hdri_strength=args.hdri_strength)
    cam = make_camera()

    note = (f"{asset}  |  {size.x:.2f} x {size.y:.2f} x {size.z:.2f} m  "
            f"|  olcek: {HUMAN_HEIGHT:.2f} m figur")
    if args.note:
        note += f"  |  {args.note}"
    setup_render(args.res, args.samples, note)

    rendered = []
    if args.eye:
        for name, az, dist, tz, lens in EYE_VIEWS:
            eye_view(cam, lo, hi, az, dist, tz, lens)
            rendered.append(render_to(os.path.join(out_dir, f"{name}.png")))
    else:
        for name, az, el, focus, margin in VIEWS:
            frame_view(cam, lo, hi, az, el, focus, margin)
            rendered.append(render_to(os.path.join(out_dir, f"{name}.png")))

    refs = [os.path.abspath(p) for p in args.ref if os.path.exists(p)]
    for p in args.ref:
        if not os.path.exists(p):
            hz.log(f"WARN: referans bulunamadi, atlandi: {p}")

    contact_sheet(rendered, os.path.join(out_dir, "contact_sheet.png"),
                  tile=args.res, refs=refs)

    cmd = ("blender --background --factory-startup --python tools/blender/render_preview.py -- "
           f"--in {args.src} --asset {asset}")
    write_info(os.path.join(out_dir, "info.md"), asset, out_dir, src, lo, hi, targets, cmd)

    hz.log(f"review package OK -> {out_dir}")


if __name__ == "__main__":
    main()
