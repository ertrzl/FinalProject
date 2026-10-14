// The menu of every signed-in page, in one place: a new link is added here once, and the phone layout is fixed once.
//
// A page only has   <nav id="siteNav" data-active="home"></nav>   where the menu goes, and loads this file before app.js.
// data-active names the section the page belongs to: home, explore, friends, notifications, messages, groups, marketplace, events
// (the main links) or profile, saved, settings (the account links); leave it empty on any other page.
//
// The menu is drawn for the chosen design (js/design.js):
//   inbox    a column of text links on the left (wide screens), with the search, "Gruplarım" and the account at its ends;
//            below 992 px a top bar (logo, search, burger) and the bottom bar of the phone
//   classic  the old top menu (logo, search, links, theme button, account menu); the phone bar below 768 px
// Phones: the top has the logo, the search and a burger button (☰) that opens a panel with every link; the four most
// used links are also on a bar at the bottom, whose "Daha fazla" button opens the same panel.

const MAIN_LINKS = [
  { key: "home", href: "home.html", icon: "bi-house-door-fill", label: "Ana Sayfa" },
  { key: "explore", href: "explore.html", icon: "bi-compass-fill", label: "Keşfet" },
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

// Only for an admin (the sign-in answer carries isAdmin; the server checks the role on every admin request anyway).
const ADMIN_LINK = { key: "admin", href: "admin.html", icon: "bi-shield-check", label: "Yönetim" };

function isAdmin() {
  return !!getSession()?.isAdmin;
}

function accountLinks() {
  return isAdmin() ? [...ACCOUNT_LINKS, ADMIN_LINK] : ACCOUNT_LINKS;
}

// Which main links also get a place on the phone's bottom bar (the panel has all of them).
const PHONE_BAR_KEYS = ["home", "friends", "notifications", "messages"];

// The inbox design: the order of the column on the left, and the keys that press with "g" (js/shortcuts.js reads data-shortcut).
const SIDE_LINKS = [
  { key: "home", shortcut: "a" }, { key: "explore", shortcut: "k" }, { key: "friends", shortcut: "r" },
  { key: "messages", shortcut: "m" }, { key: "notifications", shortcut: "b" }, { key: "groups", shortcut: "g" },
  { key: "events", shortcut: "e" }, { key: "marketplace", shortcut: "p" }, { key: "saved", shortcut: "s" }, { key: "settings", shortcut: "t" }
];

// The width where the menu turns into the phone layout: inbox has a column on the left from "lg", classic a top menu from "md".
const MENU_BREAKPOINT = getDesign() === "inbox" ? "lg" : "md";

function findLink(key) {
  return MAIN_LINKS.find(link => link.key === key) || accountLinks().find(link => link.key === key);
}

// The red counter on a link; navbar-badge.js fills every element with the badge's class.
function navBadgeHtml(badgeClass) {
  return badgeClass ? `<span class="position-absolute top-0 end-0 badge rounded-pill bg-danger ${badgeClass} d-none">0</span>` : "";
}

function navLinkHtml(link, active, extraClass = "") {
  return `<a href="${link.href}" title="${link.label}" data-section="${link.key}" class="nav-pill-link ${extraClass} ${active === link.key ? "active" : ""}"><i class="bi ${link.icon}"></i><span class="nav-label">${link.label}</span>${navBadgeHtml(link.badge)}</a>`;
}

function accountMenuItemHtml(link, active) {
  return `<li><a class="dropdown-item ${active === link.key ? "active" : ""}" href="${link.href}"><i class="bi ${link.icon} me-2"></i>${link.label}</a></li>`;
}

function searchFormHtml(extraClass = "") {
  return `
      <form class="d-flex align-items-center bg-body-tertiary rounded-pill px-3 py-2 nav-search-wrap ${extraClass}" onsubmit="return submitSearch(this)">
        <i class="bi bi-search text-muted me-2"></i>
        <input type="text" class="form-control form-control-sm border-0 bg-transparent p-0 shadow-none" placeholder="Kullanıcı ara..." name="q" data-search-input>
      </form>`;
}

function topMenuHtml(active) {
  return `
    <div class="container-fluid px-3 px-md-4">
      <a class="navbar-brand site-brand" href="home.html">SocialNet</a>

      ${searchFormHtml("mx-2 mx-md-3")}

      <button type="button" class="burger-btn d-md-none" data-bs-toggle="offcanvas" data-bs-target="#siteMenu" aria-label="Menü"><i class="bi bi-list"></i></button>

      <div class="d-none d-md-flex align-items-center gap-1 ms-auto">
        ${MAIN_LINKS.map(link => navLinkHtml(link, active)).join("")}
        <button type="button" class="theme-toggle-btn" onclick="toggleTheme()" title="Temayı Değiştir"><i class="bi bi-moon-stars-fill"></i></button>        <div class="dropdown ms-2">
          <button class="btn p-0 border-0 bg-transparent position-relative" data-bs-toggle="dropdown">
            <img src="images/default-avatar.png" class="avatar-xs nav-profile-avatar" alt="Profilim">
            <span class="position-absolute bg-success border border-2 border-white rounded-circle" style="width:11px;height:11px;bottom:0;right:0;" title="Çevrimiçi"></span>
          </button>
          <ul class="dropdown-menu dropdown-menu-end">
            ${accountLinks().map(link => accountMenuItemHtml(link, active)).join("")}
            <li><hr class="dropdown-divider"></li>
            <li><a class="dropdown-item text-danger" href="#" onclick="logout(); return false;"><i class="bi bi-box-arrow-right me-2"></i>Çıkış Yap</a></li>
          </ul>
        </div>
      </div>
    </div>`;
}

// ---- Inbox design ----

// A row of the column on the left: the name, a counter if the link has one, and (on the page you are on) the key that jumps to it.
function sideLinkHtml(item, active) {
  const link = findLink(item.key);
  const isActive = active === item.key;
  const counter = link.badge ? `<span class="badge bg-danger ${link.badge} d-none">0</span>` : "";
  const hint = isActive ? `<span class="kbd" title="g, sonra ${item.shortcut}">g ${item.shortcut}</span>` : "";
  const label = link.key === "marketplace" ? "Pazar" : link.label; // the design calls it "Pazar" (the classic menu says Marketplace)
  return `<a href="${link.href}" data-section="${link.key}" data-shortcut="${item.shortcut}" class="side-link ${isActive ? "active" : ""}"><span>${label}</span><span class="side-link-end">${counter}${hint}</span></a>`;
}

function sideMenuHtml(active) {
  const me = getSession();
  return `
    <div class="side-menu d-none d-lg-flex">
      <a class="side-brand" href="home.html"><span class="side-brand-mark"></span>SocialNet</a>
      ${searchFormHtml("side-search")}
      <div class="side-links">${SIDE_LINKS.map(item => sideLinkHtml(item, active)).join("")}${isAdmin() ? sideLinkHtml({ key: "admin", shortcut: "y" }, active) : ""}</div>
      <div class="side-groups d-none" id="sideGroups">
        <div class="side-caption">Gruplarım</div>
        <div id="sideGroupsList"></div>
      </div>
      <div class="side-grow"></div>
      <div class="side-account">
        <a href="profile.html" class="side-me ${active === "profile" ? "active" : ""}" title="Profilim">
          <img src="images/default-avatar.png" class="avatar-xs nav-profile-avatar" alt="">
          <span class="side-me-name">${escapeHtml(me?.fullName || "Profilim")}</span>
        </a>
        <button type="button" class="side-tool" onclick="toggleTheme()" title="Temayı Değiştir"><i class="bi bi-moon-stars-fill theme-icon"></i></button>
        <button type="button" class="side-tool" onclick="logout()" title="Çıkış Yap"><i class="bi bi-box-arrow-right"></i></button>
      </div>
    </div>
    <div class="top-bar d-flex d-lg-none">
      <a class="side-brand" href="home.html"><span class="side-brand-mark"></span>SocialNet</a>
      ${searchFormHtml("top-bar-search")}
      <button type="button" class="burger-btn" data-bs-toggle="offcanvas" data-bs-target="#siteMenu" aria-label="Menü"><i class="bi bi-list"></i></button>
    </div>`;
}

// "Gruplarım": the groups the person is in, under the links. A person with no group sees nothing there.
async function loadSideGroups() {
  const box = document.getElementById("sideGroups");
  if (!box) return;

  try {
    const groups = await apiFetch("/api/groups/mine");
    if (!groups.length) return;

    document.getElementById("sideGroupsList").innerHTML = groups.slice(0, 6)
      .map(g => `<a href="group.html?id=${g.id}" class="side-link" data-no-i18n><span>${escapeHtml(g.name)}</span></a>`).join("");
    box.classList.remove("d-none");
  } catch (err) {
    // Silent: the column works without the list.
  }
}

// The bottom bar of a phone. "Daha fazla" lights up when the current page is not one of the four links on the bar.
function phoneBarHtml(active) {
  const tabs = PHONE_BAR_KEYS
    .map(key => MAIN_LINKS.find(link => link.key === key))
    .map(link => navLinkHtml(link, active, "phone-tab"))
    .join("");
  const moreIsActive = active !== "" && !PHONE_BAR_KEYS.includes(active);

  return `
    <div class="phone-tabbar d-${MENU_BREAKPOINT}-none bg-body border-top">
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
    <div class="offcanvas offcanvas-end d-${MENU_BREAKPOINT}-none" tabindex="-1" id="siteMenu">
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
          ${accountLinks().map(row).join("")}
          <button type="button" class="list-group-item list-group-item-action d-flex align-items-center gap-3" onclick="toggleTheme()"><i class="bi bi-moon-stars-fill theme-icon"></i>Temayı Değiştir</button>          <a href="#" class="list-group-item list-group-item-action d-flex align-items-center gap-3 text-danger" onclick="logout(); return false;"><i class="bi bi-box-arrow-right"></i>Çıkış Yap</a>
        </div>
      </div>
    </div>`;
}

(function renderSiteNav() {
  const nav = document.getElementById("siteNav");
  if (!nav) return;

  const active = nav.dataset.active || "";
  if (getDesign() === "inbox") {
    nav.className = "inbox-nav";
    nav.innerHTML = sideMenuHtml(active);
    loadSideGroups();
  } else {
    nav.className = "navbar navbar-expand bg-body border-bottom sticky-top py-2";
    nav.innerHTML = topMenuHtml(active);
  }
  document.body.insertAdjacentHTML("beforeend", phoneBarHtml(active) + menuPanelHtml(active));
})();
