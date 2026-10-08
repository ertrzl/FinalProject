// post.html — one post on its own page, with its comments open. This is where "Gönderiyi Gör" in a notification and
// the link a post's "Paylaş" button copies lead. The card itself is the shared one in post-card.js, so likes,
// comments, replies, editing and live updates all work as they do everywhere else.

requireAuth();

const pagePostId = new URLSearchParams(window.location.search).get("id");
const postContainer = document.getElementById("postContainer");

const POST_MISSING_HTML = `
  <div class="text-center text-muted py-5">
    <i class="bi bi-file-earmark-x" style="font-size: 42px;"></i>
    <p class="mt-2 mb-0">Bu gönderi bulunamadı. Silinmiş olabilir ya da görmeye iznin yok.</p>
  </div>`;

async function loadPost() {
  if (!pagePostId) {
    postContainer.innerHTML = POST_MISSING_HTML;
    return;
  }

  try {
    postContainer.innerHTML = postCardHtml(await apiFetch(`/api/posts/${pagePostId}`));

    // Open the comments right away: reading them is why people come to this page.
    document.getElementById(`comments-${pagePostId}`).classList.add("show");
    loadComments(pagePostId);
  } catch (err) {
    // The server answers 404 both for a post that is gone and for one the viewer may not see.
    postContainer.innerHTML = POST_MISSING_HTML;
  }
}

// The post was deleted (here or by someone else while this page was open).
window.afterLivePostRemoved = () => {
  postContainer.innerHTML = POST_MISSING_HTML;
};

document.addEventListener("realtime:reconnected", loadPost);

loadPost();
