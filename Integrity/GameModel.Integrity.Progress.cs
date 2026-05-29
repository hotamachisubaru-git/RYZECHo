using System.Security.Cryptography;
using System.Text;

namespace RYZECHo;

internal sealed partial class GameModel
{
    private static byte[] ProgressIntegrityEntropy()
    {
        return Encoding.UTF8.GetBytes($"{ProgressIntegrityVersion}|{AppContext.BaseDirectory}|RYZECHo::Prototype");
    }

    private static string EnsureProgressSalt(string? salt)
    {
        return string.IsNullOrWhiteSpace(salt) ? Guid.NewGuid().ToString("N") : salt;
    }

    private static string CreateProgressIntegrityPayload(ProgressProfile profile)
    {
        static string JoinDistinct(IEnumerable<string> values)
        {
            return string.Join("|", values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal));
        }

        return string.Join("::",
        [
            ProgressIntegrityVersion,
            EnsureProgressSalt(profile.IntegritySalt),
            profile.AccountLevel.ToString(),
            profile.CurrentXp.ToString(),
            profile.AgentCredits.ToString(),
            profile.CosmeticTokens.ToString(),
            profile.LifetimeAdImpressions.ToString(),
            profile.StoreCursor.ToString(),
            profile.RankRating.ToString(),
            profile.MatchesPlayed.ToString(),
            profile.MatchesWon.ToString(),
            profile.ContractsCompleted.ToString(),
            profile.ActiveContract,
            profile.ActiveContractProgress.ToString(),
            JoinDistinct(profile.UnlockedAgents),
            JoinDistinct(profile.UnlockedStructureSkins),
            JoinDistinct(profile.UnlockedAdThemes),
            JoinDistinct(profile.UnlockedBanners),
            JoinDistinct(profile.UnlockedKillEffects),
            profile.SelectedStructureSkin,
            profile.SelectedAdTheme,
            profile.SelectedBanner,
            profile.SelectedKillEffect,
        ]);
    }

    private static string CreateProgressIntegrityStamp(ProgressProfile profile)
    {
        profile.IntegritySalt = EnsureProgressSalt(profile.IntegritySalt);
        var payloadBytes = SHA256.HashData(Encoding.UTF8.GetBytes(CreateProgressIntegrityPayload(profile)));
        var protectedHash = ProtectedData.Protect(payloadBytes, ProgressIntegrityEntropy(), DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedHash);
    }

    private static bool HasValidProgressIntegrity(ProgressProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.IntegritySalt) || string.IsNullOrWhiteSpace(profile.IntegrityStamp))
        {
            return true;
        }

        try
        {
            var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(CreateProgressIntegrityPayload(profile)));
            var storedHash = ProtectedData.Unprotect(Convert.FromBase64String(profile.IntegrityStamp), ProgressIntegrityEntropy(), DataProtectionScope.CurrentUser);
            return CryptographicOperations.FixedTimeEquals(expectedHash, storedHash);
        }
        catch
        {
            return false;
        }
    }

    private static void NormalizeProgressList(List<string> source, IEnumerable<string> allowedValues, int maxCount = 16)
    {
        var allowed = new HashSet<string>(allowedValues, StringComparer.Ordinal);
        var normalized = source
            .Where(value => !string.IsNullOrWhiteSpace(value) && allowed.Contains(value))
            .Distinct(StringComparer.Ordinal)
            .Take(maxCount)
            .ToList();

        source.Clear();
        source.AddRange(normalized);
    }

    private static void NormalizeLooseProgressList(List<string> source, int maxCount = 32)
    {
        var normalized = source
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Take(maxCount)
            .ToList();

        source.Clear();
        source.AddRange(normalized);
    }

    private bool IsIntegrityRewardsLocked()
    {
        return _integrityRewardsLocked;
    }

    private string IntegrityRewardLockSummary()
    {
        return string.IsNullOrWhiteSpace(_integrityStatusLine)
            ? "整合性違反検知により報酬と進行保存を凍結"
            : $"整合性違反検知により報酬と進行保存を凍結 ({_integrityStatusLine})";
    }

    private void ResetIntegrityRewardsLock()
    {
        _integrityRewardsLocked = false;
    }
}
