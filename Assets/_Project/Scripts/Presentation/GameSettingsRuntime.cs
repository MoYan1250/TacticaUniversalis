using TacticaUniversalis.Domain;
using TacticaUniversalis.Infrastructure;
using UnityEngine;

namespace TacticaUniversalis.Presentation
{
    public static class GameSettingsRuntime
    {
        private static GameSettingsState _current = GameSettingsRules.CreateDefault();
        private static bool _hasFocus = true;
        private static float _volumeBeforeMute = 1f;

        public static GameSettingsState Current => GameSettingsRules.Clone(_current);

        public static GameSettingsState LoadAndApply()
        {
            _current = GameSettingsStore.Load();
            Apply(_current, persist: false);
            return Current;
        }

        public static void Apply(GameSettingsState state, bool persist = true)
        {
            if (state == null) return;
            _current = GameSettingsRules.Normalize(GameSettingsRules.Clone(state));
            ApplyDisplay(_current);
            ApplyFrameRate(_current);
            ApplyAudio(_current);
            if (persist)
                GameSettingsStore.Save(_current);
        }

        public static void UpdateAndApply(System.Action<GameSettingsState> mutator)
        {
            if (mutator == null) return;
            GameSettingsState next = GameSettingsRules.Clone(_current);
            mutator(next);
            Apply(next, persist: true);
        }

        public static void HandleFocus(bool focused)
        {
            _hasFocus = focused;
            ApplyAudio(_current);
        }

        public static BattleSimMode EffectiveBattleSimMode()
        {
            BattleSimMode mode = _current.battleSimMode;
            return GameSettingsRules.IsBattleModeImplemented(mode) ? mode : BattleSimMode.Quick;
        }

        private static void ApplyDisplay(GameSettingsState state)
        {
            GameSettingsRules.GetResolutionSize(state.resolution, out int width, out int height);
            FullScreenMode mode = ToFullScreenMode(state.windowMode);
            Screen.SetResolution(width, height, mode);
        }

        private static void ApplyFrameRate(GameSettingsState state)
        {
            UnityEngine.Application.targetFrameRate = (int)state.frameRate;
            QualitySettings.vSyncCount = 0;
        }

        private static void ApplyAudio(GameSettingsState state)
        {
            float master = Mathf.Clamp01(state.masterVolume);
            if (state.muteWhenUnfocused && !_hasFocus)
            {
                if (AudioListener.volume > 0.0001f)
                    _volumeBeforeMute = AudioListener.volume;
                AudioListener.volume = 0f;
                return;
            }

            AudioListener.volume = master;
            _volumeBeforeMute = master;
        }

        private static FullScreenMode ToFullScreenMode(DisplayWindowMode mode) =>
            mode switch
            {
                DisplayWindowMode.Borderless => FullScreenMode.FullScreenWindow,
                DisplayWindowMode.ExclusiveFullscreen => FullScreenMode.ExclusiveFullScreen,
                _ => FullScreenMode.Windowed
            };
    }
}
