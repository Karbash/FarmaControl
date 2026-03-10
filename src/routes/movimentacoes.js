'use strict';

const { Router } = require('express');
const { db } = require('../database');
const { requireRole, ROLES } = require('../middleware/auth');

const router = Router();

router.get('/', (req, res) => {
  res.json(db.prepare('SELECT * FROM movimentacoes ORDER BY id DESC').all());
});

router.post('/', requireRole(...ROLES.PODE_SAIDA), (req, res) => {
  const m = req.body;
  const r = db.prepare(`
    INSERT INTO movimentacoes (tipo,medId,qtd,data,responsavel,obs,lote,motivo)
    VALUES (@tipo,@medId,@qtd,@data,@responsavel,@obs,@lote,@motivo)
  `).run({ motivo: null, ...m });
  res.status(201).json({ id: r.lastInsertRowid });
});

module.exports = router;
