import type { HideSeekLevel, HideSeekState, ToolId, VocabularyEntity } from './contracts';

export function createState(level: HideSeekLevel, catalog: readonly VocabularyEntity[]): HideSeekState {
  const entityIds = new Set(catalog.map(e => e.id));
  if (entityIds.size !== catalog.length) throw new Error('Duplicate vocabulary entity ids');
  if (!entityIds.has(level.targetEntityId)) throw new Error('Missing target entity');
  if (level.spots.filter(s => s.entityId === level.targetEntityId).length !== 1)
    throw new Error('Exactly one target spot is required');
  if (new Set(level.spots.map(s => s.id)).size !== level.spots.length)
    throw new Error('Duplicate hiding spot ids');
  if (level.initialStars !== 3 || level.spots.length < 2)
    throw new Error('Invalid initial stars or spot count');
  for (const spot of level.spots) {
    if (!entityIds.has(spot.entityId)) throw new Error('Missing occupant entity');
    if (!spot.allowedTools.length || !spot.allowedTools.some(t => level.tools.includes(t)))
      throw new Error('Unreachable hiding spot');
  }
  return { phase: 'searching', paused: false, stars: 3, pending: null, feedback: null,
    usedRevealIds: [], revealedSpotIds: [], resolvedSpotIds: [], chargedRevealIds: [],
    answeredRevealIds: [], processedAnswerIds: [], attempts: [] };
}

export function startReveal(state: HideSeekState, level: HideSeekLevel,
  spotId: string, tool: ToolId, revealId: string): HideSeekState {
  if (state.paused || state.phase !== 'searching' || !revealId ||
      state.usedRevealIds.includes(revealId) || state.resolvedSpotIds.includes(spotId)) return state;
  const spot = level.spots.find(s => s.id === spotId);
  if (!spot || !level.tools.includes(tool) || !spot.allowedTools.includes(tool)) return state;
  return { ...state, phase: 'revealing', pending: { id: revealId, spotId },
    usedRevealIds: [...state.usedRevealIds, revealId] };
}

/** Call once the cover animation is finished, never just because pointerdown fired. */
export function finishReveal(state: HideSeekState, revealId: string): HideSeekState {
  if (state.paused || state.phase !== 'revealing' || state.pending?.id !== revealId) return state;
  return { ...state, phase: 'asking',
    revealedSpotIds: [...new Set([...state.revealedSpotIds, state.pending.spotId])] };
}

export function submitAnswer(state: HideSeekState, level: HideSeekLevel,
  catalog: readonly VocabularyEntity[], answer: boolean, answerEventId: string): HideSeekState {
  if (state.paused || !state.pending || !answerEventId || state.processedAnswerIds.includes(answerEventId) ||
      (state.phase !== 'asking' && state.phase !== 'assisted_retry')) return state;
  const pending = state.pending;
  const spot = level.spots.find(s => s.id === pending.spotId);
  const occupant = catalog.find(e => e.id === spot?.entityId);
  if (!spot || !occupant) throw new Error('Invalid pending reveal; validate content before loading');
  const isTarget = spot.entityId === level.targetEntityId;
  const correct = answer === isTarget;
  const assisted = state.phase === 'assisted_retry';
  const charge = !correct && !state.chargedRevealIds.includes(pending.id);
  const deltaStars: 0 | -1 = charge && state.stars > 0 ? -1 : 0;
  const nextPhase = isTarget ? (correct ? 'completed' : 'assisted_retry') : 'searching';
  const phrase = isTarget ? occupant.affirmative : occupant.negativeIdentification;
  return { ...state, phase: 'feedback', stars: Math.max(0, state.stars + deltaStars),
    feedback: { answerEventId, correct, assisted, deltaStars, phrase, nextPhase },
    chargedRevealIds: charge ? [...state.chargedRevealIds, pending.id] : state.chargedRevealIds,
    answeredRevealIds: [...new Set([...state.answeredRevealIds, pending.id])],
    processedAnswerIds: [...state.processedAnswerIds, answerEventId],
    attempts: [...state.attempts, { answerEventId, revealId: pending.id, spotId: spot.id,
      entityId: spot.entityId, answer, correct, assisted }] };
}

/** Called by Continue, or once the feedback clip finishes. Never call it twice from two sources. */
export function acknowledgeFeedback(state: HideSeekState): HideSeekState {
  if (state.paused || state.phase !== 'feedback' || !state.feedback || !state.pending) return state;
  const phase = state.feedback.nextPhase;
  if (phase === 'assisted_retry') return { ...state, phase, feedback: null };
  return { ...state, phase, feedback: null, pending: null,
    resolvedSpotIds: [...new Set([...state.resolvedSpotIds, state.pending.spotId])] };
}
export function setPaused(state: HideSeekState, paused: boolean): HideSeekState {
  return { ...state, paused };
}
export function independentCorrectCount(state: HideSeekState): number {
  return state.attempts.filter(a => !a.assisted && a.correct).length;
}
