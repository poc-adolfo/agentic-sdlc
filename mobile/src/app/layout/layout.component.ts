import { Component, inject } from '@angular/core';
import { IonTabs, IonTabBar, IonTabButton, IonIcon, IonLabel, IonBadge } from '@ionic/angular/standalone';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Layout com tabs + banner de aviso se ALLOW_ADMIN_BOOTSTRAP ativo (seção 4.2).
 */
@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [IonTabs, IonTabBar, IonTabButton, IonIcon, IonLabel, IonBadge, RouterOutlet],
  template: `
    @if (auth.flagActive()) {
      <div style="background:var(--ion-color-warning);padding:8px;text-align:center;font-weight:bold;">
        ⚠ ALLOW_ADMIN_BOOTSTRAP ativo — o próximo login sem papel vira Administrador.
      </div>
    }
    <ion-tabs>
      <ion-tab-bar slot="bottom">
        <ion-tab-button tab="users">
          <ion-icon name="people-outline"></ion-icon>
          <ion-label>Usuários</ion-label>
        </ion-tab-button>
        <ion-tab-button tab="roles">
          <ion-icon name="key-outline"></ion-icon>
          <ion-label>Papéis</ion-label>
        </ion-tab-button>
      </ion-tab-bar>
    </ion-tabs>
  `,
})
export class LayoutComponent {
  auth = inject(AuthService);
}
