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

for (const path of ["/", "/listings"]) {
  test(`${path} renders the API payload and opens its real ID`, async ({ page }) => {
    await page.route("**/api/listings", route => route.fulfill({ json: [listing] }));
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
    await page.route("**/api/listings", async route => {
      await gate;
      await route.fulfill({ json: [] });
    });
    try {
      await page.goto(absoluteUrl(path));
      await expect(page.locator(".skeleton-card")).toHaveCount(4);
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
    await page.route("**/api/listings", route => {
      attempts++;
      return route.fulfill(attempts === 1 ? { status: 503, json: {} } : { json: [listing] });
    });
    await page.goto(absoluteUrl(path));
    await expect(page.getByRole("alert")).toBeVisible();
    await expect(page.locator(".listing-card")).toHaveCount(0);
    await page.getByRole("button", { name: /try again/i }).click();
    await expect(page.locator(".make-label")).toHaveText("API Motors");
    expect(attempts).toBe(2);
  });

  test(`${path} treats a malformed API payload as an error`, async ({ page }) => {
    await page.route("**/api/listings", route => route.fulfill({ json: null }));
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
  const response = await request.get(absoluteUrl("/api/listings"));
  expect(response.status()).toBe(200);
  const cards = await response.json();
  expect(cards.length).toBe(8);
  const detail = await request.get(absoluteUrl(`/api/listings/${cards[0].id}`));
  expect(detail.status()).toBe(200);
  expect(await detail.json()).toEqual(cards[0]);
  const missing = await request.get(absoluteUrl("/api/listings/2147483647"));
  expect(missing.status()).toBe(404);
  expect(missing.headers()["content-type"]).toContain("application/problem+json");
  expect((await missing.json()).status).toBe(404);
});
