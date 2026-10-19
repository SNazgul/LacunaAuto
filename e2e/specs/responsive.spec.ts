import { test, expect } from "@playwright/test";
import { gotoApp, ensurePreferences, DEFAULT_PREFERENCES, maxCardsPerRow, bottomNavHidden } from "../helpers/app";

// Breakpoints taken from the app CSS (Home.razor.css, Listings.razor.css):
//   .listings-feed / .listings-content: 1 column < 600px, 2 at >= 600px,
//   3 at >= 900px, 4 at >= 1200px.
//   NavMenu.razor.css: .bottom-nav is display:none above 1023.98px.
// Probes sit one CSS pixel either side of each boundary so an off-by-one
// media query fails visibly.
const GRID_PROBES = [
  { width: 599, columns: 1 },
  { width: 600, columns: 2 },
  { width: 899, columns: 2 },
  { width: 900, columns: 3 },
  { width: 1199, columns: 3 },
  { width: 1200, columns: 4 },
];

test.describe("Responsive layout breakpoints", () => {
  test("home grid columns at 600/900/1200 boundaries", async ({ page }) => {
    for (const probe of GRID_PROBES) {
      await page.setViewportSize({ width: probe.width, height: 900 });
      await gotoApp(page, "/", ".home-page");
      await ensurePreferences(page, DEFAULT_PREFERENCES, ".home-page");

      expect(await maxCardsPerRow(page), `home grid at ${probe.width}px`).toBe(probe.columns);
    }
  });

  test("listings grid columns at 600/900/1200 boundaries", async ({ page }) => {
    for (const probe of GRID_PROBES) {
      await page.setViewportSize({ width: probe.width, height: 900 });
      await gotoApp(page, "/listings", ".listings-page");
      await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

      expect(await maxCardsPerRow(page), `listings grid at ${probe.width}px`).toBe(probe.columns);
    }
  });

  test("bottom nav hidden at the 1024px boundary", async ({ page }) => {
    for (const width of [1023, 1024]) {
      await page.setViewportSize({ width, height: 900 });
      await gotoApp(page, "/", ".home-page");
      await ensurePreferences(page, DEFAULT_PREFERENCES, ".home-page");

      expect(await bottomNavHidden(page), `bottom nav at ${width}px`).toBe(width >= 1024);
    }
  });

  test("listing details usable without horizontal overflow at mobile/desktop widths", async ({ page }) => {
    for (const viewport of [{ width: 390, height: 844 }, { width: 1440, height: 900 }]) {
      await page.setViewportSize(viewport);
      await gotoApp(page, "/listings/1", ".details-shell");
      await ensurePreferences(page, DEFAULT_PREFERENCES, ".details-shell");

      // No horizontal scrollbar: the document never scrolls wider than the viewport.
      const scrollOverflow = await page.evaluate(
        () => Math.max(0, document.documentElement.scrollWidth - window.innerWidth),
      );
      expect(scrollOverflow, `details horizontal overflow at ${viewport.width}px`).toBe(0);

      // The details shell stays inside the viewport.
      const shell = await page.locator(".details-shell").boundingBox();
      expect(Math.ceil(shell.x + shell.width) <= viewport.width).toBe(true);

      // Key content is visible (not clipped off-screen).
      const priceBox = await page.locator(".details-content .price").boundingBox();
      expect(priceBox.x >= 0 && priceBox.x + priceBox.width <= viewport.width).toBe(true);
    }
  });
});
