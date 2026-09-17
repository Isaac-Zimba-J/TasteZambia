using FluentIcons.Maui;
using Microsoft.Extensions.Logging;
using TasteZambia.Core.Data;
using TasteZambia.Core.Data.Http;
using TasteZambia.Core.Services;
using TasteZambia.Mobile.Services;
using TasteZambia.Mobile.Views;
using TasteZambia.Mobile.Views.Tabs;
using TasteZambia.Mobile.Views.Details;
using TasteZambia.Mobile.Views.Onboarding;
using TasteZambia.Mobile.Views.Share;
using TasteZambia.Mobile.Views.Family;
using TasteZambia.Mobile.Views.Collections;
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

        // ---- Repositories: the ONLY layer that changed when the API landed. ----
        // The five read-side interfaces now go over HTTP to the archive API. Nothing
        // above this layer knows; RepositoryParityTests in the API suite prove the
        // HTTP and seeded implementations return equal models.
        static void Api(HttpClient c) => c.BaseAddress = new Uri(ArchiveApiOptions.BaseUrl);

        // ---- Identity: anonymous, device-bound. No sign-in screen. ----
        // The "auth" client carries NO AuthenticatedHandler: the handler signs in
        // through this client, and giving it the handler too would recurse.
        builder.Services.AddSingleton<ISecureStore, SecureStore>();
        builder.Services.AddSingleton<IDeviceIdentity, SecureDeviceIdentity>();
        builder.Services.AddHttpClient("auth", Api);
        builder.Services.AddSingleton<IAuthSession>(sp => new AuthSession(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("auth"),
            sp.GetRequiredService<IDeviceIdentity>(),
            sp.GetRequiredService<ISecureStore>()));
        builder.Services.AddTransient<AuthenticatedHandler>();

        // Personal data is written locally first and reconciled to the account.
        builder.Services.AddSingleton<ILocalStore, PreferencesLocalStore>();
        builder.Services.AddHttpClient("me", Api).AddHttpMessageHandler<AuthenticatedHandler>();

        // The archive endpoints are anonymous today; the handler rides along so that
        // the day any of them needs a token, nothing on the app side changes.
        builder.Services.AddHttpClient<IDishRepository, HttpDishRepository>(Api).AddHttpMessageHandler<AuthenticatedHandler>();
        builder.Services.AddHttpClient<IIngredientRepository, HttpIngredientRepository>(Api).AddHttpMessageHandler<AuthenticatedHandler>();
        builder.Services.AddHttpClient<IRegionRepository, HttpRegionRepository>(Api).AddHttpMessageHandler<AuthenticatedHandler>();
        builder.Services.AddHttpClient<IArticleRepository, HttpArticleRepository>(Api).AddHttpMessageHandler<AuthenticatedHandler>();
        builder.Services.AddHttpClient<ICategoryRepository, HttpCategoryRepository>(Api).AddHttpMessageHandler<AuthenticatedHandler>();

        // Profile reads the account; its counts come from the local personal store.
        builder.Services.AddHttpClient<IProfileRepository, HttpProfileRepository>(Api).AddHttpMessageHandler<AuthenticatedHandler>();

        // ---- Services: unchanged by the API swap. ----
        // Favourites, progress and preferences are singletons on purpose: a heart
        // toggled on Home must stay toggled on Explore and on the recipe screen.
        builder.Services.AddSingleton<IOnboardingService>(sp => new OnboardingService(
            sp.GetRequiredService<ILocalStore>(),
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("me")));
        builder.Services.AddSingleton<ICatalogService, CatalogService>();
        builder.Services.AddSingleton<PersonalStore>(sp => new PersonalStore(sp.GetRequiredService<ILocalStore>(), TimeProvider.System));
        builder.Services.AddSingleton<IFavouritesService, FavouritesService>();
        builder.Services.AddSingleton<ICookingProgressService, CookingProgressService>();
        builder.Services.AddSingleton<IPersonalSyncService>(sp => new PersonalSyncService(
            sp.GetRequiredService<PersonalStore>(),
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("me")));
        builder.Services.AddSingleton<SyncScheduler>();
        builder.Services.AddSingleton<IPreferenceService, PreferenceService>();
        builder.Services.AddSingleton<IContributionService, ContributionService>();
        // Singleton: a privacy change on famPublic must be visible everywhere.
        builder.Services.AddSingleton<IFamilyArchiveService, FamilyArchiveService>();
        builder.Services.AddSingleton<ICollectionsService, CollectionsService>();
        // One instance, resolved as both the concrete type (App attaches the host
        // to it) and the interface the ViewModels depend on.
        builder.Services.AddSingleton<AppNavigationService>();
        builder.Services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<AppNavigationService>());

        // ---- Onboarding: its own host, no nav bar, runs before the app proper.
        // One ViewModel shared by all seven so choices survive the walk. ----
        builder.Services.AddSingleton<OnboardingViewModel>();
        builder.Services.AddSingleton<OnboardingPage>();
        builder.Services.AddSingleton<SplashView>();
        builder.Services.AddSingleton<IntroView>();
        builder.Services.AddSingleton<OnbLanguageView>();
        builder.Services.AddSingleton<OnbWhoView>();
        builder.Services.AddSingleton<OnbTasteView>();
        builder.Services.AddSingleton<OnbNotifyView>();
        builder.Services.AddSingleton<OnbReadyView>();

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
        builder.Services.AddTransient<ShareStartViewModel>();
        builder.Services.AddTransient<ShareStartView>();
        builder.Services.AddTransient<DraftsViewModel>();
        builder.Services.AddTransient<ShareDraftsView>();
        builder.Services.AddTransient<ShareReviewViewModel>();
        builder.Services.AddTransient<ShareReviewView>();
        builder.Services.AddTransient<ShareChangesViewModel>();
        builder.Services.AddTransient<ShareChangesView>();
        builder.Services.AddTransient<SharePublishedViewModel>();
        builder.Services.AddTransient<SharePublishedView>();
        builder.Services.AddTransient<FamStartViewModel>();
        builder.Services.AddTransient<FamStartView>();
        builder.Services.AddTransient<FamDraftViewModel>();
        builder.Services.AddTransient<FamDraftView>();
        builder.Services.AddTransient<FamSavedViewModel>();
        builder.Services.AddTransient<FamSavedView>();
        builder.Services.AddTransient<FamSharedViewModel>();
        builder.Services.AddTransient<FamSharedView>();
        builder.Services.AddTransient<FamPublicViewModel>();
        builder.Services.AddTransient<FamPublicView>();
        builder.Services.AddTransient<FavouritesViewModel>();
        builder.Services.AddTransient<FavouritesView>();
        builder.Services.AddTransient<WantToTryViewModel>();
        builder.Services.AddTransient<WantToTryView>();
        builder.Services.AddTransient<CookedViewModel>();
        builder.Services.AddTransient<CookedView>();
        builder.Services.AddTransient<FamilyRecipesViewModel>();
        builder.Services.AddTransient<FamilyRecipesView>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsView>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
