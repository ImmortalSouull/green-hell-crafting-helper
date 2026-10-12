using System;
using System.Collections.Generic;
using System.Linq;
using Enums;
using UnityEngine;
using Event = UnityEngine.Event;

namespace GreenHellCraftingHelper
{
    /// <summary>The recipe book window (IMGUI) and the pinned-recipe HUD.</summary>
    internal class RecipeBook : MonoBehaviour
    {
        private enum Filter { All, CanCraft, Pinned, Building }

        private bool _open;
        private bool _weShowedCursor;
        private string _search = "";
        private Filter _filter = Filter.All;
        private Recipe _selected;
        private Vector2 _listScroll, _detailScroll;
        private readonly List<ItemID> _pinned = new List<ItemID>();
        private string _status;
        private float _statusUntil;
        private float _nextRefresh;
        private List<Recipe> _visible = new List<Recipe>();
        private int _canCraftCount;
        private readonly Stack<Recipe> _back = new Stack<Recipe>();

        private GUIStyle _win, _title, _row, _rowSel, _label, _small, _good, _bad, _muted, _btn, _tab, _tabOn, _field, _hud, _hudTitle;
        private Texture2D _bgTex, _rowSelTex, _btnTex, _btnHoverTex, _hudTex, _tabOnTex;
        private float _scale = 1f;

        public static bool IsOpen;
        public static int ClosedFrame = -100;
        public static Rect WindowRect;

        // ------------------------------------------------------------------ lifecycle

        private void Start()
        {
            foreach (var t in (Plugin.Instance.Pinned.Value ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                if (Enum.IsDefined(typeof(ItemID), t.Trim())) _pinned.Add((ItemID)Enum.Parse(typeof(ItemID), t.Trim()));
        }

        private void SavePins() => Plugin.Instance.Pinned.Value = string.Join(",", _pinned.Select(p => p.ToString()));

        private void Update()
        {
            if (!InGame()) { if (_open) Close(); return; }
            if (Plugin.Instance.ToggleKey.Value.IsDown() && !TypingInGameField() && (_open || !GameScreenOpen()))
            {
                if (_open) Close(); else Open();
            }

            if (_open && Time.unscaledTime >= _nextRefresh) { Rebuild(); _nextRefresh = Time.unscaledTime + 0.5f; }
        }

        private static bool InGame()
        {
            try
            {
                return Player.Get() != null && ItemsManager.Get() != null && !LoadingScreen.Get().m_Active
                       && !MainMenuManager.Get();
            }
            catch { return false; }
        }

        /// <summary>Pause menu, notebook, map, dialogs, sleep... anything that should hide our HUD.</summary>
        private static bool GameScreenOpen()
        {
            try
            {
                if (MenuInGameManager.Get().IsAnyScreenVisible()) return true;
                if (SleepController.Get() != null && SleepController.Get().IsActive()) return true;
                if (Player.Get().IsDead()) return true;
            }
            catch { }
            return false;
        }

        private static bool BackpackOpen()
        {
            try { return Inventory3DManager.Get().IsActive(); } catch { return false; }
        }

        private static bool TypingInGameField() => GUIUtility.keyboardControl != 0 && IsOpen;

        private void Open()
        {
            _open = IsOpen = true;
            RecipeData.Invalidate();
            Rebuild();
            if (_visible.Count == 0 && _filter == Filter.CanCraft) { _filter = Filter.All; Rebuild(); }
            if (_selected == null) _selected = _visible.FirstOrDefault();
            // show the mouse unless the backpack/crafting already does
            if (!Inventory3DManager.Get().IsActive())
            {
                Player.Get().BlockRotation();
                Player.Get().BlockMoves();
                CursorManager.Get().ShowCursor(true);
                _weShowedCursor = true;
            }
        }

        private void Close()
        {
            _open = IsOpen = false;
            ClosedFrame = Time.frameCount;
            GUIUtility.keyboardControl = 0;
            if (_weShowedCursor)
            {
                try
                {
                    Player.Get().UnblockRotation();
                    Player.Get().UnblockMoves();
                    CursorManager.Get().ShowCursor(false);
                }
                catch { }
                _weShowedCursor = false;
            }
        }

        private void Rebuild()
        {
            var showAll = Plugin.Instance.ShowAllRecipes.Value;
            var q = _search.Trim().ToLowerInvariant();
            IEnumerable<Recipe> src = RecipeData.All;
            switch (_filter)
            {
                case Filter.All: src = src.Where(r => !r.IsConstruction); break;
                case Filter.Building: src = src.Where(r => r.IsConstruction); break;
                case Filter.CanCraft: src = src.Where(RecipeData.CanCraftNow); break;
                case Filter.Pinned: src = src.Where(r => _pinned.Contains(r.Id)); break;
            }
            if (!showAll) src = src.Where(RecipeData.IsKnown);
            if (q.Length > 0)
                src = src.Where(r => r.NameLower.Contains(q) || r.Ingredients.Any(i => RecipeData.NameOf(i.Key).ToLowerInvariant().Contains(q)));
            _visible = src.ToList();
            _canCraftCount = RecipeData.All.Count(r => (showAll || RecipeData.IsKnown(r)) && RecipeData.CanCraftNow(r));
        }

        private void Toast(string s)
        {
            _status = s;
            _statusUntil = Time.unscaledTime + 3.5f;
        }

        // ------------------------------------------------------------------ drawing

        private void OnGUI()
        {
            if (!InGame()) return;
            EnsureStyles();
            if (_open && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Event.current.Use();
                if (GUIUtility.keyboardControl != 0 && _search.Length > 0) { _search = ""; Rebuild(); GUIUtility.keyboardControl = 0; }
                else Close();
                return;
            }
            var m = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_scale, _scale, 1f));
            float sw = Screen.width / _scale, sh = Screen.height / _scale;

            bool hideHud = GameScreenOpen();
            if (_pinned.Count > 0 && !_open && !hideHud) DrawPinnedHud(sw);
            if (!_open && !hideHud && Plugin.Instance.ShowBackpackHint.Value && BackpackOpen())
            {
                var hint = $"[{Plugin.Instance.ToggleKey.Value}]  {T._("Recipe Book hint")}";
                var size = _hudTitle.CalcSize(new GUIContent(hint));
                var hr = new Rect(sw / 2f - size.x / 2f - 12, 14, size.x + 24, size.y + 8);
                GUI.Box(hr, GUIContent.none, _hud);
                GUI.Label(new Rect(hr.x + 12, hr.y + 4, size.x, size.y), hint, _hudTitle);
            }
            if (_open)
            {
                float w = Mathf.Min(980, sw - 40), h = Mathf.Min(640, sh - 80);
                var rect = new Rect((sw - w) / 2f, (sh - h) / 2f, w, h);
                WindowRect = new Rect(rect.x * _scale, rect.y * _scale, rect.width * _scale, rect.height * _scale);
                GUI.Box(rect, GUIContent.none, _win);
                GUILayout.BeginArea(new Rect(rect.x + 16, rect.y + 12, rect.width - 32, rect.height - 24));
                DrawHeader();
                GUILayout.Space(8);
                GUILayout.BeginHorizontal();
                DrawList(w * 0.42f, h - 150);
                GUILayout.Space(12);
                DrawDetails(h - 196);
                GUILayout.EndHorizontal();
                DrawFooter();
                GUILayout.EndArea();
            }
            GUI.matrix = m;
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(T._("Recipe Book"), _title);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"[{Plugin.Instance.ToggleKey.Value}] / Esc  {T._("close")}", _muted);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("chsearch");
            var ns = GUILayout.TextField(_search, 40, _field, GUILayout.Width(300), GUILayout.Height(30));
            if (ns != _search)
            {
                _search = ns; Rebuild();
                if (_visible.Count == 0 && _search.Length > 0 && _filter == Filter.CanCraft) { _filter = Filter.All; Rebuild(); }
                if (_selected == null || !_visible.Contains(_selected)) { _selected = _visible.FirstOrDefault(); _detailScroll = Vector2.zero; }
                _listScroll = Vector2.zero;
            }
            if (_search.Length == 0 && GUI.GetNameOfFocusedControl() != "chsearch")
            {
                var last = GUILayoutUtility.GetLastRect();
                GUI.Label(new Rect(last.x + 8, last.y + 5, last.width, last.height), T._("Search item or ingredient..."), _muted);
            }
            GUILayout.Space(10);
            TabButton(T._("All items"), Filter.All);
            TabButton($"{T._("Can craft now")} ({_canCraftCount})", Filter.CanCraft);
            TabButton($"{T._("Pinned")} ({_pinned.Count})", Filter.Pinned);
            TabButton(T._("Building"), Filter.Building);
            GUILayout.EndHorizontal();
        }

        private void TabButton(string text, Filter f)
        {
            if (GUILayout.Button(text, _filter == f ? _tabOn : _tab, GUILayout.Height(30)))
            {
                _filter = f; Rebuild();
                if (_selected == null || !_visible.Contains(_selected)) _selected = _visible.FirstOrDefault();
            }
        }

        private void DrawList(float width, float height)
        {
            GUILayout.BeginVertical(GUILayout.Width(width));
            _listScroll = GUILayout.BeginScrollView(_listScroll, GUILayout.Height(height));
            if (_visible.Count == 0)
                GUILayout.Label(_filter == Filter.Pinned ? T._("Nothing pinned yet.") : T._("No recipes found."), _muted);
            foreach (var r in _visible)
            {
                bool sel = r == _selected;
                GUILayout.BeginHorizontal(sel ? _rowSel : _row, GUILayout.Height(34));
                DrawIcon(r.Info, 28);
                GUILayout.Label(r.Name, _label, GUILayout.ExpandWidth(true));
                if (!r.IsConstruction)
                {
                    if (RecipeData.IsLocked(r)) GUILayout.Label(T._("locked"), _muted);
                    else if (RecipeData.CanCraftNow(r)) GUILayout.Label(T._("ready"), _good);
                    else GUILayout.Label($"-{RecipeData.MissingCount(r)}", _bad);
                }
                GUILayout.EndHorizontal();
                var rr = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && rr.Contains(Event.current.mousePosition))
                {
                    if (_selected != null && _selected != r) _back.Push(_selected);
                    _selected = r; _detailScroll = Vector2.zero; Event.current.Use();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.Label($"{_visible.Count} {T._("recipes")}", _small);
            GUILayout.EndVertical();
        }

        private void DrawDetails(float height)
        {
            GUILayout.BeginVertical();
            _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUILayout.Height(height));
            var r = _selected;
            if (r == null) { GUILayout.Label(T._("Select a recipe on the left."), _muted); GUILayout.EndScrollView(); GUILayout.EndVertical(); return; }

            GUILayout.BeginHorizontal();
            DrawIcon(r.Info, 64);
            GUILayout.BeginVertical();
            GUILayout.Label(r.Name, _title);
            if (r.IsConstruction) GUILayout.Label(T._("Building: place it with the notebook (construction tab)."), _muted);
            else if (RecipeData.IsLocked(r)) GUILayout.Label(T._("Locked: you need to find this blueprint first."), _bad);
            else if (RecipeData.CanCraftNow(r)) GUILayout.Label(T._("You have everything to craft this."), _good);
            else GUILayout.Label(T._("You're missing some ingredients."), _bad);
            if (!RecipeData.IsKnown(r)) GUILayout.Label(T._("Not in your notebook yet."), _small);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.Label(T._("Ingredients"), _label);
            foreach (var ing in r.Ingredients)
            {
                int have = RecipeData.Have(ing.Key);
                var info = ItemsManager.Get().GetInfo(ing.Key);
                GUILayout.BeginHorizontal(_row, GUILayout.Height(32));
                DrawIcon(info, 26);
                GUILayout.Label($"{ing.Value} x {RecipeData.NameOf(ing.Key)}", _label, GUILayout.ExpandWidth(true));
                GUILayout.Label($"{have}/{ing.Value}", have >= ing.Value ? _good : _bad, GUILayout.Width(60));
                var sub = RecipeData.RecipeFor(ing.Key);
                if (sub != null && !sub.IsConstruction)
                {
                    if (GUILayout.Button(T._("recipe"), _btn, GUILayout.Width(70)))
                    { _back.Push(r); _selected = sub; _detailScroll = Vector2.zero; }
                }
                else GUILayout.Space(74);
                GUILayout.EndHorizontal();
            }

            var used = RecipeData.UsedIn(r.Id).Where(x => !x.IsConstruction || _filter == Filter.Building).Take(12).ToList();
            if (used.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label(T._("Used to craft"), _label);
                GUILayout.BeginHorizontal();
                int col = 0;
                foreach (var u in used)
                {
                    if (GUILayout.Button(u.Name, _btn, GUILayout.MaxWidth(170))) { _back.Push(r); _selected = u; _detailScroll = Vector2.zero; }
                    if (++col % 3 == 0) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (_back.Count > 0 && GUILayout.Button(T._("< Back"), _btn, GUILayout.Width(80), GUILayout.Height(34))) _selected = _back.Pop();
            GUILayout.FlexibleSpace();
            bool pinned = _pinned.Contains(r.Id);
            if (GUILayout.Button(pinned ? T._("Unpin") : T._("Pin to screen"), _btn, GUILayout.Width(130), GUILayout.Height(34)))
            {
                if (pinned) _pinned.Remove(r.Id); else if (_pinned.Count < 5) _pinned.Add(r.Id); else Toast(T._("You can pin up to 5 recipes"));
                SavePins();
            }
            if (!r.IsConstruction)
            {
                GUI.enabled = !RecipeData.IsLocked(r) && RecipeData.CanCraftNow(r);
                if (GUILayout.Button(T._("Put on crafting table"), _btn, GUILayout.Width(200), GUILayout.Height(34)))
                {
                    string err = null;
                    try { err = TableFiller.Fill(r); }
                    catch (Exception e) { err = "Error: " + e.Message; Plugin.Log.LogError(e); }
                    if (err == null) { Toast($"{r.Name}: {T._("ingredients on the table, press Craft")}"); Close(); }
                    else Toast(err);
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            GUILayout.FlexibleSpace();
            if (_status != null && Time.unscaledTime < _statusUntil) GUILayout.Label(_status, _label);
            else GUILayout.Label(Plugin.Instance.ShowAllRecipes.Value ? T._("Showing every recipe in the game.") : T._("Showing only recipes from your notebook."), _small);
        }

        private void DrawPinnedHud(float sw)
        {
            const float w = 280;
            // left edge normally; when the backpack is open it covers the left third, so sit next to it
            float x = BackpackOpen() ? sw * 0.31f : 20, y = 160;
            foreach (var id in _pinned.ToList())
            {
                var r = RecipeData.RecipeFor(id);
                if (r == null) continue;
                float h = 30 + r.Ingredients.Count * 22;
                GUI.Box(new Rect(x, y, w, h), GUIContent.none, _hud);
                GUI.Label(new Rect(x + 10, y + 4, w - 20, 24), r.Name, _hudTitle);
                float yy = y + 28;
                foreach (var ing in r.Ingredients)
                {
                    int have = RecipeData.Have(ing.Key);
                    GUI.Label(new Rect(x + 14, yy, w - 80, 22), RecipeData.NameOf(ing.Key), _small);
                    GUI.Label(new Rect(x + w - 64, yy, 54, 22), $"{have}/{ing.Value}", have >= ing.Value ? _good : _bad);
                    yy += 22;
                }
                y += h + 8;
            }
            if (_status != null && Time.unscaledTime < _statusUntil)
                GUI.Label(new Rect(x, y, w, 30), _status, _hudTitle);
        }

        private void DrawIcon(ItemInfo info, float size)
        {
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            if (info == null || Event.current.type != EventType.Repaint) return;
            Sprite sp = null;
            try { ItemsManager.Get().m_ItemIconsSprites.TryGetValue(info.m_IconName ?? "", out sp); } catch { }
            if (sp == null || sp.texture == null) return;
            var tr = sp.textureRect;
            var tex = sp.texture;
            var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            float aspect = tr.width / tr.height;
            var dst = aspect >= 1 ? new Rect(rect.x, rect.y + (size - size / aspect) / 2, size, size / aspect)
                                  : new Rect(rect.x + (size - size * aspect) / 2, rect.y, size * aspect, size);
            GUI.DrawTextureWithTexCoords(dst, tex, uv);
        }

        // ------------------------------------------------------------------ styles

        private static Texture2D Tex(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c); t.Apply();
            return t;
        }

        private void EnsureStyles()
        {
            float auto = Mathf.Clamp(Screen.height / 1080f, 0.8f, 2.5f);
            var cfg = Plugin.Instance.UiScale.Value;
            _scale = cfg > 0.1f ? cfg : auto;
            if (_win != null) return;
            _bgTex = Tex(new Color(0.09f, 0.08f, 0.06f, 0.96f));
            _rowSelTex = Tex(new Color(0.42f, 0.33f, 0.16f, 0.9f));
            _btnTex = Tex(new Color(0.24f, 0.2f, 0.14f, 1f));
            _btnHoverTex = Tex(new Color(0.36f, 0.29f, 0.17f, 1f));
            _hudTex = Tex(new Color(0.05f, 0.04f, 0.03f, 0.78f));
            _tabOnTex = Tex(new Color(0.75f, 0.58f, 0.25f, 1f));
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 18);
            _win = new GUIStyle { normal = { background = _bgTex }, border = new RectOffset(2, 2, 2, 2) };
            _label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, normal = { textColor = new Color(0.95f, 0.92f, 0.85f) }, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            _title = new GUIStyle(_label) { fontSize = 26, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.98f, 0.82f, 0.45f) } };
            _small = new GUIStyle(_label) { fontSize = 15, normal = { textColor = new Color(0.85f, 0.82f, 0.75f) } };
            _muted = new GUIStyle(_small) { normal = { textColor = new Color(0.65f, 0.62f, 0.55f) } };
            _good = new GUIStyle(_label) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.55f, 0.9f, 0.45f) }, alignment = TextAnchor.MiddleRight };
            _bad = new GUIStyle(_good) { normal = { textColor = new Color(1f, 0.5f, 0.4f) } };
            _row = new GUIStyle { padding = new RectOffset(6, 6, 2, 2), margin = new RectOffset(0, 0, 1, 1) };
            _rowSel = new GUIStyle(_row) { normal = { background = _rowSelTex } };
            _btn = new GUIStyle(GUI.skin.button) { font = font, fontSize = 16, normal = { background = _btnTex, textColor = Color.white }, hover = { background = _btnHoverTex, textColor = Color.white }, active = { background = _rowSelTex, textColor = Color.white } };
            _tab = new GUIStyle(_btn) { padding = new RectOffset(12, 12, 4, 4) };
            _tabOn = new GUIStyle(_tab) { normal = { background = _tabOnTex, textColor = Color.black }, hover = { background = _tabOnTex, textColor = Color.black } };
            _field = new GUIStyle(GUI.skin.textField) { font = font, fontSize = 18, padding = new RectOffset(8, 8, 4, 4), normal = { background = _btnTex, textColor = Color.white }, focused = { background = _btnHoverTex, textColor = Color.white } };
            _hud = new GUIStyle { normal = { background = _hudTex } };
            _hudTitle = new GUIStyle(_label) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.98f, 0.82f, 0.45f) } };
        }
    }
}
