import React from 'react';
import { useAuth } from '../../hooks/useAuth';
import { useTheme } from '../../hooks/useTheme';
import './Header.css';

const Header: React.FC = () => {
  const { user, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();

  return (
    <header className="header">
      <div className="header-brand">
        <h1>Task Manager</h1>
      </div>
      <div className="header-user">
        <span className="user-name">
          {user?.firstName || user?.email}
        </span>
        <button
          onClick={toggleTheme}
          className="btn btn-theme"
          aria-label={`Switch to ${theme === 'light' ? 'dark' : 'light'} mode`}
          title={`Switch to ${theme === 'light' ? 'dark' : 'light'} mode`}
        >
          {theme === 'light' ? '🌙' : '☀️'}
        </button>
        <button onClick={logout} className="btn btn-logout">
          Logout
        </button>
      </div>
    </header>
  );
};

export default Header;
