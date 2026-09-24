"""
analyze_results.py

Genera las 5 graficas minimas que pide el laboratorio a partir de
results/results_summary.csv (generado por la app de consola en C#).

Uso:
    pip install pandas matplotlib
    python analyze_results.py [carpeta_resultados]

Por defecto busca la carpeta "results".
"""

import sys
import os
import pandas as pd
import matplotlib.pyplot as plt

results_dir = sys.argv[1] if len(sys.argv) > 1 else "results"
csv_path = os.path.join(results_dir, "results_summary.csv")

if not os.path.exists(csv_path):
    print(f"No se encontro {csv_path}. Corre primero la app de consola (dotnet run).")
    sys.exit(1)

df = pd.read_csv(csv_path)
df = df.sort_values("Concurrency")

out_dir = os.path.join(results_dir, "graphs")
os.makedirs(out_dir, exist_ok=True)


def save(fig, name):
    path = os.path.join(out_dir, name)
    fig.savefig(path, bbox_inches="tight", dpi=150)
    print(f"Guardado: {path}")
    plt.close(fig)


# 1. Carga (concurrencia) vs tiempo de respuesta
fig, ax = plt.subplots()
ax.plot(df["Concurrency"], df["AvgLatencyMs"], marker="o", label="Promedio")
ax.plot(df["Concurrency"], df["P95LatencyMs"], marker="s", label="P95")
ax.plot(df["Concurrency"], df["MaxLatencyMs"], marker="^", label="Maximo")
ax.set_xlabel("Carga (usuarios concurrentes)")
ax.set_ylabel("Tiempo de respuesta (ms)")
ax.set_title("Carga vs Tiempo de respuesta")
ax.legend()
ax.grid(True, alpha=0.3)
save(fig, "1_carga_vs_tiempo_respuesta.png")

# 2. Carga vs tasa de exito
fig, ax = plt.subplots()
ax.plot(df["Concurrency"], df["SuccessRatePct"], marker="o", color="green")
ax.set_xlabel("Carga (usuarios concurrentes)")
ax.set_ylabel("Tasa de exito (%)")
ax.set_title("Carga vs Tasa de exito")
ax.set_ylim(0, 105)
ax.grid(True, alpha=0.3)
save(fig, "2_carga_vs_tasa_exito.png")

# 3. Carga vs numero de fallos
fig, ax = plt.subplots()
ax.bar(df["Concurrency"].astype(str), df["Failed"], color="firebrick")
ax.set_xlabel("Carga (usuarios concurrentes)")
ax.set_ylabel("Numero de solicitudes fallidas")
ax.set_title("Carga vs Fallos")
ax.grid(True, axis="y", alpha=0.3)
save(fig, "3_carga_vs_fallos.png")

# 4. Ronda vs uso de CPU
fig, ax = plt.subplots()
ax.plot(df["Round"], df["AvgCpuPercent"], marker="o", label="CPU promedio")
ax.plot(df["Round"], df["PeakCpuPercent"], marker="s", label="CPU pico")
ax.set_xlabel("Ronda de prueba")
ax.set_ylabel("Uso de CPU (%)")
ax.set_title("Ronda vs Uso de CPU")
ax.legend()
ax.grid(True, alpha=0.3)
save(fig, "4_ronda_vs_cpu.png")

# 5. Ronda vs uso de memoria
fig, ax = plt.subplots()
ax.plot(df["Round"], df["AvgMemoryMB"], marker="o", label="Memoria promedio (MB)")
ax.plot(df["Round"], df["PeakMemoryMB"], marker="s", label="Memoria pico (MB)")
ax.set_xlabel("Ronda de prueba")
ax.set_ylabel("Memoria (MB)")
ax.set_title("Ronda vs Uso de memoria")
ax.legend()
ax.grid(True, alpha=0.3)
save(fig, "5_ronda_vs_memoria.png")

# Bonus: carga vs throughput (requests/seg), util para justificar el punto de degradacion
fig, ax = plt.subplots()
ax.plot(df["Concurrency"], df["RequestsPerSec"], marker="o", color="purple")
ax.set_xlabel("Carga (usuarios concurrentes)")
ax.set_ylabel("Solicitudes / segundo")
ax.set_title("Carga vs Throughput")
ax.grid(True, alpha=0.3)
save(fig, "6_carga_vs_throughput.png")

print("\nListo. Revisa la carpeta:", out_dir)
