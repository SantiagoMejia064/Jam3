# Menu de inicio

Escena: `Assets/inicio/MenuInicio.unity`.

La escena del menu se renombro desde `inicio` para evitar que las cargas por
nombre la confundieran con el nivel `Assets/Scenes/Inicio.unity`. Conserva el
mismo GUID y se coloco primera en las escenas del build. Las demas entradas
del build mantienen su orden y estado.

## Jerarquia

Todo lo nuevo esta en `MENU_InterfazInicio`, separado del entorno existente:

- `MENU_Controlador`: acciones de Jugar y Salir.
- `MENU_Canvas`: Screen Space - Camera, enlazado a `MENU_Camara_Principal`.
  - `MENU_Composicion16x9`: composicion de referencia para la interfaz.
    - `MENU_AreaInterfaz`: titulo PNG y botones editables de Jugar y Salir.
  - `MENU_Cargando`: panel visible durante el cambio de escena.
- `MENU_EventSystem`: entrada de raton, teclado y mando con el Input System.

El entorno, sus materiales, luces y camara se conservan. Se compararon todos
los componentes originales con la copia previa: solo cambia la lista de raices
para incluir la nueva interfaz.

## Imagen del titulo

`LoQueGuarde_Titulo.png` es un PNG RGBA de 1774 x 887 con transparencia real,
importado como Sprite, sin mipmaps ni compresion. Creado con la herramienta
integrada de generacion de imagenes a partir de la propuesta de menu aprobada.
Incluye titulo, rama botanica y linea ambar. Los textos de los botones siguen
siendo componentes TextMeshPro independientes; no forman parte del PNG.

## Scripts

En `Assets/Scripts/ScriptsMenuInicio`:

- `MENU_MainMenu.cs`: Jugar comprueba y carga `Assets/Scenes/Javier.unity`.
  Reinicia banderas e inventario, establece el acto 1 y crea el inventario
  existente de Resources si hace falta. Evita iniciar la carga dos veces.
  Salir cierra la aplicacion compilada; dentro del editor detiene Play.
- `MENU_MenuButton.cs`: colores, indicador de seleccion y comportamiento visual
  de los botones, conservando Button y su navegacion normal.
- `MENU_ButtonFrame.cs`: dibuja el marco con extremo recortado directamente
  en el Canvas, para mantener el borde fino al escalar.

## Edicion manual

1. Abrir MenuInicio y la vista Game.
2. Seleccionar `MENU_AreaInterfaz` para desplazar el conjunto.
3. Seleccionar `MENU_Titulo_PNG` para cambiar el tamano o sustituir la imagen.
4. En cada boton, editar el hijo `MENU_Texto_JUGAR` o `MENU_Texto_SALIR`.
5. La posicion de cada boton se ajusta con su RectTransform.
6. En Play, el boton activo usa color ambar. Se puede pulsar con raton o
   seleccionar con las flechas y activar con Intro/espacio.

Se usa la fuente LiberationSans SDF ya incluida en el proyecto. El Canvas
tiene referencia de 1920 x 1080 y escala con la pantalla.

Las copias previas y la vista capturada en Unity estan en `Tools/MenuInicio`.
La utilidad usada para crear el Canvas quedo archivada en
`Tools/MenuInicio/UtilidadesEditor`, fuera de Assets, para que no se ejecute
automaticamente ni forme parte del juego.

## Comprobacion realizada

- PNG: comprobado canal alfa con valores de 0 a 255.
- Unity compilo los scripts y ejecuto la escena.
- Jugar: clic real en Play abrio `Assets/Scenes/Javier.unity`, con inventario visible.
- Salir: clic real en Play detuvo la ejecucion del editor.
- Interfaz revisada en la vista Game a referencia Full HD y captura 1920 x 1080.
- No se ha generado ni probado un ejecutable. El cierre de aplicacion compilada
  usa `Application.Quit`; teclado y mando quedan configurados en el Input System,
  sin dar por validada su entrada fisica.
