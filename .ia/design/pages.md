# Pantallas

Rutas en `frontend.md`. Composición visual en el lienzo (`README.md`).

## Acceso — `/ingresar`, `/registro`
- AuthLayout en diagonal.
- Control segmentado Iniciar sesión / Crear cuenta.
- Registro: nombre completo, correo, contraseña y campus. **No** hay elección de rol: el registro público siempre crea un alumno.
- Botón "Continuar con Google" debajo del formulario, separado por "o".
- "¿Olvidaste tu contraseña?" junto a la etiqueta de la contraseña.
- `/verificar-correo`: 6 casillas para el código y "Reenviar código" (con 60 s de espera).
- `/activar?token=`: el docente invitado establece su contraseña.

## Inicio del docente — `/inicio`
- Saludo con la fecha y las acciones "Unirme con código" (secondary) y "Nueva clase" (brand).
- **Esta semana**: las próximas entregas de todas sus clases, con la más próxima en la variante `ink`.
- **Mis clases**: control segmentado Activas / Archivadas y tarjetas de clase, más una tarjeta punteada "Crear una clase".

## Inicio del alumno — `/inicio`
- Saludo con un resumen ("Tienes 1 entrega en 2 días…") y un formulario de código de clase sobre un bloque `brand`.
- **Pendientes, por urgencia**: tareas ordenadas por vencimiento, con la urgente en `accent-soft`, y la última nota recibida al pie.
- **Panel oscuro del equipo**: actividad reciente, con "Entrar a la pizarra" (accent) y "Abrir chat".
- **Mis clases**: tarjetas compactas (iniciales en un cuadrado de color).

## Clase · Inicio — `/clases/$classId`
- ClassLayout con el header completo.
- Próximas tareas, equipos y chat (General / Mi equipo).
- Código copiable en el header (solo para el docente).

## Equipos — `/clases/$classId/equipos`
- Docente: columna "Sin equipo" (punteada) y grilla de equipos. Elige un equipo destino y toca alumnos para sumarlos (también puede arrastrarlos). Botones "Armar al azar" y "+ Nuevo equipo".
- Alumno: su equipo destacado y la lista del resto de los equipos (solo lectura).

## Tareas — `/clases/$classId/tareas`
- Docente: **una sección por tarea**: lista de tareas con barra de progreso → detalle con tiles (Entregadas, Por calificar, Sin entregar, Tarde), filtros, buscador y tabla de alumnos con la nota editable. "Descargar todo (.zip)" y "Editar consigna".
- Alumno: la lista de tareas de la clase con su estado.

## Tarea — `/clases/$classId/tareas/$assignmentId`
- Alumno: chips (vencimiento, modalidad y puntaje), consigna, material de apoyo y consultas. Lateral "Tu entrega": archivos, enlaces, zona para soltar archivos y "Entregar tarea" / "Anular entrega". Debajo, un comentario privado para el docente.
- Docente: la misma vista con la tabla de entregas (redirige a la sección de la tarea).

## Archivos — `/clases/$classId/archivos`
Lista de archivos con ícono de tipo, quién lo subió y cuándo. Vista previa para imágenes y PDF. Filtros por tipo. Zona para soltar archivos. *(Pantalla aún no diseñada en el lienzo: seguir los patrones de "Material de apoyo" y de la tabla de entregas.)*

## Pizarra — `/clases/$classId/equipos/$teamId/pizarra`
WhiteboardLayout. Excalidraw con las herramientas: seleccionar, lápiz, rectángulo, elipse, flecha, texto, nota adhesiva y borrador; colores y grosores abajo; zoom abajo a la derecha; cursores con nombre.

## Notificaciones, Perfil, Admin
*(Aún no diseñadas.)* Usar AppShell y los patrones de lista, panel y formulario existentes.

## Copy

- Español neutro con **tú**: "Inicia sesión", "Elige un equipo destino", "Arrastra archivos o elígelos".
- Si algún texto del lienzo contradice estas reglas, **mandan estas reglas**.
- Los mensajes de error siguen `errors.md` §UX.
