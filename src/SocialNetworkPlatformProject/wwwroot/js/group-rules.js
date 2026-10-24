function groupAvatarHtml(group, sizeClass = "avatar-sm") {
  const url = group.avatarUrl || group.groupAvatarUrl;
  if (url) return `<img src="${url}" class="${sizeClass} group-avatar" alt="">`;
  const name = group.name || group.groupName || "?";
  return `<span class="${sizeClass} group-avatar group-avatar-empty" aria-hidden="true" data-no-i18n>${escapeHtml(name.trim().charAt(0).toLocaleUpperCase(localeTag()) || "?")}</span>`;
}

async function acceptGroupRulesDialog(groupId, groupName, confirmText = "Gruba Katıl") {
  let rules;
  try {
    rules = await apiFetch(`/api/groups/${groupId}/rules`);
  } catch (err) {
    toast(err.message || "Kurallar yüklenemedi.");
    return false;
  }
  if (!rules.length) return true;

  const items = rules.map((r, i) => `
    <li class="group-rule-item">
      <div class="fw-semibold" data-no-i18n>${i + 1}. ${escapeHtml(r.title)}</div>
      ${r.text ? `<div class="small text-muted" data-no-i18n>${escapeHtml(r.text)}</div>` : ""}
    </li>`).join("");

  return new Promise(resolve => {
    openSiteDialog(`
      <div class="fw-bold fs-5 mb-1">Grup Kuralları</div>
      <div class="small text-muted mb-3" data-no-i18n>${i18n("group.rulesIntro", `<b>${escapeHtml(groupName || "")}</b>`)}</div>
      <ol class="group-rules-list list-unstyled mb-3">${items}</ol>
      <label class="form-check d-flex align-items-center gap-2 mb-4 group-rules-accept">
        <input type="checkbox" class="form-check-input m-0" data-accept>
        <span class="form-check-label">Kuralları okudum ve kabul ediyorum</span>
      </label>
      <div class="d-flex justify-content-end flex-wrap gap-2">
        <button type="button" class="btn btn-light rounded-pill px-4" data-answer="no">Vazgeç</button>
        <button type="button" class="btn btn-primary rounded-pill px-4" data-answer="yes" disabled></button>
      </div>`,
      (box, close) => {
        const answer = value => { close(); resolve(value); };
        const yes = box.querySelector('[data-answer="yes"]');
        yes.textContent = confirmText;
        box.querySelector("[data-accept]").addEventListener("change", e => { yes.disabled = !e.target.checked; });
        yes.addEventListener("click", () => answer(true));
        box.querySelector('[data-answer="no"]').addEventListener("click", () => answer(false));
        box.addEventListener("dismiss", () => answer(false));
        box.querySelector("[data-accept]").focus();
      });
  });
}
