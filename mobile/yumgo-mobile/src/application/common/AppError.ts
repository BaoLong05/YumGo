export class AppError extends Error {
  readonly _tag = 'AppError';

  constructor(
    message: string,
    readonly code?: string,
  ) {
    super(message);
  }
}