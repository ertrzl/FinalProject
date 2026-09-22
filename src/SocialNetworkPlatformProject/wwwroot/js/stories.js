// Fullscreen story viewer (home.html only). Visual only, no backend.

const homeStories = [
  { name: "Lana Rose", avatar: "https://i.pravatar.cc/80?img=47", image: "https://picsum.photos/seed/story1/500/900" },
  { name: "Winnie Haley", avatar: "https://i.pravatar.cc/80?img=32", image: "https://picsum.photos/seed/story2/500/900" },
  { name: "Daniel Bale", avatar: "https://i.pravatar.cc/80?img=15", image: "https://picsum.photos/seed/story3/500/900" },
];

const STORY_DURATION = 4000;
let currentStory = 0;
let storyTimer = null;
let storyProgressStart = 0;

function renderStoryBars() {
  const bars = document.getElementById("storyBars");
  bars.innerHTML = homeStories
    .map((_, i) => `<div class="bar" id="storyBar-${i}"><div class="fill"></div></div>`)
    .join("");
}

function markBarsDone(upToIndex) {
  homeStories.forEach((_, i) => {
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
  if (index >= homeStories.length) {
    closeStory();
    return;
  }
  currentStory = index;
  const story = homeStories[index];
  document.getElementById("storyViewerAvatar").src = story.avatar;
  document.getElementById("storyViewerName").textContent = story.name;
  document.getElementById("storyViewerImage").src = story.image;
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
  document.body.style.overflow = "";
}

// ---- Add-story modal ----
function previewNewStory(input) {
  const wrap = document.getElementById("addStoryPreviewWrap");
  const img = document.getElementById("addStoryPreviewImg");
  if (input.files && input.files[0]) {
    img.src = URL.createObjectURL(input.files[0]);
    wrap.classList.remove("d-none");
  }
}
