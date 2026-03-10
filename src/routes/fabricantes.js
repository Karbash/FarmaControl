'use strict';

const { Router } = require('express');
const { db, logAudit } = require('../database');
const { requireRole, ROLES } = require('../middleware/auth');

const router = Router();

const requireMedicamentos = requireRole(...ROLES.PODE_ENTRADA);

router.get('/', requireMedicamentos, (req, res) => {
  res.json(db.prepare('SELECT * FROM fabricantes ORDER BY nome').all());
});

router.post('/', requireRole(...ROLES.GERENCIA), (req, res) => {
  const { nome, cnpj } = req.body;
  if (!nome) return res.status(400).json({ error: 'Nome é obrigatório' });
  const r = db.prepare(
    'INSERT INTO fabricantes (nome,cnpj) VALUES (@nome,@cnpj)',
  ).run({ nome: nome.trim(), cnpj: cnpj || null });
  logAudit(req, 'criar', 'fabricante', r.lastInsertRowid, `Cadastrou fabricante "${nome.trim()}"`);
  res.status(201).json({ id: r.lastInsertRowid });
});

router.delete('/:id', requireRole(...ROLES.GERENCIA), (req, res) => {
  const row = db.prepare('SELECT nome FROM fabricantes WHERE id=?').get(req.params.id);
  db.prepare('DELETE FROM fabricantes WHERE id=?').run(req.params.id);
  logAudit(req, 'excluir', 'fabricante', req.params.id, `Excluiu fabricante "${row?.nome || '?'}"`);
  res.json({ ok: true });
});

module.exports = router;
