using System;
using System.Collections.Generic;
using BlackAlert.Simulation;
using UnityEngine;

namespace BlackAlert
{
    /// <summary>
    /// Connects headless match rules to actual battlefield units and Unity HUD interactions.
    /// Offline systems prototype: multiplayer, final visual art and production game modes still require work.
    /// </summary>
    public sealed class BlackAlertPlayableModes : MonoBehaviour
    {
        private sealed class Sector
        {
            public Vector3 position;
            public GameObject marker;
            public int owner = -1;
            public int id;
        }

        private BattlefieldPrototype battle;
        private MatchState match;
        private FaceOffDeck duel;
        private WorldWarCampaign campaign;
        private readonly List<Sector> sectors = new List<Sector>();
        private LineRenderer survivalRing;
        private float nextPoll;
        private int lastStormTick = -1;
        private string notification = "";
        private bool panelOpen;
        private bool worldPanel;
        private bool endRecorded;
        private BlackAlertCommanderProfile profile;
        private BattleMode mode = BattleMode.Skirmish;
        private int selectedCard = -1;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;

        public BattleMode Mode { get { return mode; } }
        public bool MatchFinished { get { return match != null && match.Outcome != BattleOutcome.InProgress; } }

        public void Initialize(BattlefieldPrototype host)
        {
            battle = host;
            profile = BlackAlertCommanderProfile.Load();
            BeginMode(BattleMode.Skirmish, false);
        }

        public bool BlocksWorldTouch(Vector2 screenPoint)
        {
            float guiY = Screen.height - screenPoint.y;
            return (screenPoint.x <= 230f && guiY <= 585f) ||
                   (screenPoint.x >= Screen.width - 318f && guiY <= (panelOpen ? 690f : 228f));
        }

        private void BeginMode(BattleMode next, bool resetArmies)
        {
            mode = next;
            panelOpen = false;
            selectedCard = -1;
            endRecorded = false;
            notification = next == BattleMode.WorldWarIII ?
                "WWIII strategic tutorial (6 territories)" : next.ToString()+" offline simulation";
            worldPanel = next == BattleMode.WorldWarIII;
            ClearObjectives();

            match = new MatchState(new MatchSettings(next, 2, next == BattleMode.Battlefield ? 125 : 500,
                next == BattleMode.Survival ? 40 : 90));
            match.AddCommander(0, 0);
            match.AddCommander(1, 1);
            match.Begin();
            duel = next == BattleMode.FaceOffDuel
                ? new FaceOffDeck(Math.Max(1, profile.Level), 40523, DuelRole.RollingCards) : null;
            campaign = next == BattleMode.WorldWarIII ? CreateCampaignDemo() : null;

            if(resetArmies && battle != null) battle.RestartBattle();
            if(next == BattleMode.Battlefield) CreateSectors();
            if(next == BattleMode.Survival) CreateSurvivalRing();
            if(battle != null) battle.SetDuelCamera(next == BattleMode.FaceOffDuel);
        }

        private static WorldWarCampaign CreateCampaignDemo()
        {
            // Six actual country names are an OFFLINE demonstration, not the requested worldwide dataset.
            var territories = new []
            {
                new CountryTerritory("PT","Portugal",0,7,9,0,new[]{"ES"}),
                new CountryTerritory("ES","Spain",0,8,12,-1,new[]{"PT","FR"}),
                new CountryTerritory("FR","France",0,9,14,-1,new[]{"ES","BE","DE","IT"}),
                new CountryTerritory("BE","Belgium",1,4,8,-1,new[]{"FR","DE"}),
                new CountryTerritory("DE","Germany",1,6,14,1,new[]{"FR","BE"}),
                new CountryTerritory("IT","Italy",1,7,12,-1,new[]{"FR"})
            };
            return new WorldWarCampaign(territories);
        }

        private void ClearObjectives()
        {
            foreach(var s in sectors) if(s.marker != null) Destroy(s.marker);
            sectors.Clear();
            if(survivalRing != null) Destroy(survivalRing.gameObject);
            survivalRing = null;
        }

        private void CreateSectors()
        {
            var locations = new [] {new Vector3(-9,0,-23), new Vector3(0,0,0), new Vector3(9,0,23)};
            for(int i=0; i<locations.Length;i++)
            {
                var p=locations[i];
                p.y=battle.TerrainHeight(p.x,p.z)+.36f;
                var disk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disk.name="Battlefield Sector "+(i+1);
                disk.transform.position=p;
                disk.transform.localScale=new Vector3(5.5f,.14f,5.5f);
                var collision=disk.GetComponent<Collider>();
                if(collision!=null)Destroy(collision);
                var shader=Shader.Find("Standard");
                if(shader==null)shader=Shader.Find("Sprites/Default");
                var material=new Material(shader);
                material.color=new Color(.8f,.7f,.27f);
                disk.GetComponent<Renderer>().sharedMaterial=material;
                sectors.Add(new Sector{position=p,marker=disk,id=i});
            }
        }

        private void CreateSurvivalRing()
        {
            var go=new GameObject("Survival danger zone ring");
            survivalRing=go.AddComponent<LineRenderer>();
            survivalRing.useWorldSpace=true;
            survivalRing.loop=true;
            survivalRing.positionCount=96;
            survivalRing.startWidth=.75f;
            survivalRing.endWidth=.75f;
            var shader=Shader.Find("Sprites/Default");
            if(shader==null)shader=Shader.Find("Standard");
            survivalRing.material=new Material(shader);
            survivalRing.material.color=new Color(1f,.36f,.09f);
        }

        private void Update()
        {
            if(battle == null || match == null || match.Outcome != BattleOutcome.InProgress)return;
            float dt=Time.deltaTime;
            match.Tick(dt);
            if(duel != null)duel.Tick(dt);

            if(mode==BattleMode.Survival && survivalRing != null)
            {
                float radius=Mathf.Max(11f,46f - 6f*match.RingStage);
                for(int i=0;i<survivalRing.positionCount;i++)
                {
                    float angle=2f*Mathf.PI*i/survivalRing.positionCount;
                    float x=Mathf.Cos(angle)*radius,z=Mathf.Sin(angle)*radius;
                    survivalRing.SetPosition(i,new Vector3(x,battle.TerrainHeight(x,z)+1.2f,z));
                }
                int tick=Mathf.FloorToInt(Time.time);
                if(tick!=lastStormTick)
                {
                    lastStormTick=tick;
                    if(match.RingStage>0)
                        battle.DamageOutsideCircle(radius,3f + 2f*match.RingStage);
                }
            }
            if(mode==BattleMode.Battlefield && Time.time>=nextPoll)
            {
                nextPoll=Time.time+1f;
                foreach(var sector in sectors)
                {
                    int a=battle.CountUnitsNear(sector.position,false,8f);
                    int b=battle.CountUnitsNear(sector.position,true,8f);
                    if(a==b || (a==0 && b==0))continue;
                    int owner=a>b?0:1;
                    if(owner==sector.owner)continue;
                    sector.owner=owner;
                    match.CaptureSector(sector.id,owner);
                    sector.marker.GetComponent<Renderer>().material.color=owner==0?
                        new Color(.1f,.55f,.94f):new Color(.88f,.16f,.13f);
                    notification="Sector "+(sector.id+1)+" captured by "+(owner==0?"Azure":"Crimson");
                }
            }
            // Prevent match ending at first casualty or while a replacement can be produced.
            if(mode!=BattleMode.WorldWarIII && battle.BattleDuration>=90f)
            {
                if(battle.CountLivingUnits(false)==0 && !endRecorded)
                    match.Eliminate(0);
                else if(battle.CountLivingUnits(true)==0 && !endRecorded)
                    match.Eliminate(1);
            }
            if(match.Outcome!=BattleOutcome.InProgress && !endRecorded)
            {
                endRecorded=true;
                if(match.Outcome==BattleOutcome.Victory && match.WinningTeam==0)
                    profile.RecordVictory();
                else
                    profile.RecordDefeat();
                notification=match.Outcome==BattleOutcome.Draw?"DRAW":
                    match.WinningTeam==0?"AZURE VICTORY":"CRIMSON VICTORY";
            }
        }

        private void ApplyDuelCard()
        {
            if(duel==null || selectedCard<0 || selectedCard>=duel.Hand.Count)return;
            string id=duel.Hand[selectedCard].Id;
            if(!duel.CanPlay(id))
            {
                notification="Card not ready: check energy / cooldown";
                return;
            }
            // Centered on enemy forces. Card effects are actual battlefield operations,
            // not yet final animated rolling cards or a complete asymmetric Duel mode.
            var card=duel.Play(id,1024,1024);
            battle.ApplyCardEffect(card);
            selectedCard=-1;
            notification="Activated "+card.CardId;
        }

        private void OnGUI()
        {
            if(battle==null)return;
            if(labelStyle==null)
            {
                labelStyle=new GUIStyle(GUI.skin.label);
                labelStyle.fontSize=17;
                labelStyle.normal.textColor=Color.white;
                headerStyle=new GUIStyle(labelStyle);
                headerStyle.fontStyle=FontStyle.Bold;
                headerStyle.fontSize=20;
            }
            float x=Mathf.Max(300,Screen.width-318);
            GUI.Box(new Rect(x,12,310,212),"BLACK ALERT / COMMAND");
            GUI.Label(new Rect(x+12,37,285,26), "MODE: "+mode,headerStyle);
            GUI.Label(new Rect(x+12,66,284,30), "COMMANDER: "+profile.DisplayName+
                "  LV "+profile.Level,labelStyle);
            if(match!=null && mode==BattleMode.Battlefield)
                GUI.Label(new Rect(x+12,94,284,30),
                    "TICKETS Azure "+match.GetTickets(0)+" / Crimson "+match.GetTickets(1),labelStyle);
            else if(mode==BattleMode.Survival && match!=null)
                GUI.Label(new Rect(x+12,94,284,30),
                    "RING STAGE "+match.RingStage,labelStyle);
            else
                GUI.Label(new Rect(x+12,94,284,30),
                    "OFFLINE MODE / TEST BUILD",labelStyle);
            GUI.Label(new Rect(x+12,124,290,45),notification,labelStyle);
            if(GUI.Button(new Rect(x+12,170,285,44),panelOpen?"CLOSE MODES":"CHANGE GAME MODE"))
                panelOpen=!panelOpen;

            if(!panelOpen)return;
            GUI.Box(new Rect(x,230,310,mode==BattleMode.WorldWarIII?540:492),"MODES / LIVE RULES TEST");
            string[] titles={"SKIRMISH","BATTLEFIELD","SURVIVAL","FACE-OFF 1V1","WORLD WAR III (DEMO)"};
            BattleMode[] gameModes={BattleMode.Skirmish,BattleMode.Battlefield,BattleMode.Survival,
                BattleMode.FaceOffDuel,BattleMode.WorldWarIII};
            for(int i=0;i<titles.Length;i++)
                if(GUI.Button(new Rect(x+12,260+i*48,285,43),titles[i]))
                {
                    BeginMode(gameModes[i],true);
                    return;
                }
            if(mode==BattleMode.FaceOffDuel && duel!=null)
            {
                if(GUI.Button(new Rect(x+12,510,140,43),"ROLL CARD") && duel.Hand.Count<5)
                {
                    var card=duel.Roll();selectedCard=duel.Hand.Count-1;
                    notification="Rolled "+card.Label;
                }
                if(GUI.Button(new Rect(x+158,510,138,43),"ACTIVATE CARD"))
                    ApplyDuelCard();
                GUI.Label(new Rect(x+12,562,290,27),"Energy "+duel.Energy+"/10",labelStyle);
                for(int i=0;i<duel.Hand.Count && i<3;i++)
                {
                    int idx=i;
                    if(GUI.Button(new Rect(x+12,596+idx*37,285,35),
                        (idx==selectedCard?"> ":"") + duel.Hand[idx].Label))selectedCard=idx;
                }
            }
            if(mode==BattleMode.WorldWarIII && campaign!=null)
            {
                GUI.Label(new Rect(x+12,511,286,28),"6-country STRATEGY DATA DEMO",labelStyle);
                GUI.Label(new Rect(x+12,541,280,28),
                    "Turn "+campaign.TurnNumber+"   Treasury "+campaign.Treasury[0],labelStyle);
                if(GUI.Button(new Rect(x+12,574,285,40),"NEXT STRATEGIC TURN"))
                {campaign.AdvanceTurn(); notification="Territory incomes credited";}
                if(GUI.Button(new Rect(x+12,617,285,40),"REINFORCE SPAIN"))
                {
                    try{campaign.Reinforce(0,"ES",1); notification="Spain reinforced";}
                    catch(InvalidOperationException){ notification="Cannot reinforce: check supply / funds"; }
                }
                if(GUI.Button(new Rect(x+12,660,285,40),"ATTACK FRANCE -> BELGIUM"))
                {
                    var attack=campaign.Invade("FR","BE",2,6,2);
                    notification=attack.TerritoryCaptured?"Belgium captured":"Attack not successful";
                }
            }
            if(match!=null && match.Outcome!=BattleOutcome.InProgress)
                GUI.Label(new Rect(x+12,708,300,40),"MATCH ENDED: "+match.Outcome,headerStyle);
        }
    }
}
