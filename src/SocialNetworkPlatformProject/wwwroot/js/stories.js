// home.html story bar + fullscreen story viewer, wired to the real backend.
//
// The server sends a flat list of stories. The page shows one card per person and the viewer plays one person's
// stories at a time (the bars at the top are that person's stories); when they end it moves on to the next person.
// Your own stories are a person like any other, shown first.

let storyGroups = [];
const STORY_DURATION = 4000;
let currentGroup = 0;
let currentStory = 0; // index inside the current person's stories
let renderedGroup = -1; // whose bars are on screen
let storyTimer = null;
let storyLoading = false; // the picture of the current story is not there yet
let storyPaused = false;  // somebody is typing a reply or reading the viewers list
let storyLoadToken = 0;   // tells a late picture of a story we left from the current one

// One entry per person: { userId, userName, userAvatarUrl, stories: oldest first }. Your own first, then the people with
// something you have not watched, then the rest; newest person first inside each part.
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

// Where a person's stories start playing: the first one you have not watched, or the beginning.
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

// The bars at the top: one per story of the person being watched.
function renderStoryBars() {
  const bars = document.getElementById("storyBars");
  bars.innerHTML = currentGroupItem().stories.map((_, i) => `<div class="bar" id="storyBar-${i}"><div class="fill"></div></div>`).join("");
  renderedGroup = currentGroup;
}

function markBarsDone(upToIndex) {
  currentGroupItem().stories.forEach((_, i) => {
    const bar = document.getElementById("storyBar-" + i);
    if (!bar) return;

    // No transition here: a bar that was filling (or full) goes to its place at once. With the transition still set
    // from the last run, going back made the bar to the right drain slowly instead of being empty.
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

// How long ago the story was posted, short: "şimdi", "27dk", "35dk", then "1s", "2s"... "23s" (a story lasts 24 hours).
function storyAgeText(createdAt) {
  const minutes = Math.max(0, Math.floor((Date.now() - parseServerDate(createdAt).getTime()) / 60000));
  if (minutes < 1) return i18n("story.age.now");
  if (minutes < 60) return i18n("story.age.minutes", minutes);
  return i18n("story.age.hours", Math.floor(minutes / 60));
}

// Shows one story of one person. Past the last story of a person comes the next person; before the first, the one before.
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

  // Moving to another person: that person's story slides in from the side it was on (not when the viewer has just been opened).
  const slideDirection = renderedGroup !== -1 && renderedGroup !== groupIndex ? (groupIndex > renderedGroup ? 1 : -1) : 0;
  currentGroup = groupIndex;
  currentStory = storyIndex;
  storyLoading = true; // until the picture is there (below): nothing runs, and resumeStory() leaves the bar alone
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

  // Own story: the eye counter (who watched), the heart count and delete. Somebody else's: the heart and the
  // reply box, and watching it is reported once.
  const isMine = story.userId === getSession().userId;
  document.getElementById("storyViewersBtn").classList.toggle("d-none", !isMine);
  document.getElementById("storyDeleteBtn").classList.toggle("d-none", !isMine);
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

// Puts the story's picture or video on screen. The bar does not start until it is really there: on a slow connection a
// photo takes longer than a story lasts, and the viewer used to keep showing the one before it (the stories looked
// like the same one three times) while the bars ran on. A spinner shows meanwhile.
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

  // A story we have already left (tapped past, or closed) never finishes loading into the current one.
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
    img.removeAttribute("src"); // the previous picture goes at once
    img.src = story.mediaUrl;
  }
}

// Starts the bar (and a video) once the picture is there and nobody is typing or reading the viewers list.
function startStoryIfReady() {
  if (storyLoading || storyPaused) return;
  runProgress(currentStory);
  playStoryMusic();
  if (currentStoryItem().mediaType === "Video") document.getElementById("storyViewerVideo").play().catch(() => {});
}

// The next story of the same person, or the first one to watch of the next person.
function nextStoryItem() {
  const next = currentGroupItem().stories[currentStory + 1];
  if (next) return next;
  const nextGroup = storyGroups[currentGroup + 1];
  return nextGroup ? nextGroup.stories[firstUnviewedIndex(nextGroup)] : null;
}

// Fetches a picture ahead of time so it is there when its turn comes (videos are left to stream).
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
    // Applies the empty bar before the transition starts. Without it a bar that was full (the story just before the one
    // watched now) never saw "0%", so going back left it full instead of filling again.
    void fill.offsetWidth;
    fill.style.transition = "width " + STORY_DURATION + "ms linear";
    fill.style.width = "100%";
  }
  storyTimer = setTimeout(() => nextStory(), STORY_DURATION);
}

// The story stands still while somebody reads the viewers list or types a reply; resuming starts its bar over.
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
  if (document.getElementById("storyViewer").classList.contains("d-none")) return; // closed meanwhile
  storyPaused = false;
  startStoryIfReady();
}

function openStory(groupIndex) {
  renderedGroup = -1;
  document.getElementById("storyViewer").classList.remove("d-none");
  document.body.style.overflow = "hidden";
  showStory(groupIndex, firstUnviewedIndex(storyGroups[groupIndex]));
}

// ---- Wide screens: the neighbours of the story on screen ----
// The previous and the next person show on the left and right of the story (their story to watch first, dimmed). Clicking one goes
// there, like the arrows next to the story do for the previous and next story. Narrow screens hide all of this (css).
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
  void inner.offsetWidth; // restarts the animation
  inner.classList.add(direction > 0 ? "slide-from-right" : "slide-from-left");
}

// The arrow keys go to the previous and next story, Escape closes the viewer (while it is open and nobody is typing).
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

// Holding a finger or the mouse button down on the story stops it (the bar, the video and the song) and lets go resumes it,
// like the real thing. The click that ends a hold must not count as a tap to the next or the previous story.
const STORY_HOLD_MS = 250;
let storyHoldTimer = null;
let storyHeld = false;       // the story is paused because of a hold
let storyHoldEndedAt = 0;

function startStoryHold(event) {
  if (event.button > 0) return; // only the main button
  clearTimeout(storyHoldTimer);
  storyHoldTimer = setTimeout(() => {
    if (storyPaused || storyLoading) return; // already stopped by something else (typing a reply, the viewers list)
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

// True for the click that comes right after a hold was let go: it is ignored.
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
  nav.addEventListener("contextmenu", event => event.preventDefault()); // a long press on a phone must not open the picture menu
})();

function closeStory() {
  clearTimeout(storyTimer);
  storyLoadToken++; // a picture still on its way is no longer wanted
  storyLoading = false;
  document.getElementById("storyViewer").classList.add("d-none");
  document.getElementById("storyViewerVideo").pause();
  stopStoryMusic();
  document.body.style.overflow = "";
  hideStoryViewersPanel();
  resetStoryReply();

  // The rings of the stories just watched turn grey.
  loadStories();
}

// Tells the server this story was watched (it keeps one count per person). The page keeps its own flag so going back
// and forth between stories does not report it again.
function reportStoryView(story) {
  story.isViewed = true;
  apiFetch(`/api/stories/${story.id}/view`, { method: "POST" }).catch(() => {});
}

// ---- Somebody else's story: the heart and the reply ----
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

// The send arrow is there only while something is typed.
function updateStoryReplyButton() {
  const hasText = document.getElementById("storyReplyInput").value.trim() !== "";
  document.getElementById("storyReplySend").classList.toggle("d-none", !hasText);
}

// The reply is an ordinary message to the story's owner: it lands in the same chat as any other (messages.html).
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

// ---- Your own story: who watched it, and delete ----
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

// Closing the list lets the story run again (its bar starts over).
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

// ---- The song of a story (viewer) ----
// A story can carry a 30-second song (see the add-story window). It starts with the story, stops when the story is paused,
// left or closed, and the little button on the chip turns the sound off for the stories that follow too.
let storyMusicMuted = false;

function prepareStoryMusic(story) {
  const audio = document.getElementById("storyMusic");
  const chip = document.getElementById("storyMusicChip");

  if (!story.musicUrl) {
    stopStoryMusic();
    return;
  }

  // The same song in the next story of a person goes on without a break.
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
  audio.play().catch(() => { /* the browser wants a tap first: the button on the chip starts it */ });
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
  // When the browser held the sound back, the first tap starts it instead of muting it.
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

// ---- Tags and "add to your story" (viewer) ----
// Under the header: "Story by @Owner" when this story was added from somebody else's, then the tagged people. A tagged
// person also gets a button that puts the story on their own story.
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

    // The list is built again: my new story shows up (my own stories come first), so the viewer finds its place again.
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

// ---- Add-story modal: tagging friends ----
const MAX_STORY_TAGS = 5;
let storyTagFriends = null; // my friends, loaded when the picker is first opened
let chosenStoryTags = [];   // [{ userId, name, avatarUrl }]

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

// ---- Add-story modal: choosing a song ----
let chosenStoryMusic = null;       // { title, artist, previewUrl, artworkUrl }
let storyMusicResults = [];
let storyMusicSearchTimer = null;
let storyMusicSearchToken = 0;
let storyMusicPreview = null;      // the <audio> that plays a result while the person is choosing

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
    if (token !== storyMusicSearchToken) return; // a newer search is already running
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
  storyTagFriends = null; // friends may have changed by the next time
});

// ---- Add-story modal ----
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

// With the "Hikaye Ekle" window open, a picture pasted anywhere in it becomes the story's picture.
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
