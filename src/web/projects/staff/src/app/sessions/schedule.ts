/** The value of a `datetime-local` input for an ISO timestamp, in the browser's time zone; empty for none. */
export function toLocalInput(timestamp: string | null | undefined): string {
  if (!timestamp) {
    return '';
  }
  const date = new Date(timestamp);
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
}

/** The ISO timestamp for a `datetime-local` input's value; `null` when it is empty or invalid. */
export function fromLocalInput(value: string): string | null {
  if (!value) {
    return null;
  }
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}
