requireAuth();

const gameId = new URLSearchParams(window.location.search).get("id");
const byId = id => document.getElementById(id);

let game = null;
let busy = false;
let shownBoard = null;
let shownStatus = null;
let shownRounds = null;
let shownHistory = "";

const isRps = () => gameKind(game) === "RockPaperScissors";
const isBs = () => gameKind(game) === "Battleship";

const CHOICES = {
  Rock: { emoji: "✊", key: "game.choiceRock" },
  Paper: { emoji: "✋", key: "game.choicePaper" },
  Scissors: { emoji: "✌️", key: "game.choiceScissors" }
};
const ROUND_RESULT = { Win: "win", Loss: "loss", Draw: "draw" };
const choiceName = choice => CHOICES[choice] ? i18n(CHOICES[choice].key) : "";


function buildBoard() {
  const board = byId("gameBoard");
  board.innerHTML = Array.from({ length: 9 }, (_, i) =>
    `<button type="button" class="xo-cell" data-cell="${i}" aria-disabled="true"><span aria-hidden="true"></span></button>`).join("");

  board.addEventListener("click", event => {
    const cell = event.target.closest("[data-cell]");
    if (cell && cell.getAttribute("aria-disabled") !== "true") makeMove(Number(cell.dataset.cell));
  });

  board.addEventListener("keydown", event => {
    const cell = event.target.closest("[data-cell]");
    const step = { ArrowLeft: [0, -1], ArrowRight: [0, 1], ArrowUp: [-1, 0], ArrowDown: [1, 0] }[event.key];
    if (!cell || !step) return;

    const index = Number(cell.dataset.cell);
    const row = Math.floor(index / 3) + step[0];
    const column = (index % 3) + step[1];
    if (row < 0 || row > 2 || column < 0 || column > 2) return;
    event.preventDefault();
    board.children[row * 3 + column].focus();
  });
}

function cellLabel(index, mark) {
  return i18n("game.cellLabel", Math.floor(index / 3) + 1, (index % 3) + 1, mark || i18n("game.cellEmpty"));
}

function renderBoard() {
  const board = byId("gameBoard");
  const winning = new Set(game.winningLine || []);
  const canPlay = game.status === "Active" && game.isMyTurn && !busy;

  board.classList.toggle("is-idle", game.status !== "Active" && game.status !== "Finished");
  board.classList.toggle("is-busy", busy);

  const changed = shownBoard !== game.board;

  [...board.children].forEach((cell, index) => {
    const char = game.board?.[index];
    const mark = char === "X" || char === "O" ? char : "";
    const playable = canPlay && !mark;

    if (cell.dataset.mark !== mark) cell.firstElementChild.textContent = mark;
    if (changed) cell.classList.toggle("is-new", !!mark && shownBoard !== null && shownBoard[index] !== mark);
    cell.dataset.mark = mark;
    cell.dataset.ghost = game.myMark || "";
    cell.classList.toggle("is-playable", playable);
    cell.classList.toggle("is-win", winning.has(index));
    cell.setAttribute("aria-disabled", playable ? "false" : "true");
    cell.setAttribute("aria-label", cellLabel(index, mark));
  });
  shownBoard = game.board;
}

async function makeMove(cell) {
  busy = true;
  renderBoard();
  try {
    applyGame(await apiFetch(`/api/games/${gameId}/move`, { method: "POST", body: { cell } }));
  } catch (err) {
    toast(err.message || "Hamle yapılamadı.");
    await loadGame();
  } finally {
    busy = false;
    if (game) renderBoard();
  }
}


function playerHtml(person, mark, { isTurn = game.status === "Active" && !!mark && game.turn === mark, extra = "" } = {}) {
  const isMe = isSameId(person.id, getSession()?.userId);
  return `
    <div class="xo-player ${isTurn ? "is-turn" : ""}">
      <img src="${person.avatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt="">
      <div class="min-w-0">
        <a href="profile.html?id=${person.id}" class="xo-player-name text-body text-decoration-none" data-no-i18n title="${escapeHtml(person.name)}">${escapeHtml(person.name)}</a>
        <div class="small text-muted">${isMe ? "Sen" : "Rakip"}</div>
      </div>
      ${mark ? `<span class="xo-player-mark" data-mark="${mark}">${mark}</span>` : extra}
    </div>`;
}

function rpsPlayerHtml(person) {
  const isMe = isSameId(person.id, getSession()?.userId);
  const active = game.status === "Active";
  const picked = active && (isMe ? !game.isMyTurn : game.opponentHasChosen);
  return playerHtml(person, "", {
    isTurn: active && !picked,
    extra: picked ? `<span class="rps-picked" role="img" aria-label="Seçti" title="Seçti"><i class="bi bi-check-circle-fill"></i></span>` : ""
  });
}

function renderPlayers() {
  if (isBs()) {
    byId("gamePlayers").innerHTML = bsPlayerHtml(game.inviter) + bsPlayerHtml(game.invitee);
    return;
  }
  if (isRps()) {
    byId("gamePlayers").innerHTML = rpsPlayerHtml(game.inviter) + rpsPlayerHtml(game.invitee);
    return;
  }

  byId("gamePlayers").innerHTML = game.playerX && game.playerO
    ? playerHtml(game.playerX, "X") + playerHtml(game.playerO, "O")
    : playerHtml(game.inviter, "") + playerHtml(game.invitee, "");
}

function statusInfo() {
  const other = game.opponent?.name || "";

  switch (game.status) {
    case "Invited":
      return game.isInvitedToMe
        ? { text: i18n("game.invitedYou", other, gameKindName(gameKind(game))) }
        : { text: "Davet gönderildi", sub: i18n("game.waitingFor", other) };
    case "Active":
      if (isBs()) return bsStatusInfo();
      if (isRps()) {
        if (!game.isMyTurn) return { text: i18n("game.waitingPick", other) };
        return game.opponentHasChosen
          ? { text: i18n("game.opponentPicked", other) }
          : { text: "Sıra sende", sub: "Seçimin gizli kalır: ikiniz de seçince açılır." };
      }
      return { text: game.isMyTurn ? "Sıra sende" : i18n("game.turnOf", other) };
    case "Finished": {
      const result = gameResult(game);
      return {
        text: result === "win" ? "Kazandın!" : result === "loss" ? "Kaybettin" : "Berabere",
        tone: result,
        sub: gameResignText(game)
      };
    }
    case "Declined": return { text: "Davet reddedildi." };
    case "Cancelled": return { text: "Davet iptal edildi." };
    default: return { text: "Bu oyunun süresi doldu." };
  }
}

function renderStatus() {
  const info = statusInfo();
  const status = byId("gameStatus");
  if (info.text !== shownStatus) status.textContent = info.text;
  shownStatus = info.text;
  status.classList.toggle("is-win", info.tone === "win");
  status.classList.toggle("is-loss", info.tone === "loss");
  byId("gameSub").textContent = info.sub || "";
}

function actionsHtml() {
  switch (game.status) {
    case "Invited":
      return game.isInvitedToMe
        ? `<button type="button" class="btn btn-primary rounded-pill px-4" data-act="accept">Kabul Et</button>
           <button type="button" class="btn btn-light border rounded-pill px-4" data-act="decline">Reddet</button>`
        : `<button type="button" class="btn btn-light border rounded-pill px-4" data-act="cancel">İptal Et</button>`;
    case "Active":
      return `<button type="button" class="btn btn-outline-danger rounded-pill px-4" data-act="resign"><i class="bi bi-flag me-1"></i>Pes Et</button>`;
    case "Finished":
    case "Expired":
      return `<button type="button" class="btn btn-primary rounded-pill px-4" data-act="rematch"><i class="bi bi-arrow-repeat me-1"></i>Tekrar Oyna</button>`
        + (isRps() || isBs() ? `<a href="games.html" class="btn btn-light border rounded-pill px-4">Oyunlara dön</a>` : "");
    default:
      return "";
  }
}

function render() {
  byId("gameLoading").classList.add("d-none");
  byId("gameError").classList.add("d-none");
  byId("gameView").classList.remove("d-none");

  const rps = isRps();
  const battleship = isBs();
  byId("gameTitle").textContent = gameKindName(gameKind(game));
  byId("gameView").closest(".xo-page").classList.toggle("is-bs", battleship);
  byId("gameView").closest(".xo-game").classList.toggle("is-bs", battleship);
  byId("gameBoard").classList.toggle("d-none", rps || battleship);
  byId("rpsScore").classList.toggle("d-none", !rps || (game.status !== "Active" && game.status !== "Finished"));
  byId("rpsArea").classList.toggle("d-none", !rps);

  renderPlayers();
  renderStatus();
  if (battleship) renderBattleship();
  else if (rps) renderRps();
  else renderBoard();
  byId("gameActions").innerHTML = actionsHtml();
}

function applyGame(next) {
  if (game && parseServerDate(next.updatedAt) < parseServerDate(game.updatedAt)) return;
  game = next;
  render();
}

function showError(message) {
  byId("gameLoading").classList.add("d-none");
  byId("gameView").classList.add("d-none");
  const box = byId("gameError");
  box.textContent = message;
  box.classList.remove("d-none");
}

async function loadGame() {
  if (!gameId) {
    showError("Oyun bulunamadı.");
    return;
  }

  try {
    applyGame(await apiFetch(`/api/games/${gameId}`));
  } catch (err) {
    if (!game) showError(err.message);
    else toast(err.message);
  }
}


function buildPicks() {
  const picks = byId("rpsPicks");
  picks.innerHTML = Object.entries(CHOICES).map(([choice, info]) => `
    <button type="button" class="rps-pick" data-choice="${choice}" aria-disabled="true" aria-pressed="false">
      <span class="rps-pick-emoji" aria-hidden="true">${info.emoji}</span>
      <span class="rps-pick-word">${escapeHtml(i18n(info.key))}</span>
    </button>`).join("");

  picks.addEventListener("click", event => {
    const pick = event.target.closest("[data-choice]");
    if (pick && pick.getAttribute("aria-disabled") !== "true") makeChoice(pick.dataset.choice);
  });

  picks.addEventListener("keydown", event => {
    const pick = event.target.closest("[data-choice]");
    const step = { ArrowLeft: -1, ArrowRight: 1 }[event.key];
    if (!pick || !step) return;
    event.preventDefault();
    const buttons = [...picks.children];
    buttons[(buttons.indexOf(pick) + step + buttons.length) % buttons.length].focus();
  });
}

async function makeChoice(choice) {
  busy = true;
  renderRps();
  try {
    applyGame(await apiFetch(`/api/games/${gameId}/choose`, { method: "POST", body: { choice } }));
  } catch (err) {
    toast(err.message || "Seçim yapılamadı.");
    await loadGame();
  } finally {
    busy = false;
    if (game) renderRps();
  }
}

function renderScoreboard() {
  const mine = game.myScore ?? 0;
  const theirs = game.opponentScore ?? 0;
  const board = byId("rpsScore");
  board.setAttribute("aria-label", i18n("game.scoreLabel", mine, theirs));
  board.innerHTML = `
    <div class="rps-score-line" aria-hidden="true">
      <span class="rps-score-who">Sen</span>
      <b class="rps-score-num">${mine}</b><span class="rps-score-dash">–</span><b class="rps-score-num">${theirs}</b>
      <span class="rps-score-who">Rakip</span>
    </div>
    <div class="rps-score-round">${escapeHtml(i18n("game.round", game.roundNumber || 1, game.totalRounds || 3))}</div>`;
}

function renderStage() {
  const mine = CHOICES[game.myChoice];
  byId("rpsStage").innerHTML = `
    <div class="rps-slot ${mine ? "is-filled" : ""}"><span class="rps-slot-hand">${mine ? mine.emoji : "…"}</span><span class="rps-slot-who">Sen</span></div>
    <span class="rps-slot-vs">vs</span>
    <div class="rps-slot ${game.opponentHasChosen ? "is-filled" : ""}"><span class="rps-slot-hand">${game.opponentHasChosen ? "🔒" : "…"}</span><span class="rps-slot-who">Rakip</span></div>`;
}

function renderPicks() {
  const canPick = game.status === "Active" && game.isMyTurn && !busy;
  byId("rpsPicks").classList.toggle("is-busy", busy);

  byId("rpsPicks").querySelectorAll("[data-choice]").forEach(pick => {
    const chosen = game.myChoice === pick.dataset.choice;
    pick.classList.toggle("is-chosen", chosen);
    pick.classList.toggle("is-playable", canPick);
    pick.setAttribute("aria-disabled", canPick ? "false" : "true");
    pick.setAttribute("aria-pressed", chosen ? "true" : "false");
    pick.setAttribute("aria-label", i18n(chosen ? "game.yourPick" : "game.pickLabel", choiceName(pick.dataset.choice)));
  });
}

function handHtml(choice) {
  const info = CHOICES[choice];
  return info ? `<span class="rps-hand" aria-hidden="true"><span class="rps-hand-emoji">${info.emoji}</span> ${escapeHtml(i18n(info.key))}</span>` : "";
}

function renderHistory() {
  const rounds = game.rounds || [];
  const signature = JSON.stringify(rounds);
  if (signature === shownHistory) return;

  const firstNew = shownRounds === null ? rounds.length : shownRounds;
  shownHistory = signature;
  shownRounds = rounds.length;

  byId("rpsHistory").innerHTML = rounds.length ? `
    <h2 class="xo-section-title">Turlar</h2>
    <ol class="rps-rounds">${rounds.map((round, index) => {
      const result = ROUND_RESULT[round.result] || "draw";
      const label = GAME_RESULT_LABEL[result];
      return `
      <li class="rps-round is-${result} ${index >= firstNew ? "is-new" : ""}">
        <span class="visually-hidden">${escapeHtml(i18n("game.roundLabel", index + 1, choiceName(round.myChoice), choiceName(round.opponentChoice)))}</span>
        <span class="rps-round-no" aria-hidden="true">${index + 1}</span>
        ${handHtml(round.myChoice)}<span class="rps-vs" aria-hidden="true">vs</span>${handHtml(round.opponentChoice)}
        <span class="badge rounded-pill ${label.css}">${label.text}</span>
      </li>`;
    }).join("")}</ol>` : "";
}

function renderRps() {
  byId("rpsLive").classList.toggle("d-none", game.status !== "Active");
  if (game.status === "Active" || game.status === "Finished") renderScoreboard();
  if (game.status === "Active") {
    renderStage();
    renderPicks();
  }
  renderHistory();
}


async function runAction(action) {
  byId("gameActions").querySelectorAll("button").forEach(b => b.disabled = true);

  try {
    if (action === "rematch") {
      const next = await apiFetch("/api/games", { method: "POST", body: { opponentId: game.opponent.id, kind: gameKind(game) } });
      window.location.href = `game.html?id=${next.id}`;
      return;
    }
    applyGame(await apiFetch(`/api/games/${gameId}/${action}`, { method: "POST" }));
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
    if (action !== "rematch") await loadGame();
  }
  byId("gameActions").innerHTML = actionsHtml();
}

byId("gameActions").addEventListener("click", async event => {
  const button = event.target.closest("[data-act]");
  if (!button) return;

  if (button.dataset.act === "resign"
    && !(await confirmDialog("Pes etmek istediğine emin misin? Oyunu kaybedersin.", { confirmText: "Pes Et" }))) return;
  runAction(button.dataset.act);
});


document.addEventListener("realtime:game-updated", event => {
  if (isSameId(event.detail?.id, gameId)) applyGame(event.detail);
});
document.addEventListener("realtime:reconnected", loadGame);

buildBoard();
buildPicks();
loadGame();
