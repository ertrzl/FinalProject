// Navbar extras shared by every authenticated page: notification bell badge, messages badge and the current user's avatar.
// refreshNotificationBadge / refreshMessageBadge are also called by realtime.js whenever SignalR pushes something.

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
    // Silent — a stale/wrong badge is harmless, no need to interrupt the user for this.
  }
}

function ensureMessageBadge() {
  const link = document.querySelector('a.nav-pill-link[href="messages.html"]');
  if (!link) return null;
  let badge = link.querySelector(".message-badge-count");
  if (!badge) {
    badge = document.createElement("span");
    badge.className = "position-absolute top-0 end-0 badge rounded-pill bg-danger message-badge-count d-none";
    link.appendChild(badge);
  }
  return badge;
}

async function refreshMessageBadge() {
  const badge = ensureMessageBadge();
  if (!badge) return;
  try {
    const count = await apiFetch("/api/messages/unread-count");
    paintBadge(badge, count);
  } catch (err) {
    // Silent, same reasoning as the notification badge.
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

  // The session's avatarUrl is only as fresh as the last login/profile-save on THIS device,
  // so re-fetch the real profile here to catch changes made elsewhere.
  try {
    const freshProfile = await apiFetch(`/api/users/${session.userId}`);
    updateSessionAvatar(freshProfile.avatarUrl);
    document.querySelectorAll(".nav-profile-avatar").forEach(el => {
      el.src = freshProfile.avatarUrl || DEFAULT_AVATAR;
    });
  } catch (err) {
    // Silent — worst case the avatar stays whatever it already was.
  }
})();
