# Mana Zone

Mana Zone es un prototipo de duelos mágicos en tercera persona, desarrollado en Unity para PC. El combate combina proyectiles, control, defensa, gestión de maná y lectura de patrones enemigos.

Este documento describe la implementación actual del proyecto. Las funciones indicadas como limitaciones o trabajo futuro no deben interpretarse como mecánicas terminadas.

## Tecnologías

- Unity `6000.3.7f1`.
- C#.
- Unity Input System.
- Unity Physics y `CharacterController`.
- ScriptableObjects para definiciones de hechizos, patrones y personalidades.

## Combate existente

El sistema enemigo reutiliza los sistemas de combate existentes:

| Componente o contrato | Función |
|---|---|
| `SpellCaster` | Selecciona y lanza hechizos del loadout. También ofrece `TryCastSpell` y `CanCastSpell` para la IA. |
| `SpellDefinition` | Define un hechizo reutilizable como ScriptableObject; cada definición implementa `Cast`. |
| `ManaSystem` | Mantiene el maná, comprueba gastos y regenera recursos. El valor inicial habitual es 100 de máximo y 12 unidades/s. |
| `HPSystem` | Expone vida y recibe daño mediante `IDamageable`. Al llegar a cero destruye el GameObject. |
| `IDamageable` | Contrato para recibir daño. |
| `IStunnable` | Contrato para recibir un aturdimiento. |
| `ISlowable` | Contrato para recibir ralentización. |
| `ILaunchableSpell` | Contrato que permite lanzar un prefab de proyectil en una dirección. |
| `SpellProjectile` | Proyectil de daño. Puede aplicar daño y, si está configurado, ralentización. |
| `CCProjectile` | Proyectil de control; aturde si encuentra un `IStunnable`. |
| `TPSController` | Movimiento del jugador. Implementa `IStunnable`, por lo que el proyectil de control puede detenerlo temporalmente. |

La IA no mantiene una segunda reserva de maná ni un segundo mecanismo de lanzamiento. El `SpellCaster` cobra el coste, comprueba el cooldown y llama a la definición del hechizo. Para que un enemigo pueda usar un hechizo, este debe aparecer tanto en el patrón como en `SpellCaster → Equipped Spells`.

Los proyectiles ofensivos atraviesan otros proyectiles. Shield intercepta y destruye el proyectil ofensivo; Parry lo refleja. El resto de las colisiones del juego siguen resolviéndose en los scripts de cada proyectil.

## IA enemiga

### Componentes

`EnemyCombatAI` es el cerebro común de los enemigos. Requiere un `CharacterController` y recibe referencias a:

- El jugador.
- Un asset `EnemyAIPersonality`.
- Una lista de assets `EnemyAIPattern`.
- El `SpellCaster`, `ManaSystem` y, opcionalmente, `HPSystem` del enemigo.

Si `SpellCaster`, `ManaSystem` o `HPSystem` están en el mismo GameObject, la IA intenta encontrarlos automáticamente. El `SpellCaster` también busca `ManaSystem` en su propio GameObject si no se asignó en su campo. El punto de lanzamiento sí debe asignarse en `SpellCaster → Cast Point`.

El enemigo no necesita Input Actions. Deja `SpellCaster → Input Actions Asset` vacío: los controles del jugador y la API de lanzamiento compartida son independientes.

### Máquina de estados

| Estado | Qué hace |
|---|---|
| `Idle` | Espera la próxima evaluación mientras conserva movimiento de combate. |
| `Approach` | Se acerca cuando está más lejos de la distancia preferida. |
| `Reposition` | Se aleja cuando está demasiado cerca. |
| `EvaluateCombat` | Busca patrones elegibles y elige uno ponderado. |
| `PrepareAttack` | Espera el tiempo de preparación y reacción antes de atacar. |
| `Attack` | Ejecuta una acción del patrón por turno. |
| `Defend` | Prepara y ejecuta un patrón cuya primera acción es un `DefensiveSpellDefinition`. |
| `Recover` | Espera la recuperación del patrón o la pausa entre acciones. |
| `Stunned` | Interrumpe el patrón y no mueve al enemigo hasta que termina el stun. |
| `Dead` | Detiene las decisiones si el `HPSystem` llega a cero. |

La IA primero busca la distancia preferida de la personalidad, con una tolerancia de una unidad. Solo después evalúa patrones. Durante preparación, ataque, espera y recuperación, el enemigo mantiene movimiento lateral y corrige su separación respecto al jugador.

### Cómo se elige un patrón

La aleatoriedad solo interviene después de comprobar las condiciones obligatorias. Un patrón se descarta si:

1. La distancia queda fuera de `Min Distance` y `Max Distance`.
2. Es de tipo `Counter` y ningún sensor ha llamado a `ObservePlayerAttack()`.
3. No tiene acciones, contiene una referencia vacía o alguna acción no está equipada.
4. El `SpellCaster` informa que una acción está en cooldown o no se puede pagar.
5. El maná actual no alcanza para cubrir el coste sumado de las acciones del patrón.

Para los patrones válidos, la IA usa selección ponderada. El peso comienza con `Selection Weight × Preference(tipo)`. El hostigamiento recibe un multiplicador de agresividad; el contraataque recibe el inverso. El coste total reduce el peso según el maná máximo y `Mana Spending`. Si el patrón configura `Low Player Health Bonus` y el jugador tiene `HPSystem`, el peso aumenta cuando al jugador le queda poca vida.

El peso solo afecta la probabilidad de elegir entre opciones válidas; nunca habilita una acción que incumple rango, maná, cooldown o loadout.

### ScriptableObjects de patrón

Crea un patrón desde **Create → Mana Zone → Enemy AI → Attack Pattern**. Los assets de ejemplo están en `Assets/EnemyAI/PatronAtaque`.

| Parámetro | Qué configura y cómo influye |
|---|---|
| `Pattern Id` | Identificador legible para depuración y organización. No altera por sí solo la selección. |
| `Kind` | Categoría usada para consultar las preferencias de la personalidad: `Harassment`, `Charged`, `Counter` o `SpaceControl`. |
| `Actions` | Referencias a `SpellDefinition`, en orden de lanzamiento. No son prefabs. Todas las acciones deben estar en el loadout del `SpellCaster`. |
| `Min Distance` / `Max Distance` | Rango obligatorio de selección. Fuera de él el patrón no participa en el sorteo. |
| `Preparation Time` | Pausa legible antes de ejecutar la primera acción. Se suma al tiempo de reacción efectivo. |
| `Pause Between Actions` | Pausa entre acciones consecutivas del mismo patrón. |
| `Recovery Time` | Ventana después de acabar un patrón. Más recuperación deja una oportunidad mayor para que el jugador responda. |
| `Selection Weight` | Peso base frente a otros patrones válidos. No omite sus condiciones. |
| `Low Player Health Bonus` | Aumenta el peso si el jugador tiene `HPSystem` y está herido. Rango 0–1. |

Los cooldowns también se aplican entre acciones. Por eso evita poner varias veces el mismo hechizo en una cadena si su cooldown supera `Pause Between Actions`: la acción posterior podría no estar disponible y la secuencia se interrumpirá.

#### Patrones incluidos

| Asset | Tipo | Acciones | Rango | Preparación | Recuperación | Uso de prueba |
|---|---|---|---:|---:|---:|---|
| `Hostigamiento_Prueba` | `Harassment` | `OffensiveBolt` | 0–18 | 0,15 s | 0,75 s | Ataque rápido de bajo coste. |
| `AtaqueCargado_Prueba` | `Charged` | `OffensiveBolt` | 5–24 | 1,25 s | 1,6 s | Mismo hechizo de prueba con una señal previa más larga. |
| `ControlEspacio_Prueba` | `SpaceControl` | `BindingBolt` | 2–16 | 0,65 s | 1,2 s | Prueba el proyectil de stun y el control del jugador. |

El patrón `PatronPrueba` es un asset adicional de la escena y puede editarse desde el Inspector.

### ScriptableObjects de personalidad

Crea un asset desde **Create → Mana Zone → Enemy AI → Personality**. El menú **Mana Zone → Enemy AI → Create Example Personalities** genera cuatro ejemplos en `Assets/EnemyAI/Personalities`.

| Parámetro | Qué configura y cómo influye |
|---|---|
| `Aggression` | Un valor mayor acorta reacción y reevaluación (con límites mínimos de 0,15 s y 0,1 s), aumenta el peso del hostigamiento y reduce relativamente el del contraataque. |
| `Mana Spending` | A mayor valor, menos se penaliza el peso de patrones costosos. No permite gastar maná que no existe. |
| `Preferred Distance` | Distancia objetivo. La IA se acerca si está más de una unidad lejos y se reposiciona si está más de una unidad cerca. |
| `Reaction Time` | Espera base antes de una selección/ataque. El valor efectivo se divide entre `max(0,5, Aggression)`. |
| `Selection Interval` | Frecuencia de evaluación y movimiento de aproximación. También se divide entre `max(0,5, Aggression)`. |
| `Movement Speed` | Velocidad para acercarse y alejarse. |
| `Combat Strafe Speed` | Velocidad de desplazamiento lateral mientras combate. |
| `Dodge Speed Multiplier` | Multiplicador de velocidad durante una esquiva. |
| `Dodge Duration` | Duración de cada impulso evasivo. |
| `Dodge Every Patterns` | Cada cuántos patrones completados se dispara una esquiva. Un valor de 1 significa tras cada patrón; 2, cada segundo patrón. |
| `Strafe Direction Interval` | Tiempo que mantiene una dirección antes de invertir el giro. La duración real varía entre 0,85 y 1,25 veces este valor. Las esquivas no invierten el giro. |
| `Harassment Preference`, `Charged Preference`, `Counter Preference`, `Control Preference` | Preferencia de 0–1 por categoría. Cero excluye en la práctica ese tipo; valores mayores aumentan su peso relativo. |

Valores actuales de los perfiles de ejemplo:

| Personalidad | Agresión | Distancia | Velocidad de aproximación | Velocidad lateral | Esquiva cada | Intervalo de giro | Prioridad principal |
|---|---:|---:|---:|---:|---:|---:|---|
| `Prodigy` | 1,35 | 7 | 3,5 | 2,5 | 1 patrón | 3 s | Hostigamiento |
| `Strategist` | 0,75 | 8 | 2,8 | 1,8 | 3 patrones | 4 s | Contraataque |
| `Artillery` | 0,85 | 13 | 2,5 | 1,7 | 2 patrones | 4,5 s | Ataque cargado |
| `Illusionist` | 0,95 | 9 | 3 | 2,4 | 1 patrón | 3,3 s | Control de espacio |

Los perfiles son datos reutilizables: asignar otro perfil cambia prioridades, distancias y movimiento sin crear otra clase de IA. Ajusta los valores en el asset para balancear un enemigo sin editar código.

### Movimiento, suelo y paredes

El enemigo usa `CharacterController`; no usa `NavMeshAgent`. Antes de cada paso comprueba que haya suelo de la layer `Suelo` bajo el destino. Si el paso directo sale de la superficie, prueba direcciones próximas para bordear el límite.

Antes de desplazarse, también hace un `SphereCast` frente al cuerpo usando `Obstacle Layers`. Si encuentra una pared dentro de `Wall Clearance`, busca una ruta cercana libre. La comprobación ignora al jugador para permitir que el enemigo se acerque a distancia de combate.

| Parámetro de `EnemyCombatAI` | Efecto |
|---|---|
| `Ground Layers` | Capas que cuentan como suelo transitable. Debe incluir `Suelo`. |
| `Ground Probe Distance` | Distancia hacia abajo para comprobar el suelo bajo el siguiente paso. |
| `Obstacle Layers` | Capas que bloquean el movimiento. Por defecto incluye todo excepto `Suelo`. |
| `Wall Clearance` | Margen de seguridad para empezar a buscar una ruta alternativa antes de rozar una pared. Aumentarlo produce desvíos más tempranos. |

El proyecto reserva la layer `Suelo` y asigna `Suelo1` y `Suelo2` de `EscenarioPrueba` a ella. Asigna también esa layer a cualquier otra superficie por la que deba caminar un enemigo. Las paredes deben tener colliders y estar incluidas en `Obstacle Layers`.

### Justicia, control y depuración

- `Attack Accuracy` en `EnemyCombatAI` es la probabilidad de no fallar un lanzamiento. Una acción fallida se omite; las condiciones de maná y cooldown siguen siendo obligatorias.
- `Stun Duration` configura el stun del enemigo. El `TPSController` del jugador tiene su propio `Stun Duration`; durante el stun bloquea movimiento y entrada de hechizos.
- `Debug State` escribe en la consola las transiciones con patrón, acción, maná y motivo de selección.
- `State`, `CurrentPattern`, `CurrentAction`, `SelectionReason` y `RecoveryRemaining` están expuestos para lectura desde código y depuración.
- Al seleccionar el enemigo, un Gizmo muestra su distancia preferida.

## Configurar un enemigo en una escena

1. Crea la raíz del enemigo y añade un `CharacterController`.
2. Añade `EnemyCombatAI`, `SpellCaster`, `ManaSystem` y `HPSystem` si esa unidad usa el sistema actual de vida.
3. Crea un hijo `CastPoint`, oriéntalo hacia delante y asígnalo a `SpellCaster → Cast Point`.
4. Deja vacío `SpellCaster → Input Actions Asset` para un enemigo manejado por IA.
5. Añade a `SpellCaster → Equipped Spells` los hechizos que podrá utilizar.
6. Asigna el jugador y una personalidad en `EnemyCombatAI`. Si no asignas el jugador, la IA busca el tag `Player` al iniciar.
7. Añade los patrones deseados a `EnemyCombatAI → Patterns`.
8. Verifica `Ground Layers`, `Obstacle Layers` y que los colliders de suelo/pared estén en las capas correctas.
9. Activa `Debug State`, entra en Play Mode y comprueba la consola.

La escena `Assets/Scenes/EscenarioPrueba.unity` contiene el objeto `PruebaEnemy` con una configuración de referencia. Los componentes de IA deben estar activos y sus referencias seguir asignadas al editar la escena.

## Crear patrones y personalidades nuevos

### Nuevo patrón

1. Crea un asset desde **Create → Mana Zone → Enemy AI → Attack Pattern**.
2. Asigna un identificador y tipo.
3. Arrastra `SpellDefinition` a `Actions`, en orden.
4. Define rango, preparación, pausas, recuperación y peso.
5. Añade cada hechizo a `SpellCaster → Equipped Spells` de los enemigos que usarán el patrón.
6. Arrastra el asset a la lista `Patterns` de `EnemyCombatAI`.

### Nueva personalidad

1. Crea un asset desde **Create → Mana Zone → Enemy AI → Personality**.
2. Ajusta agresión, conservación de maná, distancia preferida y velocidades.
3. Define preferencias por categoría y frecuencia/duración de esquiva.
4. Asigna el asset a `EnemyCombatAI → Personality`.

Los ScriptableObjects son assets de configuración compartidos, no componentes de escena. Varios enemigos pueden referirse al mismo asset. Si necesitan balance distinto, duplica el asset y modifica la copia.

## Limitaciones conocidas

- Los contraataques no detectan ataques del jugador de forma automática. Un sensor futuro debe observar una señal real del juego y llamar a `ObservePlayerAttack()`; sin eso, `Counter` se rechaza.
- El tiempo de preparación es una ventana temporal; no hay animación o efecto visual de carga conectado por defecto.
- El desplazamiento evita bordes de suelo y paredes mediante consultas físicas locales, no mediante navegación global. Arenas complejas con obstáculos requieren una navegación más avanzada.
- `SpellableTarget.Stun()` actualmente registra el stun, pero no congela por sí mismo el movimiento de ese objeto. El jugador sí recibe el stun mediante `TPSController`, y la IA enemiga mediante `EnemyCombatAI`.
- Las escenas deben tener colliders para suelo y paredes. Un objeto visible sin collider no puede detectarse mediante Physics.
- La compilación se verificó con `dotnet build ManaZone.slnx --no-restore`. No se ha validado el combate en Play Mode en este entorno.

## Estructura relevante

```text
Assets/
├── EnemyAI/
│   ├── Personalities/       # Perfiles de ejemplo
│   └── PatronAtaque/        # Patrones de ejemplo
├── Scripts/
│   ├── Combat/              # Interfaces de combate
│   ├── Spells/              # SpellCaster y definiciones/proyectiles
│   ├── enemies/             # EnemyCombatAI y datos ScriptableObject
│   ├── HPSystem.cs
│   ├── ManaSystem.cs
│   └── TPSController.cs
└── Scenes/
    └── EscenarioPrueba.unity
```

## Desarrollo futuro

- Añadir sensores observables para contraataques y otras decisiones contextuales.
- Conectar señales visuales o animaciones a preparación, defensa y stun.
- Probar y balancear patrones, recursos y movimiento en Play Mode.
- Extender la navegación si las arenas dejan de ser espacios de combate relativamente abiertos.
