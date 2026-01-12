export enum Priority {
  Low = 0,
  Medium = 1,
  High = 2
}

export interface Task {
  id: number;
  title: string;
  description?: string;
  isComplete: boolean;
  priority: Priority;
  sortOrder: number;
  userId: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTaskDto {
  title: string;
  description?: string;
  priority?: Priority;
}

export interface UpdateTaskDto {
  title: string;
  description?: string;
  isComplete: boolean;
  priority: Priority;
  sortOrder?: number;
}

export interface User {
  id: number;
  email: string;
  firstName?: string;
  lastName?: string;
}

// Backend returns this format
export interface BackendAuthResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  user: User;
}

// Internal auth response used by the app
export interface AuthResponse {
  token: string;
  user: User;
  expiresIn: number;
}

export interface LoginDto {
  email: string;
  password: string;
}

export interface RegisterDto {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export type TaskFilter = 'all' | 'completed' | 'incomplete';
export type PriorityFilter = Priority | null;
