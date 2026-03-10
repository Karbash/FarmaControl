// ============================================================
// AUTH.JS — Guarda de autenticação + RBAC frontend
// ============================================================

const _ROLE_LABELS = {
  admin:        'Administrador',
  gerente:      'Gerente',
  medico:       'Médico',
  enfermeira:   'Enfermeira',
  farmaceutico: 'Farmacêutico',
  movimentacao: 'Movimentação',
  entrada:      'Entrada',
  saida:        'Saída',
  visualizacao: 'Visualização',
};

// Páginas com acesso restrito (pathname → roles permitidas)
const _PAGE_ACCESS = {
  '/entrada.html':       ['admin', 'gerente', 'movimentacao', 'entrada', 'farmaceutico'],
  '/saida.html':         ['admin', 'gerente', 'movimentacao', 'saida', 'farmaceutico'],
  '/transferencia.html': ['admin', 'gerente', 'movimentacao', 'saida', 'farmaceutico'],
  '/cadastros.html':     ['admin', 'gerente'],
  '/auditoria.html':     ['admin', 'gerente'],
  '/dashboard.html':     ['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao'],
  '/atendimento.html':         ['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico'],
  '/at-dashboard.html':        ['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico'],
  '/at-pacientes.html':        ['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico'],
  '/at-fila.html':             ['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico'],
  '/at-atendimento.html':      ['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico'],
  '/at-paciente-perfil.html':  ['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico'],
  '/at-relatorios.html':       ['admin', 'gerente', 'medico', 'farmaceutico'],
};

// Links de nav ocultos por role (href contém → roles que NÃO veem o link)
const _NAV_HIDDEN_FROM = {
  'entrada.html':       ['visualizacao', 'saida'],
  'saida.html':         ['visualizacao', 'entrada'],
  'transferencia.html': ['visualizacao', 'entrada'],
  'cadastros.html':     ['movimentacao', 'entrada', 'saida', 'visualizacao'],
  'auditoria.html':     ['movimentacao', 'entrada', 'saida', 'visualizacao'],
};

(async function verificarAuth() {
  try {
    const r = await fetch('/api/auth/me');
    if (r.status === 401) {
      window.location.replace('/login.html');
      return;
    }
    window.usuarioAtual = await r.json();
    
    // Carrega módulos de acesso do usuário
    await _carregarModulos();
    _aplicarRBAC(window.usuarioAtual);
    _preencherSidebar(window.usuarioAtual);
  } catch (e) {
    window.location.replace('/login.html');
  }
})();

async function _carregarModulos() {
  try {
    const r = await fetch('/api/auth/me');
    if (r.ok) {
      const u = await r.json();
      window.usuarioAtual.modulos = u.modulos || [];
    }
  } catch (e) {
    window.usuarioAtual.modulos = [];
  }
}

function _aplicarRBAC(u) {
  const page = window.location.pathname;

  // Redireciona se não tiver acesso à página
  const allowedRoles = _PAGE_ACCESS[page];
  if (allowedRoles && !allowedRoles.includes(u.tipo)) {
    window.location.replace('/dashboard.html');
    return;
  }

  // Oculta links de nav que o role não pode acessar
  document.querySelectorAll('.nav-item').forEach((link) => {
    const href = (link.getAttribute('href') || '').replace(/^.*\//, '');
    const hiddenFrom = _NAV_HIDDEN_FROM[href];
    if (hiddenFrom && hiddenFrom.includes(u.tipo)) {
      link.style.display = 'none';
    }
  });

  // Oculta menu de atendimentos baseado no role
  const navAtMenu = document.getElementById('nav-atendimentos');
  if (navAtMenu) {
    navAtMenu.style.display = ['admin','gerente','medico','enfermeira','farmaceutico'].includes(u.tipo) ? '' : 'none';
  }
  
  // Oculta aba de Usuários para não-admin
  const usuariosTab = document.querySelector('button[data-role-required="admin"]');
  if (usuariosTab && u.tipo !== 'admin') {
    usuariosTab.style.display = 'none';
  }

  // Sinaliza para app.js que o auth foi resolvido
  document.dispatchEvent(new CustomEvent('authReady', { detail: u }));
}

function _preencherSidebar(u) {
  const nome = document.getElementById('sb-user-nome');
  const tipo = document.getElementById('sb-user-tipo');
  if (nome) nome.textContent = u.nome;
  if (tipo) tipo.textContent = _ROLE_LABELS[u.tipo] || u.tipo;
}

function logout() {
  fetch('/api/auth/logout', { method: 'POST' }).finally(() => {
    window.location.replace('/login.html');
  });
}

/**
 * Retorna true se o usuário atual possuir um dos roles listados.
 * Uso:  if (podeAcessar('admin','gerente')) { ... }
 */
function podeAcessar(...roles) {
  return roles.includes(window.usuarioAtual?.tipo);
}

/**
 * Retorna true se o usuário tem acesso ao módulo especificado.
 * Uso:  if (temAcessoModulo('medicamentos')) { ... }
 */
function temAcessoModulo(modulo) {
  if (!window.usuarioAtual) return false;
  const u = window.usuarioAtual;
  
  const rolesMedicamentos = ['admin', 'gerente', 'movimentacao', 'entrada', 'saida', 'visualizacao', 'farmaceutico'];
  const rolesAtendimentos = ['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico'];
  
  if (modulo === 'medicamentos') {
    return rolesMedicamentos.includes(u.tipo) || (u.modulos && u.modulos.includes('medicamentos'));
  }
  if (modulo === 'atendimentos') {
    return rolesAtendimentos.includes(u.tipo) || (u.modulos && u.modulos.includes('atendimentos'));
  }
  return false;
}

// ============================================================
// MENU MOBILE — barra superior + drawer lateral
// ============================================================
document.addEventListener('DOMContentLoaded', function _initMobMenu() {
  const sb = document.querySelector('.sb');
  if (!sb) return;

  // Backdrop
  const ovr = document.createElement('div');
  ovr.className = 'sb-ovr';
  ovr.addEventListener('click', _closeSb);
  document.body.appendChild(ovr);

  // Barra superior mobile
  const titulo = document.title
    .replace(' - FarmaControl', '')
    .replace('FarmaControl - ', '');
  const bar = document.createElement('div');
  bar.className = 'mob-bar';
  bar.innerHTML =
    '<button class="sb-tog" onclick="_togSb()" title="Menu">' +
    '<span class="material-icons" style="font-size:1.4rem">menu</span>' +
    '</button>' +
    '<span class="mob-ttl">' + titulo + '</span>' +
    '<button class="sb-tog" onclick="_abrirBuscaGlobal()" title="Busca global (Ctrl+K)" style="margin-left:auto">' +
    '<span class="material-icons" style="font-size:1.4rem">search</span>' +
    '</button>';
  document.body.insertBefore(bar, document.body.firstChild);

  // Fechar ao clicar em qualquer item do nav
  document.querySelectorAll('.nav-item').forEach(function (el) {
    el.addEventListener('click', _closeSb);
  });
});

function _togSb() {
  document.querySelector('.sb').classList.toggle('open');
  document.querySelector('.sb-ovr').classList.toggle('open');
}

function _closeSb() {
  document.querySelector('.sb')?.classList.remove('open');
  document.querySelector('.sb-ovr')?.classList.remove('open');
}
