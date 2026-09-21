import type { GameMechanic, MechanicContext } from '../mechanics';
import type { LevelDefinition } from '../../types';
import { WordBuilderManager } from './WordBuilderManager';
export class WordBuilderSceneController implements GameMechanic {
  readonly id = 'word_builder' as const;
  private manager?: WordBuilderManager;
  mount(ctx: MechanicContext, level: LevelDefinition) { this.manager = new WordBuilderManager(ctx, level); }
  dispose() { this.manager?.dispose(); }
}
