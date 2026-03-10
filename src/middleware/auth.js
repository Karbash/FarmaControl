'use strict';

function requireAuth(req, res, next) {
  if (!req.session.usuario) {
    return res.status(401).json({ error: 'Não autenticado' });
  }
  next();
}

/**
 * Cria um middleware que exige um dos roles listados.
 * Uso:  router.post('/', requireRole('admin','gerente'), handler)
 */
function requireRole(...roles) {
  return (req, res, next) => {
    const tipo = req.session.usuario?.tipo;
    if (!tipo || !roles.includes(tipo)) {
      return res.status(403).json({ error: 'Sem permissão para esta ação' });
    }
    next();
  };
}

// Conjuntos reutilizáveis para importar nos route files
const ROLES = {
  ADMIN:        ['admin'],
  GERENCIA:     ['admin', 'gerente'],
  PODE_ENTRADA: ['admin', 'gerente', 'movimentacao', 'entrada', 'farmaceutico'],
  PODE_SAIDA:   ['admin', 'gerente', 'movimentacao', 'saida', 'farmaceutico'],
  QUALQUER:     ['admin', 'gerente', 'movimentacao', 'entrada', 'saida', 'visualizacao', 'farmaceutico'],
  AT_EQUIPE:    ['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico'],
  AT_MEDICO:    ['admin', 'medico'],
  AT_ENFERMEIRA: ['admin', 'medico', 'enfermeira', 'farmaceutico'],
};

module.exports = { requireAuth, requireRole, ROLES };
