﻿using global::Nop.Core.Data;
using global::Nop.Services.Events;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Qixol.Nop.Promo.Core.Domain.AttributeValues;
using Qixol.Nop.Promo.Core.Domain.Products;
using Qixol.Nop.Promo.Core.Domain.Promo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qixol.Nop.Promo.Services.ProductMapping
{
    public class ProductMappingService : IProductMappingService
    {
        private readonly IRepository<ProductMappingItem> _repository;
        private readonly IProductService _productService;
        private readonly IEventPublisher _eventPublisher;
        private readonly PromoSettings _promoSettings;
        private readonly IProductAttributeParser _productAttributeParser;

        public ProductMappingService(IRepository<ProductMappingItem> repository,
                                     IProductService productService,
                                     IEventPublisher eventPublisher,
                                     PromoSettings promoSettings,
                                     IProductAttributeParser productAttributeParser)
        {
            this._repository = repository;
            this._productService = productService;
            this._eventPublisher = eventPublisher;
            this._promoSettings = promoSettings;
            this._productAttributeParser = productAttributeParser;
        }

        public ProductMappingItem RetrieveFromAttributesXml(Product product, string attributesXml)
        {
            if (product.IsGiftCard)
                attributesXml = string.Empty;

            if (product.ProductAttributeCombinations == null || product.ProductAttributeCombinations.Count == 0)
                attributesXml = string.Empty;

            if (product.ProductAttributeMappings != null && product.ProductAttributeMappings.Count > _promoSettings.MaximumAttributesForVariants)
                attributesXml = string.Empty;

            return this._repository.Table.Where(pm => pm.EntityId == product.Id && pm.EntityName == EntityAttributeName.Product &&
                (pm.AttributesXml ?? string.Empty).Equals((attributesXml ?? string.Empty), StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        public ProductMappingItem RetrieveFromAttributesXml(OrderItem orderItem)
        {
            var attributesXml = orderItem.AttributesXml;

            if (orderItem.Product.IsGiftCard)
                attributesXml = string.Empty;

            return this._repository.Table.Where(pm => pm.EntityId == orderItem.Product.Id && pm.EntityName == EntityAttributeName.Product &&
                (pm.AttributesXml ?? string.Empty).Equals((attributesXml ?? string.Empty), StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        public ProductMappingItem RetrieveFromShoppingCartItem(ShoppingCartItem shoppingCartItem)
        {
            string attributesXml = string.Empty;

            var product = _productService.GetProductById(shoppingCartItem.ProductId);

            var productAttributeValues = _productAttributeParser.ParseProductAttributeValues(shoppingCartItem.AttributesXml);
            if (productAttributeValues != null && productAttributeValues.ToList().Any())
            {
                attributesXml = shoppingCartItem.AttributesXml;
            }

            return this._repository.Table.Where(pm => pm.EntityId == product.Id && pm.EntityName == EntityAttributeName.Product &&
                (pm.AttributesXml ?? string.Empty).Equals((attributesXml ?? string.Empty), StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        public ProductMappingItem RetrieveFromVariantCode(int productId, string variantcode)
        {
            if (string.IsNullOrEmpty(variantcode))
            {
                List<ProductMappingItem> productMappingItems = this._repository.Table.Where(pm => pm.EntityId == productId).ToList();
                if (productMappingItems == null)
                    return null;
                if (productMappingItems.Count != 1)
                    return null;
                if (!productMappingItems.First().NoVariants)
                    return null;
                return productMappingItems.First();
            }
            else
            {
                return this._repository.Table.Where(pm => pm.EntityId == productId && pm.VariantCode.Equals(variantcode, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            }
        }

        public IQueryable<ProductMappingItem> RetrieveAllVariantsByProductId(int sourceEntityId, string sourceEntityName, bool getGroupedProducts)
        {
            List<int> lookForProductIds = new List<int>();

            if (getGroupedProducts)
            {
                var product = _productService.GetProductById(sourceEntityId);
                if (product.ProductType == global::Nop.Core.Domain.Catalog.ProductType.GroupedProduct)
                {
                    var groupProducts = _productService.GetAssociatedProducts(product.Id);
                    lookForProductIds.AddRange(groupProducts.Select(gp => gp.Id).ToList());
                }
                else
                {
                    lookForProductIds.Add(sourceEntityId);
                }
            }
            else
            {
                lookForProductIds.Add(sourceEntityId);
            }

            return this._repository.Table.Where(pm => lookForProductIds.Contains(pm.EntityId) && pm.EntityName == sourceEntityName);
        }

        public void Insert(ProductMappingItem item)
        {
            item.CreatedOnUtc = DateTime.UtcNow;
            _repository.Insert(item);
            _eventPublisher.EntityInserted<ProductMappingItem>(item);
        }

        public void Delete(ProductMappingItem item)
        {
            _repository.Delete(item);
            _eventPublisher.EntityDeleted<ProductMappingItem>(item);
        }
    }
}
