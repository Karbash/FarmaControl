'use strict';

const { Router } = require('express');
const { db, logAudit } = require('../database');
const { requireRole, ROLES } = require('../middleware/auth');

const router = Router();

const requireMedicamentos = requireRole(...ROLES.PODE_ENTRADA);

router.get('/', requireMedicamentos, (req, res) => {
  res.json(db.prepare('SELECT * FROM doadores ORDER BY nome').all());
});

router.post('/', requireRole(...ROLES.GERENCIA), (req, res) => {
  const { nome, telefone, obs } = req.body;
  if (!nome) return res.status(400).json({ error: 'Nome é obrigatório' });
  const r = db.prepare(
    'INSERT INTO doadores (nome,telefone,obs) VALUES (@nome,@telefone,@obs)',
  ).run({ nome: nome.trim(), telefone: telefone || null, obs: obs || null });
  logAudit(req, 'criar', 'doador', r.lastInsertRowid, `Cadastrou doador "${nome.trim()}"`);
  res.status(201).json({ id: r.lastInsertRowid });
});

router.delete('/:id', requireRole(...ROLES.GERENCIA), (req, res) => {
  const row = db.prepare('SELECT nome FROM doadores WHERE id=?').get(req.params.id);
  db.prepare('DELETE FROM doadores WHERE id=?').run(req.params.id);
  logAudit(req, 'excluir', 'doador', req.params.id, `Excluiu doador "${row?.nome || '?'}"`);
  res.json({ ok: true });
});

module.exports = router;
