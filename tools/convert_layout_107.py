"""Convertit les atlas Display / DeckBox / PreconDeck (layout 1.06) vers le layout UV des meshes 1.07.

Sources : tools/legacy106/*.png  ->  sortie : data/patchtextures/shared1/*.png
Layout 1.07 mesuré via export GPU des meshes (CardBoxMesh_low / DeckBox_low) et sonde UV 3D (CardDeck).
"""
import argparse
import json
import os
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "tools", "legacy106")
DST = os.path.join(ROOT, "data", "patchtextures", "shared1")
DECKBOX_MESH_JSON = os.path.join(ROOT, "tools", "deckbox_low_uv.json")


def trim_white_edges(img, threshold=242, max_trim=8):
    """Retire d'éventuels fins liserés blancs résiduels (1-4 px) sur les bords d'un crop."""
    arr = np.asarray(img.convert("RGB"))
    h, w, _ = arr.shape
    x0, y0, x1, y1 = 0, 0, w, h

    def is_white_line(pixels):
        return bool(np.mean(np.all(pixels >= threshold, axis=-1)) > 0.92)

    for _ in range(max_trim):
        if x1 - x0 > 16 and is_white_line(arr[y0:y1, x0]):
            x0 += 1
        else:
            break
    for _ in range(max_trim):
        if x1 - x0 > 16 and is_white_line(arr[y0:y1, x1 - 1]):
            x1 -= 1
        else:
            break
    for _ in range(max_trim):
        if y1 - y0 > 16 and is_white_line(arr[y0, x0:x1]):
            y0 += 1
        else:
            break
    for _ in range(max_trim):
        if y1 - y0 > 16 and is_white_line(arr[y1 - 1, x0:x1]):
            y1 -= 1
        else:
            break
    return img.crop((x0, y0, x1, y1))


def cover(img, w, h):
    """Redimensionne en remplissant (w,h) avec recadrage centré."""
    sw, sh = img.size
    s = max(w / sw, h / sh)
    r = img.resize((max(1, round(sw * s)), max(1, round(sh * s))), Image.LANCZOS)
    x, y = (r.width - w) // 2, (r.height - h) // 2
    return r.crop((x, y, x + w, y + h))


def convert_display(name):
    old = Image.open(os.path.join(SRC, name)).convert("RGB")
    front = trim_white_edges(old.crop((20, 17, 511, 451)))
    logo_band = trim_white_edges(old.crop((558, 189, 970, 320)))
    legal_band = trim_white_edges(old.crop((558, 524, 970, 654)))
    plain = trim_white_edges(old.crop((20, 496, 511, 930)))

    canvas = cover(plain, 2048, 2048)

    # Faces (px) calculées depuis les UV du mesh CardBoxMesh_low : A=avant (-Z), B=dessus (+Y), C=dos (+Z), D=dessous (-Y), E/F=côtés
    B = (645, 570, 1401, 1024)
    A = (645, 1024, 1401, 1573)
    C = (645, 21, 1401, 570)
    E = (1401, 570, 1950, 1024)
    F = (95, 570, 645, 1024)
    D = (645, 1573, 1401, 2027)

    # D'abord les faces adjacentes avec léger débord (bleed) pour éviter tout liseré aux arêtes
    for box in (E, F, D):
        bw, bh = box[2] - box[0], box[3] - box[1]
        panel = cover(plain, bw, bh)
        canvas.paste(panel.resize((bw + 16, bh + 16), Image.LANCZOS), (box[0] - 8, box[1] - 8))
        canvas.paste(panel, (box[0], box[1]))

    # Dessus (+Y) et dos (+Z) étirés pleine hauteur/largeur sans raccord ni bordure horizontale
    top_face = logo_band.resize((B[2] - B[0], B[3] - B[1]), Image.LANCZOS)
    canvas.paste(top_face, (B[0], B[1]))

    back_face = legal_band.resize((C[2] - C[0], C[3] - C[1]), Image.LANCZOS).rotate(180)
    # Petit bleed de 16 px vers y<21 pour l'arête haute du dos
    canvas.paste(back_face.crop((0, 0, back_face.width, 16)), (C[0], max(0, C[1] - 16)))
    canvas.paste(back_face, (C[0], C[1]))

    # Face avant (-Z) : rendu net pleine largeur sans flou latéral
    front_face = front.resize((A[2] - A[0], A[3] - A[1]), Image.LANCZOS)
    canvas.paste(front_face, (A[0], A[1]))

    canvas.save(os.path.join(DST, name))


_DECKBOX_MESH_CACHE = None


def _load_deckbox_mesh():
    global _DECKBOX_MESH_CACHE
    if _DECKBOX_MESH_CACHE is None:
        with open(DECKBOX_MESH_JSON, "r", encoding="utf-8") as f:
            _DECKBOX_MESH_CACHE = json.load(f)
    return _DECKBOX_MESH_CACHE


def _sample_bilinear(panel_arr, u, v):
    """Échantillonne un tableau (H, W, 3) en bilinéaire avec coordonnées normalisées u,v in [0,1]."""
    h, w, _ = panel_arr.shape
    fx = np.clip(u, 0.0, 1.0) * (w - 1)
    fy = np.clip(v, 0.0, 1.0) * (h - 1)
    x0 = np.floor(fx).astype(np.int32)
    y0 = np.floor(fy).astype(np.int32)
    x1 = np.minimum(x0 + 1, w - 1)
    y1 = np.minimum(y0 + 1, h - 1)
    wx = (fx - x0)[..., None]
    wy = (fy - y0)[..., None]
    c00 = panel_arr[y0, x0]
    c10 = panel_arr[y0, x1]
    c01 = panel_arr[y1, x0]
    c11 = panel_arr[y1, x1]
    return c00 * (1 - wx) * (1 - wy) + c10 * wx * (1 - wy) + c01 * (1 - wx) * wy + c11 * wx * wy


def convert_deckbox(name):
    """Projette les 6 faces 1.06 sur l'intégralité des 152 triangles du mesh 1.07 (DeckBox_low)."""
    old = Image.open(os.path.join(SRC, name)).convert("RGB")
    panels = {
        "-X": np.asarray(trim_white_edges(old.crop((2, 2, 294, 506))), dtype=np.float32),
        "-Z": np.asarray(trim_white_edges(old.crop((316, 2, 706, 506))), dtype=np.float32),
        "+X": np.asarray(trim_white_edges(old.crop((730, 2, 1020, 506))), dtype=np.float32),
        "+Z": np.asarray(trim_white_edges(old.crop((2, 516, 383, 1022))), dtype=np.float32),
        "+Y": np.asarray(trim_white_edges(old.crop((556, 516, 868, 758))), dtype=np.float32),
        "-Y": np.asarray(trim_white_edges(old.crop((548, 767, 872, 1022))), dtype=np.float32),
    }

    mesh = _load_deckbox_mesh()
    verts = np.asarray(mesh["v"], dtype=np.float32)
    vts = np.asarray(mesh["vt"], dtype=np.float32)
    faces = mesh["f"]

    xmin, ymin, zmin = verts.min(axis=0)
    xmax, ymax, zmax = verts.max(axis=0)
    dx, dy, dz = xmax - xmin, ymax - ymin, zmax - zmin

    size = 1024
    # Couleur moyenne de la face avant en fond de secours (loin hors des îles UV)
    avg = Image.fromarray(panels["-Z"].astype(np.uint8)).resize((1, 1), Image.LANCZOS).getpixel((0, 0))
    canvas = np.full((size, size, 3), avg, dtype=np.float32)
    painted = np.zeros((size, size), dtype=bool)

    # Trier les petits triangles/chanfreins en premier et les grandes faces principales en dernier,
    # sans tolérance barycentrique négative qui ferait déborder un triangle voisin sur la face d'à côté.
    draw_order = []
    for tri in faces:
        vi = [tri[0][0], tri[1][0], tri[2][0]]
        ti = [tri[0][1], tri[1][1], tri[2][1]]
        uv0 = np.array([vts[ti[0]][0] * size, (1.0 - vts[ti[0]][1]) * size], dtype=np.float32)
        uv1 = np.array([vts[ti[1]][0] * size, (1.0 - vts[ti[1]][1]) * size], dtype=np.float32)
        uv2 = np.array([vts[ti[2]][0] * size, (1.0 - vts[ti[2]][1]) * size], dtype=np.float32)
        uv_area = abs((uv1[0] - uv0[0]) * (uv2[1] - uv0[1]) - (uv2[0] - uv0[0]) * (uv1[1] - uv0[1]))
        draw_order.append((uv_area, vi, uv0, uv1, uv2))

    draw_order.sort(key=lambda item: item[0])

    for uv_area, vi, uv0, uv1, uv2 in draw_order:
        p0, p1, p2 = verts[vi[0]], verts[vi[1]], verts[vi[2]]

        # Normale géométrique exacte du triangle (évite les ambiguïtés des normales lissées aux coins)
        n = np.cross(p1 - p0, p2 - p0)
        nl = np.linalg.norm(n)
        if nl < 1e-8:
            continue
        nx, ny, nz = n / nl
        ax, ay, az = abs(nx), abs(ny), abs(nz)
        cx = (p0[0] + p1[0] + p2[0]) / 3.0
        cy = (p0[1] + p1[1] + p2[1]) / 3.0
        cz = (p0[2] + p1[2] + p2[2]) / 3.0

        if az >= ax and az >= ay:
            face_key = "+Z" if nz >= 0 else "-Z"
        elif ay >= ax and ay >= az:
            if ny >= 0:
                if cy > 0.2:
                    face_key = "+Y"
                elif cz < -0.38:
                    face_key = "-Z"
                else:
                    face_key = "+X" if cx >= 0 else "-X"
            elif cy < -0.60:
                face_key = "-Y"
            elif cz < -0.38:
                # Lèvre horizontale sous la façade avant (tri 0..3 à Y=-0.55) -> raccord propre avec le bas de -Z
                face_key = "-Z"
            else:
                face_key = "+X" if cx >= 0 else "-X"
        else:
            face_key = "+X" if nx >= 0 else "-X"

        x0 = max(0, int(np.floor(min(uv0[0], uv1[0], uv2[0]))))
        x1 = min(size - 1, int(np.ceil(max(uv0[0], uv1[0], uv2[0]))))
        y0 = max(0, int(np.floor(min(uv0[1], uv1[1], uv2[1]))))
        y1 = min(size - 1, int(np.ceil(max(uv0[1], uv1[1], uv2[1]))))

        denom = (uv1[1] - uv2[1]) * (uv0[0] - uv2[0]) + (uv2[0] - uv1[0]) * (uv0[1] - uv2[1])
        if abs(denom) < 1e-5:
            continue

        xs = np.arange(x0, x1 + 1, dtype=np.float32) + 0.5
        ys = np.arange(y0, y1 + 1, dtype=np.float32) + 0.5
        gx, gy = np.meshgrid(xs, ys)

        w0 = ((uv1[1] - uv2[1]) * (gx - uv2[0]) + (uv2[0] - uv1[0]) * (gy - uv2[1])) / denom
        w1 = ((uv2[1] - uv0[1]) * (gx - uv2[0]) + (uv0[0] - uv2[0]) * (gy - uv2[1])) / denom
        w2 = 1.0 - w0 - w1

        # Strictement à l'intérieur du triangle (0.001 d'epsilon numérique uniquement)
        mask = (w0 >= -0.001) & (w1 >= -0.001) & (w2 >= -0.001)
        if not np.any(mask):
            continue

        # Coordonnées 3D interpolées (clampées à [0,1])
        wx = np.clip(w0[mask], 0.0, 1.0)[:, None]
        wy = np.clip(w1[mask], 0.0, 1.0)[:, None]
        wz = np.clip(w2[mask], 0.0, 1.0)[:, None]
        wsum = wx + wy + wz
        pos = (wx * p0 + wy * p1 + wz * p2) / np.maximum(wsum, 1e-6)

        px_n = (pos[:, 0] - xmin) / dx
        py_down = (ymax - pos[:, 1]) / dy
        pz_f2b = (pos[:, 2] - zmin) / dz

        if face_key == "-Z":
            su, sv = px_n, py_down
        elif face_key == "+Z":
            su, sv = px_n, 1.0 - py_down
        elif face_key == "+Y":
            su, sv = px_n, 1.0 - pz_f2b
        elif face_key == "-Y":
            su, sv = px_n, pz_f2b
        elif face_key == "-X":
            su, sv = 1.0 - pz_f2b, py_down
        else:  # "+X"
            su, sv = pz_f2b, py_down

        cols = _sample_bilinear(panels[face_key], su, sv)
        sub_y, sub_x = np.where(mask)
        py_idx = y0 + sub_y
        px_idx = x0 + sub_x
        canvas[py_idx, px_idx] = cols
        painted[py_idx, px_idx] = True

    # Dilatation morphologique (bleed 6 px) hors des îles UV pour éviter les liserés de mipmap
    for _ in range(6):
        new_painted = painted.copy()
        new_canvas = canvas.copy()
        for dy_o, dx_o in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            src_y0, dst_y0 = max(0, -dy_o), max(0, dy_o)
            src_y1, dst_y1 = size - max(0, dy_o), size - max(0, -dy_o)
            src_x0, dst_x0 = max(0, -dx_o), max(0, dx_o)
            src_x1, dst_x1 = size - max(0, dx_o), size - max(0, -dx_o)
            fill = painted[src_y0:src_y1, src_x0:src_x1] & (~new_painted[dst_y0:dst_y1, dst_x0:dst_x1])
            if np.any(fill):
                new_canvas[dst_y0:dst_y1, dst_x0:dst_x1][fill] = canvas[src_y0:src_y1, src_x0:src_x1][fill]
                new_painted[dst_y0:dst_y1, dst_x0:dst_x1][fill] = True
        canvas, painted = new_canvas, new_painted

    Image.fromarray(np.clip(canvas, 0, 255).astype(np.uint8), "RGB").save(os.path.join(DST, name))


def convert_precon(name):
    """Convertit un patron PreconDeck / Starter 1.06 (1024x1024) vers le patron déplié 1.07 (CardDeck)."""
    old = Image.open(os.path.join(SRC, name)).convert("RGBA")
    w_out, h_out = 1024, 1024

    front_src = old.crop((226, 0, 800, 755))
    left_src = old.crop((42, 0, 226, 755))        # évite le fragment de code-barres parasites en [0..42]
    right_src = old.crop((802, 0, 975, 755))       # recentre le texte latéral éventuel (Battle / Stellar)
    top_src = old.crop((630, 760, 1020, 890))
    tab_src = old.crop((642, 918, 1020, 1018))     # exclut la bordure sombre haute
    # Dos : y=756 préserve l'intégralité du bord gauche (QR / www.wankul.fr / copyright) et y=1013 coupe le liseré droit
    back_raw = old.crop((14, 756, 396, 1013))
    back_src = back_raw.transpose(Image.Transpose.ROTATE_270).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    plain_src = old.crop((408, 762, 618, 886))

    canvas = plain_src.resize((w_out, h_out), Image.LANCZOS)

    def paste_with_bleed(box, patch, pad=6):
        x0, y0, x1, y1 = box
        bw, bh = x1 - x0, y1 - y0
        fitted = patch.resize((bw, bh), Image.LANCZOS)
        bled = fitted.resize((bw + 2 * pad, bh + 2 * pad), Image.LANCZOS)
        canvas.paste(bled, (x0 - pad, y0 - pad))
        canvas.paste(fitted, (x0, y0))

    # Côté Droit [505..633] + continuité jusque 674 pour couvrir tout l'interstice avant le dos
    right_fit = right_src.resize((128, 478), Image.LANCZOS)
    canvas.paste(right_fit.crop((right_fit.width - 12, 0, right_fit.width, 478)).resize((56, 490), Image.LANCZOS), (625, 378))
    paste_with_bleed((505, 384, 633, 862), right_src, pad=6)

    # Côté Gauche [34..162, 384..862]
    paste_with_bleed((34, 384, 162, 862), left_src, pad=6)
    # Dessus [162..505, 256..384]
    paste_with_bleed((162, 256, 505, 384), top_src, pad=6)

    # Languette haute (Face Avant) : décalée légèrement vers le haut pour dégager le logo en vue frontale
    tab_rays_base = tab_src.crop((0, tab_src.height - 25, tab_src.width, tab_src.height))
    paste_with_bleed((162, 134, 505, 258), tab_rays_base, pad=6)
    tab_front_fit = tab_src.resize((312, 78), Image.LANCZOS)
    canvas.paste(tab_front_fit.crop((0, 0, 312, 12)).resize((343, 18), Image.LANCZOS), (162, 132))
    canvas.paste(tab_front_fit, (178, 139))
    canvas.paste(top_src.resize((343, 128), Image.LANCZOS), (162, 256))

    # Dos Languette [674..1018, 294..384] & Dos Principal [674..1018, 384..862]
    paste_with_bleed((674, 384, 1018, 862), back_src, pad=6)
    paste_with_bleed((674, 294, 1018, 384), tab_src, pad=6)
    canvas.paste(back_src.resize((344, 478), Image.LANCZOS), (674, 384))

    # Face Avant [162..505, 384..862] + bleed vers le dessous
    front_fit = front_src.resize((343, 478), Image.LANCZOS)
    canvas.paste(front_fit.resize((343, 490), Image.LANCZOS), (162, 384))
    canvas.paste(front_fit, (162, 384))

    canvas.convert("RGBA").save(os.path.join(DST, name))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Convertit les textures legacy 1.06 vers le layout 1.07.")
    parser.add_argument("--precon-only", action="store_true", help="Ne convertit que les PreconDecks / Starters.")
    args = parser.parse_args()

    for f in sorted(os.listdir(SRC)):
        if not args.precon_only and f.startswith("Texture_Display_"):
            convert_display(f)
            print("display", f)
        elif not args.precon_only and f.startswith("T_DeckBox"):
            convert_deckbox(f)
            print("deckbox", f)
        elif f.startswith("T_PreconDeck") or f.startswith("Texture_Starter_"):
            convert_precon(f)
            print("precon", f)
