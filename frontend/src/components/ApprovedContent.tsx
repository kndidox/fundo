"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";

export function ApprovedContent() {
  const params = useSearchParams();
  const applicationId = params.get("applicationId");
  const customerId = params.get("customerId");

  return (
    <>
      <div className="mb-4 text-4xl">✅</div>
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-gray-100">
        Application approved
      </h1>
      <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">
        Thanks — your application has been approved. We&apos;ll be in touch with next steps.
      </p>
      {applicationId && (
        <dl className="mt-6 space-y-1 text-left text-sm text-gray-600 dark:text-gray-400">
          <div className="flex justify-between gap-4">
            <dt className="font-medium">Application ID</dt>
            <dd className="font-mono">{applicationId}</dd>
          </div>
          {customerId && (
            <div className="flex justify-between gap-4">
              <dt className="font-medium">Customer ID</dt>
              <dd className="font-mono">{customerId}</dd>
            </div>
          )}
        </dl>
      )}
      <Link
        href="/"
        className="mt-8 inline-block rounded-md bg-gray-900 px-4 py-2 text-sm font-medium text-white transition hover:bg-gray-700 dark:bg-white dark:text-gray-900 dark:hover:bg-gray-200"
      >
        Submit another application
      </Link>
    </>
  );
}
