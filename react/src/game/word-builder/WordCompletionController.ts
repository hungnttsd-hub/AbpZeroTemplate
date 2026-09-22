import Phaser from 'phaser';
import { woodPanel } from '../theme';
import type { MechanicContext } from '../mechanics';
import type { WordBuilderConfig } from './config';
import type { CompletionAction } from './CompletionActions';
import type { WordBuilderEffects } from './WordBuilderEffects';
import type { LetterSlot } from './LetterSlot';
import type { BuilderEventName } from './events';
export class WordCompletionController {
  private disposed = false;
  private started = false;
  private continueButton?: Phaser.GameObjects.Container;
  constructor(private ctx: MechanicContext, private effects: WordBuilderEffects, private emit: (event: BuilderEventName) => void) {}
  start(config: WordBuilderConfig, slots: LetterSlot[], action: CompletionAction) {
    if (this.started) return; this.started = true;
    this.emit('WORD_COMPLETED');
    this.ctx.feedback('Từ đã hoàn thành! Xem điều kỳ diệu xảy ra nhé.');
    action.setMeaningVisible(true);
    let actionDone = false; let voiceDone = false;
    const ready = () => { if (actionDone && voiceDone && !this.disposed) this.showContinue(); };
    const units = config.unit === 'word' ? config.targetWord.split(' ') : [...config.targetWord];
    void this.ctx.audio.speakSequence([...units, config.targetWord], index => {
      if (this.disposed) return;
      if (index < slots.length) { slots[index].glow(); this.effects.bounce(slots[index].view); }
      else slots.forEach(slot => slot.glow());
    }).finally(() => {
      if (this.disposed) return;
      voiceDone = true; this.emit('COMPLETION_ACTION_STARTED');
      action.play(() => { if (this.disposed) return; this.emit('COMPLETION_ACTION_FINISHED'); actionDone = true; void this.ctx.audio.enqueueSpeech(config.example); ready(); });
    });
  }
  private showContinue() {
    const s = this.ctx.scene;
    const background = woodPanel(s, 300, 82, 0x398d77);
    const text = s.add.text(0, 0, '★  Nhận ngôi sao  →', { fontFamily: 'Nunito, Arial', fontSize: '26px', color: '#ffffff', fontStyle: 'bold' }).setOrigin(.5).setResolution(2);
    this.continueButton = s.add.container(s.scale.width / 2, s.scale.height - 58, [background, text]).setSize(300, 82).setDepth(60).setInteractive({ useHandCursor: true });
    this.continueButton.once('pointerdown', () => { if (this.disposed) return; this.continueButton?.disableInteractive(); this.emit('LEVEL_COMPLETED'); this.ctx.complete(); });
    this.ctx.feedback('Cảm ơn bé! Chạm Nhận ngôi sao để tiếp tục.');
  }
  resize() { this.continueButton?.setPosition(this.ctx.scene.scale.width / 2, this.ctx.scene.scale.height - 58); }
  dispose() { this.disposed = true; this.continueButton?.destroy(); }
}
