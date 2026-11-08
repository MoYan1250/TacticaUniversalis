# UTF-8-safe: insert official match-end result overlay with logos beside the score.
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing anchor: {old[:120]!r}")
    return text.replace(old, new, 1)


FIELDS = """        private Image _rightTeamLogo;
        private Text _roundText;"""

FIELDS_NEW = """        private Image _rightTeamLogo;
        private GameObject _officialResultRoot;
        private Image _officialResultLeftLogo;
        private Image _officialResultRightLogo;
        private Text _officialResultLeftName;
        private Text _officialResultRightName;
        private Text _officialResultLeftScore;
        private Text _officialResultRightScore;
        private Text _officialResultOutcome;
        private bool _awaitingOfficialResultConfirm;
        private Text _roundText;"""

COLORS = """        private static readonly Color AliveRowBg = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 0.72f);"""

COLORS_NEW = """        private static readonly Color AliveRowBg = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 0.72f);
        private static readonly Color OfficialWinUi = new Color(0.45f, 0.78f, 0.52f, 1f);
        private static readonly Color OfficialLossUi = new Color(0.94f, 0.40f, 0.36f, 1f);"""

FLOW_OLD = """            if (_careerOfficialMatch && _result != null)
            {
                CareerMatchLaunchContext.SetCompletedResult(_result);
                UnityEngine.SceneManagement.SceneManager.LoadScene("Demo_Career");
            }
        }

        private IEnumerator PlayEventRange(int startInclusive, int endExclusive, bool animate)"""

FLOW_NEW = """            if (_careerOfficialMatch && _result != null)
                yield return ShowOfficialResultFlow();
        }

        private IEnumerator ShowOfficialResultFlow()
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
        }

        private IEnumerator PlayEventRange(int startInclusive, int endExclusive, bool animate)"""

BIND_OLD = """        private void BindTeamLogo(Image target, string teamId)
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
        }"""

BIND_NEW = """        private void BindTeamLogo(Image target, string teamId)
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
        }"""


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    if "CreateResultTeamLogo" in text and "HeightControlsWidth" in text and "_officialResultLeftLogo" in text:
        print("already present with fixed logos")
        return

    if "_officialResultLeftLogo" not in text:
        text = must_replace(text, FIELDS, FIELDS_NEW)
    if "OfficialWinUi" not in text:
        text = must_replace(text, COLORS, COLORS_NEW)
    if "ShowOfficialResultFlow" not in text:
        text = must_replace(text, FLOW_OLD, FLOW_NEW)
    if "ResolveTeamLogoSprite" not in text and BIND_OLD in text:
        text = text.replace(BIND_OLD, BIND_NEW, 1)

    for needle in ("比賽結束", "確認", "CreateResultTeamLogo", "HeightControlsWidth", "擊殺廣播"):
        if needle not in text:
            raise RuntimeError(f"missing after patch: {needle}")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
