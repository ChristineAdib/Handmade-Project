$connectionString = "Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;"
$registerUrl = "https://handauraa.runasp.net/api/auth/register"
$verifyUrl = "https://handauraa.runasp.net/api/auth/verify-otp"
$loginUrl = "https://handauraa.runasp.net/api/auth/login"
$reviewUrl = "https://handauraa.runasp.net/api/reviews"

$productId = "7fb98e19-7380-4d34-a9c8-1c868855fa6e"
$productName = "Hand-painted Ceramic Mug with Playful Design"
$pictureUrl = "https://res.cloudinary.com/dyyhjgtuw/image/upload/v1782951876/products/iwnrxnxnfjevavo0ubhx.jpg"
$price = 400.00
$shopId = "aaaaaaaa-0000-0000-0000-000000000003"
$deliveryMethodId = "dddddddd-0000-0000-0000-000000000001"

$reviews = @(
    @{ Rating = 5; Comment = "Absolutely perfect! The glazing is beautiful and it holds heat really well. My new favorite mug." },
    @{ Rating = 4; Comment = "Very beautiful handcraft. The design is unique and adorable. The handle could be slightly larger, but overall I love it!" },
    @{ Rating = 5; Comment = "Exceeded my expectations! The colors are vibrant and it arrived in a very secure package." },
    @{ Rating = 3; Comment = "The design is very cute, but it is a bit smaller than I expected from the photos. Still a nice mug." },
    @{ Rating = 5; Comment = "Incredible quality! You can tell a lot of care went into hand-painting this. Will buy more as gifts." },
    @{ Rating = 4; Comment = "Excellent piece. Fast shipping and great customer service. A lovely addition to my daily coffee routine." }
)

foreach ($i in 1..6) {
    $email = "testreview_u$i@example.com"
    $name = "Test User Review $i"
    $password = "Buyer@123"
    $rev = $reviews[$i - 1]

    Write-Output "----------------------------------------"
    Write-Output ("Processing User " + $i + ": " + $email)

    # 1. Register User (expecting 400 Bad Request due to SMTP error, but user will be created)
    try {
        $boundary = [System.Guid]::NewGuid().ToString()
        $LF = "`r`n"
        $bodyLines = (
            "--$boundary",
            'Content-Disposition: form-data; name="Name"',
            "",
            $name,
            "--$boundary",
            'Content-Disposition: form-data; name="Email"',
            "",
            $email,
            "--$boundary",
            'Content-Disposition: form-data; name="Password"',
            "",
            $password,
            "--$boundary",
            'Content-Disposition: form-data; name="ConfirmPassword"',
            "",
            $password,
            "--$boundary",
            'Content-Disposition: form-data; name="Role"',
            "",
            "Buyer",
            "--$boundary--"
        ) -join $LF

        $headers = @{
            "Content-Type" = "multipart/form-data; boundary=$boundary"
        }

        Write-Output "Sending registration request..."
        $null = Invoke-RestMethod -Uri $registerUrl -Method Post -Headers $headers -Body $bodyLines
    } catch {
        # Catch and check if SMTP error (normal behavior here)
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $body = $reader.ReadToEnd()
            if ($body -like "*Failed to send OTP email*") {
                Write-Output "User registered (SMTP error expected)."
            } else {
                Write-Output "Registration failed with unexpected error: $body"
                continue
            }
        } else {
            Write-Output "Registration connection error: $_"
            continue
        }
    }

    # 2. Get OTP and UserId from DB
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 OtpCode, UserId FROM OtpVerifications WHERE Email = @Email ORDER BY ExpiresAt DESC"
    $cmd.Parameters.AddWithValue("@Email", $email) | Out-Null
    $r = $cmd.ExecuteReader()
    $otp = ""
    $userId = ""
    while ($r.Read()) {
        $otp = $r['OtpCode']
        $userId = $r['UserId']
    }
    $r.Close()
    
    if (-not $otp -or -not $userId) {
        Write-Output "Failed to retrieve OTP/UserId from DB. Skipping."
        $conn.Close()
        continue
    }
    Write-Output "Retrieved OTP: $otp for UserId: $userId"

    # 3. Verify OTP via API
    try {
        $verifyBody = @{
            email = $email
            otpCode = $otp
        } | ConvertTo-Json
        $verifyResponse = Invoke-RestMethod -Uri $verifyUrl -Method Post -Body $verifyBody -ContentType "application/json"
        Write-Output "User verification: $($verifyResponse.success)"
    } catch {
        Write-Output "Failed to verify OTP: $_"
        $conn.Close()
        continue
    }

    # 4. Create Delivered Order and OrderItem in DB to establish eligibility
    try {
        $transaction = $conn.BeginTransaction()
        
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
        
        $transaction.Commit()
        Write-Output "Delivered order created in database."
    } catch {
        if ($transaction) { $transaction.Rollback() }
        Write-Output "Error creating order: $_"
        $conn.Close()
        continue
    } finally {
        $conn.Close()
    }

    # 5. Log in to get Bearer Token
    $token = ""
    try {
        $loginBody = @{
            email = $email
            password = $password
        } | ConvertTo-Json
        $loginResponse = Invoke-RestMethod -Uri $loginUrl -Method Post -Body $loginBody -ContentType "application/json"
        $token = $loginResponse.data.token
    } catch {
        Write-Output "Login failed: $_"
        continue
    }

    # 6. Submit Review via API
    try {
        $reviewBody = @{
            productId = $productId
            rating = $rev.Rating
            comment = $rev.Comment
        } | ConvertTo-Json

        $headers = @{
            "Authorization" = "Bearer $token"
        }

        $reviewResponse = Invoke-RestMethod -Uri $reviewUrl -Method Post -Headers $headers -Body $reviewBody -ContentType "application/json"
        Write-Output "Review submitted! Rating: $($rev.Rating), Comment: '$($rev.Comment)'"
    } catch {
        Write-Output "Review submission failed: $_"
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $body = $reader.ReadToEnd()
            Write-Output "Error Response: $body"
        }
    }
}

Write-Output "----------------------------------------"
Write-Output "All operations finished!"
