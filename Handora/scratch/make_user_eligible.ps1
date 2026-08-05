$connectionString = "Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;"

$productId = "7fb98e19-7380-4d34-a9c8-1c868855fa6e"
$productName = "Hand-painted Ceramic Mug with Playful Design"
$pictureUrl = "https://res.cloudinary.com/dyyhjgtuw/image/upload/v1782951876/products/iwnrxnxnfjevavo0ubhx.jpg"
$price = 400.00
$shopId = "aaaaaaaa-0000-0000-0000-000000000003"
$deliveryMethodId = "dddddddd-0000-0000-0000-000000000001"
$userId = "buyer-0000-0000-0000-000000000001"
$email = "sara@gmail.com"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $transaction = $conn.BeginTransaction()
    
    # Check if user already has a delivered order for this product
    $cmdCheck = $conn.CreateCommand()
    $cmdCheck.Transaction = $transaction
    $cmdCheck.CommandText = @"
        SELECT COUNT(1) FROM Orders o
        JOIN OrderItems oi ON o.Id = oi.OrderId
        WHERE o.UserId = @UserId AND o.Status = 'Delivered' AND oi.Product_ProductId = @ProductId
"@
    $cmdCheck.Parameters.AddWithValue("@UserId", $userId) | Out-Null
    $cmdCheck.Parameters.AddWithValue("@ProductId", [Guid]$productId) | Out-Null
    $hasOrder = [int]$cmdCheck.ExecuteScalar()
    
    if ($hasOrder -eq 0) {
        $orderId = [Guid]::NewGuid().ToString()
        $createdAt = [DateTime]::UtcNow.AddDays(-2)
        
        # Insert Order
        $cmdOrder = $conn.CreateCommand()
        $cmdOrder.Transaction = $transaction
        $cmdOrder.CommandText = @"
            INSERT INTO Orders (Id, PaymentIntentId, PaymobOrderId, IsFundsReleased, DeliveredAt, TotalAmount, SellerAmount, PlatformCommission, BuyerEmail, OrderDate, Status, DeliveryMethodId, SubTotal, CouponId, DiscountAmount, Notes, UserId, PaymentStatus, CreatedAt, IsDeleted)
            VALUES (@Id, NULL, NULL, 1, @DeliveredAt, @TotalAmount, @SellerAmount, @PlatformCommission, @BuyerEmail, @OrderDate, 'Delivered', @DeliveryMethodId, @SubTotal, NULL, 0, 'Demo Order for Review', @UserId, 'Paid', @CreatedAt, 0)
"@
        $cmdOrder.Parameters.AddWithValue("@Id", [Guid]$orderId) | Out-Null
        $cmdOrder.Parameters.AddWithValue("@DeliveredAt", $createdAt.AddHours(2)) | Out-Null
        $cmdOrder.Parameters.AddWithValue("@TotalAmount", $price) | Out-Null
        $cmdOrder.Parameters.AddWithValue("@SellerAmount", ($price * 0.90)) | Out-Null
        $cmdOrder.Parameters.AddWithValue("@PlatformCommission", ($price * 0.10)) | Out-Null
        $cmdOrder.Parameters.AddWithValue("@BuyerEmail", $email) | Out-Null
        $cmdOrder.Parameters.AddWithValue("@OrderDate", $createdAt) | Out-Null
        $cmdOrder.Parameters.AddWithValue("@DeliveryMethodId", [Guid]$deliveryMethodId) | Out-Null
        $cmdOrder.Parameters.AddWithValue("@SubTotal", $price) | Out-Null
        $cmdOrder.Parameters.AddWithValue("@UserId", $userId) | Out-Null
        $cmdOrder.Parameters.AddWithValue("@CreatedAt", $createdAt) | Out-Null
        $cmdOrder.ExecuteNonQuery() | Out-Null
        
        # Insert Order Item
        $cmdItem = $conn.CreateCommand()
        $cmdItem.Transaction = $transaction
        $cmdItem.CommandText = @"
            INSERT INTO OrderItems (Id, Product_ProductId, Product_ProductName, Product_PictureUrl, Quantity, Price, OrderId, ShopId, ProductId, CreatedAt, IsDeleted)
            VALUES (@Id, @Product_ProductId, @Product_ProductName, @Product_PictureUrl, 1, @Price, @OrderId, @ShopId, @ProductId, @CreatedAt, 0)
"@
        $cmdItem.Parameters.AddWithValue("@Id", [Guid]::NewGuid().ToString()) | Out-Null
        $cmdItem.Parameters.AddWithValue("@Product_ProductId", [Guid]$productId) | Out-Null
        $cmdItem.Parameters.AddWithValue("@Product_ProductName", $productName) | Out-Null
        $cmdItem.Parameters.AddWithValue("@Product_PictureUrl", $pictureUrl) | Out-Null
        $cmdItem.Parameters.AddWithValue("@Price", $price) | Out-Null
        $cmdItem.Parameters.AddWithValue("@OrderId", [Guid]$orderId) | Out-Null
        $cmdItem.Parameters.AddWithValue("@ShopId", [Guid]$shopId) | Out-Null
        $cmdItem.Parameters.AddWithValue("@ProductId", [Guid]$productId) | Out-Null
        $cmdItem.Parameters.AddWithValue("@CreatedAt", $createdAt) | Out-Null
        $cmdItem.ExecuteNonQuery() | Out-Null
        
        Write-Output "Created delivered order for user $email for product $productId."
    } else {
        Write-Output "User $email already has a delivered order for product $productId."
    }
    
    $transaction.Commit()
    Write-Output "Success!"
} catch {
    if ($transaction) { $transaction.Rollback() }
    Write-Output "Error: $_"
} finally {
    if ($conn) { $conn.Close() }
}
