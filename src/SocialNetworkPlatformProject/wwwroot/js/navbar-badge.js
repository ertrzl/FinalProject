// Navbar bell badge — every authenticated page includes this to show the real unread notification count.

(async function () {
  const session = getSession();
  if (!session) return;

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
