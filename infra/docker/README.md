# Deploying the Taste Zambia API

Follows `Docs/shared-vps-deployment-pattern.md`: one shared reverse-proxy stack
in front, one compose stack per app behind it. Nothing here needs nginx config,
certbot, or systemd.

What ships: the API and its Postgres. The mobile app is not deployed — it is
built and installed onto phones, and points at `API_HOST`.

---

## Before you start

You need three things decided:

| | |
|---|---|
| `API_HOST` | The hostname the phones will talk to, e.g. `api.tastezambia.co.zm`. **Its DNS A record must resolve to the server before you bring the stack up** — Let's Encrypt validates over HTTP and fails otherwise. |
| `LETSENCRYPT_EMAIL` | Where expiry warnings go. |
| Server access | An SSH key on the server. The first step below is the only one that runs as root. |

---

## 1. Prepare the server (once, as root)

Skip this entirely if the server already has the `deploy` user and the proxy
stack — go to step 3.

```bash
ssh root@YOUR_SERVER_IP

apt update && apt upgrade -y

# Docker Engine from the official script; the apt package lags badly.
curl -fsSL https://get.docker.com | sh

# A non-root user to own the apps. Nothing below this line needs root.
adduser --disabled-password --gecos "" deploy
usermod -aG docker,sudo deploy
mkdir -p /home/deploy/.ssh
cp /root/.ssh/authorized_keys /home/deploy/.ssh/
chown -R deploy:deploy /home/deploy/.ssh
chmod 700 /home/deploy/.ssh && chmod 600 /home/deploy/.ssh/authorized_keys

# Only SSH, HTTP and HTTPS. The database is never exposed.
ufw allow OpenSSH && ufw allow 80 && ufw allow 443 && ufw --force enable

exit
```

Confirm you can get back in as `deploy` **before** closing your root session:

```bash
ssh deploy@YOUR_SERVER_IP 'docker ps'
```

## 2. The shared proxy stack (once, as `deploy`)

```bash
ssh deploy@YOUR_SERVER_IP

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
docker compose ps        # both should be running
```

## 3. Bring up Taste Zambia

```bash
ssh deploy@YOUR_SERVER_IP
mkdir -p ~/apps/tastezambia && cd ~/apps/tastezambia
git clone https://github.com/Isaac-Zimba-J/TasteZambia.git .

cp infra/docker/.env.production.example .env.production
chmod 600 .env.production

# Generate the two secrets rather than inventing them:
echo "POSTGRES_PASSWORD=$(openssl rand -base64 32)"
echo "JWT_SIGNING_KEY=$(openssl rand -base64 48)"

nano .env.production          # paste those in, set API_HOST and LETSENCRYPT_EMAIL
```

A shorthand for the rest of this file:

```bash
alias tz='docker compose -f infra/docker/compose.production.yml --env-file .env.production'
```

Build, migrate, then start:

```bash
tz build

# Migrations are a deliberate step, never on startup. This also seeds the
# editorial archive - the eight dishes, the ingredients, the provinces, the
# articles - and the contributor/reviewer/admin roles.
tz run --rm api dotnet TasteZambia.API.dll --migrate

tz up -d
tz ps
```

Within 30–90 seconds acme-companion issues the certificate. Check:

```bash
curl -s https://$API_HOST/health          # {"status":"healthy"}
curl -s https://$API_HOST/api/v1/dishes | head -c 200
```

If the certificate has not appeared, the DNS record is the usual cause:

```bash
docker logs $(docker ps -qf name=acme-companion) --tail 40
```

## 4. Point the app at it

The phone build takes the API host at build time:

```bash
# on your Mac, from the repository root
dotnet build TasteZambia.Mobile -f net10.0-android -p:ArchiveApiHost=api.tastezambia.example
```

`scripts/android.sh` injects your Mac's LAN address for local work; a release
build passes the production host instead. The app speaks HTTPS to that host —
the `UsesCleartextTraffic` allowance is `#if DEBUG` only.

## 5. Make yourself a reviewer

Nothing seeds a privileged account in Production — a known password in a repo
is not a login. Grant the role to your own device account instead.

Sign in on the phone once so the account exists, then find it and promote it:

```bash
# on the server
psql -h 127.0.0.1 -p 5436 -U tastezambia -d tastezambia \
  -c 'select "UserName", "CreatedAt" from "AspNetUsers" order by "CreatedAt" desc limit 5;'
```

The first admin is a chicken-and-egg problem: `PUT /admin/users/{userName}/roles`
itself needs the admin role. Do the first one directly, once:

```bash
psql -h 127.0.0.1 -p 5436 -U tastezambia -d tastezambia <<'SQL'
insert into "AspNetUserRoles" ("UserId", "RoleId")
select u."Id", r."Id"
  from "AspNetUsers" u, "AspNetRoles" r
 where u."UserName" = 'device-xxxxxxxx'      -- yours, from the query above
   and r."Name" in ('reviewer', 'admin')
on conflict do nothing;
SQL
```

Sign out and in on the phone afterwards — roles travel in the token, so an old
one does not carry them.

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

The pattern gives you none. Two volumes hold everything that cannot be rebuilt:
`pgdata` (accounts, contributions, family recipes) and `media` (photographs and
voice recordings).

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
a backup.

## Housekeeping

```bash
docker system df                 # check monthly
docker builder prune -a -f       # build cache grows fast
docker compose -f infra/docker/compose.production.yml logs -f api
```

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
