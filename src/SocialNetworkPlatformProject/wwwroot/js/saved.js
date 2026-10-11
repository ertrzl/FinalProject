// saved.html — the saved posts, drawn with the same post card as everywhere else (post-card.js); its menu has the
// "Kaydı Kaldır" item. A post that is un-saved here leaves the list (afterPostUnsaved below).

requireAuth();

// 30 saved posts at a time (pager.js); with nothing saved the page shows its own "empty" picture instead.
const savedPager = createPager({
  list: "savedList",
  url: page => `/api/posts/saved?page=${page}&pageSize=30`,
  render: post => postCardHtml(post),
  shown: post => !!livePostCard(post.id),
  onLoaded: (items, firstPage) => {
    if (firstPage) document.getElementById("emptyState").classList.toggle("d-none", items.length > 0);
  }
});

function loadSaved() {
  return savedPager.reload();
}

window.afterPostUnsaved = card => {
  card.remove();
  if (!document.getElementById("savedList").querySelector("[data-post-id]")) {
    document.getElementById("emptyState").classList.remove("d-none");
  }
};

// A saved post was deleted (here or by its author): it leaves the list; the "empty" picture shows with the last one.
window.afterLivePostRemoved = () => {
  if (!document.getElementById("savedList").querySelector("[data-post-id]")) {
    document.getElementById("emptyState").classList.remove("d-none");
  }
};

document.addEventListener("realtime:reconnected", loadSaved);

loadSaved();
