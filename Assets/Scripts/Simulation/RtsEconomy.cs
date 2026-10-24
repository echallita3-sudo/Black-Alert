using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackAlert.Simulation
{
    /// <summary>
    /// Pure game-rules economy: credits are earned by harvesting finite ore, never by
    /// magic frame-rate increments. Production respects available power, tech and cost.
    /// The Unity harvester drive/animation and refinery visuals integrate separately.
    /// </summary>
    public sealed class RtsEconomy
    {
        public sealed class Blueprint
        {
            public string Id { get; private set; }
            public int Cost { get; private set; }
            public double BuildSeconds { get; private set; }
            public int TechRequired { get; private set; }
            public Blueprint(string id,int cost,double seconds,int tech = 1)
            {
                Id=id;Cost=cost;BuildSeconds=seconds;TechRequired=tech;
            }
        }
        public sealed class OreNode
        {
            public string Id { get; private set; }
            public int Remaining { get; internal set; }
            public OreNode(string id,int remaining)
            {
                if(string.IsNullOrWhiteSpace(id) || remaining<0)
                    throw new ArgumentException("Invalid ore patch.");
                Id=id;Remaining=remaining;
            }
        }
        public sealed class ProductionOrder
        {
            public string UnitId { get; private set; }
            public double RemainingSeconds { get; internal set; }
            public ProductionOrder(Blueprint blueprint)
            {
                UnitId=blueprint.Id;
                RemainingSeconds=blueprint.BuildSeconds;
            }
        }
        private readonly Dictionary<string,Blueprint> units;
        private readonly Dictionary<string,OreNode> ore = new Dictionary<string,OreNode>();
        private readonly Dictionary<string,int> buildings = new Dictionary<string,int>();
        private readonly Queue<ProductionOrder> production = new Queue<ProductionOrder>();
        private readonly List<string> completed = new List<string>();
        private string harvesterNode;
        private double harvesterTimer;
        public int Credits { get; private set; }
        public int PowerGenerated { get; private set; }
        public int PowerConsumed { get; private set; }
        public int TechLevel { get; private set; }
        public int TotalHarvested { get; private set; }
        public int PendingUnits { get { return production.Count; } }
        public bool LowPower { get { return PowerConsumed > PowerGenerated; } }
        public IReadOnlyDictionary<string,OreNode> Ore => ore;

        public RtsEconomy(int startingCredits)
        {
            if(startingCredits<0)throw new ArgumentOutOfRangeException(nameof(startingCredits));
            Credits=startingCredits;
            TechLevel=2; PowerGenerated=100; PowerConsumed=55;
            buildings["reactor"]=1;buildings["refinery"]=1;buildings["factory"]=1;
            units=new Dictionary<string,Blueprint>
            {
                ["rifleman"]=new Blueprint("rifleman",100,3),
                ["tank"]=new Blueprint("tank",700,9,2),
                ["helicopter"]=new Blueprint("helicopter",1200,13,2),
                ["jet"]=new Blueprint("jet",1600,17,3),
                ["destroyer"]=new Blueprint("destroyer",2200,21,3)
            };
        }

        public void AddOreNode(string id,int unitsRemaining)
        {
            if(ore.ContainsKey(id))throw new ArgumentException("Duplicate ore patch.");
            ore.Add(id,new OreNode(id,unitsRemaining));
        }
        public void AssignHarvester(string oreId)
        {
            if(!ore.ContainsKey(oreId))throw new ArgumentException("Unknown ore patch.");
            if(GetBuildings("refinery")==0)throw new InvalidOperationException("A refinery is required.");
            harvesterNode=oreId;
            harvesterTimer=0;
        }
        public int GetBuildings(string id)
        {
            return buildings.TryGetValue(id,out int n) ? n : 0;
        }
        public bool TrySpend(int amount)
        {
            if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
            if(amount>Credits)return false;
            Credits-=amount;
            return true;
        }
        public void AddCredits(int amount)
        {
            if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
            Credits=checked(Credits+amount);
        }

        public bool BuildStructure(string id)
        {
            int cost,power;
            switch(id)
            {
                case "reactor":cost=900;power=-85;break;
                case "refinery":cost=1500;power=20;break;
                case "factory":cost=1400;power=25;break;
                case "tech":cost=1600;power=40;break;
                default:return false;
            }
            if(Credits<cost)return false;
            Credits-=cost;
            buildings[id]=GetBuildings(id)+1;
            if(power<0)PowerGenerated-=power;
            else PowerConsumed+=power;
            if(id=="tech")TechLevel=3;
            return true;
        }
        public bool QueueUnit(string id)
        {
            if(!units.TryGetValue(id,out Blueprint blueprint) || PendingUnits>=8)
                return false;
            if(TechLevel<blueprint.TechRequired ||
                Credits<blueprint.Cost || GetBuildings("factory")==0)
                return false;
            Credits-=blueprint.Cost;
            production.Enqueue(new ProductionOrder(blueprint));
            return true;
        }
        public string FirstQueuedUnit
        {
            get{return production.Count>0?production.Peek().UnitId:null;}
        }
        public double FirstQueueRemaining
        {
            get{return production.Count>0?production.Peek().RemainingSeconds:0;}
        }
        public IReadOnlyList<string> CollectCompleted()
        {
            var collected=completed.ToArray();
            completed.Clear();
            return collected;
        }
        public void Tick(double seconds)
        {
            if(double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds<0 || seconds>3600)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if(harvesterNode!=null)
            {
                harvesterTimer+=seconds;
                while(harvesterTimer>=8)
                {
                    harvesterTimer-=8;
                    var patch=ore[harvesterNode];
                    int shipment=Math.Min(patch.Remaining,110);
                    patch.Remaining-=shipment;
                    Credits+=shipment;
                    TotalHarvested+=shipment;
                    if(patch.Remaining==0){harvesterNode=null;break;}
                }
            }
            if(production.Count>0)
            {
                double effective=seconds*(LowPower ? 0.25 : 1.0);
                while(effective>0 && production.Count>0)
                {
                    var active=production.Peek();
                    double consumed=Math.Min(effective,active.RemainingSeconds);
                    active.RemainingSeconds-=consumed;
                    effective-=consumed;
                    if(active.RemainingSeconds>1e-6) break;
                    completed.Add(active.UnitId);
                    production.Dequeue();
                }
            }
        }
    }
}
