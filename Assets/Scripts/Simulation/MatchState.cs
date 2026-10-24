using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackAlert.Simulation
{
    // Headless C# simulation rules: usable in Unity or a free .NET runner.
    // Rendering, online transport and account hosting are separate integrations.
    public enum BattleMode { Skirmish, Battlefield, Survival, FaceOffDuel, WorldWarIII }
    public enum BattleOutcome { InProgress, Victory, Draw }

    public sealed class MatchSettings
    {
        public BattleMode Mode { get; private set; }
        public int MaximumPlayers { get; private set; }
        public int StartingTickets { get; private set; }
        public double SurvivalRingSeconds { get; private set; }

        public MatchSettings(BattleMode mode, int maximumPlayers, int startingTickets = 500,
                             double survivalRingSeconds = 90)
        {
            if (maximumPlayers < 2 || maximumPlayers > 8)
                throw new ArgumentOutOfRangeException(nameof(maximumPlayers), "A match supports 2–8 players.");
            if (mode == BattleMode.FaceOffDuel && maximumPlayers != 2)
                throw new ArgumentException("Face-Off is strictly one commander against one commander.");
            if (startingTickets <= 0 || survivalRingSeconds <= 0)
                throw new ArgumentOutOfRangeException("Ticket count and survival ring period must be positive.");
            Mode = mode; MaximumPlayers = maximumPlayers;
            StartingTickets = startingTickets; SurvivalRingSeconds = survivalRingSeconds;
        }
    }

    public sealed class Commander
    {
        public int Id { get; private set; }
        public int Team { get; private set; }
        public bool Alive { get; internal set; } = true;
        public int UnitCount { get; internal set; }
        public bool HeadquartersAlive { get; internal set; } = true;
        public int Kills { get; internal set; }
        public int Losses { get; internal set; }
        public Commander(int id, int team)
        {
            if (id < 0 || team < 0) throw new ArgumentOutOfRangeException();
            Id = id; Team = team;
        }
    }

    public sealed class MatchState
    {
        private readonly Dictionary<int, Commander> commanders = new Dictionary<int, Commander>();
        private readonly Dictionary<int, int> tickets = new Dictionary<int, int>();
        private readonly Dictionary<int, int> sectors = new Dictionary<int, int>();
        private readonly MatchSettings config;
        private double scoreAccumulator;
        private double elapsed;
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.InProgress;
        public int WinningTeam { get; private set; } = -1;
        public int RingStage { get; private set; }
        public IReadOnlyDictionary<int, Commander> Commanders => commanders;
        public IReadOnlyDictionary<int, int> Tickets => tickets;
        public IReadOnlyDictionary<int, int> Sectors => sectors;
        public double ElapsedSeconds => elapsed;

        public MatchState(MatchSettings settings)
        {
            config = settings ?? throw new ArgumentNullException(nameof(settings));
        }
        public void AddCommander(int id, int team)
        {
            if (isStarted || elapsed != 0 || Outcome != BattleOutcome.InProgress)
                throw new InvalidOperationException("Players must be registered before a match starts.");
            if (commanders.Count >= config.MaximumPlayers)
                throw new InvalidOperationException("Room is full.");
            if (commanders.ContainsKey(id)) throw new InvalidOperationException("Duplicate player ID.");
            commanders.Add(id, new Commander(id, team));
            if (!tickets.ContainsKey(team)) tickets[team] = config.StartingTickets;
        }
        public void Begin()
        {
            if (isStarted) throw new InvalidOperationException("Match already begun.");
            if (commanders.Count < 2) throw new InvalidOperationException("At least two players required.");
            if (commanders.Values.Select(c => c.Team).Distinct().Count() < 2)
                throw new InvalidOperationException("At least two opposing teams required.");
            isStarted = true;
        }
        private bool isStarted;
        private void EnsureRunning()
        {
            if (!isStarted || Outcome != BattleOutcome.InProgress)
                throw new InvalidOperationException("Match has not started or has finished.");
        }
        public int GetTickets(int team) => tickets.TryGetValue(team, out int value) ? value : 0;
        public void CaptureSector(int sector, int ownerTeam)
        {
            EnsureRunning();
            if (config.Mode != BattleMode.Battlefield)
                throw new InvalidOperationException("Sectors apply to Battlefield mode only.");
            if (!tickets.ContainsKey(ownerTeam)) throw new ArgumentException("Unknown team.");
            if (sector < 0 || sector >= 64) throw new ArgumentOutOfRangeException(nameof(sector));
            sectors[sector] = ownerTeam;
        }
        public void RegisterUnit(int commanderId)
        {
            EnsureRunning(); Commander c = Require(commanderId);
            if (!c.Alive) throw new InvalidOperationException("Commander eliminated.");
            c.UnitCount++;
        }
        public void UnitDestroyed(int victimId, int attackerId)
        {
            EnsureRunning();
            Commander v = Require(victimId), a = Require(attackerId);
            v.UnitCount = Math.Max(0, v.UnitCount - 1);
            v.Losses++;
            if (v.Team != a.Team) a.Kills++;
            // Destruction of a single army unit is not instant victory.
        }
        public void Eliminate(int commanderId)
        {
            EnsureRunning();
            var c = Require(commanderId);
            c.Alive = false;
            c.HeadquartersAlive = false;
            c.UnitCount = 0;
            CheckWin();
        }
        public void Tick(double deltaSeconds)
        {
            EnsureRunning();
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds)
                || deltaSeconds < 0 || deltaSeconds > 3600)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            elapsed += deltaSeconds;
            RingStage = config.Mode == BattleMode.Survival
                ? (int)(elapsed / config.SurvivalRingSeconds) : 0;
            if (config.Mode == BattleMode.Battlefield)
            {
                scoreAccumulator += deltaSeconds;
                int whole = (int)Math.Floor(scoreAccumulator);
                scoreAccumulator -= whole;
                if (whole > 0)
                {
                    foreach (int team in tickets.Keys.ToArray())
                    {
                        int enemyOwned = sectors.Values.Count(owner => owner != team);
                        tickets[team] = Math.Max(0, tickets[team] - whole * enemyOwned);
                    }
                    var surviving = tickets.Where(x => x.Value > 0).Select(x => x.Key).ToArray();
                    if (surviving.Length == 1) { Outcome = BattleOutcome.Victory; WinningTeam = surviving[0]; }
                    else if (surviving.Length == 0) { Outcome = BattleOutcome.Draw; WinningTeam = -1; }
                }
            }
        }
        private Commander Require(int id)
        {
            if (!commanders.TryGetValue(id, out Commander c))
                throw new ArgumentException("Unknown commander: " + id);
            return c;
        }
        private void CheckWin()
        {
            int[] surviving = commanders.Values.Where(c => c.Alive)
                .Select(c => c.Team).Distinct().ToArray();
            if (surviving.Length == 1)
            {
                Outcome = BattleOutcome.Victory;
                WinningTeam = surviving[0];
            }
            else if (surviving.Length == 0) Outcome = BattleOutcome.Draw;
        }
    }
}
