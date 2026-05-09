// StyleBazar — UI behaviour
// Mobile menu, user dropdown, marker for active bottom-tab, basic wishlist heart toggle.

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        setupMobileMenu();
        setupUserDropdown();
        setupBottomBarActiveState();
        setupWishlistHearts();
    });

    /* ----------- Mobile menu ----------- */
    function setupMobileMenu() {
        var burger = document.getElementById('sbBurger');
        var menu   = document.getElementById('sbMobileMenu');
        var close  = document.getElementById('sbMenuClose');
        if (!burger || !menu) return;

        function open() {
            menu.classList.add('is-open');
            menu.setAttribute('aria-hidden', 'false');
            document.body.classList.add('sb-locked');
        }
        function shut() {
            menu.classList.remove('is-open');
            menu.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('sb-locked');
        }

        burger.addEventListener('click', open);
        if (close) close.addEventListener('click', shut);

        // Close on link click
        menu.querySelectorAll('a').forEach(function (a) {
            a.addEventListener('click', shut);
        });
        // Close on Escape
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && menu.classList.contains('is-open')) shut();
        });
    }

    /* ----------- User dropdown ----------- */
    function setupUserDropdown() {
        var toggle = document.getElementById('sbUserToggle');
        var menu   = document.getElementById('sbUserMenu');
        if (!toggle || !menu) return;

        function close() {
            menu.classList.remove('is-open');
            toggle.setAttribute('aria-expanded', 'false');
        }

        toggle.addEventListener('click', function (e) {
            e.stopPropagation();
            var open = menu.classList.toggle('is-open');
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
        });
        document.addEventListener('click', function (e) {
            if (!menu.contains(e.target) && !toggle.contains(e.target)) close();
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') close();
        });
    }

    /* ----------- Bottom bar active state ----------- */
    function setupBottomBarActiveState() {
        var path = (window.location.pathname || '/').toLowerCase();
        document.querySelectorAll('.sb-bottombar a').forEach(function (a) {
            try {
                var url = new URL(a.href, window.location.origin);
                var p = url.pathname.toLowerCase();
                if (p === '/' && path === '/') {
                    a.classList.add('is-active');
                } else if (p !== '/' && path.indexOf(p) === 0) {
                    a.classList.add('is-active');
                }
            } catch (_) { /* ignore */ }
        });
    }

    /* ----------- Wishlist hearts (visual toggle; real persistence is a later phase) ----------- */
    function setupWishlistHearts() {
        document.querySelectorAll('.sb-product__wish').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                btn.classList.toggle('is-saved');
                // TODO: hit /Account/AddToWishlist or API endpoint when wishlist UI lands.
            });
        });
    }
})();
