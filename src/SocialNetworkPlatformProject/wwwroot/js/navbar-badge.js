// Navbar bell badge — every authenticated page includes this to show the real unread notification count.

(async function () {
  const session = getSession();
  if (!session) return;

  document.querySelectorAll(".nav-profile-avatar").forEach(el => {
    el.src = session.avatarUrl || DEFAULT_AVATAR;
  });

  // The session's avatarUrl is only as fresh as the last login/profile-save on THIS device,
  // so re-fetch the real profile here to catch changes made elsewhere (or before this script existed).
  try {
    const freshProfile = await apiFetch(`/api/users/${session.userId}`);
    updateSessionAvatar(freshProfile.avatarUrl);
    document.querySelectorAll(".nav-profile-avatar").forEach(el => {
      el.src = freshProfile.avatarUrl || DEFAULT_AVATAR;
    });
  } catch (err) {
    // Silent — worst case the avatar stays whatever it already was.
  }

  try {
    const count = await apiFetch("/api/notifications/unread-count");
    document.querySelectorAll(".badge-count").forEach(el => {
      if (count > 0) {
        el.textContent = count > 99 ? "99+" : count;
        el.classList.remove("d-none");
      } else {
        el.classList.add("d-none");
      }
    });
  } catch (err) {
    // Silent — a stale/wrong badge is harmless, no need to interrupt the user for this.
  }
})();
