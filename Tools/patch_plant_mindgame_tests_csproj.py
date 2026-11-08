# -*- coding: utf-8 -*-
from pathlib import Path

cs = Path("TacticaUniversalis.Tests.EditMode.csproj")
t = cs.read_text(encoding="utf-8")
line = '    <Compile Include="Assets\\_Project\\Tests\\EditMode\\PlantMindGameTests.cs" />\n'
anchor = '    <Compile Include="Assets\\_Project\\Tests\\EditMode\\GeneralModeTests.cs" />\n'
if "PlantMindGameTests.cs" not in t:
    if anchor not in t:
        raise SystemExit("anchor missing: " + repr(anchor) + " in " + t[:500])
    cs.write_text(t.replace(anchor, anchor + line, 1), encoding="utf-8")
    print("csproj updated")
else:
    print("already in csproj")

sim = Path(r"Assets/_Project/Scripts/Domain/SimulationDomain.cs").read_text(encoding="utf-8")
tag_field = "reasonTags" if "public string[] reasonTags" in sim else "reasonTags"
if "public string[] reasonTags" in sim:
    tag_field = "reasonTags"
elif "public string[] reasonTags" in sim:
    tag_field = "reasonTags"
else:
    # sniff
    import re
    m = re.search(r"public string\[\] (reason\w+)", sim)
    tag_field = m.group(1) if m else "reasonTags"
print("tag_field", tag_field)

test = Path(r"Assets/_Project/Tests/EditMode/PlantMindGameTests.cs")
src = test.read_text(encoding="utf-8")
src = src.replace("e.reasonTags", "e." + tag_field)
# Acoustic API on disk
a = Path(r"Assets/_Project/Scripts/Domain/AcousticIntelRules.cs").read_text(encoding="utf-8")
# Ensure test uses ScanAudible / HearsThreatNear / BoardHasFreshTag / FreshTagZone
for want in ("ScanAudible", "HearsThreatNear", "BoardHasFreshTag", "FreshTagZone"):
    print(want, want in a)
test.write_text(src, encoding="utf-8")
print("test patched")
