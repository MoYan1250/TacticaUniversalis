# -*- coding: utf-8 -*-
"""Skip settle: remove top HUD spinner, lock double-click, keep clock ticking, ensure FinishMatch."""
from pathlib import Path
import backup_quickmatch as bak  # Tools/backup_quickmatch.py

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError("missing block:\n" + repr(old[:320]))
    return text.replace(old, new, 1)


NEW_SKIP_BLOCK = """        private void Skip()
        {
            if (IsGeneralBattleMode())
                return;
            if (_skippingMatch)
                return;

            _skippingMatch = true;
            if (_skipButton != null)
                _skipButton.interactable = false;

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

            if (_session != null)
            {
                if (_session.TimeoutActive)
                    _session.EndTimeoutWindow();
                if (_session.TimeoutPending)
                {
                    _session.TryBeginPendingTimeoutWindow();
                    _session.EndTimeoutWindow();
                }
            }

            _playback = StartCoroutine(SkipMatchRoutine());
        }

        private IEnumerator SkipMatchRoutine()
        {
            _skipBusyStartedAt = Time.unscaledTime;
            RefreshSkipBusyHud();
            try
            {
                // Yield each round so the elapsed clock can update.
                yield return null;

                if (_session != null)
                {
                    while (!_session.IsFinished)
                    {
                        if (!_session.AutoPlayOneRound(myTeamId))
                            break;
                        RefreshSkipBusyHud();
                        yield return null;
                    }

                    if (_session.IsComplete && !_session.IsFinished)
                        _session.FinishMatch();
                }

                _result = _session?.Result;
                ApplySkippedMatchPresentation();

                if (_careerOfficialMatch && _result != null && _result.winnerTeamId != null)
                    yield return ShowOfficialResultFlow();
            }
            finally
            {
                _skippingMatch = false;
                _playback = null;
                if (_skipButton != null)
                    _skipButton.interactable = _session == null || !_session.IsFinished;
            }
        }

        private void TickSkipBusyHud()
        {
            if (!_skippingMatch)
                return;
            RefreshSkipBusyHud();
        }

"""


def patch(text: str) -> str:
    text = must_replace(
        text,
        "        private bool _careerOfficialMatch;\n"
        "        private bool _skippingMatch;\n"
        "        private Image _skipBusySpinner;\n"
        "        private float _skipBusyStartedAt;\n",
        "        private bool _careerOfficialMatch;\n"
        "        private bool _skippingMatch;\n"
        "        private float _skipBusyStartedAt;\n"
        "        private Button _skipButton;\n",
    )

    start = text.find("        private void Skip()\n")
    if start < 0:
        raise RuntimeError("Skip() not found")
    end = text.find("        private void RefreshSkipBusyHud()\n", start)
    if end < 0:
        raise RuntimeError("RefreshSkipBusyHud() not found after Skip")
    text = text[:start] + NEW_SKIP_BLOCK + text[end:]

    text = must_replace(
        text,
        "            TickSkipBusySpinner();\n",
        "            TickSkipBusyHud();\n",
    )

    text = must_replace(
        text,
        "            _skipBusySpinner = CreateUiObject<Image>(\"SkipBusySpinner\", topHud.transform);\n"
        "            SetRect(_skipBusySpinner.rectTransform, new Vector2(0.285f, 0.38f), new Vector2(0.335f, 0.62f));\n"
        "            _skipBusySpinner.sprite = _playbackSkipSprite;\n"
        "            _skipBusySpinner.preserveAspect = true;\n"
        "            _skipBusySpinner.color = TextPrimary;\n"
        "            _skipBusySpinner.raycastTarget = false;\n"
        "            _skipBusySpinner.gameObject.SetActive(false);\n"
        "\n",
        "",
    )

    text = must_replace(
        text,
        "            Button skipBtn = CreateIconButton(\"Skip\", controlBar.transform,\n"
        "                new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f),\n"
        "                _playbackSkipSprite, Skip, out _);\n"
        "            _skipButtonRect = skipBtn.GetComponent<RectTransform>();\n",
        "            Button skipBtn = CreateIconButton(\"Skip\", controlBar.transform,\n"
        "                new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f),\n"
        "                _playbackSkipSprite, Skip, out _);\n"
        "            _skipButton = skipBtn;\n"
        "            _skipButtonRect = skipBtn.GetComponent<RectTransform>();\n",
    )

    return text


def main() -> None:
    bak.backup("skip_settle_fix")
    original = TARGET.read_text(encoding="utf-8")
    updated = patch(original)
    for banned in ("_skipBusySpinner", "TickSkipBusySpinner", "SetSkipBusyVisible"):
        if banned in updated:
            raise RuntimeError(f"{banned} still present")
    for required in (
        "TickSkipBusyHud",
        "_skipButton = skipBtn",
        "_session.FinishMatch()",
        "EndTimeoutWindow()",
        "TryBeginPendingTimeoutWindow()",
    ):
        if required not in updated:
            raise RuntimeError(f"{required} missing")
    if "擊殺廣播" not in updated and "叫暫停" not in updated:
        raise RuntimeError("Chinese HUD anchors lost")
    TARGET.write_text(updated, encoding="utf-8")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
