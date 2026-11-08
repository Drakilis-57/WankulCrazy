from PIL import Image, ImageDraw

W, H = 2048, 2048

# Colors matching user reference exactly:
COLOR_RING_SILVER = (195, 195, 200, 255)   # Metallic silver contour ring
COLOR_MID_TIER_BLACK = (22, 22, 24, 255)   # Matte black middle cylinder tier
COLOR_BUTTON_WHITE = (238, 238, 242, 255)  # Clean off-white spray button
COLOR_NOZZLE_BLACK = (15, 15, 18, 255)     # Black spray nozzle hole
COLOR_SHOULDER_BLACK = (22, 22, 24, 255)   # Seamless matte black shoulder
COLOR_BODY_BLACK = (22, 22, 24, 255)       # Matte black body

# Start with base HD texture containing Tempete artwork and text
base = Image.open('tools/test_cleanser_complete_hd.png').convert('RGBA')
out = Image.new('RGBA', (W, H), COLOR_BODY_BLACK)
draw = ImageDraw.Draw(out)

# 1. Body (Y: 880..2048): keep HD Tempete artwork & vector typography
body_crop = base.crop((0, 880, W, H))
out.paste(body_crop, (0, 880))

# 2. Entire shoulder (Y: 0..880, X: 256..2048) and (Y: 650..880, X: 0..2048):
# Pure solid seamless matte black
draw.rectangle([0, 0, W, 880], fill=COLOR_SHOULDER_BLACK)

# 3. Cap region (Y: 0..650, X: 0..256):
# a) Lower contour ring: X: 0..42
draw.rectangle([0, 0, 42, 650], fill=COLOR_RING_SILVER)

# b) Middle tier (separating ring from button): X: 42..75
draw.rectangle([42, 0, 75, 650], fill=COLOR_MID_TIER_BLACK)

# c) Spray button (cylinder + bevel + top disc): X: 75..256
draw.rectangle([75, 0, 256, 650], fill=COLOR_BUTTON_WHITE)

# d) Spray nozzle hole on front wall:
# In our diagnostic: Y = 350..420 is on the front side of the button.
# Wall is at X: 90..115.
# Draw small black spray hole:
nozzle_x = 100
for ny in [370, 371, 372, 373, 374, 375, 376, 377, 378]:
    draw.ellipse([nozzle_x - 10, 360, nozzle_x + 10, 390], fill=COLOR_NOZZLE_BLACK)

# Save texture
out.save('tools/cleanser_final_perfect.png')
dest_game = 'E:/jeux/TCG Card Shop Simulator/BepInEx/plugins/WankulCrazy/data/patchtextures/shared1/T_Cleanser_BaseColor.png'
dest_repo = 'data/patchtextures/shared1/T_Cleanser_BaseColor.png'
out.save(dest_game)
out.save(dest_repo)
print("Perfect cleanser texture generated and saved successfully!")
