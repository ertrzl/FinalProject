// SocialNet — small helpers every page shares: toasts, the composer's attachment preview, search form, dark mode.
// Tabs, collapses, dropdowns, and modals are handled by Bootstrap's own JS bundle; posts and comments are in post-card.js.

// ---- Tiny toast helper ----
function toast(message) {
  let container = document.getElementById("toastContainer");
  if (!container) {
    container = document.createElement("div");
    container.id = "toastContainer";
    container.className = "toast-container position-fixed bottom-0 end-0 p-3";
    container.style.zIndex = 1080;
    document.body.appendChild(container);
  }
  const el = document.createElement("div");
  el.className = "toast align-items-center text-bg-dark border-0";
  el.setAttribute("role", "alert");
  el.innerHTML = `<div class="d-flex">
      <div class="toast-body">${message}</div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
    </div>`;
  container.appendChild(el);
  const t = new bootstrap.Toast(el, { delay: 2200 });
  t.show();
  el.addEventListener("hidden.bs.toast", () => el.remove());
}

// ---- Auth: avatar preview on register page ----
function previewAvatar(input) {
  const preview = document.getElementById("avatarPreview");
  if (input.files && input.files[0] && preview) {
    preview.src = URL.createObjectURL(input.files[0]);
  }
}

// ---- Composer: the photos / video chosen for the post being written ----
// The page's file input only picks files; they are kept here so each can be taken out again, and more can be added
// later. How many are allowed, and whether photos and a video may mix, is the server's rule: the page just sends them.
let composerFiles = [];

function addComposerFiles(input) {
  composerFiles.push(...Array.from(input.files || []));
  input.value = ""; // the same file can be picked again after removing it
  renderComposerPreview();
}

function removeComposerFile(index) {
  composerFiles.splice(index, 1);
  renderComposerPreview();
}

function clearComposerImage() {
  composerFiles = [];
  renderComposerPreview();
  resetComposerPoll();
}

// Everything chosen for the post besides its text: the photos / video, or the poll. The submit code of the home,
// profile and group pages calls this; what is allowed together (a poll takes no photos...) is the server's rule.
function appendComposerAttachments(formData) {
  composerFiles.forEach(file => formData.append("media", file));

  const poll = getComposerPoll();
  if (poll) {
    poll.options.forEach(option => formData.append("pollOptions", option));
    formData.append("pollDurationHours", poll.hours);
  }
}

// ---- Composer: "Anket" (the answers and how long it stays open) ----
// Added to the page's own composer by this file, so the three pages that have one carry no markup for it.

function setUpComposerPoll() {
  const preview = document.getElementById("composerPreview");
  const attach = document.querySelector(".composer .attach-label");
  if (!preview || !attach) return;

  attach.insertAdjacentHTML("afterend",
    `<button type="button" class="attach-label border-0" onclick="toggleComposerPoll()"><i class="bi bi-bar-chart-line"></i> Anket</button>`);

  preview.insertAdjacentHTML("afterend", `
    <div class="d-none mt-3" id="composerPoll">
      <div class="fw-semibold small mb-2"><i class="bi bi-bar-chart-line me-1"></i>Anket: sorunu yukarıya yaz, seçenekleri buraya</div>
      <div id="composerPollOptions" class="d-flex flex-column gap-2"></div>
      <div class="d-flex align-items-center gap-2 mt-2 flex-wrap">
        <button type="button" class="btn btn-light btn-sm rounded-pill" onclick="addComposerPollOption()"><i class="bi bi-plus-lg"></i> Seçenek ekle</button>
        <select id="composerPollHours" class="form-select form-select-sm w-auto rounded-pill">
          <option value="1">1 saat açık</option>
          <option value="6">6 saat açık</option>
          <option value="24" selected>1 gün açık</option>
          <option value="72">3 gün açık</option>
          <option value="168">7 gün açık</option>
        </select>
      </div>
    </div>`);
}

function addComposerPollOption() {
  const list = document.getElementById("composerPollOptions");
  const number = list.children.length + 1;
  list.insertAdjacentHTML("beforeend",
    `<input type="text" class="form-control form-control-sm" maxlength="80" placeholder="${number}. seçenek">`);
}

function toggleComposerPoll() {
  const panel = document.getElementById("composerPoll");
  const opening = panel.classList.contains("d-none");
  panel.classList.toggle("d-none", !opening);
  if (!opening) { resetComposerPoll(); return; }

  document.getElementById("composerPollOptions").innerHTML = "";
  addComposerPollOption();
  addComposerPollOption();
  document.querySelector("#composerPollOptions input").focus();
}

function resetComposerPoll() {
  document.getElementById("composerPoll")?.classList.add("d-none");
  const list = document.getElementById("composerPollOptions");
  if (list) list.innerHTML = "";
}

// The poll being written, or null when the panel is closed. Empty boxes are left out.
function getComposerPoll() {
  const panel = document.getElementById("composerPoll");
  if (!panel || panel.classList.contains("d-none")) return null;

  const options = Array.from(panel.querySelectorAll("#composerPollOptions input"))
    .map(input => input.value.trim())
    .filter(Boolean);
  return { options, hours: document.getElementById("composerPollHours").value };
}

setUpComposerPoll();

// What the post will be sent with (the submit code of home, profile and group pages reads it).
function getComposerFiles() {
  return composerFiles;
}

function renderComposerPreview() {
  const wrap = document.getElementById("composerPreview");
  if (!wrap) return;

  wrap.classList.toggle("d-none", composerFiles.length === 0);
  wrap.innerHTML = composerFiles.map((file, i) => {
    const url = URL.createObjectURL(file);
    const thumb = file.type.startsWith("video/")
      ? `<video src="${url}" class="composer-thumb" muted></video><i class="bi bi-play-circle-fill composer-thumb-badge"></i>`
      : `<img src="${url}" class="composer-thumb" alt="">`;
    return `
      <div class="composer-thumb-wrap">
        ${thumb}
        <button type="button" class="btn btn-dark btn-sm rounded-circle composer-thumb-remove" title="Kaldır" onclick="removeComposerFile(${i})"><i class="bi bi-x"></i></button>
      </div>`;
  }).join("");
}

// ---- Search form: keep the query in the results heading ----
function submitSearch(form) {
  const input = form.querySelector('input[name="q"]');
  if (input && input.value.trim()) {
    window.location.href = "search.html?q=" + encodeURIComponent(input.value.trim());
    return false;
  }
  return true;
}

// ---- Dark mode ----
function applyTheme(theme) {
  document.documentElement.setAttribute("data-bs-theme", theme);
  // Swap only the two icon classes: the panel's icon is found by its own "theme-icon" class and must keep it.
  document.querySelectorAll(".theme-toggle-btn i, .theme-icon").forEach(icon => {
    icon.classList.toggle("bi-sun-fill", theme === "dark");
    icon.classList.toggle("bi-moon-stars-fill", theme !== "dark");
  });
}

function toggleTheme() {
  const current = document.documentElement.getAttribute("data-bs-theme") === "dark" ? "dark" : "light";
  const next = current === "dark" ? "light" : "dark";
  try { localStorage.setItem("socialnet-theme", next); } catch (e) {}
  applyTheme(next);
}

(function initTheme() {
  let saved = "light";
  try { saved = localStorage.getItem("socialnet-theme") || "light"; } catch (e) {}
  applyTheme(saved);
})();

// Live search suggestions moved to js/navbar-search.js (real backend data, not a hardcoded list).
