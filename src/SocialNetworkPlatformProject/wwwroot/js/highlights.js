// profile.html — story highlights: the round covers under the profile header, the viewer that plays one (like the story viewer of
// home.html: progress bars, tap to go on, a song), and the owner's manage window.
//
// A highlight keeps copies of stories, so it stays after the 24 hours are over. Which stories go in is chosen in the story viewer
// of home.html ("Öne çıkanlara ekle", js/stories.js). Every rule (who sees it, how many, who may change it) is decided by the server.

let highlightCards = [];     // the covers on screen: [{ id, title, itemCount, coverUrl, coverMediaType }]

// ---- The row under the header ----
async function loadHighlights(profile) {
  try {
    highlightCards = await apiFetch(`/api/users/${profile.id}/highlights`);
  } catch (err) {
    highlightCards = []; // a missing row is not worth an error on the profile page
  }
  renderHighlightRow(profile.isOwnProfile);
}

function highlightCoverHtml(card) {
  if (!card.coverUrl) return `<i class="bi bi-collection-play"></i>`;
  // A video cover shows its first picture (the #t=0.1 asks for a frame just after the start).
  return card.coverMediaType === "Video"
    ? `<video src="${escapeHtml(card.coverUrl)}#t=0.1" muted playsinline preload="metadata"></video>`
    : `<img src="${escapeHtml(card.coverUrl)}" alt="">`;
}

function renderHighlightRow(isOwn) {
  const row = document.getElementById("highlightsRow");
  if (highlightCards.length === 0 && !isOwn) {
    row.classList.add("d-none");
    row.innerHTML = "";
    return;
  }

  const tiles = highlightCards.map(card => `
    <div class="highlight-tile">
      <button type="button" class="highlight-open" onclick="openHighlight('${card.id}')" aria-label="${escapeHtml(card.title)}">
        <span class="highlight-cover">${highlightCoverHtml(card)}</span>
        <span class="highlight-title" data-no-i18n>${escapeHtml(card.title)}</span>
      </button>
      ${isOwn ? `<button type="button" class="highlight-edit" onclick="openManageHighlight('${card.id}')" aria-label="Düzenle" title="Düzenle"><i class="bi bi-pencil-fill"></i></button>` : ""}
    </div>`).join("");

  // The owner's "new" tile explains how a highlight is made: it starts from a running story.
  const addTile = isOwn ? `
    <div class="highlight-tile">
      <button type="button" class="highlight-open" onclick="explainNewHighlight()">
        <span class="highlight-cover highlight-cover-new"><i class="bi bi-plus-lg"></i></span>
        <span class="highlight-title">Yeni</span>
      </button>
    </div>` : "";

  row.innerHTML = tiles + addTile;
  row.classList.remove("d-none");
}

function explainNewHighlight() {
  openSiteDialog(`
    <div class="fw-bold mb-2">Öne çıkan ekle</div>
    <div class="mb-4">Bir hikâye paylaş, hikâyeni aç ve üstteki "Öne çıkanlara ekle" düğmesine dokun. Öne çıkanlar 24 saat sonra kaybolmaz; profilinde kalır.</div>
    <div class="d-flex justify-content-end flex-wrap gap-2">
      <button type="button" class="btn btn-light rounded-pill px-4" data-answer="close">Kapat</button>
      <a href="home.html" class="btn btn-primary rounded-pill px-4">Hikâyelere Git</a>
    </div>`,
    (box, close) => {
      box.querySelector('[data-answer="close"]').addEventListener("click", close);
      box.addEventListener("dismiss", close);
    });
}

// ---- The viewer ----
const HL_IMAGE_MS = 4000;       // how long a picture stays (a video stays as long as it is, up to HL_VIDEO_MAX_MS)
const HL_VIDEO_MAX_MS = 30000;
const HL_HOLD_MS = 250;

const hl = {
  detail: null, index: 0, timer: null, loadToken: 0, loading: false, paused: false,
  muted: false, holdTimer: null, held: false, holdEndedAt: 0
};

function ensureHighlightViewer() {
  if (document.getElementById("hlViewer")) return;

  document.body.insertAdjacentHTML("beforeend", `
    <div class="story-viewer d-none" id="hlViewer" role="dialog" aria-modal="true">
      <div class="story-stage">
        <button type="button" class="story-arrow" id="hlArrowPrev" aria-label="Önceki hikâye" onclick="hlStep(-1)"><i class="bi bi-chevron-left"></i></button>
        <div class="story-viewer-inner" id="hlInner">
          <div class="story-viewer-bars" id="hlBars"></div>
          <div class="story-viewer-header">
            <img id="hlAvatar" class="avatar-xs" src="" alt="">
            <div class="d-flex flex-column lh-sm" style="min-width:0">
              <span id="hlTitle" class="fw-bold text-white small text-truncate" data-no-i18n></span>
              <span id="hlAge" class="small text-white-50"></span>
            </div>
            <div class="ms-auto d-flex align-items-center gap-2">
              <button type="button" id="hlEditBtn" class="btn btn-sm btn-dark rounded-circle d-none" title="Düzenle" aria-label="Düzenle" onclick="editFromViewer()"><i class="bi bi-pencil-fill"></i></button>
              <button type="button" class="btn-close btn-close-white" aria-label="Kapat" onclick="closeHighlightViewer()"></button>
            </div>
          </div>
          <img id="hlImage" class="story-viewer-image" src="" alt="">
          <video id="hlVideo" class="story-viewer-image d-none" muted playsinline></video>
          <audio id="hlMusic" preload="auto" loop></audio>
          <div class="story-info-stack">
            <div class="story-music-chip d-none" id="hlMusicChip">
              <img id="hlMusicArt" class="story-music-art d-none" src="" alt="">
              <i class="bi bi-music-note-beamed" id="hlMusicIcon"></i>
              <span class="story-music-text" id="hlMusicText" data-no-i18n></span>
              <button type="button" class="story-music-mute" id="hlMusicMute" onclick="toggleHighlightMute()"></button>
            </div>
          </div>
          <div class="story-loading d-none" id="hlLoading"><div class="spinner-border text-light" role="status" aria-label="Yükleniyor"></div></div>
          <div class="story-viewer-nav" id="hlNav">
            <div class="story-nav-zone" onclick="hlStep(-1)"></div>
            <div class="story-nav-zone" onclick="hlStep(1)"></div>
          </div>
        </div>
        <button type="button" class="story-arrow" id="hlArrowNext" aria-label="Sonraki hikâye" onclick="hlStep(1)"><i class="bi bi-chevron-right"></i></button>
      </div>
    </div>`);

  // Holding a finger or the mouse button on the story stops it, like in the story viewer; the click that ends a hold is not a tap.
  const nav = document.getElementById("hlNav");
  nav.addEventListener("pointerdown", event => {
    if (event.button > 0) return;
    clearTimeout(hl.holdTimer);
    hl.holdTimer = setTimeout(() => {
      if (hl.paused || hl.loading) return;
      hl.held = true;
      pauseHighlight();
    }, HL_HOLD_MS);
  });
  const endHold = () => {
    clearTimeout(hl.holdTimer);
    if (!hl.held) return;
    hl.held = false;
    hl.holdEndedAt = Date.now();
    resumeHighlight();
  };
  nav.addEventListener("pointerup", endHold);
  nav.addEventListener("pointercancel", endHold);
  nav.addEventListener("pointerleave", endHold);
  nav.addEventListener("contextmenu", event => event.preventDefault());

  document.addEventListener("keydown", event => {
    if (document.getElementById("hlViewer").classList.contains("d-none")) return;
    if (event.key === "ArrowRight") hlStep(1);
    else if (event.key === "ArrowLeft") hlStep(-1);
    else if (event.key === "Escape") closeHighlightViewer();
  });
}

async function openHighlight(id) {
  let detail;
  try {
    detail = await apiFetch(`/api/highlights/${id}`);
  } catch (err) {
    toast(err.message || "Öne çıkan yüklenemedi.");
    return;
  }
  if (detail.items.length === 0) return;

  ensureHighlightViewer();
  hl.detail = detail;
  document.getElementById("hlAvatar").src = detail.ownerAvatarUrl || DEFAULT_AVATAR;
  document.getElementById("hlTitle").textContent = detail.title;
  document.getElementById("hlEditBtn").classList.toggle("d-none", !detail.isOwn);
  document.getElementById("hlBars").innerHTML = detail.items.map((_, i) => `<div class="bar" id="hlBar-${i}"><div class="fill"></div></div>`).join("");

  document.getElementById("hlViewer").classList.remove("d-none");
  document.body.style.overflow = "hidden";
  showHighlightItem(0);
}

function closeHighlightViewer() {
  clearTimeout(hl.timer);
  hl.loadToken++; // a picture still on its way is no longer wanted
  hl.loading = false;
  document.getElementById("hlViewer")?.classList.add("d-none");
  document.getElementById("hlVideo")?.pause();
  stopHighlightMusic();
  document.body.style.overflow = "";
}

// A tap or an arrow: one story on (-1 = back). Past the last one the viewer closes; before the first one the first starts again.
function hlStep(delta) {
  if (Date.now() - hl.holdEndedAt < 400) {   // the click that ends a hold
    hl.holdEndedAt = 0;
    return;
  }
  showHighlightItem(hl.index + delta);
}

function showHighlightItem(index) {
  const items = hl.detail.items;
  if (index >= items.length) {
    closeHighlightViewer();
    return;
  }
  index = Math.max(index, 0);

  hl.index = index;
  hl.paused = false;
  const item = items[index];

  items.forEach((_, i) => {
    const bar = document.getElementById("hlBar-" + i);
    const fill = bar.querySelector(".fill");
    fill.style.transition = "none";
    fill.style.width = i < index ? "100%" : "0%";
  });

  document.getElementById("hlAge").textContent = timeAgo(item.createdAt);
  document.getElementById("hlArrowPrev").classList.toggle("invisible", index === 0);
  document.getElementById("hlArrowNext").classList.toggle("invisible", index === items.length - 1);

  loadHighlightMedia(item);
  prepareHighlightMusic(item);
}

// The picture or video goes on screen first; the bar starts when it is really there (a slow connection must not eat the story).
function loadHighlightMedia(item) {
  const img = document.getElementById("hlImage");
  const video = document.getElementById("hlVideo");
  const spinner = document.getElementById("hlLoading");
  const isVideo = item.mediaType === "Video";
  const token = ++hl.loadToken;

  clearTimeout(hl.timer);
  hl.loading = true;
  spinner.classList.remove("d-none");
  img.classList.add("story-media-hidden");
  video.classList.add("story-media-hidden");
  img.classList.toggle("d-none", isVideo);
  video.classList.toggle("d-none", !isVideo);

  const loaded = () => {
    if (token !== hl.loadToken) return;
    hl.loading = false;
    spinner.classList.add("d-none");
    (isVideo ? video : img).classList.remove("story-media-hidden");
    startHighlightIfReady();
  };
  const failed = () => {
    if (token !== hl.loadToken) return;
    toast("Hikâye yüklenemedi.");
    hl.loading = false;
    spinner.classList.add("d-none");
    startHighlightIfReady();
  };

  if (isVideo) {
    img.onload = img.onerror = null;
    img.removeAttribute("src");
    video.onloadeddata = loaded;
    video.onerror = failed;
    video.src = item.mediaUrl;
    video.currentTime = 0;
  } else {
    video.onloadeddata = video.onerror = null;
    video.pause();
    video.removeAttribute("src");
    img.onload = loaded;
    img.onerror = failed;
    img.removeAttribute("src"); // the previous picture goes at once
    img.src = item.mediaUrl;
  }
}

function currentHighlightItem() {
  return hl.detail.items[hl.index];
}

function startHighlightIfReady() {
  if (hl.loading || hl.paused) return;

  const item = currentHighlightItem();
  const video = document.getElementById("hlVideo");
  let duration = HL_IMAGE_MS;
  if (item.mediaType === "Video") {
    if (Number.isFinite(video.duration) && video.duration > 0) duration = Math.min(video.duration * 1000, HL_VIDEO_MAX_MS);
    video.play().catch(() => {});
  }

  const fill = document.querySelector(`#hlBar-${hl.index} .fill`);
  if (fill) {
    fill.style.transition = "none";
    fill.style.width = "0%";
    void fill.offsetWidth; // applies the empty bar before the transition starts
    fill.style.transition = `width ${duration}ms linear`;
    fill.style.width = "100%";
  }
  clearTimeout(hl.timer);
  hl.timer = setTimeout(() => showHighlightItem(hl.index + 1), duration);
  playHighlightMusic();
}

function pauseHighlight() {
  hl.paused = true;
  clearTimeout(hl.timer);
  document.getElementById("hlMusic").pause();
  document.getElementById("hlVideo").pause();
  const fill = document.querySelector(`#hlBar-${hl.index} .fill`);
  if (fill) {
    fill.style.width = getComputedStyle(fill).width;
    fill.style.transition = "none";
  }
}

// Letting go starts the story's bar over (the same as in the story viewer).
function resumeHighlight() {
  if (document.getElementById("hlViewer").classList.contains("d-none")) return;
  hl.paused = false;
  startHighlightIfReady();
}

// ---- The song ----
function prepareHighlightMusic(item) {
  const audio = document.getElementById("hlMusic");
  if (!item.musicUrl) {
    stopHighlightMusic();
    return;
  }

  // The same song in the next story goes on without a break.
  if (audio.dataset.url !== item.musicUrl) {
    audio.pause();
    audio.src = item.musicUrl;
    audio.dataset.url = item.musicUrl;
    audio.currentTime = 0;
  }
  audio.muted = hl.muted;

  const art = document.getElementById("hlMusicArt");
  art.classList.toggle("d-none", !item.musicArtworkUrl);
  if (item.musicArtworkUrl) art.src = item.musicArtworkUrl;
  document.getElementById("hlMusicIcon").classList.toggle("d-none", !!item.musicArtworkUrl);
  document.getElementById("hlMusicText").textContent = item.musicArtist ? `${item.musicTitle} · ${item.musicArtist}` : item.musicTitle;
  document.getElementById("hlMusicChip").classList.remove("d-none");
  showHighlightMuteState();
}

function playHighlightMusic() {
  const audio = document.getElementById("hlMusic");
  if (!audio.dataset.url || hl.paused) return;
  audio.play().catch(() => { /* the browser wants a tap first: the button on the chip starts it */ });
}

function stopHighlightMusic() {
  const audio = document.getElementById("hlMusic");
  if (!audio) return;
  audio.pause();
  audio.removeAttribute("src");
  delete audio.dataset.url;
  document.getElementById("hlMusicChip").classList.add("d-none");
}

function toggleHighlightMute() {
  const audio = document.getElementById("hlMusic");
  // When the browser held the sound back, the first tap starts it instead of muting it.
  if (audio.paused && !hl.paused && audio.dataset.url) {
    audio.muted = hl.muted = false;
    playHighlightMusic();
  } else {
    hl.muted = !hl.muted;
    audio.muted = hl.muted;
  }
  showHighlightMuteState();
}

function showHighlightMuteState() {
  const button = document.getElementById("hlMusicMute");
  button.innerHTML = `<i class="bi ${hl.muted ? "bi-volume-mute-fill" : "bi-volume-up-fill"}"></i>`;
  const label = i18n(hl.muted ? "story.musicUnmute" : "story.musicMute");
  button.setAttribute("aria-label", label);
  button.title = label;
}

function editFromViewer() {
  const id = hl.detail.id;
  closeHighlightViewer();
  openManageHighlight(id);
}

// ---- The owner's manage window ----
let managed = null;        // the highlight being edited (what the server last sent)
let managedCover = null;   // the item chosen as the cover (null = the first one)

async function openManageHighlight(id) {
  try {
    managed = await apiFetch(`/api/highlights/${id}`);
  } catch (err) {
    toast(err.message || "Öne çıkan yüklenemedi.");
    return;
  }

  managedCover = managed.coverItemId;
  document.getElementById("manageHighlightName").value = managed.title;
  drawManagedItems();
  bootstrap.Modal.getOrCreateInstance(document.getElementById("manageHighlightModal")).show();
}

function drawManagedItems() {
  const selected = managedCover || managed.items[0]?.id;
  document.getElementById("manageHighlightItems").innerHTML = managed.items.map(item => `
    <div class="highlight-thumb ${item.id === selected ? "selected" : ""}">
      <button type="button" class="highlight-thumb-pick" onclick="pickHighlightCover('${item.id}')" aria-label="Kapak yap" aria-pressed="${item.id === selected}">
        ${item.mediaType === "Video"
          ? `<video src="${escapeHtml(item.mediaUrl)}#t=0.1" muted playsinline preload="metadata"></video>`
          : `<img src="${escapeHtml(item.mediaUrl)}" alt="">`}
      </button>
      ${item.id === selected ? `<span class="highlight-thumb-badge">Kapak</span>` : ""}
      <button type="button" class="highlight-thumb-remove" onclick="removeHighlightItem('${item.id}')" aria-label="Hikâyeyi kaldır" title="Hikâyeyi kaldır"><i class="bi bi-x-lg"></i></button>
    </div>`).join("");
}

function pickHighlightCover(itemId) {
  managedCover = itemId;
  drawManagedItems();
}

async function removeHighlightItem(itemId) {
  if (!(await confirmDialog("Bu hikâye öne çıkandan kaldırılsın mı?", { confirmText: "Kaldır" }))) return;

  const wasLast = managed.items.length === 1;
  try {
    await apiFetch(`/api/highlights/${managed.id}/items/${itemId}`, { method: "DELETE" });
  } catch (err) {
    toast(err.message || "Hikâye kaldırılamadı.");
    return;
  }

  // Taking out the last story deletes the highlight (the server does it): nothing left to edit.
  if (wasLast) {
    bootstrap.Modal.getInstance(document.getElementById("manageHighlightModal"))?.hide();
    toast("Öne çıkan silindi.");
    await loadHighlights(currentProfile);
    return;
  }

  if (managedCover === itemId) managedCover = null;
  managed = await apiFetch(`/api/highlights/${managed.id}`);
  drawManagedItems();
  loadHighlights(currentProfile);
}

async function saveManagedHighlight() {
  const title = document.getElementById("manageHighlightName").value.trim();
  if (!title) {
    toast("Öne çıkana bir ad ver.");
    return;
  }

  const button = document.getElementById("manageHighlightSave");
  button.disabled = true;
  try {
    await apiFetch(`/api/highlights/${managed.id}`, { method: "PUT", body: { title, coverItemId: managedCover } });
    bootstrap.Modal.getInstance(document.getElementById("manageHighlightModal"))?.hide();
    toast("Öne çıkan güncellendi.");
    await loadHighlights(currentProfile);
  } catch (err) {
    toast(err.message || "Öne çıkan güncellenemedi.");
  } finally {
    button.disabled = false;
  }
}

async function deleteManagedHighlight() {
  if (!(await confirmDialog("Bu öne çıkan silinsin mi? İçindeki hikâyeler de silinir.", { confirmText: "Sil" }))) return;

  try {
    await apiFetch(`/api/highlights/${managed.id}`, { method: "DELETE" });
  } catch (err) {
    toast(err.message || "Öne çıkan silinemedi.");
    return;
  }

  bootstrap.Modal.getInstance(document.getElementById("manageHighlightModal"))?.hide();
  toast("Öne çıkan silindi.");
  await loadHighlights(currentProfile);
}
