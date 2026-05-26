using System.Collections.Generic;
using UnityEngine;

// cada valor es una potencia de dos para poder combinar varios con flags de bits
[System.Flags]
public enum AtaqueID
{
    Ninguno = 0,

    // ataques de puno flojo
    PunoFlojoSuelo = 1 << 0,
    PunoFlojoAgachado = 1 << 1,
    PunoFlojoAire = 1 << 2,

    // ataques de puno fuerte
    PunoFuerteSuelo = 1 << 3,
    PunoFuerteAgachado = 1 << 4,
    PunoFuerteAire = 1 << 5,

    // ataques de patada floja
    PatadaFlojaSuelo = 1 << 6,
    PatadaFlojaAgachado = 1 << 7,
    PatadaFlojaAire = 1 << 8,

    // ataques de patada fuerte
    PatadaFuerteSuelo = 1 << 9,
    PatadaFuerteAgachado = 1 << 10,
    PatadaFuerteAire = 1 << 11,

    // comandos especiales (move list especifica del personaje)
    Comando1 = 1 << 12,
    Comando2 = 1 << 13,
    Comando3 = 1 << 14,
    Comando4 = 1 << 15,

    // acciones de movimiento cancelables
    Salto = 1 << 16,
    DashAdelanteTerrestre = 1 << 17,
    DashAtrasTerrestre = 1 << 18,
    DashAdelanteAereo = 1 << 19,
    DashAtrasAereo = 1 << 20,
}

[System.Serializable]
public class CancelRule
{
    [Tooltip("El ataque que se esta ejecutando (elige UNO)")]
    public AtaqueID ataqueFuente;

    // campo flags: se pueden marcar multiples ataques destino en el Inspector
    [Tooltip("Ataques/acciones a los que se puede cancelar al conectar " +
             "(puedes marcar varios en el dropdown)")]
    public AtaqueID cancelesPermitidos;
}