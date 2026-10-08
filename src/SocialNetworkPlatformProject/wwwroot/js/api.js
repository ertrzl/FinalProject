// Shared API helper: talks to our own backend (same origin, so no CORS needed).
// Every other page's JS will call apiFetch()/apiFetchForm() instead of using fetch() directly.

const SESSION_KEY = "socialnet-session"; // { accessToken, expiresAt, refreshToken, userId, fullName, avatarUrl }

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

// Keeps the navbar/composer avatars in sync right after a profile edit, without forcing a re-login.
function updateSessionAvatar(avatarUrl) {
  const session = getSession();
  if (!session) return;
  session.avatarUrl = avatarUrl;
  saveSession(session);
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

async function logout() {
  const session = getSession();
  clearSession();
  if (session?.refreshToken) {
    // Best-effort — invalidates the refresh token server-side so it can't be replayed later.
    // Never blocks the redirect: a network hiccup shouldn't trap the user on the page.
    fetch("/api/auth/logout", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: session.refreshToken })
    }).catch(() => {});
  }
  window.location.href = "index.html";
}

// Access tokens are short-lived; this refreshes one just before it expires, using the long-lived
// refresh token, so the user is never bounced back to the login page mid-session.
let refreshPromise = null;

// forceRefresh = true skips the "still fresh enough" shortcut: used when the server turned a token down.
async function getValidAccessToken(forceRefresh = false) {
  const session = getSession();
  if (!session) return null;

  const expiresAtMs = new Date(/Z$|[+-]\d\d:\d\d$/.test(session.expiresAt) ? session.expiresAt : `${session.expiresAt}Z`).getTime();
  if (!forceRefresh && Number.isFinite(expiresAtMs) && expiresAtMs - Date.now() > 30000) {
    return session.accessToken;
  }

  if (!session.refreshToken) {
    clearSession();
    window.location.href = "index.html";
    throw new Error("Oturum sona erdi.");
  }

  // Several requests can notice the stale token at nearly the same time — only one of them should refresh.
  if (!refreshPromise) {
    refreshPromise = fetch("/api/auth/refresh", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: session.refreshToken })
    })
      .then(response => { if (!response.ok) throw new Error("refresh failed"); return response.json(); })
      .finally(() => { refreshPromise = null; });
  }

  try {
    const fresh = await refreshPromise;
    saveSession(fresh);
    return fresh.accessToken;
  } catch (err) {
    // Another tab may have refreshed with the same token a moment ago (the server accepts each token once and
    // that tab already stored the new pair): then this tab simply carries on with it.
    const current = getSession();
    if (current && current.refreshToken && current.refreshToken !== session.refreshToken) {
      return current.accessToken;
    }

    clearSession();
    window.location.href = "index.html";
    throw new Error("Oturum sona erdi.");
  }
}

// Sends a request with the access token attached. A 401 for a token that has not expired means the server no longer
// accepts it (the password was changed, the signing key was replaced...): the session is renewed once and the request
// is repeated. Only when the renewal fails too does the user go back to the login page (getValidAccessToken does that).
async function fetchWithToken(path, init) {
  const send = token => fetch(path, {
    ...init,
    headers: { ...(init.headers || {}), ...(token ? { Authorization: `Bearer ${token}` } : {}) }
  });

  let token = await getValidAccessToken();
  let response = await send(token);

  if (response.status === 401 && token) {
    token = await getValidAccessToken(true);
    response = await send(token);
  }

  return { response, hadToken: !!token };
}

// JSON requests (most endpoints): body is a plain object, auto-stringified.
async function apiFetch(path, options = {}) {
  const { response, hadToken } = await fetchWithToken(path, {
    ...options,
    headers: { "Content-Type": "application/json", ...(options.headers || {}) },
    body: options.body ? JSON.stringify(options.body) : undefined
  });

  return handleResponse(response, hadToken);
}

// Authenticated file download: a plain <a href> can't send the bearer token, so fetch the file and save the blob.
// The file name comes from the response's Content-Disposition (fallbackName if it is missing).
async function downloadApiFile(path, fallbackName) {
  const { response, hadToken } = await fetchWithToken(path, {});
  if (!response.ok) await handleResponse(response, hadToken); // throws with the server's message

  const disposition = response.headers.get("content-disposition") || "";
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
  const fileName = match ? decodeURIComponent(match[1]) : fallbackName;

  const url = URL.createObjectURL(await response.blob());
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

// multipart/form-data requests (endpoints with [FromForm], e.g. file uploads): pass a FormData body as-is.
async function apiFetchForm(path, options = {}) {
  const { response, hadToken } = await fetchWithToken(path, options);
  return handleResponse(response, hadToken);
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

// Turns "#tag" into a link to its search results. Text must already be escaped (hashtags contain
// no characters escapeHtml touches, so running this after escaping is safe).
function linkifyHashtags(escapedText) {
  return escapedText.replace(/(^|[\s(])#([\p{L}0-9_]+)/gu,
    (match, before, tag) => `${before}<a href="search.html?q=${encodeURIComponent("#" + tag)}" class="text-primary text-decoration-none">#${tag}</a>`);
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

// Shown where an item has no photo of its own (listing, group cover, event cover): a neutral grey "no photo"
// picture, so nobody mistakes a stand-in for the real product.
const NO_PHOTO = "/images/no-photo.svg";

// Yellow star row for a rating out of 5 (full / half / empty stars, rounded to the nearest half):
// starsHtml(4.3) shows four full stars and one half star.
function starsHtml(value) {
  const rounded = Math.round((value || 0) * 2) / 2;
  let stars = "";
  for (let i = 1; i <= 5; i++) {
    const icon = rounded >= i ? "bi-star-fill" : rounded >= i - 0.5 ? "bi-star-half" : "bi-star";
    stars += `<i class="bi ${icon} rating-star"></i>`;
  }
  return `<span class="rating-stars" title="${value} / 5">${stars}</span>`;
}

// Event times (startsAt / endsAt) arrive as UTC ("...Z"), so `new Date` shows them in the viewer's own time zone.
// "6 Ekim Salı · 18:00". Shared by events.html, event.html and group.html.
function formatEventDate(isoDate) {
  const date = new Date(isoDate);
  return date.toLocaleDateString("tr-TR", { day: "numeric", month: "long", weekday: "long" })
    + " · " + date.toLocaleTimeString("tr-TR", { hour: "2-digit", minute: "2-digit" });
}

function formatEventTime(date) {
  return date.toLocaleTimeString("tr-TR", { hour: "2-digit", minute: "2-digit" });
}

// "6 Ekim Salı · 18:00 – 21:00", or "6 Ekim · 22:00 → 7 Ekim · 02:00" when the event runs past midnight.
function formatEventRange(startsAt, endsAt) {
  const start = new Date(startsAt);
  const end = new Date(endsAt);
  if (start.toDateString() === end.toDateString()) {
    return `${formatEventDate(startsAt)} – ${formatEventTime(end)}`;
  }
  const short = d => `${d.toLocaleDateString("tr-TR", { day: "numeric", month: "long" })} · ${formatEventTime(d)}`;
  return `${short(start)} → ${short(end)}`;
}

// UTC instant -> the value an <input type="date" | "time" | "datetime-local"> wants, in the viewer's time zone.
function toLocalInputValue(isoDate, kind) {
  const d = new Date(isoDate);
  const pad = n => String(n).padStart(2, "0");
  const date = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  const time = `${pad(d.getHours())}:${pad(d.getMinutes())}`;
  return kind === "date" ? date : kind === "time" ? time : `${date}T${time}`;
}

// The other direction: what the viewer typed (local, no offset) -> a UTC ISO string for the API.
function localInputToIso(value) {
  return new Date(value).toISOString();
}

// Small pills for an event's state: "Devam ediyor" (started, not finished) and "Özel" (invite-only).
function eventStateBadgeHtml(ev) {
  const ongoing = ev.isOngoing ? `<span class="badge bg-success-subtle text-success-emphasis rounded-pill">Devam ediyor</span>` : "";
  const isPrivate = ev.isPrivate ? `<span class="badge bg-dark-subtle text-dark-emphasis rounded-pill"><i class="bi bi-lock-fill me-1"></i>Özel</span>` : "";
  return `${ongoing} ${isPrivate}`.trim();
}

// "Çevrimiçi" or the place of an event, with its icon.
function eventPlaceHtml(ev) {
  return ev.isOnline
    ? `<i class="bi bi-camera-video me-1"></i>Çevrimiçi`
    : `<i class="bi bi-geo-alt me-1"></i>${escapeHtml(ev.location || "Belirtilmedi")}`;
}

// The main button of an event. "Katılıyorum" -> "Katılıyorsun"; on a full event it becomes "Bekleme listesine katıl"
// -> "Sıradasın (3.)". status is what pressing it asks the server for; pressing it again undoes it.
function eventPrimaryAction(ev) {
  switch (ev.currentUserStatus) {
    case "Going":
      return { label: "Katılıyorsun", css: "btn-primary joined", status: "Going" };
    case "Waitlisted":
      return { label: `Sıradasın (${ev.myWaitlistPosition}.)`, css: "btn-warning", status: "Waitlisted" };
    default:
      return ev.isFull
        ? { label: "Bekleme listesine katıl", css: "btn-outline-primary", status: "Waitlisted" }
        : { label: "Katılıyorum", css: "btn-primary", status: "Going" };
  }
}

// "3 kişi bekliyor" for a full event with a waiting list, otherwise nothing.
function eventWaitlistText(ev) {
  return ev.waitlistCount > 0 ? `${ev.waitlistCount} kişi bekliyor` : "";
}

// "12 katılımcı", or "12 / 50 katılımcı" when the event has a limit.
function eventAttendanceText(ev) {
  return ev.capacity != null ? `${ev.goingCount} / ${ev.capacity} katılımcı` : `${ev.goingCount} katılımcı`;
}

// Post card media block: an <img> for photo posts, a <video> for video posts.
function postMediaHtml(post) {
  if (!post.mediaUrl) return "";
  if (post.mediaType === "Video") {
    return `<div class="rounded-3 overflow-hidden mb-2"><video src="${post.mediaUrl}" class="w-100" style="max-height:420px;object-fit:cover;" controls></video></div>`;
  }
  return `<div class="rounded-3 overflow-hidden mb-2"><img src="${post.mediaUrl}" class="w-100" style="max-height:420px;object-fit:cover;" alt=""></div>`;
}
