# Library Load Tester

Aplicación de consola en C# (.NET 10) para el laboratorio *"Controlled Load
Testing and Resilience Analysis of a .NET Web API"*, apuntada a tu API CRUD de
libros (`SimpleBooksApi`, endpoint `/api/books`).

⚠️ **Úsala solo contra tu API corriendo en `localhost`**, nunca contra
servicios públicos o de terceros (regla de seguridad del laboratorio).

## Qué hace

- Envía peticiones HTTP repetidas a una URL configurable.
- Ejecuta **rondas progresivas** con concurrencia creciente (5, 20, 50, 100,
  200 usuarios simultáneos por defecto), cada una por una duración fija.
- Por cada petición registra: latencia, éxito/fallo y código de estado.
- Muestrea en paralelo el **CPU y la memoria** del proceso de tu API
  (por nombre de proceso) mientras corre cada ronda.
- Al terminar, escribe dos CSV en `results/`:
  - `results_summary.csv`: una fila por ronda (lo que necesitas para las
    gráficas y la tabla de resultados del informe).
  - `raw_latencies.csv`: latencia de cada petición individual, por si quieres
    hacer histogramas u otros análisis.

## 1. Ejecuta tu API

En una terminal, dentro de tu proyecto `MiPrimerApi`:

```bash
dotnet run
```

Anota la URL y el puerto (por defecto en tu proyecto:
`http://localhost:5018`) y el nombre del ejecutable
(`MiPrimerApi`), que necesitas para el muestreo de CPU/RAM.

## 2. Ejecuta el load tester

En **otra** terminal, dentro de esta carpeta:

```bash
dotnet run -- --url http://localhost:5018/api/books --process MiPrimerApi
```

Con los valores por defecto corre 5 rondas: 5, 20, 50, 100 y 200 usuarios
concurrentes, 10 segundos cada una, con 5 segundos de descanso entre rondas.

### Parámetros configurables

| Parámetro     | Descripción                                                        | Ejemplo |
|---------------|---------------------------------------------------------------------|---------|
| `--url`       | Endpoint a probar                                                   | `http://localhost:5018/api/books` |
| `--method`    | Verbo HTTP (GET, POST, PUT, DELETE)                                  | `GET` |
| `--body`      | Cuerpo JSON (para POST/PUT)                                          | `"{\"title\":\"1984\",\"author\":\"Orwell\",\"year\":1949}"` |
| `--process`   | Nombre del proceso de la API (para medir CPU/RAM)                   | `MiPrimerApi` |
| `--rounds`    | Lista `concurrencia:duracionSeg` separada por comas                 | `5:10,20:10,50:10,100:10,200:10` |
| `--cooldown`  | Segundos de descanso entre rondas                                    | `5` |
| `--timeout`   | Timeout por petición en segundos                                    | `10` |
| `--output`    | Carpeta de salida de los CSV                                         | `results` |

Ejemplo probando el endpoint de un libro específico con más rondas:

```bash
dotnet run -- --url http://localhost:5018/api/books/1 --process MiPrimerApi \
  --rounds 10:10,50:10,100:10,150:10,250:10,400:10 --output results_get_by_id
```

> Nota: como `MiPrimerApi` usa una lista en memoria (no hay base de datos),
> las rondas de POST/PUT/DELETE modifican los datos reales de esa lista.
> Para GET es completamente seguro repetir; para POST/DELETE reinicia la API
> entre pruebas si necesitas partir de datos limpios.

## 3. Genera las gráficas

Necesitas Python con `pandas` y `matplotlib`:

```bash
pip install pandas matplotlib
python analyze_results.py results
```

Esto crea en `results/graphs/` las 5 gráficas mínimas que pide el
laboratorio (carga vs tiempo de respuesta, carga vs tasa de éxito, carga vs
fallos, ronda vs CPU, ronda vs memoria) más un bono de carga vs throughput.

## 4. Cómo identificar el punto de degradación

En `results_summary.csv`, busca la primera ronda donde ocurre alguna de estas
señales:

- `AvgLatencyMs` o `P95LatencyMs` crecen de forma marcada respecto a la ronda
  anterior (no lineal, "se dispara").
- `SuccessRatePct` empieza a bajar de 100%.
- `RequestsPerSec` deja de crecer o cae aunque la concurrencia sigue subiendo
  (el servicio ya no puede procesar más).
- `AvgCpuPercent` se satura cerca del 100% × núcleos disponibles.

Esa ronda es tu "punto de degradación" — repórtalo con los números exactos
de esa fila en la sección de Análisis del informe.

## Estructura del proyecto

```
LibraryLoadTester/
├── LibraryLoadTester.csproj
├── Program.cs          # punto de entrada, orquesta las rondas y escribe CSV
├── CliOptions.cs        # parseo de argumentos de línea de comandos
├── RoundConfig.cs        # configuración de una ronda (concurrencia, duración)
├── Models.cs            # RequestResult, ResourceSample, RoundResult
├── LoadEngine.cs         # motor que dispara las peticiones concurrentes
├── ResourceMonitor.cs    # muestreo de CPU/RAM del proceso de la API
├── analyze_results.py    # genera las gráficas requeridas
└── README.md
```
