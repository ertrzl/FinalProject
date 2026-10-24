let storyGroups = [];
const STORY_DURATION = 4000;
let currentGroup = 0;
let currentStory = 0;
let renderedGroup = -1;
let storyTimer = null;
let storyLoading = false;
let storyPaused = false;
let storyLoadToken = 0;

function groupStories(stories, myId) {
  const byUser = new Map();
  stories.forEach(story => {
    if (!byUser.has(story.userId)) {
      byUser.set(story.userId, { userId: story.userId, userName: story.userName, userAvatarUrl: story.userAvatarUrl, stories: [] });
    }
    byUser.get(story.userId).stories.push(story);
  });

  const groups = [...byUser.values()];
  groups.forEach(group => group.stories.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt)));

  const rank = group => group.userId === myId ? 0 : group.stories.some(s => !s.isViewed) ? 1 : 2;
  const newest = group => new Date(group.stories[group.stories.length - 1].createdAt);
  return groups.sort((a, b) => rank(a) - rank(b) || newest(b) - newest(a));
}

function firstUnviewedIndex(group) {
  const index = group.stories.findIndex(s => !s.isViewed);
  return index === -1 ? 0 : index;
}

function storyCardHtml(group, index) {
  const cover = group.stories[firstUnviewedIndex(group)];
  const seen = group.stories.every(s => s.isViewed);
  const thumb = cover.mediaType === "Video"
    ? `<video src="${cover.mediaUrl}" muted></video>`
    : `<img src="${cover.mediaUrl}" alt="">`;

  return `
    <div class="story-card ${seen ? "story-seen" : ""}" onclick="openStory(${index})">
      <span class="story-ring"><img src="${group.userAvatarUrl || DEFAULT_AVATAR}" alt=""></span>
      ${thumb}
      <span class="story-name">${escapeHtml(group.userName)}</span>
    </div>`;
}

async function loadStories() {
  const bar = document.getElementById("storyBarDynamic");
  try {
    const stories = await apiFetch("/api/stories");
    storyGroups = groupStories(stories, getSession().userId);
    bar.innerHTML = storyGroups.map(storyCardHtml).join("");
  } catch (err) {
    storyGroups = [];
    bar.innerHTML = "";
  }
}

const currentGroupItem = () => storyGroups[currentGroup];
const currentStoryItem = () => storyGroups[currentGroup].stories[currentStory];

function renderStoryBars() {
  const bars = document.getElementById("storyBars");
  bars.innerHTML = currentGroupItem().stories.map((_, i) => `<div class="bar" id="storyBar-${i}"><div class="fill"></div></div>`).join("");
  renderedGroup = currentGroup;
}

function markBarsDone(upToIndex) {
  currentGroupItem().stories.forEach((_, i) => {
    const bar = document.getElementById("storyBar-" + i);
    if (!bar) return;

    const fill = bar.querySelector(".fill");
    if (i < upToIndex) {
      bar.classList.add("done");
      fill.style.transition = "none";
      fill.style.width = "100%";
    } else if (i > upToIndex) {
      bar.classList.remove("done");
      fill.style.transition = "none";
      fill.style.width = "0%";
    }
  });
}

function storyAgeText(createdAt) {
  const minutes = Math.max(0, Math.floor((Date.now() - parseServerDate(createdAt).getTime()) / 60000));
  if (minutes < 1) return i18n("story.age.now");
  if (minutes < 60) return i18n("story.age.minutes", minutes);
  return i18n("story.age.hours", Math.floor(minutes / 60));
}

function showStory(groupIndex, storyIndex) {
  if (storyIndex < 0) {
    if (groupIndex > 0) showStory(groupIndex - 1, 0);
    return;
  }
  if (storyIndex >= storyGroups[groupIndex].stories.length) {
    if (groupIndex + 1 < storyGroups.length) showStory(groupIndex + 1, firstUnviewedIndex(storyGroups[groupIndex + 1]));
    else closeStory();
    return;
  }

  const slideDirection = renderedGroup !== -1 && renderedGroup !== groupIndex ? (groupIndex > renderedGroup ? 1 : -1) : 0;
  currentGroup = groupIndex;
  currentStory = storyIndex;
  storyLoading = true;
  storyPaused = false;
  if (renderedGroup !== currentGroup) {
    renderStoryBars();
    storyGroups[currentGroup].stories.forEach(preloadStory);
  }
  renderStorySides();
  if (slideDirection !== 0) slideStoryIn(slideDirection);

  const group = currentGroupItem();
  const story = currentStoryItem();
  document.getElementById("storyViewerAvatar").src = group.userAvatarUrl || DEFAULT_AVATAR;
  const storyViewerName = document.getElementById("storyViewerName");
  storyViewerName.textContent = group.userName;
  storyViewerName.href = `profile.html?id=${group.userId}`;
  document.getElementById("storyViewerAge").textContent = storyAgeText(story.createdAt);

  const isMine = story.userId === getSession().userId;
  document.getElementById("storyViewersBtn").classList.toggle("d-none", !isMine);
  document.getElementById("storyDeleteBtn").classList.toggle("d-none", !isMine);
  document.getElementById("storyHighlightBtn").classList.toggle("d-none", !isMine);
  document.getElementById("storyViewCount").textContent = story.viewCount;
  document.getElementById("storyLikeCount").textContent = story.likeCount;
  document.getElementById("storyReplyBar").classList.toggle("d-none", isMine);
  showStoryLikeState(story);
  resetStoryReply();
  hideStoryViewersPanel();
  if (!isMine && !story.isViewed) reportStoryView(story);

  markBarsDone(storyIndex);
  const fill = document.querySelector(`#storyBar-${storyIndex} .fill`);
  if (fill) {
    fill.style.transition = "none";
    fill.style.width = "0%";
  }

  loadStoryMedia(story);
  prepareStoryMusic(story);
  showStoryTags(story);
  preloadStory(nextStoryItem());
}

function loadStoryMedia(story) {
  const img = document.getElementById("storyViewerImage");
  const video = document.getElementById("storyViewerVideo");
  const spinner = document.getElementById("storyLoading");
  const isVideo = story.mediaType === "Video";
  const token = ++storyLoadToken;

  clearTimeout(storyTimer);
  storyLoading = true;
  spinner.classList.remove("d-none");
  img.classList.add("story-media-hidden");
  video.classList.add("story-media-hidden");
  img.classList.toggle("d-none", isVideo);
  video.classList.toggle("d-none", !isVideo);

  const loaded = () => {
    if (token !== storyLoadToken) return;
    storyLoading = false;
    spinner.classList.add("d-none");
    (isVideo ? video : img).classList.remove("story-media-hidden");
    startStoryIfReady();
  };
  const failed = () => {
    if (token !== storyLoadToken) return;
    toast("Hikâye yüklenemedi.");
    storyLoading = false;
    spinner.classList.add("d-none");
    startStoryIfReady();
  };

  if (isVideo) {
    img.onload = img.onerror = null;
    img.removeAttribute("src");
    video.onloadeddata = loaded;
    video.onerror = failed;
    video.src = story.mediaUrl;
    video.currentTime = 0;
  } else {
    video.onloadeddata = video.onerror = null;
    video.pause();
    video.removeAttribute("src");
    img.onload = loaded;
    img.onerror = failed;
    img.removeAttribute("src");
    img.src = story.mediaUrl;
  }
}

function startStoryIfReady() {
  if (storyLoading || storyPaused) return;
  runProgress(currentStory);
  playStoryMusic();
  if (currentStoryItem().mediaType === "Video") document.getElementById("storyViewerVideo").play().catch(() => {});
}

function nextStoryItem() {
  const next = currentGroupItem().stories[currentStory + 1];
  if (next) return next;
  const nextGroup = storyGroups[currentGroup + 1];
  return nextGroup ? nextGroup.stories[firstUnviewedIndex(nextGroup)] : null;
}

function preloadStory(story) {
  if (story && story.mediaType !== "Video") new Image().src = story.mediaUrl;
}

function runProgress(index) {
  clearTimeout(storyTimer);
  const bar = document.getElementById("storyBar-" + index);
  const fill = bar ? bar.querySelector(".fill") : null;
  if (fill) {
    fill.style.transition = "none";
    fill.style.width = "0%";
    void fill.offsetWidth;
    fill.style.transition = "width " + STORY_DURATION + "ms linear";
    fill.style.width = "100%";
  }
  storyTimer = setTimeout(() => nextStory(), STORY_DURATION);
}

function pauseStory() {
  storyPaused = true;
  clearTimeout(storyTimer);
  document.getElementById("storyMusic").pause();
  document.getElementById("storyViewerVideo").pause();
  const fill = document.querySelector(`#storyBar-${currentStory} .fill`);
  if (fill) {
    fill.style.width = getComputedStyle(fill).width;
    fill.style.transition = "none";
  }
}

function resumeStory() {
  if (document.getElementById("storyViewer").classList.contains("d-none")) return;
  storyPaused = false;
  startStoryIfReady();
}

function openStory(groupIndex) {
  renderedGroup = -1;
  document.getElementById("storyViewer").classList.remove("d-none");
  document.body.style.overflow = "hidden";
  showStory(groupIndex, firstUnviewedIndex(storyGroups[groupIndex]));
}

function storySideHtml(group) {
  const cover = group.stories[firstUnviewedIndex(group)];
  const media = cover.mediaType === "Video"
    ? `<video src="${cover.mediaUrl}" muted preload="metadata"></video>`
    : `<img src="${cover.mediaUrl}" alt="">`;
  return `${media}
    <div class="story-side-who">
      <span class="story-side-name" data-no-i18n>${escapeHtml(group.userName)}</span>
      <span class="story-side-age">${storyAgeText(cover.createdAt)}</span>
    </div>`;
}

function renderStorySides() {
  [["storySidePrev", storyGroups[currentGroup - 1]], ["storySideNext", storyGroups[currentGroup + 1]]].forEach(([id, group]) => {
    const side = document.getElementById(id);
    side.classList.toggle("empty", !group);
    side.innerHTML = group ? storySideHtml(group) : "";
  });

  const isFirst = currentGroup === 0 && currentStory === 0;
  const isLast = currentGroup === storyGroups.length - 1 && currentStory === currentGroupItem().stories.length - 1;
  document.getElementById("storyArrowPrev").classList.toggle("invisible", isFirst);
  document.getElementById("storyArrowNext").classList.toggle("invisible", isLast);
}

function goStoryGroup(delta) {
  const target = currentGroup + delta;
  if (!storyGroups[target]) return;
  showStory(target, firstUnviewedIndex(storyGroups[target]));
}

function slideStoryIn(direction) {
  const inner = document.getElementById("storyViewerInner");
  inner.classList.remove("slide-from-right", "slide-from-left");
  void inner.offsetWidth;
  inner.classList.add(direction > 0 ? "slide-from-right" : "slide-from-left");
}

document.addEventListener("keydown", event => {
  if (document.getElementById("storyViewer").classList.contains("d-none")) return;
  if (event.target.matches?.("input, textarea")) return;
  if (event.key === "ArrowRight") nextStory();
  else if (event.key === "ArrowLeft") prevStory();
  else if (event.key === "Escape") closeStory();
});

function nextStory() {
  if (consumeHoldClick()) return;
  showStory(currentGroup, currentStory + 1);
}

function prevStory() {
  if (consumeHoldClick()) return;
  showStory(currentGroup, currentStory - 1);
}

const STORY_HOLD_MS = 250;
let storyHoldTimer = null;
let storyHeld = false;
let storyHoldEndedAt = 0;

function startStoryHold(event) {
  if (event.button > 0) return;
  clearTimeout(storyHoldTimer);
  storyHoldTimer = setTimeout(() => {
    if (storyPaused || storyLoading) return;
    storyHeld = true;
    pauseStory();
  }, STORY_HOLD_MS);
}

function endStoryHold() {
  clearTimeout(storyHoldTimer);
  if (!storyHeld) return;
  storyHeld = false;
  storyHoldEndedAt = Date.now();
  resumeStory();
}

function consumeHoldClick() {
  if (Date.now() - storyHoldEndedAt < 400) {
    storyHoldEndedAt = 0;
    return true;
  }
  return false;
}

(function setUpStoryHold() {
  const nav = document.querySelector(".story-viewer-nav");
  if (!nav) return;
  nav.addEventListener("pointerdown", startStoryHold);
  nav.addEventListener("pointerup", endStoryHold);
  nav.addEventListener("pointercancel", endStoryHold);
  nav.addEventListener("pointerleave", endStoryHold);
  nav.addEventListener("contextmenu", event => event.preventDefault());
})();

function closeStory() {
  clearTimeout(storyTimer);
  storyLoadToken++;
  storyLoading = false;
  document.getElementById("storyViewer").classList.add("d-none");
  document.getElementById("storyViewerVideo").pause();
  stopStoryMusic();
  document.body.style.overflow = "";
  hideStoryViewersPanel();
  resetStoryReply();

  loadStories();
}

function reportStoryView(story) {
  story.isViewed = true;
  apiFetch(`/api/stories/${story.id}/view`, { method: "POST" }).catch(() => {});
}

function showStoryLikeState(story) {
  const button = document.getElementById("storyLikeBtn");
  button.classList.toggle("liked", story.isLiked);
  button.querySelector("i").className = story.isLiked ? "bi bi-heart-fill" : "bi bi-heart";
}

async function toggleStoryLike() {
  const story = currentStoryItem();
  try {
    const result = await apiFetch(`/api/stories/${story.id}/like`, { method: "POST" });
    story.isLiked = result.isLiked;
    showStoryLikeState(story);
  } catch (err) {
    toast(err.message || "Beğenilemedi.");
  }
}

function resetStoryReply() {
  const input = document.getElementById("storyReplyInput");
  input.value = "";
  input.blur();
  updateStoryReplyButton();
}

function updateStoryReplyButton() {
  const hasText = document.getElementById("storyReplyInput").value.trim() !== "";
  document.getElementById("storyReplySend").classList.toggle("d-none", !hasText);
}

async function sendStoryReply(event) {
  event.preventDefault();
  const input = document.getElementById("storyReplyInput");
  const text = input.value.trim();
  if (!text) return false;

  try {
    const story = currentStoryItem();
    await apiFetch("/api/messages", { method: "POST", body: { receiverId: story.userId, storyId: story.id, text } });
    resetStoryReply();
    toast("Mesajın gönderildi.");
  } catch (err) {
    toast(err.message || "Mesaj gönderilemedi.");
  }
  return false;
}

function hideStoryViewersPanel() {
  document.getElementById("storyViewersPanel").classList.add("d-none");
}

async function openStoryViewers() {
  const story = currentStoryItem();
  const list = document.getElementById("storyViewersList");

  pauseStory();

  list.innerHTML = `<div class="text-center small py-3 text-white-50">Yükleniyor...</div>`;
  document.getElementById("storyViewersPanel").classList.remove("d-none");

  try {
    const viewers = await apiFetch(`/api/stories/${story.id}/viewers`);
    story.viewCount = viewers.length;
    document.getElementById("storyViewCount").textContent = viewers.length;
    list.innerHTML = viewers.length
      ? viewers.map(v => `
          <a href="profile.html?id=${v.userId}" class="story-viewer-row text-decoration-none">
            <img src="${v.avatarUrl || DEFAULT_AVATAR}" class="avatar-xs" alt="">
            <span class="flex-grow-1 fw-semibold small">${escapeHtml(v.fullName)}</span>
            ${v.hasLiked ? `<i class="bi bi-heart-fill text-danger"></i>` : ""}
            <span class="small">${timeAgo(v.viewedAt)}</span>
          </a>`).join("")
      : `<div class="text-center small py-3 text-white-50">Henüz kimse izlemedi.</div>`;
  } catch (err) {
    list.innerHTML = `<div class="text-center small py-3 text-danger">${escapeHtml(err.message)}</div>`;
  }
}

function closeStoryViewers() {
  hideStoryViewersPanel();
  resumeStory();
}

async function deleteCurrentStory() {
  const story = currentStoryItem();
  if (!(await confirmDialog("Bu hikâye silinsin mi?"))) return;

  try {
    await apiFetch(`/api/stories/${story.id}`, { method: "DELETE" });
  } catch (err) {
    toast(err.message || "Hikâye silinemedi.");
    return;
  }

  closeStory();
  toast("Hikâyen silindi.");
}

(function addHighlightButton() {
  document.getElementById("storyDeleteBtn")?.insertAdjacentHTML("beforebegin",
    `<button type="button" id="storyHighlightBtn" class="btn btn-sm btn-dark rounded-pill d-none" title="Öne çıkanlara ekle" aria-label="Öne çıkanlara ekle" onclick="openAddToHighlight()">` +
    `<i class="bi bi-stars"></i><span class="d-none d-md-inline ms-1">Öne çıkanlara ekle</span></button>`);
})();

async function openAddToHighlight() {
  const story = currentStoryItem();
  if (story.userId !== getSession().userId) return;

  pauseStory();
  let highlights;
  try {
    highlights = await apiFetch(`/api/highlights/mine?storyId=${story.id}`);
  } catch (err) {
    toast(err.message || "Öne çıkanlar yüklenemedi.");
    resumeStory();
    return;
  }

  const row = h => `
    <button type="button" class="story-tag-row ${h.containsStory ? "picked" : ""}" data-highlight="${h.id}" ${h.containsStory ? 'aria-disabled="true"' : ""}>
      ${h.coverUrl && h.coverMediaType !== "Video"
        ? `<img src="${escapeHtml(h.coverUrl)}" class="avatar-xs" alt="">`
        : `<span class="avatar-xs d-inline-flex align-items-center justify-content-center bg-body-secondary"><i class="bi bi-collection-play"></i></span>`}
      <span class="flex-grow-1 text-truncate" data-no-i18n>${escapeHtml(h.title)}</span>
      <i class="bi ${h.containsStory ? "bi-check-circle-fill text-success" : "bi-plus-circle text-muted"}"></i>
    </button>`;

  openSiteDialog(`
    <div class="fw-bold mb-3">Öne çıkanlara ekle</div>
    ${highlights.length ? `<div class="mb-3" style="max-height:240px;overflow-y:auto">${highlights.map(row).join("")}</div>` : ""}
    <label class="form-label small fw-semibold" for="newHighlightName">Yeni öne çıkan</label>
    <div class="input-group mb-4">
      <input type="text" id="newHighlightName" class="form-control" maxlength="30" autocomplete="off" placeholder="Öne çıkan adı">
      <button type="button" class="btn btn-primary" data-answer="create">Oluştur</button>
    </div>
    <div class="d-flex justify-content-end"><button type="button" class="btn btn-light rounded-pill px-4" data-answer="close">Kapat</button></div>`,
    (box, close) => {
      const finish = () => { close(); resumeStory(); };
      let busy = false;

      box.addEventListener("keydown", event => { if (event.key.startsWith("Arrow")) event.stopPropagation(); });

      const send = async (request, doneText) => {
        if (busy) return;
        busy = true;
        try {
          await request();
          toast(doneText);
          finish();
        } catch (err) {
          busy = false;
          toast(err.message || "Öne çıkanlara eklenemedi.");
        }
      };

      box.querySelectorAll("[data-highlight]").forEach(button => button.addEventListener("click", () => {
        if (button.getAttribute("aria-disabled") === "true") return;
        send(() => apiFetch(`/api/highlights/${button.dataset.highlight}/items`, { method: "POST", body: { storyId: story.id } }),
          "Öne çıkanlara eklendi.");
      }));

      const create = () => send(
        () => apiFetch("/api/highlights", { method: "POST", body: { title: box.querySelector("#newHighlightName").value.trim(), storyId: story.id } }),
        "Yeni öne çıkan oluşturuldu.");
      box.querySelector('[data-answer="create"]').addEventListener("click", create);
      box.querySelector("#newHighlightName").addEventListener("keydown", event => {
        if (event.key === "Enter") { event.preventDefault(); create(); }
      });
      box.querySelector('[data-answer="close"]').addEventListener("click", finish);
      box.addEventListener("dismiss", finish);
    });
}

let storyMusicMuted = false;

function prepareStoryMusic(story) {
  const audio = document.getElementById("storyMusic");
  const chip = document.getElementById("storyMusicChip");

  if (!story.musicUrl) {
    stopStoryMusic();
    return;
  }

  if (audio.dataset.url !== story.musicUrl) {
    audio.pause();
    audio.src = story.musicUrl;
    audio.dataset.url = story.musicUrl;
    audio.currentTime = 0;
  }
  audio.muted = storyMusicMuted;

  const art = document.getElementById("storyMusicArt");
  art.classList.toggle("d-none", !story.musicArtworkUrl);
  if (story.musicArtworkUrl) art.src = story.musicArtworkUrl;
  document.getElementById("storyMusicIcon").classList.toggle("d-none", !!story.musicArtworkUrl);
  document.getElementById("storyMusicText").textContent = story.musicArtist ? `${story.musicTitle} · ${story.musicArtist}` : story.musicTitle;
  chip.classList.remove("d-none");
  showStoryMusicMuteState();
}

function playStoryMusic() {
  const audio = document.getElementById("storyMusic");
  if (!audio.dataset.url || storyPaused) return;
  audio.play().catch(() => { });
}

function stopStoryMusic() {
  const audio = document.getElementById("storyMusic");
  audio.pause();
  audio.removeAttribute("src");
  delete audio.dataset.url;
  document.getElementById("storyMusicChip").classList.add("d-none");
}

function toggleStoryMusicMute() {
  const audio = document.getElementById("storyMusic");
  if (audio.paused && !storyPaused && audio.dataset.url) {
    audio.muted = storyMusicMuted = false;
    playStoryMusic();
  } else {
    storyMusicMuted = !storyMusicMuted;
    audio.muted = storyMusicMuted;
  }
  showStoryMusicMuteState();
}

function showStoryMusicMuteState() {
  const button = document.getElementById("storyMusicMute");
  button.innerHTML = `<i class="bi ${storyMusicMuted ? "bi-volume-mute-fill" : "bi-volume-up-fill"}"></i>`;
  const label = i18n(storyMusicMuted ? "story.musicUnmute" : "story.musicMute");
  button.setAttribute("aria-label", label);
  button.title = label;
}

function showStoryTags(story) {
  const from = story.resharedFrom;
  const fromChip = document.getElementById("storyResharedChip");
  fromChip.classList.toggle("d-none", !from);
  if (from) {
    fromChip.href = `profile.html?id=${from.userId}`;
    fromChip.innerHTML = `<img src="${escapeHtml(from.avatarUrl || DEFAULT_AVATAR)}" class="story-chip-avatar" alt="">` +
      `<span class="story-chip-text">${i18n("story.resharedFrom", "@" + escapeHtml(from.name))}</span>`;
  }

  const tags = story.tags || [];
  const row = document.getElementById("storyTagsRow");
  row.classList.toggle("d-none", tags.length === 0);
  row.innerHTML = tags.map(tag =>
    `<a href="profile.html?id=${tag.userId}" class="story-chip"><span class="story-chip-text">@${escapeHtml(tag.name)}</span></a>`).join("");

  const button = document.getElementById("storyReshareBtn");
  button.classList.toggle("d-none", !story.canReshare);
  button.disabled = false;
}

let storyResharing = false;

async function reshareCurrentStory() {
  if (storyResharing) return;
  const story = currentStoryItem();
  const button = document.getElementById("storyReshareBtn");

  storyResharing = true;
  button.disabled = true;
  try {
    await apiFetch(`/api/stories/${story.id}/reshare`, { method: "POST" });
    story.canReshare = false;
    if (currentStoryItem() === story) button.classList.add("d-none");
    toast("Hikâyene eklendi.");

    await loadStories();
    const group = storyGroups.findIndex(g => g.stories.some(s => s.id === story.id));
    if (group !== -1 && !document.getElementById("storyViewer").classList.contains("d-none")) {
      currentGroup = group;
      currentStory = storyGroups[group].stories.findIndex(s => s.id === story.id);
      renderedGroup = group;
    }
  } catch (err) {
    button.disabled = false;
    toast(err.message || "Hikâyene eklenemedi.");
  } finally {
    storyResharing = false;
  }
}

const MAX_STORY_TAGS = 5;
let storyTagFriends = null;
let chosenStoryTags = [];

async function toggleStoryTagPicker() {
  const panel = document.getElementById("addStoryTagPanel");
  panel.classList.toggle("d-none");
  if (panel.classList.contains("d-none")) return;

  const input = document.getElementById("addStoryTagInput");
  input.value = "";
  input.focus();
  if (storyTagFriends) { drawStoryTagList(); return; }

  const list = document.getElementById("addStoryTagList");
  list.innerHTML = `<div class="small text-muted px-2 py-2">Yükleniyor...</div>`;
  try {
    storyTagFriends = await apiFetch("/api/friends");
    drawStoryTagList();
  } catch (err) {
    list.innerHTML = `<div class="small text-danger px-2 py-2">${escapeHtml(err.message || "Arkadaşlar yüklenemedi.")}</div>`;
  }
}

function drawStoryTagList() {
  if (!storyTagFriends) return;
  const query = document.getElementById("addStoryTagInput").value.trim().toLocaleLowerCase();
  const shown = storyTagFriends.filter(f => !query || (f.name || "").toLocaleLowerCase().includes(query));
  const list = document.getElementById("addStoryTagList");

  list.innerHTML = shown.length
    ? shown.map(f => {
        const picked = chosenStoryTags.some(t => t.userId === f.userId);
        return `
          <button type="button" class="story-tag-row ${picked ? "picked" : ""}" onclick="toggleStoryTag('${f.userId}')">
            <img src="${escapeHtml(f.avatarUrl || DEFAULT_AVATAR)}" class="avatar-xs" alt="">
            <span class="flex-grow-1 text-truncate">${escapeHtml(f.name)}</span>
            <i class="bi ${picked ? "bi-check-circle-fill text-primary" : "bi-circle text-muted"}"></i>
          </button>`;
      }).join("")
    : `<div class="small text-muted px-2 py-2">${storyTagFriends.length ? "Aramana uyan arkadaş bulunamadı." : "Henüz arkadaşın yok."}</div>`;
}

function toggleStoryTag(userId) {
  const at = chosenStoryTags.findIndex(t => t.userId === userId);
  if (at !== -1) {
    chosenStoryTags.splice(at, 1);
  } else {
    if (chosenStoryTags.length >= MAX_STORY_TAGS) {
      toast(i18n("story.tagLimit", MAX_STORY_TAGS));
      return;
    }
    const friend = storyTagFriends.find(f => f.userId === userId);
    if (friend) chosenStoryTags.push({ userId: friend.userId, name: friend.name, avatarUrl: friend.avatarUrl });
  }
  drawStoryTagList();
  drawChosenStoryTags();
}

function removeStoryTag(userId) {
  chosenStoryTags = chosenStoryTags.filter(t => t.userId !== userId);
  drawStoryTagList();
  drawChosenStoryTags();
}

function drawChosenStoryTags() {
  const box = document.getElementById("addStoryTagChosen");
  box.classList.toggle("d-none", chosenStoryTags.length === 0);
  box.innerHTML = chosenStoryTags.map(t => `
    <span class="story-tag-pill">
      <img src="${escapeHtml(t.avatarUrl || DEFAULT_AVATAR)}" class="story-chip-avatar" alt="">
      <span class="text-truncate">@${escapeHtml(t.name)}</span>
      <button type="button" class="story-tag-remove" aria-label="${escapeHtml(i18n("story.tagRemove"))}" onclick="removeStoryTag('${t.userId}')"><i class="bi bi-x-lg"></i></button>
    </span>`).join("");
}

function resetStoryTags() {
  chosenStoryTags = [];
  drawChosenStoryTags();
  document.getElementById("addStoryTagPanel").classList.add("d-none");
}

let chosenStoryMusic = null;
let storyMusicResults = [];
let storyMusicSearchTimer = null;
let storyMusicSearchToken = 0;
let storyMusicPreview = null;

function toggleStoryMusicSearch() {
  const panel = document.getElementById("addStoryMusicPanel");
  panel.classList.toggle("d-none");
  if (!panel.classList.contains("d-none")) document.getElementById("addStoryMusicInput").focus();
  else stopStoryMusicPreview();
}

function searchStoryMusicSoon() {
  clearTimeout(storyMusicSearchTimer);
  storyMusicSearchTimer = setTimeout(searchStoryMusic, 400);
}

async function searchStoryMusic() {
  const query = document.getElementById("addStoryMusicInput").value.trim();
  const box = document.getElementById("addStoryMusicResults");
  const token = ++storyMusicSearchToken;
  stopStoryMusicPreview();

  if (query.length < 2) {
    storyMusicResults = [];
    box.innerHTML = "";
    return;
  }

  box.innerHTML = `<div class="small text-muted px-2 py-2">Aranıyor...</div>`;
  try {
    const found = await apiFetch(`/api/music/search?q=${encodeURIComponent(query)}`);
    if (token !== storyMusicSearchToken) return;
    storyMusicResults = found;
    box.innerHTML = found.length
      ? found.map((track, i) => `
          <div class="story-music-row">
            <button type="button" class="story-music-play" aria-label="Önizle" onclick="previewStoryMusic(${i}, this)"><i class="bi bi-play-fill"></i></button>
            <button type="button" class="story-music-pick" onclick="chooseStoryMusic(${i})" data-no-i18n>
              ${track.artworkUrl ? `<img src="${escapeHtml(track.artworkUrl)}" alt="">` : ""}
              <span class="text-truncate"><b>${escapeHtml(track.title)}</b><br><span class="text-muted small">${escapeHtml(track.artist)}</span></span>
            </button>
          </div>`).join("")
      : `<div class="small text-muted px-2 py-2">Sonuç bulunamadı.</div>`;
  } catch (err) {
    if (token !== storyMusicSearchToken) return;
    box.innerHTML = `<div class="small text-danger px-2 py-2">${escapeHtml(err.message || "Arama yapılamadı.")}</div>`;
  }
}

function previewStoryMusic(index, button) {
  const playing = storyMusicPreview && !storyMusicPreview.paused && storyMusicPreview.dataset.index === String(index);
  stopStoryMusicPreview();
  if (playing) return;

  storyMusicPreview = new Audio(storyMusicResults[index].previewUrl);
  storyMusicPreview.dataset.index = String(index);
  storyMusicPreview.play().catch(() => {});
  button.innerHTML = `<i class="bi bi-pause-fill"></i>`;
  storyMusicPreview.onended = stopStoryMusicPreview;
}

function stopStoryMusicPreview() {
  if (storyMusicPreview) { storyMusicPreview.pause(); storyMusicPreview = null; }
  document.querySelectorAll(".story-music-play").forEach(b => { b.innerHTML = `<i class="bi bi-play-fill"></i>`; });
}

function chooseStoryMusic(index) {
  const track = storyMusicResults[index];
  chosenStoryMusic = { title: track.title, artist: track.artist, previewUrl: track.previewUrl, artworkUrl: track.artworkUrl };
  stopStoryMusicPreview();
  document.getElementById("addStoryMusicPanel").classList.add("d-none");
  document.getElementById("addStoryMusicChosenText").textContent = track.artist ? `${track.title} · ${track.artist}` : track.title;
  document.getElementById("addStoryMusicChosen").classList.remove("d-none");
  document.getElementById("addStoryMusicBtn").classList.add("d-none");
}

function removeStoryMusic() {
  chosenStoryMusic = null;
  document.getElementById("addStoryMusicChosen").classList.add("d-none");
  document.getElementById("addStoryMusicBtn").classList.remove("d-none");
}

document.getElementById("addStoryModal")?.addEventListener("hidden.bs.modal", () => {
  stopStoryMusicPreview();
  document.getElementById("addStoryMusicPanel").classList.add("d-none");
  document.getElementById("addStoryTagPanel").classList.add("d-none");
  storyTagFriends = null;
});

function previewNewStory(input) {
  const file = input.files && input.files[0];
  if (!file) return;

  const wrap = document.getElementById("addStoryPreviewWrap");
  const img = document.getElementById("addStoryPreviewImg");
  const video = document.getElementById("addStoryPreviewVideo");
  const isVideo = file.type.startsWith("video/");
  const url = URL.createObjectURL(file);

  img.classList.toggle("d-none", isVideo);
  video.classList.toggle("d-none", !isVideo);
  if (isVideo) { img.src = ""; video.src = url; } else { video.src = ""; img.src = url; }
  wrap.classList.remove("d-none");
}

document.addEventListener("paste", event => {
  if (!document.getElementById("addStoryModal")?.classList.contains("show")) return;
  const pictures = getPastedImages(event);
  if (pictures.length === 0) return;

  const input = document.getElementById("addStoryFileInput");
  const transfer = new DataTransfer();
  transfer.items.add(pictures[0]);
  input.files = transfer.files;
  previewNewStory(input);
  event.preventDefault();
});

async function publishStory() {
  const fileInput = document.getElementById("addStoryFileInput");
  const file = fileInput.files[0];
  if (!file) return;

  try {
    const formData = new FormData();
    formData.append("media", file);
    if (chosenStoryMusic) {
      formData.append("musicTitle", chosenStoryMusic.title);
      formData.append("musicArtist", chosenStoryMusic.artist || "");
      formData.append("musicUrl", chosenStoryMusic.previewUrl);
      if (chosenStoryMusic.artworkUrl) formData.append("musicArtworkUrl", chosenStoryMusic.artworkUrl);
    }
    chosenStoryTags.forEach(tag => formData.append("taggedUserIds", tag.userId));
    await apiFetchForm("/api/stories", { method: "POST", body: formData });

    bootstrap.Modal.getInstance(document.getElementById("addStoryModal"))?.hide();
    fileInput.value = "";
    document.getElementById("addStoryPreviewVideo").src = "";
    document.getElementById("addStoryPreviewWrap").classList.add("d-none");
    removeStoryMusic();
    resetStoryTags();
    toast("Hikayen paylaşıldı!");
    await loadStories();
  } catch (err) {
    toast(err.message || "Hikaye paylaşılamadı.");
  }
}

loadStories();
