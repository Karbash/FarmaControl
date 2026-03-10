'use strict';

const { Router } = require('express');
const { db, logAudit } = require('../database');
const { requireRole, ROLES } = require('../middleware/auth');

const router = Router();

const requireMedicamentos = requireRole(...ROLES.PODE_ENTRADA);

router.get('/', requireMedicamentos, (req, res) => {
  res.json(db.prepare('SELECT * FROM locais_estoque ORDER BY nome').all());
});

router.post('/', requireRole(...ROLES.GERENCIA), (req, res) => {
  const { nome } = req.body;
  if (!nome) return res.status(400).json({ error: 'Nome é obrigatório' });
  try {
    const r = db.prepare('INSERT INTO locais_estoque (nome) VALUES (?)').run(nome.trim());
    logAudit(req, 'criar', 'local', r.lastInsertRowid, `Cadastrou local "${nome.trim()}"`);
    res.status(201).json({ id: r.lastInsertRowid });
  } catch {
    res.status(400).json({ error: 'Local já cadastrado' });
  }
});

router.delete('/:id', requireRole(...ROLES.GERENCIA), (req, res) => {
  const row = db.prepare('SELECT nome FROM locais_estoque WHERE id=?').get(req.params.id);
  db.prepare('DELETE FROM locais_estoque WHERE id=?').run(req.params.id);
  logAudit(req, 'excluir', 'local', req.params.id, `Excluiu local "${row?.nome || '?'}"`);
  res.json({ ok: true });
});

module.exports = router;
