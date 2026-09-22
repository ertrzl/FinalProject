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

// ---- Profile: cover photo preview (profile.html) ----
function previewCoverPhoto(input) {
  const cover = document.getElementById("profileCover");
  if (input.files && input.files[0] && cover) {
    cover.style.backgroundImage = `url(${URL.createObjectURL(input.files[0])})`;
    cover.style.backgroundSize = "cover";
    cover.style.backgroundPosition = "center";
    toast("Kapak fotoğrafı güncellendi.");
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

function deletePost(btn) {
  const post = btn.closest(".card");
  post.style.transition = "opacity .2s";
  post.style.opacity = "0";
  setTimeout(() => post.remove(), 200);
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

// ---- Add a comment (front-end only, no persistence) ----
function addComment(event, form, threadId) {
  event.preventDefault();
  const input = form.querySelector("input[type=text]");
  const text = input.value.trim();
  if (!text) return;
  const list = document.getElementById(threadId);
  const comment = document.createElement("div");
  comment.className = "d-flex gap-2 mb-3";
  comment.innerHTML = `
    <img class="avatar-xs flex-shrink-0" src="https://i.pravatar.cc/80?img=45" alt="">
    <div class="comment-bubble flex-grow-1">
      <div class="fw-bold small">Sen</div>
      <div class="small">${text.replace(/</g, "&lt;")}</div>
      <div class="small text-muted mt-1 d-flex gap-3">
        <span>Şimdi</span><a href="#" class="text-muted" onclick="likeComment(event, this)">Beğen</a><a href="#" class="text-muted" onclick="focusReply(event, this)">Yanıtla</a>
      </div>
    </div>`;
  list.insertBefore(comment, list.lastElementChild);
  input.value = "";

  const post = list.closest(".card");
  const counter = post.querySelector(".post-stats span:last-child, .d-flex.justify-content-between.text-muted.small span:last-child");
  if (counter) {
    const n = parseInt(counter.textContent, 10) || 0;
    counter.textContent = (n + 1) + " yorum";
  }
}

// ---- Like / reply on an individual comment ----
function likeComment(event, link) {
  event.preventDefault();
  const liked = link.classList.toggle("fw-bold");
  link.classList.toggle("text-primary", liked);
  link.textContent = liked ? "Beğenildi" : "Beğen";
}

function focusReply(event, link) {
  event.preventDefault();
  const bubble = link.closest(".comment-bubble");
  const name = bubble ? bubble.querySelector(".c-name").textContent : "";
  const thread = link.closest(".comments-collapse");
  const input = thread ? thread.querySelector('form input[type="text"]') : null;
  if (input) {
    input.value = "@" + name + " ";
    input.focus();
  }
}

// ---- Friend request actions (visual only) ----
function acceptRequest(btn) {
  const card = btn.closest(".request-card");
  card.style.transition = "opacity .2s";
  card.style.opacity = "0";
  setTimeout(() => card.remove(), 200);
  toast("Arkadaşlık isteği kabul edildi.");
}
function declineRequest(btn) {
  const card = btn.closest(".request-card");
  card.style.transition = "opacity .2s";
  card.style.opacity = "0";
  setTimeout(() => card.remove(), 200);
}

// ---- Send / cancel a friend request from suggestion cards ----
function toggleFriendRequest(btn) {
  const sent = btn.classList.toggle("sent");
  if (sent) {
    btn.textContent = "İstek Gönderildi";
    btn.classList.remove("btn-outline-primary");
    btn.classList.add("btn-muted", "btn-secondary");
    btn.disabled = true;
  }
}

// ---- Notifications ----
function markAllRead() {
  document.querySelectorAll(".notif-unread").forEach(el => el.classList.remove("notif-unread"));
  toast("Tüm bildirimler okundu olarak işaretlendi.");
}

function respondNotifRequest(btn, accepted) {
  const item = btn.closest(".notif-unread") || btn.closest(".rounded-3");
  const actions = btn.parentElement;
  actions.innerHTML = accepted
    ? `<span class="badge text-bg-success rounded-pill">Kabul edildi</span>`
    : `<span class="badge text-bg-secondary rounded-pill">Reddedildi</span>`;
  if (item) item.classList.remove("notif-unread");
}

// ---- Join / leave a group (groups.html) ----
function toggleGroupMembership(btn) {
  const joined = btn.classList.toggle("joined");
  btn.textContent = joined ? "Ayrıl" : "Katıl";
  btn.classList.toggle("btn-primary", joined);
  btn.classList.toggle("btn-outline-primary", !joined);
  toast(joined ? "Gruba katıldın." : "Gruptan ayrıldın.");
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

// ---- Live search suggestions ----
const directoryUsers = [
  { name: "Ada Lovelace", avatar: "https://i.pravatar.cc/80?img=45" },
  { name: "Lana Rose", avatar: "https://i.pravatar.cc/80?img=47" },
  { name: "Winnie Haley", avatar: "https://i.pravatar.cc/80?img=32" },
  { name: "Daniel Bale", avatar: "https://i.pravatar.cc/80?img=15" },
  { name: "Jane Doe", avatar: "https://i.pravatar.cc/80?img=25" },
  { name: "Tina White", avatar: "https://i.pravatar.cc/80?img=5" },
  { name: "Marcus Lee", avatar: "https://i.pravatar.cc/80?img=51" },
  { name: "Diana Prince", avatar: "https://i.pravatar.cc/80?img=60" },
  { name: "Sophie Turner", avatar: "https://i.pravatar.cc/80?img=41" },
  { name: "Emre Kaya", avatar: "https://i.pravatar.cc/80?img=22" },
  { name: "Elif Aksoy", avatar: "https://i.pravatar.cc/80?img=9" },
  { name: "Bora Yıldız", avatar: "https://i.pravatar.cc/80?img=18" },
  { name: "Jane Smith", avatar: "https://i.pravatar.cc/80?img=44" },
  { name: "Janet Kim", avatar: "https://i.pravatar.cc/80?img=36" },
];

function initSearchSuggestions() {
  document.querySelectorAll(".nav-search-wrap input[name='q']").forEach(input => {
    const wrap = input.closest(".nav-search-wrap");
    let box = wrap.querySelector(".search-suggestions");
    if (!box) {
      box = document.createElement("div");
      box.className = "search-suggestions d-none";
      wrap.appendChild(box);
    }

    input.addEventListener("input", () => {
      const q = input.value.trim().toLowerCase();
      if (!q) {
        box.classList.add("d-none");
        box.innerHTML = "";
        return;
      }
      const matches = directoryUsers.filter(u => u.name.toLowerCase().includes(q)).slice(0, 5);
      if (!matches.length) {
        box.classList.add("d-none");
        box.innerHTML = "";
        return;
      }
      box.innerHTML = matches
        .map(u => `<button type="button" onclick="window.location.href='search.html?q=${encodeURIComponent(u.name)}'">
          <img src="${u.avatar}" class="avatar-xs" alt="">
          <span class="small fw-semibold">${u.name}</span>
        </button>`)
        .join("");
      box.classList.remove("d-none");
    });

    document.addEventListener("click", e => {
      if (!wrap.contains(e.target)) box.classList.add("d-none");
    });
  });
}

document.addEventListener("DOMContentLoaded", initSearchSuggestions);
