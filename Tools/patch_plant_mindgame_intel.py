# -*- coding: utf-8 -*-
from pathlib import Path

path = Path(r"Assets/_Project/Scripts/Domain/CombatIntelDomain.cs")
text = path.read_text(encoding="utf-8")
old = """    public sealed class IntelNote
    {
        public string ZoneId;
        public int Count;
        public string[] WeaponIds = Array.Empty<string>();
        public float SeenAt;
        public string ReporterId;
    }
"""
new = """    public sealed class IntelNote
    {
        public string ZoneId;
        public int Count;
        public string[] WeaponIds = Array.Empty<string>();
        public float SeenAt;
        public string ReporterId;
        /// <summary>Acoustic / action tags: planting, footsteps, defusing.</summary>
        public string[] Tags = Array.Empty<string>();
    }
"""
if old not in text:
    raise SystemExit("IntelNote block missing")
if "public string[] Tags" not in text:
    text = text.replace(old, new, 1)
    path.write_text(text, encoding="utf-8")
    print("CombatIntelDomain: Tags added")
else:
    print("CombatIntelDomain: Tags already present")

csproj = Path("TacticaUniversalis.Domain.csproj")
proj = csproj.read_text(encoding="utf-8")
anchor = '    <Compile Include="Assets\\_Project\\Scripts\\Domain\\CombatIntelDomain.cs" />\n'
if anchor not in proj:
    raise SystemExit("csproj anchor missing")
for compile in [
    "Assets\\_Project\\Scripts\\Domain\\AcousticIntelRules.cs",
    "Assets\\_Project\\Scripts\\Domain\\PlantChannelRules.cs",
]:
    line = f'    <Compile Include="{compile}" />\n'
    if compile not in proj:
        proj = proj.replace(anchor, anchor + line, 1)
        print("csproj added", compile)
    else:
        print("csproj already has", compile)
csproj.write_text(proj, encoding="utf-8")
print("done intel+csproj")
