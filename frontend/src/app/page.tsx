import { ApplicationForm } from "@/components/ApplicationForm";

export default function HomePage() {
  return (
    <main className="mx-auto flex w-full max-w-2xl flex-1 flex-col px-4 py-12 sm:py-16">
      <div className="mb-8">
        <h1 className="text-2xl font-semibold text-gray-900 dark:text-gray-100">
          Loan application
        </h1>
        <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">
          Fill out the form below to apply. You&apos;ll hear back immediately.
        </p>
      </div>
      <ApplicationForm />
    </main>
  );
}
