import path from 'path';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vite';
import runtimeErrorOverlay from '@replit/vite-plugin-runtime-error-modal';

export default defineConfig(async ({ command }) => {
  const isBuild = command === 'build';

  // 배포 빌드 시 환경변수가 없을 경우 기본값 적용 (에러 방지)
  const port = Number(process.env.PORT || '3000');
  const basePath = process.env.BASE_PATH || '/';

  const plugins = [
    react(),
    tailwindcss(),
  ];

  // 개발 모드(Preview/Dev)일 때만 Replit 디버깅 플러그인 활성화
  if (!isBuild) {
    plugins.push(runtimeErrorOverlay());

    if (process.env.REPL_ID !== undefined) {
      const { cartographer } = await import('@replit/vite-plugin-cartographer');
      const { devBanner } = await import('@replit/vite-plugin-dev-banner');

      plugins.push(
        cartographer({ root: path.resolve(import.meta.dirname, '..') }),
        devBanner()
      );
    }
  }

  return {
    base: basePath,
    plugins,
    resolve: {
      alias: {
        '@': path.resolve(import.meta.dirname, 'src'),
        '@assets': path.resolve(
          import.meta.dirname,
          '..',
          '..',
          'attached_assets',
        ),
      },
      dedupe: ['react', 'react-dom'],
    },
    root: path.resolve(import.meta.dirname),
    build: {
      outDir: path.resolve(import.meta.dirname, 'dist/public'),
      emptyOutDir: true,
    },
    server: {
      port,
      strictPort: true,
      host: '0.0.0.0',
      allowedHosts: true,
      fs: {
        strict: true,
      },
    },
    preview: {
      port,
      host: '0.0.0.0',
      allowedHosts: true,
    },
  };
});
