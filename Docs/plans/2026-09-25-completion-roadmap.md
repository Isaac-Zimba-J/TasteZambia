# Taste Zambia — Completion Roadmap

The work between here and an app on Google Play talking to a hardened API. Four
subsystems, each with its own plan, each shippable on its own.

**Where we are:** 33 screens built, Stages 1–3 of the API merged (read-only archive,
anonymous device accounts, contributions and review), 163 Core + 89 API tests green.
Everything on the Profile, Share and archive screens is real. What remains is the
family archive, true offline, the production hardening, and the release itself.

| # | Plan | Delivers | Unblocks |
|---|---|---|---|
| **1** | `2026-09-25-stage4-media-family.md` | Photos and audio; the family archive tier | 5 family screens, photo upload in both wizards, `PreservedCount` |
| **2** | Stage 5 — transcription and delta sync | Transcription workflow; `?since=` cursors and tombstones; the mobile archive cache | The offline promise onboarding makes |
| **3** | Feature integration and housekeeping | Wishlist, settings toggles, collection notes, language status; motion pass; TalkBack | The last seeded services; the app feels finished |
| **4** | Security hardening and release | Rate limits, secrets, TLS, audit log, CI; signed AAB; deployed API | Google Play; a real audience |

---

## 1. Stage 4 — Media and the family archive

**Why first:** it is the largest block of fake content left. Five screens
(`famStart`, `famDraft`, `famSaved`, `famShared`, `famPublic`) read entirely from
`FamilyArchiveService`, which returns a hard-coded family — named members, notes,
an audio clip that does not exist. Photo upload is a dashed rectangle in both
wizards. Nothing else in the app is this far from true.

Access control is the hard part: a family recipe is visible to its owner plus
accepted members, and becomes public only on the owner's action. That rule has to
live in a service every query goes through, never in a controller.

**Detailed plan written.** See `2026-09-25-stage4-media-family.md`.

---

## 2. Stage 5 — Transcription and delta sync

**Delivers**
- A transcription queue driving `Pending → Transcribing → AwaitingApproval → Approved`.
  The archive team writes the transcript; the contributor approves it before it
  attaches. The design is explicit that the family approves the text.
- `GET /api/v1/sync/changes?since={cursor}` returning created and updated rows plus a
  tombstone list, paging on the `UpdatedAt` column Stage 1 already stamps.
- A `DeletedRow` table. Soft deletes are what make tombstones possible, and
  retrofitting them after real deletions have happened loses history.
- The **mobile archive cache**: the five read repositories get a cache decorator over
  `ILocalStore`, ETag-aware, falling back to disk when the API cannot be reached.

**Decide first:** cursor shape. `UpdatedAt` alone is unsafe under concurrent writes
sharing a millisecond; a composite `(UpdatedAt, Id)` cursor is not.

---

## 3. Feature integration and housekeeping

The services still returning `SeedData`, and the polish deferred while features landed.

**Still seeded**
- `ICollectionsService` — `Saved`, `Wishlist`, `Cooked`, `LanguageStatus`, `SettingToggle`.
  Saved and Cooked can be derived from `PersonalStore` today; **Wishlist needs a way to
  add to it** (a second heart on the recipe screen, or a "want to try" action) and a
  `WishlistEntry` row in the personal layer.
- `IPreferenceService` — title language and the verification badge are in memory only;
  they belong in `ILocalStore` beside onboarding.
- Settings toggles are inert: offline storage, photo downloads, story notifications,
  reviewer replies. Each needs a real effect or it should not be on screen.
- `SeedData.HomeStories`, `SeedData.Filters` — Home's story teasers and Explore's filter
  chips are hard-coded strings rather than archive content.

**Housekeeping**
- Motion pass: detail pushes do not slide, hearts do not respond, only the recipe sheet animates.
- TalkBack verification on device — labels are written, never tested.
- Recipe view counts, so the published screen's reach panel can come back.

---

## 4. Security hardening and release

**API hardening** (production-ready for a real audience)
- Secrets out of configuration entirely: `Jwt:SigningKey`, the connection string and the
  dev reviewer credentials come from the environment, and the app refuses to start in
  Production if any is missing or still the development value.
- Rate limiting: `POST /auth/device` and `/auth/refresh` per IP; `/me/sync` and
  `POST /me/contributions` per user. A device that can mint accounts without limit is a
  free write endpoint.
- HTTPS enforcement and HSTS behind the ingress; cleartext stays `#if DEBUG` on the app.
- Request size limits, especially once `POST /media` exists.
- CORS: deny by default; the mobile app needs no origin.
- Audit log on every review action — who published what, and when. A cultural archive
  whose provenance cannot be reconstructed is worth less than one that can.
- Dependency scanning in CI (`dotnet list package --vulnerable`), and CI running both
  suites with Testcontainers on every push.
- A threat-model pass on the anonymous-account design: what a stolen device secret
  gets you, what a lost phone costs, whether account transfer is needed before launch.

**Deployment**
- API: a multi-stage `Dockerfile`, migrations as a deliberate deploy step (never on
  startup in Production), a `/health` endpoint, structured logging. Cloud-neutral:
  container + `ConnectionStrings__Archive` + `Jwt__SigningKey` runs on Fly.io, Railway,
  Azure Container Apps or a VPS without changes.
- Android: release keystore, signed AAB, `versionCode`/`versionName` policy, Play
  listing (icon, screenshots, description, privacy policy — the app collects a device
  identifier and contributed content, which the listing must declare).
- **iOS is blocked**: .NET for iOS 26.4 needs Xcode 26.4; this machine has 26.3. Either
  upgrade Xcode or ship Android first. Nothing in the codebase blocks iOS — only the
  toolchain.

---

## Sequence

1 → 2 → 3 → 4. Stage 4 before Stage 5 because delta sync should carry media rows from
the start rather than be extended for them. Housekeeping third because it is cheapest
once no screen is still fake. Hardening last, against the finished surface — a security
pass over an API that is still growing endpoints has to be repeated.
