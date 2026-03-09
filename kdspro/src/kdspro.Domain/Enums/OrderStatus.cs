namespace kdspro.Domain.Enums;

/// <summary>
/// Define los estados posibles del ciclo de vida de un pedido en el sistema KDS.
/// Crucial para la sincronización en tiempo real vía SignalR (Mes 2).
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Estado inicial: El pedido ha sido registrado por el mesero y espera en la cola FIFO.
    /// Se visualiza como un ticket nuevo en la pantalla de cocina.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// El cocinero ha marcado el pedido para comenzar su elaboración.
    /// Útil para informar al mesero que el plato ya está en proceso.
    /// </summary>
    Preparing = 1,

    /// <summary>
    /// El pedido ha finalizado su preparación. 
    /// Dispara una notificación automática al mesero (Requisito 5: "Listo para servir").
    /// </summary>
    Ready = 2,

    /// <summary>
    /// El pedido ha sido entregado físicamente al cliente. 
    /// En este punto, el ticket desaparece de la pantalla de despacho activa.
    /// </summary>
    Delivered = 3,

    /// <summary>
    /// El pedido ha sido anulado por el administrador o el mesero. 
    /// No genera tiempos de preparación ni afecta KPIs de velocidad.
    /// </summary>
    Cancelled = 4,
}