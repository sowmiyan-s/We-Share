// ═══════════════════════════════════════════════════════════
//  WE SHARE — main.js  |  Signal & Static Design
// ═══════════════════════════════════════════════════════════


const navbar = document.getElementById('navbar');
if (navbar) {
  window.addEventListener('scroll', () => {
    navbar.classList.toggle('scrolled', window.scrollY > 20);
  }, { passive: true });
}

// ── Mobile hamburger ──
const hamburger = document.getElementById('hamburger');
const navLinks  = document.getElementById('navLinks');
if (hamburger && navLinks) {
  hamburger.addEventListener('click', () => {
    const open = navLinks.classList.toggle('open');
    const spans = hamburger.querySelectorAll('span');
    if (open) {
      spans[0].style.transform = 'rotate(45deg) translate(4.5px, 4.5px)';
      spans[1].style.opacity   = '0';
      spans[2].style.transform = 'rotate(-45deg) translate(4.5px, -4.5px)';
    } else {
      spans.forEach(s => { s.style.transform = ''; s.style.opacity = ''; });
    }
  });
  navLinks.querySelectorAll('a').forEach(a => {
    a.addEventListener('click', () => {
      navLinks.classList.remove('open');
      hamburger.querySelectorAll('span').forEach(s => { s.style.transform = ''; s.style.opacity = ''; });
    });
  });
}

// ── Active nav link ──
(function markActive() {
  const page = location.pathname.split('/').pop() || 'index.html';
  document.querySelectorAll('.nav-link').forEach(a => {
    const href = (a.getAttribute('href') || '').replace('#', '');
    const match = href === page || (page === '' && href === 'index.html');
    a.classList.toggle('active', match);
  });
})();

// ── Scroll reveal ──
const revealEls = document.querySelectorAll(
  '.feat, .how-step, .scenario, .patch-entry, .step-item, .faq-item, .howto-block, .version-row, .reveal'
);
if (revealEls.length) {
  const revealObs = new IntersectionObserver(entries => {
    entries.forEach(e => {
      if (!e.isIntersecting) return;
      const siblings = [...(e.target.parentElement?.children || [])];
      const i = siblings.indexOf(e.target);
      setTimeout(() => e.target.classList.add('visible'), Math.min(i, 5) * 90);
      revealObs.unobserve(e.target);
    });
  }, { threshold: 0.06, rootMargin: '0px 0px -30px 0px' });

  revealEls.forEach(el => {
    el.classList.add('reveal');
    revealObs.observe(el);
  });
}

// ── FAQ accordion ──
document.querySelectorAll('.faq-question').forEach(btn => {
  btn.addEventListener('click', () => {
    const item    = btn.closest('.faq-item');
    const wasOpen = item.classList.contains('open');
    document.querySelectorAll('.faq-item.open').forEach(i => {
      i.classList.remove('open');
      i.querySelector('.faq-question')?.setAttribute('aria-expanded', 'false');
    });
    if (!wasOpen) {
      item.classList.add('open');
      btn.setAttribute('aria-expanded', 'true');
    }
  });
});

// ── How-to-use tab highlight on scroll ──
const howtoSections = ['pc-to-pc','pc-to-mobile','mobile-to-pc','desert-mode'];
const tabMap = { 'pc-to-pc':'tab-pc-to-pc','pc-to-mobile':'tab-pc-to-mobile','mobile-to-pc':'tab-mobile-to-pc','desert-mode':'tab-desert' };
if (howtoSections.some(id => document.getElementById(id))) {
  const obs2 = new IntersectionObserver(entries => {
    entries.forEach(e => {
      if (!e.isIntersecting) return;
      document.querySelectorAll('.howto-tab').forEach(t => t.classList.remove('active'));
      const tab = document.getElementById(tabMap[e.target.id]);
      if (tab) tab.classList.add('active');
    });
  }, { threshold: 0.35 });
  howtoSections.forEach(id => { const el = document.getElementById(id); if (el) obs2.observe(el); });
}

// ── Typewriter effect on terminal (index page only) ──
const terminalLines = document.querySelectorAll('.terminal-body .t-line');
if (terminalLines.length) {
  terminalLines.forEach((line, i) => {
    line.style.opacity = '0';
    line.style.transform = 'translateY(4px)';
    line.style.transition = 'opacity 0.3s, transform 0.3s';
    setTimeout(() => {
      line.style.opacity = '1';
      line.style.transform = 'none';
    }, 600 + i * 200);
  });
}
