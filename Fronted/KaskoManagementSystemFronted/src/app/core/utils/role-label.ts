const ROLE_LABELS: Record<string, string> = {
  Admin: 'Sistem Yöneticisi',
  Manager: 'Operasyon Yöneticisi',
  Customer: 'Müşteri'
};

export function roleLabel(roleName?: string | null): string {
  if (!roleName) {
    return 'Rol atanmamış';
  }
  return ROLE_LABELS[roleName] ?? roleName;
}
