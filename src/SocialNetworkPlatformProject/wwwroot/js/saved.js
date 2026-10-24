requireAuth();

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

window.afterLivePostRemoved = () => {
  if (!document.getElementById("savedList").querySelector("[data-post-id]")) {
    document.getElementById("emptyState").classList.remove("d-none");
  }
};

document.addEventListener("realtime:reconnected", loadSaved);

loadSaved();
