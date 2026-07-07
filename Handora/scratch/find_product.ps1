try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Id, TitleEn, Price, AverageRating, ReviewCount FROM Products WHERE Id LIKE 'a0e06fc4%' OR TitleEn LIKE '%Ceramic Mug%'"
    $r = $cmd.ExecuteReader()
    Write-Output "=== Matching Products ==="
    while ($r.Read()) {
        Write-Output "Product: Id=$($r['Id']), Title=$($r['TitleEn']), Price=$($r['Price']), Rating=$($r['AverageRating']), ReviewCount=$($r['ReviewCount'])"
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
