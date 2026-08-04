# Administração de usuários, perfis e permissões (app mobile)

Implementação da spec `specs/administracao-usuarios-perfis-permissoes-mobile.md`.

## Stack (seção 9 da spec)

- **Mobile**: Ionic + Angular 18 (standalone components, Capacitor para iOS/Android)
- **Backend**: .NET 8 Web API / C# com Entity Framework Core + PostgreSQL
- **Auth**: ASP.NET Core Identity + JWT
- **Hospedagem**: dockerizada e rodável localmente (sem cluster k3s nesta fase)

## Estrutura

```
backend/src/AdminApi/          # API .NET 8
  Data/Entities/               # Modelo de dados (seção 3): User, Role, Permission, RolePermission, UserRole
  Authorization/               # Catálogo de permissões + PermissionHandler + policies dinâmicas
  Services/                    # UserService, RoleService, AdminSeeder (bootstrap seção 4), TokenService
  Controllers/                 # Auth, Users, Roles, Permissions
mobile/src/app/                # App Ionic + Angular
  pages/                       # Telas de administração (seção 5)
  guards/                      # authGuard + permissionGuard (UX, não segurança — seção 6)
  services/                    # AuthService (token + permissões), ApiService, http interceptor
docker-compose.yml            # API + Postgres + preview web
```

## Modelo de dados (seção 3)

- `users` — id, email (único), password_hash, name, status (active/disabled), created_at, last_login_at
- `roles` — id, name, description, is_system
- `permissions` — catálogo fixo em código (formato `recurso:ação`)
- `role_permissions` — junção role × permission (granularidade)
- `user_roles` — junção user × role (múltiplos papéis por usuário)

`permissões_efetivas(user) = união das permissions de todos os roles em user_roles(user)`

## Bootstrap do admin inicial (seção 4)

- **4.1**: Tabela `users` vazia → primeiro cadastro vira Administrador (is_system=true, todas as permissões).
- **4.2**: Flag `ALLOW_ADMIN_BOOTSTRAP=true` → próximo login sem papel vira Administrador. Aviso visível no app. Log de auditoria.

## Como rodar

```bash
# API + Postgres + preview web
make up

# Apenas a API (dev local, precisa de Postgres na porta 5432)
cd backend/src/AdminApi
dotnet restore
dotnet ef migrations add Init
dotnet run

# Mobile (dev server com proxy para a API)
cd mobile
npm install
npm start
```

## Segurança

- Validação de permissão é sempre no backend (seção 6): `[HasPermission("users:create")]` por endpoint.
- Esconder botões no mobile é UX, não controle de segurança.
- JWT carrega as permissões efetivas como claims `perm`. Token expira em 8h.
- Soft-delete: usuários desativados (`status=disabled`) nunca são excluídos fisicamente.
