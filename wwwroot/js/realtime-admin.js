/**
 * Studio Girl Pink - Realtime Admin Module (SignalR)
 * Notificaciones en vivo de citas entrantes y cambios de estado
 */
(function () {
    if (typeof signalR === "undefined") {
        console.warn("SignalR library not found in Admin. Realtime disabled.");
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/studio")
        .withAutomaticReconnect([0, 1500, 3000, 5000, 10000, 20000])
        .build();

    // Reproducir un suave sonido de campanilla (Chime) usando Web Audio API nativo
    function playChime() {
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            const now = ctx.currentTime;
            
            // Primer tono (Fa# 5)
            const osc1 = ctx.createOscillator();
            const gain1 = ctx.createGain();
            osc1.type = "sine";
            osc1.frequency.setValueAtTime(740, now);
            gain1.gain.setValueAtTime(0.18, now);
            gain1.gain.exponentialRampToValueAtTime(0.001, now + 0.6);
            osc1.connect(gain1);
            gain1.connect(ctx.destination);
            osc1.start(now);
            osc1.stop(now + 0.6);

            // Segundo tono (La 5)
            const osc2 = ctx.createOscillator();
            const gain2 = ctx.createGain();
            osc2.type = "sine";
            osc2.frequency.setValueAtTime(880, now + 0.12);
            gain2.gain.setValueAtTime(0.22, now + 0.12);
            gain2.gain.exponentialRampToValueAtTime(0.001, now + 0.9);
            osc2.connect(gain2);
            gain2.connect(ctx.destination);
            osc2.start(now + 0.12);
            osc2.stop(now + 0.9);
        } catch (e) {
            console.log("Audio no permitido aún por interacción del usuario.", e);
        }
    }

    // Banner/Toast flotante destacado en el panel admin
    function showAdminToast(title, body, url = null) {
        let container = document.getElementById("adminRealtimeAlerts");
        if (!container) {
            container = document.createElement("div");
            container.id = "adminRealtimeAlerts";
            container.style.cssText = "position: fixed; top: 20px; right: 20px; z-index: 999999; display: flex; flex-direction: column; gap: 12px; max-width: 440px; width: calc(100% - 40px);";
            document.body.appendChild(container);
        }

        const alertBox = document.createElement("div");
        alertBox.style.cssText = "background: linear-gradient(135deg, #831843 0%, #db2777 100%); color: #ffffff; padding: 16px 20px; border-radius: 16px; box-shadow: 0 12px 32px rgba(219, 39, 119, 0.4); border: 2px solid #f472b6; animation: slideInRight 0.35s cubic-bezier(0.16, 1, 0.3, 1);";

        alertBox.innerHTML = `
            <div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 10px;">
                <div style="display: flex; gap: 10px; align-items: center;">
                    <span style="font-size: 1.8rem;">🌸</span>
                    <div>
                        <strong style="font-size: 1.05rem; display: block; letter-spacing: 0.3px;">${title}</strong>
                        <span style="font-size: 0.78rem; opacity: 0.85;">Notificación en vivo (SignalR)</span>
                    </div>
                </div>
                <button type="button" onclick="this.closest('div[style*=\\'background\\']').remove()" style="background: transparent; border: none; color: #fff; font-size: 1.3rem; cursor: pointer; line-height: 1;">&times;</button>
            </div>
            <div style="margin-top: 10px; font-size: 0.92rem; line-height: 1.45; background: rgba(0,0,0,0.15); padding: 10px 14px; border-radius: 10px;">
                ${body}
            </div>
            ${url ? `
            <div style="margin-top: 12px; text-align: right;">
                <a href="${url}" class="studio-girl-pink-button studio-girl-pink-button-primary" style="background: #fff; color: #831843; font-weight: bold; padding: 6px 14px; font-size: 0.82rem; text-decoration: none; border-radius: 8px;">
                    Gestionar Cita &rarr;
                </a>
            </div>` : ''}
        `;

        container.appendChild(alertBox);
        playChime();

        setTimeout(() => {
            alertBox.style.transition = "opacity 0.5s ease, transform 0.5s ease";
            alertBox.style.opacity = "0";
            alertBox.style.transform = "translateX(50px)";
            setTimeout(() => alertBox.remove(), 500);
        }, 12000);
    }

    // Incrementar contador de pendientes
    function incrementPendingCounter() {
        const counters = document.querySelectorAll(".stat-number, #pendingCountBadge, .admin-quick-actions a");
        counters.forEach(c => {
            const numMatch = c.innerText.match(/\d+/);
            if (numMatch) {
                const current = parseInt(numMatch[0]);
                c.innerText = c.innerText.replace(numMatch[0], current + 1);
            }
        });
    }

    // Escuchar nueva cita entrante
    connection.on("ReceiveNewAppointment", function (app) {
        console.log("Realtime: Nueva cita recibida", app);

        const bodyHtml = `
            <strong>${app.clientName}</strong> (${app.clientPhone})<br/>
            🌸 <strong>Servicio:</strong> ${app.serviceName}<br/>
            📅 <strong>Fecha:</strong> ${app.startsAtFormatted} (${app.duration})<br/>
            💵 <strong>Cotización:</strong> $${Number(app.quotedPrice).toFixed(2)} MXN &bull; <strong>Anticipo:</strong> $${Number(app.depositAmount).toFixed(2)} MXN
            ${app.notes ? `<br/><em>"${app.notes}"</em>` : ''}
        `;

        showAdminToast(`¡Nueva Cita #${app.id} Recibida!`, bodyHtml, `/admin/citas?statusFilter=PendienteConfirmacion`);
        incrementPendingCounter();

        // Si estamos en la página del Dashboard y la tabla de pendientes existe:
        const tableBody = document.querySelector("#pendingAppointmentsTable tbody, .admin-table-responsive tbody");
        if (tableBody) {
            // Ocultar mensaje de vacío si existiera
            const emptyState = document.querySelector(".admin-empty-state");
            if (emptyState) emptyState.style.display = "none";

            const newRow = document.createElement("tr");
            newRow.id = "appointment-row-" + app.id;
            newRow.style.cssText = "background: #fdf2f8; animation: pulseGlow 2s ease-in-out infinite alternate;";

            newRow.innerHTML = `
                <td>
                    <strong>#${app.id}</strong>
                    <span style="display: block; font-size: 0.7rem; color: #db2777; font-weight: bold;">⚡ EN VIVO</span>
                </td>
                <td>
                    <strong>${app.clientName}</strong><br/>
                    <small style="color: #64748b;">${app.clientPhone}</small>
                </td>
                <td>
                    ${app.serviceName}<br/>
                    <small style="color: #64748b;">$${Number(app.quotedPrice).toFixed(2)} MXN</small>
                </td>
                <td>
                    <strong>${app.startsAtFormatted}</strong><br/>
                    <small style="color: #64748b;">${app.duration}</small>
                </td>
                <td>
                    <span class="status-badge status-deposit-pending">
                        $${Number(app.depositAmount).toFixed(2)} (Revisar)
                    </span>
                    ${app.receiptPath ? `<br/><a href="${app.receiptPath}" target="_blank" style="font-size: 0.78rem; color: #db2777;">📄 Ver Comprobante</a>` : ''}
                </td>
                <td style="white-space: nowrap;">
                    <a href="/admin/citas?statusFilter=PendienteConfirmacion" class="studio-girl-pink-button studio-girl-pink-button-primary" style="padding: 4px 10px; font-size: 0.78rem;">
                        Validar &rarr;
                    </a>
                </td>
            `;

            tableBody.prepend(newRow);
        }
    });

    // Escuchar cambio de estado
    connection.on("ReceiveAppointmentStatusChange", function (data) {
        console.log("Realtime: Estatus de cita actualizado", data);
        const row = document.getElementById("appointment-row-" + data.appointmentId);
        if (row) {
            row.style.transition = "background-color 0.5s ease";
            if (data.newStatus === "Confirmada") {
                row.style.background = "#f0fdf4";
            } else if (data.newStatus === "Rechazada") {
                row.style.background = "#fef2f2";
            }
        }
    });

    connection.start()
        .then(() => {
            console.log("SignalR: Administrador conectado al canal en tiempo real.");
            connection.invoke("JoinAdminGroup").catch(e => console.warn(e));
        })
        .catch(err => console.warn("SignalR: Error al conectar en panel de administración:", err));
})();
