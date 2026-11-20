// One query for the module's lifetime, so the browser keeps it (and its listeners) alive.
const darkQuery = window.matchMedia('(prefers-color-scheme: dark)');

export function prefersDark() {
  return darkQuery.matches;
}

export function listen(listener) {
  const handler = (e) => listener.invokeMethodAsync('OnSystemThemeChanged', e.matches);
  darkQuery.addEventListener('change', handler);
  return { dispose: () => darkQuery.removeEventListener('change', handler) };
}
