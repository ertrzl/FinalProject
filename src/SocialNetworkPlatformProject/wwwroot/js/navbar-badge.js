function paintBadge(el, count) {
  if (count > 0) {
    el.textContent = count > 99 ? "99+" : count;
    el.classList.remove("d-none");
  } else {
    el.classList.add("d-none");
  }
}

async function refreshNotificationBadge() {
  try {
    const count = await apiFetch("/api/notifications/unread-count");
    document.querySelectorAll(".badge-count").forEach(el => paintBadge(el, count));
  } catch (err) {
  }
}

async function refreshMessageBadge() {
  try {
    const count = await apiFetch("/api/messages/unread-count");
    document.querySelectorAll(".message-badge-count").forEach(el => paintBadge(el, count));
  } catch (err) {
  }
}

(async function () {
  const session = getSession();
  if (!session) return;

  document.querySelectorAll(".nav-profile-avatar").forEach(el => {
    el.src = session.avatarUrl || DEFAULT_AVATAR;
  });

  refreshNotificationBadge();
  refreshMessageBadge();

  try {
    const freshProfile = await apiFetch(`/api/users/${session.userId}`);
    updateSessionAvatar(freshProfile.avatarUrl);
    document.querySelectorAll(".nav-profile-avatar").forEach(el => {
      el.src = freshProfile.avatarUrl || DEFAULT_AVATAR;
    });
  } catch (err) {
  }
})();
