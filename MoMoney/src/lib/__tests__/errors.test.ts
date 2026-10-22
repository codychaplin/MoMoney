import { isExpense } from '@/lib/constants';
import { DuplicateError, InvalidError, isDomainError, NotFoundError } from '@/lib/errors';

describe('errors', () => {
  it('domain errors keep name and message', () => {
    for (const [Cls, name] of [[NotFoundError, 'NotFoundError'], [DuplicateError, 'DuplicateError'], [InvalidError, 'InvalidError']] as const) {
      const e = new Cls('boom');
      expect(e).toBeInstanceOf(Error);
      expect(e.name).toBe(name);
      expect(e.message).toBe('boom');
      expect(isDomainError(e)).toBe(true);
    }
  });
  it('other values are not domain errors', () => {
    expect(isDomainError(new Error('x'))).toBe(false);
    expect(isDomainError('x')).toBe(false);
    expect(isDomainError(null)).toBe(false);
  });
});

describe('constants', () => {
  it('isExpense uses the id 5 threshold', () => {
    expect(isExpense(4)).toBe(false);
    expect(isExpense(5)).toBe(true);
    expect(isExpense(33)).toBe(true);
  });
});
