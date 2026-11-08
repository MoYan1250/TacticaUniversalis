using System.Linq;
using TacticaUniversalis.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    public sealed partial class CareerDemoController
    {
        private void DrawCommunityTab()
        {
            DrawTabIntro(
                "社群運營",
                "由運營團隊規劃日常發文，調節俱樂部名氣與外界觀感。",
                "每日最多發布兩則",
                "同一類型每日限一次",
                "名氣反映話題熱度，威望反映外界觀感");

            if (_session == null)
            {
                AddMutedLine("請先開始生涯。");
                return;
            }

            DrawCommunityOperationsPanel();
            DrawCommunityPostHistory();
            DrawPlaceholderSection("comm_fans", "粉絲趨勢", "圖表與長期趨勢分析開發中。");
        }

        private void DrawCommunityOperationsPanel()
        {
            if (!AddCollapsibleSection("comm_ops", "日常發文"))
                return;

            CareerState state = _session.State;
            ClubState club = state.Club(state.playerClubId);
            int day = CommunityOperationsRules.DayIndex(state.clockHour);
            int used = CommunityOperationsRules.PostsOnDay(state.communityPosts, day);
            int remaining = Mathf.Max(0, CommunityOperationsRules.MaxPostsPerDay - used);

            AddMutedLine(
                $"今日配額 {used}/{CommunityOperationsRules.MaxPostsPerDay} · " +
                $"剩餘 {remaining} 則 · 目前名氣 {club.fame} · 威望 {club.exposure} · 粉絲 {club.fans:N0}");

            bool autoDaily = state.communityAutoDailyUpdate;
            RectTransform autoRow = AddRow(56f);
            AddRowLabel(
                autoRow, 0.02f, 0.70f,
                "自動日常動態",
                TextPrimary, 13, TextAnchor.MiddleLeft, 0.52f, 0.92f);
            AddRowLabel(
                autoRow, 0.02f, 0.70f,
                "每日跨日後自動發布一則日常動態，仍受每日配額與同類型限制",
                TextMuted, 11, TextAnchor.UpperLeft, 0.08f, 0.50f, wrap: true);
            Button autoBtn = CreateGlassCapsuleButton(
                "AutoDailyToggle",
                autoDaily ? "已開啟" : "已關閉",
                autoRow,
                new Vector2(0.72f, 0.14f),
                new Vector2(0.96f, 0.86f),
                ButtonEdge,
                autoDaily ? AccentDim : ButtonFace,
                () => OnToggleCommunityAutoDaily(),
                _uiFont700,
                13);

            foreach (CommunityPostTemplate template in CommunityOperationsRules.AllTemplates)
            {
                bool canPublish = CommunityOperationsRules.CanPublish(state, club, template, out string reason);
                RectTransform row = AddRow(canPublish ? 78f : 92f);
                AddRowLabel(
                    row, 0.02f, 0.70f, template.title,
                    TextPrimary, 14, TextAnchor.MiddleLeft, 0.68f, 0.96f);
                AddRowLabel(
                    row, 0.02f, 0.70f, template.description,
                    TextMuted, 12, TextAnchor.UpperLeft, 0.38f, 0.66f, wrap: true);

                if (!canPublish && !string.IsNullOrEmpty(reason))
                {
                    AddRowLabel(
                        row, 0.02f, 0.70f,
                        CommunityOperationsRules.FormatEffectPreview(template),
                        ProfileValue, 11, TextAnchor.MiddleLeft, 0.20f, 0.38f);
                    AddRowLabel(
                        row, 0.02f, 0.70f, reason,
                        Warning, 11, TextAnchor.MiddleLeft, 0.04f, 0.20f);
                }
                else
                {
                    AddRowLabel(
                        row, 0.02f, 0.70f,
                        CommunityOperationsRules.FormatEffectPreview(template),
                        ProfileValue, 12, TextAnchor.MiddleLeft, 0.06f, 0.36f);
                }

                string templateId = template.id;
                bool publish = canPublish;
                Button btn = CreateGlassCapsuleButton(
                    "Publish_" + templateId,
                    publish ? "發布" : "不可用",
                    row,
                    new Vector2(0.72f, 0.14f),
                    new Vector2(0.96f, 0.86f),
                    ButtonEdge,
                    publish ? AccentDim : ButtonFace,
                    () => OnPublishCommunityPost(templateId),
                    _uiFont700,
                    13);
                btn.interactable = publish;
            }
        }

        private void DrawCommunityPostHistory()
        {
            if (!AddCollapsibleSection("comm_history", "近期動態"))
                return;

            CommunityPostLogEntry[] recent = (_session.State.communityPosts ?? Enumerable.Empty<CommunityPostLogEntry>())
                .OrderByDescending(x => x.clockHour)
                .Take(12)
                .ToArray();

            if (recent.Length == 0)
            {
                AddMutedLine("尚無發文紀錄。從上方選擇類型發布第一則動態。");
                return;
            }

            foreach (CommunityPostLogEntry entry in recent)
            {
                CommunityPostTemplate template = CommunityOperationsRules.FindTemplate(entry.templateId);
                string title = template?.title ?? entry.templateId;
                RectTransform row = AddRow(44f);
                AddRowLabel(
                    row, 0.02f, 0.28f,
                    CareerCalendar.FormatShort(_content.Balance, entry.clockHour),
                    TextMuted, 12);
                AddRowLabel(row, 0.24f, 0.62f, title, TextPrimary, 13);
                string effect = $"名氣 {FormatSigned(entry.fameDelta)} · 威望 {FormatSigned(entry.exposureDelta)}";
                if (entry.fansDelta != 0)
                    effect += $" · 粉絲 {FormatSigned(entry.fansDelta)}";
                AddRowLabel(row, 0.64f, 0.98f, effect, TextMuted, 11, TextAnchor.MiddleRight);
            }
        }

        private void OnToggleCommunityAutoDaily()
        {
            if (_session == null) return;
            bool next = !_session.State.communityAutoDailyUpdate;
            _session.SetCommunityAutoDailyUpdate(next, out string message);
            _status = message ?? (next ? "已開啟自動日常動態" : "已關閉自動日常動態");
            if (_statusText != null) _statusText.text = _status;
            RefreshAll();
        }

        private void OnPublishCommunityPost(string templateId)
        {
            if (_session == null) return;
            if (!_session.TryPublishCommunityPost(templateId, out string message))
            {
                _status = message ?? "無法發布";
                if (_statusText != null) _statusText.text = _status;
                RebuildContent();
                return;
            }

            _status = message;
            if (_statusText != null) _statusText.text = _status;
            RefreshAll();
        }

        private static string FormatSigned(int value) =>
            value > 0 ? $"+{value}" : value.ToString();
    }
}
