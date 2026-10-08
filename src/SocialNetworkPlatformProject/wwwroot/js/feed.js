// home.html — feed, stories and friend suggestions, all wired to the backend. The post cards, their comments and
// the like / edit / delete actions are in post-card.js; the "Daha fazla yükle" button is pager.js.

const session = requireAuth();

const composerFirstName = session.fullName.split(" ")[0];
document.getElementById("composerText").placeholder = `Aklında ne var, ${composerFirstName}?`;
document.getElementById("composerAvatar").src = session.avatarUrl || DEFAULT_AVATAR;

const FEED_EMPTY_HTML = `<div class="text-center text-muted small py-4">Henüz gönderi yok. İlk paylaşımı sen yap!</div>`;

const feedPager = createPager({
  list: "feedList",
  url: page => `/api/posts/feed?page=${page}&pageSize=10`,
  render: post => postCardHtml(post),
  shown: post => !!livePostCard(post.id),
  emptyHtml: FEED_EMPTY_HTML
});

function loadFeed() {
  return feedPager.reload();
}

async function publishPost(btn) {
  const composer = btn.closest(".composer");
  const textarea = document.getElementById("composerText");
  const fileInput = document.getElementById("composerFile");
  const privacy = document.getElementById("composerPrivacy").value;
  const text = textarea.value.trim();
  const file = fileInput.files[0];

  if (!text && !file) {
    textarea.classList.add("is-invalid");
    setTimeout(() => textarea.classList.remove("is-invalid"), 1200);
    return;
  }

  btn.disabled = true;
  try {
    const formData = new FormData();
    if (text) formData.append("text", text);
    if (file) formData.append("media", file);
    formData.append("privacy", privacy);

    await apiFetchForm("/api/posts", { method: "POST", body: formData });

    textarea.value = "";
    clearComposerImage();
    toast("Gönderi paylaşıldı!");
    await loadFeed();
  } catch (err) {
    toast(err.message || "Gönderi paylaşılamadı.");
  } finally {
    btn.disabled = false;
  }
}

function suggestionCardHtml(user) {
  const mutualLabel = user.mutualFriendsCount > 0 ? `${user.mutualFriendsCount} ortak arkadaş` : "Yeni üye";
  return `
    <div class="d-flex align-items-center gap-2 mb-3" data-user-id="${user.id}">
      <img src="${user.avatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt="">
      <div class="flex-grow-1">
        <div class="fw-bold small"><a href="profile.html?id=${user.id}" class="text-dark text-decoration-none">${escapeHtml(user.fullName)}</a></div>
        <div class="text-muted small">${mutualLabel}</div>
      </div>
      <button class="btn btn-outline-primary btn-sm rounded-pill" onclick="sendSuggestionRequest('${user.id}', this)">Ekle</button>
    </div>`;
}

async function loadSuggestions() {
  const list = document.getElementById("suggestionsList");
  try {
    const suggestions = await apiFetch("/api/friends/suggestions?take=3");
    list.innerHTML = suggestions.length
      ? suggestions.map(suggestionCardHtml).join("")
      : `<div class="text-muted small">Şu an önerecek kimse yok.</div>`;
  } catch (err) {
    list.innerHTML = "";
  }
}

async function sendSuggestionRequest(userId, btn) {
  btn.disabled = true;
  try {
    await apiFetch("/api/friends/requests", { method: "POST", body: { receiverId: userId } });
    btn.textContent = "İstek Gönderildi";
    btn.classList.remove("btn-outline-primary");
    btn.classList.add("btn-secondary");
    toast("Arkadaşlık isteği gönderildi.");
  } catch (err) {
    btn.disabled = false;
    toast(err.message || "İstek gönderilemedi.");
  }
}

// A friend (or one of our other tabs) posted: fetch it as we would see it and put it on top.
document.addEventListener("realtime:post-created", async e => {
  const { postId } = e.detail;
  if (livePostCard(postId)) return;
  try {
    const post = await apiFetch(`/api/posts/${postId}`);
    if (livePostCard(postId)) return;
    const list = document.getElementById("feedList");
    if (!list.querySelector("[data-post-id]")) list.innerHTML = "";
    list.insertAdjacentHTML("afterbegin", postCardHtml(post));
  } catch (err) {
    // Not visible to us after all, or already gone: nothing to show.
  }
});

window.afterLivePostRemoved = () => {
  const list = document.getElementById("feedList");
  if (!list.querySelector("[data-post-id]"))
    list.innerHTML = FEED_EMPTY_HTML;
};

document.addEventListener("realtime:reconnected", loadFeed);

loadFeed();
loadSuggestions();
