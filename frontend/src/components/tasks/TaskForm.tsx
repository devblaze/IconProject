import React, { useState, useEffect } from 'react';
import { useTasks } from '../../hooks/useTasks';
import { Priority, type Task } from '../../types';
import './TaskForm.css';

interface TaskFormProps {
  task?: Task | null;
  onClose: () => void;
}

const TaskForm: React.FC<TaskFormProps> = ({ task, onClose }) => {
  const { createTask, updateTask } = useTasks();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<Priority>(Priority.Medium);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isEditing = !!task;

  useEffect(() => {
    if (task) {
      setTitle(task.title);
      setDescription(task.description || '');
      setPriority(task.priority);
    }
  }, [task]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!title.trim()) {
      setError('Title is required');
      return;
    }

    setIsSubmitting(true);
    try {
      if (isEditing && task) {
        await updateTask(task.id, {
          title: title.trim(),
          description: description.trim() || undefined,
          isComplete: task.isComplete,
          priority,
        });
      } else {
        await createTask({
          title: title.trim(),
          description: description.trim() || undefined,
          priority,
        });
      }
      onClose();
    } catch {
      setError('Failed to save task. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{isEditing ? 'Edit Task' : 'New Task'}</h2>
          <button onClick={onClose} className="modal-close">
            &times;
          </button>
        </div>
        <form onSubmit={handleSubmit}>
          {error && <div className="form-error">{error}</div>}
          <div className="form-group">
            <label htmlFor="title">Title *</label>
            <input
              id="title"
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Enter task title"
              maxLength={200}
              autoFocus
            />
          </div>
          <div className="form-group">
            <label htmlFor="description">Description</label>
            <textarea
              id="description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Enter task description (optional)"
              rows={3}
              maxLength={2000}
            />
          </div>
          <div className="form-group">
            <label htmlFor="priority">Priority</label>
            <select
              id="priority"
              value={priority}
              onChange={(e) => setPriority(Number(e.target.value) as Priority)}
            >
              <option value={Priority.Low}>Low</option>
              <option value={Priority.Medium}>Medium</option>
              <option value={Priority.High}>High</option>
            </select>
          </div>
          <div className="form-actions">
            <button type="button" onClick={onClose} className="btn-cancel">
              Cancel
            </button>
            <button type="submit" className="btn-submit" disabled={isSubmitting}>
              {isSubmitting ? 'Saving...' : isEditing ? 'Update' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default TaskForm;
