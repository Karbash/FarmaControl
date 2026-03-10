'use strict';

const { Router } = require('express');
const { db, logAudit } = require('../database');
const { requireRole, ROLES } = require('../middleware/auth');

const router = Router();

// Middleware para verificar acesso ao módulo de atendimentos
const requireAtendimento = requireRole(...ROLES.AT_ENFERMEIRA);

router.get('/', requireAtendimento, (req, res) => {
  const { data, status, pacienteId } = req.query;
  let sql = `
    SELECT a.*, p.nome as pacienteNome, p.dataNascimento, p.sexo, p.cpf, p.telefone
    FROM atendimentos a
    JOIN pacientes p ON p.id = a.pacienteId
    WHERE 1=1`;
  const params = [];
  if (data)       { sql += ' AND a.data = ?';        params.push(data); }
  if (status)     { sql += ' AND a.status = ?';      params.push(status); }
  if (pacienteId) { sql += ' AND a.pacienteId = ?';  params.push(pacienteId); }
  sql += ' ORDER BY a.data DESC, a.hora DESC';
  res.json(db.prepare(sql).all(...params));
});

router.get('/:id', requireAtendimento, (req, res) => {
  const row = db.prepare(`
    SELECT a.*, p.nome as pacienteNome, p.dataNascimento, p.sexo, p.cpf, p.telefone
    FROM atendimentos a JOIN pacientes p ON p.id=a.pacienteId
    WHERE a.id=?`).get(req.params.id);
  if (!row) return res.status(404).json({ error: 'Atendimento não encontrado' });
  res.json(row);
});

router.post('/', requireRole(...ROLES.AT_EQUIPE), (req, res) => {
  const { pacienteId, data, hora, tipo, emergencia, responsavel, obs } = req.body;
  if (!pacienteId) return res.status(400).json({ error: 'pacienteId é obrigatório' });
  const r = db.prepare(
    'INSERT INTO atendimentos (pacienteId,data,hora,tipo,emergencia,status,responsavel,obs) VALUES (?,?,?,?,?,?,?,?)',
  ).run(pacienteId, data || new Date().toISOString().split('T')[0], hora || null, tipo || 'consulta', emergencia ? 1 : 0, 'aguardando', responsavel || null, obs || null);
  logAudit(req, 'criar', 'atendimento', r.lastInsertRowid, `Abriu atendimento para paciente #${pacienteId}${emergencia ? ' (EMERGÊNCIA)' : ''}`);
  res.status(201).json({ id: r.lastInsertRowid });
});

router.put('/:id/status', requireRole(...ROLES.AT_EQUIPE), (req, res) => {
  const { status, medicoNome } = req.body;
  if (!status) return res.status(400).json({ error: 'status é obrigatório' });
  const upd = { status, id: req.params.id };
  if (medicoNome !== undefined) {
    db.prepare('UPDATE atendimentos SET status=?, medicoNome=? WHERE id=?').run(status, medicoNome, req.params.id);
  } else {
    db.prepare('UPDATE atendimentos SET status=? WHERE id=?').run(status, req.params.id);
  }
  logAudit(req, 'editar', 'atendimento', req.params.id, `Status → ${status}`);
  res.json({ ok: true });
});

router.put('/:id', requireRole(...ROLES.AT_EQUIPE), (req, res) => {
  const { tipo, obs, medicoNome } = req.body;
  db.prepare('UPDATE atendimentos SET tipo=?, obs=?, medicoNome=? WHERE id=?').run(tipo || 'consulta', obs || null, medicoNome || null, req.params.id);
  res.json({ ok: true });
});

router.delete('/:id', requireRole(...ROLES.GERENCIA), (req, res) => {
  db.prepare('DELETE FROM atendimentos WHERE id=?').run(req.params.id);
  res.json({ ok: true });
});

module.exports = router;
