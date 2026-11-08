# -*- coding: utf-8 -*-
"""
TopHud order: series(big) | logo | round-score(small) | center | small | logo | series
- Series pips stacked vertically as true circles (not ellipses)
- Logo slot always reserves space when sprite missing
"""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError("missing:\n" + old[:280])
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_tophud_series_layout")
    text = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in text:
        raise RuntimeError("UTF-8 corrupted")

    # --- TopHud: widen slightly; reorder left/right chrome ---
    text = must_replace(
        text,
        """            // Top-center: large logos close to scores; scores hug ROUND / clock
            Image topHud = CreateUiObject<Image>("TopHud", canvas.transform);
            SetRect(topHud.rectTransform, new Vector2(0.32f, 0.886f), new Vector2(0.68f, 0.992f));
            topHud.color = new Color(Panel.r, Panel.g, Panel.b, 0.72f);

            _leftTeamLogo = CreateTeamLogo("LeftLogo", topHud.transform, 0.11f);
            _leftSeriesPips = CreateSeriesPips("LeftSeriesPips", topHud.transform, 0.185f, true);
            _leftScoreText = CreateText("LeftScore", topHud.transform, 42, TextAnchor.MiddleRight, TextPrimary);
            SetRect(_leftScoreText.rectTransform, new Vector2(0.27f, 0.28f), new Vector2(0.37f, 0.95f));
            _leftScoreText.fontStyle = FontStyle.Bold;
            _leftScoreText.resizeTextForBestFit = true;
            _leftScoreText.resizeTextMinSize = 22;
            _leftScoreText.resizeTextMaxSize = 54;

            _roundText = CreateText("Round", topHud.transform, 17, TextAnchor.MiddleCenter, Accent);
            SetRect(_roundText.rectTransform, new Vector2(0.37f, 0.66f), new Vector2(0.63f, 0.96f));
""",
        """            // L→R: series | logo | round score | ROUND/clock/meta | round score | logo | series
            Image topHud = CreateUiObject<Image>("TopHud", canvas.transform);
            SetRect(topHud.rectTransform, new Vector2(0.28f, 0.886f), new Vector2(0.72f, 0.992f));
            topHud.color = new Color(Panel.r, Panel.g, Panel.b, 0.72f);

            _leftSeriesPips = CreateSeriesPips("LeftSeriesPips", topHud.transform, 0.045f, true);
            _leftTeamLogo = CreateTeamLogo("LeftLogo", topHud.transform, 0.13f);
            _leftScoreText = CreateText("LeftScore", topHud.transform, 42, TextAnchor.MiddleRight, TextPrimary);
            SetRect(_leftScoreText.rectTransform, new Vector2(0.20f, 0.28f), new Vector2(0.32f, 0.95f));
            _leftScoreText.fontStyle = FontStyle.Bold;
            _leftScoreText.resizeTextForBestFit = true;
            _leftScoreText.resizeTextMinSize = 22;
            _leftScoreText.resizeTextMaxSize = 54;

            _roundText = CreateText("Round", topHud.transform, 17, TextAnchor.MiddleCenter, Accent);
            SetRect(_roundText.rectTransform, new Vector2(0.34f, 0.66f), new Vector2(0.66f, 0.96f));
""",
    )

    text = must_replace(
        text,
        """            _clockText = CreateText("Clock", topHud.transform, 20, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_clockText.rectTransform, new Vector2(0.37f, 0.36f), new Vector2(0.63f, 0.64f));
""",
        """            _clockText = CreateText("Clock", topHud.transform, 20, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_clockText.rectTransform, new Vector2(0.34f, 0.36f), new Vector2(0.66f, 0.64f));
""",
    )

    text = must_replace(
        text,
        """            _careerMatchMetaText = CreateText("CareerMeta", topHud.transform, 13, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_careerMatchMetaText.rectTransform, new Vector2(0.28f, 0.02f), new Vector2(0.72f, 0.34f));
""",
        """            _careerMatchMetaText = CreateText("CareerMeta", topHud.transform, 13, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_careerMatchMetaText.rectTransform, new Vector2(0.32f, 0.02f), new Vector2(0.68f, 0.34f));
""",
    )

    text = must_replace(
        text,
        """            _rightScoreText = CreateText("RightScore", topHud.transform, 42, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(_rightScoreText.rectTransform, new Vector2(0.63f, 0.28f), new Vector2(0.73f, 0.95f));
            _rightScoreText.fontStyle = FontStyle.Bold;
            _rightScoreText.resizeTextForBestFit = true;
            _rightScoreText.resizeTextMinSize = 22;
            _rightScoreText.resizeTextMaxSize = 54;
            _rightTeamLogo = CreateTeamLogo("RightLogo", topHud.transform, 0.89f);
            _rightSeriesPips = CreateSeriesPips("RightSeriesPips", topHud.transform, 0.815f, false);
""",
        """            _rightScoreText = CreateText("RightScore", topHud.transform, 42, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(_rightScoreText.rectTransform, new Vector2(0.68f, 0.28f), new Vector2(0.80f, 0.95f));
            _rightScoreText.fontStyle = FontStyle.Bold;
            _rightScoreText.resizeTextForBestFit = true;
            _rightScoreText.resizeTextMinSize = 22;
            _rightScoreText.resizeTextMaxSize = 54;
            _rightTeamLogo = CreateTeamLogo("RightLogo", topHud.transform, 0.87f);
            _rightSeriesPips = CreateSeriesPips("RightSeriesPips", topHud.transform, 0.955f, false);
""",
    )

    # --- Vertical circular series pips ---
    text = must_replace(
        text,
        """        private Image[] CreateSeriesPips(string name, Transform parent, float anchorX, bool growRight)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorX, 0.28f);
            rt.anchorMax = new Vector2(anchorX, 0.88f);
            rt.pivot = new Vector2(growRight ? 0f : 1f, 0.5f);
            rt.sizeDelta = new Vector2(36f, 0f);
            rt.anchoredPosition = Vector2.zero;

            Image[] pips = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                Image pip = CreateUiObject<Image>("Pip" + i, root.transform);
                RectTransform pr = pip.rectTransform;
                float x0 = growRight ? (0.05f + i * 0.48f) : (0.05f + (1 - i) * 0.48f);
                pr.anchorMin = new Vector2(x0, 0.18f);
                pr.anchorMax = new Vector2(x0 + 0.42f, 0.82f);
                pr.offsetMin = Vector2.zero;
                pr.offsetMax = Vector2.zero;
                pip.sprite = _circleSprite;
                pip.color = SeriesPipOff;
                pip.raycastTarget = false;
                pips[i] = pip;
            }

            return pips;
        }
""",
        """        private Image[] CreateSeriesPips(string name, Transform parent, float anchorX, bool growRight)
        {
            int winsNeeded = Mathf.Max(1, _seriesBestOf / 2 + 1);
            _ = growRight;
            const float pipSize = 13f;
            const float gap = 4f;
            float totalH = winsNeeded * pipSize + (winsNeeded - 1) * gap;

            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorX, 0.5f);
            rt.anchorMax = new Vector2(anchorX, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(pipSize, totalH);
            rt.anchoredPosition = Vector2.zero;

            Image[] pips = new Image[winsNeeded];
            for (int i = 0; i < winsNeeded; i++)
            {
                Image pip = CreateUiObject<Image>("Pip" + i, root.transform);
                RectTransform pr = pip.rectTransform;
                pr.anchorMin = new Vector2(0.5f, 1f);
                pr.anchorMax = new Vector2(0.5f, 1f);
                pr.pivot = new Vector2(0.5f, 1f);
                pr.sizeDelta = new Vector2(pipSize, pipSize);
                pr.anchoredPosition = new Vector2(0f, -(i * (pipSize + gap)));
                pip.sprite = _circleSprite;
                pip.type = Image.Type.Simple;
                pip.preserveAspect = true;
                pip.color = SeriesPipOff;
                pip.raycastTarget = false;
                pips[i] = pip;
            }

            return pips;
        }
""",
    )

    # --- Logo always reserves space ---
    text = must_replace(
        text,
        """        private void BindTeamLogo(Image target, string teamId)
        {
            if (target == null)
                return;
            Sprite sprite = ResolveTeamLogoSprite(teamId);
            target.sprite = sprite;
            target.enabled = sprite != null;
            target.color = Color.white;
        }
""",
        """        private void BindTeamLogo(Image target, string teamId)
        {
            if (target == null)
                return;
            Sprite sprite = ResolveTeamLogoSprite(teamId);
            target.sprite = sprite;
            target.enabled = true;
            target.color = sprite != null ? Color.white : Color.clear;
        }
""",
    )

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in verify:
        raise RuntimeError("UTF-8 lost after write")
    if "pipSize = 13f" not in verify:
        raise RuntimeError("vertical pips not applied")
    if "Color.clear" not in verify or "target.enabled = true" not in verify:
        raise RuntimeError("logo reserve space not applied")
    if "_leftSeriesPips = CreateSeriesPips(\"LeftSeriesPips\", topHud.transform, 0.045f" not in verify:
        raise RuntimeError("TopHud order not applied")
    # growRight unused but kept for call-site compat — silence by referencing
    if "bool growRight" not in verify:
        raise RuntimeError("CreateSeriesPips signature broken")
    print("patched TopHud series layout OK")


if __name__ == "__main__":
    main()
