import { expect, test, type Locator, type Page } from "@playwright/test";

const starterNames = [/이상해씨, 도감 번호 1/, /파이리, 도감 번호 4/, /꼬부기, 도감 번호 7/];

async function tabTo(page: Page, target: Locator, maxTabs = 180) {
  await expect(target).toHaveCount(1);

  for (let attempt = 0; attempt < maxTabs; attempt += 1) {
    if (await target.evaluate((element) => element === document.activeElement)) {
      return;
    }
    await page.keyboard.press("Tab");
  }

  throw new Error(`Could not reach ${await target.getAttribute("aria-label")}`);
}

async function pressEnter(page: Page, target: Locator) {
  await tabTo(page, target);
  await page.keyboard.press("Enter");
}

async function assertNoHorizontalOverflow(page: Page) {
  await expect
    .poll(async () => page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth))
    .toBe(true);
  await expect
    .poll(async () => page.evaluate(() => document.body.scrollWidth <= document.body.clientWidth))
    .toBe(true);
}

async function fillWithKeyboard(page: Page, username: string, password: string, route: "/register" | "/login") {
  await page.goto(route);
  await page.waitForTimeout(300);

  const usernameInput = page.locator(`#${route === "/register" ? "register" : "login"}-username`);
  const passwordInput = page.locator(`#${route === "/register" ? "register" : "login"}-password`);
  await tabTo(page, usernameInput);
  await page.keyboard.type(username);
  await page.keyboard.press("Tab");
  await page.keyboard.type(password);
  await page.keyboard.press("Tab");
  await page.keyboard.press("Enter");
}

async function configureStarter(page: Page, name: RegExp) {
  const card = page.getByRole("button", { name });
  await pressEnter(page, card);

  const move = page.locator(".move-tile").first();
  await tabTo(page, move);
  await page.keyboard.press("Enter");

  const moveSelect = page.locator(".info-panel .info-select-btn").first();
  await tabTo(page, moveSelect);
  await page.keyboard.press("Space");
  await expect(page.locator(".move-tile").first()).toHaveAttribute("aria-pressed", "true");

  const addToTeam = page.getByRole("button", { name: "이 구성으로 팀에 추가" });
  await expect(addToTeam).toBeEnabled();
  await pressEnter(page, addToTeam);
}

async function configureInitialStarters(page: Page) {
  for (const starterName of starterNames) {
    const card = page.getByRole("button", { name: starterName });
    await card.click();
    await expect(card).toHaveAttribute("aria-pressed", "true");
  }

  const confirm = page.getByRole("button", { name: "이 3마리로 시작하기" });
  await expect(confirm).toBeEnabled();
  await confirm.click();
  await expect
    .poll(
      async () => {
        if (page.url().endsWith("/preview")) return "preview";
        const error = page.getByRole("alert", { name: "저장 중 문제가 발생했습니다. 다시 시도해주세요" });
        if (await error.count()) return `error:${await error.innerText()}`;
        return page.url();
      },
      { timeout: 15_000 },
    )
    .toBe("preview");
}

async function waitForBattleReadyOrResult(page: Page) {
  await expect
    .poll(
      async () => {
        if (page.url().endsWith("/result") || await page.locator("main.result-page").count()) return "result";

        const attack = page.getByRole("button", { name: /공격/ });
        if (await attack.count() && await attack.isEnabled()) return "battle";

        const forcedSwitch = page.getByRole("heading", { name: "내보낼 포켓몬을 선택하세요" });
        if (await forcedSwitch.count()) return "forced-switch";

        return "busy";
      },
      { timeout: 45_000 },
    )
    .toMatch(/^(battle|forced-switch|result)$/);

  if (page.url().endsWith("/result") || await page.locator("main.result-page").count()) return "result";
  if (await page.getByRole("button", { name: /공격/ }).count()) return "battle";
  return "forced-switch";
}

async function issueKeyboardBattleCommand(page: Page) {
  if (page.url().endsWith("/result") || await page.locator("main.result-page").count()) {
    return;
  }

  const forcedSwitch = page.getByRole("heading", { name: "내보낼 포켓몬을 선택하세요" });
  const availableMember = page.locator(".move-button:not(:disabled)").first();
  if ((await forcedSwitch.count()) && (await forcedSwitch.isVisible()) && (await availableMember.count())) {
    await availableMember.focus();
    await page.keyboard.press("Enter");
    return;
  }

  const attack = page.getByRole("button", { name: /공격/ });
  await pressEnter(page, attack);

  const availableMove = page.locator(".move-name-button:not(:disabled)").first();
  await tabTo(page, availableMove);
  await page.keyboard.press("Enter");
  await expect(availableMove).toHaveAttribute("aria-pressed", "true");

  const confirmMove = page.getByRole("button", { name: /시전 확정/ });
  await expect(confirmMove).toBeEnabled();
  await pressEnter(page, confirmMove);
}

test("completes the game flow with keyboard controls at mobile and desktop widths", async ({ page }) => {
  const uniqueUsername = `keyboard${Date.now()}${test.info().workerIndex}`;
  const password = "keyboard-only-test";

  await fillWithKeyboard(page, uniqueUsername, password, "/register");
  await expect(page).toHaveURL(/\/login$/);

  await fillWithKeyboard(page, uniqueUsername, password, "/login");
  await expect(page).toHaveURL(/\/$/);
  await assertNoHorizontalOverflow(page);

  const start = page.getByRole("button", { name: "시작하기" });
  await pressEnter(page, start);
  await expect(page).toHaveURL(/\/(starter|preview)$/);
  if (page.url().endsWith("/starter")) {
    await expect(page.getByRole("heading", { name: "스타터 선택" })).toBeVisible();
    await configureInitialStarters(page);
  }
  await expect(page).toHaveURL(/\/preview$/);
  await assertNoHorizontalOverflow(page);

  const firstOpponent = page.locator(".enemy-card-toggle").first();
  await tabTo(page, firstOpponent);
  await expect(firstOpponent).toHaveAttribute("aria-expanded", "false");
  await page.keyboard.press("Enter");
  await expect(firstOpponent).toHaveAttribute("aria-expanded", "true");
  await expect(page.locator(".enemy-card.is-selected .enemy-detail")).toBeVisible();
  await assertNoHorizontalOverflow(page);

  const confirmOpponents = page.getByRole("button", { name: /이 상대 팀.*과 배틀 준비하기/ });
  await pressEnter(page, confirmOpponents);
  await expect(page).toHaveURL(/\/(select|continue)$/);

  if (page.url().endsWith("/continue")) {
    const keepTeam = page.getByRole("button", { name: "현재 팀 유지하고 배틀 시작" });
    await pressEnter(page, keepTeam);
    await expect(page).toHaveURL(/\/battle$/);
  } else {
    const startTeam = page.getByRole("button", { name: "이 팀으로 배틀 시작" });
    await expect(startTeam).toBeDisabled();

    for (const starterName of starterNames) {
      await configureStarter(page, starterName);
    }

    const selectedCards = page.locator(".dex-entry-button.is-selected[aria-pressed='true']");
    await expect(selectedCards).toHaveCount(starterNames.length);
    await expect(startTeam).toBeEnabled();
    await assertNoHorizontalOverflow(page);
    await pressEnter(page, startTeam);
    await expect(page).toHaveURL(/\/battle$/);
  }

  const speedToggle = page.getByRole("button", { name: "배틀 메시지 재생 속도 변경" });
  await pressEnter(page, speedToggle);
  await page.keyboard.press("Enter");
  await expect(speedToggle).toContainText("x2.4");

  const battleLog = page.getByRole("log", { name: "전투 로그" });
  await expect(battleLog).toHaveAttribute("aria-live", "polite");
  const initialLog = await battleLog.innerText();
  await issueKeyboardBattleCommand(page);
  await expect
    .poll(async () => {
      if (!(await page.locator('[role="log"]').count())) return null;
      const currentLog = await battleLog.innerText();
      return currentLog === initialLog ? null : currentLog;
    })
    .not.toBeNull();
  await assertNoHorizontalOverflow(page);

  let outcome = await waitForBattleReadyOrResult(page);
  for (let turn = 0; outcome !== "result" && turn < 40; turn += 1) {
    await issueKeyboardBattleCommand(page);
    outcome = await waitForBattleReadyOrResult(page);
  }
  expect(outcome).toBe("result");
  await assertNoHorizontalOverflow(page);

  const nextRound = page.getByRole("button", { name: "다음 상대와 계속하기" });
  const restart = page.getByRole("button", { name: "처음부터 다시 시작" });
  if (await nextRound.count()) {
    await expect(page.getByRole("button", { name: "이 포켓몬 데려가기" })).toBeDisabled();
    await pressEnter(page, nextRound);
    await expect(page).toHaveURL(/\/preview$/);
  } else {
    await expect(restart).toHaveCount(1);
    await pressEnter(page, restart);
    await expect(page).toHaveURL(/\/$/);
  }
  await assertNoHorizontalOverflow(page);
});