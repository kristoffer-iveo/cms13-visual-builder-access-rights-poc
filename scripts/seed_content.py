"""Creates the POC content through the CMS MCP server (site must be running). Writes created keys to keys.json.

usage: python3 seed_content.py blocks | experience [--update] | classic   (run from this folder, in that order)
The keys are referenced by Business/PocSeeder.cs (PocContent) - update them there if content is recreated.
"""
import json, os, sys
import mcp
mcp.init()
KEYS = "keys.json"
keys = json.load(open(KEYS)) if os.path.exists(KEYS) else {}
START = "7b08c7d5-7585-47e5-add7-5822da68c3ce"  # Alloy start page (ContentGUID)

def create(name, **args):
    """Create + publish once; remembered by name in keys.json."""
    if name in keys and not args.pop("update", False): return keys[name]
    if name in keys:
        args = {"contentKey": keys[name], "contentVersion": 0, **{k: v for k, v in args.items() if k in ("properties", "composition")}}
    else:
        args = {"displayName": name, "locale": "en", **args}
    # The tool schema types "properties"/"composition" as strings, so objects are sent JSON-encoded.
    for k in ("properties", "composition"):
        if k in args: args[k] = json.dumps(args[k], ensure_ascii=False)
    args.setdefault("properties", "{}")
    r = mcp.call("paas_cms_update_content_item", args)
    c = r["content"]
    if r["isError"] or not isinstance(c, dict) or not c.get("contentKey"):
        print("FAILED", name, json.dumps(r, ensure_ascii=False)[:1500]); sys.exit(1)
    p = mcp.call("paas_cms_publish_content_item", {"contentKey": c["contentKey"], "contentVersion": int(c["contentVersion"])})
    print(name, "->", c["contentKey"], "| publish:", json.dumps(p["content"], ensure_ascii=False)[:160])
    keys[name] = c["contentKey"]; json.dump(keys, open(KEYS, "w"), indent=1, ensure_ascii=False)
    return c["contentKey"]

def ref(k): return "cms://content/" + k.replace("-", "")

def shared(name, html):
    return create(name, contentType="EditorialBlock", properties={"MainBody": html})

if __name__ == "__main__":
    step = sys.argv[1]
    if step == "blocks":
        shared("POC Shared block – Public", "<h3>Shared block: public</h3><p>No specific access rights. All visitors should see this block, both on the Visual Builder page and on the classic Content Area page.</p>")
        shared("POC Shared block – Members only (read ACL)", "<h3>Shared block: Members only</h3><p>Read access for the role <strong>Members</strong> only (Everyone removed). Anonymous visitors should NOT see this block. Log in as <em>member</em> to see it.</p>")
        shared("POC Shared block – Only admins can edit (edit ACL)", "<h3>Shared block: locked for editors</h3><p>Everyone can read, but only <strong>WebAdmins</strong> can edit/publish. The user <em>editor</em> should not be able to edit this block.</p>")
        shared("POC Shared block – VIP message", "<h3>VIP message</h3><p>This block is meant to be shown only to the audience <strong>POC VIP</strong> (add <code>?audience=vip</code> to the URL).</p>")
        shared("POC Shared block as section – Members only (read ACL)", "<h3>Shared block used as a section: Members only</h3><p>A shared, section-enabled block placed directly as a <strong>section</strong> in the experience. Read access for the role Members only – anonymous visitors should not see it.</p>")
        shared("POC Shared block – Default message", "<h3>Default message</h3><p>This block is the fallback for visitors who do not match any audience.</p>")
    k = keys
    def comp(name, ctype=None, props=None, shared_key=None):
        c = {"reference": ref(shared_key)} if shared_key else {"contentType": ctype, "properties": props}
        return {"nodeType": "component", "displayName": name, "component": c}
    def col(*nodes): return {"nodeType": "column", "nodes": list(nodes)}
    def row(*cols): return {"nodeType": "row", "nodes": list(cols)}
    def section(name, *rows): return {"nodeType": "section", "contentType": "BlankSection", "displayName": name, "nodes": list(rows)}

    if step == "experience":
        # Only inline elements here. The MCP tool turns a {"reference": ...} component into an inline COPY of the
        # shared block, so the sections with real shared-block references are added in Business/PocSeeder.cs.
        create("Visual Builder POC", update="--update" in sys.argv, contentType="BlankExperience", container=START, routeSegment="visual-builder-poc",
               composition={"nodes": [
                   section("1. Intro (inline elements)", row(col(
                       comp("Heading", "HeadingElement", {"Heading": "Visual Builder: audiences and access rights"}),
                       comp("Intro", "TextElement", {"Body": "This page is an experience built in Visual Builder. Sections 1 and 2 contain inline elements (stored in the page). The other sections use shared blocks with their own access rights. Compare with the page Classic page (Content Area), which uses the same shared blocks."})))),
                   section("2. Inline elements to test audience/ACL on", row(
                       col(comp("Inline A", "TextElement", {"Body": "Inline element A. Can an audience or access rights be set on this element in Visual Builder?"})),
                       col(comp("Inline B", "TextElement", {"Body": "Inline element B. Same question here – and for the whole section."})))),
               ]})
    if step == "classic":
        create("Classic page (Content Area)", contentType="StandardPage", container=START, routeSegment="classic-page",
               properties={
                   "MetaTitle": "Classic page (Content Area)",
                   "MainBody": "<p>This page uses a classic <strong>Content Area</strong> (On-page edit / All Properties) with the same shared blocks as the Visual Builder page. The VIP and Default messages are in a personalization group: add <code>?audience=vip</code> to see the VIP variant.</p>",
                   "MainContentArea": [{"reference": ref(k[n])} for n in [
                       "POC Shared block – Public", "POC Shared block – Members only (read ACL)",
                       "POC Shared block – Only admins can edit (edit ACL)", "POC Shared block – VIP message",
                       "POC Shared block – Default message"]]})
