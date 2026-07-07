try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
        SELECT s.OwnerId, u.UserName, u.Email 
        FROM Products p
        JOIN Shops s ON p.ShopId = s.Id
        JOIN AspNetUsers u ON s.OwnerId = u.Id
        WHERE p.Id = 'a0e06fc4-e44e-4f31-8367-5ee47ee09008'
"@
    $r = $cmd.ExecuteReader()
    Write-Output "=== Shop Owner ==="
    while ($r.Read()) {
        Write-Output "OwnerId: $($r['OwnerId']), UserName: $($r['UserName']), Email: $($r['Email'])"
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
