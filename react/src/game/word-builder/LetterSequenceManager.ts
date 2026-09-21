export interface SequenceToken { id: string; char: string }
export type Placement = { ok: true; index: number; complete: boolean } | { ok: false; reason: 'unknown' | 'used' | 'occupied' | 'mismatch' | 'range' };

/** Pure position-based logic. Identical characters remain distinct inventory items. */
export class LetterSequenceManager {
  readonly slots: Array<string | null>;
  private readonly tokens: Map<string, SequenceToken>;
  private readonly used = new Set<string>();
  constructor(readonly units: readonly string[], tokens: readonly SequenceToken[], prefilled: readonly number[] = []) {
    if (!units.length) throw new Error('A sequence needs at least one unit.');
    this.tokens = new Map(tokens.map(t => [t.id, t]));
    if (this.tokens.size !== tokens.length) throw new Error('Each token needs a unique ID.');
    if (prefilled.some(i => !Number.isInteger(i) || i < 0 || i >= units.length)) throw new Error('Invalid prefilled position.');
    this.slots = units.map((u, i) => prefilled.includes(i) ? u : null);
    const available = tokens.map(t => t.char);
    for (let i = 0; i < units.length; i++) if (!prefilled.includes(i)) {
      const match = available.indexOf(units[i]);
      if (match < 0) throw new Error(`Missing token for position ${i}.`);
      available.splice(match, 1);
    }
  }
  get complete() { return this.slots.every((value, i) => value === this.units[i]); }
  get nextIndex() { return this.slots.findIndex(value => value === null); }
  isUsed(id: string) { return this.used.has(id); }
  matchingIndex(id: string) {
    const t = this.tokens.get(id);
    return !t || this.used.has(id) ? -1 : this.units.findIndex((unit, i) => unit === t.char && this.slots[i] === null);
  }
  place(id: string, index: number): Placement {
    const token = this.tokens.get(id);
    if (!token) return { ok: false, reason: 'unknown' };
    if (this.used.has(id)) return { ok: false, reason: 'used' };
    if (!Number.isInteger(index) || index < 0 || index >= this.units.length) return { ok: false, reason: 'range' };
    if (this.slots[index] !== null) return { ok: false, reason: 'occupied' };
    if (token.char !== this.units[index]) return { ok: false, reason: 'mismatch' };
    this.used.add(id); this.slots[index] = token.char;
    return { ok: true, index, complete: this.complete };
  }
}
