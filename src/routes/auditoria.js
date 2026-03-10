'use strict';

const { Router } = require('express');
const { db } = require('../database');
const { requireRole, ROLES } = require('../middleware/auth');

const router = Router();

// Somente admin e gerente consultam auditoria
router.get('/', requireRole(...ROLES.GERENCIA), (req, res) => {
  const { acao, entidade, usuario, dataIni, dataFim } = req.query;

  let sql = 'SELECT * FROM auditoria WHERE 1=1';
  const p = [];

  if (acao)    { sql += ' AND acao = ?';                   p.push(acao); }
  if (entidade){ sql += ' AND entidade = ?';               p.push(entidade); }
  if (usuario) { sql += ' AND usuarioNome LIKE ?';         p.push('%' + usuario + '%'); }
  if (dataIni) { sql += " AND date(criadoEm) >= ?";        p.push(dataIni); }
  if (dataFim) { sql += " AND date(criadoEm) <= ?";        p.push(dataFim); }

  sql += ' ORDER BY id DESC LIMIT 500';

  res.json(db.prepare(sql).all(...p));
});

module.exports = router;
