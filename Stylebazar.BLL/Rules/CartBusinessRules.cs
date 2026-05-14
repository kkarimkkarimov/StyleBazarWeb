using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.Exceptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.BLL.Rules;

/// <summary>
/// Cross-cutting checks for cart operations. Loads the live product (with its
/// sizes) once and hands it back so callers don't have to re-query.
/// </summary>
public class CartBusinessRules
{
    private readonly IProductRepository _products;
    private readonly IProductSizeRepository _sizes;

    public CartBusinessRules(IProductRepository products, IProductSizeRepository sizes)
    {
        _products = products;
        _sizes = sizes;
    }

    public async Task<Product> ProductMustExistAndBeActiveAsync(int productId, CancellationToken ct)
    {
        Product? product = await _products.GetAsync(
            p => p.Id == productId,
            include: q => q.Include(p => p.Sizes),
            enableTracking: false,
            cancellationToken: ct);

        if (product is null)
            throw new NotFoundException(Messages.Cart.ProductNotFound);

        if (!product.IsActive)
            throw new BusinessException(Messages.Cart.ProductInactive);

        return product;
    }

    /// <summary>
    /// If the product defines sizes, the caller must pick one. If it has no
    /// sizes, ProductSizeId must be null.
    /// </summary>
    public ProductSize? ValidateSizeChoice(Product product, int? sizeId)
    {
        bool productHasSizes = product.Sizes.Count > 0;

        if (productHasSizes)
        {
            if (sizeId is null)
                throw new BusinessException(Messages.Cart.SizeRequired);

            ProductSize? size = product.Sizes.FirstOrDefault(s => s.Id == sizeId.Value);
            if (size is null)
                throw new BusinessException(Messages.Cart.SizeBelongsToOtherProduct);

            return size;
        }

        // Product has no sizes — ignore any size sent. (Don't 400 on it; just
        // store null so the cart row is clean.)
        return null;
    }

    public void MustHaveStockFor(Product product, ProductSize? size, int desiredQuantity)
    {
        int available = size is not null ? size.StockQuantity : product.StockQuantity;
        if (desiredQuantity > available)
            throw new BusinessException(Messages.Cart.InsufficientStock);
    }
}
