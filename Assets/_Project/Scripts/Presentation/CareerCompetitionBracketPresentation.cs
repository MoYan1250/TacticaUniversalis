using System;
using System.Collections.Generic;
using TacticaUniversalis.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    public sealed partial class CareerDemoController
    {
        private static readonly Color BracketBg = CareerEditorialChrome.Paper;
        private static readonly Color BracketCard = CareerEditorialChrome.Paper;
        private static readonly Color BracketWinBar = CareerEditorialChrome.Mustard;
        private static readonly Color BracketLoseBar = CareerEditorialChrome.Red;
        private static readonly Color BracketPendingBar = CareerEditorialChrome.Ink;
        private static readonly Color BracketLoseText = CareerEditorialChrome.InkMuted;
        private static readonly Color BracketLine = CareerEditorialChrome.Ink;

        private const float HltvMatchHeight = 56f;
        private const float HltvMatchWidth = 210f;
        private const float HltvColumnGap = 56f;
        private const float HltvRound0Pitch = 92f;
        private const float HltvTitleBand = 28f;
        private const float HltvPad = 12f;

        private void PopulateCompetitionBracketUi(string competitionId)
        {
            if (_competitionDetailBracketRoot == null)
                return;
            ClearTransformChildren(_competitionDetailBracketRoot);

            if (_session == null || string.IsNullOrEmpty(competitionId))
                return;

            CompetitionBracketSnapshot snapshot = _session.GetCompetitionBracketSnapshot(competitionId);
            if (snapshot.usesMajorStructure &&
                (snapshot.majorStage == MajorStageKind.ChallengersSwiss ||
                 snapshot.majorStage == MajorStageKind.LegendsSwiss ||
                 snapshot.majorStage == MajorStageKind.None))
            {
                AddBracketSectionTitle(_competitionDetailBracketRoot,
                    string.IsNullOrEmpty(snapshot.majorStageLabel) ? "Major 賽制" : snapshot.majorStageLabel);
                if (snapshot.swissStandings != null && snapshot.swissStandings.Count > 0)
                    AddSwissStandingsTable(_competitionDetailBracketRoot, snapshot);
                else
                    AddBracketPlaceholder(_competitionDetailBracketRoot,
                        "由地區 Major 排名取得資格後，挑戰者與傳奇階段將顯示瑞士制戰績表。");
                if (snapshot.swissPairings != null && snapshot.swissPairings.Count > 0)
                    AddSwissPairingsRow(_competitionDetailBracketRoot, snapshot);
                LayoutRebuilder.ForceRebuildLayoutImmediate(_competitionDetailBracketRoot);
                return;
            }

            if ((snapshot.rounds == null || snapshot.rounds.Count == 0) &&
                (snapshot.firstRound == null || snapshot.firstRound.Count == 0))
            {
                AddBracketPlaceholder(_competitionDetailBracketRoot, "對陣圖將於隊伍陸續報名後顯示。");
                LayoutRebuilder.ForceRebuildLayoutImmediate(_competitionDetailBracketRoot);
                return;
            }

            if (snapshot.usesMajorStructure && !string.IsNullOrEmpty(snapshot.majorStageLabel))
                AddBracketSectionTitle(_competitionDetailBracketRoot, snapshot.majorStageLabel);

            AddBracketProgressRow(_competitionDetailBracketRoot, snapshot);
            BuildHltvStyleBracket(_competitionDetailBracketRoot, snapshot);

            bool hasPlayerNotes = !string.IsNullOrEmpty(snapshot.currentOpponentClubId) ||
                                  !string.IsNullOrEmpty(snapshot.nextRoundHint);
            if (hasPlayerNotes)
            {
                AddBracketSectionTitle(_competitionDetailBracketRoot, "你的賽程");
                if (!string.IsNullOrEmpty(snapshot.currentOpponentClubId))
                    AddBracketInfoChip(_competitionDetailBracketRoot,
                        $"本場對手：{BracketClubLabel(snapshot.currentOpponentClubId)}");
                if (!string.IsNullOrEmpty(snapshot.nextRoundHint))
                    AddBracketInfoChip(_competitionDetailBracketRoot, snapshot.nextRoundHint);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_competitionDetailBracketRoot);
        }

        private void BuildHltvStyleBracket(Transform parent, CompetitionBracketSnapshot snapshot)
        {
            List<CompetitionBracketRoundView> rounds = snapshot.rounds;
            if (rounds == null || rounds.Count == 0)
            {
                rounds = CompetitionRegistrationRules.BuildRoundViews(
                    FlattenFirstRound(snapshot.firstRound),
                    _session.State.playerClubId,
                    null);
            }

            if (rounds.Count == 0 || rounds[0].matches.Count == 0)
            {
                AddBracketPlaceholder(parent, "對陣圖將於隊伍陸續報名後顯示。");
                return;
            }

            int firstMatches = rounds[0].matches.Count;
            float contentHeight = HltvTitleBand + HltvPad * 2f +
                                  Math.Max(1, firstMatches - 1) * HltvRound0Pitch + HltvMatchHeight;
            float contentWidth = HltvPad * 2f +
                                 rounds.Count * HltvMatchWidth +
                                 Math.Max(0, rounds.Count - 1) * HltvColumnGap;

            Image host = CreateUiObject<Image>("HltvBracketHost", parent);
            host.color = BracketBg;
            var hostLayout = host.gameObject.AddComponent<LayoutElement>();
            hostLayout.minHeight = contentHeight + 8f;
            hostLayout.preferredHeight = contentHeight + 8f;
            hostLayout.flexibleWidth = 1f;

            // Nested horizontal scroll so wide brackets stay usable.
            // NestedScrollRect forwards vertical drag/wheel to the competition detail scroller.
            NestedScrollRect hScroll = host.gameObject.AddComponent<NestedScrollRect>();
            hScroll.horizontal = true;
            hScroll.vertical = false;
            hScroll.movementType = ScrollRect.MovementType.Clamped;
            hScroll.scrollSensitivity = 48f;
            hScroll.inertia = true;

            Image viewport = CreateUiObject<Image>("Viewport", host.transform);
            Stretch(viewport.rectTransform);
            viewport.rectTransform.offsetMin = new Vector2(4f, 4f);
            viewport.rectTransform.offsetMax = new Vector2(-4f, -4f);
            viewport.color = Color.clear;
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            hScroll.viewport = viewport.rectTransform;

            RectTransform canvas = new GameObject("BracketCanvas", typeof(RectTransform)).GetComponent<RectTransform>();
            canvas.SetParent(viewport.transform, false);
            canvas.anchorMin = new Vector2(0f, 1f);
            canvas.anchorMax = new Vector2(0f, 1f);
            canvas.pivot = new Vector2(0f, 1f);
            canvas.anchoredPosition = Vector2.zero;
            canvas.sizeDelta = new Vector2(contentWidth, contentHeight);
            hScroll.content = canvas;

            float[][] matchTops = new float[rounds.Count][];
            for (int r = 0; r < rounds.Count; r++)
            {
                int count = rounds[r].matches.Count;
                matchTops[r] = new float[count];
                float colX = HltvPad + r * (HltvMatchWidth + HltvColumnGap);

                Text title = CreateText("RoundTitle", canvas, 13, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
                title.rectTransform.anchorMin = new Vector2(0f, 1f);
                title.rectTransform.anchorMax = new Vector2(0f, 1f);
                title.rectTransform.pivot = new Vector2(0f, 1f);
                title.rectTransform.anchoredPosition = new Vector2(colX, -6f);
                title.rectTransform.sizeDelta = new Vector2(HltvMatchWidth, HltvTitleBand - 4f);
                title.text = rounds[r].title;
                title.raycastTarget = false;

                for (int m = 0; m < count; m++)
                {
                    float top = r == 0
                        ? HltvTitleBand + HltvPad + m * HltvRound0Pitch
                        : (matchTops[r - 1][m * 2] + matchTops[r - 1][m * 2 + 1]) * 0.5f;
                    matchTops[r][m] = top;
                    PlaceHltvMatchCard(canvas, rounds[r].matches[m], colX, top);
                }
            }

            for (int r = 0; r < rounds.Count - 1; r++)
            {
                float fromRight = HltvPad + r * (HltvMatchWidth + HltvColumnGap) + HltvMatchWidth;
                float toLeft = fromRight + HltvColumnGap;
                float midX = (fromRight + toLeft) * 0.5f;
                for (int m = 0; m < rounds[r + 1].matches.Count; m++)
                {
                    int a = m * 2;
                    int b = m * 2 + 1;
                    if (b >= matchTops[r].Length)
                        break;
                    float yA = matchTops[r][a] + HltvMatchHeight * 0.5f;
                    float yB = matchTops[r][b] + HltvMatchHeight * 0.5f;
                    float yOut = matchTops[r + 1][m] + HltvMatchHeight * 0.5f;
                    DrawBracketElbow(canvas, fromRight, yA, midX, yB, toLeft, yOut);
                }
            }
        }

        private static List<string> FlattenFirstRound(List<CompetitionBracketPair> firstRound)
        {
            var order = new List<string>();
            if (firstRound == null)
                return order;
            foreach (CompetitionBracketPair pair in firstRound)
            {
                order.Add(pair.clubAId);
                order.Add(pair.clubBId);
            }
            return order;
        }

        private void PlaceHltvMatchCard(
            RectTransform canvas, CompetitionBracketMatchView match, float x, float top)
        {
            Image card = CreateUiObject<Image>("Match", canvas);
            card.color = BracketCard;
            card.raycastTarget = false;
            RectTransform cardRect = card.rectTransform;
            cardRect.anchorMin = new Vector2(0f, 1f);
            cardRect.anchorMax = new Vector2(0f, 1f);
            cardRect.pivot = new Vector2(0f, 1f);
            cardRect.anchoredPosition = new Vector2(x, -top);
            cardRect.sizeDelta = new Vector2(HltvMatchWidth, HltvMatchHeight);

            if (match != null && match.involvesPlayerClub)
            {
                Image outline = CreateUiObject<Image>("PlayerEdge", card.transform);
                Stretch(outline.rectTransform);
                outline.color = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
                outline.raycastTarget = false;
                Image face = CreateUiObject<Image>("Face", card.transform);
                Stretch(face.rectTransform);
                face.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
                face.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
                face.color = BracketCard;
                face.raycastTarget = false;
            }

            PlaceHltvTeamRow(card.transform, match, upper: true);
            PlaceHltvTeamRow(card.transform, match, upper: false);

            Image divider = CreateUiObject<Image>("Divider", card.transform);
            SetRect(divider.rectTransform, new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.52f));
            divider.color = new Color(1f, 1f, 1f, 0.06f);
            divider.raycastTarget = false;
        }

        private void PlaceHltvTeamRow(Transform card, CompetitionBracketMatchView match, bool upper)
        {
            string clubId = match == null ? null : (upper ? match.clubAId : match.clubBId);
            int score = match == null ? -1 : (upper ? match.scoreA : match.scoreB);
            bool completed = match != null && match.completed && !string.IsNullOrEmpty(match.winnerClubId);
            bool isWinner = completed &&
                            !string.IsNullOrEmpty(clubId) &&
                            string.Equals(clubId, match.winnerClubId, StringComparison.OrdinalIgnoreCase);
            bool isLoser = completed && !string.IsNullOrEmpty(clubId) && !isWinner;

            float y0 = upper ? 0.52f : 0.02f;
            float y1 = upper ? 0.98f : 0.48f;

            Image row = CreateUiObject<Image>(upper ? "TeamA" : "TeamB", card);
            SetRect(row.rectTransform, new Vector2(0f, y0), new Vector2(1f, y1));
            row.color = Color.clear;
            row.raycastTarget = false;

            Image bar = CreateUiObject<Image>("Bar", row.transform);
            SetRect(bar.rectTransform, new Vector2(0f, 0.12f), new Vector2(0.025f, 0.88f));
            bar.color = !completed ? BracketPendingBar : isWinner ? BracketWinBar : BracketLoseBar;
            bar.raycastTarget = false;

            Image logo = CreateUiObject<Image>("Logo", row.transform);
            SetRect(logo.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.18f, 0.88f));
            logo.sprite = string.IsNullOrEmpty(clubId) ? null : LoadClubLogoOrNull(clubId);
            logo.preserveAspect = true;
            logo.color = logo.sprite != null
                ? (isLoser ? new Color(1f, 1f, 1f, 0.45f) : Color.white)
                : new Color(1f, 1f, 1f, 0.12f);
            if (!string.IsNullOrEmpty(clubId))
                WireRankingClubLogoButton(logo, clubId);
            else
                logo.raycastTarget = false;

            Color nameColor = isLoser ? BracketLoseText : TextPrimary;
            Text name = CreateText("Name", row.transform, 13, TextAnchor.MiddleLeft,
                nameColor, _uiFont700);
            SetRect(name.rectTransform, new Vector2(0.20f, 0.08f), new Vector2(0.78f, 0.92f));
            name.text = string.IsNullOrEmpty(clubId) ? "TBD" : BracketClubLabel(clubId);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.raycastTarget = false;

            Color scoreColor = isLoser ? BracketLoseText : TextPrimary;
            Text scoreText = CreateText("Score", row.transform, 14, TextAnchor.MiddleRight,
                scoreColor, _uiFont700);
            SetRect(scoreText.rectTransform, new Vector2(0.78f, 0.08f), new Vector2(0.96f, 0.92f));
            scoreText.text = completed && score >= 0 ? score.ToString() : string.Empty;
            scoreText.raycastTarget = false;
        }

        private void DrawBracketElbow(
            RectTransform canvas,
            float fromX, float yA,
            float midX, float yB,
            float toX, float yOut)
        {
            const float thickness = 2f;
            float minY = Math.Min(yA, yB);
            float maxY = Math.Max(yA, yB);

            // Horizontal stubs from each match to the vertical spine.
            AddBracketLine(canvas, fromX, yA - thickness * 0.5f, midX - fromX, thickness);
            AddBracketLine(canvas, fromX, yB - thickness * 0.5f, midX - fromX, thickness);
            // Vertical spine between the two feeds.
            AddBracketLine(canvas, midX - thickness * 0.5f, minY, thickness, maxY - minY);
            // Horizontal out to next match.
            AddBracketLine(canvas, midX, yOut - thickness * 0.5f, toX - midX, thickness);
        }

        private void AddBracketLine(RectTransform canvas, float x, float top, float width, float height)
        {
            if (width <= 0.01f || height <= 0.01f)
                return;
            Image line = CreateUiObject<Image>("Line", canvas);
            line.color = BracketLine;
            line.raycastTarget = false;
            RectTransform rect = line.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -top);
            rect.sizeDelta = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
        }
    }
}
