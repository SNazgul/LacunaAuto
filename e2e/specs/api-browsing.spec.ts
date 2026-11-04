import { test, expect } from "@playwright/test";
import { absoluteUrl, gotoApp, spaNavigate } from "../helpers/app";

// Deliberately different from all Development seed records: these assertions
// fail if the browser silently falls back to its old local seed data.
const listing = {
  id: 101, make: "API Motors", model: "Live vehicle", trimOrVariant: null,
  year: 2024, price: 12345, originalCurrencyCode: "EUR", mileage: 7654,
  fuelType: "Hybrid", transmission: "Manual", location: "API location",
  imageUrl: null, isNew: false,
};

const paged = (items: unknown[], page = 1, pageSize = 8, totalCount = items.length, totalPages = totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize)) => ({
  items,
  page,
  pageSize,
  totalCount,
  totalPages,
});

for (const path of ["/", "/listings"]) {
  test(`${path} renders the API payload and opens its real ID`, async ({ page }) => {
    if (path === "/") {
      await page.route("**/api/listings", route => route.fulfill({ json: [listing] }));
    } else {
      await page.route("**/api/listings/makes", route => route.fulfill({ json: ["API Motors"] }));
      await page.route("**/api/listings/page*", route => route.fulfill({ json: paged([listing]) }));
    }

    await page.route("**/api/listings/101", route => route.fulfill({ json: listing }));
    await gotoApp(page, path, ".listing-card");
    await expect(page.locator(".listing-card")).toHaveCount(1);
    await expect(page.locator(".make-label")).toHaveText("API Motors");
    await expect(page.locator(".price")).toContainText("EUR");
    await expect(page.locator(".badge-new")).toHaveCount(0);
    await page.locator(".listing-card").click();
    await expect(page).toHaveURL(/\/listings\/101$/);
    await expect(page.locator(".details-shell .model-heading")).toHaveText("Live vehicle");
    await expect(page.locator(".quick-facts")).toContainText("Hybrid");
    await expect(page.locator(".quick-facts")).toContainText("Manual");
    await expect(page.locator(".details-image .image-placeholder")).toBeVisible();
  });

  test(`${path} distinguishes loading from an empty API result`, async ({ page }) => {
    let release!: () => void;
    const gate = new Promise<void>(resolve => { release = resolve; });
    if (path === "/") {
      await page.route("**/api/listings", async route => {
        await gate;
        await route.fulfill({ json: [] });
      });
    } else {
      await page.route("**/api/listings/makes", route => route.fulfill({ json: [] }));
      await page.route("**/api/listings/page*", async route => {
        await gate;
        await route.fulfill({ json: paged([]) });
      });
    }

    try {
      await page.goto(absoluteUrl(path));
      await expect(page.locator(".skeleton-card")).toHaveCount(4);
      await expect(page.locator('[data-lacuna-loading="true"]')).toHaveCount(1);
      await expect(page.locator(".browsing-feedback")).toHaveCount(0);
      release();
      await expect(page.locator(".browsing-feedback")).toContainText(/no results/i);
      await expect(page.locator(".listing-card, .skeleton-card")).toHaveCount(0);
    } finally {
      release();
    }
  });

  test(`${path} retries a failed API request without seed fallback`, async ({ page }) => {
    let attempts = 0;
    if (path === "/") {
      await page.route("**/api/listings", route => {
        attempts++;
        return route.fulfill(attempts === 1 ? { status: 503, json: {} } : { json: [listing] });
      });
    } else {
      await page.route("**/api/listings/makes", route => route.fulfill({ json: ["API Motors"] }));
      await page.route("**/api/listings/page*", route => {
        attempts++;
        return route.fulfill(attempts === 1 ? { status: 503, json: {} } : { json: paged([listing]) });
      });
    }

    await page.goto(absoluteUrl(path));
    await expect(page.getByRole("alert")).toBeVisible();
    await expect(page.locator(".listing-card")).toHaveCount(0);
    await page.getByRole("button", { name: /try again/i }).click();
    await expect(page.locator(".make-label")).toHaveText("API Motors");
    expect(attempts).toBe(2);
  });

  test(`${path} treats a malformed API payload as an error`, async ({ page }) => {
    if (path === "/") {
      await page.route("**/api/listings", route => route.fulfill({ json: null }));
    } else {
      await page.route("**/api/listings/makes", route => route.fulfill({ json: ["API Motors"] }));
      await page.route("**/api/listings/page*", route => route.fulfill({ json: null }));
    }

    await page.goto(absoluteUrl(path));
    await expect(page.getByRole("alert")).toBeVisible();
    await expect(page.locator(".listing-card")).toHaveCount(0);
  });
}

test("details 404 is distinct from an API error", async ({ page }) => {
  await page.route("**/api/listings/101", route => route.fulfill({ status: 404 }));
  await page.goto(absoluteUrl("/listings/101"));
  await expect(page.locator(".not-found")).toBeVisible();
  await expect(page.getByRole("alert")).toHaveCount(0);
  await expect(page.locator(".details-shell")).toHaveCount(0);
});

test("details retries a failed request and clears its error", async ({ page }) => {
  let attempts = 0;
  await page.route("**/api/listings/101", route => {
    attempts++;
    return route.fulfill(attempts === 1 ? { status: 502, json: {} } : { json: listing });
  });
  await page.goto(absoluteUrl("/listings/101"));
  await expect(page.getByRole("alert")).toBeVisible();
  await expect(page.locator(".not-found")).toHaveCount(0);
  await page.getByRole("button", { name: /try again/i }).click();
  await expect(page.locator(".model-heading")).toHaveText("Live vehicle");
  await expect(page.getByRole("alert")).toHaveCount(0);
});

test("details reloads on parameter changes and ignores a cancelled response", async ({ page }) => {
  let release!: () => void;
  let started!: () => void;
  let finished!: () => void;
  const gate = new Promise<void>(resolve => { release = resolve; });
  const requested = new Promise<void>(resolve => { started = resolve; });
  const completed = new Promise<void>(resolve => { finished = resolve; });
  await page.route("**/api/listings/101", route => route.fulfill({ json: listing }));
  await page.route("**/api/listings/202", async route => {
    started();
    await gate;
    try {
      await route.fulfill({ json: { ...listing, id: 202, model: "Stale response" } });
    } finally {
      finished();
    }
  });
  await page.route("**/api/listings/303", route => route.fulfill({
    json: { ...listing, id: 303, model: "Current vehicle" },
  }));
  try {
    await gotoApp(page, "/listings/101", ".details-shell");
    await spaNavigate(page, "/listings/202");
    await requested;
    await expect(page.locator(".details-page")).toHaveAttribute("aria-busy", "true");
    await expect(page.locator(".details-page")).toHaveAttribute("data-lacuna-loading", "true");
    await expect(page.locator(".details-shell")).toHaveCount(0);
    await spaNavigate(page, "/listings/303");
    await expect(page.locator(".model-heading")).toHaveText("Current vehicle");
    release();
    await completed;
    await expect(page.locator(".model-heading")).toHaveText("Current vehicle");
  } finally {
    release();
  }
});

test("Web forwards the live API contract and keeps missing details as JSON 404", async ({ request }) => {
  const isOrderedByYear = (
    cards: Array<{ year: number; id: number }>,
    direction: "asc" | "desc"
  ): boolean => cards.every((card, index) => {
    if (index === 0) {
      return true;
    }

    const previous = cards[index - 1];
    if (direction === "desc") {
      return previous.year > card.year || (previous.year === card.year && previous.id < card.id);
    }

    return previous.year < card.year || (previous.year === card.year && previous.id < card.id);
  });

  const response = await request.get(absoluteUrl("/api/listings"));
  expect(response.status()).toBe(200);
  const cards = await response.json();
  expect(cards.length).toBe(8);
  const expectedOrderedIds = cards.map((card: { id: number }) => card.id);

  const teslaFiltered = await request.get(absoluteUrl("/api/listings?make=tesla"));
  expect(teslaFiltered.status()).toBe(200);
  const teslaCards = await teslaFiltered.json();
  expect(teslaCards.length).toBeGreaterThan(0);
  expect(teslaCards.length).toBeLessThan(cards.length);
  expect(teslaCards.every((card: { make: string }) => card.make.toLowerCase() === "tesla")).toBe(true);

  const whitespaceMake = await request.get(absoluteUrl("/api/listings?make=%20%20%20"));
  expect(whitespaceMake.status()).toBe(200);
  expect((await whitespaceMake.json()).length).toBe(cards.length);

  const noMatches = await request.get(absoluteUrl("/api/listings?make=NotARealMake"));
  expect(noMatches.status()).toBe(200);
  expect((await noMatches.json())).toEqual([]);

  const yearDesc = await request.get(absoluteUrl("/api/listings?sort=year-desc"));
  expect(yearDesc.status()).toBe(200);
  const yearDescCards = await yearDesc.json();
  expect(yearDescCards.length).toBe(cards.length);
  expect(isOrderedByYear(yearDescCards, "desc")).toBe(true);

  const yearAsc = await request.get(absoluteUrl("/api/listings?sort=year-asc"));
  expect(yearAsc.status()).toBe(200);
  const yearAscCards = await yearAsc.json();
  expect(yearAscCards.length).toBe(cards.length);
  expect(isOrderedByYear(yearAscCards, "asc")).toBe(true);

  const unsupportedSort = await request.get(absoluteUrl("/api/listings?sort=newest"));
  expect(unsupportedSort.status()).toBe(200);
  expect((await unsupportedSort.json()).map((card: { id: number }) => card.id)).toEqual(
    cards.map((card: { id: number }) => card.id)
  );

  const blankSort = await request.get(absoluteUrl("/api/listings?sort=%20%20%20"));
  expect(blankSort.status()).toBe(200);
  expect((await blankSort.json()).map((card: { id: number }) => card.id)).toEqual(
    cards.map((card: { id: number }) => card.id)
  );

  const makeAndSort = await request.get(absoluteUrl("/api/listings?make=tesla&sort=year-desc"));
  expect(makeAndSort.status()).toBe(200);
  const makeAndSortCards = await makeAndSort.json();
  expect(makeAndSortCards.length).toBeGreaterThan(0);
  expect(makeAndSortCards.every((card: { make: string }) => card.make.toLowerCase() === "tesla")).toBe(true);
  expect(isOrderedByYear(makeAndSortCards, "desc")).toBe(true);

  const pagedPageOne = await request.get(absoluteUrl("/api/listings/page?page=1&pageSize=3"));
  expect(pagedPageOne.status()).toBe(200);
  const pagedPageOnePayload = await pagedPageOne.json();
  expect(pagedPageOnePayload.page).toBe(1);
  expect(pagedPageOnePayload.pageSize).toBe(3);
  expect(pagedPageOnePayload.totalCount).toBe(cards.length);
  expect(pagedPageOnePayload.totalPages).toBe(Math.ceil(cards.length / 3));
  expect(pagedPageOnePayload.items.map((card: { id: number }) => card.id)).toEqual(expectedOrderedIds.slice(0, 3));

  const pagedPageTwo = await request.get(absoluteUrl("/api/listings/page?page=2&pageSize=3"));
  expect(pagedPageTwo.status()).toBe(200);
  const pagedPageTwoPayload = await pagedPageTwo.json();
  expect(pagedPageTwoPayload.page).toBe(2);
  expect(pagedPageTwoPayload.pageSize).toBe(3);
  expect(pagedPageTwoPayload.totalCount).toBe(cards.length);
  expect(pagedPageTwoPayload.totalPages).toBe(Math.ceil(cards.length / 3));
  expect(pagedPageTwoPayload.items.map((card: { id: number }) => card.id)).toEqual(expectedOrderedIds.slice(3, 6));

  const pagedMakeAndSort = await request.get(absoluteUrl("/api/listings/page?make=tesla&sort=year-desc&page=1&pageSize=8"));
  expect(pagedMakeAndSort.status()).toBe(200);
  const pagedMakeAndSortPayload = await pagedMakeAndSort.json();
  expect(pagedMakeAndSortPayload.totalCount).toBeGreaterThan(0);
  expect(pagedMakeAndSortPayload.items.every((card: { make: string }) => card.make.toLowerCase() === "tesla")).toBe(true);
  expect(isOrderedByYear(pagedMakeAndSortPayload.items, "desc")).toBe(true);

  const beyondLastPage = await request.get(absoluteUrl("/api/listings/page?page=2147483647&pageSize=8"));
  expect(beyondLastPage.status()).toBe(200);
  const beyondLastPagePayload = await beyondLastPage.json();
  expect(beyondLastPagePayload.page).toBe(2147483647);
  expect(beyondLastPagePayload.pageSize).toBe(8);
  expect(beyondLastPagePayload.totalCount).toBe(cards.length);
  expect(beyondLastPagePayload.totalPages).toBe(1);
  expect(beyondLastPagePayload.items).toEqual([]);

  const invalidPage = await request.get(absoluteUrl("/api/listings/page?page=abc&pageSize=8"));
  expect(invalidPage.status()).toBe(400);
  expect(invalidPage.headers()["content-type"]).toContain("application/problem+json");
  const invalidPageProblem = await invalidPage.json();
  expect(invalidPageProblem.title).toBe("Invalid pagination parameters.");
  expect(typeof invalidPageProblem.traceId).toBe("string");
  expect(invalidPageProblem.traceId.length).toBeGreaterThan(0);

  const invalidPageSize = await request.get(absoluteUrl("/api/listings/page?page=1&pageSize=25"));
  expect(invalidPageSize.status()).toBe(400);
  expect(invalidPageSize.headers()["content-type"]).toContain("application/problem+json");
  const invalidPageSizeProblem = await invalidPageSize.json();
  expect(invalidPageSizeProblem.title).toBe("Invalid pagination parameters.");
  expect(typeof invalidPageSizeProblem.traceId).toBe("string");
  expect(invalidPageSizeProblem.traceId.length).toBeGreaterThan(0);

  const makesResponse = await request.get(absoluteUrl("/api/listings/makes"));
  expect(makesResponse.status()).toBe(200);
  const makes = await makesResponse.json();
  expect(Array.isArray(makes)).toBe(true);
  expect(makes.length).toBeGreaterThan(0);
  expect(makes.some((make: string) => make.toLowerCase() === "volkswagen")).toBe(true);

  const detail = await request.get(absoluteUrl(`/api/listings/${cards[0].id}`));
  expect(detail.status()).toBe(200);
  expect(await detail.json()).toEqual(cards[0]);
  const missing = await request.get(absoluteUrl("/api/listings/2147483647"));
  expect(missing.status()).toBe(404);
  expect(missing.headers()["content-type"]).toContain("application/problem+json");
  expect((await missing.json()).status).toBe(404);
});
