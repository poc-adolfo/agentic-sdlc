import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { IonContent, IonList, IonItem, IonLabel, IonInput, IonButton, IonSelect, IonSelectOption, IonChip, IonText, IonNote } from '@ionic/angular/standalone';
import { ApiService, UserDetail, RoleListItem } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [FormsModule, IonContent, IonList, IonItem, IonLabel, IonInput, IonButton, IonSelect, IonSelectOption, IonChip, IonText, IonNote],
  template: `
    <ion-content class="ion-padding">
      @if (user()) {
        <h2>{{ user()!.name }}</h2>
        <p>{{ user()!.email }}</p>
        <ion-chip [color]="user()!.status === 'Active' ? 'success' : 'medium'">{{ user()!.status }}</ion-chip>

        @if (auth.hasPermission('users:edit')) {
          <ion-list>
            <ion-item><ion-input label="Nome" [(ngModel)]="editName" /></ion-item>
            <ion-item><ion-input label="E-mail" [(ngModel)]="editEmail" /></ion-item>
          </ion-list>
          <ion-button (click)="save()">Salvar</ion-button>
        }

        @if (auth.hasPermission('roles:assign')) {
          <h3>Papéis</h3>
          <ion-select label="Papéis" [multiple]="true" [(ngModel)]="selectedRoles" (ionChange)="onRolesChange()">
            @for (r of roles(); track r.id) {
              <ion-select-option [value]="r.id">{{ r.name }}</ion-select-option>
            }
          </ion-select>
          <ion-button (click)="saveRoles()" [disabled]="!dirty()">Salvar papéis</ion-button>

          <h4>Prévia de permissões efetivas</h4>
          <ion-note color="medium">União das permissões dos papéis selecionados — atualizada em tempo real, antes de salvar.</ion-note>
          @if (previewLoading()) {
            <ion-text color="medium"><p>Calculando…</p></ion-text>
          } @else if (previewPermissions().length === 0) {
            <ion-text color="medium"><p>Nenhuma permissão para a seleção atual.</p></ion-text>
          } @else {
            <ion-list>
              @for (p of previewPermissions(); track p) {
                <ion-item><ion-label>{{ p }}</ion-label></ion-item>
              }
            </ion-list>
          }
        }

        @if (auth.hasPermission('users:disable')) {
          @if (user()!.status === 'Active') {
            <ion-button color="danger" (click)="disable()">Desativar</ion-button>
          } @else {
            <ion-button color="success" (click)="reactivate()">Reativar</ion-button>
          }
        }

        <h3>Permissões efetivas (salvas)</h3>
        <ion-list>
          @for (p of user()!.effectivePermissions; track p) {
            <ion-item><ion-label>{{ p }}</ion-label></ion-item>
          }
        </ion-list>
      }
    </ion-content>
  `,
})
export class UserDetailPage implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  auth = inject(AuthService);

  user = signal<UserDetail | null>(null);
  roles = signal<RoleListItem[]>([]);
  editName = '';
  editEmail = '';
  selectedRoles: string[] = [];
  dirty = signal(false);

  previewPermissions = signal<string[]>([]);
  previewLoading = signal(false);

  async ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.user.set(await this.api.getUser(id));
    this.editName = this.user()?.name ?? '';
    this.editEmail = this.user()?.email ?? '';
    this.roles.set(await this.api.listRoles());
    this.selectedRoles = this.user()?.roles ? await this.matchRoleIds() : [];
    // Calcula a prévia inicial com base na seleção carregada do usuário.
    await this.updatePreview();
  }

  private async matchRoleIds(): Promise<string[]> {
    const all = await this.api.listRoles();
    const names = new Set(this.user()!.roles);
    return all.filter(r => names.has(r.name)).map(r => r.id);
  }

  async save() {
    const id = this.user()!.id;
    this.user.set(await this.api.updateUser(id, { name: this.editName, email: this.editEmail }));
  }

  async saveRoles() {
    await this.api.assignRoles(this.user()!.id, this.selectedRoles);
    this.user.set(await this.api.getUser(this.user()!.id));
    this.dirty.set(false);
    // Recalcula a prévia para refletir o estado salvo (deve coincidir com as permissões efetivas).
    await this.updatePreview();
  }

  async disable() {
    await this.api.disableUser(this.user()!.id);
    this.user.set(await this.api.getUser(this.user()!.id));
  }

  async reactivate() {
    await this.api.reactivateUser(this.user()!.id);
    this.user.set(await this.api.getUser(this.user()!.id));
  }

  /** Disparado a cada mudança na seleção de papéis. */
  onRolesChange() {
    this.dirty.set(true);
    void this.updatePreview();
  }

  /**
   * Calcula a união (deduplicada) das permissões de todos os papéis
   * atualmente selecionados, consultando GET /api/roles/{id} para cada um.
   * Resultado ordenado alfabeticamente para exibição estável.
   */
  private async updatePreview() {
    const ids = [...(this.selectedRoles ?? [])];
    if (ids.length === 0) {
      this.previewPermissions.set([]);
      return;
    }
    this.previewLoading.set(true);
    try {
      const details = await Promise.all(ids.map(id => this.api.getRole(id)));
      const union = new Set<string>();
      for (const d of details) {
        for (const p of d.permissions ?? []) union.add(p);
      }
      this.previewPermissions.set([...union].sort((a, b) => a.localeCompare(b)));
    } finally {
      this.previewLoading.set(false);
    }
  }
}
