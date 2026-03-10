// ============================================================
// API - Comunicação com o servidor Node.js
// ============================================================
function _checkStatus(r) {
  if (r.status === 401) {
    window.location.replace("/login.html");
    throw new Error("401");
  }
  if (!r.ok) throw new Error(r.status);
  return r.json();
}
function apiGet(path) {
  return fetch(path).then(_checkStatus);
}
function apiPost(path, data) {
  return fetch(path, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  }).then(_checkStatus);
}
function apiPut(path, data) {
  return fetch(path, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  }).then(_checkStatus);
}
function apiDelete(path) {
  return fetch(path, { method: "DELETE" }).then(_checkStatus);
}

// Medicamentos
function dbSalvarMedicamento(m) {
  return apiPost("/api/medicamentos", m);
}
function dbBuscarMedicamentos() {
  return apiGet("/api/medicamentos");
}
function dbExcluirMedicamento(id) {
  return apiDelete("/api/medicamentos/" + id);
}
function dbAtualizarMedicamento(m) {
  return apiPut("/api/medicamentos/" + m.id, m);
}

// Movimentações
function dbSalvarMovimentacao(m) {
  return apiPost("/api/movimentacoes", m);
}
function dbBuscarMovimentacoes() {
  return apiGet("/api/movimentacoes");
}

// Doadores
function dbSalvarDoador(d) {
  return apiPost("/api/doadores", d);
}
function dbBuscarDoadores() {
  return apiGet("/api/doadores");
}
function dbExcluirDoador(id) {
  return apiDelete("/api/doadores/" + id);
}

// Fabricantes
function dbSalvarFabricante(f) {
  return apiPost("/api/fabricantes", f);
}
function dbBuscarFabricantes() {
  return apiGet("/api/fabricantes");
}
function dbExcluirFabricante(id) {
  return apiDelete("/api/fabricantes/" + id);
}

// Usuários
function dbSalvarUsuario(u) {
  return apiPost("/api/usuarios", u);
}
function dbBuscarUsuarios() {
  return apiGet("/api/usuarios");
}
function dbExcluirUsuario(id) {
  return apiDelete("/api/usuarios/" + id);
}
function dbAtualizarUsuario(u) {
  return apiPut("/api/usuarios/" + u.id, u);
}

// Locais de Estoque
function dbSalvarLocal(l) {
  return apiPost("/api/locais", l);
}
function dbBuscarLocais() {
  return apiGet("/api/locais");
}
function dbExcluirLocal(id) {
  return apiDelete("/api/locais/" + id);
}

// Transferências
function dbTransferir(t) {
  return apiPost("/api/transferencias", t);
}

// Auditoria
function dbBuscarAuditoria(params) {
  const q = new URLSearchParams(
    Object.fromEntries(Object.entries(params).filter(([, v]) => v)),
  );
  return apiGet("/api/auditoria?" + q);
}

// ============================================================
// DADOS
// ============================================================
let medicamentos = [];
let movimentacoes = [];
let dbLoaded = false;

// Cadastros
let doadores = [];
let fabricantes = [];
let usuarios = [];
let locais = [];

// Filtros — Estoque
let searchText = "";
let filtroForma = "Todos";
let filtroLocal = "Todos";
let pageNum = 1;
const PER = 25;

// Filtros — Histórico
let histFiltroTipo    = "";
let histFiltroLote    = "";
let histFiltroResp    = "";
let histFiltroDataIni = "";
let histFiltroDataFim = "";

// ============================================================
// HELPERS
// ============================================================
const hoje = () => new Date().toISOString().split("T")[0];
function vStatus(v) {
  if (!v) return "ok";
  const d = (new Date(v + "T00:00:00") - new Date()) / 86400000;
  return d < 0 ? "vencido" : d <= 90 ? "breve" : "ok";
}
function vLabel(v) {
  if (!v) return "—";
  const s = vStatus(v),
    dt = new Date(v + "T12:00:00").toLocaleDateString("pt-BR");
  if (s === "vencido")
    return `<span class="vb"><span class="material-icons" style="font-size:1rem;vertical-align:-3px">warning</span> ${dt} (VENCIDO)</span>`;
  if (s === "breve")
    return `<span class="vs"><span class="material-icons" style="font-size:1rem;vertical-align:-3px">schedule</span> ${dt} (&lt;90d)</span>`;
  return `<span class="vo">${dt}</span>`;
}
function getStat(m) {
  return m.qtd === 0 ? "out" : m.qtd <= m.minimo ? "low" : "ok";
}
const sLabel = (s) =>
  s === "ok" ? "Normal" : s === "low" ? "Baixo" : "Esgotado";
const sBadge = (s) =>
  `<span class="bdg ${s === "ok" ? "b-ok" : s === "low" ? "b-low" : "b-out"}">${sLabel(s)}</span>`;
const getMed = (id) => medicamentos.find((m) => m.id === id);
const fmtD = (d) =>
  d ? new Date(d + "T12:00:00").toLocaleDateString("pt-BR") : "—";
function openModal(id) {
  document.getElementById(id).classList.add("open");
}
function closeModal(id) {
  document.getElementById(id).classList.remove("open");
}
function formas() {
  return [
    "Todos",
    ...new Set(medicamentos.map((m) => m.formaFarmaceutica).filter(Boolean)),
  ].sort();
}
function locaisUsados() {
  return [
    "Todos",
    ...new Set(medicamentos.map((m) => m.localizacao).filter(Boolean)),
  ].sort();
}
function toast(msg) {
  const t = document.createElement("div");
  t.innerHTML = `<div style="position:fixed;bottom:26px;right:26px;background:#1E8449;color:#fff;padding:11px 18px;border-radius:8px;font-size:.84rem;box-shadow:0 4px 16px rgba(0,0,0,.18);z-index:999;animation:slideUp .25s">${msg}</div>`;
  document.body.appendChild(t);
  setTimeout(() => t.remove(), 3200);
}

// ============================================================
// DASHBOARD
// ============================================================
function atualizarDashboard() {
  const total = medicamentos.length;
  const totalU = medicamentos.reduce((s, m) => s + m.qtd, 0);
  const alrt = medicamentos.filter((m) => getStat(m) !== "ok").length;
  const venc = medicamentos.filter(
    (m) => vStatus(m.validade) === "vencido",
  ).length;
  const br = medicamentos.filter((m) => vStatus(m.validade) === "breve").length;
  const ctrl = medicamentos.filter((m) => m.controlado).length;
  const rec = [...movimentacoes]
    .sort((a, b) => b.data.localeCompare(a.data))
    .slice(0, 6);

  document.getElementById("total-registros").textContent = total;
  document.getElementById("total-unidades").textContent =
    totalU.toLocaleString("pt-BR");
  document.getElementById("estoque-critico").textContent = alrt;
  document.getElementById("vencidos-proximos").textContent = venc + br;
  document.getElementById("vencidos-sub").textContent = venc + " vencido(s)";
  document.getElementById("controlados").textContent = ctrl;

  const divVencido = document.getElementById("alerta-vencido");
  const divProximo = document.getElementById("alerta-proximo");
  if (venc > 0) {
    divVencido.innerHTML = `<div class="alrt" style="display:flex;align-items:center;justify-content:space-between;gap:12px"><span style="display:flex;align-items:center;gap:6px"><span class="material-icons" style="font-size:1.1rem">block</span> <strong>${venc} medicamento(s) vencido(s)</strong> no estoque.</span><a href="alertas.html" class="btn-gh" style="white-space:nowrap">Ver alertas <span class="material-icons" style="font-size:1rem;vertical-align:-3px">arrow_forward</span></a></div>`;
  } else {
    divVencido.innerHTML = "";
  }
  if (br > 0 && venc === 0) {
    divProximo.innerHTML = `<div class="alrt yel" style="display:flex;align-items:center;justify-content:space-between;gap:12px"><span style="display:flex;align-items:center;gap:6px"><span class="material-icons" style="font-size:1.1rem">schedule</span> <strong>${br} medicamento(s)</strong> com validade próxima (&lt;90 dias).</span><a href="alertas.html" class="btn-gh" style="white-space:nowrap">Ver alertas <span class="material-icons" style="font-size:1rem;vertical-align:-3px">arrow_forward</span></a></div>`;
  } else {
    divProximo.innerHTML = "";
  }

  const divMov = document.getElementById("ultimas-movimentacoes");
  if (rec.length === 0) {
    divMov.innerHTML = `<div style="text-align:center;padding:30px;color:var(--g400)">Nenhuma movimentação ainda.</div>`;
  } else {
    divMov.innerHTML = rec
      .map((mov) => {
        const m = getMed(mov.medId);
        return `<div class="mv-item">
        <div class="mv-ico ${mov.tipo === "entrada" ? "mv-in" : "mv-out"}">
          <span class="material-icons" style="font-size:1rem">${mov.tipo === "entrada" ? "arrow_upward" : "arrow_downward"}</span>
        </div>
        <div class="mv-nfo">
          <div class="mv-nm">${m ? m.nomeGenerico : "—"} ${m ? `<span style="color:var(--g400);font-weight:400">(${m.nomeComercial})</span>` : ""}</div>
          <div class="mv-dt">${fmtD(mov.data)} · ${mov.responsavel} · ${mov.obs || "—"}</div>
        </div>
        <div class="mv-q" style="color:${mov.tipo === "entrada" ? "#1E8449" : "var(--red)"}">${mov.tipo === "entrada" ? "+" : "−"}${mov.qtd}</div>
      </div>`;
      })
      .join("");
  }

  const divSit = document.getElementById("situacao-estoque");
  divSit.innerHTML =
    medicamentos
      .slice(0, 12)
      .map((m) => {
        const s = getStat(m);
        const pct =
          m.minimo > 0
            ? Math.min(100, Math.round((m.qtd / (m.minimo * 2)) * 100))
            : m.qtd > 0
              ? 80
              : 0;
        const c = s === "ok" ? "#27AE60" : s === "low" ? "#E67E22" : "#C8102E";
        return `<div style="margin-bottom:9px">
      <div style="display:flex;justify-content:space-between;margin-bottom:2px">
        <span style="font-size:.79rem;font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;max-width:66%">${m.nomeGenerico}</span>
        ${sBadge(s)}
      </div>
      <div style="display:flex;align-items:center;gap:6px">
        <div class="pb" style="flex:1"><div class="pf" style="width:${pct}%;background:${c}"></div></div>
        <span style="font-size:.68rem;color:var(--g400);min-width:40px;text-align:right">${m.qtd}/${m.minimo}</span>
      </div>
    </div>`;
      })
      .join("") +
    (medicamentos.length > 12
      ? `<div style="text-align:center;padding-top:7px"><a href="estoque.html" class="btn btn-sm btn-s">Ver todos ${medicamentos.length} <span class="material-icons" style="font-size:1rem;vertical-align:-3px">arrow_forward</span></a></div>`
      : "");
}

// ============================================================
// ESTOQUE
// ============================================================
function atualizarEstoque() {
  let lista = medicamentos;
  if (searchText)
    lista = lista.filter(
      (m) =>
        m.nomeGenerico.toLowerCase().includes(searchText.toLowerCase()) ||
        (m.nomeComercial || "")
          .toLowerCase()
          .includes(searchText.toLowerCase()) ||
        (m.fabricante || "").toLowerCase().includes(searchText.toLowerCase()) ||
        (m.lote || "").toLowerCase().includes(searchText.toLowerCase()) ||
        (m.classeTerapeutica || "")
          .toLowerCase()
          .includes(searchText.toLowerCase()),
    );
  if (filtroForma !== "Todos")
    lista = lista.filter((m) => m.formaFarmaceutica === filtroForma);
  if (filtroLocal !== "Todos")
    lista = lista.filter((m) => m.localizacao === filtroLocal);

  document.getElementById("total-encontrados").textContent = lista.length;

  const fl = locaisUsados();
  const ff = formas();
  document.getElementById("filtros-local").innerHTML =
    `<span style="font-size:.7rem;color:var(--g400);font-weight:600">Localização:</span>` +
    fl
      .map(
        (l) =>
          `<button class="fc ${filtroLocal === l ? "active" : ""}" onclick="setFiltroLocal('${l}')">${l}</button>`,
      )
      .join("");
  document.getElementById("filtros-forma").innerHTML =
    `<span style="font-size:.7rem;color:var(--g400);font-weight:600">Forma:</span>` +
    ff
      .map(
        (f) =>
          `<button class="fc ${filtroForma === f ? "active" : ""}" onclick="setFiltroForma('${f}')">${f}</button>`,
      )
      .join("");

  const tp = Math.max(1, Math.ceil(lista.length / PER));
  if (pageNum > tp) pageNum = tp;
  const st = (pageNum - 1) * PER;
  const pg = lista.slice(st, st + PER);

  const tbody = document.getElementById("tabela-estoque");
  if (pg.length === 0) {
    tbody.innerHTML = `<tr><td colspan="11" style="text-align:center;padding:30px;color:var(--g400)"><span class="material-icons" style="vertical-align:-4px">medication</span> Nenhum medicamento encontrado.</td></tr>`;
  } else {
    tbody.innerHTML = pg
      .map((m) => {
        const s = getStat(m);
        const vs = vStatus(m.validade);
        return `<tr>
        <td data-label="Genérico">
          <div class="tdm">${m.nomeGenerico}</div>
          ${m.controlado ? `<span class="bdg b-ctrl" style="margin-top:2px">CONTROLADO</span>` : ""}
          ${vs === "vencido" ? `<span class="bdg b-venc" style="margin-top:2px">VENCIDO</span>` : ""}
          ${m.classeTerapeutica ? `<div class="tds">${m.classeTerapeutica}</div>` : ""}
        </td>
        <td data-label="Comercial">${m.nomeComercial || "—"}</td>
        <td data-label="Forma">${m.formaFarmaceutica || "—"}</td>
        <td data-label="Lote">${m.lote || "—"}</td>
        <td data-label="Validade">${vLabel(m.validade)}</td>
        <td data-label="Fabricante">${m.fabricante || "—"}</td>
        <td data-label="Local">${m.localizacao || `<span style="color:var(--g400)">—</span>`}</td>
        <td data-label="Origem">${m.origem || "—"}</td>
        <td data-label="Qtd">${m.qtd} ${m.unidade || ""}</td>
        <td data-label="Status">${sBadge(s)}</td>
        <td data-label="Ações"><div style="display:flex;gap:4px">
          ${podeAcessar("admin","gerente","movimentacao","saida") ? `<a href="saida.html?med=${m.id}" class="btn btn-sm btn-gs"><span class="material-icons" style="font-size:.9rem">arrow_downward</span> Baixa</a>` : ""}
          ${podeAcessar("admin","gerente") ? `<button class="btn btn-sm btn-ds" onclick="excluir(${m.id})"><span class="material-icons" style="font-size:.9rem">close</span></button>` : ""}
        </div></td>
      </tr>`;
      })
      .join("");
  }

  const pag = document.getElementById("paginacao");
  if (tp > 1) {
    let btns = `<span style="font-size:.73rem;color:var(--g400);margin-right:7px">Pág. ${pageNum} de ${tp} (${lista.length} itens)</span>`;
    btns += `<button class="pb2" onclick="chPg(${pageNum - 1})" ${pageNum <= 1 ? "disabled" : ""}>‹ Ant.</button>`;
    for (let i = 0; i < Math.min(tp, 5); i++) {
      const p =
        tp <= 5
          ? i + 1
          : pageNum <= 3
            ? i + 1
            : pageNum >= tp - 2
              ? tp - 4 + i
              : pageNum - 2 + i;
      btns += `<button class="pb2 ${p === pageNum ? "active" : ""}" onclick="chPg(${p})">${p}</button>`;
    }
    btns += `<button class="pb2" onclick="chPg(${pageNum + 1})" ${pageNum >= tp ? "disabled" : ""}>Prox. ›</button>`;
    pag.innerHTML = btns;
  } else {
    pag.innerHTML = "";
  }
}
function setFiltroLocal(l) {
  filtroLocal = l;
  pageNum = 1;
  atualizarEstoque();
}
function setFiltroForma(f) {
  filtroForma = f;
  pageNum = 1;
  atualizarEstoque();
}
function chPg(p) {
  pageNum = p;
  atualizarEstoque();
  window.scrollTo(0, 0);
}
function filtrarEstoque() {
  searchText = document.getElementById("busca").value;
  pageNum = 1;
  atualizarEstoque();
}

function excluir(id) {
  const m = getMed(id);
  if (
    !confirm(
      `Excluir ${m.nomeGenerico} (${m.nomeComercial})? Esta ação não pode ser desfeita.`,
    )
  )
    return;

  dbExcluirMedicamento(id)
    .then(() => {
      medicamentos = medicamentos.filter((x) => x.id !== id);
      atualizarEstoque();
      toast("Medicamento excluído!");
    })
    .catch((err) => {
      console.error("Erro ao excluir:", err);
      toast("Erro ao excluir!");
    });
}

// Modal saída
function abrirSM(id) {
  const opts = medicamentos
    .map(
      (m) =>
        `<option value="${m.id}" ${m.id === id ? "selected" : ""}>${m.nomeGenerico} — ${m.nomeComercial} (${m.qtd} ${m.unidade || "un."})</option>`,
    )
    .join("");
  document.getElementById("sm-med").innerHTML = opts;
  document.getElementById("sm-data").value = hoje();
  document.getElementById("sm-qtd").value = "";
  document.getElementById("sm-resp").value = window.usuarioAtual?.nome || "";
  document.getElementById("sm-obs").value = "";
  updateSI();
  openModal("modal-saida");
}
function updateSI() {
  const id = parseInt(document.getElementById("sm-med").value);
  const el = document.getElementById("sm-info");
  if (!id) {
    el.textContent = "";
    return;
  }
  const m = getMed(id);
  if (!m) return;
  el.innerHTML = `Lote: <strong>${m.lote || "—"}</strong> · Validade: ${vLabel(m.validade)} · Estoque: <strong>${m.qtd} ${m.unidade || "un."}</strong>`;
}
function togglePaciente(prefix) {
  const motivo = document.getElementById(prefix + "-motivo")?.value;
  const grupo = document.getElementById(prefix + "-paciente-group");
  if (grupo) {
    grupo.style.display = motivo === "Dispensação" ? "flex" : "none";
  }
}
function salvarSaidaModal() {
  const id = parseInt(document.getElementById("sm-med").value);
  const qtd = parseInt(document.getElementById("sm-qtd").value);
  const dt = document.getElementById("sm-data").value;
  const resp = document.getElementById("sm-resp").value.trim();
  const obs = document.getElementById("sm-obs").value.trim();
  const motivo = document.getElementById("sm-motivo")?.value || "Dispensação";
  const paciente = document.getElementById("sm-paciente")?.value.trim();
  let obsFinal = obs || motivo;
  if (motivo === "Dispensação" && paciente) {
    obsFinal = `Paciente: ${paciente}` + (obs ? ` - ${obs}` : "");
  }
  if (!qtd || qtd < 1) return alert("Quantidade inválida.");
  if (!resp) return alert("Informe o responsável.");
  if (!dt) return alert("Informe a data.");
  const m = getMed(id);
  if (m.qtd < qtd)
    return alert(
      `Estoque insuficiente. Disponível: ${m.qtd} ${m.unidade || "un."}.`,
    );

  m.qtd -= qtd;
  const mov = {
    tipo: "saida",
    medId: id,
    qtd,
    data: dt,
    responsavel: resp,
    obs: obsFinal,
    lote: m.lote,
    motivo,
  };

  dbAtualizarMedicamento(m)
    .then(() => {
      return dbSalvarMovimentacao(mov);
    })
    .then((r) => {
      mov.id = r.id;
      movimentacoes.push(mov);
      closeModal("modal-saida");
      toast("Baixa registrada com sucesso!");
      atualizarEstoque();
    })
    .catch((err) => {
      console.error("Erro ao salvar:", err);
      toast("Erro ao salvar dados!");
    });
}

// ============================================================
// ENTRADA
// ============================================================
function procBC() {
  const cod = document.getElementById("bc-inp").value.trim();
  const msg = document.getElementById("bc-msg");
  if (!cod) {
    msg.textContent = "Digite um código de barras.";
    return;
  }
  msg.innerHTML = `<span style="color:#1E8449"><span class="material-icons" style="font-size:.9rem;vertical-align:-2px">check_circle</span> Código <strong>${cod}</strong> lido. Inserido no campo Lote — confirme ou complete os dados.</span>`;
  document.getElementById("e-lot").value = cod;
  document.getElementById("bc-inp").value = "";
}
function limparE() {
  [
    "e-gen",
    "e-com",
    "e-cls",
    "e-dos",
    "e-lot",
    "e-fab",
    "e-resp",
    "e-obs",
    "e-qtd",
    "e-uni",
  ].forEach((id) => {
    const el = document.getElementById(id);
    if (el) el.value = "";
  });
  ["e-forma", "e-loc"].forEach((id) => {
    const el = document.getElementById(id);
    if (el) el.value = "";
  });
  const ori = document.getElementById("e-ori");
  if (ori) ori.value = ori.options[0]?.value || "";
  document.getElementById("e-ctrl").value = "false";
  document.getElementById("e-min").value = "5";
  document.getElementById("e-data").value = hoje();
  document.getElementById("e-val").value = "";
  document.getElementById("e-err").style.display = "none";
}
async function salvarE() {
  const g = (v) => document.getElementById(v)?.value.trim() || "";
  const c = {
    gen: g("e-gen"),
    com: g("e-com"),
    cls: g("e-cls"),
    forma: g("e-forma"),
    dos: g("e-dos"),
    lot: g("e-lot"),
    val: g("e-val"),
    fab: g("e-fab"),
    ori: g("e-ori"),
    qtd: parseInt(document.getElementById("e-qtd")?.value) || 0,
    uni: g("e-uni"),
    loc: g("e-loc"),
    min: parseInt(document.getElementById("e-min")?.value) || 5,
    ctrl: document.getElementById("e-ctrl")?.value === "true",
    resp: g("e-resp"),
    data: g("e-data"),
    obs: g("e-obs"),
  };
  const erros = [];
  if (!c.gen) erros.push("Nome genérico");
  if (!c.com) erros.push("Nome comercial");
  if (!c.forma) erros.push("Forma farmacêutica");
  if (!c.dos) erros.push("Dosagem");
  if (!c.lot) erros.push("Lote");
  if (!c.val) erros.push("Validade");
  if (!c.fab) erros.push("Fabricante");
  if (!c.qtd || c.qtd < 1) erros.push("Quantidade");
  if (!c.resp) erros.push("Responsável");
  if (!c.data) erros.push("Data");
  const ee = document.getElementById("e-err");
  if (erros.length > 0) {
    ee.textContent = "Preencha os campos: " + erros.join(", ");
    ee.style.display = "block";
    return;
  }
  ee.style.display = "none";

  const nm = {
    nomeGenerico: c.gen,
    nomeComercial: c.com,
    classeTerapeutica: c.cls,
    formaFarmaceutica: c.forma,
    dosagem: c.dos,
    dataEntrada: c.data,
    origem: c.ori,
    responsavel: c.resp,
    fabricante: c.fab,
    lote: c.lot,
    validade: c.val,
    qtd: c.qtd,
    unidade: c.uni,
    localizacao: c.loc,
    minimo: c.min,
    controlado: c.ctrl,
  };

  const medId = document.getElementById('e-med-id')?.value;
  const qtdAtual = parseInt(document.getElementById('e-med-qtd-atual')?.value) || 0;

  if (medId) {
    // REPOSIÇÃO: atualiza medicamento existente
    const medAtualizado = {
      id: medId,
      nomeGenerico: c.gen,
      nomeComercial: c.com,
      classeTerapeutica: c.cls,
      formaFarmaceutica: c.forma,
      dosagem: c.dos,
      dataEntrada: c.data,
      origem: c.ori,
      responsavel: c.resp,
      fabricante: c.fab,
      lote: c.lot,
      validade: c.val,
      qtd: qtdAtual + c.qtd,
      unidade: c.uni,
      localizacao: c.loc,
      minimo: c.min,
      controlado: c.ctrl,
    };
    try {
      await dbAtualizarMedicamento(medAtualizado);
      const mov = {
        tipo: 'entrada',
        medId: medId,
        qtd: c.qtd,
        data: c.data,
        responsavel: c.resp,
        obs: c.obs || 'Reposição de estoque',
        lote: c.lot,
      };
      await dbSalvarMovimentacao(mov);
      toast(`Reposição registrada! <strong>${c.gen}</strong> +${c.qtd} un. Novo estoque: ${qtdAtual + c.qtd}.`);
      setTimeout(() => { window.location.href = 'estoque.html'; }, 1000);
    } catch (err) {
      console.error('Erro ao salvar:', err);
      toast('Erro ao salvar dados!');
    }
    return;
  }

  // ENTRADA NOVA: cria novo medicamento
  try {
    const r = await dbSalvarMedicamento(nm);
    nm.id = r.id;
    const mov = {
      tipo: "entrada",
      medId: nm.id,
      qtd: c.qtd,
      data: c.data,
      responsavel: c.resp,
      obs: c.obs || "Entrada inicial",
      lote: c.lot,
    };
    await dbSalvarMovimentacao(mov);
    toast(
      `Medicamento <strong>${c.gen}</strong> cadastrado! +${c.qtd} unidade(s).`,
    );
    setTimeout(() => {
      window.location.href = "estoque.html";
    }, 1000);
  } catch (err) {
    console.error("Erro ao salvar:", err);
    toast("Erro ao salvar dados!");
  }
}

// Popula select de Fabricante
async function popularSelectFabricantes() {
  const sel = document.getElementById("e-fab");
  if (!sel) return;
  try {
    const lista = await dbBuscarFabricantes();
    sel.innerHTML =
      '<option value="">Selecione...</option>' +
      lista.map((f) => `<option value="${f.nome}">${f.nome}</option>`).join("");
  } catch (e) {
    /* silencioso */
  }
}

// Popula select de Localização com locais cadastrados
async function popularSelectLocais() {
  const sel = document.getElementById("e-loc");
  if (!sel) return;
  try {
    const lista = await dbBuscarLocais();
    sel.innerHTML =
      '<option value="">Selecione a localização...</option>' +
      lista.map((l) => `<option value="${l.nome}">${l.nome}</option>`).join("");
  } catch (e) {
    /* silencioso */
  }
}

// Popula select de Origem com doadores cadastrados
async function popularSelectOrigem() {
  const sel = document.getElementById("e-ori");
  if (!sel) return;
  try {
    const lista = await dbBuscarDoadores();
    // Mantém opções fixas já existentes + adiciona doadores
    const fixas = ["Doação", "Aquisição Maanaim", "Outro"];
    const doOpts = lista
      .map((d) => `<option value="Doação ${d.nome}">Doação ${d.nome}</option>`)
      .join("");
    sel.innerHTML =
      fixas.map((f) => `<option value="${f}">${f}</option>`).join("") + doOpts;
  } catch (e) {
    /* sem servidor, mantém fixas */
  }
}

// Popula select de vinculação com medicamentos existentes
async function popularSelectVincular() {
  const sel = document.getElementById('e-vincular');
  if (!sel) return;
  try {
    const lista = await dbBuscarMedicamentos();
    const opts = lista
      .map(m => {
        const dos = m.dosagem ? ` ${m.dosagem}` : '';
        const loc = m.localizacao ? ` · ${m.localizacao}` : '';
        return `<option value="${m.id}">${m.nomeGenerico}${dos} — ${m.nomeComercial}${loc} (estoque: ${m.qtd})</option>`;
      })
      .join('');
    sel.innerHTML = '<option value="">— Cadastrar medicamento novo —</option>' + opts;
  } catch (e) { /* silencioso */ }
}

// Preenche o formulário com dados de um medicamento existente para reposição
async function vincularMed() {
  const sel = document.getElementById('e-vincular');
  const id = sel?.value;
  const msg = document.getElementById('e-vinc-msg');
  const dvdTxt = document.getElementById('e-dvd-txt');

  if (!id) {
    document.getElementById('e-med-id').value = '';
    document.getElementById('e-med-qtd-atual').value = '0';
    if (msg) msg.style.display = 'none';
    if (dvdTxt) dvdTxt.textContent = 'ou preencha manualmente';
    limparE();
    return;
  }

  try {
    const lista = await dbBuscarMedicamentos();
    const med = lista.find(m => String(m.id) === String(id));
    if (!med) return;

    document.getElementById('e-med-id').value = med.id;
    document.getElementById('e-med-qtd-atual').value = med.qtd;

    // Preenche campos fixos do medicamento
    const set = (elId, val) => { const el = document.getElementById(elId); if (el) el.value = val || ''; };
    set('e-gen', med.nomeGenerico);
    set('e-com', med.nomeComercial);
    set('e-cls', med.classeTerapeutica);
    set('e-forma', med.formaFarmaceutica);
    set('e-dos', med.dosagem);
    set('e-fab', med.fabricante);
    set('e-uni', med.unidade);
    set('e-loc', med.localizacao);
    document.getElementById('e-min').value = med.minimo ?? 5;
    document.getElementById('e-ctrl').value = med.controlado ? 'true' : 'false';

    // Limpa campos específicos do novo lote
    set('e-lot', '');
    set('e-val', '');
    set('e-qtd', '');
    set('e-obs', '');

    if (dvdTxt) dvdTxt.textContent = 'confirme ou ajuste os dados abaixo';
    if (msg) {
      msg.style.display = 'block';
      msg.innerHTML = `<span class="material-icons" style="font-size:.95rem;vertical-align:-3px;color:#1e8449">inventory</span> <strong>Reposição:</strong> ${med.nomeGenerico} ${med.dosagem || ''} — Estoque atual: <strong>${med.qtd}</strong> un. Informe o novo lote, validade e quantidade recebida.`;
    }
  } catch (e) {
    console.error(e);
  }
}

// Popula select de Responsável com usuários cadastrados
async function popularSelectResponsavel() {
  const sel = document.getElementById("e-resp");
  if (!sel) return;
  try {
    let lista = [];
    if (usuarios && usuarios.length > 0) {
      lista = usuarios;
    } else {
      lista = await dbBuscarUsuarios();
    }
    const opts = lista
      .map((u) => `<option value="${u.nome}">${u.nome}</option>`)
      .join("");
    sel.innerHTML = '<option value="">Selecione...</option>' + opts;
    // Preenche com usuário logado
    if (window.usuarioAtual?.nome) {
      sel.value = window.usuarioAtual.nome;
    }
  } catch (e) {
    // Se falhar, tenta usar usuário atual
    if (window.usuarioAtual?.nome) {
      sel.innerHTML = `<option value="">Selecione...</option><option value="${window.usuarioAtual.nome}">${window.usuarioAtual.nome}</option>`;
      sel.value = window.usuarioAtual.nome;
    } else {
      // Espera auth estar pronto e tenta novamente
      document.addEventListener('authReady', () => popularSelectResponsavel(), { once: true });
    }
  }
}

// ============================================================
// SAÍDA
// ============================================================
function preencherSelectSaida() {
  const opts = medicamentos
    .map(
      (m) =>
        `<option value="${m.id}">${m.nomeGenerico} — ${m.nomeComercial} (${m.qtd} ${m.unidade || "un."})</option>`,
    )
    .join("");
  const sel = document.getElementById("s2-med");
  if (sel)
    sel.innerHTML = `<option value="">Selecione o medicamento...</option>${opts}`;
  const respEl = document.getElementById("s2-resp");
  if (respEl && !respEl.value) {
    respEl.value = window.usuarioAtual?.nome || "";
  }
  const params = new URLSearchParams(window.location.search);
  const medId = params.get("med");
  if (medId && sel) {
    sel.value = medId;
    updSI2();
  }
}
function updSI2() {
  const id = parseInt(document.getElementById("s2-med").value);
  const el = document.getElementById("s2-info");
  if (!id) {
    el.textContent = "";
    return;
  }
  const m = getMed(id);
  if (!m) return;
  el.innerHTML = `Lote: <strong>${m.lote || "—"}</strong> · Validade: ${vLabel(m.validade)} · Estoque: <strong>${m.qtd} ${m.unidade || "un."}</strong> · Local: ${m.localizacao || "—"}`;
}
async function salvarS2() {
  const id = parseInt(document.getElementById("s2-med").value);
  const qtd = parseInt(document.getElementById("s2-qtd").value);
  const dt = document.getElementById("s2-dt").value;
  const resp = document.getElementById("s2-resp").value.trim();
  const obs = document.getElementById("s2-obs").value.trim();
  const motivo = document.getElementById("s2-motivo")?.value || "Dispensação";
  const paciente = document.getElementById("s2-paciente")?.value.trim();
  let obsFinal = obs || motivo;
  if (motivo === "Dispensação" && paciente) {
    obsFinal = `Paciente: ${paciente}` + (obs ? ` - ${obs}` : "");
  }
  const ee = document.getElementById("s2-err");
  if (!id) {
    ee.textContent = "Selecione um medicamento.";
    ee.style.display = "block";
    return;
  }
  if (!qtd || qtd < 1) {
    ee.textContent = "Informe uma quantidade válida.";
    ee.style.display = "block";
    return;
  }
  if (!resp) {
    ee.textContent = "Informe o responsável.";
    ee.style.display = "block";
    return;
  }
  if (!dt) {
    ee.textContent = "Informe a data.";
    ee.style.display = "block";
    return;
  }
  const m = getMed(id);
  if (m.qtd < qtd) {
    ee.textContent = `Estoque insuficiente. Disponível: ${m.qtd} ${m.unidade || "un."}.`;
    ee.style.display = "block";
    return;
  }
  ee.style.display = "none";

  m.qtd -= qtd;
  const mov = {
    tipo: "saida",
    medId: id,
    qtd,
    data: dt,
    responsavel: resp,
    obs: obsFinal,
    lote: m.lote,
    motivo,
  };

  try {
    await dbAtualizarMedicamento(m);
    const r = await dbSalvarMovimentacao(mov);
    mov.id = r.id;
    movimentacoes.push(mov);
    toast(
      `Baixa de <strong>${qtd}</strong> de <strong>${m.nomeGenerico}</strong> registrada!`,
    );
    setTimeout(() => {
      window.location.href = "estoque.html";
    }, 1000);
  } catch (err) {
    console.error("Erro ao salvar:", err);
    toast("Erro ao salvar dados!");
  }
}

// ============================================================
// TRANSFERÊNCIA
// ============================================================
async function popularSelectTransfMed() {
  const sel = document.getElementById("tr-med");
  if (!sel) return;
  const opts = medicamentos
    .map(
      (m) =>
        `<option value="${m.id}">${m.nomeGenerico} — ${m.nomeComercial} (${m.qtd} ${m.unidade || "un."}) · ${m.localizacao || "—"}</option>`,
    )
    .join("");
  sel.innerHTML = `<option value="">Selecione o medicamento...</option>${opts}`;
}

function updTrInfo() {
  const id = parseInt(document.getElementById("tr-med")?.value);
  const el = document.getElementById("tr-info");
  if (!el) return;
  if (!id) {
    el.textContent = "";
    return;
  }
  const m = getMed(id);
  if (!m) return;
  el.innerHTML = `Local atual: <strong>${m.localizacao || "—"}</strong> · Lote: <strong>${m.lote || "—"}</strong> · Validade: ${vLabel(m.validade)} · Estoque: <strong>${m.qtd} ${m.unidade || "un."}</strong>`;
}

async function popularSelectTransfLoc() {
  const sel = document.getElementById("tr-loc");
  if (!sel) return;
  try {
    const lista = await dbBuscarLocais();
    sel.innerHTML =
      '<option value="">Selecione o destino...</option>' +
      lista.map((l) => `<option value="${l.nome}">${l.nome}</option>`).join("");
  } catch (e) {}
}

async function salvarTransferencia() {
  const medId = parseInt(document.getElementById("tr-med")?.value);
  const novaLocalizacao = document.getElementById("tr-loc")?.value.trim();
  const qtd = parseInt(document.getElementById("tr-qtd")?.value);
  const responsavel = document.getElementById("tr-resp")?.value.trim();
  const data = document.getElementById("tr-data")?.value;
  const obs = document.getElementById("tr-obs")?.value.trim();
  const ee = document.getElementById("tr-err");

  if (!medId) {
    ee.textContent = "Selecione um medicamento.";
    ee.style.display = "block";
    return;
  }
  if (!novaLocalizacao) {
    ee.textContent = "Selecione o local de destino.";
    ee.style.display = "block";
    return;
  }
  if (!qtd || qtd < 1) {
    ee.textContent = "Informe uma quantidade válida.";
    ee.style.display = "block";
    return;
  }
  if (!responsavel) {
    ee.textContent = "Informe o responsável.";
    ee.style.display = "block";
    return;
  }
  if (!data) {
    ee.textContent = "Informe a data.";
    ee.style.display = "block";
    return;
  }

  const m = getMed(medId);
  if (m && novaLocalizacao === m.localizacao) {
    ee.textContent = "O destino é o mesmo local atual.";
    ee.style.display = "block";
    return;
  }
  if (m && qtd > m.qtd) {
    ee.textContent = `Estoque insuficiente. Disponível: ${m.qtd} ${m.unidade || "un."}.`;
    ee.style.display = "block";
    return;
  }
  ee.style.display = "none";

  try {
    const r = await dbTransferir({
      medId,
      novaLocalizacao,
      qtd,
      responsavel,
      data,
      obs,
    });
    // Atualiza memória local
    if (m) {
      if (qtd === m.qtd) {
        m.localizacao = novaLocalizacao;
      } else {
        m.qtd -= qtd;
        if (r.novoId)
          medicamentos.push({
            ...m,
            id: r.novoId,
            qtd,
            localizacao: novaLocalizacao,
          });
      }
    }
    toast(
      `Transferência de <strong>${qtd}</strong> de <strong>${m ? m.nomeGenerico : ""}</strong> para <strong>${novaLocalizacao}</strong> registrada!`,
    );
    setTimeout(() => {
      window.location.href = "estoque.html";
    }, 1000);
  } catch (err) {
    console.error("Erro na transferência:", err);
    ee.textContent = "Erro ao registrar transferência.";
    ee.style.display = "block";
  }
}

// ============================================================
// HISTÓRICO
// ============================================================
function filtrarHistorico() {
  histFiltroTipo    = document.getElementById("h-tipo")?.value    || "";
  histFiltroLote    = (document.getElementById("h-lote")?.value   || "").trim().toLowerCase();
  histFiltroResp    = (document.getElementById("h-resp")?.value   || "").trim().toLowerCase();
  histFiltroDataIni = document.getElementById("h-data-ini")?.value || "";
  histFiltroDataFim = document.getElementById("h-data-fim")?.value || "";
  atualizarHistorico();
}

function limparFiltrosHistorico() {
  ["h-tipo","h-lote","h-resp","h-data-ini","h-data-fim"].forEach((id) => {
    const el = document.getElementById(id);
    if (el) el.value = "";
  });
  histFiltroTipo = histFiltroLote = histFiltroResp = histFiltroDataIni = histFiltroDataFim = "";
  atualizarHistorico();
}

function atualizarHistorico() {
  let lista = [...movimentacoes].sort((a, b) => b.data.localeCompare(a.data));

  // Aplicar filtros
  if (histFiltroTipo)    lista = lista.filter((m) => m.tipo === histFiltroTipo);
  if (histFiltroLote)    lista = lista.filter((m) => (m.lote || "").toLowerCase().includes(histFiltroLote));
  if (histFiltroResp)    lista = lista.filter((m) => (m.responsavel || "").toLowerCase().includes(histFiltroResp));
  if (histFiltroDataIni) lista = lista.filter((m) => m.data >= histFiltroDataIni);
  if (histFiltroDataFim) lista = lista.filter((m) => m.data <= histFiltroDataFim);

  const totalEl = document.getElementById("total-mov");
  if (totalEl) totalEl.textContent = lista.length;

  const div = document.getElementById("tabela-historico");
  if (!div) return;
  if (lista.length === 0) {
    div.innerHTML = `<div style="text-align:center;padding:48px;color:var(--g400)"><span class="material-icons" style="font-size:2rem;display:block;margin-bottom:8px">assignment</span>Nenhuma movimentação encontrada.</div>`;
  } else {
    div.innerHTML = `<table><thead><tr><th data-label="Tipo">Tipo</th><th data-label="Medicamento">Medicamento</th><th data-label="Qtd.">Qtd.</th><th data-label="Motivo">Motivo</th><th data-label="Lote">Lote</th><th data-label="Responsável">Responsável</th><th data-label="Data">Data</th><th data-label="Obs.">Obs.</th></tr></thead>
      <tbody>${lista
        .map((mov) => {
          const m = getMed(mov.medId);
          const isTr = mov.tipo === "transferencia";
          const isIn = mov.tipo === "entrada";
          const badgeCls = isIn ? "b-in" : isTr ? "b-tr" : "b-out2";
          const badgeTxt = isIn
            ? "↑ Entrada"
            : isTr
              ? "⇄ Transferência"
              : "↓ Saída";
          const qtdColor = isIn ? "#1E8449" : isTr ? "#2980B9" : "var(--red)";
          const qtdSign = isIn ? "+" : isTr ? "⇄" : "−";
          return `<tr>
          <td data-label="Tipo"><span class="bdg ${badgeCls}">${badgeTxt}</span></td>
          <td data-label="Medicamento"><div class="tdm">${m ? m.nomeGenerico : "—"}</div><div class="tds">${m ? `${m.nomeComercial || ""} ${m.dosagem || ""}` : ""}</div></td>
          <td data-label="Qtd."><strong style="color:${qtdColor}">${qtdSign}${mov.qtd}</strong></td>
          <td data-label="Motivo" style="font-size:.76rem">${mov.motivo || "—"}</td>
          <td data-label="Lote" style="font-family:monospace;font-size:.76rem">${mov.lote || "—"}</td>
          <td data-label="Responsável">${mov.responsavel || "—"}</td>
          <td data-label="Data" style="white-space:nowrap">${fmtD(mov.data)}</td>
          <td data-label="Obs." style="font-size:.76rem;color:var(--g600)">${mov.obs || "—"}</td>
        </tr>`;
        })
        .join("")}</tbody></table>`;
  }
}

// ============================================================
// ALERTAS
// ============================================================
function renderAlertas() {
  const el = document.getElementById("alertas-content");
  const esg = medicamentos.filter((m) => m.qtd === 0);
  const bxs = medicamentos.filter((m) => m.qtd > 0 && m.qtd <= m.minimo);
  const vnc = medicamentos.filter((m) => vStatus(m.validade) === "vencido");
  const vbr = medicamentos.filter(
    (m) => vStatus(m.validade) === "breve" && m.qtd > 0,
  );
  const ctr = medicamentos.filter((m) => m.controlado && m.qtd > 0);

  let h = `<div class="ph"><div><div class="pt">Central de Alertas</div><div class="ps">${esg.length + bxs.length + vnc.length} alerta(s) crítico(s)</div></div></div>`;

  if (!esg.length && !bxs.length && !vnc.length && !vbr.length) {
    h += `<div style="text-align:center;margin-top:55px;color:var(--g400)">
      <span class="material-icons" style="font-size:3rem;color:#1E8449;display:block;margin-bottom:8px">check_circle</span>
      <div style="font-size:.95rem;font-weight:700;color:#1E8449">Tudo em ordem!</div>
      <div>Nenhum alerta crítico no momento.</div>
    </div>`;
  }

  const tbl = (rows, hdrs) =>
    `<div class="cp"><table><thead><tr>${hdrs.map((h) => `<th>${h}</th>`).join("")}</tr></thead><tbody>${rows}</tbody></table></div>`;

  if (ctr.length)
    h += `<div style="margin-bottom:18px">
    <div style="font-size:.72rem;font-weight:700;color:#8E44AD;text-transform:uppercase;letter-spacing:1px;margin-bottom:7px">
      <span class="material-icons" style="font-size:.9rem;vertical-align:-2px">medication</span> Medicamentos Controlados em Estoque (${ctr.length})
    </div>${tbl(ctr.map((m) => `<tr><td data-label="Medicamento"><div class="tdm">${m.nomeGenerico}</div></td><td data-label="Nome Comercial" style="font-size:.79rem">${m.nomeComercial}</td><td data-label="Lote" style="font-family:monospace;font-size:.76rem">${m.lote || "—"}</td><td data-label="Validade">${vLabel(m.validade)}</td><td data-label="Qtd."><strong>${m.qtd}</strong> ${m.unidade}</td><td data-label="Local" style="font-size:.78rem">${m.localizacao || "—"}</td></tr>`).join(""), ["Medicamento", "Nome Comercial", "Lote", "Validade", "Qtd.", "Local"])}</div>`;

  if (vnc.length)
    h += `<div style="margin-bottom:18px">
    <div style="font-size:.72rem;font-weight:700;color:var(--red);text-transform:uppercase;letter-spacing:1px;margin-bottom:7px">
      <span class="material-icons" style="font-size:.9rem;vertical-align:-2px">cancel</span> Vencidos (${vnc.length})
    </div>${tbl(vnc.map((m) => `<tr><td data-label="Medicamento"><div class="tdm">${m.nomeGenerico}</div><div class="tds">${m.nomeComercial}</div></td><td data-label="Lote" style="font-family:monospace;font-size:.76rem">${m.lote || "—"}</td><td data-label="Validade">${vLabel(m.validade)}</td><td data-label="Qtd."><strong style="color:var(--red)">${m.qtd}</strong> ${m.unidade}</td><td data-label="Local">${m.localizacao || "—"}</td></tr>`).join(""), ["Medicamento", "Lote", "Validade", "Qtd.", "Local"])}</div>`;

  if (esg.length)
    h += `<div style="margin-bottom:18px">
    <div style="font-size:.72rem;font-weight:700;color:#C0392B;text-transform:uppercase;letter-spacing:1px;margin-bottom:7px">
      <span class="material-icons" style="font-size:.9rem;vertical-align:-2px">block</span> Esgotados (${esg.length})
    </div>${tbl(esg.map((m) => `<tr><td data-label="Medicamento"><div class="tdm">${m.nomeGenerico}</div><div class="tds">${m.nomeComercial}</div></td><td data-label="Forma Farm.">${m.formaFarmaceutica}</td><td data-label="Mínimo">${m.minimo}</td><td data-label="Local">${m.localizacao || "—"}</td><td data-label="Ação"><a href="entrada.html" class="btn btn-sm btn-p">+ Dar Entrada</a></td></tr>`).join(""), ["Medicamento", "Forma Farm.", "Mínimo", "Local", "Ação"])}</div>`;

  if (bxs.length)
    h += `<div style="margin-bottom:18px">
    <div style="font-size:.72rem;font-weight:700;color:#E67E22;text-transform:uppercase;letter-spacing:1px;margin-bottom:7px">
      <span class="material-icons" style="font-size:.9rem;vertical-align:-2px">warning_amber</span> Estoque Baixo (${bxs.length})
    </div>${tbl(bxs.map((m) => `<tr><td data-label="Medicamento"><div class="tdm">${m.nomeGenerico}</div><div class="tds">${m.nomeComercial}</div></td><td data-label="Qtd. Atual" style="color:#E67E22;font-weight:700">${m.qtd} ${m.unidade}</td><td data-label="Mínimo">${m.minimo}</td><td data-label="Déficit" style="color:var(--red);font-weight:600">−${m.minimo - m.qtd}</td><td data-label="Local">${m.localizacao || "—"}</td><td data-label="Ação"><a href="entrada.html" class="btn btn-sm btn-p">+ Dar Entrada</a></td></tr>`).join(""), ["Medicamento", "Qtd. Atual", "Mínimo", "Déficit", "Local", "Ação"])}</div>`;

  if (vbr.length)
    h += `<div>
    <div style="font-size:.72rem;font-weight:700;color:#D35400;text-transform:uppercase;letter-spacing:1px;margin-bottom:7px">
      <span class="material-icons" style="font-size:.9rem;vertical-align:-2px">hourglass_empty</span> Validade Próxima &lt;90 dias (${vbr.length})
    </div>${tbl(vbr.map((m) => `<tr><td data-label="Medicamento"><div class="tdm">${m.nomeGenerico}</div><div class="tds">${m.nomeComercial}</div></td><td data-label="Lote" style="font-family:monospace;font-size:.76rem">${m.lote || "—"}</td><td data-label="Validade">${vLabel(m.validade)}</td><td data-label="Qtd."><strong>${m.qtd}</strong> ${m.unidade}</td><td data-label="Local">${m.localizacao || "—"}</td></tr>`).join(""), ["Medicamento", "Lote", "Validade", "Qtd.", "Local"])}</div>`;

  el.innerHTML = h;
}

// ============================================================
// RELATÓRIOS
// ============================================================
function renderRelatorios() {
  const el = document.getElementById("relatorios-content");
  const tU = medicamentos.reduce((s, m) => s + m.qtd, 0);
  const ent = movimentacoes
    .filter((m) => m.tipo === "entrada")
    .reduce((s, m) => s + m.qtd, 0);
  const sai = movimentacoes
    .filter((m) => m.tipo === "saida")
    .reduce((s, m) => s + m.qtd, 0);

  const fMap = {};
  medicamentos.forEach((m) => {
    fMap[m.formaFarmaceutica || "Outro"] =
      (fMap[m.formaFarmaceutica || "Outro"] || 0) + m.qtd;
  });
  const fArr = Object.entries(fMap)
    .sort((a, b) => b[1] - a[1])
    .slice(0, 8);
  const mx = fArr.length > 0 ? fArr[0][1] : 1;

  const lMap = {};
  medicamentos.forEach((m) => {
    if (m.localizacao) lMap[m.localizacao] = (lMap[m.localizacao] || 0) + 1;
  });
  const lArr = Object.entries(lMap).sort((a, b) => b[1] - a[1]);

  const cols = [
    "var(--red)",
    "#C0392B",
    "#E74C3C",
    "#E67E22",
    "#D35400",
    "#922B21",
    "#2980B9",
    "#27AE60",
  ];

  let h = `<div class="ph"><div><div class="pt">Relatórios</div><div class="ps">Análise do estoque · Ambulatório Maanaim</div></div></div>`;

  h += `<div class="cg" style="margin-bottom:20px">
    <div class="sc red"><div class="sc-acc"></div><div class="sc-lbl">Itens no Sistema</div><div class="sc-val">${medicamentos.length}</div></div>
    <div class="sc"><div class="sc-acc"></div><div class="sc-lbl">Total Unidades</div><div class="sc-val">${tU.toLocaleString("pt-BR")}</div></div>
    <div class="sc green"><div class="sc-acc"></div><div class="sc-lbl">Total Entradas</div><div class="sc-val">+${ent}</div></div>
    <div class="sc danger"><div class="sc-acc"></div><div class="sc-lbl">Total Saídas</div><div class="sc-val">−${sai}</div></div>
  </div>`;

  h += `<div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:18px">
    <div class="cp" style="padding:20px">
      <div class="cpt" style="margin-bottom:14px">Por Forma Farmacêutica</div>
      ${fArr
        .map(
          ([f, q], i) => `<div style="margin-bottom:10px">
        <div style="display:flex;justify-content:space-between;margin-bottom:3px">
          <span style="font-size:.8rem;font-weight:500">${f}</span>
          <span style="font-size:.72rem;color:var(--g400)">${q} un.</span>
        </div>
        <div class="pb"><div class="pf" style="width:${((q / mx) * 100).toFixed(1)}%;background:${cols[i % cols.length]}"></div></div>
      </div>`,
        )
        .join("")}
    </div>
    <div class="cp" style="padding:20px">
      <div class="cpt" style="margin-bottom:14px">Por Localização</div>
      ${lArr
        .map(
          ([
            l,
            c,
          ]) => `<div style="display:flex;justify-content:space-between;align-items:center;padding:6px 0;border-bottom:1px solid var(--g100)">
        <span style="font-size:.81rem">${l}</span>
        <span class="bdg b-ok">${c} item(s)</span>
      </div>`,
        )
        .join("")}
    </div>
  </div>`;

  h += `<div class="cp">
    <div class="cph"><div class="cpt">Todos os Registros do Estoque</div></div>
    <table><thead><tr><th>Nome Genérico</th><th>Nome Comercial</th><th>Forma</th><th>Fabricante</th><th>Lote</th><th>Validade</th><th>Local</th><th>Origem</th><th>Qtd.</th><th>Mínimo</th><th>Status</th></tr></thead>
    <tbody>${medicamentos
      .map((m) => {
        const s = getStat(m);
        const deficit = m.qtd < m.minimo ? m.minimo - m.qtd : 0;
        return `<tr>
        <td data-label="Nome Genérico"><div class="tdm">${m.nomeGenerico}</div>${m.controlado ? '<span class="bdg b-ctrl">CTRL</span>' : ""}</td>
        <td data-label="Nome Comercial" style="font-size:.78rem">${m.nomeComercial || "—"}</td>
        <td data-label="Forma" style="font-size:.77rem">${m.formaFarmaceutica || "—"}</td>
        <td data-label="Fabricante" style="font-size:.77rem">${m.fabricante || "—"}</td>
        <td data-label="Lote" style="font-family:monospace;font-size:.75rem">${m.lote || "—"}</td>
        <td data-label="Validade" style="font-size:.77rem">${vLabel(m.validade)}</td>
        <td data-label="Local" style="font-size:.77rem">${m.localizacao || "—"}</td>
        <td data-label="Origem" style="font-size:.75rem">${m.origem || "—"}</td>
        <td data-label="Qtd."><strong>${m.qtd}</strong> <span style="font-size:.68rem;color:var(--g400)">${m.unidade || ""}</span></td>
        <td data-label="Mínimo" style="text-align:center">
          <span style="font-weight:600">${m.minimo}</span>
          ${deficit > 0 ? `<br><span style="font-size:.68rem;color:var(--red);font-weight:600">−${deficit}</span>` : ""}
        </td>
        <td data-label="Status">${sBadge(s)}</td>
      </tr>`;
      })
      .join("")}</tbody></table>
  </div>`;

  el.innerHTML = h;
}

// ============================================================
// INICIALIZAÇÃO
// ============================================================
async function iniciarAplicativo() {
  // — fase 1: carregar dados do servidor —
  let meds, movs;
  try {
    [meds, movs] = await Promise.all([
      dbBuscarMedicamentos(),
      dbBuscarMovimentacoes(),
    ]);
  } catch (err) {
    if (err.message === "401") return; // auth.js já redireciona para login
    // Só exibe banner se for falha de rede real (TypeError: Failed to fetch)
    if (err instanceof TypeError) {
      const aviso = document.createElement("div");
      aviso.innerHTML = `<div style="position:fixed;top:0;left:0;right:0;background:#C8102E;color:#fff;padding:12px 20px;text-align:center;z-index:9999;font-size:.88rem">
        <span class="material-icons" style="vertical-align:-4px;margin-right:6px">wifi_off</span>
        Servidor indisponível. Inicie o servidor com <strong>node server.js</strong> e recarregue a página.
      </div>`;
      document.body.appendChild(aviso);
    }
    return;
  }

  medicamentos = meds;
  movimentacoes = movs;
  dbLoaded = true;

  // — fase 2: renderizar cada função de forma isolada —
  [
    atualizarDashboard,
    atualizarEstoque,
    atualizarHistorico,
    renderAlertas,
    renderRelatorios,
    carregarDoadores,
    carregarFabricantes,
    carregarUsuarios,
    carregarLocais,
    popularSelectOrigem,
    popularSelectResponsavel,
    popularSelectFabricantes,
    popularSelectLocais,
    popularSelectTransfMed,
    popularSelectTransfLoc,
    preencherSelectSaida,
    () => togglePaciente("s2"),
    renderAuditoria,
  ].forEach((fn) => {
    try {
      fn();
    } catch (e) {
      /* elemento não existe nesta página */
    }
  });

  _initBuscaGlobal();
}

// ============================================================
// CADASTROS - Doadores
// ============================================================
async function carregarDoadores() {
  try {
    doadores = await dbBuscarDoadores();
    const tbody = document.getElementById("lista-doadores");
    if (!tbody) return;

    if (doadores.length === 0) {
      tbody.innerHTML =
        '<tr><td colspan="4" style="text-align:center;padding:30px;color:var(--g400)">Nenhum doador cadastrado.</td></tr>';
      return;
    }

    tbody.innerHTML = doadores
      .map(
        (d) => `<tr>
      <td data-label="Nome">${d.nome}</td>
      <td data-label="Telefone">${d.telefone || "—"}</td>
      <td data-label="Obs.">${d.obs || "—"}</td>
      <td data-label="Ação"><button class="btn btn-sm btn-ds" onclick="excluirDoador(${d.id})"><span class="material-icons" style="font-size:.9rem">close</span></button></td>
    </tr>`,
      )
      .join("");
  } catch (err) {
    console.error("Erro ao carregar doadores:", err);
  }
}

async function salvarDoador() {
  const nome = document.getElementById("d-nome")?.value.trim();
  const telefone = document.getElementById("d-tel")?.value.trim();
  const obs = document.getElementById("d-obs")?.value.trim();

  if (!nome) return alert("Preencha o nome do doador!");

  try {
    await dbSalvarDoador({ nome, telefone, obs });
    document.getElementById("d-nome").value = "";
    document.getElementById("d-tel").value = "";
    document.getElementById("d-obs").value = "";
    await carregarDoadores();
    toast("Doador adicionado!");
  } catch (err) {
    console.error("Erro ao salvar doador:", err);
    alert("Erro ao salvar doador!");
  }
}

async function excluirDoador(id) {
  if (!confirm("Excluir este doador?")) return;
  try {
    await dbExcluirDoador(id);
    await carregarDoadores();
    toast("Doador excluído!");
  } catch (err) {
    console.error("Erro ao excluir doador:", err);
  }
}

// ============================================================
// CADASTROS - Fabricantes
// ============================================================
async function carregarFabricantes() {
  try {
    fabricantes = await dbBuscarFabricantes();
    const tbody = document.getElementById("lista-fabricantes");
    if (!tbody) return;

    if (fabricantes.length === 0) {
      tbody.innerHTML =
        '<tr><td colspan="3" style="text-align:center;padding:30px;color:var(--g400)">Nenhum fabricante cadastrado.</td></tr>';
      return;
    }

    tbody.innerHTML = fabricantes
      .map(
        (f) => `<tr>
      <td data-label="Nome">${f.nome}</td>
      <td data-label="CNPJ">${f.cnpj || "—"}</td>
      <td data-label="Ação"><button class="btn btn-sm btn-ds" onclick="excluirFabricante(${f.id})"><span class="material-icons" style="font-size:.9rem">close</span></button></td>
    </tr>`,
      )
      .join("");
  } catch (err) {
    console.error("Erro ao carregar fabricantes:", err);
  }
}

async function salvarFabricante() {
  const nome = document.getElementById("f-nome")?.value.trim();
  const cnpj = document.getElementById("f-cnpj")?.value.trim();

  if (!nome) return alert("Preencha o nome do fabricante!");

  try {
    await dbSalvarFabricante({ nome, cnpj });
    document.getElementById("f-nome").value = "";
    document.getElementById("f-cnpj").value = "";
    await carregarFabricantes();
    toast("Fabricante adicionado!");
  } catch (err) {
    console.error("Erro ao salvar fabricante:", err);
    alert("Erro ao salvar fabricante!");
  }
}

async function excluirFabricante(id) {
  if (!confirm("Excluir este fabricante?")) return;
  try {
    await dbExcluirFabricante(id);
    await carregarFabricantes();
    toast("Fabricante excluído!");
  } catch (err) {
    console.error("Erro ao excluir fabricante:", err);
  }
}

// ============================================================
// CADASTROS - Usuários
// ============================================================
async function carregarUsuarios() {
  try {
    usuarios = await dbBuscarUsuarios();
    const tbody = document.getElementById("lista-usuarios");
    if (!tbody) return;

    if (usuarios.length === 0) {
      tbody.innerHTML =
        '<tr><td colspan="5" style="text-align:center;padding:30px;color:var(--g400)">Nenhum usuário cadastrado.</td></tr>';
      return;
    }

    const _rl = { admin:"Admin", gerente:"Gerente", medico:"Médico", enfermeira:"Enfermeira", farmaceutico:"Farmacêutico", movimentacao:"Moviment.", entrada:"Entrada", saida:"Saída", visualizacao:"Visualiz." };
    const _rb = { admin:"b-ctrl", gerente:"b-in", medico:"b-info", enfermeira:"b-warn", farmaceutico:"b-purple", movimentacao:"b-ok", entrada:"b-ok", saida:"b-ok", visualizacao:"" };
    tbody.innerHTML = usuarios
      .map(
        (u) => `<tr>
      <td data-label="Nome">${u.nome}</td>
      <td data-label="Email">${u.email}</td>
      <td data-label="Tipo"><span class="bdg ${_rb[u.tipo] || ""}">${_rl[u.tipo] || u.tipo}</span></td>
      <td data-label="Módulos"><button class="btn btn-sm btn-g" onclick="abrirGerenciarModulos(${u.id})"><span class="material-icons" style="font-size:.9rem">settings</span></button></td>
      <td data-label="Ações"><div style="display:flex;gap:4px">
        <button class="btn btn-sm btn-s" onclick="abrirEditarUsuario(${u.id})"><span class="material-icons" style="font-size:.9rem">edit</span></button>
        <button class="btn btn-sm btn-ds" onclick="excluirUsuario(${u.id})"><span class="material-icons" style="font-size:.9rem">close</span></button>
      </div></td>
    </tr>`,
      )
      .join("");
  } catch (err) {
    console.error("Erro ao carregar usuários:", err);
  }
}

async function salvarUsuario() {
  const nome = document.getElementById("u-nome")?.value.trim();
  const email = document.getElementById("u-email")?.value.trim();
  const senha = document.getElementById("u-senha")?.value;
  const tipo = document.getElementById("u-tipo")?.value || "usuario";

  if (!nome) return alert("Preencha o nome!");
  if (!email) return alert("Preencha o email!");
  if (!senha) return alert("Preencha a senha!");

  try {
    await dbSalvarUsuario({ nome, email, senha, tipo });
    document.getElementById("u-nome").value = "";
    document.getElementById("u-email").value = "";
    document.getElementById("u-senha").value = "";
    document.getElementById("u-tipo").value = "usuario";
    await carregarUsuarios();
    toast("Usuário adicionado!");
  } catch (err) {
    console.error("Erro ao salvar usuário:", err);
    alert("Erro ao salvar usuário!");
  }
}

async function excluirUsuario(id) {
  if (!confirm("Excluir este usuário?")) return;
  try {
    await dbExcluirUsuario(id);
    await carregarUsuarios();
    toast("Usuário excluído!");
  } catch (err) {
    console.error("Erro ao excluir usuário:", err);
  }
}

function abrirEditarUsuario(id) {
  const u = usuarios.find((x) => x.id === id);
  if (!u) return;
  document.getElementById("eu-id").value = u.id;
  document.getElementById("eu-nome").value = u.nome;
  document.getElementById("eu-email").value = u.email;
  document.getElementById("eu-senha").value = "";
  document.getElementById("eu-tipo").value = u.tipo;
  openModal("modal-editar-usuario");
}

async function salvarEditarUsuario() {
  const id = parseInt(document.getElementById("eu-id").value);
  const nome = document.getElementById("eu-nome")?.value.trim();
  const email = document.getElementById("eu-email")?.value.trim();
  const senha = document.getElementById("eu-senha")?.value;
  const tipo = document.getElementById("eu-tipo")?.value;

  if (!nome) return alert("Preencha o nome!");
  if (!email) return alert("Preencha o email!");

  try {
    await dbAtualizarUsuario({ id, nome, email, senha, tipo });
    closeModal("modal-editar-usuario");
    await carregarUsuarios();
    toast("Usuário atualizado!");
  } catch (err) {
    console.error("Erro ao atualizar usuário:", err);
    alert("Erro ao atualizar usuário!");
  }
}

// ============================================================
// CADASTROS - Módulos de Acesso
// ============================================================
let modulosUsuarioAtual = [];

async function abrirGerenciarModulos(usuarioId) {
  const u = usuarios.find(x => x.id === usuarioId);
  if (!u) return;

  document.getElementById('gm-usuario-id').value = usuarioId;

  const rolesMed = ['admin','gerente','movimentacao','entrada','saida','visualizacao','farmaceutico'];
  const rolesAt  = ['admin','gerente','medico','enfermeira','farmaceutico'];

  try {
    // Busca os módulos extras do usuário alvo (não do admin logado)
    const r = await fetch('/api/usuarios/' + usuarioId);
    const alvo = await r.json();
    const mods = alvo.modulos || [];

    document.getElementById('gm-mod-medicamentos').checked = rolesMed.includes(u.tipo) || mods.includes('medicamentos');
    document.getElementById('gm-mod-atendimentos').checked = rolesAt.includes(u.tipo)  || mods.includes('atendimentos');
  } catch (err) {
    document.getElementById('gm-mod-medicamentos').checked = rolesMed.includes(u.tipo);
    document.getElementById('gm-mod-atendimentos').checked = rolesAt.includes(u.tipo);
  }

  openModal('modal-gerenciar-modulos');
}

async function salvarModulosAcesso() {
  const usuarioId = document.getElementById('gm-usuario-id').value;
  if (!usuarioId) return;
  
  const modulos = [];
  if (document.getElementById('gm-mod-medicamentos').checked) modulos.push('medicamentos');
  if (document.getElementById('gm-mod-atendimentos').checked) modulos.push('atendimentos');
  
  try {
    const r = await fetch('/api/usuarios/' + usuarioId + '/modulos', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ modulos })
    });
    if (!r.ok) throw new Error('Erro');
    closeModal('modal-gerenciar-modulos');
    toast('Módulos de acesso atualizados!');
  } catch (err) {
    console.error(err);
    alert('Erro ao salvar módulos!');
  }
}

// ============================================================
// CADASTROS - Locais de Estoque
// ============================================================
async function carregarLocais() {
  try {
    locais = await dbBuscarLocais();
    const tbody = document.getElementById("lista-locais");
    if (!tbody) return;

    if (locais.length === 0) {
      tbody.innerHTML =
        '<tr><td colspan="2" style="text-align:center;padding:30px;color:var(--g400)">Nenhum local cadastrado.</td></tr>';
      return;
    }

    tbody.innerHTML = locais
      .map(
        (l) => `<tr>
      <td data-label="Local">${l.nome}</td>
      <td data-label="Ação"><button class="btn btn-sm btn-ds" onclick="excluirLocal(${l.id})"><span class="material-icons" style="font-size:.9rem">close</span></button></td>
    </tr>`,
      )
      .join("");
  } catch (err) {
    console.error("Erro ao carregar locais:", err);
  }
}

async function salvarLocal() {
  const nome = document.getElementById("l-nome")?.value.trim();
  if (!nome) return alert("Preencha o nome do local!");

  try {
    await dbSalvarLocal({ nome });
    document.getElementById("l-nome").value = "";
    await carregarLocais();
    toast("Local adicionado!");
  } catch (err) {
    console.error("Erro ao salvar local:", err);
    alert("Erro ao salvar local!");
  }
}

async function excluirLocal(id) {
  if (!confirm("Excluir este local?")) return;
  try {
    await dbExcluirLocal(id);
    await carregarLocais();
    toast("Local excluído!");
  } catch (err) {
    console.error("Erro ao excluir local:", err);
  }
}

// ============================================================
// AUDITORIA
// ============================================================
async function renderAuditoria() {
  const el = document.getElementById("tabela-auditoria");
  if (!el) return;

  const params = {
    acao:     document.getElementById("aud-acao")?.value     || "",
    entidade: document.getElementById("aud-entidade")?.value || "",
    usuario:  document.getElementById("aud-usuario")?.value  || "",
    dataIni:  document.getElementById("aud-data-ini")?.value || "",
    dataFim:  document.getElementById("aud-data-fim")?.value || "",
  };

  try {
    const dados = await dbBuscarAuditoria(params);
    const totalEl = document.getElementById("total-audit");
    if (totalEl) totalEl.textContent = dados.length;

    if (dados.length === 0) {
      el.innerHTML = `<div style="text-align:center;padding:48px;color:var(--g400)"><span class="material-icons" style="font-size:2rem;display:block;margin-bottom:8px">manage_search</span>Nenhum registro encontrado.</div>`;
      return;
    }

    const acaoBadge = (a) => {
      if (a === "criar")   return `<span class="bdg b-ok">Criar</span>`;
      if (a === "editar")  return `<span class="bdg b-in">Editar</span>`;
      if (a === "excluir") return `<span class="bdg b-out2">Excluir</span>`;
      return `<span class="bdg">${a}</span>`;
    };
    const entLabel = {
      medicamento: "Medicamento", usuario: "Usuário",
      doador: "Doador", fabricante: "Fabricante", local: "Local",
    };

    el.innerHTML = `<table>
      <thead><tr>
        <th>Data/Hora</th>
        <th>Usuário</th>
        <th>Ação</th>
        <th>Entidade</th>
        <th>Descrição</th>
      </tr></thead>
      <tbody>${dados.map((a) => `<tr>
        <td data-label="Data/Hora" style="font-size:.75rem;white-space:nowrap;font-family:monospace">${a.criadoEm}</td>
        <td data-label="Usuário"><strong>${a.usuarioNome}</strong></td>
        <td data-label="Ação">${acaoBadge(a.acao)}</td>
        <td data-label="Entidade" style="font-size:.8rem">${entLabel[a.entidade] || a.entidade}</td>
        <td data-label="Descrição" style="font-size:.8rem;color:var(--g600)">${a.descricao || "—"}</td>
      </tr>`).join("")}</tbody>
    </table>`;
  } catch (err) {
    console.error("Erro ao carregar auditoria:", err);
    el.innerHTML = `<div style="text-align:center;padding:30px;color:var(--red)">Sem permissão ou erro ao carregar auditoria.</div>`;
  }
}

async function filtrarAuditoria() {
  await renderAuditoria();
}

function limparFiltrosAuditoria() {
  ["aud-acao", "aud-entidade", "aud-usuario", "aud-data-ini", "aud-data-fim"].forEach((id) => {
    const el = document.getElementById(id);
    if (el) el.value = "";
  });
  renderAuditoria();
}

// ============================================================
// BUSCA GLOBAL (Ctrl+K)
// ============================================================
const _isPaginaAtendimento = () => {
  const path = window.location.pathname;
  return path.includes('/at-') || path.includes('/atendimento') || path.includes('at-fila') || path.includes('at-pacientes') || path.includes('at-dashboard');
};

function _initBuscaGlobal() {
  if (document.getElementById("modal-busca-global")) return;

  const isAtendimento = _isPaginaAtendimento();
  const placeholder = isAtendimento ? "Buscar paciente..." : "Buscar medicamento...";
  const icon = isAtendimento ? "person" : "medication";

  const modal = document.createElement("div");
  modal.id = "modal-busca-global";
  modal.style.cssText =
    "display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,.45);z-index:9000;align-items:flex-start;justify-content:center;padding-top:72px";
  modal.innerHTML = `
    <div style="background:#fff;border-radius:14px;width:calc(100% - 32px);max-width:560px;box-shadow:0 8px 40px rgba(0,0,0,.2);overflow:hidden">
      <div style="display:flex;align-items:center;gap:10px;padding:14px 16px;border-bottom:1px solid var(--g100)">
        <span class="material-icons" style="color:var(--g400)">${icon}</span>
        <input id="bg-input" type="text" autocomplete="off" placeholder="${placeholder}"
          style="flex:1;border:none;outline:none;font-size:1rem;background:transparent"
          oninput="_buscaGlobalFiltrar()" />
        <kbd style="font-size:.68rem;color:var(--g400);background:var(--g50);border:1px solid var(--g200);border-radius:4px;padding:2px 6px">Esc</kbd>
      </div>
      <div id="bg-results" style="max-height:380px;overflow-y:auto;padding:6px 0"></div>
    </div>`;
  modal.addEventListener("click", (e) => { if (e.target === modal) _fecharBuscaGlobal(); });
  document.body.appendChild(modal);

  document.addEventListener("keydown", (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key === "k") {
      e.preventDefault();
      _abrirBuscaGlobal();
    }
    if (e.key === "Escape") _fecharBuscaGlobal();
  });
}

function _abrirBuscaGlobal() {
  const modal = document.getElementById("modal-busca-global");
  if (!modal) return;
  modal.style.display = "flex";
  const inp = document.getElementById("bg-input");
  if (inp) { inp.value = ""; inp.focus(); }
  _buscaGlobalFiltrar();
}

function _fecharBuscaGlobal() {
  const modal = document.getElementById("modal-busca-global");
  if (modal) modal.style.display = "none";
}

function _buscaGlobalFiltrar() {
  const q = (document.getElementById("bg-input")?.value || "").toLowerCase().trim();
  const res = document.getElementById("bg-results");
  if (!res) return;

  const isAtendimento = _isPaginaAtendimento();

  if (!q) {
    const msg = isAtendimento ? "Digite para buscar pacientes..." : "Digite para buscar medicamentos...";
    res.innerHTML = `<div style="text-align:center;padding:28px;color:var(--g400);font-size:.88rem">${msg}</div>`;
    return;
  }

  if (isAtendimento) {
    _buscarPacientesGlobal(q, res);
    return;
  }

  const found = medicamentos.filter((m) =>
    (m.nomeGenerico || "").toLowerCase().includes(q) ||
    (m.nomeComercial || "").toLowerCase().includes(q) ||
    (m.fabricante || "").toLowerCase().includes(q) ||
    (m.lote || "").toLowerCase().includes(q) ||
    (m.classeTerapeutica || "").toLowerCase().includes(q) ||
    (m.localizacao || "").toLowerCase().includes(q),
  ).slice(0, 10);

  if (found.length === 0) {
    res.innerHTML = `<div style="text-align:center;padding:28px;color:var(--g400);font-size:.88rem">Nenhum medicamento encontrado.</div>`;
    return;
  }

  res.innerHTML = found.map((m) => {
    const s = getStat(m);
    const vs = vStatus(m.validade);
    return `<a href="estoque.html" onclick="_fecharBuscaGlobal()"
      style="display:flex;align-items:center;gap:12px;padding:10px 16px;color:inherit;border-bottom:1px solid var(--g100)"
      onmouseover="this.style.background='var(--g50)'" onmouseout="this.style.background=''">
      <span class="material-icons" style="color:var(--g300);font-size:1.3rem">medication</span>
      <div style="flex:1;min-width:0">
        <div style="font-weight:600;font-size:.88rem;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">
          ${m.nomeGenerico} <span style="font-weight:400;color:var(--g400)">${m.nomeComercial || ""}</span>
        </div>
        <div style="font-size:.72rem;color:var(--g400)">${m.localizacao || "—"} · Lote: ${m.lote || "—"} · ${m.qtd} ${m.unidade || "un."}</div>
      </div>
      <div style="text-align:right;flex-shrink:0">
        ${sBadge(s)}
        ${vs === "vencido" ? `<br><span class="bdg b-venc" style="font-size:.6rem">VENCIDO</span>` : ""}
      </div>
    </a>`;
  }).join("");
}

async function _buscarPacientesGlobal(q, res) {
  try {
    const response = await fetch("/api/pacientes?busca=" + encodeURIComponent(q));
    const lista = await response.json();
    
    if (!lista || lista.length === 0) {
      res.innerHTML = `<div style="text-align:center;padding:28px;color:var(--g400);font-size:.88rem">Nenhum paciente encontrado.</div>`;
      return;
    }

    res.innerHTML = lista.slice(0, 10).map((p) => {
      const idade = p.dataNascimento ? _calcIdade(p.dataNascimento) : "—";
      return `<a href="at-paciente-perfil.html?id=${p.id}" onclick="_fecharBuscaGlobal()"
        style="display:flex;align-items:center;gap:12px;padding:10px 16px;color:inherit;border-bottom:1px solid var(--g100)"
        onmouseover="this.style.background='var(--g50)'" onmouseout="this.style.background=''">
        <span class="material-icons" style="color:var(--g300);font-size:1.3rem">person</span>
        <div style="flex:1;min-width:0">
          <div style="font-weight:600;font-size:.88rem;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">
            ${p.nome}
          </div>
          <div style="font-size:.72rem;color:var(--g400)">${p.cpf || "—"} · ${p.telefone || "—"} · ${idade}</div>
        </div>
      </a>`;
    }).join("");
  } catch (e) {
    res.innerHTML = `<div style="text-align:center;padding:28px;color:var(--g400);font-size:.88rem">Erro ao buscar pacientes.</div>`;
  }
}

function _calcIdade(dataNasc) {
  if (!dataNasc) return "—";
  const nasc = new Date(dataNasc);
  const hoje = new Date();
  let idade = hoje.getFullYear() - nasc.getFullYear();
  const m = hoje.getMonth() - nasc.getMonth();
  if (m < 0 || (m === 0 && hoje.getDate() < nasc.getDate())) {
    idade--;
  }
  return idade + " anos";
}

iniciarAplicativo();

// Após auth resolver: esconde elementos estáticos conforme o role
document.addEventListener("authReady", (e) => {
  const u = e.detail;

  // Botão "+ Dar Entrada" no cabeçalho do estoque
  const btnEntrada = document.querySelector('a[href="entrada.html"].btn');
  if (btnEntrada && !podeAcessar("admin","gerente","movimentacao","entrada")) {
    btnEntrada.style.display = "none";
  }

  // Re-renderiza a tabela de estoque para aplicar botões role-aware
  if (dbLoaded) {
    try { atualizarEstoque(); } catch (_) {}
  }
});
