// admin.html — the admin panel: numbers, the reports people sent, accounts, content and the log of what admins did.
// Only the draw-and-ask part lives here: who may do what is decided by the server (/api/admin needs the Admin role;
// an admin cannot act on another admin or on themselves). A person without the role is sent back to the home page.

requireAuth();

const REASON_LABELS = {
  Spam: "Spam ya da reklam", Harassment: "Taciz ya da zorbalık", Inappropriate: "Uygunsuz içerik", Fake: "Sahte ya da yanıltıcı", Other: "Diğer"
};
const TARGET_LABELS = { Post: "Gönderi", Comment: "Yorum", Listing: "İlan", User: "Kişi", Event: "Etkinlik", Group: "Grup" };
const STATUS_LABELS = { Pending: "Bekliyor", Resolved: "Çözüldü", Dismissed: "Reddedildi" };
const ACTION_LABELS = {
  "report.resolve": "Şikâyet çözüldü", "report.dismiss": "Şikâyet reddedildi",
  "user.suspend": "Hesap askıya alındı", "user.unsuspend": "Askı kaldırıldı", "user.delete": "Hesap silindi",
  "post.remove": "Gönderi kaldırıldı", "comment.remove": "Yorum kaldırıldı", "listing.remove": "İlan kaldırıldı",
  "event.remove": "Etkinlik kaldırıldı", "group.remove": "Grup kaldırıldı"
};

const NOTE_MAX = 500;

function dateTimeText(isoDate) {
  return parseServerDate(isoDate).toLocaleString(localeTag(), { day: "numeric", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

// Somebody's own words (a post, a name) are never run through the page translation.
function userText(text) {
  return `<span data-no-i18n>${escapeHtml(text)}</span>`;
}

// A date as a piece of html that is never translated (dates are written for the chosen language already).
function dateHtml(isoDate) {
  return `<span data-no-i18n>${dateTimeText(isoDate)}</span>`;
}

// Small grey facts side by side; every fact is its own element so the page translation can handle each one.
function factsHtml(facts, extraClass = "mt-2") {
  return `<div class="small text-muted d-flex flex-wrap gap-x-3 ${extraClass}" style="column-gap:14px">${facts.filter(Boolean).map(f => `<span>${f}</span>`).join("")}</div>`;
}

// A link to the thing a report or a content row is about (a comment has no page of its own).
function targetHref(type, id) {
  switch (type) {
    case "Post": return `post.html?id=${id}`;
    case "User": return `profile.html?id=${id}`;
    case "Listing": return `marketplace.html?listing=${id}`;
    case "Event": return `event.html?id=${id}`;
    case "Group": return `group.html?id=${id}`;
    default: return null;
  }
}

// If the person is no longer an admin (the role was taken away since sign-in) the server says 403 and the page leaves.
async function adminFetch(path, options) {
  try {
    return await apiFetch(path, options);
  } catch (err) {
    if (!getSession()?.isAdmin) window.location.replace("home.html");
    throw err;
  }
}

// ---- A question with an optional note and some tick boxes (resolve, dismiss, suspend, remove) ----
// Resolves to null when cancelled, else { note, checks: { key: true | false } }.
function noteDialog({ title, intro, checks = [], placeholder = "Not (isteğe bağlı)", confirmText, danger = false }) {
  return new Promise(resolve => {
    openSiteDialog(`
      <div class="fw-bold mb-1 note-title"></div>
      <div class="text-muted small mb-3 note-intro" style="white-space:pre-line"></div>
      <div class="mb-3 note-checks"></div>
      <textarea class="form-control mb-3 note-text" rows="3" maxlength="${NOTE_MAX}"></textarea>
      <div class="d-flex justify-content-end flex-wrap gap-2">
        <button type="button" class="btn btn-light rounded-pill px-4" data-answer="no">Vazgeç</button>
        <button type="button" class="btn ${danger ? "btn-danger" : "btn-primary"} rounded-pill px-4" data-answer="yes"></button>
      </div>`,
      (box, close) => {
        box.querySelector(".note-title").textContent = title;
        box.querySelector(".note-intro").textContent = intro || "";
        box.querySelector(".note-text").placeholder = placeholder;
        box.querySelector('[data-answer="yes"]').textContent = confirmText;
        box.querySelector(".note-checks").innerHTML = checks.map(c => `
          <label class="report-reason"><input type="checkbox" data-check="${c.key}"><span></span></label>`).join("");
        checks.forEach((c, i) => { box.querySelectorAll(".note-checks span")[i].textContent = c.label; });

        const answer = value => { close(); resolve(value); };
        box.querySelector('[data-answer="yes"]').addEventListener("click", () => answer({
          note: box.querySelector(".note-text").value.trim() || null,
          checks: Object.fromEntries(checks.map(c => [c.key, box.querySelector(`[data-check="${c.key}"]`).checked]))
        }));
        box.querySelector('[data-answer="no"]').addEventListener("click", () => answer(null));
        box.addEventListener("dismiss", () => answer(null));
        box.querySelector(".note-text").focus();
      });
  });
}

// ---- Overview ----

const STAT_CARDS = [
  ["users", "Toplam kullanıcı", "bi-people"], ["newUsersThisWeek", "Bu hafta katılan", "bi-person-plus"], ["suspendedUsers", "Askıdaki hesap", "bi-person-slash"],
  ["posts", "Gönderi", "bi-chat-square-text"], ["postsToday", "Bugünkü gönderi", "bi-calendar-day"], ["listings", "İlan", "bi-shop"],
  ["events", "Etkinlik", "bi-calendar-event"], ["groups", "Grup", "bi-diagram-3"],
  ["pendingReports", "Bekleyen şikâyet", "bi-flag"], ["resolvedReports", "Kapatılan şikâyet", "bi-check2-circle"]
];

async function loadStats() {
  const box = document.getElementById("statsGrid");
  try {
    const stats = await adminFetch("/api/admin/stats");
    box.innerHTML = STAT_CARDS.map(([key, label, icon]) => `
      <div class="col-6 col-md-4 col-lg-3">
        <div class="card border-0 shadow-sm rounded-4 p-3 h-100 admin-stat ${key === "pendingReports" && stats[key] > 0 ? "admin-stat-alert" : ""}">
          <div class="text-muted small"><i class="bi ${icon} me-1"></i>${label}</div>
          <div class="fs-3 fw-bold">${stats[key]}</div>
        </div>
      </div>`).join("");

    const badge = document.getElementById("pendingBadge");
    badge.textContent = stats.pendingReports;
    badge.classList.toggle("d-none", stats.pendingReports === 0);
  } catch (err) {
    box.innerHTML = `<div class="text-danger small">${escapeHtml(err.message)}</div>`;
  }
}

// ---- Reports ----

let reportPager = null;
const drawnReports = new Map(); // the reports on the page, so the resolve box knows what each one is about

function reportRowHtml(r) {
  drawnReports.set(r.id, r);
  const href = targetHref(r.targetType, r.targetId);
  const pile = r.reportsAboutTarget > 1 ? `<span class="badge text-bg-light" title="Aynı şey için gelen şikâyet">${r.reportsAboutTarget} şikâyet</span>` : "";
  const gone = r.targetExists ? "" : `<span class="badge text-bg-secondary">Artık yok</span>`;
  const suspended = r.targetOwnerSuspended ? `<span class="badge text-bg-warning">Hesap askıda</span>` : "";

  const closed = r.status === "Pending" ? "" : `
    ${factsHtml([STATUS_LABELS[r.status], userText(r.resolvedByName || ""), r.resolvedAt ? dateHtml(r.resolvedAt) : "", r.adminNote ? userText(r.adminNote) : ""])}`;

  const actions = r.status !== "Pending" ? "" : `
    <div class="d-flex flex-wrap gap-2 mt-3">
      <button type="button" class="btn btn-primary btn-sm rounded-pill" onclick="resolveReport('${r.id}', false)"><i class="bi bi-check2 me-1"></i>Çöz</button>
      <button type="button" class="btn btn-light border btn-sm rounded-pill" onclick="resolveReport('${r.id}', true)">Reddet</button>
      ${href && r.targetExists ? `<a class="btn btn-light border btn-sm rounded-pill" href="${href}" target="_blank" rel="noopener">Aç</a>` : ""}
    </div>`;

  return `
    <div class="card border-0 shadow-sm rounded-4 p-3 mb-3 admin-row" data-report-id="${r.id}">
      <div class="d-flex flex-wrap align-items-center gap-2 small mb-2">
        <span class="badge text-bg-danger">${REASON_LABELS[r.reason] || r.reason}</span>
        <span class="badge text-bg-light">${TARGET_LABELS[r.targetType] || r.targetType}</span>
        ${pile}${gone}${suspended}
        <span class="text-muted ms-auto">${dateHtml(r.createdAt)}</span>
      </div>
      <div class="admin-quote">${userText(r.targetSummary || "—")}</div>
      ${factsHtml([
        `<span>Sahibi:</span> <a href="profile.html?id=${r.targetOwnerId}" class="text-decoration-none">${userText(r.targetOwnerName)}</a>`,
        `<span>Şikâyet eden:</span> <a href="profile.html?id=${r.reporterId}" class="text-decoration-none">${userText(r.reporterName)}</a>`
      ])}
      ${r.details ? `<div class="small mt-2"><span class="text-muted">Ayrıntı:</span> ${userText(r.details)}</div>` : ""}
      ${closed}${actions}
    </div>`;
}

function loadReports() {
  const status = document.getElementById("reportStatus").value;
  reportPager = createPager({
    list: "reportList",
    url: page => `/api/admin/reports?page=${page}&pageSize=20${status ? `&status=${status}` : ""}`,
    render: reportRowHtml,
    emptyHtml: `<div class="text-center text-muted py-5"><i class="bi bi-flag" style="font-size:42px;"></i><p class="mt-2 mb-0">Bu listede şikâyet yok.</p></div>`
  });
  return reportPager.reload();
}

async function resolveReport(id, dismiss) {
  const report = drawnReports.get(id);
  const removable = report.targetExists && ["Post", "Comment", "Listing"].includes(report.targetType);
  const suspendable = report.targetExists && !report.targetOwnerSuspended;

  const answer = await noteDialog(dismiss
    ? { title: "Şikâyet reddedilsin mi?", intro: "Bir sorun görülmedi; şikâyet kapanır, hiçbir şey kaldırılmaz.", confirmText: "Reddet" }
    : {
        title: "Şikâyet çözülsün mü?",
        intro: "İstersen aynı anda içeriği kaldırabilir ya da sahibinin hesabını askıya alabilirsin.",
        checks: [
          ...(removable ? [{ key: "removeContent", label: "Bu içeriği kaldır" }] : []),
          ...(suspendable ? [{ key: "suspendOwner", label: "Sahibinin hesabını askıya al" }] : [])
        ],
        confirmText: "Çöz"
      });
  if (!answer) return;

  try {
    await adminFetch(`/api/admin/reports/${id}/resolve`, {
      method: "POST",
      body: { dismiss, note: answer.note, removeContent: !!answer.checks.removeContent, suspendOwner: !!answer.checks.suspendOwner }
    });
    toast(dismiss ? "Şikâyet reddedildi." : "Şikâyet çözüldü.");
    loadReports();
    loadStats();
  } catch (err) {
    toast(err.message);
  }
}

// ---- Users ----

function userRowHtml(u) {
  const badges = [
    u.isAdmin ? `<span class="badge text-bg-primary">Yönetici</span>` : "",
    u.isSuspended ? `<span class="badge text-bg-warning">Askıda</span>` : "",
    u.reportsAgainst > 0 ? `<span class="badge text-bg-danger" title="Hakkındaki şikâyetler">${u.reportsAgainst} şikâyet</span>` : ""
  ].join(" ");

  const actions = u.isAdmin ? "" : `
    <div class="d-flex flex-wrap gap-2 mt-3">
      ${u.isSuspended
        ? `<button type="button" class="btn btn-light border btn-sm rounded-pill" onclick="unsuspendUser('${u.id}')">Askıyı kaldır</button>`
        : `<button type="button" class="btn btn-light border btn-sm rounded-pill" onclick="suspendUser('${u.id}', this)">Askıya al</button>`}
      <button type="button" class="btn btn-outline-danger btn-sm rounded-pill" onclick="deleteUser('${u.id}', this)">Hesabı sil</button>
    </div>`;

  return `
    <div class="card border-0 shadow-sm rounded-4 p-3 mb-3 admin-row" data-user-id="${u.id}" data-user-name="${escapeHtml(u.fullName)}">
      <div class="d-flex align-items-center gap-3">
        <a href="profile.html?id=${u.id}"><img src="${u.avatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt=""></a>
        <div class="min-w-0 flex-grow-1">
          <div class="fw-bold"><a href="profile.html?id=${u.id}" class="text-body text-decoration-none">${userText(u.fullName)}</a> ${badges}</div>
          <div class="text-muted small">${userText("@" + u.userName)} · ${userText(u.email)}</div>
        </div>
      </div>
      ${factsHtml([`<span>Katıldı:</span> ${dateHtml(u.createdAt)}`, `${u.postCount} gönderi`, u.lastSeenAt ? `<span>Son görülme:</span> ${dateHtml(u.lastSeenAt)}` : ""])}
      ${u.isSuspended ? `<div class="small mt-1"><span class="text-muted">Askı sebebi:</span> ${userText(u.suspendReason || "—")}</div>` : ""}
      ${actions}
    </div>`;
}

function loadUsers() {
  const term = document.getElementById("userTerm").value.trim();
  const onlySuspended = document.getElementById("userSuspendedOnly").checked;
  return createPager({
    list: "userList",
    url: page => `/api/admin/users?page=${page}&pageSize=20&term=${encodeURIComponent(term)}${onlySuspended ? "&suspended=true" : ""}`,
    render: userRowHtml,
    emptyHtml: `<div class="text-center text-muted py-5">Kullanıcı bulunamadı.</div>`
  }).reload();
}

function userNameOf(button) {
  return button.closest("[data-user-id]").dataset.userName;
}

async function suspendUser(id, button) {
  const name = userNameOf(button);
  const answer = await noteDialog({
    title: `${name} askıya alınsın mı?`,
    intro: "Kişi oturumdan atılır ve giriş yapamaz; gönderileri ve verileri silinmez. Sebep, giriş yapmayı denediğinde ona gösterilir.",
    placeholder: "Sebep (isteğe bağlı)",
    confirmText: "Askıya Al",
    danger: true
  });
  if (!answer) return;

  try {
    await adminFetch(`/api/admin/users/${id}/suspend`, { method: "POST", body: { reason: answer.note } });
    toast("Hesap askıya alındı.");
    loadUsers();
    loadStats();
  } catch (err) {
    toast(err.message);
  }
}

async function unsuspendUser(id) {
  try {
    await adminFetch(`/api/admin/users/${id}/unsuspend`, { method: "POST" });
    toast("Askı kaldırıldı.");
    loadUsers();
    loadStats();
  } catch (err) {
    toast(err.message);
  }
}

async function deleteUser(id, button) {
  const name = userNameOf(button);
  if (!(await confirmDialog(`${name} hesabı ve bütün içeriği kalıcı olarak silinsin mi? Bu işlem geri alınamaz.`, { confirmText: "Hesabı Sil" }))) return;

  try {
    await adminFetch(`/api/admin/users/${id}`, { method: "DELETE" });
    toast("Hesap silindi.");
    loadUsers();
    loadStats();
  } catch (err) {
    toast(err.message);
  }
}

// ---- Content ----

const CONTENT_TYPE_NAMES = { post: "Post", comment: "Comment", listing: "Listing", event: "Event", group: "Group" };

function contentRowHtml(c) {
  const kind = CONTENT_TYPE_NAMES[c.type];
  const href = targetHref(kind, c.id);
  return `
    <div class="card border-0 shadow-sm rounded-4 p-3 mb-3 admin-row" data-content-id="${c.id}" data-content-type="${c.type}">
      <div class="admin-quote">${userText(c.title || "—")}</div>
      <div class="d-flex flex-wrap align-items-center gap-2 small text-muted mt-2">
        <a href="profile.html?id=${c.ownerId}" class="text-decoration-none">${userText(c.ownerName)}</a>
        <span>${dateHtml(c.createdAt)}</span>
        <span class="ms-auto d-flex gap-2">
          ${href ? `<a class="btn btn-light border btn-sm rounded-pill" href="${href}" target="_blank" rel="noopener">Aç</a>` : ""}
          <button type="button" class="btn btn-outline-danger btn-sm rounded-pill" onclick="removeContent(this)">Kaldır</button>
        </span>
      </div>
    </div>`;
}

function loadContent() {
  const type = document.getElementById("contentType").value;
  const term = document.getElementById("contentTerm").value.trim();
  return createPager({
    list: "contentList",
    url: page => `/api/admin/content/${type}?page=${page}&pageSize=20&term=${encodeURIComponent(term)}`,
    render: contentRowHtml,
    emptyHtml: `<div class="text-center text-muted py-5">Bulunamadı.</div>`
  }).reload();
}

async function removeContent(button) {
  const row = button.closest("[data-content-id]");
  const answer = await noteDialog({
    title: "Bu içerik kaldırılsın mı?",
    intro: "Kalıcı olarak silinir; Kayıtlar sekmesine yazılır.",
    confirmText: "Kaldır",
    danger: true
  });
  if (!answer) return;

  try {
    await adminFetch(`/api/admin/content/${row.dataset.contentType}/${row.dataset.contentId}`, { method: "DELETE", body: { note: answer.note } });
    toast("İçerik kaldırıldı.");
    row.remove();
    loadStats();
  } catch (err) {
    toast(err.message);
  }
}

// ---- Audit log ----

function auditRowHtml(a) {
  return `
    <div class="card border-0 shadow-sm rounded-4 p-3 mb-2 admin-row">
      <div class="d-flex flex-wrap align-items-center gap-2 small">
        <span class="badge text-bg-light">${ACTION_LABELS[a.action] || userText(a.action)}</span>
        <span class="fw-semibold">${userText(a.actorName)}</span>
        <span class="text-muted ms-auto">${dateHtml(a.createdAt)}</span>
      </div>
      ${a.details ? `<div class="small mt-2">${userText(a.details)}</div>` : ""}
    </div>`;
}

function loadAudit() {
  const action = document.getElementById("auditAction").value;
  const term = document.getElementById("auditTerm").value.trim();
  return createPager({
    list: "auditList",
    url: page => `/api/admin/audit?page=${page}&pageSize=30&action=${encodeURIComponent(action)}&term=${encodeURIComponent(term)}`,
    render: auditRowHtml,
    emptyHtml: `<div class="text-center text-muted py-5">Henüz kayıt yok.</div>`
  }).reload();
}

// ---- Wiring ----

// A tab loads when it is first opened; typing in a search box waits a moment before asking the server.
const tabLoaders = { "panel-users": loadUsers, "panel-content": loadContent, "panel-audit": loadAudit };
const loadedTabs = new Set();

document.querySelectorAll('[data-bs-toggle="pill"]').forEach(button => {
  button.addEventListener("shown.bs.tab", () => {
    const panel = button.dataset.bsTarget.slice(1);
    if (tabLoaders[panel] && !loadedTabs.has(panel)) {
      loadedTabs.add(panel);
      tabLoaders[panel]();
    }
  });
});

function onTyping(inputId, load) {
  let timer = null;
  document.getElementById(inputId).addEventListener("input", () => {
    clearTimeout(timer);
    timer = setTimeout(load, 300);
  });
}

onTyping("userTerm", loadUsers);
onTyping("contentTerm", loadContent);
onTyping("auditTerm", loadAudit);
document.getElementById("userSuspendedOnly").addEventListener("change", loadUsers);
document.getElementById("reportStatus").addEventListener("change", loadReports);
document.getElementById("contentType").addEventListener("change", loadContent);
document.getElementById("auditAction").addEventListener("change", loadAudit);

if (!getSession()?.isAdmin) {
  window.location.replace("home.html");
} else {
  loadStats();
  loadReports();
}
