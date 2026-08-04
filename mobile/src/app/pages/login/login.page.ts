import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { IonContent, IonButton, IonInput, IonItem, IonLabel, IonList, IonNote, IonText } from '@ionic/angular/standalone';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, IonContent, IonButton, IonInput, IonItem, IonLabel, IonList, IonNote, IonText],
  template: `
    <ion-content class="ion-padding">
      <h2>Admin Mobile</h2>
      @if (error()) { <ion-text color="danger">{{ error() }}</ion-text> }
      <ion-list>
        <ion-item><ion-input label="E-mail" type="email" [(ngModel)]="email" /></ion-item>
        <ion-item><ion-input label="Senha" type="password" [(ngModel)]="password" /></ion-item>
      </ion-list>
      <ion-button expand="block" (click)="submit()">Entrar</ion-button>
      <ion-button expand="block" fill="clear" (click)="registerMode.set(!registerMode())">
        {{ registerMode() ? 'Já tenho conta' : 'Criar conta' }}
      </ion-button>
      @if (registerMode()) {
        <ion-item><ion-input label="Nome" [(ngModel)]="name" /></ion-item>
        <ion-button expand="block" (click)="submit(true)">Registrar</ion-button>
      }
      <ion-note>Se for o primeiro usuário, você vira Administrador automaticamente.</ion-note>
    </ion-content>
  `,
})
export class LoginPage {
  private auth = inject(AuthService);
  private router = inject(Router);

  email = '';
  password = '';
  name = '';
  error = signal('');
  registerMode = signal(false);

  async submit(isRegister = false) {
    this.error.set('');
    try {
      if (isRegister) await this.auth.register(this.name, this.email, this.password);
      else await this.auth.login(this.email, this.password);
      this.router.navigate(['/users']);
    } catch (e: any) {
      this.error.set(e?.error?.error ?? 'Erro ao autenticar');
    }
  }
}
