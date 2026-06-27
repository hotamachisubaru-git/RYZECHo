namespace System.Runtime.CompilerServices;

internal static class IsExternalInit
{
}

[System.AttributeUsage(System.AttributeTargets.All, Inherited = false)]
internal sealed class RequiredMemberAttribute : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = false)]
internal sealed class CompilerFeatureRequiredAttribute : System.Attribute
{
    public const string RefStructs = nameof(RefStructs);
    public const string RequiredMembers = nameof(RequiredMembers);

    public CompilerFeatureRequiredAttribute(string featureName)
    {
        FeatureName = featureName;
    }

    public string FeatureName { get; }
    public bool IsOptional { get; init; }
}

[System.AttributeUsage(System.AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
internal sealed class SetsRequiredMembersAttribute : System.Attribute
{
}
