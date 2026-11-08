# UTF-8-safe: center 4 official buttons + rich result overlay with logos
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"

OLD_FLOW = '''        private IEnumerator ShowOfficialResultFlow()
        {
            EnsureOfficialResultOverlay();
            if (_officialResultBody != null)
                _officialResultBody.text = FormatOfficialResultSummary();
            if (_officialResultRoot != null)
                _officialResultRoot.SetActive(true);
            _awaitingOfficialResultConfirm = true;
            while (_awaitingOfficialResultConfirm)
                yield return null;
        }

        private string FormatOfficialResultSummary()
        {
            if (_result == null || _session == null)
                return string.Empty;
            string teamAId = _session.State.TeamA.Definition.id;
            bool myTeamA = string.Equals(myTeamId, teamAId, StringComparison.OrdinalIgnoreCase);
            int myScore = myTeamA ? _result.teamAScore : _result.teamBScore;
            int oppScore = myTeamA ? _result.teamBScore : _result.teamAScore;
            string myName = DemoZh.TeamName(myTeamId);
            string oppName = DemoZh.TeamName(_opponentTeamId);
            bool won = string.Equals(_result.winnerTeamId, myTeamId, StringComparison.OrdinalIgnoreCase);
            string outcome = won ? "勝利" : "落敗";
            return $"{myName}  {myScore} : {oppScore}  {oppName}\\n\\n{outcome}";
        }

        private void EnsureOfficialResultOverlay()
        {
            if (_officialResultRoot != null || _canvasRect == null)
                return;
            Image dim = CreateUiObject<Image>("OfficialResultDim", _canvasRect);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.62f);
            dim.raycastTarget = true;
            _officialResultRoot = dim.gameObject;

            Image panel = CreateUiObject<Image>("OfficialResultPanel", dim.transform);
            SetRect(panel.rectTransform, new Vector2(0.36f, 0.40f), new Vector2(0.64f, 0.62f));
            panel.color = new Color(Panel.r, Panel.g, Panel.b, 0.94f);

            Text title = CreateText("Title", panel.transform, 18, TextAnchor.MiddleCenter, Accent, _uiFont700);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.94f));
            title.text = "比賽結束";
            title.fontStyle = FontStyle.Bold;

            _officialResultBody = CreateText("Body", panel.transform, 16, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_officialResultBody.rectTransform, new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.72f));
            _officialResultBody.lineSpacing = 1.15f;

            CreateButton("ConfirmOfficialResult", "確認", panel.transform,
                new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.26f), OnConfirmOfficialResult);
            _officialResultRoot.SetActive(false);
        }

        private void OnConfirmOfficialResult()
        {
            _awaitingOfficialResultConfirm = false;
            if (_officialResultRoot != null)
                _officialResultRoot.SetActive(false);
            if (_result != null)
                CareerMatchLaunchContext.SetCompletedResult(_result);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Demo_Career");
        }'''

NEW_FLOW = '''        private IEnumerator ShowOfficialResultFlow()
        {
            EnsureOfficialResultOverlay();
            RefreshOfficialResultPresentation();
            if (_officialResultRoot != null)
                _officialResultRoot.SetActive(true);
            _awaitingOfficialResultConfirm = true;
            while (_awaitingOfficialResultConfirm)
                yield return null;
        }

        private void RefreshOfficialResultPresentation()
        {
            if (_result == null || _session == null)
                return;
            string teamAId = _session.State.TeamA.Definition.id;
            bool myTeamA = string.Equals(myTeamId, teamAId, StringComparison.OrdinalIgnoreCase);
            int myScore = myTeamA ? _result.teamAScore : _result.teamBScore;
            int oppScore = myTeamA ? _result.teamBScore : _result.teamAScore;
            bool won = string.Equals(_result.winnerTeamId, myTeamId, StringComparison.OrdinalIgnoreCase);

            BindTeamLogo(_officialResultLeftLogo, myTeamId);
            BindTeamLogo(_officialResultRightLogo, _opponentTeamId);
            if (_officialResultLeftName != null)
                _officialResultLeftName.text = DemoZh.TeamName(myTeamId);
            if (_officialResultRightName != null)
                _officialResultRightName.text = DemoZh.TeamName(_opponentTeamId);
            if (_officialResultLeftScore != null)
                _officialResultLeftScore.text = myScore.ToString("00");
            if (_officialResultRightScore != null)
                _officialResultRightScore.text = oppScore.ToString("00");
            if (_officialResultOutcome != null)
            {
                _officialResultOutcome.text = won ? "勝利" : "戰敗";
                _officialResultOutcome.color = won ? OfficialWinUi : OfficialLossUi;
            }
        }

        private void EnsureOfficialResultOverlay()
        {
            if (_canvasRect == null)
                return;
            if (_officialResultRoot != null && _officialResultLeftLogo == null)
            {
                Destroy(_officialResultRoot);
                _officialResultRoot = null;
            }
            if (_officialResultRoot != null)
                return;

            Image dim = CreateUiObject<Image>("OfficialResultDim", _canvasRect);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.62f);
            dim.raycastTarget = true;
            _officialResultRoot = dim.gameObject;

            Image panel = CreateUiObject<Image>("OfficialResultPanel", dim.transform);
            SetRect(panel.rectTransform, new Vector2(0.30f, 0.34f), new Vector2(0.70f, 0.66f));
            panel.color = new Color(Panel.r, Panel.g, Panel.b, 0.94f);

            Text title = CreateText("Title", panel.transform, 18, TextAnchor.MiddleCenter, Accent, _uiFont700);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.96f));
            title.text = "比賽結束";
            title.fontStyle = FontStyle.Bold;

            _officialResultLeftLogo = CreateResultTeamLogo("LeftLogo", panel.transform, 0.16f);
            _officialResultRightLogo = CreateResultTeamLogo("RightLogo", panel.transform, 0.84f);

            _officialResultLeftName = CreateText("LeftName", panel.transform, 12, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_officialResultLeftName.rectTransform, new Vector2(0.06f, 0.38f), new Vector2(0.26f, 0.48f));

            _officialResultRightName = CreateText("RightName", panel.transform, 12, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_officialResultRightName.rectTransform, new Vector2(0.74f, 0.38f), new Vector2(0.94f, 0.48f));

            _officialResultLeftScore = CreateText("LeftScore", panel.transform, 36, TextAnchor.MiddleRight, TextPrimary, _uiFont700);
            SetRect(_officialResultLeftScore.rectTransform, new Vector2(0.26f, 0.50f), new Vector2(0.42f, 0.74f));
            _officialResultLeftScore.fontStyle = FontStyle.Bold;

            Text colon = CreateText("Colon", panel.transform, 28, TextAnchor.MiddleCenter, TextMuted);
            SetRect(colon.rectTransform, new Vector2(0.44f, 0.50f), new Vector2(0.56f, 0.74f));
            colon.text = ":";
            colon.fontStyle = FontStyle.Bold;

            _officialResultRightScore = CreateText("RightScore", panel.transform, 36, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);
            SetRect(_officialResultRightScore.rectTransform, new Vector2(0.58f, 0.50f), new Vector2(0.74f, 0.74f));
            _officialResultRightScore.fontStyle = FontStyle.Bold;

            _officialResultOutcome = CreateText("Outcome", panel.transform, 22, TextAnchor.MiddleCenter, OfficialWinUi, _uiFont700);
            SetRect(_officialResultOutcome.rectTransform, new Vector2(0.28f, 0.28f), new Vector2(0.72f, 0.42f));
            _officialResultOutcome.fontStyle = FontStyle.Bold;

            CreateButton("ConfirmOfficialResult", "確認", panel.transform,
                new Vector2(0.30f, 0.06f), new Vector2(0.70f, 0.20f), OnConfirmOfficialResult);
            _officialResultRoot.SetActive(false);
        }

        private Image CreateResultTeamLogo(string name, Transform parent, float anchorX)
        {
            Image logo = CreateUiObject<Image>(name, parent);
            RectTransform rt = logo.rectTransform;
            // Same pattern as TopHud logos: fixed X, height-driven square beside the score.
            rt.anchorMin = new Vector2(anchorX, 0.48f);
            rt.anchorMax = new Vector2(anchorX, 0.78f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            var fitter = logo.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fitter.aspectRatio = 1f;
            logo.preserveAspect = false;
            logo.raycastTarget = false;
            logo.color = Color.white;
            return logo;
        }

        private void OnConfirmOfficialResult()
        {
            _awaitingOfficialResultConfirm = false;
            if (_officialResultRoot != null)
                _officialResultRoot.SetActive(false);
            if (_result != null)
                CareerMatchLaunchContext.SetCompletedResult(_result);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Demo_Career");
        }'''


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")

    if OLD_FLOW not in text and "_officialResultLeftLogo" in text:
        print("result overlay already upgraded")
    elif OLD_FLOW in text:
        text = text.replace(OLD_FLOW, NEW_FLOW, 1)
    else:
        raise RuntimeError("ShowOfficialResultFlow block not found")

    text = text.replace(
        "        private GameObject _officialResultRoot;\n        private Text _officialResultBody;\n        private bool _awaitingOfficialResultConfirm;",
        "        private GameObject _officialResultRoot;\n"
        "        private Image _officialResultLeftLogo;\n"
        "        private Image _officialResultRightLogo;\n"
        "        private Text _officialResultLeftName;\n"
        "        private Text _officialResultRightName;\n"
        "        private Text _officialResultLeftScore;\n"
        "        private Text _officialResultRightScore;\n"
        "        private Text _officialResultOutcome;\n"
        "        private bool _awaitingOfficialResultConfirm;",
        1,
    )

    text = text.replace(
        "                else\n                    SetRect(_controlBarRect, new Vector2(0.392f, 0.028f), new Vector2(0.608f, 0.054f));",
        "                else\n                    SetRect(_controlBarRect, new Vector2(0.372f, 0.028f), new Vector2(0.628f, 0.054f));",
        1,
    )
    text = text.replace(
        "                else\n                    SetRect(_settingsButtonRect, new Vector2(0.612f, 0.028f), new Vector2(0.642f, 0.054f));",
        "                else\n                    SetRect(_settingsButtonRect, new Vector2(0.634f, 0.028f), new Vector2(0.664f, 0.054f));",
        1,
    )
    text = text.replace(
        "            if (_timeoutButtonRect != null)\n"
        "                SetRect(_timeoutButtonRect, new Vector2(0.04f, 0.20f), new Vector2(0.26f, 0.80f));\n"
        "            if (_pauseButtonRect != null)\n"
        "                SetRect(_pauseButtonRect, new Vector2(0.28f, 0.20f), new Vector2(0.44f, 0.80f));\n"
        "            if (_speedButtonRect != null)\n"
        "                SetRect(_speedButtonRect, new Vector2(0.46f, 0.20f), new Vector2(0.62f, 0.80f));\n"
        "            if (_skipButtonRect != null)\n"
        "                SetRect(_skipButtonRect, new Vector2(0.64f, 0.20f), new Vector2(0.80f, 0.80f));",
        "            if (_timeoutButtonRect != null)\n"
        "                SetRect(_timeoutButtonRect, new Vector2(0.06f, 0.20f), new Vector2(0.27f, 0.80f));\n"
        "            if (_pauseButtonRect != null)\n"
        "                SetRect(_pauseButtonRect, new Vector2(0.29f, 0.20f), new Vector2(0.44f, 0.80f));\n"
        "            if (_speedButtonRect != null)\n"
        "                SetRect(_speedButtonRect, new Vector2(0.46f, 0.20f), new Vector2(0.61f, 0.80f));\n"
        "            if (_skipButtonRect != null)\n"
        "                SetRect(_skipButtonRect, new Vector2(0.63f, 0.20f), new Vector2(0.78f, 0.80f));",
        1,
    )

    old_bind = '''        private void BindTeamLogo(Image target, string teamId)
        {
            if (target == null)
                return;
            Sprite sprite = teamId == "navi_2021"
                ? _naviLogoSprite
                : teamId == "gambit_2021"
                    ? _gambitLogoSprite
                    : null;
            target.sprite = sprite;
            target.enabled = sprite != null;
            target.color = Color.white;
        }'''

    new_bind = '''        private void BindTeamLogo(Image target, string teamId)
        {
            if (target == null)
                return;
            Sprite sprite = ResolveTeamLogoSprite(teamId);
            target.sprite = sprite;
            target.enabled = sprite != null;
            target.color = Color.white;
        }

        private Sprite ResolveTeamLogoSprite(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
                return null;
            if (string.Equals(teamId, "navi_2021", StringComparison.OrdinalIgnoreCase))
                return _naviLogoSprite;
            if (string.Equals(teamId, "gambit_2021", StringComparison.OrdinalIgnoreCase))
                return _gambitLogoSprite;
            int suffix = teamId.IndexOf("_2021", StringComparison.OrdinalIgnoreCase);
            string slug = suffix > 0 ? teamId.Substring(0, suffix) : teamId;
            return Resources.Load<Sprite>("UI/Logos/" + slug);
        }'''

    if old_bind in text:
        text = text.replace(old_bind, new_bind, 1)

    if "private static readonly Color OfficialWinUi" not in text:
        text = text.replace(
            "        private static readonly Color AliveRowBg = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 0.72f);",
            "        private static readonly Color AliveRowBg = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 0.72f);\n"
            "        private static readonly Color OfficialWinUi = new Color(0.45f, 0.78f, 0.52f, 1f);\n"
            "        private static readonly Color OfficialLossUi = new Color(0.94f, 0.40f, 0.36f, 1f);",
            1,
        )

    if "CreateResultTeamLogo" not in text:
        raise RuntimeError("overlay patch incomplete")

    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
