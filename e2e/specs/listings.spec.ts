import { test, expect } from "@playwright/test";
import { gotoApp, ensurePreferences, DEFAULT_PREFERENCES, normalizeSpaces, pollLoadedImages } from "../helpers/app";

test.describe("Listings screen", () => {
  test("header shows result count and default sort selector", async ({ page }) => {
    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    expect(normalizeSpaces(await page.locator(".result-count").innerText())).toBe("8 results");
    const sortFilter = page.getByRole("combobox", { name: "Sort by model year" });
    await expect(sortFilter.locator("option")).toHaveText(["Default order", "Newest first", "Oldest first"]);
    await expect(sortFilter).toHaveValue("");
    await expect(sortFilter.locator("option:checked")).toHaveText("Default order");
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

  test("make selector filters, keeps options, and clears with all makes", async ({ page }) => {
    const allListings = [
      {
        id: 1, make: "Tesla", model: "Model 3", trimOrVariant: "Long Range",
        year: 2023, price: 45000, originalCurrencyCode: "USD", mileage: 12000,
        fuelType: "Electric", transmission: "Automatic", location: "Kyiv, Ukraine",
        imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: true,
      },
      {
        id: 2, make: "BMW", model: "X5", trimOrVariant: "xDrive40i",
        year: 2022, price: 62000, originalCurrencyCode: "USD", mileage: 25000,
        fuelType: "Petrol", transmission: "Automatic", location: "Lviv, Ukraine",
        imageUrl: "/images/vehicles/bmw-x5.svg", isNew: false,
      },
      {
        id: 3, make: "Tesla", model: "Model Y", trimOrVariant: "Performance",
        year: 2024, price: 52000, originalCurrencyCode: "USD", mileage: 3000,
        fuelType: "Electric", transmission: "Automatic", location: "Odesa, Ukraine",
        imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: true,
      },
    ];
    const teslaListings = allListings.filter((listing) => listing.make === "Tesla");
    const requestedMakes: Array<string | null> = [];

    await page.route("**/api/listings*", async route => {
      const url = new URL(route.request().url());
      const make = url.searchParams.get("make");
      requestedMakes.push(make);

      if (make === null || make.trim() === "") {
        await route.fulfill({ json: allListings });
        return;
      }

      if (make === "Tesla") {
        await route.fulfill({ json: teslaListings });
        return;
      }

      if (make === "BMW") {
        await route.fulfill({ json: [] });
        return;
      }

      await route.fulfill({ json: [] });
    });

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    const makeFilter = page.locator("#listings-make-filter");
    await expect(makeFilter).toBeEnabled();
    await expect(makeFilter.locator("option")).toHaveText(["All makes", "BMW", "Tesla"]);

    await makeFilter.selectOption("Tesla");
    await expect(page.locator(".listing-card")).toHaveCount(2);
    await expect(page.locator(".make-label")).toHaveText(["Tesla", "Tesla"]);

    await makeFilter.selectOption("BMW");
    await expect(page.locator(".listing-card")).toHaveCount(0);
    await expect(page.locator(".browsing-feedback")).toContainText(/no results/i);
    await expect(makeFilter.locator("option")).toHaveText(["All makes", "BMW", "Tesla"]);

    await makeFilter.selectOption("");
    await expect(page.locator(".listing-card")).toHaveCount(3);
    await expect(page.locator(".make-label")).toHaveText(["Tesla", "BMW", "Tesla"]);

    expect(requestedMakes).toContain("Tesla");
    expect(requestedMakes).toContain("BMW");
    expect(requestedMakes.filter((value) => value === null).length).toBeGreaterThan(0);
  });

  test("make and sort compose, preserve selections, and honor year tie-breakers", async ({ page }) => {
    const allListings = [
      {
        id: 1, make: "Tesla", model: "Alpha", trimOrVariant: "Long Range",
        year: 2024, price: 45000, originalCurrencyCode: "USD", mileage: 12000,
        fuelType: "Electric", transmission: "Automatic", location: "Kyiv, Ukraine",
        imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: true,
      },
      {
        id: 2, make: "Tesla", model: "Bravo", trimOrVariant: "Base",
        year: 2022, price: 35000, originalCurrencyCode: "USD", mileage: 25000,
        fuelType: "Electric", transmission: "Automatic", location: "Lviv, Ukraine",
        imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: false,
      },
      {
        id: 3, make: "Tesla", model: "Charlie", trimOrVariant: "Plaid",
        year: 2024, price: 65000, originalCurrencyCode: "USD", mileage: 9000,
        fuelType: "Electric", transmission: "Automatic", location: "Odesa, Ukraine",
        imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: true,
      },
      {
        id: 4, make: "BMW", model: "Delta", trimOrVariant: "xDrive40i",
        year: 2025, price: 72000, originalCurrencyCode: "USD", mileage: 3000,
        fuelType: "Petrol", transmission: "Automatic", location: "Dnipro, Ukraine",
        imageUrl: "/images/vehicles/bmw-x5.svg", isNew: true,
      },
      {
        id: 5, make: "Tesla", model: "Echo", trimOrVariant: "Performance",
        year: 2023, price: 53000, originalCurrencyCode: "USD", mileage: 11000,
        fuelType: "Electric", transmission: "Automatic", location: "Kharkiv, Ukraine",
        imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: false,
      },
    ];
    const requested = new Array<{ make: string | null; sort: string | null }>();

    await page.route("**/api/listings*", async route => {
      const url = new URL(route.request().url());
      const make = url.searchParams.get("make");
      const sort = url.searchParams.get("sort");
      requested.push({ make, sort });

      const filtered = make === null || make.trim() === ""
        ? [...allListings]
        : allListings.filter((listing) => listing.make === make);

      const response = sort === "year-desc"
        ? filtered.sort((a, b) => b.year - a.year || a.id - b.id)
        : sort === "year-asc"
          ? filtered.sort((a, b) => a.year - b.year || a.id - b.id)
          : filtered.sort((a, b) => a.id - b.id);

      await route.fulfill({ json: response });
    });

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    const makeFilter = page.locator("#listings-make-filter");
    const sortFilter = page.locator("#listings-sort-filter");

    await makeFilter.selectOption("Tesla");
    await expect(page.locator(".model-heading")).toHaveText([
      "Alpha Long Range",
      "Bravo Base",
      "Charlie Plaid",
      "Echo Performance",
    ]);
    await expect(sortFilter).toHaveValue("");

    await sortFilter.selectOption("year-desc");
    await expect(makeFilter).toHaveValue("Tesla");
    await expect(page.locator(".model-heading")).toHaveText([
      "Alpha Long Range",
      "Charlie Plaid",
      "Echo Performance",
      "Bravo Base",
    ]);

    await makeFilter.selectOption("");
    await expect(sortFilter).toHaveValue("year-desc");
    await expect(page.locator(".model-heading")).toHaveText([
      "Delta xDrive40i",
      "Alpha Long Range",
      "Charlie Plaid",
      "Echo Performance",
      "Bravo Base",
    ]);

    await makeFilter.selectOption("Tesla");
    await sortFilter.selectOption("");
    await expect(makeFilter).toHaveValue("Tesla");
    await expect(page.locator(".model-heading")).toHaveText([
      "Alpha Long Range",
      "Bravo Base",
      "Charlie Plaid",
      "Echo Performance",
    ]);

    expect(requested.some((item) => item.make === "Tesla" && item.sort === "year-desc")).toBe(true);
    expect(requested.some((item) => item.make === null && item.sort === "year-desc")).toBe(true);
  });

  test("make and sort selectors stay within 390px, show distinct sort states, and remain usable", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });

    const longMake = "Straßenfahrzeugmanufaktur-International";
    const allListings = [
      {
        id: 1, make: longMake, model: "Tiguan", trimOrVariant: "R-Line",
        year: 2022, price: 35000, originalCurrencyCode: "USD", mileage: 18000,
        fuelType: "Diesel", transmission: "Automatic", location: "Kharkiv, Ukraine",
        imageUrl: "/images/vehicles/vw-tiguan.svg", isNew: false,
      },
      {
        id: 2, make: "Tesla", model: "Model 3", trimOrVariant: "Long Range",
        year: 2023, price: 45000, originalCurrencyCode: "USD", mileage: 12000,
        fuelType: "Electric", transmission: "Automatic", location: "Kyiv, Ukraine",
        imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: true,
      },
    ];

    await page.route("**/api/listings*", async route => {
      const url = new URL(route.request().url());
      const make = url.searchParams.get("make");
      if (make === null || make.trim() === "") {
        await route.fulfill({ json: allListings });
        return;
      }

      await route.fulfill({ json: allListings.filter((listing) => listing.make === make) });
    });

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    const makeFilter = page.locator("#listings-make-filter");
    const sortFilter = page.getByRole("combobox", { name: "Sort by model year" });
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

    await expect(sortFilter.locator("option:checked")).toHaveText("Default order");
    await assertControlsWithinViewport();

    await sortFilter.selectOption("year-desc");
    await expect(sortFilter).toHaveValue("year-desc");
    await expect(sortFilter.locator("option:checked")).toHaveText("Newest first");
    await assertControlsWithinViewport();

    await makeFilter.selectOption(longMake);
    await expect(page.locator(".listing-card")).toHaveCount(1);
    await expect(page.locator(".make-label")).toHaveText(longMake);
    await assertControlsWithinViewport();

    await sortFilter.selectOption("year-asc");
    await expect(sortFilter).toHaveValue("year-asc");
    await expect(sortFilter.locator("option:checked")).toHaveText("Oldest first");
    await assertControlsWithinViewport();

    await makeFilter.selectOption("");
    await expect(sortFilter).toHaveValue("year-asc");
    await expect(page.locator(".listing-card")).toHaveCount(2);
  });

  test("live listings filter selects a seeded make through web proxy and clears", async ({ page }) => {
    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    const makeFilter = page.locator("#listings-make-filter");
    await makeFilter.selectOption("Volkswagen");
    await expect(page.locator(".listing-card")).toHaveCount(1);
    await expect(page.locator(".make-label")).toHaveText("Volkswagen");
    expect(normalizeSpaces(await page.locator(".result-count").innerText())).toBe("1 result");

    await makeFilter.selectOption("");
    await expect(page.locator(".listing-card")).toHaveCount(8);
    expect(normalizeSpaces(await page.locator(".result-count").innerText())).toBe("8 results");
  });
});
