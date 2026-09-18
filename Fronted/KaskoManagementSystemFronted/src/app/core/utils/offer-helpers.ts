export interface CoverageRow {
  coverageId: string;
  coverageName: string;
  description?: string | null;
}

export function collectCoverageRows(packages: { coverages: { coverageId: string; coverageName: string; description?: string | null }[] }[]): CoverageRow[] {
  const seen = new Map<string, CoverageRow>();
  [...packages]
    .sort((a, b) => b.coverages.length - a.coverages.length)
    .forEach(item => item.coverages.forEach(coverage => {
      const current = seen.get(coverage.coverageId);
      if (!current) {
        seen.set(coverage.coverageId, { coverageId: coverage.coverageId, coverageName: coverage.coverageName, description: coverage.description });
      } else if (!current.description && coverage.description) {
        current.description = coverage.description;
      }
    }));
  return [...seen.values()];
}

export function upgradeNote(
  fromName: string | null | undefined,
  fromPrice: number | null | undefined,
  fromCoverageIds: string[],
  toPrice: number | null | undefined,
  toCoverages: { coverageId: string; coverageName: string }[]
): string | null {
  if (fromPrice == null || toPrice == null || toPrice <= fromPrice) {
    return null;
  }
  const added = toCoverages.filter(item => !fromCoverageIds.includes(item.coverageId)).map(item => item.coverageName);
  if (!added.length) {
    return null;
  }
  const difference = Math.round(toPrice - fromPrice).toLocaleString('tr-TR');
  return `${fromName ?? 'Seçili paket'} yerine +${difference} ₺ ile ${added.join(', ')} eklenir.`;
}

export function deductibleExample(percent: number): string {
  if (!percent) {
    return 'Hasarın tamamı ödenir, sizden kesinti yapılmaz.';
  }
  const own = (10000 * percent / 100).toLocaleString('tr-TR');
  return `Örnek: 10.000 ₺ hasarda ${own} ₺ sizden, kalanı sigortadan.`;
}

const LIMIT_HINTS: Record<string, string> = {
  'yol yardım': 'Çekici mesafesi ve yol hizmetleri',
  'imm': 'Karşı tarafa verilen zarar limiti',
  'cam kırılması': 'Değişimde kullanılacak cam türü',
  'ikame araç': 'Serviste size verilecek araç süresi'
};

export function limitHint(name: string): string {
  return LIMIT_HINTS[name.toLocaleLowerCase('tr-TR')] ?? 'Seçiminiz tüm paketlere uygulanır';
}
