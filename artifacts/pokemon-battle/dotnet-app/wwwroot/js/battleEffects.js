window.battleEffects = {
    cfg: null,
    sequence: 0,
    activeSequence: 0,

    async load() {
        if (!this.cfg) {
            const res = await fetch('effects-config.json', { cache: 'no-store' });
            this.cfg = await res.json();
        }
        return this.cfg;
    },

    beginSequence() {
        this.activeSequence = ++this.sequence;
        this.cleanup();
        return this.activeSequence;
    },

    cancel() {
        this.sequence++;
        this.activeSequence = this.sequence;
        this.cleanup();
    },

    endSequence() {
        this.cleanup();
        this.sequence++;
        this.activeSequence = 0;
    },

    isCurrent(sequence) {
        return sequence !== 0 && sequence === this.activeSequence;
    },

    sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, Math.max(0, ms)));
    },

    scaled(ms, speed) {
        return Math.max(16, (ms || 0) / Math.max(1, Number(speed) || 1));
    },

    hitstop(ms, speed, sequence) {
        const duration = this.scaled(ms, speed);
        document.body.classList.add('hitstop');
        return this.sleep(duration).finally(() => {
            if (!sequence || this.isCurrent(sequence)) document.body.classList.remove('hitstop');
        });
    },

    // 로그 입력도 FX와 같은 재생 배율을 사용한다.
    typeLastLine(durationMs, sequence = this.activeSequence) {
        return new Promise(resolve => {
            const lines = document.querySelectorAll('.log-line.is-latest .log-text');
            const el = lines[lines.length - 1];
            if (!el) { resolve(); return; }

            const fullText = el.getAttribute('data-full') || el.textContent || '';
            el.setAttribute('data-full', fullText);
            el.textContent = '';
            if (!fullText.length) { resolve(); return; }

            const perCharMs = Math.max(8, durationMs / fullText.length);
            let i = 0;
            const tick = () => {
                if (sequence && !this.isCurrent(sequence)) { resolve(); return; }
                i++;
                el.textContent = fullText.slice(0, i);
                if (i < fullText.length) setTimeout(tick, perCharMs);
                else resolve();
            };
            tick();
        });
    },

    actor(side) {
        return document.getElementById('sprite-' + side);
    },

    layer() {
        return document.getElementById('battle-fx-layer');
    },

    relativePoint(element, layer) {
        const rect = element.getBoundingClientRect();
        const layerRect = layer.getBoundingClientRect();
        return {
            x: rect.left + rect.width / 2 - layerRect.left,
            y: rect.top + rect.height / 2 - layerRect.top
        };
    },

    resolveKind(presentationKey, category, fallback) {
        const key = (presentationKey || '').toLowerCase();
        const map = {
            tackle: 'impact', scratch: 'slash', 'quick-attack': 'dash',
            'extreme-speed': 'dash', 'aqua-jet': 'dash', 'mach-punch': 'dash',
            thunderbolt: 'beam', thunder: 'beam', 'charge-beam': 'beam',
            flamethrower: 'beam', 'fire-blast': 'burst', 'ice-beam': 'beam',
            psychic: 'wave', psybeam: 'beam', 'shadow-ball': 'orb',
            'water-pulse': 'wave', surf: 'wave', 'hydro-pump': 'beam',
            'energy-ball': 'orb', 'sludge-bomb': 'orb', 'dark-pulse': 'wave',
            'aerial-ace': 'slash', 'air-slash': 'slash', 'night-slash': 'slash',
            'cross-chop': 'slash', 'sacred-sword': 'slash', 'psycho-cut': 'slash',
            earthquake: 'quake', 'rock-slide': 'quake', 'stone-edge': 'slash',
            'razor-leaf': 'slash', 'leaf-blade': 'slash',
            'pin-missile': 'multi', 'bullet-seed': 'multi',
            'fury-swipes': 'multi', 'double-slap': 'multi',
            recover: 'heal', roost: 'heal', rest: 'heal', 'soft-boiled': 'heal',
            protect: 'shield', detect: 'shield', 'kings-shield': 'shield',
            'solar-beam': 'charge', 'skull-bash': 'charge', fly: 'charge',
            bounce: 'charge', 'future-sight': 'delayed', 'doom-desire': 'delayed',
            'self-destruct': 'recoil', explosion: 'recoil', memento: 'recoil'
        };
        if (map[key]) return map[key];
        if (category === 'status') return 'status';
        const safeFallback = { bolt: 'beam', pierce: 'projectile', sparkle: 'status' };
        return safeFallback[fallback] || fallback || (category === 'special' ? 'beam' : 'impact');
    },

    async play(
        kind, attackerSide, colorHex, moveName = '기술', attackerName = '',
        typeLabel = '', phase = 'impact', speed = 1, presentationKey = '',
        target = 'opponent', hitIndex = 0, hitCount = 0, damage = 0,
        effectiveness = 1, critical = false, attackerActorId = '', defenderActorId = '') {
        const cfg = await this.load();
        const sequence = this.activeSequence || this.sequence;
        const attacker = this.actor(attackerSide);
        const defender = this.actor(attackerSide === 'hero' ? 'enemy' : 'hero');
        const layer = this.layer();
        if (!attacker || !defender || !layer || !this.isCurrent(sequence)) return;

        const category = attacker.dataset.category || '';
        const resolvedKind = this.resolveKind(presentationKey, category, kind);
        const timing = cfg[resolvedKind] || cfg[kind] || cfg.impact || {};
        const reduced = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
        const factor = reduced ? 2.5 : 1;

        if (phase === 'faint') {
            attacker.dataset.state = 'fainted';
            attacker.classList.add('is-fainted', 'pose-hit');
            await this.sleep(this.scaled(260, speed) * factor);
            return;
        }

        if (phase === 'switch') {
            attacker.dataset.state = 'switching';
            attacker.classList.add('is-switching');
            await this.sleep(this.scaled(260, speed) * factor);
            attacker.classList.remove('is-switching');
            attacker.dataset.state = 'idle';
            return;
        }

        if (phase === 'announce') {
            const banner = document.createElement('div');
            banner.className = 'move-cast-banner';
            banner.dataset.sequence = sequence;
            banner.setAttribute('role', 'status');
            banner.style.setProperty('--fx-color', colorHex);
            banner.innerHTML =
                `<span class="move-cast-attacker">${attackerName}</span>` +
                `<strong class="move-cast-name">${moveName}</strong>` +
                `<span class="move-cast-type" style="--fx-color:${colorHex}">${typeLabel}</span>`;
            layer.appendChild(banner);
            requestAnimationFrame(() => banner.classList.add('is-active'));
            await this.sleep(this.scaled(120, speed) * factor);
            return;
        }

        if (phase === 'windup') {
            attacker.dataset.state = 'windup';
            attacker.style.setProperty('--lunge-duration', this.scaled(timing.windupMs, speed) * factor + 'ms');
            attacker.classList.remove('pose-attack', 'pose-hit');
            attacker.classList.add(attackerSide === 'hero' ? 'lunge-right' : 'lunge-left', 'pose-attack');
            await this.sleep(this.scaled(timing.windupMs, speed) * factor);
            if (!this.isCurrent(sequence)) this.cleanup();
            return;
        }

        if (phase === 'recovery') {
            await this.sleep(this.scaled(timing.tailMs || 100, speed) * factor);
            if (this.isCurrent(sequence)) this.cleanup();
            return;
        }

        if (phase !== 'impact') return;

        const targetActor = target === 'self' ? attacker : defender;
        const from = this.relativePoint(attacker, layer);
        const to = this.relativePoint(targetActor, layer);
        const fx = document.createElement('div');
        fx.className = `fx-dynamic fx-${resolvedKind}`;
        fx.dataset.sequence = sequence;
        fx.style.setProperty('--fx-color', colorHex);
        fx.style.setProperty('--fx-duration', this.scaled(timing.mainMs, speed) * factor + 'ms');
        fx.style.left = to.x + 'px';
        fx.style.top = to.y + 'px';
        fx.setAttribute('aria-hidden', 'true');
        this.buildFx(fx, resolvedKind, from, to, colorHex);
        layer.appendChild(fx);
        requestAnimationFrame(() => fx.classList.add('is-active'));

        targetActor.dataset.state = 'hit';
        targetActor.classList.remove('pose-attack');
        targetActor.classList.add('pose-hit');
        targetActor.style.setProperty('--shake-duration', this.scaled(timing.shakeMs || 180, speed) * factor + 'ms');
        if ((timing.shakeMs || 0) > 0 && resolvedKind !== 'heal' && resolvedKind !== 'status') {
            targetActor.classList.add('is-shaking');
        }

        if (damage > 0) this.addDamageBadge(layer, to, damage, critical, effectiveness, colorHex, sequence);
        if ((timing.flashMs || 0) > 0) this.addFlash(layer, timing.flashMs, speed, factor, sequence);
        if ((timing.hitstopMs || 0) > 0 && !['heal', 'status', 'shield'].includes(resolvedKind)) {
            await this.hitstop(timing.hitstopMs, speed, sequence);
        }
        await this.sleep(this.scaled(timing.mainMs || 220, speed) * factor);
        if (!this.isCurrent(sequence)) return;
        if (fx.isConnected) fx.remove();
        targetActor.classList.remove('is-shaking');
        targetActor.dataset.state = 'idle';
    },

    buildFx(fx, kind, from, to, color) {
        const dx = to.x - from.x;
        const dy = to.y - from.y;
        const distance = Math.max(24, Math.hypot(dx, dy));
        const angle = Math.atan2(dy, dx) * 180 / Math.PI;
        fx.style.setProperty('--fx-angle', angle + 'deg');
        fx.style.setProperty('--fx-distance', distance + 'px');
        if (kind === 'beam' || kind === 'projectile' || kind === 'dash') {
            fx.innerHTML = '<span class="fx-beam-trail"></span><span class="fx-projectile-core"></span>';
            fx.style.width = distance + 'px';
            fx.style.height = kind === 'beam' ? '18px' : '12px';
            fx.style.transform = `translate(0, -50%) rotate(${angle}deg)`;
            fx.style.transformOrigin = '0 50%';
        } else if (kind === 'slash' || kind === 'multi') {
            fx.innerHTML = '<span class="slash-line slash-1"></span><span class="slash-line slash-2"></span>' +
                '<span class="slash-line slash-3"></span><span class="slash-burst"></span>';
        } else if (kind === 'wave' || kind === 'quake') {
            fx.innerHTML = '<span class="wave-ring ring-1"></span><span class="wave-ring ring-2"></span>' +
                '<span class="wave-core"></span>';
        } else if (kind === 'orb' || kind === 'burst' || kind === 'impact' || kind === 'recoil') {
            fx.innerHTML = '<span class="burst-particle p-1"></span><span class="burst-particle p-2"></span>' +
                '<span class="burst-particle p-3"></span><span class="burst-particle p-4"></span>' +
                '<span class="burst-core"></span>';
        } else if (kind === 'heal' || kind === 'status' || kind === 'shield') {
            fx.innerHTML = '<span class="sparkle-ring ring-1"></span><span class="sparkle-ring ring-2"></span>' +
                '<span class="status-sparkles">✦</span>';
        } else {
            fx.innerHTML = '<span class="impact-crack crack-1"></span><span class="impact-crack crack-2"></span>' +
                '<span class="impact-crack crack-3"></span><span class="burst-core"></span>';
        }
    },

    addDamageBadge(layer, point, damage, critical, effectiveness, color, sequence) {
        const badge = document.createElement('span');
        badge.className = 'damage-badge' + (critical ? ' is-critical' : '');
        badge.dataset.sequence = sequence;
        badge.style.left = point.x + 'px';
        badge.style.top = (point.y - 42) + 'px';
        badge.style.setProperty('--fx-color', color);
        badge.textContent = `-${damage}`;
        if (effectiveness === 0) badge.textContent = '효과 없음';
        else if (effectiveness >= 2) badge.textContent += ' · 약점';
        else if (effectiveness > 0 && effectiveness < 1) badge.textContent += ' · 저항';
        layer.appendChild(badge);
        requestAnimationFrame(() => badge.classList.add('is-active'));
        setTimeout(() => badge.remove(), 650);
    },

    addFlash(layer, duration, speed, factor, sequence) {
        const flash = document.createElement('div');
        flash.className = 'impact-flash-overlay';
        flash.dataset.sequence = sequence;
        flash.style.setProperty('--fx-duration', this.scaled(duration, speed) * factor + 'ms');
        layer.appendChild(flash);
        setTimeout(() => flash.remove(), this.scaled(duration, speed) * factor + 40);
    },

    cleanup() {
        document.querySelectorAll('.fx-dynamic, .impact-flash-overlay, .damage-badge, .move-cast-banner').forEach(el => el.remove());
        document.querySelectorAll('.fighter-sprite').forEach(sprite => {
            sprite.classList.remove('lunge-right', 'lunge-left', 'pose-attack', 'pose-hit', 'is-shaking');
            sprite.dataset.state = sprite.classList.contains('is-fainted') ? 'fainted' : 'idle';
        });
        document.body.classList.remove('hitstop');
    },

    actorTransition(side, state) {
        const actor = this.actor(side);
        if (!actor) return;
        actor.dataset.state = state;
        actor.classList.toggle('is-switching', state === 'switching');
        if (state === 'fainted') actor.classList.add('is-fainted');
    }
};