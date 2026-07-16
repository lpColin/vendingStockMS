namespace VendingStock.Api.Common;
public sealed class PageQuery { public int Page { get; init; } = 1; public int PageSize { get; init; } = 20; public int Skip => (Math.Max(1, Page) - 1) * Math.Clamp(PageSize, 1, 100); public int Take => Math.Clamp(PageSize, 1, 100); }
