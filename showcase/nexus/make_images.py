"""Nexus gallery images for Crafting Helper: 16:9 crops of in-game screenshots with a caption bar."""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "raw")
OUT = os.path.join(HERE, "final")
os.makedirs(OUT, exist_ok=True)

W, H = 1920, 1080
ACCENT = (232, 194, 90)
DARK = (10, 12, 9)
BOLD = "C:/Windows/Fonts/segoeuib.ttf"
REG = "C:/Windows/Fonts/segoeui.ttf"

# The game renders 2304x1440 (16:10) centred in the 3440x1440 ultrawide capture.
GAME_X, GAME_W = 568, 2304


def font(path, size):
    return ImageFont.truetype(path, size)


def base(name, zoom=1.0, cx=0.5, cy=0.5):
    """16:9 crop of the game area; zoom > 1 punches in around (cx, cy) (0..1 of the game area)."""
    im = Image.open(os.path.join(RAW, name + ".png")).convert("RGB")
    cw = GAME_W / zoom
    ch = cw * 9 / 16
    x0 = GAME_X + (GAME_W - cw) * cx
    y0 = (1440 - ch) * cy
    return im.crop((int(x0), int(y0), int(x0 + cw), int(y0 + ch))).resize((W, H), Image.LANCZOS)


def bar(im, key, title, sub):
    grad = Image.new("L", (1, 300))
    for y in range(300):
        grad.putpixel((0, y), int(230 * (y / 299) ** 1.2))
    im.paste(Image.new("RGB", (W, 300), DARK), (0, H - 300), grad.resize((W, 300)))
    d = ImageDraw.Draw(im)
    x, y = 80, H - 175
    if key:
        f = font(BOLD, 64)
        tw = d.textlength(key, font=f)
        d.rounded_rectangle((x, y - 6, x + tw + 44, y + 82), radius=14, fill=ACCENT)
        d.text((x + 22, y), key, font=f, fill=(15, 15, 15))
        x += tw + 44 + 30
    d.text((x, y - 2), title, font=font(BOLD, 66), fill="white")
    if sub:
        d.text((80, y + 92), sub, font=font(REG, 40), fill=(228, 228, 228))
    tag = "Crafting Helper"
    ft = font(BOLD, 34)
    d.text((W - 60 - d.textlength(tag, font=ft), 40), tag, font=ft, fill=ACCENT, stroke_width=3, stroke_fill=(0, 0, 0))
    return im


def cover():
    # blurred, darkened jungle as background
    bg = base("pinned", 1.0, 0.5, 1.0).filter(ImageFilter.GaussianBlur(6))
    bg = Image.blend(bg, Image.new("RGB", (W, H), DARK), 0.45)
    # the recipe book window, cut out of the screenshot, on the right
    src = Image.open(os.path.join(RAW, "search.png")).convert("RGB")
    book = src.crop((1060, 286, 2382, 1150))
    bh = 720
    book = book.resize((int(book.width * bh / book.height), bh), Image.LANCZOS)
    bx, by = W - book.width - 70, (H - bh) // 2
    shadow = Image.new("L", (W, H), 0)
    ImageDraw.Draw(shadow).rectangle((bx - 10, by - 10, bx + book.width + 10, by + bh + 10), fill=200)
    bg.paste(Image.new("RGB", (W, H), (0, 0, 0)), (0, 0), shadow.filter(ImageFilter.GaussianBlur(24)))
    bg.paste(book, (bx, by))
    d = ImageDraw.Draw(bg)
    ImageDraw.Draw(bg).rectangle((bx - 3, by - 3, bx + book.width + 3, by + bh + 3), outline=ACCENT, width=3)
    X = 80
    d.text((X, 250), "Crafting", font=font(BOLD, 112), fill=ACCENT, stroke_width=2, stroke_fill=(0, 0, 0))
    d.text((X, 370), "Helper", font=font(BOLD, 112), fill=ACCENT, stroke_width=2, stroke_fill=(0, 0, 0))
    d.text((X + 4, 520), "In-game recipe book", font=font(BOLD, 44), fill="white")
    d.text((X + 4, 576), "for Green Hell", font=font(BOLD, 44), fill="white")
    feats = ["Search items & ingredients", "Can craft now", "One-click crafting table", "Pin recipes to screen"]
    fr = font(REG, 34)
    for i, t in enumerate(feats):
        y = 680 + i * 52
        d.ellipse((X + 6, y + 15, X + 20, y + 29), fill=ACCENT)
        d.text((X + 36, y), t, font=fr, fill=(235, 235, 235))
    k = "K"
    fk = font(BOLD, 44)
    d.rounded_rectangle((X + 4, 920, X + 64, 980), radius=10, fill=ACCENT)
    d.text((X + 34 - d.textlength(k, font=fk) / 2, 924), k, font=fk, fill=(15, 15, 15))
    d.text((X + 84, 928), "to open", font=font(REG, 38), fill=(235, 235, 235))
    return bg


SHOTS = [
    ("01_cover", None),
    ("02_search", ("search", 1.0, 0.5, 1.0), "K", "Search any item or ingredient",
     "Typing \"stone\" finds every recipe that uses stone"),
    ("03_can_craft_now", ("cancraft", 1.0, 0.5, 1.0), "", "Can craft now",
     "Only what you can make right now from your backpack and hands"),
    ("04_ingredients", ("details", 1.0, 0.5, 1.0), "", "Ingredients and \"Used to craft\"",
     "have / need for each ingredient, jump to sub-recipes with one click"),
    ("05_crafting_table", ("table", 1.0, 0.5, 0.4), "", "One click: ingredients on the table",
     "Then just press Craft"),
    ("06_pinned", ("pinned", 1.0, 0.5, 0.35), "", "Pin recipes to the screen",
     "Live have / need counters while you gather"),
    ("07_russian", ("russian", 1.0, 0.5, 1.0), "", "Follows the game language",
     "Item names from the game, mod text in English and Russian"),
]

for spec in SHOTS:
    out = spec[0]
    if spec[1] is None:
        im = cover()
    else:
        (raw, zoom, cx, cy), key, title, sub = spec[1], spec[2], spec[3], spec[4]
        im = bar(base(raw, zoom, cx, cy), key, title, sub)
    im.save(os.path.join(OUT, out + ".jpg"), quality=92)
    print("wrote", out)
