import { artUrl, friends, type Friend } from '../game/artManifest';

/** Shared atlas keeps character identity identical across React and Phaser. */
export function Pip({ className = '', friend = 'pip' }: { className?: string; friend?: string }) {
  const f = friends[friend as Friend] ?? friends.pip;
  return <svg className={className} viewBox={`${f.x} ${f.y} ${f.w} ${f.h}`} overflow="hidden" aria-hidden="true"><image href={artUrl('friends-v1.png')} width="1254" height="1254"/></svg>;
}
export function Island({ type = 0, className = '' }: { type?: number; className?: string }) {
  const index = Math.max(0, Math.min(2, type));
  return <svg className={className} viewBox={`${index * 724} 0 724 724`} aria-hidden="true"><image href={artUrl('islands-v1.png')} width="2172" height="724"/></svg>;
}
