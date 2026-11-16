// Builds www/ for the iOS app from the single-file web game (../web/play.html):
// wraps it in a full HTML document, swaps Google Fonts for bundled font files so the app works offline,
// and adds native-feel CSS (safe areas, no zoom, no text selection).
import { readFileSync, writeFileSync, mkdirSync, copyFileSync, rmSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const out = join(root, "www");
const FONTS = { "chakra-petch": [600, 700], "ibm-plex-sans": [400, 500, 600], "saira-condensed": [500, 600, 700] };

rmSync(out, { recursive: true, force: true });
mkdirSync(join(out, "fonts"), { recursive: true });

let css = "";
for (const [pkg, weights] of Object.entries(FONTS)) {
  const dir = join(root, "node_modules", "@fontsource", pkg);
  for (const w of weights) {
    const blocks = readFileSync(join(dir, `${w}.css`), "utf8").split("@font-face").slice(1);
    for (const b of blocks) {
      const file = (b.match(/files\/([^)]+?-(latin|latin-ext)-\d+-normal\.woff2)/) || [])[1];
      if (!file) continue; // keep only Latin + Latin Extended (Turkish letters and ₺)
      copyFileSync(join(dir, "files", file), join(out, "fonts", file));
      css += "@font-face" + b.replace(/src:[^;]+;/, `src: url(./${file}) format('woff2');`).replace(/\/\*[^*]*\*\//g, "").trimEnd() + "\n";
    }
  }
}
writeFileSync(join(out, "fonts", "fonts.css"), css);

let game = readFileSync(join(root, "..", "web", "play.html"), "utf8");
const before = game.length;
game = game.replace(/<link rel="preconnect"[^>]*>\n?/, "").replace(/<link rel="stylesheet" href="https:\/\/fonts\.googleapis\.com[^>]*>/, '<link rel="stylesheet" href="fonts/fonts.css">');
if (game.length === before || game.includes("fonts.googleapis.com")) throw new Error("Could not replace the Google Fonts link in play.html");

const html = `<!doctype html>
<html lang="tr">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no, viewport-fit=cover">
<meta name="format-detection" content="telephone=no">
<style>
  html { background: #101418; }
  body { margin: 0; box-sizing: border-box; padding-top: env(safe-area-inset-top, 0px); -webkit-user-select: none; user-select: none; -webkit-touch-callout: none; overscroll-behavior: none; }
  input, select { -webkit-user-select: text; user-select: text; }
</style>
${game}
</body>
</html>
`;
writeFileSync(join(out, "index.html"), html);
console.log(`www/index.html written (${Math.round(html.length / 1024)} KB), ${css.split("@font-face").length - 1} font faces bundled`);
