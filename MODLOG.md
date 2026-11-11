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

### 2026-11-11 v1.1.0 — fixes from Nexus comments (Green Hell v2.9.5 build 114117, BepInEx 5.4.23.5)
Comments: (1) "whenever I switch input methods, K stops bringing up the menu"; (2) Fire Bow: put on table + Craft ->
endless crafting-hands loop, C (inspection wheel) disabled.
- (1) BepInEx `KeyboardShortcut.IsDown()` = GetKeyDown(main) AND no other supported KeyCode held
  (`_modifierBlockKeyCodes` = every KeyCode except mouse buttons -> includes all JoystickButtonN). A gamepad button /
  trigger that reads as held blocks the hotkey. Fix: own check (GetKeyDown(main) + its modifiers only).
  Verified: K with W held opens the book; switching the game to Pad (no pad connected -> game flips back to PC and
  opens the pause menu) -> Resume -> K works.
- (2) Fire_Bow = Fire_Board + Small_Stick + Bow + Stone. Reproduced: bow in left hand, backpack closed, book ->
  Put on table -> `CraftingManager.Activate()` asks for the Crafting controller, but `SetWantedItem(Hand.Left, null)`
  (bow leaving the hand) makes the game re-pick the controller (FPP / FistFight) -> table shown, Crafting controller
  never enabled. Craft -> `CraftingController.StartCrafting` on a disabled controller -> m_InProgress, moves/rotation
  blocked, the `CraftingEnd` anim event never arrives (only active controllers get it) -> stuck forever.
  With the backpack already open (Inventory controller active) the same fill works.
- Fix: RecipeBook.PutOnTable coroutine: Close book -> Inventory3DManager.Activate() -> wait for the Inventory
  controller -> TableFiller.Fill -> 0.75 s later make sure Crafting is the controller (StartController otherwise).
  Harmony prefix on CraftingManager.StartCrafting: if !CraftingController.enabled (and not a multi-craft continuation)
  -> StartController(Crafting) and skip this press. Watchdog: m_InProgress && !enabled for 1.5 s ->
  FinishCrafting(false) via reflection (game's own cancel: items back to the backpack, unblocks). All three verified.
- Status toasts are drawn at the top when the book is closed and nothing is pinned (before: invisible).
- Dev tooling (devtools/, gitignored): GHDev plugin, file commands in BepInEx/ghdev_cmd.txt (load/give/hold/bp/book/
  put/fill/craft/craftnow/rawcraft/stopcraft/dump/ctrl/unet/host/connect/peers). Load from the main menu =
  `SaveGame.s_MainSaveName` + `m_GHGameMode` + `MainMenuManager.CallLoadGameFadeSequence()` (SaveGame.Load alone hangs).
- Co-op test on one PC: copy `C:\Users\PC\GH_B` (exe, UnityPlayer, BepInEx; GH_Data + MonoBleedingEdge junctions,
  steam_appid.txt). Both load a save, then on both `unet` (P2PSession.s_TransportLayerType + P2PTransportLayer.s_Instance
  -> TransportLayerUNet), host: `host 7777`, client: `host 7778` + `connect 127.0.0.1 7777` (JoinLobby shuts the
  transport down first, don't use it). Connected, 2 logical players; Put on table + Craft (bow in hand) works on both.
- Saves: Steam cloud folder `Steam\userdata\<id>\815370\remote` (SPSlot0/1, MPSlot0, Settings2.sav).
  Second copy launched windowed writes the resolution to HKCU\Software\Creepy Jar\Green Hell (restored after tests).
