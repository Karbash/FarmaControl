// ============================================================
// AT-APP.JS — Módulo de Atendimentos
// ============================================================

// API - Pacientes
function atApiGet(path) {
  return fetch(path).then(r => {
    if (r.status === 401) { window.location.replace('/login.html'); throw new Error('401'); }
    if (!r.ok) throw new Error(r.status);
    return r.json();
  });
}

function atApiPost(path, data) {
  return fetch(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  }).then(r => {
    if (r.status === 401) { window.location.replace('/login.html'); throw new Error('401'); }
    if (!r.ok) throw new Error(r.status);
    return r.json();
  });
}

function atApiPut(path, data) {
  return fetch(path, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  }).then(r => {
    if (r.status === 401) { window.location.replace('/login.html'); throw new Error('401'); }
    if (!r.ok) throw new Error(r.status);
    return r.json();
  });
}

function atApiDelete(path) {
  return fetch(path, { method: 'DELETE' }).then(r => {
    if (r.status === 401) { window.location.replace('/login.html'); throw new Error('401'); }
    if (!r.ok) throw new Error(r.status);
    return r.json();
  });
}

// Pacientes
function buscarPacientes(params = {}) {
  const q = new URLSearchParams(Object.fromEntries(Object.entries(params).filter(([, v]) => v !== undefined && v !== '')));
  return atApiGet('/api/pacientes?' + q);
}
function buscarPaciente(id) {
  return atApiGet('/api/pacientes/' + id);
}
function salvarPaciente(p) {
  return atApiPost('/api/pacientes', p);
}
function atualizarPaciente(id, p) {
  return atApiPut('/api/pacientes/' + id, p);
}
function excluirPaciente(id) {
  return atApiDelete('/api/pacientes/' + id);
}

// Atendimentos
function buscarAtendimentos(params = {}) {
  const q = new URLSearchParams(Object.fromEntries(Object.entries(params).filter(([, v]) => v !== undefined && v !== '')));
  return atApiGet('/api/atendimentos?' + q);
}
function buscarAtendimento(id) {
  return atApiGet('/api/atendimentos/' + id);
}
function criarAtendimento(a) {
  return atApiPost('/api/atendimentos', a);
}
function atualizarAtendimentoStatus(id, status, medicoNome) {
  return atApiPut('/api/atendimentos/' + id + '/status', { status, medicoNome });
}

// Triagem
function buscarTriagem(atendimentoId) {
  return atApiGet('/api/triagem?atendimentoId=' + atendimentoId);
}
function salvarTriagem(t) {
  return atApiPost('/api/triagem', t);
}
function atualizarTriagem(id, t) {
  return atApiPut('/api/triagem/' + id, t);
}

// Prontuarios
function buscarProntuarios(params = {}) {
  const q = new URLSearchParams(Object.fromEntries(Object.entries(params).filter(([, v]) => v !== undefined && v !== '')));
  return atApiGet('/api/prontuarios?' + q);
}
function buscarProntuario(id) {
  return atApiGet('/api/prontuarios/' + id);
}
function salvarProntuario(p) {
  return atApiPost('/api/prontuarios', p);
}
function atualizarProntuario(id, p) {
  return atApiPut('/api/prontuarios/' + id, p);
}

// Prescricoes
function buscarPrescricoes(params = {}) {
  const q = new URLSearchParams(Object.fromEntries(Object.entries(params).filter(([, v]) => v !== undefined && v !== '')));
  return atApiGet('/api/prescricoes?' + q);
}
function salvarPrescricao(p) {
  return atApiPost('/api/prescricoes', p);
}
function excluirPrescricao(id) {
  return atApiDelete('/api/prescricoes/' + id);
}
function dispensarPrescricao(id, responsavel) {
  return atApiPost('/api/prescricoes/' + id + '/dispensar', { responsavel });
}

// ============================================================
// DADOS
// ============================================================
let pacientes = [];
let atendimentos = [];
let prontuarios = [];
let prescricoes = [];
let equipeAtendimento = [];
let dbLoaded = false;

// Filtros
let filtroData = '';
let filtroStatus = '';
let buscaPaciente = '';

// Equipe de atendimento
async function atCarregarEquipe() {
  try {
    equipeAtendimento = await atApiGet('/api/usuarios/equipe');
  } catch (e) {
    equipeAtendimento = [];
  }
}

function atPopularSelectResponsavel(selectId) {
  const sel = document.getElementById(selectId);
  if (!sel) return;
  const atual = sel.value;
  const lblTipo = { medico: 'Médico', farmaceutico: 'Farmacêutico', enfermeira: 'Enfermeira' };
  sel.innerHTML = '<option value="">Selecione o responsável...</option>';
  equipeAtendimento.forEach(u => {
    const opt = document.createElement('option');
    opt.value = u.nome;
    opt.textContent = u.nome + ' (' + (lblTipo[u.tipo] || u.tipo) + ')';
    sel.appendChild(opt);
  });
  if (atual) sel.value = atual;
  // Pré-seleciona o usuário logado se estiver na equipe
  if (!sel.value && window.usuarioAtual) {
    sel.value = window.usuarioAtual.nome;
  }
}

function atAbrirModalNovoAtendimento() {
  const pacNomeEl = document.getElementById('at-nat-paciente-nome');
  const pacIdEl   = document.getElementById('at-nat-paciente');
  const buscaEl   = document.getElementById('at-nat-busca-paciente');
  const resEl     = document.getElementById('at-nat-resultados');
  if (pacNomeEl) pacNomeEl.textContent = 'Selecione abaixo...';
  if (pacIdEl)   pacIdEl.value = '';
  if (buscaEl)   buscaEl.value = '';
  if (resEl)     resEl.innerHTML = '';
  const dataEl = document.getElementById('at-nat-data');
  const horaEl = document.getElementById('at-nat-hora');
  const tipoEl = document.getElementById('at-nat-tipo');
  const obsEl  = document.getElementById('at-nat-obs');
  const emgEl  = document.getElementById('at-nat-emergencia');
  if (dataEl) dataEl.value = atHoje();
  if (horaEl) horaEl.value = new Date().toTimeString().slice(0, 5);
  if (tipoEl) tipoEl.value = 'consulta';
  if (obsEl)  obsEl.value = '';
  if (emgEl)  emgEl.checked = false;
  atPopularSelectResponsavel('at-nat-responsavel');
  atOpenModal('modal-novo-atendimento');
}

// ============================================================
// HELPERS
// ============================================================
const atHoje = () => new Date().toISOString().split('T')[0];

function atStatusBadge(s) {
  const map = {
    'aguardando': { cls: 'b-pending', txt: 'Aguardando' },
    'triagem': { cls: 'b-triagem', txt: 'Triagem' },
    'em_atendimento': { cls: 'b-atendimento', txt: 'Em Atendimento' },
    'encerrado': { cls: 'b-ok', txt: 'Encerrado' },
    'cancelado': { cls: 'b-err', txt: 'Cancelado' }
  };
  const m = map[s] || { cls: '', txt: s };
  return `<span class="bdg ${m.cls}">${m.txt}</span>`;
}

function atFormatData(d) {
  if (!d) return '—';
  return new Date(d + 'T12:00:00').toLocaleDateString('pt-BR');
}

function atFormatDataHora(d, h) {
  if (!d) return '—';
  const dt = atFormatData(d);
  return h ? dt + ' ' + h : dt;
}

function atOpenModal(id) {
  document.getElementById(id).classList.add('open');
}

function atCloseModal(id) {
  document.getElementById(id).classList.remove('open');
}

let _cid10BuscaTimer = null;
function atBuscarCID10(q) {
  clearTimeout(_cid10BuscaTimer);
  const resDiv = document.getElementById('at-pr-cid10-resultados');
  if (!q || q.length < 1) { resDiv.style.display = 'none'; return; }
  _cid10BuscaTimer = setTimeout(() => {
    fetch('/api/cid10?q=' + encodeURIComponent(q))
      .then(r => r.json())
      .then(lista => {
        if (!lista.length) {
          resDiv.innerHTML = '<div style="padding:10px;color:var(--g400)">Nenhum código CID-10 encontrado.</div>';
        } else {
          resDiv.innerHTML = lista.map(c => `
            <div class="at-result-item" onclick="atSelecionarCID10('${c.codigo}', '${c.nome.replace(/'/g, "\\'")}')" style="cursor:pointer;padding:8px 12px;border-bottom:1px solid var(--g100)">
              <strong>${c.codigo}</strong> - ${c.nome}
            </div>
          `).join('');
        }
        resDiv.style.display = 'block';
      })
      .catch(() => { resDiv.style.display = 'none'; });
  }, 300);
}

function atSelecionarCID10(codigo, nome) {
  document.getElementById('at-pr-cid10-codigo').value = codigo;
  document.getElementById('at-pr-cid10').value = codigo + ' - ' + nome;
  document.getElementById('at-pr-cid10-resultados').style.display = 'none';
  document.getElementById('at-pr-cid10-nome').textContent = nome;
  document.getElementById('at-pr-cid10-selecionado').style.display = 'block';
}

function atLimparCID10() {
  document.getElementById('at-pr-cid10-codigo').value = '';
  document.getElementById('at-pr-cid10').value = '';
  document.getElementById('at-pr-cid10-selecionado').style.display = 'none';
}

function atToast(msg) {
  const t = document.createElement('div');
  t.innerHTML = `<div style="position:fixed;bottom:26px;right:26px;background:#1E8449;color:#fff;padding:11px 18px;border-radius:8px;font-size:.84rem;box-shadow:0 4px 16px rgba(0,0,0,.18);z-index:999;animation:slideUp .25s">${msg}</div>`;
  document.body.appendChild(t);
  setTimeout(() => t.remove(), 3200);
}

function atIdade(dataNasc) {
  if (!dataNasc) return '—';
  const today = new Date();
  const birth = new Date(dataNasc + 'T12:00:00');
  let age = today.getFullYear() - birth.getFullYear();
  const m = today.getMonth() - birth.getMonth();
  if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) {
    age--;
  }
  return age + ' ano(s)';
}

// ============================================================
// AT-DASHBOARD
// ============================================================
function atAtualizarDashboard() {
  const hoje = atHoje();
  const hojeAt = atendimentos.filter(a => a.data === hoje);
  const aguardando = hojeAt.filter(a => a.status === 'aguardando').length;
  const triagem = hojeAt.filter(a => a.status === 'triagem').length;
  const emAt = hojeAt.filter(a => a.status === 'em_atendimento').length;
  const encerrado = hojeAt.filter(a => a.status === 'encerrado').length;
  
  document.getElementById('at-hoje-total').textContent = hojeAt.length;
  document.getElementById('at-hoje-aguardando').textContent = aguardando;
  document.getElementById('at-hoje-triagem').textContent = triagem;
  document.getElementById('at-hoje-atendimento').textContent = emAt;
  document.getElementById('at-hoje-encerrado').textContent = encerrado;

  const divFila = document.getElementById('at-fila-hoje');
  const fila = hojeAt.filter(a => a.status !== 'encerrado' && a.status !== 'cancelado')
    .sort((a, b) => {
      const ord = { aguardando: 1, triagem: 2, em_atendimento: 3 };
      return (ord[a.status] || 99) - (ord[b.status] || 99);
    });
  
  if (fila.length === 0) {
    divFila.innerHTML = '<div style="text-align:center;padding:30px;color:var(--g400)">Nenhum paciente na fila hoje.</div>';
  } else {
    divFila.innerHTML = fila.map(a => `
      <div class="at-list-item ${a.emergencia ? 'emergencia' : ''}" onclick="window.location.href='at-atendimento.html?id=${a.id}'">
        <div class="at-list-avatar ${a.emergencia ? 'emergencia' : ''}">${(a.pacienteNome || '?').charAt(0).toUpperCase()}</div>
        <div class="at-list-info">
          <div class="at-list-nome">${a.pacienteNome} ${a.emergencia ? '<span class="bdg b-emergencia">EMERGÊNCIA</span>' : ''}</div>
          <div class="at-list-meta">${a.tipo || 'Consulta'} · ${a.hora || '--:--'}</div>
        </div>
        <div class="at-list-actions">${atStatusBadge(a.status)}</div>
      </div>
    `).join('');
  }
}

function atAtualizarFila() {
  const params = {};
  if (filtroData) params.data = filtroData;
  if (filtroStatus) params.status = filtroStatus;
  
  buscarAtendimentos(params).then(lista => {
    atendimentos = lista;
    const div = document.getElementById('at-fila');
    const fila = lista.filter(a => a.status !== 'encerrado' && a.status !== 'cancelado')
      .sort((a, b) => {
        // Emergência primeiro
        if (a.emergencia !== b.emergencia) return b.emergencia - a.emergencia;
        // Depois por status
        const ord = { aguardando: 1, triagem: 2, em_atendimento: 3 };
        return (ord[a.status] || 99) - (ord[b.status] || 99);
      });
    
    const cphFila = `<div class="cph"><div class="cpt"><span class="material-icons" style="font-size:1rem">queue</span> Fila de Espera</div><span style="font-size:0.75rem;color:var(--md-on-surface-variant)">${fila.length} na fila</span></div>`;
    if (fila.length === 0) {
      div.innerHTML = '<div class="cp">' + cphFila + '<div style="padding:40px;text-align:center;color:var(--md-on-surface-variant)">Nenhum atendimento na fila.</div></div>';
    } else {
      div.innerHTML = '<div class="cp">' + cphFila + '<div>' + fila.map(a => `
        <div class="at-list-item ${a.emergencia ? 'emergencia' : ''}" onclick="window.location.href='at-atendimento.html?id=${a.id}'">
          <div class="at-list-avatar ${a.emergencia ? 'emergencia' : ''}">${(a.pacienteNome || '?').charAt(0).toUpperCase()}</div>
          <div class="at-list-info">
            <div class="at-list-nome">${a.pacienteNome} ${a.emergencia ? '<span class="bdg b-emergencia">EMERGÊNCIA</span>' : ''}</div>
            <div class="at-list-meta">${a.cpf ? 'CPF: ' + a.cpf : ''} ${a.telefone ? '· Tel: ' + a.telefone : ''}</div>
          </div>
          <div class="at-list-actions">
            ${atStatusBadge(a.status)}
            <span style="font-size:0.8rem;color:var(--md-on-surface-variant);margin-left:8px">${a.tipo || 'Consulta'} · ${a.hora || '--:--'}</span>
          </div>
        </div>
      `).join('') + '</div></div>';
    }
  });
}

async function atEncerrar(id) {
  if (!confirm('Encerrar este atendimento?')) return;
  try {
    await atualizarAtendimentoStatus(id, 'encerrado');
    atToast('Atendimento encerrado!');
    atAtualizarFila();
  } catch (e) {
    atToast('Erro ao encerrar!');
  }
}

// ============================================================
// PACIENTES
// ============================================================
function atAtualizarPacientes() {
  const params = {};
  if (buscaPaciente) params.busca = buscaPaciente;
  
  buscarPacientes(params).then(lista => {
    pacientes = lista;
    const div = document.getElementById('at-pacientes-lista');
    const cphPacientes = `<div class="cph"><div class="cpt"><span class="material-icons" style="font-size:1rem">people</span> Pacientes</div><span style="font-size:0.75rem;color:var(--md-on-surface-variant)">${lista.length} paciente(s)</span></div>`;
    if (lista.length === 0) {
      div.innerHTML = '<div class="cp">' + cphPacientes + '<div style="padding:40px;text-align:center;color:var(--md-on-surface-variant)">Nenhum paciente encontrado.</div></div>';
    } else {
      div.innerHTML = '<div class="cp">' + cphPacientes + '<div>' + lista.map(p => `
        <div class="at-list-item" onclick="window.location.href='at-paciente-perfil.html?id=${p.id}'" style="cursor:pointer">
          <div class="at-list-avatar">${(p.nome || '?').charAt(0).toUpperCase()}</div>
          <div class="at-list-info">
            <div class="at-list-nome">${p.nome} ${p.ativo ? '' : '<span class="bdg b-err" style="margin-left:8px">Inativo</span>'}</div>
            <div class="at-list-meta">${p.cpf ? 'CPF: ' + p.cpf : ''} ${p.telefone ? '· Tel: ' + p.telefone : ''} · ${atIdade(p.dataNascimento)}</div>
          </div>
          <div class="at-list-actions">
            <button class="btn btn-sm btn-p" onclick="event.stopPropagation();atNovoAtendimento(${p.id}, '${p.nome.replace(/'/g, "\\'")}')">+ Atendimento</button>
            <button class="btn btn-sm btn-s" onclick="event.stopPropagation();atEditarPaciente(${p.id})"><span class="material-icons" style="font-size:1rem;vertical-align:middle">edit</span></button>
          </div>
        </div>
      `).join('') + '</div></div>';
    }
  });
}

function atNovoAtendimento(pacienteId, pacienteNome) {
  const pacNomeEl = document.getElementById('at-nat-paciente-nome');
  const pacIdEl   = document.getElementById('at-nat-paciente');
  if (pacNomeEl) pacNomeEl.textContent = pacienteNome;
  if (pacIdEl)   pacIdEl.value = pacienteId;
  const dataEl = document.getElementById('at-nat-data');
  const horaEl = document.getElementById('at-nat-hora');
  const tipoEl = document.getElementById('at-nat-tipo');
  const obsEl  = document.getElementById('at-nat-obs');
  const emgEl  = document.getElementById('at-nat-emergencia');
  if (dataEl) dataEl.value = atHoje();
  if (horaEl) horaEl.value = new Date().toTimeString().slice(0, 5);
  if (tipoEl) tipoEl.value = 'consulta';
  if (obsEl)  obsEl.value = '';
  if (emgEl)  emgEl.checked = false;
  atPopularSelectResponsavel('at-nat-responsavel');
  atOpenModal('modal-novo-atendimento');
}

function atSalvarNovoAtendimento() {
  const pacienteId = document.getElementById('at-nat-paciente').value;
  const data = document.getElementById('at-nat-data').value;
  const hora = document.getElementById('at-nat-hora').value;
  const tipo = document.getElementById('at-nat-tipo').value;
  const emergencia = document.getElementById('at-nat-emergencia').checked;
  const responsavel = document.getElementById('at-nat-responsavel').value;
  const obs = document.getElementById('at-nat-obs').value;
  
  if (!pacienteId) return atToast('Selecione um paciente!');
  if (!data) return atToast('Informe a data!');
  
  criarAtendimento({ pacienteId, data, hora, tipo, emergencia, responsavel, obs })
    .then(r => {
      atToast(emergencia ? 'Atendimento de EMERGÊNCIA criado!' : 'Atendimento criado!');
      atCloseModal('modal-novo-atendimento');
      window.location.href = 'at-atendimento.html?id=' + r.id;
    })
    .catch(e => atToast('Erro ao criar atendimento!'));
}

function atBuscarPacienteInput() {
  const busca = document.getElementById('at-nat-busca-paciente').value;
  const div = document.getElementById('at-nat-resultados');
  if (busca.length < 2) {
    if (div) { div.innerHTML = ''; div.style.display = 'none'; }
    return;
  }

  buscarPacientes({ busca }).then(lista => {
    if (!div) return;
    if (lista.length === 0) {
      div.innerHTML = '<div style="padding:10px;color:var(--g400)">Nenhum paciente encontrado.</div>';
    } else {
      div.innerHTML = lista.map(p => `
        <div class="at-result-item" onclick="atSelecionarPaciente(${p.id}, '${p.nome.replace(/'/g, "\\'")}')">
          <strong>${p.nome}</strong>
          <span style="color:var(--g400);font-size:0.78rem">${p.cpf || ''} ${p.telefone || ''}</span>
        </div>
      `).join('');
    }
    div.style.display = 'block';
  });
}

function atSelecionarPaciente(id, nome) {
  document.getElementById('at-nat-paciente').value = id;
  document.getElementById('at-nat-paciente-nome').textContent = nome;
  const div = document.getElementById('at-nat-resultados');
  if (div) { div.innerHTML = ''; div.style.display = 'none'; }
}

function atAbrirNovoPaciente() {
  document.getElementById('at-np-nome').value = '';
  document.getElementById('at-np-cpf').value = '';
  document.getElementById('at-np-nascimento').value = '';
  document.getElementById('at-np-sexo').value = '';
  document.getElementById('at-np-telefone').value = '';
  document.getElementById('at-np-endereco').value = '';
  document.getElementById('at-np-obs').value = '';
  atCloseModal('modal-novo-atendimento');
  atOpenModal('modal-novo-paciente');
}

function atSalvarNovoPaciente() {
  const nome = document.getElementById('at-np-nome').value.trim();
  if (!nome) return atToast('Nome é obrigatório!');
  
  const dados = {
    nome,
    cpf: document.getElementById('at-np-cpf').value.trim(),
    dataNascimento: document.getElementById('at-np-nascimento').value,
    sexo: document.getElementById('at-np-sexo').value,
    telefone: document.getElementById('at-np-telefone').value.trim(),
    endereco: document.getElementById('at-np-endereco').value.trim(),
    obs: document.getElementById('at-np-obs').value.trim()
  };
  
  salvarPaciente(dados)
    .then(r => {
      atToast('Paciente salvo!');
      atCloseModal('modal-novo-paciente');
      atSelecionarPaciente(r.id, nome);
    })
    .catch(e => atToast('Erro ao salvar!'));
}

function atEditarPaciente(id) {
  buscarPaciente(id).then(p => {
    document.getElementById('at-ep-id').value = p.id;
    document.getElementById('at-ep-nome').value = p.nome;
    document.getElementById('at-ep-cpf').value = p.cpf || '';
    document.getElementById('at-ep-nascimento').value = p.dataNascimento || '';
    document.getElementById('at-ep-sexo').value = p.sexo || '';
    document.getElementById('at-ep-telefone').value = p.telefone || '';
    document.getElementById('at-ep-endereco').value = p.endereco || '';
    document.getElementById('at-ep-obs').value = p.obs || '';
    atOpenModal('modal-editar-paciente');
  });
}

function atSalvarEditarPaciente() {
  const id = document.getElementById('at-ep-id').value;
  const nome = document.getElementById('at-ep-nome').value.trim();
  if (!nome) return atToast('Nome é obrigatório!');
  
  const dados = {
    nome,
    cpf: document.getElementById('at-ep-cpf').value.trim(),
    dataNascimento: document.getElementById('at-ep-nascimento').value,
    sexo: document.getElementById('at-ep-sexo').value,
    telefone: document.getElementById('at-ep-telefone').value.trim(),
    endereco: document.getElementById('at-ep-endereco').value.trim(),
    obs: document.getElementById('at-ep-obs').value.trim()
  };
  
  atualizarPaciente(id, dados)
    .then(() => {
      atToast('Paciente atualizado!');
      atCloseModal('modal-editar-paciente');
      atAtualizarPacientes();
    })
    .catch(e => atToast('Erro ao atualizar!'));
}

// ============================================================
// ATENDIMENTO (Triagem + Prontuario)
// ============================================================
let atdAtual = null;
let triagemAtual = null;
let prontuarioAtual = null;

async function atCarregarAtendimento(id) {
  try {
    await atCarregarEquipe();
    atdAtual = await buscarAtendimento(id);
    
    document.getElementById('at-at-paciente-nome').textContent = atdAtual.pacienteNome;
    document.getElementById('at-at-paciente-info').textContent = 
      `${atdAtual.cpf || ''} · ${atIdade(atdAtual.dataNascimento)} · ${atdAtual.sexo || ''}`;
    document.getElementById('at-at-data').textContent = atFormatDataHora(atdAtual.data, atdAtual.hora);
    document.getElementById('at-at-tipo').textContent = atdAtual.tipo || 'Consulta';
    document.getElementById('at-at-status').innerHTML = atStatusBadge(atdAtual.status);
    
    const params = new URLSearchParams();
    params.append('atendimentoId', id);
    const triagens = await buscarTriagem(id);
    if (triagens.length > 0) {
      triagemAtual = triagens[0];
      atPreencherTriagem(triagemAtual);
    }
    
    const prots = await buscarProntuarios({ atendimentoId: id });
    if (prots.length > 0) {
      prontuarioAtual = prots[0];
    }
    
    const prescs = await buscarPrescricoes({ prontuarioId: prontuarioAtual?.id });
    prescricoes = prescs;
    atAtualizarPrescricoes();
    
    atPopularSelectResponsavel('at-tr-responsavel');
    atPopularSelectResponsavel('at-pr-medico');
    
    if (prots.length > 0) {
      atPreencherProntuario(prontuarioAtual);
    }
    
    atAtualizarBotoes();
  } catch (e) {
    console.error(e);
    atToast('Erro ao carregar atendimento!');
  }
}

function atPreencherTriagem(t) {
  document.getElementById('at-tr-pressao').value = t.pressaoArterial || '';
  document.getElementById('at-tr-temp').value = t.temperatura || '';
  document.getElementById('at-tr-peso').value = t.peso || '';
  document.getElementById('at-tr-altura').value = t.altura || '';
  document.getElementById('at-tr-fc').value = t.frequenciaCardiaca || '';
  document.getElementById('at-tr-saturacao').value = t.saturacao || '';
  document.getElementById('at-tr-queixa').value = t.queixaPrincipal || '';
  document.getElementById('at-tr-obs').value = t.obs || '';
  if (t.responsavel) {
    document.getElementById('at-tr-responsavel').value = t.responsavel;
  }
}

function atPreencherProntuario(p) {
  document.getElementById('at-pr-anamnese').value = p.anamnese || '';
  document.getElementById('at-pr-exame').value = p.exameFisico || '';
  document.getElementById('at-pr-hipotese').value = p.hipoteseDiagnostica || '';
  document.getElementById('at-pr-conduta').value = p.conduta || '';
  document.getElementById('at-pr-obs').value = p.obs || '';
  const responsavelTriagem = triagemAtual?.responsavel || '';
  document.getElementById('at-pr-medico').value = p.medicoNome || responsavelTriagem || window.usuarioAtual?.nome || '';
  if (p.cid10) {
    document.getElementById('at-pr-cid10-codigo').value = p.cid10;
    document.getElementById('at-pr-cid10').value = p.cid10;
  }
}

function atAtualizarBotoes() {
  const encerrado = atdAtual?.status === 'encerrado';
  const etapaTriagem = document.getElementById('etapa-triagem');
  const etapaProntuario = document.getElementById('etapa-prontuario');
  const btnIniciar = document.getElementById('btn-iniciar');
  const btnEncerrar = document.querySelector('button[onclick="atEncerrarAtendimento()"]');
  const btnSalvarTriagem = document.querySelector('#etapa-triagem .btn');
  const btnSalvarProntuario = document.querySelector('#etapa-prontuario .btn');
  
  if (etapaTriagem) {
    etapaTriagem.style.display = atdAtual?.status === 'triagem' || atdAtual?.status === 'em_atendimento' || atdAtual?.status === 'encerrado' || atdAtual?.status === 'aguardando' ? 'block' : 'none';
  }
  if (etapaProntuario) {
    etapaProntuario.style.display = atdAtual?.status === 'em_atendimento' || atdAtual?.status === 'encerrado' ? 'block' : 'none';
  }
  if (btnIniciar) {
    btnIniciar.style.display = atdAtual?.status === 'triagem' ? 'inline-block' : 'none';
  }
  if (btnEncerrar) {
    btnEncerrar.style.display = atdAtual?.status === 'em_atendimento' || atdAtual?.status === 'triagem' ? 'inline-block' : 'none';
  }
  
  // Bloquear campos se já foram salvos
  atBloquearCampos();
  
  // Ocultar botão de nova prescrição se encerrado
  const btnNovaPrescricao = document.getElementById('btn-nova-prescricao');
  if (btnNovaPrescricao) {
    btnNovaPrescricao.style.display = encerrado ? 'none' : 'inline-block';
  }
}

function atBloquearCampos() {
  const temTriagem = triagemAtual !== null;
  const temProntuario = prontuarioAtual !== null;
  const encerrado = atdAtual?.status === 'encerrado';
  
  // Campos da triagem
  const camposTriagem = ['at-tr-pressao', 'at-tr-temp', 'at-tr-peso', 'at-tr-altura', 'at-tr-fc', 'at-tr-saturacao', 'at-tr-queixa', 'at-tr-responsavel', 'at-tr-obs'];
  camposTriagem.forEach(id => {
    const el = document.getElementById(id);
    if (el) el.disabled = temTriagem || encerrado;
  });
  
  // Campos do prontuário
  const camposProntuario = ['at-pr-medico', 'at-pr-anamnese', 'at-pr-exame', 'at-pr-hipotese', 'at-pr-cid10', 'at-pr-conduta', 'at-pr-obs'];
  camposProntuario.forEach(id => {
    const el = document.getElementById(id);
    if (el) el.disabled = temProntuario || encerrado;
  });
  
  // Botões
  const btnSalvarTriagem = document.querySelector('#etapa-triagem .btn');
  const btnSalvarProntuario = document.querySelector('#etapa-prontuario .btn');
  if (btnSalvarTriagem) btnSalvarTriagem.style.display = (temTriagem || encerrado) ? 'none' : 'inline-block';
  if (btnSalvarProntuario) btnSalvarProntuario.style.display = (temProntuario || encerrado) ? 'none' : 'inline-block';
}

function atSalvarTriagem() {
  if (!atdAtual) return;
  
  const dados = {
    atendimentoId: atdAtual.id,
    pressaoArterial: document.getElementById('at-tr-pressao').value.trim(),
    temperatura: document.getElementById('at-tr-temp').value.trim(),
    peso: document.getElementById('at-tr-peso').value.trim(),
    altura: document.getElementById('at-tr-altura').value.trim(),
    frequenciaCardiaca: document.getElementById('at-tr-fc').value.trim(),
    saturacao: document.getElementById('at-tr-saturacao').value.trim(),
    queixaPrincipal: document.getElementById('at-tr-queixa').value.trim(),
    responsavel: document.getElementById('at-tr-responsavel').value.trim(),
    obs: document.getElementById('at-tr-obs').value.trim()
  };
  
  const nomeResp = window.usuarioAtual?.nome || '';
  
  if (triagemAtual) {
    atualizarTriagem(triagemAtual.id, dados)
      .then(() => {
        atToast('Triagem atualizada!');
        if (atdAtual.status === 'aguardando') {
          atualizarAtendimentoStatus(atdAtual.id, 'triagem');
        }
        atAtualizarBotoes();
      })
      .catch(e => atToast('Erro!'));
  } else {
    salvarTriagem(dados)
      .then(() => {
        atToast('Triagem salva!');
        if (atdAtual.status === 'aguardando') {
          atualizarAtendimentoStatus(atdAtual.id, 'triagem').then(() => {
            window.location.reload();
          });
        }
        atAtualizarBotoes();
      })
      .catch(e => atToast('Erro!'));
  }
}

function atIniciarAtendimento() {
  if (!atdAtual) return;
  atualizarAtendimentoStatus(atdAtual.id, 'em_atendimento', window.usuarioAtual?.nome)
    .then(() => {
      atToast('Atendimento iniciado!');
      window.location.reload();
    })
    .catch(e => atToast('Erro!'));
}

function atSalvarProntuario() {
  if (!atdAtual) return;
  
  const dados = {
    atendimentoId: atdAtual.id,
    pacienteId: atdAtual.pacienteId,
    medicoNome: document.getElementById('at-pr-medico').value.trim(),
    anamnese: document.getElementById('at-pr-anamnese').value.trim(),
    exameFisico: document.getElementById('at-pr-exame').value.trim(),
    hipoteseDiagnostica: document.getElementById('at-pr-hipotese').value.trim(),
    cid10: document.getElementById('at-pr-cid10-codigo').value.trim(),
    conduta: document.getElementById('at-pr-conduta').value.trim(),
    obs: document.getElementById('at-pr-obs').value.trim()
  };
  
  if (prontuarioAtual) {
    atualizarProntuario(prontuarioAtual.id, dados)
      .then(() => {
        atToast('Prontuário atualizado!');
        atAtualizarBotoes();
      })
      .catch(e => atToast('Erro!'));
  } else {
    salvarProntuario(dados)
      .then(r => {
        prontuarioAtual = { id: r.id };
        atToast('Prontuário salvo!');
        atAtualizarBotoes();
        buscarPrescricoes({ prontuarioId: r.id }).then(ps => {
          prescricoes = ps;
          atAtualizarPrescricoes();
        });
      })
      .catch(e => atToast('Erro!'));
  }
}

function atAtualizarPrescricoes() {
  const div = document.getElementById('at-prescricoes-lista');
  if (!prontuarioAtual || prescricoes.length === 0) {
    div.innerHTML = '<div style="text-align:center;padding:20px;color:var(--md-on-surface-variant)">Nenhuma prescrição.</div>';
    return;
  }
  
  div.innerHTML = '<div class="cp" style="padding:0"><div style="padding:0">' + prescricoes.map(p => `
    <div class="at-presc-row">
      <div class="at-presc-nome">${p.nomeMed || 'Medicamento não especificado'}</div>
      <div class="at-presc-dosagem">${p.dosagem || ''} · ${p.posologia || ''}</div>
      <div class="at-presc-qtd">Qtd: ${p.quantidade}</div>
      <div style="min-width:80px;text-align:right">
        ${p.dispensado ? '<span class="bdg b-ok">Dispensado</span>' : '<span class="bdg b-pending">Pendente</span>'}
      </div>
      <div style="display:flex;gap:4px;margin-left:12px">
        ${!p.dispensado ? `<button class="btn btn-sm btn-ds" onclick="atExcluirPresc(${p.id})">×</button>` : ''}
      </div>
    </div>
  `).join('') + '</div></div>';
}

function atAbrirPrescricao() {
  if (!prontuarioAtual) {
    return atToast('Salve o prontuário primeiro!');
  }
  if (atdAtual?.status === 'encerrado') {
    return atToast('Atendimento encerrado! Não é possível adicionar prescrições.');
  }
  document.getElementById('at-np-medicamento').value = '';
  document.getElementById('at-np-dosagem').value = '';
  document.getElementById('at-np-posologia').value = '';
  document.getElementById('at-np-qtd').value = '1';
  document.getElementById('at-np-obs').value = '';
  atOpenModal('modal-nova-prescricao');
}

async function atSalvarPrescricao() {
  if (!prontuarioAtual) return atToast('Salve o prontuário primeiro!');
  
  const dados = {
    prontuarioId: prontuarioAtual.id,
    pacienteId: atdAtual.pacienteId,
    nomeMed: document.getElementById('at-np-medicamento').value.trim(),
    dosagem: document.getElementById('at-np-dosagem').value.trim(),
    posologia: document.getElementById('at-np-posologia').value.trim(),
    quantidade: parseInt(document.getElementById('at-np-qtd').value) || 1,
    obs: document.getElementById('at-np-obs').value.trim()
  };
  
  if (!dados.nomeMed) return atToast('Informe o medicamento!');
  
  try {
    await salvarPrescricao(dados);
    atToast('Prescrição salva!');
    atCloseModal('modal-nova-prescricao');
    const ps = await buscarPrescricoes({ prontuarioId: prontuarioAtual.id });
    prescricoes = ps;
    atAtualizarPrescricoes();
  } catch (e) {
    atToast('Erro ao salvar!');
  }
}

async function atDispensar(id) {
  try {
    await dispensarPrescricao(id, window.usuarioAtual?.nome);
    atToast('Dispensado!');
    const ps = await buscarPrescricoes({ prontuarioId: prontuarioAtual.id });
    prescricoes = ps;
    atAtualizarPrescricoes();
  } catch (e) {
    atToast('Erro: ' + (e.message || 'não foi possível dispensar'));
  }
}

async function atExcluirPresc(id) {
  if (!confirm('Excluir esta prescrição?')) return;
  try {
    await excluirPrescricao(id);
    atToast('Excluído!');
    const ps = await buscarPrescricoes({ prontuarioId: prontuarioAtual.id });
    prescricoes = ps;
    atAtualizarPrescricoes();
  } catch (e) {
    atToast('Erro!');
  }
}

function atEncerrarAtendimento() {
  if (!atdAtual) return;
  if (!confirm('Encerrar este atendimento?')) return;
  atualizarAtendimentoStatus(atdAtual.id, 'encerrado')
    .then(() => {
      atToast('Atendimento encerrado!');
      window.location.href = 'at-dashboard.html';
    })
    .catch(e => atToast('Erro!'));
}

// ============================================================
// RELATÓRIOS
// ============================================================
function atAtualizarRelatorios() {
  const div = document.getElementById('at-rel-content');
  
  const totalPacientes = pacientes.length;
  const totalAtendimentos = atendimentos.length;
  const encerrados = atendimentos.filter(a => a.status === 'encerrado').length;
  
  const porStatus = {};
  atendimentos.forEach(a => {
    porStatus[a.status] = (porStatus[a.status] || 0) + 1;
  });
  
  let h = `<div class="ph"><div><div class="pt">Relatórios - Atendimentos</div><div class="ps">Estatísticas do módulo</div></div></div>`;
  
  h += `<div class="cg" style="margin-bottom:20px">
    <div class="sc blue"><div class="sc-acc"></div><div class="sc-lbl">Pacientes</div><div class="sc-val">${totalPacientes}</div></div>
    <div class="sc"><div class="sc-acc"></div><div class="sc-lbl">Atendimentos</div><div class="sc-val">${totalAtendimentos}</div></div>
    <div class="sc green"><div class="sc-acc"></div><div class="sc-lbl">Encerrados</div><div class="sc-val">${encerrados}</div></div>
    <div class="sc warn"><div class="sc-acc"></div><div class="sc-lbl">Em Aberto</div><div class="sc-val">${totalAtendimentos - encerrados}</div></div>
  </div>`;
  
  h += `<div class="cp" style="margin-bottom:16px">
    <div class="cph"><div class="cpt">Atendimentos por Status</div></div>
    <div style="padding:16px">`;
  
  const statusMap = {
    'aguardando': 'Aguardando',
    'triagem': 'Triagem',
    'em_atendimento': 'Em Atendimento',
    'encerrado': 'Encerrado',
    'cancelado': 'Cancelado'
  };
  
  for (const [status, qtd] of Object.entries(porStatus)) {
    h += `<div style="display:flex;justify-content:space-between;padding:8px 0;border-bottom:1px solid var(--g100)">
      <span>${statusMap[status] || status}</span>
      <span class="bdg b-ok">${qtd}</span>
    </div>`;
  }
  
  h += `</div></div>`;
  
  h += `<div class="cp">
    <div class="cph"><div class="cpt">Histórico de Atendimentos</div></div>
    <table><thead><tr><th>Paciente</th><th>Data</th><th>Tipo</th><th>Status</th><th>Médico</th></tr></thead>
    <tbody>${atendimentos.slice(0, 50).map(a => `
      <tr>
        <td data-label="Paciente">${a.pacienteNome || '—'}</td>
        <td data-label="Data">${atFormatData(a.data)}</td>
        <td data-label="Tipo">${a.tipo || 'Consulta'}</td>
        <td data-label="Status">${atStatusBadge(a.status)}</td>
        <td data-label="Médico">${a.medicoNome || '—'}</td>
      </tr>
    `).join('')}</tbody></table>
  </div>`;
  
  div.innerHTML = h;
}

async function atIniciarModulo() {
  try {
    [[pacientes, atendimentos]] = await Promise.all([
      Promise.all([buscarPacientes(), buscarAtendimentos()]),
      atCarregarEquipe()
    ]);
    dbLoaded = true;
  } catch (e) {
    console.error(e);
  }
}

// Inicializa componentes de UI ao carregar qualquer página at-*
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    _atInitBuscaGlobal();
    _atInitMobileBar();
  });
} else {
  _atInitBuscaGlobal();
  _atInitMobileBar();
}

// ============================================================
// BARRA MOBILE (injeta .mob-bar e overlay em todas as páginas)
// ============================================================
function _atInitMobileBar() {
  if (document.querySelector('.mob-bar')) return;

  const pageTitles = {
    'at-dashboard.html': 'Dashboard',
    'at-fila.html': 'Fila de Espera',
    'at-pacientes.html': 'Pacientes',
    'at-atendimento.html': 'Atendimento',
    'at-paciente-perfil.html': 'Perfil do Paciente',
    'at-relatorios.html': 'Relatórios'
  };
  const currentPage = window.location.pathname.split('/').pop();
  const title = pageTitles[currentPage] || 'Ambulatório Maanaim';

  // Overlay
  const ovr = document.createElement('div');
  ovr.className = 'sb-ovr';
  ovr.onclick = _atToggleSb;
  document.body.appendChild(ovr);

  // Barra mobile
  const bar = document.createElement('div');
  bar.className = 'mob-bar';
  bar.innerHTML = `
    <button class="sb-tog" aria-label="Menu">
      <span class="material-icons">menu</span>
    </button>
    <div class="mob-ttl">${title}</div>
    <button class="mob-search-btn" aria-label="Buscar paciente">
      <span class="material-icons">person_search</span>
    </button>`;
  
  bar.querySelector('.sb-tog').onclick = _atToggleSb;
  bar.querySelector('.mob-search-btn').onclick = _atAbrirBuscaGlobal;

  const app = document.getElementById('app');
  if (app) app.before(bar);
  else document.body.prepend(bar);
}

function _atToggleSb() {
  document.querySelector('.sb')?.classList.toggle('open');
  document.querySelector('.sb-ovr')?.classList.toggle('open');
}

// ============================================================
// BUSCA GLOBAL DE PACIENTES (Ctrl+K)
// ============================================================
function _atInitBuscaGlobal() {
  if (document.getElementById('at-modal-busca-global')) return;

  const modal = document.createElement('div');
  modal.id = 'at-modal-busca-global';
  modal.style.cssText = 'display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,.45);z-index:9000;align-items:flex-start;justify-content:center;padding-top:72px';
  modal.innerHTML = `
    <div style="background:#fff;border-radius:14px;width:calc(100% - 32px);max-width:560px;box-shadow:0 8px 40px rgba(0,0,0,.2);overflow:hidden">
      <div style="display:flex;align-items:center;gap:10px;padding:14px 16px;border-bottom:1px solid var(--g100)">
        <span class="material-icons" style="color:var(--g400)">person</span>
        <input id="at-bg-input" type="text" autocomplete="off" placeholder="Buscar paciente..."
          style="flex:1;border:none;outline:none;font-size:1rem;background:transparent"
          oninput="_atBuscaGlobalFiltrar()" />
        <kbd style="font-size:.68rem;color:var(--g400);background:var(--g50);border:1px solid var(--g200);border-radius:4px;padding:2px 6px">Esc</kbd>
      </div>
      <div id="at-bg-results" style="max-height:380px;overflow-y:auto;padding:6px 0"></div>
    </div>`;
  modal.addEventListener('click', (e) => { if (e.target === modal) _atFecharBuscaGlobal(); });
  document.body.appendChild(modal);

  document.addEventListener('keydown', (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
      e.preventDefault();
      _atAbrirBuscaGlobal();
    }
    if (e.key === 'Escape') _atFecharBuscaGlobal();
  });
}

function _atAbrirBuscaGlobal() {
  console.log('Abrindo busca global...');
  const modal = document.getElementById('at-modal-busca-global');
  if (!modal) {
    console.log('Modal não encontrado, inicializando...');
    _atInitBuscaGlobal();
  }
  const m = document.getElementById('at-modal-busca-global');
  if (!m) return;
  m.style.display = 'flex';
  const inp = document.getElementById('at-bg-input');
  if (inp) { inp.value = ''; inp.focus(); }
  _atBuscaGlobalFiltrar();
}

function _atFecharBuscaGlobal() {
  const modal = document.getElementById('at-modal-busca-global');
  if (modal) modal.style.display = 'none';
}

let _atBgTimer = null;
function _atBuscaGlobalFiltrar() {
  const q = (document.getElementById('at-bg-input')?.value || '').trim();
  const res = document.getElementById('at-bg-results');
  if (!res) return;

  if (!q) {
    res.innerHTML = '<div style="text-align:center;padding:28px;color:var(--g400);font-size:.88rem">Digite para buscar pacientes...</div>';
    return;
  }

  clearTimeout(_atBgTimer);
  _atBgTimer = setTimeout(() => {
    fetch('/api/pacientes?busca=' + encodeURIComponent(q))
      .then(r => r.json())
      .then(lista => {
        if (!lista || lista.length === 0) {
          res.innerHTML = '<div style="text-align:center;padding:28px;color:var(--g400);font-size:.88rem">Nenhum paciente encontrado.</div>';
          return;
        }
        res.innerHTML = lista.slice(0, 10).map(p => {
          const idade = p.dataNascimento ? atIdade(p.dataNascimento) : '—';
          return `<a href="at-paciente-perfil.html?id=${p.id}" onclick="_atFecharBuscaGlobal()"
            style="display:flex;align-items:center;gap:12px;padding:10px 16px;color:inherit;border-bottom:1px solid var(--g100)"
            onmouseover="this.style.background='var(--g50)'" onmouseout="this.style.background=''">
            <span class="material-icons" style="color:var(--g300);font-size:1.3rem">person</span>
            <div style="flex:1;min-width:0">
              <div style="font-weight:600;font-size:.88rem;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">${p.nome}</div>
              <div style="font-size:.72rem;color:var(--g400)">${p.cpf || '—'} · ${p.telefone || '—'} · ${idade}</div>
            </div>
          </a>`;
        }).join('');
      })
      .catch(() => {
        res.innerHTML = '<div style="text-align:center;padding:28px;color:var(--g400);font-size:.88rem">Erro ao buscar pacientes.</div>';
      });
  }, 250);
}
