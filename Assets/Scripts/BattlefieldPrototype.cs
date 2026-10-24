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
            public AiUnitType type;
            public bool aiRetreat;
            public int preferredTargetId=-1;
            public float hp = 100f;
            public Vector3 goal;
            public float cooldown;
            public float speed;
            public float altitude = .6f;
            public bool infantry;
            public bool naval;
            public float momentum;
            public float stunnedUntil;
            public float nextPathAt;
            public readonly Queue<Vector3> route = new Queue<Vector3>();
            public bool selected;
        }

        private readonly List<Agent> agents = new List<Agent>();
        private Camera cam;
        private BlackAlertTerrain physicalTerrain;
        private BlackAlertPlayableModes modeController;
        private BlackAlertTacticalIntel tacticalIntel;
        public float BattleDuration { get { return battleTime; } }
        public float TerrainHeight(float x,float z) { return physicalTerrain!=null?physicalTerrain.Height(x,z):0f; }

        public bool IsWater(Vector3 p) { return physicalTerrain!=null && physicalTerrain.IsWater(p); }
        public void SetFogMask(Texture2D texture)
        {
            if(physicalTerrain!=null)physicalTerrain.SetFogMask(texture);
        }
        public void JumpCameraTo(Vector3 target)
        {
            camFocus=new Vector3(target.x,0,target.z);
            AlignCamera();
        }
        public void EnumerateScoutPositions(System.Action<bool,Vector3,int> visitor)
        {
            if(visitor==null)return;
            foreach(var unit in agents)
            {
                if(unit.model==null || unit.hp<=0)continue;
                int radius=unit.infantry?10:unit.naval?15:
                    unit.altitude>9f?22:unit.altitude>2f?19:14;
                visitor(unit.enemy,unit.model.transform.position,radius);
            }
        }
        public void ApplyEnemyVisibility(System.Func<Vector3,bool> isVisible)
        {
            if(isVisible==null)return;
            foreach(var unit in agents)
            {
                if(!unit.enemy || unit.model==null || unit.hp<=0)continue;
                bool visible=isVisible(unit.model.transform.position);
                if(unit.model.activeSelf!=visible)
                    unit.model.SetActive(visible);
            }
        }

        private Vector3 camFocus = new Vector3(0, 0, 0);
        private float camHeight = 53f;
        private Agent selected;
        private readonly List<Agent> selectedGroup=new List<Agent>();
        private bool dragCamera;
        private bool dragSelect;
        private bool startedOnHud;
        private Vector2 dragOrigin;
        private Vector2 dragEndpoint;
        private float fingerStartTime;
        private float lastTapTime=-100f;
        private string lastTappedType="";
        private Vector2 lastPointer;
        private Material friendMat;
        private Material foeMat;
        private Material earthMat;
        private Material waterMat;
        private StrongholdBattle headquarters;
        private GameObject azureHeadquarters,crimsonHeadquarters;
        private float battleTime;
        private int shotsFired;
        private RtsEconomy economy;
        private RtsEconomy crimsonEconomy;
        private TacticalCommanderBrain tacticalBrain;
        private AiStrategy lastEnemyStrategy=AiStrategy.Hold;
        private float nextEnemyDecision;
        private BlackAlertOreHarvester harvester;
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
            var riverShader=Resources.Load<Shader>("Shaders/WaterBundled");
            if(riverShader==null)riverShader=Shader.Find("BlackAlert/RiverWater");
            waterMat=riverShader!=null
                ? new Material(riverShader)
                : NewMaterial(new Color(.08f,.35f,.6f));
            BuildGround();
            BuildLighting();
            BuildBases();
            headquarters=new StrongholdBattle();
            StartEconomy();
            CreateStartingArmies();
            modeController=gameObject.AddComponent<BlackAlertPlayableModes>();
            modeController.Initialize(this);
            tacticalIntel=gameObject.AddComponent<BlackAlertTacticalIntel>();
            tacticalIntel.Initialize(this);
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

        private void StartEconomy()
        {
            if(harvester!=null)
                Destroy(harvester.gameObject);
            economy=new RtsEconomy(5500);
            economy.AddOreNode("Azure Ore Field",5000);
            var oreVehicle=new GameObject("Azure Ore Harvester");
            harvester=oreVehicle.AddComponent<BlackAlertOreHarvester>();
            harvester.Configure(economy,physicalTerrain,"Azure Ore Field",
                new Vector3(-31,0,-24),new Vector3(-19,0,-31));
            // The opponent uses a virtual (not yet animated) ore-delivery truck.
            crimsonEconomy=new RtsEconomy(3000);
            crimsonEconomy.AddOreNode("Crimson Ore Field",4600);
            crimsonEconomy.AssignHarvester("Crimson Ore Field");
            tacticalBrain=new TacticalCommanderBrain(2);
            lastEnemyStrategy=AiStrategy.Hold;
            nextEnemyDecision=0f;
        }

        private void DeliverProduction(string id,bool hostile=false)
        {
            if(CountLivingUnits(hostile)>=32)return;
            float x=hostile?27f:-27f;
            float z=Random.Range(-16f,16f);
            switch(id)
            {
                case "rifleman": SpawnInfantry(new Vector3(x,0,z),hostile);break;
                case "tank": Spawn(new Vector3(x,0,z),hostile);break;
                case "helicopter": SpawnHelicopter(new Vector3(x,7,z),hostile);break;
                case "jet": SpawnJet(new Vector3(x,13,z),hostile);break;
                case "destroyer": SpawnShip(new Vector3(0,.7f,hostile?32:-32),hostile);break;
            }
        }

        private void ThinkEnemyCommander()
        {
            if(tacticalBrain==null || crimsonEconomy==null ||
               Time.time<nextEnemyDecision)return;
            nextEnemyDecision=Time.time+4.5f;

            var owned=new List<AiUnitIntel>();
            var observed=new List<AiUnitIntel>();
            for(int i=0;i<agents.Count;i++)
            {
                Agent agent=agents[i];
                if(agent.model==null || agent.hp<=0)continue;
                Vector3 p=agent.model.transform.position;
                var snapshot=new AiUnitIntel(i,agent.type,p.x,p.z,
                    Mathf.Clamp01(agent.hp/100f));
                if(agent.enemy)owned.Add(snapshot);
                else if(tacticalIntel==null || tacticalIntel.CanSpot(true,p))
                    observed.Add(snapshot);
            }
            var mode=modeController!=null?modeController.Mode:BattleMode.Skirmish;
            var objectives=modeController!=null?modeController.GetCommanderObjectives():
                new List<AiObjective>();
            var situation=new AiCommanderSituation(mode,32f,0f,-32f,0f,
                crimsonEconomy.Credits,crimsonEconomy.TechLevel,
                crimsonEconomy.PendingUnits,crimsonEconomy.LowPower,
                owned,observed,objectives,battleTime);
            var plan=tacticalBrain.Decide(situation);
            lastEnemyStrategy=plan.Strategy;
            if(plan.Structure!=null)crimsonEconomy.BuildStructure(plan.Structure);
            if(plan.ProductionUnit!=null)crimsonEconomy.QueueUnit(plan.ProductionUnit);

            foreach(var command in plan.Orders)
            {
                if(command.UnitId<0 || command.UnitId>=agents.Count)continue;
                var agent=agents[command.UnitId];
                if(!agent.enemy || agent.model==null || agent.hp<=0)continue;
                agent.aiRetreat=command.Action==AiOrderType.Retreat;
                agent.preferredTargetId=command.Action==AiOrderType.Engage ?
                    command.TargetEnemyId : -1;
                var target=new Vector3(Mathf.Clamp(command.TargetX,-45f,45f),
                    0f,Mathf.Clamp(command.TargetZ,-40f,40f));
                // Replan only if the new tactical goal is meaningfully different.
                Vector3 previous=agent.route.Count>0?agent.route.ToArray()[agent.route.Count-1]:agent.goal;
                if((target-previous).sqrMagnitude<16f && agent.route.Count>0)continue;
                if(agent.altitude>2f || agent.naval)
                    agent.goal=target;
                else
                    PlanRoute(agent,target);
            }
        }

        public void RestartBattle()
        {
            StopAllCoroutines();
            BlackAlertCombatEffects.Clear();
            foreach(var unit in agents)if(unit.model!=null)Destroy(unit.model);
            agents.Clear();
            selected=null;
            selectedGroup.Clear();
            StartEconomy();
            battleTime=0;
            shotsFired=0;
            targetingNuclear=false;
            nuclearReadyAt=0;
            if(headquarters!=null)headquarters.Reset();
            if(azureHeadquarters!=null)azureHeadquarters.SetActive(true);
            if(crimsonHeadquarters!=null)crimsonHeadquarters.SetActive(true);
            CreateStartingArmies();
        }

        public void SetDuelCamera(bool enabled)
        {
            camFocus=enabled?Vector3.zero:new Vector3(0,0,0);
            camHeight=enabled?29f:53f;
            AlignCamera();
        }

        public int HeadquartersHP(bool hostile)
        {
            return headquarters==null?0:headquarters.Health(hostile?1:0);
        }

        private void DamageHeadquarters(int targetFaction,int amount,Vector3 impact)
        {
            if(headquarters==null || headquarters.Finished ||
               headquarters.Health(targetFaction)==0)return;
            int applied=headquarters.Damage(targetFaction,amount);
            if(applied>0)BlackAlertCombatEffects.Hit(impact);
            if(headquarters.Health(targetFaction)>0)return;
            var model=targetFaction==0?azureHeadquarters:crimsonHeadquarters;
            if(model!=null)
            {
                var explosionPoint=model.transform.position;
                BlackAlertSoundFx.Explosion(explosionPoint);
                BlackAlertCombatEffects.NuclearDetonation(explosionPoint);
                var rubbleMaterial=NewMaterial(new Color(.24f,.23f,.22f));
                for(int i=0;i<12;i++)
                {
                    var debris=GameObject.CreatePrimitive(PrimitiveType.Cube);
                    debris.name="Physics-enabled destroyed command HQ wreckage";
                    debris.transform.position=explosionPoint+
                        new Vector3(Random.Range(-3.0f,3.0f),Random.Range(0f,3.0f),
                            Random.Range(-3f,3f));
                    debris.transform.localScale=new Vector3(Random.Range(.6f,1.7f),
                        Random.Range(.3f,1.4f),Random.Range(.7f,1.9f));
                    debris.GetComponent<Renderer>().sharedMaterial=rubbleMaterial;
                    var rb=debris.AddComponent<Rigidbody>();
                    rb.mass=Random.Range(12f,40f);
                    rb.AddExplosionForce(220f,explosionPoint-Vector3.up,12f,1.4f,
                        ForceMode.Impulse);
                    Destroy(debris,14f);
                }
                model.SetActive(false);
            }
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
                selectedGroup.Remove(target);
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
                    economy.AddCredits(900);
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
                    // Enemy nuclear availability is not yet implemented; this is a temporary energy refund.
                    economy.AddCredits(500);
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
            l.type=LightType.Directional;
            l.intensity=1.25f;
            l.color=new Color(1f,.94f,.82f);
            l.shadows=LightShadows.Soft;
            l.shadowStrength=.60f;
            l.transform.rotation=Quaternion.Euler(48,-30,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.37f,.43f,.51f);
            RenderSettings.fog=true;
            RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=new Color(.37f,.44f,.51f);
            RenderSettings.fogDensity=.0075f;
            QualitySettings.shadowDistance=70f;
            var go = new GameObject("RTS Camera");
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color(.39f,.46f,.52f);
            cam.fieldOfView=48f;
            cam.nearClipPlane=.1f;
            cam.farClipPlane=220f;
            AlignCamera();
        }

        private void BuildBases()
        {
            azureHeadquarters=Cube("Azure Command HQ",new Vector3(-33,3,-2),
                new Vector3(8,6,10),friendMat);
            crimsonHeadquarters=Cube("Crimson Command HQ",new Vector3(33,3,2),
                new Vector3(8,6,10),foeMat);
            BlackAlertModelFactory.CreateFactory(new Vector3(-35,0,22),false);
            BlackAlertModelFactory.CreateFactory(new Vector3(35,0,-22),true);
            BlackAlertAdditionalModels.Reactor(new Vector3(-36,0,-28),false);
            BlackAlertAdditionalModels.Reactor(new Vector3(36,0,28),true);
        }

        private void Spawn(Vector3 p,bool hostile)
        {
            p.y=.6f+(physicalTerrain!=null?physicalTerrain.Height(p.x,p.z):0);
            var model=BlackAlertDetailedVehicleArt.Tank(p,hostile);
            agents.Add(new Agent {model=model,enemy=hostile,type=AiUnitType.Tank,
                goal=hostile?new Vector3(-22,0,p.z):p,speed=hostile?3.1f:3.8f,altitude=.6f});
        }
        private void SpawnInfantry(Vector3 p,bool hostile)
        {
            p.y=(physicalTerrain!=null?physicalTerrain.Height(p.x,p.z):0);
            var model=BlackAlertDetailedInfantryArt.Create(p,hostile);
            agents.Add(new Agent {model=model,enemy=hostile,
                goal=hostile?new Vector3(-21,0,p.z):p,speed=hostile?3.7f:4.2f,altitude=.1f,infantry=true,type=AiUnitType.Rifleman});
        }
        private void SpawnHelicopter(Vector3 p,bool hostile)
        {
            var model=BlackAlertDetailedVehicleArt.Helicopter(p,hostile);
            agents.Add(new Agent {model=model,enemy=hostile,
                goal=hostile?new Vector3(-20,0,p.z):p,speed=hostile?5f:5.5f,altitude=7f,type=AiUnitType.Helicopter});
        }
        private void SpawnJet(Vector3 p,bool hostile)
        {
            var model=BlackAlertDetailedVehicleArt.Jet(p,hostile);
            agents.Add(new Agent {model=model,enemy=hostile,
                goal=hostile?new Vector3(-22,0,p.z):p,speed=hostile?10f:11f,altitude=13f,type=AiUnitType.Jet});
        }
        private void SpawnShip(Vector3 p,bool hostile)
        {
            p.x=physicalTerrain!=null?physicalTerrain.RiverCenter(p.z):0;
            var model=BlackAlertDetailedVehicleArt.Destroyer(p,hostile);
            agents.Add(new Agent {model=model,enemy=hostile,
                goal=new Vector3(p.x,0,hostile?-18:18),speed=hostile?2.7f:2.3f,
                altitude=.7f,naval=true,type=AiUnitType.Destroyer});
        }

        private void Update()
        {
            if(modeController!=null && modeController.MatchFinished)
            {
                // Preserve the result screen without generating additional units.
                UpdateInput();
                return;
            }
            battleTime += Time.deltaTime;
            if(economy!=null)
            {
                economy.Tick(Time.deltaTime);
                foreach(string unit in economy.CollectCompleted())DeliverProduction(unit);
            }
            if(crimsonEconomy!=null)
            {
                crimsonEconomy.Tick(Time.deltaTime);
                foreach(string unit in crimsonEconomy.CollectCompleted())
                    DeliverProduction(unit,true);
            }
            ThinkEnemyCommander();
            UpdateInput();
            if(modeController==null || !modeController.MatchFinished)
                Simulate();
        }

        private void UpdateInput()
        {
            if(cam==null)return;
            if(Input.touchCount>=2)
            {
                dragSelect=false;
                var first=Input.GetTouch(0);
                var second=Input.GetTouch(1);
                var previous=(first.position-first.deltaPosition) -
                             (second.position-second.deltaPosition);
                float earlier=previous.magnitude;
                float now=(first.position-second.position).magnitude;
                camHeight=Mathf.Clamp(camHeight+(earlier-now)*.045f,20f,100f);
                camFocus-=new Vector3((first.deltaPosition.x+second.deltaPosition.x)*.023f,
                    0,(first.deltaPosition.y+second.deltaPosition.y)*.023f);
                AlignCamera();
                return;
            }
            if(Input.touchCount==1)
            {
                var touch=Input.GetTouch(0);
                switch(touch.phase)
                {
                    case TouchPhase.Began:
                        dragOrigin=touch.position;
                        dragEndpoint=touch.position;
                        fingerStartTime=Time.time;
                        dragCamera=false;
                        dragSelect=false;
                        startedOnHud=(modeController!=null &&
                            modeController.BlocksWorldTouch(touch.position)) ||
                            (tacticalIntel!=null && tacticalIntel.BlocksTap(touch.position));
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if(startedOnHud)break;
                        dragEndpoint=touch.position;
                        float distance=(dragEndpoint-dragOrigin).magnitude;
                        if(!dragSelect && Time.time-fingerStartTime>.40f && distance>12f)
                        {
                            dragSelect=true;
                            dragCamera=false;
                        }
                        if(!dragSelect && distance>7f)
                        {
                            dragCamera=true;
                            camFocus-=new Vector3(touch.deltaPosition.x*.025f,0,
                                touch.deltaPosition.y*.025f);
                            AlignCamera();
                        }
                        break;
                    case TouchPhase.Ended:
                        if(startedOnHud)
                        {
                            if(tacticalIntel!=null)tacticalIntel.ProcessTap(touch.position);
                            break;
                        }
                        if(tacticalIntel!=null && tacticalIntel.ProcessTap(touch.position))
                            break;
                        if(dragSelect)
                            SelectRectangle(dragOrigin,touch.position);
                        else if(!dragCamera &&
                                 (modeController==null || !modeController.BlocksWorldTouch(touch.position)) &&
                                 (tacticalIntel==null || !tacticalIntel.BlocksTap(touch.position)))
                            SelectOrOrder(touch.position);
                        dragSelect=false;
                        break;
                }
                return;
            }
#if UNITY_EDITOR
            if(Input.GetMouseButtonDown(0))
            {
                dragOrigin=Input.mousePosition;
                dragEndpoint=dragOrigin;
                dragSelect=false;
                startedOnHud=(modeController!=null &&
                    modeController.BlocksWorldTouch(Input.mousePosition)) ||
                    (tacticalIntel!=null && tacticalIntel.BlocksTap(Input.mousePosition));
            }
            if(Input.GetMouseButton(0) && !startedOnHud)
            {
                dragEndpoint=Input.mousePosition;
                if((dragEndpoint-dragOrigin).sqrMagnitude>196f)dragSelect=true;
            }
            if(Input.GetMouseButtonUp(0))
            {
                if(tacticalIntel!=null && tacticalIntel.ProcessTap(Input.mousePosition)){}
                else if(!startedOnHud && dragSelect)
                    SelectRectangle(dragOrigin,Input.mousePosition);
                else if(!startedOnHud)
                    SelectOrOrder(Input.mousePosition);
                dragSelect=false;
            }
            camHeight=Mathf.Clamp(camHeight-Input.mouseScrollDelta.y*3f,20f,100f);
            AlignCamera();
#endif
        }

        private void SelectRectangle(Vector2 start,Vector2 finish)
        {
            float left=Mathf.Min(start.x,finish.x);
            float right=Mathf.Max(start.x,finish.x);
            float bottom=Mathf.Min(start.y,finish.y);
            float top=Mathf.Max(start.y,finish.y);
            var group=new List<Agent>();
            foreach(var a in agents)
            {
                if(a.enemy || a.model==null || a.hp<=0)continue;
                Vector3 screen=cam.WorldToScreenPoint(a.model.transform.position);
                if(screen.z>0 && screen.x>=left && screen.x<=right &&
                   screen.y>=bottom && screen.y<=top)
                    group.Add(a);
            }
            ApplySelection(group);
        }

        private void SetSelectedRing(Agent a,bool visible)
        {
            if(a==null || a.model==null)return;
            Transform ring=a.model.transform.Find("Selection Ring");
            if(ring==null)
            {
                var marker=new GameObject("Selection Ring");
                marker.transform.SetParent(a.model.transform,false);
                marker.transform.localPosition=new Vector3(0,a.altitude>2f?-a.altitude:
                    a.infantry ? .05f : -.49f,0);
                var line=marker.AddComponent<LineRenderer>();
                line.useWorldSpace=false;
                line.loop=true;
                line.positionCount=48;
                line.startWidth=.17f;
                line.endWidth=.17f;
                var shader=Shader.Find("Sprites/Default");
                if(shader==null)shader=Shader.Find("Standard");
                var mat=new Material(shader);mat.color=new Color(.16f,.9f,1f);
                line.sharedMaterial=mat;
                float radius=a.infantry ? .82f : 2.3f;
                for(int i=0;i<48;i++)
                {
                    float theta=i*Mathf.PI*2/48f;
                    line.SetPosition(i,new Vector3(Mathf.Cos(theta)*radius,0,
                        Mathf.Sin(theta)*radius));
                }
                ring=marker.transform;
            }
            ring.gameObject.SetActive(visible);
        }

        private void ApplySelection(List<Agent> group)
        {
            foreach(var old in selectedGroup)SetSelectedRing(old,false);
            selectedGroup.Clear();
            foreach(var a in group)
            {
                if(a==null || a.model==null || a.enemy)continue;
                selectedGroup.Add(a);
                SetSelectedRing(a,true);
            }
            selected=selectedGroup.Count>0?selectedGroup[0]:null;
        }

        private void IssueGroupOrder(Vector3 position)
        {
            int count=0;
            foreach(var a in selectedGroup)
            {
                if(a==null || a.model==null || a.hp<=0)continue;
                // Mobile-friendly formation offsets prevent 8 tanks from stacking.
                float dx=(count%4-1.5f)*2.45f;
                float dz=(count/4)*2.45f;
                Vector3 target=new Vector3(Mathf.Clamp(position.x+dx,-46f,46f),0,
                    Mathf.Clamp(position.z+dz,-42f,42f));
                PlanRoute(a,target);
                count++;
            }
        }

        private void PlanRoute(Agent unit,Vector3 destination)
        {
            unit.route.Clear();
            if(physicalTerrain==null || unit.altitude>2f || unit.naval)
            {
                unit.goal=destination;
                return;
            }

            // The navigation grid covers the actual 100x90m battlefield at 2m intervals.
            // A* avoids steep impassable mountain sections and chooses cheaper roads/bridges.
            const int width=50,height=45;
            int sx=Mathf.Clamp(Mathf.RoundToInt((unit.model.transform.position.x+49f)/2f),0,width-1);
            int sz=Mathf.Clamp(Mathf.RoundToInt((unit.model.transform.position.z+44f)/2f),0,height-1);
            int gx=Mathf.Clamp(Mathf.RoundToInt((destination.x+49f)/2f),0,width-1);
            int gz=Mathf.Clamp(Mathf.RoundToInt((destination.z+44f)/2f),0,height-1);
            var navigator=new GridPathfinder(width,height,
                (x,z)=>physicalTerrain.CanCross(new Vector3(-49f+x*2f,0,-44f+z*2f),
                    unit.infantry,!unit.infantry),
                (x,z)=>Mathf.Max(0f,1f/physicalTerrain.SpeedFactor(
                    new Vector3(-49f+x*2f,0,-44f+z*2f),
                    unit.infantry,!unit.infantry)-1f));
            var path=navigator.Find(new GridCell(sx,sz),new GridCell(gx,gz));
            if(path.Count<1)
            {
                Debug.LogWarning("[BLACK ALERT NAV] Route blocked: "+unit.model.name);
                unit.goal=unit.model.transform.position;
                return;
            }
            for(int i=1;i<path.Count;i++)
                unit.route.Enqueue(new Vector3(-49f+path[i].X*2f,0,-44f+path[i].Y*2f));
            if(unit.route.Count>0)unit.goal=unit.route.Dequeue();
            else unit.goal=destination;
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
                if(economy!=null && Time.time>=nuclearReadyAt && economy.TrySpend(3800))
                {
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
            if(clicked!=null && clicked.enemy && tacticalIntel!=null &&
               !tacticalIntel.CanSelectEnemy(clicked.model.transform.position))
                clicked=null;
            if(clicked!=null && !clicked.enemy)
            {
                var matching=new List<Agent>();
                if(Time.time-lastTapTime<.34f && lastTappedType==clicked.model.name)
                {
                    foreach(var a in agents)if(!a.enemy && a.model!=null &&
                        a.model.name==clicked.model.name && a.hp>0)
                        matching.Add(a);
                }
                else matching.Add(clicked);
                ApplySelection(matching);
                lastTappedType=clicked.model.name;
                lastTapTime=Time.time;
                return;
            }
            if(selectedGroup.Count>0)
            {
                Vector3 target=new Vector3(Mathf.Clamp(hit.point.x,-47f,47f),0,
                    Mathf.Clamp(hit.point.z,-43f,43f));
                if(clicked!=null && clicked.enemy && clicked.model!=null)
                    target=clicked.model.transform.position;
                IssueGroupOrder(target);
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
                    if(tacticalIntel!=null &&
                        !tacticalIntel.CanSpot(a.enemy,b.model.transform.position))continue;
                    var d=(b.model.transform.position-a.model.transform.position).sqrMagnitude;
                    if(d<best) {best=d;enemy=b;}
                }
                if(a.enemy && tacticalBrain==null && enemy!=null &&
                   best<2500f && Time.time>=a.nextPathAt)
                {
                    if(a.altitude>2f || a.naval)
                        a.goal=enemy.model.transform.position;
                    else PlanRoute(a,enemy.model.transform.position);
                    a.nextPathAt=Time.time+3.5f;
                }
                if(a.naval && physicalTerrain!=null)
                    a.goal=new Vector3(physicalTerrain.RiverCenter(a.goal.z),.7f,
                        Mathf.Clamp(a.goal.z,-40f,40f));
                var position=a.model.transform.position;
                var flatGoal=new Vector3(a.goal.x,
                    a.naval ? .7f : a.altitude>2f ? a.altitude :
                    a.altitude+(physicalTerrain!=null?physicalTerrain.Height(a.goal.x,a.goal.z):0),
                    a.goal.z);
                var diff=flatGoal-position;
                float waypointRange=a.infantry ? .75f : 1.95f;
                Vector3 planeDiff=diff;planeDiff.y=0;
                if(planeDiff.magnitude<waypointRange && a.route.Count>0)
                {
                    a.goal=a.route.Dequeue();
                    flatGoal=new Vector3(a.goal.x,
                        a.altitude>2f ? a.altitude :
                        a.altitude+physicalTerrain.Height(a.goal.x,a.goal.z),a.goal.z);
                    diff=flatGoal-position;
                }
                if(diff.sqrMagnitude>(a.infantry ? .55f : 4.5f) &&
                    (enemy==null || best>70f || !a.enemy || a.aiRetreat))
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
                if(enemy!=null && enemy.model!=null)
                {
                    Vector3 toward=enemy.model.transform.position-a.model.transform.position;
                    toward.y=0;
                    if(toward.sqrMagnitude>.01f)
                    {
                        var detailed=a.model.GetComponent<BlackAlertVehicleAnimator>();
                        var pivot=detailed!=null?detailed.turret:null;
                        var legacy=a.model.GetComponent<BlackAlertVisualMotion>();
                        if(pivot==null && legacy!=null)pivot=legacy.turret;
                        if(pivot!=null)
                            pivot.rotation=Quaternion.Slerp(pivot.rotation,
                                Quaternion.LookRotation(toward),Time.deltaTime*5f);
                    }
                }
                a.cooldown-=Time.deltaTime;
                if(Time.time<a.stunnedUntil)continue;
                if(enemy!=null && best < 120f && a.cooldown<=0)
                {
                    a.cooldown=a.infantry ? .8f : (a.naval ? 2.6f : 1.3f);
                    shotsFired++;
                    var visual=a.model.GetComponent<BlackAlertVehicleAnimator>();
                    var infantryVisual=a.model.GetComponent<BlackAlertInfantryAnimator>();
                    Vector3 muzzle=visual!=null?visual.MuzzlePosition:
                        infantryVisual!=null && infantryVisual.muzzle!=null?
                        infantryVisual.muzzle.position:position+Vector3.up;
                    Vector3 impact=enemy.model.transform.position+Vector3.up*(enemy.infantry?1.1f:.55f);
                    if(visual!=null)visual.Fire();
                    BlackAlertSoundFx.Shot(muzzle);
                    BlackAlertCombatEffects.MuzzleFlash(muzzle);
                    ApplyDamage(enemy,a.infantry?6f:a.naval?34f:18f);
                    SpawnTracer(muzzle,impact);
                }
                else if((enemy==null || best>120f) && a.cooldown<=0 &&
                        headquarters!=null && !headquarters.Finished)
                {
                    int targetFaction=a.enemy?0:1;
                    GameObject targetHQ=a.enemy?azureHeadquarters:crimsonHeadquarters;
                    if(targetHQ!=null && targetHQ.activeSelf &&
                       (new Vector2(position.x-targetHQ.transform.position.x,
                           position.z-targetHQ.transform.position.z)).sqrMagnitude<225f)
                    {
                        a.cooldown=a.infantry ? .82f : (a.naval ? 2.6f : 1.3f);
                        var visual=a.model.GetComponent<BlackAlertVehicleAnimator>();
                        var infantryVisual=a.model.GetComponent<BlackAlertInfantryAnimator>();
                        Vector3 muzzle=visual!=null?visual.MuzzlePosition:
                            infantryVisual!=null && infantryVisual.muzzle!=null?
                            infantryVisual.muzzle.position:position+Vector3.up;
                        if(visual!=null)visual.Fire();
                        var point=targetHQ.transform.position;
                        BlackAlertCombatEffects.MuzzleFlash(muzzle);
                        BlackAlertSoundFx.Shot(muzzle);
                        SpawnTracer(muzzle,point);
                        DamageHeadquarters(targetFaction,
                            a.infantry?9:a.naval?62:a.type==AiUnitType.Jet?45:28,point);
                    }
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
            BlackAlertCombatEffects.Hit(end);
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
            BlackAlertCombatEffects.NuclearDetonation(target);
            if(azureHeadquarters!=null && Vector3.Distance(target,
                azureHeadquarters.transform.position)<20f)
                DamageHeadquarters(0,250,azureHeadquarters.transform.position);
            if(crimsonHeadquarters!=null && Vector3.Distance(target,
                crimsonHeadquarters.transform.position)<20f)
                DamageHeadquarters(1,250,crimsonHeadquarters.transform.position);
            foreach(var unit in agents)
            {
                if(unit.model==null || unit.hp<=0)continue;
                float distance=Vector3.Distance(target,unit.model.transform.position);
                if(distance<20f)
                {
                    var direction=unit.model.transform.position-target;
                    direction.y=0;
                    ApplyDamage(unit,320f*(1f-distance/22f));
                    if(unit.model!=null && direction.sqrMagnitude>.01f)
                        unit.model.transform.position+=direction.normalized*
                            (20f-distance)*.22f;
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
            GUI.Label(new Rect(20, 132, 780, 30), "Credits "+economy.Credits+
                " | Power "+economy.PowerGenerated+"/"+economy.PowerConsumed+
                " | Ore "+economy.Ore["Azure Ore Field"].Remaining+
                " | Shots "+shotsFired+" | Time "+battleTime.ToString("0")+"s");
            GUI.Label(new Rect(20, 160, 700, 30), selected!=null&&selected.model!=null?
                "Selected ("+selectedGroup.Count+"): "+selected.model.name:
                "Tap an Azure unit / double-tap same type / long-press and drag squad");
            if(GUI.Button(new Rect(20, 195, 195, 55), "QUEUE TANK $700"))
                economy.QueueUnit("tank");
            if(GUI.Button(new Rect(20, 257, 195, 55), "RIFLEMAN $100"))
                economy.QueueUnit("rifleman");
            if(GUI.Button(new Rect(20, 319, 195, 55), "HELICOPTER $1200"))
                economy.QueueUnit("helicopter");
            if(GUI.Button(new Rect(20, 381, 195, 55), "JET $1600"))
            {
                if(!economy.QueueUnit("jet"))Debug.Log("[BlackAlert] Jet requires tech building.");
            }
            if(GUI.Button(new Rect(20, 443, 195, 55), "DESTROYER $2200"))
                economy.QueueUnit("destroyer");
            var nukeLabel=Time.time<nuclearReadyAt ?
                "NUKE RECHARGING "+Mathf.CeilToInt(nuclearReadyAt-Time.time)+"s":
                targetingNuclear ? "TAP TARGET ON MAP" : "NUCLEAR $3800";
            if(GUI.Button(new Rect(20,505,195,55),nukeLabel) &&
                Time.time>=nuclearReadyAt && economy.Credits>=3800)
                targetingNuclear=!targetingNuclear;
            if(GUI.Button(new Rect(20,565,195,40),"TECH CENTER $1600"))
                economy.BuildStructure("tech");
            if(GUI.Button(new Rect(20,608,195,40),"NEW REACTOR $900"))
                economy.BuildStructure("reactor");
            GUI.Label(new Rect(20,653,930,28),
                "HQ "+HeadquartersHP(false)+"/"+StrongholdBattle.DefaultHealth+
                " | Enemy HQ "+HeadquartersHP(true)+"/"+StrongholdBattle.DefaultHealth+
                " | Production "+economy.PendingUnits+" | "+
                (economy.FirstQueuedUnit??"Idle")+" "+
                economy.FirstQueueRemaining.ToString("0.0")+"s"+
                (economy.LowPower?" | LOW POWER":""));
            GUI.Label(new Rect(20,686,880,28),
                "ENEMY AI "+lastEnemyStrategy+" | Crimson credits "+
                (crimsonEconomy!=null?crimsonEconomy.Credits:0)+
                " | Offline prototype, final art and online pending");
            if(dragSelect)
            {
                float x=Mathf.Min(dragOrigin.x,dragEndpoint.x);
                float y=Screen.height-Mathf.Max(dragOrigin.y,dragEndpoint.y);
                float width=Mathf.Abs(dragEndpoint.x-dragOrigin.x);
                float height=Mathf.Abs(dragEndpoint.y-dragOrigin.y);
                GUI.Box(new Rect(x,y,width,height),"SELECT SQUAD");
            }
        }
    }
}
