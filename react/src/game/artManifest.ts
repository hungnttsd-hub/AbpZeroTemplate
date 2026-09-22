export const artUrl = (file: string) => `${import.meta.env.BASE_URL}art/${file}`;
export const friends = {
  pip: { x: 165, y: 48, w: 370, h: 554 },
  poki: { x: 742, y: 48, w: 382, h: 554 },
  lulu: { x: 175, y: 601, w: 349, h: 623 },
  momo: { x: 700, y: 642, w: 463, h: 582 }
} as const;
export type Friend = keyof typeof friends;
