// StyleBazar — UI behaviour
// Header scroll, mega-menus, search overlay, mobile menu+accordion,
// user dropdown, language dropdown, wishlist heart toggle.

(function () {
    'use strict';

    // Each setup is wrapped in a try-catch so a single page-specific
    // setup throwing (e.g. a selector that didn't exist) never cascades
    // and prevents later setups (notably setupRevealOnScroll) from
    // running and leaving content invisible.
    function safe(name, fn) {
        try { fn(); }
        catch (e) {
            if (typeof console !== 'undefined' && console.error) {
                console.error('[StyleBazar] ' + name + ' failed:', e);
            }
        }
    }

    // jQuery Validate adapter for our custom [MustBeTrue] server-side attribute.
    // Must register BEFORE jquery.validate.unobtrusive auto-parses the forms,
    // which happens on its own DOMContentLoaded. Run on our IIFE's
    // DOMContentLoaded via the safe() wrapper below — by then all <script>
    // tags have parsed and $.validator is on window.
    function setupMustBeTrueAdapter() {
        if (typeof window.jQuery === 'undefined') return;
        var $$ = window.jQuery;
        if (!$$.validator || !$$.validator.unobtrusive) return;
        if ($$.validator.methods.mustbetrue) return;  // idempotent

        $$.validator.addMethod('mustbetrue', function (value, element) {
            // Checkboxes report value="true" via .val() regardless of checked
            // state, so we MUST use element.checked instead.
            if (element.type === 'checkbox' || element.type === 'radio') {
                return element.checked === true;
            }
            return value === true || value === 'true';
        });
        $$.validator.unobtrusive.adapters.addBool('mustbetrue');

        // jquery.validate.unobtrusive already auto-parsed the form on its own
        // DOMContentLoaded — before we registered the adapter. Re-parse so our
        // mustbetrue rule attaches to the existing inputs.
        $$('form').each(function () {
            var $form = $$(this);
            $form.removeData('validator').removeData('unobtrusiveValidation');
            $$.validator.unobtrusive.parse($form);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        safe('setupMustBeTrueAdapter',   setupMustBeTrueAdapter);
        safe('setupScrollHeader',        setupScrollHeader);
        safe('setupMegaMenus',           setupMegaMenus);
        safe('setupSearchOverlay',       setupSearchOverlay);
        safe('setupMobileMenu',          setupMobileMenu);
        safe('setupMobileAccordion',     setupMobileAccordion);
        safe('setupUserDropdown',        setupUserDropdown);
        safe('setupLanguageDropdown',    setupLanguageDropdown);
        safe('setupBottomBarActiveState',setupBottomBarActiveState);
        safe('setupWishlistHearts',      setupWishlistHearts);
        safe('setupAuthForms',           setupAuthForms);
        safe('setupHeroCarousel',        setupHeroCarousel);
        safe('setupProductRails',        setupProductRails);
        safe('setupStoryViewer',         setupStoryViewer);
        safe('setupNewsletterForm',      setupNewsletterForm);
        safe('setupRevealOnScroll',      setupRevealOnScroll);
        safe('setupShopFilter',          setupShopFilter);
        safe('setupShopSort',            setupShopSort);
        safe('setupProductGallery',      setupProductGallery);
        safe('setupSizePicker',          setupSizePicker);
        safe('setupQuantityStepper',     setupQuantityStepper);
        safe('setupLightbox',            setupLightbox);
        safe('setupReviewForm',          setupReviewForm);
        safe('setupMyReviewActions',     setupMyReviewActions);
        safe('setupPdpDescription',      setupPdpDescription);
        safe('setupColorPicker',         setupColorPicker);
        safe('setupContactCtas',         setupContactCtas);
        safe('setupShareRow',            setupShareRow);
        safe('setupPdpTabs',             setupPdpTabs);
        safe('setupTileRails',           setupTileRails);
        safe('setupShopsSearch',         setupShopsSearch);
        safe('setupAvatarUploader',      setupAvatarUploader);
        safe('setupAccountProfileForm',  setupAccountProfileForm);
        safe('setupSellerProfile',       setupSellerProfile);
        safe('setupChangePasswordModal', setupChangePasswordModal);
        safe('setupAddresses',           setupAddresses);

        // Final safety net: even if every reveal-on-scroll path failed,
        // any .sb-reveal element still hidden after 1500 ms is force-shown.
        setTimeout(function () {
            document.querySelectorAll('.sb-reveal:not(.is-visible)').forEach(function (el) {
                el.classList.add('is-visible');
            });
        }, 1500);
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
        // Hearts come in two flavours: product card overlay (sb-product__wish)
        // and the big detail-page button (sb-btn-wish-lg). Both opt-in via
        // [data-wish-toggle] so seller/admin UIs can re-use the same icon
        // without firing the wishlist endpoint.
        var buttons = document.querySelectorAll('[data-wish-toggle]');
        if (!buttons.length) return;

        buttons.forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();

                var productId = btn.getAttribute('data-product-id');
                if (!productId) return;
                if (btn.dataset.busy === '1') return;

                // Anonymous user → bounce to login with returnUrl, never call the
                // endpoint (fetch follows the auth redirect silently otherwise).
                if (document.body.getAttribute('data-auth') !== 'true') {
                    var returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
                    window.location.href = '/Account/Login?returnUrl=' + returnUrl;
                    return;
                }

                btn.dataset.busy = '1';

                var formData = new FormData();
                formData.append('productId', productId);
                var token = getAntiForgeryToken();
                if (token) formData.append('__RequestVerificationToken', token);

                fetch('/Wishlist/Toggle', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: formData
                })
                .then(function (res) {
                    if (res.status === 401) {
                        // Anonymous → kick to login with returnUrl back to this page.
                        var returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
                        window.location.href = '/Account/Login?returnUrl=' + returnUrl;
                        return null;
                    }
                    if (!res.ok) throw new Error('HTTP ' + res.status);
                    return res.json();
                })
                .then(function (data) {
                    if (!data) return;
                    var nowActive = !!data.isInWishlist;
                    btn.classList.toggle('is-active', nowActive);
                    btn.classList.toggle('is-saved', nowActive);
                    btn.setAttribute('aria-pressed', nowActive ? 'true' : 'false');
                    btn.classList.remove('is-pulsing');
                    void btn.offsetWidth; // restart animation
                    btn.classList.add('is-pulsing');

                    updateWishlistCount(data.count);

                    showToast(nowActive ? 'Wishlist-ə əlavə olundu' : 'Wishlist-dən silindi', 'success');
                })
                .catch(function () {
                    showToast('Xəta baş verdi. Yenidən cəhd edin.', 'error');
                })
                .finally(function () {
                    btn.dataset.busy = '';
                });
            });
        });

        // Dedicated "remove" buttons on the wishlist page — animate the card
        // out, then call /Wishlist/Remove so we never accidentally re-add.
        document.querySelectorAll('[data-wishlist-remove]').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                var productId = btn.getAttribute('data-product-id');
                if (!productId) return;
                if (btn.dataset.busy === '1') return;
                btn.dataset.busy = '1';

                var card = btn.closest('[data-wishlist-card]');
                if (card) card.classList.add('is-removing');

                var formData = new FormData();
                formData.append('productId', productId);
                var token = getAntiForgeryToken();
                if (token) formData.append('__RequestVerificationToken', token);

                fetch('/Wishlist/Remove', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: formData
                })
                .then(function (res) { return res.ok ? res.json() : null; })
                .then(function (data) {
                    if (card) {
                        setTimeout(function () {
                            card.remove();
                            // If grid is now empty, force a reload so the empty
                            // state renders cleanly.
                            var grid = document.querySelector('.sb-wishlist-grid');
                            if (grid && !grid.querySelector('.sb-wishlist-card')) {
                                window.location.reload();
                            }
                        }, 320);
                    }
                    if (data) updateWishlistCount(data.count);
                })
                .catch(function () {
                    if (card) card.classList.remove('is-removing');
                    showToast('Xəta baş verdi.', 'error');
                })
                .finally(function () { btn.dataset.busy = ''; });
            });
        });
    }

    /* ----------- Helpers shared by wishlist + review ----------- */
    function getAntiForgeryToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : null;
    }

    function updateWishlistCount(count) {
        var el = document.querySelector('[data-wishlist-count]');
        if (!el) return;
        el.textContent = String(count);
        el.classList.toggle('is-visible', count > 0);
        el.classList.remove('is-bumping');
        void el.offsetWidth;
        el.classList.add('is-bumping');
    }

    var toastEl = null, toastTimer = null;
    function showToast(message, kind) {
        if (!toastEl) {
            toastEl = document.createElement('div');
            toastEl.className = 'sb-toast';
            toastEl.setAttribute('role', 'status');
            toastEl.setAttribute('aria-live', 'polite');
            document.body.appendChild(toastEl);
        }
        toastEl.textContent = message;
        toastEl.classList.remove('is-error', 'is-success', 'is-visible');
        if (kind === 'error') toastEl.classList.add('is-error');
        else if (kind === 'success') toastEl.classList.add('is-success');
        void toastEl.offsetWidth;
        toastEl.classList.add('is-visible');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () {
            toastEl.classList.remove('is-visible');
        }, 2400);
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

    /* ----------- Shop filter (mobile drawer) ----------- */
    function setupShopFilter() {
        var filter = document.getElementById('sbShopFilter');
        var openBtn = document.getElementById('sbFilterOpen');
        var closeBtn = document.getElementById('sbFilterClose');
        var backdrop = document.getElementById('sbFilterBackdrop');
        if (!filter || !openBtn) return;

        function open() {
            filter.classList.add('is-open');
            if (backdrop) backdrop.classList.add('is-open');
            document.body.classList.add('sb-locked');
        }
        function close() {
            filter.classList.remove('is-open');
            if (backdrop) backdrop.classList.remove('is-open');
            document.body.classList.remove('sb-locked');
        }

        openBtn.addEventListener('click', open);
        if (closeBtn) closeBtn.addEventListener('click', close);
        if (backdrop) backdrop.addEventListener('click', close);
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && filter.classList.contains('is-open')) close();
        });
    }

    /* ----------- Shop sort (auto-submit form on change) ----------- */
    function setupShopSort() {
        var sel = document.getElementById('sbShopSort');
        var form = document.getElementById('sbShopFilterForm');
        if (!sel || !form) return;
        sel.addEventListener('change', function () {
            // Find the hidden sortBy input inside the filter form and submit.
            var hidden = form.querySelector('input[name="sortBy"]');
            if (hidden) hidden.value = sel.value;
            // Drop pagination when sort changes
            var pageHidden = form.querySelector('input[name="page"]');
            if (pageHidden) pageHidden.remove();
            form.submit();
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

        // Password strength meter — calibrated against Identity's actual rules
        // in AddIdentityServices() (min 6 chars + lowercase + digit). Anything
        // that *passes Identity* is at least Orta. Length / Az alphabet
        // letters / extra classes promote it to Güclü.
        document.querySelectorAll('[data-pwd-meter]').forEach(function (input) {
            var field = input.closest('.sb-field');
            if (!field) return;
            var bars = field.querySelectorAll('[data-meter] .sb-pwd-meter__bar');
            var label = field.querySelector('[data-meter-label]');
            if (!bars.length) return;

            function score(v) {
                if (!v) return 0;
                var lower    = /[a-zəıöüşçğ]/.test(v);
                var digit    = /\d/.test(v);
                var long6    = v.length >= 6;
                if (!long6) return 0;
                // Identity-passing baseline (≥6 + lower + digit) = Orta.
                if (lower && digit) {
                    var upper  = /[A-ZƏIÖÜŞÇĞ]/.test(v);
                    var symbol = /[^a-zA-Z0-9əıöüşçğƏIÖÜŞÇĞ]/.test(v);
                    var long10 = v.length >= 10;
                    if ((upper && long10) || (symbol && long10) || (upper && symbol)) return 3;
                    return 2;
                }
                return 1;
            }

            function render() {
                var s = score(input.value);
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
                        label.textContent = 'Zəif — rəqəm və hərf əlavə et';
                        label.classList.add('is-weak');
                    } else if (s === 2) {
                        label.textContent = 'Yaxşı — qəbul olunur';
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

        // Auth forms — submit loading + stale-error clearing
        document.querySelectorAll('[data-auth-form]').forEach(function (form) {
            var btn = form.querySelector('[data-submit]');
            var summary = form.querySelector('.sb-form-summary');

            // ---- 1. Submit-loading spinner ----------------------------
            // jQuery Unobtrusive Validation may preventDefault() if a field
            // is invalid. setTimeout(0) lets that run first, so we only paint
            // the spinner when the form is actually navigating — otherwise
            // the button (which has pointer-events: none in is-loading) would
            // be permanently dead until full page reload.
            if (btn) {
                form.addEventListener('submit', function (e) {
                    setTimeout(function () {
                        if (!e.defaultPrevented) btn.classList.add('is-loading');
                    }, 0);
                });
            }

            // ---- 2. Clear stale server-rendered errors ----------------
            // When ModelState has errors, asp-validation-for bakes the message
            // text into <span class="sb-field__error"> server-side. jQuery
            // Unobtrusive only manages errors it produced — server-rendered
            // text stays put until the next submit. That's why "Davam etmək
            // üçün şərtləri qəbul et." persisted in the screenshot even
            // AFTER the user checked the box.
            //
            // We clear the error span + the `is-error` paint on the input
            // as soon as the user touches the field. The form summary at
            // the top is cleared on the first edit anywhere.
            function clearError(field) {
                if (!field) return;
                var err = field.querySelector('.sb-field__error');
                if (err) err.textContent = '';
                field.querySelectorAll('input, select, textarea').forEach(function (el) {
                    el.classList.remove('is-error', 'input-validation-error');
                });
                // Also clear the wrapper modifier some inputs use.
                field.classList.remove('is-error');
            }

            function clearSummary() {
                if (summary) summary.style.display = 'none';
            }

            // Inputs / textareas / selects — clear on every keystroke.
            form.querySelectorAll('.sb-field input, .sb-field textarea, .sb-field select')
                .forEach(function (el) {
                    el.addEventListener('input', function () {
                        clearError(el.closest('.sb-field'));
                        clearSummary();
                        if (btn) btn.classList.remove('is-loading');
                    });
                });

            // Checkboxes / radios — clear on toggle (covers AcceptTerms,
            // role cards, RememberMe).
            form.querySelectorAll('input[type="checkbox"], input[type="radio"]')
                .forEach(function (el) {
                    el.addEventListener('change', function () {
                        clearError(el.closest('.sb-field'));
                        clearSummary();
                        if (btn) btn.classList.remove('is-loading');
                    });
                });
        });
    }

    /* ----------- Product gallery — thumb click swaps main image ----------- */
    function setupProductGallery() {
        var gallery = document.querySelector('[data-gallery]');
        if (!gallery) return;
        var mainImg = gallery.querySelector('[data-main-img]');
        var thumbs = gallery.querySelectorAll('[data-thumb-index]');
        if (!mainImg || !thumbs.length) return;

        thumbs.forEach(function (thumb) {
            thumb.addEventListener('click', function () {
                var url = thumb.getAttribute('data-thumb-url');
                if (!url || mainImg.src === url) return;

                thumbs.forEach(function (t) { t.classList.remove('is-active'); });
                thumb.classList.add('is-active');

                mainImg.classList.add('is-fading');
                var pre = new Image();
                pre.onload = function () {
                    mainImg.src = url;
                    requestAnimationFrame(function () {
                        mainImg.classList.remove('is-fading');
                    });
                };
                pre.src = url;
            });
        });
    }

    /* ----------- Size picker — single-select chip group ----------- */
    function setupSizePicker() {
        var picker = document.querySelector('[data-size-picker]');
        if (!picker) return;
        var label = document.getElementById('sbSizeLabel');
        var qtyWrap = document.querySelector('[data-qty]');

        picker.querySelectorAll('.sb-size-chip').forEach(function (chip) {
            if (chip.disabled) return;
            chip.addEventListener('click', function () {
                picker.querySelectorAll('.sb-size-chip').forEach(function (c) {
                    c.classList.remove('is-active');
                });
                chip.classList.add('is-active');

                var size = chip.getAttribute('data-size-value');
                var stock = parseInt(chip.getAttribute('data-size-stock') || '0', 10);
                if (label) {
                    label.textContent = size + ' — ' + stock + ' ədəd stokda';
                    label.style.color = 'var(--sb-ink)';
                }
                // If size has its own stock, clamp qty max to that stock
                if (qtyWrap && stock > 0) {
                    qtyWrap.setAttribute('data-max', String(stock));
                    var input = qtyWrap.querySelector('[data-qty-input]');
                    if (input) {
                        input.max = String(stock);
                        if (parseInt(input.value, 10) > stock) input.value = String(stock);
                    }
                }
            });
        });
    }

    /* ----------- Quantity stepper — clamp [1, max] ----------- */
    function setupQuantityStepper() {
        document.querySelectorAll('[data-qty]').forEach(function (wrap) {
            var input = wrap.querySelector('[data-qty-input]');
            var dec = wrap.querySelector('[data-qty-dec]');
            var inc = wrap.querySelector('[data-qty-inc]');
            if (!input || !dec || !inc) return;

            function getMax() { return parseInt(wrap.getAttribute('data-max') || input.max || '99', 10); }
            function clamp(v) {
                var max = getMax();
                if (isNaN(v) || v < 1) v = 1;
                if (v > max) v = max;
                return v;
            }
            function paint() {
                var v = clamp(parseInt(input.value, 10));
                input.value = String(v);
                dec.disabled = v <= 1;
                inc.disabled = v >= getMax() || getMax() <= 0;
            }

            dec.addEventListener('click', function () {
                input.value = String(clamp(parseInt(input.value, 10) - 1));
                paint();
            });
            inc.addEventListener('click', function () {
                input.value = String(clamp(parseInt(input.value, 10) + 1));
                paint();
            });
            input.addEventListener('input', paint);
            input.addEventListener('blur', paint);
            paint();
        });
    }

    /* ----------- Lightbox — full-screen gallery viewer ----------- */
    function setupLightbox() {
        var lightbox = document.getElementById('sbLightbox');
        if (!lightbox) return;
        var dataEl = document.getElementById('sbLightboxData');
        var images = [];
        if (dataEl) {
            try { images = JSON.parse(dataEl.textContent || '[]'); }
            catch (e) { images = []; }
        }
        if (!images.length) return;

        var img = document.getElementById('sbLightboxImg');
        var counter = document.getElementById('sbLightboxCounter');
        var btnClose = lightbox.querySelector('[data-lightbox-close]');
        var btnPrev = lightbox.querySelector('[data-lightbox-prev]');
        var btnNext = lightbox.querySelector('[data-lightbox-next]');
        var idx = 0;

        function show(i, animate) {
            idx = (i + images.length) % images.length;
            if (animate && img) {
                img.classList.add('is-fading');
                setTimeout(function () {
                    img.src = images[idx].url;
                    img.alt = images[idx].alt || '';
                    requestAnimationFrame(function () { img.classList.remove('is-fading'); });
                }, 120);
            } else if (img) {
                img.src = images[idx].url;
                img.alt = images[idx].alt || '';
            }
            if (counter) counter.textContent = (idx + 1) + ' / ' + images.length;
            // Hide nav buttons when only one image
            if (images.length <= 1) {
                if (btnPrev) btnPrev.style.display = 'none';
                if (btnNext) btnNext.style.display = 'none';
                if (counter) counter.style.display = 'none';
            }
        }
        function open(startIdx) {
            show(typeof startIdx === 'number' ? startIdx : 0, false);
            lightbox.classList.add('is-open');
            lightbox.setAttribute('aria-hidden', 'false');
            document.body.style.overflow = 'hidden';
        }
        function close() {
            lightbox.classList.remove('is-open');
            lightbox.setAttribute('aria-hidden', 'true');
            document.body.style.overflow = '';
        }
        function next() { show(idx + 1, true); }
        function prev() { show(idx - 1, true); }

        // Open from main image click
        var main = document.querySelector('[data-gallery-main]');
        if (main) {
            main.addEventListener('click', function () { open(0); });
            main.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); open(0); }
            });
        }
        // Open from any thumb (matching its index)
        document.querySelectorAll('[data-thumb-index]').forEach(function (thumb) {
            thumb.addEventListener('dblclick', function () {
                var i = parseInt(thumb.getAttribute('data-thumb-index') || '0', 10);
                open(i);
            });
        });
        // Open from review image
        document.querySelectorAll('[data-review-image]').forEach(function (b) {
            b.addEventListener('click', function () {
                var url = b.getAttribute('data-review-image');
                if (!url) return;
                // Use the review image as a single-entry override
                images = [{ url: url, alt: 'Rəy şəkli' }];
                idx = 0;
                open(0);
            });
        });

        if (btnClose) btnClose.addEventListener('click', close);
        if (btnPrev) btnPrev.addEventListener('click', prev);
        if (btnNext) btnNext.addEventListener('click', next);

        lightbox.addEventListener('click', function (e) {
            if (e.target === lightbox) close();
        });

        document.addEventListener('keydown', function (e) {
            if (!lightbox.classList.contains('is-open')) return;
            if (e.key === 'Escape') close();
            else if (e.key === 'ArrowRight') next();
            else if (e.key === 'ArrowLeft') prev();
        });
    }

    /* ----------- Review form — rating picker + image upload + AJAX submit ----------- */
    function setupReviewForm() {
        var form = document.querySelector('[data-review-form]');
        if (!form) return;
        var productId = form.getAttribute('data-product-id');
        var body = form.querySelector('[data-review-form-body]');
        var toggle = form.querySelector('[data-review-form-toggle]');
        var cancel = form.querySelector('[data-review-form-cancel]');
        var submit = form.querySelector('[data-review-submit]');
        var ratingInput = form.querySelector('[data-rating-value-input]');
        var ratingHint = form.querySelector('[data-rating-hint]');
        var stars = form.querySelectorAll('[data-rating-value]');
        var textarea = form.querySelector('[data-review-textarea]');
        var counter = form.querySelector('[data-review-counter]');
        var fileInput = form.querySelector('[data-review-image-input]');
        var previews = form.querySelector('[data-review-previews]');
        var errorBox = form.querySelector('[data-review-error]');

        var pendingFiles = []; // staged images attached after the review is created
        var MAX_FILES = 3;
        var MAX_BYTES = 10 * 1024 * 1024;

        function open() {
            form.classList.add('is-open');
            if (toggle) toggle.setAttribute('aria-expanded', 'true');
            if (textarea) setTimeout(function () { textarea.focus(); }, 100);
        }
        function close() {
            form.classList.remove('is-open');
            if (toggle) toggle.setAttribute('aria-expanded', 'false');
        }
        if (toggle) toggle.addEventListener('click', open);
        if (cancel) cancel.addEventListener('click', function () {
            close();
            resetForm();
        });

        // Rating stars — hover preview + click set
        stars.forEach(function (star, idx) {
            star.addEventListener('mouseenter', function () { paintStars(idx + 1, true); });
            star.addEventListener('focus', function () { paintStars(idx + 1, true); });
            star.addEventListener('click', function () {
                var v = idx + 1;
                ratingInput.value = String(v);
                ratingHint.textContent = v + ' / 5';
                ratingHint.classList.add('is-set');
                paintStars(v, false);
                stars.forEach(function (s, i) { s.setAttribute('aria-checked', i < v ? 'true' : 'false'); });
            });
        });
        var ratingWrap = form.querySelector('[data-rating-input]');
        if (ratingWrap) {
            ratingWrap.addEventListener('mouseleave', function () {
                var v = parseInt(ratingInput.value, 10) || 0;
                paintStars(v, false);
            });
        }
        function paintStars(count, isPreview) {
            stars.forEach(function (s, i) {
                s.classList.remove('is-active', 'is-preview');
                if (i < count) s.classList.add(isPreview ? 'is-preview' : 'is-active');
            });
        }

        // Textarea char counter
        if (textarea && counter) {
            var paintCounter = function () {
                var len = textarea.value.length;
                counter.textContent = len + ' / 1000';
                counter.classList.toggle('is-warning', len > 900);
            };
            textarea.addEventListener('input', paintCounter);
            paintCounter();
        }

        // Image staging (we upload after the POST succeeds and we have a review id)
        if (fileInput) {
            fileInput.addEventListener('change', function () {
                var files = Array.from(fileInput.files || []);
                files.forEach(function (f) {
                    if (pendingFiles.length >= MAX_FILES) return;
                    if (!/^image\//.test(f.type)) return;
                    if (f.size > MAX_BYTES) {
                        showToast('Şəkil 10 MB-dan böyük ola bilməz.', 'error');
                        return;
                    }
                    pendingFiles.push(f);
                });
                fileInput.value = '';
                renderPreviews();
            });
        }
        function renderPreviews() {
            if (!previews) return;
            previews.innerHTML = '';
            pendingFiles.forEach(function (file, i) {
                var url = URL.createObjectURL(file);
                var wrap = document.createElement('div');
                wrap.className = 'sb-review-form__preview';
                wrap.innerHTML =
                    '<img src="' + url + '" alt="" />' +
                    '<button type="button" class="sb-review-form__preview-remove" aria-label="Sil">' +
                    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>' +
                    '</button>';
                wrap.querySelector('button').addEventListener('click', function () {
                    URL.revokeObjectURL(url);
                    pendingFiles.splice(i, 1);
                    renderPreviews();
                });
                previews.appendChild(wrap);
            });
        }

        // Submit
        if (body) {
            body.addEventListener('submit', function (e) {
                e.preventDefault();
                hideError();

                var rating = parseInt(ratingInput.value, 10) || 0;
                if (rating < 1 || rating > 5) {
                    showError('Zəhmət olmasa ulduz seçin.');
                    return;
                }

                if (submit) submit.classList.add('is-loading');

                var formData = new FormData();
                formData.append('productId', productId);
                formData.append('rating', String(rating));
                formData.append('comment', textarea ? textarea.value : '');
                var token = getAntiForgeryToken();
                if (token) formData.append('__RequestVerificationToken', token);

                fetch('/Review/Create', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: formData
                })
                .then(function (res) {
                    return res.json().then(function (json) { return { ok: res.ok, status: res.status, json: json }; });
                })
                .then(function (r) {
                    if (!r.ok) {
                        var msg = r.json && (r.json.error || (r.json.errors && firstError(r.json.errors))) || 'Rəy göndərilə bilmədi.';
                        showError(msg);
                        return;
                    }
                    var reviewId = r.json.review && r.json.review.id;
                    // Upload images sequentially, then reload to show the final state.
                    return uploadImagesSequentially(reviewId).then(function () {
                        showToast('Rəyiniz əlavə olundu', 'success');
                        setTimeout(function () { window.location.reload(); }, 400);
                    });
                })
                .catch(function () { showError('Xəta baş verdi. Yenidən cəhd edin.'); })
                .finally(function () {
                    if (submit) submit.classList.remove('is-loading');
                });
            });
        }

        function uploadImagesSequentially(reviewId) {
            if (!reviewId || !pendingFiles.length) return Promise.resolve();
            return pendingFiles.reduce(function (chain, file) {
                return chain.then(function () {
                    var fd = new FormData();
                    fd.append('reviewId', String(reviewId));
                    fd.append('file', file);
                    var token = getAntiForgeryToken();
                    if (token) fd.append('__RequestVerificationToken', token);
                    return fetch('/Review/UploadImage', {
                        method: 'POST',
                        credentials: 'same-origin',
                        headers: { 'X-Requested-With': 'XMLHttpRequest' },
                        body: fd
                    });
                });
            }, Promise.resolve());
        }

        function showError(msg) {
            if (!errorBox) return;
            errorBox.textContent = msg;
            errorBox.hidden = false;
        }
        function hideError() {
            if (!errorBox) return;
            errorBox.hidden = true;
            errorBox.textContent = '';
        }
        function firstError(errors) {
            if (!errors) return null;
            if (Array.isArray(errors) && errors.length) return errors[0].errorMessage || errors[0];
            // ValidationException maps to a dict { field: [messages] }
            var keys = Object.keys(errors);
            if (!keys.length) return null;
            var v = errors[keys[0]];
            return Array.isArray(v) ? v[0] : v;
        }
        function resetForm() {
            ratingInput.value = '0';
            ratingHint.textContent = 'Ulduz seç';
            ratingHint.classList.remove('is-set');
            paintStars(0, false);
            if (textarea) textarea.value = '';
            if (counter) counter.textContent = '0 / 1000';
            pendingFiles = [];
            renderPreviews();
            hideError();
        }
    }

    /* ----------- "My review" inline delete (edit is a TODO once UI is ready) ----------- */
    function setupMyReviewActions() {
        var card = document.querySelector('[data-my-review]');
        if (!card) return;
        var reviewId = card.getAttribute('data-review-id');
        var del = card.querySelector('[data-my-review-delete]');
        if (del) {
            del.addEventListener('click', function () {
                if (!confirm('Bu rəyi silmək istədiyinizdən əminsiniz?')) return;
                var formData = new FormData();
                formData.append('id', reviewId);
                var token = getAntiForgeryToken();
                if (token) formData.append('__RequestVerificationToken', token);
                fetch('/Review/Delete', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: formData
                })
                .then(function (res) {
                    if (res.ok) {
                        showToast('Rəy silindi', 'success');
                        setTimeout(function () { window.location.reload(); }, 400);
                    } else {
                        showToast('Silinə bilmədi.', 'error');
                    }
                })
                .catch(function () { showToast('Xəta baş verdi.', 'error'); });
            });
        }
        // Edit — open the inline form pre-filled. For now we just scroll user
        // to the form section (where it would re-render). Full inline edit
        // can land in a follow-up; we already support Update on the API.
        var edit = card.querySelector('[data-my-review-edit]');
        if (edit) {
            edit.addEventListener('click', function () {
                showToast('Redaktə yaxında qoşulacaq — hələlik silib yenidən yaza bilərsiniz.', 'success');
            });
        }
    }

    /* ----------- PDP: description "daha çox" toggle ----------- */
    function setupPdpDescription() {
        var wrap = document.querySelector('[data-desc]');
        if (!wrap) return;
        var btn = wrap.querySelector('[data-desc-more]');
        var text = wrap.querySelector('.sb-pdp__desc-text');
        if (!btn || !text) return;
        // Only show "daha çox" if the text actually overflows
        if (text.scrollHeight <= text.clientHeight + 2) {
            btn.style.display = 'none';
            return;
        }
        btn.addEventListener('click', function () {
            var open = wrap.classList.toggle('is-expanded');
            btn.textContent = open ? 'daha az' : 'daha çox';
        });
    }

    /* ----------- PDP: color swatch picker ----------- */
    function setupColorPicker() {
        var picker = document.querySelector('[data-color-picker]');
        if (!picker) return;
        var label = document.querySelector('[data-color-label]');
        picker.querySelectorAll('.sb-color-swatch').forEach(function (sw) {
            sw.addEventListener('click', function () {
                picker.querySelectorAll('.sb-color-swatch').forEach(function (s) { s.classList.remove('is-active'); });
                sw.classList.add('is-active');
                if (label) label.textContent = sw.getAttribute('data-color-name') || '—';
            });
        });
    }

    /* ----------- PDP: contact CTA dispatcher (WhatsApp / Instagram / Telegram / Phone) ----------- */
    function setupContactCtas() {
        var root = document.querySelector('.sb-pdp');
        if (!root) return;
        var phone     = root.getAttribute('data-phone')     || '';
        var whatsapp  = root.getAttribute('data-whatsapp')  || '';
        var instagram = root.getAttribute('data-instagram') || '';
        var telegram  = root.getAttribute('data-telegram')  || '';
        var productName = root.getAttribute('data-product-name') || '';
        var productUrl  = root.getAttribute('data-product-url')  || '';
        var storeName   = root.getAttribute('data-store-name')   || '';

        // Strip any non-digit from whatsapp / phone (wa.me wants digits only).
        function clean(num) { return (num || '').replace(/[^\d]/g, ''); }

        function selectedSize() {
            var s = document.querySelector('.sb-size-chip.is-active');
            return s ? s.getAttribute('data-size-value') : null;
        }
        function selectedColor() {
            var c = document.querySelector('.sb-color-swatch.is-active');
            return c ? c.getAttribute('data-color-name') : null;
        }
        function buildMessage() {
            var size = selectedSize();
            var color = selectedColor();
            var lines = ['Salam! "' + productName + '" məhsulu ilə maraqlanıram.'];
            if (color) lines.push('Rəng: ' + color);
            if (size) lines.push('Ölçü: ' + size);
            if (productUrl) lines.push('Link: ' + productUrl);
            return lines.join('\n');
        }
        function open(channel) {
            var msg = buildMessage();
            var url = null;
            switch (channel) {
                case 'whatsapp':
                    if (!whatsapp) return;
                    url = 'https://wa.me/' + clean(whatsapp) + '?text=' + encodeURIComponent(msg);
                    break;
                case 'instagram':
                    if (!instagram) return;
                    url = 'https://instagram.com/' + instagram.replace(/^@/, '');
                    break;
                case 'telegram':
                    if (!telegram) return;
                    url = 'https://t.me/' + telegram.replace(/^@/, '');
                    break;
                case 'phone':
                    if (!phone) return;
                    url = 'tel:' + phone;
                    break;
            }
            if (url) window.open(url, '_blank', 'noopener');
        }

        var primary = root.querySelector('[data-contact-primary]');
        if (primary) {
            primary.addEventListener('click', function () { open(primary.getAttribute('data-channel')); });
        }
        root.querySelectorAll('.sb-pdp-channel').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                var ch = btn.getAttribute('data-channel');
                // <a href="tel:...">  — let the browser handle it
                if (ch === 'phone' && btn.tagName === 'A') return;
                e.preventDefault();
                open(ch);
            });
        });
    }

    /* ----------- PDP: social share row ----------- */
    function setupShareRow() {
        var root = document.querySelector('.sb-pdp');
        if (!root) return;
        var productName = root.getAttribute('data-product-name') || '';
        var productUrl  = root.getAttribute('data-product-url')  || window.location.href;
        var msg = productName + ' — ' + productUrl;
        var enc = encodeURIComponent;

        var wa = root.querySelector('[data-share-whatsapp]');
        if (wa) wa.setAttribute('href', 'https://wa.me/?text=' + enc(msg));

        var tg = root.querySelector('[data-share-telegram]');
        if (tg) tg.setAttribute('href', 'https://t.me/share/url?url=' + enc(productUrl) + '&text=' + enc(productName));

        var fb = root.querySelector('[data-share-facebook]');
        if (fb) fb.setAttribute('href', 'https://www.facebook.com/sharer/sharer.php?u=' + enc(productUrl));

        var x = root.querySelector('[data-share-x]');
        if (x) x.setAttribute('href', 'https://twitter.com/intent/tweet?text=' + enc(productName) + '&url=' + enc(productUrl));

        // Open share links in a new tab (Instagram link is just to instagram.com — no share API)
        ['[data-share-whatsapp]','[data-share-telegram]','[data-share-facebook]','[data-share-x]'].forEach(function (sel) {
            var el = root.querySelector(sel);
            if (el) { el.setAttribute('target', '_blank'); el.setAttribute('rel', 'noopener'); }
        });

        var copy = root.querySelector('[data-share-copy]');
        if (copy) {
            copy.addEventListener('click', function () {
                if (!navigator.clipboard) {
                    showToast('Brauzeriniz dəstəkləmir.', 'error');
                    return;
                }
                navigator.clipboard.writeText(productUrl).then(function () {
                    copy.classList.add('is-copied');
                    showToast('Link köçürüldü', 'success');
                    setTimeout(function () { copy.classList.remove('is-copied'); }, 1500);
                });
            });
        }
    }

    /* ----------- Pastel tile rails — arrow controls scroll the rail ----------- */
    function setupTileRails() {
        document.querySelectorAll('.sb-tile__rail-wrap').forEach(function (wrap) {
            var rail = wrap.querySelector('[data-rail]');
            var prev = wrap.querySelector('[data-rail-prev]');
            var next = wrap.querySelector('[data-rail-next]');
            if (!rail) return;

            function scrollBy(dir) {
                var step = Math.max(220, Math.round(rail.clientWidth * 0.8));
                rail.scrollBy({ left: dir * step, behavior: 'smooth' });
            }

            if (prev) prev.addEventListener('click', function () { scrollBy(-1); });
            if (next) next.addEventListener('click', function () { scrollBy(1); });

            // Disable arrows at edges
            function updateState() {
                var atStart = rail.scrollLeft <= 4;
                var atEnd = rail.scrollLeft + rail.clientWidth >= rail.scrollWidth - 4;
                if (prev) prev.style.opacity = atStart ? '0.4' : '1';
                if (prev) prev.style.pointerEvents = atStart ? 'none' : 'auto';
                if (next) next.style.opacity = atEnd ? '0.4' : '1';
                if (next) next.style.pointerEvents = atEnd ? 'none' : 'auto';
            }
            updateState();
            rail.addEventListener('scroll', updateState, { passive: true });
            window.addEventListener('resize', updateState, { passive: true });
        });
    }

    /* ----------- Shops page — live filter as user types ----------- */
    function setupShopsSearch() {
        var input = document.querySelector('[data-shops-search]');
        var grid = document.querySelector('[data-shop-grid]');
        if (!input || !grid) return;

        var debounce;
        input.addEventListener('input', function () {
            clearTimeout(debounce);
            debounce = setTimeout(function () {
                var q = (input.value || '').trim().toLowerCase();
                var visible = 0;
                grid.querySelectorAll('[data-shop-card]').forEach(function (card) {
                    var name = (card.getAttribute('data-shop-name') || '').toLowerCase();
                    var match = !q || name.indexOf(q) !== -1;
                    card.classList.toggle('is-hidden', !match);
                    if (match) visible++;
                });
                // We don't submit on every keystroke — but if the user presses Enter,
                // we submit the form to re-query the backend (gets results past
                // the first page too).
            }, 120);
        });
    }

    /* ----------- PDP: about / Q&A / reviews tabs ----------- */
    function setupPdpTabs() {
        var tabs = document.querySelector('[data-tabs]');
        if (!tabs) return;
        var heads = tabs.querySelectorAll('[data-tab]');
        var panels = tabs.querySelectorAll('[data-tab-panel]');
        heads.forEach(function (h) {
            h.addEventListener('click', function () {
                var key = h.getAttribute('data-tab');
                heads.forEach(function (x) {
                    var on = x === h;
                    x.classList.toggle('is-active', on);
                    x.setAttribute('aria-selected', on ? 'true' : 'false');
                });
                panels.forEach(function (p) {
                    p.classList.toggle('is-active', p.getAttribute('data-tab-panel') === key);
                });
            });
        });
        // If URL has #sbReviews / #sbQa, jump to that tab
        if (window.location.hash === '#sbReviews') {
            var rev = tabs.querySelector('[data-tab="reviews"]');
            if (rev) rev.click();
        } else if (window.location.hash === '#sbQa') {
            var qa = tabs.querySelector('[data-tab="qa"]');
            if (qa) qa.click();
        }
    }

    /* ----------- Avatar uploader (clicking the circle opens the file picker) ----------- */
    function setupAvatarUploader() {
        document.querySelectorAll('[data-avatar-uploader]').forEach(function (wrap) {
            var input = wrap.querySelector('[data-avatar-input]');
            if (!input) return;
            var preview = wrap.querySelector('[data-avatar-preview]');
            var label = document.querySelector('[data-avatar-label]');
            var triggerBtn = document.querySelector('[data-avatar-trigger]');

            if (triggerBtn) {
                triggerBtn.addEventListener('click', function (e) { e.preventDefault(); input.click(); });
            }

            input.addEventListener('change', function () {
                var file = input.files && input.files[0];
                if (!file) return;

                if (label) label.textContent = file.name;

                // Local preview
                if (preview) {
                    var url = URL.createObjectURL(file);
                    preview.innerHTML = '<img src="' + url + '" alt="" />';
                }

                // Server upload
                var fd = new FormData();
                fd.append('file', file);
                var token = getAntiForgeryToken();
                if (token) fd.append('__RequestVerificationToken', token);

                fetch('/Account/UploadAvatar', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: fd
                })
                .then(function (res) { return res.json().catch(function () { return null; }); })
                .then(function (data) {
                    if (data && data.ok) {
                        showToast('Şəkil yeniləndi', 'success');
                    } else {
                        var msg = (data && (data.error || firstValidationError(data.errors))) || 'Şəkil yüklənmədi.';
                        showToast(msg, 'error');
                    }
                })
                .catch(function () { showToast('Şəkil yüklənmədi.', 'error'); });
            });
        });
    }

    /* ----------- Account Profile form (image 1) — AJAX update ----------- */
    function setupAccountProfileForm() {
        var form = document.querySelector('[data-acc-profile-form]');
        if (!form) return;
        var submit = form.querySelector('[data-acc-profile-submit]');
        var errBox = form.querySelector('[data-acc-profile-error]');

        form.addEventListener('submit', function (e) {
            e.preventDefault();
            if (errBox) { errBox.hidden = true; errBox.textContent = ''; }
            if (submit) submit.classList.add('is-loading');

            var fd = new FormData(form);
            fetch('/Account/UpdateProfile', {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: fd
            })
            .then(function (res) { return res.json().then(function (j) { return { ok: res.ok, json: j }; }); })
            .then(function (r) {
                if (!r.ok) {
                    var msg = (r.json && (r.json.error || firstValidationError(r.json.errors))) || 'Yenilənmədi.';
                    if (errBox) { errBox.textContent = msg; errBox.hidden = false; }
                    else showToast(msg, 'error');
                    return;
                }
                showToast('Profil yeniləndi', 'success');
            })
            .catch(function () {
                if (errBox) { errBox.textContent = 'Şəbəkə xətası.'; errBox.hidden = false; }
                else showToast('Şəbəkə xətası.', 'error');
            })
            .finally(function () { if (submit) submit.classList.remove('is-loading'); });
        });
    }

    /* ----------- Seller Profile (image 2) — Save All button posts both forms ----------- */
    function setupSellerProfile() {
        var saveBtn = document.querySelector('[data-seller-save-all]');
        if (!saveBtn) return;
        var profileForm = document.querySelector('[data-seller-profile-form]');
        var socialForm = document.querySelector('[data-seller-social-form]');

        saveBtn.addEventListener('click', function () {
            if (saveBtn.dataset.busy === '1') return;
            saveBtn.dataset.busy = '1';
            saveBtn.classList.add('is-loading');

            var calls = [];

            if (profileForm) {
                var fd1 = new FormData(profileForm);
                calls.push(fetch('/Seller/UpdateProfile', {
                    method: 'POST', credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: fd1
                }).then(function (r) { return r.json().then(function (j) { return { ok: r.ok, json: j, label: 'profile' }; }); }));
            }
            if (socialForm) {
                var storeId = socialForm.querySelector('input[name="storeId"]')?.value || '0';
                if (storeId !== '0') {
                    var fd2 = new FormData(socialForm);
                    calls.push(fetch('/Seller/UpdateStoreContacts', {
                        method: 'POST', credentials: 'same-origin',
                        headers: { 'X-Requested-With': 'XMLHttpRequest' },
                        body: fd2
                    }).then(function (r) { return r.json().then(function (j) { return { ok: r.ok, json: j, label: 'social' }; }); }));
                }
            }

            Promise.all(calls).then(function (results) {
                var fail = results.find(function (r) { return !r.ok; });
                if (fail) {
                    var msg = (fail.json && (fail.json.error || firstValidationError(fail.json.errors))) || 'Yenilənmədi.';
                    showToast(msg, 'error');
                } else {
                    showToast('Bütün dəyişikliklər yadda saxlanıldı', 'success');
                }
            })
            .catch(function () { showToast('Şəbəkə xətası.', 'error'); })
            .finally(function () {
                saveBtn.dataset.busy = '';
                saveBtn.classList.remove('is-loading');
            });
        });
    }

    /* ----------- Change Password modal (shared between Account + Seller) ----------- */
    function setupChangePasswordModal() {
        var modal = document.getElementById('sbChangePasswordModal');
        if (!modal) return;
        var form = modal.querySelector('[data-change-password-form]');
        var errBox = modal.querySelector('[data-change-password-error]');
        var submit = modal.querySelector('[data-change-password-submit]');

        function open() {
            modal.classList.add('is-open');
            modal.setAttribute('aria-hidden', 'false');
            document.body.style.overflow = 'hidden';
            setTimeout(function () {
                var first = modal.querySelector('input[name="currentPassword"]');
                if (first) first.focus();
            }, 80);
        }
        function close() {
            modal.classList.remove('is-open');
            modal.setAttribute('aria-hidden', 'true');
            document.body.style.overflow = '';
            if (form) form.reset();
            if (errBox) { errBox.hidden = true; errBox.textContent = ''; }
        }

        document.querySelectorAll('[data-change-password-open]').forEach(function (btn) {
            btn.addEventListener('click', open);
        });
        modal.querySelectorAll('[data-modal-close]').forEach(function (btn) {
            btn.addEventListener('click', close);
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && modal.classList.contains('is-open')) close();
        });

        if (form) {
            form.addEventListener('submit', function (e) {
                e.preventDefault();
                if (errBox) { errBox.hidden = true; errBox.textContent = ''; }
                if (submit) submit.classList.add('is-loading');

                var fd = new FormData(form);
                fetch('/Account/ChangePassword', {
                    method: 'POST', credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: fd
                })
                .then(function (res) { return res.json().then(function (j) { return { ok: res.ok, json: j }; }); })
                .then(function (r) {
                    if (!r.ok) {
                        var msg = (r.json && (r.json.error || firstValidationError(r.json.errors))) || 'Parol yenilənmədi.';
                        if (errBox) { errBox.textContent = msg; errBox.hidden = false; }
                        return;
                    }
                    showToast('Parol yeniləndi', 'success');
                    close();
                })
                .catch(function () {
                    if (errBox) { errBox.textContent = 'Şəbəkə xətası.'; errBox.hidden = false; }
                })
                .finally(function () { if (submit) submit.classList.remove('is-loading'); });
            });
        }
    }

    /* -----------------------------------------------------------
       Addresses (Phase 6b) — used on /Account/Addresses + Seller Profile
       - One modal, two consumers
       - Server-rendered initial list; we mutate DOM after successful CRUD
       - All actions require an anti-forgery token
       ----------------------------------------------------------- */
    function setupAddresses() {
        var modal   = document.getElementById('sbAddressModal');
        var list    = document.querySelector('[data-addr-list]');
        var emptyEl = document.querySelector('[data-addr-empty]');
        if (!modal || !list) return;

        var form          = modal.querySelector('[data-addr-form]');
        var titleEl       = modal.querySelector('[data-addr-modal-title]');
        var submitBtn     = modal.querySelector('[data-addr-submit]');
        var submitLabelEl = modal.querySelector('[data-addr-submit-label]');
        var errBox        = modal.querySelector('[data-addr-form-error]');
        var idEl          = modal.querySelector('[data-addr-id]');
        if (!form) return;

        var editingId = null;

        // ---------- modal lifecycle ----------
        function open() {
            modal.classList.add('is-open');
            modal.setAttribute('aria-hidden', 'false');
            document.body.style.overflow = 'hidden';
            setTimeout(function () {
                var first = form.querySelector('input[name="title"]');
                if (first) first.focus();
            }, 80);
        }
        function close() {
            modal.classList.remove('is-open');
            modal.setAttribute('aria-hidden', 'true');
            document.body.style.overflow = '';
            if (errBox) { errBox.hidden = true; errBox.textContent = ''; }
        }
        function resetForm() {
            form.reset();
            editingId = null;
            if (idEl) idEl.value = '';
            if (titleEl) titleEl.textContent = 'Yeni ünvan';
            if (submitLabelEl) submitLabelEl.textContent = 'Yadda saxla';
        }
        function fillForm(addr) {
            editingId = addr.id;
            if (idEl) idEl.value = String(addr.id);
            form.querySelector('input[name="title"]').value       = addr.title || '';
            form.querySelector('input[name="fullName"]').value    = addr.fullName || '';
            form.querySelector('input[name="phone"]').value       = addr.phone || '';
            form.querySelector('input[name="city"]').value        = addr.city || '';
            form.querySelector('input[name="district"]').value    = addr.district || '';
            form.querySelector('input[name="postalCode"]').value  = addr.postalCode || '';
            form.querySelector('textarea[name="addressLine"]').value = addr.addressLine || '';
            form.querySelector('textarea[name="notes"]').value    = addr.notes || '';
            var defCb = form.querySelector('[data-addr-default-cb]');
            if (defCb) defCb.checked = !!addr.isDefault;

            if (titleEl) titleEl.textContent = 'Ünvanı redaktə et';
            if (submitLabelEl) submitLabelEl.textContent = 'Yadda saxla';
        }

        // ---------- DOM helpers ----------
        function refreshEmptyState() {
            var anyCard = list.querySelector('.sb-addr-card');
            if (!emptyEl) return;
            emptyEl.hidden = !!anyCard;
        }

        function escapeHtml(s) {
            if (s == null) return '';
            return String(s)
                .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
        }

        function renderCard(addr) {
            var defaultBadge = addr.isDefault
                ? '<span class="sb-addr-card__badge">Defolt</span>'
                : '';
            var setDefaultBtn = addr.isDefault ? '' :
                '<button type="button" class="sb-addr-card__btn" data-addr-set-default aria-label="Defolt et" title="Defolt et">' +
                '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.7"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>' +
                '</button>';
            var districtPart = addr.district ? ', ' + escapeHtml(addr.district) : '';
            var postalPart = addr.postalCode
                ? '<span class="sb-addr-card__postal" data-addr-postal>· ' + escapeHtml(addr.postalCode) + '</span>'
                : '';
            var notesPart = addr.notes
                ? '<p class="sb-addr-card__notes" data-addr-notes>' +
                  '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.7"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg> ' +
                  escapeHtml(addr.notes) + '</p>'
                : '';

            var html =
                '<article class="sb-addr-card ' + (addr.isDefault ? 'is-default' : '') + '" data-address-id="' + addr.id + '">' +
                  '<header class="sb-addr-card__head">' +
                    '<div class="sb-addr-card__title">' +
                      '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.7"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0118 0z"/><circle cx="12" cy="10" r="3"/></svg>' +
                      '<strong data-addr-title>' + escapeHtml(addr.title) + '</strong>' +
                      defaultBadge +
                    '</div>' +
                    '<div class="sb-addr-card__actions">' +
                      setDefaultBtn +
                      '<button type="button" class="sb-addr-card__btn" data-addr-edit aria-label="Redaktə et" title="Redaktə et">' +
                      '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.7"><path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7"/><path d="M18.5 2.5a2.12 2.12 0 013 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>' +
                      '</button>' +
                      '<button type="button" class="sb-addr-card__btn sb-addr-card__btn--danger" data-addr-delete aria-label="Sil" title="Sil">' +
                      '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.7"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2"/><line x1="10" y1="11" x2="10" y2="17"/><line x1="14" y1="11" x2="14" y2="17"/></svg>' +
                      '</button>' +
                    '</div>' +
                  '</header>' +
                  '<div class="sb-addr-card__body">' +
                    '<p class="sb-addr-card__name" data-addr-name>' + escapeHtml(addr.fullName) + '</p>' +
                    '<p class="sb-addr-card__phone" data-addr-phone>' +
                      '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.7"><path d="M22 16.92v3a2 2 0 01-2.18 2 19.79 19.79 0 01-8.63-3.07 19.5 19.5 0 01-6-6 19.79 19.79 0 01-3.07-8.67A2 2 0 014.11 2h3a2 2 0 012 1.72c.13.96.37 1.9.72 2.81a2 2 0 01-.45 2.11L8.09 9.91a16 16 0 006 6l1.27-1.27a2 2 0 012.11-.45c.91.35 1.85.59 2.81.72A2 2 0 0122 16.92z"/></svg> ' +
                      escapeHtml(addr.phone) + '</p>' +
                    '<p class="sb-addr-card__line" data-addr-line>' +
                      '<span data-addr-city>' + escapeHtml(addr.city) + '</span>' + districtPart + ' · <span data-addr-text>' + escapeHtml(addr.addressLine) + '</span>' +
                      postalPart +
                    '</p>' +
                    notesPart +
                  '</div>' +
                '</article>';

            var tpl = document.createElement('template');
            tpl.innerHTML = html.trim();
            return tpl.content.firstChild;
        }

        function replaceAllDefaults(newDefaultId) {
            // Strip "Defolt" badge + restore "set default" buttons on previous default cards.
            list.querySelectorAll('.sb-addr-card.is-default').forEach(function (card) {
                if (parseInt(card.getAttribute('data-address-id'), 10) === newDefaultId) return;
                card.classList.remove('is-default');
                var badge = card.querySelector('.sb-addr-card__badge');
                if (badge) badge.remove();
                var actions = card.querySelector('.sb-addr-card__actions');
                if (actions && !actions.querySelector('[data-addr-set-default]')) {
                    var btn = document.createElement('button');
                    btn.type = 'button';
                    btn.className = 'sb-addr-card__btn';
                    btn.setAttribute('data-addr-set-default', '');
                    btn.setAttribute('aria-label', 'Defolt et');
                    btn.setAttribute('title', 'Defolt et');
                    btn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.7"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>';
                    actions.insertBefore(btn, actions.firstChild);
                }
            });
        }

        // ---------- network ----------
        function post(url, fd) {
            var token = getAntiForgeryToken();
            if (token && !fd.has('__RequestVerificationToken')) fd.append('__RequestVerificationToken', token);
            return fetch(url, {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: fd
            }).then(function (res) { return res.json().then(function (j) { return { ok: res.ok, json: j }; }); });
        }

        // ---------- delegated clicks on the list ----------
        list.addEventListener('click', function (e) {
            var card = e.target.closest('[data-address-id]');
            if (!card) return;
            var id = parseInt(card.getAttribute('data-address-id'), 10);
            if (!id) return;

            if (e.target.closest('[data-addr-edit]')) {
                e.preventDefault();
                resetForm();
                // Pull values straight off the DOM — avoid a round-trip.
                var addr = {
                    id: id,
                    title:       (card.querySelector('[data-addr-title]') || {}).textContent || '',
                    fullName:    (card.querySelector('[data-addr-name]')  || {}).textContent || '',
                    phone:       ((card.querySelector('[data-addr-phone]') || {}).textContent || '').trim(),
                    city:        (card.querySelector('[data-addr-city]')  || {}).textContent || '',
                    addressLine: (card.querySelector('[data-addr-text]')  || {}).textContent || '',
                    postalCode:  ((card.querySelector('[data-addr-postal]') || {}).textContent || '').replace(/^·\s*/, ''),
                    notes:       ((card.querySelector('[data-addr-notes]') || {}).textContent || '').trim(),
                    isDefault:   card.classList.contains('is-default')
                };
                fillForm(addr);
                open();
                return;
            }
            if (e.target.closest('[data-addr-delete]')) {
                e.preventDefault();
                if (!window.confirm('Bu ünvanı silmək istədiyinə əminsən?')) return;
                var fd = new FormData();
                fd.append('id', String(id));
                post('/Address/Delete', fd).then(function (r) {
                    if (!r.ok) {
                        showToast((r.json && r.json.error) || 'Silinmədi.', 'error');
                        return;
                    }
                    card.style.transition = 'opacity .25s ease, transform .25s ease';
                    card.style.opacity = '0';
                    card.style.transform = 'translateY(-4px)';
                    setTimeout(function () {
                        var wasDefault = card.classList.contains('is-default');
                        card.remove();
                        // If we removed the default, the server promotes the next one —
                        // re-fetch the list to reflect new default state.
                        if (wasDefault) {
                            refreshFromServer();
                        } else {
                            refreshEmptyState();
                        }
                    }, 230);
                    showToast('Ünvan silindi', 'success');
                }).catch(function () { showToast('Şəbəkə xətası.', 'error'); });
                return;
            }
            if (e.target.closest('[data-addr-set-default]')) {
                e.preventDefault();
                var fd2 = new FormData();
                fd2.append('id', String(id));
                post('/Address/SetDefault', fd2).then(function (r) {
                    if (!r.ok) {
                        showToast((r.json && r.json.error) || 'Yenilənmədi.', 'error');
                        return;
                    }
                    replaceAllDefaults(id);
                    card.classList.add('is-default');
                    // Drop the star button + add a badge to this card.
                    var setBtn = card.querySelector('[data-addr-set-default]');
                    if (setBtn) setBtn.remove();
                    var title = card.querySelector('.sb-addr-card__title');
                    if (title && !title.querySelector('.sb-addr-card__badge')) {
                        var span = document.createElement('span');
                        span.className = 'sb-addr-card__badge';
                        span.textContent = 'Defolt';
                        title.appendChild(span);
                    }
                    // Move the default card to the top of the list.
                    list.insertBefore(card, list.firstChild);
                    showToast('Defolt ünvan dəyişdi', 'success');
                }).catch(function () { showToast('Şəbəkə xətası.', 'error'); });
            }
        });

        // ---------- "Yeni ünvan" triggers ----------
        document.querySelectorAll('[data-addr-modal-open]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                resetForm();
                open();
            });
        });

        // ---------- modal close ----------
        modal.querySelectorAll('[data-modal-close]').forEach(function (btn) {
            btn.addEventListener('click', close);
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && modal.classList.contains('is-open')) close();
        });

        // ---------- form submit ----------
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            if (errBox) { errBox.hidden = true; errBox.textContent = ''; }
            if (submitBtn) submitBtn.classList.add('is-loading');

            var fd = new FormData(form);
            // Normalise the checkbox payload to a real bool the model binder understands.
            var defCb = form.querySelector('[data-addr-default-cb]');
            fd.delete('isDefault');
            fd.append('isDefault', defCb && defCb.checked ? 'true' : 'false');

            var url = editingId ? '/Address/Update' : '/Address/Create';
            if (!editingId) fd.delete('id');

            post(url, fd).then(function (r) {
                if (!r.ok) {
                    var msg = (r.json && (r.json.error || firstValidationError(r.json.errors))) || 'Yadda saxlanılmadı.';
                    if (errBox) { errBox.textContent = msg; errBox.hidden = false; }
                    else showToast(msg, 'error');
                    return;
                }

                var saved = r.json && r.json.item;
                if (!saved) {
                    showToast('Yadda saxlandı', 'success');
                    close();
                    return;
                }

                if (editingId) {
                    // Replace existing card in place.
                    var existing = list.querySelector('[data-address-id="' + saved.id + '"]');
                    var fresh = renderCard(saved);
                    if (existing) existing.replaceWith(fresh);
                    else list.appendChild(fresh);
                    if (saved.isDefault) {
                        replaceAllDefaults(saved.id);
                        list.insertBefore(fresh, list.firstChild);
                    }
                } else {
                    // Prepend the new card.
                    if (saved.isDefault) replaceAllDefaults(saved.id);
                    var node = renderCard(saved);
                    list.insertBefore(node, list.firstChild);
                }

                showToast(editingId ? 'Ünvan yeniləndi' : 'Yeni ünvan əlavə olundu', 'success');
                close();
                resetForm();
                refreshEmptyState();
            }).catch(function () {
                if (errBox) { errBox.textContent = 'Şəbəkə xətası.'; errBox.hidden = false; }
            }).finally(function () {
                if (submitBtn) submitBtn.classList.remove('is-loading');
            });
        });

        // ---------- after a delete-of-default, refetch authoritative state ----------
        function refreshFromServer() {
            fetch('/Address/Mine', {
                method: 'GET',
                credentials: 'same-origin',
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(function (res) { return res.ok ? res.json() : null; })
            .then(function (data) {
                if (!data || !data.ok) return;
                list.innerHTML = '';
                (data.items || []).forEach(function (a) { list.appendChild(renderCard(a)); });
                refreshEmptyState();
            }).catch(function () { /* leave UI as-is */ });
        }

        // Initial sync of empty-state flag in case it drifted server-side.
        refreshEmptyState();
    }
})();
