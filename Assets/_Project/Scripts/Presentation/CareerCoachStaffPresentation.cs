using System;
using System.Collections.Generic;
using System.Linq;
using TacticaUniversalis.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    public sealed partial class CareerDemoController
    {
        private string _selectedCoachId;
        private GameObject _coachDetailRoot;
        private Text _coachDetailCaption;
        private ScrollRect _coachDetailScroll;
        private RectTransform _coachDetailContent;

        private void BuildCoachStaffDetailOverlay(Transform canvas)
        {
            Image backdrop = CreateUiObject<Image>("CoachDetailOverlay", canvas);
            Stretch(backdrop.rectTransform);
            backdrop.color = new Color(0f, 0f, 0f, 0.78f);
            backdrop.raycastTarget = true;
            _coachDetailRoot = backdrop.gameObject;

            Button dismiss = backdrop.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = backdrop;
            ColorBlock colors = dismiss.colors;
            colors.normalColor = backdrop.color;
            colors.highlightedColor = backdrop.color;
            colors.pressedColor = backdrop.color;
            colors.selectedColor = backdrop.color;
            dismiss.colors = colors;
            dismiss.onClick.AddListener(CloseCoachDetail);

            // D3 light_chrome: Shadow > Frame > Face + inset well; no blur.
            Image card = UiLightChromeUtility.CreateCard(
                "DetailCard", backdrop.transform,
                new Vector2(0.16f, 0.04f), new Vector2(0.96f, 0.96f), Panel);
            UiChromeMotion.BindFace(_coachDetailRoot, card);
            Image cardFace = UiLightChromeUtility.CreateInsetFace(card.transform, PanelInner);

            _coachDetailCaption = CreateText("Caption", cardFace.transform, 20, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(_coachDetailCaption.rectTransform, new Vector2(0.03f, 0.93f), new Vector2(0.70f, 0.985f));
            _coachDetailCaption.text = "個人資料";

            CreateGlassCapsuleButton("CloseCoachDetail", "關閉", cardFace.transform,
                new Vector2(0.84f, 0.935f), new Vector2(0.97f, 0.98f), ButtonEdge, ButtonFace, CloseCoachDetail,
                _uiFont700);

            Image scrollHost = CreateUiObject<Image>("DetailScrollHost", cardFace.transform);
            SetRect(scrollHost.rectTransform, new Vector2(0.025f, 0.025f), new Vector2(0.975f, 0.92f));
            scrollHost.color = Color.clear;
            scrollHost.raycastTarget = true;

            _coachDetailScroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            _coachDetailScroll.horizontal = false;
            _coachDetailScroll.vertical = true;
            _coachDetailScroll.movementType = ScrollRect.MovementType.Clamped;
            _coachDetailScroll.scrollSensitivity = 48f;

            Image viewport = CreateUiObject<Image>("Viewport", scrollHost.transform);
            Stretch(viewport.rectTransform);
            viewport.color = Color.clear;
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            _coachDetailScroll.viewport = viewport.rectTransform;

            _coachDetailContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _coachDetailContent.SetParent(viewport.transform, false);
            _coachDetailContent.anchorMin = new Vector2(0f, 1f);
            _coachDetailContent.anchorMax = new Vector2(1f, 1f);
            _coachDetailContent.pivot = new Vector2(0.5f, 1f);
            _coachDetailContent.anchoredPosition = Vector2.zero;
            _coachDetailContent.sizeDelta = Vector2.zero;

            var layout = _coachDetailContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 6f;
            layout.padding = new RectOffset(8, 12, 6, 14);

            var fitter = _coachDetailContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _coachDetailScroll.content = _coachDetailContent;

            CloseCoachDetail();
        }

        private void DrawCoachStaffPanel()
        {
            if (!AddCollapsibleSection("base_coaches", "教練組管理"))
                return;

            if (_session == null)
            {
                AddMutedLine("請先開始生涯。");
                return;
            }

            AddMutedLine("點教練名查看個人資訊 · 主教練／助教／分析師皆屬組織教練組");

            string clubId = _session.State.playerClubId;
            CoachStaffState[] staff = ClubCoachStaffRules.ClubCoaches(_session.State, clubId)
                .OrderBy(c => (int)ClubCoachStaffRules.ParseRole(c.role))
                .ThenBy(c => c.coachId, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (staff.Length == 0)
            {
                AddMutedLine("尚未聘用教練組。NAVI／Gambit 開局應有預設三人組。");
                return;
            }

            AddCoachStaffCardRow(staff);
        }

        private void AddCoachStaffCardRow(CoachStaffState[] staff)
        {
            if (staff == null || staff.Length == 0)
            {
                AddMutedLine("無教練。");
                return;
            }

            for (int i = 0; i < staff.Length; i++)
            {
                RectTransform row = AddRow(148f);
                CreateCoachStaffCard(row, staff[i], 0f, 1f);
            }
        }

        private void CreateCoachStaffCard(RectTransform row, CoachStaffState coach, float x0, float x1)
        {
            Image card = CreateUiObject<Image>("CoachCard_" + coach.coachId, row);
            SetRect(card.rectTransform, new Vector2(x0, 0.04f), new Vector2(x1, 0.96f));
            card.color = CareerEditorialChrome.Paper;
            card.raycastTarget = true;

            string displayName = coach.coachId;
            if (_content.Coaches.TryGetValue(coach.coachId, out CareerCoachDefinition def) &&
                !string.IsNullOrWhiteSpace(def.displayName))
                displayName = def.displayName;

            string coachId = coach.coachId;
            Button open = card.gameObject.AddComponent<Button>();
            open.targetGraphic = card;
            open.transition = Selectable.Transition.None;
            open.onClick.AddListener(() => OpenCoachDetail(coachId));

            Text name = CreateText("Name", card.transform, 32, TextAnchor.MiddleLeft, CareerEditorialChrome.Ink, _uiFontBlack);
            SetRect(name.rectTransform, new Vector2(0.03f, 0.62f), new Vector2(0.62f, 0.94f));
            name.text = displayName;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.verticalOverflow = VerticalWrapMode.Overflow;
            name.raycastTarget = false;

            CareerEditorialChrome.CreateBlock(
                card.transform, "NameRule",
                new Vector2(0.03f, 0.58f), new Vector2(0.22f, 0.61f),
                CareerEditorialChrome.Mustard);

            CoachStaffRole role = ClubCoachStaffRules.ParseRole(coach.role);
            string[] headers;
            string[] values;
            bool[] numeric;
            if (role == CoachStaffRole.Assistant)
            {
                headers = new[] { "執行", "設計", "配合", "薪資", "精力" };
                values = new[]
                {
                    coach.trainingExecution.ToString(),
                    coach.tacticalDesign.ToString(),
                    coach.coachSynergy.ToString(),
                    $"${coach.monthlySalary:N0}",
                    coach.energy.ToString()
                };
                numeric = new[] { true, true, true, true, true };
            }
            else if (role == CoachStaffRole.Analyst)
            {
                headers = new[] { "調查", "趨勢", "積累", "薪資", "精力" };
                values = new[]
                {
                    coach.dataInvestigation.ToString(),
                    coach.trendAnalysis.ToString(),
                    coach.dataAccumulation.ToString(),
                    $"${coach.monthlySalary:N0}",
                    coach.energy.ToString()
                };
                numeric = new[] { true, true, true, true, true };
            }
            else
            {
                headers = new[] { "威望", "名氣", "經驗", "薪資", "精力" };
                values = new[]
                {
                    coach.prestige.ToString(),
                    coach.fame.ToString(),
                    coach.experience.ToString(),
                    $"${coach.monthlySalary:N0}",
                    coach.energy.ToString()
                };
                numeric = new[] { true, true, true, true, true };
            }

            float[] weights = NormalizeColumnWeights(null, headers.Length);
            PlaceInlineTable(card.rectTransform, headers, values, numeric, weights, 0.52f, 0.68f, 0.22f, 0.50f);

            Text roleHint = CreateText("Role", card.transform, 11, TextAnchor.MiddleCenter, TextMuted, _uiFont700);
            SetRect(roleHint.rectTransform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.14f));
            string trust = ClubCoachStaffRules.IsLowPrestige(coach) ? " · 信任風險" : string.Empty;
            roleHint.text = ClubCoachStaffRules.RoleLabel(role) + trust;
        }

        private void OpenCoachDetail(string coachId)
        {
            if (string.IsNullOrEmpty(coachId) || _session == null)
                return;
            CoachStaffState coach = _session.State.coaches?.FirstOrDefault(c =>
                string.Equals(c.coachId, coachId, StringComparison.OrdinalIgnoreCase));
            if (coach == null)
                return;

            ClosePlayerDetail();
            _selectedCoachId = coachId;
            RefreshCoachDetail(scrollToTop: true);
            if (_coachDetailRoot != null)
            {
                UiChromeMotion.Show(_coachDetailRoot);
                _coachDetailRoot.transform.SetAsLastSibling();
            }
        }

        private void CloseCoachDetail()
        {
            _selectedCoachId = null;
            UiChromeMotion.Hide(_coachDetailRoot);
        }

        private void RefreshCoachDetail(bool scrollToTop = false)
        {
            if (_coachDetailRoot == null || string.IsNullOrEmpty(_selectedCoachId) || _session == null)
                return;

            float scrollPos = !scrollToTop && _coachDetailScroll != null
                ? _coachDetailScroll.verticalNormalizedPosition
                : 1f;

            CoachStaffState coach = _session.State.coaches?.FirstOrDefault(c =>
                string.Equals(c.coachId, _selectedCoachId, StringComparison.OrdinalIgnoreCase));
            if (coach == null)
            {
                CloseCoachDetail();
                return;
            }

            _content.Coaches.TryGetValue(coach.coachId, out CareerCoachDefinition def);
            string name = def != null && !string.IsNullOrWhiteSpace(def.displayName)
                ? def.displayName
                : coach.coachId;
            if (_coachDetailCaption != null)
            {
                _coachDetailCaption.font = _playerIdFont;
                _coachDetailCaption.fontStyle = FontStyle.Normal;
                _coachDetailCaption.text = name;
            }

            ClearCoachDetailContent();
            _activeDetailContent = _coachDetailContent;
            BuildCoachDetailContent(coach, def);

            Canvas.ForceUpdateCanvases();
            if (_coachDetailContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_coachDetailContent);
            if (_coachDetailScroll != null)
                _coachDetailScroll.verticalNormalizedPosition = scrollPos;
        }

        private void ClearCoachDetailContent()
        {
            if (_coachDetailContent == null)
                return;
            for (int i = _coachDetailContent.childCount - 1; i >= 0; i--)
                DestroyImmediate(_coachDetailContent.GetChild(i).gameObject);
        }

        private void BuildCoachDetailContent(CoachStaffState coach, CareerCoachDefinition def)
        {
            CoachStaffRole role = ClubCoachStaffRules.ParseRole(coach.role);

            AddProfileSection("個人資料");
            AddProfileTable(
                new[] { "職位", "年齡", "在隊月數", "合約剩餘" },
                new[]
                {
                    ClubCoachStaffRules.RoleLabel(role),
                    coach.age.ToString(),
                    coach.monthsAtClub.ToString(),
                    coach.remainingMonths > 0 ? $"{coach.remainingMonths} 月" : "即將到期"
                },
                new[] { false, true, true, false },
                new[] { 1.2f, 0.8f, 1f, 1.2f },
                headerHeight: 32f,
                valueHeight: 40f);

            if (role == CoachStaffRole.HeadCoach)
                BuildHeadCoachAttributes(coach);
            else if (role == CoachStaffRole.Assistant)
                BuildAssistantAttributes(coach);
            else
                BuildAnalystAttributes(coach);

            AddProfileSection("當前情況");
            if (role == CoachStaffRole.HeadCoach)
            {
                AddProfileTable(
                    new[] { "精力", "忠誠", "月薪", "信心", "壓力", "健康" },
                    new[]
                    {
                        coach.energy.ToString(),
                        coach.loyalty.ToString(),
                        $"${coach.monthlySalary:N0}",
                        coach.confidence.ToString(),
                        coach.pressure.ToString(),
                        coach.health.ToString()
                    },
                    new[] { true, true, true, true, true, true },
                    headerHeight: 30f,
                    valueHeight: 38f);
            }
            else
            {
                AddProfileTable(
                    new[] { "精力", "忠誠", "月薪" },
                    new[]
                    {
                        coach.energy.ToString(),
                        coach.loyalty.ToString(),
                        $"${coach.monthlySalary:N0}"
                    },
                    new[] { true, true, true },
                    headerHeight: 30f,
                    valueHeight: 38f);
            }

            AddProfileSection("評級系統");
            if (role == CoachStaffRole.HeadCoach)
            {
                AddProfileTable(
                    new[] { "威望", "名氣", "經驗" },
                    new[]
                    {
                        FormatCoachRated(coach.prestige),
                        FormatCoachRated(coach.fame),
                        FormatCoachRated(coach.experience)
                    },
                    new[] { true, true, true },
                    headerHeight: 30f,
                    valueHeight: 38f);
                if (ClubCoachStaffRules.IsLowPrestige(coach))
                    AddProfileMutedLine("威望過低：選手可能不信任教練組調度。");
                AddProfileMutedLine("經驗會成長，並可能改變屬性。");
            }
            else
            {
                AddProfileTable(
                    new[] { "經驗" },
                    new[] { FormatCoachRated(coach.experience) },
                    new[] { true },
                    headerHeight: 30f,
                    valueHeight: 38f);
                if (role == CoachStaffRole.Assistant)
                {
                    if (ClubCoachStaffRules.CanPromoteAssistantToHeadCoach(coach))
                        AddProfileMutedLine("已達接班門檻：可轉職為主教練，屬性依助教型態重分配。");
                    else
                        AddProfileMutedLine(
                            $"潛在接班人：經驗達 {ClubCoachStaffRules.AssistantPromoteExperience} 後可轉職主教練，目前 {coach.experience}。");
                }
            }

            if (role == CoachStaffRole.Analyst || role == CoachStaffRole.HeadCoach)
                BuildOpponentReadSection(coach);

            if (role == CoachStaffRole.Analyst)
            {
                AddProfileMutedLine("困難模式：選手多數能力值隱藏時，可依數據調查／星探深度揭露部分對手數據。");
                AddProfileMutedLine("遊戲版本更新時，分析師的數據積累會自動下降；版本適應可減緩損失。");
            }

            AddProfileSection("簡介");
            string bio = def != null && !string.IsNullOrWhiteSpace(def.bio)
                ? def.bio
                : "教練組成員：影響訓練、戰術與對手情報。";
            AddProfileTrait("教練簡介", bio);

            if (role == CoachStaffRole.Assistant &&
                ClubCoachStaffRules.CanPromoteAssistantToHeadCoach(coach))
            {
                AddProfileSection("人事");
                RectTransform actionRow = CreateCoachActionRow();
                CreateGlassCapsuleButton(
                    "PromoteAssistant", "晉升為主教練", actionRow,
                    new Vector2(0.28f, 0.12f), new Vector2(0.72f, 0.88f),
                    AccentDim, Accent, OnPromoteAssistantClicked, _uiFont700, 14);
            }
        }

        private static string FormatCoachRated(int value) =>
            ClubCoachStaffRules.Clamp(value).ToString();

        private void BuildHeadCoachAttributes(CoachStaffState coach)
        {
            AddProfileSection("暫停屬性");
            AddProfileTable(
                new[] { "戰術", "激勵", "分析", "觀察" },
                new[]
                {
                    FormatCoachRated(coach.tactics),
                    FormatCoachRated(coach.motivation),
                    FormatCoachRated(coach.analysis),
                    FormatCoachRated(coach.observation)
                },
                new[] { true, true, true, true },
                headerHeight: 30f,
                valueHeight: 38f);
            AddProfileMutedLine("戰術卡加成 · 喊話／心態 · 解讀對手 · 暫時提升選手屬性／心態");

            AddProfileSection("後備屬性");
            AddProfileTable(
                new[] { "研究", "陣容", "訓練", "管理" },
                new[]
                {
                    FormatCoachRated(coach.research),
                    FormatCoachRated(coach.lineup),
                    FormatCoachRated(coach.training),
                    FormatCoachRated(coach.management)
                },
                new[] { true, true, true, true },
                headerHeight: 30f,
                valueHeight: 38f);
            AddProfileMutedLine("開局 buff · 減緩位置不擅 · 訓練效率 · 衝突／忠誠維持");
        }

        private void BuildAssistantAttributes(CoachStaffState coach)
        {
            AddProfileSection("助教屬性");
            AddProfileTable(
                new[] { "訓練執行", "戰術設計", "錄像復盤", "教練配合" },
                new[]
                {
                    FormatCoachRated(coach.trainingExecution),
                    FormatCoachRated(coach.tacticalDesign),
                    FormatCoachRated(coach.vodReview),
                    FormatCoachRated(coach.coachSynergy)
                },
                new[] { true, true, true, true },
                new[] { 1.1f, 1.1f, 1.1f, 1.1f },
                headerHeight: 36f,
                valueHeight: 40f);
            AddProfileMutedLine("訓練落地質量 · 研究／開局準備 · 復盤質量 · 低配合度使戰術卡打折");
        }

        private void BuildAnalystAttributes(CoachStaffState coach)
        {
            AddProfileSection("分析師屬性");
            AddProfileTable(
                new[] { "數據調查", "趨勢分析", "版本適應", "數據積累" },
                new[]
                {
                    FormatCoachRated(coach.dataInvestigation),
                    FormatCoachRated(coach.trendAnalysis),
                    FormatCoachRated(coach.patchAdaptation),
                    FormatCoachRated(coach.dataAccumulation)
                },
                new[] { true, true, true, true },
                new[] { 1.1f, 1.1f, 1.1f, 1.1f },
                headerHeight: 36f,
                valueHeight: 40f);

            int intelCount = coach.opponentIntel?.Count(x => x != null && x.meetings > 0) ?? 0;
            AddProfileKeyValue("對手熟悉檔", intelCount > 0 ? $"{intelCount} 隊有紀錄" : "尚無交手積累", false);
        }

        private void BuildOpponentReadSection(CoachStaffState coach)
        {
            AddProfileSection("解讀對手戰術");
            CoachStaffState analyst = ClubCoachStaffRules.FindByRole(
                                          _session.State, coach.clubId, CoachStaffRole.Analyst)
                                      ?? coach;
            int skill = ClubCoachStaffRules.ParseRole(analyst.role) == CoachStaffRole.Analyst
                ? analyst.trendAnalysis
                : analyst.analysis;
            int awareness = ClubCoachStaffRules.ResolveIglAwareness(
                _session.State, _content.Players, coach.clubId);
            var rng = new SeededRandom(
                _session.State.seed ^ (_session.State.clockHour * 397) ^
                (analyst.coachId ?? string.Empty).GetHashCode());
            OpponentReadOutcome outcome = ClubCoachStaffRules.RollOpponentRead(skill, awareness, rng);
            string facing = ClubCoachStaffRules.PlayerFacingOpponentReadMessage(outcome);
            if (!string.IsNullOrEmpty(facing))
                AddProfileMutedLine(facing);
            else
                AddProfileMutedLine("本週解讀結果未公開，僅失敗會顯示。");
            AddProfileMutedLine(
                $"參考：趨勢／分析 {skill} · 指揮意識 {awareness} · 成功率約 " +
                $"{ClubCoachStaffRules.OpponentReadSuccessChance(skill, awareness) * 100:0}%");
        }

        private RectTransform CreateCoachActionRow()
        {
            Image row = CreateUiObject<Image>("CoachAction", ActiveDetailContent);
            row.color = PanelRow;
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 44f;
            le.flexibleWidth = 1f;
            return row.rectTransform;
        }

        private void OnPromoteAssistantClicked()
        {
            if (_session == null || string.IsNullOrEmpty(_selectedCoachId))
                return;
            string clubId = _session.State.playerClubId;
            if (!ClubCoachStaffRules.TryPromoteAssistantToHeadCoach(_session.State, clubId, out string failure))
            {
                _status = failure ?? "無法晉升";
                if (_statusText != null) _statusText.text = _status;
                return;
            }

            _status = "助教已晉升為主教練";
            if (_statusText != null) _statusText.text = _status;
            RefreshCoachDetail(scrollToTop: true);
            RefreshAll();
        }
    }
}
