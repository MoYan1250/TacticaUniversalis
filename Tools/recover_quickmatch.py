# Restore QuickMatchDemoController from clean UTF-8 backup + apply official UI, visuals, marker fix.
from pathlib import Path

import apply_quickmatch_patches as ui

BACKUP = Path(r"C:\Users\approlu\AppData\Roaming\Cursor\User\History\-27279c02\FwxJ.cs")
ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def patch_match_visuals_and_markers(text: str) -> str:
    if "_bgImage" not in text:
        text = text.replace(
            "        private RectTransform _mapRect;\n        private Sprite _mapSprite;",
            "        private RectTransform _mapRect;\n"
            "        private Image _bgImage;\n"
            "        private int _matchBackgroundIndex = -1;\n"
            "        private Sprite _mapSprite;",
            1,
        )

    if "DeferredInitialMapLayout" not in text:
        deferred_anchor = (
            "            GenerateMatch();\n"
            "            ApplyMatchModeUi();\n"
            "        }\n\n"
            "        private void ApplyMatchModeUi()"
        )
        if deferred_anchor in text:
            text = text.replace(
                deferred_anchor,
                "            GenerateMatch();\n"
                "            ApplyMatchModeUi();\n"
                "            StartCoroutine(DeferredInitialMapLayout());\n"
                "        }\n\n"
                "        private IEnumerator DeferredInitialMapLayout()\n"
                "        {\n"
                "            yield return null;\n"
                "            Canvas.ForceUpdateCanvases();\n"
                "            if (_session == null)\n"
                "                yield break;\n"
                "            SyncMatchMarkerVisibility();\n"
                "            ResetMarkers(_currentRound > 0 ? _currentRound : 1, false);\n"
                "            RelayoutZoneLabels();\n"
                "        }\n\n"
                "        private void ApplyMatchModeUi()",
                1,
            )
        else:
            text = text.replace(
                "            GenerateMatch();\n        }\n\n        private void OnDestroy()",
                "            GenerateMatch();\n"
                "            ApplyMatchModeUi();\n"
                "            StartCoroutine(DeferredInitialMapLayout());\n"
                "        }\n\n"
                "        private IEnumerator DeferredInitialMapLayout()\n"
                "        {\n"
                "            yield return null;\n"
                "            Canvas.ForceUpdateCanvases();\n"
                "            if (_session == null)\n"
                "                yield break;\n"
                "            SyncMatchMarkerVisibility();\n"
                "            ResetMarkers(_currentRound > 0 ? _currentRound : 1, false);\n"
                "            RelayoutZoneLabels();\n"
                "        }\n\n"
                "        private void OnDestroy()",
                1,
            )

    if "ApplyMatchBackground()" not in text:
        text = text.replace(
            "                myTeamId, _opponentTeamId);\n            _result = _session.Result;",
            "                myTeamId, _opponentTeamId);\n"
            "            ApplyMatchBackground();\n"
            "            _result = _session.Result;",
            1,
        )

    if "SyncMatchMarkerVisibility()" not in text or text.count("SyncMatchMarkerVisibility()") < 2:
        text = text.replace(
            "            ApplyMapOrientation(1, false);\n            ResetMarkers(1, false);",
            "            ApplyMapOrientation(1, false);\n"
            "            SyncMatchMarkerVisibility();\n"
            "            ResetMarkers(1, false);",
            1,
        )

    old_my_team = (
        "        private bool MyTeamIsTerrorist(int roundNumber)\n"
        "        {\n"
        "            bool naviIsT = MatchRules.TeamAIsTerrorist(Mathf.Max(0, roundNumber - 1));\n"
        "            bool myIsNavi = myTeamId == \"navi_2021\";\n"
        "            return myIsNavi ? naviIsT : !naviIsT;\n"
        "        }\n\n"
        "        private void ApplyMapOrientation(int roundNumber, bool animate)"
    )
    new_my_team = (
        "        private bool MyTeamIsTerrorist(int roundNumber)\n"
        "        {\n"
        "            bool teamAIsT = MatchRules.TeamAIsTerrorist(Mathf.Max(0, roundNumber - 1));\n"
        "            if (_session != null)\n"
        "            {\n"
        "                string teamAId = _session.State.TeamA.Definition.id;\n"
        "                bool myIsTeamA = string.Equals(myTeamId, teamAId, StringComparison.OrdinalIgnoreCase);\n"
        "                return myIsTeamA ? teamAIsT : !teamAIsT;\n"
        "            }\n\n"
        "            bool naviIsT = teamAIsT;\n"
        "            bool myIsNavi = myTeamId == \"navi_2021\";\n"
        "            return myIsNavi ? naviIsT : !naviIsT;\n"
        "        }\n\n"
        "        private void ApplyMatchBackground()\n"
        "        {\n"
        "            if (_bgImage == null)\n"
        "                return;\n"
        "            int idx = Mathf.Abs(seed) % 4;\n"
        "            if (_matchBackgroundIndex == idx && _bgImage.sprite != null)\n"
        "                return;\n"
        "            _matchBackgroundIndex = idx;\n"
        "            Sprite sprite = Resources.Load<Sprite>(\"UI/Backgrounds/dust2_bg_\" + idx);\n"
        "            _bgImage.sprite = sprite;\n"
        "            _bgImage.enabled = sprite != null;\n"
        "            _bgImage.color = sprite != null ? Color.white : Bg;\n"
        "        }\n\n"
        "        private void SyncMatchMarkerVisibility()\n"
        "        {\n"
        "            var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);\n"
        "            if (_session != null)\n"
        "            {\n"
        "                foreach (string id in _session.State.TeamA.Definition.playerIds)\n"
        "                    active.Add(id);\n"
        "                foreach (string id in _session.State.TeamB.Definition.playerIds)\n"
        "                    active.Add(id);\n"
        "            }\n"
        "            else\n"
        "            {\n"
        "                if (_content.Teams.TryGetValue(myTeamId, out TeamDefinition mine))\n"
        "                {\n"
        "                    foreach (string id in mine.playerIds)\n"
        "                        active.Add(id);\n"
        "                }\n\n"
        "                if (_content.Teams.TryGetValue(_opponentTeamId, out TeamDefinition opponent))\n"
        "                {\n"
        "                    foreach (string id in opponent.playerIds)\n"
        "                        active.Add(id);\n"
        "                }\n"
        "            }\n\n"
        "            foreach (KeyValuePair<string, RectTransform> pair in _markers)\n"
        "                pair.Value.gameObject.SetActive(active.Contains(pair.Key));\n"
        "        }\n\n"
        "        private void ApplyMapOrientation(int roundNumber, bool animate)"
    )
    if "private void ApplyMatchBackground()" not in text:
        text = text.replace(old_my_team, new_my_team, 1)

    old_team_is_t = (
        "        private bool TeamIsTerrorist(string teamId, int roundNumber)\n"
        "        {\n"
        "            bool naviIsT = MatchRules.TeamAIsTerrorist(Mathf.Max(0, roundNumber - 1));\n"
        "            if (teamId == \"navi_2021\")\n"
        "                return naviIsT;\n"
        "            if (teamId == \"gambit_2021\")\n"
        "                return !naviIsT;\n"
        "            return false;\n"
        "        }"
    )
    new_team_is_t = (
        "        private bool TeamIsTerrorist(string teamId, int roundNumber)\n"
        "        {\n"
        "            bool teamAIsT = MatchRules.TeamAIsTerrorist(Mathf.Max(0, roundNumber - 1));\n"
        "            if (_session != null)\n"
        "            {\n"
        "                string teamAId = _session.State.TeamA.Definition.id;\n"
        "                if (string.Equals(teamId, teamAId, StringComparison.OrdinalIgnoreCase))\n"
        "                    return teamAIsT;\n"
        "                string teamBId = _session.State.TeamB.Definition.id;\n"
        "                if (string.Equals(teamId, teamBId, StringComparison.OrdinalIgnoreCase))\n"
        "                    return !teamAIsT;\n"
        "                return false;\n"
        "            }\n\n"
        "            if (teamId == \"navi_2021\")\n"
        "                return teamAIsT;\n"
        "            if (teamId == \"gambit_2021\")\n"
        "                return !teamAIsT;\n"
        "            return false;\n"
        "        }"
    )
    if old_team_is_t in text:
        text = text.replace(old_team_is_t, new_team_is_t, 1)

    bg_block_old = (
        "            Image background = CreateUiObject<Image>(\"Background\", canvas.transform);\n"
        "            Stretch(background.rectTransform);\n"
        "            background.color = Bg;\n\n"
        "            // Map slot (actual frame is fitted to sprite aspect to remove side letterbox)\n"
        "            Image mapFrame = CreateUiObject<Image>(\"MapFrame\", canvas.transform);\n"
        "            _mapFrame = mapFrame.rectTransform;\n"
        "            mapFrame.color = new Color(0.12f, 0.13f, 0.145f, 1f);\n\n"
        "            // Top-center: large logos close to scores; scores hug ROUND / clock\n"
        "            Image topHud = CreateUiObject<Image>(\"TopHud\", canvas.transform);\n"
        "            SetRect(topHud.rectTransform, new Vector2(0.32f, 0.900f), new Vector2(0.68f, 0.992f));\n"
        "            topHud.color = new Color(Panel.r, Panel.g, Panel.b, 0.92f);"
    )
    bg_block_new = (
        "            Image background = CreateUiObject<Image>(\"Background\", canvas.transform);\n"
        "            Stretch(background.rectTransform);\n"
        "            background.color = Bg;\n\n"
        "            Image bgPhoto = CreateUiObject<Image>(\"MatchBackgroundPhoto\", canvas.transform);\n"
        "            _bgImage = bgPhoto;\n"
        "            Stretch(bgPhoto.rectTransform);\n"
        "            bgPhoto.preserveAspect = false;\n"
        "            bgPhoto.raycastTarget = false;\n"
        "            bgPhoto.color = Bg;\n\n"
        "            Image bgVignette = CreateUiObject<Image>(\"BackgroundVignette\", canvas.transform);\n"
        "            Stretch(bgVignette.rectTransform);\n"
        "            bgVignette.raycastTarget = false;\n"
        "            bgVignette.color = new Color(0.02f, 0.025f, 0.04f, 0.52f);\n\n"
        "            // Map slot (actual frame is fitted to sprite aspect to remove side letterbox)\n"
        "            Image mapFrame = CreateUiObject<Image>(\"MapFrame\", canvas.transform);\n"
        "            _mapFrame = mapFrame.rectTransform;\n"
        "            mapFrame.color = new Color(0.12f, 0.13f, 0.145f, 0.82f);\n\n"
        "            // Top-center: large logos close to scores; scores hug ROUND / clock\n"
        "            Image topHud = CreateUiObject<Image>(\"TopHud\", canvas.transform);\n"
        "            SetRect(topHud.rectTransform, new Vector2(0.32f, 0.900f), new Vector2(0.68f, 0.992f));\n"
        "            topHud.color = new Color(Panel.r, Panel.g, Panel.b, 0.72f);"
    )
    if "MatchBackgroundPhoto" not in text:
        text = text.replace(bg_block_old, bg_block_new, 1)

    text = text.replace(
        "            killBlock.color = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 0.92f);",
        "            killBlock.color = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 0.72f);",
        1,
    )
    text = text.replace(
        "            traitBlock.color = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 0.92f);",
        "            traitBlock.color = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 0.72f);",
        1,
    )
    text = text.replace(
        "            leftPanel.color = CtPanel;",
        "            leftPanel.color = new Color(CtPanel.r, CtPanel.g, CtPanel.b, 0.78f);",
        1,
    )
    text = text.replace(
        "            rightPanel.color = TPanel;",
        "            rightPanel.color = new Color(TPanel.r, TPanel.g, TPanel.b, 0.78f);",
        1,
    )
    text = text.replace(
        "            controlBar.color = Panel;",
        "            controlBar.color = new Color(Panel.r, Panel.g, Panel.b, 0.78f);",
        1,
    )

    for needle in (
        "擊殺廣播",
        "SyncMatchMarkerVisibility",
        "ApplyMatchBackground",
        "MatchBackgroundPhoto",
        "ApplyMatchModeUi",
    ):
        if needle not in text:
            raise RuntimeError(f"missing after patch: {needle}")
    return text


def main() -> None:
    if not BACKUP.is_file():
        raise SystemExit(f"backup missing: {BACKUP}")
    text = BACKUP.read_text(encoding="utf-8")
    text = ui.patch(text)
    text = patch_match_visuals_and_markers(text)
    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("recovered", TARGET)


if __name__ == "__main__":
    main()
