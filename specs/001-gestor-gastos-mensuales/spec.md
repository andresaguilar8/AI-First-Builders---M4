# Feature Specification: Gestor de gastos mensuales

**Feature Branch**: `001-gestor-gastos-mensuales`

**Created**: 2026-10-12

**Status**: Draft

**Input**: User description: "Generá el spec a partir del PRD.MD"

**Fuente**: [PRD.md](../../PRD.md) (PRD-001). Cada requisito y escenario de este spec cita entre
paréntesis los códigos RF/RNF/AC del PRD que lo respaldan; ante cualquier diferencia, prevalece el
PRD.

## Clarifications

### Session 2026-10-13

- Q: Al desmarcar el pago de un recurrente en un mes donde ese pago era la única información propia, ¿qué pasa con su registro mensual? → A: Se elimina el registro; el mes vuelve a 0 registros y se muestra con los valores vigentes.
- Q: Al editar nombre o descripción de un recurrente con "Este mes y los siguientes", ¿los meses anteriores conservan los valores viejos? → A: Sí; nombre y descripción no son retroactivos, igual que monto, vencimiento y categoría.
- Q: ¿Qué debe hacer el sistema ante varios intentos fallidos seguidos de inicio de sesión? → A: Nada; no hay límite de intentos (fuera de alcance del PRD, riesgo aceptado).
- Q: ¿Cuál es el monto máximo que debe aceptar un gasto? → A: $999.999.999.999,99.
- Q: Al registrar la fecha real de pago de un gasto vencido, ¿hay una fecha mínima permitida? → A: No; se acepta cualquier fecha hasta hoy inclusive.
- Q: ¿Qué pasa con un registro mensual de un recurrente cuando una operación lo deja sin información propia? → A: Se elimina; un registro se conserva solo mientras tenga un pago o algún campo distinto del valor vigente (regla general que incluye desmarcar y "Este mes y los siguientes").
- Q: Al dar de baja un recurrente o pasarlo a puntual, ¿qué pasa con los registros guardados de meses posteriores? → A: Se eliminan.
- Q: ¿Qué gastos disparan la alerta de "vence hoy"? → A: Todo pendiente de cualquier mes cuyo vencimiento es hoy, incluidos los recurrentes solo proyectados del mes actual.
- Q: Si se edita un gasto que ya está pagado, ¿qué pasa con su estado de pago? → A: Se conservan el estado y la fecha de pago, sin importar qué campo se cambie.
- Q: En "Este mes y los siguientes", ¿qué se sobrescribe en meses futuros con cambios propios? → A: Solo los campos cuyo valor cambió en esa edición; los demás cambios propios se conservan.
- Q: ¿La regla de vencimiento no anterior al mes del gasto aplica también al editar? → A: Sí; al crear y al editar, con cualquier alcance.
- Q: Al pasar a puntual un recurrente con meses anteriores, ¿cómo quedan esos meses y qué es el gasto del mes visualizado? → A: El recurrente termina en el mes anterior (sus meses previos siguen siendo suyos) y en el mes visualizado queda un puntual nuevo e independiente con los valores y el pago de ese mes.
- Q: ¿Las suposiciones del spec que no están en el PRD pasan al PRD como requisitos explícitos? → A: Sí; se incorporan al texto de los RF correspondientes, sin AC nuevos.

### Session 2026-10-14

- Q: ¿Se puede eliminar un gasto puntual pagado? → A: Sí, sin restricción.
- Q: Al editar un gasto, ¿se puede quitar el vencimiento? → A: Sí; en un recurrente con "Este mes y los siguientes", los meses siguientes se proyectan sin vencimiento.
- Q: ¿Se puede reactivar un recurrente dado de baja? → A: No; queda fuera de alcance.
- Q: ¿Se ignoran los espacios repetidos al comparar nombres de categoría? → A: Sí, se colapsan.

## User Scenarios & Testing *(mandatory)*

Actor único: **Andrés**, que administra sus gastos personales mes a mes. Necesita registrar sus
obligaciones rápido, ver qué está pago y qué no, anticipar vencimientos y saber en qué categorías
concentra sus gastos.

### User Story 1 - Acceder de forma segura a la información (Priority: P1)

Andrés inicia sesión con su email y contraseña y, desde ese momento, puede ver y modificar sus
gastos y categorías. Nadie sin sesión válida puede ver ni modificar esos datos, y no existe forma
de crear otras cuentas.

**Why this priority**: es condición para cualquier otra funcionalidad: los datos son personales y
no deben quedar expuestos.

**Independent Test**: con el usuario inicial cargado, se prueba iniciar sesión con credenciales
correctas e incorrectas y acceder sin sesión; entrega un acceso protegido funcional.

**Acceptance Scenarios**:

1. **Dado** el usuario inicial, **Cuando** inicia sesión con su email y contraseña correctos,
   **Entonces** accede a la vista de gastos del mes actual (AC-52).
2. **Dado** el usuario inicial, **Cuando** intenta iniciar sesión con una contraseña incorrecta,
   **Entonces** se rechaza el acceso y no se muestra ningún dato de gastos ni categorías (AC-53).
3. **Dado** un usuario no autenticado, **Cuando** intenta ver gastos o categorías, **Entonces** no
   se muestran los datos y se solicita autenticación (AC-54).
4. **Dado** que no hay una sesión válida, **Cuando** se consultan o modifican datos accediendo
   directamente al servicio, **Entonces** la solicitud se rechaza como no autenticada y la
   respuesta no contiene ningún dato de gastos, categorías ni del usuario (AC-55).
5. **Dada** una sesión sin actividad durante 30 días, **Cuando** el usuario intenta ver sus gastos,
   **Entonces** no se muestran los datos y se le vuelve a pedir email y contraseña (AC-56).
6. **Dado** que existe el usuario inicial, **Cuando** alguien intenta crear otra cuenta por
   cualquier medio, **Entonces** no hay opción de registro, la solicitud se rechaza y sigue
   existiendo exactamente 1 usuario (AC-57).

---

### User Story 2 - Registrar gastos del mes y navegar entre períodos (Priority: P1)

Andrés abre la aplicación, ve el mes actual y carga sus gastos indicando nombre y monto y,
opcionalmente, descripción, categoría, fecha de vencimiento y si es recurrente o puntual. Puede
moverse a meses anteriores o futuros y cargar gastos en el mes que esté viendo.

**Why this priority**: reemplaza la planilla; sin alta y consulta mensual no hay producto.

**Independent Test**: se crean gastos puntuales en distintos meses y se navega entre ellos;
entrega el registro mensual básico.

**Acceptance Scenarios**:

1. **Dado** que el usuario ve octubre de 2026, **Cuando** crea "Alquiler" por $300.000 como
   recurrente, **Entonces** aparece en octubre de 2026 identificado como recurrente (AC-01).
2. **Dado** que ve octubre de 2026, **Cuando** crea el puntual "Regalo", $15.000, descripción
   "Cumpleaños", categoría "Varios" y vencimiento 20/10/2026, **Entonces** aparece en octubre como
   puntual con exactamente esos valores (AC-02).
3. **Dado** el formulario de alta, **Cuando** confirma un gasto sin nombre o sin monto,
   **Entonces** se rechaza y no se crea ningún gasto (AC-03).
4. **Dado** que hoy es 05/10/2026 y ve diciembre de 2026, **Cuando** crea un gasto puntual,
   **Entonces** aparece en diciembre y no en octubre (AC-04).
5. **Dado** el formulario de alta, **Cuando** ingresa como monto 0, -100 o 10,999, **Entonces** se
   rechaza el alta (AC-05).
6. **Dado** el formulario de alta, **Cuando** ingresa 1.234,56, **Entonces** el gasto se registra
   por $1.234,56 (AC-06).
7. **Dado** que ve octubre de 2026, **Cuando** crea un gasto con vencimiento 05/11/2026,
   **Entonces** aparece en octubre con ese vencimiento (AC-07).
8. **Dado** que ve octubre de 2026, **Cuando** intenta crear un gasto con vencimiento 30/09/2026,
   **Entonces** se rechaza el alta (AC-08).
9. **Dado** un gasto puntual de un mes, **Cuando** consulta el mes siguiente, **Entonces** ese gasto
   no aparece (AC-13).
10. **Dado** que ingresa sin haber navegado antes, **Cuando** se carga la vista principal,
    **Entonces** ve los gastos del mes y año actuales (AC-49).
11. **Dado** que ve septiembre de 2026, **Cuando** va al mes anterior, **Entonces** ve agosto de
    2026 (AC-50).
12. **Dado** que ve enero de 2027, **Cuando** va al mes anterior, **Entonces** ve diciembre de 2026
    (AC-51).
13. **Dado** el formulario de alta, **Cuando** ingresa 999.999.999.999,99, **Entonces** el gasto se
    registra por ese monto; **Cuando** ingresa 1.000.000.000.000, **Entonces** se rechaza (AC-62).

---

### User Story 3 - Gastos recurrentes que se proyectan y se editan sin reescribir el pasado (Priority: P1)

Andrés carga una sola vez sus gastos habituales (alquiler, cuotas, servicios) y estos aparecen
solos en los meses siguientes. Cuando un importe cambia, elige si el cambio aplica "Solo este mes"
o "Este mes y los siguientes", y los meses anteriores quedan como estaban.

**Why this priority**: es el problema central del PRD: hoy debe trasladar a mano cada aumento a
los meses siguientes.

**Independent Test**: se crea un recurrente, se consultan meses futuros y se editan con ambos
alcances verificando meses anteriores y posteriores.

**Acceptance Scenarios**:

1. **Dado** un recurrente activo, **Cuando** consulta el mes siguiente, **Entonces** el gasto
   aparece aunque no se haya cargado manualmente en ese mes (AC-09).
2. **Dado** un recurrente creado en enero de 2027 con vencimiento 30/01/2027, **Cuando** consulta
   febrero y marzo, **Entonces** vence el 28/02/2027 y el 30/03/2027 (AC-10).
3. **Dado** un recurrente creado en octubre de 2026 con vencimiento 05/11/2026, **Cuando** consulta
   noviembre y diciembre, **Entonces** vence el 05/11/2026 y el 05/12/2026 (AC-11).
4. **Dado** un recurrente que vence el día 10, **Cuando**, viendo septiembre de 2026, cambia el
   vencimiento a 15/09/2026 con "Este mes y los siguientes", **Entonces** octubre vence el
   15/10/2026 y agosto sigue venciendo el 10/08/2026 (AC-12).
5. **Dado** un recurrente de $30.000, **Cuando**, viendo septiembre, lo cambia a $35.000 con "Solo
   este mes", **Entonces** septiembre muestra $35.000 y octubre $30.000 (AC-15).
6. **Dado** un recurrente de $25.000 hasta agosto, **Cuando**, viendo septiembre, lo cambia a
   $30.000 con "Este mes y los siguientes", **Entonces** agosto sigue en $25.000 y septiembre y
   posteriores muestran $30.000 (AC-16).
7. **Dado** un recurrente de $30.000 cuyo noviembre se cambió a $32.000 con "Solo este mes" y está
   pagado, **Cuando**, viendo septiembre, lo cambia a $35.000 con "Este mes y los siguientes",
   **Entonces** noviembre muestra $35.000 y sigue pagado (AC-17).
8. **Dado** un recurrente de $20.000 que vence el 15/09/2026, **Cuando**, viendo septiembre, lo
   marca pagado el 10/09/2026, **Entonces** septiembre queda pagado con esa fecha y octubre
   pendiente por $20.000 (AC-24).
9. **Dado** un recurrente creado en octubre de 2026 sin modificaciones ni pagos, **Cuando** consulta
   diciembre de 2026, **Entonces** el gasto se ve en diciembre y no existe ningún registro mensual
   guardado de ese gasto para noviembre ni diciembre (AC-60).
10. **Dado** un recurrente sin registro mensual propio en septiembre de 2026, **Cuando**, viendo
    septiembre, lo marca pagado y luego cambia su monto con "Solo este mes", **Entonces** existe
    exactamente 1 registro mensual de ese gasto para septiembre (AC-61).
11. **Dado** un recurrente "Internet" ("Plan 100 MB") activo desde agosto de 2026, **Cuando**,
    viendo septiembre, cambia nombre a "Fibra" y descripción a "Plan 300 MB" con "Este mes y los
    siguientes", **Entonces** agosto sigue mostrando "Internet" y "Plan 100 MB", y septiembre y
    octubre muestran "Fibra" y "Plan 300 MB" (AC-63).
12. **Dado** un recurrente de $30.000 cuyo noviembre se cambió a $32.000 con "Solo este mes" y no
    está pagado, **Cuando**, viendo septiembre, lo cambia a $32.000 con "Este mes y los
    siguientes", **Entonces** noviembre muestra $32.000 y no existe ningún registro mensual de ese
    gasto para noviembre (AC-64).
13. **Dado** un recurrente sin registro mensual propio en septiembre de 2026, **Cuando**, viendo
    septiembre, lo marca pagado y luego lo desmarca, **Entonces** queda pendiente y no existe
    ningún registro mensual de ese gasto para septiembre (AC-68).

---

### User Story 4 - Pagar a tiempo: estados, marcado de pago y alerta del día (Priority: P2)

Andrés ve en cada gasto pendiente si está al día, próximo a vencer, vence hoy o vencido. Al abrir
la aplicación recibe una alerta si algo vence hoy. Marca gastos como pagados (con la fecha de hoy
o, si estaba vencido, con la fecha real) y puede desmarcarlos.

**Why this priority**: materializa el objetivo medible del PRD (0 gastos pagados tarde), pero
requiere que antes existan gastos registrados.

**Independent Test**: con la fecha actual fijada, se crean gastos con distintos vencimientos y se
verifican estados, alerta y marcado/desmarcado de pago.

**Acceptance Scenarios**:

1. **Dado** que hoy es 05/10/2026 y un pendiente vence el 10/10/2026, **Cuando** lo marca pagado,
   **Entonces** no se pide fecha y queda pagado con fecha 05/10/2026 sin recargar la página
   (AC-35).
2. **Dado** que hoy es 05/10/2026 y un pendiente venció el 01/10/2026, **Cuando** lo marca pagado,
   indica que no pagó hoy e ingresa 03/10/2026, **Entonces** queda pagado con fecha 03/10/2026
   (AC-36).
3. **Dado** el mismo gasto vencido, **Cuando** confirma que pagó hoy, **Entonces** queda pagado
   con fecha 05/10/2026 (AC-37).
4. **Dado** el mismo gasto vencido, **Cuando** ingresa como fecha de pago 06/10/2026,
   **Entonces** se rechaza la fecha y el gasto sigue pendiente (AC-38).
5. **Dado** un gasto pagado el 05/10/2026, **Cuando** lo desmarca, **Entonces** queda pendiente y
   sin fecha de pago (AC-39).
6. **Dado** un pendiente que vence hoy, **Cuando** inicia sesión, **Entonces** ve una alerta de que
   tiene al menos un gasto que vence hoy (AC-40).
7. **Dada** una sesión vigente y un pendiente que vence hoy, **Cuando** abre la aplicación sin
   iniciar sesión, **Entonces** ve la misma alerta (AC-41).
8. **Dado** que el único gasto que vence hoy ya está pagado, **Cuando** abre la aplicación,
   **Entonces** no se muestra alerta (AC-42).
9. **Dado** que hoy es 05/10/2026, **Cuando** ve el mes, **Entonces** un pendiente que vence el
   09/10 está "Al día" (AC-43); uno sin vencimiento está "Al día" (AC-44); los que vencen el 08/10
   y el 06/10 están "Próximo a vencer" (AC-45); uno que vence el 05/10 está "Vence hoy" (AC-46);
   uno que venció el 04/10 está "Vencido" (AC-47).
10. **Dado** que son las 22:00 del 05/10/2026 en Buenos Aires (01:00 del 06/10 en UTC) y un
    pendiente vence el 05/10/2026, **Cuando** ve el mes, **Entonces** figura "Vence hoy" (AC-48).
11. **Dado** que hoy es 05/10/2026 y un pendiente de octubre venció el 01/10/2026, **Cuando** lo
    marca pagado, indica que no pagó hoy e ingresa 15/08/2026, **Entonces** queda pagado con fecha
    15/08/2026 (AC-66).
12. **Dado** que hoy es 05/10/2026 y un pendiente de septiembre de 2026 vence el 05/10/2026,
    **Cuando** abre la aplicación, **Entonces** ve la alerta de que tiene al menos un gasto que
    vence hoy (AC-67).

---

### User Story 5 - Corregir el plan: baja, eliminación y cambio de tipo (Priority: P2)

Andrés deja de tener un gasto habitual y lo da de baja, sin perder lo registrado en meses
anteriores. Elimina un gasto puntual cargado por error o convierte un gasto entre puntual y
recurrente.

**Why this priority**: mantiene la información fiel en el tiempo; sin esto la lista se ensucia,
pero el registro básico ya aporta valor.

**Independent Test**: sobre gastos ya creados se ejecutan baja, eliminación y cambios de tipo
verificando meses anteriores, actual y posteriores.

**Acceptance Scenarios**:

1. **Dado** un gasto puntual existente, **Cuando** edita nombre, descripción, monto, categoría y
   vencimiento, **Entonces** el gasto refleja los nuevos valores (AC-14).
2. **Dado** un puntual de octubre, **Cuando**, viendo octubre, lo cambia a recurrente,
   **Entonces** aparece en octubre y en noviembre (AC-18).
3. **Dado** un recurrente activo desde agosto de 2026, **Cuando**, viendo octubre, lo cambia a
   puntual, **Entonces** sigue en agosto y septiembre, aparece en octubre y no en noviembre
   (AC-19).
4. **Dado** un recurrente pagado en noviembre de 2026, **Cuando**, viendo octubre, lo cambia a
   puntual, **Entonces** no aparece en noviembre y el total de noviembre no incluye su monto
   (AC-20).
5. **Dado** un recurrente con registros de enero a agosto de 2026, **Cuando**, viendo septiembre,
   lo da de baja, **Entonces** sigue apareciendo de enero a agosto y no aparece desde septiembre
   (AC-21).
6. **Dado** un recurrente pagado en septiembre de 2026, **Cuando**, viendo septiembre, lo da de
   baja, **Entonces** no aparece en septiembre y el total del mes no incluye su monto (AC-22).
7. **Dado** un puntual en octubre, **Cuando** lo elimina, **Entonces** no aparece en octubre
   (AC-23).
8. **Dado** un recurrente pagado en noviembre de 2026, **Cuando**, viendo octubre, lo da de baja,
   **Entonces** no existe ningún registro mensual de ese gasto para octubre ni noviembre (AC-65).

---

### User Story 6 - Categorías y resumen del mes (Priority: P3)

Andrés organiza sus gastos en categorías propias y consulta, para cada mes, cuánto gasta por
categoría, el total del mes y cuánto le queda por pagar.

**Why this priority**: responde a "dónde estoy gastando más", objetivo secundario del PRD.

**Independent Test**: se crean categorías, se asocian a gastos de un mes y se verifica el resumen
y los totales.

**Acceptance Scenarios**:

1. **Dado** el nombre de una categoría nueva, **Cuando** confirma su creación, **Entonces** queda
   disponible para asociarla a gastos (AC-25).
2. **Dada** la categoría "Educación", **Cuando** intenta crear o renombrar otra como "Educación",
   "educacion" o "EDUCACIÓN", **Entonces** se rechaza y sigue existiendo una sola (AC-26).
3. **Dado** el formulario de categorías, **Cuando** intenta crear o renombrar una categoría como
   "Sin categoría" o "sin categoria", **Entonces** se rechaza (AC-27).
4. **Dada** "Servicios" asociada a gastos de agosto y octubre de 2026, **Cuando** la renombra a
   "Servicios del hogar", **Entonces** ambos meses muestran el nuevo nombre (AC-28).
5. **Dada** una categoría sin gastos, **Cuando** la elimina, **Entonces** deja de estar disponible
   (AC-29).
6. **Dada** "Ocio" asociada a gastos de agosto y octubre de 2026, **Cuando** la elimina,
   **Entonces** esos gastos se muestran como "Sin categoría" en ambos meses (AC-30).
7. **Dado** un gasto sin categoría, **Cuando** le asocia una categoría existente, **Entonces** se
   muestra con esa categoría (AC-31).
8. **Dados** en septiembre "Vivienda" por $200.000 pagados y $100.000 pendientes y "Servicios" por
   $100.000, **Cuando** consulta el resumen, **Entonces** ve $300.000 en Vivienda y $100.000 en
   Servicios (AC-32).
9. **Dado** un gasto de $50.000 sin categoría en septiembre, **Cuando** consulta el resumen,
   **Entonces** ve $50.000 en "Sin categoría" (AC-33).
10. **Dados** en octubre pagados por $300.000 y pendientes por $100.000, **Cuando** consulta
    octubre, **Entonces** ve total del mes $400.000 y total pendiente $100.000 (AC-34).

---

### User Story 7 - Exportar el historial a CSV (Priority: P4)

Andrés descarga un archivo CSV con todo su historial de gastos, mes por mes, hasta el mes actual,
para analizarlo o respaldarlo fuera de la aplicación.

**Why this priority**: útil pero no imprescindible para la operación mensual.

**Independent Test**: con gastos puntuales y recurrentes en varios meses, se exporta y se valida el
contenido y formato del archivo.

**Acceptance Scenarios**:

1. **Dado** que hoy es 05/10/2026, existe un puntual en agosto de 2026 y un recurrente activo
   desde agosto sin registros propios, **Cuando** exporta, **Entonces** el CSV tiene 4 filas de
   datos: el puntual en `2026-08` y el recurrente en `2026-08`, `2026-09` y `2026-10`, y ninguna de
   `2026-11` o posterior (AC-58).
2. **Dado** un pendiente "Luz, gas" de $1.234,56 sin categoría que vence el 10/10/2026,
   **Cuando** exporta, **Entonces** el archivo está en UTF-8 con BOM, su encabezado es
   `periodo,nombre,monto,categoria,vencimiento,estado,fecha_pago` y contiene la fila
   `2026-10,"Luz, gas",1234.56,Sin categoría,2026-10-10,Pendiente,` (AC-59).

---

### Edge Cases

- Vencimiento el día 29, 30 o 31 en un mes que no tiene ese día: se usa el último día del mes
  (RF-07, AC-10).
- Recurrente cuyo vencimiento cae en el mes siguiente al de alta: en los meses proyectados el
  vencimiento cae dentro del propio mes (AC-11).
- Edición "Este mes y los siguientes" cuando hay meses futuros con cambios propios o pagos: se
  sobrescriben solo los campos cuyo valor cambió en esa edición (p. ej., si se cambia solo el
  monto, una categoría propia de noviembre se conserva) y se conserva el estado de pago (RF-11,
  AC-17).
- Baja o paso a puntual de un recurrente ya pagado en el mes visualizado o en meses posteriores:
  esos meses dejan de mostrarlo, no suman a sus totales y sus registros mensuales guardados se
  eliminan (RF-13, RF-14, AC-20, AC-22, AC-65).
- Edición "Este mes y los siguientes" que deja un registro mensual futuro sin pago e igual a los
  valores vigentes: el registro se elimina (RNF-06, AC-64).
- Nombres de categoría que difieren solo en mayúsculas, acentos o espacios repetidos, o que
  coinciden con "Sin categoría": se rechazan (RF-19).
- Fecha de pago posterior a hoy: se rechaza (RF-30).
- Edición de un gasto pagado (monto, vencimiento u otro campo): sigue pagado con la misma fecha de
  pago (RF-09).
- Desmarcar el pago de un recurrente cuyo único dato propio en el mes era ese pago: se elimina el
  registro mensual y el mes queda con 0 registros de ese gasto (RF-31, RNF-05).
- Diferencia horaria: entre las 21:00 y las 23:59 de Buenos Aires, la fecha UTC ya es el día
  siguiente; manda la fecha de Buenos Aires (RNF-07, AC-48).
- Monto de 999.999.999.999,99: se acepta; 1.000.000.000.000 o más: se rechaza (FR-003).
- Navegación entre años (enero ↔ diciembre) (AC-51).
- Operar sobre un recurrente en un mes en que no aparece (antes de su inicio o después de su
  baja): no es posible; solo se opera sobre los gastos visibles en el mes visualizado (FR-009).
- Eliminar un puntual pagado: se permite y es definitivo (FR-016).
- Quitar el vencimiento al editar: se permite; el gasto pasa a "Al día" (FR-009, FR-033).
- Mes sin gastos: se muestra vacío con totales en $0.
- Textos con comas o comillas en el CSV: se encierran entre comillas dobles (RF-39, AC-59).
- Sesión inactiva por 30 días en un dispositivo compartido: se vuelve a pedir credenciales
  (RNF-08); al no existir cierre de sesión, el riesgo se acepta.
- Intentos fallidos repetidos de inicio de sesión: no se limitan ni bloquean; cada intento se
  rechaza individualmente (riesgo de fuerza bruta aceptado, fuera de alcance del PRD).

## Requirements *(mandatory)*

### Functional Requirements

**Alta de gastos**

- **FR-001** (RF-01): El sistema DEBE permitir crear un gasto con nombre y monto obligatorios, y
  descripción, categoría y fecha de vencimiento opcionales. Nombre hasta 100 caracteres y
  descripción hasta 500, sin espacios al inicio ni al final; un nombre vacío tras recortarlo se
  considera no indicado.
- **FR-002** (RF-02): El sistema DEBE asignar cada gasto nuevo al mes que el usuario está viendo.
- **FR-003** (RF-03): El sistema DEBE aceptar solo montos mayores a 0 y menores o iguales a
  $999.999.999.999,99, con hasta 2 decimales, en pesos argentinos, ingresados con coma decimal y
  punto de miles opcional, tanto al crear como al editar.
- **FR-004** (RF-04): El usuario DEBE poder indicar al crear un gasto si es recurrente o puntual.
- **FR-005** (RF-05): El sistema DEBE aceptar vencimientos en el mes del gasto o en meses
  posteriores y rechazar vencimientos en meses anteriores, tanto al crear como al editar (en un
  recurrente, con cualquier alcance, tomando como mes del gasto el mes visualizado).

**Recurrentes y puntuales**

- **FR-006** (RF-06): Un gasto recurrente DEBE aparecer automáticamente en los meses siguientes
  mientras esté activo.
- **FR-007** (RF-07): En cada mes proyectado, el vencimiento de un recurrente DEBE caer en ese mes,
  el mismo día que su vencimiento vigente, o el último día del mes si ese día no existe. Un
  recurrente sin vencimiento se proyecta sin vencimiento (su estado es "Al día", FR-033).
- **FR-008** (RF-08): Un gasto puntual DEBE pertenecer solo a su mes y NO DEBE repetirse en
  períodos posteriores.

**Edición, baja y eliminación**

- **FR-009** (RF-09): El sistema DEBE permitir editar nombre, descripción, monto, categoría y
  vencimiento de un gasto. Editar un gasto pagado DEBE conservar su estado y su fecha de pago,
  cualquiera sea el campo modificado. Al editar se PUEDE quitar el vencimiento; en un recurrente
  con "Este mes y los siguientes", los meses siguientes se proyectan sin vencimiento. Solo se
  pueden editar, pagar, dar de baja, convertir o eliminar gastos que aparecen en el mes visualizado.
- **FR-010** (RF-10): Al editar un recurrente, el usuario DEBE elegir entre "Solo este mes" y "Este
  mes y los siguientes".
- **FR-011** (RF-11): "Este mes y los siguientes" DEBE sobrescribir también en meses futuros con
  cambios propios solo los campos cuyo valor cambió en esa edición; los demás cambios propios de
  esos meses y su estado de pago DEBEN conservarse.
- **FR-012** (RF-12): El sistema DEBE permitir pasar un puntual a recurrente, proyectándolo desde
  el mes visualizado y conservando nombre, descripción, monto, categoría y vencimiento como
  valores vigentes.
- **FR-013** (RF-13): El sistema DEBE permitir pasar un recurrente a puntual: el recurrente termina
  en el mes anterior al visualizado (sus meses anteriores se conservan como parte de él) y en el
  mes visualizado queda un gasto puntual nuevo e independiente, con los valores y el estado de
  pago que el gasto tenía en ese mes. El recurrente deja de mostrarse en los meses posteriores
  aunque estuvieran pagados; los registros mensuales guardados de esos meses posteriores DEBEN
  eliminarse.
- **FR-014** (RF-14): El sistema DEBE permitir dar de baja un recurrente; desde el mes visualizado
  en adelante NO DEBE mostrarse, aunque estuviera pagado, y sus registros mensuales guardados de
  esos meses DEBEN eliminarse.
- **FR-015** (RF-15): Al dar de baja un recurrente, el sistema DEBE conservar sus registros de los
  meses anteriores al visualizado.
- **FR-016** (RF-16): El sistema DEBE permitir eliminar un gasto puntual, esté pendiente o pagado;
  la eliminación es definitiva. Un recurrente NO se elimina: se da de baja (FR-014).
- **FR-017** (RF-17): Ninguna modificación de un recurrente hecha en un mes DEBE alterar nombre,
  descripción, monto, vencimiento, categoría ni estado de pago de meses anteriores (la única
  excepción es renombrar o eliminar la categoría en sí, FR-020/FR-021).

**Categorías**

- **FR-018** (RF-18): El sistema DEBE permitir crear categorías, con nombre obligatorio de hasta 50
  caracteres, sin espacios al inicio ni al final.
- **FR-019** (RF-19): El sistema DEBE rechazar crear o renombrar una categoría con un nombre ya
  existente o igual a "Sin categoría", sin distinguir mayúsculas, minúsculas ni acentos, e
  ignorando espacios al inicio, al final y repetidos dentro del nombre.
- **FR-020** (RF-20): El sistema DEBE permitir renombrar una categoría; el nuevo nombre DEBE verse
  en todos sus gastos, en todos los meses, incluidos los anteriores.
- **FR-021** (RF-21): El sistema DEBE permitir eliminar una categoría; sus gastos DEBEN pasar a
  "Sin categoría" en todos los meses, incluidos los anteriores.
- **FR-022** (RF-22): El usuario DEBE poder asociar opcionalmente un gasto a una categoría.

**Resumen**

- **FR-023** (RF-23): El sistema DEBE mostrar, por mes, el total por categoría sumando pagados y
  pendientes.
- **FR-024** (RF-24): Los gastos sin categoría DEBEN agruparse en "Sin categoría".
- **FR-025** (RF-25): El sistema DEBE mostrar el total de gastos del mes visualizado.
- **FR-026** (RF-26): El sistema DEBE mostrar el total pendiente de pago del mes visualizado.

**Pagos y vencimientos**

- **FR-027** (RF-27): El sistema DEBE permitir marcar un gasto como pagado.
- **FR-028** (RF-28): Si el gasto no está Vencido, al marcarlo pagado el sistema DEBE registrar la
  fecha actual como fecha de pago sin preguntar, incluso si el gasto es de un mes futuro.
- **FR-029** (RF-29): Si el gasto está Vencido, el sistema DEBE preguntar si se pagó hoy y, si no,
  permitir ingresar la fecha real.
- **FR-030** (RF-30): El sistema DEBE rechazar fechas de pago posteriores a la fecha actual; no
  hay fecha mínima (se acepta cualquier fecha hasta hoy inclusive, aun anterior al mes del gasto).
- **FR-031** (RF-31): El sistema DEBE permitir desmarcar un pago; el gasto vuelve a pendiente sin
  fecha de pago. Si es un recurrente y el pago era la única información propia de ese mes, su
  registro mensual DEBE eliminarse (el mes vuelve a 0 registros, NFR-005); si el mes tiene además
  un cambio "Solo este mes", el registro se conserva sin pago.
- **FR-032** (RF-32): Cada vez que se abre o recarga la aplicación, con o sin inicio de sesión, el
  sistema DEBE mostrar una alerta si hay al menos un pendiente que vence hoy, de cualquier mes
  (incluidos los recurrentes solo proyectados del mes actual). Los gastos ya vencidos no disparan
  la alerta; se distinguen por su estado (FR-033).
- **FR-033** (RF-33): Para cada pendiente, el sistema DEBE mostrar su estado: "Al día" (más de 3
  días o sin vencimiento), "Próximo a vencer" (1 a 3 días), "Vence hoy" o "Vencido", contando
  días calendario entre hoy y el vencimiento, sin considerar la hora. Un gasto pagado DEBE
  mostrarse como "Pagado" con su fecha de pago.

**Navegación**

- **FR-034** (RF-34): El sistema DEBE mostrar inicialmente el mes actual.
- **FR-035** (RF-35): El usuario DEBE poder navegar entre meses y años, pasados y futuros.

**Acceso**

- **FR-036** (RF-36): El sistema DEBE exigir email y contraseña antes de dar acceso a gastos y
  categorías.
- **FR-037** (RF-37): El único usuario DEBE crearse mediante carga inicial; NO DEBE existir ninguna
  forma de registrar usuarios.

**Exportación**

- **FR-038** (RF-38): El sistema DEBE exportar a CSV el historial desde el primer mes con algún
  gasto hasta el mes actual inclusive, incluyendo los meses en que un recurrente solo está
  proyectado. Si no hay ningún gasto hasta el mes actual, el archivo contiene solo el encabezado.
- **FR-039** (RF-39): El CSV DEBE estar en UTF-8 con BOM, separado por comas con comillas según
  RFC 4180, con encabezado `periodo,nombre,monto,categoria,vencimiento,estado,fecha_pago` y filas
  ordenadas por `periodo` ascendente y, dentro de un período, por `vencimiento` ascendente (sin
  vencimiento al final) y luego por `nombre`;
  `periodo` como `aaaa-mm`; fechas como `aaaa-mm-dd` o vacías; `monto` con punto decimal, 2
  decimales y sin separador de miles; `categoria` = `Sin categoría` si no tiene; `estado` =
  `Pagado` o `Pendiente`. Cada fila refleja los valores del gasto en ese mes y el nombre actual
  de su categoría; el archivo no incluye la descripción ni el tipo puntual/recurrente.

### Non-Functional Requirements

- **NFR-001** (RNF-01): Las operaciones de uso (iniciar sesión, ver la alerta, consultar y navegar
  meses, crear/editar/dar de baja/eliminar gastos, cambiar tipo, marcar/desmarcar pagos, gestionar
  categorías, ver el resumen y exportar) DEBEN responder en menos de 2 segundos en el p95, medido
  en el servidor (desde que recibe la solicitud hasta que envía la respuesta), con un usuario,
  hasta 100 gastos por mes y 10 años de historial.
- **NFR-002** (RNF-02): Todas esas operaciones DEBEN poder usarse en pantallas desde 360 px de
  ancho sin scroll horizontal ni superposición de elementos.
- **NFR-003** (RNF-03): La aplicación DEBE poder instalarse como aplicación web en las 2 últimas
  versiones estables de Chrome y Edge de escritorio y Chrome para Android, y usarse completa desde
  el navegador en las 2 últimas de Firefox de escritorio y Safari de macOS. Requiere conexión a
  Internet.
- **NFR-004** (RNF-04): Las contraseñas NO DEBEN guardarse en texto plano; DEBEN protegerse con un
  hash con salt de costo equivalente o superior al estándar de la industria indicado en el PRD.
- **NFR-005** (RNF-05): Los recurrentes NO DEBEN generar registros por adelantado: un mes sin
  información propia de un recurrente tiene 0 registros guardados de ese gasto.
- **NFR-006** (RNF-06): Un mes con información propia de un recurrente (cambio o pago) DEBE tener
  exactamente 1 registro guardado de ese gasto. El registro se conserva solo mientras tenga
  información propia (un pago o algún campo distinto del valor vigente); toda operación que lo
  deje sin información propia (desmarcar, "Este mes y los siguientes" que iguala sus campos) DEBE
  eliminarlo.
- **NFR-007** (RNF-07): La fecha actual DEBE calcularse en la zona horaria de Buenos Aires para
  la alerta, los estados de vencimiento, la fecha de pago, el mes inicial y el último mes incluido
  en la exportación.
- **NFR-008** (RNF-08): La sesión DEBE expirar tras 30 días sin actividad.

### Key Entities

- **Usuario**: única persona que usa el sistema; se identifica por email y contraseña. Existe
  exactamente uno.
- **Gasto**: obligación de pago con nombre, monto, descripción opcional, categoría opcional,
  vencimiento opcional y tipo (puntual o recurrente). Un puntual pertenece a un mes; un recurrente
  tiene un mes de inicio, está activo hasta su baja o paso a puntual (en ese caso su último mes es
  el anterior al de la conversión, y el mes de la conversión pasa a un puntual nuevo), y tiene valores vigentes
  (nombre, descripción, monto, categoría, vencimiento) que pueden cambiar desde un mes en adelante
  sin afectar los meses anteriores.
- **Registro mensual de un gasto recurrente**: información propia de un recurrente en un mes
  concreto (cambio "Solo este mes" o pago). Solo existe cuando hay información propia; si no, el
  gasto se muestra con sus valores vigentes para ese mes.
- **Pago**: estado pagado/pendiente de un gasto en un mes, con su fecha de pago si está pagado.
- **Categoría**: agrupador de gastos con nombre único (sin distinguir mayúsculas, acentos ni espacios
  repetidos). "Sin
  categoría" es un grupo reservado, no una categoría creable.
- **Período**: un mes de un año; es la unidad de consulta, alta, resumen y exportación.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Al cierre de cada mes, 0 gastos con vencimiento en ese mes quedan en estado Vencido
  o marcados como pagados con fecha de pago posterior a su vencimiento (métrica de éxito del PRD).
  La mide el usuario al inicio de cada mes con el CSV exportado (columnas `vencimiento`, `estado`
  y `fecha_pago` del mes anterior); no requiere una funcionalidad propia.
- **SC-002**: Un aumento permanente de un gasto recurrente se registra con una sola edición y se ve
  reflejado en todos los meses siguientes, sin cambiar ningún mes anterior.
- **SC-003**: El 95% de las operaciones de uso se completan en menos de 2 segundos con 10 años de
  historial y hasta 100 gastos por mes.
- **SC-004**: Todas las operaciones de uso pueden completarse en una pantalla de 360 px de ancho
  sin scroll horizontal.
- **SC-005**: El usuario identifica qué gastos del mes están pendientes, vencen hoy o están
  vencidos, y el total pendiente, sin salir de la vista del mes.
- **SC-006**: Los 68 criterios de aceptación del PRD (AC-01 a AC-68) se verifican y pasan.

## Assumptions

- La carga inicial del único usuario (email y contraseña) se hace al instalar el sistema, con
  valores provistos por fuera del código.
- Fuera de alcance (según PRD): múltiples usuarios, registro, cierre de sesión, recuperación de
  contraseña, límite o bloqueo por intentos fallidos de inicio de sesión, roles, reactivación de un recurrente dado de baja, multimoneda, cuentas bancarias, ingresos, filtros de exportación, instalación
  en Firefox y Safari, uso sin conexión y notificaciones push, por correo o WhatsApp.
