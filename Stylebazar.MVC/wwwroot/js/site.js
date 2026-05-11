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
        setupHeroCarousel();
        setupProductRails();
        setupStoryViewer();
        setupNewsletterForm();
        setupRevealOnScroll();
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

    /* ----------- Hero carousel ----------- */
    function setupHeroCarousel() {
        var root = document.getElementById('sbHeroCarousel');
        if (!root) return;
        var slides = root.querySelectorAll('.sb-hero-slide');
        if (slides.length < 2) return;
        var dots = root.querySelectorAll('[data-hero-dot]');
        var prevBtn = root.querySelector('[data-hero-prev]');
        var nextBtn = root.querySelector('[data-hero-next]');
        var autoplayMs = parseInt(root.getAttribute('data-autoplay') || '6000', 10);
        var current = 0;
        var timer = null;

        function show(i) {
            slides.forEach(function (s) { s.classList.remove('is-active'); });
            dots.forEach(function (d) { d.classList.remove('is-active'); });
            slides[i].classList.add('is-active');
            if (dots[i]) dots[i].classList.add('is-active');
            current = i;
        }
        function next() { show((current + 1) % slides.length); }
        function prev() { show((current - 1 + slides.length) % slides.length); }
        function start() { stop(); timer = setInterval(next, autoplayMs); }
        function stop() { if (timer) clearInterval(timer); timer = null; }

        if (prevBtn) prevBtn.addEventListener('click', function () { prev(); start(); });
        if (nextBtn) nextBtn.addEventListener('click', function () { next(); start(); });
        dots.forEach(function (d, i) {
            d.addEventListener('click', function () { show(i); start(); });
        });

        root.addEventListener('mouseenter', stop);
        root.addEventListener('mouseleave', start);

        start();
    }

    /* ----------- Product rails (arrow scroll) ----------- */
    function setupProductRails() {
        document.querySelectorAll('[data-rail]').forEach(function (rail) {
            var key = rail.getAttribute('data-rail');
            var prev = document.querySelector('[data-rail-prev="' + key + '"]');
            var next = document.querySelector('[data-rail-next="' + key + '"]');
            function step() {
                var first = rail.firstElementChild;
                if (!first) return rail.clientWidth * 0.8;
                return first.getBoundingClientRect().width + 24;
            }
            if (prev) prev.addEventListener('click', function () {
                rail.scrollBy({ left: -step() * 2, behavior: 'smooth' });
            });
            if (next) next.addEventListener('click', function () {
                rail.scrollBy({ left: step() * 2, behavior: 'smooth' });
            });
        });
    }

    /* ----------- Story viewer ----------- */
    function setupStoryViewer() {
        var rail = document.getElementById('sbStoriesRail');
        var viewer = document.getElementById('sbStoryViewer');
        var dataEl = document.getElementById('sbStoriesData');
        if (!rail || !viewer || !dataEl) return;

        var data;
        try { data = JSON.parse(dataEl.textContent || '[]'); }
        catch (e) { return; }
        if (!Array.isArray(data) || !data.length) return;

        var progressEl = document.getElementById('sbStoryProgress');
        var imgEl      = document.getElementById('sbStoryImage');
        var captionEl  = document.getElementById('sbStoryCaption');
        var avatarEl   = document.getElementById('sbStoryAvatar');
        var nameEl     = document.getElementById('sbStoryStoreName');
        var closeBtn   = document.getElementById('sbStoryClose');
        var tapPrev    = document.getElementById('sbStoryTapPrev');
        var tapNext    = document.getElementById('sbStoryTapNext');

        var storeIdx = 0;
        var storyIdx = 0;
        var timer = null;
        var startTs = 0;
        var elapsed = 0;
        var paused = false;
        var DURATION_MS = 5000;
        var rafId = null;

        function renderHeader() {
            var store = data[storeIdx];
            nameEl.textContent = store.storeName || '';
            avatarEl.innerHTML = '';
            if (store.storeProfileImageUrl) {
                var img = document.createElement('img');
                img.src = store.storeProfileImageUrl;
                img.alt = store.storeName || '';
                avatarEl.appendChild(img);
            } else {
                avatarEl.textContent = (store.storeName || '?').substring(0, 1).toUpperCase();
            }
        }
        function renderProgressBars() {
            var store = data[storeIdx];
            progressEl.innerHTML = '';
            for (var i = 0; i < store.stories.length; i++) {
                var bar = document.createElement('span');
                bar.className = 'sb-story-viewer__progress-bar';
                if (i < storyIdx) bar.classList.add('is-done');
                var fill = document.createElement('i');
                bar.appendChild(fill);
                progressEl.appendChild(bar);
            }
        }
        function setFill(percent) {
            var bars = progressEl.querySelectorAll('.sb-story-viewer__progress-bar');
            if (!bars[storyIdx]) return;
            var fill = bars[storyIdx].querySelector('i');
            if (fill) fill.style.right = (100 - percent) + '%';
        }
        function play() {
            var s = data[storeIdx].stories[storyIdx];
            imgEl.src = s.imageUrl;
            captionEl.textContent = s.caption || '';
            renderProgressBars();
            elapsed = 0;
            startTs = performance.now();
            paused = false;
            // Fire-and-forget view tracking
            fetch('/Stories/View/' + s.id, { method: 'POST' }).catch(function () {});
            cancelAnimationFrame(rafId);
            tick();
        }
        function tick() {
            if (paused) { return; }
            var now = performance.now();
            elapsed = now - startTs;
            var pct = Math.min(100, (elapsed / DURATION_MS) * 100);
            setFill(pct);
            if (elapsed >= DURATION_MS) { advance(1); return; }
            rafId = requestAnimationFrame(tick);
        }
        function advance(delta) {
            cancelAnimationFrame(rafId);
            var stories = data[storeIdx].stories;
            var ni = storyIdx + delta;
            if (ni >= stories.length) {
                // Next store
                if (storeIdx + 1 >= data.length) { close(); return; }
                storeIdx += 1;
                storyIdx = 0;
                renderHeader();
            } else if (ni < 0) {
                if (storeIdx === 0) { storyIdx = 0; } // clamp
                else { storeIdx -= 1; storyIdx = data[storeIdx].stories.length - 1; renderHeader(); }
            } else {
                storyIdx = ni;
            }
            play();
        }
        function open(initialStoreIdx) {
            storeIdx = Math.max(0, Math.min(initialStoreIdx || 0, data.length - 1));
            storyIdx = 0;
            viewer.classList.add('is-open');
            viewer.setAttribute('aria-hidden', 'false');
            document.body.classList.add('sb-locked');
            renderHeader();
            play();
        }
        function close() {
            cancelAnimationFrame(rafId);
            viewer.classList.remove('is-open');
            viewer.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('sb-locked');
            // Mark this rail item as viewed
            var item = rail.querySelector('[data-story-store-id="' + data[storeIdx].storeId + '"] .sb-story-item__ring');
            if (item) item.classList.add('is-viewed');
        }

        rail.querySelectorAll('[data-story-store-id]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var sid = parseInt(btn.getAttribute('data-story-store-id'), 10);
                var idx = data.findIndex(function (x) { return x.storeId === sid; });
                open(idx);
            });
        });

        closeBtn.addEventListener('click', close);
        tapNext.addEventListener('click', function () { advance(1); });
        tapPrev.addEventListener('click', function () { advance(-1); });
        document.addEventListener('keydown', function (e) {
            if (!viewer.classList.contains('is-open')) return;
            if (e.key === 'Escape') close();
            else if (e.key === 'ArrowRight') advance(1);
            else if (e.key === 'ArrowLeft') advance(-1);
        });
    }

    /* ----------- Newsletter AJAX form ----------- */
    function setupNewsletterForm() {
        var form = document.getElementById('sbNewsletterForm');
        var result = document.getElementById('sbNewsletterResult');
        if (!form || !result) return;
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var emailInput = form.querySelector('input[name="email"]');
            if (!emailInput || !emailInput.value) return;
            var submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn) submitBtn.classList.add('is-loading');
            result.className = 'sb-newsletter__result';
            result.textContent = '';

            fetch('/Home/Subscribe', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: emailInput.value })
            }).then(function (r) {
                if (submitBtn) submitBtn.classList.remove('is-loading');
                if (r.ok) {
                    result.classList.add('is-success');
                    result.textContent = 'Təşəkkürlər! Abunəliyin təsdiqləndi.';
                    emailInput.value = '';
                } else {
                    result.classList.add('is-error');
                    result.textContent = 'Email düzgün formatda deyil. Yenidən cəhd et.';
                }
            }).catch(function () {
                if (submitBtn) submitBtn.classList.remove('is-loading');
                result.classList.add('is-error');
                result.textContent = 'Şəbəkə xətası. Sonra cəhd et.';
            });
        });
    }

    /* ----------- Reveal on scroll ----------- */
    function setupRevealOnScroll() {
        var els = document.querySelectorAll('.sb-reveal');
        if (!els.length || typeof IntersectionObserver === 'undefined') {
            els.forEach(function (el) { el.classList.add('is-visible'); });
            return;
        }
        var obs = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('is-visible');
                    obs.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1, rootMargin: '0px 0px -50px 0px' });
        els.forEach(function (el) { obs.observe(el); });
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
