# 🚀 Guess The Song - Server (SignalR Hub)

![.NET](https://img.shields.io/badge/.NET%208.0-512BD4?style=flat&logo=dotnet&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-Real--Time-orange?style=flat)
![MonsterASP](https://img.shields.io/badge/Hosted%20on-MonsterASP-blue?style=flat)

Este repositorio contiene el núcleo lógico y el servidor de comunicación para el ecosistema **Guess The Song**. Desarrollado con **ASP.NET Core SignalR**, este servidor actúa como el orquestador central que gestiona las salas de duelo, la sincronización de audio y la validación de respuestas en tiempo real.

---

## 🏗️ Arquitectura y Lógica del Sistema

El servidor está diseñado bajo un modelo de **gestión de estado en memoria** utilizando colecciones concurrentes para garantizar la integridad de los datos en entornos con múltiples hilos.

### 🧩 Gestión de Salas (`SalaDuelo`)
Cada partida se encapsula en una instancia de `SalaDuelo`, que mantiene el estado crítico de forma aislada:
* **Concurrencia Segura:** Uso de `ConcurrentDictionary` para el manejo de múltiples salas simultáneas sin bloqueos.
* **Ciclo de Vida de Ronda:** Controlado mediante `CancellationTokenSource`, permitiendo cancelar los tiempos de espera (25s) de forma segura cuando un jugador acierta antes de que expire el tiempo.
* **Control de Host:** Lógica para asignar privilegios de inicio y reinicio al primer jugador de la sala, con reasignación automática del rol de "Host" en caso de desconexión.

### 📊 Modelado de Datos
El servidor gestiona modelos de datos optimizados para la API de iTunes:
* **`Result` Model:** Implementa `INotifyPropertyChanged` para el seguimiento de metadatos (trackName, previewUrl, artwork).
* **Normalización de Títulos:** Algoritmo basado en **Regex** que limpia los nombres de las canciones (eliminando paréntesis y corchetes como "(Remix)" o "[Live]") para asegurar que la validación de respuestas sea justa.

---

## 🛠️ Funcionalidades Técnicas Core

### 1. Orquestación de Partida
El servidor no solo transmite mensajes, sino que gestiona el flujo del juego:
* **Shuffle Logic:** Mezcla aleatoriamente la lista de canciones antes de cada partida.
* **Extracción de Arte:** Realiza *scraping* técnico en tiempo real para extraer la imagen `og:image` del perfil del artista en iTunes, proporcionando una estética visual de alta resolución.
* **Generador de Distractores:** Crea dinámicamente una lista de opciones incorrectas basadas en las otras canciones del setlist para las preguntas de opción múltiple.

### 2. Validación y Puntuación
* **Single Answer Policy:** Uso de `HashSet<string>` para registrar jugadores que fallaron, impidiendo múltiples intentos en una misma ronda.
* **Sistema de Puntos:** +10 puntos por acierto, -5 puntos por fallo.
* **Sincronización de Audio:** El servidor emite comandos `PararAudio` y `NuevaRonda` para asegurar que todos los clientes estén en el mismo punto de la partida.

### 3. Configuración de Red (Middleware)
* **CORS Evolucionado:** Política optimizada para permitir cualquier origen, método y cabecera, asegurando la compatibilidad con clientes de escritorio WPF/WinForms.
* **Hybrid Environment Support:** Incluye inyección de cabeceras (`ngrok-skip-browser-warning`) para facilitar el desarrollo local mediante túneles, permitiendo exponer el `localhost` sin interrupciones de seguridad.
* **Production Ready:** Configurado para despliegue inmediato en entornos como **MonsterASP**, manejando correctamente el enrutamiento de WebSockets.
---

## 📡 Hub de Comunicación (`GameHub`)

* **Hub URL:** `/gamehub`
* **Métodos Clave:**
    * `UnirseASala(string salaId, string nombreJugador)`: Gestiona el login y grupos de SignalR.
    * `IniciarPartida(string salaId, List<Result> canciones, string artista)`: Prepara el setlist e inicia el conteo.
    * `ValidarRespuesta(string salaId, string respuestaCliente)`: Evalúa la respuesta y otorga puntos.
    * `ProximaRonda(string salaId)`: Salta a la siguiente canción del setlist.

---

## 💻 Tecnologías Utilizadas
* **ASP.NET Core 6.0**
* **SignalR** (WebSockets / Long Polling)
* **HttpClient** (Scraping de imágenes de artista)
* **Regular Expressions (Regex)** (Limpieza de metadatos)

---

## 📄 Licencia
Este proyecto es de código abierto bajo la licencia [MIT](LICENSE).

---

**Desarrollado por [Benjamin Ramos](https://github.com/Benjamin-Ramos)**
