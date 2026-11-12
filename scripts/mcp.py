#!/usr/bin/env python3
"""Minimal MCP (streamable HTTP) client for the local CMS. usage: mcp.py list | mcp.py call <tool> '<json-args>' (or @file)"""
import json, sys, urllib.request
URL, SECRET = "http://localhost:5080/mcp", "poc-local-mcp-secret-7f3a9c"
_sid = None
def rpc(method, params=None, notify=False):
    global _sid
    body = {"jsonrpc": "2.0", "method": method, "params": params or {}}
    if not notify: body["id"] = 1
    h = {"Authorization": SECRET, "Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
    if _sid: h["Mcp-Session-Id"] = _sid
    with urllib.request.urlopen(urllib.request.Request(URL, json.dumps(body).encode(), h)) as r:
        _sid = r.headers.get("Mcp-Session-Id") or _sid
        raw = r.read().decode()
    msgs = [json.loads(l[5:]) for l in raw.splitlines() if l.startswith("data:")] or ([json.loads(raw)] if raw.strip() else [])
    return next((m for m in msgs if "result" in m or "error" in m), None)
def init():
    rpc("initialize", {"protocolVersion": "2025-03-26", "capabilities": {}, "clientInfo": {"name": "poc-client", "version": "1"}})
    rpc("notifications/initialized", notify=True)
def call(tool, args):
    r = rpc("tools/call", {"name": tool, "arguments": args})
    if "error" in r: return r
    res = r["result"]; out = []
    for c in res.get("content", []):
        t = c.get("text", "")
        try: out.append(json.loads(t))
        except Exception: out.append(t)
    return {"isError": res.get("isError", False), "content": out[0] if len(out) == 1 else out}
if __name__ == "__main__":
    init()
    if sys.argv[1] == "list":
        print(json.dumps(rpc("tools/list")["result"]["tools"]))
    else:
        a = sys.argv[3] if len(sys.argv) > 3 else "{}"
        if a.startswith("@"): a = open(a[1:]).read()
        print(json.dumps(call(sys.argv[2], json.loads(a)), indent=1, ensure_ascii=False))
