try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 5 Id, Product_ProductId, Product_ProductName, OrderId, ProductId FROM OrderItems"
    $r = $cmd.ExecuteReader()
    Write-Output "=== OrderItems Samples ==="
    while ($r.Read()) {
        Write-Output "OrderItem: Id=$($r['Id']), Product_ProductId=$($r['Product_ProductId']), Name=$($r['Product_ProductName']), OrderId=$($r['OrderId']), ProductId=$($r['ProductId'])"
    }
    $r.Close()
    
    $cmd.CommandText = "SELECT TOP 5 Id, UserId, Status, PaymentStatus FROM Orders"
    $r = $cmd.ExecuteReader()
    Write-Output "`n=== Orders Samples ==="
    while ($r.Read()) {
        Write-Output "Order: Id=$($r['Id']), UserId=$($r['UserId']), Status=$($r['Status']), PaymentStatus=$($r['PaymentStatus'])"
    }
    $r.Close()
    
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
