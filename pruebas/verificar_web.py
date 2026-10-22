"""Pruebas de lectura y archivos inválidos del frontend con datos 01 y 02 cargados.
Ejemplo: python pruebas/verificar_web.py --url http://localhost:5000
No inicializa el sistema ni agrega facturas.
"""
import argparse
from html import unescape
import http.cookiejar
import json
from pathlib import Path
import re
import urllib.error
import urllib.parse
import urllib.request
import uuid

parser = argparse.ArgumentParser()
parser.add_argument('--url', default='http://localhost:5000')
parser.add_argument('--salida', type=Path)
args = parser.parse_args()
base = args.url.rstrip('/')
cliente = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
resultados = []
def leer(ruta, data=None, headers=None):
    with cliente.open(urllib.request.Request(base+ruta,data=data,headers=headers or {}),timeout=30) as r:
        return r.read(),r.headers.get_content_type()
def probar(nombre, accion):
    try: accion(); resultados.append({'prueba':nombre,'resultado':'OK'}); print('OK  ',nombre)
    except Exception as ex: resultados.append({'prueba':nombre,'resultado':'FAIL','detalle':str(ex)}); print('FAIL',nombre,ex)
def contiene(ruta, texto):
    raw,_=leer(ruta); html=unescape(raw.decode('utf-8')); assert texto in html,(ruta,texto)
    assert 'Revisa los parámetros de la consulta.' not in html
    assert 'No se pudo completar la operación' not in html
filtros={'Inicio':'2025-03-01','Fin':'2025-03-02','Fecha':'2025-03-02','Buscar':'true'}
def ruta(**kw): return '/Peticiones?'+urllib.parse.urlencode({**filtros,**kw})
probar('Inicio muestra RTU y datos',lambda:contiene('/','Contribuyentes en RTU'))
probar('Formulario de envío',lambda:contiene('/Enviar','multipart/form-data'))
probar('Consulta emitidas sin filtros ajenos',lambda:contiene(ruta(Tipo='contribuyente',Nit='999999K',Sentido='emitidas'),'9,672.32'))
probar('Consulta recibidas',lambda:contiene(ruta(Tipo='contribuyente',Nit='999999K',Sentido='recibidas'),'9,258.06'))
probar('IVA sin NIT obligatorio',lambda:contiene('/Peticiones?Tipo=iva&Fecha=2025-03-02&Buscar=true','494.06'))
probar('Rango con IVA',lambda:contiene(ruta(Tipo='rango'),'18,930.38'))
probar('Rango sin IVA',lambda:contiene(ruta(Tipo='rango',SinIva='true'),'16,902.13'))
probar('Reporte de errores sin NIT',lambda:contiene(ruta(Tipo='errores'),'Recuento diario'))
def descargar(tipo,handler,formato):
    raw,mime=leer(ruta(Tipo=tipo,handler=handler,Nit='999999K',Sentido='emitidas'))
    assert mime==formato,(mime,formato)
    assert raw.startswith(b'%PDF-') if formato=='application/pdf' else b'<svg ' in raw
for tipo in ['contribuyente','iva','rango','errores']:
    probar('Descarga PDF en Web: '+tipo,lambda t=tipo:descargar(t,'Pdf','application/pdf'))
for tipo in ['iva','rango']:
    probar('Gráfica en Web: '+tipo,lambda t=tipo:descargar(t,'Grafica','image/svg+xml'))
probar('Ayuda personalizada',lambda:contiene('/Ayuda','William René Toledo Corado'))
def documentos():
    raw,mime=leer('/Ayuda?handler=Documento'); assert mime=='application/pdf' and raw.startswith(b'%PDF-')
probar('Documentación accesible desde Ayuda',documentos)
def invalido(nombre):
    html=leer('/Enviar')[0].decode('utf-8')
    token=re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',html).group(1)
    boundary='----SatDte'+uuid.uuid4().hex
    raw=(Path(__file__).parent/'entrada'/nombre).read_bytes()
    payload=(f'--{boundary}\r\nContent-Disposition: form-data; name="__RequestVerificationToken"\r\n\r\n{token}\r\n'
             f'--{boundary}\r\nContent-Disposition: form-data; name="Archivos"; filename="{nombre}"\r\nContent-Type: application/json\r\n\r\n').encode()+raw+f'\r\n--{boundary}--\r\n'.encode()
    respuesta,_=leer('/Enviar',payload,{'Content-Type':'multipart/form-data; boundary='+boundary})
    html=respuesta.decode('utf-8'); assert 'No procesado' in html and 'No se pudo completar la operación' not in html
for nombre in ['05a_json_malformado.json','05b_arreglo_vacio.json','05c_objeto_suelto.json','05d_sin_llaves_conocidas.json']:
    probar('Archivo inválido mostrado sin 500: '+nombre,lambda n=nombre:invalido(n))
reporte={'correctas':sum(r['resultado']=='OK' for r in resultados),'fallidas':sum(r['resultado']=='FAIL' for r in resultados),'resultados':resultados}
if args.salida: args.salida.write_text(json.dumps(reporte,ensure_ascii=False,indent=2),encoding='utf-8')
print(f"\n{reporte['correctas']} correctas; {reporte['fallidas']} fallidas.")
raise SystemExit(bool(reporte['fallidas']))
