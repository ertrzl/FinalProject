// Shared by games.html and game.html: how a finished game looks from the viewer's side.
// The server decides who won; these helpers only turn its answer (winnerId, outcome) into words for the person looking.

function isSameId(a, b) {
  return !!a && !!b && String(a).toLowerCase() === String(b).toLowerCase();
}

// "win" | "loss" | "draw" for a finished game, null for any other.
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

// The line under a result when the game ended by a surrender ("Pes ettin" / "Ali pes etti"), otherwise "".
function gameResignText(game) {
  if (game.outcome !== "Resigned") return "";
  return gameResult(game) === "win" ? i18n("game.opponentResigned", game.opponent?.name || "") : "Pes ettin";
}

// The kinds of game. The server names them "TicTacToe", "RockPaperScissors" and "Battleship"; a game with no kind is an X-O game.
const GAME_KINDS = ["TicTacToe", "RockPaperScissors", "Battleship"];

function gameKind(game) {
  return GAME_KINDS.includes(game?.kind) ? game.kind : "TicTacToe";
}

// The name of the game in the language of the viewer ("X-O", "Taş-Kağıt-Makas", "Amiral Battı" / "Tic-tac-toe", "Rock-paper-scissors", "Battleship").
function gameKindName(kind) {
  return i18n({ RockPaperScissors: "game.kindRockPaperScissors", Battleship: "game.kindBattleship" }[kind] || "game.kindTicTacToe");
}
