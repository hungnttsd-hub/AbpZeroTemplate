import { readFile, writeFile } from "node:fs/promises";
import { createRequire } from "node:module";

// Reuse the web project's pinned esbuild; no additional npm dependency or download.
const require = createRequire(new URL("../../../src/WebHoanTien.Web/package.json", import.meta.url));
const { transform } = require("esbuild");
const output = new URL("../../../src/WebHoanTien.Web/wwwroot/tools/shopee-settlements/", import.meta.url);
const source = await readFile(new URL("bookmarklet.js", output), "utf8");
const result = await transform(source, {
  loader: "js", minify: true, charset: "utf8", target: "es2020", legalComments: "none"
});
const code = `void function(){${result.code}}();/*catsback-mobile-v2-end*/`;
const bookmarklet = `javascript:${encodeURIComponent(code)}`;
await writeFile(new URL("catsback-json-bookmarklet.txt", output), bookmarklet, "utf8");
console.log(`Mã dấu trang v2: ${bookmarklet.length} ký tự (đã rút gọn).`);
