import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonContent, IonList, IonItem, IonLabel, IonSearchbar, IonSelect, IonSelectOption, IonButton, IonChip, IonText } from '@ionic/angular/standalone';
import { ApiService, UserListItem, PagedResult } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [FormsModule, IonContent, IonList, IonItem, IonLabel, IonSearchbar, IonSelect, IonSelectOption, IonButton, IonChip, IonText],
  template: `
    <ion-content class="ion-padding">
      <ion-searchbar placeholder="Buscar por nome ou e-mail" [(ngModel)]="search" (ionInput)="onSearch()"></ion-searchbar>
      <ion-item>
        <ion-select label="Status" [(ngModel)]="statusFilter" (ionChange)="load()">
          <ion-select-option value="">Todos</ion-select-option>
          <ion-select-option value="Active">Ativos</ion-select-option>
          <ion-select-option value="Disabled">Desativados</ion-select-option>
        </ion-select>
      </ion-item>
      <ion-list>
        @for (u of result()?.items ?? []; track u.id) {
          <ion-item [routerLink]="['/users', u.id]">
            <ion-label>
              <h3>{{ u.name }}</h3>
              <p>{{ u.email }}</p>
              <ion-chip [color]="u.status === 'Active' ? 'success' : 'medium'">{{ u.status }}</ion-chip>
              <p>{{ u.roles.join(', ') }}</p>
            </ion-label>
          </ion-item>
        }
      </ion-list>
      <ion-text>{{ result()?.total ?? 0 }} usuários</ion-text>
    </ion-content>
  `,
})
export class UsersListPage implements OnInit {
  private api = inject(ApiService);
  auth = inject(AuthService);

  result = signal<PagedResult<UserListItem> | null>(null);
  search = '';
  statusFilter = '';
  private debounce?: any;

  ngOnInit() { this.load(); }

  onSearch() {
    clearTimeout(this.debounce);
    this.debounce = setTimeout(() => this.load(), 300);
  }

  async load() {
    this.result.set(await this.api.listUsers({
      name: this.search || undefined,
      status: this.statusFilter || undefined,
      page: 1, pageSize: 50,
    }));
  }
}
