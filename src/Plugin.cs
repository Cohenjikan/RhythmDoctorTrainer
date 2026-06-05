using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RDTrainer
{
    // In-game trainer for Rhythm Doctor (单机). Insert opens an overlay with three tabs:
    // 普通玩家 (recording / QoL), 开发者 (dev unlocks), 高级 (experimental / scene-jump).
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.cohen.rdtrainer";
        public const string Name = "RD Trainer (节奏医生修改器)";
        public const string Version = "2.1.0";

        internal static ManualLogSource Log;
        private static ConfigEntry<KeyboardShortcut> _menuKey;

        private bool _menuOpen;
        private int _tab;
        private Rect _win = new Rect(24, 24, 480, 620);
        private Vector2 _scroll;
        private string _lvFilter = "";
        private Font _cjk;
        private bool _lastSpeedOverride;

        // local UI state
        private bool _wipeConfirm;
        private bool _showMisc;
        private bool _calLoaded;
        private int _bestRound = 1;

        private void Awake()
        {
            Log = Logger;
            _menuKey = Config.Bind("General", "MenuKey", new KeyboardShortcut(KeyCode.Insert),
                "Hotkey to open/close the trainer overlay.");
            new Harmony(Guid).PatchAll();
            Log.LogInfo($"{Name} v{Version} loaded. Menu key = {_menuKey.Value}. " +
                        "Patches: isDev / GetHitMargin / FailLevel / isZeroOffset.");
        }

        private void Update()
        {
            try
            {
                if (_menuKey.Value.IsDown()) _menuOpen = !_menuOpen;
                if (_menuOpen) { Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }
                ApplyState();
            }
            catch (Exception e) { Log.LogError("Update: " + e); }
        }

        private void ApplyState()
        {
            var ds = DebugSettings.instance;
            bool inGame = false;
            try { inGame = scnGame.instance != null; } catch { }

            if (inGame)
            {
                if (ds.Auto != Cheats.autoplay) ds.Auto = Cheats.autoplay;
            }

            // Speed: the engine applies it at level Start via `RDTime.speed = scnGame.levelSpeed`,
            // which then scales bpm AND the song's pitch consistently. So we arm the static
            // `scnGame.levelSpeed`; it takes effect on the next level start / restart. Writing
            // RDTime.speed live does NOT work — bpm + currentSong.pitch are fixed at load, so a
            // mid-song change just desyncs.
            try
            {
                if (Cheats.speedOverride)
                {
                    if (scnGame.levelSpeed != Cheats.speed) scnGame.levelSpeed = Cheats.speed;
                }
                else if (_lastSpeedOverride)
                {
                    scnGame.levelSpeed = 1f;
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

        private void EnsureFont()
        {
            if (_cjk != null) return;
            try { _cjk = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "SimSun", "Arial" }, 14); }
            catch { }
        }

        private void OnGUI()
        {
            if (!_menuOpen) return;
            EnsureFont();
            var prev = GUI.skin.font;
            if (_cjk != null) GUI.skin.font = _cjk;
            _win = GUILayout.Window(740181, _win, DrawWindow, $"节奏医生修改器 v{Version}  ·  [{_menuKey.Value}] 开/关");
            GUI.skin.font = prev;
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_tab == 0, " 普通 ", "Button")) _tab = 0;
            if (GUILayout.Toggle(_tab == 1, " 开发者 ", "Button")) _tab = 1;
            if (GUILayout.Toggle(_tab == 2, " 高级 ", "Button")) _tab = 2;
            if (GUILayout.Toggle(_tab == 3, " 关卡直达 ", "Button")) _tab = 3;
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            _scroll = GUILayout.BeginScrollView(_scroll);
            switch (_tab) { case 1: DrawDev(); break; case 2: DrawAdvanced(); break; case 3: DrawLevels(); break; default: DrawNormal(); break; }
            GUILayout.EndScrollView();

            bool inGame = false; try { inGame = scnGame.instance != null; } catch { }
            GUILayout.Label(inGame ? "● 当前在关卡内" : "○ 当前在菜单/编辑器", Lbl());
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
        }

        // ---------------- 普通玩家 ----------------
        private void DrawNormal()
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
            Cheats.noFail = GUILayout.Toggle(Cheats.noFail, " 无敌 — 不会失败/被打断");
            Cheats.instantDialogue = GUILayout.Toggle(Cheats.instantDialogue, " 瞬间对白 — 跳过剧情文本");
            Cheats.skipTransitions = GUILayout.Toggle(Cheats.skipTransitions, " 跳过菜单转场");
            Cheats.unlimitedFps = GUILayout.Toggle(Cheats.unlimitedFps, " 解锁帧率上限");
            Cheats.muteBeatSounds = GUILayout.Toggle(Cheats.muteBeatSounds, " 关闭节拍提示音");

            GUILayout.Space(8);
            Section("关卡控制（需在关卡内）");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("秒通本关")) Run("WinLevel", () => { var g = scnGame.instance; if (g != null) g.WinLevel(); });
            if (GUILayout.Button("跳过本关")) Run("SkipLevel", () => { var g = scnGame.instance; if (g != null) g.SkipLevel(); });
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            Section("解锁");
            if (GUILayout.Button("解锁全部关卡（写入存档）"))
                Run("UnlockAllLevels", UnlockAllLevelsForce);
            GUILayout.Label("点完请退出选关界面再重新进入（或重启游戏）刷新显示；无需删档。", Lbl());

            GUILayout.Space(8);
            Section("彩蛋");
            Cheats.samuraiMode = GUILayout.Toggle(Cheats.samuraiMode, " 武士文本模式（SAMURAI.）");
        }

        // ---------------- 开发者 ----------------
        private void DrawDev()
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
                Run("AllLevelsToS", SetAllLevelsToS);
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

        // ---------------- 高级（实验） ----------------
        private void DrawAdvanced()
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
                Run("SongOfTheSea", () => Traverse.Create(scnBase.instance).Method("GoToLevelWithEnum", Level.SongOfTheSea).GetValue());
            GUILayout.Label("手动秘籍：主菜单 JJDF=隐藏曲；选关 ←→ 序列=狗狗模式。", Lbl());
        }

        // ---------------- 关卡直达 ----------------
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
        // IsBetterRank gate, then mark game done and flush to disk. (The native
        // UnlockAllLevels uses force=false; this version is bulletproof on fresh saves.)
        private static void UnlockAllLevelsForce()
        {
            foreach (Level lv in Enum.GetValues(typeof(Level)))
            {
                if (lv == Level.None) continue;
                try { Persistence.SetLevelRank(lv, (Rank)(-1), force: true); } catch { }
            }
            try { Persistence.SetIsGameDone(true); } catch { }
            try { Persistence.SaveAll(); } catch { }
        }

        private static void SetAllLevelsToS()
        {
            var s = Rank.FromString("S");
            foreach (Level lv in Enum.GetValues(typeof(Level)))
            {
                if (lv == Level.None) continue;
                try { Persistence.SetLevelRank(lv, s, force: true); } catch { }
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
            try { act(); Log.LogInfo("Trainer action OK: " + what); }
            catch (Exception e) { Log.LogError("Trainer action FAILED (" + what + "): " + e); }
        }

        private static void Indent(Action body)
        {
            GUILayout.BeginHorizontal(); GUILayout.Space(18);
            GUILayout.BeginVertical(); body(); GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static void Section(string title) => GUILayout.Label("── " + title + " ──", Hdr());

        private static GUIStyle _hdr, _lbl;
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
    }
}
