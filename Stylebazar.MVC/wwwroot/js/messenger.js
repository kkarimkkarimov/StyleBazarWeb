// StyleBazar Messenger V2 — premium 3-panelli mesajlaşma mərkəzinin client mühərriki.
// /Account/Messages səhifəsində işləyir. SignalR (real-vaxt) + REST (/api/chat) qarışığı:
// bütün yazma əməliyyatları REST-lə gedir, server hər iki tərəfə SignalR yayımı edir.
(function () {
    'use strict';

    var root = document.getElementById('sbMsgr');
    if (!root) return;

    // Köhnə üzən chat vidcetini bu səhifədə söndür (messenger onu əvəz edir).
    var oldWidget = document.querySelector('[data-chat-widget]');
    if (oldWidget) oldWidget.remove();

    // ---- i18n: layout-un yeritdiyi lüğətdən oxu (AZ mənbə mətni → tərcümə) ----
    function t(s) {
        try { var d = window.SB_I18N; if (d && Object.prototype.hasOwnProperty.call(d, s)) return d[s]; } catch (_) { }
        return s;
    }

    // ---- Konfiq ----
    var ME = root.getAttribute('data-me') || '';
    var INITIAL_PEER = root.getAttribute('data-peer') || '';
    var INITIAL_PRODUCT = parseInt(root.getAttribute('data-product') || '0', 10) || 0;

    // ---- DOM qısayolları ----
    function $(sel) { return root.querySelector(sel); }
    var els = {
        convs: $('[data-m-convs]'), listSkeleton: $('[data-m-list-skeleton]'), listEmpty: $('[data-m-list-empty]'),
        totalUnread: $('[data-m-total-unread]'), search: $('[data-m-search]'),
        welcome: $('[data-m-welcome]'), head: $('[data-m-head]'), body: $('[data-m-body]'),
        msgs: $('[data-m-msgs]'), composer: $('[data-m-composer]'), input: $('[data-m-input]'),
        send: $('[data-m-send]'), typing: $('[data-m-typing]'), typingName: $('[data-m-typing-name]'),
        jump: $('[data-m-jump]'), jumpCount: $('[data-m-jump-count]'),
        peerAva: $('[data-m-peer-ava]'), peerName: $('[data-m-peer-name]'), peerStatus: $('[data-m-peer-status]'),
        back: $('[data-m-back]'),
        replybar: $('[data-m-replybar]'), replyName: $('[data-m-replybar-name]'), replyText: $('[data-m-replybar-text]'),
        productchip: $('[data-m-productchip]'),
        recbar: $('[data-m-recbar]'), recTime: $('[data-m-rec-time]'),
        attachBtn: $('[data-m-attach-btn]'), attachMenu: $('[data-m-attachmenu]'),
        emojiBtn: $('[data-m-emoji-btn]'), emoji: $('[data-m-emoji]'),
        voiceBtn: $('[data-m-voice-btn]'),
        fileImage: $('[data-m-file-image]'), fileDoc: $('[data-m-file-doc]'),
        qr: $('[data-m-qr]'), qrList: $('[data-m-qr-list]'), qrForm: $('[data-m-qr-form]'),
        info: root.querySelector('[data-m-pane="info"]'), infoBody: $('[data-m-info-body]'),
        ctx: $('[data-m-ctx]'), modal: $('[data-m-modal]'), modalCard: $('[data-m-modal-card]'),
        lightbox: $('[data-m-lightbox]'), lightboxImg: $('[data-m-lightbox-img]'),
        drag: $('[data-m-drag]'),
        msgsearch: $('[data-m-msgsearch]'), msgsearchInput: $('[data-m-msgsearch-input]'), msgsearchResults: $('[data-m-msgsearch-results]')
    };

    // ---- Vəziyyət ----
    var state = {
        convs: [],            // siyahı keşi (ConversationDto-lar)
        filter: 'all',
        search: '',
        active: null,         // açıq söhbətin ConversationDto-su
        page: 1, pages: 1, loadingOlder: false,
        replyTo: null,        // {id, name, text}
        editing: null,        // messageId redaktə rejimi
        attachProduct: null,  // {id, name, imageUrl, price, discountPrice}
        presence: {},         // userId -> bool
        quickReplies: null,
        lastRenderedDate: null,
        lastSender: null, lastTime: 0,
        unseenWhileScrolled: 0,
        notifAsked: false
    };

    // ---- Köməkçilər ----
    function esc(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }
    function initials(name) {
        var p = String(name || '?').trim().split(/\s+/);
        return ((p[0] || '?')[0] + (p[1] ? p[1][0] : '')).toUpperCase();
    }
    // Server tarixləri UTC-dir, amma çox vaxt "Z" şəkilçisiz serializə olunur —
    // şəkilçi yoxdursa əlavə edirik ki, JS onu lokal vaxta düzgün çevirsin.
    function pdate(d) {
        if (typeof d === 'string' && !/[zZ]$|[+\-]\d\d:?\d\d$/.test(d)) d += 'Z';
        return new Date(d);
    }
    function fmtTime(d) {
        var x = pdate(d);
        return ('0' + x.getHours()).slice(-2) + ':' + ('0' + x.getMinutes()).slice(-2);
    }
    function dayKey(d) { var x = pdate(d); return x.getFullYear() + '-' + x.getMonth() + '-' + x.getDate(); }
    function fmtDay(d) {
        var x = pdate(d), now = new Date();
        if (dayKey(x) === dayKey(now)) return t('Bu gün');
        var y = new Date(now); y.setDate(now.getDate() - 1);
        if (dayKey(x) === dayKey(y)) return t('Dünən');
        return ('0' + x.getDate()).slice(-2) + '.' + ('0' + (x.getMonth() + 1)).slice(-2) + '.' + x.getFullYear();
    }
    function fmtListTime(d) {
        var x = pdate(d), now = new Date();
        if (dayKey(x) === dayKey(now)) return fmtTime(x);
        return ('0' + x.getDate()).slice(-2) + '.' + ('0' + (x.getMonth() + 1)).slice(-2);
    }
    function fmtSize(b) {
        if (b == null) return '';
        if (b < 1024) return b + ' B';
        if (b < 1048576) return (b / 1024).toFixed(1) + ' KB';
        return (b / 1048576).toFixed(1) + ' MB';
    }
    function fmtDur(s) { s = s || 0; return Math.floor(s / 60) + ':' + ('0' + (s % 60)).slice(-2); }
    function lastSeenText(conv) {
        if (state.presence[conv.peerUserId]) return t('indi onlayn');
        if (!conv.peerLastSeenAt) return t('oflayn');
        var x = conv.peerLastSeenAt, now = new Date();
        var label = dayKey(x) === dayKey(now) ? fmtTime(x) : fmtListTime(x) + ' ' + fmtTime(x);
        return t('son görülmə:') + ' ' + label;
    }
    function toast(msg, kind) {
        // site.js-dəki qlobal toast paneli yoxdursa sadə fallback.
        var el = document.querySelector('.sb-toast');
        if (!el) {
            el = document.createElement('div'); el.className = 'sb-toast';
            el.setAttribute('role', 'status'); document.body.appendChild(el);
        }
        el.textContent = t(msg);
        el.classList.remove('is-error', 'is-success', 'is-visible');
        if (kind === 'error') el.classList.add('is-error');
        if (kind === 'success') el.classList.add('is-success');
        void el.offsetWidth; el.classList.add('is-visible');
        clearTimeout(el._t); el._t = setTimeout(function () { el.classList.remove('is-visible'); }, 2400);
    }

    // ---- API ----
    function api(url, opts) {
        opts = opts || {};
        opts.headers = Object.assign({ 'X-Requested-With': 'XMLHttpRequest' }, opts.headers || {});
        if (opts.json !== undefined) {
            opts.headers['Content-Type'] = 'application/json';
            opts.body = JSON.stringify(opts.json);
            delete opts.json;
        }
        return fetch(url, opts).then(function (r) {
            if (r.status === 204) return null;
            return r.text().then(function (txt) {
                var data = null;
                try { data = txt ? JSON.parse(txt) : null; } catch (_) { }
                if (!r.ok) {
                    var msg = (data && (data.error || (data.errors && Object.values(data.errors)[0]))) || t('Xəta baş verdi.');
                    if (Array.isArray(msg)) msg = msg[0];
                    throw new Error(msg);
                }
                return data;
            });
        });
    }

    // ---- SignalR ----
    var hub = null, hubReady = false;
    function setupHub() {
        if (typeof signalR === 'undefined') return;
        hub = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/chat')
            .withAutomaticReconnect()
            .build();

        hub.on('MessageReceived', onMessageReceived);
        hub.on('MessagesRead', function (p) {
            if (state.active && state.active.id === p.conversationId) {
                els.msgs.querySelectorAll('.sb-msgr-tick').forEach(function (tk) { tk.classList.add('is-read'); });
            }
        });
        hub.on('PeerTyping', function (p) {
            if (!state.active || state.active.peerUserId !== p.userId) return;
            els.typingName.textContent = state.active.peerFullName.split(' ')[0];
            els.typing.hidden = !p.isTyping;
            if (p.isTyping) {
                clearTimeout(els.typing._t);
                els.typing._t = setTimeout(function () { els.typing.hidden = true; }, 4000);
            }
        });
        hub.on('UserPresence', function (p) {
            state.presence[p.userId] = p.online;
            if (p.lastSeenAt) {
                var c = state.convs.find(function (x) { return x.peerUserId === p.userId; });
                if (c) c.peerLastSeenAt = p.lastSeenAt;
            }
            paintPresence();
        });
        hub.on('MessageEdited', function (p) {
            if (state.active && state.active.id === p.conversationId) {
                var row = els.msgs.querySelector('[data-mid="' + p.messageId + '"]');
                if (row) {
                    var bodyEl = row.querySelector('[data-m-text]');
                    if (bodyEl) bodyEl.textContent = p.body;
                    var ed = row.querySelector('[data-m-edited]');
                    if (ed) ed.hidden = false;
                }
            }
        });
        hub.on('MessageDeleted', function (p) {
            if (state.active && state.active.id === p.conversationId) {
                var row = els.msgs.querySelector('[data-mid="' + p.messageId + '"]');
                if (row) {
                    var b = row.querySelector('.sb-msgr-bubble');
                    if (b) {
                        b.className = 'sb-msgr-bubble sb-msgr-bubble--deleted';
                        b.innerHTML = '🚫 ' + esc(t('Bu mesaj silindi'));
                    }
                    var more = row.querySelector('.sb-msgr-row__more'); if (more) more.remove();
                }
            }
        });
        hub.on('ReactionChanged', function (p) {
            if (state.active && state.active.id === p.conversationId) {
                var row = els.msgs.querySelector('[data-mid="' + p.messageId + '"]');
                if (row) paintReactions(row, p.reactions || []);
            }
        });

        hub.start().then(function () {
            hubReady = true;
            refreshPresence();
        }).catch(function () { /* REST fallback işləyir */ });

        hub.onreconnected(function () { hubReady = true; refreshPresence(); });
        hub.onclose(function () { hubReady = false; });
    }

    function refreshPresence() {
        if (!hubReady || !state.convs.length) return;
        var ids = state.convs.map(function (c) { return c.peerUserId; });
        hub.invoke('GetPresence', ids).then(function (map) {
            Object.assign(state.presence, map || {});
            paintPresence();
        }).catch(function () { });
    }

    function paintPresence() {
        state.convs.forEach(function (c) {
            var row = els.convs.querySelector('[data-cid="' + c.id + '"] .sb-msgr-ava');
            if (row) row.classList.toggle('is-online', !!state.presence[c.peerUserId]);
        });
        if (state.active) {
            els.peerAva.classList.toggle('is-online', !!state.presence[state.active.peerUserId]);
            var st = lastSeenText(state.active);
            els.peerStatus.textContent = st;
            els.peerStatus.classList.toggle('is-online', !!state.presence[state.active.peerUserId]);
        }
    }

    // ---- Bildirişlər (səs + brauzer) ----
    function playBlip() {
        try {
            var Ctx = window.AudioContext || window.webkitAudioContext;
            if (!Ctx) return;
            var ctx = playBlip._ctx || (playBlip._ctx = new Ctx());
            var o = ctx.createOscillator(), g = ctx.createGain();
            o.type = 'sine'; o.frequency.value = 880;
            g.gain.setValueAtTime(0.001, ctx.currentTime);
            g.gain.exponentialRampToValueAtTime(0.12, ctx.currentTime + 0.02);
            g.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.25);
            o.connect(g); g.connect(ctx.destination);
            o.start(); o.stop(ctx.currentTime + 0.26);
        } catch (_) { }
    }
    function askNotifPermission() {
        if (state.notifAsked || !('Notification' in window)) return;
        state.notifAsked = true;
        if (Notification.permission === 'default') Notification.requestPermission();
    }
    function notify(conv, msg) {
        if (conv && conv.isMuted) return;
        playBlip();
        if ('Notification' in window && Notification.permission === 'granted' && document.hidden) {
            try {
                var n = new Notification(conv ? conv.peerFullName : 'StyleBazar', {
                    body: previewOf(msg), icon: '/favicon.ico', tag: 'sb-chat-' + (conv ? conv.id : 0)
                });
                n.onclick = function () { window.focus(); if (conv) openConversation(conv); n.close(); };
            } catch (_) { }
        }
    }
    function previewOf(m) {
        switch (m.messageType) {
            case 1: return '📷 ' + t('Şəkil');
            case 4: return '🎤 ' + t('Səsli mesaj');
            case 5: return '📎 ' + (m.attachmentName || t('Fayl'));
            case 3: return '🛍 ' + t('Məhsul');
            default: return m.body || '';
        }
    }

    // ---- Söhbət siyahısı ----
    var searchTimer = null;
    function loadConversations() {
        els.listSkeleton.hidden = false;
        els.listEmpty.hidden = true;
        var url = '/api/chat/conversations?filter=' + encodeURIComponent(state.filter) +
            (state.search ? '&search=' + encodeURIComponent(state.search) : '') + '&pageSize=50';
        return api(url).then(function (data) {
            state.convs = (data && data.items) || [];
            els.listSkeleton.hidden = true;
            renderConversations();
            updateTotalUnread();
            refreshPresence();
        }).catch(function (e) {
            els.listSkeleton.hidden = true;
            toast(e.message, 'error');
        });
    }

    function renderConversations() {
        els.convs.innerHTML = '';
        if (!state.convs.length) { els.listEmpty.hidden = false; return; }
        els.listEmpty.hidden = true;
        state.convs.forEach(function (c) {
            var btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'sb-msgr-conv' + (state.active && state.active.id === c.id ? ' is-active' : '');
            btn.setAttribute('data-cid', c.id);
            var unread = c.unreadCount > 0;
            btn.innerHTML =
                '<span class="sb-msgr-ava' + (state.presence[c.peerUserId] ? ' is-online' : '') + '">' +
                (c.peerAvatarUrl ? '<img src="' + esc(c.peerAvatarUrl) + '" alt="" loading="lazy" />' : esc(initials(c.peerFullName))) +
                '</span>' +
                '<span class="sb-msgr-conv__mid">' +
                '<span class="sb-msgr-conv__name">' +
                (c.isPinned ? '<svg class="pin" viewBox="0 0 24 24" fill="currentColor"><path d="M12 17v5M9 10.76V7a1 1 0 011-1h4a1 1 0 011 1v3.76l1.71 1.71A1 1 0 0116 14H8a1 1 0 01-.71-1.53L9 10.76z"/></svg>' : '') +
                esc(c.peerFullName) +
                (c.tag ? '<span class="sb-msgr-conv__tag">' + esc(c.tag) + '</span>' : '') +
                (c.isFavorite ? ' <span title="' + esc(t('Sevimli')) + '">⭐</span>' : '') +
                '</span>' +
                '<span class="sb-msgr-conv__preview' + (unread ? ' is-unread' : '') + '">' +
                esc(c.peerStoreName && !c.lastMessagePreview ? c.peerStoreName : (c.lastMessagePreview || t('Yeni söhbət'))) +
                '</span>' +
                '</span>' +
                '<span class="sb-msgr-conv__side">' +
                '<span class="sb-msgr-conv__time">' + esc(fmtListTime(c.lastMessageAt)) + '</span>' +
                (unread ? '<span class="sb-msgr-conv__badge">' + (c.unreadCount > 99 ? '99+' : c.unreadCount) + '</span>'
                    : (c.isMuted ? '<svg class="sb-msgr-conv__mute" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M13.73 21a2 2 0 01-3.46 0"/><path d="M18.63 13A17.89 17.89 0 0118 8M6.26 6.26A5.86 5.86 0 006 8c0 7-3 9-3 9h14"/><path d="M18 8a6 6 0 00-9.33-5"/><line x1="1" y1="1" x2="23" y2="23"/></svg>' : '')) +
                '</span>';
            btn.addEventListener('click', function () { openConversation(c); });
            els.convs.appendChild(btn);
        });
    }

    function updateTotalUnread() {
        var n = state.convs.reduce(function (a, c) { return a + (c.unreadCount || 0); }, 0);
        els.totalUnread.hidden = n === 0;
        els.totalUnread.textContent = n > 99 ? '99+' : n;
        document.title = (n > 0 ? '(' + n + ') ' : '') + t('Mesajlar') + ' — StyleBazar';
    }

    // ---- Söhbət açma / mesaj render ----
    function openConversation(conv) {
        state.active = conv;
        state.page = 1; state.pages = 1;
        state.replyTo = null; state.editing = null;
        state.lastRenderedDate = null; state.lastSender = null; state.lastTime = 0;
        state.unseenWhileScrolled = 0;
        hideReplyBar(); closePopovers();

        root.classList.add('is-thread-open');
        els.welcome.hidden = true;
        els.head.hidden = false;
        els.body.hidden = false;
        els.composer.hidden = false;
        els.msgs.innerHTML = '';
        els.typing.hidden = true;
        els.jump.hidden = true;
        els.msgsearch.hidden = true;

        els.peerName.textContent = conv.peerFullName;
        els.peerAva.innerHTML = conv.peerAvatarUrl
            ? '<img src="' + esc(conv.peerAvatarUrl) + '" alt="" />' : esc(initials(conv.peerFullName));
        paintPresence();
        paintHeadToggles();
        renderConversations();

        loadMessages(1, true).then(function () { markRead(); });
        if (root.classList.contains('has-info')) loadInfo();
        els.input.focus();
    }

    function paintHeadToggles() {
        var pinBtn = els.head.querySelector('[data-m-act="pin"]');
        var muteBtn = els.head.querySelector('[data-m-act="mute"]');
        if (pinBtn) pinBtn.classList.toggle('is-on', !!(state.active && state.active.isPinned));
        if (muteBtn) muteBtn.classList.toggle('is-on', !!(state.active && state.active.isMuted));
    }

    function loadMessages(page, scrollBottom) {
        if (!state.active) return Promise.resolve();
        var convId = state.active.id;
        return api('/api/chat/conversations/' + convId + '/messages?page=' + page + '&pageSize=30')
            .then(function (data) {
                if (!state.active || state.active.id !== convId) return;
                state.pages = (data && data.pages) || 1;
                state.page = page;
                var items = ((data && data.items) || []).slice().reverse(); // köhnə → yeni
                if (page === 1) {
                    els.msgs.innerHTML = '';
                    state.lastRenderedDate = null; state.lastSender = null; state.lastTime = 0;
                    items.forEach(function (m) { appendMessage(m, false); });
                    if (scrollBottom) scrollToBottom(false);
                } else {
                    // Köhnə səhifə yuxarıya əlavə olunur — scroll mövqeyi qorunur.
                    var prevH = els.msgs.scrollHeight, prevTop = els.msgs.scrollTop;
                    var frag = document.createDocumentFragment();
                    var saveDate = state.lastRenderedDate, saveSender = state.lastSender, saveTime = state.lastTime;
                    state.lastRenderedDate = null; state.lastSender = null; state.lastTime = 0;
                    items.forEach(function (m) { frag.appendChild(buildRow(m)); });
                    state.lastRenderedDate = saveDate; state.lastSender = saveSender; state.lastTime = saveTime;
                    els.msgs.insertBefore(frag, els.msgs.firstChild);
                    els.msgs.scrollTop = prevTop + (els.msgs.scrollHeight - prevH);
                }
            }).catch(function (e) { toast(e.message, 'error'); });
    }

    function scrollToBottom(smooth) {
        if (!smooth) els.msgs.style.scrollBehavior = 'auto';
        els.msgs.scrollTop = els.msgs.scrollHeight;
        if (!smooth) requestAnimationFrame(function () { els.msgs.style.scrollBehavior = ''; });
        state.unseenWhileScrolled = 0;
        els.jump.hidden = true;
    }
    function isNearBottom() {
        return els.msgs.scrollHeight - els.msgs.scrollTop - els.msgs.clientHeight < 120;
    }

    // Tarix ayırıcısı + qruplaşdırma ilə mesaj əlavə et.
    function appendMessage(m, animateScroll) {
        els.msgs.appendChild(buildRow(m));
        if (animateScroll) scrollToBottom(true);
    }

    function buildRow(m) {
        var frag = document.createDocumentFragment();
        var dk = dayKey(m.createdDate);
        if (state.lastRenderedDate !== dk) {
            state.lastRenderedDate = dk;
            state.lastSender = null;
            var sep = document.createElement('div');
            sep.className = 'sb-msgr-date';
            sep.innerHTML = '<span>' + esc(fmtDay(m.createdDate)) + '</span>';
            frag.appendChild(sep);
        }
        var grouped = state.lastSender === m.senderId &&
            (new Date(m.createdDate) - state.lastTime) < 5 * 60 * 1000;
        state.lastSender = m.senderId;
        state.lastTime = +new Date(m.createdDate);

        var out = m.senderId === ME;
        var row = document.createElement('div');
        row.className = 'sb-msgr-row' + (out ? ' is-out' : '') + (grouped ? ' is-grouped' : '');
        row.setAttribute('data-mid', m.id);

        var bubble = document.createElement('div');
        bubble.className = 'sb-msgr-bubble';
        if (m.isDeletedBySender) {
            bubble.classList.add('sb-msgr-bubble--deleted');
            bubble.innerHTML = '🚫 ' + esc(t('Bu mesaj silindi'));
        } else {
            bubble.innerHTML = bubbleHtml(m, out);
        }

        var more = null;
        if (!m.isDeletedBySender) {
            more = document.createElement('button');
            more.type = 'button';
            more.className = 'sb-msgr-row__more';
            more.setAttribute('aria-label', t('Əməliyyatlar'));
            more.textContent = '⋯';
            more.addEventListener('click', function (e) {
                e.stopPropagation();
                openContextMenu(m, e.currentTarget.getBoundingClientRect());
            });
        }

        if (out) { if (more) row.appendChild(more); row.appendChild(bubble); }
        else { row.appendChild(bubble); if (more) row.appendChild(more); }

        // Reaksiyalar bubble-ın altına.
        if (m.reactions && m.reactions.length) paintReactions(row, m.reactions);

        // Şəkil böyütmə.
        var img = bubble.querySelector('.sb-msgr-bubble__img');
        if (img) img.addEventListener('click', function () { openLightbox(img.getAttribute('src')); });

        // Səs pleyeri.
        var voice = bubble.querySelector('.sb-msgr-voice');
        if (voice) wireVoice(voice);

        frag.appendChild(row);
        var wrap = document.createElement('div');
        wrap.appendChild(frag);
        // DocumentFragment qaytarmaq əvəzinə tək konteynersiz children köçürülür:
        var holder = document.createDocumentFragment();
        while (wrap.firstChild) holder.appendChild(wrap.firstChild);
        return holder;
    }

    function bubbleHtml(m, out) {
        var h = '';
        if (m.replyToMessageId) {
            h += '<span class="sb-msgr-quote"><strong>' + esc(m.replyToSenderName || '') + '</strong>' +
                esc(m.replyToSnippet || '') + '</span>';
        }
        switch (m.messageType) {
            case 1: // Şəkil
                h += '<img class="sb-msgr-bubble__img" src="' + esc(m.attachmentUrl) + '" alt="" loading="lazy" />';
                if (m.body) h += '<div class="sb-msgr-bubble__caption" data-m-text>' + esc(m.body) + '</div>';
                break;
            case 4: // Səsli mesaj
                h += '<span class="sb-msgr-voice" data-src="' + esc(m.attachmentUrl) + '">' +
                    '<button type="button" class="sb-msgr-voice__play">▶</button>' +
                    '<span class="sb-msgr-voice__bar"><i></i></span>' +
                    '<span class="sb-msgr-voice__time">' + fmtDur(m.durationSeconds) + '</span>' +
                    '<audio preload="none" src="' + esc(m.attachmentUrl) + '"></audio></span>';
                break;
            case 5: // Fayl
                h += '<a class="sb-msgr-file" href="' + esc(m.attachmentUrl) + '" target="_blank" rel="noopener">' +
                    '<span class="sb-msgr-file__icon">📄</span>' +
                    '<span class="sb-msgr-file__meta"><strong>' + esc(m.attachmentName || t('Fayl')) + '</strong>' +
                    '<span>' + esc(fmtSize(m.attachmentSize)) + '</span></span></a>';
                if (m.body) h += '<div class="sb-msgr-bubble__caption" data-m-text>' + esc(m.body) + '</div>';
                break;
            case 3: // Məhsul
                if (m.productPreview) {
                    var p = m.productPreview;
                    var price = p.discountPrice != null ? p.discountPrice : p.price;
                    h += '<a class="sb-msgr-prodcard" href="/Shop/Detail/' + esc(p.slug || p.id) + '">' +
                        (p.imageUrl ? '<img src="' + esc(p.imageUrl) + '" alt="" loading="lazy" />' : '') +
                        '<span class="sb-msgr-prodcard__body"><strong>' + esc(p.name) + '</strong>' +
                        '<span class="sb-msgr-prodcard__price">' + price + ' ₼' +
                        (p.discountPrice != null ? '<span class="sb-msgr-prodcard__old">' + p.price + ' ₼</span>' : '') +
                        '</span></span></a>';
                }
                if (m.body) h += '<div class="sb-msgr-bubble__caption" data-m-text>' + esc(m.body) + '</div>';
                break;
            case 2: // Hekayəyə cavab
                if (m.storyPreview) {
                    h += '<span class="sb-msgr-quote"><strong>' + esc(t('Hekayəyə cavab')) + '</strong>' +
                        esc(m.storyPreview.caption || (m.storyPreview.isExpired ? t('Hekayənin vaxtı bitib') : '')) + '</span>';
                }
                h += '<span data-m-text>' + esc(m.body || '') + '</span>';
                break;
            default:
                h += '<span data-m-text>' + esc(m.body || '') + '</span>';
        }
        h += '<span class="sb-msgr-bubble__meta">' +
            '<span data-m-edited' + (m.editedAt ? '' : ' hidden') + '>' + esc(t('redaktə olunub')) + ' · </span>' +
            esc(fmtTime(m.createdDate)) +
            (out ? '<span class="sb-msgr-tick' + (m.isReadByRecipient ? ' is-read' : '') + '">' +
                '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/><polyline points="24 8 13 19 11 17"/></svg></span>' : '') +
            '</span>';
        return h;
    }

    function paintReactions(row, reactions) {
        var old = row.querySelector('.sb-msgr-reacts');
        if (old) old.remove();
        if (!reactions || !reactions.length) return;
        var bubble = row.querySelector('.sb-msgr-bubble');
        var box = document.createElement('div');
        box.className = 'sb-msgr-reacts';
        reactions.forEach(function (r) {
            var chip = document.createElement('button');
            chip.type = 'button';
            chip.className = 'sb-msgr-reacts__chip' + (r.mine ? ' is-mine' : '');
            chip.textContent = r.emoji + (r.count > 1 ? ' ' + r.count : '');
            chip.addEventListener('click', function () {
                react(parseInt(row.getAttribute('data-mid'), 10), r.emoji);
            });
            box.appendChild(chip);
        });
        bubble.appendChild(box);
    }

    function wireVoice(el) {
        var audio = el.querySelector('audio');
        var btn = el.querySelector('.sb-msgr-voice__play');
        var bar = el.querySelector('.sb-msgr-voice__bar i');
        btn.addEventListener('click', function () {
            if (audio.paused) {
                document.querySelectorAll('.sb-msgr-voice audio').forEach(function (a) { if (a !== audio) a.pause(); });
                audio.play(); btn.textContent = '❚❚';
            } else { audio.pause(); btn.textContent = '▶'; }
        });
        audio.addEventListener('timeupdate', function () {
            if (audio.duration) bar.style.width = (audio.currentTime / audio.duration * 100) + '%';
        });
        audio.addEventListener('ended', function () { btn.textContent = '▶'; bar.style.width = '0%'; });
        audio.addEventListener('pause', function () { btn.textContent = '▶'; });
    }

    // ---- Gələn mesaj hadisəsi ----
    function onMessageReceived(m) {
        var conv = state.convs.find(function (c) { return c.id === m.conversationId; });
        var isActive = state.active && state.active.id === m.conversationId;
        var fromMe = m.senderId === ME;

        if (isActive) {
            var nearBottom = isNearBottom();
            appendMessage(m, fromMe || nearBottom);
            if (!fromMe) {
                if (nearBottom && !document.hidden) { markRead(); }
                else {
                    state.unseenWhileScrolled++;
                    els.jumpCount.hidden = false;
                    els.jumpCount.textContent = state.unseenWhileScrolled;
                    els.jump.hidden = false;
                }
            }
            els.typing.hidden = true;
        }
        if (!fromMe && (!isActive || document.hidden)) notify(conv, m);
        // Siyahını yenilə (önizləmə + sıra + sayğac) — yüngül yol: yenidən yüklə.
        loadConversations();
    }

    function markRead() {
        if (!state.active) return;
        var id = state.active.id;
        if (hubReady) hub.invoke('MarkRead', id).catch(function () { api('/api/chat/conversations/' + id + '/read', { method: 'POST' }); });
        else api('/api/chat/conversations/' + id + '/read', { method: 'POST' }).catch(function () { });
        var c = state.convs.find(function (x) { return x.id === id; });
        if (c) { c.unreadCount = 0; renderConversations(); updateTotalUnread(); }
    }

    // ---- Göndərmə ----
    function autosize() {
        els.input.style.height = 'auto';
        els.input.style.height = Math.min(els.input.scrollHeight, 132) + 'px';
    }

    var typingSent = 0;
    function sendTyping() {
        if (!hubReady || !state.active) return;
        var now = Date.now();
        if (now - typingSent < 2000) return;
        typingSent = now;
        hub.invoke('Typing', state.active.peerUserId, true).catch(function () { });
    }

    function doSend() {
        if (!state.active) return;
        var text = els.input.value.trim();

        // Redaktə rejimi.
        if (state.editing) {
            if (!text) return;
            var mid = state.editing;
            api('/api/chat/messages/' + mid + '/edit', { method: 'POST', json: { body: text } })
                .then(function () { exitEditMode(); })
                .catch(function (e) { toast(e.message, 'error'); });
            return;
        }

        var payload = {
            recipientUserId: state.active.peerUserId,
            messageType: 0,
            body: text,
            replyToMessageId: state.replyTo ? state.replyTo.id : null
        };
        // Məhsul çipi bərkidilibsə — ilk mesaj ProductShare olur.
        if (state.attachProduct) {
            payload.messageType = 3;
            payload.productId = state.attachProduct.id;
            if (!text) payload.body = null;
        } else if (!text) { return; }

        els.send.disabled = true;
        api('/api/chat/send', { method: 'POST', json: payload })
            .then(function () {
                els.input.value = ''; autosize();
                hideReplyBar(); hideProductChip();
                askNotifPermission();
            })
            .catch(function (e) { toast(e.message, 'error'); })
            .finally(function () { els.send.disabled = false; els.input.focus(); });
    }

    function uploadAndSend(url, file, extra) {
        if (!state.active) return;
        var fd = new FormData();
        fd.append('recipientUserId', state.active.peerUserId);
        fd.append('file', file);
        if (state.replyTo) fd.append('replyToMessageId', state.replyTo.id);
        Object.keys(extra || {}).forEach(function (k) { fd.append(k, extra[k]); });
        toast(t('Göndərilir...'));
        api(url, { method: 'POST', body: fd })
            .then(function () { hideReplyBar(); })
            .catch(function (e) { toast(e.message, 'error'); });
    }

    // ---- Reply / Edit / Product çipləri ----
    function showReplyBar(m) {
        state.editing = null;
        state.replyTo = { id: m.id, name: m.senderFullName, text: previewOf(m) };
        els.replyName.textContent = m.senderId === ME ? t('Sən') : m.senderFullName;
        els.replyText.textContent = previewOf(m);
        els.replybar.hidden = false;
        els.input.focus();
    }
    function hideReplyBar() { state.replyTo = null; els.replybar.hidden = true; }

    function enterEditMode(m) {
        hideReplyBar();
        state.editing = m.id;
        els.replyName.textContent = t('Redaktə');
        els.replyText.textContent = m.body || '';
        els.replybar.hidden = false;
        els.input.value = m.body || '';
        autosize(); els.input.focus();
    }
    function exitEditMode() {
        state.editing = null;
        els.replybar.hidden = true;
        els.input.value = ''; autosize();
    }

    function showProductChip(p) {
        state.attachProduct = p;
        $('[data-m-productchip-img]').src = p.imageUrl || '/img/placeholder.svg';
        $('[data-m-productchip-name]').textContent = p.name;
        var price = p.discountPrice != null ? p.discountPrice : p.price;
        $('[data-m-productchip-price]').textContent = price + ' ₼';
        els.productchip.hidden = false;
        els.input.placeholder = t('Bu məhsul haqqında soruş...');
    }
    function hideProductChip() {
        state.attachProduct = null;
        els.productchip.hidden = true;
        els.input.placeholder = t('Mesaj yaz...');
    }

    // ---- Kontekst menyu ----
    var QUICK_REACTS = ['❤️', '👍', '😂', '😮', '😢', '🙏'];
    function openContextMenu(m, anchorRect) {
        closePopovers();
        var out = m.senderId === ME;
        var html = '<div class="sb-msgr-ctx__reacts">' +
            QUICK_REACTS.map(function (e) { return '<button type="button" data-react="' + e + '">' + e + '</button>'; }).join('') +
            '</div>' +
            '<button type="button" class="sb-msgr-ctx__item" data-act="reply">↩ ' + esc(t('Cavabla')) + '</button>' +
            (m.body ? '<button type="button" class="sb-msgr-ctx__item" data-act="copy">⧉ ' + esc(t('Kopyala')) + '</button>' : '') +
            '<button type="button" class="sb-msgr-ctx__item" data-act="forward">➦ ' + esc(t('Yönləndir')) + '</button>' +
            (out && m.messageType === 0 ? '<button type="button" class="sb-msgr-ctx__item" data-act="edit">✎ ' + esc(t('Redaktə et')) + '</button>' : '') +
            (out ? '<button type="button" class="sb-msgr-ctx__item is-danger" data-act="delete">🗑 ' + esc(t('Sil')) + '</button>' : '');
        els.ctx.innerHTML = html;
        els.ctx.hidden = false;
        var x = Math.min(anchorRect.left, window.innerWidth - 230);
        var y = Math.min(anchorRect.bottom + 4, window.innerHeight - els.ctx.offsetHeight - 12);
        els.ctx.style.left = x + 'px';
        els.ctx.style.top = Math.max(8, y) + 'px';

        els.ctx.querySelectorAll('[data-react]').forEach(function (b) {
            b.addEventListener('click', function () { react(m.id, b.getAttribute('data-react')); closePopovers(); });
        });
        els.ctx.querySelectorAll('[data-act]').forEach(function (b) {
            b.addEventListener('click', function () {
                var act = b.getAttribute('data-act');
                closePopovers();
                if (act === 'reply') showReplyBar(m);
                if (act === 'copy') { navigator.clipboard && navigator.clipboard.writeText(m.body || ''); toast(t('Kopyalandı'), 'success'); }
                if (act === 'edit') enterEditMode(m);
                if (act === 'delete') confirmDelete(m);
                if (act === 'forward') openForward(m);
            });
        });
    }

    function react(messageId, emoji) {
        api('/api/chat/messages/' + messageId + '/react', { method: 'POST', json: { emoji: emoji } })
            .catch(function (e) { toast(e.message, 'error'); });
    }

    function confirmDelete(m) {
        openModal('<h3>' + esc(t('Mesaj silinsin?')) + '</h3>' +
            '<p style="color:var(--m-muted);font-size:.88rem">' + esc(t('Mesaj hər iki tərəf üçün silinəcək.')) + '</p>' +
            '<div class="sb-msgr-modal__foot">' +
            '<button type="button" class="sb-msgr-btn sb-msgr-btn--ghost" data-close>' + esc(t('İmtina')) + '</button>' +
            '<button type="button" class="sb-msgr-btn sb-msgr-btn--danger" data-ok>' + esc(t('Sil')) + '</button></div>');
        els.modalCard.querySelector('[data-ok]').addEventListener('click', function () {
            api('/api/chat/messages/' + m.id + '/delete', { method: 'POST' })
                .then(closeModal)
                .catch(function (e) { toast(e.message, 'error'); });
        });
    }

    function openForward(m) {
        var listHtml = state.convs.filter(function (c) { return !state.active || c.id !== state.active.id; })
            .slice(0, 20)
            .map(function (c) {
                return '<button type="button" class="sb-msgr-fwd-item" data-peer="' + esc(c.peerUserId) + '">' +
                    '<span class="sb-msgr-ava">' + (c.peerAvatarUrl ? '<img src="' + esc(c.peerAvatarUrl) + '" alt=""/>' : esc(initials(c.peerFullName))) + '</span>' +
                    '<span>' + esc(c.peerFullName) + '</span></button>';
            }).join('');
        openModal('<h3>' + esc(t('Kimə yönləndirilsin?')) + '</h3>' +
            (listHtml || '<p style="color:var(--m-muted)">' + esc(t('Başqa söhbət yoxdur.')) + '</p>') +
            '<div class="sb-msgr-modal__foot"><button type="button" class="sb-msgr-btn sb-msgr-btn--ghost" data-close>' + esc(t('İmtina')) + '</button></div>');
        els.modalCard.querySelectorAll('.sb-msgr-fwd-item').forEach(function (b) {
            b.addEventListener('click', function () {
                var payload = {
                    recipientUserId: b.getAttribute('data-peer'),
                    messageType: m.messageType,
                    body: m.body,
                    attachmentUrl: m.attachmentUrl,
                    attachmentName: m.attachmentName,
                    attachmentSize: m.attachmentSize,
                    durationSeconds: m.durationSeconds,
                    productId: m.productPreview ? m.productPreview.id : null,
                    storyId: null
                };
                if (payload.messageType === 2) { payload.messageType = 0; } // hekayə cavabı adi mətn kimi gedir
                api('/api/chat/send', { method: 'POST', json: payload })
                    .then(function () { closeModal(); toast(t('Yönləndirildi'), 'success'); })
                    .catch(function (e) { toast(e.message, 'error'); });
            });
        });
    }

    // ---- Modal / lightbox / popover idarəsi ----
    function openModal(html) {
        els.modalCard.innerHTML = html;
        els.modal.hidden = false;
        els.modalCard.querySelectorAll('[data-close]').forEach(function (b) { b.addEventListener('click', closeModal); });
    }
    function closeModal() { els.modal.hidden = true; els.modalCard.innerHTML = ''; }
    function openLightbox(src) { els.lightboxImg.src = src; els.lightbox.hidden = false; }
    function closePopovers() {
        els.ctx.hidden = true;
        els.attachMenu.hidden = true;
        els.emoji.hidden = true;
        els.qr.hidden = true;
    }

    // ---- Emoji picker ----
    var EMOJIS = ['😀','😁','😂','🤣','😊','😍','😘','😎','🤩','🥳','😅','😉','🙂','😋','😜','🤔','😏','🥺','😢','😭','😡','🤯','😴','🤗','😇','🙃','👍','👎','👏','🙏','💪','🤝','👋','✌️','🤞','❤️','🧡','💛','💚','💙','💜','🖤','🤍','💯','🔥','⭐','✨','🎉','🎁','✅','❌','❓','❗','🛍️','👗','👠','👜','🧥','👟','⌚','🕶️','💄','📦','🚚','💰','💳','📸'];
    function buildEmoji() {
        if (els.emoji.childElementCount) return;
        EMOJIS.forEach(function (e) {
            var b = document.createElement('button');
            b.type = 'button'; b.textContent = e;
            b.addEventListener('click', function () {
                els.input.value += e;
                els.input.focus(); autosize();
            });
            els.emoji.appendChild(b);
        });
    }

    // ---- Sürətli cavablar ----
    function loadQuickReplies() {
        return api('/api/chat/quick-replies').then(function (list) {
            state.quickReplies = list || [];
            renderQuickReplies();
        }).catch(function () { });
    }
    function renderQuickReplies() {
        els.qrList.innerHTML = '';
        if (!state.quickReplies || !state.quickReplies.length) {
            els.qrList.innerHTML = '<p style="padding:10px 14px;color:var(--m-muted);font-size:.8rem">' +
                esc(t('Hələ şablon yoxdur. Aşağıdan əlavə et.')) + '</p>';
            return;
        }
        state.quickReplies.forEach(function (q) {
            var div = document.createElement('div');
            div.className = 'sb-msgr-qr__item';
            div.innerHTML = '<div style="min-width:0;flex:1"><strong>' + esc(q.title) + '</strong><span>' + esc(q.body) + '</span></div>' +
                '<button type="button" aria-label="' + esc(t('Sil')) + '">✕</button>';
            div.addEventListener('click', function (e) {
                if (e.target.tagName === 'BUTTON') return;
                els.input.value = q.body; autosize(); closePopovers(); els.input.focus();
            });
            div.querySelector('button').addEventListener('click', function () {
                api('/api/chat/quick-replies/' + q.id, { method: 'DELETE' }).then(loadQuickReplies);
            });
            els.qrList.appendChild(div);
        });
    }

    // ---- Sağ panel (info) ----
    function toggleInfo(forceOpen) {
        var open = forceOpen === true || (forceOpen !== false && !root.classList.contains('has-info'));
        root.classList.toggle('has-info', open);
        els.info.hidden = !open;
        var infoBtn = els.head.querySelector('[data-m-act="info"]');
        if (infoBtn) infoBtn.classList.toggle('is-on', open);
        if (open && state.active) loadInfo();
    }

    function loadInfo() {
        if (!state.active) return;
        var convId = state.active.id;
        els.infoBody.innerHTML = '<div class="sb-msgr-skeleton"><div class="sb-msgr-skeleton__row"></div><div class="sb-msgr-skeleton__row"></div></div>';
        api('/api/chat/conversations/' + convId + '/detail').then(function (d) {
            if (!state.active || state.active.id !== convId) return;
            renderInfo(d);
        }).catch(function (e) { els.infoBody.innerHTML = '<p style="color:var(--m-muted)">' + esc(e.message) + '</p>'; });
    }

    function renderInfo(d) {
        var roleLabel = d.peerRole === 'Seller' ? t('Satıcı') : (d.peerRole === 'Admin' ? 'Admin' : t('Alıcı'));
        var html =
            '<div class="sb-msgr-info-peer">' +
            '<span class="sb-msgr-ava' + (state.presence[d.peerUserId] ? ' is-online' : '') + '">' +
            (d.peerAvatarUrl ? '<img src="' + esc(d.peerAvatarUrl) + '" alt=""/>' : esc(initials(d.peerFullName))) + '</span>' +
            '<h3>' + esc(d.peerFullName) + '</h3>' +
            '<p>' + esc(roleLabel) + ' · ' + esc(t('üzv:')) + ' ' + new Date(d.peerMemberSince).getFullYear() + '</p>' +
            '</div>';

        if (d.storeName) {
            html += '<div class="sb-msgr-info-sec"><strong>' + esc(t('Mağaza')) + '</strong>' +
                '<a class="sb-msgr-info-store" href="/Store/Detail/' + esc(d.storeSlug || '') + '">' +
                (d.storeLogoUrl ? '<img src="' + esc(d.storeLogoUrl) + '" alt=""/>' : '') +
                '<span><strong>' + esc(d.storeName) + '</strong><span>' + esc(t('Mağazaya bax')) + ' →</span></span></a></div>';
        }
        if (d.lastProduct) {
            var lp = d.lastProduct;
            var lpPrice = lp.discountPrice != null ? lp.discountPrice : lp.price;
            html += '<div class="sb-msgr-info-sec"><strong>' + esc(t('Müzakirə olunan məhsul')) + '</strong>' +
                '<a class="sb-msgr-prodcard" href="/Shop/Detail/' + esc(lp.slug || lp.id) + '">' +
                (lp.imageUrl ? '<img src="' + esc(lp.imageUrl) + '" alt=""/>' : '') +
                '<span class="sb-msgr-prodcard__body"><strong>' + esc(lp.name) + '</strong>' +
                '<span class="sb-msgr-prodcard__price">' + lpPrice + ' ₼</span></span></a></div>';
        }
        if (d.sharedImages && d.sharedImages.length) {
            html += '<div class="sb-msgr-info-sec"><strong>' + esc(t('Paylaşılan şəkillər')) + '</strong>' +
                '<div class="sb-msgr-info-grid">' +
                d.sharedImages.map(function (u) { return '<img src="' + esc(u) + '" alt="" loading="lazy" data-zoom/>'; }).join('') +
                '</div></div>';
        }
        if (d.sharedFiles && d.sharedFiles.length) {
            html += '<div class="sb-msgr-info-sec"><strong>' + esc(t('Paylaşılan fayllar')) + '</strong>' +
                d.sharedFiles.map(function (f) {
                    return '<a class="sb-msgr-info-file" href="' + esc(f.url) + '" target="_blank" rel="noopener">📄 ' +
                        esc(f.name) + ' <small style="color:var(--m-muted)">' + esc(fmtSize(f.size)) + '</small></a>';
                }).join('') + '</div>';
        }

        html += '<div class="sb-msgr-info-sec sb-msgr-info-tag"><strong>' + esc(t('Etiket')) + '</strong>' +
            '<input type="text" maxlength="40" placeholder="' + esc(t('məs. VIP, Yeni')) + '" value="' + esc(d.tag || '') + '" data-m-tag />' +
            '</div>' +
            '<div class="sb-msgr-info-sec sb-msgr-info-note"><strong>' + esc(t('Şəxsi qeyd')) + '</strong>' +
            '<textarea rows="3" maxlength="500" placeholder="' + esc(t('Yalnız sən görürsən...')) + '" data-m-note>' + esc(d.note || '') + '</textarea>' +
            '<small>' + esc(t('Qarşı tərəf bu qeydi görmür.')) + '</small></div>';

        html += '<div class="sb-msgr-info-actions">' +
            '<button type="button" class="sb-msgr-btn sb-msgr-btn--ghost" data-m-info-fav>' + (d.isFavorite ? '⭐ ' + esc(t('Sevimlidən çıxar')) : '☆ ' + esc(t('Sevimli et'))) + '</button>' +
            '<button type="button" class="sb-msgr-btn sb-msgr-btn--ghost" data-m-info-block>' + (d.isBlockedByMe ? '🔓 ' + esc(t('Blokdan çıxar')) : '🚫 ' + esc(t('Blokla'))) + '</button>' +
            '<button type="button" class="sb-msgr-btn sb-msgr-btn--danger" data-m-info-report>⚠ ' + esc(t('Şikayət et')) + '</button>' +
            '</div>';

        els.infoBody.innerHTML = html;

        els.infoBody.querySelectorAll('[data-zoom]').forEach(function (img) {
            img.addEventListener('click', function () { openLightbox(img.getAttribute('src')); });
        });

        // Etiket + qeyd avtomatik yadda saxlanır (blur-da).
        var tagInput = els.infoBody.querySelector('[data-m-tag]');
        tagInput.addEventListener('blur', function () {
            api('/api/chat/conversations/' + d.conversationId + '/tag', { method: 'POST', json: { tag: tagInput.value } })
                .then(function () { toast(t('Yadda saxlanıldı'), 'success'); loadConversations(); })
                .catch(function (e) { toast(e.message, 'error'); });
        });
        var noteInput = els.infoBody.querySelector('[data-m-note]');
        noteInput.addEventListener('blur', function () {
            api('/api/chat/conversations/' + d.conversationId + '/note', { method: 'POST', json: { note: noteInput.value } })
                .then(function () { toast(t('Yadda saxlanıldı'), 'success'); })
                .catch(function (e) { toast(e.message, 'error'); });
        });

        els.infoBody.querySelector('[data-m-info-fav]').addEventListener('click', function () {
            api('/api/chat/conversations/' + d.conversationId + '/fav', { method: 'POST' })
                .then(function () { loadInfo(); loadConversations(); });
        });
        els.infoBody.querySelector('[data-m-info-block]').addEventListener('click', function () {
            api('/api/chat/block/' + encodeURIComponent(d.peerUserId), { method: 'POST' })
                .then(function (r) {
                    toast(r.blocked ? t('İstifadəçi bloklandı') : t('Blok götürüldü'), 'success');
                    loadInfo();
                })
                .catch(function (e) { toast(e.message, 'error'); });
        });
        els.infoBody.querySelector('[data-m-info-report]').addEventListener('click', function () {
            openModal('<h3>' + esc(t('Şikayət et')) + '</h3>' +
                '<textarea rows="4" maxlength="1000" placeholder="' + esc(t('Səbəbi qısaca izah et...')) + '" data-m-report-reason></textarea>' +
                '<div class="sb-msgr-modal__foot">' +
                '<button type="button" class="sb-msgr-btn sb-msgr-btn--ghost" data-close>' + esc(t('İmtina')) + '</button>' +
                '<button type="button" class="sb-msgr-btn sb-msgr-btn--danger" data-ok>' + esc(t('Göndər')) + '</button></div>');
            els.modalCard.querySelector('[data-ok]').addEventListener('click', function () {
                var reason = els.modalCard.querySelector('[data-m-report-reason]').value;
                api('/api/chat/report', { method: 'POST', json: { peerUserId: d.peerUserId, conversationId: d.conversationId, reason: reason } })
                    .then(function () { closeModal(); toast(t('Şikayət göndərildi. Komandamız baxacaq.'), 'success'); })
                    .catch(function (e) { toast(e.message, 'error'); });
            });
        });
    }

    // ---- Səs yazma ----
    var rec = { mr: null, chunks: [], timer: null, secs: 0, stream: null, sendOnStop: false };
    function startRecording() {
        if (!navigator.mediaDevices || !window.MediaRecorder) { toast(t('Brauzerin səs yazmağı dəstəkləmir.'), 'error'); return; }
        navigator.mediaDevices.getUserMedia({ audio: true }).then(function (stream) {
            var mime = MediaRecorder.isTypeSupported('audio/webm;codecs=opus') ? 'audio/webm;codecs=opus'
                : (MediaRecorder.isTypeSupported('audio/mp4') ? 'audio/mp4' : '');
            rec.stream = stream;
            rec.chunks = []; rec.secs = 0; rec.sendOnStop = false;
            rec.mr = mime ? new MediaRecorder(stream, { mimeType: mime }) : new MediaRecorder(stream);
            rec.mr.ondataavailable = function (e) { if (e.data && e.data.size) rec.chunks.push(e.data); };
            rec.mr.onstop = function () {
                stream.getTracks().forEach(function (tr) { tr.stop(); });
                if (rec.sendOnStop && rec.chunks.length && state.active) {
                    var blob = new Blob(rec.chunks, { type: rec.mr.mimeType || 'audio/webm' });
                    uploadAndSend('/api/chat/send-voice', new File([blob], 'voice', { type: blob.type }),
                        { durationSeconds: Math.max(1, rec.secs) });
                }
                els.recbar.hidden = true;
                clearInterval(rec.timer);
            };
            rec.mr.start();
            els.recbar.hidden = false;
            els.recTime.textContent = '0:00';
            rec.timer = setInterval(function () {
                rec.secs++;
                els.recTime.textContent = fmtDur(rec.secs);
                if (rec.secs >= 300) stopRecording(true);
            }, 1000);
        }).catch(function () { toast(t('Mikrofona icazə verilmədi.'), 'error'); });
    }
    function stopRecording(send) {
        if (!rec.mr || rec.mr.state === 'inactive') return;
        rec.sendOnStop = !!send;
        rec.mr.stop();
    }

    // ---- Mesaj axtarışı (söhbət daxili) ----
    var msgSearchTimer = null;
    function runMsgSearch() {
        var q = els.msgsearchInput.value.trim();
        if (!state.active || q.length < 2) { els.msgsearchResults.hidden = true; return; }
        api('/api/chat/conversations/' + state.active.id + '/search?q=' + encodeURIComponent(q))
            .then(function (hits) {
                els.msgsearchResults.innerHTML = (hits && hits.length)
                    ? hits.map(function (h) {
                        return '<div class="sb-msgr-msgsearch__hit"><span>' + esc(h.body || '') + '</span>' +
                            '<small>' + esc((h.senderId === ME ? t('Sən') : h.senderFullName) + ' · ' + fmtDay(h.createdDate) + ' ' + fmtTime(h.createdDate)) + '</small></div>';
                    }).join('')
                    : '<div class="sb-msgr-msgsearch__hit" style="color:var(--m-muted)">' + esc(t('Nəticə tapılmadı')) + '</div>';
                els.msgsearchResults.hidden = false;
            }).catch(function () { });
    }

    // ---- Hadisə bağlamaları ----
    els.search.addEventListener('input', function () {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(function () {
            state.search = els.search.value.trim();
            loadConversations();
        }, 280);
    });

    root.querySelectorAll('[data-m-filter]').forEach(function (chip) {
        chip.addEventListener('click', function () {
            root.querySelectorAll('[data-m-filter]').forEach(function (c) { c.classList.remove('is-active'); });
            chip.classList.add('is-active');
            state.filter = chip.getAttribute('data-m-filter');
            loadConversations();
        });
    });

    els.back.addEventListener('click', function () { root.classList.remove('is-thread-open'); });

    els.send.addEventListener('click', doSend);
    els.input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); doSend(); }
        if (e.key === 'Escape' && state.editing) exitEditMode();
    });
    els.input.addEventListener('input', function () { autosize(); sendTyping(); });

    els.msgs.addEventListener('scroll', function () {
        if (els.msgs.scrollTop < 80 && state.page < state.pages && !state.loadingOlder) {
            state.loadingOlder = true;
            loadMessages(state.page + 1, false).finally(function () { state.loadingOlder = false; });
        }
        if (isNearBottom()) { els.jump.hidden = true; state.unseenWhileScrolled = 0; }
    });
    els.jump.addEventListener('click', function () { scrollToBottom(true); markRead(); });

    // Header əməliyyatları.
    els.head.querySelector('[data-m-act="pin"]').addEventListener('click', function () {
        if (!state.active) return;
        api('/api/chat/conversations/' + state.active.id + '/pin', { method: 'POST' }).then(function (r) {
            state.active.isPinned = r.value; paintHeadToggles(); loadConversations();
        });
    });
    els.head.querySelector('[data-m-act="mute"]').addEventListener('click', function () {
        if (!state.active) return;
        api('/api/chat/conversations/' + state.active.id + '/mute', { method: 'POST' }).then(function (r) {
            state.active.isMuted = r.value; paintHeadToggles(); loadConversations();
            toast(r.value ? t('Söhbət səssizə alındı') : t('Səssiz rejim söndürüldü'), 'success');
        });
    });
    els.head.querySelector('[data-m-act="info"]').addEventListener('click', function () { toggleInfo(); });
    els.head.querySelector('[data-m-act="searchmsg"]').addEventListener('click', function () {
        els.msgsearch.hidden = !els.msgsearch.hidden;
        if (!els.msgsearch.hidden) els.msgsearchInput.focus();
        else els.msgsearchResults.hidden = true;
    });
    $('[data-m-msgsearch-close]').addEventListener('click', function () {
        els.msgsearch.hidden = true; els.msgsearchResults.hidden = true;
    });
    els.msgsearchInput.addEventListener('input', function () {
        clearTimeout(msgSearchTimer);
        msgSearchTimer = setTimeout(runMsgSearch, 320);
    });

    $('[data-m-info-close]').addEventListener('click', function () { toggleInfo(false); });

    // Composer popoverləri.
    els.attachBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        var willOpen = els.attachMenu.hidden;
        closePopovers();
        els.attachMenu.hidden = !willOpen;
    });
    els.emojiBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        buildEmoji();
        var willOpen = els.emoji.hidden;
        closePopovers();
        els.emoji.hidden = !willOpen;
    });
    $('[data-m-quickreplies-btn]').addEventListener('click', function (e) {
        e.stopPropagation();
        closePopovers();
        els.qr.hidden = false;
        if (state.quickReplies === null) loadQuickReplies(); else renderQuickReplies();
    });
    $('[data-m-qr-close]').addEventListener('click', closePopovers);
    els.qrForm.addEventListener('submit', function (e) {
        e.preventDefault();
        var titleEl = $('[data-m-qr-title]'), bodyEl = $('[data-m-qr-body]');
        api('/api/chat/quick-replies', { method: 'POST', json: { title: titleEl.value, body: bodyEl.value } })
            .then(function () { titleEl.value = ''; bodyEl.value = ''; loadQuickReplies(); })
            .catch(function (err) { toast(err.message, 'error'); });
    });

    document.addEventListener('click', function (e) {
        if (!e.target.closest('[data-m-attachmenu]') && !e.target.closest('[data-m-emoji]') &&
            !e.target.closest('[data-m-qr]') && !e.target.closest('[data-m-ctx]') &&
            !e.target.closest('[data-m-attach-btn]') && !e.target.closest('[data-m-emoji-btn]')) {
            closePopovers();
        }
    });
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { closePopovers(); closeModal(); els.lightbox.hidden = true; }
    });

    els.fileImage.addEventListener('change', function () {
        if (this.files[0]) uploadAndSend('/api/chat/send-image', this.files[0], {});
        this.value = ''; closePopovers();
    });
    els.fileDoc.addEventListener('change', function () {
        if (this.files[0]) uploadAndSend('/api/chat/send-file', this.files[0], {});
        this.value = ''; closePopovers();
    });

    els.voiceBtn.addEventListener('click', startRecording);
    $('[data-m-rec-cancel]').addEventListener('click', function () { stopRecording(false); });
    $('[data-m-rec-send]').addEventListener('click', function () { stopRecording(true); });

    $('[data-m-replybar-close]').addEventListener('click', function () {
        if (state.editing) exitEditMode(); else hideReplyBar();
    });
    $('[data-m-productchip-close]').addEventListener('click', hideProductChip);

    // Drag & drop.
    ['dragenter', 'dragover'].forEach(function (ev) {
        els.body.addEventListener(ev, function (e) {
            if (!state.active) return;
            e.preventDefault(); els.drag.hidden = false;
        });
    });
    ['dragleave', 'drop'].forEach(function (ev) {
        els.body.addEventListener(ev, function (e) {
            e.preventDefault();
            if (ev === 'dragleave' && e.relatedTarget && els.body.contains(e.relatedTarget)) return;
            els.drag.hidden = true;
        });
    });
    els.body.addEventListener('drop', function (e) {
        if (!state.active || !e.dataTransfer || !e.dataTransfer.files.length) return;
        var f = e.dataTransfer.files[0];
        if (f.type.indexOf('image/') === 0) uploadAndSend('/api/chat/send-image', f, {});
        else if (f.type === 'application/pdf') uploadAndSend('/api/chat/send-file', f, {});
        else toast(t('Yalnız şəkil və PDF göndərmək olar.'), 'error');
    });

    $('[data-m-lightbox-close]').addEventListener('click', function () { els.lightbox.hidden = true; });
    els.lightbox.addEventListener('click', function (e) { if (e.target === els.lightbox) els.lightbox.hidden = true; });
    els.modal.addEventListener('click', function (e) { if (e.target === els.modal) closeModal(); });

    // Pəncərə fokusu qayıdanda açıq söhbəti oxundu işarələ.
    window.addEventListener('focus', function () { if (state.active) markRead(); });

    // ---- Başlatma ----
    setupHub();
    loadConversations().then(function () {
        if (INITIAL_PEER) {
            api('/api/chat/conversations/with/' + encodeURIComponent(INITIAL_PEER), { method: 'POST' })
                .then(function (conv) {
                    var existing = state.convs.find(function (c) { return c.id === conv.id; });
                    if (!existing) { state.convs.unshift(conv); renderConversations(); }
                    openConversation(existing || conv);
                    if (INITIAL_PRODUCT) {
                        api('/api/chat/product-preview/' + INITIAL_PRODUCT)
                            .then(function (p) { if (p) showProductChip(p); })
                            .catch(function () { });
                    }
                })
                .catch(function (e) { toast(e.message, 'error'); });
        }
    });
})();
