import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { adminShellMatch, orgAdminGuard, superAdminGuard, workerShellMatch } from './core/role.guard';
import { LoginComponent } from './features/auth/login.component';
import { RegisterComponent } from './features/auth/register.component';
import { AdminShellComponent } from './features/admin/admin-shell.component';
import { WorkerShellComponent } from './features/worker/worker-shell.component';
import { TaskListComponent } from './features/tasks/task-list.component';
import { TaskDetailsComponent } from './features/tasks/task-details.component';
import { MapViewComponent } from './features/map/map-view.component';
import { UsersComponent } from './features/users/users.component';
import { OrganizationsComponent } from './features/organizations/organizations.component';
import { HomeRedirectComponent } from './features/home-redirect.component';
import { NotificationsComponent } from './features/notifications/notifications.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  {
    path: '',
    component: AdminShellComponent,
    canMatch: [adminShellMatch],
    canActivate: [authGuard],
    children: [
      { path: '', component: HomeRedirectComponent },
      { path: 'tasks', component: TaskListComponent, canActivate: [orgAdminGuard] },
      { path: 'tasks/:id', component: TaskDetailsComponent, canActivate: [orgAdminGuard] },
      { path: 'map', component: MapViewComponent, canActivate: [orgAdminGuard] },
      { path: 'notifications', component: NotificationsComponent },
      { path: 'users', component: UsersComponent, canActivate: [orgAdminGuard] },
      { path: 'organizations', component: OrganizationsComponent, canActivate: [superAdminGuard] }
    ]
  },
  {
    path: '',
    component: WorkerShellComponent,
    canMatch: [workerShellMatch],
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'tasks', pathMatch: 'full' },
      { path: 'tasks', component: TaskListComponent },
      { path: 'tasks/:id', component: TaskDetailsComponent },
      { path: 'map', component: MapViewComponent },
      { path: 'notifications', component: NotificationsComponent }
    ]
  },
  { path: '**', redirectTo: '' }
];
