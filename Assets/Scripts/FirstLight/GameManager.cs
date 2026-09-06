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

        [Tooltip("On-screen controls. Auto means phones and tablets only - desktop is untouched.")]
        public TouchControlsMode touchControls = TouchControlsMode.Auto;

        [Tooltip("Hand-drawn art. Anything left empty falls back to the procedural sprite.")]
        public Skin skin = new();

        [Tooltip("One track per stretch of the game, spread evenly over the levels.")]
        public AudioClip[] stageMusic;

        List<LevelDef> levels;
        LevelView view;
        Camera cam;
        Sfx sfx;
        Music music;
        readonly TouchControls touch = new();

        Phase phase = Phase.Title;
        int index;

        Transform player;
        SpriteRenderer playerGlow, playerBody;
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

            touch.Mode = touchControls;
            if (touch.Active)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;

                // Levels are wide, so portrait would shrink them to nothing. Lock to
                // landscape but leave both ways up, so the device can still be flipped.
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.orientation = ScreenOrientation.AutoRotation;
            }

            levels = LevelLibrary.Build();
            sfx = Sfx.Create(transform);
            music = Music.Create(transform, stageMusic);
            BuildPlayer();
            BuildOverlay();
            index = Mathf.Clamp(startLevel, 0, levels.Count - 1);
        }

        void BuildPlayer()
        {
            var go = new GameObject("Entity");
            go.transform.SetParent(transform, false);
            player = go.transform;

            // the body is a child of its own: fitting a hand-drawn sprite to the cell
            // must not drag the aura's size along with it
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(player, false);
            playerBody = bodyGo.AddComponent<SpriteRenderer>();
            playerBody.sprite = skin.HasEntity ? skin.EntityFrame(0f) : Art.Star();
            playerBody.color = skin.HasEntity ? Color.white : Palette.Player;
            playerBody.sortingOrder = 27;

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
            view = LevelView.Create(def, ambient, skin);
            view.transform.SetParent(transform, false);
            music.PlayFor(index, levels.Count);

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

            // The pad covers the bottom of the screen, so the level has to fit in what
            // is left above it, and the camera drops by half that band to re-centre.
            float reserved = Mathf.Clamp(touch.ReservedBottom, 0f, 0.6f);
            float usable = 1f - reserved;

            float halfH = (def.Height * 0.5f + 1.2f) / usable;
            float halfW = (def.Width * 0.5f + 1.2f) / aspect;
            cam.orthographicSize = Mathf.Max(halfH, halfW);

            float centreY = (def.Height - 1) * 0.5f - reserved * cam.orthographicSize;
            cam.transform.position = new Vector3((def.Width - 1) * 0.5f, centreY, -10);
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
            var pad = touch.Active ? touch.Sample() : default;

            if (phase == Phase.Title)
            {
                if ((kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                    || pad.AnyTap)
                    StartLevel(index);
                return;
            }

            if (phase == Phase.Ending)
            {
                if ((kb != null && kb.rKey.wasPressedThisFrame) || pad.AnyTap)
                {
                    index = 0;
                    phase = Phase.Title;
                    if (view != null) Destroy(view.gameObject);
                    player.gameObject.SetActive(false);
                }
                return;
            }

            if (phase == Phase.Playing)
            {
                HandleMovement(kb, pad, dt);
                HandleActions(kb, pad);
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
            if (skin.HasEntity)
            {
                playerBody.sprite = skin.EntityFrame(Time.time);
                playerBody.transform.localScale =
                    Vector3.one * (Skin.ScaleToCells(playerBody.sprite, 1.25f) * bob);
            }
            else
            {
                playerBody.transform.localScale = Vector3.one * bob;
            }
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

        void HandleMovement(Keyboard kb, TouchControls.State pad, float dt)
        {
            var dir = Vector2Int.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir = Vector2Int.up;
                else if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir = Vector2Int.down;
                else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir = Vector2Int.left;
                else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir = Vector2Int.right;
            }
            // a held direction on the pad repeats exactly like a held key
            if (dir == Vector2Int.zero) dir = pad.Move;

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

        void HandleActions(Keyboard kb, TouchControls.State pad)
        {
            bool ccw = pad.TurnCcw, cw = pad.TurnCw, restart = pad.Restart, toTitle = false;
            if (kb != null)
            {
                ccw |= kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                cw |= kb.qKey.wasPressedThisFrame;
                restart |= kb.rKey.wasPressedThisFrame;
                toTitle = kb.escapeKey.wasPressedThisFrame;
            }

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

            if (restart) StartLevel(index);
            if (toTitle)
            {
                phase = Phase.Title;
                if (view != null) Destroy(view.gameObject);
                player.gameObject.SetActive(false);
            }
            if (kb == null) return;
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
                          touch.Active
                              ? "the cross  -  move\n" +
                                "the two round buttons  -  turn the nearest mirror\n" +
                                "the ring, top right  -  restart the level"
                              : "WASD / arrows  -  move\n" +
                                "E or Space  -  turn the nearest mirror\nQ  -  turn it back\n" +
                                "R  -  restart level     [ ]  -  change level",
                          Style(Mathf.RoundToInt(16 * s), faint));
                GUI.Label(new Rect(0, h * 0.80f, w, h * 0.08f),
                          touch.Active ? "tap to begin" : "press SPACE to begin",
                          Style(Mathf.RoundToInt(20 * s), warm));
                touch.Draw();
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
                GUI.Label(new Rect(0, h * 0.72f, w, h * 0.08f),
                          touch.Active ? "tap to begin again" : "press R to begin again",
                          Style(Mathf.RoundToInt(18 * s), faint));
                return;
            }

            var def = view.Level;
            GUI.Label(new Rect(w * 0.04f, h * 0.03f, w * 0.6f, 30 * s),
                      $"{index + 1}/{levels.Count}   {def.Name}",
                      Style(Mathf.RoundToInt(20 * s), warm, TextAnchor.UpperLeft, FontStyle.Bold));
            bool byMirror = view.MirrorNextTo(playerCell) >= 0;
            string hint = touch.Active
                ? (byMirror ? "the round buttons turn this mirror"
                            : "walk up to a mirror to turn it")
                : (byMirror ? "E / Q  turn the mirror"
                            : "walk up to a mirror to turn it");
            // keep the two hint lines clear of the pad
            float hintY = h * (1f - touch.ReservedBottom) - 34 * s;
            GUI.Label(new Rect(w * 0.04f, hintY, w * 0.92f, 30 * s), hint,
                      Style(Mathf.RoundToInt(14 * s), faint, TextAnchor.LowerLeft));

            var st = view.State;
            string status = st.ClearedEyes.Count < def.Eyes.Count
                ? $"eyes remaining: {def.Eyes.Count - st.ClearedEyes.Count}"
                : (def.Eyes.Count > 0 ? "the dark is lifted" : "");
            GUI.Label(new Rect(w * 0.04f, hintY, w * 0.92f, 30 * s), status,
                      Style(Mathf.RoundToInt(14 * s), new Color(1f, 0.45f, 0.45f, 0.9f),
                            TextAnchor.LowerRight));

            touch.Draw();
        }
    }
}
