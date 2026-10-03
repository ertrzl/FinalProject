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
          <label class="form-label fw-semibold small">Etkinlik Adı</label>
          <input type="text" id="eventTitle" class="form-control" placeholder="Örn. Sahil Voleybolu">
        </div>
        <div class="row g-3 mb-3">
          <div class="col-6">
            <label class="form-label fw-semibold small">Tarih</label>
            <input type="date" id="eventDate" class="form-control">
          </div>
          <div class="col-6">
            <label class="form-label fw-semibold small">Saat</label>
            <input type="time" id="eventTime" class="form-control">
          </div>
        </div>
        <div class="form-check form-switch mb-3">
          <input class="form-check-input" type="checkbox" id="eventIsOnline" onchange="syncEventFormToggles()">
          <label class="form-check-label small fw-semibold" for="eventIsOnline">Çevrimiçi etkinlik</label>
        </div>
        <div class="mb-3" id="eventLocationWrap">
          <label class="form-label fw-semibold small">Konum</label>
          <input type="text" id="eventLocation" class="form-control" placeholder="Örn. Caddebostan Sahili">
        </div>
        <div class="mb-3 d-none" id="eventOnlineWrap">
          <label class="form-label fw-semibold small">Bağlantı</label>
          <input type="url" id="eventOnlineLink" class="form-control" placeholder="https://meet.google.com/...">
          <div class="form-text">Bağlantıyı sadece "Katılıyorum" diyenler ve sen görürsün.</div>
        </div>
        <div class="mb-3">
          <label class="form-label fw-semibold small">Açıklama</label>
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

// Online events swap the place for a link; the limit field only shows while the limit switch is on.
function syncEventFormToggles() {
  const online = document.getElementById("eventIsOnline").checked;
  document.getElementById("eventLocationWrap").classList.toggle("d-none", online);
  document.getElementById("eventOnlineWrap").classList.toggle("d-none", !online);
  document.getElementById("eventCapacityWrap").classList.toggle("d-none", !document.getElementById("eventLimitToggle").checked);
}

function clearEventForm() {
  ["eventTitle", "eventDate", "eventTime", "eventLocation", "eventDescription", "eventCover", "eventOnlineLink", "eventCapacity"].forEach(id => {
    const field = document.getElementById(id);
    field.value = "";
    field.classList.remove("is-invalid");
  });
  document.getElementById("eventRemoveCover").checked = false;
  document.getElementById("eventIsOnline").checked = false;
  document.getElementById("eventLimitToggle").checked = false;
  document.getElementById("eventError").classList.add("d-none");
}

// ev = an event DTO to edit, or null to create a new one. onSaved(savedEvent) runs after a successful save.
// options.group = { id, name } creates a group event (only that group's members can see it).
function openEventForm(ev, onSaved, options = {}) {
  clearEventForm();
  formEventId = ev ? ev.id : null;
  formOnSaved = onSaved || null;
  formGroup = !ev && options.group ? options.group : null;

  const groupNote = document.getElementById("eventGroupNote");
  groupNote.classList.toggle("d-none", !formGroup);
  if (formGroup) {
    groupNote.innerHTML = `<i class="bi bi-people-fill me-1"></i><b>${escapeHtml(formGroup.name)}</b> grubu için etkinlik: sadece grup üyeleri görebilir.`;
  }

  document.getElementById("eventModalTitle").textContent = ev ? "Etkinliği Düzenle" : "Etkinlik Oluştur";
  document.getElementById("eventSubmitBtn").textContent = ev ? "Kaydet" : "Oluştur";
  document.getElementById("eventCoverPreviewWrap").classList.toggle("d-none", !(ev && ev.coverImageUrl));

  if (ev) {
    // startsAt is the plain local time the organizer typed — slice it, don't run it through Date.
    document.getElementById("eventTitle").value = ev.title;
    document.getElementById("eventDate").value = ev.startsAt.slice(0, 10);
    document.getElementById("eventTime").value = ev.startsAt.slice(11, 16);
    document.getElementById("eventLocation").value = ev.location || "";
    document.getElementById("eventDescription").value = ev.description || "";
    if (ev.coverImageUrl) document.getElementById("eventCoverPreview").src = ev.coverImageUrl;

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

  document.getElementById("eventTitle").classList.toggle("is-invalid", !title);
  document.getElementById("eventDate").classList.toggle("is-invalid", !date);
  document.getElementById("eventCapacity").classList.toggle("is-invalid", capacityInvalid);
  document.getElementById("eventOnlineLink").classList.toggle("is-invalid", linkInvalid);
  if (!title || !date || capacityInvalid || linkInvalid) return;

  const submitBtn = document.getElementById("eventSubmitBtn");
  submitBtn.disabled = true;
  try {
    const formData = new FormData();
    formData.append("title", title);
    formData.append("description", document.getElementById("eventDescription").value.trim());
    formData.append("location", online ? "" : document.getElementById("eventLocation").value.trim());
    formData.append("startsAt", `${date}T${time}:00`);
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
