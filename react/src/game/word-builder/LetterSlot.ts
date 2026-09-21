import Phaser from 'phaser';
export class LetterSlot {
  readonly view: Phaser.GameObjects.Container;
  private background: Phaser.GameObjects.Rectangle;
  private label: Phaser.GameObjects.Text;
  constructor(scene: Phaser.Scene, readonly index: number, ghost: string, value: string | null) {
    this.background = scene.add.rectangle(0, 0, 108, 104, 0xe4ecdb).setStrokeStyle(3, 0xa2b996);
    this.label = scene.add.text(0, 0, value ?? ghost, { fontFamily: 'Nunito, Arial', fontStyle: 'bold', fontSize: `${ghost.length > 1 ? 26 : 48}px`, color: value ? '#3c6e52' : '#b8c9ac' }).setOrigin(.5);
    this.view = scene.add.container(0, 0, [this.background, this.label]).setSize(116, 116).setDepth(20).setInteractive({ useHandCursor: true });
  }
  fill(value: string) { this.label.setText(value).setColor('#3c6e52'); this.background.setFillStyle(0xfff2c6); }
  glow() { this.background.setStrokeStyle(6, 0xe4b65a); }
  contains(x: number, y: number) { return Math.abs(this.view.x - x) <= 64 && Math.abs(this.view.y - y) <= 64; }
  destroy() { this.view.destroy(); }
}
