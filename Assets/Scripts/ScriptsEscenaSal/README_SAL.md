# Sala de máquinas (SAL)

> Este documento describe el prototipo original. Para el juego integrado inicia **Javier** y consulta [GUIA_FlujoJuego](../GUIA_FlujoJuego.md). En la integración actual `Standalone Demo` está desactivado, el sello ◆ está en Taller, el panel requiere N2 y los tres sellos, y SAL utiliza el inventario, Cuaderno y HUD compartidos. No reconstruyas la escena integrada con el generador del prototipo.

Esta carpeta contiene únicamente la implementación de la Persona 3. La escena está en `Assets/Scenes/SAL.unity`; el generador editable está en `Assets/Scripts/ScriptsEscenaSal/Editor/SAL_SceneBuilder.cs`.

## Abrir la escena

1. Abre el proyecto con Unity 6000.3.11f1 y espera a que terminen las importaciones.
2. Abre `Assets/Scenes/SAL.unity`. La primera vez, el generador llena la escena vacía y la guarda. También puedes usar `Jam3 > Build SAL Sala de Máquinas` para reconstruirla voluntariamente.
3. Pulsa Play. La opción `Standalone Demo` del objeto `SAL_Puzles` entrega Pelota, sello ▲ y sello ✚ para probar la sala sin las otras zonas. Desactívala al conectar las escenas del equipo.

La cámara es lateral y ortográfica, Siete usa un sprite 2D provisional y se mueve solo en X, y paredes, ductos, luces y suelo ocupan profundidad 3D. El arte actual es un graybox editable en la jerarquía.

## Recorrido de prueba

- `A` y `D`: caminar; `E`: interactuar; mantener `E`: limpiar; `Tab`: Cuaderno provisional.
- Limpia el basurero y observa los dibujos de Ella y la firma `||·`.
- Limpia el punto de ronda ◆.
- Lee la etiqueta del ducto 14. Usa la Pelota en la válvula y extrae el filtro. Comprueba que el LED se apaga y cambia el sonido del aire.
- Pulsa `▲ ◆ ✚` en ese orden. Un error bloquea el panel tres segundos y luego lo reinicia.
- Examina los seis bloques y elige el marcado `||·` para recuperar la Semilla y N9.
- Sal al Pasillo y vuelve: dibujos, filtro, LED, panel y semilla deben conservar su estado durante esta partida.

## Responsabilidades de los scripts

| Script | Responsabilidad |
| --- | --- |
| `SAL_RoomController` | Reglas de los puzles, recompensas y presentación del estado de la sala. |
| `SAL_InteractionDriver` y `SAL_Interactable` | Selección de un único objeto cercano y controles de interacción. |
| `SAL_Progress` | Banderas y acto de la partida; punto de conexión para `GameState` cuando exista. |
| `SAL_UI` | Indicaciones y Cuaderno provisional; se sustituye por el Cuaderno de la Persona 4. |
| `SAL_NoteData` | Datos de N5, N6, N7 y N9. |
| `SAL_AudioDirector` y `SAL_AudioLogData` | Audio por capas, efectos y registros A1 a A4 con subtítulos. |
| `SAL_ProceduralSound` | Sonidos originales provisionales; pueden reemplazarse desde el Inspector. |
| `SAL_Atmosphere` | Saturación y luz de SAL según el acto. |

`SAL_AudioDirector.PlayLog("A2", false)` reproduce el fragmento de la cabina y `PlayLog("A2", true)` su versión completa. Los datos A1 a A4 se generan en `Assets/SAL/Data`. Hay subtítulos y sonidos de prueba, pero no hay grabaciones de voz finales en el proyecto.

La salida a `Javier` usa la escena que ya existe. Los objetos de Sala se entregan al puente `CORE_InventoryBridge`; cuando la Persona 2 instale su inventario definitivo, hay que conectarlo allí. `SAL_Progress` expone `SetFlag`, `HasFlag` y `SetAct` para conectar el estado definitivo de la Persona 5. La escena de otro compañero no se modifica.

## Punto narrativo pendiente del GDD

El GDD coloca el sello ◆ en SAL, pero describe la secuencia `▲ ◆ ✚` como Laboratorio, Taller y Bodega. Esta escena sigue los símbolos y posiciones definidos en las tablas de zonas: ▲ Laboratorio, ◆ Sala, ✚ Bodega. El equipo debe corregir la frase sobre Taller para que N2 y el panel cuenten el mismo recorrido.
