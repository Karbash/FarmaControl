import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-access-denied-page',
  imports: [PageHeaderComponent, RouterLink],
  template: `
    <app-page-header
      title="Acesso negado"
      description="Seu perfil nao possui permissao para esta area." />

    <section class="work-surface empty-state">
      <strong>Acesso indisponivel para o perfil atual</strong>
      <span>Solicite liberacao de modulo ao administrador caso precise usar esta area.</span>
      <a routerLink="/dashboard" class="ghost-button">Voltar ao painel</a>
    </section>
  `
})
export class AccessDeniedPageComponent {}
