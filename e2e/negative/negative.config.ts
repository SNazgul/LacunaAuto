import { defineConfig } from "@playwright/test";

// Fault-injection suite: it deliberately breaks the app (blocks every vehicle
// image request) and re-runs the suite's image assertions. Those assertions
// MUST fail, which is the negative proof that image loading is really checked.
// Run with: npm run test:negative   (expected exit code: non-zero)
export default defineConfig({
  testDir: ".",
  outputDir: "../e2e-artifacts-negative",
  timeout: 30_000,
  retries: 0,
  reporter: [["list"]],
  use: {
    browserName: "chromium",
    headless: true,
    locale: "en-US",
    baseURL: process.env.E2E_BASE_URL ?? "http://localhost:5032/",
  },
});
