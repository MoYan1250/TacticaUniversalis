# -*- coding: utf-8 -*-
"""Planted bomb: 40s clock countdown + red map marker."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"anchor missing:\n{old[:220]!r}")
    return text.replace(old, new, 1)


def main() -> int:
    subprocess.check_call(
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "bomb_timer_red"]
    )
    text = TARGET.read_text(encoding="utf-8")
    if "_bombPlantedAtRoundTime" in text and "SetBombMarkerPlantedStyle" in text:
        print("already patched")
        return 0

    text = must_replace(
        text,
        "        private string _bombCarrierId;\n"
        "        private string _droppedBombZoneId;\n",
        "        private string _bombCarrierId;\n"
        "        private string _droppedBombZoneId;\n"
        "        /// <summary>Round clock time when bomb was planted; negative = not planted.</summary>\n"
        "        private float _bombPlantedAtRoundTime = -1f;\n"
        "        private const float BombCountdownSeconds = 40f;\n"
        "        private Image _bombMarkerGlow;\n"
        "        private Image _bombMarkerCore;\n",
    )

    text = must_replace(
        text,
        "                    _bombCarrierId = null;\n"
        "                    _droppedBombZoneId = null;\n"
        "                    HideBomb();\n",
        "                    _bombCarrierId = null;\n"
        "                    _droppedBombZoneId = null;\n"
        "                    _bombPlantedAtRoundTime = -1f;\n"
        "                    HideBomb();\n",
    )

    text = must_replace(
        text,
        """                case SimulationEventType.BombPlanted:
                    _droppedBombZoneId = null;
                    _bombCarrierId = evt.actorId;
                    MoveMarkerAlongPath(evt.actorId, evt.zoneId, animate);
                    ShowBomb(evt.zoneId);
                    RefreshStatPanels();
                    break;
                case SimulationEventType.BombDefused:
                case SimulationEventType.BombExploded:
                    HideBomb();
                    break;
""",
        """                case SimulationEventType.BombPlanted:
                    _droppedBombZoneId = null;
                    _bombCarrierId = evt.actorId;
                    _bombPlantedAtRoundTime = evt.roundTime;
                    MoveMarkerAlongPath(evt.actorId, evt.zoneId, animate);
                    ShowBomb(evt.zoneId, planted: true);
                    SetRoundHud(evt.roundNumber, FormatClock(evt.roundTime));
                    RefreshStatPanels();
                    break;
                case SimulationEventType.BombDefused:
                case SimulationEventType.BombExploded:
                    _bombPlantedAtRoundTime = -1f;
                    HideBomb();
                    break;
""",
    )

    text = must_replace(
        text,
        """        private void ShowBomb(string zoneId)
        {
            if (_bombMarker == null)
                return;
            _bombZoneId = zoneId;
            _bombMarker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId));
            _bombMarker.SetAsLastSibling();
            RefreshFloorVisibility();
        }

        private void HideBomb()
        {
            _bombZoneId = null;
            if (_bombMarker != null)
                _bombMarker.gameObject.SetActive(false);
        }
""",
        """        private void ShowBomb(string zoneId, bool planted = false)
        {
            if (_bombMarker == null)
                return;
            _bombZoneId = zoneId;
            _bombMarker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId));
            SetBombMarkerPlantedStyle(planted);
            _bombMarker.gameObject.SetActive(true);
            _bombMarker.SetAsLastSibling();
            RefreshFloorVisibility();
        }

        private void SetBombMarkerPlantedStyle(bool planted)
        {
            if (_bombMarkerGlow != null)
                _bombMarkerGlow.color = planted
                    ? new Color(0.95f, 0.12f, 0.12f, 0.40f)
                    : new Color(0.95f, 0.55f, 0.12f, 0.35f);
            if (_bombMarkerCore != null)
                _bombMarkerCore.color = planted
                    ? new Color(0.92f, 0.12f, 0.12f, 1f)
                    : new Color(0.98f, 0.72f, 0.18f, 1f);
        }

        private void HideBomb()
        {
            _bombZoneId = null;
            if (_bombMarker != null)
                _bombMarker.gameObject.SetActive(false);
        }
""",
    )

    text = must_replace(
        text,
        """            Image glow = CreateUiObject<Image>("Glow", root.transform);
            glow.sprite = _circleSprite;
            glow.color = new Color(0.95f, 0.55f, 0.12f, 0.35f);
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            glow.rectTransform.sizeDelta = new Vector2(36f, 36f);

            Image core = CreateUiObject<Image>("Core", root.transform);
            core.sprite = _circleSprite;
            core.color = new Color(0.98f, 0.72f, 0.18f, 1f);
            core.rectTransform.anchorMin = core.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            core.rectTransform.sizeDelta = new Vector2(18f, 18f);
""",
        """            Image glow = CreateUiObject<Image>("Glow", root.transform);
            glow.sprite = _circleSprite;
            glow.color = new Color(0.95f, 0.55f, 0.12f, 0.35f);
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            glow.rectTransform.sizeDelta = new Vector2(36f, 36f);
            _bombMarkerGlow = glow;

            Image core = CreateUiObject<Image>("Core", root.transform);
            core.sprite = _circleSprite;
            core.color = new Color(0.98f, 0.72f, 0.18f, 1f);
            core.rectTransform.anchorMin = core.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            core.rectTransform.sizeDelta = new Vector2(18f, 18f);
            _bombMarkerCore = core;
""",
    )

    # Dropped bomb ShowBomb(zone) stays orange (planted:false default)
    # FormatClock: planted → 40s countdown
    text = must_replace(
        text,
        """        private static string FormatClock(float seconds)
        {
            // Whole seconds only; general-mode sim may advance at 0.01s.
            int remaining = Mathf.Max(0, 115 - Mathf.FloorToInt(seconds));
            return $"{remaining / 60}:{remaining % 60:00}";
        }
""",
        """        private string FormatClock(float seconds)
        {
            // After plant, HUD switches to the 40s bomb countdown.
            if (_bombPlantedAtRoundTime >= 0f)
            {
                float elapsed = Mathf.Max(0f, seconds - _bombPlantedAtRoundTime);
                int remaining = Mathf.Max(0, Mathf.CeilToInt(BombCountdownSeconds - elapsed));
                return $"{remaining / 60}:{remaining % 60:00}";
            }

            int roundLeft = Mathf.Max(0, 115 - Mathf.FloorToInt(seconds));
            return $"{roundLeft / 60}:{roundLeft % 60:00}";
        }
""",
    )

    if "叫暫停" not in text:
        raise RuntimeError("UTF-8 corruption")
    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
