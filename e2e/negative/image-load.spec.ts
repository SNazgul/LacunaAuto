import { test, expect } from "@playwright/test";
import { gotoApp, ensurePreferences, DEFAULT_PREFERENCES, pollLoadedImages, loadedImageCount } from "../helpers/app";

// EXPECTED RESULT: this test FAILS. It blocks every vehicle image request and
// then applies exactly the assertion specs/listings.spec.ts uses. A green run
// here would mean the suite does not really verify image loading.
test.describe("Negative check: broken image loading must be detected", () => {
  test("blocked vehicle images fail the decoded-image assertion", async ({ page }) => {
    await page.route("**/images/vehicles/**", (request) => request.abort());

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    // The elements are still present, so an element-count check alone would pass.
    expect(await page.locator(".listings-content img").count()).toBe(8);
    const srcs = await page.locator(".listings-content img").first().getAttribute("src");
    console.log(`NEGATIVE probe: first img src = ${srcs}, decoded = ${await loadedImageCount(page, ".listings-content img")}`);

    // Decoding is what the real suite asserts, and it must fail here.
    await expect.poll(pollLoadedImages(page, ".listings-content img"), "decoded images with loading blocked").toBe(8);
  });
});
