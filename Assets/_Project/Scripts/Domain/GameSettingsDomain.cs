using System;

namespace TacticaUniversalis.Domain
{
    public enum DisplayResolutionPreset
    {
        P720 = 0,
        P1080 = 1,
        P2K = 2,
        P4K = 3
    }

    public enum DisplayWindowMode
    {
        Windowed = 0,
        Borderless = 1,
        ExclusiveFullscreen = 2
    }

    public enum DisplayFrameRate
    {
        Fps30 = 30,
        Fps60 = 60,
        Fps120 = 120
    }

    public enum BattleSimMode
    {
        Quick = 0,
        BehaviorTree = 1,
        MachineLearning = 2
    }

    public enum AutoSaveIntervalMinutes
    {
        Min5 = 5,
        Min10 = 10,
        Min15 = 15,
        Min30 = 30
    }

    [Serializable]
    public sealed class GameSettingsState
    {
        public DisplayResolutionPreset resolution = DisplayResolutionPreset.P1080;
        public DisplayWindowMode windowMode = DisplayWindowMode.Windowed;
        public DisplayFrameRate frameRate = DisplayFrameRate.Fps60;
        public float masterVolume = 1f;
        public float sfxVolume = 1f;
        public float musicVolume = 1f;
        public bool muteWhenUnfocused = true;
        public BattleSimMode battleSimMode = BattleSimMode.Quick;
        public bool autoSaveEnabled = false;
        public AutoSaveIntervalMinutes autoSaveIntervalMinutes = AutoSaveIntervalMinutes.Min10;
        /// <summary>正式賽結束回到生涯介面時是否自動暫停時間。預設為是。</summary>
        public bool pauseOnMatchEnd = true;
        /// <summary>S 變革事件觸發時是否暫停。S+ 恒為暫停。</summary>
        public bool pauseOnEventS = true;
        /// <summary>A 歷史事件觸發時是否暫停。</summary>
        public bool pauseOnEventA = true;
        /// <summary>B 隊伍事件觸發時是否暫停。</summary>
        public bool pauseOnEventB = true;
        /// <summary>C 場外事件觸發時是否暫停。預設否。</summary>
        public bool pauseOnEventC = false;
    }

    public static class GameSettingsRules
    {
        public static readonly DisplayResolutionPreset[] Resolutions =
        {
            DisplayResolutionPreset.P720,
            DisplayResolutionPreset.P1080,
            DisplayResolutionPreset.P2K,
            DisplayResolutionPreset.P4K
        };

        public static readonly DisplayWindowMode[] WindowModes =
        {
            DisplayWindowMode.Windowed,
            DisplayWindowMode.Borderless,
            DisplayWindowMode.ExclusiveFullscreen
        };

        public static readonly DisplayFrameRate[] FrameRates =
        {
            DisplayFrameRate.Fps30,
            DisplayFrameRate.Fps60,
            DisplayFrameRate.Fps120
        };

        public static readonly BattleSimMode[] BattleModes =
        {
            BattleSimMode.Quick,
            BattleSimMode.BehaviorTree,
            BattleSimMode.MachineLearning
        };

        public static readonly AutoSaveIntervalMinutes[] AutoSaveIntervals =
        {
            AutoSaveIntervalMinutes.Min5,
            AutoSaveIntervalMinutes.Min10,
            AutoSaveIntervalMinutes.Min15,
            AutoSaveIntervalMinutes.Min30
        };

        public static bool IsBattleModeImplemented(BattleSimMode mode) =>
            mode == BattleSimMode.Quick || mode == BattleSimMode.BehaviorTree;

        public static void GetResolutionSize(DisplayResolutionPreset preset, out int width, out int height)
        {
            switch (preset)
            {
                case DisplayResolutionPreset.P720:
                    width = 1280;
                    height = 720;
                    break;
                case DisplayResolutionPreset.P2K:
                    width = 2560;
                    height = 1440;
                    break;
                case DisplayResolutionPreset.P4K:
                    width = 3840;
                    height = 2160;
                    break;
                default:
                    width = 1920;
                    height = 1080;
                    break;
            }
        }

        public static string ResolutionLabel(DisplayResolutionPreset preset) =>
            preset switch
            {
                DisplayResolutionPreset.P720 => "720p",
                DisplayResolutionPreset.P2K => "2K",
                DisplayResolutionPreset.P4K => "4K",
                _ => "1080p"
            };

        public static string WindowModeLabel(DisplayWindowMode mode) =>
            mode switch
            {
                DisplayWindowMode.Borderless => "無邊框視窗",
                DisplayWindowMode.ExclusiveFullscreen => "全螢幕",
                _ => "全視窗"
            };

        public static string FrameRateLabel(DisplayFrameRate rate) =>
            ((int)rate) + " FPS";

        public static string BattleModeLabel(BattleSimMode mode) =>
            mode switch
            {
                BattleSimMode.BehaviorTree => "一般模式",
                BattleSimMode.MachineLearning => "實驗性模式",
                _ => "快速模式"
            };

        public static string BattleModeButtonLabel(BattleSimMode mode)
        {
            if (IsBattleModeImplemented(mode))
                return BattleModeLabel(mode);
            return BattleModeLabel(mode) + " · 即將推出";
        }

        public static string BattleModeHoverDescription(BattleSimMode mode) =>
            mode switch
            {
                BattleSimMode.BehaviorTree =>
                    "相較快速模式，一般模式會在地圖上實際移動與交戰，並顯示血條。選手會逐項購買武器與護甲，對局中也會使用道具。",
                BattleSimMode.MachineLearning =>
                    "以強化學習等方式讓 AI 模擬較接近現實的打法；效果可能不穩定。\n（目前尚未實裝，僅預留選項。）",
                _ =>
                    "精簡快速結算，著重節奏與結果，不做完整地圖移動演出。"
            };

        public static string AutoSaveIntervalLabel(AutoSaveIntervalMinutes minutes) =>
            ((int)minutes) + " 分鐘";

        public static GameSettingsState Normalize(GameSettingsState state)
        {
            if (state == null)
                return CreateDefault();

            state.masterVolume = Clamp01(state.masterVolume);
            state.sfxVolume = Clamp01(state.sfxVolume);
            state.musicVolume = Clamp01(state.musicVolume);

            if (!Enum.IsDefined(typeof(DisplayResolutionPreset), state.resolution))
                state.resolution = DisplayResolutionPreset.P1080;
            if (!Enum.IsDefined(typeof(DisplayWindowMode), state.windowMode))
                state.windowMode = DisplayWindowMode.Windowed;
            if (!IsKnownFrameRate(state.frameRate))
                state.frameRate = DisplayFrameRate.Fps60;
            if (!Enum.IsDefined(typeof(BattleSimMode), state.battleSimMode))
                state.battleSimMode = BattleSimMode.Quick;
            if (!IsBattleModeImplemented(state.battleSimMode))
                state.battleSimMode = BattleSimMode.Quick;
            if (!IsKnownAutoSaveInterval(state.autoSaveIntervalMinutes))
                state.autoSaveIntervalMinutes = AutoSaveIntervalMinutes.Min10;

            return state;
        }

        public static GameSettingsState CreateDefault() => Normalize(new GameSettingsState());

        public static GameSettingsState Clone(GameSettingsState source)
        {
            source = Normalize(source);
            return new GameSettingsState
            {
                resolution = source.resolution,
                windowMode = source.windowMode,
                frameRate = source.frameRate,
                masterVolume = source.masterVolume,
                sfxVolume = source.sfxVolume,
                musicVolume = source.musicVolume,
                muteWhenUnfocused = source.muteWhenUnfocused,
                battleSimMode = source.battleSimMode,
                autoSaveEnabled = source.autoSaveEnabled,
                autoSaveIntervalMinutes = source.autoSaveIntervalMinutes,
                pauseOnMatchEnd = source.pauseOnMatchEnd,
                pauseOnEventS = source.pauseOnEventS,
                pauseOnEventA = source.pauseOnEventA,
                pauseOnEventB = source.pauseOnEventB,
                pauseOnEventC = source.pauseOnEventC
            };
        }

        private static float Clamp01(float value) =>
            value < 0f ? 0f : (value > 1f ? 1f : value);

        private static bool IsKnownFrameRate(DisplayFrameRate rate) =>
            rate == DisplayFrameRate.Fps30 ||
            rate == DisplayFrameRate.Fps60 ||
            rate == DisplayFrameRate.Fps120;

        private static bool IsKnownAutoSaveInterval(AutoSaveIntervalMinutes minutes) =>
            minutes == AutoSaveIntervalMinutes.Min5 ||
            minutes == AutoSaveIntervalMinutes.Min10 ||
            minutes == AutoSaveIntervalMinutes.Min15 ||
            minutes == AutoSaveIntervalMinutes.Min30;
    }
}
