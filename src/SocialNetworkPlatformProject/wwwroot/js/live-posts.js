// Live likes / comments / deletions on post cards, shared by every page that renders them (home, profile, group).
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

  // The pushed comment is the same for everybody (no "liked by me", no "may I delete it?"), so a list that is
  // open is simply loaded again: it comes back with the right buttons for this viewer.
  const list = liveCommentsList(postId);
  if (!list || list.querySelector(`[data-comment-id="${comment.id}"]`)) return;
  loadComments(postId, true);
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

// The post was edited (by its author, here or on another device): fetch it as this viewer sees it and draw it again.
document.addEventListener("realtime:post-updated", async e => {
  if (!livePostCard(e.detail.postId)) return;
  try {
    replacePostCard(await apiFetch(`/api/posts/${e.detail.postId}`));
  } catch (err) {
    // Gone or no longer visible: the delete event (or the next reload) takes care of it.
  }
});

document.addEventListener("realtime:post-deleted", e => {
  livePostCard(e.detail.postId)?.remove();
  window.afterLivePostRemoved?.();
});
