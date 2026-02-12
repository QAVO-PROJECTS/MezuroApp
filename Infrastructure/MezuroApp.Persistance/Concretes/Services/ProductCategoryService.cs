using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.ProductCategories;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.ProductCategory;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Persistance.Concretes.Services;

public class ProductCategoryService:IProductCategoryService
{
    private readonly IProductCategoryReadRepository _productCategoryReadRepository;
    private readonly IProductCategoryWriteRepository _productCategoryWriteRepository;
    private readonly IMapper _mapper;

    public ProductCategoryService(IProductCategoryReadRepository productCategoryReadRepository, IProductCategoryWriteRepository productCategoryWriteRepository, IMapper mapper)
    {
        _productCategoryReadRepository = productCategoryReadRepository;
        _productCategoryWriteRepository = productCategoryWriteRepository;
        _mapper = mapper;
    }

    public async Task AddProductCategory(ProductCategory productCategory)
    {
        await _productCategoryWriteRepository.AddAsync(productCategory);
        await _productCategoryWriteRepository.CommitAsync();
    }

    public async Task DeleteProductCategory(ProductCategory productCategory)
    {
        await _productCategoryWriteRepository.SoftDeleteAsync(productCategory);
        await _productCategoryWriteRepository.CommitAsync();
    }

    public async Task<List<ProductCategoryDto>> GetAllProductCategories()
    {
       var productcategories= await _productCategoryReadRepository.GetAllAsync();
       return _mapper.Map<List<ProductCategoryDto>>(productcategories);

    }
}