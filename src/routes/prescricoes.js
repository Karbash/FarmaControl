'use strict';

const { Router } = require('express');
const { db } = require('../database');
const { requireRole, ROLES } = require('../middleware/auth');

const router = Router();

const requireAtendimento = requireRole(...ROLES.AT_ENFERMEIRA);

router.get('/', requireAtendimento, (req, res) => {
  const { prontuarioId, pacienteId, dispensado } = req.query;
  let sql = `
    SELECT pr.*, p.nome as pacienteNome,
      at.data as atendimentoData, at.id as atendimentoId
    FROM prescricoes pr
    JOIN pacientes p ON p.id = pr.pacienteId
    JOIN prontuarios pron ON pron.id = pr.prontuarioId
    JOIN atendimentos at ON at.id = pron.atendimentoId
    WHERE 1=1`;
  const params = [];
  if (prontuarioId !== undefined) { sql += ' AND pr.prontuarioId=?'; params.push(prontuarioId); }
  if (pacienteId   !== undefined) { sql += ' AND pr.pacienteId=?';   params.push(pacienteId); }
  if (dispensado   !== undefined) { sql += ' AND pr.dispensado=?';   params.push(Number(dispensado)); }
  sql += ' ORDER BY pr.id DESC';
  res.json(db.prepare(sql).all(...params));
});

router.post('/', requireRole(...ROLES.AT_EQUIPE), (req, res) => {
  const { prontuarioId, pacienteId, medicamentoId, nomeMed, dosagem, posologia, quantidade, obs } = req.body;
  if (!prontuarioId || !pacienteId) return res.status(400).json({ error: 'prontuarioId e pacienteId são obrigatórios' });
  const r = db.prepare(
    'INSERT INTO prescricoes (prontuarioId,pacienteId,medicamentoId,nomeMed,dosagem,posologia,quantidade,obs) VALUES (?,?,?,?,?,?,?,?)',
  ).run(prontuarioId, pacienteId, medicamentoId || null, nomeMed || null, dosagem || null, posologia || null, quantidade || 1, obs || null);
  res.status(201).json({ id: r.lastInsertRowid });
});

router.delete('/:id', requireRole(...ROLES.AT_EQUIPE), (req, res) => {
  db.prepare('DELETE FROM prescricoes WHERE id=?').run(req.params.id);
  res.json({ ok: true });
});

// Dispensar — gera saída no estoque de medicamentos
router.post('/:id/dispensar', requireRole(...ROLES.PODE_SAIDA), (req, res) => {
  const { responsavel } = req.body;
  const pres = db.prepare('SELECT * FROM prescricoes WHERE id=?').get(req.params.id);
  if (!pres) return res.status(404).json({ error: 'Prescrição não encontrada' });
  if (pres.dispensado) return res.status(400).json({ error: 'Já dispensado' });
  if (!pres.medicamentoId) return res.status(400).json({ error: 'Medicamento não vinculado ao estoque' });

  const med = db.prepare('SELECT * FROM medicamentos WHERE id=?').get(pres.medicamentoId);
  if (!med) return res.status(404).json({ error: 'Medicamento não encontrado no estoque' });
  if (med.qtd < pres.quantidade) return res.status(400).json({ error: `Estoque insuficiente: ${med.qtd} disponíveis` });

  const pac = db.prepare('SELECT nome FROM pacientes WHERE id=?').get(pres.pacienteId);

  // Deduz do estoque
  db.prepare('UPDATE medicamentos SET qtd=qtd-? WHERE id=?').run(pres.quantidade, pres.medicamentoId);

  // Registra movimentação
  db.prepare('INSERT INTO movimentacoes (tipo,medId,qtd,data,responsavel,obs,lote,motivo) VALUES (?,?,?,?,?,?,?,?)')
    .run('saida', pres.medicamentoId, pres.quantidade, new Date().toISOString().split('T')[0],
      responsavel || 'Sistema', `Paciente: ${pac?.nome || '?'} (prescrição #${pres.id})`, med.lote, 'Dispensação');

  // Marca prescrição como dispensada
  db.prepare('UPDATE prescricoes SET dispensado=1 WHERE id=?').run(req.params.id);

  res.json({ ok: true });
});

module.exports = router;
