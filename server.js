"use strict";

require("dotenv").config();

const express = require("express");
const session = require("express-session");
const morgan = require("morgan");
const path = require("path");

// Inicializa o banco (schema, migrações e seed)
require("./src/database");

const { requireAuth } = require("./src/middleware/auth");
const errorHandler = require("./src/middleware/errorHandler");

const app = express();

// ── Middlewares globais ──────────────────────────────────────
app.use(morgan(process.env.NODE_ENV === "production" ? "combined" : "dev"));
app.use(express.json());
app.use(
  session({
    secret: process.env.SESSION_SECRET || "farmacontrol-secret",
    resave: false,
    saveUninitialized: false,
    cookie: { maxAge: Number(process.env.SESSION_MAX_AGE) || 8 * 3600 * 1000 },
  }),
);

// ── Arquivos estáticos ───────────────────────────────────────
app.use(express.static(path.join(__dirname, "public")));

// ── Rotas públicas ───────────────────────────────────────────
app.use("/api/auth", require("./src/routes/auth"));

// ── Rotas protegidas ─────────────────────────────────────────
app.use("/api/medicamentos", requireAuth, require("./src/routes/medicamentos"));
app.use(
  "/api/movimentacoes",
  requireAuth,
  require("./src/routes/movimentacoes"),
);
app.use(
  "/api/transferencias",
  requireAuth,
  require("./src/routes/transferencias"),
);
app.use("/api/doadores", requireAuth, require("./src/routes/doadores"));
app.use("/api/fabricantes", requireAuth, require("./src/routes/fabricantes"));
app.use("/api/usuarios", requireAuth, require("./src/routes/usuarios"));
app.use("/api/locais", requireAuth, require("./src/routes/locais"));
app.use("/api/auditoria", requireAuth, require("./src/routes/auditoria"));

// ── Módulo de Atendimentos ─────────────────────────────────
app.use("/api/pacientes", requireAuth, require("./src/routes/pacientes"));
app.use("/api/atendimentos", requireAuth, require("./src/routes/atendimentos"));
app.use("/api/triagem", requireAuth, require("./src/routes/triagem"));
app.use("/api/prontuarios", requireAuth, require("./src/routes/prontuarios"));
app.use("/api/prescricoes", requireAuth, require("./src/routes/prescricoes"));
app.use("/api/cid10", requireAuth, require("./src/routes/cid10"));

// ── Tratamento de erros ──────────────────────────────────────
app.use(errorHandler);

// ── Iniciar servidor ─────────────────────────────────────────
const PORT = Number(process.env.PORT) || 3001;
app.listen(PORT, () => {
  console.log(`\n  FarmaControl → http://localhost:${PORT}`);
  console.log(`  Ambiente   : ${process.env.NODE_ENV || "development"}\n`);
});
