'use strict';

const { Router } = require('express');
const { requireAuth } = require('../middleware/auth');
const path = require('path');
const fs = require('fs');

const router = Router();

let cid10Data = null;

function loadCID10() {
  if (cid10Data) return;
  try {
    const filePath = path.join(__dirname, '..', '..', 'public', 'dados', 'cid10.json');
    const data = fs.readFileSync(filePath, 'utf8');
    cid10Data = JSON.parse(data);
  } catch (e) {
    console.error('Erro ao carregar CID-10:', e.message);
    cid10Data = { codigos: [] };
  }
}

router.get('/', requireAuth, (req, res) => {
  loadCID10();
  const q = (req.query.q || '').toLowerCase().trim();
  if (!q || q.length < 1) return res.json(cid10Data.codigos.slice(0, 50));

  const results = cid10Data.codigos.filter(item =>
    item.codigo.toLowerCase().includes(q) ||
    item.nome.toLowerCase().includes(q)
  ).slice(0, 30);

  res.json(results);
});

module.exports = router;
