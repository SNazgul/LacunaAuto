import { test, expect } from "@playwright/test";
import { gotoApp, ensurePreferences, DEFAULT_PREFERENCES, normalizeSpaces } from "../helpers/app";

const UKRAINE_PREFS = { Language: "uk", RegionalFormat: "uk-UA", CurrencyCode: "UAH" };

// Guards the synchronization in seedPreferences/ensurePreferences: after a
// preference-triggered reload the helper must not return before Blazor WASM has
// rendered. No sleeps or manual retries are used; the wait is the rendered
// selector itself (see also negative/preference-readiness.spec.ts, which fails
// when the wait is removed).
test.describe("Preference reload readiness", () => {
  test("reload path waits for the rendered app", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");

    // Force the reload branch: persisted preferences differ from the requested ones.
    await ensurePreferences(page, UKRAINE_PREFS, ".home-page");
    expect(await page.locator(".listing-card").count()).toBe(8);
    expect(normalizeSpaces(await page.locator(".search-placeholder").innerText())).toBe("Пошук");

    // And back to the defaults, again through the reload branch.
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".home-page");
    expect(await page.locator(".listing-card").count()).toBe(8);
    expect(normalizeSpaces(await page.locator(".search-placeholder").innerText())).toBe("Search");
  });

  test("slow startup still waits for the rendered app", async ({ page }) => {
    // Delay every response so the WASM bootstrap starts late; the readiness
    // wait must still hold before the assertions run.
    await page.route("**/*", async (request) => {
      await new Promise((timer) => setTimeout(timer, 400));
      return request.continue();
    });

    await gotoApp(page, "/", ".home-page");
    await ensurePreferences(page, UKRAINE_PREFS, ".home-page");

    expect(await page.locator(".listing-card").count()).toBe(8);
    expect(normalizeSpaces(await page.locator(".badge-new").first().innerText()).toLowerCase()).toBe("щойно додано");
  });
});
