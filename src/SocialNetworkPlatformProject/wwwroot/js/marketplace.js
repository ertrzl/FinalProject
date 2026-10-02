// Marketplace page — browse/filter/sort/page listings, "İlanlarım", edit, mark as sold; all wired to the backend.

requireAuth();

const PAGE_SIZE = 12;
const ALL_CATEGORIES = "Tümü";

let categories = [];
let activeCategory = ALL_CATEGORIES;
let currentView = "all"; // "all" | "mine"
let currentListings = [];
let currentPage = 1;
let totalCount = 0;
let selectedListing = null;
let editingListingId = null;
let requestSeq = 0; // drops responses of superseded requests (fast typing in the filters)
let filterDebounce = null;

function formatPrice(n) {
  return n.toLocaleString("tr-TR", { maximumFractionDigits: 2 }) + " ₺";
}

function isSold(item) {
  return item.status === "Sold";
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

function clearFilters() {
  document.getElementById("marketSearch").value = "";
  document.getElementById("filterMinPrice").value = "";
  document.getElementById("filterMaxPrice").value = "";
  document.getElementById("filterLocation").value = "";
  document.getElementById("filterSort").value = "newest";
  activeCategory = ALL_CATEGORIES;
  renderCategories();
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

function listingCardHtml(item) {
  const sold = isSold(item);
  return `
    <div class="col-6 col-md-4">
      <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100 position-relative ${sold ? "opacity-75" : ""}" role="button" onclick="openListing('${item.id}')">
        ${sold ? `<span class="badge text-bg-secondary position-absolute top-0 start-0 m-2">Satıldı</span>` : ""}
        <img src="${item.imageUrl || "https://picsum.photos/seed/" + item.id + "/500/400"}" class="w-100" style="height:140px;object-fit:cover;" alt="">
        <div class="p-2">
          <div class="fw-bold text-primary">${formatPrice(item.price)}</div>
          <div class="small text-truncate">${escapeHtml(item.title)}</div>
          <div class="text-muted small text-truncate"><i class="bi bi-geo-alt"></i> ${escapeHtml(item.location || "Belirtilmedi")}</div>
        </div>
      </div>
    </div>`;
}

function renderGrid() {
  const grid = document.getElementById("listingsGrid");
  const emptyText = currentView === "mine" ? "Henüz bir ilan yayınlamadın." : "Sonuç bulunamadı.";
  grid.innerHTML = currentListings.length
    ? currentListings.map(listingCardHtml).join("")
    : `<div class="col-12 text-center text-muted py-5">${emptyText}</div>`;

  document.getElementById("resultCount").textContent = (currentView === "mine" ? currentListings.length : totalCount) + " ilan";
  document.getElementById("loadMoreWrap").classList.toggle("d-none", currentView === "mine" || currentListings.length >= totalCount);
}

async function renderListings() {
  const seq = ++requestSeq;
  const grid = document.getElementById("listingsGrid");

  try {
    if (currentView === "mine") {
      const mine = await apiFetch("/api/marketplace/mine");
      if (seq !== requestSeq) return;
      currentListings = mine;
      totalCount = mine.length;
    } else {
      currentPage = 1;
      const result = await apiFetch(`/api/marketplace?${buildBrowseParams(1).toString()}`);
      if (seq !== requestSeq) return;
      currentListings = result.items;
      totalCount = result.totalCount;
    }
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

  const allTab = document.getElementById("tabAll");
  const mineTab = document.getElementById("tabMine");
  allTab.classList.toggle("active", view === "all");
  allTab.classList.toggle("text-muted", view !== "all");
  mineTab.classList.toggle("active", view === "mine");
  mineTab.classList.toggle("text-muted", view !== "mine");
  document.getElementById("browseFilters").classList.toggle("d-none", view === "mine");

  renderListings();
}

// ---- Detail modal ----

function openListing(id) {
  const item = currentListings.find(l => l.id === id);
  if (!item) return;
  selectedListing = item;

  document.getElementById("listingModalTitle").textContent = item.title;
  document.getElementById("listingModalImage").src = item.imageUrl || "https://picsum.photos/seed/" + item.id + "/500/400";
  document.getElementById("listingModalPrice").textContent = formatPrice(item.price);
  document.getElementById("listingModalSoldBadge").classList.toggle("d-none", !isSold(item));
  document.getElementById("listingModalMeta").innerHTML = `<i class="bi bi-geo-alt"></i> ${escapeHtml(item.location || "Belirtilmedi")} · ${escapeHtml(item.category)}`;
  document.getElementById("listingModalDescription").textContent = item.description || "";
  document.getElementById("listingModalSellerAvatar").src = item.sellerAvatarUrl || DEFAULT_AVATAR;
  document.getElementById("listingModalSellerName").textContent = item.sellerName;

  const session = getSession();
  const isMine = session && item.sellerId === session.userId;
  const messageLink = document.getElementById("listingModalMessageLink");
  messageLink.classList.toggle("d-none", isMine || isSold(item));
  messageLink.href = `messages.html?userId=${item.sellerId}`;

  const ownerActions = document.getElementById("listingOwnerActions");
  ownerActions.classList.toggle("d-none", !isMine);
  ownerActions.classList.toggle("d-flex", !!isMine);
  const statusBtn = document.getElementById("listingModalStatusBtn");
  statusBtn.innerHTML = isSold(item)
    ? `<i class="bi bi-arrow-counterclockwise me-1"></i>Tekrar Satışa Çıkar`
    : `<i class="bi bi-check2-circle me-1"></i>Satıldı Olarak İşaretle`;

  bootstrap.Modal.getOrCreateInstance(document.getElementById("listingModal")).show();
}

function closeListingModal() {
  bootstrap.Modal.getInstance(document.getElementById("listingModal"))?.hide();
}

async function deleteListing() {
  if (!selectedListing || !confirm("Bu ilanı kaldırmak istediğine emin misin?")) return;
  try {
    await apiFetch(`/api/marketplace/${selectedListing.id}`, { method: "DELETE" });
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

function previewSellImage(input) {
  const wrap = document.getElementById("sellPreviewWrap");
  const img = document.getElementById("sellPreviewImg");
  if (input.files && input.files[0]) {
    img.src = URL.createObjectURL(input.files[0]);
    wrap.classList.remove("d-none");
    document.getElementById("sellRemoveImage").checked = false;
  }
}

function resetSellForm() {
  editingListingId = null;
  document.getElementById("sellModalTitle").textContent = "Ürün Sat";
  document.getElementById("sellSubmitBtn").textContent = "Yayınla";
  document.getElementById("sellTitle").value = "";
  document.getElementById("sellPrice").value = "";
  document.getElementById("sellCategory").selectedIndex = 0;
  document.getElementById("sellLocation").value = "";
  document.getElementById("sellDescription").value = "";
  document.getElementById("sellImageInput").value = "";
  document.getElementById("sellPreviewImg").src = "";
  document.getElementById("sellPreviewWrap").classList.add("d-none");
  document.getElementById("sellRemoveImage").checked = false;
  document.getElementById("sellRemoveImageWrap").classList.add("d-none");
  document.getElementById("sellError").classList.add("d-none");
}

function openEditListing() {
  if (!selectedListing) return;
  const item = selectedListing;

  resetSellForm();
  editingListingId = item.id;
  document.getElementById("sellModalTitle").textContent = "İlanı Düzenle";
  document.getElementById("sellSubmitBtn").textContent = "Kaydet";
  document.getElementById("sellTitle").value = item.title;
  document.getElementById("sellPrice").value = item.price;
  document.getElementById("sellCategory").value = item.category;
  document.getElementById("sellLocation").value = item.location || "";
  document.getElementById("sellDescription").value = item.description || "";
  if (item.imageUrl) {
    document.getElementById("sellPreviewImg").src = item.imageUrl;
    document.getElementById("sellPreviewWrap").classList.remove("d-none");
    document.getElementById("sellRemoveImageWrap").classList.remove("d-none");
  }

  // Two Bootstrap modals can't stack: let the detail modal finish closing, then open the form.
  const listingModalEl = document.getElementById("listingModal");
  listingModalEl.addEventListener("hidden.bs.modal", () => {
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

    const image = document.getElementById("sellImageInput").files[0];
    if (image) formData.append("image", image);
    if (isEdit && document.getElementById("sellRemoveImage").checked) formData.append("removeImage", "true");

    await apiFetchForm(isEdit ? `/api/marketplace/${editingListingId}` : "/api/marketplace", {
      method: isEdit ? "PUT" : "POST",
      body: formData
    });

    bootstrap.Modal.getInstance(document.getElementById("sellModal"))?.hide();
    toast(isEdit ? "İlan güncellendi." : "İlanın yayınlandı!");

    if (!isEdit) {
      // A fresh listing should be visible right away, whatever filters were active.
      if (currentView === "all") {
        document.getElementById("marketSearch").value = "";
        document.getElementById("filterMinPrice").value = "";
        document.getElementById("filterMaxPrice").value = "";
        document.getElementById("filterLocation").value = "";
        document.getElementById("filterSort").value = "newest";
        activeCategory = ALL_CATEGORIES;
        renderCategories();
      }
    }
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

loadCategories().then(renderListings);
