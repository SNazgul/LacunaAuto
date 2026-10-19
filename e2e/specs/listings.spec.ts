import { test, expect } from "@playwright/test";
import { gotoApp, ensurePreferences, DEFAULT_PREFERENCES, normalizeSpaces, allDisabled, pollLoadedImages } from "../helpers/app";

test.describe("Listings screen", () => {
  test("header shows result count and sort label", async ({ page }) => {
    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    expect(normalizeSpaces(await page.locator(".result-count").innerText())).toBe("8 results");
    expect(normalizeSpaces(await page.locator(".sort-label").innerText())).toBe("Newest");
  });

  test("back button returns home", async ({ page }) => {
    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    await page.locator("button.back-button").click();
    await expect(page).toHaveURL(/^\S+\/$/);
  });

  test("all 8 seed images load and decode", async ({ page }) => {
    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    const images = await page.locator(".listings-content img").all();
    expect(images.length).toBe(8);
    for (const image of images) {
      const src = await image.getAttribute("src");
      expect(src).toContain("/images/vehicles/");
    }

    // Poll until every <img> has finished decoding (complete + intrinsic size).
    // A blocked/failed request leaves complete=true with naturalWidth=0, so this
    // assertion fails when image loading is broken (see negative/image-load.spec.ts).
    await expect.poll(pollLoadedImages(page, ".listings-content img"), "decoded .listings-content images").toBe(8);
  });

  test("filter button stays disabled", async ({ page }) => {
    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    expect(await allDisabled(page, "button.filter-button")).toBe(true);
  });
});
