namespace kdspro.Domain.Enums;

public enum OrderStatus
{
    Pending,    // Recién llegado
    Preparing,  // El cocinero lo está haciendo
    Ready,      // Ya salió de la cocina
    Delivered   // Ya se le entregó al cliente
}