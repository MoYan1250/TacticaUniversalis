# -*- coding: utf-8 -*-
"""Rewrite prestige/fame player copy; expand tooltip; AGENTS text-gen reminder."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:120]!r}")
    return text.replace(old, new, 1)


def patch_demo() -> None:
    path = ROOT / "Assets/_Project/Scripts/Presentation/CareerDemoController.cs"
    text = path.read_text(encoding="utf-8")

    text = must_replace(
        text,
        """            const float width = 380f;
            float bodyHeight = Mathf.Max(80f, _statTooltipBody.preferredHeight);
            // Title band + divider + padding ≈ 70px; fans stays compact, others grow with body.
            float height = Mathf.Clamp(70f + bodyHeight + 24f, 220f, 420f);
            _statTooltipRoot.rectTransform.sizeDelta = new Vector2(width, height);""",
        """            const float width = 440f;
            float bodyHeight = Mathf.Max(80f, _statTooltipBody.preferredHeight);
            // Title band + divider + padding; grow with body so long copy is not clipped.
            float height = Mathf.Clamp(70f + bodyHeight + 28f, 220f, 620f);
            _statTooltipRoot.rectTransform.sizeDelta = new Vector2(width, height);""",
    )

    text = must_replace(
        text,
        """                case "prestige":
                    title = "威望";
                    body =
                        "俱樂部在圈內的觀感與聲譽（類似《歐陸風雲》的威望）。\\n" +
                        "高威望較易獲得邀請賽、董事會支持與引援吸引力；低威望則談判與邀約較吃力。\\n" +
                        "與「名氣」不同：威望是外界怎麼看你，名氣是你有多出名。\\n" +
                        "HLTV 積分請在世界排名面板查看。\\n\\n" +
                        $"目前：{club.exposure}\\n" +
                        $"狀態：{ReputationBandLabel(club.exposure)}\\n\\n" +
                        legend;
                    return true;
                case "fame":
                    title = "名氣";
                    body =
                        "俱樂部的有名程度與大眾辨識度（類似《歐陸風雲》的正統性／知名度軸）。\\n" +
                        "高名氣有利商品、媒體關注與被動曝光成長；低名氣則較難被看見。\\n" +
                        "與「威望」不同：名氣是你有多紅，威望是外界觀感好不好。\\n" +
                        "HLTV 積分請在世界排名面板查看。\\n\\n" +
                        $"目前：{club.fame}\\n" +
                        $"狀態：{ReputationBandLabel(club.fame)}\\n\\n" +
                        legend;
                    return true;""",
        """                case "prestige":
                    title = "威望";
                    body =
                        "俱樂部在圈內的觀感與聲譽。\\n" +
                        "高威望較易獲得邀請賽、董事會支持與引援吸引力；低威望則談判與邀約較吃力。\\n" +
                        "與名氣不同：威望是外界怎麼看你，名氣是你有多出名。\\n" +
                        "HLTV 積分請在世界排名面板查看。\\n\\n" +
                        $"目前：{club.exposure}\\n" +
                        $"狀態：{ReputationBandLabel(club.exposure)}\\n\\n" +
                        legend;
                    return true;
                case "fame":
                    title = "名氣";
                    body =
                        "俱樂部的有名程度與大眾辨識度。\\n" +
                        "高名氣有利商品、媒體關注與被動曝光成長；低名氣則較難被看見。\\n" +
                        "與威望不同：名氣是你有多紅，威望是外界觀感好不好。\\n" +
                        "HLTV 積分請在世界排名面板查看。\\n\\n" +
                        $"目前：{club.fame}\\n" +
                        $"狀態：{ReputationBandLabel(club.fame)}\\n\\n" +
                        legend;
                    return true;""",
    )

    path.write_text(text, encoding="utf-8", newline="\n")
    print("patched", path)


def patch_domain() -> None:
    path = ROOT / "Assets/_Project/Scripts/Domain/CareerDomain.cs"
    text = path.read_text(encoding="utf-8")

    text = must_replace(
        text,
        """    /// <summary>
    /// Club 名氣 / 威望 — analogous to EU4 legitimacy / prestige.
    /// 名氣 (fame): how widely known the club is.
    /// 威望 (exposure): how the scene perceives the club's standing.
    /// </summary>""",
        """    /// <summary>
    /// Club fame / exposure standing helpers for UI modifiers.
    /// fame: how widely known the club is; exposure: how the scene perceives standing.
    /// </summary>""",
    )

    text = must_replace(
        text,
        '                    $"名氣 {club.fame}：俱樂部廣為人知（類似 EU4 高正統性）。商品與媒體關注較佳，較易吸引目光與合作邀約。"));',
        '                    $"名氣 {club.fame}：俱樂部廣為人知。商品與媒體關注較佳，較易吸引目光與合作邀約。"));',
    )
    text = must_replace(
        text,
        '                    $"威望 {club.exposure}：外界觀感良好（類似 EU4 高威望）。邀請賽、董事會支持與引援吸引力提升。"));',
        '                    $"威望 {club.exposure}：外界觀感良好。邀請賽、董事會支持與引援吸引力提升。"));',
    )

    # code comments mentioning EU4-style are fine for developers; leave math comments

    path.write_text(text, encoding="utf-8", newline="\n")
    print("patched", path)


def patch_hub() -> None:
    path = ROOT / "Assets/_Project/Scripts/Presentation/CareerClubHubPresentation.cs"
    text = path.read_text(encoding="utf-8")
    text = must_replace(
        text,
        '                "類似 HOI4 國家任務：分階段節點將建在同一套條件／flag 之上（開發中）。");',
        '                "分階段任務節點將建在同一套條件與旗標之上，目前開發中。");',
    )
    text = must_replace(
        text,
        '                "類似 EU4 決議：消耗現金並調整各組織滿意度（綠色↑／紅色↓）。通過後進入冷卻。");',
        '                "消耗現金並調整各組織滿意度，綠色為上升、紅色為下降。通過後進入冷卻。");',
    )
    path.write_text(text, encoding="utf-8", newline="\n")
    print("patched", path)


def patch_agents() -> None:
    path = ROOT / "AGENTS.md"
    text = path.read_text(encoding="utf-8")
    old = """## 文本生成（必守）

- 撰寫或生成面向玩家的文案、JSON 內容、UI 字串、說明文件等**文本**時，**禁止隨意使用**引號與括號類特殊符號，例如：`「」`、`（）` / `()`、`《》`，以及同類裝飾性括號／書名號。
- 若文案確實需要上述符號，**必須先詢問使用者並取得同意**後再寫入；不得自行決定加上。
- 本條針對**文本內容**；程式碼語法中的括號、既有檔案中已存在且未要求改寫的符號，不在此限。
"""
    new = """## 文本生成（必守）

- 撰寫或生成面向玩家的文案、JSON 內容、UI 字串、說明文件等**文本**時，**禁止隨意使用**引號與括號類特殊符號，例如：`「」`、`（）` / `()`、`《》`，以及同類裝飾性括號／書名號。
- 若文案確實需要上述符號，**必須先詢問使用者並取得同意**後再寫入；不得自行決定加上。
- 本條針對**文本內容**；程式碼語法中的括號、既有檔案中已存在且未要求改寫的符號，不在此限。
- **禁止把設計 prompt／參考比喻寫進玩家文案**。例如「類 EU4 的威望」「類似《歐陸風雲》」「類似 HOI4 任務」只供開發對齊機制，**不得**出現在 UI tooltip、事件說明、JSON 玩家可見字串等。應改寫成遊戲內自洽的說明。
"""
    if old not in text:
        raise RuntimeError("AGENTS text-gen section missing")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")
    print("patched", path)


def patch_rule() -> None:
    path = ROOT / ".cursor/rules/tactica-core.mdc"
    text = path.read_text(encoding="utf-8")
    needle = "6. **文本符號**：生成文案／JSON／UI 字串時禁止隨意使用「」()《》等；需要時先問用戶。詳見 `AGENTS.md`。\n"
    if "禁止把設計 prompt" in text:
        print("rule already has prompt note")
        return
    insert = (
        needle
        + "6b. **勿寫入 prompt 比喻**：類 EU4／類似歐陸風雲等僅供設計參考，不得進玩家文案。詳見 `AGENTS.md`。\n"
    )
    if needle not in text:
        raise RuntimeError("rule needle missing")
    path.write_text(text.replace(needle, insert, 1), encoding="utf-8", newline="\n")
    print("patched", path)


def main() -> None:
    patch_demo()
    patch_domain()
    patch_hub()
    patch_agents()
    patch_rule()
    # verify
    demo = (ROOT / "Assets/_Project/Scripts/Presentation/CareerDemoController.cs").read_text(encoding="utf-8")
    assert "歐陸風雲" not in demo
    assert "類 EU4" not in demo and "類似 EU4" not in demo
    assert "擊殺廣播" not in demo  # career file shouldn't have this; just ensure utf8 ok
    assert "俱樂部在圈內的觀感與聲譽。" in demo
    print("ok")


if __name__ == "__main__":
    main()
