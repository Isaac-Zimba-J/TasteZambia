# Shared-VPS deployment pattern

How to structure apps so that one Linux server can host many independent
web apps (each with its own backend, database, and frontend) without
per-app nginx configs, manual TLS, or systemd. Written for reuse when
setting up a fresh server for TasteZambia and other future apps.

Written for: engineers who will host TasteZambia's API + any additional
apps on a shared VPS.

---

## The core idea

One **reverse-proxy stack** in front, **one docker-compose stack per app**
behind it, everything wired together by container environment variables.
Adding a new app is: `git clone`, create a `.env`, `docker compose up -d`,
and DNS. That's it — no sudo, no nginx edits, no certbot commands.

```
┌────────────────────────────────────────────────────────────────────┐
│  Server (Ubuntu + Docker + `deploy` user in the `docker` group)    │
│                                                                    │
│  ┌──────────────────── proxy-stack ─────────────────────────┐     │
│  │  nginx-proxy       ← holds :80/:443 on the host          │     │
│  │  acme-companion    ← auto Let's Encrypt certs + renewal  │     │
│  └──────────────────────────┬──────────────────────────────┘     │
│                             │  (docker network: proxy-net)         │
│    ┌────────────┬───────────┼────────────┬────────────┐            │
│    ▼            ▼           ▼            ▼            ▼            │
│  app-A       app-B      TasteZ        app-D        app-E           │
│  compose     compose    compose       compose      compose         │
│                                                                    │
│  Each public container declares:                                   │
│    VIRTUAL_HOST=foo.example.com                                    │
│    VIRTUAL_PORT=8080                                               │
│    LETSENCRYPT_HOST=foo.example.com                                │
│    LETSENCRYPT_EMAIL=you@example.com                               │
│  …and joins `networks: [default, proxy-net]`.                      │
└────────────────────────────────────────────────────────────────────┘
```

**How the wiring works:**

- `nginx-proxy` watches the Docker socket. Every time a container appears
  with `VIRTUAL_HOST=x.com`, it writes a matching `server` block that
  proxies to that container on `VIRTUAL_PORT` over the shared network.
- `acme-companion` watches for `LETSENCRYPT_HOST`. It runs an HTTP-01
  challenge (using nginx-proxy's fallback location), stores the cert on a
  shared volume, tells nginx-proxy to reload, and renews on its own timer.
- No sudo. No per-app config file. Remove a container and its vhost is
  removed with it.

---

## Standard app layout

```
~/apps/<app-name>/
├── docker-compose.yml           # or infra/docker/compose.production.yml
├── .env.production              # secrets, chmod 600, NEVER committed
├── api/                         # backend source
│   └── Dockerfile               # multi-stage build
├── web/                         # frontend source (if any)
│   ├── Dockerfile               # e.g. node build → nginx-alpine serve
│   └── nginx.conf               # SPA fallback + cache headers
└── infra/docker/secrets/        # PEM keys etc, chmod 600, NEVER committed
```

Every service that needs public HTTPS gets the four env vars above.
Anything private — postgres, redis, background workers — does NOT get
those vars and stays on the internal `default` network only.

---

## Bringing up a fresh server

### 1. Provision

Ubuntu 22.04 or 24.04 LTS. A small VPS is fine: 2 vCPU / 4 GB RAM /
40 GB disk fits several small apps. Run `apt update && apt upgrade` first.

### 2. One-time system prep (as root)

```bash
# Docker Engine — official install script, not the outdated `apt` package
curl -fsSL https://get.docker.com | sh

# Non-root deploy user in the docker + sudo groups
adduser --disabled-password --gecos "" deploy
usermod -aG docker,sudo deploy
mkdir -p /home/deploy/.ssh
cp /root/.ssh/authorized_keys /home/deploy/.ssh/     # or paste your own key
chown -R deploy:deploy /home/deploy/.ssh
chmod 700 /home/deploy/.ssh && chmod 600 /home/deploy/.ssh/authorized_keys

# Firewall — allow only SSH, HTTP, HTTPS
ufw allow OpenSSH && ufw allow 80 && ufw allow 443 && ufw --force enable
```

Log out of root. Everything from here on runs as `deploy`.

### 3. Create the shared proxy stack (as `deploy`)

```bash
docker network create proxy-net

mkdir -p ~/apps/proxy-stack && cd ~/apps/proxy-stack
cat > docker-compose.yml <<'EOF'
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
EOF

docker compose up -d
```

That is the whole reverse-proxy + TLS layer. It is stable — you rarely
touch it again.

### 4. Ship your first app

```bash
mkdir -p ~/apps/myapp && cd ~/apps/myapp
# git clone your repo here, or rsync from your laptop
```

The app's `docker-compose.yml` needs three things: public services declare
the four env vars, private services do not, and the compose file references
the external `proxy-net`.

```yaml
services:
  api:
    build: ./api
    environment:
      VIRTUAL_HOST: api.myapp.com
      VIRTUAL_PORT: "8080"
      LETSENCRYPT_HOST: api.myapp.com
      LETSENCRYPT_EMAIL: you@example.com
      # …your app config (connection strings, secrets from .env)…
    networks: [default, proxy-net]

  web:
    build: ./web
    environment:
      VIRTUAL_HOST: "myapp.com,www.myapp.com"
      VIRTUAL_PORT: "8080"
      LETSENCRYPT_HOST: "myapp.com,www.myapp.com"
      LETSENCRYPT_EMAIL: you@example.com
    networks: [default, proxy-net]

  postgres:
    image: postgres:16-alpine
    # NO VIRTUAL_HOST — this stays private
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "127.0.0.1:5435:5432"   # loopback-only, for host-side psql
    networks: [default]

networks:
  proxy-net:
    external: true              # ← key line: use the shared network

volumes:
  postgres_data:
```

Then:

1. Point your DNS A records at the server's IP (apex, `www`, `api` — whatever
   hosts you declared).
2. `docker compose up -d --build`.
3. Wait 30–90 s for acme-companion to issue certificates. Done.

---

## Rules that make this scale

| Rule | Why |
|---|---|
| One `docker-compose.yml` per app in `~/apps/<name>/` | Isolation. `docker compose down` only touches this app. |
| Databases and workers stay on `default` network, never `proxy-net` | Nginx-proxy cannot accidentally expose them. |
| Every public container declares all four `VIRTUAL_*` / `LETSENCRYPT_*` vars | Automatic vhost and TLS with no other config. |
| Loopback-only host ports for admin access, e.g. `127.0.0.1:5435:5432` | You can `psql` from the host, nothing from outside. |
| Secrets live in `.env.production` (chmod 600, never committed) or in `secrets/*.pem` mounts | Rotating a key is edit one file + restart one container. |
| Pick unique loopback ports per app: 5433, 5434, 5435… | Multiple apps can each map their Postgres to the host. |
| `docker builder prune -a -f` on a schedule | Build cache grows fast. It reached 16 GB on one existing server before cleanup. |
| DNS first, then bring the app up | acme-companion needs the hostname to resolve or the challenge fails. |

---

## Gotchas to plan for from day one

1. **Disk fills up quietly.** Build cache, old images, and Postgres data
   grow. Watch `docker system df` monthly. `/var/lib/docker` on the root
   partition is the usual pain point — if you can, point Docker's
   `data-root` at a larger disk in `/etc/docker/daemon.json`.

2. **Framework session / signing keys must be persistent.** ASP.NET Core,
   Django, and others generate ephemeral keys by default. Every container
   restart then invalidates every session or JWT. Mount real keys from a
   `secrets/` directory:

   ```yaml
   volumes:
     - ./secrets:/run/app-secrets:ro
   entrypoint: ["sh", "-c",
     "export Jwt__PrivateKey=\"$$(cat /run/app-secrets/jwt-private.pem)\" && exec dotnet MyApp.dll"]
   ```

3. **Migrations are per-app.** Standardize on a one-shot command
   (`docker compose run --rm api dotnet MyApp.dll --migrate`) so
   first-time bring-up is scripted.

4. **Backups.** Named volumes (`postgres_data`, `api_uploads`) are your
   database and uploaded files. Set up nightly `pg_dump` and copy the
   dump off-server. The pattern gives you zero backup by default.

5. **CORS.** If your API is on `api.myapp.com` and the frontend is on
   `myapp.com`, the API must allow that origin. Set
   `Cors__AllowedOrigins__0: "https://myapp.com"` (ASP.NET) or the
   equivalent for your framework.

6. **Frontend production env vars.** For SPAs, remember that the build
   bakes in the API URL. Verify the built bundle actually contains the
   production URL after each build:

   ```bash
   curl -s https://myapp.com/ | grep -oE 'main-[A-Z0-9]+\.js'      # find bundle
   curl -s https://myapp.com/main-XXX.js | grep -oE 'https?://[^"]+'
   ```

7. **DNS + Let's Encrypt rate limits.** If you cycle a hostname on/off
   many times while testing, you can hit LE's 5-certs-per-week limit.
   Use their staging endpoint (`ACME_CA_URI` on acme-companion) for
   experimentation.

---

## Adding a new app later

Once the proxy stack and `deploy` user exist, adding a fifth or fifteenth
app is always the same three steps:

```bash
ssh deploy@server
mkdir -p ~/apps/next-app && cd ~/apps/next-app
git clone <repo> .
cp .env.production.example .env.production
nano .env.production            # fill in secrets
docker compose up -d --build
```

Combined with DNS pointing to the server, TLS and routing come up on
their own within ~90 seconds.

---

## Reference: what a working example looks like

The FinHive ATLAS repo has a real-world implementation of this pattern
under `infra/docker/`:

- `compose.production.yml` — 4-service stack (api, web, postgres, redis)
  joined to `proxy-net`, with PEM keys mounted from `secrets/`
- `.env.production.example` — template for the secrets file
- `README.md` — first-time bring-up + redeploy runbook
- API `Dockerfile` — multi-stage .NET 10 SDK → aspnet runtime
- Web `Dockerfile` + `nginx.conf` — node build → nginx-alpine serve with
  SPA fallback

Copy those files as a starting point when standing up TasteZambia's
deployment. The only per-app changes are: image names, `VIRTUAL_HOST`
values, database name, and app-specific env vars.
