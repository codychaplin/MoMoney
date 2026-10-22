import { and, asc, count, desc, eq, type SQL } from 'drizzle-orm';

import { Db } from '@/db/db';
import { logs, type Log } from '@/db/schema';

export const LOG_LEVELS = ['Info', 'Warning', 'Error', 'Critical'] as const;
export type LogLevel = (typeof LOG_LEVELS)[number];

/** Local log table only (Firebase is not part of the port). Never throws. */
export async function writeLog(db: Db, level: LogLevel, className: string, message: string, exceptionType = ''): Promise<void> {
  try {
    await db.insert(logs).values({ timestamp: new Date().toISOString(), level, className, message, exceptionType });
  } catch {
    // logging must never break the app
  }
}

export function createLogger(db: Db, className: string) {
  const err = (fn: string, e: unknown) => `${fn}: ${e instanceof Error ? e.message : String(e)}`;
  const type = (e: unknown) => (e instanceof Error ? e.name : '');
  return {
    info: (message: string) => writeLog(db, 'Info', className, message),
    warn: (fn: string, e: unknown) => writeLog(db, 'Warning', className, err(fn, e), type(e)),
    error: (fn: string, e: unknown) => writeLog(db, 'Error', className, err(fn, e), type(e)),
    critical: (fn: string, e: unknown) => writeLog(db, 'Critical', className, err(fn, e), type(e)),
  };
}

export type LogFilter = {
  level?: LogLevel | null;
  className?: string | null;
  /** Use '' for "None". */
  exceptionType?: string | null;
  limit?: number;
  offset?: number;
};

function whereFor(f: LogFilter): SQL | undefined {
  const c: SQL[] = [];
  if (f.level) c.push(eq(logs.level, f.level));
  if (f.className) c.push(eq(logs.className, f.className));
  if (f.exceptionType != null) c.push(eq(logs.exceptionType, f.exceptionType));
  return c.length ? and(...c) : undefined;
}

export async function getLogs(db: Db, f: LogFilter = {}): Promise<Log[]> {
  const q = db.select().from(logs).where(whereFor(f)).orderBy(desc(logs.timestamp), desc(logs.id));
  return f.limit != null ? q.limit(f.limit).offset(f.offset ?? 0) : q;
}

/** Count with the same filter, so paging stays correct after filtering. */
export async function countLogs(db: Db, f: LogFilter = {}): Promise<number> {
  const [{ n }] = await db.select({ n: count() }).from(logs).where(whereFor(f));
  return n;
}

export async function getLogFilterOptions(db: Db): Promise<{ classes: string[]; exceptions: string[] }> {
  const classes = await db.selectDistinct({ v: logs.className }).from(logs).orderBy(asc(logs.className));
  const exceptions = await db.selectDistinct({ v: logs.exceptionType }).from(logs).orderBy(asc(logs.exceptionType));
  return { classes: classes.map((r) => r.v), exceptions: exceptions.map((r) => r.v) };
}

export async function removeAllLogs(db: Db): Promise<number> {
  return (await db.delete(logs).returning({ id: logs.id })).length;
}
