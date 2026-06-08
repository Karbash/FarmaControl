import { Injectable } from '@angular/core';

const TOKEN_KEY = 'farmacontrol.accessToken';

@Injectable({ providedIn: 'root' })
export class AuthTokenStore {
  get(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  set(token: string | null): void {
    if (token) {
      localStorage.setItem(TOKEN_KEY, token);
      return;
    }

    this.clear();
  }

  clear(): void {
    localStorage.removeItem(TOKEN_KEY);
  }
}
