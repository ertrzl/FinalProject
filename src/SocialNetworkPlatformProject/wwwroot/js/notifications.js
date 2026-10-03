// notifications.html — real notification feed wired to the backend.

requireAuth();

const NOTIF_ICON = {
  FriendRequestReceived: { badge: "bg-success", icon: "bi-person-plus-fill" },
  FriendRequestAccepted: { badge: "bg-success", icon: "bi-check-lg" },
  PostLiked: { badge: "bg-danger", icon: "bi-heart-fill" },
  CommentAdded: { badge: "bg-primary", icon: "bi-chat-fill" },
  CommentLiked: { badge: "bg-danger", icon: "bi-heart-fill" },
  GroupJoinRequestReceived: { badge: "bg-info", icon: "bi-people-fill" },
  GroupInviteReceived: { badge: "bg-info", icon: "bi-envelope-fill" },
  GroupMemberRemoved: { badge: "bg-danger", icon: "bi-person-dash-fill" },
  GroupRoleChanged: { badge: "bg-warning", icon: "bi-award-fill" },
  GroupJoinRequestApproved: { badge: "bg-success", icon: "bi-check-circle-fill" },
  GroupJoinRequestRejected: { badge: "bg-secondary", icon: "bi-x-circle-fill" },
  MarketplaceOfferReceived: { badge: "bg-warning", icon: "bi-tag-fill" },
  MarketplaceOfferCountered: { badge: "bg-info", icon: "bi-arrow-left-right" },
  MarketplaceOfferAccepted: { badge: "bg-success", icon: "bi-check-lg" },
  MarketplaceOfferRejected: { badge: "bg-danger", icon: "bi-x-lg" },
  MarketplaceOfferWithdrawn: { badge: "bg-secondary", icon: "bi-arrow-counterclockwise" },
  MarketplaceOfferClosed: { badge: "bg-secondary", icon: "bi-lock-fill" },
  MarketplaceRatingReceived: { badge: "bg-warning", icon: "bi-star-fill" },
  EventUpdated: { badge: "bg-info", icon: "bi-calendar-event-fill" },
  EventCancelled: { badge: "bg-danger", icon: "bi-calendar-x-fill" },
  EventInviteReceived: { badge: "bg-info", icon: "bi-calendar-plus-fill" },
  EventAnnouncement: { badge: "bg-warning", icon: "bi-megaphone-fill" }
};

function offerMoney(n) {
  return n == null ? "" : n.toLocaleString("tr-TR", { maximumFractionDigits: 2 }) + " ₺";
}

// The title links to the listing itself; once the listing is deleted there is nothing left to link to.
function offerListing(n) {
  if (!n.listingId) return `<b>silinmiş bir</b>`;
  return `<a href="marketplace.html?listing=${n.listingId}" class="fw-bold text-decoration-none" onclick="event.stopPropagation()">${escapeHtml(n.listingTitle || "ilan")}</a>`;
}

const NOTIF_TEXT = {
  FriendRequestReceived: n => `<b>${escapeHtml(n.actorName)}</b> sana arkadaşlık isteği gönderdi.`,
  FriendRequestAccepted: n => `<b>${escapeHtml(n.actorName)}</b> arkadaşlık isteğini kabul etti.`,
  PostLiked: n => `<b>${escapeHtml(n.actorName)}</b> gönderini beğendi.`,
  CommentAdded: n => `<b>${escapeHtml(n.actorName)}</b> gönderine yorum yaptı.`,
  CommentLiked: n => `<b>${escapeHtml(n.actorName)}</b> yorumunu beğendi.`,
  GroupJoinRequestReceived: n => `<b>${escapeHtml(n.actorName)}</b> <b>${escapeHtml(n.groupName || "")}</b> grubuna katılmak istiyor.`,
  GroupInviteReceived: n => `<b>${escapeHtml(n.actorName)}</b> seni <b>${escapeHtml(n.groupName || "")}</b> grubuna davet etti.`,
  GroupMemberRemoved: n => `<b>${escapeHtml(n.actorName)}</b> seni <b>${escapeHtml(n.groupName || "")}</b> grubundan çıkardı.`,
  GroupRoleChanged: n => `<b>${escapeHtml(n.actorName)}</b> <b>${escapeHtml(n.groupName || "")}</b> grubundaki rolünü değiştirdi.`,
  GroupJoinRequestApproved: n => `<b>${escapeHtml(n.actorName)}</b> <b>${escapeHtml(n.groupName || "")}</b> grubuna katılma isteğini onayladı.`,
  GroupJoinRequestRejected: n => `<b>${escapeHtml(n.actorName)}</b> <b>${escapeHtml(n.groupName || "")}</b> grubuna katılma isteğini reddetti.`,
  MarketplaceOfferReceived: n => `<b>${escapeHtml(n.actorName)}</b>, ${offerListing(n)} ilanına <b>${offerMoney(n.amount)}</b> teklif verdi.`,
  MarketplaceOfferCountered: n => `<b>${escapeHtml(n.actorName)}</b>, ${offerListing(n)} ilanında <b>${offerMoney(n.amount)}</b> karşı teklif yaptı.`,
  MarketplaceOfferAccepted: n => `<b>${escapeHtml(n.actorName)}</b>, ${offerListing(n)} ilanındaki <b>${offerMoney(n.amount)}</b> teklifi kabul etti.`,
  MarketplaceOfferRejected: n => `<b>${escapeHtml(n.actorName)}</b>, ${offerListing(n)} ilanındaki teklifini reddetti.`,
  MarketplaceOfferWithdrawn: n => `<b>${escapeHtml(n.actorName)}</b>, ${offerListing(n)} ilanındaki teklifini geri çekti.`,
  MarketplaceOfferClosed: n => `${offerListing(n)} ilanı satıldığı için teklifin kapandı.`,
  MarketplaceRatingReceived: n => `<b>${escapeHtml(n.actorName)}</b>, ${offerListing(n)} satışı için seni puanladı: ${starsHtml(n.amount)}`,
  EventUpdated: n => `<b>${escapeHtml(n.actorName)}</b>, katıldığın <b>${escapeHtml(n.eventTitle || "bir")}</b> etkinliğini güncelledi.`,
  EventCancelled: n => `<b>${escapeHtml(n.actorName)}</b>, katılacağın <b>${escapeHtml(n.eventTitle || "bir")}</b> etkinliğini iptal etti.`,
  EventInviteReceived: n => `<b>${escapeHtml(n.actorName)}</b> seni <b>${escapeHtml(n.eventTitle || "bir")}</b> etkinliğine davet etti.`,
  EventAnnouncement: n => `<b>${escapeHtml(n.actorName)}</b>, <b>${escapeHtml(n.eventTitle || "bir")}</b> etkinliğinde yeni bir duyuru yaptı.`
};

function notificationHtml(n) {
  const iconInfo = NOTIF_ICON[n.type] || { badge: "bg-secondary", icon: "bi-bell-fill" };
  const textFn = NOTIF_TEXT[n.type] || (x => escapeHtml(x.actorName));
  const unreadClass = n.isRead ? "" : "notif-unread";

  let actions = "";
  if (n.type === "FriendRequestReceived" && !n.isRead) {
    actions = `<div class="d-flex gap-2 flex-shrink-0">
         <button class="btn btn-primary btn-sm rounded-pill" onclick="respondFromNotification('${n.friendRequestId}', '${n.id}', true)">Kabul Et</button>
         <button class="btn btn-light btn-sm rounded-pill" onclick="respondFromNotification('${n.friendRequestId}', '${n.id}', false)">Reddet</button>
       </div>`;
  } else if (n.type === "GroupJoinRequestReceived" && !n.isRead) {
    actions = `<div class="d-flex gap-2 flex-shrink-0">
         <button class="btn btn-primary btn-sm rounded-pill" onclick="respondToGroupRequest('${n.groupId}', '${n.actorId}', '${n.id}', true)">Onayla</button>
         <button class="btn btn-light btn-sm rounded-pill" onclick="respondToGroupRequest('${n.groupId}', '${n.actorId}', '${n.id}', false)">Reddet</button>
       </div>`;
  } else if (n.type === "MarketplaceRatingReceived") {
    actions = `<a href="marketplace.html?sellerReviews=${getSession().userId}" class="btn btn-light border btn-sm rounded-pill flex-shrink-0" onclick="event.stopPropagation()">Değerlendirmeleri Gör</a>`;
  } else if (["EventUpdated", "EventInviteReceived", "EventAnnouncement"].includes(n.type)) {
    actions = `<a href="event.html?id=${n.eventId}" class="btn btn-light border btn-sm rounded-pill flex-shrink-0" onclick="event.stopPropagation()">Etkinliği Gör</a>`;
  } else if (n.type.startsWith("MarketplaceOffer")) {
    actions = `<a href="marketplace.html?view=offers" class="btn btn-light border btn-sm rounded-pill flex-shrink-0" onclick="event.stopPropagation()">Teklifleri Gör</a>`;
  } else if (n.type === "GroupInviteReceived" && !n.isRead) {
    actions = `<div class="d-flex gap-2 flex-shrink-0">
         <button class="btn btn-primary btn-sm rounded-pill" onclick="respondToGroupInvite('${n.groupId}', '${n.id}', true)">Kabul Et</button>
         <button class="btn btn-light btn-sm rounded-pill" onclick="respondToGroupInvite('${n.groupId}', '${n.id}', false)">Reddet</button>
       </div>`;
  }

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

async function respondToGroupRequest(groupId, requesterId, notificationId, approve) {
  event.stopPropagation();
  try {
    await apiFetch(`/api/groups/${groupId}/join-requests/${requesterId}/${approve ? "approve" : "reject"}`, { method: "POST" });
    toast(approve ? "İstek onaylandı." : "İstek reddedildi.");
    await markRead(notificationId);
    await loadNotifications();
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

async function respondToGroupInvite(groupId, notificationId, accept) {
  event.stopPropagation();
  try {
    await apiFetch(`/api/groups/${groupId}/invites/${accept ? "accept" : "decline"}`, { method: "POST" });
    toast(accept ? "Davet kabul edildi." : "Davet reddedildi.");
    await markRead(notificationId);
    await loadNotifications();
  } catch (err) {
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
