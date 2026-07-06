try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Id, Rating, Comment, UserId, IsDeleted, CreatedAt FROM Reviews WHERE ProductId = 'a0e06fc4-e44e-4f31-8367-5ee47ee09008'"
    $r = $cmd.ExecuteReader()
    Write-Output "=== Reviews ==="
    while ($r.Read()) {
        Write-Output "Review: Id=$($r['Id']), Rating=$($r['Rating']), Comment=$($r['Comment']), UserId=$($r['UserId']), IsDeleted=$($r['IsDeleted']), CreatedAt=$($r['CreatedAt'])"
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
