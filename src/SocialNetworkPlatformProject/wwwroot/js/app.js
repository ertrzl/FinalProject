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

// ---- Composer: image/video attach preview ----
function previewComposerImage(input) {
  const wrap = document.getElementById("composerPreview");
  const img = document.getElementById("composerPreviewImg");
  const video = document.getElementById("composerPreviewVideo");
  const file = input.files && input.files[0];
  if (!file || !wrap) return;

  const isVideo = file.type.startsWith("video/");
  const url = URL.createObjectURL(file);

  if (img) { img.src = isVideo ? "" : url; img.classList.toggle("d-none", isVideo); }
  if (video) { video.src = isVideo ? url : ""; video.classList.toggle("d-none", !isVideo); }
  wrap.classList.remove("d-none");
}
function clearComposerImage() {
  const wrap = document.getElementById("composerPreview");
  const fileInput = document.getElementById("composerFile");
  const img = document.getElementById("composerPreviewImg");
  const video = document.getElementById("composerPreviewVideo");
  if (wrap) wrap.classList.add("d-none");
  if (fileInput) fileInput.value = "";
  if (img) img.src = "";
  if (video) video.src = "";
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
  document.querySelectorAll(".theme-toggle-btn i").forEach(icon => {
    icon.className = theme === "dark" ? "bi bi-sun-fill" : "bi bi-moon-stars-fill";
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
