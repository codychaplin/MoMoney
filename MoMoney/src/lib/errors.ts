// Ported from MoMoney.Core/Exceptions/CustomExceptions.cs

export class NotFoundError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'NotFoundError';
  }
}

export class DuplicateError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'DuplicateError';
  }
}

export class InvalidError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidError';
  }
}

/** Domain errors are shown as "Warning" alerts, anything else as "Error". */
export const isDomainError = (e: unknown): e is NotFoundError | DuplicateError | InvalidError =>
  e instanceof NotFoundError || e instanceof DuplicateError || e instanceof InvalidError;
