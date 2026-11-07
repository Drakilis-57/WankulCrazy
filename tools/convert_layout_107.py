"""Convertit les atlas Display / DeckBox (layout 1.06) vers le layout UV des meshes 1.07.

Sources : tools/legacy106/*.png  ->  sortie : data/patchtextures/shared1/*.png
Layout 1.07 mesuré via export GPU des meshes (CardBoxMesh_low / DeckBox_low).
"""
import os
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "tools", "legacy106")
DST = os.path.join(ROOT, "data", "patchtextures", "shared1")


def cover(img, w, h):
    """Redimensionne en remplissant (w,h) avec recadrage centré."""
    sw, sh = img.size
    s = max(w / sw, h / sh)
    r = img.resize((max(1, round(sw * s)), max(1, round(sh * s))), Image.LANCZOS)
    x, y = (r.width - w) // 2, (r.height - h) // 2
    return r.crop((x, y, x + w, y + h))


def fit_with_blur(img, w, h):
    """Place img à hauteur h au centre, complète les côtés avec une version floutée étirée."""
    bg = img.resize((w, h), Image.LANCZOS).filter(ImageFilter.GaussianBlur(30))
    s = h / img.height
    fg = img.resize((round(img.width * s), h), Image.LANCZOS)
    # fondu des bords latéraux
    mask = Image.new("L", fg.size, 255)
    px = mask.load()
    fade = 40
    for x in range(fade):
        a = int(255 * x / fade)
        for y in range(fg.height):
            px[x, y] = a
            px[fg.width - 1 - x, y] = a
    bg.paste(fg, ((w - fg.width) // 2, 0), mask)
    return bg


def paste_rect(canvas, img, box):
    x0, y0, x1, y1 = box
    canvas.paste(img.resize((x1 - x0, y1 - y0), Image.LANCZOS), (x0, y0))


def convert_display(name):
    old = Image.open(os.path.join(SRC, name)).convert("RGB")
    front = old.crop((19, 16, 512, 452))
    logo_band = old.crop((557, 188, 970, 320))
    legal_band = old.crop((557, 523, 970, 655))
    plain = old.crop((19, 495, 512, 930))

    canvas = plain.resize((2048, 2048), Image.LANCZOS)

    # Faces (px) calculées depuis les UV du mesh : B=face principale, A/C=grandes faces, E/F=côtés
    B = (645, 570, 1401, 1024)
    A = (645, 1024, 1401, 1573)
    C = (645, 21, 1401, 570)
    E = (1401, 570, 1950, 1024)
    F = (95, 570, 645, 1024)
    D = (645, 1573, 1401, 2027)

    # A = face avant réellement visible en rayon (hero art), B = dessus (logo)
    canvas.paste(fit_with_blur(front, A[2] - A[0], A[3] - A[1]), (A[0], A[1]))

    for box, band, rot in ((B, logo_band, 0), (C, legal_band, 180)):
        w, h = box[2] - box[0], box[3] - box[1]
        face = cover(plain, w, h)
        bw = int(w * 0.85)
        bh = int(bw * band.height / band.width)
        face.paste(band.resize((bw, bh), Image.LANCZOS), ((w - bw) // 2, (h - bh) // 2))
        if rot:
            face = face.rotate(rot)
        canvas.paste(face, (box[0], box[1]))

    for box in (E, F, D):
        canvas.paste(cover(plain, box[2] - box[0], box[3] - box[1]), (box[0], box[1]))

    canvas.save(os.path.join(DST, name))


def convert_deckbox(name):
    old = Image.open(os.path.join(SRC, name)).convert("RGB")
    p_front = old.crop((315, 0, 708, 510))
    p_left = old.crop((0, 0, 295, 510))
    p_right = old.crop((730, 0, 1024, 510))
    p_back = old.crop((5, 515, 385, 1024))
    p_top = old.crop((555, 515, 870, 765))
    p_bottom = old.crop((545, 768, 875, 1024))

    # fond : couleur moyenne foncée du panneau principal
    avg = p_front.resize((1, 1), Image.LANCZOS).getpixel((0, 0))
    canvas = Image.new("RGB", (1024, 1024), avg)

    faces = {
        "front": ((6, 763, 205, 1015), p_front),
        "back": ((6, 292, 205, 540), p_back),
        "top": ((6, 570, 205, 752), p_top),
        "bottom": ((6, 95, 205, 292), p_bottom),
        "xm": ((426, 475, 615, 727), p_left),
        "xp": ((797, 475, 985, 727), p_right),
    }
    pad = 5
    for key, (box, src) in faces.items():
        x0, y0, x1, y1 = box
        w, h = x1 - x0, y1 - y0
        face = cover(src, w, h)
        # étend les bords (marge) pour éviter les liserés
        big = face.resize((w + 2 * pad, h + 2 * pad), Image.LANCZOS)
        canvas.paste(big, (x0 - pad, y0 - pad))
        canvas.paste(face, (x0, y0))

    canvas.save(os.path.join(DST, name))


if __name__ == "__main__":
    for f in sorted(os.listdir(SRC)):
        if f.startswith("Texture_Display_"):
            convert_display(f)
            print("display", f)
        elif f.startswith("T_DeckBox"):
            convert_deckbox(f)
            print("deckbox", f)
