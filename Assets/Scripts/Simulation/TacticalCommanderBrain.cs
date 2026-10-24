using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackAlert.Simulation
{
    /// <summary>
    /// Deterministic, visibility-constrained offline AI tactical decisions.
    /// The caller must pass ONLY currently observed enemy contacts.
    /// This class neither sees hidden Unity scene objects nor spends money itself.
    /// </summary>
    public enum AiUnitType { Rifleman, Tank, Helicopter, Jet, Destroyer }
    public enum AiOrderType { Advance, Engage, Capture, Retreat, Guard }
    public enum AiStrategy { Hold, Recon, Assault, Defend, Capture, Regroup }

    public sealed class AiUnitIntel
    {
        public int Id { get; }
        public AiUnitType Type { get; }
        public float X { get; }
        public float Z { get; }
        public float Health01 { get; }
        public AiUnitIntel(int id,AiUnitType type,float x,float z,float health01)
        {
            if(id<0 || float.IsNaN(x) || float.IsNaN(z) ||
               float.IsInfinity(x) || float.IsInfinity(z) ||
               float.IsNaN(health01) || health01<0 || health01>1)
                throw new ArgumentException("Invalid unit observation.");
            Id=id;Type=type;X=x;Z=z;Health01=health01;
        }
    }

    public sealed class AiObjective
    {
        public float X { get; }
        public float Z { get; }
        public bool Contested { get; }
        public AiObjective(float x,float z,bool contested)
        {
            if(float.IsNaN(x) || float.IsNaN(z) ||
               float.IsInfinity(x) || float.IsInfinity(z))
                throw new ArgumentException("Invalid objective location.");
            X=x;Z=z;Contested=contested;
        }
    }

    public sealed class AiCommanderSituation
    {
        public BattleMode Mode { get; }
        public float HomeX { get; }
        public float HomeZ { get; }
        public float EnemyBaseX { get; }
        public float EnemyBaseZ { get; }
        public int Credits { get; }
        public int Technology { get; }
        public int QueueCount { get; }
        public bool LowPower { get; }
        public IReadOnlyList<AiUnitIntel> Friendly { get; }
        public IReadOnlyList<AiUnitIntel> Contacts { get; }
        public IReadOnlyList<AiObjective> Objectives { get; }

        public AiCommanderSituation(BattleMode mode,float homeX,float homeZ,
            float enemyBaseX,float enemyBaseZ,int credits,int technology,
            int queueCount,bool lowPower,IEnumerable<AiUnitIntel> friendly,
            IEnumerable<AiUnitIntel> visibleContacts,IEnumerable<AiObjective> objectives)
        {
            if(credits<0 || technology<1 || queueCount<0)
                throw new ArgumentException("Invalid economy snapshot.");
            Mode=mode;HomeX=homeX;HomeZ=homeZ;
            EnemyBaseX=enemyBaseX;EnemyBaseZ=enemyBaseZ;
            Credits=credits;Technology=technology;QueueCount=queueCount;LowPower=lowPower;
            Friendly=(friendly??throw new ArgumentNullException(nameof(friendly))).ToArray();
            Contacts=(visibleContacts??throw new ArgumentNullException(nameof(visibleContacts))).ToArray();
            Objectives=(objectives??throw new ArgumentNullException(nameof(objectives))).ToArray();
            if(Friendly.Select(u=>u.Id).Distinct().Count()!=Friendly.Count)
                throw new ArgumentException("Friendly unit IDs must be unique.");
        }
    }

    public sealed class AiUnitOrder
    {
        public int UnitId { get; }
        public AiOrderType Action { get; }
        public float TargetX { get; }
        public float TargetZ { get; }
        public int TargetEnemyId { get; }
        public AiUnitOrder(int id,AiOrderType action,float x,float z,int targetEnemyId=-1)
        {
            UnitId=id;Action=action;TargetX=x;TargetZ=z;
            TargetEnemyId=targetEnemyId;
        }
    }

    public sealed class AiCommanderPlan
    {
        public AiStrategy Strategy { get; }
        public string ProductionUnit { get; }
        public string Structure { get; }
        public IReadOnlyList<AiUnitOrder> Orders { get; }
        public AiCommanderPlan(AiStrategy strategy,string productionUnit,string structure,
            IReadOnlyList<AiUnitOrder> orders)
        {
            Strategy=strategy;ProductionUnit=productionUnit;Structure=structure;Orders=orders;
        }
    }

    public sealed class TacticalCommanderBrain
    {
        public int Difficulty { get; }
        public TacticalCommanderBrain(int difficulty=2)
        {
            if(difficulty<1 || difficulty>3)
                throw new ArgumentOutOfRangeException(nameof(difficulty));
            Difficulty=difficulty;
        }

        private static float DistanceSquared(float x,float z,float ex,float ez)
        {
            float dx=x-ex,dz=z-ez;
            return dx*dx+dz*dz;
        }
        private static bool IsAir(AiUnitType type) =>
            type==AiUnitType.Jet || type==AiUnitType.Helicopter;
        private static float AttackSuitability(AiUnitType shooter,AiUnitType target)
        {
            if(shooter==AiUnitType.Rifleman && IsAir(target))return .18f;
            if(shooter==AiUnitType.Destroyer && IsAir(target))return 1.5f;
            if(shooter==AiUnitType.Jet && IsAir(target))return 1.8f;
            if(shooter==AiUnitType.Helicopter && target==AiUnitType.Tank)return 1.7f;
            if(shooter==AiUnitType.Tank && IsAir(target))return .58f;
            if(shooter==AiUnitType.Rifleman && target==AiUnitType.Tank)return .58f;
            if(shooter==AiUnitType.Tank && target==AiUnitType.Rifleman)return 1.12f;
            return 1f;
        }

        private static AiUnitIntel PickEnemy(AiUnitIntel unit,
                                              IReadOnlyList<AiUnitIntel> visibleContacts,
                                              float homeX,float homeZ)
        {
            AiUnitIntel best=null;
            float highest=float.NegativeInfinity;
            foreach(var enemy in visibleContacts)
            {
                float distance=DistanceSquared(unit.X,unit.Z,enemy.X,enemy.Z);
                float score=AttackSuitability(unit.Type,enemy.Type)*35f
                    -(float)Math.Sqrt(distance)*1.35f
                    +(1f-enemy.Health01)*11f;
                if(DistanceSquared(homeX,homeZ,enemy.X,enemy.Z)<225f)
                    score+=22f;
                if(best==null || score>highest || score==highest && enemy.Id<best.Id)
                {best=enemy;highest=score;}
            }
            return best;
        }

        private static AiObjective ChooseObjective(AiUnitIntel unit,
            IReadOnlyList<AiObjective> objectives)
        {
            AiObjective best=null;
            float score=float.MaxValue;
            foreach(var objective in objectives)
            {
                float distance=DistanceSquared(unit.X,unit.Z,objective.X,objective.Z);
                if(objective.Contested)distance*=.68f;
                if(distance<score)
                {score=distance;best=objective;}
            }
            return best;
        }

        private static string ChooseProduction(AiCommanderSituation s)
        {
            if(s.QueueCount >= (s.Mode==BattleMode.Survival?2:4))return null;
            int total=s.Friendly.Count+s.QueueCount;
            if(total>=32)return null;
            int tanks=s.Friendly.Count(u=>u.Type==AiUnitType.Tank);
            int infantry=s.Friendly.Count(u=>u.Type==AiUnitType.Rifleman);
            int air=s.Friendly.Count(u=>IsAir(u.Type));
            bool enemyAircraft=s.Contacts.Any(u=>IsAir(u.Type));
            if(enemyAircraft && s.Technology>=3 && s.Credits>=1600 && air<3)return "jet";
            if(tanks<Math.Max(2,total/4) && s.Credits>=700)return "tank";
            if(s.Mode==BattleMode.Battlefield && infantry<6 && s.Credits>=100)
                return "rifleman";
            if(s.Technology>=3 && air<Math.Max(2,total/5) && s.Credits>=1600)
                return "jet";
            if(s.Credits>=1200 && air<3)return "helicopter";
            if(s.Credits>=700)return "tank";
            return s.Credits>=100 ? "rifleman" : null;
        }

        public AiCommanderPlan Decide(AiCommanderSituation situation)
        {
            if(situation==null)throw new ArgumentNullException(nameof(situation));
            var ours=situation.Friendly;
            var threats=situation.Contacts;
            bool threatsNearBase=threats.Any(e=>DistanceSquared(e.X,e.Z,
                situation.HomeX,situation.HomeZ)<22f*22f);
            bool depleted=ours.Count<4;
            AiStrategy strategic=situation.Mode==BattleMode.Battlefield
                ? AiStrategy.Capture
                : threatsNearBase?AiStrategy.Defend
                : depleted?AiStrategy.Regroup
                : threats.Count==0?AiStrategy.Recon:AiStrategy.Assault;

            string structure=null;
            if(situation.LowPower && situation.Credits>=900)structure="reactor";
            else if(situation.Technology<3 && situation.Credits>=3000 &&
                    situation.Mode!=BattleMode.Survival)
                structure="tech";
            string production=structure==null?ChooseProduction(situation):null;

            var orders=new List<AiUnitOrder>();
            // Stable iteration, no random teleportation or accessing the full hidden Unity scene.
            foreach(var unit in ours.OrderBy(u=>u.Id))
            {
                if(orders.Count>=64)break;
                AiUnitIntel enemy=PickEnemy(unit,threats,situation.HomeX,situation.HomeZ);
                bool vulnerable=unit.Health01<.27f && unit.Type!=AiUnitType.Destroyer;
                if(vulnerable && !threatsNearBase)
                {
                    orders.Add(new AiUnitOrder(unit.Id,AiOrderType.Retreat,
                        situation.HomeX,situation.HomeZ));
                    continue;
                }
                if(unit.Type==AiUnitType.Destroyer)
                {
                    // Ships guard river approaches rather than trying to drive overland.
                    float riverZ=unit.Z>=0 ? 18f:-18f;
                    orders.Add(new AiUnitOrder(unit.Id,AiOrderType.Guard,0f,riverZ));
                    continue;
                }
                if(enemy!=null)
                {
                    // Hold infantry farther from armor, push jet/helicopter into mobile pursuit.
                    float standoff=unit.Type==AiUnitType.Rifleman?7f:
                        IsAir(unit.Type)?5f:4f;
                    float dx=unit.X-enemy.X,dz=unit.Z-enemy.Z;
                    float length=(float)Math.Sqrt(dx*dx+dz*dz);
                    if(length<.01f)length=1f;
                    float goalX=enemy.X+dx/length*standoff;
                    float goalZ=enemy.Z+dz/length*standoff;
                    orders.Add(new AiUnitOrder(unit.Id,AiOrderType.Engage,
                        goalX,goalZ,enemy.Id));
                    continue;
                }
                if(situation.Mode==BattleMode.Battlefield &&
                   unit.Type==AiUnitType.Rifleman && situation.Objectives.Count>0)
                {
                    var target=ChooseObjective(unit,situation.Objectives);
                    orders.Add(new AiUnitOrder(unit.Id,AiOrderType.Capture,target.X,target.Z));
                    continue;
                }
                if(situation.Mode==BattleMode.Survival)
                {
                    // Gather near center before storm rings close. Do not suicide into borders.
                    orders.Add(new AiUnitOrder(unit.Id,AiOrderType.Advance,unit.Id%5-2,
                        (unit.Id/5)%5-2));
                    continue;
                }
                if(depleted && unit.Type!=AiUnitType.Jet)
                {
                    orders.Add(new AiUnitOrder(unit.Id,AiOrderType.Guard,
                        situation.HomeX-7f,situation.HomeZ+(unit.Id%4-2)*2f));
                    continue;
                }
                // Recon routes are deterministically fanned across the central battlefield.
                float side=(unit.Id%5-2)*5f;
                orders.Add(new AiUnitOrder(unit.Id,AiOrderType.Advance,
                    (situation.HomeX+situation.EnemyBaseX)*.5f,
                    (situation.HomeZ+situation.EnemyBaseZ)*.5f+side));
            }
            return new AiCommanderPlan(strategic,production,structure,orders);
        }
    }
}
