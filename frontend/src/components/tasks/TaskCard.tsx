import React, { useState } from 'react';
import { useTasks } from '../../hooks/useTasks';
import { Priority, type Task } from '../../types';
import './TaskCard.css';

interface TaskCardProps {
  task: Task;
  onEdit: (task: Task) => void;
}

const priorityLabels: Record<Priority, string> = {
  [Priority.Low]: 'Low',
  [Priority.Medium]: 'Medium',
  [Priority.High]: 'High',
};

const priorityColors: Record<Priority, string> = {
  [Priority.Low]: '#4caf50',
  [Priority.Medium]: '#ff9800',
  [Priority.High]: '#f44336',
};

const TaskCard: React.FC<TaskCardProps> = ({ task, onEdit }) => {
  const { updateTask, deleteTask } = useTasks();
  const [isDeleting, setIsDeleting] = useState(false);

  const handleToggleComplete = async () => {
    await updateTask(task.id, {
      title: task.title,
      description: task.description,
      isComplete: !task.isComplete,
      priority: task.priority,
    });
  };

  const handleDelete = async () => {
    if (window.confirm('Are you sure you want to delete this task?')) {
      setIsDeleting(true);
      try {
        await deleteTask(task.id);
      } finally {
        setIsDeleting(false);
      }
    }
  };

  return (
    <div className={`task-card ${task.isComplete ? 'completed' : ''}`}>
      <div className="task-card-left">
        <input
          type="checkbox"
          checked={task.isComplete}
          onChange={handleToggleComplete}
          className="task-checkbox"
        />
        <div className="task-content">
          <h3 className="task-title">{task.title}</h3>
          {task.description && <p className="task-description">{task.description}</p>}
        </div>
      </div>
      <div className="task-card-right">
        <span
          className="priority-badge"
          style={{ backgroundColor: priorityColors[task.priority] }}
        >
          {priorityLabels[task.priority]}
        </span>
        <div className="task-actions">
          <button onClick={() => onEdit(task)} className="btn-edit" title="Edit">
            Edit
          </button>
          <button
            onClick={handleDelete}
            className="btn-delete"
            disabled={isDeleting}
            title="Delete"
          >
            {isDeleting ? '...' : 'Delete'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default TaskCard;
