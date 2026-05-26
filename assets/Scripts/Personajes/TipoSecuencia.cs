using UnityEngine;

/// Tipos de secuencia de direcciones disponibles para cualquier comando.
public enum TipoSecuencia
{
    QCF,                   // abajo-adelante
    QCB,                   // abajo-atras
    DobleAbajo,
    MedioCirculoAdelante,  // abajo-atras  abajo-adelante
    MedioCirculoAtras,     // abajo-adelante  abajo-atras
    DobleQCF,              // abajo-adelante  abajo-adelante  (el que usa el sistema de ultimate actual)
    QCFmasQCFInverso       // abajo-adelante  seguido de  abajo-atras
}

[System.Serializable]
public class ConfiguracionComando
{
    [Tooltip("Secuencia de direcciones requerida.")]
    public TipoSecuencia secuencia = TipoSecuencia.QCB;

    [Tooltip("Boton que confirma el comando al terminar la secuencia.\n" +
             "0 = Puño Flojo  |  1 = Puño Fuerte\n" +
             "2 = Patada Floja  |  3 = Patada Fuerte\n" +
             "-1 = cualquier boton")]
    public int botonActivacion = 0;
}