import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "playwright";

const viewports = new Map([
  ["mobile", { width: 390, height: 844 }],
  ["tablet", { width: 768, height: 1024 }],
  ["desktop", { width: 1440, height: 900 }],
]);
const loadingSelector = '[data-lacuna-loading="true"]';

function validate(input) {
  const origin = new URL(process.env.E2E_BASE_URL).origin;
  const { mode, route, viewport, requiredSelector, readySelector, selectors, file, byteLimit } = input;
  if (!["layout", "screenshot"].includes(mode) || !viewports.has(viewport)) {
    throw new Error("Unsupported browser probe mode or viewport.");
  }
  if (typeof route !== "string" || route !== route.trim() || !/^\/(?:[\w-]+(?:\/[\w-]+)*)?$/.test(route)) {
    throw new Error("Expected a canonical Web-relative route.");
  }
  const url = new URL(route, origin);
  if (url.origin !== origin || !["http:", "https:"].includes(url.protocol)) {
    throw new Error("Browser probes require the configured HTTP(S) Web origin.");
  }
  const validSelector = (value) => typeof value === "string" && value.trim() &&
    value.length <= 256 && !/[\u0000-\u001f\u007f]/.test(value);
  if (!validSelector(requiredSelector) || (readySelector !== null && !validSelector(readySelector)) ||
      !Array.isArray(selectors) || selectors.length > 8 || !selectors.every(validSelector)) {
    throw new Error("Invalid CSS selector arguments.");
  }
  if (mode === "screenshot" &&
      (typeof file !== "string" || file !== file.trim() ||
       !/^screenshots\/[a-f0-9]{32}\/\d{2}-(mobile|tablet|desktop)\.png$/.test(file) ||
       !Number.isInteger(byteLimit) || byteLimit <= 0 || byteLimit > 1024 * 1024)) {
    throw new Error("Invalid screenshot destination or byte limit.");
  }
  return url;
}

async function inspect(page, selectors) {
  return page.evaluate((requestedSelectors) => {
    const root = document.documentElement;
    const round = (value) => Math.round(value * 100) / 100;
    const horizontalOverflow = Math.max(0, root.scrollWidth - root.clientWidth);
    const anomalies = horizontalOverflow > 1 ? [{ type: "document-horizontal-overflow", pixels: horizontalOverflow }] : [];
    const elements = requestedSelectors.map((selector) => {
      const matches = document.querySelectorAll(selector);
      const boxes = [];
      for (let index = 0; index < Math.min(matches.length, 4); index++) {
        const element = matches[index];
        const rect = element.getBoundingClientRect();
        const styles = getComputedStyle(element);
        if (rect.width > 0 && rect.height > 0 && (rect.left < -1 || rect.right > root.clientWidth + 1)) {
          anomalies.push({ type: "element-outside-horizontal-viewport", selector, index });
        }
        boxes.push({
          x: round(rect.x), y: round(rect.y), width: round(rect.width), height: round(rect.height),
          display: styles.display,
          position: styles.position,
          overflowX: styles.overflowX,
          overflowY: styles.overflowY,
          gridTemplateColumns: styles.gridTemplateColumns.slice(0, 256),
          flexDirection: styles.flexDirection,
          gap: styles.gap,
        });
      }
      return { selector, matchCount: matches.length, boxes };
    });
    return {
      url: location.href,
      title: document.title.slice(0, 256),
      viewport: { width: innerWidth, height: innerHeight },
      document: {
        clientWidth: root.clientWidth,
        scrollWidth: root.scrollWidth,
        height: root.scrollHeight,
        horizontalOverflow,
      },
      elements,
      anomalies,
    };
  }, selectors);
}

async function probe(input) {
  const url = validate(input);
  const browser = await chromium.launch({ headless: true, timeout: 10_000 });
  try {
    const context = await browser.newContext({
      viewport: viewports.get(input.viewport),
      deviceScaleFactor: 1,
      locale: "en-US",
      serviceWorkers: "block",
      acceptDownloads: false,
    });
    const page = await context.newPage();
    context.on("page", (other) => { void other.close().catch(() => {}); });
    await context.routeWebSocket(/.*/, (socket) => socket.close());
    await context.route(/.*/, async (route) => {
      const request = route.request();
      if (new URL(request.url()).origin !== url.origin ||
          !["GET", "HEAD"].includes(request.method()) ||
          (request.isNavigationRequest() && request.url() !== url.href)) {
        await route.abort();
        return;
      }
      try {
        // Intercept redirects before Chromium can follow them outside this origin.
        const response = await route.fetch({ maxRedirects: 0, timeout: 15_000 });
        try {
          if (response.status() >= 300 && response.status() < 400) {
            await route.abort();
          } else {
            await route.fulfill({ response });
          }
        } finally {
          await response.dispose();
        }
      } catch {
        await route.abort().catch(() => {});
      }
    });

    const response = await page.goto(url.href, { waitUntil: "domcontentloaded", timeout: 20_000 });
    if (!response?.ok()) {
      throw new Error("Web route returned HTTP " + (response?.status() ?? "unknown") + ".");
    }
    await page.waitForFunction(({ required, ready, loading }) => {
      const isVisible = (selector) => Array.from(document.querySelectorAll(selector)).some(
        (element) => element.checkVisibility({ visibilityProperty: true }),
      );
      return isVisible(required) && (!ready || isVisible(ready)) && !document.querySelector(loading);
    }, { required: input.requiredSelector, ready: input.readySelector, loading: loadingSelector }, { timeout: 20_000 });

    if (page.url() !== url.href) {
      throw new Error("The Web route changed before evidence collection.");
    }
    if (input.mode === "layout") {
      return { route: input.route, preset: input.viewport, ...await inspect(page, input.selectors) };
    }

    await page.evaluate(async () => {
      const visibleImages = Array.from(document.images).filter((image) => {
        const rect = image.getBoundingClientRect();
        return rect.bottom > 0 && rect.top < innerHeight && rect.right > 0 && rect.left < innerWidth;
      });
      await Promise.race([
        Promise.allSettled([document.fonts.ready, ...visibleImages.slice(0, 32).map((image) => image.decode())]),
        new Promise((resolve) => setTimeout(resolve, 1_000)),
      ]);
    });
    const png = await page.screenshot({ type: "png", animations: "disabled", caret: "hide", timeout: 5_000 });
    if (page.url() !== url.href || await page.locator(loadingSelector).count() > 0) {
      throw new Error("The page changed or resumed loading during capture.");
    }
    if (png.length > input.byteLimit) {
      throw new Error("PNG exceeds the remaining screenshot byte budget.");
    }
    const output = fileURLToPath(new URL("../qwen-reviews/" + input.file, import.meta.url));
    await mkdir(path.dirname(output), { recursive: true });
    await writeFile(output, png, { flag: "wx" });
    return { route: input.route, preset: input.viewport, viewport: viewports.get(input.viewport), file: input.file, bytes: png.length };
  } finally {
    await browser.close();
  }
}

try {
  if (process.argv.length !== 3) throw new Error("Expected one browser probe argument.");
  const result = await probe(JSON.parse(process.argv[2]));
  console.log(JSON.stringify(result));
} catch (error) {
  console.error(String(error.message).split("\n", 1)[0].slice(0, 600));
  process.exitCode = 1;
}
