using System.Collections.Generic;
using System.Linq;
using Enums;
using UnityEngine;

namespace GreenHellCraftingHelper
{
    /// <summary>Moves a recipe's ingredients from the backpack onto the crafting table,
    /// using the same steps as the game's own "Craft" context action (HUDItem.Action.Craft).</summary>
    internal static class TableFiller
    {
        public static string Fill(Recipe r)
        {
            var player = Player.Get();
            var cm = CraftingManager.Get();
            var bp = InventoryBackpack.Get();
            if (player == null || cm == null || bp == null) return T._("Not in game");
            if (!player.CanStartCrafting()) return T._("You can't craft right now");
            if (CraftingController.Get() != null && CraftingController.Get().m_InProgress) return T._("Already crafting");
            if (RecipeData.IsLocked(r)) return T._("Recipe locked: find its blueprint first");

            // 1) what is already on the table and fits the recipe stays; everything else goes back
            var need = r.Ingredients.ToDictionary(k => k.Key, k => k.Value);
            if (cm.IsActive())
            {
                foreach (var it in cm.m_Items.ToList())
                {
                    if (it == null) continue;
                    var id = it.m_Info.m_ID;
                    if (need.TryGetValue(id, out var n) && n > 0) { need[id] = n - 1; continue; }
                    cm.RemoveItem(it);
                    if (it.m_Info.IsHeavyObject() || !it.Take()) Inventory3DManager.Get().DropItem(it);
                }
            }

            // 2) check everything is there before moving anything
            var plan = new List<Item>();
            foreach (var kv in need)
            {
                if (kv.Value <= 0) continue;
                var found = FindItems(kv.Key, kv.Value, plan);
                if (found.Count < kv.Value)
                    return $"{T._("Missing")} {kv.Value - found.Count} x {RecipeData.NameOf(kv.Key)}";
                plan.AddRange(found);
            }

            // 3) move them, exactly like the game's Craft action does
            int i = 0;
            foreach (var item in plan) MoveToTable(item, i++, plan.Count);
            if (cm.IsActive()) cm.SetupResult();
            return null;
        }

        private static List<Item> FindItems(ItemID id, int count, List<Item> exclude)
        {
            var res = new List<Item>();
            var p = Player.Get();
            // backpack first (lightest items first is how the game stacks them anyway)
            foreach (var it in InventoryBackpack.Get().m_Items)
            {
                if (res.Count >= count) break;
                if (it == null || it.m_Info.m_ID != id || exclude.Contains(it) || res.Contains(it)) continue;
                if (it.m_OnCraftingTable) continue;
                res.Add(it);
            }
            // then whatever is in the hands (e.g. a heavy log being carried)
            foreach (Hand h in new[] { Hand.Right, Hand.Left })
            {
                if (res.Count >= count) break;
                var it = p.GetCurrentItem(h);
                if (it != null && it.m_Info.m_ID == id && !exclude.Contains(it) && !res.Contains(it) && !it.m_OnCraftingTable)
                    res.Add(it);
            }
            return res;
        }

        private static void MoveToTable(Item item, int index, int count)
        {
            var player = Player.Get();
            if (WalkieTalkieController.Get() != null && WalkieTalkieController.Get().IsActive())
                WalkieTalkieController.Get().Stop();
            if (player.GetCurrentItem(Hand.Left) == item) player.SetWantedItem(Hand.Left, null);
            else if (player.GetCurrentItem(Hand.Right) == item) player.SetWantedItem(Hand.Right, null);
            else if (!item.m_CurrentSlot && (bool)item.m_InventorySlot && item.m_InventorySlot.m_Items.Count > 0)
                item.m_InventorySlot.RemoveItem(item);
            else if ((bool)item.m_CurrentSlot && item.m_CurrentSlot.m_InventoryStackSlot)
                item.m_CurrentSlot.RemoveItem(item);
            else if ((bool)item.m_CurrentSlot && (bool)item.m_CurrentSlot.m_WeaponRackParent)
                item.m_CurrentSlot.RemoveItem(item);
            if (InventoryBackpack.Get().m_EquippedItem == item) InventoryBackpack.Get().m_EquippedItem = null;
            InventoryBackpack.Get().RemoveItem(item);
            Storage3D.Get()?.RemoveItem(item);
            // items from a backpack pocket that isn't on screen are deactivated by the game; on the table they must show
            if (!item.gameObject.activeSelf) item.gameObject.SetActive(true);
            CraftingManager.Get().Activate();
            CraftingManager.Get().AddItem(item, setup_pos: true, setup_result: false);
            // spread the items out a little so they don't all sit in one pile
            if (count > 1)
            {
                var table = CraftingManager.Get().m_TableCollider.transform;
                float spread = 0.09f;
                float off = (index - (count - 1) / 2f) * spread;
                item.transform.position += table.right * off + table.up * ((index % 2 == 0) ? 0.05f : -0.05f);
            }
        }
    }
}
