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
  document.title = `${ev.title} | Stoop`;
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

  // The attendance list is the organizer's only (the server refuses everyone else too).
  document.getElementById("evAttendanceTabItem").classList.toggle("d-none", !ev.isOwner);
  if (!eventAlbum) setPhotoTabCount(ev.photoCount);

  renderEventActions();
  renderEventMap();
}

// ---- Map: the pin of an in-person event, with a link to OpenStreetMap's route planner ----

let eventMap = null;
let eventMapMarker = null;
let eventMapSpot = null; // "lat,lng" of the pin as last drawn: the page is redrawn on every status change and must not recentre the map each time

async function renderEventMap() {
  const ev = currentEvent;
  const hasPin = !ev.isOnline && ev.latitude != null && ev.longitude != null;
  document.getElementById("evMapCard").classList.toggle("d-none", !hasPin);
  if (!hasPin) {
    eventMapSpot = null;
    return;
  }

  document.getElementById("evDirections").href = eventDirectionsUrl(ev.latitude, ev.longitude);

  const spot = `${ev.latitude},${ev.longitude}`;
  if (spot === eventMapSpot) return;

  const box = document.getElementById("evMap");
  try {
    await loadLeaflet();
  } catch (err) {
    box.innerHTML = `<div class="text-muted small p-3">Harita yüklenemedi.</div>`;
    return;
  }

  // A finger on the map would otherwise drag the map instead of scrolling the page: on a phone the page scrolls, the buttons zoom.
  if (!eventMap) {
    box.innerHTML = "";
    eventMap = createEventMap(box, [ev.latitude, ev.longitude], 15, { dragging: !L.Browser.mobile });
  }

  eventMap.invalidateSize();
  eventMap.setView([ev.latitude, ev.longitude], 15);
  if (eventMapMarker) eventMapMarker.setLatLng([ev.latitude, ev.longitude]);
  else eventMapMarker = L.marker([ev.latitude, ev.longitude], { icon: eventPinIcon(), keyboard: false }).addTo(eventMap);
  eventMapSpot = spot;
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

  // On a finished event the organizer's tick on the attendance list ("Yoklama") is what really says you were there.
  const statusButtons = ev.isPast
    ? (ev.myCheckedIn
      ? `<span class="btn btn-success-subtle text-success-emphasis disabled rounded-pill"><i class="bi bi-patch-check-fill me-1"></i>Katıldın</span>`
      : `<span class="btn btn-light disabled rounded-pill"><i class="bi bi-check2-circle me-1"></i>Sona erdi${going ? " · katıldın" : interested ? " · ilgileniyordun" : ""}</span>`)
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

  // A post on your own feed with this event as a card. A private event or a group's is not for the feed (the server refuses it too).
  const shareButton = !ev.isPrivate && !ev.groupId
    ? `<button class="btn btn-light border rounded-pill" onclick="openEventShare()"><i class="bi bi-share me-1"></i>Gönderide paylaş</button>`
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
    ${shareButton}
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
  if (!(await confirmDialog(`"${currentEvent.title}" silinsin mi?${attendeeNote} Bu işlem geri alınamaz.`))) return;

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
    linkDialog(link);
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
  if (!(await confirmDialog("Bu mesaj silinsin mi?"))) return;
  try {
    await apiFetch(`/api/events/${eventId}/comments/${commentId}`, { method: "DELETE" });
    await loadComments(true);
  } catch (err) {
    toast(err.message || "Mesaj silinemedi.");
  }
}

// ---- Fotoğraflar: the album. The organizer and the people who are going add pictures; the server decides who may do what ----

let eventAlbum = null; // the album as the server last sent it (null until the tab was opened)

function setPhotoTabCount(count) {
  document.getElementById("evPhotosTab").textContent = `Fotoğraflar (${count || 0})`;
}

function photoTileHtml(photo, index, key) {
  return `
    <div class="ev-photo">
      <img src="${escapeAttr(photo.url)}" class="photo-zoomable" loading="lazy" alt="" onclick="openPhotoViewer('${key}', ${index})">
      <span class="ev-photo-by" data-no-i18n>${escapeHtml(photo.uploadedByName)}</span>
      ${photo.canDelete ? `<button type="button" class="ev-photo-del" aria-label="Fotoğrafı sil" onclick="deleteEventPhoto('${photo.id}')"><i class="bi bi-trash"></i></button>` : ""}
    </div>`;
}

function renderPhotos(album) {
  eventAlbum = album;
  setPhotoTabCount(album.totalCount);

  // The button is there only while the server says this person may add more.
  document.getElementById("evPhotoAddBtn").classList.toggle("d-none", !album.canUpload || album.myRemaining <= 0);

  document.getElementById("evPhotoInfo").textContent = album.canUpload
    ? `Sen ${album.myCount} / ${album.maxPerPerson} fotoğraf ekledin · Albümde ${album.totalCount} / ${album.maxPerEvent} fotoğraf var.`
    : "Fotoğraf eklemek için etkinliğe katılıyor olmalısın.";

  const grid = document.getElementById("evPhotoGrid");
  if (album.items.length === 0) {
    grid.innerHTML = `<div class="text-muted small">Henüz fotoğraf yok.</div>`;
    return;
  }

  const key = registerPhotoSet(`event-${eventId}`, album.items.map(p => p.url));
  grid.innerHTML = album.items.map((p, i) => photoTileHtml(p, i, key)).join("");
}

async function loadPhotos() {
  try {
    renderPhotos(await apiFetch(`/api/events/${eventId}/photos`));
  } catch (err) {
    document.getElementById("evPhotoGrid").innerHTML = `<div class="alert alert-danger small mb-0">${escapeHtml(err.message)}</div>`;
  }
}

async function uploadEventPhotos(input) {
  const files = Array.from(input.files || []);
  input.value = ""; // choosing the same file again must fire "change" again
  if (files.length === 0) return;

  const button = document.getElementById("evPhotoAddBtn");
  button.disabled = true;
  try {
    const form = new FormData();
    files.forEach(file => form.append("photos", file));
    renderPhotos(await apiFetchForm(`/api/events/${eventId}/photos`, { method: "POST", body: form }));
    toast(files.length === 1 ? "Fotoğraf eklendi." : "Fotoğraflar eklendi.");
  } catch (err) {
    toast(err.message || "Fotoğraflar eklenemedi.");
  } finally {
    button.disabled = false;
  }
}

async function deleteEventPhoto(photoId) {
  if (!(await confirmDialog("Bu fotoğraf silinsin mi?"))) return;
  try {
    await apiFetch(`/api/events/${eventId}/photos/${photoId}`, { method: "DELETE" });
    await loadPhotos();
  } catch (err) {
    toast(err.message || "Fotoğraf silinemedi.");
  }
}

// ---- Yoklama: the organizer ticks the people who came ----

function renderAttendance(attendance) {
  document.getElementById("evCheckInCount").textContent = `${attendance.checkedInCount}/${attendance.goingCount} geldi`;

  // The server says whether the list can still be changed; this only words it.
  document.getElementById("evCheckInInfo").textContent = attendance.state === "NotYet"
    ? "Yoklama etkinlik başlayınca açılır."
    : attendance.state === "Closed"
      ? "Yoklama süresi doldu, liste artık değiştirilemez."
      : "Gelen kişileri işaretle. Etkinlik bittikten sonra 7 gün boyunca değiştirebilirsin.";

  const list = document.getElementById("evCheckInList");
  if (attendance.items.length === 0) {
    list.innerHTML = `<div class="text-muted small">Henüz katılan yok.</div>`;
    return;
  }

  list.innerHTML = attendance.items.map(person => `
    <label class="ev-checkin-row" data-user-id="${person.userId}">
      <img src="${escapeAttr(person.avatarUrl || DEFAULT_AVATAR)}" class="rounded-circle flex-shrink-0" style="width:36px;height:36px;object-fit:cover;" alt="">
      <span class="flex-grow-1 fw-semibold small text-truncate" data-no-i18n>${escapeHtml(person.fullName)}</span>
      <span class="small ${person.checkedIn ? "text-success fw-semibold" : "text-muted"}">Geldi</span>
      <input type="checkbox" class="form-check-input ev-checkin-box" ${person.checkedIn ? "checked" : ""} ${attendance.isOpen ? "" : "disabled"} onchange="toggleCheckIn('${person.userId}', this)">
    </label>`).join("");
}

async function loadAttendance() {
  try {
    renderAttendance(await apiFetch(`/api/events/${eventId}/attendance`));
  } catch (err) {
    document.getElementById("evCheckInList").innerHTML = `<div class="alert alert-danger small mb-0">${escapeHtml(err.message)}</div>`;
  }
}

async function toggleCheckIn(userId, box) {
  const wanted = box.checked;
  box.disabled = true;
  try {
    renderAttendance(await apiFetch(`/api/events/${eventId}/attendance/${userId}`, { method: "PUT", body: { checkedIn: wanted } }));
    document.querySelector(`.ev-checkin-row[data-user-id="${userId}"] input`)?.focus(); // the list was drawn again; the keyboard stays on the same person
  } catch (err) {
    box.checked = !wanted;
    box.disabled = false;
    toast(err.message || "Yoklama kaydedilemedi.");
  }
}

// ---- Gönderide paylaş ----

function openEventShare() {
  document.getElementById("shareEventTitle").textContent = currentEvent.title;
  document.getElementById("shareEventWhen").textContent = formatEventRange(currentEvent.startsAt, currentEvent.endsAt);
  document.getElementById("shareEventText").value = "";
  document.getElementById("shareEventPrivacy").value = "Public";
  document.getElementById("shareEventError").classList.add("d-none");
  bootstrap.Modal.getOrCreateInstance(document.getElementById("shareEventModal")).show();
}

async function submitEventShare() {
  const error = document.getElementById("shareEventError");
  const button = document.getElementById("shareEventSend");
  button.disabled = true;
  try {
    const post = await apiFetch(`/api/events/${eventId}/share`, {
      method: "POST",
      body: {
        text: document.getElementById("shareEventText").value,
        privacy: document.getElementById("shareEventPrivacy").value
      }
    });
    bootstrap.Modal.getInstance(document.getElementById("shareEventModal"))?.hide();
    toast(`${i18n("event.sharedToast")} <a href="post.html?id=${post.id}" class="text-white fw-semibold">${i18n("event.sharedSee")}</a>`, { html: true });
  } catch (err) {
    error.textContent = err.message || "Paylaşılamadı.";
    error.classList.remove("d-none");
  } finally {
    button.disabled = false;
  }
}

// Tabs load what they show when they are opened (and again each time, so the album and the list are current).
document.getElementById("evPhotosTab").addEventListener("shown.bs.tab", loadPhotos);
document.getElementById("evAttendanceTab").addEventListener("shown.bs.tab", loadAttendance);
document.querySelector('[data-bs-target="#evPane-about"]').addEventListener("shown.bs.tab", () => eventMap?.invalidateSize());

// A link ending in #photos (or #attendance) opens that tab.
function openTabFromHash() {
  const target = { "#photos": "evPhotosTab", "#attendance": "evAttendanceTab" }[window.location.hash];
  if (target && !document.getElementById(target).closest(".d-none")) bootstrap.Tab.getOrCreateInstance(document.getElementById(target)).show();
}

loadEvent().then(openTabFromHash);
