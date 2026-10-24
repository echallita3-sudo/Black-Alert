from pathlib import Path
import json
req = ["ProjectSettings/ProjectVersion.txt","ProjectSettings/EditorBuildSettings.asset","Assets/Scenes/Entry.unity","Packages/manifest.json","Assets/Scripts/BlackAlertBootstrap.cs","Assets/Scripts/BattlefieldPrototype.cs","Assets/Editor/AndroidBuilder.cs","BLACK_ALERT_REQUIREMENTS.md"]
absent = [p for p in req if not Path(p).is_file()]
if absent: raise SystemExit("Missing: "+str(absent))
json.load(open("Packages/manifest.json"))
for p in Path("Assets").rglob("*.cs"):
    txt=p.read_text(encoding="utf8")
    assert txt.count("{")==txt.count("}"), str(p)+" unbalanced C# braces"
assert "Assets/Scenes/Entry.unity" in Path("ProjectSettings/EditorBuildSettings.asset").read_text(), "Boot scene absent from build settings"
assert "SceneRoots:" in Path("Assets/Scenes/Entry.unity").read_text(), "Boot scene missing root serialization"
print("Boot scene registered, manifest and C# structural sanity checks passed; NOT A UNITY COMPILATION.")
