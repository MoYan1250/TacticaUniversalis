using System;
using System.Linq;
using TacticaUniversalis.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    public sealed partial class CareerDemoController
    {
        private void BuildRankingClubIntelOverlay(Transform canvas)
        {
            Image dim = CreateUiObject<Image>("RankingClubIntelOverlay", canvas);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.82f);
            dim.raycastTarget = true;
            _rankingClubIntelRoot = dim.gameObject;
            _rankingClubIntelRoot.SetActive(false);

            Button closeDim = dim.gameObject.AddComponent<Button>();
            closeDim.targetGraphic = dim;
            closeDim.transition = Selectable.Transition.None;
            closeDim.onClick.AddListener(HideRankingClubIntel);

            // D3 light_chrome: Shadow > Frame > Face + inset well; no blur.
            Image panel = UiLightChromeUtility.CreateCard(
                "IntelPanel", dim.transform,
                new Vector2(0.18f, 0.06f), new Vector2(0.90f, 0.94f), Panel);
            UiChromeMotion.BindFace(_rankingClubIntelRoot, panel);
            Image face = UiLightChromeUtility.CreateInsetFace(panel.transform, PanelInner);

            Image accent = CreateUiObject<Image>("Accent", face.transform);
            SetRect(accent.rectTransform, new Vector2(0f, 0.975f), new Vector2(1f, 1f));
            accent.color = Accent;
            accent.raycastTarget = false;

            Text caption = CreateText("Caption", face.transform, 13, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            SetRect(caption.rectTransform, new Vector2(0.04f, 0.905f), new Vector2(0.40f, 0.96f));
            caption.text = "俱樂部簡報";

            CreateGlassCapsuleButton("CloseIntel", "關閉", face.transform,
                new Vector2(0.78f, 0.905f), new Vector2(0.96f, 0.965f),
                ButtonEdge, ButtonFace, HideRankingClubIntel, _uiFont700);

            Image logoFrame = CreateUiObject<Image>("LogoFrame", face.transform);
            SetRect(logoFrame.rectTransform, new Vector2(0.04f, 0.74f), new Vector2(0.20f, 0.90f));
            UiLightChromeUtility.ApplySoftNavyPanel(logoFrame);

            _rankingClubIntelLogo = CreateUiObject<Image>("Logo", logoFrame.transform);
            SetRect(_rankingClubIntelLogo.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
            _rankingClubIntelLogo.preserveAspect = true;
            _rankingClubIntelLogo.color = Color.white;
            _rankingClubIntelLogo.raycastTarget = false;

            _rankingClubIntelName = CreateText("Name", face.transform, 22, TextAnchor.MiddleLeft, Accent, _uiFont);
            SetRect(_rankingClubIntelName.rectTransform, new Vector2(0.22f, 0.82f), new Vector2(0.96f, 0.90f));
            _rankingClubIntelName.fontStyle = FontStyle.Bold;

            _rankingClubIntelFullName = CreateText("FullName", face.transform, 12, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);
            SetRect(_rankingClubIntelFullName.rectTransform, new Vector2(0.22f, 0.775f), new Vector2(0.96f, 0.82f));

            _rankingClubIntelRegion = CreateText("Region", face.transform, 12, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            SetRect(_rankingClubIntelRegion.rectTransform, new Vector2(0.22f, 0.735f), new Vector2(0.96f, 0.775f));

            Image statsHost = CreateUiObject<Image>("Stats", face.transform);
            SetRect(statsHost.rectTransform, new Vector2(0.04f, 0.44f), new Vector2(0.96f, 0.72f));
            UiLightChromeUtility.ApplySoftNavyPanel(statsHost);

            _rankingClubIntelCash = CreateLabeledIconStat(
                "Cash", statsHost.transform, "UI/Icons/cash", "現金",
                new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.98f), TextPrimary);
            _rankingClubIntelFans = CreateLabeledIconStat(
                "Fans", statsHost.transform, "UI/Icons/fans", "粉絲",
                new Vector2(0.04f, 0.60f), new Vector2(0.96f, 0.78f), TextPrimary);
            _rankingClubIntelPrestige = CreateLabeledIconStat(
                "Prestige", statsHost.transform, "UI/Icons/prestige", "威望",
                new Vector2(0.04f, 0.40f), new Vector2(0.96f, 0.58f), TextPrimary);
            _rankingClubIntelFame = CreateLabeledIconStat(
                "Fame", statsHost.transform, "UI/Icons/fame", "名氣",
                new Vector2(0.04f, 0.20f), new Vector2(0.96f, 0.38f), TextPrimary);
            _rankingClubIntelRank = CreateLabeledIconStat(
                "Rank", statsHost.transform, "UI/Icons/rank", "排名",
                new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.18f), RankGold);

            Text rosterCaption = CreateText("RosterCaption", face.transform, 12, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            SetRect(rosterCaption.rectTransform, new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.43f));
            rosterCaption.text = "陣容（停留 1 秒看屬性）";

            Image rosterHost = CreateUiObject<Image>("Roster", face.transform);
            SetRect(rosterHost.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.37f));
            UiLightChromeUtility.ApplySoftNavyPanel(rosterHost);
            _rankingClubIntelRosterRoot = rosterHost.rectTransform;
        }

        private void WireRankingClubLogoButton(Image logo, string clubId)
        {
            if (logo == null || string.IsNullOrEmpty(clubId))
                return;

            logo.raycastTarget = true;
            Button existing = logo.GetComponent<Button>();
            if (existing != null)
                DestroyImmediate(existing);

            Button btn = logo.gameObject.AddComponent<Button>();
            btn.targetGraphic = logo;
            btn.transition = Selectable.Transition.None;
            string captured = clubId;
            btn.onClick.AddListener(() => OpenRankingClubIntel(captured));
        }

        private void OpenRankingClubIntel(string clubId)
        {
            if (!_careerStarted || _session == null || string.IsNullOrEmpty(clubId))
                return;

            HideTitleHoverPopup();
            CancelTitleHover();
            CancelStatTooltip();
            HideStatTooltip();

            RefreshRankingClubIntel(clubId);
            if (_rankingClubIntelRoot == null)
                return;

            UiChromeMotion.Show(_rankingClubIntelRoot);
            _rankingClubIntelRoot.transform.SetAsLastSibling();
        }

        private void HideRankingClubIntel()
        {
            HideTitleHoverPopup();
            CancelTitleHover();
            UiChromeMotion.Hide(_rankingClubIntelRoot);
        }

        private void RefreshRankingClubIntel(string clubId)
        {
            CareerClubDefinition clubDef = null;
            if (_content != null)
                _content.Clubs.TryGetValue(clubId, out clubDef);
            ClubState clubState = _session?.State.clubs.FirstOrDefault(x =>
                string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase));

            if (_rankingClubIntelLogo != null)
            {
                _rankingClubIntelLogo.sprite = LoadClubLogoOrNull(clubId);
                _rankingClubIntelLogo.color = _rankingClubIntelLogo.sprite != null
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.2f);
            }

            if (_rankingClubIntelName != null)
                _rankingClubIntelName.text = ClubDisplayName(clubId);
            if (_rankingClubIntelFullName != null)
                _rankingClubIntelFullName.text = ClubFullDisplayName(clubId);
            if (_rankingClubIntelRegion != null)
                _rankingClubIntelRegion.text = ClubRegionLabel(clubId);

            int cash = clubState?.cash ?? (clubDef != null && clubDef.startingCash > 0
                ? clubDef.startingCash
                : _content?.Balance.startingCash ?? 0);
            int fans = clubState?.fans ?? (clubDef != null ? 15000 + clubDef.fame * 1000 : 0);
            int prestige = clubState?.exposure ?? clubDef?.fame ?? 0;
            int fame = clubState?.fame ?? clubDef?.fame ?? 0;
            int rank = RankFor(clubId);

            if (_rankingClubIntelCash != null)
            {
                _rankingClubIntelCash.text = $"${cash:N0}";
                _rankingClubIntelCash.color = cash < 0 ? Danger : TextPrimary;
            }
            if (_rankingClubIntelFans != null)
                _rankingClubIntelFans.text = fans.ToString("N0");
            if (_rankingClubIntelPrestige != null)
                _rankingClubIntelPrestige.text = prestige.ToString();
            if (_rankingClubIntelFame != null)
                _rankingClubIntelFame.text = fame.ToString();
            if (_rankingClubIntelRank != null)
            {
                _rankingClubIntelRank.text = rank > 0 ? $"#{rank}" : "#—";
                _rankingClubIntelRank.color = RankGold;
            }

            RebuildClubIntelRoster(
                _rankingClubIntelRosterRoot,
                _rankingClubIntelRosterRows,
                clubId,
                includePlayerLoreInHover: false);
        }
    }
}
