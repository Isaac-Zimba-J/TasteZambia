namespace TasteZambia.Shared.Routes;

/// <summary>
/// Every API path, in one place. The mobile client builds requests from these
/// constants so a route rename is a compile error rather than a 404 at runtime.
/// </summary>
public static class ApiRoutes
{
    public const string Base = "/api";
    public const string Version = "v1";
    public const string Root = $"{Base}/{Version}";

    public static class Dishes
    {
        public const string Collection = $"{Root}/dishes";
        public const string ById = $"{Collection}/{{id}}";
        public const string Recipe = $"{Collection}/{{id}}/recipe";
        public const string Search = $"{Collection}/search";
    }

    public static class Ingredients
    {
        public const string Collection = $"{Root}/ingredients";
        public const string ByKey = $"{Collection}/{{key}}";
    }

    public static class Regions
    {
        public const string Collection = $"{Root}/regions";
        public const string ByName = $"{Collection}/{{name}}";
    }

    public static class Culture
    {
        public const string Articles = $"{Root}/articles";
        public const string ArticleById = $"{Articles}/{{id}}";
    }

    public static class Categories
    {
        public const string Collection = $"{Root}/categories";
    }

    public static class Auth
    {
        /// <summary>Register-or-sign-in for a device. Idempotent for a given (id, secret) pair.</summary>
        public const string Device = $"{Root}/auth/device";
        public const string Refresh = $"{Root}/auth/refresh";
    }

    /// <summary>The signed-in account's own data. Every route needs a bearer token.</summary>
    public static class Me
    {
        public const string Profile = $"{Root}/me";
        public const string Onboarding = $"{Root}/me/onboarding";
        public const string Saved = $"{Root}/me/saved";
        public const string Progress = $"{Root}/me/progress/{{dishId}}";
        public const string Sync = $"{Root}/me/sync";

        public const string Contributions = $"{Root}/me/contributions";
        public const string ContributionById = $"{Contributions}/{{id}}";
        public const string Resubmit = $"{Contributions}/{{id}}/resubmit";
        public const string Withdraw = $"{Contributions}/{{id}}/withdraw";
    }

    /// <summary>Reviewer-only. Needs the "reviewer" role.</summary>
    public static class Review
    {
        public const string Queue = $"{Root}/review/queue";
        public const string RequestChanges = $"{Root}/review/{{id}}/request-changes";
        public const string Publish = $"{Root}/review/{{id}}/publish";
    }

    /// <summary>Admin-only. Needs the "admin" role.</summary>
    public static class Admin
    {
        public const string UserRoles = $"{Root}/admin/users/{{userName}}/roles";
    }

    public static class Sync
    {
        /// <summary>Delta endpoint: pass ?since={cursor} to receive only what changed.</summary>
        public const string Changes = $"{Root}/sync/changes";
    }

    public static class Media
    {
        public const string Collection = $"{Root}/media";
        public const string ById = $"{Collection}/{{id}}";
    }

    /// <summary>The private family tier. Every route needs a bearer token.</summary>
    public static class Family
    {
        public const string Collection = $"{Root}/me/family-recipes";
        public const string ById = $"{Collection}/{{id}}";
        public const string Members = $"{ById}/members";
        public const string MemberById = $"{Members}/{{memberId}}";
        public const string Notes = $"{ById}/notes";
        public const string Privacy = $"{ById}/privacy";
        public const string Media = $"{ById}/media/{{mediaId}}";

        /// <summary>Redeeming an invite code is not scoped to a recipe the caller cannot see yet.</summary>
        public const string Accept = $"{Root}/family-recipes/accept";
    }
}
