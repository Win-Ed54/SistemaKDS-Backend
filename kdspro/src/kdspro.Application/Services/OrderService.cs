using kdspro.Application.DTOs;
using kdspro.Application.Interfaces;
using kdspro.Domain.Entities;
using kdspro.Domain.Enums;
using kdspro.Domain.Interfaces;

namespace kdspro.Application.Services;

/// <summary>
/// Orquesta el flujo completo de pedidos: validaciones, reserva de stock,
/// cambios de estado, cobro parcial y notificaciones en tiempo real.
/// </summary>
public class OrderService : IOrderService
{
    private const decimal TaxRate = 0.13m;
    /// <summary>
    /// Catalogo de metodos admitidos por caja para evitar valores arbitrarios
    /// entre frontend, backend y reportes.
    /// </summary>
    private static readonly HashSet<string> AllowedPaymentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "efectivo",
        "tarjeta",
        "transferencia",
    };
    /// <summary>
    /// Tipos de comprobante soportados por el flujo de cobro.
    /// </summary>
    private static readonly HashSet<string> AllowedDocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ticket",
        "consumidor_final",
        "factura",
    };
    private static readonly string[] AllowedTakeoutDestinations =
    {
        "Mostrador",
        "Autoservicio",
        "Delivery",
    };
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly ITableRepository _tableRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrderNotificationService _notificationService;
    private readonly IKdsSettingsService _settingsService;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IIngredientRepository ingredientRepository,
        ITableRepository tableRepository,
        IUserRepository userRepository,
        IOrderNotificationService notificationService,
        IKdsSettingsService settingsService)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _ingredientRepository = ingredientRepository;
        _tableRepository = tableRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _settingsService = settingsService;
    }

    /// <summary>
    /// Crea una orden validando reglas operativas, reservando ingredientes
    /// y descontando productos antes de notificar al resto de vistas.
    /// </summary>
    public async Task<OrderDto> CreateOrder(CreateOrderDto dto, string userId, string username)
    {
        var settings = await _settingsService.GetAsync();
        var normalizedCustomerName = (dto.CustomerName ?? string.Empty).Trim();
        var normalizedTakeoutDestination = (dto.TakeoutDestination ?? string.Empty).Trim();
        var normalizedDeliveryAddress = (dto.DeliveryAddress ?? string.Empty).Trim();
        var requiresTakeoutPrepayment = dto.TableNumber == 0 && settings.TakeoutRequirePrepayment;

        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("La orden debe incluir al menos un producto.");

        if (dto.TableNumber == 0 && settings.RequireCustomerNameForTakeout && string.IsNullOrWhiteSpace(normalizedCustomerName))
            throw new InvalidOperationException("El nombre del cliente es obligatorio para pedidos para llevar.");

        if (normalizedCustomerName.Length > 80)
            throw new InvalidOperationException("El nombre del cliente no puede exceder 80 caracteres.");

        if (normalizedTakeoutDestination.Length > 80)
            throw new InvalidOperationException("El destino para llevar no puede exceder 80 caracteres.");

        if (normalizedDeliveryAddress.Length > 180)
            throw new InvalidOperationException("La direccion de delivery no puede exceder 180 caracteres.");

        if (dto.TableNumber == 0)
        {
            normalizedTakeoutDestination = NormalizeTakeoutDestination(normalizedTakeoutDestination);

            if (string.Equals(normalizedTakeoutDestination, "Delivery", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(normalizedDeliveryAddress))
            {
                throw new InvalidOperationException("La direccion es obligatoria para pedidos delivery.");
            }
        }

        if (dto.Items.Count > settings.MaxDistinctItems)
            throw new InvalidOperationException($"Maximo {settings.MaxDistinctItems} productos distintos por orden.");

        var totalUnits = dto.Items.Sum(item => item.Quantity);
        if (totalUnits > settings.MaxTotalUnits)
            throw new InvalidOperationException($"Maximo {settings.MaxTotalUnits} unidades totales por orden.");

        var invalidItem = dto.Items.FirstOrDefault(item =>
            string.IsNullOrWhiteSpace(item.ProductId) ||
            item.Quantity < 1 ||
            item.Quantity > settings.MaxQuantityPerProduct ||
            (OrderValidationRules.NormalizeKitchenNote(item.Notes).Length > OrderValidationRules.MaxKitchenNoteLength) ||
            !OrderValidationRules.IsKitchenNoteAllowed(item.Notes)
        );

        if (invalidItem != null)
            throw new InvalidOperationException($"{invalidItem.ProductName}: revise cantidad o notas. Las notas solo aceptan letras y simbolos permitidos.");

        var productsById = new Dictionary<string, Product>();
        var ingredients = await _ingredientRepository.GetAllAsync();
        var ingredientsById = ingredients
            .Where(ingredient => !string.IsNullOrWhiteSpace(ingredient.Id))
            .ToDictionary(ingredient => ingredient.Id!, ingredient => ingredient, StringComparer.OrdinalIgnoreCase);
        var aggregatedIngredientDemand = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        // Primero validamos disponibilidad real de productos e ingredientes sin tocar stock.
        foreach (var item in dto.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null || product.Stock < item.Quantity || product.Price < 0)
            {
                await _notificationService.NotifyProductOutOfStock(item.ProductId);
                throw new Exception($"Stock insuficiente para: {item.ProductName}. Disponible: {product?.Stock ?? 0}");
            }

            var shortages = IngredientAvailabilityService.GetShortages(product, ingredientsById, item.Quantity);
            if (shortages.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Ingredientes insuficientes para {product.Name}: {string.Join(", ", shortages.Select(shortage => $"{shortage.IngredientName} ({shortage.Available}/{shortage.Required} {shortage.Unit})"))}.");
            }

            foreach (var recipeItem in product.Recipe ?? [])
            {
                var ingredientId = recipeItem.IngredientId ?? string.Empty;
                if (string.IsNullOrWhiteSpace(ingredientId)) continue;

                aggregatedIngredientDemand[ingredientId] =
                    (aggregatedIngredientDemand.TryGetValue(ingredientId, out var currentDemand) ? currentDemand : 0) +
                    (recipeItem.QuantityRequired * item.Quantity);
            }

            productsById[item.ProductId] = product;
        }

        // Luego reservamos ingredientes antes de descontar productos para evitar sobreventa.
        foreach (var entry in aggregatedIngredientDemand)
        {
            if (!ingredientsById.TryGetValue(entry.Key, out var ingredient) || ingredient == null || !ingredient.IsActive)
            {
                throw new InvalidOperationException("Uno de los ingredientes requeridos ya no esta disponible.");
            }

            if (ingredient.Stock < entry.Value)
            {
                throw new InvalidOperationException(
                    $"No hay suficiente {ingredient.Name}. Disponible: {ingredient.Stock} {ingredient.Unit}. Requerido: {entry.Value} {ingredient.Unit}.");
            }
        }

        var updatedStocks = new Dictionary<string, int>();
        var reservedIngredients = new List<OrderIngredientReservation>();
        var deductedProducts = new List<(string ProductId, int Quantity)>();

        foreach (var entry in aggregatedIngredientDemand)
        {
            if (!ingredientsById.TryGetValue(entry.Key, out var ingredient) || ingredient == null)
                throw new InvalidOperationException("Uno de los ingredientes requeridos ya no esta disponible.");

            var reserved = await _ingredientRepository.DeductStockAsync(entry.Key, entry.Value);
            if (!reserved)
            {
                await RestoreReservedIngredientsAsync(reservedIngredients);
                throw new InvalidOperationException(
                    $"No se pudo reservar suficiente {ingredient.Name} para completar la orden.");
            }

            reservedIngredients.Add(new OrderIngredientReservation
            {
                IngredientId = ingredient.Id ?? string.Empty,
                IngredientName = ingredient.Name,
                Unit = ingredient.Unit,
                Quantity = entry.Value,
            });
        }

        // Con ingredientes reservados, descontamos stock de productos visibles en UI.
        foreach (var item in dto.Items)
        {
            var success = await _productRepository.DeductStockAsync(item.ProductId, item.Quantity);

            if (!success)
            {
                await RestoreDeductedProductsAsync(deductedProducts);
                await RestoreReservedIngredientsAsync(reservedIngredients);
                await _notificationService.NotifyProductOutOfStock(item.ProductId);
                throw new Exception($"Lo sentimos. Ya no hay stock suficiente para: {item.ProductName}.");
            }

            deductedProducts.Add((item.ProductId, item.Quantity));

            var updated = await _productRepository.GetByIdAsync(item.ProductId);
            if (updated != null)
            {
                updatedStocks[item.ProductId] = updated.Stock;
                await _notificationService.NotifyStockUpdated(updated.Id!, updated.Stock);
            }
        }

        var grossTotal = dto.Items.Sum(i => productsById[i.ProductId].Price * i.Quantity);
        var taxableAmount = grossTotal <= 0
            ? 0
            : Math.Round(grossTotal / (1 + TaxRate), 2, MidpointRounding.AwayFromZero);
        var taxAmount = Math.Round(grossTotal - taxableAmount, 2, MidpointRounding.AwayFromZero);
        var correlativeNumber = await _orderRepository.GetNextCorrelativeNumberAsync();

        var order = new Order
        {
            CorrelativeNumber = correlativeNumber,
            CorrelativeCode = $"ORD-{correlativeNumber:000000}",
            TableNumber = dto.TableNumber,
            CustomerName = string.IsNullOrWhiteSpace(normalizedCustomerName) ? "GENERAL" : normalizedCustomerName,
            TakeoutDestination = dto.TableNumber == 0 ? normalizedTakeoutDestination : string.Empty,
            DeliveryAddress = dto.TableNumber == 0 ? normalizedDeliveryAddress : string.Empty,
            WaiterId = userId,
            WaiterName = username,
            TaxableAmount = taxableAmount,
            TaxAmount = taxAmount,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = dto.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = productsById[i.ProductId].Name,
                UnitPrice = productsById[i.ProductId].Price,
                Quantity = i.Quantity,
                Notes = OrderValidationRules.NormalizeKitchenNote(i.Notes)
            }).ToList(),
            IngredientReservations = reservedIngredients
        };

        try
        {
            await _orderRepository.CreateAsync(order);
        }
        catch
        {
            await RestoreDeductedProductsAsync(deductedProducts);
            await RestoreReservedIngredientsAsync(reservedIngredients);
            throw;
        }

        if (dto.TableNumber > 0)
        {
            await _tableRepository.SetOccupiedAsync(dto.TableNumber, true);
            var updatedTable = await _tableRepository.GetByNumberAsync(dto.TableNumber);
            if (updatedTable != null)
                await _notificationService.NotifyTableStatusUpdated(updatedTable);
        }

        var resultDto = MapToDto(order);

        // El flujo de para llevar puede quedarse en caja antes de publicarse a cocina.
        if (requiresTakeoutPrepayment)
        {
            await _notificationService.NotifyPendingPrepaymentOrder(resultDto);
        }
        else
        {
            await _notificationService.NotifyNewOrder(resultDto);
        }

        foreach (var itemDto in resultDto.Items)
        {
            if (updatedStocks.TryGetValue(itemDto.ProductId, out var stock))
                itemDto.CurrentStock = stock;
        }

        return resultDto;
    }

    public async Task SetPreparing(string orderId, string preparedByName)
    {
        // StartedAt se llena al entrar a preparacion; ReadyAt permanece intacto.
        await _orderRepository.UpdateStatusWithTimeAsync(
            orderId,
            OrderStatus.Preparing,
            DateTime.UtcNow,
            null
        );
        await _orderRepository.SetPreparedByAsync(orderId, preparedByName);

        var order = await GetOrderById(orderId);

        if (order != null)
            await _notificationService.NotifyOrderPreparing(order);
    }

    public async Task SetReady(string orderId, string preparedByName)
    {
        // Solo marcamos ReadyAt para conservar el inicio original de preparacion.
        await _orderRepository.UpdateStatusWithTimeAsync(
            orderId,
            OrderStatus.Ready,
            null,
            DateTime.UtcNow
        );
        await _orderRepository.SetPreparedByAsync(orderId, preparedByName);

        var order = await GetOrderById(orderId);

        if (order != null)
            await _notificationService.NotifyOrderReady(order);
    }

    public async Task SetFinished(string orderId)
    {
        // La entrega saca la orden de cocina, pero no necesariamente la deja cobrada.
        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Delivered);

        var order = await GetOrderById(orderId);

        if (order != null)
            await _notificationService.NotifyOrderDelivered(order);
    }

    public async Task MarkAsPaid(string orderId, string paidByName, MarkOrderPaidDto dto)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            throw new InvalidOperationException("La orden no existe.");

        var settings = await _settingsService.GetAsync();
        var allowTakeoutPrepayment =
            settings.TakeoutRequirePrepayment &&
            order.TableNumber == 0 &&
            order.Status != OrderStatus.Delivered;

        if (order.Status != OrderStatus.Delivered && !allowTakeoutPrepayment)
            throw new InvalidOperationException("Solo se pueden cobrar ordenes entregadas.");

        if (order.IsPaid)
            throw new InvalidOperationException("La orden ya fue cobrada.");

        dto ??= new MarkOrderPaidDto();
        var paymentMethod = (dto.PaymentMethod ?? "efectivo").Trim().ToLowerInvariant();
        var documentType = (dto.DocumentType ?? "ticket").Trim().ToLowerInvariant();
        var receiptNumber = OrderValidationRules.NormalizeReceiptNumber(dto.ReceiptNumber);

        if (!AllowedPaymentMethods.Contains(paymentMethod))
            throw new InvalidOperationException("Metodo de pago no permitido.");

        if (!AllowedDocumentTypes.Contains(documentType))
            throw new InvalidOperationException("Tipo de comprobante no permitido.");

        // Si caja no manda lineas puntuales, se cobra todo lo pendiente.
        var requestedPayments = dto.ItemPayments?
            .Where(item => item.Quantity > 0)
            .ToList() ?? [];

        if (requestedPayments.Count == 0)
        {
            requestedPayments = order.Items
                .Select((item, index) => new OrderItemPaymentDto
                {
                    LineIndex = index,
                    Quantity = Math.Max(0, item.Quantity - item.PaidQuantity),
                })
                .Where(item => item.Quantity > 0)
                .ToList();
        }

        if (requestedPayments.Count == 0)
            throw new InvalidOperationException("No hay productos pendientes por cobrar en esta orden.");

        // Validamos primero todas las lineas para no dejar la orden a medio actualizar.
        foreach (var payment in requestedPayments)
        {
            if (payment.LineIndex < 0 || payment.LineIndex >= order.Items.Count)
                throw new InvalidOperationException("La linea de producto seleccionada no existe.");

            var targetItem = order.Items[payment.LineIndex];
            var remainingQuantity = Math.Max(0, targetItem.Quantity - targetItem.PaidQuantity);

            if (remainingQuantity <= 0)
                throw new InvalidOperationException($"{targetItem.ProductName} ya fue cobrado por completo.");

            if (payment.Quantity > remainingQuantity)
            {
                throw new InvalidOperationException(
                    $"{targetItem.ProductName}: solo quedan {remainingQuantity} unidades pendientes de cobro.");
            }
        }

        // Aplicamos cantidades cobradas una vez que toda la solicitud es valida.
        foreach (var payment in requestedPayments)
        {
            order.Items[payment.LineIndex].PaidQuantity += payment.Quantity;
        }

        var allItemsPaid = order.Items.All(item => item.PaidQuantity >= item.Quantity);

        if (allItemsPaid)
        {
            order.IsPaid = true;
            order.PaidAt = DateTime.UtcNow;
            order.PaidByName = paidByName;
            order.PaymentMethod = paymentMethod;
            order.ReceiptNumber = receiptNumber;
            order.DocumentType = documentType;
            order.InvoiceRequested = dto.InvoiceRequested;
        }

        await _orderRepository.UpdateAsync(orderId, order);

        if (allItemsPaid)
        {
            var updatedOrder = await GetOrderById(orderId);
            if (updatedOrder != null)
            {
                await _notificationService.NotifyOrderPaid(updatedOrder);
                if (allowTakeoutPrepayment && order.Status == OrderStatus.Pending)
                    await _notificationService.NotifyNewOrder(updatedOrder);
            }
        }
    }

    public async Task CloseTable(int tableNumber, string? requesterUserId = null, bool isAdmin = false)
    {
        // El cierre de mesa es el final del ciclo operativo, no solo del pago.
        if (await _orderRepository.HasActiveOrdersForTableAsync(tableNumber, string.Empty))
            throw new InvalidOperationException($"La mesa {tableNumber} todavia tiene ordenes activas.");

        if (await _orderRepository.HasPendingPaymentForTableAsync(tableNumber))
            throw new InvalidOperationException($"La mesa {tableNumber} tiene cobros pendientes.");

        // La limpieza solo puede cerrarse sobre la ultima orden pagada sin otra mas reciente.
        var cleanupCandidate = (await _orderRepository.GetHistoryAsync())
            .Where(o => o.TableNumber == tableNumber)
            .Where(o => o.Status == OrderStatus.Delivered && o.IsPaid && !o.IsCleanupCompleted)
            .OrderByDescending(o => o.PaidAt ?? o.DeliveredAt ?? o.CreatedAt)
            .FirstOrDefault();

        if (cleanupCandidate == null)
            throw new InvalidOperationException($"La mesa {tableNumber} no tiene limpieza pendiente.");

        var hasNewerOrderForSameTable = await _orderRepository.HasNewerOrdersForTableAsync(
            tableNumber,
            cleanupCandidate.CreatedAt,
            cleanupCandidate.Id!
        );

        if (hasNewerOrderForSameTable)
            throw new InvalidOperationException($"La mesa {tableNumber} tiene una orden mas reciente y aun no puede liberarse.");

        if (!isAdmin && !string.IsNullOrEmpty(requesterUserId) && cleanupCandidate.WaiterId != requesterUserId)
            throw new InvalidOperationException($"Solo el mesero que tomo la ultima orden pagada de la mesa {tableNumber} puede limpiarla.");

        await _orderRepository.MarkCleanupCompletedForTableAsync(tableNumber);
        await _tableRepository.ClearServiceStateAsync(tableNumber, false);
        var updatedTable = await _tableRepository.GetByNumberAsync(tableNumber);
        if (updatedTable != null)
            await _notificationService.NotifyTableStatusUpdated(updatedTable);
    }

    public async Task<IEnumerable<OrderDto>> GetWaiterOrdersToday(string waiterId)
    {
        var allOrders = await _orderRepository.GetOrdersByWaiterAsync(waiterId);
        var today = DateTime.UtcNow.Date;

        // Se filtra en UTC porque todas las marcas de tiempo de orden se guardan en UTC.
        return allOrders
            .Where(o => o.CreatedAt >= today)
            .OrderByDescending(o => o.CreatedAt)
            .Select(MapToDto)
            .ToList();
    }

    /// <summary>
    /// Resume la operacion del mesero: ordenes creadas, activas y mesas que
    /// todavia requieren limpieza para liberarse.
    /// </summary>
    public async Task<WaiterSummaryDto> GetWaiterSummary(string userId, string username)
    {
        var allOrders = await _orderRepository.GetOrdersByWaiterAsync(userId);
        var user = await _userRepository.GetById(userId);
        var hasDedicatedTakeoutWaiter = await _userRepository.HasWaiterWithServiceScope("takeout", userId);
        var pendingCleanupOrders = new List<OrderDto>();

        foreach (var order in allOrders
            .Where(o => o.Status == OrderStatus.Delivered && o.IsPaid && !o.IsCleanupCompleted)
            .Where(o => o.TableNumber > 0)
            .OrderByDescending(o => o.PaidAt ?? o.DeliveredAt ?? o.CreatedAt))
        {
            var hasNewerOrderForSameTable = await _orderRepository.HasNewerOrdersForTableAsync(
                order.TableNumber,
                order.CreatedAt,
                order.Id!
            );

            if (!hasNewerOrderForSameTable)
            {
                pendingCleanupOrders.Add(MapToDto(order));
            }
        }

        return new WaiterSummaryDto
        {
            WaiterId = userId,
            WaiterName = username,
            ServiceScope = string.IsNullOrWhiteSpace(user?.ServiceScope)
                ? "hybrid"
                : user.ServiceScope.Trim().ToLowerInvariant(),
            TotalCreated = allOrders.Count,
            TotalDelivered = allOrders.Count(o => o.Status == OrderStatus.Delivered),
            HasDedicatedTakeoutWaiter = hasDedicatedTakeoutWaiter,
            MyActiveOrders = allOrders
                .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
                .Select(MapToDto)
                .ToList(),
            PendingCleanupOrders = pendingCleanupOrders
        };
    }

    public async Task CancelOrder(string orderId, string cancelledByName)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null) return;
        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("La orden ya fue cancelada.");
        if (order.Status == OrderStatus.Delivered)
            throw new InvalidOperationException("No puedes cancelar una orden ya entregada.");
        if (order.IsPaid)
            throw new InvalidOperationException("No puedes cancelar una orden ya cobrada.");

        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Cancelled);
        await _orderRepository.SetCancelledByAsync(orderId, cancelledByName);

        // La cancelacion devuelve stock de productos y notifica el nuevo saldo visible.
        foreach (var item in order.Items)
        {
            await _productRepository.RestoreStockAsync(item.ProductId, item.Quantity);

            var updated = await _productRepository.GetByIdAsync(item.ProductId);

            if (updated != null)
                await _notificationService.NotifyStockUpdated(item.ProductId, updated.Stock);
        }

        // Ordenes antiguas pueden no traer reservas persistidas; se reconstruyen si hace falta.
        var ingredientReservations = order.IngredientReservations?.Count > 0
            ? order.IngredientReservations
            : await BuildIngredientReservationsFallbackAsync(order);

        await RestoreReservedIngredientsAsync(ingredientReservations);

        if (order.TableNumber > 0)
        {
            var stillActive = await _orderRepository.HasActiveOrdersForTableAsync(order.TableNumber, orderId);
            var hasPendingPayment = await _orderRepository.HasPendingPaymentForTableAsync(order.TableNumber);
            var hasPendingCleanup = await HasPendingCleanupForTableAsync(order.TableNumber);

            if (!stillActive && !hasPendingPayment && !hasPendingCleanup)
            {
                await _tableRepository.ClearServiceStateAsync(order.TableNumber, false);
                var updatedTable = await _tableRepository.GetByNumberAsync(order.TableNumber);
                if (updatedTable != null)
                    await _notificationService.NotifyTableStatusUpdated(updatedTable);
            }
        }

        var cancelledOrder = await GetOrderById(orderId);
        var orderDto = cancelledOrder ?? MapToDto(order);
        await _notificationService.NotifyOrderCancelled(orderDto);
    }

    private async Task<bool> HasPendingCleanupForTableAsync(int tableNumber)
    {
        // Reutiliza la misma regla de "ultima orden pagada sin reemplazo posterior".
        var cleanupCandidate = (await _orderRepository.GetHistoryAsync())
            .Where(o => o.TableNumber == tableNumber)
            .Where(o => o.Status == OrderStatus.Delivered && o.IsPaid && !o.IsCleanupCompleted)
            .OrderByDescending(o => o.PaidAt ?? o.DeliveredAt ?? o.CreatedAt)
            .FirstOrDefault();

        if (cleanupCandidate == null)
            return false;

        return !await _orderRepository.HasNewerOrdersForTableAsync(
            tableNumber,
            cleanupCandidate.CreatedAt,
            cleanupCandidate.Id!
        );
    }

    private async Task<List<OrderIngredientReservation>> BuildIngredientReservationsFallbackAsync(Order order)
    {
        // Compatibilidad con ordenes antiguas que no persistieron reservas de ingredientes.
        var reservations = new Dictionary<string, OrderIngredientReservation>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in order.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null) continue;

            foreach (var recipeItem in product.Recipe ?? [])
            {
                var ingredientId = recipeItem.IngredientId ?? string.Empty;
                if (string.IsNullOrWhiteSpace(ingredientId)) continue;

                if (!reservations.TryGetValue(ingredientId, out var reservation))
                {
                    reservation = new OrderIngredientReservation
                    {
                        IngredientId = ingredientId,
                        IngredientName = recipeItem.IngredientName,
                        Unit = recipeItem.Unit,
                        Quantity = 0,
                    };
                    reservations[ingredientId] = reservation;
                }

                reservation.Quantity += recipeItem.QuantityRequired * item.Quantity;
            }
        }

        return reservations.Values.ToList();
    }

    private async Task RestoreDeductedProductsAsync(IEnumerable<(string ProductId, int Quantity)> deductedProducts)
    {
        foreach (var entry in deductedProducts)
        {
            if (string.IsNullOrWhiteSpace(entry.ProductId) || entry.Quantity <= 0) continue;
            await _productRepository.RestoreStockAsync(entry.ProductId, entry.Quantity);
        }
    }

    private async Task RestoreReservedIngredientsAsync(IEnumerable<OrderIngredientReservation> reservations)
    {
        foreach (var reservation in reservations)
        {
            if (string.IsNullOrWhiteSpace(reservation?.IngredientId) || reservation.Quantity <= 0) continue;
            await _ingredientRepository.RestoreStockAsync(reservation.IngredientId, reservation.Quantity);
        }
    }

    public async Task<OrderDto?> GetOrderById(string id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        return order != null ? MapToDto(order) : null;
    }

    public async Task<List<OrderDto>> GetActiveOrders()
    {
        var settings = await _settingsService.GetAsync();

        // Cuando el takeout exige prepago, cocina no debe ver la orden hasta que caja la habilite.
        return (await _orderRepository.GetActiveOrdersAsync())
            .Where(order =>
                !(settings.TakeoutRequirePrepayment &&
                  order.TableNumber == 0 &&
                  !order.IsPaid))
            .Select(MapToDto)
            .ToList();
    }

    public async Task<List<OrderDto>> GetHistory()
    {
        var settings = await _settingsService.GetAsync();
        var history = await _orderRepository.GetHistoryAsync();

        // Para caja, los pedidos takeout pendientes de prepago tambien cuentan como historial operativo.
        if (settings.TakeoutRequirePrepayment)
        {
            var takeoutPendingPayment = (await _orderRepository.GetActiveOrdersAsync())
                .Where(order => order.TableNumber == 0 && !order.IsPaid);

            history = history
                .Concat(takeoutPendingPayment)
                .OrderByDescending(order => order.DeliveredAt ?? order.CreatedAt)
                .ToList();
        }

        return history
            .Select(MapToDto)
            .ToList();
    }

    public async Task<List<OrderDto>> GetMyOrders(string userId) =>
        (await _orderRepository.GetOrdersByWaiterAsync(userId))
            .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
            .Select(MapToDto)
            .ToList();

    private static string NormalizeTakeoutDestination(string destination)
    {
        if (string.IsNullOrWhiteSpace(destination)) return AllowedTakeoutDestinations[0];

        // Se admite "Mesa N" para pedidos que salen de una mesa pero terminan como para llevar.
        if (destination.Trim().StartsWith("Mesa ", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(destination.Trim()[5..], out var tableNumber) &&
            tableNumber > 0)
        {
            return $"Mesa {tableNumber}";
        }

        var allowedDestination = AllowedTakeoutDestinations.FirstOrDefault(
            item => string.Equals(item, destination.Trim(), StringComparison.OrdinalIgnoreCase));

        if (allowedDestination == null)
            throw new InvalidOperationException("El destino para llevar debe ser Mostrador, Autoservicio, Delivery o una mesa valida.");

        return allowedDestination;
    }

    /// <summary>
    /// Traduce la entidad de dominio a DTO con los campos derivados que consume
    /// la UI, como montos pendientes y cantidades ya cobradas por linea.
    /// </summary>
    private static OrderDto MapToDto(Order order) => new()
    {
        Id = order.Id!,
        TableNumber = order.TableNumber,
        CorrelativeNumber = order.CorrelativeNumber,
        CorrelativeCode = order.CorrelativeCode,
        CustomerName = order.CustomerName,
        TakeoutDestination = order.TakeoutDestination,
        DeliveryAddress = order.DeliveryAddress,
        WaiterId = order.WaiterId,
        WaiterName = order.WaiterName,
        PreparedByName = order.PreparedByName,
        PaidByName = order.PaidByName,
        CancelledByName = order.CancelledByName,
        PaymentMethod = order.PaymentMethod,
        ReceiptNumber = order.ReceiptNumber,
        DocumentType = order.DocumentType,
        InvoiceRequested = order.InvoiceRequested,
        TaxableAmount = order.TaxableAmount,
        TaxAmount = order.TaxAmount,
        TotalAmount = order.TotalAmount,
        PaidAmount = order.Items?.Sum(i => i.UnitPrice * i.PaidQuantity) ?? 0,
        RemainingAmount = order.Items?.Sum(i => i.UnitPrice * Math.Max(0, i.Quantity - i.PaidQuantity)) ?? 0,
        Status = order.Status,
        CreatedAt = order.CreatedAt,
        StartedAt = order.StartedAt,
        ReadyAt = order.ReadyAt,
        DeliveredAt = order.DeliveredAt,
        IsPaid = order.IsPaid,
        PaidAt = order.PaidAt,
        IsCleanupCompleted = order.IsCleanupCompleted,
        CleanupCompletedAt = order.CleanupCompletedAt,
        Items = order.Items?.Select((i, index) => new OrderItemDto
        {
            LineIndex = index,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            PaidQuantity = i.PaidQuantity,
            RemainingQuantity = Math.Max(0, i.Quantity - i.PaidQuantity),
            UnitPrice = i.UnitPrice,
            Notes = i.Notes,
            CurrentStock = 0
        }).ToList() ?? []
    };
}
