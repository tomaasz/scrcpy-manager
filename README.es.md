# scrcpy Manager 📱🖥️

> **Compañero de escritorio universal para `scrcpy` y usuarios avanzados de Android.**  
> Abra aplicaciones de Android en ventanas flotantes independientes, gestione pantallas virtuales, conéctese de forma inalámbrica por Wi‑Fi (ADB), evite el bloqueo de pantalla y optimice flujos de trabajo de Escritorio Remoto (RDP).

<p align="center">
  <b>🌐 Language / Język / Sprache / Idioma:</b><br>
  <a href="README.md">🇬🇧 English</a> &nbsp;|&nbsp;
  <a href="README.pl.md">🇵🇱 Polski</a> &nbsp;|&nbsp;
  <a href="README.de.md">🇩🇪 Deutsch</a> &nbsp;|&nbsp;
  <b><a href="README.es.md">🇪🇸 Español</a></b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/C%23-Native%20WinForms-239120.svg" alt="C# Native" />
  <a href="https://github.com/Genymobile/scrcpy"><img src="https://img.shields.io/badge/scrcpy-v4.1%2B-brightgreen.svg" alt="scrcpy" /></a>
  <a href="https://github.com/tomaasz/scrcpy-manager/releases"><img src="https://img.shields.io/badge/Versi%C3%B3n-Edici%C3%B3n%20Portable-orange.svg" alt="Edición Portable" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/Licencia-MIT-yellow.svg" alt="Licencia: MIT" /></a>
  <img src="https://img.shields.io/badge/Idiomas-PL%20%7C%20EN%20%7C%20DE%20%7C%20ES-lightgrey.svg" alt="Idiomas" />
</p>

<p align="center">
  <img src="docs/screenshot.es.png" alt="Captura de pantalla de scrcpy Manager" width="360" />
</p>

---

## ⚡ Descarga Inmediata (Versión Portable)

👉 **[Descargar ScrcpyManager-Portable.exe (Última versión)](https://github.com/tomaasz/scrcpy-manager/releases)**

* **Cero instalación y sin dependencias**: Incluye `scrcpy v4.1`, Android Debug Bridge (`adb`) y todas las librerías necesarias en un único archivo ejecutable.
* **Listo para usar**: Funciona en cualquier equipo con Windows 10/11 sin necesidad de instalar previamente `scrcpy` ni configurar `PATH`.
* **Sin ventana de comandos**: Experiencia visual limpia sin ventanas de consola parpadeantes.
* **Inicio ultrarrápido**: El entorno de ejecución en caché verificado con SHA-256 se inicia en menos de 0,2 segundos.

---

## 🌟 Características Principales

* 🚀 **Aplicaciones en ventanas flotantes**: Abra cualquier app de Android en una ventana virtual independiente (`--new-display`).
* 🎨 **Iconos nativos en Windows**: Cada ventana abierta recibe su icono oficial en la barra de tareas de Windows y en Alt+Tab en lugar del icono genérico de `scrcpy`.
* 🖥️ **Diseño moderno y renovado**: Elegante tema grafito con bordes redondeados, suavizado anti-aliasing y efectos visuales al pasar el cursor.
* ⚙️ **Perfiles de inicio por aplicación**: Personalice resolución/DPI, FPS, tasa de bits, códec, orientación, teclado, ratón, audio, pantalla completa y visibilidad de la barra de tareas.
* 🎛️ **Preajustes listos para usar**: Predeterminado, Trabajo/RDP, Terminal (DPI alto para máxima legibilidad), Juegos, Ahorro de Wi-Fi y Presentación.
* 🛡️ **Control de Taskbar e instalador**: Ocultación de la barra de tareas del sistema (`--no-vd-system-decorations`) para que las ventanas no queden tapadas, además de detección e instalación automática en 1 clic de la app Taskbar vía ADB.
* 🎥 **Grabación de sesiones**: Grabe sesiones de apps específicas en formato MP4 con fecha en la carpeta `Videos\scrcpy-manager`.
* 🔎 **Búsqueda de paquetes en tiempo real**: Filtre y busque al instante aplicaciones instaladas en el teléfono conectado.
* 📶 **ADB inalámbrico (Wi‑Fi) en un clic**: Detección automática de la IP local y cambio inmediato de cable USB a conexión inalámbrica.
* 🔊 **Control de audio**: Redirija el sonido del teléfono a los altavoces o auriculares del ordenador en tiempo real.
* ⌨️ **Teclado físico y ratón (UHID)**: Compatibilidad total con caracteres internacionales y atajos AltGr (`-K`), junto con emulación de ratón por hardware (`--mouse=uhid`, `-ForwardAllClicks`) para facilitar la selección y copiado de texto.
* 🖥️ **Perfiles RDP optimizados (Windows App)**:
  * `Full HD 1080p (Nativa 1:1)` – Nitidez píxel por píxel.
  * `2K QHD (2560x1440)` – 77 % más de área de trabajo.
  * `4K UHD (3840x2160)` – Máxima resolución para pantallas grandes.
  * *Tasa de bits alta (16 Mbps)* para fuentes nítidas en sesiones remotas.
* 🔙 **Navegación cómoda con tecla ESC**: Al pulsar `ESC` en cualquier ventana de app se ejecuta de inmediato la acción `Atrás` (igual que la flecha `←` de la app).
* 🧭 **Barra de navegación anclada**: Barra flotante inferior con botones `◀` (Atrás), `●` (Inicio) y `▢` (Recientes) acoplada a las ventanas de scrcpy.
* 📱 **Botones de control en el panel**: Manejo rápido del dispositivo (`◀ Atrás`, `● Inicio`, `▢ Recientes`) directamente desde el dashboard.
* 📐 **Diseños de cuadrícula flexibles**: Cambie con un clic entre lista de 1 columna, vista de 2 columnas o modo compacto de 3 columnas.
* 🌓 **Modo Oscuro y Claro**: Alternancia rápida de tema visual (`☀️ Claro` / `🌙 Oscuro`).
* 🌐 **Multilingüe (4 idiomas)**: Interfaz en español, polaco, inglés y alemán con persistencia de preferencias.
* 🔄 **Actualizaciones automáticas integradas y verificación**: Comprobación al inicio, notas de versión y actualización en 1 clic de la versión Portable con verificación SHA-256.
* 🔌 **Guía interactiva de conexión y autorreparación ADB**: Asistente en 3 pasos para la primera conexión (Opciones de desarrollador, Depuración USB, Autorización RSA), consejos de cables/puertos, acceso al Administrador de dispositivos y recuperación automática del servidor ADB.
* 🔋 **Tarjeta de estado y botón de GitHub**: Nivel de batería con indicación de carga, estado de conexión, modo activo, enlace directo al repositorio de GitHub y comprobación interactiva de versiones.

---

## 🚀 Inicio Rápido (Ejecución desde el código fuente)

Si prefiere ejecutar el script directamente en lugar de descargar el `.exe` portable:

### Requisitos
1. **Windows 10 / 11** (PowerShell 5.1 o PowerShell 7+)
2. **[scrcpy](https://github.com/Genymobile/scrcpy)** (v2.0 o posterior) y **adb** configurados en el `PATH`
3. Dispositivo Android con **Depuración USB** activada

### Instrucciones
1. Clone el repositorio:
   ```bash
   git clone https://github.com/tomaasz/scrcpy-manager.git
   cd scrcpy-manager
   ```
2. Haga doble clic en `ScrcpyApp.cmd` o ejecute en PowerShell:
   ```powershell
   & .\ScrcpyApp.ps1
   ```

---

## ⚙️ Personalización de Botones (`apps.json`)

`apps.json` define la lista predeterminada de accesos directos. También puede usar el botón **Editar** en el programa para añadir aplicaciones desde su teléfono, renombrarlas y organizarlas. Las preferencias personalizadas se guardan en `%APPDATA%\scrcpy-manager\apps.json`.

```json
[
  {
    "name": "Messenger",
    "package": "com.facebook.orca",
    "flags": []
  },
  {
    "name": "Spotify",
    "package": "com.spotify.music",
    "flags": []
  },
  {
    "name": "Windows App (RDP)",
    "package": "com.microsoft.rdc.androidx",
    "flags": ["-UseUhidKeyboard", "-UseUhidMouse", "-ForwardAllClicks"]
  }
]
```

---

## 🙏 Créditos y Software de Terceros

Este proyecto se ha desarrollado gracias a las siguientes tecnologías de código abierto:

* **[scrcpy](https://github.com/Genymobile/scrcpy)** creado por [Romain Vimont (@rom1v)](https://github.com/rom1v) y [Genymobile](https://github.com/Genymobile) (Licencia Apache 2.0)
* **[Android Debug Bridge (ADB)](https://developer.android.com/tools/adb)** por Google y AOSP (Licencia Apache 2.0)
* **[Taskbar](https://github.com/farmerbb/Taskbar)** por [Braden Farmer (farmerbb)](https://github.com/farmerbb) (Licencia Apache 2.0)
* **[Windows Forms CueBanner](https://learn.microsoft.com/en-us/windows/win32/controls/em-setcuebanner)** por Microsoft
* **[Google Play Store API](https://play.google.com)** para iconos vectoriales

---

## 📜 Licencia

Este proyecto se distribuye bajo los términos de la **Licencia MIT**. Consulte el archivo [LICENSE](LICENSE) para más detalles.