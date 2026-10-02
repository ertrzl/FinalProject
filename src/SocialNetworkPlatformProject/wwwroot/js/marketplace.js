// Marketplace page — browse/filter/sort/page listings, "İlanlarım", "Kaydedilenler", multi-photo sell/edit form,
// photo carousel, mark as sold and the seller's other listings; all wired to the backend.

requireAuth();

const PAGE_SIZE = 12;
const MAX_PHOTOS = 5;
const ALL_CATEGORIES = "Tümü";

let categories = [];
let activeCategory = ALL_CATEGORIES;
let currentView = "all"; // "all" | "mine" | "saved"
let currentListings = [];
let currentPage = 1;
let totalCount = 0;
let selectedListing = null;
let editingListingId = null;
let requestSeq = 0; // drops responses of superseded requests (fast typing in the filters)
let filterDebounce = null;

// Every listing the page has seen (grid, saved, seller's others), so a card can always open its modal.
const listingIndex = new Map();

// "Tekliflerim": negotiations on my listings (received) and the ones I started (sent).
let offersSide = "received"; // "received" | "sent"
const offerLists = { received: [], sent: [] };
let offerModalState = null; // { kind: "new" | "counter", id, low, high } — what the offer modal is currently doing

// Sell/edit form photo state: photos already on the listing, and new files waiting to be uploaded.
let existingPhotos = [];
let removedPhotoIds = [];
let pendingPhotos = []; // { file, url }

function formatPrice(n) {
  return n.toLocaleString("tr-TR", { maximumFractionDigits: 2 });
}

function priceLabel(n) {
  return formatPrice(n) + " ₺";
}

function isSold(item) {
  return item.status === "Sold";
}

function isOwnListing(item) {
  const session = getSession();
  return !!session && item.sellerId === session.userId;
}

function coverOf(item) {
  return item.imageUrl || "https://picsum.photos/seed/" + item.id + "/500/400";
}

function remember(items) {
  items.forEach(item => listingIndex.set(item.id, item));
}

// ---- Categories (single source of truth is the backend) ----

async function loadCategories() {
  try {
    categories = await apiFetch("/api/marketplace/categories");
  } catch (err) {
    categories = [];
  }
  renderCategories();
  document.getElementById("sellCategory").innerHTML = categories.map(c => `<option>${escapeHtml(c)}</option>`).join("");
}

function renderCategories() {
  const wrap = document.getElementById("categoryList");
  wrap.innerHTML = [ALL_CATEGORIES, ...categories]
    .map(cat => `<button type="button" class="btn btn-sm text-start rounded-pill ${cat === activeCategory ? "btn-primary" : "btn-light"}" onclick="setCategory('${cat}')">${escapeHtml(cat)}</button>`)
    .join("");
}

function setCategory(cat) {
  activeCategory = cat;
  renderCategories();
  renderListings();
}

// ---- Filters ----

function onFilterInput() {
  clearTimeout(filterDebounce);
  filterDebounce = setTimeout(renderListings, 350);
}

function resetFilterInputs() {
  document.getElementById("marketSearch").value = "";
  document.getElementById("filterMinPrice").value = "";
  document.getElementById("filterMaxPrice").value = "";
  document.getElementById("filterLocation").value = "";
  document.getElementById("filterSort").value = "newest";
  activeCategory = ALL_CATEGORIES;
  renderCategories();
}

function clearFilters() {
  resetFilterInputs();
  renderListings();
}

function buildBrowseParams(page) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) });
  const search = document.getElementById("marketSearch").value.trim();
  const minPrice = document.getElementById("filterMinPrice").value;
  const maxPrice = document.getElementById("filterMaxPrice").value;
  const location = document.getElementById("filterLocation").value.trim();

  if (activeCategory !== ALL_CATEGORIES) params.set("category", activeCategory);
  if (search) params.set("search", search);
  if (minPrice !== "") params.set("minPrice", minPrice);
  if (maxPrice !== "") params.set("maxPrice", maxPrice);
  if (location) params.set("location", location);
  params.set("sort", document.getElementById("filterSort").value);
  return params;
}

// ---- Listing grid ----

function heartButtonHtml(item) {
  if (isOwnListing(item)) return "";
  return `<button type="button" class="btn btn-sm btn-light rounded-circle position-absolute top-0 end-0 m-2 shadow-sm" data-heart-id="${item.id}" onclick="toggleSave('${item.id}', event)" title="Kaydet">
            <i class="bi ${item.isSavedByCurrentUser ? "bi-heart-fill text-danger" : "bi-heart"}"></i>
          </button>`;
}

function listingCardHtml(item) {
  const sold = isSold(item);
  return `
    <div class="col-6 col-md-4">
      <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100 position-relative ${sold ? "opacity-75" : ""}" role="button" onclick="openListing('${item.id}')">
        ${sold ? `<span class="badge text-bg-secondary position-absolute top-0 start-0 m-2">Satıldı</span>` : ""}
        ${heartButtonHtml(item)}
        <img src="${coverOf(item)}" class="w-100" style="height:140px;object-fit:cover;" alt="">
        <div class="p-2">
          <div class="fw-bold text-primary">${priceLabel(item.price)}${item.openOfferCount > 0 ? ` <span class="badge text-bg-warning fw-normal">${item.openOfferCount} teklif</span>` : ""}</div>
          <div class="small text-truncate">${escapeHtml(item.title)}</div>
          <div class="text-muted small text-truncate"><i class="bi bi-geo-alt"></i> ${escapeHtml(item.location || "Belirtilmedi")}</div>
        </div>
      </div>
    </div>`;
}

function renderGrid() {
  const grid = document.getElementById("listingsGrid");
  const emptyText = {
    all: "Sonuç bulunamadı.",
    mine: "Henüz bir ilan yayınlamadın.",
    saved: "Henüz kaydettiğin bir ilan yok. Kartlardaki kalbe tıklayarak ilan kaydedebilirsin."
  }[currentView];

  grid.innerHTML = currentListings.length
    ? currentListings.map(listingCardHtml).join("")
    : `<div class="col-12 text-center text-muted py-5">${emptyText}</div>`;

  document.getElementById("resultCount").textContent = (currentView === "all" ? totalCount : currentListings.length) + " ilan";
  document.getElementById("loadMoreWrap").classList.toggle("d-none", currentView !== "all" || currentListings.length >= totalCount);
}

async function renderListings() {
  // Listing actions (delete, sold, edit) can also be reached from the offers tab, which has no grid to redraw.
  if (currentView === "offers") return loadOffers();

  const seq = ++requestSeq;
  const grid = document.getElementById("listingsGrid");

  try {
    if (currentView === "all") {
      currentPage = 1;
      const result = await apiFetch(`/api/marketplace?${buildBrowseParams(1).toString()}`);
      if (seq !== requestSeq) return;
      currentListings = result.items;
      totalCount = result.totalCount;
    } else {
      const items = await apiFetch(currentView === "mine" ? "/api/marketplace/mine" : "/api/marketplace/saved");
      if (seq !== requestSeq) return;
      currentListings = items;
      totalCount = items.length;
    }
    remember(currentListings);
    renderGrid();
  } catch (err) {
    if (seq !== requestSeq) return;
    grid.innerHTML = `<div class="col-12"><div class="alert alert-danger small">${escapeHtml(err.message)}</div></div>`;
    document.getElementById("loadMoreWrap").classList.add("d-none");
  }
}

async function loadMore() {
  const btn = document.getElementById("loadMoreBtn");
  btn.disabled = true;
  const seq = requestSeq;

  try {
    const result = await apiFetch(`/api/marketplace?${buildBrowseParams(currentPage + 1).toString()}`);
    if (seq !== requestSeq) return; // filters changed while loading
    currentPage += 1;
    currentListings = currentListings.concat(result.items);
    totalCount = result.totalCount;
    remember(result.items);
    renderGrid();
  } catch (err) {
    toast(err.message || "İlanlar yüklenemedi.");
  } finally {
    btn.disabled = false;
  }
}

function setView(view) {
  if (view === currentView) return;
  currentView = view;

  for (const [name, id] of [["all", "tabAll"], ["mine", "tabMine"], ["saved", "tabSaved"], ["offers", "tabOffers"]]) {
    const tab = document.getElementById(id);
    tab.classList.toggle("active", view === name);
    tab.classList.toggle("text-muted", view !== name);
  }
  const offersView = view === "offers";
  document.getElementById("browseFilters").classList.toggle("d-none", view !== "all");
  document.getElementById("offersPanel").classList.toggle("d-none", !offersView);
  document.getElementById("listingsGrid").classList.toggle("d-none", offersView);
  document.getElementById("loadMoreWrap").classList.add("d-none");
  document.getElementById("resultCount").textContent = "";

  if (offersView) loadOffers();
  else renderListings();
}

// ---- Favourites ----

function updateHeart(id, saved) {
  document.querySelectorAll(`[data-heart-id="${id}"] i`).forEach(icon => {
    icon.className = "bi " + (saved ? "bi-heart-fill text-danger" : "bi-heart");
  });
  if (selectedListing && selectedListing.id === id) renderModalHeart(selectedListing);
}

async function toggleSave(id, event) {
  event.stopPropagation();
  try {
    const saved = await apiFetch(`/api/marketplace/${id}/save`, { method: "POST" });
    const item = listingIndex.get(id);
    if (item) item.isSavedByCurrentUser = saved;

    if (currentView === "saved" && !saved) {
      currentListings = currentListings.filter(l => l.id !== id);
      renderGrid();
    }
    updateHeart(id, saved);
    toast(saved ? "İlan kaydedildi." : "Kayıt kaldırıldı.");
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

// ---- Detail modal ----

function renderCarousel(item) {
  const urls = item.images && item.images.length ? item.images.map(i => i.url) : [coverOf(item)];
  document.getElementById("listingCarouselInner").innerHTML = urls
    .map((url, index) => `<div class="carousel-item ${index === 0 ? "active" : ""}"><img src="${url}" class="d-block w-100" style="height:280px;object-fit:cover;" alt=""></div>`)
    .join("");
  document.querySelectorAll("#listingCarousel .carousel-control-prev, #listingCarousel .carousel-control-next")
    .forEach(btn => btn.classList.toggle("d-none", urls.length < 2));
  bootstrap.Carousel.getOrCreateInstance(document.getElementById("listingCarousel")).to(0);
}

function renderModalHeart(item) {
  const btn = document.getElementById("listingModalSaveBtn");
  btn.classList.toggle("d-none", isOwnListing(item));
  btn.querySelector("i").className = "bi " + (item.isSavedByCurrentUser ? "bi-heart-fill text-danger" : "bi-heart");
}

function openListing(id) {
  const item = listingIndex.get(id);
  if (!item) return;
  selectedListing = item;

  document.getElementById("listingModalTitle").textContent = item.title;
  renderCarousel(item);
  document.getElementById("listingModalPrice").textContent = priceLabel(item.price);
  document.getElementById("listingModalSoldBadge").classList.toggle("d-none", !isSold(item));
  renderModalHeart(item);
  document.getElementById("listingModalMeta").innerHTML = `<i class="bi bi-geo-alt"></i> ${escapeHtml(item.location || "Belirtilmedi")} · ${escapeHtml(item.category)}`;
  document.getElementById("listingModalDescription").textContent = item.description || "";
  document.getElementById("listingModalSellerAvatar").src = item.sellerAvatarUrl || DEFAULT_AVATAR;
  document.getElementById("listingModalSellerName").textContent = item.sellerName;
  document.getElementById("listingModalSellerLink").href = `profile.html?id=${item.sellerId}`;

  const mine = isOwnListing(item);
  const messageLink = document.getElementById("listingModalMessageLink");
  messageLink.classList.toggle("d-none", mine || isSold(item));
  messageLink.href = `messages.html?userId=${item.sellerId}`;

  const ownerActions = document.getElementById("listingOwnerActions");
  ownerActions.classList.toggle("d-none", !mine);
  ownerActions.classList.toggle("d-flex", mine);
  document.getElementById("listingModalStatusBtn").innerHTML = isSold(item)
    ? `<i class="bi bi-arrow-counterclockwise me-1"></i>Tekrar Satışa Çıkar`
    : `<i class="bi bi-check2-circle me-1"></i>Satıldı Olarak İşaretle`;

  renderModalOfferButtons(item, mine);
  loadSellerOtherListings(item);
  bootstrap.Modal.getOrCreateInstance(document.getElementById("listingModal")).show();
}

function renderModalOfferButtons(item, mine) {
  const offerBtn = document.getElementById("listingModalOfferBtn");
  const ownerBtn = document.getElementById("listingModalOwnerOffersBtn");

  const canBuy = !mine && !isSold(item);
  offerBtn.classList.toggle("d-none", !canBuy);
  offerBtn.innerHTML = item.myOpenOfferId
    ? `<i class="bi bi-tag me-1"></i>Teklifini Gör`
    : `<i class="bi bi-tag me-1"></i>Teklif Ver`;

  const pending = mine && item.openOfferCount > 0;
  ownerBtn.classList.toggle("d-none", !pending);
  if (pending) ownerBtn.innerHTML = `<i class="bi bi-inbox me-1"></i>Gelen Teklifler (${item.openOfferCount})`;
}

function onOfferButton() {
  if (!selectedListing) return;
  if (selectedListing.myOpenOfferId) goToOffers("sent");
  else openOfferModal({ kind: "new", id: selectedListing.id, title: selectedListing.title, asking: selectedListing.price });
}

async function loadSellerOtherListings(item) {
  const section = document.getElementById("sellerOtherSection");
  const list = document.getElementById("sellerOtherList");
  section.classList.add("d-none");
  list.innerHTML = "";

  try {
    const result = await apiFetch(`/api/marketplace?sellerId=${item.sellerId}&pageSize=7`);
    if (!selectedListing || selectedListing.id !== item.id) return; // another listing was opened meanwhile
    const others = result.items.filter(l => l.id !== item.id).slice(0, 6);
    if (!others.length) return;

    remember(others);
    list.innerHTML = others.map(o => `
      <div class="col-4 col-md-2">
        <div class="card border-0 shadow-sm rounded-3 overflow-hidden h-100" role="button" onclick="openListing('${o.id}')">
          <img src="${coverOf(o)}" class="w-100" style="height:70px;object-fit:cover;" alt="">
          <div class="p-1">
            <div class="fw-bold text-primary small">${priceLabel(o.price)}</div>
            <div class="text-truncate" style="font-size:.75rem;">${escapeHtml(o.title)}</div>
          </div>
        </div>
      </div>`).join("");
    section.classList.remove("d-none");
  } catch (err) {
    // The extra section is a nicety; the listing itself is already shown.
  }
}

function closeListingModal() {
  bootstrap.Modal.getInstance(document.getElementById("listingModal"))?.hide();
}

async function deleteListing() {
  if (!selectedListing || !confirm("Bu ilanı kaldırmak istediğine emin misin?")) return;
  try {
    await apiFetch(`/api/marketplace/${selectedListing.id}`, { method: "DELETE" });
    listingIndex.delete(selectedListing.id);
    closeListingModal();
    toast("İlan kaldırıldı.");
    await renderListings();
  } catch (err) {
    toast(err.message || "İlan kaldırılamadı.");
  }
}

async function toggleListingSold() {
  if (!selectedListing) return;
  const status = isSold(selectedListing) ? "Active" : "Sold";
  try {
    await apiFetch(`/api/marketplace/${selectedListing.id}/status`, { method: "PUT", body: { status } });
    closeListingModal();
    toast(status === "Sold" ? "İlan satıldı olarak işaretlendi." : "İlan tekrar satışa çıktı.");
    await renderListings();
  } catch (err) {
    toast(err.message || "Durum değiştirilemedi.");
  }
}

// ---- Sell / edit form (one modal for both) ----

function totalPhotoCount() {
  return existingPhotos.length + pendingPhotos.length;
}

function renderSellPhotos() {
  const thumb = (url, onRemove) => `
    <div class="position-relative" style="width:76px;height:76px;">
      <img src="${url}" class="rounded-3 w-100 h-100" style="object-fit:cover;" alt="">
      <button type="button" class="btn btn-dark btn-sm rounded-circle position-absolute top-0 end-0 p-0 d-flex align-items-center justify-content-center"
              style="width:22px;height:22px;transform:translate(35%,-35%);" onclick="${onRemove}"><i class="bi bi-x"></i></button>
    </div>`;

  document.getElementById("sellPhotoGrid").innerHTML =
    existingPhotos.map(p => thumb(p.url, `removeExistingPhoto('${p.id}')`)).join("") +
    pendingPhotos.map((p, index) => thumb(p.url, `removePendingPhoto(${index})`)).join("");

  document.getElementById("sellPhotoCounter").textContent = `(${totalPhotoCount()}/${MAX_PHOTOS})`;
  document.getElementById("sellAddPhotoLabel").classList.toggle("d-none", totalPhotoCount() >= MAX_PHOTOS);
}

function addSellPhotos(input) {
  const room = MAX_PHOTOS - totalPhotoCount();
  const files = Array.from(input.files || []);
  if (files.length > room) toast(`En fazla ${MAX_PHOTOS} fotoğraf ekleyebilirsin.`);

  files.slice(0, Math.max(room, 0)).forEach(file => pendingPhotos.push({ file, url: URL.createObjectURL(file) }));
  input.value = "";
  renderSellPhotos();
}

function removePendingPhoto(index) {
  URL.revokeObjectURL(pendingPhotos[index].url);
  pendingPhotos.splice(index, 1);
  renderSellPhotos();
}

function removeExistingPhoto(id) {
  existingPhotos = existingPhotos.filter(p => p.id !== id);
  removedPhotoIds.push(id);
  renderSellPhotos();
}

function resetSellForm() {
  editingListingId = null;
  pendingPhotos.forEach(p => URL.revokeObjectURL(p.url));
  pendingPhotos = [];
  existingPhotos = [];
  removedPhotoIds = [];

  document.getElementById("sellModalTitle").textContent = "Ürün Sat";
  document.getElementById("sellSubmitBtn").textContent = "Yayınla";
  document.getElementById("sellTitle").value = "";
  document.getElementById("sellPrice").value = "";
  document.getElementById("sellCategory").selectedIndex = 0;
  document.getElementById("sellLocation").value = "";
  document.getElementById("sellDescription").value = "";
  document.getElementById("sellImageInput").value = "";
  document.getElementById("sellError").classList.add("d-none");
  renderSellPhotos();
}

function openEditListing() {
  if (!selectedListing) return;
  const item = selectedListing;

  resetSellForm();
  editingListingId = item.id;
  existingPhotos = (item.images || []).map(i => ({ id: i.id, url: i.url }));
  document.getElementById("sellModalTitle").textContent = "İlanı Düzenle";
  document.getElementById("sellSubmitBtn").textContent = "Kaydet";
  document.getElementById("sellTitle").value = item.title;
  document.getElementById("sellPrice").value = item.price;
  document.getElementById("sellCategory").value = item.category;
  document.getElementById("sellLocation").value = item.location || "";
  document.getElementById("sellDescription").value = item.description || "";
  renderSellPhotos();

  // Two Bootstrap modals can't stack: let the detail modal finish closing, then open the form.
  document.getElementById("listingModal").addEventListener("hidden.bs.modal", () => {
    bootstrap.Modal.getOrCreateInstance(document.getElementById("sellModal")).show();
  }, { once: true });
  closeListingModal();
}

async function submitListing() {
  const titleInput = document.getElementById("sellTitle");
  const title = titleInput.value.trim();
  if (!title) {
    titleInput.classList.add("is-invalid");
    setTimeout(() => titleInput.classList.remove("is-invalid"), 1200);
    return;
  }

  const errorBox = document.getElementById("sellError");
  errorBox.classList.add("d-none");
  const submitBtn = document.getElementById("sellSubmitBtn");
  submitBtn.disabled = true;

  const isEdit = editingListingId !== null;

  try {
    const formData = new FormData();
    formData.append("title", title);
    formData.append("price", document.getElementById("sellPrice").value || "0");
    formData.append("category", document.getElementById("sellCategory").value);
    formData.append("location", document.getElementById("sellLocation").value.trim());
    formData.append("description", document.getElementById("sellDescription").value.trim());
    pendingPhotos.forEach(p => formData.append("images", p.file));
    if (isEdit) removedPhotoIds.forEach(id => formData.append("removeImageIds", id));

    await apiFetchForm(isEdit ? `/api/marketplace/${editingListingId}` : "/api/marketplace", {
      method: isEdit ? "PUT" : "POST",
      body: formData
    });

    bootstrap.Modal.getInstance(document.getElementById("sellModal"))?.hide();
    toast(isEdit ? "İlan güncellendi." : "İlanın yayınlandı!");

    // A fresh listing should be visible right away, whatever filters were active.
    if (!isEdit && currentView === "all") resetFilterInputs();
    await renderListings();
  } catch (err) {
    errorBox.textContent = err.message || "İlan kaydedilemedi.";
    errorBox.classList.remove("d-none");
  } finally {
    submitBtn.disabled = false;
  }
}

// Whenever the form closes (published, saved or cancelled) it goes back to a clean "new listing" state.
document.getElementById("sellModal").addEventListener("hidden.bs.modal", resetSellForm);

// ---- Offers: counter-offer bargaining ("Tekliflerim") ----

function myId() {
  return getSession().userId;
}

// A counter must land strictly between the other side's latest price and my own previous price
// (the asking price, for the seller's first counter) — same rule the backend enforces.
function counterWindow(o) {
  const mine = o.rounds.filter(r => r.proposerId === myId());
  const ownPrevious = mine.length ? mine[mine.length - 1].price : o.listingPrice;
  return { low: Math.min(o.currentPrice, ownPrevious), high: Math.max(o.currentPrice, ownPrevious) };
}

function offerStatusBadge(o) {
  if (o.status === "Open") {
    return o.isMyTurn
      ? `<span class="badge text-bg-warning">Senin sıran</span>`
      : `<span class="badge text-bg-info">Yanıt bekleniyor</span>`;
  }
  const label = { Accepted: ["success", "Anlaşıldı"], Rejected: ["danger", "Reddedildi"], Withdrawn: ["secondary", "Geri çekildi"], Closed: ["secondary", "İlan satıldı"] }[o.status];
  return `<span class="badge text-bg-${label[0]}">${label[1]}</span>`;
}

function offerActionsHtml(o) {
  if (o.isMyTurn) {
    const { low, high } = counterWindow(o);
    const canCounter = high - low > 0.01;
    return `
      <button class="btn btn-success btn-sm rounded-pill" onclick="respondOffer('${o.id}', 'accept')">Kabul Et (${priceLabel(o.currentPrice)})</button>
      <button class="btn btn-outline-primary btn-sm rounded-pill" ${canCounter ? "" : "disabled title=\"Karşı teklif için uygun aralık kalmadı\""} onclick="openCounterModal('${o.id}')">Karşı Teklif</button>
      <button class="btn btn-outline-danger btn-sm rounded-pill" onclick="respondOffer('${o.id}', 'reject')">Reddet</button>`;
  }
  if (o.canWithdraw) {
    return `<button class="btn btn-light border btn-sm rounded-pill" onclick="respondOffer('${o.id}', 'withdraw')">Teklifi Geri Çek</button>`;
  }
  return "";
}

function offerCardHtml(o) {
  const other = o.isCurrentUserBuyer
    ? { name: o.sellerName, avatar: o.sellerAvatarUrl, role: "Satıcı" }
    : { name: o.buyerName, avatar: o.buyerAvatarUrl, role: "Alıcı" };
  const who = proposerId => (proposerId === myId() ? "Sen" : other.name);
  const history = o.rounds.map(r => `<span class="${r.proposerId === myId() ? "text-primary" : "text-body"}">${escapeHtml(who(r.proposerId))}: ${priceLabel(r.price)}</span>`).join(` <i class="bi bi-arrow-right text-muted"></i> `);

  return `
    <div class="card border-0 shadow-sm rounded-4 p-3 mb-3" data-offer-id="${o.id}">
      <div class="d-flex gap-3">
        <img src="${o.listingImageUrl || "https://picsum.photos/seed/" + o.listingId + "/200/200"}" class="rounded-3 flex-shrink-0" style="width:84px;height:84px;object-fit:cover;" alt="" role="button" onclick="openListingById('${o.listingId}')">
        <div class="flex-grow-1 overflow-hidden">
          <div class="d-flex justify-content-between align-items-start gap-2">
            <a href="#" class="fw-bold text-body text-decoration-none text-truncate" onclick="openListingById('${o.listingId}'); return false;">${escapeHtml(o.listingTitle)}</a>
            ${offerStatusBadge(o)}
          </div>
          <div class="small text-muted">İstenen: ${priceLabel(o.listingPrice)}</div>
          <div class="d-flex align-items-center gap-2 small mt-1">
            <img src="${other.avatar || DEFAULT_AVATAR}" class="avatar-xs" alt="">
            <span>${other.role}: <a href="profile.html?id=${o.isCurrentUserBuyer ? o.sellerId : o.buyerId}" class="text-body fw-semibold text-decoration-none">${escapeHtml(other.name)}</a></span>
          </div>
          <div class="mt-2">
            <span class="fs-5 fw-bold text-success">${priceLabel(o.currentPrice)}</span>
            <span class="text-muted small ms-1">son teklif: ${escapeHtml(who(o.lastProposerId))}</span>
          </div>
          <div class="small mt-1 text-muted">${history}</div>
          <div class="d-flex flex-wrap gap-2 mt-2">${offerActionsHtml(o)}</div>
        </div>
      </div>
    </div>`;
}

function updateOffersChrome() {
  document.getElementById("offersCountReceived").textContent = offerLists.received.length;
  document.getElementById("offersCountSent").textContent = offerLists.sent.length;

  const waitingOnMe = [...offerLists.received, ...offerLists.sent].filter(o => o.isMyTurn).length;
  const badge = document.getElementById("offersBadge");
  badge.textContent = waitingOnMe;
  badge.classList.toggle("d-none", waitingOnMe === 0);
}

function renderOffers() {
  for (const [side, id] of [["received", "offersTabReceived"], ["sent", "offersTabSent"]]) {
    const tab = document.getElementById(id);
    tab.classList.toggle("active", offersSide === side);
    tab.classList.toggle("text-muted", offersSide !== side);
  }

  const list = document.getElementById("offersList");
  const items = offerLists[offersSide];
  const empty = offersSide === "received"
    ? "Henüz ilanlarına gelen bir teklif yok."
    : "Henüz bir ilana teklif vermedin. Bir ilanın detayından \"Teklif Ver\"i kullanabilirsin.";
  list.innerHTML = items.length ? items.map(offerCardHtml).join("") : `<div class="text-center text-muted py-5">${empty}</div>`;
}

let offersSideChosen = false;

async function loadOffers() {
  try {
    const [received, sent] = await Promise.all([
      apiFetch("/api/marketplace/offers/received"),
      apiFetch("/api/marketplace/offers/sent")
    ]);
    offerLists.received = received;
    offerLists.sent = sent;
    updateOffersChrome();

    // First time in: open the side that has something waiting on me.
    if (!offersSideChosen && currentView === "offers") {
      offersSideChosen = true;
      if (!received.some(o => o.isMyTurn) && sent.some(o => o.isMyTurn)) offersSide = "sent";
    }
    if (currentView === "offers") renderOffers();
  } catch (err) {
    if (currentView === "offers") {
      document.getElementById("offersList").innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
    }
  }
}

function setOffersSide(side) {
  offersSide = side;
  offersSideChosen = true;
  renderOffers();
}

function goToOffers(side) {
  offersSide = side;
  offersSideChosen = true;
  closeListingModal();
  if (currentView === "offers") renderOffers();
  else setView("offers");
}

async function openListingById(id) {
  try {
    remember([await apiFetch(`/api/marketplace/${id}`)]); // always fresh: status / offer hints may have moved on
    openListing(id);
  } catch (err) {
    toast("İlan bulunamadı ya da kaldırılmış olabilir.");
  }
}

// ---- Sharing: marketplace.html?listing=<id> opens that listing's modal straight away ----

function listingLink(id) {
  return `${window.location.origin}${window.location.pathname}?listing=${id}`;
}

async function copyListingLink() {
  if (!selectedListing) return;
  const link = listingLink(selectedListing.id);
  try {
    await navigator.clipboard.writeText(link);
    toast("İlan bağlantısı kopyalandı.");
  } catch (err) {
    // Clipboard access can be blocked (insecure origin, permissions): show the link so it can be copied by hand.
    window.prompt("Bağlantıyı kopyala:", link);
  }
}

// Offer modals can't stack on top of the listing detail modal: wait for it to close first.
function openModalAfterListingModal(modalId) {
  const listingEl = document.getElementById("listingModal");
  const show = () => bootstrap.Modal.getOrCreateInstance(document.getElementById(modalId)).show();
  if (listingEl.classList.contains("show")) {
    listingEl.addEventListener("hidden.bs.modal", show, { once: true });
    closeListingModal();
  } else {
    show();
  }
}

function openOfferModal(state) {
  offerModalState = state;
  const isNew = state.kind === "new";

  document.getElementById("offerModalTitle").textContent = isNew ? "Teklif Ver" : "Karşı Teklif";
  document.getElementById("offerModalListing").textContent = state.title;
  document.getElementById("offerModalHint").textContent = isNew
    ? `İstenen fiyat ${priceLabel(state.asking)}. Teklifin bundan yüksek olamaz.`
    : `${priceLabel(state.low)} ile ${priceLabel(state.high)} arasında (bu değerler hariç) bir fiyat gir.`;
  document.getElementById("offerPriceInput").value = "";
  document.getElementById("offerModalError").classList.add("d-none");
  openModalAfterListingModal("offerModal");
}

function openCounterModal(offerId) {
  const o = [...offerLists.received, ...offerLists.sent].find(x => x.id === offerId);
  if (!o) return;
  const { low, high } = counterWindow(o);
  openOfferModal({ kind: "counter", id: o.id, title: o.listingTitle, low, high });
}

async function submitOfferModal() {
  const state = offerModalState;
  if (!state) return;

  const errorBox = document.getElementById("offerModalError");
  const showError = text => { errorBox.textContent = text; errorBox.classList.remove("d-none"); };
  errorBox.classList.add("d-none");

  const price = Math.round(parseFloat(document.getElementById("offerPriceInput").value) * 100) / 100;
  if (!(price > 0)) return showError("Geçerli bir fiyat gir.");
  if (state.kind === "new" && price > state.asking) return showError("Teklifin istenen fiyattan yüksek olamaz.");
  if (state.kind === "counter" && !(price > state.low && price < state.high)) {
    return showError(`Fiyat ${priceLabel(state.low)} ile ${priceLabel(state.high)} arasında olmalı.`);
  }

  const submitBtn = document.getElementById("offerModalSubmit");
  submitBtn.disabled = true;
  try {
    const path = state.kind === "new" ? `/api/marketplace/${state.id}/offers` : `/api/marketplace/offers/${state.id}/counter`;
    const offer = await apiFetch(path, { method: "POST", body: { price } });

    bootstrap.Modal.getInstance(document.getElementById("offerModal"))?.hide();
    toast(state.kind === "new" ? "Teklifin gönderildi." : "Karşı teklifin gönderildi.");

    if (state.kind === "new") {
      const item = listingIndex.get(state.id);
      if (item) item.myOpenOfferId = offer.id;
      await loadOffers();
      goToOffers("sent");
    } else {
      await loadOffers();
    }
  } catch (err) {
    showError(err.message || "Teklif gönderilemedi.");
  } finally {
    submitBtn.disabled = false;
  }
}

async function respondOffer(offerId, action) {
  const o = [...offerLists.received, ...offerLists.sent].find(x => x.id === offerId);
  if (!o) return;

  const confirmText = {
    accept: `${priceLabel(o.currentPrice)} teklifini kabul etmek istediğine emin misin? İlan satıldı olarak işaretlenecek.`,
    reject: "Bu teklifi reddetmek istediğine emin misin?",
    withdraw: "Teklifini geri çekmek istediğine emin misin?"
  }[action];
  if (!confirm(confirmText)) return;

  try {
    await apiFetch(`/api/marketplace/offers/${offerId}/${action}`, { method: "POST" });
    toast({ accept: "Teklif kabul edildi. İlan satıldı.", reject: "Teklif reddedildi.", withdraw: "Teklifin geri çekildi." }[action]);

    // Keep the cached listing in step with what the negotiation just did to it.
    const item = listingIndex.get(o.listingId);
    if (item) {
      item.myOpenOfferId = null;
      if (action === "accept") item.status = "Sold";
    }
    await loadOffers();
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
    await loadOffers(); // the other side may have answered in the meantime
  }
}

// A negotiation moved on the other side (counter, accept, ...): the notification arrives live, so refresh quietly.
document.addEventListener("realtime:notification", e => {
  if (e.detail && e.detail.type && e.detail.type.startsWith("MarketplaceOffer")) loadOffers();
});

// Same city/country autocomplete as profile edit and settings, for the sell form and the location filter.
enableLocationAutocomplete(document.getElementById("sellLocation"));
enableLocationAutocomplete(document.getElementById("filterLocation"));

renderSellPhotos();
loadCategories().then(() => {
  const params = new URLSearchParams(window.location.search);
  const sharedListingId = params.get("listing");

  if (params.get("view") === "offers" && !sharedListingId) {
    setView("offers");
  } else {
    renderListings();
    loadOffers(); // fills the "Tekliflerim" badge
  }

  if (sharedListingId) {
    openListingById(sharedListingId);
    window.history.replaceState(null, "", window.location.pathname); // a refresh shouldn't pop the modal again
  }
});
