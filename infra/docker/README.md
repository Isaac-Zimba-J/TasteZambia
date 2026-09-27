# Deploying the Taste Zambia API

Two paths. Follow **one**.

| | Use when | Address the phone dials |
|---|---|---|
| **A — bare IP** (this runbook) | No domain yet. Your own phone and a handful of testers. | `http://SERVER_IP:5080` |
| **B — domain + HTTPS** ([below](#path-b--when-you-have-a-domain)) | Real contributors, or the Play Store. | `https://api.yourdomain/` |

Path A has **no TLS**. Let's Encrypt validates DNS names, so it cannot issue a
certificate for an IP address — this is not a setting we skipped, it is not
available. [What that costs you](#what-path-a-costs-you) is worth reading before
you hand the address to anyone else.

What ships either way: the API and its Postgres. The mobile app is not deployed —
it is built and installed onto phones, and points at whatever address you chose.

---

# Path A — bare IP

## Before you start

You need two things:

| | |
|---|---|
| The server's public IP | Written `SERVER_IP` throughout. Substitute it literally. |
| SSH access as `root` | Step 1 is the only step that runs as root. |

Nothing else is decided in advance — no hostname, no email, no DNS.

## 1. Prepare the server (once, as root)

Skip to step 2 if the server already has a `deploy` user and Docker.

```bash
ssh root@SERVER_IP

apt update && apt upgrade -y

# Docker Engine from the official script; the apt package lags badly.
curl -fsSL https://get.docker.com | sh

# A non-root user to own the apps. Nothing after this step needs root.
adduser --disabled-password --gecos "" deploy
usermod -aG docker,sudo deploy
mkdir -p /home/deploy/.ssh
cp /root/.ssh/authorized_keys /home/deploy/.ssh/
chown -R deploy:deploy /home/deploy/.ssh
chmod 700 /home/deploy/.ssh && chmod 600 /home/deploy/.ssh/authorized_keys

# SSH and the API port. Not 80 or 443 - nothing is listening there on this path.
# The database is never opened; it binds to the server's loopback only.
ufw allow OpenSSH && ufw allow 5080/tcp && ufw --force enable

exit
```

Confirm you can get back in as `deploy` **before** you close the root session:

```bash
ssh deploy@SERVER_IP 'docker ps'
```

> If the server already runs other apps, check 5080 is free first:
> `ss -lntp | grep 5080`. If it is taken, pick another port and change it in
> **both** places — `API_PORT` in step 2 and `ufw allow` above — then remember
> the phone build in step 5 needs the same number.

## 2. Configuration

```bash
ssh deploy@SERVER_IP
mkdir -p ~/apps/tastezambia && cd ~/apps/tastezambia
git clone https://github.com/Isaac-Zimba-J/TasteZambia.git .

cp infra/docker/.env.production.ip.example .env.production
chmod 600 .env.production

# Generate the two secrets rather than inventing them:
echo "POSTGRES_PASSWORD=$(openssl rand -base64 32)"
echo "JWT_SIGNING_KEY=$(openssl rand -base64 48)"

nano .env.production      # paste both in; leave API_PORT at 5080
```

`JWT_SIGNING_KEY` must never change again. Every phone's session is signed with
it, so replacing it signs everyone out with no way back. The API refuses to
start in Production if you leave the example value in place.

A shorthand for the rest of this file — note the `.ip.` in the filename:

```bash
alias tz='docker compose -f infra/docker/compose.production.ip.yml --env-file .env.production'
```

## 3. Bring it up

```bash
tz build

# Migrations are a deliberate step, never on startup. This also seeds the
# editorial archive - the eight dishes, the ingredients, the provinces, the
# articles - and the contributor/reviewer/admin roles.
tz run --rm api dotnet TasteZambia.API.dll --migrate

tz up -d
tz ps                     # api and db both "running", api eventually "healthy"
```

## 4. Check it from outside the server

Run these **on your Mac**, not on the server — reaching it from the server
itself proves nothing about the firewall.

```bash
curl -s http://SERVER_IP:5080/health                        # {"status":"healthy"}
curl -s http://SERVER_IP:5080/api/v1/dishes | head -c 200   # the seeded archive
```

If `/health` hangs rather than refusing, it is the firewall or the provider's own
security group, not the app. If it refuses immediately, the container is not
listening — `tz logs api --tail 50`.

## 5. Point the phone at it

The API address is baked in at build time. Pass the server's IP instead of your
Mac's LAN address:

```bash
# on your Mac, from the repository root
dotnet build TasteZambia.Mobile -t:Run -f net10.0-android \
  -p:ArchiveApiHost=SERVER_IP
```

That produces `http://SERVER_IP:5080` inside the app, which is what step 4 just
proved works. `scripts/android.sh` keeps injecting your Mac's LAN address for
local work — the two are the same switch, different value.

**This has to be a Debug build.** .NET for Android adds
`usesCleartextTraffic="true"` to the manifest in Debug only, so a Release APK or
AAB will refuse every plain-HTTP request before it leaves the phone. That is
Android's rule, not ours. A Release build needs Path B.

## 6. Make yourself a reviewer

Nothing seeds a privileged account in Production — a known password in a
repository is not a login. Grant the role to your own device account instead.

Sign in on the phone once so the account exists, then find it:

```bash
tz exec -T db psql -U tastezambia -d tastezambia \
  -c 'select "UserName", "CreatedAt" from "AspNetUsers" order by "CreatedAt" desc limit 5;'
```

The first admin is a chicken-and-egg problem: `PUT /admin/users/{userName}/roles`
itself needs the admin role. Do the first one directly, once:

```bash
tz exec -T db psql -U tastezambia -d tastezambia <<'SQL'
insert into "AspNetUserRoles" ("UserId", "RoleId")
select u."Id", r."Id"
  from "AspNetUsers" u, "AspNetRoles" r
 where u."UserName" = 'device-xxxxxxxx'      -- yours, from the query above
   and r."Name" in ('reviewer', 'admin')
on conflict do nothing;
SQL
```

Sign out and back in on the phone afterwards — roles travel inside the token, so
the one it already holds does not carry them.

---

## Redeploying

```bash
cd ~/apps/tastezambia
git pull
tz build
tz run --rm api dotnet TasteZambia.API.dll --migrate     # only when migrations changed
tz up -d
```

## Backups — set this up on day one

Two volumes hold everything that cannot be rebuilt: `pgdata` (accounts,
contributions, family recipes) and `media` (photographs and voice recordings).
Nothing backs them up for you.

```bash
mkdir -p ~/backups
cat > ~/backups/tastezambia.sh <<'SH'
#!/bin/bash
set -euo pipefail
cd /home/deploy/apps/tastezambia
STAMP=$(date +%F)
docker compose -f infra/docker/compose.production.ip.yml --env-file .env.production \
  exec -T db pg_dump -U tastezambia tastezambia | gzip > ~/backups/db-$STAMP.sql.gz
docker run --rm -v tastezambia_media:/m -v /home/deploy/backups:/out alpine \
  tar czf /out/media-$STAMP.tar.gz -C /m .
find ~/backups -name '*.gz' -mtime +14 -delete
SH
chmod +x ~/backups/tastezambia.sh
( crontab -l 2>/dev/null; echo "15 2 * * * /home/deploy/backups/tastezambia.sh" ) | crontab -
```

**Copy the dumps off the server.** A backup on the same disk as the data is not
a backup. Also back up `.env.production` somewhere safe — without
`JWT_SIGNING_KEY` a restored database signs nobody in.

## Housekeeping

```bash
tz logs -f api
docker system df                 # check monthly
docker builder prune -a -f       # build cache grows fast
```

---

## What Path A costs you

Plain HTTP is readable and rewritable by anything between the phone and the
server — the Wi-Fi it is on, its mobile carrier, every network in between. In
concrete terms:

- **Session tokens travel in the clear.** Anyone who reads one can act as that
  reader until it expires: submit contributions in their name, open the family
  recipes shared with them.
- **Contributed content travels in the clear** — the recipe text, the
  photographs, the recording of somebody's grandmother.
- **Nothing proves the server is yours.** A network that answers for
  `SERVER_IP` first can serve the app whatever it likes.

That is an acceptable trade for your own phone and people you can tell in
person. It is not one to make on behalf of contributors who are trusting the
archive with a family recipe, and the Play Store will not ship a Release build
that talks to it at all.

## Getting HTTPS without buying anything

You need a **DNS name**, not a paid domain. Either of these gets you one free,
and then Path B works unchanged:

- **`sslip.io` / `nip.io`** — resolve automatically. If your server is
  `203.0.113.9`, the name `203-0-113-9.sslip.io` already points at it, today,
  with no account. Let's Encrypt issues for it happily. Set that as `API_HOST`.
- **DuckDNS / Afraid.org** — a free subdomain you register and point at the IP.
  Slightly nicer to read, one account to keep.

Both are real DNS names, so the certificate is real and Android is satisfied.
The only thing a paid domain buys you over these is a name you'd want on a
poster.

---

# Path B — when you have a domain

Same repository, different compose file: `compose.production.yml` instead of
`compose.production.ip.yml`, and `.env.production.example` instead of
`.env.production.ip.example`. It puts the API behind a shared nginx-proxy that
terminates TLS, per `Docs/shared-vps-deployment-pattern.md` — no nginx config,
no certbot, no systemd.

**Changes from Path A:**

1. **Firewall:** open 80 and 443 instead of 5080. The API no longer publishes a
   host port at all; the proxy reaches it over `proxy-net`.
   ```bash
   ufw allow 80 && ufw allow 443 && ufw delete allow 5080/tcp
   ```
2. **DNS first.** The A record for `API_HOST` must already resolve to the server
   before you bring the stack up — Let's Encrypt validates over HTTP and fails
   otherwise.
3. **The proxy stack**, once, as `deploy`:
   ```bash
   docker network create proxy-net

   mkdir -p ~/apps/proxy-stack && cd ~/apps/proxy-stack
   cat > docker-compose.yml <<'COMPOSE'
   services:
     nginx-proxy:
       image: nginxproxy/nginx-proxy
       restart: unless-stopped
       ports: ["80:80", "443:443"]
       volumes:
         - conf:/etc/nginx/conf.d
         - vhost:/etc/nginx/vhost.d
         - html:/usr/share/nginx/html
         - certs:/etc/nginx/certs:ro
         - /var/run/docker.sock:/tmp/docker.sock:ro
       networks: [proxy-net]

     acme-companion:
       image: nginxproxy/acme-companion
       restart: unless-stopped
       volumes_from: [nginx-proxy]
       volumes:
         - certs:/etc/nginx/certs
         - /var/run/docker.sock:/var/run/docker.sock:ro
       networks: [proxy-net]

   networks:
     proxy-net:
       external: true

   volumes:
     conf:
     vhost:
     html:
     certs:
   COMPOSE

   docker compose up -d && docker compose ps
   ```
4. **`.env.production`** gains `API_HOST` and `LETSENCRYPT_EMAIL`, and drops
   `API_PORT`. Keep `POSTGRES_PASSWORD` and `JWT_SIGNING_KEY` **exactly as they
   are** if you are moving an existing deployment across — changing the signing
   key signs every phone out.
5. **The alias** loses the `.ip.`:
   ```bash
   alias tz='docker compose -f infra/docker/compose.production.yml --env-file .env.production'
   ```
   Then `tz build && tz up -d`. Within 30–90 seconds the certificate appears:
   ```bash
   curl -s https://$API_HOST/health
   docker logs $(docker ps -qf name=acme-companion) --tail 40   # if it does not
   ```
6. **The phone build** takes the hostname, and no port:
   ```bash
   dotnet build TasteZambia.Mobile -f net10.0-android -p:ArchiveApiHost=api.yourdomain
   ```
   No code change: `ArchiveApiOptions` reads what you pass. A bare IP becomes
   `http://ip:5080`, a hostname becomes `https://host` — because an IP cannot
   have a certificate and a name can.

---

## Not done yet — read before taking real contributions

This deploys the API as it stands. The hardening in
`Docs/plans/2026-09-25-completion-roadmap.md` §4 is **not** in place:

- **No rate limiting.** `POST /auth/device` will mint accounts as fast as anyone
  asks. Fine for a closed test; not for a public address.
- **No audit log** on review actions — who published what is only inferable
  from `review_events`.
- **No CI**, so nothing but a person stops a broken commit reaching here.
- **No CORS policy**; the app needs none, but a browser client would.

The startup guards do refuse to run in Production with the sample signing key,
without a connection string, or with the `Reviewer` seeding section present.
