using System.Collections.Generic;
using UnityEngine;
using BlackAlert.Simulation;

namespace BlackAlert
{
    // Small, self-contained testbed for scene boot, camera, simple physics and unit commands.
    // Not a substitute for the full production battle simulation or final animated models.
    public sealed class BattlefieldPrototype : MonoBehaviour
    {
        private sealed class Agent
        {
            public GameObject model;
            public bool enemy;
            public float hp = 100f;
            public Vector3 goal;
            public float cooldown;
            public float speed;
            public float altitude = .6f;
            public bool infantry;
            public bool naval;
            public float momentum;
            public float stunnedUntil;
            public bool selected;
        }

        private readonly List<Agent> agents = new List<Agent>();
        private Camera cam;
        private BlackAlertTerrain physicalTerrain;
        private BlackAlertPlayableModes modeController;
        public float BattleDuration { get { return battleTime; } }
        public float TerrainHeight(float x,float z) { return physicalTerrain!=null?physicalTerrain.Height(x,z):0f; }
        private Vector3 camFocus = new Vector3(0, 0, 0);
        private float camHeight = 53f;
        private Agent selected;
        private bool dragCamera;
        private Vector2 lastPointer;
        private Material friendMat;
        private Material foeMat;
        private Material earthMat;
        private Material waterMat;
        private int friendlyHQ = 1;
        private int enemyHQ = 1;
        private float battleTime;
        private int shotsFired;
        private float credits=5500;
        private float nuclearReadyAt;
        private bool targetingNuclear;

        private Material NewMaterial(Color color)
        {
            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        private GameObject Cube(string label, Vector3 position, Vector3 scale, Material mat, Transform parent = null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = label;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            if (parent != null) cube.transform.SetParent(parent, true);
            cube.GetComponent<Renderer>().sharedMaterial = mat;
            return cube;
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            friendMat = NewMaterial(new Color(0.12f, 0.6f, 0.95f));
            foeMat = NewMaterial(new Color(0.86f, 0.19f, 0.16f));
            earthMat = NewMaterial(new Color(0.20f, 0.31f, 0.25f));
            waterMat = NewMaterial(new Color(0.08f, 0.35f, 0.6f));
            BuildGround();
            BuildLighting();
            BuildBases();
            CreateStartingArmies();
            modeController=gameObject.AddComponent<BlackAlertPlayableModes>();
            modeController.Initialize(this);
        }

        private void CreateStartingArmies()
        {
                for (int i = 0; i < 5; i++)
                {
                    Spawn(new Vector3(-27 + i*2.8f, 0, -9 + i%3*3f),false);
                    Spawn(new Vector3(23 + i*2.8f, 0, 9 + i%3*3f),true);
                }
                for (int i=0; i<6; i++)
                {
                    SpawnInfantry(new Vector3(-25+i*1.6f,0,-16-i%2*1.7f),false);
                    SpawnInfantry(new Vector3(22+i*1.6f,0,16+i%2*1.7f),true);
                }
                SpawnHelicopter(new Vector3(-26f,7f,24f), false);
                SpawnHelicopter(new Vector3(26f,7f,-24f), true);
                SpawnJet(new Vector3(-19f,13f,28f),false);
                SpawnJet(new Vector3(19f,13f,-28f),true);
                SpawnShip(new Vector3(0,.7f,-35f),false);
                SpawnShip(new Vector3(0,.7f,35f),true);
        }

        public void RestartBattle()
        {
            StopAllCoroutines();
            foreach(var unit in agents)if(unit.model!=null)Destroy(unit.model);
            agents.Clear();
            selected=null;
            credits=5500;
            battleTime=0;
            shotsFired=0;
            targetingNuclear=false;
            nuclearReadyAt=0;
            CreateStartingArmies();
        }

        public void SetDuelCamera(bool enabled)
        {
            camFocus=enabled?Vector3.zero:new Vector3(0,0,0);
            camHeight=enabled?29f:53f;
            AlignCamera();
        }

        public int CountLivingUnits(bool hostile)
        {
            int alive=0;
            foreach(var a in agents) if(a.enemy==hostile && a.model!=null && a.hp>0) alive++;
            return alive;
        }

        public int CountUnitsNear(Vector3 where,bool hostile,float radius)
        {
            int alive=0;
            float r2=radius*radius;
            foreach(var a in agents)
            {
                if(a.model==null || a.enemy!=hostile || a.hp<=0)continue;
                Vector3 d=a.model.transform.position-where;d.y=0f;
                if(d.sqrMagnitude<=r2)alive++;
            }
            return alive;
        }

        public void DamageOutsideCircle(float radius,float damage)
        {
            float radiusSquared=radius*radius;
            foreach(var a in agents)
            {
                if(a.model==null || a.hp<=0)continue;
                Vector3 p=a.model.transform.position;p.y=0;
                if(p.sqrMagnitude>radiusSquared)
                    ApplyDamage(a,damage);
            }
        }

        private void ApplyDamage(Agent target,float damage)
        {
            if(target==null || target.model==null || target.hp<=0)return;
            target.hp=Mathf.Max(0,target.hp-damage);
            if(target.hp<=0)
            {
                if(selected==target)selected=null;
                Destroy(target.model);
                target.model=null;
            }
        }

        public void ApplyCardEffect(CardActivation card)
        {
            if(card==null)return;
            Vector3 enemyCenter=new Vector3(22,0,0);
            var enemy=agents.Find(a=>a.enemy && a.model!=null && a.hp>0);
            if(enemy!=null)enemyCenter=enemy.model.transform.position;
            switch(card.Effect)
            {
                case CardEffect.Artillery:
                case CardEffect.Airstrike:
                    StartCoroutine(TravelShell(enemyCenter+Vector3.up*30f,enemyCenter));
                    AreaDamage(enemyCenter,card.Effect==CardEffect.Airstrike?18f:10f,
                        card.Effect==CardEffect.Airstrike?65f:40f,true);
                    break;
                case CardEffect.EMP:
                case CardEffect.Freeze:
                    foreach(var a in agents)
                        if(a.enemy && a.model!=null &&
                           Vector3.Distance(a.model.transform.position,enemyCenter)<20f)
                            a.stunnedUntil=Time.time+(card.Effect==CardEffect.EMP?8f:5f);
                    break;
                case CardEffect.Repair:
                case CardEffect.Shield:
                    foreach(var a in agents)if(!a.enemy && a.model!=null)
                        a.hp=Mathf.Min(a.hp+(card.Effect==CardEffect.Repair?35f:55f),150f);
                    break;
                case CardEffect.Recon:
                    camFocus=enemyCenter;AlignCamera();
                    break;
                case CardEffect.Supply:
                    credits+=900;
                    break;
                case CardEffect.Reinforce:
                case CardEffect.Decoy:
                    for(int i=0;i<3;i++)
                        SpawnInfantry(new Vector3(-24+i*2f,0,5f+i),false);
                    break;
                case CardEffect.AntiAir:
                    foreach(var a in agents)if(a.enemy && a.model!=null && a.altitude>2f)
                        ApplyDamage(a,30f);
                    break;
                case CardEffect.NuclearJammer:
                    // Opponent superweapon cooldown logic is not yet integrated.
                    credits+=500;
                    break;
            }
        }

        private void AreaDamage(Vector3 center,float radius,float amount,bool enemyOnly)
        {
            foreach(var a in agents)
            {
                if(a.model==null || a.hp<=0)continue;
                if(enemyOnly && !a.enemy)continue;
                float distance=Vector3.Distance(a.model.transform.position,center);
                if(distance<radius) ApplyDamage(a,amount*Mathf.Clamp01(1f-distance/radius));
            }
        }

        private void BuildGround()
        {
            var world = new GameObject("Black Alert - 3D landscape / physics");
            physicalTerrain = world.AddComponent<BlackAlertTerrain>();
            physicalTerrain.Build();
            Cube("Water material under surface", new Vector3(0,-2.9f,0),
                new Vector3(100,0.1f,90), waterMat);
            for (int z = -42; z <= 42; z += 3)
            {
                float start = physicalTerrain.RiverCenter(z);
                Cube("Water river", new Vector3(start,.02f,z+1.5f),
                    new Vector3(5.2f,.09f,3.1f), waterMat);
            }
            var bridgeMat = NewMaterial(new Color(.36f,.35f,.32f));
            foreach (int z in new int[] {-20,20})
                Cube("Steel river bridge", new Vector3(physicalTerrain.RiverCenter(z),.35f,z),
                    new Vector3(9f,.55f,5.1f),bridgeMat);
        }

        private void BuildLighting()
        {
            var l = new GameObject("Sun").AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.5f;
            l.transform.rotation = Quaternion.Euler(48,-30,0);
            var go = new GameObject("RTS Camera");
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 50;
            AlignCamera();
        }

        private void BuildBases()
        {
            Cube("Azure Command HQ",new Vector3(-33,3,-2),new Vector3(8,6,10),friendMat);
            Cube("Crimson Command HQ",new Vector3(33,3,2),new Vector3(8,6,10),foeMat);
            BlackAlertModelFactory.CreateFactory(new Vector3(-35,0,22),false);
            BlackAlertModelFactory.CreateFactory(new Vector3(35,0,-22),true);
            BlackAlertAdditionalModels.Reactor(new Vector3(-36,0,-28),false);
            BlackAlertAdditionalModels.Reactor(new Vector3(36,0,28),true);
        }

        private void Spawn(Vector3 p,bool hostile)
        {
            p.y=.6f+(physicalTerrain!=null?physicalTerrain.Height(p.x,p.z):0);
            var model=BlackAlertModelFactory.CreateTank(p,hostile);
            agents.Add(new Agent {model=model,enemy=hostile,
                goal=hostile?new Vector3(-22,0,p.z):p,speed=hostile?3.1f:3.8f,altitude=.6f});
        }
        private void SpawnInfantry(Vector3 p,bool hostile)
        {
            p.y=(physicalTerrain!=null?physicalTerrain.Height(p.x,p.z):0);
            var model=BlackAlertAdditionalModels.Rifleman(p,hostile);
            agents.Add(new Agent {model=model,enemy=hostile,
                goal=hostile?new Vector3(-21,0,p.z):p,speed=hostile?3.7f:4.2f,altitude=.1f,infantry=true});
        }
        private void SpawnHelicopter(Vector3 p,bool hostile)
        {
            var model=BlackAlertModelFactory.CreateHelicopter(p,hostile);
            agents.Add(new Agent {model=model,enemy=hostile,
                goal=hostile?new Vector3(-20,0,p.z):p,speed=hostile?5f:5.5f,altitude=7f});
        }
        private void SpawnJet(Vector3 p,bool hostile)
        {
            var model=BlackAlertAdditionalModels.Jet(p,hostile);
            agents.Add(new Agent {model=model,enemy=hostile,
                goal=hostile?new Vector3(-22,0,p.z):p,speed=hostile?10f:11f,altitude=13f});
        }
        private void SpawnShip(Vector3 p,bool hostile)
        {
            p.x=physicalTerrain!=null?physicalTerrain.RiverCenter(p.z):0;
            var model=BlackAlertAdditionalModels.Destroyer(p,hostile);
            agents.Add(new Agent {model=model,enemy=hostile,
                goal=new Vector3(p.x,0,hostile?-18:18),speed=hostile?2.7f:2.3f,
                altitude=.7f,naval=true});
        }

        private void Update()
        {
            battleTime += Time.deltaTime;
            credits += Time.deltaTime*5f;
            UpdateInput();
            Simulate();
        }

        private void UpdateInput()
        {
            if (cam == null) return;
            if (Input.touchCount >= 2)
            {
                var a=Input.GetTouch(0); var b=Input.GetTouch(1);
                var prev=(a.position-a.deltaPosition)-(b.position-b.deltaPosition);
                float before=prev.magnitude, now=(a.position-b.position).magnitude;
                camHeight=Mathf.Clamp(camHeight+(before-now)*0.05f,20f,100f);
                camFocus -= new Vector3((a.deltaPosition.x+b.deltaPosition.x)*0.025f,0,(a.deltaPosition.y+b.deltaPosition.y)*0.025f);
                AlignCamera();
                return;
            }
            if (Input.touchCount == 1)
            {
                var t=Input.GetTouch(0);
                if (t.phase==TouchPhase.Began) {lastPointer=t.position;dragCamera=false;}
                else if (t.phase==TouchPhase.Moved)
                {
                    if ((t.position-lastPointer).magnitude > 7f) dragCamera=true;
                    if (dragCamera) {camFocus -= new Vector3(t.deltaPosition.x*0.025f,0,t.deltaPosition.y*0.025f); AlignCamera();}
                }
                else if (t.phase==TouchPhase.Ended && !dragCamera &&
                         (modeController==null || !modeController.BlocksWorldTouch(t.position)))
                    SelectOrOrder(t.position);
                return;
            }
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0) &&
                (modeController==null || !modeController.BlocksWorldTouch(Input.mousePosition)))
                SelectOrOrder(Input.mousePosition);
            camHeight=Mathf.Clamp(camHeight-Input.mouseScrollDelta.y*3f,20f,100f);
            AlignCamera();
#endif
        }

        private void AlignCamera()
        {
            if (cam == null) return;
            camFocus.x=Mathf.Clamp(camFocus.x,-45,45);
            camFocus.z=Mathf.Clamp(camFocus.z,-39,39);
            cam.transform.position=camFocus + new Vector3(0,camHeight,-camHeight*0.65f);
            cam.transform.rotation=Quaternion.LookRotation(camFocus-cam.transform.position,Vector3.up);
        }

        private void SelectOrOrder(Vector2 point)
        {
            if (!Physics.Raycast(cam.ScreenPointToRay(point),out RaycastHit hit,500f)) return;
            if(targetingNuclear)
            {
                targetingNuclear=false;
                if(credits>=3800f && Time.time>=nuclearReadyAt)
                {
                    credits-=3800f;
                    nuclearReadyAt=Time.time+80f;
                    StartCoroutine(NuclearStrikeAt(hit.point));
                }
                return;
            }
            Agent clicked=null;
            foreach(var a in agents)
            {
                if(a.model != null && hit.collider != null && hit.collider.gameObject.transform.IsChildOf(a.model.transform))
                    {clicked=a;break;}
            }
            if(clicked!=null && !clicked.enemy)
            {
                if(selected!=null && selected.model!=null)
                {
                    var oldRing=selected.model.transform.Find("Selection Ring");
                    if(oldRing!=null) oldRing.gameObject.SetActive(false);
                }
                selected=clicked;
                var newRing=clicked.model.transform.Find("Selection Ring");
                if(newRing!=null) newRing.gameObject.SetActive(true);
                return;
            }
            if(selected!=null && selected.model!=null)
            {
                selected.goal=new Vector3(Mathf.Clamp(hit.point.x,-47,47),0,Mathf.Clamp(hit.point.z,-43,43));
                if(clicked!=null && clicked.enemy) selected.goal=clicked.model.transform.position;
            }
        }

        private void Simulate()
        {
            foreach(var a in agents)
            {
                if(a.model==null || a.hp<=0) continue;
                Agent enemy=null; float best=999999;
                foreach(var b in agents)
                {
                    if(b.enemy==a.enemy || b.hp<=0 || b.model==null) continue;
                    var d=(b.model.transform.position-a.model.transform.position).sqrMagnitude;
                    if(d<best) {best=d;enemy=b;}
                }
                if(a.enemy && enemy!=null && best<2500f) a.goal=enemy.model.transform.position;
                if(a.naval && physicalTerrain!=null)
                    a.goal=new Vector3(physicalTerrain.RiverCenter(a.goal.z),.7f,
                        Mathf.Clamp(a.goal.z,-40f,40f));
                var position=a.model.transform.position;
                var flatGoal=new Vector3(a.goal.x,
                    a.naval ? .7f : a.altitude>2f ? a.altitude :
                    a.altitude+(physicalTerrain!=null?physicalTerrain.Height(a.goal.x,a.goal.z):0),
                    a.goal.z);
                var diff=flatGoal-position;
                if(diff.sqrMagnitude>(a.infantry ? .55f : 4.5f) && (enemy==null || best>70f || !a.enemy))
                {
                    float speedFactor=1f;
                    if(physicalTerrain!=null && a.altitude<=2f && !a.naval)
                    {
                        var probe=Vector3.MoveTowards(position,flatGoal,Time.deltaTime*a.speed);
                        if(!physicalTerrain.CanCross(probe,a.infantry,!a.infantry))
                        {
                            a.goal=position;
                            continue;
                        }
                        speedFactor=physicalTerrain.SpeedFactor(position,a.infantry,!a.infantry);
                    }
                    // Massive ships accelerate slowly; jets and helicopters fly above terrain.
                    if(a.naval)
                    {
                        a.momentum=Mathf.MoveTowards(a.momentum,a.speed,Time.deltaTime*.5f);
                        speedFactor=Mathf.Max(.1f,a.momentum/a.speed);
                    }
                    position=Vector3.MoveTowards(position,flatGoal,Time.deltaTime*a.speed*speedFactor);
                    if(a.naval)position.y=.7f;
                    else if(a.altitude<=2f && physicalTerrain!=null)
                        position.y=a.altitude+physicalTerrain.Height(position.x,position.z);
                    a.model.transform.position=position;
                    if(diff.sqrMagnitude>0.1f)
                        a.model.transform.rotation=Quaternion.Slerp(a.model.transform.rotation,
                            Quaternion.LookRotation(new Vector3(diff.x,0,diff.z)),
                            Time.deltaTime*(a.naval ? .55f : 4f));
                }
                else if(a.naval)
                    a.momentum=Mathf.MoveTowards(a.momentum,0,Time.deltaTime*.32f);
                if(enemy!=null)
                {
                    var motion=a.model.GetComponent<BlackAlertVisualMotion>();
                    if(motion != null && motion.turret != null)
                    {
                        Vector3 toward=enemy.model.transform.position-a.model.transform.position;
                        toward.y=0;
                        if(toward.sqrMagnitude>.01f)
                            motion.turret.rotation=Quaternion.Slerp(motion.turret.rotation,Quaternion.LookRotation(toward),Time.deltaTime*5f);
                    }
                }
                a.cooldown-=Time.deltaTime;
                if(Time.time<a.stunnedUntil)continue;
                if(enemy!=null && best < 120f && a.cooldown<=0)
                {
                    a.cooldown=a.infantry ? .8f : (a.naval ? 2.6f : 1.3f);
                    shotsFired++;
                    BlackAlertSoundFx.Shot(position);
                    enemy.hp-=a.infantry?6f:a.naval?34f:18f;
                    if(enemy.hp<=0) {Object.Destroy(enemy.model);enemy.model=null;}
                    SpawnTracer(position+Vector3.up,enemy.model!=null?enemy.model.transform.position+Vector3.up:position+Vector3.forward*3);
                }
            }
        }

        private void SpawnTracer(Vector3 start,Vector3 end)
        {
            StartCoroutine(TravelShell(start,end));
        }
        private System.Collections.IEnumerator TravelShell(Vector3 start,Vector3 end)
        {
            var projectile=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name="Animated combat shell";
            projectile.transform.localScale=Vector3.one*.37f;
            projectile.GetComponent<Renderer>().sharedMaterial=NewMaterial(new Color(1f,.68f,.16f));
            Object.Destroy(projectile.GetComponent<Collider>());
            float t=0;
            while(t<1f)
            {
                t+=Time.deltaTime*4f;
                var arc=Vector3.up*(Mathf.Sin(Mathf.Clamp01(t)*Mathf.PI)*3.4f);
                projectile.transform.position=Vector3.Lerp(start,end,Mathf.Clamp01(t))+arc;
                yield return null;
            }
            projectile.name="Impact flare";
            float radius=0;
            while(radius<1.7f)
            {
                radius+=Time.deltaTime*6f;
                projectile.transform.localScale=Vector3.one*radius;
                yield return null;
            }
            Object.Destroy(projectile);
        }

        // Physically placed, animated 3D missile and blast volume — pre-alpha effect, not final VFX.
        private System.Collections.IEnumerator NuclearStrikeAt(Vector3 target)
        {
            target.y=physicalTerrain!=null ? physicalTerrain.Height(target.x,target.z):0;
            var payload=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            payload.name="Descending ballistic warhead";
            payload.transform.localScale=new Vector3(.85f,2.2f,.85f);
            var warheadMaterial=NewMaterial(new Color(.42f,.42f,.43f));
            payload.GetComponent<Renderer>().sharedMaterial=warheadMaterial;
            Destroy(payload.GetComponent<Collider>());
            var start=target+Vector3.up*70f;
            BlackAlertSoundFx.Warning(target);
            float t=0f;
            while(t<3.6f)
            {
                t+=Time.deltaTime;
                payload.transform.position=Vector3.Lerp(start,target+Vector3.up,
                    Mathf.Clamp01(t/3.6f));
                payload.transform.Rotate(Vector3.up,Time.deltaTime*24f);
                yield return null;
            }
            Destroy(payload);
            BlackAlertSoundFx.Explosion(target);
            foreach(var unit in agents)
            {
                if(unit.model==null || unit.hp<=0)continue;
                float distance=Vector3.Distance(target,unit.model.transform.position);
                if(distance<20f)
                {
                    unit.hp-=320f*(1f-distance/22f);
                    if(unit.hp<=0)
                    {
                        Destroy(unit.model); unit.model=null;
                    }
                    else
                    {
                        var direction=(unit.model.transform.position-target);
                        direction.y=0;
                        if(direction.sqrMagnitude>.01f)
                            unit.model.transform.position+=direction.normalized*(20f-distance)*.22f;
                    }
                }
            }
            var flash=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name="Nuclear hot core / real-time animated";
            flash.transform.position=target+Vector3.up*4f;
            flash.GetComponent<Renderer>().sharedMaterial=NewMaterial(new Color(1f,.57f,.11f));
            Destroy(flash.GetComponent<Collider>());
            var shock=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shock.name="Nuclear radial blast wave";
            shock.transform.position=target+Vector3.up*.15f;
            shock.GetComponent<Renderer>().sharedMaterial=NewMaterial(new Color(.97f,.66f,.24f));
            Destroy(shock.GetComponent<Collider>());
            var ash=NewMaterial(new Color(.17f,.16f,.16f));
            float age=0;
            while(age<3.8f)
            {
                age+=Time.deltaTime;
                float radius=1f+age*7f;
                flash.transform.localScale=Vector3.one*(1f+age*4.7f);
                shock.transform.localScale=new Vector3(radius,.035f,radius);
                flash.transform.position=target+Vector3.up*(4f+age*4f);
                yield return null;
            }
            Destroy(flash);
            Destroy(shock);
            var crater=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crater.name="Nuclear scorched crater";
            crater.transform.position=target+Vector3.up*.1f;
            crater.transform.localScale=new Vector3(14f,.07f,14f);
            crater.GetComponent<Renderer>().sharedMaterial=ash;
            Destroy(crater.GetComponent<Collider>());
            Destroy(crater,85f);
        }

        private void OnGUI()
        {
            GUI.skin.button.fontSize=18;
            GUI.skin.label.fontSize=17;
            GUI.Label(new Rect(20, 20, 520, 55), "BLACK ALERT / 3D LIVE COMBAT LAB");
            GUI.Label(new Rect(20, 82, 420, 30), "SKIRMISH TECH TEST — NOT FINAL ART");
            GUI.Label(new Rect(20, 105, 700, 30), "Tap any Azure unit, then ground to move. Two fingers pan / pinch zoom.");
            GUI.Label(new Rect(20, 132, 700, 30), "Credits "+(int)credits+"  Shots "+shotsFired+
                "  Time "+battleTime.ToString("0")+"s");
            GUI.Label(new Rect(20, 160, 700, 30), selected!=null&&selected.model!=null?
                "Selected: "+selected.model.name:"Select an Azure unit to command");
            if(GUI.Button(new Rect(20, 195, 195, 55), "BUILD TANK $700") && credits>=700)
            {credits-=700;Spawn(new Vector3(-28,0,Random.Range(-20f,20f)),false);}
            if(GUI.Button(new Rect(20, 257, 195, 55), "RIFLEMAN $100") && credits>=100)
            {credits-=100;SpawnInfantry(new Vector3(-27,0,Random.Range(-19f,19f)),false);}
            if(GUI.Button(new Rect(20, 319, 195, 55), "HELICOPTER $1200") && credits>=1200)
            {credits-=1200;SpawnHelicopter(new Vector3(-27,7,Random.Range(-19f,19f)),false);}
            if(GUI.Button(new Rect(20, 381, 195, 55), "JET $1600") && credits>=1600)
            {credits-=1600;SpawnJet(new Vector3(-28,13,Random.Range(-19f,19f)),false);}
            if(GUI.Button(new Rect(20, 443, 195, 55), "DESTROYER $2200") && credits>=2200)
            {credits-=2200;SpawnShip(new Vector3(0,.7f,-32),false);}
            var nukeLabel=Time.time<nuclearReadyAt ?
                "NUKE RECHARGING "+Mathf.CeilToInt(nuclearReadyAt-Time.time)+"s":
                targetingNuclear ? "TAP TARGET ON MAP" : "NUCLEAR $3800";
            if(GUI.Button(new Rect(20,505,195,55),nukeLabel) &&
                Time.time>=nuclearReadyAt && credits>=3800)
                targetingNuclear=!targetingNuclear;
            GUI.Label(new Rect(20,574,680,40), "TEST GEOMETRY - NOT FINAL GRAPHICS OR ONLINE PLAY");
        }
    }
}
