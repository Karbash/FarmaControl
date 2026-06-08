import { NavItem } from '../auth/auth.models';

export const NAV_ITEMS: NavItem[] = [
  {
    label: 'Inicio',
    route: '/dashboard',
    icon: 'dashboard',
    roles: ['admin', 'gerente', 'atendente', 'medico', 'enfermeira', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao']
  },
  {
    label: 'Perfil',
    route: '/perfil',
    icon: 'profile',
    roles: ['admin', 'gerente', 'atendente', 'medico', 'enfermeira', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao']
  },
  {
    label: 'Estoque',
    route: '/estoque',
    icon: 'inventory',
    roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao'],
    modules: ['estoque']
  },
  {
    label: 'Atendimentos',
    route: '/atendimentos',
    icon: 'activity',
    roles: ['admin', 'gerente', 'atendente', 'medico', 'enfermeira', 'farmaceutico']
  },
  {
    label: 'Usuarios',
    route: '/usuarios',
    icon: 'users',
    roles: ['admin'],
    modules: ['usuarios']
  },
  {
    label: 'Auditoria',
    route: '/auditoria',
    icon: 'audit',
    roles: ['admin', 'gerente'],
    modules: ['auditoria']
  }
];
