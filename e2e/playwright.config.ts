import { defineConfig } from "@playwright/test";

// Base URL of the running LacunaAuto Blazor Web App. Override with E2E_BASE_URL
// when the app is started on another port (e.g. by the VS Code debugger).
const _appBaseUrl = process.env.E2E_BASE_URL ?? "http://localhost:5032/";

export default defineConfig({
  testDir: "./specs",
  outputDir: "./e2e-artifacts",
  timeout: 60_000,
  workers: 2,
  retries: 1,
  reporter: [
    ["list"],
    ["json", { outputFile: "e2e-report.json" }],
  ],
  use: {
    browserName: "chromium",
    headless: true,
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    // The app detects its default language from navigator.language on first run,
    // so the browser locale must be pinned for the suite to be host-independent.
    locale: "en-US",
    baseURL: _appBaseUrl,
    screenshot: "off",
    video: "off",
    trace: "off",
  },
});




