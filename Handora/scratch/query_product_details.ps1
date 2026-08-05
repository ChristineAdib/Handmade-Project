try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    $productId = [Guid]"7fb98e19-7380-4d34-a9c8-1c868855fa6e"
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT * FROM Products WHERE Id = @ProductId"
    $cmd.Parameters.AddWithValue("@ProductId", $productId) | Out-Null
    $r = $cmd.ExecuteReader()
    while ($r.Read()) {
        Write-Output "Product: Id=$($r['Id']), TitleEn=$($r['TitleEn']), Price=$($r['Price']), ShopId=$($r['ShopId']), AverageRating=$($r['AverageRating']), ReviewCount=$($r['ReviewCount'])"
    }
    $r.Close()

    $cmd2 = $conn.CreateCommand()
    $cmd2.CommandText = "SELECT * FROM ProductImages WHERE ProductId = @ProductId"
    $cmd2.Parameters.AddWithValue("@ProductId", $productId) | Out-Null
    $r2 = $cmd2.ExecuteReader()
    Write-Output "=== Images ==="
    while ($r2.Read()) {
        Write-Output "Image: Id=$($r2['Id']), ImageUrl=$($r2['ImageUrl']), IsMain=$($r2['IsMain'])"
    }
    $r2.Close()

    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
