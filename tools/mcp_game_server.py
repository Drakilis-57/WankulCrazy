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
        
        with urllib.request.urlopen(req, timeout=5) as res:
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
        "description": "Exporte le mesh 3D et ses coordonnees UV exactes de l'objet en main vers un fichier .OBJ.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "mesh_name": {"type": "string", "description": "Nom optionnel du mesh a exporter (ex: Cleanser_low). Si omis, prend l'objet en main."}
            }
        }
    }
]

def handle_call_tool(params):
    name = params.get("name")
    args = params.get("arguments", {})

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
        endpoint = f"dump_mesh?name={m_name}" if m_name else "dump_mesh"
        res = request_api(endpoint, method="POST")
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
