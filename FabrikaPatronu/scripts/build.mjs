// Tek dosya paket: src/main.js → esbuild IIFE; CSS ve JS index.html'e gömülür.
// dist/index.html: tarayıcı ve Artifact sürümü (Google Fonts bağlantısı). dist/app.html: iOS sürümü (yazı tipi gömülü, ağ isteği yok).
import { build } from "esbuild";
import { readFileSync, writeFileSync, mkdirSync, existsSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const r = await build({ entryPoints: [join(root, "src/main.js")], bundle: true, format: "iife", minify: true, write: false, target: ["safari15"], legalComments: "none" });
const js = r.outputFiles[0].text.replace(/<\/script/g, "<\\/script");
const css = readFileSync(join(root, "src/ui/style.css"), "utf8");
const tpl = readFileSync(join(root, "src/ui/index.html"), "utf8");
mkdirSync(join(root, "dist"), { recursive: true });
const page = fonts => tpl.replace("<!--FONTS-->", fonts).replace("/*CSS*/", () => css).replace("/*JS*/", () => js);
const web = page('<link rel="preconnect" href="https://fonts.googleapis.com"><link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Nunito:wght@600;700;800;900&display=swap">');
writeFileSync(join(root, "dist/index.html"), web);
// Artifact sürümü: <head>/<body> iskeleti olmadan gövde (barındırıcı kendi iskeletini ekler)
const art = web.replace(/^[\s\S]*?<head>/, "").replace("</head>", "").replace(/<body>|<\/body>|<\/html>/g, "");
writeFileSync(join(root, "dist/play.html"), art);
mkdirSync(join(root, "web"), { recursive: true }); writeFileSync(join(root, "web/play.html"), art);
const fontDir = join(root, "node_modules/@fontsource/nunito");
let inline = "";
if (existsSync(fontDir)) for (const w of [600, 700, 800, 900]) for (const blk of readFileSync(join(fontDir, `${w}.css`), "utf8").split("@font-face").slice(1)) {
  const f = (blk.match(/files\/([^)]+?-(latin|latin-ext)-\d+-normal\.woff2)/) || [])[1]; if (!f) continue;
  inline += "@font-face" + blk.replace(/src:[^;]+;/, `src: url(data:font/woff2;base64,${readFileSync(join(fontDir, "files", f)).toString("base64")}) format('woff2');`).replace(/\/\*[^*]*\*\//g, "");
}
writeFileSync(join(root, "dist/app.html"), page(inline ? `<style>${inline}</style>` : ""));
console.log("dist/index.html", Math.round(web.length / 1024), "KB · dist/app.html", Math.round(readFileSync(join(root, "dist/app.html")).length / 1024), "KB");
