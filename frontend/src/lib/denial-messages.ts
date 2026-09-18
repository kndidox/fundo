// The backend's denial `reason` is free-form text, not a closed enum — see the backend's
// ARCHITECTURE.md ("Data-driven rules"). New rules can ship reasons this map has never seen,
// so an unknown reason always falls back to a generic message instead of rendering nothing.
const DENIAL_MESSAGES: Record<string, string> = {
  StateNotAllowed:
    "We're sorry, we're not able to accept applications from that state at this time.",
  SsnBlacklisted: "We're unable to approve this application at this time.",
};

const DEFAULT_MESSAGE = "Your application was not approved at this time.";

export function denialMessage(reason: string | null | undefined): string {
  if (!reason) return DEFAULT_MESSAGE;
  return DENIAL_MESSAGES[reason] ?? DEFAULT_MESSAGE;
}
