// explore.html (Keşfet) — what is popular this week. Which posts and tags make the list, and in what order, is decided
// by the server; this page only draws them. The post cards are post-card.js, "Daha fazla yükle" is pager.js.

requireAuth();

const POPULAR_EMPTY_HTML = `<div class="text-center text-muted small py-4">Bu hafta henüz keşfedilecek bir gönderi yok.</div>`;

const popularPager = createPager({
  list: "popularList",
  url: page => `/api/explore/posts?page=${page}&pageSize=10`,
  render: post => postCardHtml(post),
  shown: post => !!livePostCard(post.id),
  emptyHtml: POPULAR_EMPTY_HTML
});

async function loadTrendingTags() {
  const box = document.getElementById("trendingTags");
  try {
    const tags = await apiFetch("/api/explore/trending-hashtags?take=12");
    box.innerHTML = tags.length
      ? tags.map(t => `
          <a href="search.html?q=${encodeURIComponent("#" + t.name)}" class="btn btn-light border rounded-pill btn-sm">
            #${escapeHtml(t.name)} <span class="badge rounded-pill text-bg-primary ms-1">${t.postCount}</span>
          </a>`).join("")
      : `<span class="text-muted small">Bu hafta henüz etiket kullanılmadı.</span>`;
  } catch (err) {
    box.innerHTML = "";
  }
}

// Something shown here was deleted: say so when the last one is gone.
window.afterLivePostRemoved = () => {
  const list = document.getElementById("popularList");
  if (!list.querySelector("[data-post-id]"))
    list.innerHTML = POPULAR_EMPTY_HTML;
};

document.addEventListener("realtime:reconnected", () => popularPager.reload());

loadTrendingTags();
loadSuggestions(5);
popularPager.reload();
