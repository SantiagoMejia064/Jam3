# Audio del Taller

En la escena `Taller`, selecciona el objeto `TAL_Audio` para cambiar los clips de Cortador, Cable, tablilla N3, conexión del Cable, arranque de la pulidora y limpieza. Los cinco WAV de esta carpeta son efectos cortos sintetizados para estas interacciones y pueden sustituirse desde el Inspector.

Sus hijos `Objetos`, `Notas`, `Pulidora` y `Limpieza` contienen las fuentes para ajustar el volumen de cada grupo. Son fuentes 2D y no se reproducen al entrar. Las recogidas usan una fuente separada del objeto recogido, por lo que el sonido termina aunque desaparezca el objeto.

La cámara activa `SAL_Camara_2_5D` lleva el `AudioListener` del Taller. Debe existir uno activo durante la partida. Los WAV nuevos se precargan para responder al interactuar.

La limpieza reutiliza `Assets/Sonidos/Bodega/limpiar.mp3` y solo suena mientras se mantiene E sobre el motor, el sello o el registro N10. Se detiene al soltar E, salir del área, abrir el Cuaderno o terminar.

Las conexiones son:

- `CORE_Collectible.onCollected` de Cortador y Cable llama a `PlayCutterPickup` o `PlayCablePickup`.
- `TAL_Estante.audioFeedback` reproduce la tablilla solo cuando se obtiene N3 por primera vez.
- `TAL_Pulidora.audioFeedback` reproduce la conexión y el arranque después de validar cada paso.
- N10 conserva su `CORE_Dirt.cleanAudioSource` del Cargador y usa `Leer_Tablilla.wav` al descubrir la nota.

El casillero conserva `openingSound`; el Cargador, `restSound`; el sello, su sonido de recompensa. El pulso sigue en `TAL_Pulidora.pulseSource`, con su clip o el generado por `SAL_ProceduralSound.CreatePulse()` y el intervalo original de 1,2 segundos. El arranque nuevo no sustituye ese pulso.
