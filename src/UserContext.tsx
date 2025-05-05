import React, { createContext, useContext, useEffect, useState } from 'react';
import { AccountInfo, EventType } from '@azure/msal-browser';
import { useMsal } from '@azure/msal-react';

export interface UserContextType {
  userAccount: AccountInfo | null;
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

export const UserProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { instance } = useMsal();
  const [userAccount, setUserAccount] = useState<AccountInfo | null>(null);
  const [openAIAPIKey, setOpenAIAPIKey] = useState<string>('');

  // Initial account setup
  useEffect(() => {
    const currentAccount = instance.getActiveAccount();
    if (currentAccount) {
      setUserAccount(currentAccount);
    } else {
      // If no active account, try to get the first account
      const allAccounts = instance.getAllAccounts();
      if (allAccounts.length > 0) {
        instance.setActiveAccount(allAccounts[0]);
        setUserAccount(allAccounts[0]);
      }
    }
  }, [instance]);

  // Listen for auth events
  useEffect(() => {
    const callbackId = instance.addEventCallback((event) => {
      if (
        event.eventType === EventType.LOGIN_SUCCESS ||
        event.eventType === EventType.LOGOUT_SUCCESS ||
        event.eventType === EventType.ACCOUNT_ADDED ||
        event.eventType === EventType.ACCOUNT_REMOVED
      ) {
        // Update user account state when auth events occur
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
      }
    });

    return () => {
      if (callbackId) {
        instance.removeEventCallback(callbackId);
      }
    };
  }, [instance]);

  return (
    <UserContext.Provider value={{ userAccount, openAIAPIKey, setOpenAIAPIKey }}>
      {children}
    </UserContext.Provider>
  );
};
