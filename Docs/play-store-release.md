# Releasing Taste Zambia to Google Play

The API is deployed separately; see `infra/docker/README.md`. This is the app.

Android only. iOS is blocked on tooling: .NET for iOS 26.4 needs Xcode 26.4 and
this machine has 26.3.

---

## 1. The upload key (once, and then never again)

Google Play signs the app you ship. What you hold is the **upload key**, which
proves a build came from you. Generate it once:

```bash
keytool -genkeypair -v \
  -keystore ~/keys/tastezambia-upload.keystore \
  -alias tastezambia-upload \
  -keyalg RSA -keysize 4096 -validity 10000 \
  -dname "CN=Taste Zambia, O=Taste Zambia, L=Lusaka, C=ZM"
```

It asks for a password twice. Use one password for both the store and the key.

**Then, before anything else:** copy `~/keys/tastezambia-upload.keystore` and its
password somewhere you will still have them in five years — a password manager and
one offline copy. Losing either means you can never update the app under this
listing again; the only way forward is a new listing, and everyone who installed
the old one is stranded on it.

Tell the build where it is:

```bash
cd TasteZambia.Mobile
cp signing.props.example signing.props        # gitignored
$EDITOR signing.props                         # path, alias, both passwords
```

A Release build without `signing.props` fails with a message saying so, rather
than quietly producing an `.aab` the Play Console rejects.

## 2. Point the app at the deployed archive

The API host is baked in at build time. There is no runtime setting, on purpose:
a shipped app cannot be talked into pointing somewhere else.

```
-p:ArchiveApiHost=104-237-6-144.sslip.io
```

A hostname becomes `https://…`; a bare IP would become `http://…:5080`, and a
Release build cannot use cleartext at all, so the host must be a name. If you buy
a domain later, this argument is the only thing that changes.

## 3. Build the bundle

```bash
# from the repository root
dotnet build TasteZambia.Mobile -c Release -f net10.0-android \
  -p:ArchiveApiHost=104-237-6-144.sslip.io
```

The `.aab` lands in
`TasteZambia.Mobile/bin/Release/net10.0-android/zm.tastezambia.mobile-Signed.aab`.

Check it before uploading:

```bash
unzip -p <the .aab> base/manifest/AndroidManifest.xml | strings | grep -i cleartext
```

Nothing should come back. `usesCleartextTraffic` is added in Debug only; if it
appears here, the build configuration is wrong and the app would be shipping a
permission it must not have.

### Version numbers

`ApplicationVersion` in `TasteZambia.Mobile.csproj` is Play's **version code**.
It must increase with every upload and can never be reused — not even for a build
the Console rejected. `ApplicationDisplayVersion` is what people see.

Bump `ApplicationVersion` for every upload. Bump `ApplicationDisplayVersion` when
the change is worth telling people about.

## 4. What the Console asks for

- **App name:** Taste Zambia
- **Short description (80 chars):** Zambian food, its recipes, and the stories and people behind them.
- **Full description:** the listing copy in `Docs/play-store-listing.md`.
- **Category:** Food & Drink
- **Content rating:** answer the questionnaire honestly. The app takes
  user-contributed text and photographs, which the questionnaire asks about.
- **Privacy policy URL:** required, because the app collects personal data.
  `Docs/privacy-policy.md` is the text; it has to be reachable at a public URL.
  The cheapest honest option is a GitHub Pages page on this repository.
- **Data safety form:** the answers are set out in `Docs/privacy-policy.md` under
  "What the data safety form should say". Filling it in inconsistently with the
  policy is a common rejection.
- **Screenshots:** at least two phone screenshots. Take them on the device with
  `adb exec-out screencap -p > shot.png`.
- **Feature graphic:** 1024×500.
- **Target audience:** not children. The app has no parental controls and takes
  free-text contributions, so claiming otherwise would pull in Families policy
  requirements it does not meet.

## 5. Ship it to yourself first

Upload to **internal testing**, not production. Install from the Play link on a
phone that has never had a debug build, and walk the app: onboarding, the archive
loads, search, submit a recipe, preserve a family one, add a photograph and a
recording.

This is the run that catches what a debug build cannot — a Release build is
shrunk and AOT-compiled, and it cannot use cleartext HTTP.

---

## Before you press publish

Read `infra/docker/README.md`'s "Not done yet" section. What is still missing
there is missing from the thing people will be installing.

`104-237-6-144.sslip.io` is a free DNS name pointing at one server with no
failover and backups that are only as good as the cron job. That is a reasonable
place to launch a small archive from; it is worth knowing it is what you are
launching from.
