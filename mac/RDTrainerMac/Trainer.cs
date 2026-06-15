using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace RDTrainer
{
    // In-game trainer for Rhythm Doctor (单机), macOS-native. F3 opens an overlay with three
    // tabs: 普通 (gameplay toggles), 存档修改 (irreversible save edits), 关卡直达 (level jump),
    // plus a 旧版菜单 button that swaps in the legacy 4-tab UI until the menu is reopened.
    // This is the BepInEx BaseUnityPlugin ported to a plain MonoBehaviour hosted by Loader.
    public class Trainer : MonoBehaviour
    {
        public const string Guid = "com.cohen.rdtrainer";
        public const string Name = "RD Trainer (节奏医生修改器)";
        public const string Version = "2.50";

        // 防倒卖水印：此串同时用于「显示」与「启动完整性校验」。改动或删除它会导致 SHA256 校验失败、
        // 整个修改器拒绝工作（不挂补丁、不应用任何功能）。谁删水印谁失效。
        public const string Watermark = "本工具免费开源，严禁倒卖 · FREE · github.com/Cohenjikan/RhythmDoctorTrainer";
        private const string ExpectedSig = "aa5b99cb20d0aee2d25454b831b309f2ac6432c6a41ed393ec43de203b4043c1";
        internal static bool IntegrityOK;

        private static readonly KeyCode MenuKey = KeyCode.F3;

        private bool _menuOpen;
        private int _tab;          // new UI: 0 普通, 1 存档修改, 2 关卡直达
        private bool _legacy;      // 旧版菜单 mode; resets to the new UI when the menu is reopened
        private int _legacyTab;    // legacy UI: 0 普通, 1 开发者, 2 高级, 3 关卡直达
        private Rect _win = new Rect(24, 24, 480, 620);
        private Rect _hintWin = new Rect(20, 20, 300, 70);
        private Rect _keysWin = new Rect(20, 120, 280, 84);
        private Vector2 _scroll;
        private string _lvFilter = "";
        private Font _cjk;
        private bool _lastSpeedOverride;
        private bool _lastMasterEnabled = true;   // tracks the master-switch ON→OFF edge

        // PlayerPrefs key remembering whether the top-left hint window is shown (1) or hidden (0).
        private const string HintPrefKey = "RDTrainerMac.showHintWindow";

        // local UI state
        private bool _wipeConfirm;
        private bool _showMisc;
        private bool _calLoaded;
        private int _bestRound = 1;        // legacy 高级 tab
        private bool _saveLoaded;
        private string _wlRoundText = "0"; // 举重 best infinite round (keyboard input)
        private string _beansText = "0";   // 武士豆子跳 (Beans Hopper 2-B1) high score
        private string _stackerText = "0"; // Ian 桌面·叠方块 high score
        private string _tempresText = "0"; // Ian 桌面·Tempres high score (float)
        private bool _paigeStays;          // 佩奇结局分支 (Persistence.GetPaigeEnding)
        private string _completion;        // 完成度 summary line
        private string _slotDelText = "1";

        // key overlay
        private struct KeyHit { public string name; public float t; public KeyHit(string n, float tt) { name = n; t = tt; } }
        private readonly List<KeyHit> _recent = new List<KeyHit>();
        private static readonly KeyCode[] OverlayKeys =
        {
            KeyCode.Space, KeyCode.Return, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L,
            KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P,
            KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M,
            KeyCode.LeftShift, KeyCode.RightShift
        };

        private float _lastAutoRestart;

        private void Awake()
        {
            // Anti-resale integrity gate: the watermark must be intact, or the trainer refuses to
            // function (no Harmony patches, no feature application, no menu). 删/改水印即失效。
            IntegrityOK = Sig(Watermark) == ExpectedSig;
            if (!IntegrityOK)
            {
                Log.Error("完整性校验失败：水印被篡改，修改器已禁用。" +
                          "请从 github.com/Cohenjikan/RhythmDoctorTrainer 获取免费正版。");
                return; // do NOT patch or enable anything
            }

            // Restore the remembered "show hint window" choice. The master switch is intentionally
            // NOT persisted — it always starts ON so the trainer is live on every launch.
            LoadPrefs();

            // Patch each [HarmonyPatch] class independently so a single failure (e.g. an
            // unpatchable method on a given runtime) can't take down the rest of the trainer.
            var harmony = new Harmony(Guid);
            int ok = 0, fail = 0;
            foreach (var t in typeof(Trainer).Assembly.GetTypes())
            {
                if (t.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0) continue;
                try { harmony.CreateClassProcessor(t).Patch(); ok++; }
                catch (Exception e) { fail++; Log.Error($"patch {t.Name} failed: {e.Message}"); }
            }
            Log.Info($"{Name} v{Version} loaded · {Watermark} · Menu key = {MenuKey} · patches ok={ok} fail={fail}");
        }

        private static string Sig(string s)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
                var sb = new StringBuilder(h.Length * 2);
                foreach (byte b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private void Update()
        {
            if (!IntegrityOK) return;
            try
            {
                if (Input.GetKeyDown(MenuKey))
                {
                    _menuOpen = !_menuOpen;
                    if (_menuOpen) _legacy = false; // reopening always returns to the new UI
                }
                if (_menuOpen) { Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }

                // Master switch off → only the F3 menu (above) stays live; every feature, hotkey
                // and overlay is skipped. ApplyState still runs to perform the one-time restore.
                if (Cheats.masterEnabled)
                {
                    if (Input.GetKeyDown(KeyCode.F4))
                        Run("WinLevel(F4)", () => { var g = scnGame.instance; if (g != null) g.WinLevel(); });
                    if (Input.GetKeyDown(KeyCode.F5))
                        Run("Restart(F5)", () => { var g = scnGame.instance; if (g != null) g.Restart(false); });

                    if (Cheats.autoRestartOnMiss) AutoRestartTick();
                    if (Cheats.keyOverlay) CaptureKeys();
                }

                ApplyState();
            }
            catch (Exception e) { Log.Error("Update: " + e); }
        }

        private void AutoRestartTick()
        {
            try
            {
                var g = scnGame.instance;
                if (g == null || g.currentLevel == null) return;
                if (g.currentLevel.numMistakes <= 0f) return;
                if (Time.unscaledTime - _lastAutoRestart < 1.5f) return;
                _lastAutoRestart = Time.unscaledTime;
                g.Restart(false);
                Log.Info("auto-restart: mistake detected");
            }
            catch { }
        }

        private void CaptureKeys()
        {
            float now = Time.unscaledTime;
            foreach (var k in OverlayKeys)
                if (Input.GetKeyDown(k)) _recent.Add(new KeyHit(KeyName(k), now));
            _recent.RemoveAll(h => now - h.t > 4f);
            while (_recent.Count > 10) _recent.RemoveAt(0);
        }

        private static string KeyName(KeyCode k)
        {
            switch (k)
            {
                case KeyCode.Space: return "空格";
                case KeyCode.Return: return "回车";
                case KeyCode.UpArrow: return "↑";
                case KeyCode.DownArrow: return "↓";
                case KeyCode.LeftArrow: return "←";
                case KeyCode.RightArrow: return "→";
                case KeyCode.LeftShift: return "L⇧";
                case KeyCode.RightShift: return "R⇧";
                default: return k.ToString();
            }
        }

        private void ApplyState()
        {
            // Master switch: when OFF the trainer goes fully inert so the game / level editor
            // regains control of its own switches (e.g. otherwise we'd keep forcing
            // DebugSettings.Auto back and the editor's own autoplay could never stay on). We
            // restore everything we continuously enforce ONCE on the ON→OFF edge, then write
            // nothing until re-enabled.
            if (!Cheats.masterEnabled)
            {
                if (_lastMasterEnabled) RestoreGameDefaults();
                _lastMasterEnabled = false;
                return;
            }
            _lastMasterEnabled = true;

            var ds = DebugSettings.instance;
            bool inGame = false;
            try { inGame = scnGame.instance != null; } catch { }

            if (inGame)
            {
                if (ds.Auto != Cheats.autoplay) ds.Auto = Cheats.autoplay;
            }

            // Speed: the engine applies it at level Start via `RDTime.speed = scnGame.levelSpeed`,
            // which then scales bpm AND the song's pitch consistently. So we arm the static
            // `scnGame.levelSpeed`; it takes effect on the next level start / restart.
            // (立即变速 button additionally pushes RDTime.speed + conductor pitch mid-level.)
            try
            {
                if (Cheats.speedOverride)
                {
                    if (scnGame.levelSpeed != Cheats.speed) scnGame.levelSpeed = Cheats.speed;
                }
                else if (_lastSpeedOverride)
                {
                    scnGame.levelSpeed = 1f;
                    ApplyLiveSpeed(1f); // also undo any mid-level override
                }
            }
            catch { }
            _lastSpeedOverride = Cheats.speedOverride;

            if (ds.InstantDialogue != Cheats.instantDialogue) ds.InstantDialogue = Cheats.instantDialogue;
            if (ds.SkipMenuTransitions != Cheats.skipTransitions) ds.SkipMenuTransitions = Cheats.skipTransitions;
            if (ds.UnlimitedFramerate != Cheats.unlimitedFps) ds.UnlimitedFramerate = Cheats.unlimitedFps;
            if (ds.Debug != Cheats.debugMode) ds.Debug = Cheats.debugMode;

            bool wantBeat = !Cheats.muteBeatSounds;
            if (ds.BeatSounds != wantBeat) ds.BeatSounds = wantBeat;
            bool wantAch = !Cheats.noAchievements;
            if (ds.GiveAchievements != wantAch) ds.GiveAchievements = wantAch;

            try { if (RDString.samuraiMode != Cheats.samuraiMode) RDString.samuraiMode = Cheats.samuraiMode; } catch { }
        }

        // Hand every continuously-enforced switch back to its default. Called once when the master
        // switch flips OFF, so the game / editor isn't left with a flag we forced on and we then
        // stop fighting its own controls. (Booth mode is a manual one-shot toggle, not enforced
        // per-frame, so it's intentionally left untouched.)
        private void RestoreGameDefaults()
        {
            try
            {
                var ds = DebugSettings.instance;
                if (ds != null)
                {
                    ds.Auto = false;
                    ds.InstantDialogue = false;
                    ds.SkipMenuTransitions = false;
                    ds.UnlimitedFramerate = false;
                    ds.Debug = false;
                    ds.BeatSounds = true;        // default = beat sounds on
                    ds.GiveAchievements = true;  // default = achievements on
                }
            }
            catch { }
            try { scnGame.levelSpeed = 1f; } catch { }
            try { ApplyLiveSpeed(1f); } catch { }
            try { if (RDString.samuraiMode) RDString.samuraiMode = false; } catch { }
            _lastSpeedOverride = false;
            Log.Info("主开关已关闭：已恢复游戏自身开关，修改器进入停用状态（再次打开总开关即恢复）。");
        }

        // Mid-level speed: RDTime.speed drives the conductor's timing math each frame; the song's
        // audible pitch is whatever AudioSource.pitch was set at Play, so push both together.
        private static void ApplyLiveSpeed(float v)
        {
            try { RDTime.speed = v; } catch { }
            try
            {
                var fi = typeof(scrConductor).GetField("_instance",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var cond = fi != null ? fi.GetValue(null) as scrConductor : null;
                if (cond != null) cond.pitch = v;
            }
            catch (Exception e) { Log.Error("ApplyLiveSpeed: " + e.Message); }
        }

        private void EnsureFont()
        {
            if (_cjk != null) return;
            // CreateDynamicFontFromOSFont silently yields an empty (glyph-less) font if the FIRST
            // name doesn't exist on this OS, so pick only names actually present (macOS reports
            // "Heiti SC" etc., NOT "PingFang SC"). Order = preferred Simplified-Chinese families.
            string[] inst;
            try { inst = Font.GetOSInstalledFontNames() ?? new string[0]; } catch { inst = new string[0]; }
            var prefer = new[] { "PingFang SC", "Heiti SC", "Hiragino Sans GB", "Songti SC", "STSong", "STHeiti", "Arial Unicode MS" };
            var pick = prefer.Where(p => Array.IndexOf(inst, p) >= 0).ToArray();
            try
            {
                _cjk = pick.Length > 0
                    ? Font.CreateDynamicFontFromOSFont(pick, 16)
                    : Font.CreateDynamicFontFromOSFont("Arial Unicode MS", 16);
            }
            catch (Exception e) { Log.Error("font create failed: " + e.Message); }
        }

        private void OnGUI()
        {
            if (!IntegrityOK) return;
            EnsureFont();
            var prev = GUI.skin.font;
            if (_cjk != null) GUI.skin.font = _cjk;

            // Top-left "game modified" hint — drawn as a Window (same mechanism as the menu, which
            // renders text correctly). Fixed position; shown only on non-level scenes while the menu
            // is CLOSED (complementary to the menu). Hidden inside a level. The user-toggleable
            // showHintWindow (persisted) lets people who find it intrusive hide it entirely.
            if (Cheats.showHintWindow && InNonLevelScene() && !_menuOpen)
                GUILayout.Window(740182, _hintWin, DrawHintWindow, "节奏医生修改器");

            if (Cheats.keyOverlay)
                _keysWin = GUILayout.Window(740183, _keysWin, DrawKeysWindow, "按键显示");

            if (_menuOpen)
                _win = GUILayout.Window(740181, _win, DrawWindow, $"节奏医生修改器 v{Version}");
            GUI.skin.font = prev;
        }

        // True on any non-level scene (menu/hub/level-select/editor). In a level scnGame.instance is set.
        private static bool InNonLevelScene()
        {
            try { return scnGame.instance == null; } catch { return true; }
        }

        private void DrawHintWindow(int id)
        {
            GUILayout.Label("-- 全局快捷键 --");
            GUILayout.Label("F3 开启/关闭 修改器菜单");
            GUILayout.Label("-- 关卡内 --");
            GUILayout.Label("F4 一键通关");
            GUILayout.Label("F5 快速重开");
        }

        private void DrawKeysWindow(int id)
        {
            var held = new List<string>();
            foreach (var k in OverlayKeys)
                if (Input.GetKey(k)) held.Add(KeyName(k));
            GUILayout.Label(held.Count > 0 ? string.Join(" + ", held) : "—", Big());
            GUILayout.Label(_recent.Count > 0 ? string.Join("  ", _recent.Select(h => h.name)) : " ", Lbl());
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void DrawWindow(int id)
        {
            // 总开关（默认开启）。关闭后整个修改器暂停、释放对游戏的接管，下方功能全部置灰。
            // 始终可点（不受置灰影响），以便随时重新启用。绿=开 / 红=关，与 Windows 版一致。
            var prevColor = GUI.color;
            GUI.color = Cheats.masterEnabled ? new Color(0.55f, 1f, 0.55f) : new Color(1f, 0.5f, 0.5f);
            Cheats.masterEnabled = GUILayout.Toggle(Cheats.masterEnabled,
                Cheats.masterEnabled
                    ? "  ● 修改器总开关：开   （点此关闭＝完全不干预游戏 / 编辑器）"
                    : "  ○ 修改器总开关：关   （已暂停，游戏恢复原版；点此重新启用）", "Button");
            GUI.color = prevColor;
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            if (_legacy)
            {
                if (GUILayout.Toggle(_legacyTab == 0, " 普通 ", "Button")) _legacyTab = 0;
                if (GUILayout.Toggle(_legacyTab == 1, " 开发者 ", "Button")) _legacyTab = 1;
                if (GUILayout.Toggle(_legacyTab == 2, " 高级 ", "Button")) _legacyTab = 2;
                if (GUILayout.Toggle(_legacyTab == 3, " 关卡直达 ", "Button")) _legacyTab = 3;
            }
            else
            {
                if (GUILayout.Toggle(_tab == 0, " 普通 ", "Button")) _tab = 0;
                if (GUILayout.Toggle(_tab == 1, " 存档修改 ", "Button")) _tab = 1;
                if (GUILayout.Toggle(_tab == 2, " 关卡直达 ", "Button")) _tab = 2;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("免费开源 严禁倒卖 · github.com/Cohenjikan/RhythmDoctorTrainer", Lbl());
            GUILayout.Space(4);

            _scroll = GUILayout.BeginScrollView(_scroll);
            bool prevEnabled = GUI.enabled;
            GUI.enabled = Cheats.masterEnabled;   // 总开关关闭时整页置灰、功能不可用
            if (_legacy)
                switch (_legacyTab) { case 1: DrawLegacyDev(); break; case 2: DrawLegacyAdvanced(); break; case 3: DrawLevels(); break; default: DrawLegacyNormal(); break; }
            else
                switch (_tab) { case 1: DrawSave(); break; case 2: DrawLevels(); break; default: DrawNormal(); break; }
            GUI.enabled = prevEnabled;
            GUILayout.EndScrollView();

            bool inGame = false; try { inGame = scnGame.instance != null; } catch { }
            GUILayout.Label(inGame ? "● 当前在关卡内" : "○ 当前在菜单/编辑器", Lbl());
            if (_legacy)
            {
                if (GUILayout.Button("新版菜单")) _legacy = false;
            }
            else if (GUILayout.Button("旧版菜单")) { _legacy = true; _legacyTab = 0; }
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
        }

        // ---------------- 普通（新版） ----------------
        private void DrawNormal()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("秒通本关")) Run("WinLevel", () => { var g = scnGame.instance; if (g != null) g.WinLevel(); });
            if (GUILayout.Button("快速重开")) Run("Restart", () => { var g = scnGame.instance; if (g != null) g.Restart(false); });
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            bool showHint = GUILayout.Toggle(Cheats.showHintWindow, " 左上角提示窗口（菜单关闭时显示快捷键）");
            if (showHint != Cheats.showHintWindow) { Cheats.showHintWindow = showHint; SaveHintPref(); }
            Indent(() => GUILayout.Label($"关闭后左上角的快捷键提示窗不再出现（按 [{MenuKey}] 仍可随时呼出本菜单）。此开关会被记住，重启后保持。", Lbl()));

            GUILayout.Space(6);
            Cheats.speedOverride = GUILayout.Toggle(Cheats.speedOverride, $" 变速 {Cheats.speed:0.00}x（重开本关生效）");
            if (Cheats.speedOverride)
                Indent(() =>
                {
                    GUILayout.BeginHorizontal();
                    Cheats.speed = GUILayout.HorizontalSlider(Cheats.speed, 0.1f, 3.0f);
                    if (GUILayout.Button("1x", GUILayout.Width(36))) Cheats.speed = 1f;
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button("立即变速（实验：本关直接生效，不重开）"))
                        Run("LiveSpeed", () => ApplyLiveSpeed(Cheats.speed));
                });

            Cheats.autoplay = GUILayout.Toggle(Cheats.autoplay, " Autoplay 自动演奏");
            if (Cheats.autoplay)
                Indent(() => Cheats.forceFlawless = GUILayout.Toggle(Cheats.forceFlawless, " 保留完美计算"));

            Cheats.autoRestartOnMiss = GUILayout.Toggle(Cheats.autoRestartOnMiss, " 失误(miss)自动重开");
            Cheats.instantDialogue = GUILayout.Toggle(Cheats.instantDialogue, " 瞬间对白");
            Cheats.devMode = GUILayout.Toggle(Cheats.devMode, " 开发者总开关");
            Cheats.debugMode = GUILayout.Toggle(Cheats.debugMode, " 调试模式（Debug Mode）");
            Cheats.samuraiMode = GUILayout.Toggle(Cheats.samuraiMode, " 武士模式");
            try { RDC.booth = GUILayout.Toggle(RDC.booth, " 展会模式（谨慎！会破坏存档）"); } catch { }
            Cheats.keyOverlay = GUILayout.Toggle(Cheats.keyOverlay, " 按键显示（录制用悬浮窗）");

            GUILayout.Space(6);
            _showMisc = GUILayout.Toggle(_showMisc, _showMisc ? "▼ 杂项调试标志" : "▶ 杂项调试标志", "Button");
            if (_showMisc)
            {
                var ds = DebugSettings.instance;
                Indent(() =>
                {
                    ds.NoPro = GUILayout.Toggle(ds.NoPro, " NoPro");
                    ds.ForceNoSteamworks = GUILayout.Toggle(ds.ForceNoSteamworks, " ForceNoSteamworks");
                    ds.EmulateMobile = GUILayout.Toggle(ds.EmulateMobile, " EmulateMobile（模拟手机端）");
                    ds.RunningOnSteamDeck = GUILayout.Toggle(ds.RunningOnSteamDeck, " RunningOnSteamDeck");
                    ds.DebugAmbience = GUILayout.Toggle(ds.DebugAmbience, " DebugAmbience");
                    ds.PauseOnFocusLost = GUILayout.Toggle(ds.PauseOnFocusLost, " PauseOnFocusLost（失焦暂停）");
                });
            }
        }

        // ---------------- 存档修改（新版） ----------------
        private void DrawSave()
        {
            GUILayout.Label("⚠ 存档修改无法撤回 谨慎修改", Warn());

            // 目标槽位 = 游戏的 currentSlotIndex；所有 Persistence 写入都落到这个槽。
            GUILayout.BeginHorizontal();
            GUILayout.Label("目标槽位:", GUILayout.Width(70));
            int cur = 0; try { cur = Persistence.currentSlotIndex; } catch { }
            for (int s = 0; s < 3; s++)
            {
                bool on = cur == s;
                if (GUILayout.Toggle(on, $" 槽 {s + 1} ", "Button") && !on)
                {
                    int target = s;
                    Run("SelectSlot " + (s + 1), () => { Persistence.currentSlotIndex = target; InvalidateCaches(); });
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("默认为游戏当前使用的存档；切换后本页所有修改写入所选槽位", Lbl());
            GUILayout.Space(4);

            if (!_saveLoaded) LoadSaveFields();
            if (_completion != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(_completion, Lbl());
                if (GUILayout.Button("刷新", GUILayout.Width(56))) InvalidateCaches();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(4);

            if (GUILayout.Button("标记通关"))
                Run("SetIsGameDone", () => { Persistence.SetIsGameDone(true); Persistence.SaveAll(); InvalidateCaches(); });
            if (GUILayout.Button("存档全 S+（含小游戏纪录 99999）"))
                Run("AllLevelsToSPlus", () =>
                {
                    SetAllLevelsTo(Rank.FromString("S+"));
                    Persistence.SetRhythmWeightlifterBestInfiniteRound(99999);
                    Persistence.SetLevelScore(BeansLevel, 99999, true);
                    Persistence.SetRhythmStackerScore(99999);
                    Persistence.SaveAll();
                    InvalidateCaches();
                });
            if (GUILayout.Button("一键推进剧情"))
            { Run("RevealStory", RevealAllStory); _paigeStays = true; InvalidateCaches(); }
            if (GUILayout.Button("解锁全部成就（写入 Steam）"))
                Run("UnlockAllAchievements", UnlockAllAchievements);

            bool paige = GUILayout.Toggle(_paigeStays, " 佩奇留下（结局分支，含剧透，改动立即写入存档）");
            if (paige != _paigeStays)
            {
                _paigeStays = paige;
                Run("SetPaigeEnding", () => { Persistence.SetPaigeEnding(paige); Persistence.SaveAll(); });
            }

            GUILayout.Space(8);
            NumRow("举重最佳轮数", ref _wlRoundText,
                t => { Persistence.SetRhythmWeightlifterBestInfiniteRound(ParseCount(t)); });
            NumRow("豆子跳最高分", ref _beansText,
                t => { Persistence.SetLevelScore(BeansLevel, ParseCount(t), true); });
            NumRow("叠方块最高分", ref _stackerText,
                t => { Persistence.SetRhythmStackerScore(ParseCount(t)); });
            NumRow("Tempres 纪录", ref _tempresText,
                t => { Persistence.SetTempresScore(ParseFloatVal(t)); });

            GUILayout.Space(8);
            _wipeConfirm = GUILayout.Toggle(_wipeConfirm, " 我确认删除操作不可恢复");
            GUI.enabled = _wipeConfirm;
            GUILayout.BeginHorizontal();
            GUILayout.Label("删除槽位 (1-3)", GUILayout.Width(110));
            _slotDelText = GUILayout.TextField(_slotDelText ?? "", GUILayout.Width(30));
            if (GUILayout.Button("删除该槽", GUILayout.Width(90)))
            { Run("DeleteSlot", () => { Persistence.DeleteSlotData(ParseSlot(_slotDelText)); Persistence.SaveAll(); InvalidateCaches(); }); _wipeConfirm = false; }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("🗑 删除全部存档（不可恢复）"))
            { Run("DeleteSavedData", () => { Persistence.DeleteSavedData(); InvalidateCaches(); }); _wipeConfirm = false; }
            GUI.enabled = true;
        }

        // label + text field + 写入 button, shared layout for the record editors
        private void NumRow(string label, ref string text, Action<string> write)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(110));
            text = GUILayout.TextField(text ?? "", GUILayout.Width(70));
            string captured = text;
            if (GUILayout.Button("写入", GUILayout.Width(56)))
                Run("Write " + label, () => { write(captured); Persistence.SaveAll(); });
            GUILayout.EndHorizontal();
        }

        private static string BeansLevel => Level.BeansHopper.ToString();

        private static int ParseCount(string text)
        {
            int n;
            if (!int.TryParse((text ?? "").Trim(), out n) || n < 0)
                throw new Exception("请输入 0 或正整数");
            return n;
        }

        private static float ParseFloatVal(string text)
        {
            float f;
            if (!float.TryParse((text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out f) || f < 0f)
                throw new Exception("请输入非负数字");
            return f;
        }

        // slots are shown 1-based in the UI, stored 0-based
        private static int ParseSlot(string text)
        {
            int n;
            if (!int.TryParse((text ?? "").Trim(), out n) || n < 1 || n > 3)
                throw new Exception("槽位必须是 1-3");
            return n - 1;
        }

        private void InvalidateCaches()
        {
            _saveLoaded = false;
        }

        private void LoadSaveFields()
        {
            _saveLoaded = true;
            try { _wlRoundText = Persistence.GetBestInfiniteRound().ToString(); } catch { _wlRoundText = "0"; }
            try { _beansText = Persistence.GetLevelScore(BeansLevel).ToString(); } catch { _beansText = "0"; }
            try { _stackerText = Persistence.GetRhythmStackerScore().ToString(); } catch { _stackerText = "0"; }
            try { _tempresText = Persistence.GetTempresScore().ToString("0.###", CultureInfo.InvariantCulture); } catch { _tempresText = "0"; }
            try { _paigeStays = Persistence.GetPaigeEnding(); } catch { _paigeStays = false; }
            try
            {
                var gc = Persistence.GetGameCompletion();
                float pct = gc.percentCompletion <= 1.5f ? gc.percentCompletion * 100f : gc.percentCompletion;
                _completion = string.Format(CultureInfo.InvariantCulture,
                    "游戏完成度 {0:0.#}% · 已通关 {1:0} · 完美 {2:0}", pct, gc.passedLevels, gc.perfectLevels);
            }
            catch { _completion = null; }
        }

        // ---------------- 旧版 · 普通玩家 ----------------
        private void DrawLegacyNormal()
        {
            Section("录制 / 演示");
            Cheats.autoplay = GUILayout.Toggle(Cheats.autoplay, " Autoplay — 全程满分自动演奏（进关卡生效）");
            Indent(() => Cheats.forceFlawless = GUILayout.Toggle(Cheats.forceFlawless, " 保留「完美/JCI」结算标记"));

            GUILayout.Space(6);
            Cheats.speedOverride = GUILayout.Toggle(Cheats.speedOverride, $" 游戏变速：{Cheats.speed:0.00}x（含音高）");
            Indent(() => {
                Cheats.speed = GUILayout.HorizontalSlider(Cheats.speed, 0.1f, 3.0f);
                if (GUILayout.Button("1x", GUILayout.Width(36))) Cheats.speed = 1f;
            });
            Indent(() => {
                GUILayout.Label("⚠ 在关卡开始/重开时生效，无法中途变速（引擎限制）", Lbl());
                if (GUILayout.Button("重开本关并应用"))
                    Run("Restart", () => { var g = scnGame.instance; if (g != null) g.Restart(false); });
            });

            GUILayout.Space(6);
            Cheats.widenJudge = GUILayout.Toggle(Cheats.widenJudge, $" 放宽判定窗口 ×{Cheats.judgeMult:0.0}");
            Indent(() => Cheats.judgeMult = GUILayout.HorizontalSlider(Cheats.judgeMult, 1f, 10f));

            GUILayout.Space(8);
            Section("便利 / 玩法");
            Cheats.instantDialogue = GUILayout.Toggle(Cheats.instantDialogue, " 瞬间对白 — 跳过剧情文本");
            Cheats.skipTransitions = GUILayout.Toggle(Cheats.skipTransitions, " 跳过菜单转场");
            Cheats.unlimitedFps = GUILayout.Toggle(Cheats.unlimitedFps, " 解锁帧率上限");
            Cheats.muteBeatSounds = GUILayout.Toggle(Cheats.muteBeatSounds, " 关闭节拍提示音");

            GUILayout.Space(8);
            Section("关卡控制（需在关卡内）");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("秒通本关")) Run("WinLevel", () => { var g = scnGame.instance; if (g != null) g.WinLevel(); });
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            Section("解锁");
            if (GUILayout.Button("解锁全部关卡（写入存档）"))
                Run("UnlockAllLevels", UnlockAllLevelsForce);
            GUILayout.Label("点完请退出选关界面再重新进入（或重启游戏）刷新显示；无需删档。", Lbl());

            GUILayout.Space(8);
            Section("彩蛋");
            Cheats.samuraiMode = GUILayout.Toggle(Cheats.samuraiMode, " 武士文本模式（SAMURAI.）");

            GUILayout.Space(8);
            Section("界面");
            bool showHintL = GUILayout.Toggle(Cheats.showHintWindow, " 左上角提示窗口（菜单关闭时显示快捷键）");
            if (showHintL != Cheats.showHintWindow) { Cheats.showHintWindow = showHintL; SaveHintPref(); }
            Indent(() => GUILayout.Label("关闭后左上角的快捷键提示窗不再出现；此开关会被记住，重启后保持。", Lbl()));
        }

        // ---------------- 旧版 · 开发者 ----------------
        private void DrawLegacyDev()
        {
            Section("开发者模式");
            Cheats.devMode = GUILayout.Toggle(Cheats.devMode, " 开发者模式总开关（isDev=true）");
            GUILayout.Label("开后游戏内可用 Ctrl+Home 或输入 DESPACIT0 切换调试，并解锁部分 dev 工具。", Lbl());

            GUILayout.Space(6);
            Cheats.debugMode = GUILayout.Toggle(Cheats.debugMode, " 调试模式（Debug）");
            GUILayout.Label("⚠ 会在画面显示调试文字，不适合干净录制。", Lbl());

            GUILayout.Space(8);
            Section("进度 / 评级");
            if (GUILayout.Button("标记游戏通关（SetIsGameDone）"))
                Run("SetIsGameDone", () => Persistence.SetIsGameDone(true));
            if (GUILayout.Button("全部关卡刷成 S 评级"))
                Run("AllLevelsToS", () => SetAllLevelsTo(Rank.FromString("S")));
            if (GUILayout.Button("一键推进全部剧情（铺满 hub 角色）"))
                Run("RevealStory", RevealAllStory);
            GUILayout.Label("把所有过场/章节标志设为已播放，让 hub 里角色就位；点完退菜单重进。含 Paige 结局分支。\n（想直接录关卡，更推荐用「关卡直达」标签，无需改剧情）", Lbl());

            GUILayout.Space(8);
            Section("成就");
            Cheats.noAchievements = GUILayout.Toggle(Cheats.noAchievements, " 关闭成就发放（防污染账号）");
            if (GUILayout.Button("⚠ 解锁全部成就（写入 Steam 账号！）"))
                Run("UnlockAllAchievements", UnlockAllAchievements);

            GUILayout.Space(8);
            Section("存档");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("打开存档目录"))
                Run("OpenSaveDir", () => RDUtils.RevealInExplorer(Persistence.DataPath));
            GUILayout.EndHorizontal();
            _wipeConfirm = GUILayout.Toggle(_wipeConfirm, " 我确认要删除全部存档");
            GUI.enabled = _wipeConfirm;
            if (GUILayout.Button("🗑 删除全部存档（不可恢复）"))
            { Run("DeleteSavedData", () => Persistence.DeleteSavedData()); _wipeConfirm = false; }
            GUI.enabled = true;

            GUILayout.Space(8);
            _showMisc = GUILayout.Toggle(_showMisc, _showMisc ? "▼ 杂项调试标志" : "▶ 杂项调试标志", "Button");
            if (_showMisc)
            {
                var ds = DebugSettings.instance;
                GUILayout.Label("（dev/冷门，直接读写 DebugSettings）", Lbl());
                ds.NoPro = GUILayout.Toggle(ds.NoPro, " NoPro");
                ds.ForceNoSteamworks = GUILayout.Toggle(ds.ForceNoSteamworks, " ForceNoSteamworks");
                ds.EmulateMobile = GUILayout.Toggle(ds.EmulateMobile, " EmulateMobile（模拟手机端）");
                ds.RunningOnSteamDeck = GUILayout.Toggle(ds.RunningOnSteamDeck, " RunningOnSteamDeck");
                ds.DebugAmbience = GUILayout.Toggle(ds.DebugAmbience, " DebugAmbience");
                ds.PaigeStays = GUILayout.Toggle(ds.PaigeStays, " PaigeStays（剧情分支，含剧透）");
                ds.PauseOnFocusLost = GUILayout.Toggle(ds.PauseOnFocusLost, " PauseOnFocusLost（失焦暂停）");
            }
        }

        // ---------------- 旧版 · 高级（实验） ----------------
        private void DrawLegacyAdvanced()
        {
            GUILayout.Label("⚠ 实验区：以下涉及场景跳转/底层状态，可能需在特定界面使用，个别可能不稳。出问题重进关卡或重启游戏即可。", Lbl());

            GUILayout.Space(6);
            Section("音画校准（影响对点，谨慎）");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("读取当前")) LoadCalibration();
            if (GUILayout.Button("应用并保存"))
                Run("SetCalibration", () => Persistence.SetCalibrationValues(Cheats.calV, Cheats.calI, Cheats.calIP2, Cheats.calLat));
            GUILayout.EndHorizontal();
            if (_calLoaded)
            {
                CalRow("视觉偏移 v", ref Cheats.calV);
                CalRow("输入偏移 i", ref Cheats.calI);
                CalRow("输入偏移 i(P2)", ref Cheats.calIP2);
                CalRow("延迟 latency", ref Cheats.calLat);
            }
            else GUILayout.Label("先点「读取当前」载入数值。", Lbl());

            GUILayout.Space(8);
            Section("无限模式（举重小游戏的无尽档）");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"最佳轮数：{_bestRound}", GUILayout.Width(110));
            if (GUILayout.Button("-", GUILayout.Width(28))) _bestRound = Mathf.Max(0, _bestRound - 1);
            if (GUILayout.Button("+", GUILayout.Width(28))) _bestRound++;
            if (GUILayout.Button("读取")) Run("GetBestRound", () => _bestRound = Persistence.GetBestInfiniteRound());
            if (GUILayout.Button("写入")) Run("SetBestRound", () => Persistence.SetRhythmWeightlifterBestInfiniteRound(_bestRound));
            GUILayout.EndHorizontal();
            GUILayout.Label("无限模式本身在「举重节奏」打到底即可进入，非作弊；这里只改记录。", Lbl());

            GUILayout.Space(8);
            Section("展会 / 隐藏");
            try { RDC.booth = GUILayout.Toggle(RDC.booth, " 展会(Booth)模式（自助机模式；也会让 isDev 生效）"); } catch { }
            if (GUILayout.Button("狗狗模式（下次进 Les Mis 关生效）"))
                Run("DogMode", () => scnGame.loadDogMode = true);
            if (GUILayout.Button("跳到隐藏曲 Song of the Sea（建议在主菜单点）"))
                Run("SongOfTheSea", () => scnBase.GoToLevelWithEnum(Level.SongOfTheSea));
            GUILayout.Label("手动秘籍：主菜单 JJDF=隐藏曲；选关 ←→ 序列=狗狗模式。", Lbl());
        }

        // ---------------- 关卡直达（保持旧版样式） ----------------
        private void DrawLevels()
        {
            GUILayout.Label("点关卡名直接进入，绕过 hub 的剧情/NPC 限制。配合「普通」页的 Autoplay 即可录制任意关卡。", Lbl());
            GUILayout.BeginHorizontal();
            GUILayout.Label("筛选:", GUILayout.Width(40));
            _lvFilter = GUILayout.TextField(_lvFilter ?? "");
            if (GUILayout.Button("×", GUILayout.Width(26))) _lvFilter = "";
            GUILayout.EndHorizontal();
            GUILayout.Space(2);

            string f = (_lvFilter ?? "").ToLowerInvariant();
            int shown = 0;
            foreach (Level lv in Enum.GetValues(typeof(Level)))
            {
                if (lv == Level.None) continue;
                string name = lv.ToString();
                if (f.Length > 0 && name.ToLowerInvariant().IndexOf(f) < 0) continue;
                Level captured = lv;
                if (GUILayout.Button(name))
                    Run("GoTo " + name, () => scnBase.GoToLevelWithEnum(captured));
                shown++;
            }
            if (shown == 0) GUILayout.Label("（无匹配关卡）", Lbl());
        }

        private static void RevealAllStory()
        {
            Persistence.SetPlayedPassedLevelCutscene(true);
            Persistence.SetPlayedPostAct2Cutscene(true);
            Persistence.SetPlayedPostAct3Cutscene(true);
            Persistence.SetPlayedPostAct4Cutscene(true);
            Persistence.SetPlayedPreAct5Cutscene(true);
            Persistence.SetPlayedPreAct6Cutscene(true);
            Persistence.SetPlayedHaileyDuetIntroduction(true);
            Persistence.SetPlayedPreBitternessCutscene(true);
            Persistence.SetPlayedRooftopCutscene(true);
            Persistence.SetPlayedAct6Intro(true);
            Persistence.SetPaigeEnding(true);
            Persistence.SetIsGameDone(true);
            UnlockAllLevelsForce();
            try { Persistence.SaveAll(); } catch { }
        }

        private void CalRow(string label, ref float val)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {val:0.0000}", GUILayout.Width(150));
            val = GUILayout.HorizontalSlider(val, -0.3f, 0.3f);
            GUILayout.EndHorizontal();
        }

        private void LoadCalibration()
        {
            Run("GetCalibration", () =>
            {
                Persistence.GetCalibrationValues(out var v, out var i, out var i2, out var l);
                Cheats.calV = v; Cheats.calI = i; Cheats.calIP2 = i2; Cheats.calLat = l;
                _calLoaded = true;
            });
        }

        // Force-write rank -1 (= unlocked / NotPassed) for every level, bypassing the
        // IsBetterRank gate, then mark game done and flush to disk.
        private static void UnlockAllLevelsForce()
        {
            foreach (Level lv in Enum.GetValues(typeof(Level)))
            {
                if (lv == Level.None) continue;
                try { Persistence.SetLevelRank(lv, (Rank)(-1), true, true); } catch { }
            }
            try { Persistence.SetIsGameDone(true); } catch { }
            try { Persistence.SaveAll(); } catch { }
        }

        private static void SetAllLevelsTo(Rank rank)
        {
            foreach (Level lv in Enum.GetValues(typeof(Level)))
            {
                if (lv == Level.None) continue;
                try { Persistence.SetLevelRank(lv, rank, true, true); } catch { }
            }
            try { Persistence.SaveAll(); } catch { }
        }

        private static void UnlockAllAchievements()
        {
            foreach (Achievement a in Enum.GetValues(typeof(Achievement)))
            { try { Persistence.UnlockAchievement(a, storeStats: true); } catch { } }
        }

        // ---------------- helpers ----------------
        private static void Run(string what, Action act)
        {
            try { act(); Log.Info("Trainer action OK: " + what); }
            catch (Exception e) { Log.Error("Trainer action FAILED (" + what + "): " + e); }
        }

        // Hint-window visibility is the only persisted trainer setting (PlayerPrefs).
        private static void LoadPrefs()
        {
            try { Cheats.showHintWindow = PlayerPrefs.GetInt(HintPrefKey, 1) != 0; } catch { }
        }
        private static void SaveHintPref()
        {
            try { PlayerPrefs.SetInt(HintPrefKey, Cheats.showHintWindow ? 1 : 0); PlayerPrefs.Save(); } catch { }
        }

        private static void Indent(Action body)
        {
            GUILayout.BeginHorizontal(); GUILayout.Space(18);
            GUILayout.BeginVertical(); body(); GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static void Section(string title) => GUILayout.Label("── " + title + " ──", Hdr());

        private static GUIStyle _hdr, _lbl, _warn, _big;
        private static GUIStyle Hdr()
        {
            if (_hdr == null) _hdr = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            return _hdr;
        }
        private static GUIStyle Lbl()
        {
            if (_lbl == null) _lbl = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 };
            return _lbl;
        }
        private static GUIStyle Warn()
        {
            if (_warn == null)
            {
                _warn = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true };
                _warn.normal.textColor = new Color(1f, 0.45f, 0.35f);
            }
            return _warn;
        }
        private static GUIStyle Big()
        {
            if (_big == null) _big = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 18 };
            return _big;
        }
    }
}
