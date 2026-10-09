// events.html — real events wired to the backend.

requireAuth();

const eventsById = new Map(); // every event any tab has loaded, so edit/delete also work from the "Etkinliklerim" lists

function eventCardHtml(ev) {
  const primary = eventPrimaryAction(ev);
  const interestedBtnClass = ev.currentUserStatus === "Interested" ? "btn-outline-secondary interested" : "btn-outline-secondary";
  const interestedLabel = ev.currentUserStatus === "Interested" ? "İlgileniyorsun" : "İlgileniyorum";

  return `
    <div class="col-md-6" data-event-id="${ev.id}">
      <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100">
        <a href="event.html?id=${ev.id}"><img src="${ev.coverImageUrl || NO_PHOTO}" class="w-100" style="height:150px;object-fit:cover;" alt=""></a>
        <div class="p-3">
          <div class="text-danger fw-bold small mb-1"><i class="bi bi-calendar3 me-1"></i>${formatEventRange(ev.startsAt, ev.endsAt)} ${eventStateBadgeHtml(ev)}</div>
          <div class="fw-bold fs-6"><a href="event.html?id=${ev.id}" class="text-body text-decoration-none">${escapeHtml(ev.title)}</a></div>
          ${ev.groupName ? `<div class="small text-primary"><i class="bi bi-people-fill me-1"></i><a href="group.html?id=${ev.groupId}" class="text-primary text-decoration-none">${escapeHtml(ev.groupName)}</a></div>` : ""}
          <div class="text-muted small mb-3">${eventPlaceHtml(ev)} · ${eventAttendanceText(ev)}${ev.isFull ? ` <span class="badge bg-secondary-subtle text-secondary-emphasis rounded-pill">Dolu</span>` : ""}${ev.waitlistCount > 0 ? ` · ${eventWaitlistText(ev)}` : ""}</div>
          ${ev.isOwner ? `
          <div class="d-flex gap-2 mb-2">
            ${ev.isPast ? "" : `<button class="btn btn-light border btn-sm rounded-pill flex-fill" onclick="openEventModal('${ev.id}')"><i class="bi bi-pencil me-1"></i>Düzenle</button>`}
            <button class="btn btn-light border btn-sm rounded-pill flex-fill text-danger" onclick="deleteEvent('${ev.id}')"><i class="bi bi-trash me-1"></i>Sil</button>
          </div>` : ""}
          ${ev.isPast
            ? `<div class="text-center text-muted small py-1"><i class="bi bi-check2-circle me-1"></i>Sona erdi${ev.currentUserStatus === "Going" ? " · katıldın" : ev.currentUserStatus === "Interested" ? " · ilgileniyordun" : ""}</div>`
            : `<div class="d-flex gap-2">
            <button class="btn ${primary.css} btn-sm rounded-pill flex-fill" onclick="setEventStatus('${ev.id}', '${primary.status}')">${primary.label}</button>
            <button class="btn ${interestedBtnClass} btn-sm rounded-pill flex-fill" onclick="setEventStatus('${ev.id}', 'Interested')">${interestedLabel}</button>
          </div>`}
        </div>
      </div>
    </div>`;
}

function rememberEvents(events) {
  events.forEach(e => eventsById.set(e.id, e));
}

// ---- "Yaklaşan": search, quick filters and paging ----

const UPCOMING_PAGE_SIZE = 12;
let upcomingPage = 1;
let upcomingTotalPages = 1;
let upcomingRequest = 0; // answers can arrive out of order while typing: only the newest one may render
let upcomingSearchDebounce = null;

function upcomingQueryString(page) {
  const params = new URLSearchParams({ page, pageSize: UPCOMING_PAGE_SIZE });

  const search = document.getElementById("eventSearch").value.trim();
  if (search) params.set("search", search);

  // "Bugün / Bu hafta / Bu ay": the server works out the range, it only needs to know the viewer's time zone.
  const when = document.getElementById("eventWhen").value;
  if (when) {
    params.set("when", when);
    params.set("timeZone", Intl.DateTimeFormat().resolvedOptions().timeZone);
  }

  const format = document.getElementById("eventFormat").value;
  if (format) params.set("isOnline", format === "online");

  return params.toString();
}

function hasUpcomingFilters() {
  return !!(document.getElementById("eventSearch").value.trim()
    || document.getElementById("eventWhen").value
    || document.getElementById("eventFormat").value);
}

// Typing waits a moment so every keystroke isn't a request; the selects apply at once.
function onEventFilterChange(debounced) {
  clearTimeout(upcomingSearchDebounce);
  upcomingSearchDebounce = setTimeout(() => loadUpcoming(true), debounced ? 350 : 0);
}

async function loadUpcoming(reset = true) {
  const grid = document.getElementById("upcomingGrid");
  const moreWrap = document.getElementById("upcomingMoreWrap");
  const page = reset ? 1 : upcomingPage + 1;
  const requestId = ++upcomingRequest;

  try {
    const result = await apiFetch(`/api/events/upcoming?${upcomingQueryString(page)}`);
    if (requestId !== upcomingRequest) return;

    upcomingPage = result.page;
    upcomingTotalPages = result.totalPages;
    rememberEvents(result.items);

    const html = result.items.map(eventCardHtml).join("");
    if (reset) {
      grid.innerHTML = html || `<div class="col-12 text-center text-muted py-5">${hasUpcomingFilters() ? "Aramana uyan etkinlik bulunamadı." : "Yaklaşan etkinlik yok."}</div>`;
    } else {
      grid.insertAdjacentHTML("beforeend", html);
    }

    document.getElementById("upcomingCount").textContent = result.totalCount ? `${result.totalCount} etkinlik` : "";
    moreWrap.classList.toggle("d-none", upcomingPage >= upcomingTotalPages);
  } catch (err) {
    if (requestId !== upcomingRequest) return;
    grid.innerHTML = `<div class="col-12"><div class="alert alert-danger small">${escapeHtml(err.message)}</div></div>`;
  }
}

function loadMoreUpcoming() {
  return loadUpcoming(false);
}

// "Katıldıklarım": upcoming and ongoing events I'm going to or interested in (its own list, not a filter of "Yaklaşan").
async function loadGoing() {
  const grid = document.getElementById("goingGrid");
  const empty = document.getElementById("goingEmptyState");
  try {
    const events = await apiFetch("/api/events/mine?scope=attending");
    rememberEvents(events);
    grid.innerHTML = events.map(eventCardHtml).join("");
    empty.classList.toggle("d-none", events.length > 0);
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
          <div class="text-danger fw-bold small mb-1"><i class="bi bi-calendar3 me-1"></i>${formatEventRange(invite.startsAt, invite.endsAt)}</div>
          <div class="fw-bold fs-6"><a href="event.html?id=${invite.eventId}" class="text-body text-decoration-none">${escapeHtml(invite.title)}</a></div>
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
  return Promise.all([loadUpcoming(true), loadGoing(), loadMine("created"), loadMine("past"), loadInvites()]);
}

async function setEventStatus(eventId, status) {
  const current = eventsById.get(eventId);
  const newStatus = current && current.currentUserStatus === status ? "None" : status;

  try {
    const updated = await apiFetch(`/api/events/${eventId}/status`, { method: "PUT", body: { status: newStatus } });
    eventsById.set(eventId, updated);

    // The card may sit in several lists at once; "Katıldıklarım" gains or loses a card, so it reloads.
    document.querySelectorAll(`[data-event-id="${eventId}"]`).forEach(el => el.outerHTML = eventCardHtml(updated));
    await loadGoing();
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
