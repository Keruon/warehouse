import axios, { AxiosError, AxiosHeaders, AxiosInstance, AxiosRequestConfig } from 'axios';

export const ACCESS_TOKEN_KEY = 'accessToken';
export const REFRESH_TOKEN_KEY = 'refreshToken';

type TokenPair = {
  accessToken: string;
  refreshToken: string;
};

type RefreshResponseEnvelope = {
  data?: {
    tokens?: TokenPair;
    accessToken?: string;
    refreshToken?: string;
  };
};

type RetriableRequestConfig = AxiosRequestConfig & { _retryAfterRefresh?: boolean };

let refreshPromise: Promise<string> | null = null;

export function getAccessToken(): string | null {
  return localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_KEY);
}

export function setAuthTokens(tokens: Partial<TokenPair> | null | undefined): void {
  if (tokens?.accessToken) {
    localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
  }

  if (tokens?.refreshToken) {
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
  }
}

export function clearAuthTokens(): void {
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
}

function getDeviceFingerprint(): string {
  return window.navigator.userAgent;
}

function getRefreshClient(): AxiosInstance {
  return axios.create();
}

async function refreshAccessToken(): Promise<string> {
  if (!refreshPromise) {
    const refreshToken = getRefreshToken();

    if (!refreshToken) {
      throw new Error('Missing refresh token');
    }

    refreshPromise = getRefreshClient()
      .post<RefreshResponseEnvelope>('/api/auth/refresh', {
        refreshToken,
        deviceFingerprint: getDeviceFingerprint(),
      })
      .then((response) => {
        const payload = response.data?.data;
        const tokens = payload?.tokens ?? {
          accessToken: payload?.accessToken ?? '',
          refreshToken: payload?.refreshToken ?? '',
        };

        setAuthTokens(tokens);

        if (!tokens.accessToken) {
          throw new Error('No access token returned from refresh endpoint');
        }

        return tokens.accessToken;
      })
      .finally(() => {
        refreshPromise = null;
      });
  }

  return refreshPromise;
}

const api = axios.create();

api.interceptors.request.use((config) => {
  const accessToken = getAccessToken();

  if (accessToken) {
    if (!config.headers) {
      config.headers = new AxiosHeaders();
    }

    if (config.headers instanceof AxiosHeaders) {
      config.headers.set('Authorization', `Bearer ${accessToken}`);
    } else {
      (config.headers as Record<string, string>).Authorization = `Bearer ${accessToken}`;
    }
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetriableRequestConfig | undefined;
    const statusCode = error.response?.status;

    if (!originalRequest || statusCode !== 401 || originalRequest._retryAfterRefresh) {
      return Promise.reject(error);
    }

    if (!getRefreshToken()) {
      clearAuthTokens();
      window.location.assign('/login');
      return Promise.reject(error);
    }

    try {
      originalRequest._retryAfterRefresh = true;
      const newAccessToken = await refreshAccessToken();

      if (!originalRequest.headers) {
        originalRequest.headers = new AxiosHeaders();
      }

      if (originalRequest.headers instanceof AxiosHeaders) {
        originalRequest.headers.set('Authorization', `Bearer ${newAccessToken}`);
      } else {
        (originalRequest.headers as Record<string, string>).Authorization = `Bearer ${newAccessToken}`;
      }

      return api(originalRequest);
    } catch (refreshError) {
      clearAuthTokens();
      window.location.assign('/login');
      return Promise.reject(refreshError);
    }
  }
);

export default api;