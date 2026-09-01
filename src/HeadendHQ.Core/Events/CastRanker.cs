using HeadendHQ.Core.Catalog.Sources;

namespace HeadendHQ.Core.Events;

/// <summary>
/// Decides which players a game should bill, and in what order.
/// <para>
/// Lives in the domain rather than in a source adapter because "who matters in this game" is our
/// judgement, not ESPN's. A second source only has to report the signals it knows about; every term
/// is additive, so a missing signal contributes nothing instead of needing a special case.
/// </para>
/// <para>
/// Keyed by sport slug rather than an enum, so a sport we have never seen scores on generic
/// fallbacks instead of failing.
/// </para>
/// </summary>
public static class CastRanker
{
    private const int LeaderScore = 100;
    private const int ProbableStarterScore = 90;
    private const int StarterScore = 60;
    private const int SecondStringScore = 30;
    private const int ReserveScore = 10;
    private const int MaxExperienceBonus = 10;
    private const int QuestionablePenalty = 15;
    private const int OutPenalty = 40;

    private static readonly Dictionary<string, int> FootballWeights = new(StringComparer.OrdinalIgnoreCase)
    {
        ["QB"] = 50,
        ["RB"] = 40, ["FB"] = 30, ["WR"] = 40, ["TE"] = 35,
        ["DE"] = 30, ["EDGE"] = 30, ["LB"] = 28, ["CB"] = 28, ["S"] = 26, ["DT"] = 25,
        ["OT"] = 15, ["G"] = 15, ["OG"] = 15, ["C"] = 15, ["OL"] = 15,
        ["PK"] = 5, ["K"] = 5, ["P"] = 5, ["LS"] = 3,
    };

    // Basketball positions barely differentiate anyone; the depth chart decides the starting five,
    // so these weights stay flat on purpose.
    private static readonly Dictionary<string, int> BasketballWeights = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, int> BaseballWeights = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SP"] = 50, ["C"] = 35, ["SS"] = 32,
        ["1B"] = 30, ["2B"] = 30, ["3B"] = 30, ["CF"] = 30,
        ["LF"] = 28, ["RF"] = 28, ["DH"] = 28,
        ["P"] = 20, ["RP"] = 10,
    };

    private static readonly Dictionary<string, int> HockeyWeights = new(StringComparer.OrdinalIgnoreCase)
    {
        ["G"] = 45, ["C"] = 35, ["LW"] = 33, ["RW"] = 33, ["D"] = 28,
    };

    private static readonly HashSet<string> QuestionableStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Questionable", "Doubtful",
    };

    private static readonly HashSet<string> OutStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Out", "Injured Reserve", "Suspension",
    };

    /// <summary>
    /// Picks at most <paramref name="maxPerTeam"/> from each side, then alternates them so both
    /// teams lead the cast — a client that truncates the actor list would otherwise show only
    /// the home team.
    /// </summary>
    public static IReadOnlyList<CastCandidate> Rank(
        IEnumerable<CastCandidate> candidates, string sportSlug, int maxPerTeam)
    {
        var scored = candidates
            .DistinctBy(c => c.Athlete.ExternalId)
            .Select(c => (Candidate: c, Score: Score(c, sportSlug)))
            .ToList();

        var home = Top(scored, isHome: true, maxPerTeam);
        var away = Top(scored, isHome: false, maxPerTeam);

        return [.. Interleave(home, away)];
    }

    private static List<CastCandidate> Top(
        List<(CastCandidate Candidate, int Score)> scored, bool isHome, int maxPerTeam) =>
        [.. scored
            .Where(s => s.Candidate.IsHome == isHome)
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.Candidate.Athlete.ExperienceYears ?? 0)
            .ThenBy(s => s.Candidate.Athlete.DisplayName)
            .Take(maxPerTeam)
            .Select(s => s.Candidate)];

    public static int Score(CastCandidate candidate, string sportSlug)
    {
        var athlete = candidate.Athlete;
        var score = PositionWeight(sportSlug, athlete.Position);

        if (candidate.IsStatLeader) score += LeaderScore;
        if (candidate.IsProbableStarter) score += ProbableStarterScore;

        if (candidate.IsListedStarter)
            score += StarterScore;
        else
            score += candidate.DepthRank switch
            {
                1 => StarterScore,
                2 => SecondStringScore,
                >= 3 => ReserveScore,
                _ => 0,
            };

        score += Math.Min(athlete.ExperienceYears ?? 0, MaxExperienceBonus);
        score -= InjuryPenalty(candidate.InjuryStatus);

        return score;
    }

    private static int PositionWeight(string sportSlug, string? position)
    {
        var (weights, fallback) = sportSlug.ToLowerInvariant() switch
        {
            "football" => (FootballWeights, 20),
            "basketball" => (BasketballWeights, 30),
            "baseball" => (BaseballWeights, 25),
            "hockey" => (HockeyWeights, 28),
            _ => (BasketballWeights, 25),
        };

        if (position is null)
            return fallback;

        // Sources report either an abbreviation or a display name; try the token as given, then its
        // initials, which is what turns "Starting Pitcher" into "SP".
        if (weights.TryGetValue(position, out var direct))
            return direct;

        var initials = string.Concat(position
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word[0]));

        return weights.GetValueOrDefault(initials, fallback);
    }

    private static int InjuryPenalty(string? status)
    {
        if (status is null)
            return 0;

        if (OutStatuses.Contains(status))
            return OutPenalty;

        return QuestionableStatuses.Contains(status) ? QuestionablePenalty : 0;
    }

    private static IEnumerable<T> Interleave<T>(IReadOnlyList<T> home, IReadOnlyList<T> away)
    {
        for (var i = 0; i < Math.Max(home.Count, away.Count); i++)
        {
            if (i < home.Count) yield return home[i];
            if (i < away.Count) yield return away[i];
        }
    }
}
