import assert from "node:assert/strict";
import { execFile } from "node:child_process";
import { readFile, rmdir, unlink } from "node:fs/promises";
import { createServer } from "node:http";
import { randomUUID } from "node:crypto";
import { test } from "node:test";
import { fileURLToPath } from "node:url";
import { promisify } from "node:util";

const execute = promisify(execFile);
const probePath = fileURLToPath(new URL("./qwen-browser-probe.mjs", import.meta.url));

async function listen(t, respond) {
  const server = createServer(respond);
  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", resolve);
  });
  t.after(() => {
    server.closeAllConnections();
    return new Promise((resolve) => server.close(resolve));
  });
  return "http://127.0.0.1:" + server.address().port;
}

async function run(origin, options) {
  const input = {
    mode: "layout", route: "/", viewport: "mobile", requiredSelector: ".ready",
    readySelector: null, selectors: [".box"], file: null, byteLimit: 1024 * 1024,
    ...options,
  };
  const { stdout } = await execute(process.execPath, [probePath, JSON.stringify(input)], {
    env: { ...process.env, E2E_BASE_URL: origin }, timeout: 45_000,
  });
  return JSON.parse(stdout);
}

test("measures the ready page and captures viewport-only PNGs at each preset", async (t) => {
  const origin = await listen(t, (request, response) => {
    response.writeHead(200, { "Content-Type": "text/html" });
    response.end([
      '<title>Probe fixture</title><style>body{margin:0}.box{display:grid;gap:8px;width:200px;height:2000px}</style>',
      '<main class="ready" data-lacuna-loading="true"><div class="box">Sample</div></main>',
      '<script>setTimeout(() => { document.querySelector("main").removeAttribute("data-lacuna-loading");',
      'document.querySelector(".box").style.width = "240px"; }, 150)</script>',
    ].join(""));
  });
  const directory = "screenshots/" + randomUUID().replaceAll("-", "") + "/";
  const directoryUrl = new URL("../qwen-reviews/" + directory, import.meta.url);
  t.after(() => rmdir(directoryUrl).catch((error) => {
    if (error.code !== "ENOENT") throw error;
  }));

  for (const [viewport, width, height] of [["mobile", 390, 844], ["tablet", 768, 1024], ["desktop", 1440, 900]]) {
    const layout = await run(origin, { viewport });
    assert.deepEqual(layout.viewport, { width, height });
    assert.equal(layout.document.horizontalOverflow, 0);
    assert.equal(layout.document.height, 2000);
    assert.equal(layout.elements[0].boxes[0].display, "grid");
    assert.equal(layout.elements[0].boxes[0].width, 240);

    const file = directory + "01-" + viewport + ".png";
    const artifact = new URL("../qwen-reviews/" + file, import.meta.url);
    try {
      const capture = await run(origin, { mode: "screenshot", viewport, file });
      assert.equal(capture.file, file);
      const png = await readFile(artifact);
      assert.equal(capture.bytes, png.length);
      assert.equal(png.subarray(0, 8).toString("hex"), "89504e470d0a1a0a");
      assert.deepEqual([png.readUInt32BE(16), png.readUInt32BE(20)], [width, height]);
    } finally {
      await unlink(artifact).catch((error) => {
        if (error.code !== "ENOENT") throw error;
      });
    }
  }
  const oversized = directory + "02-mobile.png";
  await assert.rejects(run(origin, { mode: "screenshot", file: oversized, byteLimit: 1 }), /PNG exceeds/);
  await assert.rejects(readFile(new URL("../qwen-reviews/" + oversized, import.meta.url)), { code: "ENOENT" });
});

test("rejects invalid routes, viewport, selectors and screenshot destinations", async () => {
  const origin = "http://127.0.0.1:1";
  for (const route of ["http://127.0.0.1:1/", "//127.0.0.1:1/", "/\\example", "/%2Fexample", "/../example"]) {
    await assert.rejects(run(origin, { route }), /Expected a canonical Web-relative route/);
  }
  await assert.rejects(run(origin, { viewport: "wide" }), /Unsupported browser probe/);
  await assert.rejects(run(origin, { selectors: Array(9).fill(".box") }), /Invalid CSS selector arguments/);
  await assert.rejects(run(origin, { mode: "screenshot", file: "../escape.png" }), /Invalid screenshot destination/);
});

test("blocks external requests, redirected resources and browser writes", async (t) => {
  let externalHits = 0;
  let writes = 0;
  const external = await listen(t, (request, response) => {
    externalHits++;
    response.end();
  });
  const origin = await listen(t, (request, response) => {
    if (request.method === "POST") writes++;
    if (request.url === "/redirect") {
      response.writeHead(302, { Location: external + "/landing" }).end();
      return;
    }
    response.writeHead(200, { "Content-Type": "text/html" });
    response.end(
      '<div class="box">Fixture</div><script>Promise.allSettled([' +
      'fetch("' + external + '/external"),fetch("/redirect"),fetch("/write",{method:"POST"})' +
      ']).then(() => document.querySelector(".box").classList.add("ready"));</script>',
    );
  });

  const layout = await run(origin);
  assert.equal(layout.elements[0].matchCount, 1);
  await assert.rejects(run(origin, { route: "/redirect" }), /ERR_FAILED|ERR_ABORTED/);
  assert.equal(externalHits, 0);
  assert.equal(writes, 0);
});
