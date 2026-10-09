// home.html story bar + fullscreen viewer, wired to the real backend.

let activeStories = [];
const STORY_DURATION = 4000;
let currentStory = 0;
let storyTimer = null;

function storyCardHtml(story, index) {
  const thumb = story.mediaType === "Video"
    ? `<video src="${story.mediaUrl}" muted></video><i class="bi bi-play-circle-fill story-video-badge"></i>`
    : `<img src="${story.mediaUrl}" alt="">`;

  return `
    <div class="story-card ${story.isViewed ? "story-seen" : ""}" onclick="openStory(${index})">
      <span class="story-ring"><img src="${story.userAvatarUrl || DEFAULT_AVATAR}" alt=""></span>
      ${thumb}
      <span class="story-name">${escapeHtml(story.userName)}</span>
    </div>`;
}

async function loadStories() {
  const bar = document.getElementById("storyBarDynamic");
  try {
    activeStories = await apiFetch("/api/stories");
    bar.innerHTML = activeStories.map(storyCardHtml).join("");
  } catch (err) {
    bar.innerHTML = "";
  }
}

function renderStoryBars() {
  const bars = document.getElementById("storyBars");
  bars.innerHTML = activeStories.map((_, i) => `<div class="bar" id="storyBar-${i}"><div class="fill"></div></div>`).join("");
}

function markBarsDone(upToIndex) {
  activeStories.forEach((_, i) => {
    const bar = document.getElementById("storyBar-" + i);
    if (!bar) return;
    if (i < upToIndex) {
      bar.classList.add("done");
      bar.querySelector(".fill").style.width = "100%";
    } else if (i > upToIndex) {
      bar.classList.remove("done");
      bar.querySelector(".fill").style.width = "0%";
    }
  });
}

function showStory(index) {
  if (index < 0) return;
  if (index >= activeStories.length) {
    closeStory();
    return;
  }
  currentStory = index;
  const story = activeStories[index];
  document.getElementById("storyViewerAvatar").src = story.userAvatarUrl || DEFAULT_AVATAR;
  document.getElementById("storyViewerName").textContent = story.userName;

  // Own story: the eye counter (who watched) and delete. Somebody else's: watching it is reported once.
  const isMine = story.userId === getSession().userId;
  document.getElementById("storyViewersBtn").classList.toggle("d-none", !isMine);
  document.getElementById("storyDeleteBtn").classList.toggle("d-none", !isMine);
  document.getElementById("storyViewCount").textContent = story.viewCount;
  hideStoryViewersPanel();
  if (!isMine && !story.isViewed) reportStoryView(story);

  const img = document.getElementById("storyViewerImage");
  const video = document.getElementById("storyViewerVideo");
  const isVideo = story.mediaType === "Video";

  img.classList.toggle("d-none", isVideo);
  video.classList.toggle("d-none", !isVideo);
  if (isVideo) {
    img.src = "";
    video.src = story.mediaUrl;
    video.currentTime = 0;
    video.play().catch(() => {});
  } else {
    video.pause();
    video.removeAttribute("src");
    img.src = story.mediaUrl;
  }

  markBarsDone(index);
  runProgress(index);
}

function runProgress(index) {
  clearTimeout(storyTimer);
  const bar = document.getElementById("storyBar-" + index);
  const fill = bar ? bar.querySelector(".fill") : null;
  if (fill) {
    fill.style.transition = "none";
    fill.style.width = "0%";
    requestAnimationFrame(() => {
      fill.style.transition = "width " + STORY_DURATION + "ms linear";
      fill.style.width = "100%";
    });
  }
  storyTimer = setTimeout(() => nextStory(), STORY_DURATION);
}

function openStory(index) {
  renderStoryBars();
  document.getElementById("storyViewer").classList.remove("d-none");
  document.body.style.overflow = "hidden";
  showStory(index);
}

function nextStory() {
  showStory(currentStory + 1);
}

function prevStory() {
  showStory(currentStory - 1);
}

function closeStory() {
  clearTimeout(storyTimer);
  document.getElementById("storyViewer").classList.add("d-none");
  document.getElementById("storyViewerVideo").pause();
  document.body.style.overflow = "";
  hideStoryViewersPanel();

  // The rings of the stories just watched turn grey.
  loadStories();
}

// Tells the server this story was watched (it keeps one count per person). The page keeps its own flag so going back
// and forth between stories does not report it again.
function reportStoryView(story) {
  story.isViewed = true;
  apiFetch(`/api/stories/${story.id}/view`, { method: "POST" }).catch(() => {});
}

// ---- Your own story: who watched it, and delete ----
function hideStoryViewersPanel() {
  document.getElementById("storyViewersPanel").classList.add("d-none");
}

async function openStoryViewers() {
  const story = activeStories[currentStory];
  const list = document.getElementById("storyViewersList");

  // The story stands still while the list is open.
  clearTimeout(storyTimer);
  document.getElementById("storyViewerVideo").pause();
  const fill = document.querySelector(`#storyBar-${currentStory} .fill`);
  if (fill) {
    fill.style.width = getComputedStyle(fill).width;
    fill.style.transition = "none";
  }

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
  runProgress(currentStory);
  const story = activeStories[currentStory];
  if (story.mediaType === "Video") document.getElementById("storyViewerVideo").play().catch(() => {});
}

async function deleteCurrentStory() {
  const story = activeStories[currentStory];
  if (!confirm("Bu hikâye silinsin mi?")) return;

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
