# -*- coding: utf-8 -*-
import json
from pathlib import Path

p = Path(__file__).resolve().parents[1] / "Assets/_Project/Data/JSON/career_events.json"
data = json.loads(p.read_text(encoding="utf-8"))


def opt(oid, label):
    return {"id": oid, "label": label, "tooltip": "", "effects": []}


new_events = [
    {
        "id": "hist_train_out_ancient_in_2021_04_05",
        "title": "Train正式移出比賽圖池，Ancient加入",
        "tier": 2,  # S
        "isMajor": True,
        "fireHour": 2256,  # 2021-04-05
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "",
        "firstPersonReserved": True,
        "onFireRosterMapId": "Train",
        "onFireRosterMinGrade": "S",
        "onFireEffects": [
            {"type": "add_exposure", "value": -1},
            {"type": "add_estate", "key": "coaching_staff", "value": -2},
            {"type": "set_flag", "key": "train_pool_removed", "durationDays": 0},
        ],
        "description": "Valve在狂牙大行動結束後宣布Train被移出現役地圖池，由Ancient正式取代。這是自2019年3月Vertigo取代Cache以來，官方首次對現役地圖池進行調整。Train在職業比賽中的選擇率僅百分之四點一七，在服役地圖中排名倒數第二。Ancient在加入前已進行多處調整，包括CT出生點通往A點的新路線、中路左側T入口的拓寬、A點Tunnel天窗的開放、B點炸彈安置區的延伸以及A點邊緣的開放等。接下來幾週的RMR賽事仍將沿用舊地圖池，新地圖池將在這些賽事結束後正式啟用。",
        "firstPersonDescription": "Valve在狂牙大行動結束後宣布了現役地圖池的更換決定，Train被移出，Ancient正式加入。這是自2019年3月Vertigo取代Cache以來，官方首次對現役地圖池進行調整。Train在職業比賽中的選擇率僅百分之四點一七，在所有地圖中排名倒數第二，僅高於Vertigo。Valve同時對Ancient進行了多處調整，包括CT出生點通往A點的新路線、中路左側T入口的拓寬、A點Tunnel天窗的開放、B點炸彈安置區的延伸以及A點邊緣的開放等。這項變革將影響所有隊伍的戰術體系與練圖策略，我們必須盡快適應這張新地圖。",
        "triggers": [{"type": "always"}],
        "options": [
            opt("redefine", "這項變革將重新定義職業賽場的戰術版圖。"),
            opt("test", "地圖池的更動對所有隊伍都是一次考驗。"),
            opt("nostalgia", "經典地圖的退役總是令人感慨。"),
        ],
        "firstPersonOptions": [
            opt("accept", "我們接受這項變革，並將投入時間研究Ancient。"),
            opt("reshuffle", "地圖池的更動將重新洗牌各隊的戰術儲備。"),
            opt("blow", "這對擅長Train的隊伍而言是一個沉重的打擊。"),
        ],
    },
    {
        "id": "hist_b1t_replaces_flamie_navi_2021_04_11",
        "title": "b1t正式取代flamie成為NAVI主力",
        "tier": 3,  # A
        "isMajor": True,
        "fireHour": 2400,  # 2021-04-11
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "navi_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": 1},
            {"type": "add_board", "value": 2},
            {"type": "add_estate", "key": "star_players", "value": 2},
            {"type": "add_estate", "key": "reserves", "value": 1},
            {"type": "add_estate", "key": "veterans", "value": -1},
        ],
        "description": "NAVI宣布由青訓選手b1t正式取代flamie成為先發陣容的固定成員。這項決策主要是為了因應Valve的RMR新制，由於人員替換將扣除積分，隊伍必須在兩位選手中做出最終選擇。教練B1ad3透露flamie近期承受巨大的心理壓力，即使有心理醫生協助也未能恢復狀態，因此需要暫時休息。NAVI將於4月11日至24日在基輔進行集訓，新陣容將於DreamHack春季大師賽首次亮相。",
        "firstPersonDescription": "我們正式決定讓b1t取代flamie成為先發陣容的固定成員。這項決策主要是為了因應Valve的RMR新制，由於人員替換將扣除百分之二十的RMR積分，我們必須在b1t與flamie之間做出最終選擇。教練B1ad3表示，flamie近期承受著巨大的心理壓力，即使有心理醫生的協助也未能恢復最佳狀態，為了隊伍的發展，他需要暫時休息並重新調整心態。b1t此前已在多場賽事中輪換上場，展現出令人期待的潛力。我們將以這套固定的五人陣容備戰接下來的DreamHack春季大師賽與RMR賽事。",
        "triggers": [{"type": "always"}],
        "options": [
            opt("major", "這項決策將決定NAVI在Major之路上的成敗。"),
            opt("potential", "flamie的離開令人惋惜，但b1t的潛力值得期待。"),
            opt("rmr", "RMR新制正在迫使各隊做出艱難的陣容抉擇。"),
        ],
        "firstPersonOptions": [
            opt("hard", "這是艱難的決定，但我們相信這對隊伍的長遠發展是正確的。"),
            opt("thanks", "感謝flamie多年來的貢獻，期待他調整後的回歸。"),
            opt("support", "我們別無選擇，只能全力支持b1t的成長。"),
        ],
    },
    {
        "id": "hist_xyp9x_benched_astralis_2021_04_13",
        "title": "Xyp9x被Bubzkji換上首發",
        "tier": 5,  # C
        "isMajor": False,
        "fireHour": 2448,  # 2021-04-13
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "astralis_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": -1},
            {"type": "add_estate", "key": "veterans", "value": -1},
            {"type": "add_estate", "key": "public_opinion", "value": -1},
        ],
        "description": "Astralis將Bubzkji調入先發陣容，取代Xyp9x的位置。Xyp9x自2020年中因職業倦怠申請休假後，狀態一直未能完全恢復。Bubzkji在此前的輪換中已累積了一定的上場經驗，此次調整被視為Astralis七人輪換制的一部分。",
        "firstPersonDescription": "我們在陣容上做出調整，由Bubzkji取代Xyp9x進入先發陣容。Xyp9x自2020年中因職業倦怠申請休假後，狀態一直未能完全恢復。Bubzkji在此前的輪換中已累積了一定的上場經驗，我們相信他能為隊伍帶來新的活力。這項調整也與我們七人輪換制的長期規劃相符。",
        "triggers": [{"type": "always"}],
        "options": [
            opt("routine", "這或許只是輪換制度下的例行調整。"),
            opt("warning", "Xyp9x的狀態令人擔憂，這對Astralis是一個警訊。"),
        ],
        "firstPersonOptions": [
            opt("normal", "這是我們輪換制度的正常運作。"),
            opt("prove", "希望Bubzkji能抓住這次機會證明自己。"),
        ],
    },
]

existing = {e.get("id") for e in data["events"]}
for e in new_events:
    if e["id"] in existing:
        data["events"] = [x for x in data["events"] if x.get("id") != e["id"]]
    data["events"].append(e)

p.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
text = p.read_text(encoding="utf-8")
for t in [
    "Train正式移出比賽圖池，Ancient加入",
    "b1t正式取代flamie成為NAVI主力",
    "Xyp9x被Bubzkji換上首發",
]:
    assert t in text, t
print("total", len(data["events"]), "ok")
