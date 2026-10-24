using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackAlert.Simulation
{
    public enum DuelRole { RollingCards, DirectCommander }
    public enum CardEffect { Artillery, EMP, Shield, Repair, Recon, Airstrike, Reinforce, Decoy, Freeze, Supply, AntiAir, NuclearJammer }

    public sealed class TacticalCard
    {
        public string Id { get; private set; }
        public string Label { get; private set; }
        public CardEffect Effect { get; private set; }
        public int CareerLevelRequired { get; private set; }
        public int EnergyCost { get; private set; }
        public float CooldownSeconds { get; private set; }
        public TacticalCard(string id, string label, CardEffect effect, int level, int cost, float cooldown)
        {
            if (string.IsNullOrWhiteSpace(id) || level < 1 || cost < 0 || cooldown < 0)
                throw new ArgumentException("Invalid card.");
            Id = id; Label = label; Effect = effect;
            CareerLevelRequired = level; EnergyCost = cost; CooldownSeconds = cooldown;
        }
    }

    public sealed class CardActivation
    {
        public string CardId { get; private set; }
        public CardEffect Effect { get; private set; }
        public int TargetX { get; private set; }
        public int TargetY { get; private set; }
        public CardActivation(TacticalCard card, int x, int y)
        {
            CardId = card.Id; Effect = card.Effect; TargetX = x; TargetY = y;
        }
    }

    /// <summary>
    /// Deterministic asymmetric Face-Off card economy and spin/draw logic.
    /// Does not render reels, spawn soldiers, or network the match.
    /// </summary>
    public sealed class FaceOffDeck
    {
        private readonly Dictionary<string, TacticalCard> definitions;
        private readonly Dictionary<string, double> readyTimes = new Dictionary<string, double>();
        private readonly List<TacticalCard> eligible;
        private readonly List<TacticalCard> hand = new List<TacticalCard>();
        private uint randomState;
        public IReadOnlyList<TacticalCard> Hand => hand;
        public int CareerLevel { get; private set; }
        public DuelRole Role { get; private set; }
        public int Energy { get; private set; }
        public int MaxEnergy { get; private set; }
        public double MatchSeconds { get; private set; }

        public static TacticalCard[] StandardCards()
        {
            return new [] {
                new TacticalCard("artillery", "Artillery Barrage", CardEffect.Artillery, 1, 3, 16),
                new TacticalCard("shield", "Shield Dome", CardEffect.Shield, 1, 2, 12),
                new TacticalCard("repair", "Emergency Repair", CardEffect.Repair, 1, 1, 9),
                new TacticalCard("recon", "Recon Sweep", CardEffect.Recon, 1, 1, 5),
                new TacticalCard("supply", "Supply Drop", CardEffect.Supply, 2, 2, 10),
                new TacticalCard("reinforce", "Rifleman Squad", CardEffect.Reinforce, 2, 3, 14),
                new TacticalCard("emp", "EMP Pulse", CardEffect.EMP, 3, 4, 19),
                new TacticalCard("antiair", "Air Defense", CardEffect.AntiAir, 3, 2, 13),
                new TacticalCard("decoy", "Decoy Command", CardEffect.Decoy, 4, 2, 9),
                new TacticalCard("freeze", "Cryo Field", CardEffect.Freeze, 5, 4, 18),
                new TacticalCard("airstrike", "Strike Squadron", CardEffect.Airstrike, 6, 5, 23),
                new TacticalCard("jammer", "Nuclear Jammer", CardEffect.NuclearJammer, 7, 5, 28)
            };
        }
        public FaceOffDeck(int level, int seed, DuelRole role = DuelRole.RollingCards)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            CareerLevel = level; Role = role;
            MaxEnergy = 10;
            Energy = 5;
            randomState = (uint)seed;
            if (randomState == 0) randomState = 0xA5B35729u;
            eligible = StandardCards().Where(c => c.CareerLevelRequired <= level).ToList();
            definitions = eligible.ToDictionary(c => c.Id, c => c);
        }
        private uint NextRandom()
        {
            randomState ^= randomState << 13;
            randomState ^= randomState >> 17;
            randomState ^= randomState << 5;
            return randomState;
        }
        public void Tick(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > 3600)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            MatchSeconds += seconds;
            // One energy per 5 seconds, deterministic across differently sized frame steps.
            int energy = (int)(MatchSeconds / 5.0);
            Energy = Math.Min(MaxEnergy, initialEnergy + energy - spentEnergy);
        }
        private int initialEnergy = 5, spentEnergy;
        public TacticalCard Roll()
        {
            if (Role != DuelRole.RollingCards) throw new InvalidOperationException("Direct commander cannot roll cards.");
            if (hand.Count >= 5) throw new InvalidOperationException("Hand full.");
            TacticalCard result = eligible[(int)(NextRandom() % (uint)eligible.Count)];
            hand.Add(result);
            return result;
        }
        public bool CanPlay(string id)
        {
            if (Role != DuelRole.RollingCards || !definitions.TryGetValue(id, out TacticalCard card)) return false;
            if (!hand.Any(c => c.Id == id) || Energy < card.EnergyCost) return false;
            return !readyTimes.TryGetValue(id, out double ready) || ready <= MatchSeconds;
        }
        public CardActivation Play(string id, int x, int y)
        {
            if (x < 0 || y < 0 || x > 2048 || y > 2048)
                throw new ArgumentOutOfRangeException("Target coordinates outside duel map.");
            if (!CanPlay(id)) throw new InvalidOperationException("Card locked, on cooldown, missing or insufficient energy.");
            TacticalCard card = definitions[id];
            spentEnergy += card.EnergyCost;
            Energy = Math.Max(0, Energy - card.EnergyCost);
            readyTimes[id] = MatchSeconds + card.CooldownSeconds;
            int index = hand.FindIndex(c => c.Id == id);
            hand.RemoveAt(index);
            return new CardActivation(card, x, y);
        }
        public double CooldownRemaining(string id)
        {
            return readyTimes.TryGetValue(id, out double end)
                ? Math.Max(0, end - MatchSeconds) : 0;
        }
    }
}
