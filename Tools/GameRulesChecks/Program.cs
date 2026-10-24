using System;
using BlackAlert.Simulation;

static class Program
{
    private static int successes;
    static void Assert(bool predicate,string message)
    {
        if(!predicate) throw new Exception("FAILED: "+message);
        Console.WriteLine("PASS "+message);
        successes++;
    }
    static void ExpectError(Action operation, string description)
    {
        try {operation();}
        catch(Exception) {Assert(true,description);return;}
        Assert(false,description);
    }

    static void Main()
    {
        // Same simulation C# files Unity will import. No Unity license or build minutes.
        var eight=new MatchState(new MatchSettings(BattleMode.Skirmish,8));
        for(int i=0;i<8;i++)eight.AddCommander(i,i);
        eight.Begin();
        Assert(eight.Commanders.Count==8,"8-player room registers seats");
        ExpectError(()=>eight.AddCommander(9,9),"cannot add seats after game begins");

        var one=new MatchState(new MatchSettings(BattleMode.Skirmish,2));
        one.AddCommander(1,1);one.AddCommander(2,2);one.Begin();
        one.RegisterUnit(1); one.RegisterUnit(2);
        one.UnitDestroyed(2,1);
        Assert(one.Outcome==BattleOutcome.InProgress,"unit loss never prematurely ends match");
        one.Eliminate(2);
        Assert(one.Outcome==BattleOutcome.Victory && one.WinningTeam==1,"elimination wins skirmish");

        var survival=new MatchState(new MatchSettings(BattleMode.Survival,4,500,30));
        survival.AddCommander(1,1);survival.AddCommander(2,2);
        survival.AddCommander(3,3);survival.AddCommander(4,4);
        survival.Begin();survival.Tick(61);
        Assert(survival.RingStage==2,"survival ring advances in deterministic stages");
        survival.Eliminate(1);survival.Eliminate(2);
        Assert(survival.Outcome==BattleOutcome.InProgress,"survival continues until one last team");
        survival.Eliminate(4);
        Assert(survival.WinningTeam==3,"survival last commander wins");

        var battlefield=new MatchState(new MatchSettings(BattleMode.Battlefield,2,8));
        battlefield.AddCommander(1,1);battlefield.AddCommander(2,2);
        battlefield.Begin();
        battlefield.CaptureSector(0,1);
        battlefield.Tick(3.5);
        Assert(battlefield.GetTickets(2)==5,"battlefield sector control drains enemy tickets");
        battlefield.Tick(5);
        Assert(battlefield.Outcome==BattleOutcome.Victory && battlefield.WinningTeam==1,
            "battlefield ticket exhaustion declares winner");

        ExpectError(()=>new MatchSettings(BattleMode.Skirmish,9),"reject more than eight players");
        ExpectError(()=>new MatchSettings(BattleMode.FaceOffDuel,4),"duel requires exactly two players");

        var cards=new FaceOffDeck(1,1234);
        var cards2=new FaceOffDeck(1,1234);
        Assert(cards.Roll().Id==cards2.Roll().Id,"duel card reel repeatable for same seed");
        Assert(cards.CareerLevel==1 && FaceOffDeck.StandardCards().Length==12,
            "career levels and twelve starter cards defined");
        Assert(!cards.CanPlay("emp"),"low-level player cannot use high-level EMP");
        var chosen=cards.Hand[0];
        cards.Play(chosen.Id,10,10);
        Assert(cards.Energy==5-chosen.EnergyCost,"played card consumes energy");
        Assert(cards.CooldownRemaining(chosen.Id)>0,"card begins cooldown immediately");
        cards.Tick(5);
        Assert(cards.Energy==6-chosen.EnergyCost,"card energy regenerates after five seconds");
        var hi=new FaceOffDeck(7,900);
        Assert(hi.CareerLevel==7 && FaceOffDeck.StandardCards().Length==12,
            "advanced duel career deck includes higher tiers");
        ExpectError(()=>new FaceOffDeck(1,1,DuelRole.DirectCommander).Roll(),
            "direct-control duel role cannot roll tactical cards");

        var map=new []
        {
            new CountryTerritory("AA","Home Alpha",1,8,25,1,new[]{"BB"}),
            new CountryTerritory("BB","Border",1,7,10,-1,new[]{"AA","CC"}),
            new CountryTerritory("CC","Rival Border",2,1,10,-1,new[]{"BB","DD"}),
            new CountryTerritory("DD","Rival Capital",2,5,25,2,new[]{"CC"})
        };
        var campaign=new WorldWarCampaign(map);
        Assert(campaign.HasSupply("BB") && campaign.HasSupply("CC"),
            "world war supply propagates across owned territory");
        campaign.AdvanceTurn();
        Assert(campaign.Treasury[1]==35 && campaign.Treasury[2]==35,
            "world economy pays supplied territory resources");
        campaign.Reinforce(1,"BB",2);
        Assert(campaign.Countries["BB"].Armies==9 && campaign.Treasury[1]==15,
            "campaign treasury buys reinforcement");
        campaign.Alliance(1,2,true);
        Assert(!campaign.Invade("BB","CC",3,6,1).WasLegal,
            "diplomatic alliance blocks allied invasion");
        campaign.Alliance(1,2,false);
        var invasion=campaign.Invade("BB","CC",3,6,1);
        Assert(invasion.WasLegal && invasion.TerritoryCaptured &&
            campaign.Countries["CC"].Controller==1,"legal border invasion transfers country control");
        Assert(campaign.HasSupply("CC"),"newly conquered territory connects to supply network");
        ExpectError(()=>new WorldWarCampaign(new []{
            new CountryTerritory("A","Invalid",1,1,1,1,new[]{"DOESNOTEXIST"}),
            new CountryTerritory("B","Other",2,1,1,2,new string[0])
        }),"reject broken country borders");

        var economy=new RtsEconomy(5500);
        economy.AddOreNode("A",220);
        Assert(economy.Credits==5500 && economy.PowerGenerated==100,
            "economy begins with credits and electricity supply");
        var firstLoad=economy.LoadOre("A",110);
        Assert(firstLoad==110 && economy.Ore["A"].Remaining==110,
            "physical harvester removes finite ore cargo");
        economy.DeliverOre(firstLoad);
        Assert(economy.Credits==5610 && economy.TotalHarvested==110,
            "credits are only gained when the refinery receives ore");
        Assert(economy.QueueUnit("tank") && economy.Credits==4910,
            "tank production reserves resources immediately");
        Assert(economy.PendingUnits==1,"production order is queued");
        economy.Tick(8);
        Assert(economy.CollectCompleted().Count==0,"tank does not spawn before production completes");
        economy.Tick(1);
        var finished=economy.CollectCompleted();
        Assert(finished.Count==1 && finished[0]=="tank",
            "tank spawns after full build duration");
        Assert(economy.CollectCompleted().Count==0,"collecting does not deliver unit twice");
        Assert(!economy.QueueUnit("jet"),"jets are locked until tech center exists");
        Assert(economy.BuildStructure("tech") && economy.TechLevel==3,
            "tech building unlocks advanced aircraft");
        Assert(economy.BuildStructure("factory") && economy.LowPower,
            "additional buildings can overload available power");
        Assert(economy.QueueUnit("jet"),"jet can be queued after tech research");
        economy.Tick(17);
        Assert(economy.CollectCompleted().Count==0,
            "production slows under electrical deficit");
        economy.AddCredits(900); // Test an earned/supplied reserve before adding generation.
        Assert(economy.BuildStructure("reactor") && !economy.LowPower,
            "new reactor restores power to factory");
        economy.Tick(13);
        Assert(economy.CollectCompleted().Count==1,
            "production finishes when sufficient power is restored");
        Assert(economy.LoadOre("A",110)==110 && economy.Ore["A"].Remaining==0,
            "ore field becomes depleted after last load");
        Assert(economy.LoadOre("A",110)==0,"depleted ore does not grant infinite money");
        ExpectError(()=>economy.DeliverOre(-1),"cannot create money using negative cargo");
        ExpectError(()=>economy.Tick(-1),"negative simulation time is rejected");

        var autoHarvester=new RtsEconomy(0);
        autoHarvester.AddOreNode("A",110);
        autoHarvester.AssignHarvester("A");
        autoHarvester.Tick(8);
        Assert(autoHarvester.Credits==110 && autoHarvester.TotalHarvested==110,
            "headless economy can simulate a virtual harvester delivery");

        Console.WriteLine("All "+successes+" headless simulation assertions passed. These do not validate Unity editor or APK.");
    }
}
