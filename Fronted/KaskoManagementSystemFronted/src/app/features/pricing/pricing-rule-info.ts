export interface PricingRuleInfo {
  group: string;
  title: string;
  explanation: string;
  isRate: boolean;
}

const GROUPS: { prefix: string; group: string; build: (code: string) => Omit<PricingRuleInfo, 'group' | 'isRate'> }[] = [
  {
    prefix: 'BASE_KASKO_RATE',
    group: 'Temel Oran',
    build: () => ({
      title: 'Temel kasko oranı',
      explanation: 'Aracın kasko değerinin yüzde kaçının temel prim olarak alınacağını belirler. Tüm tekliflerin başlangıç fiyatını doğrudan etkiler.'
    })
  },
  {
    prefix: 'AGE_',
    group: 'Araç Yaşı',
    build: code => ({
      title: `${range(code)} yaşındaki araçlar`,
      explanation: `Model yılına göre ${range(code)} yaşında olan araçların primi bu katsayı ile çarpılır. Araç eskidikçe risk arttığı için katsayı yükselir.`
    })
  },
  {
    prefix: 'USAGE_',
    group: 'Kullanım Tipi',
    build: code => ({
      title: `${usage(code)} kullanım`,
      explanation: `${usage(code)} kullanılan araçların primi bu katsayı ile çarpılır. Günlük kullanım yoğunluğu arttıkça katsayı yükselir.`
    })
  },
  {
    prefix: 'DRIVER_',
    group: 'Sürücü Yaşı',
    build: code => ({
      title: `${range(code)} yaş sürücüler`,
      explanation: `Sigortalı sürücünün yaşı ${range(code)} aralığındaysa prim bu katsayı ile çarpılır. Genç ve deneyimsiz sürücülerde risk daha yüksektir.`
    })
  },
  {
    prefix: 'CLAIMS_',
    group: 'Hasar Geçmişi',
    build: code => ({
      title: claims(code),
      explanation: `Son dönemde ${claims(code).toLowerCase()} olan müşterilerin primi bu katsayı ile çarpılır. 1'in altındaki değer indirim, üstündeki değer ek prim demektir.`
    })
  },
  {
    prefix: 'REGION_',
    group: 'Bölge Riski',
    build: code => ({
      title: `${region(code)} riskli bölgeler`,
      explanation: `Müşterinin yaşadığı şehir ${region(code).toLowerCase()} riskli gruptaysa prim bu katsayı ile çarpılır. Hırsızlık ve kaza yoğunluğu dikkate alınır.`
    })
  }
];

function range(code: string): string {
  const parts = code.split('_').slice(1);
  if (parts.includes('PLUS')) {
    return `${parts[0]} ve üzeri`;
  }
  return parts.join('-');
}

function usage(code: string): string {
  return ({ USAGE_PRIVATE: 'Özel', USAGE_COMMERCIAL: 'Ticari', USAGE_RENTAL: 'Kiralık' } as Record<string, string>)[code] ?? 'Bu tipte';
}

function claims(code: string): string {
  if (code === 'CLAIMS_0') {
    return 'Hiç hasarı olmayanlar';
  }
  return code.endsWith('PLUS') ? '3 ve üzeri hasarı olanlar' : `${code.split('_')[1]} hasarı olanlar`;
}

function region(code: string): string {
  return ({ REGION_LOW: 'Düşük', REGION_NORMAL: 'Normal', REGION_HIGH: 'Yüksek' } as Record<string, string>)[code] ?? 'Bu';
}

export function describePricingRule(code?: string, name?: string, description?: string | null): PricingRuleInfo {
  const value = code ?? '';
  const match = GROUPS.find(item => value.startsWith(item.prefix));
  if (!match) {
    return {
      group: 'Diğer',
      title: name || value || 'Fiyat kuralı',
      explanation: description || 'Prim hesaplamasında kullanılan katsayıdır.',
      isRate: value.includes('RATE')
    };
  }
  return { group: match.group, isRate: value.includes('RATE'), ...match.build(value) };
}

export function describePricingImpact(code: string | undefined, oldValue: number, newValue: number): string {
  if (!oldValue) {
    return '';
  }
  const change = ((newValue - oldValue) / oldValue) * 100;
  if (Math.abs(change) < 0.05) {
    return 'Primde değişiklik olmaz';
  }
  const percent = Math.abs(change).toLocaleString('tr-TR', { maximumFractionDigits: 1 });
  const direction = change > 0 ? 'artar' : 'azalır';
  if ((code ?? '').includes('RATE')) {
    const sample = 1_000_000;
    const money = (value: number) => (sample * value).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
    return `Temel prim %${percent} ${direction} · 1.000.000 ₺ araçta ${money(oldValue)} ₺ → ${money(newValue)} ₺`;
  }
  return `Bu gruptaki müşterilerin primi %${percent} ${direction}`;
}
