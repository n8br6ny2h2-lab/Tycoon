// www/ for the iOS app: the single-file game from ../dist/app.html (fonts embedded, no network requests)
// plus native-feel CSS (no zoom, no text selection outside inputs, no overscroll).
import { readFileSync, writeFileSync, mkdirSync, rmSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const out = join(root, "www");
rmSync(out, { recursive: true, force: true }); mkdirSync(out, { recursive: true });
let html = readFileSync(join(root, "..", "dist", "app.html"), "utf8");
if (/fonts\.googleapis|https?:\/\//.test(html.replace(/http:\/\/www\.w3\.org\/[^"]*/g, ""))) throw new Error("app.html must not reference network URLs");
html = html.replace("</head>", `<style>html,body{overscroll-behavior:none;-webkit-touch-callout:none}</style>\n</head>`);
writeFileSync(join(out, "index.html"), html);
console.log(`www/index.html ${Math.round(html.length / 1024)} KB`);
