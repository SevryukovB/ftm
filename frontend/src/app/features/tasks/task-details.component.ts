import { AfterViewInit, Component, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { TextareaModule } from 'primeng/textarea';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import * as L from 'leaflet';

import { AuthService } from '../../core/auth.service';
import { TaskService } from '../../core/task.service';
import { STATUS_META, TaskItem, TaskStatus } from '../../core/models';
import { DEFAULT_ZOOM, createTileLayer, statusIcon } from '../../core/map-utils';
import { TaskFormComponent } from './task-form.component';

@Component({
  selector: 'app-task-details',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink, CardModule, TagModule, ButtonModule,
    TextareaModule, ConfirmDialogModule, TranslatePipe, TaskFormComponent
  ],
  providers: [ConfirmationService],
  template: `
    <div class="page">
      <p-button [label]="'tasks.details.back' | translate" icon="pi pi-arrow-left" [text]="true" routerLink="/tasks" />

      @if (task(); as t) {
        <div class="details-grid">
          <p-card>
            <ng-template #title>
              <div class="card-head">
                <span>{{ t.title }}</span>
                <p-tag [value]="statusLabel(t.status)" [severity]="severity(t.status)" />
              </div>
            </ng-template>

            <div class="meta">
              <div class="meta-row"><span class="label">{{ 'tasks.description' | translate }}</span><span>{{ t.description || '—' }}</span></div>
              <div class="meta-row"><span class="label">{{ 'tasks.assignee' | translate }}</span><span>{{ t.assignee?.fullName || ('tasks.unassigned' | translate) }}</span></div>
              <div class="meta-row">
                <span class="label">{{ 'tasks.deadline' | translate }}</span>
                <span [class.overdue]="isOverdue(t)">{{ t.deadline ? (t.deadline | date: 'dd.MM.yyyy HH:mm') : '—' }}</span>
              </div>
              <div class="meta-row"><span class="label">{{ 'tasks.createdBy' | translate }}</span><span>{{ t.createdBy?.fullName || '—' }}, {{ t.createdAt | date: 'dd.MM.yyyy HH:mm' }}</span></div>
              <div class="meta-row"><span class="label">{{ 'tasks.location' | translate }}</span><span>{{ t.latitude | number: '1.5-5' }}, {{ t.longitude | number: '1.5-5' }}</span></div>
            </div>

            <div class="actions">
              @for (s of availableTransitions(t); track s) {
                <p-button [label]="actionLabel(s)" [icon]="actionIcon(s)"
                          [severity]="actionSeverity(s)" (onClick)="changeStatus(s)" />
              }
              @if (auth.isAdmin()) {
                <p-button [label]="'common.edit' | translate" icon="pi pi-pencil" [outlined]="true" (onClick)="form?.openEdit(t)" />
                <p-button [label]="'common.delete' | translate" icon="pi pi-trash" severity="danger" [outlined]="true" (onClick)="confirmDelete()" />
              }
            </div>
          </p-card>

          <p-card>
            <ng-template #title>
              <div class="card-head">
                <span>{{ 'tasks.location' | translate }}</span>
                @if (auth.isAdmin()) {
                  <small class="hint">{{ 'tasks.details.dragHint' | translate }}</small>
                }
              </div>
            </ng-template>
            <div id="details-map" class="details-map"></div>
            @if (auth.isAdmin() && locationDirty()) {
              <div class="loc-actions">
                <p-button [label]="'tasks.details.saveLocation' | translate" icon="pi pi-check" size="small" (onClick)="saveLocation()" />
                <p-button [label]="'common.reset' | translate" icon="pi pi-undo" size="small" [text]="true" (onClick)="resetLocation()" />
              </div>
            }
          </p-card>
        </div>

        <p-card styleClass="comments-card">
          <ng-template #title>{{ 'comments.title' | translate: { count: t.comments?.length || 0 } }}</ng-template>
          <div class="comments">
            @for (c of t.comments; track c.id) {
              <div class="comment">
                <div class="comment-head">
                  <strong>{{ c.author?.fullName || ('comments.unknownAuthor' | translate) }}</strong>
                  <span class="comment-date">{{ c.createdAt | date: 'dd.MM.yyyy HH:mm' }}</span>
                </div>
                <div class="comment-text">{{ c.text }}</div>
              </div>
            } @empty {
              <div class="no-comments">{{ 'comments.empty' | translate }}</div>
            }
          </div>
          <div class="add-comment">
            <textarea pTextarea rows="3" [placeholder]="'comments.placeholder' | translate" [(ngModel)]="commentText"></textarea>
            <p-button [label]="'comments.add' | translate" icon="pi pi-send"
                      [disabled]="!commentText.trim() || sending()"
                      (onClick)="addComment()" />
          </div>
        </p-card>
      } @else if (!loading()) {
        <p-card><div class="no-comments">{{ 'tasks.details.notFound' | translate }}</div></p-card>
      }
    </div>

    <app-task-form #form (saved)="reload()" />
    <p-confirmDialog />
  `,
  styles: [`
    .details-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin: 1rem 0; }
    @media (max-width: 900px) { .details-grid { grid-template-columns: 1fr; } }
    .card-head { display: flex; align-items: center; justify-content: space-between; gap: .5rem; }
    .hint { color: var(--p-text-muted-color); font-weight: 400; }
    .meta { display: flex; flex-direction: column; gap: .55rem; }
    .meta-row { display: grid; grid-template-columns: 8rem 1fr; gap: .5rem; }
    .label { color: var(--p-text-muted-color); }
    .overdue { color: var(--p-red-500); font-weight: 600; }
    .actions { display: flex; gap: .5rem; flex-wrap: wrap; margin-top: 1.25rem; }
    .details-map { height: 320px; border-radius: 8px; }
    .loc-actions { display: flex; gap: .5rem; margin-top: .75rem; }
    .comments { display: flex; flex-direction: column; gap: .75rem; margin-bottom: 1rem; }
    .comment { background: var(--p-surface-100); border-radius: 8px; padding: .65rem .85rem; }
    :host-context(.app-dark) .comment { background: var(--p-surface-800); }
    .comment-head { display: flex; gap: .75rem; align-items: baseline; margin-bottom: .25rem; }
    .comment-date { color: var(--p-text-muted-color); font-size: .8rem; }
    .comment-text { white-space: pre-wrap; }
    .no-comments { color: var(--p-text-muted-color); padding: .5rem 0; }
    .add-comment { display: flex; flex-direction: column; gap: .5rem; }
    .add-comment textarea { width: 100%; }
    .add-comment p-button { align-self: flex-end; }
  `]
})
export class TaskDetailsComponent implements OnInit, AfterViewInit, OnDestroy {
  readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly taskService = inject(TaskService);
  private readonly messages = inject(MessageService);
  private readonly confirm = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);

  @ViewChild('form') form?: TaskFormComponent;

  readonly task = signal<TaskItem | null>(null);
  readonly loading = signal(true);
  readonly sending = signal(false);
  readonly locationDirty = signal(false);

  commentText = '';

  private id = '';
  private map?: L.Map;
  private marker?: L.Marker;
  private viewReady = false;

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id') ?? '';
    this.reload();
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.tryInitMap();
  }

  ngOnDestroy(): void {
    this.map?.remove();
  }

  reload(): void {
    this.loading.set(true);
    this.taskService.get(this.id).subscribe({
      next: t => {
        this.task.set(t);
        this.loading.set(false);
        this.locationDirty.set(false);
        setTimeout(() => this.tryInitMap(), 0);
      },
      error: err => {
        this.loading.set(false);
        this.task.set(null);
        this.messages.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('tasks.details.loadFailed')
        });
      }
    });
  }

  private tryInitMap(): void {
    const t = this.task();
    if (!this.viewReady || !t) return;
    const el = document.getElementById('details-map');
    if (!el) return;

    const pos: L.LatLngExpression = [t.latitude, t.longitude];

    if (!this.map) {
      this.map = L.map(el, { center: pos, zoom: DEFAULT_ZOOM + 2 });
      createTileLayer().addTo(this.map);
      setTimeout(() => this.map?.invalidateSize(), 50);
    } else {
      this.map.setView(pos);
    }

    if (this.marker) this.marker.remove();
    this.marker = L.marker(pos, {
      icon: statusIcon(t.status),
      draggable: this.auth.isAdmin()
    }).addTo(this.map);

    this.marker.on('dragend', () => this.locationDirty.set(true));
  }

  saveLocation(): void {
    const t = this.task();
    const pos = this.marker?.getLatLng();
    if (!t || !pos) return;
    this.taskService.updateLocation(t.id, pos.lat, pos.lng).subscribe({
      next: () => {
        this.messages.add({
          severity: 'success',
          summary: this.translate.instant('common.saved'),
          detail: this.translate.instant('tasks.details.locationUpdated')
        });
        this.reload();
      },
      error: err => this.messages.add({
        severity: 'error',
        summary: this.translate.instant('common.error'),
        detail: this.translate.instant('tasks.details.locationUpdateFailed')
      })
    });
  }

  resetLocation(): void {
    const t = this.task();
    if (!t || !this.marker) return;
    this.marker.setLatLng([t.latitude, t.longitude]);
    this.locationDirty.set(false);
  }

  availableTransitions(t: TaskItem): TaskStatus[] {
    const me = this.auth.user();
    if (!me) return [];
    if (me.role === 'Admin') {
      return t.status === 'Done' ? ['Verified', 'InProgress'] : [];
    }
    if (t.assignee?.id !== me.id) return [];
    if (t.status === 'Created') return ['InProgress'];
    if (t.status === 'InProgress') return ['Done'];
    return [];
  }

  actionLabel(s: TaskStatus): string {
    switch (s) {
      case 'InProgress': return this.translate.instant(this.task()?.status === 'Done' ? 'tasks.actions.returnToWork' : 'tasks.actions.startWork');
      case 'Done': return this.translate.instant('tasks.actions.markDone');
      case 'Verified': return this.translate.instant('tasks.actions.verifyClose');
      default: return this.statusLabel(s);
    }
  }

  actionIcon(s: TaskStatus): string {
    switch (s) {
      case 'InProgress': return this.task()?.status === 'Done' ? 'pi pi-replay' : 'pi pi-play';
      case 'Done': return 'pi pi-check';
      case 'Verified': return 'pi pi-verified';
      default: return 'pi pi-arrow-right';
    }
  }

  actionSeverity(s: TaskStatus): any {
    switch (s) {
      case 'Done': return 'success';
      case 'Verified': return 'help';
      default: return 'primary';
    }
  }

  changeStatus(status: TaskStatus): void {
    this.taskService.changeStatus(this.id, status).subscribe({
      next: () => {
        this.messages.add({
          severity: 'success',
          summary: this.translate.instant('tasks.messages.statusChanged'),
          detail: this.translate.instant('tasks.messages.taskIsNow', { status: this.statusLabel(status) })
        });
        this.reload();
      },
      error: err => this.messages.add({
        severity: 'error',
        summary: this.translate.instant('common.error'),
        detail: this.translate.instant('tasks.messages.statusChangeFailed')
      })
    });
  }

  addComment(): void {
    const text = this.commentText.trim();
    if (!text) return;
    this.sending.set(true);
    this.taskService.addComment(this.id, text).subscribe({
      next: () => {
        this.sending.set(false);
        this.commentText = '';
        this.reload();
      },
      error: err => {
        this.sending.set(false);
        this.messages.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('comments.addFailed')
        });
      }
    });
  }

  confirmDelete(): void {
    this.confirm.confirm({
      message: this.translate.instant('tasks.delete.message'),
      header: this.translate.instant('tasks.delete.header'),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonProps: { label: this.translate.instant('common.delete'), severity: 'danger' },
      rejectButtonProps: { label: this.translate.instant('common.cancel'), severity: 'secondary', outlined: true },
      accept: () => {
        this.taskService.remove(this.id).subscribe({
          next: () => {
            this.messages.add({
              severity: 'success',
              summary: this.translate.instant('common.deleted'),
              detail: this.translate.instant('tasks.messages.removed')
            });
            this.router.navigate(['/tasks']);
          },
          error: err => this.messages.add({
            severity: 'error',
            summary: this.translate.instant('common.error'),
            detail: this.translate.instant('tasks.messages.deleteFailed')
          })
        });
      }
    });
  }

  severity(status: TaskStatus): string {
    return STATUS_META[status]?.severity ?? 'info';
  }

  statusLabel(status: TaskStatus): string {
    return this.translate.instant(`status.${status}`);
  }

  isOverdue(t: TaskItem): boolean {
    return !!t.deadline
      && t.status !== 'Done' && t.status !== 'Verified'
      && new Date(t.deadline).getTime() < Date.now();
  }
}
