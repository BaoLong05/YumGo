import * as SecureStore from 'expo-secure-store';
import { Effect } from 'effect';

export const secureStorage = {
  get: (key: string) =>
    Effect.tryPromise({
      try: () => SecureStore.getItemAsync(key),
      catch: (error) =>
        error instanceof Error ? error : new Error(String(error)),
    }),

  set: (key: string, value: string) =>
    Effect.tryPromise({
      try: () => SecureStore.setItemAsync(key, value),
      catch: (error) =>
        error instanceof Error ? error : new Error(String(error)),
    }),

  remove: (key: string) =>
    Effect.tryPromise({
      try: () => SecureStore.deleteItemAsync(key),
      catch: (error) =>
        error instanceof Error ? error : new Error(String(error)),
    }),
};