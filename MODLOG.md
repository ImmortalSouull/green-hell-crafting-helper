# Green Hell Crafting Helper — MODLOG

Goal: in-game recipe book / crafting assistant for **Green Hell** (Steam 815370).

Why (2026-10-12 research):
- ~2.1k concurrent, Very Positive (35k reviews), active 2026 Nexus scene (104 mods).
- 1000 recent negative reviews: 81 about crafting/recipes, 46 "need a wiki/guide/YouTube to play",
  122 about the tutorial. Quote: "you will need a separate device open at all times in order to research how to craft anything".
- Nexus has no recipe browser / crafting helper (only mods that ADD recipes).

Planned features:
1. Recipe book window (hotkey): search every craftable item, show ingredients.
2. Mark what you can craft right now from your backpack.
3. One click: put the ingredients on the crafting table.
4. Pin a recipe to a small HUD list while gathering.

## Environment
- Tools: .NET 8 SDK in `%USERPROFILE%\.dotnet`; ilspycmd 8.2 (`DOTNET_ROLL_FORWARD=Major`).
- Game: downloading (5.9 GB).

## Log

### 2026-10-12 v1.0.0 built and verified in game (Green Hell v2.9.5 build 114117, Unity 2021.2 Mono)
- Route: BepInEx 5.4.23.5 x64 plugin (C#, net472) + HarmonyX. Decompiled Assembly-CSharp with ilspycmd 8.2 -> `~/gh-decomp/acs`.
- Recipes: `ItemsManager.GetAllInfos()`, `ItemInfo.m_Components` (Dictionary<int ItemID, count>). 135 recipes = 80 items + 55 constructions.
  Several molds share a display name -> disambiguated from ItemID tokens.
- Locked: `ItemsManager.m_CraftingLockedItems`. Known: `m_UnlockedInNotepadItems` + private `m_WasCraftedData`.
- Icons: `ItemsManager.m_ItemIconsSprites[info.m_IconName]` (atlas sprites, draw with textureRect UVs).
- Put on table = replicate HUDItem.Action.Craft: remove from slot/hands/backpack, `CraftingManager.Activate()`, `AddItem(item, setup_pos:true)`, then `SetupResult()`.
  GOTCHA: items in a backpack pocket that's not shown are SetActive(false) -> invisible on the table; re-activate.
- Harmony: `MenuInGameManager.CanShowMenuInGame` postfix (Esc closing the book must not open pause) and
  `InputsManager.UpdateActions` prefix (no game hotkeys while typing in search).
- Verified: open/search/filter/select, put-on-table + Craft (Axe, Stone Spear with held Long_Stick), cancel returns items,
  pin HUD live counters, pins persisted in cfg, backpack hint, HUD hidden under pause menu, Russian UI via game language.
- Dev tooling (not shipped): `devtools/` GHDevTools plugin (file command bridge: give/clearbp/dump/hold/lang/tablepos),
  `_bridge/loadtest.sh` loads SP slot 2 (test save). Disabled after testing (moved to `_tools/disabled`).
- Showcase: `showcase/gh_crafting_helper_16x9.mp4`, `showcase/gh_crafting_helper_tiktok.mp4` (14 MB).
