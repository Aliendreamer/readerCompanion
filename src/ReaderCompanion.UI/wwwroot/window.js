// F11 and Escape for the window frame, wherever the focus is.
export function listen(listener) {
  const handler = (e) => {
    if (e.key === 'F11' || e.key === 'Escape') {
      if (e.key === 'F11') {
        e.preventDefault();
      }
      listener.invokeMethodAsync('OnWindowKey', e.key);
    }
  };
  document.addEventListener('keydown', handler);
  return { dispose: () => document.removeEventListener('keydown', handler) };
}
