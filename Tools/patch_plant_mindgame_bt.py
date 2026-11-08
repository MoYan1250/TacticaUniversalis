# -*- coding: utf-8 -*-
"""Wire plant channel + acoustic contest/fake-defuse into BehaviorTreeRoundSimulator."""
from pathlib import Path

TARGET = Path(r"Assets/_Project/Scripts/Domain/BehaviorTreeRoundSimulator.cs")


def must_replace(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing anchor: {label}\n---\n{old[:300]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    if "PlantChannelRules.Tick" in text and "ShouldContestPlant" in text:
        print("already patched")
        return

    text = must_replace(
        text,
        "        public float PlantTime { get; set; } = -1f;\n"
        "        public float RoundTimeLimit { get; set; } = 115f;\n"
        "        public float BombTimer { get; set; } = 40f;\n"
        "        /// <summary>CT currently channeling the defuse, or null.</summary>\n"
        "        public string DefuserId { get; set; }\n"
        "        public float DefuseStartTime { get; set; } = -1f;\n"
        "        public float DefuseRequiredSeconds { get; set; }\n",
        "        public float PlantTime { get; set; } = -1f;\n"
        "        /// <summary>T currently channeling the plant, or null.</summary>\n"
        "        public string PlanterId { get; set; }\n"
        "        public float PlantStartTime { get; set; } = -1f;\n"
        "        public float PlantRequiredSeconds { get; set; }\n"
        "        public float RoundTimeLimit { get; set; } = 115f;\n"
        "        public float BombTimer { get; set; } = 40f;\n"
        "        /// <summary>CT currently channeling the defuse, or null.</summary>\n"
        "        public string DefuserId { get; set; }\n"
        "        public float DefuseStartTime { get; set; } = -1f;\n"
        "        public float DefuseRequiredSeconds { get; set; }\n"
        "        /// <summary>Short hold/peek after plant cancel or fake defuse.</summary>\n"
        "        public Dictionary<string, float> MindGameUntilTime { get; } =\n"
        "            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);\n",
        "world plant fields",
    )

    text = must_replace(
        text,
        "                TickTagging(world);\n"
        "                TickAgents(world, time, random);\n"
        "                TickDefuse(world, time);\n",
        "                TickTagging(world);\n"
        "                TickAgents(world, time, random);\n"
        "                PlantChannelRules.Tick(world, time);\n"
        "                TickDefuse(world, time);\n",
        "tick plant",
    )

    text = must_replace(
        text,
        "            world.DroppedBombZoneId = null;\n"
        "            world.GroundLoot.Clear();\n"
        "            ClearDefuse(world);\n",
        "            world.DroppedBombZoneId = null;\n"
        "            world.GroundLoot.Clear();\n"
        "            PlantChannelRules.Clear(world, time, emitCancel: false);\n"
        "            world.MindGameUntilTime.Clear();\n"
        "            ClearDefuse(world);\n",
        "finish clear plant",
    )

    text = must_replace(
        text,
        "                // 3. Retake after plant\n"
        "                new BtSequence(\n"
        "                    new BtCondition(c =>\n"
        "                        c.World.BombPlanted &&\n"
        "                        c.SelfTeam.Side == Side.CounterTerrorist &&\n"
        "                        !c.World.BombDefused),\n"
        "                    new BtAction(ActRetake)),\n",
        "                // 2b. Contest live plant when team heard/saw planting\n"
        "                new BtSequence(\n"
        "                    new BtCondition(ShouldContestPlant),\n"
        "                    new BtAction(ActContestPlant)),\n"
        "                // 3. Retake after plant\n"
        "                new BtSequence(\n"
        "                    new BtCondition(c =>\n"
        "                        c.World.BombPlanted &&\n"
        "                        c.SelfTeam.Side == Side.CounterTerrorist &&\n"
        "                        !c.World.BombDefused),\n"
        "                    new BtAction(ActRetake)),\n",
        "tree contest",
    )

    text = must_replace(
        text,
        "            List<IntelNote> seen = IntelRules.ScanVisible(\n"
        "                c.World.Request.map, c.Self, c.EnemyTeam.AlivePlayers.ToList(), _catalog, c.Time,\n"
        "                c.World.Request?.Los);\n"
        "            for (int i = 0; i < seen.Count; i++)\n"
        "            {\n"
        "                IntelNote note = seen[i];\n"
        "                personal.Upsert(note);\n"
        "                TryCalloutIntel(c, team, note);\n"
        "            }\n"
        "        }\n",
        "            List<IntelNote> seen = IntelRules.ScanVisible(\n"
        "                c.World.Request.map, c.Self, c.EnemyTeam.AlivePlayers.ToList(), _catalog, c.Time,\n"
        "                c.World.Request?.Los);\n"
        "            for (int i = 0; i < seen.Count; i++)\n"
        "            {\n"
        "                IntelNote note = seen[i];\n"
        "                personal.Upsert(note);\n"
        "                TryCalloutIntel(c, team, note);\n"
        "            }\n"
        "\n"
        "            string plantingZone = PlantChannelRules.IsChanneling(c.World)\n"
        "                ? ContentDatabase.ClampZoneToMap(c.World.Request.map, c.World.PlantSiteId)\n"
        "                : null;\n"
        "            string defuseZone = !string.IsNullOrEmpty(c.World.DefuserId)\n"
        "                ? ContentDatabase.ClampZoneToMap(c.World.Request.map, c.World.PlantSiteId)\n"
        "                : null;\n"
        "            AcousticIntelRules.ScanAudible(\n"
        "                c.World.Graph, c.Self, c.EnemyTeam.AlivePlayers,\n"
        "                personal, team, c.Time, c.Random,\n"
        "                plantingZone, c.World.PlanterId, c.World.DefuserId, defuseZone);\n"
        "\n"
        "            if (PlantChannelRules.IsChanneling(c.World) &&\n"
        "                !string.IsNullOrEmpty(plantingZone) &&\n"
        "                seen.Exists(n => string.Equals(n.ZoneId, plantingZone, StringComparison.OrdinalIgnoreCase)))\n"
        "            {\n"
        "                AcousticIntelRules.UpsertTagged(\n"
        "                    personal, plantingZone, AcousticIntelRules.TagPlanting, c.Time, c.Self.Definition.id);\n"
        "                AcousticIntelRules.UpsertTagged(\n"
        "                    team, plantingZone, AcousticIntelRules.TagPlanting, c.Time, c.Self.Definition.id);\n"
        "                c.World.Add(c.Time, EventType.StatusUpdated, c.Self.Definition.id, null, plantingZone,\n"
        "                    \"heard_plant\", c.SelfTeam.Definition.id, null, \"heard_plant\");\n"
        "            }\n"
        "        }\n",
        "scan acoustic",
    )

    text = must_replace(
        text,
        "                    if (string.Equals(c.Self.Definition.id, c.World.BombCarrierId,\n"
        "                            StringComparison.OrdinalIgnoreCase) &&\n"
        "                        string.Equals(c.Self.ZoneId, site, StringComparison.OrdinalIgnoreCase) &&\n"
        "                        c.Time >= 18f)\n"
        "                    {\n"
        "                        c.World.BombPlanted = true;\n"
        "                        c.World.PlantTime = c.Time;\n"
        "                        c.World.PlantSiteId = site;\n"
        "                        c.World.Add(c.Time, EventType.BombPlanted, c.Self.Definition.id, null, site,\n"
        "                            $\"{c.Self.Definition.id} planted\", c.SelfTeam.Definition.id);\n"
        "                    }\n",
        "                    if (string.Equals(c.Self.Definition.id, c.World.BombCarrierId,\n"
        "                            StringComparison.OrdinalIgnoreCase) &&\n"
        "                        string.Equals(c.Self.ZoneId, site, StringComparison.OrdinalIgnoreCase) &&\n"
        "                        c.Time >= 18f)\n"
        "                    {\n"
        "                        bool threatened =\n"
        "                            AcousticIntelRules.HearsThreatNear(\n"
        "                                c.World.Graph, c.Self.ZoneId, c.EnemyTeam.AlivePlayers,\n"
        "                                PersonalIntelBoard(c), c.Time) ||\n"
        "                            EnemiesInZone(c, site) > 0 ||\n"
        "                            EnemiesAdjacent(c) > 0;\n"
        "                        if (threatened)\n"
        "                        {\n"
        "                            if (PlantChannelRules.IsChanneling(c.World))\n"
        "                            {\n"
        "                                PlantChannelRules.Clear(c.World, c.Time, emitCancel: true);\n"
        "                                c.World.MindGameUntilTime[c.Self.Definition.id] = c.Time + 2.4f;\n"
        "                            }\n"
        "                        }\n"
        "                        else if (c.World.MindGameUntilTime.TryGetValue(c.Self.Definition.id, out float until) &&\n"
        "                                 c.Time < until)\n"
        "                        {\n"
        "                            // Hold / peek after cancel — do not restart plant yet.\n"
        "                        }\n"
        "                        else\n"
        "                        {\n"
        "                            PlantChannelRules.TryBegin(c.World, c.Self, site, c.Time);\n"
        "                        }\n"
        "                    }\n"
        "                    else if (PlantChannelRules.IsChanneling(c.World) &&\n"
        "                             string.Equals(c.World.PlanterId, c.Self.Definition.id,\n"
        "                                 StringComparison.OrdinalIgnoreCase))\n"
        "                    {\n"
        "                        PlantChannelRules.Clear(c.World, c.Time, emitCancel: true);\n"
        "                        c.World.MindGameUntilTime[c.Self.Definition.id] = c.Time + 1.2f;\n"
        "                    }\n",
        "act execute plant",
    )

    text = must_replace(
        text,
        "                else\n"
        "                {\n"
        "                    // Hold near plant with personal spread, not all stacked on site.\n"
        "                    StepToward(c, HoldNearSite(c, c.World.PlantSiteId, assigned));\n"
        "                }\n",
        "                else\n"
        "                {\n"
        "                    // Heard CT defusing → collapse to site; else hold near plant with spread.\n"
        "                    bool heardDefuse = AcousticIntelRules.BoardHasFreshTag(\n"
        "                        TeamIntelBoard(c), AcousticIntelRules.TagDefusing, c.Time,\n"
        "                        AcousticIntelRules.AcousticTtlSeconds);\n"
        "                    string postPlant = heardDefuse\n"
        "                        ? ContentDatabase.ClampZoneToMap(c.World.Request.map, c.World.PlantSiteId)\n"
        "                        : HoldNearSite(c, c.World.PlantSiteId, assigned);\n"
        "                    StepToward(c, postPlant);\n"
        "                }\n",
        "post plant hold",
    )

    text = must_replace(
        text,
        "            float need = c.Self.Loadout != null && c.Self.Loadout.HasDefuseKit ? 5f : 10f;\n"
        "            if (!string.Equals(c.World.DefuserId, c.Self.Definition.id, StringComparison.OrdinalIgnoreCase) ||\n"
        "                c.World.DefuseStartTime < 0f)\n"
        "            {\n"
        "                c.World.DefuserId = c.Self.Definition.id;\n"
        "                c.World.DefuseStartTime = c.Time;\n"
        "                c.World.DefuseRequiredSeconds = need;\n"
        "                c.World.Add(c.Time, EventType.StatusUpdated, c.Self.Definition.id, null, site,\n"
        "                    $\"defusing:{need:0}\", c.SelfTeam.Definition.id, null, \"defusing\");\n"
        "            }\n"
        "            else\n"
        "            {\n"
        "                // Kit pickup mid-channel shortens remaining requirement.\n"
        "                c.World.DefuseRequiredSeconds = Math.Min(c.World.DefuseRequiredSeconds, need);\n",
        "            if (c.World.MindGameUntilTime.TryGetValue(c.Self.Definition.id, out float mindUntil) &&\n"
        "                c.Time < mindUntil)\n"
        "            {\n"
        "                return BtStatus.Running;\n"
        "            }\n"
        "\n"
        "            float need = c.Self.Loadout != null && c.Self.Loadout.HasDefuseKit ? 5f : 10f;\n"
        "            bool alreadyDefusing = string.Equals(\n"
        "                c.World.DefuserId, c.Self.Definition.id, StringComparison.OrdinalIgnoreCase) &&\n"
        "                c.World.DefuseStartTime >= 0f;\n"
        "\n"
        "            if (alreadyDefusing)\n"
        "            {\n"
        "                float channeled = c.Time - c.World.DefuseStartTime;\n"
        "                bool hearsT = AcousticIntelRules.HearsThreatNear(\n"
        "                        c.World.Graph, c.Self.ZoneId, c.EnemyTeam.AlivePlayers,\n"
        "                        PersonalIntelBoard(c), c.Time) ||\n"
        "                    EnemiesAdjacent(c) > 0;\n"
        "                bool bait = channeled >= 0.45f && channeled < need * 0.85f &&\n"
        "                    c.Random != null && c.Random.Next01() < 0.18f;\n"
        "                if (hearsT || bait)\n"
        "                {\n"
        "                    ClearDefuse(c.World);\n"
        "                    c.World.MindGameUntilTime[c.Self.Definition.id] = c.Time + 1.6f;\n"
        "                    c.World.Add(c.Time, EventType.StatusUpdated, c.Self.Definition.id, null, site,\n"
        "                        \"fake_defuse\", c.SelfTeam.Definition.id, null, \"fake_defuse\");\n"
        "                    return BtStatus.Running;\n"
        "                }\n"
        "\n"
        "                c.World.DefuseRequiredSeconds = Math.Min(c.World.DefuseRequiredSeconds, need);\n"
        "            }\n"
        "            else\n"
        "            {\n"
        "                c.World.DefuserId = c.Self.Definition.id;\n"
        "                c.World.DefuseStartTime = c.Time;\n"
        "                c.World.DefuseRequiredSeconds = need;\n"
        "                c.World.Add(c.Time, EventType.StatusUpdated, c.Self.Definition.id, null, site,\n"
        "                    $\"defusing:{need:0}\", c.SelfTeam.Definition.id, null, \"defusing\");\n",
        "act retake fake",
    )

    contest = """
        private static bool ShouldContestPlant(BtContext c)
        {
            if (!c.Self.Alive || c.SelfTeam.Side != Side.CounterTerrorist || c.World.BombPlanted)
                return false;
            return AcousticIntelRules.BoardHasFreshTag(
                       TeamIntelBoard(c), AcousticIntelRules.TagPlanting, c.Time,
                       AcousticIntelRules.AcousticTtlSeconds) ||
                   AcousticIntelRules.BoardHasFreshTag(
                       PersonalIntelBoard(c), AcousticIntelRules.TagPlanting, c.Time,
                       AcousticIntelRules.AcousticTtlSeconds);
        }

        private static BtStatus ActContestPlant(BtContext c)
        {
            string site = AcousticIntelRules.FreshTagZone(
                TeamIntelBoard(c), AcousticIntelRules.TagPlanting, c.Time,
                AcousticIntelRules.AcousticTtlSeconds);
            if (string.IsNullOrEmpty(site))
                site = AcousticIntelRules.FreshTagZone(
                    PersonalIntelBoard(c), AcousticIntelRules.TagPlanting, c.Time,
                    AcousticIntelRules.AcousticTtlSeconds);
            if (string.IsNullOrEmpty(site))
                site = ContentDatabase.ClampZoneToMap(c.World.Request.map, c.World.PlantSiteId);
            site = ContentDatabase.ClampZoneToMap(c.World.Request.map, site);
            if (string.IsNullOrEmpty(site))
                return BtStatus.Failure;

            StepToward(c, site);
            return BtStatus.Running;
        }

"""
    text = must_replace(
        text,
        "        private static BtStatus ActRetake(BtContext c)\n",
        contest + "        private static BtStatus ActRetake(BtContext c)\n",
        "contest methods",
    )

    if not any("\u4e00" <= ch <= "\u9fff" for ch in text):
        raise RuntimeError("UTF-8 corruption: no CJK left")

    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
