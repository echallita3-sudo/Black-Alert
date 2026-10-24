using System;

namespace BlackAlert.Simulation
{
    /// <summary>
    /// Deterministic RTS headquarters hit points and victory condition.
    /// Rendering, colliders, cover, hit location and physical rubble belong to Unity.
    /// </summary>
    public sealed class StrongholdBattle
    {
        public const int DefaultHealth=1200;
        private readonly int[] hp=new int[2];
        public int Winner {get;private set;}=-1;
        public bool Finished {get;private set;}
        public int MaxHealth {get;}
        public int Health(int faction)
        {
            if(faction<0||faction>1)throw new ArgumentOutOfRangeException(nameof(faction));
            return hp[faction];
        }
        public StrongholdBattle(int maxHealth=DefaultHealth)
        {
            if(maxHealth<=0)throw new ArgumentOutOfRangeException(nameof(maxHealth));
            MaxHealth=maxHealth;
            Reset();
        }
        public void Reset()
        {
            hp[0]=hp[1]=MaxHealth;
            Finished=false;
            Winner=-1;
        }
        public int Damage(int targetFaction,int amount)
        {
            if(targetFaction<0 || targetFaction>1)
                throw new ArgumentOutOfRangeException(nameof(targetFaction));
            if(amount<=0)throw new ArgumentOutOfRangeException(nameof(amount));
            if(Finished)return 0;
            int applied=Math.Min(amount,hp[targetFaction]);
            hp[targetFaction]-=applied;
            if(hp[targetFaction]==0)
            {
                Finished=true;
                Winner=1-targetFaction;
            }
            return applied;
        }
    }
}
