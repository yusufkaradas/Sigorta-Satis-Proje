export function newestFirst<T extends { createdDate?: string | null }>(items: T[] | null | undefined): T[] {
  return [...(items ?? [])].sort(
    (a, b) => new Date(b.createdDate ?? 0).getTime() - new Date(a.createdDate ?? 0).getTime()
  );
}
