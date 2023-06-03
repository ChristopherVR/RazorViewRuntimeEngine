document
  .querySelector('button[data-url]')
  .addEventListener(
    'click',
    ({ currentTarget }) => (window.location.href = currentTarget.dataset.url),
  );
