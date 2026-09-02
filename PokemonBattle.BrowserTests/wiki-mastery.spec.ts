import { expect, test, type Page } from "@playwright/test";

async function registerAndLogin(page: Page, username: string, password: string) {
  await page.goto("/register");
  await page.waitForTimeout(300);
  await page.locator("#register-username").pressSequentially(username);
  await page.locator("#register-password").pressSequentially(password);
  await page.getByRole("button", { name: "가입하기" }).click();
  await expect(page).toHaveURL(/\/login$/);

  await page.locator("#login-username").pressSequentially(username);
  await page.locator("#login-password").pressSequentially(password);
  await page.getByRole("button", { name: "로그인" }).click();
  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator("main.start-page")).toContainText(
    `${username} 님, 다시 전장으로 돌아오셨습니다.`,
  );
}

test("wiki anchors stay on the page and collection cards show mastery progress", async ({
  page,
}) => {
  const suffix = `${Date.now()}${test.info().workerIndex}${test.info().project.name}`;
  const username = `wikiMastery${suffix.replace(/[^a-zA-Z0-9]/g, "")}`;
  const password = "wiki-mastery-browser-test";

  await registerAndLogin(page, username, password);

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

  await page.getByRole("button", { name: "홈으로 돌아가기" }).click();
  await expect(page).toHaveURL(/\/$/);
  await page.getByRole("button", { name: "획득한 포켓몬 전체 보기" }).click();
  await expect(page).toHaveURL(/\/collection$/);
  await expect(page.locator(".collection-card")).toHaveCount(3);

  const firstMastery = page.locator(".mastery-progress").first();
  await expect(firstMastery).toBeVisible();
  await expect(firstMastery).toContainText("숙련도 · 견습");
  await expect(firstMastery).toContainText("0회 기여");
  await expect(firstMastery).toContainText("전 스탯 +0%");
  await expect(firstMastery).toContainText("다음 단계까지 5회");
  await expect(firstMastery).toHaveAttribute("aria-label", /숙련도 0회/);
});