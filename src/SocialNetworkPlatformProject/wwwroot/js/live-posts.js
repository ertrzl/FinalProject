function livePostCard(postId) {
  return document.querySelector(`.card[data-post-id="${postId}"]`);
}

function liveCommentsList(postId) {
  const container = document.getElementById(`comments-${postId}`);
  return container && container.dataset.loaded ? container.querySelector(".comments-list") : null;
}

document.addEventListener("realtime:post-likes", e => {
  const { postId, likeCount, reactions } = e.detail;
  const card = livePostCard(postId);
  if (card) applyReactionResult(card, { likeCount, reactions }, false);
});

document.addEventListener("realtime:poll-voted", async e => {
  if (!livePostCard(e.detail.postId)) return;
  try {
    replacePoll(await apiFetch(`/api/posts/${e.detail.postId}`));
  } catch (err) {
  }
});

document.addEventListener("realtime:comment-added", e => {
  const { postId, commentCount, comment } = e.detail;
  const card = livePostCard(postId);
  if (!card) return;
  card.querySelector(".comment-count-label").textContent = commentCount;

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

document.addEventListener("realtime:post-updated", async e => {
  if (!livePostCard(e.detail.postId)) return;
  try {
    replacePostCard(await apiFetch(`/api/posts/${e.detail.postId}`));
  } catch (err) {
  }
});

document.addEventListener("realtime:post-deleted", e => {
  livePostCard(e.detail.postId)?.remove();
  window.afterLivePostRemoved?.();
});
