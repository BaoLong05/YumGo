import { Effect } from 'effect';
import { env } from '@/config/env';
import { HttpError } from './HttpError';

export interface HttpClient {
  get<T>(path: string): Effect.Effect<T, HttpError | Error>;
  post<TRequest, TResponse>(
    path: string,
    body: TRequest,
  ): Effect.Effect<TResponse, HttpError | Error>;
}

const request = <T>(
  path: string,
  options?: RequestInit,
): Effect.Effect<T, HttpError | Error> =>
  Effect.tryPromise({
    try: async () => {
      const response = await fetch(`${env.apiUrl}${path}`, {
        ...options,
        headers: {
          'Content-Type': 'application/json',
          ...options?.headers,
        },
      });

      if (!response.ok) {
        throw new HttpError(
          response.status,
          `HTTP ${response.status}`,
        );
      }

      return (await response.json()) as T;
    },
    catch: (error) => {
      if (error instanceof HttpError) {
        return error;
      }

      return error instanceof Error ? error : new Error(String(error));
    },
  });

export const httpClient: HttpClient = {
  get: <T>(path: string) => request<T>(path),

  post: <TRequest, TResponse>(path: string, body: TRequest) =>
    request<TResponse>(path, {
      method: 'POST',
      body: JSON.stringify(body),
    }),
};