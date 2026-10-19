// notifications.html — real notification feed wired to the backend.

requireAuth();

const NOTIF_ICON = {
  FriendRequestReceived: { badge: "bg-success", icon: "bi-person-plus-fill" },
  FriendRequestAccepted: { badge: "bg-success", icon: "bi-check-lg" },
  PostLiked: { badge: "bg-danger", icon: "bi-heart-fill" },
  CommentAdded: { badge: "bg-primary", icon: "bi-chat-fill" },
  CommentLiked: { badge: "bg-danger", icon: "bi-heart-fill" },
  StoryLiked: { badge: "bg-danger", icon: "bi-heart-fill" },
  StoryMentioned: { badge: "bg-primary", icon: "bi-at" },
  StoryReshared: { badge: "bg-primary", icon: "bi-arrow-repeat" },
  CommentReplied: { badge: "bg-primary", icon: "bi-reply-fill" },
  Mentioned: { badge: "bg-primary", icon: "bi-at" },
  PostShared: { badge: "bg-primary", icon: "bi-arrow-repeat" },
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
  MarketplacePriceDropped: { badge: "bg-success", icon: "bi-graph-down-arrow" },
  EventUpdated: { badge: "bg-info", icon: "bi-calendar-event-fill" },
  EventCancelled: { badge: "bg-danger", icon: "bi-calendar-x-fill" },
  EventInviteReceived: { badge: "bg-info", icon: "bi-calendar-plus-fill" },
  EventAnnouncement: { badge: "bg-warning", icon: "bi-megaphone-fill" },
  EventJoined: { badge: "bg-success", icon: "bi-person-check-fill" },
  EventCommentAdded: { badge: "bg-info", icon: "bi-chat-left-text-fill" },
  EventWaitlistPromoted: { badge: "bg-success", icon: "bi-arrow-up-circle-fill" },
  EventReminder: { badge: "bg-warning", icon: "bi-alarm-fill" },
  GameInviteReceived: { badge: "bg-info", icon: "bi-controller" },
  GameInviteAccepted: { badge: "bg-success", icon: "bi-controller" },
  ContentRemoved: { badge: "bg-danger", icon: "bi-shield-exclamation" }
};

function offerMoney(n) {
  return n == null ? "" : n.toLocaleString(localeTag(), { maximumFractionDigits: 2 }) + " ₺";
}

// The title links to the listing itself; once the listing is deleted there is nothing left to link to.
function offerListing(n) {
  if (!n.listingId) return `<b>${i18n("notif.deletedListing")}</b>`;
  return `<a href="marketplace.html?listing=${n.listingId}" class="fw-bold text-decoration-none" onclick="event.stopPropagation()">${escapeHtml(n.listingTitle || i18n("notif.listing"))}</a>`;
}

// The sentences are templates in the language files (the word order differs between languages): {0} is the person,
// {1}... the group / listing / event and money, already made safe and in bold.
const notifBold = text => `<b>${escapeHtml(text)}</b>`;
// The person in a sentence is a link to their profile (the click does not count as a click on the whole notification).
const notifPerson = n => n.actorId
  ? `<a href="profile.html?id=${n.actorId}" class="fw-bold text-body text-decoration-none" onclick="event.stopPropagation()">${escapeHtml(n.actorName)}</a>`
  : notifBold(n.actorName);
const notifEventName = n => notifBold(n.eventTitle || i18n("notif.untitled"));
const notifGroupName = n => notifBold(n.groupName || "");

const NOTIF_TEXT = {
  FriendRequestReceived: n => i18n("notif.friendRequestReceived", notifPerson(n)),
  FriendRequestAccepted: n => i18n("notif.friendRequestAccepted", notifPerson(n)),
  PostLiked: n => n.reaction && n.reaction !== "Like" && REACTIONS[n.reaction]
    ? i18n("notif.postReacted", notifPerson(n), REACTIONS[n.reaction].emoji)
    : i18n("notif.postLiked", notifPerson(n)),
  PostShared: n => i18n("notif.postShared", notifPerson(n)),
  CommentAdded: n => i18n("notif.commentAdded", notifPerson(n)),
  CommentLiked: n => i18n("notif.commentLiked", notifPerson(n)),
  StoryLiked: n => i18n("notif.storyLiked", notifPerson(n)),
  StoryMentioned: n => i18n("notif.storyMentioned", notifPerson(n)),
  StoryReshared: n => i18n("notif.storyReshared", notifPerson(n)),
  CommentReplied: n => i18n("notif.commentReplied", notifPerson(n)),
  Mentioned: n => i18n(n.commentId ? "notif.mentionedInComment" : "notif.mentionedInPost", notifPerson(n)),
  GroupJoinRequestReceived: n => i18n("notif.groupJoinRequest", notifPerson(n), notifGroupName(n)),
  GroupInviteReceived: n => i18n("notif.groupInvite", notifPerson(n), notifGroupName(n)),
  GroupMemberRemoved: n => i18n("notif.groupRemoved", notifPerson(n), notifGroupName(n)),
  GroupRoleChanged: n => i18n("notif.groupRole", notifPerson(n), notifGroupName(n)),
  GroupJoinRequestApproved: n => i18n("notif.groupApproved", notifPerson(n), notifGroupName(n)),
  GroupJoinRequestRejected: n => i18n("notif.groupRejected", notifPerson(n), notifGroupName(n)),
  MarketplaceOfferReceived: n => i18n("notif.offerReceived", notifPerson(n), offerListing(n), notifBold(offerMoney(n.amount))),
  MarketplaceOfferCountered: n => i18n("notif.offerCountered", notifPerson(n), offerListing(n), notifBold(offerMoney(n.amount))),
  MarketplaceOfferAccepted: n => i18n("notif.offerAccepted", notifPerson(n), offerListing(n), notifBold(offerMoney(n.amount))),
  MarketplaceOfferRejected: n => i18n("notif.offerRejected", notifPerson(n), offerListing(n)),
  MarketplaceOfferWithdrawn: n => i18n("notif.offerWithdrawn", notifPerson(n), offerListing(n)),
  MarketplaceOfferClosed: n => i18n("notif.offerClosed", offerListing(n)),
  MarketplaceRatingReceived: n => i18n("notif.ratingReceived", notifPerson(n), offerListing(n), starsHtml(n.amount)),
  MarketplacePriceDropped: n => i18n("notif.priceDropped", offerListing(n), notifBold(offerMoney(n.previousAmount)), notifBold(offerMoney(n.amount))),
  EventUpdated: n => i18n("notif.eventUpdated", notifPerson(n), notifEventName(n)),
  EventCancelled: n => i18n("notif.eventCancelled", notifPerson(n), notifEventName(n)),
  EventInviteReceived: n => i18n("notif.eventInvite", notifPerson(n), notifEventName(n)),
  EventAnnouncement: n => i18n("notif.eventAnnouncement", notifPerson(n), notifEventName(n)),
  EventJoined: n => i18n("notif.eventJoined", notifPerson(n), notifEventName(n)),
  EventCommentAdded: n => i18n("notif.eventComment", notifPerson(n), notifEventName(n)),
  EventWaitlistPromoted: n => i18n("notif.eventWaitlist", notifEventName(n)),
  GameInviteReceived: n => i18n("notif.gameInviteReceived", notifPerson(n)),
  GameInviteAccepted: n => i18n("notif.gameInviteAccepted", notifPerson(n)),
  // The admins are not named; the title of what was removed comes in the event-title field (the server's "subject").
  ContentRemoved: n => i18n("notif.contentRemoved", notifBold(n.eventTitle || i18n("notif.photoOrPoll"))),
  EventReminder: n => i18n("notif.eventReminder", notifBold(n.eventTitle || i18n("notif.anEvent")), n.eventStartsAt ? `: ${formatEventDate(n.eventStartsAt)}` : "")
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
  } else if (["EventUpdated", "EventInviteReceived", "EventAnnouncement", "EventJoined", "EventCommentAdded", "EventWaitlistPromoted", "EventReminder"].includes(n.type)) {
    actions = `<a href="event.html?id=${n.eventId}" class="btn btn-light border btn-sm rounded-pill flex-shrink-0" onclick="event.stopPropagation()">Etkinliği Gör</a>`;
  } else if (["GameInviteReceived", "GameInviteAccepted"].includes(n.type) && n.gameId) {
    actions = `<a href="game.html?id=${n.gameId}" class="btn btn-light border btn-sm rounded-pill flex-shrink-0" onclick="event.stopPropagation()">Oyuna Git</a>`;
  } else if (["PostLiked", "CommentAdded", "CommentReplied", "CommentLiked", "Mentioned", "PostShared"].includes(n.type) && n.postId) {
    actions = `<a href="post.html?id=${n.postId}" class="btn btn-light border btn-sm rounded-pill flex-shrink-0" onclick="event.stopPropagation()">Gönderiyi Gör</a>`;
  } else if (n.type === "StoryMentioned" || n.type === "StoryReshared") {
    actions = `<a href="home.html" class="btn btn-light border btn-sm rounded-pill flex-shrink-0" onclick="event.stopPropagation()">Hikâyelere Git</a>`;
  } else if (n.type === "MarketplacePriceDropped" && n.listingId) {
    actions = `<a href="marketplace.html?listing=${n.listingId}" class="btn btn-light border btn-sm rounded-pill flex-shrink-0" onclick="event.stopPropagation()">İlana Git</a>`;
  } else if (n.type.startsWith("MarketplaceOffer")) {
    actions = `<a href="marketplace.html?view=offers" class="btn btn-light border btn-sm rounded-pill flex-shrink-0" onclick="event.stopPropagation()">Teklifleri Gör</a>`;
  } else if (n.type === "GroupInviteReceived" && !n.isRead) {
    actions = `<div class="d-flex gap-2 flex-shrink-0">
         <button class="btn btn-primary btn-sm rounded-pill" onclick="respondToGroupInvite('${n.groupId}', '${n.id}', true)">Kabul Et</button>
         <button class="btn btn-light btn-sm rounded-pill" onclick="respondToGroupInvite('${n.groupId}', '${n.id}', false)">Reddet</button>
       </div>`;
  }

  // Opening the page marks everything read, so a request that was not answered right then shows a way to the place where it can be.
  if (!actions) {
    const toFriends = n.type === "FriendRequestReceived";
    const toGroup = n.type === "GroupJoinRequestReceived" && n.groupId;
    const toGroups = n.type === "GroupInviteReceived";
    if (toFriends || toGroup || toGroups) {
      const href = toFriends ? "friends.html" : toGroup ? `group.html?id=${n.groupId}` : "groups.html";
      const label = toFriends ? "Arkadaşlar" : toGroup ? "Gruba Git" : "Gruplar";
      actions = `<a href="${href}" class="btn btn-light border btn-sm rounded-pill flex-shrink-0" onclick="event.stopPropagation()">${label}</a>`;
    }
  }

  return `
    <div class="d-flex align-items-start gap-3 p-3 rounded-3 ${unreadClass} mb-2" data-notification-id="${n.id}" data-created="${n.createdAt}" onclick="markRead('${n.id}')">
      <div class="position-relative flex-shrink-0">
        ${n.type === "ContentRemoved"
          ? `<span class="avatar-sm notif-system-avatar"><i class="bi bi-shield-check"></i></span>`
          : n.actorId
            ? `<a href="profile.html?id=${n.actorId}" onclick="event.stopPropagation()"><img src="${n.actorAvatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt=""></a>`
            : `<img src="${n.actorAvatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt="">`}
        <span class="notif-icon-badge ${iconInfo.badge}"><i class="bi ${iconInfo.icon}"></i></span>
      </div>
      <div class="flex-grow-1">
        <div class="small">${textFn(n)}</div>
        <div class="text-muted small mt-1">${timeAgo(n.createdAt)}</div>
      </div>
      ${actions}
    </div>`;
}

// The inbox design groups the list by day: a small caption ("Bugün", "Dün", a date) above the first notification of each day.
// Drawn again whenever the list changes, so the captions are always right.
function labelNotificationDays() {
  if (getDesign() !== "inbox") return;

  const list = document.getElementById("notificationsList");
  list.querySelectorAll(".day-caption").forEach(el => el.remove());

  const today = new Date().toDateString();
  const yesterday = new Date(Date.now() - 86400000).toDateString();
  let lastDay = null;

  list.querySelectorAll("[data-notification-id]").forEach(row => {
    const day = parseServerDate(row.dataset.created).toDateString();
    if (day === lastDay) return;
    lastDay = day;

    const label = day === today ? "Bugün" : day === yesterday ? "Dün"
      : parseServerDate(row.dataset.created).toLocaleDateString(localeTag(), { day: "numeric", month: "long", year: "numeric" });
    row.insertAdjacentHTML("beforebegin", `<div class="day-caption cap"${day === today || day === yesterday ? "" : " data-no-i18n"}>${label}</div>`);
  });
}

// 30 notifications at a time (pager.js).
const notificationsPager = createPager({
  list: "notificationsList",
  url: page => `/api/notifications?page=${page}&pageSize=30`,
  render: notificationHtml,
  shown: n => !!document.querySelector(`[data-notification-id="${n.id}"]`),
  emptyHtml: `<div class="text-center text-muted small py-4">Henüz bildirim yok.</div>`,
  onLoaded: onNotificationsLoaded
});

// Going to the notifications is reading them: the first time the list is on the screen everything is marked read, so the
// number on the bell is gone at once. The rows keep their "new" look until the page is left, so it is still clear what is new.
let openedMarkedRead = false;
function onNotificationsLoaded() {
  labelNotificationDays();
  if (openedMarkedRead) return;
  openedMarkedRead = true;
  if (!document.querySelector(".notif-unread")) return;
  apiFetch("/api/notifications/read-all", { method: "POST" })
    .then(() => window.refreshNotificationBadge?.())
    .catch(() => { /* a background convenience; the single clicks still mark rows read */ });
}

function loadNotifications() {
  return notificationsPager.reload();
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
  labelNotificationDays();
  markRead(n.id); // the page is open in front of the person: it is read the moment it arrives
});

document.addEventListener("realtime:reconnected", loadNotifications);

loadNotifications();
