try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ProductImage'"
    $r = $cmd.ExecuteReader()
    Write-Output "=== ProductImage Columns ==="
    while ($r.Read()) {
        Write-Output "Column: Name=$($r['COLUMN_NAME']), Type=$($r['DATA_TYPE'])"
    }
    $r.Close()
    
    $cmd.CommandText = "SELECT ImageUrl FROM ProductImage WHERE ProductId = 'a0e06fc4-e44e-4f31-8367-5ee47ee09008'"
    $r = $cmd.ExecuteReader()
    Write-Output "`n=== Product ImageUrl ==="
    while ($r.Read()) {
        Write-Output "Image: $($r['ImageUrl'])"
    }
    $r.Close()
    
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
