import './globals.css';
import type { ReactNode } from 'react';

export const metadata = {
  title: 'DevOps Loop Tracker',
  description: 'Realtime issue -> PR -> workflow tracking dashboard'
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
