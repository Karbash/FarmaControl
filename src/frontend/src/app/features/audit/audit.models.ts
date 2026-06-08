export interface AuditLog {
  id: number;
  userId: number | null;
  userName: string;
  action: string;
  entity: string;
  entityId: number | null;
  description: string;
  createdAt: string;
}

export interface AuditFilter {
  action: string | null;
  entity: string | null;
  user: string | null;
  startDate: string | null;
  endDate: string | null;
}
