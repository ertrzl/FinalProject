// Shared API helper: talks to our own backend (same origin, so no CORS needed).
// Every other page's JS will call apiFetch()/apiFetchForm() instead of using fetch() directly.

const SESSION_KEY = "socialnet-session"; // { accessToken, expiresAt, userId, fullName, avatarUrl }

function saveSession(tokenResponse) {
  localStorage.setItem(SESSION_KEY, JSON.stringify(tokenResponse));
}

function getSession() {
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch (e) {
    return null;
  }
}

function clearSession() {
  localStorage.removeItem(SESSION_KEY);
}

function getToken() {
  const session = getSession();
  return session ? session.accessToken : null;
}

// Any page that requires login calls this first; sends the visitor back to index.html otherwise.
function requireAuth() {
  const session = getSession();
  if (!session) {
    window.location.href = "index.html";
    return null;
  }
  return session;
}

function logout() {
  clearSession();
  window.location.href = "index.html";
}

// JSON requests (most endpoints): body is a plain object, auto-stringified.
async function apiFetch(path, options = {}) {
  const headers = { "Content-Type": "application/json", ...(options.headers || {}) };
  const token = getToken();
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const response = await fetch(path, {
    ...options,
    headers,
    body: options.body ? JSON.stringify(options.body) : undefined
  });

  return handleResponse(response, !!token);
}

// multipart/form-data requests (endpoints with [FromForm], e.g. file uploads): pass a FormData body as-is.
async function apiFetchForm(path, options = {}) {
  const headers = { ...(options.headers || {}) };
  const token = getToken();
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const response = await fetch(path, { ...options, headers });
  return handleResponse(response, !!token);
}

// hadTokenAttached distinguishes "your session expired" (401 on an authenticated request,
// e.g. a stale token on the feed) from "this request itself failed" (401 on login = wrong password).
async function handleResponse(response, hadTokenAttached) {
  if (response.status === 401 && hadTokenAttached) {
    clearSession();
    window.location.href = "index.html";
    throw new Error("Oturum sona erdi.");
  }

  if (response.status === 204) return null;

  // Validation errors come back as "application/problem+json", not "application/json" —
  // matching on "json" alone catches both instead of silently swallowing the real error.
  const isJson = (response.headers.get("content-type") || "").includes("json");
  const data = isJson ? await response.json() : null;

  if (!response.ok) {
    throw new Error(extractErrorMessage(data));
  }

  return data;
}

// Two different error shapes can come back:
// - our own GlobalExceptionMiddleware: { statusCode, message }
// - [ApiController]'s automatic FluentValidation 400s: ASP.NET's ValidationProblemDetails,
//   { title, errors: { FieldName: ["reason", ...] } } — no "message" field at all.
function extractErrorMessage(data) {
  if (!data) return "Bir hata oluştu.";
  if (data.message) return data.message;
  if (data.errors) {
    return Object.values(data.errors).flat().join(" ");
  }
  return data.title || "Bir hata oluştu.";
}

// ---- Shared rendering helpers (every page-specific *.js file uses these) ----

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text == null ? "" : text;
  return div.innerHTML;
}

function timeAgo(isoDate) {
  // ASP.NET Core serializes DateTime (not DateTimeOffset) without a "Z" suffix even though it's UTC —
  // without it, JS's Date parser treats the string as local time and skews every timestamp.
  const utcDate = /Z$|[+-]\d\d:\d\d$/.test(isoDate) ? isoDate : `${isoDate}Z`;
  const seconds = Math.floor((Date.now() - new Date(utcDate).getTime()) / 1000);
  if (seconds < 60) return "Şimdi";
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes} dakika önce`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} saat önce`;
  const days = Math.floor(hours / 24);
  return `${days} gün önce`;
}

const DEFAULT_AVATAR = "/images/default-avatar.png";
