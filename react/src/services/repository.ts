import rawLevels from '../content/levels.json';
import rawWorlds from '../content/worlds.json';
import { levelSchema, starsFor, type Attempt, type Child, type Dashboard, type LevelDefinition, type Mastery, type Progress, type World } from '../types';
import { storage } from './storage';
import type { HideSeekArchive } from '../game/hide-seek/store';

export interface Session { userId: string | null; name: string; isAdmin: boolean; csrfToken: string; csrfHeader: string }
export class ApiError extends Error { constructor(public status: number, message: string) { super(message); } }
export class GameRepository {
  private session?: Session;
  private syncing?: Promise<number>;
  constructor(public readonly owner: string, public readonly local: boolean) {}
  private key(suffix: string) { return `${this.local ? 'device' : this.owner}:${suffix}`; }
  async api<T>(path: string, method = 'GET', body?: unknown): Promise<T> {
    if (method !== 'GET' && !this.session) this.session = await getSession();
    const headers: Record<string, string> = { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' };
    if (this.session?.csrfToken) headers[this.session.csrfHeader || 'RequestVerificationToken'] = this.session.csrfToken;
    const response = await fetch(`/api/game/${path}`, { method, credentials: 'include', headers, body: body === undefined ? undefined : JSON.stringify(body), signal: AbortSignal.timeout(12000) });
    if (!response.ok || response.redirected) {
      if ([400, 401, 403].includes(response.status)) this.session = undefined;
      const payload = await response.json().catch(() => ({})) as { error?: { message?: string } | string };
      throw new ApiError(response.status, response.status === 401 || response.redirected ? 'Phiên đăng nhập đã hết. Tiến trình vẫn được giữ trên thiết bị.' :
        response.status === 428 ? 'Tài khoản cần hoàn tất bước xác nhận. Hãy mở lại trang đăng nhập.' :
          typeof payload.error === 'object' && payload.error.message ? payload.error.message : 'Chưa kết nối được máy chủ. Dữ liệu đã lưu trên thiết bị sẽ đồng bộ sau.');
    }
    if (response.status === 204 || response.headers.get('content-length') === '0') return undefined as T;
    const text = await response.text(); return text ? JSON.parse(text) as T : undefined as T;
  }
  async content(): Promise<{ levels: LevelDefinition[]; worlds: World[] }> {
    const bundled = { levels: rawLevels.map(l => levelSchema.parse(l)), worlds: rawWorlds.slice(0, 3) as World[] };
    if (this.local) return bundled;
    try {
      const [levels, worlds] = await Promise.all([this.api<unknown[]>('levels'), this.api<World[]>('worlds')]);
      const content = { levels: levels.map(l => levelSchema.parse(l)), worlds };
      if (!content.levels.length) throw new Error('Nội dung chưa được seed. Hãy chạy DbMigrator.');
      await storage.set(this.key('content'), content); return content;
    } catch (error) {
      const cached = await storage.get<typeof bundled>(this.key('content'));
      if (cached) return { ...cached, levels: cached.levels.map(l => levelSchema.parse(l)) };
      throw error;
    }
  }
  async children(): Promise<Child[]> {
    if (this.local) return await storage.get<Child[]>(this.key('children')) ?? [];
    try { const children = await this.api<Child[]>('children'); await storage.set(this.key('children'), children); return children; }
    catch (error) { const cached = await storage.get<Child[]>(this.key('children')); if (cached) return cached; throw error; }
  }
  async createChild(input: Omit<Child, 'id'>) {
    const child = this.local ? { ...input, id: crypto.randomUUID() } : await this.api<Child>('children', 'POST', input);
    const children = await this.children();
    if (!children.some(c => c.id === child.id)) await storage.set(this.key('children'), [...children, child]);
    return child;
  }
  async pending() { return storage.entries<Attempt>(this.key('attempt:')); }
  async save(attempt: Attempt) {
    // Durable write must complete before displaying rewards or leaving the level.
    await storage.set(this.key(`attempt:${attempt.attemptId}`), attempt);
  }
  async progress(childId: string): Promise<Progress[]> {
    if (!this.local) {
      try { const base = await this.api<Progress[]>(`children/${childId}/progress`); await storage.set(this.key(`progress:${childId}`), base); }
      catch { /* The durable snapshot and pending queue keep the map usable offline. */ }
    }
    return this.localProgress(childId);
  }
  async localProgress(childId: string): Promise<Progress[]> {
    const base = await storage.get<Progress[]>(this.key(`progress:${childId}`)) ?? [];
    const map = new Map(base.map(p => [p.levelId, { ...p }]));
    for (const { value: a } of await this.pending()) {
      if (a.childId !== childId) continue;
      const p = map.get(a.levelId) ?? { levelId: a.levelId, bestStars: 0, completedCount: 0 };
      p.bestStars = Math.max(p.bestStars, starsFor(a)); p.completedCount++; map.set(a.levelId, p);
    }
    const hideSeek = await storage.get<HideSeekArchive>(this.key(`hide-seek:${childId}`));
    for (const [levelId, record] of Object.entries(hideSeek?.history ?? {})) {
      const existing = map.get(levelId);
      map.set(levelId, { levelId, bestStars: Math.max(existing?.bestStars ?? 0, record.bestStars), completedCount: Math.max(existing?.completedCount ?? 0, record.completedCount) });
    }
    return [...map.values()];
  }
  sync(): Promise<number> {
    if (this.local) return Promise.resolve(0);
    if (this.syncing) return this.syncing;
    this.syncing = this.performSync().finally(() => { this.syncing = undefined; });
    return this.syncing;
  }
  private async performSync() {
    // Refresh CSRF/session after reauthentication. Never transfer another parent's queue.
    this.session = await getSession();
    if (this.session.userId !== this.owner) throw new ApiError(401, 'Hãy đăng nhập đúng tài khoản để đồng bộ tiến trình.');
    const queued = (await this.pending()).sort((a, b) => a.value.levelId.localeCompare(b.value.levelId) || a.value.startedAt.localeCompare(b.value.startedAt));
    for (const { key, value: a } of queued) {
      const p = await this.api<Progress>('progress/sync', 'POST', a);
      const base = await storage.get<Progress[]>(this.key(`progress:${a.childId}`)) ?? [];
      await storage.set(this.key(`progress:${a.childId}`), [...base.filter(x => x.levelId !== p.levelId), p]);
      await storage.remove(key);
    }
    return queued.length;
  }
  async dashboard(childId: string): Promise<Dashboard> {
    if (!this.local) {
      try { const result = await this.api<Dashboard>(`children/${childId}/dashboard`); await storage.set(this.key(`dashboard:${childId}`), result); return result; }
      catch (error) { const cached = await storage.get<Dashboard>(this.key(`dashboard:${childId}`)); if (cached) return cached; throw error; }
    }
    const attempts = (await this.pending()).map(a => a.value).filter(a => a.childId === childId);
    const p = await this.progress(childId); const words = new Map<string, Mastery>();
    for (const a of attempts) {
      for (const term of new Set(a.targetResults.map(r => r.term))) {
        const word = words.get(term) ?? { term, exposureCount: 0, correctCount: 0, incorrectCount: 0, masteryScore: 0 };
        word.exposureCount++; word.correctCount += a.targetResults.filter(r => r.term === term && r.correct).length;
        word.incorrectCount += a.targetResults.filter(r => r.term === term && !r.correct).length;
        word.masteryScore = Math.round(100 * (.4 * Math.min(word.exposureCount / 4, 1) + .6 * word.correctCount / Math.max(word.correctCount + word.incorrectCount, 1)));
        words.set(term, word);
      }
    }
    const hideSeek = await storage.get<HideSeekArchive>(this.key(`hide-seek:${childId}`));
    for (const session of hideSeek?.completed ?? []) {
      const term = session.state.attempts.at(-1)?.entityId; if (!term) continue;
      const word = words.get(term) ?? { term, exposureCount: 0, correctCount: 0, incorrectCount: 0, masteryScore: 0 };
      const independent = session.state.attempts.filter(a => !a.assisted);
      word.exposureCount++; word.correctCount += independent.filter(a => a.correct).length; word.incorrectCount += independent.filter(a => !a.correct).length;
      word.masteryScore = Math.round(100 * (.4 * Math.min(word.exposureCount / 4, 1) + .6 * word.correctCount / Math.max(word.correctCount + word.incorrectCount, 1)));
      words.set(term, word);
    }
    const hideSeekMs = (hideSeek?.completed ?? []).reduce((n, s) => n + s.activeDurationMs, 0);
    return { completedLevels: p.length, stars: p.reduce((n, x) => n + x.bestStars, 0), minutes: Math.round((hideSeekMs + attempts.reduce((n, a) => n + Date.parse(a.completedAt) - Date.parse(a.startedAt), 0)) / 6000) / 10,
      words: [...words.values()], review: [...words.values()].filter(w => w.exposureCount >= 2 && w.masteryScore < 70) };
  }
}
export async function getSession(): Promise<Session> {
  const response = await fetch('/api/game/session', { credentials: 'include', signal: AbortSignal.timeout(6000) });
  if (!response.ok || response.redirected) throw new ApiError(response.status, 'Chưa kết nối được tài khoản.');
  return response.json() as Promise<Session>;
}
