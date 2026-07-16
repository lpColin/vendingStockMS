using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingStock.Api.Common;
using VendingStock.Api.Contracts;
using VendingStock.Api.Domain;
using VendingStock.Api.Infrastructure;
namespace VendingStock.Api.Controllers;
[ApiController]
[Route("api/v1")]
public sealed class LookupsController(VendingStockDbContext db) : ControllerBase
{
    [HttpGet("product-category/list")] public Task<ApiResponse> Categories([FromQuery] PageQuery q, CancellationToken ct) => List(db.ProductCategories, q, ct);
    [HttpPost("product-category")] public Task<ApiResponse> CreateCategory(LookupRequest r, CancellationToken ct) => CreateCategoryOrSpec(db.ProductCategories, r, "分类名称已存在", ct);
    [HttpPut("product-category/{id:long}")] public Task<ApiResponse> UpdateCategory(long id, LookupRequest r, CancellationToken ct) => UpdateCategoryOrSpec(db.ProductCategories, id, r, "分类名称已存在", ct);
    [HttpDelete("product-category/{id:long}")] public Task<ApiResponse> DeleteCategory(long id, CancellationToken ct) => Delete(db.ProductCategories, id, ct);
    [HttpGet("product-spec/list")] public Task<ApiResponse> Specs([FromQuery] PageQuery q, CancellationToken ct) => List(db.ProductSpecs, q, ct);
    [HttpPost("product-spec")] public Task<ApiResponse> CreateSpec(LookupRequest r, CancellationToken ct) => CreateCategoryOrSpec(db.ProductSpecs, r, "规格名称已存在", ct);
    [HttpPut("product-spec/{id:long}")] public Task<ApiResponse> UpdateSpec(long id, LookupRequest r, CancellationToken ct) => UpdateCategoryOrSpec(db.ProductSpecs, id, r, "规格名称已存在", ct);
    [HttpDelete("product-spec/{id:long}")] public Task<ApiResponse> DeleteSpec(long id, CancellationToken ct) => Delete(db.ProductSpecs, id, ct);
    [HttpGet("product-brand/list")] public Task<ApiResponse> Brands([FromQuery] PageQuery q, CancellationToken ct) => List(db.ProductBrands, q, ct);
    [HttpPost("product-brand")] public async Task<ApiResponse> CreateBrand(LookupRequest r, CancellationToken ct) { ValidateName(r.Name); if (await db.ProductBrands.AnyAsync(x => x.Name == r.Name.Trim(), ct)) throw new BusinessException("品牌名称已存在", 4001); var entity = new ProductBrand { Name = r.Name.Trim(), SortOrder = r.SortOrder }; db.ProductBrands.Add(entity); await db.SaveChangesAsync(ct); return ApiResponse.Ok(new { id = entity.Id }); }
    [HttpPut("product-brand/{id:long}")] public async Task<ApiResponse> UpdateBrand(long id, LookupRequest r, CancellationToken ct) { ValidateName(r.Name); var entity = await db.ProductBrands.FindAsync([id], ct) ?? throw new BusinessException("记录不存在", 4040); if (await db.ProductBrands.AnyAsync(x => x.Id != id && x.Name == r.Name.Trim(), ct)) throw new BusinessException("品牌名称已存在", 4001); entity.Name = r.Name.Trim(); entity.SortOrder = r.SortOrder; await db.SaveChangesAsync(ct); return ApiResponse.Ok(); }
    [HttpDelete("product-brand/{id:long}")] public Task<ApiResponse> DeleteBrand(long id, CancellationToken ct) => Delete(db.ProductBrands, id, ct);
    private static async Task<ApiResponse> List<TEntity>(DbSet<TEntity> set, PageQuery q, CancellationToken ct) where TEntity : Entity { var total = await set.CountAsync(ct); var list = await set.OrderBy(x => EF.Property<int>(x, "SortOrder")).ThenBy(x => x.Id).Skip(q.Skip).Take(q.Take).Select(x => new { x.Id, Name = EF.Property<string>(x, "Name"), SortOrder = EF.Property<int>(x, "SortOrder") }).ToListAsync(ct); return ApiResponse.Ok(new PagedResult<object>(list.Cast<object>().ToArray(), total, q.Page, q.Take)); }
    private async Task<ApiResponse> CreateCategoryOrSpec<TEntity>(DbSet<TEntity> set, LookupRequest r, string duplicate, CancellationToken ct) where TEntity : Entity, new() { ValidateName(r.Name); if (await set.AnyAsync(x => EF.Property<string>(x, "Name") == r.Name.Trim(), ct)) throw new BusinessException(duplicate, 4001); var entity = new TEntity(); if (entity is ProductCategory category) { category.Name = r.Name.Trim(); category.SortOrder = r.SortOrder; } else if (entity is ProductSpec spec) { spec.Name = r.Name.Trim(); spec.SortOrder = r.SortOrder; } set.Add(entity); await db.SaveChangesAsync(ct); return ApiResponse.Ok(new { id = entity.Id }); }
    private async Task<ApiResponse> UpdateCategoryOrSpec<TEntity>(DbSet<TEntity> set, long id, LookupRequest r, string duplicate, CancellationToken ct) where TEntity : Entity { ValidateName(r.Name); var entity = await set.FindAsync([id], ct) ?? throw new BusinessException("记录不存在", 4040); if (await set.AnyAsync(x => x.Id != id && EF.Property<string>(x, "Name") == r.Name.Trim(), ct)) throw new BusinessException(duplicate, 4001); if (entity is ProductCategory category) { category.Name = r.Name.Trim(); category.SortOrder = r.SortOrder; } else if (entity is ProductSpec spec) { spec.Name = r.Name.Trim(); spec.SortOrder = r.SortOrder; } await db.SaveChangesAsync(ct); return ApiResponse.Ok(); }
    private async Task<ApiResponse> Delete<TEntity>(DbSet<TEntity> set, long id, CancellationToken ct) where TEntity : Entity { var entity = await set.FindAsync([id], ct) ?? throw new BusinessException("记录不存在", 4040); set.Remove(entity); await db.SaveChangesAsync(ct); return ApiResponse.Ok(); }
    private static void ValidateName(string name) { if (string.IsNullOrWhiteSpace(name)) throw new BusinessException("名称不能为空"); }
}
