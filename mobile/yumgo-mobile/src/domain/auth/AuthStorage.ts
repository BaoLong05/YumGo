import { secureStorage } from '@/infrastructure/storage/SecureStorage';
import { Effect } from 'effect';

const ACCESS_TOKEN_KEY = 'yumgo.access_token';
const REFRESH_TOKEN_KEY = 'yumgo.refresh_token';

export const authStorage = {
  getAccessToken: () => secureStorage.get(ACCESS_TOKEN_KEY),

  saveAccessToken: (token: string) =>
    secureStorage.set(ACCESS_TOKEN_KEY, token),

  getRefreshToken: () => secureStorage.get(REFRESH_TOKEN_KEY),

  saveRefreshToken: (token: string) =>
    secureStorage.set(REFRESH_TOKEN_KEY, token),

  clear: () =>
    Effect.all([
      secureStorage.remove(ACCESS_TOKEN_KEY),
      secureStorage.remove(REFRESH_TOKEN_KEY),
    ]),
};