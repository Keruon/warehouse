import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { AuthUser, getMe, login as loginRequest, LoginPayload, logout as logoutRequest } from '../services/authService';
import { getAccessToken } from '../services/api';

type AuthContextValue = {
  currentUser: AuthUser | null;
  accessToken: string | null;
  loading: boolean;
  login: (usernameOrEmail: string, password: string) => Promise<LoginPayload>;
  logout: () => Promise<void>;
  isAdmin: boolean;
  isReadOnly: boolean;
};

const AuthContext = createContext<AuthContextValue | null>(null);

function normalizeRole(role: string | undefined): string {
  return String(role ?? '').toLowerCase();
}

type AuthProviderProps = {
  children: React.ReactNode;
};

export function AuthProvider({ children }: AuthProviderProps): React.ReactElement {
  const [currentUser, setCurrentUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    let isMounted = true;

    async function bootstrapAuth(): Promise<void> {
      const accessToken = getAccessToken();

      if (!accessToken) {
        if (isMounted) {
          setLoading(false);
        }
        return;
      }

      try {
        const me = await getMe();

        if (isMounted) {
          setCurrentUser(me);
        }
      } catch (_error) {
        if (isMounted) {
          setCurrentUser(null);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    }

    bootstrapAuth();

    return () => {
      isMounted = false;
    };
  }, []);

  const login = useCallback(async (usernameOrEmail: string, password: string) => {
    const payload = await loginRequest(usernameOrEmail, password);
    setCurrentUser(payload.user);
    return payload;
  }, []);

  const logout = useCallback(async () => {
    await logoutRequest();
    setCurrentUser(null);
  }, []);

  const accessToken = getAccessToken();
  const role = normalizeRole(currentUser?.role);

  const contextValue = useMemo<AuthContextValue>(
    () => ({
      currentUser,
      accessToken,
      loading,
      login,
      logout,
      isAdmin: role === 'admin',
      isReadOnly: role === 'readonly',
    }),
    [accessToken, currentUser, loading, login, logout, role]
  );

  return <AuthContext.Provider value={contextValue}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }

  return context;
}