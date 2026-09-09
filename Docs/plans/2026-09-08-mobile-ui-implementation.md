# Taste Zambia Mobile UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build all eleven screens of the Taste Zambia mobile app in .NET MAUI, pixel-faithful to the approved design canvas, running on seeded in-memory data behind repository interfaces that a REST backend can later replace without touching any View or ViewModel.

**Architecture:** Three layers with one-way dependencies. `TasteZambia.Core` (plain `net10.0` class library) holds Models, Repositories, Services and ViewModels — no MAUI types, so it is fully unit-testable and reusable by the future API. `TasteZambia.Mobile` holds Views, Controls, Converters and the MAUI `INavigationService` implementation. Repositories do data access only; Services own domain logic (search, favourites, cook progress, title-language, submissions); ViewModels own presentation state only. Swapping seed data for the live API is a registration change in `MauiProgram.cs` and nothing else.

**Tech Stack:** .NET 10 (SDK 10.0.201), .NET MAUI 10, XAML + MVVM, CommunityToolkit.Mvvm 8.4.0, xUnit for tests, Shell for routing with a custom-drawn bottom navigation bar.

**Spec:** `Docs/Mobile app design project/Taste Zambia.dc.html` (open it in a browser with `support.js` alongside to interact with all eleven frames). Design assets: `Docs/Mobile app design project/assets/`.

---

## Global Constraints

Every task's requirements implicitly include this section. Values are copied verbatim from the spec.

### Frame geometry
- Design frame is **390 x 812** points. All sizes below are design points; use them as MAUI device-independent units 1:1.
- Bottom navigation bar is **66pt** tall and present on **every** screen, including detail screens. Scrollable content is inset by 66pt at the bottom.
- Standard horizontal page padding is **20pt**. The Story screen uses **24pt**.
- Every scrollable screen ends with a **26pt** spacer before the nav bar.

### Colour tokens (exact)
| Token | Hex | Use |
|---|---|---|
| `TzGreenDeep` | `#17402F` | Dark panels, active chips, primary buttons on cream |
| `TzGreenMid` | `#2F6A4D` | Accent green, links, completed-step chrome, meta text |
| `TzGreenTint` | `#EEF2EC` | Time pill background, completed step background |
| `TzGold` | `#8A5A12` | **Actions only** — primary CTA fills, gold text, gold kickers |
| `TzGoldPressed` | `#6F4A10` | Gold pressed |
| `TzGoldDecor` | `#C07F1E` | Decoration ONLY — 7px progress dots, 3px track fills, palette swatch |
| `TzGoldLight` | `#E8BD77` | Gold on dark green surfaces |
| `TzGoldTint` | `#F7EEDA` | Difficulty pill / "In review" badge background |
| `TzGoldTintText` | `#7A5A10` | Text on `TzGoldTint` |
| `TzClay` | `#A3452A` | Kickers, saved-heart fill, regional-variation headings |
| `TzCream` | `#F7F1E6` | Inset panel background |
| `TzSurface` | `#FFFDF9` | Page and card background |
| `TzInk` | `#221A12` | Primary text |
| `TzInkSoft` | `#2C2318` | Story lede text |
| `TzStoryBody` | `#3D3225` | Story body paragraphs |
| `TzBody` | `#4A3D2E` | Body text |
| `TzBodyMuted` | `#5F5241` | Secondary body text |
| `TzMuted` | `#7A6B59` | Captions, English subtitles |
| `TzMuted2` | `#6B5C4A` | Meta, mono labels |
| `TzFaint` | `#7A6B59` | Alias of `TzMuted` since v2 |
| `TzChevron` | `#7A6B59` | Alias of `TzMuted` since v2 |
| `TzTimelineIdle` | `#D8CDB9` | Not-yet-reached timeline dot |
| `TzClayText` | `#8F3B23` | "Changes requested" badge text |
| `TzStripeA` | `#DED4C2` | Missing-photo stripe, colour A |
| `TzStripeB` | `#E9E1D3` | Missing-photo stripe, colour B |
| `TzDraftBg` | `#F0ECE4` | "Draft" status badge background |

Alpha borders over ink `#221A12`, written as `#AARRGGBB`:
`TzHairline` `#17221A12` (.09) · `TzHairlineSoft` `#12221A12` (.07) · `TzRule` `#1A221A12` (.10) · `TzBorder` `#24221A12` (.14) · `TzBorderStrong` `#29221A12` (.16) · `TzBorderDashed` `#33221A12` (.20) · `TzHandle` `#29221A12` (.16)

Light text over dark green `#FFFDF9`:
`TzOnDark86` `#DBFFFDF9` · `TzOnDark84` `#D6FFFDF9` · `TzOnDark82` `#D1FFFDF9` · `TzOnDark66` `#A8FFFDF9` · `TzOnDark60` `#99FFFDF9` · `TzOnDark50` `#80FFFDF9` · `TzOnDark14` `#24FFFDF9` · `TzOnDark10` `#1AFFFDF9`

Scrim over content: `#800E2018` (rgba(14,32,24,.5)).

### Typography
Three families, registered from static instances (not variable fonts):
- **Newsreader** (serif) — every title, dish name, ingredient name, province name. Aliases: `NewsreaderRegular`, `NewsreaderMedium`, `NewsreaderSemiBold`, `NewsreaderItalic`.
- **Archivo** (sans) — all UI text and body copy. Aliases: `ArchivoRegular`, `ArchivoMedium`, `ArchivoSemiBold`, `ArchivoBold`.
- **IBM Plex Mono** — kickers, uppercase micro-labels, quantities, timestamps. Aliases: `PlexMonoRegular`, `PlexMonoMedium`.

Letter-spacing in the spec is in `em`; MAUI `CharacterSpacing` is in device-independent units. Convert with `size * em`. The recurring kicker (`9px`, `.16em`) is `CharacterSpacing="1.44"`.

### Content rules (non-negotiable — this is a cultural archive)
- **The local name is always the title; English is always the explanation underneath.** Never invert this without the `titleLanguage` preference being set to `English as title`.
- Kickers are uppercase IBM Plex Mono, never sentence case.
- Gold (`TzGold`, `#8A5A12`) appears **only** on actions and gold text. `TzGoldDecor` (`#C07F1E`) is for decoration only — progress dots and track fills. Never use `TzGold` as decoration, and never use `TzGoldDecor` for text or a CTA fill (it fails contrast on cream).
- Missing photography is shown as a **45°-striped placeholder** with an italic mono caption naming the photo needed — never a grey box, never a stock image.

### Screen hosting — REVISED during execution

The plan originally gave every screen its own `ContentPage` carrying its own
`BottomNavBar`, routed by Shell. **That was wrong and has been replaced.**

Because each page built its own copy of the bar, every tab change destroyed one bar
and constructed another, so the bar visibly repainted on each switch. Shell's Android
fragment transactions compounded it. Neither `animate: false` nor selecting
`Shell.CurrentItem` fixed it — the bar was inside the thing being swapped.

**The app is now a single host page.** `Views/MainShellPage.xaml` owns a
`Grid RowDefinitions="*,Auto"`: row 0 is a content region, row 1 is one
`BottomNavBar` created once and never rebuilt. Shell is removed entirely.

Consequences for every screen task below:

- Screens are **`ContentView`s under `Views/Sections/`**, not `ContentPage`s.
- A screen does **not** include a `BottomNavBar`; the host owns it. Ignore every
  `<controls:BottomNavBar .../>` line in the tasks that follow.
- A screen's root is the content itself — no outer `Grid RowDefinitions="*,Auto"`.
- Sections are registered **singleton** so returning to a tab restores its state.
- Navigation still goes through `INavigationService`; `AppNavigationService` drives
  the host instead of Shell. Routes are unchanged (`//home`, `recipe`, …), so no
  ViewModel changes. New routes are added to `MainShellPage.Routes`, which also
  maps each detail route to the nav section that stays lit.
- Detail screens push onto the host's own view stack, and Android's back button
  pops it via `OnBackButtonPressed`.
- This matches the design canvas, which is one frame with fixed chrome and
  changing content.

### .NET MAUI 10 API rules
- `Border`, never `Frame`. `CollectionView`, never `ListView`/`TableView`.
- Animation methods are `*Async`: `TranslateToAsync`, `FadeToAsync`, `ScaleToAsync`.
- `DisplayAlertAsync` / `DisplayActionSheetAsync`, never the sync-named forms.
- `Color.FromArgb`, never `Color.FromHex`. `DeviceInfo.Platform`, never `Device.RuntimePlatform`.
- Constructor injection via DI. Never `DependencyService`, never `MessagingCenter`.
- **Never nest a `CollectionView` inside a `ScrollView`.** Detail screens are `ScrollView` + `VerticalStackLayout`; their inner fixed-length lists use `BindableLayout`. Only top-level scrolling lists (Explore results, Ingredients grid) are `CollectionView`.

### Known gaps carried from the design (do not invent content for these)
1. The **province map** on the Regions screen is a labelled placeholder. It awaits real provincial boundary data and must ship as the placeholder.
2. Photography exists for **five** subjects only: ifisashi, nshima, chikanda, market ingredients, avatar. Kapenta, inkoko, kandolo, munkoyo, delele and every story image use the striped placeholder with its caption.
3. In the design, detail routes resolve to **Ifisashi** (recipe) and **Chibwabwa** (ingredient) only. The implementation must route by id properly, but only those two have full detail content seeded.

---

## File Structure

### New project: `TasteZambia.Core` (`net10.0` class library)
| File | Responsibility |
|---|---|
| `Models/Dish.cs` | Dish record + `RecipeDetail` |
| `Models/Ingredient.cs` | Ingredient, `LocalName`, `RecipeIngredient` |
| `Models/CookingStep.cs` | Step + `RegionalVariation` + `PreparationMethod` enum |
| `Models/Province.cs` | Province record |
| `Models/Article.cs` | Article + `ArticleBody` blocks |
| `Models/Category.cs` | Food category |
| `Models/Profile.cs` | UserProfile, `Collection`, `Contribution`, `ContributionStatus` |
| `Models/ContributionDraft.cs` | Wizard draft + `PrivacyLevel` + `ReviewStage` |
| `Data/IDishRepository.cs` … | One interface per aggregate |
| `Data/SeedData.cs` | All design content, verbatim |
| `Data/InMemoryDishRepository.cs` … | Seeded implementations |
| `Services/ICatalogService.cs` + impl | Search and filtering |
| `Services/IFavouritesService.cs` + impl | Saved dishes |
| `Services/ICookingProgressService.cs` + impl | Step check-off |
| `Services/IPreferenceService.cs` + impl | Title language, verification badge |
| `Services/IContributionService.cs` + impl | Share + family submissions |
| `Services/INavigationService.cs` | Navigation abstraction (implemented in Mobile) |
| `ViewModels/BaseViewModel.cs` | Shared observable base |
| `ViewModels/*ViewModel.cs` | One per screen + item VMs |

### `TasteZambia.Mobile` (existing)
| File | Responsibility |
|---|---|
| `Resources/Styles/Colors.xaml` | Design tokens (replaces template colours) |
| `Resources/Styles/Styles.xaml` | Typography + control styles |
| `Resources/Fonts/*.ttf` | Newsreader, Archivo, IBM Plex Mono |
| `Resources/Images/*.png` | Design assets |
| `Controls/StripePlaceholder.cs` | Drawn 45° stripe fill + caption |
| `Controls/BottomNavBar.xaml` | Custom 5-item nav bar |
| `Controls/DishCard.xaml` | 236pt carousel card |
| `Controls/DishRow.xaml` | 96pt search-result row |
| `Controls/SectionHeader.xaml` | Kicker + serif title + "View all" |
| `Controls/WizardProgress.xaml` | Step label + gold progress track |
| `Controls/IngredientSheet.xaml` | Bottom sheet overlay |
| `Views/*.xaml` | Eleven screens |
| `Converters/` | Bool→colour, count→label helpers |
| `Services/ShellNavigationService.cs` | `INavigationService` implementation |
| `AppShell.xaml` | Routes only; native tab bar hidden |
| `MauiProgram.cs` | All DI registration |

### New project: `TasteZambia.Core.Tests` (`net10.0`, xUnit)
Mirrors `Services/` and `ViewModels/` with one test file per unit.

---

## Task 1: Solution scaffolding, design tokens and fonts

**Files:**
- Create: `TasteZambia.Core/TasteZambia.Core.csproj`
- Create: `TasteZambia.Core.Tests/TasteZambia.Core.Tests.csproj`
- Create: `TasteZambia.Mobile/Resources/Fonts/` (10 `.ttf` files)
- Modify: `TasteZambia.Mobile/Resources/Styles/Colors.xaml` (replace template colours)
- Modify: `TasteZambia.Mobile/Resources/Styles/Styles.xaml` (replace template styles)
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Modify: `TasteZambia.Mobile/TasteZambia.Mobile.csproj`
- Modify: `TasteZambia.sln`

**Interfaces:**
- Consumes: nothing.
- Produces: `StaticResource` keys `TzGreenDeep`, `TzGold`, … (full table in Global Constraints); text styles `KickerLabel`, `DisplayTitle`, `SectionTitle`, `CardTitle`, `BodyText`, `BodySmall`, `MetaText`, `MonoMeta`; font aliases `NewsreaderRegular`/`NewsreaderMedium`/`NewsreaderSemiBold`/`NewsreaderItalic`, `ArchivoRegular`/`ArchivoMedium`/`ArchivoSemiBold`/`ArchivoBold`, `PlexMonoRegular`/`PlexMonoMedium`.

- [ ] **Step 1: Create the Core and test projects and wire the solution**

```bash
cd "/Users/zimbadev/Documents/Workspace/Maui Projects/TasteZambia"
dotnet new classlib -n TasteZambia.Core -f net10.0
dotnet new xunit   -n TasteZambia.Core.Tests -f net10.0
rm TasteZambia.Core/Class1.cs TasteZambia.Core.Tests/UnitTest1.cs
dotnet add TasteZambia.Core/TasteZambia.Core.csproj package CommunityToolkit.Mvvm --version 8.4.0
dotnet add TasteZambia.Core.Tests/TasteZambia.Core.Tests.csproj reference TasteZambia.Core/TasteZambia.Core.csproj
dotnet add TasteZambia.Mobile/TasteZambia.Mobile.csproj reference TasteZambia.Core/TasteZambia.Core.csproj
dotnet sln add TasteZambia.Core/TasteZambia.Core.csproj TasteZambia.Core.Tests/TasteZambia.Core.Tests.csproj
```

Then enable nullable + implicit usings in `TasteZambia.Core.csproj`:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

- [ ] **Step 2: Verify the solution still builds**

Run: `dotnet build TasteZambia.Core/TasteZambia.Core.csproj && dotnet test TasteZambia.Core.Tests/TasteZambia.Core.Tests.csproj`
Expected: build succeeds; test run reports 0 tests, exit code 0.

- [ ] **Step 3: Download the three font families as static instances**

```bash
cd "/Users/zimbadev/Documents/Workspace/Maui Projects/TasteZambia/TasteZambia.Mobile/Resources/Fonts"
BASE=https://raw.githubusercontent.com/google/fonts/main
curl -fL -o Newsreader-Regular.ttf   "$BASE/ofl/newsreader/static/Newsreader-Regular.ttf"
curl -fL -o Newsreader-Medium.ttf    "$BASE/ofl/newsreader/static/Newsreader-Medium.ttf"
curl -fL -o Newsreader-SemiBold.ttf  "$BASE/ofl/newsreader/static/Newsreader-SemiBold.ttf"
curl -fL -o Newsreader-Italic.ttf    "$BASE/ofl/newsreader/static/Newsreader-Italic.ttf"
curl -fL -o Archivo-Regular.ttf      "$BASE/ofl/archivo/static/Archivo-Regular.ttf"
curl -fL -o Archivo-Medium.ttf       "$BASE/ofl/archivo/static/Archivo-Medium.ttf"
curl -fL -o Archivo-SemiBold.ttf     "$BASE/ofl/archivo/static/Archivo-SemiBold.ttf"
curl -fL -o Archivo-Bold.ttf         "$BASE/ofl/archivo/static/Archivo-Bold.ttf"
curl -fL -o IBMPlexMono-Regular.ttf  "$BASE/ofl/ibmplexmono/IBMPlexMono-Regular.ttf"
curl -fL -o IBMPlexMono-Medium.ttf   "$BASE/ofl/ibmplexmono/IBMPlexMono-Medium.ttf"
file *.ttf
```

Expected: every file reports `TrueType Font data`. If any URL 404s, fetch the family zip from `https://fonts.google.com/download?family=Newsreader` (etc.) and extract the same static instance names. All three families are OFL-licensed — copy each family's `OFL.txt` into `Resources/Fonts/` alongside the fonts.

- [ ] **Step 4: Copy the design photography into the app**

```bash
cd "/Users/zimbadev/Documents/Workspace/Maui Projects/TasteZambia"
SRC="Docs/Mobile app design project/assets"
DST="TasteZambia.Mobile/Resources/Images"
cp "$SRC/spread-nshima.png"       "$DST/spread_nshima.png"
cp "$SRC/ifisashi.png"            "$DST/ifisashi.png"
cp "$SRC/chikanda.png"            "$DST/chikanda.png"
cp "$SRC/market-ingredients.png"  "$DST/market_ingredients.png"
cp "$SRC/avatar-chanda.png"       "$DST/avatar_chanda.png"
cp "$SRC/logo-pot.png"            "$DST/logo.png"
rm -f "$DST/dotnet_bot.png"
ls "$DST"
```

MAUI resource filenames must be lowercase with underscores — hyphens break the Android resource pipeline. Source images are ~530px wide; add resize hints in the `.csproj` in Step 5 so the hero images are not upscaled.

- [ ] **Step 5: Update the csproj — fonts, images, splash colour, remove the bot**

Replace the `<ItemGroup>` containing icon/splash/image entries in `TasteZambia.Mobile/TasteZambia.Mobile.csproj` with:

```xml
<ItemGroup>
    <MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#17402F"/>
    <MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#17402F" BaseSize="128,128"/>

    <MauiImage Include="Resources\Images\*"/>
    <MauiImage Update="Resources\Images\spread_nshima.png"      BaseSize="533,293"/>
    <MauiImage Update="Resources\Images\ifisashi.png"           BaseSize="531,393"/>
    <MauiImage Update="Resources\Images\chikanda.png"           BaseSize="536,396"/>
    <MauiImage Update="Resources\Images\market_ingredients.png" BaseSize="542,405"/>
    <MauiImage Update="Resources\Images\avatar_chanda.png"      BaseSize="529,529"/>
    <MauiImage Update="Resources\Images\logo.png"           BaseSize="531,519"/>

    <MauiFont Include="Resources\Fonts\*"/>
    <MauiAsset Include="Resources\Raw\**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)"/>
</ItemGroup>
```

Also set the app identity in the first `<PropertyGroup>`:

```xml
<ApplicationTitle>Taste Zambia</ApplicationTitle>
<ApplicationId>zm.tastezambia.mobile</ApplicationId>
```

The app icon still uses the .NET template vector. Replacing it needs `logo-pot` as an SVG, which we do not have — leave the template icon and record it in the Known Gaps section of the README rather than shipping a rasterised icon.

- [ ] **Step 6: Write `Colors.xaml` with the design tokens**

Replace the entire contents of `TasteZambia.Mobile/Resources/Styles/Colors.xaml`:

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">

    <!-- Greens -->
    <Color x:Key="TzGreenDeep">#17402F</Color>
    <Color x:Key="TzGreenMid">#2F6A4D</Color>
    <Color x:Key="TzGreenTint">#EEF2EC</Color>

    <!-- Gold: ACTIONS ONLY -->
    <Color x:Key="TzGold">#8A5A12</Color>
    <Color x:Key="TzGoldPressed">#6F4A10</Color>
    <Color x:Key="TzGoldDecor">#C07F1E</Color>
    <Color x:Key="TzGoldLight">#E8BD77</Color>
    <Color x:Key="TzGoldTint">#F7EEDA</Color>
    <Color x:Key="TzGoldTintText">#7A5A10</Color>

    <!-- Clay -->
    <Color x:Key="TzClay">#A3452A</Color>

    <!-- Surfaces -->
    <Color x:Key="TzCream">#F7F1E6</Color>
    <Color x:Key="TzSurface">#FFFDF9</Color>
    <Color x:Key="TzDraftBg">#F0ECE4</Color>
    <Color x:Key="TzStripeA">#DED4C2</Color>
    <Color x:Key="TzStripeB">#E9E1D3</Color>

    <!-- Ink ramp -->
    <Color x:Key="TzInk">#221A12</Color>
    <Color x:Key="TzInkSoft">#2C2318</Color>
    <Color x:Key="TzStoryBody">#3D3225</Color>
    <Color x:Key="TzBody">#4A3D2E</Color>
    <Color x:Key="TzBodyMuted">#5F5241</Color>
    <Color x:Key="TzMuted">#7A6B59</Color>
    <Color x:Key="TzMuted2">#6B5C4A</Color>
    <!-- v2 contrast pass: TzFaint and TzChevron both collapsed into TzMuted.
         Kept as aliases so existing styles need no edit. -->
    <Color x:Key="TzFaint">#7A6B59</Color>
    <Color x:Key="TzChevron">#7A6B59</Color>
    <Color x:Key="TzTimelineIdle">#D8CDB9</Color>
    <Color x:Key="TzClayText">#8F3B23</Color>

    <!-- Hairlines over ink -->
    <Color x:Key="TzHairlineSoft">#12221A12</Color>
    <Color x:Key="TzHairline">#17221A12</Color>
    <Color x:Key="TzRule">#1A221A12</Color>
    <Color x:Key="TzBorder">#24221A12</Color>
    <Color x:Key="TzBorderStrong">#29221A12</Color>
    <Color x:Key="TzBorderDashed">#33221A12</Color>

    <!-- Light over dark green -->
    <Color x:Key="TzOnDark86">#DBFFFDF9</Color>
    <Color x:Key="TzOnDark84">#D6FFFDF9</Color>
    <Color x:Key="TzOnDark82">#D1FFFDF9</Color>
    <Color x:Key="TzOnDark66">#A8FFFDF9</Color>
    <Color x:Key="TzOnDark60">#99FFFDF9</Color>
    <Color x:Key="TzOnDark50">#80FFFDF9</Color>
    <Color x:Key="TzOnDark14">#24FFFDF9</Color>
    <Color x:Key="TzOnDark10">#1AFFFDF9</Color>

    <!-- Scrim -->
    <Color x:Key="TzScrim">#800E2018</Color>

    <SolidColorBrush x:Key="TzGreenDeepBrush" Color="{StaticResource TzGreenDeep}"/>
    <SolidColorBrush x:Key="TzGoldBrush" Color="{StaticResource TzGold}"/>
    <SolidColorBrush x:Key="TzSurfaceBrush" Color="{StaticResource TzSurface}"/>
    <SolidColorBrush x:Key="TzCreamBrush" Color="{StaticResource TzCream}"/>
    <SolidColorBrush x:Key="TzHairlineBrush" Color="{StaticResource TzHairline}"/>
</ResourceDictionary>
```

- [ ] **Step 7: Write `Styles.xaml` with the typography scale**

Replace the entire contents of `TasteZambia.Mobile/Resources/Styles/Styles.xaml`:

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">

    <!-- Uppercase mono kicker: 9pt, .16em -> 1.44 -->
    <Style x:Key="KickerLabel" TargetType="Label">
        <Setter Property="FontFamily" Value="PlexMonoMedium"/>
        <Setter Property="FontSize" Value="9"/>
        <Setter Property="CharacterSpacing" Value="1.44"/>
        <Setter Property="TextTransform" Value="Uppercase"/>
        <Setter Property="TextColor" Value="{StaticResource TzClay}"/>
    </Style>

    <!-- Smaller uppercase mono label: 8.5pt, .14em -> 1.19 -->
    <Style x:Key="MicroLabel" TargetType="Label">
        <Setter Property="FontFamily" Value="PlexMonoMedium"/>
        <Setter Property="FontSize" Value="8.5"/>
        <Setter Property="CharacterSpacing" Value="1.19"/>
        <Setter Property="TextTransform" Value="Uppercase"/>
        <Setter Property="TextColor" Value="{StaticResource TzFaint}"/>
    </Style>

    <!-- Screen title: Newsreader Medium 31 -->
    <Style x:Key="DisplayTitle" TargetType="Label">
        <Setter Property="FontFamily" Value="NewsreaderMedium"/>
        <Setter Property="FontSize" Value="31"/>
        <Setter Property="LineHeight" Value="1.05"/>
        <Setter Property="TextColor" Value="{StaticResource TzInk}"/>
    </Style>

    <!-- Hero title over photography: Newsreader Medium 40 -->
    <Style x:Key="HeroTitle" TargetType="Label">
        <Setter Property="FontFamily" Value="NewsreaderMedium"/>
        <Setter Property="FontSize" Value="40"/>
        <Setter Property="LineHeight" Value="1"/>
        <Setter Property="TextColor" Value="{StaticResource TzSurface}"/>
    </Style>

    <!-- In-page section heading: Newsreader SemiBold 23 -->
    <Style x:Key="SectionTitle" TargetType="Label">
        <Setter Property="FontFamily" Value="NewsreaderSemiBold"/>
        <Setter Property="FontSize" Value="23"/>
        <Setter Property="TextColor" Value="{StaticResource TzInk}"/>
    </Style>

    <!-- Home/profile section heading: Newsreader SemiBold 21 -->
    <Style x:Key="SubsectionTitle" TargetType="Label">
        <Setter Property="FontFamily" Value="NewsreaderSemiBold"/>
        <Setter Property="FontSize" Value="21"/>
        <Setter Property="TextColor" Value="{StaticResource TzInk}"/>
    </Style>

    <!-- Dish / ingredient name on a card -->
    <Style x:Key="CardTitle" TargetType="Label">
        <Setter Property="FontFamily" Value="NewsreaderSemiBold"/>
        <Setter Property="FontSize" Value="18"/>
        <Setter Property="LineHeight" Value="1.15"/>
        <Setter Property="TextColor" Value="{StaticResource TzInk}"/>
    </Style>

    <!-- English explanation under a local name -->
    <Style x:Key="EnglishSubtitle" TargetType="Label">
        <Setter Property="FontFamily" Value="ArchivoRegular"/>
        <Setter Property="FontSize" Value="11"/>
        <Setter Property="LineHeight" Value="1.35"/>
        <Setter Property="TextColor" Value="{StaticResource TzMuted}"/>
    </Style>

    <Style x:Key="BodyText" TargetType="Label">
        <Setter Property="FontFamily" Value="ArchivoRegular"/>
        <Setter Property="FontSize" Value="13"/>
        <Setter Property="LineHeight" Value="1.65"/>
        <Setter Property="TextColor" Value="{StaticResource TzBody}"/>
    </Style>

    <Style x:Key="BodySmall" TargetType="Label">
        <Setter Property="FontFamily" Value="ArchivoRegular"/>
        <Setter Property="FontSize" Value="12.5"/>
        <Setter Property="LineHeight" Value="1.6"/>
        <Setter Property="TextColor" Value="{StaticResource TzBodyMuted}"/>
    </Style>

    <Style x:Key="MetaText" TargetType="Label">
        <Setter Property="FontFamily" Value="ArchivoRegular"/>
        <Setter Property="FontSize" Value="11"/>
        <Setter Property="LineHeight" Value="1.4"/>
        <Setter Property="TextColor" Value="{StaticResource TzMuted2}"/>
    </Style>

    <Style x:Key="MonoMeta" TargetType="Label">
        <Setter Property="FontFamily" Value="PlexMonoRegular"/>
        <Setter Property="FontSize" Value="11"/>
        <Setter Property="TextColor" Value="{StaticResource TzMuted2}"/>
    </Style>

    <!-- Form field caption -->
    <Style x:Key="FieldLabel" TargetType="Label">
        <Setter Property="FontFamily" Value="ArchivoSemiBold"/>
        <Setter Property="FontSize" Value="11"/>
        <Setter Property="TextColor" Value="{StaticResource TzBody}"/>
    </Style>

    <!-- Primary gold action -->
    <Style x:Key="PrimaryAction" TargetType="Border">
        <Setter Property="BackgroundColor" Value="{StaticResource TzGold}"/>
        <Setter Property="StrokeThickness" Value="0"/>
        <Setter Property="Padding" Value="15"/>
        <Setter Property="StrokeShape" Value="RoundRectangle 14"/>
    </Style>

    <!-- Outline action -->
    <Style x:Key="SecondaryAction" TargetType="Border">
        <Setter Property="BackgroundColor" Value="Transparent"/>
        <Setter Property="Stroke" Value="{StaticResource TzBorderStrong}"/>
        <Setter Property="StrokeThickness" Value="1"/>
        <Setter Property="Padding" Value="15,15"/>
        <Setter Property="StrokeShape" Value="RoundRectangle 14"/>
    </Style>

    <!-- Standard card -->
    <Style x:Key="Card" TargetType="Border">
        <Setter Property="BackgroundColor" Value="{StaticResource TzSurface}"/>
        <Setter Property="Stroke" Value="{StaticResource TzHairline}"/>
        <Setter Property="StrokeThickness" Value="1"/>
        <Setter Property="StrokeShape" Value="RoundRectangle 20"/>
    </Style>

    <!-- Cream inset panel -->
    <Style x:Key="CreamPanel" TargetType="Border">
        <Setter Property="BackgroundColor" Value="{StaticResource TzCream}"/>
        <Setter Property="StrokeThickness" Value="0"/>
        <Setter Property="Padding" Value="20"/>
        <Setter Property="StrokeShape" Value="RoundRectangle 20"/>
    </Style>

    <!-- Dark green panel -->
    <Style x:Key="DeepPanel" TargetType="Border">
        <Setter Property="BackgroundColor" Value="{StaticResource TzGreenDeep}"/>
        <Setter Property="StrokeThickness" Value="0"/>
        <Setter Property="Padding" Value="20"/>
        <Setter Property="StrokeShape" Value="RoundRectangle 22"/>
    </Style>

    <!-- Every page: no Shell chrome, cream-white ground -->
    <Style TargetType="ContentPage" ApplyToDerivedTypes="True">
        <Setter Property="BackgroundColor" Value="{StaticResource TzSurface}"/>
        <Setter Property="Shell.NavBarIsVisible" Value="False"/>
        <Setter Property="Shell.TabBarIsVisible" Value="False"/>
    </Style>
</ResourceDictionary>
```

- [ ] **Step 8: Register the fonts in `MauiProgram.cs`**

```csharp
using Microsoft.Extensions.Logging;

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

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

- [ ] **Step 9: Prove the tokens and fonts render**

Replace `MainPage.xaml`'s content with a temporary probe:

```xml
<ScrollView>
    <VerticalStackLayout Padding="20" Spacing="12" BackgroundColor="{StaticResource TzSurface}">
        <Label Style="{StaticResource KickerLabel}" Text="Living cultural archive"/>
        <Label Style="{StaticResource DisplayTitle}" Text="Discover the Taste of Zambia"/>
        <Label Style="{StaticResource SectionTitle}" Text="Food Categories"/>
        <Label Style="{StaticResource BodyText}" Text="Leafy greens simmered in pounded groundnuts until the sauce thickens."/>
        <Label Style="{StaticResource MonoMeta}" Text="45 min  ·  Easy"/>
        <Border Style="{StaticResource PrimaryAction}">
            <Label Text="Explore Zambian Food" TextColor="{StaticResource TzSurface}"
                   FontFamily="ArchivoSemiBold" FontSize="13.5" HorizontalOptions="Center"/>
        </Border>
    </VerticalStackLayout>
</ScrollView>
```

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Expected: build succeeds. Launch on a simulator and confirm by eye: the kicker is monospaced and letter-spaced, the display title is a serif, the body is a grotesque sans, the button is gold `#8A5A12`. If any label falls back to the system font, the alias in `ConfigureFonts` does not match the `FontFamily` in `Styles.xaml`.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "feat(mobile): add Core/Tests projects, design tokens, fonts and assets"
```

---

## Task 2: Domain models

**Files:**
- Create: `TasteZambia.Core/Models/Dish.cs`, `Ingredient.cs`, `CookingStep.cs`, `Province.cs`, `Article.cs`, `Category.cs`, `Profile.cs`, `ContributionDraft.cs`
- Test: `TasteZambia.Core.Tests/Models/DishTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: every record below. All later tasks bind to these exact property names.

- [ ] **Step 1: Write the failing test**

`TasteZambia.Core.Tests/Models/DishTests.cs`:

```csharp
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Tests.Models;

public class DishTests
{
    [Fact]
    public void Dish_WithoutPhoto_ExposesPlaceholderCaption()
    {
        var dish = new Dish
        {
            Id = "kapenta",
            LocalName = "Kapenta",
            EnglishName = "Dried lake sardines",
            Region = "Luapula",
            TimeLabel = "25 min",
            Difficulty = "Easy",
            ImageAsset = null,
            PhotoNeededCaption = "photo: fried kapenta with tomato",
            Description = "Small dried fish."
        };

        Assert.False(dish.HasPhoto);
        Assert.Equal("photo: fried kapenta with tomato", dish.PhotoNeededCaption);
    }

    [Fact]
    public void Dish_WithPhoto_ReportsHasPhoto()
    {
        var dish = new Dish
        {
            Id = "ifisashi",
            LocalName = "Ifisashi",
            EnglishName = "Groundnut and greens relish",
            Region = "Pan-Zambian",
            TimeLabel = "45 min",
            Difficulty = "Easy",
            ImageAsset = "ifisashi.png",
            Description = "Leafy greens simmered in pounded groundnuts."
        };

        Assert.True(dish.HasPhoto);
    }

    [Fact]
    public void MetaLabel_JoinsTimeAndDifficultyWithDoubleSpacedInterpunct()
    {
        var dish = new Dish
        {
            Id = "chikanda", LocalName = "Chikanda", EnglishName = "Wild orchid cake",
            Region = "Northern and Muchinga", TimeLabel = "1 hr 30", Difficulty = "Medium",
            ImageAsset = "chikanda.png", Description = "Ground orchid tubers."
        };

        Assert.Equal("1 hr 30  ·  Medium", dish.MetaLabel);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~DishTests`
Expected: FAIL — `The type or namespace name 'Models' does not exist`.

- [ ] **Step 3: Write the models**

`TasteZambia.Core/Models/Dish.cs`:

```csharp
namespace TasteZambia.Core.Models;

public sealed record Dish
{
    public required string Id { get; init; }
    public required string LocalName { get; init; }
    public required string EnglishName { get; init; }
    public required string Region { get; init; }
    public required string TimeLabel { get; init; }
    public required string Difficulty { get; init; }
    public required string Description { get; init; }

    /// <summary>Resource image name, or null when photography is still missing.</summary>
    public string? ImageAsset { get; init; }

    /// <summary>Caption shown inside the striped placeholder when <see cref="ImageAsset"/> is null.</summary>
    public string PhotoNeededCaption { get; init; } = "photo needed";

    public string? PrepTime { get; init; }
    public string? CookTime { get; init; }

    public bool HasPhoto => !string.IsNullOrEmpty(ImageAsset);

    /// <summary>"45 min  ·  Easy" — two spaces either side of the interpunct, per the design.</summary>
    public string MetaLabel => $"{TimeLabel}  ·  {Difficulty}";
}

public sealed record RecipeDetail
{
    public required Dish Dish { get; init; }
    public required string Subtitle { get; init; }
    public bool IsVerified { get; init; } = true;
    public required IReadOnlyList<string> CulturalContext { get; init; }
    public required IReadOnlyList<RecipeIngredient> Ingredients { get; init; }
    public required IReadOnlyList<CookingStep> Steps { get; init; }
    public required MethodNarrative TraditionalMethod { get; init; }
    public required MethodNarrative ModernMethod { get; init; }
    public required IReadOnlyList<RegionalVariation> Variations { get; init; }
    public required Contributor Contributor { get; init; }
}

public sealed record Contributor(string Name, string Location, string? AvatarAsset);
```

`TasteZambia.Core/Models/Ingredient.cs`:

```csharp
namespace TasteZambia.Core.Models;

public sealed record LocalName(string Language, string Name);

public sealed record Ingredient
{
    public required string Key { get; init; }
    public required string LocalName { get; init; }
    public required string EnglishName { get; init; }
    public required string Description { get; init; }
    public required string WhereFound { get; init; }
    public required string TraditionalPreparation { get; init; }
    public required IReadOnlyList<LocalName> LocalNames { get; init; }

    /// <summary>Languages whose name for this ingredient is not yet recorded, comma separated.</summary>
    public required string PendingLanguages { get; init; }

    /// <summary>Dish ids this ingredient is used in.</summary>
    public required IReadOnlyList<string> UsedInDishIds { get; init; }

    public string? ImageAsset { get; init; }
    public bool HasPhoto => !string.IsNullOrEmpty(ImageAsset);
}

/// <summary>One line of a recipe's ingredient list. Linked entries open the ingredient sheet.</summary>
public sealed record RecipeIngredient
{
    public string? IngredientKey { get; init; }
    public required string DisplayName { get; init; }
    public required string DisplaySubtitle { get; init; }
    public required string Quantity { get; init; }
    public bool IsLinked => !string.IsNullOrEmpty(IngredientKey);
}
```

`TasteZambia.Core/Models/CookingStep.cs`:

```csharp
namespace TasteZambia.Core.Models;

public sealed record CookingStep(int Number, string Title, string Body);

public sealed record RegionalVariation(string Place, string Description);

public sealed record MethodNarrative(string Heading, IReadOnlyList<string> Paragraphs);

public enum PreparationMethod
{
    Traditional,
    Modern
}
```

`TasteZambia.Core/Models/Province.cs`:

```csharp
namespace TasteZambia.Core.Models;

public sealed record Province
{
    public required string Name { get; init; }
    public required string Seat { get; init; }
    public required string Blurb { get; init; }
    public required string CookingTradition { get; init; }
    public required IReadOnlyList<string> SignatureFoods { get; init; }
    public required IReadOnlyList<string> CommonIngredients { get; init; }
}
```

`TasteZambia.Core/Models/Article.cs`:

```csharp
namespace TasteZambia.Core.Models;

public enum ArticleBlockKind { Lede, Paragraph, PullQuote }

public sealed record ArticleBlock(ArticleBlockKind Kind, string Text);

public sealed record Article
{
    public required string Id { get; init; }
    public required string Kicker { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
    public required string Meta { get; init; }
    public string? Lede { get; init; }
    public bool IsLead { get; init; }
    public string? ImageAsset { get; init; }
    public string PhotoNeededCaption { get; init; } = "photo needed";
    public IReadOnlyList<ArticleBlock> Body { get; init; } = [];
    public IReadOnlyList<string> RelatedDishIds { get; init; } = [];
    public AudioNarration? Audio { get; init; }
    public bool HasPhoto => !string.IsNullOrEmpty(ImageAsset);
}

public sealed record AudioNarration(string Label, string Duration, double Progress);
```

`TasteZambia.Core/Models/Category.cs`:

```csharp
namespace TasteZambia.Core.Models;

public sealed record Category(string Name, string? ImageAsset)
{
    public bool HasPhoto => !string.IsNullOrEmpty(ImageAsset);
}
```

`TasteZambia.Core/Models/Profile.cs`:

```csharp
namespace TasteZambia.Core.Models;

public enum ContributionStatus { Published, InReview, Draft }

public sealed record UserProfile
{
    public required string Name { get; init; }
    public required string Location { get; init; }
    public required string Languages { get; init; }
    public required string AvatarAsset { get; init; }
    public required int CookedCount { get; init; }
    public required int FavouriteCount { get; init; }
    public required int ContributedCount { get; init; }
    public required int PreservedCount { get; init; }
}

/// <summary><paramref name="Tint"/> is the hex swatch on the collection's left edge.</summary>
public sealed record RecipeCollection(string Label, string CountLabel, string Tint);

public sealed record Contribution(string Name, ContributionStatus Status, string Meta);
```

`TasteZambia.Core/Models/ContributionDraft.cs`:

```csharp
namespace TasteZambia.Core.Models;

public enum PrivacyLevel { PrivateToMe, SharedWithFamily, PublicInArchive }

public sealed record ReviewStage(string Label, string Note, bool IsComplete);

/// <summary>Mutable working state for the Share and Preserve wizards.</summary>
public sealed class ContributionDraft
{
    public string LocalName { get; set; } = "";
    public string EnglishDescription { get; set; } = "";
    public string Province { get; set; } = "";
    public string MealType { get; set; } = "";
    public string Language { get; set; } = "";
    public List<RecipeIngredient> Ingredients { get; set; } = [];
    public List<string> Steps { get; set; } = [];
    public string Origin { get; set; } = "";
    public string CulturalSignificance { get; set; } = "";
    public string TraditionalMethod { get; set; } = "";
    public string TaughtBy { get; set; } = "";
    public string TaughtByOrigin { get; set; } = "";
    public string Story { get; set; } = "";
    public bool CreditTeacher { get; set; } = true;
    public bool AddToFoodStories { get; set; } = true;
    public PrivacyLevel Privacy { get; set; } = PrivacyLevel.SharedWithFamily;
    public List<string> PhotoPaths { get; set; } = [];
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~DishTests`
Expected: PASS, 3 tests.

- [ ] **Step 5: Commit**

```bash
git add TasteZambia.Core/Models TasteZambia.Core.Tests/Models
git commit -m "feat(core): add domain models for dishes, ingredients, regions and contributions"
```

---

## Task 3: Repositories and seed data

Repositories do **data access only** — no filtering, no sorting, no business rules. The in-memory implementations below are the swap point: when the API lands, add `Http*Repository` classes with the same interfaces and change only `MauiProgram.cs`.

**Files:**
- Create: `TasteZambia.Core/Data/IDishRepository.cs`, `IIngredientRepository.cs`, `IRegionRepository.cs`, `IArticleRepository.cs`, `IProfileRepository.cs`, `ICategoryRepository.cs`
- Create: `TasteZambia.Core/Data/SeedData.cs`
- Create: `TasteZambia.Core/Data/InMemoryRepositories.cs`
- Test: `TasteZambia.Core.Tests/Data/SeedDataTests.cs`

**Interfaces:**
- Consumes: every model from Task 2.
- Produces:
  - `IDishRepository`: `Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken)`, `Task<Dish?> GetByIdAsync(string id, CancellationToken)`, `Task<RecipeDetail?> GetRecipeAsync(string dishId, CancellationToken)`
  - `IIngredientRepository`: `GetAllAsync`, `Task<Ingredient?> GetByKeyAsync(string key, CancellationToken)`
  - `IRegionRepository`: `Task<IReadOnlyList<Province>> GetAllAsync(CancellationToken)`
  - `IArticleRepository`: `GetAllAsync`, `Task<Article?> GetByIdAsync(string id, CancellationToken)`
  - `ICategoryRepository`: `Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken)`
  - `IProfileRepository`: `Task<UserProfile> GetAsync(CancellationToken)`, `Task<IReadOnlyList<RecipeCollection>> GetCollectionsAsync(CancellationToken)`, `Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken)`

- [ ] **Step 1: Write the failing test**

`TasteZambia.Core.Tests/Data/SeedDataTests.cs`:

```csharp
using TasteZambia.Core.Data;

namespace TasteZambia.Core.Tests.Data;

public class SeedDataTests
{
    [Fact]
    public async Task DishRepository_ReturnsTheEightSeededDishes()
    {
        var repo = new InMemoryDishRepository();
        var dishes = await repo.GetAllAsync(CancellationToken.None);
        Assert.Equal(8, dishes.Count);
        Assert.Equal("ifisashi", dishes[0].Id);
    }

    [Fact]
    public async Task DishRepository_ReturnsFullRecipeForIfisashi()
    {
        var repo = new InMemoryDishRepository();
        var recipe = await repo.GetRecipeAsync("ifisashi", CancellationToken.None);

        Assert.NotNull(recipe);
        Assert.Equal(6, recipe!.Ingredients.Count);
        Assert.Equal(4, recipe.Steps.Count);
        Assert.Equal(4, recipe.Variations.Count);
        Assert.Equal(3, recipe.CulturalContext.Count);
        Assert.True(recipe.IsVerified);
    }

    [Fact]
    public async Task IngredientRepository_ChibwabwaLinksToThreeDishes()
    {
        var repo = new InMemoryIngredientRepository();
        var chibwabwa = await repo.GetByKeyAsync("chibwabwa", CancellationToken.None);

        Assert.NotNull(chibwabwa);
        Assert.Equal("Pumpkin leaves", chibwabwa!.EnglishName);
        Assert.Equal(3, chibwabwa.UsedInDishIds.Count);
        Assert.Contains("Tonga", chibwabwa.PendingLanguages);
    }

    [Fact]
    public async Task RegionRepository_ReturnsTenProvincesWithNorthernAtIndexSix()
    {
        var repo = new InMemoryRegionRepository();
        var provinces = await repo.GetAllAsync(CancellationToken.None);

        Assert.Equal(10, provinces.Count);
        Assert.Equal("Northern", provinces[6].Name);
        Assert.Equal("Kasama", provinces[6].Seat);
    }

    [Fact]
    public async Task DishesWithoutPhotography_CarryTheirOwnPlaceholderCaption()
    {
        var repo = new InMemoryDishRepository();
        var dishes = await repo.GetAllAsync(CancellationToken.None);
        var unphotographed = dishes.Where(d => !d.HasPhoto).ToList();

        Assert.Equal(5, unphotographed.Count);
        Assert.All(unphotographed, d => Assert.StartsWith("photo:", d.PhotoNeededCaption));
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~SeedDataTests`
Expected: FAIL — `InMemoryDishRepository` does not exist.

- [ ] **Step 3: Write the repository interfaces**

`TasteZambia.Core/Data/IDishRepository.cs` (put all six interfaces in this one file for cohesion — they change together):

```csharp
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Data;

public interface IDishRepository
{
    Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken ct = default);
    Task<Dish?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<RecipeDetail?> GetRecipeAsync(string dishId, CancellationToken ct = default);
}

public interface IIngredientRepository
{
    Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken ct = default);
    Task<Ingredient?> GetByKeyAsync(string key, CancellationToken ct = default);
}

public interface IRegionRepository
{
    Task<IReadOnlyList<Province>> GetAllAsync(CancellationToken ct = default);
}

public interface IArticleRepository
{
    Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken ct = default);
    Task<Article?> GetByIdAsync(string id, CancellationToken ct = default);
}

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
}

public interface IProfileRepository
{
    Task<UserProfile> GetAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RecipeCollection>> GetCollectionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default);
}
```

- [ ] **Step 4: Write `SeedData.cs` — the design content, verbatim**

`TasteZambia.Core/Data/SeedData.cs`. Every string is transcribed from the spec; do not paraphrase, do not add dishes, do not invent photography.

```csharp
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Data;

public static class SeedData
{
    public const string ImgIfisashi = "ifisashi.png";
    public const string ImgNshima   = "spread_nshima.png";
    public const string ImgChikanda = "chikanda.png";
    public const string ImgMarket   = "market_ingredients.png";
    public const string ImgAvatar   = "avatar_chanda.png";

    public static readonly IReadOnlyList<Dish> Dishes =
    [
        new() { Id = "ifisashi", LocalName = "Ifisashi", EnglishName = "Groundnut and greens relish",
            Region = "Pan-Zambian", TimeLabel = "45 min", Difficulty = "Easy", ImageAsset = ImgIfisashi,
            PrepTime = "20 min", CookTime = "30 min",
            Description = "Leafy greens simmered in pounded groundnuts until the sauce thickens and the oil rises. Eaten with nshima across the country." },

        new() { Id = "nshima", LocalName = "Nshima", EnglishName = "Maize meal staple",
            Region = "Pan-Zambian", TimeLabel = "30 min", Difficulty = "Easy", ImageAsset = ImgNshima,
            Description = "The staple at the centre of nearly every Zambian meal, stirred from maize meal and eaten by hand with relish." },

        new() { Id = "chikanda", LocalName = "Chikanda", EnglishName = "Wild orchid cake",
            Region = "Northern and Muchinga", TimeLabel = "1 hr 30", Difficulty = "Medium", ImageAsset = ImgChikanda,
            Description = "Ground orchid tubers cooked with groundnut flour and chilli into a firm loaf, sliced and served cold." },

        new() { Id = "kapenta", LocalName = "Kapenta", EnglishName = "Dried lake sardines",
            Region = "Luapula", TimeLabel = "25 min", Difficulty = "Easy",
            PhotoNeededCaption = "photo: fried kapenta with tomato",
            Description = "Small dried fish from Lake Tanganyika and Lake Kariba, fried with onion and tomato into a salty, deeply savoury relish." },

        new() { Id = "inkoko", LocalName = "Inkoko ya Mumushi", EnglishName = "Village chicken",
            Region = "Central", TimeLabel = "1 hr 15", Difficulty = "Medium",
            PhotoNeededCaption = "photo: village chicken stew",
            Description = "Free-range chicken cooked slowly with tomato and onion. Tougher and far more flavourful than farmed birds." },

        new() { Id = "kandolo", LocalName = "Kandolo", EnglishName = "Sweet potatoes",
            Region = "Eastern", TimeLabel = "35 min", Difficulty = "Easy",
            PhotoNeededCaption = "photo: boiled sweet potatoes",
            Description = "Boiled or roasted and eaten at breakfast with tea, or pounded with groundnuts as a sweet afternoon dish." },

        new() { Id = "munkoyo", LocalName = "Munkoyo", EnglishName = "Fermented root drink",
            Region = "North-Western", TimeLabel = "3 days", Difficulty = "Medium",
            PhotoNeededCaption = "photo: munkoyo in a calabash",
            Description = "Maize porridge fermented with munkoyo root into a lightly sour, faintly sweet drink served cool." },

        new() { Id = "delele", LocalName = "Delele", EnglishName = "Okra relish",
            Region = "Southern", TimeLabel = "30 min", Difficulty = "Easy",
            PhotoNeededCaption = "photo: okra relish",
            Description = "Okra cooked with a pinch of bicarbonate of soda until it draws into a smooth relish. Divisive, and beloved." },
    ];

    public static readonly IReadOnlyList<Category> Categories =
    [
        new("Traditional Meals", ImgNshima),
        new("Staple Foods", null),
        new("Vegetable Dishes", ImgIfisashi),
        new("Meat Dishes", null),
        new("Fish and Seafood", null),
        new("Snacks", ImgChikanda),
        new("Desserts", null),
        new("Traditional Drinks", null),
    ];

    public static readonly IReadOnlyList<Ingredient> Ingredients =
    [
        new() { Key = "chibwabwa", LocalName = "Chibwabwa", EnglishName = "Pumpkin leaves", ImageAsset = ImgMarket,
            LocalNames = [new("Bemba, Nyanja", "Chibwabwa")],
            PendingLanguages = "Tonga, Lozi, Kaonde, Lunda, Luvale",
            Description = "The young leaves and tender shoots of the pumpkin plant, picked before the fruit is taken. Sold in tied bundles at every market and grown in almost every village garden.",
            WhereFound = "Grown countrywide, most abundant in the rainy season from December to March.",
            TraditionalPreparation = "The leaves are stripped from their stalks, rolled tight and shredded fine with a knife, then rubbed between the palms with a little salt to soften them before cooking.",
            UsedInDishIds = ["ifisashi", "nshima", "delele"] },

        new() { Key = "mbalala", LocalName = "Mbalala", EnglishName = "Groundnuts",
            LocalNames = [new("Bemba", "Mbalala"), new("Nyanja", "Nsawawa")],
            PendingLanguages = "Tonga, Lozi",
            Description = "Roasted and pounded into a coarse, oily flour that thickens relishes and supplies the fat in cooking where oil was never used.",
            WhereFound = "Eastern Province is the heartland of groundnut farming; grown in every province.",
            TraditionalPreparation = "Roasted in a clay pan over coals, winnowed by hand, then pounded in a wooden mortar until the flour begins to release its oil.",
            UsedInDishIds = ["ifisashi", "chikanda", "kandolo"] },

        new() { Key = "kapenta", LocalName = "Kapenta", EnglishName = "Dried lake sardines",
            LocalNames = [new("Bemba", "Kapenta"), new("Nyanja", "Kapenta")],
            PendingLanguages = "Tonga, Lozi",
            Description = "Small freshwater sardines caught at night under lamps, sun-dried whole on racks and sold by the tin.",
            WhereFound = "Lake Tanganyika, Lake Mweru and Lake Kariba. Traded countrywide.",
            TraditionalPreparation = "Rinsed, then dry-fried without oil until crisp before tomato and onion are added.",
            UsedInDishIds = ["kapenta", "ifisashi"] },

        new() { Key = "tute", LocalName = "Tute", EnglishName = "Cassava",
            LocalNames = [new("Bemba", "Tute"), new("Luvale", "Mbombo")],
            PendingLanguages = "Tonga, Nyanja",
            Description = "A starchy root that stores in the ground for years, making it the food that carries households through a poor harvest.",
            WhereFound = "Luapula, Northern and North-Western Provinces.",
            TraditionalPreparation = "Peeled, soaked for several days to ferment and remove bitterness, then sun-dried and pounded into flour.",
            UsedInDishIds = ["nshima"] },

        new() { Key = "katapa", LocalName = "Katapa", EnglishName = "Cassava leaves",
            LocalNames = [new("Bemba", "Katapa")],
            PendingLanguages = "Luvale, Lunda, Kaonde",
            Description = "The leaves of the cassava plant, pounded rather than chopped, cooked long to soften and to drive off bitterness.",
            WhereFound = "Luapula and Northern Provinces.",
            TraditionalPreparation = "Pounded in a mortar with a little water, then simmered a long while with groundnuts.",
            UsedInDishIds = ["ifisashi"] },

        new() { Key = "chikanda", LocalName = "Chikanda", EnglishName = "Wild orchid tubers",
            LocalNames = [new("Bemba", "Chikanda")],
            PendingLanguages = "Nyanja, Tonga",
            Description = "Tubers of wild terrestrial orchids, foraged from grassland. Now under pressure from overharvesting.",
            WhereFound = "Northern, Muchinga and Eastern Provinces.",
            TraditionalPreparation = "Peeled, boiled and pounded, then cooked with groundnut flour, soda and chilli until it sets firm.",
            UsedInDishIds = ["chikanda"] },

        new() { Key = "impwa", LocalName = "Impwa", EnglishName = "Garden eggs",
            LocalNames = [new("Bemba", "Impwa"), new("Nyanja", "Sumu")],
            PendingLanguages = "Tonga, Lozi",
            Description = "Small pale African eggplants, firmer and more bitter than the purple kind, eaten raw with salt or cooked into relish.",
            WhereFound = "Grown countrywide in home gardens.",
            TraditionalPreparation = "Sliced and cooked briefly with tomato so the bitterness stays present.",
            UsedInDishIds = ["ifisashi"] },

        new() { Key = "bondwe", LocalName = "Bondwe", EnglishName = "Amaranth greens",
            LocalNames = [new("Bemba", "Bondwe"), new("Nyanja", "Bonongwe")],
            PendingLanguages = "Tonga, Lozi",
            Description = "A wild green that comes up on its own in cultivated ground. Gathered, not planted.",
            WhereFound = "Countrywide, following the rains.",
            TraditionalPreparation = "Gathered young, cooked with groundnuts or with a little soda to hold the colour.",
            UsedInDishIds = ["ifisashi"] },

        new() { Key = "kandolo", LocalName = "Kandolo", EnglishName = "Sweet potatoes",
            LocalNames = [new("Bemba", "Kandolo"), new("Nyanja", "Mbatatesi")],
            PendingLanguages = "Tonga, Lozi",
            Description = "Boiled or roasted in the coals, eaten with tea in the morning or pounded with groundnuts.",
            WhereFound = "Eastern and Northern Provinces.",
            TraditionalPreparation = "Buried in hot ash and coals to roast slowly in the skin.",
            UsedInDishIds = ["kandolo"] },
    ];

    public static readonly IReadOnlyList<Province> Provinces =
    [
        new() { Name = "Central", Seat = "Kabwe",
            Blurb = "Maize country, and the province most associated with free-range village chicken cooked slowly over an open fire.",
            SignatureFoods = ["Inkoko ya Mumushi", "Nshima", "Ifisashi"],
            CommonIngredients = ["Maize", "Groundnuts", "Village chicken"],
            CookingTradition = "Chicken is cooked whole in a clay pot set directly in the coals, the broth reduced rather than thickened." },

        new() { Name = "Copperbelt", Seat = "Ndola",
            Blurb = "The most urban food culture in the country. Mining towns drew people from every province, and the cooking absorbed all of it.",
            SignatureFoods = ["Kapenta", "Ifisashi", "Nshima"],
            CommonIngredients = ["Kapenta", "Tomatoes", "Cooking oil"],
            CookingTradition = "Relishes here carry more tomato and more oil than their village versions, a change that came with wage kitchens." },

        new() { Name = "Eastern", Seat = "Chipata",
            Blurb = "The groundnut heartland. Almost every relish in the province passes through pounded groundnuts at some point.",
            SignatureFoods = ["Chikanda", "Kandolo", "Ifisashi"],
            CommonIngredients = ["Groundnuts", "Sweet potatoes", "Pumpkin leaves"],
            CookingTradition = "Groundnuts are roasted in a dry clay pan, winnowed in a flat basket, then pounded in a wooden mortar." },

        new() { Name = "Luapula", Seat = "Mansa",
            Blurb = "Fish and cassava. The Luapula river, Lake Mweru and the Bangweulu swamps supply most of what is eaten here.",
            SignatureFoods = ["Kapenta", "Katapa", "Nshima"],
            CommonIngredients = ["Cassava", "Kapenta", "Cassava leaves"],
            CookingTradition = "Cassava is soaked for days to ferment, sun-dried on rocks, then pounded into flour for nshima." },

        new() { Name = "Lusaka", Seat = "Lusaka",
            Blurb = "Every province cooks here. The capital is where regional dishes meet, get shortened for time, and turn into street food.",
            SignatureFoods = ["Nshima", "Kapenta", "Delele"],
            CommonIngredients = ["Maize meal", "Tomatoes", "Rape leaves"],
            CookingTradition = "Market kitchens serve nshima with a choice of relishes from four or five provinces at one counter." },

        new() { Name = "Muchinga", Seat = "Chinsali",
            Blurb = "Foraged food. Wild orchid tubers and rainy-season mushrooms are gathered here in quantities found almost nowhere else.",
            SignatureFoods = ["Chikanda", "Nshima", "Ifisashi"],
            CommonIngredients = ["Wild orchid tubers", "Bowa mushrooms", "Millet"],
            CookingTradition = "Mushrooms are dried on grass mats above the cooking fire so they keep through the dry season." },

        new() { Name = "Northern", Seat = "Kasama",
            Blurb = "Bemba cooking at its source. Cassava, groundnuts and pounded leaves, with long cooking times and little added fat.",
            SignatureFoods = ["Ifisashi", "Katapa", "Chikanda"],
            CommonIngredients = ["Cassava", "Groundnuts", "Cassava leaves"],
            CookingTradition = "Leaves are pounded in a mortar rather than chopped, then simmered long enough to lose all bitterness." },

        new() { Name = "North-Western", Seat = "Solwezi",
            Blurb = "Cassava, wild fruit and honey. Home of munkoyo, the fermented root drink served cool from a calabash.",
            SignatureFoods = ["Munkoyo", "Nshima", "Katapa"],
            CommonIngredients = ["Munkoyo root", "Cassava", "Wild honey"],
            CookingTradition = "Munkoyo root is crushed and stirred into cooled maize porridge, then left to ferment for two or three days." },

        new() { Name = "Southern", Seat = "Choma",
            Blurb = "Cattle country. Soured milk sits alongside the relish here in a way it does not elsewhere in Zambia.",
            SignatureFoods = ["Delele", "Nshima", "Ifisashi"],
            CommonIngredients = ["Maize", "Soured milk", "Okra"],
            CookingTradition = "Fresh milk is left to sour in a covered gourd and eaten with nshima, needing no cooking at all." },

        new() { Name = "Western", Seat = "Mongu",
            Blurb = "The Barotse floodplain. Fish from the Zambezi, rice grown on the plain, and wild fruit from the sandveld.",
            SignatureFoods = ["Nshima", "Kapenta", "Delele"],
            CommonIngredients = ["Freshwater fish", "Rice", "Wild fruit"],
            CookingTradition = "Fish is smoked over a slow fire on raised racks, a method that carries it through the flood season." },
    ];

    /// <summary>Names appearing in <see cref="Province.SignatureFoods"/> that are not seeded dishes.</summary>
    public static readonly IReadOnlyDictionary<string, string> ExtraFoodSubtitles =
        new Dictionary<string, string> { ["Katapa"] = "Cassava leaf relish" };

    public static readonly IReadOnlyList<string> Filters =
        ["All", "Province", "Main Ingredient", "Difficulty", "Cooking Time", "Vegetarian", "Traditional", "Meal Type"];

    public static readonly IReadOnlyList<string> ShareSteps =
        ["Recipe basics", "Ingredients and method", "Cultural background", "Submit for review"];

    public static readonly IReadOnlyList<string> FamilySteps =
        ["The recipe", "Who taught you", "The story", "Who can see it"];

    public static RecipeDetail IfisashiRecipe() => new()
    {
        Dish = Dishes[0],
        Subtitle = "Traditional Zambian vegetable dish",
        IsVerified = true,
        CulturalContext =
        [
            "Ifisashi is what Zambian cooking does with what the land gives: greens from the garden, groundnuts from the field, and nothing else it does not need. There is no oil in the traditional version. The fat comes out of the pounded groundnuts themselves as the pot simmers.",
            "The name is Bemba, but the dish is eaten in every province, and the greens change with what is growing. Pumpkin leaves in the wet season, cassava leaves in Luapula, bondwe wherever it comes up on its own. It is served with nshima and eaten by hand.",
            "Households guard their own proportions. How coarse the groundnuts are pounded, whether tomato belongs in it at all, whether soda goes in to hold the colour of the leaves. These are the details that make one family's ifisashi recognisable from another's.",
        ],
        Ingredients =
        [
            new() { IngredientKey = "chibwabwa", DisplayName = "Chibwabwa", DisplaySubtitle = "Pumpkin leaves", Quantity = "2 large bundles" },
            new() { IngredientKey = "mbalala",   DisplayName = "Mbalala",   DisplaySubtitle = "Groundnuts",    Quantity = "1 cup, roasted" },
            new() { DisplayName = "Onion",    DisplaySubtitle = "Anyezi", Quantity = "1 medium, sliced" },
            new() { DisplayName = "Tomatoes", DisplaySubtitle = "Tomato", Quantity = "3 ripe, chopped" },
            new() { DisplayName = "Salt",     DisplaySubtitle = "Mucele", Quantity = "To taste" },
            new() { DisplayName = "Water",    DisplaySubtitle = "Menshi", Quantity = "1 cup" },
        ],
        Steps =
        [
            new(1, "Prepare the greens", "Strip the chibwabwa leaves from their stalks, roll them into a tight bundle and shred them finely. Rinse twice in cool water and leave to drain."),
            new(2, "Pound the groundnuts", "Roast the mbalala lightly, rub off the skins, then pound in a mortar until they turn to a coarse, oily flour. A blender works, but stop before it becomes butter."),
            new(3, "Build the base", "Soften the onion and tomato in a little water over medium heat until the tomato collapses into a thick sauce. No oil is needed."),
            new(4, "Combine and simmer", "Add the greens with a splash of water and cover for five minutes, then stir the groundnut flour through. Simmer uncovered until it thickens and the oil rises to the surface."),
        ],
        TraditionalMethod = new("Over charcoal, in a clay pot",
        [
            "The pot is earthenware, set on a mbaula of glowing charcoal. Clay holds heat evenly and lets the relish reduce slowly without catching.",
            "Groundnuts are roasted in a dry clay pan, winnowed by hand, then pounded in a wooden mortar with a heavy pestle until the flour begins to release its oil.",
            "Greens are shredded with a knife against the palm, never chopped on a board, and stirred with a wooden mwiko.",
        ]),
        ModernMethod = new("In a flat you rent abroad",
        [
            "A heavy-based saucepan on medium heat stands in for the clay pot. Keep the lid on for the first five minutes, then off to reduce.",
            "Pulse roasted peanuts in a blender in short bursts. Unsweetened natural peanut butter works if you thin it with water first; anything with sugar in it will not.",
            "Frozen chopped spinach, collard greens or kale substitute for chibwabwa. Squeeze the water out before it goes in.",
        ]),
        Variations =
        [
            new("Copperbelt", "More tomato, and often a handful of kapenta dropped in with the greens."),
            new("Eastern Province", "Made with pounded cassava leaves in place of pumpkin leaves, cooked much longer."),
            new("Northern Province", "Groundnuts pounded coarse so the texture stays rough and nutty."),
            new("Family style", "Some households finish with a spoon of soda to keep the greens bright; others refuse it."),
        ],
        Contributor = new("Chanda M.", "Kitwe", ImgAvatar),
    };

    public static readonly IReadOnlyList<Article> Articles =
    [
        new() { Id = "nshima", Kicker = "Archive essay", Title = "The History of Nshima",
            Author = "Dr. Mutale Chileshe", Meta = "8 min read · Pan-Zambian", IsLead = true,
            Lede = "The dish at the centre of every Zambian meal is younger than most people assume.",
            PhotoNeededCaption = "photo: woman stirring nshima with a mwiko",
            Audio = new("Listen in Bemba", "12:40", 0.18),
            RelatedDishIds = ["nshima"],
            Body =
            [
                new(ArticleBlockKind.Lede, "The dish at the centre of every Zambian meal is younger than most people assume. Maize arrived in this part of Africa through trade, and for a long time it sat alongside older grains rather than replacing them."),
                new(ArticleBlockKind.Paragraph, "Before maize, the staples were millet and sorghum. They were pounded, sifted and stirred into a thick porridge by the same method still used today: water brought to the boil, a thin gruel made first, then more meal worked in with a wooden mwiko until the mixture pulls away from the sides of the pot."),
                new(ArticleBlockKind.Paragraph, "What changed was the grain, not the technique. Maize gave a higher yield and a whiter meal, and through the colonial period it was actively promoted over the older grains. Within two or three generations it had become the default, and nshima came to mean maize nshima specifically."),
                new(ArticleBlockKind.PullQuote, "The older grains never disappeared. In parts of Muchinga and North-Western Province, millet nshima is still what is served when the meal matters."),
                new(ArticleBlockKind.Paragraph, "Cassava followed a similar path in the north. In Luapula and Northern Province, nshima made from fermented, sun-dried cassava flour is the everyday version, and maize is the visitor. The name stays the same; the flour, the colour and the taste do not."),
                new(ArticleBlockKind.Paragraph, "This is why the archive records nshima as a family of dishes rather than one recipe. What is constant is the method, the mwiko, and the fact that it is never eaten alone."),
            ] },

        new() { Id = "groundnuts", Kicker = "Archive essay", Title = "Why Groundnuts Anchor Zambian Cooking",
            Author = "Namakau Sitali", Meta = "6 min read · Eastern Province",
            PhotoNeededCaption = "photo: groundnuts being winnowed" },

        new() { Id = "methods", Kicker = "Technique", Title = "Traditional Cooking Methods in Zambia",
            Author = "Archive team", Meta = "11 min read · Countrywide",
            PhotoNeededCaption = "photo: clay pot on a mbaula" },

        new() { Id = "growingup", Kicker = "Community voices", Title = "Foods We Ate Growing Up",
            Author = "12 contributors", Meta = "Audio and text · Open for submissions",
            PhotoNeededCaption = "photo: family eating together" },

        new() { Id = "passeddown", Kicker = "Family archive", Title = "Recipes Passed Down Through Generations",
            Author = "Archive team", Meta = "9 min read · 24 family recipes",
            PhotoNeededCaption = "photo: handwritten recipe book" },

        new() { Id = "beforekitchens", Kicker = "Archive essay", Title = "Cooking Before the Modern Kitchen",
            Author = "Dr. Mutale Chileshe", Meta = "7 min read · Countrywide",
            PhotoNeededCaption = "photo: open fire cooking" },
    ];

    /// <summary>Compact story teasers shown on Home.</summary>
    public static readonly IReadOnlyList<(string Title, string Meta)> HomeStories =
    [
        ("The History of Nshima", "Archive essay · 8 min read"),
        ("Why Groundnuts Anchor Zambian Cooking", "Archive essay · 6 min read"),
        ("Foods We Ate Growing Up", "Community voices · 12 contributions"),
    ];

    public static readonly UserProfile Profile = new()
    {
        Name = "Chanda Mwaba", Location = "Kitwe, Copperbelt", Languages = "Bemba, English",
        AvatarAsset = ImgAvatar,
        CookedCount = 23, FavouriteCount = 14, ContributedCount = 3, PreservedCount = 4,
    };

    public static readonly IReadOnlyList<RecipeCollection> Collections =
    [
        new("My Favourite Zambian Foods", "14 recipes", "#A3452A"),
        new("Recipes I Want to Try",      "9 recipes",  "#C07F1E"),
        new("Recipes I've Cooked",        "23 recipes", "#2F6A4D"),
        new("My Family Recipes",          "4 preserved", "#17402F"),
    ];

    public static readonly IReadOnlyList<Contribution> Contributions =
    [
        new("Ubwali bwa Cassava",      ContributionStatus.Published, "Luapula · verified 12 Aug 2026"),
        new("Grandmother's Ifisashi",  ContributionStatus.InReview,  "Submitted 2 Sep 2026"),
        new("Munkoyo",                 ContributionStatus.Draft,     "Missing photos and ingredients"),
    ];

    public static readonly IReadOnlyList<string> SettingsRows =
        ["Language — English", "Offline recipes", "Notifications", "Account and privacy", "About the archive"];

    public static readonly IReadOnlyList<ReviewStage> ReviewPipeline =
    [
        new("Submitted", "Your recipe leaves your device and enters the review queue.", true),
        new("Checked against regional sources", "The archive team compares names, ingredients and method against records for the province you named.", true),
        new("Contributor contacted if anything is unclear", "We ask rather than edit. Nothing is changed without your reply.", false),
        new("Published and credited", "Credited to you, and to the person who taught you, in the public archive.", false),
    ];
}
```

- [ ] **Step 5: Write the in-memory repositories**

`TasteZambia.Core/Data/InMemoryRepositories.cs`:

```csharp
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Data;

public sealed class InMemoryDishRepository : IDishRepository
{
    public Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Dishes);

    public Task<Dish?> GetByIdAsync(string id, CancellationToken ct = default)
        => Task.FromResult(SeedData.Dishes.FirstOrDefault(d => d.Id == id));

    public Task<RecipeDetail?> GetRecipeAsync(string dishId, CancellationToken ct = default)
        => Task.FromResult<RecipeDetail?>(dishId == "ifisashi" ? SeedData.IfisashiRecipe() : null);
}

public sealed class InMemoryIngredientRepository : IIngredientRepository
{
    public Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Ingredients);

    public Task<Ingredient?> GetByKeyAsync(string key, CancellationToken ct = default)
        => Task.FromResult(SeedData.Ingredients.FirstOrDefault(i => i.Key == key));
}

public sealed class InMemoryRegionRepository : IRegionRepository
{
    public Task<IReadOnlyList<Province>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Provinces);
}

public sealed class InMemoryArticleRepository : IArticleRepository
{
    public Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Articles);

    public Task<Article?> GetByIdAsync(string id, CancellationToken ct = default)
        => Task.FromResult(SeedData.Articles.FirstOrDefault(a => a.Id == id));
}

public sealed class InMemoryCategoryRepository : ICategoryRepository
{
    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Categories);
}

public sealed class InMemoryProfileRepository : IProfileRepository
{
    public Task<UserProfile> GetAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Profile);

    public Task<IReadOnlyList<RecipeCollection>> GetCollectionsAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Collections);

    public Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Contributions);
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~SeedDataTests`
Expected: PASS, 5 tests.

- [ ] **Step 7: Commit**

```bash
git add TasteZambia.Core/Data TasteZambia.Core.Tests/Data
git commit -m "feat(core): add repository interfaces and seeded in-memory implementations"
```

---

## Task 4: Domain services

This is where the answer to *"where does the logic live?"* is enforced. Services own everything that would be identical in a web or desktop build of this app. ViewModels get none of it.

**Files:**
- Create: `TasteZambia.Core/Services/CatalogService.cs`, `FavouritesService.cs`, `CookingProgressService.cs`, `PreferenceService.cs`, `ContributionService.cs`, `INavigationService.cs`
- Test: `TasteZambia.Core.Tests/Services/CatalogServiceTests.cs`, `FavouritesServiceTests.cs`, `CookingProgressServiceTests.cs`, `PreferenceServiceTests.cs`

**Interfaces:**
- Consumes: all repositories from Task 3, all models from Task 2.
- Produces:
  - `ICatalogService`: `Task<IReadOnlyList<Dish>> SearchAsync(string query, string filter, CancellationToken)`, `Task<IReadOnlyList<Dish>> GetDishesByIdsAsync(IEnumerable<string> ids, CancellationToken)`
  - `IFavouritesService`: `bool IsSaved(string dishId)`, `void Toggle(string dishId)`, `event EventHandler<string>? Changed`
  - `ICookingProgressService`: `bool IsDone(string dishId, int stepNumber)`, `void Toggle(string dishId, int stepNumber)`, `int CompletedCount(string dishId, IEnumerable<int> stepNumbers)`, `event EventHandler<string>? Changed`
  - `IPreferenceService`: `TitleLanguage TitleLanguage { get; set; }`, `bool ShowVerificationBadge { get; set; }`, `DisplayName Resolve(string localName, string englishName)`, `event EventHandler? Changed`
  - `IContributionService`: `ContributionDraft StartShareDraft()`, `ContributionDraft StartFamilyDraft()`, `Task<Contribution> SubmitAsync(ContributionDraft draft, CancellationToken)`, `IReadOnlyList<ReviewStage> ReviewPipeline { get; }`
  - `INavigationService`: `Task GoToAsync(string route)`, `Task GoToAsync(string route, IDictionary<string, object> parameters)`, `Task GoBackAsync()`

- [ ] **Step 1: Write the failing tests**

`TasteZambia.Core.Tests/Services/CatalogServiceTests.cs`:

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Services;

public class CatalogServiceTests
{
    private static CatalogService Sut() => new(new InMemoryDishRepository());

    [Fact]
    public async Task EmptyQuery_ReturnsEveryDish()
    {
        var results = await Sut().SearchAsync("", "All", CancellationToken.None);
        Assert.Equal(8, results.Count);
    }

    [Fact]
    public async Task Query_MatchesLocalName_CaseInsensitively()
    {
        var results = await Sut().SearchAsync("IFISASHI", "All", CancellationToken.None);
        Assert.Single(results);
        Assert.Equal("ifisashi", results[0].Id);
    }

    [Fact]
    public async Task Query_MatchesEnglishNameRegionAndDescription()
    {
        var byEnglish = await Sut().SearchAsync("orchid", "All", CancellationToken.None);
        Assert.Equal("chikanda", byEnglish[0].Id);

        var byRegion = await Sut().SearchAsync("luapula", "All", CancellationToken.None);
        Assert.Equal("kapenta", byRegion[0].Id);

        var byDescription = await Sut().SearchAsync("bicarbonate", "All", CancellationToken.None);
        Assert.Equal("delele", byDescription[0].Id);
    }

    [Fact]
    public async Task Query_SurroundedByWhitespace_IsTrimmed()
    {
        var sut = Sut();
        var padded = await sut.SearchAsync("   nshima   ", "All", CancellationToken.None);
        var exact = await sut.SearchAsync("nshima", "All", CancellationToken.None);

        // "nshima" legitimately matches two dishes: Nshima itself, and Ifisashi,
        // whose description reads "Eaten with nshima across the country."
        // Trimming means the padded query behaves identically to the exact one.
        Assert.Equal(exact.Select(d => d.Id), padded.Select(d => d.Id));
        Assert.Equal(2, padded.Count);
    }

    [Fact]
    public async Task Query_WithNoMatch_ReturnsEmpty()
    {
        var results = await Sut().SearchAsync("sushi", "All", CancellationToken.None);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetDishesByIds_PreservesTheRequestedOrder()
    {
        var results = await Sut().GetDishesByIdsAsync(["delele", "ifisashi"], CancellationToken.None);
        Assert.Equal(["delele", "ifisashi"], results.Select(d => d.Id));
    }
}
```

`TasteZambia.Core.Tests/Services/FavouritesServiceTests.cs`:

```csharp
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Services;

public class FavouritesServiceTests
{
    [Fact]
    public void IfisashiIsSavedByDefault_ChikandaIsNot()
    {
        var sut = new FavouritesService();
        Assert.True(sut.IsSaved("ifisashi"));
        Assert.False(sut.IsSaved("chikanda"));
    }

    [Fact]
    public void Toggle_FlipsStateAndRaisesChangedWithTheDishId()
    {
        var sut = new FavouritesService();
        string? raised = null;
        sut.Changed += (_, id) => raised = id;

        sut.Toggle("chikanda");

        Assert.True(sut.IsSaved("chikanda"));
        Assert.Equal("chikanda", raised);
    }

    [Fact]
    public void Toggle_Twice_ReturnsToTheOriginalState()
    {
        var sut = new FavouritesService();
        sut.Toggle("nshima");
        sut.Toggle("nshima");
        Assert.False(sut.IsSaved("nshima"));
    }
}
```

`TasteZambia.Core.Tests/Services/CookingProgressServiceTests.cs`:

```csharp
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Services;

public class CookingProgressServiceTests
{
    [Fact]
    public void NoStepsDoneInitially()
    {
        var sut = new CookingProgressService();
        Assert.False(sut.IsDone("ifisashi", 1));
        Assert.Equal(0, sut.CompletedCount("ifisashi", [1, 2, 3, 4]));
    }

    [Fact]
    public void Toggle_MarksOneStepDoneForOneDishOnly()
    {
        var sut = new CookingProgressService();
        sut.Toggle("ifisashi", 2);

        Assert.True(sut.IsDone("ifisashi", 2));
        Assert.False(sut.IsDone("ifisashi", 1));
        Assert.False(sut.IsDone("chikanda", 2));
        Assert.Equal(1, sut.CompletedCount("ifisashi", [1, 2, 3, 4]));
    }

    [Fact]
    public void Toggle_RaisesChangedWithTheDishId()
    {
        var sut = new CookingProgressService();
        string? raised = null;
        sut.Changed += (_, id) => raised = id;

        sut.Toggle("ifisashi", 1);

        Assert.Equal("ifisashi", raised);
    }
}
```

`TasteZambia.Core.Tests/Services/PreferenceServiceTests.cs`:

```csharp
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Services;

public class PreferenceServiceTests
{
    [Fact]
    public void DefaultsToLocalNameAsTitle()
    {
        var sut = new PreferenceService();
        Assert.Equal(TitleLanguage.LocalName, sut.TitleLanguage);

        var name = sut.Resolve("Ifisashi", "Groundnut and greens relish");
        Assert.Equal("Ifisashi", name.Title);
        Assert.Equal("Groundnut and greens relish", name.Subtitle);
    }

    [Fact]
    public void EnglishAsTitle_SwapsTitleAndSubtitle()
    {
        var sut = new PreferenceService { TitleLanguage = TitleLanguage.English };

        var name = sut.Resolve("Ifisashi", "Groundnut and greens relish");
        Assert.Equal("Groundnut and greens relish", name.Title);
        Assert.Equal("Ifisashi", name.Subtitle);
    }

    [Fact]
    public void ChangingTitleLanguage_RaisesChanged()
    {
        var sut = new PreferenceService();
        var raised = false;
        sut.Changed += (_, _) => raised = true;

        sut.TitleLanguage = TitleLanguage.English;

        Assert.True(raised);
    }

    [Fact]
    public void VerificationBadgeIsShownByDefault()
    {
        Assert.True(new PreferenceService().ShowVerificationBadge);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~Services`
Expected: FAIL — none of the service types exist.

- [ ] **Step 3: Write the services**

`TasteZambia.Core/Services/CatalogService.cs`:

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Services;

public interface ICatalogService
{
    Task<IReadOnlyList<Dish>> SearchAsync(string query, string filter, CancellationToken ct = default);
    Task<IReadOnlyList<Dish>> GetDishesByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default);
}

public sealed class CatalogService(IDishRepository dishes) : ICatalogService
{
    public async Task<IReadOnlyList<Dish>> SearchAsync(string query, string filter, CancellationToken ct = default)
    {
        var all = await dishes.GetAllAsync(ct);
        var q = query.Trim();

        if (q.Length == 0)
            return all;

        // The design searches local name, English name, region and description as one haystack.
        return all
            .Where(d => $"{d.LocalName} {d.EnglishName} {d.Region} {d.Description}"
                .Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IReadOnlyList<Dish>> GetDishesByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        var all = await dishes.GetAllAsync(ct);
        var lookup = all.ToDictionary(d => d.Id);

        return ids.Where(lookup.ContainsKey).Select(id => lookup[id]).ToList();
    }
}
```

The `filter` parameter is accepted but not yet applied — the design's filter chips change the selected chip's appearance without narrowing results, and the real filtering will be a server-side query once the API exists. Keeping the parameter in the signature now means the ViewModel does not change later.

`TasteZambia.Core/Services/FavouritesService.cs`:

```csharp
namespace TasteZambia.Core.Services;

public interface IFavouritesService
{
    bool IsSaved(string dishId);
    void Toggle(string dishId);
    event EventHandler<string>? Changed;
}

public sealed class FavouritesService : IFavouritesService
{
    // Matches the design's initial state: ifisashi saved, chikanda not.
    private readonly HashSet<string> _saved = ["ifisashi"];

    public event EventHandler<string>? Changed;

    public bool IsSaved(string dishId) => _saved.Contains(dishId);

    public void Toggle(string dishId)
    {
        if (!_saved.Remove(dishId))
            _saved.Add(dishId);

        Changed?.Invoke(this, dishId);
    }
}
```

`TasteZambia.Core/Services/CookingProgressService.cs`:

```csharp
namespace TasteZambia.Core.Services;

public interface ICookingProgressService
{
    bool IsDone(string dishId, int stepNumber);
    void Toggle(string dishId, int stepNumber);
    int CompletedCount(string dishId, IEnumerable<int> stepNumbers);
    event EventHandler<string>? Changed;
}

public sealed class CookingProgressService : ICookingProgressService
{
    private readonly Dictionary<string, HashSet<int>> _done = [];

    public event EventHandler<string>? Changed;

    public bool IsDone(string dishId, int stepNumber)
        => _done.TryGetValue(dishId, out var steps) && steps.Contains(stepNumber);

    public void Toggle(string dishId, int stepNumber)
    {
        if (!_done.TryGetValue(dishId, out var steps))
            _done[dishId] = steps = [];

        if (!steps.Remove(stepNumber))
            steps.Add(stepNumber);

        Changed?.Invoke(this, dishId);
    }

    public int CompletedCount(string dishId, IEnumerable<int> stepNumbers)
        => _done.TryGetValue(dishId, out var steps)
            ? stepNumbers.Count(steps.Contains)
            : 0;
}
```

`TasteZambia.Core/Services/PreferenceService.cs`:

```csharp
namespace TasteZambia.Core.Services;

public enum TitleLanguage { LocalName, English }

/// <summary>A title and its explanation, already ordered per the user's preference.</summary>
public readonly record struct DisplayName(string Title, string Subtitle);

public interface IPreferenceService
{
    TitleLanguage TitleLanguage { get; set; }
    bool ShowVerificationBadge { get; set; }
    DisplayName Resolve(string localName, string englishName);
    event EventHandler? Changed;
}

public sealed class PreferenceService : IPreferenceService
{
    private TitleLanguage _titleLanguage = TitleLanguage.LocalName;
    private bool _showVerificationBadge = true;

    public event EventHandler? Changed;

    public TitleLanguage TitleLanguage
    {
        get => _titleLanguage;
        set
        {
            if (_titleLanguage == value) return;
            _titleLanguage = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool ShowVerificationBadge
    {
        get => _showVerificationBadge;
        set
        {
            if (_showVerificationBadge == value) return;
            _showVerificationBadge = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// The archive rule: the local name is the title and English is the explanation,
    /// unless the reader has explicitly asked for English titles.
    /// </summary>
    public DisplayName Resolve(string localName, string englishName)
        => _titleLanguage == TitleLanguage.English
            ? new DisplayName(englishName, localName)
            : new DisplayName(localName, englishName);
}
```

`TasteZambia.Core/Services/ContributionService.cs`:

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Services;

public interface IContributionService
{
    ContributionDraft StartShareDraft();
    ContributionDraft StartFamilyDraft();
    Task<Contribution> SubmitAsync(ContributionDraft draft, CancellationToken ct = default);
    IReadOnlyList<ReviewStage> ReviewPipeline { get; }
}

public sealed class ContributionService : IContributionService
{
    public IReadOnlyList<ReviewStage> ReviewPipeline => SeedData.ReviewPipeline;

    /// <summary>Pre-filled to match the walkthrough content in the design.</summary>
    public ContributionDraft StartShareDraft() => new()
    {
        LocalName = "Chibwabwa na Mbalala",
        EnglishDescription = "Pumpkin leaves cooked with pounded groundnuts and nothing else",
        Province = "Northern",
        MealType = "Relish",
        Ingredients =
        [
            new() { IngredientKey = "chibwabwa", DisplayName = "Chibwabwa", DisplaySubtitle = "Pumpkin leaves", Quantity = "2 bundles" },
            new() { IngredientKey = "mbalala",   DisplayName = "Mbalala",   DisplaySubtitle = "Groundnuts",    Quantity = "1 cup" },
            new() { DisplayName = "Salt", DisplaySubtitle = "Mucele", Quantity = "To taste" },
        ],
        Steps =
        [
            "Shred the leaves fine and rinse them twice.",
            "Pound the groundnuts until the oil starts to show.",
        ],
        Origin = "Cooked in Mungwi and the villages around Kasama. It is a rainy-season dish because that is when the pumpkin leaves are at their best.",
        CulturalSignificance = "This is the relish cooked when there is no money for meat, and it is not thought of as a lesser meal. It is what most people mean when they talk about eating well at home.",
        TraditionalMethod = "Clay pot on charcoal. Groundnuts pounded in a mortar, not blended.",
        CreditTeacher = true,
    };

    public ContributionDraft StartFamilyDraft() => new()
    {
        LocalName = "Ifisashi ya Banakulu",
        EnglishDescription = "Pumpkin leaves in groundnuts, the way my grandmother made it",
        Province = "Northern",
        Language = "Bemba",
        TaughtBy = "Banakulu Mwaba, my father's mother",
        TaughtByOrigin = "Mungwi, outside Kasama. She was taught by her own mother.",
        Story = "She cooked this every time we arrived from Kitwe, before we had even put our bags down. She never measured the groundnuts. She said your hand learns the amount and your head forgets it.",
        TraditionalMethod = "Clay pot on the mbaula. Groundnuts pounded, never blended. No tomato. She added the salt at the very end, off the heat.",
        AddToFoodStories = true,
        Privacy = PrivacyLevel.SharedWithFamily,
    };

    public Task<Contribution> SubmitAsync(ContributionDraft draft, CancellationToken ct = default)
        => Task.FromResult(new Contribution(
            draft.LocalName,
            ContributionStatus.InReview,
            $"{draft.Province} Province · submitted just now"));
}
```

`TasteZambia.Core/Services/INavigationService.cs`:

```csharp
namespace TasteZambia.Core.Services;

/// <summary>
/// Keeps MAUI Shell out of the ViewModels so they stay unit-testable.
/// Implemented in TasteZambia.Mobile by ShellNavigationService.
/// </summary>
public interface INavigationService
{
    Task GoToAsync(string route);
    Task GoToAsync(string route, IDictionary<string, object> parameters);
    Task GoBackAsync();
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~Services`
Expected: PASS, 16 tests.

- [ ] **Step 5: Commit**

```bash
git add TasteZambia.Core/Services TasteZambia.Core.Tests/Services
git commit -m "feat(core): add catalog, favourites, progress, preference and contribution services"
```

---

## Task 5: Shell routing, navigation service and the custom bottom nav bar

The design's bottom bar is a dot-over-label bar with no icons, and it is present on **every** screen including detail screens. Shell's native `TabBar` cannot be styled to that, so: Shell owns routing only (`Shell.TabBarIsVisible="False"` is already set globally in `Styles.xaml`), and a `BottomNavBar` control is placed in each page's root `Grid`.

**Files:**
- Create: `TasteZambia.Mobile/Services/ShellNavigationService.cs`
- Create: `TasteZambia.Mobile/Controls/BottomNavBar.xaml` + `.xaml.cs`
- Create: `TasteZambia.Core/ViewModels/BaseViewModel.cs`
- Modify: `TasteZambia.Mobile/AppShell.xaml` + `.xaml.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/BaseViewModelTests.cs`

**Interfaces:**
- Consumes: `INavigationService` from Task 4.
- Produces:
  - Routes: `//home`, `//explore`, `//regions`, `//culture`, `//profile` (tab roots, absolute); `recipe`, `ingredients`, `ingredient`, `story`, `share`, `family` (pushed, relative).
  - `BaseViewModel` with `IsBusy`, `Title`, and `protected INavigationService Navigation`.
  - `BottomNavBar` with bindable `ActiveSection` (string) — one of `home`, `explore`, `regions`, `culture`, `profile`.

**Section mapping (from the design's `NAV` table — a detail screen keeps its parent tab lit):**

| Section | Also active on |
|---|---|
| `home` | — |
| `explore` | `recipe`, `ingredients`, `ingredient` |
| `regions` | — |
| `culture` | `story` |
| `profile` | `share`, `shareStart`, `shareDraft`, `shareReview`, `shareChanges`, `sharePublished`, `family`, `famStart`, `famDraft`, `famSaved`, `famShared`, `famPublic`, `favs`, `wantTry`, `cooked`, `famList`, `settings` |

The seven onboarding screens show **no bottom nav at all** — they run before Shell (see Task 19).

- [ ] **Step 1: Write the failing test**

`TasteZambia.Core.Tests/ViewModels/BaseViewModelTests.cs`:

```csharp
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class FakeNavigation : INavigationService
{
    public List<string> Routes { get; } = [];
    public int BackCount { get; private set; }

    public Task GoToAsync(string route) { Routes.Add(route); return Task.CompletedTask; }
    public Task GoToAsync(string route, IDictionary<string, object> parameters) { Routes.Add(route); return Task.CompletedTask; }
    public Task GoBackAsync() { BackCount++; return Task.CompletedTask; }
}

file sealed class TestViewModel(INavigationService nav) : BaseViewModel(nav)
{
    public Task Go() => Navigation.GoToAsync("recipe");
}

public class BaseViewModelTests
{
    [Fact]
    public void IsBusy_RaisesPropertyChanged()
    {
        var vm = new TestViewModel(new FakeNavigation());
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.IsBusy = true;

        Assert.Contains(nameof(vm.IsBusy), changed);
    }

    [Fact]
    public async Task Navigation_IsReachableFromDerivedViewModels()
    {
        var nav = new FakeNavigation();
        await new TestViewModel(nav).Go();
        Assert.Equal("recipe", nav.Routes.Single());
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~BaseViewModelTests`
Expected: FAIL — `BaseViewModel` does not exist.

- [ ] **Step 3: Write `BaseViewModel`**

`TasteZambia.Core/ViewModels/BaseViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public abstract partial class BaseViewModel(INavigationService navigation) : ObservableObject
{
    protected INavigationService Navigation { get; } = navigation;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = "";

    /// <summary>Called by each page's OnAppearing. Override to load data.</summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;
}
```

> Note on syntax: this plan uses the field-based `[ObservableProperty]` form, which works on every CommunityToolkit.Mvvm 8.x. The partial-property form (`[ObservableProperty] public partial bool IsBusy { get; set; }`) is available in 8.4+ and is equivalent — use it consistently or not at all, do not mix.

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~BaseViewModelTests`
Expected: PASS, 2 tests.

- [ ] **Step 5: Write the Shell navigation service**

`TasteZambia.Mobile/Services/ShellNavigationService.cs`:

```csharp
using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route)
        => Shell.Current.GoToAsync(route);

    public Task GoToAsync(string route, IDictionary<string, object> parameters)
        => Shell.Current.GoToAsync(route, parameters);

    public Task GoBackAsync()
        => Shell.Current.GoToAsync("..");
}
```

- [ ] **Step 6: Write `AppShell` with the five tab roots and six pushed routes**

`TasteZambia.Mobile/AppShell.xaml`:

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Shell x:Class="TasteZambia.Mobile.AppShell"
       xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
       xmlns:views="clr-namespace:TasteZambia.Mobile.Views"
       Shell.FlyoutBehavior="Disabled"
       Shell.NavBarIsVisible="False"
       Shell.TabBarIsVisible="False">

    <ShellContent Route="home"     ContentTemplate="{DataTemplate views:HomePage}"/>
    <ShellContent Route="explore"  ContentTemplate="{DataTemplate views:ExplorePage}"/>
    <ShellContent Route="regions"  ContentTemplate="{DataTemplate views:RegionsPage}"/>
    <ShellContent Route="culture"  ContentTemplate="{DataTemplate views:CulturePage}"/>
    <ShellContent Route="profile"  ContentTemplate="{DataTemplate views:ProfilePage}"/>
</Shell>
```

`TasteZambia.Mobile/AppShell.xaml.cs`:

```csharp
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("recipe",      typeof(RecipePage));
        Routing.RegisterRoute("ingredients", typeof(IngredientsPage));
        Routing.RegisterRoute("ingredient",  typeof(IngredientPage));
        Routing.RegisterRoute("story",       typeof(StoryPage));
        Routing.RegisterRoute("share",       typeof(SharePage));
        Routing.RegisterRoute("family",      typeof(FamilyPage));
    }
}
```

- [ ] **Step 7: Write the `BottomNavBar` control**

`TasteZambia.Mobile/Controls/BottomNavBar.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TasteZambia.Mobile.Controls.BottomNavBar"
             x:Name="Root">

    <Grid HeightRequest="66"
          ColumnDefinitions="*,*,*,*,*"
          BackgroundColor="{StaticResource TzSurface}">

        <!-- 1pt hairline along the top edge -->
        <BoxView Grid.ColumnSpan="5" HeightRequest="1" VerticalOptions="Start"
                 Color="{StaticResource TzHairline}"/>

        <VerticalStackLayout Grid.Column="0" x:Name="HomeItem"     Spacing="6" VerticalOptions="Center" HorizontalOptions="Center"/>
        <VerticalStackLayout Grid.Column="1" x:Name="ExploreItem"  Spacing="6" VerticalOptions="Center" HorizontalOptions="Center"/>
        <VerticalStackLayout Grid.Column="2" x:Name="RegionsItem"  Spacing="6" VerticalOptions="Center" HorizontalOptions="Center"/>
        <VerticalStackLayout Grid.Column="3" x:Name="CultureItem"  Spacing="6" VerticalOptions="Center" HorizontalOptions="Center"/>
        <VerticalStackLayout Grid.Column="4" x:Name="ProfileItem"  Spacing="6" VerticalOptions="Center" HorizontalOptions="Center"/>
    </Grid>
</ContentView>
```

`TasteZambia.Mobile/Controls/BottomNavBar.xaml.cs` — builds the five items in code so the active-state styling stays in one place:

```csharp
using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Controls;

public partial class BottomNavBar : ContentView
{
    public static readonly BindableProperty ActiveSectionProperty =
        BindableProperty.Create(nameof(ActiveSection), typeof(string), typeof(BottomNavBar), "home",
            propertyChanged: (b, _, _) => ((BottomNavBar)b).Rebuild());

    public string ActiveSection
    {
        get => (string)GetValue(ActiveSectionProperty);
        set => SetValue(ActiveSectionProperty, value);
    }

    private static readonly (string Key, string Label)[] Sections =
    [
        ("home", "Home"), ("explore", "Explore"), ("regions", "Regions"),
        ("culture", "Culture"), ("profile", "Profile"),
    ];

    public BottomNavBar()
    {
        InitializeComponent();
        Rebuild();
    }

    private void Rebuild()
    {
        VerticalStackLayout[] slots = [HomeItem, ExploreItem, RegionsItem, CultureItem, ProfileItem];

        for (var i = 0; i < Sections.Length; i++)
        {
            var (key, label) = Sections[i];
            var active = key == ActiveSection;
            var slot = slots[i];

            slot.Children.Clear();

            // 5pt dot: clay when active, transparent otherwise.
            slot.Children.Add(new BoxView
            {
                WidthRequest = 5,
                HeightRequest = 5,
                CornerRadius = 2.5,
                HorizontalOptions = LayoutOptions.Center,
                Color = active ? GetColor("TzClay") : Colors.Transparent,
            });

            slot.Children.Add(new Label
            {
                Text = label,
                FontFamily = active ? "ArchivoSemiBold" : "ArchivoRegular",
                FontSize = 10.5,
                CharacterSpacing = 0.1,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = GetColor(active ? "TzGreenDeep" : "TzMuted"),
            });

            var tap = new TapGestureRecognizer();
            var target = key;
            tap.Tapped += async (_, _) => await NavigateAsync(target);
            slot.GestureRecognizers.Clear();
            slot.GestureRecognizers.Add(tap);

            SemanticProperties.SetDescription(slot, label);
            SemanticProperties.SetHint(slot, active ? $"{label}, selected" : $"Go to {label}");
        }
    }

    private static async Task NavigateAsync(string section)
    {
        var nav = Application.Current?.Handler?.MauiContext?.Services
            .GetService<INavigationService>();

        if (nav is not null)
            await nav.GoToAsync($"//{section}");
    }

    private static Color GetColor(string key)
        => Application.Current!.Resources.TryGetValue(key, out var value) && value is Color c
            ? c
            : Colors.Black;
}
```

- [ ] **Step 8: Register everything in `MauiProgram.cs`**

Add above `return builder.Build();`:

```csharp
// ---- Repositories: the ONLY layer that changes when the API lands. ----
builder.Services.AddSingleton<IDishRepository, InMemoryDishRepository>();
builder.Services.AddSingleton<IIngredientRepository, InMemoryIngredientRepository>();
builder.Services.AddSingleton<IRegionRepository, InMemoryRegionRepository>();
builder.Services.AddSingleton<IArticleRepository, InMemoryArticleRepository>();
builder.Services.AddSingleton<ICategoryRepository, InMemoryCategoryRepository>();
builder.Services.AddSingleton<IProfileRepository, InMemoryProfileRepository>();

// ---- Services: unchanged by the API swap. ----
builder.Services.AddSingleton<ICatalogService, CatalogService>();
builder.Services.AddSingleton<IFavouritesService, FavouritesService>();
builder.Services.AddSingleton<ICookingProgressService, CookingProgressService>();
builder.Services.AddSingleton<IPreferenceService, PreferenceService>();
builder.Services.AddSingleton<IContributionService, ContributionService>();
builder.Services.AddSingleton<INavigationService, ShellNavigationService>();

// ---- Shell ----
builder.Services.AddSingleton<AppShell>();
```

with these usings at the top:

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Mobile.Services;
```

Favourites, cooking progress and preferences are **singletons on purpose**: the design shows a heart toggled on Home staying toggled on Explore and on the recipe screen.

Page and ViewModel registrations are added by each page task. Register both as `Transient`, and give each page a constructor that takes its ViewModel — Shell resolves pages from the container.

- [ ] **Step 9: Verify the shell builds and the bar renders**

Create the five tab pages as empty stubs so the Shell compiles — each is a `ContentPage` whose content is a `Grid RowDefinitions="*,Auto"` with a placeholder `Label` in row 0 and `<controls:BottomNavBar Grid.Row="1" ActiveSection="home"/>` in row 1 (with the section name matching the page).

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Expected: build succeeds. Launch and confirm: the bar is 66pt, the active label is `TzGreenDeep` semibold with a clay dot above it, the other four are `TzMuted` regular with no dot, and tapping each label switches pages.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "feat(mobile): add Shell routing, navigation service and custom bottom nav bar"
```

---

## Task 6: Shared controls

Six controls carry most of the design's repetition. Build them once here; every page task consumes them.

**Files:**
- Create: `TasteZambia.Mobile/Controls/StripePlaceholder.cs`
- Create: `TasteZambia.Mobile/Controls/PhotoOrPlaceholder.xaml` + `.xaml.cs`
- Create: `TasteZambia.Mobile/Controls/SectionHeader.xaml` + `.xaml.cs`
- Create: `TasteZambia.Mobile/Controls/PillLabel.xaml` + `.xaml.cs`
- Create: `TasteZambia.Mobile/Controls/CircleButton.xaml` + `.xaml.cs`
- Create: `TasteZambia.Mobile/Controls/WizardProgress.xaml` + `.xaml.cs`
- Create: `TasteZambia.Mobile/Converters/Converters.cs`

**Interfaces:**
- Consumes: colour tokens and text styles from Task 1.
- Produces:
  - `StripePlaceholder` — `GraphicsView` subclass drawing 45° stripes. Bindable: `StripeWidth` (double, default 6).
  - `PhotoOrPlaceholder` — bindables `ImageAsset` (string?), `Caption` (string), `CornerRadius` (double), `ShowCaption` (bool).
  - `SectionHeader` — bindables `Kicker` (string?), `TitleText` (string), `ActionText` (string?), `ActionCommand` (ICommand?).
  - `PillLabel` — bindables `Text`, `TextColor`, `PillBackground`.
  - `CircleButton` — bindables `Glyph`, `GlyphColor`, `Diameter` (default 36), `Command`.
  - `WizardProgress` — bindables `StepTitle`, `StepNumber` (int), `StepCount` (int).
  - Converters: `InvertedBoolConverter`, `IsNotNullConverter`, `HexToColorConverter`, `BoolToColorConverter` (params `TrueColor`/`FalseColor`).

- [ ] **Step 1: Write `StripePlaceholder`**

The missing-photo treatment is `repeating-linear-gradient(135deg, #DED4C2 0 6px, #E9E1D3 6px 12px)`. MAUI has no repeating gradient, so draw it.

`TasteZambia.Mobile/Controls/StripePlaceholder.cs`:

```csharp
namespace TasteZambia.Mobile.Controls;

/// <summary>45-degree two-tone stripe fill standing in for photography we do not have yet.</summary>
public sealed class StripePlaceholder : GraphicsView
{
    public static readonly BindableProperty StripeWidthProperty =
        BindableProperty.Create(nameof(StripeWidth), typeof(double), typeof(StripePlaceholder), 6.0,
            propertyChanged: (b, _, _) => ((StripePlaceholder)b).Invalidate());

    public double StripeWidth
    {
        get => (double)GetValue(StripeWidthProperty);
        set => SetValue(StripeWidthProperty, value);
    }

    public StripePlaceholder()
    {
        Drawable = new StripeDrawable(this);
    }

    private sealed class StripeDrawable(StripePlaceholder owner) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF rect)
        {
            var a = Color.FromArgb("#DED4C2");
            var b = Color.FromArgb("#E9E1D3");
            var w = (float)owner.StripeWidth;

            canvas.FillColor = b;
            canvas.FillRectangle(rect);

            canvas.SaveState();
            canvas.ClipRectangle(rect);
            canvas.StrokeColor = a;
            canvas.StrokeSize = w;

            // 135deg stripes: draw diagonals from bottom-left to top-right,
            // stepping by twice the stripe width so A and B alternate evenly.
            var span = rect.Width + rect.Height;
            for (var x = -rect.Height; x < span; x += w * 2)
            {
                canvas.DrawLine(rect.X + x, rect.Bottom, rect.X + x + rect.Height, rect.Y);
            }

            canvas.RestoreState();
        }
    }
}
```

- [ ] **Step 2: Write `PhotoOrPlaceholder`**

Every dish, ingredient, category and article image goes through this control, so the "photo missing" rule can never be forgotten.

`TasteZambia.Mobile/Controls/PhotoOrPlaceholder.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TasteZambia.Mobile.Controls"
             x:Class="TasteZambia.Mobile.Controls.PhotoOrPlaceholder"
             x:Name="Root">

    <Border x:Name="Clip" StrokeThickness="0" Padding="0">
        <Grid>
            <Image x:Name="Photo" Aspect="AspectFill" IsVisible="False"/>

            <Grid x:Name="Placeholder" IsVisible="False">
                <controls:StripePlaceholder/>
                <Label x:Name="CaptionLabel"
                       Margin="11"
                       VerticalOptions="End"
                       FontFamily="PlexMonoRegular"
                       FontSize="9"
                       LineHeight="1.3"
                       TextColor="{StaticResource TzMuted2}"/>
            </Grid>
        </Grid>
    </Border>
</ContentView>
```

`TasteZambia.Mobile/Controls/PhotoOrPlaceholder.xaml.cs`:

```csharp
using Microsoft.Maui.Controls.Shapes;

namespace TasteZambia.Mobile.Controls;

public partial class PhotoOrPlaceholder : ContentView
{
    public static readonly BindableProperty ImageAssetProperty =
        BindableProperty.Create(nameof(ImageAsset), typeof(string), typeof(PhotoOrPlaceholder), null,
            propertyChanged: (b, _, _) => ((PhotoOrPlaceholder)b).Apply());

    public static readonly BindableProperty CaptionProperty =
        BindableProperty.Create(nameof(Caption), typeof(string), typeof(PhotoOrPlaceholder), "photo needed",
            propertyChanged: (b, _, _) => ((PhotoOrPlaceholder)b).Apply());

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(double), typeof(PhotoOrPlaceholder), 16.0,
            propertyChanged: (b, _, _) => ((PhotoOrPlaceholder)b).Apply());

    public static readonly BindableProperty ShowCaptionProperty =
        BindableProperty.Create(nameof(ShowCaption), typeof(bool), typeof(PhotoOrPlaceholder), true,
            propertyChanged: (b, _, _) => ((PhotoOrPlaceholder)b).Apply());

    public string? ImageAsset { get => (string?)GetValue(ImageAssetProperty); set => SetValue(ImageAssetProperty, value); }
    public string Caption { get => (string)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public bool ShowCaption { get => (bool)GetValue(ShowCaptionProperty); set => SetValue(ShowCaptionProperty, value); }

    public PhotoOrPlaceholder()
    {
        InitializeComponent();
        Apply();
    }

    private void Apply()
    {
        Clip.StrokeShape = new RoundRectangle { CornerRadius = CornerRadius };

        var hasPhoto = !string.IsNullOrEmpty(ImageAsset);
        Photo.IsVisible = hasPhoto;
        Placeholder.IsVisible = !hasPhoto;

        if (hasPhoto)
            Photo.Source = ImageSource.FromFile(ImageAsset);

        CaptionLabel.Text = Caption;
        CaptionLabel.IsVisible = ShowCaption && !string.IsNullOrEmpty(Caption);

        SemanticProperties.SetDescription(this, hasPhoto ? "" : Caption);
    }
}
```

- [ ] **Step 3: Write `SectionHeader`, `PillLabel`, `CircleButton` and `WizardProgress`**

`SectionHeader.xaml` — the "kicker / serif title / View all" trio used on Home, Explore, Ingredients, Regions, Culture, Share and Family:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TasteZambia.Mobile.Controls.SectionHeader"
             x:Name="Root">

    <VerticalStackLayout Spacing="7" BindingContext="{x:Reference Root}">
        <Label Style="{StaticResource KickerLabel}"
               Text="{Binding Kicker}"
               IsVisible="{Binding HasKicker}"/>

        <Grid ColumnDefinitions="*,Auto" VerticalOptions="End">
            <Label Style="{StaticResource SubsectionTitle}" Text="{Binding TitleText}"/>
            <Label Grid.Column="1"
                   Text="{Binding ActionText}"
                   IsVisible="{Binding HasAction}"
                   FontFamily="ArchivoMedium" FontSize="11"
                   TextColor="{StaticResource TzGold}"
                   VerticalOptions="End">
                <Label.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding ActionCommand}"/>
                </Label.GestureRecognizers>
            </Label>
        </Grid>
    </VerticalStackLayout>
</ContentView>
```

```csharp
using System.Windows.Input;

namespace TasteZambia.Mobile.Controls;

public partial class SectionHeader : ContentView
{
    public static readonly BindableProperty KickerProperty =
        BindableProperty.Create(nameof(Kicker), typeof(string), typeof(SectionHeader), null,
            propertyChanged: (b, _, _) => ((SectionHeader)b).OnPropertyChanged(nameof(HasKicker)));

    public static readonly BindableProperty TitleTextProperty =
        BindableProperty.Create(nameof(TitleText), typeof(string), typeof(SectionHeader), "");

    public static readonly BindableProperty ActionTextProperty =
        BindableProperty.Create(nameof(ActionText), typeof(string), typeof(SectionHeader), null,
            propertyChanged: (b, _, _) => ((SectionHeader)b).OnPropertyChanged(nameof(HasAction)));

    public static readonly BindableProperty ActionCommandProperty =
        BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(SectionHeader), null);

    public string? Kicker { get => (string?)GetValue(KickerProperty); set => SetValue(KickerProperty, value); }
    public string TitleText { get => (string)GetValue(TitleTextProperty); set => SetValue(TitleTextProperty, value); }
    public string? ActionText { get => (string?)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => (ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }

    public bool HasKicker => !string.IsNullOrEmpty(Kicker);
    public bool HasAction => !string.IsNullOrEmpty(ActionText);

    public SectionHeader() => InitializeComponent();
}
```

`PillLabel.xaml` — the time / difficulty / status pills:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TasteZambia.Mobile.Controls.PillLabel"
             x:Name="Root">

    <Border BindingContext="{x:Reference Root}"
            BackgroundColor="{Binding PillBackground}"
            StrokeThickness="0"
            StrokeShape="RoundRectangle 6"
            Padding="8,5"
            HorizontalOptions="Start">
        <Label Text="{Binding Text}"
               TextColor="{Binding TextColor}"
               FontFamily="ArchivoMedium"
               FontSize="10"/>
    </Border>
</ContentView>
```

```csharp
namespace TasteZambia.Mobile.Controls;

public partial class PillLabel : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(PillLabel), "");

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(PillLabel), Colors.Black);

    public static readonly BindableProperty PillBackgroundProperty =
        BindableProperty.Create(nameof(PillBackground), typeof(Color), typeof(PillLabel), Colors.Transparent);

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public Color TextColor { get => (Color)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }
    public Color PillBackground { get => (Color)GetValue(PillBackgroundProperty); set => SetValue(PillBackgroundProperty, value); }

    public PillLabel() => InitializeComponent();
}
```

`CircleButton.xaml` — the translucent back / save circles on hero images:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TasteZambia.Mobile.Controls.CircleButton"
             x:Name="Root">

    <Border BindingContext="{x:Reference Root}"
            WidthRequest="{Binding Diameter}"
            HeightRequest="{Binding Diameter}"
            BackgroundColor="#E6FFFDF9"
            StrokeThickness="0"
            StrokeShape="{Binding Shape}"
            Padding="0">
        <Label Text="{Binding Glyph}"
               TextColor="{Binding GlyphColor}"
               FontSize="16"
               HorizontalOptions="Center"
               VerticalOptions="Center"/>
        <Border.GestureRecognizers>
            <TapGestureRecognizer Command="{Binding Command}"/>
        </Border.GestureRecognizers>
    </Border>
</ContentView>
```

```csharp
using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace TasteZambia.Mobile.Controls;

public partial class CircleButton : ContentView
{
    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(CircleButton), "←");

    public static readonly BindableProperty GlyphColorProperty =
        BindableProperty.Create(nameof(GlyphColor), typeof(Color), typeof(CircleButton), Colors.Black);

    public static readonly BindableProperty DiameterProperty =
        BindableProperty.Create(nameof(Diameter), typeof(double), typeof(CircleButton), 36.0,
            propertyChanged: (b, _, _) => ((CircleButton)b).OnPropertyChanged(nameof(Shape)));

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(CircleButton), null);

    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public Color GlyphColor { get => (Color)GetValue(GlyphColorProperty); set => SetValue(GlyphColorProperty, value); }
    public double Diameter { get => (double)GetValue(DiameterProperty); set => SetValue(DiameterProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    public IShape Shape => new RoundRectangle { CornerRadius = Diameter / 2 };

    public CircleButton() => InitializeComponent();
}
```

`WizardProgress.xaml` — the "Step 2 of 4" header and gold track shared by both wizards:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TasteZambia.Mobile.Controls.WizardProgress"
             x:Name="Root">

    <VerticalStackLayout Spacing="8" BindingContext="{x:Reference Root}">
        <Grid ColumnDefinitions="*,Auto" VerticalOptions="End">
            <Label Text="{Binding StepTitle}"
                   FontFamily="ArchivoSemiBold" FontSize="12"
                   TextColor="{StaticResource TzGreenDeep}"/>
            <Label Grid.Column="1"
                   Text="{Binding StepLabel}"
                   FontFamily="PlexMonoRegular" FontSize="10.5"
                   TextColor="{StaticResource TzMuted2}"/>
        </Grid>

        <Border HeightRequest="4" StrokeThickness="0" Padding="0"
                StrokeShape="RoundRectangle 2"
                BackgroundColor="{StaticResource TzRule}">
            <BoxView Color="{StaticResource TzGoldDecor}"
                     HorizontalOptions="Start"
                     WidthRequest="{Binding FillWidth}"/>
        </Border>
    </VerticalStackLayout>
</ContentView>
```

```csharp
namespace TasteZambia.Mobile.Controls;

public partial class WizardProgress : ContentView
{
    public static readonly BindableProperty StepTitleProperty =
        BindableProperty.Create(nameof(StepTitle), typeof(string), typeof(WizardProgress), "");

    public static readonly BindableProperty StepNumberProperty =
        BindableProperty.Create(nameof(StepNumber), typeof(int), typeof(WizardProgress), 1,
            propertyChanged: (b, _, _) => ((WizardProgress)b).Refresh());

    public static readonly BindableProperty StepCountProperty =
        BindableProperty.Create(nameof(StepCount), typeof(int), typeof(WizardProgress), 4,
            propertyChanged: (b, _, _) => ((WizardProgress)b).Refresh());

    public string StepTitle { get => (string)GetValue(StepTitleProperty); set => SetValue(StepTitleProperty, value); }
    public int StepNumber { get => (int)GetValue(StepNumberProperty); set => SetValue(StepNumberProperty, value); }
    public int StepCount { get => (int)GetValue(StepCountProperty); set => SetValue(StepCountProperty, value); }

    public string StepLabel => $"Step {StepNumber} of {StepCount}";

    /// <summary>Track is laid out at the page's content width (390 - 40 padding).</summary>
    public double FillWidth => 350.0 * StepNumber / Math.Max(1, StepCount);

    public WizardProgress() => InitializeComponent();

    private void Refresh()
    {
        OnPropertyChanged(nameof(StepLabel));
        OnPropertyChanged(nameof(FillWidth));
    }
}
```

- [ ] **Step 4: Write the converters**

`TasteZambia.Mobile/Converters/Converters.cs`:

```csharp
using System.Globalization;

namespace TasteZambia.Mobile.Converters;

public sealed class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

public sealed class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null && value is not string { Length: 0 };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class HexToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string hex && hex.Length > 0 ? Color.FromArgb(hex) : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Binds a bool to one of two colours. Set TrueColor/FalseColor in XAML.</summary>
public sealed class BoolToColorConverter : IValueConverter
{
    public Color TrueColor { get; set; } = Colors.Black;
    public Color FalseColor { get; set; } = Colors.Transparent;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueColor : FalseColor;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

Register the stateless ones once in `App.xaml` so pages do not each declare them:

```xml
<ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
        <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
    </ResourceDictionary.MergedDictionaries>

    <converters:InvertedBoolConverter x:Key="InvertedBool"/>
    <converters:IsNotNullConverter x:Key="IsNotNull"/>
    <converters:HexToColorConverter x:Key="HexToColor"/>
</ResourceDictionary>
```

with `xmlns:converters="clr-namespace:TasteZambia.Mobile.Converters"` on the `Application` element.

- [ ] **Step 5: Verify the controls render**

Put one of each into the temporary `MainPage` probe from Task 1 Step 9:

```xml
<controls:PhotoOrPlaceholder HeightRequest="158" CornerRadius="18"
                             ImageAsset="ifisashi.png"/>
<controls:PhotoOrPlaceholder HeightRequest="158" CornerRadius="18"
                             Caption="photo: fried kapenta with tomato"/>
<controls:SectionHeader Kicker="Heirloom flavours"
                        TitleText="Popular Zambian Dishes"
                        ActionText="View all"/>
<HorizontalStackLayout Spacing="8">
    <controls:PillLabel Text="45 min" PillBackground="{StaticResource TzGreenTint}"
                        TextColor="{StaticResource TzGreenMid}"/>
    <controls:PillLabel Text="Easy" PillBackground="{StaticResource TzGoldTint}"
                        TextColor="{StaticResource TzGoldTintText}"/>
</HorizontalStackLayout>
<controls:WizardProgress StepTitle="Ingredients and method" StepNumber="2" StepCount="4"/>
```

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Expected: build succeeds. On the simulator confirm: the first photo shows ifisashi with an 18pt radius; the second shows diagonal stripes with the mono caption bottom-left; the pills are green-tinted and gold-tinted; the wizard track is half-filled in gold.

- [ ] **Step 6: Commit**

```bash
git add TasteZambia.Mobile/Controls TasteZambia.Mobile/Converters TasteZambia.Mobile/App.xaml
git commit -m "feat(mobile): add shared controls and value converters"
```

---

## Task 7: Home screen (frame 01)

**Layout, top to bottom:** header (34pt logo + wordmark + search circle + avatar) · 340pt hero card with gradient scrim and gold CTA · three stat figures · "Food Categories" horizontal rail of 112x136 cards · "Popular Zambian Dishes" horizontal rail of 236pt cards · cream "Food stories" panel with three rows · 26pt spacer.

**Files:**
- Create: `TasteZambia.Core/ViewModels/Items/DishItemViewModel.cs`
- Create: `TasteZambia.Core/ViewModels/HomeViewModel.cs`
- Create: `TasteZambia.Mobile/Views/HomePage.xaml` + `.xaml.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/HomeViewModelTests.cs`

**Interfaces:**
- Consumes: `ICatalogService`, `ICategoryRepository`, `IFavouritesService`, `IPreferenceService`, `INavigationService`.
- Produces:
  - `DishItemViewModel` — `Id`, `Title`, `Subtitle`, `Region`, `MetaLabel`, `TimeLabel`, `Difficulty`, `Description`, `ImageAsset`, `PhotoCaption`, `IsSaved`, `SaveGlyph`, `SaveColorHex`, `ToggleSaveCommand`, `OpenCommand`. Reused by Home, Explore, Ingredient detail and Story.
  - `HomeViewModel` — `Categories`, `Dishes`, `Stories`, `OpenExploreCommand`, `OpenCultureCommand`.

- [ ] **Step 1: Write the failing test**

`TasteZambia.Core.Tests/ViewModels/HomeViewModelTests.cs`:

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class RecordingNavigation : INavigationService
{
    public List<string> Routes { get; } = [];
    public Task GoToAsync(string route) { Routes.Add(route); return Task.CompletedTask; }
    public Task GoToAsync(string route, IDictionary<string, object> p) { Routes.Add(route); return Task.CompletedTask; }
    public Task GoBackAsync() => Task.CompletedTask;
}

public class HomeViewModelTests
{
    private static HomeViewModel Sut(INavigationService? nav = null) => new(
        new CatalogService(new InMemoryDishRepository()),
        new InMemoryCategoryRepository(),
        new FavouritesService(),
        new PreferenceService(),
        nav ?? new RecordingNavigation());

    [Fact]
    public async Task Initialize_LoadsCategoriesDishesAndStories()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal(8, vm.Categories.Count);
        Assert.Equal(8, vm.Dishes.Count);
        Assert.Equal(3, vm.Stories.Count);
    }

    [Fact]
    public async Task DishItems_PutTheLocalNameInTheTitle()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal("Ifisashi", vm.Dishes[0].Title);
        Assert.Equal("Groundnut and greens relish", vm.Dishes[0].Subtitle);
    }

    [Fact]
    public async Task IfisashiStartsSaved_AndToggleFlipsTheGlyph()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        var ifisashi = vm.Dishes[0];

        Assert.True(ifisashi.IsSaved);
        Assert.Equal("♥", ifisashi.SaveGlyph);

        ifisashi.ToggleSaveCommand.Execute(null);

        Assert.False(ifisashi.IsSaved);
        Assert.Equal("♡", ifisashi.SaveGlyph);
    }

    [Fact]
    public async Task OpeningADish_NavigatesToTheRecipeRoute()
    {
        var nav = new RecordingNavigation();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        vm.Dishes[0].OpenCommand.Execute(null);

        Assert.Equal("recipe", nav.Routes.Single());
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~HomeViewModelTests`
Expected: FAIL — `HomeViewModel` does not exist.

- [ ] **Step 3: Write `DishItemViewModel`**

`TasteZambia.Core/ViewModels/Items/DishItemViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels.Items;

public sealed partial class DishItemViewModel : ObservableObject
{
    private readonly IFavouritesService _favourites;
    private readonly INavigationService _navigation;

    public string Id { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string Region { get; }
    public string MetaLabel { get; }
    public string TimeLabel { get; }
    public string Difficulty { get; }
    public string Description { get; }
    public string? ImageAsset { get; }
    public string PhotoCaption { get; }

    public DishItemViewModel(Dish dish, IFavouritesService favourites,
                             IPreferenceService preferences, INavigationService navigation)
    {
        _favourites = favourites;
        _navigation = navigation;

        var name = preferences.Resolve(dish.LocalName, dish.EnglishName);

        Id = dish.Id;
        Title = name.Title;
        Subtitle = name.Subtitle;
        Region = dish.Region;
        MetaLabel = dish.MetaLabel;
        TimeLabel = dish.TimeLabel;
        Difficulty = dish.Difficulty;
        Description = dish.Description;
        ImageAsset = dish.ImageAsset;
        PhotoCaption = dish.PhotoNeededCaption;

        _isSaved = favourites.IsSaved(dish.Id);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveGlyph))]
    [NotifyPropertyChangedFor(nameof(SaveColorHex))]
    private bool _isSaved;

    /// <summary>Filled heart when saved, outline heart when not.</summary>
    public string SaveGlyph => IsSaved ? "♥" : "♡";

    public string SaveColorHex => IsSaved ? "#A3452A" : "#57493A";

    [RelayCommand]
    private void ToggleSave()
    {
        _favourites.Toggle(Id);
        IsSaved = _favourites.IsSaved(Id);
    }

    [RelayCommand]
    private Task Open()
        => _navigation.GoToAsync("recipe", new Dictionary<string, object> { ["dishId"] = Id });
}
```

`SaveColorHex` is a hex string rather than a `Color` so the ViewModel stays free of MAUI types; the XAML converts it with `HexToColorConverter`.

- [ ] **Step 4: Write `HomeViewModel`**

`TasteZambia.Core/ViewModels/HomeViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels.Items;

namespace TasteZambia.Core.ViewModels;

public sealed record StoryTeaser(string Title, string Meta);

public sealed partial class HomeViewModel(
    ICatalogService catalog,
    ICategoryRepository categories,
    IFavouritesService favourites,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<DishItemViewModel> Dishes { get; } = [];
    public ObservableCollection<StoryTeaser> Stories { get; } = [];

    public override async Task InitializeAsync()
    {
        if (Dishes.Count > 0) return;

        IsBusy = true;
        try
        {
            foreach (var category in await categories.GetAllAsync())
                Categories.Add(category);

            foreach (var dish in await catalog.SearchAsync("", "All"))
                Dishes.Add(new DishItemViewModel(dish, favourites, preferences, Navigation));

            foreach (var (title, meta) in SeedData.HomeStories)
                Stories.Add(new StoryTeaser(title, meta));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenExplore() => Navigation.GoToAsync("//explore");

    [RelayCommand]
    private Task OpenCulture() => Navigation.GoToAsync("//culture");
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~HomeViewModelTests`
Expected: PASS, 4 tests.

- [ ] **Step 6: Write `HomePage.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TasteZambia.Mobile.Controls"
             xmlns:vm="clr-namespace:TasteZambia.Core.ViewModels;assembly=TasteZambia.Core"
             xmlns:items="clr-namespace:TasteZambia.Core.ViewModels.Items;assembly=TasteZambia.Core"
             xmlns:models="clr-namespace:TasteZambia.Core.Models;assembly=TasteZambia.Core"
             x:Class="TasteZambia.Mobile.Views.HomePage"
             x:DataType="vm:HomeViewModel">

    <Grid RowDefinitions="*,Auto">

        <ScrollView Grid.Row="0">
            <VerticalStackLayout>

                <!-- Header -->
                <Grid Padding="20,16,20,10" ColumnDefinitions="Auto,*,Auto,Auto" ColumnSpacing="9">
                    <Image Source="logo.png" WidthRequest="34" HeightRequest="34" Aspect="AspectFit"/>
                    <VerticalStackLayout Grid.Column="1" Spacing="3" VerticalOptions="Center">
                        <Label Text="Taste Zambia" FontFamily="NewsreaderSemiBold" FontSize="19"
                               LineHeight="1" TextColor="{StaticResource TzGreenDeep}"/>
                        <Label Text="Heritage &amp; Flavours" Style="{StaticResource KickerLabel}"
                               FontSize="8" CharacterSpacing="1.12" TextColor="{StaticResource TzGold}"/>
                    </VerticalStackLayout>
                    <controls:CircleButton Grid.Column="2" Glyph="⌕" Diameter="34"
                                           GlyphColor="{StaticResource TzBodyMuted}"
                                           Command="{Binding OpenExploreCommand}"/>
                    <Border Grid.Column="3" WidthRequest="34" HeightRequest="34"
                            StrokeThickness="0" StrokeShape="RoundRectangle 17" Padding="0">
                        <Image Source="avatar_chanda.png" Aspect="AspectFill"/>
                    </Border>
                </Grid>

                <!-- Hero -->
                <Border Margin="20,6,20,0" HeightRequest="340" StrokeThickness="0"
                        StrokeShape="RoundRectangle 22" Padding="0">
                    <Grid>
                        <Image Source="spread_nshima.png" Aspect="AspectFill"/>
                        <BoxView>
                            <BoxView.Background>
                                <LinearGradientBrush StartPoint="0,1" EndPoint="0,0">
                                    <GradientStop Color="#F00E2018" Offset="0.0"/>
                                    <GradientStop Color="#B80E2018" Offset="0.42"/>
                                    <GradientStop Color="#2E0E2018" Offset="0.78"/>
                                    <GradientStop Color="#0D0E2018" Offset="1.0"/>
                                </LinearGradientBrush>
                            </BoxView.Background>
                        </BoxView>

                        <VerticalStackLayout Padding="20" VerticalOptions="End" Spacing="0">
                            <Label Text="Living cultural archive" Style="{StaticResource KickerLabel}"
                                   TextColor="{StaticResource TzGoldLight}" Margin="0,0,0,11"/>
                            <Label Text="Discover the Taste of Zambia"
                                   FontFamily="NewsreaderMedium" FontSize="35" LineHeight="1.05"
                                   CharacterSpacing="-0.35" TextColor="{StaticResource TzSurface}"
                                   Margin="0,0,0,10"/>
                            <Label Text="Explore traditional recipes, discover local ingredients, and preserve the stories behind Zambian food."
                                   FontFamily="ArchivoRegular" FontSize="13" LineHeight="1.55"
                                   TextColor="{StaticResource TzOnDark86}" Margin="0,0,0,16"/>

                            <Border BackgroundColor="{StaticResource TzGold}" StrokeThickness="0"
                                    StrokeShape="RoundRectangle 14" Padding="18,14">
                                <Grid ColumnDefinitions="*,Auto">
                                    <Label Text="Explore Zambian Food" FontFamily="ArchivoSemiBold"
                                           FontSize="14" TextColor="{StaticResource TzSurface}"/>
                                    <Label Grid.Column="1" Text="→" FontSize="15"
                                           TextColor="{StaticResource TzSurface}"/>
                                </Grid>
                                <Border.GestureRecognizers>
                                    <TapGestureRecognizer Command="{Binding OpenExploreCommand}"/>
                                </Border.GestureRecognizers>
                            </Border>
                        </VerticalStackLayout>
                    </Grid>
                </Border>

                <!-- Stats -->
                <HorizontalStackLayout Padding="24,14,20,0" Spacing="20">
                    <HorizontalStackLayout Spacing="4">
                        <Label Text="120+" FontFamily="NewsreaderSemiBold" FontSize="17" TextColor="{StaticResource TzGreenDeep}"/>
                        <Label Text="Recipes" FontFamily="ArchivoRegular" FontSize="11.5" TextColor="{StaticResource TzMuted}" VerticalOptions="End"/>
                    </HorizontalStackLayout>
                    <HorizontalStackLayout Spacing="4">
                        <Label Text="10" FontFamily="NewsreaderSemiBold" FontSize="17" TextColor="{StaticResource TzGreenDeep}"/>
                        <Label Text="Provinces" FontFamily="ArchivoRegular" FontSize="11.5" TextColor="{StaticResource TzMuted}" VerticalOptions="End"/>
                    </HorizontalStackLayout>
                    <HorizontalStackLayout Spacing="4">
                        <Label Text="45" FontFamily="NewsreaderSemiBold" FontSize="17" TextColor="{StaticResource TzGreenDeep}"/>
                        <Label Text="Ingredients" FontFamily="ArchivoRegular" FontSize="11.5" TextColor="{StaticResource TzMuted}" VerticalOptions="End"/>
                    </HorizontalStackLayout>
                </HorizontalStackLayout>

                <!-- Food Categories rail -->
                <controls:SectionHeader Margin="20,26,20,13" TitleText="Food Categories"
                                        ActionText="View all" ActionCommand="{Binding OpenExploreCommand}"/>
                <CollectionView ItemsSource="{Binding Categories}" HeightRequest="136">
                    <CollectionView.ItemsLayout>
                        <LinearItemsLayout Orientation="Horizontal" ItemSpacing="11"/>
                    </CollectionView.ItemsLayout>
                    <CollectionView.ItemTemplate>
                        <DataTemplate x:DataType="models:Category">
                            <Grid WidthRequest="112" HeightRequest="136">
                                <controls:PhotoOrPlaceholder ImageAsset="{Binding ImageAsset}"
                                                             CornerRadius="16" ShowCaption="False"/>
                                <Border StrokeThickness="0" StrokeShape="RoundRectangle 16" Padding="0"
                                        InputTransparent="True">
                                    <BoxView>
                                        <BoxView.Background>
                                            <LinearGradientBrush StartPoint="0,1" EndPoint="0,0">
                                                <GradientStop Color="#DB0E2018" Offset="0.0"/>
                                                <GradientStop Color="#0F0E2018" Offset="0.7"/>
                                            </LinearGradientBrush>
                                        </BoxView.Background>
                                    </BoxView>
                                </Border>
                                <Label Text="{Binding Name}" Margin="11" VerticalOptions="End"
                                       FontFamily="ArchivoSemiBold" FontSize="12.5" LineHeight="1.25"
                                       TextColor="{StaticResource TzSurface}"/>
                            </Grid>
                        </DataTemplate>
                    </CollectionView.ItemTemplate>
                </CollectionView>

                <!-- Popular dishes rail -->
                <controls:SectionHeader Margin="20,26,20,13" Kicker="Heirloom flavours"
                                        TitleText="Popular Zambian Dishes"
                                        ActionText="View all" ActionCommand="{Binding OpenExploreCommand}"/>
                <CollectionView ItemsSource="{Binding Dishes}" HeightRequest="252">
                    <CollectionView.ItemsLayout>
                        <LinearItemsLayout Orientation="Horizontal" ItemSpacing="14"/>
                    </CollectionView.ItemsLayout>
                    <CollectionView.ItemTemplate>
                        <DataTemplate x:DataType="items:DishItemViewModel">
                            <VerticalStackLayout WidthRequest="236" Spacing="0">
                                <Grid HeightRequest="158">
                                    <controls:PhotoOrPlaceholder ImageAsset="{Binding ImageAsset}"
                                                                 Caption="{Binding PhotoCaption}"
                                                                 CornerRadius="18"/>
                                    <Border BackgroundColor="#B80E2018" StrokeThickness="0"
                                            StrokeShape="RoundRectangle 20" Padding="10,5"
                                            Margin="11" HorizontalOptions="Start" VerticalOptions="Start">
                                        <Label Text="{Binding Region}" FontFamily="ArchivoMedium"
                                               FontSize="9.5" TextColor="{StaticResource TzSurface}"/>
                                    </Border>
                                    <controls:CircleButton Glyph="{Binding SaveGlyph}" Diameter="30"
                                                           Margin="9" HorizontalOptions="End" VerticalOptions="Start"
                                                           GlyphColor="{Binding SaveColorHex, Converter={StaticResource HexToColor}}"
                                                           Command="{Binding ToggleSaveCommand}"/>
                                </Grid>
                                <Label Text="{Binding Title}" Margin="2,11,2,0"
                                       FontFamily="NewsreaderSemiBold" FontSize="21" LineHeight="1.15"
                                       TextColor="{StaticResource TzInk}"/>
                                <Label Text="{Binding Subtitle}" Margin="2,3,2,0"
                                       FontFamily="ArchivoRegular" FontSize="12" LineHeight="1.4"
                                       TextColor="{StaticResource TzMuted}"/>
                                <Label Text="{Binding MetaLabel}" Margin="2,8,2,0"
                                       FontFamily="ArchivoMedium" FontSize="11"
                                       TextColor="{StaticResource TzGreenMid}"/>
                                <VerticalStackLayout.GestureRecognizers>
                                    <TapGestureRecognizer Command="{Binding OpenCommand}"/>
                                </VerticalStackLayout.GestureRecognizers>
                            </VerticalStackLayout>
                        </DataTemplate>
                    </CollectionView.ItemTemplate>
                </CollectionView>

                <!-- Food stories panel -->
                <Border Style="{StaticResource CreamPanel}" Margin="20,28,20,0">
                    <VerticalStackLayout Spacing="0">
                        <Label Text="Food stories" Style="{StaticResource KickerLabel}"
                               TextColor="{StaticResource TzGreenMid}" Margin="0,0,0,12"/>
                        <VerticalStackLayout BindableLayout.ItemsSource="{Binding Stories}">
                            <BindableLayout.ItemTemplate>
                                <DataTemplate x:DataType="vm:StoryTeaser">
                                    <Grid ColumnDefinitions="Auto,*" ColumnSpacing="13" Padding="0,11">
                                        <BoxView Grid.ColumnSpan="2" HeightRequest="1" VerticalOptions="Start"
                                                 Color="{StaticResource TzRule}" Margin="0,-11,0,0"/>
                                        <controls:PhotoOrPlaceholder WidthRequest="52" HeightRequest="52"
                                                                     CornerRadius="12" ShowCaption="False"/>
                                        <VerticalStackLayout Grid.Column="1" Spacing="4">
                                            <Label Text="{Binding Title}" FontFamily="NewsreaderSemiBold"
                                                   FontSize="16" LineHeight="1.25" TextColor="{StaticResource TzInk}"/>
                                            <Label Text="{Binding Meta}" Style="{StaticResource MetaText}"/>
                                        </VerticalStackLayout>
                                        <Grid.GestureRecognizers>
                                            <TapGestureRecognizer Command="{Binding Source={RelativeSource AncestorType={x:Type vm:HomeViewModel}}, Path=OpenCultureCommand}"/>
                                        </Grid.GestureRecognizers>
                                    </Grid>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                        </VerticalStackLayout>
                    </VerticalStackLayout>
                </Border>

                <BoxView HeightRequest="26" Color="Transparent"/>
            </VerticalStackLayout>
        </ScrollView>

        <controls:BottomNavBar Grid.Row="1" ActiveSection="home"/>
    </Grid>
</ContentPage>
```

The two rails are `CollectionView`s laid out **horizontally** inside a vertical `ScrollView` — that is safe, because their scroll axis differs from the page's. The Food stories list is a `BindableLayout`, not a `CollectionView`, precisely because it scrolls on the same axis as the page.

- [ ] **Step 7: Write the code-behind**

`TasteZambia.Mobile/Views/HomePage.xaml.cs`:

```csharp
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
```

- [ ] **Step 8: Register the page and ViewModel**

In `MauiProgram.cs`, below the service registrations:

```csharp
builder.Services.AddTransient<HomeViewModel>();
builder.Services.AddTransient<HomePage>();
```

with `using TasteZambia.Core.ViewModels;` and `using TasteZambia.Mobile.Views;`.

- [ ] **Step 9: Verify against the design**

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Then launch and compare side by side with frame 01 of the design canvas. Check specifically:
- Hero is 340pt with the gradient reading dark at the bottom and nearly clear at the top.
- The gold CTA is `#8A5A12` and nothing else on the screen is gold except "View all" (also `#8A5A12`).
- Dish names are serif; English subtitles are sans and `TzMuted`.
- Kapenta, Inkoko, Kandolo, Munkoyo and Delele cards show stripes with their captions — not blank boxes.
- Tapping a heart on Home, then navigating to Explore, shows the same heart state (proves the singleton favourites service).

- [ ] **Step 10: Commit**

```bash
git add TasteZambia.Core/ViewModels TasteZambia.Mobile/Views/HomePage.xaml TasteZambia.Mobile/Views/HomePage.xaml.cs TasteZambia.Mobile/MauiProgram.cs TasteZambia.Core.Tests/ViewModels
git commit -m "feat(mobile): implement Home screen"
```

---

## Task 8: Explore and Search screen (frame 02)

**Layout:** deep-green search header (kicker, 28pt serif title, white 14pt-radius search field with a clear "×" when there is a query) · horizontal filter chip rail · result count + "Sort" row · vertical list of 96pt result rows · empty state when nothing matches · 26pt spacer.

**Files:**
- Create: `TasteZambia.Core/ViewModels/ExploreViewModel.cs`
- Create: `TasteZambia.Mobile/Views/ExplorePage.xaml` + `.xaml.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/ExploreViewModelTests.cs`

**Interfaces:**
- Consumes: `ICatalogService`, `IFavouritesService`, `IPreferenceService`, `INavigationService`, `DishItemViewModel`.
- Produces: `ExploreViewModel` — `Query`, `HasQuery`, `ResultCountLabel`, `NoResults`, `Chips` (`FilterChipViewModel`), `Results`, `ClearQueryCommand`.
- `FilterChipViewModel` — `Label`, `IsSelected`, `SelectCommand`.

- [ ] **Step 1: Write the failing test**

`TasteZambia.Core.Tests/ViewModels/ExploreViewModelTests.cs`:

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class ExploreViewModelTests
{
    private static ExploreViewModel Sut() => new(
        new CatalogService(new InMemoryDishRepository()),
        new FavouritesService(),
        new PreferenceService(),
        new StubNavigation());

    private sealed class StubNavigation : INavigationService
    {
        public Task GoToAsync(string route) => Task.CompletedTask;
        public Task GoToAsync(string route, IDictionary<string, object> p) => Task.CompletedTask;
        public Task GoBackAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task Initialize_ShowsEveryDishAndEightChipsWithAllSelected()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal(8, vm.Results.Count);
        Assert.Equal(8, vm.Chips.Count);
        Assert.Equal("All", vm.Chips[0].Label);
        Assert.True(vm.Chips[0].IsSelected);
        Assert.False(vm.HasQuery);
    }

    [Fact]
    public async Task ResultCountLabel_IsSingularForOneResult()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Query = "ifisashi";
        await vm.WaitForSearchAsync();

        Assert.Equal("1 recipe", vm.ResultCountLabel);
        Assert.True(vm.HasQuery);
    }

    [Fact]
    public async Task ResultCountLabel_IsPluralOtherwise()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        Assert.Equal("8 recipes", vm.ResultCountLabel);
    }

    [Fact]
    public async Task NoMatch_SetsNoResults()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Query = "sushi";
        await vm.WaitForSearchAsync();

        Assert.True(vm.NoResults);
        Assert.Equal("0 recipes", vm.ResultCountLabel);
    }

    [Fact]
    public async Task ClearQuery_RestoresTheFullList()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        vm.Query = "sushi";
        await vm.WaitForSearchAsync();

        vm.ClearQueryCommand.Execute(null);
        await vm.WaitForSearchAsync();

        Assert.Equal("", vm.Query);
        Assert.Equal(8, vm.Results.Count);
        Assert.False(vm.NoResults);
    }

    [Fact]
    public async Task SelectingAChip_MovesTheSelection()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Chips[2].SelectCommand.Execute(null);

        Assert.False(vm.Chips[0].IsSelected);
        Assert.True(vm.Chips[2].IsSelected);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~ExploreViewModelTests`
Expected: FAIL — `ExploreViewModel` does not exist.

- [ ] **Step 3: Write `ExploreViewModel`**

`TasteZambia.Core/ViewModels/ExploreViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels.Items;

namespace TasteZambia.Core.ViewModels;

public sealed partial class FilterChipViewModel(string label, Action<FilterChipViewModel> onSelect)
    : ObservableObject
{
    public string Label { get; } = label;

    [ObservableProperty]
    private bool _isSelected;

    [RelayCommand]
    private void Select() => onSelect(this);
}

public sealed partial class ExploreViewModel(
    ICatalogService catalog,
    IFavouritesService favourites,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    private Task _search = Task.CompletedTask;

    public ObservableCollection<FilterChipViewModel> Chips { get; } = [];
    public ObservableCollection<DishItemViewModel> Results { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQuery))]
    private string _query = "";

    [ObservableProperty]
    private string _resultCountLabel = "";

    [ObservableProperty]
    private bool _noResults;

    private string _filter = "All";

    public bool HasQuery => Query.Trim().Length > 0;

    public override async Task InitializeAsync()
    {
        if (Chips.Count == 0)
        {
            foreach (var label in SeedData.Filters)
                Chips.Add(new FilterChipViewModel(label, SelectChip));

            Chips[0].IsSelected = true;
        }

        await RunSearchAsync();
    }

    partial void OnQueryChanged(string value) => _search = RunSearchAsync();

    /// <summary>Lets tests await the search kicked off by setting <see cref="Query"/>.</summary>
    public Task WaitForSearchAsync() => _search;

    private void SelectChip(FilterChipViewModel chip)
    {
        foreach (var c in Chips)
            c.IsSelected = ReferenceEquals(c, chip);

        _filter = chip.Label;
        _search = RunSearchAsync();
    }

    private async Task RunSearchAsync()
    {
        var dishes = await catalog.SearchAsync(Query, _filter);

        Results.Clear();
        foreach (var dish in dishes)
            Results.Add(new DishItemViewModel(dish, favourites, preferences, Navigation));

        NoResults = Results.Count == 0;
        ResultCountLabel = $"{Results.Count} {(Results.Count == 1 ? "recipe" : "recipes")}";
    }

    [RelayCommand]
    private void ClearQuery() => Query = "";
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~ExploreViewModelTests`
Expected: PASS, 6 tests.

- [ ] **Step 5: Write `ExplorePage.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TasteZambia.Mobile.Controls"
             xmlns:vm="clr-namespace:TasteZambia.Core.ViewModels;assembly=TasteZambia.Core"
             xmlns:items="clr-namespace:TasteZambia.Core.ViewModels.Items;assembly=TasteZambia.Core"
             x:Class="TasteZambia.Mobile.Views.ExplorePage"
             x:DataType="vm:ExploreViewModel">

    <Grid RowDefinitions="Auto,Auto,Auto,*,Auto">

        <!-- Deep green search header -->
        <VerticalStackLayout Grid.Row="0" Padding="20,18,20,14"
                             BackgroundColor="{StaticResource TzGreenDeep}" Spacing="0">
            <Label Text="Terroir and lineage" Style="{StaticResource KickerLabel}"
                   TextColor="{StaticResource TzGoldLight}" Margin="0,0,0,9"/>
            <Label Text="Explore Zambian Food" FontFamily="NewsreaderMedium" FontSize="28"
                   LineHeight="1.1" TextColor="{StaticResource TzSurface}" Margin="0,0,0,14"/>

            <Border BackgroundColor="{StaticResource TzSurface}" StrokeThickness="0"
                    StrokeShape="RoundRectangle 14" Padding="14,12">
                <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="10">
                    <Label Text="⌕" FontSize="15" TextColor="{StaticResource TzMuted}" VerticalOptions="Center"/>
                    <Entry Grid.Column="1" Text="{Binding Query}"
                           Placeholder="Search Zambian dishes, ingredients or recipes..."
                           FontFamily="ArchivoRegular" FontSize="13"
                           TextColor="{StaticResource TzInk}"
                           PlaceholderColor="{StaticResource TzMuted2}"
                           BackgroundColor="Transparent"/>
                    <Label Grid.Column="2" Text="✕" FontSize="15"
                           TextColor="{StaticResource TzFaint}" VerticalOptions="Center"
                           IsVisible="{Binding HasQuery}">
                        <Label.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding ClearQueryCommand}"/>
                        </Label.GestureRecognizers>
                    </Label>
                </Grid>
            </Border>
        </VerticalStackLayout>

        <!-- Filter chips -->
        <CollectionView Grid.Row="1" ItemsSource="{Binding Chips}" HeightRequest="46" Margin="20,14,0,4">
            <CollectionView.ItemsLayout>
                <LinearItemsLayout Orientation="Horizontal" ItemSpacing="8"/>
            </CollectionView.ItemsLayout>
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="vm:FilterChipViewModel">
                    <Border Padding="13,8" StrokeShape="RoundRectangle 20" StrokeThickness="1"
                            VerticalOptions="Start">
                        <Border.Triggers>
                            <DataTrigger TargetType="Border" Binding="{Binding IsSelected}" Value="True">
                                <Setter Property="BackgroundColor" Value="{StaticResource TzGreenDeep}"/>
                                <Setter Property="Stroke" Value="{StaticResource TzGreenDeep}"/>
                            </DataTrigger>
                            <DataTrigger TargetType="Border" Binding="{Binding IsSelected}" Value="False">
                                <Setter Property="BackgroundColor" Value="{StaticResource TzSurface}"/>
                                <Setter Property="Stroke" Value="{StaticResource TzBorder}"/>
                            </DataTrigger>
                        </Border.Triggers>
                        <Label Text="{Binding Label}" FontFamily="ArchivoMedium" FontSize="11.5">
                            <Label.Triggers>
                                <DataTrigger TargetType="Label" Binding="{Binding IsSelected}" Value="True">
                                    <Setter Property="TextColor" Value="{StaticResource TzSurface}"/>
                                </DataTrigger>
                                <DataTrigger TargetType="Label" Binding="{Binding IsSelected}" Value="False">
                                    <Setter Property="TextColor" Value="{StaticResource TzBody}"/>
                                </DataTrigger>
                            </Label.Triggers>
                        </Label>
                        <Border.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding SelectCommand}"/>
                        </Border.GestureRecognizers>
                    </Border>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>

        <!-- Count + sort -->
        <Grid Grid.Row="2" Padding="20,16,20,4" ColumnDefinitions="*,Auto">
            <Label Text="{Binding ResultCountLabel}" FontFamily="PlexMonoMedium" FontSize="11"
                   TextColor="{StaticResource TzMuted2}"/>
            <Label Grid.Column="1" Text="Sort" FontFamily="ArchivoMedium" FontSize="11"
                   TextColor="{StaticResource TzGold}"/>
        </Grid>

        <!-- Results -->
        <CollectionView Grid.Row="3" ItemsSource="{Binding Results}" Margin="20,10,20,0">
            <CollectionView.ItemsLayout>
                <LinearItemsLayout Orientation="Vertical" ItemSpacing="14"/>
            </CollectionView.ItemsLayout>
            <CollectionView.EmptyView>
                <Label Text="Nothing in the archive matches that yet.&#10;Try a region, an ingredient, or a local name."
                       Padding="10,40" HorizontalTextAlignment="Center"
                       FontFamily="ArchivoRegular" FontSize="13" LineHeight="1.6"
                       TextColor="{StaticResource TzMuted2}"/>
            </CollectionView.EmptyView>
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="items:DishItemViewModel">
                    <Border Style="{StaticResource Card}" Padding="12">
                        <Grid ColumnDefinitions="Auto,*" ColumnSpacing="14">
                            <controls:PhotoOrPlaceholder WidthRequest="96" HeightRequest="96"
                                                         CornerRadius="15" ShowCaption="False"
                                                         ImageAsset="{Binding ImageAsset}"/>

                            <VerticalStackLayout Grid.Column="1" Spacing="0">
                                <Grid ColumnDefinitions="*,Auto" ColumnSpacing="8">
                                    <VerticalStackLayout Spacing="2">
                                        <Label Text="{Binding Title}" FontFamily="NewsreaderSemiBold"
                                               FontSize="19" LineHeight="1.15" TextColor="{StaticResource TzInk}"/>
                                        <Label Text="{Binding Subtitle}" FontFamily="ArchivoRegular"
                                               FontSize="11.5" LineHeight="1.3" TextColor="{StaticResource TzMuted}"/>
                                    </VerticalStackLayout>
                                    <Label Grid.Column="1" Text="{Binding SaveGlyph}" FontSize="15"
                                           TextColor="{Binding SaveColorHex, Converter={StaticResource HexToColor}}"
                                           VerticalOptions="Start">
                                        <Label.GestureRecognizers>
                                            <TapGestureRecognizer Command="{Binding ToggleSaveCommand}"/>
                                        </Label.GestureRecognizers>
                                    </Label>
                                </Grid>

                                <Label Text="{Binding Description}" Margin="0,6,0,0" MaxLines="2"
                                       LineBreakMode="TailTruncation"
                                       FontFamily="ArchivoRegular" FontSize="11.5" LineHeight="1.45"
                                       TextColor="{StaticResource TzBodyMuted}"/>

                                <HorizontalStackLayout Spacing="8" Margin="0,8,0,0">
                                    <controls:PillLabel Text="{Binding TimeLabel}"
                                                        PillBackground="{StaticResource TzGreenTint}"
                                                        TextColor="{StaticResource TzGreenMid}"/>
                                    <controls:PillLabel Text="{Binding Difficulty}"
                                                        PillBackground="{StaticResource TzGoldTint}"
                                                        TextColor="{StaticResource TzGoldTintText}"/>
                                    <Label Text="{Binding Region}" FontFamily="ArchivoRegular" FontSize="10"
                                           LineHeight="1.4" TextColor="{StaticResource TzFaint}"
                                           LineBreakMode="TailTruncation" VerticalOptions="Center"/>
                                </HorizontalStackLayout>
                            </VerticalStackLayout>
                        </Grid>

                        <Border.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding OpenCommand}"/>
                        </Border.GestureRecognizers>
                    </Border>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>

        <controls:BottomNavBar Grid.Row="4" ActiveSection="explore"/>
    </Grid>
</ContentPage>
```

- [ ] **Step 6: Write the code-behind and register**

`ExplorePage.xaml.cs` follows the exact shape of `HomePage.xaml.cs` (constructor takes `ExploreViewModel`, assigns `BindingContext`, `OnAppearing` awaits `InitializeAsync`). Add to `MauiProgram.cs`:

```csharp
builder.Services.AddTransient<ExploreViewModel>();
builder.Services.AddTransient<ExplorePage>();
```

- [ ] **Step 7: Verify against the design**

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Launch and compare with frame 02. Check: typing "chikanda" narrows to one row and the count reads "1 recipe"; the "×" appears only while there is text; typing "sushi" shows the two-line empty state; tapping a chip moves the dark fill; hearts match whatever Home shows.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat(mobile): implement Explore and Search screen"
```

---

## Task 9: Recipe screen and the ingredient bottom sheet (frame 03)

The longest screen in the design and the only one with an overlay. **Layout:** 322pt hero photo with back/save circles, "VERIFIED BY THE ARCHIVE" badge and a 40pt serif title · four-column Prep/Cook/Difficulty/Region strip · cream "Cultural context" panel with three paragraphs · ingredient table where linked rows are green with a dotted underline · numbered, tappable cooking steps that tint green when done · deep-green method switcher (Traditional / Modern) · regional variations list · dashed contributor card · 26pt spacer. Tapping a linked ingredient slides up a bottom sheet over a scrim.

**Files:**
- Create: `TasteZambia.Core/ViewModels/RecipeViewModel.cs`
- Create: `TasteZambia.Mobile/Views/RecipePage.xaml` + `.xaml.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/RecipeViewModelTests.cs`

**Interfaces:**
- Consumes: `IDishRepository`, `IIngredientRepository`, `IFavouritesService`, `ICookingProgressService`, `IPreferenceService`, `INavigationService`.
- Produces: `RecipeViewModel` — `DishId` (query property), `Title`, `Subtitle`, `IsVerified`, `PrepTime`, `CookTime`, `Difficulty`, `Region`, `HeroAsset`, `CulturalContext`, `Ingredients` (`RecipeIngredientItemViewModel`), `Steps` (`CookingStepItemViewModel`), `StepProgressLabel`, `IsTraditional`, `MethodHeading`, `MethodParagraphs`, `Variations`, `ContributorName`, `ContributorAvatar`, `IsSaved`/`SaveGlyph`/`SaveColorHex`, `Sheet` (`IngredientSheetViewModel?`), commands `ToggleSave`, `Back`, `ShowTraditional`, `ShowModern`, `CloseSheet`, `OpenFullIngredient`.

- [ ] **Step 1: Write the failing test**

`TasteZambia.Core.Tests/ViewModels/RecipeViewModelTests.cs`:

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class RecipeViewModelTests
{
    private sealed class StubNavigation : INavigationService
    {
        public List<string> Routes { get; } = [];
        public int BackCount { get; private set; }
        public Task GoToAsync(string route) { Routes.Add(route); return Task.CompletedTask; }
        public Task GoToAsync(string route, IDictionary<string, object> p) { Routes.Add(route); return Task.CompletedTask; }
        public Task GoBackAsync() { BackCount++; return Task.CompletedTask; }
    }

    private static RecipeViewModel Sut(INavigationService? nav = null,
                                       ICookingProgressService? progress = null) => new(
        new InMemoryDishRepository(),
        new InMemoryIngredientRepository(),
        new FavouritesService(),
        progress ?? new CookingProgressService(),
        new PreferenceService(),
        nav ?? new StubNavigation()) { DishId = "ifisashi" };

    [Fact]
    public async Task Initialize_LoadsTheIfisashiRecipe()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal("Ifisashi", vm.Title);
        Assert.Equal("Traditional Zambian vegetable dish", vm.Subtitle);
        Assert.True(vm.IsVerified);
        Assert.Equal("20 min", vm.PrepTime);
        Assert.Equal("30 min", vm.CookTime);
        Assert.Equal(6, vm.Ingredients.Count);
        Assert.Equal(4, vm.Steps.Count);
        Assert.Equal(4, vm.Variations.Count);
    }

    [Fact]
    public async Task OnlyIngredientsInTheArchiveAreLinked()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.True(vm.Ingredients[0].IsLinked);   // Chibwabwa
        Assert.True(vm.Ingredients[1].IsLinked);   // Mbalala
        Assert.False(vm.Ingredients[2].IsLinked);  // Onion
        Assert.False(vm.Ingredients[4].IsLinked);  // Salt
    }

    [Fact]
    public async Task StepProgressLabel_CountsCompletedSteps()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal("0 of 4 steps done", vm.StepProgressLabel);

        vm.Steps[1].ToggleCommand.Execute(null);

        Assert.True(vm.Steps[1].IsDone);
        Assert.Equal("1 of 4 steps done", vm.StepProgressLabel);
    }

    [Fact]
    public async Task MethodSwitcher_DefaultsToTraditionalAndSwaps()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.True(vm.IsTraditional);
        Assert.Equal("Over charcoal, in a clay pot", vm.MethodHeading);

        vm.ShowModernCommand.Execute(null);

        Assert.False(vm.IsTraditional);
        Assert.Equal("In a flat you rent abroad", vm.MethodHeading);
        Assert.Equal(3, vm.MethodParagraphs.Count);
    }

    [Fact]
    public async Task TappingALinkedIngredient_OpensTheSheet()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Ingredients[0].OpenCommand.Execute(null);

        Assert.NotNull(vm.Sheet);
        Assert.Equal("Chibwabwa", vm.Sheet!.Name);
        Assert.Equal("Pumpkin leaves", vm.Sheet.EnglishName);
        Assert.Single(vm.Sheet.LocalNames);
        Assert.Equal(3, vm.Sheet.UsedIn.Count);

        vm.CloseSheetCommand.Execute(null);
        Assert.Null(vm.Sheet);
    }

    [Fact]
    public async Task OpenFullIngredient_ClosesTheSheetAndNavigates()
    {
        var nav = new StubNavigation();
        var vm = Sut(nav);
        await vm.InitializeAsync();
        vm.Ingredients[0].OpenCommand.Execute(null);

        await vm.OpenFullIngredientCommand.ExecuteAsync(null);

        Assert.Null(vm.Sheet);
        Assert.Equal("ingredient", nav.Routes.Single());
    }

    [Fact]
    public async Task Back_PopsTheNavigationStack()
    {
        var nav = new StubNavigation();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        await vm.BackCommand.ExecuteAsync(null);

        Assert.Equal(1, nav.BackCount);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~RecipeViewModelTests`
Expected: FAIL — `RecipeViewModel` does not exist.

- [ ] **Step 3: Write the item ViewModels and `RecipeViewModel`**

`TasteZambia.Core/ViewModels/RecipeViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class RecipeIngredientItemViewModel(
    RecipeIngredient ingredient, Action<string> openSheet) : ObservableObject
{
    public string Name { get; } = ingredient.DisplayName;
    public string Subtitle { get; } = ingredient.DisplaySubtitle;
    public string Quantity { get; } = ingredient.Quantity;
    public bool IsLinked { get; } = ingredient.IsLinked;
    public bool IsPlain => !IsLinked;

    [RelayCommand]
    private void Open()
    {
        if (ingredient.IngredientKey is { } key)
            openSheet(key);
    }
}

public sealed partial class CookingStepItemViewModel : ObservableObject
{
    private readonly ICookingProgressService _progress;
    private readonly string _dishId;
    private readonly Action _notifyParent;

    public int Number { get; }
    public string NumberLabel { get; }
    public string StepTitle { get; }
    public string Body { get; }

    public CookingStepItemViewModel(CookingStep step, string dishId,
                                    ICookingProgressService progress, Action notifyParent)
    {
        _progress = progress;
        _dishId = dishId;
        _notifyParent = notifyParent;

        Number = step.Number;
        NumberLabel = step.Number.ToString();
        StepTitle = step.Title;
        Body = step.Body;
        _isDone = progress.IsDone(dishId, step.Number);
    }

    [ObservableProperty]
    private bool _isDone;

    [RelayCommand]
    private void Toggle()
    {
        _progress.Toggle(_dishId, Number);
        IsDone = _progress.IsDone(_dishId, Number);
        _notifyParent();
    }
}

public sealed record LocalNameRow(string Language, string Name);
public sealed record UsedInRow(string Name, string Subtitle);

public sealed class IngredientSheetViewModel(Ingredient ingredient, IReadOnlyList<UsedInRow> usedIn)
{
    public string Key { get; } = ingredient.Key;
    public string Name { get; } = ingredient.LocalName;
    public string EnglishName { get; } = ingredient.EnglishName;
    public string Description { get; } = ingredient.Description;
    public string WhereFound { get; } = ingredient.WhereFound;
    public string TraditionalPreparation { get; } = ingredient.TraditionalPreparation;
    public IReadOnlyList<LocalNameRow> LocalNames { get; } =
        ingredient.LocalNames.Select(l => new LocalNameRow(l.Language, l.Name)).ToList();
    public IReadOnlyList<UsedInRow> UsedIn { get; } = usedIn;
}

public sealed partial class RecipeViewModel(
    IDishRepository dishes,
    IIngredientRepository ingredients,
    IFavouritesService favourites,
    ICookingProgressService progress,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    private RecipeDetail? _recipe;

    /// <summary>Set from the navigation query string. Defaults to the one seeded recipe.</summary>
    public string DishId { get; set; } = "ifisashi";

    public ObservableCollection<string> CulturalContext { get; } = [];
    public ObservableCollection<RecipeIngredientItemViewModel> Ingredients { get; } = [];
    public ObservableCollection<CookingStepItemViewModel> Steps { get; } = [];
    public ObservableCollection<RegionalVariation> Variations { get; } = [];
    public ObservableCollection<string> MethodParagraphs { get; } = [];

    [ObservableProperty] private string _subtitle = "";
    [ObservableProperty] private bool _isVerified;
    [ObservableProperty] private string _prepTime = "";
    [ObservableProperty] private string _cookTime = "";
    [ObservableProperty] private string _difficulty = "";
    [ObservableProperty] private string _region = "";
    [ObservableProperty] private string? _heroAsset;
    [ObservableProperty] private string _heroCaption = "";
    [ObservableProperty] private string _stepProgressLabel = "";
    [ObservableProperty] private string _methodHeading = "";
    [ObservableProperty] private string _contributorName = "";
    [ObservableProperty] private string _contributorAvatar = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModern))]
    private bool _isTraditional = true;

    public bool IsModern => !IsTraditional;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveGlyph))]
    [NotifyPropertyChangedFor(nameof(SaveColorHex))]
    private bool _isSaved;

    public string SaveGlyph => IsSaved ? "♥" : "♡";
    public string SaveColorHex => IsSaved ? "#A3452A" : "#4A3D2E";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSheetOpen))]
    private IngredientSheetViewModel? _sheet;

    public bool IsSheetOpen => Sheet is not null;

    public override async Task InitializeAsync()
    {
        if (_recipe is not null) return;

        _recipe = await dishes.GetRecipeAsync(DishId);
        if (_recipe is null) return;

        var dish = _recipe.Dish;
        var name = preferences.Resolve(dish.LocalName, dish.EnglishName);

        Title = name.Title;
        Subtitle = _recipe.Subtitle;
        IsVerified = _recipe.IsVerified && preferences.ShowVerificationBadge;
        PrepTime = dish.PrepTime ?? "—";
        CookTime = dish.CookTime ?? "—";
        Difficulty = dish.Difficulty;
        Region = dish.Region;
        HeroAsset = dish.ImageAsset;
        HeroCaption = dish.PhotoNeededCaption;
        IsSaved = favourites.IsSaved(dish.Id);
        ContributorName = $"{_recipe.Contributor.Name}, {_recipe.Contributor.Location}";
        ContributorAvatar = _recipe.Contributor.AvatarAsset ?? "";

        foreach (var paragraph in _recipe.CulturalContext)
            CulturalContext.Add(paragraph);

        foreach (var ingredient in _recipe.Ingredients)
            Ingredients.Add(new RecipeIngredientItemViewModel(ingredient, OpenSheet));

        foreach (var step in _recipe.Steps)
            Steps.Add(new CookingStepItemViewModel(step, dish.Id, progress, RefreshStepProgress));

        foreach (var variation in _recipe.Variations)
            Variations.Add(variation);

        RefreshStepProgress();
        ApplyMethod();
    }

    private void RefreshStepProgress()
    {
        var done = progress.CompletedCount(DishId, Steps.Select(s => s.Number));
        StepProgressLabel = $"{done} of {Steps.Count} steps done";
    }

    private void ApplyMethod()
    {
        if (_recipe is null) return;

        var method = IsTraditional ? _recipe.TraditionalMethod : _recipe.ModernMethod;
        MethodHeading = method.Heading;

        MethodParagraphs.Clear();
        foreach (var paragraph in method.Paragraphs)
            MethodParagraphs.Add(paragraph);
    }

    private async void OpenSheet(string ingredientKey)
    {
        var ingredient = await ingredients.GetByKeyAsync(ingredientKey);
        if (ingredient is null) return;

        var rows = new List<UsedInRow>();
        foreach (var id in ingredient.UsedInDishIds)
        {
            if (await dishes.GetByIdAsync(id) is { } dish)
                rows.Add(new UsedInRow(dish.LocalName, dish.EnglishName));
        }

        Sheet = new IngredientSheetViewModel(ingredient, rows);
    }

    [RelayCommand]
    private void ToggleSave()
    {
        favourites.Toggle(DishId);
        IsSaved = favourites.IsSaved(DishId);
    }

    [RelayCommand]
    private void ShowTraditional() { IsTraditional = true; ApplyMethod(); }

    [RelayCommand]
    private void ShowModern() { IsTraditional = false; ApplyMethod(); }

    [RelayCommand]
    private void CloseSheet() => Sheet = null;

    [RelayCommand]
    private async Task OpenFullIngredient()
    {
        var key = Sheet?.Key;
        Sheet = null;

        if (key is not null)
            await Navigation.GoToAsync("ingredient", new Dictionary<string, object> { ["key"] = key });
    }

    [RelayCommand]
    private Task Back() => Navigation.GoBackAsync();
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~RecipeViewModelTests`
Expected: PASS, 7 tests.

- [ ] **Step 5: Write `RecipePage.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TasteZambia.Mobile.Controls"
             xmlns:vm="clr-namespace:TasteZambia.Core.ViewModels;assembly=TasteZambia.Core"
             xmlns:models="clr-namespace:TasteZambia.Core.Models;assembly=TasteZambia.Core"
             xmlns:sys="clr-namespace:System;assembly=netstandard"
             x:Class="TasteZambia.Mobile.Views.RecipePage"
             x:DataType="vm:RecipeViewModel">

    <Grid RowDefinitions="*,Auto">

        <ScrollView Grid.Row="0">
            <VerticalStackLayout Spacing="0">

                <!-- Hero -->
                <Grid HeightRequest="322">
                    <controls:PhotoOrPlaceholder ImageAsset="{Binding HeroAsset}"
                                                 Caption="{Binding HeroCaption}" CornerRadius="0"/>
                    <BoxView>
                        <BoxView.Background>
                            <LinearGradientBrush StartPoint="0,1" EndPoint="0,0">
                                <GradientStop Color="#E60E2018" Offset="0.0"/>
                                <GradientStop Color="#1A0E2018" Offset="0.55"/>
                                <GradientStop Color="#590E2018" Offset="1.0"/>
                            </LinearGradientBrush>
                        </BoxView.Background>
                    </BoxView>

                    <Grid Margin="16" VerticalOptions="Start" ColumnDefinitions="Auto,*,Auto">
                        <controls:CircleButton Glyph="←" Command="{Binding BackCommand}"
                                               GlyphColor="{StaticResource TzInk}"/>
                        <controls:CircleButton Grid.Column="2" Glyph="{Binding SaveGlyph}"
                                               Command="{Binding ToggleSaveCommand}"
                                               GlyphColor="{Binding SaveColorHex, Converter={StaticResource HexToColor}}"/>
                    </Grid>

                    <VerticalStackLayout Padding="20" VerticalOptions="End" Spacing="0">
                        <Border IsVisible="{Binding IsVerified}" HorizontalOptions="Start"
                                BackgroundColor="#33E8BD77" Stroke="#80E8BD77" StrokeThickness="1"
                                StrokeShape="RoundRectangle 20" Padding="11,5" Margin="0,0,0,11">
                            <Label Text="VERIFIED BY THE ARCHIVE" FontFamily="ArchivoMedium"
                                   FontSize="9.5" CharacterSpacing="0.38"
                                   TextColor="{StaticResource TzGoldLight}"/>
                        </Border>
                        <Label Text="{Binding Title}" Style="{StaticResource HeroTitle}"
                               CharacterSpacing="-0.6"/>
                        <Label Text="{Binding Subtitle}" Margin="0,7,0,0"
                               FontFamily="ArchivoRegular" FontSize="13.5" LineHeight="1.4"
                               TextColor="{StaticResource TzOnDark82}"/>
                    </VerticalStackLayout>
                </Grid>

                <!-- Prep / Cook / Difficulty / Region -->
                <Grid Padding="20,16" ColumnDefinitions="*,*,*,*"
                      BackgroundColor="{StaticResource TzSurface}">
                    <VerticalStackLayout Spacing="6">
                        <Label Text="Prep" Style="{StaticResource MicroLabel}" CharacterSpacing="0.85"/>
                        <Label Text="{Binding PrepTime}" FontFamily="ArchivoSemiBold" FontSize="13" TextColor="{StaticResource TzInk}"/>
                    </VerticalStackLayout>
                    <VerticalStackLayout Grid.Column="1" Spacing="6">
                        <Label Text="Cook" Style="{StaticResource MicroLabel}" CharacterSpacing="0.85"/>
                        <Label Text="{Binding CookTime}" FontFamily="ArchivoSemiBold" FontSize="13" TextColor="{StaticResource TzInk}"/>
                    </VerticalStackLayout>
                    <VerticalStackLayout Grid.Column="2" Spacing="6">
                        <Label Text="Difficulty" Style="{StaticResource MicroLabel}" CharacterSpacing="0.85"/>
                        <Label Text="{Binding Difficulty}" FontFamily="ArchivoSemiBold" FontSize="13" TextColor="{StaticResource TzInk}"/>
                    </VerticalStackLayout>
                    <VerticalStackLayout Grid.Column="3" Spacing="6">
                        <Label Text="Region" Style="{StaticResource MicroLabel}" CharacterSpacing="0.85"/>
                        <Label Text="{Binding Region}" FontFamily="ArchivoSemiBold" FontSize="13" TextColor="{StaticResource TzInk}"/>
                    </VerticalStackLayout>
                </Grid>
                <BoxView HeightRequest="1" Color="{StaticResource TzHairline}"/>

                <!-- Cultural context -->
                <Border Style="{StaticResource CreamPanel}" Margin="20">
                    <VerticalStackLayout Spacing="12">
                        <Label Text="Cultural context" Style="{StaticResource KickerLabel}"
                               TextColor="{StaticResource TzGreenMid}"/>
                        <Label Text="The Story Behind This Dish" FontFamily="NewsreaderSemiBold"
                               FontSize="23" LineHeight="1.2" TextColor="{StaticResource TzInk}"/>
                        <VerticalStackLayout Spacing="12" BindableLayout.ItemsSource="{Binding CulturalContext}">
                            <BindableLayout.ItemTemplate>
                                <DataTemplate x:DataType="sys:String">
                                    <Label Text="{Binding}" FontFamily="ArchivoRegular" FontSize="13.5"
                                           LineHeight="1.7" TextColor="{StaticResource TzBody}"/>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                        </VerticalStackLayout>
                    </VerticalStackLayout>
                </Border>

                <!-- Ingredients -->
                <VerticalStackLayout Padding="20,0" Spacing="0">
                    <Grid ColumnDefinitions="*,Auto" Margin="0,0,0,5">
                        <Label Text="Ingredients" Style="{StaticResource SectionTitle}"/>
                        <Label Grid.Column="1" Text="6 items" Style="{StaticResource MonoMeta}" VerticalOptions="End"/>
                    </Grid>
                    <Label Text="Tap an underlined ingredient to learn what it is, its local names, and where it grows."
                           FontFamily="ArchivoRegular" FontSize="11.5" LineHeight="1.5"
                           TextColor="{StaticResource TzMuted}" Margin="0,0,0,12"/>

                    <Border Stroke="{StaticResource TzHairline}" StrokeThickness="1"
                            StrokeShape="RoundRectangle 18" Padding="0" BackgroundColor="{StaticResource TzSurface}">
                        <VerticalStackLayout BindableLayout.ItemsSource="{Binding Ingredients}">
                            <BindableLayout.ItemTemplate>
                                <DataTemplate x:DataType="vm:RecipeIngredientItemViewModel">
                                    <Grid Padding="16,14" ColumnDefinitions="*,Auto" ColumnSpacing="10">
                                        <VerticalStackLayout Spacing="3">
                                            <VerticalStackLayout Spacing="1" IsVisible="{Binding IsLinked}" HorizontalOptions="Start">
                                                <Label Text="{Binding Name}" FontFamily="ArchivoSemiBold"
                                                       FontSize="14" LineHeight="1.2"
                                                       TextColor="{StaticResource TzGreenDeep}"/>
                                                <BoxView HeightRequest="1.5" Color="{StaticResource TzGreenMid}"/>
                                            </VerticalStackLayout>
                                            <Label Text="{Binding Name}" IsVisible="{Binding IsPlain}"
                                                   FontFamily="ArchivoMedium" FontSize="14" LineHeight="1.2"
                                                   TextColor="{StaticResource TzInk}"/>
                                            <Label Text="{Binding Subtitle}" FontFamily="ArchivoRegular"
                                                   FontSize="11" LineHeight="1.3" TextColor="{StaticResource TzMuted2}"/>
                                        </VerticalStackLayout>
                                        <Label Grid.Column="1" Text="{Binding Quantity}"
                                               FontFamily="PlexMonoRegular" FontSize="11.5"
                                               TextColor="{StaticResource TzBodyMuted}"
                                               HorizontalTextAlignment="End" VerticalOptions="Center"/>
                                        <BoxView Grid.ColumnSpan="2" HeightRequest="1" VerticalOptions="End"
                                                 Margin="0,0,0,-14" Color="{StaticResource TzHairlineSoft}"/>
                                        <Grid.GestureRecognizers>
                                            <TapGestureRecognizer Command="{Binding OpenCommand}"/>
                                        </Grid.GestureRecognizers>
                                    </Grid>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                        </VerticalStackLayout>
                    </Border>
                </VerticalStackLayout>

                <!-- Cooking steps -->
                <VerticalStackLayout Padding="20,26,20,0" Spacing="12">
                    <Grid ColumnDefinitions="*,Auto">
                        <Label Text="Cooking Instructions" Style="{StaticResource SectionTitle}"/>
                        <Label Grid.Column="1" Text="{Binding StepProgressLabel}" VerticalOptions="End"
                               FontFamily="PlexMonoMedium" FontSize="10.5" TextColor="{StaticResource TzGreenMid}"/>
                    </Grid>

                    <VerticalStackLayout Spacing="10" BindableLayout.ItemsSource="{Binding Steps}">
                        <BindableLayout.ItemTemplate>
                            <DataTemplate x:DataType="vm:CookingStepItemViewModel">
                                <Border Stroke="{StaticResource TzHairline}" StrokeThickness="1"
                                        StrokeShape="RoundRectangle 18" Padding="15">
                                    <Border.Triggers>
                                        <DataTrigger TargetType="Border" Binding="{Binding IsDone}" Value="True">
                                            <Setter Property="BackgroundColor" Value="{StaticResource TzGreenTint}"/>
                                        </DataTrigger>
                                        <DataTrigger TargetType="Border" Binding="{Binding IsDone}" Value="False">
                                            <Setter Property="BackgroundColor" Value="{StaticResource TzSurface}"/>
                                        </DataTrigger>
                                    </Border.Triggers>

                                    <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="13">
                                        <Border WidthRequest="30" HeightRequest="30" StrokeThickness="0"
                                                StrokeShape="RoundRectangle 9" Padding="0" VerticalOptions="Start">
                                            <Border.Triggers>
                                                <DataTrigger TargetType="Border" Binding="{Binding IsDone}" Value="True">
                                                    <Setter Property="BackgroundColor" Value="{StaticResource TzGreenMid}"/>
                                                </DataTrigger>
                                                <DataTrigger TargetType="Border" Binding="{Binding IsDone}" Value="False">
                                                    <Setter Property="BackgroundColor" Value="{StaticResource TzCream}"/>
                                                </DataTrigger>
                                            </Border.Triggers>
                                            <Label Text="{Binding NumberLabel}" FontFamily="ArchivoSemiBold" FontSize="13"
                                                   HorizontalOptions="Center" VerticalOptions="Center">
                                                <Label.Triggers>
                                                    <DataTrigger TargetType="Label" Binding="{Binding IsDone}" Value="True">
                                                        <Setter Property="TextColor" Value="{StaticResource TzSurface}"/>
                                                    </DataTrigger>
                                                    <DataTrigger TargetType="Label" Binding="{Binding IsDone}" Value="False">
                                                        <Setter Property="TextColor" Value="{StaticResource TzGreenDeep}"/>
                                                    </DataTrigger>
                                                </Label.Triggers>
                                            </Label>
                                        </Border>

                                        <VerticalStackLayout Grid.Column="1" Spacing="5">
                                            <Label Text="{Binding StepTitle}" FontFamily="ArchivoSemiBold"
                                                   FontSize="14" LineHeight="1.25">
                                                <Label.Triggers>
                                                    <DataTrigger TargetType="Label" Binding="{Binding IsDone}" Value="True">
                                                        <Setter Property="TextColor" Value="{StaticResource TzMuted}"/>
                                                    </DataTrigger>
                                                    <DataTrigger TargetType="Label" Binding="{Binding IsDone}" Value="False">
                                                        <Setter Property="TextColor" Value="{StaticResource TzInk}"/>
                                                    </DataTrigger>
                                                </Label.Triggers>
                                            </Label>
                                            <Label Text="{Binding Body}" FontFamily="ArchivoRegular" FontSize="12.5"
                                                   LineHeight="1.6" TextColor="{StaticResource TzBodyMuted}"/>
                                        </VerticalStackLayout>

                                        <Border Grid.Column="2" WidthRequest="21" HeightRequest="21" Padding="0"
                                                StrokeThickness="1.5" StrokeShape="RoundRectangle 6" VerticalOptions="Start">
                                            <Border.Triggers>
                                                <DataTrigger TargetType="Border" Binding="{Binding IsDone}" Value="True">
                                                    <Setter Property="Stroke" Value="{StaticResource TzGreenMid}"/>
                                                    <Setter Property="BackgroundColor" Value="{StaticResource TzGreenMid}"/>
                                                </DataTrigger>
                                                <DataTrigger TargetType="Border" Binding="{Binding IsDone}" Value="False">
                                                    <Setter Property="Stroke" Value="{StaticResource TzBorderDashed}"/>
                                                    <Setter Property="BackgroundColor" Value="Transparent"/>
                                                </DataTrigger>
                                            </Border.Triggers>
                                            <Label Text="✓" FontSize="12" IsVisible="{Binding IsDone}"
                                                   TextColor="{StaticResource TzSurface}"
                                                   HorizontalOptions="Center" VerticalOptions="Center"/>
                                        </Border>
                                    </Grid>

                                    <Border.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding ToggleCommand}"/>
                                    </Border.GestureRecognizers>
                                </Border>
                            </DataTemplate>
                        </BindableLayout.ItemTemplate>
                    </VerticalStackLayout>
                </VerticalStackLayout>

                <!-- Method switcher -->
                <Border Margin="20,26,20,0" BackgroundColor="{StaticResource TzGreenDeep}"
                        StrokeThickness="0" StrokeShape="RoundRectangle 20" Padding="0">
                    <VerticalStackLayout Spacing="0">
                        <Border Margin="14,14,14,0" BackgroundColor="{StaticResource TzOnDark10}"
                                StrokeThickness="0" StrokeShape="RoundRectangle 12" Padding="6">
                            <Grid ColumnDefinitions="*,*" ColumnSpacing="4">
                                <Border StrokeThickness="0" StrokeShape="RoundRectangle 9" Padding="9">
                                    <Border.Triggers>
                                        <DataTrigger TargetType="Border" Binding="{Binding IsTraditional}" Value="True">
                                            <Setter Property="BackgroundColor" Value="{StaticResource TzGreenDeep}"/>
                                        </DataTrigger>
                                    </Border.Triggers>
                                    <Label Text="The Traditional Way" FontFamily="ArchivoSemiBold" FontSize="12"
                                           HorizontalOptions="Center">
                                        <Label.Triggers>
                                            <DataTrigger TargetType="Label" Binding="{Binding IsTraditional}" Value="True">
                                                <Setter Property="TextColor" Value="{StaticResource TzSurface}"/>
                                            </DataTrigger>
                                            <DataTrigger TargetType="Label" Binding="{Binding IsTraditional}" Value="False">
                                                <Setter Property="TextColor" Value="{StaticResource TzBodyMuted}"/>
                                            </DataTrigger>
                                        </Label.Triggers>
                                    </Label>
                                    <Border.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding ShowTraditionalCommand}"/>
                                    </Border.GestureRecognizers>
                                </Border>

                                <Border Grid.Column="1" StrokeThickness="0" StrokeShape="RoundRectangle 9" Padding="9">
                                    <Border.Triggers>
                                        <DataTrigger TargetType="Border" Binding="{Binding IsModern}" Value="True">
                                            <Setter Property="BackgroundColor" Value="{StaticResource TzGreenDeep}"/>
                                        </DataTrigger>
                                    </Border.Triggers>
                                    <Label Text="Modern Kitchen" FontFamily="ArchivoSemiBold" FontSize="12"
                                           HorizontalOptions="Center">
                                        <Label.Triggers>
                                            <DataTrigger TargetType="Label" Binding="{Binding IsModern}" Value="True">
                                                <Setter Property="TextColor" Value="{StaticResource TzSurface}"/>
                                            </DataTrigger>
                                            <DataTrigger TargetType="Label" Binding="{Binding IsModern}" Value="False">
                                                <Setter Property="TextColor" Value="{StaticResource TzBodyMuted}"/>
                                            </DataTrigger>
                                        </Label.Triggers>
                                    </Label>
                                    <Border.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding ShowModernCommand}"/>
                                    </Border.GestureRecognizers>
                                </Border>
                            </Grid>
                        </Border>

                        <VerticalStackLayout Padding="18" Spacing="11">
                            <Label Text="{Binding MethodHeading}" FontFamily="NewsreaderMedium" FontSize="21"
                                   TextColor="{StaticResource TzSurface}" Margin="0,0,0,1"/>
                            <VerticalStackLayout Spacing="11" BindableLayout.ItemsSource="{Binding MethodParagraphs}">
                                <BindableLayout.ItemTemplate>
                                    <DataTemplate x:DataType="sys:String">
                                        <Label Text="{Binding}" FontFamily="ArchivoRegular" FontSize="12.5"
                                               LineHeight="1.65" TextColor="{StaticResource TzOnDark84}"/>
                                    </DataTemplate>
                                </BindableLayout.ItemTemplate>
                            </VerticalStackLayout>
                        </VerticalStackLayout>
                    </VerticalStackLayout>
                </Border>

                <!-- Regional variations -->
                <VerticalStackLayout Padding="20,26,20,0" Spacing="0">
                    <Label Text="Regional Variations" Style="{StaticResource SectionTitle}" Margin="0,0,0,4"/>
                    <Label Text="The same dish, cooked differently across the country."
                           FontFamily="ArchivoRegular" FontSize="11.5" LineHeight="1.5"
                           TextColor="{StaticResource TzMuted}" Margin="0,0,0,14"/>
                    <VerticalStackLayout BindableLayout.ItemsSource="{Binding Variations}">
                        <BindableLayout.ItemTemplate>
                            <DataTemplate x:DataType="models:RegionalVariation">
                                <VerticalStackLayout Padding="0,14" Spacing="6">
                                    <BoxView HeightRequest="1" Color="{StaticResource TzRule}" Margin="0,-14,0,0"/>
                                    <Label Text="{Binding Place}" FontFamily="ArchivoSemiBold" FontSize="12.5"
                                           TextColor="{StaticResource TzClay}"/>
                                    <Label Text="{Binding Description}" FontFamily="ArchivoRegular" FontSize="12.5"
                                           LineHeight="1.6" TextColor="{StaticResource TzBody}"/>
                                </VerticalStackLayout>
                            </DataTemplate>
                        </BindableLayout.ItemTemplate>
                    </VerticalStackLayout>
                </VerticalStackLayout>

                <!-- Contributor -->
                <Border Margin="20,26,20,0" Stroke="{StaticResource TzBorderDashed}" StrokeThickness="1"
                        StrokeDashArray="4,3" StrokeShape="RoundRectangle 18" Padding="16"
                        BackgroundColor="Transparent">
                    <Grid ColumnDefinitions="Auto,*" ColumnSpacing="12">
                        <Border WidthRequest="40" HeightRequest="40" StrokeThickness="0"
                                StrokeShape="RoundRectangle 20" Padding="0">
                            <Image Source="{Binding ContributorAvatar}" Aspect="AspectFill"/>
                        </Border>
                        <VerticalStackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
                            <Label Text="Contributed by" Style="{StaticResource MetaText}" TextColor="{StaticResource TzMuted}"/>
                            <Label Text="{Binding ContributorName}" FontFamily="ArchivoSemiBold" FontSize="13"
                                   LineHeight="1.3" TextColor="{StaticResource TzInk}"/>
                        </VerticalStackLayout>
                    </Grid>
                </Border>

                <BoxView HeightRequest="26" Color="Transparent"/>
            </VerticalStackLayout>
        </ScrollView>

        <controls:BottomNavBar Grid.Row="1" ActiveSection="explore"/>

        <!-- Ingredient bottom sheet -->
        <Grid Grid.RowSpan="2" IsVisible="{Binding IsSheetOpen}">
            <BoxView Color="{StaticResource TzScrim}">
                <BoxView.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding CloseSheetCommand}"/>
                </BoxView.GestureRecognizers>
            </BoxView>

            <Border x:Name="Sheet" VerticalOptions="End" MaximumHeightRequest="634"
                    BackgroundColor="{StaticResource TzSurface}" StrokeThickness="0"
                    StrokeShape="RoundRectangle 26,26,0,0" Padding="20,0,20,24"
                    BindingContext="{Binding Sheet}" x:DataType="vm:IngredientSheetViewModel">
                <ScrollView>
                    <VerticalStackLayout Spacing="0">
                        <BoxView WidthRequest="38" HeightRequest="4" CornerRadius="2" Margin="0,14,0,14"
                                 HorizontalOptions="Center" Color="{StaticResource TzHairline}"/>

                        <Grid ColumnDefinitions="*,Auto" ColumnSpacing="10" Margin="0,0,0,12">
                            <VerticalStackLayout Spacing="4">
                                <Label Text="{Binding Name}" FontFamily="NewsreaderSemiBold" FontSize="29"
                                       LineHeight="1.05" TextColor="{StaticResource TzInk}"/>
                                <Label Text="{Binding EnglishName}" FontFamily="ArchivoRegular" FontSize="12.5"
                                       LineHeight="1.3" TextColor="{StaticResource TzMuted}"/>
                            </VerticalStackLayout>
                            <controls:CircleButton Grid.Column="1" Glyph="✕" Diameter="30" VerticalOptions="Start"
                                                   GlyphColor="{StaticResource TzBodyMuted}"
                                                   Command="{Binding Source={RelativeSource AncestorType={x:Type vm:RecipeViewModel}}, Path=CloseSheetCommand}"/>
                        </Grid>

                        <Label Text="{Binding Description}" Style="{StaticResource BodyText}" FontSize="13" Margin="0,0,0,16"/>

                        <Border BackgroundColor="{StaticResource TzCream}" StrokeThickness="0"
                                StrokeShape="RoundRectangle 14" Padding="15,14" Margin="0,0,0,14">
                            <VerticalStackLayout Spacing="0">
                                <Label Text="Local names" Style="{StaticResource MicroLabel}"
                                       TextColor="{StaticResource TzGreenMid}" Margin="0,0,0,10"/>
                                <VerticalStackLayout BindableLayout.ItemsSource="{Binding LocalNames}">
                                    <BindableLayout.ItemTemplate>
                                        <DataTemplate x:DataType="vm:LocalNameRow">
                                            <Grid ColumnDefinitions="*,Auto" Padding="0,4">
                                                <Label Text="{Binding Language}" FontFamily="ArchivoRegular"
                                                       FontSize="11.5" TextColor="{StaticResource TzMuted}"/>
                                                <Label Grid.Column="1" Text="{Binding Name}"
                                                       FontFamily="NewsreaderSemiBold" FontSize="15"
                                                       TextColor="{StaticResource TzInk}"/>
                                            </Grid>
                                        </DataTemplate>
                                    </BindableLayout.ItemTemplate>
                                </VerticalStackLayout>
                            </VerticalStackLayout>
                        </Border>

                        <Label Text="Where it grows" Style="{StaticResource MicroLabel}" Margin="0,0,0,7"/>
                        <Label Text="{Binding WhereFound}" Style="{StaticResource BodySmall}" Margin="0,0,0,14"/>

                        <Label Text="Traditionally prepared" Style="{StaticResource MicroLabel}" Margin="0,0,0,7"/>
                        <Label Text="{Binding TraditionalPreparation}" Style="{StaticResource BodySmall}" Margin="0,0,0,16"/>

                        <Label Text="Used in these dishes" Style="{StaticResource MicroLabel}" Margin="0,0,0,9"/>
                        <VerticalStackLayout Spacing="8" BindableLayout.ItemsSource="{Binding UsedIn}" Margin="0,0,0,16">
                            <BindableLayout.ItemTemplate>
                                <DataTemplate x:DataType="vm:UsedInRow">
                                    <Border Stroke="{StaticResource TzRule}" StrokeThickness="1"
                                            StrokeShape="RoundRectangle 12" Padding="12,10">
                                        <Grid ColumnDefinitions="Auto,*" ColumnSpacing="11">
                                            <controls:PhotoOrPlaceholder WidthRequest="32" HeightRequest="32"
                                                                         CornerRadius="9" ShowCaption="False"/>
                                            <VerticalStackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
                                                <Label Text="{Binding Name}" FontFamily="NewsreaderSemiBold"
                                                       FontSize="15" LineHeight="1.1" TextColor="{StaticResource TzInk}"/>
                                                <Label Text="{Binding Subtitle}" FontFamily="ArchivoRegular"
                                                       FontSize="10.5" LineHeight="1.3" TextColor="{StaticResource TzMuted2}"/>
                                            </VerticalStackLayout>
                                        </Grid>
                                    </Border>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                        </VerticalStackLayout>

                        <Border BackgroundColor="{StaticResource TzGreenDeep}" StrokeThickness="0"
                                StrokeShape="RoundRectangle 13" Padding="14">
                            <Label Text="Open full ingredient profile" FontFamily="ArchivoSemiBold" FontSize="13"
                                   TextColor="{StaticResource TzSurface}" HorizontalOptions="Center"/>
                            <Border.GestureRecognizers>
                                <TapGestureRecognizer Command="{Binding Source={RelativeSource AncestorType={x:Type vm:RecipeViewModel}}, Path=OpenFullIngredientCommand}"/>
                            </Border.GestureRecognizers>
                        </Border>
                    </VerticalStackLayout>
                </ScrollView>
            </Border>
        </Grid>
    </Grid>
</ContentPage>
```

- [ ] **Step 6: Write the code-behind with the sheet animation and query parameter**

`TasteZambia.Mobile/Views/RecipePage.xaml.cs`:

```csharp
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views;

[QueryProperty(nameof(DishId), "dishId")]
public partial class RecipePage : ContentPage
{
    private readonly RecipeViewModel _viewModel;

    public string DishId
    {
        get => _viewModel.DishId;
        set => _viewModel.DishId = value;
    }

    public RecipePage(RecipeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        _viewModel.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(RecipeViewModel.IsSheetOpen) && _viewModel.IsSheetOpen)
                await SlideSheetInAsync();
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async Task SlideSheetInAsync()
    {
        Sheet.TranslationY = 400;
        await Sheet.TranslateToAsync(0, 0, 260, Easing.CubicOut);
    }
}
```

`TranslateToAsync` — not `TranslateTo` — is the .NET 10 name.

- [ ] **Step 7: Register and verify**

```csharp
builder.Services.AddTransient<RecipeViewModel>();
builder.Services.AddTransient<RecipePage>();
```

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Launch, open Ifisashi from Explore, and compare with frame 03. Check: the verified badge sits above a 40pt serif title; tapping "Chibwabwa" (green, underlined) slides the sheet up over a scrim; tapping "Onion" does nothing; ticking step 2 turns its card `TzGreenTint` and the counter reads "1 of 4 steps done"; the method switcher swaps both heading and all three paragraphs; the bottom nav still shows **Explore** as active.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat(mobile): implement Recipe screen with ingredient bottom sheet"
```

---

## Task 10: Zambian Ingredients grid (frame 04)

**Layout:** clay kicker · 31pt serif title · intro paragraph · two-column grid of 112pt-tall image cards with serif local name and English subtitle · cream "contribute a name" panel · 26pt spacer.

**Files:**
- Create: `TasteZambia.Core/ViewModels/IngredientsViewModel.cs`
- Create: `TasteZambia.Mobile/Views/IngredientsPage.xaml` + `.xaml.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/IngredientsViewModelTests.cs`

**Interfaces:**
- Consumes: `IIngredientRepository`, `IPreferenceService`, `INavigationService`.
- Produces: `IngredientsViewModel` — `Items` (`IngredientTileViewModel`). `IngredientTileViewModel` — `Key`, `Title`, `Subtitle`, `ImageAsset`, `OpenCommand`.

- [ ] **Step 1: Write the failing test**

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class IngredientsViewModelTests
{
    private sealed class Nav : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoBackAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task Initialize_LoadsNineIngredientsWithLocalNamesAsTitles()
    {
        var vm = new IngredientsViewModel(new InMemoryIngredientRepository(), new PreferenceService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(9, vm.Items.Count);
        Assert.Equal("Chibwabwa", vm.Items[0].Title);
        Assert.Equal("Pumpkin leaves", vm.Items[0].Subtitle);
    }

    [Fact]
    public async Task OnlyChibwabwaHasPhotography()
    {
        var vm = new IngredientsViewModel(new InMemoryIngredientRepository(), new PreferenceService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal("market_ingredients.png", vm.Items[0].ImageAsset);
        Assert.All(vm.Items.Skip(1), i => Assert.Null(i.ImageAsset));
    }

    [Fact]
    public async Task OpeningATile_NavigatesToTheIngredientRoute()
    {
        var nav = new Nav();
        var vm = new IngredientsViewModel(new InMemoryIngredientRepository(), new PreferenceService(), nav);
        await vm.InitializeAsync();

        vm.Items[0].OpenCommand.Execute(null);

        Assert.Equal("ingredient", nav.Routes.Single());
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~IngredientsViewModelTests`
Expected: FAIL — `IngredientsViewModel` does not exist.

- [ ] **Step 3: Write the ViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class IngredientTileViewModel(
    Ingredient ingredient, IPreferenceService preferences, INavigationService navigation)
{
    private readonly DisplayName _name = preferences.Resolve(ingredient.LocalName, ingredient.EnglishName);

    public string Key { get; } = ingredient.Key;
    public string Title => _name.Title;
    public string Subtitle => _name.Subtitle;
    public string? ImageAsset { get; } = ingredient.ImageAsset;

    [RelayCommand]
    private Task Open()
        => navigation.GoToAsync("ingredient", new Dictionary<string, object> { ["key"] = Key });
}

public sealed class IngredientsViewModel(
    IIngredientRepository ingredients,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<IngredientTileViewModel> Items { get; } = [];

    public override async Task InitializeAsync()
    {
        if (Items.Count > 0) return;

        foreach (var ingredient in await ingredients.GetAllAsync())
            Items.Add(new IngredientTileViewModel(ingredient, preferences, Navigation));
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~IngredientsViewModelTests`
Expected: PASS, 3 tests.

- [ ] **Step 5: Write `IngredientsPage.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TasteZambia.Mobile.Controls"
             xmlns:vm="clr-namespace:TasteZambia.Core.ViewModels;assembly=TasteZambia.Core"
             x:Class="TasteZambia.Mobile.Views.IngredientsPage"
             x:DataType="vm:IngredientsViewModel">

    <Grid RowDefinitions="*,Auto">
        <ScrollView Grid.Row="0">
            <VerticalStackLayout Spacing="0">

                <VerticalStackLayout Padding="20,20,20,18" Spacing="9">
                    <Label Text="Botanical living archive" Style="{StaticResource KickerLabel}"/>
                    <Label Text="Zambian Ingredients" Style="{StaticResource DisplayTitle}" CharacterSpacing="-0.31"/>
                    <Label Text="A visual encyclopedia of what Zambian cooking is actually made from. Local names first, English underneath."
                           Style="{StaticResource BodyText}" TextColor="{StaticResource TzBodyMuted}"/>
                </VerticalStackLayout>

                <!-- Two-column grid. Vertical scroll is owned by the page's ScrollView,
                     so this uses BindableLayout over a FlexLayout, not a CollectionView. -->
                <FlexLayout Padding="20,0" Wrap="Wrap" JustifyContent="SpaceBetween"
                            BindableLayout.ItemsSource="{Binding Items}">
                    <BindableLayout.ItemTemplate>
                        <DataTemplate x:DataType="vm:IngredientTileViewModel">
                            <VerticalStackLayout WidthRequest="168" Margin="0,0,0,13" Spacing="0">
                                <controls:PhotoOrPlaceholder HeightRequest="112" CornerRadius="16"
                                                             ShowCaption="False" ImageAsset="{Binding ImageAsset}"/>
                                <Label Text="{Binding Title}" Margin="2,9,2,0"
                                       FontFamily="NewsreaderSemiBold" FontSize="18" LineHeight="1.15"
                                       TextColor="{StaticResource TzInk}"/>
                                <Label Text="{Binding Subtitle}" Margin="2,2,2,0"
                                       Style="{StaticResource EnglishSubtitle}"/>
                                <VerticalStackLayout.GestureRecognizers>
                                    <TapGestureRecognizer Command="{Binding OpenCommand}"/>
                                </VerticalStackLayout.GestureRecognizers>
                            </VerticalStackLayout>
                        </DataTemplate>
                    </BindableLayout.ItemTemplate>
                </FlexLayout>

                <Border Margin="20,22,20,0" BackgroundColor="{StaticResource TzCream}"
                        StrokeThickness="0" StrokeShape="RoundRectangle 16" Padding="16">
                    <Label FontFamily="ArchivoRegular" FontSize="12" LineHeight="1.6"
                           TextColor="{StaticResource TzBodyMuted}">
                        <Label.FormattedText>
                            <FormattedString>
                                <Span Text="Know a local name we are missing? Ingredient names in Tonga, Lozi, Kaonde, Lunda and Luvale are still incomplete. "/>
                                <Span Text="Contribute a name" FontFamily="ArchivoSemiBold"
                                      TextColor="{StaticResource TzGold}"/>
                            </FormattedString>
                        </Label.FormattedText>
                    </Label>
                </Border>

                <BoxView HeightRequest="26" Color="Transparent"/>
            </VerticalStackLayout>
        </ScrollView>

        <controls:BottomNavBar Grid.Row="1" ActiveSection="explore"/>
    </Grid>
</ContentPage>
```

Tile width is 168 = (390 − 40 padding − 13 gap) / 2, rounded down.

- [ ] **Step 6: Write the code-behind, register and verify**

`IngredientsPage.xaml.cs` follows `HomePage.xaml.cs`. Register:

```csharp
builder.Services.AddTransient<IngredientsViewModel>();
builder.Services.AddTransient<IngredientsPage>();
```

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Compare with frame 04: two even columns, only Chibwabwa photographed, the other eight striped; "Contribute a name" is the only gold text on the screen.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(mobile): implement Zambian Ingredients grid"
```

---

## Task 11: Ingredient detail (frame 05)

**Layout:** 262pt hero with back circle, gold kicker, 40pt serif local name and "English name: …" · description paragraph · deep-green "Local names" panel listing language/name pairs with a pending-languages footer and a gold "Suggest →" · "Where it is commonly found" · cream "How it is traditionally prepared" panel · "Used in These Dishes" list · 26pt spacer.

**Files:**
- Create: `TasteZambia.Core/ViewModels/IngredientViewModel.cs`
- Create: `TasteZambia.Mobile/Views/IngredientPage.xaml` + `.xaml.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/IngredientViewModelTests.cs`

**Interfaces:**
- Consumes: `IIngredientRepository`, `ICatalogService`, `IFavouritesService`, `IPreferenceService`, `INavigationService`, `DishItemViewModel`, `LocalNameRow`.
- Produces: `IngredientViewModel` — `Key` (query property), `Title`, `EnglishLine`, `Description`, `WhereFound`, `TraditionalPreparation`, `HeroAsset`, `LocalNames`, `PendingLanguages`, `UsedIn`, `BackCommand`.

- [ ] **Step 1: Write the failing test**

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class IngredientViewModelTests
{
    private sealed class Nav : INavigationService
    {
        public int BackCount { get; private set; }
        public Task GoToAsync(string r) => Task.CompletedTask;
        public Task GoToAsync(string r, IDictionary<string, object> p) => Task.CompletedTask;
        public Task GoBackAsync() { BackCount++; return Task.CompletedTask; }
    }

    private static IngredientViewModel Sut(Nav nav) => new(
        new InMemoryIngredientRepository(),
        new CatalogService(new InMemoryDishRepository()),
        new FavouritesService(), new PreferenceService(), nav) { Key = "chibwabwa" };

    [Fact]
    public async Task Initialize_LoadsChibwabwa()
    {
        var vm = Sut(new Nav());
        await vm.InitializeAsync();

        Assert.Equal("Chibwabwa", vm.Title);
        Assert.Equal("English name: Pumpkin leaves", vm.EnglishLine);
        Assert.Equal("market_ingredients.png", vm.HeroAsset);
        Assert.Equal("Tonga, Lozi, Kaonde, Lunda, Luvale", vm.PendingLanguages);
        Assert.Single(vm.LocalNames);
        Assert.Equal("Bemba, Nyanja", vm.LocalNames[0].Language);
    }

    [Fact]
    public async Task UsedIn_ResolvesDishesInTheOrderTheIngredientLists()
    {
        var vm = Sut(new Nav());
        await vm.InitializeAsync();

        Assert.Equal(["ifisashi", "nshima", "delele"], vm.UsedIn.Select(d => d.Id));
    }

    [Fact]
    public async Task Back_PopsTheStack()
    {
        var nav = new Nav();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        await vm.BackCommand.ExecuteAsync(null);

        Assert.Equal(1, nav.BackCount);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~IngredientViewModelTests`
Expected: FAIL — `IngredientViewModel` does not exist.

- [ ] **Step 3: Write the ViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels.Items;

namespace TasteZambia.Core.ViewModels;

public sealed partial class IngredientViewModel(
    IIngredientRepository ingredients,
    ICatalogService catalog,
    IFavouritesService favourites,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    /// <summary>Set from the navigation query string. Defaults to the one seeded profile.</summary>
    public string Key { get; set; } = "chibwabwa";

    public ObservableCollection<LocalNameRow> LocalNames { get; } = [];
    public ObservableCollection<DishItemViewModel> UsedIn { get; } = [];

    [ObservableProperty] private string _englishLine = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _whereFound = "";
    [ObservableProperty] private string _traditionalPreparation = "";
    [ObservableProperty] private string _pendingLanguages = "";
    [ObservableProperty] private string? _heroAsset;

    public override async Task InitializeAsync()
    {
        if (LocalNames.Count > 0 || UsedIn.Count > 0) return;

        var ingredient = await ingredients.GetByKeyAsync(Key);
        if (ingredient is null) return;

        var name = preferences.Resolve(ingredient.LocalName, ingredient.EnglishName);

        Title = name.Title;
        EnglishLine = $"English name: {name.Subtitle}";
        Description = ingredient.Description;
        WhereFound = ingredient.WhereFound;
        TraditionalPreparation = ingredient.TraditionalPreparation;
        PendingLanguages = ingredient.PendingLanguages;
        HeroAsset = ingredient.ImageAsset;

        foreach (var local in ingredient.LocalNames)
            LocalNames.Add(new LocalNameRow(local.Language, local.Name));

        foreach (var dish in await catalog.GetDishesByIdsAsync(ingredient.UsedInDishIds))
            UsedIn.Add(new DishItemViewModel(dish, favourites, preferences, Navigation));
    }

    [RelayCommand]
    private Task Back() => Navigation.GoBackAsync();
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~IngredientViewModelTests`
Expected: PASS, 3 tests.

- [ ] **Step 5: Write `IngredientPage.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TasteZambia.Mobile.Controls"
             xmlns:vm="clr-namespace:TasteZambia.Core.ViewModels;assembly=TasteZambia.Core"
             xmlns:items="clr-namespace:TasteZambia.Core.ViewModels.Items;assembly=TasteZambia.Core"
             x:Class="TasteZambia.Mobile.Views.IngredientPage"
             x:DataType="vm:IngredientViewModel">

    <Grid RowDefinitions="*,Auto">
        <ScrollView Grid.Row="0">
            <VerticalStackLayout Spacing="0">

                <Grid HeightRequest="262">
                    <controls:PhotoOrPlaceholder ImageAsset="{Binding HeroAsset}" CornerRadius="0" ShowCaption="False"/>
                    <BoxView>
                        <BoxView.Background>
                            <LinearGradientBrush StartPoint="0,1" EndPoint="0,0">
                                <GradientStop Color="#E00E2018" Offset="0.0"/>
                                <GradientStop Color="#0D0E2018" Offset="0.6"/>
                                <GradientStop Color="#4D0E2018" Offset="1.0"/>
                            </LinearGradientBrush>
                        </BoxView.Background>
                    </BoxView>

                    <controls:CircleButton Margin="16" HorizontalOptions="Start" VerticalOptions="Start"
                                           Glyph="←" GlyphColor="{StaticResource TzInk}"
                                           Command="{Binding BackCommand}"/>

                    <VerticalStackLayout Padding="20,0,20,18" VerticalOptions="End" Spacing="0">
                        <Label Text="Indigenous ingredient" Style="{StaticResource KickerLabel}"
                               TextColor="{StaticResource TzGoldLight}" Margin="0,0,0,9"/>
                        <Label Text="{Binding Title}" Style="{StaticResource HeroTitle}" CharacterSpacing="-0.6"/>
                        <Label Text="{Binding EnglishLine}" Margin="0,7,0,0"
                               FontFamily="ArchivoRegular" FontSize="13.5" LineHeight="1.4"
                               TextColor="{StaticResource TzOnDark82}"/>
                    </VerticalStackLayout>
                </Grid>

                <Label Text="{Binding Description}" Margin="20"
                       FontFamily="ArchivoRegular" FontSize="14" LineHeight="1.7"
                       TextColor="{StaticResource TzBody}"/>

                <!-- Local names, on deep green -->
                <Border Margin="20,0" BackgroundColor="{StaticResource TzGreenDeep}" StrokeThickness="0"
                        StrokeShape="RoundRectangle 18" Padding="18">
                    <VerticalStackLayout Spacing="0">
                        <Label Text="Local names" Style="{StaticResource MicroLabel}"
                               TextColor="{StaticResource TzGoldLight}" Margin="0,0,0,12"/>

                        <VerticalStackLayout BindableLayout.ItemsSource="{Binding LocalNames}">
                            <BindableLayout.ItemTemplate>
                                <DataTemplate x:DataType="vm:LocalNameRow">
                                    <Grid ColumnDefinitions="*,Auto" ColumnSpacing="10" Padding="0,7">
                                        <Label Text="{Binding Language}" FontFamily="ArchivoRegular" FontSize="11.5"
                                               LineHeight="1.3" TextColor="{StaticResource TzOnDark66}" VerticalOptions="End"/>
                                        <Label Grid.Column="1" Text="{Binding Name}" FontFamily="NewsreaderSemiBold"
                                               FontSize="19" TextColor="{StaticResource TzSurface}"/>
                                        <BoxView Grid.ColumnSpan="2" HeightRequest="1" VerticalOptions="End"
                                                 Margin="0,0,0,-7" Color="{StaticResource TzOnDark14}"/>
                                    </Grid>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                        </VerticalStackLayout>

                        <Grid ColumnDefinitions="*,Auto" ColumnSpacing="10" Margin="0,11,0,0">
                            <Label Text="{Binding PendingLanguages}" FontFamily="ArchivoRegular" FontSize="11"
                                   LineHeight="1.4" TextColor="{StaticResource TzOnDark50}"/>
                            <Label Grid.Column="1" Text="Suggest →" FontFamily="ArchivoSemiBold" FontSize="10.5"
                                   TextColor="{StaticResource TzGoldLight}" VerticalOptions="Center"/>
                        </Grid>
                    </VerticalStackLayout>
                </Border>

                <VerticalStackLayout Padding="20,24,20,0" Spacing="8">
                    <Label Text="Where it is commonly found" Style="{StaticResource MicroLabel}"/>
                    <Label Text="{Binding WhereFound}" Style="{StaticResource BodyText}"/>
                </VerticalStackLayout>

                <Border Margin="20,22,20,0" BackgroundColor="{StaticResource TzCream}" StrokeThickness="0"
                        StrokeShape="RoundRectangle 18" Padding="18">
                    <VerticalStackLayout Spacing="9">
                        <Label Text="How it is traditionally prepared" FontFamily="NewsreaderSemiBold"
                               FontSize="20" TextColor="{StaticResource TzInk}"/>
                        <Label Text="{Binding TraditionalPreparation}" FontFamily="ArchivoRegular"
                               FontSize="13" LineHeight="1.7" TextColor="{StaticResource TzBody}"/>
                    </VerticalStackLayout>
                </Border>

                <VerticalStackLayout Padding="20,24,20,0" Spacing="12">
                    <Label Text="Used in These Dishes" Style="{StaticResource SectionTitle}"/>
                    <VerticalStackLayout Spacing="11" BindableLayout.ItemsSource="{Binding UsedIn}">
                        <BindableLayout.ItemTemplate>
                            <DataTemplate x:DataType="items:DishItemViewModel">
                                <Border Style="{StaticResource Card}" StrokeShape="RoundRectangle 16" Padding="11">
                                    <Grid ColumnDefinitions="Auto,*" ColumnSpacing="12">
                                        <controls:PhotoOrPlaceholder WidthRequest="62" HeightRequest="62"
                                                                     CornerRadius="13" ShowCaption="False"
                                                                     ImageAsset="{Binding ImageAsset}"/>
                                        <VerticalStackLayout Grid.Column="1" Spacing="3" VerticalOptions="Center">
                                            <Label Text="{Binding Title}" Style="{StaticResource CardTitle}"/>
                                            <Label Text="{Binding Subtitle}" Style="{StaticResource EnglishSubtitle}"/>
                                            <Label Text="{Binding MetaLabel}" Margin="0,3,0,0"
                                                   FontFamily="ArchivoMedium" FontSize="10.5"
                                                   TextColor="{StaticResource TzGreenMid}"/>
                                        </VerticalStackLayout>
                                    </Grid>
                                    <Border.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding OpenCommand}"/>
                                    </Border.GestureRecognizers>
                                </Border>
                            </DataTemplate>
                        </BindableLayout.ItemTemplate>
                    </VerticalStackLayout>
                </VerticalStackLayout>

                <BoxView HeightRequest="26" Color="Transparent"/>
            </VerticalStackLayout>
        </ScrollView>

        <controls:BottomNavBar Grid.Row="1" ActiveSection="explore"/>
    </Grid>
</ContentPage>
```

- [ ] **Step 6: Write the code-behind, register and verify**

`IngredientPage.xaml.cs` mirrors `RecipePage.xaml.cs` minus the sheet animation:

```csharp
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views;

[QueryProperty(nameof(Key), "key")]
public partial class IngredientPage : ContentPage
{
    private readonly IngredientViewModel _viewModel;

    public string Key { get => _viewModel.Key; set => _viewModel.Key = value; }

    public IngredientPage(IngredientViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
```

Register:

```csharp
builder.Services.AddTransient<IngredientViewModel>();
builder.Services.AddTransient<IngredientPage>();
```

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Compare with frame 05. Check: the local-names panel is deep green with gold micro-label and gold "Suggest →"; the pending languages line is the faint `TzOnDark50`; three dish rows appear (Ifisashi photographed, Nshima photographed, Delele striped).

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(mobile): implement ingredient detail screen"
```

---

## Task 12: Explore by Region (frame 06)

**Layout:** kicker · 31pt serif title · intro · **196pt striped map placeholder** with its two-line caption (ships as a placeholder — see Known Gaps) · horizontal province chip rail, Northern selected by default · deep-green province panel (name + seat, blurb, "Common ingredients" chips, "Cooking tradition") · "Foods of X Province" list with chevrons · cream story teaser · 26pt spacer.

**Files:**
- Create: `TasteZambia.Core/ViewModels/RegionsViewModel.cs`
- Create: `TasteZambia.Mobile/Views/RegionsPage.xaml` + `.xaml.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/RegionsViewModelTests.cs`

**Interfaces:**
- Consumes: `IRegionRepository`, `IDishRepository`, `INavigationService`.
- Produces: `RegionsViewModel` — `Provinces` (`ProvinceChipViewModel`), `SelectedName`, `SelectedSeat`, `SelectedBlurb`, `SelectedTradition`, `SelectedIngredients`, `SelectedFoods` (`ProvinceFoodViewModel`), `FoodsHeading`. `ProvinceChipViewModel` — `Name`, `IsSelected`, `SelectCommand`. `ProvinceFoodViewModel` — `Name`, `Subtitle`, `HasSubtitle`, `OpenCommand`.

- [ ] **Step 1: Write the failing test**

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class RegionsViewModelTests
{
    private sealed class Nav : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoBackAsync() => Task.CompletedTask;
    }

    private static RegionsViewModel Sut(Nav? nav = null)
        => new(new InMemoryRegionRepository(), new InMemoryDishRepository(), nav ?? new Nav());

    [Fact]
    public async Task DefaultsToNorthern_MatchingTheDesign()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal(10, vm.Provinces.Count);
        Assert.Equal("Northern", vm.SelectedName);
        Assert.Equal("Kasama", vm.SelectedSeat);
        Assert.True(vm.Provinces[6].IsSelected);
        Assert.Equal("Foods of Northern Province", vm.FoodsHeading);
    }

    [Fact]
    public async Task SelectingAProvince_SwapsEveryDetailField()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Provinces[3].SelectCommand.Execute(null);

        Assert.Equal("Luapula", vm.SelectedName);
        Assert.Equal("Mansa", vm.SelectedSeat);
        Assert.False(vm.Provinces[6].IsSelected);
        Assert.True(vm.Provinces[3].IsSelected);
        Assert.Equal(3, vm.SelectedIngredients.Count);
        Assert.Equal("Foods of Luapula Province", vm.FoodsHeading);
    }

    [Fact]
    public async Task FoodsNotInTheDishArchive_StillGetTheirEnglishSubtitle()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        var katapa = vm.SelectedFoods.Single(f => f.Name == "Katapa");
        Assert.True(katapa.HasSubtitle);
        Assert.Equal("Cassava leaf relish", katapa.Subtitle);
    }

    [Fact]
    public async Task OpeningAFood_NavigatesToTheRecipeRoute()
    {
        var nav = new Nav();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        vm.SelectedFoods[0].OpenCommand.Execute(null);

        Assert.Equal("recipe", nav.Routes.Single());
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~RegionsViewModelTests`
Expected: FAIL — `RegionsViewModel` does not exist.

- [ ] **Step 3: Write the ViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class ProvinceChipViewModel(string name, Action<ProvinceChipViewModel> onSelect)
    : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty]
    private bool _isSelected;

    [RelayCommand]
    private void Select() => onSelect(this);
}

public sealed partial class ProvinceFoodViewModel(
    string name, string subtitle, INavigationService navigation, string? dishId)
{
    public string Name { get; } = name;
    public string Subtitle { get; } = subtitle;
    public bool HasSubtitle => Subtitle.Length > 0;

    [RelayCommand]
    private Task Open() => dishId is null
        ? Task.CompletedTask
        : navigation.GoToAsync("recipe", new Dictionary<string, object> { ["dishId"] = dishId });
}

public sealed partial class RegionsViewModel(
    IRegionRepository regions,
    IDishRepository dishes,
    INavigationService navigation) : BaseViewModel(navigation)
{
    private IReadOnlyList<Province> _all = [];

    public ObservableCollection<ProvinceChipViewModel> Provinces { get; } = [];
    public ObservableCollection<string> SelectedIngredients { get; } = [];
    public ObservableCollection<ProvinceFoodViewModel> SelectedFoods { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FoodsHeading))]
    private string _selectedName = "";

    [ObservableProperty] private string _selectedSeat = "";
    [ObservableProperty] private string _selectedBlurb = "";
    [ObservableProperty] private string _selectedTradition = "";

    public string FoodsHeading => $"Foods of {SelectedName} Province";

    public override async Task InitializeAsync()
    {
        if (Provinces.Count > 0) return;

        _all = await regions.GetAllAsync();

        foreach (var province in _all)
            Provinces.Add(new ProvinceChipViewModel(province.Name, SelectChip));

        // Index 6 is Northern — the design opens on it.
        await SelectAsync(Provinces[6]);
    }

    private async void SelectChip(ProvinceChipViewModel chip) => await SelectAsync(chip);

    private async Task SelectAsync(ProvinceChipViewModel chip)
    {
        foreach (var c in Provinces)
            c.IsSelected = ReferenceEquals(c, chip);

        var province = _all.First(p => p.Name == chip.Name);

        SelectedName = province.Name;
        SelectedSeat = province.Seat;
        SelectedBlurb = province.Blurb;
        SelectedTradition = province.CookingTradition;

        SelectedIngredients.Clear();
        foreach (var ingredient in province.CommonIngredients)
            SelectedIngredients.Add(ingredient);

        SelectedFoods.Clear();
        foreach (var food in province.SignatureFoods)
        {
            var dish = await dishes.GetAllAsync() is var all
                ? all.FirstOrDefault(d => d.LocalName == food)
                : null;

            var subtitle = dish?.EnglishName
                ?? (SeedData.ExtraFoodSubtitles.TryGetValue(food, out var extra) ? extra : "");

            SelectedFoods.Add(new ProvinceFoodViewModel(food, subtitle, Navigation, dish?.Id));
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~RegionsViewModelTests`
Expected: PASS, 4 tests.

- [ ] **Step 5: Write `RegionsPage.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TasteZambia.Mobile.Controls"
             xmlns:vm="clr-namespace:TasteZambia.Core.ViewModels;assembly=TasteZambia.Core"
             xmlns:sys="clr-namespace:System;assembly=netstandard"
             x:Class="TasteZambia.Mobile.Views.RegionsPage"
             x:DataType="vm:RegionsViewModel">

    <Grid RowDefinitions="*,Auto">
        <ScrollView Grid.Row="0">
            <VerticalStackLayout Spacing="0">

                <VerticalStackLayout Padding="20,20,20,16" Spacing="9">
                    <Label Text="Regional discovery" Style="{StaticResource KickerLabel}"/>
                    <Label Text="Taste Zambia by Region" Style="{StaticResource DisplayTitle}" CharacterSpacing="-0.31"/>
                    <Label Text="Ten provinces, each with its own staples, its own greens, and its own way of cooking them."
                           Style="{StaticResource BodyText}" TextColor="{StaticResource TzBodyMuted}"/>
                </VerticalStackLayout>

                <!-- KNOWN GAP: awaiting real provincial boundary data. Ships as a placeholder. -->
                <Border Margin="20,0" HeightRequest="196" StrokeThickness="0"
                        StrokeShape="RoundRectangle 20" Padding="0">
                    <Grid>
                        <controls:StripePlaceholder StripeWidth="7"/>
                        <VerticalStackLayout VerticalOptions="Center" HorizontalOptions="Center"
                                             Padding="20" Spacing="9">
                            <Label Text="stylized province map" FontFamily="PlexMonoMedium" FontSize="10.5"
                                   LineHeight="1.4" HorizontalTextAlignment="Center"
                                   TextColor="{StaticResource TzMuted}"/>
                            <Label Text="Awaiting real provincial boundary data. Not drawn by hand."
                                   MaximumWidthRequest="230" FontFamily="ArchivoRegular" FontSize="11"
                                   LineHeight="1.5" HorizontalTextAlignment="Center"
                                   TextColor="{StaticResource TzMuted2}"/>
                        </VerticalStackLayout>
                    </Grid>
                </Border>

                <CollectionView ItemsSource="{Binding Provinces}" HeightRequest="48" Margin="20,16,0,4">
                    <CollectionView.ItemsLayout>
                        <LinearItemsLayout Orientation="Horizontal" ItemSpacing="8"/>
                    </CollectionView.ItemsLayout>
                    <CollectionView.ItemTemplate>
                        <DataTemplate x:DataType="vm:ProvinceChipViewModel">
                            <Border Padding="14,9" StrokeShape="RoundRectangle 20" StrokeThickness="1"
                                    VerticalOptions="Start">
                                <Border.Triggers>
                                    <DataTrigger TargetType="Border" Binding="{Binding IsSelected}" Value="True">
                                        <Setter Property="BackgroundColor" Value="{StaticResource TzGreenDeep}"/>
                                        <Setter Property="Stroke" Value="{StaticResource TzGreenDeep}"/>
                                    </DataTrigger>
                                    <DataTrigger TargetType="Border" Binding="{Binding IsSelected}" Value="False">
                                        <Setter Property="BackgroundColor" Value="{StaticResource TzSurface}"/>
                                        <Setter Property="Stroke" Value="{StaticResource TzBorder}"/>
                                    </DataTrigger>
                                </Border.Triggers>
                                <Label Text="{Binding Name}" FontFamily="ArchivoMedium" FontSize="11.5">
                                    <Label.Triggers>
                                        <DataTrigger TargetType="Label" Binding="{Binding IsSelected}" Value="True">
                                            <Setter Property="TextColor" Value="{StaticResource TzSurface}"/>
                                        </DataTrigger>
                                        <DataTrigger TargetType="Label" Binding="{Binding IsSelected}" Value="False">
                                            <Setter Property="TextColor" Value="{StaticResource TzBody}"/>
                                        </DataTrigger>
                                    </Label.Triggers>
                                </Label>
                                <Border.GestureRecognizers>
                                    <TapGestureRecognizer Command="{Binding SelectCommand}"/>
                                </Border.GestureRecognizers>
                            </Border>
                        </DataTemplate>
                    </CollectionView.ItemTemplate>
                </CollectionView>

                <!-- Province panel -->
                <Border Style="{StaticResource DeepPanel}" Margin="20,16,20,0">
                    <VerticalStackLayout Spacing="0">
                        <Grid ColumnDefinitions="*,Auto" ColumnSpacing="10" Margin="0,0,0,10">
                            <Label Text="{Binding SelectedName}" FontFamily="NewsreaderMedium" FontSize="29"
                                   LineHeight="1.05" TextColor="{StaticResource TzSurface}"/>
                            <Label Grid.Column="1" Text="{Binding SelectedSeat}" VerticalOptions="End"
                                   FontFamily="PlexMonoRegular" FontSize="10.5"
                                   TextColor="{StaticResource TzGoldLight}"/>
                        </Grid>

                        <Label Text="{Binding SelectedBlurb}" FontFamily="ArchivoRegular" FontSize="13"
                               LineHeight="1.7" TextColor="{StaticResource TzOnDark84}"/>

                        <BoxView HeightRequest="1" Color="{StaticResource TzOnDark14}" Margin="0,18,0,16"/>
                        <Label Text="Common ingredients" Style="{StaticResource MicroLabel}"
                               TextColor="{StaticResource TzGoldLight}" Margin="0,0,0,11"/>
                        <FlexLayout Wrap="Wrap" BindableLayout.ItemsSource="{Binding SelectedIngredients}">
                            <BindableLayout.ItemTemplate>
                                <DataTemplate x:DataType="sys:String">
                                    <Border BackgroundColor="{StaticResource TzOnDark10}" StrokeThickness="0"
                                            StrokeShape="RoundRectangle 16" Padding="11,6" Margin="0,0,7,7">
                                        <Label Text="{Binding}" FontFamily="ArchivoMedium" FontSize="11"
                                               TextColor="{StaticResource TzOnDark86}"/>
                                    </Border>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                        </FlexLayout>

                        <BoxView HeightRequest="1" Color="{StaticResource TzOnDark14}" Margin="0,9,0,16"/>
                        <Label Text="Cooking tradition" Style="{StaticResource MicroLabel}"
                               TextColor="{StaticResource TzGoldLight}" Margin="0,0,0,9"/>
                        <Label Text="{Binding SelectedTradition}" FontFamily="ArchivoRegular" FontSize="12.5"
                               LineHeight="1.7" TextColor="{StaticResource TzOnDark84}"/>
                    </VerticalStackLayout>
                </Border>

                <!-- Foods of the province -->
                <VerticalStackLayout Padding="20,24,20,0" Spacing="12">
                    <Label Text="{Binding FoodsHeading}" Style="{StaticResource SubsectionTitle}"/>
                    <VerticalStackLayout Spacing="9" BindableLayout.ItemsSource="{Binding SelectedFoods}">
                        <BindableLayout.ItemTemplate>
                            <DataTemplate x:DataType="vm:ProvinceFoodViewModel">
                                <Border Style="{StaticResource Card}" StrokeShape="RoundRectangle 15" Padding="15,14">
                                    <Grid ColumnDefinitions="*,Auto" ColumnSpacing="12">
                                        <VerticalStackLayout Spacing="3">
                                            <Label Text="{Binding Name}" Style="{StaticResource CardTitle}"/>
                                            <Label Text="{Binding Subtitle}" IsVisible="{Binding HasSubtitle}"
                                                   Style="{StaticResource EnglishSubtitle}"
                                                   TextColor="{StaticResource TzMuted2}"/>
                                        </VerticalStackLayout>
                                        <Label Grid.Column="1" Text="›" FontSize="15" VerticalOptions="Center"
                                               TextColor="{StaticResource TzChevron}"/>
                                    </Grid>
                                    <Border.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding OpenCommand}"/>
                                    </Border.GestureRecognizers>
                                </Border>
                            </DataTemplate>
                        </BindableLayout.ItemTemplate>
                    </VerticalStackLayout>
                </VerticalStackLayout>

                <Border Margin="20,22,20,0" BackgroundColor="{StaticResource TzCream}" StrokeThickness="0"
                        StrokeShape="RoundRectangle 18" Padding="18">
                    <VerticalStackLayout Spacing="6">
                        <Label Text="Food stories from here" Style="{StaticResource MicroLabel}"
                               TextColor="{StaticResource TzGreenMid}" Margin="0,0,0,3"/>
                        <Label Text="Cooking Before the Modern Kitchen" FontFamily="NewsreaderSemiBold"
                               FontSize="19" LineHeight="1.2" TextColor="{StaticResource TzInk}"/>
                        <Label Text="Dr. Mutale Chileshe · 7 min read" FontFamily="ArchivoRegular"
                               FontSize="11.5" LineHeight="1.4" TextColor="{StaticResource TzMuted}"/>
                    </VerticalStackLayout>
                </Border>

                <BoxView HeightRequest="26" Color="Transparent"/>
            </VerticalStackLayout>
        </ScrollView>

        <controls:BottomNavBar Grid.Row="1" ActiveSection="regions"/>
    </Grid>
</ContentPage>
```

- [ ] **Step 6: Write the code-behind, register and verify**

`RegionsPage.xaml.cs` follows `HomePage.xaml.cs`. Register `RegionsViewModel` and `RegionsPage` as transient.

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Compare with frame 06. Check: opens on Northern; the map area is striped with both caption lines; tapping "Luapula" swaps the panel, chips, and the "Foods of Luapula Province" heading in one go; Katapa shows "Cassava leaf relish" even though it is not a seeded dish.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(mobile): implement Explore by Region screen"
```

---

## Task 13: Food Stories (frame 07)

**Layout:** kicker · 31pt serif title · intro · large lead article card (186pt striped image, kicker, 27pt serif title, lede, byline) · "More from the archive" list of five rows with 74pt striped thumbnails · deep-green "Record an elder telling it" panel with gold CTA · 26pt spacer.

**Files:**
- Create: `TasteZambia.Core/ViewModels/CultureViewModel.cs`
- Create: `TasteZambia.Mobile/Views/CulturePage.xaml` + `.xaml.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/CultureViewModelTests.cs`

**Interfaces:**
- Consumes: `IArticleRepository`, `INavigationService`.
- Produces: `CultureViewModel` — `LeadTitle`, `LeadLede`, `LeadByline`, `LeadCaption`, `OpenLeadCommand`, `Articles` (`ArticleRowViewModel`). `ArticleRowViewModel` — `Id`, `Kicker`, `TitleText`, `Byline`, `OpenCommand`.

- [ ] **Step 1: Write the failing test**

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class CultureViewModelTests
{
    private sealed class Nav : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoBackAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task LeadIsNshima_AndTheOtherFiveBecomeRows()
    {
        var vm = new CultureViewModel(new InMemoryArticleRepository(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal("The History of Nshima", vm.LeadTitle);
        Assert.Equal("The dish at the centre of every Zambian meal is younger than most people assume.", vm.LeadLede);
        Assert.Equal("Dr. Mutale Chileshe · 8 min read · Pan-Zambian", vm.LeadByline);
        Assert.Equal(5, vm.Articles.Count);
        Assert.Equal("groundnuts", vm.Articles[0].Id);
    }

    [Fact]
    public async Task OpeningAnArticle_NavigatesToTheStoryRoute()
    {
        var nav = new Nav();
        var vm = new CultureViewModel(new InMemoryArticleRepository(), nav);
        await vm.InitializeAsync();

        vm.Articles[0].OpenCommand.Execute(null);

        Assert.Equal("story", nav.Routes.Single());
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~CultureViewModelTests`
Expected: FAIL — `CultureViewModel` does not exist.

- [ ] **Step 3: Write the ViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class ArticleRowViewModel(Article article, INavigationService navigation)
{
    public string Id { get; } = article.Id;
    public string Kicker { get; } = article.Kicker;
    public string TitleText { get; } = article.Title;
    public string Byline { get; } = $"{article.Author} · {article.Meta}";
    public string PhotoCaption { get; } = article.PhotoNeededCaption;
    public string? ImageAsset { get; } = article.ImageAsset;

    [RelayCommand]
    private Task Open()
        => navigation.GoToAsync("story", new Dictionary<string, object> { ["articleId"] = Id });
}

public sealed partial class CultureViewModel(
    IArticleRepository articles,
    INavigationService navigation) : BaseViewModel(navigation)
{
    private string _leadId = "";

    public ObservableCollection<ArticleRowViewModel> Articles { get; } = [];

    [ObservableProperty] private string _leadKicker = "";
    [ObservableProperty] private string _leadTitle = "";
    [ObservableProperty] private string _leadLede = "";
    [ObservableProperty] private string _leadByline = "";
    [ObservableProperty] private string _leadCaption = "";

    public override async Task InitializeAsync()
    {
        if (Articles.Count > 0) return;

        var all = await articles.GetAllAsync();
        var lead = all.First(a => a.IsLead);

        _leadId = lead.Id;
        LeadKicker = lead.Kicker;
        LeadTitle = lead.Title;
        LeadLede = lead.Lede ?? "";
        LeadByline = $"{lead.Author} · {lead.Meta}";
        LeadCaption = lead.PhotoNeededCaption;

        foreach (var article in all.Where(a => !a.IsLead))
            Articles.Add(new ArticleRowViewModel(article, Navigation));
    }

    [RelayCommand]
    private Task OpenLead()
        => Navigation.GoToAsync("story", new Dictionary<string, object> { ["articleId"] = _leadId });
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~CultureViewModelTests`
Expected: PASS, 2 tests.

- [ ] **Step 5: Write `CulturePage.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TasteZambia.Mobile.Controls"
             xmlns:vm="clr-namespace:TasteZambia.Core.ViewModels;assembly=TasteZambia.Core"
             x:Class="TasteZambia.Mobile.Views.CulturePage"
             x:DataType="vm:CultureViewModel">

    <Grid RowDefinitions="*,Auto">
        <ScrollView Grid.Row="0">
            <VerticalStackLayout Spacing="0">

                <VerticalStackLayout Padding="20,20,20,16" Spacing="9">
                    <Label Text="Oral and written record" Style="{StaticResource KickerLabel}"/>
                    <Label Text="Food Stories" Style="{StaticResource DisplayTitle}" CharacterSpacing="-0.31"/>
                    <Label Text="What the recipes cannot hold: where a dish came from, who cooked it, and what it meant to eat it."
                           Style="{StaticResource BodyText}" TextColor="{StaticResource TzBodyMuted}"/>
                </VerticalStackLayout>

                <!-- Lead article -->
                <Border Style="{StaticResource Card}" Margin="20,0" StrokeShape="RoundRectangle 22" Padding="0">
                    <VerticalStackLayout Spacing="0">
                        <controls:PhotoOrPlaceholder HeightRequest="186" CornerRadius="0"
                                                     Caption="{Binding LeadCaption}"/>
                        <VerticalStackLayout Padding="18" Spacing="0">
                            <Label Text="{Binding LeadKicker}" Style="{StaticResource KickerLabel}"
                                   CharacterSpacing="1.26" TextColor="{StaticResource TzGold}" Margin="0,0,0,10"/>
                            <Label Text="{Binding LeadTitle}" FontFamily="NewsreaderSemiBold" FontSize="27"
                                   LineHeight="1.12" CharacterSpacing="-0.27" TextColor="{StaticResource TzInk}"/>
                            <Label Text="{Binding LeadLede}" Margin="0,9,0,0" FontFamily="ArchivoRegular"
                                   FontSize="13" LineHeight="1.65" TextColor="{StaticResource TzBodyMuted}"/>
                            <Label Text="{Binding LeadByline}" Margin="0,12,0,0" Style="{StaticResource MetaText}"/>
                        </VerticalStackLayout>
                    </VerticalStackLayout>
                    <Border.GestureRecognizers>
                        <TapGestureRecognizer Command="{Binding OpenLeadCommand}"/>
                    </Border.GestureRecognizers>
                </Border>

                <!-- More from the archive -->
                <VerticalStackLayout Padding="20,24,20,0" Spacing="0">
                    <Label Text="More from the archive" Style="{StaticResource KickerLabel}"
                           TextColor="{StaticResource TzMuted2}" Margin="0,0,0,14"/>
                    <VerticalStackLayout BindableLayout.ItemsSource="{Binding Articles}">
                        <BindableLayout.ItemTemplate>
                            <DataTemplate x:DataType="vm:ArticleRowViewModel">
                                <Grid Padding="0,15" ColumnDefinitions="Auto,*" ColumnSpacing="14">
                                    <BoxView Grid.ColumnSpan="2" HeightRequest="1" VerticalOptions="Start"
                                             Margin="0,-15,0,0" Color="{StaticResource TzRule}"/>
                                    <controls:PhotoOrPlaceholder WidthRequest="74" HeightRequest="74"
                                                                 CornerRadius="14" ShowCaption="False"
                                                                 ImageAsset="{Binding ImageAsset}"/>
                                    <VerticalStackLayout Grid.Column="1" Spacing="0">
                                        <Label Text="{Binding Kicker}" Style="{StaticResource MicroLabel}"
                                               CharacterSpacing="1.11" TextColor="{StaticResource TzGold}"
                                               Margin="0,0,0,6"/>
                                        <Label Text="{Binding TitleText}" FontFamily="NewsreaderSemiBold"
                                               FontSize="18" LineHeight="1.2" TextColor="{StaticResource TzInk}"/>
                                        <Label Text="{Binding Byline}" Margin="0,6,0,0" FontFamily="ArchivoRegular"
                                               FontSize="10.5" LineHeight="1.4" TextColor="{StaticResource TzMuted2}"/>
                                    </VerticalStackLayout>
                                    <Grid.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding OpenCommand}"/>
                                    </Grid.GestureRecognizers>
                                </Grid>
                            </DataTemplate>
                        </BindableLayout.ItemTemplate>
                    </VerticalStackLayout>
                </VerticalStackLayout>

                <!-- Record an elder -->
                <Border Style="{StaticResource DeepPanel}" Margin="20,22,20,0">
                    <VerticalStackLayout Spacing="0">
                        <Label Text="Record an elder telling it" FontFamily="NewsreaderMedium" FontSize="21"
                               LineHeight="1.2" TextColor="{StaticResource TzSurface}" Margin="0,0,0,8"/>
                        <Label Text="Audio in any Zambian language. The archive keeps the recording alongside a written translation."
                               FontFamily="ArchivoRegular" FontSize="12.5" LineHeight="1.65"
                               TextColor="{StaticResource TzOnDark82}" Margin="0,0,0,15"/>
                        <Border BackgroundColor="{StaticResource TzGold}" StrokeThickness="0"
                                StrokeShape="RoundRectangle 13" Padding="17,13" HorizontalOptions="Start">
                            <HorizontalStackLayout Spacing="9">
                                <BoxView WidthRequest="9" HeightRequest="9" CornerRadius="4.5"
                                         Color="{StaticResource TzSurface}" VerticalOptions="Center"/>
                                <Label Text="Start a recording" FontFamily="ArchivoSemiBold" FontSize="13"
                                       TextColor="{StaticResource TzSurface}"/>
                            </HorizontalStackLayout>
                        </Border>
                    </VerticalStackLayout>
                </Border>

                <BoxView HeightRequest="26" Color="Transparent"/>
            </VerticalStackLayout>
        </ScrollView>

        <controls:BottomNavBar Grid.Row="1" ActiveSection="culture"/>
    </Grid>
</ContentPage>
```

- [ ] **Step 6: Write the code-behind, register and verify**

`CulturePage.xaml.cs` follows `HomePage.xaml.cs`. Register `CultureViewModel` and `CulturePage` as transient.

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Compare with frame 07. Check: the lead card's striped image carries the caption "photo: woman stirring nshima with a mwiko"; the five rows have gold uppercase kickers and serif titles; gold appears only on the kickers and the recording CTA.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(mobile): implement Food Stories screen"
```

---

## Task 14: Story reader (frame 08)

**Layout:** 250pt striped image with back circle and caption · 24pt side padding from here down · gold kicker · 33pt serif headline · author row with 36pt striped avatar, separated by a rule · cream audio player card (green circle with a play triangle, "Listen in Bemba", 18%-filled gold track, "12:40") · article body where the lede is 19pt Newsreader and paragraphs are 14pt Archivo at 1.8 line height, with a cream italic pull-quote · "Recipes in this story" list · dashed "Do you remember it differently?" card · 26pt spacer.

**Files:**
- Create: `TasteZambia.Core/ViewModels/StoryViewModel.cs`
- Create: `TasteZambia.Mobile/Views/StoryPage.xaml` + `.xaml.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/StoryViewModelTests.cs`

**Interfaces:**
- Consumes: `IArticleRepository`, `ICatalogService`, `IFavouritesService`, `IPreferenceService`, `INavigationService`.
- Produces: `StoryViewModel` — `ArticleId` (query property), `Kicker`, `TitleText`, `Author`, `ReadMeta`, `HeroCaption`, `Blocks` (`ArticleBlock`), `HasAudio`, `AudioLabel`, `AudioDuration`, `AudioProgressWidth`, `RelatedDishes`, `BackCommand`.

- [ ] **Step 1: Write the failing test**

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class StoryViewModelTests
{
    private sealed class Nav : INavigationService
    {
        public int BackCount { get; private set; }
        public Task GoToAsync(string r) => Task.CompletedTask;
        public Task GoToAsync(string r, IDictionary<string, object> p) => Task.CompletedTask;
        public Task GoBackAsync() { BackCount++; return Task.CompletedTask; }
    }

    private static StoryViewModel Sut(Nav nav) => new(
        new InMemoryArticleRepository(),
        new CatalogService(new InMemoryDishRepository()),
        new FavouritesService(), new PreferenceService(), nav) { ArticleId = "nshima" };

    [Fact]
    public async Task Initialize_LoadsTheNshimaEssay()
    {
        var vm = Sut(new Nav());
        await vm.InitializeAsync();

        Assert.Equal("Archive essay · Pan-Zambian", vm.Kicker);
        Assert.Equal("The History of Nshima", vm.TitleText);
        Assert.Equal("Dr. Mutale Chileshe", vm.Author);
        Assert.Equal(6, vm.Blocks.Count);
        Assert.Equal(ArticleBlockKind.Lede, vm.Blocks[0].Kind);
        Assert.Equal(ArticleBlockKind.PullQuote, vm.Blocks[3].Kind);
    }

    [Fact]
    public async Task AudioNarrationIsExposedWithAnEighteenPercentTrack()
    {
        var vm = Sut(new Nav());
        await vm.InitializeAsync();

        Assert.True(vm.HasAudio);
        Assert.Equal("Listen in Bemba", vm.AudioLabel);
        Assert.Equal("12:40", vm.AudioDuration);
        Assert.Equal(0.18, vm.AudioProgress, 3);
    }

    [Fact]
    public async Task RelatedDishesResolveFromTheArticle()
    {
        var vm = Sut(new Nav());
        await vm.InitializeAsync();

        Assert.Single(vm.RelatedDishes);
        Assert.Equal("Nshima", vm.RelatedDishes[0].Title);
    }

    [Fact]
    public async Task Back_PopsTheStack()
    {
        var nav = new Nav();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        await vm.BackCommand.ExecuteAsync(null);

        Assert.Equal(1, nav.BackCount);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~StoryViewModelTests`
Expected: FAIL — `StoryViewModel` does not exist.

- [ ] **Step 3: Write the ViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels.Items;

namespace TasteZambia.Core.ViewModels;

public sealed partial class StoryViewModel(
    IArticleRepository articles,
    ICatalogService catalog,
    IFavouritesService favourites,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    /// <summary>Set from the navigation query string. Defaults to the one fully written essay.</summary>
    public string ArticleId { get; set; } = "nshima";

    public ObservableCollection<ArticleBlock> Blocks { get; } = [];
    public ObservableCollection<DishItemViewModel> RelatedDishes { get; } = [];

    [ObservableProperty] private string _kicker = "";
    [ObservableProperty] private string _titleText = "";
    [ObservableProperty] private string _author = "";
    [ObservableProperty] private string _readMeta = "";
    [ObservableProperty] private string _heroCaption = "";
    [ObservableProperty] private bool _hasAudio;
    [ObservableProperty] private string _audioLabel = "";
    [ObservableProperty] private string _audioDuration = "";
    [ObservableProperty] private double _audioProgress;

    /// <summary>Track is the 24pt-padded content width (390 - 48) minus the play button and duration.</summary>
    public double AudioProgressWidth => 214 * AudioProgress;

    public override async Task InitializeAsync()
    {
        if (Blocks.Count > 0) return;

        var article = await articles.GetByIdAsync(ArticleId);
        if (article is null) return;

        var region = article.Meta.Contains('·')
            ? article.Meta[(article.Meta.IndexOf('·') + 1)..].Trim()
            : article.Meta;

        Kicker = $"{article.Kicker} · {region}";
        TitleText = article.Title;
        Author = article.Author;
        ReadMeta = article.Meta;
        HeroCaption = article.PhotoNeededCaption;

        if (article.Audio is { } audio)
        {
            HasAudio = true;
            AudioLabel = audio.Label;
            AudioDuration = audio.Duration;
            AudioProgress = audio.Progress;
            OnPropertyChanged(nameof(AudioProgressWidth));
        }

        foreach (var block in article.Body)
            Blocks.Add(block);

        foreach (var dish in await catalog.GetDishesByIdsAsync(article.RelatedDishIds))
            RelatedDishes.Add(new DishItemViewModel(dish, favourites, preferences, Navigation));
    }

    [RelayCommand]
    private Task Back() => Navigation.GoBackAsync();
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~StoryViewModelTests`
Expected: PASS, 4 tests.

- [ ] **Step 5: Write `StoryPage.xaml`**

The three block kinds are rendered by a `DataTemplateSelector` so the lede, paragraphs and pull-quote each get their own typography.

`TasteZambia.Mobile/Views/ArticleBlockTemplateSelector.cs`:

```csharp
using TasteZambia.Core.Models;

namespace TasteZambia.Mobile.Views;

public sealed class ArticleBlockTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Lede { get; set; }
    public DataTemplate? Paragraph { get; set; }
    public DataTemplate? PullQuote { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        => item is ArticleBlock block
            ? block.Kind switch
            {
                ArticleBlockKind.Lede => Lede!,
                ArticleBlockKind.PullQuote => PullQuote!,
                _ => Paragraph!,
            }
            : Paragraph!;
}
```

`StoryPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TasteZambia.Mobile.Controls"
             xmlns:views="clr-namespace:TasteZambia.Mobile.Views"
             xmlns:vm="clr-namespace:TasteZambia.Core.ViewModels;assembly=TasteZambia.Core"
             xmlns:items="clr-namespace:TasteZambia.Core.ViewModels.Items;assembly=TasteZambia.Core"
             xmlns:models="clr-namespace:TasteZambia.Core.Models;assembly=TasteZambia.Core"
             x:Class="TasteZambia.Mobile.Views.StoryPage"
             x:DataType="vm:StoryViewModel">

    <ContentPage.Resources>
        <DataTemplate x:Key="LedeBlock" x:DataType="models:ArticleBlock">
            <Label Text="{Binding Text}" Margin="0,0,0,18" FontFamily="NewsreaderRegular"
                   FontSize="19" LineHeight="1.55" TextColor="{StaticResource TzInkSoft}"/>
        </DataTemplate>

        <DataTemplate x:Key="ParagraphBlock" x:DataType="models:ArticleBlock">
            <Label Text="{Binding Text}" Margin="0,0,0,16" FontFamily="ArchivoRegular"
                   FontSize="14" LineHeight="1.8" TextColor="{StaticResource TzStoryBody}"/>
        </DataTemplate>

        <DataTemplate x:Key="PullQuoteBlock" x:DataType="models:ArticleBlock">
            <Border Margin="0,6,0,22" BackgroundColor="{StaticResource TzCream}" StrokeThickness="0"
                    StrokeShape="RoundRectangle 16" Padding="20,18">
                <Label Text="{Binding Text}" FontFamily="NewsreaderItalic" FontSize="20"
                       LineHeight="1.5" TextColor="{StaticResource TzGreenDeep}"/>
            </Border>
        </DataTemplate>

        <views:ArticleBlockTemplateSelector x:Key="BlockSelector"
                                            Lede="{StaticResource LedeBlock}"
                                            Paragraph="{StaticResource ParagraphBlock}"
                                            PullQuote="{StaticResource PullQuoteBlock}"/>
    </ContentPage.Resources>

    <Grid RowDefinitions="*,Auto">
        <ScrollView Grid.Row="0">
            <VerticalStackLayout Spacing="0">

                <Grid HeightRequest="250">
                    <controls:StripePlaceholder StripeWidth="7"/>
                    <controls:CircleButton Margin="16" HorizontalOptions="Start" VerticalOptions="Start"
                                           Glyph="←" GlyphColor="{StaticResource TzInk}"
                                           Command="{Binding BackCommand}"/>
                    <Label Text="{Binding HeroCaption}" Margin="16,0,0,13"
                           HorizontalOptions="Start" VerticalOptions="End"
                           FontFamily="PlexMonoRegular" FontSize="9.5" LineHeight="1.3"
                           TextColor="{StaticResource TzMuted2}"/>
                </Grid>

                <VerticalStackLayout Padding="24,22,24,0" Spacing="0">
                    <Label Text="{Binding Kicker}" Style="{StaticResource KickerLabel}"
                           TextColor="{StaticResource TzGold}" Margin="0,0,0,12"/>
                    <Label Text="{Binding TitleText}" FontFamily="NewsreaderSemiBold" FontSize="33"
                           LineHeight="1.08" CharacterSpacing="-0.5" TextColor="{StaticResource TzInk}"/>

                    <Grid ColumnDefinitions="Auto,*" ColumnSpacing="11" Margin="0,16,0,0" Padding="0,0,0,18">
                        <controls:PhotoOrPlaceholder WidthRequest="36" HeightRequest="36"
                                                     CornerRadius="18" ShowCaption="False"/>
                        <VerticalStackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
                            <Label Text="{Binding Author}" FontFamily="ArchivoSemiBold" FontSize="12.5"
                                   LineHeight="1.2" TextColor="{StaticResource TzInk}"/>
                            <Label Text="{Binding ReadMeta}" FontFamily="ArchivoRegular" FontSize="10.5"
                                   LineHeight="1.3" TextColor="{StaticResource TzMuted2}"/>
                        </VerticalStackLayout>
                        <BoxView Grid.ColumnSpan="2" HeightRequest="1" VerticalOptions="End"
                                 Color="{StaticResource TzBorder}"/>
                    </Grid>
                </VerticalStackLayout>

                <!-- Audio player -->
                <Border IsVisible="{Binding HasAudio}" Margin="24,18,24,0"
                        BackgroundColor="{StaticResource TzCream}" StrokeThickness="0"
                        StrokeShape="RoundRectangle 15" Padding="14">
                    <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="13">
                        <Border WidthRequest="40" HeightRequest="40" BackgroundColor="{StaticResource TzGreenDeep}"
                                StrokeThickness="0" StrokeShape="RoundRectangle 20" Padding="0">
                            <Label Text="▶" FontSize="13" TextColor="{StaticResource TzSurface}"
                                   HorizontalOptions="Center" VerticalOptions="Center"/>
                        </Border>
                        <VerticalStackLayout Grid.Column="1" Spacing="8" VerticalOptions="Center">
                            <Label Text="{Binding AudioLabel}" FontFamily="ArchivoSemiBold" FontSize="11.5"
                                   LineHeight="1.2" TextColor="{StaticResource TzInk}"/>
                            <Border HeightRequest="3" StrokeThickness="0" Padding="0"
                                    StrokeShape="RoundRectangle 1.5" BackgroundColor="{StaticResource TzBorder}">
                                <BoxView Color="{StaticResource TzGold}" HorizontalOptions="Start"
                                         WidthRequest="{Binding AudioProgressWidth}"/>
                            </Border>
                        </VerticalStackLayout>
                        <Label Grid.Column="2" Text="{Binding AudioDuration}" VerticalOptions="Center"
                               FontFamily="PlexMonoRegular" FontSize="10.5" TextColor="{StaticResource TzMuted}"/>
                    </Grid>
                </Border>

                <!-- Body -->
                <VerticalStackLayout Padding="24,22,24,0"
                                     BindableLayout.ItemsSource="{Binding Blocks}"
                                     BindableLayout.ItemTemplateSelector="{StaticResource BlockSelector}"/>

                <!-- Recipes in this story -->
                <VerticalStackLayout Padding="24,26,24,0" Spacing="12">
                    <Label Text="Recipes in this story" Style="{StaticResource MicroLabel}"/>
                    <VerticalStackLayout Spacing="9" BindableLayout.ItemsSource="{Binding RelatedDishes}">
                        <BindableLayout.ItemTemplate>
                            <DataTemplate x:DataType="items:DishItemViewModel">
                                <Border Style="{StaticResource Card}" StrokeShape="RoundRectangle 15" Padding="11">
                                    <Grid ColumnDefinitions="Auto,*" ColumnSpacing="12">
                                        <controls:PhotoOrPlaceholder WidthRequest="58" HeightRequest="58"
                                                                     CornerRadius="12" ShowCaption="False"
                                                                     ImageAsset="{Binding ImageAsset}"/>
                                        <VerticalStackLayout Grid.Column="1" Spacing="3" VerticalOptions="Center">
                                            <Label Text="{Binding Title}" Style="{StaticResource CardTitle}"/>
                                            <Label Text="{Binding Subtitle}" Style="{StaticResource EnglishSubtitle}"/>
                                        </VerticalStackLayout>
                                    </Grid>
                                    <Border.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding OpenCommand}"/>
                                    </Border.GestureRecognizers>
                                </Border>
                            </DataTemplate>
                        </BindableLayout.ItemTemplate>
                    </VerticalStackLayout>
                </VerticalStackLayout>

                <!-- Correction invitation -->
                <Border Margin="24,24,24,0" Stroke="{StaticResource TzBorderDashed}" StrokeThickness="1"
                        StrokeDashArray="4,3" StrokeShape="RoundRectangle 16" Padding="16"
                        BackgroundColor="Transparent">
                    <VerticalStackLayout Spacing="5">
                        <Label Text="Do you remember it differently?" FontFamily="ArchivoSemiBold"
                               FontSize="12.5" LineHeight="1.3" TextColor="{StaticResource TzInk}"/>
                        <Label FontFamily="ArchivoRegular" FontSize="11.5" LineHeight="1.55"
                               TextColor="{StaticResource TzBodyMuted}">
                            <Label.FormattedText>
                                <FormattedString>
                                    <Span Text="The archive corrects itself from community memory. "/>
                                    <Span Text="Add what you know" FontFamily="ArchivoSemiBold"
                                          TextColor="{StaticResource TzGold}"/>
                                </FormattedString>
                            </Label.FormattedText>
                        </Label>
                    </VerticalStackLayout>
                </Border>

                <BoxView HeightRequest="26" Color="Transparent"/>
            </VerticalStackLayout>
        </ScrollView>

        <controls:BottomNavBar Grid.Row="1" ActiveSection="culture"/>
    </Grid>
</ContentPage>
```

- [ ] **Step 6: Write the code-behind, register and verify**

`StoryPage.xaml.cs` mirrors `IngredientPage.xaml.cs` with `[QueryProperty(nameof(ArticleId), "articleId")]`. Register `StoryViewModel` and `StoryPage` as transient.

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Compare with frame 08. Check: the lede is visibly serif and larger than the body; the pull-quote is italic serif on cream in `TzGreenDeep`; the audio track is filled ~18%; opening any of the other five articles from Culture shows an empty body (only the Nshima essay has one) — that is expected, and is listed under Known Gaps.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(mobile): implement Story reader screen"
```

---

## Task 15: Share a Recipe wizard (frame 09)

**Layout:** clay kicker · 31pt serif title · intro · `WizardProgress` · one of four step bodies · Back / gold-primary button row. After step 4 the whole form is replaced by a deep-green "Submitted for review" confirmation, an "IN REVIEW" status card and a "Share another recipe" outline button.

**Files:**
- Create: `TasteZambia.Core/ViewModels/ShareViewModel.cs`
- Create: `TasteZambia.Mobile/Views/SharePage.xaml` + `.xaml.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/ShareViewModelTests.cs`

**Interfaces:**
- Consumes: `IContributionService`, `INavigationService`.
- Produces: `ShareViewModel` — `Step`, `StepTitle`, `IsStep1`…`IsStep4`, `IsSubmitted`, `IsPending`, `CanGoBack`, `NextLabel`, `Draft`, `Pipeline`, `SubmittedName`, `SubmittedMeta`, commands `Next`, `Back`, `Reset`.

- [ ] **Step 1: Write the failing test**

```csharp
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class ShareViewModelTests
{
    private sealed class Nav : INavigationService
    {
        public Task GoToAsync(string r) => Task.CompletedTask;
        public Task GoToAsync(string r, IDictionary<string, object> p) => Task.CompletedTask;
        public Task GoBackAsync() => Task.CompletedTask;
    }

    private static ShareViewModel Sut() => new(new ContributionService(), new Nav());

    [Fact]
    public async Task StartsOnStepOneWithNoBackButton()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal(1, vm.Step);
        Assert.Equal("Recipe basics", vm.StepTitle);
        Assert.True(vm.IsStep1);
        Assert.False(vm.CanGoBack);
        Assert.Equal("Continue", vm.NextLabel);
        Assert.True(vm.IsPending);
    }

    [Fact]
    public async Task DraftIsPreFilledFromTheDesignWalkthrough()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal("Chibwabwa na Mbalala", vm.Draft.LocalName);
        Assert.Equal("Northern", vm.Draft.Province);
        Assert.Equal(3, vm.Draft.Ingredients.Count);
        Assert.Equal(2, vm.Draft.Steps.Count);
    }

    [Fact]
    public async Task StepFour_ShowsThePipelineAndTheSubmitLabel()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        for (var i = 0; i < 3; i++)
            await vm.NextCommand.ExecuteAsync(null);

        Assert.Equal(4, vm.Step);
        Assert.True(vm.IsStep4);
        Assert.Equal("Submit for review", vm.NextLabel);
        Assert.Equal(4, vm.Pipeline.Count);
        Assert.True(vm.Pipeline[0].IsComplete);
        Assert.False(vm.Pipeline[2].IsComplete);
    }

    [Fact]
    public async Task SubmittingFromStepFour_SwitchesToTheConfirmation()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        for (var i = 0; i < 4; i++)
            await vm.NextCommand.ExecuteAsync(null);

        Assert.True(vm.IsSubmitted);
        Assert.False(vm.IsPending);
        Assert.Equal("Chibwabwa na Mbalala", vm.SubmittedName);
        Assert.Equal("Northern Province · submitted just now", vm.SubmittedMeta);
    }

    [Fact]
    public async Task Reset_ReturnsToStepOne()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        for (var i = 0; i < 4; i++)
            await vm.NextCommand.ExecuteAsync(null);

        vm.ResetCommand.Execute(null);

        Assert.False(vm.IsSubmitted);
        Assert.Equal(1, vm.Step);
    }

    [Fact]
    public async Task Back_NeverGoesBelowStepOne()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.BackCommand.Execute(null);

        Assert.Equal(1, vm.Step);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~ShareViewModelTests`
Expected: FAIL — `ShareViewModel` does not exist.

- [ ] **Step 3: Write the ViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class ShareViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ContributionDraft Draft { get; private set; } = new();
    public ObservableCollection<ReviewStage> Pipeline { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(IsStep4))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(NextLabel))]
    private int _step = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPending))]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(IsStep4))]
    private bool _isSubmitted;

    [ObservableProperty] private string _submittedName = "";
    [ObservableProperty] private string _submittedMeta = "";

    public string StepTitle => SeedData.ShareSteps[Step - 1];
    public bool IsPending => !IsSubmitted;
    public bool IsStep1 => IsPending && Step == 1;
    public bool IsStep2 => IsPending && Step == 2;
    public bool IsStep3 => IsPending && Step == 3;
    public bool IsStep4 => IsPending && Step == 4;
    public bool CanGoBack => Step > 1;
    public string NextLabel => Step == 4 ? "Submit for review" : "Continue";

    public override Task InitializeAsync()
    {
        if (Pipeline.Count > 0) return Task.CompletedTask;

        Draft = contributions.StartShareDraft();

        foreach (var stage in contributions.ReviewPipeline)
            Pipeline.Add(stage);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Next()
    {
        if (Step < 4)
        {
            Step++;
            return;
        }

        var contribution = await contributions.SubmitAsync(Draft);
        SubmittedName = contribution.Name;
        SubmittedMeta = contribution.Meta;
        IsSubmitted = true;
    }

    [RelayCommand]
    private void Back() => Step = Math.Max(1, Step - 1);

    [RelayCommand]
    private void Reset()
    {
        IsSubmitted = false;
        Step = 1;
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~ShareViewModelTests`
Expected: PASS, 6 tests.

- [ ] **Step 5: Write `SharePage.xaml`**

The four step bodies are read-only field cards in the design — they present the draft rather than accept typing, so they are `Border`-wrapped `Label`s. Replacing them with `Entry`/`Editor` is the next iteration and is listed in Known Gaps.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TasteZambia.Mobile.Controls"
             xmlns:vm="clr-namespace:TasteZambia.Core.ViewModels;assembly=TasteZambia.Core"
             xmlns:models="clr-namespace:TasteZambia.Core.Models;assembly=TasteZambia.Core"
             x:Class="TasteZambia.Mobile.Views.SharePage"
             x:DataType="vm:ShareViewModel">

    <ContentPage.Resources>
        <Style x:Key="FieldCard" TargetType="Border">
            <Setter Property="BackgroundColor" Value="{StaticResource TzSurface}"/>
            <Setter Property="Stroke" Value="{StaticResource TzBorder}"/>
            <Setter Property="StrokeThickness" Value="1"/>
            <Setter Property="StrokeShape" Value="RoundRectangle 13"/>
            <Setter Property="Padding" Value="14,13"/>
        </Style>
    </ContentPage.Resources>

    <Grid RowDefinitions="*,Auto">
        <ScrollView Grid.Row="0">
            <VerticalStackLayout Spacing="0">

                <VerticalStackLayout Padding="20,20,20,0" Spacing="9">
                    <Label Text="Community contribution" Style="{StaticResource KickerLabel}"/>
                    <Label Text="Share a Recipe" Style="{StaticResource DisplayTitle}" CharacterSpacing="-0.31"/>
                    <Label Text="Add a dish to the public archive. Everything submitted is reviewed before it appears."
                           Style="{StaticResource BodyText}" TextColor="{StaticResource TzBodyMuted}"/>
                </VerticalStackLayout>

                <!-- Pending form -->
                <VerticalStackLayout IsVisible="{Binding IsPending}" Spacing="0">
                    <controls:WizardProgress Margin="20,20,20,0" StepTitle="{Binding StepTitle}"
                                             StepNumber="{Binding Step}" StepCount="4"/>

                    <!-- Step 1: Recipe basics -->
                    <VerticalStackLayout Padding="20" Spacing="16" IsVisible="{Binding IsStep1}">
                        <VerticalStackLayout Spacing="7">
                            <Label Text="Local name of the dish" Style="{StaticResource FieldLabel}"/>
                            <Border Style="{StaticResource FieldCard}">
                                <Label Text="{Binding Draft.LocalName}" FontFamily="NewsreaderSemiBold"
                                       FontSize="19" TextColor="{StaticResource TzInk}"/>
                            </Border>
                        </VerticalStackLayout>
                        <VerticalStackLayout Spacing="7">
                            <Label Text="Short description in English" Style="{StaticResource FieldLabel}"/>
                            <Border Style="{StaticResource FieldCard}">
                                <Label Text="{Binding Draft.EnglishDescription}" FontFamily="ArchivoRegular"
                                       FontSize="13" LineHeight="1.5" TextColor="{StaticResource TzBodyMuted}"/>
                            </Border>
                        </VerticalStackLayout>
                        <Grid ColumnDefinitions="*,*" ColumnSpacing="11">
                            <VerticalStackLayout Spacing="7">
                                <Label Text="Province" Style="{StaticResource FieldLabel}"/>
                                <Border Style="{StaticResource FieldCard}">
                                    <Label Text="{Binding Draft.Province}" FontFamily="ArchivoMedium"
                                           FontSize="13" TextColor="{StaticResource TzInk}"/>
                                </Border>
                            </VerticalStackLayout>
                            <VerticalStackLayout Grid.Column="1" Spacing="7">
                                <Label Text="Meal type" Style="{StaticResource FieldLabel}"/>
                                <Border Style="{StaticResource FieldCard}">
                                    <Label Text="{Binding Draft.MealType}" FontFamily="ArchivoMedium"
                                           FontSize="13" TextColor="{StaticResource TzInk}"/>
                                </Border>
                            </VerticalStackLayout>
                        </Grid>
                        <VerticalStackLayout Spacing="7">
                            <Label Text="Photos of the finished dish" Style="{StaticResource FieldLabel}"/>
                            <Grid ColumnDefinitions="Auto,*" ColumnSpacing="10" HeightRequest="84">
                                <Border WidthRequest="84" StrokeThickness="0" StrokeShape="RoundRectangle 13" Padding="0">
                                    <controls:StripePlaceholder/>
                                </Border>
                                <Border Grid.Column="1" Stroke="{StaticResource TzBorderDashed}" StrokeThickness="1"
                                        StrokeDashArray="4,3" StrokeShape="RoundRectangle 13"
                                        BackgroundColor="Transparent">
                                    <VerticalStackLayout Spacing="5" VerticalOptions="Center" HorizontalOptions="Center">
                                        <Label Text="+" FontSize="19" TextColor="{StaticResource TzFaint}"
                                               HorizontalOptions="Center"/>
                                        <Label Text="add photo" FontFamily="PlexMonoRegular" FontSize="10.5"
                                               TextColor="{StaticResource TzMuted2}"/>
                                    </VerticalStackLayout>
                                </Border>
                            </Grid>
                        </VerticalStackLayout>
                    </VerticalStackLayout>

                    <!-- Step 2: Ingredients and method -->
                    <VerticalStackLayout Padding="20" Spacing="16" IsVisible="{Binding IsStep2}">
                        <VerticalStackLayout Spacing="8">
                            <Grid ColumnDefinitions="*,Auto">
                                <Label Text="Ingredients" Style="{StaticResource FieldLabel}"/>
                                <Label Grid.Column="1" Text="+ Add" FontFamily="ArchivoMedium" FontSize="10.5"
                                       TextColor="{StaticResource TzGold}"/>
                            </Grid>
                            <Border Stroke="{StaticResource TzBorder}" StrokeThickness="1"
                                    StrokeShape="RoundRectangle 13" Padding="0"
                                    BackgroundColor="{StaticResource TzSurface}">
                                <VerticalStackLayout BindableLayout.ItemsSource="{Binding Draft.Ingredients}">
                                    <BindableLayout.ItemTemplate>
                                        <DataTemplate x:DataType="models:RecipeIngredient">
                                            <Grid Padding="14,12" ColumnDefinitions="*,Auto">
                                                <Label Text="{Binding DisplayName}" FontFamily="ArchivoMedium" FontSize="13">
                                                    <Label.Triggers>
                                                        <DataTrigger TargetType="Label" Binding="{Binding IsLinked}" Value="True">
                                                            <Setter Property="TextColor" Value="{StaticResource TzGreenDeep}"/>
                                                        </DataTrigger>
                                                        <DataTrigger TargetType="Label" Binding="{Binding IsLinked}" Value="False">
                                                            <Setter Property="TextColor" Value="{StaticResource TzInk}"/>
                                                        </DataTrigger>
                                                    </Label.Triggers>
                                                </Label>
                                                <Label Grid.Column="1" Text="{Binding Quantity}"
                                                       FontFamily="PlexMonoRegular" FontSize="11.5"
                                                       TextColor="{StaticResource TzBodyMuted}"/>
                                                <BoxView Grid.ColumnSpan="2" HeightRequest="1" VerticalOptions="End"
                                                         Margin="0,0,0,-12" Color="{StaticResource TzHairline}"/>
                                            </Grid>
                                        </DataTemplate>
                                    </BindableLayout.ItemTemplate>
                                </VerticalStackLayout>
                            </Border>
                            <Label Text="Green names are already in the ingredient archive and will link automatically."
                                   FontFamily="ArchivoRegular" FontSize="10.5" LineHeight="1.5"
                                   TextColor="{StaticResource TzMuted2}"/>
                        </VerticalStackLayout>

                        <VerticalStackLayout Spacing="9">
                            <Grid ColumnDefinitions="*,Auto">
                                <Label Text="Cooking steps" Style="{StaticResource FieldLabel}"/>
                                <Label Grid.Column="1" Text="+ Add step" FontFamily="ArchivoMedium" FontSize="10.5"
                                       TextColor="{StaticResource TzGold}"/>
                            </Grid>
                            <VerticalStackLayout Spacing="9" BindableLayout.ItemsSource="{Binding Draft.Steps}">
                                <BindableLayout.ItemTemplate>
                                    <DataTemplate>
                                        <Border Style="{StaticResource FieldCard}" Padding="13">
                                            <Label Text="{Binding}" FontFamily="ArchivoRegular" FontSize="12.5"
                                                   LineHeight="1.6" TextColor="{StaticResource TzBody}"/>
                                        </Border>
                                    </DataTemplate>
                                </BindableLayout.ItemTemplate>
                            </VerticalStackLayout>
                            <Border Stroke="{StaticResource TzBorderDashed}" StrokeThickness="1" StrokeDashArray="4,3"
                                    StrokeShape="RoundRectangle 13" Padding="13" BackgroundColor="Transparent">
                                <Label Text="Add step 3" FontFamily="ArchivoRegular" FontSize="11.5"
                                       HorizontalOptions="Center" TextColor="{StaticResource TzMuted2}"/>
                            </Border>
                        </VerticalStackLayout>
                    </VerticalStackLayout>

                    <!-- Step 3: Cultural background -->
                    <VerticalStackLayout Padding="20" Spacing="16" IsVisible="{Binding IsStep3}">
                        <VerticalStackLayout Spacing="7">
                            <Label Text="Where the dish comes from" Style="{StaticResource FieldLabel}"/>
                            <Border Style="{StaticResource FieldCard}">
                                <Label Text="{Binding Draft.Origin}" FontFamily="ArchivoRegular" FontSize="13"
                                       LineHeight="1.7" TextColor="{StaticResource TzBody}"/>
                            </Border>
                        </VerticalStackLayout>
                        <VerticalStackLayout Spacing="7">
                            <Label Text="Cultural significance" Style="{StaticResource FieldLabel}"/>
                            <Border Style="{StaticResource FieldCard}">
                                <Label Text="{Binding Draft.CulturalSignificance}" FontFamily="ArchivoRegular"
                                       FontSize="13" LineHeight="1.7" TextColor="{StaticResource TzBody}"/>
                            </Border>
                        </VerticalStackLayout>
                        <VerticalStackLayout Spacing="7">
                            <Label Text="Traditional method, if you know it" Style="{StaticResource FieldLabel}"/>
                            <Border Style="{StaticResource FieldCard}">
                                <Label Text="{Binding Draft.TraditionalMethod}" FontFamily="ArchivoRegular"
                                       FontSize="13" LineHeight="1.7" TextColor="{StaticResource TzBody}"/>
                            </Border>
                        </VerticalStackLayout>
                        <HorizontalStackLayout Spacing="9">
                            <Border WidthRequest="18" HeightRequest="18" BackgroundColor="{StaticResource TzGreenMid}"
                                    Stroke="{StaticResource TzGreenMid}" StrokeThickness="1.5"
                                    StrokeShape="RoundRectangle 5" Padding="0" VerticalOptions="Center">
                                <Label Text="✓" FontSize="11" TextColor="{StaticResource TzSurface}"
                                       HorizontalOptions="Center" VerticalOptions="Center"/>
                            </Border>
                            <Label Text="Credit the person who taught me this recipe" VerticalOptions="Center"
                                   FontFamily="ArchivoRegular" FontSize="11.5" LineHeight="1.5"
                                   TextColor="{StaticResource TzMuted}"/>
                        </HorizontalStackLayout>
                    </VerticalStackLayout>

                    <!-- Step 4: Submit for review -->
                    <VerticalStackLayout Padding="20" Spacing="0" IsVisible="{Binding IsStep4}">
                        <Label Text="Before it goes public, your recipe passes through review. Here is what happens to it."
                               FontFamily="ArchivoRegular" FontSize="13" LineHeight="1.7"
                               TextColor="{StaticResource TzBody}" Margin="0,0,0,18"/>
                        <VerticalStackLayout BindableLayout.ItemsSource="{Binding Pipeline}">
                            <BindableLayout.ItemTemplate>
                                <DataTemplate x:DataType="models:ReviewStage">
                                    <Grid Padding="0,13" ColumnDefinitions="Auto,*" ColumnSpacing="13">
                                        <BoxView Grid.ColumnSpan="2" HeightRequest="1" VerticalOptions="Start"
                                                 Margin="0,-13,0,0" Color="{StaticResource TzRule}"/>
                                        <BoxView WidthRequest="9" HeightRequest="9" CornerRadius="4.5"
                                                 Margin="0,5,0,0" VerticalOptions="Start"
                                                 Color="{StaticResource TzGold}"/>
                                        <VerticalStackLayout Grid.Column="1" Spacing="5">
                                            <Label Text="{Binding Label}" FontFamily="ArchivoSemiBold" FontSize="12.5"
                                                   LineHeight="1.3" TextColor="{StaticResource TzInk}"/>
                                            <Label Text="{Binding Note}" FontFamily="ArchivoRegular" FontSize="11.5"
                                                   LineHeight="1.6" TextColor="{StaticResource TzBodyMuted}"/>
                                        </VerticalStackLayout>
                                    </Grid>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                        </VerticalStackLayout>
                        <Border Margin="0,18,0,0" BackgroundColor="{StaticResource TzCream}" StrokeThickness="0"
                                StrokeShape="RoundRectangle 16" Padding="15">
                            <Label Text="Review usually takes under two weeks. You keep authorship of anything you submit and can withdraw it at any time."
                                   FontFamily="ArchivoRegular" FontSize="12" LineHeight="1.65"
                                   TextColor="{StaticResource TzBodyMuted}"/>
                        </Border>
                    </VerticalStackLayout>

                    <!-- Nav buttons -->
                    <Grid Padding="20,0" ColumnDefinitions="Auto,*" ColumnSpacing="11">
                        <Border Grid.Column="0" Style="{StaticResource SecondaryAction}" Padding="20,15"
                                IsVisible="{Binding CanGoBack}">
                            <Label Text="Back" FontFamily="ArchivoSemiBold" FontSize="13"
                                   TextColor="{StaticResource TzBody}"/>
                            <Border.GestureRecognizers>
                                <TapGestureRecognizer Command="{Binding BackCommand}"/>
                            </Border.GestureRecognizers>
                        </Border>
                        <Border Grid.Column="1" Style="{StaticResource PrimaryAction}">
                            <Label Text="{Binding NextLabel}" FontFamily="ArchivoSemiBold" FontSize="13.5"
                                   HorizontalOptions="Center" TextColor="{StaticResource TzSurface}"/>
                            <Border.GestureRecognizers>
                                <TapGestureRecognizer Command="{Binding NextCommand}"/>
                            </Border.GestureRecognizers>
                        </Border>
                    </Grid>
                </VerticalStackLayout>

                <!-- Confirmation -->
                <VerticalStackLayout IsVisible="{Binding IsSubmitted}" Padding="20,24,20,0" Spacing="0">
                    <Border BackgroundColor="{StaticResource TzGreenDeep}" StrokeThickness="0"
                            StrokeShape="RoundRectangle 22" Padding="20,24">
                        <VerticalStackLayout Spacing="0" HorizontalOptions="Center">
                            <Border WidthRequest="52" HeightRequest="52" BackgroundColor="#33E8BD77"
                                    Stroke="#80E8BD77" StrokeThickness="1" StrokeShape="RoundRectangle 26"
                                    Padding="0" Margin="0,0,0,16" HorizontalOptions="Center">
                                <Label Text="✓" FontSize="22" TextColor="{StaticResource TzGoldLight}"
                                       HorizontalOptions="Center" VerticalOptions="Center"/>
                            </Border>
                            <Label Text="Submitted for review" FontFamily="NewsreaderMedium" FontSize="26"
                                   LineHeight="1.15" HorizontalTextAlignment="Center"
                                   TextColor="{StaticResource TzSurface}" Margin="0,0,0,10"/>
                            <Label FontFamily="ArchivoRegular" FontSize="12.5" LineHeight="1.7"
                                   HorizontalTextAlignment="Center" TextColor="{StaticResource TzOnDark82}">
                                <Label.FormattedText>
                                    <FormattedString>
                                        <Span Text="{Binding SubmittedName}"/>
                                        <Span Text=" is now with the archive team. You will hear from us before anything is published."/>
                                    </FormattedString>
                                </Label.FormattedText>
                            </Label>
                        </VerticalStackLayout>
                    </Border>

                    <Border Style="{StaticResource Card}" StrokeShape="RoundRectangle 16" Padding="16" Margin="0,20,0,0">
                        <VerticalStackLayout Spacing="6">
                            <Grid ColumnDefinitions="*,Auto" ColumnSpacing="10">
                                <Label Text="{Binding SubmittedName}" FontFamily="NewsreaderSemiBold"
                                       FontSize="18" TextColor="{StaticResource TzInk}"/>
                                <controls:PillLabel Grid.Column="1" Text="IN REVIEW"
                                                    PillBackground="{StaticResource TzGoldTint}"
                                                    TextColor="{StaticResource TzGoldTintText}"/>
                            </Grid>
                            <Label Text="{Binding SubmittedMeta}" Style="{StaticResource MetaText}"/>
                        </VerticalStackLayout>
                    </Border>

                    <Border Style="{StaticResource SecondaryAction}" Margin="0,14,0,0">
                        <Label Text="Share another recipe" FontFamily="ArchivoSemiBold" FontSize="13"
                               HorizontalOptions="Center" TextColor="{StaticResource TzBody}"/>
                        <Border.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding ResetCommand}"/>
                        </Border.GestureRecognizers>
                    </Border>
                </VerticalStackLayout>

                <BoxView HeightRequest="26" Color="Transparent"/>
            </VerticalStackLayout>
        </ScrollView>

        <controls:BottomNavBar Grid.Row="1" ActiveSection="profile"/>
    </Grid>
</ContentPage>
```

- [ ] **Step 6: Write the code-behind, register and verify**

`SharePage.xaml.cs` follows `HomePage.xaml.cs`. Register `ShareViewModel` and `SharePage` as transient.

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Compare with frame 09. Check: the gold track advances a quarter per step; "Back" is absent on step 1; Chibwabwa and Mbalala are green in the step-2 ingredient list and Salt is not; the step-4 button reads "Submit for review" and produces the deep-green confirmation; the bottom nav shows **Profile** as active.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(mobile): implement Share a Recipe wizard"
```

---

## Task 16: Preserve a Family Recipe wizard (frame 10)

Structurally a sibling of Task 15, but the four steps differ and there is **no submitted state** — step 4 is a privacy chooser and the button reads "Save to the archive".

**Layout:** clay kicker · 31pt serif title · intro · `WizardProgress` (steps: *The recipe*, *Who taught you*, *The story*, *Who can see it*) · step body · Back / gold button row.

**Files:**
- Create: `TasteZambia.Core/ViewModels/FamilyViewModel.cs`
- Create: `TasteZambia.Mobile/Views/FamilyPage.xaml` + `.xaml.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/FamilyViewModelTests.cs`

**Interfaces:**
- Consumes: `IContributionService`, `INavigationService`.
- Produces: `FamilyViewModel` — `Step`, `StepTitle`, `IsStep1`…`IsStep4`, `CanGoBack`, `NextLabel`, `Draft`, `PrivacyOptions` (`PrivacyOptionViewModel`), commands `Next`, `Back`. `PrivacyOptionViewModel` — `Level`, `Label`, `Note`, `IsSelected`, `SelectCommand`.

- [ ] **Step 1: Write the failing test**

```csharp
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class FamilyViewModelTests
{
    private sealed class Nav : INavigationService
    {
        public Task GoToAsync(string r) => Task.CompletedTask;
        public Task GoToAsync(string r, IDictionary<string, object> p) => Task.CompletedTask;
        public Task GoBackAsync() => Task.CompletedTask;
    }

    private static FamilyViewModel Sut() => new(new ContributionService(), new Nav());

    [Fact]
    public async Task StartsOnTheRecipeStepWithThePreFilledDraft()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal(1, vm.Step);
        Assert.Equal("The recipe", vm.StepTitle);
        Assert.Equal("Ifisashi ya Banakulu", vm.Draft.LocalName);
        Assert.Equal("Bemba", vm.Draft.Language);
        Assert.False(vm.CanGoBack);
    }

    [Fact]
    public async Task StepTitlesFollowTheDesign()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        var titles = new List<string> { vm.StepTitle };
        for (var i = 0; i < 3; i++)
        {
            vm.NextCommand.Execute(null);
            titles.Add(vm.StepTitle);
        }

        Assert.Equal(["The recipe", "Who taught you", "The story", "Who can see it"], titles);
    }

    [Fact]
    public async Task FinalStepOffersThreePrivacyLevelsWithFamilySelected()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        for (var i = 0; i < 3; i++) vm.NextCommand.Execute(null);

        Assert.True(vm.IsStep4);
        Assert.Equal("Save to the archive", vm.NextLabel);
        Assert.Equal(3, vm.PrivacyOptions.Count);
        Assert.True(vm.PrivacyOptions[1].IsSelected);
        Assert.Equal(PrivacyLevel.SharedWithFamily, vm.Draft.Privacy);
    }

    [Fact]
    public async Task ChoosingAPrivacyLevel_UpdatesTheDraftAndMovesTheSelection()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        for (var i = 0; i < 3; i++) vm.NextCommand.Execute(null);

        vm.PrivacyOptions[2].SelectCommand.Execute(null);

        Assert.True(vm.PrivacyOptions[2].IsSelected);
        Assert.False(vm.PrivacyOptions[1].IsSelected);
        Assert.Equal(PrivacyLevel.PublicInArchive, vm.Draft.Privacy);
    }

    [Fact]
    public async Task NextOnTheFinalStep_DoesNotAdvancePastFour()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        for (var i = 0; i < 6; i++) vm.NextCommand.Execute(null);

        Assert.Equal(4, vm.Step);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~FamilyViewModelTests`
Expected: FAIL — `FamilyViewModel` does not exist.

- [ ] **Step 3: Write the ViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class PrivacyOptionViewModel(
    PrivacyLevel level, string label, string note, Action<PrivacyOptionViewModel> onSelect)
    : ObservableObject
{
    public PrivacyLevel Level { get; } = level;
    public string Label { get; } = label;
    public string Note { get; } = note;

    [ObservableProperty]
    private bool _isSelected;

    [RelayCommand]
    private void Select() => onSelect(this);
}

public sealed partial class FamilyViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ContributionDraft Draft { get; private set; } = new();
    public ObservableCollection<PrivacyOptionViewModel> PrivacyOptions { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(IsStep4))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(NextLabel))]
    private int _step = 1;

    public string StepTitle => SeedData.FamilySteps[Step - 1];
    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;
    public bool IsStep3 => Step == 3;
    public bool IsStep4 => Step == 4;
    public bool CanGoBack => Step > 1;
    public string NextLabel => Step == 4 ? "Save to the archive" : "Continue";

    public override Task InitializeAsync()
    {
        if (PrivacyOptions.Count > 0) return Task.CompletedTask;

        Draft = contributions.StartFamilyDraft();

        PrivacyOptions.Add(new PrivacyOptionViewModel(PrivacyLevel.PrivateToMe, "Private to me",
            "Only you can open it. Nothing is reviewed or published.", SelectPrivacy));
        PrivacyOptions.Add(new PrivacyOptionViewModel(PrivacyLevel.SharedWithFamily, "Shared with family",
            "Anyone you invite by name can read and add to it.", SelectPrivacy));
        PrivacyOptions.Add(new PrivacyOptionViewModel(PrivacyLevel.PublicInArchive, "Public in the archive",
            "Goes to the archive team for verification before it appears publicly.", SelectPrivacy));

        foreach (var option in PrivacyOptions)
            option.IsSelected = option.Level == Draft.Privacy;

        return Task.CompletedTask;
    }

    private void SelectPrivacy(PrivacyOptionViewModel option)
    {
        foreach (var o in PrivacyOptions)
            o.IsSelected = ReferenceEquals(o, option);

        Draft.Privacy = option.Level;
    }

    [RelayCommand]
    private void Next() => Step = Math.Min(4, Step + 1);

    [RelayCommand]
    private void Back() => Step = Math.Max(1, Step - 1);
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~FamilyViewModelTests`
Expected: PASS, 5 tests.

- [ ] **Step 5: Write `FamilyPage.xaml`**

Reuse the `FieldCard` style, the nav-button row and the photo picker exactly as written in Task 15 Step 5 (copy them into this file; the two wizards are separate pages and do not share a base class). The four step bodies:

```xml
<!-- Step 1: The recipe -->
<VerticalStackLayout Padding="20" Spacing="16" IsVisible="{Binding IsStep1}">
    <VerticalStackLayout Spacing="7">
        <Label Text="Recipe name, in your own language" Style="{StaticResource FieldLabel}"/>
        <Border Style="{StaticResource FieldCard}">
            <Label Text="{Binding Draft.LocalName}" FontFamily="NewsreaderSemiBold"
                   FontSize="19" TextColor="{StaticResource TzInk}"/>
        </Border>
    </VerticalStackLayout>
    <VerticalStackLayout Spacing="7">
        <Label Text="What it is, in English" Style="{StaticResource FieldLabel}"/>
        <Border Style="{StaticResource FieldCard}">
            <Label Text="{Binding Draft.EnglishDescription}" FontFamily="ArchivoRegular"
                   FontSize="13" LineHeight="1.5" TextColor="{StaticResource TzBodyMuted}"/>
        </Border>
    </VerticalStackLayout>
    <Grid ColumnDefinitions="*,*" ColumnSpacing="11">
        <VerticalStackLayout Spacing="7">
            <Label Text="Region" Style="{StaticResource FieldLabel}"/>
            <Border Style="{StaticResource FieldCard}">
                <Label Text="{Binding Draft.Province}" FontFamily="ArchivoMedium"
                       FontSize="13" TextColor="{StaticResource TzInk}"/>
            </Border>
        </VerticalStackLayout>
        <VerticalStackLayout Grid.Column="1" Spacing="7">
            <Label Text="Language" Style="{StaticResource FieldLabel}"/>
            <Border Style="{StaticResource FieldCard}">
                <Label Text="{Binding Draft.Language}" FontFamily="ArchivoMedium"
                       FontSize="13" TextColor="{StaticResource TzInk}"/>
            </Border>
        </VerticalStackLayout>
    </Grid>
    <VerticalStackLayout Spacing="7">
        <Label Text="Photos" Style="{StaticResource FieldLabel}"/>
        <Grid ColumnDefinitions="Auto,*" ColumnSpacing="10" HeightRequest="84">
            <Border WidthRequest="84" StrokeThickness="0" StrokeShape="RoundRectangle 13" Padding="0">
                <controls:StripePlaceholder/>
            </Border>
            <Border Grid.Column="1" Stroke="{StaticResource TzBorderDashed}" StrokeThickness="1"
                    StrokeDashArray="4,3" StrokeShape="RoundRectangle 13" BackgroundColor="Transparent">
                <VerticalStackLayout Spacing="5" VerticalOptions="Center" HorizontalOptions="Center">
                    <Label Text="+" FontSize="19" TextColor="{StaticResource TzFaint}" HorizontalOptions="Center"/>
                    <Label Text="add photo" FontFamily="PlexMonoRegular" FontSize="10.5"
                           TextColor="{StaticResource TzMuted2}"/>
                </VerticalStackLayout>
            </Border>
        </Grid>
    </VerticalStackLayout>
</VerticalStackLayout>

<!-- Step 2: Who taught you -->
<VerticalStackLayout Padding="20" Spacing="16" IsVisible="{Binding IsStep2}">
    <VerticalStackLayout Spacing="7">
        <Label Text="Who taught you this recipe" Style="{StaticResource FieldLabel}"/>
        <Border Style="{StaticResource FieldCard}">
            <Label Text="{Binding Draft.TaughtBy}" FontFamily="ArchivoMedium" FontSize="14"
                   TextColor="{StaticResource TzInk}"/>
        </Border>
    </VerticalStackLayout>
    <VerticalStackLayout Spacing="7">
        <Label Text="Where she learned it" Style="{StaticResource FieldLabel}"/>
        <Border Style="{StaticResource FieldCard}">
            <Label Text="{Binding Draft.TaughtByOrigin}" FontFamily="ArchivoRegular" FontSize="13"
                   LineHeight="1.5" TextColor="{StaticResource TzBodyMuted}"/>
        </Border>
    </VerticalStackLayout>
    <Border BackgroundColor="{StaticResource TzCream}" StrokeThickness="0"
            StrokeShape="RoundRectangle 16" Padding="16">
        <Grid ColumnDefinitions="Auto,*" ColumnSpacing="13">
            <Border WidthRequest="44" HeightRequest="44" BackgroundColor="{StaticResource TzGreenDeep}"
                    StrokeThickness="0" StrokeShape="RoundRectangle 22" Padding="0">
                <Label Text="▶" FontSize="14" TextColor="{StaticResource TzSurface}"
                       HorizontalOptions="Center" VerticalOptions="Center"/>
            </Border>
            <VerticalStackLayout Grid.Column="1" Spacing="3" VerticalOptions="Center">
                <Label Text="Record her telling it" FontFamily="ArchivoSemiBold" FontSize="12.5"
                       LineHeight="1.3" TextColor="{StaticResource TzInk}"/>
                <Label Text="Audio in any language. Kept alongside the written recipe."
                       FontFamily="ArchivoRegular" FontSize="11" LineHeight="1.45"
                       TextColor="{StaticResource TzMuted}"/>
            </VerticalStackLayout>
        </Grid>
    </Border>
</VerticalStackLayout>

<!-- Step 3: The story -->
<VerticalStackLayout Padding="20" Spacing="16" IsVisible="{Binding IsStep3}">
    <VerticalStackLayout Spacing="7">
        <Label Text="The story behind it" Style="{StaticResource FieldLabel}"/>
        <Border Style="{StaticResource FieldCard}" Padding="14">
            <Label Text="{Binding Draft.Story}" FontFamily="ArchivoRegular" FontSize="13"
                   LineHeight="1.7" TextColor="{StaticResource TzBody}"/>
        </Border>
    </VerticalStackLayout>
    <VerticalStackLayout Spacing="7">
        <Label Text="Her method, as she did it" Style="{StaticResource FieldLabel}"/>
        <Border Style="{StaticResource FieldCard}" Padding="14">
            <Label Text="{Binding Draft.TraditionalMethod}" FontFamily="ArchivoRegular" FontSize="13"
                   LineHeight="1.7" TextColor="{StaticResource TzBody}"/>
        </Border>
    </VerticalStackLayout>
    <HorizontalStackLayout Spacing="9">
        <Border WidthRequest="18" HeightRequest="18" BackgroundColor="{StaticResource TzGreenMid}"
                Stroke="{StaticResource TzGreenMid}" StrokeThickness="1.5"
                StrokeShape="RoundRectangle 5" Padding="0" VerticalOptions="Center">
            <Label Text="✓" FontSize="11" TextColor="{StaticResource TzSurface}"
                   HorizontalOptions="Center" VerticalOptions="Center"/>
        </Border>
        <Label Text="Also add this to the Food Stories collection" VerticalOptions="Center"
               FontFamily="ArchivoRegular" FontSize="11.5" LineHeight="1.5"
               TextColor="{StaticResource TzMuted}"/>
    </HorizontalStackLayout>
</VerticalStackLayout>

<!-- Step 4: Who can see it -->
<VerticalStackLayout Padding="20" Spacing="11" IsVisible="{Binding IsStep4}">
    <VerticalStackLayout Spacing="11" BindableLayout.ItemsSource="{Binding PrivacyOptions}">
        <BindableLayout.ItemTemplate>
            <DataTemplate x:DataType="vm:PrivacyOptionViewModel">
                <Border StrokeThickness="1.5" StrokeShape="RoundRectangle 16" Padding="15">
                    <Border.Triggers>
                        <DataTrigger TargetType="Border" Binding="{Binding IsSelected}" Value="True">
                            <Setter Property="Stroke" Value="{StaticResource TzGreenDeep}"/>
                            <Setter Property="BackgroundColor" Value="{StaticResource TzGreenTint}"/>
                        </DataTrigger>
                        <DataTrigger TargetType="Border" Binding="{Binding IsSelected}" Value="False">
                            <Setter Property="Stroke" Value="{StaticResource TzBorder}"/>
                            <Setter Property="BackgroundColor" Value="{StaticResource TzSurface}"/>
                        </DataTrigger>
                    </Border.Triggers>

                    <Grid ColumnDefinitions="Auto,*" ColumnSpacing="12">
                        <Border WidthRequest="19" HeightRequest="19" StrokeThickness="1.5"
                                StrokeShape="RoundRectangle 9.5" Padding="0" Margin="0,1,0,0"
                                VerticalOptions="Start" BackgroundColor="Transparent">
                            <Border.Triggers>
                                <DataTrigger TargetType="Border" Binding="{Binding IsSelected}" Value="True">
                                    <Setter Property="Stroke" Value="{StaticResource TzGreenDeep}"/>
                                </DataTrigger>
                                <DataTrigger TargetType="Border" Binding="{Binding IsSelected}" Value="False">
                                    <Setter Property="Stroke" Value="{StaticResource TzBorderStrong}"/>
                                </DataTrigger>
                            </Border.Triggers>
                            <BoxView WidthRequest="9" HeightRequest="9" CornerRadius="4.5"
                                     IsVisible="{Binding IsSelected}"
                                     Color="{StaticResource TzGreenDeep}"
                                     HorizontalOptions="Center" VerticalOptions="Center"/>
                        </Border>

                        <VerticalStackLayout Grid.Column="1" Spacing="5">
                            <Label Text="{Binding Label}" FontFamily="ArchivoSemiBold" FontSize="13.5"
                                   LineHeight="1.2" TextColor="{StaticResource TzInk}"/>
                            <Label Text="{Binding Note}" FontFamily="ArchivoRegular" FontSize="11.5"
                                   LineHeight="1.5" TextColor="{StaticResource TzBodyMuted}"/>
                        </VerticalStackLayout>
                    </Grid>

                    <Border.GestureRecognizers>
                        <TapGestureRecognizer Command="{Binding SelectCommand}"/>
                    </Border.GestureRecognizers>
                </Border>
            </DataTemplate>
        </BindableLayout.ItemTemplate>
    </VerticalStackLayout>

    <Border Margin="0,5,0,0" BackgroundColor="{StaticResource TzGreenDeep}" StrokeThickness="0"
            StrokeShape="RoundRectangle 16" Padding="15">
        <Label Text="Public submissions are read by the archive team, checked against regional sources, and credited to you and to the person who taught you. Nothing is edited without asking."
               FontFamily="ArchivoRegular" FontSize="12" LineHeight="1.65"
               TextColor="{StaticResource TzOnDark84}"/>
    </Border>
</VerticalStackLayout>
```

The page header uses kicker "Living archive", title "Preserve a Family Recipe", and intro "Write down a recipe the way it was taught to you, so it survives past the person who taught it." The bottom nav is `ActiveSection="profile"`.

- [ ] **Step 6: Write the code-behind, register and verify**

`FamilyPage.xaml.cs` follows `HomePage.xaml.cs`. Register `FamilyViewModel` and `FamilyPage` as transient.

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Compare with frame 10. Check: step 4 shows three radio cards with "Shared with family" pre-selected in `TzGreenTint` with a filled dot; tapping "Public in the archive" moves both ring and fill; the button reads "Save to the archive".

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(mobile): implement Preserve a Family Recipe wizard"
```

---

## Task 17: Profile (frame 11)

**Layout:** deep-green header (62pt avatar with a gold ring, 26pt serif name, location · languages, four gold stat figures) · two side-by-side action cards (gold "Share a Recipe", outlined "Preserve a Family Recipe") · "My Collections" list with coloured left-edge swatches · "My Contributions" table with status badges · "Settings" table with chevrons · 26pt spacer.

**Files:**
- Create: `TasteZambia.Core/ViewModels/ProfileViewModel.cs`
- Create: `TasteZambia.Mobile/Views/ProfilePage.xaml` + `.xaml.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/ProfileViewModelTests.cs`

**Interfaces:**
- Consumes: `IProfileRepository`, `INavigationService`.
- Produces: `ProfileViewModel` — `Name`, `Meta`, `AvatarAsset`, `CookedCount`, `FavouriteCount`, `ContributedCount`, `PreservedCount`, `Collections`, `Contributions` (`ContributionRowViewModel`), `SettingsRows`, `OpenShareCommand`, `OpenFamilyCommand`. `ContributionRowViewModel` — `Name`, `StatusLabel`, `StatusBackgroundHex`, `StatusTextHex`, `Meta`.

- [ ] **Step 1: Write the failing test**

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class ProfileViewModelTests
{
    private sealed class Nav : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoBackAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task Initialize_LoadsChandaAndAllFourLists()
    {
        var vm = new ProfileViewModel(new InMemoryProfileRepository(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal("Chanda Mwaba", vm.Name);
        Assert.Equal("Kitwe, Copperbelt · Bemba, English", vm.Meta);
        Assert.Equal(23, vm.CookedCount);
        Assert.Equal(4, vm.Collections.Count);
        Assert.Equal(3, vm.Contributions.Count);
        Assert.Equal(5, vm.SettingsRows.Count);
    }

    [Fact]
    public async Task ContributionStatusesGetTheirDesignColours()
    {
        var vm = new ProfileViewModel(new InMemoryProfileRepository(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal("Published", vm.Contributions[0].StatusLabel);
        Assert.Equal("#EEF2EC", vm.Contributions[0].StatusBackgroundHex);
        Assert.Equal("#2F6A4D", vm.Contributions[0].StatusTextHex);

        Assert.Equal("In review", vm.Contributions[1].StatusLabel);
        Assert.Equal("#F7EEDA", vm.Contributions[1].StatusBackgroundHex);

        Assert.Equal("Draft", vm.Contributions[2].StatusLabel);
        Assert.Equal("#F0ECE4", vm.Contributions[2].StatusBackgroundHex);
    }

    [Fact]
    public async Task ActionCardsNavigateToBothContributionFlows()
    {
        var nav = new Nav();
        var vm = new ProfileViewModel(new InMemoryProfileRepository(), nav);
        await vm.InitializeAsync();

        vm.OpenShareCommand.Execute(null);
        vm.OpenFamilyCommand.Execute(null);

        Assert.Equal(["share", "family"], nav.Routes);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~ProfileViewModelTests`
Expected: FAIL — `ProfileViewModel` does not exist.

- [ ] **Step 3: Write the ViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed class ContributionRowViewModel(Contribution contribution)
{
    public string Name { get; } = contribution.Name;
    public string Meta { get; } = contribution.Meta;

    public string StatusLabel { get; } = contribution.Status switch
    {
        ContributionStatus.Published => "Published",
        ContributionStatus.InReview => "In review",
        _ => "Draft",
    };

    public string StatusBackgroundHex { get; } = contribution.Status switch
    {
        ContributionStatus.Published => "#EEF2EC",
        ContributionStatus.InReview => "#F7EEDA",
        _ => "#F0ECE4",
    };

    public string StatusTextHex { get; } = contribution.Status switch
    {
        ContributionStatus.Published => "#2F6A4D",
        ContributionStatus.InReview => "#7A5A10",
        _ => "#6B5C4A",
    };
}

public sealed partial class ProfileViewModel(
    IProfileRepository profiles,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<RecipeCollection> Collections { get; } = [];
    public ObservableCollection<ContributionRowViewModel> Contributions { get; } = [];
    public ObservableCollection<string> SettingsRows { get; } = [];

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _meta = "";
    [ObservableProperty] private string _avatarAsset = "";
    [ObservableProperty] private int _cookedCount;
    [ObservableProperty] private int _favouriteCount;
    [ObservableProperty] private int _contributedCount;
    [ObservableProperty] private int _preservedCount;

    public override async Task InitializeAsync()
    {
        if (Collections.Count > 0) return;

        var profile = await profiles.GetAsync();

        Name = profile.Name;
        Meta = $"{profile.Location} · {profile.Languages}";
        AvatarAsset = profile.AvatarAsset;
        CookedCount = profile.CookedCount;
        FavouriteCount = profile.FavouriteCount;
        ContributedCount = profile.ContributedCount;
        PreservedCount = profile.PreservedCount;

        foreach (var collection in await profiles.GetCollectionsAsync())
            Collections.Add(collection);

        foreach (var contribution in await profiles.GetContributionsAsync())
            Contributions.Add(new ContributionRowViewModel(contribution));

        foreach (var row in SeedData.SettingsRows)
            SettingsRows.Add(row);
    }

    [RelayCommand]
    private Task OpenShare() => Navigation.GoToAsync("share");

    [RelayCommand]
    private Task OpenFamily() => Navigation.GoToAsync("family");
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~ProfileViewModelTests`
Expected: PASS, 3 tests.

- [ ] **Step 5: Write `ProfilePage.xaml`**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TasteZambia.Mobile.Controls"
             xmlns:vm="clr-namespace:TasteZambia.Core.ViewModels;assembly=TasteZambia.Core"
             xmlns:models="clr-namespace:TasteZambia.Core.Models;assembly=TasteZambia.Core"
             xmlns:sys="clr-namespace:System;assembly=netstandard"
             x:Class="TasteZambia.Mobile.Views.ProfilePage"
             x:DataType="vm:ProfileViewModel">

    <Grid RowDefinitions="*,Auto">
        <ScrollView Grid.Row="0">
            <VerticalStackLayout Spacing="0">

                <!-- Header -->
                <VerticalStackLayout Padding="20,24" BackgroundColor="{StaticResource TzGreenDeep}" Spacing="18">
                    <Grid ColumnDefinitions="Auto,*" ColumnSpacing="14">
                        <Border WidthRequest="62" HeightRequest="62" Stroke="#99E8BD77" StrokeThickness="2"
                                StrokeShape="RoundRectangle 31" Padding="0">
                            <Image Source="{Binding AvatarAsset}" Aspect="AspectFill"/>
                        </Border>
                        <VerticalStackLayout Grid.Column="1" Spacing="4" VerticalOptions="Center">
                            <Label Text="{Binding Name}" FontFamily="NewsreaderSemiBold" FontSize="26"
                                   LineHeight="1.05" TextColor="{StaticResource TzSurface}"/>
                            <Label Text="{Binding Meta}" FontFamily="ArchivoRegular" FontSize="12"
                                   LineHeight="1.4" TextColor="{StaticResource TzOnDark66}"/>
                        </VerticalStackLayout>
                    </Grid>

                    <Grid ColumnDefinitions="*,*,*,*" ColumnSpacing="14">
                        <VerticalStackLayout Spacing="2">
                            <Label Text="{Binding CookedCount}" FontFamily="NewsreaderSemiBold" FontSize="22"
                                   TextColor="{StaticResource TzGoldLight}"/>
                            <Label Text="Cooked" FontFamily="ArchivoRegular" FontSize="10.5" LineHeight="1.3"
                                   TextColor="{StaticResource TzOnDark60}"/>
                        </VerticalStackLayout>
                        <VerticalStackLayout Grid.Column="1" Spacing="2">
                            <Label Text="{Binding FavouriteCount}" FontFamily="NewsreaderSemiBold" FontSize="22"
                                   TextColor="{StaticResource TzGoldLight}"/>
                            <Label Text="Favourites" FontFamily="ArchivoRegular" FontSize="10.5" LineHeight="1.3"
                                   TextColor="{StaticResource TzOnDark60}"/>
                        </VerticalStackLayout>
                        <VerticalStackLayout Grid.Column="2" Spacing="2">
                            <Label Text="{Binding ContributedCount}" FontFamily="NewsreaderSemiBold" FontSize="22"
                                   TextColor="{StaticResource TzGoldLight}"/>
                            <Label Text="Contributed" FontFamily="ArchivoRegular" FontSize="10.5" LineHeight="1.3"
                                   TextColor="{StaticResource TzOnDark60}"/>
                        </VerticalStackLayout>
                        <VerticalStackLayout Grid.Column="3" Spacing="2">
                            <Label Text="{Binding PreservedCount}" FontFamily="NewsreaderSemiBold" FontSize="22"
                                   TextColor="{StaticResource TzGoldLight}"/>
                            <Label Text="Preserved" FontFamily="ArchivoRegular" FontSize="10.5" LineHeight="1.3"
                                   TextColor="{StaticResource TzOnDark60}"/>
                        </VerticalStackLayout>
                    </Grid>
                </VerticalStackLayout>

                <!-- Action cards -->
                <Grid Padding="20,20,20,0" ColumnDefinitions="*,*" ColumnSpacing="11">
                    <Border BackgroundColor="{StaticResource TzGold}" StrokeThickness="0"
                            StrokeShape="RoundRectangle 16" Padding="14,15">
                        <VerticalStackLayout Spacing="4">
                            <Label Text="Share a Recipe" FontFamily="ArchivoSemiBold" FontSize="13"
                                   LineHeight="1.25" TextColor="{StaticResource TzSurface}"/>
                            <Label Text="Add to the public archive" FontFamily="ArchivoRegular" FontSize="10.5"
                                   LineHeight="1.4" TextColor="{StaticResource TzOnDark82}"/>
                        </VerticalStackLayout>
                        <Border.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding OpenShareCommand}"/>
                        </Border.GestureRecognizers>
                    </Border>

                    <Border Grid.Column="1" BackgroundColor="Transparent" Stroke="{StaticResource TzBorderStrong}"
                            StrokeThickness="1" StrokeShape="RoundRectangle 16" Padding="14,15">
                        <VerticalStackLayout Spacing="4">
                            <Label Text="Preserve a Family Recipe" FontFamily="ArchivoSemiBold" FontSize="13"
                                   LineHeight="1.25" TextColor="{StaticResource TzGreenDeep}"/>
                            <Label Text="Private, family or public" FontFamily="ArchivoRegular" FontSize="10.5"
                                   LineHeight="1.4" TextColor="{StaticResource TzMuted}"/>
                        </VerticalStackLayout>
                        <Border.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding OpenFamilyCommand}"/>
                        </Border.GestureRecognizers>
                    </Border>
                </Grid>

                <!-- My Collections -->
                <VerticalStackLayout Padding="20,22,20,0" Spacing="12">
                    <Label Text="My Collections" Style="{StaticResource SubsectionTitle}"/>
                    <VerticalStackLayout Spacing="9" BindableLayout.ItemsSource="{Binding Collections}">
                        <BindableLayout.ItemTemplate>
                            <DataTemplate x:DataType="models:RecipeCollection">
                                <Border Style="{StaticResource Card}" StrokeShape="RoundRectangle 15" Padding="15,14">
                                    <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="13">
                                        <BoxView WidthRequest="9" HeightRequest="36" CornerRadius="5"
                                                 Color="{Binding Tint, Converter={StaticResource HexToColor}}"/>
                                        <VerticalStackLayout Grid.Column="1" Spacing="3" VerticalOptions="Center">
                                            <Label Text="{Binding Label}" FontFamily="ArchivoSemiBold" FontSize="13.5"
                                                   LineHeight="1.25" TextColor="{StaticResource TzInk}"/>
                                            <Label Text="{Binding CountLabel}" Style="{StaticResource MetaText}"/>
                                        </VerticalStackLayout>
                                        <Label Grid.Column="2" Text="›" FontSize="15" VerticalOptions="Center"
                                               TextColor="{StaticResource TzChevron}"/>
                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </BindableLayout.ItemTemplate>
                    </VerticalStackLayout>
                </VerticalStackLayout>

                <!-- My Contributions -->
                <VerticalStackLayout Padding="20,24,20,0" Spacing="12">
                    <Label Text="My Contributions" Style="{StaticResource SubsectionTitle}"/>
                    <Border Stroke="{StaticResource TzHairline}" StrokeThickness="1"
                            StrokeShape="RoundRectangle 16" Padding="0" BackgroundColor="{StaticResource TzSurface}">
                        <VerticalStackLayout BindableLayout.ItemsSource="{Binding Contributions}">
                            <BindableLayout.ItemTemplate>
                                <DataTemplate x:DataType="vm:ContributionRowViewModel">
                                    <VerticalStackLayout Padding="15,14" Spacing="5">
                                        <Grid ColumnDefinitions="*,Auto" ColumnSpacing="10">
                                            <Label Text="{Binding Name}" FontFamily="NewsreaderSemiBold" FontSize="17"
                                                   LineHeight="1.15" TextColor="{StaticResource TzInk}"/>
                                            <controls:PillLabel Grid.Column="1" Text="{Binding StatusLabel}"
                                                                PillBackground="{Binding StatusBackgroundHex, Converter={StaticResource HexToColor}}"
                                                                TextColor="{Binding StatusTextHex, Converter={StaticResource HexToColor}}"/>
                                        </Grid>
                                        <Label Text="{Binding Meta}" Style="{StaticResource MetaText}"/>
                                        <BoxView HeightRequest="1" Margin="0,9,0,-14"
                                                 Color="{StaticResource TzHairlineSoft}"/>
                                    </VerticalStackLayout>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                        </VerticalStackLayout>
                    </Border>
                </VerticalStackLayout>

                <!-- Settings -->
                <VerticalStackLayout Padding="20,24,20,0" Spacing="12">
                    <Label Text="Settings" Style="{StaticResource SubsectionTitle}"/>
                    <Border Stroke="{StaticResource TzHairline}" StrokeThickness="1"
                            StrokeShape="RoundRectangle 16" Padding="0" BackgroundColor="{StaticResource TzSurface}">
                        <VerticalStackLayout BindableLayout.ItemsSource="{Binding SettingsRows}">
                            <BindableLayout.ItemTemplate>
                                <DataTemplate x:DataType="sys:String">
                                    <Grid Padding="15,14" ColumnDefinitions="*,Auto">
                                        <Label Text="{Binding}" FontFamily="ArchivoMedium" FontSize="13"
                                               TextColor="{StaticResource TzBody}" VerticalOptions="Center"/>
                                        <Label Grid.Column="1" Text="›" FontSize="15" VerticalOptions="Center"
                                               TextColor="{StaticResource TzChevron}"/>
                                        <BoxView Grid.ColumnSpan="2" HeightRequest="1" VerticalOptions="End"
                                                 Margin="0,0,0,-14" Color="{StaticResource TzHairlineSoft}"/>
                                    </Grid>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                        </VerticalStackLayout>
                    </Border>
                </VerticalStackLayout>

                <BoxView HeightRequest="26" Color="Transparent"/>
            </VerticalStackLayout>
        </ScrollView>

        <controls:BottomNavBar Grid.Row="1" ActiveSection="profile"/>
    </Grid>
</ContentPage>
```

- [ ] **Step 6: Write the code-behind, register and verify**

`ProfilePage.xaml.cs` follows `HomePage.xaml.cs`. Register `ProfileViewModel` and `ProfilePage` as transient.

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Compare with frame 11. Check: the four stat numbers are `TzGoldLight` serif on deep green; the collection swatches are clay / gold / green-mid / green-deep top to bottom; "In review" is gold-tinted and "Draft" is grey-tinted; both action cards navigate and the nav bar stays on Profile.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(mobile): implement Profile screen"
```

---

## Task 18: Safe areas, accessibility and final verification

The design canvas draws a bare 390x812 rectangle with no status bar and no home indicator. On real hardware the hero images must bleed under the status bar while their controls stay tappable, and the bottom nav must sit above the home indicator.

**Files:**
- Modify: `TasteZambia.Mobile/Views/HomePage.xaml`, `ExplorePage.xaml`, `RecipePage.xaml`, `IngredientsPage.xaml`, `IngredientPage.xaml`, `RegionsPage.xaml`, `CulturePage.xaml`, `StoryPage.xaml`, `SharePage.xaml`, `FamilyPage.xaml`, `ProfilePage.xaml`
- Modify: `TasteZambia.Mobile/Controls/BottomNavBar.xaml`
- Modify: `TasteZambia.Mobile/Resources/Styles/Styles.xaml`
- Create: `Docs/plans/known-gaps.md`

**Interfaces:**
- Consumes: every page from Tasks 7–17.
- Produces: no new public API. This task hardens what exists.

- [ ] **Step 1: Set the safe-area behaviour**

In .NET 10, `ContentPage` defaults to `SafeAreaEdges="None"` (edge-to-edge) on every platform, and `SafeAreaEdges` replaces the old `Page.UseSafeArea` and `Layout.IgnoreSafeArea`.

Three page shapes, three treatments:

**a) Pages whose first element is a hero image** — `RecipePage`, `IngredientPage`, `StoryPage`. Keep the page edge-to-edge so the photo bleeds under the status bar, but pad the back/save circles down. Change each hero's control row margin from `16` to:

```xml
<Grid Margin="16,0,16,0" Padding="0,16,0,0" VerticalOptions="Start" SafeAreaEdges="Top" ...>
```

**b) Pages whose first element is content** — `HomePage`, `ExplorePage`, `IngredientsPage`, `RegionsPage`, `CulturePage`, `SharePage`, `FamilyPage`, `ProfilePage`. Add `SafeAreaEdges="Top"` to the root `Grid` so the header clears the status bar. On `ExplorePage` and `ProfilePage` the first element is a coloured panel, so instead set the root `Grid`'s `BackgroundColor` to `{StaticResource TzGreenDeep}` and put `SafeAreaEdges="Top"` on the `ScrollView`, keeping the green running to the top edge.

**c) The bottom nav on every page.** In `BottomNavBar.xaml`, wrap the existing `Grid` so the bar's cream ground extends into the home-indicator area while its content stays above it:

```xml
<Grid BackgroundColor="{StaticResource TzSurface}" SafeAreaEdges="Bottom">
    <!-- existing HeightRequest="66" Grid goes here, unchanged -->
</Grid>
```

- [ ] **Step 2: Add semantic descriptions to every interactive element**

Screen readers currently announce the heart as "♥" and the chevron as "›". Fix the glyph-only and image-only controls.

In `Controls/CircleButton.xaml.cs`, add a bindable `SemanticLabel` and apply it:

```csharp
public static readonly BindableProperty SemanticLabelProperty =
    BindableProperty.Create(nameof(SemanticLabel), typeof(string), typeof(CircleButton), null,
        propertyChanged: (b, _, n) => SemanticProperties.SetDescription((CircleButton)b, (string?)n ?? ""));

public string? SemanticLabel
{
    get => (string?)GetValue(SemanticLabelProperty);
    set => SetValue(SemanticLabelProperty, value);
}
```

Then set it everywhere a `CircleButton` is used:
- Back circles: `SemanticLabel="Go back"`
- Sheet close: `SemanticLabel="Close ingredient details"`
- Home search: `SemanticLabel="Search recipes"`

Add a `SaveSemanticLabel` to `DishItemViewModel` and `RecipeViewModel` so the state is spoken:

```csharp
public string SaveSemanticLabel => IsSaved ? $"Remove {Title} from saved" : $"Save {Title}";
```

Add `[NotifyPropertyChangedFor(nameof(SaveSemanticLabel))]` to the `_isSaved` field in both, and bind it: `SemanticProperties.Description="{Binding SaveSemanticLabel}"` on each heart.

Mark the screen titles as headings so rotor navigation works. In `Styles.xaml`, add to the `DisplayTitle` and `HeroTitle` styles:

```xml
<Setter Property="SemanticProperties.HeadingLevel" Value="Level1"/>
```

and to `SectionTitle` and `SubsectionTitle`:

```xml
<Setter Property="SemanticProperties.HeadingLevel" Value="Level2"/>
```

For cooking steps, describe the checkbox state on the step `Border`:

```xml
SemanticProperties.Description="{Binding StepTitle}"
SemanticProperties.Hint="Double tap to mark this step done"
```

- [ ] **Step 3: Verify the full test suite still passes**

Run: `dotnet test TasteZambia.Core.Tests`
Expected: PASS. Total across all tasks: **3 + 5 + 16 + 2 + 4 + 6 + 7 + 3 + 3 + 4 + 2 + 4 + 6 + 5 + 3 = 73 tests**, 0 failures.

- [ ] **Step 4: Build every target framework**

```bash
cd "/Users/zimbadev/Documents/Workspace/Maui Projects/TasteZambia"
dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios
dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-android
dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-maccatalyst
```

Expected: three successful builds, no warnings about missing fonts or images. Do not report the UI complete until all three succeed — record the exact output.

- [ ] **Step 5: Walk every screen against the design canvas**

Open `Docs/Mobile app design project/Taste Zambia.dc.html` in a browser next to the running simulator and walk this list. Each line is pass/fail, and a fail is a bug to fix before the task closes.

| # | Frame | Check |
|---|---|---|
| 01 | Home | Hero 340pt, gold CTA, three stats, both rails scroll horizontally, five striped dish cards |
| 02 | Explore | Green header, clear "×" appears with text, chips move selection, "1 recipe" singular, empty state on "sushi" |
| 03 | Recipe | Verified badge, 40pt serif title, linked ingredients green + underlined, steps tint on tap, method switcher swaps all text, sheet slides over scrim |
| 04 | Ingredients | Two even columns, only Chibwabwa photographed, gold only on "Contribute a name" |
| 05 | Ingredient | Deep-green local-names panel, gold "Suggest →", three used-in dishes |
| 06 | Regions | Opens on Northern, striped map with both caption lines, province swap updates every field, Katapa keeps its subtitle |
| 07 | Culture | Lead card with captioned stripe, five rows with gold kickers, deep-green recording panel |
| 08 | Story | Serif lede larger than body, italic serif pull-quote on cream, audio track ~18% |
| 09 | Share | Track advances per step, no Back on step 1, green ingredient names, submit → deep-green confirmation |
| 10 | Family | Four step titles correct, privacy radios, "Save to the archive" on step 4 |
| 11 | Profile | Gold stat numbers, four swatch colours, three status badge colours, both action cards navigate |
| all | Nav bar | 66pt, clay dot over active label, detail screens keep their parent tab lit (recipe → Explore, story → Culture, share/family → Profile) |
| all | Type | Every dish/ingredient/province/article title is Newsreader; every English subtitle is Archivo `TzMuted` |
| all | Gold | Gold appears on actions only — never as decoration |

- [ ] **Step 6: Record the known gaps**

Create `Docs/plans/known-gaps.md`:

```markdown
# Taste Zambia — known gaps after the UI phase

These are carried forward deliberately from the design. None is a defect.

1. **Province map** — the Regions screen ships a labelled striped placeholder.
   Needs real provincial boundary data before anything is drawn.
2. **Photography** — only ifisashi, nshima, chikanda, market ingredients and the
   avatar exist. Kapenta, inkoko, kandolo, munkoyo, delele, every category
   without an image, and every story image use the striped placeholder with a
   caption naming the shot needed.
3. **Detail coverage** — only Ifisashi has a full recipe and only Chibwabwa a
   full ingredient profile, matching the design. Other ids route correctly but
   resolve to nothing.
4. **Article bodies** — only "The History of Nshima" has body copy. The other
   five articles have titles and bylines only.
5. **Wizard fields are read-only.** Both contribution wizards present the
   pre-filled draft in field-shaped cards, exactly as the design does. Swapping
   the Labels for Entry/Editor and wiring two-way binding to ContributionDraft
   is the next iteration.
6. **App icon** — still the .NET template vector. Replacing it needs `logo-pot`
   as an SVG; we only have a 531x519 PNG.
7. **Photo picker** — the "+ add photo" tiles are inert. Wiring MediaPicker is
   part of the contribution feature, not the UI phase.
8. **Audio** — the story player and both "record an elder" CTAs are visual only.
9. **Filter chips** — selecting a chip moves the selection but does not narrow
   results. `ICatalogService.SearchAsync` already takes the filter argument so
   the ViewModels will not change when the API implements it server-side.
```

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(mobile): add safe-area handling, accessibility semantics and known-gaps record

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Swapping in the real API later

When the backend plan lands, this is the entire mobile-side change:

1. Add `TasteZambia.Core/Data/Http/HttpDishRepository.cs` (and siblings) implementing the **same** interfaces from Task 3 against `HttpClient`.
2. In `MauiProgram.cs`, change six lines:

```csharp
builder.Services.AddSingleton<IDishRepository, HttpDishRepository>();
builder.Services.AddSingleton<IIngredientRepository, HttpIngredientRepository>();
builder.Services.AddSingleton<IRegionRepository, HttpRegionRepository>();
builder.Services.AddSingleton<IArticleRepository, HttpArticleRepository>();
builder.Services.AddSingleton<ICategoryRepository, HttpCategoryRepository>();
builder.Services.AddSingleton<IProfileRepository, HttpProfileRepository>();
```

3. Keep `InMemory*Repository` registered in the test project so the 73 ViewModel and service tests keep running offline.

No View, no ViewModel and no service changes. `SeedData` stays as the offline/first-run fixture.

---

## Self-Review

**Spec coverage.** All eleven frames map to a task: Home → 7, Explore → 8, Recipe → 9, Ingredients → 10, Ingredient → 11, Regions → 12, Culture → 13, Story → 14, Share → 15, Family → 16, Profile → 17. The ingredient bottom sheet is inside Task 9 because it only exists on the recipe screen. The bottom nav and its detail-screen section mapping are Task 5. The design's two exposed props — `titleLanguage` and `showVerification` — are `IPreferenceService.TitleLanguage` and `ShowVerificationBadge` (Task 4), consumed by `DishItemViewModel`, `IngredientTileViewModel`, `IngredientViewModel` and `RecipeViewModel`. All three design-declared open items are recorded in Global Constraints and again in `known-gaps.md`.

**Placeholder scan.** No "TBD", no "similar to Task N" standing in for code, no "add error handling". Task 16 does say to copy the `FieldCard` style and button row from Task 15 — that is a deliberate duplication instruction with the source named, not an omission, and both files are listed under Files.

**Type consistency.** `DishItemViewModel` is constructed identically in Tasks 7, 8, 11 and 14 as `(Dish, IFavouritesService, IPreferenceService, INavigationService)`. `LocalNameRow` and `UsedInRow` are declared once in Task 9 and reused by Task 11. `SeedData.ShareSteps` / `FamilySteps` are declared in Task 3 and read in Tasks 15 / 16. `INavigationService.GoToAsync(route, parameters)` matches every call site. Route strings (`recipe`, `ingredient`, `ingredients`, `story`, `share`, `family`, `//home`, `//explore`, `//regions`, `//culture`, `//profile`) match `AppShell` registration in Task 5. Query keys match the `[QueryProperty]` attributes: `dishId` → `RecipePage`, `key` → `IngredientPage`, `articleId` → `StoryPage`.

**One risk flagged for the executor.** `RegionsViewModel.SelectAsync` awaits inside a loop over `dishes.GetAllAsync()`. With the in-memory repository this is free; against HTTP it would be N+1. Hoist the `GetAllAsync()` call above the loop when the API arrives.

---

# Part 2 — Design v2 (screens 12–33)

The canvas grew from 11 screens to 33. Tasks 1–18 above cover row 2 of the canvas
(`home`…`profile`) plus the two contribution forms; the tokens in Task 1 have already been
corrected for v2's contrast pass. Tasks 19–23 cover the remaining 22 screens.

See `Docs/plans/2026-09-08-design-v2-delta.md` for the full v1→v2 diff.

## Global additions for Part 2

**Onboarding runs before Shell.** The seven onboarding screens show **no bottom nav**.
They are hosted by a separate `OnboardingShell`, and only on completion is
`Application.Current.Windows[0].Page` swapped to `AppShell`.

**Two new colour roles** (already in `Colors.xaml`): `TzTimelineIdle` `#D8CDB9` for a
not-yet-reached timeline dot, `TzClayText` `#8F3B23` for the "changes requested" badge.

**A four-segment step rail** appears on onboarding screens 3–6 — four `flex:1` bars, 3pt
tall, 2pt radius, 6pt gap. Filled segments are `TzGoldDecor` on light grounds and
`TzGoldLight` on the dark green ground; unfilled are `TzRule` on light, `TzOnDark20`
(`#33FFFDF9`, add to `Colors.xaml`) on dark.

---

## Task 19: Onboarding — seven screens, pre-Shell (frames 01–07)

| Screen | Route | Ground | Notes |
|---|---|---|---|
| `splash` | `splash` | Deep green, cross-hatched | 104pt logo, 38pt serif wordmark, no controls |
| `intro` | `intro` | 430pt photo fading to cream | "Skip" pill, three bullet promises, "Get started" |
| `onbLang` | `onbLang` | Deep green | Language list, gold check on selection, rail 1/4 |
| `onbWho` | `onbWho` | Cream | Radio list, rail 2/4 |
| `onbTaste` | `onbTaste` | Cream | Multi-select chips + "N selected", rail 3/4 |
| `onbNotify` | `onbNotify` | Cream | Two toggles + privacy panel, rail 4/4 |
| `onbReady` | `onbReady` | Deep green | Summary of picks, "Start exploring" → Shell |

**Files:**
- Create: `TasteZambia.Core/Models/Onboarding.cs`
- Create: `TasteZambia.Core/Services/OnboardingService.cs`
- Create: `TasteZambia.Core/ViewModels/OnboardingViewModel.cs`
- Create: `TasteZambia.Mobile/Views/Onboarding/` — `SplashPage`, `IntroPage`, `OnbLanguagePage`, `OnbWhoPage`, `OnbTastePage`, `OnbNotifyPage`, `OnbReadyPage` (`.xaml` + `.xaml.cs` each)
- Create: `TasteZambia.Mobile/OnboardingShell.xaml` + `.xaml.cs`
- Create: `TasteZambia.Mobile/Controls/StepRail.xaml` + `.xaml.cs`
- Modify: `TasteZambia.Mobile/App.xaml.cs`, `MauiProgram.cs`, `Resources/Styles/Colors.xaml`
- Test: `TasteZambia.Core.Tests/Services/OnboardingServiceTests.cs`, `ViewModels/OnboardingViewModelTests.cs`

**Interfaces:**
- Produces:
  - `ReadingLanguage(string Name, string Note)`, `VisitorKind(string Key, string Note)`, `TasteOption(string Key, string Label)`
  - `IOnboardingService`: `bool IsComplete { get; }`, `void Complete(OnboardingChoices)`, `OnboardingChoices Current { get; }`, `IReadOnlyList<ReadingLanguage> Languages`, `IReadOnlyList<VisitorKind> VisitorKinds`, `IReadOnlyList<TasteOption> Tastes`
  - `OnboardingChoices` — `Language` (default `"English"`), `Who` (default `"I grew up here"`), `Tastes` (`HashSet<string>`, default `{traditional, veg}`), `OfflineEnabled` (true), `StoryNotifications` (false)
  - `OnboardingViewModel` — `Languages`/`VisitorKinds`/`TasteChips` item VMs, `TasteCountLabel`, `PickedLanguage`, `PickedWho`, commands `Skip`, `ToIntro`, `ToLanguage`, `ToWho`, `ToTaste`, `ToNotify`, `ToReady`, `Finish`
  - `StepRail` — bindable `Step` (int 1–4), `OnDark` (bool)

- [ ] **Step 1: Write the failing tests**

`TasteZambia.Core.Tests/Services/OnboardingServiceTests.cs`:

```csharp
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Services;

public class OnboardingServiceTests
{
    [Fact]
    public void SixReadingLanguages_EnglishFirst()
    {
        var sut = new OnboardingService();
        Assert.Equal(6, sut.Languages.Count);
        Assert.Equal("English", sut.Languages[0].Name);
        Assert.Equal("Full interface", sut.Languages[0].Note);
        Assert.Equal("Icibemba", sut.Languages[1].Note);
    }

    [Fact]
    public void FourVisitorKinds_WithTheirExplanations()
    {
        var sut = new OnboardingService();
        Assert.Equal(4, sut.VisitorKinds.Count);
        Assert.Equal("I grew up here", sut.VisitorKinds[0].Key);
        Assert.Equal("I am learning to cook", sut.VisitorKinds[3].Key);
    }

    [Fact]
    public void SixTasteOptions()
    {
        Assert.Equal(6, new OnboardingService().Tastes.Count);
    }

    [Fact]
    public void Defaults_MatchTheDesignsInitialState()
    {
        var c = new OnboardingService().Current;
        Assert.Equal("English", c.Language);
        Assert.Equal("I grew up here", c.Who);
        Assert.Equal(["traditional", "veg"], c.Tastes.OrderBy(x => x));
        Assert.True(c.OfflineEnabled);
        Assert.False(c.StoryNotifications);
    }

    [Fact]
    public void IsComplete_FlipsOnlyAfterComplete()
    {
        var sut = new OnboardingService();
        Assert.False(sut.IsComplete);
        sut.Complete(sut.Current);
        Assert.True(sut.IsComplete);
    }
}
```

`TasteZambia.Core.Tests/ViewModels/OnboardingViewModelTests.cs`:

```csharp
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class OnboardingViewModelTests
{
    private sealed class Nav : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoBackAsync() => Task.CompletedTask;
    }

    private static OnboardingViewModel Sut(Nav? nav = null, IOnboardingService? svc = null)
        => new(svc ?? new OnboardingService(), nav ?? new Nav());

    [Fact]
    public async Task EnglishAndGrewUpHereAreSelectedInitially()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.True(vm.Languages.Single(l => l.Name == "English").IsSelected);
        Assert.True(vm.VisitorKinds[0].IsSelected);
        Assert.Equal("English", vm.PickedLanguage);
        Assert.Equal("I grew up here", vm.PickedWho);
    }

    [Fact]
    public async Task PickingALanguage_MovesTheSelectionAndTheSummary()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Languages[2].SelectCommand.Execute(null);

        Assert.True(vm.Languages[2].IsSelected);
        Assert.False(vm.Languages[0].IsSelected);
        Assert.Equal("Nyanja", vm.PickedLanguage);
    }

    [Fact]
    public async Task TasteChips_AreMultiSelectAndCount()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal("2 selected", vm.TasteCountLabel);

        vm.TasteChips.Single(t => t.Key == "quick").ToggleCommand.Execute(null);
        Assert.Equal("3 selected", vm.TasteCountLabel);

        vm.TasteChips.Single(t => t.Key == "traditional").ToggleCommand.Execute(null);
        Assert.Equal("2 selected", vm.TasteCountLabel);
    }

    [Fact]
    public async Task Finish_MarksOnboardingCompleteAndPersistsChoices()
    {
        var svc = new OnboardingService();
        var vm = Sut(svc: svc);
        await vm.InitializeAsync();
        vm.Languages[1].SelectCommand.Execute(null);

        await vm.FinishCommand.ExecuteAsync(null);

        Assert.True(svc.IsComplete);
        Assert.Equal("Bemba", svc.Current.Language);
    }

    [Fact]
    public async Task Skip_AlsoCompletesOnboarding()
    {
        var svc = new OnboardingService();
        var vm = Sut(svc: svc);
        await vm.InitializeAsync();

        await vm.SkipCommand.ExecuteAsync(null);

        Assert.True(svc.IsComplete);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test TasteZambia.Core.Tests --filter "FullyQualifiedName~Onboarding"`
Expected: FAIL — `OnboardingService` does not exist.

- [ ] **Step 3: Write the model and service**

`TasteZambia.Core/Models/Onboarding.cs`:

```csharp
namespace TasteZambia.Core.Models;

public sealed record ReadingLanguage(string Name, string Note);
public sealed record VisitorKind(string Key, string Note);
public sealed record TasteOption(string Key, string Label);

public sealed class OnboardingChoices
{
    public string Language { get; set; } = "English";
    public string Who { get; set; } = "I grew up here";
    public HashSet<string> Tastes { get; set; } = ["traditional", "veg"];
    public bool OfflineEnabled { get; set; } = true;
    public bool StoryNotifications { get; set; }
}
```

`TasteZambia.Core/Services/OnboardingService.cs`:

```csharp
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Services;

public interface IOnboardingService
{
    bool IsComplete { get; }
    OnboardingChoices Current { get; }
    void Complete(OnboardingChoices choices);
    IReadOnlyList<ReadingLanguage> Languages { get; }
    IReadOnlyList<VisitorKind> VisitorKinds { get; }
    IReadOnlyList<TasteOption> Tastes { get; }
}

public sealed class OnboardingService : IOnboardingService
{
    public bool IsComplete { get; private set; }
    public OnboardingChoices Current { get; private set; } = new();

    public void Complete(OnboardingChoices choices)
    {
        Current = choices;
        IsComplete = true;
    }

    public IReadOnlyList<ReadingLanguage> Languages { get; } =
    [
        new("English", "Full interface"),
        new("Bemba", "Icibemba"),
        new("Nyanja", "Chinyanja"),
        new("Tonga", "Chitonga"),
        new("Lozi", "Silozi"),
        new("Kaonde", "Kikaonde"),
    ];

    public IReadOnlyList<VisitorKind> VisitorKinds { get; } =
    [
        new("I grew up here", "Show me dishes from my province first, and the ones I might not know."),
        new("I live abroad", "Show me what I can cook with what is available where I am."),
        new("I am visiting Zambia", "Explain the dishes and what to expect when I eat them."),
        new("I am learning to cook", "Start me on the staples, with more detail in the steps."),
    ];

    public IReadOnlyList<TasteOption> Tastes { get; } =
    [
        new("traditional", "Traditional dishes"),
        new("quick", "Quick weekday meals"),
        new("veg", "Vegetarian relishes"),
        new("family", "Family recipes"),
        new("drinks", "Traditional drinks"),
        new("snacks", "Snacks and street food"),
    ];
}
```

> The service is in-memory for this phase, matching the repository rule: when persistence
> lands, back `IsComplete`/`Current` with `Preferences` or the API and change nothing else.

- [ ] **Step 4: Write `OnboardingViewModel`**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class LanguageOptionViewModel(
    ReadingLanguage language, Action<LanguageOptionViewModel> onSelect) : ObservableObject
{
    public string Name { get; } = language.Name;
    public string Note { get; } = language.Note;

    [ObservableProperty] private bool _isSelected;

    [RelayCommand]
    private void Select() => onSelect(this);
}

public sealed partial class VisitorKindViewModel(
    VisitorKind kind, Action<VisitorKindViewModel> onSelect) : ObservableObject
{
    public string Key { get; } = kind.Key;
    public string Note { get; } = kind.Note;

    [ObservableProperty] private bool _isSelected;

    [RelayCommand]
    private void Select() => onSelect(this);
}

public sealed partial class TasteChipViewModel(
    TasteOption option, Action onChanged) : ObservableObject
{
    public string Key { get; } = option.Key;
    public string Label { get; } = option.Label;

    [ObservableProperty] private bool _isSelected;

    [RelayCommand]
    private void Toggle()
    {
        IsSelected = !IsSelected;
        onChanged();
    }
}

public sealed partial class OnboardingViewModel(
    IOnboardingService onboarding,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<LanguageOptionViewModel> Languages { get; } = [];
    public ObservableCollection<VisitorKindViewModel> VisitorKinds { get; } = [];
    public ObservableCollection<TasteChipViewModel> TasteChips { get; } = [];

    [ObservableProperty] private string _pickedLanguage = "English";
    [ObservableProperty] private string _pickedWho = "I grew up here";
    [ObservableProperty] private string _tasteCountLabel = "";
    [ObservableProperty] private bool _offlineEnabled = true;
    [ObservableProperty] private bool _storyNotifications;

    public override Task InitializeAsync()
    {
        if (Languages.Count > 0) return Task.CompletedTask;

        var choices = onboarding.Current;

        foreach (var language in onboarding.Languages)
            Languages.Add(new LanguageOptionViewModel(language, SelectLanguage)
            { IsSelected = language.Name == choices.Language });

        foreach (var kind in onboarding.VisitorKinds)
            VisitorKinds.Add(new VisitorKindViewModel(kind, SelectWho)
            { IsSelected = kind.Key == choices.Who });

        foreach (var taste in onboarding.Tastes)
            TasteChips.Add(new TasteChipViewModel(taste, RefreshTasteCount)
            { IsSelected = choices.Tastes.Contains(taste.Key) });

        PickedLanguage = choices.Language;
        PickedWho = choices.Who;
        OfflineEnabled = choices.OfflineEnabled;
        StoryNotifications = choices.StoryNotifications;

        RefreshTasteCount();
        return Task.CompletedTask;
    }

    private void SelectLanguage(LanguageOptionViewModel picked)
    {
        foreach (var l in Languages) l.IsSelected = ReferenceEquals(l, picked);
        PickedLanguage = picked.Name;
    }

    private void SelectWho(VisitorKindViewModel picked)
    {
        foreach (var w in VisitorKinds) w.IsSelected = ReferenceEquals(w, picked);
        PickedWho = picked.Key;
    }

    private void RefreshTasteCount()
        => TasteCountLabel = $"{TasteChips.Count(t => t.IsSelected)} selected";

    private OnboardingChoices Snapshot() => new()
    {
        Language = PickedLanguage,
        Who = PickedWho,
        Tastes = [.. TasteChips.Where(t => t.IsSelected).Select(t => t.Key)],
        OfflineEnabled = OfflineEnabled,
        StoryNotifications = StoryNotifications,
    };

    [RelayCommand] private Task ToIntro()  => Navigation.GoToAsync("intro");
    [RelayCommand] private Task ToLanguage() => Navigation.GoToAsync("onbLang");
    [RelayCommand] private Task ToWho()    => Navigation.GoToAsync("onbWho");
    [RelayCommand] private Task ToTaste()  => Navigation.GoToAsync("onbTaste");
    [RelayCommand] private Task ToNotify() => Navigation.GoToAsync("onbNotify");
    [RelayCommand] private Task ToReady()  => Navigation.GoToAsync("onbReady");

    [RelayCommand]
    private Task Finish()
    {
        onboarding.Complete(Snapshot());
        return Navigation.GoToAsync("//home");
    }

    [RelayCommand]
    private Task Skip()
    {
        onboarding.Complete(Snapshot());
        return Navigation.GoToAsync("//home");
    }
}
```

All seven pages share this **one** ViewModel instance (registered `Singleton` for the
onboarding run) so the choices survive page-to-page navigation.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test TasteZambia.Core.Tests --filter "FullyQualifiedName~Onboarding"`
Expected: PASS, 10 tests.

- [ ] **Step 6: Write the `StepRail` control and the new dark-ground token**

Add to `Colors.xaml`:

```xml
<Color x:Key="TzOnDark20">#33FFFDF9</Color>
```

`TasteZambia.Mobile/Controls/StepRail.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TasteZambia.Mobile.Controls.StepRail"
             x:Name="Root">
    <Grid x:Name="Rail" ColumnDefinitions="*,*,*,*" ColumnSpacing="6" HeightRequest="3"/>
</ContentView>
```

```csharp
namespace TasteZambia.Mobile.Controls;

public partial class StepRail : ContentView
{
    public static readonly BindableProperty StepProperty =
        BindableProperty.Create(nameof(Step), typeof(int), typeof(StepRail), 1,
            propertyChanged: (b, _, _) => ((StepRail)b).Rebuild());

    public static readonly BindableProperty OnDarkProperty =
        BindableProperty.Create(nameof(OnDark), typeof(bool), typeof(StepRail), false,
            propertyChanged: (b, _, _) => ((StepRail)b).Rebuild());

    public int Step { get => (int)GetValue(StepProperty); set => SetValue(StepProperty, value); }
    public bool OnDark { get => (bool)GetValue(OnDarkProperty); set => SetValue(OnDarkProperty, value); }

    public StepRail()
    {
        InitializeComponent();
        Rebuild();
    }

    private void Rebuild()
    {
        Rail.Children.Clear();

        var filled = Get(OnDark ? "TzGoldLight" : "TzGoldDecor");
        var empty  = Get(OnDark ? "TzOnDark20" : "TzRule");

        for (var i = 0; i < 4; i++)
        {
            var bar = new BoxView
            {
                HeightRequest = 3,
                CornerRadius = 2,
                Color = i < Step ? filled : empty,
            };
            Rail.Add(bar, i, 0);
        }

        SemanticProperties.SetDescription(this, $"Step {Step} of 4");
    }

    private static Color Get(string key)
        => Application.Current!.Resources.TryGetValue(key, out var v) && v is Color c ? c : Colors.Gray;
}
```

- [ ] **Step 7: Write the seven pages**

Each is a `ContentPage` with **no `BottomNavBar`**. Full markup for the two that carry the
most structure; the remaining five follow the same vocabulary with the copy from the table
at the top of this task.

`Views/Onboarding/SplashPage.xaml` — deep green, cross-hatched, everything centred:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TasteZambia.Mobile.Views.Onboarding.SplashPage"
             BackgroundColor="{StaticResource TzGreenDeep}">

    <Grid Padding="40">
        <VerticalStackLayout VerticalOptions="Center" HorizontalOptions="Center" Spacing="0">
            <Image Source="logo.png" WidthRequest="104" HeightRequest="104"
                   Aspect="AspectFit" Margin="0,0,0,26"/>
            <Label Text="Taste Zambia" FontFamily="NewsreaderMedium" FontSize="38"
                   LineHeight="1" CharacterSpacing="-0.57"
                   TextColor="{StaticResource TzSurface}" HorizontalOptions="Center"/>
            <Label Text="Heritage &amp; Flavours" Margin="0,14,0,0"
                   FontFamily="PlexMonoMedium" FontSize="10" CharacterSpacing="2.0"
                   TextTransform="Uppercase" TextColor="{StaticResource TzGoldLight}"
                   HorizontalOptions="Center"/>
        </VerticalStackLayout>

        <Label Text="A living cultural archive of Zambian food"
               VerticalOptions="End" HorizontalTextAlignment="Center" Margin="0,0,0,44"
               FontFamily="ArchivoRegular" FontSize="11" LineHeight="1.6"
               TextColor="#9EFFFDF9"/>
    </Grid>
</ContentPage>
```

`Views/Onboarding/OnbLanguagePage.xaml` — the dark-ground selection list:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TasteZambia.Mobile.Controls"
             xmlns:vm="clr-namespace:TasteZambia.Core.ViewModels;assembly=TasteZambia.Core"
             x:Class="TasteZambia.Mobile.Views.Onboarding.OnbLanguagePage"
             x:DataType="vm:OnboardingViewModel"
             BackgroundColor="{StaticResource TzGreenDeep}">

    <Grid Padding="24,26,24,24" RowDefinitions="Auto,Auto,Auto,*,Auto" RowSpacing="0">

        <controls:StepRail Grid.Row="0" Step="1" OnDark="True" Margin="0,0,0,26"/>

        <Label Grid.Row="1" Text="Which language do you read in?"
               FontFamily="NewsreaderMedium" FontSize="31" LineHeight="1.08"
               CharacterSpacing="-0.47" TextColor="{StaticResource TzSurface}"/>

        <Label Grid.Row="2" Margin="0,11,0,0"
               Text="The interface is in English for now. Dish and ingredient names always appear in their own language, whichever you choose."
               FontFamily="ArchivoRegular" FontSize="12.5" LineHeight="1.65"
               TextColor="#B8FFFDF9"/>

        <ScrollView Grid.Row="3" Margin="0,20,0,0">
            <VerticalStackLayout Spacing="9" BindableLayout.ItemsSource="{Binding Languages}">
                <BindableLayout.ItemTemplate>
                    <DataTemplate x:DataType="vm:LanguageOptionViewModel">
                        <Border StrokeThickness="1.5" StrokeShape="RoundRectangle 14" Padding="15,14">
                            <Border.Triggers>
                                <DataTrigger TargetType="Border" Binding="{Binding IsSelected}" Value="True">
                                    <Setter Property="Stroke" Value="{StaticResource TzGoldLight}"/>
                                    <Setter Property="BackgroundColor" Value="#29E8BD77"/>
                                </DataTrigger>
                                <DataTrigger TargetType="Border" Binding="{Binding IsSelected}" Value="False">
                                    <Setter Property="Stroke" Value="#2EFFFDF9"/>
                                    <Setter Property="BackgroundColor" Value="#0FFFFDF9"/>
                                </DataTrigger>
                            </Border.Triggers>

                            <Grid ColumnDefinitions="*,Auto" ColumnSpacing="12">
                                <VerticalStackLayout Spacing="3">
                                    <Label Text="{Binding Name}" FontFamily="ArchivoSemiBold"
                                           FontSize="14" LineHeight="1.2"
                                           TextColor="{StaticResource TzSurface}"/>
                                    <Label Text="{Binding Note}" FontFamily="ArchivoRegular"
                                           FontSize="10.5" LineHeight="1.3" TextColor="#99FFFDF9"/>
                                </VerticalStackLayout>

                                <Border Grid.Column="1" WidthRequest="19" HeightRequest="19"
                                        IsVisible="{Binding IsSelected}" Padding="0"
                                        StrokeThickness="0" StrokeShape="RoundRectangle 9.5"
                                        BackgroundColor="{StaticResource TzGoldLight}"
                                        VerticalOptions="Center">
                                    <Label Text="✓" FontSize="11" TextColor="{StaticResource TzGreenDeep}"
                                           HorizontalOptions="Center" VerticalOptions="Center"/>
                                </Border>
                            </Grid>

                            <Border.GestureRecognizers>
                                <TapGestureRecognizer Command="{Binding SelectCommand}"/>
                            </Border.GestureRecognizers>
                        </Border>
                    </DataTemplate>
                </BindableLayout.ItemTemplate>
            </VerticalStackLayout>
        </ScrollView>

        <Border Grid.Row="4" Margin="0,18,0,0" Style="{StaticResource PrimaryAction}" Padding="16">
            <Label Text="Continue" FontFamily="ArchivoSemiBold" FontSize="14"
                   HorizontalOptions="Center" TextColor="{StaticResource TzSurface}"/>
            <Border.GestureRecognizers>
                <TapGestureRecognizer Command="{Binding ToWhoCommand}"/>
            </Border.GestureRecognizers>
        </Border>
    </Grid>
</ContentPage>
```

The remaining five:

- **`IntroPage`** — 430pt `spread_nshima.png` hero with a gradient running to solid
  `TzSurface` at the bottom (stops: `#FFFFFDF9` 0%, `#D1FFFDF9` 14%, `#570E2018` 62%,
  `#330E2018` 100%), a "Skip" pill top-right (`#800E2018`, 18pt radius) bound to
  `SkipCommand`, then content pulled up with `Margin="0,-46,0,0"`: clay kicker
  "Before we start", 34pt serif "Zambian food, recorded properly", a 14pt intro paragraph,
  and three bullet rows — 7pt `TzGoldDecor` dot + text whose lead clause
  ("Cook from it.", "Learn from it.", "Add to it.") is `ArchivoSemiBold` `TzInk` in a
  `FormattedString`. Gold "Get started" → `ToLanguageCommand`.
- **`OnbWhoPage`** — cream ground, `StepRail Step="2"`, 31pt serif
  "What brings you to Zambian food?", radio list over `VisitorKinds` using the same radio
  anatomy as Task 16's privacy cards (19pt ring, 9pt dot, `TzGreenTint` fill when selected).
  Back → `ToLanguageCommand`, Continue → `ToTasteCommand`.
- **`OnbTastePage`** — `StepRail Step="3"`, 31pt serif "What should we show you first?",
  "Pick as many as you like." beside `TasteCountLabel` in `PlexMonoMedium` `TzGreenMid`.
  Chips in a `FlexLayout` (12,16 padding, 22pt radius) inverting to `TzGreenDeep` fill with
  `TzSurface` text when on. Cream "One more thing" panel below.
- **`OnbNotifyPage`** — `StepRail Step="4"`, 31pt serif "Two things worth knowing", two
  cards each with a 40x23 pill toggle (18pt knob, `TzGreenMid` track when on,
  `#2E221A12` when off) bound to `OfflineEnabled` and `StoryNotifications`, then the deep
  green "Anything you add stays yours" panel.
- **`OnbReadyPage`** — deep green, 66pt logo, 36pt serif "Mwaiseni. Welcome to the
  archive.", the 120-recipes paragraph, a rule, then two summary rows showing
  `PickedLanguage` (Newsreader 16 `TzGoldLight`) and `PickedWho`. Gold "Start exploring"
  → `FinishCommand`, with "You can change any of this in Settings" beneath.

- [ ] **Step 8: Write `OnboardingShell` and switch the app root on completion**

`OnboardingShell.xaml` registers the seven routes with `Shell.TabBarIsVisible="False"`
and `splash` as the entry `ShellContent`. `App.xaml.cs` picks the root:

```csharp
using TasteZambia.Core.Services;

namespace TasteZambia.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var onboarding = _services.GetRequiredService<IOnboardingService>();

        Page root = onboarding.IsComplete
            ? _services.GetRequiredService<AppShell>()
            : _services.GetRequiredService<OnboardingShell>();

        return new Window(root);
    }
}
```

`OnboardingViewModel.Finish`/`Skip` navigate to `//home`, which does not exist inside
`OnboardingShell` — so the two commands must also swap the root. Add to both, after
`onboarding.Complete(...)`:

```csharp
if (Application.Current is { } app && app.Windows.Count > 0)
    app.Windows[0].Page = _services.GetRequiredService<AppShell>();
```

Inject `IServiceProvider` into `OnboardingViewModel` for this, and drop the `GoToAsync("//home")`.

- [ ] **Step 9: Register**

```csharp
builder.Services.AddSingleton<IOnboardingService, OnboardingService>();
builder.Services.AddSingleton<OnboardingViewModel>();   // shared across the 7 pages
builder.Services.AddSingleton<OnboardingShell>();
builder.Services.AddTransient<SplashPage>();
builder.Services.AddTransient<IntroPage>();
builder.Services.AddTransient<OnbLanguagePage>();
builder.Services.AddTransient<OnbWhoPage>();
builder.Services.AddTransient<OnbTastePage>();
builder.Services.AddTransient<OnbNotifyPage>();
builder.Services.AddTransient<OnbReadyPage>();
```

`App` now takes `IServiceProvider`, so also add `builder.Services.AddSingleton<App>();`.

- [ ] **Step 10: Verify and commit**

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Launch. Check against canvas row 1: no bottom nav on any of the seven; the rail fills one
segment per screen and is gold-light on green, `TzGoldDecor` on cream; "Skip" from Intro
lands on Home; choices made on screens 3–4 appear on the Ready summary; "Start exploring"
swaps to the tabbed Shell and a relaunch goes straight to Home.

```bash
git add -A
git commit -m "feat(mobile): implement onboarding flow (7 screens, pre-Shell)

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 20: Share a Recipe — the full lifecycle (frames 17–22)

Task 15 built `share`, the four-step form. v2 wraps it in five more screens: an entry
point, a drafts list, and three post-submission states.

| Screen | Route | Purpose |
|---|---|---|
| `shareStart` | `shareStart` | Chooses between public contribution and family preservation |
| `shareDraft` | `shareDraft` | Drafts saved on device, with completeness bars |
| `shareReview` | `shareReview` | Submission timeline and the assigned reviewer |
| `shareChanges` | `shareChanges` | Reviewer's questions, per field |
| `sharePublished` | `sharePublished` | Published entry, credit, reach, reader suggestion |

**Files:**
- Create: `TasteZambia.Core/Models/Contribution.cs` *(extends the Task 2 file)*
- Create: `TasteZambia.Core/ViewModels/ShareStartViewModel.cs`, `DraftsViewModel.cs`, `ShareReviewViewModel.cs`, `ShareChangesViewModel.cs`, `SharePublishedViewModel.cs`
- Create: `TasteZambia.Mobile/Views/Share/` — `ShareStartPage`, `ShareDraftsPage`, `ShareReviewPage`, `ShareChangesPage`, `SharePublishedPage`
- Modify: `TasteZambia.Core/Data/SeedData.cs`, `TasteZambia.Core/Services/ContributionService.cs`, `TasteZambia.Mobile/AppShell.xaml.cs`, `MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/ShareLifecycleTests.cs`

**Interfaces:**
- Produces:
  - `RecipeDraft(string Name, int PercentComplete, string Missing, string When, string TintHex)`
  - `ReviewStep(string Label, string When, string Note, ReviewState State)` with
    `enum ReviewState { Done, InProgress, Pending }`
  - `FlaggedField(string Field, string Question, string CurrentValue)`
  - `IContributionService` gains: `IReadOnlyList<RecipeDraft> Drafts`,
    `IReadOnlyList<ReviewStep> Timeline`, `IReadOnlyList<FlaggedField> FlaggedFields`
  - Five ViewModels, each with the commands named in Step 4.

- [ ] **Step 1: Write the failing test**

`TasteZambia.Core.Tests/ViewModels/ShareLifecycleTests.cs`:

```csharp
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class ShareLifecycleTests
{
    private sealed class Nav : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoBackAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task ShareStart_ShowsBothRoutesAndTheRecordCounts()
    {
        var nav = new Nav();
        var vm = new ShareStartViewModel(new ContributionService(), nav);
        await vm.InitializeAsync();

        Assert.Equal(1, vm.PublishedCount);
        Assert.Equal(1, vm.InReviewCount);
        Assert.Equal(4, vm.PreservedCount);
        Assert.Equal("3 drafts in progress", vm.DraftsLabel);

        vm.GoShareCommand.Execute(null);
        vm.GoPreserveCommand.Execute(null);
        vm.GoDraftsCommand.Execute(null);

        Assert.Equal(["share", "famStart", "shareDraft"], nav.Routes);
    }

    [Fact]
    public async Task Drafts_AreOrderedMostCompleteFirstWithTheirTints()
    {
        var vm = new DraftsViewModel(new ContributionService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(3, vm.Drafts.Count);
        Assert.Equal("Chibwabwa na Mbalala", vm.Drafts[0].Name);
        Assert.Equal(85, vm.Drafts[0].PercentComplete);
        Assert.Equal("85%", vm.Drafts[0].PercentLabel);
        Assert.Equal("#2F6A4D", vm.Drafts[0].TintHex);
        Assert.Equal("#A3452A", vm.Drafts[2].TintHex);
    }

    [Fact]
    public async Task ReviewTimeline_HasOneInProgressAndOnePending()
    {
        var vm = new ShareReviewViewModel(new ContributionService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(4, vm.Timeline.Count);
        Assert.Equal(ReviewState.Done, vm.Timeline[0].State);
        Assert.Equal(ReviewState.InProgress, vm.Timeline[2].State);
        Assert.Equal(ReviewState.Pending, vm.Timeline[3].State);
        Assert.True(vm.Timeline[2].IsNotLast);
        Assert.False(vm.Timeline[3].IsNotLast);
        Assert.Equal("Namakau Sitali", vm.ReviewerName);
    }

    [Fact]
    public async Task ChangesRequested_CarriesTwoFieldQuestions()
    {
        var vm = new ShareChangesViewModel(new ContributionService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(2, vm.Flagged.Count);
        Assert.Equal("Local name", vm.Flagged[0].Field);
        Assert.Contains("Mungwi", vm.Flagged[0].Question);
        Assert.Equal("Chibwabwa na Mbalala", vm.Flagged[0].CurrentValue);
        Assert.Equal("Cooking step 2", vm.Flagged[1].Field);
    }

    [Fact]
    public async Task Published_ShowsCreditAndReach()
    {
        var vm = new SharePublishedViewModel(new ContributionService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal("Chibwabwa na Mbalala", vm.Title);
        Assert.Equal(318, vm.OpenedCount);
        Assert.Equal(64, vm.SavedCount);
        Assert.Equal(11, vm.CookedCount);
        Assert.Contains("Banakulu Mwaba", vm.Credit);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~ShareLifecycleTests`
Expected: FAIL — `ShareStartViewModel` does not exist.

- [ ] **Step 3: Extend the models and seed data**

Append to `TasteZambia.Core/Models/ContributionDraft.cs`:

```csharp
public enum ReviewState { Done, InProgress, Pending }

public sealed record RecipeDraft(string Name, int PercentComplete, string Missing, string When, string TintHex)
{
    public string PercentLabel => $"{PercentComplete}%";
    public double Fraction => PercentComplete / 100.0;
}

public sealed record ReviewStep(string Label, string When, string Note, ReviewState State, bool IsNotLast)
{
    /// <summary>Green when done or in progress; the pale idle tone when not yet reached.</summary>
    public string DotHex => State switch
    {
        ReviewState.Done => "#2F6A4D",
        ReviewState.InProgress => "#C07F1E",
        _ => "#D8CDB9",
    };

    public string LabelHex => State == ReviewState.Pending ? "#7A6B59" : "#221A12";
}

public sealed record FlaggedField(string Field, string Question, string CurrentValue);
```

Append to `SeedData.cs`:

```csharp
public static readonly IReadOnlyList<RecipeDraft> Drafts =
[
    new("Chibwabwa na Mbalala", 85, "Needs one more cooking step",              "Edited 2 hours ago", "#2F6A4D"),
    new("Munkoyo",              40, "Needs photos and the fermenting times",    "Edited 4 days ago",  "#C07F1E"),
    new("Ubwali bwa Tute",      15, "Only the name and province so far",        "Edited 3 weeks ago", "#A3452A"),
];

public static readonly IReadOnlyList<ReviewStep> SubmissionTimeline =
[
    new("Submitted", "2 Sep 2026",
        "Left your device and entered the queue.", ReviewState.Done, true),
    new("Read by the archive team", "3 Sep 2026",
        "Reviewed by Namakau Sitali, Northern Province records.", ReviewState.Done, true),
    new("Checked against regional sources", "In progress",
        "Names, ingredients and method compared against the provincial record.", ReviewState.InProgress, true),
    new("Published and credited", "Expected mid-September",
        "Credited to you and to whoever taught you the dish.", ReviewState.Pending, false),
];

public static readonly IReadOnlyList<FlaggedField> FlaggedFields =
[
    new("Local name",
        "Is this dish called Chibwabwa na Mbalala in Mungwi specifically, or is that the Kasama town name? Our Northern records have both.",
        "Chibwabwa na Mbalala"),
    new("Cooking step 2",
        "You say pound until the oil shows. Roughly how long does that take by hand? Readers abroad will be using a blender.",
        "Pound the groundnuts until the oil starts to show."),
];

public const string PublishedCredit =
    "Recorded by Chanda Mwaba, Kitwe. As taught by Banakulu Mwaba of Mungwi, Northern Province. Verified against provincial records, September 2026.";
```

Extend `IContributionService` and `ContributionService` with three passthrough members:

```csharp
IReadOnlyList<RecipeDraft> Drafts { get; }
IReadOnlyList<ReviewStep> Timeline { get; }
IReadOnlyList<FlaggedField> FlaggedFields { get; }
```

```csharp
public IReadOnlyList<RecipeDraft> Drafts => SeedData.Drafts;
public IReadOnlyList<ReviewStep> Timeline => SeedData.SubmissionTimeline;
public IReadOnlyList<FlaggedField> FlaggedFields => SeedData.FlaggedFields;
```

- [ ] **Step 4: Write the five ViewModels**

`TasteZambia.Core/ViewModels/ShareStartViewModel.cs` (the other four follow the same shape —
each loads one seed list and exposes navigation commands):

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class ShareStartViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    [ObservableProperty] private int _publishedCount = 1;
    [ObservableProperty] private int _inReviewCount = 1;
    [ObservableProperty] private int _preservedCount = 4;
    [ObservableProperty] private string _draftsLabel = "";

    public override Task InitializeAsync()
    {
        var n = contributions.Drafts.Count;
        DraftsLabel = $"{n} draft{(n == 1 ? "" : "s")} in progress";
        return Task.CompletedTask;
    }

    [RelayCommand] private Task GoShare()    => Navigation.GoToAsync("share");
    [RelayCommand] private Task GoPreserve() => Navigation.GoToAsync("famStart");
    [RelayCommand] private Task GoDrafts()   => Navigation.GoToAsync("shareDraft");
}

public sealed partial class DraftsViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<RecipeDraft> Drafts { get; } = [];

    public override Task InitializeAsync()
    {
        if (Drafts.Count > 0) return Task.CompletedTask;
        foreach (var d in contributions.Drafts) Drafts.Add(d);
        return Task.CompletedTask;
    }

    [RelayCommand] private Task Continue() => Navigation.GoToAsync("share");
}

public sealed partial class ShareReviewViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<ReviewStep> Timeline { get; } = [];

    public string DishName => "Chibwabwa na Mbalala";
    public string SubmittedMeta => "Northern Province · submitted 2 September 2026";
    public string ReviewerName => "Namakau Sitali";
    public string ReviewerScope => "Northern Province records";

    public override Task InitializeAsync()
    {
        if (Timeline.Count > 0) return Task.CompletedTask;
        foreach (var s in contributions.Timeline) Timeline.Add(s);
        return Task.CompletedTask;
    }

    [RelayCommand] private Task Withdraw() => Navigation.GoToAsync("shareChanges");
}

public sealed partial class ShareChangesViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<FlaggedField> Flagged { get; } = [];

    public string ReviewerName => "Namakau Sitali";
    public string ReviewerNote =>
        "This is a good record and the method matches what we have for Mungwi. Two things I want to get right before it goes public.";

    public override Task InitializeAsync()
    {
        if (Flagged.Count > 0) return Task.CompletedTask;
        foreach (var q in contributions.FlaggedFields) Flagged.Add(q);
        return Task.CompletedTask;
    }

    [RelayCommand] private Task Resubmit() => Navigation.GoToAsync("sharePublished");
}

public sealed partial class SharePublishedViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public string Subtitle => "Pumpkin leaves with pounded groundnuts";
    public string Credit => SeedDataCredit;
    public int OpenedCount => 318;
    public int SavedCount => 64;
    public int CookedCount => 11;

    private const string SeedDataCredit = Data.SeedData.PublishedCredit;

    public override Task InitializeAsync()
    {
        Title = "Chibwabwa na Mbalala";
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~ShareLifecycleTests`
Expected: PASS, 5 tests.

- [ ] **Step 6: Write the five pages**

All five use `<controls:BottomNavBar ActiveSection="profile"/>` and the vocabulary already
established. The two with new anatomy:

**`ShareStartPage`** — clay kicker "Contribute", 31pt serif "Add to the archive", intro,
then two choice cards: a deep-green one (24pt serif "Share a Recipe", body, three
`#1AFFFDF9` pills "Reviewed"/"Public"/"Credited to you") → `GoShareCommand`, and a bordered
cream-white one (24pt serif semibold "Preserve a Family Recipe", body, two `TzCream` pills)
→ `GoPreserveCommand`. Below: a `TzCream` row bound to `DraftsLabel` with a chevron →
`GoDraftsCommand`, then a "Your record so far" three-up of bordered stat cards whose
numbers are `TzGreenDeep` / `TzGoldDecor` / `TzClay`.

**`ShareReviewPage`** — the vertical timeline is the one genuinely new layout. Each step is
a `Grid ColumnDefinitions="Auto,*"` where column 0 is an 11pt dot over a 1.5pt connector
line (`TzRule`) that is hidden on the last step:

```xml
<VerticalStackLayout BindableLayout.ItemsSource="{Binding Timeline}">
    <BindableLayout.ItemTemplate>
        <DataTemplate x:DataType="models:ReviewStep">
            <Grid ColumnDefinitions="Auto,*" ColumnSpacing="14">
                <Grid Grid.Column="0" WidthRequest="11" RowDefinitions="Auto,*">
                    <BoxView WidthRequest="11" HeightRequest="11" CornerRadius="5.5" Margin="0,4,0,0"
                             Color="{Binding DotHex, Converter={StaticResource HexToColor}}"/>
                    <BoxView Grid.Row="1" WidthRequest="1.5" HorizontalOptions="Center"
                             IsVisible="{Binding IsNotLast}" Color="{StaticResource TzRule}"/>
                </Grid>
                <VerticalStackLayout Grid.Column="1" Padding="0,0,0,20" Spacing="5">
                    <Grid ColumnDefinitions="*,Auto" ColumnSpacing="10">
                        <Label Text="{Binding Label}" FontFamily="ArchivoSemiBold" FontSize="13"
                               LineHeight="1.25"
                               TextColor="{Binding LabelHex, Converter={StaticResource HexToColor}}"/>
                        <Label Grid.Column="1" Text="{Binding When}" FontFamily="PlexMonoRegular"
                               FontSize="10" TextColor="{StaticResource TzMuted2}"/>
                    </Grid>
                    <Label Text="{Binding Note}" FontFamily="ArchivoRegular" FontSize="11.5"
                           LineHeight="1.6" TextColor="{StaticResource TzBodyMuted}"/>
                </VerticalStackLayout>
            </Grid>
        </DataTemplate>
    </BindableLayout.ItemTemplate>
</VerticalStackLayout>
```

The header is a deep-green band with the gold "IN REVIEW" badge, 28pt serif dish name and
`SubmittedMeta`. Below the timeline: a reviewer card (38pt striped avatar, "Reviewing your
submission" / `ReviewerName` / `ReviewerScope`) and an outlined "Withdraw submission".

The other three:
- **`ShareDraftsPage`** — clay kicker "Saved on this device", 31pt serif "My Drafts". Each
  draft is a card: 19pt serif name + `PercentLabel` in mono, a 4pt track whose fill width
  is `Fraction` of the content width and whose colour is `TintHex`, the `Missing` line,
  then `When` beside outlined "Delete" and `TzGreenDeep` "Continue" buttons.
- **`ShareChangesPage`** — a `TzGoldTint` header band with a `TzClayText` "CHANGES
  REQUESTED" badge on `#1FA3452A` with a `#59A3452A` stroke, 28pt serif "Two questions
  before we publish". A reviewer note card, then per-field cards stroked `#47A3452A` with a
  clay micro-label, the question, a `TzCream` "Your entry: …" block, and a dashed
  "Reply or edit this field" in `TzGold`. Gold "Resubmit for review" at the foot.
- **`SharePublishedPage`** — 262pt `ifisashi.png` hero with the gold "PUBLISHED IN THE
  ARCHIVE" badge and 32pt serif title, a deep-green "Credit" panel bound to `Credit`, a
  "Since publishing" three-up (`OpenedCount`/`SavedCount`/`CookedCount`), and a `TzCream`
  reader-suggestion panel whose "Review it" is `TzGold` semibold.

- [ ] **Step 7: Register routes and services**

In `AppShell.xaml.cs`:

```csharp
Routing.RegisterRoute("shareStart",     typeof(ShareStartPage));
Routing.RegisterRoute("shareDraft",     typeof(ShareDraftsPage));
Routing.RegisterRoute("shareReview",    typeof(ShareReviewPage));
Routing.RegisterRoute("shareChanges",   typeof(ShareChangesPage));
Routing.RegisterRoute("sharePublished", typeof(SharePublishedPage));
```

Register all five ViewModels and pages as `Transient` in `MauiProgram.cs`.

- [ ] **Step 8: Verify and commit**

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Check against canvas rows 3: the timeline's third dot is `TzGoldDecor` and its fourth is the
pale `#D8CDB9` with grey label; the connector line stops after step 3; draft bars are
green/gold/clay top to bottom; the nav bar shows **Profile** on all five.

```bash
git add -A
git commit -m "feat(mobile): implement Share a Recipe lifecycle (entry, drafts, review, changes, published)

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 21: Preserve a Family Recipe — the full lifecycle (frames 23–28)

Task 16 built `family`, the four-step form. v2 wraps it in five more screens. The
distinguishing material here is **audio**: a recording of the person who taught the recipe,
pending transcription, later approved and attached.

| Screen | Route | Purpose |
|---|---|---|
| `famStart` | `famStart` | Deep-green intro naming the four things you will be asked |
| `famDraft` | `famDraft` | Autosaved draft, 72% checklist, uploaded audio pending transcription |
| `famSaved` | `famSaved` | Confirmation, current privacy, who has access |
| `famShared` | `famShared` | The family view — approved audio and relatives' notes |
| `famPublic` | `famPublic` | Provenance trail and permanent, unremovable credit |

**Files:**
- Create: `TasteZambia.Core/Models/FamilyArchive.cs`
- Create: `TasteZambia.Core/Services/FamilyArchiveService.cs`
- Create: `TasteZambia.Core/ViewModels/FamilyLifecycleViewModels.cs`
- Create: `TasteZambia.Mobile/Views/Family/` — `FamStartPage`, `FamDraftPage`, `FamSavedPage`, `FamSharedPage`, `FamPublicPage`
- Modify: `AppShell.xaml.cs`, `MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/FamilyLifecycleTests.cs`

**Interfaces:**
- Produces:
  - `FamilyMember(string Name, string Role, string Status, string BadgeBgHex, string BadgeFgHex)`
  - `FamilyNote(string Who, string When, string Body)`
  - `DraftChecklistItem(string Label, string? Detail, bool IsDone)`
  - `ProvenanceStep(string Label, string Detail, string DotHex)`
  - `AudioClip(string Speaker, string Duration, bool TranscriptApproved)`
  - `IFamilyArchiveService`: `Members`, `Notes`, `Checklist`, `Provenance`, `Recording`,
    `int PercentComplete`, `PrivacyLevel Privacy`, `void SetPrivacy(PrivacyLevel)`
  - `FamStartViewModel`, `FamDraftViewModel`, `FamSavedViewModel`, `FamSharedViewModel`, `FamPublicViewModel`

- [ ] **Step 1: Write the failing test**

```csharp
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class FamilyLifecycleTests
{
    private sealed class Nav : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoBackAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task Draft_IsSeventyTwoPercentWithTwoItemsOutstanding()
    {
        var vm = new FamDraftViewModel(new FamilyArchiveService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(72, vm.PercentComplete);
        Assert.Equal("72% complete · 2 things left", vm.ProgressLabel);
        Assert.Equal(4, vm.Checklist.Count);
        Assert.True(vm.Checklist[0].IsDone);
        Assert.False(vm.Checklist[2].IsDone);
        Assert.Equal("Cooking steps", vm.Checklist[2].Label);
    }

    [Fact]
    public async Task Draft_CarriesAudioPendingTranscription()
    {
        var vm = new FamDraftViewModel(new FamilyArchiveService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal("Banakulu Mwaba, in Bemba", vm.Recording.Speaker);
        Assert.Equal("12:40", vm.Recording.Duration);
        Assert.False(vm.Recording.TranscriptApproved);
    }

    [Fact]
    public async Task Saved_ListsFourFamilyMembersWithOneStillInvited()
    {
        var vm = new FamSavedViewModel(new FamilyArchiveService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(4, vm.Members.Count);
        Assert.Equal("Owner", vm.Members[0].Status);
        Assert.Equal("Invited", vm.Members[3].Status);
        Assert.Equal("Kaunda Mwaba", vm.Members[3].Name);
        Assert.Equal(PrivacyLevel.SharedWithFamily, vm.Privacy);
    }

    [Fact]
    public async Task Shared_ShowsApprovedAudioAndTwoFamilyNotes()
    {
        var vm = new FamSharedViewModel(new FamilyArchiveService(), new Nav());
        await vm.InitializeAsync();

        Assert.True(vm.Recording.TranscriptApproved);
        Assert.Equal("FAMILY ONLY · 4 PEOPLE", vm.AccessBadge);
        Assert.Equal(2, vm.Notes.Count);
        Assert.Equal("Aunt Bwalya", vm.Notes[0].Who);
        Assert.Contains("never used tomato", vm.Notes[0].Body);
    }

    [Fact]
    public async Task Public_HasFourProvenanceStepsEndingOnPublished()
    {
        var vm = new FamPublicViewModel(new FamilyArchiveService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(4, vm.Provenance.Count);
        Assert.Equal("Preserved privately", vm.Provenance[0].Label);
        Assert.Equal("#2F6A4D", vm.Provenance[0].DotHex);
        Assert.Equal("Published and credited", vm.Provenance[3].Label);
        Assert.Equal("#C07F1E", vm.Provenance[3].DotHex);
        Assert.Contains("cannot be removed", vm.CreditNote);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~FamilyLifecycleTests`
Expected: FAIL — `FamilyArchiveService` does not exist.

- [ ] **Step 3: Write the models and service**

`TasteZambia.Core/Models/FamilyArchive.cs`:

```csharp
namespace TasteZambia.Core.Models;

public sealed record FamilyMember(string Name, string Role, string Status, string BadgeBgHex, string BadgeFgHex);
public sealed record FamilyNote(string Who, string When, string Body);
public sealed record DraftChecklistItem(string Label, string? Detail, bool IsDone);
public sealed record ProvenanceStep(string Label, string Detail, string DotHex);
public sealed record AudioClip(string Speaker, string Duration, bool TranscriptApproved);

public sealed record PreservedRecipe(string Name, string TaughtBy, string Privacy,
                                     string BadgeBgHex, string BadgeFgHex, string Extras);
```

`TasteZambia.Core/Services/FamilyArchiveService.cs`:

```csharp
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Services;

public interface IFamilyArchiveService
{
    int PercentComplete { get; }
    AudioClip Recording { get; }
    AudioClip ApprovedRecording { get; }
    PrivacyLevel Privacy { get; }
    void SetPrivacy(PrivacyLevel level);
    IReadOnlyList<DraftChecklistItem> Checklist { get; }
    IReadOnlyList<FamilyMember> Members { get; }
    IReadOnlyList<FamilyNote> Notes { get; }
    IReadOnlyList<ProvenanceStep> Provenance { get; }
    IReadOnlyList<PreservedRecipe> PreservedRecipes { get; }
}

public sealed class FamilyArchiveService : IFamilyArchiveService
{
    public int PercentComplete => 72;

    public AudioClip Recording { get; } = new("Banakulu Mwaba, in Bemba", "12:40", false);
    public AudioClip ApprovedRecording { get; } = new("Her voice, in Bemba", "12:40", true);

    public PrivacyLevel Privacy { get; private set; } = PrivacyLevel.SharedWithFamily;
    public void SetPrivacy(PrivacyLevel level) => Privacy = level;

    public IReadOnlyList<DraftChecklistItem> Checklist { get; } =
    [
        new("Recipe name, region and photos", null, true),
        new("Who taught you, and the story", null, true),
        new("Cooking steps", "Two of four written", false),
        new("Who can see it", "Not chosen yet — private until you do", false),
    ];

    public IReadOnlyList<FamilyMember> Members { get; } =
    [
        new("Chanda Mwaba",  "You · owner",          "Owner",   "#EEF2EC", "#2F6A4D"),
        new("Mutinta Mwaba", "Sister, Lusaka",       "Joined",  "#EEF2EC", "#2F6A4D"),
        new("Aunt Bwalya",   "Mungwi",               "Joined",  "#EEF2EC", "#2F6A4D"),
        new("Kaunda Mwaba",  "Cousin, Manchester",   "Invited", "#F7EEDA", "#7A5A10"),
    ];

    public IReadOnlyList<FamilyNote> Notes { get; } =
    [
        new("Aunt Bwalya", "Added a note · 4 Sep",
            "She never used tomato in this. If you add tomato it becomes a different relish and she would have said so."),
        new("Mutinta Mwaba", "Added a photo · 5 Sep",
            "Found the picture from Christmas 2011, the year we all came home. The pot in it is the same clay pot."),
    ];

    public IReadOnlyList<ProvenanceStep> Provenance { get; } =
    [
        new("Preserved privately",              "Written down in March 2026, with her recording.",              "#2F6A4D"),
        new("Family agreed to publish",         "All four members with access consented in August.",            "#2F6A4D"),
        new("Verified against Northern records","Reviewed by Namakau Sitali, September 2026.",                  "#2F6A4D"),
        new("Published and credited",           "Listed under Northern Province, linked to chibwabwa and mbalala.", "#C07F1E"),
    ];

    public IReadOnlyList<PreservedRecipe> PreservedRecipes { get; } =
    [
        new("Ifisashi ya Banakulu",   "Banakulu Mwaba, Mungwi", "Public",  "#EEF2EC", "#2F6A4D", "Audio 12:40 · 3 photos · 2 family notes"),
        new("Inkoko ya Bataata",      "my father, Kitwe",       "Family",  "#F7EEDA", "#7A5A10", "1 photo · no audio yet"),
        new("Munkoyo wa Ba Shikulu",  "Grandfather, Solwezi",   "Private", "#F0ECE4", "#6B5C4A", "Draft · 40% complete"),
        new("Chikanda ya Ba Mayo",    "my mother, Chinsali",    "Family",  "#F7EEDA", "#7A5A10", "Audio 6:12 · 2 photos"),
    ];
}
```

- [ ] **Step 4: Write the five ViewModels**

`TasteZambia.Core/ViewModels/FamilyLifecycleViewModels.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class FamStartViewModel(INavigationService navigation)
    : BaseViewModel(navigation)
{
    public IReadOnlyList<(string Number, string Title, string Detail)> Steps { get; } =
    [
        ("1", "The recipe",     "Name in your own language, region, photos."),
        ("2", "Who taught you", "Their name, where they learned it, and their voice if you can record it."),
        ("3", "The story",      "When it was cooked, what it meant, how they did it differently."),
        ("4", "Who can see it", "Private, your family, or the public archive. Changeable at any time."),
    ];

    [RelayCommand] private Task Begin() => Navigation.GoToAsync("family");
}

public sealed partial class FamDraftViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<DraftChecklistItem> Checklist { get; } = [];

    public int PercentComplete => archive.PercentComplete;
    public AudioClip Recording => archive.Recording;
    public string RecipeName => "Ifisashi ya Banakulu";
    public string Meta => "Northern Province · Bemba · edited 2 hours ago";
    public string ProgressLabel { get; private set; } = "";

    public string TranscriptionNote =>
        "Transcription pending. A Bemba speaker on the archive team will transcribe it, and you approve the text before it is attached.";

    public override Task InitializeAsync()
    {
        if (Checklist.Count > 0) return Task.CompletedTask;

        foreach (var item in archive.Checklist) Checklist.Add(item);

        var left = Checklist.Count(c => !c.IsDone);
        ProgressLabel = $"{PercentComplete}% complete · {left} thing{(left == 1 ? "" : "s")} left";
        return Task.CompletedTask;
    }

    [RelayCommand] private Task Finish() => Navigation.GoToAsync("famSaved");
}

public sealed partial class FamSavedViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<FamilyMember> Members { get; } = [];

    public PrivacyLevel Privacy => archive.Privacy;
    public string PrivacyLabel => "Shared with family";
    public string PrivacyNote =>
        "Anyone you invite can read it and add their own notes. It stays out of the public archive.";
    public string Headline => "Kept in your family archive";
    public string Body =>
        "Ifisashi ya Banakulu is saved with her recording, her name, and the story behind it.";

    public override Task InitializeAsync()
    {
        if (Members.Count > 0) return Task.CompletedTask;
        foreach (var m in archive.Members) Members.Add(m);
        return Task.CompletedTask;
    }

    [RelayCommand] private Task OpenFamilyView() => Navigation.GoToAsync("famShared");
}

public sealed partial class FamSharedViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<FamilyNote> Notes { get; } = [];

    public AudioClip Recording => archive.ApprovedRecording;
    public string AccessBadge => $"FAMILY ONLY · {archive.Members.Count} PEOPLE";
    public string RecipeName => "Ifisashi ya Banakulu";
    public string TaughtBy => "As taught by Banakulu Mwaba, Mungwi";
    public string NotesCountLabel => $"{Notes.Count} notes";

    public override Task InitializeAsync()
    {
        if (Notes.Count > 0) return Task.CompletedTask;
        foreach (var n in archive.Notes) Notes.Add(n);
        return Task.CompletedTask;
    }
}

public sealed partial class FamPublicViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<ProvenanceStep> Provenance { get; } = [];

    public string Headline => "Ifisashi ya Banakulu";
    public string Intro =>
        "The family chose to open this recipe to everyone. It kept its name, its recording and its credit.";
    public string Credit =>
        "Banakulu Mwaba of Mungwi, Northern Province. Recorded by her grandson, Chanda Mwaba.";
    public string CreditNote =>
        "This credit cannot be removed by anyone but the family, and travels with the recipe wherever it is shown.";

    public override Task InitializeAsync()
    {
        if (Provenance.Count > 0) return Task.CompletedTask;
        foreach (var s in archive.Provenance) Provenance.Add(s);
        return Task.CompletedTask;
    }

    [RelayCommand] private void MakePrivate() => archive.SetPrivacy(PrivacyLevel.PrivateToMe);
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~FamilyLifecycleTests`
Expected: PASS, 5 tests.

- [ ] **Step 6: Write the five pages**

All carry `<controls:BottomNavBar ActiveSection="profile"/>`. Anatomy per screen:

- **`FamStartPage`** — deep-green header band (gold kicker "Family archive", 33pt serif
  title, body). Then "What you will be asked": four rows over `Steps`, each a 24pt
  `TzCream` rounded square with the number in `TzGreenDeep`, separated by `TzRule`
  top-borders with a closing bottom border on the last. `TzCream` reassurance panel, gold
  "Begin" → `BeginCommand`.
- **`FamDraftPage`** — a `TzDraftBg` "DRAFT · AUTOSAVED" badge, 29pt serif name, meta line.
  A 5pt `TzGreenMid` progress track at `PercentComplete`, `ProgressLabel` in mono. Then the
  checklist: done items are `TzGreenTint` with a `#33 2F6A4D` stroke and a filled 20pt
  `TzGreenMid` check; undone are `TzSurface` with an empty 20pt box, a `Detail` sub-line and
  a chevron. Below, the deep-green audio card: gold micro-label "Audio uploaded", a 38pt
  `#24FFFDF9` circle with a play triangle, `Recording.Speaker`, an inert 3pt track,
  `Recording.Duration`, then a rule and `TranscriptionNote`. Foot: outlined "Leave as draft"
  + gold "Finish and save" → `FinishCommand`.
- **`FamSavedPage`** — the deep-green confirmation panel (52pt gold ring + `✓`, 27pt serif
  `Headline`, `Body`), then a `TzGreenTint` privacy card stroked 1.5pt `TzGreenDeep` with
  `PrivacyLabel`, a `TzGold` "Change", and `PrivacyNote`. Then "Who has access" with a
  `TzGold` "+ Invite" and a bordered member table over `Members` — 32pt striped avatar,
  name, role, and a status pill using `BadgeBgHex`/`BadgeFgHex`. `TzGreenDeep` "Open the
  family view" → `OpenFamilyViewCommand`.
- **`FamSharedPage`** — 222pt `ifisashi.png` hero with a translucent `#29FFFDF9` badge bound
  to `AccessBadge`, 29pt serif name, `TaughtBy`. A `TzCream` audio row (38pt `TzGreenDeep`
  circle, `Recording.Speaker`, "Transcript approved · 12:40"). "What the family added" with
  `NotesCountLabel` in mono, then note cards over `Notes` — 28pt striped avatar, `Who`,
  `When`, `Body`. Dashed "Add your version" panel to close.
- **`FamPublicPage`** — deep-green header with the gold "NOW IN THE PUBLIC ARCHIVE" badge,
  30pt serif name, `Intro`. "How it got here" renders `Provenance` as rule-separated rows
  with a 9pt dot coloured by `DotHex`. Then a `TzCream` "Permanent credit" panel: gold-green
  micro-label, 19pt serif `Credit`, and `CreditNote` beneath. Outlined "Make private again"
  → `MakePrivateCommand`.

- [ ] **Step 7: Register and verify**

```csharp
Routing.RegisterRoute("famStart",  typeof(FamStartPage));
Routing.RegisterRoute("famDraft",  typeof(FamDraftPage));
Routing.RegisterRoute("famSaved",  typeof(FamSavedPage));
Routing.RegisterRoute("famShared", typeof(FamSharedPage));
Routing.RegisterRoute("famPublic", typeof(FamPublicPage));
```

Register `IFamilyArchiveService` as a **Singleton** (privacy changes must persist across
screens) and the five ViewModel/page pairs as `Transient`.

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Check against canvas row 4: the draft checklist shows two green and two open; the audio card
says transcription pending on `famDraft` and "Transcript approved" on `famShared`; Kaunda
Mwaba's pill is gold-tinted while the other three are green-tinted; the last provenance dot
is `TzGoldDecor`.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat(mobile): implement family archive lifecycle (start, draft, saved, shared, public)

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 22: Profile collections and Settings (frames 29–33)

The four collection rows and the Settings row built in Task 17 currently go nowhere. These
five screens are their destinations. Each collection presents the **same dishes** in a
different frame, with a different piece of personal metadata attached — a saved date, a
reason, a cook count.

| Screen | Route | List | Personal metadata |
|---|---|---|---|
| `favs` | `favs` | 6 dishes | `when` saved + optional private note |
| `wantTry` | `wantTry` | 5 dishes | optional `why`, plus "Cook this next" / "Remove" |
| `cooked` | `cooked` | 5 dishes | times cooked, last cooked, optional note |
| `famList` | `famList` | 4 preserved recipes | who taught it, privacy pill, extras |
| `settings` | `settings` | 8 languages, 5 toggles | — |

**Files:**
- Create: `TasteZambia.Core/Models/Collections.cs`
- Create: `TasteZambia.Core/Services/CollectionsService.cs`
- Create: `TasteZambia.Core/ViewModels/CollectionViewModels.cs`, `SettingsViewModel.cs`
- Create: `TasteZambia.Mobile/Views/Collections/` — `FavouritesPage`, `WantToTryPage`, `CookedPage`, `FamilyRecipesPage`, `SettingsPage`
- Modify: `SeedData.cs`, `AppShell.xaml.cs`, `MauiProgram.cs`, `Views/ProfilePage.xaml`
- Test: `TasteZambia.Core.Tests/ViewModels/CollectionsTests.cs`

**Interfaces:**
- Produces:
  - `SavedEntry(string DishId, string When, string Note)`,
    `WishlistEntry(string DishId, string Why)`,
    `CookedEntry(string DishId, string Times, string Last, string Note)`
  - `LanguageStatus(string Name, string Note, bool IsCurrent)`
  - `SettingToggle(string Label, string Note, bool IsOn)`
  - `ICollectionsService`: `Saved`, `Wishlist`, `Cooked`, `Languages`, `Toggles`
  - `FavouritesViewModel`, `WantToTryViewModel`, `CookedViewModel`, `FamilyRecipesViewModel`, `SettingsViewModel`

- [ ] **Step 1: Write the failing test**

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class CollectionsTests
{
    private sealed class Nav : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoBackAsync() => Task.CompletedTask;
    }

    private static (ICatalogService cat, IFavouritesService fav, IPreferenceService pref) Deps()
        => (new CatalogService(new InMemoryDishRepository()), new FavouritesService(), new PreferenceService());

    [Fact]
    public async Task Favourites_AreSixDishesWithTwoCarryingNotes()
    {
        var (cat, fav, pref) = Deps();
        var vm = new FavouritesViewModel(new CollectionsService(), cat, fav, pref, new Nav());
        await vm.InitializeAsync();

        Assert.Equal(6, vm.Items.Count);
        Assert.Equal("Ifisashi", vm.Items[0].Dish.Title);
        Assert.Equal("Saved March 2026", vm.Items[0].When);
        Assert.True(vm.Items[0].HasNote);
        Assert.Equal("The one I cook most", vm.Items[0].Note);
        Assert.False(vm.Items[1].HasNote);
        Assert.Equal(2, vm.Items.Count(i => i.HasNote));
        Assert.Equal("14 recipes", vm.CountLabel);
    }

    [Fact]
    public async Task WantToTry_HasThreeReasonsOutOfFive()
    {
        var (cat, fav, pref) = Deps();
        var vm = new WantToTryViewModel(new CollectionsService(), cat, fav, pref, new Nav());
        await vm.InitializeAsync();

        Assert.Equal(5, vm.Items.Count);
        Assert.Equal("Munkoyo", vm.Items[0].Dish.Title);
        Assert.Contains("Grandfather", vm.Items[0].Why);
        Assert.Equal(3, vm.Items.Count(i => i.HasWhy));
        Assert.Equal("9 recipes", vm.CountLabel);
    }

    [Fact]
    public async Task Cooked_LeadsWithNshimaAtThirtyOneTimes()
    {
        var (cat, fav, pref) = Deps();
        var vm = new CookedViewModel(new CollectionsService(), cat, fav, pref, new Nav());
        await vm.InitializeAsync();

        Assert.Equal(5, vm.Items.Count);
        Assert.Equal("Nshima", vm.Items[0].Dish.Title);
        Assert.Equal("Cooked 31 times", vm.Items[0].Times);
        Assert.Equal("Yesterday", vm.Items[0].Last);
        Assert.Equal("23 recipes · 62 times cooked", vm.CountLabel);
    }

    [Fact]
    public async Task FamilyRecipes_ShowFourWithMixedPrivacy()
    {
        var vm = new FamilyRecipesViewModel(new FamilyArchiveService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(4, vm.Items.Count);
        Assert.Equal("Public", vm.Items[0].Privacy);
        Assert.Equal("Private", vm.Items[2].Privacy);
        Assert.Equal("4 preserved", vm.CountLabel);
    }

    [Fact]
    public async Task Settings_HasEightLanguagesAndFiveToggles()
    {
        var vm = new SettingsViewModel(new CollectionsService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(8, vm.Languages.Count);
        Assert.True(vm.Languages[0].IsCurrent);
        Assert.Equal("English", vm.Languages[0].Name);
        Assert.Contains("In progress", vm.Languages[3].Note);

        Assert.Equal(5, vm.Toggles.Count);
        Assert.True(vm.Toggles[0].IsOn);
        Assert.False(vm.Toggles[4].IsOn);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~CollectionsTests`
Expected: FAIL — `CollectionsService` does not exist.

- [ ] **Step 3: Write the models, seed data and service**

`TasteZambia.Core/Models/Collections.cs`:

```csharp
namespace TasteZambia.Core.Models;

public sealed record SavedEntry(string DishId, string When, string Note);
public sealed record WishlistEntry(string DishId, string Why);
public sealed record CookedEntry(string DishId, string Times, string Last, string Note);
public sealed record LanguageStatus(string Name, string Note, bool IsCurrent);
public sealed record SettingToggle(string Label, string Note, bool IsOn);
```

Append to `SeedData.cs`:

```csharp
public static readonly IReadOnlyList<SavedEntry> Saved =
[
    new("ifisashi", "Saved March 2026",    "The one I cook most"),
    new("chikanda", "Saved March 2026",    ""),
    new("nshima",   "Saved January 2026",  ""),
    new("kapenta",  "Saved January 2026",  "Mum makes this better"),
    new("inkoko",   "Saved December 2025", ""),
    new("delele",   "Saved December 2025", ""),
];

public static readonly IReadOnlyList<WishlistEntry> Wishlist =
[
    new("munkoyo",  "Grandfather used to make this. No one in the family wrote it down."),
    new("chikanda", "Want to try it before buying it at the market again."),
    new("kandolo",  ""),
    new("inkoko",   "For when the family visits at Christmas."),
    new("delele",   ""),
];

public static readonly IReadOnlyList<CookedEntry> Cooked =
[
    new("nshima",   "Cooked 31 times", "Yesterday",      ""),
    new("ifisashi", "Cooked 18 times", "Last week",      "Less water than the recipe says. Mine came out thin the first time."),
    new("kapenta",  "Cooked 7 times",  "Two weeks ago",  ""),
    new("kandolo",  "Cooked 4 times",  "August",         "Roasting beats boiling."),
    new("delele",   "Cooked 2 times",  "July",           ""),
];

public static readonly IReadOnlyList<LanguageStatus> LanguageStatuses =
[
    new("English", "Current interface language",            true),
    new("Bemba",   "Recipe and ingredient names available",  false),
    new("Nyanja",  "Recipe and ingredient names available",  false),
    new("Tonga",   "In progress · names being collected",    false),
    new("Lozi",    "In progress · names being collected",    false),
    new("Kaonde",  "Not started",                            false),
    new("Lunda",   "Not started",                            false),
    new("Luvale",  "Not started",                            false),
];

public static readonly IReadOnlyList<SettingToggle> SettingToggles =
[
    new("Keep recipes for offline cooking", "Saved and family recipes stay on this device. 38 MB used.", true),
    new("Download photos too",              "Uses more storage. Turn off on a limited data plan.",       true),
    new("New stories from the archive",     "About twice a month.",                                      true),
    new("Replies from the archive team",    "When a reviewer asks you something.",                       true),
    new("Family recipe activity",           "When a relative adds a note or a photo.",                   false),
];
```

`TasteZambia.Core/Services/CollectionsService.cs`:

```csharp
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Services;

public interface ICollectionsService
{
    IReadOnlyList<SavedEntry> Saved { get; }
    IReadOnlyList<WishlistEntry> Wishlist { get; }
    IReadOnlyList<CookedEntry> Cooked { get; }
    IReadOnlyList<LanguageStatus> Languages { get; }
    IReadOnlyList<SettingToggle> Toggles { get; }
}

public sealed class CollectionsService : ICollectionsService
{
    public IReadOnlyList<SavedEntry> Saved => SeedData.Saved;
    public IReadOnlyList<WishlistEntry> Wishlist => SeedData.Wishlist;
    public IReadOnlyList<CookedEntry> Cooked => SeedData.Cooked;
    public IReadOnlyList<LanguageStatus> Languages => SeedData.LanguageStatuses;
    public IReadOnlyList<SettingToggle> Toggles => SeedData.SettingToggles;
}
```

- [ ] **Step 4: Write the ViewModels**

`TasteZambia.Core/ViewModels/CollectionViewModels.cs`. Each row VM **wraps** a
`DishItemViewModel` rather than re-implementing it, so hearts and navigation keep working:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels.Items;

namespace TasteZambia.Core.ViewModels;

public sealed class SavedRowViewModel(DishItemViewModel dish, SavedEntry entry)
{
    public DishItemViewModel Dish { get; } = dish;
    public string When { get; } = entry.When;
    public string Note { get; } = entry.Note;
    public bool HasNote => Note.Length > 0;
}

public sealed class WishlistRowViewModel(DishItemViewModel dish, WishlistEntry entry)
{
    public DishItemViewModel Dish { get; } = dish;
    public string Why { get; } = entry.Why;
    public bool HasWhy => Why.Length > 0;
}

public sealed class CookedRowViewModel(DishItemViewModel dish, CookedEntry entry)
{
    public DishItemViewModel Dish { get; } = dish;
    public string Times { get; } = entry.Times;
    public string Last { get; } = entry.Last;
    public string Note { get; } = entry.Note;
    public bool HasNote => Note.Length > 0;
}

/// <summary>Shared plumbing: resolve a dish id to a DishItemViewModel.</summary>
public abstract class CollectionViewModelBase(
    ICatalogService catalog,
    IFavouritesService favourites,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    protected async Task<Dictionary<string, DishItemViewModel>> ResolveAsync(IEnumerable<string> ids)
    {
        var dishes = await catalog.GetDishesByIdsAsync(ids.Distinct().ToList());
        return dishes.ToDictionary(
            d => d.Id,
            d => new DishItemViewModel(d, favourites, preferences, Navigation));
    }

    [RelayCommand]
    protected Task BackToProfile() => Navigation.GoToAsync("//profile");
}

public sealed class FavouritesViewModel(
    ICollectionsService collections, ICatalogService catalog, IFavouritesService favourites,
    IPreferenceService preferences, INavigationService navigation)
    : CollectionViewModelBase(catalog, favourites, preferences, navigation)
{
    public ObservableCollection<SavedRowViewModel> Items { get; } = [];
    public string CountLabel => "14 recipes";

    public override async Task InitializeAsync()
    {
        if (Items.Count > 0) return;

        var lookup = await ResolveAsync(collections.Saved.Select(s => s.DishId));
        foreach (var entry in collections.Saved)
            if (lookup.TryGetValue(entry.DishId, out var dish))
                Items.Add(new SavedRowViewModel(dish, entry));
    }
}

public sealed class WantToTryViewModel(
    ICollectionsService collections, ICatalogService catalog, IFavouritesService favourites,
    IPreferenceService preferences, INavigationService navigation)
    : CollectionViewModelBase(catalog, favourites, preferences, navigation)
{
    public ObservableCollection<WishlistRowViewModel> Items { get; } = [];
    public string CountLabel => "9 recipes";

    public override async Task InitializeAsync()
    {
        if (Items.Count > 0) return;

        var lookup = await ResolveAsync(collections.Wishlist.Select(w => w.DishId));
        foreach (var entry in collections.Wishlist)
            if (lookup.TryGetValue(entry.DishId, out var dish))
                Items.Add(new WishlistRowViewModel(dish, entry));
    }
}

public sealed class CookedViewModel(
    ICollectionsService collections, ICatalogService catalog, IFavouritesService favourites,
    IPreferenceService preferences, INavigationService navigation)
    : CollectionViewModelBase(catalog, favourites, preferences, navigation)
{
    public ObservableCollection<CookedRowViewModel> Items { get; } = [];
    public string CountLabel => "23 recipes · 62 times cooked";

    public override async Task InitializeAsync()
    {
        if (Items.Count > 0) return;

        var lookup = await ResolveAsync(collections.Cooked.Select(c => c.DishId));
        foreach (var entry in collections.Cooked)
            if (lookup.TryGetValue(entry.DishId, out var dish))
                Items.Add(new CookedRowViewModel(dish, entry));
    }
}

public sealed partial class FamilyRecipesViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<PreservedRecipe> Items { get; } = [];
    public string CountLabel => $"{Items.Count} preserved";

    public override Task InitializeAsync()
    {
        if (Items.Count > 0) return Task.CompletedTask;
        foreach (var r in archive.PreservedRecipes) Items.Add(r);
        return Task.CompletedTask;
    }

    [RelayCommand] private Task PreserveAnother() => Navigation.GoToAsync("famStart");
    [RelayCommand] private Task BackToProfile()   => Navigation.GoToAsync("//profile");
}

public sealed partial class SettingsViewModel(
    ICollectionsService collections, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<LanguageStatus> Languages { get; } = [];
    public ObservableCollection<SettingToggle> Toggles { get; } = [];

    public IReadOnlyList<string> AccountRows { get; } =
    [
        "Profile and photo",
        "Who can see my contributions",
        "Download everything I have added",
        "About the archive",
    ];

    public override Task InitializeAsync()
    {
        if (Languages.Count > 0) return Task.CompletedTask;
        foreach (var l in collections.Languages) Languages.Add(l);
        foreach (var t in collections.Toggles) Toggles.Add(t);
        return Task.CompletedTask;
    }

    [RelayCommand] private Task BackToProfile() => Navigation.GoToAsync("//profile");
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~CollectionsTests`
Expected: PASS, 5 tests.

- [ ] **Step 6: Write the five pages**

All five open with the same header: a 34pt outlined circular back button
(`BackToProfileCommand`), a mono kicker carrying `CountLabel`, and a 30pt serif title. The
kicker's colour differs per collection — `TzClay` on Favourites, `TzGold` on Want to Try,
`TzGreenMid` on Cooked, `TzGreenDeep` on Family Recipes.

- **`FavouritesPage`** — rows of 74pt thumbnails via `PhotoOrPlaceholder`, 18pt serif title,
  English subtitle, the heart bound to `Dish.SaveGlyph`/`Dish.ToggleSaveCommand`, an
  optional note paragraph gated on `HasNote`, and `When` in mono.
- **`WantToTryPage`** — no thumbnail. 19pt serif name, English subtitle, a `TzGreenTint`
  time pill top-right, an optional `TzCream` "why" block gated on `HasWhy`, then a
  `TzGold` "Cook this next" and an outlined "Remove".
- **`CookedPage`** — `TzRule`-separated rows (no card), 60pt thumbnail, 18pt serif name,
  `Times` in `TzGreenMid` beside `Last` in `TzMuted2`, optional `TzCream` note block.
  Closes with a `TzCream` panel about notes staying private.
- **`FamilyRecipesPage`** — cards over `Items`: 19pt serif `Name`, "As taught by
  {TaughtBy}", a privacy pill using `BadgeBgHex`/`BadgeFgHex`, and `Extras` in mono. A
  dashed `TzGold` "+ Preserve another recipe" → `PreserveAnotherCommand`.
- **`SettingsPage`** — three sections. **Language**: explanatory paragraph, then rows over
  `Languages` — selected gets a `TzGreenTint` fill, 1.5pt `TzGreenDeep` stroke and an 18pt
  `TzGreenDeep` check; a `TzCream` "Help complete it" footer with `TzGold` link text.
  **Storage and notifications**: a bordered table over `Toggles`, each row a label, note and
  a 40x23 pill toggle (`TzGreenMid` track when on, `#2E221A12` off, 18pt knob).
  **Account**: a bordered table over `AccountRows` with chevrons. Closes with the deep-green
  "Your recipes stay yours" panel.

- [ ] **Step 7: Wire the Profile rows to their destinations**

In `ProfilePage.xaml`, the collection cards and settings rows are currently inert. Give
`ProfileViewModel` an `OpenCollectionCommand` taking the collection index and mapping
0→`favs`, 1→`wantTry`, 2→`cooked`, 3→`famList`, plus `OpenSettingsCommand` → `settings`.
Bind each `RecipeCollection` card and the Settings section header accordingly.

- [ ] **Step 8: Register and verify**

```csharp
Routing.RegisterRoute("favs",     typeof(FavouritesPage));
Routing.RegisterRoute("wantTry",  typeof(WantToTryPage));
Routing.RegisterRoute("cooked",   typeof(CookedPage));
Routing.RegisterRoute("famList",  typeof(FamilyRecipesPage));
Routing.RegisterRoute("settings", typeof(SettingsPage));
```

Register `ICollectionsService` as a Singleton and the five ViewModel/page pairs as Transient.

Run: `dotnet build TasteZambia.Mobile/TasteZambia.Mobile.csproj -f net10.0-ios`
Check against canvas row 5: each collection's kicker takes its own colour; only two
favourites carry notes; three of five wishlist entries carry a reason; the Cooked list is
rule-separated rather than carded; Settings shows English checked with Tonga and Lozi marked
"In progress"; every screen's back button returns to Profile with the nav bar on **Profile**.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat(mobile): implement profile collections and settings screens

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Part 2 Self-Review

**Spec coverage.** All 33 canvas screens now map to a task. Row 1 → Task 19; row 2 + the two
forms → Tasks 7–17; row 3 → Task 15 (`share`) + Task 20; row 4 → Task 16 (`family`) +
Task 21; row 5 → Task 22. The v2 contrast pass is in Task 1's tokens and Task 17's status
hexes. The expanded `NAV.profile.also` is in Task 5's mapping table.

**Placeholder scan.** Every new screen names its exact anatomy, colours and copy. Where a
page reuses an established pattern the pattern is named with its source task rather than
left blank. No "TBD", no unexplained "similar to".

**Type consistency.** `DishItemViewModel` is *wrapped*, never re-implemented, by the three
collection row VMs — hearts and `OpenCommand` therefore behave identically to Explore.
`PrivacyLevel` is the Task 2 enum, reused by `IFamilyArchiveService`. `ReviewState`/
`ReviewStep` (Task 20) are distinct from Task 15's `ReviewStage` — both exist deliberately:
`ReviewStage` is the static pipeline explainer shown *before* submitting, `ReviewStep` is
the live timeline shown *after*. Route strings match every `RegisterRoute` call and the
`NAV.profile.also` list.

**Two risks flagged for the executor.**
1. `OnboardingViewModel` is a **Singleton** for the onboarding run but the app root swaps to
   `AppShell` on completion. If onboarding is ever re-entered from Settings, the VM must be
   reset or re-registered as Scoped — otherwise stale selections persist.
2. `CollectionViewModelBase.ResolveAsync` calls `GetDishesByIdsAsync` once per screen. Against
   the in-memory repository that is free; against HTTP each collection screen becomes one
   round trip, which is correct, but do not move the call inside the item loop.
