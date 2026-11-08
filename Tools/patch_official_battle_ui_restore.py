# UTF-8-safe: restore official battle UI (meta under clock, no bottom 正式賽 dup, skip→result).
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing anchor:\n{old[:180]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted before patch")

    # --- fields ---
    if "_careerMatchMetaText" not in text:
        text = must_replace(
            text,
            "        private Text _controlBarTitle;\n"
            "        private Text _careerBarInfoText;\n"
            "        private GameObject _settingsRoot;",
            "        private Text _controlBarTitle;\n"
            "        private Text _careerBarInfoText;\n"
            "        private Text _careerMatchMetaText;\n"
            "        private RectTransform _controlBarRect;\n"
            "        private RectTransform _settingsButtonRect;\n"
            "        private RectTransform _timeoutButtonRect;\n"
            "        private RectTransform _pauseButtonRect;\n"
            "        private RectTransform _speedButtonRect;\n"
            "        private RectTransform _skipButtonRect;\n"
            "        private GameObject _settingsRoot;",
        )

    # --- ApplyMatchModeUi + meta helpers ---
    old_mode = """        private void ApplyMatchModeUi()
        {
            bool simulation = !_careerOfficialMatch;
            if (_naviSideButtonRoot != null)
                _naviSideButtonRoot.SetActive(simulation);
            if (_gambitSideButtonRoot != null)
                _gambitSideButtonRoot.SetActive(simulation);
            if (_newSeedButtonRoot != null)
                _newSeedButtonRoot.SetActive(simulation);
            if (_controlBarTitle != null)
                _controlBarTitle.gameObject.SetActive(simulation);
            if (_sideLabel != null)
                _sideLabel.gameObject.SetActive(simulation);
            if (_careerBarInfoText != null)
            {
                _careerBarInfoText.gameObject.SetActive(!simulation);
                if (!simulation)
                    RefreshOfficialControlBarInfo();
            }
        }

        private void RefreshOfficialControlBarInfo()
        {
            if (_careerBarInfoText == null)
                return;
            string stage = string.IsNullOrEmpty(_careerBracketStageLabel)
                ? string.Empty
                : $" · {_careerBracketStageLabel}";
            string competition = string.IsNullOrEmpty(_careerCompetitionTitle)
                ? "正式賽"
                : _careerCompetitionTitle;
            string mapName = string.IsNullOrEmpty(_careerMapDisplayName) ? "Dust II" : _careerMapDisplayName;
            _careerBarInfoText.text = $"正式賽 · {mapName} · {competition}{stage}";
        }"""

    new_mode = """        private void ApplyMatchModeUi()
        {
            bool simulation = !_careerOfficialMatch;
            if (_naviSideButtonRoot != null)
                _naviSideButtonRoot.SetActive(simulation);
            if (_gambitSideButtonRoot != null)
                _gambitSideButtonRoot.SetActive(simulation);
            if (_newSeedButtonRoot != null)
                _newSeedButtonRoot.SetActive(simulation);
            if (_controlBarTitle != null)
                _controlBarTitle.gameObject.SetActive(simulation);
            if (_sideLabel != null)
                _sideLabel.gameObject.SetActive(simulation);
            if (_matchText != null)
                _matchText.gameObject.SetActive(simulation);
            // Bottom career info was duplicating map/competition/stage — keep hidden.
            if (_careerBarInfoText != null)
                _careerBarInfoText.gameObject.SetActive(false);
            if (_careerMatchMetaText != null)
            {
                _careerMatchMetaText.gameObject.SetActive(!simulation);
                if (!simulation)
                    RefreshOfficialMatchMeta();
            }

            ApplyControlBarLayout(simulation);
        }

        private void RefreshOfficialMatchMeta()
        {
            if (_careerMatchMetaText == null)
                return;
            string mapName = string.IsNullOrEmpty(_careerMapDisplayName) ? "Dust II" : _careerMapDisplayName;
            string competition = _careerCompetitionTitle ?? string.Empty;
            string stage = _careerBracketStageLabel ?? string.Empty;
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(mapName))
                parts.Add(mapName);
            if (!string.IsNullOrEmpty(competition))
                parts.Add(competition);
            if (!string.IsNullOrEmpty(stage))
                parts.Add(stage);
            _careerMatchMetaText.text = parts.Count > 0 ? string.Join(" · ", parts) : string.Empty;
        }

        private void ApplyControlBarLayout(bool practiceLayout)
        {
            if (_controlBarRect != null)
            {
                if (practiceLayout)
                    SetRect(_controlBarRect, new Vector2(0.12f, 0.015f), new Vector2(0.88f, 0.100f));
                else
                    SetRect(_controlBarRect, new Vector2(0.392f, 0.028f), new Vector2(0.608f, 0.054f));
            }

            if (_settingsButtonRect != null)
            {
                if (practiceLayout)
                    SetRect(_settingsButtonRect, new Vector2(0.905f, 0.015f), new Vector2(0.985f, 0.100f));
                else
                    SetRect(_settingsButtonRect, new Vector2(0.612f, 0.028f), new Vector2(0.642f, 0.054f));
            }

            if (practiceLayout)
            {
                if (_timeoutButtonRect != null)
                    SetRect(_timeoutButtonRect, new Vector2(0.50f, 0.15f), new Vector2(0.64f, 0.85f));
                if (_pauseButtonRect != null)
                    SetRect(_pauseButtonRect, new Vector2(0.65f, 0.15f), new Vector2(0.715f, 0.85f));
                if (_speedButtonRect != null)
                    SetRect(_speedButtonRect, new Vector2(0.72f, 0.15f), new Vector2(0.785f, 0.85f));
                if (_skipButtonRect != null)
                    SetRect(_skipButtonRect, new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f));
                if (_matchText != null)
                    SetRect(_matchText.rectTransform, new Vector2(0.43f, 0.15f), new Vector2(0.495f, 0.85f));
                return;
            }

            if (_timeoutButtonRect != null)
                SetRect(_timeoutButtonRect, new Vector2(0.04f, 0.20f), new Vector2(0.26f, 0.80f));
            if (_pauseButtonRect != null)
                SetRect(_pauseButtonRect, new Vector2(0.28f, 0.20f), new Vector2(0.44f, 0.80f));
            if (_speedButtonRect != null)
                SetRect(_speedButtonRect, new Vector2(0.46f, 0.20f), new Vector2(0.62f, 0.80f));
            if (_skipButtonRect != null)
                SetRect(_skipButtonRect, new Vector2(0.64f, 0.20f), new Vector2(0.80f, 0.80f));
        }"""

    if "RefreshOfficialMatchMeta" not in text:
        text = must_replace(text, old_mode, new_mode)
    elif "正式賽 · {mapName}" in text:
        # Partial restore: still has old RefreshOfficialControlBarInfo
        text = must_replace(text, old_mode, new_mode)

    # --- match label: no 正式賽 ---
    text = must_replace(
        text,
        """            if (_matchText != null)
            {
                _matchText.text = _careerOfficialMatch && !string.IsNullOrEmpty(_careerCompetitionTitle)
                    ? $"正式賽 · {_careerCompetitionTitle}"
                    : $"第 {_matchNumber} 場";
            }""",
        """            if (_matchText != null)
                _matchText.text = $"第 {_matchNumber} 場";""",
    )

    # --- Skip: finish properly + show result overlay ---
    old_skip = """        private void Skip()
        {
            StopAllMovement();
            if (_playback != null)
            {
                StopCoroutine(_playback);
                _playback = null;
            }
            _awaitingCard = false;
            _awaitingTacticalCards = false;
            _awaitingTimeout = false;
            _awaitingFocus = false;
            _selectedCard = null;
            HideCardPicker();
            HideTacticalCardPicker();
            HideTimeoutPicker();
            HideFocusPicker();
            HideRoundWinBanner();

            if (_session != null && !_session.IsFinished)
                _session.AutoPlayRemaining(myTeamId);
            _result = _session?.Result;

            _naviScore = 0;
            _gambitScore = 0;
            _roundHasDeath = false;
            _killFeed.Clear();
            _traitFeed.Clear();
            ResetEconomyDisplay();
            ResetStats();
            if (_result != null)
            {
                foreach (SimulationEvent evt in _result.events.OrderBy(x => x.roundNumber).ThenBy(x => x.roundTime).ThenBy(x => x.sequence))
                    ApplyEvent(evt, false);
            }
            UpdateKillFeed();
            UpdateTraitFeed();
            UpdateAliveCountHud();
            UpdateEconomyHud();
        }"""

    new_skip = """        private void Skip()
        {
            StopAllMovement();
            if (_playback != null)
            {
                StopCoroutine(_playback);
                _playback = null;
            }
            _awaitingCard = false;
            _awaitingTacticalCards = false;
            _awaitingTimeout = false;
            _awaitingFocus = false;
            _selectedCard = null;
            HideCardPicker();
            HideTacticalCardPicker();
            HideTimeoutPicker();
            HideFocusPicker();
            HideRoundWinBanner();

            if (_session != null && !_session.IsFinished)
            {
                if (_session.IsComplete)
                    _session.FinishMatch();
                else
                    _session.AutoPlayRemaining(myTeamId);
            }
            _result = _session?.Result;

            _naviScore = 0;
            _gambitScore = 0;
            _roundHasDeath = false;
            _killFeed.Clear();
            _traitFeed.Clear();
            ResetEconomyDisplay();
            ResetStats();
            if (_result != null)
            {
                foreach (SimulationEvent evt in _result.events.OrderBy(x => x.roundNumber).ThenBy(x => x.roundTime).ThenBy(x => x.sequence))
                    ApplyEvent(evt, false);
            }
            UpdateKillFeed();
            UpdateTraitFeed();
            UpdateAliveCountHud();
            UpdateEconomyHud();

            // MatchLoop was stopped; official career must still confirm result to resume.
            if (_careerOfficialMatch && _result != null && !string.IsNullOrEmpty(_result.winnerTeamId))
                StartCoroutine(ShowOfficialResultFlow());
        }"""

    if "StartCoroutine(ShowOfficialResultFlow())" not in text.split("private void TogglePause()")[0]:
        text = must_replace(text, old_skip, new_skip)

    # --- TopHud meta under clock ---
    if "_careerMatchMetaText = CreateText" not in text:
        text = must_replace(
            text,
            """            _clockText = CreateText("Clock", topHud.transform, 16, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_clockText.rectTransform, new Vector2(0.37f, 0.30f), new Vector2(0.63f, 0.58f));
            _clockText.text = "1:55";

            _rightScoreText = CreateText("RightScore", topHud.transform, 42, TextAnchor.MiddleLeft, TextPrimary);""",
            """            _clockText = CreateText("Clock", topHud.transform, 16, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_clockText.rectTransform, new Vector2(0.37f, 0.36f), new Vector2(0.63f, 0.58f));
            _clockText.text = "1:55";

            _careerMatchMetaText = CreateText("CareerMeta", topHud.transform, 11, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_careerMatchMetaText.rectTransform, new Vector2(0.28f, 0.06f), new Vector2(0.72f, 0.30f));
            _careerMatchMetaText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _careerMatchMetaText.gameObject.SetActive(false);

            _rightScoreText = CreateText("RightScore", topHud.transform, 42, TextAnchor.MiddleLeft, TextPrimary);""",
        )

    # --- Wire control bar rects ---
    if "_controlBarRect = controlBar.rectTransform" not in text:
        text = must_replace(
            text,
            """            Image controlBar = CreateUiObject<Image>("Controls", canvas.transform);
            SetRect(controlBar.rectTransform, new Vector2(0.12f, 0.015f), new Vector2(0.88f, 0.100f));
            controlBar.color = new Color(Panel.r, Panel.g, Panel.b, 0.78f);

            Text title = CreateText("Title", controlBar.transform, 14, TextAnchor.MiddleLeft, TextMuted);""",
            """            Image controlBar = CreateUiObject<Image>("Controls", canvas.transform);
            SetRect(controlBar.rectTransform, new Vector2(0.12f, 0.015f), new Vector2(0.88f, 0.100f));
            controlBar.color = new Color(Panel.r, Panel.g, Panel.b, 0.78f);
            _controlBarRect = controlBar.rectTransform;

            Text title = CreateText("Title", controlBar.transform, 14, TextAnchor.MiddleLeft, TextMuted);""",
        )

    if "_timeoutButtonRect = callTimeout" not in text:
        text = must_replace(
            text,
            """            Button callTimeout = CreateButton("CallTimeout", "叫暫停", controlBar.transform,
                new Vector2(0.50f, 0.15f), new Vector2(0.64f, 0.85f), OnRequestTimeoutClicked);
            _timeoutCallLabel = callTimeout.GetComponentInChildren<Text>();""",
            """            Button callTimeout = CreateButton("CallTimeout", "叫暫停", controlBar.transform,
                new Vector2(0.50f, 0.15f), new Vector2(0.64f, 0.85f), OnRequestTimeoutClicked);
            _timeoutButtonRect = callTimeout.GetComponent<RectTransform>();
            _timeoutCallLabel = callTimeout.GetComponentInChildren<Text>();""",
        )

    if "_pauseButtonRect = pauseBtn" not in text:
        text = must_replace(
            text,
            """            CreateIconButton("Pause", controlBar.transform,
                new Vector2(0.65f, 0.15f), new Vector2(0.715f, 0.85f),
                _playbackPauseSprite, TogglePause, out _pauseIcon);
            RefreshPauseIcon();

            Button speed = CreateButton("Speed", "16×", controlBar.transform,
                new Vector2(0.72f, 0.15f), new Vector2(0.785f, 0.85f), CycleSpeed);
            _speedText = speed.GetComponentInChildren<Text>();

            CreateIconButton("Skip", controlBar.transform,
                new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f),
                _playbackSkipSprite, Skip, out _);""",
            """            Button pauseBtn = CreateIconButton("Pause", controlBar.transform,
                new Vector2(0.65f, 0.15f), new Vector2(0.715f, 0.85f),
                _playbackPauseSprite, TogglePause, out _pauseIcon);
            _pauseButtonRect = pauseBtn.GetComponent<RectTransform>();
            RefreshPauseIcon();

            Button speed = CreateButton("Speed", "16×", controlBar.transform,
                new Vector2(0.72f, 0.15f), new Vector2(0.785f, 0.85f), CycleSpeed);
            _speedButtonRect = speed.GetComponent<RectTransform>();
            _speedText = speed.GetComponentInChildren<Text>();

            Button skipBtn = CreateIconButton("Skip", controlBar.transform,
                new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f),
                _playbackSkipSprite, Skip, out _);
            _skipButtonRect = skipBtn.GetComponent<RectTransform>();""",
        )

    if "_settingsButtonRect = settingsBtn" not in text:
        text = must_replace(
            text,
            """            Sprite settingsSprite = Resources.Load<Sprite>("UI/Icons/settings");
            CreateIconButton(
                "Settings",
                canvas.transform,
                new Vector2(0.905f, 0.015f),
                new Vector2(0.985f, 0.100f),
                settingsSprite,
                ToggleSettingsPanel,
                out _);""",
            """            Sprite settingsSprite = Resources.Load<Sprite>("UI/Icons/settings");
            Button settingsBtn = CreateIconButton(
                "Settings",
                canvas.transform,
                new Vector2(0.905f, 0.015f),
                new Vector2(0.985f, 0.100f),
                settingsSprite,
                ToggleSettingsPanel,
                out _);
            _settingsButtonRect = settingsBtn.GetComponent<RectTransform>();""",
        )

    # --- opponent team id (not hard-coded NAVI/Gambit flip) ---
    old_right = 'string rightId = myTeamId == "navi_2021" ? "gambit_2021" : "navi_2021";'
    while old_right in text:
        text = text.replace(old_right, "string rightId = _opponentTeamId;", 1)

    text = must_replace(
        text,
        """        private void UpdateScore()
        {
            if (_leftScoreText == null || _rightScoreText == null)
                return;

            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            int leftScore = leftId == "navi_2021" ? _naviScore : _gambitScore;
            int rightScore = rightId == "navi_2021" ? _naviScore : _gambitScore;""",
        """        private void UpdateScore()
        {
            if (_leftScoreText == null || _rightScoreText == null)
                return;

            string teamAId = _session?.State.TeamA.Definition.id ?? myTeamId;
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            int teamAScore = _naviScore;
            int teamBScore = _gambitScore;
            int leftScore = string.Equals(leftId, teamAId, System.StringComparison.OrdinalIgnoreCase)
                ? teamAScore : teamBScore;
            int rightScore = string.Equals(rightId, teamAId, System.StringComparison.OrdinalIgnoreCase)
                ? teamAScore : teamBScore;""",
    )

    text = must_replace(
        text,
        """            bool naviIsT = MatchRules.TeamAIsTerrorist(Mathf.Max(0, _currentRound - 1));
            bool leftIsNavi = myTeamId == "navi_2021";
            bool leftIsT = leftIsNavi ? naviIsT : !naviIsT;
            _leftStatsPanel.color = leftIsT ? TPanel : CtPanel;
            _rightStatsPanel.color = leftIsT ? CtPanel : TPanel;""",
        """            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            _leftStatsPanel.color = leftIsT ? TPanel : CtPanel;
            _rightStatsPanel.color = rightIsT ? TPanel : CtPanel;""",
    )

    text = must_replace(
        text,
        """            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            if (_leftAliveBadge != null)
                _leftAliveBadge.color = leftIsT ? TAliveBg : CtAliveBg;
            if (_rightAliveBadge != null)
                _rightAliveBadge.color = leftIsT ? CtAliveBg : TAliveBg;""",
        """            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            if (_leftAliveBadge != null)
                _leftAliveBadge.color = leftIsT ? TAliveBg : CtAliveBg;
            if (_rightAliveBadge != null)
                _rightAliveBadge.color = rightIsT ? TAliveBg : CtAliveBg;""",
    )

    # Keep career bar object but never show (avoid leftover text if ApplyMatchModeUi missed)
    if "_careerBarInfoText.gameObject.SetActive(false);" not in text.split("ApplyMatchModeUi")[1][:800]:
        pass  # already in new ApplyMatchModeUi

    for needle in (
        "RefreshOfficialMatchMeta",
        "ApplyControlBarLayout",
        "StartCoroutine(ShowOfficialResultFlow())",
        "_careerMatchMetaText = CreateText",
        "擊殺廣播",
    ):
        if needle not in text:
            raise RuntimeError(f"patch incomplete: missing {needle}")

    if "正式賽 · {mapName}" in text or '正式賽 · {_careerCompetitionTitle}' in text:
        raise RuntimeError("still has 正式賽 duplication strings")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
