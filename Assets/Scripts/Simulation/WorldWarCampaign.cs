using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackAlert.Simulation
{
    /// <summary>
    /// Persistent world-campaign game rules. Nation records, borders and strategic
    /// adjacency are injected by data; a full validated global dataset is still required.
    /// These rules are not the live tactical battle or worldwide server implementation.
    /// </summary>
    public sealed class CountryTerritory
    {
        public string CountryCode { get; private set; }
        public string DisplayName { get; private set; }
        public int Controller { get; internal set; }
        public int Armies { get; internal set; }
        public int Resources { get; private set; }
        public int CapitalOf { get; private set; }
        public HashSet<string> Neighbors { get; private set; }

        public CountryTerritory(string code, string label, int owner, int armies,
                                int output, int capitalOf, IEnumerable<string> neighbors)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(label)
                || armies < 0 || output < 0 || owner < 0)
                throw new ArgumentException("Invalid campaign territory.");
            CountryCode=code.ToUpperInvariant(); DisplayName=label; Controller=owner;
            Armies=armies; Resources=output; CapitalOf=capitalOf;
            Neighbors=new HashSet<string>(neighbors.Select(c=>c.ToUpperInvariant()));
        }
    }

    public sealed class CampaignResult
    {
        public bool WasLegal { get; private set; }
        public bool TerritoryCaptured { get; private set; }
        public int NewController { get; private set; }
        public int AttackerSurvivors { get; private set; }
        public int DefenderSurvivors { get; private set; }
        public CampaignResult(bool legal,bool captured,int controller,int attacker,int defender)
        {
            WasLegal=legal;TerritoryCaptured=captured;NewController=controller;
            AttackerSurvivors=attacker;DefenderSurvivors=defender;
        }
    }

    public sealed class WorldWarCampaign
    {
        private readonly Dictionary<string,CountryTerritory> countries;
        private readonly Dictionary<int,int> treasury=new Dictionary<int,int>();
        private readonly HashSet<long> alliances=new HashSet<long>();
        public int TurnNumber { get; private set; } = 1;
        public IReadOnlyDictionary<string,CountryTerritory> Countries => countries;
        public IReadOnlyDictionary<int,int> Treasury => treasury;

        private static long AllianceKey(int x,int y)
        {
            int a=Math.Min(x,y),b=Math.Max(x,y);
            return ((long)a << 32) | (uint)b;
        }

        public WorldWarCampaign(IEnumerable<CountryTerritory> territoryData)
        {
            if (territoryData == null) throw new ArgumentNullException(nameof(territoryData));
            var list=territoryData.ToList();
            countries=new Dictionary<string,CountryTerritory>(StringComparer.OrdinalIgnoreCase);
            foreach(var t in list)
            {
                if (countries.ContainsKey(t.CountryCode))
                    throw new ArgumentException("Duplicate country code.");
                countries.Add(t.CountryCode,t);
                if(!treasury.ContainsKey(t.Controller)) treasury[t.Controller]=0;
            }
            if(countries.Count < 2) throw new ArgumentException("At least two countries required.");
            foreach(var t in list)
                foreach(string neighbor in t.Neighbors)
                {
                    if(!countries.ContainsKey(neighbor))
                        throw new ArgumentException(t.CountryCode+" names unknown neighbor "+neighbor);
                    if(!countries[neighbor].Neighbors.Contains(t.CountryCode))
                        throw new ArgumentException("Borders must be bidirectional: "+t.CountryCode+"/"+neighbor);
                }
        }

        public void Alliance(int factionA,int factionB,bool enabled)
        {
            if(factionA==factionB || !treasury.ContainsKey(factionA) || !treasury.ContainsKey(factionB))
                throw new ArgumentException("Two valid different factions required.");
            var key=AllianceKey(factionA,factionB);
            if(enabled) alliances.Add(key); else alliances.Remove(key);
        }
        public bool IsAllied(int a,int b) => a==b || alliances.Contains(AllianceKey(a,b));
        public bool HasSupply(string country)
        {
            var start=Require(country);
            int owner=start.Controller;
            var visited=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue=new Queue<string>();
            queue.Enqueue(start.CountryCode);visited.Add(start.CountryCode);
            while(queue.Count>0)
            {
                var code=queue.Dequeue();var t=countries[code];
                if(t.CapitalOf==owner) return true;
                foreach(string n in t.Neighbors)
                {
                    if(visited.Add(n) && countries[n].Controller==owner)
                        queue.Enqueue(n);
                }
            }
            return false;
        }
        private CountryTerritory Require(string code)
        {
            if(!countries.TryGetValue(code,out CountryTerritory t))
                throw new ArgumentException("Unknown country "+code);
            return t;
        }
        public int ControlledTerritories(int faction) =>
            countries.Values.Count(t=>t.Controller==faction);

        public void AdvanceTurn()
        {
            TurnNumber++;
            foreach(var t in countries.Values)
                if(HasSupply(t.CountryCode))
                    treasury[t.Controller]=treasury.TryGetValue(t.Controller,out int old)
                        ? old+t.Resources : t.Resources;
        }
        public void Reinforce(int faction,string country,int units)
        {
            if(units <= 0) throw new ArgumentOutOfRangeException(nameof(units));
            var t=Require(country);
            if(t.Controller!=faction || !HasSupply(country))
                throw new InvalidOperationException("Cannot reinforce territory outside supplied faction.");
            int cost=units*10;
            if(!treasury.TryGetValue(faction,out int funds) || funds<cost)
                throw new InvalidOperationException("Insufficient treasury.");
            treasury[faction]-=cost;t.Armies+=units;
        }
        public CampaignResult Invade(string from,string to,int attackingArmies,
                                     int attackerRoll,int defenderRoll)
        {
            var origin=Require(from);var target=Require(to);
            if(attackingArmies<=0 || attackingArmies>=origin.Armies
                || !origin.Neighbors.Contains(target.CountryCode)
                || origin.Controller==target.Controller
                || IsAllied(origin.Controller,target.Controller)
                || !HasSupply(origin.CountryCode))
                return new CampaignResult(false,false,target.Controller,0,target.Armies);
            if(attackerRoll<1 || attackerRoll>6 || defenderRoll<1 || defenderRoll>6)
                throw new ArgumentOutOfRangeException("Rolls must be 1–6, supplied by an authoritative service.");
            // This is a strategy-layer skirmish resolver. Live tactical RTS resolution
            // will replace it; random rolls must come from the trusted match authority.
            int attrition = attackerRoll > defenderRoll
                ? Math.Min(target.Armies,Math.Max(1,attackingArmies/2))
                : Math.Min(attackingArmies,Math.Max(1,target.Armies/2));
            if(attackerRoll>defenderRoll)target.Armies-=attrition;
            else attackingArmies-=attrition;
            if(target.Armies==0 && attackingArmies>0)
            {
                int survivor=attackingArmies;
                origin.Armies-=survivor;
                target.Controller=origin.Controller;
                target.Armies=survivor;
                return new CampaignResult(true,true,target.Controller,survivor,0);
            }
            if(attackerRoll<=defenderRoll)
                origin.Armies=Math.Max(1,origin.Armies-attrition);
            return new CampaignResult(true,false,target.Controller,attackingArmies,target.Armies);
        }
    }
}
