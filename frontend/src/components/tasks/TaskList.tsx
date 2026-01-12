import React from 'react';
import { DragDropContext, Droppable, Draggable, type DropResult } from '@hello-pangea/dnd';
import { useTasks } from '../../hooks/useTasks';
import TaskCard from './TaskCard';
import type { Task } from '../../types';
import './TaskList.css';

interface TaskListProps {
  onEditTask: (task: Task) => void;
}

const TaskList: React.FC<TaskListProps> = ({ onEditTask }) => {
  const { tasks, reorderTasks, isLoading, error } = useTasks();

  const handleDragEnd = (result: DropResult) => {
    if (!result.destination) return;
    if (result.source.index === result.destination.index) return;

    const reorderedIds = Array.from(tasks.map((t) => t.id));
    const [removed] = reorderedIds.splice(result.source.index, 1);
    reorderedIds.splice(result.destination.index, 0, removed);

    reorderTasks(reorderedIds);
  };

  if (isLoading) {
    return (
      <div className="task-list-loading">
        <div className="spinner"></div>
        <p>Loading tasks...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="task-list-error">
        <p>{error}</p>
      </div>
    );
  }

  if (tasks.length === 0) {
    return (
      <div className="task-list-empty">
        <p>No tasks found. Create your first task!</p>
      </div>
    );
  }

  return (
    <DragDropContext onDragEnd={handleDragEnd}>
      <Droppable droppableId="tasks">
        {(provided) => (
          <div {...provided.droppableProps} ref={provided.innerRef} className="task-list">
            {tasks.map((task, index) => (
              <Draggable key={task.id} draggableId={String(task.id)} index={index}>
                {(provided, snapshot) => (
                  <div
                    ref={provided.innerRef}
                    {...provided.draggableProps}
                    {...provided.dragHandleProps}
                    className={`task-item ${snapshot.isDragging ? 'dragging' : ''}`}
                  >
                    <TaskCard task={task} onEdit={onEditTask} />
                  </div>
                )}
              </Draggable>
            ))}
            {provided.placeholder}
          </div>
        )}
      </Droppable>
    </DragDropContext>
  );
};

export default TaskList;
