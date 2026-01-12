import api from './axios';
import type { Task, CreateTaskDto, UpdateTaskDto, Priority } from '../types';

export const tasksApi = {
  getAll: async (isComplete?: boolean, priority?: Priority): Promise<Task[]> => {
    const params = new URLSearchParams();
    if (isComplete !== undefined) params.append('isComplete', String(isComplete));
    if (priority !== undefined) params.append('priority', String(priority));

    const query = params.toString();
    const response = await api.get<Task[]>(`/tasks${query ? `?${query}` : ''}`);
    return response.data;
  },

  getById: async (id: number): Promise<Task> => {
    const response = await api.get<Task>(`/tasks/${id}`);
    return response.data;
  },

  create: async (data: CreateTaskDto): Promise<Task> => {
    const response = await api.post<Task>('/tasks', data);
    return response.data;
  },

  update: async (id: number, data: UpdateTaskDto): Promise<Task> => {
    const response = await api.put<Task>(`/tasks/${id}`, data);
    return response.data;
  },

  delete: async (id: number): Promise<void> => {
    await api.delete(`/tasks/${id}`);
  },

  reorder: async (taskIds: number[]): Promise<void> => {
    await api.put('/tasks/reorder', { taskIds });
  },
};
