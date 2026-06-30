
using Backend_ThriftFlowSystem.Data;
using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Backend_ThriftFlowSystem.Services
{
    public class POSServices : IPOSServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IResultReplyServices _reply;
        private readonly ILogger<POSServices> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly Supabase.Client _supabase;

        public POSServices(
            ApplicationDbContext context,
            IResultReplyServices reply,
            ILogger<POSServices> logger,
            IWebHostEnvironment env,
            Supabase.Client supabase)
        {
            _context = context;
            _reply = reply;
            _logger = logger;
            _env = env;
            _supabase = supabase;
        }

        //เก็บผลลัพธ์การคำนวณส่วนลดของสินค้า 1 ชิ้น (ใช้ภายใน CheckoutAsync เท่านั้น)
        private class PricedItem
        {
            public required Product Product { get; set; }
            public required OrderItemRequestDto Request { get; set; }
            public decimal FullLineTotal => Product.SellingPrice * Request.Quantity;
            public decimal DiscountedLineTotal { get; set; } // จะถูกตั้งค่าตอนคำนวณ BUNDLE/PERCENT
            public int? AssignedPromotionId { get; set; }    // null = ไม่เข้าโปรไหนเลย คิดราคาเต็ม
        }

        public async Task<ResultListReply> CheckoutAsync(CheckoutRequest request, int employeeId)
        {
            var reply = new ResultListReply();
            string? uploadedFileName = null;
            List<OrderItemRequestDto> requestItems;

            // แปลงข้อมูล JSON จากตะกร้าสินค้าหน้าบ้าน
            try
            {
                string cleanJson = request.OrderItemsJson?.Trim() ?? "[]";

                if (cleanJson.StartsWith("\"") && cleanJson.EndsWith("\""))
                {
                    cleanJson = cleanJson.Trim('"');
                    cleanJson = cleanJson.Replace("\\\"", "\"");
                }

                if (cleanJson.StartsWith("{") && cleanJson.EndsWith("}"))
                {
                    cleanJson = $"[{cleanJson}]";
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                requestItems = JsonSerializer.Deserialize<List<OrderItemRequestDto>>(cleanJson, options) ?? new List<OrderItemRequestDto>();

                if (!requestItems.Any())
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Cart is empty. Please add items to checkout.";
                    return reply;
                }

                if (requestItems.Any(item => item.Quantity <= 0))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid item quantity. All quantities must be greater than zero.";
                    return reply;
                }

                // AUTO-MERGE: ถ้ายิงบาร์โค้ดหางผ้าซ้ำกันมา 
                requestItems = requestItems
                    .GroupBy(x => x.ProductId)
                    .Select(g => new OrderItemRequestDto
                    {
                        ProductId = g.Key,
                        Quantity = g.Sum(x => x.Quantity)
                    })
                    .ToList();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Failed to parse OrderItemsJson. Raw Data: {request.OrderItemsJson}");
                reply.Result.ToErrorStatus();
                reply.Data = "Invalid order items data format.";
                return reply;
            }

            //CheckSlip (option only TRANSFER)
            if (request.PaymentMethod.ToUpper() == "TRANSFER" && (request.SlipImage == null || request.SlipImage.Length == 0))
            {
                reply.Result.ToErrorStatus();
                reply.Data = "Payment slip image is required for TRANSFER method.";
                return reply;
            }

            //Check Permissions — Dont use SpecialPrice and SkipPromotion both
            if (request.SkipPromotion && request.SpecialPrice.HasValue)
            {
                reply.Result.ToErrorStatus();
                reply.Data = "Cannot combine SkipPromotion and Special Price in the same order.";
                return reply;
            }

            if (request.SpecialPrice.HasValue && request.SpecialPrice.Value < 0)
            {
                reply.Result.ToErrorStatus();
                reply.Data = "Special price cannot be negative.";
                return reply;
            }

            // Save ID (Manager/Owner)
            int? approverId = null;

            bool requiresManagerCheck = request.SpecialPrice.HasValue || request.SkipPromotion;

            if (requiresManagerCheck)
            {
                if (string.IsNullOrWhiteSpace(request.ManagerPin))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = request.SkipPromotion
                        ? "Manager PIN is required to skip promotions."
                        : "Manager PIN is required for special price.";
                    return reply;
                }

                
                var eligibleManagers = await _context.Employees
                    .Include(e => e.Role)
                    .Where(e => e.Role != null && (e.Role.Level == 1 || e.Role.Level == 2))
                    .ToListAsync();

                var manager = eligibleManagers.FirstOrDefault(m =>
                    BCrypt.Net.BCrypt.Verify(request.ManagerPin, m.PinHash)
                );

                if (manager == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid PIN or insufficient permissions.";
                    return reply;
                }


                approverId = manager.Id;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // upload Slip Supabase
                string? slipUrl = null;
                if (request.SlipImage != null && request.SlipImage.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await request.SlipImage.CopyToAsync(memoryStream);
                    var fileExtension = Path.GetExtension(request.SlipImage.FileName);

                    uploadedFileName = $"{Guid.NewGuid()}{fileExtension}";

                    await _supabase.Storage
                        .From("payment-slips")
                        .Upload(memoryStream.ToArray(), uploadedFileName, new Supabase.Storage.FileOptions { Upsert = false });

                    slipUrl = _supabase.Storage.From("payment-slips").GetPublicUrl(uploadedFileName);
                }

                decimal totalAmount = 0;
                var orderItemsToSave = new List<OrderItem>();
                var inventoryLogsToSave = new List<InventoryLog>();

                //  วนลูปเช็คสินค้าและหักสต็อกแบบ Atomic
                //  เก็บ Product object ไว้ใน list ต่างหากเพื่อนำไปคำนวณโปรโมชั่นในขั้นต่อไป
                var pricedItems = new List<PricedItem>();

                foreach (var item in requestItems)
                {
                    var product = await _context.Products
                        .Include(p => p.ProductLot)
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                    if (product == null || !product.IsActive)
                    {
                        throw new Exception($"Product ID {item.ProductId} not found or inactive.");
                    }

                    int rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE ""Products""
                        SET ""QuantityInStock"" = ""QuantityInStock"" - {item.Quantity}
                        WHERE ""Id"" = {item.ProductId} 
                        AND ""QuantityInStock"" - {item.Quantity} >= 0");

                    if (rowsAffected == 0)
                    {
                        throw new Exception($"Insufficient stock for prodoctId :{item.ProductId} '{product.Name}' (SKU: {product.SKU}). Remaining: {product.QuantityInStock}");
                    }

                    await _context.Entry(product).ReloadAsync();
                    int newQuantity = product.QuantityInStock;

                    if (newQuantity <= 0 && product.IsActive)
                    {
                        product.IsActive = false;
                        _context.Products.Update(product);
                    }

                    totalAmount += product.SellingPrice * item.Quantity;

                    pricedItems.Add(new PricedItem
                    {
                        Product = product,
                        Request = item,
                        DiscountedLineTotal = product.SellingPrice * item.Quantity // ค่าเริ่มต้น = ราคาเต็ม จะถูกแก้ทีหลังถ้าเข้าโปร
                    });

                    inventoryLogsToSave.Add(new InventoryLog
                    {
                        EmployeeId = employeeId,
                        ActionType = "OUT_SALE",
                        QuantityChanged = -item.Quantity,
                        Note = "Sold via POS Checkout",
                        ProductId = product.Id
                    });
                }

                //    คำนวณส่วนลดจากโปรโมชั่น
                //    ทำงานก็ต่อเมื่อ: ไม่ได้ SkipPromotion และไม่ได้เคาะ SpecialPrice มา
                decimal finalDiscountAmount = 0;
                var appliedPromotionIds = new List<int>();

                bool shouldApplyPromotions = !request.SkipPromotion && !request.SpecialPrice.HasValue;

                if (shouldApplyPromotions)
                {
                    finalDiscountAmount = await ApplyPromotionsAsync(pricedItems, appliedPromotionIds);
                }

                // คำนวณยอดเงินสุทธิ
                decimal netAmount;
                bool isSpecialPriceApplied = false;

                if (request.SpecialPrice.HasValue)
                {
                    if (request.SpecialPrice.Value > totalAmount)
                    {
                        throw new Exception("Special price cannot be greater than the total amount.");
                    }

                    netAmount = request.SpecialPrice.Value;
                    finalDiscountAmount = totalAmount - netAmount;
                    isSpecialPriceApplied = true;
                }
                else
                {
                    netAmount = totalAmount - finalDiscountAmount;
                }

                if (netAmount < 0) netAmount = 0;

                // สร้างรายการ OrderItem (ใช้ราคาเต็มต่อหน่วยเหมือนเดิม ส่วนลดสะท้อนที่ระดับ Order รวม)
                foreach (var pi in pricedItems)
                {
                    orderItemsToSave.Add(new OrderItem
                    {
                        ProductId = pi.Product.Id,
                        Quantity = pi.Request.Quantity,
                        UnitPrice = pi.Product.SellingPrice,
                        SubTotal = pi.FullLineTotal,
                        CostPrice = pi.Product.ProductLot != null ? pi.Product.ProductLot.CostPerUnit : 0
                    });
                }

                //  สร้างใบเสร็จ
                var order = new Order
                {
                    
                    EmployeeId = employeeId,
                    ApprovedById = approverId,
                    TotalAmount = totalAmount,
                    DiscountAmount = finalDiscountAmount,
                    NetAmount = netAmount,
                    PaymentMethod = request.PaymentMethod.ToUpper(),
                    PaymentSlipUrl = slipUrl,
                    Status = "COMPLETED",
                    PromotionId = appliedPromotionIds.FirstOrDefault() == 0 ? null : appliedPromotionIds.FirstOrDefault(),
                    AppliedPromotionIds = appliedPromotionIds.Any() ? string.Join(",", appliedPromotionIds) : null,
                    IsSpecialPrice = isSpecialPriceApplied,
                    IsPromotionSkipped = request.SkipPromotion,
                    OrderItems = orderItemsToSave
                };

                _context.Orders.Add(order);
                _context.InventoryLogs.AddRange(inventoryLogsToSave);

                await _context.SaveChangesAsync();
                order.ReceiptNumber = $"REC-{DateTime.UtcNow:yyyyMMdd}-{order.Id:D6}";
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Response
                reply.Data = new
                {
                    OrderId = order.Id,
                    ReceiptNumber = order.ReceiptNumber,
                    TotalAmount = order.TotalAmount,
                    DiscountAmount = order.DiscountAmount,
                    NetAmount = order.NetAmount,
                    PaymentMethod = order.PaymentMethod,
                    AppliedPromotionIds = appliedPromotionIds,
                    IsSpecialPrice = order.IsSpecialPrice,
                    IsPromotionSkipped = order.IsPromotionSkipped,
                    ApprovedById = approverId,
                    CreatedAt = order.CreatedAt,
                    ItemCount = orderItemsToSave.Sum(i => i.Quantity)
                };

                reply.Result.ToSuccessStatus("201");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during POS Checkout");

                if (!string.IsNullOrEmpty(uploadedFileName))
                {
                    try
                    {
                        await _supabase.Storage.From("payment-slips").Remove(new List<string> { uploadedFileName });
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, $"Failed to delete slip from Supabase: {uploadedFileName}");
                    }
                }

                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }

            return reply;
        }

        // คำนวณส่วนลดของทุกสินค้าในตะกร้า แล้ว return ยอดส่วนลดรวมทั้งหมด
        // อัปเดต pi.DiscountedLineTotal และ pi.AssignedPromotionId ของแต่ละชิ้นไปด้วย
        private async Task<decimal> ApplyPromotionsAsync(List<PricedItem> pricedItems, List<int> appliedPromotionIds)
        {
            var now = DateTime.UtcNow;

            // ดึงโปรที่ Active อยู่ ณ ตอนนี้ทั้งหมด ไม่รับ PromotionId จาก client เลย
            var activePromotions = await _context.Promotions
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
                .ToListAsync();

            if (!activePromotions.Any())
            {
                return 0; 
            }

            // ลำดับความเจาะจง: Lot (1) > Category (2) > ทั้งร้าน (3)
            foreach (var pi in pricedItems)
            {
                var candidates = activePromotions.Where(promo => IsProductEligible(pi.Product, promo)).ToList();

                if (!candidates.Any())
                {
                    pi.AssignedPromotionId = null; 
                    continue;
                }

                // เลือกโปรที่เจาะจงที่สุดก่อน ถ้าเสมอกันในระดับเดียวกัน เลือกตัวที่ Id ใหม่สุด 
                var chosen = candidates
                    .OrderBy(promo => GetSpecificityRank(promo))   // 1 = เจาะจงสุด มาก่อน
                    .ThenByDescending(promo => promo.Id)            // เสมอกัน -> ใหม่สุดชนะ
                    .First();

                pi.AssignedPromotionId = chosen.Id;
            }

            // กลุ่มสินค้าตาม AssignedPromotionId ที่จับคู่ได้
            var groups = pricedItems
                .Where(pi => pi.AssignedPromotionId.HasValue)
                .GroupBy(pi => pi.AssignedPromotionId!.Value);

            decimal totalDiscount = 0;

            // คำนวณส่วนลดของแต่ละกลุ่ม ตามประเภทโปรของกลุ่มนั้น
            //  คำนวณจากยอดของ "กลุ่มนี้เท่านั้น" ห้าม totalAmount รวมทั้งบิล
            foreach (var group in groups)
            {
                var promo = activePromotions.First(p => p.Id == group.Key);
                var groupItems = group.ToList();
                decimal groupFullTotal = groupItems.Sum(pi => pi.FullLineTotal);
                decimal groupDiscount = 0;

                if (promo.PromotionType == "BUNDLE" && promo.BundlePrice.HasValue
                    && promo.ConditionQuantity.HasValue && promo.ConditionQuantity.Value > 0)
                {
                    groupDiscount = CalculateBundleDiscount(groupItems, promo, out var perItemDiscountedTotal);

                    // เก็บราคาหลังลดต่อชิ้นไว้เผื่อใช้แสดงผลละเอียดในอนาคต 
                    foreach (var pi in groupItems)
                    {
                        pi.DiscountedLineTotal = perItemDiscountedTotal.TryGetValue(pi, out var val) ? val : pi.FullLineTotal;
                    }
                }
                else if (promo.PromotionType == "PERCENT")
                {
                    // PERCENT ไม่เช็ค ConditionQuantity เลย — ลดทุกชิ้นที่เข้าเงื่อนไข Category/Lot ทันที
                    groupDiscount = Math.Round(groupFullTotal * (promo.DiscountValue / 100m), 2);

                    foreach (var pi in groupItems)
                    {
                        decimal itemShareRatio = pi.FullLineTotal / groupFullTotal;
                        pi.DiscountedLineTotal = pi.FullLineTotal - Math.Round(groupDiscount * itemShareRatio, 2);
                    }
                }
                else
                {
                    // โปร type อื่นที่ไม่รองรับ หรือ BUNDLE ที่ตั้งค่าไม่ครบ (ไม่มี BundlePrice/ConditionQuantity)
                    // -> ไม่ลดอะไรเลย ปฏิบัติเหมือนไม่เข้าโปร เพื่อความปลอดภัย (fail-safe ไม่ใช่ fail-discount)
                    groupDiscount = 0;
                }

                if (groupDiscount > 0)
                {
                    totalDiscount += groupDiscount;
                    if (!appliedPromotionIds.Contains(promo.Id))
                    {
                        appliedPromotionIds.Add(promo.Id);
                    }
                }
            }

            return totalDiscount;
        }

        // เช็คว่าสินค้าชิ้นนี้ "เข้าเงื่อนไข" โปรนี้ไหม (ไม่สนใจเรื่อง ConditionQuantity ตรงนี้ แค่เช็คว่า "ประเภทสินค้าตรงกับกฎไหม")
        private bool IsProductEligible(Product product, Promotion promo)
        {
            //Unique (IsGenericSKU = false) ห้ามเข้า BUNDLE เด็ดขาด ไม่ว่า Category/Lot จะตรงแค่ไหน
            if (!product.IsGenericSKU && promo.PromotionType == "BUNDLE")
            {
                return false;
            }

            if (promo.ApplicableProductLotId.HasValue)
            {
                return product.ProductLotId == promo.ApplicableProductLotId.Value;
            }

            if (promo.ApplicableCategoryId.HasValue)
            {
                return product.CategoryId == promo.ApplicableCategoryId.Value;
            }

            return true; 
        }

       
        private int GetSpecificityRank(Promotion promo)
        {
            if (promo.ApplicableProductLotId.HasValue) return 1; // เจาะจงสุด
            if (promo.ApplicableCategoryId.HasValue) return 2;
            return 3; // ทั้งร้าน กว้างที่สุด
        }

        // คำนวณส่วนลดของกลุ่มสินค้าที่เข้าโปร BUNDLE เดียวกัน
        // ถูกสุดจัดเซ็ตก่อน (รักษา margin), เศษที่จัดเซ็ตไม่ครบคิดราคาเต็ม
        private decimal CalculateBundleDiscount(List<PricedItem> groupItems, Promotion promo, out Dictionary<PricedItem, decimal> perItemDiscountedTotal)
        {
            perItemDiscountedTotal = new Dictionary<PricedItem, decimal>();


            var units = new List<(PricedItem Parent, decimal UnitPrice)>();
            foreach (var pi in groupItems)
            {
                for (int i = 0; i < pi.Request.Quantity; i++)
                {
                    units.Add((pi, pi.Product.SellingPrice));
                }
            }

            // ถูกสุดมาก่อน เพื่อจัดเข้าเซ็ตลดราคาก่อน (รักษา margin ของร้าน)
            var sortedUnits = units.OrderBy(u => u.UnitPrice).ToList();

            int conditionQty = promo.ConditionQuantity!.Value;
            decimal bundlePrice = promo.BundlePrice!.Value;

            int totalUnits = sortedUnits.Count;
            int setsCount = totalUnits / conditionQty;          // จำนวนเซ็ตที่จัดได้ครบ
            int leftoverCount = totalUnits % conditionQty;       // เศษที่ไม่ครบเซ็ต คิดราคาเต็ม

            decimal discountedTotal = (setsCount * bundlePrice)
                + sortedUnits.Skip(setsCount * conditionQty).Take(leftoverCount).Sum(u => u.UnitPrice);

            decimal fullTotal = sortedUnits.Sum(u => u.UnitPrice);
            decimal discount = fullTotal - discountedTotal;
            if (discount < 0) discount = 0;

            foreach (var pi in groupItems)
            {
           
                decimal shareRatio = fullTotal == 0 ? 0 : pi.FullLineTotal / fullTotal;
                perItemDiscountedTotal[pi] = pi.FullLineTotal - Math.Round(discount * shareRatio, 2);
            }

            return Math.Round(discount, 2);
        }

        
        private async Task<string> GenerateReceiptNumberAsync()
        {
            var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var todayDate = DateTime.UtcNow.Date;
            var tomorrowDate = todayDate.AddDays(1);

            var lastOrder = await _context.Orders
                .Where(o => o.CreatedAt >= todayDate && o.CreatedAt < tomorrowDate)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            int runningNumber = 1;
            if (lastOrder != null && lastOrder.ReceiptNumber.StartsWith($"REC-{todayStr}-"))
            {
                var lastRunningStr = lastOrder.ReceiptNumber.Replace($"REC-{todayStr}-", "");
                if (int.TryParse(lastRunningStr, out int lastNumber))
                {
                    runningNumber = lastNumber + 1;
                }
            }

            return $"REC-{todayStr}-{runningNumber:D4}";
        }
    }
}