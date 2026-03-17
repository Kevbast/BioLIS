(() => {
    const pagesToEnhance = [
        '.orders-page',
        '.catalog-page',
        '.users-page',
        '.doctors-page',
        '.dashboard-home',
        '.patients-page'
    ];

    const hasTargetPage = pagesToEnhance.some(selector => document.querySelector(selector));
    if (!hasTargetPage)
        return;

    const fadeTargets = document.querySelectorAll(
        '.orders-header, .catalog-header, .users-header, .doctors-header, .card, .table-responsive, .alert'
    );

    let delayStep = 0;
    fadeTargets.forEach(element => {
        if (element.classList.contains('animate-fade-up') || element.closest('.modal'))
            return;

        const delay = Math.min(delayStep * 80, 480);
        element.classList.add('animate-fade-up');
        element.style.animationDelay = `${delay}ms`;
        delayStep++;
    });

    const pulseButtons = document.querySelectorAll('.btn i');
    pulseButtons.forEach(icon => {
        const button = icon.closest('.btn');
        if (button && !button.classList.contains('hover-pulse'))
            button.classList.add('hover-pulse');
    });
})();
