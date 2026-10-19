import { Page } from "playwright";

export const PREFERENCES_STORAGE_KEY = "lacuna_user_preferences";

// Absolute origin of the app under test. Used explicitly (instead of relying
// on the runner's baseURL resolution) so specs work identically across
// Playwright versions.
export const APP_BASE_URL = process.env.E2E_BASE_URL || "http://localhost:5032/";

export function absoluteUrl(path: string): string {
  return new URL(path, APP_BASE_URL).toString();
}

// Blazor/ICU formatting uses NBSP / narrow NBSP as thousands separators.
export function normalizeSpaces(text: string): string {
  return text.replace(/[\u00A0\u202F]/g, " ").replace(/\s+/g, " ").trim();
}

export type Preferences = {
  Language: string;
  RegionalFormat: string;
  CurrencyCode: string;
};

// Navigates to an app route and waits until the Blazor WASM page is rendered.
export async function gotoApp(page: Page, path: string, readySelector: string) {
  const response = await page.goto(absoluteUrl(path), { waitUntil: "load" });
  await waitForAppReady(page, readySelector);
  return response;
}

// Blazor WASM renders asynchronously after the document "load" event, so waiting
// for a rendered selector (an event-driven DOM wait) is the readiness signal;
// waiting for "load" alone can return before any component is rendered.
export async function waitForAppReady(page: Page, readySelector: string) {
  await page.waitForSelector(readySelector, { timeout: 30_000, state: "attached" });
}

// Writes the given preferences into localStorage and reloads the page so the
// app re-initializes cultures from them (see LocalStorageUserPreferencesService).
// The reload is followed by a wait for the rendered selector: "load" fires
// before Blazor WASM has rendered anything.
export async function seedPreferences(page: Page, prefs: Preferences, readySelector: string) {
  await page.evaluate(
    (params: { key: string; json: string }) => localStorage.setItem(params.key, params.json),
    { key: PREFERENCES_STORAGE_KEY, json: JSON.stringify(prefs) },
  );
  await page.reload({ waitUntil: "load" });
  await waitForAppReady(page, readySelector);
}

// Removes persisted preferences so the next load re-detects from navigator.language.
export async function clearPreferences(page: Page, readySelector: string) {
  await page.evaluate(
    (key: string) => localStorage.removeItem(key),
    PREFERENCES_STORAGE_KEY,
  );
  await page.reload({ waitUntil: "load" });
  await waitForAppReady(page, readySelector);
}

export async function readPreferences(page: Page): Promise<Preferences | null> {
  const raw = await page.evaluate(
    (key: string) => localStorage.getItem(key),
    PREFERENCES_STORAGE_KEY,
  );
  return raw === null ? null : JSON.parse(raw) as Preferences;
}

export const DEFAULT_PREFERENCES: Preferences = {
  Language: "en",
  RegionalFormat: "en-US",
  CurrencyCode: "USD",
};

// Makes the persisted preferences deterministic. When the persisted value
// already matches, no reload is needed; either way the rendered selector is
// awaited so the caller never observes a half-rendered page.
export async function ensurePreferences(page: Page, prefs: Preferences, readySelector: string) {
  const current = await readPreferences(page);
  if (
    current !== null &&
    current.Language === prefs.Language &&
    current.RegionalFormat === prefs.RegionalFormat &&
    current.CurrencyCode === prefs.CurrencyCode
  ) {
    await waitForAppReady(page, readySelector);
    return;
  }
  await seedPreferences(page, prefs, readySelector);
}

// Counts <img> elements matching the selector that have actually finished
// decoding (complete + non-zero intrinsic size). A blocked/failed request
// leaves complete=true with naturalWidth=0, so this distinguishes real loads.
export async function loadedImageCount(page: Page, selector: string): Promise<number> {
  return await page.evaluate((sel: string) => {
    const images = [...document.querySelectorAll(sel)];
    return images.filter((img) => img.complete && img.naturalWidth > 0 && img.naturalHeight > 0).length;
  }, selector);
}

// Pollable form for expect(...) so the count is re-read until images finish
// decoding (or the assertion times out when loading is broken).
export function pollLoadedImages(page: Page, selector: string): () => Promise<number> {
  return () => loadedImageCount(page, selector);
}
export async function maxCardsPerRow(page: Page): Promise<number> {
  const count = await page.evaluate(() => {
    const tops = [...document.querySelectorAll(".listing-card")]
      .map((card) => Math.round(card.getBoundingClientRect().top));
    const rows: number[] = [];
    for (const top of tops) {
      if (!rows.some((row) => Math.abs(row - top) < 10)) rows.push(top);
    }
    return rows.reduce((max, row) => Math.max(max, tops.filter((top) => Math.abs(top - row) < 10).length), 0);
  });
  return count;
}

// True when the bottom navigation is display:none (hidden from 1024px up).
export async function bottomNavHidden(page: Page): Promise<boolean> {
  return (await page.evaluate(() => {
    const nav = document.querySelector("nav.bottom-nav");
    return nav === null ? true : getComputedStyle(nav).display === "none";
  }));
}

// True when every element matching the selector carries the HTML disabled attribute.
export async function allDisabled(page: Page, selector: string): Promise<boolean> {
  const handles = await page.locator(selector).all();
  if (handles.length === 0) return false;
  for (const handle of handles) {
    if ((await handle.getAttribute("disabled")) === null) return false;
  }
  return true;
}

// Blazor WASM exposes its client-side router on the page window; NavigationManager.NavigateTo
// compiles to a call to window.Blazor.navigateTo(url, forceLoad, replaceHistoryEntry).
interface BlazorClientRouter {
  navigateTo(path: string, forceLoad: boolean, replaceHistoryEntry: boolean): void;
}

// Performs client-side navigation from inside the already-running Blazor app. Unlike page.goto,
// a reload, or a window.location assignment, this keeps the existing JS window and issues no
// new main-frame document request.
export async function spaNavigate(page: Page, path: string) {
  await page.evaluate(
    (args: { route: string }) => {
      const blazor = (window as unknown as { Blazor: BlazorClientRouter }).Blazor;
      blazor.navigateTo(args.route, false, false);
    },
    { route: path },
  );
}

// Starts recording main-frame document requests (a new HTML document fetched for the top frame)
// from the moment of the call onwards. Returns the array callers assert against.
export function trackMainFrameDocumentRequests(page: Page): string[] {
  const urls: string[] = [];
  page.on("request", (request) => {
    if (request.resourceType() === "document" && request.isNavigationRequest()) {
      urls.push(request.url());
    }
  });
  return urls;
}

// A value stored on the page window survives Blazor client-side routing but is lost by any real
// document navigation, which creates a new JS window.
export async function setWindowMarker(page: Page, key: string, value: string) {
  await page.evaluate(
    (args: { key: string; value: string }) => {
      (window as unknown as Record<string, unknown>)[args.key] = args.value;
    },
    { key, value },
  );
}

export async function readWindowMarker(page: Page, key: string): Promise<string | null> {
  return await page.evaluate(
    (args: { key: string }) => {
      const stored = (window as unknown as Record<string, unknown>)[args.key];
      return stored === undefined ? null : String(stored);
    },
    { key },
  );
}

