import type { LetterToken } from './LetterToken';
import type { WordBuilderEffects } from './WordBuilderEffects';
import type Phaser from 'phaser';
export class LetterCollector {
  private props = new Set<Phaser.GameObjects.GameObject>();
  constructor(private effects: WordBuilderEffects, private collected: (token: LetterToken) => void) {}
  collect(token: LetterToken, pulled = false): boolean {
    if (token.state !== 'hidden') return false;
    const kind = token.definition.spawn;
    token.touches++;
    const needed = kind === 'crate' || kind === 'obstacle' || kind === 'pulley' ? 3 : kind === 'spring' || kind === 'bush' ? 2 : 1;
    if (!pulled && token.touches < needed) {
      if (kind === 'spring') { token.motion?.stop(); token.view.setScale(1.06, .82); token.helper.setText('Chạm lần nữa để bật!'); }
      else { this.effects.shake(token.view); token.cover.setAlpha(1 - token.touches * .23); token.prop?.setAlpha(1 - token.touches * .23); token.helper.setText(kind === 'pulley' ? 'Kéo dây xuống nhé!' : `Còn ${needed - token.touches} chạm`); }
      return false;
    }
    token.motion?.stop();
    if (kind === 'balloon') {
      token.state = 'flying';
      const dart = this.effects.scene.add.triangle(token.view.x - 50, token.view.y + 65, 0, 15, 6, 0, 12, 15, 0xe9b75e).setDepth(40);
      this.props.add(dart);
      this.effects.tween({ targets: dart, x: token.view.x, y: token.view.y - 55, duration: this.effects.reduced ? 100 : 260, onComplete: () => { dart.destroy(); this.props.delete(dart); token.reveal(); this.effects.sparkle(token.view.x, token.view.y - 55); this.collected(token); } });
      return true;
    }
    if (kind === 'spring' || kind === 'pulley') {
      token.state = 'flying';
      this.effects.tween({ targets: token.view, y: token.view.y - (this.effects.reduced ? 0 : 48), scaleY: 1, duration: this.effects.reduced ? 100 : 330, onComplete: () => { token.reveal(); this.collected(token); } });
      return true;
    }
    if (token.definition.spawn === 'falling' && !this.effects.reduced) {
      token.fall(() => { this.effects.bounce(token.view); this.collected(token); });
    } else { token.reveal(); this.effects.sparkle(token.view.x, token.view.y); this.effects.bounce(token.view); this.collected(token); }
    return true;
  }
  dispose() { this.props.forEach(p => p.destroy()); this.props.clear(); }
}
