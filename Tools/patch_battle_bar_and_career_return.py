# -*- coding: utf-8 -*-
"""Fix official control bar layout invert + career return null UI after match."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
QM = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"
CAREER_APP = ROOT / "Assets/_Project/Scripts/Application/CareerApplication.cs"
CAREER_UI = ROOT / "Assets/_Project/Scripts/Presentation/CareerDemoController.cs"
DOMAIN = ROOT / "Assets/_Project/Scripts/Domain/CareerDomain.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:160]!r}")
    return text.replace(old, new, 1)


def patch_quickmatch() -> None:
    text = QM.read_text(encoding="utf-8")
    if "擊殺廣播" not in text:
        raise RuntimeError("QuickMatch UTF-8 corrupted")

    # Critical: was ApplyControlBarLayout(!simulation) → official used practice/full bar
    text = must_replace(
        text,
        "            ApplyControlBarLayout(!simulation);\n"
        "            ApplySettingsFrostedChrome(true);",
        "            // simulation=true → practice (wide bar); false → official (compact centered)\n"
        "            ApplyControlBarLayout(simulation);\n"
        "            ApplySettingsFrostedChrome(true);",
    )

    QM.write_text(text, encoding="utf-8", newline="\n")
    print("patched", QM)


def patch_domain_club_lookup() -> None:
    text = DOMAIN.read_text(encoding="utf-8")
    if "TryGetClub" in text:
        print("TryGetClub already present")
        return

    text = must_replace(
        text,
        "        public ClubState Club(string id) =>\n"
        "            clubs.First(x => string.Equals(x.clubId, id, StringComparison.OrdinalIgnoreCase));\n\n"
        "        public PlayerCareerState Player(string id) =>\n"
        "            players.First(x => string.Equals(x.playerId, id, StringComparison.OrdinalIgnoreCase));",
        "        public ClubState Club(string id) =>\n"
        "            clubs.First(x => string.Equals(x.clubId, id, StringComparison.OrdinalIgnoreCase));\n\n"
        "        public bool TryGetClub(string id, out ClubState club)\n"
        "        {\n"
        "            club = null;\n"
        "            if (clubs == null || string.IsNullOrWhiteSpace(id))\n"
        "                return false;\n"
        "            club = clubs.FirstOrDefault(x =>\n"
        "                x != null && string.Equals(x.clubId, id, StringComparison.OrdinalIgnoreCase));\n"
        "            return club != null;\n"
        "        }\n\n"
        "        public PlayerCareerState Player(string id) =>\n"
        "            players.First(x => string.Equals(x.playerId, id, StringComparison.OrdinalIgnoreCase));",
    )
    DOMAIN.write_text(text, encoding="utf-8", newline="\n")
    print("patched", DOMAIN)


def patch_apply_match() -> None:
    text = CAREER_APP.read_text(encoding="utf-8")

    # Map QuickMatch winner/loser strictly to the two career club ids passed in
    old_bridge = """            MatchResult result = _simulator.Simulate(_matchContent, clubAId, clubBId, seed);
            string loser = result.winnerTeamId == clubAId ? clubBId : clubAId;
            bool teamAWon = result.winnerTeamId == clubAId;
            PlayerMatchSummary[] winnerPlayers = result.playerSummaries
                .Where(x => x.teamId == result.winnerTeamId).ToArray();
            PlayerMatchSummary[] loserPlayers = result.playerSummaries
                .Where(x => x.teamId == loser).ToArray();
            return new CareerMatchSummary
            {
                winnerClubId = result.winnerTeamId,
                loserClubId = loser,
                winnerScore = teamAWon ? result.teamAScore : result.teamBScore,
                loserScore = teamAWon ? result.teamBScore : result.teamAScore,
                winnerRating = winnerPlayers.Length == 0 ? 1.1f : winnerPlayers.Average(x => x.rating),
                loserRating = loserPlayers.Length == 0 ? .9f : loserPlayers.Average(x => x.rating),
                exposure = 20 + (int)tier * 14
            };"""

    new_bridge = """            MatchResult result = _simulator.Simulate(_matchContent, clubAId, clubBId, seed);
            bool teamAWon = string.Equals(
                result.winnerTeamId, clubAId, StringComparison.OrdinalIgnoreCase);
            string winner = teamAWon ? clubAId : clubBId;
            string loser = teamAWon ? clubBId : clubAId;
            PlayerMatchSummary[] winnerPlayers = result.playerSummaries
                .Where(x => string.Equals(x.teamId, result.winnerTeamId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            PlayerMatchSummary[] loserPlayers = result.playerSummaries
                .Where(x => !string.Equals(x.teamId, result.winnerTeamId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return new CareerMatchSummary
            {
                winnerClubId = winner,
                loserClubId = loser,
                winnerScore = teamAWon ? result.teamAScore : result.teamBScore,
                loserScore = teamAWon ? result.teamBScore : result.teamAScore,
                winnerRating = winnerPlayers.Length == 0 ? 1.1f : winnerPlayers.Average(x => x.rating),
                loserRating = loserPlayers.Length == 0 ? .9f : loserPlayers.Average(x => x.rating),
                exposure = 20 + (int)tier * 14
            };"""

    if old_bridge in text:
        text = text.replace(old_bridge, new_bridge, 1)

    old_apply = """        private void ApplyMatch(CareerMatchSummary summary)
        {
            foreach (PlayerCareerState player in State.players.Where(x =>
                         x.clubId == summary.winnerClubId || x.clubId == summary.loserClubId))
            {
                bool won = player.clubId == summary.winnerClubId;
                player.careerMatches++;
                player.hoursWithoutMatch = 0;
                player.recentRating = won ? summary.winnerRating : summary.loserRating;
                player.ratingTrend = (player.recentRating - 1f) * 10f;
                player.form = Math.Max(0, Math.Min(100, player.form + (won ? 4 : -2)));
                player.energy = Math.Max(0, player.energy - 12);
                player.experience += 3;
            }
            State.Club(summary.winnerClubId).exposure =
                Math.Min(100, State.Club(summary.winnerClubId).exposure + summary.exposure / 5);
            State.Club(summary.loserClubId).exposure =
                Math.Min(100, State.Club(summary.loserClubId).exposure + summary.exposure / 8);
            TryAwardMatchRankingPoints(summary);
            RecordAnalystOpponentMeeting(summary);
            AddEvent(CareerEventType.MatchCompleted, summary.winnerClubId, summary.loserClubId,
                $"{summary.winnerClubId} {summary.winnerScore}:{summary.loserScore} {summary.loserClubId}");
        }"""

    new_apply = """        private void ApplyMatch(CareerMatchSummary summary)
        {
            if (summary == null ||
                string.IsNullOrWhiteSpace(summary.winnerClubId) ||
                string.IsNullOrWhiteSpace(summary.loserClubId))
                return;

            foreach (PlayerCareerState player in State.players.Where(x =>
                         string.Equals(x.clubId, summary.winnerClubId, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(x.clubId, summary.loserClubId, StringComparison.OrdinalIgnoreCase)))
            {
                bool won = string.Equals(player.clubId, summary.winnerClubId, StringComparison.OrdinalIgnoreCase);
                player.careerMatches++;
                player.hoursWithoutMatch = 0;
                player.recentRating = won ? summary.winnerRating : summary.loserRating;
                player.ratingTrend = (player.recentRating - 1f) * 10f;
                player.form = Math.Max(0, Math.Min(100, player.form + (won ? 4 : -2)));
                player.energy = Math.Max(0, player.energy - 12);
                player.experience += 3;
            }

            // Bracket may reference clubs missing from older saves — never throw here.
            if (State.TryGetClub(summary.winnerClubId, out ClubState winnerClub))
                winnerClub.exposure = Math.Min(100, winnerClub.exposure + summary.exposure / 5);
            if (State.TryGetClub(summary.loserClubId, out ClubState loserClub))
                loserClub.exposure = Math.Min(100, loserClub.exposure + summary.exposure / 8);

            TryAwardMatchRankingPoints(summary);
            RecordAnalystOpponentMeeting(summary);
            AddEvent(CareerEventType.MatchCompleted, summary.winnerClubId, summary.loserClubId,
                $"{summary.winnerClubId} {summary.winnerScore}:{summary.loserScore} {summary.loserClubId}");
        }"""

    if old_apply not in text:
        raise RuntimeError("ApplyMatch block missing")
    text = text.replace(old_apply, new_apply, 1)

    CAREER_APP.write_text(text, encoding="utf-8", newline="\n")
    print("patched", CAREER_APP)


def patch_career_start() -> None:
    text = CAREER_UI.read_text(encoding="utf-8")
    if "套用賽事結果時發生錯誤" in text:
        print("Start guard already present")
        return

    old = """            if (_session.TryApplyOfficialMatchResult(result, _matchContent))
            {
                _persistence.Save(_session.State, 1);
                ApplyOfficialMatchReturnUi();
            }
            else
                SetStatus("無法套用賽事結果，已載入存檔");

            RefreshAll();
            StartCoroutine(DeferredOfficialMatchReturnRefresh());
        }

        private IEnumerator DeferredOfficialMatchReturnRefresh()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (!_careerStarted || _session == null)
                yield break;
            RefreshAll();
            RefreshTimeControlVisuals();
        }"""

    new = """            try
            {
                if (_session.TryApplyOfficialMatchResult(result, _matchContent))
                {
                    _persistence.Save(_session.State, 1);
                    ApplyOfficialMatchReturnUi();
                }
                else
                    SetStatus("無法套用賽事結果，已載入存檔");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                SetStatus("套用賽事結果時發生錯誤，已載入存檔");
            }

            // Always refresh header/logo even if match apply failed mid-way.
            RefreshAll();
            StartCoroutine(DeferredOfficialMatchReturnRefresh());
        }

        private IEnumerator DeferredOfficialMatchReturnRefresh()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (!_careerStarted || _session == null)
                yield break;
            RefreshAll();
            RefreshTimeControlVisuals();
            yield return null;
            Canvas.ForceUpdateCanvases();
            RefreshAll();
        }"""

    text = must_replace(text, old, new)

    # Harden RefreshAll against missing club
    old_refresh = """            if (!_careerStarted || _session == null) return;
            CareerState state = _session.State;
            ClubState club = state.Club(state.playerClubId);

            if (_clubLogoImage != null)
                _clubLogoImage.sprite = LoadClubLogo(state.playerClubId);
            if (_cashText != null)
            {
                _cashText.text = $"${club.cash:N0}";
                _cashText.color = club.cash < 0 ? Danger : TextPrimary;
            }
            if (_fansText != null)
                _fansText.text = club.fans.ToString("N0");
            if (_prestigeText != null)
                _prestigeText.text = club.exposure.ToString();
            if (_fameText != null)
                _fameText.text = club.fame.ToString();
            if (_rankText != null)
            {
                int rank = RankFor(club.clubId);
                _rankText.text = rank > 0 ? $"#{rank}" : "#—";
                _rankText.color = RankGold;
            }"""

    new_refresh = """            if (!_careerStarted || _session == null) return;
            CareerState state = _session.State;
            if (!state.TryGetClub(state.playerClubId, out ClubState club) || club == null)
                return;

            if (_clubLogoImage != null)
            {
                _clubLogoImage.sprite = LoadClubLogo(state.playerClubId);
                _clubLogoImage.color = Color.white;
                _clubLogoImage.enabled = true;
            }
            if (_cashText != null)
            {
                _cashText.text = $"${club.cash:N0}";
                _cashText.color = club.cash < 0 ? Danger : TextPrimary;
            }
            if (_fansText != null)
                _fansText.text = club.fans.ToString("N0");
            if (_prestigeText != null)
                _prestigeText.text = club.exposure.ToString();
            if (_fameText != null)
                _fameText.text = club.fame.ToString();
            if (_rankText != null)
            {
                int rank = RankFor(club.clubId);
                _rankText.text = rank > 0 ? $"#{rank}" : "#—";
                _rankText.color = RankGold;
            }"""

    if old_refresh in text:
        text = text.replace(old_refresh, new_refresh, 1)

    CAREER_UI.write_text(text, encoding="utf-8", newline="\n")
    print("patched", CAREER_UI)


def main() -> None:
    patch_quickmatch()
    patch_domain_club_lookup()
    patch_apply_match()
    patch_career_start()
    print("done")


if __name__ == "__main__":
    main()
