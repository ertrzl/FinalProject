const BS_SIZE = 10;
const BS_CELLS = BS_SIZE * BS_SIZE;
const BS_SHIPS = [
  { id: "Carrier", length: 5, key: "game.shipCarrier" },
  { id: "Battleship", length: 4, key: "game.shipBattleship" },
  { id: "Cruiser", length: 3, key: "game.shipCruiser" },
  { id: "Submarine", length: 3, key: "game.shipSubmarine" },
  { id: "Destroyer", length: 2, key: "game.shipDestroyer" }
];

const BS_SEA = `<rect width="400" height="400" fill="#BFDDEC"/><path d="${
  Array.from({ length: BS_SIZE - 1 }, (_, i) => `M${(i + 1) * 40} 0V400M0 ${(i + 1) * 40}H400`).join("")
}" stroke="#9CC4DA" stroke-width="1" fill="none"/>`;

const bs = {
  built: false,
  mode: null,
  placed: {},
  orient: {},
  selected: null,
  drag: null,
  hover: null,
  hoverByKeyboard: false,
  sending: false,
  loadingRandom: false,
  firing: false,
  suppressClick: false,
  tab: "enemy",
  shownShot: undefined
};

const bsShip = id => BS_SHIPS.find(ship => ship.id === id);
const bsShipName = id => (bsShip(id) ? i18n(bsShip(id).key) : "");
const bsCoord = cell => String.fromCharCode(65 + (cell % BS_SIZE)) + (Math.floor(cell / BS_SIZE) + 1);
const bsIsHorizontal = id => (bs.placed[id] ? bs.placed[id].horizontal : bs.orient[id] !== false);
const bsPlacedCount = () => BS_SHIPS.filter(ship => bs.placed[ship.id]).length;

function bsCellsOf(id, row, col, horizontal) {
  return Array.from({ length: bsShip(id).length }, (_, i) =>
    (horizontal ? row : row + i) * BS_SIZE + (horizontal ? col + i : col));
}


function bsShipArt(id, sunk) {
  const length = bsShip(id).length;
  const width = length * 40;
  const hull = fill => `<path d="M8 8H${width - 24}Q${width - 4} 20 ${width - 24} 32H8Q2 20 8 8Z" fill="${fill}"/>`;
  const stripe = x => `<line x1="${x}" y1="14" x2="${x}" y2="26" stroke="#E8793A" stroke-width="3"/>`;
  const turret = (x, radius, end) => `<circle cx="${x}" cy="20" r="${radius}" fill="#8A96A3"/><line x1="${x}" y1="20" x2="${end}" y2="20" stroke="#2F3841" stroke-width="3"/>`;
  const submarineHull = fill => `<rect x="4" y="9" width="112" height="22" rx="11" fill="${fill}"/>`;

  if (sunk) {
    const crosses = Array.from({ length }, (_, i) => {
      const x = i * 40 + 20;
      return `M${x - 8} 14L${x + 8} 26M${x + 8} 14L${x - 8} 26`;
    }).join("");
    return (id === "Submarine" ? submarineHull("#2E353D") : hull("#2E353D"))
      + `<path d="${crosses}" stroke="#D9442B" stroke-width="3" stroke-linecap="round" fill="none"/>`;
  }

  switch (id) {
    case "Carrier":
      return hull("#5F6B78")
        + `<line x1="16" y1="20" x2="162" y2="20" stroke="#E8E2D0" stroke-width="2" stroke-dasharray="8 6"/><rect x="120" y="9" width="22" height="8" rx="2" fill="#3F4A56"/>`
        + stripe(10);
    case "Battleship":
      return hull("#5F6B78") + turret(34, 7, 52) + turret(64, 7, 82) + `<rect x="96" y="14" width="16" height="12" rx="2" fill="#3F4A56"/>` + stripe(10);
    case "Cruiser":
      return hull("#5F6B78") + turret(30, 7, 46) + `<rect x="58" y="15" width="14" height="10" rx="2" fill="#3F4A56"/>` + stripe(10);
    case "Submarine":
      return submarineHull("#4A5663") + `<rect x="48" y="13" width="24" height="14" rx="5" fill="#7D8996"/>` + stripe(14);
    default:
      return hull("#5F6B78") + turret(26, 6, 40) + `<rect x="44" y="15" width="12" height="10" rx="2" fill="#3F4A56"/>` + stripe(10);
  }
}

function bsShipGroup(p, sunk = false, attributes = "") {
  const transform = p.horizontal
    ? `translate(${p.col * 40},${p.row * 40})`
    : `translate(${(p.col + 1) * 40},${p.row * 40}) rotate(90)`;
  return `<g transform="${transform}"${attributes}>${bsShipArt(p.ship, sunk)}</g>`;
}

const bsMissMark = cell => {
  const x = (cell % BS_SIZE) * 40 + 20;
  const y = Math.floor(cell / BS_SIZE) * 40 + 20;
  return `<g fill="none" stroke="#FFFFFF" stroke-width="2.5"><circle cx="${x}" cy="${y}" r="12"/><circle cx="${x}" cy="${y}" r="5"/></g>`;
};

const bsHitMark = cell => {
  const x = (cell % BS_SIZE) * 40 + 20;
  const y = Math.floor(cell / BS_SIZE) * 40 + 20;
  return `<g transform="translate(${x},${y})"><polygon points="0,-15 5,-5 15,-4 8,3 10,14 0,9 -10,14 -8,3 -15,-4 -5,-5" fill="#D9442B"/><circle r="5" fill="#F5B13D"/></g>`;
};


function bsCreateBoard(mount, { interactive, label }) {
  const buttons = interactive
    ? Array.from({ length: BS_CELLS }, (_, i) =>
        `<button type="button" class="bs-cell" data-cell="${i}" tabindex="${i === 0 ? 0 : -1}" aria-disabled="true"></button>`).join("")
    : "";

  mount.classList.add("bs-board");
  mount.innerHTML = `
    <svg class="bs-svg" viewBox="0 0 400 400" aria-hidden="true" focusable="false">${BS_SEA}</svg>
    <div class="bs-last d-none" aria-hidden="true"></div>
    ${interactive ? `<div class="bs-grid" role="group" aria-label="${escapeHtml(label)}">${buttons}</div>` : ""}`;
  if (!interactive) {
    mount.setAttribute("role", "img");
    mount.setAttribute("aria-label", label);
  }

  const board = {
    mount,
    svg: mount.querySelector(".bs-svg"),
    lastMark: mount.querySelector(".bs-last"),
    grid: mount.querySelector(".bs-grid"),
    buttons: [...mount.querySelectorAll(".bs-cell")],
    draw(html) { board.svg.innerHTML = BS_SEA + html; },
    setLast(cell, restart) {
      const mark = board.lastMark;
      if (cell === null || cell === undefined) { mark.classList.add("d-none"); mark.classList.remove("is-new"); return; }
      mark.style.left = `${(cell % BS_SIZE) * 10}%`;
      mark.style.top = `${Math.floor(cell / BS_SIZE) * 10}%`;
      mark.classList.remove("d-none");
      if (restart) {
        mark.classList.remove("is-new");
        void mark.offsetWidth;
        mark.classList.add("is-new");
      }
    }
  };

  if (interactive) {
    board.grid.addEventListener("keydown", event => {
      const cell = event.target.closest(".bs-cell");
      const step = { ArrowLeft: [0, -1], ArrowRight: [0, 1], ArrowUp: [-1, 0], ArrowDown: [1, 0] }[event.key];
      if (!cell || !step) return;
      const index = Number(cell.dataset.cell);
      const row = Math.floor(index / BS_SIZE) + step[0];
      const column = (index % BS_SIZE) + step[1];
      if (row < 0 || row >= BS_SIZE || column < 0 || column >= BS_SIZE) return;
      event.preventDefault();
      board.buttons[row * BS_SIZE + column].focus();
    });
    board.grid.addEventListener("focusin", event => {
      const cell = event.target.closest(".bs-cell");
      if (cell) board.buttons.forEach(button => { button.tabIndex = button === cell ? 0 : -1; });
    });
  }
  return board;
}


function bsBuild() {
  if (bs.built) return;
  bs.built = true;

  bs.placeBoard = bsCreateBoard(byId("bsPlaceBoard"), { interactive: true, label: "Yerleştirme tahtası" });
  bs.enemyBoard = bsCreateBoard(byId("bsEnemyBoard"), { interactive: true, label: "Rakibin tahtası" });
  bs.mineBoard = bsCreateBoard(byId("bsMineBoard"), { interactive: false, label: "Senin filon" });

  byId("bsTray").innerHTML = BS_SHIPS.map(ship => `
    <button type="button" class="bs-tray-ship" data-ship="${ship.id}" aria-pressed="false" style="--len:${ship.length}">
      <svg class="bs-tray-art" viewBox="0 0 ${ship.length * 40} 40" aria-hidden="true" focusable="false">${bsShipArt(ship.id, false)}</svg>
      <span class="bs-tray-name">${escapeHtml(i18n(ship.key))} · ${ship.length}</span>
      <i class="bs-tray-state bi" aria-hidden="true"></i>
    </button>`).join("");

  const grid = bs.placeBoard.grid;
  grid.addEventListener("pointerdown", event => {
    const cell = event.target.closest(".bs-cell");
    const id = cell && bsOccupant(Number(cell.dataset.cell));
    if (id) bsDragStart(event, id, "board");
  });
  grid.addEventListener("click", bsPlaceClick);
  grid.addEventListener("pointermove", event => {
    if (bs.drag || event.pointerType !== "mouse") return;
    const cell = event.target.closest(".bs-cell");
    bsSetHover(cell ? Number(cell.dataset.cell) : null, false);
  });
  grid.addEventListener("pointerleave", () => { if (!bs.hoverByKeyboard) bsSetHover(null, false); });
  grid.addEventListener("focusin", event => {
    const cell = event.target.closest(".bs-cell");
    if (cell && cell.matches(":focus-visible")) bsSetHover(Number(cell.dataset.cell), true);
  });
  grid.addEventListener("focusout", () => bsSetHover(null, false));

  byId("bsTray").addEventListener("pointerdown", event => {
    const row = event.target.closest("[data-ship]");
    if (row) bsDragStart(event, row.dataset.ship, "tray");
  });
  byId("bsTray").addEventListener("click", event => {
    const row = event.target.closest("[data-ship]");
    if (row && event.detail === 0 && !bs.sending) {
      bs.selected = row.dataset.ship;
      bsDrawPlace();
      bsUpdatePlaceUi();
    }
  });

  byId("bsPlace").addEventListener("contextmenu", event => event.preventDefault());

  byId("bsRotate").addEventListener("click", () => bsRotate());
  byId("bsRandom").addEventListener("click", bsRandomFleet);
  byId("bsReady").addEventListener("click", bsSendFleet);

  document.addEventListener("keydown", bsKeyDown);

  bs.enemyBoard.grid.addEventListener("click", event => {
    const cell = event.target.closest(".bs-cell");
    if (cell && cell.getAttribute("aria-disabled") !== "true") bsFire(Number(cell.dataset.cell));
  });
  byId("bsTabs").addEventListener("click", event => {
    const tab = event.target.closest("[data-board-tab]");
    if (!tab) return;
    bs.tab = tab.dataset.boardTab;
    bsUpdateTabs();
  });
}


function bsTakenCells(exceptId) {
  const taken = new Map();
  for (const ship of BS_SHIPS) {
    const p = bs.placed[ship.id];
    if (p && ship.id !== exceptId) bsCellsOf(ship.id, p.row, p.col, p.horizontal).forEach(cell => taken.set(cell, ship.id));
  }
  return taken;
}

const bsOccupant = (cell, exceptId) => bsTakenCells(exceptId).get(cell) || null;

function bsFits(id, row, col, horizontal) {
  const length = bsShip(id).length;
  if (row < 0 || col < 0 || (horizontal ? col + length > BS_SIZE || row >= BS_SIZE : row + length > BS_SIZE || col >= BS_SIZE)) return false;
  const taken = bsTakenCells(id);
  return bsCellsOf(id, row, col, horizontal).every(cell => !taken.has(cell));
}

function bsClamp(id, horizontal, row, col) {
  const length = bsShip(id).length;
  return {
    row: Math.min(Math.max(row, 0), horizontal ? BS_SIZE - 1 : BS_SIZE - length),
    col: Math.min(Math.max(col, 0), horizontal ? BS_SIZE - length : BS_SIZE - 1)
  };
}

function bsGhostSvg(id, row, col, horizontal, valid) {
  const length = bsShip(id).length;
  const width = horizontal ? length * 40 : 40;
  const height = horizontal ? 40 : length * 40;
  const x = col * 40;
  const y = row * 40;
  const stroke = valid ? "#1E7F4F" : "#A82E1A";
  const cross = valid ? "" : `<path d="M${x + width / 2 - 10} ${y + height / 2 - 10}l20 20m0 -20l-20 20" stroke="#A82E1A" stroke-width="4" stroke-linecap="round"/>`;
  return `<rect x="${x}" y="${y}" width="${width}" height="${height}" fill="${valid ? "#2FA66A" : "#D9442B"}" opacity="0.35"/>`
    + `<rect x="${x + 1}" y="${y + 1}" width="${width - 2}" height="${height - 2}" fill="none" stroke="${stroke}" stroke-width="2" stroke-dasharray="6 4"/>`
    + bsShipGroup({ ship: id, row, col, horizontal }, false, ' opacity="0.85"')
    + cross;
}

function bsDrawPlace() {
  const drag = bs.drag && bs.drag.moved ? bs.drag : null;
  let html = "";

  for (const ship of BS_SHIPS) {
    const p = bs.placed[ship.id];
    if (p && !(drag && drag.id === ship.id)) html += bsShipGroup({ ship: ship.id, ...p });
  }

  const selected = bs.selected && !(drag && drag.id === bs.selected) ? bs.placed[bs.selected] : null;
  if (selected) {
    const horizontal = selected.horizontal;
    const length = bsShip(bs.selected).length;
    html += `<rect x="${selected.col * 40 + 2}" y="${selected.row * 40 + 2}" width="${(horizontal ? length : 1) * 40 - 4}" height="${(horizontal ? 1 : length) * 40 - 4}" rx="5" fill="none" stroke="#E8793A" stroke-width="3"/>`;
  }

  if (drag) {
    if (drag.target) html += bsGhostSvg(drag.id, drag.target.row, drag.target.col, drag.horizontal, drag.target.valid);
  } else if (bs.hover !== null && bs.selected && (!bs.placed[bs.selected] || bs.hoverByKeyboard)) {
    const horizontal = bsIsHorizontal(bs.selected);
    const at = bsClamp(bs.selected, horizontal, Math.floor(bs.hover / BS_SIZE), bs.hover % BS_SIZE);
    if (!bsOccupant(bs.hover, bs.selected)) html += bsGhostSvg(bs.selected, at.row, at.col, horizontal, bsFits(bs.selected, at.row, at.col, horizontal));
  }
  bs.placeBoard.draw(html);

  const taken = bsTakenCells(drag ? drag.id : null);
  bs.placeBoard.buttons.forEach((button, i) => {
    const id = taken.get(i);
    button.classList.toggle("is-ship", !!id);
    button.setAttribute("aria-disabled", "false");
    button.setAttribute("aria-label", id ? i18n("game.bsCellShip", bsCoord(i), bsShipName(id))
      : bs.selected ? i18n("game.bsCellPlace", bsCoord(i)) : i18n("game.bsCellEmpty", bsCoord(i)));
  });
}

function bsSetHover(cell, byKeyboard) {
  if (bs.hover === cell && bs.hoverByKeyboard === byKeyboard) return;
  bs.hover = cell;
  bs.hoverByKeyboard = cell !== null && byKeyboard;
  if (bs.mode === "place") bsDrawPlace();
}

function bsUpdatePlaceUi(refreshStatus = true) {
  const count = bsPlacedCount();
  byId("bsTray").querySelectorAll("[data-ship]").forEach(row => {
    const id = row.dataset.ship;
    const placed = !!bs.placed[id];
    const horizontal = bsIsHorizontal(id);
    row.classList.toggle("is-selected", bs.selected === id);
    row.classList.toggle("is-placed", placed);
    row.setAttribute("aria-pressed", bs.selected === id ? "true" : "false");
    row.setAttribute("aria-label", i18n("game.bsTrayShip", bsShipName(id), bsShip(id).length,
      i18n(horizontal ? "game.horizontal" : "game.vertical"), i18n(placed ? "game.onBoard" : "game.notPlaced")));
    row.querySelector(".bs-tray-state").className = `bs-tray-state bi ${placed ? "bi-check-circle-fill" : horizontal ? "bi-arrow-left-right" : "bi-arrow-down-up"}`;
  });

  byId("bsRotate").disabled = !bs.selected || bs.sending;
  byId("bsRandom").disabled = bs.sending || bs.loadingRandom;
  const ready = byId("bsReady");
  ready.disabled = count < BS_SHIPS.length || bs.sending;
  ready.textContent = count < BS_SHIPS.length ? i18n("game.bsReadyMore", BS_SHIPS.length - count) : "Hazırım";
  if (refreshStatus && game) renderStatus();
}


function bsPointerPos(event) {
  const rect = bs.placeBoard.grid.getBoundingClientRect();
  const fx = ((event.clientX - rect.left) / rect.width) * BS_SIZE;
  const fy = ((event.clientY - rect.top) / rect.height) * BS_SIZE;
  return { fx, fy, inside: fx >= 0 && fx < BS_SIZE && fy >= 0 && fy < BS_SIZE };
}

function bsDragStart(event, id, from) {
  if (bs.sending || bs.drag || event.button > 0) return;
  const placed = bs.placed[id];
  const horizontal = bsIsHorizontal(id);
  const length = bsShip(id).length;
  const { fx, fy } = bsPointerPos(event);

  let offX = horizontal ? length / 2 : 0.5;
  let offY = horizontal ? 0.5 : length / 2;
  if (from === "board" && placed) {
    offX = Math.min(Math.max(fx - placed.col, 0), horizontal ? length : 1);
    offY = Math.min(Math.max(fy - placed.row, 0), horizontal ? 1 : length);
  }

  bs.drag = {
    id, from, horizontal, offX, offY,
    pointerId: event.pointerId, startX: event.clientX, startY: event.clientY, lastX: event.clientX, lastY: event.clientY,
    moved: false, target: null, wasSelected: bs.selected === id
  };
  try { event.currentTarget.setPointerCapture(event.pointerId); } catch (e) { }
  document.addEventListener("pointermove", bsDragMove);
  document.addEventListener("pointerup", bsDragUp);
  document.addEventListener("pointercancel", bsDragCancel);
}

function bsDragAim(drag) {
  const { fx, fy, inside } = bsPointerPos({ clientX: drag.lastX, clientY: drag.lastY });
  if (!inside) { drag.target = null; return; }
  const at = bsClamp(drag.id, drag.horizontal, Math.round(fy - drag.offY), Math.round(fx - drag.offX));
  drag.target = { ...at, valid: bsFits(drag.id, at.row, at.col, drag.horizontal) };
}

function bsDragMove(event) {
  const drag = bs.drag;
  if (!drag || event.pointerId !== drag.pointerId) return;
  drag.lastX = event.clientX;
  drag.lastY = event.clientY;
  if (!drag.moved) {
    if (Math.hypot(event.clientX - drag.startX, event.clientY - drag.startY) < 6) return;
    drag.moved = true;
  }
  bsDragAim(drag);
  bsDrawPlace();
}

function bsDragUp(event) {
  if (bs.drag && event.pointerId === bs.drag.pointerId) bsDragEnd(false);
}

function bsDragCancel(event) {
  if (bs.drag && event.pointerId === bs.drag.pointerId) bsDragEnd(true);
}

function bsDragEnd(cancelled) {
  const drag = bs.drag;
  if (!drag) return;
  bs.drag = null;
  document.removeEventListener("pointermove", bsDragMove);
  document.removeEventListener("pointerup", bsDragUp);
  document.removeEventListener("pointercancel", bsDragCancel);

  bs.suppressClick = true;
  setTimeout(() => { bs.suppressClick = false; }, 0);

  if (!drag.moved) {
    if (!cancelled) {
      const onBoard = !!bs.placed[drag.id];
      bs.selected = drag.id;
      if ((drag.from === "board" && onBoard) || (drag.from === "tray" && !onBoard && drag.wasSelected)) bsRotate();
    }
  } else {
    if (!cancelled) {
      if (drag.target && drag.target.valid) {
        bs.placed[drag.id] = { row: drag.target.row, col: drag.target.col, horizontal: drag.horizontal };
        bs.selected = drag.id;
      } else if (!drag.target) {
        delete bs.placed[drag.id];
        bs.orient[drag.id] = drag.horizontal;
        bs.selected = drag.id;
      }
    }
  }
  bsDrawPlace();
  bsUpdatePlaceUi();
}

function bsRotate() {
  const id = bs.selected;
  if (!id) return;

  const drag = bs.drag && bs.drag.moved && bs.drag.id === id ? bs.drag : null;
  if (drag) {
    drag.horizontal = !drag.horizontal;
    [drag.offX, drag.offY] = [drag.offY, drag.offX];
    bsDragAim(drag);
    bsDrawPlace();
    return;
  }

  const p = bs.placed[id];
  if (!p) {
    bs.orient[id] = !bsIsHorizontal(id);
  } else {
    const horizontal = !p.horizontal;
    let turned = false;
    for (let k = 0; k < bsShip(id).length && !turned; k++) {
      const pivotRow = p.horizontal ? p.row : p.row + k;
      const pivotCol = p.horizontal ? p.col + k : p.col;
      const row = horizontal ? pivotRow : pivotRow - k;
      const col = horizontal ? pivotCol - k : pivotCol;
      if (bsFits(id, row, col, horizontal)) {
        bs.placed[id] = { row, col, horizontal };
        turned = true;
      }
    }
    if (!turned) toast(i18n("game.bsNoRoom"));
  }
  bsDrawPlace();
  bsUpdatePlaceUi();
}

function bsPlaceClick(event) {
  if (bs.suppressClick || bs.sending) return;
  const button = event.target.closest(".bs-cell");
  if (!button) return;

  const cell = Number(button.dataset.cell);
  const onShip = bsOccupant(cell);
  if (onShip) {
    if (event.detail === 0) {
      bs.selected = onShip;
      bsDrawPlace();
      bsUpdatePlaceUi();
    }
    return;
  }

  const id = bs.selected;
  if (!id) return;
  const horizontal = bsIsHorizontal(id);
  const at = bsClamp(id, horizontal, Math.floor(cell / BS_SIZE), cell % BS_SIZE);
  if (!bsFits(id, at.row, at.col, horizontal)) {
    toast(i18n("game.bsOverlap"));
    return;
  }
  bs.placed[id] = { row: at.row, col: at.col, horizontal };
  bs.selected = BS_SHIPS.find(ship => !bs.placed[ship.id])?.id || id;
  bsDrawPlace();
  bsUpdatePlaceUi();
}

function bsKeyDown(event) {
  if (bs.mode !== "place" || event.ctrlKey || event.altKey || event.metaKey) return;
  if (document.querySelector(".confirm-overlay, .modal.show, .offcanvas.show")) return;
  const target = event.target;
  if (target && (target.isContentEditable || ["INPUT", "TEXTAREA", "SELECT"].includes(target.tagName))) return;

  if (event.key === "Escape" && bs.drag) {
    bsDragEnd(true);
  } else if (event.key === "r" || event.key === "R") {
    if (bs.selected) { event.preventDefault(); bsRotate(); }
  } else if ((event.key === "Delete" || event.key === "Backspace") && bs.selected && bs.placed[bs.selected] && !bs.drag) {
    event.preventDefault();
    bs.orient[bs.selected] = bs.placed[bs.selected].horizontal;
    delete bs.placed[bs.selected];
    bsDrawPlace();
    bsUpdatePlaceUi();
  }
}


async function bsRandomFleet() {
  if (bs.sending || bs.loadingRandom) return;
  bs.loadingRandom = true;
  bsUpdatePlaceUi(false);
  try {
    const fleet = await apiFetch("/api/games/random-fleet");
    bs.placed = {};
    (fleet.ships || []).forEach(ship => { bs.placed[ship.ship] = { row: ship.row, col: ship.col, horizontal: !!ship.horizontal }; });
    bs.selected = null;
  } catch (err) {
    toast(err.message || "Rastgele dizilim alınamadı.");
  } finally {
    bs.loadingRandom = false;
    if (bs.mode === "place") {
      bsDrawPlace();
      bsUpdatePlaceUi();
    }
  }
}

async function bsSendFleet() {
  if (bs.sending || bsPlacedCount() < BS_SHIPS.length) return;
  bs.sending = true;
  bsUpdatePlaceUi(false);
  try {
    const ships = BS_SHIPS.map(ship => ({ ship: ship.id, ...bs.placed[ship.id] }));
    applyGame(await apiFetch(`/api/games/${gameId}/place`, { method: "POST", body: { ships } }));
  } catch (err) {
    toast(err.message || "Filo yerleştirilemedi.");
    await loadGame();
  } finally {
    bs.sending = false;
    if (game) renderBattleship();
  }
}


async function bsFire(cell) {
  if (bs.firing) return;
  bs.firing = true;
  renderBattleship();
  try {
    applyGame(await apiFetch(`/api/games/${gameId}/shoot`, { method: "POST", body: { cell } }));
  } catch (err) {
    toast(err.message || "Atış yapılamadı.");
    await loadGame();
  } finally {
    bs.firing = false;
    if (game) renderBattleship();
  }
}

function bsSunkShipName(shot) {
  const list = (shot.byMe ? game.opponentShips : game.myShips) || [];
  const ship = list.find(s => bsCellsOf(s.ship, s.row, s.col, s.horizontal).includes(shot.cell));
  return ship ? bsShipName(ship.ship) : "";
}

function bsBoardLayer(ships, shots) {
  const sunkCells = new Set();
  ships.forEach(s => { if (s.sunk) bsCellsOf(s.ship, s.row, s.col, s.horizontal).forEach(cell => sunkCells.add(cell)); });

  let html = ships.map(s => bsShipGroup(s, !!s.sunk)).join("");
  for (let i = 0; i < BS_CELLS; i++) {
    const shot = shots?.[i];
    if (shot === "o") html += bsMissMark(i);
    else if (shot === "x" && !sunkCells.has(i)) html += bsHitMark(i);
  }
  return { html, sunkCells };
}

function bsDrawEnemy() {
  const { html, sunkCells } = bsBoardLayer(game.opponentShips || [], game.myShots);
  bs.enemyBoard.draw(html);

  const canFire = game.status === "Active" && game.phase === "Battle" && game.isMyTurn && !bs.firing;
  bs.enemyBoard.mount.classList.toggle("is-busy", bs.firing);
  bs.enemyBoard.buttons.forEach((button, i) => {
    const shot = game.myShots?.[i];
    const shootable = canFire && shot !== "o" && shot !== "x";
    const name = bsCoord(i);
    button.classList.toggle("is-fire", shootable);
    button.setAttribute("aria-disabled", shootable ? "false" : "true");
    button.setAttribute("aria-label",
      shot === "o" ? i18n("game.bsCellMiss", name)
      : shot === "x" ? i18n(sunkCells.has(i) ? "game.bsCellSunk" : "game.bsCellHit", name)
      : i18n(shootable ? "game.bsCellOpen" : "game.bsCellIdle", name));
  });
}

function bsDrawMine() {
  bs.mineBoard.draw(bsBoardLayer(game.myShips || [], game.opponentShots).html);
}

function bsDrawShot(battle) {
  const shot = battle ? game.lastShot : null;
  const box = byId("bsShot");

  if (!shot) {
    box.innerHTML = "";
    bs.shownShot = null;
    bs.enemyBoard.setLast(null);
    bs.mineBoard.setLast(null);
    return;
  }

  const key = `${shot.byMe ? "me" : "them"}:${shot.cell}`;
  const isNew = bs.shownShot !== undefined && key !== bs.shownShot;
  if (key !== bs.shownShot) {
    const tone = shot.result === "Sunk" ? "is-sunk" : shot.result === "Hit" ? "is-hit" : "is-miss";
    const word = shot.result === "Sunk" ? i18n("game.bsSunk", bsSunkShipName(shot))
      : shot.result === "Hit" ? i18n("game.bsHit") : i18n("game.bsMiss");
    const text = shot.byMe
      ? i18n("game.bsMyShot", bsCoord(shot.cell), word)
      : i18n("game.bsTheirShot", game.opponent?.name || "", bsCoord(shot.cell), word);
    box.innerHTML = `<span class="bs-shot-pill ${tone} ${isNew ? "is-new" : ""}" data-no-i18n>${escapeHtml(text)}</span>`;
    bs.shownShot = key;
  }
  bs.enemyBoard.setLast(shot.byMe ? shot.cell : null, isNew);
  bs.mineBoard.setLast(shot.byMe ? null : shot.cell, isNew);
}

function bsUpdateTabs() {
  byId("bsBoards").dataset.tab = bs.tab;
  byId("bsTabs").querySelectorAll("[data-board-tab]").forEach(tab => {
    tab.setAttribute("aria-pressed", tab.dataset.boardTab === bs.tab ? "true" : "false");
  });
}

function bsDrawBattle(mode) {
  const battle = mode === "battle";
  if (battle) bsDrawEnemy();
  bsDrawMine();
  bsDrawShot(battle);

  byId("bsCounters").innerHTML = battle
    ? `<span class="xo-chip" data-no-i18n>${escapeHtml(i18n("game.bsEnemyLeft", game.opponentShipsLeft ?? BS_SHIPS.length, BS_SHIPS.length))}</span>
       <span class="xo-chip" data-no-i18n>${escapeHtml(i18n("game.bsMyLeft", game.myShipsLeft ?? BS_SHIPS.length, BS_SHIPS.length))}</span>`
    : "";
  bsUpdateTabs();
}


function bsMode() {
  if (game.status === "Active") {
    if (game.phase === "Placing") return game.iAmReady ? "wait" : "place";
    return "battle";
  }
  return game.status === "Finished" ? "battle" : null;
}

function renderBattleship() {
  const mode = bsMode();
  byId("bsArea").classList.toggle("d-none", !mode);
  if (!mode) { bs.mode = null; return; }

  bsBuild();
  const before = bs.mode;
  bs.mode = mode;
  byId("bsPlace").classList.toggle("d-none", mode !== "place");
  byId("bsBattle").classList.toggle("d-none", mode === "place");
  byId("bsBattle").classList.toggle("is-wait", mode === "wait");

  if (mode === "place") {
    if (before !== "place") {
      if (!bs.selected) bs.selected = BS_SHIPS.find(ship => !bs.placed[ship.id])?.id || null;
      bsDrawPlace();
    }
    bsUpdatePlaceUi(false);
  } else {
    bsDrawBattle(mode);
  }
}

function bsStatusInfo() {
  const other = game.opponent?.name || "";

  if (game.phase === "Placing") {
    if (game.iAmReady) return { text: i18n("game.bsWaitingFleet", other) };
    const placed = bsPlacedCount();
    return {
      text: game.opponentReady ? i18n("game.bsOpponentReady", other) : "Filonu diz",
      sub: i18n("game.bsPlaced", placed, BS_SHIPS.length - placed)
    };
  }

  if (!game.isMyTurn) return { text: i18n("game.turnOf", other) };

  const shot = game.lastShot;
  if (shot && shot.byMe && shot.result === "Sunk") return { text: i18n("game.bsSunkAgain", bsSunkShipName(shot)) };
  if (shot && shot.byMe && shot.result === "Hit") return { text: i18n("game.bsHitAgain") };
  return { text: i18n("game.bsYourShot") };
}

function bsPlayerHtml(person) {
  const isMe = isSameId(person.id, getSession()?.userId);
  const active = game.status === "Active";
  const placing = game.phase === "Placing";
  const ready = active && placing && (isMe ? game.iAmReady : game.opponentReady);
  return playerHtml(person, "", {
    isTurn: active && (placing ? !ready : (isMe ? game.isMyTurn : !game.isMyTurn)),
    extra: ready ? `<span class="rps-picked" role="img" aria-label="Hazır" title="Hazır"><i class="bi bi-check-circle-fill"></i></span>` : ""
  });
}
