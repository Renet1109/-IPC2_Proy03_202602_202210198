"""Pruebas HTTP aisladas. Python 3.10+, sin paquetes externos; compilar antes.

Inicia una API en un puerto libre y un archivo nuevo. No toca la API del usuario.
"""
import argparse
import concurrent.futures
import datetime as dt
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from decimal import Decimal

RAIZ = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--configuracion', default='Release', choices=['Debug', 'Release'])
parser.add_argument('--salida', type=Path, default=RAIZ / 'pruebas' / 'App_Data' / 'http')
parser.add_argument('--ejemplos-pdf', type=Path)
parser.add_argument('--api', type=Path, help='DLL de una API publicada para comprobar el paquete compilado.')
args = parser.parse_args()
args.salida.mkdir(parents=True, exist_ok=True)
ejecucion = args.salida / uuid.uuid4().hex
ejecucion.mkdir()
dll = args.api.resolve() if args.api else RAIZ / 'src' / 'SatDte.Api' / 'bin' / args.configuracion / 'net10.0' / 'SatDte.Api.dll'
if not dll.is_file():
    raise SystemExit('Compila primero: ./Compilar.ps1')
dotnet = shutil.which('dotnet')
with socket.socket() as s:
    s.bind(('127.0.0.1', 0))
    puerto = s.getsockname()[1]
BASE = f'http://127.0.0.1:{puerto}'
env = dict(os.environ, Datos__Archivo=str(ejecucion / 'estado.json'), Logging__LogLevel__Default='Warning')
log = (ejecucion / 'api.log').open('w', encoding='utf-8')
proceso = None
resultados = []

def iniciar():
    global proceso
    proceso = subprocess.Popen([dotnet, str(dll), '--urls', BASE], cwd=dll.parent if args.api else dll.parents[3], env=env,
        stdout=log, stderr=subprocess.STDOUT, creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
    for _ in range(150):
        if proceso.poll() is not None:
            raise RuntimeError('La API se detuvo: ' + (ejecucion / 'api.log').read_text(encoding='utf-8'))
        try:
            with urllib.request.urlopen(BASE + '/salud', timeout=.5) as r:
                if r.status == 200: return
        except (OSError, urllib.error.URLError): time.sleep(.1)
    raise TimeoutError('La API no inició en 15 segundos.')

def detener():
    if proceso and proceso.poll() is None:
        proceso.terminate()
        try: proceso.wait(timeout=5)
        except subprocess.TimeoutExpired: proceso.kill(); proceso.wait(timeout=5)

def http(ruta, cuerpo=None, metodo=None, contenido='application/json'):
    if cuerpo is not None and not isinstance(cuerpo, bytes):
        cuerpo = json.dumps(cuerpo).encode()
    solicitud = urllib.request.Request(BASE + ruta, data=cuerpo, method=metodo,
                                      headers={'Content-Type': contenido})
    try: respuesta = urllib.request.urlopen(solicitud, timeout=30)
    except urllib.error.HTTPError as error: respuesta = error
    with respuesta:
        return respuesta.code, respuesta.read(), respuesta.headers

def jget(ruta):
    status, raw, _ = http(ruta)
    assert status == 200, (status, raw)
    return json.loads(raw, parse_float=Decimal)

def cargar(nombre):
    status, raw, _ = http('/Procesar', (RAIZ / 'pruebas' / 'entrada' / nombre).read_bytes())
    assert status == 200, (status, raw)
    return json.loads(raw)

def prueba(nombre, accion):
    try:
        accion()
        resultados.append({'prueba': nombre, 'resultado': 'OK'})
        print('OK  ', nombre, flush=True)
    except Exception as error:
        resultados.append({'prueba': nombre, 'resultado': 'FAIL', 'detalle': str(error)})
        print('FAIL', nombre, str(error), flush=True)

def igual(a, b): assert a == b, (a, b)
def factura(ref):
    return [{'TIEMPO':'Guatemala, 01/03/2025 10:00 hrs.', 'REFERENCIA':ref,
             'NIT_EMISOR':'7378106', 'NIT_RECEPTOR':'8338817', 'VALOR':100, 'IVA':12, 'TOTAL':112}]
def aprobacion(raw): return json.loads(raw)['resultadoAutorizaciones']['detalleAprobaciones'][0]['codigoAprobacion']
def q(ruta, **parametros): return ruta + '?' + urllib.parse.urlencode(parametros)

try:
    iniciar()
    prueba('Inicializar por HTTP', lambda: igual(200, http('/Inicializar', metodo='POST')[0]))
    prueba('RTU: nombres exactos de claves y 9/1/3', lambda: igual(
        {'CONTRIBUYENTES_NUEVOS':9,'CONTRIBUYENTES_ACTUALIZADOS':1,'NITS_INVALIDOS':3}, cargar('01_rtu_completo.json')))
    prueba('Marzo: 4 autorizaciones', lambda: igual(4, cargar('02_facturas_marzo_graficas.json')['resultadoAutorizaciones']['cantidadAutorizacionesAprobadas']))
    prueba('Consulta emitidas Q 9672.32', lambda: igual(Decimal('9672.32'), jget(q('/ConsultaDatos',nit='999999K',tipo='emitidas',inicio='2025-03-01',fin='2025-03-02'))['totalFacturado']))
    prueba('Consulta recibidas Q 9258.06', lambda: igual(Decimal('9258.06'), jget(q('/ConsultaDatos',nit='999999K',tipo='recibidas',inicio='2025-03-01',fin='2025-03-02'))['totalFacturado']))
    prueba('IVA por NIT 494.06 / 392.20', lambda: igual(
        (Decimal('494.06'), Decimal('392.20')),
        next((m['ivaEmitido'],m['ivaRecibido']) for m in jget('/ResumenIva?fecha=2025-03-02')['movimientos'] if m['nit']=='999999K')))
    for siniva, total in [('false', '18930.38'), ('true', '16902.13')]:
        prueba('Rango sinIva=' + siniva, lambda s=siniva,t=total: igual(Decimal(t), jget(q('/ResumenRango',inicio='2025-03-01',fin='2025-03-02',sinIva=s))['totalGeneral']))
    filtros = {'nit':'999999K','tipo':'emitidas','inicio':'2025-03-01','fin':'2025-03-02','fecha':'2025-03-02'}
    def pdf(tipo):
        estado, raw, cabecera = http(q('/Reportes/' + tipo + '.pdf', **filtros))
        igual(200, estado); assert raw.startswith(b'%PDF-1.4'); igual('application/pdf', cabecera.get_content_type())
        if args.ejemplos_pdf:
            args.ejemplos_pdf.mkdir(parents=True, exist_ok=True)
            (args.ejemplos_pdf / ('reporte-' + tipo + '.pdf')).write_bytes(raw)
    for tipo in ['contribuyente','iva','rango']:
        prueba('PDF ' + tipo, lambda t=tipo: pdf(t))
    for tipo in ['iva','rango']:
        def grafica(t=tipo):
            status, raw, cabecera = http(q('/Grafica',tipo=t,inicio='2025-03-01',fin='2025-03-02',fecha='2025-03-02'))
            igual(200,status); igual('image/svg+xml',cabecera.get_content_type()); assert b'<svg ' in raw
        prueba('SVG ' + tipo, grafica)
    def errores():
        r = cargar('03_facturas_todos_los_errores.json')['resultadoAutorizaciones']
        igual((3,10,1), tuple(r[c] for c in ['cantidadAutorizacionesAprobadas','cantidadAutorizacionesRechazadas','cantidadDescartadasPorFormato']))
        esperados = {'NitEmisorInexistente':2,'NitReceptorInexistente':2,'NitEmisorInvalido':2,
                     'NitReceptorInvalido':1,'IvaMalCalculado':2,'TotalMalCalculado':2,'ReferenciaDuplicada':2}
        for k,v in esperados.items(): igual(v,r['cantidadAutorizacionesRechazadasPor'+k])
    prueba('Todos los errores: respuesta completa', errores)
    def matriz_errores():
        r = jget('/ReporteErrores?inicio=2025-04-01&fin=2025-04-07')
        campos = ['facturasRecibidas','nitEmisorInvalido','nitReceptorInvalido','nitEmisorInexistente','nitReceptorInexistente','ivaMalCalculado','totalMalCalculado','referenciaDuplicada','facturasSinErrores','emisores','receptores']
        esperados = [[2,0,0,0,0,0,0,0,2,2,2],[2,0,0,1,1,0,0,0,0,2,2],[2,1,1,0,0,0,0,0,0,2,2],
            [2,0,0,0,0,1,1,0,0,1,1],[2,0,0,0,0,0,0,2,0,1,1],[2,1,0,1,1,1,1,0,0,2,2],[1,0,0,0,0,0,0,0,1,1,1]]
        igual(esperados, [[d[c] for c in campos] for d in r['dias']])
    prueba('Reporte errores: matriz completa del LEEME', matriz_errores)
    filtros.update(inicio='2025-04-01',fin='2025-04-07')
    prueba('PDF errores',lambda: pdf('errores'))
    def bordes():
        r = cargar('04_facturas_bordes_formato.json')['resultadoAutorizaciones']
        igual((11,0,2),tuple(r[c] for c in ['cantidadAutorizacionesAprobadas','cantidadAutorizacionesRechazadas','cantidadDescartadasPorFormato']))
    prueba('Bordes: 11 aprobadas, 0 rechazadas, 2 descartadas', bordes)
    for archivo in ['05a_json_malformado.json','05b_arreglo_vacio.json','05c_objeto_suelto.json','05d_sin_llaves_conocidas.json']:
        def invalido(a=archivo):
            antes = jget('/Estado')
            status, raw, _ = http('/Procesar',(RAIZ/'pruebas'/'entrada'/a).read_bytes())
            igual(400,status); assert 'error' in json.loads(raw); igual(antes,jget('/Estado'))
        prueba('HTTP 400 sin mutación: ' + archivo, invalido)
    prueba('Volumen: 100 facturas', lambda: igual(100,cargar('06_carga_100_facturas.json')['resultadoAutorizaciones']['cantidadAutorizacionesAprobadas']))
    for ruta in ['/ResumenRango?inicio=2025-04-01&fin=2025-03-01','/ResumenIva?fecha=ayer',
        '/ConsultaDatos?nit=12A4&inicio=2025-03-01&fin=2025-03-02','/ResumenRango?inicio=2025-03-01&fin=2025-03-02&sinIva=quizas']:
        prueba('Parámetro inválido: ' + ruta, lambda r=ruta: igual(400,http(r)[0]))
    prueba('Content-Type incorrecto: HTTP 415', lambda: igual(415,http('/Procesar',b'[]',contenido='text/plain')[0]))
    def concurrencia():
        def enviar(i):
            status, raw, _ = http('/Procesar',factura('HTTP-CONC-' + str(i)))
            igual(200,status); return aprobacion(raw)
        with concurrent.futures.ThreadPoolExecutor(max_workers=12) as pool: codigos = sorted(pool.map(enviar,range(24)))
        igual(24,len(set(codigos)))
        for a,b in zip(codigos,codigos[1:]): igual(a+1,b)
    prueba('Concurrencia HTTP: 24 códigos únicos y contiguos',concurrencia)
    def reinicio():
        antes = jget('/Estado'); detener(); iniciar(); igual(antes,jget('/Estado'))
        status,raw,_ = http('/Procesar',factura('HTTP-REINICIO')); igual(200,status)
        igual(143,int(str(aprobacion(raw))[-8:]))
    prueba('Reinicio real del proceso y continuidad del correlativo',reinicio)
    def reset():
        igual(200,http('/Inicializar',metodo='POST')[0])
        assert all(v == 0 for v in jget('/Estado').values())
        igual(0,len(jget('/ResumenIva?fecha=2025-03-02')['movimientos']))
        igual(0,len(jget('/ResumenRango?inicio=2025-03-01&fin=2025-03-02')['dias']))
    prueba('Inicializar: estado y consultas vacías',reset)
finally:
    detener(); log.close()
    reporte = {'fechaGuatemala':dt.datetime.now(dt.timezone(dt.timedelta(hours=-6))).isoformat(),
        'correctas':sum(r['resultado']=='OK' for r in resultados),'fallidas':sum(r['resultado']=='FAIL' for r in resultados),
        'resultados':resultados}
    (args.salida/'resultado-http.json').write_text(json.dumps(reporte,ensure_ascii=False,indent=2),encoding='utf-8')
print(f"\n{reporte['correctas']} correctas; {reporte['fallidas']} fallidas.")
sys.exit(1 if reporte['fallidas'] else 0)
