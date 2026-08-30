window.battleEffects = {
    cfg: null,

    async load() {
        if (!this.cfg) {
            const res = await fetch('effects-config.json', { cache: 'no-store' });
            this.cfg = await res.json();
        }
        return this.cfg;
    },

    hitstop(ms) {
        document.body.classList.add('hitstop');
        return new Promise(r => setTimeout(() => {
            document.body.classList.remove('hitstop');
            r();
        }, ms));
    },

    //로그 패널의 가장 최근 줄을 타이핑 애니메이션으로 표시 (한 글자씩 나타남)
    typeLastLine(durationMs) {
        return new Promise(resolve => {
            const lines = document.querySelectorAll('.log-line.is-latest .log-text');
            const el = lines[lines.length - 1];
            if (!el) { resolve(); return; }

            const fullText = el.getAttribute('data-full') || el.textContent;
            el.setAttribute('data-full', fullText);
            el.textContent = '';

            const totalChars = fullText.length;
            if (totalChars === 0) { resolve(); return; }

            const perCharMs = Math.max(8, durationMs / totalChars);
            let i = 0;

            const tick = () => {
                i++;
                el.textContent = fullText.slice(0, i);
                if (i < totalChars) {
                    setTimeout(tick, perCharMs);
                } else {
                    resolve();
                }
            };
            tick();
        });
    },

    async play(kind, attackerSide, colorHex) {
        const cfg = await this.load();
        const t = cfg[kind] || cfg.burst;
        const defenderSide = attackerSide === 'hero' ? 'enemy' : 'hero';

        const attackerSprite = document.getElementById('sprite-' + attackerSide);
        const defenderSprite = document.getElementById('sprite-' + defenderSide);
        if (!attackerSprite || !defenderSprite) return;

        attackerSprite.style.setProperty('--lunge-duration', t.windupMs + 'ms');
        attackerSprite.classList.add(attackerSide === 'hero' ? 'lunge-right' : 'lunge-left');
        await new Promise(r => setTimeout(r, t.windupMs));

        const arect = attackerSprite.getBoundingClientRect();
        const drect = defenderSprite.getBoundingClientRect();
        const ax = arect.left + arect.width / 2 + window.scrollX;
        const ay = arect.top + arect.height / 2 + window.scrollY;
        const dx = drect.left + drect.width / 2 + window.scrollX;
        const dy = drect.top + drect.height / 2 + window.scrollY;

        const fx = document.createElement('div');
        fx.className = `fx-dynamic fx-${kind}`;
        fx.style.setProperty('--fx-color', colorHex);
        fx.style.setProperty('--fx-duration', t.mainMs + 'ms');
        fx.style.pointerEvents = 'none';

        if (kind === 'pierce') {
            const dist = Math.hypot(dx - ax, dy - ay);
            const angle = Math.atan2(dy - ay, dx - ax) * 180 / Math.PI;
            fx.style.left = ax + 'px';
            fx.style.top = ay + 'px';
            fx.style.width = dist + 'px';
            fx.style.height = '10px';
            fx.style.transformOrigin = '0 50%';
            fx.style.transform = `rotate(${angle}deg)`;
            fx.innerHTML = '<span class="pierce-orb"></span>';
        } else if (kind === 'burst') {
            fx.style.left = dx + 'px';
            fx.style.top = dy + 'px';
            let particles = '';
            for (let i = 0; i < 8; i++) {
                particles += `<span class="burst-particle" style="--angle:${i * 45}deg"></span>`;
            }
            fx.innerHTML = particles + '<span class="burst-core"></span>';
        } else if (kind === 'impact') {
            fx.style.left = dx + 'px';
            fx.style.top = dy + 'px';
            fx.innerHTML = '<span class="impact-crack crack-1"></span><span class="impact-crack crack-2"></span><span class="impact-crack crack-3"></span>';
        } else if (kind === 'multi') {
            fx.style.left = dx + 'px';
            fx.style.top = dy + 'px';
            fx.innerHTML = '<span class="slash-line slash-1"></span><span class="slash-line slash-2"></span><span class="slash-line slash-3"></span><span class="slash-burst"></span>';
        } else {
            fx.style.left = ax + 'px';
            fx.style.top = ay + 'px';
            fx.innerHTML = '<span class="sparkle-ring ring-1"></span><span class="sparkle-ring ring-2"></span>';
        }

        document.body.appendChild(fx);
        requestAnimationFrame(() => fx.classList.add('is-active'));

        if (kind !== 'sparkle' && t.hitstopMs > 0) {
            setTimeout(() => this.hitstop(t.hitstopMs), t.windupMs === 0 ? 40 : 60);
        }

        if (t.flashMs > 0) {
            const flash = document.createElement('div');
            flash.className = 'impact-flash-overlay';
            flash.style.setProperty('--fx-duration', t.flashMs + 'ms');
            document.body.appendChild(flash);
            setTimeout(() => flash.remove(), t.flashMs);
        }

        if (t.shakeMs > 0 && kind !== 'sparkle') {
            defenderSprite.style.setProperty('--shake-duration', t.shakeMs + 'ms');
            defenderSprite.classList.add('is-shaking');
            setTimeout(() => defenderSprite.classList.remove('is-shaking'), t.shakeMs);
        }

        await new Promise(r => setTimeout(r, t.mainMs + t.tailMs));

        fx.remove();
        attackerSprite.classList.remove('lunge-right', 'lunge-left');
        defenderSprite.classList.remove('is-shaking');
    }
};
