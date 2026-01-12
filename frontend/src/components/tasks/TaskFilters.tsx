import React from 'react';
import { useTasks } from '../../hooks/useTasks';
import { Priority, type TaskFilter } from '../../types';
import './TaskFilters.css';

const TaskFilters: React.FC = () => {
  const { statusFilter, priorityFilter, setStatusFilter, setPriorityFilter } = useTasks();

  const statusOptions: { value: TaskFilter; label: string }[] = [
    { value: 'all', label: 'All' },
    { value: 'incomplete', label: 'Active' },
    { value: 'completed', label: 'Completed' },
  ];

  const priorityOptions: { value: Priority | null; label: string }[] = [
    { value: null, label: 'All Priorities' },
    { value: Priority.High, label: 'High' },
    { value: Priority.Medium, label: 'Medium' },
    { value: Priority.Low, label: 'Low' },
  ];

  return (
    <div className="task-filters">
      <div className="filter-group">
        <label>Status:</label>
        <div className="filter-buttons">
          {statusOptions.map((option) => (
            <button
              key={option.value}
              className={`filter-btn ${statusFilter === option.value ? 'active' : ''}`}
              onClick={() => setStatusFilter(option.value)}
            >
              {option.label}
            </button>
          ))}
        </div>
      </div>
      <div className="filter-group">
        <label>Priority:</label>
        <select
          value={priorityFilter === null ? '' : priorityFilter}
          onChange={(e) =>
            setPriorityFilter(e.target.value === '' ? null : (Number(e.target.value) as Priority))
          }
          className="filter-select"
        >
          {priorityOptions.map((option) => (
            <option key={option.label} value={option.value === null ? '' : option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
};

export default TaskFilters;
