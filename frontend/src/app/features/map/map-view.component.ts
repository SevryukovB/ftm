import { AfterViewInit, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import * as L from 'leaflet';

import { TaskService } from '../../core/task.service';
import { STATUS_LIST, STATUS_META, TaskItem, TaskStatus } from '../../core/models';
import { DEFAULT_CENTER, DEFAULT_ZOOM, createTileLayer, statusIcon } from '../../core/map-utils';

@Component({
  selector: 'app-map-view',
  standalone: true,
  imports: [CommonModule, FormsModule, SelectModule, TranslatePipe],
  template: `
    <div class="page map-page">
      <div class="map-toolbar">
        <h2 class="title">{{ 'nav.map' | translate }}</h2>
        <p-select [options]="statusOptions()"
                  [ngModel]="status()"
                  (ngModelChange)="status.set($event); render()"
                  optionLabel="label" optionValue="value"
                  [placeholder]="'map.allStatuses' | translate" [showClear]="true"
                  styleClass="filter-select" />
      </div>

      <div class="map-wrap">
        <div id="tasks-map" class="tasks-map"></div>
        <div class="map-legend">
          @for (s of statusList; track s) {
            <div class="legend-item">
              <span class="legend-dot" [style.background]="meta[s].color"></span>
              <span>{{ statusLabel(s) }}</span>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .map-page { display: flex; flex-direction: column; height: calc(100vh - 5.5rem); }
    .map-toolbar { display: flex; align-items: center; justify-content: space-between; margin-bottom: .75rem; gap: .75rem; flex-wrap: wrap; }
    .title { margin: 0; }
    :host ::ng-deep .filter-select { min-width: 12rem; }
    .map-wrap { position: relative; flex: 1; min-height: 420px; }
    .tasks-map { position: absolute; inset: 0; border-radius: 10px; }
    .legend-item { display: flex; align-items: center; gap: .45rem; }
    .legend-dot { width: .8rem; height: .8rem; border-radius: 50%; display: inline-block; border: 2px solid #fff; box-shadow: 0 0 2px rgba(0,0,0,.5); }
  `]
})
export class MapViewComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly taskService = inject(TaskService);
  private readonly messages = inject(MessageService);
  private readonly translate = inject(TranslateService);

  readonly status = signal<TaskStatus | null>(null);
  readonly statusList = STATUS_LIST;
  readonly meta = STATUS_META;
  readonly statusOptions = computed(() => {
    this.translate.currentLang();
    return STATUS_LIST.map(s => ({ label: this.statusLabel(s), value: s }));
  });

  private tasks: TaskItem[] = [];
  private map?: L.Map;
  private markersLayer = L.layerGroup();
  private langSub?: Subscription;

  ngOnInit(): void {
    this.langSub = this.translate.onLangChange.subscribe(() => this.render());
    this.taskService.list({}).subscribe({
      next: tasks => { this.tasks = tasks; this.render(); },
      error: err => this.messages.add({
        severity: 'error',
        summary: this.translate.instant('common.error'),
        detail: this.translate.instant('tasks.loadFailed')
      })
    });
  }

  ngAfterViewInit(): void {
    const el = document.getElementById('tasks-map');
    if (!el) return;
    this.map = L.map(el, { center: DEFAULT_CENTER, zoom: DEFAULT_ZOOM });
    createTileLayer().addTo(this.map);
    this.markersLayer.addTo(this.map);
    setTimeout(() => { this.map?.invalidateSize(); this.render(); }, 50);
  }

  ngOnDestroy(): void {
    this.langSub?.unsubscribe();
    this.map?.remove();
  }

  render(): void {
    if (!this.map) return;
    this.markersLayer.clearLayers();

    const visible = this.status()
      ? this.tasks.filter(t => t.status === this.status())
      : this.tasks;

    const bounds: L.LatLngExpression[] = [];

    for (const t of visible) {
      const pos: L.LatLngExpression = [t.latitude, t.longitude];
      bounds.push(pos);
      const marker = L.marker(pos, { icon: statusIcon(t.status) });
      const safeTitle = this.escape(t.title);
      const assignee = t.assignee ? this.escape(t.assignee.fullName) : this.escape(this.translate.instant('tasks.unassigned'));
      const statusLabel = this.escape(this.statusLabel(t.status));
      const statusText = this.escape(this.translate.instant('tasks.status'));
      const assigneeText = this.escape(this.translate.instant('tasks.assignee'));
      const openText = this.escape(this.translate.instant('map.openTask'));
      marker.bindPopup(
        `<div style="min-width:180px">
           <div style="font-weight:600;margin-bottom:.25rem">${safeTitle}</div>
           <div>${statusText}: <b style="color:${STATUS_META[t.status].color}">${statusLabel}</b></div>
           <div>${assigneeText}: ${assignee}</div>
           <a href="/tasks/${t.id}" style="display:inline-block;margin-top:.4rem">${openText} -></a>
         </div>`
      );
      marker.addTo(this.markersLayer);
    }

    if (bounds.length > 0) {
      this.map.fitBounds(L.latLngBounds(bounds), { padding: [40, 40], maxZoom: 14 });
    }
  }

  private escape(s: string): string {
    return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  }

  statusLabel(status: TaskStatus): string {
    return this.translate.instant(`status.${status}`);
  }
}
