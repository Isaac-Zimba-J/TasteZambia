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

    public static class Sync
    {
        /// <summary>Delta endpoint: pass ?since={cursor} to receive only what changed.</summary>
        public const string Changes = $"{Root}/sync/changes";
    }
}
