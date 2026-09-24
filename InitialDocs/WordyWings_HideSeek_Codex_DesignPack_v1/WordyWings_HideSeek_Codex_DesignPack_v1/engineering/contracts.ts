/** Engine-independent contract. Adapt to the existing repository; do not replace its architecture. */
export type ToolId = 'swipe_leaves' | 'throw_net' | 'blast_berry';
export type SpotKind = 'tree_hollow' | 'bush' | 'rock' | 'tall_grass' | 'hollow_log';
export type Phase = 'searching' | 'revealing' | 'asking' | 'feedback' | 'assisted_retry' | 'completed';
export interface Rect { x: number; y: number; width: number; height: number }
export interface VocabularyEntity {
  id: string;
  label: string;
  grammar: 'singular' | 'plural';
  assetKey: string;
  quest: string;
  question: string;
  affirmative: string;
  negativeIdentification: string;
}
export interface HidingSpot {
  id: string;
  kind: SpotKind;
  entityId: string;
  allowedTools: readonly ToolId[];
  anchor: Rect;
  coverAssetKey: string;
}
export interface HideSeekLevel {
  id: string;
  mechanic: 'hide_seek';
  contentVersion: string;
  targetEntityId: string;
  difficulty: 1 | 2 | 3;
  initialStars: 3;
  shuffleOccupants: boolean;
  tools: readonly ToolId[];
  spots: readonly HidingSpot[];
}
export interface PendingReveal { id: string; spotId: string }
export interface Feedback {
  answerEventId: string;
  correct: boolean;
  assisted: boolean;
  deltaStars: 0 | -1;
  phrase: string;
  nextPhase: 'searching' | 'assisted_retry' | 'completed';
}
export interface LearningAttempt {
  answerEventId: string;
  revealId: string;
  spotId: string;
  entityId: string;
  answer: boolean;
  correct: boolean;
  assisted: boolean;
}
export interface HideSeekState {
  phase: Phase;
  paused: boolean;
  stars: number;
  pending: PendingReveal | null;
  feedback: Feedback | null;
  usedRevealIds: readonly string[];
  revealedSpotIds: readonly string[];
  resolvedSpotIds: readonly string[];
  chargedRevealIds: readonly string[];
  answeredRevealIds: readonly string[];
  processedAnswerIds: readonly string[];
  attempts: readonly LearningAttempt[];
}
/** Proposed persistence envelope; map this through the existing progress adapter. */
export interface ProgressEnvelope {
  schemaVersion: '1.0.0';
  eventId: string;
  sessionId: string;
  levelId: string;
  contentVersion: string;
  seed: number;
  status: 'checkpoint' | 'completed';
  starsRemaining: number;
  activeDurationMs: number;
  attempts: readonly LearningAttempt[];
}
