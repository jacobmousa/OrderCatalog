import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { federation } from '@module-federation/vite';

export default defineConfig({
  plugins: [
    react(),
  federation({
      name: 'web_shell',
      remotes: {
        catalog: {
          type: 'module',
          name: 'catalog',
          entry: 'http://localhost:3001/remoteEntry.js'
        },
        orders: {
          type: 'module',
          name: 'orders',
          entry: 'http://localhost:3002/remoteEntry.js'
        }
      },
      filename: 'remoteEntry.js',
      exposes: {},
      shared: {
        react: { singleton: true, requiredVersion: '^18.0.0' },
        'react-dom': { singleton: true, requiredVersion: '^18.0.0' }
      }
    })
  ],
  build: {
    target: 'es2020'
  }
});
