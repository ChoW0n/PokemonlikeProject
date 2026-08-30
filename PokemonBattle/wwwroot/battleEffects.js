window.battleEffects = {
    cfg: null,

    async load() {
        if (!this.cfg) {
            const res = await fetch('effects-config.json', { cache: 'no-store' });
            this.cfg = await res.json();
        }
        return this.cfg;
    },

    async play(kind, attackerSide, colorHex) {
        const cfg = await this.load();
        const t = cfg[kind] || cfg.beam;
        const defenderSide = attackerSide === 'hero' ? 'enemy' : 'hero';

        const attackerSprite = document.getElementById('sprite-' + attackerSide);
        const defenderSprite = document.getElementById('sprite-' + defenderSide);
        if (!attackerSprite || !defenderSprite) return;

        //예비 동작: 공격자가 살짝 돌진
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

        if (kind === 'beam') {
            //실제 두 지점 사이 거리/각도를 계산해서 진짜 궤적을 그림
            const dist = Math.hypot(dx - ax, dy - ay);
            const angle = Math.atan2(dy - ay, dx - ax) * 180 / Math.PI;
            fx.style.left = ax + 'px';
            fx.style.top = ay + 'px';
            fx.style.width = dist + 'px';
            fx.style.height = '10px';
            fx.style.transformOrigin = '0 50%';
            fx.style.transform = `rotate(${angle}deg)`;
            fx.innerHTML = '<span class="fx-beam-trail"></span>';
        } else if (kind === 'slash') {
            fx.style.left = dx + 'px';
            fx.style.top = dy + 'px';
            fx.innerHTML = '<span class="slash-line slash-1"></span><span class="slash-line slash-2"></span><span class="slash-burst"></span>';
        } else {
            fx.style.left = ax + 'px';
            fx.style.top = ay + 'px';
            fx.innerHTML = '<span class="sparkle-ring ring-1"></span><span class="sparkle-ring ring-2"></span>';
        }

        document.body.appendChild(fx);
        requestAnimationFrame(() => fx.classList.add('is-active'));

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
