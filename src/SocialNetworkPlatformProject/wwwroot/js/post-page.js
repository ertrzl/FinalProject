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

    document.getElementById(`comments-${pagePostId}`).classList.add("show");
    loadComments(pagePostId);
  } catch (err) {
    postContainer.innerHTML = POST_MISSING_HTML;
  }
}

window.afterLivePostRemoved = () => {
  postContainer.innerHTML = POST_MISSING_HTML;
};

document.addEventListener("realtime:reconnected", loadPost);

loadPost();
