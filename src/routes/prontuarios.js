'use strict';

const { Router } = require('express');
const { db, logAudit } = require('../database');
const { requireRole, ROLES } = require('../middleware/auth');

const router = Router();

const requireAtendimento = requireRole(...ROLES.AT_ENFERMEIRA);

router.get('/', requireAtendimento, (req, res) => {
  const { atendimentoId, pacienteId } = req.query;
  let sql = 'SELECT * FROM prontuarios WHERE 1=1';
  const p = [];
  if (atendimentoId) { sql += ' AND atendimentoId=?'; p.push(atendimentoId); }
  if (pacienteId)    { sql += ' AND pacienteId=?';    p.push(pacienteId); }
  sql += ' ORDER BY id DESC';
  res.json(db.prepare(sql).all(...p));
});

router.get('/:id', (req, res) => {
  const row = db.prepare('SELECT * FROM prontuarios WHERE id=?').get(req.params.id);
  if (!row) return res.status(404).json({ error: 'Prontuário não encontrado' });
  res.json(row);
});

router.post('/', requireRole(...ROLES.AT_EQUIPE), (req, res) => {
  const { atendimentoId, pacienteId, medicoNome, anamnese, exameFisico, hipoteseDiagnostica, cid10, conduta, obs } = req.body;
  if (!atendimentoId || !pacienteId) return res.status(400).json({ error: 'atendimentoId e pacienteId são obrigatórios' });

  // Atualiza status do atendimento para "em_atendimento"
  db.prepare("UPDATE atendimentos SET status='em_atendimento', medicoNome=? WHERE id=?").run(medicoNome || null, atendimentoId);

  const r = db.prepare(
    'INSERT INTO prontuarios (atendimentoId,pacienteId,medicoNome,anamnese,exameFisico,hipoteseDiagnostica,cid10,conduta,obs) VALUES (?,?,?,?,?,?,?,?,?)',
  ).run(atendimentoId, pacienteId, medicoNome || null, anamnese || null, exameFisico || null, hipoteseDiagnostica || null, cid10 || null, conduta || null, obs || null);

  logAudit(req, 'criar', 'prontuario', r.lastInsertRowid, `Prontuário do paciente #${pacienteId} — atend. #${atendimentoId}`);
  res.status(201).json({ id: r.lastInsertRowid });
});

router.put('/:id', requireRole(...ROLES.AT_EQUIPE), (req, res) => {
  const { medicoNome, anamnese, exameFisico, hipoteseDiagnostica, cid10, conduta, obs } = req.body;
  db.prepare(
    'UPDATE prontuarios SET medicoNome=?,anamnese=?,exameFisico=?,hipoteseDiagnostica=?,cid10=?,conduta=?,obs=? WHERE id=?',
  ).run(medicoNome || null, anamnese || null, exameFisico || null, hipoteseDiagnostica || null, cid10 || null, conduta || null, obs || null, req.params.id);
  res.json({ ok: true });
});

module.exports = router;
