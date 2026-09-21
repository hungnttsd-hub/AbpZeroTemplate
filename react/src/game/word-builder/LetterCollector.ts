import type { LetterToken } from './LetterToken';
import type { WordBuilderEffects } from './WordBuilderEffects';
export class LetterCollector {
  constructor(private effects: WordBuilderEffects, private collected: (token: LetterToken) => void) {}
  collect(token: LetterToken) {
    if (token.state !== 'hidden') return;
    if (token.definition.spawn === 'falling' && !this.effects.reduced) {
      token.fall(() => { this.effects.bounce(token.view); this.collected(token); });
    } else { token.reveal(); this.effects.sparkle(token.view.x, token.view.y); this.effects.bounce(token.view); this.collected(token); }
  }
}
