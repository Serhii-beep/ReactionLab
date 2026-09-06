import { WorkspaceItem } from "../../state/workspace-store";

export type BenchDelta =
    | { readonly kind: 'added'; readonly name: string; readonly count: number }
    | { readonly kind: 'removed'; readonly name: string; readonly count: number }
    | { readonly kind: 'cleared' }
    | { readonly kind: 'changed'; readonly size: number };

export function benchDelta(before: readonly WorkspaceItem[], after: readonly WorkspaceItem[]): BenchDelta | null {
    if (after.length === 0) {
        return before.length === 0 ? null : { kind: 'cleared' };
    }

    const counts = new Map(after.map((item) => [item.substance.id, item.count]));
    const names = new Map([...before, ...after].map((item) => [item.substance.id, item.substance.name]));
    const previous = new Map(before.map((item) => [item.substance.id, item.count]));
    const changed = [...new Set([...previous.keys(), ...counts.keys()])]
        .filter((id) => (previous.get(id) ?? 0) !== (counts.get(id) ?? 0));
    
    if (changed.length === 0) {
        return null;
    }

    if (changed.length > 1) {
        return { kind: 'changed', size: after.length };
    }

    const [id] = changed;
    const count = counts.get(id) ?? 0;
    const name = names.get(id) ?? '';

    return count > (previous.get(id) ?? 0) ? { kind: 'added', name, count } : { kind: 'removed', name, count };
}