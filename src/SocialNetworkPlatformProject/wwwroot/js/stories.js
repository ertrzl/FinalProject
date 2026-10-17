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
    ? `<video src="${cover.mediaUrl}" muted></video><i class="bi bi-play-circle-fill story-video-badge"></i>`
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

  currentGroup = groupIndex;
  currentStory = storyIndex;
  storyLoading = true; // until the picture is there (below): nothing runs, and resumeStory() leaves the bar alone
  storyPaused = false;
  if (renderedGroup !== currentGroup) {
    renderStoryBars();
    storyGroups[currentGroup].stories.forEach(preloadStory);
  }

  const group = currentGroupItem();
  const story = currentStoryItem();
  document.getElementById("storyViewerAvatar").src = group.userAvatarUrl || DEFAULT_AVATAR;
  document.getElementById("storyViewerName").textContent = group.userName;
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

function nextStory() {
  showStory(currentGroup, currentStory + 1);
}

function prevStory() {
  showStory(currentGroup, currentStory - 1);
}

function closeStory() {
  clearTimeout(storyTimer);
  storyLoadToken++; // a picture still on its way is no longer wanted
  storyLoading = false;
  document.getElementById("storyViewer").classList.add("d-none");
  document.getElementById("storyViewerVideo").pause();
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
    await apiFetchForm("/api/stories", { method: "POST", body: formData });

    bootstrap.Modal.getInstance(document.getElementById("addStoryModal"))?.hide();
    fileInput.value = "";
    document.getElementById("addStoryPreviewVideo").src = "";
    document.getElementById("addStoryPreviewWrap").classList.add("d-none");
    toast("Hikayen paylaşıldı!");
    await loadStories();
  } catch (err) {
    toast(err.message || "Hikaye paylaşılamadı.");
  }
}

loadStories();
