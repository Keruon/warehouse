import api, { clearAuthTokens, getRefreshToken, setAuthTokens } from './api';

export type AuthUser = {
  id?: string;
  userId?: string;
  username: string;
  email?: string;
  role: string;
};

export type TokenResponse = {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc?: string;
};

export type LoginPayload = {
  user: AuthUser;
  tokens: TokenResponse;
};

type ApiEnvelope<T> = {
  data?: T;
  success?: boolean;
  message?: string;
};

function getDeviceFingerprint(): string {
  return window.navigator.userAgent;
}

function unwrap<T>(response: { data?: ApiEnvelope<T> }): T {
  if (!response?.data?.data) {
    throw new Error('Unexpected API response shape');
  }

  return response.data.data;
}

export async function login(usernameOrEmail: string, password: string): Promise<LoginPayload> {
  const response = await api.post<ApiEnvelope<LoginPayload>>('/api/auth/login', {
    usernameOrEmail,
    password,
    deviceFingerprint: getDeviceFingerprint(),
  });

  const payload = unwrap(response);
  setAuthTokens(payload.tokens);
  return payload;
}

export async function refresh(refreshToken: string = getRefreshToken() ?? ''): Promise<TokenResponse> {
  if (!refreshToken) {
    throw new Error('Missing refresh token');
  }

  const response = await api.post<ApiEnvelope<{ tokens?: TokenResponse } & TokenResponse>>('/api/auth/refresh', {
    refreshToken,
    deviceFingerprint: getDeviceFingerprint(),
  });

  const payload = unwrap(response);
  const tokens = payload.tokens ?? payload;
  setAuthTokens(tokens);
  return tokens;
}

export async function logout(refreshToken: string = getRefreshToken() ?? ''): Promise<void> {
  try {
    if (refreshToken) {
      await api.post('/api/auth/logout', {
        refreshToken,
        deviceFingerprint: getDeviceFingerprint(),
      });
    }
  } finally {
    clearAuthTokens();
  }
}

export async function getMe(): Promise<AuthUser> {
  const response = await api.get<ApiEnvelope<AuthUser>>('/api/auth/me');
  return unwrap(response);
}