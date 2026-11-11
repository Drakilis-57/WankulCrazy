#!/usr/bin/env python3
"""
Wankul Game Bridge MCP Server (Model Context Protocol)
Expose les outils de communication en direct avec TCG Card Shop Simulator.
"""
import sys
import json
import urllib.request
import urllib.error

BASE_URL = "http://127.0.0.1:8765"

def request_api(endpoint, method="GET", data=None):
    url = f"{BASE_URL}/{endpoint.lstrip('/')}"
    try:
        req = urllib.request.Request(url, method=method)
        if data is not None:
            body = json.dumps(data).encode("utf-8")
            req.data = body
            req.add_header("Content-Type", "application/json; charset=utf-8")
        
        with urllib.request.urlopen(req, timeout=30) as res:
            resp_body = res.read().decode("utf-8")
            return json.loads(resp_body)
    except urllib.error.URLError as ex:
        return {"error": f"Le jeu ne repond pas sur {url}. Assurez-vous que le jeu est lance: {ex}"}
    except Exception as ex:
        return {"error": f"Exception bridge: {ex}"}

TOOLS = [
    {
        "name": "game_status",
        "description": "Verifie si TCG Card Shop Simulator tourne et si le Game Bridge est actif.",
        "inputSchema": {"type": "object", "properties": {}}
    },
    {
        "name": "game_get_held_item",
        "description": "Recupere les informations detaillees de l'objet tenu en main (nom, ItemType, Mesh, materiaux, shaders, textures, bornes UV).",
        "inputSchema": {"type": "object", "properties": {}}
    },
    {
        "name": "game_get_target",
        "description": "Recupere l'objet 3D ou l'etiquette de rayon visee au centre de l'ecran.",
        "inputSchema": {"type": "object", "properties": {}}
    },
    {
        "name": "game_reload_textures",
        "description": "Recharge a chaud (Hot-Reload) l'integralite des textures Wankul en jeu sans redemarrer.",
        "inputSchema": {"type": "object", "properties": {}}
    },
    {
        "name": "game_take_screenshot",
        "description": "Declenche une capture d'ecran haute definition en jeu et retourne son chemin d'acces.",
        "inputSchema": {"type": "object", "properties": {}}
    },
    {
        "name": "game_dump_mesh",
        "description": "Exporte le mesh 3D et ses coordonnees UV exactes de l'objet en main (ou par nom) vers un fichier .OBJ, meme si isReadable est false.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "mesh_name": {"type": "string", "description": "Nom optionnel du mesh a exporter (ex: Cleanser_low). Si omis, prend l'objet en main."}
            }
        }
    },
    {
        "name": "game_dump_texture",
        "description": "Exporte une texture du jeu en PNG dans BepInEx/plugins/WankulCrazy/texture_dumps/<texture_name>.png via RenderTexture (contourne isReadable == false et les compressions DXT/BC7).",
        "inputSchema": {
            "type": "object",
            "properties": {
                "texture_name": {"type": "string", "description": "Nom exact de la texture a exporter (ex: T_Cleanser_BaseColor, PriceTagAtlas, T_CardSleeveClear)."}
            },
            "required": ["texture_name"]
        }
    },
    {
        "name": "game_render_item",
        "description": "Studio Photo 3D isole : instancie temporairement un item ou mesh hors-champ et effectue 4 rendus (Face, 3/4 Iso, Dessus, Dos) assembles en grille 2x2 dans screenshots/preview_<name>.png.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "item_type": {"type": "string", "description": "Type d'item (ex: Deodorant, PlayMat1, DeckBox1, DisplayStellar)."},
                "mesh_name": {"type": "string", "description": "Nom du mesh 3D si item_type n'est pas utilise (ex: Cleanser_low)."},
                "size": {"type": "integer", "description": "Taille en pixels de chaque vue de la grille 2x2 (defaut: 512).", "default": 512}
            }
        }
    },
    {
        "name": "game_get_logs",
        "description": "Recupere les 200 derniers messages de log Unity/BepInEx (filtrables par level: Error, Warning, Info et par mot-cle).",
        "inputSchema": {
            "type": "object",
            "properties": {
                "level": {"type": "string", "description": "Filtre optionnel par niveau : Error, Warning, Info."},
                "keyword": {"type": "string", "description": "Filtre optionnel par mot-cle dans le message ou la stacktrace."}
            }
        }
    }
]

def handle_call_tool(params):
    import urllib.parse
    name = params.get("name")
    args = params.get("arguments", {}) or {}

    if name == "game_status":
        res = request_api("status")
    elif name == "game_get_held_item":
        res = request_api("held_item")
    elif name == "game_get_target":
        res = request_api("target")
    elif name == "game_reload_textures":
        res = request_api("reload_textures", method="POST")
    elif name == "game_take_screenshot":
        res = request_api("screenshot", method="POST")
    elif name == "game_dump_mesh":
        m_name = args.get("mesh_name", "")
        query = f"?name={urllib.parse.quote(str(m_name))}" if m_name else ""
        res = request_api(f"dump_mesh{query}", method="POST", data={"mesh_name": m_name} if m_name else None)
    elif name == "game_dump_texture":
        t_name = args.get("texture_name", "")
        query = f"?texture_name={urllib.parse.quote(str(t_name))}" if t_name else ""
        res = request_api(f"dump_texture{query}", method="POST", data={"texture_name": t_name})
    elif name == "game_render_item":
        item_type = args.get("item_type", "")
        mesh_name = args.get("mesh_name", "")
        size = args.get("size", 512)
        q_parts = []
        if item_type:
            q_parts.append(f"item_type={urllib.parse.quote(str(item_type))}")
        if mesh_name:
            q_parts.append(f"mesh_name={urllib.parse.quote(str(mesh_name))}")
        if size:
            q_parts.append(f"size={int(size)}")
        query = ("?" + "&".join(q_parts)) if q_parts else ""
        res = request_api(
            f"render_item{query}",
            method="POST",
            data={"item_type": item_type, "mesh_name": mesh_name, "size": size}
        )
    elif name == "game_get_logs":
        level = args.get("level", "")
        keyword = args.get("keyword", "")
        q_parts = []
        if level:
            q_parts.append(f"level={urllib.parse.quote(str(level))}")
        if keyword:
            q_parts.append(f"keyword={urllib.parse.quote(str(keyword))}")
        query = ("?" + "&".join(q_parts)) if q_parts else ""
        res = request_api(f"logs{query}", method="GET")
    else:
        res = {"error": f"Outil inconnu : {name}"}

    return {
        "content": [
            {
                "type": "text",
                "text": json.dumps(res, indent=2, ensure_ascii=False)
            }
        ]
    }

def main():
    while True:
        line = sys.stdin.readline()
        if not line:
            break
        try:
            req = json.loads(line)
        except Exception:
            continue

        msg_id = req.get("id")
        method = req.get("method")
        params = req.get("params", {})

        if method == "tools/list":
            resp = {
                "jsonrpc": "2.0",
                "id": msg_id,
                "result": {"tools": TOOLS}
            }
        elif method == "tools/call":
            result = handle_call_tool(params)
            resp = {
                "jsonrpc": "2.0",
                "id": msg_id,
                "result": result
            }
        elif method == "initialize":
            resp = {
                "jsonrpc": "2.0",
                "id": msg_id,
                "result": {
                    "protocolVersion": "2024-11-05",
                    "capabilities": {"tools": {}},
                    "serverInfo": {"name": "wankul-game-bridge", "version": "1.0.0"}
                }
            }
        elif method == "notifications/initialized":
            continue
        else:
            resp = {
                "jsonrpc": "2.0",
                "id": msg_id,
                "error": {"code": -32601, "message": f"Method not found: {method}"}
            }

        sys.stdout.write(json.dumps(resp) + "\n")
        sys.stdout.flush()

if __name__ == "__main__":
    main()
