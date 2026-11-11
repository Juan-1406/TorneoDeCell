using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Pequeña base instrumental y efectos originales, generados y guardados una sola vez.</summary>
public static class GenerarAudioAfiche
{
    const string Carpeta = "Assets/_Project/Audio/MenuAfiche/";
    const int Frecuencia = 32000;
    const double Beat = 60.0 / 110;
    static readonly int[] Raices = { 50, 46, 53, 48 };
    static readonly int[] Melodia = { 74, 0, 77, 76, 0, 72, 69, 0, 70, 0, 74, 77, 0, 74, 72, 0,
        77, 0, 81, 79, 0, 77, 76, 0, 72, 0, 76, 79, 0, 76, 74, 72 };

    public static void Crear()
    {
        Directory.CreateDirectory(Carpeta);
        Guardar("Torneo_Nocturno", Beat * 32, Musica);
        Guardar("Seleccion", .085, (t,c) => .28 * Math.Sin(2*Math.PI*(630*t+850*t*t))*Math.Exp(-t*44)*Math.Min(1,t*700));
        Guardar("Confirmar", .32, (t,c) =>
        {
            double f = t < .09 ? 440 : t < .18 ? 554.365 : 659.255;
            return .23 * (Math.Sin(2*Math.PI*f*t)+.2*Math.Sin(4*Math.PI*f*t))*Math.Exp(-t*7)*Math.Min(1,t*500);
        });
        Guardar("Papel", .48, (t,c) => Ruido(t,c)*.18*Math.Sin(Math.PI*t/.48)*Math.Exp(-t*3));
    }

    static double Musica(double t, int canal)
    {
        double b = t / Beat;
        int pulso = (int)b;
        double f = b - pulso;
        int raiz = Raices[(pulso / 8) % 4];
        double salida = 0;
        // Bajo cálido y acordes cortos, sin voces ni muestras de otras canciones.
        double bajo = Nota(raiz - 12);
        salida += .15 * Math.Sin(2*Math.PI*bajo*t) * Math.Exp(-f*6) * Math.Min(1,f*65);
        double acorde = Math.Exp(-f*10) * Math.Min(1,f*65);
        if (pulso % 2 == 0)
        {
            int tercera = (pulso / 8) % 4 == 0 ? 3 : 4;
            salida += .022 * acorde * (Math.Sin(2*Math.PI*Nota(raiz+12)*t) +
                Math.Sin(2*Math.PI*Nota(raiz+12+tercera)*t) + Math.Sin(2*Math.PI*Nota(raiz+19)*t));
        }
        // Bombo suave, caja filtrada y charles discretos.
        if (pulso % 4 == 0 || pulso % 4 == 2)
            salida += .25 * Math.Sin(2*Math.PI*(48*f*Beat + 4*(1-Math.Exp(-f*22)))) * Math.Exp(-f*16);
        if (pulso % 4 == 1 || pulso % 4 == 3)
            salida += .055 * Ruido(t,canal) * Math.Exp(-f*18) * Math.Min(1,f*80);
        double medio = b*2-Math.Floor(b*2);
        salida += .018 * Ruido(t+7,canal) * Math.Exp(-medio*35);
        int indice = (int)(b*2) % Melodia.Length;
        int nota = Melodia[indice];
        if (nota > 0)
        {
            double fase = b*2-Math.Floor(b*2);
            double tono = Nota(nota);
            double onda = Math.Sin(2*Math.PI*tono*t) + .18*Math.Sin(4*Math.PI*tono*t);
            double pan = canal == 0 ? .95 : 1.05;
            salida += .042 * onda * Math.Exp(-fase*5) * Math.Min(1,fase*65) * pan;
        }
        // El final se amortigua para que la vuelta al primer compás no haga clic.
        double fin = Beat*32-t;
        return salida * Math.Min(1,t*80) * Math.Min(1,fin*100);
    }
    static double Nota(int midi) => 440 * Math.Pow(2,(midi-69)/12.0);
    static double Ruido(double t, int canal)
    {
        double n = Math.Sin(Math.Floor(t*Frecuencia)*12.9898+canal*78.233)*43758.5453;
        return (n-Math.Floor(n))*2-1;
    }
    static void Guardar(string nombre, double duracion, Func<double,int,double> muestra)
    {
        string ruta = Carpeta + nombre + ".wav";
        if (!File.Exists(ruta))
        {
            int cantidad = (int)(duracion*Frecuencia), bytes = cantidad*4;
            using (var w = new BinaryWriter(File.Create(ruta)))
            {
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36+bytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16);
                w.Write((short)1); w.Write((short)2); w.Write(Frecuencia); w.Write(Frecuencia*4);
                w.Write((short)4); w.Write((short)16); w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                for (int i=0;i<cantidad;i++) for (int c=0;c<2;c++)
                    w.Write((short)(Math.Max(-.95,Math.Min(.95,muestra(i/(double)Frecuencia,c)))*32767));
            }
        }
        AssetDatabase.ImportAsset(ruta,ImportAssetOptions.ForceSynchronousImport);
        var importer = (AudioImporter)AssetImporter.GetAtPath(ruta);
        var s = importer.defaultSampleSettings;
        s.loadType = AudioClipLoadType.DecompressOnLoad;
        s.compressionFormat = nombre == "Torneo_Nocturno" ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
        s.quality = .8f; importer.defaultSampleSettings=s; importer.SaveAndReimport();
    }
}
