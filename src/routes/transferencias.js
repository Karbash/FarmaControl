'use strict';

const { Router } = require('express');
const { db } = require('../database');
const { requireRole, ROLES } = require('../middleware/auth');

const router = Router();

router.post('/', requireRole(...ROLES.PODE_SAIDA), (req, res) => {
  const { medId, novaLocalizacao, qtd, responsavel, data, obs } = req.body;

  if (!medId || !novaLocalizacao || !qtd || !responsavel || !data) {
    return res.status(400).json({ error: 'Preencha todos os campos obrigatórios' });
  }

  const med = db.prepare('SELECT * FROM medicamentos WHERE id=?').get(medId);
  if (!med) return res.status(404).json({ error: 'Medicamento não encontrado' });
  if (qtd > med.qtd) return res.status(400).json({ error: 'Quantidade insuficiente em estoque' });

  const origemLocal = med.localizacao || '—';
  const obsLog = obs
    ? `${origemLocal} → ${novaLocalizacao} | ${obs}`
    : `${origemLocal} → ${novaLocalizacao}`;

  // Operação atômica via transação SQLite
  const transferir = db.transaction(() => {
    let novoId = null;

    if (qtd === med.qtd) {
      // Transferência total: apenas muda localização
      db.prepare('UPDATE medicamentos SET localizacao=? WHERE id=?').run(novaLocalizacao, medId);
    } else {
      // Transferência parcial: reduz original e cria novo registro no destino
      db.prepare('UPDATE medicamentos SET qtd=? WHERE id=?').run(med.qtd - qtd, medId);
      const r = db.prepare(`
        INSERT INTO medicamentos
          (nomeGenerico,nomeComercial,classeTerapeutica,formaFarmaceutica,dosagem,
           dataEntrada,origem,responsavel,fabricante,lote,validade,qtd,unidade,
           localizacao,minimo,controlado)
        VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)
      `).run(
        med.nomeGenerico, med.nomeComercial, med.classeTerapeutica, med.formaFarmaceutica,
        med.dosagem, med.dataEntrada, med.origem, med.responsavel, med.fabricante,
        med.lote, med.validade, qtd, med.unidade, novaLocalizacao, med.minimo, med.controlado,
      );
      novoId = r.lastInsertRowid;
    }

    db.prepare(`
      INSERT INTO movimentacoes (tipo,medId,qtd,data,responsavel,obs,lote,motivo)
      VALUES (?,?,?,?,?,?,?,?)
    `).run('transferencia', medId, qtd, data, responsavel, obsLog, med.lote, 'Transferência de local');

    return novoId;
  });

  const novoId = transferir();
  res.json({ ok: true, novoId });
});

module.exports = router;
