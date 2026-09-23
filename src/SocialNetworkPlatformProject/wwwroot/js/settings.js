// settings.html — account, privacy, password and account deletion wired to the backend.

const session = requireAuth();

let settingsAvatarMarkedForRemoval = false;

async function loadSettings() {
  try {
    const p = await apiFetch(`/api/users/${session.userId}`);
    document.getElementById("settingsFullName").value = p.fullName;
    document.getElementById("settingsUserName").value = p.userName;
    document.getElementById("settingsLocation").value = p.location || "";
    document.getElementById("settingsOccupation").value = p.occupation || "";
    document.getElementById("settingsBio").value = p.bio || "";
    document.getElementById("settingsAvatarPreview").src = p.avatarUrl || DEFAULT_AVATAR;
    document.getElementById("privateAccount").checked = p.isPrivateAccount;
    settingsAvatarMarkedForRemoval = false;
    document.getElementById("removeSettingsAvatarBtn").classList.toggle("d-none", !p.avatarUrl);
  } catch (err) {
    toast(err.message || "Ayarlar yüklenemedi.");
  }
}

function previewSettingsAvatar(input) {
  const preview = document.getElementById("settingsAvatarPreview");
  if (input.files && input.files[0] && preview) {
    settingsAvatarMarkedForRemoval = false;
    preview.src = URL.createObjectURL(input.files[0]);
  }
}

function markSettingsAvatarForRemoval() {
  settingsAvatarMarkedForRemoval = true;
  document.getElementById("settingsAvatarInput").value = "";
  document.getElementById("settingsAvatarPreview").src = DEFAULT_AVATAR;
}

async function saveAccount() {
  const btn = document.getElementById("saveAccountBtn");
  btn.disabled = true;
  try {
    const formData = new FormData();
    formData.append("fullName", document.getElementById("settingsFullName").value.trim());
    formData.append("bio", document.getElementById("settingsBio").value.trim());
    formData.append("location", document.getElementById("settingsLocation").value.trim());
    formData.append("occupation", document.getElementById("settingsOccupation").value.trim());

    const avatarFile = document.getElementById("settingsAvatarInput").files[0];
    if (avatarFile) formData.append("avatar", avatarFile);
    else if (settingsAvatarMarkedForRemoval) formData.append("removeAvatar", "true");

    await apiFetchForm("/api/users/me/profile", { method: "PUT", body: formData });
    toast("Hesap bilgileri kaydedildi.");
    await loadSettings();
  } catch (err) {
    toast(err.message || "Kaydedilemedi.");
  } finally {
    btn.disabled = false;
  }
}

async function savePrivacy() {
  try {
    await apiFetch("/api/users/me/privacy", {
      method: "PUT",
      body: {
        isPrivateAccount: document.getElementById("privateAccount").checked,
        showOnlineStatus: document.getElementById("showOnline").checked
      }
    });
    toast("Gizlilik ayarları kaydedildi.");
  } catch (err) {
    toast(err.message || "Kaydedilemedi.");
  }
}

async function submitPasswordForm(event) {
  event.preventDefault();
  const newPassword = document.getElementById("newPassword").value;
  const confirmNewPassword = document.getElementById("newPasswordConfirm").value;
  const error = document.getElementById("passwordError");

  if (newPassword !== confirmNewPassword) {
    error.textContent = "Yeni şifreler eşleşmiyor.";
    error.classList.remove("d-none");
    return false;
  }

  try {
    await apiFetch("/api/users/me/password", {
      method: "PUT",
      body: { currentPassword: document.getElementById("currentPassword").value, newPassword, confirmNewPassword }
    });
    error.classList.add("d-none");
    document.getElementById("passwordForm").reset();
    toast("Şifren güncellendi.");
  } catch (err) {
    error.textContent = err.message || "Şifre güncellenemedi.";
    error.classList.remove("d-none");
  }
  return false;
}

async function deleteAccount() {
  try {
    await apiFetch("/api/users/me", { method: "DELETE" });
    clearSession();
    window.location.href = "index.html";
  } catch (err) {
    toast(err.message || "Hesap silinemedi.");
  }
}

loadSettings();
