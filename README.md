# Crafting Helper (Green Hell)

**English** | [Русский](README.ru.md)

![Crafting Helper](showcase/nexus/final/01_cover.jpg)

Green Hell gives you no way to see recipes until you've crafted them. A common complaint in recent negative Steam
reviews is that you need a wiki or YouTube open on a second screen just to know what to put on the crafting table.

Crafting Helper adds an in-game **recipe book**:

| | |
|---|---|
| **K** | Open / close the recipe book (any time: walking around or with the backpack open). |
| **Search** | Type an item **or an ingredient**: "stone" finds every recipe that uses stone. |
| **Can craft now** | Shows only what you can make right now from your backpack (and what you hold). |
| **Ingredients** | Shows `have / need` for every ingredient. If an ingredient has its own recipe, click **recipe** to jump to it. The **Used to craft** list shows what an item is used for. |
| **Put on crafting table** | One click puts all the ingredients on the crafting table, including heavy items you're carrying, like long sticks. Then just press **Craft**. |
| **Pin to screen** | Pin up to 5 recipes. They stay on screen with live `have / need` counts while you gather. |
| **Building** | Constructions (shelters, fire, racks...) with what they need. |

- Locked recipes (blueprints you haven't found) are marked **locked**.
- Uses the game's own item names and icons, so it follows the game's language. The mod's own text is in English
  and Russian.
- Esc first clears the search, then closes the book. While the book is open, keys don't trigger game actions.

## Install
1. Install **BepInEx 5** (x64): download `BepInEx_win_x64_5.4.23.x.zip` from
   [BepInEx releases](https://github.com/BepInEx/BepInEx/releases) and unzip it into the Green Hell folder (next to
   `GH.exe`). Start the game once and quit.
2. Copy the `GreenHellCraftingHelper` folder into `Green Hell\BepInEx\plugins\`.
3. Start the game and press **K**.

## Settings
`Green Hell\BepInEx\config\immortalsouul.greenhell.craftinghelper.cfg` (created after the first start):
- `OpenRecipeBook`: the hotkey (default `K`).
- `ShowAllRecipes`: `true` shows every recipe in the game. `false` shows only recipes already in your notebook
  (no spoilers).
- `Scale`: UI scale (`0` = automatic).
- `ShowBackpackHint`: the small "[K] Recipe Book" hint shown with the backpack open.

## Compatibility
- Tested on Green Hell **v2.9.5** (build 114117), Survival, single player.
- It changes no game files and doesn't touch your saves. Pinned recipes are stored in the config file.
- Multiplayer is untested. The crafting-table button uses the same steps as the game's own "Craft" action, so it
  should work for every player who has the mod.

## Build from source
Needs the .NET SDK. Set `GamePath` if Green Hell isn't in the default Steam folder:
```
dotnet build src -c Release -p:GamePath="D:\Steam\steamapps\common\Green Hell"
```
The DLL is copied into `BepInEx\plugins\GreenHellCraftingHelper` automatically.

## Uninstall
Delete `BepInEx\plugins\GreenHellCraftingHelper`.

## Credits
- Mod created by Claude (Anthropic AI) for [immortalsouul](https://www.nexusmods.com/profile/immortalsouul), who
  publishes it.
- BepInEx and HarmonyX by the BepInEx team.
- Green Hell by Creepy Jar.

## License
MIT (see `LICENSE`). This repository contains no game files.
