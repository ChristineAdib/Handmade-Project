try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 5 Id, ShortName FROM DeliveryMethods"
    $r = $cmd.ExecuteReader()
    Write-Output "=== Delivery Methods ==="
    while ($r.Read()) {
        Write-Output "Method: Id=$($r['Id']), Name=$($r['ShortName'])"
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
