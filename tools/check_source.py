from pathlib import Path
import json
req = ["ProjectSettings/ProjectVersion.txt","Packages/manifest.json","Assets/Scripts/BlackAlertBootstrap.cs","Assets/Scripts/BattlefieldPrototype.cs","Assets/Editor/AndroidBuilder.cs","BLACK_ALERT_REQUIREMENTS.md"]
absent = [p for p in req if not Path(p).is_file()]
if absent: raise SystemExit("Missing: "+str(absent))
json.load(open("Packages/manifest.json"))
for p in Path("Assets").rglob("*.cs"):
    txt=p.read_text(encoding="utf8")
    assert txt.count("{")==txt.count("}"), str(p)+" unbalanced C# braces"
print("Source manifest check passed; THIS IS NOT A UNITY COMPILATION OR DEVICE TEST")
