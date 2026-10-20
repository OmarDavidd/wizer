# Wizer — Riesgos

Escala: probabilidad e impacto **Alto / Medio / Bajo**. Revisión en cada planificación de sprint.

| ID | Riesgo | Prob. | Impacto | Mitigación |
|----|--------|:-----:|:-------:|------------|
| R-01 | **El plazo a diciembre es ajustado** para el alcance (tiempo real, pizarra, antivirus, Google) | Alta | Alto | Orden de recorte definido de antemano; congelamiento de features el 11 dic; demo al final de cada sprint |
| R-02 | **La sincronización de la pizarra** resulta más compleja de lo previsto (conflictos, rendimiento) | Media | Alto | Excalidraw ya trae el algoritmo de reconciliación; empezar con un spike de 2 días al inicio de S3; recortar los cursores si hace falta |
| R-03 | **No hay dominio**: Resend no envía correos a cualquier destinatario sin un dominio verificado, así que la verificación de correo no funciona en producción | Alta | Alto | Comprar un dominio antes del 30 nov; mientras tanto, Mailpit en desarrollo y SMTP de Gmail como plan B en producción |
| R-04 | **ClamAV consume unos 1,5 GB de RAM** y tarda en arrancar; puede no entrar en el Home Lab | Media | Medio | Medir en S0; si no entra, limitar la memoria y aceptar el escaneo más lento; los archivos esperan en `pending_scan` sin perderse |
| R-05 | **Una sola instancia sin Redis**: si se cae la API, se cortan el chat y la pizarra | Media | Medio | Reconexión automática y outbox en el cliente; snapshots de la pizarra cada 5 s; reinicio automático del contenedor |
| R-06 | **Disponibilidad del Home Lab** (cortes de luz o de internet) durante la demo | Media | Alto | Backups diarios; tener la demo grabada; poder levantar el stack completo en una laptop con `docker compose` |
| R-07 | **Integración entre módulos** de distintos devs (contratos, eventos) que se rompe al final | Media | Medio | Orval y el check de contrato en CI; interfaces `I<Module>ModuleService` acordadas al inicio de cada sprint; PRs chicos |
| R-08 | **Configuración de Google OAuth** (pantalla de consentimiento, orígenes permitidos) bloquea el login con Google | Baja | Bajo | Configurarlo temprano con el origen local; es candidato a recorte si se complica |
| R-09 | **Pruebas E2E inestables** con tiempo real (timing) | Media | Medio | Esperas por eventos y `data-testid`, nunca `sleep`; reintento solo en CI (1 vez) |
| R-10 | **Disponibilidad del equipo** en diciembre (exámenes, fiestas) | Media | Medio | El margen del 21 al 31 no tiene trabajo planificado; avisar las ausencias en la planificación |
