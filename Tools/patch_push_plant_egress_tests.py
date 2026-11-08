# -*- coding: utf-8 -*-
from pathlib import Path

p = Path("Assets/_Project/Tests/EditMode/GeneralModeTests.cs")
src = p.read_text(encoding="utf-8")
anchor = """        public void SaveHuntRules_HuntWhenLeadingByTwoAgainstLowEnemyCount()
        {
            Assert.That(SaveHuntRules.HasHuntAdvantage(5, 2), Is.True);
            Assert.That(SaveHuntRules.HasHuntAdvantage(4, 1), Is.True);
            Assert.That(SaveHuntRules.HasHuntAdvantage(3, 2), Is.False);
            Assert.That(SaveHuntRules.HasHuntAdvantage(5, 3), Is.False);
            Assert.That(SaveHuntRules.HasHuntAdvantage(2, 1), Is.False);
        }
"""
insert = anchor + """
        [Test]
        public void SaveHuntRules_TerroristsDoNotHuntBeforePlant()
        {
            Assert.That(SaveHuntRules.ShouldAllowHunt(true, false, 5, 2), Is.False);
            Assert.That(SaveHuntRules.ShouldAllowHunt(true, true, 5, 2), Is.True);
            Assert.That(SaveHuntRules.ShouldAllowHunt(false, false, 5, 2), Is.True);
        }

        [Test]
        public void PostPlantHoldRules_EvacuateNearBoomWhenClear()
        {
            Assert.That(PostPlantHoldRules.BombRemaining(10f, 40f, 42f), Is.EqualTo(8f).Within(0.001f));
            Assert.That(PostPlantHoldRules.ShouldEvacuate(8f, false, false), Is.True);
            Assert.That(PostPlantHoldRules.ShouldEvacuate(9f, false, false), Is.False);
            Assert.That(PostPlantHoldRules.ShouldEvacuate(5f, true, false), Is.False);
            Assert.That(PostPlantHoldRules.ShouldEvacuate(5f, false, true), Is.False);
        }

        [Test]
        public void SpawnHoldRules_RemapAwayFromSpawnForBothSides()
        {
            Assert.That(SpawnHoldRules.IsSpawnZone("ct_spawn"), Is.True);
            Assert.That(SpawnHoldRules.IsSpawnZone("a_site"), Is.False);
            Assert.That(
                SpawnHoldRules.RemapAwayFromSpawn("ct_spawn", new[] { "ct_spawn", "a_site", "mid" }, "mid", "a_site"),
                Is.EqualTo("a_site"));
            Assert.That(
                SpawnHoldRules.RemapAwayFromSpawn("t_spawn", new[] { "t_spawn" }, "t_spawn", "a_site"),
                Is.EqualTo("a_site"));
            Assert.That(
                SpawnHoldRules.RemapAwayFromSpawn("cat", new[] { "ct_spawn", "cat" }, "mid", "a_site"),
                Is.EqualTo("cat"));
        }
"""
if "TerroristsDoNotHuntBeforePlant" in src:
    print("tests already present")
else:
    if anchor not in src:
        raise SystemExit("anchor missing")
    src = src.replace(anchor, insert, 1)
    p.write_text(src, encoding="utf-8")
    print("tests inserted")
