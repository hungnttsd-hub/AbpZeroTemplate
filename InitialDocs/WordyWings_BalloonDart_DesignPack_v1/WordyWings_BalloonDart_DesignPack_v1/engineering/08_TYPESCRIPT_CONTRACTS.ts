// Reference contracts for Codex. Adapt names to the existing project type system.

export type BalloonDartInputMode = 'drag' | 'tap' | 'both';
export type BalloonDartLearningMode =
  | 'pictureListening'
  | 'wordRecognition'
  | 'colorRecognition'
  | 'compoundListening'
  | 'sizeAndShape';

export type BalloonMovementPattern =
  | 'gentleBob'
  | 'driftHorizontal'
  | 'driftVertical'
  | 'figureEight'
  | 'crossLane';

export interface BalloonSemanticTarget {
  id: string;
  word?: string;
  imageKey?: string;
  shape?: 'circle' | 'star' | 'square' | 'triangle' | 'heart' | string;
  color?: string;
  size?: 'small' | 'medium' | 'big';
  audioKey?: string;
}

export interface BalloonMovementConfig {
  pattern: BalloonMovementPattern;
  amplitudeX?: number;
  amplitudeY?: number;
  periodMs?: number;
  speed?: number;
  phase?: number;
  lane?: number;
}

export interface BalloonOptionConfig {
  instanceId: string;
  semantic: BalloonSemanticTarget;
  movement: BalloonMovementConfig;
  initialPosition?: { x: number; y: number };
  balloonSkin?: string;
  labelMode?: 'visible' | 'fade' | 'hidden';
}

export interface BalloonDartTargetRule {
  semanticId?: string;
  requiredWord?: string;
  requiredShape?: string;
  requiredColor?: string;
  requiredSize?: string;
}

export interface BalloonDartRoundConfig {
  id: string;
  instructionText: string;
  instructionAudioKey: string;
  target: BalloonDartTargetRule;
  balloons: BalloonOptionConfig[];
  aimAssist: 'long' | 'medium' | 'short';
  seed?: number;
}

export interface BalloonDartLevelConfig {
  id: string;
  mechanic: 'balloon_dart';
  title?: string;
  difficulty: 1 | 2 | 3 | 4 | 5;
  learningMode: BalloonDartLearningMode;
  inputMode: BalloonDartInputMode;
  rounds: BalloonDartRoundConfig[];
  reward: { stars: 3 };
}

export interface BalloonDartLevelResult {
  levelId: string;
  mechanic: 'balloon_dart';
  roundsCompleted: number;
  shots: number;
  wrongHits: number;
  misses: number;
  hintCount: number;
  durationSeconds: number;
  stars: 3;
}
