# -*- coding: utf-8 -*-
from pathlib import Path

path = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/CareerDemoController.cs"
text = path.read_text(encoding="utf-8")

replacements = [
    (
        '                "Fame", statsHost.transform, "UI/Icons/fame", "HLTV 積分",',
        '                "Fame", statsHost.transform, "UI/Icons/fame", "HLTV points",',
    ),
    (
        '            _rankingHeaderPts.text = "積分";',
        '            _rankingHeaderPts.text = "HLTV pts";',
    ),
    (
        '                _rankingHeaderPts.text = _rankingShowAllClubs ? "狀態" : "積分 · 近期";',
        '                _rankingHeaderPts.text = _rankingShowAllClubs ? "狀態" : "HLTV pts · 近期";',
    ),
    (
        """                case "fame":
                    title = "名聲";
                    body =
                        "俱樂部整體知名度，影響贊助等級、商品收入與轉會吸引力。\\n" +
                        "每月會自然衰減；重大賽事表現可推高名聲。\\n\\n" +
                        $"目前：{club.fame}\\n\\n" +
                        legend;
                    return true;""",
        """                case "fame":
                    title = "HLTV points";
                    body =
                        "世界排名積分（上限 1000），依 52 週窗口內賽事成績與時間衰減計算。\\n" +
                        "Major / S 級等高階賽事名次貢獻較大；淘汰賽勝場另有少量加分。\\n\\n" +
                        $"目前：{HltvPointsFor(club.clubId)}\\n\\n" +
                        legend;
                    return true;""",
    ),
    (
        """                    string pts = entry != null
                        ? $"{WorldPointsLabel(club.clubId, entry, _session.State):0.0}"
                        : "—";
                    string recent = entry != null ? $"{entry.recentPoints:0.0}" : "—";""",
        """                    string pts = entry != null
                        ? $"{WorldPointsLabel(club.clubId, entry, _session.State):0}"
                        : "—";
                    string recent = entry != null
                        ? $"{RankingRules.ClampHltvPoints(entry.recentPoints):0}"
                        : "—";""",
    ),
    (
        """                        "依 52 週窗口內、帶時間衰減的賽事積分排序（HLTV 風格）。\\n" +
                        $"積分半衰期約 {halfLifeWeeks} 週；「近期」為過去 13 週有效積分。\\n" +
                        "Major / Super-Elite 等高階賽事名次貢獻最大；淘汰賽勝場另有少量加分。\\n\\n" +
                        $"目前：{(rank > 0 ? $"#{rank}" : "#—")} · 總積分 {pts} · 近期 {recent}\\n\\n" +""",
        """                        "依 52 週窗口內、帶時間衰減的 HLTV points 排序（上限 1000）。\\n" +
                        $"積分半衰期約 {halfLifeWeeks} 週；「近期」為過去 13 週有效分數。\\n" +
                        "Major / Super-Elite 等高階賽事名次貢獻較大；淘汰賽勝場另有少量加分。\\n\\n" +
                        $"目前：{(rank > 0 ? $"#{rank}" : "#—")} · HLTV points {pts} · 近期 {recent}\\n\\n" +""",
    ),
    (
        '            ptsText.text = $"{total:0.0}\\n<size=14><color=#8890A0>近 {rank.recentPoints:0.0}</color></size>";',
        '            ptsText.text = $"{total:0}\\n<size=14><color=#8890A0>近 {RankingRules.ClampHltvPoints(rank.recentPoints):0}</color></size>";',
    ),
    (
        '            string ptsLabel = rank != null ? $"{WorldPointsLabel(clubId, rank, state):0.0} pts" : "—";',
        '            string ptsLabel = rank != null ? $"{WorldPointsLabel(clubId, rank, state):0} HLTV pts" : "—";',
    ),
]

for old, new in replacements:
    if old not in text:
        raise SystemExit(f"block not found:\n{old[:120]}...")
    text = text.replace(old, new, 1)

path.write_text(text, encoding="utf-8", newline="\n")
print("Patched HLTV points UI copy")
