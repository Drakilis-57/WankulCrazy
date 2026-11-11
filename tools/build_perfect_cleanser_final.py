from PIL import Image, ImageDraw

W, H = 2048, 2048
base = Image.open('tools/test_cleanser_complete_hd.png').convert('RGBA')

COLOR_BLACK = (22, 22, 24, 255)
COLOR_BUTTON_WHITE = (244, 244, 248, 255)
COLOR_NOZZLE_RECESS = (205, 205, 212, 255)
COLOR_NOZZLE_BLACK = (12, 12, 15, 255)
COLOR_NOZZLE_PIN = (45, 45, 50, 255)

out = Image.new('RGBA', (W, H), COLOR_BLACK)
draw = ImageDraw.Draw(out)

# 1. Corps (Y: 880..2048): Illustration Tempete HD et typographie vectorielle
body = base.crop((0, 880, W, H))
out.paste(body, (0, 880))

# 2. Epaule et etages intermediaires (Y: 0..880): 100% noir mat sans bavure
draw.rectangle([0, 0, W, 880], fill=COLOR_BLACK)

# 3. Poussoir sommital blanc au minimum complet (paroi + disque superieur complet) :
# X: 1024..2048, Y: 0..660
draw.rectangle([1024, 0, 2048, 660], fill=COLOR_BUTTON_WHITE)

# 4. Orifice de buse de spray centre sur la face avant du poussoir:
# Centre horizontal X = 1536, Centre vertical Y = 140
cx, cy = 1536, 140
draw.ellipse([cx - 22, cy - 22, cx + 22, cy + 22], fill=COLOR_NOZZLE_RECESS)
draw.ellipse([cx - 15, cy - 15, cx + 15, cy + 15], fill=COLOR_NOZZLE_BLACK)
draw.ellipse([cx - 5, cy - 5, cx + 5, cy + 5], fill=COLOR_NOZZLE_PIN)

# Sauvegarde et deploiement
dest_game = 'E:/jeux/TCG Card Shop Simulator/BepInEx/plugins/WankulCrazy/data/patchtextures/shared1/T_Cleanser_BaseColor.png'
dest_repo = 'data/patchtextures/shared1/T_Cleanser_BaseColor.png'
out.save('tools/cleanser_final_perfect.png')
out.save(dest_game)
out.save(dest_repo)
print("Cleanser final perfect texture generated and deployed!")
