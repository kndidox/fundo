"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { ApiError, submitApplication } from "@/lib/api";
import { US_STATES } from "@/lib/us-states";

type FormState = {
  firstName: string;
  lastName: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  state: string;
  zipCode: string;
  companyName: string;
  ssn: string;
  requestedAmount: string;
};

const INITIAL_STATE: FormState = {
  firstName: "",
  lastName: "",
  addressLine1: "",
  addressLine2: "",
  city: "",
  state: "",
  zipCode: "",
  companyName: "",
  ssn: "",
  requestedAmount: "",
};

type FieldErrors = Partial<Record<keyof FormState, string>>;

const SSN_PATTERN = /^\d{3}-?\d{2}-?\d{4}$/;
const ZIP_PATTERN = /^\d{5}(-\d{4})?$/;

function validate(form: FormState): FieldErrors {
  const errors: FieldErrors = {};

  if (!form.firstName.trim()) errors.firstName = "Required";
  if (!form.lastName.trim()) errors.lastName = "Required";
  if (!form.addressLine1.trim()) errors.addressLine1 = "Required";
  if (!form.city.trim()) errors.city = "Required";
  if (!form.state) errors.state = "Required";
  if (!ZIP_PATTERN.test(form.zipCode.trim())) errors.zipCode = "Enter a valid ZIP code";
  if (!form.companyName.trim()) errors.companyName = "Required";
  if (!SSN_PATTERN.test(form.ssn.trim())) errors.ssn = "Format: 123-45-6789";

  const amount = Number(form.requestedAmount);
  if (!form.requestedAmount || Number.isNaN(amount) || amount <= 0) {
    errors.requestedAmount = "Enter an amount greater than 0";
  }

  return errors;
}

export function ApplicationForm() {
  const router = useRouter();
  const [form, setForm] = useState<FormState>(INITIAL_STATE);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function updateField<K extends keyof FormState>(field: K, value: FormState[K]) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSubmitError(null);

    const validationErrors = validate(form);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setIsSubmitting(true);
    try {
      const result = await submitApplication({
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        addressLine1: form.addressLine1.trim(),
        addressLine2: form.addressLine2.trim() || undefined,
        city: form.city.trim(),
        state: form.state,
        zipCode: form.zipCode.trim(),
        companyName: form.companyName.trim(),
        ssn: form.ssn.trim(),
        requestedAmount: Number(form.requestedAmount),
      });

      if (result.status === "Approved") {
        const params = new URLSearchParams({
          applicationId: result.applicationId ?? "",
          customerId: result.customerId ?? "",
        });
        router.push(`/approved?${params.toString()}`);
      } else {
        const params = new URLSearchParams({ reason: result.reason ?? "" });
        router.push(`/denied?${params.toString()}`);
      }
    } catch (error) {
      setSubmitError(
        error instanceof ApiError
          ? error.message
          : "Something went wrong. Please try again.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-6">
      {submitError && (
        <div
          role="alert"
          className="rounded-md border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-800 dark:border-red-900 dark:bg-red-950 dark:text-red-200"
        >
          {submitError}
        </div>
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Field label="First name" error={errors.firstName}>
          <input
            className={inputClass(errors.firstName)}
            value={form.firstName}
            onChange={(e) => updateField("firstName", e.target.value)}
            autoComplete="given-name"
          />
        </Field>
        <Field label="Last name" error={errors.lastName}>
          <input
            className={inputClass(errors.lastName)}
            value={form.lastName}
            onChange={(e) => updateField("lastName", e.target.value)}
            autoComplete="family-name"
          />
        </Field>
      </div>

      <Field label="Address line 1" error={errors.addressLine1}>
        <input
          className={inputClass(errors.addressLine1)}
          value={form.addressLine1}
          onChange={(e) => updateField("addressLine1", e.target.value)}
          autoComplete="address-line1"
        />
      </Field>

      <Field label="Address line 2 (optional)">
        <input
          className={inputClass()}
          value={form.addressLine2}
          onChange={(e) => updateField("addressLine2", e.target.value)}
          autoComplete="address-line2"
        />
      </Field>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-[2fr_1fr_1fr]">
        <Field label="City" error={errors.city}>
          <input
            className={inputClass(errors.city)}
            value={form.city}
            onChange={(e) => updateField("city", e.target.value)}
            autoComplete="address-level2"
          />
        </Field>
        <Field label="State" error={errors.state}>
          <select
            className={inputClass(errors.state)}
            value={form.state}
            onChange={(e) => updateField("state", e.target.value)}
            autoComplete="address-level1"
          >
            <option value="">Select…</option>
            {US_STATES.map(([code, name]) => (
              <option key={code} value={code}>
                {name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="ZIP code" error={errors.zipCode}>
          <input
            className={inputClass(errors.zipCode)}
            value={form.zipCode}
            onChange={(e) => updateField("zipCode", e.target.value)}
            autoComplete="postal-code"
            inputMode="numeric"
          />
        </Field>
      </div>

      <Field label="Company name" error={errors.companyName}>
        <input
          className={inputClass(errors.companyName)}
          value={form.companyName}
          onChange={(e) => updateField("companyName", e.target.value)}
          autoComplete="organization"
        />
      </Field>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Field label="SSN" error={errors.ssn} hint="Format: 123-45-6789">
          <input
            className={inputClass(errors.ssn)}
            value={form.ssn}
            onChange={(e) => updateField("ssn", e.target.value)}
            placeholder="123-45-6789"
            inputMode="numeric"
          />
        </Field>
        <Field label="Requested amount" error={errors.requestedAmount}>
          <div className="relative">
            <span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-gray-500 dark:text-gray-400">
              $
            </span>
            <input
              className={`${inputClass(errors.requestedAmount)} pl-7`}
              value={form.requestedAmount}
              onChange={(e) => updateField("requestedAmount", e.target.value)}
              inputMode="decimal"
              placeholder="5000"
            />
          </div>
        </Field>
      </div>

      <button
        type="submit"
        disabled={isSubmitting}
        className="w-full rounded-md bg-gray-900 px-4 py-2.5 font-medium text-white transition hover:bg-gray-700 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-white dark:text-gray-900 dark:hover:bg-gray-200"
      >
        {isSubmitting ? "Submitting…" : "Submit application"}
      </button>
    </form>
  );
}

function inputClass(error?: string) {
  return [
    "w-full rounded-md border bg-white px-3 py-2 text-sm text-gray-900 shadow-sm outline-none transition",
    "focus:ring-2 focus:ring-gray-900 dark:bg-gray-900 dark:text-gray-100 dark:focus:ring-white",
    error ? "border-red-400" : "border-gray-300 dark:border-gray-700",
  ].join(" ");
}

function Field({
  label,
  error,
  hint,
  children,
}: {
  label: string;
  error?: string;
  hint?: string;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
        {label}
      </span>
      {children}
      {error ? (
        <span className="mt-1 block text-xs text-red-600 dark:text-red-400">{error}</span>
      ) : hint ? (
        <span className="mt-1 block text-xs text-gray-500 dark:text-gray-400">{hint}</span>
      ) : null}
    </label>
  );
}
