import { test, expect } from "@playwright/test";
import { gotoApp, seedPreferences, clearPreferences, readPreferences, DEFAULT_PREFERENCES, normalizeSpaces, absoluteUrl } from "../helpers/app";

const UKRAINE_PREFS = { Language: "uk", RegionalFormat: "uk-UA", CurrencyCode: "UAH" };
const RUSSIA_PREFS = { Language: "ru", RegionalFormat: "ru-RU", CurrencyCode: "UAH" };
const MIXED_UK_FORMAT = { Language: "en", RegionalFormat: "uk-UA", CurrencyCode: "UAH" };
const MIXED_US_FORMAT = { Language: "ru", RegionalFormat: "en-US", CurrencyCode: "UAH" };

// The app has no preferences UI yet (see README "Not covered"), so preferences
// are seeded through the same localStorage key the app itself writes
// (LocalStorageUserPreferencesService). The browser locale is pinned to en-US
// by playwright.config.ts (use.locale), which makes first-run detection
// deterministic.
test.describe("Localization and persisted preferences", () => {
  test("first run persists detected preferences", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");

    const prefs = await readPreferences(page);
    expect(prefs === null).toBe(false);
    expect(prefs!.Language).toBe("en");
    expect(prefs.RegionalFormat).toBe("en-US");
    expect(prefs.CurrencyCode).toBe("USD");
  });

  test("preferences persist across a plain reload", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await seedPreferences(page, UKRAINE_PREFS, ".home-page");
    expect(normalizeSpaces(await page.locator(".search-placeholder").innerText())).toBe("Пошук");

    // No seeding on the second load: the persisted value must survive and still
    // drive the rendered strings.
    await page.reload({ waitUntil: "load" });
    await page.waitForSelector(".home-page", { timeout: 30_000 });
    const prefs = await readPreferences(page);
    expect(prefs!.Language).toBe("uk");
    expect(prefs.RegionalFormat).toBe("uk-UA");
    expect(prefs.CurrencyCode).toBe("UAH");
    expect(normalizeSpaces(await page.locator(".search-placeholder").innerText())).toBe("Пошук");
    expect(normalizeSpaces(await page.locator(".badge-new").first().innerText()).toLowerCase()).toBe("щойно додано");
  });

  test("uk language switches UI strings", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await seedPreferences(page, UKRAINE_PREFS, ".home-page");

    expect(normalizeSpaces(await page.locator(".search-placeholder").innerText())).toBe("Пошук");
    expect(normalizeSpaces(await page.locator(".badge-new").first().innerText()).toLowerCase()).toBe("щойно додано");

    await page.goto(absoluteUrl("/listings"));
    await page.waitForSelector(".listings-page", { timeout: 30_000 });
    await expect(page.getByRole("combobox", { name: "Сортування за роком моделі" })).toBeVisible();
    expect(normalizeSpaces(await page.locator("#listings-sort-filter option:checked").innerText())).toBe("Порядок за замовчуванням");
    expect(normalizeSpaces(await page.locator(".result-count").innerText())).toBe("8 результатів");

    // The 404 page is localized too.
    await gotoApp(page, "/definitely-not-a-page", ".not-found-page");
    expect(normalizeSpaces(await page.locator(".not-found-page h1").innerText())).toBe("Не знайдено");
  });

  test("ru language switches UI strings", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await seedPreferences(page, RUSSIA_PREFS, ".home-page");

    expect(normalizeSpaces(await page.locator(".search-placeholder").innerText())).toBe("Поиск");
    expect(normalizeSpaces(await page.locator(".badge-new").first().innerText()).toLowerCase()).toBe("только добавлено");

    await gotoApp(page, "/definitely-not-a-page", ".not-found-page");
    expect(normalizeSpaces(await page.locator(".not-found-page h1").innerText())).toBe("Не найдено");
  });

  test("uk 390px sort selector shows distinct year directions and keeps controls in viewport", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await gotoApp(page, "/listings", ".listings-page");
    await seedPreferences(page, UKRAINE_PREFS, ".listings-page");

    const sortFilter = page.getByRole("combobox", { name: "Сортування за роком моделі" });
    const makeFilter = page.locator("#listings-make-filter");
    const viewport = page.viewportSize();
    expect(viewport).not.toBeNull();

    const assertControlsWithinViewport = async () => {
      const sortBounds = await sortFilter.boundingBox();
      const makeBounds = await makeFilter.boundingBox();
      expect(sortBounds).not.toBeNull();
      expect(makeBounds).not.toBeNull();
      expect(sortBounds!.x).toBeGreaterThanOrEqual(0);
      expect(sortBounds!.x + sortBounds!.width).toBeLessThanOrEqual(viewport!.width);
      expect(makeBounds!.x).toBeGreaterThanOrEqual(0);
      expect(makeBounds!.x + makeBounds!.width).toBeLessThanOrEqual(viewport!.width);
      expect(
        sortBounds!.x + sortBounds!.width <= makeBounds!.x
          || makeBounds!.x + makeBounds!.width <= sortBounds!.x
          || sortBounds!.y + sortBounds!.height <= makeBounds!.y
          || makeBounds!.y + makeBounds!.height <= sortBounds!.y
      ).toBe(true);
    };

    await expect(sortFilter.locator("option")).toHaveText([
      "Порядок за замовчуванням",
      "Новіші спочатку",
      "Старіші спочатку",
    ]);

    await sortFilter.selectOption("year-desc");
    await expect(sortFilter.locator("option:checked")).toHaveText("Новіші спочатку");
    await assertControlsWithinViewport();

    await sortFilter.selectOption("year-asc");
    await expect(sortFilter.locator("option:checked")).toHaveText("Старіші спочатку");
    await assertControlsWithinViewport();
  });

  test("ru 390px sort selector shows distinct year directions and keeps controls in viewport", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await gotoApp(page, "/listings", ".listings-page");
    await seedPreferences(page, RUSSIA_PREFS, ".listings-page");

    const sortFilter = page.getByRole("combobox", { name: "Сортировка по году модели" });
    const makeFilter = page.locator("#listings-make-filter");
    const viewport = page.viewportSize();
    expect(viewport).not.toBeNull();

    const assertControlsWithinViewport = async () => {
      const sortBounds = await sortFilter.boundingBox();
      const makeBounds = await makeFilter.boundingBox();
      expect(sortBounds).not.toBeNull();
      expect(makeBounds).not.toBeNull();
      expect(sortBounds!.x).toBeGreaterThanOrEqual(0);
      expect(sortBounds!.x + sortBounds!.width).toBeLessThanOrEqual(viewport!.width);
      expect(makeBounds!.x).toBeGreaterThanOrEqual(0);
      expect(makeBounds!.x + makeBounds!.width).toBeLessThanOrEqual(viewport!.width);
      expect(
        sortBounds!.x + sortBounds!.width <= makeBounds!.x
          || makeBounds!.x + makeBounds!.width <= sortBounds!.x
          || sortBounds!.y + sortBounds!.height <= makeBounds!.y
          || makeBounds!.y + makeBounds!.height <= sortBounds!.y
      ).toBe(true);
    };

    await expect(sortFilter.locator("option")).toHaveText([
      "Порядок по умолчанию",
      "Новые сначала",
      "Старые сначала",
    ]);

    await sortFilter.selectOption("year-desc");
    await expect(sortFilter.locator("option:checked")).toHaveText("Новые сначала");
    await assertControlsWithinViewport();

    await sortFilter.selectOption("year-asc");
    await expect(sortFilter.locator("option:checked")).toHaveText("Старые сначала");
    await assertControlsWithinViewport();
  });

  test("uk regional format changes separators but not currency", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await seedPreferences(page, UKRAINE_PREFS, ".home-page");

    // The listing price keeps its own currency (USD) even when CurrencyCode is UAH.
    expect(normalizeSpaces(await page.locator(".listing-card .price").first().innerText())).toBe("45 000 USD");
  });

  test("language, regional format and currency are independent", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await seedPreferences(page, MIXED_UK_FORMAT, ".home-page");

    // English strings despite uk regional format...
    expect(normalizeSpaces(await page.locator(".search-placeholder").innerText())).toBe("Search");
    // ...with uk number separators and no currency relabeling.
    expect(normalizeSpaces(await page.locator(".listing-card .price").first().innerText())).toBe("45 000 USD");
  });

  test("ru language with en regional format keeps en separators", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await seedPreferences(page, MIXED_US_FORMAT, ".home-page");

    expect(normalizeSpaces(await page.locator(".search-placeholder").innerText())).toBe("Поиск");
    expect(normalizeSpaces(await page.locator(".listing-card .price").first().innerText())).toBe("45,000 USD");
  });

  test("clearing preferences re-detects from the browser locale", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await seedPreferences(page, UKRAINE_PREFS, ".home-page");
    expect(normalizeSpaces(await page.locator(".search-placeholder").innerText())).toBe("Пошук");

    await clearPreferences(page, ".home-page");
    const prefs = await readPreferences(page);
    expect(prefs!.Language).toBe("en");
    expect(prefs.RegionalFormat).toBe("en-US");
    expect(normalizeSpaces(await page.locator(".search-placeholder").innerText())).toBe("Search");
  });
});
