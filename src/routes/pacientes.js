'use strict';

const { Router } = require('express');
const { db, logAudit } = require('../database');
const { requireRole, ROLES } = require('../middleware/auth');

const router = Router();

const requireAtendimento = requireRole(...ROLES.AT_ENFERMEIRA);

router.get('/', requireAtendimento, (req, res) => {
  const { busca, ativo } = req.query;
  let sql = 'SELECT * FROM pacientes WHERE 1=1';
  const p = [];
  if (ativo !== undefined) { sql += ' AND ativo=?'; p.push(Number(ativo)); }
  if (busca) { sql += ' AND (nome LIKE ? OR cpf LIKE ? OR telefone LIKE ?)'; p.push(`%${busca}%`, `%${busca}%`, `%${busca}%`); }
  sql += ' ORDER BY nome';
  res.json(db.prepare(sql).all(...p));
});

router.get('/:id', (req, res) => {
  const pac = db.prepare('SELECT * FROM pacientes WHERE id=?').get(req.params.id);
  if (!pac) return res.status(404).json({ error: 'Paciente não encontrado' });
  res.json(pac);
});

router.post('/', requireRole(...ROLES.AT_EQUIPE), (req, res) => {
  const { nome, cpf, dataNascimento, sexo, telefone, endereco, obs } = req.body;
  if (!nome) return res.status(400).json({ error: 'Nome é obrigatório' });
  const r = db.prepare(
    'INSERT INTO pacientes (nome,cpf,dataNascimento,sexo,telefone,endereco,obs) VALUES (@nome,@cpf,@dataNascimento,@sexo,@telefone,@endereco,@obs)',
  ).run({ nome: nome.trim(), cpf: cpf || null, dataNascimento: dataNascimento || null, sexo: sexo || null, telefone: telefone || null, endereco: endereco || null, obs: obs || null });
  logAudit(req, 'criar', 'paciente', r.lastInsertRowid, `Cadastrou paciente "${nome.trim()}"`);
  res.status(201).json({ id: r.lastInsertRowid });
});

router.put('/:id', requireRole(...ROLES.AT_EQUIPE), (req, res) => {
  const { nome, cpf, dataNascimento, sexo, telefone, endereco, obs, ativo } = req.body;
  if (!nome) return res.status(400).json({ error: 'Nome é obrigatório' });
  db.prepare(
    'UPDATE pacientes SET nome=?,cpf=?,dataNascimento=?,sexo=?,telefone=?,endereco=?,obs=?,ativo=? WHERE id=?',
  ).run(nome.trim(), cpf || null, dataNascimento || null, sexo || null, telefone || null, endereco || null, obs || null, ativo !== undefined ? Number(ativo) : 1, req.params.id);
  logAudit(req, 'editar', 'paciente', req.params.id, `Editou paciente "${nome.trim()}"`);
  res.json({ ok: true });
});

router.delete('/:id', requireRole(...ROLES.GERENCIA), (req, res) => {
  const row = db.prepare('SELECT nome FROM pacientes WHERE id=?').get(req.params.id);
  db.prepare('DELETE FROM pacientes WHERE id=?').run(req.params.id);
  logAudit(req, 'excluir', 'paciente', req.params.id, `Excluiu paciente "${row?.nome || '?'}"`);
  res.json({ ok: true });
});

module.exports = router;
