import { test, expect } from "@playwright/test";
import { APP_BASE_URL } from "../helpers/app";

// The runner's injectable option fixtures are the authoritative view of the effective
// configuration. Playwright performs no TypeScript checking on test files, and options placed
// outside the `use` block are silently ignored, so asserting here is the only way to prove that
// the intended values actually reach the runner.
test.describe("Effective runner configuration", () => {
  test("timeouts, locale and baseURL reach the runner", async ({ actionTimeout, navigationTimeout, locale, baseURL, viewport, browserName, headless }) => {
    console.log(
      "runner-config:",
      JSON.stringify(
        {
          actionTimeout,
          navigationTimeout,
          locale,
          baseURL,
          viewport,
          browserName,
          headless,
        },
      ),
    );
    expect(actionTimeout).toBe(15_000);
    expect(navigationTimeout).toBe(30_000);
    expect(locale).toBe("en-US");
    // The expected origin is derived from E2E_BASE_URL (same fallback as the config), so a
    // legitimate origin override keeps this assertion meaningful.
    expect(baseURL).toBe(APP_BASE_URL);
  });
});
