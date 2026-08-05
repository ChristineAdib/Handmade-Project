try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    $productId = [Guid]"7fb98e19-7380-4d34-a9c8-1c868855fa6e"
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
        SELECT o.Id, o.UserId, o.BuyerEmail, o.Status 
        FROM Orders o
        JOIN OrderItems oi ON o.Id = oi.OrderId
        WHERE oi.Product_ProductId = @ProductId
"@
    $cmd.Parameters.AddWithValue("@ProductId", $productId) | Out-Null
    $r = $cmd.ExecuteReader()
    Write-Output "=== Orders ==="
    while ($r.Read()) {
        Write-Output "Order: Id=$($r['Id']), UserId=$($r['UserId']), BuyerEmail=$($r['BuyerEmail']), Status=$($r['Status'])"
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
