// SocialNet — shared front-end interactions (no backend, UI only)
// Tabs, collapses, dropdowns, and modals are handled by Bootstrap's own JS bundle.

// ---- Tiny toast helper (visual feedback for actions with no backend) ----
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

// ---- Composer: image attach preview ----
function previewComposerImage(input) {
  const wrap = document.getElementById("composerPreview");
  const img = document.getElementById("composerPreviewImg");
  if (input.files && input.files[0] && wrap && img) {
    img.src = URL.createObjectURL(input.files[0]);
    wrap.classList.remove("d-none");
  }
}
function clearComposerImage() {
  const wrap = document.getElementById("composerPreview");
  const fileInput = document.getElementById("composerFile");
  if (wrap) wrap.classList.add("d-none");
  if (fileInput) fileInput.value = "";
}

// ---- Publish a new post from the composer into the feed above it ----
let postCounter = 100;
function publishPost(btn) {
  const composer = btn.closest(".composer");
  const textarea = composer.querySelector("textarea");
  const fileInput = composer.querySelector('input[type="file"]');
  const privacy = composer.querySelector(".privacy-select").value;
  const text = textarea.value.trim();
  const previewImg = composer.querySelector("#composerPreviewImg");
  const hasImage = previewImg && previewImg.src && !composer.querySelector("#composerPreview").classList.contains("d-none");

  if (!text && !hasImage) {
    textarea.classList.add("is-invalid");
    setTimeout(() => textarea.classList.remove("is-invalid"), 1200);
    return;
  }

  postCounter++;
  const id = "comments-new-" + postCounter;
  const privacyIcon = privacy.includes("Arkadaş") ? "bi-people-fill" : "bi-globe-americas";

  const post = document.createElement("div");
  post.className = "card border-0 shadow-sm rounded-4 p-3 mb-4";
  post.innerHTML = `
    <div class="d-flex align-items-center gap-3">
      <img src="https://i.pravatar.cc/80?img=45" class="avatar-sm" alt="">
      <div class="flex-grow-1">
        <div class="fw-bold">Sen</div>
        <div class="text-muted small">Şimdi · <i class="bi ${privacyIcon}"></i> ${privacy.replace(/^[^\s]+\s/, "")}</div>
      </div>
      <div class="dropdown">
        <button class="btn btn-sm btn-light rounded-circle" data-bs-toggle="dropdown"><i class="bi bi-three-dots"></i></button>
        <ul class="dropdown-menu dropdown-menu-end">
          <li><button class="dropdown-item" onclick="deletePost(this)"><i class="bi bi-trash me-2"></i>Gönderiyi Sil</button></li>
          <li><button class="dropdown-item" onclick="toast('Gönderi kaydedildi.')"><i class="bi bi-bookmark me-2"></i>Kaydet</button></li>
        </ul>
      </div>
    </div>
    ${text ? `<p class="mt-3 mb-2">${text.replace(/</g, "&lt;")}</p>` : ""}
    ${hasImage ? `<div class="rounded-3 overflow-hidden mb-2"><img src="${previewImg.src}" class="w-100" style="max-height:420px;object-fit:cover;" alt=""></div>` : ""}
    <div class="d-flex justify-content-between text-muted small py-2 border-bottom">
      <span><i class="bi bi-heart-fill text-danger"></i> 0 beğeni</span>
      <span>0 yorum</span>
    </div>
    <div class="d-flex pt-1">
      <button class="btn btn-sm flex-fill text-muted fw-semibold like-btn" onclick="toggleLike(this)">
        <i class="like-icon bi bi-heart"></i> Beğen <span class="like-count" data-count="0">0</span>
      </button>
      <button class="btn btn-sm flex-fill text-muted fw-semibold" data-bs-toggle="collapse" data-bs-target="#${id}">
        <i class="bi bi-chat"></i> Yorum Yap
      </button>
      <button class="btn btn-sm flex-fill text-muted fw-semibold" onclick="copyPostLink(this)"><i class="bi bi-share"></i> Paylaş</button>
    </div>
    <div class="collapse comments-collapse mt-3 pt-3 border-top" id="${id}">
      <form class="d-flex gap-2 align-items-center" onsubmit="addComment(event, this, '${id}')">
        <img src="https://i.pravatar.cc/80?img=45" class="avatar-xs" alt="">
        <input type="text" class="form-control form-control-sm rounded-pill" placeholder="Bir yorum yaz...">
      </form>
    </div>`;

  composer.insertAdjacentElement("afterend", post);

  textarea.value = "";
  clearComposerImage();
  toast("Gönderi paylaşıldı!");
}

function copyPostLink(btn) {
  const url = window.location.href.split("#")[0] + "#post";
  if (navigator.clipboard) {
    navigator.clipboard.writeText(url).catch(() => {});
  }
  toast("Bağlantı kopyalandı.");
}

// ---- Like toggle ----
function toggleLike(btn) {
  const countEl = btn.querySelector(".like-count");
  const icon = btn.querySelector(".like-icon");
  let count = parseInt(countEl.dataset.count, 10);
  const liked = btn.classList.toggle("liked");
  count = liked ? count + 1 : count - 1;
  countEl.dataset.count = count;
  countEl.textContent = count;
  icon.className = "like-icon bi " + (liked ? "bi-heart-fill" : "bi-heart");
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
