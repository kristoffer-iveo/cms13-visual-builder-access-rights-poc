# POC notes

Findings from the original proof of concept that are not part of the bug report in `README.md`.

## Extra test content

- The account `editor` (password `Test123!poc`, roles WebEditors and VisitorGroupAdmins) is an editor in Visual Builder.
- The shared block "POC Shared block – Only admins can edit (edit ACL)" tests edit rights: Everyone and WebEditors
  have Read only; WebAdmins and CmsAdmins have full access. `editor` should not be able to change it.
- The audience **POC Student** (matches `?audience=student`) exists but is not used by any test.
- The view of the hybrid section uses `@Html.PropertyFor`. The `epi-property` tag helper produced empty output
  (cause not investigated).

## On-page edit

On-page edit is off by default in CMS 13 (the view selector only has All Properties and Visual Builder).
It is switched on in `appsettings.json`:

```json
"EPiServer": { "CmsUI": { "CmsFeature": { "SectionsVisibility": { "OnPageEditing": true } } } }
```

- Classic pages then get **On-Page Editing** in the view selector, with the Content Area editor and Personalize.
- Experiences do **not**: `ExperiencePageUIDescriptor` explicitly disables the `onpageedit` view for `ExperienceData`.
- The Visual Builder view is also shown for classic pages (properties on the left, preview on the right). It can be
  limited to experiences with `EPiServer:CmsUI:VisualBuilder:ShowVisualBuilderForAllContentTypes = false` (not tested).

## Other findings

- Audiences are opt-in in CMS 13: the package `EPiServer.CMS.UI.VisitorGroups` plus
  `AddVisitorGroupsMvc().AddVisitorGroupsUI()`.
- No experience or section types are included; `BlankExperience` and `BlankSection` are defined in
  `Models/Pages/VisualBuilderTypes.cs`.
- A grid section cannot be saved as a shared block ("can only be used within experiences or as a blueprint").
  A shared block used as a section has to be a regular block type with `SectionEnabled`.
- `<epi-outline>` throws a NullReferenceException for an experience without a layout (guarded in
  `Views/Experience/Index.cshtml`).
- The CMS MCP server (`Optimizely.Cms.Opal.Tools` 13.4, `http://localhost:5080/mcp`, see `.mcp.json`):
  - has no tools for access rights, users or audiences,
  - stores a `reference` component in a composition as an inline **copy**, not as a shared reference,
  - fails on XhtmlString in such copies ("The XhtmlString instance is read-only"),
  - requires `properties` and `composition` as JSON strings,
  - runs as the role `CmsAdmins`, which needs access rights in the content tree.

## Scripts

`scripts/` shows how the content was first created through the MCP server (`seed_content.py`, keys in `keys.json`).
They are not needed when the database is restored from `db/Cms13Poc.bak`.
