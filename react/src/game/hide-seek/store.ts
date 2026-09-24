import { storage, type StorageAdapter } from '../../services/storage';
import type { HideSeekSession } from './controller';
import type { HideSeekBank } from './content';

export interface HideSeekRecord { bestStars: number; completedCount: number }
export interface HideSeekArchive {
  active?: HideSeekSession;
  history: Record<string, HideSeekRecord>;
  completed: Array<HideSeekSession & { synced?: boolean }>;
}
export const hideSeekKey = (owner: string, childId: string) => `${owner}:hide-seek:${childId}`;
interface HideSeekRepository { readonly owner: string; readonly local: boolean; api<T>(path: string, method?: string, body?: unknown): Promise<T> }
export class HideSeekStore {
  data: HideSeekArchive = { history: {}, completed: [] };
  private chain: Promise<void> = Promise.resolve();
  private syncing?: Promise<void>;
  private key: string;
  constructor(readonly repository: HideSeekRepository, readonly childId: string, readonly bank: HideSeekBank, private disk: StorageAdapter = storage) {
    this.key = hideSeekKey(repository.local ? 'device' : repository.owner, childId);
  }
  async load() {
    this.data = await this.disk.get<HideSeekArchive>(this.key) ?? this.data;
    if (this.data.active && (!this.bank.levels.some(l => l.id === this.data.active!.levelId && l.contentVersion === this.data.active!.contentVersion) || this.data.active.childId !== this.childId))
      throw new Error('Phiên Trốn tìm đã lưu không khớp nội dung hiện tại. Hãy giữ dữ liệu và dùng đúng phiên bản trước khi tiếp tục.');
  }
  private write(change: (next: HideSeekArchive) => void) {
    const operation = this.chain.catch(() => {}).then(async () => {
      const next = structuredClone(this.data); change(next);
      await this.disk.set(this.key, next); this.data = next;
    });
    this.chain = operation; return operation;
  }
  checkpoint = (session: HideSeekSession) => this.write(next => {
    next.active = session;
    if (session.state.phase !== 'completed' || next.completed.some(s => s.id === session.id)) return;
    next.completed.push(session);
    const record = next.history[session.levelId] ?? { bestStars: 0, completedCount: 0 };
    next.history[session.levelId] = { bestStars: Math.max(record.bestStars, session.state.stars), completedCount: record.completedCount + 1 };
  });
  sync() {
    if (this.repository.local) return Promise.resolve();
    return this.syncing ??= this.performSync().finally(() => { this.syncing = undefined; });
  }
  private async performSync() {
    await this.chain;
    for (const session of this.data.completed.filter(s => !s.synced)) {
      const result = await this.repository.api<{ levelId: string; bestStars: number; completedCount: number; starsRemaining: number }>('hide-seek/complete', 'POST', {
        schemaVersion: session.schemaVersion, sessionId: session.id, childId: session.childId, levelId: session.levelId,
        contentVersion: session.contentVersion, seed: session.seed, startedAt: session.startedAt, completedAt: session.completedAt,
        activeDurationMs: Math.round(session.activeDurationMs), attempts: session.state.attempts.map(a => ({ answerEventId: a.answerEventId, revealId: a.revealId, spotId: a.spotId, answer: a.answer }))
      });
      await this.write(next => {
        const saved = next.completed.find(s => s.id === session.id); if (saved) saved.synced = true;
        const local = next.history[result.levelId];
        next.history[result.levelId] = { bestStars: Math.max(local?.bestStars ?? 0, result.bestStars), completedCount: Math.max(local?.completedCount ?? 0, result.completedCount) };
      });
    }
    const history = await this.repository.api<Array<{ levelId: string; bestStars: number; completedCount: number }>>(`hide-seek/children/${this.childId}/history`);
    await this.write(next => { for (const p of history) {
      const local = next.history[p.levelId]; next.history[p.levelId] = { bestStars: Math.max(local?.bestStars ?? 0, p.bestStars), completedCount: Math.max(local?.completedCount ?? 0, p.completedCount) };
    } });
  }
}
