import { test, expect } from "@playwright/test";
import { absoluteUrl } from "../helpers/app";

test.describe("PWA manifest and static assets", () => {
  test("manifest is served and valid", async ({ request }) => {
    const response = await request.get(absoluteUrl("/manifest.webmanifest"));
    expect(response.status()).toBe(200);

    const manifest = await response.json();
    expect(manifest.name).toBe("LacunaAuto");
    expect(manifest.short_name).toBe("Lacuna");
    expect(manifest.start_url).toBe("/");
    expect(manifest.display).toBe("standalone");

    const sizes = (manifest.icons as Array<{ sizes: string }>).map((icon) => icon.sizes);
    expect(sizes).toContain("192x192");
    expect(sizes).toContain("512x512");
  });

  test("PWA icons are served", async ({ request }) => {
    for (const icon of ["/icon-192.png", "/icon-512.png"]) {
      const response = await request.get(absoluteUrl(icon));
      expect(response.status()).toBe(200);
    }
  });

  test("root page bootstraps Blazor WASM", async ({ request }) => {
    const response = await request.get(absoluteUrl("/"));
    expect(response.status()).toBe(200);

    const html = await response.text();
    expect(html.toLowerCase()).toContain("blazor");
  });
});
