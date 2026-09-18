import { Suspense } from "react";
import { ApprovedContent } from "@/components/ApprovedContent";

export default function ApprovedPage() {
  return (
    <main className="mx-auto flex w-full max-w-2xl flex-1 flex-col items-center justify-center px-4 py-12 text-center">
      <Suspense fallback={null}>
        <ApprovedContent />
      </Suspense>
    </main>
  );
}
