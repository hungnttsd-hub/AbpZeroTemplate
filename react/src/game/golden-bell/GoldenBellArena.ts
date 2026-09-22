import Phaser from 'phaser';
import { AssetResolver, AudioService } from '../services';
import { artUrl, character, prepareTheme } from '../theme';
import { bellSvg, cardPanel, queueQuestionArt, svgData } from './art';
import { vocabularyKey, vocabularySvg } from './vocabularyArt';
import { rendererRegistry, label, type QuestionRenderer } from './renderers';
import type { GoldenBellStore } from './store';
import { questionNames, type AnswerInput } from './model';

export interface ArenaEvents { status: (text: string) => void; prompt: (text: string) => void; error: (text: string) => void; changed: () => void; ready: (scene: GoldenBellArena) => void }
/** One persistent arena; question-specific objects are owned by the renderer registry. */
export class GoldenBellArena extends Phaser.Scene {
  private audio: AudioService;
  private questionRenderer?: QuestionRenderer;
  private panel?: Phaser.GameObjects.Container;
  private questionLabel!: Phaser.GameObjects.Text;
  private promptLabel!: Phaser.GameObjects.Text;
  private bell!: Phaser.GameObjects.Image;
  private energy: Phaser.GameObjects.Text[] = [];
  private helper!: Phaser.GameObjects.Image;
  private toast!: Phaser.GameObjects.Text;
  private transient: Phaser.GameObjects.GameObject[] = [];
  private answerTimer?: Phaser.Time.TimerEvent;
  private hintTimer?: Phaser.Time.TimerEvent;
  private busy = false;
  private dead = false;
  private elapsed = 0;
  private hinted = false;
  private reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
  private retryAction?: () => Promise<void>;
  private ropeY?: number;
  private particles: Array<{ sprite: Phaser.GameObjects.Text; life: number; vx: number; vy: number }> = [];
  private get session() { return this.store.session!; }
  private get compact() { return this.scale.width < 1000; }
  private get center() { return this.compact ? 450 : 620; }
  private get feedbackY() { return this.compact ? 1242 : 838; }
  constructor(private store: GoldenBellStore, muted: boolean, private callbacks: ArenaEvents) {
    super('GoldenBellArena'); this.audio = new AudioService(new AssetResolver(), muted, () => this.feedback('Thiết bị chưa có giọng đọc. Bé vẫn có thể chơi bằng hình và chữ.'));
  }
  preload() {
    this.load.image('gb:backdrop', artUrl('golden-bell-island-v1.png'));
    this.load.image('art:friends', artUrl('friends-v1.png')); this.load.image('art:props', artUrl('props-v1.png'));
    this.load.svg('gb:bell', svgData(bellSvg), { scale: 1.5 });
    this.load.svg(vocabularyKey('vocab_foxy'), svgData(vocabularySvg('vocab_foxy')));
    for (const question of this.session.questions) queueQuestionArt(this, question);
  }
  create() {
    prepareTheme(this);
    const { width, height } = this.scale;
    const background = this.add.image(width / 2, height / 2, 'gb:backdrop');
    background.setScale(Math.max(width / background.width, height / background.height)).setDepth(-10);
    const boardWidth = this.compact ? 846 : 1164, boardHeight = this.compact ? 1035 : 644;
    this.add.graphics().fillStyle(0xfff9e7, .92).fillRoundedRect(this.center - boardWidth / 2, this.compact ? 155 : 111, boardWidth, boardHeight, 34).lineStyle(3, 0xd9c594).strokeRoundedRect(this.center - boardWidth / 2, this.compact ? 155 : 111, boardWidth, boardHeight, 34);
    this.bell = this.add.image(this.compact ? 793 : 1410, this.compact ? 77 : 382, 'gb:bell').setDisplaySize(this.compact ? 91 : 278, this.compact ? 119 : 363).setDepth(4);
    this.helper = character(this, 'pip', this.compact ? 124 : 1390, this.compact ? 1240 : 675, this.compact ? 94 : 197).setDepth(7);
    if (!this.compact) this.add.text(1400, 546, 'STAR BELL', { fontFamily: 'Nunito', fontSize: '20px', fontStyle: 'bold', color: '#5c8071', letterSpacing: 4 }).setOrigin(.5);
    this.questionLabel = label(this, this.center, this.compact ? 187 : 136, '', 19).setColor('#8a936c');
    this.promptLabel = label(this, this.center, this.compact ? 265 : 205, '', this.compact ? 30 : 29, boardWidth - 74);
    this.toast = label(this, this.compact ? 476 : 790, this.feedbackY, '', 22, this.compact ? 625 : 1120).setDepth(20);
    for (let i = 0; i < 12; i++) {
      const x = this.compact ? 75 + i * 55 : 133 + i * 85;
      const star = label(this, x, this.compact ? 93 : 58, '★', this.compact ? 32 : 37).setColor('#d1d6bd'); this.energy.push(star);
      label(this, x, this.compact ? 124 : 90, String(i + 1), 14).setColor('#79927b');
    }
    for (let i = 0; i < (this.reduced ? 8 : 45); i++) this.particles.push({ sprite: label(this, 0, 0, i % 3 ? '✦' : '★', 22).setColor(i % 2 ? '#edbf54' : '#a3c9aa').setVisible(false).setDepth(25), life: 0, vx: 0, vy: 0 });
    const keyboard = (event: KeyboardEvent) => { if (this.busy || !this.sys.isActive() || event.target instanceof HTMLButtonElement) return; this.questionRenderer?.key(event.key); if (['Backspace', 'Enter'].includes(event.key)) event.preventDefault(); };
    this.input.keyboard?.on('keydown', keyboard);
    this.events.once('shutdown', () => {
      this.dead = true; this.questionRenderer?.dispose(); this.audio.stopAll(); this.input.keyboard?.off('keydown', keyboard); this.answerTimer?.remove(false); this.hintTimer?.remove(false);
    });
    this.bell.setInteractive({ useHandCursor: true });
    this.bell.on('pointerdown', (p: Phaser.Input.Pointer) => { if (this.session.phase === 'bell') this.ropeY = p.y; });
    this.bell.on('pointerup', (p: Phaser.Input.Pointer) => { if (this.ropeY !== undefined && (Math.abs(p.y - this.ropeY) < 12 || p.y - this.ropeY > 28)) this.ring(); this.ropeY = undefined; });
    this.callbacks.ready(this); void this.mountPhase();
  }
  setMuted(muted: boolean) { this.audio.muted = muted; if (muted) this.audio.stopAll(); }
  setPaused(paused: boolean) { if (paused) { this.audio.stopAll(); this.scene.pause(); } else this.scene.resume(); }
  replay() { this.questionRenderer?.replay(); }
  async speak(texts: string[]) {
    // Short utterances avoid the shared voice watchdog cutting a long story in half.
    const parts = texts.flatMap(text => text.replace(/_+/g, 'blank').split(/(?<=[.!?])\s+/)).flatMap(text => {
      const words = text.split(/\s+/), result: string[] = []; while (words.length) result.push(words.splice(0, 12).join(' ')); return result;
    });
    await this.audio.speakSequence(parts);
  }
  private feedback(text: string) { if (this.dead) return; this.toast?.setText(text); this.callbacks.status(text); }
  private prompt(text: string) { this.promptLabel.setText(text); this.callbacks.prompt(text); }
  private async safe(action: () => Promise<void>): Promise<boolean> {
    if (this.dead) return false; this.busy = true;
    try { await action(); if (!this.dead) { this.callbacks.error(''); this.callbacks.changed(); } this.retryAction = undefined; return true; }
    catch (error) { this.retryAction = action; this.callbacks.error(error instanceof Error ? error.message : 'Chưa lưu được lượt chơi. Hãy thử lại.'); return false; }
    finally { this.busy = false; }
  }
  retry() { if (this.retryAction) void this.safe(this.retryAction).then(ok => { if (ok) void this.mountPhase(); }); }
  private clearLayer() {
    this.questionRenderer?.dispose(); this.questionRenderer = undefined; this.panel?.destroy(true); this.panel = undefined;
    this.answerTimer?.remove(false); this.hintTimer?.remove(false); this.transient.forEach(o => { this.tweens.killTweensOf(o); o.destroy(); }); this.transient = [];
  }
  private async mountPhase() {
    if (this.dead) return;
    this.clearLayer(); this.energy.forEach((star, i) => star.setColor(this.session.records[i].completed ? '#dcae44' : '#c9cfb5'));
    this.bell.setPosition(this.compact ? 793 : 1410, this.compact ? 77 : 382).setDisplaySize(this.compact ? 91 : 278, this.compact ? 119 : 363);
    this.questionLabel.setText('ĐUỔI HÌNH BẮT CHỮ · CHUÔNG SAO'); this.prompt('');
    if (this.session.phase === 'intro') {
      this.prompt('Đánh thức Chuông Sao!');
      this.messageCard('Mr. Mumble làm chuông mất tiếng.\nCùng các bạn tìm 12 Word Stars nhé!', 'Bắt đầu khám phá', () => void this.safe(() => this.store.begin()).then(ok => { if (ok) void this.mountPhase(); }));
      this.feedback('Nghe · Nhìn · Khám phá. Sai cũng không sao!'); return;
    }
    if (this.session.phase === 'checkpoint') {
      this.prompt(this.session.index === 3 ? 'Cây cầu sao đã sáng!' : 'Tháp chuông đang thức dậy!');
      this.messageCard(`Đã tìm được ${this.session.index + 1} Word Stars.\nCả đội cùng đi tiếp nào!`);
      this.sparkles(this.center, this.compact ? 565 : 450);
      this.answerTimer = this.time.delayedCall(2200, () => void this.safe(() => this.store.advance()).then(ok => { if (ok) void this.mountPhase(); })); return;
    }
    if (this.session.phase === 'bell' || this.session.phase === 'summary') {
      this.prompt(this.session.bellRung ? 'Chuông Sao đã thức dậy!' : 'Đủ 12 ngôi sao rồi!');
      this.bell.setPosition(this.center, this.compact ? 660 : 447).setDisplaySize(this.compact ? 333 : 276, this.compact ? 435 : 360).setDepth(12);
      if (!this.session.bellRung) this.phaseButton(this.center, this.compact ? 1040 : 704, 'Kéo dây hoặc chạm để rung chuông', () => this.ring(), this.compact ? 696 : 694);
      this.feedback(this.session.bellRung ? 'Great job! Một Bell Token dành cho bé.' : 'Chính bé sẽ đánh thức Chuông Sao!'); return;
    }
    const question = this.session.questions[this.session.index];
    const pending = queueQuestionArt(this, question);
    if (pending) {
      this.busy = true;
      await new Promise<void>(resolve => { this.load.once('complete', resolve); this.load.start(); });
      this.busy = false; if (this.dead) return;
    }
    this.elapsed = 0; this.hinted = this.session.records[this.session.index].hintUsed;
    this.questionLabel.setText(`CÂU ${this.session.index + 1} / 12   ·   ${questionNames[question.questionType]}   ·   ĐỘ KHÓ ${question.difficulty}`);
    this.prompt(question.promptText); this.feedback('Chạm hình để chọn · Có thể nghe lại bất cứ lúc nào');
    this.questionRenderer = rendererRegistry[question.questionType](question, { scene: this, audio: this.audio, reduced: this.reduced,
      locked: () => this.busy || this.session.phase !== 'question', record: () => this.session.records[this.session.index],
      submit: (input, x, y) => void this.submit(input, x, y),
      remember: patch => this.safe(() => this.store.remember(patch)),
      replayMemory: async () => { if (this.session.records[this.session.index].memoryReplays > 0) return false; return this.safe(() => this.store.hint(true)); },
      prompt: value => this.prompt(value), feedback: value => this.feedback(value), speak: texts => this.speak(texts),
    });
    this.questionRenderer.mount();
    if (this.session.records[this.session.index].completed) {
      this.questionRenderer.reveal(); this.answerTimer = this.time.delayedCall(600, () => void this.next()); return;
    }
    this.hintTimer = this.time.delayedCall(Math.max(100, (question.difficulty > 6 ? 18000 : 14000) - this.session.records[this.session.index].durationMs), () => this.hint());
  }
  private async submit(input: AnswerInput, x = this.center, y = 560) {
    if (this.busy || this.session.phase !== 'question' || this.session.records[this.session.index].completed) return;
    const q = this.session.questions[this.session.index];
    let result: { correct: boolean; hint: boolean } | undefined;
    const saved = await this.safe(async () => { result = await this.store.recordInput(input, this.elapsed); this.elapsed = 0; });
    if (!saved || !result || this.dead) return;
    const outcome = result as { correct: boolean; hint: boolean };
    if (outcome.correct) {
      this.busy = true; this.hintTimer?.remove(false); this.questionRenderer?.reveal(); this.sparkles(x, y);
      this.feedback('Đúng rồi! Một ngôi sao cho Chuông Sao.');
      if (!this.reduced) {
        const star = label(this, x, y, '★', 66).setColor('#f2c45d').setDepth(22); this.transient.push(star);
        this.tweens.add({ targets: star, x: this.bell.x, y: this.bell.y - 40, scale: .35, alpha: .1, duration: 780, ease: 'Cubic.easeIn' });
        if (q.mechanic === 'listen_run') this.tweens.add({ targets: this.helper, x: Math.min(this.compact ? 745 : 1380, Math.max(100, x)), duration: 580, yoyo: true, ease: 'Sine.easeInOut' });
      }
      if (q.mechanic === 'raise_board') { const board = cardPanel(this, this.compact ? 250 : 205, 83).setPosition(this.helper.x, this.helper.y - 23).setDepth(14); const text = label(this, this.helper.x, this.helper.y - 26, q.answer.type === 'option' ? q.options.find(o => o.id === q.answer.value)?.label ?? 'Great!' : 'Great!', 22, 182).setDepth(15); this.transient.push(board, text); }
      const words = q.questionType === 'missing_letter' ? [...q.targetVocabulary[0].toUpperCase(), q.targetVocabulary[0], 'Great!'] : ['Great!', q.explanation];
      await this.speak(words);
      if (this.dead) return;
      this.answerTimer = this.time.delayedCall(600, () => void this.next());
    } else {
      this.feedback('Almost! Thử thêm một lần nữa nhé.'); void this.speak(['Almost. Try again!']);
      if (!this.reduced) this.tweens.add({ targets: this.helper, angle: { from: -4, to: 4 }, duration: 90, yoyo: true, repeat: 1, onComplete: () => this.helper.setAngle(0) });
      if (outcome.hint) { this.hinted = true; this.questionRenderer?.showHint(this.session.records[this.session.index].wrong); }
    }
  }
  private async next() { if (await this.safe(() => this.store.advance())) await this.mountPhase(); }
  hint() {
    if (this.busy || this.session.phase !== 'question' || this.session.records[this.session.index].completed) return;
    const r = this.session.records[this.session.index];
    const q = this.session.questions[this.session.index];
    if (r.wrong < q.hint.afterWrong && r.durationMs + this.elapsed < (q.difficulty > 6 ? 18000 : 14000) - 150) {
      this.feedback('Bé thử trước nhé. Chuông Sao sẽ gợi ý sau một chút!'); return;
    }
    void this.safe(() => this.store.hint()).then(ok => { if (ok) { this.hinted = true; this.questionRenderer?.showHint(Math.max(2, r.wrong)); } });
  }
  ring() {
    if (this.busy || this.session.phase !== 'bell') return;
    void this.safe(() => this.store.ring()).then(ok => {
      if (!ok || this.dead) return;
      this.clearLayer(); this.prompt('Chuông Sao đã thức dậy!'); this.sparkles(this.center, this.compact ? 530 : 405, true);
      if (!this.reduced) this.tweens.add({ targets: this.bell, angle: { from: -10, to: 10 }, duration: 170, yoyo: true, repeat: 4, onComplete: () => this.bell.setAngle(0) });
      this.bellSound(); this.feedback('Bé đã nhận một Bell Token!'); void this.speak(['Wonderful! You woke up the Star Bell!']);
      this.callbacks.changed();
    });
  }
  private bellSound() {
    if (this.audio.muted || !('AudioContext' in window)) return;
    const context = new AudioContext();
    void context.resume().then(() => { for (const [frequency, gainValue] of [[660, .13], [1320, .05], [1760, .025]]) { const wave = context.createOscillator(), gain = context.createGain(); wave.frequency.value = frequency; gain.gain.setValueAtTime(gainValue, context.currentTime); gain.gain.exponentialRampToValueAtTime(.0001, context.currentTime + 2); wave.connect(gain); gain.connect(context.destination); wave.start(); wave.stop(context.currentTime + 2.1); } setTimeout(() => void context.close(), 2300); }).catch(() => void context.close());
  }
  private messageCard(text: string, button?: string, action?: () => void) {
    const y = this.compact ? 629 : 438;
    this.panel = this.add.container(this.center, y, [cardPanel(this, this.compact ? 727 : 865, 296), label(this, 0, -40, text, 32, this.compact ? 650 : 785)]).setDepth(15);
    this.panel.add(character(this, 'momo', this.compact ? 249 : 321, 66, 123));
    if (button && action) this.phaseButton(this.center - 47, y + 74, button, action);
  }
  private phaseButton(x: number, y: number, text: string, action: () => void, width = 340) {
    const button = this.add.container(x, y, [cardPanel(this, width, 97, true), label(this, 0, -3, text, 27, width - 30)]).setDepth(18).setSize(width, 97).setInteractive({ useHandCursor: true });
    button.on('pointerdown', () => { if (!this.busy) action(); }); this.transient.push(button);
  }
  private sparkles(x: number, y: number, all = false) {
    this.particles.forEach((p, i) => { if (!all && i > 18) return; p.life = this.reduced ? .2 : .9 + i % 7 * .1; p.vx = this.reduced ? 0 : Math.cos(i * 2.4) * (all ? 390 : 175); p.vy = this.reduced ? 0 : Math.sin(i * 2.4) * 165 - 100; p.sprite.setPosition(x, y).setAlpha(1).setVisible(true).setScale(.7 + i % 3 * .25); });
  }
  update(_time: number, delta: number) {
    const dt = Math.min(delta, 80) / 1000;
    if (this.session.phase === 'question' && !this.session.records[this.session.index].completed) {
      this.elapsed += Math.min(delta, 100);
      if (this.elapsed >= 5000 && !this.busy) {
        const elapsed = this.elapsed; this.elapsed = 0;
        void this.safe(() => this.store.remember({}, elapsed));
      }
    }
    for (const p of this.particles) if (p.life > 0) { p.life -= dt; p.vy += this.reduced ? 0 : 125 * dt; p.sprite.x += p.vx * dt; p.sprite.y += p.vy * dt; p.sprite.setAlpha(Math.max(0, Math.min(1, p.life))); if (p.life <= 0) p.sprite.setVisible(false); }
  }
}
