"""
Générateur de PriceTagAtlas Wankul.
Injecte toutes les icônes Wankul dans l'atlas des étiquettes de rayon (PriceTagAtlas 4096x4096).
"""
import os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BASE_ATLAS = os.path.join(ROOT, "tools", "pricetag_atlas_base.png")
OUT_ATLAS = os.path.join(ROOT, "data", "patchtextures", "shared1", "PriceTagAtlas.png")

WANKUL_ICONS = os.path.join(ROOT, "tools", "wankul_icons")
SPRITES = os.path.join(ROOT, "data", "sprites")
CUSTOM_ICONS = os.path.join(ROOT, "data", "customitems", "icons")
SHARED1 = os.path.join(ROOT, "data", "patchtextures", "shared1")

def find_file(name):
    for d in [WANKUL_ICONS, CUSTOM_ICONS, SHARED1, SPRITES]:
        p = os.path.join(d, name)
        if os.path.exists(p):
            return p
    raise FileNotFoundError(f"Fichier non trouvé: {name}")

MAPPING = {
    # Boosters et Displays vanilla
    32: ("PackCommon.png", 6),
    33: ("CardBox.png", 6),
    34: ("PackRare.png", 6),
    35: ("RareCardBox.png", 6),
    36: ("PackEpic.png", 6),
    37: ("EpicCardBox.png", 6),
    38: ("PackLegend.png", 6),
    39: ("LegendCardBox.png", 6),
    40: ("PackDestinyCommon.png", 6),
    41: ("Destiny_CardBox.png", 6),
    42: ("PackDestinyRare.png", 6),
    43: ("Destiny_RareCardBox.png", 6),
    44: ("PackDestinyEpic.png", 6),
    45: ("Destiny_EpicCardBox.png", 6),
    46: ("PackDestinyLegendary.png", 6),
    47: ("Destiny_LegendCardBox.png", 6),

    # Déo / Cleanser Wankul
    49: ("Icon_Cleanser.png", 8),

    # Deckboxes (cases 50 à 53)
    50: (("T_DeckBox.png", "Icon_DeckBox1.png"), 8),
    51: (("T_DeckBox2.png", "Icon_DeckBox2.png"), 8),
    52: (("T_DeckBox3.png", "Icon_DeckBox3.png"), 8),
    53: (("T_DeckBox4.png", "Icon_DeckBox4.png"), 8),

    # Classeurs
    54: ("Icon_ItemCollectionBook.png", 8),
    75: ("Icon_PremiumCollectionBook.png", 8),

    # Dés D20
    55: ("Icon_D20DiceBox.png", 8),
    56: ("Icon_D20DiceBox 2.png", 8),
    57: ("Icon_D20DiceBox 3.png", 8),
    58: ("Icon_D20DiceBox 4.png", 8),

    # Precon Decks / Starters vanilla
    81: ("Icon_PreconFire.png", 8),
    82: ("Icon_PreconEarth.png", 8),
    83: ("Icon_PreconWater.png", 8),
    84: ("Icon_PreconWind.png", 8),
    85: ("Icon_PreconFireDestiny.png", 8),
    86: ("Icon_PreconEarthDestiny.png", 8),
    87: ("Icon_PreconWaterDestiny.png", 8),
    88: ("Icon_PreconWindDestiny.png", 8),

    # Sleeves vanilla (paquets - cases 89 à 94)
    89: (("T_CardSleeveClear.png", "Icon_CardSleeveClear.png"), 8),
    90: (("T_CardSleeveTetramon.png", "Icon_CardSleeveTetramon.png"), 8),
    91: (("T_CardSleeveFire.png", "Icon_CardSleeveFire.png"), 8),
    92: (("T_CardSleeveEarth.png", "Icon_CardSleeveEarth.png"), 8),
    93: (("T_CardSleeveWater.png", "Icon_CardSleeveWater.png"), 8),
    94: (("T_CardSleeveWind.png", "Icon_CardSleeveWind.png"), 8),

    # Tapis de jeu (Playmats) 1 à 14
    95: ("Icon_Playmat1.png", 8),
    96: ("Icon_Playmat2.png", 8),
    97: ("Icon_Playmat2b.png", 8),
    98: ("Icon_Playmat3.png", 8),
    99: ("Icon_Playmat4.png", 8),
    100: ("Icon_Playmat5.png", 8),
    101: ("Icon_Playmat6.png", 8),
    102: ("Icon_Playmat7.png", 8),
    103: ("Icon_Playmat8.png", 8),
    104: ("Icon_Playmat9.png", 8),
    105: ("Icon_Playmat10.png", 8),
    106: ("Icon_Playmat11.png", 8),
    107: ("Icon_Playmat12.png", 8),
    108: ("Icon_Playmat13.png", 8),
    109: ("Icon_Playmat14.png", 8),

    # Mangas 1 à 12
    121: ("Icon_Manga1.png", 8),
    122: ("Icon_Manga2.png", 8),
    123: ("Icon_Manga3.png", 8),
    124: ("Icon_Manga4.png", 8),
    125: ("Icon_Manga5.png", 8),
    126: ("Icon_Manga6.png", 8),
    127: ("Icon_Manga7.png", 8),
    128: ("Icon_Manga8.png", 8),
    129: ("Icon_Manga9.png", 8),
    130: ("Icon_Manga10.png", 8),
    131: ("Icon_Manga11.png", 8),
    132: ("Icon_Manga12.png", 8),

    # Tapis de jeu (Playmats) 15 à 18
    135: ("Icon_Playmat15.png", 8),
    136: ("Icon_Playmat16.png", 8),
    137: ("Icon_Playmat17.png", 8),
    138: ("Icon_Playmat18.png", 8),

    # Items custom Wankul (cellules libres 224+)
    224: ("Icon_Display_S4.png", 6),
    225: ("Icon_Display_S4_TauxDrop.png", 6),
    226: ("Icon_Booster_S4.png", 6),
    227: ("Icon_Booster_S4_TauxDrop.png", 6),
    228: ("Icon_Display_Legacy.png", 6),
    229: ("Icon_Display_Legacy.png", 6),
    230: ("Icon_Booster_Legacy.png", 6),
    231: ("Icon_Booster_Legacy.png", 6),
    232: ("Texture_Calecon.png", 10),
    233: ("Icon_Starter_Apocalypse.png", 6),
    234: ("Icon_Starter_Showtime.png", 6),
    235: ("Icon_Tapis_S4_1.png", 8),
    236: ("Icon_Tapis_S4_1.png", 8),
    237: ("Icon_ItemCollectionBook.png", 8),
}


def resolve_candidate_file(name_or_tuple):
    """Résout le premier fichier disponible parmi un nom ou un tuple de noms."""
    if isinstance(name_or_tuple, (tuple, list)):
        for candidate in name_or_tuple:
            try:
                return find_file(candidate)
            except FileNotFoundError:
                continue
        raise FileNotFoundError(f"Aucun fichier trouvé parmi: {name_or_tuple}")
    return find_file(name_or_tuple)


def trim_uniform_border(im, tolerance=8):
    """Supprime les bandes transparentes ou uniformes parasites autour d'une image RGBA."""
    alpha_bbox = im.getbbox()
    if alpha_bbox:
        im = im.crop(alpha_bbox)
    return im


def prepare_deckbox_icon(im, sprite_path):
    """
    Extrait proprement la face principale de la Deckbox (cases 50 à 53)
    sans écrasement ni bande parasite issue des rabats adjacents.
    """
    fname = os.path.basename(sprite_path).lower()
    w, h = im.size

    if fname.startswith("t_deckbox"):
        # Texture 3D de DeckBox (layout 1.07 : face avant dans (6, 763, 205, 1015) sur 1024x1024)
        sx = w / 1024.0
        sy = h / 1024.0
        # Marge intérieure de 2px pour exclure tout liseré/padding des îlots voisins (ex: top à y<=752)
        x0 = int(round(8 * sx))
        y0 = int(round(765 * sy))
        x1 = int(round(203 * sx))
        y1 = int(round(1013 * sy))
        front = im.crop((x0, y0, x1, y1))

        # Si la zone 1.07 est monochrome (ex: ancien layout 1.06 où la face avant est à (315, 0, 708, 510))
        extrema = front.convert("L").getextrema()
        if extrema and (extrema[1] - extrema[0] < 5):
            front = im.crop((
                int(round(318 * sx)),
                int(round(4 * sy)),
                int(round(705 * sx)),
                int(round(506 * sy)),
            ))
        return trim_uniform_border(front)

    # Cas Icon_DeckBox*.png : nettoyer les éventuelles bandes parasites haut/bas et conserver le ratio
    im = trim_uniform_border(im)
    cw, ch = im.size
    if cw > 0 and ch > 0:
        # Si l'icône inclut une bande parasite supérieure (rabat top collé au-dessus de la face avant)
        aspect = cw / float(ch)
        if aspect < 0.68:
            # Ne garder que la face principale inférieure (ratio nominal ~199x252 ≈ 0.79)
            target_h = min(ch, int(round(cw / 0.79)))
            im = im.crop((0, ch - target_h, cw, ch))
    return im


def prepare_sleeve_icon(im, sprite_path):
    """
    Ajuste le crop et l'orientation des textures/icônes de Sleeves (cases 89 à 94)
    pour que la languette d'accroche cartonnée ("Wankul 100 Sleeves") soit en haut
    et l'illustration principale en bas.
    """
    fname = os.path.basename(sprite_path).lower()
    im = trim_uniform_border(im)
    w, h = im.size

    if fname.startswith("t_cardsleeve"):
        # Dans les textures T_CardSleeve*.png, le sachet complet (languette + illustration)
        # occupe l'îlot UV principal. Si la texture est un atlas carré contenant verso + recto,
        # le recto occupe la moitié gauche ou centrale ; on isole d'abord l'îlot actif.
        # De plus, l'UV mapping des sleeves place la languette cartonnée en bas en espace texture brute :
        # on redresse à 180° pour placer "Wankul 100 Sleeves" en haut et l'illustration en bas.
        if w == h:
            # Vérifier si la moitié droite est vide/noire ou dupliquée (verso)
            right_half = im.crop((int(w * 0.55), int(h * 0.1), int(w * 0.95), int(h * 0.9))).convert("L")
            ext = right_half.getextrema()
            if ext and (ext[1] - ext[0] < 12):
                im = im.crop((0, 0, int(w * 0.52), h))
                im = trim_uniform_border(im)

        # Détecter où se trouve le bandeau cartonné ("Wankul 100 Sleeves") :
        # Si le bandeau est en bas (cas standard des UV T_CardSleeve), on pivote de 180°.
        if _is_sleeve_header_at_bottom(im):
            im = im.rotate(180, expand=True)
        return trim_uniform_border(im)

    # Cas Icon_CardSleeve*.png : vérifier aussi que la languette n'est pas en bas
    if _is_sleeve_header_at_bottom(im):
        im = im.rotate(180, expand=True)
    return trim_uniform_border(im)


def _is_sleeve_header_at_bottom(im):
    """
    Détermine si la languette d'accroche cartonnée ("Wankul 100 Sleeves") se trouve
    actuellement en bas de l'image (auquel cas une rotation à 180° est requise).
    """
    w, h = im.size
    if w < 16 or h < 16:
        return False

    rgba = im.convert("RGBA")
    band_h = max(4, int(h * 0.16))
    top_band = rgba.crop((0, 0, w, band_h))
    bot_band = rgba.crop((0, h - band_h, w, h))

    # 1. Si l'image est détourée (alpha), la languette d'accroche a souvent une encoche ou moins de largeur opaque
    top_alpha = sum(p[3] for p in top_band.getdata())
    bot_alpha = sum(p[3] for p in bot_band.getdata())
    if abs(top_alpha - bot_alpha) > (w * band_h * 255 * 0.08):
        # La zone avec encoche/découpe a moins d'alpha total -> doit être en haut
        return bot_alpha < top_alpha

    # 2. Sur une texture pleine (T_CardSleeve*.png), la languette cartonnée sombre/noire avec texte clair
    # a une luminance moyenne plus basse et plus uniforme sur les bords que l'illustration colorée
    top_luma = sum(0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2] for p in top_band.getdata()) / (w * band_h)
    bot_luma = sum(0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2] for p in bot_band.getdata()) / (w * band_h)
    return bot_luma < top_luma


def paste_centered(atlas, sprite_path, cell_idx, padding=8):
    im = Image.open(sprite_path).convert('RGBA')

    # Traitement spécifique par plage de cellules (Mission 3)
    if 50 <= cell_idx <= 53:
        im = prepare_deckbox_icon(im, sprite_path)
    elif 89 <= cell_idx <= 94:
        im = prepare_sleeve_icon(im, sprite_path)

    max_w = 256 - 2 * padding
    max_h = 256 - 2 * padding
    scale = min(max_w / im.width, max_h / im.height)
    new_w = max(1, round(im.width * scale))
    new_h = max(1, round(im.height * scale))
    resized = im.resize((new_w, new_h), Image.LANCZOS)

    r = cell_idx // 16
    c = cell_idx % 16
    x_cell = c * 256
    y_cell = r * 256

    clear_box = Image.new('RGBA', (256, 256), (0, 0, 0, 0))
    atlas.paste(clear_box, (x_cell, y_cell))

    px = x_cell + (256 - new_w) // 2
    py = y_cell + (256 - new_h) // 2
    atlas.paste(resized, (px, py), resized)


def main():
    print("Ouverture de l'atlas de base...")
    atlas = Image.open(BASE_ATLAS).convert('RGBA')
    
    for cell_idx, (fn, pad) in sorted(MAPPING.items()):
        path = resolve_candidate_file(fn)
        paste_centered(atlas, path, cell_idx, padding=pad)

    os.makedirs(os.path.dirname(OUT_ATLAS), exist_ok=True)
    atlas.save(OUT_ATLAS)
    print(f"Atlas Wankul sauvegardé ({len(MAPPING)} cellules remplacées) dans: {OUT_ATLAS} ({os.path.getsize(OUT_ATLAS)} octets)")

    game_atlas = r"E:\jeux\TCG Card Shop Simulator\BepInEx\plugins\WankulCrazy\data\patchtextures\shared1\PriceTagAtlas.png"
    if os.path.exists(os.path.dirname(game_atlas)):
        import shutil
        shutil.copy2(OUT_ATLAS, game_atlas)
        print(f"Atlas Wankul copié avec succès dans le jeu: {game_atlas}")

if __name__ == "__main__":
    main()

