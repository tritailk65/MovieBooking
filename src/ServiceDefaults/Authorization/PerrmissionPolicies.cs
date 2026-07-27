namespace ServiceDefaults.Authorization;

public static class PermissionPolicies
{
    public static string Require(string permission)
        => $"{PermissionPolicyProvider.Prefix}{permission}";
}