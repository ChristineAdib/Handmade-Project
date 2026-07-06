$connectionString = "Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;"

# Define the 10 reviews
$reviewsData = @(
    @{
        UserId = "05c9cdf7-9224-4285-b235-2aca352ae800"
        Email = "saharabdouma@gmail.com"
        Rating = 5
        Comment = "Absolutely perfect! The quality is amazing and it looks even better in person."
        DaysAgo = 2.5
    },
    @{
        UserId = "112d9cd3-a5ac-4ae9-9b13-07efd99b03f5"
        Email = "testuser1@example.com"
        Rating = 5
        Comment = "Stunning ceramic mug, very well made and holds heat perfectly."
        DaysAgo = 2.2
    },
    @{
        UserId = "163c74af-a3ce-4e5a-ba1b-a7ea1e0625c5"
        Email = "christinaadiiib@gmail.com"
        Rating = 4
        Comment = "Very beautiful handcraft. Delivery was fast and packaging was secure."
        DaysAgo = 1.9
    },
    @{
        UserId = "1da92fef-a3c8-4e31-820c-9e781ef385b7"
        Email = "saraadib950@gmail.com"
        Rating = 5
        Comment = "I love the unique design! Highly recommend this shop."
        DaysAgo = 1.6
    },
    @{
        UserId = "314ef698-6714-454b-b38c-4f2317783643"
        Email = "rodgaber12@gmail.com"
        Rating = 4
        Comment = "Excellent mug, very comfortable to hold. Great addition to my collection."
        DaysAgo = 1.3
    },
    @{
        UserId = "3279d19c-7632-451d-9aff-16198a558893"
        Email = "am4126199@gmail.com"
        Rating = 1
        Comment = "Very disappointed. It arrived with a crack in the handle."
        DaysAgo = 1.0
    },
    @{
        UserId = "376af86c-1dab-4de5-b7ce-95e9ebd6428a"
        Email = "stevenayman31@gmail.com"
        Rating = 2
        Comment = "The color is much darker than the photos. Quality feels quite cheap for the price."
        DaysAgo = 0.7
    },
    @{
        UserId = "3a95f4a7-773d-4a3f-9460-e03e171192c9"
        Email = "rwidajaber@gmail.com"
        Rating = 2
        Comment = "Smaller than expected, and the glaze has some rough spots on the rim."
        DaysAgo = 0.4
    },
    @{
        UserId = "421ba2ea-0fb2-40ce-81ff-03df1d6bb884"
        Email = "stevena6010@gmail.com"
        Rating = 1
        Comment = "Terrible experience, it leaked from the bottom on the first use."
        DaysAgo = 0.2
    },
    @{
        UserId = "4543db94-1690-499a-ada8-eb3a75ee53aa"
        Email = "ahmedkamalpay414@gmail.com"
        Rating = 2
        Comment = "Not worth the money. Handcrafted should mean quality, but this feels rushed."
        DaysAgo = 0.05
    }
)

$productId = "a0e06fc4-e44e-4f31-8367-5ee47ee09008"
$productName = "Handmade Ceramic Mug"
$pictureUrl = "https://res.cloudinary.com/dyyhjgtuw/image/upload/v1782951876/products/iwnrxnxnfjevavo0ubhx.jpg"
$price = 95.00
$shopId = "aaaaaaaa-0000-0000-0000-000000000002"
$deliveryMethodId = "dddddddd-0000-0000-0000-000000000001"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $transaction = $conn.BeginTransaction()
    
    # 1. Clean up existing reviews if any exist (to avoid duplicates or conflicts)
    $cmd = $conn.CreateCommand()
    $cmd.Transaction = $transaction
    $cmd.CommandText = "DELETE FROM Reviews WHERE ProductId = @ProductId"
    $cmd.Parameters.AddWithValue("@ProductId", $productId) | Out-Null
    $deletedCount = $cmd.ExecuteNonQuery()
    Write-Output "Cleaned up $deletedCount existing reviews."
    
    # 2. Add orders, order items and reviews for each user
    foreach ($rev in $reviewsData) {
        $userId = $rev.UserId
        $email = $rev.Email
        $rating = $rev.Rating
        $comment = $rev.Comment
        $createdAt = [DateTime]::UtcNow.AddDays(-$rev.DaysAgo)
        
        # Check if user already has a delivered order for this product
        $cmdCheck = $conn.CreateCommand()
        $cmdCheck.Transaction = $transaction
        $cmdCheck.CommandText = @"
            SELECT COUNT(1) FROM Orders o
            JOIN OrderItems oi ON o.Id = oi.OrderId
            WHERE o.UserId = @UserId AND o.Status = 'Delivered' AND oi.Product_ProductId = @ProductId
"@
        $cmdCheck.Parameters.AddWithValue("@UserId", $userId) | Out-Null
        $cmdCheck.Parameters.AddWithValue("@ProductId", $productId) | Out-Null
        $hasOrder = [int]$cmdCheck.ExecuteScalar()
        
        $orderId = [Guid]::NewGuid().ToString()
        if ($hasOrder -eq 0) {
            # Insert Order
            $cmdOrder = $conn.CreateCommand()
            $cmdOrder.Transaction = $transaction
            $cmdOrder.CommandText = @"
                INSERT INTO Orders (Id, PaymentIntentId, PaymobOrderId, IsFundsReleased, DeliveredAt, TotalAmount, SellerAmount, PlatformCommission, BuyerEmail, OrderDate, Status, DeliveryMethodId, SubTotal, CouponId, DiscountAmount, Notes, UserId, PaymentStatus, CreatedAt, IsDeleted)
                VALUES (@Id, NULL, NULL, 1, @DeliveredAt, @TotalAmount, @SellerAmount, @PlatformCommission, @BuyerEmail, @OrderDate, 'Delivered', @DeliveryMethodId, @SubTotal, NULL, 0, 'Demo Order for Review', @UserId, 'Paid', @CreatedAt, 0)
"@
            $cmdOrder.Parameters.AddWithValue("@Id", $orderId) | Out-Null
            $cmdOrder.Parameters.AddWithValue("@DeliveredAt", $createdAt.AddHours(-1)) | Out-Null
            $cmdOrder.Parameters.AddWithValue("@TotalAmount", $price) | Out-Null
            $cmdOrder.Parameters.AddWithValue("@SellerAmount", ($price * 0.90)) | Out-Null
            $cmdOrder.Parameters.AddWithValue("@PlatformCommission", ($price * 0.10)) | Out-Null
            $cmdOrder.Parameters.AddWithValue("@BuyerEmail", $email) | Out-Null
            $cmdOrder.Parameters.AddWithValue("@OrderDate", $createdAt.AddHours(-12)) | Out-Null
            $cmdOrder.Parameters.AddWithValue("@DeliveryMethodId", $deliveryMethodId) | Out-Null
            $cmdOrder.Parameters.AddWithValue("@SubTotal", $price) | Out-Null
            $cmdOrder.Parameters.AddWithValue("@UserId", $userId) | Out-Null
            $cmdOrder.Parameters.AddWithValue("@CreatedAt", $createdAt.AddHours(-12)) | Out-Null
            $cmdOrder.ExecuteNonQuery() | Out-Null
            
            # Insert Order Item
            $cmdItem = $conn.CreateCommand()
            $cmdItem.Transaction = $transaction
            $cmdItem.CommandText = @"
                INSERT INTO OrderItems (Id, Product_ProductId, Product_ProductName, Product_PictureUrl, Quantity, Price, OrderId, ShopId, ProductId, CreatedAt, IsDeleted)
                VALUES (@Id, @Product_ProductId, @Product_ProductName, @Product_PictureUrl, 1, @Price, @OrderId, @ShopId, @ProductId, @CreatedAt, 0)
"@
            $cmdItem.Parameters.AddWithValue("@Id", [Guid]::NewGuid().ToString()) | Out-Null
            $cmdItem.Parameters.AddWithValue("@Product_ProductId", $productId) | Out-Null
            $cmdItem.Parameters.AddWithValue("@Product_ProductName", $productName) | Out-Null
            $cmdItem.Parameters.AddWithValue("@Product_PictureUrl", $pictureUrl) | Out-Null
            $cmdItem.Parameters.AddWithValue("@Price", $price) | Out-Null
            $cmdItem.Parameters.AddWithValue("@OrderId", $orderId) | Out-Null
            $cmdItem.Parameters.AddWithValue("@ShopId", $shopId) | Out-Null
            $cmdItem.Parameters.AddWithValue("@ProductId", $productId) | Out-Null
            $cmdItem.Parameters.AddWithValue("@CreatedAt", $createdAt.AddHours(-12)) | Out-Null
            $cmdItem.ExecuteNonQuery() | Out-Null
            
            Write-Output "Created delivered order for user $email."
        } else {
            Write-Output "User $email already has a delivered order for this product."
        }
        
        # Insert Review
        $cmdReview = $conn.CreateCommand()
        $cmdReview.Transaction = $transaction
        $cmdReview.CommandText = @"
            INSERT INTO Reviews (Id, Rating, Comment, IsApproved, IsVerifiedPurchase, UserId, ProductId, CreatedAt, IsDeleted)
            VALUES (@Id, @Rating, @Comment, 1, 1, @UserId, @ProductId, @CreatedAt, 0)
"@
        $cmdReview.Parameters.AddWithValue("@Id", [Guid]::NewGuid().ToString()) | Out-Null
        $cmdReview.Parameters.AddWithValue("@Rating", $rating) | Out-Null
        $cmdReview.Parameters.AddWithValue("@Comment", $comment) | Out-Null
        $cmdReview.Parameters.AddWithValue("@UserId", $userId) | Out-Null
        $cmdReview.Parameters.AddWithValue("@ProductId", $productId) | Out-Null
        $cmdReview.Parameters.AddWithValue("@CreatedAt", $createdAt) | Out-Null
        $cmdReview.ExecuteNonQuery() | Out-Null
        
        Write-Output "Inserted $rating-star review for user $email."
    }
    
    # 3. Update Product Stats
    $cmdProduct = $conn.CreateCommand()
    $cmdProduct.Transaction = $transaction
    $cmdProduct.CommandText = @"
        UPDATE Products 
        SET ReviewCount = 10, AverageRating = 2.6
        WHERE Id = @ProductId
"@
    $cmdProduct.Parameters.AddWithValue("@ProductId", $productId) | Out-Null
    $cmdProduct.ExecuteNonQuery() | Out-Null
    Write-Output "Updated Product ReviewCount=10 and AverageRating=2.6."
    
    # 4. Insert or Update ProductReviewSummary
    # Clean up existing summary first
    $cmdCleanSummary = $conn.CreateCommand()
    $cmdCleanSummary.Transaction = $transaction
    $cmdCleanSummary.CommandText = "DELETE FROM ProductReviewSummaries WHERE ProductId = @ProductId"
    $cmdCleanSummary.Parameters.AddWithValue("@ProductId", $productId) | Out-Null
    $cmdCleanSummary.ExecuteNonQuery() | Out-Null
    
    $overallSummary = "Customers have mixed feelings about the Handmade Ceramic Mug. On the positive side, many buyers appreciate the beautiful handcrafted design, excellent clay quality, and how comfortable it is to hold. However, half of the reviewers reported issues, such as items arriving with cracked handles due to fragile packaging, inconsistencies in color compared to listing photos, and minor glazing defects on the rim."
    $prosJson = '["Beautiful unique handcrafted design","High quality ceramic build that holds heat well","Comfortable to hold and use daily"]'
    $consJson = '["Fragile handle susceptible to damage during shipping","Color shade variations from listing photos","Occasional minor defects in glaze and finish"]'
    
    $cmdSummary = $conn.CreateCommand()
    $cmdSummary.Transaction = $transaction
    $cmdSummary.CommandText = @"
        INSERT INTO ProductReviewSummaries (Id, ProductId, OverallSummary, Pros, Cons, LastUpdated, CreatedAt, IsDeleted)
        VALUES (@Id, @ProductId, @OverallSummary, @Pros, @Cons, @LastUpdated, @CreatedAt, 0)
"@
    $cmdSummary.Parameters.AddWithValue("@Id", [Guid]::NewGuid().ToString()) | Out-Null
    $cmdSummary.Parameters.AddWithValue("@ProductId", $productId) | Out-Null
    $cmdSummary.Parameters.AddWithValue("@OverallSummary", $overallSummary) | Out-Null
    $cmdSummary.Parameters.AddWithValue("@Pros", $prosJson) | Out-Null
    $cmdSummary.Parameters.AddWithValue("@Cons", $consJson) | Out-Null
    $cmdSummary.Parameters.AddWithValue("@LastUpdated", [DateTime]::UtcNow) | Out-Null
    $cmdSummary.Parameters.AddWithValue("@CreatedAt", [DateTime]::UtcNow) | Out-Null
    $cmdSummary.ExecuteNonQuery() | Out-Null
    Write-Output "Inserted custom AI summary matching the reviews."
    
    $transaction.Commit()
    Write-Output "Transaction committed successfully!"
} catch {
    if ($transaction) { $transaction.Rollback() }
    Write-Output "Error: $_"
} finally {
    if ($conn) { $conn.Close() }
}
