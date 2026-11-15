# Sokoban de baterías — diseño y ciencia

## Propósito educativo

El jugador ayuda a una estación lunar a recuperar energía eléctrica colocando dos baterías en sus ranuras de carga. El rompecabezas enseña planificación, espacio limitado, secuencias y el costo de usar el tiempo y el descanso de la tripulación.

## Cómo se juega

- Mueve al astronauta con las flechas o WASD; también puedes usar los botones en pantalla.
- Empuja una batería por vez. No puedes tirar de ella ni empujarla contra una pared u otra batería.
- Coloca las dos baterías sobre las dos ranuras doradas.
- `Deshacer` revierte el último movimiento. `Reiniciar tablero` vuelve al inicio de la misión actual.
- Iniciar una misión consume 10 puntos de Descanso. Completarla aumenta Energía eléctrica en 25 puntos (máximo 100).
- Volver a jugar inicia una misión nueva y vuelve a consumir 10 puntos de Descanso.

## Conexión con ciencia real

En microgravedad, una persona no puede confiar en su peso para mantenerse fija mientras aplica fuerza. NASA describe el uso de pasamanos, anclajes y restricciones para estabilizar a la tripulación y controlar su movimiento. Por eso, el juego presenta un astronauta que trabaja desde puntos de sujeción y mueve baterías sobre guías de carga, no como si pudiera caminar normalmente sobre un piso lunar.

Los sistemas eléctricos de naves y estaciones espaciales incluyen generación, almacenamiento, control y distribución de energía. Los paneles solares generan electricidad y las baterías almacenan energía para usarla cuando hace falta. El minijuego simplifica esa idea en una tarea de colocar baterías en ranuras de carga; sus puntos son una abstracción educativa, no una medida de kilovatios-hora reales.

## Fuentes NASA para el equipo

- NASA, *NASA Spaceflight Human-System Standard, Volume 2*, apartado 8.5.1 sobre restricciones y estabilización de la tripulación: https://www.nasa.gov/wp-content/uploads/2025/05/nasa-std-3001-vol-2.pdf
- NASA, *Electrical Power System (EPS)*: explica generación por arreglos solares y almacenamiento en baterías: https://www.nasa.gov/wp-content/uploads/2023/05/162182main-sts-116-press-kit.pdf
- NASA Small Spacecraft Systems Virtual Institute, *3.0 Power*: describe generación, almacenamiento, control y distribución: https://www.nasa.gov/smallsat-institute/sst-soa/power-subsystems/

## Alcance y limitaciones del prototipo

- El tablero es fijo y tiene dos baterías y dos ranuras.
- Los costos `restCost` y `powerReward` se pueden modificar desde el Inspector.
- La interfaz se genera al abrir la escena y utiliza Canvas, uGUI, TextMeshPro y Sliders.
- No se simula física de microgravedad; los rieles y anclajes representan una forma controlada de manipular carga.
- No se ha ejecutado ni compilado este proyecto con Unity en el entorno de preparación. Se debe validar en el Editor antes de presentar una build.
