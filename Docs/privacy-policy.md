# Taste Zambia — Privacy Policy

**Last updated:** 30 September 2026

Taste Zambia is a cultural archive of Zambian food. This policy says exactly what
the app collects, why, and what you can do about it. It describes what the app
actually does, not what it might do one day.

> **Before publishing:** replace `CONTACT_EMAIL` below with the address you will
> answer, and host this at a public URL — the Play Console requires one.

---

## The short version

There are no adverts, no analytics, and no trackers. Nothing is sold or shared
with anyone. You do not give a name, an email address or a phone number to use
the app. What the archive holds about you is what you chose to contribute to it.

## What the app collects

### An anonymous account

When you first open the app it generates two random values on your phone — a
device identifier and a secret — and keeps them in Android's encrypted storage.
They are your account. The archive stores the identifier and a cryptographic hash
of the secret, and nothing else is required to sign in.

This is not linked to your Google account, your phone number, your email, or your
advertising ID. We have no way to connect it to you as a person unless you tell us
who you are by filling in your profile.

If you uninstall the app, that account becomes unreachable — the secret was only
ever on that phone.

### Your profile, if you fill it in

Optional, and entirely free text you type: a display name, where you are, and the
languages you cook in. Anything you contribute to the archive is credited with the
name you put here, so other readers see it. Leave it blank and your contributions
are credited to no one.

### What you contribute

- **Recipes you share:** the dish name, description, ingredients, cooking steps,
  and what you write about where it comes from and what it means. Submitted
  recipes are read by a reviewer before they are published.
- **Photographs** you take or choose.
- **Voice recordings** you make — usually someone telling a recipe in their own
  words.
- **Family recipes:** the names and relationships of the relatives you invite, the
  invitation codes, and the notes any of you add. A family recipe is visible only
  to the people invited to it, unless you choose to make it public.

Please only record someone, or write down what they taught you, with their
knowledge.

### What you have saved

Which recipes you have saved, and which cooking steps you have marked done, with
the time you did so. This is sent to the archive so your lists survive a new
phone. It is a list of recipe identifiers and timestamps — nothing about how you
use the app beyond that.

### On the server

Your IP address appears in the archive's request logs and is used to limit how
many requests come from one place, which is what stops the archive being flooded.
Logs are ordinary server logs, kept short-term for operating and debugging the
service.

## What the app does not collect

- No advertising or analytics identifiers, and no third-party analytics of any kind.
- No location. The app never asks for or reads your GPS position. The "where you
  are" in your profile is text you type.
- No contacts. Inviting a relative to a family recipe gives you a code to pass on
  yourself; the app does not read your address book.
- No browsing or usage tracking.

## Permissions, and why

| Permission | Why |
|---|---|
| Internet, network state | To reach the archive, and to tell being offline from being broken. |
| Camera | Only when you choose to photograph a dish. |
| Microphone | Only when you choose to record someone telling a recipe. |
| Photos (Android 12 and below) | Only when you choose a photograph from your gallery. |

Each is used at the moment you ask for it and never in the background.

## Who else sees it

- **Published recipes** are public in the archive, credited with your profile name.
- **Recipes under review** are seen by the archive's reviewers.
- **Family recipes** are seen only by the people you invited, unless you make them
  public yourself.
- **Nobody else.** Nothing is sold, rented, or shared with advertisers, data
  brokers, or any third party. There are no third-party SDKs in the app that
  receive your data.

The archive runs on a server rented from a hosting provider, who necessarily
handles the data in transit and at rest as any host does.

## Keeping it

Contributions stay in the archive as long as it exists — that is what an archive
is for. Your saved lists and profile stay until you ask for them to be removed.
Server logs are short-lived.

## Your choices

- **Change your profile** at any time in the app.
- **Remove someone's access** to a family recipe you preserved, at any time.
- **Delete a photograph** from a draft before you submit it.
- **Ask for your account and everything in it to be deleted** by writing to
  `CONTACT_EMAIL` from the app, including your device identifier, which the
  Settings screen shows. Deletion removes your account, profile, saved lists,
  family recipes you own, and contributions that have not been published.

  Recipes already **published** in the archive are handled case by case: the
  credit can be removed so the recipe is anonymous, and a recipe can be withdrawn
  if the person who taught it asks. Say which you want.

- **Ask for a copy** of what the archive holds about you, at the same address.

## Children

Taste Zambia is not directed at children and does not knowingly collect anything
from a child under 13. If you believe a child has contributed, write to
`CONTACT_EMAIL` and it will be removed.

## Changes

If this policy changes in a way that affects what is collected or who sees it,
the app will say so before the change takes effect.

## Contact

`CONTACT_EMAIL`

---

# What the data safety form should say

The Play Console asks separately, and an answer that contradicts this policy is a
common reason for rejection. These are the consistent answers.

**Does your app collect or share any of the required user data types?** Yes.

| Data type | Collected | Shared | Optional? | Purpose |
|---|---|---|---|---|
| Name (profile display name) | Yes | No | Optional | App functionality (crediting contributions) |
| Photos | Yes | No | Optional | App functionality |
| Audio (voice recordings) | Yes | No | Optional | App functionality |
| Other user-generated content (recipe text, notes) | Yes | No | Optional | App functionality |
| Device or other IDs | Yes | No | Required | Account management |
| App interactions | No | — | — | — |
| Location | No | — | — | — |
| Contacts | No | — | — | — |

**Is all of the user data encrypted in transit?** Yes — HTTPS.

**Do you provide a way for users to request that their data be deleted?** Yes, by
the contact address above.

**Data collection is optional** for everything except the device identifier, which
is the account itself.

> **Not built yet, and Play will ask:** there is no in-app "delete my account"
> button. Google Play requires apps that create accounts to offer account deletion,
> with a request route reachable from the listing. Until that is built, the email
> route above is what this policy promises — it must be a live address that is
> actually answered.
