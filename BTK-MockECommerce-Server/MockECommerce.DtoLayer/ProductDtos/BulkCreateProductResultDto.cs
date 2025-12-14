namespace MockECommerce.DtoLayer.ProductDtos;

public class BulkCreateProductResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<ProductDto> CreatedProducts { get; set; } = new();
    public List<BulkCreateErrorDto> Errors { get; set; } = new();
}

public class BulkCreateErrorDto
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}
