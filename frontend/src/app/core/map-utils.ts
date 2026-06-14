import * as L from 'leaflet';
import { STATUS_META, TaskStatus } from './models';

export const DEFAULT_CENTER: L.LatLngTuple = [50.4501, 30.5234]; // Kyiv
export const DEFAULT_ZOOM = 11;

export function createTileLayer(): L.TileLayer {
  return L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; OpenStreetMap contributors'
  });
}

export function statusIcon(status: TaskStatus): L.DivIcon {
  const color = STATUS_META[status].color;
  return L.divIcon({
    className: '',
    html: `<span class="ftm-marker" style="background:${color}"></span>`,
    iconSize: [22, 22],
    iconAnchor: [11, 22],
    popupAnchor: [0, -22]
  });
}

export function pickIcon(): L.DivIcon {
  return L.divIcon({
    className: '',
    html: `<span class="ftm-marker" style="background:#0ea5e9"></span>`,
    iconSize: [22, 22],
    iconAnchor: [11, 22],
    popupAnchor: [0, -22]
  });
}
