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
        safe('setupStoryCreate',         setupStoryCreate);
        safe('setupChatWidget',          setupChatWidget);
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
        safe('setupContactClickTracking',setupContactClickTracking);
        safe('setupPackagesModal',       setupPackagesModal);
        safe('setupShareRow',            setupShareRow);
        safe('setupPdpTabs',             setupPdpTabs);
        safe('setupTileRails',           setupTileRails);
        safe('setupShopsSearch',         setupShopsSearch);
        safe('setupAvatarUploader',      setupAvatarUploader);
        safe('setupAccountProfileForm',  setupAccountProfileForm);
        safe('setupSellerProfile',       setupSellerProfile);
        safe('setupChangePasswordModal', setupChangePasswordModal);
        safe('setupAddresses',           setupAddresses);
        safe('setupSellerProducts',      setupSellerProducts);
        safe('setupProductImageUploader',setupProductImageUploader);
        safe('setupStoreSettings',       setupStoreSettings);
        safe('setupStorePage',           setupStorePage);
        safe('setupAdmin',               setupAdmin);

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
        // Each option is now a real <form> POST to /Culture/Set (see _Layout).
        // We don't preventDefault — letting the form submit performs the actual
        // culture switch + cookie set + redirect back to the current page. No
        // cosmetic-only swap here; the server re-renders with the chosen language.
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
        // NOTE: do not early-return when there are no [data-wish-toggle] hearts.
        // The wishlist page (/Wishlist) has only [data-wishlist-remove] buttons
        // and no toggle hearts — an early return here would leave its remove
        // buttons unwired. forEach over an empty NodeList is a harmless no-op.
        var buttons = document.querySelectorAll('[data-wish-toggle]');

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
    /* ----------- Story viewer (Phase 14.1 — image + video) -----------
       Handles the home-page "Hekayələr" rail click → opens fullscreen
       modal, plays each story (image: fixed 5s timer, video: real duration
       from <video>), advances on tap/arrow, closes on Esc.

       Data is read from a JSON <script id="sbStoriesData"> block emitted
       by the view. Each entry: { storeId, storeName, storeProfileImageUrl,
       stories: [{ id, mediaUrl, mediaType, durationSeconds, thumbnailUrl,
       caption, linkUrl }] }. mediaType matches the server enum: "Image" |
       "Video". */
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
        var backdropEl = document.getElementById('sbStoryBackdrop');
        var vidEl      = document.getElementById('sbStoryVideo');
        var captionEl  = document.getElementById('sbStoryCaption');
        var avatarEl   = document.getElementById('sbStoryAvatar');
        var nameEl     = document.getElementById('sbStoryStoreName');
        var closeBtn   = document.getElementById('sbStoryClose');
        var muteBtn    = document.getElementById('sbStoryMute');
        var tapPrev    = document.getElementById('sbStoryTapPrev');
        var tapNext    = document.getElementById('sbStoryTapNext');
        var replyForm  = document.getElementById('sbStoryReply');

        var storeIdx = 0;
        var storyIdx = 0;
        var startTs = 0;
        var elapsedAtPause = 0;
        var paused = false;
        var IMAGE_MS = 5000;
        var currentDurationMs = IMAGE_MS;
        var rafId = null;
        var currentMode = 'image'; // or 'video'
        var muted = true;          // start muted (autoplay policy)

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
            if (fill) fill.style.right = (100 - Math.max(0, Math.min(100, percent))) + '%';
        }
        function stopMedia() {
            cancelAnimationFrame(rafId);
            // Pause + reset video so it doesn't bleed audio into next story.
            try { vidEl.pause(); } catch (_) {}
            vidEl.removeAttribute('src');
            vidEl.load();
            vidEl.hidden = true;
            imgEl.hidden = true;
            imgEl.removeAttribute('src');
            if (muteBtn) muteBtn.hidden = true;
        }
        function play() {
            stopMedia();
            var s = data[storeIdx].stories[storyIdx];
            captionEl.textContent = s.caption || '';
            renderProgressBars();
            elapsedAtPause = 0;
            paused = false;

            // Accept either enum name ("Image"/"Video") or integer (0/1). The
            // legacy `(x || 'Image').toLowerCase()` shortcut crashed when x was
            // the integer 1 because Number has no toLowerCase().
            var rawType = s.mediaType;
            var type = (rawType === 1 || rawType === '1' ||
                        (typeof rawType === 'string' && rawType.toLowerCase() === 'video')) ? 'video' : 'image';
            // Fire-and-forget view tracking (anonymous endpoint).
            fetch('/Stories/View/' + s.id, { method: 'POST' }).catch(function () {});

            if (type === 'video') {
                currentMode = 'video';
                // Duration from server if known; fall back to 5s placeholder
                // until video metadata loads (then re-set from videoDuration).
                currentDurationMs = (s.durationSeconds && s.durationSeconds > 0)
                    ? s.durationSeconds * 1000
                    : IMAGE_MS;
                vidEl.hidden = false;
                vidEl.muted = muted;
                vidEl.src = s.mediaUrl;
                // Portrait video fills the frame (cover) — no blurred backdrop.
                if (backdropEl) backdropEl.classList.remove('is-on');
                if (muteBtn) muteBtn.hidden = false;
                reflectMuteBtn();
                // playsinline + muted ensures autoplay survives browser policies.
                vidEl.play().catch(function () { /* autoplay blocked — user tap will retry */ });
                vidEl.onloadedmetadata = function () {
                    if (isFinite(vidEl.duration) && vidEl.duration > 0) {
                        currentDurationMs = vidEl.duration * 1000;
                    }
                };
                vidEl.onended = function () { advance(1); };
                startTs = performance.now();
                rafId = requestAnimationFrame(tick);
            } else {
                currentMode = 'image';
                currentDurationMs = IMAGE_MS;
                imgEl.hidden = false;
                imgEl.src = s.mediaUrl;
                // Blurred fill from the same photo so a non-9:16 image never
                // shows black bars (the photo stays crisp + un-cropped on top).
                if (backdropEl) {
                    backdropEl.style.backgroundImage = "url('" + s.mediaUrl + "')";
                    backdropEl.classList.add('is-on');
                }
                startTs = performance.now();
                rafId = requestAnimationFrame(tick);
            }
        }
        function tick() {
            if (paused) return;
            // For videos, prefer the real currentTime over wall-clock so the
            // bar matches buffering hiccups. For images, use wall-clock.
            var elapsed;
            if (currentMode === 'video' && isFinite(vidEl.duration) && vidEl.duration > 0) {
                elapsed = vidEl.currentTime * 1000;
            } else {
                elapsed = performance.now() - startTs + elapsedAtPause;
            }
            var pct = currentDurationMs > 0 ? (elapsed / currentDurationMs) * 100 : 0;
            setFill(pct);
            if (elapsed >= currentDurationMs && currentMode !== 'video') {
                // Video advance is handled by its `onended` handler so a slow
                // network doesn't cause a double-advance.
                advance(1);
                return;
            }
            rafId = requestAnimationFrame(tick);
        }
        function advance(delta) {
            cancelAnimationFrame(rafId);
            var stories = data[storeIdx].stories;
            var ni = storyIdx + delta;
            if (ni >= stories.length) {
                if (storeIdx + 1 >= data.length) { close(); return; }
                storeIdx += 1;
                storyIdx = 0;
                renderHeader();
            } else if (ni < 0) {
                if (storeIdx === 0) { storyIdx = 0; }
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
            // Reply input visible only when we know the owner — wired in Phase 14.7.
            if (replyForm) replyForm.hidden = !data[storeIdx] || !data[storeIdx].storeOwnerId;
            renderHeader();
            play();
        }
        function close() {
            stopMedia();
            viewer.classList.remove('is-open');
            viewer.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('sb-locked');
            var item = rail.querySelector('[data-story-store-id="' + data[storeIdx].storeId + '"] .sb-story-item__ring');
            if (item) item.classList.add('is-viewed');
        }
        function reflectMuteBtn() {
            if (!muteBtn) return;
            var on = muteBtn.querySelector('.sb-story-viewer__mute-on');
            var off = muteBtn.querySelector('.sb-story-viewer__mute-off');
            if (on) on.hidden = !muted;
            if (off) off.hidden = muted;
        }

        rail.querySelectorAll('[data-story-store-id]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var sid = parseInt(btn.getAttribute('data-story-store-id'), 10);
                var idx = data.findIndex(function (x) { return x.storeId === sid; });
                open(idx);
            });
        });

        if (closeBtn) closeBtn.addEventListener('click', close);
        if (tapNext) tapNext.addEventListener('click', function () { advance(1); });
        if (tapPrev) tapPrev.addEventListener('click', function () { advance(-1); });
        if (muteBtn) muteBtn.addEventListener('click', function () {
            muted = !muted;
            vidEl.muted = muted;
            reflectMuteBtn();
        });

        // ---- Story-reply submit (Phase 14.7) ----
        // Sends a StoryReply-typed chat message to the store owner, then
        // closes the viewer and opens the chat widget on that conversation
        // so the user sees their reply land.
        if (replyForm) {
            var replyInput = replyForm.querySelector('input[type="text"]');
            replyForm.addEventListener('submit', function (e) {
                e.preventDefault();
                if (!replyInput) return;
                var text = (replyInput.value || '').trim();
                if (!text) return;
                var store = data[storeIdx];
                var story = store && store.stories && store.stories[storyIdx];
                if (!store || !story || !store.storeOwnerId) return;

                // Pause playback while we send.
                paused = true; cancelAnimationFrame(rafId);
                try { vidEl.pause(); } catch (_) {}

                // Use SignalR hub when available (snappier), REST as fallback.
                var dto = {
                    recipientUserId: store.storeOwnerId,
                    messageType: 2,           // StoryReply (DAL enum int)
                    body: text,
                    storyId: story.id
                };
                var done = function () {
                    replyInput.value = '';
                    close();
                    // Open the widget on this conversation so the buyer sees
                    // their message + can keep chatting.
                    var w = document.querySelector('[data-chat-widget]');
                    if (w && w.__sbOpenWithUser) {
                        var panel = w.querySelector('[data-chat-panel]');
                        if (panel) panel.hidden = false;
                        w.__sbOpenWithUser(store.storeOwnerId);
                    }
                };
                var fail = function () {
                    alert('Cavab göndərilə bilmədi. Qoşulduğunuzu yoxlayın.');
                    paused = false;
                    if (currentMode === 'video') { try { vidEl.play(); } catch (_) {} }
                    startTs = performance.now();
                    rafId = requestAnimationFrame(tick);
                };
                if (window.__sbChat && window.__sbChat.connected && window.__sbChat.conn) {
                    window.__sbChat.conn.invoke('SendMessage', dto).then(done).catch(function () {
                        // Fall through to REST.
                        fetch('/api/chat/send', {
                            method: 'POST',
                            credentials: 'same-origin',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(dto)
                        }).then(function (r) { return r.ok ? done() : fail(); }).catch(fail);
                    });
                } else {
                    fetch('/api/chat/send', {
                        method: 'POST',
                        credentials: 'same-origin',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(dto)
                    }).then(function (r) { return r.ok ? done() : fail(); }).catch(fail);
                }
            });
        }

        document.addEventListener('keydown', function (e) {
            if (!viewer.classList.contains('is-open')) return;
            if (e.key === 'Escape') close();
            else if (e.key === 'ArrowRight') advance(1);
            else if (e.key === 'ArrowLeft') advance(-1);
            else if (e.key === ' ' || e.key === 'Spacebar') {
                // Pause/resume on space.
                paused = !paused;
                if (currentMode === 'video') { if (paused) vidEl.pause(); else vidEl.play(); }
                if (!paused) { startTs = performance.now(); rafId = requestAnimationFrame(tick); }
                e.preventDefault();
            }
        });
    }

    /* ----------- Seller story upload preview (Phase 14.1) ----------- */
    function setupStoryCreate() {
        var form = document.querySelector('[data-story-create]');
        if (!form) return;
        var input = form.querySelector('[data-story-file]');
        var empty = form.querySelector('[data-story-empty]');
        var preview = form.querySelector('[data-story-preview]');
        var previewImg = form.querySelector('[data-story-preview-img]');
        var previewVid = form.querySelector('[data-story-preview-vid]');
        var clearBtn = form.querySelector('[data-story-clear]');
        var durationField = form.querySelector('[data-story-duration]');
        var submitBtn = form.querySelector('[data-story-submit]');

        function reset() {
            input.value = '';
            previewImg.removeAttribute('src');
            previewImg.hidden = true;
            previewVid.removeAttribute('src');
            previewVid.hidden = true;
            preview.hidden = true;
            empty.hidden = false;
            if (durationField) durationField.value = '';
            if (submitBtn) submitBtn.disabled = true;
        }
        function showError(msg) {
            // Cheap inline error — replace with proper toast once chat lands.
            alert(msg);
            reset();
        }

        input.addEventListener('change', function () {
            var f = input.files && input.files[0];
            if (!f) { reset(); return; }
            var isVideo = (f.type || '').indexOf('video/') === 0
                || /\.(mp4|webm|mov|m4v)$/i.test(f.name || '');
            var maxBytes = isVideo ? 30 * 1024 * 1024 : 10 * 1024 * 1024;
            if (f.size > maxBytes) {
                showError(isVideo ? 'Video maksimum 30 MB ola bilər.' : 'Şəkil maksimum 10 MB ola bilər.');
                return;
            }
            var url = URL.createObjectURL(f);
            empty.hidden = true;
            preview.hidden = false;

            if (isVideo) {
                previewImg.hidden = true;
                previewImg.removeAttribute('src');
                previewVid.hidden = false;
                previewVid.src = url;
                previewVid.onloadedmetadata = function () {
                    var dur = Math.round(previewVid.duration || 0);
                    if (!isFinite(dur) || dur <= 0) {
                        showError('Video uzunluğu oxuna bilmədi.');
                        return;
                    }
                    if (dur > 30) {
                        showError('Video maksimum 30 saniyə ola bilər.');
                        return;
                    }
                    if (durationField) durationField.value = String(dur);
                    if (submitBtn) submitBtn.disabled = false;
                };
            } else {
                previewVid.hidden = true;
                previewVid.removeAttribute('src');
                previewImg.hidden = false;
                previewImg.src = url;
                if (durationField) durationField.value = '';
                if (submitBtn) submitBtn.disabled = false;
            }
        });
        if (clearBtn) clearBtn.addEventListener('click', function (e) {
            e.preventDefault();
            reset();
        });
    }

    /* -----------------------------------------------------------
       Chat widget + fullpage messages (Phase 14.6)
       ----------------------------------------------------------- */
    function setupChatWidget() {
        // Both the floating widget (data-chat-widget) and the fullpage Messages
        // view (data-chat-fullpage) share this handler. They reuse selectors
        // (data-chat-*) inside their root so the same logic drives both.
        var widget = document.querySelector('[data-chat-widget]');
        var fullpage = document.querySelector('[data-chat-fullpage]');
        if (!widget && !fullpage) return;
        if (typeof signalR === 'undefined') {
            // SignalR client isn't loaded — likely anonymous page. Bail.
            return;
        }

        // ---- Shared connection (one per tab) ----
        if (!window.__sbChat) {
            window.__sbChat = {
                conn: null,
                connected: false,
                conversations: [],
                openConvId: null,
                typingPeers: {},
                presence: {},
                handlers: { onMessage: [], onPresence: [], onTyping: [], onMessagesRead: [] }
            };
        }
        var hub = window.__sbChat;

        function connectIfNeeded() {
            if (hub.conn) return Promise.resolve();
            hub.conn = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/chat')
                .withAutomaticReconnect()
                .build();
            hub.conn.on('MessageReceived', function (msg) {
                hub.handlers.onMessage.forEach(function (fn) { try { fn(msg); } catch (_) {} });
            });
            hub.conn.on('UserPresence', function (e) {
                hub.presence[e.userId] = !!e.online;
                hub.handlers.onPresence.forEach(function (fn) { try { fn(e); } catch (_) {} });
            });
            hub.conn.on('PeerTyping', function (e) {
                hub.handlers.onTyping.forEach(function (fn) { try { fn(e); } catch (_) {} });
            });
            hub.conn.on('MessagesRead', function (e) {
                hub.handlers.onMessagesRead.forEach(function (fn) { try { fn(e); } catch (_) {} });
            });
            return hub.conn.start().then(function () {
                hub.connected = true;
            }).catch(function () { hub.connected = false; });
        }

        // ---- REST helpers (Json fetch with anti-forgery header where needed) ----
        function getCsrf() {
            var el = document.querySelector('input[name="__RequestVerificationToken"]');
            return el ? el.value : '';
        }
        function jget(url) {
            return fetch(url, { credentials: 'same-origin' }).then(function (r) { return r.ok ? r.json() : Promise.reject(r); });
        }
        function jpost(url, body) {
            return fetch(url, {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json' },
                body: body ? JSON.stringify(body) : null
            }).then(function (r) { return r.ok ? (r.status === 204 ? null : r.json()) : Promise.reject(r); });
        }
        function jpostForm(url, formData) {
            return fetch(url, {
                method: 'POST',
                credentials: 'same-origin',
                body: formData
            }).then(function (r) { return r.ok ? r.json() : Promise.reject(r); });
        }
        function escapeHtml(s) {
            if (s == null) return '';
            return String(s).replace(/[&<>"']/g, function (c) {
                return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
            });
        }
        function fmtTime(iso) {
            try {
                var d = new Date(iso);
                var h = d.getHours(), m = d.getMinutes();
                return (h < 10 ? '0' : '') + h + ':' + (m < 10 ? '0' : '') + m;
            } catch (_) { return ''; }
        }

        // ---- Per-root binding (widget AND/OR fullpage can both be present) ----
        function bind(root, options) {
            var isWidget = !!options.isWidget;
            var convListEl = root.querySelector('[data-chat-conversations]');
            var emptyEl = root.querySelector('[data-chat-empty]');
            var messagesEl = root.querySelector('[data-chat-messages]');
            var composerEl = root.querySelector('[data-chat-composer]');
            var inputEl = root.querySelector('[data-chat-input]');
            var attachEl = root.querySelector('[data-chat-attach]');
            var typingEl = root.querySelector('[data-chat-typing]');
            var backBtn = root.querySelector('[data-chat-back]');
            var listView = root.querySelector('[data-chat-list]');
            var threadView = root.querySelector('[data-chat-thread]');
            var titleEl = root.querySelector('[data-chat-title]');
            var placeholderEl = root.querySelector('[data-chat-placeholder]');
            var threadHeadEl = root.querySelector('[data-chat-thread-head]');
            var peerNameEl = root.querySelector('[data-chat-peer-name]');
            var peerAvatarEl = root.querySelector('[data-chat-peer-avatar]');
            var peerStatusEl = root.querySelector('[data-chat-peer-status]');

            var currentConv = null;

            function showList() {
                if (isWidget) {
                    if (listView) listView.hidden = false;
                    if (threadView) threadView.hidden = true;
                    if (backBtn) backBtn.hidden = true;
                    if (titleEl) titleEl.textContent = 'Mesajlar';
                }
                // fullpage: thread stays visible (right pane), no toggle needed
            }
            function showThread(conv) {
                currentConv = conv;
                if (isWidget) {
                    if (listView) listView.hidden = true;
                    if (threadView) threadView.hidden = false;
                    if (backBtn) backBtn.hidden = false;
                    if (titleEl) titleEl.textContent = conv.peerFullName;
                } else {
                    if (placeholderEl) placeholderEl.hidden = true;
                    if (threadHeadEl) threadHeadEl.hidden = false;
                    if (messagesEl) messagesEl.hidden = false;
                    if (composerEl) composerEl.hidden = false;
                    if (peerNameEl) peerNameEl.textContent = conv.peerFullName;
                    if (peerAvatarEl) {
                        peerAvatarEl.innerHTML = '';
                        if (conv.peerAvatarUrl) {
                            var im = document.createElement('img');
                            im.src = conv.peerAvatarUrl;
                            peerAvatarEl.appendChild(im);
                        } else {
                            peerAvatarEl.textContent = (conv.peerFullName || '?').substring(0, 1).toUpperCase();
                        }
                    }
                    if (peerStatusEl) peerStatusEl.textContent = hub.presence[conv.peerUserId] ? 'Online' : 'Offline';
                }
            }

            function renderConversations() {
                if (!convListEl) return;
                convListEl.innerHTML = '';
                if (!hub.conversations.length) {
                    if (emptyEl) emptyEl.hidden = false;
                    return;
                }
                if (emptyEl) emptyEl.hidden = true;
                hub.conversations.forEach(function (c) {
                    var li = document.createElement('button');
                    li.type = 'button';
                    li.className = 'sb-chat-conv' + (currentConv && currentConv.id === c.id ? ' is-active' : '');
                    li.setAttribute('data-conv-id', String(c.id));
                    var avatar = c.peerAvatarUrl
                        ? '<img src="' + escapeHtml(c.peerAvatarUrl) + '" alt="" />'
                        : '<span>' + escapeHtml((c.peerFullName || '?').substring(0, 1).toUpperCase()) + '</span>';
                    var onlineDot = hub.presence[c.peerUserId]
                        ? '<span class="sb-chat-conv__dot" title="Online"></span>' : '';
                    var unread = c.unreadCount > 0
                        ? '<span class="sb-chat-conv__badge">' + c.unreadCount + '</span>' : '';
                    li.innerHTML =
                        '<span class="sb-chat-conv__avatar">' + avatar + onlineDot + '</span>' +
                        '<span class="sb-chat-conv__body">' +
                            '<strong>' + escapeHtml(c.peerFullName) + '</strong>' +
                            '<span>' + escapeHtml(c.lastMessagePreview || '(yeni söhbət)') + '</span>' +
                        '</span>' +
                        '<span class="sb-chat-conv__meta">' +
                            '<small>' + fmtTime(c.lastMessageAt) + '</small>' +
                            unread +
                        '</span>';
                    li.addEventListener('click', function () { openConversation(c); });
                    convListEl.appendChild(li);
                });
            }

            function renderMessage(msg, prepend) {
                if (!messagesEl) return;
                // Dedup guard: a message can arrive twice — once from a REST
                // .then() and once from the SignalR echo (both fire for the same
                // saved row). Skip if we already have a bubble with this id. This
                // also lets optimistic reconcile (below) call renderMessage safely
                // when the echo may already have rendered the row.
                if (msg.id != null && messagesEl.querySelector('[data-msg-id="' + msg.id + '"]')) return;
                var bubble = document.createElement('div');
                var mine = (msg.senderId === (root.getAttribute('data-me-id') || ''));
                // Resolve "mine" by sender vs. open conversation peer.
                if (currentConv) {
                    mine = msg.senderId !== currentConv.peerUserId;
                }
                bubble.className = 'sb-chat-msg ' + (mine ? 'is-mine' : 'is-peer');
                bubble.setAttribute('data-msg-id', String(msg.id));

                var body = '';
                if (msg.messageType === 1 || msg.messageType === 'Image') {
                    body = '<img class="sb-chat-msg__img" src="' + escapeHtml(msg.attachmentUrl || '') + '" alt="" />';
                    if (msg.body) body += '<div class="sb-chat-msg__text">' + escapeHtml(msg.body) + '</div>';
                } else if (msg.messageType === 2 || msg.messageType === 'StoryReply') {
                    var sp = msg.storyPreview || {};
                    var thumb = sp.thumbnailUrl || sp.mediaUrl || '';
                    body = '<div class="sb-chat-msg__story">' +
                        (thumb ? '<img src="' + escapeHtml(thumb) + '" alt="" />' : '<span>↩</span>') +
                        '<span>' + escapeHtml(sp.caption || 'Hekayə') + '</span>' +
                        '</div>' +
                        '<div class="sb-chat-msg__text">' + escapeHtml(msg.body || '') + '</div>';
                } else if (msg.messageType === 3 || msg.messageType === 'ProductShare') {
                    var pp = msg.productPreview || {};
                    body = '<div class="sb-chat-msg__product">' +
                        (pp.imageUrl ? '<img src="' + escapeHtml(pp.imageUrl) + '" alt="" />' : '') +
                        '<span>' + escapeHtml(pp.name || 'Məhsul') + '</span>' +
                        '</div>' +
                        (msg.body ? '<div class="sb-chat-msg__text">' + escapeHtml(msg.body) + '</div>' : '');
                } else {
                    body = '<div class="sb-chat-msg__text">' + escapeHtml(msg.body || '') + '</div>';
                }
                bubble.innerHTML = body +
                    '<span class="sb-chat-msg__time">' + fmtTime(msg.createdDate) +
                    (mine && msg.isReadByRecipient ? ' · <span class="sb-chat-msg__seen">Görüldü</span>' : '') +
                    '</span>';

                if (prepend) messagesEl.insertBefore(bubble, messagesEl.firstChild);
                else messagesEl.appendChild(bubble);
            }

            function loadConversations() {
                jget('/api/chat/conversations?pageSize=50').then(function (page) {
                    hub.conversations = (page && page.items) || [];
                    renderConversations();
                    refreshUnreadBadge();
                }).catch(function () {});
            }

            function openConversation(conv) {
                showThread(conv);
                hub.openConvId = conv.id;
                if (messagesEl) messagesEl.innerHTML = '';
                jget('/api/chat/conversations/' + conv.id + '/messages?pageSize=50').then(function (page) {
                    var items = (page && page.items) || [];
                    // API returns newest-first; render oldest at top.
                    items.slice().reverse().forEach(function (m) { renderMessage(m, false); });
                    if (messagesEl) messagesEl.scrollTop = messagesEl.scrollHeight;
                });
                // Mark as read
                jpost('/api/chat/conversations/' + conv.id + '/read', null).catch(function () {});
                conv.unreadCount = 0;
                renderConversations();
                refreshUnreadBadge();
            }

            // ---- Hub event hookups (one per bind) ----
            hub.handlers.onMessage.push(function (msg) {
                var convId = msg.conversationId;
                // Update conversation preview + bump to top.
                var conv = hub.conversations.find(function (c) { return c.id === convId; });
                if (conv) {
                    conv.lastMessageAt = msg.createdDate;
                    conv.lastMessagePreview = (msg.body || '').substring(0, 120);
                    if (!currentConv || currentConv.id !== convId) {
                        conv.unreadCount = (conv.unreadCount || 0) + (msg.senderId === conv.peerUserId ? 1 : 0);
                    }
                    hub.conversations = [conv].concat(hub.conversations.filter(function (c) { return c.id !== convId; }));
                    renderConversations();
                    refreshUnreadBadge();
                } else {
                    // New conversation we haven't loaded yet — refresh inbox.
                    loadConversations();
                }
                if (currentConv && currentConv.id === convId) {
                    renderMessage(msg, false);
                    if (messagesEl) messagesEl.scrollTop = messagesEl.scrollHeight;
                    // Auto-mark read since the thread is visible.
                    jpost('/api/chat/conversations/' + convId + '/read', null).catch(function () {});
                }
            });
            hub.handlers.onPresence.push(function () { renderConversations(); });
            hub.handlers.onTyping.push(function (e) {
                if (!currentConv || e.userId !== currentConv.peerUserId) return;
                if (typingEl) typingEl.hidden = !e.isTyping;
            });
            hub.handlers.onMessagesRead.push(function (e) {
                // Peer just read my messages in the open thread — flip every one
                // of my bubbles to "Görüldü" without waiting for a refresh.
                if (!currentConv || !messagesEl || e.conversationId !== currentConv.id) return;
                if (e.readerUserId && e.readerUserId !== currentConv.peerUserId) return;
                var times = messagesEl.querySelectorAll('.sb-chat-msg.is-mine .sb-chat-msg__time');
                for (var i = 0; i < times.length; i++) {
                    if (!times[i].querySelector('.sb-chat-msg__seen')) {
                        times[i].innerHTML += ' · <span class="sb-chat-msg__seen">Görüldü</span>';
                    }
                }
            });

            // ---- Composer (optimistic send) ----
            // Render the bubble instantly in a "sending" state, then reconcile
            // when the server confirms (replace temp row with the saved one) or
            // mark it "failed · retry" if both SignalR and the REST fallback fail.
            function renderOptimistic(clientId, text) {
                if (!messagesEl) return;
                var b = document.createElement('div');
                b.className = 'sb-chat-msg is-mine is-sending';
                b.setAttribute('data-client-id', clientId);
                b.setAttribute('data-msg-id', 'tmp-' + clientId);
                b.innerHTML =
                    '<div class="sb-chat-msg__text">' + escapeHtml(text) + '</div>' +
                    '<span class="sb-chat-msg__time">' + fmtTime(new Date().toISOString()) +
                    ' · <span class="sb-chat-msg__status">Göndərilir…</span></span>';
                messagesEl.appendChild(b);
                messagesEl.scrollTop = messagesEl.scrollHeight;
            }
            function reconcileOptimistic(clientId, saved) {
                var temp = messagesEl && messagesEl.querySelector('[data-client-id="' + clientId + '"]');
                if (temp) temp.remove();
                // renderMessage's dedup guard no-ops if the echo already rendered it.
                if (saved) { renderMessage(saved, false); if (messagesEl) messagesEl.scrollTop = messagesEl.scrollHeight; }
            }
            function markOptimisticFailed(clientId, retryFn) {
                var temp = messagesEl && messagesEl.querySelector('[data-client-id="' + clientId + '"]');
                if (!temp) return;
                temp.classList.remove('is-sending');
                temp.classList.add('is-failed');
                var status = temp.querySelector('.sb-chat-msg__status');
                if (status) {
                    status.textContent = 'Göndərilmədi · ';
                    var retry = document.createElement('button');
                    retry.type = 'button';
                    retry.className = 'sb-chat-msg__retry';
                    retry.textContent = 'Yenidən';
                    retry.addEventListener('click', function () { temp.remove(); retryFn(); });
                    status.appendChild(retry);
                }
            }
            function sendText(text) {
                if (!currentConv) return;
                var clientId = 'c' + Date.now().toString(36) + Math.random().toString(36).slice(2, 7);
                var dto = { recipientUserId: currentConv.peerUserId, messageType: 0, body: text };
                renderOptimistic(clientId, text);
                var ok = function (saved) { reconcileOptimistic(clientId, saved); };
                var fail = function () { markOptimisticFailed(clientId, function () { sendText(text); }); };
                if (hub.connected && hub.conn) {
                    hub.conn.invoke('SendMessage', dto).then(ok).catch(function () {
                        // SignalR path failed — try the REST fallback before giving up.
                        jpost('/api/chat/send', dto).then(ok).catch(fail);
                    });
                } else {
                    jpost('/api/chat/send', dto).then(ok).catch(fail);
                }
            }
            if (composerEl) {
                composerEl.addEventListener('submit', function (e) {
                    e.preventDefault();
                    if (!currentConv) return;
                    var text = (inputEl.value || '').trim();
                    if (!text) return;
                    inputEl.value = '';
                    sendText(text);
                });
                inputEl && inputEl.addEventListener('input', function () {
                    if (!currentConv || !hub.connected) return;
                    hub.conn.invoke('Typing', currentConv.peerUserId, true).catch(function () {});
                    clearTimeout(inputEl.__sbTypingTimeout);
                    inputEl.__sbTypingTimeout = setTimeout(function () {
                        hub.conn.invoke('Typing', currentConv.peerUserId, false).catch(function () {});
                    }, 1500);
                });
            }
            if (attachEl) {
                attachEl.addEventListener('change', function () {
                    if (!currentConv) return;
                    var file = attachEl.files && attachEl.files[0];
                    if (!file) return;
                    var fd = new FormData();
                    fd.append('recipientUserId', currentConv.peerUserId);
                    if (inputEl && inputEl.value) fd.append('body', inputEl.value);
                    fd.append('file', file);
                    jpostForm('/api/chat/send-image', fd).then(function (msg) {
                        if (msg && currentConv && msg.conversationId === currentConv.id) {
                            renderMessage(msg, false);
                            if (messagesEl) messagesEl.scrollTop = messagesEl.scrollHeight;
                        }
                        attachEl.value = '';
                        if (inputEl) inputEl.value = '';
                    }).catch(function () { alert('Şəkil göndərilə bilmədi.'); });
                });
            }
            if (backBtn) backBtn.addEventListener('click', function () { showList(); currentConv = null; });

            // Public hook so Phase 14.7 (story-reply) can request a conversation
            // open by user-id without going through the inbox.
            root.__sbOpenWithUser = function (peerUserId) {
                jpost('/api/chat/conversations/with/' + encodeURIComponent(peerUserId), null).then(function (conv) {
                    if (!conv) return;
                    var existing = hub.conversations.find(function (c) { return c.id === conv.id; });
                    if (!existing) {
                        hub.conversations.unshift(conv);
                        renderConversations();
                    }
                    openConversation(conv);
                }).catch(function () {});
            };

            loadConversations();
            return { renderConversations: renderConversations, showList: showList };
        }

        // ---- Wire each present root ----
        var widgetApi = null, fullpageApi = null;
        if (widget) {
            widgetApi = bind(widget, { isWidget: true });
            var toggleBtn = widget.querySelector('[data-chat-toggle]');
            var closeBtn = widget.querySelector('[data-chat-close]');
            var panel = widget.querySelector('[data-chat-panel]');
            if (toggleBtn) toggleBtn.addEventListener('click', function () {
                if (!panel) return;
                panel.hidden = !panel.hidden;
                if (!panel.hidden && widgetApi) widgetApi.showList();
            });
            if (closeBtn) closeBtn.addEventListener('click', function () { if (panel) panel.hidden = true; });
        }
        if (fullpage) {
            fullpageApi = bind(fullpage, { isWidget: false });
        }

        // ---- Unread badge ----
        function refreshUnreadBadge() {
            var total = hub.conversations.reduce(function (a, c) { return a + (c.unreadCount || 0); }, 0);
            var badge = document.querySelector('[data-chat-badge]');
            if (badge) {
                badge.textContent = total > 99 ? '99+' : String(total);
                badge.hidden = total === 0;
            }
        }
        // Initial fetch — covers anonymous tabs that signed in after page load.
        jget('/api/chat/unread-count').then(function (r) {
            if (r && typeof r.count === 'number') {
                var badge = document.querySelector('[data-chat-badge]');
                if (badge) { badge.textContent = r.count > 99 ? '99+' : String(r.count); badge.hidden = r.count === 0; }
            }
        }).catch(function () {});

        // ---- "Mesaj yaz" buttons across the site (Store detail, Story reply
        // in Phase 14.7, etc.) — single delegated listener. ----
        document.addEventListener('click', function (e) {
            var trigger = e.target.closest('[data-chat-open-with]');
            if (!trigger) return;
            e.preventDefault();
            var peerId = trigger.getAttribute('data-chat-open-with');
            if (!peerId) return;
            // Prefer the floating widget on every page; fall back to fullpage if open.
            var w = document.querySelector('[data-chat-widget]');
            var fp = document.querySelector('[data-chat-fullpage]');
            if (w && w.__sbOpenWithUser) {
                var panel = w.querySelector('[data-chat-panel]');
                if (panel) panel.hidden = false;
                w.__sbOpenWithUser(peerId);
            } else if (fp && fp.__sbOpenWithUser) {
                fp.__sbOpenWithUser(peerId);
            }
        });

        connectIfNeeded();
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

            var headers = { 'Content-Type': 'application/json' };
            var token = getAntiForgeryToken();
            if (token) headers['RequestVerificationToken'] = token;

            fetch('/Home/Subscribe', {
                method: 'POST',
                headers: headers,
                body: JSON.stringify({ email: emailInput.value })
            }).then(function (r) {
                if (submitBtn) submitBtn.classList.remove('is-loading');
                if (r.ok) {
                    result.classList.add('is-success');
                    result.textContent = 'Təşəkkürlər! Abunəliyin təsdiqləndi.';
                    emailInput.value = '';
                } else if (r.status === 429) {
                    result.classList.add('is-error');
                    result.textContent = 'Çox tez-tez cəhd edirsən. Bir az gözlə.';
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

            // ---- 0. Server-rendered error → scroll to it on page load ----
            // When the form POST comes back with ModelState errors, the server
            // re-renders the page with .sb-form-summary at the top OR
            // .is-error / .input-validation-error inputs. Browsers preserve
            // the user's pre-submit scroll position, so a user at the bottom
            // of /Seller/AddProduct still sees the bottom after the failed
            // submit and thinks "nothing happened". Yank them to the first
            // visible error on next animation frame.
            requestAnimationFrame(function () {
                var firstServerError = form.querySelector('.sb-form-summary, .is-error, .input-validation-error');
                if (!firstServerError) return;
                // Only scroll if it's off-screen above the fold.
                var rect = firstServerError.getBoundingClientRect();
                if (rect.top < 0 || rect.top > window.innerHeight) {
                    var topOffset = window.scrollY + rect.top - 120;
                    window.scrollTo({ top: Math.max(0, topOffset), behavior: 'smooth' });
                }
            });

            // ---- 1. Submit-loading spinner + scroll-to-first-error -----
            // jQuery Unobtrusive Validation may preventDefault() if a field
            // is invalid. setTimeout(0) lets that run first, so we only paint
            // the spinner when the form is actually navigating — otherwise
            // the button (which has pointer-events: none in is-loading) would
            // be permanently dead until full page reload.
            //
            // When validation DID fail, jump the page to the first invalid
            // field so a user at the bottom of a long form (Seller/AddProduct
            // is 200+ lines) doesn't think "nothing happened" — the errors
            // are right there, just off-screen above them.
            if (btn) {
                form.addEventListener('submit', function (e) {
                    setTimeout(function () {
                        if (!e.defaultPrevented) {
                            btn.classList.add('is-loading');
                            return;
                        }
                        // Validation failed silently above the fold.
                        // Find the first input flagged by jQuery Unobtrusive
                        // (.input-validation-error) OR by server re-render
                        // (.is-error). Scroll it into view with a buffer for
                        // the sticky header + brief button shake for feedback.
                        var firstError = form.querySelector('.input-validation-error, .is-error');
                        if (firstError) {
                            var rect = firstError.getBoundingClientRect();
                            var topOffset = window.scrollY + rect.top - 120; // 120px breathing room
                            window.scrollTo({ top: Math.max(0, topOffset), behavior: 'smooth' });
                            // Focus the field so the user can immediately type.
                            try { firstError.focus({ preventScroll: true }); } catch (_) { /* old browser */ }
                        }
                        // Pulse the submit button so the rejection is felt
                        // even if the offending field is already in view.
                        btn.classList.remove('is-loading');
                        btn.classList.add('is-shake');
                        setTimeout(function () { btn.classList.remove('is-shake'); }, 500);
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
            // Two field-wrapper conventions coexist in the project:
            //   .sb-field        — auth views (Login, Register, ForgotPassword)
            //   .sb-acc-field    — account + seller forms (AddProduct, Profile)
            // Without both selectors the product form's stale errors never
            // clear when the user starts typing.
            form.querySelectorAll('.sb-field input, .sb-field textarea, .sb-field select, .sb-acc-field input, .sb-acc-field textarea, .sb-acc-field select')
                .forEach(function (el) {
                    el.addEventListener('input', function () {
                        clearError(el.closest('.sb-field, .sb-acc-field'));
                        clearSummary();
                        if (btn) btn.classList.remove('is-loading');
                    });
                });

            // Checkboxes / radios — clear on toggle (covers AcceptTerms,
            // role cards, RememberMe, IsActive / HasDelivery toggles).
            form.querySelectorAll('input[type="checkbox"], input[type="radio"]')
                .forEach(function (el) {
                    el.addEventListener('change', function () {
                        clearError(el.closest('.sb-field, .sb-acc-field'));
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

    /* ----------- Packages page modal (Pro/VIP upgrade contact info) ----------- */
    function setupPackagesModal() {
        var modal = document.getElementById('sbPkgModal');
        if (!modal) return;
        var nameOut  = modal.querySelector('[data-pkg-modal-name]');
        var priceOut = modal.querySelector('[data-pkg-modal-price]');

        function open(name, price) {
            if (nameOut)  nameOut.textContent  = name  || '—';
            if (priceOut) priceOut.textContent = price || '—';
            modal.classList.add('is-open');
            modal.setAttribute('aria-hidden', 'false');
            document.body.classList.add('sb-locked');
        }
        function close() {
            modal.classList.remove('is-open');
            modal.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('sb-locked');
        }

        document.querySelectorAll('[data-pkg-upgrade]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                open(btn.getAttribute('data-pkg-name'),
                     btn.getAttribute('data-pkg-price'));
            });
        });
        modal.querySelectorAll('[data-pkg-close]').forEach(function (el) {
            el.addEventListener('click', close);
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && modal.classList.contains('is-open')) close();
        });
    }

    /* -----------
       Contact click tracking — fire-and-forget telemetry.
       Hooks any element marked with [data-click-track="<channel>"]:
         data-click-track       = whatsapp | instagram | telegram | phone |
                                  facebook | tiktok | email
         data-click-store-id    = (required) numeric store id
         data-click-product-id  = (optional) numeric product id

       Uses capture-phase delegation + keepalive:true so the request
       survives navigation (the user is about to leave for wa.me).
       ----------- */
    function setupContactClickTracking() {
        var typeMap = {
            'whatsapp':  0,
            'instagram': 1,
            'telegram':  2,
            'phone':     3,
            'facebook':  4,
            'tiktok':    5,
            'email':     6
        };

        document.addEventListener('click', function (e) {
            var el = e.target && e.target.closest ? e.target.closest('[data-click-track]') : null;
            if (!el) return;
            var channel = el.getAttribute('data-click-track');
            var typeId = typeMap[channel];
            if (typeId === undefined) return;

            var storeId = parseInt(el.getAttribute('data-click-store-id') || '0', 10);
            if (!storeId) return;
            var productIdAttr = el.getAttribute('data-click-product-id');
            var productId = productIdAttr ? parseInt(productIdAttr, 10) : null;

            var body = { storeId: storeId, contactType: typeId };
            if (productId && productId > 0) body.productId = productId;

            // keepalive so the POST is not cancelled when the new tab opens
            // or the page navigates away. Telemetry is best-effort.
            try {
                fetch('/Store/ClickContact', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(body),
                    keepalive: true,
                    credentials: 'same-origin'
                }).catch(function () { /* silent — telemetry only */ });
            } catch (_) { /* old browser without keepalive — silent */ }
        }, true); // capture so we fire before navigation handlers
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

    /* -----------------------------------------------------------
       Seller — EditProduct image gallery (was missing entirely — Phase 13.9)

       The View renders the gallery with these hooks but no handler
       existed, so picking a file did nothing. Wires three AJAX flows:

         data-img-input   change  → POST .../Images/Upload
         data-img-delete  click   → POST .../Images/{id}/Delete
         data-img-set-main click  → POST .../Images/{id}/SetMain

       All three reuse the antiforgery token from the form below the
       gallery — the gallery isn't inside a form (it'd nest with the
       core EditProduct form). Errors surface in [data-img-error].
       ----------------------------------------------------------- */
    function setupProductImageUploader() {
        var root = document.querySelector('[data-prod-images]');
        if (!root) return;
        var productId = root.getAttribute('data-product-id');
        if (!productId) return;
        var gallery   = root.querySelector('[data-img-gallery]');
        var input     = root.querySelector('[data-img-input]');
        var uploader  = root.querySelector('[data-img-uploader]');
        var errorBox  = root.querySelector('[data-img-error]');
        var token     = getAntiForgeryToken();

        function showError(msg) {
            if (!errorBox) return;
            errorBox.textContent = msg;
            errorBox.hidden = false;
            setTimeout(function () { errorBox.hidden = true; }, 6000);
        }
        function clearError() { if (errorBox) errorBox.hidden = true; }

        function buildTile(img) {
            // Mirrors the Razor _ProductImageTile equivalent so the DOM
            // looks identical to a fresh page load after the upload.
            var fig = document.createElement('figure');
            fig.className = 'sb-img-tile' + (img.isMain ? ' is-main' : '');
            fig.setAttribute('data-image-id', img.id);

            var imgEl = document.createElement('img');
            imgEl.src = img.imageUrl;
            imgEl.alt = '';
            fig.appendChild(imgEl);

            if (img.isMain) {
                var badge = document.createElement('span');
                badge.className = 'sb-img-tile__main-badge';
                badge.textContent = 'Əsas';
                fig.appendChild(badge);
            }

            var actions = document.createElement('div');
            actions.className = 'sb-img-tile__actions';

            if (!img.isMain) {
                var starBtn = document.createElement('button');
                starBtn.type = 'button';
                starBtn.className = 'sb-img-tile__btn';
                starBtn.title = 'Əsas et';
                starBtn.setAttribute('data-img-set-main', '');
                starBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.7"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>';
                actions.appendChild(starBtn);
            }

            var delBtn = document.createElement('button');
            delBtn.type = 'button';
            delBtn.className = 'sb-img-tile__btn sb-img-tile__btn--danger';
            delBtn.title = 'Sil';
            delBtn.setAttribute('data-img-delete', '');
            delBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.7"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6"/></svg>';
            actions.appendChild(delBtn);

            fig.appendChild(actions);
            return fig;
        }

        // ----- Upload (multi-file) -----
        if (input) {
            input.addEventListener('change', async function () {
                var files = Array.from(input.files || []);
                if (!files.length) return;
                clearError();
                if (uploader) uploader.classList.add('is-loading');

                // First image becomes main when the product has no images yet.
                var existingTiles = gallery ? gallery.querySelectorAll('[data-image-id]') : [];
                var noImagesYet = existingTiles.length === 0;

                for (var i = 0; i < files.length; i++) {
                    var file = files[i];
                    if (file.size > 10 * 1024 * 1024) {
                        showError('"' + file.name + '" 10 MB-dan böyükdür.');
                        continue;
                    }

                    var fd = new FormData();
                    fd.append('file', file);
                    fd.append('isMain', String(noImagesYet && i === 0));
                    if (token) fd.append('__RequestVerificationToken', token);

                    try {
                        var resp = await fetch('/Seller/Products/' + productId + '/Images/Upload', {
                            method: 'POST',
                            body: fd,
                            credentials: 'same-origin'
                        });
                        if (!resp.ok) {
                            var errData = null;
                            try { errData = await resp.json(); } catch (_) { /* non-JSON */ }
                            showError((errData && errData.error)
                                || 'Yükləmə uğursuz oldu (HTTP ' + resp.status + ')');
                            continue;
                        }
                        var data = await resp.json();
                        if (data && data.ok && data.image && gallery && uploader) {
                            gallery.insertBefore(buildTile(data.image), uploader);
                        }
                    } catch (err) {
                        showError('Şəbəkə xətası — yenidən cəhd et.');
                    }
                }

                if (uploader) uploader.classList.remove('is-loading');
                input.value = ''; // allow re-picking the same file
            });
        }

        // ----- Delete -----
        root.addEventListener('click', async function (e) {
            var btn = e.target.closest('[data-img-delete]');
            if (!btn) return;
            var tile = btn.closest('[data-image-id]');
            if (!tile) return;
            if (!confirm('Bu şəkli silmək istəyirsən?')) return;
            var imageId = tile.getAttribute('data-image-id');

            var fd = new FormData();
            if (token) fd.append('__RequestVerificationToken', token);
            try {
                var resp = await fetch(
                    '/Seller/Products/' + productId + '/Images/' + imageId + '/Delete',
                    { method: 'POST', body: fd, credentials: 'same-origin' });
                if (resp.ok) {
                    tile.remove();
                    clearError();
                } else {
                    showError('Silmək mümkün olmadı (HTTP ' + resp.status + ')');
                }
            } catch (_) {
                showError('Şəbəkə xətası.');
            }
        });

        // ----- Set main -----
        root.addEventListener('click', async function (e) {
            var btn = e.target.closest('[data-img-set-main]');
            if (!btn) return;
            var tile = btn.closest('[data-image-id]');
            if (!tile) return;
            var imageId = tile.getAttribute('data-image-id');

            var fd = new FormData();
            if (token) fd.append('__RequestVerificationToken', token);
            try {
                var resp = await fetch(
                    '/Seller/Products/' + productId + '/Images/' + imageId + '/SetMain',
                    { method: 'POST', body: fd, credentials: 'same-origin' });
                if (!resp.ok) {
                    showError('Əsas etmək mümkün olmadı (HTTP ' + resp.status + ')');
                    return;
                }
                // Strip is-main + badge from all tiles, then mark this one.
                root.querySelectorAll('[data-image-id]').forEach(function (t) {
                    t.classList.remove('is-main');
                    var b = t.querySelector('.sb-img-tile__main-badge');
                    if (b) b.remove();
                    // Re-add the star button to previously-main tiles that
                    // lost it (they shouldn't have one when not-main).
                    var actions = t.querySelector('.sb-img-tile__actions');
                    if (actions && !actions.querySelector('[data-img-set-main]')) {
                        var star = document.createElement('button');
                        star.type = 'button';
                        star.className = 'sb-img-tile__btn';
                        star.title = 'Əsas et';
                        star.setAttribute('data-img-set-main', '');
                        star.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.7"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>';
                        actions.insertBefore(star, actions.firstChild);
                    }
                });
                tile.classList.add('is-main');
                // Remove this tile's star button (it's already main) + add badge.
                var ownStar = tile.querySelector('[data-img-set-main]');
                if (ownStar) ownStar.remove();
                if (!tile.querySelector('.sb-img-tile__main-badge')) {
                    var badge = document.createElement('span');
                    badge.className = 'sb-img-tile__main-badge';
                    badge.textContent = 'Əsas';
                    tile.insertBefore(badge, tile.querySelector('.sb-img-tile__actions'));
                }
                clearError();
            } catch (_) {
                showError('Şəbəkə xətası.');
            }
        });
    }

    /* -----------------------------------------------------------
       Seller — Products (Phase 6c)
       - Tag input for sizes/colors (Enter → tag, with stock or color picker)
       - Image gallery: multi-upload, set-main, delete (all AJAX)
       - List inline toggle (active/inactive)
       - Delete-product confirm
       ----------------------------------------------------------- */
    function setupSellerProducts() {
        var token = getAntiForgeryToken();

        // ---------- 1. Tag input (sizes + colors) ----------
        document.querySelectorAll('[data-tag-input]').forEach(function (root) {
            var kind = root.getAttribute('data-tag-kind');  // 'size' | 'color'
            var list = root.querySelector('[data-tag-list]');
            var entry = root.querySelector('[data-tag-entry]');
            if (!list || !entry) return;

            var colorInput = root.querySelector('[data-tag-color]');
            var colorPreview = root.querySelector('[data-tag-color-preview]');
            if (colorInput && colorPreview) {
                colorInput.addEventListener('input', function () {
                    colorPreview.style.background = colorInput.value;
                });
            }

            function makeIndexNames() {
                // Re-index hidden field names so the model binder sees a
                // contiguous Form.Sizes[0..n] / Form.Colors[0..n] array.
                var tags = list.querySelectorAll('[data-tag]');
                tags.forEach(function (tag, i) {
                    var labelInput = tag.querySelector('[data-tag-hidden-label]');
                    var stockInput = tag.querySelector('[data-tag-hidden-stock]');
                    if (labelInput) {
                        labelInput.name = kind === 'size'
                            ? 'Form.Sizes[' + i + '].Size'
                            : 'Form.Colors[' + i + '].Name';
                    }
                    if (stockInput) {
                        stockInput.name = kind === 'size'
                            ? 'Form.Sizes[' + i + '].StockQuantity'
                            : 'Form.Colors[' + i + '].Hex';
                    }
                });
            }

            function buildSizeTag(label, stock) {
                var tag = document.createElement('span');
                tag.className = 'sb-tag';
                tag.setAttribute('data-tag', '');
                tag.innerHTML =
                    '<span class="sb-tag__label" data-tag-label></span>' +
                    '<input class="sb-tag__stock" type="number" min="0" step="1" data-tag-stock />' +
                    '<input type="hidden" data-tag-hidden-label />' +
                    '<input type="hidden" data-tag-hidden-stock />' +
                    '<button type="button" class="sb-tag__remove" data-tag-remove aria-label="Sil">' +
                    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>' +
                    '</button>';
                tag.querySelector('[data-tag-label]').textContent = label;
                tag.querySelector('[data-tag-stock]').value = String(stock);
                tag.querySelector('[data-tag-hidden-label]').value = label;
                tag.querySelector('[data-tag-hidden-stock]').value = String(stock);
                return tag;
            }

            function buildColorTag(name, hex) {
                var tag = document.createElement('span');
                tag.className = 'sb-tag sb-tag--color';
                tag.setAttribute('data-tag', '');
                tag.innerHTML =
                    '<span class="sb-tag__swatch"></span>' +
                    '<span class="sb-tag__label" data-tag-label></span>' +
                    '<input type="hidden" data-tag-hidden-label />' +
                    '<input type="hidden" data-tag-hidden-stock />' +
                    '<button type="button" class="sb-tag__remove" data-tag-remove aria-label="Sil">' +
                    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>' +
                    '</button>';
                tag.querySelector('.sb-tag__swatch').style.background = hex;
                tag.querySelector('[data-tag-label]').textContent = name;
                tag.querySelector('[data-tag-hidden-label]').value = name;
                tag.querySelector('[data-tag-hidden-stock]').value = hex;
                return tag;
            }

            function tryAdd(value) {
                value = (value || '').trim();
                if (!value) return;

                // De-dup by label (case-insensitive)
                var existing = Array.from(list.querySelectorAll('[data-tag-label]'))
                    .some(function (el) { return el.textContent.trim().toLowerCase() === value.toLowerCase(); });
                if (existing) { entry.value = ''; return; }

                var tag;
                if (kind === 'size') {
                    tag = buildSizeTag(value, 0);
                } else {
                    var hex = colorInput ? colorInput.value : '#000000';
                    tag = buildColorTag(value, hex);
                }
                list.appendChild(tag);
                makeIndexNames();
                entry.value = '';
            }

            entry.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ',') {
                    e.preventDefault();
                    tryAdd(entry.value);
                } else if (e.key === 'Backspace' && !entry.value) {
                    // Quick-delete last tag on backspace in an empty entry
                    var last = list.querySelector('[data-tag]:last-child');
                    if (last) {
                        last.remove();
                        makeIndexNames();
                    }
                }
            });
            entry.addEventListener('blur', function () { tryAdd(entry.value); });

            // Delegated handlers — remove + stock-input sync
            list.addEventListener('click', function (e) {
                var rm = e.target.closest('[data-tag-remove]');
                if (rm) {
                    rm.closest('[data-tag]').remove();
                    makeIndexNames();
                }
            });
            list.addEventListener('input', function (e) {
                var stockEl = e.target.closest('[data-tag-stock]');
                if (!stockEl) return;
                var tag = stockEl.closest('[data-tag]');
                var hidden = tag.querySelector('[data-tag-hidden-stock]');
                if (hidden) hidden.value = stockEl.value || '0';
            });

            // Server-rendered tags already have hidden inputs — make sure
            // they're indexed before first submit in case the order shifted.
            makeIndexNames();
        });

        // ---------- 2. Image gallery (EditProduct) ----------
        var imgRoot = document.querySelector('[data-prod-images]');
        if (imgRoot) {
            var productId = parseInt(imgRoot.getAttribute('data-product-id'), 10);
            var gallery = imgRoot.querySelector('[data-img-gallery]');
            var uploader = imgRoot.querySelector('[data-img-uploader]');
            var fileInput = imgRoot.querySelector('[data-img-input]');
            var errBox = imgRoot.querySelector('[data-img-error]');

            function showImgError(msg) {
                if (!errBox) return;
                errBox.textContent = msg;
                errBox.hidden = false;
                setTimeout(function () { errBox.hidden = true; }, 4000);
            }

            function buildTile(image) {
                var tile = document.createElement('figure');
                tile.className = 'sb-img-tile' + (image.isMain ? ' is-main' : '');
                tile.setAttribute('data-image-id', String(image.id));
                tile.innerHTML =
                    '<img src="' + image.imageUrl + '" alt="" />' +
                    (image.isMain ? '<span class="sb-img-tile__main-badge">Əsas</span>' : '') +
                    '<div class="sb-img-tile__actions">' +
                        (image.isMain ? '' :
                            '<button type="button" class="sb-img-tile__btn" data-img-set-main title="Əsas et">' +
                            '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.7"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>' +
                            '</button>') +
                        '<button type="button" class="sb-img-tile__btn sb-img-tile__btn--danger" data-img-delete title="Sil">' +
                        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.7"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6"/></svg>' +
                        '</button>' +
                    '</div>';
                return tile;
            }

            function clearAllMainBadges() {
                gallery.querySelectorAll('.sb-img-tile.is-main').forEach(function (t) {
                    t.classList.remove('is-main');
                    var badge = t.querySelector('.sb-img-tile__main-badge');
                    if (badge) badge.remove();
                    var actions = t.querySelector('.sb-img-tile__actions');
                    if (actions && !actions.querySelector('[data-img-set-main]')) {
                        var btn = document.createElement('button');
                        btn.type = 'button';
                        btn.className = 'sb-img-tile__btn';
                        btn.setAttribute('data-img-set-main', '');
                        btn.title = 'Əsas et';
                        btn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.7"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>';
                        actions.insertBefore(btn, actions.firstChild);
                    }
                });
            }

            function markAsMain(tile) {
                clearAllMainBadges();
                tile.classList.add('is-main');
                var badge = document.createElement('span');
                badge.className = 'sb-img-tile__main-badge';
                badge.textContent = 'Əsas';
                tile.insertBefore(badge, tile.firstChild.nextSibling);
                var setBtn = tile.querySelector('[data-img-set-main]');
                if (setBtn) setBtn.remove();
            }

            function uploadFiles(files) {
                if (!files || !files.length) return;
                Array.from(files).forEach(function (file) {
                    if (!file.type.startsWith('image/')) return;
                    if (file.size > 10 * 1024 * 1024) {
                        showImgError('Şəkil 10 MB-dan böyük ola bilməz.');
                        return;
                    }

                    var fd = new FormData();
                    fd.append('file', file);
                    fd.append('isMain', 'false');
                    if (token) fd.append('__RequestVerificationToken', token);

                    fetch('/Seller/Products/' + productId + '/Images/Upload', {
                        method: 'POST',
                        credentials: 'same-origin',
                        headers: { 'X-Requested-With': 'XMLHttpRequest' },
                        body: fd
                    })
                    .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, json: j }; }); })
                    .then(function (r) {
                        if (!r.ok) {
                            showImgError((r.json && r.json.error) || 'Yüklənmədi.');
                            return;
                        }
                        var tile = buildTile(r.json.image);
                        gallery.insertBefore(tile, uploader);
                        if (r.json.image.isMain) markAsMain(tile);
                        showToast('Şəkil yükləndi', 'success');
                    })
                    .catch(function () { showImgError('Şəbəkə xətası.'); });
                });
            }

            if (uploader && fileInput) {
                fileInput.addEventListener('change', function () {
                    uploadFiles(fileInput.files);
                    fileInput.value = '';
                });
            }

            // Delegated actions on each tile (set-main + delete)
            gallery.addEventListener('click', function (e) {
                var tile = e.target.closest('[data-image-id]');
                if (!tile) return;
                var imageId = parseInt(tile.getAttribute('data-image-id'), 10);

                if (e.target.closest('[data-img-set-main]')) {
                    var fd = new FormData();
                    if (token) fd.append('__RequestVerificationToken', token);
                    fetch('/Seller/Products/' + productId + '/Images/' + imageId + '/SetMain', {
                        method: 'POST',
                        credentials: 'same-origin',
                        headers: { 'X-Requested-With': 'XMLHttpRequest' },
                        body: fd
                    })
                    .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, json: j }; }); })
                    .then(function (r) {
                        if (!r.ok) { showImgError((r.json && r.json.error) || 'Yenilənmədi.'); return; }
                        markAsMain(tile);
                        showToast('Əsas şəkil təyin edildi', 'success');
                    })
                    .catch(function () { showImgError('Şəbəkə xətası.'); });
                    return;
                }
                if (e.target.closest('[data-img-delete]')) {
                    if (!window.confirm('Bu şəkli silmək istədiyinə əminsən?')) return;
                    var fd2 = new FormData();
                    if (token) fd2.append('__RequestVerificationToken', token);
                    fetch('/Seller/Products/' + productId + '/Images/' + imageId + '/Delete', {
                        method: 'POST',
                        credentials: 'same-origin',
                        headers: { 'X-Requested-With': 'XMLHttpRequest' },
                        body: fd2
                    })
                    .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, json: j }; }); })
                    .then(function (r) {
                        if (!r.ok) { showImgError((r.json && r.json.error) || 'Silinmədi.'); return; }
                        tile.style.transition = 'opacity .25s ease, transform .25s ease';
                        tile.style.opacity = '0';
                        tile.style.transform = 'scale(.85)';
                        setTimeout(function () { tile.remove(); }, 230);
                        showToast('Şəkil silindi', 'success');
                    })
                    .catch(function () { showImgError('Şəbəkə xətası.'); });
                }
            });
        }

        // ---------- 3. Inline toggle (Products list) ----------
        document.querySelectorAll('[data-prod-toggle]').forEach(function (cb) {
            cb.addEventListener('change', function () {
                var pid = parseInt(cb.getAttribute('data-prod-id'), 10);
                var fd = new FormData();
                if (token) fd.append('__RequestVerificationToken', token);
                cb.disabled = true;
                fetch('/Seller/ToggleProductActive/' + pid, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: fd
                })
                .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, json: j }; }); })
                .then(function (r) {
                    if (!r.ok) {
                        // Revert checkbox state
                        cb.checked = !cb.checked;
                        showToast((r.json && r.json.error) || 'Yenilənmədi.', 'error');
                        return;
                    }
                    var card = cb.closest('[data-product-id]');
                    if (card) card.classList.toggle('is-inactive', !r.json.isActive);
                    showToast(r.json.isActive ? 'Məhsul aktivləşdirildi' : 'Məhsul deaktiv edildi', 'success');
                })
                .catch(function () {
                    cb.checked = !cb.checked;
                    showToast('Şəbəkə xətası.', 'error');
                })
                .finally(function () { cb.disabled = false; });
            });
        });

        // ---------- 4. Delete-product confirm ----------
        document.querySelectorAll('[data-prod-delete-form]').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                if (!window.confirm('Bu məhsulu silmək istədiyinə əminsən? Bu əməliyyat geri qaytarıla bilməz.')) {
                    e.preventDefault();
                }
            });
        });
    }

    /* -----------------------------------------------------------
       Seller — Store Settings (Phase 6e)
       - Cover banner upload + remove
       - Square logo / avatar upload (overlapping the cover)
       - Active/Deaktiv toggle
       ----------------------------------------------------------- */
    function setupStoreSettings() {
        var root = document.querySelector('[data-store-id]');
        if (!root) return;
        var storeId = parseInt(root.getAttribute('data-store-id'), 10);
        if (!storeId) return;

        var token = getAntiForgeryToken();
        var errBox = root.querySelector('[data-store-img-error]');

        function showErr(msg) {
            if (errBox) {
                errBox.textContent = msg;
                errBox.hidden = false;
                setTimeout(function () { errBox.hidden = true; }, 4500);
            } else {
                showToast(msg, 'error');
            }
        }

        // ---------- Cover banner ----------
        var coverWrap = root.querySelector('[data-store-cover]');
        var coverInput = root.querySelector('[data-store-cover-input]');
        var coverImg = root.querySelector('[data-store-cover-img]');
        var coverPlaceholder = root.querySelector('[data-store-cover-placeholder]');
        var coverRemove = root.querySelector('[data-store-cover-remove]');

        function setCoverUrl(url) {
            if (!coverWrap) return;
            if (url) {
                coverWrap.classList.remove('is-empty');
                if (coverImg) {
                    coverImg.src = url;
                } else {
                    var img = document.createElement('img');
                    img.src = url;
                    img.alt = '';
                    img.setAttribute('data-store-cover-img', '');
                    coverWrap.insertBefore(img, coverWrap.firstChild);
                    coverImg = img;
                }
                if (coverPlaceholder) coverPlaceholder.style.display = 'none';
                if (coverRemove) coverRemove.hidden = false;
            } else {
                coverWrap.classList.add('is-empty');
                if (coverImg) { coverImg.remove(); coverImg = null; }
                if (coverPlaceholder) coverPlaceholder.style.display = '';
                if (coverRemove) coverRemove.hidden = true;
            }
        }

        if (coverInput) {
            coverInput.addEventListener('change', function () {
                var file = coverInput.files && coverInput.files[0];
                if (!file) return;
                if (file.size > 10 * 1024 * 1024) { showErr('Banner 10 MB-dan böyük ola bilməz.'); return; }

                var fd = new FormData();
                fd.append('storeId', String(storeId));
                fd.append('file', file);
                if (token) fd.append('__RequestVerificationToken', token);

                fetch('/Seller/UploadStoreCoverImage', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: fd
                })
                .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, json: j }; }); })
                .then(function (r) {
                    if (!r.ok) { showErr((r.json && r.json.error) || 'Banner yüklənmədi.'); return; }
                    setCoverUrl(r.json.url);
                    showToast('Banner yeniləndi', 'success');
                })
                .catch(function () { showErr('Şəbəkə xətası.'); })
                .finally(function () { coverInput.value = ''; });
            });
        }

        if (coverRemove) {
            coverRemove.addEventListener('click', function () {
                if (!window.confirm('Banner şəklini silmək istədiyinə əminsən?')) return;
                var fd = new FormData();
                fd.append('storeId', String(storeId));
                if (token) fd.append('__RequestVerificationToken', token);
                fetch('/Seller/RemoveStoreCoverImage', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: fd
                })
                .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, json: j }; }); })
                .then(function (r) {
                    if (!r.ok) { showErr((r.json && r.json.error) || 'Silinmədi.'); return; }
                    setCoverUrl(null);
                    showToast('Banner silindi', 'success');
                })
                .catch(function () { showErr('Şəbəkə xətası.'); });
            });
        }

        // ---------- Avatar / logo ----------
        var avatarLabel = root.querySelector('[data-store-avatar]');
        var avatarInput = root.querySelector('[data-store-avatar-input]');
        var avatarPreview = root.querySelector('[data-store-avatar-preview]');

        if (avatarInput && avatarPreview) {
            avatarInput.addEventListener('change', function () {
                var file = avatarInput.files && avatarInput.files[0];
                if (!file) return;
                if (file.size > 5 * 1024 * 1024) { showErr('Logo 5 MB-dan böyük ola bilməz.'); return; }

                // Optimistic local preview
                var localUrl = URL.createObjectURL(file);
                avatarPreview.innerHTML = '<img src="' + localUrl + '" alt="" />';

                var fd = new FormData();
                fd.append('storeId', String(storeId));
                fd.append('file', file);
                if (token) fd.append('__RequestVerificationToken', token);

                fetch('/Seller/UploadStoreProfileImage', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: fd
                })
                .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, json: j }; }); })
                .then(function (r) {
                    if (!r.ok) {
                        showErr((r.json && r.json.error) || 'Logo yüklənmədi.');
                        return;
                    }
                    avatarPreview.innerHTML = '<img src="' + r.json.url + '" alt="" />';
                    showToast('Logo yeniləndi', 'success');
                })
                .catch(function () { showErr('Şəbəkə xətası.'); })
                .finally(function () { avatarInput.value = ''; });
            });
        }

        // ---------- Active / deactive toggle ----------
        var statusCb = root.querySelector('[data-store-toggle]');
        if (statusCb) {
            var statusWrap = statusCb.closest('.sb-store-status-toggle');
            var statusLabel = statusWrap ? statusWrap.querySelector('.sb-store-status-toggle__label') : null;

            statusCb.addEventListener('change', function () {
                var fd = new FormData();
                fd.append('storeId', String(storeId));
                if (token) fd.append('__RequestVerificationToken', token);
                statusCb.disabled = true;

                fetch('/Seller/ToggleStoreActive', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: fd
                })
                .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, json: j }; }); })
                .then(function (r) {
                    if (!r.ok) {
                        statusCb.checked = !statusCb.checked;  // revert
                        showErr((r.json && r.json.error) || 'Status dəyişmədi.');
                        return;
                    }
                    var on = !!r.json.isActive;
                    if (statusWrap) {
                        statusWrap.classList.toggle('is-on', on);
                        statusWrap.classList.toggle('is-off', !on);
                    }
                    if (statusLabel) statusLabel.textContent = on ? 'Aktiv' : 'Deaktiv';
                    showToast(on ? 'Mağaza aktivdir' : 'Mağaza deaktiv edildi', 'success');
                })
                .catch(function () {
                    statusCb.checked = !statusCb.checked;
                    showErr('Şəbəkə xətası.');
                })
                .finally(function () { statusCb.disabled = false; });
            });
        }
    }

    /* -----------------------------------------------------------
       Public Store page (Phase 6f) — /Store/Detail/{slug}
       - Follow / unfollow toggle (AJAX) with optimistic UI
       - Hook into the existing setupStoryViewer via the
         #sbStorePageStories JSON payload
       ----------------------------------------------------------- */
    function setupStorePage() {
        var followBtn = document.querySelector('[data-store-follow]');
        var counter = document.querySelector('[data-store-follower-count]');
        var afForm = document.getElementById('sb-store-af');
        var token = afForm
            ? afForm.querySelector('input[name="__RequestVerificationToken"]')?.value
            : getAntiForgeryToken();

        if (followBtn && token) {
            followBtn.addEventListener('click', function () {
                if (followBtn.classList.contains('is-loading')) return;
                var storeId = followBtn.getAttribute('data-store-id');
                var willFollow = !followBtn.classList.contains('is-following');
                followBtn.classList.add('is-loading');

                var fd = new FormData();
                fd.append('storeId', storeId);
                fd.append('__RequestVerificationToken', token);

                fetch(willFollow ? '/Store/Follow' : '/Store/Unfollow', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: fd
                })
                .then(function (res) {
                    if (res.status === 401) {
                        // Not signed in — bounce to login with returnUrl.
                        window.location.href = '/Account/Login?ReturnUrl=' + encodeURIComponent(window.location.pathname);
                        return null;
                    }
                    return res.json().then(function (j) { return { ok: res.ok, json: j }; });
                })
                .then(function (r) {
                    if (!r) return;  // already redirected
                    if (!r.ok) {
                        showToast((r.json && r.json.error) || 'Yenilənmədi.', 'error');
                        return;
                    }
                    var nowFollowing = !!r.json.isFollowing;
                    followBtn.classList.toggle('is-following', nowFollowing);
                    var label = followBtn.querySelector('[data-store-follow-label]');
                    if (label) label.textContent = nowFollowing ? 'İzlənilir' : 'İzlə';
                    // Swap the icon — plus → check
                    var icon = followBtn.querySelector('.sb-storepage__follow-icon');
                    if (icon) {
                        icon.innerHTML = nowFollowing
                            ? '<polyline points="20 6 9 17 4 12"/>'
                            : '<line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>';
                    }
                    if (counter) counter.textContent = String(r.json.followerCount);
                    showToast(nowFollowing ? 'Mağaza izlənir' : 'İzləməni dayandırdın', 'success');
                })
                .catch(function () { showToast('Şəbəkə xətası.', 'error'); })
                .finally(function () { followBtn.classList.remove('is-loading'); });
            });
        }

        // ---------- Stories hook ----------
        // Wire each story bubble click into the existing #sbStoryViewer modal
        // already initialised by setupStoryViewer. The viewer reads a global
        // dataset of stories, so we feed our store-scoped list into the same
        // model by triggering the same event the rail uses on Home.
        var storiesScript = document.getElementById('sbStorePageStories');
        var bubbles = document.querySelectorAll('[data-story-open]');
        if (storiesScript && bubbles.length) {
            var stories;
            try { stories = JSON.parse(storiesScript.textContent); }
            catch (e) { stories = []; }

            // Re-package as a single "store group" matching the home rail's shape.
            var storeGroup = stories.length ? {
                storeName: stories[0].storeName,
                storeLogoUrl: stories[0].storeLogoUrl,
                stories: stories
            } : null;

            bubbles.forEach(function (btn, idx) {
                btn.addEventListener('click', function () {
                    var viewer = document.getElementById('sbStoryViewer');
                    if (!viewer || !storeGroup) {
                        // No viewer on the page — just open the linkUrl or image.
                        var s = stories[idx];
                        if (s && s.linkUrl) window.open(s.linkUrl, '_blank');
                        else if (s) window.open(s.imageUrl, '_blank');
                        return;
                    }
                    // Use a global hook exposed by setupStoryViewer (if present).
                    if (typeof window.__sbOpenStoryGroup === 'function') {
                        window.__sbOpenStoryGroup(storeGroup, idx);
                    } else {
                        // Graceful fallback: open the image directly.
                        var s2 = stories[idx];
                        if (s2 && s2.linkUrl) window.open(s2.linkUrl, '_blank');
                        else if (s2) window.open(s2.imageUrl, '_blank');
                    }
                });
            });
        }
    }

    /* -----------------------------------------------------------
       Admin Dashboard (Phase 7) — Areas/Admin/*
       - Mobile sidebar toggle
       - Bulk select (master checkbox + per-row + floating action bar)
       - data-confirm interceptor (window.confirm before form submit / click)
       - Category modal-less in-place edit (single form, JS swaps fields)
       - Hero banner modal-less in-place edit (single form, JS swaps fields)
       - Auto-hide toasts after 4.5 s
       ----------------------------------------------------------- */
    function setupAdmin() {
        // ---- 1. Mobile sidebar toggle ----
        var menuBtn = document.querySelector('[data-admin-menu-toggle]');
        var sidebar = document.querySelector('[data-admin-sidebar]');
        if (menuBtn && sidebar) {
            menuBtn.addEventListener('click', function () {
                sidebar.classList.toggle('is-open');
            });
            // Close sidebar on outside click (mobile)
            document.addEventListener('click', function (e) {
                if (!sidebar.classList.contains('is-open')) return;
                if (sidebar.contains(e.target) || menuBtn.contains(e.target)) return;
                sidebar.classList.remove('is-open');
            });
        }

        // ---- 2. Auto-hide toasts ----
        document.querySelectorAll('[data-admin-toast]').forEach(function (el) {
            setTimeout(function () {
                el.style.transition = 'opacity .35s ease, transform .35s ease';
                el.style.opacity = '0';
                el.style.transform = 'translateY(-6px)';
                setTimeout(function () { el.remove(); }, 360);
            }, 4500);
        });

        // ---- 3. data-confirm interceptor (forms + buttons + links) ----
        document.addEventListener('click', function (e) {
            var trigger = e.target.closest('[data-confirm]');
            if (!trigger) return;

            // For buttons inside forms, we let the form submit handler do the prompt.
            // For raw buttons / links, prompt here.
            if (trigger.tagName === 'BUTTON' && trigger.type === 'submit') {
                // Defer to the submit handler below.
                return;
            }
            if (!window.confirm(trigger.getAttribute('data-confirm'))) {
                e.preventDefault();
            }
        });
        document.addEventListener('submit', function (e) {
            var form = e.target;
            if (!form || form.tagName !== 'FORM') return;
            // Confirm if the submitter has data-confirm OR the form itself has it.
            var submitter = e.submitter;
            var msg = (submitter && submitter.getAttribute('data-confirm'))
                || form.getAttribute('data-confirm');
            if (msg && !window.confirm(msg)) e.preventDefault();
        });

        // ---- 4. Bulk select ----
        document.querySelectorAll('[data-admin-bulk-form]').forEach(function (form) {
            var master = form.querySelector('[data-admin-bulk-toggle]');
            var bar = form.querySelector('[data-admin-bulk-bar]');
            var counter = form.querySelector('[data-admin-bulk-count]');
            var items = function () { return form.querySelectorAll('[data-admin-bulk-item]'); };

            function refresh() {
                var checked = Array.from(items()).filter(function (cb) { return cb.checked; });
                var n = checked.length;
                if (counter) counter.textContent = String(n);
                if (bar) bar.hidden = n === 0;
                if (master) {
                    var total = items().length;
                    master.checked = total > 0 && n === total;
                    master.indeterminate = n > 0 && n < total;
                }
            }

            if (master) {
                master.addEventListener('change', function () {
                    items().forEach(function (cb) { cb.checked = master.checked; });
                    refresh();
                });
            }
            form.addEventListener('change', function (e) {
                if (e.target && e.target.matches('[data-admin-bulk-item]')) refresh();
            });
            refresh();
        });

        // ---- 5. Category modal-less in-place edit ----
        var catForm = document.querySelector('[data-admin-cat-form]');
        if (catForm) {
            var catTitle = document.querySelector('[data-admin-cat-form-title]');
            var catIdInput = catForm.querySelector('[data-admin-cat-id]');
            var catNameInput = catForm.querySelector('[data-admin-cat-name]');
            var catSlugInput = catForm.querySelector('[data-admin-cat-slug]');
            var catParentSelect = catForm.querySelector('[data-admin-cat-parent]');
            var catSubmit = catForm.querySelector('[data-admin-cat-submit]');
            var catCancel = catForm.querySelector('[data-admin-cat-cancel]');

            function resetCatForm() {
                catForm.action = catForm.action.replace(/\/Update\/\d+/, '/Create');
                if (catIdInput) catIdInput.value = '';
                if (catNameInput) catNameInput.value = '';
                if (catSlugInput) catSlugInput.value = '';
                if (catParentSelect) catParentSelect.value = '';
                if (catTitle) catTitle.textContent = 'Yeni kateqoriya';
                if (catSubmit) catSubmit.textContent = 'Yarat';
            }

            document.querySelectorAll('[data-admin-cat-edit]').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var id = btn.getAttribute('data-id');
                    catForm.action = catForm.action.replace(/\/(Create|Update\/\d+)$/, '/Update/' + id);
                    if (catIdInput) catIdInput.value = id;
                    if (catNameInput) catNameInput.value = btn.getAttribute('data-name') || '';
                    if (catSlugInput) catSlugInput.value = btn.getAttribute('data-slug') || '';
                    if (catParentSelect) catParentSelect.value = btn.getAttribute('data-parent') || '';
                    if (catTitle) catTitle.textContent = 'Kateqoriyanı redaktə et';
                    if (catSubmit) catSubmit.textContent = 'Yenilə';
                    catForm.scrollIntoView({ behavior: 'smooth', block: 'start' });
                });
            });
            if (catCancel) catCancel.addEventListener('click', resetCatForm);
        }

        // ---- 6. Hero banner modal-less in-place edit ----
        var bForm = document.querySelector('[data-admin-banner-form]');
        if (bForm) {
            var bTitle = document.querySelector('[data-admin-banner-form-title]');
            var bId = bForm.querySelector('[data-admin-banner-id]');
            var bT = bForm.querySelector('[data-admin-banner-title]');
            var bE = bForm.querySelector('[data-admin-banner-eyebrow]');
            var bS = bForm.querySelector('[data-admin-banner-subtitle]');
            var bCt = bForm.querySelector('[data-admin-banner-ctatext]');
            var bCu = bForm.querySelector('[data-admin-banner-ctaurl]');
            var bO = bForm.querySelector('[data-admin-banner-order]');
            var bA = bForm.querySelector('[data-admin-banner-active]');
            var bAField = bForm.querySelector('[data-admin-banner-active-field]');
            var bSubmit = bForm.querySelector('[data-admin-banner-submit]');
            var bCancel = bForm.querySelector('[data-admin-banner-cancel]');

            function resetBannerForm() {
                bForm.action = bForm.action.replace(/\/Update\/\d+/, '/Create');
                if (bId) bId.value = '';
                if (bT) bT.value = '';
                if (bE) bE.value = '';
                if (bS) bS.value = '';
                if (bCt) bCt.value = '';
                if (bCu) bCu.value = '';
                if (bO) bO.value = '0';
                if (bA) bA.checked = true;
                if (bAField) bAField.hidden = true;
                if (bTitle) bTitle.textContent = 'Yeni banner';
                if (bSubmit) bSubmit.textContent = 'Yarat';
            }

            document.querySelectorAll('[data-admin-banner-edit]').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var id = btn.getAttribute('data-id');
                    bForm.action = bForm.action.replace(/\/(Create|Update\/\d+)$/, '/Update/' + id);
                    if (bId) bId.value = id;
                    if (bT) bT.value = btn.getAttribute('data-title') || '';
                    if (bE) bE.value = btn.getAttribute('data-eyebrow') || '';
                    if (bS) bS.value = btn.getAttribute('data-subtitle') || '';
                    if (bCt) bCt.value = btn.getAttribute('data-ctatext') || '';
                    if (bCu) bCu.value = btn.getAttribute('data-ctaurl') || '';
                    if (bO) bO.value = btn.getAttribute('data-order') || '0';
                    if (bA) bA.checked = btn.getAttribute('data-active') === 'true';
                    if (bAField) bAField.hidden = false;
                    if (bTitle) bTitle.textContent = 'Banner redaktə et';
                    if (bSubmit) bSubmit.textContent = 'Yenilə';
                    bForm.scrollIntoView({ behavior: 'smooth', block: 'start' });
                });
            });
            if (bCancel) bCancel.addEventListener('click', resetBannerForm);
        }
    }
})();
