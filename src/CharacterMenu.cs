// Kreator wygladu postaci na ekranie wyboru zapisu (jak w Terrarii).
// Obok kazdego slotu widac postac z Terrarii. Na podswietlonym slocie [C] (albo klik w postac) otwiera edytor:
// gora/dol wybiera wiersz, lewo/prawo zmienia wartosc. Przy kolorach: lewo/prawo = odcien,
// Shift = jasnosc, Ctrl = nasycenie (albo suwaki mysza). Enter na "Gotowe" albo Esc zamyka i zapisuje.

using System.Collections.Generic;
using GlobalEnums;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TerrariaArsenal
{
    public class CharacterMenu : MonoBehaviour
    {
        public KeyCode OpenKey = KeyCode.C;

        private static readonly string[] Rows =
        {
            "Typ ciała", "Fryzura", "Kolor włosów", "Skóra", "Oczy", "Koszula", "Podkoszulek", "Spodnie", "Buty", "Losuj", "Gotowe"
        };
        private const int RowVariant = 0, RowHair = 1, RowFirstColor = 2, RowLastColor = 8, RowRandom = 9, RowDone = 10;

        private SaveSlotButton[] buttons = new SaveSlotButton[0];
        private float scanTimer;
        private readonly Dictionary<int, Texture2D> previews = new Dictionary<int, Texture2D>();
        private readonly Dictionary<int, int> previewVersion = new Dictionary<int, int>();

        // Edytor
        private bool open;
        private int editSlot;
        private Look edit;
        private int row;
        private Texture2D editPreview;
        private bool editDirty = true;
        private EventSystem blockedEvents;
        private InControl.HollowKnightInputModule blockedModule;
        private bool hadMouse;
        private int restoreIn = -1;        // menu HK odblokowane dopiero po klatce, zeby Esc/Enter nie zadzialal tez tam
        private GameObject reselect;
        private float repeatTimer;
        private Texture2D white;

        private bool OnSaveScreen()
        {
            UIManager ui = UIManager.instance;
            return ui != null && ui.menuState == MainMenuState.SAVE_PROFILES && TerrariaContent.Available;
        }

        private void Update()
        {
            if (restoreIn >= 0 && --restoreIn < 0) Unblock();
#if !RELEASE
            // Test okna sciezki bez odinstalowywania Terrarii.
            if (Input.GetKeyDown(KeyCode.F8) && UIManager.instance != null && UIManager.instance.menuState == MainMenuState.MAIN_MENU)
            {
                TerrariaFiles.ForceMissing = true;
                pathSkipped = false;
                TerrariaContent.ReloadAll();
            }
#endif
            bool needPath = NeedPathWindow();
            if (needPath && !pathBlocked)
            {
                Block();
                pathBlocked = true;
                string cur = TerrariaArsenal.Instance.Settings.TerrariaPath;
                pathInput = string.IsNullOrEmpty(cur) ? @"C:\Program Files (x86)\Steam\steamapps\common\Terraria" : cur;
            }
            else if (!needPath && pathBlocked)
            {
                pathBlocked = false;
                restoreIn = 2;
            }
            if (!OnSaveScreen())
            {
                if (open) Close(false);
                return;
            }

            scanTimer -= Time.unscaledDeltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = 1f;
                buttons = FindObjectsOfType<SaveSlotButton>();
            }

            if (!open)
            {
                if (Input.GetKeyDown(OpenKey))
                {
                    SaveSlotButton sel = Selected();
                    if (sel != null) Open(sel);
                }
                return;
            }
            EditorInput();
        }

        private SaveSlotButton Selected()
        {
            EventSystem es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject == null) return null;
            return es.currentSelectedGameObject.GetComponent<SaveSlotButton>();
        }

        private static int SlotOf(SaveSlotButton b)
        {
            return (int)b.saveSlot + 1;
        }

        private static bool Visible(SaveSlotButton b)
        {
            if (b == null || !b.gameObject.activeInHierarchy) return false;
            return b.myCanvasGroup == null || b.myCanvasGroup.alpha > 0.5f;
        }

        // --- Edytor ---
        private void Open(SaveSlotButton b)
        {
            open = true;
            editSlot = SlotOf(b);
            edit = PlayerLook.Get(editSlot).Copy();
            row = 0;
            editDirty = true;
            reselect = b.gameObject;
            Block();
            Sounds.Play("Menu_Open", 0.6f);
        }

        // Menu HK nie moze reagowac na klawisze ani mysz, kiedy nasze okno jest otwarte.
        private void Block()
        {
            restoreIn = -1;
            if (blockedEvents == null) blockedEvents = EventSystem.current;
            if (blockedEvents != null)
            {
                blockedEvents.sendNavigationEvents = false;
                if (blockedModule == null)
                {
                    blockedModule = blockedEvents.GetComponent<InControl.HollowKnightInputModule>();
                    if (blockedModule != null) hadMouse = blockedModule.allowMouseInput;
                }
                if (blockedModule != null) blockedModule.allowMouseInput = false;
            }
        }

        private void Close(bool save)
        {
            open = false;
            if (save)
            {
                PlayerLook.Set(editSlot, edit);
                TerrariaArsenal.Instance.SaveSettings();
            }
            restoreIn = 2;
            Sounds.Play("Menu_Close", 0.6f);
        }

        private void Unblock()
        {
            if (blockedEvents != null)
            {
                blockedEvents.sendNavigationEvents = true;
                if (reselect != null && reselect.activeInHierarchy) blockedEvents.SetSelectedGameObject(reselect);
            }
            if (blockedModule != null) blockedModule.allowMouseInput = hadMouse;
            blockedEvents = null;
            blockedModule = null;
        }

        private void EditorInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close(true);
                return;
            }
            if (Pressed(KeyCode.UpArrow, KeyCode.W)) Move(-1);
            if (Pressed(KeyCode.DownArrow, KeyCode.S)) Move(1);

            int dir = 0;
            bool held = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) ||
                        Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);
            if (Pressed(KeyCode.LeftArrow, KeyCode.A)) dir = -1;
            else if (Pressed(KeyCode.RightArrow, KeyCode.D)) dir = 1;
            else if (held && row >= RowFirstColor && row <= RowLastColor)
            {
                // Przytrzymanie przesuwa kolor plynnie.
                repeatTimer -= Time.unscaledDeltaTime;
                if (repeatTimer <= 0f)
                {
                    repeatTimer = 0.03f;
                    dir = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) ? -1 : 1;
                }
            }
            if (dir != 0)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
                    Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D)) repeatTimer = 0.3f;
                Change(dir);
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                if (row == RowRandom) Randomize();
                else if (row == RowDone) Close(true);
            }
        }

        private static bool Pressed(KeyCode a, KeyCode b)
        {
            return Input.GetKeyDown(a) || Input.GetKeyDown(b);
        }

        private void Move(int d)
        {
            row = (row + d + Rows.Length) % Rows.Length;
            Sounds.Play("Menu_Tick", 0.5f);
        }

        private void Change(int dir)
        {
            if (row == RowVariant)
            {
                int n = PlayerLook.VariantOrder.Length;
                int idx = System.Array.IndexOf(PlayerLook.VariantOrder, edit.Variant);
                if (idx < 0) idx = 0;
                idx = ((idx + dir) % n + n) % n;
                edit.Variant = PlayerLook.VariantOrder[idx];
            }
            else if (row == RowHair)
            {
                int h = edit.Hair;
                for (int i = 0; i < 300; i++)
                {
                    h += dir;
                    if (h < 1) h = PlayerLook.MaxHair;
                    if (h > PlayerLook.MaxHair) h = 1;
                    if (TerrariaContent.HasImage("Player_Hair_" + h)) break;
                }
                edit.Hair = h;
            }
            else if (row >= RowFirstColor && row <= RowLastColor)
            {
                Color c = GetColor(row);
                float hh, s, v;
                Color.RGBToHSV(c, out hh, out s, out v);
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                if (shift) v = Mathf.Clamp01(v + dir * 0.02f);
                else if (ctrl) s = Mathf.Clamp01(s + dir * 0.02f);
                else hh = Mathf.Repeat(hh + dir * 0.01f, 1f);
                SetColor(row, Color.HSVToRGB(hh, s, v));
            }
            else
            {
                return;
            }
            editDirty = true;
            Sounds.Play("Menu_Tick", 0.35f);
        }

        private Color GetColor(int r)
        {
            return Look.Parse(ColorField(r));
        }

        private string ColorField(int r)
        {
            switch (r)
            {
                case 2: return edit.HairColor;
                case 3: return edit.Skin;
                case 4: return edit.Eyes;
                case 5: return edit.Shirt;
                case 6: return edit.Undershirt;
                case 7: return edit.Pants;
                default: return edit.Shoes;
            }
        }

        private void SetColor(int r, Color c)
        {
            string hex = Look.Hex(c);
            switch (r)
            {
                case 2: edit.HairColor = hex; break;
                case 3: edit.Skin = hex; break;
                case 4: edit.Eyes = hex; break;
                case 5: edit.Shirt = hex; break;
                case 6: edit.Undershirt = hex; break;
                case 7: edit.Pants = hex; break;
                default: edit.Shoes = hex; break;
            }
        }

        private void Randomize()
        {
            edit.Variant = PlayerLook.VariantOrder[Random.Range(0, PlayerLook.VariantOrder.Length)];
            for (int i = 0; i < 50; i++)
            {
                edit.Hair = Random.Range(1, PlayerLook.MaxHair + 1);
                if (TerrariaContent.HasImage("Player_Hair_" + edit.Hair)) break;
            }
            // Skora: odcienie naturalne jak w Terrarii.
            edit.Skin = Look.Hex(Color.HSVToRGB(Random.Range(0.02f, 0.09f), Random.Range(0.35f, 0.75f), Random.Range(0.35f, 1f)));
            for (int r = RowFirstColor; r <= RowLastColor; r++)
            {
                if (r == 3) continue;
                SetColor(r, Color.HSVToRGB(Random.value, Random.Range(0.2f, 0.9f), Random.Range(0.3f, 1f)));
            }
            editDirty = true;
            Sounds.Play("Menu_Tick", 0.5f);
        }

        // --- Rysowanie ---
        private Texture2D Preview(int slot)
        {
            Texture2D t;
            int v;
            if (previews.TryGetValue(slot, out t) && previewVersion.TryGetValue(slot, out v) && v == PlayerLook.Version) return t;
            if (t != null) Destroy(t);
            t = PlayerLook.BuildPreview(PlayerLook.Get(slot));
            previews[slot] = t;
            previewVersion[slot] = PlayerLook.Version;
            return t;
        }

        private void Box(Rect r, Color c)
        {
            if (white == null)
            {
                white = new Texture2D(1, 1);
                white.SetPixel(0, 0, Color.white);
                white.Apply();
            }
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }

        // Panel w stylu Terrarii: ciemnoniebieskie tlo z jasniejsza ramka.
        private void Panel(Rect r)
        {
            Box(r, new Color(0.11f, 0.15f, 0.33f, 0.92f));
            float b = 2f;
            Color edge = new Color(0.55f, 0.62f, 0.95f, 0.9f);
            Box(new Rect(r.x, r.y, r.width, b), edge);
            Box(new Rect(r.x, r.yMax - b, r.width, b), edge);
            Box(new Rect(r.x, r.y, b, r.height), edge);
            Box(new Rect(r.xMax - b, r.y, b, r.height), edge);
        }

        private GUIStyle noticeStyle, textStyle, titleStyle, fieldStyle, buttonStyle;

        // --- Okno "podaj sciezke Terrarii" (gdy mod jej nie znalazl) ---
        private bool pathSkipped;
        private bool pathBlocked;
        private string pathInput = "";
        private string pathError = "";
        private float pathOkTime;

        private bool NeedPathWindow()
        {
            UIManager uim = UIManager.instance;
            return !pathSkipped && !TerrariaContent.Available && uim != null && uim.menuState == MainMenuState.MAIN_MENU;
        }

        private void Styles(float ui)
        {
            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label);
                textStyle.wordWrap = true;
                textStyle.normal.textColor = Color.white;
                titleStyle = new GUIStyle(textStyle);
                titleStyle.alignment = TextAnchor.UpperCenter;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.normal.textColor = new Color(1f, 0.85f, 0.4f);
                fieldStyle = new GUIStyle(GUI.skin.textField);
                buttonStyle = new GUIStyle(GUI.skin.button);
                noticeStyle = new GUIStyle(GUI.skin.label);
                noticeStyle.alignment = TextAnchor.UpperCenter;
                noticeStyle.wordWrap = true;
                noticeStyle.normal.textColor = new Color(1f, 0.45f, 0.35f);
            }
            textStyle.fontSize = Mathf.RoundToInt(22f * ui);
            titleStyle.fontSize = Mathf.RoundToInt(32f * ui);
            fieldStyle.fontSize = Mathf.RoundToInt(22f * ui);
            buttonStyle.fontSize = Mathf.RoundToInt(22f * ui);
            noticeStyle.fontSize = Mathf.RoundToInt(22f * ui);
        }

        private void SubmitPath()
        {
            string dir = TerrariaFiles.Validate(pathInput);
            if (dir == null)
            {
                pathError = "Nie ma tam Terrarii (brak Content\\Wave Bank.xwb).  /  No Terraria there (Content\\Wave Bank.xwb missing).";
                return;
            }
            TerrariaFiles.ForceMissing = false;
            TerrariaArsenal.Instance.Settings.TerrariaPath = dir;
            TerrariaArsenal.Instance.SaveSettings();
            TerrariaContent.ReloadAll();
            pathError = TerrariaContent.Available ? "" : "Nie udało się wczytać Terrarii z tego folderu.  /  Could not load Terraria from this folder.";
            if (TerrariaContent.Available) pathOkTime = 6f;
        }

        private void DrawPathWindow(float ui)
        {
            Styles(ui);
            Box(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));
            float w = 1100f * ui, h = 560f * ui;
            Rect panel = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            Panel(panel);
            float pad = 30f * ui;
            float x = panel.x + pad, cw = w - pad * 2f, y = panel.y + pad;

            GUI.Label(new Rect(x, y, cw, 44f * ui), "Gdzie jest Terraria? / Where is Terraria?", titleStyle);
            y += 60f * ui;
            GUI.Label(new Rect(x, y, cw, 120f * ui),
                "Mod bierze grafiki, dźwięki i muzykę z Twojej Terrarii, ale nie znalazł jej automatycznie. " +
                "Wklej (Ctrl+V) folder Terrarii, np. ...\\steamapps\\common\\Terraria. W Steam: Terraria → Zarządzaj → Przeglądaj pliki lokalne, potem skopiuj adres z paska.\n" +
                "The mod loads graphics, sounds and music from your Terraria but could not find it. Paste the Terraria folder " +
                "(Steam: Terraria → Manage → Browse local files, copy the path).", textStyle);
            y += 150f * ui;

            Event e = Event.current;
            bool enter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);
            GUI.SetNextControlName("TA_TerrariaPath");
            pathInput = GUI.TextField(new Rect(x, y, cw, 44f * ui), pathInput, 400, fieldStyle);
            if (GUI.GetNameOfFocusedControl() != "TA_TerrariaPath" && e.type == EventType.Repaint) GUI.FocusControl("TA_TerrariaPath");
            y += 64f * ui;

            if (pathError.Length > 0)
            {
                Color old = GUI.color;
                GUI.color = new Color(1f, 0.5f, 0.4f);
                GUI.Label(new Rect(x, y, cw, 60f * ui), pathError, textStyle);
                GUI.color = old;
            }
            y += 70f * ui;

            float bw = (cw - 40f * ui) / 3f, bh = 56f * ui;
            if (GUI.Button(new Rect(x, y, bw, bh), "Zapisz / Save (Enter)", buttonStyle) || enter)
            {
                if (enter) e.Use();
                SubmitPath();
            }
            if (GUI.Button(new Rect(x + bw + 20f * ui, y, bw, bh), "Szukaj ponownie / Search again", buttonStyle))
            {
                TerrariaFiles.ForceMissing = false;
                TerrariaContent.ReloadAll();
                pathError = TerrariaContent.Available ? "" : "Dalej nie znaleziono.  /  Still not found.";
                if (TerrariaContent.Available) pathOkTime = 6f;
            }
            if (GUI.Button(new Rect(x + (bw + 20f * ui) * 2f, y, bw, bh), "Pomiń / Skip", buttonStyle))
            {
                pathSkipped = true;
            }
        }

        // Po pominieciu okna: krotka informacja na gorze menu.
        private void DrawMissingTerraria(float ui)
        {
            UIManager uim = UIManager.instance;
            if (uim == null || (uim.menuState != MainMenuState.MAIN_MENU && uim.menuState != MainMenuState.SAVE_PROFILES)) return;
            Styles(ui);
            string msg = "Mod: brak Terrarii – mod nie ma grafik ani dźwięków. Uruchom grę ponownie, żeby podać folder Terrarii.\n" +
                         "Mod: Terraria not found – no graphics or sounds. Restart the game to enter the Terraria folder.";
            GUI.Label(new Rect(Screen.width * 0.1f, Screen.height * 0.02f, Screen.width * 0.8f, Screen.height * 0.2f), msg, noticeStyle);
        }

        private void OnGUI()
        {
            float uiScale = Screen.height / 1080f;
            if (pathOkTime > 0f)
            {
                if (Event.current.type == EventType.Repaint) pathOkTime -= Time.unscaledDeltaTime;
                Styles(uiScale);
                GUI.Label(new Rect(Screen.width * 0.1f, Screen.height * 0.02f, Screen.width * 0.8f, 60f * uiScale),
                    "Terraria znaleziona – wszystko działa.  /  Terraria found – all set.", titleStyle);
            }
            if (!TerrariaContent.Available)
            {
                if (NeedPathWindow()) DrawPathWindow(uiScale);
                else DrawMissingTerraria(uiScale);
                return;
            }
            if (!OnSaveScreen()) return;
            float ui = Screen.height / 1080f;
            if (open)
            {
                DrawEditor(ui);
                return;
            }
            DrawSlotPreviews(ui);
        }

        private void DrawSlotPreviews(float ui)
        {
            SaveSlotButton sel = Selected();
            Vector3[] corners = new Vector3[4];
            foreach (SaveSlotButton b in buttons)
            {
                if (!Visible(b)) continue;
                RectTransform rt = b.transform as RectTransform;
                if (rt == null) continue;
                Canvas canvas = b.GetComponentInParent<Canvas>();
                Camera cam = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                rt.GetWorldCorners(corners);
                Vector2 bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                Vector2 tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                float h = Mathf.Abs(tr.y - bl.y);
                if (h < 4f) continue;
                float ph = Mathf.Clamp(h * 0.9f, 60f * ui, 170f * ui);
                float pw = ph * 40f / 56f;
                float x = bl.x - pw - 20f * ui;
                if (x < 4f) x = tr.x + 20f * ui;
                float y = Screen.height - (bl.y + tr.y) * 0.5f - ph * 0.5f;
                Rect pr = new Rect(x, y, pw, ph);

                Texture2D tex = Preview(SlotOf(b));
                if (tex != null) GUI.DrawTexture(pr, tex);

                bool isSel = sel == b;
                if (isSel)
                {
                    float ts = 0.75f * ui;
                    TerrariaFont.DrawCentered(pr.center.x, pr.yMax + 2f * ui, "[C] Wygląd", new Color(1f, 0.85f, 0.4f), ts);
                }
                // Klik mysza w postac tez otwiera edytor.
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && pr.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    Open(b);
                    return;
                }
            }
        }

        private void DrawEditor(float ui)
        {
            float w = 760f * ui, h = 720f * ui;
            Rect panel = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            Box(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.55f));
            Panel(panel);

            float pad = 24f * ui;
            TerrariaFont.DrawCentered(panel.center.x, panel.y + pad * 0.6f, "Wygląd postaci – zapis " + editSlot, Color.white, 1.1f * ui);

            // Podglad
            if (editDirty || editPreview == null)
            {
                if (editPreview != null) Destroy(editPreview);
                editPreview = PlayerLook.BuildPreview(edit);
                editDirty = false;
            }
            float ph = 360f * ui, pw = ph * 40f / 56f;
            Rect prev = new Rect(panel.x + pad * 1.5f, panel.y + 110f * ui, pw, ph);
            Box(new Rect(prev.x - 8f * ui, prev.y - 8f * ui, prev.width + 16f * ui, prev.height + 16f * ui), new Color(0.06f, 0.08f, 0.2f, 0.9f));
            if (editPreview != null) GUI.DrawTexture(prev, editPreview);

            // Wiersze
            float lx = prev.xMax + pad * 1.5f;
            float ly = panel.y + 80f * ui;
            float lh = 40f * ui;
            float ts = 0.85f * ui;
            Event e = Event.current;
            for (int i = 0; i < Rows.Length; i++)
            {
                Rect rr = new Rect(lx - 10f * ui, ly + i * lh, panel.xMax - lx - pad + 10f * ui, lh - 4f * ui);
                bool selected = i == row;
                if (selected) Box(rr, new Color(0.45f, 0.55f, 1f, 0.25f));
                Color tc = selected ? new Color(1f, 0.9f, 0.4f) : Color.white;
                TerrariaFont.Draw(lx, rr.y + 4f * ui, Rows[i], tc, ts);

                string value = null;
                if (i == RowVariant)
                {
                    int vi = System.Array.IndexOf(PlayerLook.VariantOrder, edit.Variant);
                    value = "< " + PlayerLook.VariantNames[Mathf.Max(0, vi)] + " >";
                }
                else if (i == RowHair) value = "< " + edit.Hair + " >";
                if (value != null)
                {
                    float vw = TerrariaFont.Measure(value, ts);
                    TerrariaFont.Draw(rr.xMax - vw - 10f * ui, rr.y + 4f * ui, value, tc, ts);
                }
                if (i >= RowFirstColor && i <= RowLastColor)
                {
                    float sw = 60f * ui;
                    Rect sr = new Rect(rr.xMax - sw - 10f * ui, rr.y + 6f * ui, sw, rr.height - 12f * ui);
                    Box(new Rect(sr.x - 2f, sr.y - 2f, sr.width + 4f, sr.height + 4f), Color.black);
                    Box(sr, GetColor(i));
                }

                if (e.type == EventType.MouseDown && e.button == 0 && rr.Contains(e.mousePosition))
                {
                    row = i;
                    if (i == RowRandom) Randomize();
                    else if (i == RowDone) Close(true);
                    else if (i == RowVariant || i == RowHair)
                    {
                        Change(e.mousePosition.x > rr.center.x ? 1 : -1);
                    }
                    e.Use();
                }
            }

            // Suwaki koloru (mysz) dla wybranego koloru
            float sy = ly + Rows.Length * lh + 6f * ui;
            if (row >= RowFirstColor && row <= RowLastColor)
            {
                Color c = GetColor(row);
                float hh, s, v;
                Color.RGBToHSV(c, out hh, out s, out v);
                float sx = prev.x;
                float sliderW = panel.xMax - pad - sx - 150f * ui;
                string[] names = { "Odcień", "Nasycenie", "Jasność" };
                float[] vals = { hh, s, v };
                bool changed = false;
                for (int k = 0; k < 3; k++)
                {
                    float yy = panel.yMax - pad - (3 - k) * 34f * ui;
                    TerrariaFont.Draw(sx, yy, names[k], Color.white, 0.7f * ui);
                    Rect bar = new Rect(sx + 150f * ui, yy + 8f * ui, sliderW, 14f * ui);
                    Box(bar, new Color(0f, 0f, 0f, 0.6f));
                    Box(new Rect(bar.x + bar.width * vals[k] - 4f * ui, bar.y - 5f * ui, 8f * ui, bar.height + 10f * ui), new Color(1f, 0.85f, 0.4f));
                    Rect hit = new Rect(bar.x, bar.y - 8f * ui, bar.width, bar.height + 16f * ui);
                    if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && hit.Contains(e.mousePosition))
                    {
                        vals[k] = Mathf.Clamp01((e.mousePosition.x - bar.x) / bar.width);
                        changed = true;
                        e.Use();
                    }
                }
                if (changed)
                {
                    SetColor(row, Color.HSVToRGB(vals[0], vals[1], vals[2]));
                    editDirty = true;
                }
            }
            else
            {
                string help = "Strzałki/WASD: wybór i zmiana · Enter: Losuj/Gotowe · Esc: zapisz i wyjdź";
                TerrariaFont.DrawCentered(panel.center.x, panel.yMax - pad - 30f * ui, help, new Color(0.8f, 0.8f, 0.8f), 0.65f * ui);
            }
            if (row >= RowFirstColor && row <= RowLastColor)
            {
                TerrariaFont.DrawCentered(panel.center.x, sy - 2f * ui, "Lewo/Prawo: odcień · Shift: jasność · Ctrl: nasycenie", new Color(0.8f, 0.8f, 0.8f), 0.6f * ui);
            }
        }
    }
}
