export type UserRole =
  | 'admin'
  | 'gerente'
  | 'atendimento'
  | 'atendente'
  | 'medico'
  | 'enfermagem'
  | 'enfermeiro'
  | 'enfermeira'
  | 'farmaceutico'
  | 'movimentacao'
  | 'entrada'
  | 'saida'
  | 'visualizacao';

export interface AuthenticatedUser {
  id: number;
  name: string;
  email: string;
  role: UserRole;
  signaturePasswordResetRequired: boolean;
  modules: string[];
  accessToken: string | null;
  accessTokenExpiresAt: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface ChangeSignaturePasswordRequest {
  currentPassword: string | null;
  currentSignaturePassword: string | null;
  newSignaturePassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface NavItem {
  label: string;
  route: string;
  roles: UserRole[];
  modules?: string[];
  icon:
    | 'activity'
    | 'alert'
    | 'audit'
    | 'dashboard'
    | 'dispense'
    | 'entry'
    | 'history'
    | 'inventory'
    | 'profile'
    | 'report'
    | 'transfer'
    | 'users';
  showOnDashboard?: boolean;
}
