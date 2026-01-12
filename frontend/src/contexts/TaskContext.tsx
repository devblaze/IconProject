import React, { createContext, useState, type ReactNode, useCallback } from 'react';
import { tasksApi } from '../api/tasks';
import type { Task, CreateTaskDto, UpdateTaskDto, TaskFilter, Priority } from '../types';

interface TaskContextType {
  tasks: Task[];
  isLoading: boolean;
  error: string | null;
  statusFilter: TaskFilter;
  priorityFilter: Priority | null;
  fetchTasks: () => Promise<void>;
  createTask: (data: CreateTaskDto) => Promise<void>;
  updateTask: (id: number, data: UpdateTaskDto) => Promise<void>;
  deleteTask: (id: number) => Promise<void>;
  reorderTasks: (taskIds: number[]) => Promise<void>;
  setStatusFilter: (filter: TaskFilter) => void;
  setPriorityFilter: (filter: Priority | null) => void;
}

export const TaskContext = createContext<TaskContextType | undefined>(undefined);

interface TaskProviderProps {
  children: ReactNode;
}

export const TaskProvider: React.FC<TaskProviderProps> = ({ children }) => {
  const [tasks, setTasks] = useState<Task[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<TaskFilter>('all');
  const [priorityFilter, setPriorityFilter] = useState<Priority | null>(null);

  const fetchTasks = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const isComplete =
        statusFilter === 'completed'
          ? true
          : statusFilter === 'incomplete'
            ? false
            : undefined;

      const data = await tasksApi.getAll(isComplete, priorityFilter ?? undefined);
      setTasks(data);
    } catch {
      setError('Failed to fetch tasks');
    } finally {
      setIsLoading(false);
    }
  }, [statusFilter, priorityFilter]);

  const createTask = async (data: CreateTaskDto) => {
    const newTask = await tasksApi.create(data);
    setTasks((prev) => [...prev, newTask]);
  };

  const updateTask = async (id: number, data: UpdateTaskDto) => {
    const updatedTask = await tasksApi.update(id, data);
    setTasks((prev) => prev.map((task) => (task.id === id ? updatedTask : task)));
  };

  const deleteTask = async (id: number) => {
    await tasksApi.delete(id);
    setTasks((prev) => prev.filter((task) => task.id !== id));
  };

  const reorderTasks = async (taskIds: number[]) => {
    // Optimistically update local state
    const reorderedTasks = taskIds
      .map((id) => tasks.find((t) => t.id === id))
      .filter((t): t is Task => t !== undefined)
      .map((t, index) => ({ ...t, sortOrder: index }));
    setTasks(reorderedTasks);

    // Persist to server
    await tasksApi.reorder(taskIds);
  };

  return (
    <TaskContext.Provider
      value={{
        tasks,
        isLoading,
        error,
        statusFilter,
        priorityFilter,
        fetchTasks,
        createTask,
        updateTask,
        deleteTask,
        reorderTasks,
        setStatusFilter,
        setPriorityFilter,
      }}
    >
      {children}
    </TaskContext.Provider>
  );
};
