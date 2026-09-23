import { storage } from '../../services/storage';
import type { GameRepository } from '../../services/repository';
import { chooseQuestion, contentWords, loadQuestion, selectSession, type Selection } from './bank';
import { emptyHistory, evaluate, pointsFor, profile, resolved, sessionScore, timeLimitMs, type AnswerInput, type Bank, type BellHistory, type BellSession } from './model';

interface SavedProfile { history: BellHistory; active?: BellSession; completedSessions: BellSession[] }
export class GoldenBellStore {
  private data: SavedProfile = { history: emptyHistory(), completedSessions: [] };
  private saving: Promise<unknown> = Promise.resolve();
  private syncing?: Promise<void>;
  onChange: () => void = () => {};
  constructor(readonly scope: string, readonly childId: string, readonly bank: Bank, readonly repository?: GameRepository) {}
  get key() { return `golden-bell:v1:${this.scope}`; }
  get session() { return this.data.active; }
  get history() { return this.data.history; }
  async settled() { await this.saving.catch(() => {}); }
  async load() {
    this.data = await storage.get<SavedProfile>(this.key) ?? this.data;
    // Completed snapshots are retained for idempotent account synchronization.
    return this.session;
  }
  private async save(next: SavedProfile) {
    const snapshot = structuredClone(next);
    const task = this.saving.catch(() => {}).then(() => storage.set(this.key, snapshot));
    this.saving = task; await task; this.data = snapshot; this.onChange();
  }
  private async patchLatest(change: (current: SavedProfile) => void) {
    const task = this.saving.catch(() => {}).then(async () => {
      const next = structuredClone(this.data); change(next); await storage.set(this.key, next); this.data = next; this.onChange();
    });
    this.saving = task; await task;
  }
  async start(config: Omit<Selection, 'seed'> & { mode: BellSession['mode'] }) {
    const seed = crypto.getRandomValues(new Uint32Array(1))[0];
    const ids = selectSession(this.bank, this.history, { ...config, seed });
    const questions = await Promise.all(ids.map(loadQuestion));
    const session: BellSession = { id: crypto.randomUUID(), childId: this.childId, bankVersion: this.bank.version, seed, mode: config.mode, scoringVersion: 1,
      maxDifficulty: config.maxDifficulty, allowedVocabulary: config.allowedVocabulary, startedAt: new Date().toISOString(), questions, index: 0, phase: 'intro',
      records: questions.map(q => ({ questionId: q.id, attempts: 0, wrong: 0, hintUsed: false, durationMs: 0, completed: false, memoryReplays: 0, answerMs: 0, timedOut: false, score: 0 })), answers: [], bellRung: false, checkpointSeen: 0, syncedAnswers: 0 };
    await this.save({ ...this.data, active: session });
  }
  async begin() { const s = structuredClone(this.session!); s.phase = 'question'; await this.save({ ...this.data, active: s }); }
  async recordInput(input: AnswerInput, elapsedMs: number, answerMs = 0) {
    const s = structuredClone(this.session!), q = s.questions[s.index], record = s.records[s.index];
    if (s.phase !== 'question' || resolved(record)) return undefined;
    record.answerMs = (record.answerMs ?? 0) + Math.max(0, Math.round(answerMs));
    const timedOut = s.scoringVersion === 1 && record.answerMs >= timeLimitMs(q);
    if (input.type === 'timeout' && !timedOut) throw new Error('Câu hỏi vẫn còn thời gian.');
    if (timedOut) { input = { type: 'timeout', value: null }; record.answerMs = timeLimitMs(q); }
    const correct = !timedOut && evaluate(q, input); record.attempts++; record.wrong += correct || timedOut ? 0 : 1; record.durationMs += Math.max(0, Math.round(elapsedMs));
    record.hintUsed ||= record.wrong >= q.hint.afterWrong;
    record.completed = correct; record.timedOut = timedOut;
    record.score = correct && s.scoringVersion === 1 ? pointsFor(q, record.answerMs, record.wrong, record.hintUsed) : 0;
    s.answers.push({ id: crypto.randomUUID(), questionId: q.id, questionIndex: s.index, input, correct, hintUsed: record.hintUsed, durationMs: record.durationMs, answerMs: record.answerMs, createdAt: new Date().toISOString() });
    const history = structuredClone(this.history);
    if (record.attempts === 1) history.seen[q.id] = (history.seen[q.id] ?? 0) + 1;
    if (record.wrong >= 2 || timedOut) q.targetVocabulary.forEach(w => history.review[w.toLowerCase().replace(/s$/, '')] = (history.review[w.toLowerCase().replace(/s$/, '')] ?? 0) + 1);
    if (correct && !history.completed.includes(q.id)) history.completed.push(q.id);
    if (correct && record.wrong === 0) q.targetVocabulary.forEach(w => { const key = w.toLowerCase().replace(/s$/, ''); if (history.review[key]) history.review[key]--; });
    await this.save({ ...this.data, history, active: s });
    return { correct, hint: record.hintUsed, timedOut, score: record.score };
  }
  async hint(memoryReplay = false) {
    const s = structuredClone(this.session!), r = s.records[s.index];
    if (s.phase !== 'question' || resolved(r)) return;
    r.hintUsed = true; if (memoryReplay) r.memoryReplays++;
    await this.save({ ...this.data, active: s });
  }
  async remember(patch: { memorySeen?: boolean; draft?: string[] }, elapsedMs = 0, answerMs = 0, index = this.session!.index) {
    const s = structuredClone(this.session!);
    if (s.index !== index || s.phase !== 'question' || resolved(s.records[index])) return;
    Object.assign(s.records[index], patch); s.records[index].durationMs += Math.max(0, Math.round(elapsedMs));
    s.records[index].answerMs = (s.records[index].answerMs ?? 0) + Math.max(0, Math.round(answerMs));
    await this.save({ ...this.data, active: s });
  }
  async advance() {
    const s = structuredClone(this.session!);
    if (!resolved(s.records[s.index])) return;
    if (s.index === 11) s.phase = 'bell';
    else if ([3, 7].includes(s.index) && s.checkpointSeen < s.index + 1 && s.phase !== 'checkpoint') s.phase = 'checkpoint';
    else {
      if (s.phase === 'checkpoint') s.checkpointSeen = s.index + 1;
      const next = s.index + 1, recent = s.records.slice(0, next);
      const weak = recent.slice(-3).filter(r => r.wrong > 0 || r.timedOut).length >= 2;
      const streak = recent.length >= 4 && recent.slice(-4).every(r => r.completed && r.wrong === 0 && !r.hintUsed);
      if (s.mode !== 'practice' && next < 11 && (weak || streak)) {
        const desired = Math.max(1, Math.min(s.maxDifficulty, profile[next] + (weak ? -1 : 1)));
        let replacement;
        try { replacement = chooseQuestion(this.bank, this.history, { seed: s.seed, maxDifficulty: s.maxDifficulty, allowedVocabulary: s.allowedVocabulary }, s.questions.slice(0, next), s.questions.filter((_, i) => i !== next).map(q => q.id), desired, weak ? s.questions[s.index].skill : undefined); }
        catch { /* A small learned-vocabulary pool may have no replacement; keep the valid saved plan. */ }
        // Keep anti-repeat constraints with the following planned questions too.
        const after = s.questions[next + 1], afterNext = s.questions[next + 2];
        if (replacement && !(replacement.questionType === 'memory_scene' && after?.questionType === 'memory_scene') && !(replacement.questionType === after?.questionType && (after?.questionType === afterNext?.questionType || s.questions[next - 1].questionType === after?.questionType))
          && !contentWords(replacement).some(w => [after, afterNext].filter(Boolean).some(q => contentWords(q).includes(w)))) {
          s.questions[next] = await loadQuestion(replacement.id); s.records[next].questionId = replacement.id;
        }
      }
      s.index = next; s.phase = 'question';
    }
    await this.save({ ...this.data, active: s });
  }
  async ring() {
    const s = structuredClone(this.session!);
    if (s.bellRung || s.phase !== 'bell' || s.records.some(r => !resolved(r))) return;
    s.bellRung = true; s.phase = 'summary'; s.completedAt = new Date().toISOString();
    const history = structuredClone(this.history); history.tokens++; history.sessions++;
    history.bestScore = Math.max(history.bestScore ?? 0, sessionScore(s));
    history.minutes += s.records.reduce((n, r) => n + r.durationMs, 0) / 60000;
    await this.save({ ...this.data, history, active: s, completedSessions: [...this.data.completedSessions.filter(x => x.id !== s.id), s] });
  }
  sync() {
    if (!this.repository || this.repository.local) return Promise.resolve();
    return this.syncing ??= this.performSync().finally(() => { this.syncing = undefined; });
  }
  private async performSync() {
    const repo = this.repository!;
    const pending = this.data.completedSessions;
    for (const original of pending) {
      if (original.serverCompleted) continue;
      const s = structuredClone(original);
      await repo.api('golden-bell/session/start', 'POST', { sessionId: s.id, childId: s.childId, seed: s.seed, questionCodes: s.questions.map(q => q.id), bankVersion: s.bankVersion, startedAt: s.startedAt, scoringVersion: s.scoringVersion ?? 0 });
      for (const answer of s.answers.slice(s.syncedAnswers)) await repo.api(`golden-bell/session/${s.id}/answer`, 'POST', answer);
      if (s.bellRung) await repo.api(`golden-bell/session/${s.id}/complete`, 'POST', { bellRung: true, completedAt: s.completedAt });
      // Read the current aggregate after network I/O so new local answers are not overwritten.
      await this.patchLatest(next => {
        const active = next.active?.id === s.id ? next.active : undefined, completed = next.completedSessions.find(x => x.id === s.id);
        for (const target of [active, completed]) if (target) { target.syncedAnswers = s.answers.length; target.serverCompleted = s.bellRung; target.serverStarted = true; }
      });
    }
    const remote = await repo.api<{ completed: string[]; tokens: number; sessions: number; minutes: number; bestScore?: number }>(`golden-bell/children/${this.childId}/history`);
    await this.patchLatest(next => {
      next.history.completed = [...new Set([...next.history.completed, ...remote.completed])];
      next.history.tokens = Math.max(next.history.tokens, remote.tokens);
      next.history.sessions = Math.max(next.history.sessions, remote.sessions);
      next.history.minutes = Math.max(next.history.minutes, remote.minutes);
      next.history.bestScore = Math.max(next.history.bestScore ?? 0, remote.bestScore ?? 0);
    });
  }
}
