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

function filterFeed(tab) {
  document.querySelectorAll("#feedFilter .nav-link").forEach(el => el.classList.toggle("active", el === tab));
  document.getElementById("feedList").dataset.filter = tab.dataset.filter;
}

async function publishPost(btn) {
  const composer = btn.closest(".composer");
  const textarea = document.getElementById("composerText");
  const privacy = document.getElementById("composerPrivacy").value;
  const text = textarea.value.trim();
  const files = getComposerFiles();

  if (!text && files.length === 0) {
    textarea.classList.add("is-invalid");
    setTimeout(() => textarea.classList.remove("is-invalid"), 1200);
    return;
  }

  btn.disabled = true;
  try {
    const formData = new FormData();
    if (text) formData.append("text", text);
    appendComposerAttachments(formData);
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
