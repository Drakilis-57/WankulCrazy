#!/usr/bin/env python3
"""
Wankul Game Bridge Client
Permet d'interroger et de commander TCG Card Shop Simulator en temps reel via http://127.0.0.1:8765/
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
        return {"error": f"Impossible de contacter le jeu sur {url}: {ex}"}
    except Exception as ex:
        return {"error": f"Exception: {ex}"}

def main():
    import urllib.parse
    if len(sys.argv) < 2:
        print("Usage: python game_bridge.py [status|held|target|reload|screenshot|dump_mesh|dump_texture|render_item|logs]")
        return

    cmd = sys.argv[1].lower()
    if cmd == "status":
        print(json.dumps(request_api("status"), indent=2, ensure_ascii=False))
    elif cmd in ["held", "held_item"]:
        print(json.dumps(request_api("held_item"), indent=2, ensure_ascii=False))
    elif cmd in ["target", "target_item"]:
        print(json.dumps(request_api("target"), indent=2, ensure_ascii=False))
    elif cmd in ["reload", "reload_textures"]:
        print(json.dumps(request_api("reload_textures", method="POST"), indent=2, ensure_ascii=False))
    elif cmd in ["screenshot", "screen"]:
        print(json.dumps(request_api("screenshot", method="POST"), indent=2, ensure_ascii=False))
    elif cmd in ["dump", "dump_mesh"]:
        mesh_name = sys.argv[2] if len(sys.argv) > 2 else ""
        query = f"dump_mesh?name={urllib.parse.quote(mesh_name)}" if mesh_name else "dump_mesh"
        print(json.dumps(request_api(query, method="POST"), indent=2, ensure_ascii=False))
    elif cmd in ["dump_texture", "tex"]:
        tex_name = sys.argv[2] if len(sys.argv) > 2 else ""
        query = f"dump_texture?texture_name={urllib.parse.quote(tex_name)}"
        print(json.dumps(request_api(query, method="POST", data={"texture_name": tex_name}), indent=2, ensure_ascii=False))
    elif cmd in ["render", "render_item"]:
        target = sys.argv[2] if len(sys.argv) > 2 else "Deodorant"
        size = int(sys.argv[3]) if len(sys.argv) > 3 else 512
        query = f"render_item?item_type={urllib.parse.quote(target)}&size={size}"
        print(json.dumps(request_api(query, method="POST", data={"item_type": target, "size": size}), indent=2, ensure_ascii=False))
    elif cmd == "logs":
        level = sys.argv[2] if len(sys.argv) > 2 else ""
        keyword = sys.argv[3] if len(sys.argv) > 3 else ""
        q = []
        if level:
            q.append(f"level={urllib.parse.quote(level)}")
        if keyword:
            q.append(f"keyword={urllib.parse.quote(keyword)}")
        endpoint = "logs" + ("?" + "&".join(q) if q else "")
        print(json.dumps(request_api(endpoint, method="GET"), indent=2, ensure_ascii=False))
    else:
        print(f"Commande inconnue: {cmd}")

if __name__ == "__main__":
    main()
