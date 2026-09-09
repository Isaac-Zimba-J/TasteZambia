using FluentIcons.Maui;
using Microsoft.Extensions.Logging;
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Mobile.Services;
using TasteZambia.Mobile.Views;
using TasteZambia.Mobile.Views.Sections;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseFluentIcons()
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
        // One instance, resolved as both the concrete type (App attaches the host
        // to it) and the interface the ViewModels depend on.
        builder.Services.AddSingleton<AppNavigationService>();
        builder.Services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<AppNavigationService>());

        // ---- Host page and sections ----
        // Sections are singletons: the host keeps them alive so switching back to a
        // tab restores it as the user left it, rather than rebuilding it.
        builder.Services.AddSingleton<MainShellPage>();
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<HomeView>();
        builder.Services.AddSingleton<ExploreViewModel>();
        builder.Services.AddSingleton<ExploreView>();
        builder.Services.AddSingleton<RegionsViewModel>();
        builder.Services.AddSingleton<RegionsView>();
        builder.Services.AddSingleton<CultureViewModel>();
        builder.Services.AddSingleton<CultureView>();
        builder.Services.AddSingleton<ProfileViewModel>();
        builder.Services.AddSingleton<ProfileView>();

        // Detail screens are transient: they carry a route parameter and must
        // rebuild for whichever dish or ingredient was opened.
        builder.Services.AddTransient<RecipeViewModel>();
        builder.Services.AddTransient<RecipeView>();
        builder.Services.AddTransient<IngredientsViewModel>();
        builder.Services.AddTransient<IngredientsView>();
        builder.Services.AddTransient<IngredientViewModel>();
        builder.Services.AddTransient<IngredientView>();
        builder.Services.AddTransient<StoryViewModel>();
        builder.Services.AddTransient<StoryView>();
        builder.Services.AddTransient<ShareViewModel>();
        builder.Services.AddTransient<ShareView>();
        builder.Services.AddTransient<FamilyViewModel>();
        builder.Services.AddTransient<FamilyView>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
