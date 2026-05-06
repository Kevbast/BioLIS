// ==========================================================
// 1. TU CÓDIGO ORIGINAL DE ANIMACIONES (UI)
// ==========================================================
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

// ==========================================================
// 1.1 Normalizador de mensajes para SweetAlert
// ==========================================================
window.normalizeSwalMessage = function (message) {
    if (message === null || message === undefined) {
        return '';
    }

    if (typeof message !== 'string') {
        return message;
    }

    const trimmed = message.trim();
    if (!trimmed) {
        return '';
    }

    try {
        const parsed = JSON.parse(trimmed);
        if (typeof parsed === 'string') {
            return parsed;
        }

        if (parsed && typeof parsed === 'object') {
            return parsed.message || parsed.error || parsed.title || trimmed;
        }
    } catch {
        // Ignorar errores de parseo
    }

    if ((trimmed.startsWith('"') && trimmed.endsWith('"')) || (trimmed.startsWith("'") && trimmed.endsWith("'"))) {
        return trimmed.substring(1, trimmed.length - 1);
    }

    return trimmed;
};

// ==========================================================
// 2. NUEVA LÓGICA DE SIGNALR (NOTIFICACIONES EN TIEMPO REAL)
// ==========================================================
document.addEventListener("DOMContentLoaded", function () {
    // Verificamos si las variables globales (inyectadas en _Layout.cshtml) existen.
    // Si no existen (ej. usuario no logueado), no hacemos nada.
    if (typeof signalRToken === 'undefined' || !signalRToken || typeof apiBaseUrl === 'undefined' || !apiBaseUrl) {
        return;
    }

    // 1. Construir la conexión usando el token JWT
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`${apiBaseUrl}/notificationHub`, {
            accessTokenFactory: () => signalRToken
        })
        .withAutomaticReconnect()
        .build();

    // 2. Escuchar mensajes entrantes DESDE la API
    connection.on("ReceiveNotification", function (notification) {
        console.log("¡Notificación recibida en tiempo real!", notification);

        // Actualizar el contador de la campanita visualmente
        const badge = document.getElementById('notification-badge');
        const badgeInline = document.getElementById('notification-badge-inline');

        if (badge) {
            let currentCount = parseInt(badge.innerText) || 0;
            let newCount = currentCount + 1;

            badge.innerText = newCount;
            badge.style.display = 'inline-block';

            if (badgeInline) {
                badgeInline.innerText = newCount;
                badgeInline.style.display = 'inline-block';
            }
        }

        // Si existe la función de refrescar la vista previa (en Default.cshtml), la llamamos
        if (typeof window.refreshNotificationPreview === 'function') {
            window.refreshNotificationPreview();
        }

        // Mostrar un Toast (si usas SweetAlert2 en tu proyecto)
        if (typeof Swal !== 'undefined') {
            const isCritical = notification.title.toLowerCase().includes('crítico') || notification.title.toLowerCase().includes('urgente');
            const normalizedMessage = window.normalizeSwalMessage
                ? window.normalizeSwalMessage(notification.message)
                : notification.message;

            Swal.fire({
                title: notification.title,
                text: normalizedMessage,
                icon: isCritical ? 'error' : 'info',
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: 5000,
                timerProgressBar: true
            });
        }
    });

    // 3. Iniciar conexión a SignalR
    async function startSignalR() {
        try {
            await connection.start();
            console.log("SignalR Conectado al Hub.");

            // Aquí, si en el futuro necesitas unir al usuario a un grupo específico (ej. "User_123")
            // puedes hacerlo llamando a un método del Hub:
            // if (typeof currentUserId !== 'undefined' && currentUserId) {
            //     await connection.invoke("JoinUserGroup", currentUserId);
            // }
        } catch (err) {
            console.error("Error al conectar con SignalR:", err);
            // Intentar reconectar en 5 segundos si falla la primera vez
            setTimeout(startSignalR, 5000);
        }
    }

    // Arrancamos el motor
    startSignalR();
});