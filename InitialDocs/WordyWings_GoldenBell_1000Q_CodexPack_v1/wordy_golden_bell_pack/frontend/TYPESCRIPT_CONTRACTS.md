# TypeScript Contracts — Golden Bell

```ts
export type GoldenBellMechanic =
  | 'answer_zone'
  | 'raise_board'
  | 'listen_run'
  | 'quick_match'
  | 'bell_choice';

export type GoldenBellAnswer =
  | { type: 'option'; value: string; sequence?: string[]; missingPositions?: number[] }
  | { type: 'sequence'; value: string[] };

export interface GoldenBellOption {
  id: string;
  label: string;
  assetKey?: string;
  emoji?: string;
  displayColor?: string;
  size?: string;
  scene?: Record<string, unknown>;
  sequence?: string[];
  assets?: string[];
}

export interface GoldenBellQuestion {
  id: string;
  difficulty: number;
  questionType: string;
  mechanic: GoldenBellMechanic;
  skill: string;
  promptText: string;
  audioText: string;
  stimulus: Record<string, unknown>;
  options: GoldenBellOption[];
  answer: GoldenBellAnswer;
  hint: {
    afterWrong: number;
    text: string;
    strategy: string;
  };
  explanation: string;
  targetVocabulary: string[];
  worldTags: string[];
  estimatedSeconds: number;
  requiresMemoryPhase: boolean;
  notes: string;
}

export interface QuestionRenderer {
  mount(question: GoldenBellQuestion, context: GoldenBellRenderContext): void;
  submit(input: unknown): GoldenBellSubmitResult;
  showHint(level: number): void;
  dispose(): void;
}
```

## Rule
Frontend renderer không được biết Question ID cụ thể. Chỉ render dựa trên `questionType`, `stimulus`, `options`, `answer` và config.
