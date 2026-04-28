# k6 load & stress tests (MyADA)

These scripts focus only on:

- **Event registration**
- **Event ticket scan/check-in**
- **Attendance QR token issuance**
- **Attendance QR scan**

They do **not** call any `DELETE` endpoints.

## Base URLs used

- Gateway: `BASE_URL` (default `http://13.60.31.141:5000`)
- Auth: `${BASE_URL}/api/auth/login`
- Club service via gateway: `${BASE_URL}/club/...`
- Attendance service via gateway: `${BASE_URL}/attendance/...`

## Required env vars

### Event (club) tests

- `ADMIN_USERNAME` / `ADMIN_PASSWORD` (used for `/events/{id}/check-in`)
- `STUDENT_USERNAMES` / `STUDENT_PASSWORDS` (comma-separated lists, same length)
- Optional: `EVENT_ID` to skip event discovery and test one known event directly

### Attendance QR tests

- `INSTRUCTOR_USERNAME` / `INSTRUCTOR_PASSWORD`
- `STUDENT_USERNAMES` / `STUDENT_PASSWORDS`

## Install k6 (Linux — AWS / CI runner)

Follow the official install for your distro: [Install k6](https://grafana.com/docs/k6/latest/set-up/install-k6/). Examples:

- **Debian/Ubuntu** (Grafana APT repo — see docs for the current key/repo lines).
- **Amazon Linux 2023**: often simplest to download the release binary from GitHub releases or use the RPM from Grafana’s instructions.

You also need **curl** and **python3** if you use the Pushgateway publisher script below.

## Run (Linux / bash)

From the repo root:

### 1) Event registration + ticket check-in (load)

```bash
export BASE_URL="http://13.60.31.141:5000"
export ADMIN_USERNAME="Admin"
export ADMIN_PASSWORD='Admin123*'
export STUDENT_USERNAMES="seed_user1,seed_user02"
export STUDENT_PASSWORDS="pass1,pass2"

k6 run load-tests/k6/club/load.js
```

By default the script discovers events with:

```text
GET /club/api/v1/events?myClubsOnly=false&page=1&limit=0
```

If you already know a valid event id, you can bypass discovery:

```bash
export EVENT_ID=123
k6 run load-tests/k6/club/load.js
```

### 2) Event registration + ticket check-in (stress)

```bash
export STRESS_START_VUS=10
export STRESS_MAX_VUS=200
export STRESS_RAMP_UP=5m
export STRESS_HOLD=5m
export STRESS_RAMP_DOWN=2m

k6 run load-tests/k6/club/stress.js
```

### 3) Attendance QR (load)

```bash
export INSTRUCTOR_USERNAME="your_instructor"
export INSTRUCTOR_PASSWORD='***'
export STUDENT_USERNAMES="seed_user1,seed_user02"
export STUDENT_PASSWORDS="pass1,pass2"

k6 run load-tests/k6/attendance/load.js
```

### 4) Attendance QR (stress)

```bash
k6 run load-tests/k6/attendance/stress.js
```

## Run (Windows PowerShell)

Same variables as Linux; use `$env:NAME="value"` and Windows paths, e.g. `k6 run .\load-tests\k6\club\load.js`.

## Tuning knobs (optional)

- `LOAD_VUS` (default `20`)
- `LOAD_DURATION` (default `5m`)
- `MAX_FAIL_RATE` (default `0.02`)
- `P95_MS` (default `1200`)
- `THINK_MIN_MS` / `THINK_MAX_MS` (default `200` / `1200`)

## Visualize in Grafana

This repo includes **Prometheus + Pushgateway + Grafana** in `docker-compose.yml` (typical for a dev/staging host).

**Linux / macOS** — run k6 and push summary metrics to Pushgateway:

```bash
chmod +x scripts/publish-k6-results-to-grafana.sh
export PUSHGATEWAY_URL="http://localhost:9091"   # or http://your-pushgateway:9091
./scripts/publish-k6-results-to-grafana.sh load-tests/k6/club/load.js club_load
```

**Windows** — same idea:

- `scripts/Publish-K6ResultsToGrafana.ps1`

Metrics pushed include `myada_k6_http_req_duration_p95_ms`, `myada_k6_http_req_failed_rate`, `myada_k6_checks_rate`, etc. Explore them in Prometheus or build a Grafana panel.

---

## After deploying to AWS — how to test

1. **Do not run heavy stress tests against production** unless that is explicitly approved, monitored, and isolated (separate data, off-hours, rollback plan). Prefer a **staging** environment that mirrors production sizing.

2. **Run k6 from a separate machine** (small EC2 “load runner”, GitHub Actions runner with fixed egress, or your laptop) so results are not distorted by k6 and the API sharing one CPU. Point `BASE_URL` at the **public or internal gateway URL** of the deployed stack (same paths: `/api/auth`, `/club/...`, `/attendance/...`).

3. **Secrets**: pass credentials via env vars or a secret store — never commit real passwords. On EC2 you can use `export $(grep -v '^#' .env.k6 | xargs)` from a file with `0600` permissions.

4. **Observability**:
   - If Pushgateway/Prometheus/Grafana run on the same VPC as the app, set `PUSHGATEWAY_URL` to that instance (security group must allow TCP 9091 from the k6 runner only).
   - If you have no Pushgateway in AWS, rely on k6’s **stdout summary** and `--summary-export` JSON, or use **k6 Cloud** / your existing APM.

5. **Order of operations**: deploy the **application** first; smoke-test manually; then run **load** (sustained moderate VUs); only then **stress** (ramp to failure) on non-prod.

---

## Should you deploy these repo changes to AWS?

**You do not need to deploy the k6 scripts to your API servers** for the app to work. They are tooling only.

- **Deploy** whatever **application** image/changes your release needs (gateway, services, configs).
- **Optional**: commit `load-tests/k6/` and `scripts/publish-k6-results-to-grafana.sh` to git so CI or a load-runner EC2 can `git clone` and run tests — that is versioning test assets, not “deploying” them into the production container.

If your deployment pipeline only ships Docker images, the new files under `load-tests/` and `scripts/` **do not need to be inside those images** unless you deliberately bundle them for convenience.

