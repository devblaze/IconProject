import React, { useState, useEffect } from 'react';
import Header from '../components/layout/Header';
import TaskFilters from '../components/tasks/TaskFilters';
import TaskList from '../components/tasks/TaskList';
import TaskForm from '../components/tasks/TaskForm';
import { useTasks } from '../hooks/useTasks';
import type { Task } from '../types';
import './DashboardPage.css';

const DashboardPage: React.FC = () => {
  const { fetchTasks, statusFilter, priorityFilter } = useTasks();
  const [showForm, setShowForm] = useState(false);
  const [editingTask, setEditingTask] = useState<Task | null>(null);

  useEffect(() => {
    fetchTasks();
  }, [fetchTasks, statusFilter, priorityFilter]);

  const handleAddTask = () => {
    setEditingTask(null);
    setShowForm(true);
  };

  const handleEditTask = (task: Task) => {
    setEditingTask(task);
    setShowForm(true);
  };

  const handleCloseForm = () => {
    setShowForm(false);
    setEditingTask(null);
  };

  return (
    <div className="dashboard">
      <Header />
      <main className="dashboard-main">
        <div className="dashboard-container">
          <div className="dashboard-header">
            <h2>My Tasks</h2>
            <button onClick={handleAddTask} className="btn-add-task">
              + New Task
            </button>
          </div>
          <TaskFilters />
          <TaskList onEditTask={handleEditTask} />
        </div>
      </main>
      {showForm && <TaskForm task={editingTask} onClose={handleCloseForm} />}
    </div>
  );
};

export default DashboardPage;
