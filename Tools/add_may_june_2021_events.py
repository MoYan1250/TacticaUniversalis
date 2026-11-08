# -*- coding: utf-8 -*-
import json
from pathlib import Path

p = Path(__file__).resolve().parents[1] / "Assets/_Project/Data/JSON/career_events.json"
data = json.loads(p.read_text(encoding="utf-8"))


def opt(oid, label):
    return {"id": oid, "label": label, "tooltip": "", "effects": []}


# Jan 4 historically brought olof back as bench; coldzera stayed until June.
for e in data["events"]:
    if e.get("id") != "hist_olofmeister_faze_bench_2021_01_04":
        continue
    e["description"] = (
        "FaZe Clan在NiKo離隊轉投G2之後，戰績持續下滑。管理層在尋找解決方案的過程中，"
        "決定召回老將olofmeister作為替補，填補NiKo離隊後留下的空缺。據悉，隊伍曾接觸過f0rest，"
        "但最終未能成行。olofmeister的回歸被普遍視為短期救火方案，而非長期規劃。"
        "這位兩屆Major冠軍得主曾在2017年隨隊闖入波士頓Major決賽，隊伍希望借助他的經驗來穩定當前的混亂局面。"
    )
    e["firstPersonDescription"] = (
        "在NiKo離隊轉投G2之後，我們的陣容出現了明顯的缺口。戰績持續下滑之際，管理層不得不尋找緊急方案來穩定局面。"
        "olofmeister的名字再次被提起，這位曾在2017年隨隊闖入波士頓Major決賽的老將，雖然一度被認為即將退役，"
        "但我們仍然選擇啟動這項短期救援計畫。他以替補身分回歸，填補NiKo離隊後留下的空缺。"
        "這不是一筆長期投資，而是一場為了止血的緊急措施，我們需要他的經驗與殘存的威懾力來渡過這段動盪期。"
    )
    e["onFireRosterOps"] = [
        {
            "type": "set_squad",
            "playerId": "olofmeister",
            "squadStatus": "bench",
            "clubId": "",
        }
    ]
    break

new_events = [
    {
        "id": "hist_akuma_cis_rmr_cheat_allegations_2021_05_20",
        "title": "Akuma戰隊於獨聯體RMR接連擊敗NaVi與VP，遭作弊指控",
        "tier": 4,  # B
        "isMajor": True,
        "fireHour": 3336,  # 2021-05-20
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "",
        "onFireEffects": [],
        "description": (
            "Akuma戰隊在EPIC League CIS RMR賽事中接連擊敗NaVi與Virtus.pro，引發業界重大爭議。"
            "這支隊伍在賽前的表現與頂級隊伍相去甚遠，卻在本次賽事中展現出遠超以往的實力，多次以壓倒性方式擊敗一線隊伍。"
            "比賽過程中多個可疑鏡頭被社群放大檢視，作弊指控迅速蔓延，多位職業選手公開表達質疑。"
            "賽事方介入調查但未能取得決定性證據，最終以Akuma獲得RMR積分作結。"
            "此事重創了賽事公信力，也讓獨聯體CS的賽事公正性受到前所未有的挑戰。"
        ),
        "firstPersonDescription": "",
        "triggers": [{"type": "always"}],
        "options": [
            opt("abnormal", "他們的表現與過往判若兩人，這絕非正常現象。"),
            opt("stain", "缺乏證據並不代表清白，這件事將永遠成為獨聯體CS的一個汙點。"),
            opt("disappointed", "賽事方的處理方式令人失望。"),
        ],
        "firstPersonOptions": [],
    },
    {
        "id": "hist_coldzera_benched_faze_2021_06_01",
        "title": "coldzera離開FaZe",
        "tier": 4,  # B
        "isMajor": True,
        "fireHour": 3624,  # 2021-06-01
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "faze_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": -4},
            {"type": "add_fame", "value": -2},
            {"type": "add_board", "value": -3},
            {"type": "add_estate", "key": "public_opinion", "value": -3},
            {"type": "add_estate", "key": "star_players", "value": -4},
        ],
        "onFireRosterOps": [
            {
                "type": "set_squad",
                "playerId": "coldzera",
                "squadStatus": "bench",
                "clubId": "",
            },
            {
                "type": "set_squad",
                "playerId": "olofmeister",
                "squadStatus": "starter",
                "clubId": "",
            },
        ],
        "description": (
            "coldzera被FaZe下放至替補席，這位兩屆年度最佳選手與兩屆Major冠軍得主在歐洲的旅程就此告一段落。"
            "coldzera在推特上表示此舉是基於隊伍近期成績不佳，以及他與FaZe的合約即將到期。"
            "他自2019年9月從MIBR加入FaZe以來，為隊伍贏得BLAST Pro Series哥本哈根站冠軍，"
            "但隨著NiKo離隊與陣容多次重組，隊伍始終未能重現往日榮光。"
            "FaZe的世界排名在六月初已跌至歷史新低的第二十五位，未來先發陣容的空缺將由olofmeister回歸填補。"
        ),
        "firstPersonDescription": (
            "coldzera已正式被移至替補席，他在推特上表示這項決定是基於隊伍近期的成績不佳，"
            "以及他與FaZe的合約即將到期。coldzera自2019年9月從MIBR加入以來，"
            "為我們贏得了BLAST Pro Series哥本哈根站的冠軍，並在2020年多次打入賽事四強。"
            "然而隨著NiKo離隊、陣容重組，隊伍始終未能找回過去的競爭力。"
            "coldzera表示將開始評估新的職業機會，我們感謝他這段時間的付出，並祝福他未來一切順利。"
        ),
        "triggers": [{"type": "always"}],
        "options": [
            opt("fail", "巴西明星的歐洲冒險以失敗告終。"),
            opt("sigh", "這筆轉會當初被寄予厚望，如今卻令人唏噓。"),
            opt("next", "他的下一步將備受關注。"),
        ],
        "firstPersonOptions": [
            opt("thanks", "感謝coldzera的貢獻，祝福他找到新的歸宿。"),
            opt("best", "這對雙方都是最好的選擇。"),
        ],
    },
    {
        "id": "hist_nbk_leaves_cs_for_valorant_2021_06_29",
        "title": "NBK-宣布轉戰Valorant，結束CS生涯",
        "tier": 3,  # A
        "isMajor": True,
        "fireHour": 4296,  # 2021-06-29
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "og_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": -5},
            {"type": "add_fame", "value": -3},
            {"type": "add_board", "value": -4},
            {"type": "add_estate", "key": "public_opinion", "value": -3},
            {"type": "add_estate", "key": "star_players", "value": -5},
            {"type": "add_estate", "key": "veterans", "value": -4},
        ],
        "onFireRosterOps": [
            {
                "type": "release",
                "playerId": "nbk",
                "squadStatus": "bench",
                "clubId": "",
            }
        ],
        "description": (
            "NBK-正式告別CS:GO，轉戰Valorant。"
            "這位兩屆Major冠軍得主在推特發布的影片中表示，自己已經經歷了一切，從地方賽事到贏得Major，"
            "將生命奉獻給了CS，但回憶終究屬於過去，是時候做出改變了。"
            "他強調這項決定並非出於金錢考量，而是基於個人職涯發展的考量。"
            "NBK-在CS:GO職業生涯中共奪得五十二座賽事冠軍，三次入選HLTV年度TOP20，並在2019年底與Aleksib共同組建了OG。"
            "法國CS的另一面旗幟就此降下。"
        ),
        "firstPersonDescription": (
            "NBK-正式宣布離開CS:GO並轉戰Valorant。"
            "他在推特發布了一段名為「新的開始」的影片，表示經過數週的深思熟慮後做出了這項決定，並強調這並非出於金錢考量。"
            "NBK-自2019年12月OG成立之初便是我們陣容的核心成員，帶領隊伍在IEM紐約2020歐洲區與Flashpoint第二季中拿下亞軍。"
            "我們感謝他為這支年輕隊伍所做的一切，並祝福他在新的戰場上繼續發光發熱。"
        ),
        "triggers": [{"type": "always"}],
        "options": [
            opt("france", "法國CS傳奇的離去，將進一步削弱法國選手的競爭力。"),
            opt("wave", "這股轉瓦浪潮恐怕還會持續。"),
            opt("watch", "他在Valorant的表現值得關注。"),
        ],
        "firstPersonOptions": [
            opt("thanks", "感謝NBK-為OG奠定的基礎，祝福他在Valorant取得成功。"),
            opt("respect", "一代傳奇的離去令人惋惜，但我們尊重他的選擇。"),
        ],
    },
    {
        "id": "hist_kjaerbye_retires_2021_06_30",
        "title": "Kjaerbye宣布退役",
        "tier": 3,  # A
        "isMajor": True,
        "fireHour": 4320,  # 2021-06-30
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "faze_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": -3},
            {"type": "add_fame", "value": -2},
            {"type": "add_board", "value": -2},
            {"type": "add_estate", "key": "public_opinion", "value": -2},
            {"type": "add_estate", "key": "star_players", "value": -3},
        ],
        "onFireRosterOps": [
            {
                "type": "release",
                "playerId": "kjaerbye",
                "squadStatus": "bench",
                "clubId": "",
            }
        ],
        "description": (
            "Kjaerbye正式宣布退役，結束了他長達十年的CS職業生涯。"
            "這位二十三歲的丹麥步槍手在推特上表示，他已不再擁有在頂級賽場競爭所需的渴望與決心。"
            "Kjaerbye最輝煌的時刻莫過於2017年以十八歲之齡率領Astralis奪得ELEAGUE亞特蘭大Major冠軍，並獲頒賽事MVP，"
            "當時他成為Major史上最年輕的MVP得主。"
            "然而離開Astralis加入North後，他的職業生涯開始走下坡，2020年轉會FaZe亦未能重現昔日風采。"
            "他曾兩度入選HLTV年度TOP20，如今卻以二十三歲的年紀告別了這項他曾經統治的運動。"
        ),
        "firstPersonDescription": (
            "Kjaerbye正式宣布退役，結束了他長達十年的CS職業生涯。"
            "他在推特發布的聲明中表示，他已不再擁有在頂級賽場競爭所需的渴望與決心。"
            "Kjaerbye在2020年8月加入我們，期間為隊伍贏得IEM紐約2020歐洲區冠軍，"
            "然而他的表現始終未能回到2017年的巔峰水準。"
            "我們感謝他在FaZe期間的付出，並祝福他在人生的下一篇章中一切順利。"
        ),
        "triggers": [{"type": "always"}],
        "options": [
            opt("mvp", "亞特蘭大Major的MVP，竟以這樣的方式告別賽場。"),
            opt("decline", "自從離開Astralis後，他的職業生涯就再也沒有回到正軌。"),
            opt("young", "二十三歲退役，這無疑是CSGO史上最令人惋惜的結局之一。"),
        ],
        "firstPersonOptions": [
            opt("thanks", "感謝Kjaerbye的付出，他的成就不會被遺忘。"),
            opt("sigh", "二十三歲就告別賽場，令人不勝唏噓。"),
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
assert "Akuma戰隊於獨聯體RMR" in text
assert "coldzera離開FaZe" in text
assert "NBK-宣布轉戰Valorant" in text
assert "Kjaerbye宣布退役" in text
nbk = next(x for x in data["events"] if x["id"] == "hist_nbk_leaves_cs_for_valorant_2021_06_29")
assert "「" not in nbk["description"]
assert "名為「新的開始」" in nbk["firstPersonDescription"]
jan = next(x for x in data["events"] if x["id"] == "hist_olofmeister_faze_bench_2021_01_04")
assert all(op.get("playerId") != "coldzera" for op in jan.get("onFireRosterOps", []))
print("total", len(data["events"]), "ok")
