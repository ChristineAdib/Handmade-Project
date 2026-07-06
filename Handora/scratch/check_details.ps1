try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    # 1. Delivery Methods Columns
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DeliveryMethods'"
    $r = $cmd.ExecuteReader()
    Write-Output "=== DeliveryMethods Columns ==="
    while ($r.Read()) {
        Write-Output "Column: Name=$($r['COLUMN_NAME']), Type=$($r['DATA_TYPE'])"
    }
    $r.Close()
    
    # 2. Delivery Methods
    $cmd.CommandText = "SELECT TOP 5 Id, ShortName FROM DeliveryMethods"
    $r = $cmd.ExecuteReader()
    Write-Output "`n=== Delivery Methods ==="
    while ($r.Read()) {
        Write-Output "Delivery: Id=$($r['Id']), Name=$($r['ShortName'])"
    }
    $r.Close()
    
    # 3. Product Shop
    $cmd.CommandText = @"
        SELECT ShopId, TitleEn FROM Products WHERE Id = 'a0e06fc4-e44e-4f31-8367-5ee47ee09008'
"@
    $r = $cmd.ExecuteReader()
    Write-Output "`n=== Product Info ==="
    while ($r.Read()) {
        Write-Output "ShopId: $($r['ShopId']), Title: $($r['TitleEn'])"
    }
    $r.Close()
    
    # 4. Product Images
    $cmd.CommandText = @"
        SELECT ImageUrl FROM ProductImages WHERE ProductId = 'a0e06fc4-e44e-4f31-8367-5ee47ee09008'
"@
    $r = $cmd.ExecuteReader()
    Write-Output "`n=== Product Images ==="
    while ($r.Read()) {
        Write-Output "Image: $($r['ImageUrl'])"
    }
    $r.Close()
    
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
