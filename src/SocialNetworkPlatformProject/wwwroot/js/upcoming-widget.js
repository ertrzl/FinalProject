(async function loadUpcomingWidget() {
  const box = document.getElementById("upcomingWidget");
  if (!box || getDesign() !== "inbox") return;

  try {
    const events = await apiFetch("/api/events/mine?scope=attending");
    const next = events.filter(ev => !ev.isPast).sort((a, b) => new Date(a.startsAt) - new Date(b.startsAt))[0];
    if (!next) return;

    const start = new Date(next.startsAt);
    const when = `${start.toLocaleDateString(localeTag(), { weekday: "long", day: "numeric", month: "long" })} · ${start.toLocaleTimeString(localeTag(), { hour: "2-digit", minute: "2-digit" })}`;
    box.innerHTML = `
      <div class="cap mb-2">Yaklaşan</div>
      <a href="event.html?id=${next.id}" class="upcoming-card">
        <div class="upcoming-when" data-no-i18n>${when}</div>
        <div class="upcoming-title" data-no-i18n>${escapeHtml(next.title)}</div>
        <div class="upcoming-place">${next.isOnline ? "Çevrimiçi" : `<span data-no-i18n>${escapeHtml(next.location || "")}</span>`} · ${next.goingCount} katılımcı</div>
      </a>`;
  } catch (err) {
  }
})();
