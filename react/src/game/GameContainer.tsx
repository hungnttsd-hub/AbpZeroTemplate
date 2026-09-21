import { useEffect, useRef, useState } from 'react';
import Phaser from 'phaser';
import { Pause, Volume2, VolumeX, Play, RotateCcw, Map } from 'lucide-react';
import type { Attempt, LevelDefinition } from '../types';
import { wordTextureUrl } from './art';
import { AssetResolver, AttemptTracker, AudioService } from './services';
import { createMechanic, type GameMechanic } from './mechanics';
import { builderSize, resolveWordBuilder } from './word-builder/config';
import { resolveBalloonDart } from './balloon-dart/model';
import { palette as balloonPalette } from './balloon-dart/visuals';

interface Props { level: LevelDefinition; childId: string; muted: boolean; onMute: () => void; onComplete: (a: Attempt) => void; onExit: () => void }
export default function GameContainer({ level, childId, muted, onMute, onComplete, onExit }: Props) {
  const host = useRef<HTMLDivElement>(null);
  const game = useRef<Phaser.Game>(); const audio = useRef<AudioService>();
  const callbacks = useRef({ onComplete, onExit }); callbacks.current = { onComplete, onExit };
  const [paused, setPaused] = useState(false); const [restart, setRestart] = useState(0);
  const [instruction, setInstruction] = useState(level.instruction);
  const [feedback, setFeedback] = useState('Listen, look & play!'); const [error, setError] = useState('');
  const [audioUnavailable, setAudioUnavailable] = useState(false);
  const [ready, setReady] = useState(false);
  const isBuilder = level.mechanic === 'letter_puzzle' || level.mechanic === 'word_builder';
  const isBalloon = level.mechanic === 'balloon_pop' || level.mechanic === 'balloon_dart';
  const [canvasRatio, setCanvasRatio] = useState('16 / 9');
  const currentInstruction = useRef(instruction); currentInstruction.current = instruction;
  useEffect(() => {
    if (!host.current) return;
    const builderConfig = isBuilder ? resolveWordBuilder(level) : undefined;
    const balloonConfig = isBalloon ? resolveBalloonDart(level) : undefined;
    const builderWidth = () => host.current?.closest('.play-page')?.clientWidth ?? window.innerWidth;
    const size = builderConfig ? builderSize(builderConfig, builderWidth()) : isBalloon ? { width: 1440, height: 810 } : { width: 1600, height: 900 };
    setCanvasRatio(`${size.width} / ${size.height}`);
    let active = true; const assets = new AssetResolver();
    const sound = new AudioService(assets, muted, () => { if (active) setAudioUnavailable(true); }); audio.current = sound;
    const tracker = new AttemptTracker(); let mechanic: GameMechanic;
    setInstruction(level.instruction); setError(''); setPaused(false); setReady(false);
    const startupTimeout = window.setTimeout(() => {
      if (active) setError('Pip chưa tải được màn chơi. Bé thử mở lại nhé.');
    }, 20000);
    class LevelScene extends Phaser.Scene {
      constructor() { super('LevelScene'); }
      preload() {
        const terms = new Set([...level.targets.map(t => t.value), ...level.targetVocabulary, ...level.reviewVocabulary]);
        const balloonAssets = new Set<string>();
        if (builderConfig) terms.add(builderConfig.targetWord.toLowerCase());
        if (balloonConfig) for (const r of balloonConfig.rounds) for (const b of r.balloons) {
          terms.add(b.semantic.shape ?? b.semantic.word ?? b.semantic.id);
          if (b.semantic.shape && b.semantic.color && balloonPalette[b.semantic.color] !== undefined) {
            const key = `bd:shape:${b.semantic.shape}:${b.semantic.color}`;
            if (!balloonAssets.has(key)) { balloonAssets.add(key); this.load.svg(key, wordTextureUrl(b.semantic.shape, `#${balloonPalette[b.semantic.color].toString(16).padStart(6, '0')}`)); }
          }
          // Only explicit file paths are remote assets; vocabulary symbolic keys use original local art.
          if (b.semantic.imageKey?.includes('/') && !balloonAssets.has(`bd:${b.semantic.imageKey}`)) { balloonAssets.add(`bd:${b.semantic.imageKey}`); this.load.image(`bd:${b.semantic.imageKey}`, assets.image(b.semantic.imageKey)); }
        }
        for (const term of terms) this.load.svg(`word:${term}`, wordTextureUrl(term));
        if (builderConfig?.meaningAsset?.src) this.load.image('wb:meaning', assets.image(builderConfig.meaningAsset.src));
        if (assets.hasRemoteAssets) for (const target of level.targets) if (target.assetKey) this.load.image(`remote:${target.value}`, assets.image(target.assetKey));
      }
      create() {
        if (!active) return;
        try {
          this.cameras.main.setBackgroundColor('#eff7e9');
          this.add.ellipse(260, 840, 1500, 420, 0xdcebcf); this.add.ellipse(1420, 900, 1400, 530, 0xcfe5c5);
          const art = this.add.graphics().fillStyle(0xffffff, .72);
          [[180, 100], [1350, 160], [1150, 64]].forEach(([x, y]) => { art.fillEllipse(x, y, 150, 45).fillCircle(x - 20, y - 20, 40).fillCircle(x + 30, y - 10, 30); });
          // Optional production assets replace only successfully loaded textures.
          for (const t of level.targets) if (this.textures.exists(`remote:${t.value}`)) {
            const source = this.textures.get(`remote:${t.value}`).getSourceImage();
            this.textures.remove(`word:${t.value}`); this.textures.addImage(`word:${t.value}`, source as HTMLImageElement);
          }
          mechanic = createMechanic(level.mechanic);
          mechanic.mount({ scene: this, audio: sound, tracker, reducedMotion: matchMedia('(prefers-reduced-motion: reduce)').matches,
            instruction: text => { if (active) setInstruction(text); }, feedback: text => { if (active) setFeedback(text); },
            complete: () => { if (active) callbacks.current.onComplete(tracker.complete(childId, level.id)); }
          }, level);
          clearTimeout(startupTimeout);
          setReady(true); setError('');
          if (isBalloon && matchMedia('(orientation: portrait) and (max-width: 700px)').matches) { this.scene.pause(); sound.stopAll(); setPaused(true); }
          if (level.mechanic !== 'boss_challenge' && !isBuilder && !isBalloon) void sound.playInstruction(level);
          this.events.once('shutdown', () => { mechanic?.dispose(); sound.stopAll(); });
        } catch (e) { clearTimeout(startupTimeout); if (active) setError(e instanceof Error ? e.message : 'Pip chưa mở được màn này.'); }
      }
    }
    game.current = new Phaser.Game({ type: Phaser.AUTO, parent: host.current, width: size.width, height: size.height,
      physics: isBuilder || isBalloon ? { default: 'matter', matter: { gravity: { x: 0, y: isBalloon ? 0 : .8 }, debug: false } } : undefined,
      backgroundColor: '#eff7e9', scene: [LevelScene], render: { antialias: true },
      scale: { mode: Phaser.Scale.FIT, autoCenter: Phaser.Scale.CENTER_BOTH }, input: { activePointers: 2 },
      audio: { noAudio: true }, callbacks: { postBoot: g => { g.canvas.setAttribute('aria-label', level.mechanic === 'word_shot' || level.isBoss ? 'Wordy Wings. Kéo ná để ngắm và thả để bắn. Phím 1 đến 4 chọn đạn, A D di chuyển, mũi tên chỉnh góc và lực, Space bắn.' : isBalloon ? 'Wordy Wings. Kéo ngắm rồi thả để bắn, hoặc chạm bóng. Phím mũi tên chỉnh góc, Space bắn.' : isBuilder ? 'Wordy Wings. Chạm vật thể để tìm chữ; kéo chữ đến vị trí hoặc chạm chữ rồi chạm vị trí.' : 'Wordy Wings. Phím 1 đến 6 chọn mục tiêu từ trái sang phải.'); g.canvas.setAttribute('tabindex', '0'); } } });
    const resize = new ResizeObserver(() => {
      if (!builderConfig || !host.current || !game.current) return;
      const next = builderSize(builderConfig, builderWidth());
      if (next.width !== game.current.scale.width || next.height !== game.current.scale.height) {
        setCanvasRatio(`${next.width} / ${next.height}`); game.current.scale.resize(next.width, next.height);
      }
    });
    if (isBuilder) resize.observe(host.current.closest('.play-page') ?? host.current);
    const visibility = () => { if (document.hidden) { game.current?.scene.pause('LevelScene'); sound.stopAll(); setPaused(true); } };
    document.addEventListener('visibilitychange', visibility);
    const portrait = matchMedia('(orientation: portrait) and (max-width: 700px)');
    const orientation = () => { if (isBalloon && portrait.matches) { game.current?.scene.pause('LevelScene'); sound.stopAll(); setPaused(true); } };
    portrait.addEventListener('change', orientation);
    return () => { active = false; resize.disconnect(); portrait.removeEventListener('change', orientation); clearTimeout(startupTimeout); document.removeEventListener('visibilitychange', visibility); sound.stopAll(); game.current?.destroy(true); game.current = undefined; };
  }, [level, childId, restart]);
  useEffect(() => { if (audio.current) { audio.current.muted = muted; if (muted) audio.current.stopAll(); } }, [muted]);
  function pause(value: boolean) { setPaused(value); if (value) { game.current?.scene.pause('LevelScene'); audio.current?.stopAll(); } else game.current?.scene.resume('LevelScene'); }
  return <div className={`play-page${isBuilder ? ' word-builder-game' : ''}${isBalloon ? ' balloon-dart-game' : ''}`}>
    {isBalloon && <div className="balloon-rotate"><span aria-hidden="true">↻</span><h2>Xoay ngang để chơi nhé!</h2><p>Vườn bóng cần thêm chỗ để bé ngắm và bắn.</p><button className="secondary" onClick={onExit}>Về bản đồ</button></div>}
    <div className="play-toolbar"><button className="icon-button" aria-label="Tạm dừng" onClick={() => pause(true)}><Pause/></button><span className="eyebrow">{level.id} · {level.isBoss ? 'WORD STAR CHALLENGE' : 'LET’S PLAY'}</span><button className="icon-button" aria-label={muted ? 'Bật âm thanh' : 'Tắt âm thanh'} onClick={onMute}>{muted ? <VolumeX/> : <Volume2/>}</button></div>
    <div className="instruction"><button className="speaker" aria-label="Nghe lại hướng dẫn" disabled={paused} onClick={() => { if (muted) { onMute(); if (audio.current) audio.current.muted = false; } if (isBuilder) { const config = resolveWordBuilder(level); void audio.current?.speakSequence([config.instruction.text, config.targetWord]); } else void audio.current?.speak(currentInstruction.current.replace(/★.*?· /, '')); }}><Volume2/></button><h1>{instruction}</h1></div>
    <div className="game-frame" style={isBuilder ? { aspectRatio: canvasRatio, width: Number(canvasRatio.split(' / ')[0]) >= 600 ? `min(100%, calc((100dvh - 100px) * ${Number(canvasRatio.split(' / ')[0]) / Number(canvasRatio.split(' / ')[1])}))` : '100%' } : undefined}><div className="canvas-host" ref={host}/>{!ready && !error && !paused && <div className="game-overlay" role="status"><section className="modal"><h2>Pip đang chuẩn bị hình…</h2></section></div>}{paused && <div className="game-overlay"><section className="modal"><span className="eyebrow">TAKE A LITTLE BREAK</span><h2>Pip đợi bé ở đây!</h2><button className="primary" onClick={() => pause(false)}><Play/> Chơi tiếp</button><button className="secondary" onClick={() => setRestart(r => r + 1)}><RotateCcw/> Chơi lại</button><button className="text-button" onClick={onExit}><Map/> Về bản đồ</button></section></div>}{error && <div className="game-overlay"><section className="modal"><h2>{error}</h2><button className="primary" onClick={() => setRestart(r => r + 1)}>Mở lại màn chơi</button><button className="text-button" onClick={onExit}>Về bản đồ</button></section></div>}</div>
    <p className="game-feedback" aria-live="polite">{feedback}</p>{audioUnavailable && <p className="subtle">Thiết bị chưa phát được giọng đọc. Bé vẫn có thể chơi bằng hình và chữ.</p>}
  </div>;
}




