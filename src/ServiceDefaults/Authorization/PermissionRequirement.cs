
namespace ServiceDefaults.Authorization;
using Microsoft.AspNetCore.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;