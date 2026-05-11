// StyleBazar — UI behaviour
// Header scroll, mega-menus, search overlay, mobile menu+accordion,
// user dropdown, language dropdown, wishlist heart toggle.

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        setupScrollHeader();
        setupMegaMenus();
        setupSearchOverlay();
        setupMobileMenu();
        setupMobileAccordion();
        setupUserDropdown();
        setupLanguageDropdown();
        setupBottomBarActiveState();
        setupWishlistHearts();
        setupAuthForms();
    });

    /* ----------- Header scroll-shrink ----------- */
    function setupScrollHeader() {
        var header = document.getElementById('sbHeader');
        if (!header) return;
        var apply = function () {
            if (window.scrollY > 16) header.classList.add('is-scrolled');
            else header.classList.remove('is-scrolled');
        };
        apply();
        window.addEventListener('scroll', apply, { passive: true });
    }

    /* ----------- Mega-menus (hover + click) ----------- */
    function setupMegaMenus() {
        var items = document.querySelectorAll('.sb-nav__item[data-mega]');
        var backdrop = document.getElementById('sbMegaBackdrop');
        if (!items.length) return;

        var openItem = null;
        var closeTimer = null;

        function openOne(item) {
            if (openItem && openItem !== item) closeOne(openItem, true);
            clearTimeout(closeTimer);
            item.classList.add('is-open');
            var btn = item.querySelector('button.sb-nav__link');
            if (btn) btn.setAttribute('aria-expanded', 'true');
            if (backdrop) backdrop.classList.add('is-open');
            openItem = item;
        }
        function closeOne(item, immediate) {
            var doIt = function () {
                item.classList.remove('is-open');
                var btn = item.querySelector('button.sb-nav__link');
                if (btn) btn.setAttribute('aria-expanded', 'false');
                if (openItem === item) openItem = null;
                if (!document.querySelector('.sb-nav__item.is-open') && backdrop) {
                    backdrop.classList.remove('is-open');
                }
            };
            if (immediate) doIt();
            else { clearTimeout(closeTimer); closeTimer = setTimeout(doIt, 100); }
        }

        items.forEach(function (item) {
            item.addEventListener('mouseenter', function () { openOne(item); });
            item.addEventListener('mouseleave', function () { closeOne(item); });
            var btn = item.querySelector('button.sb-nav__link');
            if (btn) {
                btn.addEventListener('click', function (e) {
                    e.stopPropagation();
                    if (item.classList.contains('is-open')) closeOne(item, true);
                    else openOne(item);
                });
            }
        });

        document.addEventListener('click', function (e) {
            if (!openItem) return;
            if (!openItem.contains(e.target)) closeOne(openItem, true);
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && openItem) closeOne(openItem, true);
        });
    }

    /* ----------- Search overlay ----------- */
    function setupSearchOverlay() {
        var open = document.getElementById('sbSearchOpen');
        var close = document.getElementById('sbSearchClose');
        var overlay = document.getElementById('sbSearchOverlay');
        var input = document.getElementById('sbSearchInput');
        if (!open || !overlay) return;

        function show() {
            overlay.classList.add('is-open');
            overlay.setAttribute('aria-hidden', 'false');
            document.body.classList.add('sb-locked');
            setTimeout(function () { if (input) input.focus(); }, 200);
        }
        function hide() {
            overlay.classList.remove('is-open');
            overlay.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('sb-locked');
        }

        open.addEventListener('click', show);
        if (close) close.addEventListener('click', hide);
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && overlay.classList.contains('is-open')) hide();
        });
    }

    /* ----------- Mobile menu ----------- */
    function setupMobileMenu() {
        var burger = document.getElementById('sbBurger');
        var menu   = document.getElementById('sbMobileMenu');
        var close  = document.getElementById('sbMenuClose');
        if (!burger || !menu) return;

        function show() {
            menu.classList.add('is-open');
            menu.setAttribute('aria-hidden', 'false');
            document.body.classList.add('sb-locked');
        }
        function hide() {
            menu.classList.remove('is-open');
            menu.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('sb-locked');
        }

        burger.addEventListener('click', show);
        if (close) close.addEventListener('click', hide);
        menu.querySelectorAll('a').forEach(function (a) {
            a.addEventListener('click', hide);
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && menu.classList.contains('is-open')) hide();
        });
    }

    /* ----------- Mobile accordion ----------- */
    function setupMobileAccordion() {
        document.querySelectorAll('.sb-mobile-acc').forEach(function (acc) {
            var trigger = acc.querySelector('.sb-mobile-acc__trigger');
            if (!trigger) return;
            trigger.addEventListener('click', function () {
                acc.classList.toggle('is-open');
            });
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
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') close(); });
    }

    /* ----------- Language dropdown ----------- */
    function setupLanguageDropdown() {
        var toggle = document.getElementById('sbLangToggle');
        var menu   = document.getElementById('sbLangMenu');
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
        menu.querySelectorAll('button[data-lang]').forEach(function (b) {
            b.addEventListener('click', function () {
                menu.querySelectorAll('button').forEach(function (x) { x.classList.remove('is-active'); });
                b.classList.add('is-active');
                var lang = b.getAttribute('data-lang') || 'az';
                var flag = toggle.querySelector('.sb-lang__flag');
                if (flag) flag.className = 'sb-lang__flag sb-lang__flag--' + lang;
                var label = toggle.querySelector('span:nth-of-type(1)');
                // The label span comes after the flag; locate by text role.
                var labelSpan = toggle.querySelectorAll('span')[1];
                if (labelSpan) labelSpan.textContent = lang.toUpperCase();
                close();
            });
        });
        document.addEventListener('click', function (e) {
            if (!menu.contains(e.target) && !toggle.contains(e.target)) close();
        });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') close(); });
    }

    /* ----------- Bottom bar active state ----------- */
    function setupBottomBarActiveState() {
        var path = (window.location.pathname || '/').toLowerCase();
        document.querySelectorAll('.sb-bottombar a').forEach(function (a) {
            try {
                var url = new URL(a.href, window.location.origin);
                var p = url.pathname.toLowerCase();
                if (p === '/' && path === '/') a.classList.add('is-active');
                else if (p !== '/' && path.indexOf(p) === 0) a.classList.add('is-active');
            } catch (_) { /* ignore */ }
        });
    }

    /* ----------- Wishlist hearts ----------- */
    function setupWishlistHearts() {
        document.querySelectorAll('.sb-product__wish').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                btn.classList.toggle('is-saved');
            });
        });
    }

    /* ----------- Auth forms: password toggle, strength meter, role cards, submit loading ----------- */
    function setupAuthForms() {
        // Password show/hide
        document.querySelectorAll('[data-pwd-toggle]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var wrap = btn.closest('.sb-field__input-wrap');
                if (!wrap) return;
                var input = wrap.querySelector('input[data-pwd]');
                if (!input) return;
                var isHidden = input.type === 'password';
                input.type = isHidden ? 'text' : 'password';
                var eye = btn.querySelector('[data-eye]');
                var eyeOff = btn.querySelector('[data-eye-off]');
                if (eye && eyeOff) {
                    eye.style.display = isHidden ? 'none' : '';
                    eyeOff.style.display = isHidden ? '' : 'none';
                }
                btn.setAttribute('aria-label', isHidden ? 'Parolu gizlət' : 'Parolu göstər');
            });
        });

        // Password strength meter
        document.querySelectorAll('[data-pwd-meter]').forEach(function (input) {
            var field = input.closest('.sb-field');
            if (!field) return;
            var bars = field.querySelectorAll('[data-meter] .sb-pwd-meter__bar');
            var label = field.querySelector('[data-meter-label]');
            if (!bars.length) return;

            function score(v) {
                if (!v) return 0;
                var s = 0;
                if (v.length >= 6) s++;
                if (v.length >= 10 && /[a-z]/.test(v) && /[0-9]/.test(v)) s++;
                if (v.length >= 12 && /[A-Z]/.test(v) && /[^a-zA-Z0-9]/.test(v)) s++;
                if (v.length < 6) s = 0;
                return s; // 0..3
            }

            function render() {
                var s = score(input.value);
                var classes = ['is-weak', 'is-medium', 'is-strong'];
                bars.forEach(function (b, i) {
                    b.classList.remove('is-weak', 'is-medium', 'is-strong');
                    if (i < s) {
                        b.classList.add(s === 1 ? 'is-weak' : s === 2 ? 'is-medium' : 'is-strong');
                    }
                });
                if (label) {
                    label.classList.remove('is-weak', 'is-medium', 'is-strong');
                    if (!input.value) {
                        label.textContent = 'Parol gücü';
                    } else if (s === 0) {
                        label.textContent = 'Çox zəif — minimum 6 simvol';
                        label.classList.add('is-weak');
                    } else if (s === 1) {
                        label.textContent = 'Zəif';
                        label.classList.add('is-weak');
                    } else if (s === 2) {
                        label.textContent = 'Orta';
                        label.classList.add('is-medium');
                    } else {
                        label.textContent = 'Güclü';
                        label.classList.add('is-strong');
                    }
                }
            }

            input.addEventListener('input', render);
            render();
        });

        // Role card click — toggle hidden radio + visual state
        document.querySelectorAll('[data-role-group]').forEach(function (group) {
            var cards = group.querySelectorAll('[data-role-card]');
            cards.forEach(function (card) {
                card.addEventListener('click', function (e) {
                    // Let the native label/radio behaviour happen; we just paint
                    setTimeout(function () {
                        cards.forEach(function (c) {
                            var radio = c.querySelector('input[type="radio"]');
                            if (radio && radio.checked) c.classList.add('is-active');
                            else c.classList.remove('is-active');
                        });
                    }, 0);
                });
            });
        });

        // Submit loading
        document.querySelectorAll('[data-auth-form]').forEach(function (form) {
            form.addEventListener('submit', function () {
                var btn = form.querySelector('[data-submit]');
                if (btn) btn.classList.add('is-loading');
            });
        });
    }
})();
