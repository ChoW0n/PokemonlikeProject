import { expect, test, type Page } from "@playwright/test";

const starterNames = [
  /이상해씨, 도감 번호 1/,
  /파이리, 도감 번호 4/,
  /꼬부기, 도감 번호 7/,
];

async function registerAndLogin(page: Page) {
  const suffix = `${Date.now()}${test.info().workerIndex}${test.info().project.name}`;
  const username = `tm${suffix.replace(/[^a-zA-Z0-9]/g, "")}`;
  const password = "technical-machine-flow-test";

  await page.goto("/register");
  await page.waitForTimeout(500);
  await page.locator("#register-username").pressSequentially(username);
  await page.locator("#register-password").pressSequentially(password);
  await expect(page.locator("#register-username")).toHaveValue(username);
  await expect(page.locator("#register-password")).toHaveValue(password);
  await page.getByRole("button", { name: "가입하기" }).click();
  await expect(page).toHaveURL(/\/login$/);

  await page.waitForTimeout(300);
  await page.locator("#login-username").pressSequentially(username);
  await page.locator("#login-password").pressSequentially(password);
  await expect(page.locator("#login-username")).toHaveValue(username);
  await expect(page.locator("#login-password")).toHaveValue(password);
  await page.getByRole("button", { name: "로그인" }).click();
  await expect(page).toHaveURL(/\/$/);
}

async function enterTeamSelect(page: Page) {
  await page.getByRole("button", { name: "시작하기" }).click();
  await expect(page).toHaveURL(/\/starter$/);

  for (const starterName of starterNames) {
    const card = page.getByRole("button", { name: starterName });
    await card.click();
    await expect(card).toHaveAttribute("aria-pressed", "true");
  }

  await page.getByRole("button", { name: "이 3마리로 시작하기" }).click();
  await expect(page).toHaveURL(/\/preview$/);
  await page.getByRole("button", { name: /이 상대 팀.*과 배틀 준비하기/ }).click();
  await expect(page).toHaveURL(/\/(select|continue)$/);
  if (page.url().endsWith("/continue")) {
    await page.getByRole("button", { name: "포켓몬 변경" }).click();
  }
  await expect(page).toHaveURL(/\/select$/);
}

test("TM-only move stays locked without inventory, including detail and double-click", async ({
  page,
}) => {
  await registerAndLogin(page);
  await enterTeamSelect(page);

  const bulbasaur = page.getByRole("button", { name: /이상해씨, 도감 번호 1/ });
  await bulbasaur.click();

  const cut = page.getByRole("button", { name: /풀베기/ });
  await expect(cut).toHaveCount(1);
  await expect(cut).toHaveClass(/is-unavailable/);
  await expect(cut).toHaveAttribute("aria-disabled", "true");
  await expect(cut).toContainText("기술머신 필요");
  await expect(cut).toHaveAttribute("aria-pressed", "false");

  await cut.click({ force: true });
  const detail = page.locator(".info-panel");
  await expect(detail).toBeVisible();
  await expect(detail).toContainText("기술머신 필요");
  await expect(detail.getByRole("button", { name: "기술머신 필요" })).toBeDisabled();

  await cut.dblclick({ force: true });
  await expect(cut).toHaveAttribute("aria-pressed", "false");
  await expect(page.getByRole("button", { name: "이 구성으로 팀에 추가" })).toBeEnabled();
});

test("ability cards preview on click and equip on double-click", async ({ page }) => {
  await registerAndLogin(page);
  await enterTeamSelect(page);

  await page.getByRole("button", { name: /이상해씨, 도감 번호 1/ }).click();

  const alternateAbility = page.getByRole("button", { name: /엽록소/ });
  await expect(alternateAbility).toHaveCount(1);
  await expect(alternateAbility).toHaveAttribute("aria-pressed", "false");

  await alternateAbility.click();
  await expect(alternateAbility).toHaveAttribute("aria-expanded", "true");
  await expect(alternateAbility).toHaveAttribute("aria-pressed", "false");
  await expect(page.getByRole("button", { name: "이 특성 선택" })).toBeVisible();

  await alternateAbility.dblclick();
  await expect(alternateAbility).toHaveAttribute("aria-pressed", "true");
  await expect(alternateAbility).toHaveClass(/is-selected/);
});
