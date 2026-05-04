// Shared header / navigation injected on every page.
const NAV = [
  { href: 'index.html', label: 'Home' },
  { href: 'pages/extract.html', label: 'Extract' },
  { href: 'pages/tracker.html', label: 'Notice Tracker' },
  { href: 'pages/notification.html', label: 'Notification Agent' },
  { href: 'pages/reporting.html', label: 'Reporting Agent' },
  { href: 'pages/quality.html', label: 'Quality Check' },
  { href: 'pages/correspondence.html', label: 'Correspondence Agent' },
  { href: 'pages/review.html', label: 'Human Review' }
];

function renderHeader(activeKey) {
  const inPages = location.pathname.includes('/pages/');
  const prefix = inPages ? '../' : '';
  const links = NAV.map(n => {
    const href = prefix + n.href;
    const cls = n.label === activeKey ? 'active' : '';
    return `<a class="${cls}" href="${href}">${n.label}</a>`;
  }).join('');
  document.body.insertAdjacentHTML('afterbegin', `
    <header>
      <div class="brand">Notice Intelligence</div>
      <nav>${links}</nav>
    </header>
  `);
}

function esc(v) {
  if (v == null || v === '') return '—';
  return String(v).replace(/[&<>"']/g, c => ({ '&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;' }[c]));
}
