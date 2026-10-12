using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Enums;
using UnityEngine;

namespace GreenHellCraftingHelper
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "immortalsouul.greenhell.craftinghelper";
        public const string Name = "Crafting Helper";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;
        internal static Plugin Instance;

        internal ConfigEntry<KeyboardShortcut> ToggleKey;
        internal ConfigEntry<bool> ShowAllRecipes;
        internal ConfigEntry<float> UiScale;
        internal ConfigEntry<string> Pinned;
        internal ConfigEntry<bool> ShowBackpackHint;

        private RecipeBook _book;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            ToggleKey = Config.Bind("Keys", "OpenRecipeBook", new KeyboardShortcut(KeyCode.K),
                "Open / close the recipe book.");
            ShowAllRecipes = Config.Bind("Recipes", "ShowAllRecipes", true,
                "true = every recipe in the game. false = only recipes already in your notebook (no spoilers).");
            UiScale = Config.Bind("UI", "Scale", 0f,
                "Window scale. 0 = automatic (based on screen height).");
            Pinned = Config.Bind("Recipes", "Pinned", "", "Pinned recipes (item IDs), saved automatically.");
            ShowBackpackHint = Config.Bind("UI", "ShowBackpackHint", true, "Show a small \"[K] Recipe Book\" hint while the backpack is open.");
            _book = gameObject.AddComponent<RecipeBook>();
            try { Patches.Apply(new HarmonyLib.Harmony(Guid)); }
            catch (Exception e) { Log.LogWarning("Harmony patches failed (menu/input guards off): " + e.Message); }
            Log.LogInfo($"{Name} {Version} loaded");
        }
    }
}
