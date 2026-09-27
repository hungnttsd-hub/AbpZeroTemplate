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
await writeFile(new URL("mobile.js", output), result.code, "utf8");

// Only this small loader goes into the bookmark URL. The setup page substitutes
// its own HTTPS asset URL, so no production domain is hard-coded into the build.
const loader = await readFile(new URL("loader.js", import.meta.url), "utf8");
const template = await transform(`void (${loader})("__CATSBACK_MOBILE_URL__");`, {
  loader: "js", minify: true, charset: "utf8", target: "es2020", legalComments: "none"
});
await writeFile(new URL("loader-template.txt", output), "/*catsback-loader-v3*/" + template.code.trim(), "utf8");
console.log(`Đã tạo mobile.js và mẫu mã tải tool (${template.code.trim().length} ký tự trước mã hóa URL).`);
