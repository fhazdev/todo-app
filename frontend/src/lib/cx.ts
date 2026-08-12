/**
 * Joins class names, dropping anything falsy. Small enough not to warrant a
 * dependency, and every component in the app composes classes this way.
 */
export function cx(...values: Array<string | false | null | undefined>): string {
  return values.filter(Boolean).join(' ')
}
