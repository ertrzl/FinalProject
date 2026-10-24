function isSameId(a, b) {
  return !!a && !!b && String(a).toLowerCase() === String(b).toLowerCase();
}

function gameResult(game) {
  if (game.status !== "Finished") return null;
  if (game.outcome === "Draw") return "draw";
  return isSameId(game.winnerId, getSession()?.userId) ? "win" : "loss";
}

const GAME_RESULT_LABEL = {
  win: { text: "Kazandın", css: "bg-success-subtle text-success-emphasis" },
  loss: { text: "Kaybettin", css: "bg-danger-subtle text-danger-emphasis" },
  draw: { text: "Berabere", css: "bg-secondary-subtle text-secondary-emphasis" }
};

function gameResultBadgeHtml(game) {
  const label = GAME_RESULT_LABEL[gameResult(game)];
  return label ? `<span class="badge rounded-pill ${label.css}">${label.text}</span>` : "";
}

function gameResignText(game) {
  if (game.outcome !== "Resigned") return "";
  return gameResult(game) === "win" ? i18n("game.opponentResigned", game.opponent?.name || "") : "Pes ettin";
}

const GAME_KINDS = ["TicTacToe", "RockPaperScissors", "Battleship"];

function gameKind(game) {
  return GAME_KINDS.includes(game?.kind) ? game.kind : "TicTacToe";
}

function gameKindName(kind) {
  return i18n({ RockPaperScissors: "game.kindRockPaperScissors", Battleship: "game.kindBattleship" }[kind] || "game.kindTicTacToe");
}
