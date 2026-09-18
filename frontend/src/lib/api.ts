export type SubmitApplicationRequest = {
  firstName: string;
  lastName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state: string;
  zipCode: string;
  companyName: string;
  ssn: string;
  requestedAmount: number;
};

export type SubmitApplicationResponse = {
  status: "Approved" | "Denied";
  reason?: string | null;
  applicationId?: string | null;
  customerId?: string | null;
};

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5200";

export class ApiError extends Error {
  constructor(
    message: string,
    public status?: number,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export async function submitApplication(
  payload: SubmitApplicationRequest,
): Promise<SubmitApplicationResponse> {
  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}/api/applications`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
  } catch {
    throw new ApiError(
      "Could not reach the application service. Is it running?",
    );
  }

  if (!response.ok) {
    throw new ApiError(
      "The application service rejected the request. Please check your information and try again.",
      response.status,
    );
  }

  return (await response.json()) as SubmitApplicationResponse;
}
