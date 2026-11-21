// Calls back when the element comes within about a screen of the view (and right away if it already is).
export function watch(element, listener) {
  const observer = new IntersectionObserver(
    (entries) => {
      if (entries.some((e) => e.isIntersecting)) {
        listener.invokeMethodAsync('OnInView');
      }
    },
    { rootMargin: '0px 0px 1200px 0px' },
  );
  observer.observe(element);
  return { dispose: () => observer.disconnect() };
}
