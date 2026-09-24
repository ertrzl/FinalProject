// notifications.html — real notification feed wired to the backend.

requireAuth();

const NOTIF_ICON = {
  FriendRequestReceived: { badge: "bg-success", icon: "bi-person-plus-fill" },
  FriendRequestAccepted: { badge: "bg-success", icon: "bi-check-lg" },
  PostLiked: { badge: "bg-danger", icon: "bi-heart-fill" },
  CommentAdded: { badge: "bg-primary", icon: "bi-chat-fill" },
  CommentLiked: { badge: "bg-danger", icon: "bi-heart-fill" }
};

const NOTIF_TEXT = {
  FriendRequestReceived: n => `<b>${escapeHtml(n.actorName)}</b> sana arkadaşlık isteği gönderdi.`,
  FriendRequestAccepted: n => `<b>${escapeHtml(n.actorName)}</b> arkadaşlık isteğini kabul etti.`,
  PostLiked: n => `<b>${escapeHtml(n.actorName)}</b> gönderini beğendi.`,
  CommentAdded: n => `<b>${escapeHtml(n.actorName)}</b> gönderine yorum yaptı.`,
  CommentLiked: n => `<b>${escapeHtml(n.actorName)}</b> yorumunu beğendi.`
};

function notificationHtml(n) {
  const iconInfo = NOTIF_ICON[n.type] || { badge: "bg-secondary", icon: "bi-bell-fill" };
  const textFn = NOTIF_TEXT[n.type] || (x => escapeHtml(x.actorName));
  const unreadClass = n.isRead ? "" : "notif-unread";

  const actions = n.type === "FriendRequestReceived" && !n.isRead
    ? `<div class="d-flex gap-2 flex-shrink-0">
         <button class="btn btn-primary btn-sm rounded-pill" onclick="respondFromNotification('${n.friendRequestId}', '${n.id}', true)">Kabul Et</button>
         <button class="btn btn-light btn-sm rounded-pill" onclick="respondFromNotification('${n.friendRequestId}', '${n.id}', false)">Reddet</button>
       </div>`
    : "";

  return `
    <div class="d-flex align-items-start gap-3 p-3 rounded-3 ${unreadClass} mb-2" data-notification-id="${n.id}" onclick="markRead('${n.id}')">
      <div class="position-relative flex-shrink-0">
        <img src="${n.actorAvatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt="">
        <span class="notif-icon-badge ${iconInfo.badge}"><i class="bi ${iconInfo.icon}"></i></span>
      </div>
      <div class="flex-grow-1">
        <div class="small">${textFn(n)}</div>
        <div class="text-muted small mt-1">${timeAgo(n.createdAt)}</div>
      </div>
      ${actions}
    </div>`;
}

async function loadNotifications() {
  const list = document.getElementById("notificationsList");
  try {
    const result = await apiFetch("/api/notifications?page=1&pageSize=30");
    list.innerHTML = result.items.length
      ? result.items.map(notificationHtml).join("")
      : `<div class="text-center text-muted small py-4">Henüz bildirim yok.</div>`;
  } catch (err) {
    list.innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
  }
}

async function markRead(id) {
  try {
    await apiFetch(`/api/notifications/${id}/read`, { method: "POST" });
    document.querySelector(`[data-notification-id="${id}"]`)?.classList.remove("notif-unread");
    window.refreshNotificationBadge?.();
  } catch (err) {
    // Silent: marking as read is a background convenience, not worth interrupting the user for.
  }
}

async function markAllRead() {
  try {
    await apiFetch("/api/notifications/read-all", { method: "POST" });
    document.querySelectorAll(".notif-unread").forEach(el => el.classList.remove("notif-unread"));
    window.refreshNotificationBadge?.();
    toast("Tüm bildirimler okundu olarak işaretlendi.");
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

async function respondFromNotification(requestId, notificationId, accepted) {
  event.stopPropagation();
  try {
    await apiFetch(`/api/friends/requests/${requestId}/${accepted ? "accept" : "decline"}`, { method: "POST" });
    toast(accepted ? "Arkadaşlık isteği kabul edildi." : "İstek reddedildi.");
    await markRead(notificationId);
    await loadNotifications();
  } catch (err) {
    // Already answered elsewhere (e.g. from friends.html) — the notification just hasn't caught up yet.
    // Mark it read so the stale Accept/Decline buttons don't linger.
    if (err.message && err.message.includes("already been answered")) {
      await markRead(notificationId);
      await loadNotifications();
      return;
    }
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

document.addEventListener("realtime:notification", e => {
  const n = e.detail;
  const list = document.getElementById("notificationsList");
  if (list.querySelector(`[data-notification-id="${n.id}"]`)) return;
  if (!list.querySelector("[data-notification-id]")) list.innerHTML = "";
  list.insertAdjacentHTML("afterbegin", notificationHtml(n));
});

document.addEventListener("realtime:reconnected", loadNotifications);

loadNotifications();
