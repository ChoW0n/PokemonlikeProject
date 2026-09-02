window.pokemonWiki = {
    scrollToSection: function (id) {
        const section = document.getElementById(id);
        if (!section) return;

        history.replaceState(null, "", `${window.location.pathname}#${encodeURIComponent(id)}`);
        section.scrollIntoView({ behavior: "smooth", block: "start" });
    }
};