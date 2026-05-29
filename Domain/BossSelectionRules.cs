namespace RYZECHo;

internal static class BossSelectionRules
{
    public static bool AllSelectionsSpent(
        IEnumerable<string> candidates,
        IReadOnlyDictionary<string, int> selectionCounts,
        int maxSelectionsPerActor)
    {
        var names = candidates.ToArray();
        return names.Length > 0 && names.All(name => SelectionCount(name, selectionCounts) >= maxSelectionsPerActor);
    }

    public static bool CanSelect(
        string actorName,
        IEnumerable<string> candidates,
        IReadOnlyDictionary<string, int> selectionCounts,
        int maxSelectionsPerActor)
    {
        var names = candidates.ToArray();
        if (!names.Contains(actorName))
        {
            return false;
        }

        return AllSelectionsSpent(names, selectionCounts, maxSelectionsPerActor) ||
               SelectionCount(actorName, selectionCounts) < maxSelectionsPerActor;
    }

    public static int SelectionsRemaining(
        string actorName,
        IEnumerable<string> candidates,
        IReadOnlyDictionary<string, int> selectionCounts,
        int maxSelectionsPerActor)
    {
        var names = candidates.ToArray();
        if (AllSelectionsSpent(names, selectionCounts, maxSelectionsPerActor))
        {
            return 1;
        }

        return Math.Max(0, maxSelectionsPerActor - SelectionCount(actorName, selectionCounts));
    }

    public static string ResolveSelection(
        string requestedActorName,
        IEnumerable<string> candidates,
        IReadOnlyDictionary<string, int> selectionCounts,
        int maxSelectionsPerActor)
    {
        var names = candidates.ToArray();
        if (names.Length == 0)
        {
            return requestedActorName;
        }

        if (CanSelect(requestedActorName, names, selectionCounts, maxSelectionsPerActor))
        {
            return requestedActorName;
        }

        return names.FirstOrDefault(name => CanSelect(name, names, selectionCounts, maxSelectionsPerActor)) ?? names[0];
    }

    private static int SelectionCount(string actorName, IReadOnlyDictionary<string, int> selectionCounts)
    {
        return selectionCounts.TryGetValue(actorName, out var count) ? count : 0;
    }
}
