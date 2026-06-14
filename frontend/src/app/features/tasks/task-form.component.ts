import { AfterViewInit, Component, EventEmitter, Input, OnDestroy, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import * as L from 'leaflet';

import { TaskItem, User } from '../../core/models';
import { TaskService } from '../../core/task.service';
import { DEFAULT_CENTER, DEFAULT_ZOOM, createTileLayer, pickIcon } from '../../core/map-utils';

let mapSeq = 0;

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [CommonModule, FormsModule, DialogModule, ButtonModule, InputTextModule, TextareaModule, SelectModule, DatePickerModule, TranslatePipe],
  template: `
    <p-dialog
      [header]="(task ? 'tasks.form.editTitle' : 'tasks.form.newTitle') | translate"
      [visible]="visible"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '680px', maxWidth: '95vw' }"
      (onShow)="initMap()"
      (onHide)="destroyMap()">

      <div class="field">
        <label>{{ 'tasks.form.title' | translate }} *</label>
        <input pInputText [(ngModel)]="title" [placeholder]="'tasks.form.titlePlaceholder' | translate" />
      </div>

      <div class="field">
        <label>{{ 'tasks.form.description' | translate }}</label>
        <textarea pTextarea rows="3" [(ngModel)]="description" [placeholder]="'tasks.form.descriptionPlaceholder' | translate"></textarea>
      </div>

      <div class="grid-3">
        <div class="field">
          <label>{{ 'tasks.assignee' | translate }}</label>
          <p-select
            [options]="workers"
            optionLabel="fullName"
            optionValue="id"
            [(ngModel)]="assigneeId"
            [placeholder]="'tasks.unassigned' | translate"
            [showClear]="true"
            [filter]="true"
            appendTo="body" />
        </div>
        <div class="field">
          <label>{{ 'tasks.deadline' | translate }}</label>
          <p-datepicker
            [(ngModel)]="deadline"
            [showTime]="true"
            hourFormat="24"
            dateFormat="dd.mm.yy"
            [showClear]="true"
            (ngModelChange)="onDeadlineChange($event)"
            appendTo="body" />
        </div>
        <div class="field">
          <label>{{ 'tasks.form.reminder' | translate }}</label>
          <p-select
            [options]="reminderOptions"
            optionLabel="label"
            optionValue="value"
            [(ngModel)]="reminderOffsetMinutes"
            [disabled]="!deadline"
            appendTo="body" />
        </div>
      </div>

      <div class="field">
        <label>{{ 'tasks.location' | translate }} * <span class="muted">- {{ 'tasks.form.locationHint' | translate }}</span></label>
        <div class="pick-map" [id]="mapId"></div>
        @if (latitude !== null && longitude !== null) {
          <small class="muted">{{ latitude!.toFixed(5) }}, {{ longitude!.toFixed(5) }}</small>
        } @else {
          <small class="overdue">{{ 'tasks.form.locationMissing' | translate }}</small>
        }
      </div>

      <ng-template pTemplate="footer">
        <p-button [label]="'common.cancel' | translate" severity="secondary" [text]="true" (onClick)="close()" />
        <p-button [label]="(task ? 'tasks.form.saveChanges' : 'tasks.form.createTask') | translate" icon="pi pi-check" [loading]="saving" [disabled]="!canSave" (onClick)="save()" />
      </ng-template>
    </p-dialog>
  `,
  styles: [`
    .field { display: flex; flex-direction: column; gap: 0.35rem; margin-bottom: 1rem; }
    .field input, .field textarea { width: 100%; }
    .grid-3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 1rem; }
    @media (max-width: 720px) { .grid-3 { grid-template-columns: 1fr; } }
    .pick-map { height: 260px; border-radius: 8px; overflow: hidden; }
    :host ::ng-deep .p-select, :host ::ng-deep .p-datepicker { width: 100%; }
  `]
})
export class TaskFormComponent implements AfterViewInit, OnDestroy {
  @Input() visible = false;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Input() task: TaskItem | null = null;
  @Input() workers: User[] = [];
  @Output() saved = new EventEmitter<TaskItem>();

  readonly mapId = `task-form-map-${++mapSeq}`;

  title = '';
  description = '';
  assigneeId: string | null = null;
  deadline: Date | null = null;
  reminderOffsetMinutes: number | null = null;
  latitude: number | null = null;
  longitude: number | null = null;
  saving = false;

  private map: L.Map | null = null;
  private marker: L.Marker | null = null;

  constructor(private tasks: TaskService, private messages: MessageService, private translate: TranslateService) {}

  get reminderOptions(): Array<{ label: string; value: number | null }> {
    return [
      { label: this.translate.instant('tasks.form.noReminder'), value: null },
      { label: this.translate.instant('tasks.form.reminder1h'), value: 60 },
      { label: this.translate.instant('tasks.form.reminder4h'), value: 240 },
      { label: this.translate.instant('tasks.form.reminder1d'), value: 1440 }
    ];
  }

  get canSave(): boolean {
    return !!this.title.trim() && this.latitude !== null && this.longitude !== null;
  }

  ngAfterViewInit(): void {
    if (this.visible) this.initMap();
  }

  ngOnDestroy(): void {
    this.destroyMap();
  }

  onVisibleChange(value: boolean): void {
    this.visible = value;
    this.visibleChange.emit(value);
  }

  openCreate(): void {
    this.task = null;
    this.ensureWorkers();
    this.onVisibleChange(true);
  }

  openEdit(task: TaskItem): void {
    this.task = task;
    this.ensureWorkers();
    this.onVisibleChange(true);
  }

  private ensureWorkers(): void {
    if (this.workers.length > 0) return;
    this.tasks.workers().subscribe({
      next: ws => (this.workers = ws),
      error: () => {}
    });
  }

  initMap(): void {
    this.patchFromTask();
    setTimeout(() => {
      if (this.map) {
        this.map.invalidateSize();
        return;
      }
      const center: L.LatLngTuple =
        this.latitude !== null && this.longitude !== null
          ? [this.latitude, this.longitude]
          : DEFAULT_CENTER;

      this.map = L.map(this.mapId, { attributionControl: false }).setView(center, DEFAULT_ZOOM);
      createTileLayer().addTo(this.map);

      if (this.latitude !== null && this.longitude !== null) {
        this.placeMarker(this.latitude, this.longitude);
      }

      this.map.on('click', (e: L.LeafletMouseEvent) => {
        this.placeMarker(e.latlng.lat, e.latlng.lng);
      });
    }, 50);
  }

  destroyMap(): void {
    this.map?.remove();
    this.map = null;
    this.marker = null;
  }

  private patchFromTask(): void {
    if (this.task) {
      this.title = this.task.title;
      this.description = this.task.description;
      this.assigneeId = this.task.assignee?.id ?? null;
      this.deadline = this.task.deadline ? new Date(this.task.deadline) : null;
      this.reminderOffsetMinutes = this.task.reminderOffsetMinutes ?? null;
      this.latitude = this.task.latitude;
      this.longitude = this.task.longitude;
    } else {
      this.title = '';
      this.description = '';
      this.assigneeId = null;
      this.deadline = null;
      this.reminderOffsetMinutes = null;
      this.latitude = null;
      this.longitude = null;
    }
  }

  onDeadlineChange(value: Date | null): void {
    if (!value) {
      this.reminderOffsetMinutes = null;
    }
  }

  private placeMarker(lat: number, lng: number): void {
    this.latitude = lat;
    this.longitude = lng;
    if (!this.map) return;

    if (!this.marker) {
      this.marker = L.marker([lat, lng], { icon: pickIcon(), draggable: true }).addTo(this.map);
      this.marker.on('dragend', () => {
        const pos = this.marker!.getLatLng();
        this.latitude = pos.lat;
        this.longitude = pos.lng;
      });
    } else {
      this.marker.setLatLng([lat, lng]);
    }
  }

  close(): void {
    this.onVisibleChange(false);
  }

  save(): void {
    if (!this.canSave) return;
    this.saving = true;

    const payload = {
      title: this.title.trim(),
      description: this.description.trim(),
      latitude: this.latitude!,
      longitude: this.longitude!,
      assigneeId: this.assigneeId,
      deadline: this.deadline ? this.deadline.toISOString() : null,
      reminderOffsetMinutes: this.deadline ? this.reminderOffsetMinutes : null
    };

    const request$ = this.task
      ? this.tasks.update(this.task.id, payload)
      : this.tasks.create(payload);

    request$.subscribe({
      next: task => {
        this.saving = false;
        this.messages.add({
          severity: 'success',
          summary: this.translate.instant(this.task ? 'tasks.messages.updated' : 'tasks.messages.created')
        });
        this.saved.emit(task);
        this.close();
      },
      error: err => {
        this.saving = false;
        this.messages.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('tasks.messages.saveFailed')
        });
      }
    });
  }
}
