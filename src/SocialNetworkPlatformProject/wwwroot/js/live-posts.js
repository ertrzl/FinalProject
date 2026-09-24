// Live likes / comments / deletions on post cards, shared by every page that renders them (home, profile).
// The server sends absolute counts, so applying the same event twice (or after our own REST update) is harmless.

function livePostCard(postId) {
  return document.querySelector(`.card[data-post-id="${postId}"]`);
}

function liveCommentsList(postId) {
  const container = document.getElementById(`comments-${postId}`);
  return container && container.dataset.loaded ? container.querySelector(".comments-list") : null;
}

document.addEventListener("realtime:post-likes", e => {
  const { postId, likeCount } = e.detail;
  const card = livePostCard(postId);
  if (!card) return;
  card.querySelector(".like-count-label").textContent = likeCount;
  const counter = card.querySelector(".like-count");
  counter.textContent = likeCount;
  counter.dataset.count = likeCount;
});

document.addEventListener("realtime:comment-added", e => {
  const { postId, commentCount, comment } = e.detail;
  const card = livePostCard(postId);
  if (!card) return;
  card.querySelector(".comment-count-label").textContent = commentCount;

  const list = liveCommentsList(postId);
  if (!list || list.querySelector(`[data-comment-id="${comment.id}"]`)) return;
  if (list.dataset.empty) { list.innerHTML = ""; delete list.dataset.empty; }
  list.insertAdjacentHTML("beforeend", commentHtml(comment));
});

document.addEventListener("realtime:comment-deleted", e => {
  const { postId, commentIds, commentCount } = e.detail;
  const card = livePostCard(postId);
  if (!card) return;
  card.querySelector(".comment-count-label").textContent = commentCount;

  const list = liveCommentsList(postId);
  if (list) commentIds.forEach(id => list.querySelector(`[data-comment-id="${id}"]`)?.remove());
});

document.addEventListener("realtime:comment-likes", e => {
  const { postId, commentId, likeCount } = e.detail;
  const label = liveCommentsList(postId)?.querySelector(`[data-comment-id="${commentId}"] .comment-like-count`);
  if (label) label.textContent = likeCount > 0 ? ` (${likeCount})` : "";
});

document.addEventListener("realtime:post-deleted", e => {
  livePostCard(e.detail.postId)?.remove();
  window.afterLivePostRemoved?.();
});
