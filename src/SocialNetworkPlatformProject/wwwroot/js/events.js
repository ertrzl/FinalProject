// events.html — real events wired to the backend.

requireAuth();

let allEvents = [];
const eventsById = new Map(); // every event any tab has loaded, so edit/delete also work from the "Etkinliklerim" lists

function eventCardHtml(ev) {
  const goingBtnClass = ev.currentUserStatus === "Going" ? "btn-primary joined" : "btn-primary";
  // A full event only closes "Katılıyorum" for people who aren't already in.
  const goingBlocked = ev.isFull && ev.currentUserStatus !== "Going";
  const goingLabel = ev.currentUserStatus === "Going" ? "Katılıyorsun" : goingBlocked ? "Dolu" : "Katılıyorum";
  const interestedBtnClass = ev.currentUserStatus === "Interested" ? "btn-outline-secondary interested" : "btn-outline-secondary";
  const interestedLabel = ev.currentUserStatus === "Interested" ? "İlgileniyorsun" : "İlgileniyorum";

  return `
    <div class="col-md-6" data-event-id="${ev.id}">
      <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100">
        <a href="event.html?id=${ev.id}"><img src="${ev.coverImageUrl || NO_PHOTO}" class="w-100" style="height:150px;object-fit:cover;" alt=""></a>
        <div class="p-3">
          <div class="text-danger fw-bold small mb-1"><i class="bi bi-calendar3 me-1"></i>${formatEventDate(ev.startsAt)}</div>
          <div class="fw-bold fs-6"><a href="event.html?id=${ev.id}" class="text-dark text-decoration-none">${escapeHtml(ev.title)}</a></div>
          ${ev.groupName ? `<div class="small text-primary"><i class="bi bi-people-fill me-1"></i><a href="group.html?id=${ev.groupId}" class="text-primary text-decoration-none">${escapeHtml(ev.groupName)}</a></div>` : ""}
          <div class="text-muted small mb-3">${eventPlaceHtml(ev)} · ${eventAttendanceText(ev)}${ev.isFull ? ` <span class="badge bg-secondary-subtle text-secondary-emphasis rounded-pill">Dolu</span>` : ""}</div>
          ${ev.isOwner ? `
          <div class="d-flex gap-2 mb-2">
            ${ev.isPast ? "" : `<button class="btn btn-light border btn-sm rounded-pill flex-fill" onclick="openEventModal('${ev.id}')"><i class="bi bi-pencil me-1"></i>Düzenle</button>`}
            <button class="btn btn-light border btn-sm rounded-pill flex-fill text-danger" onclick="deleteEvent('${ev.id}')"><i class="bi bi-trash me-1"></i>Sil</button>
          </div>` : ""}
          ${ev.isPast
            ? `<div class="text-center text-muted small py-1"><i class="bi bi-check2-circle me-1"></i>Sona erdi${ev.currentUserStatus === "Going" ? " · katıldın" : ev.currentUserStatus === "Interested" ? " · ilgileniyordun" : ""}</div>`
            : `<div class="d-flex gap-2">
            <button class="btn ${goingBtnClass} btn-sm rounded-pill flex-fill" ${goingBlocked ? "disabled" : ""} onclick="setEventStatus('${ev.id}', 'Going')">${goingLabel}</button>
            <button class="btn ${interestedBtnClass} btn-sm rounded-pill flex-fill" onclick="setEventStatus('${ev.id}', 'Interested')">${interestedLabel}</button>
          </div>`}
        </div>
      </div>
    </div>`;
}

function renderGoing() {
  const grid = document.getElementById("goingGrid");
  const empty = document.getElementById("goingEmptyState");
  const going = allEvents.filter(e => e.currentUserStatus === "Going" || e.currentUserStatus === "Interested");
  grid.innerHTML = going.map(eventCardHtml).join("");
  empty.classList.toggle("d-none", going.length > 0);
}

function rememberEvents(events) {
  events.forEach(e => eventsById.set(e.id, e));
}

async function loadUpcoming() {
  const grid = document.getElementById("upcomingGrid");
  try {
    allEvents = await apiFetch("/api/events/upcoming");
    rememberEvents(allEvents);
    grid.innerHTML = allEvents.length
      ? allEvents.map(eventCardHtml).join("")
      : `<div class="col-12 text-center text-muted py-5">Yaklaşan etkinlik yok.</div>`;
    renderGoing();
  } catch (err) {
    grid.innerHTML = `<div class="col-12"><div class="alert alert-danger small">${escapeHtml(err.message)}</div></div>`;
  }
}

// "Etkinliklerim" sub-tabs: scope "created" (events I organized) or "past" (finished events I attended).
async function loadMine(scope) {
  const grid = document.getElementById(scope === "created" ? "createdGrid" : "pastGrid");
  const empty = document.getElementById(scope === "created" ? "createdEmptyState" : "pastEmptyState");
  try {
    const events = await apiFetch(`/api/events/mine?scope=${scope}`);
    rememberEvents(events);
    grid.innerHTML = events.map(eventCardHtml).join("");
    empty.classList.toggle("d-none", events.length > 0);
  } catch (err) {
    grid.innerHTML = `<div class="col-12"><div class="alert alert-danger small">${escapeHtml(err.message)}</div></div>`;
  }
}

// "Davetler" tab: events other people invited me to.
function inviteCardHtml(invite) {
  return `
    <div class="col-md-6" data-invite-event-id="${invite.eventId}">
      <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100">
        <a href="event.html?id=${invite.eventId}"><img src="${invite.coverImageUrl || NO_PHOTO}" class="w-100" style="height:120px;object-fit:cover;" alt=""></a>
        <div class="p-3">
          <div class="text-danger fw-bold small mb-1"><i class="bi bi-calendar3 me-1"></i>${formatEventDate(invite.startsAt)}</div>
          <div class="fw-bold fs-6"><a href="event.html?id=${invite.eventId}" class="text-dark text-decoration-none">${escapeHtml(invite.title)}</a></div>
          <div class="text-muted small mb-1">${eventPlaceHtml(invite)}${invite.groupName ? ` · <i class="bi bi-people-fill me-1"></i>${escapeHtml(invite.groupName)}` : ""}</div>
          <div class="text-muted small mb-3"><b>${escapeHtml(invite.invitedByName)}</b> seni davet etti · ${timeAgo(invite.invitedAt)}</div>
          <div class="d-flex gap-2">
            <button class="btn btn-primary btn-sm rounded-pill flex-fill" onclick="respondToEventInvite('${invite.eventId}', true, this)">Katılıyorum</button>
            <button class="btn btn-light border btn-sm rounded-pill flex-fill" onclick="respondToEventInvite('${invite.eventId}', false, this)">Reddet</button>
          </div>
        </div>
      </div>
    </div>`;
}

async function loadInvites() {
  const grid = document.getElementById("invitesGrid");
  try {
    const invites = await apiFetch("/api/events/invites/mine");
    document.getElementById("invitesCount").textContent = invites.length;
    document.getElementById("invitesEmptyState").classList.toggle("d-none", invites.length > 0);
    grid.innerHTML = invites.map(inviteCardHtml).join("");
  } catch (err) {
    grid.innerHTML = `<div class="col-12"><div class="alert alert-danger small">${escapeHtml(err.message)}</div></div>`;
  }
}

async function respondToEventInvite(eventId, accept, btn) {
  const buttons = btn.closest(".d-flex").querySelectorAll("button");
  buttons.forEach(b => b.disabled = true);
  try {
    await apiFetch(`/api/events/${eventId}/invites/${accept ? "accept" : "decline"}`, { method: "POST" });
    toast(accept ? "Etkinliğe katılıyorsun." : "Davet reddedildi.");
    await refreshAll();
  } catch (err) {
    buttons.forEach(b => b.disabled = false);
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

// After anything that can change several lists at once (create / edit / delete / answering an invite).
function refreshAll() {
  return Promise.all([loadUpcoming(), loadMine("created"), loadMine("past"), loadInvites()]);
}

async function setEventStatus(eventId, status) {
  const current = eventsById.get(eventId);
  const newStatus = current && current.currentUserStatus === status ? "None" : status;

  try {
    const updated = await apiFetch(`/api/events/${eventId}/status`, { method: "PUT", body: { status: newStatus } });
    const index = allEvents.findIndex(e => e.id === eventId);
    if (index >= 0) allEvents[index] = updated;
    eventsById.set(eventId, updated);

    document.querySelectorAll(`[data-event-id="${eventId}"]`).forEach(el => el.outerHTML = eventCardHtml(updated));
    renderGoing();
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
    // "Full" or "someone else just took the spot": show the real, current state instead of the stale card.
    refreshAll();
  }
}

// The create/edit modal lives in event-form.js (shared with event.html).
function openEventModal(eventId) {
  openEventForm(eventId ? eventsById.get(eventId) : null, refreshAll);
}

async function deleteEvent(eventId) {
  const ev = eventsById.get(eventId);
  const attendeeNote = ev && ev.goingCount + ev.interestedCount > 0 ? " Katılımcılar da etkinlikten düşer." : "";
  if (!confirm(`"${ev ? ev.title : "Bu etkinlik"}" silinsin mi?${attendeeNote} Bu işlem geri alınamaz.`)) return;

  try {
    await apiFetch(`/api/events/${eventId}`, { method: "DELETE" });
    toast("Etkinlik silindi.");
    await refreshAll();
  } catch (err) {
    toast(err.message || "Etkinlik silinemedi.");
  }
}

refreshAll();
