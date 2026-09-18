"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { denialMessage } from "@/lib/denial-messages";

export function DeniedContent() {
  const params = useSearchParams();
  const reason = params.get("reason");

  return (
    <>
      <div className="mb-4 text-4xl">✖️</div>
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-gray-100">
        Application not approved
      </h1>
      <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">{denialMessage(reason)}</p>
      <Link
        href="/"
        className="mt-8 inline-block rounded-md bg-gray-900 px-4 py-2 text-sm font-medium text-white transition hover:bg-gray-700 dark:bg-white dark:text-gray-900 dark:hover:bg-gray-200"
      >
        Back to the form
      </Link>
    </>
  );
}
