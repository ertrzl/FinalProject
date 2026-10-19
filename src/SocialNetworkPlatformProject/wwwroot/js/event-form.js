// Create / edit event modal, shared by events.html and event.html. The markup is injected here so both pages
// get the same form; callers use openEventForm(existingEventOrNull, onSaved).

const EVENT_FORM_HTML = `
<div class="modal fade" id="createEventModal" tabindex="-1">
  <div class="modal-dialog modal-dialog-centered">
    <div class="modal-content rounded-4">
      <div class="modal-header">
        <h5 class="modal-title fw-bold" id="eventModalTitle">Etkinlik Oluştur</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        <div class="mb-3 d-none" id="eventError"></div>
        <div class="alert alert-info small d-none" id="eventGroupNote"></div>
        <div class="mb-3">
          <label class="form-label fw-semibold small" for="eventTitle">Etkinlik Adı</label>
          <input type="text" id="eventTitle" class="form-control" placeholder="Örn. Sahil Voleybolu">
        </div>
        <div class="row g-3 mb-3">
          <div class="col-6">
            <label class="form-label fw-semibold small" for="eventDate">Tarih</label>
            <input type="date" id="eventDate" class="form-control">
          </div>
          <div class="col-6">
            <label class="form-label fw-semibold small" for="eventTime">Saat</label>
            <input type="time" id="eventTime" class="form-control">
          </div>
        </div>
        <div class="mb-3">
          <label class="form-label fw-semibold small" for="eventEndsAt">Bitiş <span class="text-muted fw-normal">(isteğe bağlı)</span></label>
          <input type="datetime-local" id="eventEndsAt" class="form-control">
          <div class="form-text" id="eventScheduleHint">Boş bırakırsan etkinlik 3 saat sürer.</div>
        </div>
        <div class="form-check form-switch mb-3" id="eventPrivateWrap">
          <input class="form-check-input" type="checkbox" id="eventIsPrivate">
          <label class="form-check-label small fw-semibold" for="eventIsPrivate">Özel etkinlik</label>
          <div class="form-text">Sadece davet ettiklerin ve katılanlar görebilir. Listede ve aramada görünmez, bağlantıyı bilen herkes açamaz.</div>
        </div>
        <div class="form-check form-switch mb-3">
          <input class="form-check-input" type="checkbox" id="eventIsOnline" onchange="syncEventFormToggles()">
          <label class="form-check-label small fw-semibold" for="eventIsOnline">Çevrimiçi etkinlik</label>
        </div>
        <div class="mb-3" id="eventLocationWrap">
          <label class="form-label fw-semibold small" for="eventLocation">Konum</label>
          <input type="text" id="eventLocation" class="form-control" placeholder="Örn. Caddebostan Sahili">
        </div>
        <div class="mb-3" id="eventMapWrap">
          <label class="form-label fw-semibold small" for="eventMapSearch">Haritada işaretle <span class="text-muted fw-normal">(isteğe bağlı)</span></label>
          <div class="d-flex gap-2 mb-2">
            <input type="text" id="eventMapSearch" class="form-control form-control-sm" maxlength="200" placeholder="Adres ara..." onkeydown="if (event.key === 'Enter') { event.preventDefault(); searchEventMap(); }">
            <button type="button" class="btn btn-light border btn-sm rounded-pill px-3 flex-shrink-0 event-map-btn" id="eventMapSearchBtn" onclick="searchEventMap()" aria-label="Ara"><i class="bi bi-search"></i></button>
          </div>
          <div class="list-group mb-2 d-none" id="eventMapResults"></div>
          <div id="eventMapPicker" class="event-map"></div>
          <div class="d-flex justify-content-between align-items-center gap-2 mt-2 small">
            <span class="text-muted" id="eventMapHint">Pini koymak için haritaya dokun.</span>
            <button type="button" class="btn btn-light border btn-sm rounded-pill flex-shrink-0 d-none event-map-btn" id="eventMapClear" onclick="clearEventPin()"><i class="bi bi-x-lg me-1"></i>Pini kaldır</button>
          </div>
        </div>
        <div class="mb-3 d-none" id="eventOnlineWrap">
          <label class="form-label fw-semibold small" for="eventOnlineLink">Bağlantı</label>
          <input type="url" id="eventOnlineLink" class="form-control" placeholder="https://meet.google.com/...">
          <div class="form-text">Bağlantıyı sadece "Katılıyorum" diyenler ve sen görürsün.</div>
        </div>
        <div class="mb-3">
          <label class="form-label fw-semibold small" for="eventDescription">Açıklama</label>
          <textarea id="eventDescription" class="form-control" rows="3" placeholder="Etkinlik hakkında bilgi ver..."></textarea>
        </div>
        <div class="mb-3">
          <div class="form-check form-switch">
            <input class="form-check-input" type="checkbox" id="eventLimitToggle" onchange="syncEventFormToggles()">
            <label class="form-check-label small fw-semibold" for="eventLimitToggle">Katılımcı sınırı koy</label>
          </div>
          <div class="d-none mt-2" id="eventCapacityWrap">
            <input type="number" id="eventCapacity" class="form-control" min="1" step="1" placeholder="Kaç kişi katılabilir?">
            <div class="form-text">Sınır dolunca "Katılıyorum" kapanır, "İlgileniyorum" açık kalır.</div>
          </div>
        </div>
        <div class="mb-1">
          <label class="form-label fw-semibold small">Kapak Fotoğrafı</label>
          <div id="eventCoverPreviewWrap" class="d-none mb-2">
            <img id="eventCoverPreview" class="w-100 rounded-3" style="height:120px;object-fit:cover;" alt="">
            <div class="form-check mt-2">
              <input class="form-check-input" type="checkbox" id="eventRemoveCover">
              <label class="form-check-label small" for="eventRemoveCover">Mevcut kapak fotoğrafını kaldır</label>
            </div>
          </div>
          <input type="file" id="eventCover" class="form-control" accept="image/*">
        </div>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-light rounded-pill" data-bs-dismiss="modal">Vazgeç</button>
        <button type="button" class="btn btn-primary rounded-pill" id="eventSubmitBtn" onclick="saveEventForm()">Oluştur</button>
      </div>
    </div>
  </div>
</div>`;

document.body.insertAdjacentHTML("beforeend", EVENT_FORM_HTML);
enableLocationAutocomplete(document.getElementById("eventLocation"));

let formEventId = null;   // null = creating
let formOnSaved = null;
let formGroup = null;     // { id, name } when a new event is created for a group

// ---- Map (Leaflet + OpenStreetMap tiles, loaded the first time a map is needed) ----
// Shared with event.js, which shows the pin on the event page. Leaflet is only loaded from jsdelivr when a map is opened.

const LEAFLET_URL = "https://cdn.jsdelivr.net/npm/leaflet@1.9.4/dist/";
const EVENT_MAP_TILES = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
const EVENT_MAP_START = { center: [39, 35], zoom: 5 }; // Turkey, until a pin or a search result says otherwise

let leafletPromise = null;

function loadLeaflet() {
  if (window.L) return Promise.resolve(window.L);
  if (leafletPromise) return leafletPromise;

  leafletPromise = new Promise((resolve, reject) => {
    const style = document.createElement("link");
    style.rel = "stylesheet";
    style.href = LEAFLET_URL + "leaflet.css";
    document.head.appendChild(style);

    const script = document.createElement("script");
    script.src = LEAFLET_URL + "leaflet.js";
    script.onload = () => resolve(window.L);
    script.onerror = () => { leafletPromise = null; reject(new Error("Leaflet could not be loaded")); };
    document.head.appendChild(script);
  });
  return leafletPromise;
}

// A map in the given element. The pin is a Bootstrap icon in a divIcon, so no marker pictures have to be fetched.
function createEventMap(element, center, zoom, options = {}) {
  const map = L.map(element, { scrollWheelZoom: false, ...options }).setView(center, zoom);
  L.tileLayer(EVENT_MAP_TILES, {
    maxZoom: 19,
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener noreferrer">OpenStreetMap</a>'
  }).addTo(map);
  return map;
}

function eventPinIcon() {
  return L.divIcon({ className: "event-pin", html: '<i class="bi bi-geo-alt-fill"></i>', iconSize: [36, 36], iconAnchor: [18, 34] });
}

// OpenStreetMap's route planner with the event as the destination; the visitor types where they start from.
function eventDirectionsUrl(latitude, longitude) {
  return `https://www.openstreetmap.org/directions?route=%3B${encodeURIComponent(latitude)}%2C${encodeURIComponent(longitude)}`;
}

// ---- The picker in the create / edit form ----

let eventPin = null;          // { lat, lng } the organizer placed, or null for "no pin"
let eventPickerMap = null;
let eventPickerMarker = null;
let eventFormOpen = false;    // the modal is on the screen (a map can only be sized then)

function syncEventPinUi() {
  document.getElementById("eventMapClear").classList.toggle("d-none", !eventPin);
  document.getElementById("eventMapHint").textContent = eventPin
    ? "Pin yerleştirildi. Taşımak için sürükle ya da haritaya tekrar dokun."
    : "Pini koymak için haritaya dokun.";
}

// The marker follows the pin: made when there is a pin and none yet, moved when it changes, removed when it is gone.
function drawEventPin() {
  if (!eventPickerMap) return;

  if (!eventPin) {
    if (eventPickerMarker) { eventPickerMarker.remove(); eventPickerMarker = null; }
    return;
  }

  if (!eventPickerMarker) {
    eventPickerMarker = L.marker([eventPin.lat, eventPin.lng], { draggable: true, icon: eventPinIcon() }).addTo(eventPickerMap);
    eventPickerMarker.on("dragend", () => {
      const spot = eventPickerMarker.getLatLng().wrap();
      setEventPin(spot.lat, spot.lng, false);
    });
  } else {
    eventPickerMarker.setLatLng([eventPin.lat, eventPin.lng]);
  }
}

// Six decimals are about ten centimetres: plenty, and a short number to send.
function setEventPin(latitude, longitude, moveView) {
  eventPin = { lat: Math.round(latitude * 1e6) / 1e6, lng: Math.round(longitude * 1e6) / 1e6 };
  drawEventPin();
  if (moveView && eventPickerMap) eventPickerMap.setView([eventPin.lat, eventPin.lng], Math.max(eventPickerMap.getZoom(), 16));
  syncEventPinUi();
}

function clearEventPin() {
  eventPin = null;
  drawEventPin();
  syncEventPinUi();
}

// Makes the map (once) and shows the current pin on it. Called when the form is on the screen and the place is not online.
async function showEventPickerMap() {
  const box = document.getElementById("eventMapPicker");
  try {
    await loadLeaflet();
  } catch (err) {
    box.innerHTML = `<div class="text-muted small p-3">Harita yüklenemedi.</div>`;
    return;
  }
  if (!eventFormOpen) return;

  if (!eventPickerMap) {
    box.innerHTML = "";
    eventPickerMap = createEventMap(box, EVENT_MAP_START.center, EVENT_MAP_START.zoom);
    eventPickerMap.on("click", e => {
      const spot = e.latlng.wrap();
      setEventPin(spot.lat, spot.lng, false);
    });
  }

  eventPickerMap.invalidateSize();
  if (eventPickerMarker) { eventPickerMarker.remove(); eventPickerMarker = null; }
  drawEventPin();
  if (eventPin) eventPickerMap.setView([eventPin.lat, eventPin.lng], 15);
  else eventPickerMap.setView(EVENT_MAP_START.center, EVENT_MAP_START.zoom);
}

// "Adres ara": Nominatim (OpenStreetMap's search) with the typed address, or the Konum field when the box is empty.
// Only when the button is pressed, never while typing, as the service asks.
async function searchEventMap() {
  const term = (document.getElementById("eventMapSearch").value.trim() || document.getElementById("eventLocation").value.trim());
  const list = document.getElementById("eventMapResults");
  if (term.length < 2) return;

  const button = document.getElementById("eventMapSearchBtn");
  button.disabled = true;
  try {
    const url = `https://nominatim.openstreetmap.org/search?format=jsonv2&limit=5&accept-language=${encodeURIComponent(getLang())}&q=${encodeURIComponent(term)}`;
    const response = await fetch(url, { headers: { Accept: "application/json" } });
    if (!response.ok) throw new Error("lookup failed");
    const found = (await response.json()).filter(r => Number.isFinite(parseFloat(r.lat)) && Number.isFinite(parseFloat(r.lon)));

    list.innerHTML = found.length
      ? found.map((r, i) => `<button type="button" class="list-group-item list-group-item-action small text-start" data-index="${i}"><i class="bi bi-geo-alt me-2 text-muted"></i><span data-no-i18n>${escapeHtml(r.display_name)}</span></button>`).join("")
      : `<div class="list-group-item small text-muted">Sonuç bulunamadı.</div>`;
    list.classList.remove("d-none");

    list.onclick = e => {
      const item = e.target.closest("[data-index]");
      if (!item) return;
      const result = found[Number(item.dataset.index)];
      setEventPin(parseFloat(result.lat), parseFloat(result.lon), true);

      // An empty place name takes the first parts of the address, so the pin and the text agree.
      const location = document.getElementById("eventLocation");
      if (!location.value.trim()) location.value = result.display_name.split(",").slice(0, 3).join(",").trim().slice(0, 200);

      list.classList.add("d-none");
      list.innerHTML = "";
    };
  } catch (err) {
    list.innerHTML = `<div class="list-group-item small text-danger">Arama yapılamadı.</div>`;
    list.classList.remove("d-none");
  } finally {
    button.disabled = false;
  }
}

document.getElementById("createEventModal").addEventListener("shown.bs.modal", () => {
  eventFormOpen = true;
  if (!document.getElementById("eventIsOnline").checked) showEventPickerMap();
});
document.getElementById("createEventModal").addEventListener("hidden.bs.modal", () => { eventFormOpen = false; });

// Online events swap the place for a link; the limit field only shows while the limit switch is on.
function syncEventFormToggles() {
  const online = document.getElementById("eventIsOnline").checked;
  document.getElementById("eventLocationWrap").classList.toggle("d-none", online);
  document.getElementById("eventMapWrap").classList.toggle("d-none", online);
  document.getElementById("eventOnlineWrap").classList.toggle("d-none", !online);
  document.getElementById("eventCapacityWrap").classList.toggle("d-none", !document.getElementById("eventLimitToggle").checked);

  // The map was hidden while the event was online: it needs to measure itself again when it comes back.
  if (!online && eventFormOpen) showEventPickerMap();
}

function clearEventForm() {
  ["eventTitle", "eventDate", "eventTime", "eventEndsAt", "eventLocation", "eventDescription", "eventCover", "eventOnlineLink", "eventCapacity", "eventMapSearch"].forEach(id => {
    const field = document.getElementById(id);
    field.value = "";
    field.classList.remove("is-invalid");
  });
  // An ongoing event's start can't be edited (see openEventForm); every open starts from a clean state.
  document.getElementById("eventDate").disabled = false;
  document.getElementById("eventTime").disabled = false;
  document.getElementById("eventScheduleHint").textContent = "Boş bırakırsan etkinlik 3 saat sürer.";
  document.getElementById("eventRemoveCover").checked = false;
  document.getElementById("eventIsPrivate").checked = false;
  document.getElementById("eventIsOnline").checked = false;
  document.getElementById("eventLimitToggle").checked = false;
  document.getElementById("eventError").classList.add("d-none");

  // No pin and no search results from the last time the form was open.
  eventPin = null;
  document.getElementById("eventMapResults").classList.add("d-none");
  document.getElementById("eventMapResults").innerHTML = "";
  syncEventPinUi();
}

// ev = an event DTO to edit, or null to create a new one. onSaved(savedEvent) runs after a successful save.
// options.group = { id, name } creates a group event (only that group's members can see it).
function openEventForm(ev, onSaved, options = {}) {
  clearEventForm();
  formEventId = ev ? ev.id : null;
  formOnSaved = onSaved || null;
  formGroup = !ev && options.group ? options.group : null;

  // A group event is already members-only, so "özel" doesn't apply to it.
  document.getElementById("eventPrivateWrap").classList.toggle("d-none", !!formGroup || !!(ev && ev.groupId));

  const groupNote = document.getElementById("eventGroupNote");
  groupNote.classList.toggle("d-none", !formGroup);
  if (formGroup) {
    groupNote.innerHTML = `<i class="bi bi-people-fill me-1"></i><b>${escapeHtml(formGroup.name)}</b> grubu için etkinlik: sadece grup üyeleri görebilir.`;
  }

  document.getElementById("eventModalTitle").textContent = ev ? "Etkinliği Düzenle" : "Etkinlik Oluştur";
  document.getElementById("eventSubmitBtn").textContent = ev ? "Kaydet" : "Oluştur";
  document.getElementById("eventCoverPreviewWrap").classList.toggle("d-none", !(ev && ev.coverImageUrl));

  if (ev) {
    // The API sends UTC instants; the inputs show them in the viewer's own time zone.
    document.getElementById("eventTitle").value = ev.title;
    document.getElementById("eventDate").value = toLocalInputValue(ev.startsAt, "date");
    document.getElementById("eventTime").value = toLocalInputValue(ev.startsAt, "time");
    document.getElementById("eventEndsAt").value = toLocalInputValue(ev.endsAt, "datetime");

    if (ev.isOngoing) {
      // It already started, so only the end can still move.
      document.getElementById("eventDate").disabled = true;
      document.getElementById("eventTime").disabled = true;
      document.getElementById("eventScheduleHint").textContent = "Etkinlik başladığı için başlangıç değiştirilemez, sadece bitişi değiştirebilirsin.";
    }
    document.getElementById("eventLocation").value = ev.location || "";
    if (ev.latitude != null && ev.longitude != null) {
      eventPin = { lat: ev.latitude, lng: ev.longitude };
      syncEventPinUi();
    }
    document.getElementById("eventDescription").value = ev.description || "";
    if (ev.coverImageUrl) document.getElementById("eventCoverPreview").src = ev.coverImageUrl;

    document.getElementById("eventIsPrivate").checked = ev.isPrivate;
    document.getElementById("eventIsOnline").checked = ev.isOnline;
    document.getElementById("eventOnlineLink").value = ev.onlineLink || ""; // the organizer always gets the link back
    document.getElementById("eventLimitToggle").checked = ev.capacity != null;
    document.getElementById("eventCapacity").value = ev.capacity ?? "";
  }

  syncEventFormToggles();

  bootstrap.Modal.getOrCreateInstance(document.getElementById("createEventModal")).show();
}

async function saveEventForm() {
  const title = document.getElementById("eventTitle").value.trim();
  const date = document.getElementById("eventDate").value;
  const time = document.getElementById("eventTime").value || "00:00";
  const errorBox = document.getElementById("eventError");
  errorBox.classList.add("d-none");

  const online = document.getElementById("eventIsOnline").checked;
  const onlineLink = document.getElementById("eventOnlineLink").value.trim();
  const limited = document.getElementById("eventLimitToggle").checked;
  const capacity = Number(document.getElementById("eventCapacity").value);
  const capacityInvalid = limited && (!Number.isInteger(capacity) || capacity < 1);
  const linkInvalid = online && !/^https?:\/\/\S+$/i.test(onlineLink);

  const endsAtValue = document.getElementById("eventEndsAt").value;
  const endsBeforeStart = !!endsAtValue && !!date && new Date(endsAtValue) <= new Date(`${date}T${time}`);

  document.getElementById("eventTitle").classList.toggle("is-invalid", !title);
  document.getElementById("eventDate").classList.toggle("is-invalid", !date);
  document.getElementById("eventCapacity").classList.toggle("is-invalid", capacityInvalid);
  document.getElementById("eventOnlineLink").classList.toggle("is-invalid", linkInvalid);
  document.getElementById("eventEndsAt").classList.toggle("is-invalid", endsBeforeStart);
  if (!title || !date || capacityInvalid || linkInvalid || endsBeforeStart) return;

  const submitBtn = document.getElementById("eventSubmitBtn");
  submitBtn.disabled = true;
  try {
    const formData = new FormData();
    formData.append("title", title);
    formData.append("description", document.getElementById("eventDescription").value.trim());
    formData.append("location", online ? "" : document.getElementById("eventLocation").value.trim());
    // The pin goes as plain text with a dot; the server reads it with the invariant culture. No pin = nothing sent.
    if (!online && eventPin) {
      formData.append("latitude", String(eventPin.lat));
      formData.append("longitude", String(eventPin.lng));
    }
    formData.append("startsAt", localInputToIso(`${date}T${time}`));
    if (endsAtValue) formData.append("endsAt", localInputToIso(endsAtValue)); // leaving it out means "default duration"
    formData.append("isPrivate", document.getElementById("eventIsPrivate").checked);
    formData.append("isOnline", online);
    if (online) formData.append("onlineLink", onlineLink);
    if (limited) formData.append("capacity", capacity); // leaving it out means "no limit"

    const cover = document.getElementById("eventCover").files[0];
    if (cover) formData.append("coverImage", cover);

    const editing = formEventId !== null;
    if (!editing && formGroup) formData.append("groupId", formGroup.id);
    let saved;
    if (editing) {
      formData.append("removeCoverImage", document.getElementById("eventRemoveCover").checked);
      saved = await apiFetchForm(`/api/events/${formEventId}`, { method: "PUT", body: formData });
    } else {
      saved = await apiFetchForm("/api/events", { method: "POST", body: formData });
    }

    bootstrap.Modal.getInstance(document.getElementById("createEventModal"))?.hide();
    toast(editing ? "Etkinlik güncellendi." : "Etkinlik oluşturuldu!");
    if (formOnSaved) await formOnSaved(saved);
  } catch (err) {
    errorBox.textContent = err.message || "Etkinlik kaydedilemedi.";
    errorBox.classList.remove("d-none");
  } finally {
    submitBtn.disabled = false;
  }
}
