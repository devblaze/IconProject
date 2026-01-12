import api from './axios';
import type { AuthResponse, BackendAuthResponse, LoginDto, RegisterDto } from '../types';

// Transform backend response to internal format
const transformAuthResponse = (response: BackendAuthResponse): AuthResponse => ({
  token: response.accessToken,
  user: response.user,
  expiresIn: response.expiresIn,
});

export const authApi = {
  login: async (data: LoginDto): Promise<AuthResponse> => {
    const response = await api.post<BackendAuthResponse>('/auth/login', data);
    return transformAuthResponse(response.data);
  },

  register: async (data: RegisterDto): Promise<AuthResponse> => {
    const response = await api.post<BackendAuthResponse>('/auth/register', data);
    return transformAuthResponse(response.data);
  },

  getCurrentUser: async (): Promise<AuthResponse['user']> => {
    const response = await api.get<AuthResponse['user']>('/auth/me');
    return response.data;
  },
};
