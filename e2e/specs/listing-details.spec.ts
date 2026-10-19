import { test, expect } from "@playwright/test";
import { gotoApp, ensurePreferences, DEFAULT_PREFERENCES, normalizeSpaces, allDisabled } from "../helpers/app";

test.describe("Listing details screen", () => {
  test("valid id renders full details", async ({ page }) => {
    await gotoApp(page, "/listings/1", ".details-shell");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".details-shell");

    expect(normalizeSpaces(await page.locator(".make-label").innerText())).toBe("Tesla");
    expect(normalizeSpaces(await page.locator(".model-heading").innerText())).toBe("Model 3 Long Range");
    expect(normalizeSpaces(await page.locator(".details-content .price").innerText())).toBe("45,000 USD");
    expect(normalizeSpaces(await page.locator(".location span").innerText())).toBe("Kyiv, Ukraine");
  });

  test("quick facts render year, mileage, fuel, transmission", async ({ page }) => {
    await gotoApp(page, "/listings/1", ".details-shell");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".details-shell");

    expect(await page.locator(".quick-facts .fact-item").count()).toBe(4);
    const facts = await page.locator(".quick-facts .fact-item").all();
    const factTexts = [];
    for (const fact of facts) {
      factTexts.push(normalizeSpaces(await fact.innerText()));
    }
    expect(factTexts).toEqual([
      "Year 2023",
      "Mileage 12,000 km",
      "Fuel Electric",
      "Transmission Automatic",
    ]);
  });

  test("description section renders", async ({ page }) => {
    await gotoApp(page, "/listings/1", ".details-shell");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".details-shell");

    expect(normalizeSpaces(await page.locator(".description-section h2").innerText())).toBe("Description");
    expect(normalizeSpaces(await page.locator(".description-section p").innerText())).toContain("Tesla Model 3");
  });

  test("contact buttons stay disabled", async ({ page }) => {
    await gotoApp(page, "/listings/1", ".details-shell");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".details-shell");

    expect(await page.locator(".contact-buttons button").count()).toBe(2);
    expect(await allDisabled(page, ".contact-buttons button")).toBe(true);
  });

  test("back button returns to listings", async ({ page }) => {
    await gotoApp(page, "/listings/1", ".details-shell");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".details-shell");

    await page.locator("button.back-button").click();
    await expect(page).toHaveURL(/\/listings$/);
  });
});
