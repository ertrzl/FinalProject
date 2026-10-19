// profile.html — extras around the profile: mutual friends and groups, "Profili Paylaş" (link + QR code), the "Etiketlendiklerim" tab.
// What is mutual, who may see tagged posts and which address belongs to whom is all decided by the server; this file only shows it.

// ---- Mutual friends and groups (somebody else's profile) ----
let mutualData = null;

async function loadMutual(profile) {
  const button = document.getElementById("profileMutual");
  mutualData = null;
  button.classList.add("d-none");
  if (profile.isOwnProfile || profile.isBlockedByMe) return;

  try {
    mutualData = await apiFetch(`/api/users/${profile.id}/mutual`);
  } catch (err) {
    return; // a missing line is not worth an error on the profile page
  }

  const m = mutualData;
  if (!m.friendCount && !m.groupCount) return;

  // Up to three small faces of the people you both know, then the counts.
  const faces = m.friends.slice(0, 3).map(f => `<img src="${escapeHtml(f.avatarUrl || DEFAULT_AVATAR)}" alt="">`).join("");
  const parts = [];
  if (m.friendCount) parts.push(i18n(m.friendCount === 1 ? "profile.mutualFriendsOne" : "profile.mutualFriends", m.friendCount));
  if (m.groupCount) parts.push(i18n(m.groupCount === 1 ? "profile.mutualGroupsOne" : "profile.mutualGroups", m.groupCount));
  button.innerHTML = `${faces ? `<span class="profile-mutual-faces">${faces}</span>` : ""}<span>${parts.join(" · ")}</span>`;
  button.classList.remove("d-none");
}

function openMutualList() {
  const m = mutualData;
  if (!m) return;

  document.getElementById("mutualFriendsSection").classList.toggle("d-none", m.friendCount === 0);
  document.getElementById("mutualGroupsSection").classList.toggle("d-none", m.groupCount === 0);

  document.getElementById("mutualFriendsHeading").textContent = i18n(m.friendCount === 1 ? "profile.mutualFriendsOne" : "profile.mutualFriends", m.friendCount);
  document.getElementById("mutualFriendsList").innerHTML = m.friends.map(f => `
    <a href="profile.html?id=${f.userId}" class="mutual-row">
      <img src="${escapeHtml(f.avatarUrl || DEFAULT_AVATAR)}" class="avatar-sm" alt="">
      <span class="min-w-0 flex-grow-1">
        <span class="d-block fw-semibold text-truncate" data-no-i18n>${escapeHtml(f.fullName)}</span>
        <span class="d-block small text-muted text-truncate" data-no-i18n>@${escapeHtml(f.userName)}</span>
      </span>
    </a>`).join("");

  document.getElementById("mutualGroupsHeading").textContent = i18n(m.groupCount === 1 ? "profile.mutualGroupsOne" : "profile.mutualGroups", m.groupCount);
  document.getElementById("mutualGroupsList").innerHTML = m.groups.map(g => `
    <a href="group.html?id=${g.id}" class="mutual-row">
      ${g.coverImageUrl
        ? `<img src="${escapeHtml(g.coverImageUrl)}" class="mutual-group-cover" alt="">`
        : `<span class="mutual-group-cover mutual-group-empty"><i class="bi bi-people-fill"></i></span>`}
      <span class="min-w-0 flex-grow-1">
        <span class="d-block fw-semibold text-truncate" data-no-i18n>${escapeHtml(g.name)}</span>
        <span class="d-block small text-muted">${i18n(g.memberCount === 1 ? "profile.groupMembersOne" : "profile.groupMembers", g.memberCount)}</span>
      </span>
    </a>`).join("");

  bootstrap.Modal.getOrCreateInstance(document.getElementById("mutualModal")).show();
}

// ---- Profili Paylaş: the short link and its QR code ----
// The link is /u/<username>: the server sends it to the profile page (or to the 404 page for a name nobody has).
function profileShareLink() {
  return `${window.location.origin}/u/${encodeURIComponent(currentProfile.userName)}`;
}

// Draws the QR code of `text` on the canvas, as large as fits `size` pixels in whole squares, with the quiet border scanners need.
function drawQrCode(canvas, text, size) {
  const qr = qrcode(0, "M"); // 0: the smallest version that holds the text; "M": it still reads with a part of it covered
  qr.addData(text);
  qr.make();

  const modules = qr.getModuleCount();
  const border = 4;
  const total = modules + border * 2;
  const cell = Math.max(Math.floor(size / total), 1);
  canvas.width = canvas.height = cell * total;

  const context = canvas.getContext("2d");
  context.fillStyle = "#ffffff";
  context.fillRect(0, 0, canvas.width, canvas.height);
  context.fillStyle = "#000000";
  for (let row = 0; row < modules; row++) {
    for (let col = 0; col < modules; col++) {
      if (qr.isDark(row, col)) context.fillRect((col + border) * cell, (row + border) * cell, cell, cell);
    }
  }
}

function openShareProfile() {
  const link = profileShareLink();
  document.getElementById("shareProfileLink").value = link;
  try {
    drawQrCode(document.getElementById("shareQrCanvas"), link, 512);
  } catch (err) {
    toast("QR kodu oluşturulamadı."); // the link can still be copied
  }
  bootstrap.Modal.getOrCreateInstance(document.getElementById("shareProfileModal")).show();
}

async function copyProfileLink() {
  const link = profileShareLink();
  try {
    await navigator.clipboard.writeText(link);
    toast("Bağlantı kopyalandı.");
  } catch (err) {
    // Clipboard access can be blocked (insecure origin, permissions): show the link so it can be copied by hand.
    linkDialog(link);
  }
}

// The PNG is drawn large (about 1000 px) so it prints and zooms well, whatever the size on screen.
function downloadProfileQr() {
  const canvas = document.createElement("canvas");
  try {
    drawQrCode(canvas, profileShareLink(), 1024);
  } catch (err) {
    toast("QR kodu oluşturulamadı.");
    return;
  }

  const link = document.createElement("a");
  link.href = canvas.toDataURL("image/png");
  link.download = `stoop-${currentProfile.userName}-qr.png`;
  document.body.appendChild(link);
  link.click();
  link.remove();
}

// ---- Etiketlendiklerim: posts this person is tagged in ----
let taggedPager = null;

// Loaded when the tab is first opened, and again after the profile was reloaded (a block, say, changes what is shown).
function showTaggedTabIfOpen() {
  taggedPager = null;
  if (document.getElementById("tab-tagged").classList.contains("active")) loadTaggedPosts();
}

function loadTaggedPosts() {
  if (!taggedPager) {
    taggedPager = createPager({
      list: "taggedPostsList",
      url: page => `/api/users/${profileUserId}/tagged-posts?page=${page}&pageSize=20`,
      render: post => postCardHtml(post),
      shown: post => !!document.querySelector(`#taggedPostsList .card[data-post-id="${post.id}"]`),
      emptyHtml: `<div class="text-center text-muted small py-4">${"Henüz etiketlenmiş gönderi yok."}</div>`
    });
  }
  return taggedPager.reload();
}

document.querySelector('[data-bs-target="#tab-tagged"]').addEventListener("shown.bs.tab", () => {
  if (!taggedPager) loadTaggedPosts();
});
