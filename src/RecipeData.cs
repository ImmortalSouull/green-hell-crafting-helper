using System;
using System.Collections.Generic;
using System.Linq;
using Enums;
using UnityEngine;

namespace GreenHellCraftingHelper
{
    /// <summary>One craftable item and what it needs.</summary>
    internal class Recipe
    {
        public ItemInfo Info;
        public ItemID Id;
        public string Name;
        public string NameLower;
        public bool IsConstruction;
        public List<KeyValuePair<ItemID, int>> Ingredients = new List<KeyValuePair<ItemID, int>>();
    }

    internal static class RecipeData
    {
        private static List<Recipe> _recipes;
        private static Dictionary<ItemID, Recipe> _byResult;
        private static Dictionary<ItemID, List<Recipe>> _usedIn;
        private static string _language;

        public static List<Recipe> All
        {
            get { EnsureBuilt(); return _recipes; }
        }

        public static Recipe RecipeFor(ItemID id)
        {
            EnsureBuilt();
            return _byResult.TryGetValue(id, out var r) ? r : null;
        }

        public static List<Recipe> UsedIn(ItemID id)
        {
            EnsureBuilt();
            return _usedIn.TryGetValue(id, out var l) ? l : new List<Recipe>();
        }

        public static void Invalidate() => _recipes = null;

        private static void EnsureBuilt()
        {
            var lang = GreenHellGame.Instance != null ? GreenHellGame.Instance.m_Settings.m_Language.ToString() : "";
            if (_recipes != null && lang == _language) return;
            _language = lang;
            _recipes = new List<Recipe>();
            _byResult = new Dictionary<ItemID, Recipe>();
            _usedIn = new Dictionary<ItemID, List<Recipe>>();
            var mgr = ItemsManager.Get();
            if (mgr == null) return;
            foreach (var info in mgr.GetAllInfos().Values)
            {
                if (info == null || info.m_Components == null || info.m_Components.Count == 0) continue;
                var r = new Recipe
                {
                    Info = info,
                    Id = info.m_ID,
                    Name = SafeName(info),
                    IsConstruction = info.IsConstruction(),
                };
                r.NameLower = r.Name.ToLowerInvariant();
                foreach (var kv in info.m_Components.OrderBy(k => k.Key))
                    r.Ingredients.Add(new KeyValuePair<ItemID, int>((ItemID)kv.Key, kv.Value));
                _recipes.Add(r);
                _byResult[r.Id] = r;
                foreach (var ing in r.Ingredients)
                {
                    if (!_usedIn.TryGetValue(ing.Key, out var list)) _usedIn[ing.Key] = list = new List<Recipe>();
                    list.Add(r);
                }
            }
            Disambiguate();
            _recipes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            Plugin.Log.LogInfo($"Recipe book: {_recipes.Count} recipes ({_recipes.Count(r => !r.IsConstruction)} items, {_recipes.Count(r => r.IsConstruction)} constructions)");
        }

        /// <summary>Several molds etc. share one display name ("Armor Mold"); add what makes each one different.</summary>
        private static void Disambiguate()
        {
            foreach (var g in _recipes.GroupBy(r => r.Name).Where(g => g.Count() > 1))
            {
                var tokens = g.ToDictionary(r => r, r => r.Id.ToString().ToLowerInvariant().Split('_').ToList());
                var common = tokens.Values.Aggregate((a, b) => a.Intersect(b).ToList());
                foreach (var r in g)
                {
                    var extra = tokens[r].Where(t => !common.Contains(t) && t != "baked").ToList();
                    bool baked = tokens[r].Contains("baked") && !common.Contains("baked");
                    var label = extra.Count > 0 ? string.Join(" ", extra.Select(Cap)) : "basic";
                    if (baked) label += ", baked";
                    r.Name = $"{r.Name} ({label})";
                    r.NameLower = r.Name.ToLowerInvariant();
                }
            }
        }

        private static string Cap(string t) => t.Length == 0 ? t : char.ToUpperInvariant(t[0]) + t.Substring(1);

        public static string SafeName(ItemInfo info)
        {
            try
            {
                var n = info.GetNameToDisplayLocalized();
                if (!string.IsNullOrEmpty(n) && n != info.m_ID.ToString()) return n;
            }
            catch { }
            return info.m_ID.ToString().Replace('_', ' ');
        }

        public static string NameOf(ItemID id)
        {
            var info = ItemsManager.Get()?.GetInfo(id);
            return info != null ? SafeName(info) : id.ToString().Replace('_', ' ');
        }

        // ---- knowledge / availability ------------------------------------------------------

        private static readonly System.Reflection.FieldInfo WasCraftedField =
            HarmonyLib.AccessTools.Field(typeof(ItemsManager), "m_WasCraftedData");

        public static bool IsLocked(Recipe r) => ItemsManager.Get().m_CraftingLockedItems.Contains(r.Id);

        public static bool IsKnown(Recipe r)
        {
            var m = ItemsManager.Get();
            if (m.m_UnlockedInNotepadItems.Contains(r.Id)) return true;
            var crafted = WasCraftedField?.GetValue(m) as List<ItemID>;
            return crafted != null && crafted.Contains(r.Id);
        }

        /// <summary>How many of this item the player can put on the table right now (backpack + table + hands).</summary>
        public static int Have(ItemID id)
        {
            int n = 0;
            var bp = InventoryBackpack.Get();
            if (bp != null) n += bp.GetItemsCount(id);
            var cm = CraftingManager.Get();
            if (cm != null && cm.IsActive())
                foreach (var it in cm.m_Items)
                    if (it != null && it.m_Info.m_ID == id) n++;
            var p = Player.Get();
            if (p != null)
            {
                foreach (Hand h in new[] { Hand.Left, Hand.Right })
                {
                    var it = p.GetCurrentItem(h);
                    if (it != null && it.m_Info.m_ID == id && (bp == null || !bp.m_Items.Contains(it))) n++;
                }
            }
            return n;
        }

        public static bool CanCraftNow(Recipe r)
        {
            if (r.IsConstruction || IsLocked(r)) return false;
            foreach (var ing in r.Ingredients)
                if (Have(ing.Key) < ing.Value) return false;
            return true;
        }

        public static int MissingCount(Recipe r)
        {
            int miss = 0;
            foreach (var ing in r.Ingredients) miss += Math.Max(0, ing.Value - Have(ing.Key));
            return miss;
        }
    }
}
