export type Role = 'SuperAdmin' | 'OrgAdmin' | 'Worker';

export type TaskStatus = 'Created' | 'InProgress' | 'Done' | 'Verified';

export interface User {
  id: string;
  email: string;
  fullName: string;
  role: Role;
  isActive: boolean;
  organizationId: string | null;
  organizationName: string | null;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface TaskComment {
  id: string;
  text: string;
  author: User;
  createdAt: string;
}

export interface TaskItem {
  id: string;
  title: string;
  description: string;
  latitude: number;
  longitude: number;
  deadline: string | null;
  status: TaskStatus;
  assignee: User | null;
  createdBy: User;
  createdAt: string;
  updatedAt: string;
  comments?: TaskComment[];
}

export interface TaskFilter {
  status?: TaskStatus | null;
  assigneeId?: string | null;
  search?: string | null;
}

export interface AppNotification {
  id: string;
  type: string;
  title: string;
  message: string;
  payloadJson: string;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
}

export interface NotificationPreferences {
  internal: boolean;
  email: boolean;
  sms: boolean;
  telegram: boolean;
}

export const STATUS_META: Record<TaskStatus, { label: string; color: string; severity: 'info' | 'warn' | 'success' | 'secondary' }> = {
  Created: { label: 'Created', color: '#3b82f6', severity: 'info' },
  InProgress: { label: 'In Progress', color: '#f59e0b', severity: 'warn' },
  Done: { label: 'Done', color: '#22c55e', severity: 'success' },
  Verified: { label: 'Verified', color: '#8b5cf6', severity: 'secondary' }
};

export const STATUS_LIST: TaskStatus[] = ['Created', 'InProgress', 'Done', 'Verified'];
