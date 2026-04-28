// ─── Auto-dismiss flash messages ──────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.flash').forEach(el => {
        setTimeout(() => {
            el.style.transition = 'opacity .35s, transform .35s';
            el.style.opacity = '0';
            el.style.transform = 'translateY(-6px)';
            setTimeout(() => el.remove(), 360);
        }, 4000);
    });
});

// ─── Sidebar: collapse on desktop, toggle on mobile ───────────
document.addEventListener('DOMContentLoaded', () => {
    const collapseBtn = document.querySelector('[data-sidebar-toggle]');
    const openBtn     = document.querySelector('[data-sidebar-open]');
    const COLLAPSE_KEY = 'auca:sidebarCollapsed';

    if (localStorage.getItem(COLLAPSE_KEY) === '1') {
        document.body.classList.add('sidebar-collapsed');
    }

    collapseBtn?.addEventListener('click', () => {
        if (window.matchMedia('(max-width: 760px)').matches) {
            document.body.classList.toggle('sidebar-open');
        } else {
            document.body.classList.toggle('sidebar-collapsed');
            localStorage.setItem(
                COLLAPSE_KEY,
                document.body.classList.contains('sidebar-collapsed') ? '1' : '0'
            );
        }
    });

    openBtn?.addEventListener('click', () => {
        document.body.classList.toggle('sidebar-open');
    });

    // Tap outside the sidebar on mobile to close
    document.addEventListener('click', (e) => {
        if (!document.body.classList.contains('sidebar-open')) return;
        const sidebar = document.getElementById('sidebar');
        if (sidebar && !sidebar.contains(e.target) && !openBtn?.contains(e.target)) {
            document.body.classList.remove('sidebar-open');
        }
    });
});

// ─── "Currently studying" toggle on the certificate form ──────
document.addEventListener('DOMContentLoaded', () => {
    const toggle = document.getElementById('ongoingToggle');
    const input  = document.getElementById('studiedToInput');
    if (!toggle || !input) return;

    const apply = () => {
        if (toggle.checked) {
            input.dataset.previous = input.value;
            input.value = '';
            input.disabled = true;
            input.classList.add('is-disabled');
        } else {
            input.disabled = false;
            input.classList.remove('is-disabled');
            if (input.dataset.previous) input.value = input.dataset.previous;
        }
    };

    toggle.addEventListener('change', apply);
    apply();
});
