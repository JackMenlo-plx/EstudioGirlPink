/**
 * Studio Girl Pink - Realtime Client Module (SignalR)
 * Sincronización en tiempo real de Trabajos (Galería) y Promociones
 */
(function () {
    if (typeof signalR === "undefined") {
        console.warn("SignalR library not found. Realtime updates disabled.");
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/studio")
        .withAutomaticReconnect([0, 1500, 3000, 5000, 10000, 20000])
        .build();

    // 1. Notificación flotante sutil (Toast)
    function showToast(message, icon = "🌸") {
        let toastContainer = document.getElementById("studioRealtimeToastContainer");
        if (!toastContainer) {
            toastContainer = document.createElement("div");
            toastContainer.id = "studioRealtimeToastContainer";
            toastContainer.style.cssText = "position: fixed; bottom: 25px; right: 25px; z-index: 99999; display: flex; flex-direction: column; gap: 10px; max-width: 380px;";
            document.body.appendChild(toastContainer);
        }

        const toast = document.createElement("div");
        toast.style.cssText = "background: rgba(131, 24, 67, 0.95); color: #fff; padding: 12px 18px; border-radius: 12px; box-shadow: 0 8px 24px rgba(0,0,0,0.2); font-size: 0.9rem; line-height: 1.4; display: flex; align-items: center; gap: 12px; backdrop-filter: blur(8px); border: 1px solid rgba(244, 114, 182, 0.4); animation: slideInUp 0.3s ease-out;";
        toast.innerHTML = `<span style="font-size: 1.4rem;">${icon}</span><div>${message}</div>`;
        toastContainer.appendChild(toast);

        setTimeout(() => {
            toast.style.transition = "opacity 0.4s ease, transform 0.4s ease";
            toast.style.opacity = "0";
            toast.style.transform = "translateY(10px)";
            setTimeout(() => toast.remove(), 400);
        }, 5000);
    }

    // 2. Manejo de Actualizaciones de Portafolio / Trabajos
    connection.on("ReceivePortfolioUpdate", function (work) {
        console.log("Realtime: Portfolio updated", work);
        const carrusel = document.getElementById("carruselTrabajos");
        const emptyState = document.getElementById("trabajosEmptyState");

        if (work.action === "created") {
            if (emptyState) emptyState.style.display = "none";
            if (carrusel) {
                // Eliminar tarjeta previa con mismo ID si existiera
                const existing = document.getElementById("work-card-" + work.id);
                if (existing) existing.remove();

                const card = document.createElement("div");
                card.id = "work-card-" + work.id;
                card.style.cssText = "flex: 0 0 85%; max-width: 420px; scroll-snap-align: center; border-radius: 18px; overflow: hidden; background: #fff; box-shadow: 0 8px 24px rgba(236, 72, 153, 0.12); border: 2px solid #fce7f3; display: flex; flex-direction: column; animation: pulseGlow 1.2s ease-out;";
                
                const safeTitle = encodeURIComponent(work.title || "");
                const safeCategory = encodeURIComponent(work.category || "Uñas");
                const safeImage = encodeURIComponent(work.imagePath || "");

                card.innerHTML = `
                    <div style="width: 100%; height: 360px; background: #fff5f7; overflow: hidden; position: relative;">
                        <img src="${work.imagePath}" alt="${work.title}" style="width: 100%; height: 100%; object-fit: cover; display: block;" />
                        <span style="position: absolute; top: 12px; right: 12px; background: rgba(157, 23, 77, 0.9); color: #fff; padding: 4px 12px; border-radius: 999px; font-size: 0.8rem; font-weight: 600; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
                            ${work.category}
                        </span>
                    </div>
                    <div style="padding: 1.25rem; flex: 1; display: flex; flex-direction: column; justify-content: space-between;">
                        <div>
                            <h3 style="margin: 0 0 0.4rem 0; color: #831843; font-size: 1.2rem;">${work.title}</h3>
                            ${work.description ? `<p style="margin: 0; font-size: 0.92rem; color: #64748b; line-height: 1.5;">${work.description}</p>` : ''}
                        </div>
                        <div style="margin-top: 1rem; border-top: 1px solid #fce7f3; padding-top: 0.75rem; display: flex; justify-content: space-between; align-items: center; gap: 8px;">
                            <small style="color: #9d174d; font-weight: 600;">✨ Trabajo 100% Real</small>
                            <a href="/agendar?workId=${work.id}&workTitle=${safeTitle}&workCategory=${safeCategory}&workImage=${safeImage}" 
                               class="studio-girl-pink-button studio-girl-pink-button-primary" 
                               style="padding: 6px 14px; font-size: 0.82rem; font-weight: 600; text-decoration: none; border-radius: 999px;">
                                🌸 Quiero un diseño así &rarr;
                            </a>
                        </div>
                    </div>
                `;
                carrusel.prepend(card);
                showToast(`¡Nuevo trabajo agregado a la galería: <strong>${work.title}</strong>!`, "📸");
            }
        } else if (work.action === "deleted") {
            const el = document.getElementById("work-card-" + work.id);
            if (el) {
                el.style.transition = "opacity 0.4s, transform 0.4s";
                el.style.opacity = "0";
                el.style.transform = "scale(0.85)";
                setTimeout(() => el.remove(), 400);
            }
        }
    });

    // 3. Manejo de Actualizaciones de Promociones
    connection.on("ReceivePromotionUpdate", function (promo) {
        console.log("Realtime: Promotion updated", promo);
        const section = document.getElementById("promociones");
        const container = document.getElementById("promocionesCards");

        if (promo.action === "deleted" || !promo.isActive) {
            const el = document.getElementById("promo-card-" + promo.id);
            if (el) {
                el.style.transition = "opacity 0.4s, transform 0.4s";
                el.style.opacity = "0";
                el.style.transform = "scale(0.85)";
                setTimeout(() => {
                    el.remove();
                    if (container && container.children.length === 0 && section) {
                        section.style.display = "none";
                    }
                }, 400);
            }
        } else if (promo.action === "created" || promo.action === "toggled" || promo.action === "activated") {
            if (section) section.style.display = "block";
            if (container) {
                const existing = document.getElementById("promo-card-" + promo.id);
                if (existing) existing.remove();

                const card = document.createElement("div");
                card.id = "promo-card-" + promo.id;
                card.style.cssText = "background: #ffffff; border-radius: 16px; overflow: hidden; border: 2px solid #fda4af; box-shadow: 0 10px 25px rgba(244, 114, 182, 0.15); display: flex; flex-direction: column; animation: pulseGlow 1.2s ease-out;";

                card.innerHTML = `
                    ${promo.imagePath ? `
                    <div style="width: 100%; height: 220px; background: #fff5f7; display: flex; justify-content: center; align-items: center; overflow: hidden; border-bottom: 1px solid #fecdd3;">
                        <img src="${promo.imagePath}" alt="${promo.title}" style="width: 100%; height: 100%; object-fit: contain; padding: 6px;" />
                    </div>` : ''}
                    <div style="padding: 1.5rem; flex: 1; display: flex; flex-direction: column; justify-content: space-between;">
                        <div>
                            <span style="background: #ec4899; color: #fff; padding: 4px 12px; border-radius: 999px; font-size: 0.75rem; font-weight: bold; letter-spacing: 0.5px;">
                                ${promo.badgeText || "PROMO"}
                            </span>
                            <h3 style="margin: 0.75rem 0 0.5rem 0; color: #831843;">${promo.title}</h3>
                            <p style="color: #475569; font-size: 0.95rem; line-height: 1.5;">${promo.description}</p>
                        </div>
                        <div style="margin-top: 1rem; border-top: 1px dashed #fbcfe8; padding-top: 1rem;">
                            ${promo.discountPrice ? `
                            <div style="font-size: 1.35rem; font-weight: 800; color: #db2777; margin-bottom: 0.5rem;">
                                $${Number(promo.discountPrice).toFixed(2)} MXN
                            </div>` : ''}
                            <a href="/agendar?promoId=${promo.id}" class="studio-girl-pink-button studio-girl-pink-button-primary" style="width: 100%; text-align: center; display: block;">
                                Apartar Promo ($50 Anticipo) &rarr;
                            </a>
                        </div>
                    </div>
                `;
                container.prepend(card);
                showToast(`¡Nueva promoción disponible: <strong>${promo.title}</strong>!`, "🏷️");
            }
        }
    });

    connection.start()
        .then(() => console.log("SignalR: Conectado exitosamente al canal público del estudio."))
        .catch(err => console.warn("SignalR: Error al conectar:", err));
})();
