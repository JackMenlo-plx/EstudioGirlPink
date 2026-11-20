# studio girl pink — Solución Digital Integral

Plataforma Web desarrollada en **ASP.NET Core (.NET 10)**, **Entity Framework Core**, **PostgreSQL** y **ASP.NET Core Identity**.

Diseñada para resolver la duplicación de citas, control de inventario, registro de anticipos obligatorios, diagnóstico capilar y fidelización de clientas para el estudio de belleza **studio girl pink**.

---

## 🌸 1. Reglas Operativas Implementadas

* **Marca y Diseño:** Identificador unificado a `studio girl pink`.
* **Horarios de Atención:**
  * Lunes a Viernes: 10:00 am a 6:00 pm.
  * Sábados: 9:00 am a 3:00 pm.
  * Domingos: Cerrado para agenda pública. La administración cuenta con permiso para agendar manualmente hasta las 9:00 pm.
* **Margen entre citas:** 1 hora y media (90 minutos) entre clientes garantizado por el motor de agenda para evitar traslapes y asegurar máxima higiene.
* **Anticipos y Políticas:**
  * Anticipo fijo obligatorio de **$50.00 MXN** (transferencia o efectivo con carga de comprobante en web).
  * Cancelaciones válidas entre 1 día (24 hrs) y 2 horas antes de la cita. Con menos de 2 horas o inasistencia, el anticipo NO es reembolsable.
  * Validación y aprobación manual de cada cita por parte de la administración.
* **Tarjeta de Lealtad (Fidelización):**
  * Cada 5 servicios completados, el 6to servicio incluye **Gel Semipermanente GRATIS** o **50% de Descuento** en Uñas Acrílicas o Laminado de Ceja.

---

## 💅 2. Catálogo y Cotizador Dinámico de Precios

* **Uñas Acrílicas:** Base $200 (Nivel 1) + $50 por cada nivel adicional (Niveles del 1 al 10). Duración: 60 - 120 min.
* **Baño de Acrílico:** $200 MXN (60 min).
* **Gel Semipermanente:** $150 MXN (60 min).
* **Ruber (Nivelación):** $220 MXN (60 min).
* **Acripie:** $250 MXN (60 min).
* **Laminado de Ceja:** $200 MXN (30 min).
* **Alisado Progresivo:** Según largo:
  * Al hombro: $500 MXN
  * Cabello medio (hombro a media espalda): $900 MXN
  * A la cintura: $1,350 MXN
  * Extra-largo: $1,500 MXN
  * Adicional por volumen abundante: +$200 MXN
  * Duración: 2 a 4 hrs.
* **Efectos de Color con Extensión:** Desde $450 MXN.
* **Servicios Especiales / Personalizados:** Carga de fotografía de diseño para cotización y aprobación del Admin.

---

## 📦 3. Módulos y Arquitectura

1. **Público:**
   * **Landing Page:** Información, políticas de anticipo y cancelación, catálogo.
   * **Cotizador Dinámico en Vivo:** Cálculo interactivo de uñas y alisados en tiempo real.
   * **Color Studio (Manicura Virtual):** Preview interactivo de colores de esmalte sobre ilustración de mano vectorial.
   * **Reserva de Citas en Línea:** Formulario con selección de fecha/hora, carga de comprobante de $50 y ficha de diagnóstico capilar opcional.
   * **Consulta de Estatus de Citas:** Búsqueda rápida por número de teléfono.

2. **Panel de Administración (`/admin` - Protegido):**
   * **Dashboard de KPIs:** Citas pendientes de aprobación, citas de hoy, total de clientas, ingresos, gastos y balance neto.
   * **Gestión de Citas:** Lista con filtros, visualización de comprobante de anticipo y foto de referencia, botones para Aprobar, Rechazar o Completar Cita.
   * **Clientas y Ficha Capilar:** Historial de visitas, total invertido, estatus de tarjeta de lealtad y expediente capilar detallado.
   * **Inventario:** Control de acrílicos, geles, monómero, limas, tips, químicos y guantes, con semáforo de alerta de stock mínimo y registro de merma/deterioro.
   * **Finanzas:** Registro de ingresos por servicio y gastos operacionales (renta, luz, agua, insumos).

---

## 🚀 4. Guía de Ejecución Local

### Paso 1: Configurar Base de Datos en PostgreSQL
Asegúrate de tener PostgreSQL ejecutándose y crea la base de datos `studio_girl_pink`.
Verifica tu cadena de conexión en `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=studio_girl_pink;Username=postgres;Password=TU_PASSWORD_LOCAL"
  }
}
```

### Paso 2: Configurar Secretos del Administrador
En PowerShell, define las credenciales del usuario administrador inicial:
```powershell
$env:STUDIO_GIRL_PINK_ADMIN_EMAIL = "admin@studiogirlpink.com"
$env:STUDIO_GIRL_PINK_ADMIN_PASSWORD = "StudioAdmin2026!#"
```

### Paso 3: Aplicar Migraciones
```powershell
dotnet ef database update
```

### Paso 4: Iniciar la Aplicación
```powershell
dotnet run
```
Abre en tu navegador `https://localhost:7083` (o el puerto indicado en la consola).

* **Portal Público:** `https://localhost:7083/`
* **Agendar Cita:** `https://localhost:7083/agendar`
* **Panel de Administración:** `https://localhost:7083/admin`
  * Inicia sesión con: `admin@studiogirlpink.com` y la contraseña que configuraste en `$env:STUDIO_GIRL_PINK_ADMIN_PASSWORD`.
