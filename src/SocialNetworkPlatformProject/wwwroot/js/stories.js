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
    <div class="story-card" onclick="openStory(${index})">
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
