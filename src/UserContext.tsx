import React, { createContext, useContext, useEffect, useState } from 'react';
import { AccountInfo } from '@azure/msal-browser';
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

  useEffect(() => {
    const account = instance.getActiveAccount();
    setUserAccount(account);
  }, [instance]);

  return (
    <UserContext.Provider value={{ userAccount, openAIAPIKey, setOpenAIAPIKey }}>
      {children}
    </UserContext.Provider>
  );
};
