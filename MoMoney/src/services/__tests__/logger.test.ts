import type { Db } from '@/db/db';
import { InvalidError } from '@/lib/errors';
import { countLogs, createLogger, getLogFilterOptions, getLogs } from '@/services/logger';
import { createTestDb } from '@/test/db';

let db: Db;
beforeEach(async () => {
  db = await createTestDb();
});

describe('logger', () => {
  it('writes, filters, counts and lists filter options', async () => {
    const a = createLogger(db, 'AlphaService');
    const b = createLogger(db, 'BetaService');
    await a.info('started');
    await a.error('doThing', new InvalidError('bad'));
    await b.warn('other', new Error('oops'));
    expect(await countLogs(db)).toBe(3);
    expect(await countLogs(db, { level: 'Error' })).toBe(1);
    expect(await countLogs(db, { className: 'AlphaService', exceptionType: '' })).toBe(1);
    expect((await getLogs(db, { className: 'BetaService' }))[0]).toMatchObject({ level: 'Warning', exceptionType: 'Error' });
    expect(await getLogs(db, { limit: 2, offset: 2 })).toHaveLength(1);
    expect(await getLogFilterOptions(db)).toEqual({ classes: ['AlphaService', 'BetaService'], exceptions: ['', 'Error', 'InvalidError'] });
  });

  it('critical writes a Critical entry', async () => {
    await createLogger(db, 'Svc').critical('fn', new Error('fatal'));
    expect((await getLogs(db, {}))[0]).toMatchObject({ level: 'Critical', message: 'fn: fatal', exceptionType: 'Error' });
  });
});
