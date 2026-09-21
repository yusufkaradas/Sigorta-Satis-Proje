const TR_MAP: Record<string, string> = {
  'ç': 'c', 'Ç': 'C', 'ğ': 'g', 'Ğ': 'G', 'ı': 'i', 'İ': 'I',
  'ö': 'o', 'Ö': 'O', 'ş': 's', 'Ş': 'S', 'ü': 'u', 'Ü': 'U'
};

function clean(value: string | null | undefined): string {
  return (value ?? '')
    .replace(/[çÇğĞıİöÖşŞüÜ]/g, char => TR_MAP[char] ?? char)
    .replace(/[^A-Za-z0-9-]+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '')
    .toUpperCase();
}

export function pdfFileName(title: string, ...parts: (string | null | undefined)[]): string {
  const segments = [title, ...parts].map(clean).filter(segment => segment);
  return `${segments.join('_')}.pdf`;
}
