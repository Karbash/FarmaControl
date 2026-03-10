'use strict';

const { Router } = require('express');
const { db, hashSenha } = require('../database');

const router = Router();

router.post('/login', (req, res) => {
  const { email, senha } = req.body;
  if (!email || !senha) {
    return res.status(400).json({ error: 'Preencha todos os campos' });
  }

  const usuario = db
    .prepare('SELECT * FROM usuarios WHERE email = ?')
    .get(email.trim());

  if (!usuario || usuario.senha !== hashSenha(senha)) {
    return res.status(401).json({ error: 'Email ou senha incorretos' });
  }

  req.session.usuario = {
    id:    usuario.id,
    nome:  usuario.nome,
    email: usuario.email,
    tipo:  usuario.tipo,
  };
  res.json({ ok: true, usuario: req.session.usuario });
});

router.post('/logout', (req, res) => {
  req.session.destroy();
  res.json({ ok: true });
});

router.get('/me', (req, res) => {
  if (!req.session.usuario) {
    return res.status(401).json({ error: 'Não autenticado' });
  }
  const u = req.session.usuario;
  const mods = db.prepare('SELECT modulo FROM modulo_acesso WHERE usuarioId = ?').all(u.id);
  u.modulos = mods.map(m => m.modulo);
  res.json(u);
});

router.put('/me', (req, res) => {
  if (!req.session.usuario) {
    return res.status(401).json({ error: 'Não autenticado' });
  }
  const { nome, modulos } = req.body;
  const u = req.session.usuario;
  
  if (nome) {
    db.prepare('UPDATE usuarios SET nome = ? WHERE id = ?').run(nome, u.id);
    u.nome = nome;
  }
  
  if (modulos && Array.isArray(modulos)) {
    db.prepare('DELETE FROM modulo_acesso WHERE usuarioId = ?').run(u.id);
    const ins = db.prepare('INSERT INTO modulo_acesso (usuarioId, modulo) VALUES (?, ?)');
    modulos.forEach(m => ins.run(u.id, m));
  }
  
  res.json({ ok: true, usuario: u });
});

module.exports = router;
