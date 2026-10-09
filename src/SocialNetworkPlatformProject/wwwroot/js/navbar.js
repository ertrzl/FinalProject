// The menu of every signed-in page, in one place: a new link is added here once, and the phone layout is fixed once.
//
// A page only has   <nav id="siteNav" data-active="home"></nav>   where the menu goes, and loads this file before app.js.
// data-active names the section the page belongs to: home, friends, notifications, messages, groups, marketplace, events
// (the main links) or profile, saved, settings (the account links); leave it empty on any other page.
//
// Wide screens: the top menu (logo, search, the links, theme button, account menu).
// Phones: the top has the logo, the search and a burger button (☰) that opens a panel with every link; the four most
// used links are also on a bar at the bottom, whose "Daha fazla" button opens the same panel.

const MAIN_LINKS = [
  { key: "home", href: "home.html", icon: "bi-house-door-fill", label: "Ana Sayfa" },
  { key: "friends", href: "friends.html", icon: "bi-people-fill", label: "Arkadaşlar" },
  { key: "notifications", href: "notifications.html", icon: "bi-bell-fill", label: "Bildirimler", badge: "badge-count" },
  { key: "messages", href: "messages.html", icon: "bi-chat-dots-fill", label: "Mesajlar", badge: "message-badge-count" },
  { key: "groups", href: "groups.html", icon: "bi-diagram-3-fill", label: "Gruplar" },
  { key: "marketplace", href: "marketplace.html", icon: "bi-shop", label: "Marketplace" },
  { key: "events", href: "events.html", icon: "bi-calendar-event", label: "Etkinlikler" }
];

const ACCOUNT_LINKS = [
  { key: "profile", href: "profile.html", icon: "bi-person", label: "Profilim" },
  { key: "saved", href: "saved.html", icon: "bi-bookmark", label: "Kaydedilenler" },
  { key: "settings", href: "settings.html", icon: "bi-gear", label: "Ayarlar" }
];

// Which main links also get a place on the phone's bottom bar (the panel has all of them).
const PHONE_BAR_KEYS = ["home", "friends", "notifications", "messages"];

// The red counter on a link; navbar-badge.js fills every element with the badge's class.
function navBadgeHtml(badgeClass) {
  return badgeClass ? `<span class="position-absolute top-0 end-0 badge rounded-pill bg-danger ${badgeClass} d-none">0</span>` : "";
}

function navLinkHtml(link, active, extraClass = "") {
  return `<a href="${link.href}" class="nav-pill-link ${extraClass} ${active === link.key ? "active" : ""}"><i class="bi ${link.icon}"></i>${link.label}${navBadgeHtml(link.badge)}</a>`;
}

function accountMenuItemHtml(link, active) {
  return `<li><a class="dropdown-item ${active === link.key ? "active" : ""}" href="${link.href}"><i class="bi ${link.icon} me-2"></i>${link.label}</a></li>`;
}

function topMenuHtml(active) {
  return `
    <div class="container-fluid px-3 px-md-4">
      <a class="navbar-brand site-brand" href="home.html">SocialNet</a>

      <form class="d-flex align-items-center bg-body-tertiary rounded-pill px-3 py-2 mx-2 mx-md-3 nav-search-wrap" onsubmit="return submitSearch(this)">
        <i class="bi bi-search text-muted me-2"></i>
        <input type="text" id="searchInput" class="form-control form-control-sm border-0 bg-transparent p-0 shadow-none" placeholder="Kullanıcı ara..." name="q">
      </form>

      <button type="button" class="burger-btn d-md-none" data-bs-toggle="offcanvas" data-bs-target="#siteMenu" aria-label="Menü"><i class="bi bi-list"></i></button>

      <div class="d-none d-md-flex align-items-center gap-1 ms-auto">
        ${MAIN_LINKS.map(link => navLinkHtml(link, active)).join("")}
        <button type="button" class="theme-toggle-btn" onclick="toggleTheme()" title="Temayı Değiştir"><i class="bi bi-moon-stars-fill"></i></button>
        <div class="dropdown ms-2">
          <button class="btn p-0 border-0 bg-transparent position-relative" data-bs-toggle="dropdown">
            <img src="images/default-avatar.png" class="avatar-xs nav-profile-avatar" alt="Profilim">
            <span class="position-absolute bg-success border border-2 border-white rounded-circle" style="width:11px;height:11px;bottom:0;right:0;" title="Çevrimiçi"></span>
          </button>
          <ul class="dropdown-menu dropdown-menu-end">
            ${ACCOUNT_LINKS.map(link => accountMenuItemHtml(link, active)).join("")}
            <li><hr class="dropdown-divider"></li>
            <li><a class="dropdown-item text-danger" href="#" onclick="logout(); return false;"><i class="bi bi-box-arrow-right me-2"></i>Çıkış Yap</a></li>
          </ul>
        </div>
      </div>
    </div>`;
}

// The bottom bar of a phone. "Daha fazla" lights up when the current page is not one of the four links on the bar.
function phoneBarHtml(active) {
  const tabs = PHONE_BAR_KEYS
    .map(key => MAIN_LINKS.find(link => link.key === key))
    .map(link => navLinkHtml(link, active, "phone-tab"))
    .join("");
  const moreIsActive = active !== "" && !PHONE_BAR_KEYS.includes(active);

  return `
    <div class="phone-tabbar d-md-none bg-body border-top">
      ${tabs}
      <button type="button" class="nav-pill-link phone-tab border-0 bg-transparent ${moreIsActive ? "active" : ""}" data-bs-toggle="offcanvas" data-bs-target="#siteMenu">
        <i class="bi bi-three-dots"></i>Daha fazla
      </button>
    </div>`;
}

// The burger panel: every link, in a drawer that slides in from the right. The unread counters sit on their rows.
function menuPanelHtml(active) {
  const counter = badgeClass => badgeClass ? `<span class="badge rounded-pill bg-danger ms-auto ${badgeClass} d-none">0</span>` : "";
  const row = link => `<a href="${link.href}" class="list-group-item list-group-item-action d-flex align-items-center gap-3 ${active === link.key ? "active" : ""}"><i class="bi ${link.icon}"></i>${link.label}${counter(link.badge)}</a>`;
  const me = getSession();

  return `
    <div class="offcanvas offcanvas-end d-md-none" tabindex="-1" id="siteMenu">
      <div class="offcanvas-header">
        <a href="profile.html" class="d-flex align-items-center gap-2 fw-bold min-w-0">
          <img src="images/default-avatar.png" class="avatar-sm nav-profile-avatar" alt="">
          <span class="text-truncate">${escapeHtml(me?.fullName || "Profilim")}</span>
        </a>
        <button type="button" class="btn-close" data-bs-dismiss="offcanvas" aria-label="Kapat"></button>
      </div>
      <div class="offcanvas-body pt-0">
        <div class="list-group list-group-flush">
          ${MAIN_LINKS.map(row).join("")}
          <div class="border-top my-2"></div>
          ${ACCOUNT_LINKS.map(row).join("")}
          <button type="button" class="list-group-item list-group-item-action d-flex align-items-center gap-3" onclick="toggleTheme()"><i class="bi bi-moon-stars-fill theme-icon"></i>Temayı Değiştir</button>
          <a href="#" class="list-group-item list-group-item-action d-flex align-items-center gap-3 text-danger" onclick="logout(); return false;"><i class="bi bi-box-arrow-right"></i>Çıkış Yap</a>
        </div>
      </div>
    </div>`;
}

(function renderSiteNav() {
  const nav = document.getElementById("siteNav");
  if (!nav) return;

  const active = nav.dataset.active || "";
  nav.className = "navbar navbar-expand bg-body border-bottom sticky-top py-2";
  nav.innerHTML = topMenuHtml(active);
  document.body.insertAdjacentHTML("beforeend", phoneBarHtml(active) + menuPanelHtml(active));
})();
