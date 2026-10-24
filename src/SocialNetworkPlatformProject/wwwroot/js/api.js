const SESSION_KEY = "socialnet-session";

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

function updateSessionAvatar(avatarUrl) {
  const session = getSession();
  if (!session) return;
  session.avatarUrl = avatarUrl;
  saveSession(session);
}

function requireAuth() {
  const session = getSession();
  if (!session) {
    window.location.href = "index.html";
    return null;
  }

  ensureMediaSession(session);
  return session;
}

function ensureMediaSession(session) {
  const until = session.mediaExpiresAt ? parseServerDate(session.mediaExpiresAt).getTime() : 0;
  if (until - Date.now() > 10 * 60 * 1000) return;

  apiFetch("/api/auth/media-session", { method: "POST" })
    .then(result => {
      const current = getSession();
      if (current && result) {
        current.mediaExpiresAt = result.expiresAt;
        saveSession(current);
      }
    })
    .catch(() => {});
}

async function logout() {
  const session = getSession();
  clearSession();
  if (session?.refreshToken) {
    fetch("/api/auth/logout", {
      method: "POST",
      keepalive: true,
      headers: {
        "Content-Type": "application/json",
        ...(session.accessToken ? { Authorization: `Bearer ${session.accessToken}` } : {})
      },
      body: JSON.stringify({ refreshToken: session.refreshToken })
    }).catch(() => {});
  }
  window.location.href = "index.html";
}

let refreshPromise = null;

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
    throw new Error(i18n("session.expired"));
  }

  if (!refreshPromise) {
    refreshPromise = fetch("/api/auth/refresh", {
      method: "POST",
      headers: { "Content-Type": "application/json", "Accept-Language": getLang() },
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
    const current = getSession();
    if (current && current.refreshToken && current.refreshToken !== session.refreshToken) {
      return current.accessToken;
    }

    clearSession();
    window.location.href = "index.html";
    throw new Error(i18n("session.expired"));
  }
}

const REQUEST_TIMEOUT_MS = 30000;
const UPLOAD_TIMEOUT_MS = 120000;

async function timedFetch(path, init) {
  const controller = new AbortController();
  const limit = init.body instanceof FormData ? UPLOAD_TIMEOUT_MS : REQUEST_TIMEOUT_MS;
  const timer = setTimeout(() => controller.abort(), limit);

  try {
    return await fetch(path, {
      ...init,
      headers: { ...(init.headers || {}), "Accept-Language": getLang() },
      signal: controller.signal
    });
  } catch (err) {
    if (err.name === "AbortError") throw new Error(i18n("net.timeout"));
    throw new Error(i18n("net.offline"));
  } finally {
    clearTimeout(timer);
  }
}

async function fetchWithToken(path, init) {
  const send = token => timedFetch(path, {
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

async function apiFetch(path, options = {}) {
  const { response, hadToken } = await fetchWithToken(path, {
    ...options,
    headers: { "Content-Type": "application/json", ...(options.headers || {}) },
    body: options.body ? JSON.stringify(options.body) : undefined
  });

  return handleResponse(response, hadToken);
}

async function downloadApiFile(path, fallbackName) {
  const { response, hadToken } = await fetchWithToken(path, {});
  if (!response.ok) await handleResponse(response, hadToken);

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

async function apiFetchForm(path, options = {}) {
  const { response, hadToken } = await fetchWithToken(path, options);
  return handleResponse(response, hadToken);
}

async function apiFetchAnonymous(path, options = {}) {
  const response = await timedFetch(path, {
    ...options,
    headers: { "Content-Type": "application/json" },
    body: options.body ? JSON.stringify(options.body) : undefined
  });

  return handleResponse(response, false);
}

async function handleResponse(response, hadTokenAttached) {
  if (response.status === 401 && hadTokenAttached) {
    clearSession();
    window.location.href = "index.html";
    throw new Error(i18n("session.expired"));
  }

  if (response.status === 204) return null;

  const isJson = (response.headers.get("content-type") || "").includes("json");
  const data = isJson ? await response.json() : null;

  if (!response.ok) {
    throw new Error(extractErrorMessage(data));
  }

  return data;
}

function extractErrorMessage(data) {
  if (!data) return i18n("error.generic");
  if (data.message) return data.message;
  if (data.errors) {
    return Object.values(data.errors).flat().join(" ");
  }
  return data.title || i18n("error.generic");
}


function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text == null ? "" : text;
  return div.innerHTML.replace(/"/g, "&quot;").replace(/'/g, "&#39;");
}

function linkifyHashtags(escapedText) {
  return escapedText.replace(/(^|[\s(])#([\p{L}0-9_]+)/gu,
    (match, before, tag) => `${before}<a href="search.html?q=${encodeURIComponent("#" + tag)}" class="text-primary text-decoration-none">#${tag}</a>`);
}

function linkifyMentions(escapedText, mentions) {
  let html = escapedText;
  for (const person of mentions || []) {
    const name = person.userName.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const pattern = new RegExp(`(^|[^\\p{L}0-9_.@])@${name}(?![a-zA-Z0-9_]|\\.[a-zA-Z0-9_])`, "giu");
    html = html.replace(pattern,
      (match, before) => `${before}<a href="profile.html?id=${person.userId}" class="text-primary text-decoration-none fw-semibold">@${person.userName}</a>`);
  }
  return html;
}

const REACTIONS = {
  Like: { emoji: "👍", label: "Beğen" },
  Love: { emoji: "❤️", label: "Sevdim" },
  Haha: { emoji: "😂", label: "Haha" },
  Wow: { emoji: "😮", label: "Vay" },
  Sad: { emoji: "😢", label: "Üzgün" },
  Angry: { emoji: "😡", label: "Kızgın" }
};

function richTextHtml(text, mentions) {
  return `<span data-no-i18n>${linkifyHashtags(linkifyMentions(escapeHtml(text), mentions))}</span>`;
}

function parseServerDate(isoDate) {
  return new Date(/Z$|[+-]\d\d:\d\d$/.test(isoDate) ? isoDate : `${isoDate}Z`);
}

function timeAgo(isoDate) {
  const seconds = Math.floor((Date.now() - parseServerDate(isoDate).getTime()) / 1000);
  if (seconds < 60) return "Şimdi";
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes} dakika önce`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} saat önce`;
  const days = Math.floor(hours / 24);
  return `${days} gün önce`;
}

const DEFAULT_AVATAR = "/images/default-avatar.png";

const NO_PHOTO = "/images/no-photo.svg";

function starsHtml(value) {
  const rounded = Math.round((value || 0) * 2) / 2;
  let stars = "";
  for (let i = 1; i <= 5; i++) {
    const icon = rounded >= i ? "bi-star-fill" : rounded >= i - 0.5 ? "bi-star-half" : "bi-star";
    stars += `<i class="bi ${icon} rating-star"></i>`;
  }
  return `<span class="rating-stars" title="${value} / 5">${stars}</span>`;
}

function formatEventDate(isoDate) {
  const date = new Date(isoDate);
  return date.toLocaleDateString(localeTag(), { day: "numeric", month: "long", weekday: "long" })
    + " · " + date.toLocaleTimeString(localeTag(), { hour: "2-digit", minute: "2-digit" });
}

function formatEventTime(date) {
  return date.toLocaleTimeString(localeTag(), { hour: "2-digit", minute: "2-digit" });
}

function formatEventRange(startsAt, endsAt) {
  const start = new Date(startsAt);
  const end = new Date(endsAt);
  if (start.toDateString() === end.toDateString()) {
    return `${formatEventDate(startsAt)} – ${formatEventTime(end)}`;
  }
  const short = d => `${d.toLocaleDateString(localeTag(), { day: "numeric", month: "long" })} · ${formatEventTime(d)}`;
  return `${short(start)} → ${short(end)}`;
}

function toLocalInputValue(isoDate, kind) {
  const d = new Date(isoDate);
  const pad = n => String(n).padStart(2, "0");
  const date = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  const time = `${pad(d.getHours())}:${pad(d.getMinutes())}`;
  return kind === "date" ? date : kind === "time" ? time : `${date}T${time}`;
}

function localInputToIso(value) {
  return new Date(value).toISOString();
}

function eventDateBlockHtml(startsAt) {
  const d = new Date(startsAt);
  return `<div class="event-date inbox-only" data-no-i18n><b>${d.toLocaleDateString(localeTag(), { month: "short" }).replace(".", "").toUpperCase()}</b><span>${d.getDate()}</span></div>`;
}

function eventStateBadgeHtml(ev) {
  const ongoing = ev.isOngoing ? `<span class="badge bg-success-subtle text-success-emphasis rounded-pill">Devam ediyor</span>` : "";
  const isPrivate = ev.isPrivate ? `<span class="badge bg-dark-subtle text-dark-emphasis rounded-pill"><i class="bi bi-lock-fill me-1"></i>Özel</span>` : "";
  return `${ongoing} ${isPrivate}`.trim();
}

function eventPlaceHtml(ev) {
  return ev.isOnline
    ? `<i class="bi bi-camera-video me-1"></i>Çevrimiçi`
    : `<i class="bi bi-geo-alt me-1"></i>${escapeHtml(ev.location || "Belirtilmedi")}`;
}

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

function eventWaitlistText(ev) {
  return ev.waitlistCount > 0 ? `${ev.waitlistCount} kişi bekliyor` : "";
}

function eventAttendanceText(ev) {
  return ev.capacity != null ? `${ev.goingCount} / ${ev.capacity} katılımcı` : `${ev.goingCount} katılımcı`;
}

let postCarouselCount = 0;

const photoSets = {};

function registerPhotoSet(key, urls) {
  photoSets[key] = urls;
  return key;
}

function openPhotoViewer(key, startIndex = 0) {
  const urls = photoSets[key] || [];
  if (urls.length === 0) return;

  let index = Math.min(Math.max(startIndex, 0), urls.length - 1);
  const overlay = document.createElement("div");
  overlay.className = "photo-viewer";
  overlay.setAttribute("role", "dialog");
  overlay.innerHTML = `
    <button type="button" class="photo-viewer-btn photo-viewer-close" aria-label="Kapat"><i class="bi bi-x-lg"></i></button>
    ${urls.length > 1 ? `
      <button type="button" class="photo-viewer-btn photo-viewer-prev" aria-label="Önceki"><i class="bi bi-chevron-left"></i></button>
      <button type="button" class="photo-viewer-btn photo-viewer-next" aria-label="Sonraki"><i class="bi bi-chevron-right"></i></button>
      <span class="photo-viewer-count"></span>` : ""}
    <img class="photo-viewer-image" alt="">
    <video class="photo-viewer-image photo-viewer-video d-none" controls playsinline></video>`;

  const image = overlay.querySelector("img.photo-viewer-image");
  const video = overlay.querySelector(".photo-viewer-video");
  const counter = overlay.querySelector(".photo-viewer-count");
  const stopVideo = () => { video.pause(); video.removeAttribute("src"); };
  const show = next => {
    index = (next + urls.length) % urls.length;

    const item = urls[index];
    const isVideo = typeof item === "object" && item.type === "Video";
    const src = typeof item === "object" ? item.url : item;
    image.classList.toggle("d-none", isVideo);
    video.classList.toggle("d-none", !isVideo);
    if (isVideo) {
      image.removeAttribute("src");
      video.src = src;
      video.play().catch(() => {});
    } else {
      stopVideo();
      image.src = src;
    }
    if (counter) counter.textContent = `${index + 1} / ${urls.length}`;
  };

  const previousOverflow = document.body.style.overflow;
  const close = () => {
    stopVideo();
    overlay.remove();
    document.body.style.overflow = previousOverflow;
    document.removeEventListener("keydown", onKey);
  };
  const onKey = event => {
    if (event.key === "Escape") close();
    else if (event.target.tagName === "VIDEO") return;
    else if (urls.length > 1 && event.key === "ArrowLeft") show(index - 1);
    else if (urls.length > 1 && event.key === "ArrowRight") show(index + 1);
  };

  overlay.addEventListener("click", event => {
    if (event.target.closest(".photo-viewer-close") || event.target === overlay) close();
    else if (event.target.closest(".photo-viewer-prev")) show(index - 1);
    else if (event.target.closest(".photo-viewer-next")) show(index + 1);
  });

  let touchStartX = null;
  overlay.addEventListener("touchstart", event => { touchStartX = event.touches[0].clientX; }, { passive: true });
  overlay.addEventListener("touchend", event => {
    if (touchStartX === null || urls.length < 2) return;
    const dx = event.changedTouches[0].clientX - touchStartX;
    touchStartX = null;
    if (Math.abs(dx) > 50) show(index + (dx < 0 ? 1 : -1));
  }, { passive: true });

  document.addEventListener("keydown", onKey);
  document.body.style.overflow = "hidden";
  document.body.appendChild(overlay);
  show(index);
}

function postMediaHtml(post) {
  const media = post.media || [];
  if (media.length === 0) return "";

  if (media[0].type === "Video") {
    return `<div class="post-video rounded-3 overflow-hidden mb-2"><video src="${media[0].url}" class="d-block" controls playsinline preload="metadata"></video></div>`;
  }

  const photoKey = registerPhotoSet(`post-${post.id}-${++postCarouselCount}`, media.map(m => m.url));
  if (media.length === 1) {
    return `<div class="rounded-3 overflow-hidden mb-2"><img src="${media[0].url}" class="w-100 photo-zoomable" style="max-height:420px;object-fit:cover;" alt="" onclick="openPhotoViewer('${photoKey}', 0)"></div>`;
  }

  const id = `post-media-${post.id}-${postCarouselCount}`;
  const slides = media.map((m, i) => `
        <div class="carousel-item ${i === 0 ? "active" : ""}"><img src="${m.url}" class="d-block w-100 photo-zoomable" style="height:420px;object-fit:cover;" alt="" onclick="openPhotoViewer('${photoKey}', ${i})"></div>`).join("");
  const dots = media.map((_, i) => `
        <button type="button" data-bs-target="#${id}" data-bs-slide-to="${i}" class="${i === 0 ? "active" : ""}" aria-label="${i + 1}. fotoğraf"></button>`).join("");

  return `
    <div id="${id}" class="carousel slide rounded-3 overflow-hidden mb-2 post-carousel" data-bs-interval="false">
      <div class="carousel-indicators">${dots}</div>
      <div class="carousel-inner">${slides}</div>
      <button class="carousel-control-prev" type="button" data-bs-target="#${id}" data-bs-slide="prev" aria-label="Önceki"><span class="carousel-control-prev-icon"></span></button>
      <button class="carousel-control-next" type="button" data-bs-target="#${id}" data-bs-slide="next" aria-label="Sonraki"><span class="carousel-control-next-icon"></span></button>
      <span class="badge text-bg-dark position-absolute top-0 end-0 m-2 opacity-75"><i class="bi bi-images me-1"></i>${media.length}</span>
    </div>`;
}
