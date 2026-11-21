// Calls back when the element comes within about a screen of the view (and right away if it already is).
export function watch(element, listener) {
  const observer = new IntersectionObserver(
    (entries) => {
      if (entries.some((e) => e.isIntersecting)) {
        listener.invokeMethodAsync('OnInView');
      }
    },
    // Measured against the pane that scrolls (the books pane, a filter list), so the margin reaches ahead of it;
    // against the page when nothing scrolls in between.
    { root: scrollingParent(element), rootMargin: '0px 0px 1200px 0px' },
  );
  observer.observe(element);
  return { dispose: () => observer.disconnect() };
}

function scrollingParent(element) {
  for (let node = element.parentElement; node && node !== document.body; node = node.parentElement) {
    const overflow = getComputedStyle(node).overflowY;
    if (overflow === 'auto' || overflow === 'scroll') {
      return node;
    }
  }

  return null;
}
