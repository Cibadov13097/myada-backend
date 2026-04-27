function envInt(name, fallback) {
  const raw = __ENV[name];
  if (raw === undefined || raw === null || String(raw).trim() === '') return fallback;
  const n = parseInt(String(raw), 10);
  return Number.isFinite(n) ? n : fallback;
}

function envFloat(name, fallback) {
  const raw = __ENV[name];
  if (raw === undefined || raw === null || String(raw).trim() === '') return fallback;
  const n = parseFloat(String(raw));
  return Number.isFinite(n) ? n : fallback;
}

export function getConfig() {
  const baseUrl = (__ENV.BASE_URL || 'http://13.60.31.141:5000').replace(/\/+$/, '');
  return {
    baseUrl,
    clubPrefix: __ENV.CLUB_PREFIX || '/club',
    attendancePrefix: __ENV.ATTENDANCE_PREFIX || '/attendance',

    // Default load: keep conservative; override via env.
    loadVus: envInt('LOAD_VUS', 20),
    loadDuration: __ENV.LOAD_DURATION || '5m',

    // Default stress: ramp past normal.
    stressStartVus: envInt('STRESS_START_VUS', 10),
    stressMaxVus: envInt('STRESS_MAX_VUS', 200),
    stressRampUp: __ENV.STRESS_RAMP_UP || '5m',
    stressHold: __ENV.STRESS_HOLD || '5m',
    stressRampDown: __ENV.STRESS_RAMP_DOWN || '2m',

    // Thresholds
    maxFailRate: envFloat('MAX_FAIL_RATE', 0.02),
    p95Ms: envInt('P95_MS', 1200),

    // Think time between user actions
    thinkMinMs: envInt('THINK_MIN_MS', 200),
    thinkMaxMs: envInt('THINK_MAX_MS', 1200),
  };
}

export function randomThinkTimeMs(cfg) {
  const min = Math.max(0, cfg.thinkMinMs);
  const max = Math.max(min, cfg.thinkMaxMs);
  return Math.floor(min + Math.random() * (max - min + 1));
}

