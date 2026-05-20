import React from "react";
import GridShape from "../../components/common/GridShape";
import { Link } from "react-router";
import ThemeTogglerTwo from "../../components/common/ThemeTogglerTwo";

export default function AuthLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="relative min-h-screen overflow-x-hidden bg-brand-950 p-4 dark:bg-brand-950 sm:p-6">
      <GridShape />
      <Link
        to="/"
        className="absolute left-6 top-6 z-20 text-2xl font-semibold text-white sm:left-8 sm:top-8"
      >
        SmartParking
      </Link>

      <div className="relative z-10 flex min-h-[calc(100vh-2rem)] items-center justify-center py-16 sm:min-h-[calc(100vh-3rem)]">
        <div className="w-full max-w-xl rounded-2xl bg-white p-6 shadow-2xl dark:bg-gray-900 sm:p-8">
          {children}
        </div>
      </div>

      <div className="fixed z-50 hidden bottom-6 right-6 sm:block">
        <ThemeTogglerTwo />
      </div>
    </div>
  );
}
