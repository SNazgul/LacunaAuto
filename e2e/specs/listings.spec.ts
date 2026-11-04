import { test, expect, type Locator, type Page, type Route } from "@playwright/test";
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
const PROBLEM_DETAILS = { title: "Invalid listings filters.", traceId: "test-trace-id" };

const normalizeFilter = (value: string | null): string | null => {
  if (value === null) {
    return null;
  }

  const normalized = value.trim();
  return normalized.length === 0 ? null : normalized;
};

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

const selectByMakeAndModel = (listings: ListingCard[], make: string | null, model: string | null): ListingCard[] => {
  const normalizedMake = normalizeFilter(make);
  const normalizedModel = normalizeFilter(model);

  let filteredListings = [...listings];
  if (normalizedMake !== null) {
    filteredListings = filteredListings.filter((listing) => listing.make.toLowerCase() === normalizedMake.toLowerCase());
  }

  if (normalizedModel !== null) {
    filteredListings = filteredListings.filter((listing) => listing.model.toLowerCase() === normalizedModel.toLowerCase());
  }

  return filteredListings;
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

const createModelOptions = (listings: ListingCard[], make: string | null): string[] => {
  const filteredByMake = selectByMakeAndModel(listings, make, null);
  const seenModels = new Set<string>();
  const models: string[] = [];
  for (const listing of filteredByMake) {
    const key = listing.model.toLowerCase();
    if (!seenModels.has(key)) {
      seenModels.add(key);
      models.push(listing.model);
    }
  }

  return models.sort((a, b) => a.localeCompare(b, undefined, { sensitivity: "base" }));
};

const routeModelOptions = async (route: Route, listings: ListingCard[]) => {
  const url = new URL(route.request().url());
  const make = normalizeFilter(url.searchParams.get("make"));
  if (make === null) {
    await route.fulfill({ status: 400, json: PROBLEM_DETAILS });
    return;
  }

  await route.fulfill({ json: createModelOptions(listings, make) });
};

const selectComboboxOption = async (page: Page, combobox: Locator, optionText: string) => {
  await combobox.click();
  await combobox.fill(optionText);
  await page.getByRole("option", { name: optionText, exact: true }).click();
};

const expectActiveOptionVisible = async (page: Page, combobox: Locator, listboxSelector: string) => {
  await expect.poll(async () => {
    const activeOptionId = await combobox.getAttribute("aria-activedescendant");
    if (activeOptionId === null || activeOptionId.length === 0) {
      return false;
    }

    return await page.evaluate(({ listboxSelector, activeOptionId }) => {
      const listbox = document.querySelector(listboxSelector);
      const option = document.getElementById(activeOptionId);
      if (!(listbox instanceof HTMLElement) || !(option instanceof HTMLElement)) {
        return false;
      }

      const listboxBounds = listbox.getBoundingClientRect();
      const optionBounds = option.getBoundingClientRect();
      return optionBounds.top >= listboxBounds.top - 1 && optionBounds.bottom <= listboxBounds.bottom + 1;
    }, { listboxSelector, activeOptionId });
  }).toBe(true);
};

const expectLastOptionHighlightedAndVisible = async (
  page: Page,
  combobox: Locator,
  listboxSelector: string,
  optionIdPrefix: string,
) => {
  const optionCount = await page.locator(`${listboxSelector} [role='option']`).count();
  expect(optionCount).toBeGreaterThan(0);
  await expect(combobox).toHaveAttribute("aria-activedescendant", `${optionIdPrefix}${optionCount - 1}`);
  await expectActiveOptionVisible(page, combobox, listboxSelector);
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
      const make = normalizeFilter(url.searchParams.get("make"));
      const model = normalizeFilter(url.searchParams.get("model"));
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      if (model !== null && make === null) {
        await route.fulfill({ status: 400, json: PROBLEM_DETAILS });
        return;
      }

      requestedMakes.push(make);

      if (make === "BMW") {
        await route.fulfill({ json: createPagedResponse([], pageNumber, pageSize) });
        return;
      }

      const filtered = selectByMakeAndModel(allListings, make, model);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });
    await page.route("**/api/listings/models*", async route => routeModelOptions(route, allListings));

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    const makeFilter = page.locator("#listings-make-filter");
    await expect(makeFilter).toBeEnabled();
    await makeFilter.click();
    await expect(page.locator("#listings-make-options [role='option']")).toHaveText(["All makes", "BMW", "Tesla"]);
    await makeFilter.press("Escape");

    await selectComboboxOption(page, makeFilter, "Tesla");
    await expect(page.locator(".listing-card")).toHaveCount(2);
    await expect(page.locator(".make-label")).toHaveText(["Tesla", "Tesla"]);

    await selectComboboxOption(page, makeFilter, "BMW");
    await expect(page.locator(".listing-card")).toHaveCount(0);
    await expect(page.locator(".browsing-feedback")).toContainText(/no results/i);

    await selectComboboxOption(page, makeFilter, "All makes");
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
      const make = normalizeFilter(url.searchParams.get("make"));
      const model = normalizeFilter(url.searchParams.get("model"));
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      if (model !== null && make === null) {
        await route.fulfill({ status: 400, json: PROBLEM_DETAILS });
        return;
      }

      const filtered = selectByMakeAndModel(allListings, make, model);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });
    await page.route("**/api/listings/models*", async route => routeModelOptions(route, allListings));

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    await expect(page.locator(".make-options-error")).toContainText(/unable to load make options/i);
    await page.locator(".make-options-error button").click();
    await expect(page.locator(".make-options-error")).toHaveCount(0);
    await page.locator("#listings-make-filter").click();
    await expect(page.locator("#listings-make-options [role='option']")).toHaveText(["All makes", "Tesla"]);
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
    const requested = new Array<{ make: string | null; model: string | null; sort: string | null; page: number }>();

    await page.route("**/api/listings/makes", async route => {
      await route.fulfill({ json: createMakeOptions(allListings) });
    });
    await page.route("**/api/listings/page*", async route => {
      const url = new URL(route.request().url());
      const make = normalizeFilter(url.searchParams.get("make"));
      const model = normalizeFilter(url.searchParams.get("model"));
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      if (model !== null && make === null) {
        await route.fulfill({ status: 400, json: PROBLEM_DETAILS });
        return;
      }

      requested.push({ make, model, sort, page: pageNumber });

      const filtered = selectByMakeAndModel(allListings, make, model);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });
    await page.route("**/api/listings/models*", async route => routeModelOptions(route, allListings));

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    const makeFilter = page.locator("#listings-make-filter");
    const sortFilter = page.locator("#listings-sort-filter");

    await selectComboboxOption(page, makeFilter, "Tesla");
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

    await selectComboboxOption(page, makeFilter, "All makes");
    await expect(sortFilter).toHaveValue("year-desc");
    await expect(page.locator(".model-heading")).toHaveText([
      "Delta xDrive40i",
      "Alpha Long Range",
      "Charlie Plaid",
      "Echo Performance",
      "Bravo Base",
    ]);

    await selectComboboxOption(page, makeFilter, "Tesla");
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
      const make = normalizeFilter(url.searchParams.get("make"));
      const model = normalizeFilter(url.searchParams.get("model"));
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      if (model !== null && make === null) {
        await route.fulfill({ status: 400, json: PROBLEM_DETAILS });
        return;
      }

      const filtered = selectByMakeAndModel(allListings, make, model);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });
    await page.route("**/api/listings/models*", async route => routeModelOptions(route, allListings));

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

    await selectComboboxOption(page, makeFilter, longMake);
    await expect(pageIndicator).toHaveText("Page 1 of 1");
    await expect(page.locator(".listing-card")).toHaveCount(1);
    await expect(page.locator(".make-label")).toHaveText(longMake);
    await expect(nextButton).toBeDisabled();

    await selectComboboxOption(page, makeFilter, "All makes");
    await expect(pageIndicator).toHaveText("Page 1 of 2");
    await expect(sortFilter).toHaveValue("year-desc");
    await assertControlsWithinViewport();
  });

  test("390px combobox typing does not fetch listings, Enter applies, and Escape restores", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });

    const allListings: ListingCard[] = [
      {
        id: 1, make: "Tesla", model: "Model 3", trimOrVariant: "Long Range",
        year: 2024, price: 45000, originalCurrencyCode: "USD", mileage: 12000,
        fuelType: "Electric", transmission: "Automatic", location: "Kyiv, Ukraine",
        imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: true,
      },
      {
        id: 2, make: "Tesla", model: "Model Y", trimOrVariant: "Performance",
        year: 2023, price: 52000, originalCurrencyCode: "USD", mileage: 8000,
        fuelType: "Electric", transmission: "Automatic", location: "Lviv, Ukraine",
        imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: false,
      },
      {
        id: 3, make: "Volkswagen", model: "Tiguan", trimOrVariant: "R-Line",
        year: 2022, price: 33000, originalCurrencyCode: "USD", mileage: 25000,
        fuelType: "Diesel", transmission: "Automatic", location: "Dnipro, Ukraine",
        imageUrl: "/images/vehicles/vw-tiguan.svg", isNew: false,
      },
    ];
    let listingsRequests = 0;

    await page.route("**/api/listings/makes", async route => {
      await route.fulfill({ json: createMakeOptions(allListings) });
    });
    await page.route("**/api/listings/models*", async route => routeModelOptions(route, allListings));
    await page.route("**/api/listings/page*", async route => {
      listingsRequests++;
      const url = new URL(route.request().url());
      const make = normalizeFilter(url.searchParams.get("make"));
      const model = normalizeFilter(url.searchParams.get("model"));
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      if (model !== null && make === null) {
        await route.fulfill({ status: 400, json: PROBLEM_DETAILS });
        return;
      }

      const filtered = selectByMakeAndModel(allListings, make, model);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");
    await expect(page.locator(".listing-card")).toHaveCount(3);

    const makeFilter = page.locator("#listings-make-filter");
    const initialRequestCount = listingsRequests;

    await makeFilter.click();
    await makeFilter.fill("tes");
    await expect(page.locator("#listings-make-options [role='option']")).toContainText(["Tesla"]);
    expect(listingsRequests).toBe(initialRequestCount);

    await makeFilter.press("ArrowDown");
    await makeFilter.press("Enter");
    await expect.poll(() => listingsRequests).toBe(initialRequestCount + 1);
    await expect(makeFilter).toHaveValue("Tesla");
    await expect(page.locator(".listing-card")).toHaveCount(2);

    const afterMakeApplyRequestCount = listingsRequests;
    await makeFilter.click();
    await makeFilter.fill("vol");
    expect(listingsRequests).toBe(afterMakeApplyRequestCount);
    await makeFilter.press("Escape");
    await expect(makeFilter).toHaveValue("Tesla");
    expect(listingsRequests).toBe(afterMakeApplyRequestCount);

    const modelFilter = page.locator("#listings-model-filter");
    await expect(modelFilter).toBeVisible();
    await expect(modelFilter).toBeEnabled();

    const beforeModelSelectRequestCount = listingsRequests;
    await modelFilter.click();
    await modelFilter.fill("3");
    await expect(page.locator("#listings-model-options [role='option']")).toContainText(["Model 3"]);
    expect(listingsRequests).toBe(beforeModelSelectRequestCount);

    await modelFilter.press("ArrowDown");
    await modelFilter.press("Enter");
    await expect.poll(() => listingsRequests).toBe(beforeModelSelectRequestCount + 1);
    await expect(modelFilter).toHaveValue("Model 3");
    await expect(page.locator(".listing-card")).toHaveCount(1);

    const afterModelApplyRequestCount = listingsRequests;
    await modelFilter.click();
    await modelFilter.fill("zzz");
    expect(listingsRequests).toBe(afterModelApplyRequestCount);
    await modelFilter.press("Escape");
    await expect(modelFilter).toHaveValue("Model 3");
    expect(listingsRequests).toBe(afterModelApplyRequestCount);

    await selectComboboxOption(page, makeFilter, "Volkswagen");
    await expect(page.locator(".listing-card")).toHaveCount(1);
    await expect(page.locator("#listings-model-filter")).toHaveValue("");
  });

  test("make selection loads listings immediately while model options stay independent", async ({ page }) => {
    const allListings: ListingCard[] = [
      {
        id: 1, make: "Tesla", model: "Model 3", trimOrVariant: "Long Range",
        year: 2023, price: 45000, originalCurrencyCode: "USD", mileage: 12000,
        fuelType: "Electric", transmission: "Automatic", location: "Kyiv, Ukraine",
        imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: true,
      },
      {
        id: 2, make: "BMW", model: "X5", trimOrVariant: "xDrive40i",
        year: 2024, price: 62000, originalCurrencyCode: "USD", mileage: 9000,
        fuelType: "Petrol", transmission: "Automatic", location: "Lviv, Ukraine",
        imageUrl: "/images/vehicles/bmw-x5.svg", isNew: true,
      },
      {
        id: 3, make: "Tesla", model: "Model Y", trimOrVariant: "Performance",
        year: 2024, price: 52000, originalCurrencyCode: "USD", mileage: 3000,
        fuelType: "Electric", transmission: "Automatic", location: "Odesa, Ukraine",
        imageUrl: "/images/vehicles/tesla-model-3.svg", isNew: true,
      },
      {
        id: 4, make: "BMW", model: "i4", trimOrVariant: "M50",
        year: 2022, price: 58000, originalCurrencyCode: "USD", mileage: 15000,
        fuelType: "Electric", transmission: "Automatic", location: "Dnipro, Ukraine",
        imageUrl: "/images/vehicles/default.svg", isNew: false,
      },
    ];
    const listingsRequests = new Array<{ make: string | null; model: string | null }>();
    let resolveTeslaModelRequest: (() => void) | null = null;
    const teslaModelRequestGate = new Promise<void>(resolve => {
      resolveTeslaModelRequest = resolve;
    });
    let bmwModelOptionsRequests = 0;

    await page.route("**/api/listings/makes", async route => {
      await route.fulfill({ json: createMakeOptions(allListings) });
    });
    await page.route("**/api/listings/page*", async route => {
      const url = new URL(route.request().url());
      const make = normalizeFilter(url.searchParams.get("make"));
      const model = normalizeFilter(url.searchParams.get("model"));
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      if (model !== null && make === null) {
        await route.fulfill({ status: 400, json: PROBLEM_DETAILS });
        return;
      }

      listingsRequests.push({ make, model });
      const filtered = selectByMakeAndModel(allListings, make, model);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });
    await page.route("**/api/listings/models*", async route => {
      const url = new URL(route.request().url());
      const make = normalizeFilter(url.searchParams.get("make"));
      if (make === null) {
        await route.fulfill({ status: 400, json: PROBLEM_DETAILS });
        return;
      }

      if (make === "Tesla") {
        await teslaModelRequestGate;
        await route.fulfill({ json: createModelOptions(allListings, make) });
        return;
      }

      if (make === "BMW") {
        bmwModelOptionsRequests++;
        if (bmwModelOptionsRequests === 1) {
          await route.fulfill({ status: 503, json: {} });
          return;
        }
      }

      await route.fulfill({ json: createModelOptions(allListings, make) });
    });

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    const makeFilter = page.locator("#listings-make-filter");
    await selectComboboxOption(page, makeFilter, "Tesla");
    await expect.poll(() => listingsRequests.filter(request => request.make === "Tesla").length).toBe(1);

    await selectComboboxOption(page, makeFilter, "BMW");
    await expect.poll(() => listingsRequests.filter(request => request.make === "BMW").length).toBe(1);
    await expect(page.locator(".listing-card")).toHaveCount(2);
    await expect(page.locator(".make-label")).toHaveText(["BMW", "BMW"]);
    await expect(page.locator(".model-options-error")).toContainText(/unable to load model options/i);

    const listingsRequestsBeforeRetry = listingsRequests.length;
    resolveTeslaModelRequest?.();
    await expect.poll(() => listingsRequests.filter(request => request.make === "BMW").length).toBe(1);
    await expect.poll(() => listingsRequests.length).toBe(listingsRequestsBeforeRetry);

    await page.locator(".model-options-error button").click();
    await expect(page.locator(".model-options-error")).toHaveCount(0);
    await expect.poll(() => bmwModelOptionsRequests).toBe(2);
    await expect.poll(() => listingsRequests.length).toBe(listingsRequestsBeforeRetry);

    const modelFilter = page.locator("#listings-model-filter");
    await modelFilter.click();
    await expect(page.locator("#listings-model-options [role='option']")).toHaveText(["All models", "i4", "X5"]);
    await expect(page.getByRole("option", { name: "Model 3", exact: true })).toHaveCount(0);
  });

  test("390px combobox keyboard keeps active option visible and tabs past popup options", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });

    const allListings: ListingCard[] = [];
    let listingId = 1;
    for (let index = 1; index <= 20; index++) {
      const make = `Make ${index.toString().padStart(2, "0")}`;
      allListings.push({
        id: listingId++,
        make,
        model: "Standard",
        trimOrVariant: "Base",
        year: 2024,
        price: 25000 + index,
        originalCurrencyCode: "USD",
        mileage: 1000 + index,
        fuelType: "Petrol",
        transmission: "Automatic",
        location: "Kyiv, Ukraine",
        imageUrl: "/images/vehicles/default.svg",
        isNew: index % 2 === 0,
      });
    }

    const selectedMake = "Make 18";
    for (let index = 1; index <= 24; index++) {
      allListings.push({
        id: listingId++,
        make: selectedMake,
        model: `Series ${index.toString().padStart(2, "0")}`,
        trimOrVariant: "Base",
        year: 2025,
        price: 40000 + index,
        originalCurrencyCode: "USD",
        mileage: 500 + index,
        fuelType: "Electric",
        transmission: "Automatic",
        location: "Lviv, Ukraine",
        imageUrl: "/images/vehicles/default.svg",
        isNew: true,
      });
    }

    await page.route("**/api/listings/makes", async route => {
      await route.fulfill({ json: createMakeOptions(allListings) });
    });
    await page.route("**/api/listings/page*", async route => {
      const url = new URL(route.request().url());
      const make = normalizeFilter(url.searchParams.get("make"));
      const model = normalizeFilter(url.searchParams.get("model"));
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      if (model !== null && make === null) {
        await route.fulfill({ status: 400, json: PROBLEM_DETAILS });
        return;
      }

      const filtered = selectByMakeAndModel(allListings, make, model);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });
    await page.route("**/api/listings/models*", async route => routeModelOptions(route, allListings));

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    const makeFilter = page.locator("#listings-make-filter");
    await makeFilter.click();
    await makeFilter.fill("Make");
    for (let index = 0; index < 16; index++) {
      await makeFilter.press("ArrowDown");
      await expectActiveOptionVisible(page, makeFilter, "#listings-make-options");
    }

    for (let index = 0; index < 6; index++) {
      await makeFilter.press("ArrowUp");
      await expectActiveOptionVisible(page, makeFilter, "#listings-make-options");
    }

    await makeFilter.press("Escape");
    await expect(page.locator("#listings-make-options")).toHaveCount(0);
    await expect(makeFilter).toHaveValue("");

    await makeFilter.press("ArrowUp");
    await expect(page.locator("#listings-make-options")).toHaveCount(1);
    await expectLastOptionHighlightedAndVisible(page, makeFilter, "#listings-make-options", "listings-make-option-");

    await makeFilter.press("Escape");
    await expect(page.locator("#listings-make-options")).toHaveCount(0);

    await selectComboboxOption(page, makeFilter, selectedMake);

    const modelFilter = page.locator("#listings-model-filter");
    await expect(modelFilter).toBeVisible();

    await makeFilter.click();
    await makeFilter.fill("Make");
    await makeFilter.press("Tab");
    await expect(page.locator("#listings-make-options")).toHaveCount(0);
    await expect(modelFilter).toBeFocused();
    await expect(makeFilter).toHaveValue(selectedMake);

    await modelFilter.click();
    await modelFilter.fill("Series");
    for (let index = 0; index < 16; index++) {
      await modelFilter.press("ArrowDown");
      await expectActiveOptionVisible(page, modelFilter, "#listings-model-options");
    }

    for (let index = 0; index < 8; index++) {
      await modelFilter.press("ArrowUp");
      await expectActiveOptionVisible(page, modelFilter, "#listings-model-options");
    }

    await modelFilter.press("Escape");
    await expect(page.locator("#listings-model-options")).toHaveCount(0);
    await expect(modelFilter).toHaveValue("");

    await modelFilter.press("ArrowUp");
    await expect(page.locator("#listings-model-options")).toHaveCount(1);
    await expectLastOptionHighlightedAndVisible(page, modelFilter, "#listings-model-options", "listings-model-option-");

    await modelFilter.press("Enter");
    await expect(modelFilter).not.toHaveValue("");
    await expect(page.locator(".listing-card")).toHaveCount(1);

    await modelFilter.click();
    await modelFilter.fill("Series 03");
    await page.getByRole("option", { name: "Series 03", exact: true }).click();
    await expect(modelFilter).toHaveValue("Series 03");
    await expect(page.locator(".listing-card")).toHaveCount(1);

    await modelFilter.click();
    await modelFilter.fill("zzz");
    await modelFilter.press("Escape");
    await expect(modelFilter).toHaveValue("Series 03");

    await modelFilter.click();
    await modelFilter.fill("Series");
    await modelFilter.press("Tab");
    await expect(page.locator("#listings-model-options")).toHaveCount(0);
    await expect(modelFilter).not.toBeFocused();
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
      const make = normalizeFilter(url.searchParams.get("make"));
      const model = normalizeFilter(url.searchParams.get("model"));
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      if (model !== null && make === null) {
        await route.fulfill({ status: 400, json: PROBLEM_DETAILS });
        return;
      }

      const filtered = selectByMakeAndModel(allListings, make, model);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });
    await page.route("**/api/listings/models*", async route => routeModelOptions(route, allListings));

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
      const make = normalizeFilter(url.searchParams.get("make"));
      const model = normalizeFilter(url.searchParams.get("model"));
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      if (model !== null && make === null) {
        await route.fulfill({ status: 400, json: PROBLEM_DETAILS });
        return;
      }

      requestedPages.push(pageNumber);

      if (!catalogShrunk && pageNumber === 2) {
        catalogShrunk = true;
      }

      const activeListings = catalogShrunk ? shrunkListings : allListings;
      const filtered = selectByMakeAndModel(activeListings, make, model);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });
    await page.route("**/api/listings/models*", async route => routeModelOptions(route, allListings));

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
    const requested = new Array<{ make: string | null; model: string | null; sort: string | null; page: number }>();
    let failedOnce = false;

    await page.route("**/api/listings/makes", async route => {
      await route.fulfill({ json: createMakeOptions(allListings) });
    });
    await page.route("**/api/listings/page*", async route => {
      const url = new URL(route.request().url());
      const make = normalizeFilter(url.searchParams.get("make"));
      const model = normalizeFilter(url.searchParams.get("model"));
      const sort = url.searchParams.get("sort");
      const pageNumber = Number(url.searchParams.get("page") ?? "1");
      const pageSize = Number(url.searchParams.get("pageSize") ?? PAGE_SIZE.toString());
      if (model !== null && make === null) {
        await route.fulfill({ status: 400, json: PROBLEM_DETAILS });
        return;
      }

      requested.push({ make, model, sort, page: pageNumber });

      if (!failedOnce && make === "Tesla" && sort === "year-desc" && pageNumber === 2) {
        failedOnce = true;
        await route.fulfill({ status: 503, json: {} });
        return;
      }

      const filtered = selectByMakeAndModel(allListings, make, model);
      await route.fulfill({ json: createPagedResponse(orderListings(filtered, sort), pageNumber, pageSize) });
    });
    await page.route("**/api/listings/models*", async route => routeModelOptions(route, allListings));

    await gotoApp(page, "/listings", ".listings-page");
    await ensurePreferences(page, DEFAULT_PREFERENCES, ".listings-page");

    const makeFilter = page.locator("#listings-make-filter");
    const sortFilter = page.locator("#listings-sort-filter");

    await selectComboboxOption(page, makeFilter, "Tesla");
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
    await selectComboboxOption(page, makeFilter, "Volkswagen");
    await expect(page.locator(".listing-card")).toHaveCount(1);
    await expect(page.locator(".make-label")).toHaveText("Volkswagen");
    expect(normalizeSpaces(await page.locator(".result-count").innerText())).toBe("1 result");

    await selectComboboxOption(page, makeFilter, "All makes");
    await expect(page.locator(".listing-card")).toHaveCount(8);
    expect(normalizeSpaces(await page.locator(".result-count").innerText())).toBe("8 results");
  });
});
