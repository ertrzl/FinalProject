// events.html — real events wired to the backend.

requireAuth();

let allEvents = [];

function formatEventDate(isoDate) {
  // Unlike post/story timestamps, startsAt is the plain local time the organizer typed in
  // ("başlıyor saat 18:00"), not UTC — so it must NOT get a "Z" appended before parsing.
  const date = new Date(isoDate);
  return date.toLocaleDateString("tr-TR", { day: "numeric", month: "long", weekday: "long" })
    + " · " + date.toLocaleTimeString("tr-TR", { hour: "2-digit", minute: "2-digit" });
}

function eventCardHtml(ev) {
  const goingBtnClass = ev.currentUserStatus === "Going" ? "btn-primary joined" : "btn-primary";
  const goingLabel = ev.currentUserStatus === "Going" ? "Katılıyorsun" : "Katılıyorum";
  const interestedBtnClass = ev.currentUserStatus === "Interested" ? "btn-outline-secondary interested" : "btn-outline-secondary";
  const interestedLabel = ev.currentUserStatus === "Interested" ? "İlgileniyorsun" : "İlgileniyorum";

  return `
    <div class="col-md-6" data-event-id="${ev.id}">
      <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100">
        <img src="${ev.coverImageUrl || "https://picsum.photos/seed/" + ev.id + "/500/220"}" class="w-100" style="height:150px;object-fit:cover;" alt="">
        <div class="p-3">
          <div class="text-danger fw-bold small mb-1"><i class="bi bi-calendar3 me-1"></i>${formatEventDate(ev.startsAt)}</div>
          <div class="fw-bold fs-6">${escapeHtml(ev.title)}</div>
          <div class="text-muted small mb-3"><i class="bi bi-geo-alt me-1"></i>${escapeHtml(ev.location || "Belirtilmedi")} · ${ev.goingCount} katılımcı</div>
          <div class="d-flex gap-2">
            <button class="btn ${goingBtnClass} btn-sm rounded-pill flex-fill" onclick="setEventStatus('${ev.id}', 'Going')">${goingLabel}</button>
            <button class="btn ${interestedBtnClass} btn-sm rounded-pill flex-fill" onclick="setEventStatus('${ev.id}', 'Interested')">${interestedLabel}</button>
          </div>
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

async function loadUpcoming() {
  const grid = document.getElementById("upcomingGrid");
  try {
    allEvents = await apiFetch("/api/events/upcoming");
    grid.innerHTML = allEvents.length
      ? allEvents.map(eventCardHtml).join("")
      : `<div class="col-12 text-center text-muted py-5">Yaklaşan etkinlik yok.</div>`;
    renderGoing();
  } catch (err) {
    grid.innerHTML = `<div class="col-12"><div class="alert alert-danger small">${escapeHtml(err.message)}</div></div>`;
  }
}

async function setEventStatus(eventId, status) {
  const current = allEvents.find(e => e.id === eventId);
  const newStatus = current && current.currentUserStatus === status ? "None" : status;

  try {
    const updated = await apiFetch(`/api/events/${eventId}/status`, { method: "PUT", body: { status: newStatus } });
    const index = allEvents.findIndex(e => e.id === eventId);
    allEvents[index] = updated;

    document.querySelectorAll(`[data-event-id="${eventId}"]`).forEach(el => el.outerHTML = eventCardHtml(updated));
    renderGoing();
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

async function publishEvent() {
  const title = document.getElementById("eventTitle").value.trim();
  const date = document.getElementById("eventDate").value;
  const time = document.getElementById("eventTime").value || "00:00";
  const errorBox = document.getElementById("eventError");
  errorBox.classList.add("d-none");

  if (!title || !date) {
    document.getElementById("eventTitle").classList.toggle("is-invalid", !title);
    document.getElementById("eventDate").classList.toggle("is-invalid", !date);
    return;
  }

  try {
    const formData = new FormData();
    formData.append("title", title);
    formData.append("description", document.getElementById("eventDescription").value.trim());
    formData.append("location", document.getElementById("eventLocation").value.trim());
    formData.append("startsAt", `${date}T${time}:00`);

    await apiFetchForm("/api/events", { method: "POST", body: formData });

    bootstrap.Modal.getInstance(document.getElementById("createEventModal"))?.hide();
    document.getElementById("eventTitle").value = "";
    document.getElementById("eventDate").value = "";
    document.getElementById("eventTime").value = "";
    document.getElementById("eventLocation").value = "";
    document.getElementById("eventDescription").value = "";
    toast("Etkinlik oluşturuldu!");
    await loadUpcoming();
  } catch (err) {
    errorBox.textContent = err.message || "Etkinlik oluşturulamadı.";
    errorBox.classList.remove("d-none");
  }
}

loadUpcoming();
