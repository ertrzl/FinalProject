// profile.html — F6: own profile + someone else's, wired to the backend.

const session = requireAuth();
const params = new URLSearchParams(window.location.search);
const profileUserId = params.get("id") || session.userId;

let currentProfile = null;
let loadedPosts = [];

enableLocationAutocomplete(document.getElementById("editLocation"));

function monthYear(isoDate) {
  return new Date(isoDate).toLocaleDateString("tr-TR", { month: "long", year: "numeric" });
}

// Marketplace seller rating next to the handle (hidden until the user has been rated); links to the reviews.
async function loadSellerRatingBadge(userId) {
  const slot = document.getElementById("profileSellerRating");
  try {
    const summary = await apiFetch(`/api/marketplace/sellers/${userId}/rating-summary`);
    if (!summary.count) {
      slot.classList.add("d-none");
      return;
    }
    slot.innerHTML = `<a href="marketplace.html?sellerReviews=${userId}" class="text-decoration-none text-body">
        ${starsHtml(summary.average)} <b>${summary.average.toFixed(1)}</b>
        <span class="text-muted">(${summary.count} satıcı değerlendirmesi)</span></a>`;
    slot.classList.remove("d-none");
  } catch (err) {
    slot.classList.add("d-none"); // a missing badge is not worth an error on the profile page
  }
}

async function loadProfile() {
  try {
    currentProfile = await apiFetch(`/api/users/${profileUserId}`);
  } catch (err) {
    document.querySelector(".container").innerHTML = `<div class="alert alert-danger mt-4">${escapeHtml(err.message)}</div>`;
    return;
  }

  const p = currentProfile;
  document.title = (p.isOwnProfile ? "Profilim" : p.fullName) + " | SocialNet";
  document.getElementById("profileName").textContent = p.fullName;
  document.getElementById("profileHandle").textContent = "@" + p.userName;
  document.getElementById("profileFriendCount").textContent = p.friendCount;
  document.getElementById("profileAvatar").src = p.avatarUrl || DEFAULT_AVATAR;
  const onlineDot = document.getElementById("profileOnlineDot");
  onlineDot.dataset.presenceUser = p.id;
  onlineDot.classList.toggle("d-none", !p.isOnline);

  loadSellerRatingBadge(p.id);

  const cover = document.getElementById("profileCover");
  if (p.coverPhotoUrl) {
    cover.style.backgroundImage = `url(${p.coverPhotoUrl})`;
    cover.style.backgroundSize = "cover";
    cover.style.backgroundPosition = "center";
  } else {
    cover.style.backgroundImage = "";
  }
  document.getElementById("removeCoverBtn")?.classList.toggle("d-none", !p.coverPhotoUrl);

  const aboutHtml = `
    ${p.bio ? `<p class="small mb-3"><i class="bi bi-chat-quote me-1 text-primary"></i> ${escapeHtml(p.bio)}</p>` : ""}
    ${p.location ? `<div class="small border-top pt-2 mb-2 d-flex"><span class="text-muted" style="width:110px;"><i class="bi bi-geo-alt me-1"></i>Konum</span><span>${escapeHtml(p.location)}</span></div>` : ""}
    ${p.occupation ? `<div class="small border-top pt-2 mb-2 d-flex"><span class="text-muted" style="width:110px;"><i class="bi bi-briefcase me-1"></i>Meslek</span><span>${escapeHtml(p.occupation)}</span></div>` : ""}
    ${p.education ? `<div class="small border-top pt-2 mb-2 d-flex"><span class="text-muted" style="width:110px;"><i class="bi bi-mortarboard me-1"></i>Eğitim</span><span>${escapeHtml(p.education)}</span></div>` : ""}
    <div class="small border-top pt-2 d-flex"><span class="text-muted" style="width:110px;"><i class="bi bi-calendar3 me-1"></i>Katılım</span><span>${monthYear(p.joinedAt)}</span></div>`;
  document.getElementById("aboutSummary").innerHTML = aboutHtml;
  document.getElementById("aboutFull").innerHTML = aboutHtml;

  if (p.isOwnProfile) {
    document.getElementById("composerAvatar").src = p.avatarUrl || DEFAULT_AVATAR;
    document.getElementById("composerText").placeholder = `Aklında ne var, ${p.fullName.split(" ")[0]}?`;
    updateSessionAvatar(p.avatarUrl);
    document.querySelectorAll(".nav-profile-avatar").forEach(el => { el.src = p.avatarUrl || DEFAULT_AVATAR; });
    const avatarImg = document.getElementById("profileAvatar");
    avatarImg.setAttribute("data-bs-toggle", "dropdown");
    avatarImg.style.cursor = "pointer";
  } else {
    document.getElementById("ownComposerWrap")?.remove();
    document.getElementById("profileCoverEditBtn")?.remove();
    await renderOtherProfileActions(p);
  }

  loadFriendsSummary();
  loadPosts();
  loadPhotosFromPosts();
}

async function renderOtherProfileActions(p) {
  const area = document.getElementById("profileActionArea");

  if (p.friendshipStatus === "Friends") {
    area.innerHTML = `
      <a href="messages.html?userId=${p.id}" class="btn btn-primary btn-sm rounded-pill mb-2 me-2"><i class="bi bi-chat-dots me-1"></i>Mesaj Gönder</a>
      <button class="btn btn-light btn-sm rounded-pill shadow-sm border mb-2" onclick="removeFriend('${p.id}')"><i class="bi bi-person-dash me-1"></i>Arkadaşlıktan Çıkar</button>`;
  } else if (p.friendshipStatus === "RequestSent") {
    area.innerHTML = `<button class="btn btn-secondary btn-sm rounded-pill mb-2" disabled>İstek Gönderildi</button>`;
  } else if (p.friendshipStatus === "RequestReceived") {
    area.innerHTML = `<button class="btn btn-primary btn-sm rounded-pill mb-2" disabled>İstek Bekliyor...</button>`;
    try {
      const incoming = await apiFetch("/api/friends/requests/incoming");
      const match = incoming.find(r => r.senderId === p.id);
      if (match) {
        area.innerHTML = `
          <button class="btn btn-primary btn-sm rounded-pill mb-2 me-2" onclick="respondRequest('${match.id}', true)">Kabul Et</button>
          <button class="btn btn-outline-danger btn-sm rounded-pill mb-2" onclick="respondRequest('${match.id}', false)">Reddet</button>`;
      }
    } catch (err) { /* leave the disabled hint */ }
  } else {
    area.innerHTML = `
      <button class="btn btn-primary btn-sm rounded-pill mb-2 me-2" onclick="sendRequest('${p.id}', this)"><i class="bi bi-person-plus me-1"></i>Arkadaş Ekle</button>
      ${p.canMessage ? `<a href="messages.html?userId=${p.id}" class="btn btn-light btn-sm rounded-pill shadow-sm border mb-2"><i class="bi bi-chat-dots me-1"></i>Mesaj Gönder</a>` : ""}`;
  }
}

async function sendRequest(userId, btn) {
  btn.disabled = true;
  try {
    await apiFetch("/api/friends/requests", { method: "POST", body: { receiverId: userId } });
    toast("Arkadaşlık isteği gönderildi.");
    await loadProfile();
  } catch (err) {
    btn.disabled = false;
    toast(err.message || "İstek gönderilemedi.");
  }
}

async function respondRequest(requestId, accepted) {
  try {
    await apiFetch(`/api/friends/requests/${requestId}/${accepted ? "accept" : "decline"}`, { method: "POST" });
    toast(accepted ? "Arkadaşlık isteği kabul edildi." : "İstek reddedildi.");
    await loadProfile();
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

async function removeFriend(userId) {
  try {
    await apiFetch(`/api/friends/${userId}`, { method: "DELETE" });
    toast("Arkadaşlıktan çıkarıldı.");
    await loadProfile();
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

async function loadFriendsSummary() {
  const isOwn = currentProfile.isOwnProfile;
  const heading = document.getElementById("friendsTabHeading");
  const tabList = document.getElementById("friendsTabList");
  const summary = document.getElementById("friendsSummary");

  if (!isOwn) {
    heading.textContent = `${currentProfile.friendCount} Arkadaş`;
    tabList.innerHTML = `<div class="text-muted small">Arkadaş listesi sadece profil sahibine görünür.</div>`;
    summary.innerHTML = `<div class="text-muted small">Liste gizli.</div>`;
    return;
  }

  try {
    const friends = await apiFetch("/api/friends");
    heading.textContent = `${friends.length} Arkadaş`;

    const tile = f => `<div class="col friend-tile"><img src="${f.avatarUrl || DEFAULT_AVATAR}" alt=""><div class="small fw-semibold mt-1"><a href="profile.html?id=${f.userId}" class="text-body text-decoration-none">${escapeHtml(f.name)}</a></div></div>`;
    tabList.innerHTML = friends.length ? friends.map(tile).join("") : `<div class="text-muted small">Henüz arkadaşın yok.</div>`;
    summary.innerHTML = friends.slice(0, 6).map(tile).join("") || `<div class="text-muted small">Henüz arkadaşın yok.</div>`;
  } catch (err) {
    tabList.innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
  }
}

// The wall, 20 posts at a time (pager.js); the photos tab is drawn from the posts loaded so far.
const wallPager = createPager({
  list: "profilePostsList",
  url: page => `/api/posts/by-user/${profileUserId}?page=${page}&pageSize=20`,
  render: post => postCardHtml(post),
  shown: post => !!livePostCard(post.id),
  emptyHtml: `<div class="text-center text-muted small py-4">Henüz gönderi yok.</div>`,
  onLoaded: (items, firstPage) => {
    loadedPosts = firstPage ? items : loadedPosts.concat(items);
    loadPhotosFromPosts();
  }
});

function loadPosts() {
  return wallPager.reload();
}

function loadPhotosFromPosts() {
  const grid = document.getElementById("photosGrid");
  // Video posts don't belong in the photo grid — they show up on the wall itself.
  const photos = loadedPosts.filter(p => p.mediaUrl && p.mediaType !== "Video");
  grid.innerHTML = photos.length
    ? photos.map(p => `<div class="col"><img src="${p.mediaUrl}" class="w-100 rounded-3" style="aspect-ratio:1/1;object-fit:cover;" alt=""></div>`).join("")
    : `<div class="text-muted small">Henüz fotoğraf yok.</div>`;
}

async function publishPost(btn) {
  const textarea = document.getElementById("composerText");
  const fileInput = document.getElementById("composerFile");
  const privacy = document.getElementById("composerPrivacy").value;
  const text = textarea.value.trim();
  const file = fileInput.files[0];

  if (!text && !file) {
    textarea.classList.add("is-invalid");
    setTimeout(() => textarea.classList.remove("is-invalid"), 1200);
    return;
  }

  btn.disabled = true;
  try {
    const formData = new FormData();
    if (text) formData.append("text", text);
    if (file) formData.append("media", file);
    formData.append("privacy", privacy);

    await apiFetchForm("/api/posts", { method: "POST", body: formData });

    textarea.value = "";
    clearComposerImage();
    toast("Gönderi paylaşıldı!");
    await loadPosts();
    loadPhotosFromPosts();
  } catch (err) {
    toast(err.message || "Gönderi paylaşılamadı.");
  } finally {
    btn.disabled = false;
  }
}

let avatarMarkedForRemoval = false;

function openEditProfileModal() {
  const p = currentProfile;
  document.getElementById("editFullName").value = p.fullName;
  document.getElementById("editBio").value = p.bio || "";
  document.getElementById("editLocation").value = p.location || "";
  document.getElementById("editOccupation").value = p.occupation || "";
  document.getElementById("editEducation").value = p.education || "";
  document.getElementById("editAvatarPreview").src = p.avatarUrl || DEFAULT_AVATAR;
  document.getElementById("editAvatarInput").value = "";
  avatarMarkedForRemoval = false;
  document.getElementById("removeAvatarBtn").classList.toggle("d-none", !p.avatarUrl);
}
document.getElementById("editProfileModal").addEventListener("show.bs.modal", openEditProfileModal);

function previewEditAvatar(input) {
  const preview = document.getElementById("editAvatarPreview");
  if (input.files && input.files[0] && preview) {
    avatarMarkedForRemoval = false;
    preview.src = URL.createObjectURL(input.files[0]);
  }
}

function markAvatarForRemoval() {
  avatarMarkedForRemoval = true;
  document.getElementById("editAvatarInput").value = "";
  document.getElementById("editAvatarPreview").src = DEFAULT_AVATAR;
}

async function saveProfile() {
  const btn = document.getElementById("saveProfileBtn");
  btn.disabled = true;
  try {
    const formData = new FormData();
    formData.append("fullName", document.getElementById("editFullName").value.trim());
    formData.append("bio", document.getElementById("editBio").value.trim());
    formData.append("location", document.getElementById("editLocation").value.trim());
    formData.append("occupation", document.getElementById("editOccupation").value.trim());
    formData.append("education", document.getElementById("editEducation").value.trim());

    const avatarFile = document.getElementById("editAvatarInput").files[0];
    if (avatarFile) formData.append("avatar", avatarFile);
    else if (avatarMarkedForRemoval) formData.append("removeAvatar", "true");

    await apiFetchForm("/api/users/me/profile", { method: "PUT", body: formData });

    document.getElementById("editAvatarInput").value = "";
    avatarMarkedForRemoval = false;
    bootstrap.Modal.getInstance(document.getElementById("editProfileModal"))?.hide();
    toast("Profil güncellendi.");
    await loadProfile();
  } catch (err) {
    toast(err.message || "Profil güncellenemedi.");
  } finally {
    btn.disabled = false;
  }
}

// Clicking the camera item in the avatar dropdown uploads immediately, no modal needed.
async function uploadAvatarQuick(input) {
  const file = input.files[0];
  if (!file) return;

  const avatarImg = document.getElementById("profileAvatar");
  const previousSrc = avatarImg.src;
  avatarImg.src = URL.createObjectURL(file);

  try {
    const formData = new FormData();
    formData.append("fullName", currentProfile.fullName);
    formData.append("bio", currentProfile.bio || "");
    formData.append("location", currentProfile.location || "");
    formData.append("occupation", currentProfile.occupation || "");
    formData.append("education", currentProfile.education || "");
    formData.append("avatar", file);

    await apiFetchForm("/api/users/me/profile", { method: "PUT", body: formData });
    toast("Profil fotoğrafı güncellendi.");
    await loadProfile();
  } catch (err) {
    avatarImg.src = previousSrc;
    toast(err.message || "Profil fotoğrafı güncellenemedi.");
  } finally {
    input.value = "";
  }
}

async function removeCoverPhoto() {
  const cover = document.getElementById("profileCover");
  const previousBg = cover.style.backgroundImage;
  cover.style.backgroundImage = "";

  try {
    const formData = new FormData();
    formData.append("fullName", currentProfile.fullName);
    formData.append("bio", currentProfile.bio || "");
    formData.append("location", currentProfile.location || "");
    formData.append("occupation", currentProfile.occupation || "");
    formData.append("education", currentProfile.education || "");
    formData.append("removeCoverPhoto", "true");

    await apiFetchForm("/api/users/me/profile", { method: "PUT", body: formData });
    toast("Kapak fotoğrafı kaldırıldı.");
    await loadProfile();
  } catch (err) {
    cover.style.backgroundImage = previousBg;
    toast(err.message || "Kapak fotoğrafı kaldırılamadı.");
  }
}

async function uploadCoverPhoto(input) {
  const file = input.files[0];
  if (!file) return;

  const cover = document.getElementById("profileCover");
  const previousBg = cover.style.backgroundImage;
  cover.style.backgroundImage = `url(${URL.createObjectURL(file)})`;
  cover.style.backgroundSize = "cover";
  cover.style.backgroundPosition = "center";

  try {
    const formData = new FormData();
    formData.append("fullName", currentProfile.fullName);
    formData.append("bio", currentProfile.bio || "");
    formData.append("location", currentProfile.location || "");
    formData.append("occupation", currentProfile.occupation || "");
    formData.append("education", currentProfile.education || "");
    formData.append("coverPhoto", file);

    await apiFetchForm("/api/users/me/profile", { method: "PUT", body: formData });
    toast("Kapak fotoğrafı güncellendi.");
    await loadProfile();
  } catch (err) {
    cover.style.backgroundImage = previousBg;
    toast(err.message || "Kapak fotoğrafı güncellenemedi.");
  } finally {
    input.value = "";
  }
}

loadProfile();
