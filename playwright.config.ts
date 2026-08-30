import { defineConfig, devices } from "@playwright/test";

const port = 3001;

export default defineConfig({
  testDir: "./PokemonBattle.BrowserTests",
  timeout: 120_000,
  expect: {
    timeout: 15_000,
  },
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? `http://127.0.0.1:${port}`,
    trace: "on-first-retry",
  },
  projects: [
    {
      name: "mobile",
      use: { ...devices["Desktop Chrome"], viewport: { width: 320, height: 800 } },
    },
    {
      name: "desktop",
      use: { ...devices["Desktop Chrome"], viewport: { width: 1280, height: 900 } },
    },
  ],
  webServer: process.env.PLAYWRIGHT_BASE_URL
    ? undefined
    : {
        command: `dotnet run --project PokemonBattle/PokemonBattle.csproj --no-launch-profile --urls http://127.0.0.1:${port}`,
        url: `http://127.0.0.1:${port}/healthz`,
        reuseExistingServer: false,
        timeout: 120_000,
      },
});