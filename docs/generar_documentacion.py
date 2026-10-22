"""Regenera Documentacion.pdf a partir de ENSAYO.md. Requiere Python y reportlab.
El programa de SAT/DTE y sus reportes PDF no necesitan Python.
"""
from pathlib import Path
import math
import re
from xml.sax.saxutils import escape
from reportlab.lib import colors
from reportlab.lib.enums import TA_JUSTIFY, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, PageBreak, Flowable, KeepTogether

ROOT=Path(__file__).resolve().parent
W,H=A4
INK=colors.HexColor('#142941'); BLUE=colors.HexColor('#176bce'); MUTED=colors.HexColor('#64748b')
WIDTH=W-104
body=ParagraphStyle('Cuerpo',fontName='Times-Roman',fontSize=11.3,leading=16,alignment=TA_JUSTIFY,spaceAfter=10,textColor=INK)
heading=ParagraphStyle('Titulo',fontName='Helvetica-Bold',fontSize=17,leading=22,spaceAfter=20,textColor=INK)
small=ParagraphStyle('Pequeno',fontName='Helvetica',fontSize=9,leading=13,spaceAfter=10,textColor=MUTED,wordWrap='CJK')
caption=ParagraphStyle('Pie',fontName='Helvetica-Oblique',fontSize=9,leading=13,spaceBefore=8,spaceAfter=10,textColor=MUTED)

class Diagrama(Flowable):
    def __init__(self, clase, altura=610): super().__init__(); self.clase=clase; self.width=WIDTH; self.height=altura
    def draw(self):
        c=self.canv; c.setLineWidth(.8)
        def box(x,y,w,h,title,lines=()):
            c.setStrokeColor(colors.HexColor('#b8cce4')); c.setFillColor(colors.HexColor('#f2f7fd')); c.roundRect(x,y,w,h,6,fill=1,stroke=1)
            c.setFillColor(INK);c.setFont('Helvetica-Bold',9.4);c.drawString(x+10,y+h-17,title)
            c.setFont('Helvetica',8.7)
            for i,line in enumerate(lines): c.drawString(x+10,y+h-34-i*13,line)
        def centered(x,y,w,h,lines):
            box(x,y,w,h,''); c.setFont('Helvetica',9.1);c.setFillColor(INK)
            for i,line in enumerate(lines): c.drawCentredString(x+w/2,y+h/2+(len(lines)-1)*6-i*12,line)
        def arrow(points,label=None,dashed=False):
            c.setStrokeColor(MUTED);c.setFillColor(MUTED);c.setDash(4,3) if dashed else c.setDash()
            for a,b in zip(points,points[1:]): c.line(*a,*b)
            a,b=points[-2:]; ang=math.atan2(b[1]-a[1],b[0]-a[0]); size=5
            for d in [-.5,.5]: c.line(b[0],b[1],b[0]-size*math.cos(ang+d),b[1]-size*math.sin(ang+d))
            c.setDash()
            if label: c.setFont('Helvetica',8);c.drawString((a[0]+b[0])/2+5,(a[1]+b[1])/2+5,label)
        def diamond(x,y,w,h,lines):
            p=c.beginPath();p.moveTo(x,y+h/2);p.lineTo(x+w/2,y+h);p.lineTo(x+w,y+h/2);p.lineTo(x+w/2,y);p.close()
            c.setFillColor(colors.HexColor('#fff4e9'));c.setStrokeColor(colors.HexColor('#dfa065'));c.drawPath(p,fill=1,stroke=1)
            c.setFillColor(INK);c.setFont('Helvetica',8.6)
            for i,line in enumerate(lines):c.drawCentredString(x+w/2,y+h/2+4-i*11,line)
        if self.clase=='clases':
            box(0,505,225,94,'ServicioDte',['Procesar(JSON) / Inicializar()','Estado() / Contribuyentes()','depende de IRepositorio e IReloj'])
            box(265,505,225,94,'ServicioConsultas',['Contribuyente(NIT, tipo, rango)','Iva(fecha) / Rango(...)','Errores(inicio, fin)'])
            box(20,365,205,70,'IRepositorio <<interfaz>>',['Leer(consulta)','Actualizar(operacion)'])
            box(290,365,200,70,'IReloj <<interfaz>>',['Ahora : DateTimeOffset'])
            arrow([(110,505),(110,435)],'usa');arrow([(375,505),(375,465),(185,465),(185,435)])
            arrow([(210,505),(255,485),(390,485),(390,435)],'usa')
            box(20,220,205,100,'RepositorioJson',['ruta / bloqueo de archivo','estado : EstadoSistema','Leer / Actualizar / Dispose','copia + escritura transaccional'])
            box(290,250,200,70,'RelojGuatemala',['Ahora = UTC con offset -6'])
            arrow([(120,320),(120,365)],'implementa',True);arrow([(390,320),(390,365)],'implementa',True)
            box(20,90,205,95,'EstadoSistema',['Contribuyentes : Dictionary','Facturas / Descartes : List','Correlativos : Dictionary'])
            arrow([(120,220),(120,185)],'contiene')
            box(285,110,205,95,'Factura',['Fecha / Referencia / NITs','Valor / Iva / Total : decimal','CodigoAprobacion / Errores'])
            box(285,0,205,70,'Contribuyente',['NIT : string','Nombre : string'])
            arrow([(225,145),(285,145)],'0..*');arrow([(120,90),(120,35),(285,35)],'0..*')
        elif self.clase=='autorizar':
            centered(125,565,240,40,['Recibir y analizar JSON'])
            diamond(165,475,160,65,['¿Lote homogéneo','no vacío?'])
            arrow([(245,565),(245,540)])
            centered(0,480,100,53,['HTTP 400','Sin cambios'])
            arrow([(165,507),(100,507)],'No')
            centered(125,410,240,40,['Bloquear y copiar el estado'])
            arrow([(245,475),(245,450)],'Sí')
            centered(125,345,240,40,['RTU: validar y crear/actualizar','Facturas: extraer y validar formato'])
            arrow([(245,410),(245,385)])
            centered(125,280,240,40,['Validar NIT, existencia, IVA,','total y referencia; registrar errores'])
            arrow([(245,345),(245,320)])
            diamond(165,195,160,60,['¿Factura sin errores?'])
            arrow([(245,280),(245,255)])
            centered(0,110,200,50,['Sí: asignar fecha de Guatemala','y siguiente correlativo'])
            centered(290,110,200,50,['No: guardar rechazo','o descarte por formato'])
            arrow([(165,225),(100,225),(100,160)])
            arrow([(325,225),(390,225),(390,160)])
            centered(125,25,240,50,['Tras recorrer todas las filas:','guardar temporal, reemplazar y responder'])
            arrow([(100,110),(100,50),(125,50)])
            arrow([(390,110),(390,50),(365,50)])
        elif self.clase=='nit':
            centered(110,550,270,40,['Normalizar NIT y verificar patrón'])
            arrow([(245,550),(245,515)])
            centered(110,475,270,40,['Separar el carácter verificador final'])
            arrow([(245,475),(245,440)])
            centered(110,390,270,50,['Recorrer la base de derecha a izquierda','Sumar dígito x peso; pesos 2, 3, ...'])
            arrow([(245,390),(245,355)])
            centered(110,305,270,50,['r = (11 - (suma mod 11)) mod 11','Si r = 10, convertir a K'])
            arrow([(245,305),(245,275)])
            diamond(165,210,160,65,['¿Coincide con','el verificador?'])
            centered(15,130,175,42,['Sí: NIT válido'])
            centered(300,130,175,42,['No: NIT inválido'])
            arrow([(165,242),(100,242),(100,172)])
            arrow([(325,242),(390,242),(390,172)])
            c.setFillColor(INK);c.setFont('Helvetica-Bold',11);c.drawString(0,88,'Actividad de consulta')
            for x,w,lines in [(0,110,['Validar filtros']),(130,110,['Leer instantánea']),(260,110,['Filtrar y agrupar']),(390,100,['Sumar / exportar'])]:centered(x,15,w,45,lines)
            for x in [110,240,370]:arrow([(x,37),(x+20,37)])
        elif self.clase=='despliegue':
            c.setStrokeColor(colors.HexColor('#b8cce4'));c.setFillColor(colors.HexColor('#f8fafd'));c.roundRect(0,15,490,355,8,fill=1)
            c.setFillColor(MUTED);c.setFont('Helvetica',9);c.drawString(14,350,'Equipo local - configuración predeterminada')
            centered(20,260,170,55,['Navegador','Razor Pages visible'])
            centered(295,260,175,55,['SatDte.Web','http://localhost:5000'])
            arrow([(190,287),(295,287)],'HTTP')
            centered(295,145,175,65,['SatDte.Api + SatDte.Core','http://localhost:5001','JSON / PDF / SVG'])
            arrow([(382,260),(382,210)],'HttpClient')
            centered(20,145,170,65,['Postman u otro cliente','Consumidor independiente'])
            arrow([(190,177),(295,177)],'HTTP')
            centered(295,40,175,60,['RepositorioJson','estado.json + bloqueo'])
            arrow([(382,145),(382,100)],'Archivo local')

def chrome(c,doc):
    if doc.page==1: return
    c.saveState();c.setStrokeColor(colors.HexColor('#dae3ee'));c.line(52,H-37,W-52,H-37)
    c.setFillColor(MUTED);c.setFont('Helvetica',8);c.drawString(52,H-27,'IPC2 / PROYECTO 3 - SAT / DTE');c.drawRightString(W-52,H-27,'202210198 / SECCIÓN B')
    c.line(52,40,W-52,40);c.drawString(52,26,'William René Toledo Corado - USAC - 2026');c.drawRightString(W-52,26,str(doc.page));c.restoreState()

def cover(c,doc):
    c.saveState();c.setFillColor(INK);c.rect(0,H-285,W,285,fill=1,stroke=0)
    c.setFillColor(colors.HexColor('#9ac7ff'));c.setFont('Helvetica-Bold',10);c.drawString(52,H-70,'UNIVERSIDAD DE SAN CARLOS DE GUATEMALA')
    c.setFillColor(colors.white);c.setFont('Helvetica-Bold',31)
    for i,line in enumerate(['Autorización de','documentos tributarios','mediante una API']):c.drawString(52,H-126-i*40,line)
    c.setFont('Helvetica',12);c.drawString(52,H-255,'Programación orientada a objetos / C# / HTTP')
    c.setFillColor(INK);c.setFont('Helvetica-Bold',16);c.drawString(52,H-350,'William René Toledo Corado')
    c.setFont('Helvetica',12)
    for i,line in enumerate(['Carné: 202210198','Sección B','Introducción a la Programación y Computación 2','Escuela de Ciencias y Sistemas / Facultad de Ingeniería','Proyecto 3 / Segundo semestre 2026']):c.drawString(52,H-383-i*27,line)
    c.setStrokeColor(colors.HexColor('#d7e1ed'));c.line(52,190,W-52,190)
    c.setFillColor(MUTED);c.setFont('Helvetica',10);c.drawString(52,165,'Guatemala, 22 de octubre de 2026')
    c.drawString(52,145,'Ensayo técnico y anexos de diseño')
    c.setFont('Helvetica',8.5);c.drawString(52,88,'Cinco páginas de contenido. Diagramas y referencias en anexos.')
    c.drawString(52,73,'Revisar su adecuación a la plantilla de ensayo del curso antes de entregar.')
    c.restoreState()

texto=(ROOT/'ENSAYO.md').read_text(encoding='utf-8')
secciones=texto.split('<!-- PAGE -->')[1:]
referencias=secciones[-1].split('## Referencias')[1]
secciones[-1]=secciones[-1].split('## Referencias')[0]
story=[Spacer(1,1),PageBreak()]
for i,seccion in enumerate(secciones):
    for bloque in seccion.strip().split('\n\n'):
        if bloque.startswith('## '):story.append(Paragraph(escape(bloque[3:]),heading))
        else:story.append(Paragraph(escape(bloque.replace('\n',' ')),body))
    story.append(PageBreak())
for nombre,tipo,altura,pie in [
    ('Anexo A. Modelo de clases','clases',610,'Figura A1. Clases e interfaces principales del núcleo y relaciones de dependencia.'),
    ('Anexo B. Actividad de procesamiento','autorizar',610,'Figura B1. Validación, autorización y confirmación de un lote. Los descartes de formato omiten las reglas de negocio; un lote RTU crea o actualiza solo los NIT válidos.'),
    ('Anexo C. Actividades de validación y consulta','nit',610,'Figura C1. Algoritmo módulo once para NIT y secuencia de construcción de consultas.'),
    ('Anexo D. Despliegue y referencias','despliegue',380,'Figura D1. Dos procesos HTTP y un almacenamiento local; la Web consume la API.')]:
    story.append(Paragraph(nombre,heading));story.append(Diagrama(tipo,altura));story.append(Paragraph(pie,caption))
    if tipo=='despliegue':
        story.append(Paragraph('Referencias',heading))
        for parrafo in referencias.strip().split('\n\n'):story.append(Paragraph(escape(parrafo.replace('\n',' ')),small))
    else:story.append(PageBreak())
doc=SimpleDocTemplate(str(ROOT/'Documentacion.pdf'),pagesize=A4,leftMargin=52,rightMargin=52,topMargin=57,bottomMargin=57,
    title='Autorización de documentos tributarios mediante una API',author='William René Toledo Corado',subject='Proyecto 3 IPC2 - 202210198 - Sección B')
doc.build(story,onFirstPage=cover,onLaterPages=chrome)
print(ROOT/'Documentacion.pdf')
