import React, { createContext, useState, useEffect, type ReactNode } from 'react';
import { authApi } from '../api/auth';
import type { User, LoginDto, RegisterDto } from '../types';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (data: LoginDto) => Promise<void>;
  register: (data: RegisterDto) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('token');
    const storedUser = localStorage.getItem('user');

    if (token && storedUser) {
      try {
        setUser(JSON.parse(storedUser));
      } catch {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
      }
    }
    setIsLoading(false);
  }, []);

  const login = async (data: LoginDto) => {
    const response = await authApi.login(data);
    localStorage.setItem('token', response.token);
    const userData: User = {
      email: response.email,
      firstName: response.firstName,
      lastName: response.lastName,
    };
    localStorage.setItem('user', JSON.stringify(userData));
    setUser(userData);
  };

  const register = async (data: RegisterDto) => {
    const response = await authApi.register(data);
    localStorage.setItem('token', response.token);
    const userData: User = {
      email: response.email,
      firstName: response.firstName,
      lastName: response.lastName,
    };
    localStorage.setItem('user', JSON.stringify(userData));
    setUser(userData);
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
