// games.html — the games hub (X-O, rock-paper-scissors and Battleship): invitations, running games, recent results, and the friend picker that starts a game.
// The server decides everything (who may invite, whose turn it is, who won); this page shows its answer and sends the clicks.

requireAuth();

let overviewRequest = 0; // answers can arrive out of order: only the newest one may draw
let reloadTimer = null;
let pickerFriends = [];
let pickerKind = "TicTacToe"; // the game the open friend picker will invite to

function gameRowHtml(game, { badge = "", sub = "", actions = "" }) {
  const other = game.opponent;
  return `
    <div class="xo-row" data-game-id="${game.id}">
      <a href="profile.html?id=${other.id}" tabindex="-1" aria-hidden="true"><img src="${other.avatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt=""></a>
      <div class="xo-row-body">
        <div class="xo-row-name"><a href="game.html?id=${game.id}" class="text-body text-decoration-none" data-no-i18n>${escapeHtml(other.name)}</a><span class="xo-kind" data-no-i18n>${escapeHtml(gameKindName(gameKind(game)))}</span>${badge}</div>
        <div class="text-muted small">${sub}</div>
      </div>
      <div class="xo-row-actions">${actions}</div>
    </div>`;
}

function fillList(listId, countId, games, rowHtml, emptyText) {
  document.getElementById(listId).innerHTML = games.length
    ? games.map(rowHtml).join("")
    : `<div class="xo-empty">${emptyText}</div>`;
  if (countId) document.getElementById(countId).textContent = games.length;
}

const incomingRow = game => gameRowHtml(game, {
  sub: `Seni davet etti · ${timeAgo(game.createdAt)}`,
  actions: `<button type="button" class="btn btn-primary btn-sm rounded-pill" data-game-action="accept">Kabul Et</button>
            <button type="button" class="btn btn-light border btn-sm rounded-pill" data-game-action="decline">Reddet</button>`
});

const sentRow = game => gameRowHtml(game, {
  sub: `Yanıt bekleniyor · ${timeAgo(game.createdAt)}`,
  actions: `<button type="button" class="btn btn-light border btn-sm rounded-pill" data-game-action="cancel">İptal Et</button>`
});

const activeRow = game => gameRowHtml(game, {
  // Battleship, while the fleets are placed: "my turn" means the fleet is still to be placed
  badge: game.isMyTurn ? `<span class="xo-turn-badge">${game.kind === "Battleship" && game.phase === "Placing" ? "Filonu diz" : "Sıra sende"}</span>` : "",
  sub: `Son etkinlik · ${timeAgo(game.updatedAt)}`,
  actions: `<a href="game.html?id=${game.id}" class="btn btn-primary btn-sm rounded-pill">Oyna</a>`
});

const recentRow = game => {
  const resign = gameResignText(game);
  return gameRowHtml(game, {
    badge: gameResultBadgeHtml(game),
    sub: `${resign ? escapeHtml(resign) + " · " : ""}${timeAgo(game.finishedAt || game.updatedAt)}`,
    actions: `<a href="game.html?id=${game.id}" class="btn btn-light border btn-sm rounded-pill">Görüntüle</a>`
  });
};

function renderOverview(overview) {
  document.getElementById("statWins").textContent = overview.stats.wins;
  document.getElementById("statLosses").textContent = overview.stats.losses;
  document.getElementById("statDraws").textContent = overview.stats.draws;

  fillList("incomingList", "incomingCount", overview.incomingInvites, incomingRow, "Bekleyen davetin yok.");
  fillList("sentList", "sentCount", overview.sentInvites, sentRow, "Gönderdiğin bekleyen davet yok.");
  fillList("activeList", "activeCount", overview.active, activeRow, "Devam eden oyunun yok. Bir arkadaşını davet ederek başla.");
  fillList("recentList", null, overview.recent, recentRow, "Henüz biten bir oyunun yok.");
}

async function loadOverview() {
  const request = ++overviewRequest;
  try {
    const overview = await apiFetch("/api/games");
    if (request === overviewRequest) renderOverview(overview);
  } catch (err) {
    if (request !== overviewRequest) return;
    document.getElementById("incomingList").innerHTML = `<div class="alert alert-danger small mb-0">${escapeHtml(err.message)}</div>`;
  }
}

// ---- Answering an invitation (the buttons of the rows) ----

async function answerInvite(gameId, action, row) {
  const buttons = row.querySelectorAll(".xo-row-actions button");
  buttons.forEach(b => b.disabled = true);

  try {
    const game = await apiFetch(`/api/games/${gameId}/${action}`, { method: "POST" });
    if (action === "accept") {
      window.location.href = `game.html?id=${game.id}`;
      return;
    }
    toast(action === "decline" ? "Davet reddedildi." : "Davet iptal edildi.");
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
  await loadOverview(); // also after a failure: the game may have changed meanwhile (the other player answered first)
}

document.getElementById("xoPage").addEventListener("click", event => {
  const button = event.target.closest("[data-game-action]");
  const row = button?.closest("[data-game-id]");
  if (row) answerInvite(row.dataset.gameId, button.dataset.gameAction, row);
});

// ---- The friend picker ----

function pickerRowHtml(friend) {
  return `
    <div class="xo-row" data-friend-id="${friend.userId}">
      <img src="${friend.avatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt="">
      <div class="xo-row-body"><div class="xo-row-name" data-no-i18n>${escapeHtml(friend.name)}</div></div>
      <button type="button" class="btn btn-primary btn-sm rounded-pill" data-invite-friend>Davet Et</button>
    </div>`;
}

function drawPicker() {
  const list = document.getElementById("pickFriendList");
  const search = document.getElementById("pickFriendSearch").value.trim().toLocaleLowerCase(localeTag());
  const shown = pickerFriends.filter(f => f.name.toLocaleLowerCase(localeTag()).includes(search));

  if (pickerFriends.length === 0) {
    list.innerHTML = `<div class="xo-empty">Henüz arkadaşın yok. Oynamak için önce birkaç arkadaş ekle.
      <div class="mt-2"><a href="friends.html" class="btn btn-light border btn-sm rounded-pill">Arkadaşlar</a></div></div>`;
  } else {
    list.innerHTML = shown.length ? shown.map(pickerRowHtml).join("") : `<div class="xo-empty">Aramana uyan arkadaş bulunamadı.</div>`;
  }
}

async function loadPickerFriends() {
  const list = document.getElementById("pickFriendList");
  list.innerHTML = `<div class="text-muted small">Yükleniyor...</div>`;
  try {
    pickerFriends = await apiFetch("/api/friends");
    drawPicker();
  } catch (err) {
    list.innerHTML = `<div class="alert alert-danger small mb-0">${escapeHtml(err.message)}</div>`;
  }
}

async function inviteFriend(friendId, button) {
  button.disabled = true;
  try {
    const game = await apiFetch("/api/games", { method: "POST", body: { opponentId: friendId, kind: pickerKind } });
    window.location.href = `game.html?id=${game.id}`;
  } catch (err) {
    button.disabled = false;
    toast(err.message || "Davet gönderilemedi.");
  }
}

document.getElementById("pickFriendModal").addEventListener("show.bs.modal", event => {
  const asked = event.relatedTarget?.dataset.gameKind;
  pickerKind = GAME_KINDS.includes(asked) ? asked : "TicTacToe";
  document.getElementById("pickFriendTitle").textContent = i18n("games.inviteTo", gameKindName(pickerKind));
  document.getElementById("pickFriendSearch").value = "";
  loadPickerFriends();
});
document.getElementById("pickFriendModal").addEventListener("shown.bs.modal", () => document.getElementById("pickFriendSearch").focus());
document.getElementById("pickFriendSearch").addEventListener("input", drawPicker);
document.getElementById("pickFriendList").addEventListener("click", event => {
  const button = event.target.closest("[data-invite-friend]");
  if (button) inviteFriend(button.closest("[data-friend-id]").dataset.friendId, button);
});

// ---- Live ----

// A move, an answer or a new invitation anywhere: draw the overview again (a short wait gathers a burst into one request).
function reloadSoon() {
  clearTimeout(reloadTimer);
  reloadTimer = setTimeout(loadOverview, 200);
}

document.addEventListener("realtime:game-updated", reloadSoon);
document.addEventListener("realtime:reconnected", loadOverview);

loadOverview();
