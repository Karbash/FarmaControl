import { UserRole } from '../../core/auth/auth.models';

export interface UserModuleAccess {
  id: number;
  module: string;
  isRevoked: boolean;
  revokedAt: string | null;
  revokedByUserId: number | null;
  revocationReason: string | null;
  grantedByUserId: number;
  createdAt: string;
}

export interface User {
  id: number;
  name: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  isDeleted: boolean;
  canAuthenticate: boolean;
  canSign: boolean;
  signaturePasswordResetRequired: boolean;
  accessRevokedAt: string | null;
  accessRevokedByUserId: number | null;
  accessRevocationReason: string | null;
  deletedAt: string | null;
  deletedByUserId: number | null;
  modules: UserModuleAccess[];
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateUserRequest {
  name: string;
  email: string;
  password: string;
  role: UserRole;
}

export interface UpdateUserRequest {
  name: string;
  email: string;
  password: string | null;
  role: UserRole;
  isActive: boolean;
}

export interface ResponsibleUser {
  id: number;
  name: string;
  role: UserRole;
}
