using System;
using TacticaUniversalis.Domain;
using UnityEngine;

namespace TacticaUniversalis.Infrastructure
{
    public static class GameSettingsStore
    {
        private const string KeyResolution = "tu.settings.resolution";
        private const string KeyWindowMode = "tu.settings.windowMode";
        private const string KeyFrameRate = "tu.settings.frameRate";
        private const string KeyMasterVolume = "tu.settings.masterVolume";
        private const string KeySfxVolume = "tu.settings.sfxVolume";
        private const string KeyMusicVolume = "tu.settings.musicVolume";
        private const string KeyMuteWhenUnfocused = "tu.settings.muteWhenUnfocused";
        private const string KeyBattleSimMode = "tu.settings.battleSimMode";
        private const string KeyAutoSaveEnabled = "tu.settings.autoSaveEnabled";
        private const string KeyAutoSaveInterval = "tu.settings.autoSaveInterval";
        private const string KeyPauseOnMatchEnd = "tu.settings.pauseOnMatchEnd";
        private const string KeyPauseOnEventS = "tu.settings.pauseOnEventS";
        private const string KeyPauseOnEventA = "tu.settings.pauseOnEventA";
        private const string KeyPauseOnEventB = "tu.settings.pauseOnEventB";
        private const string KeyPauseOnEventC = "tu.settings.pauseOnEventC";

        public static GameSettingsState Load()
        {
            var state = new GameSettingsState
            {
                resolution = (DisplayResolutionPreset)PlayerPrefs.GetInt(
                    KeyResolution, (int)DisplayResolutionPreset.P1080),
                windowMode = (DisplayWindowMode)PlayerPrefs.GetInt(
                    KeyWindowMode, (int)DisplayWindowMode.Windowed),
                frameRate = (DisplayFrameRate)PlayerPrefs.GetInt(
                    KeyFrameRate, (int)DisplayFrameRate.Fps60),
                masterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, 1f),
                sfxVolume = PlayerPrefs.GetFloat(KeySfxVolume, 1f),
                musicVolume = PlayerPrefs.GetFloat(KeyMusicVolume, 1f),
                muteWhenUnfocused = PlayerPrefs.GetInt(KeyMuteWhenUnfocused, 1) != 0,
                battleSimMode = (BattleSimMode)PlayerPrefs.GetInt(
                    KeyBattleSimMode, (int)BattleSimMode.Quick),
                autoSaveEnabled = PlayerPrefs.GetInt(KeyAutoSaveEnabled, 0) != 0,
                autoSaveIntervalMinutes = (AutoSaveIntervalMinutes)PlayerPrefs.GetInt(
                    KeyAutoSaveInterval, (int)AutoSaveIntervalMinutes.Min10),
                pauseOnMatchEnd = PlayerPrefs.GetInt(KeyPauseOnMatchEnd, 1) != 0,
                pauseOnEventS = PlayerPrefs.GetInt(KeyPauseOnEventS, 1) != 0,
                pauseOnEventA = PlayerPrefs.GetInt(KeyPauseOnEventA, 1) != 0,
                pauseOnEventB = PlayerPrefs.GetInt(KeyPauseOnEventB, 1) != 0,
                pauseOnEventC = PlayerPrefs.GetInt(KeyPauseOnEventC, 0) != 0
            };
            return GameSettingsRules.Normalize(state);
        }

        public static void Save(GameSettingsState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            state = GameSettingsRules.Normalize(state);
            PlayerPrefs.SetInt(KeyResolution, (int)state.resolution);
            PlayerPrefs.SetInt(KeyWindowMode, (int)state.windowMode);
            PlayerPrefs.SetInt(KeyFrameRate, (int)state.frameRate);
            PlayerPrefs.SetFloat(KeyMasterVolume, state.masterVolume);
            PlayerPrefs.SetFloat(KeySfxVolume, state.sfxVolume);
            PlayerPrefs.SetFloat(KeyMusicVolume, state.musicVolume);
            PlayerPrefs.SetInt(KeyMuteWhenUnfocused, state.muteWhenUnfocused ? 1 : 0);
            PlayerPrefs.SetInt(KeyBattleSimMode, (int)state.battleSimMode);
            PlayerPrefs.SetInt(KeyAutoSaveEnabled, state.autoSaveEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KeyAutoSaveInterval, (int)state.autoSaveIntervalMinutes);
            PlayerPrefs.SetInt(KeyPauseOnMatchEnd, state.pauseOnMatchEnd ? 1 : 0);
            PlayerPrefs.SetInt(KeyPauseOnEventS, state.pauseOnEventS ? 1 : 0);
            PlayerPrefs.SetInt(KeyPauseOnEventA, state.pauseOnEventA ? 1 : 0);
            PlayerPrefs.SetInt(KeyPauseOnEventB, state.pauseOnEventB ? 1 : 0);
            PlayerPrefs.SetInt(KeyPauseOnEventC, state.pauseOnEventC ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
