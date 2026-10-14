namespace SocialNetworkPlatformProject.Application.Common;

// The roles of the site. Everybody is a plain user; an admin has the "Admin" role, which opens the admin panel
// (api/admin/*). Who becomes an admin is set in appsettings.json: "Admin": { "Emails": [ ... ] } (see SessionIssuer).
public static class AppRoles
{
    public const string Admin = "Admin";
}
