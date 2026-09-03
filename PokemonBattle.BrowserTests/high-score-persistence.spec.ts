import { expect, test, type Page } from "@playwright/test";

const starterMoves = [
  { pokemon: /이상해씨, 도감 번호 1/, move: /솔라빔/ },
  { pokemon: /파이리, 도감 번호 4/, move: /플레어드라이브/ },
  { pokemon: /꼬부기, 도감 번호 7/, move: /웨이브태클/ },
];

async function fillLogin(page: Page, username: string, password: string) {
  await page.goto("/login");
  await page.waitForTimeout(300);
  const usernameInput = page.locator("#login-username");
  const passwordInput = page.locator("#login-password");
  await usernameInput.pressSequentially(username);
  await passwordInput.pressSequentially(password);
  await expect(usernameInput).toHaveValue(username);
  await expect(passwordInput).toHaveValue(password);
  await page.getByRole("button", { name: "로그인" }).click();
  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator("main.start-page")).toContainText(
    `${username} 님, 다시 전장으로 돌아오셨습니다.`,
  );
}

async function register(page: Page, username: string, password: string) {
  await page.goto("/register");
  await page.waitForTimeout(300);
  const usernameInput = page.locator("#register-username");
  const passwordInput = page.locator("#register-password");
  await usernameInput.pressSequentially(username);
  await passwordInput.pressSequentially(password);
  await expect(usernameInput).toHaveValue(username);
  await expect(passwordInput).toHaveValue(password);
  await page.getByRole("button", { name: "가입하기" }).click();
  await expect(page).toHaveURL(/\/login$/);
}

async function configureStarter(
  page: Page,
  pokemonName: RegExp,
  moveName: RegExp,
  replaceMove?: RegExp,
) {
  await page.getByRole("button", { name: pokemonName }).click();

  if (replaceMove) {
    await page.getByRole("button", { name: replaceMove }).click();
    await page.locator(".info-panel .info-select-btn").click();
  }

  await page.getByRole("button", { name: moveName }).click();
  await page.locator(".info-panel .info-select-btn").click();
  await page.getByRole("button", { name: "이 구성으로 팀에 추가" }).click();
}

async function enterBattle(page: Page) {
  await expect(page).toHaveURL(/\/(starter|preview)$/);

  if (page.url().endsWith("/starter")) {
    for (const starter of starterMoves) {
      const card = page.getByRole("button", { name: starter.pokemon });
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

  await expect(page).toHaveURL(/\/preview$/);
  await page.getByRole("button", { name: /이 상대 팀.*과 배틀 준비하기/ }).click();
  await expect(page).toHaveURL(/\/(select|continue)$/);

  if (page.url().endsWith("/select")) {
    const startTeam = page.getByRole("button", { name: "이 팀으로 배틀 시작" });
    for (const starter of starterMoves) {
      await configureStarter(page, starter.pokemon, starter.move);
    }
    await expect(startTeam).toBeEnabled();
    await startTeam.click();
  } else {
    await page.getByRole("button", { name: "현재 팀 유지하고 배틀 시작" }).click();
  }

  await expect(page).toHaveURL(/\/battle$/);
}

async function waitForBattleState(page: Page): Promise<"result" | "attack" | "switch"> {
  for (let attempt = 0; attempt < 225; attempt += 1) {
    if (await page.locator("main.result-page").count()) return "result";
    if (await page.locator("button.action-attack:not(:disabled)").count()) return "attack";
    if (
      (await page.getByRole("heading", { name: "내보낼 포켓몬을 선택하세요" }).count()) &&
      (await page.locator("button.team-choice-button:not(:disabled)").count())
    ) {
      return "switch";
    }
    await page.waitForTimeout(200);
  }

  throw new Error("Battle did not become ready within 45 seconds");
}

async function playBattle(page: Page) {
  const speedToggle = page.getByRole("button", { name: "배틀 메시지 재생 속도 변경" });
  await page.waitForTimeout(1_000);
  for (let attempt = 0; attempt < 5; attempt += 1) {
    if ((await speedToggle.innerText()).includes("x5")) break;
    await speedToggle.click();
    await page.waitForTimeout(500);
  }
  await expect(speedToggle).toContainText("x5");

  for (let turn = 0; turn < 60; turn += 1) {
    const state = await waitForBattleState(page);
    if (state === "result") return;

    if (state === "switch") {
      const availableMember = page.locator("button.team-choice-button:not(:disabled)").first();
      await expect(availableMember).toBeEnabled();
      await availableMember.scrollIntoViewIfNeeded();
      await availableMember.click();
      await expect
        .poll(
          async () => {
            if (await page.locator("main.result-page").count()) return "result";
            if (
              !(await page.getByRole("heading", { name: "내보낼 포켓몬을 선택하세요" }).count()) &&
              (await page.locator("button.action-attack:not(:disabled)").count())
            ) {
              return "attack";
            }
            return "waiting";
          },
          { timeout: 45_000 },
        )
        .toMatch(/^(result|attack)$/);
      continue;
    }

    await page.locator("button.action-attack:not(:disabled)").click({ force: true });
    await expect
      .poll(
        async () => {
          if (await page.locator("main.result-page").count()) return "result";
          if (
            (await page.getByRole("heading", { name: "내보낼 포켓몬을 선택하세요" }).count()) &&
            (await page.locator("button.team-choice-button:not(:disabled)").count())
          ) {
            return "switch";
          }
          if (await page.locator(".move-name-button:not(:disabled)").count()) return "move";
          return "waiting";
        },
        { timeout: 45_000 },
      )
      .toMatch(/^(result|switch|move)$/);
    if (await page.locator("main.result-page").count()) return;
    if (
      (await page.getByRole("heading", { name: "내보낼 포켓몬을 선택하세요" }).count()) &&
      (await page.locator("button.team-choice-button:not(:disabled)").count())
    ) {
      continue;
    }

    const move = page.locator(".move-name-button:not(:disabled)").first();
    await move.click({ force: true });
    await page.locator(".move-confirm:not(:disabled)").click({ force: true });
  }

  throw new Error("Battle did not reach the result screen within 60 turns");
}

function readHighScore(page: Page) {
  return page
    .locator("main.result-page, main.start-page")
    .innerText()
    .then((text) => {
      const match = text.match(/최고 기록\s*:?\s*(\d+)/);
      if (!match) throw new Error(`Could not find high score in: ${text}`);
      return Number(match[1]);
    });
}

test("persists a personal high score after signing in to a fresh browser session", async ({
  page,
  browser,
}) => {
  test.setTimeout(240_000);

  const suffix = `${Date.now()}${test.info().workerIndex}${test.info().project.name}`;
  const username = `score${suffix.replace(/[^a-zA-Z0-9]/g, "")}`;
  const password = "high-score-persistence-test";

  await register(page, username, password);
  await fillLogin(page, username, password);
  await expect(page.locator("main.start-page")).toContainText(/최고 기록\s*0/);

  let wonSetupRound = false;
  for (let attempt = 0; attempt < 3 && !wonSetupRound; attempt += 1) {
    if (attempt > 0) {
      await page.getByRole("button", { name: "처음부터 다시 시작" }).click();
      await expect(page).toHaveURL(/\/$/);
    }

    await page.getByRole("button", { name: "시작하기" }).click();
    await enterBattle(page);
    await playBattle(page);
    wonSetupRound = await page.getByRole("heading", { name: "승리!" }).count() > 0;
  }
  expect(wonSetupRound).toBe(true);

  const highScore = await readHighScore(page);
  expect(highScore).toBeGreaterThan(0);

  await page.locator("details.result-secondary-actions > summary").click();
  await page.getByRole("button", { name: "새 런 시작" }).click();
  await expect(page).toHaveURL(/\/starter$/);
  await page.getByRole("button", { name: "취소" }).click();
  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator("main.start-page")).toContainText(new RegExp(`최고 기록\\s*${highScore}`));

  const freshContext = await browser.newContext();
  try {
    const freshPage = await freshContext.newPage();
    await fillLogin(freshPage, username, password);
    await expect(freshPage.locator("main.start-page")).toContainText(
      new RegExp(`최고 기록\\s*${highScore}`),
    );
  } finally {
    await freshContext.close();
  }
});