export enum PolicyStatus {
  Draft = 1,
  Active = 2,
  Expired = 3,
  Cancelled = 4
}

export interface Policy {
  id: string;
  customerId: string;
  vehicleId: string;
  quoteId: string;
  policyNumber: string;
  premiumAmount: number;
  startDate: string;
  endDate: string;
  status: PolicyStatus;
  isActive: boolean;
  rowVersion?: string;
}

export interface PolicyCreateDto {
  customerId: string;
  vehicleId: string;
  quoteId: string;
  startDate: string;
  endDate: string;
}

export interface PolicyUpdateDto {
  endDate: string;
  rowVersion: string;
}