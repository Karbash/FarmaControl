'use strict';

const Database = require('better-sqlite3');
const crypto   = require('crypto');
const path     = require('path');

const DB_PATH = process.env.DB_PATH
  ? path.resolve(process.env.DB_PATH)
  : path.join(__dirname, '..', 'farmacontrol.db');

const db = new Database(DB_PATH);

// Performance e integridade
db.pragma('journal_mode = WAL');
db.pragma('foreign_keys = ON');

// ============================================================
// SCHEMA
// ============================================================
db.exec(`
  CREATE TABLE IF NOT EXISTS medicamentos (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    nomeGenerico      TEXT,
    nomeComercial     TEXT,
    classeTerapeutica TEXT,
    formaFarmaceutica TEXT,
    dosagem           TEXT,
    dataEntrada       TEXT,
    origem            TEXT,
    responsavel       TEXT,
    fabricante        TEXT,
    lote              TEXT,
    validade          TEXT,
    qtd               INTEGER DEFAULT 0,
    unidade           TEXT,
    localizacao       TEXT,
    minimo            INTEGER DEFAULT 5,
    controlado        INTEGER DEFAULT 0,
    criadoEm         TEXT DEFAULT (datetime('now','localtime'))
  );

  CREATE TABLE IF NOT EXISTS movimentacoes (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    tipo        TEXT,
    medId       INTEGER,
    qtd         INTEGER,
    data        TEXT,
    responsavel TEXT,
    obs         TEXT,
    lote        TEXT,
    motivo      TEXT,
    criadoEm   TEXT DEFAULT (datetime('now','localtime'))
  );

  CREATE TABLE IF NOT EXISTS doadores (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    nome      TEXT NOT NULL,
    telefone  TEXT,
    obs       TEXT,
    criadoEm TEXT DEFAULT (datetime('now','localtime'))
  );

  CREATE TABLE IF NOT EXISTS fabricantes (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    nome      TEXT NOT NULL,
    cnpj      TEXT,
    criadoEm TEXT DEFAULT (datetime('now','localtime'))
  );

  CREATE TABLE IF NOT EXISTS usuarios (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    nome      TEXT NOT NULL,
    email     TEXT UNIQUE NOT NULL,
    senha     TEXT NOT NULL,
    tipo      TEXT DEFAULT 'usuario',
    criadoEm TEXT DEFAULT (datetime('now','localtime'))
  );

  CREATE TABLE IF NOT EXISTS locais_estoque (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    nome      TEXT NOT NULL UNIQUE,
    criadoEm TEXT DEFAULT (datetime('now','localtime'))
  );

  CREATE TABLE IF NOT EXISTS auditoria (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    usuarioId   INTEGER,
    usuarioNome TEXT,
    acao        TEXT,
    entidade    TEXT,
    entidadeId  INTEGER,
    descricao   TEXT,
    criadoEm   TEXT DEFAULT (datetime('now','localtime'))
  );

  -- ── Módulo de Atendimentos ──────────────────────────────────

  CREATE TABLE IF NOT EXISTS pacientes (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    nome           TEXT NOT NULL,
    cpf            TEXT,
    dataNascimento TEXT,
    sexo           TEXT,
    telefone       TEXT,
    endereco       TEXT,
    obs            TEXT,
    ativo          INTEGER DEFAULT 1,
    criadoEm      TEXT DEFAULT (datetime('now','localtime'))
  );

  CREATE TABLE IF NOT EXISTS atendimentos (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    pacienteId  INTEGER NOT NULL,
    data        TEXT,
    hora        TEXT,
    tipo        TEXT DEFAULT 'consulta',
    emergencia  INTEGER DEFAULT 0,
    status      TEXT DEFAULT 'aguardando',
    medicoNome  TEXT,
    responsavel TEXT,
    obs         TEXT,
    criadoEm   TEXT DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (pacienteId) REFERENCES pacientes(id)
  );

  CREATE TABLE IF NOT EXISTS triagem (
    id                 INTEGER PRIMARY KEY AUTOINCREMENT,
    atendimentoId      INTEGER NOT NULL,
    pressaoArterial    TEXT,
    temperatura        TEXT,
    peso               TEXT,
    altura             TEXT,
    frequenciaCardiaca TEXT,
    saturacao          TEXT,
    queixaPrincipal    TEXT,
    responsavel        TEXT,
    obs                TEXT,
    criadoEm          TEXT DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (atendimentoId) REFERENCES atendimentos(id)
  );

  CREATE TABLE IF NOT EXISTS prontuarios (
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    atendimentoId       INTEGER NOT NULL,
    pacienteId          INTEGER NOT NULL,
    medicoNome          TEXT,
    anamnese            TEXT,
    exameFisico         TEXT,
    hipoteseDiagnostica TEXT,
    cid10               TEXT,
    conduta             TEXT,
    obs                 TEXT,
    criadoEm           TEXT DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (atendimentoId) REFERENCES atendimentos(id),
    FOREIGN KEY (pacienteId)    REFERENCES pacientes(id)
  );

  CREATE TABLE IF NOT EXISTS prescricoes (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    prontuarioId  INTEGER NOT NULL,
    pacienteId    INTEGER NOT NULL,
    medicamentoId INTEGER,
    nomeMed       TEXT,
    dosagem       TEXT,
    posologia     TEXT,
    quantidade    INTEGER DEFAULT 1,
    dispensado    INTEGER DEFAULT 0,
    obs           TEXT,
    criadoEm     TEXT DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (prontuarioId) REFERENCES prontuarios(id)
  );

  CREATE TABLE IF NOT EXISTS modulo_acesso (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    usuarioId INTEGER NOT NULL,
    modulo    TEXT NOT NULL,
    criadoEm TEXT DEFAULT (datetime('now','localtime')),
    UNIQUE(usuarioId, modulo)
  );
`);

// ============================================================
// MIGRAÇÕES (colunas adicionadas em versões anteriores)
// ============================================================
const migrations = [
  "ALTER TABLE movimentacoes ADD COLUMN motivo TEXT",
  "ALTER TABLE medicamentos  ADD COLUMN criadoEm TEXT DEFAULT (datetime('now','localtime'))",
  "ALTER TABLE movimentacoes ADD COLUMN criadoEm TEXT DEFAULT (datetime('now','localtime'))",
  "ALTER TABLE doadores      ADD COLUMN criadoEm TEXT DEFAULT (datetime('now','localtime'))",
  "ALTER TABLE fabricantes   ADD COLUMN criadoEm TEXT DEFAULT (datetime('now','localtime'))",
  "ALTER TABLE usuarios      ADD COLUMN criadoEm TEXT DEFAULT (datetime('now','localtime'))",
  "ALTER TABLE locais_estoque ADD COLUMN criadoEm TEXT DEFAULT (datetime('now','localtime'))",
  "ALTER TABLE atendimentos ADD COLUMN emergencia INTEGER DEFAULT 0",
  "ALTER TABLE prontuarios ADD COLUMN cid10 TEXT",
];

for (const sql of migrations) {
  try { db.exec(sql); } catch (_) { /* coluna já existe */ }
}

// ============================================================
// SEED — usuário master na primeira execução
// ============================================================
function hashSenha(senha) {
  return crypto.createHash('sha256').update(senha).digest('hex');
}

const totalUsuarios = db.prepare('SELECT COUNT(*) as c FROM usuarios').get();
if (totalUsuarios.c === 0) {
  db.prepare(
    'INSERT INTO usuarios (nome, email, senha, tipo) VALUES (?, ?, ?, ?)',
  ).run('Administrador', 'admin', hashSenha('admin123'), 'admin');
  console.log('  ✦ Usuário master criado → email: admin | senha: admin123');
}

/**
 * Registra uma ação na tabela de auditoria.
 * @param {import('express').Request} req
 * @param {'criar'|'editar'|'excluir'} acao
 * @param {string} entidade  ex: 'medicamento', 'usuario', 'doador'
 * @param {number|string} entidadeId
 * @param {string} descricao  texto legível descrevendo a mudança
 */
function logAudit(req, acao, entidade, entidadeId, descricao) {
  try {
    const u = req.session?.usuario;
    db.prepare(
      'INSERT INTO auditoria (usuarioId, usuarioNome, acao, entidade, entidadeId, descricao) VALUES (?,?,?,?,?,?)',
    ).run(u?.id ?? null, u?.nome ?? 'sistema', acao, entidade, entidadeId, descricao);
  } catch (_) { /* nunca deve bloquear a operação principal */ }
}

module.exports = { db, hashSenha, logAudit };
