// event.html — one event: details, going/interested buttons, attendee list, share link, organizer tools.

const session = requireAuth();

const eventId = new URLSearchParams(window.location.search).get("id");
let currentEvent = null;

function eventLink(id) {
  return `${window.location.origin}/event.html?id=${id}`;
}

// escapeHtml leaves quotes alone; attribute values need them escaped too.
function escapeAttr(text) {
  return escapeHtml(text).replace(/"/g, "&quot;").replace(/'/g, "&#39;");
}

// The pre-filled Google Calendar address comes from the server (it knows what this viewer may see, e.g. the online link).
async function loadGoogleCalendarLink() {
  const link = document.getElementById("googleCalendarLink");
  if (!link) return;
  try {
    const result = await apiFetch(`/api/events/${eventId}/calendar/google`);
    link.href = result.url;
    link.classList.remove("disabled");
  } catch (err) {
    link.remove();
  }
}

async function downloadCalendarFile() {
  try {
    await downloadApiFile(`/api/events/${eventId}/calendar`, "etkinlik.ics");
  } catch (err) {
    toast(err.message || "Takvim dosyası indirilemedi.");
  }
}

function showEventError(message) {
  document.getElementById("eventLoading").classList.add("d-none");
  document.getElementById("eventPage").classList.add("d-none");
  const box = document.getElementById("eventPageError");
  box.textContent = message;
  box.classList.remove("d-none");
}

function renderEvent() {
  const ev = currentEvent;
  document.title = `${ev.title} | SocialNet`;
  document.getElementById("eventLoading").classList.add("d-none");
  document.getElementById("eventPage").classList.remove("d-none");

  document.getElementById("evCover").src = ev.coverImageUrl || NO_PHOTO;
  document.getElementById("evDate").innerHTML = `<i class="bi bi-calendar3 me-1"></i>${formatEventRange(ev.startsAt, ev.endsAt)} ${eventStateBadgeHtml(ev)}`;
  document.getElementById("evTitle").textContent = ev.title;
  // Online events keep their link private: only the organizer and people who are going get it (see the actions below).
  document.getElementById("evLocation").innerHTML = eventPlaceHtml(ev)
    + (ev.isOnline && !ev.onlineLink ? ` <span class="small">· bağlantıyı "Katılıyorum" diyenler görür</span>` : "");

  const limitInfo = ev.capacity != null
    ? `<div class="progress mt-2" style="height:6px; max-width:320px;"><div class="progress-bar ${ev.isFull ? "bg-secondary" : ""}" style="width:${Math.min(100, Math.round(ev.goingCount * 100 / ev.capacity))}%"></div></div>
       <div class="mt-1">${ev.isFull ? "Etkinlik dolu" : `${ev.spotsLeft} yer kaldı`}${ev.waitlistCount > 0 ? ` · ${eventWaitlistText(ev)}` : ""}</div>`
    : "";
  document.getElementById("evCounts").innerHTML = `${eventAttendanceText(ev)} · ${ev.interestedCount} ilgileniyor${limitInfo}`;

  document.getElementById("evOrganizer").innerHTML = ev.createdByName
    ? `<img src="${ev.createdByAvatarUrl || DEFAULT_AVATAR}" class="rounded-circle" style="width:32px;height:32px;object-fit:cover;" alt="">
       <span class="small text-muted">Düzenleyen: <a href="profile.html?id=${ev.createdByUserId}" class="fw-semibold text-body text-decoration-none">${escapeHtml(ev.createdByName)}</a></span>`
    : "";

  const aboutCard = document.getElementById("evAboutCard");
  aboutCard.classList.toggle("d-none", !ev.description);
  document.getElementById("evDescription").textContent = ev.description || "";

  const groupBox = document.getElementById("evGroup");
  groupBox.classList.toggle("d-none", !ev.groupName);
  if (ev.groupName) {
    groupBox.innerHTML = `<i class="bi bi-people-fill me-1"></i>Grup etkinliği · <a href="group.html?id=${ev.groupId}" class="fw-semibold text-decoration-none">${escapeHtml(ev.groupName)}</a> <span class="text-muted">(sadece grup üyeleri görür)</span>`;
  }

  // Someone invited me and I haven't answered yet.
  document.getElementById("evInviteBanner").classList.toggle("d-none", !ev.invitedByName || ev.isPast);
  document.getElementById("evInvitedBy").textContent = ev.invitedByName || "";

  document.getElementById("evAnnouncementWrap").classList.toggle("d-none", !(ev.isOwner && !ev.isPast));

  renderEventActions();
}

async function answerInvite(accept) {
  try {
    if (accept) {
      currentEvent = await apiFetch(`/api/events/${eventId}/invites/accept`, { method: "POST" });
      toast("Etkinliğe katılıyorsun.");
      renderEvent();
      await loadAttendees();
    } else {
      await apiFetch(`/api/events/${eventId}/invites/decline`, { method: "POST" });
      toast("Davet reddedildi.");
      await loadEvent();
    }
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
    await loadEvent();
  }
}

function renderEventActions() {
  const ev = currentEvent;
  const going = ev.currentUserStatus === "Going";
  const interested = ev.currentUserStatus === "Interested";

  const primary = eventPrimaryAction(ev);

  const statusButtons = ev.isPast
    ? `<span class="btn btn-light disabled rounded-pill"><i class="bi bi-check2-circle me-1"></i>Sona erdi${going ? " · katıldın" : interested ? " · ilgileniyordun" : ""}</span>`
    : `<button class="btn ${primary.css} rounded-pill" onclick="setStatus('${primary.status}')">${primary.label}</button>
       <button class="btn ${interested ? "btn-outline-secondary interested" : "btn-outline-secondary"} rounded-pill" onclick="setStatus('Interested')">${interested ? "İlgileniyorsun" : "İlgileniyorum"}</button>`;

  const ownerButtons = ev.isOwner
    ? `${ev.isPast ? "" : `<button class="btn btn-light border rounded-pill" onclick="editEvent()"><i class="bi bi-pencil me-1"></i>Düzenle</button>`}
       <button class="btn btn-light border rounded-pill text-danger" onclick="deleteEvent()"><i class="bi bi-trash me-1"></i>Sil</button>`
    : "";

  const joinOnline = ev.isOnline && ev.onlineLink
    ? `<a href="${escapeAttr(ev.onlineLink)}" target="_blank" rel="noopener noreferrer" class="btn btn-success rounded-pill"><i class="bi bi-camera-video me-1"></i>Etkinliğe Bağlan</a>`
    : "";

  // The organizer and people who said "Katılıyorum" may invite others.
  // In a private event only the organizer invites; otherwise the organizer and people who are going.
  const inviteButton = (ev.isOwner || (going && !ev.isPrivate)) && !ev.isPast
    ? `<button class="btn btn-light border rounded-pill" onclick="openInviteModal()"><i class="bi bi-person-plus me-1"></i>Davet Et</button>`
    : "";

  // No point adding something that is already over to a calendar.
  const calendarMenu = ev.isPast ? "" : `
    <div class="dropdown">
      <button class="btn btn-light border rounded-pill dropdown-toggle" data-bs-toggle="dropdown"><i class="bi bi-calendar-plus me-1"></i>Takvime Ekle</button>
      <ul class="dropdown-menu">
        <li><a id="googleCalendarLink" class="dropdown-item disabled" href="#" target="_blank" rel="noopener noreferrer"><i class="bi bi-google me-2"></i>Google Takvim</a></li>
        <li><button class="dropdown-item" onclick="downloadCalendarFile()"><i class="bi bi-download me-2"></i>Apple / Outlook (.ics)</button></li>
      </ul>
    </div>`;

  document.getElementById("evActions").innerHTML = `
    ${statusButtons}
    ${joinOnline}
    ${inviteButton}
    ${calendarMenu}
    <button class="btn btn-light border rounded-pill" onclick="copyEventLink()"><i class="bi bi-link-45deg me-1"></i>Bağlantıyı Kopyala</button>
    ${ownerButtons}`;

  loadGoogleCalendarLink();
}

function attendeeChipHtml(person) {
  return `
    <a href="profile.html?id=${person.userId}" class="d-flex align-items-center gap-2 text-decoration-none text-body p-2 rounded-3 border" style="min-width:0;">
      <img src="${person.avatarUrl || DEFAULT_AVATAR}" class="rounded-circle flex-shrink-0" style="width:36px;height:36px;object-fit:cover;" alt="">
      <span class="small fw-semibold text-truncate">${escapeHtml(person.fullName)}</span>
      ${person.isOrganizer ? `<span class="badge bg-primary-subtle text-primary-emphasis rounded-pill ms-auto">Düzenleyen</span>` : ""}
    </a>`;
}

function attendeeGroupHtml(title, people, totalCount) {
  const hiddenCount = Math.max(totalCount - people.length, 0);
  const body = people.length
    ? `<div class="d-grid gap-2" style="grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));">${people.map(attendeeChipHtml).join("")}</div>`
      + (hiddenCount ? `<div class="text-muted small mt-2">ve ${hiddenCount} kişi daha</div>` : "")
    : `<div class="text-muted small">Henüz kimse yok.</div>`;
  return `<div class="mb-3"><div class="fw-semibold small mb-2">${title} <span class="text-muted">(${totalCount})</span></div>${body}</div>`;
}

async function loadAttendees() {
  const box = document.getElementById("evAttendees");
  try {
    const attendees = await apiFetch(`/api/events/${eventId}/attendees`);
    attendeeIds = new Set(attendees.map(a => a.userId));
    const going = attendees.filter(a => a.status === "Going");
    const interested = attendees.filter(a => a.status === "Interested");
    const waiting = attendees.filter(a => a.status === "Waitlisted");
    box.innerHTML = attendeeGroupHtml("Katılanlar", going, currentEvent.goingCount)
      + attendeeGroupHtml("İlgilenenler", interested, currentEvent.interestedCount)
      + (currentEvent.waitlistCount > 0 ? attendeeGroupHtml("Bekleme listesi", waiting, currentEvent.waitlistCount) : "");
  } catch (err) {
    box.innerHTML = `<div class="alert alert-danger small mb-0">${escapeHtml(err.message)}</div>`;
  }
}

async function setStatus(status) {
  const newStatus = currentEvent.currentUserStatus === status ? "None" : status;
  try {
    currentEvent = await apiFetch(`/api/events/${eventId}/status`, { method: "PUT", body: { status: newStatus } });
    renderEvent();
    await loadAttendees();
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
    await loadEvent(); // the spot may just have been taken: show the current state
  }
}

function editEvent() {
  openEventForm(currentEvent, async saved => {
    currentEvent = saved;
    renderEvent();
    await loadAttendees();
  });
}

async function deleteEvent() {
  const attendeeNote = currentEvent.goingCount + currentEvent.interestedCount > 0 ? " Katılımcılar da etkinlikten düşer." : "";
  if (!confirm(`"${currentEvent.title}" silinsin mi?${attendeeNote} Bu işlem geri alınamaz.`)) return;

  try {
    await apiFetch(`/api/events/${eventId}`, { method: "DELETE" });
    window.location.href = "events.html";
  } catch (err) {
    toast(err.message || "Etkinlik silinemedi.");
  }
}

async function copyEventLink() {
  const link = eventLink(eventId);
  try {
    await navigator.clipboard.writeText(link);
    toast("Etkinlik bağlantısı kopyalandı.");
  } catch (err) {
    // Clipboard access can be blocked (insecure origin, permissions): show the link so it can be copied by hand.
    window.prompt("Bağlantıyı kopyala:", link);
  }
}

async function loadEvent() {
  if (!eventId) {
    showEventError("Etkinlik bulunamadı.");
    return;
  }

  try {
    currentEvent = await apiFetch(`/api/events/${eventId}`);
    renderEvent();
    await Promise.all([loadAttendees(), loadComments(true)]);
  } catch (err) {
    showEventError(/not found/i.test(err.message) ? "Bu etkinlik bulunamadı ya da kaldırılmış." : err.message);
  }
}

// ---- Invite people (organizer and people who are going; any real user, not just friends) ----

let attendeeIds = new Set(); // everyone already in the event (filled by loadAttendees)
let invitedIds = new Set();  // people with a pending invite (filled when the modal opens)

async function openInviteModal() {
  document.getElementById("inviteSearchInput").value = "";
  document.getElementById("inviteResults").innerHTML = "";
  document.getElementById("inviteGroupNote").classList.toggle("d-none", !currentEvent.groupId);

  try {
    const invitees = await apiFetch(`/api/events/${eventId}/invites`);
    invitedIds = new Set(invitees.map(i => i.userId));
  } catch (err) {
    invitedIds = new Set();
  }

  bootstrap.Modal.getOrCreateInstance(document.getElementById("inviteModal")).show();
}

// Whoever is already in the event, or already invited, gets a badge instead of the "Davet Et" button (see user-search.js).
const searchInviteCandidates = createUserSearch({
  resultsId: "inviteResults",
  actionFor: u => {
    if (attendeeIds.has(u.id) || u.id === currentEvent.createdByUserId)
      return `<span class="badge text-bg-light border text-muted fw-normal">Etkinlikte</span>`;
    if (invitedIds.has(u.id))
      return `<span class="badge text-bg-light border text-muted fw-normal">Davet edildi</span>`;
    return `<button class="btn btn-sm btn-outline-primary" onclick="inviteUser('${u.id}', this)">Davet Et</button>`;
  }
});

async function inviteUser(userId, btn) {
  btn.disabled = true;
  try {
    await apiFetch(`/api/events/${eventId}/invites`, { method: "POST", body: { userId } });
    invitedIds.add(userId);
    toast("Davet gönderildi.");
    btn.outerHTML = `<span class="badge text-bg-light border text-muted fw-normal">Davet edildi</span>`;
  } catch (err) {
    btn.disabled = false;
    toast(err.message || "Davet gönderilemedi.");
  }
}

// ---- Discussion: comments, plus announcements from the organizer ----

const COMMENTS_PAGE_SIZE = 20;
let commentsPage = 1;
let commentsTotal = 0;

function commentHtml(c) {
  return `
    <div class="d-flex gap-2 py-2 border-bottom ${c.isAnnouncement ? "bg-warning-subtle rounded-3 px-2 mb-1" : ""}" data-comment-id="${c.id}">
      <a href="profile.html?id=${c.authorId}"><img src="${c.authorAvatarUrl || DEFAULT_AVATAR}" class="rounded-circle flex-shrink-0" style="width:36px;height:36px;object-fit:cover;" alt=""></a>
      <div class="flex-grow-1" style="min-width:0;">
        <div class="small">
          <a href="profile.html?id=${c.authorId}" class="fw-semibold text-body text-decoration-none">${escapeHtml(c.authorName)}</a>
          ${c.isAnnouncement ? `<span class="badge bg-warning text-dark ms-1"><i class="bi bi-megaphone-fill me-1"></i>Duyuru</span>` : ""}
          <span class="text-muted ms-1">${timeAgo(c.createdAt)}</span>
        </div>
        <div class="text-break" style="white-space: pre-wrap;">${escapeHtml(c.content)}</div>
      </div>
      ${c.canDelete ? `<button class="btn btn-sm btn-link text-muted p-0 align-self-start" onclick="deleteComment('${c.id}')" title="Sil"><i class="bi bi-trash"></i></button>` : ""}
    </div>`;
}

async function loadComments(reset) {
  const box = document.getElementById("evComments");
  if (reset) commentsPage = 1;
  try {
    const result = await apiFetch(`/api/events/${eventId}/comments?page=${commentsPage}&pageSize=${COMMENTS_PAGE_SIZE}`);
    commentsTotal = result.totalCount;
    const html = result.items.map(commentHtml).join("");
    if (reset) {
      box.innerHTML = html || `<div class="text-muted small">Henüz mesaj yok. İlk yazan sen ol.</div>`;
    } else {
      box.insertAdjacentHTML("beforeend", html);
    }
    document.getElementById("evCommentsMoreWrap").classList.toggle("d-none", commentsPage * COMMENTS_PAGE_SIZE >= commentsTotal);
  } catch (err) {
    box.innerHTML = `<div class="alert alert-danger small mb-0">${escapeHtml(err.message)}</div>`;
  }
}

function loadMoreComments() {
  commentsPage++;
  return loadComments(false);
}

async function postComment() {
  const textarea = document.getElementById("evCommentText");
  const content = textarea.value.trim();
  if (!content) return;

  const wrap = document.getElementById("evAnnouncementWrap");
  const toggle = document.getElementById("evAnnouncementToggle");
  const isAnnouncement = toggle.checked && !wrap.classList.contains("d-none");

  const btn = document.getElementById("evCommentBtn");
  btn.disabled = true;
  try {
    await apiFetch(`/api/events/${eventId}/comments`, { method: "POST", body: { content, isAnnouncement } });
    textarea.value = "";
    toggle.checked = false;
    if (isAnnouncement) toast("Duyuru paylaşıldı, katılanlara bildirim gitti.");
    await loadComments(true);
  } catch (err) {
    toast(err.message || "Mesaj gönderilemedi.");
  } finally {
    btn.disabled = false;
  }
}

async function deleteComment(commentId) {
  if (!confirm("Bu mesaj silinsin mi?")) return;
  try {
    await apiFetch(`/api/events/${eventId}/comments/${commentId}`, { method: "DELETE" });
    await loadComments(true);
  } catch (err) {
    toast(err.message || "Mesaj silinemedi.");
  }
}

loadEvent();
