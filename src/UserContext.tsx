import React, { createContext, useContext, useEffect, useState } from 'react';
import { AccountInfo, EventType } from '@azure/msal-browser';
import { useMsal } from '@azure/msal-react';
import { GetUserInfo } from './CallAPI';
import { User } from './User';

export interface UserContextType {
  userAccount: AccountInfo | null;
  user: User | undefined;
  userLoading: boolean;
  userError: string;
  openAIAPIKey: string;
  setOpenAIAPIKey: (key: string) => void;
}

export const UserContext = createContext<UserContextType | undefined>(undefined);

export const useUserAccount = () => {
  const context = useContext(UserContext);
  if (!context) {
    throw new Error('useUserAccount must be used within a UserProvider');
  }
  return context.userAccount;
};

export const useUser = () => {
  const context = useContext(UserContext);
  if (!context) {
    throw new Error('useUser must be used within a UserProvider');
  }
  return { user: context.user, loading: context.userLoading, error: context.userError };
};

export const UserProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { instance } = useMsal();
  const [userAccount, setUserAccount] = useState<AccountInfo | null>(null);
  const [openAIAPIKey, setOpenAIAPIKey] = useState<string>('');
  const [user, setUser] = useState<User | undefined>(undefined);
  const [userLoading, setUserLoading] = useState<boolean>(false);
  const [userError, setUserError] = useState<string>('');

  // Helper to fetch user info
  const fetchUserInfo = () => {
    setUserLoading(true);
    setUserError('');
    GetUserInfo(instance)
      .then(u => {
        setUser(u);
        setUserLoading(false);
      })
      .catch(e => {
        setUser(undefined);
        setUserError(e.message || 'Failed to fetch user info');
        setUserLoading(false);
      });
  };

  // Initial account setup.
  useEffect(() => {
    const currentAccount = instance.getActiveAccount();
    if (currentAccount) {
      setUserAccount(currentAccount);
    } else {
      const allAccounts = instance.getAllAccounts();
      if (allAccounts.length > 0) {
        instance.setActiveAccount(allAccounts[0]);
        setUserAccount(allAccounts[0]);
      }
    }
    // Always fetch user info on mount.
    fetchUserInfo();
  }, [instance]);

  // Listen for auth events.
  useEffect(() => {
    const callbackId = instance.addEventCallback((event) => {
      if (
        event.eventType === EventType.LOGIN_SUCCESS ||
        event.eventType === EventType.LOGOUT_SUCCESS ||
        event.eventType === EventType.ACCOUNT_ADDED ||
        event.eventType === EventType.ACCOUNT_REMOVED
      ) {
        const currentAccount = instance.getActiveAccount();
        if (currentAccount) {
          setUserAccount(currentAccount);
        } else {
          const allAccounts = instance.getAllAccounts();
          if (allAccounts.length > 0) {
            instance.setActiveAccount(allAccounts[0]);
            setUserAccount(allAccounts[0]);
          } else {
            setUserAccount(null);
          }
        }
        // Fetch user info on auth event.
        fetchUserInfo();
      }
    });

    return () => {
      if (callbackId) {
        instance.removeEventCallback(callbackId);
      }
    };
  }, [instance]);

  return (
    <UserContext.Provider value={{ userAccount, user, userLoading, userError, openAIAPIKey, setOpenAIAPIKey }}>
      {children}
    </UserContext.Provider>
  );
};
