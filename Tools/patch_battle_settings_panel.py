# -*- coding: utf-8 -*-
"""QuickMatch: reuse career SettingsPanelPresentation. UTF-8 safe."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:280]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    subprocess.check_call(
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "battle_settings_panel"]
    )
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text and "叫暫停" not in text:
        raise RuntimeError("refusing: file looks corrupted")

    text = must_replace(
        text,
        """        private GameObject _settingsRoot;
        private Text _settingsResolutionLabel;
        private int _resolutionIndex = 2;
        private bool _pausedBeforeSettings;""",
        """        private SettingsPanelPresentation _settingsPanel;
        private bool _pausedBeforeSettings;""",
    )

    text = must_replace(
        text,
        """        private static readonly Color Danger = new Color(0.92f, 0.42f, 0.38f, 1f);
        private static readonly Vector2Int[] DisplayResolutions =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440)
        };
        private static readonly Color NaviColor = new Color(0.95f, 0.78f, 0.16f, 1f);""",
        """        private static readonly Color Danger = new Color(0.92f, 0.42f, 0.38f, 1f);
        private static readonly Color NaviColor = new Color(0.95f, 0.78f, 0.16f, 1f);""",
    )

    text = must_replace(
        text,
        """        private void Awake()
        {
            _uiFont = LoadProjectFont("NotoSansTC-Bold.ttf") ?? ResolveOsFallbackFont();
            _uiFont700 = LoadProjectFont("700.otf") ?? _uiFont;
            _uiFontBlack = LoadProjectFont("NotoSansTC-Black.ttf") ?? _uiFont;
            _playerIdFont = LoadProjectFont("Anuschka-Demo.ttf") ?? _uiFont;
            _content = RuntimeContentLoader.Load();""",
        """        private void Awake()
        {
            GameSettingsRuntime.LoadAndApply();
            _uiFont = LoadProjectFont("NotoSansTC-Bold.ttf") ?? ResolveOsFallbackFont();
            _uiFont700 = LoadProjectFont("700.otf") ?? _uiFont;
            _uiFontBlack = LoadProjectFont("NotoSansTC-Black.ttf") ?? _uiFont;
            _playerIdFont = LoadProjectFont("Anuschka-Demo.ttf") ?? _uiFont;
            _content = RuntimeContentLoader.Load();""",
    )

    # Insert OnApplicationFocus before BuildInterface or after Awake block — put after DeferredInitialMapLayout.
    if "OnApplicationFocus" not in text:
        text = must_replace(
            text,
            """        private void ApplyMatchModeUi()
        {
            bool simulation = !_careerOfficialMatch;""",
            """        private void OnApplicationFocus(bool hasFocus) =>
            GameSettingsRuntime.HandleFocus(hasFocus);

        private void ApplyMatchModeUi()
        {
            bool simulation = !_careerOfficialMatch;""",
        )

    text = must_replace(
        text,
        """        private void BuildSettingsPanel(Transform canvas)
        {
            Image dim = CreateUiObject<Image>("SettingsOverlay", canvas);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = true;
            _settingsRoot = dim.gameObject;
            _settingsRoot.SetActive(false);

            Button closeDim = dim.gameObject.AddComponent<Button>();
            closeDim.targetGraphic = dim;
            closeDim.transition = Selectable.Transition.None;
            closeDim.onClick.AddListener(HideSettingsPanel);

            Image panel = CreateUiObject<Image>("SettingsPanel", dim.transform);
            SetRect(panel.rectTransform, new Vector2(0.36f, 0.28f), new Vector2(0.64f, 0.72f));
            panel.color = Panel;
            panel.raycastTarget = true;
            Button panelBlocker = panel.gameObject.AddComponent<Button>();
            panelBlocker.targetGraphic = panel;
            panelBlocker.transition = Selectable.Transition.None;

            Text title = CreateText("Title", panel.transform, 22, TextAnchor.MiddleCenter, Accent);
            SetRect(title.rectTransform, new Vector2(0.06f, 0.82f), new Vector2(0.70f, 0.96f));
            title.text = "設定";

            CreateButton("Close", "關閉", panel.transform,
                new Vector2(0.72f, 0.84f), new Vector2(0.94f, 0.96f), HideSettingsPanel);

            CreateButton("Resolution", "切換解析度", panel.transform,
                new Vector2(0.12f, 0.58f), new Vector2(0.88f, 0.74f), CycleResolution);
            _settingsResolutionLabel = CreateText("ResolutionLabel", panel.transform, 14, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_settingsResolutionLabel.rectTransform, new Vector2(0.12f, 0.46f), new Vector2(0.88f, 0.58f));
            RefreshResolutionLabel();

            CreateButton("Fullscreen", "全螢幕切換", panel.transform,
                new Vector2(0.12f, 0.30f), new Vector2(0.88f, 0.44f), ToggleFullscreen);

            CreateButton("Quit", "退出", panel.transform,
                new Vector2(0.12f, 0.10f), new Vector2(0.88f, 0.24f), QuitGame);

            Image quitFace = panel.transform.Find("Quit/Face")?.GetComponent<Image>();
            if (quitFace != null)
                quitFace.color = new Color(0.35f, 0.14f, 0.12f, 1f);
            Text quitText = panel.transform.Find("Quit/Face/Text")?.GetComponent<Text>();
            if (quitText != null)
                quitText.color = Danger;
        }

        private void ToggleSettingsPanel()
        {
            if (_settingsRoot == null) return;
            if (_settingsRoot.activeSelf)
                HideSettingsPanel();
            else
                ShowSettingsPanel();
        }

        private void ShowSettingsPanel()
        {
            if (_settingsRoot == null) return;
            _pausedBeforeSettings = _paused;
            if (!_paused)
                TogglePause();
            _settingsRoot.SetActive(true);
            _settingsRoot.transform.SetAsLastSibling();
            RefreshResolutionLabel();
        }

        private void HideSettingsPanel()
        {
            if (_settingsRoot != null)
                _settingsRoot.SetActive(false);
            if (!_pausedBeforeSettings && _paused)
                TogglePause();
        }

        private void Update()
        {
            PruneExpiredKillFeed();

            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (_settingsRoot != null && _settingsRoot.activeSelf)
            {
                HideSettingsPanel();
                return;
            }

            if (_profilePopup != null && _profilePopup.gameObject.activeSelf)
            {
                HideProfilePopup();
                CancelProfileHover();
            }
        }

        private void CycleResolution()
        {
            _resolutionIndex = (_resolutionIndex + 1) % DisplayResolutions.Length;
            Vector2Int size = DisplayResolutions[_resolutionIndex];
            Screen.SetResolution(size.x, size.y, Screen.fullScreenMode);
            RefreshResolutionLabel();
        }

        private void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            RefreshResolutionLabel();
        }

        private void RefreshResolutionLabel()
        {
            if (_settingsResolutionLabel == null) return;
            Vector2Int size = DisplayResolutions[_resolutionIndex];
            string mode = Screen.fullScreen ? "全螢幕" : "視窗";
            _settingsResolutionLabel.text = $"目前：{size.x}×{size.y} · {mode}";
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void CreateRoundWinBanner(Transform canvas)""",
        """        private void BuildSettingsPanel(Transform canvas)
        {
            var host = new SettingsPanelHost
            {
                CareerSaveLoadEnabled = false,
                OnReturnToMainMenu = ReturnToCareerFromSettings,
                OnClose = OnSettingsClosed
            };
            _settingsPanel = new SettingsPanelPresentation(_uiFont700 ?? _uiFont, host);
            _settingsPanel.Build(canvas);
        }

        private void ToggleSettingsPanel()
        {
            if (_settingsPanel == null) return;
            if (_settingsPanel.IsVisible)
                HideSettingsPanel();
            else
                ShowSettingsPanel();
        }

        private void ShowSettingsPanel()
        {
            if (_settingsPanel == null) return;
            _pausedBeforeSettings = _paused;
            if (!_paused)
                TogglePause();
            _settingsPanel.Show();
        }

        private void OnSettingsClosed()
        {
            if (!_pausedBeforeSettings && _paused)
                TogglePause();
        }

        private void HideSettingsPanel()
        {
            if (_settingsPanel == null) return;
            bool wasVisible = _settingsPanel.IsVisible;
            _settingsPanel.Hide();
            if (wasVisible)
                OnSettingsClosed();
        }

        private void ReturnToCareerFromSettings()
        {
            // Abandon in-progress battle without writing an official result.
            OnSettingsClosed();
            StopAllCoroutines();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Demo_Career");
        }

        private void Update()
        {
            PruneExpiredKillFeed();

            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (_settingsPanel != null && _settingsPanel.IsVisible)
            {
                HideSettingsPanel();
                return;
            }

            if (_profilePopup != null && _profilePopup.gameObject.activeSelf)
            {
                HideProfilePopup();
                CancelProfileHover();
            }
        }

        private void CreateRoundWinBanner(Transform canvas)""",
    )

    TARGET.write_text(text, encoding="utf-8")
    assert "SettingsPanelPresentation" in text
    assert "_settingsRoot" not in text
    assert "CycleResolution" not in text
    assert "擊殺廣播" in text or "叫暫停" in text
    print("patched", TARGET)


if __name__ == "__main__":
    main()
