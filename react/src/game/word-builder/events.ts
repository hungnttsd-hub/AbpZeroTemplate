export const builderEvents = ['WORD_BUILDER_STARTED', 'LETTER_COLLECTED', 'LETTER_PLACED', 'LETTER_WRONG', 'WORD_COMPLETED', 'COMPLETION_ACTION_STARTED', 'COMPLETION_ACTION_FINISHED', 'LEVEL_COMPLETED'] as const;
export type BuilderEventName = typeof builderEvents[number];
export interface BuilderEvent { type: BuilderEventName; levelId: string; word: string; letter?: string; tokenId?: string; slot?: number; attempt: number; timestamp: number }
export class WordBuilderEventBus {
  private listeners = new Set<(event: BuilderEvent) => void>();
  subscribe(listener: (event: BuilderEvent) => void) { this.listeners.add(listener); return () => { this.listeners.delete(listener); }; }
  emit(event: BuilderEvent) { this.listeners.forEach(fn => fn(event)); }
  clear() { this.listeners.clear(); }
}
