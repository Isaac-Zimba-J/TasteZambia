using Microsoft.Extensions.Logging;
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Mobile.Services;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Newsreader and Archivo ship from Google Fonts as variable fonts only.
                // These are static instances cut with fontTools; see the fonts note in
                // Docs/plans. Aliases here must match FontFamily in Styles.xaml exactly.
                fonts.AddFont("Newsreader-Regular.ttf",  "NewsreaderRegular");
                fonts.AddFont("Newsreader-Medium.ttf",   "NewsreaderMedium");
                fonts.AddFont("Newsreader-SemiBold.ttf", "NewsreaderSemiBold");
                fonts.AddFont("Newsreader-Italic.ttf",   "NewsreaderItalic");
                fonts.AddFont("Archivo-Regular.ttf",     "ArchivoRegular");
                fonts.AddFont("Archivo-Medium.ttf",      "ArchivoMedium");
                fonts.AddFont("Archivo-SemiBold.ttf",    "ArchivoSemiBold");
                fonts.AddFont("Archivo-Bold.ttf",        "ArchivoBold");
                fonts.AddFont("IBMPlexMono-Regular.ttf", "PlexMonoRegular");
                fonts.AddFont("IBMPlexMono-Medium.ttf",  "PlexMonoMedium");
            });

        // ---- Repositories: the ONLY layer that changes when the API lands. ----
        builder.Services.AddSingleton<IDishRepository, InMemoryDishRepository>();
        builder.Services.AddSingleton<IIngredientRepository, InMemoryIngredientRepository>();
        builder.Services.AddSingleton<IRegionRepository, InMemoryRegionRepository>();
        builder.Services.AddSingleton<IArticleRepository, InMemoryArticleRepository>();
        builder.Services.AddSingleton<ICategoryRepository, InMemoryCategoryRepository>();
        builder.Services.AddSingleton<IProfileRepository, InMemoryProfileRepository>();

        // ---- Services: unchanged by the API swap. ----
        // Favourites, progress and preferences are singletons on purpose: a heart
        // toggled on Home must stay toggled on Explore and on the recipe screen.
        builder.Services.AddSingleton<ICatalogService, CatalogService>();
        builder.Services.AddSingleton<IFavouritesService, FavouritesService>();
        builder.Services.AddSingleton<ICookingProgressService, CookingProgressService>();
        builder.Services.AddSingleton<IPreferenceService, PreferenceService>();
        builder.Services.AddSingleton<IContributionService, ContributionService>();
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();

        // ---- Shell and pages ----
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ExplorePage>();
        builder.Services.AddTransient<RegionsPage>();
        builder.Services.AddTransient<CulturePage>();
        builder.Services.AddTransient<ProfilePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
