let mutualData = null;

async function loadMutual(profile) {
  const button = document.getElementById("profileMutual");
  mutualData = null;
  button.classList.add("d-none");
  if (profile.isOwnProfile || profile.isBlockedByMe) return;

  try {
    mutualData = await apiFetch(`/api/users/${profile.id}/mutual`);
  } catch (err) {
    return;
  }

  const m = mutualData;
  if (!m.friendCount && !m.groupCount) return;

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

function profileShareLink() {
  return `${window.location.origin}/u/${encodeURIComponent(currentProfile.userName)}`;
}

function drawQrCode(canvas, text, size) {
  const qr = qrcode(0, "M");
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
    toast("QR kodu oluşturulamadı.");
  }
  bootstrap.Modal.getOrCreateInstance(document.getElementById("shareProfileModal")).show();
}

async function copyProfileLink() {
  const link = profileShareLink();
  try {
    await navigator.clipboard.writeText(link);
    toast("Bağlantı kopyalandı.");
  } catch (err) {
    linkDialog(link);
  }
}

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

let taggedPager = null;

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
