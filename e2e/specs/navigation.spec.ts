import { test, expect } from "@playwright/test";
import { gotoApp, ensurePreferences, DEFAULT_PREFERENCES, normalizeSpaces, allDisabled, spaNavigate, trackMainFrameDocumentRequests, setWindowMarker, readWindowMarker, waitForAppReady } from "../helpers/app";

test.describe("Navigation shell and routing", () => {
  test("home page renders seed feed", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".home-page");
    expect(await page.locator(".listing-card").count()).toBe(8);
  });

  test("bottom nav links navigate", async ({ page }) => {
    // Bottom nav is hidden from 1024px up, so use a mobile viewport.
    await page.setViewportSize({ width: 390, height: 844 });
    await gotoApp(page, "/", ".home-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".home-page");

    await page.locator("a.nav-item-search").click();
    await expect(page).toHaveURL(/\/listings$/);

    await page.locator("a.nav-item-home").click();
    await expect(page).toHaveURL(/^\S+\/$/);
  });

  test("sell button stays disabled", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".home-page");
    expect(await allDisabled(page, "button.sell-button")).toBe(true);
  });

  test("unknown listing id shows not-found block", async ({ page }) => {
    await gotoApp(page, "/listings/999", ".not-found");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".not-found");
    expect(normalizeSpaces(await page.locator(".not-found h2").innerText())).toBe("Not found");

    await page.locator("button.back-button-inline").click();
    await expect(page).toHaveURL(/^\S+\/$/);
  });

  test("unknown route renders the localized 404 page", async ({ page }) => {
    const response = await gotoApp(page, "/definitely-not-a-page", ".not-found-page");
    expect(response.status()).toBe(404);

    await ensurePreferences(page, DEFAULT_PREFERENCES, ".not-found-page");
    expect(normalizeSpaces(await page.locator(".not-found-page h1").innerText())).toBe("Not found");
    expect(await page.locator("button.home-button").count()).toBe(1);
    // The page keeps the application layout (main content area + bottom nav shell).
    expect(await page.locator("main").count()).toBe(1);
    expect(await page.locator("nav.bottom-nav").count()).toBe(1);

    await page.locator("button.home-button").click();
    await expect(page).toHaveURL(/^\S+\/$/);
  });

  // Distinct from the direct-request test above: here the app is already running and the
  // navigation is performed by Blazor's own client-side router, so no new document is fetched.
  test("client-side navigation to an unknown route reuses the same document", async ({ page }) => {
    await gotoApp(page, "/", ".home-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".home-page");

    // A marker on the current JS window is lost by any real document navigation (goto, reload,
    // window.location assignment) because those create a new window.
    const marker = `lacuna-spa-${Date.now()}`;
    await setWindowMarker(page, "__lacuna_document_marker__", marker);

    // Track main-frame document requests only from here on; startup already fetched Home.
    const documentRequests = trackMainFrameDocumentRequests(page);

    await spaNavigate(page, "/in-app-unknown-route");

    await waitForAppReady(page, ".not-found-page");
    await expect(page).toHaveURL(/\/in-app-unknown-route$/);
    expect(normalizeSpaces(await page.locator(".not-found-page h1").innerText())).toBe("Not found");

    // Same JS window => the marker survives; same document => no new main-frame document request.
    console.log(
      "spa-navigation:",
      JSON.stringify({ markerBefore: marker, markerAfter: await readWindowMarker(page, "__lacuna_document_marker__"), documentRequests }),
    );
    expect(await readWindowMarker(page, "__lacuna_document_marker__"), "marker survives SPA navigation").toBe(marker);
    expect(documentRequests, "no new main-frame document request during SPA navigation").toEqual([]);

    // The Home control returns to the rendered Home page, still in the same document.
    await page.locator("button.home-button").click();
    await waitForAppReady(page, ".home-page");
    await expect(page).toHaveURL(/^\S+\/$/);
    expect(await page.locator(".listing-card").count()).toBe(8);
    console.log("SPA-EVIDENCE marker after returning Home =", await readWindowMarker(page, "__lacuna_document_marker__"));
    expect(await readWindowMarker(page, "__lacuna_document_marker__")).toBe(marker);
  });
});
