import { expect, test } from "@playwright/test";

async function login(page: import("@playwright/test").Page, username: string, password: string) {
  await page.goto("/login");
  await page.waitForTimeout(300);
  const usernameInput = page.locator("#login-username");
  const passwordInput = page.locator("#login-password");
  await usernameInput.fill(username);
  await passwordInput.fill(password);
  await page.getByRole("button", { name: "로그인" }).click();
  await expect(page).toHaveURL(/\/$/);
}

test("기본 관리자 로그인 후 관리자 콘솔과 조작 도구가 보인다", async ({ page }) => {
  await login(page, "admin", "admin");

  await expect(page.getByRole("button", { name: "관리자 콘솔 열기" })).toBeVisible();
  await page.getByRole("button", { name: "관리자 콘솔 열기" }).click();
  await expect(page).toHaveURL(/\/admin$/);
  await expect(page.getByRole("heading", { name: "관리자 콘솔" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "사용자 진행 현황" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "계정 관리" })).toBeVisible();
  await expect(page.locator(".admin-tool-card button").filter({ hasText: "모든 포켓몬 해금" })).toBeVisible();
  await expect(page.getByRole("button", { name: "현재 런 초기화" })).toBeVisible();
});

test("일반 사용자는 관리자 링크와 콘솔 조작에 접근할 수 없다", async ({ page }) => {
  const username = `admin-guard-${Date.now()}`;
  await page.goto("/register");
  await page.waitForTimeout(300);
  await page.locator("#register-username").fill(username);
  await page.locator("#register-password").fill("test-password");
  await page.getByRole("button", { name: "가입하기" }).click();
  await expect(page).toHaveURL(/\/login$/);

  await login(page, username, "test-password");
  await expect(page.getByRole("button", { name: "관리자 콘솔 열기" })).toHaveCount(0);

  await page.goto("/admin");
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole("heading", { name: "포켓몬 배틀 로그인" })).toBeVisible();
  await expect(page.getByRole("button", { name: "전체 포켓몬 해금" })).toHaveCount(0);
});