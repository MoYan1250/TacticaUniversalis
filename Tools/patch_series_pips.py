# -*- coding: utf-8 -*-
"""BO3 series score pips on TopHud + official result overlay. UTF-8 safe."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError("missing:\n" + old[:240])
    return text.replace(old, new, 1)


def main() -> None:
    subprocess.check_call(
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "series_pips"]
    )
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text and "叫暫停" not in text:
        raise RuntimeError("UTF-8 look corrupted")

    if "_seriesPipOff" in text:
        print("already patched")
        return

    text = must_replace(
        text,
        """        private string _careerMapDisplayName;
""",
        """        private string _careerMapDisplayName;
        private string _careerMapId;
        private int _seriesScorePlayer;
        private int _seriesScoreOpponent;
        private int _seriesMapIndex;
        private int _seriesBestOf = 3;
        private Image[] _leftSeriesPips;
        private Image[] _rightSeriesPips;
        private Image[] _resultLeftSeriesPips;
        private Image[] _resultRightSeriesPips;
        private Text _officialResultSeriesMeta;
        private static readonly Color SeriesPipOff = new Color(0f, 0f, 0f, 1f);
        private static readonly Color SeriesPipT = HexColor(0xE0A526);
        private static readonly Color SeriesPipCt = HexColor(0x1B99FF);
""",
    )

    # HexColor helper near other color helpers if missing
    if "private static Color HexColor(int rgb)" not in text:
        text = must_replace(
            text,
            """        private bool MyTeamIsTerrorist(int roundNumber)
""",
            """        private static Color HexColor(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        private bool MyTeamIsTerrorist(int roundNumber)
""",
        )

    text = must_replace(
        text,
        """                _careerMapDisplayName = string.IsNullOrEmpty(launch.mapDisplayName) ? "Dust II" : launch.mapDisplayName;
                _careerBracketStageLabel = launch.bracketStageLabel;
            }
""",
        """                _careerMapDisplayName = string.IsNullOrEmpty(launch.mapDisplayName) ? "Dust II" : launch.mapDisplayName;
                _careerMapId = string.IsNullOrEmpty(launch.mapId) ? "dust2" : launch.mapId;
                _careerBracketStageLabel = launch.bracketStageLabel;
                _seriesBestOf = launch.bestOf > 0 ? launch.bestOf : 3;
                _seriesMapIndex = Mathf.Max(0, launch.seriesMapIndex);
                _seriesScorePlayer = Mathf.Max(0, launch.seriesScorePlayer);
                _seriesScoreOpponent = Mathf.Max(0, launch.seriesScoreOpponent);
            }
""",
    )

    text = must_replace(
        text,
        """            _leftTeamLogo = CreateTeamLogo("LeftLogo", topHud.transform, 0.11f);
""",
        """            _leftTeamLogo = CreateTeamLogo("LeftLogo", topHud.transform, 0.11f);
            _leftSeriesPips = CreateSeriesPips("LeftSeriesPips", topHud.transform, 0.185f, true);
""",
    )

    text = must_replace(
        text,
        """            _rightTeamLogo = CreateTeamLogo("RightLogo", topHud.transform, 0.89f);
""",
        """            _rightTeamLogo = CreateTeamLogo("RightLogo", topHud.transform, 0.89f);
            _rightSeriesPips = CreateSeriesPips("RightSeriesPips", topHud.transform, 0.815f, false);
""",
    )

    # Insert CreateSeriesPips + RefreshSeriesPips before CreateTeamLogo
    text = must_replace(
        text,
        """        private Image CreateTeamLogo(string name, Transform parent, float anchorX)
""",
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

        private void RefreshSeriesPips()
        {
            bool leftIsT = false;
            bool rightIsT = true;
            if (_session != null)
            {
                int round = Mathf.Max(1, _currentRound);
                bool myT = MyTeamIsTerrorist(round);
                // Official HUD: left = my team, right = opponent.
                leftIsT = myT;
                rightIsT = !myT;
            }

            ApplySeriesPipColors(_leftSeriesPips, _seriesScorePlayer, leftIsT);
            ApplySeriesPipColors(_rightSeriesPips, _seriesScoreOpponent, rightIsT);
            ApplySeriesPipColors(_resultLeftSeriesPips, _seriesScorePlayer, leftIsT);
            ApplySeriesPipColors(_resultRightSeriesPips, _seriesScoreOpponent, rightIsT);
        }

        private static void ApplySeriesPipColors(Image[] pips, int wins, bool isTerrorist)
        {
            if (pips == null)
                return;
            Color on = isTerrorist ? SeriesPipT : SeriesPipCt;
            for (int i = 0; i < pips.Length; i++)
            {
                if (pips[i] == null)
                    continue;
                pips[i].color = i < wins ? on : SeriesPipOff;
            }
        }

        private Image CreateTeamLogo(string name, Transform parent, float anchorX)
""",
    )

    # Refresh pips when match mode UI applies
    if "RefreshSeriesPips();" not in text:
        # Hook after ApplyMatchModeUi body start - find SyncMatchMarker or similar end of ApplyMatchModeUi
        marker = "        private void ApplyMatchModeUi()"
        if marker not in text:
            raise RuntimeError("ApplyMatchModeUi missing")
        # Call at end of GenerateMatch / ApplyMapOrientation paths - simpler: after ApplyMatchModeUi(); in Start
        text = must_replace(
            text,
            """            ApplyMatchModeUi();
            StartCoroutine(DeferredInitialMapLayout());
""",
            """            ApplyMatchModeUi();
            RefreshSeriesPips();
            StartCoroutine(DeferredInitialMapLayout());
""",
        )

    # Update result presentation
    text = must_replace(
        text,
        """            if (_officialResultOutcome != null)
            {
                _officialResultOutcome.text = won ? "勝利" : "戰敗";
                _officialResultOutcome.color = won ? OfficialWinUi : OfficialLossUi;
            }
        }
""",
        """            if (_officialResultOutcome != null)
            {
                _officialResultOutcome.text = won ? "勝利" : "戰敗";
                _officialResultOutcome.color = won ? OfficialWinUi : OfficialLossUi;
            }

            if (_careerOfficialMatch)
            {
                int nextPlayer = _seriesScorePlayer + (won ? 1 : 0);
                int nextOpp = _seriesScoreOpponent + (won ? 0 : 1);
                ApplySeriesPipColors(_resultLeftSeriesPips, nextPlayer, MyTeamIsTerrorist(Mathf.Max(1, _currentRound)));
                ApplySeriesPipColors(_resultRightSeriesPips, nextOpp, !MyTeamIsTerrorist(Mathf.Max(1, _currentRound)));
                if (_officialResultSeriesMeta != null)
                {
                    _officialResultSeriesMeta.gameObject.SetActive(true);
                    _officialResultSeriesMeta.text =
                        "地圖 " + (_seriesMapIndex + 1) + " / " + _seriesBestOf +
                        " · 系列 " + nextPlayer + "-" + nextOpp;
                }
            }
            else if (_officialResultSeriesMeta != null)
                _officialResultSeriesMeta.gameObject.SetActive(false);
        }
""",
    )

    # Add result pips after logos in EnsureOfficialResultOverlay
    text = must_replace(
        text,
        """            _officialResultLeftLogo = CreateResultTeamLogo("LeftLogo", panel.transform, 0.18f);
            _officialResultRightLogo = CreateResultTeamLogo("RightLogo", panel.transform, 0.82f);
""",
        """            _officialResultLeftLogo = CreateResultTeamLogo("LeftLogo", panel.transform, 0.18f);
            _resultLeftSeriesPips = CreateSeriesPips("ResultLeftPips", panel.transform, 0.26f, true);
            _officialResultRightLogo = CreateResultTeamLogo("RightLogo", panel.transform, 0.82f);
            _resultRightSeriesPips = CreateSeriesPips("ResultRightPips", panel.transform, 0.74f, false);
""",
    )

    if "_officialResultSeriesMeta" in text and "SeriesMeta" not in text[text.find("_officialResultOutcome") : text.find("_officialResultOutcome") + 800]:
        text = must_replace(
            text,
            """            SetRect(_officialResultOutcome.rectTransform, new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.34f));
""",
            """            SetRect(_officialResultOutcome.rectTransform, new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.34f));

            _officialResultSeriesMeta = CreateText("SeriesMeta", panel.transform, 14, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_officialResultSeriesMeta.rectTransform, new Vector2(0.20f, 0.12f), new Vector2(0.80f, 0.22f));
            _officialResultSeriesMeta.gameObject.SetActive(false);
""",
        )

    # Also refresh pips when round side flips - Find ApplyMapOrientation or similar
    if "ApplyMapOrientation" in text and "RefreshSeriesPips();" in text:
        # add call inside ApplyMapOrientation if we can find a safe spot
        old_orient = None
        # Try after shouldFlip block ending - optional

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    assert "SeriesPipT" in text
    assert "CreateSeriesPips" in text
    assert "擊殺廣播" in text or "叫暫停" in text
    print("patched series pips ok")


if __name__ == "__main__":
    main()
