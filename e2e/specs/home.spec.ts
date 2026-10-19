import { test, expect } from "@playwright/test";
import { gotoApp, ensurePreferences, DEFAULT_PREFERENCES, normalizeSpaces, allDisabled } from "../helpers/app";

test.describe("Home screen", () => {
  test("search bar click navigates to listings", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".home-page");

    await page.click(".search-bar");
    await expect(page).toHaveURL(/\/listings$/);
  });

  test("category chips: one active, rest disabled", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".home-page");

    expect(await page.locator(".categories button").count()).toBe(6);
    expect(await page.locator(".category-chip.active").count()).toBe(1);
    expect(normalizeSpaces(await page.locator(".category-chip.active").innerText())).toBe("All Vehicles");
    expect(await allDisabled(page, ".category-chip:not(.active)")).toBe(true);
    expect(await allDisabled(page, "button.filter-button")).toBe(true);
  });

  test("feed shows 8 cards with 4 Just Added badges", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".home-page");

    expect(await page.locator(".listing-card").count()).toBe(8);
    expect(await page.locator(".badge-new").count()).toBe(4);
    expect(normalizeSpaces(await page.locator(".badge-new").first().innerText()).toLowerCase()).toBe("just added");
  });

  test("first card click opens matching details", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".home-page");

    await page.locator(".listing-card").first().click();
    await expect(page).toHaveURL(/\/listings\/1$/);
    expect(normalizeSpaces(await page.locator(".model-heading").innerText())).toBe("Model 3 Long Range");
  });
});
