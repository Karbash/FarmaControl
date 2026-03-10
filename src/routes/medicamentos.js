'use strict';

const { Router } = require('express');
const { db, logAudit } = require('../database');
const { requireRole, ROLES } = require('../middleware/auth');

const router = Router();

const MED_DEFAULTS = {
  nomeGenerico:      null,
  nomeComercial:     null,
  classeTerapeutica: null,
  formaFarmaceutica: null,
  dosagem:           null,
  dataEntrada:       null,
  origem:            null,
  responsavel:       null,
  fabricante:        null,
  lote:              null,
  validade:          null,
  qtd:               0,
  unidade:           null,
  localizacao:       null,
  minimo:            5,
  controlado:        0,
};

router.get('/', (req, res) => {
  const rows = db.prepare('SELECT * FROM medicamentos').all();
  rows.forEach((r) => { r.controlado = r.controlado === 1; });
  res.json(rows);
});

router.post('/', requireRole(...ROLES.PODE_ENTRADA), (req, res) => {
  const m = req.body;
  const r = db.prepare(`
    INSERT INTO medicamentos
      (nomeGenerico,nomeComercial,classeTerapeutica,formaFarmaceutica,dosagem,
       dataEntrada,origem,responsavel,fabricante,lote,validade,qtd,unidade,
       localizacao,minimo,controlado)
    VALUES
      (@nomeGenerico,@nomeComercial,@classeTerapeutica,@formaFarmaceutica,@dosagem,
       @dataEntrada,@origem,@responsavel,@fabricante,@lote,@validade,@qtd,@unidade,
       @localizacao,@minimo,@controlado)
  `).run({ ...MED_DEFAULTS, ...m, controlado: m.controlado ? 1 : 0 });
  logAudit(req, 'criar', 'medicamento', r.lastInsertRowid,
    `Cadastrou "${m.nomeGenerico || ''}" (${m.nomeComercial || ''}) — lote: ${m.lote || '—'}, qtd: ${m.qtd ?? 0}`);
  res.status(201).json({ id: r.lastInsertRowid });
});

router.put('/:id', requireRole(...ROLES.PODE_ENTRADA), (req, res) => {
  const m = req.body;
  db.prepare(`
    UPDATE medicamentos SET
      nomeGenerico=@nomeGenerico, nomeComercial=@nomeComercial,
      classeTerapeutica=@classeTerapeutica, formaFarmaceutica=@formaFarmaceutica,
      dosagem=@dosagem, dataEntrada=@dataEntrada, origem=@origem,
      responsavel=@responsavel, fabricante=@fabricante, lote=@lote,
      validade=@validade, qtd=@qtd, unidade=@unidade, localizacao=@localizacao,
      minimo=@minimo, controlado=@controlado
    WHERE id=@id
  `).run({ ...MED_DEFAULTS, ...m, controlado: m.controlado ? 1 : 0, id: req.params.id });
  logAudit(req, 'editar', 'medicamento', req.params.id,
    `Editou "${m.nomeGenerico || ''}" (${m.nomeComercial || ''}) — lote: ${m.lote || '—'}, qtd: ${m.qtd ?? 0}`);
  res.json({ ok: true });
});

router.delete('/:id', requireRole(...ROLES.GERENCIA), (req, res) => {
  const med = db.prepare('SELECT nomeGenerico, nomeComercial FROM medicamentos WHERE id=?').get(req.params.id);
  db.prepare('DELETE FROM medicamentos WHERE id=?').run(req.params.id);
  logAudit(req, 'excluir', 'medicamento', req.params.id,
    `Excluiu "${med?.nomeGenerico || '?'}" (${med?.nomeComercial || '?'})`);
  res.json({ ok: true });
});

module.exports = router;
