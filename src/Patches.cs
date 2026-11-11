using HarmonyLib;
using UnityEngine;

namespace GreenHellCraftingHelper
{
    internal static class Patches
    {
        public static void Apply(Harmony h)
        {
            h.PatchAll(typeof(Patches));
        }

        // Esc that closes the recipe book must not also open the pause menu.
        [HarmonyPatch(typeof(MenuInGameManager), "CanShowMenuInGame")]
        [HarmonyPostfix]
        private static void NoPauseWhileBook(ref bool __result)
        {
            if (RecipeBook.IsOpen || Time.frameCount - RecipeBook.ClosedFrame <= 2) __result = false;
        }

        // Craft pressed while the crafting controller isn't running would start an animation whose end event
        // nobody receives (endless crafting hands). Start the controller instead; the next press crafts.
        [HarmonyPatch(typeof(CraftingManager), "StartCrafting")]
        [HarmonyPrefix]
        private static bool CraftOnlyWithCraftingController(bool continuation)
        {
            var cc = CraftingController.Get();
            if (continuation || cc == null || cc.enabled) return true;
            Plugin.Log.LogWarning("Craft pressed while the crafting controller wasn't running; starting it first");
            Player.Get()?.StartController(PlayerControllerType.Crafting);
            return false;
        }

        // While the book is open, keys go to the book (search field), not to game actions
        // (N = notebook, Q, digits, etc.).
        [HarmonyPatch(typeof(InputsManager), "UpdateActions")]
        [HarmonyPrefix]
        private static bool NoGameActionsWhileBook()
        {
            return !RecipeBook.IsOpen;
        }
    }
}
