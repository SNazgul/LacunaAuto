import { test, expect } from "@playwright/test";
import { gotoApp, readPreferences, PREFERENCES_STORAGE_KEY } from "../helpers/app";

// EXPECTED RESULT: this test FAILS. It reproduces the race the review describes:
// seed preferences, reload with waitUntil "load" only (no readiness wait), and
// assert the rendered feed immediately. Blazor WASM has not rendered yet, so the
// card count observed is 0. The positive counterpart is specs/preference-readiness.spec.ts.
test.describe("Negative check: reload without readiness wait races Blazor rendering", () => {
  test("bare reload returns before Blazor renders the feed", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");

    const current = await readPreferences(page);
    if (current === null || current.Language !== "uk") {
      await page.evaluate(
        (params: { key: string; json: string }) => localStorage.setItem(params.key, params.json),
        {
          key: PREFERENCES_STORAGE_KEY,
          json: JSON.stringify({ Language: "uk", RegionalFormat: "uk-UA", CurrencyCode: "UAH" }),
        },
      );
      await page.reload({ waitUntil: "load" });
    }

    const cards = await page.locator(".listing-card").count();
    console.log(`NEGATIVE probe: cards right after bare reload = ${cards}`);
    expect(cards, "cards after bare reload (no readiness wait)").toBe(8);
  });
});
