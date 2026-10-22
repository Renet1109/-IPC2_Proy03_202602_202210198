#!/usr/bin/env python3
"""Genera los archivos de prueba del Proyecto 3 y calcula los resultados
esperados con un oraculo independiente (no usa el codigo del API)."""
import json, os, random, sys
from datetime import date, timedelta
from decimal import Decimal, ROUND_HALF_UP

import argparse
PERFILES = {  # dias, min_por_dia, max_por_dia, carga
    "corto":  (2, 2, 3, 100),
    "normal": (7, 4, 6, 2000),
    "largo":  (31, 15, 25, 20000),
}
ap = argparse.ArgumentParser(description="Genera archivos de prueba del Proyecto 3")
ap.add_argument("salida", nargs="?", default=None, help="carpeta de salida (default: pruebas_proyecto3_<perfil>)")
ap.add_argument("--perfil", choices=PERFILES, default="normal", help="preajuste de tamanos")
ap.add_argument("--dias", type=int, help="dias de marzo con facturas en el archivo 02 (1 a 31)")
ap.add_argument("--min-por-dia", type=int, help="minimo de facturas por dia en el archivo 02")
ap.add_argument("--max-por-dia", type=int, help="maximo de facturas por dia en el archivo 02")
ap.add_argument("--carga", type=int, help="cantidad de facturas del archivo 06 (carga de volumen)")
ap.add_argument("--semilla", type=int, default=2026, help="semilla del generador aleatorio")
A = ap.parse_args()
P = PERFILES[A.perfil]
DIAS = A.dias if A.dias is not None else P[0]
MIN_DIA = A.min_por_dia if A.min_por_dia is not None else P[1]
MAX_DIA = A.max_por_dia if A.max_por_dia is not None else P[2]
CARGA = A.carga if A.carga is not None else P[3]
if not 1 <= DIAS <= 31: ap.error("--dias debe estar entre 1 y 31")
if MIN_DIA < 1 or MAX_DIA < MIN_DIA: ap.error("--min-por-dia >= 1 y --max-por-dia >= --min-por-dia")
if CARGA < 1: ap.error("--carga debe ser >= 1")
OUT = A.salida or f"pruebas_proyecto3_{A.perfil}"
os.makedirs(OUT, exist_ok=True)

D = Decimal

# ---------- utilidades ----------
def ver(cuerpo):
    s = sum(int(d) * (i + 2) for i, d in enumerate(reversed(cuerpo)))
    r = (11 - (s % 11)) % 11
    return "K" if r == 10 else str(r)

def nit(cuerpo): return cuerpo + ver(cuerpo)

def nit_valido(n):
    n = n.strip().upper()
    if not (2 <= len(n) <= 21): return False
    if not n[:-1].isdigit() or not (n[-1].isdigit() or n[-1] == "K"): return False
    return ver(n[:-1]) == n[-1]

def norm(n): return n.strip().upper()
def iva_de(v): return (v * D("0.12")).quantize(D("0.01"), ROUND_HALF_UP)
def m(d): return f"{d:.2f}"
def js(x): return json.dumps(x, ensure_ascii=False)

def tiempo(f, hh=10, mm=0, lugar="Guatemala"):
    return f"{lugar}, {f:%d/%m/%Y} {hh:02d}:{mm:02d} hrs."

class F:
    def __init__(s, ref, em, re_, valor, fecha, iva=None, total=None, texto=None):
        s.ref, s.em, s.re = ref, em, re_
        s.valor = D(valor)
        s.iva = iva_de(s.valor) if iva is None else D(iva)
        s.total = s.valor + s.iva if total is None else D(total)
        s.fecha = fecha
        s.texto = texto if texto is not None else tiempo(fecha)

def escribir_facturas(nombre, items):
    filas = []
    for f in items:
        filas.append('  {"TIEMPO": %s, "REFERENCIA": %s, "NIT_EMISOR": %s, "NIT_RECEPTOR": %s, '
                     '"VALOR": %s, "IVA": %s, "TOTAL": %s}' %
                     (js(f.texto), js(f.ref), js(f.em), js(f.re), m(f.valor), m(f.iva), m(f.total)))
    with open(os.path.join(OUT, nombre), "w", encoding="utf-8") as fh:
        fh.write("[\n" + ",\n".join(filas) + "\n]\n")

def escribir_texto(nombre, texto):
    with open(os.path.join(OUT, nombre), "w", encoding="utf-8") as fh:
        fh.write(texto)

# ---------- oraculo ----------
class Sistema:
    def __init__(s): s.rtu, s.refs, s.facts, s.corr = {}, set(), [], 0
    def rtu_procesar(s, items):
        n = a = i = 0
        for c, nombre in items:
            if not nit_valido(c): i += 1; continue
            c = norm(c)
            if c in s.rtu: a += 1
            else: n += 1
            s.rtu[c] = nombre
        return n, a, i
    def facturas_procesar(s, items):
        r = dict(aprob=0, emi_inex=0, rec_inex=0, emi_inv=0, rec_inv=0, iva=0,
                 total=0, dup=0, rech=0, desc=0, codigos=[])
        for f in items:
            if f.fecha is None: r["desc"] += 1; continue
            e = set()
            em, re_ = norm(f.em), norm(f.re)
            if not nit_valido(em): e.add("emi_inv")
            elif em not in s.rtu: e.add("emi_inex")
            if not nit_valido(re_): e.add("rec_inv")
            elif re_ not in s.rtu: e.add("rec_inex")
            if f.iva != iva_de(f.valor): e.add("iva")
            if f.total != f.valor + f.iva: e.add("total")
            if f.ref in s.refs: e.add("dup")
            s.refs.add(f.ref)
            for k in e: r[k] += 1
            if e: r["rech"] += 1
            else:
                s.corr += 1; r["aprob"] += 1; r["codigos"].append(s.corr)
            s.facts.append(dict(f=f, em=em, re=re_, err=e, ok=not e))
        return r
    def en_rango(s, ini, fin): return [x for x in s.facts if ini <= x["f"].fecha <= fin]

# ---------- 01 RTU ----------
cuerpo20 = "12345678901234567890"
nit20 = nit(cuerpo20)
NITS = {
    "7378106": "Empresa ABC, S.A.", "8338817": "Comercial XYZ, S.A.",
    "5123453": "Servicios Integrales del Norte", "1000004": "Papelera Central",
    "999999K": "Distribuidora El Quetzal", "1234560": "Ferretería La Esquina",
    "6K": "Tienda Mínima", "24680133": "Transportes Atitlán",
    nit20: "Corporación de Veinte Dígitos",
}
assert all(nit_valido(k) for k in NITS), "algun NIT de la lista no es valido"
rtu = [(k, v) for k, v in NITS.items()]
rtu += [("5123456", "NIT con verificador malo"), ("12A4", "NIT con letras"),
        ("", "NIT vacío"), ("7378106", "Empresa ABC, S.A. (razón social actualizada)")]
escribir_texto("01_rtu_completo.json", "[\n" + ",\n".join(
    '  {"NIT": %s, "nombre": %s}' % (js(a), js(b)) for a, b in rtu) + "\n]\n")

sis = Sistema()
rtu_res = sis.rtu_procesar(rtu)

# ---------- 02 facturas para graficas (marzo 2025) ----------
random.seed(A.semilla)
par = ["7378106", "8338817", "5123453", "1000004", "999999K", "1234560", "24680133"]
g = []
cont = 0
inicio = date(2025, 3, 1)
for d in range(DIAS):
    dia = inicio + timedelta(days=d)
    for _ in range(random.randint(MIN_DIA, MAX_DIA)):
        cont += 1
        e, r = random.sample(par, 2)
        g.append(F(f"G{cont:05d}", e, r, D(random.randint(1000, 500000)) / 100, dia))
escribir_facturas("02_facturas_marzo_graficas.json", g)
res02 = sis.facturas_procesar(g)
snap_ini, snap_fin = date(2025, 3, 1), date(2025, 3, DIAS)

# ---------- 03 todos los errores (abril 2025) ----------
d1 = lambda n: date(2025, 4, n)
no_exist_a, no_exist_b = nit("450987"), nit("777777")
assert nit_valido(no_exist_a) and nit_valido(no_exist_b) and no_exist_a not in NITS and no_exist_b not in NITS
iva_mal = lambda v: iva_de(D(v)) + D("1.00")
e = [
    F("E01", "7378106", "8338817", "1500.00", d1(1)),
    F("E02", "8338817", "5123453", "75.50", d1(1)),
    F("E03", no_exist_a, "8338817", "100.00", d1(2)),
    F("E04", "7378106", no_exist_b, "100.00", d1(2)),
    F("E05", "5123456", "8338817", "100.00", d1(3)),
    F("E06", "7378106", "12A4", "100.00", d1(3)),
    F("E07", "7378106", "8338817", "100.00", d1(4), iva=iva_mal("100.00"), total=D("100.00") + iva_mal("100.00")),
    F("E08", "7378106", "8338817", "100.00", d1(4), total="112.01"),
    F("E09", "7378106", "8338817", "100.00", d1(5)),
    F("E10", "7378106", "8338817", "100.00", d1(5)),
    F("E11", "5123456", "8338817", "200.00", d1(6), iva="30.00", total="235.00"),
    F("E12", no_exist_a, no_exist_b, "100.00", d1(6)),
    F("E13", "7378106", "8338817", "100.00", None, texto="sin fecha"),
    F("E14", "1000004", "7378106", "10.05", d1(7)),
]
e[8].ref = g[0].ref     # E09: referencia que ya existe en la BD (archivo 02)
e[9].ref = "E01"        # E10: E01 repetida dentro del mismo mensaje
escribir_facturas("03_facturas_todos_los_errores.json", e)
res03 = sis.facturas_procesar(e)

# ---------- 04 bordes de formato ----------
v = D("250.00")
f4 = [
    F("F01", "7378106", "8338817", "100.00", date(2025, 6, 5), texto="Guatemala, 05/06/2025 08:05 hrs."),
    F("F02", "7378106", "8338817", "100.00", date(2025, 6, 5), texto="Ciudad de Guatemala, 5/6/2025 9:05"),
    F("F03", "7378106", "8338817", "100.00", date(2025, 7, 1), texto="01/07/2025 00:00"),
    F("F04", "7378106", "8338817", "100.00", date(2025, 12, 31), texto="Guatemala, 31/12/2025 23:59 hrs."),
    F("F05", "7378106", "8338817", "100.00", date(2024, 2, 29), texto="Antigua Guatemala, 29/02/2024 10:00 hrs."),
    F("F06", "7378106", "8338817", "100.00", None, texto="Guatemala, 29/02/2025 10:00 hrs."),
    F("F07", "7378106", "8338817", "100.00", None, texto="Guatemala, 15/03/2025"),
    F("F08", "7378106", "8338817", "0.04", date(2025, 8, 15)),
    F("F09", "7378106", "8338817", "99999999.99", date(2025, 8, 15)),
    F("F10", "6K", "7378106", "250.00", date(2025, 8, 15)),
    F("F11", nit20, "7378106", "250.00", date(2025, 8, 15)),
    F("F12", "7378106", "8338817", "100.00", date(2025, 8, 15)),
    F("F13", "7378106", "6k", "100.00", date(2025, 8, 15)),
]
f4[11].ref = "F12-" + "x" * 36     # 40 caracteres exactos
assert len(f4[11].ref) == 40
escribir_facturas("04_facturas_bordes_formato.json", f4)
res04 = sis.facturas_procesar(f4)

# ---------- 05 JSON invalidos ----------
escribir_texto("05a_json_malformado.json",
               '[{"TIEMPO": "Guatemala, 01/01/2025 10:00 hrs.", "REFERENCIA": "X1", "NIT_EMISOR": "7378106",\n')
escribir_texto("05b_arreglo_vacio.json", "[]\n")
escribir_texto("05c_objeto_suelto.json", '{"NIT": "7378106", "nombre": "No soy un arreglo"}\n')
escribir_texto("05d_sin_llaves_conocidas.json", '[{"foo": 1}, {"bar": 2}]\n')

# ---------- 06 carga de volumen ----------
c = []
for i in range(1, CARGA + 1):
    c.append(F(f"L{i:06d}", "7378106", "8338817", "100.00", date(2025, 5, 1) + timedelta(days=(i - 1) % 20)))
ARCH06 = f"06_carga_{CARGA}_facturas.json"
escribir_facturas(ARCH06, c)
res06 = sis.facturas_procesar(c)

# ---------- esperados de consultas (estado tras 01 y 02) ----------
def rango_dias(ini, fin):
    d = ini
    while d <= fin: yield d; d += timedelta(days=1)

def consulta(nit_c, tipo):
    filas, tf, tm = [], 0, D("0")
    for d in rango_dias(snap_ini, snap_fin):
        xs = [x for x in sis.facts if x["f"].fecha == d and x["ok"] and (x["em"] if tipo == "emitidas" else x["re"]) == nit_c]
        if xs:
            s_ = sum((x["f"].total for x in xs), D("0"))
            filas.append((d, len(xs), s_)); tf += len(xs); tm += s_
    return filas, tf, tm

def resumen_iva(d):
    acc = {}
    for x in sis.facts:
        if x["f"].fecha == d and x["ok"]:
            acc.setdefault(x["em"], [D("0"), D("0")])[0] += x["f"].iva
            acc.setdefault(x["re"], [D("0"), D("0")])[1] += x["f"].iva
    return sorted(acc.items())

def resumen_rango(tipo):
    filas, tg = [], D("0")
    for d in rango_dias(snap_ini, snap_fin):
        xs = [x for x in sis.facts if x["f"].fecha == d and x["ok"]]
        if xs:
            s_ = sum((x["f"].total if tipo == "total" else x["f"].valor for x in xs), D("0"))
            filas.append((d, s_)); tg += s_
    return filas, tg

def reporte(ini, fin):
    out = []
    for d in rango_dias(ini, fin):
        xs = [x for x in sis.facts if x["f"].fecha == d]
        if not xs: continue
        cnt = lambda k: sum(1 for x in xs if k in x["err"])
        out.append((d, len(xs), cnt("emi_inv"), cnt("rec_inv"), cnt("emi_inex"), cnt("rec_inex"),
                    cnt("iva"), cnt("total"), cnt("dup"), sum(1 for x in xs if x["ok"]),
                    len({x["em"] for x in xs}), len({x["re"] for x in xs})))
    return out

from collections import Counter
_emit = Counter(x["em"] for x in sis.facts if x["ok"] and snap_ini <= x["f"].fecha <= snap_fin)
NIT_A = sorted(_emit.items(), key=lambda kv: (-kv[1], kv[0]))[0][0]  # NIT con mas facturas emitidas
em_f, em_tf, em_tm = consulta(NIT_A, "emitidas")
re_f, re_tf, re_tm = consulta(NIT_A, "recibidas")
dia_iva = date(2025, 3, min(3, DIAS))
iva_f = resumen_iva(dia_iva)
tot_f, tot_g = resumen_rango("total")
sin_f, sin_g = resumen_rango("sinIva")
rep_abril = reporte(date(2025, 4, 1), date(2025, 4, 7))

# ---------- LEEME ----------
fd = lambda d: f"{d:%d/%m/%Y}"
L = []
L.append("# Archivos de prueba — Proyecto 3 (SAT / DTE)\n")
L.append("Todos los valores esperados de abajo los calculó `generar_pruebas.py` con un oráculo independiente "
         "(no usa el código del API). Si cambias los archivos, vuelve a correr el script y regenera este documento.\n")
L.append("## Orden de carga\n")
L.append("| # | Archivo | Qué prueba |\n|---|---|---|")
L.append("| 0 | `POST /Inicializar` | Estado limpio (o el botón Inicializar de la Web) |")
L.append("| 1 | `01_rtu_completo.json` | RTU con NITs válidos (incluye K y 21 caracteres), inválidos, vacío y una actualización |")
L.append("| 2 | `02_facturas_marzo_graficas.json` | %d facturas válidas en %d día(s) de marzo 2025, para tablas y gráficas |" % (len(g), DIAS))
L.append("| 3 | `03_facturas_todos_los_errores.json` | Un caso por cada tipo de error, un multi-error y un descarte (requiere 1 y 2) |")
L.append("| 4 | `04_facturas_bordes_formato.json` | Formatos de TIEMPO, decimales extremos, NIT con K y de 21 caracteres |")
L.append("| 5 | `05a` a `05d` | JSON inválidos: el API responde 400 `{ \"error\": ... }` y la Web muestra el mensaje |")
L.append("| 6 | `%s` | Carga de volumen (%d facturas) |" % (ARCH06, CARGA))
L.append("\nLas fechas de cada archivo están separadas (marzo, abril, mayo, junio a diciembre) para que los números de una "
         "consulta no cambien al cargar los demás archivos.\n")
L.append("## Cargar con curl\n")
L.append("```bash\nH='Content-Type: application/json'\ncurl -s -X POST localhost:5001/Inicializar\n"
         "for f in 01_rtu_completo 02_facturas_marzo_graficas 03_facturas_todos_los_errores 04_facturas_bordes_formato; do\n"
         "  echo \"== $f\"; curl -s -X POST localhost:5001/Procesar -H \"$H\" --data-binary @$f.json; echo\ndone\n"
         "# carga de volumen (opcional):\ncurl -s -X POST localhost:5001/Procesar -H \"$H\" --data-binary @%s\n"
         "# codigos de hoy en Guatemala (UTC-6) en macOS:\ndate -u -v-6H +%%Y%%m%%d\n```\n" % ARCH06)
L.append("En la Web: **Enviar mensaje**, selecciona varios archivos a la vez (se procesan en el orden en que los elijas; "
         "pon el 01 antes que las facturas).\n")
L.append("## Resultados esperados\n")
L.append("`HOY` = fecha de hoy en Guatemala en formato `yyyymmdd`. Los códigos son `HOY` + correlativo de 8 dígitos.\n")
L.append("### 01 — RTU\n")
L.append("`CONTRIBUYENTES_NUEVOS`: **%d** · `CONTRIBUYENTES_ACTUALIZADOS`: **%d** · `NITS_INVALIDOS`: **%d**\n" % rtu_res)
L.append("(7378106 aparece dos veces: la segunda cuenta como actualizado; `5123456`, `12A4` y el vacío son inválidos.)\n")
L.append("### 02 — Facturas de marzo\n")
L.append("Aprobadas: **%d** (códigos `HOY00000001` a `HOY%08d`), sin rechazos ni descartes.\n" % (res02["aprob"], res02["aprob"]))
L.append("### 03 — Todos los errores\n")
c1 = res02["aprob"] + 1
L.append("| Campo de la respuesta | Esperado |\n|---|---|")
L.append("| `cantidadAutorizacionesAprobadas` | %d (E01, E02, E14; códigos `HOY%08d` a `HOY%08d`) |" % (res03["aprob"], c1, c1 + res03["aprob"] - 1))
L.append("| `...RechazadasPorNitEmisorInexistente` | %d (E03, E12) |" % res03["emi_inex"])
L.append("| `...RechazadasPorNitReceptorInexistente` | %d (E04, E12) |" % res03["rec_inex"])
L.append("| `...RechazadasPorNitEmisorInvalido` | %d (E05, E11) |" % res03["emi_inv"])
L.append("| `...RechazadasPorNitReceptorInvalido` | %d (E06) |" % res03["rec_inv"])
L.append("| `...RechazadasPorIvaMalCalculado` | %d (E07, E11) |" % res03["iva"])
L.append("| `...RechazadasPorTotalMalCalculado` | %d (E08, E11) |" % res03["total"])
L.append("| `...RechazadasPorReferenciaDuplicada` | %d (E09 repite G0001 de la BD; E10 repite E01 del mismo mensaje) |" % res03["dup"])
L.append("| `cantidadAutorizacionesRechazadas` | %d (facturas distintas; E11 y E12 cuentan una sola vez) |" % res03["rech"])
L.append("| `cantidadDescartadasPorFormato` | %d (E13, TIEMPO sin fecha) |\n" % res03["desc"])
L.append("### 04 — Bordes de formato\n")
L.append("Aprobadas: **%d** · descartadas por formato: **%d** · rechazadas: **%d**\n" % (res04["aprob"], res04["desc"], res04["rech"]))
L.append("| Ref | Caso | Esperado |\n|---|---|---|")
casos = [("F01", "`Guatemala, 05/06/2025 08:05 hrs.`", "aprobada"),
         ("F02", "`Ciudad de Guatemala, 5/6/2025 9:05` (un dígito, sin «hrs.»)", "aprobada"),
         ("F03", "`01/07/2025 00:00` (sin lugar, medianoche)", "aprobada"),
         ("F04", "`31/12/2025 23:59`", "aprobada"),
         ("F05", "`29/02/2024` (año bisiesto)", "aprobada"),
         ("F06", "`29/02/2025` (fecha inexistente)", "descartada"),
         ("F07", "fecha sin hora", "descartada"),
         ("F08", "VALOR 0.04 → IVA 0.00", "aprobada"),
         ("F09", "VALOR 99999999.99 → IVA 12000000.00, TOTAL 111999999.99", "aprobada"),
         ("F10", "emisor `6K` (verificador K)", "aprobada"),
         ("F11", "emisor de 21 caracteres (`%s`)" % nit20, "aprobada"),
         ("F12", "REFERENCIA de exactamente 40 caracteres", "aprobada"),
         ("F13", "receptor en minúscula `6k`", "aprobada (se normaliza a `6K`); si sale rechazada, es un hallazgo")]
for a, b, c_ in casos: L.append(f"| {a} | {b} | {c_} |")
L.append("\n### 05 — JSON inválidos\n")
L.append("Los cuatro archivos deben dar **HTTP 400** con `{ \"error\": \"...\" }` en el API, y la Web debe mostrar el mensaje "
         "sin error 500.\n")
L.append("### 06 — Carga de volumen\n")
L.append("Aprobadas: **%d**, códigos contiguos y distintos.\n" % res06["aprob"])
L.append("## Consultas esperadas (tras cargar 01 y 02)\n")
L.append("Rango: **%s a %s**. Estos números no cambian al cargar los archivos 03 a 06.\n" % (fd(snap_ini), fd(snap_fin)))
L.append("### `ConsultaDatos` NIT %s, emitidas\n" % NIT_A)
L.append("| Fecha | Facturas | Monto |\n|---|---|---|")
for d, n, s_ in em_f: L.append(f"| {fd(d)} | {n} | {m(s_)} |")
L.append(f"| **Total** | **{em_tf}** | **{m(em_tm)}** |\n")
L.append("### `ConsultaDatos` NIT %s, recibidas\n" % NIT_A)
L.append("| Fecha | Facturas | Monto |\n|---|---|---|")
for d, n, s_ in re_f: L.append(f"| {fd(d)} | {n} | {m(s_)} |")
L.append(f"| **Total** | **{re_tf}** | **{m(re_tm)}** |\n")
L.append("### `ResumenIva` fecha %s\n" % fd(dia_iva))
L.append("| NIT | IVA emitido | IVA recibido |\n|---|---|---|")
for k, (a, b) in iva_f: L.append(f"| {k} | {m(a)} | {m(b)} |")
L.append("\n### `ResumenRango` %s a %s\n" % (fd(snap_ini), fd(snap_fin)))
L.append("| Fecha | Total | Sin IVA |\n|---|---|---|")
for (d, a), (_, b) in zip(tot_f, sin_f): L.append(f"| {fd(d)} | {m(a)} | {m(b)} |")
L.append(f"| **Total general** | **{m(tot_g)}** | **{m(sin_g)}** |\n")
L.append("### `ReporteErrores` 01/04/2025 a 07/04/2025 (tras cargar 03)\n")
L.append("| Fecha | Recibidas | Emisor inválido | Receptor inválido | Emisor inexistente | Receptor inexistente | IVA mal | TOTAL mal | Ref. duplicada | Sin errores | Emisores | Receptores |")
L.append("|---|---|---|---|---|---|---|---|---|---|---|---|")
for r in rep_abril: L.append("| " + fd(r[0]) + " | " + " | ".join(str(x) for x in r[1:]) + " |")
L.append("\n## Pruebas manuales en la Web\n")
L.append("1. **Enviar mensaje**: elige `01` y `02` a la vez y mira la vista previa (tipo detectado, cantidad, primeras filas); envía.\n"
         "2. Repite con `05a` a `05d`: mensaje de error, nunca pantalla de error del servidor.\n"
         "3. **Peticiones → Consultar contribuyente**: `%s`, emitidas y recibidas, `2025-03-01` a `2025-03-%02d`.\n"
         "4. **Resumen de IVA** del `2025-03-%02d` y **Resumen por rango** con Total y Sin IVA; abre las gráficas y descarga los PDF.\n"
         "5. **Reporte de errores** del `2025-04-01` al `2025-04-07` después de cargar `03`.\n"
         "6. **Inicializar** y comprueba que las consultas vuelven a salir vacías.\n"
         % (NIT_A, DIAS, dia_iva.day))
L.append("## Regenerar\n```bash\npython3 generar_pruebas.py ./%s --perfil %s --dias %d --min-por-dia %d --max-por-dia %d --carga %d --semilla %d\n```\n"
         % (os.path.basename(os.path.normpath(OUT)), A.perfil, DIAS, MIN_DIA, MAX_DIA, CARGA, A.semilla))
escribir_texto("LEEME.md", "\n".join(L))

# verificacion de que cargas posteriores no alteran lo esperado de marzo
assert consulta(NIT_A, "emitidas")[1:] == (em_tf, em_tm)
assert resumen_rango("total")[1] == tot_g
print("RTU", rtu_res, "| 02", res02["aprob"], "| 03", {k: v for k, v in res03.items() if k != "codigos"},
      "| 04", res04["aprob"], res04["desc"], res04["rech"], "| 06", res06["aprob"])
