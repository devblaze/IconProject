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
}

export interface User {
  email: string;
  firstName?: string;
  lastName?: string;
}

export interface AuthResponse {
  token: string;
  email: string;
  firstName?: string;
  lastName?: string;
  expiresAt: string;
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
