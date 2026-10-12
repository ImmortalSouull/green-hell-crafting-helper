using System.Collections.Generic;

namespace GreenHellCraftingHelper
{
    /// <summary>UI text. Follows the game's language setting (English and Russian built in).</summary>
    internal static class T
    {
        private static readonly Dictionary<string, string> Ru = new Dictionary<string, string>
        {
            ["Recipe Book"] = "Книга рецептов",
            ["close"] = "закрыть",
            ["Search item or ingredient..."] = "Поиск предмета или ингредиента...",
            ["All items"] = "Все предметы",
            ["Can craft now"] = "Можно скрафтить",
            ["Pinned"] = "Закреплено",
            ["Building"] = "Постройки",
            ["Nothing pinned yet."] = "Пока ничего не закреплено.",
            ["No recipes found."] = "Рецептов не найдено.",
            ["Select a recipe on the left."] = "Выберите рецепт слева.",
            ["Building: place it with the notebook (construction tab)."] = "Постройка: ставится через блокнот (вкладка строительства).",
            ["Locked: you need to find this blueprint first."] = "Закрыто: сначала найдите этот чертёж.",
            ["You have everything to craft this."] = "У вас есть всё для крафта.",
            ["You're missing some ingredients."] = "Не хватает ингредиентов.",
            ["Not in your notebook yet."] = "Ещё нет в вашем блокноте.",
            ["Ingredients"] = "Ингредиенты",
            ["Used to craft"] = "Используется в",
            ["recipe"] = "рецепт",
            ["Unpin"] = "Открепить",
            ["Pin to screen"] = "Закрепить",
            ["Put on crafting table"] = "Выложить на стол",
            ["< Back"] = "< Назад",
            ["You can pin up to 5 recipes"] = "Можно закрепить до 5 рецептов",
            ["Showing every recipe in the game."] = "Показаны все рецепты игры.",
            ["Showing only recipes from your notebook."] = "Показаны только рецепты из блокнота.",
            ["recipes"] = "рецептов",
            ["ready"] = "готово",
            ["locked"] = "закрыто",
            ["Recipe Book hint"] = "Книга рецептов",
            ["ingredients on the table, press Craft"] = "ингредиенты на столе, нажмите «Создать»",
            ["Not in game"] = "Вы не в игре",
            ["You can't craft right now"] = "Сейчас крафтить нельзя",
            ["Already crafting"] = "Крафт уже идёт",
            ["Recipe locked: find its blueprint first"] = "Рецепт закрыт: сначала найдите чертёж",
            ["Missing"] = "Не хватает",
            ["the basic"] = "базовая",
        };

        public static bool IsRussian
        {
            get
            {
                try { return GreenHellGame.Instance.m_Settings.m_Language == Enums.Language.Russian; }
                catch { return false; }
            }
        }

        public static string _(string en)
        {
            if (IsRussian && Ru.TryGetValue(en, out var ru)) return ru;
            return en == "Recipe Book hint" ? "Recipe Book" : en;
        }
    }
}
