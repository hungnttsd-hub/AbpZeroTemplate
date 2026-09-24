import type { HideSeekLevel, HideSeekState, ToolId, VocabularyEntity } from './contracts';
import { acknowledgeFeedback, createState, finishReveal, setPaused, startReveal, submitAnswer } from './rules';
import { materializeLevel } from './content';

export interface HideSeekSession {
  schemaVersion: '1.0.0'; id: string; childId: string; levelId: string; contentVersion: string;
  seed: number; startedAt: string; completedAt?: string; activeDurationMs: number;
  selectedTool: ToolId; revealTool?: ToolId; state: HideSeekState;
}
export function newSession(level: HideSeekLevel, catalog: VocabularyEntity[], childId: string): HideSeekSession {
  const seed = (crypto.getRandomValues(new Uint32Array(1))[0] & 0x7fffffff) || 1;
  return { schemaVersion: '1.0.0', id: crypto.randomUUID(), childId, levelId: level.id, contentVersion: level.contentVersion,
    seed, startedAt: new Date().toISOString(), activeDurationMs: 0, selectedTool: level.tools[0], state: createState(level, catalog) };
}
/** Single state owner shared by the canvas and the accessible React presenters. */
export class HideSeekController {
  readonly level: HideSeekLevel;
  private listeners = new Set<() => void>();
  private snapshot = 0;
  saving = false;
  error = '';
  private saveGeneration = 0;
  private disposed = false;
  constructor(public session: HideSeekSession, level: HideSeekLevel, readonly catalog: VocabularyEntity[],
    private persist: (session: HideSeekSession) => Promise<void>) {
    if (level.id !== session.levelId || level.contentVersion !== session.contentVersion) throw new Error('Phiên lưu dùng phiên bản nội dung khác.');
    this.level = materializeLevel(level, session.seed);
    this.session = structuredClone(session);
    this.session.state = setPaused(session.state, false);
  }
  get state() { return this.session.state; }
  get target() { return this.catalog.find(e => e.id === this.level.targetEntityId)!; }
  get occupant() { const spot = this.level.spots.find(s => s.id === this.state.pending?.spotId); return this.catalog.find(e => e.id === spot?.entityId); }
  subscribe = (fn: () => void) => { this.listeners.add(fn); return () => { this.listeners.delete(fn); }; };
  getSnapshot = () => this.snapshot;
  private emit() { this.snapshot++; this.listeners.forEach(fn => fn()); }
  private update(next: HideSeekState) {
    if (this.disposed || next === this.state) return false;
    this.session = { ...this.session, state: next };
    if (next.phase === 'completed' && !this.session.completedAt) this.session.completedAt = new Date().toISOString();
    this.emit(); void this.checkpoint(); return true;
  }
  selectTool(tool: ToolId) {
    if (this.state.paused || this.state.phase !== 'searching' || !this.level.tools.includes(tool)) return;
    this.session = { ...this.session, selectedTool: tool }; this.emit();
  }
  reveal(spotId: string) {
    if (this.error || this.disposed) return false;
    const next = startReveal(this.state, this.level, spotId, this.session.selectedTool, crypto.randomUUID());
    if (next === this.state) return false;
    this.session.revealTool = this.session.selectedTool; return this.update(next);
  }
  finish(revealId: string) { return this.update(finishReveal(this.state, revealId)); }
  answer(yes: boolean, eventId = crypto.randomUUID()) {
    if (this.error || this.disposed) return false;
    return this.update(submitAnswer(this.state, this.level, this.catalog, yes, eventId));
  }
  continue() { if (!this.error) this.update(acknowledgeFeedback(this.state)); }
  pause(paused: boolean) { if (this.state.paused !== paused) this.update(setPaused(this.state, paused)); }
  tick(ms: number) { if (!this.state.paused && this.state.phase !== 'completed') this.session.activeDurationMs += Math.min(250, Math.max(0, ms)); }
  async checkpoint() {
    const generation = ++this.saveGeneration; this.saving = true; this.emit();
    try {
      await this.persist(structuredClone(this.session));
      if (generation === this.saveGeneration) this.error = '';
    } catch (error) {
      this.error = error instanceof Error ? error.message : 'Chưa lưu được lượt chơi.';
      this.session.state = setPaused(this.state, true);
    } finally { if (generation === this.saveGeneration) this.saving = false; if (!this.disposed) this.emit(); }
    return !this.error;
  }
  dispose() { this.disposed = true; this.listeners.clear(); }
}
