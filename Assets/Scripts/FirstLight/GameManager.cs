using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstLight
{
    /// <summary>
    /// Entry point. Put this on a single empty GameObject in a scene and press play -
    /// it creates the camera, the levels, the player and the HUD by itself.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        enum Phase { Title, Playing, Clearing, Ending }

        [Tooltip("Level to start on. Useful while building levels.")]
        public int startLevel = 0;

        List<LevelDef> levels;
        LevelView view;
        Camera cam;
        Sfx sfx;

        Phase phase = Phase.Title;
        int index;

        Transform player;
        SpriteRenderer playerGlow;
        Vector2Int playerCell;
        Vector2 playerVisual;

        SpriteRenderer overlay;
        float overlayAmount;
        Color overlayColor = Color.black;

        Vector2Int heldDir;
        float repeatTimer;
        float clearTimer;

        // remembered so we can fire a cue the moment something changes
        int lastCleared;
        bool lastA, lastB, lastRA, lastRB, lastSwitched;

        void Awake()
        {
            Application.targetFrameRate = 60;

            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;
            cam.transform.position = new Vector3(0, 0, -10);

            levels = LevelLibrary.Build();
            sfx = Sfx.Create(transform);
            BuildPlayer();
            BuildOverlay();
            index = Mathf.Clamp(startLevel, 0, levels.Count - 1);
        }

        void BuildPlayer()
        {
            var go = new GameObject("Entity");
            go.transform.SetParent(transform, false);
            player = go.transform;
            var body = go.AddComponent<SpriteRenderer>();
            body.sprite = Art.Star();
            body.color = Palette.Player;
            body.sortingOrder = 27;

            var glow = new GameObject("Aura");
            glow.transform.SetParent(player, false);
            glow.transform.localScale = Vector3.one * 2.4f;
            playerGlow = glow.AddComponent<SpriteRenderer>();
            playerGlow.sprite = Art.Glow();
            playerGlow.color = Palette.PlayerGlow;
            playerGlow.sortingOrder = 26;
            player.gameObject.SetActive(false);
        }

        void BuildOverlay()
        {
            var go = new GameObject("Overlay");
            go.transform.SetParent(transform, false);
            overlay = go.AddComponent<SpriteRenderer>();
            overlay.sprite = Art.Square();
            overlay.sortingOrder = 100;
            overlay.transform.localScale = Vector3.one * 200f;
            overlay.color = Color.clear;
        }

        // ------------------------------------------------------------------ flow

        /// <summary>Public entry point: begin play at a given level.</summary>
        public void BeginAt(int levelIndex) => StartLevel(levelIndex);

        void StartLevel(int i)
        {
            if (view != null) Destroy(view.gameObject);
            index = Mathf.Clamp(i, 0, levels.Count - 1);
            var def = levels[index];

            // the world grows a little less dark as the light climbs back toward the sky
            float ambient = 0.035f + 0.014f * index;
            view = LevelView.Create(def, ambient);
            view.transform.SetParent(transform, false);

            playerCell = def.Start;
            playerVisual = playerCell;
            player.position = playerVisual;
            player.gameObject.SetActive(true);

            FitCamera(def);
            phase = Phase.Playing;
            overlayAmount = 1f;
            overlayColor = Color.black;
            SnapshotSignals();
        }

        void FitCamera(LevelDef def)
        {
            float aspect = Mathf.Max(cam.aspect, 0.4f);
            float halfH = def.Height * 0.5f + 1.2f;
            float halfW = (def.Width * 0.5f + 1.2f) / aspect;
            cam.orthographicSize = Mathf.Max(halfH, halfW);
            cam.transform.position = new Vector3((def.Width - 1) * 0.5f, (def.Height - 1) * 0.5f, -10);
        }

        void SnapshotSignals()
        {
            var s = view.State;
            lastCleared = s.ClearedEyes.Count;
            lastA = s.CircuitA; lastB = s.CircuitB;
            lastRA = s.ReceiverA; lastRB = s.ReceiverB;
            lastSwitched = s.Switched;
        }

        void ReportSignals()
        {
            var s = view.State;
            if (s.ClearedEyes.Count > lastCleared) sfx.Strike();
            else if ((s.CircuitA && !lastA) || (s.CircuitB && !lastB) ||
                     (s.ReceiverA && !lastRA) || (s.ReceiverB && !lastRB) ||
                     (s.Switched && !lastSwitched))
                sfx.Activate();
            SnapshotSignals();
        }

        // ----------------------------------------------------------------- update

        void Update()
        {
            float dt = Time.deltaTime;
            overlayAmount = Mathf.MoveTowards(overlayAmount, 0f, dt * 1.6f);
            overlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, overlayAmount);

            var kb = Keyboard.current;

            if (phase == Phase.Title)
            {
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                    StartLevel(index);
                return;
            }

            if (phase == Phase.Ending)
            {
                if (kb != null && kb.rKey.wasPressedThisFrame)
                {
                    index = 0;
                    phase = Phase.Title;
                    if (view != null) Destroy(view.gameObject);
                    player.gameObject.SetActive(false);
                }
                return;
            }

            if (phase == Phase.Playing && kb != null)
            {
                HandleMovement(kb, dt);
                HandleActions(kb);
            }

            if (phase == Phase.Clearing)
            {
                clearTimer -= dt;
                if (clearTimer <= 0f) Advance();
            }

            // Escape, the ending, and level changes all tear the view down mid-frame.
            if (view == null || (phase != Phase.Playing && phase != Phase.Clearing)) return;

            // visuals
            playerVisual = Vector2.Lerp(playerVisual, playerCell, 1f - Mathf.Exp(-22f * dt));
            player.position = playerVisual;
            float bob = 1f + 0.06f * Mathf.Sin(Time.time * 2.6f);
            player.localScale = Vector3.one * bob;
            playerGlow.color = new Color(Palette.PlayerGlow.r, Palette.PlayerGlow.g,
                                         Palette.PlayerGlow.b,
                                         Palette.PlayerGlow.a * (0.8f + 0.2f * bob));

            // a bridge can collapse, or a nightbloom close, while you are standing on it
            if (phase == Phase.Playing && !view.Level.Walkable(playerCell, view.State))
                Die();

            view.Render(playerVisual, Time.time);

            if (phase == Phase.Playing && view.Level.IsWin(playerCell, view.State))
            {
                phase = Phase.Clearing;
                clearTimer = 0.9f;
                overlayColor = Color.white;
                overlayAmount = 0.85f;
                if (index == levels.Count - 1) sfx.Finale(); else sfx.Complete();
            }
        }

        void Advance()
        {
            if (index >= levels.Count - 1)
            {
                phase = Phase.Ending;
                player.gameObject.SetActive(false);
                if (view != null) Destroy(view.gameObject);
                return;
            }
            StartLevel(index + 1);
        }

        void HandleMovement(Keyboard kb, float dt)
        {
            var dir = Vector2Int.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir = Vector2Int.up;
            else if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir = Vector2Int.down;
            else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir = Vector2Int.left;
            else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir = Vector2Int.right;

            if (dir == Vector2Int.zero)
            {
                heldDir = Vector2Int.zero;
                repeatTimer = 0f;
                return;
            }

            if (dir != heldDir)
            {
                heldDir = dir;
                repeatTimer = 0.20f;      // initial delay before auto-repeat
                TryMove(dir);
                return;
            }

            repeatTimer -= dt;
            if (repeatTimer <= 0f)
            {
                repeatTimer = 0.11f;
                TryMove(dir);
            }
        }

        void TryMove(Vector2Int dir)
        {
            var target = playerCell + dir;
            var level = view.Level;

            // Stepping into living darkness costs you the walk back, nothing more.
            if (level.InLiveCloud(target, view.State))
            {
                Die();
                return;
            }

            if (!level.Walkable(target, view.State))
            {
                return;
            }

            playerCell = target;
        }

        void Die()
        {
            playerCell = view.Level.Start;
            playerVisual = playerCell;
            overlayColor = new Color(0.35f, 0.02f, 0.02f);
            overlayAmount = 0.8f;
            sfx.Deny();
        }

        void HandleActions(Keyboard kb)
        {
            bool ccw = kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
            bool cw = kb.qKey.wasPressedThisFrame;

            if (ccw || cw)
            {
                int m = view.MirrorNextTo(playerCell);
                if (m >= 0)
                {
                    view.RotateMirror(m, ccw ? 1 : -1);
                    sfx.Turn();
                    ReportSignals();
                }
                else sfx.Deny();
            }

            if (kb.rKey.wasPressedThisFrame) StartLevel(index);
            if (kb.escapeKey.wasPressedThisFrame)
            {
                phase = Phase.Title;
                if (view != null) Destroy(view.gameObject);
                player.gameObject.SetActive(false);
            }
            if (kb.leftBracketKey.wasPressedThisFrame && index > 0) StartLevel(index - 1);
            if (kb.rightBracketKey.wasPressedThisFrame && index < levels.Count - 1) StartLevel(index + 1);
        }

        // -------------------------------------------------------------------- HUD

        GUIStyle Style(int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter,
                       FontStyle fontStyle = FontStyle.Normal)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                alignment = anchor,
                fontStyle = fontStyle,
                normal = { textColor = color },
                wordWrap = true,
            };
        }

        void OnGUI()
        {
            float w = Screen.width, h = Screen.height;
            float s = Mathf.Clamp(h / 720f, 0.7f, 2.2f);
            var warm = new Color(1f, 0.86f, 0.58f);
            var faint = new Color(0.72f, 0.76f, 0.86f, 0.75f);

            if (phase == Phase.Title)
            {
                GUI.Label(new Rect(0, h * 0.26f, w, h * 0.12f), "FIRST LIGHT",
                          Style(Mathf.RoundToInt(64 * s), warm, TextAnchor.MiddleCenter, FontStyle.Bold));
                GUI.Label(new Rect(w * 0.2f, h * 0.40f, w * 0.6f, h * 0.16f),
                          "The sun has gone dark. You are the last spark of light, cast down " +
                          "from the sky.\nYou cannot carry the light - only bend it.",
                          Style(Mathf.RoundToInt(18 * s), faint));
                GUI.Label(new Rect(w * 0.2f, h * 0.58f, w * 0.6f, h * 0.16f),
                          "WASD / arrows  -  move\n" +
                          "E or Space  -  turn the nearest mirror\nQ  -  turn it back\n" +
                          "R  -  restart level     [ ]  -  change level",
                          Style(Mathf.RoundToInt(16 * s), faint));
                GUI.Label(new Rect(0, h * 0.80f, w, h * 0.08f), "press SPACE to begin",
                          Style(Mathf.RoundToInt(20 * s), warm));
                return;
            }

            if (phase == Phase.Ending)
            {
                GUI.Label(new Rect(0, h * 0.32f, w, h * 0.12f), "THE SUN BRIGHTENS",
                          Style(Mathf.RoundToInt(52 * s), warm, TextAnchor.MiddleCenter, FontStyle.Bold));
                GUI.Label(new Rect(w * 0.2f, h * 0.46f, w * 0.6f, h * 0.2f),
                          "The light you bent all the way up has found the sky again.\n" +
                          "Somewhere below, a world sees its first morning.",
                          Style(Mathf.RoundToInt(18 * s), faint));
                GUI.Label(new Rect(0, h * 0.72f, w, h * 0.08f), "press R to begin again",
                          Style(Mathf.RoundToInt(18 * s), faint));
                return;
            }

            var def = view.Level;
            GUI.Label(new Rect(w * 0.04f, h * 0.03f, w * 0.6f, 30 * s),
                      $"{index + 1}/{levels.Count}   {def.Name}",
                      Style(Mathf.RoundToInt(20 * s), warm, TextAnchor.UpperLeft, FontStyle.Bold));
            GUI.Label(new Rect(w * 0.04f, h * 0.03f + 26 * s, w * 0.6f, 30 * s), def.Idea,
                      Style(Mathf.RoundToInt(14 * s), faint, TextAnchor.UpperLeft));

            string hint = view.MirrorNextTo(playerCell) >= 0
                ? "E / Q  turn the mirror"
                : "walk up to a mirror to turn it";
            GUI.Label(new Rect(w * 0.04f, h - 34 * s, w * 0.92f, 30 * s), hint,
                      Style(Mathf.RoundToInt(14 * s), faint, TextAnchor.LowerLeft));

            var st = view.State;
            string status = st.ClearedEyes.Count < def.Eyes.Count
                ? $"eyes remaining: {def.Eyes.Count - st.ClearedEyes.Count}"
                : (def.Eyes.Count > 0 ? "the dark is lifted" : "");
            GUI.Label(new Rect(w * 0.04f, h - 34 * s, w * 0.92f, 30 * s), status,
                      Style(Mathf.RoundToInt(14 * s), new Color(1f, 0.45f, 0.45f, 0.9f),
                            TextAnchor.LowerRight));
        }
    }
}
