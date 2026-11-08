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

    # Deckboxes
    50: ("Icon_DeckBox1.png", 8),
    51: ("Icon_DeckBox2.png", 8),
    52: ("Icon_DeckBox3.png", 8),
    53: ("Icon_DeckBox4.png", 8),

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

    # Sleeves vanilla (paquets)
    89: ("Icon_CardSleeveClear.png", 8),
    90: ("Icon_CardSleeveTetramon.png", 8),
    91: ("Icon_CardSleeveFire.png", 8),
    92: ("Icon_CardSleeveEarth.png", 8),
    93: ("Icon_CardSleeveWater.png", 8),
    94: ("Icon_CardSleeveWind.png", 8),

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

def paste_centered(atlas, sprite_path, cell_idx, padding=8):
    im = Image.open(sprite_path).convert('RGBA')
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
        path = find_file(fn)
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
