'use strict';

const { Router } = require('express');
const { db } = require('../database');
const { requireRole, ROLES } = require('../middleware/auth');

const router = Router();

const requireAtendimento = requireRole(...ROLES.AT_ENFERMEIRA);

router.get('/', requireAtendimento, (req, res) => {
  const { atendimentoId } = req.query;
  if (!atendimentoId) return res.status(400).json({ error: 'atendimentoId é obrigatório' });
  res.json(db.prepare('SELECT * FROM triagem WHERE atendimentoId=? ORDER BY id DESC').all(atendimentoId));
});

router.post('/', requireRole(...ROLES.AT_EQUIPE), (req, res) => {
  const { atendimentoId, pressaoArterial, temperatura, peso, altura, frequenciaCardiaca, saturacao, queixaPrincipal, responsavel, obs } = req.body;
  if (!atendimentoId) return res.status(400).json({ error: 'atendimentoId é obrigatório' });

  // Atualiza status do atendimento para "triagem"
  db.prepare("UPDATE atendimentos SET status='triagem' WHERE id=? AND status='aguardando'").run(atendimentoId);

  const r = db.prepare(
    'INSERT INTO triagem (atendimentoId,pressaoArterial,temperatura,peso,altura,frequenciaCardiaca,saturacao,queixaPrincipal,responsavel,obs) VALUES (?,?,?,?,?,?,?,?,?,?)',
  ).run(atendimentoId, pressaoArterial || null, temperatura || null, peso || null, altura || null, frequenciaCardiaca || null, saturacao || null, queixaPrincipal || null, responsavel || null, obs || null);

  res.status(201).json({ id: r.lastInsertRowid });
});

router.put('/:id', requireRole(...ROLES.AT_EQUIPE), (req, res) => {
  const { pressaoArterial, temperatura, peso, altura, frequenciaCardiaca, saturacao, queixaPrincipal, responsavel, obs } = req.body;
  db.prepare(
    'UPDATE triagem SET pressaoArterial=?,temperatura=?,peso=?,altura=?,frequenciaCardiaca=?,saturacao=?,queixaPrincipal=?,responsavel=?,obs=? WHERE id=?',
  ).run(pressaoArterial || null, temperatura || null, peso || null, altura || null, frequenciaCardiaca || null, saturacao || null, queixaPrincipal || null, responsavel || null, obs || null, req.params.id);
  res.json({ ok: true });
});

module.exports = router;
