# -*- coding: utf-8 -*-
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
events_path = root / "Assets/_Project/Data/JSON/career_events.json"
data = json.loads(events_path.read_text(encoding="utf-8"))


def opt(oid, label):
    return {"id": oid, "label": label, "tooltip": "", "effects": []}


new_events = [
    {
        # Temporary substitution (9/16-9/26) reverts; no persistent roster op to
        # avoid leaving the AI Astralis roster with gla1ve permanently benched.
        "id": "hist_astralis_xyp9x_subs_glaive_2021_09_07",
        "title": "Xyp9x替補gla1ve出戰BLAST Premier Fall Groups",
        "tier": 5,  # C
        "isMajor": False,
        "fireHour": 5976,  # 2021-09-07
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "astralis_2021",
        "onFireEffects": [
            {"type": "add_estate", "key": "public_opinion", "value": 1},
        ],
        "description": (
            "Astralis指揮gla1ve因將迎來第一個孩子的出生，將缺席BLAST Premier Fall Groups賽事。"
            "Xyp9x將暫時回歸先發陣容填補空缺，gla1ve本人已在推特上證實這項安排。"
            "體育總監Kasper Hvidt表示，即使電競界缺乏這樣的傳統，但給予選手陪伴家人的時間至關重要。"
            "Xyp9x目前已與隊伍一同訓練，而gla1ve將以助理教練的身分輔佐zonic。"
        ),
        "firstPersonDescription": (
            "gla1ve因將迎來第一個孩子的出生，將暫時離開隊伍參加BLAST Premier Fall Groups賽事。"
            "Xyp9x將在此期間回歸先發陣容填補空缺，gla1ve本人也證實了這項安排。"
            "體育總監Kasper Hvidt表示，即使電競界缺乏這樣的傳統，但我們堅信在精英環境中取得生活與工作的平衡至關重要。"
            "Xyp9x目前已與隊伍一同訓練，而gla1ve將以助理教練的身分輔佐zonic。"
        ),
        "triggers": [{"type": "always"}],
        "options": [
            opt("timely", "殘局大師暫時回歸，這對Astralis而言是及時的補強。"),
            opt("respect", "gla1ve的父親身分值得尊重，這是一次合理的請假。"),
        ],
        "firstPersonOptions": [
            opt("rotation", "這是輪換制度的正常運作，祝福gla1ve成為父親。"),
            opt("direct", "Xyp9x的回歸對我們而言是最直接的選擇。"),
        ],
    },
    {
        "id": "hist_navi_grand_slam_2021_09_13",
        "title": "NAVI拿下大滿貫，成為歷史上第三支達成的隊伍",
        "tier": 2,  # S
        "isMajor": True,
        "fireHour": 6120,  # 2021-09-13
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "navi_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": 30},
            {"type": "add_roster_fame", "value": 30},
            {"type": "add_fame", "value": 12},
            {"type": "add_fans", "value": 40000},
            {"type": "add_board", "value": 10},
            {"type": "add_estate", "key": "public_opinion", "value": 12},
            {"type": "add_estate", "key": "star_players", "value": 10},
        ],
        "description": (
            "NAVI在贏得EPL S14冠軍後，正式完成英特爾大滿貫，成為繼Astralis與Team Liquid之後第三支達成此成就的隊伍。"
            "他們在十站賽事週期中奪下四座冠軍，包括2020年IEM卡托維茲、2020年ESL One科隆歐洲區、"
            "2021年IEM卡托維茲以及EPL S14。這支獨聯體勁旅在b1t正式取代flamie後，陣容達到前所未有的穩定與默契。"
            "s1mple的個人統治力與B1ad3的戰術體系共同鑄造了這座里程碑，NAVI的時代已然降臨。"
        ),
        "firstPersonDescription": (
            "我們在贏得EPL S14冠軍後，正式完成了英特爾大滿貫的壯舉，"
            "成為繼Astralis與Team Liquid之後，歷史上第三支達成此成就的隊伍。"
            "從2020年IEM卡托維茲到2021年EPL S14，我們在十站賽事中拿下了四座冠軍。"
            "這支隊伍歷經多次調整與磨合，終於攀上了這座象徵統治力的高峰。"
            "s1mple與electronic的雙核心組合，加上b1t的崛起與教練B1ad3的戰術體系，共同鑄造了這個屬於我們的黑金王朝。"
        ),
        "triggers": [{"type": "always"}],
        "options": [
            opt("dynasty", "黑金王朝正式崛起，這支隊伍正在書寫歷史。"),
            opt("dominance", "恭喜NAVI，他們展現了真正的統治力。"),
            opt("major", "接下來的斯德哥爾摩Major，他們將是最大的奪冠熱門。"),
        ],
        "firstPersonOptions": [
            opt("glory", "我們做到了，這是屬於黑金王朝的榮耀時刻。"),
            opt("deserved", "感謝每一位選手與工作人員的努力，這座大滿貫是我們應得的。"),
        ],
    },
    {
        "id": "hist_pgl_major_stockholm_offline_2021_09_16",
        "title": "PGL Major Stockholm宣布線下回歸",
        "tier": 2,  # S
        "isMajor": True,
        "fireHour": 6192,  # 2021-09-16
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "",
        "onFireEffects": [],
        "description": (
            "PGL正式宣布，斯德哥爾摩Major將如期以線下賽形式舉辦，"
            "這是自2020年初新冠疫情爆發以來，CSGO首次迎來線下Major賽事。"
            "瑞典政府於上週二宣布進一步放寬防疫限制，從九月下旬起允許室內活動滿載運作，為Major的舉辦掃除了最後的障礙。"
            "賽事將於10月26日至11月7日在Avicii Arena舉行，淘汰賽階段目標為滿場觀眾，該場館可容納約一萬四千人。"
            "獎金池提升至兩百萬美元，創下CSGO Major歷史新高。"
            "各賽區名額分配如下：歐洲十一席、北美五席、獨聯體五席、亞洲一席、大洋洲一席、南美洲一席，"
            "最終名單將由IEM Fall賽事結束後確定。"
        ),
        "firstPersonDescription": "",
        "triggers": [{"type": "always"}],
        "options": [
            opt("return", "兩年等待終於結束，線下Major的回歸對整個生態意義重大。"),
            opt("crowd", "滿場觀眾的Major才是真正的Major，這將是一場盛大的回歸。"),
            opt("rmr", "各隊的RMR積分將在此刻迎來最終的考驗。"),
        ],
        "firstPersonOptions": [],
    },
]

existing = {e.get("id") for e in data["events"]}
for e in new_events:
    if e["id"] in existing:
        data["events"] = [x for x in data["events"] if x.get("id") != e["id"]]
    data["events"].append(e)

events_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
text = events_path.read_text(encoding="utf-8")
assert "Xyp9x替補gla1ve" in text
assert "NAVI拿下大滿貫" in text
assert "PGL Major Stockholm宣布線下回歸" in text
assert "add_roster_fame" in text
print("total", len(data["events"]), "ok")
