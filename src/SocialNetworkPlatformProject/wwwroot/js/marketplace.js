requireAuth();

const PAGE_SIZE = 12;
const MAX_PHOTOS = 5;
const ALL_CATEGORIES = "Tümü";

let categories = [];
let activeCategory = ALL_CATEGORIES;
let currentView = "all";
let currentListings = [];
let currentPage = 1;
let totalCount = 0;
let selectedListing = null;
let editingListingId = null;
let requestSeq = 0;
let filterDebounce = null;

const listingIndex = new Map();

let offersSide = "received";
const offerLists = { received: [], sent: [] };
let offerModalState = null;

let existingPhotos = [];
let removedPhotoIds = [];
let pendingPhotos = [];

function formatPrice(n) {
  return n.toLocaleString(localeTag(), { maximumFractionDigits: 2 });
}

function priceLabel(n) {
  return formatPrice(n) + " ₺";
}

function isSold(item) {
  return item.status === "Sold";
}

function isReserved(item) {
  return item.status === "Reserved";
}

const CONDITION_LABELS = { New: "Sıfır", LikeNew: "Sıfır gibi", Good: "İyi", Fair: "Orta" };

function listingBadgesHtml(item) {
  const badges = [];
  if (item.isFeatured) badges.push(`<span class="badge text-bg-primary"><i class="bi bi-stars me-1"></i>Öne Çıkan</span>`);
  if (isSold(item)) badges.push(`<span class="badge text-bg-secondary">Satıldı</span>`);
  else if (isReserved(item)) badges.push(`<span class="badge text-bg-warning">Rezerve</span>`);
  if (item.isExpired) badges.push(`<span class="badge text-bg-danger">Süresi doldu</span>`);
  if (CONDITION_LABELS[item.condition]) badges.push(`<span class="badge text-bg-light border">${CONDITION_LABELS[item.condition]}</span>`);
  return badges.join("");
}

function limitDate(isoDate) {
  return parseServerDate(isoDate).toLocaleString(localeTag(), { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" });
}

function limitDay(isoDate) {
  return parseServerDate(isoDate).toLocaleDateString(localeTag(), { day: "numeric", month: "long", year: "numeric" });
}

function isOwnListing(item) {
  const session = getSession();
  return !!session && item.sellerId === session.userId;
}

function coverOf(item) {
  return item.imageUrl || NO_PHOTO;
}

function remember(items) {
  items.forEach(item => listingIndex.set(item.id, item));
}


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


function onFilterInput() {
  clearTimeout(filterDebounce);
  filterDebounce = setTimeout(renderListings, 350);
}

function resetFilterInputs() {
  document.getElementById("marketSearch").value = "";
  document.getElementById("filterMinPrice").value = "";
  document.getElementById("filterMaxPrice").value = "";
  document.getElementById("filterLocation").value = "";
  document.getElementById("filterCondition").value = "";
  document.getElementById("filterIncludeSold").checked = false;
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
  const condition = document.getElementById("filterCondition").value;
  if (condition) params.set("condition", condition);
  if (document.getElementById("filterIncludeSold").checked) params.set("includeSold", "true");
  params.set("sort", document.getElementById("filterSort").value);
  return params;
}


function heartButtonHtml(item) {
  if (isOwnListing(item)) return "";
  return `<button type="button" class="btn btn-sm btn-light rounded-circle position-absolute top-0 end-0 m-2 shadow-sm" data-heart-id="${item.id}" onclick="toggleSave('${item.id}', event)" title="Kaydet">
            <i class="bi ${item.isSavedByCurrentUser ? "bi-heart-fill text-danger" : "bi-heart"}"></i>
          </button>`;
}

function cardRenewHtml(item) {
  if (!isOwnListing(item)) return "";
  if (item.canRenew) {
    return `<div class="listing-owner-line px-2 pb-2"><button type="button" class="btn btn-warning btn-sm rounded-pill w-100" onclick="renewListing('${item.id}', event)"><i class="bi bi-arrow-repeat me-1"></i>Yenile</button></div>`;
  }
  if (item.nextRenewAt) {
    return `<div class="listing-owner-line px-2 pb-2 small text-muted">Tekrar yenileyebileceğin zaman: ${limitDate(item.nextRenewAt)}</div>`;
  }
  return "";
}

function listingCardHtml(item) {
  const sold = isSold(item);
  return `
    <div class="col-6 col-md-4">
      <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100 position-relative ${sold || item.isExpired ? "opacity-75" : ""} ${item.isFeatured ? "listing-featured" : ""}" role="button" onclick="openListing('${item.id}')">
        <div class="listing-badges">${listingBadgesHtml(item)}</div>
        ${heartButtonHtml(item)}
        <img src="${coverOf(item)}" class="w-100" style="height:140px;object-fit:cover;" alt="">
        <div class="p-2">
          <div class="fw-bold text-primary">${priceLabel(item.price)}${item.openOfferCount > 0 ? ` <span class="badge text-bg-warning fw-normal">${item.openOfferCount} teklif</span>` : ""}</div>
          <div class="small text-truncate">${escapeHtml(item.title)}</div>
          <div class="text-muted small text-truncate"><i class="bi bi-geo-alt"></i> ${escapeHtml(item.location || "Belirtilmedi")}</div>
          ${item.sellerRatingCount > 0 ? `<div class="small"><i class="bi bi-star-fill rating-star"></i> <b>${item.sellerRatingAverage.toFixed(1)}</b> <span class="text-muted">(${item.sellerRatingCount})</span></div>` : ""}
        </div>
        ${cardRenewHtml(item)}
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
    if (seq !== requestSeq) return;
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

function renderModalDynamic(item) {
  const mine = isOwnListing(item);

  document.getElementById("listingModalBadges").innerHTML = listingBadgesHtml(item);
  renderModalHeart(item);

  const messageLink = document.getElementById("listingModalMessageLink");
  messageLink.classList.toggle("d-none", mine || isSold(item) || item.isExpired || !item.canMessageSeller);
  messageLink.href = `messages.html?userId=${item.sellerId}`;

  document.getElementById("listingModalReportBtn").classList.toggle("d-none", mine);

  const ownerActions = document.getElementById("listingOwnerActions");
  ownerActions.classList.toggle("d-none", !mine);
  ownerActions.classList.toggle("d-flex", mine);
  if (mine) renderOwnerActions(item);

  renderModalOfferButtons(item, mine);
}

async function refreshOpenListing(id) {
  try {
    const fresh = await apiFetch(`/api/marketplace/${id}`);
    const item = listingIndex.get(id);
    if (!item) return;
    Object.assign(item, fresh);
    if (selectedListing && selectedListing.id === id) renderModalDynamic(item);
  } catch (err) {
  }
}

function openListing(id, { fresh = false } = {}) {
  const item = listingIndex.get(id);
  if (!item) return;
  selectedListing = item;

  document.getElementById("listingModalTitle").textContent = item.title;
  renderCarousel(item);
  document.getElementById("listingModalPrice").textContent = priceLabel(item.price);
  document.getElementById("listingModalMeta").innerHTML = `<i class="bi bi-geo-alt"></i> ${escapeHtml(item.location || "Belirtilmedi")} · ${escapeHtml(item.category)}`;
  document.getElementById("listingModalDescription").textContent = item.description || "";
  document.getElementById("listingModalSellerAvatar").src = item.sellerAvatarUrl || DEFAULT_AVATAR;
  document.getElementById("listingModalSellerName").textContent = item.sellerName;
  document.getElementById("listingModalSellerLink").href = `profile.html?id=${item.sellerId}`;
  document.getElementById("listingModalSellerRating").innerHTML = item.sellerRatingCount > 0
    ? `${starsHtml(item.sellerRatingAverage)} <b>${item.sellerRatingAverage.toFixed(1)}</b> <span class="text-muted">(${item.sellerRatingCount} değerlendirme)</span>`
    : `<span class="text-muted">Henüz değerlendirilmedi</span>`;

  renderModalDynamic(item);
  loadSellerOtherListings(item);
  loadSimilarListings(item);
  bootstrap.Modal.getOrCreateInstance(document.getElementById("listingModal")).show();

  if (!fresh) refreshOpenListing(id);
}

function renderOwnerActions(item) {
  for (const [status, id, active] of [["Active", "statusBtnActive", "btn-success"], ["Reserved", "statusBtnReserved", "btn-warning"], ["Sold", "statusBtnSold", "btn-secondary"]]) {
    const btn = document.getElementById(id);
    const current = item.status === status;
    btn.classList.toggle(active, current);
    btn.classList.toggle(active.replace("btn-", "btn-outline-"), !current);
    btn.setAttribute("aria-pressed", String(current));
  }

  document.getElementById("listingOwnerExpiry").textContent = isSold(item)
    ? ""
    : item.isExpired
      ? `Süresi doldu: ${limitDay(item.expiresAt)}`
      : `Yayında kalma süresi: ${limitDay(item.expiresAt)}`;

  const renewBtn = document.getElementById("listingModalRenewBtn");
  const renewNote = document.getElementById("listingModalRenewNote");
  renewBtn.classList.toggle("d-none", !item.canRenew);
  renewNote.classList.toggle("d-none", !item.nextRenewAt);
  renewNote.textContent = item.nextRenewAt ? `Tekrar yenileyebileceğin zaman: ${limitDate(item.nextRenewAt)}` : "";

  const featureBtn = document.getElementById("listingModalFeatureBtn");
  const featureNote = document.getElementById("listingModalFeatureNote");
  const featureShown = item.canFeature || !!item.nextFeatureAt;
  featureBtn.classList.toggle("d-none", !featureShown);
  featureBtn.disabled = !item.canFeature;
  featureNote.classList.toggle("d-none", !item.nextFeatureAt && !item.isFeatured);
  featureNote.textContent = item.isFeatured
    ? `Öne çıkarıldı: ${limitDate(item.featuredUntil)} tarihine kadar listenin başında.`
    : item.nextFeatureAt ? `Bir sonraki öne çıkarma hakkın: ${limitDate(item.nextFeatureAt)}` : "";
}

function reportListing() {
  if (selectedListing) reportDialog("Listing", selectedListing.id);
}

function renderModalOfferButtons(item, mine) {
  const offerBtn = document.getElementById("listingModalOfferBtn");
  const ownerBtn = document.getElementById("listingModalOwnerOffersBtn");

  const canOffer = !mine && item.status === "Active" && !item.isExpired;
  const canViewOffer = !mine && !isSold(item) && !!item.myOpenOfferId;
  offerBtn.classList.toggle("d-none", !(canOffer || canViewOffer));
  offerBtn.innerHTML = item.myOpenOfferId
    ? `<i class="bi bi-tag me-1"></i>Teklifini Gör`
    : `<i class="bi bi-tag me-1"></i>Teklif Ver`;

  const notice = document.getElementById("listingModalNotice");
  const noticeText = mine ? ""
    : isSold(item) ? "Bu ilan satıldı."
    : item.isExpired ? "Bu ilanın süresi doldu."
    : isReserved(item) && !item.myOpenOfferId ? "Bu ilan rezerve edildi, şu an yeni teklif alınmıyor."
    : "";
  notice.textContent = noticeText;
  notice.classList.toggle("d-none", !noticeText);

  const pending = mine && item.openOfferCount > 0;
  ownerBtn.classList.toggle("d-none", !pending);
  if (pending) ownerBtn.innerHTML = `<i class="bi bi-inbox me-1"></i>Gelen Teklifler (${item.openOfferCount})`;
}

function onOfferButton() {
  if (!selectedListing) return;
  if (selectedListing.myOpenOfferId) goToOffers("sent");
  else openOfferModal({ kind: "new", id: selectedListing.id, title: selectedListing.title, asking: selectedListing.price });
}

function miniCardHtml(o) {
  return `
      <div class="col-4 col-md-2">
        <div class="card border-0 shadow-sm rounded-3 overflow-hidden h-100" role="button" onclick="openListing('${o.id}')">
          <img src="${coverOf(o)}" class="w-100" style="height:70px;object-fit:cover;" alt="">
          <div class="p-1">
            <div class="fw-bold text-primary small">${priceLabel(o.price)}</div>
            <div class="text-truncate" style="font-size:.75rem;">${escapeHtml(o.title)}</div>
          </div>
        </div>
      </div>`;
}

async function loadExtraSection(item, sectionId, listId, fetchItems) {
  const section = document.getElementById(sectionId);
  const list = document.getElementById(listId);
  section.classList.add("d-none");
  list.innerHTML = "";

  try {
    const items = await fetchItems();
    if (!selectedListing || selectedListing.id !== item.id) return;
    if (!items.length) return;

    remember(items);
    list.innerHTML = items.map(miniCardHtml).join("");
    section.classList.remove("d-none");
  } catch (err) {
  }
}

function loadSellerOtherListings(item) {
  return loadExtraSection(item, "sellerOtherSection", "sellerOtherList", async () => {
    const result = await apiFetch(`/api/marketplace?sellerId=${item.sellerId}&pageSize=7`);
    return result.items.filter(l => l.id !== item.id).slice(0, 6);
  });
}

function loadSimilarListings(item) {
  return loadExtraSection(item, "similarSection", "similarList", () => apiFetch(`/api/marketplace/${item.id}/similar`));
}

function closeListingModal() {
  bootstrap.Modal.getInstance(document.getElementById("listingModal"))?.hide();
}

async function deleteListing() {
  if (!selectedListing || !(await confirmDialog("Bu ilanı kaldırmak istediğine emin misin?", { confirmText: "İlanı Kaldır" }))) return;
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

const STATUS_TOAST = {
  Active: "İlan tekrar satışa çıktı.",
  Reserved: "İlan rezerve edildi.",
  Sold: "İlan satıldı olarak işaretlendi."
};

async function setListingStatus(status) {
  if (!selectedListing || selectedListing.status === status) return;
  try {
    await apiFetch(`/api/marketplace/${selectedListing.id}/status`, { method: "PUT", body: { status } });
    closeListingModal();
    toast(STATUS_TOAST[status]);
    await renderListings();
  } catch (err) {
    toast(err.message || "Durum değiştirilemedi.");
  }
}

async function renewListing(id, event) {
  if (event) event.stopPropagation();
  const listingId = id || (selectedListing && selectedListing.id);
  if (!listingId) return;

  try {
    const renewed = await apiFetch(`/api/marketplace/${listingId}/renew`, { method: "POST" });
    toast("İlanın yenilendi, 30 gün daha yayında.");
    await afterOwnerAction(renewed);
  } catch (err) {
    toast(err.message || "İlan yenilenemedi.");
  }
}

async function featureListing() {
  if (!selectedListing) return;
  if (!(await confirmDialog("Bu ilan 24 saat boyunca listenin en başında görünecek. Bu hakkı haftada bir kez kullanabilirsin. Devam edilsin mi?", { confirmText: "Öne Çıkar", danger: false }))) return;

  try {
    const featured = await apiFetch(`/api/marketplace/${selectedListing.id}/feature`, { method: "POST" });
    toast("İlanın 24 saat boyunca öne çıkarıldı.");
    await afterOwnerAction(featured);
  } catch (err) {
    toast(err.message || "İlan öne çıkarılamadı.");
  }
}

async function afterOwnerAction(updated) {
  const cached = listingIndex.get(updated.id);
  if (cached) Object.assign(cached, updated);
  if (selectedListing && selectedListing.id === updated.id) renderModalDynamic(selectedListing);
  await renderListings();
}


function totalPhotoCount() {
  return existingPhotos.length + pendingPhotos.length;
}

function renderSellPhotos() {
  const thumb = (url, onRemove) => `
    <div class="position-relative" style="width:76px;height:76px;">
      <img src="${url}" class="rounded-3 w-100 h-100" style="object-fit:cover;" alt="">
      <button type="button" class="btn btn-dark btn-sm rounded-circle position-absolute top-0 end-0 p-0 d-flex align-items-center justify-content-center"
              style="width:22px;height:22px;transform:translate(35%,-35%);" onclick="${onRemove}" aria-label="Fotoğrafı Kaldır"><i class="bi bi-x"></i></button>
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
  document.getElementById("sellCondition").value = "";
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
  document.getElementById("sellCondition").value = item.condition;
  document.getElementById("sellLocation").value = item.location || "";
  document.getElementById("sellDescription").value = item.description || "";
  renderSellPhotos();

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

  const conditionSelect = document.getElementById("sellCondition");
  if (!conditionSelect.value) {
    errorBox.textContent = "Ürünün durumunu seç: Sıfır, Sıfır gibi, İyi ya da Orta.";
    errorBox.classList.remove("d-none");
    return;
  }

  const submitBtn = document.getElementById("sellSubmitBtn");
  submitBtn.disabled = true;

  const isEdit = editingListingId !== null;

  try {
    const formData = new FormData();
    formData.append("title", title);
    formData.append("price", document.getElementById("sellPrice").value || "0");
    formData.append("category", document.getElementById("sellCategory").value);
    formData.append("condition", conditionSelect.value);
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

    if (!isEdit && currentView === "all") resetFilterInputs();
    await renderListings();
  } catch (err) {
    errorBox.textContent = err.message || "İlan kaydedilemedi.";
    errorBox.classList.remove("d-none");
  } finally {
    submitBtn.disabled = false;
  }
}

document.getElementById("sellModal").addEventListener("hidden.bs.modal", resetSellForm);


function myId() {
  return getSession().userId;
}

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
        <img src="${o.listingImageUrl || NO_PHOTO}" class="rounded-3 flex-shrink-0" style="width:84px;height:84px;object-fit:cover;" alt="" role="button" onclick="openListingById('${o.listingId}')">
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
          ${offerRatingHtml(o)}
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
    remember([await apiFetch(`/api/marketplace/${id}`)]);
    openListing(id, { fresh: true });
  } catch (err) {
    toast("İlan bulunamadı ya da kaldırılmış olabilir.");
  }
}


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
    linkDialog(link);
  }
}

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
  const asked = {
    accept: { confirmText: "Kabul Et", danger: false },
    reject: { confirmText: "Reddet" },
    withdraw: { confirmText: "Geri Çek" }
  }[action];
  if (!(await confirmDialog(confirmText, asked))) return;

  try {
    await apiFetch(`/api/marketplace/offers/${offerId}/${action}`, { method: "POST" });
    toast({ accept: "Teklif kabul edildi. İlan satıldı.", reject: "Teklif reddedildi.", withdraw: "Teklifin geri çekildi." }[action]);

    const item = listingIndex.get(o.listingId);
    if (item) {
      item.myOpenOfferId = null;
      if (action === "accept") item.status = "Sold";
    }
    await loadOffers();
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
    await loadOffers();
  }
}


const STAR_LABELS = ["", "Çok kötü", "Kötü", "Fena değil", "İyi", "Mükemmel"];
let ratingState = null;
let reviewsState = null;

function ratingQuote(rating) {
  return rating.comment ? ` <span class="text-muted">“${escapeHtml(rating.comment)}”</span>` : "";
}

function offerRatingHtml(o) {
  if (o.status !== "Accepted") return "";

  if (o.isCurrentUserBuyer) {
    return o.rating
      ? `<div class="small mt-2">Puanın: ${starsHtml(o.rating.stars)}${ratingQuote(o.rating)}
           <button class="btn btn-link btn-sm p-0 ms-1" onclick="openRateModal('${o.id}')">Düzenle</button></div>`
      : `<button class="btn btn-warning btn-sm rounded-pill mt-2" onclick="openRateModal('${o.id}')"><i class="bi bi-star-fill me-1"></i>Satıcıyı Puanla</button>`;
  }
  return o.rating
    ? `<div class="small mt-2">Alıcının puanı: ${starsHtml(o.rating.stars)}${ratingQuote(o.rating)}</div>`
    : `<div class="small mt-2 text-muted">Alıcı henüz puan vermedi.</div>`;
}

function renderStarInput(hover = 0) {
  const box = document.getElementById("rateStarInput");
  if (!box.children.length) {
    box.innerHTML = [1, 2, 3, 4, 5]
      .map(i => `<i class="bi bi-star rating-star" role="radio" data-star="${i}" style="cursor:pointer;"></i>`)
      .join(" ");
  }

  const value = ratingState ? ratingState.stars : 0;
  const shown = hover || value;
  box.querySelectorAll("[data-star]").forEach(star => {
    const n = Number(star.dataset.star);
    star.className = `bi ${n <= shown ? "bi-star-fill" : "bi-star"} rating-star`;
    star.setAttribute("aria-checked", String(n === value));
  });
  document.getElementById("rateStarLabel").textContent = shown ? `${shown} / 5 · ${STAR_LABELS[shown]}` : "Bir puan seç";
}

(() => {
  const input = document.getElementById("rateStarInput");
  input.addEventListener("mouseover", e => { if (e.target.dataset.star) renderStarInput(Number(e.target.dataset.star)); });
  input.addEventListener("mouseleave", () => renderStarInput());
  input.addEventListener("click", e => {
    if (!e.target.dataset.star) return;
    ratingState.stars = Number(e.target.dataset.star);
    renderStarInput();
  });
})();

function openRateModal(offerId) {
  const o = offerLists.sent.find(x => x.id === offerId);
  if (!o) return;

  ratingState = { offerId, stars: o.rating ? o.rating.stars : 0, hadRating: !!o.rating };
  document.getElementById("rateModalSeller").textContent = `${o.sellerName} · ${o.listingTitle}`;
  document.getElementById("rateComment").value = o.rating && o.rating.comment ? o.rating.comment : "";
  document.getElementById("rateError").classList.add("d-none");
  document.getElementById("rateDeleteBtn").classList.toggle("d-none", !ratingState.hadRating);
  renderStarInput();
  bootstrap.Modal.getOrCreateInstance(document.getElementById("rateModal")).show();
}

async function submitRating() {
  if (!ratingState) return;
  const errorBox = document.getElementById("rateError");
  if (!ratingState.stars) {
    errorBox.textContent = "Lütfen 1 ile 5 arasında bir puan seç.";
    errorBox.classList.remove("d-none");
    return;
  }

  const btn = document.getElementById("rateSubmitBtn");
  btn.disabled = true;
  try {
    await apiFetch(`/api/marketplace/offers/${ratingState.offerId}/rating`, {
      method: "PUT",
      body: { stars: ratingState.stars, comment: document.getElementById("rateComment").value.trim() }
    });
    bootstrap.Modal.getInstance(document.getElementById("rateModal"))?.hide();
    toast(ratingState.hadRating ? "Puanın güncellendi." : "Puanın kaydedildi, teşekkürler!");
    await loadOffers();
  } catch (err) {
    errorBox.textContent = err.message || "Puan kaydedilemedi.";
    errorBox.classList.remove("d-none");
  } finally {
    btn.disabled = false;
  }
}

async function deleteRating() {
  if (!ratingState || !(await confirmDialog("Verdiğin puanı silmek istediğine emin misin?", { confirmText: "Puanı Sil" }))) return;
  try {
    await apiFetch(`/api/marketplace/offers/${ratingState.offerId}/rating`, { method: "DELETE" });
    bootstrap.Modal.getInstance(document.getElementById("rateModal"))?.hide();
    toast("Puanın silindi.");
    await loadOffers();
  } catch (err) {
    toast(err.message || "Puan silinemedi.");
  }
}

function reviewHtml(r) {
  return `
    <div class="d-flex gap-2 py-3 border-top">
      <a href="profile.html?id=${r.reviewerId}"><img src="${r.reviewerAvatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt=""></a>
      <div class="flex-grow-1">
        <div class="d-flex justify-content-between align-items-start gap-2">
          <a href="profile.html?id=${r.reviewerId}" class="fw-semibold text-body text-decoration-none">${escapeHtml(r.reviewerName)}</a>
          <span class="text-muted small flex-shrink-0">${timeAgo(r.createdAt)}</span>
        </div>
        <div>${starsHtml(r.stars)}</div>
        ${r.comment ? `<div class="small mt-1">${escapeHtml(r.comment)}</div>` : ""}
        <div class="text-muted small mt-1"><i class="bi bi-tag me-1"></i>${escapeHtml(r.listingTitle)}</div>
      </div>
    </div>`;
}

function reviewsHeaderHtml(summary) {
  if (!summary.count) {
    return `<div class="text-center py-3"><div class="fw-bold"><a href="profile.html?id=${summary.sellerId}" class="text-body text-decoration-none">${escapeHtml(summary.sellerName)}</a></div><div class="text-muted small">Henüz değerlendirilmedi.</div></div>`;
  }

  const bars = [5, 4, 3, 2, 1].map(star => {
    const count = summary.distribution[star - 1];
    const percent = Math.round(count / summary.count * 100);
    return `<div class="d-flex align-items-center gap-2 small">
        <span style="width:14px;">${star}</span><i class="bi bi-star-fill rating-star"></i>
        <div class="progress flex-grow-1" style="height:8px;"><div class="progress-bar bg-warning" style="width:${percent}%"></div></div>
        <span class="text-muted text-end" style="width:28px;">${count}</span>
      </div>`;
  }).join("");

  return `
    <div class="d-flex align-items-center gap-3 mb-3">
      <img src="${summary.sellerAvatarUrl || DEFAULT_AVATAR}" class="avatar-md rounded-circle" style="width:56px;height:56px;object-fit:cover;" alt="">
      <div class="flex-grow-1">
        <a href="profile.html?id=${summary.sellerId}" class="fw-bold text-body text-decoration-none">${escapeHtml(summary.sellerName)}</a>
        <div class="d-flex align-items-center gap-2">
          <span class="fs-3 fw-bold">${summary.average.toFixed(1)}</span>
          <div><div class="fs-5 lh-1">${starsHtml(summary.average)}</div><div class="text-muted small">${summary.count} değerlendirme</div></div>
        </div>
      </div>
    </div>
    <div class="d-flex flex-column gap-1 mb-2">${bars}</div>`;
}

async function openSellerReviews(sellerId) {
  reviewsState = { sellerId, page: 1, total: 0 };
  const body = document.getElementById("sellerReviewsBody");
  body.innerHTML = `<div class="text-center text-muted small py-4">Yükleniyor...</div>`;
  openModalAfterListingModal("sellerReviewsModal");

  try {
    const [summary, reviews] = await Promise.all([
      apiFetch(`/api/marketplace/sellers/${sellerId}/rating-summary`),
      apiFetch(`/api/marketplace/sellers/${sellerId}/ratings?page=1&pageSize=10`)
    ]);
    reviewsState.total = reviews.totalCount;
    body.innerHTML = `${reviewsHeaderHtml(summary)}
      <div id="reviewsList">${reviews.items.map(reviewHtml).join("")}</div>
      <div class="text-center mt-2 ${reviews.items.length >= reviews.totalCount ? "d-none" : ""}" id="reviewsMoreWrap">
        <button class="btn btn-light border btn-sm rounded-pill" onclick="loadMoreReviews()">Daha fazla göster</button>
      </div>`;
  } catch (err) {
    body.innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
  }
}

async function loadMoreReviews() {
  if (!reviewsState) return;
  try {
    const next = await apiFetch(`/api/marketplace/sellers/${reviewsState.sellerId}/ratings?page=${reviewsState.page + 1}&pageSize=10`);
    reviewsState.page += 1;
    document.getElementById("reviewsList").insertAdjacentHTML("beforeend", next.items.map(reviewHtml).join(""));
    const shown = document.querySelectorAll("#reviewsList > div").length;
    document.getElementById("reviewsMoreWrap").classList.toggle("d-none", shown >= next.totalCount);
  } catch (err) {
    toast(err.message || "Değerlendirmeler yüklenemedi.");
  }
}

document.addEventListener("realtime:notification", e => {
  if (e.detail && e.detail.type && e.detail.type.startsWith("MarketplaceOffer")) loadOffers();
});

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
    loadOffers();
  }

  const reviewsOfSeller = params.get("sellerReviews");

  if (sharedListingId) openListingById(sharedListingId);
  else if (reviewsOfSeller) openSellerReviews(reviewsOfSeller);

  if (sharedListingId || reviewsOfSeller) {
    window.history.replaceState(null, "", window.location.pathname);
  }
});
