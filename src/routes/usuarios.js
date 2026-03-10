'use strict';

const { Router } = require('express');
const { db, hashSenha, logAudit } = require('../database');
const { requireRole, ROLES } = require('../middleware/auth');

const router = Router();

// Equipe de atendimento — visível para toda a equipe de saúde
router.get('/equipe', requireRole(...ROLES.AT_EQUIPE), (req, res) => {
  res.json(db.prepare(
    "SELECT id, nome, tipo FROM usuarios WHERE tipo IN ('medico','farmaceutico','enfermeira') ORDER BY tipo, nome"
  ).all());
});

// Somente admin gerencia usuários
router.get('/', requireRole(...ROLES.ADMIN), (req, res) => {
  res.json(db.prepare('SELECT id, nome, email, tipo FROM usuarios ORDER BY nome').all());
});

router.post('/', requireRole(...ROLES.ADMIN), (req, res) => {
  const { nome, email, senha, tipo } = req.body;
  if (!nome || !email || !senha) {
    return res.status(400).json({ error: 'Nome, email e senha são obrigatórios' });
  }
  try {
    const r = db.prepare(
      'INSERT INTO usuarios (nome,email,senha,tipo) VALUES (@nome,@email,@senha,@tipo)',
    ).run({ nome, email, senha: hashSenha(senha), tipo: tipo || 'usuario' });
    logAudit(req, 'criar', 'usuario', r.lastInsertRowid, `Criou usuário "${nome}" (${email}) — tipo: ${tipo || 'usuario'}`);
    res.status(201).json({ id: r.lastInsertRowid });
  } catch {
    res.status(400).json({ error: 'Email já cadastrado' });
  }
});

// Retorna dados + módulos de um usuário específico (para admin editar)
router.get('/:id', requireRole(...ROLES.ADMIN), (req, res) => {
  const u = db.prepare('SELECT id, nome, email, tipo FROM usuarios WHERE id=?').get(req.params.id);
  if (!u) return res.status(404).json({ error: 'Usuário não encontrado' });
  const mods = db.prepare('SELECT modulo FROM modulo_acesso WHERE usuarioId=?').all(req.params.id);
  u.modulos = mods.map(m => m.modulo);
  res.json(u);
});

router.delete('/:id', requireRole(...ROLES.ADMIN), (req, res) => {
  const row = db.prepare('SELECT nome, email FROM usuarios WHERE id=?').get(req.params.id);
  db.prepare('DELETE FROM usuarios WHERE id=?').run(req.params.id);
  logAudit(req, 'excluir', 'usuario', req.params.id, `Excluiu usuário "${row?.nome || '?'}" (${row?.email || '?'})`);
  res.json({ ok: true });
});

router.put('/:id', requireRole(...ROLES.ADMIN), (req, res) => {
  const { nome, email, senha, tipo } = req.body;
  const id = req.params.id;
  if (!nome || !email) {
    return res.status(400).json({ error: 'Nome e email são obrigatórios' });
  }
  try {
    if (senha) {
      db.prepare(
        'UPDATE usuarios SET nome=?, email=?, senha=?, tipo=? WHERE id=?',
      ).run(nome, email, hashSenha(senha), tipo || 'usuario', id);
    } else {
      db.prepare(
        'UPDATE usuarios SET nome=?, email=?, tipo=? WHERE id=?',
      ).run(nome, email, tipo || 'usuario', id);
    }
    logAudit(req, 'editar', 'usuario', id, `Editou usuário "${nome}" (${email}) — tipo: ${tipo || 'usuario'}${senha ? ', senha alterada' : ''}`);
    res.json({ ok: true });
  } catch {
    res.status(400).json({ error: 'Erro ao atualizar usuário' });
  }
});

router.put('/:id/modulos', requireRole(...ROLES.ADMIN), (req, res) => {
  const { modulos } = req.body;
  const id = req.params.id;
  
  if (!Array.isArray(modulos)) {
    return res.status(400).json({ error: 'modulos deve ser um array' });
  }
  
  try {
    db.prepare('DELETE FROM modulo_acesso WHERE usuarioId = ?').run(id);
    const ins = db.prepare('INSERT INTO modulo_acesso (usuarioId, modulo) VALUES (?, ?)');
    modulos.forEach(m => ins.run(id, m));
    logAudit(req, 'editar', 'usuario', id, `Atualizou módulos de acesso: ${modulos.join(', ')}`);
    res.json({ ok: true });
  } catch {
    res.status(400).json({ error: 'Erro ao atualizar módulos' });
  }
});

module.exports = router;
