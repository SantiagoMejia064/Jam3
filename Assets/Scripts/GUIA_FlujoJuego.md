# Flujo integrado hasta la Semilla

La escena de inicio para probar el juego es `Assets/Scenes/Javier.unity`.
Invernadero y el desenlace quedan fuera de esta revisión.

## Orden de juego

1. **Javier, Laboratorio y Pasillo:** A1 se muestra al despertar. Limpia el reactor para N1 y la Pelota. Selecciona la Pelota y úsala en la cápsula para Oscuridad. Limpia el punto de ronda ▲. El terminal permite escuchar o leer A3; al acercarte a la cabina se reproduce A2.
2. **Taller:** recoge Cortador y Cable. La Pelota abre el casillero y entrega N2 sin consumirse. El estante entrega N3. Limpia el motor, selecciona e instala el Cable y pulsa E de nuevo para probar la pulidora: entrega Pulso. Limpia el punto de ronda ◆.
3. **SAL y Bodega pueden visitarse en cualquier orden.** En SAL, limpia el basurero para N6 y lee la etiqueta para N7. La Pelota seleccionada abre la válvula y permite extraer Bruma. En Bodega, el Cortador seleccionado abre la puerta por primera vez. Recoge N4, limpia las cuatro bandejas y lee sus números con una pulsación nueva de E. De oscuro a claro forman 7294. El teclado abre la cámara de Calor; dos errores completos iluminan la pista del ducto. Limpia el punto de ronda ✚.
4. **Regresa a SAL:** N2 y los tres sellos habilitan el panel. Pulsa ▲◆✚. Un error reinicia el panel tras tres segundos. Se registra N5 y aparecen seis bloques. La firma `||·` identifica el bloque de índice 3: entrega Semilla y N9.
5. **Regresa al Taller:** con la Semilla ya obtenida puedes limpiar detrás del Cargador para N10. El acceso al Invernadero requiere Semilla y Cortador seleccionado en su primera apertura.

## Cómo se conserva el progreso

- `CORE_Inventory` conserva objetos e historial de recogida durante la partida. Un objeto consumido no vuelve a aparecer.
- `SAL_Progress` conserva banderas y acto. Entrar de nuevo en una sala anterior no reduce el acto.
- `NotebookController` conserva notas y sellos. Tab abre el Cuaderno; mientras está abierto no se mueve ni interactúa el personaje.
- `BOD_Tray.cleanFlag` identifica cada bandeja limpia. Las cuatro asignaciones de Bodega son distintas.
- `BOD_CamaraAbierta` conserva la solución del teclado; Calor no reaparece después de recogerse.
- `CORE_ZoneDoor.openedFlag` permite atravesar una puerta ya abierta sin volver a seleccionar la herramienta. `BodegaAbierta` e `InvernaderoAbierto` identifican estas puertas.

El estado es de la sesión actual: no equivale a guardar y cargar una partida en disco.

## Entradas y salidas

`PlayerPersistence` conserva un solo jugador. Cada escena tiene un `SpawnPoint` de entrada. En Javier hay además puntos con `Entry From Scene` igual a `Taller`, `SAL` o `Bodega`; al volver se usa el correspondiente. Un punto con ese campo vacío sirve como entrada general. No coloques dos puntos generales en la misma escena.

## Interfaz

- `ObjetivoHUD`, mediante `TAL_ObjetivoUI`, calcula el siguiente objetivo usando objetos recogidos, notas y banderas. Las cuatro escenas tienen su presentación asignada; el Cuaderno abierto la oculta.
- `CORE_InteractionHUD` presenta acciones y respuestas. SAL usa el mismo panel mediante `SAL_UI`.
- `SubtitulosHUD`, dentro del Cuaderno persistente, usa TextMeshPro y `CORE_SubtitlesUI` para mostrar A1–A4. El prefab está en `Assets/InventoryUIAssets/Comun`.
- Las páginas especiales N5 y N6 están en `Assets/SAL/UI` y se asignan en `SAL_RoomController`.
- El teclado de Bodega detiene el movimiento mientras está abierto. Cerrar o Esc lo reanuda; Tab lo cierra para abrir el Cuaderno.

## Pendiente fuera del recorrido revisado

Faltan las grabaciones de voz A1–A4; sus subtítulos sí están disponibles. Invernadero, la incubadora y el desenlace requieren su propia revisión al terminar esa zona. Las relecturas alternativas permanecen protegidas por `Revelado`; recoger N1 no activa esa bandera.

Javier conserva una referencia anterior a `CORE_LabPasilloPalette` en `CORE_AudioDirector`, pero el archivo del script no está en el repositorio (GUID `24a57830123b4a328f01dc182f97a011`). No es el componente `SAL_AudioDirector`, que sí existe y funciona. La referencia produce un aviso de script faltante y requiere recuperar el archivo original para revisar su efecto visual. No se inventó un reemplazo para ese comportamiento desconocido.

Los generadores de escenas del menú Editor pertenecen a versiones anteriores del escenario. No reconstruyas las escenas integradas con esos menús: volverían a crear el graybox y perderías asignaciones y arte.
