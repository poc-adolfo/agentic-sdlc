import { Component, inject, signal, OnInit } from '@angular/core';
import { IonContent, IonList, IonItem, IonLabel, IonButton, IonChip, IonText } from '@ionic/angular/standalone';
import { ApiService, RoleListItem } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-roles-list',
  standalone: true,
  imports: [IonContent, IonList, IonItem, IonLabel, IonButton, IonChip, IonText],
  template: `
    <ion-content class="ion-padding">
      <h2>Papéis</h2>
      @if (auth.hasPermission('roles:manage')) {
        <ion-button (click)="newName.set(''); newDesc.set(''); showNew.set(true)">Novo papel</ion-button>
        @if (showNew()) {
          <ion-item><ion-input label="Nome" [(ngModel)]="newName" /></ion-item>
          <ion-item><ion-input label="Descrição" [(ngModel)]="newDesc" /></ion-item>
          <ion-button (click)="create()">Criar</ion-button>
        }
      }
      <ion-list>
        @for (r of roles(); track r.id) {
          <ion-item [routerLink]="['/roles', r.id]">
            <ion-label>
              <h3>{{ r.name }}</h3>
              <p>{{ r.description }}</p>
              @if (r.isSystem) { <ion-chip color="warning">is_system</ion-chip> }
            </ion-label>
          </ion-item>
        }
      </ion-list>
    </ion-content>
  `,
})
export class RolesListPage implements OnInit {
  private api = inject(ApiService);
  auth = inject(AuthService);

  roles = signal<RoleListItem[]>([]);
  showNew = signal(false);
  newName = signal('');
  newDesc = signal('');

  async ngOnInit() { this.roles.set(await this.api.listRoles()); }

  async create() {
    await this.api.createRole({ name: this.newName(), description: this.newDesc() });
    this.showNew.set(false);
    this.roles.set(await this.api.listRoles());
  }
}
