# Deploying the Taste Zambia API

Start to finish on a freshly wiped server. Follow it in order.

**The address the phone will dial:** `https://<your-ip-with-dashes>.sslip.io`

`sslip.io` is a public DNS service that resolves any IP encoded in the name —
`104-237-6-144.sslip.io` already points at `203.0.113.9`, today, with no account
and nothing to buy. Because it is a *real* DNS name, Let's Encrypt issues a real
certificate for it, so the app speaks HTTPS and a Release build works. A bare IP
could do neither.

Write your own down now and use it everywhere below:

```
104.237.6.144   = 203.0.113.9              <- yours
API_HOST    = 104-237-6-144.sslip.io     <- the same IP, dots swapped for dashes
```

What ships: the API and its Postgres. The mobile app is not deployed — it is
built and installed onto phones, pointed at `API_HOST`.

---

## 0. Get your SSH key onto the server

Everything else needs this working. On a fresh server your provider has either
already installed a key for you, or given you a root password.

### Do you have a key on your Mac?

```bash
ls -l ~/.ssh/id_ed25519.pub
```

If it is missing, make one. Press Enter at every prompt, adding a passphrase if
you want one:

```bash
ssh-keygen -t ed25519 -C "$(whoami)@$(hostname -s)"
```

### Option A — `ssh-copy-id` (easiest; needs the root password)

```bash
ssh-copy-id -i ~/.ssh/id_ed25519.pub root@104.237.6.144
```

It asks for the root password once, appends your key and fixes the permissions.

### Option B — paste it by hand (no password; provider console, or a key already works)

Copy the **public** key on your Mac:

```bash
cat ~/.ssh/id_ed25519.pub | pbcopy      # now on your clipboard
```

Paste it either into your provider's "SSH keys" box when creating the server, or
into the server's own file if you already have a way in:

```bash
ssh root@104.237.6.144
mkdir -p ~/.ssh && chmod 700 ~/.ssh
nano ~/.ssh/authorized_keys             # paste on its own line, save
chmod 600 ~/.ssh/authorized_keys
```

Never paste `id_ed25519` — the one **without** `.pub` stays on your Mac forever.

### Confirm before going on

```bash
ssh root@104.237.6.144 'echo key works'
```

If it still asks for a password it did not take. `ssh -v root@104.237.6.144` shows
which key it offered.

---

## 1. Prepare the server (once, as root)

```bash
ssh root@104.237.6.144

apt update && apt upgrade -y

# Docker Engine from the official script; the apt package lags badly.
curl -fsSL https://get.docker.com | sh

# A non-root user to own the apps. Nothing after this step needs root.
adduser --disabled-password --gecos "" deploy
usermod -aG docker,sudo deploy

# Give deploy the same key you just used, so you can log in as them.
mkdir -p /home/deploy/.ssh
cp /root/.ssh/authorized_keys /home/deploy/.ssh/
chown -R deploy:deploy /home/deploy/.ssh
chmod 700 /home/deploy/.ssh && chmod 600 /home/deploy/.ssh/authorized_keys

# SSH, HTTP and HTTPS. 80 is not optional - Let's Encrypt validates over it.
# The database is never opened; it binds to the server's loopback only.
ufw allow OpenSSH && ufw allow 80 && ufw allow 443 && ufw --force enable

exit
```

Confirm you can get back in as `deploy` **before** you close the root session —
if this fails you are locked out of your own server:

```bash
ssh deploy@104.237.6.144 'docker ps'
```

Check the name resolves to you. There is no DNS record to create and nothing to
wait for:

```bash
dig +short 104-237-6-144.sslip.io        # must print your IP
```

---

## 2. The shared proxy stack (once, as `deploy`)

This terminates TLS and fetches the certificate. One stack, in front of
everything you ever deploy on this server.

```bash
ssh deploy@104.237.6.144

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

docker compose up -d
docker compose ps        # both running
```

---

## 3. Get the code onto the server

The repository is private, so a plain `git clone` will ask for a password and
fail. Pick one of these three.

### Option A — deploy key (recommended: read-only, scoped to this one repo)

Make a key **on the server** and give GitHub its public half:

```bash
ssh deploy@104.237.6.144
ssh-keygen -t ed25519 -C "tastezambia-server" -f ~/.ssh/github_deploy -N ""
cat ~/.ssh/github_deploy.pub            # copy this whole line
```

In the browser: **GitHub → the TasteZambia repo → Settings → Deploy keys → Add
deploy key**. Paste it, title it `server`, leave *Allow write access*
**unchecked**, add.

Tell SSH to use it for GitHub, then clone:

```bash
cat >> ~/.ssh/config <<'CFG'
Host github.com
  IdentityFile ~/.ssh/github_deploy
  IdentitiesOnly yes
CFG
chmod 600 ~/.ssh/config

mkdir -p ~/apps/tastezambia && cd ~/apps/tastezambia
git clone git@github.com:Isaac-Zimba-J/TasteZambia.git .
```

A deploy key cannot push and cannot see your other repositories. Revoking it is
one click and it does not touch your own account.

### Option B — personal access token over HTTPS

GitHub → **Settings → Developer settings → Personal access tokens →
Fine-grained tokens**, scoped to this repository only, `Contents: Read-only`.

```bash
mkdir -p ~/apps/tastezambia && cd ~/apps/tastezambia
git clone https://github.com/Isaac-Zimba-J/TasteZambia.git .
# Username: your GitHub username     Password: paste the token

git config credential.helper store      # so `git pull` stops asking
```

That writes the token in cleartext to `~/.git-credentials`. Acceptable on a
server only you reach; a deploy key is better.

### Option C — forward your own key for one session

Nothing is stored on the server at all, but it only works while you are logged
in, so a redeploy you are not present for will fail.

```bash
ssh-add ~/.ssh/id_ed25519          # on your Mac, once
ssh -A deploy@104.237.6.144
git clone git@github.com:Isaac-Zimba-J/TasteZambia.git ~/apps/tastezambia
```

---

## 4. Configuration

```bash
cd ~/apps/tastezambia

cp infra/docker/.env.production.example .env.production
chmod 600 .env.production

# Generate the two secrets rather than inventing them:
echo "POSTGRES_PASSWORD=$(openssl rand -base64 32)"
echo "JWT_SIGNING_KEY=$(openssl rand -base64 48)"

nano .env.production
```

Fill in four things:

```
API_HOST=104-237-6-144.sslip.io       # yours, dashes not dots
LETSENCRYPT_EMAIL=isaacjuniorzimba@gmail.com   # where expiry warnings go
POSTGRES_PASSWORD=nGWkOUuStLYjMh/mYBv1RpbrIezfxBZhGPVcfPCjNqw=               # from above
JWT_SIGNING_KEY=YW3EdSAFBW0MxbamG1xUO2TcCGsJyuY184GsP/diK3BO0jvY5nxyQ5C60ybM5f59                 # from above
```

`JWT_SIGNING_KEY` must never change again. Every phone's session is signed with
it, so replacing it signs everyone out with no way back. The API refuses to
start in Production if you leave the example value in place.

A shorthand for the rest of this file:

```bash
alias tz='docker compose -f infra/docker/compose.production.yml --env-file .env.production'
```

---

## 5. Bring it up

```bash
tz build

# Migrations are a deliberate step, never on startup. This also seeds the
# editorial archive - the eight dishes, the ingredients, the provinces, the
# articles - and the contributor/reviewer/admin roles.
tz run --rm api dotnet TasteZambia.API.dll --migrate

tz up -d
tz ps                     # api and db running; api "healthy" within ~30s
```

The certificate arrives 30–90 seconds later. Watch it happen if you like:

```bash
docker logs -f $(docker ps -qf name=acme-companion)
```

---

## 6. Check it from your Mac

Run these **on your Mac**, not on the server — reaching it from the server
itself proves nothing about the firewall or the certificate.

```bash
curl -s https://104-237-6-144.sslip.io/health                        # {"status":"healthy"}
curl -s https://104-237-6-144.sslip.io/api/v1/dishes | head -c 200   # the seeded archive
```

Both must be **https** and must work without `-k`. If curl complains about the
certificate it has not been issued yet, and the acme-companion log says why. The
usual causes are port 80 closed, or `API_HOST` typed with dots instead of dashes.

> Let's Encrypt allows 5 certificates per week for the same exact name. If you
> are debugging, do not sit in a loop of `tz up -d --build` — read the log.

---

## 7. Point the phone at it

```bash
# on your Mac, from the repository root, phone connected
dotnet build TasteZambia.Mobile -t:Run -f net10.0-android \
  -p:ArchiveApiHost=104-237-6-144.sslip.io
```

That is the whole change. `ArchiveApiOptions` sees a hostname rather than an IP
and composes `https://104-237-6-144.sslip.io` — no port, no cleartext, so a
Release build behaves the same as a Debug one.

`scripts/android.sh` keeps injecting your Mac's LAN address for local work. The
two are the same switch with a different value: an IP gets `http://ip:5080`, a
name gets `https://name`.

---

## 8. Make yourself a reviewer

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
docker compose -f infra/docker/compose.production.yml --env-file .env.production \
  exec -T db pg_dump -U tastezambia tastezambia | gzip > ~/backups/db-$STAMP.sql.gz
docker run --rm -v tastezambia_media:/m -v /home/deploy/backups:/out alpine \
  tar czf /out/media-$STAMP.tar.gz -C /m .
find ~/backups -name '*.gz' -mtime +14 -delete
SH
chmod +x ~/backups/tastezambia.sh
( crontab -l 2>/dev/null; echo "15 2 * * * /home/deploy/backups/tastezambia.sh" ) | crontab -
```

**Copy the dumps off the server.** A backup on the same disk as the data is not
a backup. Back up `.env.production` too — without `JWT_SIGNING_KEY` a restored
database signs nobody in.

## Housekeeping

```bash
tz logs -f api
docker system df                 # check monthly
docker builder prune -a -f       # build cache grows fast
```

---

## When you buy a real domain

Point its A record at the server, change `API_HOST` in `.env.production`, run
`tz up -d`, and rebuild the app with the new name. acme-companion issues the new
certificate on its own. Nothing else moves — the database, the media and the
signing key stay where they are, so nobody is signed out.

## The no-DNS variant

`compose.production.ip.yml` and `.env.production.ip.example` serve the API as
plain HTTP on `http://104.237.6.144:5080`, with no proxy and no certificate. They
exist for a server that cannot resolve any name at all. Use them only if you
have to: session tokens and contributed recordings travel readable, and a
Release build refuses cleartext outright, so it can never reach the Play Store.
`sslip.io` costs nothing and avoids all of that.

---

## Not done yet — read before taking real contributions

This deploys the API as it stands. Rate limiting is in place - per address on
`POST /auth/device` and `/auth/refresh`, per account on writes, with a backstop on
everything else, and the health check exempt. The numbers are in the `RateLimits`
section of `appsettings.json` and can be overridden per deployment with
`RateLimits__DeviceAuthPerWindow` and friends; they are set for many readers
sharing one carrier address, not for one phone.

The rest of the hardening in `Docs/plans/2026-09-25-completion-roadmap.md` §4 is
**not** in place:

- **No audit log** on review actions — who published what is only inferable
  from `review_events`.
- **No CI**, so nothing but a person stops a broken commit reaching here.
- **No CORS policy**; the app needs none, but a browser client would.

The startup guards do refuse to run in Production with the sample signing key,
without a connection string, or with the `Reviewer` seeding section present.
