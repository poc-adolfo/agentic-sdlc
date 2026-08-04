import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { IonContent, IonList, IonItem, IonLabel, IonInput, IonButton, IonToggle, IonChip, IonText } from '@ionic/angular/standalone';
import { ApiService, RoleDetail, Permission } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-role-detail',
  standalone: true,
  imports: [FormsModule, IonContent, IonList, IonItem, IonLabel, IonInput, IonButton, IonToggle, IonChip, IonText],
  template: `
    <ion-content class="ion-padding">
      @if (role()) {
        <h2>{{ role()!.name }}</h2>
        <p>{{ role()!.description }}</p>
        @if (role()!.isSystem) { <ion-chip color="warning">is_system — edição restrita</ion-chip> }

        @if (auth.hasPermission('roles:manage') && !role()!.isSystem) {
          <ion-item><ion-input label="Nome" [(ngModel)]="editName" /></ion-item>
          <ion-item><ion-input label="Descrição" [(ngModel)]="editDesc" /></ion-item>
          <ion-button (click)="save()">Salvar</ion-button>
          <ion-button color="danger" (click)="remove()">Excluir</ion-button>
        }

        @if (auth.hasPermission('permissions:assign')) {
          <h3>Permissões</h3>
          @if (role()!.isSystem) { <ion-text color="warning">⚠ Papel is_system — aviso reforçado (seção 5).</ion-text> }
          <ion-list>
            @for (p of permissions(); track p.id) {
              <ion-item>
                <ion-toggle [checked]="selected.has(p.id)" (ionChange)="toggle(p.id, $event.detail.checked)">
                  <ion-label>{{ p.id }}</ion-label>
                </ion-toggle>
              </ion-item>
            }
          </ion-list>
          <ion-button (click)="savePermissions()">Salvar permissões</ion-button>
        }
      }
    </ion-content>
  `,
})
export class RoleDetailPage implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  auth = inject(AuthService);

  role = signal<RoleDetail | null>(null);
  permissions = signal<Permission[]>([]);
  selected = new Set<string>();
  editName = '';
  editDesc = '';

  async ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.role.set(await this.api.getRole(id));
    this.editName = this.role()?.name ?? '';
    this.editDesc = this.role()?.description ?? '';
    this.permissions.set(await this.api.listPermissions());
    this.selected = new Set(this.role()?.permissions ?? []);
  }

  toggle(perm: string, checked: boolean) {
    if (checked) this.selected.add(perm);
    else this.selected.delete(perm);
  }

  async savePermissions() {
    await this.api.setRolePermissions(this.role()!.id, [...this.selected]);
    this.role.set(await this.api.getRole(this.role()!.id));
  }

  async save() {
    this.role.set(await this.api.updateRole(this.role()!.id, { name: this.editName, description: this.editDesc }));
  }

  async remove() {
    await this.api.deleteRole(this.role()!.id);
  }
}
