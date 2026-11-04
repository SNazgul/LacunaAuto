import { test, expect } from "@playwright/test";
import { gotoApp, ensurePreferences, DEFAULT_PREFERENCES, normalizeSpaces, pollLoadedImages } from "../helpers/app";

type ListingCard = {
  id: number;
  make: string;
  model: string;
  trimOrVariant: string | null;
  year: number;
  price: number;
  originalCurrencyCode: string;
  mileage: number;
  fuelType: string | null;
  transmission: string | null;
  location: string | null;
  imageUrl: string | null;
  isNew: boolean;
};

const PAGE_SIZE = 8;

const orderListings = (listings: ListingCard[], sort: string | null): ListingCard[] => {
  const ordered = [...listings];
  if (sort === "year-desc") {
    return ordered.sort((a, b) => b.year - a.year || a.id - b.id);
  }

  if (sort === "year-asc") {
    return ordered.sort((a, b) => a.year - b.year || a.id - b.id);
  }

  return ordered.sort((a, b) => a.id - b.id);
};

const selectByMake = (listings: ListingCard[], make: string | null): ListingCard[] => {
  if (make === null || make.trim() === "") {
    return [...listings];
  }

  return listings.filter((listing) => listing.make === make);
};

const createPagedResponse = (listings: ListingCard[], page: number, pageSize: number) => {
  const totalCount = listings.length;
  const totalPages = totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize);
  if (totalCount === 0 || page > totalPages) {
    return { items: [], page, pageSize, totalCount, totalPages };
  }

  const start = (page - 1) * pageSize;
  return {
    items: listings.slice(start, start + pageSize),
    page,
    pageSize,
    totalCount,
    totalPages,
  };
};

const createMakeOptions = (listings: ListingCard[]): string[] => {
  const seenMakes = new Set<string>();
  const makes: string[] = [];
  for (const listing of listings) {
    const key = listing.make.toLowerCase();
    if (!seenMakes.has(key)) {
      seenMakes.add(key);
      makes.push(listing.make);
    }
  }

  return makes.sort((a, b) => a.localeCompare(b, undefined, { sensitivity: "base" }));
};

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

    await expect.poll(pollLoadedImages(page, ".listings-content img"), "decoded .listings-content images").toBe(8);
  });

  test("make selector filters, keeps options, and clears with all makes", async ({ page }) => {
    const allListings: ListingCard[] = [
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
    const requestedMakes: Array<string | null> = [];

    await page.route("**/api/listings/makes", async route => {
      await route.fulfill({ json: createMakeOptions(allListings) });
    });
    await page.route("**/api/listings/page*", async route => {
      const url = new URL(route.request().url());
      const make = url.searchParams.get("make");
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      requestedMakes.push(make);

      if (make === "BMW") {
        await route.fulfill({ json: createPagedResponse([], pageNumber, pageSize) });
        return;
      }

      const filtered = selectByMake(allListings, make);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
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

  test("make options failure is visible and retryable", async ({ page }) => {
    const allListings: ListingCard[] = [
      {
        id: 1, make: "Tesla", model: "Model 3", trimOrVariant: "Long Range",
        year: 2023, price: 45000, originalCurrencyCode: "USD", mileage: 12000,
        fuelType: "Electric", transmission: "Automatic", location: "Kyiv, Ukraine",
        imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: true,
      },
    ];
    let makeOptionsAttempts = 0;

    await page.route("**/api/listings/makes", async route => {
      makeOptionsAttempts++;
      if (makeOptionsAttempts === 1) {
        await route.fulfill({ status: 503, json: {} });
        return;
      }

      await route.fulfill({ json: createMakeOptions(allListings) });
    });
    await page.route("**/api/listings/page*", async route => {
      const url = new URL(route.request().url());
      const sort = url.searchParams.get("sort");
      const make = url.searchParams.get("make");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      const filtered = selectByMake(allListings, make);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    await expect(page.locator(".make-options-error")).toContainText(/unable to load make options/i);
    await page.locator(".make-options-error button").click();
    await expect(page.locator(".make-options-error")).toHaveCount(0);
    await expect(page.locator("#listings-make-filter option")).toHaveText(["All makes", "Tesla"]);
    expect(makeOptionsAttempts).toBeGreaterThanOrEqual(2);
  });

  test("make and sort compose, preserve selections, and honor year tie-breakers", async ({ page }) => {
    const allListings: ListingCard[] = [
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
    const requested = new Array<{ make: string | null; sort: string | null; page: number }>();

    await page.route("**/api/listings/makes", async route => {
      await route.fulfill({ json: createMakeOptions(allListings) });
    });
    await page.route("**/api/listings/page*", async route => {
      const url = new URL(route.request().url());
      const make = url.searchParams.get("make");
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      requested.push({ make, sort, page: pageNumber });

      const filtered = selectByMake(allListings, make);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
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

    expect(requested.some((item) => item.make === "Tesla" && item.sort === "year-desc" && item.page === 1)).toBe(true);
    expect(requested.some((item) => item.make === null && item.sort === "year-desc" && item.page === 1)).toBe(true);
  });

  test("390px paging preserves controls and resets page on sort and make changes", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });

    const longMake = "Straßenfahrzeugmanufaktur-International";
    const allListings: ListingCard[] = [
      { id: 1, make: longMake, model: "Tiguan", trimOrVariant: "R-Line", year: 2022, price: 35000, originalCurrencyCode: "USD", mileage: 18000, fuelType: "Diesel", transmission: "Automatic", location: "Kharkiv, Ukraine", imageUrl: "/images/vehicles/vw-tiguan.svg", isNew: false },
      { id: 2, make: "Tesla", model: "Model 3", trimOrVariant: "Long Range", year: 2023, price: 45000, originalCurrencyCode: "USD", mileage: 12000, fuelType: "Electric", transmission: "Automatic", location: "Kyiv, Ukraine", imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: true },
      { id: 3, make: "BMW", model: "X5", trimOrVariant: "xDrive40i", year: 2021, price: 62000, originalCurrencyCode: "USD", mileage: 25000, fuelType: "Petrol", transmission: "Automatic", location: "Lviv, Ukraine", imageUrl: "/images/vehicles/bmw-x5.svg", isNew: false },
      { id: 4, make: "Tesla", model: "Model Y", trimOrVariant: "Performance", year: 2024, price: 52000, originalCurrencyCode: "USD", mileage: 3000, fuelType: "Electric", transmission: "Automatic", location: "Odesa, Ukraine", imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: true },
      { id: 5, make: "Audi", model: "Q5", trimOrVariant: "S line", year: 2020, price: 43000, originalCurrencyCode: "USD", mileage: 41000, fuelType: "Diesel", transmission: "Automatic", location: "Dnipro, Ukraine", imageUrl: "/images/vehicles/default.svg", isNew: false },
      { id: 6, make: "Tesla", model: "Model S", trimOrVariant: "Plaid", year: 2024, price: 89000, originalCurrencyCode: "USD", mileage: 6000, fuelType: "Electric", transmission: "Automatic", location: "Kyiv, Ukraine", imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: true },
      { id: 7, make: "Ford", model: "Mustang", trimOrVariant: "GT", year: 2019, price: 39000, originalCurrencyCode: "USD", mileage: 50000, fuelType: "Petrol", transmission: "Manual", location: "Kharkiv, Ukraine", imageUrl: "/images/vehicles/default.svg", isNew: false },
      { id: 8, make: "Tesla", model: "Model X", trimOrVariant: "Long Range", year: 2023, price: 79000, originalCurrencyCode: "USD", mileage: 10000, fuelType: "Electric", transmission: "Automatic", location: "Lviv, Ukraine", imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: false },
      { id: 9, make: "Mazda", model: "CX-5", trimOrVariant: "Touring", year: 2022, price: 31000, originalCurrencyCode: "USD", mileage: 22000, fuelType: "Petrol", transmission: "Automatic", location: "Poltava, Ukraine", imageUrl: "/images/vehicles/default.svg", isNew: false },
      { id: 10, make: "Tesla", model: "Roadster", trimOrVariant: "Base", year: 2025, price: 120000, originalCurrencyCode: "USD", mileage: 1200, fuelType: "Electric", transmission: "Automatic", location: "Kyiv, Ukraine", imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: true },
    ];

    await page.route("**/api/listings/makes", async route => {
      await route.fulfill({ json: createMakeOptions(allListings) });
    });
    await page.route("**/api/listings/page*", async route => {
      const url = new URL(route.request().url());
      const make = url.searchParams.get("make");
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      const filtered = selectByMake(allListings, make);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    const makeFilter = page.locator("#listings-make-filter");
    const sortFilter = page.getByRole("combobox", { name: "Sort by model year" });
    const pageIndicator = page.locator(".pagination-indicator");
    const nextButton = page.getByRole("button", { name: "Next" });
    const previousButton = page.getByRole("button", { name: "Previous" });
    const viewport = page.viewportSize();
    expect(viewport).not.toBeNull();

    const assertControlsWithinViewport = async () => {
      const sortBounds = await sortFilter.boundingBox();
      const makeBounds = await makeFilter.boundingBox();
      const pagerBounds = await page.locator(".pagination-controls").boundingBox();

      expect(sortBounds).not.toBeNull();
      expect(makeBounds).not.toBeNull();
      expect(pagerBounds).not.toBeNull();

      expect(sortBounds!.x).toBeGreaterThanOrEqual(0);
      expect(sortBounds!.x + sortBounds!.width).toBeLessThanOrEqual(viewport!.width);
      expect(makeBounds!.x).toBeGreaterThanOrEqual(0);
      expect(makeBounds!.x + makeBounds!.width).toBeLessThanOrEqual(viewport!.width);
      expect(pagerBounds!.x).toBeGreaterThanOrEqual(0);
      expect(pagerBounds!.x + pagerBounds!.width).toBeLessThanOrEqual(viewport!.width);
    };

    await expect(pageIndicator).toHaveText("Page 1 of 2");
    await expect(previousButton).toBeDisabled();
    await expect(nextButton).toBeEnabled();
    await assertControlsWithinViewport();

    await nextButton.click();
    await expect(pageIndicator).toHaveText("Page 2 of 2");
    await expect(previousButton).toBeEnabled();
    await expect(nextButton).toBeDisabled();

    await sortFilter.selectOption("year-desc");
    await expect(pageIndicator).toHaveText("Page 1 of 2");
    await expect(sortFilter).toHaveValue("year-desc");

    await nextButton.click();
    await expect(pageIndicator).toHaveText("Page 2 of 2");

    await makeFilter.selectOption(longMake);
    await expect(pageIndicator).toHaveText("Page 1 of 1");
    await expect(page.locator(".listing-card")).toHaveCount(1);
    await expect(page.locator(".make-label")).toHaveText(longMake);
    await expect(nextButton).toBeDisabled();

    await makeFilter.selectOption("");
    await expect(pageIndicator).toHaveText("Page 1 of 2");
    await expect(sortFilter).toHaveValue("year-desc");
    await assertControlsWithinViewport();
  });

  test("390px keyboard Next moves focus to results and keeps first card below the sticky header", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });

    const allListings: ListingCard[] = Array.from({ length: 16 }, (_, index) => ({
      id: index + 1,
      make: index % 2 === 0 ? "Tesla" : "BMW",
      model: `Model ${index + 1}`,
      trimOrVariant: "Base",
      year: 2025 - (index % 4),
      price: 30000 + index,
      originalCurrencyCode: "USD",
      mileage: 5000 + index,
      fuelType: "Petrol",
      transmission: "Automatic",
      location: "Kyiv, Ukraine",
      imageUrl: "/images/vehicles/default.svg",
      isNew: index % 3 === 0,
    }));

    await page.route("**/api/listings/makes", async route => {
      await route.fulfill({ json: createMakeOptions(allListings) });
    });
    await page.route("**/api/listings/page*", async route => {
      const url = new URL(route.request().url());
      const make = url.searchParams.get("make");
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      const filtered = selectByMake(allListings, make);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    const nextButton = page.getByRole("button", { name: "Next" });
    const pageIndicator = page.locator(".pagination-indicator");
    await expect(pageIndicator).toHaveText("Page 1 of 2");

    await nextButton.scrollIntoViewIfNeeded();
    await nextButton.focus();
    await page.keyboard.press("Enter");

    await expect(pageIndicator).toHaveText("Page 2 of 2");
    await expect(page.locator("#listings-results-heading")).toBeFocused();
    await expect(page.locator(".listings-content .model-heading").first()).toHaveText("Model 9 Base");

    const headerBounds = await page.locator(".page-header").boundingBox();
    const firstCardBounds = await page.locator(".listings-content .listing-card").first().boundingBox();
    expect(headerBounds).not.toBeNull();
    expect(firstCardBounds).not.toBeNull();
    expect(firstCardBounds!.y).toBeGreaterThanOrEqual(headerBounds!.y + headerBounds!.height - 1);
  });

  test("catalog shrink recovery fetches the last valid page once", async ({ page }) => {
    const allListings: ListingCard[] = Array.from({ length: 16 }, (_, index) => ({
      id: index + 1,
      make: "Tesla",
      model: `Model ${index + 1}`,
      trimOrVariant: "Base",
      year: 2024 - (index % 3),
      price: 45000 + index,
      originalCurrencyCode: "USD",
      mileage: 10000 + index,
      fuelType: "Electric",
      transmission: "Automatic",
      location: "Kyiv, Ukraine",
      imageUrl: "/images/vehicles/tesla-model-3.svg",
      isNew: index % 2 === 0,
    }));
    const shrunkListings = allListings.slice(0, 8);
    const requestedPages: number[] = [];
    let catalogShrunk = false;

    await page.route("**/api/listings/makes", async route => {
      await route.fulfill({ json: createMakeOptions(allListings) });
    });
    await page.route("**/api/listings/page*", async route => {
      const url = new URL(route.request().url());
      const make = url.searchParams.get("make");
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      requestedPages.push(pageNumber);

      if (!catalogShrunk && pageNumber === 2) {
        catalogShrunk = true;
      }

      const activeListings = catalogShrunk ? shrunkListings : allListings;
      const filtered = selectByMake(activeListings, make);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    await expect(page.locator(".pagination-indicator")).toHaveText("Page 1 of 2");
    await page.getByRole("button", { name: "Next" }).click();

    await expect(page.locator(".pagination-indicator")).toHaveText("Page 1 of 1");
    await expect(page.locator(".listing-card")).toHaveCount(8);
    expect(requestedPages).toEqual([1, 2, 1]);
  });

  test("failed page request preserves make, sort, and page for retry", async ({ page }) => {
    const teslaListings: ListingCard[] = Array.from({ length: 10 }, (_, index) => ({
      id: index + 1,
      make: "Tesla",
      model: `Model ${index + 1}`,
      trimOrVariant: "Base",
      year: 2025 - index,
      price: 50000 + index,
      originalCurrencyCode: "USD",
      mileage: 1000 + index,
      fuelType: "Electric",
      transmission: "Automatic",
      location: "Kyiv, Ukraine",
      imageUrl: "/images/vehicles/tesla-model-3.svg",
      isNew: index % 2 === 0,
    }));
    const allListings: ListingCard[] = [
      ...teslaListings,
      {
        id: 20, make: "BMW", model: "X5", trimOrVariant: "xDrive40i",
        year: 2023, price: 72000, originalCurrencyCode: "USD", mileage: 5000,
        fuelType: "Petrol", transmission: "Automatic", location: "Lviv, Ukraine",
        imageUrl: "/images/vehicles/bmw-x5.svg", isNew: true,
      },
    ];
    const requested = new Array<{ make: string | null; sort: string | null; page: number }>();
    let failedOnce = false;

    await page.route("**/api/listings/makes", async route => {
      await route.fulfill({ json: createMakeOptions(allListings) });
    });
    await page.route("**/api/listings/page*", async route => {
      const url = new URL(route.request().url());
      const make = url.searchParams.get("make");
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      requested.push({ make, sort, page: pageNumber });

      if (!failedOnce && make === "Tesla" && sort === "year-desc" && pageNumber === 2) {
        failedOnce = true;
        await route.fulfill({ status: 503, json: {} });
        return;
      }

      const filtered = selectByMake(allListings, make);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    const makeFilter = page.locator("#listings-make-filter");
    const sortFilter = page.locator("#listings-sort-filter");

    await makeFilter.selectOption("Tesla");
    await sortFilter.selectOption("year-desc");
    await page.getByRole("button", { name: "Next" }).click();

    await expect(page.getByRole("alert")).toBeVisible();
    await expect(makeFilter).toHaveValue("Tesla");
    await expect(sortFilter).toHaveValue("year-desc");

    await page.getByRole("button", { name: /try again/i }).click();
    await expect(page.locator(".pagination-indicator")).toHaveText("Page 2 of 2");
    await expect(page.locator(".listing-card")).toHaveCount(2);

    const failedPageRequests = requested.filter((request) =>
      request.make === "Tesla" && request.sort === "year-desc" && request.page === 2);
    expect(failedPageRequests.length).toBeGreaterThanOrEqual(2);
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
