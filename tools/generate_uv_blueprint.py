#!/usr/bin/env python3
"""
Générateur de Patron UV (UV Blueprint) 2048x2048 à partir d'un fichier .OBJ.
Dessine proprement les triangles UV avec un code couleur selon la normale (vn) de chaque face :
- Face avant (+Z) / Face arrière (-Z)
- Dessus (+Y)
- Dessous (-Y)
- Côtés (+X / -X)
Inclut une grille de repérage en pixels/UV et une légende claire.
"""
import argparse
import math
import os
import sys
from PIL import Image, ImageDraw, ImageFont


# Palette de couleurs par direction principale de la normale (R, G, B)
ORIENTATION_STYLES = {
    "+Z": {
        "label": "Face Avant (+Z)",
        "fill": (52, 152, 219, 105),     # Bleu vif
        "edge": (93, 173, 226, 235),
        "swatch": (52, 152, 219, 255),
    },
    "-Z": {
        "label": "Face Arriere (-Z)",
        "fill": (155, 89, 182, 105),     # Violet / Magenta
        "edge": (187, 143, 206, 235),
        "swatch": (155, 89, 182, 255),
    },
    "+Y": {
        "label": "Dessus (+Y)",
        "fill": (46, 204, 113, 115),     # Vert émeraude
        "edge": (88, 214, 141, 240),
        "swatch": (46, 204, 113, 255),
    },
    "-Y": {
        "label": "Dessous (-Y)",
        "fill": (241, 196, 15, 115),     # Jaune / Ambre
        "edge": (245, 203, 69, 240),
        "swatch": (241, 196, 15, 255),
    },
    "+X": {
        "label": "Cote Droit (+X)",
        "fill": (231, 76, 60, 105),      # Rouge / Corail
        "edge": (236, 112, 99, 235),
        "swatch": (231, 76, 60, 255),
    },
    "-X": {
        "label": "Cote Gauche (-X)",
        "fill": (26, 188, 156, 105),     # Cyan / Turquoise
        "edge": (72, 201, 176, 235),
        "swatch": (26, 188, 156, 255),
    },
}


def parse_obj(obj_path):
    """Parse un fichier .obj et retourne les sommets (v), UVs (vt), normales (vn) et faces triangulées."""
    vertices = []
    uvs = []
    normals = []
    faces = []  # Liste de tuples : ([(v_idx, vt_idx, vn_idx), x3], group_name)
    current_group = "default"

    with open(obj_path, "r", encoding="utf-8", errors="ignore") as f:
        for raw_line in f:
            line = raw_line.strip()
            if not line or line.startswith("#"):
                continue

            parts = line.split()
            prefix = parts[0]

            if prefix == "v" and len(parts) >= 4:
                vertices.append((float(parts[1]), float(parts[2]), float(parts[3])))
            elif prefix == "vt" and len(parts) >= 3:
                uvs.append((float(parts[1]), float(parts[2])))
            elif prefix == "vn" and len(parts) >= 4:
                normals.append((float(parts[1]), float(parts[2]), float(parts[3])))
            elif prefix in ("g", "o") and len(parts) >= 2:
                current_group = " ".join(parts[1:])
            elif prefix == "f" and len(parts) >= 4:
                face_Corners = []
                for token in parts[1:]:
                    sub = token.split("/")
                    v_i = int(sub[0]) if sub[0] else 0
                    vt_i = int(sub[1]) if len(sub) > 1 and sub[1] else 0
                    vn_i = int(sub[2]) if len(sub) > 2 and sub[2] else 0

                    # Gestion des indices relatifs négatifs OBJ
                    if v_i < 0:
                        v_i = len(vertices) + 1 + v_i
                    if vt_i < 0:
                        vt_i = len(uvs) + 1 + vt_i
                    if vn_i < 0:
                        vn_i = len(normals) + 1 + vn_i

                    face_Corners.append((v_i - 1, vt_i - 1, vn_i - 1))

                # Triangulation en éventail si polygone > 3 sommets
                for i in range(1, len(face_Corners) - 1):
                    tri = (face_Corners[0], face_Corners[i], face_Corners[i + 1])
                    faces.append((tri, current_group))

    return vertices, uvs, normals, faces


def compute_geometric_normal(v0, v1, v2):
    """Calcule la normale géométrique normalisée d'un triangle 3D (v0, v1, v2)."""
    ux, uy, uz = v1[0] - v0[0], v1[1] - v0[1], v1[2] - v0[2]
    vx, vy, vz = v2[0] - v0[0], v2[1] - v0[1], v2[2] - v0[2]
    nx = uy * vz - uz * vy
    ny = uz * vx - ux * vz
    nz = ux * vy - uy * vx
    length = math.sqrt(nx * nx + ny * ny + nz * nz)
    if length > 1e-8:
        return (nx / length, ny / length, nz / length)
    return (0.0, 0.0, 1.0)


def classify_normal(normal):
    """Retourne la clé d'orientation ('+Z', '-Z', '+Y', '-Y', '+X', '-X') selon la composante dominante."""
    nx, ny, nz = normal
    ax, ay, az = abs(nx), abs(ny), abs(nz)

    if ay >= ax and ay >= az:
        return "+Y" if ny >= 0 else "-Y"
    if az >= ax and az >= ay:
        return "+Z" if nz >= 0 else "-Z"
    return "+X" if nx >= 0 else "-X"


def uv_to_pixel(u, v, size):
    """Convertit (u, v) en coordonnées pixels (x, y) avec origine image en haut à gauche."""
    # Si les UVs dépassent légèrement [0, 1], on les garde dans le plan ou on wrappe proprement
    if u < 0.0 or u > 1.0:
        u = u - math.floor(u) if (u < -0.001 or u > 1.001) else max(0.0, min(1.0, u))
    if v < 0.0 or v > 1.0:
        v = v - math.floor(v) if (v < -0.001 or v > 1.001) else max(0.0, min(1.0, v))

    px = max(0, min(size - 1, int(round(u * (size - 1)))))
    py = max(0, min(size - 1, int(round((1.0 - v) * (size - 1)))))
    return (px, py)


def draw_grid(draw, size, step=128):
    """Dessine une grille graduée en pixels et coordonnées UV."""
    for pos in range(0, size, step):
        is_major = (pos % 256 == 0)
        color = (60, 68, 82, 255) if is_major else (36, 40, 48, 255)
        width = 2 if is_major else 1
        draw.line([(pos, 0), (pos, size - 1)], fill=color, width=width)
        draw.line([(0, pos), (size - 1, pos)], fill=color, width=width)

        if is_major and pos > 0:
            u_val = pos / float(size)
            v_val = 1.0 - (pos / float(size))
            draw.text((pos + 4, 4), f"X:{pos}\nU:{u_val:.2f}", fill=(130, 145, 165, 255))
            draw.text((4, pos + 4), f"Y:{pos} (V:{v_val:.2f})", fill=(130, 145, 165, 255))


def draw_legend(base_img, mesh_name, stats, counts_by_dir):
    """Dessine un cartouche de légende clair en bas de l'image."""
    size = base_img.width
    legend_h = 84
    overlay = Image.new("RGBA", (size, legend_h), (14, 17, 24, 230))
    draw = ImageDraw.Draw(overlay)

    # Bordure supérieure dorée
    draw.line([(0, 0), (size, 0)], fill=(255, 215, 100, 220), width=2)

    title_text = (
        f"UV BLUEPRINT : {mesh_name}  |  "
        f"Vertices: {stats['vertices']}  |  UVs: {stats['uvs']}  |  Triangles: {stats['triangles']}"
    )
    draw.text((18, 10), title_text, fill=(245, 247, 250, 255))

    # Swatches des 6 orientations de normales
    order = ["+Z", "-Z", "+Y", "-Y", "+X", "-X"]
    start_x = 18
    y_box = 38
    box_w = (size - 36) // len(order)

    for idx, key in enumerate(order):
        style = ORIENTATION_STYLES[key]
        x0 = start_x + idx * box_w
        # Carré de couleur
        draw.rectangle(
            [x0, y_box, x0 + 24, y_box + 24],
            fill=style["swatch"],
            outline=(255, 255, 255, 220),
            width=1,
        )
        count = counts_by_dir.get(key, 0)
        label = f"{style['label']} ({count} tris)"
        draw.text((x0 + 32, y_box + 5), label, fill=(225, 230, 238, 255))

    base_img.alpha_composite(overlay, (0, size - legend_h))


def generate_uv_blueprint(obj_path, output_path=None, size=2048):
    """Génère et sauvegarde le patron UV 2048x2048 d'un fichier .obj."""
    if not os.path.exists(obj_path):
        raise FileNotFoundError(f"Fichier OBJ introuvable : {obj_path}")

    if output_path is None:
        base_name = os.path.splitext(os.path.basename(obj_path))[0]
        output_path = os.path.join(os.path.dirname(obj_path), f"{base_name}_uv_blueprint.png")

    vertices, uvs, normals, faces = parse_obj(obj_path)

    # Image de base sombre
    base_img = Image.new("RGBA", (size, size), (20, 23, 30, 255))
    grid_draw = ImageDraw.Draw(base_img)
    draw_grid(grid_draw, size, step=max(64, size // 16))

    # Calques séparés pour le remplissage semi-transparent et les arêtes filaires
    fill_layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    edge_layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    fill_draw = ImageDraw.Draw(fill_layer)
    edge_draw = ImageDraw.Draw(edge_layer)

    counts_by_dir = {k: 0 for k in ORIENTATION_STYLES}
    drawn_triangles = 0

    for tri, _group in faces:
        (v0_i, vt0_i, vn0_i), (v1_i, vt1_i, vn1_i), (v2_i, vt2_i, vn2_i) = tri

        if vt0_i < 0 or vt1_i < 0 or vt2_i < 0:
            continue
        if vt0_i >= len(uvs) or vt1_i >= len(uvs) or vt2_i >= len(uvs):
            continue

        uv0 = uvs[vt0_i]
        uv1 = uvs[vt1_i]
        uv2 = uvs[vt2_i]

        # Calcul de la normale moyenne de la face (vn en priorité, sinon géométrique)
        nx, ny, nz = 0.0, 0.0, 0.0
        valid_normals = 0
        for vn_idx in (vn0_i, vn1_i, vn2_i):
            if 0 <= vn_idx < len(normals):
                n = normals[vn_idx]
                nx += n[0]
                ny += n[1]
                nz += n[2]
                valid_normals += 1

        if valid_normals > 0 and (abs(nx) + abs(ny) + abs(nz) > 1e-6):
            norm = (nx / valid_normals, ny / valid_normals, nz / valid_normals)
        elif (
            0 <= v0_i < len(vertices)
            and 0 <= v1_i < len(vertices)
            and 0 <= v2_i < len(vertices)
        ):
            norm = compute_geometric_normal(vertices[v0_i], vertices[v1_i], vertices[v2_i])
        else:
            norm = (0.0, 0.0, 1.0)

        dir_key = classify_normal(norm)
        counts_by_dir[dir_key] += 1
        style = ORIENTATION_STYLES[dir_key]

        p0 = uv_to_pixel(uv0[0], uv0[1], size)
        p1 = uv_to_pixel(uv1[0], uv1[1], size)
        p2 = uv_to_pixel(uv2[0], uv2[1], size)

        fill_draw.polygon([p0, p1, p2], fill=style["fill"])
        edge_draw.line([p0, p1, p2, p0], fill=style["edge"], width=1)
        drawn_triangles += 1

    base_img.alpha_composite(fill_layer)
    base_img.alpha_composite(edge_layer)

    stats = {
        "vertices": len(vertices),
        "uvs": len(uvs),
        "triangles": drawn_triangles,
    }
    mesh_name = os.path.basename(obj_path)
    draw_legend(base_img, mesh_name, stats, counts_by_dir)

    out_dir = os.path.dirname(os.path.abspath(output_path))
    if out_dir:
        os.makedirs(out_dir, exist_ok=True)
    base_img.save(output_path, "PNG")
    return output_path, stats, counts_by_dir


def main():
    parser = argparse.ArgumentParser(
        description="Génère un patron UV 2048x2048 coloré selon les normales des faces à partir d'un fichier .obj."
    )
    parser.add_argument("obj_path", help="Chemin du fichier .obj en entrée")
    parser.add_argument("output_path", nargs="?", default=None, help="Chemin du fichier .png en sortie (optionnel)")
    parser.add_argument("--size", type=int, default=2048, help="Résolution du patron carré en pixels (défaut: 2048)")
    args = parser.parse_args()

    out_path, stats, counts = generate_uv_blueprint(args.obj_path, args.output_path, size=args.size)
    print(f"Patron UV généré avec succès : {out_path}")
    print(f"  Sommets: {stats['vertices']}, UVs: {stats['uvs']}, Triangles UV: {stats['triangles']}")
    for k, v in counts.items():
        print(f"  {ORIENTATION_STYLES[k]['label']}: {v} triangles")


if __name__ == "__main__":
    main()
