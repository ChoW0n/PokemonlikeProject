import { expect, test, type Page } from "@playwright/test";

const starterMoves = [
  { pokemon: /이상해씨, 도감 번호 1/, move: /솔라빔/ },
  { pokemon: /파이리, 도감 번호 4/, move: /플레어드라이브/ },
  { pokemon: /꼬부기, 도감 번호 7/, move: /웨이브태클/ },
];

async function register(page: Page, username: string, password: string) {
  await page.goto("/register");
  await page.waitForTimeout(1_000);
  const usernameInput = page.locator("#register-username");
  const passwordInput = page.locator("#register-password");
  await expect(usernameInput).toBeVisible();
  await expect(passwordInput).toBeVisible();
  await usernameInput.pressSequentially(username);
  await passwordInput.pressSequentially(password);
  await expect(usernameInput).toHaveValue(username);
  await expect(passwordInput).toHaveValue(password);
  await page.getByRole("button", { name: "가입하기" }).click();
  await expect(page).toHaveURL(/\/login$/);
}

async function login(page: Page, username: string, password: string) {
  await page.goto("/login");
  await page.waitForTimeout(1_000);
  const usernameInput = page.locator("#login-username");
  const passwordInput = page.locator("#login-password");
  await expect(usernameInput).toBeVisible();
  await expect(passwordInput).toBeVisible();
  await usernameInput.pressSequentially(username);
  await passwordInput.pressSequentially(password);
  await expect(usernameInput).toHaveValue(username);
  await expect(passwordInput).toHaveValue(password);
  await page.getByRole("button", { name: "로그인" }).click();
  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator("main.start-page")).toContainText(`${username} 님`);
}

async function registerAndLogin(page: Page, prefix: string) {
  const suffix = `${Date.now()}${test.info().workerIndex}${test.info().project.name}`;
  const username = `${prefix}${suffix.replace(/[^a-zA-Z0-9]/g, "")}`;
  const password = "wiki-mastery-browser-test";
  await register(page, username, password);
  await login(page, username, password);
}

async function configureStarter(
  page: Page,
  pokemonName: RegExp,
  moveName: RegExp,
) {
  await page.getByRole("button", { name: pokemonName }).click();
  await page.getByRole("button", { name: moveName }).click();
  await page.locator(".info-panel .info-select-btn").click();
  await page.getByRole("button", { name: "이 구성으로 팀에 추가" }).click();
}

async function startFirstBattle(page: Page) {
  if (page.url().endsWith("/")) {
    await page.getByRole("button", { name: "시작하기" }).click();
    await expect(page).toHaveURL(/\/starter$/);
  } else {
    await expect(page).toHaveURL(/\/starter$/);
  }

  for (const starter of starterMoves) {
    await page.getByRole("button", { name: starter.pokemon }).click();
  }
  await page.getByRole("button", { name: "이 3마리로 시작하기" }).click();
  await expect(page).toHaveURL(/\/preview$/);
  await page.getByRole("button", { name: /이 상대 팀.*과 배틀 준비하기/ }).click();
  await expect(page).toHaveURL(/\/(select|continue)$/);

  if (page.url().endsWith("/continue")) {
    const declineCovenant = page.getByRole("button", { name: "이번에는 거절" });
    if (await declineCovenant.count()) {
      await declineCovenant.click();
    }
    await page.getByRole("button", { name: "현재 팀 유지하고 배틀 시작" }).click();
    await expect(page).toHaveURL(/\/battle$/);
    return;
  }

  for (const starter of starterMoves) {
    await configureStarter(page, starter.pokemon, starter.move);
  }

  const startTeam = page.getByRole("button", { name: "이 팀으로 배틀 시작" });
  await expect(startTeam).toBeEnabled();
  await startTeam.click();
  await expect(page).toHaveURL(/\/battle$/);
}

async function continueToNextBattle(page: Page) {
  await page.getByRole("button", { name: "다음 상대와 계속하기" }).click();
  await expect(page).toHaveURL(/\/preview$/);
  await page.getByRole("button", { name: /이 상대 팀.*과 배틀 준비하기/ }).click();
  await expect(page).toHaveURL(/\/continue$/);

  const declineCovenant = page.getByRole("button", { name: "이번에는 거절" });
  if (await declineCovenant.count()) {
    await declineCovenant.click();
  }

  await page.getByRole("button", { name: "현재 팀 유지하고 배틀 시작" }).click();
  await expect(page).toHaveURL(/\/battle$/);
}

async function waitForBattleState(page: Page): Promise<"result" | "battle" | "forced-switch"> {
  await expect
    .poll(
      async () => {
        if (await page.locator("main.result-page").count()) return "result";
        if (await page.locator("button.action-attack:not(:disabled)").count()) return "battle";
        if (
          (await page.getByRole("heading", { name: "내보낼 포켓몬을 선택하세요" }).count()) &&
          (await page.locator(".move-button:not(:disabled)").count())
        ) {
          return "forced-switch";
        }
        return "busy";
      },
      { timeout: 45_000 },
    )
    .toMatch(/^(result|battle|forced-switch)$/);

  if (await page.locator("main.result-page").count()) return "result";
  if (await page.locator("button.action-attack:not(:disabled)").count()) return "battle";
  return "forced-switch";
}

async function setBattleSpeed(page: Page) {
  const speedToggle = page.getByRole("button", { name: "배틀 메시지 재생 속도 변경" });
  await page.waitForTimeout(1_000);
  for (let attempt = 0; attempt < 12; attempt += 1) {
    if ((await speedToggle.innerText()).includes("x5")) return;
    await speedToggle.click();
    await page.waitForTimeout(500);
  }
  await expect(speedToggle).toContainText("x5");
}

async function playBattle(page: Page) {
  await setBattleSpeed(page);

  for (let turn = 0; turn < 60; turn += 1) {
    const state = await waitForBattleState(page);
    if (state === "result") return;

    if (state === "forced-switch") {
      const availableMember = page.locator("button.team-choice-button:not([disabled])").first();
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
              return "battle";
            }
            return "waiting";
          },
          { timeout: 45_000 },
        )
        .toMatch(/^(result|battle)$/);
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
            return "forced-switch";
          }
          if (await page.locator(".move-name-button:not(:disabled)").count()) return "move";
          return "waiting";
        },
        { timeout: 45_000 },
      )
      .toMatch(/^(result|forced-switch|move)$/);
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

async function winRound(page: Page, isFirstRound: boolean) {
  if (isFirstRound) {
    await startFirstBattle(page);
  } else {
    await continueToNextBattle(page);
  }

  for (let attempt = 0; attempt < 3; attempt += 1) {
    await playBattle(page);
    if (await page.getByRole("heading", { name: "승리!" }).count()) return;

    if (attempt < 2) {
      await page.getByRole("button", { name: "처음부터 다시 시작" }).click();
      await expect(page).toHaveURL(/\/starter$/);
      await startFirstBattle(page);
    }
  }

  throw new Error("Could not win a setup round for the browser regression test");
}

test("로그인된 사용자가 위키 목차를 클릭하면 해당 섹션으로 이동한다", async ({ page }) => {
  await registerAndLogin(page, "wiki");
  await page.getByRole("button", { name: "게임 위키" }).click();
  await expect(page).toHaveURL(/\/wiki$/);
  await expect(page.getByRole("heading", { name: "게임 위키" })).toBeVisible();

  const progressionLink = page.getByRole("link", { name: "성장과 진행" });
  await expect(progressionLink).toHaveAttribute("data-enhance-nav", "false");
  await progressionLink.click();
  await expect(page).toHaveURL(/\/wiki#progression$/);
  await expect
    .poll(async () =>
      page.evaluate(() => {
        const section = document.getElementById("progression");
        if (!section) return false;
        const top = section.getBoundingClientRect().top;
        return top >= -40 && top <= 180;
      }),
    )
    .toBe(true);

  await page.getByRole("link", { name: "성장과 진행" }).click();
  await expect(page).toHaveURL(/\/wiki#progression$/);
  await expect
    .poll(() =>
      page.evaluate(() => {
        const section = document.querySelector("#progression");
        return {
          pathname: window.location.pathname,
          hash: window.location.hash,
          scrollY: window.scrollY,
          sectionTop: section?.getBoundingClientRect().top ?? Number.POSITIVE_INFINITY,
        };
      }),
    )
    .toMatchObject({ pathname: "/wiki", hash: "#progression" });

  const position = await page.evaluate(() => ({
    scrollY: window.scrollY,
    sectionTop: document.querySelector("#progression")?.getBoundingClientRect().top ?? Infinity,
    viewportHeight: window.innerHeight,
  }));
  expect(position.scrollY).toBeGreaterThan(0);
  expect(position.sectionTop).toBeLessThan(position.viewportHeight);
});

test("로그인된 사용자의 컬렉션에 숙련도와 다음 단계 안내가 표시된다", async ({ page }) => {
  test.setTimeout(240_000);
  await registerAndLogin(page, "collection");
  await winRound(page, true);

  await page.locator("details.result-secondary-actions summary").click();
  await page.getByRole("button", { name: "새 모험 시작" }).click();
  await expect(page).toHaveURL(/\/starter$/);
  await page.getByRole("button", { name: "취소" }).click();
  await expect(page).toHaveURL(/\/$/);
  await page.getByRole("button", { name: "획득한 포켓몬 전체 보기" }).click();
  await expect(page).toHaveURL(/\/collection$/);
  await expect(page.getByRole("heading", { name: "획득한 포켓몬" })).toBeVisible();
  const bulbasaurCard = page.locator(".collection-card").filter({ hasText: "이상해씨" });
  await expect(bulbasaurCard).toHaveCount(1);
  await expect(bulbasaurCard.locator(".mastery-label")).toHaveText("숙련도 · 견습");
  await expect(bulbasaurCard.locator(".mastery-count")).toHaveText("1회 기여 · 전 스탯 +0%");
  await expect(bulbasaurCard.locator(".mastery-next")).toHaveText("다음 단계까지 4회");
});

test("숙련도 단계가 오른 포켓몬으로 시작한 전투에 아군 보너스가 적용된다", async ({ page }) => {
  test.setTimeout(900_000);
  await registerAndLogin(page, "battlemastery");

  for (let round = 0; round < 5; round += 1) {
    await winRound(page, round === 0);
  }

  await continueToNextBattle(page);
  const heroHud = page.locator(".hud-plate.hud-hero");
  await expect(heroHud).toHaveAttribute("data-mastery-bonus-percent", "1");
  await expect(heroHud).toHaveAttribute("aria-label", /숙련도 보너스 \+1%/);
});