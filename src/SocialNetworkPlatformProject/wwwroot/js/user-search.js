function userSearchRowHtml(user, actionHtml) {
  return `
    <div class="d-flex align-items-center gap-2 py-2 border-bottom" data-invite-candidate="${user.id}">
      <a href="profile.html?id=${user.id}"><img src="${user.avatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt=""></a>
      <div class="flex-grow-1">
        <div class="fw-bold">${escapeHtml(user.fullName)}</div>
        <div class="text-muted small">@${escapeHtml(user.userName)}</div>
      </div>
      ${actionHtml}
    </div>`;
}

function createUserSearch({ resultsId, actionFor }) {
  let debounce = null;

  return function (term) {
    clearTimeout(debounce);
    const list = document.getElementById(resultsId);
    const trimmed = term.trim();
    if (!trimmed) {
      list.innerHTML = "";
      return;
    }

    debounce = setTimeout(async () => {
      try {
        const result = await apiFetch(`/api/users/search?term=${encodeURIComponent(trimmed)}&page=1&pageSize=10`);
        const myId = getSession()?.userId;
        const candidates = result.items.filter(u => u.id !== myId);
        list.innerHTML = candidates.length
          ? candidates.map(u => userSearchRowHtml(u, actionFor(u))).join("")
          : `<div class="text-muted small text-center py-2">Kullanıcı bulunamadı.</div>`;
      } catch (err) {
        list.innerHTML = `<div class="text-danger small">${escapeHtml(err.message)}</div>`;
      }
    }, 350);
  };
}
